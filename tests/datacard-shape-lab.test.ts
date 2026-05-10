import { describe, expect, it } from "vitest";

import {
  appendDatacardShapeCapture,
  createEmptyDatacardShapeCaptureStore,
  listDatacardShapePresets,
  recognizeSessionWithDatacard,
  validateDatacardShapePreset,
  type DatacardShapePreset
} from "../src/recognizer/datacard-shape-lab";
import type { PointSample, Stroke, StrokeSession } from "../src/recognizer/types";

describe("datacard shape lab", () => {
  it("keeps life out of the basic shape lab presets for now", () => {
    const builtIns = listDatacardShapePresets()
      .filter((preset) => preset.kind === "built_in")
      .map((preset) => preset.id);

    expect(builtIns).toEqual(["wind", "earth", "fire", "water"]);
  });

  it("adds at least five custom datacard shape examples", () => {
    const custom = listDatacardShapePresets().filter((preset) => preset.kind === "custom");

    expect(custom.length).toBeGreaterThanOrEqual(5);
    expect(custom.map((preset) => preset.id)).toEqual([
      "custom:spiral",
      "custom:star",
      "custom:cross",
      "custom:y_shape",
      "custom:crescent",
      "custom:diamond",
      "custom:zigzag"
    ]);
  });

  it("adds Y-shaped custom preset with a readable definition", () => {
    const yShape = getPreset("custom:y_shape");

    expect(yShape.label).toBe("Y자형");
    expect(yShape.group).toBe("custom");
    expect(validateDatacardShapePreset(yShape)).toMatchObject({ valid: true });
    expect(yShape.definition.exampleTemplate).toHaveLength(3);
  });

  it("validates definition expressions as regular expressions", () => {
    const spiral = getPreset("custom:spiral");

    expect(validateDatacardShapePreset(spiral)).toMatchObject({ valid: true });

    const invalid: DatacardShapePreset = {
      ...spiral,
      definition: {
        ...spiral.definition,
        pattern: "("
      }
    };

    expect(validateDatacardShapePreset(invalid)).toMatchObject({
      valid: false,
      issues: [expect.objectContaining({ code: "invalid_pattern" })]
    });
  });

  it("raises custom shape confidence after a local tutorial capture", () => {
    const spiral = getPreset("custom:spiral");
    const session = makeSession(scaleStrokes(spiral.definition.exampleTemplate, 180, 280, 260));
    const emptyStore = createEmptyDatacardShapeCaptureStore(1);
    const before = recognizeSessionWithDatacard(session, spiral, emptyStore);
    const afterStore = appendDatacardShapeCapture(emptyStore, spiral.id, session.strokes, 2);
    const after = recognizeSessionWithDatacard(session, spiral, afterStore);

    expect(afterStore.captures).toHaveLength(1);
    expect(after.selectedCandidate.score).toBeGreaterThan(before.selectedCandidate.score);
    expect(after.selectedCandidate.localModelLift).toBeGreaterThan(0);
  });

  it("keeps the existing built-in recognizer result while adding datacard candidates", () => {
    const fire = getPreset("fire");
    const session = makeSession(scaleStrokes(fire.definition.exampleTemplate, 190, 250, 230));
    const result = recognizeSessionWithDatacard(session, fire, createEmptyDatacardShapeCaptureStore(1));

    expect(result.baseResult.candidates[0]?.family).toBe("fire");
    expect(new Set(result.baseResult.candidates.map((candidate) => candidate.family))).toEqual(
      new Set(["wind", "earth", "fire", "water", "life"])
    );
    expect(result.selectedCandidate.id).toBe("fire");
  });

  it("excludes an enclosing circle from datacard scoring input", () => {
    const fire = getPreset("fire");
    const fireStrokes = scaleStrokes(fire.definition.exampleTemplate, 190, 250, 230);
    const session = makeSession([...fireStrokes, makeCircleStroke("seal-ring", 250, 250, 190)]);
    const result = recognizeSessionWithDatacard(session, fire, createEmptyDatacardShapeCaptureStore(1));

    expect(result.sealDetection?.ok).toBe(true);
    expect(result.sessionUsed.strokes.length).toBe(fireStrokes.length);
  });

  it("keeps custom capture storage as an in-memory immutable store", () => {
    const cross = getPreset("custom:cross");
    const store = createEmptyDatacardShapeCaptureStore(1);
    const session = makeSession(scaleStrokes(cross.definition.exampleTemplate, 160, 260, 260));
    const next = appendDatacardShapeCapture(store, cross.id, session.strokes, 2);

    expect(store.captures).toHaveLength(0);
    expect(next.captures).toHaveLength(1);
    expect(next.updatedAt).toBe(2);
  });
});

function getPreset(id: string): DatacardShapePreset {
  const preset = listDatacardShapePresets().find((candidate) => candidate.id === id);
  expect(preset).toBeDefined();
  return preset!;
}

function makeSession(strokes: readonly Stroke[]): StrokeSession {
  return {
    startedAt: 1,
    endedAt: 2,
    strokes: strokes.map((stroke) => ({
      ...stroke,
      points: stroke.points.map((point) => ({ ...point }))
    }))
  };
}

function scaleStrokes(strokes: readonly Stroke[], size: number, offsetX: number, offsetY: number): Stroke[] {
  return strokes.map((stroke) => ({
    ...stroke,
    id: `scaled-${stroke.id}`,
    points: stroke.points.map((point) => ({
      ...point,
      x: offsetX + point.x * size,
      y: offsetY + point.y * size
    }))
  }));
}

function makeCircleStroke(id: string, cx: number, cy: number, radius: number): Stroke {
  const points: PointSample[] = [];

  for (let index = 0; index <= 36; index += 1) {
    const angle = (index / 36) * Math.PI * 2;
    points.push({
      x: cx + Math.cos(angle) * radius,
      y: cy + Math.sin(angle) * radius,
      t: index * 16
    });
  }

  return { id, points };
}
