import { describe, expect, it } from "vitest";
import { OVERLAY_OPERATOR_TEMPLATES } from "../src/recognizer/operator-templates";
import {
  createOverlayReferenceFrame,
  createTutorialOperatorContext,
  recognizeOverlayStroke
} from "../src/recognizer/operators";
import {
  appendTutorialCapture,
  createEmptyTutorialProfileStore,
  createTutorialOverlayPersonalizationProfile,
  hydrateTutorialProfileStore
} from "../src/recognizer/tutorial-profile";
import {
  getTinyMlRuntimeStatus,
  rerankOverlayCandidates,
  type OverlayPersonalizationProfile,
  type OverlayRerankCandidate
} from "../src/recognizer/rerank";
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

  it("keeps martial_axis blocked even with a strong personalization profile", () => {
    const stroke = fromOverlayTemplate("martial_axis", {
      scale: 118,
      rotate: 0,
      translate: { x: 470, y: 360 }
    });
    const overlaySession = makeSession([stroke]);
    const recognition = recognizeOverlayStroke(stroke, {
      referenceFrame,
      existingOperators: [],
      overlaySession,
      personalizationProfile: createPersonalizationProfile({
        martial_axis: {
          operator: "martial_axis",
          sampleCount: 6,
          averageAngleRadians: 0,
          averageScaleRatio: 0.3,
          averageAnchorZoneId: "lower_right",
          averageStraightness: 0.58,
          averageCorners: 4,
          averageClosure: 0.08,
          averageShapeConfidence: 0.88
        }
      })
    });

    expect(recognition.status).toBe("incomplete");
    expect(recognition.operator).toBeUndefined();
    expect(recognition.invalidReason).toContain("void_cut");
    expect(recognition.shadow?.personalizationStage).not.toBe("none");
    expect(recognition.shadow?.personalizationMix).toBeGreaterThan(0);
    expect(recognition.shadow?.personalizedCandidates?.length).toBeGreaterThan(0);
    expect(recognition.shadow?.personalizedShadowStatus).not.toBe("recognized");
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
      overlaySession: makeSession([stroke]),
      personalizationProfile: createPersonalizationProfile({
        ice_bar: {
          operator: "ice_bar",
          sampleCount: 7,
          averageAngleRadians: 0,
          averageScaleRatio: 0.38,
          averageAnchorZoneId: "core",
          averageStraightness: 0.98,
          averageCorners: 1,
          averageClosure: 0.02,
          averageShapeConfidence: 0.9
        }
      })
    });

    expect(["ambiguous", "incomplete", "invalid"]).toContain(recognition.status);
    expect(recognition.operator).toBeUndefined();
  });

  it("does not let personalization boost an off-anchor void_cut stroke", () => {
    const stroke = fromOverlayTemplate("void_cut", {
      scale: 118,
      rotate: 0.04,
      translate: { x: 120, y: 110 }
    });
    const overlaySession = makeSession([stroke]);
    const baseline = recognizeOverlayStroke(stroke, {
      referenceFrame,
      existingOperators: [],
      overlaySession
    });
    const recognition = recognizeOverlayStroke(stroke, {
      referenceFrame,
      existingOperators: [],
      overlaySession,
      personalizationProfile: createPersonalizationProfile({
        void_cut: {
          operator: "void_cut",
          sampleCount: 7,
          averageAngleRadians: Math.PI / 4,
          averageScaleRatio: 0.34,
          averageAnchorZoneId: "upper_right",
          averageStraightness: 0.98,
          averageCorners: 2,
          averageClosure: 0.04,
          averageShapeConfidence: 0.92
        }
      })
    });

    expect(recognition.topCandidate?.operator).toBe(baseline.topCandidate?.operator);
    expect(recognition.topCandidate?.score).toBeLessThanOrEqual((baseline.topCandidate?.score ?? 0) + 0.001);
  });

  it("records operator shadow evaluation while keeping dependency violations blocked", () => {
    const runtime = getTinyMlRuntimeStatus();
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
    expect(recognition.invalidReason).toContain("void_cut");
    expect(recognition.shadow).toBeDefined();
    expect(recognition.shadow?.actualTopLabel).toBe(recognition.topCandidate?.operator);
    expect(recognition.shadow?.actualStatus).toBe(recognition.status);
    expect(recognition.shadow?.candidates.length).toBeGreaterThan(0);
    expect(recognition.shadow?.personalizationStage).toBe("none");
    expect(recognition.shadow?.personalizationMix).toBe(0);
    expect(recognition.shadow?.personalizedShadowTopLabel).toBe(recognition.shadow?.shadowTopLabel);
    expect(recognition.shadow?.personalizedCandidates?.length).toBe(recognition.shadow?.candidates.length);

    if (runtime.operatorShadowAvailable) {
      expect(recognition.shadow?.mode).toBe("shadow");
      expect(recognition.shadow?.artifactVersion).toBeTruthy();
    } else {
      expect(recognition.shadow?.mode).toBe("heuristic_only");
    }
  });
});

