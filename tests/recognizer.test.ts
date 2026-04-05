import { describe, expect, it } from "vitest";
import { GLYPH_TEMPLATES } from "../src/recognizer/templates";
import { recognizeSession } from "../src/recognizer/recognize";
import { getTinyMlRuntimeStatus } from "../src/recognizer/rerank";
import type { Stroke, StrokeSession } from "../src/recognizer/types";

describe("magic recognizer", () => {
  it.each([
    ["wind", 243, 0.42, { x: 290, y: 240 }],
    ["earth", 212.4, -0.38, { x: 310, y: 280 }],
    ["fire", 223.2, 0.58, { x: 300, y: 240 }],
    ["water", 198, -0.61, { x: 320, y: 260 }],
    ["life", 234, 0.36, { x: 280, y: 250 }]
  ])("recognizes canonical family %s under transform", (family, scale, rotate, translate) => {
    const session = fromTemplate(String(family), {
      scale: scale as number,
      rotate: rotate as number,
      translate: translate as { x: number; y: number },
      noise: 0.012
    });
    const result = recognizeSession(session, { sealed: true });

    expect(result.status).toBe("recognized");
    expect(result.canonicalFamily).toBe(family);
  });

  it("marks an open triangle as incomplete instead of misclassifying it", () => {
    const session = makeSession([
      makeStroke(
        [
          [260, 120],
          [410, 430],
          [120, 430]
        ],
        0
      )
    ]);
    const result = recognizeSession(session, { sealed: true });

    expect(result.status).toBe("incomplete");
    expect(result.canonicalFamily).toBeUndefined();
  });

  it("marks a two-line wind attempt as incomplete", () => {
    const session = makeSession([
      makeStroke(
        [
          [150, 180],
          [420, 180]
        ],
        0
      ),
      makeStroke(
        [
          [150, 260],
          [420, 260]
        ],
        140
      )
    ]);
    const result = recognizeSession(session, { sealed: true });

    expect(result.status).toBe("incomplete");
    expect(result.canonicalFamily).toBeUndefined();
    expect(result.topCandidate?.family).toBe("wind");
  });

  it("does not force a circle-like distorted life into a wrong family", () => {
    const session = makeSession([
      makeStroke(
        loop([
          [0, -0.7],
          [0.65, -0.2],
          [0.4, 0.6],
          [0, 0.25],
          [-0.4, 0.6],
          [-0.65, -0.2],
          [0, -0.7]
        ]).map(([x, y]) => [x * 180 + 290, y * 180 + 250]),
        0
      )
    ]);
    const result = recognizeSession(session, { sealed: true });

    expect(["ambiguous", "invalid", "incomplete"]).toContain(result.status);
    expect(result.canonicalFamily).toBeUndefined();
  });

  it("keeps family fixed while tempo changes", () => {
    const fast = fromTemplate("fire", {
      scale: 180,
      rotate: 0.15,
      translate: { x: 280, y: 250 },
      timeStep: 8
    });
    const slow = fromTemplate("fire", {
      scale: 180,
      rotate: 0.15,
      translate: { x: 280, y: 250 },
      timeStep: 42
    });
    const fastResult = recognizeSession(fast, { sealed: true });
    const slowResult = recognizeSession(slow, { sealed: true });

    expect(fastResult.canonicalFamily).toBe("fire");
    expect(slowResult.canonicalFamily).toBe("fire");
    expect(Math.abs(fastResult.quality.tempo - slowResult.quality.tempo)).toBeGreaterThan(0.08);
  });

  it("records rotation bias without changing family", () => {
    const base = fromTemplate("fire", {
      scale: 180,
      rotate: 0,
      translate: { x: 300, y: 260 },
      timeStep: 16
    });
    const slanted = fromTemplate("fire", {
      scale: 180,
      rotate: 0.7,
      translate: { x: 300, y: 260 },
      timeStep: 16
    });
    const baseResult = recognizeSession(base, { sealed: true });
    const slantedResult = recognizeSession(slanted, { sealed: true });

    expect(baseResult.canonicalFamily).toBe("fire");
    expect(slantedResult.canonicalFamily).toBe("fire");
    expect(slantedResult.quality.rotationBias).toBeGreaterThan(baseResult.quality.rotationBias + 0.2);
    expect(Math.abs(slantedResult.quality.closure - baseResult.quality.closure)).toBeLessThan(0.2);
  });

  it("records base shadow evaluation without changing a clear canonical family", () => {
    const runtime = getTinyMlRuntimeStatus();
    const session = fromTemplate("water", {
      scale: 192,
      rotate: -0.18,
      translate: { x: 300, y: 250 },
      noise: 0.01
    });
    const result = recognizeSession(session, { sealed: true });

    expect(result.status).toBe("recognized");
    expect(result.canonicalFamily).toBe("water");
    expect(result.shadow).toBeDefined();
    expect(result.shadow?.actualTopLabel).toBe(result.topCandidate?.family);
    expect(result.shadow?.actualStatus).toBe(result.status);
    expect(result.shadow?.candidates.length).toBeGreaterThan(0);

    if (runtime.baseShadowAvailable) {
      expect(result.shadow?.mode).toBe("shadow");
      expect(result.shadow?.artifactVersion).toBeTruthy();
    } else {
      expect(result.shadow?.mode).toBe("heuristic_only");
    }
  });
});

function fromTemplate(
  family: string,
  options: {
    scale: number;
    rotate: number;
    translate: { x: number; y: number };
    noise?: number;
    timeStep?: number;
  }
): StrokeSession {
  const template = GLYPH_TEMPLATES.find((item) => item.family === family);

  if (!template) {
    throw new Error(`unknown template family: ${family}`);
  }

  const strokes = template.strokes.map((stroke, strokeIndex) => {
    const transformedPoints = stroke.points.map((point, pointIndex) => {
      const jitter = options.noise ?? 0;
      const angle = options.rotate;
      const cosine = Math.cos(angle);
      const sine = Math.sin(angle);
      const nx = point.x + Math.sin(pointIndex + strokeIndex * 1.7) * jitter;
      const ny = point.y + Math.cos(pointIndex * 1.3 + strokeIndex) * jitter;
      const rotatedX = nx * cosine - ny * sine;
      const rotatedY = nx * sine + ny * cosine;
      return {
        x: rotatedX * options.scale + options.translate.x,
        y: rotatedY * options.scale + options.translate.y,
        t: (strokeIndex * 240 + pointIndex) * (options.timeStep ?? 16)
      };
    });

    return {
      id: `${family}-${strokeIndex}`,
      points: transformedPoints
    };
  });

  return makeSession(strokes);
}

function makeSession(strokes: Stroke[]): StrokeSession {
  const timestamps = strokes.flatMap((stroke) => stroke.points.map((point) => point.t));
  return {
    strokes,
    startedAt: Math.min(...timestamps, 0),
    endedAt: Math.max(...timestamps, 0)
  };
}

function makeStroke(points: Array<[number, number]>, offset: number, step = 16): Stroke {
  return {
    id: `stroke-${offset}`,
    points: points.map(([x, y], index) => ({
      x,
      y,
      t: offset + index * step
    }))
  };
}

function loop(points: Array<[number, number]>): Array<[number, number]> {
  return points;
}
