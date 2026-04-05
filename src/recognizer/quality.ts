import {
  mirrorPointCloud,
  normalizeAngleHalfPi,
  pointCloudDistance,
  rdpSimplify,
  pathLength
} from "./geometry";
import {
  QUALITY_VECTOR_KEYS,
  createEmptyQualityVector,
  resolveUserComfortBand,
  subtractQualityVectors
} from "./user-profile";
import type { QualityVector, Stroke } from "./types";
import type { NormalizedBundle } from "./geometry";
import type { UserInputProfile } from "./types";

const QUALITY_ADJUSTMENT_WEIGHTS: QualityVector = {
  closure: 0.12,
  symmetry: 0.04,
  smoothness: 0.05,
  tempo: 0.48,
  overshoot: 0.04,
  stability: 0.34,
  rotationBias: 0.28
};

export function calculateQualityVector(strokes: Stroke[], normalized: NormalizedBundle): QualityVector {
  const totalLength = strokes.reduce((sum, stroke) => sum + pathLength(stroke.points), 0);
  const durationMs = Math.max(getDurationMs(strokes), 1);
  const dominantStroke = longestStroke(strokes);
  const circularity = estimateCircularity(normalized);
  const dominantGap =
    dominantStroke && dominantStroke.points.length >= 2
      ? Math.hypot(
          dominantStroke.points[0].x - dominantStroke.points[dominantStroke.points.length - 1].x,
          dominantStroke.points[0].y - dominantStroke.points[dominantStroke.points.length - 1].y
        )
      : normalized.diagonal;
  const closure = clamp(1 - dominantGap / (normalized.diagonal * 0.35), 0, 1);
  const symmetry = calculateSymmetry(normalized);
  const smoothness = calculateSmoothness(strokes, normalized.diagonal);
  const tempo = calculateTempo(totalLength, durationMs, normalized.diagonal);
  const overshoot = calculateOvershoot(strokes, normalized.diagonal);
  const stability = calculateStability(strokes, durationMs);
  const rotationBias = clamp(
    (Math.abs(normalizeAngleHalfPi(normalized.rawAngleRadians)) / (Math.PI / 2)) * (1 - circularity * 0.7),
    0,
    1
  );

  return {
    closure,
    symmetry,
    smoothness,
    tempo,
    overshoot,
    stability,
    rotationBias
  };
}

export function calculateAdjustedQuality(
  rawQuality: QualityVector,
  profile?: UserInputProfile
): { adjustedQuality: QualityVector; qualityAdjustment: QualityVector } {
  const comfortBand = resolveUserComfortBand(profile);

  if (comfortBand.adjustmentStrength === 0) {
    return {
      adjustedQuality: { ...rawQuality },
      qualityAdjustment: createEmptyQualityVector()
    };
  }

  const adjustedQuality = QUALITY_VECTOR_KEYS.reduce<QualityVector>((accumulator, key) => {
    const rawValue = rawQuality[key];
    const comfortCenter = comfortBand.center[key];
    const comfortWidth = Math.max(comfortBand.band[key], 0.0001);
    const comfortScore = clamp(1 - Math.abs(rawValue - comfortCenter) / comfortWidth, 0, 1);
    const metricInfluence = comfortBand.adjustmentStrength * QUALITY_ADJUSTMENT_WEIGHTS[key];
    accumulator[key] = clamp(rawValue * (1 - metricInfluence) + comfortScore * metricInfluence, 0, 1);
    return accumulator;
  }, createEmptyQualityVector());

  return {
    adjustedQuality,
    qualityAdjustment: subtractQualityVectors(adjustedQuality, rawQuality)
  };
}

function calculateSymmetry(normalized: NormalizedBundle): number {
  const cloud = normalized.normalizedCloud;

  if (cloud.length === 0) {
    return 0;
  }

  const mirroredX = mirrorPointCloud(cloud, "x");
  const mirroredY = mirrorPointCloud(cloud, "y");
  const scoreX = clamp(1 - pointCloudDistance(cloud, mirroredX) / 0.55, 0, 1);
  const scoreY = clamp(1 - pointCloudDistance(cloud, mirroredY) / 0.55, 0, 1);

  return Math.max(scoreX, scoreY);
}

