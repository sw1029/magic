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
import { createEmptyQualityVector } from "./user-profile";
import { calculateAdjustedQuality, calculateQualityVector } from "./quality";
import { rerankBaseCandidates } from "./rerank";
import { GLYPH_TEMPLATES } from "./templates";
import type {
  AxisLine,
  GlyphFamily,
  RecognitionCandidate,
  RecognitionFeatures,
  RecognitionResult,
  StrokeSession,
  UserInputProfile
} from "./types";

const TEMPLATE_BUNDLES = GLYPH_TEMPLATES.map((template) => ({
  ...template,
  normalized: normalizeStrokes(template.strokes)
}));

export function recognizeSession(
  session: StrokeSession,
  options: { sealed: boolean; profile?: UserInputProfile }
): RecognitionResult {
  const strokes = session.strokes.filter((stroke) => stroke.points.length >= 2);

  if (strokes.length === 0) {
    return emptyResult(options.sealed, "입력이 아직 없습니다.");
  }

  const normalized = normalizeStrokes(strokes);
  const rawQuality = calculateQualityVector(strokes, normalized);
  const { adjustedQuality, qualityAdjustment } = calculateAdjustedQuality(rawQuality, options.profile);
  const features = deriveFeatures({ ...session, strokes }, normalized);
  const candidates = rerankBaseCandidates({
    candidates: TEMPLATE_BUNDLES.map((template) =>
      scoreCandidate(template.family, normalized.normalizedCloud, strokes.length, features, rawQuality)
    ).sort((left, right) => right.score - left.score),
    normalizedCloud: normalized.normalizedCloud,
    features,
    profile: options.profile
  });

  const topCandidate = candidates[0];
  const secondCandidate = candidates[1];
  const margin = topCandidate ? topCandidate.score - (secondCandidate?.score ?? 0) : 0;
  let status: RecognitionResult["status"] = "invalid";
  let invalidReason = "기준형과 충분히 가깝지 않습니다.";

  if (topCandidate && topCandidate.score >= 0.55 && topCandidate.completenessHint) {
    status = "incomplete";
    invalidReason = topCandidate.completenessHint;
  } else if (topCandidate && topCandidate.score >= 0.7 && margin >= 0.15) {
    status = "recognized";
    invalidReason = "seal 조건을 만족했습니다.";
  } else if (topCandidate && topCandidate.score >= 0.55) {
    status = "ambiguous";
    invalidReason = "여러 기준형의 점수가 가까워 최종 family를 확정하지 않습니다.";
  }

  return {
    status,
    sealed: options.sealed,
    quality: rawQuality,
    rawQuality,
    adjustedQuality,
    qualityAdjustment,
    features,
    candidates,
    topCandidate,
    canonicalFamily: options.sealed && status === "recognized" ? topCandidate.family : undefined,
    invalidReason,
    normalizedStrokes: normalized.normalizedStrokes,
    symmetryAxis: buildSymmetryAxis(normalized.rawCentroid, normalized.rawAngleRadians, normalized.diagonal),
    closureLine: buildClosureLine(strokes)
  };
}

function deriveFeatures(session: StrokeSession, normalized: ReturnType<typeof normalizeStrokes>): RecognitionFeatures {
  const dominantStroke = [...session.strokes].sort((left, right) => pathLength(right.points) - pathLength(left.points))[0];
  const dominantPoints = dominantStroke?.points ?? [];
  const bbox = boundingBox(normalized.normalizedCloud);
  const radiusSamples = normalized.normalizedCloud.map((point) => Math.hypot(point.x, point.y));
  const meanRadius =
    radiusSamples.reduce((sum, value) => sum + value, 0) / Math.max(radiusSamples.length, 1);
  const variance =
    radiusSamples.reduce((sum, value) => sum + (value - meanRadius) ** 2, 0) /
    Math.max(radiusSamples.length, 1);
  const fillRatio = calculateFillRatio(dominantPoints);

  return {
    strokeCount: session.strokes.length,
    pointCount: session.strokes.reduce((sum, stroke) => sum + stroke.points.length, 0),
    durationMs: getDurationMs(session),
    pathLength: session.strokes.reduce((sum, stroke) => sum + pathLength(stroke.points), 0),
    closureGap:
      dominantPoints.length >= 2 ? distanceBetween(dominantPoints[0], dominantPoints[dominantPoints.length - 1]) : 0,
    dominantCorners: countCorners(dominantPoints, Math.max(normalized.diagonal * 0.02, 3)),
    endpointClusters: clusterEndpointCount(session.strokes, Math.max(normalized.diagonal * 0.08, 14)),
    circularity: clamp(1 - Math.sqrt(variance) / Math.max(meanRadius, 0.0001) / 0.45, 0, 1),
    fillRatio,
    parallelism: calculateParallelism(session),
    rawAngleRadians: normalizeAngleHalfPi(normalized.rawAngleRadians)
  };
}

