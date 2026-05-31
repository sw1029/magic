#!/usr/bin/env python3
"""Mass synthetic analysis for user-defined shape regex inputs.

This script intentionally does not persist every generated stroke.  It streams
1M+ cases, keeps deterministic seeds and aggregate statistics, and writes only
compact summaries plus a small sample table.
"""

from __future__ import annotations

import argparse
import csv
import json
import math
import random
import statistics
from collections import Counter, defaultdict
from dataclasses import dataclass, field
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_SHAPE_SURVEY_JSON = ROOT / "survey" / "a50c3bd7cd0f45359c_shape-composition-survey.json"
DEFAULT_DASHBOARD_SUMMARY = ROOT / "survey-analysis-output" / "deep-dive-20260517-0824" / "analysis_summary.json"
DEFAULT_OUT_ROOT = ROOT / "survey-analysis-output"

SHAPES = [
    "line",
    "arrow",
    "rect",
    "roundRect",
    "ellipse",
    "triangle",
    "diamond",
    "elbow",
    "rightArrow",
    "downArrow",
    "arc",
    "curve",
    "wave",
    "braceL",
    "braceR",
]

OPEN_SHAPES = {"line", "arrow", "elbow", "arc", "curve", "wave", "braceL", "braceR"}
CLOSED_SHAPES = set(SHAPES) - OPEN_SHAPES
COMBINATORS = ["+", "->", "&"]

NOISE_PRESETS = [
    "stable_baseline",
    "personal_gt_residual",
    "trace_offset",
    "open_gap_stress",
    "rotation_drift",
    "jitter_noise_stress",
    "fast_compression",
    "closed_gap_boundary",
    "composition_merge",
    "tutorial_variation",
]

POLICIES = ["baseline", "regex_gt", "tutorial_bg", "tinyml_priority"]


@dataclass
class RunningStats:
    count: int = 0
    total: float = 0.0
    total_sq: float = 0.0
    min_value: float = math.inf
    max_value: float = -math.inf

    def add(self, value: float) -> None:
        self.count += 1
        self.total += value
        self.total_sq += value * value
        self.min_value = min(self.min_value, value)
        self.max_value = max(self.max_value, value)

    def mean(self) -> float:
        return self.total / self.count if self.count else 0.0

    def sd(self) -> float:
        if self.count <= 1:
            return 0.0
        variance = max(0.0, (self.total_sq - self.total * self.total / self.count) / (self.count - 1))
        return math.sqrt(variance)


