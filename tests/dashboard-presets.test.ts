import { describe, expect, it } from "vitest";

import {
  DASHBOARD_MATRIX_FAMILIES,
  DASHBOARD_SCENARIO_PRESETS,
  accumulateDashboardFamilyMatrixSummaries,
  buildSyntheticRecipeFromRange,
  runDashboardComparison,
  runDashboardFamilyMatrix
} from "../src/demo/dashboard-presets";

describe("dashboard presets", () => {
  it("builds deterministic random recipes inside the selected range", () => {
    const preset = DASHBOARD_SCENARIO_PRESETS.find((item) => item.id === "jitter_noise_stress")!;
    const left = buildSyntheticRecipeFromRange(preset.range, 300);
    const right = buildSyntheticRecipeFromRange(preset.range, 300);

    expect(left).toEqual(right);
    expect(left.jitterPx).toBeGreaterThanOrEqual(preset.range.jitterPx.min);
    expect(left.jitterPx).toBeLessThanOrEqual(preset.range.jitterPx.max);
    expect(left.extraNoiseStrokeCount).toBeGreaterThanOrEqual(preset.range.extraNoiseStrokeCount.min);
    expect(left.extraNoiseStrokeCount).toBeLessThanOrEqual(preset.range.extraNoiseStrokeCount.max);
  });

  it("runs baseline, tutorial, and threshold comparison lanes on one seed set", () => {
    const preset = DASHBOARD_SCENARIO_PRESETS.find((item) => item.id === "threshold_variants")!;
    const recipe = buildSyntheticRecipeFromRange(preset.range, 88);
    const comparison = runDashboardComparison({
      presetId: preset.id,
      recipe,
      iterations: 16,
      seedStart: recipe.seed
    });

    expect(comparison.lanes.map((lane) => lane.id)).toEqual([
      "baseline",
      "tutorial",
      "threshold_strict",
      "threshold_loose"
    ]);
    expect(comparison.lanes.every((lane) => lane.summary.total === 16)).toBe(true);
    expect(comparison.lanes.every((lane) => lane.summary.samples[0]?.session.startedAt === comparison.lanes[0].summary.samples[0]?.session.startedAt)).toBe(true);
    expect(comparison.userSummary).toContain("threshold");
  });

  it("exposes only preset-driven dashboard scenarios", () => {
    expect(DASHBOARD_SCENARIO_PRESETS).toHaveLength(8);
    expect(DASHBOARD_SCENARIO_PRESETS.map((preset) => preset.id)).toContain("seal_ring_pass_fail");
    expect(DASHBOARD_SCENARIO_PRESETS.every((preset) => preset.description.length > 0)).toBe(true);
  });

  it("runs the selected n count for every built-in shape", () => {
    const summary = runDashboardFamilyMatrix({
      range: DASHBOARD_SCENARIO_PRESETS[0].range,
      iterations: 4,
      seedStart: 700
    });

    expect(summary.samples).toHaveLength(DASHBOARD_MATRIX_FAMILIES.length * 4);
    expect(summary.familySummaries.map((familySummary) => familySummary.family)).toEqual(DASHBOARD_MATRIX_FAMILIES);
    expect(summary.familySummaries.every((familySummary) => familySummary.total === 4)).toBe(true);
    expect(Object.values(summary.statusCounts).reduce((sum, value) => sum + value, 0)).toBe(summary.samples.length);
    expect(summary.userSummary).toContain("각각 4회");
  });

  it("summarizes overlap settings when generated inputs leave the stable range", () => {
    const summary = runDashboardFamilyMatrix({
      range: {
        ...DASHBOARD_SCENARIO_PRESETS[0].range,
        seed: 930,
        jitterPx: { min: 24, max: 24 },
        openGapRatio: { min: 0.52, max: 0.52 },
        rotationDeg: { min: 42, max: 42 },
        curveWarp: { min: 0.32, max: 0.32 },
        extraNoiseStrokeCount: { min: 5, max: 5 }
      },
      iterations: 3,
      seedStart: 930
    });

    expect(summary.overlapCells.length).toBeGreaterThan(0);
    expect(summary.overlapSettings.length).toBeGreaterThan(0);
    expect(summary.overlapCells[0].settingHint).toContain("열린 틈");
    expect(summary.overlapSettings[0].settingHint).toMatch(/떨림|열린 틈|회전|잡선/);
  });

  it("accumulates past family matrix runs until logs are cleared", () => {
    const first = runDashboardFamilyMatrix({
      range: DASHBOARD_SCENARIO_PRESETS[0].range,
      iterations: 3,
      seedStart: 1200
    });
    const second = runDashboardFamilyMatrix({
      range: {
        ...DASHBOARD_SCENARIO_PRESETS[0].range,
        seed: 2200,
        jitterPx: { min: 12, max: 12 },
        openGapRatio: { min: 0.2, max: 0.2 }
      },
      iterations: 5,
      seedStart: 2200
    });
    const accumulated = accumulateDashboardFamilyMatrixSummaries([second, first])!;

    expect(accumulated.samples).toHaveLength(first.samples.length + second.samples.length);
    expect(accumulated.familySummaries.map((familySummary) => familySummary.family)).toEqual(DASHBOARD_MATRIX_FAMILIES);
    expect(accumulated.familySummaries.every((familySummary) => familySummary.total === 8)).toBe(true);
    expect(Object.values(accumulated.statusCounts).reduce((sum, value) => sum + value, 0)).toBe(accumulated.samples.length);
    expect(accumulated.userSummary).toContain("2개 실행");
  });
});
