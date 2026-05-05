import { describe, expect, it } from "vitest";

import { applyChemistryContract, createSurveyPhysicsState, tickSurveyPhysics } from "../src/survey/engines";

const quality = {
  closure: 0.8,
  symmetry: 0.7,
  smoothness: 0.82,
  tempo: 0.76,
  overshoot: 0.88,
  stability: 0.84,
  rotationBias: 0.16
};

describe("survey physics and chemistry engines", () => {
  it("moves a spell projectile and resolves dummy damage deterministically", () => {
    let state = createSurveyPhysicsState("fire", quality);

    for (let index = 0; index < 180 && !state.resolved; index += 1) {
      state = tickSurveyPhysics(state, 16.67);
    }

    expect(state.projectile.x).toBeGreaterThan(3);
    expect(state.resolved).toBe(true);
    expect(state.dummy.hp).toBeLessThan(100);
  });

  it("applies the ASCII chemistry transition contract", () => {
    const result = applyChemistryContract([".~fce"], "fire");

    expect(result.after).toEqual(["f.f.f"]);
    expect(result.events).toEqual([
      "0,0:dry->burning",
      "0,1:wet->dry",
      "0,3:chilled->dry",
      "0,4:charged->burning"
    ]);
  });
});
