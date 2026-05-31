import type {
  DatacardRecognitionResult,
  DatacardShapeId,
  DatacardTinyMlContrastDecision
} from "./datacard-shape-lab";

export type DynamicDecision = "accept" | "hold" | "retry";
export type TinyMlTrackId = "shadow_gate" | "meaning_recovery" | "balanced";

export interface TinyMlTrackCorrection {
  track: Exclude<TinyMlTrackId, "balanced">;
  label: string;
  adjustedScore: number;
  threshold: number;
  margin: number;
  decision: DynamicDecision;
  correction: number;
  reasons: string[];
}

export interface TinyMlTwoTrackCorrection {
  version: "tinyml-two-track-v1";
  shadowTrack: TinyMlTrackCorrection;
  meaningTrack: TinyMlTrackCorrection;
  agreement: "agree_accept" | "agree_hold" | "agree_retry" | "contrast";
  delta: number;
  selectedTrack: TinyMlTrackId;
  finalDecision: DynamicDecision;
  finalReason: string;
  promotePriority: boolean;
  blockPriorityFlip: boolean;
}

export interface RecognitionSummary {
  selectedCandidateId: string;
  finalCandidateId: string;
  finalStatus: string;
  score: number;
  shadowConfidence: number;
  meaningConfidence: number;
  unsafeRisk: number;
  flipRisk: number;
  topCandidates: Array<{ id: string; label: string; score: number; status: string }>;
}

export interface TargetThresholdState {
  captureCount: number;
  evalCount: number;
  top1Rate: number;
  confusionScore: number;
  acceptThreshold: number;
}

export interface ThresholdState {
  captureCount: number;
  globalMaturity: number;
  globalScoreLift: number;
  acceptThreshold: number;
  holdThreshold: number;
  unsafeLimit: number;
  flipLimit: number;
  targetRankLimit: number;
  topGapFloor: number;
  targetAdjustments: Record<string, TargetThresholdState>;
}

export interface ConfusionSnapshot {
  targetRank: number | null;
  topPair: string;
  topGap: number;
  targetInTop5: boolean;
  confusedWith: string;
  confusionScore: number;
}

export interface TwoTrackHistoryEntry {
  targetPresetId: string;
  recognition: Pick<RecognitionSummary, "score" | "unsafeRisk" | "flipRisk">;
  confusion: ConfusionSnapshot;
  tinyMlCorrection?: TinyMlTwoTrackCorrection;
  dynamicDecision?: DynamicDecision;
}

export interface TwoTrackThresholdInput {
  captures: readonly TwoTrackHistoryEntry[];
  evals: readonly TwoTrackHistoryEntry[];
  targetPresetIds: readonly string[];
}

export interface TwoTrackDecisionInput {
  summary: RecognitionSummary;
  threshold: ThresholdState;
  confusion: ConfusionSnapshot;
  tinyMl: TinyMlTwoTrackCorrection;
  targetPresetId: string;
}

export interface TwoTrackCorrectionInput {
  summary: RecognitionSummary;
  contrast: DatacardTinyMlContrastDecision | undefined;
  threshold: ThresholdState;
  confusion: ConfusionSnapshot;
  targetPresetId: string;
}

export function summarizeTwoTrackRecognition(result: DatacardRecognitionResult): RecognitionSummary {
  const selected = result.selectedCandidate;
  const contrast = result.contrast;

  return {
    selectedCandidateId: result.selectedPresetId,
    finalCandidateId: contrast?.finalCandidateId ?? selected.id,
    finalStatus: contrast?.finalStatus ?? selected.status,
    score: roundMetric(selected.score),
    shadowConfidence: contrast?.shadow.confidence ?? roundMetric(selected.score),
    meaningConfidence: contrast?.meaning.confidence ?? roundMetric(selected.score),
    unsafeRisk: contrast?.shadow.unsafeRisk ?? 0,
    flipRisk: contrast?.shadow.flipRisk ?? 0,
    topCandidates: result.candidates.slice(0, 5).map((candidate) => ({
      id: candidate.id,
      label: candidate.label,
      score: roundMetric(candidate.score),
      status: candidate.status
    }))
  };
}