function estimateCircularity(normalized: NormalizedBundle): number {
  const radiusSamples = normalized.normalizedCloud.map((point) => Math.hypot(point.x, point.y));
  const meanRadius =
    radiusSamples.reduce((sum, value) => sum + value, 0) / Math.max(radiusSamples.length, 1);
  const variance =
    radiusSamples.reduce((sum, value) => sum + (value - meanRadius) ** 2, 0) /
    Math.max(radiusSamples.length, 1);

  return clamp(1 - Math.sqrt(variance) / Math.max(meanRadius, 0.0001) / 0.45, 0, 1);
}

function calculateSmoothness(strokes: Stroke[], diagonal: number): number {
  const penalties = strokes
    .filter((stroke) => stroke.points.length >= 3)
    .map((stroke) => {
      const deltas = [];
      for (let index = 1; index < stroke.points.length - 1; index += 1) {
        const prev = stroke.points[index - 1];
        const current = stroke.points[index];
        const next = stroke.points[index + 1];
        const angleA = Math.atan2(current.y - prev.y, current.x - prev.x);
        const angleB = Math.atan2(next.y - current.y, next.x - current.x);
        deltas.push(Math.abs(normalizeAngleHalfPi(angleB - angleA)));
      }

      const averageDelta = deltas.reduce((sum, value) => sum + value, 0) / Math.max(deltas.length, 1);
      return averageDelta / Math.PI;
    });

  const meanPenalty = penalties.length > 0 ? penalties.reduce((sum, value) => sum + value, 0) / penalties.length : 1;
  const jitterPenalty = clamp(meanPenalty + diagonal * 0.001, 0, 1);

  return clamp(1 - jitterPenalty, 0, 1);
}

function calculateTempo(totalLength: number, durationMs: number, diagonal: number): number {
  void totalLength;
  void diagonal;
  return clamp(1 - durationMs / 400, 0, 1);
}

function calculateOvershoot(strokes: Stroke[], diagonal: number): number {
  const penalties = strokes
    .filter((stroke) => stroke.points.length >= 2)
    .map((stroke) => {
      const simplified = rdpSimplify(stroke.points, Math.max(diagonal * 0.015, 1.5));
      const actual = pathLength(stroke.points);
      const skeletal = pathLength(simplified);

      if (actual === 0) {
        return 1;
      }

      return clamp((actual - skeletal) / (actual * 0.6), 0, 1);
    });

  const penalty = penalties.length > 0 ? penalties.reduce((sum, value) => sum + value, 0) / penalties.length : 1;
  return clamp(1 - penalty, 0, 1);
}

function calculateStability(strokes: Stroke[], durationMs: number): number {
  const speeds = [];
  let pauseCount = 0;
  let sampleCount = 0;

  for (const stroke of strokes) {
    for (let index = 1; index < stroke.points.length; index += 1) {
      const previous = stroke.points[index - 1];
      const current = stroke.points[index];
      const dt = Math.max(current.t - previous.t, 1);
      const segmentDistance = Math.hypot(current.x - previous.x, current.y - previous.y);
      speeds.push(segmentDistance / dt);
      if (dt > 110) {
        pauseCount += 1;
      }
      sampleCount += 1;
    }
  }

  if (speeds.length === 0) {
    return 0;
  }

  const average = speeds.reduce((sum, value) => sum + value, 0) / speeds.length;
  const variance =
    speeds.reduce((sum, value) => sum + (value - average) ** 2, 0) / Math.max(speeds.length, 1);
  const coefficient = average > 0 ? Math.sqrt(variance) / average : 1;
  const pauseRatio = sampleCount > 0 ? pauseCount / sampleCount : 0;
  const temporalPenalty = clamp((durationMs / 4000) * 0.2, 0, 0.2);
  const penalty = clamp(0.55 * pauseRatio + 0.45 * Math.min(coefficient, 1) + temporalPenalty, 0, 1);

  return clamp(1 - penalty, 0, 1);
}

function longestStroke(strokes: Stroke[]): Stroke | undefined {
  return [...strokes].sort((left, right) => pathLength(right.points) - pathLength(left.points))[0];
}

function getDurationMs(strokes: Stroke[]): number {
  const timestamps = strokes.flatMap((stroke) => stroke.points.map((point) => point.t));

  if (timestamps.length === 0) {
    return 0;
  }

  return Math.max(...timestamps) - Math.min(...timestamps);
}

function clamp(value: number, min: number, max: number): number {
  return Math.max(min, Math.min(max, value));
}
