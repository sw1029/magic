from __future__ import annotations

import json
import sys
from pathlib import Path

import matplotlib.pyplot as plt
import numpy as np
import pandas as pd


STATUS_ORDER = ["recognized", "ambiguous", "incomplete", "invalid"]
POLICY_ORDER = [
    "tutorial_warmup",
    "ml_first",
    "stat_guardrail",
    "ml_guardrail_final",
    "ml_guardrail_dynamic",
]


def main() -> None:
    if len(sys.argv) < 2:
        raise SystemExit("usage: python scripts/survey-correction-comparison-plots.py <experiment-output-dir>")

    out_dir = Path(sys.argv[1]).resolve()
    fig_dir = out_dir / "figures"
    fig_dir.mkdir(parents=True, exist_ok=True)

    cases = pd.read_csv(out_dir / "experiment_cases.csv", low_memory=False)
    decisions = pd.read_csv(out_dir / "policy_decisions.csv", low_memory=False)
    survey_actual = pd.read_csv(out_dir / "survey_actual_policy_decisions.csv", low_memory=False)

    summary = build_correction_summary(decisions)
    source_delta = build_group_delta(decisions, "source_type")
    family_delta = build_group_delta(decisions, "expected_family")
    feature_delta = build_feature_delta(cases, decisions)

    summary.to_csv(out_dir / "correction_comparison_summary.csv", index=False)
    source_delta.to_csv(out_dir / "correction_source_delta.csv", index=False)
    family_delta.to_csv(out_dir / "correction_family_delta.csv", index=False)
    feature_delta.to_csv(out_dir / "correction_feature_delta.csv", index=False)

    figures = {
        "correction_logic_waterfall": plot_logic_waterfall(summary, fig_dir),
        "correction_status_transition_grid": plot_status_transition_grid(decisions, fig_dir),
        "correction_source_delta_heatmap": plot_group_delta_heatmap(source_delta, "source_type", fig_dir),
        "correction_family_delta_heatmap": plot_group_delta_heatmap(family_delta, "expected_family", fig_dir),
        "correction_feature_delta_heatmap": plot_feature_delta_heatmap(feature_delta, fig_dir),
        "correction_policy_metric_matrix": plot_policy_metric_matrix(summary, fig_dir),
        "correction_actual_survey_before_after": plot_actual_survey_before_after(survey_actual, fig_dir),
        "correction_logic_reason_breakdown": plot_logic_reason_breakdown(decisions, fig_dir),
    }
    validate_figures(figures)
    write_report(out_dir, summary, source_delta, family_delta, figures)
    update_summary_json(out_dir, figures)
    print(json.dumps({"out_dir": str(out_dir), "figures": len(figures), "summary_rows": len(summary)}, ensure_ascii=False))


def build_correction_summary(decisions: pd.DataFrame) -> pd.DataFrame:
    base = baseline_frame(decisions)
    rows = []
    for policy in POLICY_ORDER:
        group = decisions[decisions["policy"] == policy].copy()
        if group.empty:
            continue
        df = group.merge(base, on="case_id", how="left")
        policy_recognized = df["status"].eq("recognized")
        baseline_recognized = df["baseline_status"].eq("recognized")
        policy_correct = truthy(df["is_correct_accept"])
        baseline_correct = truthy(df["baseline_correct_accept"])
        policy_unsafe = truthy(df["is_unsafe_accept"])
        baseline_unsafe = truthy(df["baseline_unsafe_accept"])
        accepted = int(policy_recognized.sum())
        rows.append(
            {
                "policy": policy,
                "n": len(df),
                "recognized": accepted,
                "recognized_rate": ratio(accepted, len(df)),
                "recognized_delta": int(policy_recognized.sum() - baseline_recognized.sum()),
                "safe_accepts": int(policy_correct.sum()),
                "safe_delta": int(policy_correct.sum() - baseline_correct.sum()),
                "unsafe_accepts": int(policy_unsafe.sum()),
                "unsafe_delta": int(policy_unsafe.sum() - baseline_unsafe.sum()),
                "unsafe_accept_rate": ratio(policy_unsafe.sum(), max(accepted, 1)),
                "added_correct": int((~baseline_correct & policy_correct).sum()),
                "lost_correct": int((baseline_correct & ~policy_correct).sum()),
                "introduced_unsafe": int((~baseline_unsafe & policy_unsafe).sum()),
                "removed_unsafe": int((baseline_unsafe & ~policy_unsafe).sum()),
                "status_changed": int(df["status"].ne(df["baseline_status"]).sum()),
                "accepted_family_changed": int(df["accepted_family"].fillna("none").ne(df["baseline_accepted_family"].fillna("none")).sum()),
            }
        )
    return pd.DataFrame(rows)


