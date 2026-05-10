import type { GlyphFamily, OverlayAnchorZoneId, OverlayOperator, Stroke } from "./types";

export type MagicCardKind = "family" | "operator";
export type MagicFamilyCardId = `family:${GlyphFamily}`;
export type MagicOperatorCardId = `operator:${OverlayOperator}`;
export type MagicCardId = MagicFamilyCardId | MagicOperatorCardId;
export type MagicCardSetCompatibilityStatus = "ready" | "label_mismatch" | "card_set_mismatch";

export interface MagicTutorialMetadata {
  title: string;
  instruction: string;
  summary: string;
  checklist: readonly string[];
  emergentPrompts: readonly string[];
  whatIfHints: readonly string[];
}

export interface MagicCardBase<TKind extends MagicCardKind, TLabel extends GlyphFamily | OverlayOperator> {
  id: MagicCardId;
  kind: TKind;
  label: string;
  shortLabel: string;
  target: {
    kind: TKind;
    label: TLabel;
  };
  tutorial: MagicTutorialMetadata;
}

export interface MagicFamilyCard extends MagicCardBase<"family", GlyphFamily> {
  id: MagicFamilyCardId;
  family: GlyphFamily;
  recognitionHints: {
    strokeCount: readonly [number, number];
    closed: boolean;
    shapeKeywords: readonly string[];
    definitionPattern?: string;
    featureHints?: Partial<
      Record<
        "corners" | "endpointClusters" | "circularity" | "fillRatio" | "parallelism",
        readonly [number, number]
      >
    >;
    exampleTemplate?: readonly Stroke[];
  };
}

export interface MagicOperatorCard extends MagicCardBase<"operator", OverlayOperator> {
  id: MagicOperatorCardId;
  operator: OverlayOperator;
  dependencies: readonly OverlayOperator[];
  anchorHints: readonly OverlayAnchorZoneId[];
}

export type MagicCard = MagicFamilyCard | MagicOperatorCard;

export interface MagicCardSetSignature {
  cardSetId: string;
  cardSetHash: string;
  cardCount: number;
  familyLabels: readonly GlyphFamily[];
  operatorLabels: readonly OverlayOperator[];
  cardIds: readonly MagicCardId[];
}

export interface MagicCardSetSignatureLike {
  cardSetId?: string;
  cardSetHash?: string;
  hash?: string;
  signatureHash?: string;
  familyLabels?: readonly string[];
  families?: readonly string[];
  operatorLabels?: readonly string[];
  operators?: readonly string[];
  cardIds?: readonly string[];
}

export interface MagicCardSetCompatibility {
  status: MagicCardSetCompatibilityStatus;
  ready: boolean;
  expected: MagicCardSetSignature;
  actual?: MagicCardSetSignatureLike;
  reasons: readonly string[];
  labelDiff: {
    missingFamilies: readonly GlyphFamily[];
    unexpectedFamilies: readonly string[];
    missingOperators: readonly OverlayOperator[];
    unexpectedOperators: readonly string[];
  };
}

export const BUILT_IN_MAGIC_CARD_SET_ID = "magic-recognizer-v1.5/built-in-datacards";

export const BUILT_IN_MAGIC_FAMILY_LABELS = ["wind", "earth", "fire", "water", "life"] as const satisfies readonly GlyphFamily[];
export const BUILT_IN_MAGIC_OPERATOR_LABELS = [
  "steel_brace",
  "electric_fork",
  "ice_bar",
  "soul_dot",
  "void_cut",
  "martial_axis"
] as const satisfies readonly OverlayOperator[];

