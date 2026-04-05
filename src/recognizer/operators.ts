import {
  boundingBox,
  centroid,
  distance,
  lineAngle,
  normalizeAngleHalfPi,
  normalizeStrokes,
  pointCloudDistance,
  principalAxisAngle,
  rdpSimplify,
  strokeStraightness
} from "./geometry";
import { OVERLAY_OPERATOR_TEMPLATES, type OverlayOperatorTemplate } from "./operator-templates";
import type {
  AxisLine,
  OverlayAnchorZone,
  OverlayAnchorZoneId,
  OverlayOperator,
  OverlayRecognition,
  OverlayRecognitionCandidate,
  OverlayRecognitionContext,
  OverlayReferenceFrame,
  PointSample,
  Stroke,
  StrokeBounds,
  StrokeSession
} from "./types";

interface OverlayFeatures {
  bounds: StrokeBounds;
  centroid: { x: number; y: number };
  straightness: number;
  corners: number;
  closure: number;
  circularity: number;
  aspectRatio: number;
  angleRadians: number;
  scaleRatio: number;
  anchorScores: Record<OverlayAnchorZoneId, number>;
  horizontalAxisScore: number;
  verticalAxisScore: number;
  ascendingDiagonalScore: number;
}

const TEMPLATE_BUNDLES = OVERLAY_OPERATOR_TEMPLATES.map((template) => ({
  ...template,
  normalized: normalizeStrokes(template.strokes, 64)
}));

export function createOverlayReferenceFrame(input: StrokeSession | Stroke[]): OverlayReferenceFrame {
  const strokes = Array.isArray(input) ? input : input.strokes;
  const points = strokes.flatMap((stroke) => stroke.points);

  if (points.length === 0) {
    return emptyReferenceFrame();
  }

  const bounds = boundingBox(points);
  const center = centroid(points);
  const diagonal = Math.max(Math.hypot(bounds.width, bounds.height), 1);
  const axisAngleRadians = normalizeAngleHalfPi(principalAxisAngle(points));
  const offsetX = Math.max(bounds.width * 0.28, diagonal * 0.12);
  const offsetY = Math.max(bounds.height * 0.28, diagonal * 0.12);
  const zoneRadius = Math.max(diagonal * 0.16, 18);
  const marginX = Math.max(bounds.width * 0.18, diagonal * 0.08);
  const marginY = Math.max(bounds.height * 0.18, diagonal * 0.08);
  const minX = bounds.minX - marginX;
  const maxX = bounds.maxX + marginX;
  const minY = bounds.minY - marginY;
  const maxY = bounds.maxY + marginY;
  const anchorCenters: Record<OverlayAnchorZoneId, { x: number; y: number }> = {
    upper_left: { x: center.x - offsetX, y: center.y - offsetY },
    upper: { x: center.x, y: center.y - offsetY },
    upper_right: { x: center.x + offsetX, y: center.y - offsetY },
    left: { x: center.x - offsetX, y: center.y },
    core: { x: center.x, y: center.y },
    right: { x: center.x + offsetX, y: center.y },
    lower_left: { x: center.x - offsetX, y: center.y + offsetY },
    lower: { x: center.x, y: center.y + offsetY },
    lower_right: { x: center.x + offsetX, y: center.y + offsetY }
  };
  const anchorZones = Object.entries(anchorCenters).map(([id, zoneCenter]) =>
    createAnchorZone(id as OverlayAnchorZoneId, zoneCenter, zoneRadius)
  );

  return {
    centroid: center,
    bounds,
    diagonal,
    axisAngleRadians,
    anchorZones,
    referenceLines: {
      horizontal: {
        start: { x: minX, y: center.y },
        end: { x: maxX, y: center.y }
      },
      vertical: {
        start: { x: center.x, y: minY },
        end: { x: center.x, y: maxY }
      },
      ascendingDiagonal: {
        start: { x: minX, y: maxY },
        end: { x: maxX, y: minY }
      }
    }
  };
}

