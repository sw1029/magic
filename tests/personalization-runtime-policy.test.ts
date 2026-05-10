import { describe, expect, it } from "vitest";

import { getBuiltInMagicCardSetSignature } from "../src/recognizer/datacards";
import { resolveBaseEffectiveThresholdBias } from "../src/recognizer/rerank";
import { createEmptyUserInputProfile } from "../src/recognizer/user-profile";
import type { UserInputProfile, UserShapeProfile } from "../src/recognizer/types";

describe("runtime personalization threshold policy", () => {
  it("allows actual threshold bias only when profile card metadata matches the built-in card set", () => {
    const matching = createPolicyProfile({ withCurrentCardSignature: true });
    const legacy = createPolicyProfile({ withCurrentCardSignature: false });

    expect(resolveBaseEffectiveThresholdBias(matching, "fire").thresholdBias).toBeGreaterThan(0);
    expect(resolveBaseEffectiveThresholdBias(legacy, "fire").thresholdBias).toBe(0);
  });

  it("keeps feedback-only tutorial profiles out of actual threshold bias", () => {
    const feedbackOnly = createPolicyProfile({ withCurrentCardSignature: true, validated: 0, feedbackOnly: 8 });

    expect(resolveBaseEffectiveThresholdBias(feedbackOnly, "fire").thresholdBias).toBe(0);
  });
});

function createPolicyProfile(options: {
  withCurrentCardSignature: boolean;
  validated?: number;
  feedbackOnly?: number;
}): UserInputProfile {
  const signature = getBuiltInMagicCardSetSignature();
  const validated = options.validated ?? 8;
  const feedbackOnly = options.feedbackOnly ?? 0;
  const shapeProfile: UserShapeProfile = {
    tutorialSampleCount: validated + feedbackOnly,
    familyTutorialSampleCount: validated,
    operatorTutorialSampleCount: 0,
    validatedTutorialSampleCount: validated,
    feedbackOnlyTutorialSampleCount: feedbackOnly,
    familyPrototypes: {},
    operatorPrototypes: {},
    familyThresholdBias: { fire: 0.02 },
    operatorThresholdBias: {},
    familyPrototypeReliability: { fire: 1 },
    operatorPrototypeReliability: {},
    confusionPairs: [],
    updatedAt: 1,
    ...(options.withCurrentCardSignature
      ? {
          cardSetId: signature.cardSetId,
          cardSetHash: signature.cardSetHash,
          cardSignature: signature.cardSetHash
        }
      : {})
  };

  return {
    ...createEmptyUserInputProfile(),
    sampleCount: 24,
    tutorialProfile: shapeProfile,
    shapeProfile,
    recognitionCalibration: {
      userPrototypeWeight: 0.2,
      rerankStrength: 0.2,
      confidenceBias: 0.08
    }
  };
}
