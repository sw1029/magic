import { pointCloudDistance } from "./geometry";
import type {
  GlyphFamily,
  OverlayAnchorZoneId,
  OverlayOperator,
  PersonalizationRuntimeSummary,
  PointSample,
  QualityVector,
  RecognitionCandidate,
  RecognitionFeatures,
  RecognitionStatus,
  ShadowRuntimeSummary,
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
  tutorialSampleCount?: number;
  operatorTutorialSampleCount?: number;
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
  familyTutorialSampleCount?: number;
  operatorTutorialSampleCount?: number;
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
const BASE_PERSONALIZATION_WEAK_START = 6;
const BASE_PERSONALIZATION_STRONG_START = 12;
const BASE_PERSONALIZATION_FULLY_ON = 24;
const OPERATOR_PERSONALIZATION_WEAK_START = 4;
const OPERATOR_PERSONALIZATION_STRONG_START = 8;
const OPERATOR_PERSONALIZATION_FULLY_ON = 16;
const DEFAULT_OVERLAY_RERANK_STRENGTH = 0.18;
const DEFAULT_OVERLAY_CONFIDENCE_BIAS = 0.08;
const MAX_OVERLAY_SCORE_SHIFT = 0.08;
const MAX_OVERLAY_CONFIDENCE_SHIFT = 0.1;
const MAX_OPERATOR_SUPPRESSION_SCALE = 0.06;
const WEAK_OPERATOR_GATE_THRESHOLD = 0.34;

const RAW_ML_ARTIFACTS = import.meta.glob("../../artifacts/ml/*.json", {
  query: "?raw",
  import: "default",
  eager: true
}) as Record<string, string>;

interface TinyMlLogisticModel {
  intercept?: number | number[];
  coefficients?: number[];
  weights?: number[];
  featureNames?: string[];
  expandedFeatureNames?: string[];
}

interface TinyMlTreeModel {
  childrenLeft: number[];
  childrenRight: number[];
  feature?: number[];
  featureIndex?: number[];
  threshold: number[];
  value: number[];
}

interface TinyMlGradientBoostingClassifier {
  initProbability: number;
  learningRate: number;
  treeParams: {
    estimators: TinyMlTreeModel[];
  };
}

interface TinyMlGradientBoostingRegressor {
  initialPrediction: number;
  learningRate: number;
  trees: TinyMlTreeModel[];
}

interface BaseRerankArtifact {
  version?: string;
  featureOrder?: string[];
  featureNormalization?: Record<string, Record<string, unknown>>;
  supportedPairs?: string[];
  models?: {
    main?: {
      model?: TinyMlGradientBoostingClassifier;
      deltaTransform?: Record<string, number>;
    };
  };
}

interface BaseConfidenceArtifact {
  version?: string;
  featureOrder?: string[];
  featureNormalization?: Record<string, Record<string, unknown>>;
  models?: {
    main?: {
      model?: TinyMlGradientBoostingClassifier;
    };
  };
}

interface OperatorRerankArtifact {
  version?: string;
  featureOrder?: string[];
  featureNormalization?: Record<string, Record<string, unknown>>;
  treeParams?: {
    pairwiseDeltaModel?: TinyMlGradientBoostingRegressor;
  };
  weights?: {
    falsePositiveSuppression?: TinyMlLogisticModel;
  };
}

interface OperatorConfidenceArtifact {
  version?: string;
  featureOrder?: string[];
  featureNormalization?: Record<string, Record<string, unknown>>;
  weights?: TinyMlLogisticModel;
}

interface TinyMlArtifactBundle {
  featureSpec?: Record<string, unknown>;
  baseRerank?: BaseRerankArtifact;
  baseConfidence?: BaseConfidenceArtifact;
  operatorRerank?: OperatorRerankArtifact;
  operatorConfidence?: OperatorConfidenceArtifact;
}

export interface TinyMlRuntimeStatus {
  featureSpecAvailable: boolean;
  baseShadowAvailable: boolean;
  operatorShadowAvailable: boolean;
  shadowMode: true;
}

const TINY_ML_ARTIFACTS: TinyMlArtifactBundle = {
  featureSpec: parseArtifact<Record<string, unknown>>("feature-spec-v1.json"),
  baseRerank: parseArtifact<BaseRerankArtifact>("base-rerank-v1.json"),
  baseConfidence: parseArtifact<BaseConfidenceArtifact>("base-confidence-v1.json"),
  operatorRerank: parseArtifact<OperatorRerankArtifact>("operator-rerank-v1.json"),
  operatorConfidence: parseArtifact<OperatorConfidenceArtifact>("operator-confidence-v1.json")
};

export function getTinyMlRuntimeStatus(): TinyMlRuntimeStatus {
  return {
    featureSpecAvailable: Boolean(TINY_ML_ARTIFACTS.featureSpec),
    baseShadowAvailable: Boolean(TINY_ML_ARTIFACTS.baseRerank && TINY_ML_ARTIFACTS.baseConfidence),
    operatorShadowAvailable: Boolean(TINY_ML_ARTIFACTS.operatorRerank && TINY_ML_ARTIFACTS.operatorConfidence),
    shadowMode: true
  };
}

function parseArtifact<T>(fileName: string): T | undefined {
  const matchedPath = Object.keys(RAW_ML_ARTIFACTS).find((path) => path.endsWith(`/artifacts/ml/${fileName}`));

  if (!matchedPath) {
    return undefined;
  }

  try {
    return JSON.parse(RAW_ML_ARTIFACTS[matchedPath]) as T;
  } catch {
    return undefined;
  }
}

export function rerankBaseCandidates(params: {
  candidates: RecognitionCandidate[];
  normalizedCloud: PointSample[];
  features: RecognitionFeatures;
  quality?: QualityVector;
  profile?: UserInputProfile;
}): RecognitionCandidate[] {
  const { candidates, normalizedCloud, features, profile } = params;

  if (candidates.length < 2) {
    return candidates;
  }

  const shapeProfile = resolveShapeProfile(profile);
  const personalization = resolveBasePersonalizationRuntime(profile);

  if (personalization.featureInjectionMix <= 0) {
    return candidates;
  }

  const calibration = resolveCalibration(profile, shapeProfile, personalization.tutorialSampleCount);
  const topScore = candidates[0]?.score ?? 0;
  const rerankableFamilies = candidates
    .filter((candidate, index) => index < MAX_TOP_K && topScore - candidate.score <= TOP_K_WINDOW)
    .map((candidate) => candidate.family);

  if (rerankableFamilies.length < 2) {
    return candidates;
  }

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
      (prototypeSignal - 0.5) * calibration.userPrototypeWeight * personalization.featureInjectionMix * proximity;
    const rerankDelta = confusionBias * calibration.rerankStrength * personalization.featureInjectionMix * proximity;
    const confidenceDelta =
      Math.max(prototypeSignal - 0.55, 0) *
      calibration.confidenceBias *
      personalization.featureInjectionMix *
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
        `personalize=${personalization.stage}:${personalization.featureInjectionMix.toFixed(2)}`,
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
  const personalization = resolveOverlayPersonalizationRuntime(profile);
  const personalizationMix = personalization.featureInjectionMix;

  if (personalizationMix <= 0) {
    return sorted;
  }

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
          `personalize=${personalization.stage}:${personalizationMix.toFixed(2)}`,
          `placement=${placementSimilarity.toFixed(2)}`,
          `shape_conf=${nextShapeConfidence.toFixed(2)}`
        ]
      };
    })
    .sort((left, right) => right.score - left.score);
}

