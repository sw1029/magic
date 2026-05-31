#!/usr/bin/env python3
"""Case-specific synthetic analysis for user-defined shape elements.

The previous mass analysis sampled the broad regex space.  This script fixes a
taxonomy-oriented case set and generates the same number of noisy samples for
each case, so case-level behavior can be compared directly.
"""

from __future__ import annotations

import argparse
import csv
import importlib.util
import json
import math
import random
import statistics
import sys
from collections import defaultdict
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[1]
BASE_SCRIPT = ROOT / "scripts" / "shape-regex-noise-mass-analysis.py"
DEFAULT_SHAPE_SURVEY_JSON = ROOT / "survey" / "a50c3bd7cd0f45359c_shape-composition-survey.json"
DEFAULT_DASHBOARD_SUMMARY = ROOT / "survey-analysis-output" / "deep-dive-20260517-0824" / "analysis_summary.json"
DEFAULT_OUT_ROOT = ROOT / "survey-analysis-output"

SOURCE_WEIGHTS = {
    "trace": 0.55,
    "recall": 0.85,
    "variation": 1.0,
    "live": 0.72,
}

RELIABILITY_WEIGHTS = {
    "high": 1.0,
    "medium": 0.65,
    "unvalidated": 0.4,
    "feedback_only": 0.0,
}

POLICIES = ["baseline", "regex_gt", "tutorial_bg", "segmented_guarded", "tinyml_priority"]


@dataclass(frozen=True)
class CaseSpec:
    case_id: str
    label: str
    regex: str
    shapes: tuple[str, ...]
    ops: tuple[str, ...]
    relation: str
    primary_primitive: str
    role_profile: str
    risk_focus: str
    confusion_base: float
    noise_bias: dict[str, float]


