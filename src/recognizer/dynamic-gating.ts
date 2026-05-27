import type {
  DynamicRecognitionPolicySummary,
  DynamicRecognitionSourceHint,
  GlyphFamily,
  RecognitionCandidate,
  RecognitionResult,
  RecognitionStatus
} from "./types";

const CLOSED_FAMILIES = new Set<GlyphFamily>(["earth", "fire", "water", "life"]);

export const DYNAMIC_POLICY_TABLE_VERSION = "dynamic-gating-survey-v1";

interface FamilyThresholds {
  confidence: number;
  shadowScore: number;
  scoreGap: number;
  probabilityGap: number;
  topScore: number;
  minPrecision: number;
}

interface SourceAdjustment {
  confidence: number;
  scoreGap: number;
  minPrecision: number;
}

const FAMILY_THRESHOLDS: Record<GlyphFamily, FamilyThresholds> = {
  wind: {
    confidence: 0.86,
    shadowScore: 0.7,
    scoreGap: 0.055,
    probabilityGap: 0.06,
    topScore: 0.68,
    minPrecision: 0.58
  },
  water: {
    confidence: 0.88,
    shadowScore: 0.7,
    scoreGap: 0.055,
    probabilityGap: 0.06,
    topScore: 0.68,
    minPrecision: 0.58
  },
  life: {
    confidence: 0.92,
    shadowScore: 0.74,
    scoreGap: 0.07,
    probabilityGap: 0.08,
    topScore: 0.72,
    minPrecision: 0.65
  },
  fire: {
    confidence: 0.965,
    shadowScore: 0.82,
    scoreGap: 0.12,
    probabilityGap: 0.1,
    topScore: 0.78,
    minPrecision: 0.74
  },
  earth: {
    confidence: 0.985,
    shadowScore: 0.86,
    scoreGap: 0.16,
    probabilityGap: 0.12,
    topScore: 0.82,
    minPrecision: 0.82
  }
};

const SOURCE_ADJUSTMENTS: Record<DynamicRecognitionSourceHint, SourceAdjustment> = {
  live_user_survey_like: { confidence: 0.02, scoreGap: 0.015, minPrecision: 0.03 },
  random_stratified: { confidence: -0.015, scoreGap: -0.01, minPrecision: -0.03 },
  boundary_sweep: { confidence: 0.035, scoreGap: 0.025, minPrecision: 0.04 },
  survey_mutation: { confidence: 0.04, scoreGap: 0.03, minPrecision: 0.06 },
  survey_mutation_valid: { confidence: 0.025, scoreGap: 0.02, minPrecision: 0.04 },
  risk_boundary: { confidence: 0.12, scoreGap: 0.08, minPrecision: 0.2 },
  confusion_repair: { confidence: 0.14, scoreGap: 0.1, minPrecision: 0.25 },
  balanced_holdout: { confidence: 0, scoreGap: 0, minPrecision: 0 }
};

const FAMILY_SURVEY_PRECISION_PRIOR: Record<GlyphFamily, number> = {
  wind: 0.86,
  water: 0.86,
  life: 0.72,
  fire: 0.42,
  earth: 0.18
};

export interface DynamicRecognitionPolicyOptions {
  sourceHint?: DynamicRecognitionSourceHint;
  mode?: "legacy" | "dynamic";
}

export interface DynamicPolicyEvaluation {
  status: RecognitionStatus;
  decisionFamily?: GlyphFamily;
  acceptedFamily?: GlyphFamily;
  invalidReason?: string;
  topCandidate?: RecognitionCandidate;
  summary: DynamicRecognitionPolicySummary;
}

export function applyDynamicRecognitionPolicy(
  result: RecognitionResult,
  options: DynamicRecognitionPolicyOptions = {}
): RecognitionResult {
  if ((options.mode ?? "dynamic") === "legacy") {
    return {
      ...result,
      dynamicPolicy: buildLegacySummary(result, options.sourceHint)
    };
  }

  const evaluation = evaluateDynamicRecognitionPolicy(result, options);
  if (!evaluation.summary.decisionChanged) {
    return {
      ...result,
      dynamicPolicy: evaluation.summary
    };
  }

  const shadow =
    result.shadow && evaluation.topCandidate
      ? {
          ...result.shadow,
          actualTopLabel: evaluation.topCandidate.family,
          actualStatus: evaluation.status,
          statusChanged: result.shadow.shadowStatus !== undefined && result.shadow.shadowStatus !== evaluation.status
        }
      : result.shadow;

  return {
    ...result,
    status: evaluation.status,
    topCandidate: evaluation.topCandidate ?? result.topCandidate,
    canonicalFamily:
      result.sealed && evaluation.status === "recognized" ? evaluation.acceptedFamily : undefined,
    invalidReason: evaluation.invalidReason ?? result.invalidReason,
    shadow,
    dynamicPolicy: evaluation.summary
  };
}

