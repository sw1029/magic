import type { TutorialCaptureReliability } from "./types";

export type TutorialPersonalizationPolicyMode = "actual" | "shadow" | "metadata";
export type TutorialArtifactCompatibilityStatus = "compatible" | "missing" | "mismatch" | "unknown";

export interface TutorialCardSetSignature {
  cardSetId?: string;
  cardSetHash?: string;
  cardSignature?: string;
}

export interface TutorialArtifactCompatibilityInput {
  status: TutorialArtifactCompatibilityStatus;
  artifactVersion?: string;
  expectedVersion?: string;
  reason?: string;
}

export type TutorialArtifactCompatibility =
  | TutorialArtifactCompatibilityStatus
  | TutorialArtifactCompatibilityInput;

export type TutorialThresholdBiasPolicyReasonCode =
  | "store_version_supported"
  | "store_version_unsupported"
  | "captures_missing"
  | "captures_feedback_only"
  | "captures_need_validation"
  | "captures_validated_healthy"
  | "card_set_match"
  | "card_set_mismatch"
  | "card_set_unchecked"
  | "legacy_card_metadata_missing"
  | "artifact_compatible"
  | "artifact_missing"
  | "artifact_mismatch"
  | "artifact_unchecked"
  | "shadow_or_metadata_only"
  | "needs_backfill";

export interface TutorialThresholdBiasPolicyReason {
  code: TutorialThresholdBiasPolicyReasonCode;
  label: string;
  severity: "ok" | "info" | "warning" | "blocker";
  detail?: string;
}

export interface TutorialThresholdBiasPolicyInput extends TutorialCardSetSignature {
  storeVersion?: string;
  currentCardSignature?: TutorialCardSetSignature;
  captureReliabilities?: TutorialCaptureReliability[];
  totalCaptureCount?: number;
  validatedCaptureCount?: number;
  feedbackOnlyCaptureCount?: number;
  artifactCompatibility?: TutorialArtifactCompatibility;
  mode?: TutorialPersonalizationPolicyMode;
}

export interface TutorialThresholdBiasPolicyDecision {
  mode: TutorialPersonalizationPolicyMode;
  thresholdBiasMultiplier: 0 | 1;
  actualThresholdBiasMultiplier: 0 | 1;
  canApplyThresholdBias: boolean;
  canUseForShadow: boolean;
  canUseForExplanation: boolean;
  requestedModeAllowed: boolean;
  needsBackfill: boolean;
  reasonCodes: TutorialThresholdBiasPolicyReasonCode[];
  blockingReasonCodes: TutorialThresholdBiasPolicyReasonCode[];
  reasons: TutorialThresholdBiasPolicyReason[];
}

const SUPPORTED_STORE_VERSIONS = new Set(["v1.5"]);

const REASON_LABELS: Record<TutorialThresholdBiasPolicyReasonCode, string> = {
  store_version_supported: "프로필 형식 확인됨",
  store_version_unsupported: "지원하지 않는 프로필 형식",
  captures_missing: "연습 기록 없음",
  captures_feedback_only: "피드백 전용 기록만 있음",
  captures_need_validation: "검증된 연습 기록 필요",
  captures_validated_healthy: "검증된 연습 기록 확인됨",
  card_set_match: "카드 세트 일치",
  card_set_mismatch: "다른 카드 세트의 연습 기록",
  card_set_unchecked: "카드 세트 확인 전",
  legacy_card_metadata_missing: "이전 형식의 카드 정보",
  artifact_compatible: "모델 호환성 확인됨",
  artifact_missing: "모델 파일 확인 필요",
  artifact_mismatch: "모델 버전이 다름",
  artifact_unchecked: "모델 호환성 확인 전",
  shadow_or_metadata_only: "참고 표시만 가능",
  needs_backfill: "카드 정보 보강 필요"
};

