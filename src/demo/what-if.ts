import {
  listBuiltInMagicCards,
  resolveMagicCardForTarget,
  type MagicCard,
  type MagicCardId,
  type MagicFamilyCard,
  type MagicOperatorCard,
  type MagicOperatorCardId
} from "../recognizer/datacards";
import type { OverlayAnchorZoneId, OverlayOperator } from "../recognizer/types";

export type MagicWhatIfDimension = "structure" | "relation" | "placement";
export type MagicWhatIfScenarioKind =
  | "family_shape_mutation"
  | "operator_anchor_movement"
  | "dependency_ordering"
  | "underscale_risk"
  | "off_anchor_risk";
export type MagicWhatIfRiskLevel = "low" | "medium" | "high";

export interface MagicWhatIfLane {
  id: "actual" | "what_if";
  label: string;
  copy: string;
  nonMutating: true;
  mutatesRecognizerDecision: false;
  changesActualDecision: false;
}

export interface MagicWhatIfImpact {
  riskLevel: MagicWhatIfRiskLevel;
  headline: string;
  detail: string;
  actionCopy: string;
  chips: readonly string[];
}

export interface MagicWhatIfDependency {
  operator: OverlayOperator;
  cardId: MagicOperatorCardId;
  label: string;
  copy: string;
}

export interface MagicWhatIfScenario {
  id: string;
  kind: MagicWhatIfScenarioKind;
  dimension: MagicWhatIfDimension;
  cardId: MagicCardId;
  target: MagicCard["target"];
  targetLabel: string;
  title: string;
  label: string;
  prompt: string;
  relatedCardIds: readonly MagicCardId[];
  requires?: MagicWhatIfDependency;
  impact: MagicWhatIfImpact;
  actualLane: MagicWhatIfLane;
  whatIfLane: MagicWhatIfLane;
  lanes: {
    actual: MagicWhatIfLane;
    whatIf: MagicWhatIfLane;
  };
}

const SAFE_UNKNOWN_SUMMARY = "아직 준비된 비교 카드가 없습니다. 현재 판정은 그대로 유지됩니다.";

const ANCHOR_LABELS: Record<OverlayAnchorZoneId, string> = {
  upper_left: "왼쪽 위",
  upper: "위쪽",
  upper_right: "오른쪽 위",
  left: "왼쪽",
  core: "가운데",
  right: "오른쪽",
  lower_left: "왼쪽 아래",
  lower: "아래쪽",
  lower_right: "오른쪽 아래"
};

export function buildMagicWhatIfScenarios(cards: readonly MagicCard[] = listBuiltInMagicCards()): MagicWhatIfScenario[] {
  return cards.flatMap((card) => (card.kind === "family" ? buildFamilyScenarios(card) : buildOperatorScenarios(card)));
}

export function resolveMagicWhatIfScenario(
  scenarioId: string,
  scenarios: readonly MagicWhatIfScenario[] = buildMagicWhatIfScenarios()
): MagicWhatIfScenario | undefined {
  return scenarios.find((scenario) => scenario.id === scenarioId);
}

export function summarizeWhatIfImpact(scenario: MagicWhatIfScenario | undefined | null): string;
export function summarizeWhatIfImpact(scenarioId: string, scenarios?: readonly MagicWhatIfScenario[]): string;
export function summarizeWhatIfImpact(
  scenarioOrId: MagicWhatIfScenario | string | undefined | null,
  scenarios: readonly MagicWhatIfScenario[] = buildMagicWhatIfScenarios()
): string {
  const scenario = typeof scenarioOrId === "string" ? resolveMagicWhatIfScenario(scenarioOrId, scenarios) : scenarioOrId;

  if (!scenario) {
    return SAFE_UNKNOWN_SUMMARY;
  }

  return [scenario.impact.headline, scenario.impact.detail, scenario.impact.actionCopy, scenario.actualLane.copy].join(" ");
}

