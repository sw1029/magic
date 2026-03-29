import type { PointSample, Stroke } from "./types";

export interface BoundingBox {
  minX: number;
  maxX: number;
  minY: number;
  maxY: number;
  width: number;
  height: number;
}

export interface NormalizedBundle {
  smoothedStrokes: Stroke[];
  normalizedStrokes: PointSample[][];
  rawCloud: PointSample[];
  normalizedCloud: PointSample[];
  rawAngleRadians: number;
  rawCentroid: { x: number; y: number };
  bounds: BoundingBox;
  diagonal: number;
}

export function distance(a: { x: number; y: number }, b: { x: number; y: number }): number {
  return Math.hypot(a.x - b.x, a.y - b.y);
}

export function boundingBox(points: PointSample[]): BoundingBox {
  if (points.length === 0) {
    return { minX: 0, maxX: 0, minY: 0, maxY: 0, width: 0, height: 0 };
  }

  const xs = points.map((point) => point.x);
  const ys = points.map((point) => point.y);
  const minX = Math.min(...xs);
  const maxX = Math.max(...xs);
  const minY = Math.min(...ys);
  const maxY = Math.max(...ys);

  return {
    minX,
    maxX,
    minY,
    maxY,
    width: maxX - minX,
    height: maxY - minY
  };
}

export function centroid(points: PointSample[]): { x: number; y: number } {
  if (points.length === 0) {
    return { x: 0, y: 0 };
  }

  const { x, y } = points.reduce(
    (accumulator, point) => ({
      x: accumulator.x + point.x,
      y: accumulator.y + point.y
    }),
    { x: 0, y: 0 }
  );

  return {
    x: x / points.length,
    y: y / points.length
  };
}

export function pathLength(points: PointSample[]): number {
  if (points.length < 2) {
    return 0;
  }

  let total = 0;

  for (let index = 1; index < points.length; index += 1) {
    total += distance(points[index - 1], points[index]);
  }

  return total;
}

export function smoothStroke(points: PointSample[], radius = 1): PointSample[] {
  if (points.length <= 2 || radius < 1) {
    return points.map((point) => ({ ...point }));
  }

  return points.map((point, index) => {
    const start = Math.max(0, index - radius);
    const end = Math.min(points.length - 1, index + radius);
    const slice = points.slice(start, end + 1);
    const sum = slice.reduce(
      (accumulator, item) => ({
        x: accumulator.x + item.x,
        y: accumulator.y + item.y,
        t: accumulator.t + item.t
      }),
      { x: 0, y: 0, t: 0 }
    );

    return {
      x: sum.x / slice.length,
      y: sum.y / slice.length,
      t: point.t,
      pressure: point.pressure
    };
  });
}

export function resamplePoints(points: PointSample[], sampleCount: number): PointSample[] {
  if (points.length === 0) {
    return [];
  }

  if (points.length === 1) {
    return Array.from({ length: sampleCount }, (_, index) => ({
      ...points[0],
      t: points[0].t + index
    }));
  }

  const totalLength = pathLength(points);

  if (totalLength === 0) {
    return Array.from({ length: sampleCount }, (_, index) => ({
      ...points[0],
      t: points[0].t + index
    }));
  }

  const step = totalLength / Math.max(sampleCount - 1, 1);
  const resampled = [{ ...points[0] }];
  let accumulated = 0;
  let previous = { ...points[0] };
  let pointIndex = 1;

  while (pointIndex < points.length) {
    const current = points[pointIndex];
    const segmentLength = distance(previous, current);

    if (segmentLength === 0) {
      pointIndex += 1;
      continue;
    }

    if (accumulated + segmentLength >= step) {
      const remainder = step - accumulated;
      const ratio = remainder / segmentLength;
      const interpolated = {
        x: previous.x + ratio * (current.x - previous.x),
        y: previous.y + ratio * (current.y - previous.y),
        t: previous.t + ratio * (current.t - previous.t),
        pressure: previous.pressure
      };
      resampled.push(interpolated);
      previous = interpolated;
      accumulated = 0;
    } else {
      accumulated += segmentLength;
      previous = current;
      pointIndex += 1;
    }
  }

  while (resampled.length < sampleCount) {
    resampled.push({ ...points[points.length - 1] });
  }

  return resampled.slice(0, sampleCount);
}