CASE_SPECS: tuple[CaseSpec, ...] = (
    CaseSpec(
        "C01_line_open_main",
        "single open line",
        "line",
        ("line",),
        (),
        "none",
        "line",
        "main",
        "low_complexity_transform",
        0.08,
        {"stable_baseline": 1.25, "trace_offset": 1.1, "rotation_drift": 0.8},
    ),
    CaseSpec(
        "C02_wave_open_curvature",
        "single wave curvature",
        "wave",
        ("wave",),
        (),
        "none",
        "wave",
        "main",
        "curve_warp",
        0.12,
        {"tutorial_variation": 1.35, "fast_compression": 1.15, "jitter_noise_stress": 0.9},
    ),
    CaseSpec(
        "C03_arc_open_direction",
        "single arc direction",
        "arc",
        ("arc",),
        (),
        "none",
        "arc",
        "main",
        "rotation_and_endpoint",
        0.14,
        {"rotation_drift": 1.35, "trace_offset": 1.2, "open_gap_stress": 0.75},
    ),
    CaseSpec(
        "C04_rect_closed_frame",
        "single rectangle frame",
        "rect",
        ("rect",),
        (),
        "frame",
        "rect",
        "frame",
        "closure",
        0.18,
        {"closed_gap_boundary": 1.55, "open_gap_stress": 1.25, "stable_baseline": 0.85},
    ),
    CaseSpec(
        "C05_ellipse_closed_round",
        "single ellipse",
        "ellipse",
        ("ellipse",),
        (),
        "frame",
        "ellipse",
        "frame",
        "closure_circularity",
        0.18,
        {"closed_gap_boundary": 1.35, "rotation_drift": 0.9, "trace_offset": 1.1},
    ),
    CaseSpec(
        "C06_triangle_closed_corner",
        "single triangle",
        "triangle",
        ("triangle",),
        (),
        "frame",
        "triangle",
        "frame",
        "corner_closure",
        0.22,
        {"closed_gap_boundary": 1.45, "jitter_noise_stress": 1.05, "rotation_drift": 1.0},
    ),
    CaseSpec(
        "C07_wave_line3_observed_open",
        "observed open wave with three lines",
        "wave + line{3}",
        ("wave", "line", "line", "line"),
        ("+", "+", "+"),
        "beside",
        "wave",
        "main_accent",
        "stroke_compression",
        0.2,
        {"fast_compression": 1.65, "composition_merge": 1.35, "personal_gt_residual": 1.25},
    ),
    CaseSpec(
        "C08_parallel_line3_repetition",
        "parallel repeated lines",
        "line{3}",
        ("line", "line", "line"),
        ("+", "+"),
        "parallel",
        "line",
        "repeated_main",
        "repetition_confusion",
        0.24,
        {"composition_merge": 1.55, "fast_compression": 1.25, "jitter_noise_stress": 1.1},
    ),
    CaseSpec(
        "C09_arrow_line_direction",
        "arrow then line direction",
        "arrow -> line",
        ("arrow", "line"),
        ("->",),
        "sequence",
        "arrow",
        "directional_main",
        "direction_order",
        0.21,
        {"rotation_drift": 1.55, "fast_compression": 1.1, "tutorial_variation": 1.1},
    ),
    CaseSpec(
        "C10_arc_rect_observed_mixed",
        "observed arc plus rectangle",
        "arc + rect",
        ("arc", "rect"),
        ("+",),
        "attached",
        "rect",
        "frame_accent",
        "mixed_closure_layout",
        0.3,
        {"trace_offset": 1.55, "personal_gt_residual": 1.35, "closed_gap_boundary": 1.2},
    ),
    CaseSpec(
        "C11_ellipse_line_crossing",
        "ellipse crossed by line",
        "ellipse & line",
        ("ellipse", "line"),
        ("&",),
        "crossing",
        "ellipse",
        "frame_connector",
        "crossing_closure",
        0.34,
        {"composition_merge": 1.45, "closed_gap_boundary": 1.3, "jitter_noise_stress": 1.1},
    ),
    CaseSpec(
        "C12_rect_line_inside",
        "line inside rectangle",
        "rect & line",
        ("rect", "line"),
        ("&",),
        "inside",
        "rect",
        "frame_connector",
        "containment",
        0.32,
        {"closed_gap_boundary": 1.35, "composition_merge": 1.25, "trace_offset": 1.1},
    ),
    CaseSpec(
        "C13_diamond_curve_attached",
        "curve attached to diamond",
        "diamond + curve",
        ("diamond", "curve"),
        ("+",),
        "attached",
        "diamond",
        "frame_accent",
        "rotation_closure",
        0.31,
        {"rotation_drift": 1.45, "closed_gap_boundary": 1.25, "tutorial_variation": 1.1},
    ),
    CaseSpec(
        "C14_rect_ellipse_inside_closed",
        "ellipse inside rectangle",
        "rect & ellipse",
        ("rect", "ellipse"),
        ("&",),
        "inside",
        "rect",
        "nested_frame",
        "nested_closed",
        0.38,
        {"closed_gap_boundary": 1.5, "composition_merge": 1.25, "trace_offset": 1.2},
    ),
    CaseSpec(
        "C15_triangle_diamond_overlap_closed",
        "triangle overlapping diamond",
        "triangle & diamond",
        ("triangle", "diamond"),
        ("&",),
        "overlap",
        "triangle",
        "overlapped_frame",
        "closed_overlap",
        0.42,
        {"composition_merge": 1.65, "closed_gap_boundary": 1.35, "jitter_noise_stress": 1.2},
    ),
    CaseSpec(
        "C16_right_down_arrow_closed_direction",
        "closed directional arrow pair",
        "rightArrow -> downArrow",
        ("rightArrow", "downArrow"),
        ("->",),
        "sequence",
        "rightArrow",
        "directional_frame",
        "closed_direction",
        0.4,
        {"rotation_drift": 1.75, "closed_gap_boundary": 1.25, "fast_compression": 1.0},
    ),
    CaseSpec(
        "C17_brace_pair_open_symmetric",
        "open brace pair",
        "braceL + braceR",
        ("braceL", "braceR"),
        ("+",),
        "symmetric",
        "braceL",
        "paired_open",
        "symmetry_endpoint",
        0.28,
        {"tutorial_variation": 1.35, "composition_merge": 1.25, "rotation_drift": 1.1},
    ),
    CaseSpec(
        "C18_roundrect_line2_mixed_frame",
        "round rectangle with two lines",
        "roundRect & line{2}",
        ("roundRect", "line", "line"),
        ("&", "+"),
        "inside",
        "roundRect",
        "frame_accent",
        "mixed_repeated_inside",
        0.36,
        {"closed_gap_boundary": 1.35, "composition_merge": 1.45, "fast_compression": 1.1},
    ),
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--cases-per-spec", type=int, default=50_000)
    parser.add_argument("--seed", type=int, default=20260530)
    parser.add_argument("--sample-size", type=int, default=4000)
    parser.add_argument("--shape-survey-json", type=Path, default=DEFAULT_SHAPE_SURVEY_JSON)
    parser.add_argument("--dashboard-summary-json", type=Path, default=DEFAULT_DASHBOARD_SUMMARY)
    parser.add_argument("--out-root", type=Path, default=DEFAULT_OUT_ROOT)
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    base = load_base_module()
    rng = random.Random(args.seed)
    observed = base.summarize_observed_shape_survey(base.read_json(args.shape_survey_json))
    dashboard_noise = (
        base.summarize_dashboard_noise(base.read_json(args.dashboard_summary_json))
        if args.dashboard_summary_json.exists()
        else base.summarize_dashboard_noise({})
    )
    timestamp = datetime.now(timezone.utc).strftime("%Y%m%d-%H%M%S")
    out_dir = args.out_root / f"user-shape-case-noise-50000-{timestamp}"
    out_dir.mkdir(parents=True, exist_ok=True)

    global_agg: dict[str, Any] = defaultdict(base.PolicyAggregate)
    case_agg: dict[tuple[str, str], Any] = defaultdict(base.PolicyAggregate)
    case_noise_agg: dict[tuple[str, str, str], Any] = defaultdict(base.PolicyAggregate)
    topology_agg: dict[tuple[str, str], Any] = defaultdict(base.PolicyAggregate)
    relation_agg: dict[tuple[str, str], Any] = defaultdict(base.PolicyAggregate)
    source_agg: dict[tuple[str, str], Any] = defaultdict(base.PolicyAggregate)
    activation_agg: dict[tuple[str, str], Any] = defaultdict(base.PolicyAggregate)
    primitive_agg: dict[tuple[str, str], Any] = defaultdict(base.PolicyAggregate)
    risk_gate_agg: dict[tuple[str, str], RiskAggregate] = defaultdict(RiskAggregate)
    feature_values: dict[str, dict[str, list[float]]] = defaultdict(lambda: defaultdict(list))
    sample_rows: list[dict[str, Any]] = []

    case_index = 0
    for spec in CASE_SPECS:
        for local_index in range(args.cases_per_spec):
            case = generate_taxonomy_case(base, rng, spec, case_index, local_index)
            policy_results = base.evaluate_policies(case, observed, dashboard_noise)
            policy_results["segmented_guarded"] = segmented_guarded_result(base, case, policy_results)
            add_feature_values(feature_values[spec.case_id], case)

            for policy in POLICIES:
                result = policy_results[policy]
                global_agg[policy].add(result)
                case_agg[(spec.case_id, policy)].add(result)
                case_noise_agg[(spec.case_id, case["noise_preset"], policy)].add(result)
                topology_agg[(case["topology"], policy)].add(result)
                relation_agg[(spec.relation, policy)].add(result)
                source_agg[(case["capture_source"], policy)].add(result)
                activation_agg[(case["activation_status"], policy)].add(result)
                primitive_agg[(spec.primary_primitive, policy)].add(result)

            risk_gate_agg[(spec.case_id, case["risk_band"])].add(case, policy_results)

            if len(sample_rows) < args.sample_size:
                sample_rows.append(sample_row(base, case, policy_results))
            else:
                replace = rng.randrange(case_index + 1)
                if replace < args.sample_size:
                    sample_rows[replace] = sample_row(base, case, policy_results)
            case_index += 1

    write_outputs(
        base=base,
        out_dir=out_dir,
        args=args,
        observed=observed,
        dashboard_noise=dashboard_noise,
        global_agg=global_agg,
        case_agg=case_agg,
        case_noise_agg=case_noise_agg,
        topology_agg=topology_agg,
        relation_agg=relation_agg,
        source_agg=source_agg,
        activation_agg=activation_agg,
        primitive_agg=primitive_agg,
        risk_gate_agg=risk_gate_agg,
        feature_values=feature_values,
        sample_rows=sample_rows,
    )
    print(
        json.dumps(
            {
                "outDir": str(out_dir),
                "caseCount": len(CASE_SPECS),
                "casesPerSpec": args.cases_per_spec,
                "totalCases": len(CASE_SPECS) * args.cases_per_spec,
                "seed": args.seed,
            },
            ensure_ascii=False,
        )
    )


def load_base_module() -> Any:
    spec = importlib.util.spec_from_file_location("shape_regex_noise_mass_analysis", BASE_SCRIPT)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"Cannot load base analysis script: {BASE_SCRIPT}")
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