function scoreCandidate(
  family: GlyphFamily,
  normalizedCloud: ReturnType<typeof normalizeStrokes>["normalizedCloud"],
  strokeCount: number,
  features: RecognitionFeatures,
  quality: RecognitionResult["quality"]
): RecognitionCandidate {
  const template = TEMPLATE_BUNDLES.find((item) => item.family === family);

  if (!template) {
    throw new Error(`unknown family: ${family}`);
  }

  const templateDistance = pointCloudDistance(normalizedCloud, template.normalized.normalizedCloud);
  const templateScore = clamp(1 - templateDistance / 0.62, 0, 1);
  const notes = [`template=${templateScore.toFixed(2)}`];
  const strokeScore = rangeScore(strokeCount, template.expectedStrokeCount[0], template.expectedStrokeCount[1]);
  const openScore = clamp(1 - quality.closure, 0, 1);
  let score = templateScore;
  let completenessHint: string | undefined;

  switch (family) {
    case "wind": {
      score =
        templateScore * 0.45 +
        features.parallelism * 0.25 +
        strokeScore * 0.2 +
        openScore * 0.1;
      notes.push(`parallel=${features.parallelism.toFixed(2)}`, `open=${openScore.toFixed(2)}`);
      if (strokeCount < 3 && score >= 0.35) {
        completenessHint = "바람 문양은 3개의 평행 개방선이 필요합니다.";
      }
      break;
    }
    case "earth": {
      const cornerScore = expectedCornerScore(features.dominantCorners, 4);
      const fillScore = closenessScore(features.fillRatio, 0.68, 0.2);
      score =
        templateScore * 0.38 +
        quality.closure * 0.24 +
        cornerScore * 0.18 +
        fillScore * 0.12 +
        strokeScore * 0.08;
      notes.push(
        `closure=${quality.closure.toFixed(2)}`,
        `corners=${cornerScore.toFixed(2)}`,
        `fill=${fillScore.toFixed(2)}`
      );
      if (quality.closure < 0.82 && score >= 0.4) {
        completenessHint = "땅 문양은 폐합된 사다리꼴이어야 합니다.";
      }
      break;
    }
    case "fire": {
      const cornerScore = expectedCornerScore(features.dominantCorners, 3);
      const fillScore = closenessScore(features.fillRatio, 0.5, 0.12);
      score =
        templateScore * 0.38 +
        quality.closure * 0.24 +
        cornerScore * 0.18 +
        fillScore * 0.12 +
        strokeScore * 0.08;
      notes.push(
        `closure=${quality.closure.toFixed(2)}`,
        `corners=${cornerScore.toFixed(2)}`,
        `fill=${fillScore.toFixed(2)}`
      );
      if (quality.closure < 0.82 && score >= 0.4) {
        completenessHint = "불꽃 문양은 폐합된 삼각형이어야 합니다.";
      }
      break;
    }
    case "water": {
      score =
        templateScore * 0.45 +
        quality.closure * 0.2 +
        features.circularity * 0.2 +
        quality.smoothness * 0.15;
      notes.push(`circularity=${features.circularity.toFixed(2)}`, `smoothness=${quality.smoothness.toFixed(2)}`);
      if (quality.closure < 0.7 && score >= 0.45) {
        completenessHint = "물 문양은 단일 원형 폐합 루프여야 합니다.";
      }
      break;
    }
    case "life": {
      const branchScore = Math.max(
        expectedCornerScore(features.dominantCorners, 4),
        expectedEndpointScore(features.endpointClusters)
      );
      const fillScore = closenessScore(features.fillRatio, 0.12, 0.2);
      score =
        templateScore * 0.28 +
        branchScore * 0.3 +
        openScore * 0.18 +
        fillScore * 0.16 +
        strokeScore * 0.08;
      notes.push(
        `branch=${branchScore.toFixed(2)}`,
        `open=${openScore.toFixed(2)}`,
        `fill=${fillScore.toFixed(2)}`
      );
      if (branchScore < 0.6 && score >= 0.35) {
        completenessHint = "생명 문양은 줄기와 상단 분기가 있는 rooted Y 형태가 필요합니다.";
      }
      break;
    }
    default:
      break;
  }

  return {
    family,
    score: clamp(score, 0, 1),
    templateDistance,
    notes,
    completenessHint
  };
}