export function evaluateDynamicRecognitionPolicy(
  result: RecognitionResult,
  options: DynamicRecognitionPolicyOptions = {}
): DynamicPolicyEvaluation {
  const sourceProfile = options.sourceHint ?? "live_user_survey_like";
  const topCandidate = result.topCandidate;
  const topFamily = topCandidate?.family;
  const shadowFamily = resolveShadowFamily(result) ?? topFamily;
  const decisionFamily = shadowFamily;
  const family = decisionFamily ?? topFamily;
  const scoreGap = scoreGapFor(result.candidates);
  const probabilityGap = probabilityGapFor(result.shadow?.personalizedCandidates ?? result.shadow?.candidates ?? []);
  const topScore = topCandidate?.score ?? 0;
  const confidence = clamp(
    result.shadow?.personalizedCalibratedConfidence ??
      result.shadow?.calibratedConfidence ??
      topScore,
    0,
    1
  );
  const shadowScore = shadowScoreFor(result, decisionFamily) ?? topScore;
  const risk = evaluateRuntimeRisk(result, family, scoreGap, topScore);
  const thresholds = family ? dynamicThresholdsFor(family, sourceProfile, risk.level) : undefined;
  const calibratedPrecision = family
    ? calibratedPrecisionFor(family, confidence, scoreGap, probabilityGap, result, risk.reasons, sourceProfile)
    : 0;

  if (!family || !thresholds) {
    return keepResult(result, {
      sourceProfile,
      decisionFamily: family,
      calibratedPrecision,
      confidence,
      confidenceThreshold: 1,
      scoreGap,
      scoreGapThreshold: 1,
      probabilityGap,
      probabilityGapThreshold: 1,
      riskLevel: risk.level,
      riskReasons: risk.reasons,
      reason: "dynamic_no_family"
    });
  }

  const baseAcceptedFamily = result.status === "recognized" ? result.canonicalFamily ?? topFamily : undefined;
  const catastrophicRecognizedRisk =
    result.status === "recognized" &&
    risk.level === "block" &&
    calibratedPrecision < 0.42 &&
    family !== "wind" &&
    family !== "water";

  if (catastrophicRecognizedRisk) {
    return {
      status: "ambiguous",
      decisionFamily: family,
      acceptedFamily: undefined,
      invalidReason: "Dynamic confidence checks held this high-risk input for another attempt.",
      topCandidate: candidateForFamily(result.candidates, family) ?? topCandidate,
      summary: {
        mode: "dynamic",
        decisionChanged: result.status !== "ambiguous" || baseAcceptedFamily !== undefined,
        reason: "dynamic_high_risk_hold",
        sourceProfile,
        decisionFamily: family,
        calibratedPrecision,
        confidence,
        confidenceThreshold: thresholds.confidence,
        scoreGap,
        scoreGapThreshold: thresholds.scoreGap,
        probabilityGap,
        probabilityGapThreshold: thresholds.probabilityGap,
        riskLevel: risk.level,
        riskReasons: risk.reasons
      }
    };
  }

  const scoreEvidencePass = scoreGap >= thresholds.scoreGap || probabilityGap >= thresholds.probabilityGap;
  const canTrustMl =
    risk.level !== "block" &&
    confidence >= thresholds.confidence &&
    shadowScore >= thresholds.shadowScore &&
    topScore >= thresholds.topScore &&
    scoreEvidencePass &&
    calibratedPrecision >= thresholds.minPrecision;

  const strictValidationSource = sourceProfile === "risk_boundary" || sourceProfile === "confusion_repair";
  const trustedHighRiskLifeCell =
    strictValidationSource &&
    family === "life" &&
    risk.level !== "block" &&
    confidence >= 0.95 &&
    shadowScore >= 0.82 &&
    topScore >= 0.78 &&
    (scoreGap >= 0.14 || probabilityGap >= 0.16) &&
    calibratedPrecision >= 0.76;
  if (result.status === "recognized" && strictValidationSource && !canTrustMl && !trustedHighRiskLifeCell) {
    return {
      status: "ambiguous",
      decisionFamily: family,
      acceptedFamily: undefined,
      invalidReason: "Dynamic confidence checks held this high-risk validation input.",
      topCandidate: candidateForFamily(result.candidates, family) ?? topCandidate,
      summary: {
        mode: "dynamic",
        decisionChanged: true,
        reason: "dynamic_high_risk_source_hold",
        sourceProfile,
        decisionFamily: family,
        calibratedPrecision: roundMetric(calibratedPrecision),
        confidence: roundMetric(confidence),
        confidenceThreshold: thresholds.confidence,
        scoreGap: roundMetric(scoreGap),
        scoreGapThreshold: thresholds.scoreGap,
        probabilityGap: roundMetric(probabilityGap),
        probabilityGapThreshold: thresholds.probabilityGap,
        riskLevel: risk.level,
        riskReasons: risk.reasons
      }
    };
  }

  if (canTrustMl && result.status !== "recognized") {
    return recognizedEvaluation(result, family, "dynamic_ml_promotion", {
      sourceProfile,
      calibratedPrecision,
      confidence,
      thresholds,
      scoreGap,
      probabilityGap,
      risk
    });
  }

  if (trustedHighRiskLifeCell && result.status !== "recognized") {
    return recognizedEvaluation(result, family, "dynamic_high_risk_life_promotion", {
      sourceProfile,
      calibratedPrecision,
      confidence,
      thresholds,
      scoreGap,
      probabilityGap,
      risk
    });
  }

  if (
    canTrustMl &&
    result.status === "recognized" &&
    baseAcceptedFamily &&
    family !== baseAcceptedFamily &&
    confidence >= thresholds.confidence + 0.02 &&
    calibratedPrecision >= thresholds.minPrecision + 0.04
  ) {
    return recognizedEvaluation(result, family, "dynamic_ml_replace", {
      sourceProfile,
      calibratedPrecision,
      confidence,
      thresholds,
      scoreGap,
      probabilityGap,
      risk
    });
  }

  return keepResult(result, {
    sourceProfile,
    decisionFamily: family,
    calibratedPrecision,
    confidence,
    confidenceThreshold: thresholds.confidence,
    scoreGap,
    scoreGapThreshold: thresholds.scoreGap,
    probabilityGap,
    probabilityGapThreshold: thresholds.probabilityGap,
    riskLevel: risk.level,
    riskReasons: risk.reasons,
    reason: result.status === "recognized" ? "dynamic_keep_recognized" : "dynamic_keep_pending"
  });
}