def generate_taxonomy_case(
    base: Any,
    rng: random.Random,
    spec: CaseSpec,
    case_index: int,
    local_index: int,
) -> dict[str, Any]:
    open_count = sum(1 for shape in spec.shapes if shape in base.OPEN_SHAPES)
    closed_count = len(spec.shapes) - open_count
    topology = base.topology_label(open_count, closed_count)
    noise_preset = choose_noise_preset(base, rng, spec, topology)
    noise = base.draw_noise(rng, noise_preset, topology, len(spec.shapes))
    apply_case_noise_adjustments(base, rng, spec, noise, topology)

    expected_strokes = expected_stroke_count(spec.shapes, base)
    observed_strokes = observed_stroke_count(rng, spec, noise_preset, expected_strokes)
    repeated = len(spec.shapes) - len(set(spec.shapes))
    relation_complexity = relation_complexity_for(spec.relation)
    complexity = base.clamp(
        (len(spec.shapes) - 1) / 5
        + repeated * 0.08
        + relation_complexity
        + (0.16 if topology == "mixed" else 0.08 if topology == "closed_only" else 0.02),
        0,
        1.45,
    )
    capture_source = choose_capture_source(base, rng, noise_preset)
    reliability = choose_reliability(base, rng, capture_source, noise_preset, topology, noise)
    capture_count = draw_capture_count(rng, capture_source, reliability, local_index)
    confusion_risk = estimate_confusion_risk(base, spec, topology, repeated, noise)
    activation_status = resolve_activation_status(capture_count, reliability, confusion_risk)
    topology_pass = topology_passes(topology, noise)
    skeleton_pass_score = skeleton_pass_score_for(base, spec, topology, noise, observed_strokes, expected_strokes)
    risk_band = risk_band_for(confusion_risk, topology_pass, noise)

    return {
        "case_index": case_index,
        "case_local_index": local_index,
        "case_id": spec.case_id,
        "case_label": spec.label,
        "regex": spec.regex,
        "shapes": list(spec.shapes),
        "ops": list(spec.ops),
        "item_count": len(spec.shapes),
        "open_count": open_count,
        "closed_count": closed_count,
        "topology": topology,
        "relation": spec.relation,
        "primary_primitive": spec.primary_primitive,
        "role_profile": spec.role_profile,
        "risk_focus": spec.risk_focus,
        "noise_preset": noise_preset,
        "expected_strokes": expected_strokes,
        "observed_strokes": observed_strokes,
        "complexity": complexity,
        "repeated_shape_count": repeated,
        "capture_source": capture_source,
        "capture_reliability": reliability,
        "capture_count": capture_count,
        "activation_status": activation_status,
        "confusion_risk": confusion_risk,
        "topology_pass": topology_pass,
        "skeleton_pass_score": skeleton_pass_score,
        "risk_band": risk_band,
        **noise,
    }


def choose_noise_preset(base: Any, rng: random.Random, spec: CaseSpec, topology: str) -> str:
    weights = {
        "stable_baseline": 0.12,
        "personal_gt_residual": 0.14,
        "trace_offset": 0.1,
        "open_gap_stress": 0.1,
        "rotation_drift": 0.1,
        "jitter_noise_stress": 0.1,
        "fast_compression": 0.1,
        "closed_gap_boundary": 0.08,
        "composition_merge": 0.09,
        "tutorial_variation": 0.07,
    }
    if topology in {"closed_only", "mixed"}:
        weights["closed_gap_boundary"] *= 1.35
        weights["open_gap_stress"] *= 1.2
    if topology == "open_only":
        weights["fast_compression"] *= 1.25
        weights["composition_merge"] *= 1.15
    for preset, multiplier in spec.noise_bias.items():
        weights[preset] = weights.get(preset, 0.01) * multiplier
    return base.weighted_choice(rng, list(weights.items()))


