import { describe, expect, it } from "vitest";

import { buildPreviewTutorialSteps, TUTORIAL_DEMO_STEPS } from "../src/demo/tutorial-flow";
import { buildMagicWhatIfScenarios } from "../src/demo/what-if";
import { getBuiltInMagicCardSetHash, getBuiltInMagicCardSetId, listBuiltInMagicCards } from "../src/recognizer/datacards";
import { loadMagicDatacardPreview } from "../src/recognizer/datacard-loader";

function manifest(cards = listBuiltInMagicCards()) {
  return {
    cardSetId: "local-authoring/demo-v1",
    cardSetHash: "author-hash",
    cards
  };
}

describe("magic datacard preview loader", () => {
  it("loads a full 11-card authoring preview without requiring the built-in hash", () => {
    const result = loadMagicDatacardPreview({ rawJson: JSON.stringify(manifest()) });

    expect(result.ok).toBe(true);
    expect(result.mode).toBe("preview");
    expect(result.registry?.compatibility.status).toBe("ready");
    expect(result.registry?.runtimeCompatibility.status).toBe("card_set_mismatch");
    expect(result.registry?.cards).toHaveLength(11);
  });

  it("rejects one-card full_set manifests but allows patch mode after merging", () => {
    const fire = listBuiltInMagicCards().find((card) => card.id === "family:fire");
    expect(fire).toBeDefined();
    const patch = {
      cardSetId: "local-authoring/fire-copy",
      cards: [{ ...fire!, label: "불꽃형 미리보기", tutorial: { ...fire!.tutorial, title: "미리보기 불꽃" } }]
    };

    expect(loadMagicDatacardPreview({ rawJson: JSON.stringify(patch) }).ok).toBe(false);

    const patchResult = loadMagicDatacardPreview({ rawJson: JSON.stringify(patch), loadMode: "patch" });
    expect(patchResult.ok).toBe(true);
    expect(patchResult.registry?.cards.find((card) => card.id === "family:fire")?.label).toBe("불꽃형 미리보기");
  });

  it("returns a safe issue for invalid JSON", () => {
    const result = loadMagicDatacardPreview({ rawJson: "{not-json" });

    expect(result.ok).toBe(false);
    expect(result.issues[0]?.code).toBe("invalid_json");
    expect(result.userMessage).toContain("JSON");
  });

  it("keeps built-in cards immutable while preview adapters can read preview copy", () => {
    const beforeTitle = listBuiltInMagicCards().find((card) => card.id === "family:fire")?.tutorial.title;
    const fire = listBuiltInMagicCards().find((card) => card.id === "family:fire");
    const patchResult = loadMagicDatacardPreview({
      rawJson: JSON.stringify({ cards: [{ ...fire!, tutorial: { ...fire!.tutorial, title: "미리보기 불꽃" } }] }),
      loadMode: "patch"
    });

    expect(listBuiltInMagicCards().find((card) => card.id === "family:fire")?.tutorial.title).toBe(beforeTitle);
    const steps = buildPreviewTutorialSteps(TUTORIAL_DEMO_STEPS, patchResult.registry?.cards ?? []);
    expect(steps.find((step) => step.id === "fire_trace")?.title).toBe("미리보기 불꽃");
    expect(buildMagicWhatIfScenarios(patchResult.registry?.cards).length).toBeGreaterThan(0);
  });

  it("loads the exact built-in manifest as runtime compatible", () => {
    const result = loadMagicDatacardPreview({
      rawJson: JSON.stringify({
        cardSetId: getBuiltInMagicCardSetId(),
        cardSetHash: getBuiltInMagicCardSetHash(),
        cards: listBuiltInMagicCards()
      })
    });

    expect(result.ok).toBe(true);
    expect(result.registry?.runtimeCompatibility.status).toBe("ready");
  });
});
