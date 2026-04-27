export type DemoViewPreset = "clean" | "explain" | "workshop";
export type DemoPage = "test" | "tutorial" | "ml" | "quality" | "guide" | "logs";
export type GuidedDemoScenarioId =
  | "same_shape_fast"
  | "same_shape_slow"
  | "closure_leak"
  | "rotation_bias";

export interface DemoViewState {
  viewPreset: DemoViewPreset;
  activePage: DemoPage;
  analysisOverlay: boolean;
  qualityInfluence: boolean;
  explainResult: boolean;
  compareMode: boolean;
  guidanceMode: boolean;
  showRecentSeals: boolean;
  showQualitySplit: boolean;
  showTutorialFlowPanel: boolean;
  showPersonalizationPanel: boolean;
  showExemplarPanel: boolean;
  showProfilePanel: boolean;
  showLogViewer: boolean;
  selectedScenarioId: GuidedDemoScenarioId;
}

export interface GuidedDemoScenario {
  id: GuidedDemoScenarioId;
  label: string;
  title: string;
  prompt: string;
}

export const GUIDED_DEMO_SCENARIOS: GuidedDemoScenario[] = [
  {
    id: "same_shape_fast",
    label: "빠른 불꽃",
    title: "같은 모양, 빠른 입력",
    prompt: "같은 기본 모양을 빠르게 그려서 품질 반영이 결과감만 바꾸고 종류는 유지되는지 보여줍니다."
  },
  {
    id: "same_shape_slow",
    label: "느린 불꽃",
    title: "같은 모양, 느린 입력",
    prompt: "같은 모양을 천천히 반복해서 결과감과 위험 신호만 달라지고 종류는 그대로인지 설명합니다."
  },
  {
    id: "closure_leak",
    label: "끝이 열린 모양",
    title: "끝이 열린 모양",
    prompt: "도형 끝을 일부러 열어 미완성 상태와 이유 설명을 보여줍니다."
  },
  {
    id: "rotation_bias",
    label: "기울어진 모양",
    title: "기울어진 모양",
    prompt: "같은 종류를 기울여 그리고 분석 안내선으로 축, 보조 가이드, 위험도 변화를 함께 설명합니다."
  }
];

export function createDemoViewState(preset: DemoViewPreset = "clean"): DemoViewState {
  return applyDemoViewPreset(
    {
      viewPreset: preset,
      activePage: "test",
      analysisOverlay: false,
      qualityInfluence: true,
      explainResult: false,
      compareMode: true,
      guidanceMode: true,
      showRecentSeals: true,
      showQualitySplit: false,
      showTutorialFlowPanel: false,
      showPersonalizationPanel: false,
      showExemplarPanel: false,
      showProfilePanel: false,
      showLogViewer: false,
      selectedScenarioId: "same_shape_fast"
    },
    preset
  );
}

export function applyDemoViewPreset(state: DemoViewState, preset: DemoViewPreset): DemoViewState {
  switch (preset) {
    case "clean":
      return {
        ...state,
        viewPreset: preset,
        analysisOverlay: false,
        explainResult: false,
        compareMode: true,
        guidanceMode: true,
        showRecentSeals: true,
        showQualitySplit: false,
        showTutorialFlowPanel: false,
        showPersonalizationPanel: false,
        showExemplarPanel: false,
        showProfilePanel: false,
        showLogViewer: false
      };
    case "explain":
      return {
        ...state,
        viewPreset: preset,
        analysisOverlay: false,
        explainResult: true,
        compareMode: true,
        guidanceMode: true,
        showRecentSeals: true,
        showQualitySplit: true,
        showTutorialFlowPanel: true,
        showPersonalizationPanel: true,
        showExemplarPanel: true,
        showProfilePanel: true,
        showLogViewer: false
      };
    case "workshop":
      return {
        ...state,
        viewPreset: preset,
        analysisOverlay: true,
        explainResult: true,
        compareMode: true,
        guidanceMode: true,
        showRecentSeals: true,
        showQualitySplit: true,
        showTutorialFlowPanel: true,
        showPersonalizationPanel: true,
        showExemplarPanel: true,
        showProfilePanel: true,
        showLogViewer: true
      };
    default:
      return state;
  }
}