describe("overlay operator personalization rerank", () => {
  it("reranks the void_cut/electric_fork hard negative using prototype similarity", () => {
    const candidates = rerankOverlayCandidates(
      [
        makeRerankCandidate("electric_fork", {
          score: 0.716,
          baseScore: 0.716,
          shapeConfidence: 0.64,
          angleRadians: Math.PI / 4,
          scaleRatio: 0.34,
          straightness: 0.94,
          corners: 2.2,
          closure: 0.04
        }),
        makeRerankCandidate("void_cut", {
          score: 0.704,
          baseScore: 0.704,
          shapeConfidence: 0.84,
          angleRadians: Math.PI / 4,
          scaleRatio: 0.34,
          straightness: 0.94,
          corners: 2.2,
          closure: 0.04
        })
      ],
      {
        profile: createPersonalizationProfile(
          {
            void_cut: {
              operator: "void_cut",
              sampleCount: 6,
              averageAngleRadians: Math.PI / 4,
              averageScaleRatio: 0.34,
              averageAnchorZoneId: "upper_right",
              averageStraightness: 0.96,
              averageCorners: 2,
              averageClosure: 0.04,
              averageShapeConfidence: 0.88
            },
            electric_fork: {
              operator: "electric_fork",
              sampleCount: 6,
              averageAngleRadians: 0.22,
              averageScaleRatio: 0.24,
              averageAnchorZoneId: "upper_right",
              averageStraightness: 0.76,
              averageCorners: 4,
              averageClosure: 0.08,
              averageShapeConfidence: 0.7
            }
          },
          [{ left: "void_cut", right: "electric_fork", preferred: "void_cut", weight: 0.8 }]
        )
      }
    );

    expect(candidates[0].operator).toBe("void_cut");
    expect(candidates[0].score).toBeGreaterThan(candidates[1].score);
    expect(candidates[0].shapeConfidence).toBeGreaterThan(candidates[1].shapeConfidence);
  });

  it("builds live overlay personalization from the tutorial store for the hard-negative pair", () => {
    let store = createEmptyTutorialProfileStore();
    const localBaseSession = fromGlyphTemplate("earth", {
      scale: 220,
      rotate: -0.34,
      translate: { x: 300, y: 270 },
      timeStep: 20
    });
    const localReferenceFrame = createOverlayReferenceFrame(localBaseSession);

    for (const capture of [
      { operator: "void_cut" as const, variation: { scale: 118, rotate: 0.04, translate: { x: 470, y: 220 } } },
      { operator: "void_cut" as const, variation: { scale: 114, rotate: 0.06, translate: { x: 468, y: 224 } } },
      { operator: "void_cut" as const, variation: { scale: 120, rotate: 0.02, translate: { x: 472, y: 216 } } },
      { operator: "electric_fork" as const, variation: { scale: 96, rotate: 0, translate: { x: 452, y: 198 } } },
      { operator: "electric_fork" as const, variation: { scale: 94, rotate: 0.05, translate: { x: 448, y: 204 } } }
    ]) {
      const stroke = fromOverlayTemplate(capture.operator, capture.variation);
      const operatorContext = createTutorialOperatorContext(stroke, {
        referenceFrame: localReferenceFrame,
        existingOperators: []
      });
      store = appendTutorialCapture(store, {
        kind: "operator",
        expectedOperator: capture.operator,
        source: "trace",
        strokes: [stroke],
        baseSnapshot: {
          centroid: { ...localReferenceFrame.centroid },
          bounds: { ...localReferenceFrame.bounds },
          diagonal: localReferenceFrame.diagonal,
          axisAngleRadians: localReferenceFrame.axisAngleRadians
        },
        operatorContext
      });
    }

    store = {
      ...store,
      calibration: {
        userPrototypeWeight: 0.14,
        rerankStrength: 0.24,
        confidenceBias: 0.16
      }
    };

    const profile = createTutorialOverlayPersonalizationProfile(store);
    const candidates = rerankOverlayCandidates(
      [
        makeRerankCandidate("electric_fork", {
          score: 0.716,
          baseScore: 0.716,
          shapeConfidence: 0.64,
          angleRadians: Math.PI / 4,
          scaleRatio: 0.34,
          straightness: 0.94,
          corners: 2.2,
          closure: 0.04
        }),
        makeRerankCandidate("void_cut", {
          score: 0.704,
          baseScore: 0.704,
          shapeConfidence: 0.84,
          angleRadians: Math.PI / 4,
          scaleRatio: 0.34,
          straightness: 0.94,
          corners: 2.2,
          closure: 0.04
        })
      ],
      { profile }
    );

    expect(profile?.sampleCount).toBeGreaterThanOrEqual(5);
    expect(profile?.operatorPrototypes?.void_cut?.averageStraightness).toBeDefined();
    expect(profile?.operatorPrototypes?.void_cut?.averageCorners).toBeDefined();
    expect(profile?.operatorPrototypes?.void_cut?.averageAnchorZoneId).toBeDefined();
    expect(profile?.operatorPrototypes?.void_cut?.averageScaleRatio).toBeGreaterThan(0.25);
    expect(candidates[0].operator).toBe("void_cut");
  });

  it("hydrates legacy operator captures without placement metadata", () => {
    const hydrated = hydrateTutorialProfileStore({
      version: "v1.5",
      captures: [
        {
          id: "legacy-void-cut",
          kind: "operator",
          expectedOperator: "void_cut",
          source: "trace",
          timestamp: 1,
          strokes: [
            fromOverlayTemplate("void_cut", {
              scale: 118,
              rotate: 0.04,
              translate: { x: 470, y: 220 }
            })
          ]
        }
      ],
      updatedAt: 1
    });

    expect(hydrated.captures).toHaveLength(1);
    expect(hydrated.captures[0].operatorContext).toBeUndefined();
    expect(hydrated.shapeProfile.operatorPrototypes.void_cut?.sampleCount).toBe(1);
  });

  it("caps rerank gains when anchor and scale fit are weak", () => {
    const candidates = rerankOverlayCandidates(
      [
        makeRerankCandidate("electric_fork", {
          score: 0.724,
          baseScore: 0.724,
          shapeConfidence: 0.67,
          angleRadians: Math.PI / 4,
          scaleRatio: 0.34,
          straightness: 0.94,
          corners: 2.2,
          closure: 0.04,
          anchorScore: 0.9,
          scaleScore: 0.86
        }),
        makeRerankCandidate("void_cut", {
          score: 0.708,
          baseScore: 0.708,
          shapeConfidence: 0.82,
          angleRadians: Math.PI / 4,
          scaleRatio: 0.18,
          straightness: 0.94,
          corners: 2.2,
          closure: 0.04,
          anchorZoneId: "upper_left",
          anchorScore: 0.16,
          scaleScore: 0.14
        })
      ],
      {
        profile: createPersonalizationProfile(
          {
            void_cut: {
              operator: "void_cut",
              sampleCount: 6,
              averageAngleRadians: Math.PI / 4,
              averageScaleRatio: 0.34,
              averageAnchorZoneId: "upper_right",
              averageStraightness: 0.96,
              averageCorners: 2,
              averageClosure: 0.04,
              averageShapeConfidence: 0.88
            },
            electric_fork: {
              operator: "electric_fork",
              sampleCount: 6,
              averageAngleRadians: 0.2,
              averageScaleRatio: 0.24,
              averageAnchorZoneId: "upper_right",
              averageStraightness: 0.74,
              averageCorners: 4,
              averageClosure: 0.08,
              averageShapeConfidence: 0.7
            }
          },
          [{ left: "void_cut", right: "electric_fork", preferred: "void_cut", weight: 1 }]
        )
      }
    );

    expect(candidates[0].operator).toBe("electric_fork");
    expect(candidates.find((candidate) => candidate.operator === "void_cut")?.score).toBeLessThan(0.724);
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

function createPersonalizationProfile(
  operatorPrototypes: NonNullable<OverlayPersonalizationProfile["operatorPrototypes"]>,
  confusionBiases: OverlayPersonalizationProfile["confusionBiases"] = []
): OverlayPersonalizationProfile {
  return {
    sampleCount: 9,
    rerankStrength: 0.24,
    confidenceBias: 0.16,
    operatorPrototypes,
    confusionBiases
  };
}

function makeRerankCandidate(
  operator: OverlayOperator,
  overrides: Partial<OverlayRerankCandidate> = {}
): OverlayRerankCandidate {
  const score = overrides.score ?? 0.7;

  return {
    operator,
    score,
    baseScore: overrides.baseScore ?? score,
    templateDistance: overrides.templateDistance ?? 0.24,
    shapeConfidence: overrides.shapeConfidence ?? 0.72,
    anchorZoneId: overrides.anchorZoneId ?? "upper_right",
    placementAnchorZoneId: overrides.placementAnchorZoneId ?? overrides.anchorZoneId ?? "upper_right",
    anchorScore: overrides.anchorScore ?? 0.9,
    scaleScore: overrides.scaleScore ?? 0.86,
    angleRadians: overrides.angleRadians ?? Math.PI / 4,
    scaleRatio: overrides.scaleRatio ?? 0.34,
    straightness: overrides.straightness ?? 0.9,
    corners: overrides.corners ?? 2,
    closure: overrides.closure ?? 0.06,
    stackIndex: overrides.stackIndex ?? 0,
    existingOperators: overrides.existingOperators ?? [],
    notes: overrides.notes ?? []
  };
}
