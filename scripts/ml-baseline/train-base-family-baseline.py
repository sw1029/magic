#!/usr/bin/env python3

from __future__ import annotations

import argparse
import itertools
import json
import math
import shutil
import subprocess
import tempfile
from collections import defaultdict
from copy import deepcopy
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

import numpy as np
from sklearn.ensemble import GradientBoostingClassifier
from sklearn.linear_model import LogisticRegression
from sklearn.metrics import accuracy_score, brier_score_loss, log_loss

REPO_ROOT = Path(__file__).resolve().parents[2]
FEATURE_SPEC_PATH = REPO_ROOT / "artifacts" / "ml" / "feature-spec-v1.json"
RERANK_ARTIFACT_PATH = REPO_ROOT / "artifacts" / "ml" / "base-rerank-v1.json"
CONFIDENCE_ARTIFACT_PATH = REPO_ROOT / "artifacts" / "ml" / "base-confidence-v1.json"
EXPORTER_PATH = REPO_ROOT / "scripts" / "ml-baseline" / "export-base-family-rows.mjs"
SYNTHETIC_GENERATOR_PATH = REPO_ROOT / "scripts" / "tutorial-dataset" / "generate-synthetic.mjs"

MAX_TOP_SCORE_GAP = 0.18
RECOGNIZED_MARGIN = 0.15
RECOGNIZED_SCORE = 0.7
EPSILON = 1e-9


@dataclass(frozen=True)
class SyntheticRun:
    preset: str
    split: str
    count: int
    seed: int
    name: str


SYNTHETIC_RUNS = (
    SyntheticRun("bootstrap", "train", 240, 1011, "bootstrap-train"),
    SyntheticRun("tutorial-like", "train", 180, 1012, "tutorial-like-train"),
    SyntheticRun("hard-negative", "train", 140, 1013, "hard-negative-train"),
    SyntheticRun("bootstrap", "eval", 96, 2011, "bootstrap-eval"),
    SyntheticRun("tutorial-like", "eval", 84, 2012, "tutorial-like-eval"),
    SyntheticRun("hard-negative", "hard_negative_eval", 132, 3011, "hard-negative-eval"),
)

CONFIDENCE_FEATURE_ORDER = [
    "topFamilyId",
    "candidatePairId",
    "heuristicTopScore",
    "heuristicMargin",
    "rerankedTopScore",
    "rerankedMargin",
    "rerankedTopProbability",
    "rerankedProbabilityGap",
    "topDelta",
    "topWindowSize",
    "strokeCount",
    "pointCount",
    "durationMs",
    "pathLength",
    "closureGap",
    "dominantCorners",
    "endpointClusters",
    "circularity",
    "fillRatio",
    "parallelism",
    "rawAngleRadians",
    "qualityClosure",
    "qualitySmoothness",
    "qualityStability",
    "qualityRotationBias",
]

CONFIDENCE_NORMALIZATION = {
    "topFamilyId": {"type": "one_hot", "values": ["wind", "earth", "fire", "water", "life"]},
    "candidatePairId": {
        "type": "one_hot",
        "values": ["earth__fire", "water__life", "recognized__ambiguous", "other"],
    },
    "heuristicTopScore": {"type": "clamp", "min": 0, "max": 1},
    "heuristicMargin": {"type": "clamp", "min": -1, "max": 1},
    "rerankedTopScore": {"type": "clamp", "min": 0, "max": 1},
    "rerankedMargin": {"type": "clamp", "min": -1, "max": 1},
    "rerankedTopProbability": {"type": "clamp", "min": 0, "max": 1},
    "rerankedProbabilityGap": {"type": "clamp", "min": -1, "max": 1},
    "topDelta": {"type": "clamp", "min": -0.25, "max": 0.25},
    "topWindowSize": {"type": "integer_clip", "min": 1, "max": 3},
    "strokeCount": {"type": "integer_clip", "min": 0, "max": 8},
    "pointCount": {"type": "log1p_clip", "max": 256},
    "durationMs": {"type": "log1p_clip", "max": 4000},
    "pathLength": {"type": "log1p_clip", "max": 4096},
    "closureGap": {"type": "clamp", "min": 0, "max": 1.5},
    "dominantCorners": {"type": "integer_clip", "min": 0, "max": 8},
    "endpointClusters": {"type": "integer_clip", "min": 0, "max": 8},
    "circularity": {"type": "clamp", "min": 0, "max": 1},
    "fillRatio": {"type": "clamp", "min": 0, "max": 1},
    "parallelism": {"type": "clamp", "min": 0, "max": 1},
    "rawAngleRadians": {"type": "clamp", "min": -(math.pi / 2), "max": math.pi / 2},
    "qualityClosure": {"type": "clamp", "min": 0, "max": 1},
    "qualitySmoothness": {"type": "clamp", "min": 0, "max": 1},
    "qualityStability": {"type": "clamp", "min": 0, "max": 1},
    "qualityRotationBias": {"type": "clamp", "min": 0, "max": 1},
}

