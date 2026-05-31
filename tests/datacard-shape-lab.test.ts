import { describe, expect, it } from "vitest";

import {
  appendDatacardShapeCapture,
  createDatacardRecognizerRegistry,
  createEmptyDatacardShapeCaptureStore,
  listDatacardShapePresets,
  recognizeSessionWithDatacard,
  recognizeSessionWithDatacardRegistry,
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

  it("adds contrastive tinyML lanes without changing stable built-in recognition", () => {
    const fire = getPreset("fire");
    const session = makeSession(scaleStrokes(fire.definition.exampleTemplate, 190, 250, 230));
    const result = recognizeSessionWithDatacard(session, fire, createEmptyDatacardShapeCaptureStore(1));

    expect(result.contrast).toMatchObject({
      version: "datacard-contrast-v1",
      finalCandidateId: "fire",
      finalStatus: "recognized"
    });
    expect(result.selectedCandidate.status).toBe("recognized");
    expect(result.selectedCandidate.contrastRole).toBe(result.contrast?.role);
    expect(result.selectedCandidate.shadowRisk).toEqual(expect.any(Number));
  });

  it("keeps a custom shape in shadow/hold before enough tutorial captures", () => {
    const spiral = getPreset("custom:spiral");
    const session = makeSession(scaleStrokes(spiral.definition.exampleTemplate, 180, 280, 260));
    const result = recognizeSessionWithDatacard(session, spiral, createEmptyDatacardShapeCaptureStore(1));

    expect(result.contrast?.meaning.correctionClass).toBe("hold_for_capture");
    expect(result.contrast?.blockedBy).toContain("signature");
    expect(result.selectedCandidate.status).toBe("ambiguous");
  });

  it("allows the meaning layer to become actual after sufficient low-risk tutorial captures", () => {
    const spiral = getPreset("custom:spiral");
    const session = makeSession(scaleStrokes(spiral.definition.exampleTemplate, 180, 280, 260));
    let store = createEmptyDatacardShapeCaptureStore(1);

    for (let index = 0; index < 3; index += 1) {
      store = appendDatacardShapeCapture(store, spiral.id, session.strokes, index + 2);
    }

    const registry = createDatacardRecognizerRegistry([spiral], store, { builtInConfusionLimit: 1 });
    const result = recognizeSessionWithDatacardRegistry(session, registry, { selectedPresetId: spiral.id });

    expect(result.contrast?.meaning.eligibleForActual).toBe(true);
    expect(result.contrast?.blockedBy).not.toContain("signature");
    expect(result.selectedCandidate.status).toBe("recognized");
  });

  it("keeps repeated open-line custom shapes gated as high-risk even when the template matches", () => {
    const repeatedLines: DatacardShapePreset = {
      id: "custom:parallel_lines",
      kind: "custom",
      group: "custom",
      label: "parallel lines",
      shortLabel: "lines",
      description: "Three repeated open line strokes.",
      definition: {
        pattern: "line{3}",
        expression: "line{3}",
        guide: "Draw three parallel open lines.",
        keywords: ["line", "parallel"],
        features: {
          strokeCount: [3, 3],
          closed: false,
          corners: [0, 2],
          endpointClusters: [6, 6],
          fillRatio: [0, 0.12],
          parallelism: [0.8, 1]
        },
        exampleTemplate: [
          createLineStroke("line-a", -0.72, -0.32, 0.72, -0.32),
          createLineStroke("line-b", -0.72, 0, 0.72, 0),
          createLineStroke("line-c", -0.72, 0.32, 0.72, 0.32)
        ]
      }
    };
    const session = makeSession(scaleStrokes(repeatedLines.definition.exampleTemplate, 180, 260, 260));
    const result = recognizeSessionWithDatacard(session, repeatedLines, createEmptyDatacardShapeCaptureStore(1));

    expect(result.contrast?.blockedBy).toContain("repetition");
    expect(result.contrast?.meaning.correctionClass).toBe("downgrade_risk");
    expect(result.selectedCandidate.status).toBe("ambiguous");
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

function createLineStroke(id: string, x1: number, y1: number, x2: number, y2: number): Stroke {
  return {
    id,
    points: [
      { x: x1, y: y1, t: 0 },
      { x: (x1 + x2) / 2, y: (y1 + y2) / 2, t: 16 },
      { x: x2, y: y2, t: 32 }
    ]
  };
}