def apply_case_noise_adjustments(base: Any, rng: random.Random, spec: CaseSpec, noise: dict[str, float], topology: str) -> None:
    if spec.relation in {"inside", "overlap", "crossing"}:
        noise["translation_px"] *= rng.uniform(0.88, 1.2)
        noise["curve_warp"] = min(noise["curve_warp"] * rng.uniform(1.04, 1.22), 0.42)
    if spec.relation == "sequence":
        noise["rotation_deg"] *= rng.uniform(1.1, 1.45)
    if spec.relation == "symmetric":
        noise["rotation_deg"] *= rng.uniform(0.85, 1.2)
        noise["scale"] *= rng.uniform(0.94, 1.08)
    if spec.role_profile in {"nested_frame", "overlapped_frame", "directional_frame"}:
        noise["open_gap_ratio"] *= 1.12 if topology in {"closed_only", "mixed"} else 1.0
    if spec.primary_primitive in {"rightArrow", "downArrow", "arrow"}:
        noise["rotation_deg"] *= rng.uniform(1.12, 1.55)
    noise["open_gap_ratio"] = base.clamp(noise["open_gap_ratio"], 0, 0.72)
    noise["rotation_deg"] = base.clamp(noise["rotation_deg"], -78, 78)
    noise["curve_warp"] = base.clamp(noise["curve_warp"], 0, 0.5)


def expected_stroke_count(shapes: tuple[str, ...], base: Any) -> float:
    stroke_weights = {
        "line": 0.9,
        "arrow": 1.15,
        "elbow": 1.0,
        "arc": 1.05,
        "curve": 1.0,
        "wave": 1.12,
        "braceL": 1.08,
        "braceR": 1.08,
        "rect": 1.35,
        "roundRect": 1.35,
        "ellipse": 1.25,
        "triangle": 1.25,
        "diamond": 1.25,
        "rightArrow": 1.45,
        "downArrow": 1.45,
    }
    return max(1.0, sum(stroke_weights.get(shape, 1.0) for shape in shapes))


def observed_stroke_count(rng: random.Random, spec: CaseSpec, noise_preset: str, expected: float) -> float:
    if noise_preset in {"fast_compression", "composition_merge"}:
        ratio = rng.uniform(0.42, 0.82)
    elif spec.relation in {"inside", "overlap", "crossing"} and noise_preset == "jitter_noise_stress":
        ratio = rng.uniform(0.92, 1.55)
    else:
        ratio = rng.uniform(0.78, 1.32)
    if spec.role_profile in {"repeated_main", "main_accent"} and noise_preset == "fast_compression":
        ratio *= rng.uniform(0.78, 0.95)
    return max(1.0, expected * ratio)


def choose_capture_source(base: Any, rng: random.Random, noise_preset: str) -> str:
    if noise_preset == "trace_offset":
        pairs = [("trace", 0.55), ("recall", 0.15), ("variation", 0.15), ("live", 0.15)]
    elif noise_preset in {"personal_gt_residual", "tutorial_variation"}:
        pairs = [("variation", 0.42), ("recall", 0.28), ("live", 0.18), ("trace", 0.12)]
    elif noise_preset in {"fast_compression", "composition_merge"}:
        pairs = [("live", 0.36), ("variation", 0.28), ("recall", 0.22), ("trace", 0.14)]
    else:
        pairs = [("live", 0.3), ("variation", 0.28), ("recall", 0.24), ("trace", 0.18)]
    return base.weighted_choice(rng, pairs)


def choose_reliability(
    base: Any,
    rng: random.Random,
    source: str,
    noise_preset: str,
    topology: str,
    noise: dict[str, float],
) -> str:
    risk = 0.0
    if noise_preset in {"jitter_noise_stress", "composition_merge", "open_gap_stress"}:
        risk += 0.18
    if topology in {"closed_only", "mixed"} and noise["open_gap_ratio"] > 0.24:
        risk += 0.18
    if noise["extra_noise_strokes"] >= 3:
        risk += 0.15
    if source == "trace":
        pairs = [("high", 0.22), ("medium", 0.45), ("unvalidated", 0.28), ("feedback_only", 0.05)]
    elif source == "variation":
        pairs = [("high", 0.42), ("medium", 0.36), ("unvalidated", 0.18), ("feedback_only", 0.04)]
    elif source == "recall":
        pairs = [("high", 0.34), ("medium", 0.42), ("unvalidated", 0.2), ("feedback_only", 0.04)]
    else:
        pairs = [("high", 0.28), ("medium", 0.34), ("unvalidated", 0.28), ("feedback_only", 0.1)]
    adjusted = []
    for value, weight in pairs:
        if value == "high":
            weight *= max(0.25, 1 - risk)
        if value in {"unvalidated", "feedback_only"}:
            weight *= 1 + risk
        adjusted.append((value, weight))
    return base.weighted_choice(rng, adjusted)