@dataclass
class PolicyAggregate:
    cases: int = 0
    valid_mass: float = 0.0
    accept_mass: float = 0.0
    accepted_valid_mass: float = 0.0
    unsafe_accept_mass: float = 0.0
    false_reject_mass: float = 0.0
    priority_flip_mass: float = 0.0
    correction_gain: RunningStats = field(default_factory=RunningStats)
    score: RunningStats = field(default_factory=RunningStats)
    threshold: RunningStats = field(default_factory=RunningStats)
    threshold_bias: RunningStats = field(default_factory=RunningStats)
    risk: RunningStats = field(default_factory=RunningStats)

    def add(self, result: dict[str, float | bool]) -> None:
        valid = float(result["valid_probability"])
        accept = 1.0 if result["accepted"] else 0.0
        unsafe = float(result["unsafe_probability"]) * accept
        self.cases += 1
        self.valid_mass += valid
        self.accept_mass += accept
        self.accepted_valid_mass += accept * valid * (1.0 - float(result["unsafe_probability"]))
        self.unsafe_accept_mass += unsafe
        self.false_reject_mass += (1.0 - accept) * valid
        self.priority_flip_mass += float(result["priority_flip_probability"])
        self.correction_gain.add(float(result["correction_gain"]))
        self.score.add(float(result["score"]))
        self.threshold.add(float(result["threshold"]))
        self.threshold_bias.add(float(result["threshold_bias"]))
        self.risk.add(float(result["unsafe_probability"]))

    def row(self, group: dict[str, str]) -> dict[str, str | int | float]:
        precision = 1.0 - safe_div(self.unsafe_accept_mass, self.accept_mass)
        recall = safe_div(self.accepted_valid_mass, self.valid_mass)
        return {
            **group,
            "cases": self.cases,
            "accept_rate": round6(safe_div(self.accept_mass, self.cases)),
            "expected_precision": round6(precision),
            "expected_recall": round6(recall),
            "expected_unsafe_accept_rate": round6(safe_div(self.unsafe_accept_mass, self.cases)),
            "expected_false_reject_rate": round6(safe_div(self.false_reject_mass, self.cases)),
            "priority_flip_rate": round6(safe_div(self.priority_flip_mass, self.cases)),
            "avg_score": round6(self.score.mean()),
            "avg_threshold": round6(self.threshold.mean()),
            "avg_threshold_bias": round6(self.threshold_bias.mean()),
            "avg_correction_gain": round6(self.correction_gain.mean()),
            "avg_unsafe_probability": round6(self.risk.mean()),
            "score_sd": round6(self.score.sd()),
            "threshold_sd": round6(self.threshold.sd()),
        }


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--cases", type=int, default=1_200_000)
    parser.add_argument("--seed", type=int, default=20260529)
    parser.add_argument("--sample-size", type=int, default=2000)
    parser.add_argument("--shape-survey-json", type=Path, default=DEFAULT_SHAPE_SURVEY_JSON)
    parser.add_argument("--dashboard-summary-json", type=Path, default=DEFAULT_DASHBOARD_SUMMARY)
    parser.add_argument("--out-root", type=Path, default=DEFAULT_OUT_ROOT)
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    if args.cases < 1_000_000:
        raise SystemExit("--cases must be at least 1000000 for this mass analysis")

    rng = random.Random(args.seed)
    shape_survey = read_json(args.shape_survey_json)
    dashboard_summary = read_json(args.dashboard_summary_json) if args.dashboard_summary_json.exists() else {}
    observed = summarize_observed_shape_survey(shape_survey)
    dashboard_noise = summarize_dashboard_noise(dashboard_summary)
    regex_space = summarize_regex_space(max_items=4)

    timestamp = datetime.now(timezone.utc).strftime("%Y%m%d-%H%M%S")
    out_dir = args.out_root / f"shape-regex-noise-mass-{timestamp}"
    out_dir.mkdir(parents=True, exist_ok=True)

    global_agg: dict[str, PolicyAggregate] = defaultdict(PolicyAggregate)
    topology_agg: dict[tuple[str, str], PolicyAggregate] = defaultdict(PolicyAggregate)
    preset_agg: dict[tuple[str, str], PolicyAggregate] = defaultdict(PolicyAggregate)
    item_count_agg: dict[tuple[str, int, str], PolicyAggregate] = defaultdict(PolicyAggregate)
    threshold_bin_agg: dict[tuple[str, str, str], PolicyAggregate] = defaultdict(PolicyAggregate)
    regex_counter: Counter[str] = Counter()
    sample_rows: list[dict[str, Any]] = []

    for case_index in range(args.cases):
        case = generate_case(rng, case_index, observed)
        regex_counter[case["regex"]] += 1
        policy_results = evaluate_policies(case, observed, dashboard_noise)

        for policy, result in policy_results.items():
            global_agg[policy].add(result)
            topology_agg[(case["topology"], policy)].add(result)
            preset_agg[(case["noise_preset"], policy)].add(result)
            item_count_agg[(case["topology"], case["item_count"], policy)].add(result)
            threshold_bin_agg[(case["noise_preset"], threshold_bin(result["threshold"]), policy)].add(result)

        if len(sample_rows) < args.sample_size:
            sample_rows.append(sample_case_row(case, policy_results))
        else:
            replace = rng.randrange(case_index + 1)
            if replace < args.sample_size:
                sample_rows[replace] = sample_case_row(case, policy_results)

    write_csv(out_dir / "policy_summary.csv", [agg.row({"policy": policy}) for policy, agg in sorted(global_agg.items())])
    write_csv(
        out_dir / "topology_policy_summary.csv",
        [
            agg.row({"topology": topology, "policy": policy})
            for (topology, policy), agg in sorted(topology_agg.items())
        ],
    )
    write_csv(
        out_dir / "noise_policy_summary.csv",
        [
            agg.row({"noise_preset": preset, "policy": policy})
            for (preset, policy), agg in sorted(preset_agg.items())
        ],
    )
    write_csv(
        out_dir / "item_count_policy_summary.csv",
        [
            agg.row({"topology": topology, "item_count": item_count, "policy": policy})
            for (topology, item_count, policy), agg in sorted(item_count_agg.items())
        ],
    )
    write_csv(
        out_dir / "threshold_bin_summary.csv",
        [
            agg.row({"noise_preset": preset, "threshold_bin": bin_id, "policy": policy})
            for (preset, bin_id, policy), agg in sorted(threshold_bin_agg.items())
        ],
    )
    write_csv(out_dir / "sample_cases.csv", sample_rows)
    write_json(
        out_dir / "analysis_summary.json",
        {
            "generatedAt": datetime.now(timezone.utc).isoformat(),
            "caseCount": args.cases,
            "seed": args.seed,
            "regexSpace": regex_space,
            "shapeCatalog": {
                "totalShapeTools": len(SHAPES),
                "openShapes": sorted(OPEN_SHAPES),
                "closedShapes": sorted(CLOSED_SHAPES),
                "combinators": COMBINATORS,
            },
            "observedShapeSurvey": observed,
            "dashboardNoisePrior": dashboard_noise,
            "topRegexSkeletons": regex_counter.most_common(30),
            "outputFiles": [
                "policy_summary.csv",
                "topology_policy_summary.csv",
                "noise_policy_summary.csv",
                "item_count_policy_summary.csv",
                "threshold_bin_summary.csv",
                "sample_cases.csv",
                "analysis_report.md",
            ],
        },
    )
    write_report(out_dir, args.cases, regex_space, observed, dashboard_noise, global_agg)
    print(json.dumps({"outDir": str(out_dir), "cases": args.cases, "seed": args.seed}, ensure_ascii=False))