function buildSymmetryAxis(
  centroid: { x: number; y: number },
  angle: number,
  diagonal: number
): AxisLine {
  const length = Math.max(diagonal, 120);
  const dx = Math.cos(angle) * length;
  const dy = Math.sin(angle) * length;

  return {
    start: { x: centroid.x - dx, y: centroid.y - dy },
    end: { x: centroid.x + dx, y: centroid.y + dy }
  };
}

function buildClosureLine(strokes: StrokeSession["strokes"]): AxisLine | undefined {
  const dominantStroke = [...strokes].sort((left, right) => pathLength(right.points) - pathLength(left.points))[0];

  if (!dominantStroke || dominantStroke.points.length < 2) {
    return undefined;
  }

  return {
    start: {
      x: dominantStroke.points[0].x,
      y: dominantStroke.points[0].y
    },
    end: {
      x: dominantStroke.points[dominantStroke.points.length - 1].x,
      y: dominantStroke.points[dominantStroke.points.length - 1].y
    }
  };
}

function calculateParallelism(session: StrokeSession): number {
  const linearStrokes = session.strokes
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

function calculateFillRatio(points: StrokeSession["strokes"][number]["points"]): number {
  if (points.length < 3) {
    return 0;
  }

  const simplified = rdpSimplify(points, 6);
  const area = Math.abs(polygonArea(simplified));
  const box = boundingBox(points);
  const boxArea = Math.max(box.width * box.height, 1);

  return clamp(area / boxArea, 0, 1);
}

function getDurationMs(session: StrokeSession): number {
  const timestamps = session.strokes.flatMap((stroke) => stroke.points.map((point) => point.t));

  if (timestamps.length === 0) {
    return 0;
  }

  return Math.max(...timestamps) - Math.min(...timestamps);
}

function countCorners(points: StrokeSession["strokes"][number]["points"], epsilon: number): number {
  if (points.length < 2) {
    return 0;
  }

  const simplified = rdpSimplify(points, epsilon);
  return Math.max(simplified.length - 1, 0);
}

function expectedCornerScore(actual: number, expected: number): number {
  return clamp(1 - Math.abs(actual - expected) / expected, 0, 1);
}

function expectedEndpointScore(actual: number): number {
  return clamp(1 - Math.abs(actual - 3) / 3, 0, 1);
}

function closenessScore(value: number, expected: number, tolerance: number): number {
  return clamp(1 - Math.abs(value - expected) / tolerance, 0, 1);
}

function rangeScore(value: number, minimum: number, maximum: number): number {
  if (value >= minimum && value <= maximum) {
    return 1;
  }

  const distanceToRange = value < minimum ? minimum - value : value - maximum;
  return clamp(1 - distanceToRange * 0.35, 0, 1);
}

function distanceBetween(a: { x: number; y: number }, b: { x: number; y: number }): number {
  return Math.hypot(a.x - b.x, a.y - b.y);
}

function polygonArea(points: StrokeSession["strokes"][number]["points"]): number {
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

function clamp(value: number, minimum: number, maximum: number): number {
  return Math.max(minimum, Math.min(maximum, value));
}

function emptyResult(sealed: boolean, reason: string): RecognitionResult {
  const rawQuality = createEmptyQualityVector();

  return {
    status: "invalid",
    sealed,
    quality: rawQuality,
    rawQuality,
    adjustedQuality: { ...rawQuality },
    qualityAdjustment: createEmptyQualityVector(),
    features: {
      strokeCount: 0,
      pointCount: 0,
      durationMs: 0,
      pathLength: 0,
      closureGap: 0,
      dominantCorners: 0,
      endpointClusters: 0,
      circularity: 0,
      fillRatio: 0,
      parallelism: 0,
      rawAngleRadians: 0
    },
    candidates: [],
    invalidReason: reason,
    normalizedStrokes: []
  };
}
