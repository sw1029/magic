from __future__ import annotations

import json
import sys
from pathlib import Path

import matplotlib.pyplot as plt
import numpy as np
import pandas as pd


STATUS_ORDER = ["recognized", "ambiguous", "incomplete", "invalid"]
POLICY_ORDER = ["baseline", "tutorial_warmup", "ml_first", "stat_guardrail", "ml_guardrail_final", "ml_guardrail_dynamic"]


def main() -> None:
    if len(sys.argv) < 2:
        raise SystemExit("usage: python scripts/survey-guardrail-ml-plots.py <experiment-output-dir>")

    out_dir = Path(sys.argv[1]).resolve()
    fig_dir = out_dir / "figures"
    fig_dir.mkdir(parents=True, exist_ok=True)

    base_summary = read_json(out_dir / "analysis_summary.json")
    cases = pd.read_csv(out_dir / "experiment_cases.csv", low_memory=False)
    decisions = pd.read_csv(out_dir / "policy_decisions.csv")
    warmup = pd.read_csv(out_dir / "warmup_decisions.csv")
    survey_actual = pd.read_csv(out_dir / "survey_actual_policy_decisions.csv")

    policy_summary = build_policy_summary(decisions)
    warmup_summary = build_warmup_summary(warmup)
    feature_risk = build_feature_bin_risk(cases, decisions)
    confusion_pairs = build_confusion_pairs(decisions)
    calibration = build_calibration(decisions)
    guardrail_reasons = build_guardrail_reason_summary(decisions)
    family_policy_risk = build_family_policy_risk(decisions)
    source_policy_risk = build_source_policy_risk(decisions)
    warmup_family = build_warmup_family_summary(warmup)
    source_calibration = build_calibration_by_source(decisions)
    decision_reasons = build_decision_reason_summary(decisions)
    threshold_bias = build_threshold_bias_summary(decisions)
    ml_to_final = build_ml_to_final_transition(decisions)
    calibration_cells = build_dynamic_calibration_cells(cases, decisions)
    calibration_fallbacks = build_dynamic_calibration_fallbacks(decisions)
    calibration_diagnostics = build_dynamic_calibration_diagnostics(decisions)
    threshold_sweep = build_dynamic_threshold_sweep(decisions)

    policy_summary.to_csv(out_dir / "policy_summary.csv", index=False)
    warmup_summary.to_csv(out_dir / "warmup_summary.csv", index=False)
    feature_risk.to_csv(out_dir / "feature_bin_risk.csv", index=False)
    confusion_pairs.to_csv(out_dir / "confusion_pairs.csv", index=False)
    calibration.to_csv(out_dir / "ml_calibration_bins.csv", index=False)
    guardrail_reasons.to_csv(out_dir / "guardrail_reason_summary.csv", index=False)
    family_policy_risk.to_csv(out_dir / "family_policy_risk.csv", index=False)
    source_policy_risk.to_csv(out_dir / "source_policy_risk.csv", index=False)
    warmup_family.to_csv(out_dir / "warmup_family_summary.csv", index=False)
    source_calibration.to_csv(out_dir / "ml_calibration_by_source.csv", index=False)
    decision_reasons.to_csv(out_dir / "decision_reason_summary.csv", index=False)
    threshold_bias.to_csv(out_dir / "threshold_bias_summary.csv", index=False)
    ml_to_final.to_csv(out_dir / "ml_to_final_transition.csv", index=False)
    calibration_cells.to_csv(out_dir / "calibration_cells.csv", index=False)
    calibration_fallbacks.to_csv(out_dir / "calibration_fallbacks.csv", index=False)
    calibration_diagnostics.to_csv(out_dir / "calibration_diagnostics.csv", index=False)
    threshold_sweep.to_csv(out_dir / "dynamic_threshold_sweep.csv", index=False)

    figures = {
        "policy_lift_risk": plot_policy_lift_risk(policy_summary, fig_dir),
        "warmup_curve": plot_warmup_curve(warmup_summary, fig_dir),
        "feature_risk_heatmap": plot_feature_risk_heatmap(feature_risk, fig_dir),
        "ml_calibration": plot_ml_calibration(calibration, fig_dir),
        "transition_heatmap": plot_transition_heatmap(decisions, fig_dir),
        "confusion_pairs": plot_confusion_pairs(confusion_pairs, fig_dir),
        "source_policy_status": plot_source_policy_status(decisions, fig_dir),
        "survey_actual_policy": plot_survey_actual_policy(survey_actual, fig_dir),
        "guardrail_reason_frequency": plot_guardrail_reason_frequency(guardrail_reasons, fig_dir),
        "policy_score_gap_top_score": plot_policy_score_gap_top_score(decisions, fig_dir),
        "unsafe_feature_distributions": plot_unsafe_feature_distributions(cases, decisions, fig_dir),
        "family_policy_matrix": plot_family_policy_matrix(family_policy_risk, fig_dir),
        "source_policy_risk_matrix": plot_source_policy_risk_matrix(source_policy_risk, fig_dir),
        "warmup_family_curve": plot_warmup_family_curve(warmup_family, fig_dir),
        "calibration_by_source": plot_calibration_by_source(source_calibration, fig_dir),
        "decision_reason_outcomes": plot_decision_reason_outcomes(decision_reasons, fig_dir),
        "threshold_bias_effect": plot_threshold_bias_effect(threshold_bias, fig_dir),
        "ml_to_final_transition": plot_ml_to_final_transition(ml_to_final, fig_dir),
        "dynamic_policy_lift_risk": plot_dynamic_policy_lift_risk(policy_summary, fig_dir),
        "family_source_threshold_heatmap": plot_family_source_threshold_heatmap(calibration_fallbacks, fig_dir),
        "calibration_reliability_by_family": plot_calibration_reliability_by_family(calibration_diagnostics, fig_dir),
        "survey_mutation_outlier_effect": plot_survey_mutation_outlier_effect(decisions, fig_dir),
        "feature_risk_surface": plot_feature_risk_surface(cases, decisions, fig_dir),
        "pareto_recognition_vs_unsafe": plot_pareto_recognition_vs_unsafe(threshold_sweep, fig_dir),
        "actual_survey_valid_before_after": plot_actual_survey_valid_before_after(survey_actual, fig_dir),
        "dynamic_confusion_matrix": plot_dynamic_confusion_matrix(decisions, fig_dir),
    }

    summary = {
        **base_summary,
        "policySummary": policy_summary.to_dict(orient="records"),
        "warmupSummary": warmup_summary.to_dict(orient="records"),
        "mlCalibration": {
            "ece": expected_calibration_error(calibration),
            "brier": brier_score(decisions[decisions["policy"] == "ml_first"]),
        },
        "outputFiles": {
            "policySummary": "policy_summary.csv",
            "warmupSummary": "warmup_summary.csv",
            "featureBinRisk": "feature_bin_risk.csv",
            "confusionPairs": "confusion_pairs.csv",
            "calibration": "ml_calibration_bins.csv",
            "guardrailReasons": "guardrail_reason_summary.csv",
            "familyPolicyRisk": "family_policy_risk.csv",
            "sourcePolicyRisk": "source_policy_risk.csv",
            "warmupFamily": "warmup_family_summary.csv",
            "sourceCalibration": "ml_calibration_by_source.csv",
            "decisionReasons": "decision_reason_summary.csv",
            "thresholdBias": "threshold_bias_summary.csv",
            "mlToFinalTransition": "ml_to_final_transition.csv",
            "calibrationCells": "calibration_cells.csv",
            "calibrationFallbacks": "calibration_fallbacks.csv",
            "calibrationDiagnostics": "calibration_diagnostics.csv",
            "dynamicThresholdSweep": "dynamic_threshold_sweep.csv",
            "dynamicPolicyThresholds": "dynamic_policy_thresholds.json",
        },
        "figures": {name: str(Path(path).relative_to(out_dir).as_posix()) for name, path in figures.items()},
    }
    write_json(out_dir / "analysis_summary.json", summary)
    write_report(out_dir, summary, policy_summary, warmup_summary, feature_risk, calibration, figures)
    validate_figures(figures)
    print(json.dumps({"out_dir": str(out_dir), "cases": len(cases), "policy_rows": len(decisions), "figures": len(figures)}, ensure_ascii=False))


