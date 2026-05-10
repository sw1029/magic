import { describe, expect, it } from "vitest";

import { buildSyntheticStrokeSession } from "../src/demo/synthetic-input";
import {
  appendDatacardShapeCapture,
  compileDatacardShapePreset,
  createDatacardRecognizerRegistry,
  createEmptyDatacardShapeCaptureStore,
  listDatacardShapePresets
} from "../src/recognizer/datacard-shape-lab";
import { deriveRecognitionFeatureVectorV2 } from "../src/recognizer/feature-v2";
import { buildGestureRecognitionSignals } from "../src/recognizer/gesture-matcher";
import { recognizeSession } from "../src/recognizer/recognize";
import { recognizeSealedBaseSession } from "../src/recognizer/seal";
import { GLYPH_TEMPLATES } from "../src/recognizer/templates";
import { appendTutorialCapture, createEmptyTutorialProfileStore } from "../src/recognizer/tutorial-profile";
import type { PointSample, Stroke, StrokeSession } from "../src/recognizer/types";

describe("magic engine enhancements", () => {
  it("adds deterministic V2 features without changing the recognized decision", () => {
    const fire = GLYPH_TEMPLATES.find((item) => item.family === "fire");
    expect(fire).toBeDefined();
    const session = makeSession(scaleStrokes(fire!.strokes, 190, 260, 260));
    const pauseStroke = makeArcStroke("pause-probe", 260, 260, 80, 0, Math.PI / 2);
    pauseStroke.points[3].t += 260;
    const first = deriveRecognitionFeatureVectorV2([pauseStroke]);
    const second = deriveRecognitionFeatureVectorV2([pauseStroke]);
    const result = recognizeSession(session, { sealed: true });

    expect(second).toEqual(first);
    expect(first.velocityMean).toBeGreaterThan(0);
    expect(first.pauseCount).toBeGreaterThanOrEqual(1);
    expect(first.directionSequence.length).toBeGreaterThan(0);
    expect(result.status).toBe("recognized");
    expect(result.featureV2?.directionSequence.length).toBeGreaterThan(0);
  });

  it("keeps gesture matching auxiliary and sensitive to stroke order", () => {
    const template = GLYPH_TEMPLATES.find((item) => item.family === "wind");
    expect(template).toBeDefined();
    const scaled = scaleStrokes(template!.strokes, 180, 260, 260);
    const reordered = [scaled[2], scaled[0], scaled[1]];
    const same = buildGestureRecognitionSignals(scaled, template!.strokes);
    const swapped = buildGestureRecognitionSignals(reordered, template!.strokes);

    expect(same.gestureScore).toBeGreaterThan(swapped.gestureScore);
    expect(same.strokeOrderSimilarity).toBeGreaterThan(swapped.strokeOrderSimilarity);
    expect(same.temporalScore).toBeGreaterThan(swapped.temporalScore);
  });

  it("detects a recent multi-stroke seal ring and removes every ring stroke from base recognition", () => {
    const base = buildSyntheticStrokeSession({
      family: "water",
      seed: 32,
      jitterPx: 1,
      center: { x: 320, y: 260 },
      size: 150
    });
    const session = appendSeals(base, [
      makeArcStroke("seal-a", 320, 260, 145, 0, Math.PI),
      makeArcStroke("seal-b", 320, 260, 145, Math.PI, Math.PI * 2)
    ]);
    const sealed = recognizeSealedBaseSession(session);

    expect(sealed.sealDetection.ok).toBe(true);
    expect(sealed.sealDetection.multiStroke).toBe(true);
    expect(sealed.sealDetection.candidateStrokeIds).toEqual(["seal-a", "seal-b"]);
    expect(sealed.baseSession.strokes.some((stroke) => stroke.id.startsWith("seal-"))).toBe(false);
    expect(sealed.result.canonicalFamily).toBe("water");
  });

  it("keeps custom datacards in a separate lane until promotion requirements are met", () => {
    const spiral = listDatacardShapePresets().find((preset) => preset.id === "custom:spiral");
    expect(spiral).toBeDefined();
    const strokes = scaleStrokes(spiral!.definition.exampleTemplate, 180, 280, 260);
    let store = createEmptyDatacardShapeCaptureStore(1);
    const pending = compileDatacardShapePreset(spiral!, store);

    expect(pending.activationStatus).toBe("shape_definition");

    for (let index = 0; index < 3; index += 1) {
      store = appendDatacardShapeCapture(store, spiral!.id, strokes, index + 2);
    }

    const active = compileDatacardShapePreset(spiral!, store, { builtInConfusionLimit: 1 });
    const registry = createDatacardRecognizerRegistry([spiral!], store, { builtInConfusionLimit: 1 });

    expect(active.activationStatus).toBe("active_recognizer");
    expect(registry.activeProfiles.map((profile) => profile.preset.id)).toEqual(["custom:spiral"]);
  });

  it("stores V2 and gesture summaries in tutorial captures and prototypes", () => {
    const session = buildSyntheticStrokeSession({
      family: "fire",
      seed: 33,
      center: { x: 300, y: 260 },
      size: 180
    });
    const store = appendTutorialCapture(createEmptyTutorialProfileStore(1), {
      kind: "family",
      expectedFamily: "fire",
      strokes: session.strokes,
      source: "trace",
      timestamp: 2,
      validation: {
        reliability: "high",
        expectedLabel: "fire",
        actualTopLabel: "fire",
        status: "recognized"
      }
    });
    const capture = store.captures[0];
    const prototype = store.shapeProfile.familyPrototypes.fire;

    expect(capture.featureV2?.directionSequence.length).toBeGreaterThan(0);
    expect(capture.gestureSummary?.gestureScore).toBeGreaterThan(0);
    expect(prototype?.featureV2?.directionSequence).toBeTruthy();
    expect(prototype?.featureV2Variance?.velocityMean).toBeGreaterThanOrEqual(0);
  });
});

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

function appendSeals(base: StrokeSession, seals: readonly Stroke[]): StrokeSession {
  const lastSeal = seals[seals.length - 1];

  return {
    ...base,
    strokes: [...base.strokes, ...seals],
    endedAt: lastSeal?.points[lastSeal.points.length - 1]?.t ?? base.endedAt
  };
}

function makeArcStroke(
  id: string,
  cx: number,
  cy: number,
  radius: number,
  startAngle: number,
  endAngle: number
): Stroke {
  const count = 42;
  const points: PointSample[] = [];

  for (let index = 0; index <= count; index += 1) {
    const ratio = index / count;
    const angle = startAngle + (endAngle - startAngle) * ratio;
    points.push({
      x: cx + Math.cos(angle) * radius,
      y: cy + Math.sin(angle) * radius,
      t: 1_700_000 + index * 12
    });
  }

  return { id, points };
}
