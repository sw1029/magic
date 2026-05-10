import { boundingBox, centroid, distance } from "./geometry";
import { recognizeSession } from "./recognize";
import type { SealDetection, SealedBaseRecognitionResult, Stroke, StrokeBounds, StrokeSession, UserInputProfile } from "./types";

const MIN_SEAL_POINTS = 12;
const MIN_SEAL_DIAGONAL = 36;
const MIN_CLOSURE_SCORE = 0.72;
const MIN_CIRCULARITY_SCORE = 0.62;

interface SealCandidateScore {
  detection: SealDetection;
  baseSession: StrokeSession;
}

export function recognizeSealedBaseSession(
  session: StrokeSession,
  options: { profile?: UserInputProfile } = {}
): SealedBaseRecognitionResult {
  const candidate = resolveBestSealCandidate(session);
  const baseSession = candidate?.baseSession ?? cloneSession(session);
  const ringDetected = Boolean(candidate?.detection.ringDetected);
  const result = recognizeSession(baseSession, { sealed: ringDetected, profile: options.profile });

  if (!candidate) {
    return {
      result,
      baseSession,
      sealDetection: buildEmptyDetection(session, "기본 도형을 감싸는 원형 seal stroke가 아직 없습니다.")
    };
  }

  if (!result.canonicalFamily) {
    return {
      result,
      baseSession,
      sealDetection: {
        ...candidate.detection,
        ok: false,
        reason: candidate.detection.ringDetected
          ? result.invalidReason ?? "seal 원은 감지됐지만 내부 기본 도형을 아직 확정하지 못했습니다."
          : candidate.detection.reason
      }
    };
  }

  return {
    result,
    baseSession,
    sealDetection: {
      ...candidate.detection,
      ok: true,
      reason: "기본 도형을 감싸는 원형 seal stroke가 감지되어 자동으로 seal 처리했습니다."
    }
  };
}

export function detectEnclosingSeal(session: StrokeSession): SealDetection {
  return (
    resolveBestSealCandidate(session)?.detection ??
    buildEmptyDetection(session, "기본 도형을 감싸는 원형 seal stroke가 아직 없습니다.")
  );
}

function resolveBestSealCandidate(session: StrokeSession): SealCandidateScore | null {
  const strokes = session.strokes.filter((stroke) => stroke.points.length >= 2);

  if (strokes.length < 2) {
    return null;
  }

  let bestFailed: SealDetection | null = null;
  const maxCandidateStrokeCount = Math.min(3, strokes.length - 1);

  for (let groupSize = 1; groupSize <= maxCandidateStrokeCount; groupSize += 1) {
    const startIndex = strokes.length - groupSize;
    const ringStrokes = strokes.slice(startIndex);
    const baseStrokes = strokes.slice(0, startIndex);
    const basePoints = baseStrokes.flatMap((item) => item.points);

    if (basePoints.length < 2) {
      continue;
    }

    const geometry = scoreSealStrokeGroup(ringStrokes);
    const baseBounds = boundingBox(basePoints);
    const enclosureMargin = measureEnclosureMargin(geometry.bounds, baseBounds);
    const requiredMargin = Math.max(8, Math.hypot(baseBounds.width, baseBounds.height) * 0.04);
    const centerAlignment = measureCenterAlignment(geometry.center, baseBounds);
    const falsePositiveRisk = measureFalsePositiveRisk({
      closure: geometry.closure,
      circularity: geometry.circularity,
      enclosureMargin,
      requiredMargin,
      centerAlignment,
      ringCoverage: geometry.ringCoverage
    });
    const baseSession = cloneSession({ ...session, strokes: baseStrokes });
    const detection: SealDetection = {
      ok: false,
      ringDetected: false,
      strokeId: ringStrokes[0]?.id,
      strokeIndex: startIndex,
      candidateStrokeIds: ringStrokes.map((item) => item.id),
      baseStrokeCount: baseStrokes.length,
      closure: geometry.closure,
      circularity: geometry.circularity,
      enclosureMargin,
      centerAlignment,
      ringCoverage: geometry.ringCoverage,
      multiStroke: ringStrokes.length > 1,
      falsePositiveRisk,
      reason: ""
    };

    if (geometry.pointCount < MIN_SEAL_POINTS) {
      bestFailed = preferMoreUsefulFailure(bestFailed, {
        ...detection,
        reason: "seal stroke는 충분한 점을 가진 닫힌 원형이어야 합니다."
      });
      continue;
    }

    if (geometry.diagonal < MIN_SEAL_DIAGONAL) {
      bestFailed = preferMoreUsefulFailure(bestFailed, {
        ...detection,
        reason: "seal 원이 너무 작아 기본 도형을 감싸지 못했습니다."
      });
      continue;
    }

    if (!geometry.multiStroke && geometry.closure < MIN_CLOSURE_SCORE) {
      bestFailed = preferMoreUsefulFailure(bestFailed, {
        ...detection,
        reason: "seal 원의 시작점과 끝점이 충분히 닫히지 않았습니다."
      });
      continue;
    }

    if (geometry.multiStroke && geometry.ringCoverage < 0.82) {
      bestFailed = preferMoreUsefulFailure(bestFailed, {
        ...detection,
        reason: "seal 테두리를 여러 선으로 그린 경우 전체 원 둘레를 충분히 감싸야 합니다."
      });
      continue;
    }

    if (geometry.circularity < MIN_CIRCULARITY_SCORE) {
      bestFailed = preferMoreUsefulFailure(bestFailed, {
        ...detection,
        reason: "seal stroke가 원형으로 충분히 안정적이지 않습니다."
      });
      continue;
    }

    if (enclosureMargin < requiredMargin) {
      bestFailed = preferMoreUsefulFailure(bestFailed, {
        ...detection,
        reason: "seal 원이 기본 도형 전체를 충분한 여백으로 감싸지 못했습니다."
      });
      continue;
    }

    if (centerAlignment < 0.45) {
      bestFailed = preferMoreUsefulFailure(bestFailed, {
        ...detection,
        reason: "seal 원의 중심이 기본 도형과 너무 어긋나 있습니다."
      });
      continue;
    }

    return {
      baseSession,
      detection: {
        ...detection,
        ringDetected: true,
        reason: "원형 seal stroke가 기본 도형을 감싸고 있습니다."
      }
    };
  }

  return bestFailed
    ? { detection: bestFailed, baseSession: cloneSession(session) }
    : null;
}

