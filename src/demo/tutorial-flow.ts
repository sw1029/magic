import { resolveMagicCardForTarget, type MagicCard } from "../recognizer/datacards";
import type { GlyphFamily, OverlayOperator, TutorialCaptureSource } from "../recognizer/types";

export type TutorialDemoStepKind = "family" | "operator";

export interface TutorialDemoStep {
  id: string;
  shortLabel: string;
  kind: TutorialDemoStepKind;
  source: TutorialCaptureSource;
  title: string;
  instruction: string;
  shapeSummary: string;
  shapeChecklist: string[];
  expectedFamily?: GlyphFamily;
  expectedOperator?: OverlayOperator;
  requiresSealedBase?: boolean;
  requiresExistingOperator?: OverlayOperator;
}

export interface TutorialCardDependencyMetadata {
  requiresSealedBase?: boolean;
  requiresExistingOperator?: OverlayOperator;
  existingOperator?: OverlayOperator;
  operator?: OverlayOperator;
  operators?: readonly OverlayOperator[];
  operatorIds?: readonly OverlayOperator[];
}

type TutorialCardDependencies = TutorialCardDependencyMetadata | readonly OverlayOperator[];

export interface TutorialCardMetadata {
  id?: string;
  stepId?: string;
  shortLabel: string;
  source: TutorialCaptureSource;
  title: string;
  instruction: string;
  shapeSummary: string;
  shapeChecklist: readonly string[];
  requiresSealedBase?: boolean;
  requiresExistingOperator?: OverlayOperator;
  dependencies?: TutorialCardDependencies;
}

export interface TutorialCardLikeMetadata {
  id?: string;
  slug?: string;
  kind?: TutorialDemoStepKind;
  family?: GlyphFamily;
  operator?: OverlayOperator;
  tutorial: TutorialCardMetadata;
  dependencies?: TutorialCardDependencies;
}

function resolveTutorialCardKind(card: TutorialCardLikeMetadata): TutorialDemoStepKind {
  return card.kind ?? (card.operator ? "operator" : "family");
}

function resolveRequiredExistingOperator(
  dependencies?: TutorialCardDependencies
): OverlayOperator | undefined {
  if (!dependencies) {
    return undefined;
  }

  if (isOperatorDependencyList(dependencies)) {
    return dependencies[0];
  }

  return (
    dependencies.requiresExistingOperator ??
    dependencies.existingOperator ??
    dependencies.operator ??
    dependencies.operators?.[0] ??
    dependencies.operatorIds?.[0]
  );
}

function isOperatorDependencyList(dependencies: TutorialCardDependencies): dependencies is readonly OverlayOperator[] {
  return Array.isArray(dependencies);
}

function resolveRequiresSealedBase(dependencies?: TutorialCardDependencies): boolean | undefined {
  if (!dependencies || isOperatorDependencyList(dependencies)) {
    return undefined;
  }

  return dependencies.requiresSealedBase;
}

export function createTutorialStepFromCardMetadata(card: TutorialCardLikeMetadata): TutorialDemoStep {
  const kind = resolveTutorialCardKind(card);
  const requiresSealedBase =
    card.tutorial.requiresSealedBase ??
    resolveRequiresSealedBase(card.tutorial.dependencies) ??
    resolveRequiresSealedBase(card.dependencies);
  const requiresExistingOperator =
    card.tutorial.requiresExistingOperator ??
    resolveRequiredExistingOperator(card.tutorial.dependencies) ??
    resolveRequiredExistingOperator(card.dependencies);

  const step: TutorialDemoStep = {
    id: card.tutorial.stepId ?? card.tutorial.id ?? card.id ?? card.slug ?? `${card.operator ?? card.family}_${card.tutorial.source}`,
    shortLabel: card.tutorial.shortLabel,
    kind,
    source: card.tutorial.source,
    title: card.tutorial.title,
    instruction: card.tutorial.instruction,
    shapeSummary: card.tutorial.shapeSummary,
    shapeChecklist: [...card.tutorial.shapeChecklist]
  };

  if (kind === "family" && card.family) {
    step.expectedFamily = card.family;
  }

  if (kind === "operator" && card.operator) {
    step.expectedOperator = card.operator;
  }

  if (requiresSealedBase !== undefined) {
    step.requiresSealedBase = requiresSealedBase;
  }

  if (requiresExistingOperator) {
    step.requiresExistingOperator = requiresExistingOperator;
  }

  return step;
}

