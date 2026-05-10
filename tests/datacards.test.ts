import { describe, expect, it } from "vitest";

import {
  evaluateMagicCardSetCompatibility,
  getBuiltInMagicCardSetSignature,
  getMagicCardById,
  listBuiltInFamilyCards,
  listBuiltInMagicCards,
  listBuiltInOperatorCards,
  resolveMagicCardForTarget
} from "../src/recognizer/datacards";
import { OVERLAY_OPERATOR_TEMPLATES } from "../src/recognizer/operator-templates";
import { GLYPH_TEMPLATES } from "../src/recognizer/templates";

describe("built-in magic datacards", () => {
  it("represents the current closed family and operator label sets", () => {
    expect(listBuiltInFamilyCards()).toHaveLength(5);
    expect(listBuiltInOperatorCards()).toHaveLength(6);
    expect(listBuiltInMagicCards()).toHaveLength(11);

    expect(listBuiltInFamilyCards().map((card) => card.family)).toEqual(GLYPH_TEMPLATES.map((template) => template.family));
    expect(listBuiltInOperatorCards().map((card) => card.operator)).toEqual(
      OVERLAY_OPERATOR_TEMPLATES.map((template) => template.operator)
    );
  });

  it("resolves cards by id and closed recognizer target without widening labels", () => {
    expect(getMagicCardById("family:fire")?.target).toEqual({ kind: "family", label: "fire" });
    expect(resolveMagicCardForTarget("operator", "void_cut")?.id).toBe("operator:void_cut");
    expect(resolveMagicCardForTarget("family", "storm")).toBeUndefined();
    expect(resolveMagicCardForTarget("operator", "unknown_operator")).toBeUndefined();
  });

  it("captures the void_cut dependency and anchor hints for martial_axis", () => {
    const martialAxis = resolveMagicCardForTarget("operator", "martial_axis");

    expect(martialAxis?.dependencies).toEqual(["void_cut"]);
    expect(martialAxis?.anchorHints).toContain("lower_right");
    expect(resolveMagicCardForTarget("operator", "void_cut")?.dependencies).toEqual([]);
  });

  it("returns ready, label_mismatch, and card_set_mismatch compatibility decisions", () => {
    const signature = getBuiltInMagicCardSetSignature();

    expect(evaluateMagicCardSetCompatibility(signature).status).toBe("ready");

    expect(
      evaluateMagicCardSetCompatibility({
        ...signature,
        familyLabels: ["wind", "earth", "fire", "water", "life", "storm"]
      }).status
    ).toBe("label_mismatch");

    expect(
      evaluateMagicCardSetCompatibility({
        ...signature,
        cardSetHash: "fnv1a32:00000000"
      }).status
    ).toBe("card_set_mismatch");
  });

  it("keeps tutorial and HCI metadata present on every card", () => {
    for (const card of listBuiltInMagicCards()) {
      expect(card.label.length).toBeGreaterThan(0);
      expect(card.shortLabel.length).toBeGreaterThan(0);
      expect(card.tutorial.title.length).toBeGreaterThan(0);
      expect(card.tutorial.instruction.length).toBeGreaterThan(0);
      expect(card.tutorial.summary.length).toBeGreaterThan(0);
      expect(card.tutorial.checklist.length).toBeGreaterThanOrEqual(2);
      expect(card.tutorial.emergentPrompts.length).toBeGreaterThan(0);
      expect(card.tutorial.whatIfHints.length).toBeGreaterThan(0);
    }
  });
});
