import { describe, expect, it } from "vitest";

import { buildMagicWhatIfScenarios } from "../src/demo/what-if";
import { buildMagicWhatIfPreviewModel } from "../src/demo/what-if-preview";

function scenario(kind: string) {
  const found = buildMagicWhatIfScenarios().find((item) => item.kind === kind);
  expect(found).toBeDefined();
  return found!;
}

describe("magic what-if preview model", () => {
  it("builds dependency arrows for martial axis ordering", () => {
    const model = buildMagicWhatIfPreviewModel({ scenario: scenario("dependency_ordering") });

    expect(model.nonMutating).toBe(true);
    expect(model.copy).toContain("현재 판정은 그대로 유지");
    expect(model.marks.some((mark) => mark.kind === "dependency_arrow" && mark.tone === "risk")).toBe(true);
  });

  it("builds anchor marks for off-anchor placement scenarios", () => {
    const model = buildMagicWhatIfPreviewModel({ scenario: scenario("off_anchor_risk") });

    expect(model.marks.some((mark) => mark.kind === "anchor_zone")).toBe(true);
    expect(model.marks.some((mark) => mark.kind === "risk_label")).toBe(true);
  });

  it("builds more than one ghost stroke for underscale comparison", () => {
    const model = buildMagicWhatIfPreviewModel({ scenario: scenario("underscale_risk") });
    const ghostMarks = model.marks.filter((mark) => mark.kind === "ghost_stroke");

    expect(ghostMarks.length).toBeGreaterThanOrEqual(2);
    expect(ghostMarks.every((mark) => (mark.points?.length ?? 0) >= 2)).toBe(true);
  });

  it("builds family structure ghost strokes", () => {
    const model = buildMagicWhatIfPreviewModel({ scenario: scenario("family_shape_mutation") });

    expect(model.marks.some((mark) => mark.kind === "ghost_stroke")).toBe(true);
    expect(model.marks.map((mark) => mark.label).join(" ")).toContain("구조");
  });
});