export function buildTutorialStepsFromCardMetadata(cards: readonly TutorialCardLikeMetadata[]): TutorialDemoStep[] {
  return cards.map(createTutorialStepFromCardMetadata);
}

export function buildPreviewTutorialSteps(
  baseSteps: readonly TutorialDemoStep[],
  previewCards: readonly MagicCard[]
): TutorialDemoStep[] {
  return baseSteps.map((step) => {
    const previewCard = resolvePreviewCardForStep(step, previewCards);

    if (!previewCard) {
      return cloneTutorialStep(step);
    }

    const useCuratedVariationCopy = step.source === "variation";

    return {
      ...cloneTutorialStep(step),
      title: useCuratedVariationCopy ? step.title : previewCard.tutorial.title,
      instruction: useCuratedVariationCopy ? step.instruction : previewCard.tutorial.instruction,
      shapeSummary: previewCard.tutorial.summary,
      shapeChecklist: [...previewCard.tutorial.checklist]
    };
  });
}

function resolvePreviewCardForStep(step: TutorialDemoStep, cards: readonly MagicCard[]): MagicCard | undefined {
  if (step.kind === "family" && step.expectedFamily) {
    return cards.find((card) => card.kind === "family" && card.family === step.expectedFamily);
  }

  if (step.kind === "operator" && step.expectedOperator) {
    return cards.find((card) => card.kind === "operator" && card.operator === step.expectedOperator);
  }

  return undefined;
}

function cloneTutorialStep(step: TutorialDemoStep): TutorialDemoStep {
  return {
    ...step,
    shapeChecklist: [...step.shapeChecklist]
  };
}

interface BuiltInTutorialStepOverrides {
  id?: string;
  shortLabel?: string;
  title?: string;
  instruction?: string;
  shapeSummary?: string;
  shapeChecklist?: readonly string[];
  requiresSealedBase?: boolean;
  requiresExistingOperator?: OverlayOperator;
}

function createBuiltInTutorialStep(
  kind: "family",
  label: GlyphFamily,
  source: TutorialCaptureSource,
  overrides?: BuiltInTutorialStepOverrides
): TutorialDemoStep;
function createBuiltInTutorialStep(
  kind: "operator",
  label: OverlayOperator,
  source: TutorialCaptureSource,
  overrides?: BuiltInTutorialStepOverrides
): TutorialDemoStep;
function createBuiltInTutorialStep(
  kind: TutorialDemoStepKind,
  label: GlyphFamily | OverlayOperator,
  source: TutorialCaptureSource,
  overrides: BuiltInTutorialStepOverrides = {}
): TutorialDemoStep {
  const card =
    kind === "family"
      ? resolveMagicCardForTarget("family", label as GlyphFamily)
      : resolveMagicCardForTarget("operator", label as OverlayOperator);

  if (!card) {
    throw new Error(`unknown built-in tutorial card: ${kind}:${label}`);
  }

  const dependencies = kind === "operator" && "dependencies" in card ? card.dependencies : undefined;

  return createTutorialStepFromCardMetadata({
    id: overrides.id ?? `${label}_${source}`,
    kind,
    family: kind === "family" ? (label as GlyphFamily) : undefined,
    operator: kind === "operator" ? (label as OverlayOperator) : undefined,
    dependencies,
    tutorial: {
      shortLabel: overrides.shortLabel ?? card.shortLabel,
      source,
      title: overrides.title ?? card.tutorial.title,
      instruction: overrides.instruction ?? card.tutorial.instruction,
      shapeSummary: overrides.shapeSummary ?? card.tutorial.summary,
      shapeChecklist: overrides.shapeChecklist ?? card.tutorial.checklist,
      requiresSealedBase: overrides.requiresSealedBase ?? (kind === "operator" ? true : undefined),
      requiresExistingOperator: overrides.requiresExistingOperator
    }
  });
}

