import { describe, expect, it } from "vitest";
import { resolveMagicCardForTarget } from "../src/recognizer/datacards";
import {
  TUTORIAL_DEMO_STEPS,
  buildTutorialStepsFromCardMetadata,
  createTutorialStepFromCardMetadata,
  resolveNextTutorialStepIndex
} from "../src/demo/tutorial-flow";

describe("tutorial flow", () => {
  it("keeps the compact demo order with void_cut before martial_axis", () => {
    const voidCutIndex = TUTORIAL_DEMO_STEPS.findIndex((step) => step.id === "void_cut_trace");
    const martialAxisIndex = TUTORIAL_DEMO_STEPS.findIndex((step) => step.id === "martial_axis_trace");

    expect(voidCutIndex).toBeGreaterThanOrEqual(0);
    expect(martialAxisIndex).toBeGreaterThan(voidCutIndex);
    expect(TUTORIAL_DEMO_STEPS[martialAxisIndex]?.requiresExistingOperator).toBe("void_cut");
  });

  it("moves to the next unfinished step", () => {
    const completed = ["fire_trace", "fire_variation", "water_trace"];
    const nextIndex = resolveNextTutorialStepIndex(completed, 1);

    expect(TUTORIAL_DEMO_STEPS[nextIndex]?.id).toBe("water_variation");
  });

  it("describes each step with a simple shape summary and checklist", () => {
    for (const step of TUTORIAL_DEMO_STEPS) {
      expect(step.shapeSummary.length).toBeGreaterThan(0);
      expect(step.shapeChecklist.length).toBeGreaterThanOrEqual(2);
    }
  });

  it("reads built-in tutorial guide text from datacard metadata", () => {
    const fireCard = resolveMagicCardForTarget("family", "fire");
    const fireTrace = TUTORIAL_DEMO_STEPS.find((step) => step.id === "fire_trace");
    const voidCutCard = resolveMagicCardForTarget("operator", "void_cut");
    const voidCutTrace = TUTORIAL_DEMO_STEPS.find((step) => step.id === "void_cut_trace");

    expect(fireTrace?.shapeSummary).toBe(fireCard?.tutorial.summary);
    expect(fireTrace?.shapeChecklist).toEqual([...(fireCard?.tutorial.checklist ?? [])]);
    expect(voidCutTrace?.shapeSummary).toBe(voidCutCard?.tutorial.summary);
    expect(voidCutTrace?.requiresSealedBase).toBe(true);
  });

  it("builds family tutorial steps from card-like metadata", () => {
    const step = createTutorialStepFromCardMetadata({
      id: "fire",
      family: "fire",
      tutorial: {
        id: "fire_trace",
        shortLabel: "불꽃 따라",
        source: "trace",
        title: "불꽃형 따라 그리기",
        instruction: "불꽃 삼각형을 한 번 또렷하게 그린 뒤 저장해 주세요.",
        shapeSummary: "꼭짓점 3개가 보이는 닫힌 삼각형",
        shapeChecklist: ["끝이 이어져 닫혀 있어야 함", "한눈에 삼각형처럼 보여야 함"]
      }
    });

    expect(step).toEqual({
      id: "fire_trace",
      shortLabel: "불꽃 따라",
      kind: "family",
      source: "trace",
      title: "불꽃형 따라 그리기",
      instruction: "불꽃 삼각형을 한 번 또렷하게 그린 뒤 저장해 주세요.",
      shapeSummary: "꼭짓점 3개가 보이는 닫힌 삼각형",
      shapeChecklist: ["끝이 이어져 닫혀 있어야 함", "한눈에 삼각형처럼 보여야 함"],
      expectedFamily: "fire"
    });
  });

  it("builds operator tutorial steps with martial_axis-like dependencies", () => {
    const [voidCut, martialAxis] = buildTutorialStepsFromCardMetadata([
      {
        id: "void_cut",
        operator: "void_cut",
        tutorial: {
          id: "void_cut_trace",
          shortLabel: "절단",
          source: "trace",
          title: "공백 절단 연습",
          instruction: "기본 모양을 고정한 뒤 대각선 한 줄을 그려 저장해 주세요.",
          shapeSummary: "오른쪽 위에 짧게 긋는 대각선 한 획",
          shapeChecklist: ["기본 모양과 겹치지 않음", "한 획으로 짧고 또렷하게"],
          requiresSealedBase: true
        }
      },
      {
        id: "martial_axis",
        operator: "martial_axis",
        tutorial: {
          id: "martial_axis_trace",
          shortLabel: "축선 장식",
          source: "trace",
          title: "축선 장식 연습",
          instruction: "먼저 공백 절단을 기록한 뒤 축선 장식을 더해 저장해 주세요.",
          shapeSummary: "절단 장식 다음에 더하는 짧은 축선 장식",
          shapeChecklist: ["먼저 공백 절단이 있어야 함", "축을 덧대는 느낌으로 짧게"],
          requiresSealedBase: true
        },
        dependencies: {
          operators: ["void_cut"]
        }
      }
    ]);

    expect(voidCut?.expectedOperator).toBe("void_cut");
    expect(voidCut?.requiresSealedBase).toBe(true);
    expect(martialAxis).toMatchObject({
      id: "martial_axis_trace",
      kind: "operator",
      expectedOperator: "martial_axis",
      requiresSealedBase: true,
      requiresExistingOperator: "void_cut"
    });
  });
});
