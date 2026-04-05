#!/usr/bin/env python3

from __future__ import annotations

import argparse
import json
import math
from collections import defaultdict
from pathlib import Path
from typing import Any

import numpy as np
from sklearn.ensemble import GradientBoostingRegressor
from sklearn.linear_model import LogisticRegression
from sklearn.metrics import (
    accuracy_score,
    average_precision_score,
    brier_score_loss,
    mean_absolute_error,
    mean_squared_error,
    roc_auc_score,
)


REPO_ROOT = Path(__file__).resolve().parents[2]
PAIRWISE_MAX_SHIFT = 0.08
SUPPRESSION_SCALE = 0.06
WEAK_GATE_THRESHOLD = 0.34


def main() -> None:
    args = parse_args()
    rows = [json.loads(line) for line in Path(args.rows).read_text("utf8").splitlines() if line.strip()]
    summary = json.loads(Path(args.summary).read_text("utf8"))
    feature_spec = json.loads((REPO_ROOT / "artifacts/ml/feature-spec-v1.json").read_text("utf8"))

    feature_order = [
        "operatorId",
        "hardPairId",
        "candidateRank",
        "heuristicScore",
        "baseScore",
        "templateDistance",
        "shapeConfidence",
        "topScoreGap",
        "blockedByFlag",
        "blockedByOperator",
        "anchorZoneId",
        "placement",
        "anchorScore",
        "scaleScore",
        "gateStrength",
        "angleRadians",
        "scaleRatio",
        "straightness",
        "corners",
        "closure",
        "stackIndex",
        "existingOperatorsCount",
        "existingOperatorsMask",
        "hasVoidCutInStack",
    ]
    feature_norm = dict(feature_spec["featureNormalization"]["operator_candidate_row_v1"])
    feature_norm["placement"] = feature_norm["placementAnchorZoneId"]
    supported_pairs = list(feature_spec["supportedPairs"]["operator"])
    gate_policy = dict(feature_spec["gatePolicy"]["operator"])
    label_space = dict(feature_spec["labelSpace"])
    training_manifest = dict(feature_spec["trainingManifest"])

    vectorizer = FeatureVectorizer(feature_order, feature_norm)

    train_rows = [row for row in rows if row["split"] == "train"]
    eval_rows = [row for row in rows if row["split"] == "eval"]
    hard_eval_rows = [row for row in rows if row["split"] == "hard_negative_eval"]
    top_train_rows = [row for row in train_rows if row["topCandidateFlag"]]
    top_eval_rows = [row for row in eval_rows if row["topCandidateFlag"]]
    top_hard_rows = [row for row in hard_eval_rows if row["topCandidateFlag"]]

    X_pair_train = vectorizer.transform(train_rows)
    y_pair_train = np.array([float(row["targetPairwiseDelta"]) for row in train_rows], dtype=float)
    w_pair_train = np.array([float(row["sampleWeight"]) for row in train_rows], dtype=float)

    pairwise_model = GradientBoostingRegressor(
        n_estimators=72,
        learning_rate=0.06,
        max_depth=3,
        min_samples_leaf=8,
        random_state=17,
    )
    pairwise_model.fit(X_pair_train, y_pair_train, sample_weight=w_pair_train)

    X_supp_train = vectorizer.transform(train_rows)
    y_supp_train = np.array(
        [1 if float(row["targetFalsePositiveSuppression"]) >= 0.5 else 0 for row in train_rows],
        dtype=int,
    )
    w_supp_train = np.array(
        [float(row["sampleWeight"]) * (1.5 if y else 1.0) for row, y in zip(train_rows, y_supp_train)],
        dtype=float,
    )
    suppression_model = LogisticRegression(
        solver="liblinear",
        max_iter=400,
        class_weight="balanced",
        random_state=17,
    )
    suppression_model.fit(X_supp_train, y_supp_train, sample_weight=w_supp_train)

    X_conf_train = vectorizer.transform(top_train_rows)
    y_conf_train = np.array(
        [int(row["targetOperatorConfidence"] or 0) for row in top_train_rows],
        dtype=int,
    )
    w_conf_train = np.array(
        [float(row["sampleWeight"]) * (1.6 if y else 1.0) for row, y in zip(top_train_rows, y_conf_train)],
        dtype=float,
    )
    confidence_model = LogisticRegression(
        solver="liblinear",
        max_iter=400,
        class_weight="balanced",
        random_state=17,
    )
    confidence_model.fit(X_conf_train, y_conf_train, sample_weight=w_conf_train)

    pair_metrics = evaluate_pairwise(pairwise_model, suppression_model, vectorizer, eval_rows, hard_eval_rows)
    suppression_metrics = evaluate_binary_model(
        suppression_model,
        vectorizer,
        eval_rows,
        hard_eval_rows,
        "targetFalsePositiveSuppression",
    )
    confidence_metrics = evaluate_binary_model(
        confidence_model,
        vectorizer,
        top_eval_rows,
        top_hard_rows,
        "targetOperatorConfidence",
    )

    rerank_artifact = {
        "artifactType": "operator_rerank_baseline",
        "modelType": "pairwise_gbdt_with_suppression",
        "version": "v1",
        "schemaVersion": "tiny-ml-operator-rerank-v1",
        "featureOrder": feature_order,
        "featureNormalization": feature_norm,
        "featureSources": {
            "placement": "placementAnchorZoneId",
        },
        "labelSpace": label_space,
        "supportedPairs": supported_pairs,
        "trainingManifest": training_manifest,
        "gatePolicy": gate_policy,
        "treeParams": {
            "pairwiseDeltaModel": export_gradient_boosting_model(pairwise_model, vectorizer.expanded_feature_names),
        },
        "weights": {
            "falsePositiveSuppression": export_logistic_model(suppression_model, vectorizer.expanded_feature_names),
        },
        "metrics": {
            "pairwise": pair_metrics,
            "suppression": suppression_metrics,
        },
        "datasetSummary": summary,
        "trainingSummary": {
            "trainRows": len(train_rows),
            "evalRows": len(eval_rows),
            "hardNegativeEvalRows": len(hard_eval_rows),
        },
        "notes": [
            "canonical operator semantics are still rule-owned",
            "blockedBy hard stop stays outside the ML model",
            "weak gate rows train toward zero delta and stronger suppression",
        ],
    }

    confidence_artifact = {
        "artifactType": "operator_confidence_baseline",
        "modelType": "operator_confidence_logistic",
        "version": "v1",
        "schemaVersion": "tiny-ml-operator-confidence-v1",
        "featureOrder": feature_order,
        "featureNormalization": feature_norm,
        "featureSources": {
            "placement": "placementAnchorZoneId",
        },
        "labelSpace": label_space,
        "supportedPairs": supported_pairs,
        "trainingManifest": training_manifest,
        "gatePolicy": gate_policy,
        "weights": export_logistic_model(confidence_model, vectorizer.expanded_feature_names),
        "metrics": confidence_metrics,
        "datasetSummary": summary,
        "trainingSummary": {
            "topTrainRows": len(top_train_rows),
            "topEvalRows": len(top_eval_rows),
            "topHardNegativeRows": len(top_hard_rows),
        },
        "notes": [
            "confidence is trained on top-candidate rows only",
            "confidence targets drop to zero for blockedBy, off-anchor, and wrong-scale cases",
        ],
    }

    write_json(Path(args.rerank_artifact), rerank_artifact)
    write_json(Path(args.confidence_artifact), confidence_artifact)

    print("pairwise metrics:")
    print(json.dumps(pair_metrics, indent=2))
    print("suppression metrics:")
    print(json.dumps(suppression_metrics, indent=2))
    print("confidence metrics:")
    print(json.dumps(confidence_metrics, indent=2))


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--rows", required=True)
    parser.add_argument("--summary", required=True)
    parser.add_argument("--rerank-artifact", required=True)
    parser.add_argument("--confidence-artifact", required=True)
    return parser.parse_args()