export function evaluateTutorialThresholdBiasPolicy(
  input: TutorialThresholdBiasPolicyInput
): TutorialThresholdBiasPolicyDecision {
  const mode = input.mode ?? "actual";
  const reasons: TutorialThresholdBiasPolicyReason[] = [];
  const blockers = new Set<TutorialThresholdBiasPolicyReasonCode>();
  let needsBackfill = false;

  const addReason = (
    code: TutorialThresholdBiasPolicyReasonCode,
    severity: TutorialThresholdBiasPolicyReason["severity"],
    detail?: string
  ) => {
    reasons.push({ code, label: REASON_LABELS[code], severity, detail });
    if (severity === "blocker") {
      blockers.add(code);
    }
  };

  if (input.storeVersion && !SUPPORTED_STORE_VERSIONS.has(input.storeVersion)) {
    addReason("store_version_unsupported", "blocker", input.storeVersion);
  } else if (input.storeVersion) {
    addReason("store_version_supported", "ok", input.storeVersion);
  }

  const captureCounts = resolveCaptureCounts(input);
  if (captureCounts.total <= 0) {
    addReason("captures_missing", "blocker");
  } else if (captureCounts.feedbackOnly >= captureCounts.total) {
    addReason("captures_feedback_only", "blocker");
  } else if (captureCounts.validated <= 0) {
    addReason("captures_need_validation", "blocker");
  } else {
    addReason("captures_validated_healthy", "ok", `${captureCounts.validated}/${captureCounts.total}`);
  }

  const cardStatus = compareCardSetSignature(input, input.currentCardSignature);
  switch (cardStatus) {
    case "match":
      addReason("card_set_match", "ok");
      break;
    case "mismatch":
      addReason("card_set_mismatch", "blocker");
      break;
    case "legacy_missing":
      needsBackfill = true;
      addReason("legacy_card_metadata_missing", "warning");
      addReason("needs_backfill", "warning");
      break;
    case "unchecked":
      addReason("card_set_unchecked", "info");
      break;
  }

  const artifact = normalizeArtifactCompatibility(input.artifactCompatibility);
  switch (artifact.status) {
    case "compatible":
      addReason("artifact_compatible", "ok", artifactDetail(artifact));
      break;
    case "missing":
      addReason("artifact_missing", "blocker", artifactDetail(artifact));
      break;
    case "mismatch":
      addReason("artifact_mismatch", "blocker", artifactDetail(artifact));
      break;
    case "unknown":
      addReason("artifact_unchecked", "info", artifactDetail(artifact));
      break;
  }

  const hasBlockingMismatch = blockers.has("card_set_mismatch") || blockers.has("store_version_unsupported");
  const hasUsableCapture = captureCounts.total > 0 && captureCounts.feedbackOnly < captureCounts.total && captureCounts.validated > 0;
  const actualBlockedByLegacy = cardStatus === "legacy_missing";
  const canApplyThresholdBias = blockers.size === 0 && !actualBlockedByLegacy;
  const actualThresholdBiasMultiplier: 0 | 1 = canApplyThresholdBias ? 1 : 0;
  const canUseForShadow = hasUsableCapture && !hasBlockingMismatch;
  const canUseForExplanation = captureCounts.total > 0 && !hasBlockingMismatch;
  const requestedModeAllowed =
    mode === "actual" ? canApplyThresholdBias : mode === "shadow" ? canUseForShadow : canUseForExplanation;

  if (!canApplyThresholdBias && requestedModeAllowed) {
    addReason("shadow_or_metadata_only", "info");
  }

  return {
    mode,
    thresholdBiasMultiplier: actualThresholdBiasMultiplier,
    actualThresholdBiasMultiplier,
    canApplyThresholdBias,
    canUseForShadow,
    canUseForExplanation,
    requestedModeAllowed,
    needsBackfill,
    reasonCodes: uniqueReasons(reasons),
    blockingReasonCodes: [...blockers],
    reasons
  };
}

export function canTutorialProfileContributeThresholdBias(input: TutorialThresholdBiasPolicyInput): boolean {
  return evaluateTutorialThresholdBiasPolicy(input).canApplyThresholdBias;
}

export function resolveTutorialThresholdBiasMultiplier(input: TutorialThresholdBiasPolicyInput): 0 | 1 {
  return evaluateTutorialThresholdBiasPolicy(input).thresholdBiasMultiplier;
}

export function getTutorialThresholdBiasReasonLabel(code: TutorialThresholdBiasPolicyReasonCode): string {
  return REASON_LABELS[code];
}

function resolveCaptureCounts(input: TutorialThresholdBiasPolicyInput): {
  total: number;
  validated: number;
  feedbackOnly: number;
} {
  const reliabilities = input.captureReliabilities ?? [];
  const total = Math.max(input.totalCaptureCount ?? reliabilities.length, 0);
  const validatedFromReliabilities = reliabilities.filter(isValidatedReliability).length;
  const feedbackOnlyFromReliabilities = reliabilities.filter((reliability) => reliability === "feedback_only").length;

  return {
    total,
    validated: Math.max(input.validatedCaptureCount ?? validatedFromReliabilities, 0),
    feedbackOnly: Math.max(input.feedbackOnlyCaptureCount ?? feedbackOnlyFromReliabilities, 0)
  };
}

function isValidatedReliability(reliability: TutorialCaptureReliability): boolean {
  return reliability === "high" || reliability === "medium";
}

function compareCardSetSignature(
  storeSignature: TutorialCardSetSignature,
  currentSignature: TutorialCardSetSignature | undefined
): "match" | "mismatch" | "legacy_missing" | "unchecked" {
  if (!hasAnySignatureValue(currentSignature)) {
    return "unchecked";
  }

  if (!hasAnySignatureValue(storeSignature)) {
    return "legacy_missing";
  }

  if (isDifferent(storeSignature.cardSetId, currentSignature?.cardSetId)) {
    return "mismatch";
  }

  if (isDifferent(storeSignature.cardSetHash, currentSignature?.cardSetHash)) {
    return "mismatch";
  }

  if (isDifferent(storeSignature.cardSignature, currentSignature?.cardSignature)) {
    return "mismatch";
  }

  if (
    isSame(storeSignature.cardSetId, currentSignature?.cardSetId) ||
    isSame(storeSignature.cardSetHash, currentSignature?.cardSetHash) ||
    isSame(storeSignature.cardSignature, currentSignature?.cardSignature)
  ) {
    return "match";
  }

  return "unchecked";
}

function normalizeArtifactCompatibility(
  artifact: TutorialArtifactCompatibility | undefined
): TutorialArtifactCompatibilityInput {
  if (!artifact) {
    return { status: "unknown" };
  }

  if (typeof artifact === "string") {
    return { status: artifact };
  }

  return artifact;
}

function hasAnySignatureValue(signature: TutorialCardSetSignature | undefined): boolean {
  return Boolean(signature?.cardSetId || signature?.cardSetHash || signature?.cardSignature);
}

function isDifferent(left: string | undefined, right: string | undefined): boolean {
  return left !== undefined && right !== undefined && left !== right;
}

function isSame(left: string | undefined, right: string | undefined): boolean {
  return left !== undefined && right !== undefined && left === right;
}

function artifactDetail(artifact: TutorialArtifactCompatibilityInput): string | undefined {
  return artifact.reason ?? artifact.artifactVersion ?? artifact.expectedVersion;
}

function uniqueReasons(reasons: TutorialThresholdBiasPolicyReason[]): TutorialThresholdBiasPolicyReasonCode[] {
  return [...new Set(reasons.map((reason) => reason.code))];
}
