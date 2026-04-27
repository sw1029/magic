import { describe, expect, it } from "vitest";
import { compileSealResult } from "../src/recognizer/compile";
import { OVERLAY_OPERATOR_TEMPLATES } from "../src/recognizer/operator-templates";
import { createOverlayReferenceFrame, recognizeOverlayStroke } from "../src/recognizer/operators";
import {
  appendTutorialCapture,
  createEmptyTutorialProfileStore,
  mergeTutorializedUserProfile
} from "../src/recognizer/tutorial-profile";
import { createEmptyUserInputProfile, updateUserInputProfile } from "../src/recognizer/user-profile";
import { recognizeSession } from "../src/recognizer/recognize";
import { GLYPH_TEMPLATES } from "../src/recognizer/templates";
import type { GlyphFamily, OverlayOperator, OverlayStrokeRecord, Stroke, StrokeSession, UserInputProfile } from "../src/recognizer/types";

describe("magic recognizer v1.5", () => {
  it("keeps canonical family fixed while adjusted quality reflects the user profile", () => {
    const session = fromGlyphTemplate("fire", {
      scale: 190,
      rotate: 0.22,
      translate: { x: 300, y: 250 },
      timeStep: 24
    });
    const profile: UserInputProfile = {
      ...createEmptyUserInputProfile(),
      sampleCount: 8,
      averageQuality: {
        closure: 0.92,
        symmetry: 0.24,
        smoothness: 0.88,
        tempo: 0.12,
        overshoot: 0.68,
        stability: 0.3,
        rotationBias: 0.18
      }
    };
    const result = recognizeSession(session, { sealed: true, profile });

    expect(result.canonicalFamily).toBe("fire");
    expect(result.rawQuality.tempo).not.toBe(result.adjustedQuality.tempo);
    expect(result.quality).toEqual(result.rawQuality);
  });

  it("uses baseline fallback and keeps adjustment weak when sampleCount is low", () => {
    const session = fromGlyphTemplate("fire", {
      scale: 184,
      rotate: 0.18,
      translate: { x: 300, y: 245 },
      timeStep: 26
    });
    const lowSampleProfile: UserInputProfile = {
      ...createEmptyUserInputProfile(),
      sampleCount: 2,
      averageQuality: {
        closure: 0.92,
        symmetry: 0.35,
        smoothness: 0.78,
        tempo: 0.32,
        overshoot: 0.64,
        stability: 0.4,
        rotationBias: 0.12
      }
    };
    const highSampleProfile: UserInputProfile = {
      ...lowSampleProfile,
      sampleCount: 18
    };

    const lowSampleResult = recognizeSession(session, { sealed: true, profile: lowSampleProfile });
    const highSampleResult = recognizeSession(session, { sealed: true, profile: highSampleProfile });

    expect(lowSampleResult.canonicalFamily).toBe("fire");
    expect(highSampleResult.canonicalFamily).toBe("fire");
    expect(Math.abs(highSampleResult.adjustedQuality.tempo - highSampleResult.rawQuality.tempo)).toBeGreaterThan(
      Math.abs(lowSampleResult.adjustedQuality.tempo - lowSampleResult.rawQuality.tempo)
    );
    expect(
      Math.abs(highSampleResult.adjustedQuality.stability - highSampleResult.rawQuality.stability)
    ).toBeGreaterThan(Math.abs(lowSampleResult.adjustedQuality.stability - lowSampleResult.rawQuality.stability));
  });

  it("keeps fast and slow inputs in the same family while adjusted tempo becomes more stable", () => {
    const comfortProfile: UserInputProfile = {
      ...createEmptyUserInputProfile(),
      sampleCount: 18,
      averageQuality: {
        closure: 0.86,
        symmetry: 0.48,
        smoothness: 0.76,
        tempo: 0.78,
        overshoot: 0.62,
        stability: 0.7,
        rotationBias: 0.2
      }
    };
    const fast = fromGlyphTemplate("fire", {
      scale: 186,
      rotate: 0.16,
      translate: { x: 290, y: 250 },
      timeStep: 8
    });
    const slow = fromGlyphTemplate("fire", {
      scale: 186,
      rotate: 0.16,
      translate: { x: 290, y: 250 },
      timeStep: 42
    });

    const fastResult = recognizeSession(fast, { sealed: true, profile: comfortProfile });
    const slowResult = recognizeSession(slow, { sealed: true, profile: comfortProfile });
    const rawGap = Math.abs(fastResult.rawQuality.tempo - slowResult.rawQuality.tempo);
    const adjustedGap = Math.abs(fastResult.adjustedQuality.tempo - slowResult.adjustedQuality.tempo);

    expect(fastResult.canonicalFamily).toBe("fire");
    expect(slowResult.canonicalFamily).toBe("fire");
    expect(rawGap).toBeGreaterThan(0.08);
    expect(adjustedGap).toBeLessThan(rawGap);
    expect(fastResult.adjustedQuality.tempo).not.toBe(fastResult.rawQuality.tempo);
    expect(slowResult.adjustedQuality.tempo).not.toBe(slowResult.rawQuality.tempo);
  });

  it("does not let personalization flip invalid input into recognized", () => {
    const aggressiveProfile: UserInputProfile = {
      ...createEmptyUserInputProfile(),
      sampleCount: 24,
      averageQuality: {
        closure: 1,
        symmetry: 1,
        smoothness: 1,
        tempo: 0.8,
        overshoot: 1,
        stability: 1,
        rotationBias: 0
      }
    };
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
    const result = recognizeSession(session, { sealed: true, profile: aggressiveProfile });

    expect(result.status).toBe("incomplete");
    expect(result.canonicalFamily).toBeUndefined();
  });

  it("uses tutorial family prototypes to turn an earth-vs-fire near tie into a calibrated recognition", () => {
    const session = makeSession([
      makeStroke(
        [
          [210, 340],
          [240, 140],
          [360, 140],
          [390, 340],
          [210, 340]
        ],
        0
      )
    ]);
    const baseline = recognizeSession(session, { sealed: true });
    const personalized = recognizeSession(session, {
      sealed: true,
      profile: createTutorializedProfile({
        sampleCount: 8,
        tutorialSampleCount: 8,
        familyPrototypes: {
          earth: createFamilyPrototype("earth", [
            { scale: 210, rotate: -0.12, translate: { x: 300, y: 270 }, timeStep: 20 },
            { scale: 220, rotate: -0.26, translate: { x: 300, y: 270 }, noise: 4, timeStep: 24 }
          ]),
          fire: createFamilyPrototype("fire", [
            { scale: 205, rotate: 0.1, translate: { x: 300, y: 250 }, timeStep: 18 }
          ])
        },
        confusionPairs: [{ left: "earth", right: "fire", weight: 0.18 }],
        recognitionCalibration: {
          userPrototypeWeight: 0.14,
          rerankStrength: 0.14,
          confidenceBias: 0.03
        }
      })
    });
    const baselineMargin = (baseline.candidates[0]?.score ?? 0) - (baseline.candidates[1]?.score ?? 0);
    const personalizedMargin =
      (personalized.candidates[0]?.score ?? 0) - (personalized.candidates[1]?.score ?? 0);

    expect(baseline.status).toBe("ambiguous");
    expect(baseline.topCandidate?.family).toBe("earth");
    expect(baseline.candidates[1]?.family).toBe("fire");
    expect(personalized.status).toBe("recognized");
    expect(personalized.canonicalFamily).toBe("earth");
    expect(personalizedMargin).toBeGreaterThan(baselineMargin);
    expect(personalized.topCandidate?.notes.some((note) => note.startsWith("calibrated="))).toBe(true);
    expect(personalized.shadow?.personalizationStage).toBe("few_shot");
    expect(personalized.shadow?.personalizationMix).toBeGreaterThan(0);
    expect(personalized.shadow?.personalizedShadowTopLabel).toBeDefined();
    expect(personalized.shadow?.personalizedCandidates?.length).toBeGreaterThan(0);
  });

  it("does not let earth-biased tutorial prototypes flip a clear fire template", () => {
    const session = fromGlyphTemplate("fire", {
      scale: 188,
      rotate: 0.18,
      translate: { x: 300, y: 250 },
      timeStep: 22
    });
    const result = recognizeSession(session, {
      sealed: true,
      profile: createTutorializedProfile({
        sampleCount: 10,
        tutorialSampleCount: 10,
        familyPrototypes: {
          earth: createFamilyPrototype("earth", [
            { scale: 220, rotate: -0.22, translate: { x: 300, y: 270 }, timeStep: 20 },
            { scale: 214, rotate: -0.08, translate: { x: 300, y: 270 }, noise: 3, timeStep: 24 }
          ])
        },
        confusionPairs: [{ left: "earth", right: "fire", weight: 0.2 }],
        recognitionCalibration: {
          userPrototypeWeight: 0.16,
          rerankStrength: 0.16,
          confidenceBias: 0.03
        }
      })
    });

    expect(result.status).toBe("recognized");
    expect(result.topCandidate?.family).toBe("fire");
    expect(result.canonicalFamily).toBe("fire");
  });

  it("wires tutorial profile store into the live base recognizer path", () => {
    const session = makeSession([
      makeStroke(
        [
          [210, 340],
          [240, 140],
          [360, 140],
          [390, 340],
          [210, 340]
        ],
        0
      )
    ]);
    let store = createEmptyTutorialProfileStore();

    for (const capture of [
      { family: "earth" as const, variation: { scale: 210, rotate: -0.12, translate: { x: 300, y: 270 }, timeStep: 20 } },
      { family: "earth" as const, variation: { scale: 220, rotate: -0.26, translate: { x: 300, y: 270 }, noise: 4, timeStep: 24 } },
      { family: "earth" as const, variation: { scale: 214, rotate: -0.08, translate: { x: 300, y: 272 }, noise: 3, timeStep: 22 } },
      { family: "earth" as const, variation: { scale: 218, rotate: -0.18, translate: { x: 300, y: 274 }, timeStep: 21 } },
      { family: "fire" as const, variation: { scale: 205, rotate: 0.1, translate: { x: 300, y: 250 }, timeStep: 18 } },
      { family: "fire" as const, variation: { scale: 208, rotate: 0.16, translate: { x: 300, y: 248 }, noise: 2, timeStep: 19 } }
    ]) {
      store = appendTutorialCapture(store, {
        kind: "family",
        expectedFamily: capture.family,
        source: "trace",
        strokes: fromGlyphTemplate(capture.family, capture.variation).strokes
      });
    }

    store = {
      ...store,
      calibration: {
        userPrototypeWeight: 0.14,
        rerankStrength: 0.14,
        confidenceBias: 0.03
      }
    };

    const baseline = recognizeSession(session, { sealed: true });
    const personalized = recognizeSession(session, {
      sealed: true,
      profile: mergeTutorializedUserProfile(createEmptyUserInputProfile(), store)
    });

    expect(store.shapeProfile.tutorialSampleCount).toBeGreaterThanOrEqual(6);
    expect(baseline.status).toBe("ambiguous");
    expect(personalized.status).toBe("recognized");
    expect(personalized.canonicalFamily).toBe("earth");
  });

  it("keeps feedback-only tutorial captures out of personalization prototypes", () => {
    const store = appendTutorialCapture(createEmptyTutorialProfileStore(), {
      kind: "family",
      expectedFamily: "fire",
      source: "variation",
      strokes: fromGlyphTemplate("earth", {
        scale: 210,
        rotate: -0.12,
        translate: { x: 300, y: 270 },
        timeStep: 20
      }).strokes,
      validation: {
        reliability: "feedback_only",
        expectedLabel: "fire",
        actualTopLabel: "earth",
        status: "recognized",
        topScore: 0.9,
        margin: 0.22
      }
    });

    expect(store.captures[0].validation?.reliability).toBe("feedback_only");
    expect(store.shapeProfile.tutorialSampleCount).toBe(1);
    expect(store.shapeProfile.familyTutorialSampleCount).toBe(0);
    expect(store.shapeProfile.feedbackOnlyTutorialSampleCount).toBe(1);
    expect(store.shapeProfile.familyPrototypes.fire).toBeUndefined();
    expect(store.calibration.userPrototypeWeight).toBe(0);
  });

  it("builds label-specific threshold bias only from validated tutorial captures", () => {
    let store = createEmptyTutorialProfileStore();

    for (const variation of [
      { scale: 210, rotate: -0.12, translate: { x: 300, y: 270 }, timeStep: 20 },
      { scale: 218, rotate: -0.18, translate: { x: 300, y: 274 }, timeStep: 21 }
    ]) {
      store = appendTutorialCapture(store, {
        kind: "family",
        expectedFamily: "earth",
        source: "variation",
        strokes: fromGlyphTemplate("earth", variation).strokes,
        validation: {
          reliability: "high",
          expectedLabel: "earth",
          actualTopLabel: "earth",
          status: "recognized",
          topScore: 0.86,
          margin: 0.18
        }
      });
    }

    expect(store.shapeProfile.familyTutorialSampleCount).toBe(2);
    expect(store.shapeProfile.familyPrototypes.earth?.sampleCount).toBe(2);
    expect(store.shapeProfile.familyPrototypeReliability?.earth).toBeGreaterThan(0.95);
    expect(store.shapeProfile.familyThresholdBias?.earth).toBeGreaterThan(0);
  });

  it("does not let operator-only tutorial captures activate base threshold personalization", () => {
    let store = createEmptyTutorialProfileStore();

    for (const variation of [
      { scale: 118, rotate: 0.04, translate: { x: 470, y: 220 } },
      { scale: 114, rotate: 0.06, translate: { x: 468, y: 224 } },
      { scale: 120, rotate: 0.02, translate: { x: 472, y: 216 } },
      { scale: 116, rotate: 0.01, translate: { x: 466, y: 218 } },
      { scale: 121, rotate: 0.05, translate: { x: 474, y: 222 } },
      { scale: 117, rotate: 0.03, translate: { x: 469, y: 221 } }
    ]) {
      store = appendTutorialCapture(store, {
        kind: "operator",
        expectedOperator: "void_cut",
        source: "variation",
        strokes: [fromOverlayTemplate("void_cut", variation)],
        validation: {
          reliability: "high",
          expectedLabel: "void_cut",
          actualTopLabel: "void_cut",
          status: "recognized",
          topScore: 0.86,
          margin: 0.16
        }
      });
    }

    const session = fromGlyphTemplate("fire", {
      scale: 188,
      rotate: 0.18,
      translate: { x: 300, y: 250 },
      timeStep: 22
    });
    const result = recognizeSession(session, {
      sealed: true,
      profile: mergeTutorializedUserProfile(createEmptyUserInputProfile(), store)
    });

    expect(store.shapeProfile.tutorialSampleCount).toBe(6);
    expect(store.shapeProfile.operatorTutorialSampleCount).toBe(6);
    expect(store.shapeProfile.familyTutorialSampleCount).toBe(0);
    expect(result.personalization?.stage).toBe("none");
    expect(result.personalization?.thresholdBias).toBe(0);
    expect(result.personalization?.effectiveThresholdBias).toBe(0);
  });

  it("keeps a clear water template fixed even when the tutorial store is biased toward life", () => {
    let store = createEmptyTutorialProfileStore();

    for (const capture of [
      { family: "life" as const, variation: { scale: 182, rotate: -0.08, translate: { x: 300, y: 250 }, timeStep: 20 } },
      { family: "life" as const, variation: { scale: 176, rotate: 0.12, translate: { x: 302, y: 252 }, noise: 2, timeStep: 18 } },
      { family: "life" as const, variation: { scale: 188, rotate: -0.16, translate: { x: 298, y: 248 }, timeStep: 22 } },
      { family: "life" as const, variation: { scale: 180, rotate: 0.04, translate: { x: 300, y: 246 }, noise: 3, timeStep: 21 } }
    ]) {
      store = appendTutorialCapture(store, {
        kind: "family",
        expectedFamily: capture.family,
        source: "recall",
        strokes: fromGlyphTemplate(capture.family, capture.variation).strokes
      });
    }

    store = {
      ...store,
      calibration: {
        userPrototypeWeight: 0.16,
        rerankStrength: 0.16,
        confidenceBias: 0.03
      }
    };

    const session = fromGlyphTemplate("water", {
      scale: 178,
      rotate: 0.05,
      translate: { x: 300, y: 250 },
      timeStep: 24
    });
    const result = recognizeSession(session, {
      sealed: true,
      profile: mergeTutorializedUserProfile(createEmptyUserInputProfile(), store)
    });

    expect(result.status).toBe("recognized");
    expect(result.canonicalFamily).toBe("water");
    expect(result.topCandidate?.family).toBe("water");
  });

  it("recognizes overlay operators and compiles base + overlay + profile delta", () => {
    const profile = createEmptyUserInputProfile();
    const baseSession = fromGlyphTemplate("earth", {
      scale: 220,
      rotate: -0.34,
      translate: { x: 300, y: 270 },
      timeStep: 20
    });
    const baseResult = recognizeSession(baseSession, { sealed: true, profile });

    expect(baseResult.canonicalFamily).toBe("earth");

    const profileUpdate = updateUserInputProfile(profile, baseResult);
    const referenceFrame = createOverlayReferenceFrame(baseSession);
    const overlayRecords = [
      recognizeOverlayRecord("void_cut", referenceFrame, [], 118, { x: 470, y: 220 }),
      recognizeOverlayRecord("martial_axis", referenceFrame, ["void_cut"], 118, { x: 470, y: 360 }),
      recognizeOverlayRecord("soul_dot", referenceFrame, ["void_cut", "martial_axis"], 24, { x: 240, y: 200 })
    ];
    const compiled = compileSealResult(baseResult, overlayRecords, profileUpdate.delta);

    expect(
      compiled.overlayOperators.map((recognition) => recognition.operator).filter((operator): operator is OverlayOperator => Boolean(operator))
    ).toEqual(["void_cut", "martial_axis", "soul_dot"]);
    expect(compiled.baseFamily).toBe("earth");
    expect(compiled.rawQuality).toEqual(baseResult.rawQuality);
    expect(compiled.adjustedQuality).toEqual(baseResult.adjustedQuality);
    expect(compiled.profileDelta?.nextSampleCount).toBe(1);
  });

  it("only recognizes martial_axis after void_cut is already in the overlay stack", () => {
    const stroke = fromOverlayTemplate("martial_axis", {
      scale: 112,
      rotate: 0,
      translate: { x: 460, y: 280 }
    });

    const blocked = recognizeOverlayStroke(stroke, {
      referenceFrame: createOverlayReferenceFrame(
        fromGlyphTemplate("earth", {
          scale: 220,
          rotate: -0.34,
          translate: { x: 300, y: 270 },
          timeStep: 20
        })
      ),
      existingOperators: []
    });
    const enabled = recognizeOverlayStroke(stroke, {
      referenceFrame: createOverlayReferenceFrame(
        fromGlyphTemplate("earth", {
          scale: 220,
          rotate: -0.34,
          translate: { x: 300, y: 270 },
          timeStep: 20
        })
      ),
      existingOperators: ["void_cut"]
    });

    expect(["incomplete", "invalid"]).toContain(blocked.status);
    expect(blocked.invalidReason).toContain("void_cut");
    expect(enabled.status).toBe("recognized");
    expect(enabled.operator).toBe("martial_axis");
  });

  it("keeps the sealed base family fixed when an invalid overlay is added", () => {
    const profile = createEmptyUserInputProfile();
    const baseSession = fromGlyphTemplate("earth", {
      scale: 220,
      rotate: -0.34,
      translate: { x: 300, y: 270 },
      timeStep: 20
    });
    const baseResult = recognizeSession(baseSession, { sealed: true, profile });
    const invalidOverlayStroke: Stroke = {
      id: "invalid-overlay",
      points: [{ x: 512, y: 178, t: 0 }]
    };
    const invalidOverlayRecognition = recognizeOverlayStroke(invalidOverlayStroke, {
      referenceFrame: createOverlayReferenceFrame(baseSession),
      existingOperators: [],
      overlaySession: makeSession([invalidOverlayStroke])
    });
    const compiled = compileSealResult(
      baseResult,
      [{ stroke: invalidOverlayStroke, recognition: invalidOverlayRecognition }],
      undefined
    );

    expect(baseResult.canonicalFamily).toBe("earth");
    expect(invalidOverlayRecognition.status).toBe("invalid");
    expect(compiled.baseFamily).toBe("earth");
    expect(compiled.overlayOperators).toHaveLength(0);
    expect(compiled.rawQuality).toEqual(baseResult.rawQuality);
  });
});

