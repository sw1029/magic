import {
  boundingBox,
  clusterEndpointCount,
  lineAngle,
  normalizeAngleHalfPi,
  normalizeStrokes,
  pathLength,
  pointCloudDistance,
  rdpSimplify,
  strokeStraightness
} from "./geometry";
import type {
  FamilyPrototype,
  GlyphFamily,
  OperatorPrototype,
  OverlayOperator,
  PointSample,
  RecognitionCalibration,
  RecognitionFeatures,
  Stroke,
  StrokeBounds,
  TutorialCapture,
  TutorialBaseSnapshot,
  TutorialCaptureSource,
  TutorialCaptureValidation,
  TutorialOperatorContext,
  TutorialProfileStore,
  UserInputProfile,
  UserShapeProfile
} from "./types";
import type { OverlayPersonalizationProfile } from "./rerank";

export interface TutorialCaptureInput {
  id?: string;
  kind: TutorialCapture["kind"];
  expectedFamily?: GlyphFamily;
  expectedOperator?: OverlayOperator;
  strokes: Stroke[];
  source: TutorialCaptureSource | "variant";
  timestamp?: number;
  baseSnapshot?: TutorialBaseSnapshot;
  operatorContext?: TutorialOperatorContext;
  validation?: TutorialCaptureValidation;
}

export const TUTORIAL_FAMILY_ORDER: GlyphFamily[] = ["wind", "earth", "fire", "water", "life"];
export const TUTORIAL_OPERATOR_ORDER: OverlayOperator[] = [
  "steel_brace",
  "electric_fork",
  "ice_bar",
  "soul_dot",
  "void_cut",
  "martial_axis"
];

const RECOGNITION_FEATURE_KEYS: Array<keyof RecognitionFeatures> = [
  "strokeCount",
  "pointCount",
  "durationMs",
  "pathLength",
  "closureGap",
  "dominantCorners",
  "endpointClusters",
  "circularity",
  "fillRatio",
  "parallelism",
  "rawAngleRadians"
];

const MAX_PROTOTYPE_SAMPLES = 12;
const MIN_CALIBRATION_SAMPLES = 4;
const FULL_CALIBRATION_SAMPLES = 24;
const CONFUSION_THRESHOLD = 0.22;

export function createEmptyRecognitionCalibration(): RecognitionCalibration {
  return {
    userPrototypeWeight: 0,
    rerankStrength: 0,
    confidenceBias: 0
  };
}

export function createEmptyUserShapeProfile(updatedAt = Date.now()): UserShapeProfile {
  return {
    tutorialSampleCount: 0,
    familyTutorialSampleCount: 0,
    operatorTutorialSampleCount: 0,
    validatedTutorialSampleCount: 0,
    feedbackOnlyTutorialSampleCount: 0,
    familyPrototypes: {},
    operatorPrototypes: {},
    familyThresholdBias: {},
    operatorThresholdBias: {},
    familyPrototypeReliability: {},
    operatorPrototypeReliability: {},
    confusionPairs: [],
    updatedAt
  };
}

export function createEmptyTutorialProfileStore(updatedAt = Date.now()): TutorialProfileStore {
  return {
    version: "v1.5",
    captures: [],
    shapeProfile: createEmptyUserShapeProfile(updatedAt),
    calibration: createEmptyRecognitionCalibration(),
    updatedAt
  };
}

export function createTutorialCapture(input: TutorialCaptureInput): TutorialCapture {
  const normalizedSource = normalizeCaptureSource(input.source);
  const timestamp = typeof input.timestamp === "number" ? input.timestamp : Date.now();

  if (input.kind === "family" && !input.expectedFamily) {
    throw new Error("expectedFamily is required for family tutorial captures");
  }

  if (input.kind === "operator" && !input.expectedOperator) {
    throw new Error("expectedOperator is required for operator tutorial captures");
  }

  return {
    id: input.id ?? crypto.randomUUID(),
    kind: input.kind,
    expectedFamily: input.kind === "family" ? input.expectedFamily : undefined,
    expectedOperator: input.kind === "operator" ? input.expectedOperator : undefined,
    strokes: cloneStrokes(input.strokes),
    source: normalizedSource,
    timestamp,
    baseSnapshot: cloneBaseSnapshot(input.baseSnapshot),
    operatorContext: cloneOperatorContext(input.operatorContext),
    validation: cloneTutorialCaptureValidation(input.validation)
  };
}

export function appendTutorialCapture(
  previousStore: TutorialProfileStore | undefined,
  input: TutorialCaptureInput
): TutorialProfileStore {
  const previous = previousStore ?? createEmptyTutorialProfileStore();
  const capture = createTutorialCapture(input);
  const captures = upsertCapture(previous.captures, capture);

  return rebuildTutorialProfileStore(captures, capture.timestamp);
}

