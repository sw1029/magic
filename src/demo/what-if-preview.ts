import { GLYPH_TEMPLATES } from "../recognizer/templates";
import { OVERLAY_OPERATOR_TEMPLATES } from "../recognizer/operator-templates";
import { resolveMagicCardForTarget } from "../recognizer/datacards";
import type {
  OverlayAnchorZoneId,
  OverlayReferenceFrame,
  PointSample,
  Stroke
} from "../recognizer/types";
import type { MagicWhatIfScenario } from "./what-if";

export type MagicWhatIfPreviewMarkKind = "ghost_stroke" | "anchor_zone" | "dependency_arrow" | "risk_label";
export type MagicWhatIfPreviewTone = "info" | "warning" | "risk";

export interface MagicWhatIfPreviewMark {
  id: string;
  kind: MagicWhatIfPreviewMarkKind;
  label: string;
  points?: PointSample[];
  from?: { x: number; y: number };
  to?: { x: number; y: number };
  anchorZoneId?: OverlayAnchorZoneId;
  tone: MagicWhatIfPreviewTone;
}

export interface MagicWhatIfPreviewModel {
  scenarioId: string;
  title: string;
  copy: string;
  nonMutating: true;
  marks: readonly MagicWhatIfPreviewMark[];
}

export function buildMagicWhatIfPreviewModel(input: {
  scenario: MagicWhatIfScenario;
  referenceFrame?: OverlayReferenceFrame | null;
  baseStrokes?: readonly Stroke[];
}): MagicWhatIfPreviewModel {
  const marks = buildMarks(input.scenario, input.referenceFrame ?? createFallbackReferenceFrame(input.baseStrokes));

  return {
    scenarioId: input.scenario.id,
    title: input.scenario.title,
    copy: `${input.scenario.impact.actionCopy} 현재 판정은 그대로 유지됩니다.`,
    nonMutating: true,
    marks
  };
}

function buildMarks(
  scenario: MagicWhatIfScenario,
  referenceFrame: OverlayReferenceFrame
): MagicWhatIfPreviewMark[] {
  switch (scenario.kind) {
    case "dependency_ordering":
      return buildDependencyMarks(scenario, referenceFrame);
    case "off_anchor_risk":
      return buildOperatorPlacementMarks(scenario, referenceFrame, "risk");
    case "underscale_risk":
      return buildUnderscaleMarks(scenario, referenceFrame);
    case "operator_anchor_movement":
      return buildOperatorPlacementMarks(scenario, referenceFrame, "warning");
    case "family_shape_mutation":
    default:
      return buildFamilyStructureMarks(scenario, referenceFrame);
  }
}

function buildFamilyStructureMarks(
  scenario: MagicWhatIfScenario,
  referenceFrame: OverlayReferenceFrame): MagicWhatIfPreviewMark[] {
  if (scenario.target.kind !== "family") {
    return [riskLabelMark(scenario, referenceFrame.centroid, "구조 비교")];
  }

  const template = GLYPH_TEMPLATES.find((item) => item.family === scenario.target.label);
  if (!template) {
    return [riskLabelMark(scenario, referenceFrame.centroid, "구조 비교")];
  }

  return [
    ...template.strokes.map((stroke, index) => ({
      id: `${scenario.id}:ghost:${index}`,
      kind: "ghost_stroke" as const,
      label: "구조 기준선",
      points: mapTemplateStroke(stroke.points, referenceFrame.centroid, referenceFrame.diagonal * 0.32),
      tone: "warning" as const
    })),
    riskLabelMark(scenario, offsetPoint(referenceFrame.centroid, 0, -referenceFrame.diagonal * 0.22), "구조를 바꿔 보는 비교")
  ];
}

function buildDependencyMarks(scenario: MagicWhatIfScenario, referenceFrame: OverlayReferenceFrame): MagicWhatIfPreviewMark[] {
  const from = findAnchorCenter(referenceFrame, "upper_right");
  const to = findAnchorCenter(referenceFrame, "core");

  return [
    {
      id: `${scenario.id}:dependency-arrow`,
      kind: "dependency_arrow",
      label: scenario.requires?.copy ?? "먼저 필요한 장식을 남긴 뒤 후속 장식을 더합니다.",
      from,
      to,
      tone: "risk"
    },
    riskLabelMark(scenario, offsetPoint(to, 0, referenceFrame.diagonal * 0.14), "순서 관계 비교", "risk")
  ];
}

function buildOperatorPlacementMarks(
  scenario: MagicWhatIfScenario,
  referenceFrame: OverlayReferenceFrame,
  tone: MagicWhatIfPreviewTone
): MagicWhatIfPreviewMark[] {
  const card = scenario.target.kind === "operator" ? resolveMagicCardForTarget("operator", scenario.target.label) : undefined;
  const anchorIds = card?.anchorHints ?? ["core"];
  const marks: MagicWhatIfPreviewMark[] = anchorIds.slice(0, 3).map((anchorZoneId) => ({
    id: `${scenario.id}:anchor:${anchorZoneId}`,
    kind: "anchor_zone",
    label: "권장 기준 자리",
    anchorZoneId,
    from: findAnchorCenter(referenceFrame, anchorZoneId),
    tone
  }));

  if (scenario.target.kind === "operator") {
    marks.push(...buildOperatorGhostMarks(scenario, referenceFrame, 0.18, tone));
  }

  marks.push(riskLabelMark(scenario, offsetPoint(referenceFrame.centroid, referenceFrame.diagonal * 0.22, 0), "배치 비교", tone));
  return marks;
}

