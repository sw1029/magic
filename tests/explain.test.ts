import { describe, expect, it } from "vitest";
import { buildExplainNotes } from "../src/demo/explain";
import { buildDemoOutcomeCompare } from "../src/demo/outcome-summary";
import type { RecognitionResult } from "../src/recognizer/types";

describe("explain notes", () => {
  it("explains a recognized result with quality-driven outcome changes", () => {
    const result = makeResult({
      status: "recognized",
      canonicalFamily: "fire",
      topFamily: "fire",
      topScore: 0.82,
      secondScore: 0.61,
      closure: 0.88,
      adjustedQuality: {
        closure: 0.88,
        symmetry: 0.62,
        smoothness: 0.71,
        tempo: 0.8,
        overshoot: 0.66,
        stability: 0.39,
        rotationBias: 0.63
      }
    });
    const compare = buildDemoOutcomeCompare({
      family: "fire",
      status: "recognized",
      rawQuality: result.rawQuality,
      adjustedQuality: result.adjustedQuality,
      overlayOperators: ["electric_fork"]
    });

    const notes = buildExplainNotes(result, compare, true, null, null).map((item) => item.text);

    expect(notes.some((text) => text.includes("불꽃형으로"))).toBe(true);
    expect(notes.some((text) => text.includes("0.82"))).toBe(true);
    expect(notes.some((text) => text.includes("품질 반영으로"))).toBe(true);
  });

  it("explains an incomplete result as an open closure case", () => {
    const result = makeResult({
      status: "incomplete",
      topFamily: "earth",
      topScore: 0.62,
      secondScore: 0.28,
      closure: 0.44
    });
    const compare = buildDemoOutcomeCompare({
      family: null,
      status: "incomplete",
      rawQuality: result.rawQuality,
      adjustedQuality: result.adjustedQuality,
      overlayOperators: []
    });

    const notes = buildExplainNotes(result, compare, false, null, null).map((item) => item.text);

    expect(notes.some((text) => text.includes("닫힘이 부족"))).toBe(true);
    expect(notes.some((text) => text.includes("기본 결과감만 유지"))).toBe(true);
  });
});

function makeResult(input: {
  status: RecognitionResult["status"];
  canonicalFamily?: RecognitionResult["canonicalFamily"];
  topFamily: NonNullable<RecognitionResult["topCandidate"]>["family"];
  topScore: number;
  secondScore: number;
  closure: number;
  adjustedQuality?: RecognitionResult["adjustedQuality"];
}): RecognitionResult {
  const adjustedQuality =
    input.adjustedQuality ?? {
      closure: input.closure,
      symmetry: 0.52,
      smoothness: 0.54,
      tempo: 0.52,
      overshoot: 0.48,
      stability: 0.5,
      rotationBias: 0.5
    };

  return {
    status: input.status,
    sealed: Boolean(input.canonicalFamily),
    quality: {
      closure: input.closure,
      symmetry: 0.5,
      smoothness: 0.5,
      tempo: 0.5,
      overshoot: 0.5,
      stability: 0.5,
      rotationBias: 0.5
    },
    rawQuality: {
      closure: input.closure,
      symmetry: 0.5,
      smoothness: 0.5,
      tempo: 0.5,
      overshoot: 0.5,
      stability: 0.5,
      rotationBias: 0.5
    },
    adjustedQuality,
    qualityAdjustment: {
      closure: adjustedQuality.closure - input.closure,
      symmetry: adjustedQuality.symmetry - 0.5,
      smoothness: adjustedQuality.smoothness - 0.5,
      tempo: adjustedQuality.tempo - 0.5,
      overshoot: adjustedQuality.overshoot - 0.5,
      stability: adjustedQuality.stability - 0.5,
      rotationBias: adjustedQuality.rotationBias - 0.5
    },
    features: {
      strokeCount: 1,
      pointCount: 10,
      durationMs: 220,
      pathLength: 120,
      closureGap: 24,
      dominantCorners: 3,
      endpointClusters: 2,
      circularity: 0.5,
      fillRatio: 0.5,
      parallelism: 0.5,
      rawAngleRadians: 0
    },
    candidates: [
      {
        family: input.topFamily,
        score: input.topScore,
        templateDistance: 0.2,
        notes: []
      },
      {
        family: input.topFamily === "fire" ? "water" : "fire",
        score: input.secondScore,
        templateDistance: 0.28,
        notes: []
      }
    ],
    topCandidate: {
      family: input.topFamily,
      score: input.topScore,
      templateDistance: 0.2,
      notes: []
    },
    canonicalFamily: input.canonicalFamily,
    invalidReason: undefined,
    normalizedStrokes: []
  };
}