export function rebuildTutorialProfileStore(
  captures: TutorialCapture[],
  updatedAt = latestCaptureTimestamp(captures)
): TutorialProfileStore {
  const sanitizedCaptures = captures
    .map((capture) => coerceTutorialCapture(capture))
    .filter((capture): capture is TutorialCapture => Boolean(capture));
  const safeUpdatedAt = updatedAt > 0 ? updatedAt : Date.now();
  const shapeProfile = buildUserShapeProfile(sanitizedCaptures, safeUpdatedAt);

  return {
    version: "v1.5",
    captures: sanitizedCaptures.map((capture) => ({
      ...capture,
      strokes: cloneStrokes(capture.strokes),
      baseSnapshot: cloneBaseSnapshot(capture.baseSnapshot),
      operatorContext: cloneOperatorContext(capture.operatorContext),
      validation: cloneTutorialCaptureValidation(capture.validation)
    })),
    shapeProfile,
    calibration: calculateRecognitionCalibration(shapeProfile, sanitizedCaptures.filter(isAdaptationCapture)),
    updatedAt: safeUpdatedAt
  };
}

export function hydrateTutorialProfileStore(raw: Partial<TutorialProfileStore> | null | undefined): TutorialProfileStore {
  if (!raw) {
    return createEmptyTutorialProfileStore();
  }

  const captures = Array.isArray(raw.captures)
    ? raw.captures
        .map((capture) => coerceTutorialCapture(capture as Partial<TutorialCapture>))
        .filter((capture): capture is TutorialCapture => Boolean(capture))
    : [];
  const updatedAt =
    typeof raw.updatedAt === "number" && Number.isFinite(raw.updatedAt)
      ? raw.updatedAt
      : latestCaptureTimestamp(captures);

  return rebuildTutorialProfileStore(captures, updatedAt);
}

export function mergeTutorializedUserProfile(
  profile: UserInputProfile,
  store: TutorialProfileStore | null | undefined
): UserInputProfile {
  const safeStore = store ?? createEmptyTutorialProfileStore();

  if (safeStore.shapeProfile.tutorialSampleCount === 0) {
    return profile;
  }

  return {
    ...profile,
    tutorialProfile: {
      ...safeStore.shapeProfile,
      recognitionCalibration: safeStore.calibration,
      calibration: safeStore.calibration
    },
    shapeProfile: {
      ...safeStore.shapeProfile,
      recognitionCalibration: safeStore.calibration,
      calibration: safeStore.calibration
    },
    recognitionCalibration: safeStore.calibration,
    calibration: safeStore.calibration
  };
}

export function createTutorialOverlayPersonalizationProfile(
  store: TutorialProfileStore | null | undefined
): OverlayPersonalizationProfile | undefined {
  const safeStore = store ?? createEmptyTutorialProfileStore();
  const operatorPrototypes = Object.entries(safeStore.shapeProfile.operatorPrototypes).reduce<
    NonNullable<OverlayPersonalizationProfile["operatorPrototypes"]>
  >((accumulator, [operator, prototype]) => {
    if (!prototype || prototype.sampleCount <= 0) {
      return accumulator;
    }

    accumulator[operator as OverlayOperator] = {
      operator: operator as OverlayOperator,
      sampleCount: prototype.sampleCount,
      reliability: prototype.reliability,
      averageAngleRadians: prototype.averageAngleRadians,
      averageScaleRatio: prototype.averageScaleRatio,
      averageAnchorZoneId: prototype.averageAnchorZoneId,
      averageStraightness: prototype.averageStraightness,
      averageCorners: prototype.averageCorners,
      averageClosure: prototype.averageClosure,
      averageStackIndex: prototype.averageStackIndex,
      existingOperatorBiases: prototype.existingOperatorBiases
    };
    return accumulator;
  }, {});
  const sampleCount = Math.max(
    safeStore.shapeProfile.operatorTutorialSampleCount ?? 0,
    Object.values(operatorPrototypes).reduce((sum, prototype) => sum + (prototype?.sampleCount ?? 0), 0)
  );

  if (sampleCount === 0) {
    return undefined;
  }

  return {
    sampleCount,
    tutorialSampleCount: sampleCount,
    operatorTutorialSampleCount: sampleCount,
    operatorPrototypes,
    operatorThresholdBias: safeStore.shapeProfile.operatorThresholdBias,
    operatorPrototypeReliability: safeStore.shapeProfile.operatorPrototypeReliability,
    recognitionCalibration: safeStore.calibration,
    calibration: safeStore.calibration
  };
}

