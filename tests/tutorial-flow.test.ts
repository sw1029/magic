import { describe, expect, it } from "vitest";
import { TUTORIAL_DEMO_STEPS, resolveNextTutorialStepIndex } from "../src/demo/tutorial-flow";

describe("tutorial flow", () => {
  it("keeps the compact demo order with void_cut before martial_axis", () => {
    const voidCutIndex = TUTORIAL_DEMO_STEPS.findIndex((step) => step.id === "void_cut_trace");
    const martialAxisIndex = TUTORIAL_DEMO_STEPS.findIndex((step) => step.id === "martial_axis_trace");

    expect(voidCutIndex).toBeGreaterThanOrEqual(0);
    expect(martialAxisIndex).toBeGreaterThan(voidCutIndex);
    expect(TUTORIAL_DEMO_STEPS[martialAxisIndex]?.requiresExistingOperator).toBe("void_cut");
  });

  it("moves to the next unfinished step", () => {
    const completed = ["fire_trace", "fire_variation", "water_trace"];
    const nextIndex = resolveNextTutorialStepIndex(completed, 1);

    expect(TUTORIAL_DEMO_STEPS[nextIndex]?.id).toBe("water_variation");
  });
});