def build_policy_summary(decisions: pd.DataFrame) -> pd.DataFrame:
    baseline = decisions[decisions["policy"] == "baseline"][["case_id", "status", "accepted_family", "is_correct_accept"]].rename(
        columns={"status": "baseline_status", "accepted_family": "baseline_accepted_family", "is_correct_accept": "baseline_correct_accept"}
    )
    merged = decisions.merge(baseline, on="case_id", how="left")
    rows = []
    baseline_recognized = (baseline["baseline_status"] == "recognized").sum()
    for policy in POLICY_ORDER:
        group = merged[merged["policy"] == policy]
        if group.empty:
            continue
        accepted = group[group["status"] == "recognized"]
        safe = group[group["is_correct_accept"] == True]
        unsafe = group[group["is_unsafe_accept"] == True]
        reject_saved = group[(group["baseline_status"] != "recognized") & (group["is_correct_accept"] == True)]
        rows.append(
            {
                "policy": policy,
                "n": len(group),
                "recognized": int((group["status"] == "recognized").sum()),
                "recognized_rate": ratio((group["status"] == "recognized").sum(), len(group)),
                "ambiguous_rate": ratio((group["status"] == "ambiguous").sum(), len(group)),
                "incomplete_rate": ratio((group["status"] == "incomplete").sum(), len(group)),
                "invalid_rate": ratio((group["status"] == "invalid").sum(), len(group)),
                "safe_accepts": len(safe),
                "unsafe_accepts": len(unsafe),
                "safe_accept_rate": ratio(len(safe), max(len(accepted), 1)),
                "unsafe_accept_rate": ratio(len(unsafe), max(len(accepted), 1)),
                "confusion_rate": ratio(len(unsafe), len(group)),
                "reject_saved_count": len(reject_saved),
                "reject_saved_rate": ratio(len(reject_saved), max((baseline["baseline_status"] != "recognized").sum(), 1)),
                "net_recognition_lift": int((group["status"] == "recognized").sum() - baseline_recognized),
                "changed_from_baseline": int((group["changed_from_baseline"] == True).sum()),
                "avg_confidence": group["confidence"].mean(),
                "avg_ml_confidence_gate": group["ml_confidence_gate"].mean(),
            }
        )
    return pd.DataFrame(rows)


def build_warmup_summary(warmup: pd.DataFrame) -> pd.DataFrame:
    if warmup.empty:
        return pd.DataFrame()
    rows = []
    for stage, group in warmup.groupby("warmup_stage", sort=False):
        accepted = group[group["status"] == "recognized"]
        rows.append(
            {
                "warmup_stage": stage,
                "profile_capture_count": group["profile_capture_count"].max(),
                "n": len(group),
                "recognized_rate": ratio((group["status"] == "recognized").sum(), len(group)),
                "safe_accept_rate": ratio((group["is_correct_accept"] == True).sum(), max(len(accepted), 1)),
                "unsafe_accept_rate": ratio((group["is_unsafe_accept"] == True).sum(), max(len(accepted), 1)),
                "changed_from_baseline_rate": ratio((group["changed_from_baseline"] == True).sum(), len(group)),
                "avg_effective_threshold_bias": group["effective_threshold_bias"].mean(),
                "avg_ml_confidence_gate": group["ml_confidence_gate"].mean(),
                "avg_confidence": group["confidence"].mean(),
            }
        )
    return pd.DataFrame(rows)


def build_feature_bin_risk(cases: pd.DataFrame, decisions: pd.DataFrame) -> pd.DataFrame:
    final = decisions[decisions["policy"] == "ml_guardrail_final"][
        ["case_id", "status", "is_correct_accept", "is_unsafe_accept"]
    ].rename(columns={"status": "final_status", "is_correct_accept": "final_correct_accept", "is_unsafe_accept": "final_unsafe_accept"})
    baseline = decisions[decisions["policy"] == "baseline"][
        ["case_id", "status", "is_correct_accept", "is_unsafe_accept"]
    ].rename(columns={"status": "baseline_status", "is_correct_accept": "baseline_correct_accept", "is_unsafe_accept": "baseline_unsafe_accept"})
    df = cases.merge(baseline, on="case_id").merge(final, on="case_id")
    features = ["top_score_proxy", "score_gap_proxy", "closure", "open_gap_ratio", "jitter_px", "extra_noise_stroke_count", "rotation_bias"]
    df["top_score_proxy"] = decisions[decisions["policy"] == "baseline"].set_index("case_id").loc[df["case_id"], "top_score"].to_numpy()
    df["score_gap_proxy"] = decisions[decisions["policy"] == "baseline"].set_index("case_id").loc[df["case_id"], "score_gap"].to_numpy()
    rows = []
    for feature in features:
        series = df[feature].astype(float)
        try:
            bins = pd.qcut(series, q=10, duplicates="drop")
        except ValueError:
            bins = pd.cut(series, bins=5, duplicates="drop")
        for bucket, group in df.groupby(bins, observed=True):
            rows.append(
                {
                    "feature": feature,
                    "bucket": str(bucket),
                    "n": len(group),
                    "baseline_failure_rate": ratio((group["baseline_status"] != "recognized").sum(), len(group)),
                    "baseline_unsafe_rate": ratio((group["baseline_unsafe_accept"] == True).sum(), len(group)),
                    "final_failure_rate": ratio((group["final_status"] != "recognized").sum(), len(group)),
                    "final_unsafe_rate": ratio((group["final_unsafe_accept"] == True).sum(), len(group)),
                    "final_safe_accept_rate": ratio((group["final_correct_accept"] == True).sum(), len(group)),
                }
            )
    return pd.DataFrame(rows)


def build_confusion_pairs(decisions: pd.DataFrame) -> pd.DataFrame:
    accepted = decisions[decisions["status"] == "recognized"].copy()
    if accepted.empty:
        return pd.DataFrame(columns=["policy", "expected_family", "accepted_family", "n", "rate"])
    totals = accepted.groupby(["policy", "expected_family"]).size().rename("total")
    rows = accepted.groupby(["policy", "expected_family", "accepted_family"]).size().rename("n").reset_index()
    rows = rows.merge(totals.reset_index(), on=["policy", "expected_family"], how="left")
    rows["rate"] = rows["n"] / rows["total"].clip(lower=1)
    return rows.sort_values(["policy", "n"], ascending=[True, False])


def build_calibration(decisions: pd.DataFrame) -> pd.DataFrame:
    ml = decisions[decisions["policy"] == "ml_first"].copy()
    ml["correct_top"] = ml["decision_family"] == ml["expected_family"]
    ml["confidence_bin"] = pd.cut(ml["confidence"].clip(0, 1), bins=np.linspace(0, 1, 11), include_lowest=True)
    rows = []
    for bucket, group in ml.groupby("confidence_bin", observed=True):
        rows.append(
            {
                "confidence_bin": str(bucket),
                "n": len(group),
                "avg_confidence": group["confidence"].mean(),
                "accuracy": group["correct_top"].mean(),
                "recognized_rate": (group["status"] == "recognized").mean(),
            }
        )
    return pd.DataFrame(rows)


def build_guardrail_reason_summary(decisions: pd.DataFrame) -> pd.DataFrame:
    guard = decisions[decisions["policy"].isin(["stat_guardrail", "ml_guardrail_final"])].copy()
    guard["guardrail_reasons"] = guard["guardrail_reasons"].fillna("").astype(str)
    guard = guard[guard["guardrail_reasons"].str.len() > 0]
    if guard.empty:
        return pd.DataFrame(columns=["policy", "guardrail_reason", "guardrail_severity", "outcome", "n"])
    guard["guardrail_reason"] = guard["guardrail_reasons"].str.split("|")
    guard = guard.explode("guardrail_reason")
    guard["outcome"] = outcome_label(guard)
    return (
        guard.groupby(["policy", "guardrail_reason", "guardrail_severity", "outcome"], dropna=False)
        .size()
        .rename("n")
        .reset_index()
        .sort_values(["policy", "n"], ascending=[True, False])
    )


def build_family_policy_risk(decisions: pd.DataFrame) -> pd.DataFrame:
    rows = []
    for (policy, family), group in decisions.groupby(["policy", "expected_family"], sort=False):
        accepted = group[group["status"] == "recognized"]
        safe = group[group["is_correct_accept"] == True]
        unsafe = group[group["is_unsafe_accept"] == True]
        rows.append(
            {
                "policy": policy,
                "expected_family": family,
                "n": len(group),
                "recognized_rate": ratio(len(accepted), len(group)),
                "safe_accept_rate_per_case": ratio(len(safe), len(group)),
                "unsafe_accept_rate_per_case": ratio(len(unsafe), len(group)),
                "unsafe_accept_rate_among_accepted": ratio(len(unsafe), max(len(accepted), 1)),
                "ambiguous_rate": ratio((group["status"] == "ambiguous").sum(), len(group)),
            }
        )
    return pd.DataFrame(rows)