class FeatureVectorizer:
    def __init__(self, feature_order: list[str], feature_norm: dict[str, Any]) -> None:
        self.feature_order = feature_order
        self.feature_norm = feature_norm
        self.expanded_feature_names: list[str] = []
        for feature in feature_order:
            norm = feature_norm[feature]
            kind = norm["type"]
            if kind in ("one_hot", "multi_hot"):
                for value in norm["values"]:
                    self.expanded_feature_names.append(f"{feature}={value}")
            else:
                self.expanded_feature_names.append(feature)

    def transform(self, rows: list[dict[str, Any]]) -> np.ndarray:
        vectors = [self.encode_row(row) for row in rows]
        return np.array(vectors, dtype=float)

    def encode_row(self, row: dict[str, Any]) -> list[float]:
        encoded: list[float] = []
        for feature in self.feature_order:
            norm = self.feature_norm[feature]
            kind = norm["type"]
            value = row.get(feature)
            if kind == "one_hot":
                encoded.extend(1.0 if str(value) == option else 0.0 for option in norm["values"])
            elif kind == "multi_hot":
                current = set(value or [])
                encoded.extend(1.0 if option in current else 0.0 for option in norm["values"])
            elif kind == "binary":
                encoded.append(float(1 if value else 0))
            elif kind == "integer_clip":
                minimum = float(norm["min"])
                maximum = float(norm["max"])
                encoded.append(scale_to_unit(float(value or 0), minimum, maximum))
            elif kind == "clamp":
                minimum = float(norm["min"])
                maximum = float(norm["max"])
                encoded.append(scale_to_unit(float(value or 0), minimum, maximum))
            else:
                raise ValueError(f"unsupported normalization type: {kind}")
        return encoded