export function resolveTwoTrackConfusion(
  summary: RecognitionSummary,
  targetPresetId: DatacardShapeId | string
): ConfusionSnapshot {
  const top = summary.topCandidates[0];
  const second = summary.topCandidates[1];
  const targetIndex = summary.topCandidates.findIndex((candidate) => candidate.id === targetPresetId);
  const topGap = top && second ? roundMetric(top.score - second.score) : top ? roundMetric(top.score) : 0;
  const targetRank = targetIndex >= 0 ? targetIndex + 1 : null;
  const confusedWith = top?.id === targetPresetId ? second?.id ?? "none" : top?.id ?? "none";
  const rankPenalty = targetRank === 1 ? 0 : targetRank === null ? 0.72 : Math.min(0.64, targetRank * 0.12);
  const gapPenalty = clamp(0.08 - Math.max(0, topGap), 0, 0.08) / 0.08;

  return {
    targetRank,
    topPair: `${shortTargetId(top?.id ?? "none")} vs ${shortTargetId(second?.id ?? "none")}`,
    topGap,
    targetInTop5: targetRank !== null,
    confusedWith,
    confusionScore: roundMetric(clamp(rankPenalty * 0.72 + gapPenalty * 0.28, 0, 1))
  };
}

export function calculateTutorialThresholdState(input: TwoTrackThresholdInput): ThresholdState {
  const captures = input.captures;
  const evals = input.evals;
  const captureCount = captures.length;
  const globalMaturity = clamp(captureCount / 12, 0, 1);
  const captureScores = captures.map((capture) => capture.recognition.score);
  const captureUnsafe = captures.map((capture) => capture.recognition.unsafeRisk);
  const captureFlip = captures.map((capture) => capture.recognition.flipRisk);
  const avgScore = average(captureScores) || 0;
  const avgUnsafe = average(captureUnsafe) || 0;
  const avgFlip = average(captureFlip) || 0;
  const globalConfusion = average([...captures, ...evals].map((entry) => entry.confusion.confusionScore)) || 0;
  const globalScoreLift = clamp(
    globalMaturity * 0.08 + Math.max(0, avgScore - 0.64) * 0.16 - avgUnsafe * 0.05 - avgFlip * 0.03,
    0,
    0.12
  );
  const baseAccept = 0.76;
  const acceptThreshold = clamp(baseAccept - globalScoreLift + globalConfusion * 0.04, 0.58, 0.82);
  const holdThreshold = clamp(acceptThreshold - 0.13, 0.45, 0.7);
  const unsafeLimit = clamp(0.24 + globalMaturity * 0.09 - globalConfusion * 0.03, 0.2, 0.36);
  const flipLimit = clamp(0.42 + globalMaturity * 0.08, 0.36, 0.56);

  return {
    captureCount,
    globalMaturity: roundMetric(globalMaturity),
    globalScoreLift: roundMetric(globalScoreLift),
    acceptThreshold: roundMetric(acceptThreshold),
    holdThreshold: roundMetric(holdThreshold),
    unsafeLimit: roundMetric(unsafeLimit),
    flipLimit: roundMetric(flipLimit),
    targetRankLimit: 5,
    topGapFloor: roundMetric(clamp(0.04 - globalMaturity * 0.018, 0.018, 0.05)),
    targetAdjustments: buildTargetAdjustments(input, acceptThreshold)
  };
}