export function resolveBasePersonalizationRuntime(profile?: UserInputProfile): PersonalizationRuntimeSummary {
  const shapeProfile = resolveShapeProfile(profile);
  const tutorialSampleCount = Math.max(
    shapeProfile?.familyTutorialSampleCount ?? 0,
    shapeProfile?.tutorialSampleCount ?? 0
  );

  return buildPersonalizationRuntime({
    tutorialSampleCount,
    weakStart: BASE_PERSONALIZATION_WEAK_START,
    strongStart: BASE_PERSONALIZATION_STRONG_START,
    fullOn: BASE_PERSONALIZATION_FULLY_ON,
    confidenceBias: resolveCalibration(profile, shapeProfile, tutorialSampleCount).confidenceBias,
    weakBiasCap: 0.02,
    strongBiasCap: 0.05
  });
}

export function resolveOverlayPersonalizationRuntime(
  profile?: OverlayPersonalizationProfile
): PersonalizationRuntimeSummary {
  const tutorialSampleCount = Math.max(
    profile?.operatorTutorialSampleCount ?? 0,
    profile?.tutorialSampleCount ?? 0,
    profile?.sampleCount ?? 0
  );

  return buildPersonalizationRuntime({
    tutorialSampleCount,
    weakStart: OPERATOR_PERSONALIZATION_WEAK_START,
    strongStart: OPERATOR_PERSONALIZATION_STRONG_START,
    fullOn: OPERATOR_PERSONALIZATION_FULLY_ON,
    confidenceBias: resolveOverlayCalibration(profile).confidenceBias,
    weakBiasCap: 0.025,
    strongBiasCap: 0.045
  });
}