def build_source_policy_risk(decisions: pd.DataFrame) -> pd.DataFrame:
    baseline = decisions[decisions["policy"] == "baseline"][["case_id", "status", "is_correct_accept"]].rename(
        columns={"status": "baseline_status", "is_correct_accept": "baseline_correct_accept"}
    )
    merged = decisions.merge(baseline, on="case_id", how="left")
    rows = []
    for (source, policy), group in merged.groupby(["source_type", "policy"], sort=False):
        accepted = group[group["status"] == "recognized"]
        safe = group[group["is_correct_accept"] == True]
        unsafe = group[group["is_unsafe_accept"] == True]
        saved = group[(group["baseline_status"] != "recognized") & (group["is_correct_accept"] == True)]
        rows.append(
            {
                "source_type": source,
                "policy": policy,
                "n": len(group),
                "recognized_rate": ratio(len(accepted), len(group)),
                "safe_accept_rate_per_case": ratio(len(safe), len(group)),
                "unsafe_accept_rate_per_case": ratio(len(unsafe), len(group)),
                "unsafe_accept_rate_among_accepted": ratio(len(unsafe), max(len(accepted), 1)),
                "reject_saved_rate": ratio(len(saved), max((group["baseline_status"] != "recognized").sum(), 1)),
            }
        )
    return pd.DataFrame(rows)


def build_warmup_family_summary(warmup: pd.DataFrame) -> pd.DataFrame:
    if warmup.empty:
        return pd.DataFrame(columns=["warmup_stage", "expected_family", "n", "recognized_rate", "unsafe_accept_rate", "changed_from_baseline_rate"])
    rows = []
    for (stage, family), group in warmup.groupby(["warmup_stage", "expected_family"], sort=False):
        accepted = group[group["status"] == "recognized"]
        rows.append(
            {
                "warmup_stage": str(stage),
                "expected_family": family,
                "n": len(group),
                "recognized_rate": ratio(len(accepted), len(group)),
                "unsafe_accept_rate": ratio((group["is_unsafe_accept"] == True).sum(), max(len(accepted), 1)),
                "changed_from_baseline_rate": ratio((group["changed_from_baseline"] == True).sum(), len(group)),
                "avg_effective_threshold_bias": group["effective_threshold_bias"].mean(),
            }
        )
    return pd.DataFrame(rows)


def build_calibration_by_source(decisions: pd.DataFrame) -> pd.DataFrame:
    ml = decisions[decisions["policy"] == "ml_first"].copy()
    if ml.empty:
        return pd.DataFrame(columns=["source_type", "confidence_bin", "n", "avg_confidence", "accuracy", "recognized_rate"])
    ml["correct_top"] = ml["decision_family"] == ml["expected_family"]
    ml["confidence_bin"] = pd.cut(ml["confidence"].clip(0, 1), bins=np.linspace(0, 1, 11), include_lowest=True)
    rows = []
    for (source, bucket), group in ml.groupby(["source_type", "confidence_bin"], observed=True):
        rows.append(
            {
                "source_type": source,
                "confidence_bin": str(bucket),
                "n": len(group),
                "avg_confidence": group["confidence"].mean(),
                "accuracy": group["correct_top"].mean(),
                "recognized_rate": (group["status"] == "recognized").mean(),
            }
        )
    return pd.DataFrame(rows)


def build_decision_reason_summary(decisions: pd.DataFrame) -> pd.DataFrame:
    subset = decisions[decisions["policy"].isin(["ml_first", "stat_guardrail", "ml_guardrail_final"])].copy()
    if subset.empty:
        return pd.DataFrame(columns=["policy", "reason", "outcome", "n"])
    subset["outcome"] = outcome_label(subset)
    return (
        subset.groupby(["policy", "reason", "outcome"], dropna=False)
        .size()
        .rename("n")
        .reset_index()
        .sort_values(["policy", "n"], ascending=[True, False])
    )


def build_threshold_bias_summary(decisions: pd.DataFrame) -> pd.DataFrame:
    subset = decisions[decisions["policy"].isin(["tutorial_warmup", "ml_guardrail_final"])].copy()
    if subset.empty:
        return pd.DataFrame(columns=["policy", "bias_bin", "n", "avg_bias", "recognized_rate", "changed_from_baseline_rate", "unsafe_accept_rate"])
    subset["bias_bin"] = pd.cut(subset["effective_threshold_bias"], bins=[-0.001, 0.0, 0.01, 0.02, 0.035, 0.051], include_lowest=True)
    rows = []
    for (policy, bucket), group in subset.groupby(["policy", "bias_bin"], observed=True):
        accepted = group[group["status"] == "recognized"]
        rows.append(
            {
                "policy": policy,
                "bias_bin": str(bucket),
                "n": len(group),
                "avg_bias": group["effective_threshold_bias"].mean(),
                "recognized_rate": ratio(len(accepted), len(group)),
                "changed_from_baseline_rate": ratio((group["changed_from_baseline"] == True).sum(), len(group)),
                "unsafe_accept_rate": ratio((group["is_unsafe_accept"] == True).sum(), max(len(accepted), 1)),
            }
        )
    return pd.DataFrame(rows)


def build_ml_to_final_transition(decisions: pd.DataFrame) -> pd.DataFrame:
    ml = decisions[decisions["policy"] == "ml_first"][
        ["case_id", "status", "accepted_family", "is_correct_accept", "is_unsafe_accept"]
    ].rename(
        columns={
            "status": "ml_status",
            "accepted_family": "ml_accepted_family",
            "is_correct_accept": "ml_correct_accept",
            "is_unsafe_accept": "ml_unsafe_accept",
        }
    )
    final = decisions[decisions["policy"] == "ml_guardrail_final"][
        ["case_id", "status", "accepted_family", "is_correct_accept", "is_unsafe_accept"]
    ].rename(
        columns={
            "status": "final_status",
            "accepted_family": "final_accepted_family",
            "is_correct_accept": "final_correct_accept",
            "is_unsafe_accept": "final_unsafe_accept",
        }
    )
    merged = ml.merge(final, on="case_id", how="inner")
    if merged.empty:
        return pd.DataFrame(columns=["ml_status", "final_status", "outcome_shift", "n"])
    merged["outcome_shift"] = np.select(
        [
            (merged["ml_unsafe_accept"] == True) & (merged["final_unsafe_accept"] != True),
            (merged["ml_correct_accept"] == True) & (merged["final_correct_accept"] != True),
            (merged["ml_status"] != merged["final_status"]),
            (merged["ml_accepted_family"].fillna("") != merged["final_accepted_family"].fillna("")),
        ],
        ["unsafe_blocked", "safe_lost", "status_changed", "family_changed"],
        default="unchanged",
    )
    return (
        merged.groupby(["ml_status", "final_status", "outcome_shift"], dropna=False)
        .size()
        .rename("n")
        .reset_index()
        .sort_values("n", ascending=False)
    )


def build_dynamic_calibration_cells(cases: pd.DataFrame, decisions: pd.DataFrame) -> pd.DataFrame:
    dynamic = decisions[decisions["policy"] == "ml_guardrail_dynamic"].copy()
    if dynamic.empty:
        return pd.DataFrame()
    cols = ["case_id", "closure", "open_gap_ratio", "jitter_px", "rotation_bias", "extra_noise_stroke_count"]
    df = dynamic.merge(cases[cols + (["split"] if "split" in cases.columns else [])], on="case_id", how="left", suffixes=("", "_case"))
    if "split" not in df and "split_case" in df:
        df["split"] = df["split_case"]
    if "split" not in df:
        df["split"] = "calibration_train"
    df = df[df["split"] == "calibration_train"].copy()
    if df.empty:
        return pd.DataFrame()
    df["top_score_bin"] = pd.cut(df["top_score"].clip(0, 1), bins=[0, 0.62, 0.7, 0.78, 0.86, 1.0], include_lowest=True)
    df["score_gap_bin"] = pd.cut(df["score_gap"].clip(0, 1), bins=[0, 0.035, 0.06, 0.1, 0.16, 1.0], include_lowest=True)
    df["jitter_bin"] = pd.cut(df["jitter_px"].fillna(0), bins=[-0.001, 4, 10, 16, 30, 100], include_lowest=True)
    df["rotation_bin"] = pd.cut(df["rotation_bias"].fillna(0).clip(0, 1), bins=[0, 0.35, 0.6, 0.75, 0.9, 1.0], include_lowest=True)
    rows = []
    group_cols = ["source_type", "decision_family", "top_score_bin", "score_gap_bin", "jitter_bin", "rotation_bin"]
    for key, group in df.groupby(group_cols, observed=True, dropna=False):
        accepted = group[group["status"] == "recognized"]
        correct = group[group["is_correct_accept"] == True]
        unsafe = group[group["is_unsafe_accept"] == True]
        n = len(group)
        rows.append(
            {
                "source_type": key[0],
                "decision_family": key[1],
                "top_score_bin": str(key[2]),
                "score_gap_bin": str(key[3]),
                "jitter_bin": str(key[4]),
                "rotation_bin": str(key[5]),
                "n": n,
                "accepted": len(accepted),
                "correct_accepts": len(correct),
                "unsafe_accepts": len(unsafe),
                "empirical_precision": ratio(len(correct) + 1, len(accepted) + 2),
                "unsafe_accept_rate": ratio(len(unsafe), max(len(accepted), 1)),
                "recognized_rate": ratio(len(accepted), n),
                "avg_confidence": group["confidence"].mean(),
                "avg_top_score": group["top_score"].mean(),
                "avg_score_gap": group["score_gap"].mean(),
            }
        )
    return pd.DataFrame(rows)