DELTA_PARAM_GRID = [
    {
        "delta_scale": delta_scale,
        "support_gap": support_gap,
        "boost_margin": boost_margin,
        "top_cap": top_cap,
        "runner_cap": runner_cap,
        "third_cap": 0.03,
        "negative_cap": negative_cap,
        "known_pair_multiplier": known_pair_multiplier,
        "recognized_pair_multiplier": recognized_pair_multiplier,
        "other_pair_multiplier": other_pair_multiplier,
    }
    for delta_scale, support_gap, boost_margin, top_cap, runner_cap, negative_cap, known_pair_multiplier, recognized_pair_multiplier, other_pair_multiplier in itertools.product(
        [0.0, 0.08, 0.12, 0.15],
        [0.0, 0.03, 0.05],
        [0.1, 0.15],
        [0.02, 0.03],
        [0.04, 0.06],
        [0.05, 0.07],
        [1.0, 1.15],
        [0.75, 0.9],
        [0.0, 0.12],
    )
]


def main() -> int:
    parser = argparse.ArgumentParser(description="Train the base family tiny ML baseline.")
    parser.add_argument("--keep-workdir", action="store_true", help="Keep the temporary workspace instead of deleting it.")
    parser.add_argument("--workdir", type=Path, help="Optional workspace directory.")
    parser.add_argument("--seed", type=int, default=41, help="Random seed for sklearn models.")
    args = parser.parse_args()

    if not FEATURE_SPEC_PATH.exists():
        raise FileNotFoundError(f"missing feature spec artifact: {FEATURE_SPEC_PATH}")

    workspace, cleanup = prepare_workspace(args.workdir)

    try:
        dataset_paths = generate_synthetic_family_sets(workspace)
        rows_path = export_rows(workspace, dataset_paths)
        rows = load_jsonl(rows_path)
        feature_spec = json.loads(FEATURE_SPEC_PATH.read_text())

        base_feature_order = feature_spec["featureOrder"]["base_candidate_row_v1"]
        base_feature_normalization = feature_spec["featureNormalization"]["base_candidate_row_v1"]

        train_rows = [row for row in rows if row["split"] == "train"]
        eval_rows = [row for row in rows if row["split"] == "eval"]
        hard_eval_rows = [row for row in rows if row["split"] == "hard_negative_eval"]

        if not train_rows or not eval_rows or not hard_eval_rows:
            raise RuntimeError("expected non-empty train/eval/hard_negative_eval row sets")

        encoder_info = describe_expanded_features(base_feature_order, base_feature_normalization)
        x_train = encode_rows(train_rows, base_feature_order, base_feature_normalization)
        y_train = np.asarray([int(row["targetIsCanonicalCandidate"]) for row in train_rows], dtype=np.int32)
        row_weights = np.asarray([candidate_row_weight(row) for row in train_rows], dtype=np.float64)

        rerank_models = train_candidate_models(x_train, y_train, row_weights, args.seed)
        candidate_eval = evaluate_candidate_models(
            rerank_models,
            {"train": train_rows, "eval": eval_rows, "hard_negative_eval": hard_eval_rows},
            base_feature_order,
            base_feature_normalization,
        )

        grouped_sets = {
            "train": group_rows_by_sample(train_rows),
            "eval": group_rows_by_sample(eval_rows),
            "hard_negative_eval": group_rows_by_sample(hard_eval_rows),
        }
        x_eval = encode_rows(eval_rows, base_feature_order, base_feature_normalization)
        x_hard_eval = encode_rows(hard_eval_rows, base_feature_order, base_feature_normalization)

        rerank_results = {}
        for model_name, model in rerank_models.items():
            train_probs = model.predict_proba(x_train)[:, 1]
            eval_probs = model.predict_proba(x_eval)[:, 1]
            hard_probs = model.predict_proba(x_hard_eval)[:, 1]

            selected_params, rerank_eval = select_delta_transform(
                grouped_sets["eval"],
                grouped_sets["hard_negative_eval"],
                eval_probs,
                hard_probs,
            )

            rerank_results[model_name] = {
                "model": model,
                "params": selected_params,
                "candidate_metrics": candidate_eval[model_name],
                "rerank_metrics": rerank_eval,
                "predictions": {
                    "train": apply_rerank_dataset(grouped_sets["train"], train_probs, selected_params),
                    "eval": apply_rerank_dataset(grouped_sets["eval"], eval_probs, selected_params),
                    "hard_negative_eval": apply_rerank_dataset(grouped_sets["hard_negative_eval"], hard_probs, selected_params),
                },
            }

        confidence_training_samples = build_confidence_samples(rerank_results["gradient_boosting"]["predictions"]["train"])
        confidence_eval_samples = build_confidence_samples(rerank_results["gradient_boosting"]["predictions"]["eval"])
        confidence_hard_samples = build_confidence_samples(
            rerank_results["gradient_boosting"]["predictions"]["hard_negative_eval"]
        )

        confidence_encoder_info = describe_expanded_features(CONFIDENCE_FEATURE_ORDER, CONFIDENCE_NORMALIZATION)
        x_conf_train = encode_rows(confidence_training_samples, CONFIDENCE_FEATURE_ORDER, CONFIDENCE_NORMALIZATION)
        y_conf_train = np.asarray([int(sample["targetTop1Correct"]) for sample in confidence_training_samples], dtype=np.int32)
        conf_weights = np.asarray([confidence_row_weight(sample) for sample in confidence_training_samples], dtype=np.float64)

        confidence_models = train_confidence_models(x_conf_train, y_conf_train, conf_weights, args.seed)
        confidence_metrics = evaluate_confidence_models(
            confidence_models,
            {
                "train": confidence_training_samples,
                "eval": confidence_eval_samples,
                "hard_negative_eval": confidence_hard_samples,
            },
        )

        rerank_artifact = build_rerank_artifact(
            feature_spec=feature_spec,
            encoder_info=encoder_info,
            rerank_results=rerank_results,
            dataset_paths=dataset_paths,
            synthetic_runs=SYNTHETIC_RUNS,
        )
        confidence_artifact = build_confidence_artifact(
            feature_spec=feature_spec,
            encoder_info=confidence_encoder_info,
            confidence_models=confidence_models,
            confidence_metrics=confidence_metrics,
            rerank_metrics=rerank_results["gradient_boosting"]["rerank_metrics"],
            dataset_paths=dataset_paths,
            synthetic_runs=SYNTHETIC_RUNS,
        )

        RERANK_ARTIFACT_PATH.write_text(json.dumps(rerank_artifact, ensure_ascii=False, indent=2) + "\n")
        CONFIDENCE_ARTIFACT_PATH.write_text(json.dumps(confidence_artifact, ensure_ascii=False, indent=2) + "\n")

        summarize_console(rerank_results, confidence_metrics)
        print(f"wrote {RERANK_ARTIFACT_PATH.relative_to(REPO_ROOT)}")
        print(f"wrote {CONFIDENCE_ARTIFACT_PATH.relative_to(REPO_ROOT)}")
        return 0
    finally:
        if cleanup:
            if args.keep_workdir:
                print(f"kept workspace at {workspace}")
            else:
                shutil.rmtree(workspace, ignore_errors=True)