export function calculateRecognitionCalibration(
  profile: UserShapeProfile,
  captures: TutorialCapture[] = []
): RecognitionCalibration {
  const adaptationSampleCount =
    profile.validatedTutorialSampleCount ??
    (profile.familyTutorialSampleCount ?? 0) + (profile.operatorTutorialSampleCount ?? 0);

  if (adaptationSampleCount === 0) {
    return createEmptyRecognitionCalibration();
  }

  const sampleMix = clamp(
    (adaptationSampleCount - MIN_CALIBRATION_SAMPLES) /
      Math.max(FULL_CALIBRATION_SAMPLES - MIN_CALIBRATION_SAMPLES, 1),
    0,
    1
  );
  const sourceCoverage = captureSourceCoverage(captures);
  const familyCoverage = Object.keys(profile.familyPrototypes).length / Math.max(TUTORIAL_FAMILY_ORDER.length, 1);
  const operatorCoverage = Object.keys(profile.operatorPrototypes).length / Math.max(TUTORIAL_OPERATOR_ORDER.length, 1);
  const targetCoverage = clamp((familyCoverage + operatorCoverage) / 2, 0, 1);
  const coverageMix = clamp(sourceCoverage * 0.45 + targetCoverage * 0.55, 0, 1);

  return {
    userPrototypeWeight: roundMetric(clamp(0.18 + sampleMix * 0.34 + coverageMix * 0.18, 0, 0.7)),
    rerankStrength: roundMetric(clamp(0.08 + sampleMix * 0.22 + coverageMix * 0.1, 0, 0.45)),
    confidenceBias: roundMetric(clamp(sampleMix * 0.08 + coverageMix * 0.06, 0, 0.16))
  };
}

export function buildUserShapeProfile(captures: TutorialCapture[], updatedAt = latestCaptureTimestamp(captures)): UserShapeProfile {
  if (captures.length === 0) {
    return createEmptyUserShapeProfile(updatedAt > 0 ? updatedAt : Date.now());
  }

  const adaptationCaptures = captures.filter(isAdaptationCapture);
  const feedbackOnlyTutorialSampleCount = captures.filter(
    (capture) => capture.validation?.reliability === "feedback_only"
  ).length;
  const familyTutorialSampleCount = adaptationCaptures.filter((capture) => capture.kind === "family").length;
  const operatorTutorialSampleCount = adaptationCaptures.filter((capture) => capture.kind === "operator").length;

  const familyPrototypes = TUTORIAL_FAMILY_ORDER.reduce<UserShapeProfile["familyPrototypes"]>((accumulator, family) => {
    const prototype = buildFamilyPrototype(
      family,
      adaptationCaptures.filter((capture) => capture.kind === "family" && capture.expectedFamily === family)
    );

    if (prototype) {
      accumulator[family] = prototype;
    }

    return accumulator;
  }, {});
  const operatorPrototypes = TUTORIAL_OPERATOR_ORDER.reduce<UserShapeProfile["operatorPrototypes"]>(
    (accumulator, operator) => {
      const prototype = buildOperatorPrototype(
        operator,
        adaptationCaptures.filter((capture) => capture.kind === "operator" && capture.expectedOperator === operator)
      );

      if (prototype) {
        accumulator[operator] = prototype;
      }

      return accumulator;
    },
    {}
  );

  return {
    tutorialSampleCount: captures.length,
    familyTutorialSampleCount,
    operatorTutorialSampleCount,
    validatedTutorialSampleCount: adaptationCaptures.length,
    feedbackOnlyTutorialSampleCount,
    familyPrototypes,
    operatorPrototypes,
    familyThresholdBias: buildFamilyThresholdBiases(adaptationCaptures),
    operatorThresholdBias: buildOperatorThresholdBiases(adaptationCaptures),
    familyPrototypeReliability: buildFamilyPrototypeReliability(familyPrototypes),
    operatorPrototypeReliability: buildOperatorPrototypeReliability(operatorPrototypes),
    confusionPairs: buildConfusionPairs(familyPrototypes, operatorPrototypes),
    updatedAt: updatedAt > 0 ? updatedAt : Date.now()
  };
}

function buildFamilyPrototype(
  family: GlyphFamily,
  captures: TutorialCapture[]
): FamilyPrototype | undefined {
  const samples = captures
    .map((capture) => deriveTutorialFeatureSample(capture.strokes))
    .filter((sample): sample is TutorialFeatureSample => Boolean(sample));

  if (samples.length === 0) {
    return undefined;
  }

  return {
    family,
    normalizedClouds: samples.slice(-MAX_PROTOTYPE_SAMPLES).map((sample) => clonePointCloud(sample.normalizedCloud)),
    averageFeatures: averageRecognitionFeatures(samples.map((sample) => sample.features)),
    sampleCount: samples.length,
    reliability: roundMetric(resolveCaptureSetReliability(captures))
  };
}