function buildFamilyScenarios(card: MagicFamilyCard): MagicWhatIfScenario[] {
  return [
    createScenario({
      id: `structure:${card.family}:shape-mutation`,
      kind: "family_shape_mutation",
      dimension: "structure",
      card,
      label: "구조 실험",
      title: `${card.label} 실루엣 바꿔 보기`,
      prompt: `${card.shortLabel}의 닫힘, 모서리, 갈래를 살짝 바꾸면 어떤 기본형의 느낌으로 이동하는지 미리 살펴봅니다.`,
      relatedCardIds: [card.id],
      impact: {
        riskLevel: "medium",
        headline: `${card.label}의 핵심 구조가 달라지면 인상이 가까운 다른 기본형으로 이동할 수 있습니다.`,
        detail: card.tutorial.whatIfHints[0] ?? "작은 구조 변화도 사용자가 느끼는 결과감을 바꿀 수 있습니다.",
        actionCopy: "실제 판정은 유지한 채, 연습 카드에서만 변형 방향을 비교합니다.",
        chips: ["형태", "닫힘", "갈래"]
      },
      whatIfCopy: "선을 이어 닫거나 모서리 수를 바꾸는 상상 경로를 보여 줍니다."
    })
  ];
}

function buildOperatorScenarios(card: MagicOperatorCard): MagicWhatIfScenario[] {
  const scenarios = [buildAnchorMovementScenario(card), buildUnderscaleScenario(card), buildOffAnchorScenario(card)];

  return [...scenarios, ...card.dependencies.map((dependency) => buildDependencyScenario(card, dependency))];
}

function buildAnchorMovementScenario(card: MagicOperatorCard): MagicWhatIfScenario {
  const anchors = formatAnchorList(card.anchorHints);

  return createScenario({
    id: `placement:${card.operator}:anchor-move`,
    kind: "operator_anchor_movement",
    dimension: "placement",
    card,
    label: "자리 이동",
    title: `${card.label} 위치 옮겨 보기`,
    prompt: `${card.shortLabel}을 ${anchors} 근처에서 조금 옮겼을 때 의도가 어떻게 달라 보이는지 살펴봅니다.`,
    relatedCardIds: [card.id],
    impact: {
      riskLevel: "medium",
      headline: `${card.label}은 기본형과의 상대 위치가 의도 전달에 중요합니다.`,
      detail: "권장 자리에서 너무 멀어지면 사용자는 장식이 어느 부분에 붙는지 헷갈릴 수 있습니다.",
      actionCopy: "기본형을 고정해 둔 채 위치만 비교해 보세요.",
      chips: ["위치", "기준점", "읽기 쉬움"]
    },
    whatIfCopy: "장식을 주변 자리로 옮겨 보며 가장 자연스러운 붙임새를 찾습니다."
  });
}

function buildUnderscaleScenario(card: MagicOperatorCard): MagicWhatIfScenario {
  return createScenario({
    id: `placement:${card.operator}:underscale-risk`,
    kind: "underscale_risk",
    dimension: "placement",
    card,
    label: "작게 그릴 때",
    title: `${card.label} 크기 줄여 보기`,
    prompt: `${card.shortLabel}을 너무 작게 그리면 장식 의도가 충분히 보이는지 확인합니다.`,
    relatedCardIds: [card.id],
    impact: {
      riskLevel: "medium",
      headline: `${card.label}이 지나치게 작으면 장식보다 흔적처럼 느껴질 수 있습니다.`,
      detail: card.tutorial.whatIfHints.find((hint) => hint.includes("작")) ?? "크기가 줄어들수록 방향, 닫힘, 꺾임 같은 단서가 약해집니다.",
      actionCopy: "연습 카드에서는 크기만 바꿔 보며 여전히 의도가 보이는 지점을 찾습니다.",
      chips: ["크기", "선명도", "단서"]
    },
    whatIfCopy: "작게 그린 장식이 사용자 눈에 충분히 남는지 비교합니다."
  });
}

function buildOffAnchorScenario(card: MagicOperatorCard): MagicWhatIfScenario {
  const anchors = formatAnchorList(card.anchorHints);

  return createScenario({
    id: `placement:${card.operator}:off-anchor-risk`,
    kind: "off_anchor_risk",
    dimension: "placement",
    card,
    label: "붙임새 확인",
    title: `${card.label} 기준점 벗어나 보기`,
    prompt: `${card.shortLabel}이 ${anchors}에서 벗어났을 때 기본형과 한 세트로 느껴지는지 살펴봅니다.`,
    relatedCardIds: [card.id],
    impact: {
      riskLevel: "high",
      headline: `${card.label}이 기준 자리에서 벗어나면 별도 낙서처럼 보일 수 있습니다.`,
      detail: "기본형과 장식 사이의 거리가 커질수록 하나의 마법진으로 묶이는 느낌이 약해집니다.",
      actionCopy: "실제 결과는 바꾸지 않고, 안내 카드에서만 떨어진 배치를 비교합니다.",
      chips: ["붙임새", "거리", "한 세트"]
    },
    whatIfCopy: "장식을 기준 자리 밖으로 보내 보며 어디부터 어색한지 확인합니다."
  });
}