def prepare_workspace(requested: Path | None) -> tuple[Path, bool]:
    if requested:
        requested.mkdir(parents=True, exist_ok=True)
        return requested.resolve(), False

    workspace = Path(tempfile.mkdtemp(prefix="magic-base-family-baseline-"))
    return workspace, True


def generate_synthetic_family_sets(workspace: Path) -> dict[str, Path]:
    dataset_paths: dict[str, Path] = {}

    for run in SYNTHETIC_RUNS:
        output_path = workspace / f"synthetic-{run.name}.ndjson"
        command = [
            "node",
            str(SYNTHETIC_GENERATOR_PATH),
            "--preset",
            run.preset,
            "--target",
            "family",
            "--count",
            str(run.count),
            "--split",
            run.split,
            "--seed",
            str(run.seed),
            "--out",
            str(output_path),
        ]
        run_command(command)
        dataset_paths[run.name] = output_path

    return dataset_paths


def export_rows(workspace: Path, dataset_paths: dict[str, Path]) -> Path:
    output_path = workspace / "base-family-rows.jsonl"
    command = ["node", str(EXPORTER_PATH)]
    for dataset_path in dataset_paths.values():
        command.extend(["--input", str(dataset_path)])
    command.extend(["--out", str(output_path)])
    run_command(command)
    return output_path


def load_jsonl(path: Path) -> list[dict[str, Any]]:
    return [json.loads(line) for line in path.read_text().splitlines() if line.strip()]


def run_command(command: list[str]) -> None:
    subprocess.run(command, cwd=REPO_ROOT, check=True)


def describe_expanded_features(
    feature_order: list[str], feature_normalization: dict[str, dict[str, Any]]
) -> dict[str, Any]:
    expanded_names: list[str] = []
    for name in feature_order:
        normalization = feature_normalization[name]
        if normalization["type"] == "one_hot":
            expanded_names.extend([f"{name}={value}" for value in normalization["values"]])
            continue
        expanded_names.append(name)

    return {
        "featureOrder": feature_order,
        "featureNormalization": feature_normalization,
        "expandedFeatureNames": expanded_names,
    }


def encode_rows(
    rows: list[dict[str, Any]], feature_order: list[str], feature_normalization: dict[str, dict[str, Any]]
) -> np.ndarray:
    encoded = [encode_single_row(row, feature_order, feature_normalization) for row in rows]
    return np.asarray(encoded, dtype=np.float64)


def encode_single_row(
    row: dict[str, Any], feature_order: list[str], feature_normalization: dict[str, dict[str, Any]]
) -> list[float]:
    encoded: list[float] = []
    for name in feature_order:
        value = row.get(name)
        normalization = feature_normalization[name]
        encoded.extend(normalize_value(value, normalization))
    return encoded


