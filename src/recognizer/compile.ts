import type { CompiledSealResult, OverlayStrokeRecord, RecognitionResult, UserInputProfileDelta } from "./types";

export function compileSealResult(
  baseResult: RecognitionResult,
  overlayRecords: OverlayStrokeRecord[],
  profileDelta?: UserInputProfileDelta
): CompiledSealResult {
  if (!baseResult.canonicalFamily) {
    throw new Error("recognized base family is required before final compilation");
  }

  const overlayOperators = overlayRecords
    .map((record) => record.recognition)
    .filter((recognition) => recognition.status === "recognized" && recognition.operator);
  const overlaySummary =
    overlayOperators.length > 0 ? overlayOperators.map((item) => item.operator).join(" -> ") : "overlay 없음";

  return {
    phase: "final",
    baseFamily: baseResult.canonicalFamily,
    baseResult,
    overlayOperators,
    rawQuality: baseResult.rawQuality,
    adjustedQuality: baseResult.adjustedQuality,
    qualityAdjustment: baseResult.qualityAdjustment,
    profileDelta,
    compiledAt: Date.now(),
    summary: `${baseResult.canonicalFamily} + ${overlaySummary}`
  };
}
