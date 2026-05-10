import type { StrokeSession } from "../recognizer/types";

export type DashboardFixtureKind = "stroke_session" | "datacard_patch" | "batch_recipe" | "tutorial_profile" | "user_profile" | "unknown";

export interface DashboardFixtureParseResult {
  ok: boolean;
  kind: DashboardFixtureKind;
  value: unknown;
  userMessage: string;
}

export function parseDashboardFixture(rawJson: string): DashboardFixtureParseResult {
  try {
    const value = JSON.parse(rawJson) as unknown;
    const kind = inferDashboardFixtureKind(value);
    return {
      ok: kind !== "unknown",
      kind,
      value,
      userMessage: kind === "unknown" ? "알 수 없는 예시 데이터입니다." : `${fixtureKindLabel(kind)} 예시 데이터로 읽었습니다.`
    };
  } catch {
    return {
      ok: false,
      kind: "unknown",
      value: null,
      userMessage: "JSON을 읽을 수 없습니다. 쉼표와 따옴표를 확인해 주세요."
    };
  }
}

export function inferDashboardFixtureKind(value: unknown): DashboardFixtureKind {
  if (!isRecord(value)) {
    return "unknown";
  }

  if (isStrokeSessionLike(value)) {
    return "stroke_session";
  }

  if (Array.isArray(value.cards)) {
    return "datacard_patch";
  }

  if (typeof value.family === "string" && ("iterations" in value || "jitterPx" in value || "openGapRatio" in value)) {
    return "batch_recipe";
  }

  if (value.version === "v1.5" && Array.isArray(value.captures) && isRecord(value.shapeProfile)) {
    return "tutorial_profile";
  }

  if (value.version === "v1.5" && typeof value.sampleCount === "number" && isRecord(value.averageQuality)) {
    return "user_profile";
  }

  return "unknown";
}

export function fixtureKindLabel(kind: DashboardFixtureKind): string {
  switch (kind) {
    case "stroke_session":
      return "선 입력";
    case "datacard_patch":
      return "설명 카드 패치";
    case "batch_recipe":
      return "여러 번 테스트 설정";
    case "tutorial_profile":
      return "연습 기록";
    case "user_profile":
      return "입력 습관";
    default:
      return "알 수 없음";
  }
}

export function coerceStrokeSessionFixture(value: unknown): StrokeSession | null {
  if (!isRecord(value) || !isStrokeSessionLike(value)) {
    return null;
  }

  return {
    strokes: value.strokes,
    startedAt: typeof value.startedAt === "number" ? value.startedAt : Date.now(),
    endedAt: typeof value.endedAt === "number" ? value.endedAt : undefined
  } as StrokeSession;
}

function isStrokeSessionLike(value: Record<string, unknown>): boolean {
  return Array.isArray(value.strokes) && value.strokes.every((stroke) => {
    if (!isRecord(stroke) || !Array.isArray(stroke.points)) {
      return false;
    }
    return stroke.points.every((point) => isRecord(point) && typeof point.x === "number" && typeof point.y === "number");
  });
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}