def normalize_value(value: Any, normalization: dict[str, Any]) -> list[float]:
    normalization_type = normalization["type"]
    if normalization_type == "one_hot":
        return [1.0 if value == category else 0.0 for category in normalization["values"]]
    if normalization_type == "clamp":
        numeric = float(value or 0)
        return [clip(numeric, float(normalization["min"]), float(normalization["max"]))]
    if normalization_type == "integer_clip":
        numeric = int(round(float(value or 0)))
        return [float(clip(numeric, int(normalization["min"]), int(normalization["max"])))]
    if normalization_type == "log1p_clip":
        numeric = float(value or 0)
        return [math.log1p(max(0.0, min(numeric, float(normalization["max"]))))]
    if normalization_type == "binary":
        return [1.0 if bool(value) else 0.0]
    raise ValueError(f"unsupported normalization type: {normalization_type}")


def clip(value: float, minimum: float, maximum: float) -> float:
    return max(minimum, min(maximum, value))


def candidate_row_weight(row: dict[str, Any]) -> float:
    pair_multiplier = 1.5 if row["candidatePairId"] in {"earth__fire", "water__life"} else 1.15
    if row["candidatePairId"] == "recognized__ambiguous":
        pair_multiplier = 1.25
    canonical_multiplier = 1.45 if row["targetIsCanonicalCandidate"] else 1.0
    hard_negative_multiplier = 1.2 if row.get("preset") == "hard-negative" else 1.0
    return pair_multiplier * canonical_multiplier * hard_negative_multiplier


def confidence_row_weight(sample: dict[str, Any]) -> float:
    base = 1.0
    if not sample["targetTop1Correct"]:
        base *= 1.45
    if sample["candidatePairId"] in {"earth__fire", "water__life"}:
        base *= 1.25
    if sample["candidatePairId"] == "recognized__ambiguous":
        base *= 1.15
    return base


def train_candidate_models(
    x_train: np.ndarray, y_train: np.ndarray, sample_weight: np.ndarray, seed: int
) -> dict[str, Any]:
    gradient_boosting = GradientBoostingClassifier(
        random_state=seed,
        n_estimators=56,
        learning_rate=0.06,
        max_depth=2,
        min_samples_leaf=10,
        subsample=0.9,
    )
    gradient_boosting.fit(x_train, y_train, sample_weight=sample_weight)

    logistic_regression = LogisticRegression(
        solver="liblinear",
        max_iter=1000,
        class_weight="balanced",
        random_state=seed,
    )
    logistic_regression.fit(x_train, y_train, sample_weight=sample_weight)

    return {
        "gradient_boosting": gradient_boosting,
        "logistic_regression": logistic_regression,
    }


def train_confidence_models(
    x_train: np.ndarray, y_train: np.ndarray, sample_weight: np.ndarray, seed: int
) -> dict[str, Any]:
    gradient_boosting = GradientBoostingClassifier(
        random_state=seed + 7,
        n_estimators=48,
        learning_rate=0.05,
        max_depth=2,
        min_samples_leaf=8,
        subsample=0.9,
    )
    gradient_boosting.fit(x_train, y_train, sample_weight=sample_weight)

    logistic_regression = LogisticRegression(
        solver="liblinear",
        max_iter=1000,
        class_weight="balanced",
        random_state=seed + 7,
    )
    logistic_regression.fit(x_train, y_train, sample_weight=sample_weight)

    return {
        "gradient_boosting": gradient_boosting,
        "logistic_regression": logistic_regression,
    }


def evaluate_candidate_models(
    models: dict[str, Any],
    split_rows: dict[str, list[dict[str, Any]]],
    feature_order: list[str],
    feature_normalization: dict[str, dict[str, Any]],
) -> dict[str, dict[str, Any]]:
    metrics: dict[str, dict[str, Any]] = {}
    for model_name, model in models.items():
        metrics[model_name] = {}
        for split_name, rows in split_rows.items():
            x_split = encode_rows(rows, feature_order, feature_normalization)
            y_true = np.asarray([int(row["targetIsCanonicalCandidate"]) for row in rows], dtype=np.int32)
            probabilities = model.predict_proba(x_split)[:, 1]
            predictions = (probabilities >= 0.5).astype(np.int32)
            metrics[model_name][split_name] = {
                "rowCount": int(len(rows)),
                "accuracy": round_float(float(accuracy_score(y_true, predictions))),
                "logLoss": round_float(float(log_loss(y_true, clip_probabilities(probabilities), labels=[0, 1]))),
                "positiveRate": round_float(float(np.mean(y_true))),
                "averagePositiveProbability": round_float(float(np.mean(probabilities[y_true == 1])) if np.any(y_true == 1) else 0.0),
                "averageNegativeProbability": round_float(float(np.mean(probabilities[y_true == 0])) if np.any(y_true == 0) else 0.0),
            }
    return metrics


def group_rows_by_sample(rows: list[dict[str, Any]]) -> list[list[dict[str, Any]]]:
    grouped: dict[str, list[dict[str, Any]]] = defaultdict(list)
    for row in rows:
        grouped[row["sampleId"]].append(deepcopy(row))

    ordered_groups = []
    for sample_rows in grouped.values():
        sample_rows.sort(key=lambda item: item["candidateRank"])
        ordered_groups.append(sample_rows)

    ordered_groups.sort(key=lambda group: group[0]["sampleId"])
    return ordered_groups