const BUILT_IN_FAMILY_CARDS = [
  {
    id: "family:wind",
    kind: "family",
    family: "wind",
    label: "바람형",
    shortLabel: "바람",
    target: { kind: "family", label: "wind" },
    tutorial: {
      title: "바람형 세 줄 리듬",
      instruction: "세 개의 열린 가로획을 서로 평행하게 띄워 그려 주세요.",
      summary: "닫히지 않은 세 줄이 같은 방향으로 흐르는 기본형입니다.",
      checklist: ["세 획이 분리되어 있음", "대체로 평행함", "루프처럼 닫히지 않음"],
      emergentPrompts: ["세 줄 사이 간격이 일정하면 판독이 안정됩니다.", "가장 위와 아래 획이 같은 길이에 가까운지 확인해 보세요."],
      whatIfHints: ["두 줄만 남으면 입력이 미완성으로 보일 수 있습니다.", "줄 끝을 이어 닫으면 물형이나 땅형과 혼동될 수 있습니다."]
    },
    recognitionHints: {
      strokeCount: [3, 3],
      closed: false,
      shapeKeywords: ["parallel", "open", "three-stroke"]
    }
  },
  {
    id: "family:earth",
    kind: "family",
    family: "earth",
    label: "땅형",
    shortLabel: "땅",
    target: { kind: "family", label: "earth" },
    tutorial: {
      title: "땅형 사다리꼴 닫힘",
      instruction: "아래가 넓고 위가 조금 좁은 닫힌 사다리꼴을 그려 주세요.",
      summary: "네 모서리와 닫힘이 핵심인 안정적인 기반형입니다.",
      checklist: ["아래 변이 위 변보다 길어 보임", "네 모서리가 읽힘", "시작점과 끝점이 닫힘"],
      emergentPrompts: ["아래쪽을 살짝 넓히면 땅형의 무게감이 살아납니다.", "윗변을 너무 뾰족하게 만들지 않는 것이 좋습니다."],
      whatIfHints: ["꼭짓점이 세 개처럼 보이면 불꽃형으로 흔들릴 수 있습니다.", "닫힘이 벌어지면 바람형처럼 열린 입력으로 보일 수 있습니다."]
    },
    recognitionHints: {
      strokeCount: [1, 2],
      closed: true,
      shapeKeywords: ["trapezoid", "closed", "four-corner"]
    }
  },
  {
    id: "family:fire",
    kind: "family",
    family: "fire",
    label: "불꽃형",
    shortLabel: "불꽃",
    target: { kind: "family", label: "fire" },
    tutorial: {
      title: "불꽃형 삼각 닫힘",
      instruction: "위 꼭짓점이 또렷한 닫힌 삼각형을 한 번 그려 주세요.",
      summary: "세 꼭짓점과 닫힘으로 읽히는 날카로운 기본형입니다.",
      checklist: ["위쪽 꼭짓점이 하나로 모임", "밑변이 닫힘", "삼각형처럼 세 모서리가 보임"],
      emergentPrompts: ["꼭짓점에서 잠깐 방향을 꺾으면 불꽃형 신호가 강해집니다.", "밑변을 너무 둥글게 만들지 않는 것이 좋습니다."],
      whatIfHints: ["네 모서리처럼 보이면 땅형으로 흔들릴 수 있습니다.", "루프가 둥글어지면 물형과 가까워집니다."]
    },
    recognitionHints: {
      strokeCount: [1, 2],
      closed: true,
      shapeKeywords: ["triangle", "closed", "three-corner"]
    }
  },
  {
    id: "family:water",
    kind: "family",
    family: "water",
    label: "물형",
    shortLabel: "물",
    target: { kind: "family", label: "water" },
    tutorial: {
      title: "물형 둥근 루프",
      instruction: "끊기지 않고 한 바퀴 도는 둥근 닫힌 고리를 그려 주세요.",
      summary: "모서리보다 연속적인 곡률과 닫힘이 중요한 루프형입니다.",
      checklist: ["한 바퀴가 자연스럽게 이어짐", "모서리가 과하게 서지 않음", "시작점과 끝점이 만남"],
      emergentPrompts: ["속도를 일정하게 유지하면 물형의 곡선성이 잘 보입니다.", "처음과 끝을 부드럽게 겹쳐 보세요."],
      whatIfHints: ["각진 모서리가 많아지면 땅형이나 불꽃형으로 흔들릴 수 있습니다.", "루프가 열리면 닫힌 기본형으로 인정되기 어렵습니다."]
    },
    recognitionHints: {
      strokeCount: [1, 2],
      closed: true,
      shapeKeywords: ["loop", "round", "closed"]
    }
  },
  {
    id: "family:life",
    kind: "family",
    family: "life",
    label: "생명형",
    shortLabel: "생명",
    target: { kind: "family", label: "life" },
    tutorial: {
      title: "생명형 줄기와 갈래",
      instruction: "중심 줄기에서 두 갈래가 위로 뻗는 열린 형태를 그려 주세요.",
      summary: "닫힌 면보다 중심축과 양쪽 가지가 중요한 열린 기본형입니다.",
      checklist: ["중심 줄기가 보임", "두 갈래가 중심에서 갈라짐", "외곽을 닫지 않음"],
      emergentPrompts: ["중심 교차점을 또렷하게 남기면 생명형 신호가 강해집니다.", "양쪽 가지 길이를 비슷하게 맞춰 보세요."],
      whatIfHints: ["외곽을 닫아 버리면 물형이나 땅형처럼 보일 수 있습니다.", "가로 획만 남으면 바람형과 가까워질 수 있습니다."]
    },
    recognitionHints: {
      strokeCount: [1, 3],
      closed: false,
      shapeKeywords: ["stem", "branch", "open"]
    }
  }
] as const satisfies readonly MagicFamilyCard[];

