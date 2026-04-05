export type DemoViewPreset = "clean" | "explain" | "workshop";
export type GuidedDemoScenarioId =
  | "same_shape_fast"
  | "same_shape_slow"
  | "closure_leak"
  | "rotation_bias";

export interface DemoViewState {
  viewPreset: DemoViewPreset;
  analysisOverlay: boolean;
  qualityInfluence: boolean;
  explainResult: boolean;
  compareMode: boolean;
  guidanceMode: boolean;
  showRecentSeals: boolean;
  showQualitySplit: boolean;
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
    label: "same shape / fast",
    title: "같은 모양, 빠른 tempo",
    prompt: "같은 family silhouette를 빠르게 그려서 quality on/off가 결과층만 바꾸고 family는 유지되는지 보여줍니다."
  },
  {
    id: "same_shape_slow",
    label: "same shape / slow",
    title: "같은 모양, 느린 tempo",
    prompt: "같은 모양을 천천히 반복해서 output과 risk fingerprint만 달라지고 spell type은 그대로인지 설명합니다."
  },
  {
    id: "closure_leak",
    label: "closure leak",
    title: "closure leak 시나리오",
    prompt: "도형 끝을 일부러 열어 incomplete 상태와 why panel의 status 설명을 보여줍니다."
  },
  {
    id: "rotation_bias",
    label: "rotation bias",
    title: "rotation bias 시나리오",
    prompt: "같은 family를 기울여 그리고 analysis overlay로 axis, ghost guide, risk 변화를 함께 설명합니다."
  }
];

export function createDemoViewState(preset: DemoViewPreset = "clean"): DemoViewState {
  return applyDemoViewPreset(
    {
      viewPreset: preset,
      analysisOverlay: false,
      qualityInfluence: true,
      explainResult: false,
      compareMode: true,
      guidanceMode: true,
      showRecentSeals: true,
      showQualitySplit: false,
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
        showProfilePanel: true,
        showLogViewer: true
      };
    default:
      return state;
  }
}