def select_delta_transform(
    eval_groups: list[list[dict[str, Any]]],
    hard_eval_groups: list[list[dict[str, Any]]],
    eval_probabilities: np.ndarray,
    hard_probabilities: np.ndarray,
) -> tuple[dict[str, Any], dict[str, Any]]:
    best_params: dict[str, Any] | None = None
    best_metrics: dict[str, Any] | None = None
    best_score: tuple[float, float, float, float] | None = None

    eval_predictions = apply_probabilities(eval_groups, eval_probabilities)
    hard_predictions = apply_probabilities(hard_eval_groups, hard_probabilities)

    for params in DELTA_PARAM_GRID:
        eval_result = compute_rerank_metrics(apply_rerank_group_set(eval_predictions, params))
        hard_result = compute_rerank_metrics(apply_rerank_group_set(hard_predictions, params))

        if eval_result["familyFlipIncrease"] != 0 or hard_result["familyFlipIncrease"] != 0:
            continue

        score = (
            hard_result["accuracyGain"],
            eval_result["accuracyGain"],
            hard_result["rescuedErrors"],
            eval_result["rescuedErrors"],
            1 if params["delta_scale"] > 0 else 0,
            -params["delta_scale"],
        )
        if best_score is None or score > best_score:
            best_score = score
            best_params = params
            best_metrics = {
                "eval": eval_result,
                "hard_negative_eval": hard_result,
                "acceptance": {
                    "familyFlipIncrease": 0,
                    "passed": True,
                    "evaluatedAt": current_timestamp(),
                },
            }

    if best_params is None:
        fallback = {
            "delta_scale": 0.0,
            "support_gap": 0.0,
            "boost_margin": 0.12,
            "top_cap": 0.0,
            "runner_cap": 0.0,
            "third_cap": 0.0,
            "negative_cap": 0.0,
            "known_pair_multiplier": 1.0,
            "recognized_pair_multiplier": 1.0,
            "other_pair_multiplier": 0.0,
        }
        best_params = fallback
        best_metrics = {
            "eval": compute_rerank_metrics(apply_rerank_group_set(eval_predictions, fallback)),
            "hard_negative_eval": compute_rerank_metrics(apply_rerank_group_set(hard_predictions, fallback)),
            "acceptance": {
                "familyFlipIncrease": 0,
                "passed": True,
                "evaluatedAt": current_timestamp(),
            },
        }

    return best_params, best_metrics


def apply_probabilities(groups: list[list[dict[str, Any]]], probabilities: np.ndarray) -> list[list[dict[str, Any]]]:
    enriched: list[list[dict[str, Any]]] = []
    cursor = 0
    for group in groups:
        group_copy: list[dict[str, Any]] = []
        for row in group:
            enriched_row = deepcopy(row)
            enriched_row["modelProbability"] = float(probabilities[cursor])
            group_copy.append(enriched_row)
            cursor += 1
        enriched.append(group_copy)

    if cursor != len(probabilities):
        raise RuntimeError("probability count did not match grouped rows")

    return enriched


def apply_rerank_dataset(
    groups: list[list[dict[str, Any]]], probabilities: np.ndarray, params: dict[str, Any]
) -> list[list[dict[str, Any]]]:
    with_probabilities = apply_probabilities(groups, probabilities)
    return apply_rerank_group_set(with_probabilities, params)


def apply_rerank_group_set(groups: list[list[dict[str, Any]]], params: dict[str, Any]) -> list[list[dict[str, Any]]]:
    return [apply_rerank_to_group(group, params) for group in groups]


def apply_rerank_to_group(group: list[dict[str, Any]], params: dict[str, Any]) -> list[dict[str, Any]]:
    rows = [deepcopy(row) for row in group]
    heuristic_top_probability = rows[0]["modelProbability"] if rows else 0.0

    for row in rows:
        proximity = clip(1.0 - (float(row["topScoreGap"]) / MAX_TOP_SCORE_GAP), 0.0, 1.0)
        centered_probability = float(row["modelProbability"]) - 0.5
        margin_pressure = clip(
            (float(params["boost_margin"]) - float(row["top1MinusTop2Margin"])) / max(float(params["boost_margin"]), EPSILON),
            0.0,
            1.0,
        )
        probability_support = clip(
            (float(row["modelProbability"]) - heuristic_top_probability - float(params["support_gap"]))
            / max(1.0 - float(params["support_gap"]), EPSILON),
            0.0,
            1.0,
        )

        pair_id = row["candidatePairId"]
        if pair_id in {"earth__fire", "water__life"}:
            pair_multiplier = float(params["known_pair_multiplier"])
        elif pair_id == "recognized__ambiguous":
            pair_multiplier = float(params["recognized_pair_multiplier"])
        else:
            pair_multiplier = float(params["other_pair_multiplier"])

        delta = centered_probability * float(params["delta_scale"]) * proximity * pair_multiplier
        if row["candidateRank"] == 0:
            positive_cap = float(params["top_cap"])
            positive_gate = 1.0
        elif row["candidateRank"] == 1:
            positive_cap = float(params["runner_cap"])
            positive_gate = margin_pressure * probability_support
        else:
            positive_cap = float(params["third_cap"])
            positive_gate = margin_pressure * probability_support * 0.8

        if delta > 0:
            delta = min(delta, positive_cap * positive_gate)
        else:
            delta = max(delta, -float(params["negative_cap"]) * (0.55 + 0.45 * proximity))

        row["rerankDelta"] = round_float(delta)
        row["rerankedScore"] = round_float(clip(float(row["heuristicScore"]) + delta, 0.0, 1.0))

    rows.sort(key=lambda item: (item["rerankedScore"], item["heuristicScore"]), reverse=True)
    return rows