const BUILT_IN_OPERATOR_CARDS = [
  {
    id: "operator:steel_brace",
    kind: "operator",
    operator: "steel_brace",
    label: "버팀 장식",
    shortLabel: "버팀",
    target: { kind: "operator", label: "steel_brace" },
    tutorial: {
      title: "버팀 장식 ㄷ자 앵커",
      instruction: "기본형 오른쪽에 열린 ㄷ자 모양의 짧은 장식을 더해 주세요.",
      summary: "오른쪽 가장자리에서 형태를 받쳐 주는 열린 브레이스입니다.",
      checklist: ["오른쪽 앵커 근처에 위치", "ㄷ자처럼 세 변이 보임", "기본형과 과하게 겹치지 않음"],
      emergentPrompts: ["오른쪽 위나 아래에 살짝 붙이면 의도가 잘 드러납니다.", "세 변의 꺾임을 분명하게 남겨 보세요."],
      whatIfHints: ["한 줄만 남으면 얼음 막대와 혼동될 수 있습니다.", "너무 작으면 혼 점처럼 보일 수 있습니다."]
    },
    dependencies: [],
    anchorHints: ["right", "lower_right", "upper_right"]
  },
  {
    id: "operator:electric_fork",
    kind: "operator",
    operator: "electric_fork",
    label: "갈래 번개",
    shortLabel: "번개",
    target: { kind: "operator", label: "electric_fork" },
    tutorial: {
      title: "갈래 번개 꺾임",
      instruction: "기본형 주변에 번개처럼 꺾이며 갈라지는 짧은 장식을 그려 주세요.",
      summary: "꺾임과 갈래가 함께 읽히는 에너지형 오버레이입니다.",
      checklist: ["한 번 이상 방향이 꺾임", "갈래가 한 줄 이상 보임", "상단 또는 오른쪽 앵커에 가까움"],
      emergentPrompts: ["중간에서 살짝 되돌아 나오는 갈래를 남기면 좋습니다.", "너무 둥글게 그리지 말고 각을 살려 보세요."],
      whatIfHints: ["완전히 직선이면 얼음 막대나 공백 절단으로 흔들릴 수 있습니다.", "점처럼 작아지면 혼 점으로 보일 수 있습니다."]
    },
    dependencies: [],
    anchorHints: ["upper_right", "upper", "right"]
  },
  {
    id: "operator:ice_bar",
    kind: "operator",
    operator: "ice_bar",
    label: "얼음 막대",
    shortLabel: "막대",
    target: { kind: "operator", label: "ice_bar" },
    tutorial: {
      title: "얼음 막대 수평선",
      instruction: "기본형 중심을 가로지르거나 가까운 곳에 곧은 가로선을 그려 주세요.",
      summary: "길고 곧은 수평성이 핵심인 선형 오버레이입니다.",
      checklist: ["가로 방향이 분명함", "한 획이 길게 유지됨", "중심 또는 좌우 앵커에 놓임"],
      emergentPrompts: ["선의 양 끝을 안정적으로 멈추면 막대성이 강해집니다.", "중심을 지나가게 두면 판독이 쉬워집니다."],
      whatIfHints: ["대각선으로 기울면 공백 절단과 가까워집니다.", "꺾임이 생기면 갈래 번개로 흔들릴 수 있습니다."]
    },
    dependencies: [],
    anchorHints: ["core", "left", "right"]
  },
  {
    id: "operator:soul_dot",
    kind: "operator",
    operator: "soul_dot",
    label: "혼 점",
    shortLabel: "점",
    target: { kind: "operator", label: "soul_dot" },
    tutorial: {
      title: "혼 점 작은 닫힘",
      instruction: "기본형 근처에 작고 닫힌 점 모양을 또렷하게 찍어 주세요.",
      summary: "크기가 작고 닫힌 국소 루프인 점 오버레이입니다.",
      checklist: ["작지만 닫힌 형태", "기본형과 구분되는 위치", "너무 길게 늘어나지 않음"],
      emergentPrompts: ["작은 원을 한 바퀴 닫는 느낌으로 그려 보세요.", "모서리보다 점의 면적감을 남기는 것이 좋습니다."],
      whatIfHints: ["크게 늘어나면 물형 루프처럼 보일 수 있습니다.", "닫히지 않으면 단순 노이즈나 짧은 선으로 처리될 수 있습니다."]
    },
    dependencies: [],
    anchorHints: ["core", "upper_left", "upper_right", "lower_left", "lower_right"]
  },
  {
    id: "operator:void_cut",
    kind: "operator",
    operator: "void_cut",
    label: "공백 절단",
    shortLabel: "절단",
    target: { kind: "operator", label: "void_cut" },
    tutorial: {
      title: "공백 절단 대각선",
      instruction: "기본형 오른쪽 위나 중심 근처에 짧은 대각선 한 획을 그려 주세요.",
      summary: "공간을 비스듬히 가르는 단일 대각선 오버레이입니다.",
      checklist: ["한 획으로 또렷함", "대각선 방향이 분명함", "상단 오른쪽 또는 중심 앵커에 가까움"],
      emergentPrompts: ["왼아래에서 오른위로 긋는 리듬을 일정하게 유지해 보세요.", "기본형과 살짝 떨어뜨리면 절단감이 살아납니다."],
      whatIfHints: ["수평에 가까우면 얼음 막대가 될 수 있습니다.", "꺾임이 들어가면 갈래 번개와 혼동될 수 있습니다."]
    },
    dependencies: [],
    anchorHints: ["upper_right", "core", "lower_left"]
  },
  {
    id: "operator:martial_axis",
    kind: "operator",
    operator: "martial_axis",
    label: "축선 장식",
    shortLabel: "축선",
    target: { kind: "operator", label: "martial_axis" },
    tutorial: {
      title: "축선 장식 후속 결합",
      instruction: "공백 절단이 이미 기록된 상태에서 세로축과 짧은 가로축을 더해 주세요.",
      summary: "void_cut 다음에만 활성화되는 축 기반 결합 오버레이입니다.",
      checklist: ["공백 절단이 먼저 존재", "세로축이 중심을 잡음", "짧은 가로축이 축을 가로지름"],
      emergentPrompts: ["먼저 절단을 성공시킨 뒤 같은 기준 프레임에 축을 더해 보세요.", "세로축과 가로축의 교차점을 또렷하게 남기면 좋습니다."],
      whatIfHints: ["공백 절단이 없으면 단독 장식으로는 준비되지 않습니다.", "가로선만 남으면 얼음 막대로 판독될 수 있습니다."]
    },
    dependencies: ["void_cut"],
    anchorHints: ["lower_right", "core", "right"]
  }
] as const satisfies readonly MagicOperatorCard[];

