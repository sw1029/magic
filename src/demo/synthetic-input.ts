import { GLYPH_TEMPLATES } from "../recognizer/templates";
import type { GlyphFamily, PointSample, Stroke, StrokeSession } from "../recognizer/types";
import { dashboardFamilyName } from "./dashboard-copy";

export interface SyntheticInputRecipe {
  family: GlyphFamily;
  seed?: number;
  jitterPx?: number;
  rotationDeg?: number;
  scale?: number;
  curveWarp?: number;
  openGapRatio?: number;
  pointDensity?: number;
  strokeSplitChance?: number;
  extraNoiseStrokeCount?: number;
  durationMs?: number;
  center?: { x: number; y: number };
  size?: number;
}

export const DEFAULT_SYNTHETIC_RECIPE: Required<Omit<SyntheticInputRecipe, "center">> & { center: { x: number; y: number } } = {
  family: "fire",
  seed: 42,
  jitterPx: 0,
  rotationDeg: 0,
  scale: 1,
  curveWarp: 0,
  openGapRatio: 0,
  pointDensity: 4,
  strokeSplitChance: 0,
  extraNoiseStrokeCount: 0,
  durationMs: 900,
  center: { x: 450, y: 310 },
  size: 190
};

interface RandomSource {
  next(): number;
}

export function buildSyntheticStrokeSession(recipe: SyntheticInputRecipe): StrokeSession {
  const resolved = resolveRecipe(recipe);
  const template = GLYPH_TEMPLATES.find((item) => item.family === resolved.family) ?? GLYPH_TEMPLATES[0];
  const random = createSeededRandom(resolved.seed);
  const angle = (resolved.rotationDeg / 180) * Math.PI;
  const strokes: Stroke[] = [];
  const totalPointCount = template.strokes.reduce(
    (sum, stroke) => sum + densifyPoints(stroke.points, resolved.pointDensity).length,
    0
  );
  let pointOrdinal = 0;

  template.strokes.forEach((stroke, strokeIndex) => {
    let points = densifyPoints(stroke.points, resolved.pointDensity);
    points = applyOpenGap(points, resolved.openGapRatio);
    const transformed = points.map((point, index) => {
      const warped = applyCurveWarp(point, index, points.length, resolved.curveWarp);
      const scaled = { x: warped.x * resolved.size * resolved.scale, y: warped.y * resolved.size * resolved.scale };
      const rotated = rotatePoint(scaled, angle);
      const jitter = resolved.jitterPx > 0
        ? {
            x: (random.next() * 2 - 1) * resolved.jitterPx,
            y: (random.next() * 2 - 1) * resolved.jitterPx
          }
        : { x: 0, y: 0 };
      const t = Math.round((pointOrdinal / Math.max(totalPointCount - 1, 1)) * resolved.durationMs);
      pointOrdinal += 1;
      return {
        x: resolved.center.x + rotated.x + jitter.x,
        y: resolved.center.y + rotated.y + jitter.y,
        t,
        pressure: 0.62 + random.next() * 0.28
      } satisfies PointSample;
    });

    const split = resolved.strokeSplitChance > 0 && random.next() < resolved.strokeSplitChance && transformed.length > 4;
    if (split) {
      const splitIndex = Math.max(2, Math.min(transformed.length - 2, Math.floor(transformed.length * (0.35 + random.next() * 0.3))));
      strokes.push({ id: `synthetic-${resolved.family}-${strokeIndex}-a`, points: transformed.slice(0, splitIndex) });
      strokes.push({ id: `synthetic-${resolved.family}-${strokeIndex}-b`, points: transformed.slice(splitIndex) });
    } else {
      strokes.push({ id: `synthetic-${resolved.family}-${strokeIndex}`, points: transformed });
    }
  });

  for (let index = 0; index < resolved.extraNoiseStrokeCount; index += 1) {
    strokes.push(buildNoiseStroke(`synthetic-noise-${index}`, resolved, random, index));
  }

  const startedAt = 1_700_000_000_000 + resolved.seed;
  return {
    strokes,
    startedAt,
    endedAt: startedAt + resolved.durationMs
  };
}

export function describeSyntheticRecipe(recipe: SyntheticInputRecipe): string {
  const resolved = resolveRecipe(recipe);
  const parts = [dashboardFamilyName(resolved.family)];

  if (resolved.jitterPx >= 4) {
    parts.push("손떨림을 넣음");
  }
  if (resolved.openGapRatio >= 0.12) {
    parts.push("열린 틈을 남김");
  }
  if (Math.abs(resolved.rotationDeg) >= 12) {
    parts.push(`${Math.round(resolved.rotationDeg)}도 기울임`);
  }
  if (resolved.curveWarp >= 0.12) {
    parts.push("선을 휘게 만듦");
  }
  if (resolved.extraNoiseStrokeCount > 0) {
    parts.push(`노이즈 선 ${resolved.extraNoiseStrokeCount}개`);
  }

  return parts.length > 1 ? parts.join(" · ") : `${parts[0]} 또렷한 입력`;
}

export function createSeededRandom(seed = 1): RandomSource {
  let state = Math.abs(Math.floor(seed)) || 1;
  return {
    next(): number {
      state = (state * 1664525 + 1013904223) >>> 0;
      return state / 0xffffffff;
    }
  };
}