export function recognizeOverlayStroke(
  stroke: Stroke,
  context: OverlayRecognitionContext
): OverlayRecognition {
  const bounds = boundingBox(stroke.points);

  if (stroke.points.length < 2) {
    return {
      strokeId: stroke.id,
      status: "invalid",
      candidates: [],
      invalidReason: "overlay operator는 최소 2개 이상의 포인트가 필요합니다.",
      normalizedStrokes: [],
      bounds
    };
  }

  const normalized = normalizeStrokes([stroke], 64);
  const features = deriveOverlayFeatures(stroke, normalized.normalizedCloud, context.referenceFrame);
  const candidates = TEMPLATE_BUNDLES.map((template) =>
    scoreOverlayCandidate(template, normalized.normalizedCloud, features, context)
  ).sort((left, right) => right.score - left.score);
  const topCandidate = candidates[0];
  const voidCutCandidate = candidates.find((candidate) => candidate.operator === "void_cut");
  const martialAxisCandidate = candidates.find((candidate) => candidate.operator === "martial_axis");
  const secondCandidate = candidates[1];
  const margin = topCandidate ? topCandidate.score - (secondCandidate?.score ?? 0) : 0;
  let status: OverlayRecognition["status"] = "invalid";
  let operator: OverlayOperator | undefined;
  let invalidReason = "overlay operator 기준형과 충분히 가깝지 않습니다.";

  const martialAxisIsCompetitive =
    Boolean(martialAxisCandidate?.score) &&
    martialAxisCandidate!.score >= 0.62 &&
    martialAxisCandidate!.score >= (topCandidate?.score ?? 0) - 0.03;
  const voidCutIsCompetitive =
    Boolean(voidCutCandidate?.score) &&
    voidCutCandidate!.score >= 0.72 &&
    voidCutCandidate!.score >= (topCandidate?.score ?? 0) - 0.03;

  if (martialAxisIsCompetitive && martialAxisCandidate) {
    if (martialAxisCandidate.blockedBy) {
      status = "incomplete";
      invalidReason = `martial_axis는 ${martialAxisCandidate.blockedBy} 이후에만 활성화됩니다.`;
    } else {
      status = "recognized";
      operator = "martial_axis";
      invalidReason = "martial_axis operator가 stack에 추가되었습니다.";
    }
  } else if (voidCutIsCompetitive) {
    status = "recognized";
    operator = "void_cut";
    invalidReason = "void_cut operator가 stack에 추가되었습니다.";
  } else if (topCandidate?.blockedBy && topCandidate.score >= 0.62) {
    status = "incomplete";
    invalidReason = `martial_axis는 ${topCandidate.blockedBy} 이후에만 활성화됩니다.`;
  } else if (topCandidate?.completenessHint && topCandidate.score >= 0.52) {
    status = "incomplete";
    invalidReason = topCandidate.completenessHint;
  } else if (topCandidate?.score >= 0.74 && margin >= 0.05) {
    status = "recognized";
    operator = topCandidate.operator;
    invalidReason = `${topCandidate.operator} operator가 stack에 추가되었습니다.`;
  } else if (topCandidate?.score >= 0.56) {
    status = "ambiguous";
    invalidReason = "overlay operator 후보가 겹쳐 최종 stack에 추가하지 않습니다.";
  }

  return {
    strokeId: stroke.id,
    status,
    operator,
    candidates,
    topCandidate,
    invalidReason,
    normalizedStrokes: normalized.normalizedStrokes,
    bounds,
    debugAxis: buildDebugAxis(stroke),
    anchorZoneId: topCandidate?.anchorZoneId
  };
}