def scale_to_unit(value: float, minimum: float, maximum: float) -> float:
    clipped = max(minimum, min(maximum, value))
    if math.isclose(maximum, minimum):
        return 0.0
    return (clipped - minimum) / (maximum - minimum)


def evaluate_pairwise(
    pairwise_model: GradientBoostingRegressor,
    suppression_model: LogisticRegression,
    vectorizer: FeatureVectorizer,
    eval_rows: list[dict[str, Any]],
    hard_eval_rows: list[dict[str, Any]],
) -> dict[str, Any]:
    eval_metrics = evaluate_pairwise_split(pairwise_model, suppression_model, vectorizer, eval_rows)
    hard_metrics = evaluate_pairwise_split(pairwise_model, suppression_model, vectorizer, hard_eval_rows)
    return {
        "eval": eval_metrics,
        "hard_negative_eval": hard_metrics,
    }


def evaluate_pairwise_split(
    pairwise_model: GradientBoostingRegressor,
    suppression_model: LogisticRegression,
    vectorizer: FeatureVectorizer,
    rows: list[dict[str, Any]],
) -> dict[str, Any]:
    if not rows:
        return {}

    X = vectorizer.transform(rows)
    predictions = pairwise_model.predict(X)
    suppression_probabilities = suppression_model.predict_proba(X)[:, 1]
    targets = np.array([float(row["targetPairwiseDelta"]) for row in rows], dtype=float)
    by_example: dict[str, list[dict[str, Any]]] = defaultdict(list)

    for row, pred, suppression in zip(rows, predictions, suppression_probabilities):
        enriched = dict(row)
        enriched["predPairwiseDelta"] = float(np.clip(pred, -PAIRWISE_MAX_SHIFT, PAIRWISE_MAX_SHIFT))
        enriched["predSuppression"] = float(suppression)
        by_example[enriched["exampleId"]].append(enriched)

    heuristic_labels: list[str] = []
    reranked_labels: list[str] = []
    canonical_labels: list[str] = []
    allowed_heuristic_labels: list[str] = []
    allowed_reranked_labels: list[str] = []
    allowed_canonical_labels: list[str] = []
    off_anchor_rescue = 0
    wrong_scale_rescue = 0
    blocked_violation = 0
    pair_only_before = 0
    pair_only_after = 0
    pair_only_total = 0

    for example_rows in by_example.values():
        example_rows.sort(key=lambda row: row["candidateRank"])
        canonical = example_rows[0]["canonicalOperator"]
        canonical_labels.append(canonical)
        heuristic_top = max(example_rows, key=lambda row: row["heuristicScore"])
        reranked_top = max(example_rows, key=adjusted_score)
        heuristic_labels.append(heuristic_top["operatorId"])
        reranked_labels.append(reranked_top["operatorId"])

        if bool(example_rows[0]["allowRecognition"]):
            allowed_canonical_labels.append(canonical)
            allowed_heuristic_labels.append(heuristic_top["operatorId"])
            allowed_reranked_labels.append(reranked_top["operatorId"])

        if example_rows[0]["scenarioKind"] == "hard_pair":
            pair_only_total += 1
            pair_only_before += 1 if heuristic_top["operatorId"] == canonical else 0
            pair_only_after += 1 if reranked_top["operatorId"] == canonical else 0

        if (
            example_rows[0]["scenarioKind"] == "off_anchor"
            and heuristic_top["operatorId"] != canonical
            and reranked_top["operatorId"] == canonical
        ):
            off_anchor_rescue += 1

        if (
            example_rows[0]["scenarioKind"] == "wrong_scale"
            and heuristic_top["operatorId"] != canonical
            and reranked_top["operatorId"] == canonical
        ):
            wrong_scale_rescue += 1

        if example_rows[0]["scenarioKind"] == "blocked" and reranked_top["operatorId"] == "martial_axis":
            blocked_violation += 1

    result = {
        "pairwiseDeltaMae": round_float(mean_absolute_error(targets, predictions)),
        "pairwiseDeltaRmse": round_float(math.sqrt(mean_squared_error(targets, predictions))),
        "top1AccuracyBefore": round_float(accuracy_score(canonical_labels, heuristic_labels)),
        "top1AccuracyAfter": round_float(accuracy_score(canonical_labels, reranked_labels)),
        "allowedTop1AccuracyBefore": round_float(accuracy_score(allowed_canonical_labels, allowed_heuristic_labels))
        if allowed_canonical_labels
        else None,
        "allowedTop1AccuracyAfter": round_float(accuracy_score(allowed_canonical_labels, allowed_reranked_labels))
        if allowed_canonical_labels
        else None,
        "hardPairAccuracyBefore": round_float(pair_only_before / pair_only_total) if pair_only_total else None,
        "hardPairAccuracyAfter": round_float(pair_only_after / pair_only_total) if pair_only_total else None,
        "blockedByViolationCount": blocked_violation,
        "offAnchorRescueCount": off_anchor_rescue,
        "wrongScaleRescueCount": wrong_scale_rescue,
    }
    return result


