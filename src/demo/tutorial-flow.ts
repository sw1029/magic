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

export const TUTORIAL_DEMO_STEPS: TutorialDemoStep[] = [
  {
    id: "fire_trace",
    shortLabel: "불꽃 따라",
    kind: "family",
    source: "trace",
    title: "불꽃형 따라 그리기",
    instruction: "불꽃 삼각형을 한 번 또렷하게 그린 뒤 현재 입력을 연습에 저장해 주세요.",
    shapeSummary: "꼭짓점 3개가 보이는 닫힌 삼각형",
    shapeChecklist: ["끝이 이어져 닫혀 있어야 함", "위쪽 꼭짓점이 하나로 읽혀야 함", "한눈에 삼각형처럼 보여야 함"],
    expectedFamily: "fire"
  },
  {
    id: "fire_variation",
    shortLabel: "불꽃 변형",
    kind: "family",
    source: "variation",
    title: "불꽃형 자연스럽게 다시 그리기",
    instruction: "같은 불꽃형을 조금 더 자연스럽게 다시 그린 뒤 저장해 주세요.",
    shapeSummary: "같은 불꽃 삼각형을 조금 더 편한 손맛으로 다시 그리기",
    shapeChecklist: ["모양은 그대로 유지", "닫힘은 그대로 유지", "속도나 압력만 조금 달라도 괜찮음"],
    expectedFamily: "fire"
  },
  {
    id: "water_trace",
    shortLabel: "물 따라",
    kind: "family",
    source: "trace",
    title: "물형 따라 그리기",
    instruction: "닫힌 루프가 보이도록 물형을 한 번 그린 뒤 저장해 주세요.",
    shapeSummary: "끊기지 않고 한 바퀴 닫히는 둥근 고리",
    shapeChecklist: ["시작점과 끝점이 자연스럽게 만남", "모서리보다 둥근 흐름이 보임", "고리처럼 한 번에 읽힘"],
    expectedFamily: "water"
  },
  {
    id: "water_variation",
    shortLabel: "물 변형",
    kind: "family",
    source: "variation",
    title: "물형 변형 그리기",
    instruction: "같은 물형을 약간 다른 속도로 다시 그려 저장해 주세요.",
    shapeSummary: "같은 둥근 고리를 다른 리듬으로 한 번 더",
    shapeChecklist: ["루프는 계속 닫혀 있어야 함", "모양은 동그란 흐름 유지", "속도 차이만 있어도 괜찮음"],
    expectedFamily: "water"
  },
  {
    id: "earth_trace",
    shortLabel: "땅 따라",
    kind: "family",
    source: "trace",
    title: "땅형 따라 그리기",
    instruction: "아래가 넓게 닫히는 땅형을 그린 뒤 저장해 주세요.",
    shapeSummary: "아래가 넓고 위가 조금 좁은 닫힌 사다리꼴",
    shapeChecklist: ["아래 변이 더 길어 보임", "사각형보다 윗변이 조금 짧음", "끝이 닫혀 있어야 함"],
    expectedFamily: "earth"
  },
  {
    id: "void_cut_trace",
    shortLabel: "절단",
    kind: "operator",
    source: "trace",
    title: "공백 절단 연습",
    instruction: "기본 모양을 하나 고정한 뒤, 오른쪽 위에 대각선 한 줄을 그려 저장해 주세요.",
    shapeSummary: "오른쪽 위에 짧게 긋는 대각선 한 획",
    shapeChecklist: ["기본 모양과 겹치지 않게 살짝 떨어짐", "왼아래에서 오른위로 기울어짐", "한 획으로 짧고 또렷하게"],
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
    shapeSummary: "번개처럼 꺾이며 갈래가 보이는 짧은 장식",
    shapeChecklist: ["한 번 이상 꺾이는 느낌", "직선 한 줄보다 갈래가 읽혀야 함", "기본 모양 옆에서 독립적으로 보임"],
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
    shapeSummary: "절단 장식 다음에 더하는 짧은 축선 장식",
    shapeChecklist: ["먼저 공백 절단이 있어야 함", "축을 덧대는 느낌으로 짧게", "규칙상 단독으로는 읽히지 않음"],
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
