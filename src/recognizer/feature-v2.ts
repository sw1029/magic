import {
  boundingBox,
  clusterEndpointCount,
  distance,
  normalizeStrokes,
  pathLength,
  rdpSimplify
} from "./geometry";
import type { PointSample, RecognitionFeatureVectorV2, Stroke } from "./types";

const DIRECTION_BUCKETS = 8;

export function deriveRecognitionFeatureVectorV2(strokes: readonly Stroke[]): RecognitionFeatureVectorV2 {
  const validStrokes = strokes.filter((stroke) => stroke.points.length >= 2);

  if (validStrokes.length === 0) {
    return createEmptyFeatureVectorV2();
  }

  const allPoints = validStrokes.flatMap((stroke) => stroke.points);
  const velocities = collectVelocities(validStrokes);
  const curvatures = collectCurvatures(validStrokes);
  const pressures = allPoints.map((point) => point.pressure).filter((value): value is number => value !== undefined);
  const directionSequence = buildDirectionSequence(validStrokes);
  const endpointClusterCount = clusterEndpointCount(validStrokes, resolveEndpointRadius(allPoints));

  return {
    strokeOrderSignature: buildStrokeOrderSignature(validStrokes),
    strokeSplitCount: Math.max(validStrokes.length - 1, 0),
    mergeCandidateCount: countMergeCandidates(validStrokes),
    velocityMean: roundMetric(average(velocities)),
    velocityVariance: roundMetric(variance(velocities)),
    pauseCount: countPauses(validStrokes),
    curvatureMean: roundMetric(average(curvatures)),
    curvatureVariance: roundMetric(variance(curvatures)),
    curvatureHistogram: buildCurvatureHistogram(curvatures),
    selfIntersectionCount: countSelfIntersections(validStrokes),
    endpointTopology: `${endpointClusterCount}:${validStrokes.length * 2}`,
    endpointClusterCount,
    pressureMean: roundMetric(average(pressures)),
    pressureVariance: roundMetric(variance(pressures)),
    directionSequence,
    directionChangeCount: countDirectionChanges(directionSequence)
  };
}

export function averageFeatureVectorV2(
  vectors: readonly RecognitionFeatureVectorV2[]
): Partial<RecognitionFeatureVectorV2> | undefined {
  if (vectors.length === 0) {
    return undefined;
  }

  return {
    strokeOrderSignature: mostFrequent(vectors.map((vector) => vector.strokeOrderSignature)),
    strokeSplitCount: roundMetric(average(vectors.map((vector) => vector.strokeSplitCount))),
    mergeCandidateCount: roundMetric(average(vectors.map((vector) => vector.mergeCandidateCount))),
    velocityMean: roundMetric(average(vectors.map((vector) => vector.velocityMean))),
    velocityVariance: roundMetric(average(vectors.map((vector) => vector.velocityVariance))),
    pauseCount: roundMetric(average(vectors.map((vector) => vector.pauseCount))),
    curvatureMean: roundMetric(average(vectors.map((vector) => vector.curvatureMean))),
    curvatureVariance: roundMetric(average(vectors.map((vector) => vector.curvatureVariance))),
    curvatureHistogram: [
      roundMetric(average(vectors.map((vector) => vector.curvatureHistogram[0]))),
      roundMetric(average(vectors.map((vector) => vector.curvatureHistogram[1]))),
      roundMetric(average(vectors.map((vector) => vector.curvatureHistogram[2])))
    ],
    selfIntersectionCount: roundMetric(average(vectors.map((vector) => vector.selfIntersectionCount))),
    endpointTopology: mostFrequent(vectors.map((vector) => vector.endpointTopology)),
    endpointClusterCount: roundMetric(average(vectors.map((vector) => vector.endpointClusterCount))),
    pressureMean: roundMetric(average(vectors.map((vector) => vector.pressureMean))),
    pressureVariance: roundMetric(average(vectors.map((vector) => vector.pressureVariance))),
    directionSequence: mostFrequent(vectors.map((vector) => vector.directionSequence)),
    directionChangeCount: roundMetric(average(vectors.map((vector) => vector.directionChangeCount)))
  };
}