def build_group_delta(decisions: pd.DataFrame, group_col: str) -> pd.DataFrame:
    base = baseline_frame(decisions)
    rows = []
    for policy in POLICY_ORDER:
        group = decisions[decisions["policy"] == policy].merge(base, on="case_id", how="left")
        if group.empty:
            continue
        for key, cell in group.groupby(group_col, dropna=False):
            policy_recognized = cell["status"].eq("recognized")
            baseline_recognized = cell["baseline_status"].eq("recognized")
            policy_correct = truthy(cell["is_correct_accept"])
            baseline_correct = truthy(cell["baseline_correct_accept"])
            policy_unsafe = truthy(cell["is_unsafe_accept"])
            baseline_unsafe = truthy(cell["baseline_unsafe_accept"])
            rows.append(
                {
                    group_col: key,
                    "policy": policy,
                    "n": len(cell),
                    "recognized_rate_delta": ratio(policy_recognized.sum(), len(cell)) - ratio(baseline_recognized.sum(), len(cell)),
                    "safe_rate_delta": ratio(policy_correct.sum(), len(cell)) - ratio(baseline_correct.sum(), len(cell)),
                    "unsafe_rate_delta": ratio(policy_unsafe.sum(), len(cell)) - ratio(baseline_unsafe.sum(), len(cell)),
                    "introduced_unsafe_rate": ratio((~baseline_unsafe & policy_unsafe).sum(), len(cell)),
                    "lost_correct_rate": ratio((baseline_correct & ~policy_correct).sum(), len(cell)),
                }
            )
    return pd.DataFrame(rows)


def build_feature_delta(cases: pd.DataFrame, decisions: pd.DataFrame) -> pd.DataFrame:
    base = baseline_frame(decisions)
    baseline_metrics = decisions[decisions["policy"] == "baseline"][["case_id", "top_score", "score_gap"]]
    feature_df = cases.merge(baseline_metrics, on="case_id", how="left")
    features = ["top_score", "score_gap", "closure", "open_gap_ratio", "jitter_px", "rotation_bias"]
    rows = []
    for feature in features:
        series = feature_df[feature].astype(float)
        try:
            bins = pd.qcut(series, q=6, duplicates="drop")
        except ValueError:
            bins = pd.cut(series, bins=4, duplicates="drop")
        feature_df[f"{feature}_bin"] = bins.astype(str)

        for policy in POLICY_ORDER:
            policy_df = decisions[decisions["policy"] == policy].merge(base, on="case_id", how="left")
            policy_df = policy_df.merge(feature_df[["case_id", f"{feature}_bin"]], on="case_id", how="left")
            for bucket, cell in policy_df.groupby(f"{feature}_bin", dropna=False):
                policy_correct = truthy(cell["is_correct_accept"])
                baseline_correct = truthy(cell["baseline_correct_accept"])
                policy_unsafe = truthy(cell["is_unsafe_accept"])
                baseline_unsafe = truthy(cell["baseline_unsafe_accept"])
                policy_recognized = cell["status"].eq("recognized")
                baseline_recognized = cell["baseline_status"].eq("recognized")
                rows.append(
                    {
                        "feature": feature,
                        "bucket": bucket,
                        "label": f"{feature} {bucket}",
                        "policy": policy,
                        "n": len(cell),
                        "recognized_rate_delta": ratio(policy_recognized.sum(), len(cell)) - ratio(baseline_recognized.sum(), len(cell)),
                        "safe_rate_delta": ratio(policy_correct.sum(), len(cell)) - ratio(baseline_correct.sum(), len(cell)),
                        "unsafe_rate_delta": ratio(policy_unsafe.sum(), len(cell)) - ratio(baseline_unsafe.sum(), len(cell)),
                    }
                )
    return pd.DataFrame(rows)