def build_dynamic_calibration_fallbacks(decisions: pd.DataFrame) -> pd.DataFrame:
    dynamic = decisions[decisions["policy"] == "ml_guardrail_dynamic"].copy()
    if dynamic.empty:
        return pd.DataFrame()
    if "split" in dynamic:
        dynamic = dynamic[dynamic["split"] == "calibration_train"]
    rows = []
    for (source, family), group in dynamic.groupby(["source_type", "decision_family"], dropna=False):
        accepted = group[group["status"] == "recognized"]
        correct = group[group["is_correct_accept"] == True]
        unsafe = group[group["is_unsafe_accept"] == True]
        rows.append(
            {
                "source_type": source,
                "decision_family": family,
                "n": len(group),
                "accepted": len(accepted),
                "empirical_precision": ratio(len(correct) + 1, len(accepted) + 2),
                "unsafe_accept_rate": ratio(len(unsafe), max(len(accepted), 1)),
                "recognized_rate": ratio(len(accepted), len(group)),
                "recommended_confidence_threshold": float(group["confidence"].quantile(0.72)) if len(group) else 0,
                "recommended_score_gap_threshold": float(group["score_gap"].quantile(0.55)) if len(group) else 0,
            }
        )
    return pd.DataFrame(rows)


def build_dynamic_calibration_diagnostics(decisions: pd.DataFrame) -> pd.DataFrame:
    policies = [policy for policy in ["ml_first", "ml_guardrail_final", "ml_guardrail_dynamic"] if policy in set(decisions["policy"])]
    rows = []
    for policy in policies:
        group = decisions[decisions["policy"] == policy].copy()
        if "split" in group:
            group = group[group["split"] == "validation_holdout"]
            if group.empty:
                group = decisions[decisions["policy"] == policy].copy()
        group["correct_top"] = group["decision_family"] == group["expected_family"]
        for (source, family), cell in group.groupby(["source_type", "decision_family"], dropna=False):
            rows.append(
                {
                    "policy": policy,
                    "source_type": source,
                    "decision_family": family,
                    "n": len(cell),
                    "avg_confidence": cell["confidence"].mean(),
                    "accuracy": cell["correct_top"].mean(),
                    "recognized_rate": (cell["status"] == "recognized").mean(),
                    "unsafe_accept_rate": ratio((cell["is_unsafe_accept"] == True).sum(), max((cell["status"] == "recognized").sum(), 1)),
                    "ece_proxy": abs(cell["confidence"].mean() - cell["correct_top"].mean()) if len(cell) else 0,
                    "brier": float(((cell["confidence"].clip(0, 1) - cell["correct_top"].astype(float)) ** 2).mean()) if len(cell) else 0,
                }
            )
    return pd.DataFrame(rows)


def build_dynamic_threshold_sweep(decisions: pd.DataFrame) -> pd.DataFrame:
    dynamic = decisions[decisions["policy"] == "ml_guardrail_dynamic"].copy()
    if dynamic.empty:
        return pd.DataFrame()
    if "split" in dynamic:
        holdout = dynamic[dynamic["split"] == "validation_holdout"].copy()
        if not holdout.empty:
            dynamic = holdout
    rows = []
    for threshold in np.linspace(0.5, 0.98, 17):
        accepted = dynamic[(dynamic["status"] == "recognized") & (dynamic["confidence"] >= threshold)]
        rows.append(
            {
                "confidence_threshold": threshold,
                "recognized_rate": ratio(len(accepted), len(dynamic)),
                "safe_accept_rate_per_case": ratio((accepted["is_correct_accept"] == True).sum(), len(dynamic)),
                "unsafe_accept_rate_per_case": ratio((accepted["is_unsafe_accept"] == True).sum(), len(dynamic)),
                "unsafe_accept_rate_among_accepted": ratio((accepted["is_unsafe_accept"] == True).sum(), max(len(accepted), 1)),
            }
        )
    return pd.DataFrame(rows)


def plot_policy_lift_risk(summary: pd.DataFrame, fig_dir: Path) -> str:
    x = np.arange(len(summary))
    fig, ax1 = plt.subplots(figsize=(11, 6))
    ax1.bar(x - 0.2, summary["recognized_rate"], width=0.4, label="recognized rate", color="#2c7a7b")
    ax1.bar(x + 0.2, summary["unsafe_accept_rate"], width=0.4, label="unsafe accept among accepted", color="#c53030")
    ax1.set_xticks(x, summary["policy"], rotation=25, ha="right")
    ax1.set_ylim(0, max(1, summary[["recognized_rate", "unsafe_accept_rate"]].to_numpy().max() * 1.15))
    ax1.set_title("Policy recognition lift vs unsafe accept risk")
    ax1.legend()
    fig.tight_layout()
    return save(fig, fig_dir / "policy_lift_risk.png")


def plot_warmup_curve(summary: pd.DataFrame, fig_dir: Path) -> str:
    fig, ax = plt.subplots(figsize=(9, 5))
    if summary.empty:
        ax.text(0.5, 0.5, "No warmup rows", ha="center", va="center")
    else:
        labels = summary["warmup_stage"].astype(str)
        ax.plot(labels, summary["recognized_rate"], marker="o", label="recognized")
        ax.plot(labels, summary["changed_from_baseline_rate"], marker="o", label="changed")
        ax.plot(labels, summary["avg_effective_threshold_bias"], marker="o", label="avg threshold bias")
        ax.set_title("Tutorial warmup effect curve")
        ax.set_xlabel("Warmup stage")
        ax.legend()
    fig.tight_layout()
    return save(fig, fig_dir / "warmup_curve.png")


def plot_feature_risk_heatmap(feature_risk: pd.DataFrame, fig_dir: Path) -> str:
    top = feature_risk.copy()
    top["label"] = top["feature"] + " " + top["bucket"]
    top = top.sort_values("final_unsafe_rate", ascending=False).head(24)
    matrix = top[["baseline_failure_rate", "final_failure_rate", "baseline_unsafe_rate", "final_unsafe_rate", "final_safe_accept_rate"]].to_numpy()
    fig, ax = plt.subplots(figsize=(10, max(6, 0.32 * len(top))))
    image = ax.imshow(matrix, cmap="YlOrRd", vmin=0, vmax=max(0.01, np.nanmax(matrix)), aspect="auto")
    ax.set_yticks(np.arange(len(top)), top["label"])
    ax.set_xticks(np.arange(5), ["base fail", "final fail", "base unsafe", "final unsafe", "final safe"], rotation=25, ha="right")
    ax.set_title("Highest-risk feature bins")
    fig.colorbar(image, ax=ax, fraction=0.03, pad=0.02)
    fig.tight_layout()
    return save(fig, fig_dir / "feature_risk_heatmap.png")


def plot_ml_calibration(calibration: pd.DataFrame, fig_dir: Path) -> str:
    fig, ax = plt.subplots(figsize=(7, 6))
    if calibration.empty:
        ax.text(0.5, 0.5, "No calibration rows", ha="center", va="center")
    else:
        ax.plot([0, 1], [0, 1], linestyle="--", color="#718096", label="perfect")
        ax.scatter(calibration["avg_confidence"], calibration["accuracy"], s=np.maximum(calibration["n"], 1) / calibration["n"].max() * 220, color="#2b6cb0")
        for _, row in calibration.iterrows():
            ax.text(row["avg_confidence"], row["accuracy"], str(int(row["n"])), fontsize=8)
        ax.set_xlabel("Average ML confidence")
        ax.set_ylabel("Observed top-label accuracy")
        ax.set_title("ML-first calibration")
        ax.legend()
    fig.tight_layout()
    return save(fig, fig_dir / "ml_calibration.png")