export function calculateTinyMlTwoTrackCorrection(input: TwoTrackCorrectionInput): TinyMlTwoTrackCorrection {
  const { summary, contrast, threshold, confusion, targetPresetId } = input;
  const targetState = threshold.targetAdjustments[targetPresetId] ?? createDefaultTargetThresholdState();
  const targetMaturity = clamp(targetState.captureCount / 3, 0, 1);
  const combinedMaturity = clamp((threshold.globalMaturity + targetMaturity) / 2, 0, 1);
  const targetThreshold = targetState.acceptThreshold || threshold.acceptThreshold;
  const relationRisk = contrast?.shadow.relationRisk ?? 0;
  const blockerPenalty = clamp((contrast?.blockedBy.length ?? 0) * 0.035, 0, 0.14);
  const riskPressure = summary.unsafeRisk * 0.18 + summary.flipRisk * 0.12 + relationRisk * 0.1;

  const shadowCorrection = roundMetric(
    combinedMaturity * 0.035 - riskPressure - confusion.confusionScore * 0.07 - blockerPenalty * 0.35
  );
  const shadowThreshold = roundMetric(clamp(targetThreshold + confusion.confusionScore * 0.05 + blockerPenalty * 0.5, 0.54, 0.9));
  const shadowScore = roundMetric(clamp(summary.shadowConfidence + shadowCorrection, 0, 1));
  const shadowTrack = createTinyMlTrackCorrection(
    "shadow_gate",
    "Shadow gate",
    shadowScore,
    shadowThreshold,
    shadowCorrection,
    threshold.holdThreshold,
    [
      `risk ${roundMetric(riskPressure).toFixed(3)}`,
      `blockers ${contrast?.blockedBy.length ?? 0}`,
      `confusion ${confusion.confusionScore.toFixed(3)}`
    ]
  );

  const meaningLift = contrast?.meaning.actualScoreLift ?? 0;
  const eligibleLift = contrast?.meaning.eligibleForActual ? 0.025 : -0.015;
  const meaningCorrection = roundMetric(
    threshold.globalScoreLift * 0.85 +
      targetMaturity * 0.04 +
      meaningLift * 0.5 +
      eligibleLift -
      confusion.confusionScore * 0.08 -
      blockerPenalty * 0.45
  );
  const meaningThreshold = roundMetric(clamp(targetThreshold - combinedMaturity * 0.045 + confusion.confusionScore * 0.035, 0.5, 0.86));
  const meaningScore = roundMetric(clamp(summary.meaningConfidence + meaningCorrection, 0, 1));
  const meaningTrack = createTinyMlTrackCorrection(
    "meaning_recovery",
    "Meaning recovery",
    meaningScore,
    meaningThreshold,
    meaningCorrection,
    threshold.holdThreshold,
    [
      `capture lift ${threshold.globalScoreLift.toFixed(3)}`,
      `target maturity ${targetMaturity.toFixed(3)}`,
      contrast?.meaning.eligibleForActual ? "eligible" : "holdout"
    ]
  );

  const delta = roundMetric(meaningTrack.adjustedScore - shadowTrack.adjustedScore);
  const riskBlocked = summary.unsafeRisk > threshold.unsafeLimit || summary.flipRisk > threshold.flipLimit;
  const trackDisagrees = shadowTrack.decision !== meaningTrack.decision;
  const promotePriority = meaningTrack.decision === "accept" && shadowTrack.decision !== "accept" && !riskBlocked;
  const blockPriorityFlip = shadowTrack.decision === "retry" || riskBlocked || (trackDisagrees && summary.unsafeRisk > threshold.unsafeLimit * 0.92);
  let finalDecision: DynamicDecision = "hold";
  let selectedTrack: TinyMlTrackId = "balanced";
  let finalReason = "tracks disagree; hold for more tutorial capture";

  if (shadowTrack.decision === meaningTrack.decision) {
    finalDecision = shadowTrack.decision;
    selectedTrack = "balanced";
    finalReason = `both tracks ${finalDecision}`;
  } else if (blockPriorityFlip) {
    finalDecision = shadowTrack.decision === "accept" ? "hold" : "retry";
    selectedTrack = "shadow_gate";
    finalReason = "shadow gate blocks unsafe or low-evidence priority flip";
  } else if (promotePriority) {
    finalDecision = "accept";
    selectedTrack = "meaning_recovery";
    finalReason = "meaning recovery raises priority after tutorial capture";
  } else if (shadowTrack.decision === "accept" && meaningTrack.decision === "hold") {
    finalDecision = "hold";
    selectedTrack = "balanced";
    finalReason = "meaning track requests additional capture before accept";
  } else if (meaningTrack.decision === "accept") {
    finalDecision = "hold";
    selectedTrack = "meaning_recovery";
    finalReason = "meaning track suggests recovery but shadow track is not stable";
  }

  return {
    version: "tinyml-two-track-v1",
    shadowTrack,
    meaningTrack,
    agreement: tinyMlAgreement(shadowTrack.decision, meaningTrack.decision),
    delta,
    selectedTrack,
    finalDecision,
    finalReason,
    promotePriority,
    blockPriorityFlip
  };
}