def plot_logic_waterfall(summary: pd.DataFrame, fig_dir: Path) -> str:
    fig, ax = plt.subplots(figsize=(12, 6))
    x = np.arange(len(summary))
    width = 0.18
    ax.bar(x - width * 1.5, summary["added_correct"], width, label="new correct accepts", color="#2c7a7b")
    ax.bar(x - width * 0.5, summary["removed_unsafe"], width, label="removed baseline unsafe", color="#3182ce")
    ax.bar(x + width * 0.5, -summary["lost_correct"], width, label="lost baseline correct", color="#dd6b20")
    ax.bar(x + width * 1.5, -summary["introduced_unsafe"], width, label="new unsafe accepts", color="#c53030")
    ax.axhline(0, color="#2d3748", linewidth=1)
    ax.set_xticks(x, summary["policy"], rotation=20, ha="right")
    ax.set_ylabel("Cases vs baseline")
    ax.set_title("Before/after correction effects by logic")
    ax.legend(ncol=2)
    fig.tight_layout()
    return save(fig, fig_dir / "correction_logic_waterfall.png")


def plot_status_transition_grid(decisions: pd.DataFrame, fig_dir: Path) -> str:
    base = decisions[decisions["policy"] == "baseline"][["case_id", "status"]].rename(columns={"status": "baseline_status"})
    policies = [policy for policy in POLICY_ORDER if policy in set(decisions["policy"])]
    fig, axes = plt.subplots(2, 3, figsize=(15, 9))
    axes = axes.ravel()
    for ax, policy in zip(axes, policies):
        merged = decisions[decisions["policy"] == policy][["case_id", "status"]].merge(base, on="case_id", how="left")
        table = pd.crosstab(merged["baseline_status"], merged["status"]).reindex(index=STATUS_ORDER, columns=STATUS_ORDER, fill_value=0)
        image = ax.imshow(table.to_numpy(), cmap="Blues")
        ax.set_xticks(range(len(STATUS_ORDER)), STATUS_ORDER, rotation=25, ha="right")
        ax.set_yticks(range(len(STATUS_ORDER)), STATUS_ORDER)
        ax.set_title(policy)
        annotate(ax, table.to_numpy())
        fig.colorbar(image, ax=ax, fraction=0.046, pad=0.04)
    for ax in axes[len(policies) :]:
        ax.set_axis_off()
    fig.suptitle("Baseline status to corrected status transitions", y=1.02)
    fig.tight_layout()
    return save(fig, fig_dir / "correction_status_transition_grid.png")


def plot_group_delta_heatmap(delta: pd.DataFrame, group_col: str, fig_dir: Path) -> str:
    fig, axes = plt.subplots(1, 2, figsize=(15, max(5, 0.55 * delta[group_col].nunique())))
    groups = sorted(delta[group_col].dropna().unique())
    policies = [policy for policy in POLICY_ORDER if policy in set(delta["policy"])]
    for ax, metric, title, cmap in [
        (axes[0], "recognized_rate_delta", "Recognition rate delta", "PiYG"),
        (axes[1], "unsafe_rate_delta", "Unsafe rate delta per case", "RdBu_r"),
    ]:
        table = delta.pivot_table(index=group_col, columns="policy", values=metric, aggfunc="mean").reindex(index=groups, columns=policies)
        vmax = max(0.01, np.nanmax(np.abs(table.to_numpy())))
        image = ax.imshow(table.to_numpy(), cmap=cmap, vmin=-vmax, vmax=vmax, aspect="auto")
        ax.set_xticks(range(len(policies)), policies, rotation=25, ha="right")
        ax.set_yticks(range(len(groups)), groups)
        ax.set_title(title)
        annotate_float(ax, table.to_numpy())
        fig.colorbar(image, ax=ax, fraction=0.046, pad=0.04)
    fig.tight_layout()
    return save(fig, fig_dir / f"correction_{group_col}_delta_heatmap.png")


