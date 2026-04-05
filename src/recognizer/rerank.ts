import { pointCloudDistance } from "./geometry";
import type {
  GlyphFamily,
  OverlayAnchorZoneId,
  OverlayOperator,
  PointSample,
  RecognitionCandidate,
  RecognitionFeatures,
  UserInputProfile
} from "./types";

interface FamilyPrototypeLike {
  family?: GlyphFamily;
  normalizedClouds?: PointSample[][];
  averageFeatures?: Partial<RecognitionFeatures>;
  sampleCount?: number;
}

interface ConfusionPairLike {
  left: string;
  right: string;
  weight: number;
}

interface RecognitionCalibrationLike {
  userPrototypeWeight?: number;
  rerankStrength?: number;
  confidenceBias?: number;
}

export interface OverlayRerankCandidate {
  operator: OverlayOperator;
  score: number;
  baseScore: number;
  templateDistance: number;
  shapeConfidence: number;
  notes: string[];
  anchorZoneId?: OverlayAnchorZoneId;
  placementAnchorZoneId?: OverlayAnchorZoneId;
  anchorScore: number;
  scaleScore: number;
  angleRadians: number;
  scaleRatio: number;
  straightness: number;
  corners: number;
  closure: number;
  stackIndex?: number;
  existingOperators?: OverlayOperator[];
  blockedBy?: OverlayOperator;
  completenessHint?: string;
}

export interface OverlayOperatorPrototypeSummary {
  operator?: OverlayOperator;
  sampleCount?: number;
  averageAngleRadians?: number;
  averageScaleRatio?: number;
  averageAnchorZoneId?: OverlayAnchorZoneId;
  averageStraightness?: number;
  averageCorners?: number;
  averageClosure?: number;
  averageShapeConfidence?: number;
  averageStackIndex?: number;
  existingOperatorBiases?: Partial<Record<OverlayOperator, number>>;
}

export interface OverlayOperatorConfusionBias {
  left: OverlayOperator;
  right: OverlayOperator;
  preferred: OverlayOperator;
  weight: number;
}

export interface OverlayPersonalizationProfile {
  sampleCount?: number;
  userPrototypeWeight?: number;
  rerankStrength?: number;
  confidenceBias?: number;
  operatorPrototypes?: Partial<Record<OverlayOperator, OverlayOperatorPrototypeSummary>>;
  confusionPairs?: ConfusionPairLike[];
  confusionBiases?: OverlayOperatorConfusionBias[];
  recognitionCalibration?: RecognitionCalibrationLike;
  calibration?: RecognitionCalibrationLike;
}

interface ShapeProfileLike {
  tutorialSampleCount?: number;
  familyPrototypes?: Partial<Record<GlyphFamily, FamilyPrototypeLike>>;
  confusionPairs?: ConfusionPairLike[];
  recognitionCalibration?: RecognitionCalibrationLike;
  calibration?: RecognitionCalibrationLike;
}

type PersonalizationProfileLike = UserInputProfile &
  ShapeProfileLike & {
    tutorialProfile?: ShapeProfileLike;
    shapeProfile?: ShapeProfileLike;
    recognitionCalibration?: RecognitionCalibrationLike;
    calibration?: RecognitionCalibrationLike;
  };

const TOP_K_WINDOW = 0.18;
const MAX_TOP_K = 3;
const MIN_TUTORIAL_SAMPLES = 3;
const MIN_OVERLAY_PERSONALIZED_SAMPLES = 3;
const FULL_OVERLAY_PERSONALIZED_SAMPLES = 12;
const DEFAULT_OVERLAY_RERANK_STRENGTH = 0.18;
const DEFAULT_OVERLAY_CONFIDENCE_BIAS = 0.08;
const MAX_OVERLAY_SCORE_SHIFT = 0.08;
const MAX_OVERLAY_CONFIDENCE_SHIFT = 0.1;