export function varianceFeatureVectorV2(
  vectors: readonly RecognitionFeatureVectorV2[]
): Partial<RecognitionFeatureVectorV2> | undefined {
  if (vectors.length === 0) {
    return undefined;
  }

  return {
    strokeSplitCount: roundMetric(variance(vectors.map((vector) => vector.strokeSplitCount))),
    mergeCandidateCount: roundMetric(variance(vectors.map((vector) => vector.mergeCandidateCount))),
    velocityMean: roundMetric(variance(vectors.map((vector) => vector.velocityMean))),
    velocityVariance: roundMetric(variance(vectors.map((vector) => vector.velocityVariance))),
    pauseCount: roundMetric(variance(vectors.map((vector) => vector.pauseCount))),
    curvatureMean: roundMetric(variance(vectors.map((vector) => vector.curvatureMean))),
    curvatureVariance: roundMetric(variance(vectors.map((vector) => vector.curvatureVariance))),
    selfIntersectionCount: roundMetric(variance(vectors.map((vector) => vector.selfIntersectionCount))),
    endpointClusterCount: roundMetric(variance(vectors.map((vector) => vector.endpointClusterCount))),
    pressureMean: roundMetric(variance(vectors.map((vector) => vector.pressureMean))),
    pressureVariance: roundMetric(variance(vectors.map((vector) => vector.pressureVariance))),
    directionChangeCount: roundMetric(variance(vectors.map((vector) => vector.directionChangeCount)))
  };
}

export function cloneFeatureVectorV2(vector: RecognitionFeatureVectorV2 | undefined): RecognitionFeatureVectorV2 | undefined;
export function cloneFeatureVectorV2(
  vector: Partial<RecognitionFeatureVectorV2> | undefined
): Partial<RecognitionFeatureVectorV2> | undefined;
export function cloneFeatureVectorV2(
  vector: Partial<RecognitionFeatureVectorV2> | undefined
): Partial<RecognitionFeatureVectorV2> | undefined {
  if (!vector) {
    return undefined;
  }

  return vector.curvatureHistogram
    ? {
        ...vector,
        curvatureHistogram: [...vector.curvatureHistogram] as [number, number, number]
      }
    : { ...vector };
}

function createEmptyFeatureVectorV2(): RecognitionFeatureVectorV2 {
  return {
    strokeOrderSignature: "empty",
    strokeSplitCount: 0,
    mergeCandidateCount: 0,
    velocityMean: 0,
    velocityVariance: 0,
    pauseCount: 0,
    curvatureMean: 0,
    curvatureVariance: 0,
    curvatureHistogram: [0, 0, 0],
    selfIntersectionCount: 0,
    endpointTopology: "0:0",
    endpointClusterCount: 0,
    pressureMean: 0,
    pressureVariance: 0,
    directionSequence: "",
    directionChangeCount: 0
  };
}

function buildStrokeOrderSignature(strokes: readonly Stroke[]): string {
  const lengths = strokes.map((stroke) => pathLength(stroke.points));
  const maxLength = Math.max(...lengths, 1);

  return strokes
    .map((stroke, index) => {
      const start = stroke.points[0];
      const end = stroke.points[stroke.points.length - 1];
      const lengthBucket = Math.round((lengths[index] / maxLength) * 4);
      return `${quadrant(start)}${quadrant(end)}${lengthBucket}`;
    })
    .join("-");
}

function collectVelocities(strokes: readonly Stroke[]): number[] {
  const values: number[] = [];

  for (const stroke of strokes) {
    for (let index = 1; index < stroke.points.length; index += 1) {
      const previous = stroke.points[index - 1];
      const current = stroke.points[index];
      const dt = Math.max(current.t - previous.t, 1);
      values.push(distance(previous, current) / dt);
    }
  }

  return values;
}

function collectCurvatures(strokes: readonly Stroke[]): number[] {
  const values: number[] = [];

  for (const stroke of strokes) {
    const points = stroke.points;
    for (let index = 1; index < points.length - 1; index += 1) {
      const a = Math.atan2(points[index].y - points[index - 1].y, points[index].x - points[index - 1].x);
      const b = Math.atan2(points[index + 1].y - points[index].y, points[index + 1].x - points[index].x);
      values.push(Math.abs(normalizeAngle(b - a)) / Math.PI);
    }
  }

  return values;
}

function countPauses(strokes: readonly Stroke[]): number {
  const deltas = strokes.flatMap((stroke) =>
    stroke.points.slice(1).map((point, index) => Math.max(point.t - stroke.points[index].t, 1))
  );

  if (deltas.length === 0) {
    return 0;
  }

  const sorted = [...deltas].sort((left, right) => left - right);
  const median = sorted[Math.floor(sorted.length / 2)] ?? 1;
  const threshold = Math.max(median * 2.5, 48);
  return deltas.filter((delta) => delta >= threshold).length;
}