def plot_transition_heatmap(decisions: pd.DataFrame, fig_dir: Path) -> str:
    base = decisions[decisions["policy"] == "baseline"][["case_id", "status"]].rename(columns={"status": "baseline_status"})
    final = decisions[decisions["policy"] == "ml_guardrail_final"][["case_id", "status"]].rename(columns={"status": "final_status"})
    table = pd.crosstab(base.merge(final, on="case_id")["baseline_status"], base.merge(final, on="case_id")["final_status"]).reindex(index=STATUS_ORDER, columns=STATUS_ORDER, fill_value=0)
    fig, ax = plt.subplots(figsize=(7, 6))
    image = ax.imshow(table.to_numpy(), cmap="Blues")
    ax.set_xticks(range(len(table.columns)), table.columns, rotation=25)
    ax.set_yticks(range(len(table.index)), table.index)
    ax.set_title("Baseline to ML+guardrail final status transition")
    annotate(ax, table.to_numpy())
    fig.colorbar(image, ax=ax, fraction=0.046, pad=0.04)
    fig.tight_layout()
    return save(fig, fig_dir / "transition_heatmap.png")


def plot_confusion_pairs(confusion: pd.DataFrame, fig_dir: Path) -> str:
    final = confusion[confusion["policy"] == "ml_guardrail_final"].sort_values("n", ascending=False).head(20)
    fig, ax = plt.subplots(figsize=(10, max(5, len(final) * 0.3)))
    if final.empty:
        ax.text(0.5, 0.5, "No recognized rows", ha="center", va="center")
    else:
        labels = final["expected_family"] + "->" + final["accepted_family"]
        ax.barh(labels, final["n"], color=np.where(final["expected_family"] == final["accepted_family"], "#2c7a7b", "#c53030"))
        ax.invert_yaxis()
        ax.set_title("Final recognized family pairs")
        ax.set_xlabel("Cases")
    fig.tight_layout()
    return save(fig, fig_dir / "confusion_pairs.png")


def plot_source_policy_status(decisions: pd.DataFrame, fig_dir: Path) -> str:
    subset = decisions[decisions["policy"].isin(["baseline", "ml_first", "ml_guardrail_final"])]
    table = pd.crosstab([subset["source_type"], subset["policy"]], subset["status"], normalize="index").reindex(columns=STATUS_ORDER, fill_value=0)
    fig, ax = plt.subplots(figsize=(12, 7))
    bottom = np.zeros(len(table))
    colors = ["#2c7a7b", "#d69e2e", "#dd6b20", "#718096"]
    for status, color in zip(STATUS_ORDER, colors):
        ax.bar(np.arange(len(table)), table[status], bottom=bottom, color=color, label=status)
        bottom += table[status].to_numpy()
    ax.set_xticks(np.arange(len(table)), [f"{idx[0]}\n{idx[1]}" for idx in table.index], rotation=20, ha="right")
    ax.set_ylim(0, 1)
    ax.set_title("Status distribution by source type and policy")
    ax.legend(ncol=4)
    fig.tight_layout()
    return save(fig, fig_dir / "source_policy_status.png")


def plot_survey_actual_policy(survey: pd.DataFrame, fig_dir: Path) -> str:
    table = pd.crosstab(survey["policy"], survey["status"]).reindex(index=POLICY_ORDER, columns=STATUS_ORDER, fill_value=0)
    fig, ax = plt.subplots(figsize=(9, 5))
    bottom = np.zeros(len(table))
    for status, color in zip(STATUS_ORDER, ["#2c7a7b", "#d69e2e", "#dd6b20", "#718096"]):
        ax.bar(table.index, table[status], bottom=bottom, label=status, color=color)
        bottom += table[status].to_numpy()
    ax.set_title("Actual survey direct inputs by policy")
    ax.tick_params(axis="x", rotation=25)
    ax.legend(ncol=4)
    fig.tight_layout()
    return save(fig, fig_dir / "survey_actual_policy.png")


def plot_guardrail_reason_frequency(summary: pd.DataFrame, fig_dir: Path) -> str:
    policies = ["stat_guardrail", "ml_guardrail_final"]
    fig, axes = plt.subplots(1, len(policies), figsize=(15, 6), sharex=False)
    for ax, policy in zip(np.atleast_1d(axes), policies):
        group = summary[summary["policy"] == policy]
        totals = group.groupby("guardrail_reason")["n"].sum().sort_values(ascending=False).head(12)
        if totals.empty:
            ax.text(0.5, 0.5, "No guardrail reasons", ha="center", va="center")
            ax.set_axis_off()
            continue
        ax.barh(totals.index, totals.to_numpy(), color="#805ad5")
        ax.invert_yaxis()
        ax.set_title(f"{policy} guardrail reasons")
        ax.set_xlabel("Reason hits")
    fig.tight_layout()
    return save(fig, fig_dir / "guardrail_reason_frequency.png")


def plot_policy_score_gap_top_score(decisions: pd.DataFrame, fig_dir: Path) -> str:
    policies = ["baseline", "ml_first", "ml_guardrail_final"]
    colors = {"safe accept": "#2c7a7b", "unsafe accept": "#c53030", "held/rejected": "#a0aec0", "other accept": "#d69e2e"}
    fig, axes = plt.subplots(1, len(policies), figsize=(15, 5), sharex=True, sharey=True)
    for ax, policy in zip(axes, policies):
        group = decisions[decisions["policy"] == policy].copy()
        if len(group) > 9000:
            group = group.sample(9000, random_state=23)
        group["plot_outcome"] = np.select(
            [
                group["is_correct_accept"] == True,
                group["is_unsafe_accept"] == True,
                group["status"] != "recognized",
            ],
            ["safe accept", "unsafe accept", "held/rejected"],
            default="other accept",
        )
        for label in ["held/rejected", "safe accept", "unsafe accept", "other accept"]:
            points = group[group["plot_outcome"] == label]
            if points.empty:
                continue
            ax.scatter(points["score_gap"], points["top_score"], s=5, alpha=0.28, c=colors[label], label=label)
        ax.axvline(0.06, color="#4a5568", linestyle="--", linewidth=1)
        ax.axhline(0.62, color="#4a5568", linestyle="--", linewidth=1)
        ax.set_title(policy)
        ax.set_xlabel("score gap")
    axes[0].set_ylabel("top score")
    axes[-1].legend(loc="lower right", fontsize=8)
    fig.suptitle("Decision regions by top score and score gap", y=1.02)
    fig.tight_layout()
    return save(fig, fig_dir / "policy_score_gap_top_score.png")


def plot_unsafe_feature_distributions(cases: pd.DataFrame, decisions: pd.DataFrame, fig_dir: Path) -> str:
    case_cols = ["case_id", "closure", "open_gap_ratio", "jitter_px", "rotation_bias", "stability"]
    decision_cols = ["case_id", "policy", "status", "is_correct_accept", "is_unsafe_accept", "score_gap", "top_score"]
    df = decisions[decisions["policy"].isin(["ml_first", "ml_guardrail_final"])][decision_cols].merge(cases[case_cols], on="case_id", how="left")
    df = df[df["status"] == "recognized"].copy()
    df["outcome"] = np.select([df["is_correct_accept"] == True, df["is_unsafe_accept"] == True], ["safe", "unsafe"], default="other")
    groups = [
        ("ML safe", (df["policy"] == "ml_first") & (df["outcome"] == "safe")),
        ("ML unsafe", (df["policy"] == "ml_first") & (df["outcome"] == "unsafe")),
        ("Final safe", (df["policy"] == "ml_guardrail_final") & (df["outcome"] == "safe")),
        ("Final unsafe", (df["policy"] == "ml_guardrail_final") & (df["outcome"] == "unsafe")),
    ]
    features = ["top_score", "score_gap", "closure", "open_gap_ratio", "jitter_px", "rotation_bias", "stability"]
    fig, axes = plt.subplots(2, 4, figsize=(16, 8))
    axes = axes.ravel()
    for ax, feature in zip(axes, features):
        values = []
        for _, mask in groups:
            series = df.loc[mask, feature].dropna()
            if len(series) > 7000:
                series = series.sample(7000, random_state=31)
            values.append(series.to_numpy() if len(series) else np.array([np.nan]))
        box = ax.boxplot(values, showfliers=False, patch_artist=True)
        for patch, color in zip(box["boxes"], ["#2c7a7b", "#c53030", "#38a169", "#e53e3e"]):
            patch.set_facecolor(color)
            patch.set_alpha(0.55)
        ax.set_xticks(range(1, len(groups) + 1), [label for label, _ in groups], rotation=25, ha="right")
        ax.set_title(feature)
    axes[-1].set_axis_off()
    fig.suptitle("Feature distributions for safe vs unsafe recognized decisions", y=1.01)
    fig.tight_layout()
    return save(fig, fig_dir / "unsafe_feature_distributions.png")