export function buildBaseShadowSummary(params: {
  heuristicCandidates: RecognitionCandidate[];
  actualCandidates: RecognitionCandidate[];
  actualStatus: RecognitionStatus;
  features: RecognitionFeatures;
  quality: QualityVector;
}): ShadowRuntimeSummary<GlyphFamily> {
  const rerankArtifact = TINY_ML_ARTIFACTS.baseRerank;
  const confidenceArtifact = TINY_ML_ARTIFACTS.baseConfidence;

  if (
    !rerankArtifact?.featureOrder ||
    !rerankArtifact.featureNormalization ||
    !rerankArtifact.models?.main?.model ||
    !confidenceArtifact?.featureOrder ||
    !confidenceArtifact.featureNormalization ||
    !confidenceArtifact.models?.main?.model
  ) {
    return buildHeuristicOnlyShadowSummary(params.heuristicCandidates, params.actualCandidates, params.actualStatus);
  }

  const topScore = params.heuristicCandidates[0]?.score ?? 0;
  const topMargin = topScore - (params.heuristicCandidates[1]?.score ?? 0);
  const selected = params.heuristicCandidates
    .slice(0, MAX_TOP_K)
    .filter((candidate) => topScore - candidate.score <= TOP_K_WINDOW)
    .map((candidate, candidateRank) => ({
      candidate,
      candidateRank
    }));

  if (selected.length === 0) {
    return buildHeuristicOnlyShadowSummary(params.heuristicCandidates, params.actualCandidates, params.actualStatus);
  }

  const candidatePairId = resolveBaseCandidatePairId(params.heuristicCandidates, topScore, topMargin, rerankArtifact.supportedPairs);
  const rows = selected.map(({ candidate, candidateRank }) => ({
    candidateFamilyId: candidate.family,
    candidatePairId,
    candidateRank,
    heuristicScore: candidate.score,
    templateDistance: candidate.templateDistance,
    topScoreGap: topScore - candidate.score,
    top1MinusTop2Margin: topMargin,
    strokeCount: params.features.strokeCount,
    pointCount: params.features.pointCount,
    durationMs: params.features.durationMs,
    pathLength: params.features.pathLength,
    closureGap: params.features.closureGap,
    dominantCorners: params.features.dominantCorners,
    endpointClusters: params.features.endpointClusters,
    circularity: params.features.circularity,
    fillRatio: params.features.fillRatio,
    parallelism: params.features.parallelism,
    rawAngleRadians: params.features.rawAngleRadians,
    qualityClosure: params.quality.closure,
    qualitySmoothness: params.quality.smoothness,
    qualityStability: params.quality.stability,
    qualityRotationBias: params.quality.rotationBias
  }));
  const probabilities = rows.map((row) =>
    evaluateGradientBoostingClassifier(
      rerankArtifact.models!.main!.model!,
      encodeBaseFeatureRow(row, rerankArtifact.featureOrder!, rerankArtifact.featureNormalization!)
    )
  );
  const reranked = applyBaseShadowRerank(rows, probabilities, rerankArtifact.models.main.deltaTransform ?? {});
  const topShadow = reranked[0];
  const secondShadow = reranked[1];
  const confidenceRow = {
    topFamilyId: topShadow.candidateFamilyId,
    candidatePairId,
    heuristicTopScore: topScore,
    heuristicMargin: topMargin,
    rerankedTopScore: topShadow.rerankedScore,
    rerankedMargin: topShadow.rerankedScore - (secondShadow?.rerankedScore ?? 0),
    rerankedTopProbability: topShadow.modelProbability,
    rerankedProbabilityGap: topShadow.modelProbability - (secondShadow?.modelProbability ?? 0),
    topDelta: topShadow.rerankDelta,
    topWindowSize: reranked.length,
    strokeCount: params.features.strokeCount,
    pointCount: params.features.pointCount,
    durationMs: params.features.durationMs,
    pathLength: params.features.pathLength,
    closureGap: params.features.closureGap,
    dominantCorners: params.features.dominantCorners,
    endpointClusters: params.features.endpointClusters,
    circularity: params.features.circularity,
    fillRatio: params.features.fillRatio,
    parallelism: params.features.parallelism,
    rawAngleRadians: params.features.rawAngleRadians,
    qualityClosure: params.quality.closure,
    qualitySmoothness: params.quality.smoothness,
    qualityStability: params.quality.stability,
    qualityRotationBias: params.quality.rotationBias
  };
  const calibratedConfidence = evaluateGradientBoostingClassifier(
    confidenceArtifact.models.main.model,
    encodeBaseFeatureRow(confidenceRow, confidenceArtifact.featureOrder, confidenceArtifact.featureNormalization)
  );
  const ambiguityProbability = clamp(1 - calibratedConfidence, 0.001, 0.999);
  const shadowStatus = resolveBaseStatus(
    topShadow.rerankedScore,
    topShadow.rerankedScore - (secondShadow?.rerankedScore ?? 0),
    params.heuristicCandidates.find((candidate) => candidate.family === topShadow.candidateFamilyId)?.completenessHint
  );
  const actualTopLabel = params.actualCandidates[0]?.family;

  return {
    mode: "shadow",
    artifactVersion: [
      rerankArtifact.version,
      confidenceArtifact.version
    ]
      .filter(Boolean)
      .join("+"),
    heuristicTopLabel: params.heuristicCandidates[0]?.family,
    shadowTopLabel: topShadow.candidateFamilyId,
    actualTopLabel,
    actualStatus: params.actualStatus,
    shadowStatus,
    decisionChanged: actualTopLabel !== topShadow.candidateFamilyId,
    statusChanged: params.actualStatus !== shadowStatus,
    calibratedConfidence,
    ambiguityProbability,
    candidates: reranked.map((row) => ({
      label: row.candidateFamilyId,
      heuristicScore: row.heuristicScore,
      shadowScore: row.rerankedScore,
      delta: row.rerankDelta,
      probability: row.modelProbability
    }))
  };
}