export function principalAxisAngle(points: PointSample[]): number {
  if (points.length < 2) {
    return 0;
  }

  const center = centroid(points);
  let xx = 0;
  let yy = 0;
  let xy = 0;

  for (const point of points) {
    const dx = point.x - center.x;
    const dy = point.y - center.y;
    xx += dx * dx;
    yy += dy * dy;
    xy += dx * dy;
  }

  return 0.5 * Math.atan2(2 * xy, xx - yy);
}

export function rotatePoint(point: PointSample, angle: number, center = { x: 0, y: 0 }): PointSample {
  const cosine = Math.cos(angle);
  const sine = Math.sin(angle);
  const translatedX = point.x - center.x;
  const translatedY = point.y - center.y;

  return {
    ...point,
    x: translatedX * cosine - translatedY * sine + center.x,
    y: translatedX * sine + translatedY * cosine + center.y
  };
}

export function normalizeAngleHalfPi(angle: number): number {
  let normalized = angle;

  while (normalized > Math.PI / 2) {
    normalized -= Math.PI;
  }

  while (normalized < -Math.PI / 2) {
    normalized += Math.PI;
  }

  return normalized;
}

export function normalizeStrokes(strokes: Stroke[], totalSamples = 96): NormalizedBundle {
  const smoothedStrokes = strokes
    .map((stroke) => ({
      ...stroke,
      points: smoothStroke(stroke.points)
    }))
    .filter((stroke) => stroke.points.length >= 2);

  const lengths = smoothedStrokes.map((stroke) => pathLength(stroke.points));
  const totalLength = lengths.reduce((sum, value) => sum + value, 0);
  const minimumSamples = 10;
  let counts =
    totalLength === 0
      ? smoothedStrokes.map(() => Math.max(minimumSamples, Math.floor(totalSamples / Math.max(smoothedStrokes.length, 1))))
      : smoothedStrokes.map((stroke, index) =>
          Math.max(minimumSamples, Math.round((lengths[index] / totalLength) * totalSamples))
        );

  while (counts.reduce((sum, value) => sum + value, 0) > totalSamples) {
    const adjustableIndex = counts.findIndex((value) => value > minimumSamples);
    if (adjustableIndex === -1) {
      break;
    }
    counts[adjustableIndex] -= 1;
  }

  while (counts.reduce((sum, value) => sum + value, 0) < totalSamples && counts.length > 0) {
    const longestIndex = lengths.indexOf(Math.max(...lengths));
    counts[longestIndex] += 1;
  }

  const resampledStrokes = smoothedStrokes.map((stroke, index) =>
    resamplePoints(stroke.points, Math.max(counts[index], 2))
  );
  const rawCloud = resampledStrokes.flat();
  const rawCentroid = centroid(rawCloud);
  const rawAngleRadians = normalizeAngleHalfPi(principalAxisAngle(rawCloud));
  const centered = resampledStrokes.map((stroke) =>
    stroke.map((point) => ({
      ...point,
      x: point.x - rawCentroid.x,
      y: point.y - rawCentroid.y
    }))
  );
  const rotated = centered.map((stroke) => stroke.map((point) => rotatePoint(point, -rawAngleRadians)));
  const rotatedBounds = boundingBox(rotated.flat());
  const scale = Math.max(rotatedBounds.width, rotatedBounds.height, 1);
  const normalizedStrokes = rotated.map((stroke) =>
    stroke.map((point) => ({
      ...point,
      x: point.x / scale,
      y: point.y / scale
    }))
  );

  return {
    smoothedStrokes,
    normalizedStrokes,
    rawCloud,
    normalizedCloud: normalizedStrokes.flat(),
    rawAngleRadians,
    rawCentroid,
    bounds: boundingBox(rawCloud),
    diagonal: Math.max(Math.hypot(rotatedBounds.width, rotatedBounds.height), 1)
  };
}