export function rerankBaseCandidates(params: {
  candidates: RecognitionCandidate[];
  normalizedCloud: PointSample[];
  features: RecognitionFeatures;
  profile?: UserInputProfile;
}): RecognitionCandidate[] {
  const { candidates, normalizedCloud, features, profile } = params;

  if (candidates.length < 2) {
    return candidates;
  }

  const shapeProfile = resolveShapeProfile(profile);
  const tutorialSampleCount = Math.max(
    shapeProfile?.tutorialSampleCount ?? 0,
    (profile as PersonalizationProfileLike | undefined)?.sampleCount ?? 0
  );

  if (tutorialSampleCount < MIN_TUTORIAL_SAMPLES) {
    return candidates;
  }

  const calibration = resolveCalibration(profile, shapeProfile, tutorialSampleCount);
  const topScore = candidates[0]?.score ?? 0;
  const rerankableFamilies = candidates
    .filter((candidate, index) => index < MAX_TOP_K && topScore - candidate.score <= TOP_K_WINDOW)
    .map((candidate) => candidate.family);

  if (rerankableFamilies.length < 2) {
    return candidates;
  }

  const personalizationStrength = clamp((tutorialSampleCount - MIN_TUTORIAL_SAMPLES) / 9, 0, 1);
  const reranked = candidates.map((candidate, index) => {
    if (index >= MAX_TOP_K || topScore - candidate.score > TOP_K_WINDOW) {
      return candidate;
    }

    const prototypeSignal = resolvePrototypeSignal(
      shapeProfile?.familyPrototypes?.[candidate.family],
      normalizedCloud,
      features
    );
    const confusionBias = resolveConfusionBias(candidate.family, rerankableFamilies, shapeProfile?.confusionPairs);
    const proximity = clamp(1 - (topScore - candidate.score) / TOP_K_WINDOW, 0, 1);
    const prototypeDelta =
      (prototypeSignal - 0.5) * calibration.userPrototypeWeight * personalizationStrength * proximity;
    const rerankDelta = confusionBias * calibration.rerankStrength * personalizationStrength * proximity;
    const confidenceDelta =
      Math.max(prototypeSignal - 0.55, 0) *
      calibration.confidenceBias *
      personalizationStrength *
      (0.4 + proximity * 0.6);
    const rerankedScore = clamp(candidate.score + prototypeDelta + rerankDelta + confidenceDelta, 0, 1);

    return {
      ...candidate,
      score: rerankedScore,
      notes: [
        ...candidate.notes,
        `global=${candidate.score.toFixed(2)}`,
        `prototype=${prototypeSignal.toFixed(2)}`,
        `pair=${confusionBias.toFixed(2)}`,
        `calibrated=${rerankedScore.toFixed(2)}`
      ]
    };
  });

  return reranked.sort((left, right) => right.score - left.score);
}

export function rerankOverlayCandidates<T extends OverlayRerankCandidate>(
  candidates: T[],
  options?: { profile?: OverlayPersonalizationProfile; topK?: number }
): T[] {
  const sorted = [...candidates].sort((left, right) => right.score - left.score);

  if (sorted.length <= 1) {
    return sorted;
  }

  const topK = Math.max(1, Math.min(options?.topK ?? 3, sorted.length));
  const profile = options?.profile;
  const personalizationMix = resolveOverlayPersonalizationMix(profile?.sampleCount ?? 0);
  const rerankStrength = resolveOverlayCalibration(profile).rerankStrength * personalizationMix;
  const confidenceBias = resolveOverlayCalibration(profile).confidenceBias * personalizationMix;
  const activePair = resolveActiveOverlayPair(sorted.slice(0, topK));

  return sorted
    .map((candidate, index) => {
      if (index >= topK) {
        return candidate;
      }

      const prototype = profile?.operatorPrototypes?.[candidate.operator];
      const gate = resolveOverlayRuleGate(candidate.anchorScore, candidate.scaleScore);
      const shapeSimilarity = prototype ? computeOverlayShapeSimilarity(candidate, prototype) : 0.5;
      const placementSimilarity = prototype ? computeOverlayPlacementSimilarity(candidate, prototype) : 0.5;
      const pairShift = computeOverlayHardPairShift(candidate, activePair, profile) * gate;
      const shapeShift = Math.max(shapeSimilarity - 0.5, 0) * rerankStrength * 0.34 * gate;
      const placementShift = (placementSimilarity - 0.5) * rerankStrength * 0.2 * gate;
      const nextScore = clamp(
        candidate.baseScore + clamp(shapeShift + placementShift + pairShift, -MAX_OVERLAY_SCORE_SHIFT, MAX_OVERLAY_SCORE_SHIFT),
        0,
        1
      );
      const confidenceTarget = prototype
        ? Math.max(
            candidate.shapeConfidence,
            mixOverlayScalar(candidate.shapeConfidence, shapeSimilarity, confidenceBias * gate)
          )
        : candidate.shapeConfidence;
      const confidenceShift = clamp(
        confidenceTarget - candidate.shapeConfidence + pairShift * 0.5,
        -MAX_OVERLAY_CONFIDENCE_SHIFT,
        MAX_OVERLAY_CONFIDENCE_SHIFT
      );
      const nextShapeConfidence = clamp(candidate.shapeConfidence + confidenceShift, 0, 1);

      return {
        ...candidate,
        score: nextScore,
        shapeConfidence: nextShapeConfidence,
        notes: [
          ...candidate.notes,
          `rerank=${(nextScore - candidate.baseScore).toFixed(2)}`,
          `placement=${placementSimilarity.toFixed(2)}`,
          `shape_conf=${nextShapeConfidence.toFixed(2)}`
        ]
      };
    })
    .sort((left, right) => right.score - left.score);
}