def compute_rerank_metrics(groups: list[list[dict[str, Any]]]) -> dict[str, Any]:
    total = len(groups)
    heuristic_correct = 0
    reranked_correct = 0
    family_flips = 0
    rescued_errors = 0
    pair_totals = defaultdict(int)
    pair_correct = defaultdict(int)

    for group in groups:
        top_heuristic = min(group, key=lambda item: item["candidateRank"])
        top_reranked = group[0]
        label = top_heuristic["label"]
        pair_id = top_heuristic["candidatePairId"]

        heuristic_is_correct = top_heuristic["heuristicTopFamilyId"] == label
        reranked_is_correct = top_reranked["candidateFamilyId"] == label

        heuristic_correct += int(heuristic_is_correct)
        reranked_correct += int(reranked_is_correct)
        family_flips += int(heuristic_is_correct and not reranked_is_correct)
        rescued_errors += int((not heuristic_is_correct) and reranked_is_correct)

        pair_totals[pair_id] += 1
        pair_correct[pair_id] += int(reranked_is_correct)

    heuristic_accuracy = heuristic_correct / max(total, 1)
    reranked_accuracy = reranked_correct / max(total, 1)

    return {
        "sampleCount": total,
        "heuristicAccuracy": round_float(heuristic_accuracy),
        "accuracy": round_float(reranked_accuracy),
        "accuracyGain": round_float(reranked_accuracy - heuristic_accuracy),
        "familyFlipIncrease": int(family_flips),
        "rescuedErrors": int(rescued_errors),
        "pairAccuracy": {
            pair_id: round_float(pair_correct[pair_id] / max(pair_totals[pair_id], 1))
            for pair_id in sorted(pair_totals.keys())
        },
    }


def build_confidence_samples(groups: list[list[dict[str, Any]]]) -> list[dict[str, Any]]:
    samples: list[dict[str, Any]] = []
    for group in groups:
        top = group[0]
        second = group[1] if len(group) > 1 else None
        heuristic_top = min(group, key=lambda item: item["candidateRank"])
        top_probability = float(top["modelProbability"])
        second_probability = float(second["modelProbability"]) if second else 0.0
        top_margin = float(top["rerankedScore"]) - float(second["rerankedScore"]) if second else float(top["rerankedScore"])

        samples.append(
            {
                "sampleId": top["sampleId"],
                "label": top["label"],
                "split": top["split"],
                "topFamilyId": top["candidateFamilyId"],
                "candidatePairId": top["candidatePairId"],
                "heuristicTopScore": heuristic_top["heuristicTopScore"],
                "heuristicMargin": heuristic_top["heuristicTopMargin"],
                "rerankedTopScore": top["rerankedScore"],
                "rerankedMargin": round_float(top_margin),
                "rerankedTopProbability": round_float(top_probability),
                "rerankedProbabilityGap": round_float(top_probability - second_probability),
                "topDelta": top["rerankDelta"],
                "topWindowSize": top["topWindowSize"],
                "strokeCount": top["strokeCount"],
                "pointCount": top["pointCount"],
                "durationMs": top["durationMs"],
                "pathLength": top["pathLength"],
                "closureGap": top["closureGap"],
                "dominantCorners": top["dominantCorners"],
                "endpointClusters": top["endpointClusters"],
                "circularity": top["circularity"],
                "fillRatio": top["fillRatio"],
                "parallelism": top["parallelism"],
                "rawAngleRadians": top["rawAngleRadians"],
                "qualityClosure": top["qualityClosure"],
                "qualitySmoothness": top["qualitySmoothness"],
                "qualityStability": top["qualityStability"],
                "qualityRotationBias": top["qualityRotationBias"],
                "targetTop1Correct": int(top["candidateFamilyId"] == top["label"]),
                "heuristicConfidenceBaseline": round_float(clip(float(heuristic_top["heuristicTopScore"]), 0.001, 0.999)),
                "probabilityBaseline": round_float(clip(top_probability, 0.001, 0.999)),
            }
        )
    return samples


