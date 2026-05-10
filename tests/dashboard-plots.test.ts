import { describe, expect, it } from "vitest";

import { runDashboardBatch } from "../src/demo/dashboard-batch";
import { buildDashboardPlotModel } from "../src/demo/dashboard-plots";

describe("dashboard plot model", () => {
  it("turns repeated made inputs into bars, heat cells, and score points", () => {
    const summary = runDashboardBatch({
      recipe: {
        family: "fire",
        seed: 300,
        jitterPx: 6,
        openGapRatio: 0.2,
        curveWarp: 0.1,
        pointDensity: 4
      },
      iterations: 24
    });

    const model = buildDashboardPlotModel(summary);

    expect(model.statusBars.map((bar) => bar.label)).toContain("인정됨");
    expect(model.qualityBars.map((bar) => bar.label)).toEqual(expect.arrayContaining(["닫힘", "부드러움", "안정감", "기울기"]));
    expect(model.heatmap.length).toBeGreaterThan(0);
    expect(model.scorePoints).toHaveLength(24);
    expect(model.scorePoints.every((point) => point.x >= 0 && point.x <= 1 && point.y >= 0 && point.y <= 1)).toBe(true);
  });
});
