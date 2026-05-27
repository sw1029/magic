from __future__ import annotations

import json
import math
import sys
from pathlib import Path

import matplotlib.pyplot as plt
import numpy as np
import pandas as pd


STATUS_ORDER = ["recognized", "ambiguous", "incomplete", "invalid"]
FAMILY_ORDER = ["wind", "earth", "fire", "water", "life", "none"]
QUALITY_COLUMNS = ["closure", "symmetry", "smoothness", "tempo", "overshoot", "stability", "rotation_bias"]
RECIPE_COLUMNS = ["jitter_px", "open_gap_ratio", "rotation_deg", "curve_warp", "extra_noise_stroke_count"]


def main() -> None:
    if len(sys.argv) < 2:
        raise SystemExit("usage: python scripts/survey-deep-dive-plots.py <analysis-output-dir>")

    out_dir = Path(sys.argv[1]).resolve()
    fig_dir = out_dir / "figures"
    fig_dir.mkdir(parents=True, exist_ok=True)

    summary = read_json(out_dir / "analysis_summary.json")
    synthetic = pd.read_csv(out_dir / "synthetic_cases.csv")
    survey = pd.read_csv(out_dir / "survey_inputs.csv")
    threshold = pd.read_csv(out_dir / "respondent_threshold_changes.csv")
    overlap = pd.read_csv(out_dir / "synthetic_overlap_cells.csv")
    preset_family = pd.read_csv(out_dir / "synthetic_summary_by_preset_family.csv")

    figures = {
        "synthetic_status_by_preset": plot_synthetic_status_by_preset(synthetic, fig_dir),
        "synthetic_confusion_heatmap": plot_synthetic_confusion_heatmap(synthetic, fig_dir),
        "synthetic_feature_overlap_heatmap": plot_synthetic_feature_overlap_heatmap(overlap, fig_dir),
        "synthetic_score_hexbin": plot_synthetic_score_hexbin(synthetic, fig_dir),
        "threshold_delta_distribution": plot_threshold_delta_distribution(synthetic, threshold, fig_dir),
        "survey_elapsed_quality": plot_survey_elapsed_quality(survey, fig_dir),
        "respondent_threshold_slope": plot_respondent_threshold_slope(threshold, fig_dir),
        "actual_vs_synthetic_feature_overlay": plot_actual_vs_synthetic_feature_overlay(survey, synthetic, fig_dir),
        "synthetic_failure_odds": plot_synthetic_failure_odds(preset_family, fig_dir),
    }

    write_report(out_dir, summary, synthetic, survey, threshold, overlap, preset_family, figures)
    validate_figures(figures)
    print(json.dumps({"out_dir": str(out_dir), "figures": len(figures)}, ensure_ascii=False))


def plot_synthetic_status_by_preset(df: pd.DataFrame, fig_dir: Path) -> str:
    table = pd.crosstab(df["preset_id"], df["baseline_status"], normalize="index").reindex(columns=STATUS_ORDER, fill_value=0)
    fig, ax = plt.subplots(figsize=(12, 6))
    bottom = np.zeros(len(table))
    colors = ["#2c7a7b", "#d69e2e", "#dd6b20", "#718096"]
    for status, color in zip(STATUS_ORDER, colors):
        values = table[status].to_numpy()
        ax.bar(table.index, values, bottom=bottom, label=status, color=color)
        bottom += values
    ax.set_title("Synthetic 10k baseline status distribution by dashboard preset")
    ax.set_ylabel("Share")
    ax.set_ylim(0, 1)
    ax.tick_params(axis="x", rotation=35)
    ax.legend(ncol=4, loc="upper right")
    fig.tight_layout()
    return save(fig, fig_dir / "synthetic_status_by_preset.png")


def plot_synthetic_confusion_heatmap(df: pd.DataFrame, fig_dir: Path) -> str:
    table = pd.crosstab(df["expected_family"], df["baseline_actual_family"]).reindex(index=FAMILY_ORDER[:-1], columns=FAMILY_ORDER, fill_value=0)
    fig, ax = plt.subplots(figsize=(9, 6))
    image = ax.imshow(table.to_numpy(), cmap="YlGnBu")
    ax.set_title("Synthetic confusion heatmap")
    ax.set_xlabel("Actual/top family")
    ax.set_ylabel("Expected family")
    ax.set_xticks(range(len(table.columns)), table.columns, rotation=35)
    ax.set_yticks(range(len(table.index)), table.index)
    annotate_heatmap(ax, table.to_numpy())
    fig.colorbar(image, ax=ax, fraction=0.046, pad=0.04)
    fig.tight_layout()
    return save(fig, fig_dir / "synthetic_confusion_heatmap.png")