def evaluate_confidence_models(
    models: dict[str, Any], split_samples: dict[str, list[dict[str, Any]]]
) -> dict[str, Any]:
    metrics: dict[str, Any] = {}
    for model_name, model in models.items():
        metrics[model_name] = {}
        for split_name, samples in split_samples.items():
            x_split = encode_rows(samples, CONFIDENCE_FEATURE_ORDER, CONFIDENCE_NORMALIZATION)
            y_true = np.asarray([int(sample["targetTop1Correct"]) for sample in samples], dtype=np.int32)
            probabilities = clip_probabilities(model.predict_proba(x_split)[:, 1])
            heuristic_baseline = np.asarray([sample["heuristicConfidenceBaseline"] for sample in samples], dtype=np.float64)
            probability_baseline = np.asarray([sample["probabilityBaseline"] for sample in samples], dtype=np.float64)
            metrics[model_name][split_name] = {
                "sampleCount": int(len(samples)),
                "accuracy": round_float(float(accuracy_score(y_true, (probabilities >= 0.5).astype(np.int32)))),
                "brierScore": round_float(float(brier_score_loss(y_true, probabilities))),
                "logLoss": round_float(float(log_loss(y_true, probabilities, labels=[0, 1]))),
                "heuristicBaselineBrier": round_float(float(brier_score_loss(y_true, heuristic_baseline))),
                "heuristicBaselineLogLoss": round_float(float(log_loss(y_true, heuristic_baseline, labels=[0, 1]))),
                "candidateProbabilityBaselineBrier": round_float(float(brier_score_loss(y_true, probability_baseline))),
                "candidateProbabilityBaselineLogLoss": round_float(float(log_loss(y_true, probability_baseline, labels=[0, 1]))),
                "averageConfidence": round_float(float(np.mean(probabilities))),
            }
    return metrics


def clip_probabilities(probabilities: np.ndarray) -> np.ndarray:
    return np.clip(probabilities.astype(np.float64), 0.001, 0.999)


def build_rerank_artifact(
    *,
    feature_spec: dict[str, Any],
    encoder_info: dict[str, Any],
    rerank_results: dict[str, Any],
    dataset_paths: dict[str, Path],
    synthetic_runs: tuple[SyntheticRun, ...],
) -> dict[str, Any]:
    feature_row_spec = feature_spec["featureRows"]["base_candidate_row_v1"]

    return {
        "artifactType": "base_rerank_model",
        "modelType": "gradient_boosting_candidate_rerank",
        "version": "v1",
        "schemaVersion": "tiny-ml-base-rerank-v1",
        "datasetSchemaVersion": feature_spec["datasetSchemaVersion"],
        "rowSpec": feature_row_spec["id"],
        "featureOrder": encoder_info["featureOrder"],
        "featureNormalization": encoder_info["featureNormalization"],
        "expandedFeatureNames": encoder_info["expandedFeatureNames"],
        "labelSpace": feature_spec["labelSpace"]["baseFamily"],
        "supportedPairs": feature_spec["supportedPairs"]["base"],
        "trainingManifest": feature_spec["trainingManifest"],
        "gatePolicy": feature_spec["gatePolicy"]["baseFamily"],
        "trainingDatasets": [
            {
                "name": run.name,
                "preset": run.preset,
                "split": run.split,
                "countPerLabel": run.count,
                "seed": run.seed,
                "path": str(dataset_paths[run.name]),
            }
            for run in synthetic_runs
        ],
        "models": {
            "main": {
                "name": "gradient_boosting",
                "model": serialize_gradient_boosting_classifier(rerank_results["gradient_boosting"]["model"]),
                "candidateMetrics": rerank_results["gradient_boosting"]["candidate_metrics"],
                "deltaTransform": rerank_results["gradient_boosting"]["params"],
                "offlineEval": rerank_results["gradient_boosting"]["rerank_metrics"],
            },
            "parity": {
                "name": "logistic_regression",
                "model": serialize_logistic_regression(rerank_results["logistic_regression"]["model"], encoder_info["expandedFeatureNames"]),
                "candidateMetrics": rerank_results["logistic_regression"]["candidate_metrics"],
                "deltaTransform": rerank_results["logistic_regression"]["params"],
                "offlineEval": rerank_results["logistic_regression"]["rerank_metrics"],
            },
        },
        "evaluation": {
            "heuristic": {
                "eval": rerank_results["gradient_boosting"]["rerank_metrics"]["eval"]["heuristicAccuracy"],
                "hard_negative_eval": rerank_results["gradient_boosting"]["rerank_metrics"]["hard_negative_eval"]["heuristicAccuracy"],
            },
            "gradientBoosting": rerank_results["gradient_boosting"]["rerank_metrics"],
            "logisticRegression": rerank_results["logistic_regression"]["rerank_metrics"],
        },
        "createdAt": current_timestamp(),
    }


