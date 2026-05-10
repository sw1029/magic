import { describe, expect, it } from "vitest";

import { getBuiltInMagicCardSetSignature } from "../src/recognizer/datacards";
import { resolveBaseEffectiveThresholdBias } from "../src/recognizer/rerank";
import {
  backfillTutorialProfileCardSignature,
  createEmptyTutorialProfileStore,
  hydrateTutorialProfileStore,
  previewTutorialProfileCardBackfill
} from "../src/recognizer/tutorial-profile";
import { createEmptyUserInputProfile } from "../src/recognizer/user-profile";

function legacyStore() {
  return hydrateTutorialProfileStore({
    version: "v1.5",
    captures: [
      {
        id: "legacy-fire",
        kind: "family",
        expectedFamily: "fire",
        source: "variation",
        timestamp: 1,
        strokes: [
          {
            id: "stroke-1",
            points: [
              { x: 0, y: 0, t: 0 },
              { x: 1, y: 1, t: 1 }
            ]
          }
        ],
        validation: {
          reliability: "high",
          expectedLabel: "fire",
          actualTopLabel: "fire",
          status: "recognized"
        }
      }
    ],
    updatedAt: 1
  });
}

describe("tutorial profile card backfill", () => {
  it("previews and applies current card signature for legacy stores", () => {
    const store = legacyStore();
    const signature = getBuiltInMagicCardSetSignature();
    const preview = previewTutorialProfileCardBackfill(store);

    expect(preview.reason).toBe("legacy_missing");
    expect(preview.canBackfill).toBe(true);
    expect(store.cardSetHash).toBeUndefined();

    const backfilled = backfillTutorialProfileCardSignature(store);
    expect(backfilled.cardSetId).toBe(signature.cardSetId);
    expect(backfilled.cardSetHash).toBe(signature.cardSetHash);
    expect(backfilled.shapeProfile.cardSignature).toBe(signature.cardSetHash);
  });

  it("does not backfill mismatched custom signatures", () => {
    const store = { ...legacyStore(), cardSetId: "custom", cardSetHash: "different", cardSignature: "different" };
    const preview = previewTutorialProfileCardBackfill(store);

    expect(preview.canBackfill).toBe(false);
    expect(preview.reason).toBe("card_mismatch");
    expect(backfillTutorialProfileCardSignature(store)).toBe(store);
  });

  it("does not backfill unknown validation labels", () => {
    const store = legacyStore();
    store.captures[0] = {
      ...store.captures[0],
      validation: {
        ...store.captures[0].validation!,
        actualTopLabel: "storm"
      }
    };

    const preview = previewTutorialProfileCardBackfill(store);
    expect(preview.canBackfill).toBe(false);
    expect(preview.reason).toBe("unknown_capture_target");
    expect(preview.blockingDetails).toContain("captures[0].validation.actualTopLabel");
  });

  it("keeps feedback-only profiles out of actual threshold bias after backfill", () => {
    const store = hydrateTutorialProfileStore({
      version: "v1.5",
      captures: [
        {
          id: "feedback",
          kind: "family",
          expectedFamily: "fire",
          source: "variation",
          timestamp: 1,
          strokes: [{ id: "s", points: [{ x: 0, y: 0, t: 0 }, { x: 2, y: 2, t: 1 }] }],
          validation: { reliability: "feedback_only", expectedLabel: "fire", actualTopLabel: "fire" }
        }
      ],
      updatedAt: 1
    });
    const backfilled = backfillTutorialProfileCardSignature(store);
    backfilled.shapeProfile.familyThresholdBias = { fire: 0.02 };
    const profile = {
      ...createEmptyUserInputProfile(),
      tutorialProfile: backfilled.shapeProfile,
      shapeProfile: backfilled.shapeProfile
    };

    expect(resolveBaseEffectiveThresholdBias(profile, "fire").thresholdBias).toBe(0);
  });

  it("recognizes a fresh empty store as already current", () => {
    expect(previewTutorialProfileCardBackfill(createEmptyTutorialProfileStore()).reason).toBe("already_current");
  });
});