function buildOperatorPrototype(
  operator: OverlayOperator,
  captures: TutorialCapture[]
): OperatorPrototype | undefined {
  const samples = captures
    .map((capture) => deriveOperatorTutorialFeatureSample(capture.strokes))
    .filter((sample): sample is OperatorTutorialFeatureSample => Boolean(sample));

  if (samples.length === 0) {
    return undefined;
  }

  return {
    operator,
    normalizedClouds: samples.slice(-MAX_PROTOTYPE_SAMPLES).map((sample) => clonePointCloud(sample.normalizedCloud)),
    sampleCount: samples.length,
    reliability: roundMetric(resolveCaptureSetReliability(captures)),
    averageAngleRadians: averageScalar(samples.map((sample) => sample.angleRadians)),
    averageScaleRatio: averageOptionalScalar(
      captures.map((capture) => capture.operatorContext?.scaleRatio).filter((value): value is number => value !== undefined)
    ),
    averageAnchorZoneId: mostFrequentAnchorZone(
      captures
        .map((capture) => capture.operatorContext?.anchorZoneId)
        .filter((value): value is NonNullable<TutorialOperatorContext["anchorZoneId"]> => value !== undefined)
    ),
    averageStraightness: averageScalar(samples.map((sample) => sample.straightness)),
    averageCorners: averageScalar(samples.map((sample) => sample.corners)),
    averageClosure: averageScalar(samples.map((sample) => sample.closure)),
    averageStackIndex: averageOptionalScalar(
      captures.map((capture) => capture.operatorContext?.stackIndex).filter((value): value is number => value !== undefined)
    ),
    existingOperatorBiases: buildExistingOperatorBiases(captures)
  };
}

function buildFamilyThresholdBiases(captures: TutorialCapture[]): Partial<Record<GlyphFamily, number>> {
  return TUTORIAL_FAMILY_ORDER.reduce<Partial<Record<GlyphFamily, number>>>((accumulator, family) => {
    const familyCaptures = captures.filter((capture) => capture.kind === "family" && capture.expectedFamily === family);
    const thresholdCaptures = familyCaptures.filter(hasValidatedDecision);
    const weightedCount = resolveWeightedCaptureCount(thresholdCaptures);

    if (thresholdCaptures.length >= 2 && weightedCount > 0) {
      const reliability = resolveCaptureSetReliability(thresholdCaptures);
      accumulator[family] = roundMetric(
        clamp((0.006 + clamp((weightedCount - 1) / 7, 0, 1) * 0.016) * reliability, 0, 0.024)
      );
    }

    return accumulator;
  }, {});
}

function buildOperatorThresholdBiases(captures: TutorialCapture[]): Partial<Record<OverlayOperator, number>> {
  return TUTORIAL_OPERATOR_ORDER.reduce<Partial<Record<OverlayOperator, number>>>((accumulator, operator) => {
    const operatorCaptures = captures.filter(
      (capture) => capture.kind === "operator" && capture.expectedOperator === operator
    );
    const thresholdCaptures = operatorCaptures.filter(hasValidatedDecision);
    const weightedCount = resolveWeightedCaptureCount(thresholdCaptures);

    if (thresholdCaptures.length >= 2 && weightedCount > 0) {
      const reliability = resolveCaptureSetReliability(thresholdCaptures);
      accumulator[operator] = roundMetric(
        clamp((0.005 + clamp((weightedCount - 1) / 5, 0, 1) * 0.015) * reliability, 0, 0.022)
      );
    }

    return accumulator;
  }, {});
}

function buildFamilyPrototypeReliability(
  prototypes: UserShapeProfile["familyPrototypes"]
): Partial<Record<GlyphFamily, number>> {
  return TUTORIAL_FAMILY_ORDER.reduce<Partial<Record<GlyphFamily, number>>>((accumulator, family) => {
    const reliability = prototypes[family]?.reliability;

    if (reliability !== undefined) {
      accumulator[family] = reliability;
    }

    return accumulator;
  }, {});
}

function buildOperatorPrototypeReliability(
  prototypes: UserShapeProfile["operatorPrototypes"]
): Partial<Record<OverlayOperator, number>> {
  return TUTORIAL_OPERATOR_ORDER.reduce<Partial<Record<OverlayOperator, number>>>((accumulator, operator) => {
    const reliability = prototypes[operator]?.reliability;

    if (reliability !== undefined) {
      accumulator[operator] = reliability;
    }

    return accumulator;
  }, {});
}

function isAdaptationCapture(capture: TutorialCapture): boolean {
  return captureAdaptationWeight(capture) > 0;
}

function hasValidatedDecision(capture: TutorialCapture): boolean {
  return capture.validation?.reliability === "high" || capture.validation?.reliability === "medium";
}

function resolveWeightedCaptureCount(captures: TutorialCapture[]): number {
  return captures.reduce((sum, capture) => sum + captureAdaptationWeight(capture), 0);
}

function resolveCaptureSetReliability(captures: TutorialCapture[]): number {
  if (captures.length === 0) {
    return 0;
  }

  const weightedCount = resolveWeightedCaptureCount(captures);
  return clamp(weightedCount / captures.length, 0, 1);
}

function captureAdaptationWeight(capture: TutorialCapture): number {
  const validationWeight = resolveValidationReliabilityWeight(capture.validation);
  const sourceWeight = resolveTutorialSourceWeight(capture.source);
  return validationWeight * sourceWeight;
}

function resolveValidationReliabilityWeight(validation: TutorialCaptureValidation | undefined): number {
  switch (validation?.reliability) {
    case "high":
      return 1;
    case "medium":
      return 0.65;
    case "feedback_only":
      return 0;
    case "unvalidated":
    default:
      return 0.4;
  }
}

