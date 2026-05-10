import { describe, expect, it } from "vitest";

import {
  getBuiltInMagicCardSetHash,
  getBuiltInMagicCardSetId,
  listBuiltInMagicCards
} from "../src/recognizer/datacards";
import {
  summarizeMagicDatacardValidation,
  validateMagicDatacard,
  validateMagicDatacardSet
} from "../src/recognizer/datacard-schema";

function builtInCardSet() {
  return {
    cardSetId: getBuiltInMagicCardSetId(),
    cardSetHash: getBuiltInMagicCardSetHash(),
    cards: listBuiltInMagicCards()
  };
}

describe("magic datacard schema validation", () => {
  it("accepts the current built-in cards and manifest", () => {
    for (const card of listBuiltInMagicCards()) {
      expect(validateMagicDatacard(card).issues).toEqual([]);
      expect(validateMagicDatacard(card).valid).toBe(true);
    }

    const result = validateMagicDatacardSet(builtInCardSet());
    expect(result.valid).toBe(true);
    expect(result.issues).toEqual([]);
    expect(summarizeMagicDatacardValidation(result)).toMatchObject({ valid: true, issueCount: 0 });
  });

  it("rejects unknown family labels instead of widening the runtime union", () => {
    const wind = listBuiltInMagicCards().find((card) => card.id === "family:wind");
    expect(wind).toBeDefined();

    const result = validateMagicDatacard({
      ...wind,
      id: "family:storm",
      family: "storm",
      target: { kind: "family", label: "storm" }
    });

    expect(result.valid).toBe(false);
    expect(result.issues.map((issue) => issue.code)).toContain("invalid_family_label");
  });

  it("rejects duplicate card targets in a replacement manifest", () => {
    const cards = listBuiltInMagicCards().map((card) => ({ ...card }));
    cards[1] = { ...cards[1], id: "family:earth", target: { kind: "family", label: "wind" }, family: "wind" } as (typeof cards)[number];

    const result = validateMagicDatacardSet({ ...builtInCardSet(), cards });

    expect(result.valid).toBe(false);
    expect(result.issues.map((issue) => issue.code)).toContain("duplicate_target");
  });

  it("rejects invalid operator dependencies", () => {
    const martialAxis = listBuiltInMagicCards().find((card) => card.id === "operator:martial_axis");
    expect(martialAxis).toBeDefined();

    const result = validateMagicDatacard({
      ...martialAxis,
      dependencies: ["void_cut", "storm_link"]
    });

    expect(result.valid).toBe(false);
    expect(result.issues).toEqual(
      expect.arrayContaining([expect.objectContaining({ path: "card.dependencies[1]", code: "invalid_dependency", severity: "error" })])
    );
  });

  it("rejects unknown anchor zones", () => {
    const voidCut = listBuiltInMagicCards().find((card) => card.id === "operator:void_cut");
    expect(voidCut).toBeDefined();

    const result = validateMagicDatacard({
      ...voidCut,
      anchorHints: ["upper_right", "outer_rim"]
    });

    expect(result.valid).toBe(false);
    expect(result.issues).toEqual(
      expect.arrayContaining([expect.objectContaining({ path: "card.anchorHints[1]", code: "invalid_anchor_zone", severity: "error" })])
    );
  });

  it("rejects missing tutorial HCI metadata", () => {
    const fire = listBuiltInMagicCards().find((card) => card.id === "family:fire");
    expect(fire).toBeDefined();

    const result = validateMagicDatacard({
      ...fire,
      tutorial: {
        ...fire?.tutorial,
        summary: ""
      }
    });

    expect(result.valid).toBe(false);
    expect(result.issues).toEqual(
      expect.arrayContaining([expect.objectContaining({ path: "card.tutorial.summary", code: "missing_required", severity: "error" })])
    );
  });
});