def plot_feature_delta_heatmap(feature_delta: pd.DataFrame, fig_dir: Path) -> str:
    if feature_delta.empty:
        fig, ax = plt.subplots(figsize=(8, 5))
        ax.text(0.5, 0.5, "No feature delta rows", ha="center", va="center")
        return save(fig, fig_dir / "correction_feature_delta_heatmap.png")

    pivot_unsafe = feature_delta.pivot_table(index="label", columns="policy", values="unsafe_rate_delta", aggfunc="mean")
    top_labels = pivot_unsafe.abs().max(axis=1).sort_values(ascending=False).head(24).index
    policies = [policy for policy in POLICY_ORDER if policy in pivot_unsafe.columns]
    fig, axes = plt.subplots(1, 2, figsize=(16, max(7, 0.34 * len(top_labels))))
    for ax, metric, title in [
        (axes[0], "recognized_rate_delta", "Recognition delta by feature bin"),
        (axes[1], "unsafe_rate_delta", "Unsafe delta by feature bin"),
    ]:
        table = (
            feature_delta.pivot_table(index="label", columns="policy", values=metric, aggfunc="mean")
            .reindex(index=top_labels, columns=policies)
        )
        vmax = max(0.01, np.nanmax(np.abs(table.to_numpy())))
        image = ax.imshow(table.to_numpy(), cmap="RdBu_r", vmin=-vmax, vmax=vmax, aspect="auto")
        ax.set_xticks(range(len(policies)), policies, rotation=25, ha="right")
        ax.set_yticks(range(len(top_labels)), top_labels)
        ax.set_title(title)
        fig.colorbar(image, ax=ax, fraction=0.03, pad=0.02)
    fig.tight_layout()
    return save(fig, fig_dir / "correction_feature_delta_heatmap.png")


def plot_policy_metric_matrix(summary: pd.DataFrame, fig_dir: Path) -> str:
    metrics = [
        "recognized_rate",
        "unsafe_accept_rate",
        "safe_delta",
        "unsafe_delta",
        "status_changed",
        "accepted_family_changed",
    ]
    table = summary.set_index("policy")[metrics]
    normalized = table.copy()
    for col in metrics:
        max_abs = max(abs(normalized[col]).max(), 1)
        normalized[col] = normalized[col] / max_abs
    fig, ax = plt.subplots(figsize=(11, 6))
    image = ax.imshow(normalized.to_numpy(), cmap="RdBu_r", vmin=-1, vmax=1, aspect="auto")
    ax.set_xticks(range(len(metrics)), metrics, rotation=25, ha="right")
    ax.set_yticks(range(len(table.index)), table.index)
    for row in range(table.shape[0]):
        for col in range(table.shape[1]):
            value = table.iloc[row, col]
            label = f"{value:.2f}" if "rate" in table.columns[col] else f"{int(value)}"
            ax.text(col, row, label, ha="center", va="center", fontsize=8)
    ax.set_title("Correction logic metric matrix")
    fig.colorbar(image, ax=ax, fraction=0.046, pad=0.04)
    fig.tight_layout()
    return save(fig, fig_dir / "correction_policy_metric_matrix.png")