export function buildOverlayShadowSummary<T extends OverlayRerankCandidate>(params: {
  heuristicCandidates: T[];
  actualCandidates: T[];
  actualStatus: RecognitionStatus;
}): ShadowRuntimeSummary<OverlayOperator> {
  const rerankArtifact = TINY_ML_ARTIFACTS.operatorRerank;
  const confidenceArtifact = TINY_ML_ARTIFACTS.operatorConfidence;

  if (
    !rerankArtifact?.featureOrder ||
    !rerankArtifact.featureNormalization ||
    !rerankArtifact.treeParams?.pairwiseDeltaModel ||
    !rerankArtifact.weights?.falsePositiveSuppression ||
    !confidenceArtifact?.featureOrder ||
    !confidenceArtifact.featureNormalization ||
    !confidenceArtifact.weights
  ) {
    return buildHeuristicOnlyShadowSummary(params.heuristicCandidates, params.actualCandidates, params.actualStatus);
  }

  const sorted = [...params.heuristicCandidates].sort((left, right) => right.score - left.score);
  const topScore = sorted[0]?.score ?? 0;
  const topRows = sorted
    .slice(0, MAX_TOP_K)
    .filter((candidate) => topScore - candidate.score <= TOP_K_WINDOW)
    .map((candidate, candidateRank) =>
      buildOperatorShadowRow(candidate, candidateRank, resolveOperatorHardPairId(candidate, sorted), topScore)
    );

  if (topRows.length === 0) {
    return buildHeuristicOnlyShadowSummary(params.heuristicCandidates, params.actualCandidates, params.actualStatus);
  }

  const reranked = topRows
    .map((row) => {
      const encoded = encodeOperatorFeatureRow(row, rerankArtifact.featureOrder!, rerankArtifact.featureNormalization!);
      const pairwiseDelta = clamp(
        evaluateGradientBoostingRegressor(rerankArtifact.treeParams!.pairwiseDeltaModel!, encoded),
        -MAX_OVERLAY_SCORE_SHIFT,
        MAX_OVERLAY_SCORE_SHIFT
      );
      const suppression = evaluateLogisticProbability(rerankArtifact.weights!.falsePositiveSuppression!, encoded);
      const shadowScore = resolveOperatorShadowScore(row, pairwiseDelta, suppression);

      return {
        ...row,
        pairwiseDelta,
        suppression,
        shadowScore
      };
    })
    .sort((left, right) => right.shadowScore - left.shadowScore);
  const topShadow = reranked[0];
  const confidenceRow = encodeOperatorFeatureRow(topShadow, confidenceArtifact.featureOrder!, confidenceArtifact.featureNormalization!);
  const calibratedConfidence = evaluateLogisticProbability(confidenceArtifact.weights, confidenceRow);
  const ambiguityProbability = clamp(1 - calibratedConfidence, 0.001, 0.999);
  const actualTopLabel = params.actualCandidates[0]?.operator;

  return {
    mode: "shadow",
    artifactVersion: [rerankArtifact.version, confidenceArtifact.version].filter(Boolean).join("+"),
    heuristicTopLabel: sorted[0]?.operator,
    shadowTopLabel: topShadow.operatorId,
    actualTopLabel,
    actualStatus: params.actualStatus,
    shadowStatus: resolveOverlayStatus(topShadow.shadowScore, topShadow.blockedByFlag, topShadow.scaleScore),
    decisionChanged: actualTopLabel !== topShadow.operatorId,
    statusChanged: params.actualStatus !== resolveOverlayStatus(topShadow.shadowScore, topShadow.blockedByFlag, topShadow.scaleScore),
    calibratedConfidence,
    ambiguityProbability,
    candidates: reranked.map((row) => ({
      label: row.operatorId,
      heuristicScore: row.heuristicScore,
      shadowScore: row.shadowScore,
      delta: row.shadowScore - row.heuristicScore,
      probability: clamp(1 - row.suppression, 0.001, 0.999)
    }))
  };
}