function resolveActiveOverlayPair(
  candidates: OverlayRerankCandidate[]
): [OverlayOperator, OverlayOperator] | undefined {
  const topCandidates = candidates.slice(0, 2);
  const topOperators = topCandidates.map((candidate) => candidate.operator);

  if (
    topOperators.length < 2 ||
    topCandidates.some((candidate) => candidate.anchorScore < 0.3 || candidate.scaleScore < 0.3)
  ) {
    return undefined;
  }

  const normalized = [...topOperators].sort();
  return normalized[0] === "electric_fork" && normalized[1] === "void_cut"
    ? ([normalized[0], normalized[1]] as [OverlayOperator, OverlayOperator])
    : undefined;
}

function resolveOverlayCalibration(profile?: OverlayPersonalizationProfile): Required<RecognitionCalibrationLike> {
  const fallbackStrength = clamp(
    ((profile?.sampleCount ?? 0) - MIN_OVERLAY_PERSONALIZED_SAMPLES) /
      Math.max(FULL_OVERLAY_PERSONALIZED_SAMPLES - MIN_OVERLAY_PERSONALIZED_SAMPLES, 1),
    0,
    1
  );
  const calibrationSource = profile?.recognitionCalibration ?? profile?.calibration;

  return {
    userPrototypeWeight: clamp(
      calibrationSource?.userPrototypeWeight ?? (0.08 + fallbackStrength * 0.06),
      0,
      0.16
    ),
    rerankStrength: clamp(
      calibrationSource?.rerankStrength ?? profile?.rerankStrength ?? DEFAULT_OVERLAY_RERANK_STRENGTH,
      0,
      0.24
    ),
    confidenceBias: clamp(
      calibrationSource?.confidenceBias ?? profile?.confidenceBias ?? DEFAULT_OVERLAY_CONFIDENCE_BIAS,
      0,
      0.16
    )
  };
}

function computeOverlayShapeSimilarity(
  candidate: OverlayRerankCandidate,
  prototype: OverlayOperatorPrototypeSummary
): number {
  const samples: number[] = [];

  if (prototype.averageAngleRadians !== undefined) {
    samples.push(closeness(Math.abs(candidate.angleRadians), Math.abs(prototype.averageAngleRadians), Math.PI / 7));
  }
  if (prototype.averageScaleRatio !== undefined) {
    samples.push(closeness(candidate.scaleRatio, prototype.averageScaleRatio, 0.16));
  }
  if (prototype.averageAnchorZoneId !== undefined) {
    samples.push(candidate.anchorZoneId === prototype.averageAnchorZoneId ? 1 : 0.35);
  }
  if (prototype.averageStraightness !== undefined) {
    samples.push(closeness(candidate.straightness, prototype.averageStraightness, 0.22));
  }
  if (prototype.averageCorners !== undefined) {
    samples.push(closeness(candidate.corners, prototype.averageCorners, 1.4));
  }
  if (prototype.averageClosure !== undefined) {
    samples.push(closeness(candidate.closure, prototype.averageClosure, 0.3));
  }
  if (prototype.averageShapeConfidence !== undefined) {
    samples.push(closeness(candidate.shapeConfidence, prototype.averageShapeConfidence, 0.24));
  }

  if (samples.length === 0) {
    return 0.5;
  }

  return samples.reduce((sum, value) => sum + value, 0) / samples.length;
}

function computeOverlayPlacementSimilarity(
  candidate: OverlayRerankCandidate,
  prototype: OverlayOperatorPrototypeSummary
): number {
  const samples: number[] = [];

  if (prototype.averageAnchorZoneId !== undefined) {
    samples.push(candidate.placementAnchorZoneId === prototype.averageAnchorZoneId ? 1 : 0.22);
  }
  if (prototype.averageScaleRatio !== undefined) {
    samples.push(closeness(candidate.scaleRatio, prototype.averageScaleRatio, 0.16));
  }
  if (prototype.averageStackIndex !== undefined && candidate.stackIndex !== undefined) {
    samples.push(closeness(candidate.stackIndex, prototype.averageStackIndex, 1.4));
  }
  if (prototype.existingOperatorBiases !== undefined) {
    samples.push(resolveExistingOperatorContextSimilarity(candidate.existingOperators, prototype.existingOperatorBiases));
  }

  if (samples.length === 0) {
    return 0.5;
  }

  return samples.reduce((sum, value) => sum + value, 0) / samples.length;
}