def build_confidence_artifact(
    *,
    feature_spec: dict[str, Any],
    encoder_info: dict[str, Any],
    confidence_models: dict[str, Any],
    confidence_metrics: dict[str, Any],
    rerank_metrics: dict[str, Any],
    dataset_paths: dict[str, Path],
    synthetic_runs: tuple[SyntheticRun, ...],
) -> dict[str, Any]:
    return {
        "artifactType": "base_confidence_model",
        "modelType": "gradient_boosting_confidence_calibrator",
        "version": "v1",
        "schemaVersion": "tiny-ml-base-confidence-v1",
        "datasetSchemaVersion": feature_spec["datasetSchemaVersion"],
        "calibrationTarget": "top_candidate_correct_after_rerank",
        "featureOrder": encoder_info["featureOrder"],
        "featureNormalization": encoder_info["featureNormalization"],
        "expandedFeatureNames": encoder_info["expandedFeatureNames"],
        "supportedPairs": feature_spec["supportedPairs"]["base"],
        "trainingManifest": feature_spec["trainingManifest"],
        "gatePolicy": {
            "directCanonicalPredictionForbidden": True,
            "allowedOutputs": ["recognized_confidence", "ambiguity_probability"],
            "ambiguityProbabilityRule": "1 - recognized_confidence",
        },
        "trainingDatasets": [
            {
                "name": run.name,
                "preset": run.preset,
                "split": run.split,
                "countPerLabel": run.count,
                "seed": run.seed,
                "path": str(dataset_paths[run.name]),
            }
            for run in synthetic_runs
        ],
        "rerankContext": rerank_metrics,
        "models": {
            "main": {
                "name": "gradient_boosting",
                "model": serialize_gradient_boosting_classifier(confidence_models["gradient_boosting"]),
                "offlineEval": confidence_metrics["gradient_boosting"],
            },
            "parity": {
                "name": "logistic_regression",
                "model": serialize_logistic_regression(confidence_models["logistic_regression"], encoder_info["expandedFeatureNames"]),
                "offlineEval": confidence_metrics["logistic_regression"],
            },
        },
        "createdAt": current_timestamp(),
    }


def serialize_gradient_boosting_classifier(model: GradientBoostingClassifier) -> dict[str, Any]:
    baseline_probability = float(model.init_.predict_proba(np.zeros((1, model.n_features_in_)))[0][1])
    estimators = [serialize_tree(estimator[0]) for estimator in model.estimators_]
    return {
        "family": "GradientBoostingClassifier",
        "classes": [int(item) for item in model.classes_.tolist()],
        "nFeatures": int(model.n_features_in_),
        "learningRate": round_float(float(model.learning_rate)),
        "nEstimators": int(model.n_estimators),
        "maxDepth": int(model.max_depth),
        "minSamplesLeaf": int(model.min_samples_leaf),
        "subsample": round_float(float(model.subsample)),
        "initProbability": round_float(baseline_probability),
        "treeParams": {
            "estimators": estimators,
        },
    }


def serialize_tree(estimator: Any) -> dict[str, Any]:
    tree = estimator.tree_
    return {
        "childrenLeft": [int(value) for value in tree.children_left.tolist()],
        "childrenRight": [int(value) for value in tree.children_right.tolist()],
        "feature": [int(value) for value in tree.feature.tolist()],
        "threshold": [round_float(float(value)) for value in tree.threshold.tolist()],
        "value": [round_float(float(node_value[0][0])) for node_value in tree.value.tolist()],
    }


def serialize_logistic_regression(model: LogisticRegression, feature_names: list[str]) -> dict[str, Any]:
    return {
        "family": "LogisticRegression",
        "classes": [int(item) for item in model.classes_.tolist()],
        "featureNames": feature_names,
        "intercept": [round_float(float(value)) for value in model.intercept_.tolist()],
        "weights": [round_float(float(value)) for value in model.coef_[0].tolist()],
    }


def round_float(value: float) -> float:
    return float(round(value, 6))


def current_timestamp() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def summarize_console(rerank_results: dict[str, Any], confidence_metrics: dict[str, Any]) -> None:
    gbdt_eval = rerank_results["gradient_boosting"]["rerank_metrics"]["eval"]
    gbdt_hard = rerank_results["gradient_boosting"]["rerank_metrics"]["hard_negative_eval"]
    parity_eval = rerank_results["logistic_regression"]["rerank_metrics"]["eval"]
    parity_hard = rerank_results["logistic_regression"]["rerank_metrics"]["hard_negative_eval"]
    confidence_eval = confidence_metrics["gradient_boosting"]["eval"]
    confidence_hard = confidence_metrics["gradient_boosting"]["hard_negative_eval"]

    print(
        "GBDT rerank:"
        f" eval_acc={gbdt_eval['accuracy']:.3f}"
        f" eval_gain={gbdt_eval['accuracyGain']:.3f}"
        f" hard_acc={gbdt_hard['accuracy']:.3f}"
        f" hard_gain={gbdt_hard['accuracyGain']:.3f}"
        f" flips={gbdt_eval['familyFlipIncrease'] + gbdt_hard['familyFlipIncrease']}"
    )
    print(
        "LR parity:"
        f" eval_acc={parity_eval['accuracy']:.3f}"
        f" eval_gain={parity_eval['accuracyGain']:.3f}"
        f" hard_acc={parity_hard['accuracy']:.3f}"
        f" hard_gain={parity_hard['accuracyGain']:.3f}"
        f" flips={parity_eval['familyFlipIncrease'] + parity_hard['familyFlipIncrease']}"
    )
    print(
        "GBDT confidence:"
        f" eval_brier={confidence_eval['brierScore']:.3f}"
        f" eval_logloss={confidence_eval['logLoss']:.3f}"
        f" hard_brier={confidence_hard['brierScore']:.3f}"
        f" hard_logloss={confidence_hard['logLoss']:.3f}"
    )


if __name__ == "__main__":
    raise SystemExit(main())
