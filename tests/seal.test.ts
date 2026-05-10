import { describe, expect, it } from "vitest";

import { buildSyntheticStrokeSession } from "../src/demo/synthetic-input";
import { detectEnclosingSeal, recognizeSealedBaseSession } from "../src/recognizer/seal";
import type { PointSample, Stroke, StrokeSession } from "../src/recognizer/types";

describe("enclosing seal detection", () => {
  it("recognizes a closed circle around the base glyph as a seal", () => {
    const base = buildSyntheticStrokeSession({ family: "water", seed: 21, jitterPx: 1, center: { x: 320, y: 260 }, size: 150 });
    const session = appendSeal(base, makeCircleStroke("seal-ring", 320, 260, 145));
    const sealed = recognizeSealedBaseSession(session);

    expect(sealed.sealDetection.ok).toBe(true);
    expect(sealed.sealDetection.ringDetected).toBe(true);
    expect(sealed.result.canonicalFamily).toBe("water");
  });

  it("rejects an open circle as an incomplete seal", () => {
    const base = buildSyntheticStrokeSession({ family: "fire", seed: 22, center: { x: 320, y: 260 }, size: 150 });
    const session = appendSeal(base, makeCircleStroke("open-ring", 320, 260, 145, 0.74));
    const detection = detectEnclosingSeal(session);

    expect(detection.ok).toBe(false);
    expect(detection.ringDetected).toBe(false);
    expect(detection.closure).toBeLessThan(0.72);
  });

  it("rejects a circle that does not enclose the base glyph", () => {
    const base = buildSyntheticStrokeSession({ family: "water", seed: 23, center: { x: 320, y: 260 }, size: 150 });
    const session = appendSeal(base, makeCircleStroke("small-ring", 120, 120, 38));
    const sealed = recognizeSealedBaseSession(session);

    expect(sealed.sealDetection.ok).toBe(false);
    expect(sealed.sealDetection.reason).toContain("감싸지");
    expect(sealed.result.canonicalFamily).toBeUndefined();
  });

  it("excludes the seal ring from family recognition input", () => {
    const base = buildSyntheticStrokeSession({ family: "water", seed: 24, center: { x: 320, y: 260 }, size: 150 });
    const session = appendSeal(base, makeCircleStroke("seal-ring", 320, 260, 145));
    const sealed = recognizeSealedBaseSession(session);

    expect(sealed.baseSession.strokes).toHaveLength(base.strokes.length);
    expect(sealed.baseSession.strokes.some((stroke) => stroke.id === "seal-ring")).toBe(false);
    expect(sealed.result.canonicalFamily).toBe("water");
  });
});

function appendSeal(base: StrokeSession, seal: Stroke): StrokeSession {
  return {
    ...base,
    strokes: [...base.strokes, seal],
    endedAt: seal.points[seal.points.length - 1]?.t ?? base.endedAt
  };
}

function makeCircleStroke(id: string, cx: number, cy: number, radius: number, turns = 1): Stroke {
  const count = 72;
  const points: PointSample[] = [];
  const end = Math.PI * 2 * turns;

  for (let index = 0; index <= count; index += 1) {
    const angle = (index / count) * end;
    points.push({
      x: cx + Math.cos(angle) * radius,
      y: cy + Math.sin(angle) * radius,
      t: 1_700_000 + index * 12
    });
  }

  return { id, points };
}