const BUILT_IN_MAGIC_CARDS = [...BUILT_IN_FAMILY_CARDS, ...BUILT_IN_OPERATOR_CARDS] as const satisfies readonly MagicCard[];

export function listBuiltInMagicCards(): readonly MagicCard[] {
  return BUILT_IN_MAGIC_CARDS;
}

export function listBuiltInFamilyCards(): readonly MagicFamilyCard[] {
  return BUILT_IN_FAMILY_CARDS;
}

export function listBuiltInOperatorCards(): readonly MagicOperatorCard[] {
  return BUILT_IN_OPERATOR_CARDS;
}

export function getMagicCardById(id: string): MagicCard | undefined {
  return BUILT_IN_MAGIC_CARDS.find((card) => card.id === id);
}

export function resolveMagicCardForTarget(kind: "family", label: string): MagicFamilyCard | undefined;
export function resolveMagicCardForTarget(kind: "operator", label: string): MagicOperatorCard | undefined;
export function resolveMagicCardForTarget(kind: MagicCardKind, label: string): MagicCard | undefined {
  if (kind === "family") {
    return isBuiltInFamilyLabel(label) ? BUILT_IN_FAMILY_CARDS.find((card) => card.family === label) : undefined;
  }

  return isBuiltInOperatorLabel(label) ? BUILT_IN_OPERATOR_CARDS.find((card) => card.operator === label) : undefined;
}