def plot_actual_survey_before_after(survey: pd.DataFrame, fig_dir: Path) -> str:
    policies = ["baseline", *POLICY_ORDER]
    rows = []
    for policy in policies:
        group = survey[survey["policy"] == policy]
        if group.empty:
            continue
        rows.append(
            {
                "policy": policy,
                "recognized": int(group["status"].eq("recognized").sum()),
                "correct": int(truthy(group["is_correct_accept"]).sum()),
                "unsafe": int(truthy(group["is_unsafe_accept"]).sum()),
            }
        )
    data = pd.DataFrame(rows)
    fig, ax = plt.subplots(figsize=(11, 5))
    x = np.arange(len(data))
    ax.bar(x - 0.25, data["recognized"], width=0.25, label="recognized", color="#2c7a7b")
    ax.bar(x, data["correct"], width=0.25, label="correct", color="#38a169")
    ax.bar(x + 0.25, data["unsafe"], width=0.25, label="unsafe", color="#c53030")
    ax.set_xticks(x, data["policy"], rotation=25, ha="right")
    ax.set_title("Actual survey direct before/after by correction logic")
    ax.legend()
    fig.tight_layout()
    return save(fig, fig_dir / "correction_actual_survey_before_after.png")


def plot_logic_reason_breakdown(decisions: pd.DataFrame, fig_dir: Path) -> str:
    subset = decisions[decisions["policy"].isin(POLICY_ORDER)].copy()
    subset["outcome"] = outcome_label(subset)
    reason_counts = (
        subset.groupby(["policy", "reason", "outcome"], dropna=False)
        .size()
        .rename("n")
        .reset_index()
    )
    policies = [policy for policy in POLICY_ORDER if policy in set(reason_counts["policy"])]
    fig, axes = plt.subplots(2, 3, figsize=(18, 9))
    axes = axes.ravel()
    colors = {"safe_accept": "#2c7a7b", "unsafe_accept": "#c53030", "held_or_rejected": "#a0aec0", "other_accept": "#d69e2e"}
    for ax, policy in zip(axes, policies):
        group = reason_counts[reason_counts["policy"] == policy]
        top = group.groupby("reason")["n"].sum().sort_values(ascending=False).head(6).index
        pivot = group[group["reason"].isin(top)].pivot_table(index="reason", columns="outcome", values="n", aggfunc="sum", fill_value=0).reindex(index=top)
        left = np.zeros(len(pivot))
        for outcome in ["safe_accept", "unsafe_accept", "held_or_rejected", "other_accept"]:
            values = pivot[outcome].to_numpy() if outcome in pivot else np.zeros(len(pivot))
            ax.barh(pivot.index, values, left=left, color=colors[outcome], label=outcome)
            left += values
        ax.invert_yaxis()
        ax.set_title(policy)
    for ax in axes[len(policies) :]:
        ax.set_axis_off()
    axes[min(len(policies), len(axes)) - 1].legend(loc="lower right", fontsize=8)
    fig.suptitle("Correction reason breakdown by outcome", y=1.02)
    fig.tight_layout()
    return save(fig, fig_dir / "correction_logic_reason_breakdown.png")


