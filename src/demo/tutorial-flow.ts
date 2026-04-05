import type { GlyphFamily, OverlayOperator, TutorialCaptureSource } from "../recognizer/types";

export type TutorialDemoStepKind = "family" | "operator";

export interface TutorialDemoStep {
  id: string;
  shortLabel: string;
  kind: TutorialDemoStepKind;
  source: TutorialCaptureSource;
  title: string;
  instruction: string;
  expectedFamily?: GlyphFamily;
  expectedOperator?: OverlayOperator;
  requiresSealedBase?: boolean;
  requiresExistingOperator?: OverlayOperator;
}

export const TUTORIAL_DEMO_STEPS: TutorialDemoStep[] = [
  {
    id: "fire_trace",
    shortLabel: "불꽃 따라",
    kind: "family",
    source: "trace",
    title: "불꽃형 따라 그리기",
    instruction: "불꽃 삼각형을 한 번 또렷하게 그린 뒤 현재 입력을 연습에 저장해 주세요.",
    expectedFamily: "fire"
  },
  {
    id: "fire_variation",
    shortLabel: "불꽃 변형",
    kind: "family",
    source: "variation",
    title: "불꽃형 자연스럽게 다시 그리기",
    instruction: "같은 불꽃형을 조금 더 자연스럽게 다시 그린 뒤 저장해 주세요.",
    expectedFamily: "fire"
  },
  {
    id: "water_trace",
    shortLabel: "물 따라",
    kind: "family",
    source: "trace",
    title: "물형 따라 그리기",
    instruction: "닫힌 루프가 보이도록 물형을 한 번 그린 뒤 저장해 주세요.",
    expectedFamily: "water"
  },
  {
    id: "water_variation",
    shortLabel: "물 변형",
    kind: "family",
    source: "variation",
    title: "물형 변형 그리기",
    instruction: "같은 물형을 약간 다른 속도로 다시 그려 저장해 주세요.",
    expectedFamily: "water"
  },
  {
    id: "earth_trace",
    shortLabel: "땅 따라",
    kind: "family",
    source: "trace",
    title: "땅형 따라 그리기",
    instruction: "아래가 넓게 닫히는 땅형을 그린 뒤 저장해 주세요.",
    expectedFamily: "earth"
  },
  {
    id: "void_cut_trace",
    shortLabel: "절단",
    kind: "operator",
    source: "trace",
    title: "공백 절단 연습",
    instruction: "기본 모양을 하나 고정한 뒤, 오른쪽 위에 대각선 한 줄을 그려 저장해 주세요.",
    expectedOperator: "void_cut",
    requiresSealedBase: true
  },
  {
    id: "electric_fork_trace",
    shortLabel: "갈래 번개",
    kind: "operator",
    source: "trace",
    title: "갈래 번개 연습",
    instruction: "기본 모양을 고정한 상태에서 갈래 번개를 그리고 저장해 주세요.",
    expectedOperator: "electric_fork",
    requiresSealedBase: true
  },
  {
    id: "martial_axis_trace",
    shortLabel: "축선 장식",
    kind: "operator",
    source: "trace",
    title: "축선 장식 연습",
    instruction: "먼저 공백 절단을 기록한 뒤, 그 다음 축선 장식을 더하고 저장해 주세요.",
    expectedOperator: "martial_axis",
    requiresSealedBase: true,
    requiresExistingOperator: "void_cut"
  }
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