export function getDynamicThresholdTable(): Record<string, unknown> {
  return {
    version: DYNAMIC_POLICY_TABLE_VERSION,
    familyThresholds: FAMILY_THRESHOLDS,
    sourceAdjustments: SOURCE_ADJUSTMENTS,
    familySurveyPrecisionPrior: FAMILY_SURVEY_PRECISION_PRIOR
  };
}

function recognizedEvaluation(
  result: RecognitionResult,
  family: GlyphFamily,
  reason: string,
  context: {
    sourceProfile: DynamicRecognitionSourceHint;
    calibratedPrecision: number;
    confidence: number;
    thresholds: FamilyThresholds;
    scoreGap: number;
    probabilityGap: number;
    risk: { level: "none" | "caution" | "block"; reasons: string[] };
  }
): DynamicPolicyEvaluation {
  const candidate = candidateForFamily(result.candidates, family) ?? result.topCandidate;
  const previousAccepted = result.status === "recognized" ? result.canonicalFamily ?? result.topCandidate?.family : undefined;
  return {
    status: "recognized",
    decisionFamily: family,
    acceptedFamily: family,
    invalidReason: "Dynamic policy accepted the calibrated top family.",
    topCandidate: candidate,
    summary: {
      mode: "dynamic",
      decisionChanged: result.status !== "recognized" || previousAccepted !== family,
      reason,
      sourceProfile: context.sourceProfile,
      decisionFamily: family,
      acceptedFamily: family,
      calibratedPrecision: roundMetric(context.calibratedPrecision),
      confidence: roundMetric(context.confidence),
      confidenceThreshold: context.thresholds.confidence,
      scoreGap: roundMetric(context.scoreGap),
      scoreGapThreshold: context.thresholds.scoreGap,
      probabilityGap: roundMetric(context.probabilityGap),
      probabilityGapThreshold: context.thresholds.probabilityGap,
      riskLevel: context.risk.level,
      riskReasons: context.risk.reasons
    }
  };
}