def write_report(out_dir: Path, summary: pd.DataFrame, source_delta: pd.DataFrame, family_delta: pd.DataFrame, figures: dict[str, str]) -> None:
    baseline_note = "baseline is the before state; every correction logic is compared against it."
    dynamic = summary[summary["policy"] == "ml_guardrail_dynamic"].iloc[0] if "ml_guardrail_dynamic" in set(summary["policy"]) else None
    ml_first = summary[summary["policy"] == "ml_first"].iloc[0] if "ml_first" in set(summary["policy"]) else None
    lines = [
        "# Correction Before/After Comparison",
        "",
        "## Summary",
        f"- {baseline_note}",
    ]
    if dynamic is not None:
        lines.append(
            f"- dynamic policy changed `{int(dynamic.status_changed)}` cases, recognized delta `{int(dynamic.recognized_delta)}`, safe delta `{int(dynamic.safe_delta)}`, unsafe delta `{int(dynamic.unsafe_delta)}`."
        )
    if ml_first is not None:
        lines.append(
            f"- ML-first is the broadest correction: recognized delta `{int(ml_first.recognized_delta)}`, unsafe delta `{int(ml_first.unsafe_delta)}`."
        )
    lines.extend(
        [
            "",
            "## Easy Interpretation",
            "- `tutorial_warmup` mainly lowers thresholds from tutorial captures; it increases accepts but also admits more borderline mistakes.",
            "- `ml_first` trusts shadow/tinyML most aggressively; it is useful as an upper-bound lift check, not as a safe final policy.",
            "- `stat_guardrail` is the hard brake; it removes risky recognitions but also loses correct accepts.",
            "- `ml_guardrail_final` applies v0 ML then blocks risk, but in this focused run it still carries many unsafe accepts.",
            "- `ml_guardrail_dynamic` is the current production default; it selectively opens survey-like and balanced cells while blocking high-risk boundary/confusion sources.",
            "",
            "## Figures",
        ]
    )
    for name, path in figures.items():
        lines.append(f"- [{name}]({Path(path).relative_to(out_dir).as_posix()})")
    lines.extend(
        [
            "",
            "## CSV Outputs",
            "- `correction_comparison_summary.csv`",
            "- `correction_source_delta.csv`",
            "- `correction_family_delta.csv`",
            "- `correction_feature_delta.csv`",
        ]
    )
    (out_dir / "correction_comparison_report.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


def update_summary_json(out_dir: Path, figures: dict[str, str]) -> None:
    path = out_dir / "analysis_summary.json"
    summary = json.loads(path.read_text(encoding="utf-8")) if path.exists() else {}
    existing = summary.get("figures", {})
    summary["figures"] = {
        **existing,
        **{name: str(Path(path_value).relative_to(out_dir).as_posix()) for name, path_value in figures.items()},
    }
    outputs = summary.get("outputFiles", {})
    outputs.update(
        {
            "correctionComparisonSummary": "correction_comparison_summary.csv",
            "correctionSourceDelta": "correction_source_delta.csv",
            "correctionFamilyDelta": "correction_family_delta.csv",
            "correctionFeatureDelta": "correction_feature_delta.csv",
            "correctionComparisonReport": "correction_comparison_report.md",
        }
    )
    summary["outputFiles"] = outputs
    path.write_text(json.dumps(summary, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def baseline_frame(decisions: pd.DataFrame) -> pd.DataFrame:
    return decisions[decisions["policy"] == "baseline"][
        ["case_id", "status", "accepted_family", "is_correct_accept", "is_unsafe_accept"]
    ].rename(
        columns={
            "status": "baseline_status",
            "accepted_family": "baseline_accepted_family",
            "is_correct_accept": "baseline_correct_accept",
            "is_unsafe_accept": "baseline_unsafe_accept",
        }
    )


def outcome_label(df: pd.DataFrame) -> np.ndarray:
    return np.select(
        [
            truthy(df["is_correct_accept"]),
            truthy(df["is_unsafe_accept"]),
            df["status"].ne("recognized"),
        ],
        ["safe_accept", "unsafe_accept", "held_or_rejected"],
        default="other_accept",
    )


def truthy(series: pd.Series) -> pd.Series:
    if series.dtype == bool:
        return series.fillna(False)
    return series.astype(str).str.lower().isin(["true", "1", "yes"])


def annotate(ax, data: np.ndarray) -> None:
    max_value = data.max() if data.size else 0
    for row in range(data.shape[0]):
        for col in range(data.shape[1]):
            ax.text(
                col,
                row,
                f"{int(data[row, col])}",
                ha="center",
                va="center",
                color="white" if max_value and data[row, col] > max_value * 0.55 else "black",
                fontsize=8,
            )


def annotate_float(ax, data: np.ndarray) -> None:
    finite = data[np.isfinite(data)]
    max_value = np.max(np.abs(finite)) if finite.size else 0
    for row in range(data.shape[0]):
        for col in range(data.shape[1]):
            value = data[row, col]
            if not np.isfinite(value):
                label = "n/a"
                color = "black"
            else:
                label = f"{value:+.2f}"
                color = "white" if max_value and abs(value) > max_value * 0.55 else "black"
            ax.text(col, row, label, ha="center", va="center", color=color, fontsize=8)


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


if __name__ == "__main__":
    main()