function deriveOverlayFeatures(
  stroke: Stroke,
  normalizedCloud: PointSample[],
  referenceFrame: OverlayReferenceFrame
): OverlayFeatures {
  const bounds = boundingBox(stroke.points);
  const strokeCentroid = centroid(stroke.points);
  const strokeDiagonal = Math.max(Math.hypot(bounds.width, bounds.height), 1);
  const firstPoint = stroke.points[0];
  const lastPoint = stroke.points[stroke.points.length - 1];
  const closure = clamp(
    1 - Math.hypot(firstPoint.x - lastPoint.x, firstPoint.y - lastPoint.y) / (strokeDiagonal * 0.35),
    0,
    1
  );
  const aspectRatio = Math.max(bounds.width, bounds.height) / Math.max(Math.min(bounds.width, bounds.height), 1);
  const simplified = rdpSimplify(stroke.points, Math.max(strokeDiagonal * 0.08, 4));
  const corners = Math.max(simplified.length - 1, 0);
  const radii = normalizedCloud.map((point) => Math.hypot(point.x, point.y));
  const meanRadius = radii.reduce((sum, value) => sum + value, 0) / Math.max(radii.length, 1);
  const variance =
    radii.reduce((sum, value) => sum + (value - meanRadius) ** 2, 0) / Math.max(radii.length, 1);

  return {
    bounds,
    centroid: strokeCentroid,
    straightness: strokeStraightness(stroke),
    corners,
    closure,
    circularity: clamp(1 - Math.sqrt(variance) / Math.max(meanRadius, 0.0001) / 0.45, 0, 1),
    aspectRatio,
    angleRadians: normalizeAngleHalfPi(lineAngle(stroke)),
    scaleRatio: strokeDiagonal / Math.max(referenceFrame.diagonal, 1),
    anchorScores: referenceFrame.anchorZones.reduce<Record<OverlayAnchorZoneId, number>>((accumulator, zone) => {
      accumulator[zone.id] = zoneScore(strokeCentroid, zone);
      return accumulator;
    }, {
      upper_left: 0,
      upper: 0,
      upper_right: 0,
      left: 0,
      core: 0,
      right: 0,
      lower_left: 0,
      lower: 0,
      lower_right: 0
    }),
    horizontalAxisScore: axisScore(strokeCentroid, referenceFrame.referenceLines.horizontal, referenceFrame.diagonal * 0.14),
    verticalAxisScore: axisScore(strokeCentroid, referenceFrame.referenceLines.vertical, referenceFrame.diagonal * 0.14),
    ascendingDiagonalScore: axisScore(
      strokeCentroid,
      referenceFrame.referenceLines.ascendingDiagonal,
      referenceFrame.diagonal * 0.18
    )
  };
}