function computeOverlayHardPairShift(
  candidate: OverlayRerankCandidate,
  activePair: [OverlayOperator, OverlayOperator] | undefined,
  profile: OverlayPersonalizationProfile | undefined
): number {
  if (!activePair || !activePair.includes(candidate.operator)) {
    return 0;
  }

  const signal = candidate.operator === "void_cut" ? overlayVoidCutSignal(candidate) : overlayElectricForkSignal(candidate);
  const defaultShift = (signal - 0.5) * 0.05;
  const explicitBias = resolveOverlayExplicitBias(candidate.operator, activePair, profile);

  return defaultShift + explicitBias;
}

function resolveExistingOperatorContextSimilarity(
  existingOperators: OverlayOperator[] | undefined,
  biases: Partial<Record<OverlayOperator, number>>
): number {
  const entries = Object.entries(biases).filter((entry): entry is [OverlayOperator, number] => typeof entry[1] === "number");

  if (entries.length === 0) {
    return 0.5;
  }

  if (!existingOperators || existingOperators.length === 0) {
    return 0.35;
  }

  const weights = existingOperators.map((operator) => biases[operator] ?? 0);
  const peak = Math.max(...weights, 0);
  const average = weights.reduce((sum, value) => sum + value, 0) / Math.max(weights.length, 1);

  return clamp(peak * 0.75 + average * 0.25, 0.2, 1);
}

function resolveOverlayExplicitBias(
  operator: OverlayOperator,
  activePair: [OverlayOperator, OverlayOperator],
  profile: OverlayPersonalizationProfile | undefined
): number {
  const bias = profile?.confusionBiases?.find((item) => {
    const normalized = [item.left, item.right].sort();
    return normalized[0] === activePair[0] && normalized[1] === activePair[1];
  });

  if (!bias) {
    return 0;
  }

  const direction = bias.preferred === operator ? 1 : -1;
  return clamp(bias.weight, 0, 1) * 0.025 * direction;
}

function overlayVoidCutSignal(candidate: OverlayRerankCandidate): number {
  const diagonalScore = closeness(Math.abs(candidate.angleRadians), Math.PI / 4, Math.PI / 8);
  const lowCornerScore = closeness(candidate.corners, 1.8, 1.6);

  return clamp(
    candidate.shapeConfidence * 0.24 +
      candidate.straightness * 0.3 +
      diagonalScore * 0.3 +
      lowCornerScore * 0.16,
    0,
    1
  );
}

function overlayElectricForkSignal(candidate: OverlayRerankCandidate): number {
  const forkCornerScore = closeness(candidate.corners, 4, 1.6);
  const diagonalPenalty = closeness(Math.abs(candidate.angleRadians), Math.PI / 4, Math.PI / 8);

  return clamp(
    candidate.shapeConfidence * 0.22 +
      forkCornerScore * 0.34 +
      (1 - candidate.straightness) * 0.2 +
      (1 - diagonalPenalty) * 0.08 +
      (1 - candidate.closure) * 0.16,
    0,
    1
  );
}

function resolveOverlayPersonalizationMix(sampleCount: number): number {
  return clamp(
    (sampleCount - MIN_OVERLAY_PERSONALIZED_SAMPLES) /
      Math.max(FULL_OVERLAY_PERSONALIZED_SAMPLES - MIN_OVERLAY_PERSONALIZED_SAMPLES, 1),
    0,
    1
  );
}

function resolveOverlayRuleGate(anchorScore: number, scaleScore: number): number {
  return clamp(Math.min(anchorScore, scaleScore) * 1.1, 0, 1);
}

function mixOverlayScalar(left: number, right: number, amount: number): number {
  const clampedAmount = clamp(amount, 0, 1);
  return left * (1 - clampedAmount) + right * clampedAmount;
}

function resolveShapeProfile(profile?: UserInputProfile): ShapeProfileLike | undefined {
  const profileLike = profile as PersonalizationProfileLike | undefined;
  return profileLike?.tutorialProfile ?? profileLike?.shapeProfile ?? profileLike;
}