function resolveTutorialSourceWeight(source: TutorialCaptureSource): number {
  switch (source) {
    case "variation":
      return 1;
    case "recall":
      return 0.85;
    case "trace":
    default:
      return 0.55;
  }
}

function buildConfusionPairs(
  familyPrototypes: UserShapeProfile["familyPrototypes"],
  operatorPrototypes: UserShapeProfile["operatorPrototypes"]
): UserShapeProfile["confusionPairs"] {
  const pairs = [
    ...prototypeConfusionPairs(
      TUTORIAL_FAMILY_ORDER.map((family) => familyPrototypes[family]).filter(
        (prototype): prototype is FamilyPrototype => Boolean(prototype)
      ),
      (prototype) => prototype.family
    ),
    ...prototypeConfusionPairs(
      TUTORIAL_OPERATOR_ORDER.map((operator) => operatorPrototypes[operator]).filter(
        (prototype): prototype is OperatorPrototype => Boolean(prototype)
      ),
      (prototype) => prototype.operator
    )
  ];

  return pairs.sort((left, right) => right.weight - left.weight);
}

function prototypeConfusionPairs<T extends FamilyPrototype | OperatorPrototype>(
  prototypes: T[],
  label: (prototype: T) => string
): UserShapeProfile["confusionPairs"] {
  const pairs: UserShapeProfile["confusionPairs"] = [];

  for (let leftIndex = 0; leftIndex < prototypes.length; leftIndex += 1) {
    for (let rightIndex = leftIndex + 1; rightIndex < prototypes.length; rightIndex += 1) {
      const left = prototypes[leftIndex];
      const right = prototypes[rightIndex];
      const weight = prototypeSimilarity(left.normalizedClouds, right.normalizedClouds);

      if (weight >= CONFUSION_THRESHOLD) {
        pairs.push({
          left: label(left),
          right: label(right),
          weight: roundMetric(weight)
        });
      }
    }
  }

  return pairs;
}

function prototypeSimilarity(leftClouds: PointSample[][], rightClouds: PointSample[][]): number {
  const leftMean = averagePointCloud(leftClouds);
  const rightMean = averagePointCloud(rightClouds);

  if (leftMean.length === 0 || rightMean.length === 0) {
    return 0;
  }

  const distance = pointCloudDistance(leftMean, rightMean);
  const sampleBalance = clamp(Math.min(leftClouds.length, rightClouds.length) / 4, 0, 1);

  return clamp((1 - distance / 0.72) * (0.55 + sampleBalance * 0.45), 0, 1);
}

function averagePointCloud(clouds: PointSample[][]): PointSample[] {
  if (clouds.length === 0) {
    return [];
  }

  const pointCount = Math.max(...clouds.map((cloud) => cloud.length), 0);

  return Array.from({ length: pointCount }, (_, index) => {
    let x = 0;
    let y = 0;
    let t = 0;
    let pressure = 0;
    let count = 0;

    for (const cloud of clouds) {
      const point = cloud[index];

      if (!point) {
        continue;
      }

      x += point.x;
      y += point.y;
      t += point.t;
      pressure += point.pressure ?? 0;
      count += 1;
    }

    return {
      x: x / Math.max(count, 1),
      y: y / Math.max(count, 1),
      t: t / Math.max(count, 1),
      pressure: count > 0 ? pressure / count : undefined
    };
  });
}

function averageRecognitionFeatures(features: RecognitionFeatures[]): Partial<RecognitionFeatures> {
  if (features.length === 0) {
    return {};
  }

  return RECOGNITION_FEATURE_KEYS.reduce<Partial<RecognitionFeatures>>((accumulator, key) => {
    accumulator[key] =
      features.reduce((sum, feature) => sum + feature[key], 0) / Math.max(features.length, 1);
    return accumulator;
  }, {});
}

function averageScalar(values: number[]): number {
  if (values.length === 0) {
    return 0;
  }

  return values.reduce((sum, value) => sum + value, 0) / values.length;
}

function averageOptionalScalar(values: number[]): number | undefined {
  if (values.length === 0) {
    return undefined;
  }

  return averageScalar(values);
}

function mostFrequentAnchorZone(
  values: Array<NonNullable<TutorialOperatorContext["anchorZoneId"]>>
): TutorialOperatorContext["anchorZoneId"] | undefined {
  if (values.length === 0) {
    return undefined;
  }

  const counts = values.reduce<Partial<Record<NonNullable<TutorialOperatorContext["anchorZoneId"]>, number>>>(
    (accumulator, value) => {
      accumulator[value] = (accumulator[value] ?? 0) + 1;
      return accumulator;
    },
    {}
  );

  return values.reduce((best, value) => {
    if (!best) {
      return value;
    }

    return (counts[value] ?? 0) > (counts[best] ?? 0) ? value : best;
  }, values[0]);
}