function buildPersonalizationRuntime(input: {
  tutorialSampleCount: number;
  weakStart: number;
  strongStart: number;
  fullOn: number;
  confidenceBias: number;
  weakBiasCap: number;
  strongBiasCap: number;
}): PersonalizationRuntimeSummary {
  const { tutorialSampleCount, weakStart, strongStart, fullOn, confidenceBias, weakBiasCap, strongBiasCap } = input;

  if (tutorialSampleCount < weakStart) {
    return {
      tutorialSampleCount,
      stage: "none",
      featureInjectionMix: 0,
      thresholdBias: 0
    };
  }

  const weakProgress = clamp((tutorialSampleCount - weakStart) / Math.max(strongStart - weakStart, 1), 0, 1);
  const strongProgress = clamp((tutorialSampleCount - strongStart) / Math.max(fullOn - strongStart, 1), 0, 1);

  if (tutorialSampleCount < strongStart) {
    return {
      tutorialSampleCount,
      stage: "few_shot",
      featureInjectionMix: 0.2 + weakProgress * 0.35,
      thresholdBias: clamp(confidenceBias * 0.35 + weakProgress * 0.012, 0, weakBiasCap)
    };
  }

  return {
    tutorialSampleCount,
    stage: "enough_shot",
    featureInjectionMix: 0.55 + strongProgress * 0.45,
    thresholdBias: clamp(0.015 + confidenceBias * 0.6 + strongProgress * 0.025, 0, strongBiasCap)
  };
}

function buildHeuristicOnlyShadowSummary<
  TLabel extends string,
  TCandidate extends { score: number; family?: TLabel; operator?: TLabel }
>(
  heuristicCandidates: TCandidate[],
  actualCandidates: TCandidate[],
  actualStatus: RecognitionStatus
): ShadowRuntimeSummary<TLabel> {
  const resolveLabel = (candidate: TCandidate | undefined): TLabel | undefined =>
    candidate?.family ?? candidate?.operator;

  return {
    mode: "heuristic_only",
    heuristicTopLabel: resolveLabel(heuristicCandidates[0]),
    shadowTopLabel: resolveLabel(heuristicCandidates[0]),
    actualTopLabel: resolveLabel(actualCandidates[0]),
    actualStatus,
    shadowStatus: actualStatus,
    decisionChanged: false,
    statusChanged: false,
    candidates: heuristicCandidates.slice(0, MAX_TOP_K).map((candidate) => ({
      label: resolveLabel(candidate) as TLabel,
      heuristicScore: candidate.score,
      shadowScore: candidate.score,
      delta: 0
    }))
  };
}