function scoreOverlayCandidate(
  template: (typeof TEMPLATE_BUNDLES)[number],
  normalizedCloud: PointSample[],
  features: OverlayFeatures,
  context: OverlayRecognitionContext
): OverlayRecognitionCandidate {
  const templateDistance = pointCloudDistance(normalizedCloud, template.normalized.normalizedCloud);
  const templateScore = clamp(1 - templateDistance / 0.62, 0, 1);
  const openScore = clamp(1 - features.closure, 0, 1);
  const anchor = bestAnchorScore(features.anchorScores, template.preferredAnchorZones);
  const scaleScore = rangeScore(features.scaleRatio, template.expectedScaleRange[0], template.expectedScaleRange[1]);
  const notes = [
    `template=${templateScore.toFixed(2)}`,
    `anchor=${anchor.id}:${anchor.score.toFixed(2)}`,
    `scale=${scaleScore.toFixed(2)}`
  ];
  let score = templateScore;
  let completenessHint: string | undefined;

  switch (template.operator) {
    case "steel_brace": {
      const cornerScore = closenessScore(features.corners, 3, 1.6);
      score = templateScore * 0.34 + cornerScore * 0.22 + anchor.score * 0.16 + openScore * 0.16 + scaleScore * 0.12;
      notes.push(`corners=${cornerScore.toFixed(2)}`, `open=${openScore.toFixed(2)}`);
      if (cornerScore < 0.56 && score >= 0.48) {
        completenessHint = "steel_brace는 세 번 꺾이는 열린 brace 실루엣이 필요합니다.";
      }
      break;
    }
    case "electric_fork": {
      const cornerScore = closenessScore(features.corners, 4, 1.8);
      score = templateScore * 0.34 + cornerScore * 0.26 + anchor.score * 0.18 + openScore * 0.12 + scaleScore * 0.1;
      notes.push(`corners=${cornerScore.toFixed(2)}`, `open=${openScore.toFixed(2)}`);
      if (cornerScore < 0.58 && score >= 0.48) {
        completenessHint = "electric_fork는 분기된 fork 형태의 꺾임이 더 분명해야 합니다.";
      }
      break;
    }
    case "ice_bar": {
      const horizontalScore = clamp(1 - Math.abs(features.angleRadians) / (Math.PI / 8), 0, 1);
      score =
        templateScore * 0.14 +
        features.straightness * 0.34 +
        horizontalScore * 0.24 +
        anchor.score * 0.16 +
        scaleScore * 0.12;
      notes.push(`straight=${features.straightness.toFixed(2)}`, `horizontal=${horizontalScore.toFixed(2)}`);
      if (scaleScore < 0.46 && features.straightness >= 0.72) {
        completenessHint = "ice_bar는 기준선보다 조금 더 긴 수평 bar가 필요합니다.";
      }
      break;
    }
    case "soul_dot": {
      const compactScore = clamp(1 - features.scaleRatio / Math.max(template.expectedScaleRange[1], 0.0001), 0, 1);
      score =
        templateScore * 0.14 +
        features.circularity * 0.28 +
        features.closure * 0.24 +
        anchor.score * 0.2 +
        compactScore * 0.14;
      notes.push(`circular=${features.circularity.toFixed(2)}`, `closure=${features.closure.toFixed(2)}`);
      if (features.circularity >= 0.55 && features.closure < 0.56) {
        completenessHint = "soul_dot는 더 닫힌 점형 루프여야 합니다.";
      }
      break;
    }
    case "void_cut": {
      const diagonalScore = clamp(
        1 - Math.abs(Math.abs(features.angleRadians) - Math.PI / 4) / (Math.PI / 8),
        0,
        1
      );
      score =
        templateScore * 0.12 +
        features.straightness * 0.3 +
        diagonalScore * 0.26 +
        anchor.score * 0.18 +
        scaleScore * 0.14;
      notes.push(`straight=${features.straightness.toFixed(2)}`, `diagonal=${diagonalScore.toFixed(2)}`);
      if (scaleScore < 0.46 && diagonalScore >= 0.7) {
        completenessHint = "void_cut는 base silhouette를 가로지르는 충분한 길이의 대각 절개여야 합니다.";
      }
      break;
    }
    case "martial_axis": {
      const cornerScore = closenessScore(features.corners, 4, 1.8);
      const crossScore = Math.max(features.verticalAxisScore, features.horizontalAxisScore);
      score =
        templateScore * 0.46 +
        cornerScore * 0.24 +
        crossScore * 0.08 +
        anchor.score * 0.08 +
        scaleScore * 0.08 +
        openScore * 0.06;
      notes.push(`corners=${cornerScore.toFixed(2)}`, `cross=${crossScore.toFixed(2)}`);
      if (!context.existingOperators.includes("void_cut")) {
        return {
          operator: template.operator,
          score: clamp(score, 0, 1),
          templateDistance,
          notes,
          anchorZoneId: anchor.id,
          blockedBy: "void_cut",
          completenessHint: "martial_axis는 void_cut 이후에만 해석할 수 있습니다."
        };
      }
      if (cornerScore < 0.56 && score >= 0.5) {
        completenessHint = "martial_axis는 세로축과 가로축이 교차하는 축형이 더 분명해야 합니다.";
      }
      break;
    }
    default:
      break;
  }

  return {
    operator: template.operator,
    score: clamp(score, 0, 1),
    templateDistance,
    notes,
    anchorZoneId: anchor.id,
    completenessHint
  };
}

function bestAnchorScore(
  scores: Record<OverlayAnchorZoneId, number>,
  preferredZones: OverlayAnchorZoneId[]
): { id: OverlayAnchorZoneId; score: number } {
  return preferredZones.reduce<{ id: OverlayAnchorZoneId; score: number }>(
    (best, zoneId) => {
      const score = scores[zoneId];
      return score > best.score ? { id: zoneId, score } : best;
    },
    { id: preferredZones[0], score: scores[preferredZones[0]] }
  );
}