export function decideWithTwoTrackPersonalization(input: TwoTrackDecisionInput): {
  decision: DynamicDecision;
  reason: string;
} {
  const { summary, threshold, confusion, tinyMl, targetPresetId } = input;
  const targetState = threshold.targetAdjustments[targetPresetId] ?? createDefaultTargetThresholdState();
  const acceptThreshold = targetState.acceptThreshold || threshold.acceptThreshold;
  const targetRankOk = confusion.targetRank !== null && confusion.targetRank <= threshold.targetRankLimit;
  const riskOk = summary.unsafeRisk <= threshold.unsafeLimit && summary.flipRisk <= threshold.flipLimit;
  const scoreOk = summary.score >= acceptThreshold;
  const topGapOk = confusion.targetRank === 1 || confusion.topGap <= threshold.topGapFloor || targetState.captureCount >= 2;
  let baselineDecision: DynamicDecision = "retry";
  let baselineReason = `target outside top-${threshold.targetRankLimit} or below hold threshold ${threshold.holdThreshold.toFixed(3)}`;

  if (targetRankOk && riskOk && scoreOk && topGapOk) {
    baselineDecision = "accept";
    baselineReason = `score ${summary.score.toFixed(3)} >= target threshold ${acceptThreshold.toFixed(3)}`;
  } else if (targetRankOk || summary.score >= threshold.holdThreshold) {
    baselineDecision = "hold";
    baselineReason = `capture/eval more: rank ${confusion.targetRank ?? "-"}, score ${summary.score.toFixed(3)}, threshold ${acceptThreshold.toFixed(3)}`;
  }

  if (baselineDecision === "accept" && tinyMl.finalDecision === "accept") {
    return {
      decision: "accept",
      reason: `${baselineReason}; tinyML two-track agrees (${tinyMl.selectedTrack})`
    };
  }

  if (baselineDecision === "accept" && tinyMl.finalDecision !== "accept") {
    return {
      decision: tinyMl.finalDecision,
      reason: `tinyML contrast gates baseline accept: ${tinyMl.finalReason}`
    };
  }

  if (baselineDecision === "hold" && tinyMl.finalDecision === "accept" && targetRankOk && summary.score >= threshold.holdThreshold) {
    return {
      decision: "accept",
      reason: `tinyML meaning track promotes hold: ${tinyMl.finalReason}`
    };
  }

  if (baselineDecision === "retry" && tinyMl.finalDecision === "accept" && targetRankOk) {
    return {
      decision: "hold",
      reason: `tinyML suggests recovery, but threshold baseline remains retry: ${tinyMl.finalReason}`
    };
  }

  if (tinyMl.finalDecision === "retry" && tinyMl.blockPriorityFlip) {
    return {
      decision: "retry",
      reason: `tinyML shadow gate blocks priority flip: ${tinyMl.finalReason}`
    };
  }

  return { decision: baselineDecision, reason: `${baselineReason}; tinyML ${tinyMl.finalDecision}` };
}

export function calculateTwoTrackAggregate(captures: readonly TwoTrackHistoryEntry[], evals: readonly TwoTrackHistoryEntry[]) {
  const accepted = evals.filter((row) => row.dynamicDecision === "accept");
  const top1 = evals.filter((row) => row.confusion.targetRank === 1);
  const tinyMlRows = evals.map((row) => row.tinyMlCorrection).filter((row): row is TinyMlTwoTrackCorrection => Boolean(row));
  const tinyMlPromotes = tinyMlRows.filter((row) => row.promotePriority);
  const tinyMlBlocks = tinyMlRows.filter((row) => row.blockPriorityFlip);

  return {
    acceptRate: roundMetric(evals.length === 0 ? 0 : accepted.length / evals.length),
    top1Rate: roundMetric(evals.length === 0 ? 0 : top1.length / evals.length),
    avgUnsafeRisk: roundMetric(average(evals.map((row) => row.recognition.unsafeRisk)) || 0),
    avgConfusion: roundMetric(average([...captures, ...evals].map((row) => row.confusion.confusionScore)) || 0),
    tinyMlPromoteRate: roundMetric(evals.length === 0 ? 0 : tinyMlPromotes.length / evals.length),
    tinyMlBlockRate: roundMetric(evals.length === 0 ? 0 : tinyMlBlocks.length / evals.length)
  };
}