def read_json(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8"))


def summarize_regex_space(max_items: int) -> dict[str, Any]:
    exact_by_items: dict[str, int] = {}
    total = 0
    shape_count = len(SHAPES)
    op_count = len(COMBINATORS)
    for item_count in range(1, max_items + 1):
        count = (shape_count**item_count) * (op_count ** max(item_count - 1, 0))
        exact_by_items[str(item_count)] = count
        total += count
    with_repetition_macro_floor = total + shape_count * 4 * max_items
    return {
        "grammar": "SPEC := ITEM (OP ITEM){0,n}; ITEM := SHAPE | SHAPE{repeat}; OP := + | -> | &",
        "maxItemsCounted": max_items,
        "shapeCount": shape_count,
        "operatorCount": op_count,
        "orderedSkeletonsByItemCount": exact_by_items,
        "orderedSkeletonsTotal": total,
        "withSimpleRepeatMacroFloor": with_repetition_macro_floor,
    }


def summarize_observed_shape_survey(payload: dict[str, Any]) -> dict[str, Any]:
    compositions = payload.get("compositions", [])
    trials = payload.get("trials", [])
    shape_counts = Counter()
    topology_counts = Counter()
    composition_rows = []

    for comp in compositions:
        shapes = comp.get("shapes", [])
        open_count = sum(1 for shape in shapes if shape.get("type") in OPEN_SHAPES)
        closed_count = sum(1 for shape in shapes if shape.get("type") in CLOSED_SHAPES)
        topology = topology_label(open_count, closed_count)
        topology_counts[topology] += 1
        shape_counts.update(shape.get("type") for shape in shapes)
        comp_trials = [trial for trial in trials if trial.get("compositionIndex") == comp.get("index")]
        one_point_strokes, total_strokes = count_one_point_strokes(comp_trials)
        offsets, scales = composition_offsets(comp, comp_trials)
        elapsed = [trial.get("elapsedMs", 0) / 1000 for trial in comp_trials]
        stroke_counts = [len(trial.get("strokes", [])) for trial in comp_trials]
        point_counts = [sum(len(stroke.get("samples", [])) for stroke in trial.get("strokes", [])) for trial in comp_trials]
        composition_rows.append(
            {
                "compositionIndex": comp.get("index", 0) + 1,
                "shapeTypes": [shape.get("type") for shape in shapes],
                "topology": topology,
                "openCount": open_count,
                "closedCount": closed_count,
                "editEventCount": len(comp.get("editEvents", [])),
                "trialCount": len(comp_trials),
                "elapsedMeanSec": round6(mean(elapsed)),
                "elapsedCv": round6(coef_var(elapsed)),
                "strokeMean": round6(mean(stroke_counts)),
                "pointMean": round6(mean(point_counts)),
                "onePointStrokeRate": round6(safe_div(one_point_strokes, total_strokes)),
                "avgCenterOffsetX": round6(mean([item[0] for item in offsets])),
                "avgCenterOffsetY": round6(mean([item[1] for item in offsets])),
                "avgScaleX": round6(mean([item[0] for item in scales])),
                "avgScaleY": round6(mean([item[1] for item in scales])),
            }
        )

    all_trials_by_block: dict[str, list[dict[str, Any]]] = defaultdict(list)
    for trial in trials:
        all_trials_by_block[str(trial.get("blockId", "unknown"))].append(trial)

    block_rows = {}
    for block, block_trials in all_trials_by_block.items():
        elapsed = [trial.get("elapsedMs", 0) / 1000 for trial in block_trials]
        strokes = [len(trial.get("strokes", [])) for trial in block_trials]
        points = [sum(len(stroke.get("samples", [])) for stroke in trial.get("strokes", [])) for trial in block_trials]
        block_rows[block] = {
            "n": len(block_trials),
            "elapsedMeanSec": round6(mean(elapsed)),
            "elapsedCv": round6(coef_var(elapsed)),
            "strokeMean": round6(mean(strokes)),
            "pointMean": round6(mean(points)),
        }

    return {
        "sessionId": payload.get("sessionId"),
        "trialCount": len(trials),
        "compositionCount": len(compositions),
        "shapeTypeCounts": dict(shape_counts),
        "topologyCounts": dict(topology_counts),
        "compositionRows": composition_rows,
        "blockRows": block_rows,
    }


def summarize_dashboard_noise(summary: dict[str, Any]) -> dict[str, Any]:
    synthetic = summary.get("synthetic", {}) if isinstance(summary, dict) else {}
    threshold = summary.get("threshold", {}) if isinstance(summary, dict) else {}
    top_cells = synthetic.get("topOverlapCells", []) if isinstance(synthetic, dict) else []
    if not top_cells:
        return {
            "source": "fallback",
            "avgJitterPx": 6.0,
            "avgOpenGapRatio": 0.12,
            "avgCurveWarp": 0.08,
            "syntheticChangedRows": 0,
            "syntheticAverageEffectiveThresholdBias": 0.0,
        }

    return {
        "source": "deep-dive-summary",
        "topOverlapCellCount": len(top_cells),
        "avgJitterPx": round6(mean([float(cell.get("avg_jitter_px", 0)) for cell in top_cells])),
        "avgOpenGapRatio": round6(mean([float(cell.get("avg_open_gap_ratio", 0)) for cell in top_cells])),
        "avgCurveWarp": round6(mean([float(cell.get("avg_curve_warp", 0)) for cell in top_cells])),
        "avgScoreGap": round6(mean([float(cell.get("avg_score_gap", 0)) for cell in top_cells])),
        "syntheticChangedRows": threshold.get("syntheticChangedRows", 0),
        "syntheticAverageEffectiveThresholdBias": threshold.get("syntheticAverageEffectiveThresholdBias", 0),
        "surveyAverageEffectiveThresholdBias": threshold.get("surveyAverageEffectiveThresholdBias", 0),
    }


def generate_case(rng: random.Random, case_index: int, observed: dict[str, Any]) -> dict[str, Any]:
    item_count = weighted_choice(rng, [(1, 0.16), (2, 0.24), (3, 0.22), (4, 0.2), (5, 0.11), (6, 0.07)])
    shapes = [choose_shape(rng, observed) for _ in range(item_count)]
    if rng.random() < 0.16 and item_count >= 2:
        repeat_shape = rng.choice(["line", "wave", "arc", "rect", "ellipse"])
        repeat_count = rng.choice([2, 3, 4])
        shapes = [repeat_shape] * min(repeat_count, item_count) + shapes[min(repeat_count, item_count) :]
    ops = [weighted_choice(rng, [("+", 0.58), ("->", 0.26), ("&", 0.16)]) for _ in range(max(item_count - 1, 0))]
    regex = regex_for(shapes, ops)
    open_count = sum(1 for shape in shapes if shape in OPEN_SHAPES)
    closed_count = item_count - open_count
    topology = topology_label(open_count, closed_count)
    noise_preset = weighted_choice(
        rng,
        [
            ("stable_baseline", 0.14),
            ("personal_gt_residual", 0.16),
            ("trace_offset", 0.1),
            ("open_gap_stress", 0.1),
            ("rotation_drift", 0.09),
            ("jitter_noise_stress", 0.1),
            ("fast_compression", 0.1),
            ("closed_gap_boundary", 0.08),
            ("composition_merge", 0.08),
            ("tutorial_variation", 0.05),
        ],
    )
    noise = draw_noise(rng, noise_preset, topology, item_count)
    expected_strokes = max(1.0, open_count * 0.9 + closed_count * 1.25)
    if noise_preset in {"fast_compression", "composition_merge"}:
        observed_strokes = max(1.0, expected_strokes * rng.uniform(0.45, 0.82))
    else:
        observed_strokes = max(1.0, expected_strokes * rng.uniform(0.82, 1.3))
    repeated = item_count - len(set(shapes))
    complexity = clamp((item_count - 1) / 5 + repeated * 0.07 + (0.18 if topology == "mixed" else 0), 0, 1.35)
    return {
        "case_index": case_index,
        "regex": regex,
        "shapes": shapes,
        "ops": ops,
        "item_count": item_count,
        "open_count": open_count,
        "closed_count": closed_count,
        "topology": topology,
        "noise_preset": noise_preset,
        "expected_strokes": expected_strokes,
        "observed_strokes": observed_strokes,
        "complexity": complexity,
        "repeated_shape_count": repeated,
        **noise,
    }


def choose_shape(rng: random.Random, observed: dict[str, Any]) -> str:
    counts = observed.get("shapeTypeCounts", {})
    observed_shapes = [(shape, 0.04 + float(counts.get(shape, 0)) * 0.035) for shape in SHAPES]
    open_boost = 0.008
    weighted = []
    for shape, weight in observed_shapes:
        if shape in OPEN_SHAPES:
            weight += open_boost
        weighted.append((shape, weight))
    return weighted_choice(rng, weighted)


def draw_noise(rng: random.Random, preset: str, topology: str, item_count: int) -> dict[str, float]:
    closed_factor = 1.0 if topology in {"closed_only", "mixed"} else 0.35
    if preset == "stable_baseline":
        jitter = rng.uniform(0, 3)
        open_gap = rng.uniform(0, 0.035) * closed_factor
        rotation = rng.uniform(-5, 5)
        curve = rng.uniform(0, 0.04)
        extra_noise = rng.choice([0, 0, 0, 1])
        translation = rng.uniform(0, 35)
        scale = rng.uniform(0.94, 1.06)
    elif preset == "personal_gt_residual":
        jitter = rng.uniform(1, 8)
        open_gap = rng.uniform(0.015, 0.12) * closed_factor
        rotation = rng.uniform(-12, 12)
        curve = rng.uniform(0.02, 0.12)
        extra_noise = rng.choice([0, 0, 1])
        translation = abs(rng.gauss(110, 85))
        scale = rng.uniform(0.74, 1.12)
    elif preset == "trace_offset":
        jitter = rng.uniform(0, 6)
        open_gap = rng.uniform(0, 0.08) * closed_factor
        rotation = rng.uniform(-8, 8)
        curve = rng.uniform(0, 0.08)
        extra_noise = 0
        translation = abs(rng.gauss(280, 90))
        scale = rng.uniform(0.86, 1.16)
    elif preset == "open_gap_stress":
        jitter = rng.uniform(3, 12)
        open_gap = rng.uniform(0.18, 0.55) * closed_factor
        rotation = rng.uniform(-12, 12)
        curve = rng.uniform(0.02, 0.14)
        extra_noise = rng.choice([0, 1])
        translation = rng.uniform(20, 160)
        scale = rng.uniform(0.8, 1.2)
    elif preset == "rotation_drift":
        jitter = rng.uniform(1, 8)
        open_gap = rng.uniform(0, 0.08) * closed_factor
        rotation = rng.uniform(-48, 48)
        curve = rng.uniform(0.04, 0.28)
        extra_noise = rng.choice([0, 0, 1])
        translation = rng.uniform(15, 140)
        scale = rng.uniform(0.78, 1.24)
    elif preset == "jitter_noise_stress":
        jitter = rng.uniform(10, 26)
        open_gap = rng.uniform(0.02, 0.22) * closed_factor
        rotation = rng.uniform(-18, 18)
        curve = rng.uniform(0.08, 0.28)
        extra_noise = rng.randint(1, 5)
        translation = rng.uniform(20, 180)
        scale = rng.uniform(0.72, 1.28)
    elif preset == "fast_compression":
        jitter = rng.uniform(4, 16)
        open_gap = rng.uniform(0.02, 0.18) * closed_factor
        rotation = rng.uniform(-20, 20)
        curve = rng.uniform(0.04, 0.2)
        extra_noise = rng.choice([0, 0, 1])
        translation = rng.uniform(40, 210)
        scale = rng.uniform(0.68, 1.14)
    elif preset == "closed_gap_boundary":
        jitter = rng.uniform(2, 10)
        open_gap = rng.uniform(0.12, 0.34) * (1.0 if topology in {"closed_only", "mixed"} else 0.2)
        rotation = rng.uniform(-15, 15)
        curve = rng.uniform(0.02, 0.18)
        extra_noise = rng.choice([0, 1])
        translation = rng.uniform(10, 130)
        scale = rng.uniform(0.82, 1.18)
    elif preset == "composition_merge":
        jitter = rng.uniform(2, 14)
        open_gap = rng.uniform(0.01, 0.16) * closed_factor
        rotation = rng.uniform(-16, 16)
        curve = rng.uniform(0.04, 0.18)
        extra_noise = rng.choice([0, 0, 1, 2])
        translation = rng.uniform(35, 200)
        scale = rng.uniform(0.7, 1.2)
    else:
        jitter = rng.uniform(1, 9)
        open_gap = rng.uniform(0, 0.16) * closed_factor
        rotation = rng.uniform(-22, 22)
        curve = rng.uniform(0.02, 0.2)
        extra_noise = rng.choice([0, 0, 1, 2])
        translation = rng.uniform(15, 170)
        scale = rng.uniform(0.78, 1.24)

    one_point_rate = clamp(0.025 + jitter / 300 + extra_noise * 0.012 + (0.025 if preset == "fast_compression" else 0), 0, 0.18)
    duration_ms = max(500, rng.gauss(4200 / max(0.55, item_count**0.35), 900))
    if preset == "fast_compression":
        duration_ms *= rng.uniform(0.42, 0.72)
    if preset == "trace_offset":
        duration_ms *= rng.uniform(1.1, 1.8)
    return {
        "jitter_px": jitter,
        "open_gap_ratio": open_gap,
        "rotation_deg": rotation,
        "curve_warp": curve,
        "extra_noise_strokes": float(extra_noise),
        "translation_px": translation,
        "scale": scale,
        "one_point_rate": one_point_rate,
        "duration_ms": duration_ms,
    }


def evaluate_policies(
    case: dict[str, Any],
    observed: dict[str, Any],
    dashboard_noise: dict[str, Any],
) -> dict[str, dict[str, float | bool]]:
    latent = latent_quality(case)
    valid_probability = sigmoid((latent - 0.54) * 8.0)
    ambiguity = ambiguity_risk(case)
    baseline_score = score_for(case, transform_correction=0.0, user_noise_fit=0.0)
    regex_score = score_for(case, transform_correction=0.72, user_noise_fit=0.1)
    tutorial_fit = tutorial_fit_strength(case, observed)
    tutorial_score = score_for(case, transform_correction=0.86, user_noise_fit=tutorial_fit)
    tiny_delta = tinyml_priority_delta(case, dashboard_noise, tutorial_fit)
    tiny_score = clamp(tutorial_score + tiny_delta, 0, 1)

    baseline_threshold = base_threshold(case)
    regex_threshold = baseline_threshold - 0.008
    tutorial_bias = tutorial_threshold_bias(case, observed)
    tutorial_threshold = guardrailed_threshold(case, baseline_threshold - tutorial_bias)
    tiny_threshold = guardrailed_threshold(case, tutorial_threshold - max(tiny_delta * 0.22, 0))

    competitor_gap = competitor_margin(case, ambiguity)
    priority_flip = sigmoid((abs(tiny_delta) - competitor_gap) * 35) * min(1.0, tutorial_fit + 0.25)

    results = {
        "baseline": (baseline_score, baseline_threshold, 0.0, 0.0),
        "regex_gt": (regex_score, regex_threshold, baseline_threshold - regex_threshold, 0.0),
        "tutorial_bg": (tutorial_score, tutorial_threshold, baseline_threshold - tutorial_threshold, 0.0),
        "tinyml_priority": (tiny_score, tiny_threshold, baseline_threshold - tiny_threshold, priority_flip),
    }

    out: dict[str, dict[str, float | bool]] = {}
    for policy, (score, threshold, threshold_bias, flip) in results.items():
        accepted = score >= threshold
        correction_gain = score - baseline_score
        unsafe_probability = unsafe_probability_for(case, score, threshold, ambiguity, policy, flip)
        out[policy] = {
            "score": score,
            "threshold": threshold,
            "threshold_bias": threshold_bias,
            "accepted": accepted,
            "valid_probability": valid_probability,
            "unsafe_probability": unsafe_probability,
            "priority_flip_probability": flip,
            "correction_gain": correction_gain,
        }
    return out


def score_for(case: dict[str, Any], transform_correction: float, user_noise_fit: float) -> float:
    open_share = safe_div(case["open_count"], case["item_count"])
    closed_share = safe_div(case["closed_count"], case["item_count"])
    stroke_ratio = safe_div(case["observed_strokes"], case["expected_strokes"])
    translation_penalty = min(case["translation_px"] / 360, 1.25) * (0.34 * (1 - transform_correction))
    scale_penalty = min(abs(math.log(case["scale"])) / math.log(1.55), 1.0) * (0.22 * (1 - transform_correction * 0.65))
    jitter_penalty = min(case["jitter_px"] / 36, 1.0) * 0.17
    open_gap_penalty = min(case["open_gap_ratio"] / 0.52, 1.0) * (0.29 * closed_share + 0.08 * open_share)
    rotation_penalty = min(abs(case["rotation_deg"]) / 60, 1.0) * (0.12 + 0.04 * closed_share)
    curve_penalty = min(case["curve_warp"] / 0.32, 1.0) * (0.1 + 0.03 * open_share)
    noise_penalty = min(case["extra_noise_strokes"] / 5, 1.0) * 0.13
    split_penalty = min(abs(stroke_ratio - 1.0), 1.0) * (0.14 + 0.04 * case["complexity"])
    one_point_penalty = min(case["one_point_rate"] / 0.18, 1.0) * 0.06
    complexity_penalty = case["complexity"] * 0.035
    user_fit_bonus = user_noise_fit * (0.055 + 0.025 * (case["noise_preset"] in {"personal_gt_residual", "fast_compression"}))
    raw = (
        0.93
        - translation_penalty
        - scale_penalty
        - jitter_penalty
        - open_gap_penalty
        - rotation_penalty
        - curve_penalty
        - noise_penalty
        - split_penalty
        - one_point_penalty
        - complexity_penalty
        + user_fit_bonus
    )
    return clamp(raw, 0.02, 0.995)


def latent_quality(case: dict[str, Any]) -> float:
    return score_for(case, transform_correction=0.88, user_noise_fit=0.55) - min(case["extra_noise_strokes"], 4) * 0.015


def base_threshold(case: dict[str, Any]) -> float:
    closed_share = safe_div(case["closed_count"], case["item_count"])
    repeated_risk = min(case["repeated_shape_count"] * 0.012, 0.045)
    return clamp(0.7 + closed_share * 0.035 + case["complexity"] * 0.025 + repeated_risk, 0.66, 0.82)


def tutorial_threshold_bias(case: dict[str, Any], observed: dict[str, Any]) -> float:
    composition_rows = observed.get("compositionRows", [])
    mixed_cv = mean([row.get("elapsedCv", 0) for row in composition_rows if row.get("topology") == "mixed"])
    open_cv = mean([row.get("elapsedCv", 0) for row in composition_rows if row.get("topology") == "open_only"])
    stable_bonus = 0.018 if case["topology"] == "open_only" and open_cv <= 0.55 else 0.0
    mixed_penalty = -0.006 if case["topology"] == "mixed" and mixed_cv >= 0.75 else 0.0
    sample_bias = 0.01 + min(case["item_count"], 6) * 0.0025
    preset_bonus = 0.012 if case["noise_preset"] in {"personal_gt_residual", "fast_compression", "composition_merge"} else 0.0
    high_risk_penalty = -0.015 if case["open_gap_ratio"] > 0.28 and case["closed_count"] > 0 else 0.0
    return clamp(sample_bias + preset_bonus + stable_bonus + mixed_penalty + high_risk_penalty, 0, 0.055)


def guardrailed_threshold(case: dict[str, Any], threshold: float) -> float:
    if case["closed_count"] > 0 and case["open_gap_ratio"] > 0.28:
        threshold += 0.025
    if case["extra_noise_strokes"] >= 3:
        threshold += 0.018
    if abs(case["rotation_deg"]) > 38 and case["jitter_px"] > 12:
        threshold += 0.016
    return clamp(threshold, 0.62, 0.86)


def tutorial_fit_strength(case: dict[str, Any], observed: dict[str, Any]) -> float:
    if case["noise_preset"] == "personal_gt_residual":
        return 0.85
    if case["noise_preset"] in {"fast_compression", "composition_merge"}:
        return 0.62
    if case["topology"] == "open_only":
        return 0.52
    if case["topology"] == "mixed":
        return 0.38
    return 0.45


def tinyml_priority_delta(case: dict[str, Any], dashboard_noise: dict[str, Any], tutorial_fit: float) -> float:
    source_bias = float(dashboard_noise.get("syntheticAverageEffectiveThresholdBias", 0) or 0)
    prototype_signal = tutorial_fit * 0.055
    topology_bonus = 0.012 if case["noise_preset"] in {"personal_gt_residual", "composition_merge"} else 0.0
    risk_penalty = 0.0
    if case["closed_count"] > 0 and case["open_gap_ratio"] > 0.22:
        risk_penalty += 0.025
    if case["extra_noise_strokes"] >= 3:
        risk_penalty += 0.018
    if case["repeated_shape_count"] >= 2:
        risk_penalty += 0.012
    return clamp(source_bias * 0.35 + prototype_signal + topology_bonus - risk_penalty, -0.03, 0.075)


def ambiguity_risk(case: dict[str, Any]) -> float:
    repeated = min(case["repeated_shape_count"] * 0.045, 0.16)
    topology = 0.04 if case["topology"] == "mixed" else 0.025 if case["topology"] == "open_only" else 0.035
    noise = min(case["extra_noise_strokes"] * 0.025, 0.12)
    gap = 0.1 if case["closed_count"] > 0 and case["open_gap_ratio"] > 0.2 else 0
    rotation = 0.04 if abs(case["rotation_deg"]) > 32 else 0
    return clamp(topology + repeated + noise + gap + rotation + case["complexity"] * 0.035, 0.02, 0.55)


def competitor_margin(case: dict[str, Any], ambiguity: float) -> float:
    margin = 0.17 - ambiguity * 0.22 - min(case["jitter_px"] / 50, 0.08) - min(case["extra_noise_strokes"] * 0.008, 0.04)
    return clamp(margin, 0.015, 0.2)


def unsafe_probability_for(
    case: dict[str, Any],
    score: float,
    threshold: float,
    ambiguity: float,
    policy: str,
    priority_flip: float,
) -> float:
    over_accept = max(score - threshold, 0)
    risk = ambiguity * 0.45 + max(case["open_gap_ratio"] - 0.18, 0) * 0.28 + min(case["extra_noise_strokes"] * 0.018, 0.09)
    if policy == "regex_gt":
        risk *= 0.92
    if policy == "tutorial_bg":
        risk *= 0.78
    if policy == "tinyml_priority":
        risk = risk * 0.72 + priority_flip * 0.06
    return clamp(risk + over_accept * 0.08, 0.0, 0.42)


def sample_case_row(case: dict[str, Any], policy_results: dict[str, dict[str, float | bool]]) -> dict[str, Any]:
    return {
        "case_index": case["case_index"],
        "regex": case["regex"],
        "topology": case["topology"],
        "noise_preset": case["noise_preset"],
        "item_count": case["item_count"],
        "open_count": case["open_count"],
        "closed_count": case["closed_count"],
        "jitter_px": round6(case["jitter_px"]),
        "open_gap_ratio": round6(case["open_gap_ratio"]),
        "rotation_deg": round6(case["rotation_deg"]),
        "curve_warp": round6(case["curve_warp"]),
        "extra_noise_strokes": int(case["extra_noise_strokes"]),
        "translation_px": round6(case["translation_px"]),
        "scale": round6(case["scale"]),
        "baseline_score": round6(float(policy_results["baseline"]["score"])),
        "baseline_accepted": policy_results["baseline"]["accepted"],
        "tutorial_score": round6(float(policy_results["tutorial_bg"]["score"])),
        "tutorial_accepted": policy_results["tutorial_bg"]["accepted"],
        "tinyml_score": round6(float(policy_results["tinyml_priority"]["score"])),
        "tinyml_accepted": policy_results["tinyml_priority"]["accepted"],
        "tinyml_priority_flip_probability": round6(float(policy_results["tinyml_priority"]["priority_flip_probability"])),
    }


def write_report(
    out_dir: Path,
    cases: int,
    regex_space: dict[str, Any],
    observed: dict[str, Any],
    dashboard_noise: dict[str, Any],
    global_agg: dict[str, PolicyAggregate],
) -> None:
    rows = [global_agg[policy].row({"policy": policy}) for policy in POLICIES]
    best_precision = max(rows, key=lambda row: float(row["expected_precision"]))
    best_recall = max(rows, key=lambda row: float(row["expected_recall"]))
    lines = [
        "# Shape Regex Noise Mass Analysis",
        "",
        f"- Generated cases: `{cases}`.",
        f"- Counted ordered regex skeletons up to 4 items: `{regex_space['orderedSkeletonsTotal']}`.",
        f"- Current shape survey trials: `{observed.get('trialCount')}`.",
        f"- Dashboard prior source: `{dashboard_noise.get('source')}`.",
        "",
        "## Global Policy Summary",
        "",
        "| policy | accept_rate | expected_precision | expected_recall | unsafe_accept_rate | avg_threshold_bias | priority_flip_rate |",
        "|---|---:|---:|---:|---:|---:|---:|",
    ]
    for row in rows:
        lines.append(
            "| {policy} | {accept_rate} | {expected_precision} | {expected_recall} | "
            "{expected_unsafe_accept_rate} | {avg_threshold_bias} | {priority_flip_rate} |".format(**row)
        )
    lines.extend(
        [
            "",
            "## Interpretation",
            "",
            f"- Highest expected precision policy: `{best_precision['policy']}`.",
            f"- Highest expected recall policy: `{best_recall['policy']}`.",
            "- Regex GT correction mostly removes translation/scale penalties; it should not by itself lower high-risk closed-shape gates.",
            "- Tutorial background correction is the best fit for user-specific posterior updates because it can separate stable personal residuals from unsafe noise.",
            "- TinyML priority shifts are useful when they reorder near-tie candidates, but they need guardrails for closed-shape open gaps, repeated shapes, and extra noise strokes.",
            "",
            "## Output Files",
            "",
            "- `analysis_summary.json`",
            "- `policy_summary.csv`",
            "- `topology_policy_summary.csv`",
            "- `noise_policy_summary.csv`",
            "- `item_count_policy_summary.csv`",
            "- `threshold_bin_summary.csv`",
            "- `sample_cases.csv`",
        ]
    )
    (out_dir / "analysis_report.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


def write_json(path: Path, payload: Any) -> None:
    path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def write_csv(path: Path, rows: list[dict[str, Any]]) -> None:
    if not rows:
        path.write_text("", encoding="utf-8")
        return
    with path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(rows[0].keys()))
        writer.writeheader()
        writer.writerows(rows)


def regex_for(shapes: list[str], ops: list[str]) -> str:
    tokens: list[str] = []
    index = 0
    while index < len(shapes):
        repeat = 1
        while index + repeat < len(shapes) and shapes[index + repeat] == shapes[index]:
            repeat += 1
        token = shapes[index] if repeat == 1 else f"{shapes[index]}{{{repeat}}}"
        tokens.append(token)
        index += repeat
    if len(tokens) == 1:
        return tokens[0]
    normalized_ops = ops[: max(len(tokens) - 1, 0)]
    while len(normalized_ops) < len(tokens) - 1:
        normalized_ops.append("+")
    out = [tokens[0]]
    for op, token in zip(normalized_ops, tokens[1:]):
        out.append(op)
        out.append(token)
    return " ".join(out)


def topology_label(open_count: int, closed_count: int) -> str:
    if open_count > 0 and closed_count > 0:
        return "mixed"
    if open_count > 0:
        return "open_only"
    if closed_count > 0:
        return "closed_only"
    return "empty"


def count_one_point_strokes(trials: list[dict[str, Any]]) -> tuple[int, int]:
    one_point = 0
    total = 0
    for trial in trials:
        for stroke in trial.get("strokes", []):
            total += 1
            if len(stroke.get("samples", [])) <= 1:
                one_point += 1
    return one_point, total


def composition_offsets(comp: dict[str, Any], trials: list[dict[str, Any]]) -> tuple[list[tuple[float, float]], list[tuple[float, float]]]:
    shapes = comp.get("shapes", [])
    if not shapes:
        return [], []
    target_bbox = union_bboxes([(shape["x"], shape["y"], shape["x"] + shape["w"], shape["y"] + shape["h"]) for shape in shapes])
    target_center = bbox_center(target_bbox)
    target_w = max(target_bbox[2] - target_bbox[0], 1)
    target_h = max(target_bbox[3] - target_bbox[1], 1)
    offsets: list[tuple[float, float]] = []
    scales: list[tuple[float, float]] = []
    for trial in trials:
        points = [(sample["x"], sample["y"]) for stroke in trial.get("strokes", []) for sample in stroke.get("samples", [])]
        if not points:
            continue
        user_bbox = (min(x for x, _ in points), min(y for _, y in points), max(x for x, _ in points), max(y for _, y in points))
        user_center = bbox_center(user_bbox)
        offsets.append((user_center[0] - target_center[0], user_center[1] - target_center[1]))
        scales.append(((user_bbox[2] - user_bbox[0]) / target_w, (user_bbox[3] - user_bbox[1]) / target_h))
    return offsets, scales


def union_bboxes(bboxes: list[tuple[float, float, float, float]]) -> tuple[float, float, float, float]:
    return (
        min(bbox[0] for bbox in bboxes),
        min(bbox[1] for bbox in bboxes),
        max(bbox[2] for bbox in bboxes),
        max(bbox[3] for bbox in bboxes),
    )


def bbox_center(bbox: tuple[float, float, float, float]) -> tuple[float, float]:
    return ((bbox[0] + bbox[2]) / 2, (bbox[1] + bbox[3]) / 2)


def threshold_bin(value: float) -> str:
    if value < 0.66:
        return "lt_0_66"
    if value < 0.7:
        return "0_66_0_70"
    if value < 0.74:
        return "0_70_0_74"
    if value < 0.78:
        return "0_74_0_78"
    return "gte_0_78"


def weighted_choice(rng: random.Random, pairs: list[tuple[Any, float]]) -> Any:
    total = sum(weight for _, weight in pairs)
    marker = rng.random() * total
    cursor = 0.0
    for value, weight in pairs:
        cursor += weight
        if marker <= cursor:
            return value
    return pairs[-1][0]


def mean(values: list[float]) -> float:
    return statistics.mean(values) if values else 0.0


def coef_var(values: list[float]) -> float:
    avg = mean(values)
    if len(values) <= 1 or avg == 0:
        return 0.0
    return statistics.stdev(values) / avg


def safe_div(numerator: float, denominator: float) -> float:
    return numerator / denominator if denominator else 0.0


def sigmoid(value: float) -> float:
    if value >= 40:
        return 1.0
    if value <= -40:
        return 0.0
    return 1 / (1 + math.exp(-value))


def clamp(value: float, low: float, high: float) -> float:
    return max(low, min(high, value))


def round6(value: float) -> float:
    return round(float(value), 6)


if __name__ == "__main__":
    main()