def plot_synthetic_feature_overlap_heatmap(overlap: pd.DataFrame, fig_dir: Path) -> str:
    if overlap.empty:
        matrix = pd.DataFrame([[0]], index=["no-overlap"], columns=["n"])
    else:
        top = overlap.head(15).copy()
        labels = top.apply(lambda row: f"{row['preset_id']} | {row['expected_family']}->{row['actual_family']} | {row['status']}", axis=1)
        columns = [
            "avg_jitter_px",
            "avg_open_gap_ratio",
            "avg_rotation_deg",
            "avg_curve_warp",
            "avg_noise_stroke_count",
            "avg_closure",
            "avg_smoothness",
            "avg_stability",
            "avg_rotation_bias",
        ]
        matrix = top[columns].astype(float)
        matrix = (matrix - matrix.mean()) / matrix.std(ddof=0).replace(0, 1)
        matrix.index = labels
    fig, ax = plt.subplots(figsize=(12, max(5, 0.4 * len(matrix))))
    image = ax.imshow(matrix.to_numpy(), cmap="coolwarm", vmin=-2, vmax=2, aspect="auto")
    ax.set_title("Top synthetic overlap cells: standardized feature profile")
    ax.set_xticks(range(len(matrix.columns)), matrix.columns, rotation=35, ha="right")
    ax.set_yticks(range(len(matrix.index)), matrix.index)
    fig.colorbar(image, ax=ax, fraction=0.025, pad=0.02)
    fig.tight_layout()
    return save(fig, fig_dir / "synthetic_feature_overlap_heatmap.png")


def plot_synthetic_score_hexbin(df: pd.DataFrame, fig_dir: Path) -> str:
    fig, ax = plt.subplots(figsize=(8, 6))
    hb = ax.hexbin(df["baseline_score_gap"], df["baseline_top_score"], gridsize=45, cmap="viridis", mincnt=1)
    ax.axhline(0.70, color="#c53030", linestyle="--", linewidth=1, label="score 0.70")
    ax.axvline(0.15, color="#805ad5", linestyle="--", linewidth=1, label="gap 0.15")
    ax.set_title("Synthetic score-gap density and baseline thresholds")
    ax.set_xlabel("Top score gap")
    ax.set_ylabel("Top score")
    ax.legend(loc="lower right")
    fig.colorbar(hb, ax=ax, label="case count")
    fig.tight_layout()
    return save(fig, fig_dir / "synthetic_score_hexbin.png")


def plot_threshold_delta_distribution(synthetic: pd.DataFrame, threshold: pd.DataFrame, fig_dir: Path) -> str:
    direct = threshold[threshold["capture_kind"] == "direct"].copy()
    fig, axes = plt.subplots(1, 2, figsize=(12, 5))
    axes[0].hist(synthetic["personalized_effective_threshold_bias"], bins=20, color="#2b6cb0", alpha=0.8)
    axes[0].set_title("Synthetic aggregate tutorial effective threshold")
    axes[0].set_xlabel("Effective threshold bias")
    axes[0].set_ylabel("Cases")

    if direct.empty:
        axes[1].text(0.5, 0.5, "No direct survey rows", ha="center", va="center")
    else:
        jitter = np.linspace(-0.03, 0.03, len(direct))
        axes[1].scatter(np.zeros(len(direct)) + jitter, direct["baseline_top_score"], label="before", color="#718096")
        axes[1].scatter(np.ones(len(direct)) + jitter, direct["personalized_top_score"], label="after", color="#2c7a7b")
        for offset, (_, row) in zip(jitter, direct.iterrows()):
            axes[1].plot([0 + offset, 1 + offset], [row["baseline_top_score"], row["personalized_top_score"]], color="#a0aec0", alpha=0.5)
        axes[1].set_xticks([0, 1], ["baseline", "personalized"])
        axes[1].set_ylabel("Top score")
        axes[1].set_title("Survey direct inputs: respondent tutorial before/after")
        axes[1].legend()
    fig.tight_layout()
    return save(fig, fig_dir / "threshold_delta_distribution.png")


def plot_survey_elapsed_quality(survey: pd.DataFrame, fig_dir: Path) -> str:
    direct = survey[survey["capture_kind"] == "direct"].copy()
    fig, axes = plt.subplots(1, 2, figsize=(12, 5))
    if direct.empty:
        axes[0].text(0.5, 0.5, "No direct rows", ha="center", va="center")
        axes[1].text(0.5, 0.5, "No direct rows", ha="center", va="center")
    else:
        words = ["fire", "water", "wind"]
        elapsed = [direct.loc[direct["target_word"] == word, "elapsed_ms"].dropna() / 1000 for word in words]
        axes[0].boxplot(elapsed, tick_labels=words, showfliers=True)
        axes[0].set_title("Survey direct drawing elapsed time")
        axes[0].set_ylabel("Seconds")
        for index, word in enumerate(words, start=1):
            vals = direct.loc[direct["target_word"] == word, "elapsed_ms"].dropna() / 1000
            axes[0].scatter(np.full(len(vals), index) + np.random.default_rng(7).normal(0, 0.03, len(vals)), vals, color="#2d3748", s=18)

        smoothness = [direct.loc[direct["target_word"] == word, "smoothness"].dropna() for word in words]
        axes[1].boxplot(smoothness, tick_labels=words, showfliers=True)
        axes[1].set_title("Survey direct smoothness quality")
        axes[1].set_ylabel("Smoothness")
    fig.tight_layout()
    return save(fig, fig_dir / "survey_elapsed_quality.png")