function resolveBaseCandidatePairId(
  candidates: RecognitionCandidate[],
  topScore: number,
  topMargin: number,
  supportedPairs: string[] | undefined
): string {
  const topFamilies = candidates
    .slice(0, 2)
    .map((candidate) => candidate.family)
    .sort();

  if (topFamilies.length === 2) {
    const pairId = `${topFamilies[0]}__${topFamilies[1]}`;

    if (supportedPairs?.includes(pairId)) {
      return pairId;
    }
  }

  if (topScore >= 0.55 && (topScore < 0.7 || topMargin < 0.15)) {
    return "recognized__ambiguous";
  }

  return "other";
}

function applyBaseShadowRerank(
  rows: Array<{
    candidateFamilyId: GlyphFamily;
    candidatePairId: string;
    candidateRank: number;
    heuristicScore: number;
    topScoreGap: number;
    top1MinusTop2Margin: number;
  }>,
  probabilities: number[],
  deltaTransform: Record<string, number>
): Array<{
  candidateFamilyId: GlyphFamily;
  candidateRank: number;
  heuristicScore: number;
  modelProbability: number;
  topScoreGap: number;
  top1MinusTop2Margin: number;
  rerankDelta: number;
  rerankedScore: number;
}> {
  const withProbabilities = rows.map((row, index) => ({
    ...row,
    modelProbability: probabilities[index] ?? 0.5
  }));
  const heuristicTopProbability = withProbabilities[0]?.modelProbability ?? 0.5;

  return withProbabilities
    .map((row) => {
      const proximity = clamp(1 - row.topScoreGap / TOP_K_WINDOW, 0, 1);
      const centeredProbability = row.modelProbability - 0.5;
      const boostMargin = deltaTransform.boost_margin ?? 0.1;
      const supportGap = deltaTransform.support_gap ?? 0;
      const marginPressure = clamp((boostMargin - row.top1MinusTop2Margin) / Math.max(boostMargin, 0.0001), 0, 1);
      const probabilitySupport = clamp(
        (row.modelProbability - heuristicTopProbability - supportGap) / Math.max(1 - supportGap, 0.0001),
        0,
        1
      );
      const baseDeltaScale = deltaTransform.delta_scale ?? 0;
      const pairMultiplier =
        row.candidatePairId === "earth__fire" || row.candidatePairId === "water__life"
          ? deltaTransform.known_pair_multiplier ?? 1
          : row.candidatePairId === "recognized__ambiguous"
            ? deltaTransform.recognized_pair_multiplier ?? 0.75
            : deltaTransform.other_pair_multiplier ?? 0;
      let delta = centeredProbability * baseDeltaScale * proximity * pairMultiplier;

      if (row.candidateRank === 0) {
        delta = Math.min(delta, (deltaTransform.top_cap ?? 0.02));
      } else if (row.candidateRank === 1) {
        delta =
          delta > 0
            ? Math.min(delta, (deltaTransform.runner_cap ?? 0.04) * marginPressure * probabilitySupport)
            : Math.max(delta, -(deltaTransform.negative_cap ?? 0.05) * (0.55 + proximity * 0.45));
      } else {
        delta =
          delta > 0
            ? Math.min(delta, (deltaTransform.third_cap ?? 0.03) * marginPressure * probabilitySupport * 0.8)
            : Math.max(delta, -(deltaTransform.negative_cap ?? 0.05) * (0.55 + proximity * 0.45));
      }

      return {
        ...row,
        rerankDelta: delta,
        rerankedScore: clamp(row.heuristicScore + delta, 0, 1)
      };
    })
    .sort((left, right) => right.rerankedScore - left.rerankedScore);
}