def plot_family_policy_matrix(summary: pd.DataFrame, fig_dir: Path) -> str:
    families = [family for family in ["wind", "earth", "fire", "water", "life"] if family in set(summary["expected_family"])]
    policies = [policy for policy in POLICY_ORDER if policy in set(summary["policy"])]
    fig, axes = plt.subplots(1, 2, figsize=(14, 5))
    metrics = [
        ("recognized_rate", "Recognized rate"),
        ("unsafe_accept_rate_per_case", "Unsafe accept rate per case"),
    ]
    for ax, (metric, title) in zip(axes, metrics):
        table = summary.pivot_table(index="expected_family", columns="policy", values=metric, aggfunc="mean").reindex(index=families, columns=policies)
        image = ax.imshow(table.to_numpy(), cmap="YlGnBu" if metric == "recognized_rate" else "YlOrRd", vmin=0, vmax=max(0.01, np.nanmax(table.to_numpy())))
        ax.set_xticks(range(len(policies)), policies, rotation=25, ha="right")
        ax.set_yticks(range(len(families)), families)
        ax.set_title(title)
        annotate_float(ax, table.to_numpy())
        fig.colorbar(image, ax=ax, fraction=0.046, pad=0.04)
    fig.tight_layout()
    return save(fig, fig_dir / "family_policy_matrix.png")


def plot_source_policy_risk_matrix(summary: pd.DataFrame, fig_dir: Path) -> str:
    sources = sorted(summary["source_type"].unique())
    policies = [policy for policy in POLICY_ORDER if policy in set(summary["policy"])]
    fig, axes = plt.subplots(1, 3, figsize=(18, 5))
    metrics = [
        ("recognized_rate", "Recognized rate", "YlGnBu"),
        ("unsafe_accept_rate_per_case", "Unsafe per case", "YlOrRd"),
        ("reject_saved_rate", "Saved rejects", "PuBuGn"),
    ]
    for ax, (metric, title, cmap) in zip(axes, metrics):
        table = summary.pivot_table(index="source_type", columns="policy", values=metric, aggfunc="mean").reindex(index=sources, columns=policies)
        image = ax.imshow(table.to_numpy(), cmap=cmap, vmin=0, vmax=max(0.01, np.nanmax(table.to_numpy())))
        ax.set_xticks(range(len(policies)), policies, rotation=25, ha="right")
        ax.set_yticks(range(len(sources)), sources)
        ax.set_title(title)
        annotate_float(ax, table.to_numpy())
        fig.colorbar(image, ax=ax, fraction=0.046, pad=0.04)
    fig.tight_layout()
    return save(fig, fig_dir / "source_policy_risk_matrix.png")


def plot_warmup_family_curve(summary: pd.DataFrame, fig_dir: Path) -> str:
    fig, axes = plt.subplots(1, 2, figsize=(14, 5), sharex=True)
    if summary.empty:
        for ax in axes:
            ax.text(0.5, 0.5, "No warmup rows", ha="center", va="center")
    else:
        stage_order = ["0", "6", "12", "24", "full"]
        for family, group in summary.groupby("expected_family"):
            group = group.set_index("warmup_stage").reindex(stage_order).reset_index()
            axes[0].plot(group["warmup_stage"], group["recognized_rate"], marker="o", label=family)
            axes[1].plot(group["warmup_stage"], group["unsafe_accept_rate"], marker="o", label=family)
        axes[0].set_title("Warmup recognized rate by family")
        axes[1].set_title("Warmup unsafe rate by family")
        for ax in axes:
            ax.set_xlabel("Warmup stage")
            ax.set_ylim(bottom=0)
            ax.legend(fontsize=8)
    fig.tight_layout()
    return save(fig, fig_dir / "warmup_family_curve.png")


def plot_calibration_by_source(calibration: pd.DataFrame, fig_dir: Path) -> str:
    fig, ax = plt.subplots(figsize=(8, 7))
    if calibration.empty:
        ax.text(0.5, 0.5, "No calibration rows", ha="center", va="center")
    else:
        ax.plot([0, 1], [0, 1], linestyle="--", color="#718096", label="perfect")
        for source, group in calibration.groupby("source_type"):
            group = group.sort_values("avg_confidence")
            ax.plot(group["avg_confidence"], group["accuracy"], marker="o", label=source)
        ax.set_xlabel("Average ML confidence")
        ax.set_ylabel("Observed top-label accuracy")
        ax.set_title("ML-first calibration by synthetic source")
        ax.legend()
    fig.tight_layout()
    return save(fig, fig_dir / "calibration_by_source.png")


def plot_decision_reason_outcomes(summary: pd.DataFrame, fig_dir: Path) -> str:
    policies = ["ml_first", "stat_guardrail", "ml_guardrail_final"]
    colors = {"safe_accept": "#2c7a7b", "unsafe_accept": "#c53030", "held_or_rejected": "#a0aec0", "other_accept": "#d69e2e"}
    fig, axes = plt.subplots(1, len(policies), figsize=(18, 6), sharex=False)
    for ax, policy in zip(axes, policies):
        group = summary[summary["policy"] == policy]
        totals = group.groupby("reason")["n"].sum().sort_values(ascending=False).head(8)
        if totals.empty:
            ax.text(0.5, 0.5, "No reason rows", ha="center", va="center")
            ax.set_axis_off()
            continue
        pivot = (
            group[group["reason"].isin(totals.index)]
            .pivot_table(index="reason", columns="outcome", values="n", aggfunc="sum", fill_value=0)
            .reindex(index=totals.index)
        )
        left = np.zeros(len(pivot))
        for outcome in ["safe_accept", "unsafe_accept", "held_or_rejected", "other_accept"]:
            values = pivot[outcome].to_numpy() if outcome in pivot else np.zeros(len(pivot))
            ax.barh(pivot.index, values, left=left, color=colors[outcome], label=outcome)
            left += values
        ax.invert_yaxis()
        ax.set_title(policy)
        ax.set_xlabel("Cases")
    axes[-1].legend(loc="lower right", fontsize=8)
    fig.suptitle("Decision reason outcomes", y=1.02)
    fig.tight_layout()
    return save(fig, fig_dir / "decision_reason_outcomes.png")


def plot_threshold_bias_effect(summary: pd.DataFrame, fig_dir: Path) -> str:
    fig, axes = plt.subplots(1, 3, figsize=(17, 5), sharex=True)
    if summary.empty:
        for ax in axes:
            ax.text(0.5, 0.5, "No threshold rows", ha="center", va="center")
    else:
        metrics = [
            ("recognized_rate", "Recognized rate"),
            ("changed_from_baseline_rate", "Changed from baseline"),
            ("unsafe_accept_rate", "Unsafe among accepted"),
        ]
        for ax, (metric, title) in zip(axes, metrics):
            for policy, group in summary.groupby("policy"):
                group = group.sort_values("avg_bias")
                ax.plot(group["bias_bin"], group[metric], marker="o", label=policy)
            ax.set_title(title)
            ax.tick_params(axis="x", rotation=25)
            ax.set_ylim(bottom=0)
        axes[-1].legend(fontsize=8)
    fig.tight_layout()
    return save(fig, fig_dir / "threshold_bias_effect.png")


def plot_ml_to_final_transition(summary: pd.DataFrame, fig_dir: Path) -> str:
    fig, axes = plt.subplots(1, 2, figsize=(14, 6))
    if summary.empty:
        for ax in axes:
            ax.text(0.5, 0.5, "No transition rows", ha="center", va="center")
    else:
        table = summary.pivot_table(index="ml_status", columns="final_status", values="n", aggfunc="sum", fill_value=0).reindex(index=STATUS_ORDER, columns=STATUS_ORDER, fill_value=0)
        image = axes[0].imshow(table.to_numpy(), cmap="Blues")
        axes[0].set_xticks(range(len(table.columns)), table.columns, rotation=25, ha="right")
        axes[0].set_yticks(range(len(table.index)), table.index)
        axes[0].set_title("ML-first to final status")
        annotate(axes[0], table.to_numpy())
        fig.colorbar(image, ax=axes[0], fraction=0.046, pad=0.04)

        shifts = summary.groupby("outcome_shift")["n"].sum().sort_values(ascending=True)
        axes[1].barh(shifts.index, shifts.to_numpy(), color=np.where(shifts.index == "unsafe_blocked", "#2c7a7b", "#718096"))
        axes[1].set_title("Final guardrail effect on ML-first")
        axes[1].set_xlabel("Cases")
    fig.tight_layout()
    return save(fig, fig_dir / "ml_to_final_transition.png")