def plot_respondent_threshold_slope(threshold: pd.DataFrame, fig_dir: Path) -> str:
    direct = threshold[threshold["capture_kind"] == "direct"].copy()
    direct = direct.sort_values(["submission_id", "target_word"])
    fig, ax = plt.subplots(figsize=(10, max(5, len(direct) * 0.22)))
    if direct.empty:
        ax.text(0.5, 0.5, "No respondent threshold rows", ha="center", va="center")
    else:
        y = np.arange(len(direct))
        ax.scatter(direct["baseline_top_score"], y, color="#718096", label="baseline")
        ax.scatter(direct["personalized_top_score"], y, color="#2c7a7b", label="personalized")
        for yi, (_, row) in zip(y, direct.iterrows()):
            ax.plot([row["baseline_top_score"], row["personalized_top_score"]], [yi, yi], color="#a0aec0", linewidth=1)
        labels = direct.apply(lambda row: f"{short_id(row['submission_id'])} {row['target_word']}", axis=1)
        ax.set_yticks(y, labels)
        ax.set_xlabel("Top score")
        ax.set_title("Respondent tutorial effect on each direct input")
        ax.legend(loc="lower right")
    fig.tight_layout()
    return save(fig, fig_dir / "respondent_threshold_slope.png")


def plot_actual_vs_synthetic_feature_overlay(survey: pd.DataFrame, synthetic: pd.DataFrame, fig_dir: Path) -> str:
    direct = survey[survey["capture_kind"] == "direct"].copy()
    fig, axes = plt.subplots(2, 2, figsize=(12, 8))
    for ax, column in zip(axes.flat, ["closure", "smoothness", "stability", "rotation_bias"]):
        ax.hist(synthetic[column].dropna(), bins=35, density=True, alpha=0.55, label="synthetic", color="#4299e1")
        if not direct.empty:
            ax.hist(direct[column].dropna(), bins=12, density=True, alpha=0.65, label="survey direct", color="#ed8936")
        ax.set_title(column)
        ax.set_ylabel("Density")
    axes.flat[0].legend()
    fig.suptitle("Actual survey vs synthetic quality feature distributions", y=1.02)
    fig.tight_layout()
    return save(fig, fig_dir / "actual_vs_synthetic_feature_overlay.png")


def plot_synthetic_failure_odds(preset_family: pd.DataFrame, fig_dir: Path) -> str:
    df = preset_family.copy()
    df["failure_rate"] = 1 - df["recognized_rate"]
    pivot = df.pivot(index="preset_id", columns="expected_family", values="failure_rate").fillna(0)
    fig, ax = plt.subplots(figsize=(9, 6))
    image = ax.imshow(pivot.to_numpy(), cmap="OrRd", vmin=0, vmax=max(0.01, pivot.to_numpy().max()))
    ax.set_title("Synthetic failure/overlap risk by preset and family")
    ax.set_xticks(range(len(pivot.columns)), pivot.columns, rotation=35)
    ax.set_yticks(range(len(pivot.index)), pivot.index)
    annotate_heatmap(ax, np.round(pivot.to_numpy(), 2), fmt="{:.2f}")
    fig.colorbar(image, ax=ax, fraction=0.046, pad=0.04)
    fig.tight_layout()
    return save(fig, fig_dir / "synthetic_failure_odds.png")