export function getBuiltInMagicCardSetId(): string {
  return BUILT_IN_MAGIC_CARD_SET_ID;
}

export function getBuiltInMagicCardSetHash(): string {
  return buildCardSetHash();
}

export function getBuiltInMagicCardSetSignature(): MagicCardSetSignature {
  const familyLabels = BUILT_IN_FAMILY_CARDS.map((card) => card.family);
  const operatorLabels = BUILT_IN_OPERATOR_CARDS.map((card) => card.operator);
  const cardIds = BUILT_IN_MAGIC_CARDS.map((card) => card.id);

  return {
    cardSetId: BUILT_IN_MAGIC_CARD_SET_ID,
    cardSetHash: buildCardSetHash(),
    cardCount: BUILT_IN_MAGIC_CARDS.length,
    familyLabels,
    operatorLabels,
    cardIds
  };
}

export function evaluateMagicCardSetCompatibility(
  candidate?: MagicCardSetSignatureLike | null,
  expected: MagicCardSetSignature = getBuiltInMagicCardSetSignature()
): MagicCardSetCompatibility {
  const actual: MagicCardSetSignatureLike | null = arguments.length === 0 || candidate === undefined ? expected : candidate;

  if (!actual) {
    return buildCompatibilityResult("card_set_mismatch", expected, undefined, ["No card set signature was provided."]);
  }

  const actualFamilyLabels = actual.familyLabels ?? actual.families;
  const actualOperatorLabels = actual.operatorLabels ?? actual.operators;
  const labelReasons: string[] = [];

  if (actualFamilyLabels && !arrayEquals(actualFamilyLabels, expected.familyLabels)) {
    labelReasons.push("Family labels do not match the built-in closed GlyphFamily set.");
  }

  if (actualOperatorLabels && !arrayEquals(actualOperatorLabels, expected.operatorLabels)) {
    labelReasons.push("Operator labels do not match the built-in closed OverlayOperator set.");
  }

  if (labelReasons.length > 0) {
    return buildCompatibilityResult("label_mismatch", expected, actual, labelReasons);
  }

  const actualHash = actual.cardSetHash ?? actual.hash ?? actual.signatureHash;
  const cardSetReasons: string[] = [];

  if (actual.cardSetId && actual.cardSetId !== expected.cardSetId) {
    cardSetReasons.push(`Card set id mismatch: expected ${expected.cardSetId}, received ${actual.cardSetId}.`);
  }

  if (actualHash && actualHash !== expected.cardSetHash) {
    cardSetReasons.push(`Card set hash mismatch: expected ${expected.cardSetHash}, received ${actualHash}.`);
  }

  if (actual.cardIds && !arrayEquals(actual.cardIds, expected.cardIds)) {
    cardSetReasons.push("Card ids do not match the built-in datacard registry.");
  }

  if (cardSetReasons.length > 0) {
    return buildCompatibilityResult("card_set_mismatch", expected, actual, cardSetReasons);
  }

  return buildCompatibilityResult("ready", expected, actual, ["Built-in magic datacard set is compatible."]);
}