function scoreSealStrokeGroup(strokes: readonly Stroke[]): {
  bounds: StrokeBounds;
  diagonal: number;
  closure: number;
  circularity: number;
  center: { x: number; y: number };
  pointCount: number;
  ringCoverage: number;
  multiStroke: boolean;
} {
  const points = strokes.flatMap((stroke) => stroke.points);
  const bounds = boundingBox(points);
  const diagonal = Math.hypot(bounds.width, bounds.height);
  const first = points[0];
  const last = points[points.length - 1];
  const closureGap = distance(first, last);
  const closure = clamp(1 - closureGap / Math.max(diagonal * 0.22, 1), 0, 1);
  const center = centroid(points);
  const radii = points.map((point) => distance(point, center));
  const meanRadius = radii.reduce((sum, value) => sum + value, 0) / Math.max(radii.length, 1);
  const variance = radii.reduce((sum, value) => sum + (value - meanRadius) ** 2, 0) / Math.max(radii.length, 1);
  const radialScore = clamp(1 - Math.sqrt(variance) / Math.max(meanRadius * 0.38, 1), 0, 1);
  const aspectScore = clamp(1 - Math.abs(bounds.width - bounds.height) / Math.max(bounds.width, bounds.height, 1), 0, 1);
  const circularity = clamp(radialScore * 0.72 + aspectScore * 0.28, 0, 1);
  const ringCoverage = measureRingCoverage(points, center);

  return {
    bounds,
    diagonal,
    closure,
    circularity,
    center,
    pointCount: points.length,
    ringCoverage,
    multiStroke: strokes.length > 1
  };
}

function measureEnclosureMargin(ringBounds: StrokeBounds, baseBounds: StrokeBounds): number {
  return Math.min(
    baseBounds.minX - ringBounds.minX,
    ringBounds.maxX - baseBounds.maxX,
    baseBounds.minY - ringBounds.minY,
    ringBounds.maxY - baseBounds.maxY
  );
}

function measureCenterAlignment(ringCenter: { x: number; y: number }, baseBounds: StrokeBounds): number {
  const baseCenter = {
    x: (baseBounds.minX + baseBounds.maxX) / 2,
    y: (baseBounds.minY + baseBounds.maxY) / 2
  };
  const baseDiagonal = Math.max(Math.hypot(baseBounds.width, baseBounds.height), 1);

  return clamp(1 - distance(ringCenter, baseCenter) / Math.max(baseDiagonal * 0.48, 1), 0, 1);
}

function measureRingCoverage(points: readonly Stroke["points"][number][], center: { x: number; y: number }): number {
  if (points.length < 3) {
    return 0;
  }

  const angles = points
    .map((point) => Math.atan2(point.y - center.y, point.x - center.x))
    .sort((left, right) => left - right);
  let maxGap = 0;

  for (let index = 0; index < angles.length; index += 1) {
    const current = angles[index];
    const next = index === angles.length - 1 ? angles[0] + Math.PI * 2 : angles[index + 1];
    maxGap = Math.max(maxGap, next - current);
  }

  return clamp(1 - maxGap / (Math.PI * 2), 0, 1);
}

function measureFalsePositiveRisk(values: {
  closure: number;
  circularity: number;
  enclosureMargin: number;
  requiredMargin: number;
  centerAlignment: number;
  ringCoverage: number;
}): number {
  const marginScore = clamp(values.enclosureMargin / Math.max(values.requiredMargin, 1), 0, 1);
  const confidence = Math.min(
    values.closure,
    values.circularity,
    marginScore,
    values.centerAlignment,
    values.ringCoverage
  );

  return roundMetric(1 - confidence);
}

function preferMoreUsefulFailure(left: SealDetection | null, right: SealDetection): SealDetection {
  if (!left) {
    return right;
  }

  const leftScore = left.closure + left.circularity + Math.max(left.enclosureMargin, 0) / 100;
  const rightScore = right.closure + right.circularity + Math.max(right.enclosureMargin, 0) / 100;
  return rightScore >= leftScore ? right : left;
}

function buildEmptyDetection(session: StrokeSession, reason: string): SealDetection {
  return {
    ok: false,
    ringDetected: false,
    baseStrokeCount: session.strokes.filter((stroke) => stroke.points.length >= 2).length,
    closure: 0,
    circularity: 0,
    enclosureMargin: 0,
    reason
  };
}

function cloneSession(session: StrokeSession): StrokeSession {
  return {
    startedAt: session.startedAt,
    endedAt: session.endedAt,
    strokes: session.strokes.map((stroke) => ({
      id: stroke.id,
      points: stroke.points.map((point) => ({ ...point }))
    }))
  };
}

function clamp(value: number, minimum: number, maximum: number): number {
  return Math.max(minimum, Math.min(maximum, value));
}

function roundMetric(value: number): number {
  return Number(value.toFixed(4));
}
