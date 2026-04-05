import { describe, expect, it } from "vitest";
import { applyDemoViewPreset, createDemoViewState } from "../src/demo-layer";
import { buildDemoOutcomeCompare, getChangedOutcomeMetrics } from "../src/demo/outcome-summary";

describe("demo layer", () => {
  it("keeps family fixed while quality on/off only changes outcome metrics", () => {
    const compare = buildDemoOutcomeCompare({
      family: "fire",
      status: "recognized",
      rawQuality: {
        closure: 0.82,
        symmetry: 0.58,
        smoothness: 0.64,
        tempo: 0.84,
        overshoot: 0.72,
        stability: 0.34,
        rotationBias: 0.7
      },
      adjustedQuality: {
        closure: 0.88,
        symmetry: 0.62,
        smoothness: 0.7,
        tempo: 0.8,
        overshoot: 0.66,
        stability: 0.4,
        rotationBias: 0.6
      },
      overlayOperators: ["electric_fork"]
    });

    expect(compare.off.family).toBe("fire");
    expect(compare.on.family).toBe("fire");
    expect(compare.on.risk).not.toBe(compare.off.risk);
    expect(compare.delta.output !== 0 || compare.delta.control !== 0 || compare.delta.stability !== 0).toBe(true);
    expect(getChangedOutcomeMetrics(compare).length).toBeGreaterThan(0);
    expect(compare.on.summary).toContain("같은 모양은 같은 종류로 유지");
  });

  it("applies explain and workshop presets without disabling quality influence", () => {
    const initial = createDemoViewState("clean");
    const explain = applyDemoViewPreset(initial, "explain");
    const workshop = applyDemoViewPreset(explain, "workshop");

    expect(initial.qualityInfluence).toBe(true);
    expect(initial.showPersonalizationPanel).toBe(false);
    expect(initial.showTutorialFlowPanel).toBe(false);
    expect(explain.explainResult).toBe(true);
    expect(explain.showTutorialFlowPanel).toBe(true);
    expect(explain.showPersonalizationPanel).toBe(true);
    expect(explain.showExemplarPanel).toBe(true);
    expect(explain.showLogViewer).toBe(false);
    expect(workshop.analysisOverlay).toBe(true);
    expect(workshop.showTutorialFlowPanel).toBe(true);
    expect(workshop.showLogViewer).toBe(true);
    expect(workshop.qualityInfluence).toBe(true);
  });
});