function buildCompatibilityResult(
  status: MagicCardSetCompatibilityStatus,
  expected: MagicCardSetSignature,
  actual: MagicCardSetSignatureLike | undefined,
  reasons: readonly string[]
): MagicCardSetCompatibility {
  const actualFamilyLabels = actual?.familyLabels ?? actual?.families ?? expected.familyLabels;
  const actualOperatorLabels = actual?.operatorLabels ?? actual?.operators ?? expected.operatorLabels;

  return {
    status,
    ready: status === "ready",
    expected,
    actual,
    reasons,
    labelDiff: {
      missingFamilies: expected.familyLabels.filter((label) => !actualFamilyLabels.includes(label)),
      unexpectedFamilies: actualFamilyLabels.filter((label) => !isBuiltInFamilyLabel(label)),
      missingOperators: expected.operatorLabels.filter((label) => !actualOperatorLabels.includes(label)),
      unexpectedOperators: actualOperatorLabels.filter((label) => !isBuiltInOperatorLabel(label))
    }
  };
}

function isBuiltInFamilyLabel(label: string): label is GlyphFamily {
  return BUILT_IN_MAGIC_FAMILY_LABELS.includes(label as GlyphFamily);
}

function isBuiltInOperatorLabel(label: string): label is OverlayOperator {
  return BUILT_IN_MAGIC_OPERATOR_LABELS.includes(label as OverlayOperator);
}

function buildCardSetHash(): string {
  return `fnv1a32:${fnv1a32(stableStringify(buildCardSetHashSource()))}`;
}

function buildCardSetHashSource(): unknown {
  return {
    cardSetId: BUILT_IN_MAGIC_CARD_SET_ID,
    families: BUILT_IN_FAMILY_CARDS.map((card) => ({
      id: card.id,
      family: card.family,
      label: card.label,
      shortLabel: card.shortLabel,
      tutorial: card.tutorial,
      recognitionHints: card.recognitionHints
    })),
    operators: BUILT_IN_OPERATOR_CARDS.map((card) => ({
      id: card.id,
      operator: card.operator,
      label: card.label,
      shortLabel: card.shortLabel,
      tutorial: card.tutorial,
      dependencies: card.dependencies,
      anchorHints: card.anchorHints
    }))
  };
}

function stableStringify(value: unknown): string {
  if (Array.isArray(value)) {
    return `[${value.map((entry) => stableStringify(entry)).join(",")}]`;
  }

  if (value && typeof value === "object") {
    return `{${Object.keys(value)
      .sort()
      .map((key) => `${JSON.stringify(key)}:${stableStringify((value as Record<string, unknown>)[key])}`)
      .join(",")}}`;
  }

  return JSON.stringify(value) ?? "undefined";
}

function fnv1a32(value: string): string {
  let hash = 0x811c9dc5;

  for (let index = 0; index < value.length; index += 1) {
    hash ^= value.charCodeAt(index);
    hash = Math.imul(hash, 0x01000193) >>> 0;
  }

  return hash.toString(16).padStart(8, "0");
}

function arrayEquals(left: readonly string[], right: readonly string[]): boolean {
  return left.length === right.length && left.every((value, index) => value === right[index]);
}