function resolveRecipe(recipe: SyntheticInputRecipe): Required<Omit<SyntheticInputRecipe, "center">> & { center: { x: number; y: number } } {
  return {
    ...DEFAULT_SYNTHETIC_RECIPE,
    ...recipe,
    seed: recipe.seed ?? DEFAULT_SYNTHETIC_RECIPE.seed,
    jitterPx: clamp(recipe.jitterPx ?? DEFAULT_SYNTHETIC_RECIPE.jitterPx, 0, 64),
    rotationDeg: clamp(recipe.rotationDeg ?? DEFAULT_SYNTHETIC_RECIPE.rotationDeg, -180, 180),
    scale: clamp(recipe.scale ?? DEFAULT_SYNTHETIC_RECIPE.scale, 0.2, 2.5),
    curveWarp: clamp(recipe.curveWarp ?? DEFAULT_SYNTHETIC_RECIPE.curveWarp, 0, 1),
    openGapRatio: clamp(recipe.openGapRatio ?? DEFAULT_SYNTHETIC_RECIPE.openGapRatio, 0, 0.82),
    pointDensity: Math.max(1, Math.round(recipe.pointDensity ?? DEFAULT_SYNTHETIC_RECIPE.pointDensity)),
    strokeSplitChance: clamp(recipe.strokeSplitChance ?? DEFAULT_SYNTHETIC_RECIPE.strokeSplitChance, 0, 1),
    extraNoiseStrokeCount: Math.max(0, Math.round(recipe.extraNoiseStrokeCount ?? DEFAULT_SYNTHETIC_RECIPE.extraNoiseStrokeCount)),
    durationMs: Math.max(120, Math.round(recipe.durationMs ?? DEFAULT_SYNTHETIC_RECIPE.durationMs)),
    center: recipe.center ?? DEFAULT_SYNTHETIC_RECIPE.center,
    size: clamp(recipe.size ?? DEFAULT_SYNTHETIC_RECIPE.size, 40, 420)
  };
}

function densifyPoints(points: PointSample[], density: number): PointSample[] {
  if (points.length <= 1 || density <= 1) {
    return points.map((point) => ({ ...point }));
  }

  const result: PointSample[] = [];
  for (let index = 0; index < points.length - 1; index += 1) {
    const start = points[index];
    const end = points[index + 1];
    for (let step = 0; step < density; step += 1) {
      const ratio = step / density;
      result.push({
        x: start.x + (end.x - start.x) * ratio,
        y: start.y + (end.y - start.y) * ratio,
        t: start.t + (end.t - start.t) * ratio,
        pressure: start.pressure
      });
    }
  }
  result.push({ ...points[points.length - 1] });
  return result;
}

function applyOpenGap(points: PointSample[], openGapRatio: number): PointSample[] {
  if (openGapRatio <= 0 || points.length < 5) {
    return points;
  }

  const first = points[0];
  const last = points[points.length - 1];
  const closes = Math.hypot(first.x - last.x, first.y - last.y) < 0.08;
  if (!closes) {
    return points;
  }

  const removeCount = Math.max(1, Math.floor(points.length * openGapRatio * 0.45));
  return points.slice(0, Math.max(2, points.length - removeCount));
}

function applyCurveWarp(point: PointSample, index: number, count: number, curveWarp: number): PointSample {
  if (curveWarp <= 0 || count <= 1) {
    return point;
  }

  const progress = index / Math.max(count - 1, 1);
  const bend = Math.sin(progress * Math.PI) * curveWarp * 0.18;
  return {
    ...point,
    x: point.x + bend * Math.sign(point.y || 1),
    y: point.y + bend * Math.sign(point.x || 1)
  };
}

function rotatePoint(point: { x: number; y: number }, angle: number): { x: number; y: number } {
  const cos = Math.cos(angle);
  const sin = Math.sin(angle);
  return {
    x: point.x * cos - point.y * sin,
    y: point.x * sin + point.y * cos
  };
}

function buildNoiseStroke(
  id: string,
  recipe: Required<Omit<SyntheticInputRecipe, "center">> & { center: { x: number; y: number } },
  random: RandomSource,
  index: number
): Stroke {
  const angle = random.next() * Math.PI * 2;
  const radius = recipe.size * (0.28 + random.next() * 0.72);
  const length = recipe.size * (0.12 + random.next() * 0.24);
  const center = {
    x: recipe.center.x + Math.cos(angle) * radius,
    y: recipe.center.y + Math.sin(angle) * radius
  };
  const lineAngle = angle + Math.PI / 2;
  const startT = recipe.durationMs + index * 30;

  return {
    id,
    points: [
      {
        x: center.x - Math.cos(lineAngle) * length,
        y: center.y - Math.sin(lineAngle) * length,
        t: startT
      },
      {
        x: center.x + Math.cos(lineAngle) * length,
        y: center.y + Math.sin(lineAngle) * length,
        t: startT + 24
      }
    ]
  };
}

function clamp(value: number, minimum: number, maximum: number): number {
  return Math.max(minimum, Math.min(maximum, value));
}