export const TUTORIAL_DEMO_STEPS: TutorialDemoStep[] = [
  createBuiltInTutorialStep("family", "fire", "trace", {
    id: "fire_trace",
    shortLabel: "불꽃 따라",
    title: "불꽃형 따라 그리기",
    instruction: "불꽃 삼각형을 한 번 또렷하게 그린 뒤 현재 입력을 연습에 저장해 주세요."
  }),
  createBuiltInTutorialStep("family", "fire", "variation", {
    id: "fire_variation",
    shortLabel: "불꽃 변형",
    title: "불꽃형 자연스럽게 다시 그리기",
    instruction: "같은 불꽃형을 조금 더 자연스럽게 다시 그린 뒤 저장해 주세요.",
    shapeSummary: "같은 불꽃 삼각형을 조금 더 편한 손맛으로 다시 그리기",
    shapeChecklist: ["모양은 그대로 유지", "닫힘은 그대로 유지", "속도나 압력만 조금 달라도 괜찮음"]
  }),
  createBuiltInTutorialStep("family", "water", "trace", {
    id: "water_trace",
    shortLabel: "물 따라",
    title: "물형 따라 그리기",
    instruction: "닫힌 루프가 보이도록 물형을 한 번 그린 뒤 저장해 주세요."
  }),
  createBuiltInTutorialStep("family", "water", "variation", {
    id: "water_variation",
    shortLabel: "물 변형",
    title: "물형 변형 그리기",
    instruction: "같은 물형을 약간 다른 속도로 다시 그려 저장해 주세요.",
    shapeSummary: "같은 둥근 고리를 다른 리듬으로 한 번 더",
    shapeChecklist: ["루프는 계속 닫혀 있어야 함", "모양은 동그란 흐름 유지", "속도 차이만 있어도 괜찮음"]
  }),
  createBuiltInTutorialStep("family", "earth", "trace", {
    id: "earth_trace",
    shortLabel: "땅 따라",
    title: "땅형 따라 그리기",
    instruction: "아래가 넓게 닫히는 땅형을 그린 뒤 저장해 주세요."
  }),
  createBuiltInTutorialStep("operator", "void_cut", "trace", {
    id: "void_cut_trace",
    shortLabel: "절단",
    title: "공백 절단 연습",
    instruction: "기본 모양을 하나 고정한 뒤, 오른쪽 위에 대각선 한 줄을 그려 저장해 주세요."
  }),
  createBuiltInTutorialStep("operator", "electric_fork", "trace", {
    id: "electric_fork_trace",
    shortLabel: "갈래 번개",
    title: "갈래 번개 연습",
    instruction: "기본 모양을 고정한 상태에서 갈래 번개를 그리고 저장해 주세요."
  }),
  createBuiltInTutorialStep("operator", "martial_axis", "trace", {
    id: "martial_axis_trace",
    shortLabel: "축선 장식",
    title: "축선 장식 연습",
    instruction: "먼저 공백 절단을 기록한 뒤, 그 다음 축선 장식을 더하고 저장해 주세요."
  })
];
export function resolveNextTutorialStepIndex(completedStepIds: string[], currentIndex: number): number {
  const completed = new Set(completedStepIds);

  for (let index = currentIndex + 1; index < TUTORIAL_DEMO_STEPS.length; index += 1) {
    if (!completed.has(TUTORIAL_DEMO_STEPS[index].id)) {
      return index;
    }
  }

  for (let index = 0; index < TUTORIAL_DEMO_STEPS.length; index += 1) {
    if (!completed.has(TUTORIAL_DEMO_STEPS[index].id)) {
      return index;
    }
  }

  return currentIndex;
}