export function calculateTinyMlTwoTrackSessionState(rows: readonly TwoTrackHistoryEntry[]) {
  const corrections = rows
    .map((row) => row.tinyMlCorrection)
    .filter((row): row is TinyMlTwoTrackCorrection => Boolean(row));

  return {
    correctionCount: corrections.length,
    promoteCount: corrections.filter((row) => row.promotePriority).length,
    shadowBlockCount: corrections.filter((row) => row.blockPriorityFlip).length,
    avgDelta: roundMetric(average(corrections.map((row) => row.delta)) || 0),
    lastFinalDecision: corrections[corrections.length - 1]?.finalDecision ?? "none"
  };
}

export function createDefaultTargetThresholdState(): TargetThresholdState {
  return {
    captureCount: 0,
    evalCount: 0,
    top1Rate: 0,
    confusionScore: 0,
    acceptThreshold: 0.76
  };
}

function buildTargetAdjustments(
  input: TwoTrackThresholdInput,
  globalAcceptThreshold: number
): Record<string, TargetThresholdState> {
  const result: Record<string, TargetThresholdState> = {};

  for (const targetId of input.targetPresetIds) {
    const targetCaptures = input.captures.filter((capture) => capture.targetPresetId === targetId);
    const targetEvals = input.evals.filter((trial) => trial.targetPresetId === targetId);
    const rows = [...targetCaptures, ...targetEvals];
    const top1Rate = rows.length === 0 ? 0 : rows.filter((row) => row.confusion.targetRank === 1).length / rows.length;
    const confusionScore = average(rows.map((row) => row.confusion.confusionScore)) || 0;
    const targetMaturity = clamp(targetCaptures.length / 3, 0, 1);
    const acceptThreshold = clamp(globalAcceptThreshold - targetMaturity * 0.03 + confusionScore * 0.08, 0.56, 0.86);

    result[targetId] = {
      captureCount: targetCaptures.length,
      evalCount: targetEvals.length,
      top1Rate: roundMetric(top1Rate),
      confusionScore: roundMetric(confusionScore),
      acceptThreshold: roundMetric(acceptThreshold)
    };
  }

  return result;
}

function createTinyMlTrackCorrection(
  track: Exclude<TinyMlTrackId, "balanced">,
  label: string,
  adjustedScore: number,
  threshold: number,
  correction: number,
  globalHoldThreshold: number,
  reasons: string[]
): TinyMlTrackCorrection {
  const holdFloor = clamp(Math.min(globalHoldThreshold, threshold - 0.1), 0.42, threshold);
  const decision = adjustedScore >= threshold ? "accept" : adjustedScore >= holdFloor ? "hold" : "retry";

  return {
    track,
    label,
    adjustedScore,
    threshold,
    margin: roundMetric(adjustedScore - threshold),
    decision,
    correction,
    reasons
  };
}

function tinyMlAgreement(
  shadowDecision: DynamicDecision,
  meaningDecision: DynamicDecision
): TinyMlTwoTrackCorrection["agreement"] {
  if (shadowDecision !== meaningDecision) return "contrast";
  return shadowDecision === "accept" ? "agree_accept" : shadowDecision === "hold" ? "agree_hold" : "agree_retry";
}

function shortTargetId(value: string): string {
  return value.replace("custom:eval_", "");
}

function average(values: readonly number[]): number {
  if (values.length === 0) return 0;
  return values.reduce((sum, value) => sum + value, 0) / values.length;
}

function clamp(value: number, min: number, max: number): number {
  return Math.max(min, Math.min(max, value));
}

function roundMetric(value: number): number {
  return Number(value.toFixed(4));
}
