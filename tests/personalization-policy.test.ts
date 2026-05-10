import { describe, expect, it } from "vitest";
import { evaluateTutorialThresholdBiasPolicy } from "../src/recognizer/personalization-policy";

describe("tutorial personalization threshold-bias policy", () => {
  const matchingCard = {
    cardSetId: "base-v1",
    cardSetHash: "hash-a",
    currentCardSignature: {
      cardSetId: "base-v1",
      cardSetHash: "hash-a"
    }
  };

  it("zeros actual threshold bias when all captures are feedback-only", () => {
    const decision = evaluateTutorialThresholdBiasPolicy({
      storeVersion: "v1.5",
      ...matchingCard,
      captureReliabilities: ["feedback_only", "feedback_only"],
      artifactCompatibility: "compatible"
    });

    expect(decision.thresholdBiasMultiplier).toBe(0);
    expect(decision.canApplyThresholdBias).toBe(false);
    expect(decision.reasonCodes).toContain("captures_feedback_only");
  });

  it("zeros actual threshold bias on card-set mismatch", () => {
    const decision = evaluateTutorialThresholdBiasPolicy({
      storeVersion: "v1.5",
      cardSetId: "base-v1",
      cardSetHash: "hash-a",
      currentCardSignature: {
        cardSetId: "base-v2",
        cardSetHash: "hash-b"
      },
      captureReliabilities: ["high", "medium"],
      artifactCompatibility: "compatible"
    });

    expect(decision.thresholdBiasMultiplier).toBe(0);
    expect(decision.canApplyThresholdBias).toBe(false);
    expect(decision.canUseForShadow).toBe(false);
    expect(decision.reasonCodes).toContain("card_set_mismatch");
  });

  it("zeros actual threshold bias on artifact mismatch while keeping shadow/explanation possible", () => {
    const decision = evaluateTutorialThresholdBiasPolicy({
      storeVersion: "v1.5",
      ...matchingCard,
      captureReliabilities: ["high", "medium"],
      artifactCompatibility: { status: "mismatch", artifactVersion: "old", expectedVersion: "new" }
    });

    expect(decision.thresholdBiasMultiplier).toBe(0);
    expect(decision.canApplyThresholdBias).toBe(false);
    expect(decision.canUseForShadow).toBe(true);
    expect(decision.canUseForExplanation).toBe(true);
    expect(decision.reasonCodes).toContain("artifact_mismatch");
  });

  it("allows actual threshold bias for healthy validated captures", () => {
    const decision = evaluateTutorialThresholdBiasPolicy({
      storeVersion: "v1.5",
      ...matchingCard,
      captureReliabilities: ["high", "medium", "high"],
      artifactCompatibility: "compatible"
    });

    expect(decision.thresholdBiasMultiplier).toBe(1);
    expect(decision.canApplyThresholdBias).toBe(true);
    expect(decision.requestedModeAllowed).toBe(true);
    expect(decision.needsBackfill).toBe(false);
    expect(decision.reasonCodes).toContain("captures_validated_healthy");
  });

  it("keeps legacy card metadata out of actual bias but allows shadow/metadata with backfill marked", () => {
    const actualDecision = evaluateTutorialThresholdBiasPolicy({
      storeVersion: "v1.5",
      currentCardSignature: {
        cardSetId: "base-v1",
        cardSetHash: "hash-a"
      },
      captureReliabilities: ["high", "medium"],
      artifactCompatibility: "compatible"
    });
    const shadowDecision = evaluateTutorialThresholdBiasPolicy({
      storeVersion: "v1.5",
      currentCardSignature: {
        cardSetId: "base-v1",
        cardSetHash: "hash-a"
      },
      captureReliabilities: ["high", "medium"],
      artifactCompatibility: "compatible",
      mode: "shadow"
    });

    expect(actualDecision.thresholdBiasMultiplier).toBe(0);
    expect(actualDecision.canApplyThresholdBias).toBe(false);
    expect(actualDecision.requestedModeAllowed).toBe(false);
    expect(actualDecision.needsBackfill).toBe(true);
    expect(actualDecision.reasonCodes).toContain("legacy_card_metadata_missing");
    expect(actualDecision.reasonCodes).toContain("needs_backfill");

    expect(shadowDecision.thresholdBiasMultiplier).toBe(0);
    expect(shadowDecision.requestedModeAllowed).toBe(true);
    expect(shadowDecision.canUseForShadow).toBe(true);
    expect(shadowDecision.needsBackfill).toBe(true);
    expect(shadowDecision.reasonCodes).toContain("shadow_or_metadata_only");
  });
});