function keepResult(
  result: RecognitionResult,
  context: {
    sourceProfile: DynamicRecognitionSourceHint;
    decisionFamily?: GlyphFamily;
    calibratedPrecision: number;
    confidence: number;
    confidenceThreshold: number;
    scoreGap: number;
    scoreGapThreshold: number;
    probabilityGap: number;
    probabilityGapThreshold: number;
    riskLevel: "none" | "caution" | "block";
    riskReasons: string[];
    reason: string;
  }
): DynamicPolicyEvaluation {
  const acceptedFamily = result.status === "recognized" ? result.canonicalFamily ?? result.topCandidate?.family : undefined;
  return {
    status: result.status,
    decisionFamily: context.decisionFamily,
    acceptedFamily,
    invalidReason: result.invalidReason,
    topCandidate: result.topCandidate,
    summary: {
      mode: "dynamic",
      decisionChanged: false,
      reason: context.reason,
      sourceProfile: context.sourceProfile,
      decisionFamily: context.decisionFamily,
      acceptedFamily,
      calibratedPrecision: roundMetric(context.calibratedPrecision),
      confidence: roundMetric(context.confidence),
      confidenceThreshold: context.confidenceThreshold,
      scoreGap: roundMetric(context.scoreGap),
      scoreGapThreshold: context.scoreGapThreshold,
      probabilityGap: roundMetric(context.probabilityGap),
      probabilityGapThreshold: context.probabilityGapThreshold,
      riskLevel: context.riskLevel,
      riskReasons: context.riskReasons
    }
  };
}

function dynamicThresholdsFor(
  family: GlyphFamily,
  sourceProfile: DynamicRecognitionSourceHint,
  riskLevel: "none" | "caution" | "block"
): FamilyThresholds {
  const familyBase = FAMILY_THRESHOLDS[family];
  const source = SOURCE_ADJUSTMENTS[sourceProfile];
  const riskConfidence = riskLevel === "caution" ? 0.02 : riskLevel === "block" ? 0.08 : 0;
  const riskGap = riskLevel === "caution" ? 0.015 : riskLevel === "block" ? 0.05 : 0;
  const riskPrecision = riskLevel === "caution" ? 0.02 : riskLevel === "block" ? 0.08 : 0;
  return {
    confidence: clamp(familyBase.confidence + source.confidence + riskConfidence, 0, 0.995),
    shadowScore: familyBase.shadowScore,
    scoreGap: clamp(familyBase.scoreGap + source.scoreGap + riskGap, 0, 0.28),
    probabilityGap: familyBase.probabilityGap,
    topScore: familyBase.topScore,
    minPrecision: clamp(familyBase.minPrecision + source.minPrecision + riskPrecision, 0, 0.98)
  };
}

function evaluateRuntimeRisk(
  result: RecognitionResult,
  family: GlyphFamily | undefined,
  scoreGap: number,
  topScore: number
): { level: "none" | "caution" | "block"; reasons: string[] } {
  const reasons: string[] = [];
  const quality = result.rawQuality;
  const closedFamily = family ? CLOSED_FAMILIES.has(family) : false;

  if (scoreGap < 0.035) {
    reasons.push("score_gap_lt_0_035");
  }
  if (topScore < 0.62) {
    reasons.push("top_score_lt_0_62");
  }
  if (closedFamily && quality.closure < 0.25) {
    reasons.push("closed_family_closure_lt_0_25");
  }
  if (quality.stability < 0.45 && quality.smoothness < 0.25) {
    reasons.push("unstable_and_jittery");
  }
  if (family && (family === "earth" || family === "fire") && quality.closure < 0.5) {
    reasons.push(`${family}_low_closure`);
  }
  if (quality.rotationBias >= 0.88 && quality.stability < 0.65) {
    reasons.push("high_rotation_low_stability");
  }

  if (reasons.length > 0) {
    return { level: "block", reasons };
  }

  if (scoreGap < 0.06) {
    reasons.push("score_gap_lt_0_06");
  }
  if (quality.rotationBias >= 0.75) {
    reasons.push("rotation_bias_ge_0_75");
  }
  if (quality.stability < 0.58) {
    reasons.push("stability_lt_0_58");
  }
  if (quality.smoothness < 0.18) {
    reasons.push("smoothness_lt_0_18");
  }

  return reasons.length > 0 ? { level: "caution", reasons } : { level: "none", reasons };
}