function createAnchorZone(
  id: OverlayAnchorZoneId,
  centerPoint: { x: number; y: number },
  radius: number
): OverlayAnchorZone {
  return {
    id,
    center: centerPoint,
    radius,
    bounds: {
      minX: centerPoint.x - radius,
      maxX: centerPoint.x + radius,
      minY: centerPoint.y - radius,
      maxY: centerPoint.y + radius,
      width: radius * 2,
      height: radius * 2
    }
  };
}

function zoneScore(point: { x: number; y: number }, zone: OverlayAnchorZone): number {
  return clamp(1 - distance(point, zone.center) / Math.max(zone.radius, 0.0001), 0, 1);
}

function axisScore(point: { x: number; y: number }, axis: AxisLine, tolerance: number): number {
  const distanceToAxis = distanceToLine(point, axis.start, axis.end);
  return clamp(1 - distanceToAxis / Math.max(tolerance, 0.0001), 0, 1);
}

function distanceToLine(
  point: { x: number; y: number },
  start: { x: number; y: number },
  end: { x: number; y: number }
): number {
  const dx = end.x - start.x;
  const dy = end.y - start.y;

  if (dx === 0 && dy === 0) {
    return distance(point, start);
  }

  return Math.abs(dy * point.x - dx * point.y + end.x * start.y - end.y * start.x) / Math.hypot(dx, dy);
}

function rangeScore(value: number, minimum: number, maximum: number): number {
  if (value >= minimum && value <= maximum) {
    return 1;
  }

  const distanceToRange = value < minimum ? minimum - value : value - maximum;
  return clamp(1 - distanceToRange / 0.18, 0, 1);
}

function closenessScore(value: number, target: number, tolerance: number): number {
  return clamp(1 - Math.abs(value - target) / tolerance, 0, 1);
}

function buildDebugAxis(stroke: Stroke): AxisLine | undefined {
  if (stroke.points.length < 2) {
    return undefined;
  }

  return {
    start: {
      x: stroke.points[0].x,
      y: stroke.points[0].y
    },
    end: {
      x: stroke.points[stroke.points.length - 1].x,
      y: stroke.points[stroke.points.length - 1].y
    }
  };
}

function emptyReferenceFrame(): OverlayReferenceFrame {
  const radius = 24;
  const coreZone = createAnchorZone("core", { x: 0, y: 0 }, radius);
  const horizontal = { start: { x: -radius, y: 0 }, end: { x: radius, y: 0 } };
  const vertical = { start: { x: 0, y: -radius }, end: { x: 0, y: radius } };
  const ascendingDiagonal = { start: { x: -radius, y: radius }, end: { x: radius, y: -radius } };

  return {
    centroid: { x: 0, y: 0 },
    bounds: {
      minX: -radius,
      maxX: radius,
      minY: -radius,
      maxY: radius,
      width: radius * 2,
      height: radius * 2
    },
    diagonal: radius * 2,
    axisAngleRadians: 0,
    anchorZones: [
      createAnchorZone("upper_left", { x: -radius, y: -radius }, radius),
      createAnchorZone("upper", { x: 0, y: -radius }, radius),
      createAnchorZone("upper_right", { x: radius, y: -radius }, radius),
      createAnchorZone("left", { x: -radius, y: 0 }, radius),
      coreZone,
      createAnchorZone("right", { x: radius, y: 0 }, radius),
      createAnchorZone("lower_left", { x: -radius, y: radius }, radius),
      createAnchorZone("lower", { x: 0, y: radius }, radius),
      createAnchorZone("lower_right", { x: radius, y: radius }, radius)
    ],
    referenceLines: {
      horizontal,
      vertical,
      ascendingDiagonal
    }
  };
}

function clamp(value: number, minimum: number, maximum: number): number {
  return Math.max(minimum, Math.min(maximum, value));
}
