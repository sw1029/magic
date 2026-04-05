import { describe, expect, it } from "vitest";

import { renderExemplarChip, resolveRelevantExemplarIds } from "../src/demo/exemplars";
import type { OverlayRecognition, RecognitionResult } from "../src/recognizer/types";

describe("demo exemplars", () => {
  it("renders exemplar chips from internal descriptor strings", () => {
    const html = renderExemplarChip("fire", { active: true });

    expect(html).toContain("exemplar-chip active");
    expect(html).toContain("<svg");
    expect(html).toContain("polyline");
    expect(html).toContain("불꽃형 모범");
  });

  it("picks relevant family and operator exemplars for the current screen", () => {
    const result = {
      status: "ambiguous",
      topCandidate: { family: "earth", score: 0.66 },
      candidates: [
        { family: "earth", score: 0.66 },
        { family: "fire", score: 0.63 }
      ]
    } as unknown as RecognitionResult;
    const overlay = {
      status: "ambiguous",
      topCandidate: { operator: "void_cut", score: 0.71 }
    } as unknown as OverlayRecognition;

    expect(resolveRelevantExemplarIds(result, overlay)).toEqual(["earth", "fire", "void_cut"]);
  });
});