function buildExistingOperatorBiases(captures: TutorialCapture[]): Partial<Record<OverlayOperator, number>> | undefined {
  const counts = captures.reduce<Partial<Record<OverlayOperator, number>>>((accumulator, capture) => {
    for (const operator of capture.operatorContext?.existingOperators ?? []) {
      accumulator[operator] = (accumulator[operator] ?? 0) + 1;
    }

    return accumulator;
  }, {});
  const total = Object.values(counts).reduce((sum, count) => sum + (count ?? 0), 0);

  if (total === 0) {
    return undefined;
  }

  return Object.fromEntries(
    Object.entries(counts).map(([operator, count]) => [operator, roundMetric((count ?? 0) / total)])
  ) as Partial<Record<OverlayOperator, number>>;
}

interface TutorialFeatureSample {
  normalizedCloud: PointSample[];
  features: RecognitionFeatures;
}

interface OperatorTutorialFeatureSample {
  normalizedCloud: PointSample[];
  angleRadians: number;
  straightness: number;
  corners: number;
  closure: number;
}

function deriveTutorialFeatureSample(strokes: Stroke[]): TutorialFeatureSample | undefined {
  const validStrokes = strokes.filter((stroke) => stroke.points.length >= 2);

  if (validStrokes.length === 0) {
    return undefined;
  }

  const normalized = normalizeStrokes(validStrokes);
  const dominantStroke = [...validStrokes].sort((left, right) => pathLength(right.points) - pathLength(left.points))[0];
  const dominantPoints = dominantStroke?.points ?? [];
  const radiusSamples = normalized.normalizedCloud.map((point) => Math.hypot(point.x, point.y));
  const meanRadius =
    radiusSamples.reduce((sum, value) => sum + value, 0) / Math.max(radiusSamples.length, 1);
  const variance =
    radiusSamples.reduce((sum, value) => sum + (value - meanRadius) ** 2, 0) / Math.max(radiusSamples.length, 1);

  return {
    normalizedCloud: normalized.normalizedCloud,
    features: {
      strokeCount: validStrokes.length,
      pointCount: validStrokes.reduce((sum, stroke) => sum + stroke.points.length, 0),
      durationMs: getDurationMs(validStrokes),
      pathLength: validStrokes.reduce((sum, stroke) => sum + pathLength(stroke.points), 0),
      closureGap:
        dominantPoints.length >= 2
          ? distanceBetween(dominantPoints[0], dominantPoints[dominantPoints.length - 1])
          : 0,
      dominantCorners: countCorners(dominantPoints, Math.max(normalized.diagonal * 0.02, 3)),
      endpointClusters: clusterEndpointCount(validStrokes, Math.max(normalized.diagonal * 0.08, 14)),
      circularity: clamp(1 - Math.sqrt(variance) / Math.max(meanRadius, 0.0001) / 0.45, 0, 1),
      fillRatio: calculateFillRatio(dominantPoints),
      parallelism: calculateParallelism(validStrokes),
      rawAngleRadians: normalizeAngleHalfPi(normalized.rawAngleRadians)
    }
  };
}

function deriveOperatorTutorialFeatureSample(strokes: Stroke[]): OperatorTutorialFeatureSample | undefined {
  const validStrokes = strokes.filter((stroke) => stroke.points.length >= 2);
  const dominantStroke = [...validStrokes].sort((left, right) => pathLength(right.points) - pathLength(left.points))[0];

  if (!dominantStroke) {
    return undefined;
  }

  const normalized = normalizeStrokes([dominantStroke], 64);
  const bounds = boundingBox(dominantStroke.points);
  const diagonal = Math.max(Math.hypot(bounds.width, bounds.height), 1);
  const simplified = rdpSimplify(dominantStroke.points, Math.max(diagonal * 0.08, 4));
  const firstPoint = dominantStroke.points[0];
  const lastPoint = dominantStroke.points[dominantStroke.points.length - 1];

  return {
    normalizedCloud: normalized.normalizedCloud,
    angleRadians: normalizeAngleHalfPi(lineAngle(dominantStroke)),
    straightness: strokeStraightness(dominantStroke),
    corners: Math.max(simplified.length - 1, 0),
    closure: clamp(
      1 - Math.hypot(firstPoint.x - lastPoint.x, firstPoint.y - lastPoint.y) / (diagonal * 0.35),
      0,
      1
    )
  };
}

function calculateParallelism(strokes: Stroke[]): number {
  const linearStrokes = strokes
    .filter((stroke) => stroke.points.length >= 2)
    .map((stroke) => ({
      straightness: strokeStraightness(stroke),
      angle: lineAngle(stroke)
    }));

  if (linearStrokes.length === 0) {
    return 0;
  }

  const vector = linearStrokes.reduce(
    (accumulator, item) => ({
      x: accumulator.x + Math.cos(item.angle * 2),
      y: accumulator.y + Math.sin(item.angle * 2),
      straightness: accumulator.straightness + item.straightness
    }),
    { x: 0, y: 0, straightness: 0 }
  );
  const averageAngle = Math.atan2(vector.y, vector.x) / 2;
  const meanDeviation =
    linearStrokes.reduce((sum, item) => sum + Math.abs(normalizeAngleHalfPi(item.angle - averageAngle)), 0) /
    linearStrokes.length;
  const angleScore = clamp(1 - meanDeviation / (Math.PI / 6), 0, 1);
  const straightnessScore = clamp(vector.straightness / linearStrokes.length, 0, 1);

  return angleScore * 0.6 + straightnessScore * 0.4;
}