function buildCurvatureHistogram(curvatures: readonly number[]): [number, number, number] {
  if (curvatures.length === 0) {
    return [0, 0, 0];
  }

  const bins = [0, 0, 0];
  for (const value of curvatures) {
    if (value < 0.12) {
      bins[0] += 1;
    } else if (value < 0.36) {
      bins[1] += 1;
    } else {
      bins[2] += 1;
    }
  }

  return bins.map((count) => roundMetric(count / curvatures.length)) as [number, number, number];
}

function countSelfIntersections(strokes: readonly Stroke[]): number {
  const segments = strokes.flatMap((stroke) => {
    const simplified = rdpSimplify(stroke.points, 3);
    return simplified.slice(1).map((point, index) => ({
      strokeId: stroke.id,
      index,
      a: simplified[index],
      b: point
    }));
  });
  let count = 0;

  for (let left = 0; left < segments.length; left += 1) {
    for (let right = left + 1; right < segments.length; right += 1) {
      const a = segments[left];
      const b = segments[right];
      if (a.strokeId === b.strokeId && Math.abs(a.index - b.index) <= 1) {
        continue;
      }
      if (segmentsIntersect(a.a, a.b, b.a, b.b)) {
        count += 1;
      }
    }
  }

  return count;
}

function countMergeCandidates(strokes: readonly Stroke[]): number {
  if (strokes.length < 2) {
    return 0;
  }

  const allPoints = strokes.flatMap((stroke) => stroke.points);
  const radius = resolveEndpointRadius(allPoints) * 0.72;
  let count = 0;

  for (let index = 1; index < strokes.length; index += 1) {
    const previous = strokes[index - 1].points;
    const current = strokes[index].points;
    if (distance(previous[previous.length - 1], current[0]) <= radius) {
      count += 1;
    }
  }

  return count;
}

function buildDirectionSequence(strokes: readonly Stroke[]): string {
  const normalized = normalizeStrokes(strokes.map((stroke) => ({ ...stroke, points: stroke.points.map((point) => ({ ...point })) })), 48);
  const points = normalized.normalizedCloud;
  const tokens: string[] = [];

  for (let index = 1; index < points.length; index += 1) {
    const previous = points[index - 1];
    const current = points[index];
    if (distance(previous, current) < 0.01) {
      continue;
    }
    const angle = Math.atan2(current.y - previous.y, current.x - previous.x);
    const bucket = Math.round(((angle + Math.PI) / (Math.PI * 2)) * DIRECTION_BUCKETS) % DIRECTION_BUCKETS;
    if (tokens[tokens.length - 1] !== String(bucket)) {
      tokens.push(String(bucket));
    }
  }

  return tokens.join("");
}

function countDirectionChanges(sequence: string): number {
  let count = 0;
  for (let index = 1; index < sequence.length; index += 1) {
    if (sequence[index] !== sequence[index - 1]) {
      count += 1;
    }
  }
  return count;
}

function resolveEndpointRadius(points: readonly PointSample[]): number {
  const box = boundingBox([...points]);
  return Math.max(Math.hypot(box.width, box.height) * 0.08, 14);
}

function quadrant(point: PointSample): string {
  return `${point.x >= 0 ? "r" : "l"}${point.y >= 0 ? "b" : "t"}`;
}

function segmentsIntersect(a: PointSample, b: PointSample, c: PointSample, d: PointSample): boolean {
  const ab1 = orientation(a, b, c);
  const ab2 = orientation(a, b, d);
  const cd1 = orientation(c, d, a);
  const cd2 = orientation(c, d, b);
  return ab1 * ab2 < 0 && cd1 * cd2 < 0;
}

function orientation(a: PointSample, b: PointSample, c: PointSample): number {
  return Math.sign((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x));
}

function normalizeAngle(angle: number): number {
  let current = angle;
  while (current > Math.PI) {
    current -= Math.PI * 2;
  }
  while (current < -Math.PI) {
    current += Math.PI * 2;
  }
  return current;
}

function mostFrequent(values: readonly string[]): string {
  const counts = new Map<string, number>();
  for (const value of values) {
    counts.set(value, (counts.get(value) ?? 0) + 1);
  }
  return [...counts.entries()].sort((left, right) => right[1] - left[1])[0]?.[0] ?? "";
}

function average(values: readonly number[]): number {
  if (values.length === 0) {
    return 0;
  }
  return values.reduce((sum, value) => sum + value, 0) / values.length;
}

function variance(values: readonly number[]): number {
  if (values.length === 0) {
    return 0;
  }
  const mean = average(values);
  return average(values.map((value) => (value - mean) ** 2));
}

function roundMetric(value: number): number {
  return Number(value.toFixed(4));
}
