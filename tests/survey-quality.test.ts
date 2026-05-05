import { describe, expect, it } from "vitest";

import { buildQualityExplanations } from "../src/survey/quality-explain";

describe("survey quality explanations", () => {
  it("creates focused tutorial explanations for the survey metrics", () => {
    const explanations = buildQualityExplanations({
      closure: 0.3,
      symmetry: 0.7,
      smoothness: 0.8,
      tempo: 0.9,
      overshoot: 0.8,
      stability: 0.4,
      rotationBias: 0.2
    });

    expect(explanations.map((item) => item.key)).toEqual([
      "closure",
      "smoothness",
      "tempo",
      "stability",
      "rotationBias"
    ]);
    expect(explanations[0].summary).toContain("끝점");
  });
});