def adjusted_score(row: dict[str, Any]) -> float:
    base = float(row["heuristicScore"])
    gate = float(row["gateStrength"])
    delta = float(row.get("predPairwiseDelta", 0.0))
    suppression = float(row.get("predSuppression", 0.0))
    if int(row["blockedByFlag"]) == 1:
        return -1e9
    delta = float(np.clip(delta, -PAIRWISE_MAX_SHIFT, PAIRWISE_MAX_SHIFT))
    if gate < WEAK_GATE_THRESHOLD:
        return base - (0.18 + suppression * 0.18)
    delta *= gate
    return base + delta - suppression * (SUPPRESSION_SCALE + (1.0 - gate) * 0.04)


def evaluate_binary_model(
    model: LogisticRegression,
    vectorizer: FeatureVectorizer,
    eval_rows: list[dict[str, Any]],
    hard_eval_rows: list[dict[str, Any]],
    target_field: str,
) -> dict[str, Any]:
    return {
        "eval": evaluate_binary_split(model, vectorizer, eval_rows, target_field),
        "hard_negative_eval": evaluate_binary_split(model, vectorizer, hard_eval_rows, target_field),
    }


def evaluate_binary_split(
    model: LogisticRegression,
    vectorizer: FeatureVectorizer,
    rows: list[dict[str, Any]],
    target_field: str,
) -> dict[str, Any]:
    if not rows:
        return {}

    X = vectorizer.transform(rows)
    probabilities = model.predict_proba(X)[:, 1]
    targets = np.array([1 if float(row.get(target_field) or 0) >= 0.5 else 0 for row in rows], dtype=int)

    for row, probability in zip(rows, probabilities):
        row["predSuppression"] = float(probability) if target_field == "targetFalsePositiveSuppression" else row.get("predSuppression", 0.0)

    metrics = {
        "accuracy": round_float(accuracy_score(targets, probabilities >= 0.5)),
        "averagePrecision": round_float(average_precision_score(targets, probabilities)),
        "brier": round_float(brier_score_loss(targets, probabilities)),
    }

    if len(set(targets.tolist())) > 1:
        metrics["rocAuc"] = round_float(roc_auc_score(targets, probabilities))

    return metrics


def export_gradient_boosting_model(model: GradientBoostingRegressor, expanded_feature_names: list[str]) -> dict[str, Any]:
    estimators = []
    for estimator_array in model.estimators_:
        tree = estimator_array[0].tree_
        estimators.append(
            {
                "childrenLeft": tree.children_left.tolist(),
                "childrenRight": tree.children_right.tolist(),
                "featureIndex": tree.feature.tolist(),
                "featureName": [expanded_feature_names[index] if index >= 0 else None for index in tree.feature.tolist()],
                "threshold": [round_float(value) for value in tree.threshold.tolist()],
                "value": [round_float(node[0][0]) for node in tree.value.tolist()],
            }
        )

    init = model.init_
    if hasattr(init, "constant_"):
        initial_prediction = round_float(float(init.constant_[0][0]))
    else:
        initial_prediction = 0.0

    return {
        "library": "scikit-learn",
        "className": "GradientBoostingRegressor",
        "learningRate": round_float(float(model.learning_rate)),
        "nEstimators": int(model.n_estimators),
        "maxDepth": int(model.max_depth),
        "minSamplesLeaf": int(model.min_samples_leaf),
        "initialPrediction": initial_prediction,
        "expandedFeatureNames": expanded_feature_names,
        "trees": estimators,
    }


def export_logistic_model(model: LogisticRegression, expanded_feature_names: list[str]) -> dict[str, Any]:
    return {
        "library": "scikit-learn",
        "className": "LogisticRegression",
        "expandedFeatureNames": expanded_feature_names,
        "intercept": round_float(float(model.intercept_[0])),
        "coefficients": [round_float(float(value)) for value in model.coef_[0].tolist()],
        "classes": [int(value) for value in model.classes_.tolist()],
    }


def write_json(path_obj: Path, payload: dict[str, Any]) -> None:
    path_obj.parent.mkdir(parents=True, exist_ok=True)
    path_obj.write_text(f"{json.dumps(payload, indent=2)}\n", "utf8")


def round_float(value: float) -> float:
    return float(round(value, 6))


if __name__ == "__main__":
    main()