function resolveCalibration(
  profile: UserInputProfile | undefined,
  shapeProfile: ShapeProfileLike | undefined,
  tutorialSampleCount: number
): Required<RecognitionCalibrationLike> {
  const fallbackStrength = clamp((tutorialSampleCount - MIN_TUTORIAL_SAMPLES) / 12, 0, 1);
  const calibrationSource =
    shapeProfile?.recognitionCalibration ??
    shapeProfile?.calibration ??
    (profile as PersonalizationProfileLike | undefined)?.recognitionCalibration ??
    (profile as PersonalizationProfileLike | undefined)?.calibration;

  return {
    userPrototypeWeight: clamp(calibrationSource?.userPrototypeWeight ?? (0.08 + fallbackStrength * 0.06), 0, 0.16),
    rerankStrength: clamp(calibrationSource?.rerankStrength ?? (0.08 + fallbackStrength * 0.06), 0, 0.16),
    confidenceBias: clamp(calibrationSource?.confidenceBias ?? (0.01 + fallbackStrength * 0.025), 0, 0.04)
  };
}

function resolvePrototypeSignal(
  prototype: FamilyPrototypeLike | undefined,
  normalizedCloud: PointSample[],
  features: RecognitionFeatures
): number {
  if (!prototype) {
    return 0.5;
  }

  const sampleCount = Math.max(prototype.sampleCount ?? 0, prototype.normalizedClouds?.length ?? 0);
  const reliability = clamp(sampleCount / 4, 0, 1);
  const cloudSimilarity = resolveCloudSimilarity(normalizedCloud, prototype.normalizedClouds);
  const featureSimilarity = resolveFeatureSimilarity(features, prototype.averageFeatures);
  const combinedSimilarity =
    cloudSimilarity === undefined && featureSimilarity === undefined
      ? 0.5
      : cloudSimilarity === undefined
        ? featureSimilarity ?? 0.5
        : featureSimilarity === undefined
          ? cloudSimilarity
          : cloudSimilarity * 0.7 + featureSimilarity * 0.3;

  return 0.5 + (combinedSimilarity - 0.5) * reliability;
}

function resolveCloudSimilarity(currentCloud: PointSample[], prototypeClouds?: PointSample[][]): number | undefined {
  if (!prototypeClouds || prototypeClouds.length === 0) {
    return undefined;
  }

  const similarities = prototypeClouds
    .map((cloud) => clamp(1 - pointCloudDistance(currentCloud, cloud) / 0.62, 0, 1))
    .sort((left, right) => right - left)
    .slice(0, Math.min(2, prototypeClouds.length));

  return similarities.reduce((sum, value) => sum + value, 0) / similarities.length;
}

function resolveFeatureSimilarity(
  features: RecognitionFeatures,
  averageFeatures?: Partial<RecognitionFeatures>
): number | undefined {
  if (!averageFeatures) {
    return undefined;
  }

  const comparisons: number[] = [];
  const featureComparators: Array<keyof RecognitionFeatures> = [
    "strokeCount",
    "dominantCorners",
    "endpointClusters",
    "circularity",
    "fillRatio",
    "parallelism",
    "rawAngleRadians"
  ];

  for (const key of featureComparators) {
    const expected = averageFeatures[key];

    if (typeof expected !== "number" || !Number.isFinite(expected)) {
      continue;
    }

    const actual = features[key];
    comparisons.push(closeness(actual, expected, featureTolerance(key)));
  }

  if (comparisons.length === 0) {
    return undefined;
  }

  return comparisons.reduce((sum, value) => sum + value, 0) / comparisons.length;
}

function resolveConfusionBias(
  family: GlyphFamily,
  competingFamilies: GlyphFamily[],
  confusionPairs?: ConfusionPairLike[]
): number {
  if (!confusionPairs || confusionPairs.length === 0) {
    return 0;
  }

  let bias = 0;

  for (const pair of confusionPairs) {
    if (!competingFamilies.includes(pair.left as GlyphFamily) || !competingFamilies.includes(pair.right as GlyphFamily)) {
      continue;
    }

    if (pair.left === family) {
      bias += pair.weight;
    } else if (pair.right === family) {
      bias -= pair.weight;
    }
  }

  return clamp(bias, -0.2, 0.2);
}

function closeness(actual: number, expected: number, tolerance: number): number {
  return clamp(1 - Math.abs(actual - expected) / Math.max(tolerance, 0.0001), 0, 1);
}

function featureTolerance(feature: keyof RecognitionFeatures): number {
  switch (feature) {
    case "strokeCount":
      return 2;
    case "dominantCorners":
    case "endpointClusters":
      return 3;
    case "rawAngleRadians":
      return Math.PI / 4;
    case "circularity":
    case "fillRatio":
    case "parallelism":
      return 0.35;
    default:
      return 1;
  }
}

function clamp(value: number, minimum: number, maximum: number): number {
  return Math.max(minimum, Math.min(maximum, value));
}
