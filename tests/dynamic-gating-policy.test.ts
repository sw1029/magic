import { describe, expect, it } from "vitest";

import { applyDynamicRecognitionPolicy, evaluateDynamicRecognitionPolicy } from "../src/recognizer/dynamic-gating";
import type { GlyphFamily, RecognitionCandidate, RecognitionResult } from "../src/recognizer/types";

describe("dynamic gating policy", () => {
  it("promotes stable wind/water-like cells without expected-family access", () => {
    const result = applyDynamicRecognitionPolicy(
      makeResult({
        status: "ambiguous",
        topFamily: "wind",
        topScore: 0.76,
        secondScore: 0.66,
        confidence: 0.94,
        shadowFamily: "wind"
      }),
      { sourceHint: "balanced_holdout" }
    );

    expect(result.status).toBe("recognized");
    expect(result.canonicalFamily).toBe("wind");
    expect(result.dynamicPolicy?.reason).toBe("dynamic_ml_promotion");
  });

  it("holds earth/fire high-risk cells when calibrated precision is not enough", () => {
    const evaluation = evaluateDynamicRecognitionPolicy(
      makeResult({
        status: "ambiguous",
        topFamily: "earth",
        topScore: 0.82,
        secondScore: 0.78,
        confidence: 0.99,
        shadowFamily: "earth",
        closure: 0.12,
        stability: 0.5
      }),
      { sourceHint: "survey_mutation_valid" }
    );

    expect(evaluation.status).toBe("ambiguous");
    expect(evaluation.summary.riskLevel).toBe("block");
    expect(evaluation.summary.reason).toBe("dynamic_keep_pending");
  });

  it("keeps legacy mode as an opt-out path", () => {
    const legacy = applyDynamicRecognitionPolicy(
      makeResult({
        status: "ambiguous",
        topFamily: "water",
        topScore: 0.78,
        secondScore: 0.66,
        confidence: 0.96,
        shadowFamily: "water"
      }),
      { mode: "legacy" }
    );

    expect(legacy.status).toBe("ambiguous");
    expect(legacy.dynamicPolicy?.mode).toBe("legacy");
  });
});

function makeResult(input: {
  status: RecognitionResult["status"];
  topFamily: GlyphFamily;
  topScore: number;
  secondScore: number;
  confidence: number;
  shadowFamily: GlyphFamily;
  closure?: number;
  stability?: number;
}): RecognitionResult {
  const top = candidate(input.topFamily, input.topScore);
  const secondFamily = input.topFamily === "wind" ? "water" : "wind";
  const candidates = [top, candidate(secondFamily, input.secondScore)];
  return {
    status: input.status,
    sealed: true,
    quality: quality(input.closure, input.stability),
    rawQuality: quality(input.closure, input.stability),
    adjustedQuality: quality(input.closure, input.stability),
    qualityAdjustment: quality(1, 1),
    features: {
      strokeCount: 3,
      pointCount: 24,
      durationMs: 420,
      pathLength: 1200,
      closureGap: 0,
      dominantCorners: 3,
      endpointClusters: 3,
      circularity: 0.5,
      fillRatio: 0.5,
      parallelism: 0.8,
      rawAngleRadians: 0
    },
    candidates,
    topCandidate: top,
    normalizedStrokes: [],
    shadow: {
      mode: "shadow",
      shadowTopLabel: input.shadowFamily,
      actualTopLabel: input.topFamily,
      actualStatus: input.status,
      shadowStatus: "recognized",
      decisionChanged: false,
      statusChanged: false,
      calibratedConfidence: input.confidence,
      candidates: [
        { label: input.shadowFamily, heuristicScore: input.topScore, shadowScore: input.topScore + 0.08, delta: 0.08, probability: 0.92 },
        { label: secondFamily, heuristicScore: input.secondScore, shadowScore: input.secondScore, delta: 0, probability: 0.12 }
      ]
    }
  };
}

function candidate(family: GlyphFamily, score: number): RecognitionCandidate {
  return {
    family,
    score,
    templateDistance: 0.1,
    notes: []
  };
}

function quality(closure = 0.9, stability = 0.9): RecognitionResult["quality"] {
  return {
    closure,
    symmetry: 0.8,
    smoothness: 0.65,
    tempo: 0.8,
    overshoot: 0.9,
    stability,
    rotationBias: 0.1
  };
}