function calculateFillRatio(points: PointSample[]): number {
  if (points.length < 3) {
    return 0;
  }

  const simplified = rdpSimplify(points, 6);
  const area = Math.abs(polygonArea(simplified));
  const box = boundingBox(points);
  const boxArea = Math.max(box.width * box.height, 1);

  return clamp(area / boxArea, 0, 1);
}

function getDurationMs(strokes: Stroke[]): number {
  const timestamps = strokes.flatMap((stroke) => stroke.points.map((point) => point.t));

  if (timestamps.length === 0) {
    return 0;
  }

  return Math.max(...timestamps) - Math.min(...timestamps);
}

function countCorners(points: PointSample[], epsilon: number): number {
  if (points.length < 2) {
    return 0;
  }

  const simplified = rdpSimplify(points, epsilon);
  return Math.max(simplified.length - 1, 0);
}

function polygonArea(points: PointSample[]): number {
  if (points.length < 3) {
    return 0;
  }

  let sum = 0;

  for (let index = 0; index < points.length; index += 1) {
    const current = points[index];
    const next = points[(index + 1) % points.length];
    sum += current.x * next.y - next.x * current.y;
  }

  return sum / 2;
}

function captureSourceCoverage(captures: TutorialCapture[]): number {
  if (captures.length === 0) {
    return 0;
  }

  const kinds = new Set(captures.map((capture) => capture.source));
  return kinds.size / 3;
}

function latestCaptureTimestamp(captures: Array<Pick<TutorialCapture, "timestamp">>): number {
  if (captures.length === 0) {
    return Date.now();
  }

  return captures.reduce((latest, capture) => Math.max(latest, capture.timestamp), 0);
}

function normalizeCaptureSource(source: TutorialCaptureInput["source"]): TutorialCaptureSource {
  if (source === "variant") {
    return "variation";
  }

  if (source === "trace" || source === "recall" || source === "variation") {
    return source;
  }

  return "trace";
}

function coerceTutorialCapture(raw: Partial<TutorialCapture> | undefined): TutorialCapture | undefined {
  if (!raw || (raw.kind !== "family" && raw.kind !== "operator")) {
    return undefined;
  }

  const strokes = Array.isArray(raw.strokes) ? cloneStrokes(raw.strokes) : [];
  const source = normalizeCaptureSource(raw.source as TutorialCaptureInput["source"]);
  const timestamp = typeof raw.timestamp === "number" && Number.isFinite(raw.timestamp) ? raw.timestamp : Date.now();
  const id = typeof raw.id === "string" && raw.id.length > 0 ? raw.id : crypto.randomUUID();

  if (raw.kind === "family") {
    if (!isGlyphFamily(raw.expectedFamily)) {
      return undefined;
    }

    return {
      id,
      kind: "family",
      expectedFamily: raw.expectedFamily,
      strokes,
      source,
      timestamp,
      baseSnapshot: cloneBaseSnapshot(raw.baseSnapshot),
      operatorContext: cloneOperatorContext(raw.operatorContext),
      validation: cloneTutorialCaptureValidation(raw.validation)
    };
  }

  if (!isOverlayOperator(raw.expectedOperator)) {
    return undefined;
  }

  return {
    id,
    kind: "operator",
    expectedOperator: raw.expectedOperator,
    strokes,
    source,
    timestamp,
    baseSnapshot: cloneBaseSnapshot(raw.baseSnapshot),
    operatorContext: cloneOperatorContext(raw.operatorContext),
    validation: cloneTutorialCaptureValidation(raw.validation)
  };
}

function upsertCapture(existing: TutorialCapture[], nextCapture: TutorialCapture): TutorialCapture[] {
  const next = existing
    .filter((capture) => capture.id !== nextCapture.id)
    .map((capture) => ({
      ...capture,
      strokes: cloneStrokes(capture.strokes),
      baseSnapshot: cloneBaseSnapshot(capture.baseSnapshot),
      operatorContext: cloneOperatorContext(capture.operatorContext),
      validation: cloneTutorialCaptureValidation(capture.validation)
    }));

  next.push({
    ...nextCapture,
    strokes: cloneStrokes(nextCapture.strokes),
    baseSnapshot: cloneBaseSnapshot(nextCapture.baseSnapshot),
    operatorContext: cloneOperatorContext(nextCapture.operatorContext),
    validation: cloneTutorialCaptureValidation(nextCapture.validation)
  });

  return next.sort((left, right) => left.timestamp - right.timestamp);
}

function cloneStrokes(strokes: Stroke[]): Stroke[] {
  return strokes.map((stroke) => ({
    ...stroke,
    points: stroke.points.map((point) => ({ ...point }))
  }));
}