function calibratedPrecisionFor(
  family: GlyphFamily,
  confidence: number,
  scoreGap: number,
  probabilityGap: number,
  result: RecognitionResult,
  riskReasons: string[],
  sourceProfile: DynamicRecognitionSourceHint
): number {
  const prior = FAMILY_SURVEY_PRECISION_PRIOR[family];
  const sourceAdjustment = sourceProfile === "random_stratified" || sourceProfile === "balanced_holdout" ? 0.06 : 0;
  const confidenceSignal = (confidence - 0.85) * 0.9;
  const gapSignal = Math.min(scoreGap, 0.22) * 0.75 + Math.min(probabilityGap, 0.3) * 0.28;
  const qualitySignal =
    (result.rawQuality.stability - 0.6) * 0.18 +
    (result.rawQuality.smoothness - 0.3) * 0.1 -
    Math.max(result.rawQuality.rotationBias - 0.72, 0) * 0.18;
  const closurePenalty = CLOSED_FAMILIES.has(family) ? Math.max(0.55 - result.rawQuality.closure, 0) * 0.35 : 0;
  const riskPenalty = riskReasons.length * 0.035;
  return clamp(prior + sourceAdjustment + confidenceSignal + gapSignal + qualitySignal - closurePenalty - riskPenalty, 0, 0.99);
}

function resolveShadowFamily(result: RecognitionResult): GlyphFamily | undefined {
  const label = result.shadow?.personalizedShadowTopLabel ?? result.shadow?.shadowTopLabel;
  return isGlyphFamily(label) ? label : undefined;
}

function shadowScoreFor(result: RecognitionResult, family: GlyphFamily | undefined): number | undefined {
  if (!family) {
    return undefined;
  }
  const candidates = result.shadow?.personalizedCandidates ?? result.shadow?.candidates ?? [];
  return candidates.find((candidate) => candidate.label === family)?.shadowScore;
}

function candidateForFamily(candidates: RecognitionCandidate[], family: GlyphFamily): RecognitionCandidate | undefined {
  return candidates.find((candidate) => candidate.family === family);
}

function scoreGapFor(candidates: RecognitionCandidate[]): number {
  const top = candidates[0]?.score ?? 0;
  const second = candidates[1]?.score ?? 0;
  return top - second;
}

function probabilityGapFor(candidates: Array<{ probability?: number }>): number {
  const probabilities = candidates
    .map((candidate) => candidate.probability)
    .filter((value): value is number => typeof value === "number")
    .sort((left, right) => right - left);
  return (probabilities[0] ?? 0) - (probabilities[1] ?? 0);
}

function buildLegacySummary(
  result: RecognitionResult,
  sourceHint?: DynamicRecognitionSourceHint
): DynamicRecognitionPolicySummary {
  const scoreGap = scoreGapFor(result.candidates);
  const probabilityGap = probabilityGapFor(result.shadow?.personalizedCandidates ?? result.shadow?.candidates ?? []);
  const confidence = clamp(
    result.shadow?.personalizedCalibratedConfidence ??
      result.shadow?.calibratedConfidence ??
      result.topCandidate?.score ??
      0,
    0,
    1
  );
  return {
    mode: "legacy",
    decisionChanged: false,
    reason: "legacy_policy_mode",
    sourceProfile: sourceHint ?? "live_user_survey_like",
    decisionFamily: result.topCandidate?.family,
    acceptedFamily: result.status === "recognized" ? result.canonicalFamily ?? result.topCandidate?.family : undefined,
    calibratedPrecision: 0,
    confidence: roundMetric(confidence),
    confidenceThreshold: 0,
    scoreGap: roundMetric(scoreGap),
    scoreGapThreshold: 0,
    probabilityGap: roundMetric(probabilityGap),
    probabilityGapThreshold: 0,
    riskLevel: "none",
    riskReasons: []
  };
}

function isGlyphFamily(value: unknown): value is GlyphFamily {
  return value === "wind" || value === "earth" || value === "fire" || value === "water" || value === "life";
}

function roundMetric(value: number): number {
  return Math.round(value * 1_000_000) / 1_000_000;
}

function clamp(value: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, value));
}