function fromGlyphTemplate(
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

  const strokes = template.strokes.map((stroke, strokeIndex) => ({
    id: `${family}-${strokeIndex}`,
    points: stroke.points.map((point, pointIndex) => transformPoint(point, strokeIndex, pointIndex, options))
  }));

  return makeSession(strokes);
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

  const baseStroke = template.strokes[0];

  return {
    id: `${operator}-stroke`,
    points: baseStroke.points.map((point, pointIndex) => transformPoint(point, 0, pointIndex, options))
  };
}

function recognizeOverlayRecord(
  operator: OverlayOperator,
  referenceFrame: ReturnType<typeof createOverlayReferenceFrame>,
  existingOperators: OverlayOperator[],
  scale: number,
  translate: { x: number; y: number }
): OverlayStrokeRecord {
  const stroke = fromOverlayTemplate(operator, {
    scale,
    rotate: operator === "void_cut" ? 0.04 : 0,
    translate
  });
  const recognition = recognizeOverlayStroke(stroke, { referenceFrame, existingOperators });

  expect(recognition.status).toBe("recognized");
  expect(recognition.operator).toBe(operator);

  return { stroke, recognition };
}

function transformPoint(
  point: { x: number; y: number; t: number },
  strokeIndex: number,
  pointIndex: number,
  options: {
    scale: number;
    rotate: number;
    translate: { x: number; y: number };
    noise?: number;
    timeStep?: number;
  }
): { x: number; y: number; t: number } {
  const jitter = options.noise ?? 0;
  const cosine = Math.cos(options.rotate);
  const sine = Math.sin(options.rotate);
  const nx = point.x + Math.sin(pointIndex + strokeIndex * 1.7) * jitter;
  const ny = point.y + Math.cos(pointIndex * 1.3 + strokeIndex) * jitter;
  const rotatedX = nx * cosine - ny * sine;
  const rotatedY = nx * sine + ny * cosine;

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

function createFamilyPrototype(
  family: GlyphFamily,
  variations: Array<{
    scale: number;
    rotate: number;
    translate: { x: number; y: number };
    noise?: number;
    timeStep?: number;
  }>
): {
  family: GlyphFamily;
  normalizedClouds: Array<StrokeSession["strokes"][number]["points"]>;
  averageFeatures: Record<string, number>;
  sampleCount: number;
} {
  const samples = variations.map((variation) => recognizeSession(fromGlyphTemplate(family, variation), { sealed: false }));
  const averagedFeatures = samples.reduce<Record<string, number>>((accumulator, sample, sampleIndex) => {
    const featureEntries = Object.entries(sample.features);

    for (const [key, value] of featureEntries) {
      accumulator[key] = ((accumulator[key] ?? 0) * sampleIndex + value) / (sampleIndex + 1);
    }

    return accumulator;
  }, {});

  return {
    family,
    normalizedClouds: samples.map((sample) => sample.normalizedStrokes.flat()),
    averageFeatures: averagedFeatures,
    sampleCount: samples.length
  };
}

function createTutorializedProfile(input: {
  sampleCount: number;
  tutorialSampleCount: number;
  familyPrototypes: Partial<
    Record<
      GlyphFamily,
      {
        family: GlyphFamily;
        normalizedClouds: Array<StrokeSession["strokes"][number]["points"]>;
        averageFeatures: Record<string, number>;
        sampleCount: number;
      }
    >
  >;
  confusionPairs: Array<{ left: string; right: string; weight: number }>;
  recognitionCalibration: {
    userPrototypeWeight: number;
    rerankStrength: number;
    confidenceBias: number;
  };
}): UserInputProfile {
  return {
    ...createEmptyUserInputProfile(),
    sampleCount: input.sampleCount,
    tutorialProfile: {
      tutorialSampleCount: input.tutorialSampleCount,
      familyPrototypes: input.familyPrototypes,
      confusionPairs: input.confusionPairs,
      recognitionCalibration: input.recognitionCalibration
    }
  } as UserInputProfile;
}