def write_report(
    out_dir: Path,
    summary: dict,
    synthetic: pd.DataFrame,
    survey: pd.DataFrame,
    threshold: pd.DataFrame,
    overlap: pd.DataFrame,
    preset_family: pd.DataFrame,
    figures: dict[str, str],
) -> None:
    raw = summary["rawRecordCount"]
    dedup = summary["dedupRecordCount"]
    removed = summary["duplicateRowsRemoved"]
    synthetic_n = summary["synthetic"]["actualCaseCount"]
    survey_direct = summary["survey"]["directInputCount"]
    survey_tutorial = summary["survey"]["tutorialInputCount"]
    changed_survey = summary["threshold"]["surveyChangedRows"]
    changed_synth = summary["threshold"]["syntheticChangedRows"]

    top_overlap = overlap.head(8)
    overlap_lines = [
        f"- `{row.preset_id}` `{row.expected_family}->{row.actual_family}` `{row.status}`: n={int(row.n)}, "
        f"jitter={row.avg_jitter_px:.2f}, openGap={row.avg_open_gap_ratio:.3f}, rotation={row.avg_rotation_deg:.1f}, "
        f"scoreGap={row.avg_score_gap:.3f}"
        for row in top_overlap.itertuples()
    ]
    if not overlap_lines:
        overlap_lines = ["- 겹침/실패 cell이 없습니다."]

    direct = survey[survey["capture_kind"] == "direct"]
    direct_status = direct["baseline_status"].value_counts().to_dict()
    group_summary = pd.read_csv(out_dir / "survey_group_summary.csv")
    group_lines = [
        f"- `{row.experiment_group}` `{row.capture_kind}`: n={int(row.n)}, recognized={row.recognized_rate:.2%}, "
        f"avgTop={row.avg_top_score:.3f}, elapsed={row.avg_elapsed_ms / 1000:.2f}s"
        for row in group_summary.itertuples()
    ]

    fig_lines = [f"- [{name}]({Path(path).relative_to(out_dir).as_posix()})" for name, path in figures.items()]

    report = f"""# Survey Deep-Dive Analysis

## Executive Summary
- 입력 병합 결과 raw `{raw}`건, dedup `{dedup}`건이며, fingerprint 중복 `{removed}`건을 제거했습니다.
- 실제 survey 입력은 direct `{survey_direct}`개, tutorial `{survey_tutorial}`개를 기존 recognizer로 재평가했습니다.
- dashboard preset/family stratified mix로 synthetic `{synthetic_n}`개를 생성했습니다.
- respondent별 tutorial profile 적용 후 direct survey 변화는 `{changed_survey}`개, aggregate tutorial profile 적용 후 synthetic 변화는 `{changed_synth}`개입니다.
- survey 표본 수가 작아 그룹 차이는 exploratory/descriptive로 해석해야 합니다.

## Data Quality
- 두 ndjson 파일을 병합했고, 연락처 ndjson는 분석에서 제외했습니다.
- dedup fingerprint는 `submissionId`, `receivedAt`, `startedAt`, `completedAt`, `interactionMetrics`을 제외한 semantic payload 구조로 계산했습니다.
- legacy v1 `strokes`, v3 `[x,y]`, v6/v8 `[x,y,t]` shapeTrace를 모두 `StrokeSession`으로 정규화했습니다.

## Survey Input Findings
- direct baseline status counts: `{direct_status}`.
- respondent tutorial profile은 각 응답자의 tutorialCaptures만 사용했습니다. high/medium validation이 부족한 경우 threshold bias는 정책상 제한됩니다.
- tinyML 관찰은 `shadow_mode`, `ml_confidence_gate`, `effective_threshold_bias` 컬럼에 기록했습니다.

## Group Descriptives
{chr(10).join(group_lines)}

## Synthetic Overlap and Feature Findings
- synthetic은 8개 dashboard preset x 5개 family x 250개로 정확히 10000개입니다.
- 실패/겹침 cell은 `recognized`가 아니거나 actual family가 expected와 다른 경우로 정의했습니다.
{chr(10).join(overlap_lines)}

## Threshold Dynamics
- survey direct 평균 effective threshold bias: `{summary['threshold']['surveyAverageEffectiveThresholdBias']:.6f}`.
- synthetic aggregate 평균 effective threshold bias: `{summary['threshold']['syntheticAverageEffectiveThresholdBias']:.6f}`.
- strict/loose threshold variant는 baseline 점수/마진 규칙을 재분류한 counterfactual이며, actual recognizer decision을 변경하지 않습니다.

## Figures
{chr(10).join(fig_lines)}

## Output Files
- `analysis_summary.json`
- `duplicate_clusters.csv`
- `survey_inputs.csv`
- `respondent_threshold_changes.csv`
- `synthetic_cases.csv`
- `synthetic_summary_by_preset_family.csv`
- `synthetic_overlap_cells.csv`
- `survey_group_summary.csv`
"""
    (out_dir / "analysis_report.md").write_text(report, encoding="utf-8")


def annotate_heatmap(ax, data: np.ndarray, fmt: str = "{:.0f}") -> None:
    if data.size > 100:
        return
    max_value = np.nanmax(data) if data.size else 0
    for row in range(data.shape[0]):
        for col in range(data.shape[1]):
            value = data[row, col]
            color = "white" if max_value and value > max_value * 0.55 else "black"
            ax.text(col, row, fmt.format(value), ha="center", va="center", color=color, fontsize=8)


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


def short_id(value: str) -> str:
    return str(value)[:8]


if __name__ == "__main__":
    main()