def plot_dynamic_policy_lift_risk(summary: pd.DataFrame, fig_dir: Path) -> str:
    policies = [policy for policy in ["baseline", "ml_first", "ml_guardrail_final", "ml_guardrail_dynamic"] if policy in set(summary["policy"])]
    data = summary.set_index("policy").reindex(policies)
    fig, ax = plt.subplots(figsize=(10, 6))
    x = np.arange(len(data))
    ax.bar(x - 0.22, data["recognized_rate"], width=0.22, label="recognized", color="#2c7a7b")
    ax.bar(x, data["unsafe_accept_rate"], width=0.22, label="unsafe among accepted", color="#c53030")
    ax.bar(x + 0.22, data["safe_accept_rate"], width=0.22, label="safe among accepted", color="#38a169")
    ax.set_xticks(x, data.index, rotation=20, ha="right")
    ax.set_ylim(0, max(1, np.nanmax(data[["recognized_rate", "unsafe_accept_rate", "safe_accept_rate"]].to_numpy()) * 1.15))
    ax.set_title("Dynamic policy lift/risk comparison")
    ax.legend()
    fig.tight_layout()
    return save(fig, fig_dir / "dynamic_policy_lift_risk.png")


def plot_family_source_threshold_heatmap(fallbacks: pd.DataFrame, fig_dir: Path) -> str:
    fig, axes = plt.subplots(1, 2, figsize=(15, 6))
    if fallbacks.empty:
        for ax in axes:
            ax.text(0.5, 0.5, "No fallback rows", ha="center", va="center")
    else:
        sources = sorted(fallbacks["source_type"].dropna().unique())
        families = [family for family in ["wind", "earth", "fire", "water", "life"] if family in set(fallbacks["decision_family"])]
        for ax, metric, title in [
            (axes[0], "recommended_confidence_threshold", "Recommended confidence threshold"),
            (axes[1], "empirical_precision", "Empirical precision"),
        ]:
            table = fallbacks.pivot_table(index="source_type", columns="decision_family", values=metric, aggfunc="mean").reindex(index=sources, columns=families)
            image = ax.imshow(table.to_numpy(), cmap="YlOrRd" if metric.endswith("threshold") else "YlGnBu", vmin=0, vmax=max(0.01, np.nanmax(table.to_numpy())))
            ax.set_xticks(range(len(families)), families, rotation=25, ha="right")
            ax.set_yticks(range(len(sources)), sources)
            ax.set_title(title)
            annotate_float(ax, table.to_numpy())
            fig.colorbar(image, ax=ax, fraction=0.046, pad=0.04)
    fig.tight_layout()
    return save(fig, fig_dir / "family_source_threshold_heatmap.png")


def plot_calibration_reliability_by_family(diagnostics: pd.DataFrame, fig_dir: Path) -> str:
    fig, ax = plt.subplots(figsize=(9, 7))
    if diagnostics.empty:
        ax.text(0.5, 0.5, "No calibration diagnostics", ha="center", va="center")
    else:
        dyn = diagnostics[diagnostics["policy"] == "ml_guardrail_dynamic"]
        ax.plot([0, 1], [0, 1], linestyle="--", color="#718096", label="perfect")
        for family, group in dyn.groupby("decision_family"):
            group = group.sort_values("avg_confidence")
            sizes = np.maximum(group["n"], 1) / max(group["n"].max(), 1) * 260
            ax.scatter(group["avg_confidence"], group["accuracy"], s=sizes, alpha=0.7, label=family)
        ax.set_xlabel("Average confidence")
        ax.set_ylabel("Observed accuracy")
        ax.set_title("Dynamic calibration reliability by family/source cell")
        ax.legend()
    fig.tight_layout()
    return save(fig, fig_dir / "calibration_reliability_by_family.png")


def plot_survey_mutation_outlier_effect(decisions: pd.DataFrame, fig_dir: Path) -> str:
    subset = decisions[decisions["source_type"].isin(["survey_mutation", "survey_mutation_valid"])]
    fig, ax = plt.subplots(figsize=(10, 5))
    if subset.empty:
        ax.text(0.5, 0.5, "No survey mutation rows", ha="center", va="center")
    else:
        policies = [policy for policy in ["baseline", "ml_first", "ml_guardrail_final", "ml_guardrail_dynamic"] if policy in set(subset["policy"])]
        rows = []
        for (source, policy), group in subset.groupby(["source_type", "policy"]):
            accepted = group[group["status"] == "recognized"]
            rows.append({"source_type": source, "policy": policy, "recognized": ratio(len(accepted), len(group)), "unsafe": ratio((accepted["is_unsafe_accept"] == True).sum(), max(len(accepted), 1))})
        data = pd.DataFrame(rows)
        labels = [f"{source}\n{policy}" for source in sorted(data["source_type"].unique()) for policy in policies if not data[(data["source_type"] == source) & (data["policy"] == policy)].empty]
        recognized = []
        unsafe = []
        for label in labels:
            source, policy = label.split("\n")
            row = data[(data["source_type"] == source) & (data["policy"] == policy)].iloc[0]
            recognized.append(row["recognized"])
            unsafe.append(row["unsafe"])
        x = np.arange(len(labels))
        ax.bar(x - 0.18, recognized, width=0.36, label="recognized", color="#2c7a7b")
        ax.bar(x + 0.18, unsafe, width=0.36, label="unsafe among accepted", color="#c53030")
        ax.set_xticks(x, labels, rotation=25, ha="right")
        ax.set_title("Survey mutation outlier effect")
        ax.legend()
    fig.tight_layout()
    return save(fig, fig_dir / "survey_mutation_outlier_effect.png")


def plot_feature_risk_surface(cases: pd.DataFrame, decisions: pd.DataFrame, fig_dir: Path) -> str:
    dyn = decisions[decisions["policy"] == "ml_guardrail_dynamic"][["case_id", "status", "is_unsafe_accept", "top_score", "score_gap"]]
    df = dyn.merge(cases[["case_id", "jitter_px", "open_gap_ratio", "rotation_bias"]], on="case_id", how="left")
    fig, axes = plt.subplots(1, 3, figsize=(17, 5))
    if df.empty:
        for ax in axes:
            ax.text(0.5, 0.5, "No dynamic rows", ha="center", va="center")
    else:
        specs = [("score_gap", "top_score"), ("jitter_px", "open_gap_ratio"), ("rotation_bias", "top_score")]
        for ax, (xcol, ycol) in zip(axes, specs):
            sample = df.sample(min(len(df), 12000), random_state=37)
            colors = np.where(sample["is_unsafe_accept"] == True, "#c53030", np.where(sample["status"] == "recognized", "#2c7a7b", "#a0aec0"))
            ax.scatter(sample[xcol], sample[ycol], s=5, alpha=0.28, c=colors)
            ax.set_xlabel(xcol)
            ax.set_ylabel(ycol)
            ax.set_title(f"{xcol} vs {ycol}")
    fig.suptitle("Dynamic feature risk surface", y=1.02)
    fig.tight_layout()
    return save(fig, fig_dir / "feature_risk_surface.png")


def plot_pareto_recognition_vs_unsafe(sweep: pd.DataFrame, fig_dir: Path) -> str:
    fig, ax = plt.subplots(figsize=(8, 6))
    if sweep.empty:
        ax.text(0.5, 0.5, "No threshold sweep", ha="center", va="center")
    else:
        ax.plot(sweep["unsafe_accept_rate_among_accepted"], sweep["recognized_rate"], marker="o", color="#805ad5")
        for _, row in sweep.iterrows():
            ax.text(row["unsafe_accept_rate_among_accepted"], row["recognized_rate"], f"{row['confidence_threshold']:.2f}", fontsize=8)
        ax.set_xlabel("Unsafe accept among accepted")
        ax.set_ylabel("Recognized rate")
        ax.set_title("Recognition vs unsafe trade-off")
    fig.tight_layout()
    return save(fig, fig_dir / "pareto_recognition_vs_unsafe.png")


