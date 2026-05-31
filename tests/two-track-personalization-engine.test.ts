import { describe, expect, it } from "vitest";
import {
  calculateTinyMlTwoTrackCorrection,
  calculateTutorialThresholdState,
  decideWithTwoTrackPersonalization,
  resolveTwoTrackConfusion,
  summarizeTwoTrackRecognition,
  type ConfusionSnapshot,
  type RecognitionSummary,
  type ThresholdState
} from "../src/recognizer/two-track-personalization-engine";
import type { DatacardRecognitionResult, DatacardTinyMlContrastDecision } from "../src/recognizer/datacard-shape-lab";

describe("two-track personalization engine", () => {
  it("keeps tutorial threshold state independent from survey UI", () => {
    const targetPresetIds = ["custom:eval_rect", "custom:eval_line"];
    const captures = Array.from({ length: 3 }, (_, index) => ({
      targetPresetId: "custom:eval_rect",
      recognition: { score: 0.82 + index * 0.01, unsafeRisk: 0.04, flipRisk: 0.08 },
      confusion: confusion({ targetRank: 1, confusionScore: 0.02 })
    }));

    const threshold = calculateTutorialThresholdState({ captures, evals: [], targetPresetIds });

    expect(threshold.captureCount).toBe(3);
    expect(threshold.targetAdjustments["custom:eval_rect"].captureCount).toBe(3);
    expect(threshold.targetAdjustments["custom:eval_line"].captureCount).toBe(0);
    expect(threshold.acceptThreshold).toBeLessThan(0.76);
  });

  it("blocks unsafe priority flips with the shadow gate", () => {
    const threshold = thresholdState();
    const correction = calculateTinyMlTwoTrackCorrection({
      summary: summary({ score: 0.69, shadowConfidence: 0.73, meaningConfidence: 0.95, unsafeRisk: 0.38, flipRisk: 0.12 }),
      contrast: contrast({ eligibleForActual: true, actualScoreLift: 0.08, relationRisk: 0.04 }),
      threshold,
      confusion: confusion({ targetRank: 1, confusionScore: 0.01 }),
      targetPresetId: "custom:eval_rect"
    });

    expect(correction.blockPriorityFlip).toBe(true);
    expect(correction.selectedTrack).toBe("shadow_gate");
    expect(correction.finalDecision).not.toBe("accept");
  });

  it("lets meaning recovery promote a safe hold decision", () => {
    const threshold = thresholdState({
      globalScoreLift: 0.1,
      unsafeLimit: 0.36,
      flipLimit: 0.55,
      targetAdjustments: {
        "custom:eval_rect": {
          captureCount: 3,
          evalCount: 0,
          top1Rate: 1,
          confusionScore: 0,
          acceptThreshold: 0.74
        }
      }
    });
    const currentSummary = summary({
      score: 0.66,
      shadowConfidence: 0.64,
      meaningConfidence: 0.82,
      unsafeRisk: 0.04,
      flipRisk: 0.06
    });
    const currentConfusion = confusion({ targetRank: 1, topGap: 0.12, confusionScore: 0 });
    const correction = calculateTinyMlTwoTrackCorrection({
      summary: currentSummary,
      contrast: contrast({ eligibleForActual: true, actualScoreLift: 0.09 }),
      threshold,
      confusion: currentConfusion,
      targetPresetId: "custom:eval_rect"
    });
    const decision = decideWithTwoTrackPersonalization({
      summary: currentSummary,
      threshold,
      confusion: currentConfusion,
      tinyMl: correction,
      targetPresetId: "custom:eval_rect"
    });

    expect(correction.promotePriority).toBe(true);
    expect(correction.selectedTrack).toBe("meaning_recovery");
    expect(decision.decision).toBe("accept");
  });

  it("summarizes recognition and target confusion without survey state", () => {
    const result = {
      selectedPresetId: "custom:eval_rect",
      selectedCandidate: { id: "custom:eval_rect", label: "rect", score: 0.81234, status: "recognized" },
      candidates: [
        { id: "custom:eval_rect", label: "rect", score: 0.81234, status: "recognized" },
        { id: "custom:eval_line", label: "line", score: 0.71, status: "ambiguous" }
      ]
    } as unknown as DatacardRecognitionResult;

    const summarized = summarizeTwoTrackRecognition(result);
    const targetConfusion = resolveTwoTrackConfusion(summarized, "custom:eval_rect");

    expect(summarized.score).toBe(0.8123);
    expect(targetConfusion.targetRank).toBe(1);
    expect(targetConfusion.confusedWith).toBe("custom:eval_line");
  });
});

function summary(overrides: Partial<RecognitionSummary> = {}): RecognitionSummary {
  return {
    selectedCandidateId: "custom:eval_rect",
    finalCandidateId: "custom:eval_rect",
    finalStatus: "recognized",
    score: 0.7,
    shadowConfidence: 0.7,
    meaningConfidence: 0.7,
    unsafeRisk: 0.05,
    flipRisk: 0.06,
    topCandidates: [
      { id: "custom:eval_rect", label: "rect", score: 0.7, status: "recognized" },
      { id: "custom:eval_line", label: "line", score: 0.62, status: "ambiguous" }
    ],
    ...overrides
  };
}

function confusion(overrides: Partial<ConfusionSnapshot> = {}): ConfusionSnapshot {
  return {
    targetRank: 1,
    topPair: "rect vs line",
    topGap: 0.08,
    targetInTop5: true,
    confusedWith: "custom:eval_line",
    confusionScore: 0.02,
    ...overrides
  };
}

function thresholdState(overrides: Partial<ThresholdState> = {}): ThresholdState {
  return {
    captureCount: 3,
    globalMaturity: 0.25,
    globalScoreLift: 0.04,
    acceptThreshold: 0.72,
    holdThreshold: 0.59,
    unsafeLimit: 0.24,
    flipLimit: 0.44,
    targetRankLimit: 5,
    topGapFloor: 0.035,
    targetAdjustments: {
      "custom:eval_rect": {
        captureCount: 2,
        evalCount: 0,
        top1Rate: 1,
        confusionScore: 0,
        acceptThreshold: 0.7
      }
    },
    ...overrides
  };
}

function contrast(overrides: Partial<DatacardTinyMlContrastDecision["meaning"] & DatacardTinyMlContrastDecision["shadow"]> = {}): DatacardTinyMlContrastDecision {
  return {
    version: "datacard-contrast-v1",
    role: "all_agree",
    finalStatus: "recognized",
    finalCandidateId: "custom:eval_rect",
    actualCandidateId: "custom:eval_rect",
    shadow: {
      candidateId: "custom:eval_rect",
      confidence: 0.7,
      unsafeRisk: 0.05,
      flipRisk: 0.06,
      relationRisk: overrides.relationRisk ?? 0,
      suggestedAction: "accept_shadow",
      reasons: []
    },
    meaning: {
      candidateId: "custom:eval_rect",
      confidence: 0.7,
      correctionClass: "meaning_recover",
      eligibleForActual: overrides.eligibleForActual ?? false,
      actualScoreLift: overrides.actualScoreLift ?? 0,
      reasons: []
    },
    blockedBy: [],
    explanationCodes: []
  };
}