def draw_capture_count(rng: random.Random, source: str, reliability: str, local_index: int) -> int:
    if source == "trace":
        base_count = rng.randint(1, 10)
    elif source == "recall":
        base_count = rng.randint(2, 22)
    elif source == "variation":
        base_count = rng.randint(4, 38)
    else:
        base_count = rng.randint(0, 55)
    maturity_bonus = min(local_index // 12500, 3)
    reliability_bonus = 4 if reliability == "high" else 2 if reliability == "medium" else 0
    return max(0, base_count + maturity_bonus + reliability_bonus)


def estimate_confusion_risk(
    base: Any,
    spec: CaseSpec,
    topology: str,
    repeated: int,
    noise: dict[str, float],
) -> float:
    risk = spec.confusion_base
    risk += min(repeated * 0.04, 0.16)
    risk += 0.07 if topology == "mixed" else 0.05 if topology == "closed_only" else 0.02
    risk += min(noise["extra_noise_strokes"] * 0.018, 0.11)
    risk += max(noise["open_gap_ratio"] - 0.2, 0) * (0.42 if topology in {"closed_only", "mixed"} else 0.12)
    risk += 0.04 if abs(noise["rotation_deg"]) > 36 and spec.relation == "sequence" else 0.0
    risk += 0.05 if spec.relation in {"inside", "overlap", "crossing"} and noise["curve_warp"] > 0.18 else 0.0
    return base.clamp(risk, 0.02, 0.88)


def resolve_activation_status(capture_count: int, reliability: str, confusion_risk: float) -> str:
    if capture_count < 3:
        return "shape_definition"
    if reliability == "feedback_only":
        return "metadata"
    if confusion_risk > 0.48 and capture_count < 20:
        return "shadow_recognizer"
    if capture_count >= 12 and reliability in {"high", "medium"} and confusion_risk <= 0.56:
        return "active_recognizer"
    return "shadow_recognizer"


def topology_passes(topology: str, noise: dict[str, float]) -> bool:
    if topology == "open_only":
        return not (noise["extra_noise_strokes"] >= 5 and noise["jitter_px"] > 20)
    if topology == "closed_only":
        return noise["open_gap_ratio"] <= 0.26 and noise["extra_noise_strokes"] < 4
    if topology == "mixed":
        return noise["open_gap_ratio"] <= 0.22 and noise["extra_noise_strokes"] < 4
    return False


def skeleton_pass_score_for(
    base: Any,
    spec: CaseSpec,
    topology: str,
    noise: dict[str, float],
    observed_strokes: float,
    expected_strokes: float,
) -> float:
    stroke_ratio = base.safe_div(observed_strokes, expected_strokes)
    penalty = 0.0
    penalty += min(abs(stroke_ratio - 1), 1) * (0.18 if spec.relation in {"sequence", "parallel"} else 0.12)
    penalty += min(noise["jitter_px"] / 32, 1) * 0.12
    penalty += min(noise["extra_noise_strokes"] / 5, 1) * 0.14
    penalty += min(abs(noise["rotation_deg"]) / 70, 1) * (0.16 if spec.primary_primitive in {"arrow", "rightArrow", "downArrow"} else 0.08)
    if topology in {"closed_only", "mixed"}:
        penalty += min(noise["open_gap_ratio"] / 0.45, 1) * 0.22
    if spec.relation in {"inside", "overlap", "crossing"}:
        penalty += min(noise["curve_warp"] / 0.4, 1) * 0.08
    return base.clamp(0.95 - penalty, 0.02, 0.99)


def risk_band_for(confusion_risk: float, topology_pass: bool, noise: dict[str, float]) -> str:
    if not topology_pass:
        return "topology_block"
    if confusion_risk >= 0.56 or noise["extra_noise_strokes"] >= 4:
        return "high"
    if confusion_risk >= 0.36 or noise["jitter_px"] >= 14:
        return "medium"
    return "low"


def segmented_guarded_result(base: Any, case: dict[str, Any], policy_results: dict[str, dict[str, float | bool]]) -> dict[str, float | bool]:
    baseline = policy_results["baseline"]
    regex = policy_results["regex_gt"]
    tutorial = policy_results["tutorial_bg"]
    score = base.clamp(
        float(regex["score"]) * 0.35
        + float(tutorial["score"]) * 0.55
        + case["skeleton_pass_score"] * 0.1,
        0,
        1,
    )
    base_threshold = float(baseline["threshold"])
    regex_bias = 0.008 if case["skeleton_pass_score"] >= 0.62 else 0.003
    topology_cap = topology_total_cap(case["topology"])
    tutorial_cap = topology_tutorial_cap(case["topology"])
    source_weight = SOURCE_WEIGHTS[case["capture_source"]]
    reliability_weight = RELIABILITY_WEIGHTS[case["capture_reliability"]]
    maturity = base.clamp((case["capture_count"] - 4) / 18, 0, 1)
    activation_weight = {
        "metadata": 0.0,
        "shape_definition": 0.12,
        "shadow_recognizer": 0.45,
        "active_recognizer": 1.0,
    }[case["activation_status"]]
    tutorial_bias = tutorial_cap * source_weight * reliability_weight * maturity * activation_weight
    if case["capture_source"] == "trace":
        tutorial_bias = min(tutorial_bias, 0.006)
    if case["risk_band"] == "high":
        tutorial_bias *= 0.45
    if case["risk_band"] == "topology_block":
        tutorial_bias = 0.0
    actual_bias = min(regex_bias + tutorial_bias, topology_cap)
    threshold = base_threshold - actual_bias
    threshold = base.guardrailed_threshold(case, threshold)
    hard_pass = case["topology_pass"] and case["confusion_risk"] <= 0.62 and case["skeleton_pass_score"] >= 0.48
    if case["topology"] == "mixed" and case["open_gap_ratio"] > 0.22:
        hard_pass = False
    if case["topology"] == "closed_only" and case["open_gap_ratio"] > 0.26:
        hard_pass = False
    accepted = bool(score >= threshold and hard_pass)
    ambiguity = base.ambiguity_risk(case)
    unsafe = base.unsafe_probability_for(case, score, threshold, ambiguity, "tutorial_bg", 0.0)
    if not hard_pass:
        unsafe = min(unsafe * 0.45, 0.12)
    return {
        "score": score,
        "threshold": threshold,
        "threshold_bias": actual_bias,
        "accepted": accepted,
        "valid_probability": float(tutorial["valid_probability"]),
        "unsafe_probability": unsafe,
        "priority_flip_probability": 0.0,
        "correction_gain": score - float(baseline["score"]),
    }


def topology_total_cap(topology: str) -> float:
    return {"open_only": 0.035, "closed_only": 0.018, "mixed": 0.016}.get(topology, 0.012)


def topology_tutorial_cap(topology: str) -> float:
    return {"open_only": 0.034, "closed_only": 0.016, "mixed": 0.014}.get(topology, 0.01)


def relation_complexity_for(relation: str) -> float:
    return {
        "none": 0.0,
        "frame": 0.04,
        "beside": 0.06,
        "parallel": 0.08,
        "sequence": 0.1,
        "attached": 0.13,
        "crossing": 0.15,
        "inside": 0.16,
        "overlap": 0.18,
        "symmetric": 0.14,
    }.get(relation, 0.1)


class RiskAggregate:
    def __init__(self) -> None:
        self.cases = 0
        self.topology_pass = 0
        self.active = 0
        self.high_or_medium_validation = 0
        self.avg_confusion = 0.0
        self.segmented_accept = 0
        self.tinyml_accept = 0
        self.tinyml_flip = 0.0

    def add(self, case: dict[str, Any], policy_results: dict[str, dict[str, float | bool]]) -> None:
        self.cases += 1
        self.topology_pass += 1 if case["topology_pass"] else 0
        self.active += 1 if case["activation_status"] == "active_recognizer" else 0
        self.high_or_medium_validation += 1 if case["capture_reliability"] in {"high", "medium"} else 0
        self.avg_confusion += case["confusion_risk"]
        self.segmented_accept += 1 if policy_results["segmented_guarded"]["accepted"] else 0
        self.tinyml_accept += 1 if policy_results["tinyml_priority"]["accepted"] else 0
        self.tinyml_flip += float(policy_results["tinyml_priority"]["priority_flip_probability"])

    def row(self, case_id: str, risk_band: str) -> dict[str, Any]:
        return {
            "case_id": case_id,
            "risk_band": risk_band,
            "cases": self.cases,
            "topology_pass_rate": round6(self.topology_pass / self.cases if self.cases else 0),
            "active_rate": round6(self.active / self.cases if self.cases else 0),
            "validated_rate": round6(self.high_or_medium_validation / self.cases if self.cases else 0),
            "avg_confusion_risk": round6(self.avg_confusion / self.cases if self.cases else 0),
            "segmented_accept_rate": round6(self.segmented_accept / self.cases if self.cases else 0),
            "tinyml_accept_rate": round6(self.tinyml_accept / self.cases if self.cases else 0),
            "tinyml_priority_flip_rate": round6(self.tinyml_flip / self.cases if self.cases else 0),
        }


def add_feature_values(target: dict[str, list[float]], case: dict[str, Any]) -> None:
    stroke_ratio = safe_div(case["observed_strokes"], case["expected_strokes"])
    target["jitter_px"].append(case["jitter_px"])
    target["open_gap_ratio"].append(case["open_gap_ratio"])
    target["rotation_abs_deg"].append(abs(case["rotation_deg"]))
    target["curve_warp"].append(case["curve_warp"])
    target["extra_noise_strokes"].append(case["extra_noise_strokes"])
    target["translation_px"].append(case["translation_px"])
    target["scale_delta"].append(abs(case["scale"] - 1))
    target["stroke_ratio"].append(stroke_ratio)
    target["one_point_rate"].append(case["one_point_rate"])
    target["duration_ms"].append(case["duration_ms"])
    target["confusion_risk"].append(case["confusion_risk"])
    target["skeleton_pass_score"].append(case["skeleton_pass_score"])


def feature_distribution_rows(feature_values: dict[str, dict[str, list[float]]]) -> list[dict[str, Any]]:
    rows: list[dict[str, Any]] = []
    for case_id, features in sorted(feature_values.items()):
        for feature, values in sorted(features.items()):
            rows.append(
                {
                    "case_id": case_id,
                    "feature": feature,
                    "n": len(values),
                    "mean": round6(statistics.mean(values) if values else 0),
                    "sd": round6(statistics.stdev(values) if len(values) > 1 else 0),
                    "p05": round6(percentile(values, 0.05)),
                    "p25": round6(percentile(values, 0.25)),
                    "p50": round6(percentile(values, 0.5)),
                    "p75": round6(percentile(values, 0.75)),
                    "p90": round6(percentile(values, 0.9)),
                    "p95": round6(percentile(values, 0.95)),
                }
            )
    return rows


def sample_row(base: Any, case: dict[str, Any], policy_results: dict[str, dict[str, float | bool]]) -> dict[str, Any]:
    row = {
        "case_index": case["case_index"],
        "case_id": case["case_id"],
        "regex": case["regex"],
        "topology": case["topology"],
        "relation": case["relation"],
        "primary_primitive": case["primary_primitive"],
        "noise_preset": case["noise_preset"],
        "capture_source": case["capture_source"],
        "capture_reliability": case["capture_reliability"],
        "capture_count": case["capture_count"],
        "activation_status": case["activation_status"],
        "risk_band": case["risk_band"],
        "topology_pass": case["topology_pass"],
        "confusion_risk": round6(case["confusion_risk"]),
        "skeleton_pass_score": round6(case["skeleton_pass_score"]),
        "jitter_px": round6(case["jitter_px"]),
        "open_gap_ratio": round6(case["open_gap_ratio"]),
        "rotation_deg": round6(case["rotation_deg"]),
        "curve_warp": round6(case["curve_warp"]),
        "translation_px": round6(case["translation_px"]),
        "scale": round6(case["scale"]),
        "stroke_ratio": round6(base.safe_div(case["observed_strokes"], case["expected_strokes"])),
    }
    for policy in POLICIES:
        result = policy_results[policy]
        row[f"{policy}_score"] = round6(float(result["score"]))
        row[f"{policy}_threshold"] = round6(float(result["threshold"]))
        row[f"{policy}_accepted"] = result["accepted"]
        row[f"{policy}_unsafe_probability"] = round6(float(result["unsafe_probability"]))
    return row


def write_outputs(
    base: Any,
    out_dir: Path,
    args: argparse.Namespace,
    observed: dict[str, Any],
    dashboard_noise: dict[str, Any],
    global_agg: dict[str, Any],
    case_agg: dict[tuple[str, str], Any],
    case_noise_agg: dict[tuple[str, str, str], Any],
    topology_agg: dict[tuple[str, str], Any],
    relation_agg: dict[tuple[str, str], Any],
    source_agg: dict[tuple[str, str], Any],
    activation_agg: dict[tuple[str, str], Any],
    primitive_agg: dict[tuple[str, str], Any],
    risk_gate_agg: dict[tuple[str, str], RiskAggregate],
    feature_values: dict[str, dict[str, list[float]]],
    sample_rows: list[dict[str, Any]],
) -> None:
    global_rows = [global_agg[policy].row({"policy": policy}) for policy in POLICIES]
    case_rows = [
        agg.row({"case_id": case_id, "policy": policy})
        for (case_id, policy), agg in sorted(case_agg.items())
    ]
    case_noise_rows = [
        agg.row({"case_id": case_id, "noise_preset": noise, "policy": policy})
        for (case_id, noise, policy), agg in sorted(case_noise_agg.items())
    ]
    topology_rows = [
        agg.row({"topology": topology, "policy": policy})
        for (topology, policy), agg in sorted(topology_agg.items())
    ]
    relation_rows = [
        agg.row({"relation": relation, "policy": policy})
        for (relation, policy), agg in sorted(relation_agg.items())
    ]
    source_rows = [
        agg.row({"capture_source": source, "policy": policy})
        for (source, policy), agg in sorted(source_agg.items())
    ]
    activation_rows = [
        agg.row({"activation_status": status, "policy": policy})
        for (status, policy), agg in sorted(activation_agg.items())
    ]
    primitive_rows = [
        agg.row({"primary_primitive": primitive, "policy": policy})
        for (primitive, policy), agg in sorted(primitive_agg.items())
    ]
    risk_rows = [
        agg.row(case_id, risk_band)
        for (case_id, risk_band), agg in sorted(risk_gate_agg.items())
    ]
    feature_rows = feature_distribution_rows(feature_values)

    write_csv(out_dir / "global_policy_summary.csv", global_rows)
    write_csv(out_dir / "case_policy_summary.csv", case_rows)
    write_csv(out_dir / "case_noise_policy_summary.csv", case_noise_rows)
    write_csv(out_dir / "topology_policy_summary.csv", topology_rows)
    write_csv(out_dir / "relation_policy_summary.csv", relation_rows)
    write_csv(out_dir / "capture_source_policy_summary.csv", source_rows)
    write_csv(out_dir / "activation_policy_summary.csv", activation_rows)
    write_csv(out_dir / "primary_primitive_policy_summary.csv", primitive_rows)
    write_csv(out_dir / "risk_gate_summary.csv", risk_rows)
    write_csv(out_dir / "case_feature_distribution.csv", feature_rows)
    write_csv(out_dir / "sample_cases.csv", sample_rows)
    write_json(
        out_dir / "case_specs.json",
        [
            {
                "caseId": spec.case_id,
                "label": spec.label,
                "regex": spec.regex,
                "shapes": list(spec.shapes),
                "ops": list(spec.ops),
                "relation": spec.relation,
                "primaryPrimitive": spec.primary_primitive,
                "roleProfile": spec.role_profile,
                "riskFocus": spec.risk_focus,
                "confusionBase": spec.confusion_base,
                "noiseBias": spec.noise_bias,
            }
            for spec in CASE_SPECS
        ],
    )
    write_json(
        out_dir / "analysis_summary.json",
        {
            "generatedAt": datetime.now(timezone.utc).isoformat(),
            "seed": args.seed,
            "caseCount": len(CASE_SPECS),
            "casesPerSpec": args.cases_per_spec,
            "totalCases": len(CASE_SPECS) * args.cases_per_spec,
            "policies": POLICIES,
            "observedShapeSurvey": observed,
            "dashboardNoisePrior": dashboard_noise,
            "caseIds": [spec.case_id for spec in CASE_SPECS],
            "outputFiles": [
                "analysis_summary.json",
                "case_specs.json",
                "global_policy_summary.csv",
                "case_policy_summary.csv",
                "case_noise_policy_summary.csv",
                "topology_policy_summary.csv",
                "relation_policy_summary.csv",
                "capture_source_policy_summary.csv",
                "activation_policy_summary.csv",
                "primary_primitive_policy_summary.csv",
                "risk_gate_summary.csv",
                "case_feature_distribution.csv",
                "sample_cases.csv",
                "analysis_report.md",
            ],
        },
    )
    write_report(out_dir, global_rows, case_rows, topology_rows, relation_rows, source_rows, risk_rows, args)


def write_report(
    out_dir: Path,
    global_rows: list[dict[str, Any]],
    case_rows: list[dict[str, Any]],
    topology_rows: list[dict[str, Any]],
    relation_rows: list[dict[str, Any]],
    source_rows: list[dict[str, Any]],
    risk_rows: list[dict[str, Any]],
    args: argparse.Namespace,
) -> None:
    by_policy = {row["policy"]: row for row in global_rows}
    segmented = by_policy["segmented_guarded"]
    tinyml = by_policy["tinyml_priority"]
    tutorial = by_policy["tutorial_bg"]
    baseline = by_policy["baseline"]
    top_segmented_cases = sorted(
        [row for row in case_rows if row["policy"] == "segmented_guarded"],
        key=lambda row: float(row["expected_recall"]),
        reverse=True,
    )[:8]
    risky_segmented_cases = sorted(
        [row for row in case_rows if row["policy"] == "segmented_guarded"],
        key=lambda row: float(row["expected_unsafe_accept_rate"]),
        reverse=True,
    )[:8]
    topology_segmented = [row for row in topology_rows if row["policy"] == "segmented_guarded"]
    relation_segmented = sorted(
        [row for row in relation_rows if row["policy"] == "segmented_guarded"],
        key=lambda row: float(row["expected_unsafe_accept_rate"]),
        reverse=True,
    )
    source_segmented = [row for row in source_rows if row["policy"] == "segmented_guarded"]
    risk_topology_blocks = sorted(
        [row for row in risk_rows if row["risk_band"] == "topology_block"],
        key=lambda row: float(row["segmented_accept_rate"]),
        reverse=True,
    )[:8]

    lines = [
        "# User-Defined Shape Case Noise Analysis",
        "",
        f"- Case count: `{len(CASE_SPECS)}`.",
        f"- Generated per case: `{args.cases_per_spec}`.",
        f"- Total generated cases: `{len(CASE_SPECS) * args.cases_per_spec}`.",
        f"- Seed: `{args.seed}`.",
        "",
        "## Global policy summary",
        "",
        "| policy | accept_rate | precision | recall | unsafe_accept | threshold_bias | priority_flip |",
        "|---|---:|---:|---:|---:|---:|---:|",
    ]
    for row in global_rows:
        lines.append(
            "| {policy} | {accept_rate} | {expected_precision} | {expected_recall} | "
            "{expected_unsafe_accept_rate} | {avg_threshold_bias} | {priority_flip_rate} |".format(**row)
        )
    lines.extend(
        [
            "",
            "## Key interpretation",
            "",
            f"- `segmented_guarded` improved recall from `{baseline['expected_recall']}` to `{segmented['expected_recall']}` while keeping unsafe accept at `{segmented['expected_unsafe_accept_rate']}`.",
            f"- `tutorial_bg` recall was `{tutorial['expected_recall']}`, but `segmented_guarded` trades part of that recall for topology and relation safety.",
            f"- `tinyml_priority` recall was `{tinyml['expected_recall']}` with priority flip `{tinyml['priority_flip_rate']}`; it should remain shadow-first unless holdout precision passes.",
            "",
            "## Top segmented cases by recall",
            "",
            "| case_id | accept_rate | precision | recall | unsafe_accept |",
            "|---|---:|---:|---:|---:|",
        ]
    )
    for row in top_segmented_cases:
        lines.append(
            "| {case_id} | {accept_rate} | {expected_precision} | {expected_recall} | {expected_unsafe_accept_rate} |".format(
                **row
            )
        )
    lines.extend(
        [
            "",
            "## Highest segmented unsafe cases",
            "",
            "| case_id | accept_rate | precision | recall | unsafe_accept |",
            "|---|---:|---:|---:|---:|",
        ]
    )
    for row in risky_segmented_cases:
        lines.append(
            "| {case_id} | {accept_rate} | {expected_precision} | {expected_recall} | {expected_unsafe_accept_rate} |".format(
                **row
            )
        )
    lines.extend(
        [
            "",
            "## Topology summary for segmented_guarded",
            "",
            "| topology | accept_rate | precision | recall | unsafe_accept | threshold_bias |",
            "|---|---:|---:|---:|---:|---:|",
        ]
    )
    for row in topology_segmented:
        lines.append(
            "| {topology} | {accept_rate} | {expected_precision} | {expected_recall} | {expected_unsafe_accept_rate} | {avg_threshold_bias} |".format(
                **row
            )
        )
    lines.extend(
        [
            "",
            "## Relation risk summary for segmented_guarded",
            "",
            "| relation | accept_rate | precision | recall | unsafe_accept |",
            "|---|---:|---:|---:|---:|",
        ]
    )
    for row in relation_segmented:
        lines.append(
            "| {relation} | {accept_rate} | {expected_precision} | {expected_recall} | {expected_unsafe_accept_rate} |".format(
                **row
            )
        )
    lines.extend(
        [
            "",
            "## Capture source summary for segmented_guarded",
            "",
            "| source | accept_rate | precision | recall | unsafe_accept | threshold_bias |",
            "|---|---:|---:|---:|---:|---:|",
        ]
    )
    for row in source_segmented:
        lines.append(
            "| {capture_source} | {accept_rate} | {expected_precision} | {expected_recall} | {expected_unsafe_accept_rate} | {avg_threshold_bias} |".format(
                **row
            )
        )
    lines.extend(
        [
            "",
            "## Topology-block audit",
            "",
            "| case_id | topology_pass_rate | active_rate | validated_rate | segmented_accept_rate | tinyml_accept_rate | tinyml_flip |",
            "|---|---:|---:|---:|---:|---:|---:|",
        ]
    )
    for row in risk_topology_blocks:
        lines.append(
            "| {case_id} | {topology_pass_rate} | {active_rate} | {validated_rate} | {segmented_accept_rate} | {tinyml_accept_rate} | {tinyml_priority_flip_rate} |".format(
                **row
            )
        )
    lines.extend(
        [
            "",
            "## Output files",
            "",
            "- `analysis_summary.json`",
            "- `case_specs.json`",
            "- `global_policy_summary.csv`",
            "- `case_policy_summary.csv`",
            "- `case_noise_policy_summary.csv`",
            "- `topology_policy_summary.csv`",
            "- `relation_policy_summary.csv`",
            "- `capture_source_policy_summary.csv`",
            "- `activation_policy_summary.csv`",
            "- `primary_primitive_policy_summary.csv`",
            "- `risk_gate_summary.csv`",
            "- `case_feature_distribution.csv`",
            "- `sample_cases.csv`",
        ]
    )
    (out_dir / "analysis_report.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


def write_csv(path: Path, rows: list[dict[str, Any]]) -> None:
    if not rows:
        path.write_text("", encoding="utf-8")
        return
    with path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(rows[0].keys()))
        writer.writeheader()
        writer.writerows(rows)


def write_json(path: Path, payload: Any) -> None:
    path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def percentile(values: list[float], quantile: float) -> float:
    if not values:
        return 0.0
    ordered = sorted(values)
    index = (len(ordered) - 1) * quantile
    lower = math.floor(index)
    upper = math.ceil(index)
    if lower == upper:
        return ordered[int(index)]
    return ordered[lower] * (upper - index) + ordered[upper] * (index - lower)


def safe_div(numerator: float, denominator: float) -> float:
    return numerator / denominator if denominator else 0.0


def round6(value: float) -> float:
    return round(float(value), 6)


if __name__ == "__main__":
    main()
