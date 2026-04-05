import { describe, expect, it } from "vitest";
import { OVERLAY_OPERATOR_TEMPLATES } from "../src/recognizer/operator-templates";
import { createOverlayReferenceFrame, recognizeOverlayStroke } from "../src/recognizer/operators";
import { GLYPH_TEMPLATES } from "../src/recognizer/templates";
import type { OverlayOperator, Stroke, StrokeSession } from "../src/recognizer/types";

describe("overlay operator recognizer", () => {
  const baseSession = fromGlyphTemplate("earth", {
    scale: 220,
    rotate: -0.34,
    translate: { x: 300, y: 270 },
    timeStep: 20
  });
  const referenceFrame = createOverlayReferenceFrame(baseSession);

  it.each([
    ["steel_brace", 92, 0, { x: 455, y: 300 }, []],
    ["electric_fork", 96, 0, { x: 452, y: 198 }, []],
    ["ice_bar", 116, 0, { x: 300, y: 270 }, []],
    ["soul_dot", 22, 0, { x: 240, y: 200 }, []],
    ["void_cut", 118, 0.04, { x: 470, y: 220 }, []],
    ["martial_axis", 118, 0, { x: 470, y: 360 }, ["void_cut"]]
  ] as Array<[OverlayOperator, number, number, { x: number; y: number }, OverlayOperator[]]>)(
    "recognizes %s with separate overlay session context",
    (operator, scale, rotate, translate, existingOperators) => {
      const overlaySession = makeSession([
        fromOverlayTemplate(operator, {
          scale,
          rotate,
          translate
        })
      ]);
      const recognition = recognizeOverlayStroke(overlaySession.strokes[0], {
        referenceFrame,
        existingOperators,
        overlaySession
      });

      expect(recognition.status).toBe("recognized");
      expect(recognition.operator).toBe(operator);
      expect(recognition.anchorZoneId).toBeDefined();
      expect(recognition.topCandidate?.templateDistance).toBeLessThan(0.62);
    }
  );

  it("returns incomplete for martial_axis when void_cut is missing", () => {
    const stroke = fromOverlayTemplate("martial_axis", {
      scale: 118,
      rotate: 0,
      translate: { x: 470, y: 360 }
    });
    const overlaySession = makeSession([stroke]);
    const recognition = recognizeOverlayStroke(stroke, {
      referenceFrame,
      existingOperators: [],
      overlaySession
    });

    expect(recognition.status).toBe("incomplete");
    expect(recognition.operator).toBeUndefined();
    expect(recognition.invalidReason).toContain("void_cut");
  });

  it("returns ambiguous or invalid for a partial ice_bar stroke", () => {
    const stroke = makeStroke(
      [
        [280, 270],
        [332, 270]
      ],
      0
    );
    const recognition = recognizeOverlayStroke(stroke, {
      referenceFrame,
      existingOperators: [],
      overlaySession: makeSession([stroke])
    });

    expect(["ambiguous", "incomplete", "invalid"]).toContain(recognition.status);
    expect(recognition.operator).toBeUndefined();
  });
});

function fromGlyphTemplate(
  family: string,
  options: {
    scale: number;
    rotate: number;
    translate: { x: number; y: number };
    timeStep?: number;
  }
): StrokeSession {
  const template = GLYPH_TEMPLATES.find((item) => item.family === family);

  if (!template) {
    throw new Error(`unknown template family: ${family}`);
  }

  return makeSession(
    template.strokes.map((stroke, strokeIndex) => ({
      id: `${family}-${strokeIndex}`,
      points: stroke.points.map((point, pointIndex) => transformPoint(point, strokeIndex, pointIndex, options))
    }))
  );
}

function fromOverlayTemplate(
  operator: OverlayOperator,
  options: {
    scale: number;
    rotate: number;
    translate: { x: number; y: number };
    timeStep?: number;
  }
): Stroke {
  const template = OVERLAY_OPERATOR_TEMPLATES.find((item) => item.operator === operator);

  if (!template) {
    throw new Error(`unknown overlay operator: ${operator}`);
  }

  return {
    id: `${operator}-stroke`,
    points: template.strokes[0].points.map((point, pointIndex) => transformPoint(point, 0, pointIndex, options))
  };
}

function transformPoint(
  point: { x: number; y: number; t: number },
  strokeIndex: number,
  pointIndex: number,
  options: {
    scale: number;
    rotate: number;
    translate: { x: number; y: number };
    timeStep?: number;
  }
): { x: number; y: number; t: number } {
  const cosine = Math.cos(options.rotate);
  const sine = Math.sin(options.rotate);
  const rotatedX = point.x * cosine - point.y * sine;
  const rotatedY = point.x * sine + point.y * cosine;

  return {
    x: rotatedX * options.scale + options.translate.x,
    y: rotatedY * options.scale + options.translate.y,
    t: (strokeIndex * 240 + pointIndex) * (options.timeStep ?? 16)
  };
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