export function pointCloudDistance(a: PointSample[], b: PointSample[]): number {
  if (a.length === 0 || b.length === 0) {
    return 1;
  }

  const forward = averageNearestNeighborDistance(a, b);
  const backward = averageNearestNeighborDistance(b, a);

  return (forward + backward) / 2;
}

export function mirrorPointCloud(points: PointSample[], axis: "x" | "y"): PointSample[] {
  return points.map((point) => ({
    ...point,
    x: axis === "y" ? -point.x : point.x,
    y: axis === "x" ? -point.y : point.y
  }));
}

export function rdpSimplify(points: PointSample[], epsilon: number): PointSample[] {
  if (points.length <= 2) {
    return points.map((point) => ({ ...point }));
  }

  let maxDistance = 0;
  let splitIndex = 0;

  for (let index = 1; index < points.length - 1; index += 1) {
    const currentDistance = distanceToSegment(points[index], points[0], points[points.length - 1]);
    if (currentDistance > maxDistance) {
      maxDistance = currentDistance;
      splitIndex = index;
    }
  }

  if (maxDistance <= epsilon) {
    return [{ ...points[0] }, { ...points[points.length - 1] }];
  }

  const left = rdpSimplify(points.slice(0, splitIndex + 1), epsilon);
  const right = rdpSimplify(points.slice(splitIndex), epsilon);

  return [...left.slice(0, -1), ...right];
}

export function clusterEndpointCount(strokes: Stroke[], threshold: number): number {
  const endpoints = strokes.flatMap((stroke) => {
    if (stroke.points.length === 0) {
      return [];
    }
    return [stroke.points[0], stroke.points[stroke.points.length - 1]];
  });

  const clusters: Array<{ x: number; y: number }> = [];

  for (const endpoint of endpoints) {
    const existing = clusters.find((cluster) => distance(cluster, endpoint) <= threshold);

    if (existing) {
      existing.x = (existing.x + endpoint.x) / 2;
      existing.y = (existing.y + endpoint.y) / 2;
    } else {
      clusters.push({ x: endpoint.x, y: endpoint.y });
    }
  }

  return clusters.length;
}

export function strokeStraightness(stroke: Stroke): number {
  if (stroke.points.length < 2) {
    return 0;
  }

  const direct = distance(stroke.points[0], stroke.points[stroke.points.length - 1]);
  const actual = pathLength(stroke.points);

  if (actual === 0) {
    return 0;
  }

  return Math.max(0, Math.min(direct / actual, 1));
}

export function lineAngle(stroke: Stroke): number {
  if (stroke.points.length < 2) {
    return 0;
  }

  const first = stroke.points[0];
  const last = stroke.points[stroke.points.length - 1];
  return normalizeAngleHalfPi(Math.atan2(last.y - first.y, last.x - first.x));
}

function averageNearestNeighborDistance(a: PointSample[], b: PointSample[]): number {
  return (
    a.reduce((sum, point) => {
      let nearest = Number.POSITIVE_INFINITY;

      for (const candidate of b) {
        nearest = Math.min(nearest, distance(point, candidate));
      }

      return sum + nearest;
    }, 0) / a.length
  );
}

function distanceToSegment(point: PointSample, segmentStart: PointSample, segmentEnd: PointSample): number {
  const dx = segmentEnd.x - segmentStart.x;
  const dy = segmentEnd.y - segmentStart.y;

  if (dx === 0 && dy === 0) {
    return distance(point, segmentStart);
  }

  const projection =
    ((point.x - segmentStart.x) * dx + (point.y - segmentStart.y) * dy) / (dx * dx + dy * dy);
  const clamped = Math.max(0, Math.min(1, projection));

  return distance(point, {
    x: segmentStart.x + clamped * dx,
    y: segmentStart.y + clamped * dy
  });
}