def plot_actual_survey_valid_before_after(survey: pd.DataFrame, fig_dir: Path) -> str:
    fig, ax = plt.subplots(figsize=(9, 5))
    if survey.empty or "ml_guardrail_dynamic" not in set(survey["policy"]):
        ax.text(0.5, 0.5, "No dynamic survey rows", ha="center", va="center")
    else:
        policies = [policy for policy in ["baseline", "ml_first", "ml_guardrail_final", "ml_guardrail_dynamic"] if policy in set(survey["policy"])]
        rows = []
        for policy, group in survey.groupby("policy"):
            accepted = group[group["status"] == "recognized"]
            rows.append({"policy": policy, "recognized": len(accepted), "correct": (accepted["is_correct_accept"] == True).sum(), "unsafe": (accepted["is_unsafe_accept"] == True).sum()})
        data = pd.DataFrame(rows).set_index("policy").reindex(policies)
        x = np.arange(len(data))
        ax.bar(x - 0.25, data["recognized"], width=0.25, label="recognized", color="#2c7a7b")
        ax.bar(x, data["correct"], width=0.25, label="correct", color="#38a169")
        ax.bar(x + 0.25, data["unsafe"], width=0.25, label="unsafe", color="#c53030")
        ax.set_xticks(x, data.index, rotation=20, ha="right")
        ax.set_title("Actual survey direct inputs before/after")
        ax.legend()
    fig.tight_layout()
    return save(fig, fig_dir / "actual_survey_valid_before_after.png")


def plot_dynamic_confusion_matrix(decisions: pd.DataFrame, fig_dir: Path) -> str:
    dyn = decisions[(decisions["policy"] == "ml_guardrail_dynamic") & (decisions["status"] == "recognized")]
    fig, ax = plt.subplots(figsize=(7, 6))
    families = ["wind", "earth", "fire", "water", "life"]
    if dyn.empty:
        ax.text(0.5, 0.5, "No dynamic recognized rows", ha="center", va="center")
    else:
        table = pd.crosstab(dyn["expected_family"], dyn["accepted_family"]).reindex(index=families, columns=families, fill_value=0)
        image = ax.imshow(table.to_numpy(), cmap="Blues")
        ax.set_xticks(range(len(families)), families, rotation=25)
        ax.set_yticks(range(len(families)), families)
        ax.set_xlabel("Accepted")
        ax.set_ylabel("Expected")
        ax.set_title("Dynamic policy confusion matrix")
        annotate(ax, table.to_numpy())
        fig.colorbar(image, ax=ax, fraction=0.046, pad=0.04)
    fig.tight_layout()
    return save(fig, fig_dir / "dynamic_confusion_matrix.png")


def write_report(out_dir: Path, summary: dict, policy: pd.DataFrame, warmup: pd.DataFrame, risk: pd.DataFrame, calibration: pd.DataFrame, figures: dict[str, str]) -> None:
    final = policy[policy["policy"] == "ml_guardrail_final"].iloc[0]
    ml_first = policy[policy["policy"] == "ml_first"].iloc[0]
    baseline = policy[policy["policy"] == "baseline"].iloc[0]
    dynamic = policy[policy["policy"] == "ml_guardrail_dynamic"].iloc[0] if "ml_guardrail_dynamic" in set(policy["policy"]) else None
    lines = [
        "# Guardrail/ML 100k Experiment Report",
        "",
        "## Executive Summary",
        f"- 총 `{summary['actualCaseCount']}`개 synthetic/survey-mutated case를 생성했습니다.",
        f"- baseline recognized rate는 `{baseline.recognized_rate:.2%}`, ML-first는 `{ml_first.recognized_rate:.2%}`, final은 `{final.recognized_rate:.2%}`입니다.",
        f"- ML-first unsafe accept rate는 `{ml_first.unsafe_accept_rate:.2%}`, final unsafe accept rate는 `{final.unsafe_accept_rate:.2%}`입니다.",
        f"- final net recognition lift는 `{int(final.net_recognition_lift)}`건입니다.",
        f"- ML calibration ECE는 `{summary['mlCalibration']['ece']:.4f}`, Brier score는 `{summary['mlCalibration']['brier']:.4f}`입니다.",
        "",
        "## Interpretation",
        "- `ml_first`는 shadow/tinyML confidence가 충분한 경우 실제 decision을 바꾸도록 설계했습니다.",
        "- `ml_guardrail_final`은 ML의 승격/교체 결과 중 낮은 scoreGap, 낮은 closure, 큰 openGap/jitter/noise 위험을 다시 보류합니다.",
        "- dynamic 정책은 production recognizer 기본 경로에도 연결되며, legacy 모드는 비교/회귀 검증용으로 남깁니다.",
        "",
        "## Warmup",
    ]
    if dynamic is not None:
        lines.insert(
            6,
            f"- dynamic recognized rate는 `{dynamic.recognized_rate:.2%}`, dynamic unsafe accept rate는 `{dynamic.unsafe_accept_rate:.2%}`입니다."
        )
    for row in warmup.itertuples():
        lines.append(f"- stage `{row.warmup_stage}` captures={int(row.profile_capture_count)} recognized={row.recognized_rate:.2%} changed={row.changed_from_baseline_rate:.2%} threshold={row.avg_effective_threshold_bias:.4f}")
    lines.extend(["", "## Figures"])
    for name, path in figures.items():
        lines.append(f"- [{name}]({Path(path).relative_to(out_dir).as_posix()})")
    lines.extend(
        [
            "",
            "## Key CSV Outputs",
            "- `experiment_cases.csv`",
            "- `policy_decisions.csv`",
            "- `policy_summary.csv`",
            "- `warmup_summary.csv`",
            "- `feature_bin_risk.csv`",
            "- `confusion_pairs.csv`",
            "- `ml_calibration_bins.csv`",
            "- `guardrail_reason_summary.csv`",
            "- `family_policy_risk.csv`",
            "- `source_policy_risk.csv`",
            "- `warmup_family_summary.csv`",
            "- `ml_calibration_by_source.csv`",
            "- `decision_reason_summary.csv`",
            "- `threshold_bias_summary.csv`",
            "- `ml_to_final_transition.csv`",
            "- `calibration_cells.csv`",
            "- `calibration_fallbacks.csv`",
            "- `calibration_diagnostics.csv`",
            "- `dynamic_threshold_sweep.csv`",
        ]
    )
    (out_dir / "analysis_report.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


def expected_calibration_error(calibration: pd.DataFrame) -> float:
    if calibration.empty:
        return 0.0
    total = calibration["n"].sum()
    return float(((calibration["n"] / max(total, 1)) * (calibration["avg_confidence"] - calibration["accuracy"]).abs()).sum())


def brier_score(ml: pd.DataFrame) -> float:
    if ml.empty:
        return 0.0
    y = (ml["decision_family"] == ml["expected_family"]).astype(float)
    return float(((ml["confidence"].clip(0, 1) - y) ** 2).mean())


def annotate(ax, data: np.ndarray) -> None:
    max_value = data.max() if data.size else 0
    for row in range(data.shape[0]):
        for col in range(data.shape[1]):
            ax.text(col, row, f"{int(data[row, col])}", ha="center", va="center", color="white" if max_value and data[row, col] > max_value * 0.55 else "black", fontsize=8)


def annotate_float(ax, data: np.ndarray) -> None:
    finite = data[np.isfinite(data)]
    max_value = finite.max() if finite.size else 0
    for row in range(data.shape[0]):
        for col in range(data.shape[1]):
            value = data[row, col]
            if not np.isfinite(value):
                label = "n/a"
                color = "black"
            else:
                label = f"{value:.2f}"
                color = "white" if max_value and value > max_value * 0.55 else "black"
            ax.text(col, row, label, ha="center", va="center", color=color, fontsize=8)


def outcome_label(df: pd.DataFrame) -> np.ndarray:
    return np.select(
        [
            df["is_correct_accept"] == True,
            df["is_unsafe_accept"] == True,
            df["status"] != "recognized",
        ],
        ["safe_accept", "unsafe_accept", "held_or_rejected"],
        default="other_accept",
    )


def ratio(numerator: int | float, denominator: int | float) -> float:
    return float(numerator / denominator) if denominator else 0.0


def save(fig, path: Path) -> str:
    fig.savefig(path, dpi=180, bbox_inches="tight")
    plt.close(fig)
    return str(path)


def validate_figures(figures: dict[str, str]) -> None:
    missing = [path for path in figures.values() if not Path(path).exists() or Path(path).stat().st_size <= 0]
    if missing:
        raise RuntimeError(f"empty or missing figures: {missing}")


def read_json(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def write_json(path: Path, value: dict) -> None:
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