function buildOperatorShadowRow(
  candidate: OverlayRerankCandidate,
  candidateRank: number,
  hardPairId: string,
  topScore: number
): {
  operatorId: OverlayOperator;
  hardPairId: string;
  candidateRank: number;
  heuristicScore: number;
  baseScore: number;
  templateDistance: number;
  shapeConfidence: number;
  topScoreGap: number;
  blockedByFlag: number;
  blockedByOperator: OverlayOperator | "none";
  anchorZoneId: OverlayAnchorZoneId;
  placement: OverlayAnchorZoneId;
  anchorScore: number;
  scaleScore: number;
  gateStrength: number;
  angleRadians: number;
  scaleRatio: number;
  straightness: number;
  corners: number;
  closure: number;
  stackIndex: number;
  existingOperatorsCount: number;
  existingOperatorsMask: OverlayOperator[];
  hasVoidCutInStack: number;
} {
  const existingOperators = candidate.existingOperators ?? [];

  return {
    operatorId: candidate.operator,
    hardPairId,
    candidateRank,
    heuristicScore: candidate.score,
    baseScore: candidate.baseScore,
    templateDistance: candidate.templateDistance,
    shapeConfidence: candidate.shapeConfidence,
    topScoreGap: topScore - candidate.score,
    blockedByFlag: candidate.blockedBy ? 1 : 0,
    blockedByOperator: candidate.blockedBy ?? "none",
    anchorZoneId: candidate.anchorZoneId ?? "core",
    placement: candidate.placementAnchorZoneId ?? "core",
    anchorScore: candidate.anchorScore,
    scaleScore: candidate.scaleScore,
    gateStrength: Math.min(candidate.anchorScore, candidate.scaleScore),
    angleRadians: candidate.angleRadians,
    scaleRatio: candidate.scaleRatio,
    straightness: candidate.straightness,
    corners: candidate.corners,
    closure: candidate.closure,
    stackIndex: candidate.stackIndex ?? existingOperators.length,
    existingOperatorsCount: existingOperators.length,
    existingOperatorsMask: existingOperators,
    hasVoidCutInStack: existingOperators.includes("void_cut") ? 1 : 0
  };
}

function resolveOperatorHardPairId(
  candidate: OverlayRerankCandidate,
  sortedCandidates: OverlayRerankCandidate[]
): string {
  const activePair = resolveActiveOverlayPair(sortedCandidates);

  if (activePair && activePair.includes(candidate.operator)) {
    return "void_cut__electric_fork";
  }

  if (candidate.operator === "martial_axis" && candidate.blockedBy === "void_cut") {
    return "martial_axis__blocked_by_void_cut";
  }

  if (candidate.operator === "ice_bar" && candidate.scaleScore < 0.46) {
    return "ice_bar__partial_stroke";
  }

  if (candidate.operator === "steel_brace" && candidate.closure > 0.18) {
    return "steel_brace__open_box_like";
  }

  return "other";
}

function resolveOperatorShadowScore(
  row: { heuristicScore: number; gateStrength: number; blockedByFlag: number },
  pairwiseDelta: number,
  suppression: number
): number {
  if (row.blockedByFlag === 1) {
    return -1e9;
  }

  if (row.gateStrength < WEAK_OPERATOR_GATE_THRESHOLD) {
    return row.heuristicScore - (0.18 + suppression * 0.18);
  }

  return row.heuristicScore + pairwiseDelta * row.gateStrength - suppression * (MAX_OPERATOR_SUPPRESSION_SCALE + (1 - row.gateStrength) * 0.04);
}

function resolveBaseStatus(score: number, margin: number, completenessHint?: string): RecognitionStatus {
  if (score >= 0.55 && completenessHint) {
    return "incomplete";
  }

  if (score >= 0.7 && margin >= 0.15) {
    return "recognized";
  }

  if (score >= 0.55) {
    return "ambiguous";
  }

  return "invalid";
}

function resolveOverlayStatus(score: number, blockedByFlag: number, scaleScore: number): RecognitionStatus {
  if (blockedByFlag === 1 && score >= 0.62) {
    return "incomplete";
  }

  if (score >= 0.74 && scaleScore >= 0.34) {
    return "recognized";
  }

  if (score >= 0.56) {
    return "ambiguous";
  }

  return "invalid";
}

function encodeBaseFeatureRow(
  row: Record<string, unknown>,
  featureOrder: string[],
  featureNormalization: Record<string, Record<string, unknown>>
): number[] {
  return encodeFeatureRow(row, featureOrder, featureNormalization, { numericMode: "base" });
}

function encodeOperatorFeatureRow(
  row: Record<string, unknown>,
  featureOrder: string[],
  featureNormalization: Record<string, Record<string, unknown>>
): number[] {
  return encodeFeatureRow(row, featureOrder, featureNormalization, { numericMode: "operator" });
}