function buildDependencyScenario(card: MagicOperatorCard, dependency: OverlayOperator): MagicWhatIfScenario {
  const dependencyCard = resolveMagicCardForTarget("operator", dependency);
  const dependencyLabel = dependencyCard?.label ?? "선행 장식";
  const dependencyCardId = `operator:${dependency}` as MagicOperatorCardId;

  return createScenario({
    id: `relation:${card.operator}:requires-${dependency}`,
    kind: "dependency_ordering",
    dimension: "relation",
    card,
    label: "순서 확인",
    title: `${card.label} 순서 맞춰 보기`,
    prompt: `${card.shortLabel}은 ${dependencyLabel}을 먼저 남겼을 때 한 세트의 후속 장식으로 이해됩니다.`,
    relatedCardIds: [dependencyCardId, card.id],
    requires: {
      operator: dependency,
      cardId: dependencyCardId,
      label: dependencyLabel,
      copy: `${dependencyLabel}을 먼저 만든 뒤 ${card.label}을 더하는 흐름입니다.`
    },
    impact: {
      riskLevel: "high",
      headline: `${card.label}은 ${dependencyLabel}이 먼저 있어야 의도가 자연스럽게 이어집니다.`,
      detail: "순서가 바뀌면 후속 결합이 아니라 독립된 장식처럼 보일 수 있습니다.",
      actionCopy: "이 비교는 연습 흐름만 설명하며 현재 판정은 그대로 둡니다.",
      chips: ["순서", "선행 장식", "결합"]
    },
    whatIfCopy: `${dependencyLabel} 없이 바로 그렸을 때와 먼저 남긴 뒤 더했을 때의 차이를 비교합니다.`
  });
}

function createScenario(input: {
  id: string;
  kind: MagicWhatIfScenarioKind;
  dimension: MagicWhatIfDimension;
  card: MagicCard;
  label: string;
  title: string;
  prompt: string;
  relatedCardIds: readonly MagicCardId[];
  requires?: MagicWhatIfDependency;
  impact: MagicWhatIfImpact;
  whatIfCopy: string;
}): MagicWhatIfScenario {
  const actualLane = createActualLane();
  const whatIfLane = createWhatIfLane(input.whatIfCopy);

  return {
    id: input.id,
    kind: input.kind,
    dimension: input.dimension,
    cardId: input.card.id,
    target: input.card.target,
    targetLabel: input.card.label,
    title: input.title,
    label: input.label,
    prompt: input.prompt,
    relatedCardIds: input.relatedCardIds,
    requires: input.requires,
    impact: input.impact,
    actualLane,
    whatIfLane,
    lanes: {
      actual: actualLane,
      whatIf: whatIfLane
    }
  };
}

function createActualLane(): MagicWhatIfLane {
  return {
    id: "actual",
    label: "현재 결과 유지",
    copy: "캔버스의 실제 판정과 저장된 결과는 바꾸지 않습니다.",
    nonMutating: true,
    mutatesRecognizerDecision: false,
    changesActualDecision: false
  };
}

function createWhatIfLane(copy: string): MagicWhatIfLane {
  return {
    id: "what_if",
    label: "상상 비교",
    copy,
    nonMutating: true,
    mutatesRecognizerDecision: false,
    changesActualDecision: false
  };
}

function formatAnchorList(anchorHints: readonly OverlayAnchorZoneId[]): string {
  const labels = anchorHints.map((anchor) => ANCHOR_LABELS[anchor]);

  if (labels.length <= 1) {
    return labels[0] ?? "기준 자리";
  }

  return `${labels.slice(0, -1).join(", ")} 또는 ${labels[labels.length - 1]}`;
}
