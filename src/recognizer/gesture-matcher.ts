import { distance, normalizeStrokes, pointCloudDistance } from "./geometry";
import { deriveRecognitionFeatureVectorV2 } from "./feature-v2";
import type { GestureRecognitionSignals, PointSample, Stroke } from "./types";

const DTW_SAMPLE_LIMIT = 64;

export function buildGestureRecognitionSignals(
  strokes: readonly Stroke[],
  templateStrokes: readonly Stroke[]
): GestureRecognitionSignals {
  const current = cloneValidStrokes(strokes);
  const template = cloneValidStrokes(templateStrokes);

  if (current.length === 0 || template.length === 0) {
    return emptyGestureSignals();
  }

  const currentNormalized = normalizeStrokes(current, DTW_SAMPLE_LIMIT);
  const templateNormalized = normalizeStrokes(template, DTW_SAMPLE_LIMIT);
  const trajectorySimilarity = scoreDtwTrajectory(
    currentNormalized.normalizedCloud,
    templateNormalized.normalizedCloud
  );
  const strokeOrderSimilarity = scoreStrokeOrder(currentNormalized.normalizedStrokes, templateNormalized.normalizedStrokes);
  const directionSequenceSimilarity = scoreDirectionSequenceSimilarity(current, template);
  const gestureScore = clamp(
    trajectorySimilarity * 0.42 + strokeOrderSimilarity * 0.28 + directionSequenceSimilarity * 0.3,
    0,
    1
  );
  const temporalScore = clamp(strokeOrderSimilarity * 0.55 + directionSequenceSimilarity * 0.45, 0, 1);

  return {
    trajectorySimilarity: roundMetric(trajectorySimilarity),
    strokeOrderSimilarity: roundMetric(strokeOrderSimilarity),
    directionSequenceSimilarity: roundMetric(directionSequenceSimilarity),
    gestureScore: roundMetric(gestureScore),
    temporalScore: roundMetric(temporalScore)
  };
}

export function averageGestureRecognitionSignals(
  signals: readonly GestureRecognitionSignals[]
): GestureRecognitionSignals | undefined {
  if (signals.length === 0) {
    return undefined;
  }

  return {
    trajectorySimilarity: roundMetric(average(signals.map((signal) => signal.trajectorySimilarity))),
    strokeOrderSimilarity: roundMetric(average(signals.map((signal) => signal.strokeOrderSimilarity))),
    directionSequenceSimilarity: roundMetric(average(signals.map((signal) => signal.directionSequenceSimilarity))),
    gestureScore: roundMetric(average(signals.map((signal) => signal.gestureScore))),
    temporalScore: roundMetric(average(signals.map((signal) => signal.temporalScore)))
  };
}

function scoreDtwTrajectory(current: readonly PointSample[], template: readonly PointSample[]): number {
  if (current.length === 0 || template.length === 0) {
    return 0;
  }

  const dtwDistance = dynamicTimeWarpingDistance(current, template);
  const cloudDistance = pointCloudDistance([...current], [...template]);
  const dtwScore = clamp(1 - dtwDistance / 0.72, 0, 1);
  const cloudScore = clamp(1 - cloudDistance / 0.72, 0, 1);

  return dtwScore * 0.72 + cloudScore * 0.28;
}

function scoreStrokeOrder(
  currentStrokes: readonly (readonly PointSample[])[],
  templateStrokes: readonly (readonly PointSample[])[]
): number {
  if (currentStrokes.length === 0 || templateStrokes.length === 0) {
    return 0;
  }

  const orderedCount = Math.min(currentStrokes.length, templateStrokes.length);
  const orderedScores = Array.from({ length: orderedCount }, (_, index) =>
    scoreDtwTrajectory(currentStrokes[index], templateStrokes[index])
  );
  const orderedAverage = average(orderedScores);
  const tolerantAverage = average(
    currentStrokes.map((stroke) =>
      Math.max(...templateStrokes.map((templateStroke) => scoreDtwTrajectory(stroke, templateStroke)))
    )
  );
  const countPenalty = clamp(1 - Math.abs(currentStrokes.length - templateStrokes.length) * 0.18, 0, 1);

  return clamp((orderedAverage * 0.68 + tolerantAverage * 0.32) * countPenalty, 0, 1);
}

function scoreDirectionSequenceSimilarity(strokes: readonly Stroke[], templateStrokes: readonly Stroke[]): number {
  const currentSequence = deriveRecognitionFeatureVectorV2(strokes).directionSequence;
  const templateSequence = deriveRecognitionFeatureVectorV2(templateStrokes).directionSequence;

  if (!currentSequence || !templateSequence) {
    return 0;
  }

  const lcs = longestCommonSubsequenceLength(currentSequence, templateSequence);
  return clamp((2 * lcs) / (currentSequence.length + templateSequence.length), 0, 1);
}

function dynamicTimeWarpingDistance(left: readonly PointSample[], right: readonly PointSample[]): number {
  const rows = left.length + 1;
  const columns = right.length + 1;
  const matrix = Array.from({ length: rows }, () => Array.from({ length: columns }, () => Number.POSITIVE_INFINITY));
  matrix[0][0] = 0;

  for (let row = 1; row < rows; row += 1) {
    const startColumn = Math.max(1, row - 14);
    const endColumn = Math.min(columns - 1, row + 14);

    for (let column = startColumn; column <= endColumn; column += 1) {
      const cost = distance(left[row - 1], right[column - 1]);
      matrix[row][column] =
        cost +
        Math.min(
          matrix[row - 1][column],
          matrix[row][column - 1],
          matrix[row - 1][column - 1]
        );
    }
  }

  return matrix[left.length][right.length] / Math.max(left.length + right.length, 1);
}

function longestCommonSubsequenceLength(left: string, right: string): number {
  const previous = Array.from({ length: right.length + 1 }, () => 0);
  const current = Array.from({ length: right.length + 1 }, () => 0);

  for (let leftIndex = 1; leftIndex <= left.length; leftIndex += 1) {
    for (let rightIndex = 1; rightIndex <= right.length; rightIndex += 1) {
      current[rightIndex] =
        left[leftIndex - 1] === right[rightIndex - 1]
          ? previous[rightIndex - 1] + 1
          : Math.max(previous[rightIndex], current[rightIndex - 1]);
    }

    for (let index = 0; index < current.length; index += 1) {
      previous[index] = current[index];
      current[index] = 0;
    }
  }

  return previous[right.length];
}

function cloneValidStrokes(strokes: readonly Stroke[]): Stroke[] {
  return strokes
    .filter((stroke) => stroke.points.length >= 2)
    .map((stroke) => ({
      ...stroke,
      points: stroke.points.map((point) => ({ ...point }))
    }));
}

function emptyGestureSignals(): GestureRecognitionSignals {
  return {
    trajectorySimilarity: 0,
    strokeOrderSimilarity: 0,
    directionSequenceSimilarity: 0,
    gestureScore: 0,
    temporalScore: 0
  };
}

function average(values: readonly number[]): number {
  if (values.length === 0) {
    return 0;
  }

  return values.reduce((sum, value) => sum + value, 0) / values.length;
}

function roundMetric(value: number): number {
  return Number(value.toFixed(4));
}

function clamp(value: number, minimum: number, maximum: number): number {
  return Math.max(minimum, Math.min(maximum, value));
}