function clonePointCloud(points: PointSample[]): PointSample[] {
  return points.map((point) => ({ ...point }));
}

function cloneBaseSnapshot(snapshot: TutorialBaseSnapshot | undefined): TutorialBaseSnapshot | undefined {
  if (
    !snapshot ||
    !Number.isFinite(snapshot.centroid?.x) ||
    !Number.isFinite(snapshot.centroid?.y) ||
    !Number.isFinite(snapshot.diagonal) ||
    !Number.isFinite(snapshot.axisAngleRadians)
  ) {
    return undefined;
  }

  return {
    centroid: { ...snapshot.centroid },
    bounds: cloneBounds(snapshot.bounds),
    diagonal: snapshot.diagonal,
    axisAngleRadians: snapshot.axisAngleRadians
  };
}

function cloneOperatorContext(context: TutorialOperatorContext | undefined): TutorialOperatorContext | undefined {
  if (!context || !Number.isFinite(context.stackIndex) || !context.strokeBounds) {
    return undefined;
  }

  return {
    stackIndex: context.stackIndex,
    existingOperators: Array.isArray(context.existingOperators) ? [...context.existingOperators] : [],
    strokeBounds: cloneBounds(context.strokeBounds),
    anchorZoneId: context.anchorZoneId,
    anchorScore: context.anchorScore,
    scaleRatio: context.scaleRatio,
    angleRadians: context.angleRadians
  };
}

function cloneTutorialCaptureValidation(
  validation: TutorialCaptureValidation | undefined
): TutorialCaptureValidation | undefined {
  if (!validation || !isTutorialCaptureReliability(validation.reliability)) {
    return undefined;
  }

  return {
    reliability: validation.reliability,
    expectedLabel: typeof validation.expectedLabel === "string" ? validation.expectedLabel : "",
    actualTopLabel: typeof validation.actualTopLabel === "string" ? validation.actualTopLabel : undefined,
    status: isRecognitionStatus(validation.status) ? validation.status : undefined,
    topScore: finiteOrUndefined(validation.topScore),
    margin: finiteOrUndefined(validation.margin),
    quality: validation.quality
      ? {
          closure: finiteOrFallback(validation.quality.closure, 0),
          symmetry: finiteOrFallback(validation.quality.symmetry, 0),
          smoothness: finiteOrFallback(validation.quality.smoothness, 0),
          tempo: finiteOrFallback(validation.quality.tempo, 0),
          overshoot: finiteOrFallback(validation.quality.overshoot, 0),
          stability: finiteOrFallback(validation.quality.stability, 0),
          rotationBias: finiteOrFallback(validation.quality.rotationBias, 0)
        }
      : undefined,
    anchorScore: finiteOrUndefined(validation.anchorScore),
    scaleScore: finiteOrUndefined(validation.scaleScore),
    shapeConfidence: finiteOrUndefined(validation.shapeConfidence),
    blockedBy: isOverlayOperator(validation.blockedBy) ? validation.blockedBy : undefined
  };
}

function cloneBounds(bounds: StrokeBounds): StrokeBounds {
  return {
    minX: Number.isFinite(bounds.minX) ? bounds.minX : 0,
    maxX: Number.isFinite(bounds.maxX) ? bounds.maxX : 0,
    minY: Number.isFinite(bounds.minY) ? bounds.minY : 0,
    maxY: Number.isFinite(bounds.maxY) ? bounds.maxY : 0,
    width: Number.isFinite(bounds.width) ? bounds.width : 0,
    height: Number.isFinite(bounds.height) ? bounds.height : 0
  };
}

function isGlyphFamily(value: unknown): value is GlyphFamily {
  return TUTORIAL_FAMILY_ORDER.includes(value as GlyphFamily);
}

function isOverlayOperator(value: unknown): value is OverlayOperator {
  return TUTORIAL_OPERATOR_ORDER.includes(value as OverlayOperator);
}

function isTutorialCaptureReliability(value: unknown): value is TutorialCaptureValidation["reliability"] {
  return value === "unvalidated" || value === "high" || value === "medium" || value === "feedback_only";
}

function isRecognitionStatus(value: unknown): value is TutorialCaptureValidation["status"] {
  return value === "recognized" || value === "ambiguous" || value === "incomplete" || value === "invalid";
}

function distanceBetween(a: { x: number; y: number }, b: { x: number; y: number }): number {
  return Math.hypot(a.x - b.x, a.y - b.y);
}

function clamp(value: number, minimum: number, maximum: number): number {
  return Math.max(minimum, Math.min(maximum, value));
}

function roundMetric(value: number): number {
  return Number(value.toFixed(4));
}

function finiteOrUndefined(value: number | undefined): number | undefined {
  return value !== undefined && Number.isFinite(value) ? value : undefined;
}

function finiteOrFallback(value: number | undefined, fallback: number): number {
  return value !== undefined && Number.isFinite(value) ? value : fallback;
}