function encodeFeatureRow(
  row: Record<string, unknown>,
  featureOrder: string[],
  featureNormalization: Record<string, Record<string, unknown>>,
  options: { numericMode: "base" | "operator" }
): number[] {
  const encoded: number[] = [];

  for (const feature of featureOrder) {
    const normalization = featureNormalization[feature];

    if (!normalization) {
      continue;
    }

    const value = row[feature];
    const type = String(normalization.type ?? "");

    if (type === "one_hot") {
      const values = Array.isArray(normalization.values) ? normalization.values : [];
      encoded.push(...values.map((option) => (String(value) === String(option) ? 1 : 0)));
      continue;
    }

    if (type === "multi_hot") {
      const values = Array.isArray(normalization.values) ? normalization.values : [];
      const current = new Set(Array.isArray(value) ? value.map(String) : []);
      encoded.push(...values.map((option) => (current.has(String(option)) ? 1 : 0)));
      continue;
    }

    if (type === "binary") {
      encoded.push(value ? 1 : 0);
      continue;
    }

    if (type === "log1p_clip") {
      const maximum = Number(normalization.max ?? 0);
      const numericValue = Math.max(0, Math.min(Number(value ?? 0), maximum));
      encoded.push(Math.log1p(numericValue));
      continue;
    }

    const minimum = Number(normalization.min ?? 0);
    const maximum = Number(normalization.max ?? 0);
    const numericValue = Number(value ?? 0);

    if (type === "integer_clip") {
      const clipped = clamp(Math.round(numericValue), minimum, maximum);
      encoded.push(options.numericMode === "operator" ? scaleToUnit(clipped, minimum, maximum) : clipped);
      continue;
    }

    if (type === "clamp") {
      const clipped = clamp(numericValue, minimum, maximum);
      encoded.push(options.numericMode === "operator" ? scaleToUnit(clipped, minimum, maximum) : clipped);
      continue;
    }
  }

  return encoded;
}

function scaleToUnit(value: number, minimum: number, maximum: number): number {
  if (maximum <= minimum) {
    return 0;
  }

  return (value - minimum) / (maximum - minimum);
}

function evaluateGradientBoostingClassifier(model: TinyMlGradientBoostingClassifier, features: number[]): number {
  const baselineProbability = clamp(model.initProbability, 0.001, 0.999);
  const baselineLogit = Math.log(baselineProbability / (1 - baselineProbability));
  const rawScore = baselineLogit + model.learningRate * sumTreeOutputs(model.treeParams.estimators, features);
  return clamp(1 / (1 + Math.exp(-rawScore)), 0.001, 0.999);
}

function evaluateGradientBoostingRegressor(model: TinyMlGradientBoostingRegressor, features: number[]): number {
  return model.initialPrediction + model.learningRate * sumTreeOutputs(model.trees, features);
}

function sumTreeOutputs(trees: TinyMlTreeModel[], features: number[]): number {
  return trees.reduce((sum, tree) => sum + evaluateTree(tree, features), 0);
}

function evaluateTree(tree: TinyMlTreeModel, features: number[]): number {
  let nodeIndex = 0;
  const featureIndexes = tree.featureIndex ?? tree.feature ?? [];

  while (nodeIndex >= 0) {
    const featureIndex = featureIndexes[nodeIndex] ?? -1;

    if (featureIndex < 0) {
      return tree.value[nodeIndex] ?? 0;
    }

    const threshold = tree.threshold[nodeIndex] ?? 0;
    const featureValue = features[featureIndex] ?? 0;
    nodeIndex =
      featureValue <= threshold
        ? (tree.childrenLeft[nodeIndex] ?? -1)
        : (tree.childrenRight[nodeIndex] ?? -1);
  }

  return 0;
}

function evaluateLogisticProbability(model: TinyMlLogisticModel, features: number[]): number {
  const intercept = Array.isArray(model.intercept) ? Number(model.intercept[0] ?? 0) : Number(model.intercept ?? 0);
  const weights = model.coefficients ?? model.weights ?? [];
  const logit = weights.reduce((sum, weight, index) => sum + weight * (features[index] ?? 0), intercept);
  return clamp(1 / (1 + Math.exp(-logit)), 0.001, 0.999);
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
    (Math.max(profile?.operatorTutorialSampleCount ?? 0, profile?.tutorialSampleCount ?? 0, profile?.sampleCount ?? 0) -
      OPERATOR_PERSONALIZATION_WEAK_START) /
      Math.max(OPERATOR_PERSONALIZATION_FULLY_ON - OPERATOR_PERSONALIZATION_WEAK_START, 1),
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
  const fallbackStrength = clamp(
    (tutorialSampleCount - BASE_PERSONALIZATION_WEAK_START) /
      Math.max(BASE_PERSONALIZATION_FULLY_ON - BASE_PERSONALIZATION_WEAK_START, 1),
    0,
    1
  );
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
