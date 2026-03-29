import type { GlyphFamily, PointSample, Stroke } from "./types";

export interface GlyphTemplate {
  family: GlyphFamily;
  strokes: Stroke[];
  expectedStrokeCount: [number, number];
  closed: boolean;
  expectedCorners?: number;
}

export const GLYPH_TEMPLATES: GlyphTemplate[] = [
  {
    family: "wind",
    expectedStrokeCount: [3, 3],
    closed: false,
    strokes: [
      createStroke("wind-1", [
        [-0.7, -0.35],
        [0.7, -0.35]
      ]),
      createStroke("wind-2", [
        [-0.7, 0],
        [0.7, 0]
      ]),
      createStroke("wind-3", [
        [-0.7, 0.35],
        [0.7, 0.35]
      ])
    ]
  },
  {
    family: "earth",
    expectedStrokeCount: [1, 2],
    closed: true,
    expectedCorners: 4,
    strokes: [
      createStroke("earth-1", [
        [-0.65, 0.55],
        [-0.35, -0.5],
        [0.35, -0.5],
        [0.65, 0.55],
        [-0.65, 0.55]
      ])
    ]
  },
  {
    family: "fire",
    expectedStrokeCount: [1, 2],
    closed: true,
    expectedCorners: 3,
    strokes: [
      createStroke("fire-1", [
        [0, -0.72],
        [0.72, 0.62],
        [-0.72, 0.62],
        [0, -0.72]
      ])
    ]
  },
  {
    family: "water",
    expectedStrokeCount: [1, 2],
    closed: true,
    strokes: [createStroke("water-1", circlePoints(28, 0.68, 0, 0))]
  },
  {
    family: "life",
    expectedStrokeCount: [1, 3],
    closed: false,
    strokes: [
      createStroke("life-1", [
        [0, 0.72],
        [0, 0.08],
        [-0.42, -0.56],
        [0, 0.08],
        [0.42, -0.56]
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

function circlePoints(count: number, radius: number, cx: number, cy: number): Array<[number, number]> {
  const points: Array<[number, number]> = [];

  for (let index = 0; index <= count; index += 1) {
    const angle = (index / count) * Math.PI * 2;
    points.push([cx + Math.cos(angle) * radius, cy + Math.sin(angle) * radius]);
  }

  return points;
}
