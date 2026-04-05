import type { OverlayAnchorZoneId, OverlayOperator, PointSample, Stroke } from "./types";

export interface OverlayOperatorTemplate {
  operator: OverlayOperator;
  strokes: Stroke[];
  preferredAnchorZones: OverlayAnchorZoneId[];
  expectedScaleRange: [number, number];
  requiresOperator?: OverlayOperator;
}

export const OVERLAY_OPERATOR_TEMPLATES: OverlayOperatorTemplate[] = [
  {
    operator: "steel_brace",
    preferredAnchorZones: ["right", "lower_right", "upper_right"],
    expectedScaleRange: [0.16, 0.48],
    strokes: [
      createStroke("steel-brace-1", [
        [0.42, -0.72],
        [-0.4, -0.72],
        [-0.4, 0.72],
        [0.42, 0.72]
      ])
    ]
  },
  {
    operator: "electric_fork",
    preferredAnchorZones: ["upper_right", "upper", "right"],
    expectedScaleRange: [0.14, 0.4],
    strokes: [
      createStroke("electric-fork-1", [
        [-0.52, 0.58],
        [-0.08, 0.02],
        [-0.44, 0.02],
        [0.06, -0.66],
        [0.5, 0.02]
      ])
    ]
  },
  {
    operator: "ice_bar",
    preferredAnchorZones: ["core", "left", "right"],
    expectedScaleRange: [0.28, 0.52],
    strokes: [createStroke("ice-bar-1", [[-0.78, 0], [0.78, 0]])]
  },
  {
    operator: "soul_dot",
    preferredAnchorZones: ["core", "upper_left", "upper_right", "lower_left", "lower_right"],
    expectedScaleRange: [0.03, 0.16],
    strokes: [createStroke("soul-dot-1", circlePoints(18, 0.2))]
  },
  {
    operator: "void_cut",
    preferredAnchorZones: ["upper_right", "core", "lower_left"],
    expectedScaleRange: [0.14, 0.48],
    strokes: [createStroke("void-cut-1", [[-0.68, 0.68], [0.68, -0.68]])]
  },
  {
    operator: "martial_axis",
    preferredAnchorZones: ["lower_right", "core", "right"],
    expectedScaleRange: [0.14, 0.42],
    requiresOperator: "void_cut",
    strokes: [
      createStroke("martial-axis-1", [
        [0, -0.74],
        [0, 0.74],
        [0, 0],
        [0.5, 0],
        [-0.5, 0]
      ])
    ]
  }
];

function createStroke(id: string, points: Array<[number, number]>): Stroke {
  return {
    id,
    points: points.map(([x, y], index): PointSample => ({ x, y, t: index * 16 }))
  };
}

function circlePoints(count: number, radius: number): Array<[number, number]> {
  const points: Array<[number, number]> = [];

  for (let index = 0; index <= count; index += 1) {
    const angle = (index / count) * Math.PI * 2;
    points.push([Math.cos(angle) * radius, Math.sin(angle) * radius]);
  }

  return points;
}
