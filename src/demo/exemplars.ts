import type { GlyphFamily, OverlayOperator, RecognitionResult, OverlayRecognition } from "../recognizer/types";

export type ExemplarId = GlyphFamily | OverlayOperator;

export interface ExemplarSpec {
  id: ExemplarId;
  label: string;
  category: "family" | "operator";
  caption: string;
  descriptor: string;
}

const TOKEN_REGEX = /(poly|line|loop)\(([^)]*)\)/g;
const POINT_REGEX = /(-?\d+(?:\.\d+)?),(-?\d+(?:\.\d+)?)/g;

const EXEMPLARS: Record<ExemplarId, ExemplarSpec> = {
  wind: {
    id: "wind",
    label: "바람형 모범",
    category: "family",
    caption: "세 줄이 평행하게 열려 있으면 바람형으로 읽기 쉽습니다.",
    descriptor: "line(-72,-28;72,-28) line(-72,0;72,0) line(-72,28;72,28)"
  },
  earth: {
    id: "earth",
    label: "땅형 모범",
    category: "family",
    caption: "아래가 넓고 닫힘이 유지되면 땅형으로 안정적입니다.",
    descriptor: "poly(-64,54;-34,-48;34,-48;64,54;-64,54)"
  },
  fire: {
    id: "fire",
    label: "불꽃형 모범",
    category: "family",
    caption: "위 꼭짓점이 살아 있는 닫힌 삼각형이면 불꽃형으로 읽힙니다.",
    descriptor: "poly(0,-72;72,62;-72,62;0,-72)"
  },
  water: {
    id: "water",
    label: "물형 모범",
    category: "family",
    caption: "한 번에 돌아 닫히는 루프면 물형으로 안정적입니다.",
    descriptor: "loop(0,-68;48,-48;68,0;48,48;0,68;-48,48;-68,0;-48,-48;0,-68)"
  },
  life: {
    id: "life",
    label: "생명형 모범",
    category: "family",
    caption: "중심 줄기와 두 갈래가 또렷하면 생명형으로 읽기 쉽습니다.",
    descriptor: "poly(0,70;0,8;-42,-56;0,8;42,-56)"
  },
  steel_brace: {
    id: "steel_brace",
    label: "버팀 장식 모범",
    category: "operator",
    caption: "오른쪽에 열린 ㄷ자형으로 붙으면 버팀 장식으로 읽기 쉽습니다.",
    descriptor: "poly(42,-72;-40,-72;-40,72;42,72)"
  },
  electric_fork: {
    id: "electric_fork",
    label: "갈래 번개 모범",
    category: "operator",
    caption: "갈래 수와 꺾임이 분명하면 갈래 번개로 읽기 쉽습니다.",
    descriptor: "poly(-52,58;-8,2;-44,2;6,-66;50,2)"
  },
  ice_bar: {
    id: "ice_bar",
    label: "얼음 막대 모범",
    category: "operator",
    caption: "가로선이 길고 곧게 유지되면 얼음 막대로 읽기 쉽습니다.",
    descriptor: "line(-78,0;78,0)"
  },
  soul_dot: {
    id: "soul_dot",
    label: "혼 점 모범",
    category: "operator",
    caption: "작고 닫힌 점 모양이면 혼 점으로 읽기 쉽습니다.",
    descriptor: "loop(0,-20;14,-14;20,0;14,14;0,20;-14,14;-20,0;-14,-14;0,-20)"
  },
  void_cut: {
    id: "void_cut",
    label: "공백 절단 모범",
    category: "operator",
    caption: "대각선 한 줄이 또렷하면 공백 절단으로 읽기 쉽습니다.",
    descriptor: "line(-68,68;68,-68)"
  },
  martial_axis: {
    id: "martial_axis",
    label: "축선 장식 모범",
    category: "operator",
    caption: "세로축과 짧은 가로축이 같이 보이면 축선 장식으로 읽기 쉽습니다.",
    descriptor: "line(0,-74;0,74) line(-50,0;50,0)"
  }
};

const SVG_CACHE = new Map<string, string>();

export function getExemplarSpec(id: ExemplarId): ExemplarSpec {
  return EXEMPLARS[id];
}

export function renderExemplarChip(id: ExemplarId, options?: { active?: boolean; hint?: string }): string {
  const spec = EXEMPLARS[id];

  if (!spec) {
    return "";
  }

  return `
    <article class="exemplar-chip ${options?.active ? "active" : ""}">
      <div class="exemplar-thumb">${renderExemplarSvg(spec.descriptor)}</div>
      <div class="exemplar-body">
        <strong>${spec.label}</strong>
        <p>${options?.hint ?? spec.caption}</p>
      </div>
    </article>
  `;
}

export function resolveRelevantExemplarIds(
  baseResult: RecognitionResult,
  overlayRecognition: OverlayRecognition | null
): ExemplarId[] {
  const ids: ExemplarId[] = [];
  const baseTop = baseResult.canonicalFamily ?? baseResult.topCandidate?.family;
  const baseRunner = baseResult.candidates[1]?.family;
  const overlayTop = overlayRecognition?.operator ?? overlayRecognition?.topCandidate?.operator;

  if (baseTop) {
    ids.push(baseTop);
  }

  if (baseRunner && baseRunner !== baseTop && baseResult.status !== "recognized") {
    ids.push(baseRunner);
  }

  if (overlayTop) {
    ids.push(overlayTop);
  }

  return [...new Set(ids)].slice(0, 3);
}

function renderExemplarSvg(descriptor: string): string {
  const cached = SVG_CACHE.get(descriptor);

  if (cached) {
    return cached;
  }

  const polylines: string[] = [];

  for (const token of descriptor.matchAll(TOKEN_REGEX)) {
    const [, kind, body] = token;
    const points = parsePoints(body);

    if (points.length < 2) {
      continue;
    }

    const pointsAttr = points.map((point) => `${point.x},${point.y}`).join(" ");
    polylines.push(`<polyline class="exemplar-${kind}" points="${pointsAttr}"></polyline>`);
  }

  const svg = `
    <svg viewBox="0 0 100 100" aria-hidden="true" focusable="false">
      ${polylines.join("")}
    </svg>
  `;

  SVG_CACHE.set(descriptor, svg);
  return svg;
}

function parsePoints(body: string): Array<{ x: number; y: number }> {
  return [...body.matchAll(POINT_REGEX)].map((match) => ({
    x: normalizeCoord(Number(match[1])),
    y: normalizeCoord(Number(match[2]))
  }));
}

function normalizeCoord(value: number): number {
  return Number((50 + value * 0.45).toFixed(2));
}
