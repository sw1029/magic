import { describe, expect, it } from "vitest";

import {
  buildMagicWhatIfScenarios,
  resolveMagicWhatIfScenario,
  summarizeWhatIfImpact
} from "../src/demo/what-if";

const RAW_COPY_TERMS = [/threshold/i, /gate/i, /rerank/i, /ml/i, /machine learning/i];

describe("magic what-if scenarios", () => {
  it("generates structure, relation, and placement scenarios from datacards", () => {
    const scenarios = buildMagicWhatIfScenarios();

    expect(scenarios.length).toBeGreaterThan(0);
    expect(scenarios.some((scenario) => scenario.kind === "family_shape_mutation" && scenario.dimension === "structure")).toBe(true);
    expect(scenarios.some((scenario) => scenario.kind === "operator_anchor_movement" && scenario.dimension === "placement")).toBe(true);
    expect(scenarios.some((scenario) => scenario.kind === "underscale_risk")).toBe(true);
    expect(scenarios.some((scenario) => scenario.kind === "off_anchor_risk")).toBe(true);
  });

  it("includes the martial axis dependency scenario that requires void cut first", () => {
    const scenarios = buildMagicWhatIfScenarios();
    const scenario = scenarios.find(
      (candidate) => candidate.kind === "dependency_ordering" && candidate.cardId === "operator:martial_axis"
    );

    expect(scenario).toBeDefined();
    expect(scenario?.dimension).toBe("relation");
    expect(scenario?.requires).toMatchObject({ operator: "void_cut", cardId: "operator:void_cut" });
    expect(scenario?.relatedCardIds).toEqual(["operator:void_cut", "operator:martial_axis"]);
  });

  it("produces HCI-friendly Korean copy without raw model terms", () => {
    const scenarios = buildMagicWhatIfScenarios();
    const copy = scenarios
      .flatMap((scenario) => [
        scenario.title,
        scenario.label,
        scenario.prompt,
        scenario.impact.headline,
        scenario.impact.detail,
        scenario.impact.actionCopy,
        scenario.actualLane.label,
        scenario.actualLane.copy,
        scenario.whatIfLane.label,
        scenario.whatIfLane.copy,
        summarizeWhatIfImpact(scenario)
      ])
      .join("\n");

    expect(copy).toMatch(/[가-힣]/);
    for (const rawTerm of RAW_COPY_TERMS) {
      expect(copy).not.toMatch(rawTerm);
    }
  });

  it("returns undefined or safe fallback for an unknown scenario", () => {
    expect(resolveMagicWhatIfScenario("missing:scenario")).toBeUndefined();
    expect(summarizeWhatIfImpact("missing:scenario")).toContain("현재 판정은 그대로 유지");
  });

  it("marks the actual lane as non-mutating", () => {
    const [scenario] = buildMagicWhatIfScenarios();

    expect(scenario).toBeDefined();
    expect(scenario?.actualLane.nonMutating).toBe(true);
    expect(scenario?.actualLane.mutatesRecognizerDecision).toBe(false);
    expect(scenario?.actualLane.changesActualDecision).toBe(false);
    expect(scenario?.lanes.actual).toBe(scenario?.actualLane);
  });
});