function buildUnderscaleMarks(scenario: MagicWhatIfScenario, referenceFrame: OverlayReferenceFrame): MagicWhatIfPreviewMark[] {
  if (scenario.target.kind !== "operator") {
    return [riskLabelMark(scenario, referenceFrame.centroid, "크기 비교", "warning")];
  }

  return [
    ...buildOperatorGhostMarks(scenario, referenceFrame, 0.22, "info", "권장 크기"),
    ...buildOperatorGhostMarks(scenario, referenceFrame, 0.1, "warning", "작게 그린 경우", offsetPoint(referenceFrame.centroid, referenceFrame.diagonal * 0.18, 0)),
    riskLabelMark(scenario, offsetPoint(referenceFrame.centroid, 0, referenceFrame.diagonal * 0.2), "크기 차이 비교", "warning")
  ];
}

function buildOperatorGhostMarks(
  scenario: MagicWhatIfScenario,
  referenceFrame: OverlayReferenceFrame,
  scale: number,
  tone: MagicWhatIfPreviewTone,
  label = "장식 예시선",
  center = referenceFrame.centroid
): MagicWhatIfPreviewMark[] {
  if (scenario.target.kind !== "operator") {
    return [];
  }

  const template = OVERLAY_OPERATOR_TEMPLATES.find((item) => item.operator === scenario.target.label);
  if (!template) {
    return [];
  }

  return template.strokes.map((stroke, index) => ({
    id: `${scenario.id}:operator-ghost:${scale}:${index}`,
    kind: "ghost_stroke" as const,
    label,
    points: mapTemplateStroke(stroke.points, center, referenceFrame.diagonal * scale),
    tone
  }));
}

function riskLabelMark(
  scenario: MagicWhatIfScenario,
  point: { x: number; y: number },
  label: string,
  tone: MagicWhatIfPreviewTone = scenario.impact.riskLevel === "high" ? "risk" : "warning"
): MagicWhatIfPreviewMark {
  return {
    id: `${scenario.id}:label`,
    kind: "risk_label",
    label,
    from: point,
    tone
  };
}

function mapTemplateStroke(
  points: Array<{ x: number; y: number; t: number; pressure?: number }>,
  center: { x: number; y: number },
  scale: number
): PointSample[] {
  return points.map((point, index) => ({
    x: center.x + point.x * scale,
    y: center.y + point.y * scale,
    t: index * 16,
    pressure: point.pressure
  }));
}

function findAnchorCenter(referenceFrame: OverlayReferenceFrame, anchorZoneId: OverlayAnchorZoneId): { x: number; y: number } {
  return referenceFrame.anchorZones.find((zone) => zone.id === anchorZoneId)?.center ?? referenceFrame.centroid;
}

function offsetPoint(point: { x: number; y: number }, dx: number, dy: number): { x: number; y: number } {
  return { x: point.x + dx, y: point.y + dy };
}

function createFallbackReferenceFrame(baseStrokes?: readonly Stroke[]): OverlayReferenceFrame {
  const points = baseStrokes?.flatMap((stroke) => stroke.points) ?? [];
  const centroid = points.length > 0
    ? {
        x: points.reduce((sum, point) => sum + point.x, 0) / points.length,
        y: points.reduce((sum, point) => sum + point.y, 0) / points.length
      }
    : { x: 450, y: 310 };
  const diagonal = 260;
  const zoneRadius = 42;
  const offset = 96;
  const anchorCenters: Record<OverlayAnchorZoneId, { x: number; y: number }> = {
    upper_left: offsetPoint(centroid, -offset, -offset),
    upper: offsetPoint(centroid, 0, -offset),
    upper_right: offsetPoint(centroid, offset, -offset),
    left: offsetPoint(centroid, -offset, 0),
    core: centroid,
    right: offsetPoint(centroid, offset, 0),
    lower_left: offsetPoint(centroid, -offset, offset),
    lower: offsetPoint(centroid, 0, offset),
    lower_right: offsetPoint(centroid, offset, offset)
  };

  return {
    centroid,
    bounds: {
      minX: centroid.x - diagonal / 2,
      maxX: centroid.x + diagonal / 2,
      minY: centroid.y - diagonal / 2,
      maxY: centroid.y + diagonal / 2,
      width: diagonal,
      height: diagonal
    },
    diagonal,
    axisAngleRadians: 0,
    anchorZones: Object.entries(anchorCenters).map(([id, center]) => ({
      id: id as OverlayAnchorZoneId,
      center,
      radius: zoneRadius,
      bounds: {
        minX: center.x - zoneRadius,
        maxX: center.x + zoneRadius,
        minY: center.y - zoneRadius,
        maxY: center.y + zoneRadius,
        width: zoneRadius * 2,
        height: zoneRadius * 2
      }
    })),
    referenceLines: {
      horizontal: { start: offsetPoint(centroid, -diagonal / 2, 0), end: offsetPoint(centroid, diagonal / 2, 0) },
      vertical: { start: offsetPoint(centroid, 0, -diagonal / 2), end: offsetPoint(centroid, 0, diagonal / 2) },
      ascendingDiagonal: { start: offsetPoint(centroid, -diagonal / 2, diagonal / 2), end: offsetPoint(centroid, diagonal / 2, -diagonal / 2) }
    }
  };
}
