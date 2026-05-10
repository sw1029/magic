import { compileSealResult } from "./recognizer/compile";
import { getBuiltInMagicCardSetSignature, listBuiltInMagicCards, type MagicCard } from "./recognizer/datacards";
import {
  OVERLAY_OPERATOR_TEMPLATES,
  createOverlayReferenceFrame,
  createTutorialOperatorContext,
  recognizeOverlayStroke
} from "./recognizer/overlay";
import {
  appendTutorialCapture,
  backfillTutorialProfileCardSignature,
  createTutorialOverlayPersonalizationProfile,
  createEmptyTutorialProfileStore,
  hydrateTutorialProfileStore,
  mergeTutorializedUserProfile,
  previewTutorialProfileCardBackfill
} from "./recognizer/tutorial-profile";
import {
  QUALITY_VECTOR_KEYS,
  createEmptyUserInputProfile,
  updateUserInputProfile
} from "./recognizer/user-profile";
import {
  GUIDED_DEMO_SCENARIOS,
  applyDemoViewPreset,
  createDemoViewState,
} from "./demo-layer";
import { TUTORIAL_DEMO_STEPS, buildPreviewTutorialSteps, resolveNextTutorialStepIndex, type TutorialDemoStep } from "./demo/tutorial-flow";
import {
  buildDemoOutcomeCompare,
  createRecentSealSnapshot,
  getChangedOutcomeMetrics
} from "./demo/outcome-summary";
import { buildExplainNotes } from "./demo/explain";
import {
  buildMagicWhatIfScenarios,
  resolveMagicWhatIfScenario,
  summarizeWhatIfImpact,
  type MagicWhatIfScenario
} from "./demo/what-if";
import {
  buildMagicWhatIfPreviewModel,
  type MagicWhatIfPreviewMark,
  type MagicWhatIfPreviewModel
} from "./demo/what-if-preview";
import { getExemplarSpec, renderExemplarChip, resolveRelevantExemplarIds } from "./demo/exemplars";
import {
  buildDashboardSampleFromSession,
  buildDashboardSingleResult,
  describeDashboardSample,
  runDashboardBatch,
  type DashboardBatchSample,
  type DashboardBatchSummary
} from "./demo/dashboard-batch";
import { buildDashboardPlotModel, type DashboardPlotModel } from "./demo/dashboard-plots";
import {
  DASHBOARD_SCENARIO_PRESETS,
  accumulateDashboardFamilyMatrixSummaries,
  buildSyntheticRecipeFromRange,
  findDashboardScenarioPreset,
  runDashboardComparison,
  runDashboardFamilyMatrix,
  type DashboardComparisonSummary,
  type DashboardFamilyMatrixSummary,
  type SyntheticInputRange
} from "./demo/dashboard-presets";
import {
  coerceStrokeSessionFixture,
  parseDashboardFixture,
  type DashboardFixtureParseResult
} from "./demo/dashboard-fixtures";
import {
  dashboardFamilyName,
  dashboardStatusLabel,
  dashboardStatusTone
} from "./demo/dashboard-copy";
import type { SyntheticInputRecipe } from "./demo/synthetic-input";
import { recognizeSession } from "./recognizer/recognize";
import { recognizeSealedBaseSession } from "./recognizer/seal";
import { getTinyMlRuntimeStatus } from "./recognizer/rerank";
import { evaluateTutorialThresholdBiasPolicy } from "./recognizer/personalization-policy";
import {
  loadMagicDatacardPreview,
  type MagicDatacardLoadResult,
  type MagicDatacardPreviewRegistry
} from "./recognizer/datacard-loader";
import {
  appendDatacardShapeCapture,
  createEmptyDatacardShapeCaptureStore,
  getDatacardShapePresetById,
  listDatacardShapePresets,
  recognizeSessionWithDatacard,
  validateDatacardShapePreset,
  type DatacardRecognitionResult,
  type DatacardShapeCaptureStore,
  type DatacardShapeId,
  type DatacardShapePreset,
  type DatacardShapePresetGroup
} from "./recognizer/datacard-shape-lab";
import type {
  AxisLine,
  CompiledSealResult,
  GlyphFamily,
  OverlayAnchorZoneId,
  OverlayOperator,
  OverlayRecognition,
  OverlayReferenceFrame,
  OverlayStrokeRecord,
  PersonalizationRuntimeSummary,
  QualityVector,
  RecognitionLogEntry,
  RecognitionResult,
  RecognitionStatus,
  RitualPhase,
  SealDetection,
  Stroke,
  StrokeSession,
  TutorialCapture,
  TutorialBaseSnapshot,
  TutorialCaptureSource,
  TutorialCaptureValidation,
  TutorialOperatorContext,
  TutorialProfileStore,
  UserInputProfile,
  UserInputProfileDelta,
  UserShapeProfile
} from "./recognizer/types";
import type {
  DemoPage,
  DemoViewState,
  DemoViewPreset,
  GuidedDemoScenarioId
} from "./demo-layer";
import type {
  DemoOutcomeCompare,
  DemoOutcomeInput,
  DemoOutcomeMetric,
  DemoOutcomeSummary,
  RecentSealSnapshot
} from "./demo/outcome-summary";

const CANVAS_WIDTH = 900;
const CANVAS_HEIGHT = 900;
const PROFILE_STORAGE_KEY = "magic-recognizer-v1_5-profile";
const TUTORIAL_PROFILE_STORAGE_KEY = "magic-recognizer-v1_5-tutorial-profile";
const RECENT_SEAL_LIMIT = 6;
const WEB_UI_PAGES: Array<{
  id: DemoPage;
  label: string;
  title: string;
  copy: string;
}> = [
  {
    id: "test",
    label: "실제 테스트",
    title: "실제 테스트",
    copy: "캔버스 입력과 실제 판정만 집중해서 확인합니다."
  },
  {
    id: "tutorial",
    label: "따라 그리기",
    title: "따라 그리기 연습",
    copy: "캔버스와 단계 안내를 함께 보며 연습 입력, 내 손모양 반영, 전후 비교를 진행합니다."
  },
  {
    id: "ml",
    label: "보조 판독",
    title: "보조 판독",
    copy: "ML이 현재 입력을 어떻게 다시 보고, 실제 인식 기준 조정에 쓰였는지 확인합니다."
  },
  {
    id: "quality",
    label: "품질 벡터",
    title: "품질 벡터",
    copy: "원본 품질값과 필체 보정 후 값을 비교합니다."
  },
  {
    id: "guide",
    label: "가이드",
    title: "가이드라인",
    copy: "모범 선례와 HCI 설명 기준을 확인합니다."
  },
  {
    id: "logs",
    label: "로그",
    title: "기록",
    copy: "최근 결과, 연습 요약, 판정 JSON을 검증합니다."
  },
  {
    id: "dashboard",
    label: "검사판",
    title: "마법진 상세 검사판",
    copy: "떨림, 열린 틈, 곡선, 대량 입력, 보조 판독을 한 화면에서 확인합니다."
  }
];
const SCENARIO_APPEAL: Record<
  GuidedDemoScenarioId,
  { label: string; title: string; prompt: string; narration: string }
> = {
  same_shape_fast: {
    label: "빠른 불꽃",
    title: "빠른 불꽃",
    prompt: "같은 불꽃 삼각형을 빠르게 그려 품질 반영 전후에 결과감만 달라지고 종류는 그대로 유지되는지 보여줍니다.",
    narration: "불꽃 삼각형을 빠르게 그리고 결과 비교를 열어 출력감만 달라지고 종류는 그대로라는 점을 설명하세요."
  },
  same_shape_slow: {
    label: "느린 불꽃",
    title: "느린 불꽃",
    prompt: "같은 불꽃 삼각형을 천천히 반복해 출력감은 낮아지고 제어감은 달라져도 종류는 그대로인 흐름을 보여줍니다.",
    narration: "같은 불꽃 모양을 더 느리게 그려 빠른 불꽃과 결과감만 달라진다는 점을 비교하세요."
  },
  closure_leak: {
    label: "끝이 열린 모양",
    title: "끝이 열린 모양",
    prompt: "끝을 일부러 닫지 않아 왜 미완성으로 남는지 이유 설명 패널에서 바로 보여줍니다.",
    narration: "도형 끝을 열어 둔 채 기본 모양 고정을 눌러 닫힘이 부족하면 미완성으로 남는 흐름을 보여주세요."
  },
  rotation_bias: {
    label: "기울어진 모양",
    title: "기울어진 모양",
    prompt: "같은 모양을 기울여 그린 뒤 분석 안내선과 이유 설명으로 위험도 변화만 함께 설명합니다.",
    narration: "같은 모양을 기울여 그린 뒤 분석 안내선을 켜고 기울기가 결과 위험도에만 개입하는 모습을 설명하세요."
  }
};

export function resolveWebUiPageFromHash(hash: string): DemoPage {
  const normalized = hash.replace(/^#\/?/, "").split(/[/?]/)[0];
  return WEB_UI_PAGES.some((page) => page.id === normalized) ? (normalized as DemoPage) : "test";
}

interface CanvasRenderState {
  phase: RitualPhase;
  overlayAuthoringStarted: boolean;
  baseSession: StrokeSession;
  overlaySession: StrokeSession;
  previewResult: RecognitionResult;
  baseSealResult: RecognitionResult | null;
  currentOverlayPreview: OverlayRecognition | null;
  overlayRecords: OverlayStrokeRecord[];
  compiledResult: CompiledSealResult | null;
  analysisOverlay: boolean;
  whatIfPreview?: MagicWhatIfPreviewModel | null;
}

type TutorialCaptureRequest =
  | {
      kind: "family";
      expectedFamily: GlyphFamily;
      source: TutorialCaptureSource;
      strokes?: Stroke[];
      id?: string;
      timestamp?: number;
      validation?: TutorialCaptureValidation;
    }
  | {
      kind: "operator";
      expectedOperator: OverlayOperator;
      source: TutorialCaptureSource;
      strokes?: Stroke[];
      id?: string;
      timestamp?: number;
      validation?: TutorialCaptureValidation;
    };

interface TutorialOnboardingHook {
  getStore(): TutorialProfileStore;
  getProfile(): UserShapeProfile;
  listCaptures(): TutorialCapture[];
  recordCapture(request: TutorialCaptureRequest): TutorialCapture | null;
  captureBaseFamily(expectedFamily: GlyphFamily, source: TutorialCaptureSource): TutorialCapture | null;
  captureOverlayOperator(expectedOperator: OverlayOperator, source: TutorialCaptureSource): TutorialCapture | null;
  clear(): void;
}

interface TutorialHookHost extends HTMLDivElement {
  __magicTutorialOnboardingHook__?: TutorialOnboardingHook;
}

interface TutorialHookWindow extends Window {
  __magicTutorialOnboardingHook__?: TutorialOnboardingHook;
}

interface TutorialComparisonSnapshot {
  capturedAt: number;
  baseSession: StrokeSession;
  overlayStrokes: Stroke[];
  baseSealed: boolean;
  baseResult: RecognitionResult;
  overlayRecognition: OverlayRecognition | null;
  overlayRecords: OverlayStrokeRecord[];
  tutorialSampleCount: number;
}

export function mountApp(root: HTMLDivElement): void {
  const scenarioButtons = GUIDED_DEMO_SCENARIOS.map(
    (scenario) => `
      <button class="chip-button scenario-chip" data-scenario-id="${scenario.id}">${scenarioAppeal(scenario.id).label}</button>
    `
  ).join("");

  root.innerHTML = `
    <div class="shell">
      <header class="hero">
        <div>
          <p class="eyebrow">Magic Recognizer V1.5</p>
          <h1>마법진 결과 비교 데모</h1>
          <p class="hero-copy">
            기본 모양은 그대로 인식하고, 같은 캔버스에서 추가 효과를 얹어
            <span class="inline-pill">최종 결과</span>까지 한 화면에서 비교하는 시연용 데모입니다.
          </p>
        </div>
        <div class="hero-side">
          <div class="promise-stack">
            <span class="promise-badge">같은 모양은 같은 종류</span>
            <span class="promise-badge subtle">품질은 결과감만 조정</span>
          </div>
          <div class="legend">
            <span>기본 모양: 바람 / 땅 / 불꽃 / 물 / 생명</span>
            <span>추가 효과 예시: 버팀 / 번개 / 얼음 막대</span>
            <span>추가 효과 예시: 혼 점 / 공백 절단 / 축선 장식</span>
          </div>
        </div>
      </header>
      <nav class="app-nav" aria-label="데모 페이지">
        ${WEB_UI_PAGES.map(
          (page) => `
            <button class="page-tab" type="button" data-page-id="${page.id}">
              <span>${page.label}</span>
            </button>
          `
        ).join("")}
      </nav>
      <section class="page-intro">
        <div>
          <p class="panel-label">현재 페이지</p>
          <h2 id="page-title">실제 테스트</h2>
          <p id="page-copy" class="strip-copy">캔버스 입력과 실제 판정만 집중해서 확인합니다.</p>
        </div>
        <div id="page-summary-badges" class="promise-inline"></div>
      </section>
      <section class="demo-rail">
        <div class="demo-rail-card demo-controls-card demo-rail-wide">
          <div class="control-stack">
            <div>
              <p class="panel-label">보기 방식</p>
              <div class="chip-row">
                <button id="preset-clean-button" class="chip-button">간단히</button>
                <button id="preset-explain-button" class="chip-button">설명 포함</button>
                <button id="preset-workshop-button" class="chip-button">검증용</button>
              </div>
            </div>
            <div class="control-divider"></div>
            <div>
              <p class="panel-label">세부 조정</p>
              <div class="toggle-cluster">
                <label class="toggle-pill">
                  <input id="quality-toggle" type="checkbox" checked />
                  <span>품질 반영</span>
                </label>
                <label class="toggle-pill">
                  <input id="compare-toggle" type="checkbox" />
                  <span>결과 비교</span>
                </label>
                <label class="toggle-pill">
                  <input id="explain-toggle" type="checkbox" />
                  <span>이유 설명</span>
                </label>
                <label class="toggle-pill">
                  <input id="analysis-toggle" type="checkbox" />
                  <span>분석 안내선</span>
                </label>
                <label class="toggle-pill">
                  <input id="details-toggle" type="checkbox" />
                  <span>세부 정보</span>
                </label>
                <label class="toggle-pill">
                  <input id="personalization-toggle" type="checkbox" />
                  <span>입력 습관 보기</span>
                </label>
                <label class="toggle-pill">
                  <input id="tutorial-toggle" type="checkbox" />
                  <span>연습 흐름 보기</span>
                </label>
                <label class="toggle-pill">
                  <input id="exemplar-toggle" type="checkbox" />
                  <span>모범 선례 보기</span>
                </label>
              </div>
            </div>
          </div>
        </div>
        <div class="demo-rail-card">
          <div class="split-head">
            <div>
              <p class="panel-label">시연 시나리오</p>
              <h3 id="scenario-title">빠른 불꽃</h3>
            </div>
              <span id="preset-chip" class="status-chip status-ready">간단히</span>
          </div>
          <div class="chip-row scenario-row">
            ${scenarioButtons}
          </div>
          <p id="scenario-copy" class="card-copy">
            같은 불꽃 삼각형을 빠르게 그려 품질 반영 전후에 결과감만 달라지고 종류는 그대로 유지되는지 보여줍니다.
          </p>
        </div>
      </section>
      <section class="narration-strip">
        <p class="panel-label">한 줄 안내</p>
        <p id="narration-copy" class="narration-copy">
          불꽃 삼각형을 빠르게 그리고 결과 비교를 열어 출력감만 달라지고 종류는 그대로라는 점을 설명하세요.
        </p>
      </section>
      <section class="support-strip">
        <div>
          <p class="panel-label">보조 판독 상태</p>
          <p id="support-strip-copy" class="strip-copy">
            같은 모양은 같은 종류로 유지한 채, 보조 판독과 입력 습관 반영은 참고 계산으로만 비교합니다.
          </p>
        </div>
        <div id="support-strip-badges" class="promise-inline"></div>
      </section>
      <main class="workspace">
        <section id="board-panel" class="board-panel">
          <div class="board-head">
            <div>
              <p class="panel-label">진행 단계</p>
              <h2 id="phase-title">기본 모양</h2>
              <p id="phase-copy" class="phase-copy">기본 모양을 그린 뒤 먼저 종류를 고정합니다.</p>
            </div>
            <div class="toolbar">
              <div class="toolbar-group">
                <button id="seal-base-button" class="primary">기본 모양 고정</button>
                <button id="start-overlay-button">추가 효과 그리기</button>
                <button id="seal-final-button">최종 결과 보기</button>
              </div>
              <div class="toolbar-group">
                <button id="undo-button">마지막 선 취소</button>
                <button id="reset-button">처음부터 다시</button>
                <button id="export-button">기술 로그 저장</button>
              </div>
            </div>
          </div>
          <div class="phase-strip">
            <span id="phase-base" class="phase-chip active">기본</span>
            <span id="phase-overlay" class="phase-chip">추가</span>
            <span id="phase-final" class="phase-chip">완료</span>
          </div>
          <div class="canvas-wrap">
            <canvas id="glyph-canvas" width="${CANVAS_WIDTH}" height="${CANVAS_HEIGHT}"></canvas>
          </div>
          <div class="board-foot">
            <div>
              <p id="canvas-hint" class="canvas-hint">
                먼저 기본 모양의 종류를 읽고, 그다음 같은 캔버스에서 추가 효과를 따로 읽습니다.
              </p>
              <p id="analysis-copy" class="analysis-copy">
                분석 안내선은 원본 선을 덮지 않고 축선, 위치 힌트, 가이드 모양만 보조로 겹칩니다.
              </p>
            </div>
            <div id="analysis-legend" class="analysis-legend"></div>
          </div>
        </section>
        <section id="dashboard-panel" class="dashboard-panel" hidden aria-hidden="true">
          <section class="card dashboard-control-card">
            <div class="split-head">
              <div>
                <p class="panel-label">입력 만들기</p>
                <h3>떨림과 열린 틈 검사</h3>
              </div>
              <span id="dashboard-single-status" class="status-chip status-waiting">대기</span>
            </div>
            <p class="card-copy">실제 판정은 바꾸지 않고, 일부러 흔들리거나 열린 마법진 입력을 만들어 안정성을 확인합니다.</p>
            <div id="dashboard-preset-list" class="dashboard-preset-list" role="list"></div>
            <div class="dashboard-control-grid range-grid">
              <label>
                <span>기본 모양</span>
                <select id="dashboard-family-select">
                  <option value="fire">불꽃 모양</option>
                  <option value="water">물 모양</option>
                  <option value="earth">땅 모양</option>
                  <option value="wind">바람 모양</option>
                  <option value="life">생명 모양</option>
                </select>
              </label>
              <label>
                <span>Seed</span>
                <input id="dashboard-seed-input" type="number" value="42" />
              </label>
              <label>
                <span>떨림</span>
                <span class="range-pair">
                  <input id="dashboard-jitter-min-input" type="range" min="0" max="64" step="1" value="0" aria-label="떨림 최소" />
                  <input id="dashboard-jitter-input" type="range" min="0" max="64" step="1" value="8" aria-label="떨림 최대" />
                </span>
                <output id="dashboard-jitter-output" class="range-output" for="dashboard-jitter-min-input dashboard-jitter-input">0 - 8 px</output>
              </label>
              <label>
                <span>열린 틈</span>
                <span class="range-pair">
                  <input id="dashboard-open-gap-min-input" type="range" min="0" max="0.82" step="0.01" value="0" aria-label="열린 틈 최소" />
                  <input id="dashboard-open-gap-input" type="range" min="0" max="0.82" step="0.01" value="0.18" aria-label="열린 틈 최대" />
                </span>
                <output id="dashboard-open-gap-output" class="range-output" for="dashboard-open-gap-min-input dashboard-open-gap-input">0 - 18%</output>
              </label>
              <label>
                <span>기울기</span>
                <span class="range-pair">
                  <input id="dashboard-rotation-min-input" type="range" min="-180" max="180" step="1" value="-12" aria-label="기울기 최소" />
                  <input id="dashboard-rotation-input" type="range" min="-180" max="180" step="1" value="12" aria-label="기울기 최대" />
                </span>
                <output id="dashboard-rotation-output" class="range-output" for="dashboard-rotation-min-input dashboard-rotation-input">-12 - 12 deg</output>
              </label>
              <label>
                <span>휘어짐</span>
                <span class="range-pair">
                  <input id="dashboard-curve-min-input" type="range" min="0" max="1" step="0.01" value="0" aria-label="휘어짐 최소" />
                  <input id="dashboard-curve-input" type="range" min="0" max="1" step="0.01" value="0.16" aria-label="휘어짐 최대" />
                </span>
                <output id="dashboard-curve-output" class="range-output" for="dashboard-curve-min-input dashboard-curve-input">0 - 16%</output>
              </label>
              <label>
                <span>노이즈 선</span>
                <span class="range-pair">
                  <input id="dashboard-noise-min-input" type="range" min="0" max="8" step="1" value="0" aria-label="노이즈 선 최소" />
                  <input id="dashboard-noise-input" type="range" min="0" max="8" step="1" value="1" aria-label="노이즈 선 최대" />
                </span>
                <output id="dashboard-noise-output" class="range-output" for="dashboard-noise-min-input dashboard-noise-input">0 - 1</output>
              </label>
              <label>
                <span>반복 횟수</span>
                <input id="dashboard-iterations-input" type="range" min="10" max="1000" step="10" value="100" aria-label="반복 횟수" />
                <output id="dashboard-iterations-output" class="range-output" for="dashboard-iterations-input">100회</output>
              </label>
            </div>
            <div class="tutorial-actions compact-actions">
              <button id="dashboard-generate-button" type="button" class="primary">랜덤 입력 만들기</button>
              <button id="dashboard-random-button" type="button">seed 바꿔 생성</button>
              <button id="dashboard-batch-button" type="button">여러 번 테스트</button>
              <button id="dashboard-family-batch-button" type="button">모양별 n회 생성</button>
              <button id="dashboard-batch-log-clear-button" type="button">누적 로그 지우기</button>
              <span id="dashboard-batch-log-count" class="log-count">0 runs</span>
            </div>
          </section>
          <section class="card dashboard-preview-card">
            <div class="split-head">
              <div>
                <p class="panel-label">한 번 보기</p>
                <h3 id="dashboard-preview-title">만든 입력 미리보기</h3>
              </div>
            </div>
            <div id="dashboard-stroke-preview" class="dashboard-stroke-preview"></div>
            <div id="dashboard-single-summary" class="summary-grid"></div>
            <div id="dashboard-single-details" class="metric-list"></div>
            <div id="dashboard-seal-smoke" class="metric-list"></div>
          </section>
          <section class="card dashboard-plot-card">
            <div class="split-head">
              <div>
                <p class="panel-label">여러 번 테스트 결과</p>
                <h3>헷갈림 지도와 분포</h3>
              </div>
              <span id="dashboard-batch-count" class="log-count">0회</span>
            </div>
            <p id="dashboard-batch-copy" class="card-copy">여러 번 테스트를 누르면 비슷한 입력을 반복해 안정성을 보여 줍니다.</p>
            <div id="dashboard-comparison-lanes" class="dashboard-comparison-lanes"></div>
            <div id="dashboard-comparison-table" class="dashboard-comparison-table"></div>
            <div id="dashboard-status-bars" class="dashboard-bars"></div>
            <div id="dashboard-quality-bars" class="dashboard-bars"></div>
            <div id="dashboard-heatmap" class="dashboard-heatmap"></div>
            <div id="dashboard-scatter" class="dashboard-scatter"></div>
            <div id="dashboard-family-distribution" class="dashboard-family-distribution"></div>
            <div id="dashboard-overlap-table" class="dashboard-overlap-table"></div>
            <div id="dashboard-batch-log" class="dashboard-batch-log"></div>
          </section>
          <section class="card dashboard-fixture-card dashboard-hidden-json" hidden aria-hidden="true">
            <div class="split-head">
              <div>
                <p class="panel-label">예시 데이터</p>
                <h3>JSON으로 재현하기</h3>
              </div>
              <span id="dashboard-fixture-status" class="status-chip status-waiting">대기</span>
            </div>
            <p class="card-copy">선 입력, 설명 카드 패치, 여러 번 테스트 설정을 붙여 넣어 문제 상황을 미리보기로 확인합니다.</p>
            <textarea id="dashboard-fixture-input" rows="7" spellcheck="false" placeholder='{ "family": "fire", "seed": 77, "jitterPx": 8, "openGapRatio": 0.25, "iterations": 50 }'></textarea>
            <div class="tutorial-actions compact-actions">
              <button id="dashboard-fixture-button" type="button">예시 미리보기</button>
            </div>
            <div id="dashboard-fixture-summary" class="metric-list"></div>
            <pre id="dashboard-report-viewer" class="log-viewer dashboard-report-viewer">{}</pre>
          </section>
        </section>
        <aside class="sidebar">
          <section id="base-card" class="card">
            <p class="panel-label">기본 모양 판정</p>
            <h3 id="base-family">대기 중</h3>
            <p id="base-status" class="status-chip status-invalid">미인식</p>
            <p id="base-reason" class="card-copy">아직 기본 모양 입력이 없습니다.</p>
            <ol id="candidate-list" class="candidate-list"></ol>
          </section>
          <section id="overlay-preview-card" class="card">
            <p class="panel-label">추가 효과 미리보기</p>
            <h3 id="overlay-preview-title">추가 효과 전</h3>
            <p id="overlay-preview-status" class="status-chip status-waiting">대기</p>
            <p id="overlay-preview-reason" class="card-copy">기본 모양을 고정한 뒤 추가 효과 그리기를 누르면 미리보기가 열립니다.</p>
            <div id="overlay-preview-meta" class="summary-grid"></div>
            <ol id="overlay-preview-candidates" class="candidate-list"></ol>
          </section>
          <section id="overlay-records-card" class="card">
            <p class="panel-label">추가 효과 기록</p>
            <h3 id="overlay-title">추가 효과 0개</h3>
            <p id="overlay-status" class="status-chip status-waiting">대기</p>
            <p id="overlay-reason" class="card-copy">추가 효과를 그릴 때마다 기록이 쌓입니다.</p>
            <ol id="overlay-list" class="candidate-list"></ol>
          </section>
          <section id="compile-card" class="card">
            <p class="panel-label">최종 결과</p>
            <h3 id="compile-title">최종 결과 전</h3>
            <p id="compile-status" class="status-chip status-waiting">대기</p>
            <p id="compile-reason" class="card-copy">기본 모양과 추가 효과를 함께 묶어 최종 결과를 보여 줍니다.</p>
            <div id="compile-summary" class="summary-grid"></div>
          </section>
          <section id="datacard-shape-lab-card" class="card datacard-shape-lab-card">
            <div class="split-head">
              <div>
                <p class="panel-label">도형 카드</p>
                <h3 id="datacard-shape-title">정의로 도형 비교</h3>
              </div>
              <span id="datacard-shape-status" class="status-chip status-waiting">대기</span>
            </div>
            <p id="datacard-shape-copy" class="card-copy">
              preset을 골라 현재 입력이 카드의 그리기 기준과 얼마나 맞는지 비교합니다.
            </p>
            <div class="insight-tabs datacard-shape-tabs">
              <button id="datacard-shape-basic-tab" class="chip-button active" type="button" data-datacard-shape-tab="basic">기본 도형</button>
              <button id="datacard-shape-custom-tab" class="chip-button" type="button" data-datacard-shape-tab="custom">새 도형 예시</button>
            </div>
            <div id="datacard-shape-preset-list" class="datacard-shape-preset-list" role="list"></div>
            <div id="datacard-shape-definition" class="datacard-definition-panel"></div>
            <div id="datacard-shape-preview" class="datacard-shape-preview"></div>
            <div id="datacard-shape-comparison" class="datacard-comparison-grid"></div>
            <div id="datacard-shape-summary" class="summary-grid"></div>
            <ol id="datacard-shape-candidates" class="candidate-list"></ol>
            <div class="tutorial-actions compact-actions">
              <button id="datacard-shape-capture-button" type="button" class="primary">연습 입력 저장</button>
            </div>
            <div id="datacard-shape-notes" class="metric-list"></div>
          </section>
          <section id="outcome-card" class="card">
            <div class="split-head">
              <div>
                <p class="panel-label">품질 비교</p>
                <h3 id="outcome-title">품질 전후 비교</h3>
              </div>
              <span id="quality-active-badge" class="status-chip status-ready">품질 반영 켬</span>
            </div>
            <p id="outcome-copy" class="card-copy">
              같은 모양은 같은 종류로 유지하고, 품질 반영 전후의 결과감만 비교합니다.
            </p>
            <div class="promise-inline">
              <span class="inline-guarantee">같은 모양은 같은 종류</span>
              <span class="inline-guarantee">품질은 결과감만 조정</span>
            </div>
            <div id="outcome-compare" class="compare-grid"></div>
          </section>
          <section id="why-card" class="card">
            <div class="split-head">
              <div>
                <p class="panel-label">이유 설명</p>
                <h3 id="why-title">왜 이렇게 읽혔나요?</h3>
              </div>
              <span id="why-status" class="status-chip status-waiting">대기</span>
            </div>
            <p id="why-copy" class="card-copy">짧은 문장으로 현재 판정 이유와 품질 영향 설명을 보여 줍니다.</p>
            <div id="why-list" class="metric-list"></div>
          </section>
          <section id="support-card" class="card">
            <div class="split-head">
              <div>
                <p class="panel-label">핵심 비교</p>
                <h3 id="support-title">현재 판정과 참고 계산</h3>
              </div>
              <span id="support-status" class="status-chip status-waiting">대기</span>
            </div>
            <p id="support-copy" class="card-copy">보조 판독과 연습 전/후 비교를 한 카드에서 설명합니다.</p>
            <div id="support-badges" class="promise-inline"></div>
            <div id="support-metrics" class="summary-grid"></div>
            <div class="insight-tabs">
              <button id="insight-assist-tab" class="chip-button active" type="button">현재 vs 보조 판독</button>
              <button id="insight-practice-tab" class="chip-button" type="button">연습 전/후</button>
            </div>
            <div id="insight-assist-panel" class="insight-panel">
              <div id="personalization-compare" class="personalization-grid"></div>
              <div id="personalization-effects" class="metric-list"></div>
            </div>
            <div id="insight-practice-panel" class="insight-panel" hidden>
              <div id="tutorial-compare-grid" class="personalization-grid tutorial-compare-grid"></div>
              <div id="tutorial-compare-effects" class="metric-list"></div>
              <div id="tutorial-compare-metrics" class="summary-grid"></div>
            </div>
          </section>
          <section id="ml-runtime-card" class="card">
            <div class="split-head">
              <div>
                <p class="panel-label">보조 판독</p>
                <h3>인식 기준과 실제 반영</h3>
              </div>
              <span id="ml-runtime-status" class="status-chip status-waiting">대기</span>
            </div>
            <p id="ml-runtime-copy" class="card-copy">
              보조 판독이 현재 입력을 어떻게 다시 보고, 인식 기준 조정이 실제로 쓰였는지 보여 줍니다.
            </p>
            <div id="ml-runtime-summary" class="summary-grid"></div>
            <div class="analysis-grid two-up">
              <section class="detail-panel">
                <p class="mini-label">기본 모양</p>
                <div id="ml-base-rows" class="metric-list"></div>
              </section>
              <section class="detail-panel">
                <p class="mini-label">추가 효과</p>
                <div id="ml-operator-rows" class="metric-list"></div>
              </section>
            </div>
          </section>
          <section id="tutorial-card" class="card">
            <div class="split-head">
              <div>
                <p class="panel-label">연습 입력 시작</p>
                <h3 id="tutorial-title">연습 전 비교 준비</h3>
              </div>
              <span id="tutorial-status" class="status-chip status-waiting">대기</span>
            </div>
            <p id="tutorial-copy" class="card-copy">
              현재 입력을 기준으로 연습 전과 연습 후를 같은 화면에서 비교합니다.
            </p>
            <div id="tutorial-progress" class="promise-inline"></div>
            <div id="tutorial-step-card" class="tutorial-step-card"></div>
            <div id="tutorial-step-list" class="tutorial-step-list"></div>
            <div class="tutorial-actions">
              <button id="tutorial-start-button">연습 시작</button>
              <button id="tutorial-capture-button" class="primary">연습에 저장</button>
              <button id="tutorial-result-button">변화 확인</button>
              <button id="tutorial-clear-button">연습 지우기</button>
            </div>
          </section>
          <section id="tutorial-profile-card" class="card">
            <div class="split-head">
              <div>
                <p class="panel-label">저장된 연습</p>
                <h3>내 손모양 반영 상태</h3>
              </div>
              <span id="tutorial-validation-status" class="status-chip status-waiting">대기</span>
            </div>
            <p id="tutorial-validation-copy" class="card-copy">
              잘 맞게 저장된 연습만 내 손모양 기준과 인식 기준 조정에 반영합니다.
            </p>
            <div id="tutorial-validation-summary" class="summary-grid"></div>
            <div id="tutorial-validation-details" class="metric-list"></div>
            <div class="tutorial-actions backfill-actions">
              <button id="tutorial-backfill-button" type="button" class="secondary">현재 카드 기준으로 보강</button>
            </div>
          </section>
          <section id="principles-card" class="card">
            <div class="split-head">
              <div>
                <p class="panel-label">보는 기준</p>
                <h3 id="reading-guide-title">설명 기준과 모범 선례</h3>
              </div>
            </div>
            <p id="reading-guide-copy" class="card-copy">
              화면에서 무엇을 기준으로 읽는지와, 안정적으로 보이는 예시를 함께 보여 줍니다.
            </p>
            <div id="principles-list" class="principles-list"></div>
            <div id="magic-what-if-guide" class="metric-list"></div>
            <section class="datacard-authoring" id="datacard-authoring-card" aria-label="datacard preview authoring">
              <div class="split-head compact-head">
                <div>
                  <p class="panel-label">Datacard preview lane</p>
                  <h3>도형/기호 저작 검증</h3>
                </div>
                <span class="status-chip status-waiting" id="datacard-preview-status">대기</span>
              </div>
              <p class="card-copy">현재 판정은 건드리지 않고, 설명 카드 preset으로 튜토리얼 문구와 what-if 후보만 미리 바꿔봅니다.</p>
              <div id="datacard-preset-list" class="datacard-preset-list"></div>
              <textarea id="datacard-preview-input" rows="1" spellcheck="false" hidden aria-hidden="true" placeholder='{ "cards": [{ "id": "family:fire", "kind": "family", "family": "fire", "label": "불꽃 결계", "shortLabel": "불꽃", "target": { "kind": "family", "label": "fire" }, "tutorial": { "title": "세 꼭짓점의 불꽃", "instruction": "세 점의 균형과 시작/끝 접힘을 관찰합니다.", "summary": "삼각형의 압축, 회전, 획순 변화를 what-if로 비교합니다.", "checklist": ["세 꼭짓점이 분리되는가", "한 변이 과도하게 짧아지지 않는가"], "emergentPrompts": ["한 꼭짓점을 안쪽으로 당기면 어떤 오인이 생길까요?"], "whatIfHints": ["압축", "회전", "획순"] }, "recognitionHints": { "strokeCount": [2, 4], "closed": true, "shapeKeywords": ["triangle", "flame"] } }] }'></textarea>
              <div class="tutorial-actions compact-actions">
                <button id="datacard-preview-button" type="button" hidden aria-hidden="true">패치 미리보기</button>
                <button id="datacard-preview-reset-button" type="button" class="secondary">내장 카드로 복귀</button>
              </div>
              <div class="summary-grid" id="datacard-preview-summary"></div>
              <div class="metric-list" id="datacard-preview-issues"></div>
            </section>
            <div id="reading-guide-exemplar" class="reading-guide-exemplar">
              <div class="split-head">
                <div>
                  <p class="panel-label">모범 선례 패턴칩</p>
                  <h3 id="exemplar-title">안정적으로 읽히는 기준</h3>
                </div>
              </div>
              <p id="exemplar-copy" class="card-copy">내부 규칙 문자열을 바로 시각화한 작은 모범 선례입니다.</p>
              <div id="exemplar-grid" class="exemplar-grid"></div>
            </div>
          </section>
          <section id="exemplar-card" class="card" hidden aria-hidden="true"></section>
          <section id="quality-card" class="card">
            <div class="split-head">
              <div>
                <p class="panel-label">세부 품질 값</p>
                <h3>기본값과 반영값</h3>
              </div>
            </div>
            <div id="quality-summary" class="summary-grid"></div>
            <div class="quality-panels">
              <section>
                <p class="mini-label">기본 측정값</p>
                <div id="raw-quality-list" class="quality-list"></div>
              </section>
              <section>
                <p class="mini-label">품질 반영값</p>
                <div id="adjusted-quality-list" class="quality-list"></div>
              </section>
            </div>
          </section>
          <section id="recent-seals-card" class="card">
            <div class="split-head">
              <div>
                <p class="panel-label">최근 결과 비교</p>
                <h3 id="recent-seals-title">바로 비교</h3>
              </div>
              <span id="recent-count" class="log-count">3 slots</span>
            </div>
            <div id="recent-seals" class="recent-seals"></div>
          </section>
          <section id="profile-card" class="card">
            <p class="panel-label">연습 입력 상태</p>
            <h3 id="profile-samples">0회 누적</h3>
            <p id="profile-delta" class="card-copy">연습 입력은 의미를 바꾸지 않고 입력 습관만 반영합니다.</p>
            <div id="profile-baseline-list" class="summary-grid"></div>
            <div id="profile-delta-list" class="metric-list"></div>
          </section>
          <section id="log-card" class="card log-card">
            <div class="log-head">
              <div>
                <p class="panel-label">기술 로그</p>
                <h3>판정 JSON</h3>
              </div>
              <span id="log-count" class="log-count">0 entries</span>
            </div>
            <pre id="log-viewer" class="log-viewer">[]</pre>
          </section>
        </aside>
      </main>
    </div>
  `;

  const canvas = select<HTMLCanvasElement>(root, "#glyph-canvas");
  const context = canvas.getContext("2d");

  if (!context) {
    throw new Error("캔버스 컨텍스트를 초기화하지 못했습니다.");
  }

  const ctx = context;
  const workspace = select<HTMLElement>(root, ".workspace");
  const sidebar = select<HTMLElement>(root, ".sidebar");
  const canvasWrap = select<HTMLElement>(root, ".canvas-wrap");
  const pageTitle = select<HTMLElement>(root, "#page-title");
  const pageCopy = select<HTMLParagraphElement>(root, "#page-copy");
  const pageSummaryBadges = select<HTMLDivElement>(root, "#page-summary-badges");
  const pageNavButtons = Array.from(root.querySelectorAll<HTMLButtonElement>("[data-page-id]"));
  const boardPanel = select<HTMLElement>(root, "#board-panel");
  const dashboardPanel = select<HTMLElement>(root, "#dashboard-panel");
  const dashboardControlCard = select<HTMLElement>(root, ".dashboard-control-card");
  const dashboardPreviewCard = select<HTMLElement>(root, ".dashboard-preview-card");
  const dashboardPlotCard = select<HTMLElement>(root, ".dashboard-plot-card");
  const dashboardPresetList = select<HTMLDivElement>(root, "#dashboard-preset-list");
  const dashboardFamilySelect = select<HTMLSelectElement>(root, "#dashboard-family-select");
  const dashboardSeedInput = select<HTMLInputElement>(root, "#dashboard-seed-input");
  const dashboardJitterMinInput = select<HTMLInputElement>(root, "#dashboard-jitter-min-input");
  const dashboardJitterInput = select<HTMLInputElement>(root, "#dashboard-jitter-input");
  const dashboardJitterOutput = select<HTMLOutputElement>(root, "#dashboard-jitter-output");
  const dashboardOpenGapMinInput = select<HTMLInputElement>(root, "#dashboard-open-gap-min-input");
  const dashboardOpenGapInput = select<HTMLInputElement>(root, "#dashboard-open-gap-input");
  const dashboardOpenGapOutput = select<HTMLOutputElement>(root, "#dashboard-open-gap-output");
  const dashboardRotationMinInput = select<HTMLInputElement>(root, "#dashboard-rotation-min-input");
  const dashboardRotationInput = select<HTMLInputElement>(root, "#dashboard-rotation-input");
  const dashboardRotationOutput = select<HTMLOutputElement>(root, "#dashboard-rotation-output");
  const dashboardCurveMinInput = select<HTMLInputElement>(root, "#dashboard-curve-min-input");
  const dashboardCurveInput = select<HTMLInputElement>(root, "#dashboard-curve-input");
  const dashboardCurveOutput = select<HTMLOutputElement>(root, "#dashboard-curve-output");
  const dashboardNoiseMinInput = select<HTMLInputElement>(root, "#dashboard-noise-min-input");
  const dashboardNoiseInput = select<HTMLInputElement>(root, "#dashboard-noise-input");
  const dashboardNoiseOutput = select<HTMLOutputElement>(root, "#dashboard-noise-output");
  const dashboardIterationsInput = select<HTMLInputElement>(root, "#dashboard-iterations-input");
  const dashboardIterationsOutput = select<HTMLOutputElement>(root, "#dashboard-iterations-output");
  const dashboardGenerateButton = select<HTMLButtonElement>(root, "#dashboard-generate-button");
  const dashboardRandomButton = select<HTMLButtonElement>(root, "#dashboard-random-button");
  const dashboardBatchButton = select<HTMLButtonElement>(root, "#dashboard-batch-button");
  const dashboardFamilyBatchButton = select<HTMLButtonElement>(root, "#dashboard-family-batch-button");
  const dashboardBatchLogClearButton = select<HTMLButtonElement>(root, "#dashboard-batch-log-clear-button");
  const dashboardBatchLogCount = select<HTMLElement>(root, "#dashboard-batch-log-count");
  const dashboardSingleStatus = select<HTMLElement>(root, "#dashboard-single-status");
  const dashboardPreviewTitle = select<HTMLElement>(root, "#dashboard-preview-title");
  const dashboardStrokePreview = select<HTMLDivElement>(root, "#dashboard-stroke-preview");
  const dashboardSingleSummary = select<HTMLDivElement>(root, "#dashboard-single-summary");
  const dashboardSingleDetails = select<HTMLDivElement>(root, "#dashboard-single-details");
  const dashboardSealSmoke = select<HTMLDivElement>(root, "#dashboard-seal-smoke");
  const dashboardBatchCount = select<HTMLElement>(root, "#dashboard-batch-count");
  const dashboardBatchCopy = select<HTMLParagraphElement>(root, "#dashboard-batch-copy");
  const dashboardComparisonLanes = select<HTMLDivElement>(root, "#dashboard-comparison-lanes");
  const dashboardComparisonTable = select<HTMLDivElement>(root, "#dashboard-comparison-table");
  const dashboardStatusBars = select<HTMLDivElement>(root, "#dashboard-status-bars");
  const dashboardQualityBars = select<HTMLDivElement>(root, "#dashboard-quality-bars");
  const dashboardHeatmap = select<HTMLDivElement>(root, "#dashboard-heatmap");
  const dashboardScatter = select<HTMLDivElement>(root, "#dashboard-scatter");
  const dashboardFamilyDistribution = select<HTMLDivElement>(root, "#dashboard-family-distribution");
  const dashboardOverlapTable = select<HTMLDivElement>(root, "#dashboard-overlap-table");
  const dashboardBatchLog = select<HTMLDivElement>(root, "#dashboard-batch-log");
  const dashboardFixtureInput = select<HTMLTextAreaElement>(root, "#dashboard-fixture-input");
  const dashboardFixtureButton = select<HTMLButtonElement>(root, "#dashboard-fixture-button");
  const dashboardFixtureStatus = select<HTMLElement>(root, "#dashboard-fixture-status");
  const dashboardFixtureSummary = select<HTMLDivElement>(root, "#dashboard-fixture-summary");
  const dashboardReportViewer = select<HTMLPreElement>(root, "#dashboard-report-viewer");
  const sealBaseButton = select<HTMLButtonElement>(root, "#seal-base-button");
  const startOverlayButton = select<HTMLButtonElement>(root, "#start-overlay-button");
  const sealFinalButton = select<HTMLButtonElement>(root, "#seal-final-button");
  const undoButton = select<HTMLButtonElement>(root, "#undo-button");
  const resetButton = select<HTMLButtonElement>(root, "#reset-button");
  const exportButton = select<HTMLButtonElement>(root, "#export-button");
  const presetCleanButton = select<HTMLButtonElement>(root, "#preset-clean-button");
  const presetExplainButton = select<HTMLButtonElement>(root, "#preset-explain-button");
  const presetWorkshopButton = select<HTMLButtonElement>(root, "#preset-workshop-button");
  const qualityToggle = select<HTMLInputElement>(root, "#quality-toggle");
  const compareToggle = select<HTMLInputElement>(root, "#compare-toggle");
  const explainToggle = select<HTMLInputElement>(root, "#explain-toggle");
  const analysisToggle = select<HTMLInputElement>(root, "#analysis-toggle");
  const detailsToggle = select<HTMLInputElement>(root, "#details-toggle");
  const personalizationToggle = select<HTMLInputElement>(root, "#personalization-toggle");
  const tutorialToggle = select<HTMLInputElement>(root, "#tutorial-toggle");
  const exemplarToggle = select<HTMLInputElement>(root, "#exemplar-toggle");
  const presetChip = select<HTMLSpanElement>(root, "#preset-chip");
  const scenarioTitle = select<HTMLElement>(root, "#scenario-title");
  const scenarioCopy = select<HTMLParagraphElement>(root, "#scenario-copy");
  const narrationCopy = select<HTMLParagraphElement>(root, "#narration-copy");
  const supportStripCopy = select<HTMLParagraphElement>(root, "#support-strip-copy");
  const supportStripBadges = select<HTMLDivElement>(root, "#support-strip-badges");
  const scenarioChipButtons = Array.from(root.querySelectorAll<HTMLButtonElement>("[data-scenario-id]"));
  const canvasHint = select<HTMLParagraphElement>(root, "#canvas-hint");
  const analysisCopy = select<HTMLParagraphElement>(root, "#analysis-copy");
  const analysisLegend = select<HTMLDivElement>(root, "#analysis-legend");
  const phaseTitle = select<HTMLElement>(root, "#phase-title");
  const phaseCopy = select<HTMLParagraphElement>(root, "#phase-copy");
  const phaseBase = select<HTMLElement>(root, "#phase-base");
  const phaseOverlay = select<HTMLElement>(root, "#phase-overlay");
  const phaseFinal = select<HTMLElement>(root, "#phase-final");
  const baseCard = select<HTMLElement>(root, "#base-card");
  const baseFamily = select<HTMLElement>(root, "#base-family");
  const baseStatus = select<HTMLElement>(root, "#base-status");
  const baseReason = select<HTMLElement>(root, "#base-reason");
  const candidateList = select<HTMLOListElement>(root, "#candidate-list");
  const overlayPreviewCard = select<HTMLElement>(root, "#overlay-preview-card");
  const overlayPreviewTitle = select<HTMLElement>(root, "#overlay-preview-title");
  const overlayPreviewStatus = select<HTMLElement>(root, "#overlay-preview-status");
  const overlayPreviewReason = select<HTMLElement>(root, "#overlay-preview-reason");
  const overlayPreviewMeta = select<HTMLDivElement>(root, "#overlay-preview-meta");
  const overlayPreviewCandidates = select<HTMLOListElement>(root, "#overlay-preview-candidates");
  const overlayRecordsCard = select<HTMLElement>(root, "#overlay-records-card");
  const overlayTitle = select<HTMLElement>(root, "#overlay-title");
  const overlayStatus = select<HTMLElement>(root, "#overlay-status");
  const overlayReason = select<HTMLElement>(root, "#overlay-reason");
  const overlayList = select<HTMLOListElement>(root, "#overlay-list");
  const compileCard = select<HTMLElement>(root, "#compile-card");
  const compileTitle = select<HTMLElement>(root, "#compile-title");
  const compileStatus = select<HTMLElement>(root, "#compile-status");
  const compileReason = select<HTMLElement>(root, "#compile-reason");
  const compileSummary = select<HTMLDivElement>(root, "#compile-summary");
  const datacardShapeLabCard = select<HTMLElement>(root, "#datacard-shape-lab-card");
  const datacardShapeTitle = select<HTMLElement>(root, "#datacard-shape-title");
  const datacardShapeStatus = select<HTMLElement>(root, "#datacard-shape-status");
  const datacardShapeCopy = select<HTMLParagraphElement>(root, "#datacard-shape-copy");
  const datacardShapeBasicTab = select<HTMLButtonElement>(root, "#datacard-shape-basic-tab");
  const datacardShapeCustomTab = select<HTMLButtonElement>(root, "#datacard-shape-custom-tab");
  const datacardShapePresetList = select<HTMLDivElement>(root, "#datacard-shape-preset-list");
  const datacardShapeDefinition = select<HTMLDivElement>(root, "#datacard-shape-definition");
  const datacardShapePreview = select<HTMLDivElement>(root, "#datacard-shape-preview");
  const datacardShapeComparison = select<HTMLDivElement>(root, "#datacard-shape-comparison");
  const datacardShapeSummary = select<HTMLDivElement>(root, "#datacard-shape-summary");
  const datacardShapeCandidates = select<HTMLOListElement>(root, "#datacard-shape-candidates");
  const datacardShapeCaptureButton = select<HTMLButtonElement>(root, "#datacard-shape-capture-button");
  const datacardShapeNotes = select<HTMLDivElement>(root, "#datacard-shape-notes");
  const outcomeCard = select<HTMLElement>(root, "#outcome-card");
  const outcomeTitle = select<HTMLElement>(root, "#outcome-title");
  const qualityActiveBadge = select<HTMLSpanElement>(root, "#quality-active-badge");
  const outcomeCopy = select<HTMLParagraphElement>(root, "#outcome-copy");
  const outcomeCompare = select<HTMLDivElement>(root, "#outcome-compare");
  const whyCard = select<HTMLElement>(root, "#why-card");
  const whyTitle = select<HTMLElement>(root, "#why-title");
  const whyStatus = select<HTMLElement>(root, "#why-status");
  const whyCopy = select<HTMLParagraphElement>(root, "#why-copy");
  const whyList = select<HTMLDivElement>(root, "#why-list");
  const supportCard = select<HTMLElement>(root, "#support-card");
  const supportTitle = select<HTMLElement>(root, "#support-title");
  const supportStatus = select<HTMLElement>(root, "#support-status");
  const supportCopy = select<HTMLParagraphElement>(root, "#support-copy");
  const supportBadges = select<HTMLDivElement>(root, "#support-badges");
  const supportMetrics = select<HTMLDivElement>(root, "#support-metrics");
  const insightAssistTab = select<HTMLButtonElement>(root, "#insight-assist-tab");
  const insightPracticeTab = select<HTMLButtonElement>(root, "#insight-practice-tab");
  const insightAssistPanel = select<HTMLDivElement>(root, "#insight-assist-panel");
  const insightPracticePanel = select<HTMLDivElement>(root, "#insight-practice-panel");
  const mlRuntimeCard = select<HTMLElement>(root, "#ml-runtime-card");
  const mlRuntimeStatus = select<HTMLElement>(root, "#ml-runtime-status");
  const mlRuntimeCopy = select<HTMLParagraphElement>(root, "#ml-runtime-copy");
  const mlRuntimeSummary = select<HTMLDivElement>(root, "#ml-runtime-summary");
  const mlBaseRows = select<HTMLDivElement>(root, "#ml-base-rows");
  const mlOperatorRows = select<HTMLDivElement>(root, "#ml-operator-rows");
  const tutorialCard = select<HTMLElement>(root, "#tutorial-card");
  const tutorialTitle = select<HTMLElement>(root, "#tutorial-title");
  const tutorialStatus = select<HTMLElement>(root, "#tutorial-status");
  const tutorialCopy = select<HTMLParagraphElement>(root, "#tutorial-copy");
  const tutorialProgress = select<HTMLDivElement>(root, "#tutorial-progress");
  const tutorialStepCard = select<HTMLDivElement>(root, "#tutorial-step-card");
  const tutorialStepList = select<HTMLDivElement>(root, "#tutorial-step-list");
  const tutorialStartButton = select<HTMLButtonElement>(root, "#tutorial-start-button");
  const tutorialCaptureButton = select<HTMLButtonElement>(root, "#tutorial-capture-button");
  const tutorialResultButton = select<HTMLButtonElement>(root, "#tutorial-result-button");
  const tutorialClearButton = select<HTMLButtonElement>(root, "#tutorial-clear-button");
  const tutorialProfileCard = select<HTMLElement>(root, "#tutorial-profile-card");
  const tutorialValidationStatus = select<HTMLElement>(root, "#tutorial-validation-status");
  const tutorialValidationCopy = select<HTMLParagraphElement>(root, "#tutorial-validation-copy");
  const tutorialValidationSummary = select<HTMLDivElement>(root, "#tutorial-validation-summary");
  const tutorialValidationDetails = select<HTMLDivElement>(root, "#tutorial-validation-details");
  const tutorialBackfillButton = select<HTMLButtonElement>(root, "#tutorial-backfill-button");
  const personalizationCompare = select<HTMLDivElement>(root, "#personalization-compare");
  const personalizationEffects = select<HTMLDivElement>(root, "#personalization-effects");
  const tutorialCompareGrid = select<HTMLDivElement>(root, "#tutorial-compare-grid");
  const tutorialCompareEffects = select<HTMLDivElement>(root, "#tutorial-compare-effects");
  const tutorialCompareMetrics = select<HTMLDivElement>(root, "#tutorial-compare-metrics");
  const principlesCard = select<HTMLElement>(root, "#principles-card");
  const readingGuideTitle = select<HTMLElement>(root, "#reading-guide-title");
  const readingGuideCopy = select<HTMLParagraphElement>(root, "#reading-guide-copy");
  const readingGuideExemplar = select<HTMLDivElement>(root, "#reading-guide-exemplar");
  const principlesList = select<HTMLDivElement>(root, "#principles-list");
  const magicWhatIfGuide = select<HTMLDivElement>(root, "#magic-what-if-guide");
  const datacardPreviewStatus = select<HTMLElement>(root, "#datacard-preview-status");
  const datacardPresetList = select<HTMLDivElement>(root, "#datacard-preset-list");
  const datacardPreviewInput = select<HTMLTextAreaElement>(root, "#datacard-preview-input");
  const datacardPreviewButton = select<HTMLButtonElement>(root, "#datacard-preview-button");
  const datacardPreviewResetButton = select<HTMLButtonElement>(root, "#datacard-preview-reset-button");
  const datacardPreviewSummary = select<HTMLDivElement>(root, "#datacard-preview-summary");
  const datacardPreviewIssues = select<HTMLDivElement>(root, "#datacard-preview-issues");
  const exemplarCard = select<HTMLElement>(root, "#exemplar-card");
  const exemplarTitle = select<HTMLElement>(root, "#exemplar-title");
  const exemplarCopy = select<HTMLParagraphElement>(root, "#exemplar-copy");
  const exemplarGrid = select<HTMLDivElement>(root, "#exemplar-grid");
  const qualityCard = select<HTMLElement>(root, "#quality-card");
  const qualitySummary = select<HTMLDivElement>(root, "#quality-summary");
  const rawQualityList = select<HTMLDivElement>(root, "#raw-quality-list");
  const adjustedQualityList = select<HTMLDivElement>(root, "#adjusted-quality-list");
  const recentSealsCard = select<HTMLElement>(root, "#recent-seals-card");
  const recentSealsTitle = select<HTMLElement>(root, "#recent-seals-title");
  const recentCount = select<HTMLSpanElement>(root, "#recent-count");
  const recentSeals = select<HTMLDivElement>(root, "#recent-seals");
  const profileCard = select<HTMLElement>(root, "#profile-card");
  const profileSamples = select<HTMLElement>(root, "#profile-samples");
  const profileDelta = select<HTMLElement>(root, "#profile-delta");
  const profileBaselineList = select<HTMLDivElement>(root, "#profile-baseline-list");
  const profileDeltaList = select<HTMLDivElement>(root, "#profile-delta-list");
  const logCard = select<HTMLElement>(root, "#log-card");
  const logViewer = select<HTMLPreElement>(root, "#log-viewer");
  const logCount = select<HTMLSpanElement>(root, "#log-count");

  let phase: RitualPhase = "base";
  let overlayAuthoringStarted = false;
  let demoView: DemoViewState = {
    ...resolvePresetView(createDemoViewState("clean"), "clean"),
    activePage: resolveWebUiPageFromHash(window.location.hash)
  };
  let userProfile = loadUserInputProfile();
  let tutorialProfileStore = loadTutorialProfileStore();
  let datacardPreviewRegistry: MagicDatacardPreviewRegistry | null = null;
  let datacardAuthoringResult: MagicDatacardLoadResult | null = null;
  let datacardShapeTab: DatacardShapePresetGroup = "basic";
  let activeDatacardShapePresetId: DatacardShapeId = "fire";
  let datacardShapeCaptureStore: DatacardShapeCaptureStore = createEmptyDatacardShapeCaptureStore();
  let activeWhatIfScenarioId: string | null = null;
  let showWhatIfOverlay = false;
  let latestProfileDelta: UserInputProfileDelta | undefined;
  let baseSession = createEmptySession();
  let overlaySession = createEmptySession();
  let currentStroke: Stroke | null = null;
  let activeDashboardPresetId = DASHBOARD_SCENARIO_PRESETS[0].id;
  let dashboardRange: SyntheticInputRange = structuredClone(DASHBOARD_SCENARIO_PRESETS[0].range);
  let dashboardRecipe: SyntheticInputRecipe = buildSyntheticRecipeFromRange(dashboardRange, dashboardRange.seed);
  let dashboardIterations = 100;
  let dashboardSingleSample: DashboardBatchSample | null = null;
  let dashboardBatchSummary: DashboardBatchSummary | null = null;
  let dashboardComparisonSummary: DashboardComparisonSummary | null = null;
  let dashboardFamilyMatrixSummary: DashboardFamilyMatrixSummary | null = null;
  let dashboardFamilyMatrixLog: DashboardFamilyMatrixSummary[] = [];
  let dashboardFixtureResult: DashboardFixtureParseResult | null = null;
  const currentRecognitionProfile = (): UserInputProfile =>
    mergeTutorializedUserProfile(userProfile, tutorialProfileStore);
  const currentOverlayPersonalizationProfile = () =>
    createTutorialOverlayPersonalizationProfile(tutorialProfileStore);
  let previewResult = recognizeSession(baseSession, { sealed: false, profile: currentRecognitionProfile() });
  let baseSealResult: RecognitionResult | null = null;
  let baseSealDetection: SealDetection | null = null;
  let currentOverlayPreview: OverlayRecognition | null = null;
  let overlayRecords: OverlayStrokeRecord[] = [];
  let compiledResult: CompiledSealResult | null = null;
  let logs: RecognitionLogEntry[] = [];
  let recentSealSnapshots: RecentSealSnapshot[] = [];
  let tutorialFlowActive = false;
  let tutorialStepIndex = 0;
  let tutorialCompletedStepIds: string[] = [];
  let tutorialBeforeSnapshot: TutorialComparisonSnapshot | null = null;
  let tutorialAfterSnapshot: TutorialComparisonSnapshot | null = null;
  let insightTab: "assist" | "practice" = "assist";
  if (demoView.activePage === "tutorial") {
    insightTab = "practice";
  }
  const setTutorialProfileStore = (nextStore: TutorialProfileStore): void => {
    tutorialProfileStore = nextStore;
    syncTutorialHookMetadata(root, nextStore);
    saveTutorialProfileStore(nextStore);
    root.dispatchEvent(new CustomEvent("magic:tutorial-profile-updated", { detail: structuredClone(nextStore) }));
    refreshRecognitionState();
  };
  const currentMagicCards = (): readonly MagicCard[] => datacardPreviewRegistry?.cards ?? listBuiltInMagicCards();
  const currentTutorialSteps = (): TutorialDemoStep[] => buildPreviewTutorialSteps(TUTORIAL_DEMO_STEPS, currentMagicCards());
  const currentWhatIfScenarios = (): MagicWhatIfScenario[] => buildMagicWhatIfScenarios(currentMagicCards());
  const activeDatacardShapePreset = (): DatacardShapePreset =>
    getDatacardShapePresetById(activeDatacardShapePresetId) ?? listDatacardShapePresets()[0];
  const buildActiveWhatIfPreviewModel = (): MagicWhatIfPreviewModel | null => {
    if (!showWhatIfOverlay || !activeWhatIfScenarioId) {
      return null;
    }

    const scenario = resolveMagicWhatIfScenario(activeWhatIfScenarioId, currentWhatIfScenarios());
    if (!scenario) {
      return null;
    }

    return buildMagicWhatIfPreviewModel({
      scenario,
      referenceFrame: baseSession.strokes.length > 0 ? createOverlayReferenceFrame(baseSession) : null,
      baseStrokes: baseSession.strokes
    });
  };
  const tutorialOnboardingHook = createTutorialOnboardingHook({
    getBaseStrokes: () => structuredClone(baseSession.strokes),
    getOverlayStrokes: () => structuredClone(overlaySession.strokes),
    getBaseSession: () => structuredClone(baseSession),
    getOverlayRecords: () => structuredClone(overlayRecords),
    canCaptureOperator: () => Boolean(baseSealResult?.canonicalFamily),
    getStore: () => tutorialProfileStore,
    setStore: setTutorialProfileStore
  });

  exposeTutorialOnboardingHook(root, tutorialOnboardingHook);
  syncTutorialHookMetadata(root, tutorialProfileStore);
  syncTinyMlRuntimeMetadata(root);

  let layoutSyncFrame: number | null = null;

  function setLayoutVariable(name: string, value: string): void {
    if (root.style.getPropertyValue(name) !== value) {
      root.style.setProperty(name, value);
    }
  }

  function removeLayoutVariable(name: string): void {
    if (root.style.getPropertyValue(name)) {
      root.style.removeProperty(name);
    }
  }

  function syncAdaptiveLayoutBounds(page: DemoPage = demoView.activePage): void {
    if (page === "test" || page === "tutorial") {
      const workspaceRect = workspace.getBoundingClientRect();
      const canvasRect = canvasWrap.getBoundingClientRect();
      const maxHeight = Math.max(240, Math.floor(canvasRect.bottom - workspaceRect.top) - 8);

      if (workspaceRect.width > 0 && canvasRect.width > 0 && maxHeight > 0) {
        setLayoutVariable("--canvas-sidebar-max-height", `${maxHeight}px`);
      }
    } else {
      removeLayoutVariable("--canvas-sidebar-max-height");
    }

    if (page === "dashboard") {
      const controlRect = dashboardControlCard.getBoundingClientRect();
      const previewRect = dashboardPreviewCard.getBoundingClientRect();
      const maxHeight = Math.ceil(Math.max(controlRect.height, previewRect.height));

      if (controlRect.width > 0 && previewRect.width > 0 && maxHeight > 0) {
        setLayoutVariable("--dashboard-result-max-height", `${maxHeight}px`);
      }
    } else {
      removeLayoutVariable("--dashboard-result-max-height");
    }
  }

  function scheduleAdaptiveLayoutBounds(): void {
    if (typeof window.requestAnimationFrame !== "function") {
      syncAdaptiveLayoutBounds();
      return;
    }

    if (layoutSyncFrame !== null && typeof window.cancelAnimationFrame === "function") {
      window.cancelAnimationFrame(layoutSyncFrame);
    }

    layoutSyncFrame = window.requestAnimationFrame(() => {
      layoutSyncFrame = null;
      syncAdaptiveLayoutBounds();
    });
  }

  if (typeof ResizeObserver !== "undefined") {
    const adaptiveLayoutObserver = new ResizeObserver(() => {
      scheduleAdaptiveLayoutBounds();
    });

    for (const element of [
      workspace,
      boardPanel,
      canvasWrap,
      sidebar,
      dashboardControlCard,
      dashboardPreviewCard,
      dashboardPlotCard
    ]) {
      adaptiveLayoutObserver.observe(element);
    }
  }

  canvas.addEventListener("pointerdown", (event) => {
    if (phase === "final") {
      clearRitual();
    }

    if (phase === "base" && baseSealResult?.canonicalFamily) {
      return;
    }

    if (phase === "overlay" && (!baseSealResult?.canonicalFamily || !overlayAuthoringStarted)) {
      return;
    }

    if (phase === "base") {
      baseSealResult = null;
      baseSealDetection = null;
      latestProfileDelta = undefined;
      overlayAuthoringStarted = false;
      overlaySession = createEmptySession();
      overlayRecords = [];
      currentOverlayPreview = null;
      compiledResult = null;
    } else {
      compiledResult = null;
    }

    const point = pointFromEvent(canvas, event);
    const timestamp = Date.now();
    const targetSession = phase === "base" ? baseSession : overlaySession;

    currentStroke = {
      id: crypto.randomUUID(),
      points: [{ ...point, t: timestamp, pressure: event.pressure || 0.5 }]
    };

    if (targetSession.strokes.length === 0) {
      targetSession.startedAt = timestamp;
    }

    targetSession.strokes.push(currentStroke);
    targetSession.endedAt = timestamp;
    canvas.setPointerCapture(event.pointerId);

    if (phase === "base") {
      previewResult = recognizeSession(baseSession, { sealed: false, profile: currentRecognitionProfile() });
    } else {
      currentOverlayPreview = recognizeOverlayStroke(
        currentStroke,
        createOverlayContext(
          baseSession,
          overlayRecords,
          overlaySession,
          currentOverlayPersonalizationProfile()
        )
      );
    }

    render();
  });

  canvas.addEventListener("pointermove", (event) => {
    if (!currentStroke) {
      return;
    }

    const point = pointFromEvent(canvas, event);
    const lastPoint = currentStroke.points[currentStroke.points.length - 1];

    if (distance(point, lastPoint) < 1.2) {
      return;
    }

    currentStroke.points.push({
      ...point,
      t: Date.now(),
      pressure: event.pressure || 0.5
    });

    if (phase === "base") {
      previewResult = recognizeSession(baseSession, { sealed: false, profile: currentRecognitionProfile() });
    } else {
      currentOverlayPreview = recognizeOverlayStroke(
        currentStroke,
        createOverlayContext(
          baseSession,
          overlayRecords,
          overlaySession,
          currentOverlayPersonalizationProfile()
        )
      );
    }

    render();
  });

  const stopStroke = (event: PointerEvent) => {
    if (!currentStroke) {
      return;
    }

    const point = pointFromEvent(canvas, event);
    const lastPoint = currentStroke.points[currentStroke.points.length - 1];

    if (distance(point, lastPoint) >= 1.2) {
      currentStroke.points.push({
        ...point,
        t: Date.now(),
        pressure: event.pressure || 0.5
      });
    }

    const finishedStroke = currentStroke;

    if (phase === "base") {
      baseSession.endedAt = Date.now();
      previewResult = recognizeSession(baseSession, { sealed: false, profile: currentRecognitionProfile() });
      attemptSealBasePhase(true);
    } else {
      overlaySession.endedAt = Date.now();
      const recognition = recognizeOverlayStroke(
        finishedStroke,
        createOverlayContext(
          baseSession,
          overlayRecords,
          overlaySession,
          currentOverlayPersonalizationProfile()
        )
      );
      currentOverlayPreview = recognition;
      overlayRecords = [...overlayRecords, { stroke: structuredClone(finishedStroke), recognition }];
    }

    currentStroke = null;
    render();
  };

  canvas.addEventListener("pointerup", stopStroke);
  canvas.addEventListener("pointercancel", stopStroke);

  sealBaseButton.addEventListener("click", () => {
    if (phase === "base") {
      sealBasePhase();
    }
  });

  startOverlayButton.addEventListener("click", () => {
    if (phase === "base" && baseSealResult?.canonicalFamily) {
      startOverlayPhase();
    }
  });

  sealFinalButton.addEventListener("click", () => {
    if (phase === "overlay") {
      finalizeCompilePhase();
    }
  });

  undoButton.addEventListener("click", () => {
    if (phase === "base") {
      if (baseSession.strokes.length === 0) {
        return;
      }

      baseSession.strokes = baseSession.strokes.slice(0, -1);
      baseSession.endedAt = Date.now();
      currentStroke = null;
      baseSealResult = null;
      baseSealDetection = null;
      latestProfileDelta = undefined;
      overlayAuthoringStarted = false;
      overlaySession = createEmptySession();
      overlayRecords = [];
      currentOverlayPreview = null;
      compiledResult = null;
      previewResult = recognizeSession(baseSession, { sealed: false, profile: currentRecognitionProfile() });
      render();
      return;
    }

    if (phase === "overlay") {
      if (overlaySession.strokes.length === 0) {
        return;
      }

      overlaySession.strokes = overlaySession.strokes.slice(0, -1);
      overlaySession.endedAt = Date.now();
      overlayRecords = overlayRecords.slice(0, -1);
      currentStroke = null;
      currentOverlayPreview = overlayRecords[overlayRecords.length - 1]?.recognition ?? null;
      compiledResult = null;
      render();
      return;
    }

    phase = "overlay";
    overlayAuthoringStarted = true;
    compiledResult = null;
    currentOverlayPreview = overlayRecords[overlayRecords.length - 1]?.recognition ?? null;
    render();
  });

  resetButton.addEventListener("click", () => {
    clearRitual();
    render();
  });

  exportButton.addEventListener("click", () => {
    const payload = JSON.stringify(logs, null, 2);
    const blob = new Blob([payload], { type: "application/json" });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = "magic-recognizer-v1_5-logs.json";
    anchor.click();
    URL.revokeObjectURL(url);
  });

  [
    dashboardJitterMinInput,
    dashboardJitterInput,
    dashboardOpenGapMinInput,
    dashboardOpenGapInput,
    dashboardRotationMinInput,
    dashboardRotationInput,
    dashboardCurveMinInput,
    dashboardCurveInput,
    dashboardNoiseMinInput,
    dashboardNoiseInput,
    dashboardIterationsInput
  ].forEach((input) => {
    input.addEventListener("input", updateDashboardControlValueOutputs);
    input.addEventListener("change", updateDashboardControlValueOutputs);
  });

  dashboardPresetList.addEventListener("click", (event) => {
    const target = event.target instanceof HTMLElement ? event.target.closest<HTMLButtonElement>("[data-dashboard-preset-id]") : null;

    if (!target) {
      return;
    }

    const preset = findDashboardScenarioPreset(target.dataset.dashboardPresetId);
    activeDashboardPresetId = preset.id;
    dashboardRange = structuredClone(preset.range);
    dashboardIterations = preset.iterations;
    dashboardRecipe = buildSyntheticRecipeFromRange(dashboardRange, dashboardRange.seed);
    dashboardSingleSample = buildDashboardSingleResult(dashboardRecipe, currentRecognitionProfile());
    dashboardBatchSummary = null;
    dashboardComparisonSummary = null;
    dashboardFixtureResult = null;
    render();
  });

  dashboardGenerateButton.addEventListener("click", () => {
    dashboardRange = readDashboardRangeFromControls();
    dashboardRecipe = buildSyntheticRecipeFromRange(dashboardRange, dashboardRange.seed);
    dashboardIterations = readDashboardIterations();
    dashboardSingleSample = buildDashboardSingleResult(dashboardRecipe, currentRecognitionProfile());
    dashboardBatchSummary = null;
    dashboardComparisonSummary = null;
    dashboardFixtureResult = null;
    render();
  });

  dashboardRandomButton.addEventListener("click", () => {
    dashboardRange = {
      ...readDashboardRangeFromControls(),
      seed: readDashboardSeed() + 1
    };
    dashboardRecipe = buildSyntheticRecipeFromRange(dashboardRange, dashboardRange.seed);
    dashboardIterations = readDashboardIterations();
    dashboardSingleSample = buildDashboardSingleResult(dashboardRecipe, currentRecognitionProfile());
    dashboardBatchSummary = null;
    dashboardComparisonSummary = null;
    dashboardFixtureResult = null;
    render();
  });

  dashboardBatchButton.addEventListener("click", () => {
    dashboardRange = readDashboardRangeFromControls();
    dashboardRecipe = buildSyntheticRecipeFromRange(dashboardRange, dashboardRange.seed);
    dashboardIterations = readDashboardIterations();
    dashboardBatchSummary = runDashboardBatch({
      recipe: dashboardRecipe,
      iterations: dashboardIterations,
      seedStart: dashboardRecipe.seed,
      profile: currentRecognitionProfile()
    });
    dashboardComparisonSummary = runDashboardComparison({
      presetId: activeDashboardPresetId,
      recipe: dashboardRecipe,
      iterations: dashboardIterations,
      seedStart: dashboardRecipe.seed,
      baselineProfile: currentRecognitionProfile()
    });
    dashboardSingleSample = dashboardBatchSummary.samples[0] ?? buildDashboardSingleResult(dashboardRecipe, currentRecognitionProfile());
    dashboardFixtureResult = null;
    render();
  });

  dashboardFamilyBatchButton.addEventListener("click", () => {
    dashboardRange = readDashboardRangeFromControls();
    dashboardRecipe = buildSyntheticRecipeFromRange(dashboardRange, dashboardRange.seed);
    dashboardIterations = readDashboardIterations();
    dashboardFamilyMatrixSummary = runDashboardFamilyMatrix({
      range: dashboardRange,
      iterations: dashboardIterations,
      seedStart: dashboardRange.seed,
      profile: currentRecognitionProfile()
    });
    dashboardFamilyMatrixLog = [dashboardFamilyMatrixSummary, ...dashboardFamilyMatrixLog].slice(0, 20);
    dashboardFixtureResult = null;
    render();
  });

  dashboardBatchLogClearButton.addEventListener("click", () => {
    dashboardFamilyMatrixSummary = null;
    dashboardFamilyMatrixLog = [];
    render();
  });

  dashboardBatchLog.addEventListener("click", (event) => {
    const target = event.target instanceof Element ? event.target.closest<HTMLButtonElement>("[data-dashboard-log-index]") : null;

    if (!target) {
      return;
    }

    const index = Number.parseInt(target.dataset.dashboardLogIndex ?? "", 10);
    const summary = Number.isFinite(index) ? dashboardFamilyMatrixLog[index] : null;

    if (!summary) {
      return;
    }

    dashboardFamilyMatrixSummary = summary;
    render();
  });

  dashboardFixtureButton.addEventListener("click", () => {
    dashboardFixtureResult = parseDashboardFixture(dashboardFixtureInput.value.trim() || dashboardFixtureInput.placeholder);

    if (dashboardFixtureResult.kind === "stroke_session") {
      const session = coerceStrokeSessionFixture(dashboardFixtureResult.value);
      if (session) {
        dashboardRecipe = readDashboardRecipeFromControls();
        dashboardSingleSample = buildDashboardSampleFromSession(dashboardRecipe.family ?? "fire", session, currentRecognitionProfile());
        dashboardBatchSummary = null;
      }
    } else if (dashboardFixtureResult.kind === "batch_recipe" && typeof dashboardFixtureResult.value === "object" && dashboardFixtureResult.value) {
      const value = dashboardFixtureResult.value as Partial<SyntheticInputRecipe> & { iterations?: number };
      dashboardRecipe = {
        ...dashboardRecipe,
        ...value,
        family: readDashboardFamily(value.family)
      };
      const nextIterations = Number(value.iterations ?? dashboardIterations);
      dashboardIterations = Number.isFinite(nextIterations)
        ? Math.max(1, Math.min(1000, Math.round(nextIterations)))
        : dashboardIterations;
      dashboardSingleSample = buildDashboardSingleResult(dashboardRecipe, currentRecognitionProfile());
      dashboardBatchSummary = runDashboardBatch({
        recipe: dashboardRecipe,
        iterations: dashboardIterations,
        seedStart: dashboardRecipe.seed,
        profile: currentRecognitionProfile()
      });
    } else if (dashboardFixtureResult.kind === "datacard_patch") {
      datacardAuthoringResult = loadMagicDatacardPreview({
        rawJson: dashboardFixtureInput.value.trim() || dashboardFixtureInput.placeholder,
        loadMode: "patch",
        sourceName: "dashboard fixture preview"
      });
      datacardPreviewRegistry = datacardAuthoringResult.registry;
    }

    render();
  });

  presetCleanButton.addEventListener("click", () => {
    applyPreset("clean");
  });

  presetExplainButton.addEventListener("click", () => {
    applyPreset("explain");
  });

  presetWorkshopButton.addEventListener("click", () => {
    applyPreset("workshop");
  });

  for (const button of pageNavButtons) {
    button.addEventListener("click", () => {
      const page = resolvePageId(button.dataset.pageId);

      if (!page) {
        return;
      }

      navigateToPage(page);
    });
  }

  window.addEventListener("hashchange", () => {
    setActivePage(resolveWebUiPageFromHash(window.location.hash), false);
  });

  window.addEventListener("resize", () => {
    scheduleAdaptiveLayoutBounds();
  });

  qualityToggle.addEventListener("change", () => {
    demoView = {
      ...demoView,
      qualityInfluence: qualityToggle.checked
    };
    render();
  });

  explainToggle.addEventListener("change", () => {
    demoView = {
      ...demoView,
      explainResult: explainToggle.checked
    };
    render();
  });

  analysisToggle.addEventListener("change", () => {
    demoView = {
      ...demoView,
      analysisOverlay: analysisToggle.checked
    };
    render();
  });

  compareToggle.addEventListener("change", () => {
    demoView = {
      ...demoView,
      compareMode: compareToggle.checked
    };
    render();
  });

  detailsToggle.addEventListener("change", () => {
    demoView = {
      ...demoView,
      showQualitySplit: detailsToggle.checked
    };
    render();
  });

  personalizationToggle.addEventListener("change", () => {
    demoView = {
      ...demoView,
      showPersonalizationPanel: personalizationToggle.checked
    };
    render();
  });

  tutorialToggle.addEventListener("change", () => {
    demoView = {
      ...demoView,
      showTutorialFlowPanel: tutorialToggle.checked
    };
    render();
  });

  exemplarToggle.addEventListener("change", () => {
    demoView = {
      ...demoView,
      showExemplarPanel: exemplarToggle.checked
    };
    render();
  });

  tutorialStartButton.addEventListener("click", () => {
    if (!tutorialBeforeSnapshot) {
      tutorialBeforeSnapshot = createTutorialComparisonSnapshot(currentRecognitionProfile());
    }

    if (!tutorialBeforeSnapshot) {
      return;
    }

    tutorialFlowActive = true;
    tutorialAfterSnapshot = null;
    insightTab = "assist";
    render();
  });

  tutorialCaptureButton.addEventListener("click", () => {
    const tutorialSteps = currentTutorialSteps();
    const step = tutorialSteps[tutorialStepIndex] ?? tutorialSteps[0];

    if (!step) {
      return;
    }

    const stepState = getTutorialStepState(step, baseSealResult, overlayRecords, baseSession, overlaySession);

    if (!stepState.ready) {
      return;
    }

    if (!tutorialBeforeSnapshot) {
      tutorialBeforeSnapshot = createTutorialComparisonSnapshot(currentRecognitionProfile());
    }

    if (!tutorialBeforeSnapshot) {
      return;
    }

    const capture = recordTutorialDemoStep(step);

    if (!capture) {
      return;
    }

    tutorialFlowActive = true;
    tutorialAfterSnapshot = null;
    if (!tutorialCompletedStepIds.includes(step.id)) {
      tutorialCompletedStepIds = [...tutorialCompletedStepIds, step.id];
    }
    tutorialStepIndex = resolveNextTutorialStepIndex(tutorialCompletedStepIds, tutorialStepIndex);
    render();
  });

  tutorialResultButton.addEventListener("click", () => {
    if (!tutorialBeforeSnapshot) {
      return;
    }

    tutorialAfterSnapshot = replayTutorialComparisonSnapshot(tutorialBeforeSnapshot);
    tutorialFlowActive = true;
    insightTab = "practice";
    render();
  });

  tutorialClearButton.addEventListener("click", () => {
    tutorialOnboardingHook.clear();
    tutorialFlowActive = false;
    tutorialStepIndex = 0;
    tutorialCompletedStepIds = [];
    tutorialBeforeSnapshot = null;
    tutorialAfterSnapshot = null;
    insightTab = "assist";
    render();
  });

  tutorialBackfillButton.addEventListener("click", () => {
    const preview = previewTutorialProfileCardBackfill(tutorialProfileStore);

    if (!preview.canBackfill) {
      render();
      return;
    }

    const confirmed = window.confirm(
      `${preview.userMessage}

기존 획 데이터는 바꾸지 않고 카드 메타데이터만 현재 내장 카드 기준으로 보강합니다.`
    );

    if (!confirmed) {
      return;
    }

    setTutorialProfileStore(backfillTutorialProfileCardSignature(tutorialProfileStore));
    render();
  });

  datacardPresetList.addEventListener("click", (event) => {
    const target = event.target instanceof HTMLElement ? event.target.closest<HTMLButtonElement>("[data-datacard-preset-id]") : null;

    if (!target?.dataset.datacardPresetId) {
      return;
    }

    applyDatacardPreset(target.dataset.datacardPresetId);
  });

  datacardPreviewButton.addEventListener("click", () => {
    const rawJson = datacardPreviewInput.value.trim() || datacardPreviewInput.placeholder;
    datacardAuthoringResult = loadMagicDatacardPreview({
      rawJson,
      loadMode: "patch",
      sourceName: "web-ui datacard authoring preview"
    });
    datacardPreviewRegistry = datacardAuthoringResult.registry;
    activeWhatIfScenarioId = null;
    showWhatIfOverlay = false;
    tutorialStepIndex = Math.min(tutorialStepIndex, Math.max(0, currentTutorialSteps().length - 1));
    render();
  });

  datacardPreviewResetButton.addEventListener("click", () => {
    datacardAuthoringResult = null;
    datacardPreviewRegistry = null;
    activeWhatIfScenarioId = null;
    showWhatIfOverlay = false;
    render();
  });

  datacardShapeBasicTab.addEventListener("click", () => {
    setDatacardShapeTab("basic");
  });

  datacardShapeCustomTab.addEventListener("click", () => {
    setDatacardShapeTab("custom");
  });

  datacardShapePresetList.addEventListener("click", (event) => {
    const target = event.target instanceof HTMLElement ? event.target.closest<HTMLButtonElement>("[data-datacard-shape-id]") : null;

    if (!target?.dataset.datacardShapeId) {
      return;
    }

    const preset = getDatacardShapePresetById(target.dataset.datacardShapeId);

    if (!preset) {
      return;
    }

    activeDatacardShapePresetId = preset.id;
    datacardShapeTab = preset.group;
    render();
  });

  datacardShapeCaptureButton.addEventListener("click", () => {
    captureDatacardShapeExample();
  });

  function applyDatacardPreset(presetId: string): void {
    const rawJson = buildDatacardPresetPatch(presetId);
    datacardPreviewInput.value = rawJson;
    datacardAuthoringResult = loadMagicDatacardPreview({
      rawJson,
      loadMode: "patch",
      sourceName: `web-ui datacard preset:${presetId}`
    });
    datacardPreviewRegistry = datacardAuthoringResult.registry;
    activeWhatIfScenarioId = null;
    showWhatIfOverlay = false;
    tutorialStepIndex = Math.min(tutorialStepIndex, Math.max(0, currentTutorialSteps().length - 1));
    render();
  }

  function setDatacardShapeTab(tab: DatacardShapePresetGroup): void {
    datacardShapeTab = tab;
    const firstPreset = listDatacardShapePresets().find((preset) => preset.group === tab);

    if (firstPreset && getDatacardShapePresetById(activeDatacardShapePresetId)?.group !== tab) {
      activeDatacardShapePresetId = firstPreset.id;
    }

    render();
  }

  function captureDatacardShapeExample(): void {
    const preset = activeDatacardShapePreset();
    const recognition = recognizeSessionWithDatacard(
      baseSession,
      preset,
      datacardShapeCaptureStore,
      currentRecognitionProfile()
    );

    if (recognition.sessionUsed.strokes.length === 0) {
      render();
      return;
    }

    if (preset.kind === "built_in" && preset.builtInFamily) {
      const top = recognition.baseResult.candidates[0];
      const second = recognition.baseResult.candidates[1];
      setTutorialProfileStore(
        appendTutorialCapture(tutorialProfileStore, {
          kind: "family",
          expectedFamily: preset.builtInFamily,
          source: "variation",
          strokes: recognition.sessionUsed.strokes,
          validation: {
            reliability: recognition.selectedCandidate.status === "recognized" ? "medium" : "unvalidated",
            expectedLabel: preset.builtInFamily,
            actualTopLabel: top?.family,
            status: recognition.baseResult.status,
            topScore: top?.score,
            margin: top ? top.score - (second?.score ?? 0) : undefined,
            quality: recognition.baseResult.rawQuality
          }
        })
      );
    }

    datacardShapeCaptureStore = appendDatacardShapeCapture(
      datacardShapeCaptureStore,
      preset.id,
      recognition.sessionUsed.strokes
    );
    render();
  }

  function updateDatacardShapeLab(result: DatacardRecognitionResult): void {
    const preset = activeDatacardShapePreset();
    const validation = validateDatacardShapePreset(preset);
    const selected = result.selectedCandidate;
    const topCandidates = result.candidates.slice(0, 5);
    const captureCount = datacardShapeCaptureStore.captures.filter((capture) => capture.presetId === preset.id).length;

    datacardShapeBasicTab.className = ["chip-button", datacardShapeTab === "basic" ? "active" : ""].filter(Boolean).join(" ");
    datacardShapeCustomTab.className = ["chip-button", datacardShapeTab === "custom" ? "active" : ""].filter(Boolean).join(" ");
    datacardShapeTitle.textContent = `${preset.shortLabel} 카드 비교`;
    datacardShapeStatus.textContent = statusLabel(selected.status);
    datacardShapeStatus.className = `status-chip status-${datacardStatusTone(selected.status)}`;
    datacardShapeCopy.textContent =
      preset.kind === "custom"
        ? "새 도형은 선택한 카드 기준과 이 화면에서 저장한 연습 입력으로 비교합니다."
        : "기본 도형은 기존 판정 결과와 카드의 그리기 기준을 함께 비교합니다.";
    datacardShapePresetList.innerHTML = renderDatacardShapePresetButtons(datacardShapeTab, preset.id);
    datacardShapeDefinition.innerHTML = renderDatacardShapeDefinition(preset, validation);
    datacardShapePreview.innerHTML = renderDashboardStrokeSvg({
      startedAt: 0,
      endedAt: 1,
      strokes: [...preset.definition.exampleTemplate]
    });
    datacardShapeComparison.innerHTML = renderDatacardShapeComparison(result);
    datacardShapeSummary.innerHTML = renderSummaryRows([
      ["선택 카드", preset.label],
      ["현재 비교", `${datacardPercent(selected.score)} / ${statusLabel(selected.status)}`],
      ["연습 입력", `${captureCount}회`],
      ["보정 변화", selected.localModelLift > 0 ? `+${datacardPercent(selected.localModelLift)}` : "아직 없음"]
    ]);
    datacardShapeCandidates.innerHTML = topCandidates
      .map(
        (candidate) => `
          <li>
            <span>${escapeDashboardHtml(candidate.label)}</span>
            <strong>${datacardPercent(candidate.score)}</strong>
          </li>
        `
      )
      .join("");
    datacardShapeCaptureButton.disabled = result.sessionUsed.strokes.length === 0;
    datacardShapeCaptureButton.textContent =
      preset.kind === "built_in" ? "연습에 저장하고 비교" : "새 도형 예시 저장";
    datacardShapeNotes.innerHTML = renderMetricNotes([
      selected.reason,
      result.sealDetection?.ok
        ? "기본 모양을 감싸는 원은 완료 표시로만 보고, 도형 비교에서는 안쪽 입력만 사용했습니다."
        : "원을 둘러 완료 표시를 만들면 안쪽 도형만 따로 비교합니다.",
      preset.kind === "custom"
        ? "새 도형은 이 화면에서 저장한 연습 입력으로만 보정되며, 기존 최종 결과 조합은 바꾸지 않습니다."
        : "기본 도형은 기존 연습 저장 흐름에도 함께 반영됩니다."
    ]);
  }

  magicWhatIfGuide.addEventListener("click", (event) => {
    const target = event.target;

    if (!(target instanceof HTMLElement)) {
      return;
    }

    const button = target.closest<HTMLButtonElement>("[data-what-if-scenario-id]");

    if (!button) {
      return;
    }

    const scenarioId = button.dataset.whatIfScenarioId;

    if (!scenarioId) {
      return;
    }

    if (activeWhatIfScenarioId === scenarioId) {
      showWhatIfOverlay = !showWhatIfOverlay;
    } else {
      activeWhatIfScenarioId = scenarioId;
      showWhatIfOverlay = true;
    }

    render();
  });

  insightAssistTab.addEventListener("click", () => {
    insightTab = "assist";
    render();
  });

  insightPracticeTab.addEventListener("click", () => {
    insightTab = "practice";
    render();
  });

  tutorialStepList.addEventListener("click", (event) => {
    const target = event.target;

    if (!(target instanceof HTMLElement)) {
      return;
    }

    const button = target.closest<HTMLButtonElement>("[data-tutorial-step-index]");

    if (!button) {
      return;
    }

    const nextIndex = Number(button.dataset.tutorialStepIndex);

    if (!Number.isFinite(nextIndex) || nextIndex < 0 || nextIndex >= currentTutorialSteps().length) {
      return;
    }

    tutorialStepIndex = nextIndex;
    render();
  });

  for (const button of scenarioChipButtons) {
    button.addEventListener("click", () => {
      const scenarioId = button.dataset.scenarioId as GuidedDemoScenarioId | undefined;

      if (!scenarioId) {
        return;
      }

      demoView = {
        ...demoView,
        selectedScenarioId: scenarioId
      };
      render();
    });
  }

  function applyPreset(preset: DemoViewPreset): void {
    demoView = resolvePresetView(applyDemoViewPreset(demoView, preset), preset);
    render();
  }

  function navigateToPage(page: DemoPage): void {
    const targetHash = `#/${page}`;

    if (window.location.hash === targetHash) {
      setActivePage(page, false);
      return;
    }

    window.location.hash = targetHash;
  }

  function setActivePage(page: DemoPage, syncHash: boolean): void {
    demoView = {
      ...demoView,
      activePage: page
    };

    if (page === "tutorial") {
      insightTab = "practice";
    } else if (page === "ml") {
      insightTab = "assist";
    }

    if (syncHash && window.location.hash !== `#/${page}`) {
      window.location.hash = `/${page}`;
      return;
    }

    render();
  }

  function resolvePageId(value: string | undefined): DemoPage | undefined {
    return WEB_UI_PAGES.find((page) => page.id === value)?.id;
  }

  render();

  function sealBasePhase(): void {
    attemptSealBasePhase(false);
  }

  function attemptSealBasePhase(autoAdvance: boolean): void {
    if (baseSession.strokes.length === 0) {
      return;
    }

    const sealedBase = recognizeSealedBaseSession(baseSession, { profile: currentRecognitionProfile() });
    const sealed = sealedBase.result;
    const snapshotId = crypto.randomUUID();
    const timestamp = Date.now();
    previewResult = sealed;
    baseSealDetection = sealedBase.sealDetection;
    baseSealResult = sealedBase.sealDetection.ok ? sealed : null;
    let profileUpdate: { profile: UserInputProfile; delta: UserInputProfileDelta } | undefined;

    if (baseSealResult?.canonicalFamily) {
      profileUpdate = updateUserInputProfile(userProfile, sealed);
      userProfile = profileUpdate.profile;
      latestProfileDelta = profileUpdate.delta;
      saveUserInputProfile(userProfile);
      phase = autoAdvance ? "overlay" : "base";
      overlayAuthoringStarted = autoAdvance;
      overlaySession = createEmptySession();
      overlayRecords = [];
      currentOverlayPreview = null;
      compiledResult = null;
    }

    if (baseSealResult?.canonicalFamily || !autoAdvance) {
      logs = [
        {
          kind: "base_seal",
          id: snapshotId,
          timestamp,
          rawStrokeCount: baseSession.strokes.length,
          rawStrokes: structuredClone(baseSession.strokes),
          normalizedStrokes: sealed.normalizedStrokes,
          result: sealed,
          sealDetection: sealedBase.sealDetection,
          profileDelta: profileUpdate?.delta
        },
        ...logs
      ];
      recentSealSnapshots = [
        createRecentSealSnapshot(
          snapshotId,
          "base",
          timestamp,
          createOutcomeInput(sealed, []),
          structuredClone(baseSession.strokes)
        ),
        ...recentSealSnapshots
      ].slice(0, RECENT_SEAL_LIMIT);
    }

    render();
  }

  function startOverlayPhase(): void {
    if (!baseSealResult?.canonicalFamily) {
      return;
    }

    phase = "overlay";
    overlayAuthoringStarted = true;
    overlaySession = createEmptySession();
    overlayRecords = [];
    currentOverlayPreview = null;
    compiledResult = null;
    render();
  }

  function finalizeCompilePhase(): void {
    if (!baseSealResult?.canonicalFamily) {
      return;
    }

    compiledResult = compileSealResult(baseSealResult, overlayRecords, latestProfileDelta);
    const compileId = crypto.randomUUID();
    const timestamp = Date.now();
    phase = "final";
    overlayAuthoringStarted = false;
    logs = [
      {
        kind: "compile_seal",
        id: compileId,
        timestamp,
        rawStrokeCount: baseSession.strokes.length + overlaySession.strokes.length,
        rawStrokes: [...structuredClone(baseSession.strokes), ...structuredClone(overlaySession.strokes)],
        normalizedStrokes: baseSealResult.normalizedStrokes,
        result: baseSealResult,
        overlayRecords: structuredClone(overlayRecords),
        compiled: compiledResult
      },
      ...logs
    ];
    recentSealSnapshots = [
      createRecentSealSnapshot(
        compileId,
        "final",
        timestamp,
        createOutcomeInput(baseSealResult, overlayRecords),
        structuredClone(baseSession.strokes)
      ),
      ...recentSealSnapshots
    ].slice(0, RECENT_SEAL_LIMIT);

    render();
  }

  function clearRitual(): void {
    phase = "base";
    overlayAuthoringStarted = false;
    baseSession = createEmptySession();
    overlaySession = createEmptySession();
    currentStroke = null;
    previewResult = recognizeSession(baseSession, { sealed: false, profile: currentRecognitionProfile() });
    baseSealResult = null;
    baseSealDetection = null;
    currentOverlayPreview = null;
    overlayRecords = [];
    compiledResult = null;
    latestProfileDelta = undefined;
  }

  function readDashboardFamily(value: unknown): GlyphFamily {
    return value === "fire" || value === "water" || value === "earth" || value === "wind" || value === "life"
      ? value
      : dashboardRecipe.family ?? "fire";
  }

  function readDashboardRecipeFromControls(): SyntheticInputRecipe {
    return {
      ...dashboardRecipe,
      family: readDashboardFamily(dashboardFamilySelect.value),
      seed: readDashboardSeed(),
      jitterPx: Number(dashboardJitterInput.value) || 0,
      openGapRatio: Number(dashboardOpenGapInput.value) || 0,
      rotationDeg: Number(dashboardRotationInput.value) || 0,
      curveWarp: Number(dashboardCurveInput.value) || 0,
      extraNoiseStrokeCount: Number(dashboardNoiseInput.value) || 0,
      pointDensity: 5
    };
  }

  function readDashboardRangeFromControls(): SyntheticInputRange {
    return {
      family: readDashboardFamily(dashboardFamilySelect.value),
      seed: readDashboardSeed(),
      jitterPx: readRangePair(dashboardJitterMinInput, dashboardJitterInput, 0, 64),
      openGapRatio: readRangePair(dashboardOpenGapMinInput, dashboardOpenGapInput, 0, 0.82),
      rotationDeg: readRangePair(dashboardRotationMinInput, dashboardRotationInput, -180, 180),
      curveWarp: readRangePair(dashboardCurveMinInput, dashboardCurveInput, 0, 1),
      extraNoiseStrokeCount: readRangePair(dashboardNoiseMinInput, dashboardNoiseInput, 0, 8),
      pointDensity: 5
    };
  }

  function readDashboardSeed(): number {
    return Math.round(Number(dashboardSeedInput.value) || dashboardRange.seed || 42);
  }

  function readRangePair(
    minimumInput: HTMLInputElement,
    maximumInput: HTMLInputElement,
    lowerBound: number,
    upperBound: number
  ): { min: number; max: number } {
    const first = clampNumber(Number(minimumInput.value), lowerBound, upperBound);
    const second = clampNumber(Number(maximumInput.value), lowerBound, upperBound);

    return {
      min: Math.min(first, second),
      max: Math.max(first, second)
    };
  }

  function updateDashboardControlValueOutputs(): void {
    const jitter = readRangePair(dashboardJitterMinInput, dashboardJitterInput, 0, 64);
    const openGap = readRangePair(dashboardOpenGapMinInput, dashboardOpenGapInput, 0, 0.82);
    const rotation = readRangePair(dashboardRotationMinInput, dashboardRotationInput, -180, 180);
    const curve = readRangePair(dashboardCurveMinInput, dashboardCurveInput, 0, 1);
    const noise = readRangePair(dashboardNoiseMinInput, dashboardNoiseInput, 0, 8);

    setOutputText(dashboardJitterOutput, `${jitter.min.toFixed(0)} - ${jitter.max.toFixed(0)} px`);
    setOutputText(dashboardOpenGapOutput, `${(openGap.min * 100).toFixed(0)} - ${(openGap.max * 100).toFixed(0)}%`);
    setOutputText(dashboardRotationOutput, `${rotation.min.toFixed(0)} - ${rotation.max.toFixed(0)} deg`);
    setOutputText(dashboardCurveOutput, `${(curve.min * 100).toFixed(0)} - ${(curve.max * 100).toFixed(0)}%`);
    setOutputText(dashboardNoiseOutput, `${noise.min.toFixed(0)} - ${noise.max.toFixed(0)}`);
    setOutputText(dashboardIterationsOutput, `${readDashboardIterations()}회`);
  }

  function setOutputText(output: HTMLOutputElement, text: string): void {
    output.value = text;
    output.textContent = text;
  }

  function clampNumber(value: number, minimum: number, maximum: number): number {
    const safeValue = Number.isFinite(value) ? value : minimum;
    return Math.max(minimum, Math.min(maximum, safeValue));
  }

  function readDashboardIterations(): number {
    return Math.max(1, Math.min(1000, Math.round(Number(dashboardIterationsInput.value) || dashboardIterations)));
  }

  function syncDashboardControls(): void {
    dashboardFamilySelect.value = dashboardRange.family ?? dashboardRecipe.family ?? "fire";
    dashboardSeedInput.value = String(dashboardRange.seed ?? dashboardRecipe.seed ?? 42);
    dashboardJitterMinInput.value = String(dashboardRange.jitterPx.min);
    dashboardJitterInput.value = String(dashboardRange.jitterPx.max);
    dashboardOpenGapMinInput.value = String(dashboardRange.openGapRatio.min);
    dashboardOpenGapInput.value = String(dashboardRange.openGapRatio.max);
    dashboardRotationMinInput.value = String(dashboardRange.rotationDeg.min);
    dashboardRotationInput.value = String(dashboardRange.rotationDeg.max);
    dashboardCurveMinInput.value = String(dashboardRange.curveWarp.min);
    dashboardCurveInput.value = String(dashboardRange.curveWarp.max);
    dashboardNoiseMinInput.value = String(dashboardRange.extraNoiseStrokeCount.min);
    dashboardNoiseInput.value = String(dashboardRange.extraNoiseStrokeCount.max);
    dashboardIterationsInput.value = String(dashboardIterations);
    updateDashboardControlValueOutputs();
  }

  function ensureDashboardSample(): DashboardBatchSample {
    if (!dashboardSingleSample) {
      dashboardSingleSample = buildDashboardSingleResult(dashboardRecipe, currentRecognitionProfile());
    }

    return dashboardSingleSample;
  }

  function updateDashboardPanel(): void {
    syncDashboardControls();
    const sample = ensureDashboardSample();
    const plotModel = dashboardBatchSummary ? buildDashboardPlotModel(dashboardBatchSummary) : null;
    const selectedMatrixSummary = dashboardFamilyMatrixSummary ?? dashboardFamilyMatrixLog[0] ?? null;
    const matrixSummary = accumulateDashboardFamilyMatrixSummaries(dashboardFamilyMatrixLog) ?? selectedMatrixSummary;
    const topFamily = sample.actualFamily === "none" ? null : sample.actualFamily;
    const comparison = dashboardComparisonSummary;

    dashboardPresetList.innerHTML = renderDashboardPresetButtons(activeDashboardPresetId);
    dashboardSingleStatus.textContent = dashboardStatusLabel(sample.status);
    dashboardSingleStatus.className = `status-chip status-${dashboardStatusTone(sample.status)}`;
    dashboardPreviewTitle.textContent = `${dashboardFamilyName(dashboardRecipe.family)} 검사 입력`;
    dashboardStrokePreview.innerHTML = renderDashboardStrokeSvg(sample.session);
    dashboardSingleSummary.innerHTML = renderSummaryRows([
      ["기대한 모양", dashboardFamilyName(sample.expectedFamily)],
      ["읽힌 모양", topFamily ? dashboardFamilyName(topFamily) : "아직 없음"],
      ["상태", dashboardStatusLabel(sample.status)],
      ["후보 점수", `${(sample.topScore * 100).toFixed(1)}점`]
    ]);
    dashboardSingleDetails.innerHTML = renderMetricNotes([
      describeDashboardSample(sample),
      `닫힘 ${(sample.closure * 100).toFixed(0)}점 · 부드러움 ${(sample.smoothness * 100).toFixed(0)}점 · 안정감 ${(sample.stability * 100).toFixed(0)}점`,
      sample.shadowChanged ? "보조 판독이 참고 차이를 발견했습니다." : "보조 판독은 실제 결과를 바꾸지 않았습니다."
    ]);

    dashboardSealSmoke.innerHTML = renderMetricNotes([
      activeDashboardPresetId === "seal_ring_pass_fail"
        ? "테두리 자동 인식 테스트: 실제 테스트 화면에서 기본 도형을 감싸는 원이 확인되면 다음 단계로 넘어가는지 검증합니다."
        : "자동 테두리 인식: 기본 도형을 바깥에서 감싸는 원만 완료 신호로 보고, 그 원은 도형 종류 판정에는 포함하지 않습니다."
    ]);

    dashboardBatchCount.textContent = matrixSummary
      ? `${matrixSummary.samples.length}회`
      : comparison
        ? `${comparison.iterations}회`
        : dashboardBatchSummary
          ? `${dashboardBatchSummary.total}회`
          : "0회";
    dashboardBatchLogCount.textContent = `${dashboardFamilyMatrixLog.length} logs`;
    dashboardBatchCopy.textContent = matrixSummary
      ? matrixSummary.userSummary
      : comparison
        ? comparison.userSummary
        : dashboardBatchSummary
          ? dashboardBatchSummary.userSummary
          : "여러 번 테스트를 누르면 비슷한 입력을 반복해 안정성을 보여 줍니다.";
    dashboardComparisonLanes.innerHTML = comparison
      ? renderDashboardComparisonLanes(comparison)
      : renderEmptyDashboardPlot("baseline / tutorial / threshold 비교 대기");
    dashboardComparisonTable.innerHTML = comparison ? renderDashboardComparisonTable(comparison) : "";
    dashboardStatusBars.innerHTML = plotModel ? renderDashboardBars(plotModel.statusBars) : renderEmptyDashboardPlot("상태 분포 대기");
    dashboardQualityBars.innerHTML = plotModel ? renderDashboardBars(plotModel.qualityBars) : renderEmptyDashboardPlot("품질 분포 대기");
    dashboardHeatmap.innerHTML = plotModel ? renderDashboardHeatmap(plotModel) : renderEmptyDashboardPlot("헷갈림 지도 대기");
    dashboardScatter.innerHTML = plotModel ? renderDashboardScatter(plotModel) : renderEmptyDashboardPlot("점 분포 대기");
    dashboardFamilyDistribution.innerHTML = matrixSummary
      ? renderDashboardFamilyDistribution(matrixSummary)
      : renderEmptyDashboardPlot("모양별 n회 생성 대기");
    dashboardOverlapTable.innerHTML = matrixSummary
      ? renderDashboardOverlapTable(matrixSummary)
      : renderEmptyDashboardPlot("겹침 특성 대기");
    dashboardBatchLog.innerHTML = renderDashboardBatchLog(dashboardFamilyMatrixLog, selectedMatrixSummary?.id);

    const fixtureTone = dashboardFixtureResult?.ok ? "recognized" : dashboardFixtureResult ? "invalid" : "waiting";
    dashboardFixtureStatus.textContent = dashboardFixtureResult ? (dashboardFixtureResult.ok ? "읽음" : "확인 필요") : "대기";
    dashboardFixtureStatus.className = `status-chip status-${fixtureTone}`;
    dashboardFixtureSummary.innerHTML = renderMetricNotes([
      dashboardFixtureResult?.userMessage ?? "예시 데이터를 붙여 넣으면 미리보기로만 확인합니다.",
      datacardAuthoringResult?.registry ? "설명 카드 패치가 guide 미리보기에 연결되었습니다. 실제 판정은 그대로입니다." : "localStorage는 자동으로 바꾸지 않습니다."
    ]);
    dashboardReportViewer.textContent = JSON.stringify(
      buildDashboardReport(sample, dashboardBatchSummary, dashboardFixtureResult, matrixSummary),
      null,
      2
    );
  }

  function render(): void {
    drawCanvas(ctx, {
      phase,
      overlayAuthoringStarted,
      baseSession,
      overlaySession,
      previewResult,
      baseSealResult,
      currentOverlayPreview,
      overlayRecords,
      compiledResult,
      analysisOverlay: demoView.analysisOverlay,
      whatIfPreview: buildActiveWhatIfPreviewModel()
    });
    updateSidebar();
    scheduleAdaptiveLayoutBounds();
  }

  function updateSidebar(): void {
    const baseDisplay = baseSealResult ?? previewResult;
    const recognizedOverlays = overlayRecords.filter(
      (record) => record.recognition.status === "recognized" && record.recognition.operator
    );
    const overlayLive = currentOverlayPreview ?? overlayRecords[overlayRecords.length - 1]?.recognition ?? null;
    const activeQuality = baseDisplay.rawQuality;
    const adjustedQuality = baseDisplay.adjustedQuality;
    const baseSealed = Boolean(baseSealResult?.canonicalFamily);
    const compare = buildDemoOutcomeCompare(createOutcomeInput(baseDisplay, overlayRecords));
    const activeOutcome = demoView.qualityInfluence ? compare.on : compare.off;
    const compilePreview =
      compiledResult ?? (baseSealResult?.canonicalFamily ? compileSealResult(baseSealResult, overlayRecords, latestProfileDelta) : null);
    const scenario = scenarioAppeal(demoView.selectedScenarioId);
    const tutorialSteps = currentTutorialSteps();
    const personalizationModel = buildPersonalizationDemoModel(
      baseDisplay,
      overlayLive,
      tutorialProfileStore,
      demoView
    );
    const tutorialModel = buildTutorialFlowModel({
      tutorialSteps,
      stepIndex: tutorialStepIndex,
      completedStepIds: tutorialCompletedStepIds,
      tutorialFlowActive,
      beforeSnapshot: tutorialBeforeSnapshot,
      afterSnapshot: tutorialAfterSnapshot,
      baseSession,
      overlaySession,
      baseSealResult,
      overlayRecords,
      tutorialStore: tutorialProfileStore
    });
    const datacardShapeRecognition = recognizeSessionWithDatacard(
      baseSession,
      activeDatacardShapePreset(),
      datacardShapeCaptureStore,
      currentRecognitionProfile()
    );

    syncTinyMlRuntimeMetadata(root, baseDisplay, overlayLive);

    syncPresetButtons(demoView.viewPreset, [
      ["clean", presetCleanButton],
      ["explain", presetExplainButton],
      ["workshop", presetWorkshopButton]
    ]);
    qualityToggle.checked = demoView.qualityInfluence;
    compareToggle.checked = demoView.compareMode;
    explainToggle.checked = demoView.explainResult;
    analysisToggle.checked = demoView.analysisOverlay;
    detailsToggle.checked = demoView.showQualitySplit;
    personalizationToggle.checked = demoView.showPersonalizationPanel;
    tutorialToggle.checked = demoView.showTutorialFlowPanel;
    exemplarToggle.checked = demoView.showExemplarPanel;
    presetChip.textContent = viewPresetLabel(demoView.viewPreset);
    presetChip.className = `status-chip status-${demoView.viewPreset === "workshop" ? "authoring" : "ready"}`;
    scenarioTitle.textContent = scenario.title;
    scenarioCopy.textContent = scenario.prompt;
    narrationCopy.textContent = buildNarrationCopy(scenario.narration, phase, baseSealed, demoView.analysisOverlay);
    supportStripCopy.textContent = personalizationModel.stripCopy;
    supportStripBadges.innerHTML = personalizationModel.stripBadges;
    syncScenarioButtons(scenarioChipButtons, demoView.selectedScenarioId);

    syncPageLayout(demoView.activePage, baseDisplay, overlayLive, tutorialProfileStore);
    if (demoView.activePage === "dashboard") {
      updateDashboardPanel();
    }

    phaseTitle.textContent = phaseTitleFor(phase, baseSealed, overlayAuthoringStarted);
    phaseCopy.textContent = phaseCopyFor(phase, baseSealed, overlayAuthoringStarted, compiledResult);
    sealBaseButton.disabled = phase !== "base" || baseSession.strokes.length === 0 || baseSealed;
    startOverlayButton.disabled = phase !== "base" || !baseSealed;
    sealFinalButton.disabled = phase !== "overlay" || !baseSealed;
    undoButton.disabled =
      (phase === "base" && baseSession.strokes.length === 0) ||
      (phase === "overlay" && overlaySession.strokes.length === 0) ||
      (phase === "final" && !baseSealed);
    phaseBase.className = phaseChipClass(phase === "base", baseSealed && !overlayAuthoringStarted && phase === "base");
    phaseOverlay.className = phaseChipClass(phase === "overlay", baseSealed && phase === "base");
    phaseFinal.className = phaseChipClass(phase === "final", phase === "overlay");

    baseFamily.textContent = baseDisplay.canonicalFamily
      ? familyLabel(baseDisplay.canonicalFamily)
      : baseDisplay.topCandidate
        ? familyLabel(baseDisplay.topCandidate.family)
        : "후보 없음";
    baseStatus.textContent = statusLabel(baseDisplay.status);
    baseStatus.className = `status-chip status-${baseDisplay.status}`;
    baseReason.textContent =
      !baseSealed && baseSealDetection?.reason
        ? baseSealDetection.reason
        :
      baseDisplay.invalidReason ??
      (baseDisplay.topCandidate
        ? `가장 가까운 모양 점수 ${(baseDisplay.topCandidate.score * 100).toFixed(1)} / 전체 완성도 ${(averageQuality(activeQuality) * 100).toFixed(0)}`
        : "아직 충분한 입력이 없습니다.");
    candidateList.innerHTML = baseDisplay.candidates
      .slice(0, 5)
      .map(
        (candidate) => `
          <li>
            <span>${familyLabel(candidate.family)}</span>
            <strong>${(candidate.score * 100).toFixed(1)}</strong>
          </li>
        `
      )
      .join("");

    overlayPreviewTitle.textContent = overlayLive?.operator
      ? operatorLabel(overlayLive.operator)
      : overlayLive?.topCandidate?.operator
        ? operatorLabel(overlayLive.topCandidate.operator)
        : baseSealed
          ? phase === "overlay"
            ? "추가 효과를 그려 보세요"
            : "추가 효과 전"
          : "기본 모양 고정 필요";
    overlayPreviewStatus.textContent =
      statusLabel(overlayLive?.status ?? (baseSealed ? (phase === "overlay" ? "ready" : "waiting") : "waiting"));
    overlayPreviewStatus.className = `status-chip status-${overlayLive?.status ?? (phase === "overlay" ? "ready" : "waiting")}`;
    overlayPreviewReason.textContent =
      overlayLive?.invalidReason ??
      (!baseSealed
        ? "기본 모양을 먼저 고정해야 추가 효과 미리보기가 열립니다."
        : phase === "base"
          ? "추가 효과 그리기를 누르면 현재 모양을 기준으로 미리보기가 열립니다."
          : "현재 선이 어떤 추가 효과로 읽히는지 실시간으로 확인합니다.");
    overlayPreviewMeta.innerHTML = renderOverlayPreviewMeta(overlayLive);
    overlayPreviewCandidates.innerHTML = overlayLive
      ? overlayLive.candidates
          .slice(0, 4)
          .map(
            (candidate) => `
              <li>
                <span>${operatorLabel(candidate.operator)}</span>
                <strong>${candidate.anchorZoneId ?? "zone?"} · ${(candidate.score * 100).toFixed(1)}</strong>
              </li>
            `
          )
          .join("")
      : `<li><span>미리보기 대기</span><strong>기본 모양 고정 -> 추가 효과 그리기</strong></li>`;

    overlayTitle.textContent = `추가 효과 ${recognizedOverlays.length}개`;
    overlayStatus.textContent = statusLabel(
      phase === "final" ? "compiled" : baseSealed ? (phase === "overlay" ? "authoring" : "ready") : "waiting"
    );
    overlayStatus.className = `status-chip status-${phase === "final" ? "compiled" : baseSealed ? (phase === "overlay" ? "ready" : "waiting") : "waiting"}`;
    overlayReason.textContent =
      !baseSealed
        ? "추가 효과는 기본 모양을 고정한 뒤에만 읽습니다."
        : phase === "base"
          ? "지금은 기본 모양만 고정되어 있고 추가 효과는 아직 시작 전입니다."
          : phase === "overlay"
            ? "원본 선은 그대로 두고, 읽힌 추가 효과만 따로 기록합니다."
            : "최종 결과가 고정되면 다음 입력부터 새 시도가 시작됩니다.";
    overlayList.innerHTML =
      overlayRecords.length > 0
        ? overlayRecords
            .map((record) => {
              const label =
                record.recognition.operator ?? record.recognition.topCandidate?.operator ?? "rejected_stroke";
              const score = record.recognition.topCandidate ? (record.recognition.topCandidate.score * 100).toFixed(1) : "--";
              return `
                <li>
                  <span>${operatorLabel(label)}</span>
                  <strong>${statusLabel(record.recognition.status)} · ${score}</strong>
                </li>
              `;
            })
            .join("")
        : `<li><span>기록 없음</span><strong>기본 모양 고정 후 활성화</strong></li>`;

    if (compiledResult) {
      compileTitle.textContent = `${familyLabel(compiledResult.baseFamily)} 결과 확정`;
      compileStatus.textContent = statusLabel("compiled");
      compileStatus.className = "status-chip status-compiled";
      compileReason.textContent = `${compiledResult.summary} · ${activeOutcome.summary}`;
      compileSummary.innerHTML = renderCompileSummary(compiledResult);
    } else if (baseSealResult?.canonicalFamily) {
      compileTitle.textContent = "최종 결과 대기";
      compileStatus.textContent = statusLabel("waiting");
      compileStatus.className = "status-chip status-waiting";
      compileReason.textContent = `추가 효과를 더한 뒤 최종 결과 보기로 결과를 확정합니다. ${activeOutcome.summary}`;
      compileSummary.innerHTML = compilePreview ? renderCompileSummary(compilePreview) : "";
    } else {
      compileTitle.textContent = "최종 결과 전";
      compileStatus.textContent = statusLabel("waiting");
      compileStatus.className = "status-chip status-waiting";
      compileReason.textContent = "기본 모양을 고정하고 추가 효과를 더한 뒤 최종 결과를 확인합니다.";
      compileSummary.innerHTML = "";
    }
    updateDatacardShapeLab(datacardShapeRecognition);

    outcomeTitle.textContent = baseSealed ? `${familyLabel(baseSealResult?.canonicalFamily ?? "wind")} 결과감 비교` : "품질 전후 비교";
    qualityActiveBadge.textContent = demoView.qualityInfluence ? "품질 반영 켬" : "품질 반영 끔";
    qualityActiveBadge.className = `status-chip status-${demoView.qualityInfluence ? "recognized" : "waiting"}`;
    outcomeCopy.textContent = activeOutcome.summary;
    outcomeCompare.innerHTML = renderOutcomeCompare(compare, demoView.qualityInfluence);

    whyTitle.textContent = activeOutcome.family ? `${familyLabel(activeOutcome.family)}형으로 읽힌 이유` : "판정 이유";
    whyStatus.textContent = statusLabel(baseDisplay.status);
    whyStatus.className = `status-chip status-${baseDisplay.status}`;
    whyCopy.textContent =
      demoView.qualityInfluence
        ? "모양 판정 이유와 품질 때문에 달라진 결과감을 짧은 문장으로 설명합니다."
        : "품질 반영을 끈 상태에서는 모양 판정과 기본 결과감만 설명합니다.";
    whyList.innerHTML = renderWhyPanel(baseDisplay, compare, demoView.qualityInfluence, overlayLive, compilePreview);

    supportTitle.textContent = insightTab === "assist" ? "현재 판정과 참고 계산" : tutorialModel.compareTitle;
    supportStatus.textContent = insightTab === "assist" ? personalizationModel.cardStatusLabel : tutorialModel.compareStatusLabel;
    supportStatus.className = `status-chip status-${insightTab === "assist" ? personalizationModel.cardStatusTone : tutorialModel.compareStatusTone}`;
    supportCopy.textContent = insightTab === "assist" ? personalizationModel.compareCopy : tutorialModel.compareCopy;
    supportBadges.innerHTML = personalizationModel.cardBadges;
    supportMetrics.innerHTML = personalizationModel.metricRows;
    insightAssistTab.className = ["chip-button", insightTab === "assist" ? "active" : ""].filter(Boolean).join(" ");
    insightPracticeTab.className = ["chip-button", insightTab === "practice" ? "active" : ""].filter(Boolean).join(" ");
    insightAssistPanel.hidden = insightTab !== "assist";
    insightPracticePanel.hidden = insightTab !== "practice";
    tutorialTitle.textContent = tutorialModel.title;
    tutorialStatus.textContent = tutorialModel.statusLabel;
    tutorialStatus.className = `status-chip status-${tutorialModel.statusTone}`;
    tutorialCopy.textContent = tutorialModel.copy;
    tutorialProgress.innerHTML = tutorialModel.progressBadges;
    tutorialStepCard.innerHTML = tutorialModel.stepCard;
    tutorialStepList.innerHTML = tutorialModel.stepList;
    tutorialStartButton.disabled = tutorialModel.startDisabled;
    tutorialCaptureButton.disabled = tutorialModel.captureDisabled;
    tutorialResultButton.disabled = tutorialModel.resultDisabled;
    tutorialClearButton.disabled = tutorialModel.clearDisabled;
    personalizationCompare.innerHTML = personalizationModel.compareLanes;
    personalizationEffects.innerHTML = personalizationModel.effectRows;
    tutorialCompareGrid.innerHTML = tutorialModel.compareGrid;
    tutorialCompareEffects.innerHTML = tutorialModel.compareEffects;
    tutorialCompareMetrics.innerHTML = tutorialModel.compareMetrics;
    readingGuideTitle.textContent = "설명 기준과 모범 선례";
    readingGuideCopy.textContent = "같은 모양은 같은 종류로 유지한 채, 화면에서 무엇을 기준으로 읽는지와 안정적인 예시를 함께 보여 줍니다.";
    principlesList.innerHTML = renderHciPrinciples();
    magicWhatIfGuide.innerHTML = renderMagicWhatIfGuide(
      activeWhatIfScenarioId,
      showWhatIfOverlay,
      currentWhatIfScenarios()
    );
    datacardPresetList.innerHTML = renderDatacardPresetButtons();
    const datacardDisplay = buildDatacardAuthoringDisplay(datacardAuthoringResult);
    datacardPreviewStatus.textContent = datacardDisplay.statusLabel;
    datacardPreviewStatus.className = `status-chip status-${datacardDisplay.statusTone}`;
    datacardPreviewSummary.innerHTML = renderSummaryRows(datacardDisplay.summaryRows);
    datacardPreviewIssues.innerHTML = renderMetricNotes(datacardDisplay.issueLines);
    const backfillPreview = previewTutorialProfileCardBackfill(tutorialProfileStore);
    tutorialBackfillButton.disabled = !backfillPreview.canBackfill;
    tutorialBackfillButton.textContent = backfillPreview.canBackfill
      ? "현재 카드 기준으로 보강"
      : backfillPreview.reason === "already_current"
        ? "카드 메타데이터 최신"
        : "보강 불가";
    readingGuideExemplar.hidden = demoView.activePage === "guide" ? false : !demoView.showExemplarPanel;
    exemplarTitle.textContent = personalizationModel.exemplarTitle;
    exemplarCopy.textContent = personalizationModel.exemplarCopy;
    exemplarGrid.innerHTML = personalizationModel.exemplarGrid;

    qualitySummary.innerHTML = renderQualitySummary(activeQuality, adjustedQuality);
    rawQualityList.innerHTML = renderQuality(activeQuality);
    adjustedQualityList.innerHTML = renderQuality(adjustedQuality);

    recentSealsTitle.textContent = recentSealSnapshots.length > 0 ? "바로 비교" : "최근 결과";
    recentCount.textContent = `${Math.max(recentSealSnapshots.length, 3)} slots`;
    recentSeals.innerHTML =
      recentSealSnapshots.length > 0
        ? renderRecentSeals(recentSealSnapshots, demoView.qualityInfluence, {
            status: baseDisplay.status,
            family: readCurrentFamily(baseDisplay),
            compare,
            overlayCount: recognizedOverlays.length
          })
        : renderRecentSeals([], demoView.qualityInfluence, {
            status: baseDisplay.status,
            family: readCurrentFamily(baseDisplay),
            compare,
            overlayCount: recognizedOverlays.length
          });

    profileSamples.textContent = personalizationModel.profileTitle;
    profileDelta.textContent = personalizationModel.profileCopy;
    profileBaselineList.innerHTML = personalizationModel.profileSummary;
    profileDeltaList.innerHTML = personalizationModel.profileDetails;

    logCount.textContent = `${logs.length}건`;
    logViewer.textContent = JSON.stringify(logs, null, 2);
    canvasHint.textContent =
      demoView.activePage === "tutorial"
        ? buildTutorialCanvasHint(tutorialSteps[tutorialStepIndex] ?? tutorialSteps[0] ?? TUTORIAL_DEMO_STEPS[0], {
            phase,
            baseSealed,
            overlayAuthoringStarted
          })
        : !baseSealed
          ? "1. 기본 모양을 그리고, 마지막에 기본 도형을 감싸는 원을 그리면 자동으로 기본 모양이 고정됩니다."
          : phase === "base"
            ? `2. 모양이 고정됐습니다. ${scenario.title} 시나리오에 맞춰 품질 전후 비교나 추가 효과 그리기를 보여줍니다.`
            : phase === "overlay"
              ? "3. 추가 효과 선은 그대로 그리고, 분석 안내선은 보조로만 겹칩니다."
              : "4. 최종 결과가 고정됐습니다. 다음 입력을 시작하면 새 시도가 열립니다.";
    analysisCopy.textContent = demoView.analysisOverlay
      ? "분석 안내선을 켜면 축선, 닫힘 보조선, 위치 힌트, 가이드 모양이 보이지만 원본 선은 그대로 유지됩니다."
      : "분석 안내선을 끄면 핵심 결과만 남기고 보조선은 숨깁니다.";
    analysisLegend.innerHTML = renderAnalysisLegend(demoView.analysisOverlay);
  }

  function syncPageLayout(
    page: DemoPage,
    baseDisplay: RecognitionResult,
    overlayLive: OverlayRecognition | null,
    store: TutorialProfileStore
  ): void {
    const meta = WEB_UI_PAGES.find((item) => item.id === page) ?? WEB_UI_PAGES[0];
    const mlMetadata = buildTinyMlRuntimeMetadata(baseDisplay, overlayLive);

    const workspaceLayout =
      page === "test" ? "test-layout" : page === "tutorial" ? "tutorial-layout" : page === "dashboard" ? "dashboard-layout" : "analysis-layout";

    root.dataset.activePage = page;
    workspace.className = ["workspace", workspaceLayout].join(" ");
    pageTitle.textContent = meta.title;
    pageCopy.textContent = meta.copy;
    pageSummaryBadges.innerHTML = renderPageSummaryBadges(page, baseDisplay, overlayLive, store);

    for (const button of pageNavButtons) {
      const active = button.dataset.pageId === page;
      button.className = ["page-tab", active ? "active" : ""].filter(Boolean).join(" ");
      button.setAttribute("aria-current", active ? "page" : "false");
    }

    boardPanel.hidden = page !== "test" && page !== "tutorial";
    dashboardPanel.hidden = page !== "dashboard";
    dashboardPanel.setAttribute("aria-hidden", page === "dashboard" ? "false" : "true");
    sidebar.hidden = page === "dashboard";
    baseCard.hidden = page !== "test";
    overlayPreviewCard.hidden = page !== "test";
    overlayRecordsCard.hidden = page !== "test";
    compileCard.hidden = page !== "test";
    datacardShapeLabCard.hidden = page !== "test";
    outcomeCard.hidden = page !== "quality";
    whyCard.hidden = page !== "guide";
    supportCard.hidden = page !== "ml" && page !== "tutorial";
    mlRuntimeCard.hidden = page !== "ml";
    tutorialCard.hidden = page !== "tutorial";
    tutorialProfileCard.hidden = page !== "tutorial";
    principlesCard.hidden = page !== "guide";
    exemplarCard.hidden = true;
    qualityCard.hidden = page !== "quality";
    recentSealsCard.hidden = page !== "logs";
    profileCard.hidden = page !== "logs";
    logCard.hidden = page !== "logs";

    mlRuntimeStatus.textContent =
      mlMetadata.tinyMlBaseArtifacts === "ready" || mlMetadata.tinyMlOperatorArtifacts === "ready" ? "연결됨" : "대기";
    mlRuntimeStatus.className = `status-chip status-${
      mlMetadata.tinyMlBaseArtifacts === "ready" || mlMetadata.tinyMlOperatorArtifacts === "ready" ? "recognized" : "waiting"
    }`;
    mlRuntimeCopy.textContent =
      mlMetadata.baseMlActualGate !== "none" || mlMetadata.operatorMlActualGate !== "none"
        ? "현재 입력에서 보조 판독이 실제 인식 기준 또는 오인식 차단에 개입했습니다."
        : "현재 입력에서는 보조 판독이 참고 계산과 인식 기준 상태만 제공합니다.";
    mlRuntimeSummary.innerHTML = renderSummaryRows([
      ["기능 목록", readinessLabel(mlMetadata.tinyMlFeatureSpec)],
      ["기본 모양 모델", readinessLabel(mlMetadata.tinyMlBaseArtifacts)],
      ["추가 효과 모델", readinessLabel(mlMetadata.tinyMlOperatorArtifacts)]
    ]);
    mlBaseRows.innerHTML = renderMlRows([
      ["현재 판정", `${metadataFamilyLabel(mlMetadata.baseActualFamily)} / ${statusLabel(mlMetadata.baseActualStatus as RecognitionResult["status"] | "waiting")}`],
      ["보조 판독 변화", `${booleanChangeLabel(mlMetadata.baseShadowDecisionChanged)} / ${booleanChangeLabel(mlMetadata.baseShadowStatusChanged)}`],
      ["연습 반영 후보", mlMetadata.basePersonalizedShadowTopLabel],
      ["인식 기준", `${mlMetadata.baseThresholdBias} -> ${mlMetadata.baseEffectiveThresholdBias}`],
      ["믿음도 조절", mlMetadata.baseMlConfidenceGate],
      ["실제 반영", gateLabel(mlMetadata.baseMlActualGate)]
    ]);
    mlOperatorRows.innerHTML = renderMlRows([
      ["현재 판정", `${metadataOperatorLabel(mlMetadata.operatorActualLabel)} / ${statusLabel(mlMetadata.operatorActualStatus as RecognitionResult["status"] | "waiting")}`],
      ["보조 판독 변화", `${booleanChangeLabel(mlMetadata.operatorShadowDecisionChanged)} / ${booleanChangeLabel(mlMetadata.operatorShadowStatusChanged)}`],
      ["연습 반영 후보", mlMetadata.operatorPersonalizedShadowTopLabel],
      ["인식 기준", `${mlMetadata.operatorThresholdBias} -> ${mlMetadata.operatorEffectiveThresholdBias}`],
      ["믿음도 조절", mlMetadata.operatorMlConfidenceGate],
      ["실제 반영", gateLabel(mlMetadata.operatorMlActualGate)]
    ]);

    tutorialValidationStatus.textContent =
      (store.shapeProfile.validatedTutorialSampleCount ?? 0) > 0 ? "반영 중" : "대기";
    tutorialValidationStatus.className = `status-chip status-${
      (store.shapeProfile.validatedTutorialSampleCount ?? 0) > 0 ? "recognized" : "waiting"
    }`;
    tutorialValidationCopy.textContent =
      (store.shapeProfile.feedbackOnlyTutorialSampleCount ?? 0) > 0
        ? "참고만 저장된 연습은 기록으로만 남기고 인식 기준에는 반영하지 않습니다."
        : "잘 맞게 저장된 연습이 쌓이면 내 손모양 기준과 라벨별 인식 기준이 갱신됩니다.";
    tutorialValidationSummary.innerHTML = renderTutorialValidationSummary(store);
    tutorialValidationDetails.innerHTML = renderTutorialValidationDetails(store);
  }

  function refreshRecognitionState(): void {
    previewResult = recognizeSession(baseSession, { sealed: false, profile: currentRecognitionProfile() });

    const shouldReplayBase = Boolean(baseSealResult?.canonicalFamily) || phase !== "base" || overlayAuthoringStarted;

    if (!shouldReplayBase || baseSession.strokes.length === 0) {
      if (!shouldReplayBase) {
        currentOverlayPreview = null;
      }
      return;
    }

    const sealedReplay = recognizeSealedBaseSession(baseSession, { profile: currentRecognitionProfile() });
    baseSealDetection = sealedReplay.sealDetection;
    baseSealResult = sealedReplay.sealDetection.ok && sealedReplay.result.canonicalFamily ? sealedReplay.result : null;

    if (!baseSealResult?.canonicalFamily) {
      currentOverlayPreview = null;
      overlayRecords = [];
      compiledResult = null;
      return;
    }

    const replay = replayOverlaySeries(baseSession, overlaySession.strokes);
    overlayRecords = replay.records;
    currentOverlayPreview =
      phase === "overlay"
        ? replay.lastRecognition
        : replay.records[replay.records.length - 1]?.recognition ?? null;

    if (compiledResult) {
      compiledResult = compileSealResult(baseSealResult, overlayRecords, latestProfileDelta);
    }
  }

  function replayOverlaySeries(
    replayBaseSession: StrokeSession,
    overlayStrokes: Stroke[]
  ): { records: OverlayStrokeRecord[]; lastRecognition: OverlayRecognition | null } {
    const replaySession: StrokeSession = {
      strokes: [],
      startedAt:
        overlayStrokes[0]?.points[0]?.t ??
        replayBaseSession.endedAt ??
        replayBaseSession.startedAt
    };
    const records: OverlayStrokeRecord[] = [];
    let lastRecognition: OverlayRecognition | null = null;

    for (const stroke of overlayStrokes) {
      const replayStroke = structuredClone(stroke);
      replaySession.strokes.push(replayStroke);
      replaySession.endedAt = replayStroke.points[replayStroke.points.length - 1]?.t ?? replaySession.startedAt;
      const recognition = recognizeOverlayStroke(
        replayStroke,
        createOverlayContext(
          replayBaseSession,
          records,
          replaySession,
          createTutorialOverlayPersonalizationProfile(tutorialProfileStore)
        )
      );
      records.push({ stroke: replayStroke, recognition });
      lastRecognition = recognition;
    }

    return { records, lastRecognition };
  }

  function createTutorialComparisonSnapshot(profile: UserInputProfile): TutorialComparisonSnapshot | null {
    if (baseSession.strokes.length === 0) {
      return null;
    }

    const snapshotBaseSession = structuredClone(baseSession);
    const snapshotOverlayStrokes = structuredClone(overlaySession.strokes);
    const baseSealed = Boolean(baseSealResult?.canonicalFamily);
    const snapshotBaseResult = baseSealed
      ? recognizeSealedBaseSession(snapshotBaseSession, { profile }).result
      : recognizeSession(snapshotBaseSession, { sealed: false, profile });
    const replayedOverlay =
      baseSealed && snapshotBaseResult.canonicalFamily
        ? replayOverlaySeries(snapshotBaseSession, snapshotOverlayStrokes)
        : { records: [] as OverlayStrokeRecord[], lastRecognition: null as OverlayRecognition | null };

    return {
      capturedAt: Date.now(),
      baseSession: snapshotBaseSession,
      overlayStrokes: snapshotOverlayStrokes,
      baseSealed,
      baseResult: snapshotBaseResult,
      overlayRecognition: replayedOverlay.lastRecognition,
      overlayRecords: replayedOverlay.records,
      tutorialSampleCount: tutorialProfileStore.shapeProfile.tutorialSampleCount
    };
  }

  function replayTutorialComparisonSnapshot(source: TutorialComparisonSnapshot): TutorialComparisonSnapshot {
    const nextBaseSession = structuredClone(source.baseSession);
    const nextOverlayStrokes = structuredClone(source.overlayStrokes);
    const nextBaseResult = source.baseSealed
      ? recognizeSealedBaseSession(nextBaseSession, { profile: currentRecognitionProfile() }).result
      : recognizeSession(nextBaseSession, { sealed: false, profile: currentRecognitionProfile() });
    const replayedOverlay =
      source.baseSealed && nextBaseResult.canonicalFamily
        ? replayOverlaySeries(nextBaseSession, nextOverlayStrokes)
        : { records: [] as OverlayStrokeRecord[], lastRecognition: null as OverlayRecognition | null };

    return {
      capturedAt: Date.now(),
      baseSession: nextBaseSession,
      overlayStrokes: nextOverlayStrokes,
      baseSealed: source.baseSealed,
      baseResult: nextBaseResult,
      overlayRecognition: replayedOverlay.lastRecognition,
      overlayRecords: replayedOverlay.records,
      tutorialSampleCount: tutorialProfileStore.shapeProfile.tutorialSampleCount
    };
  }

  function recordTutorialDemoStep(step: TutorialDemoStep): TutorialCapture | null {
    if (step.kind === "family" && step.expectedFamily) {
      return tutorialOnboardingHook.recordCapture({
        kind: "family",
        expectedFamily: step.expectedFamily,
        source: step.source,
        validation: buildFamilyTutorialCaptureValidation(step.expectedFamily)
      });
    }

    if (step.kind === "operator" && step.expectedOperator) {
      const stroke = overlayRecords[overlayRecords.length - 1]?.stroke ?? overlaySession.strokes[overlaySession.strokes.length - 1];

      if (!stroke) {
        return null;
      }

      return tutorialOnboardingHook.recordCapture({
        kind: "operator",
        expectedOperator: step.expectedOperator,
        source: step.source,
        strokes: [structuredClone(stroke)],
        validation: buildOperatorTutorialCaptureValidation(step.expectedOperator, stroke)
      });
    }

    return null;
  }

  function buildFamilyTutorialCaptureValidation(expectedFamily: GlyphFamily): TutorialCaptureValidation {
    const result = recognizeSession(structuredClone(baseSession), {
      sealed: true,
      profile: currentRecognitionProfile()
    });
    const topCandidate = result.topCandidate;
    const secondCandidate = result.candidates[1];
    const margin = topCandidate ? topCandidate.score - (secondCandidate?.score ?? 0) : 0;
    const reliability: TutorialCaptureValidation["reliability"] =
      result.status === "recognized" && result.canonicalFamily === expectedFamily
        ? "high"
        : result.status === "ambiguous" && topCandidate?.family === expectedFamily
          ? "medium"
          : "feedback_only";

    return {
      reliability,
      expectedLabel: expectedFamily,
      actualTopLabel: topCandidate?.family,
      status: result.status,
      topScore: topCandidate?.score,
      margin,
      quality: result.rawQuality
    };
  }

  function buildOperatorTutorialCaptureValidation(
    expectedOperator: OverlayOperator,
    stroke: Stroke
  ): TutorialCaptureValidation {
    const latestRecord = overlayRecords[overlayRecords.length - 1];
    const recognition =
      latestRecord?.stroke.id === stroke.id
        ? latestRecord.recognition
        : recognizeOverlayStroke(
            stroke,
            createOverlayContext(
              baseSession,
              overlayRecords,
              overlaySession,
              createTutorialOverlayPersonalizationProfile(tutorialProfileStore)
            )
          );
    const topCandidate = recognition.topCandidate as
      | (NonNullable<OverlayRecognition["topCandidate"]> & {
          anchorScore?: number;
          scaleScore?: number;
          shapeConfidence?: number;
        })
      | undefined;
    const secondCandidate = recognition.candidates[1];
    const margin = topCandidate ? topCandidate.score - (secondCandidate?.score ?? 0) : 0;
    const reliability: TutorialCaptureValidation["reliability"] =
      recognition.status === "recognized" && recognition.operator === expectedOperator
        ? "high"
        : recognition.status === "ambiguous" && topCandidate?.operator === expectedOperator && !topCandidate.blockedBy
          ? "medium"
          : "feedback_only";

    return {
      reliability,
      expectedLabel: expectedOperator,
      actualTopLabel: topCandidate?.operator,
      status: recognition.status,
      topScore: topCandidate?.score,
      margin,
      anchorScore: topCandidate?.anchorScore,
      scaleScore: topCandidate?.scaleScore,
      shapeConfidence: topCandidate?.shapeConfidence,
      blockedBy: topCandidate?.blockedBy
    };
  }
}

function createOutcomeInput(result: RecognitionResult, overlayRecords: OverlayStrokeRecord[]): DemoOutcomeInput {
  return {
    family: result.canonicalFamily ?? null,
    status: result.status,
    rawQuality: result.rawQuality,
    adjustedQuality: result.adjustedQuality,
    overlayOperators: overlayRecords
      .filter((record) => record.recognition.status === "recognized")
      .map((record) => record.recognition.operator)
      .filter((operator): operator is OverlayOperator => Boolean(operator))
  };
}

function scenarioAppeal(id: GuidedDemoScenarioId) {
  return SCENARIO_APPEAL[id];
}

function buildNarrationCopy(
  baseNarration: string,
  phase: RitualPhase,
  baseSealed: boolean,
  analysisOverlay: boolean
): string {
  if (!baseSealed) {
    return `${baseNarration} 먼저 같은 모양을 유지한 채 기본 종류를 고정하세요.`;
  }

  if (phase === "overlay") {
    return `${baseNarration} 추가 효과 선은 원본 그대로 유지하고 가이드 모양만 보조로 겹칩니다${analysisOverlay ? "." : ". 필요하면 분석 안내선을 켜세요."}`;
  }

  if (phase === "final") {
    return `${baseNarration} 최종 결과 카드에서 결과 특징과 이유 설명을 3문장 안에 요약하세요.`;
  }

  return baseNarration;
}

function viewPresetLabel(preset: DemoViewPreset): string {
  switch (preset) {
    case "clean":
      return "간단히";
    case "explain":
      return "설명 포함";
    case "workshop":
      return "검증용";
  }
}

function buildTutorialCanvasHint(
  step: TutorialDemoStep,
  state: { phase: RitualPhase; baseSealed: boolean; overlayAuthoringStarted: boolean }
): string {
  if (step.kind === "family") {
    return `${step.shortLabel}: 캔버스에 ${step.shapeSummary}을 그린 뒤 '연습에 저장'을 누르세요.`;
  }

  if (!state.baseSealed) {
    return `${step.shortLabel}: 먼저 캔버스에 기본 모양을 그리고 '기본 모양 고정'을 누르세요.`;
  }

  if (state.phase !== "overlay" || !state.overlayAuthoringStarted) {
    return `${step.shortLabel}: '추가 효과 그리기'를 누른 뒤 ${step.shapeSummary}을 더해 주세요.`;
  }

  return `${step.shortLabel}: ${step.shapeSummary}을 그린 뒤 '연습에 저장'을 누르세요.`;
}

function syncPresetButtons(
  activePreset: DemoViewPreset,
  items: Array<[DemoViewPreset, HTMLButtonElement]>
): void {
  for (const [preset, button] of items) {
    button.className = ["chip-button", preset === activePreset ? "active" : ""].filter(Boolean).join(" ");
  }
}

function syncScenarioButtons(buttons: HTMLButtonElement[], activeScenarioId: GuidedDemoScenarioId): void {
  for (const button of buttons) {
    button.className = [
      "chip-button",
      "scenario-chip",
      button.dataset.scenarioId === activeScenarioId ? "active" : ""
    ]
      .filter(Boolean)
      .join(" ");
  }
}

function resolvePresetView(state: DemoViewState, preset: DemoViewPreset): DemoViewState {
  switch (preset) {
    case "clean":
      return {
        ...state,
        viewPreset: preset,
        compareMode: true,
        explainResult: false,
        analysisOverlay: false,
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
        compareMode: true,
        explainResult: true,
        analysisOverlay: false,
        showQualitySplit: false,
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
        compareMode: true,
        explainResult: true,
        analysisOverlay: true,
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

function createOverlayContext(
  baseSession: StrokeSession,
  overlayRecords: OverlayStrokeRecord[],
  overlaySession?: StrokeSession,
  personalizationProfile?: ReturnType<typeof createTutorialOverlayPersonalizationProfile>
): {
  referenceFrame: ReturnType<typeof createOverlayReferenceFrame>;
  existingOperators: OverlayOperator[];
  overlaySession?: StrokeSession;
  personalizationProfile?: ReturnType<typeof createTutorialOverlayPersonalizationProfile>;
} {
  return {
    referenceFrame: createOverlayReferenceFrame(baseSession),
    existingOperators: overlayRecords
      .map((record) => record.recognition.operator)
      .filter((operator): operator is OverlayOperator => Boolean(operator)),
    overlaySession,
    personalizationProfile
  };
}

function createTutorialBaseSnapshot(referenceFrame: OverlayReferenceFrame): TutorialBaseSnapshot {
  return {
    centroid: { ...referenceFrame.centroid },
    bounds: { ...referenceFrame.bounds },
    diagonal: referenceFrame.diagonal,
    axisAngleRadians: referenceFrame.axisAngleRadians
  };
}

function createEmptySession(): StrokeSession {
  return {
    strokes: [],
    startedAt: Date.now()
  };
}

function select<T extends Element>(root: ParentNode, selector: string): T {
  const element = root.querySelector<T>(selector);

  if (!element) {
    throw new Error(`요소를 찾지 못했습니다: ${selector}`);
  }

  return element;
}

function loadUserInputProfile(): UserInputProfile {
  const fallback = createEmptyUserInputProfile();

  try {
    const raw = window.localStorage.getItem(PROFILE_STORAGE_KEY);

    if (!raw) {
      return fallback;
    }

    const parsed = JSON.parse(raw) as Partial<UserInputProfile>;
    return {
      ...fallback,
      ...parsed,
      version: "v1.5",
      sampleCount: typeof parsed.sampleCount === "number" ? parsed.sampleCount : 0,
      averageDurationMs: typeof parsed.averageDurationMs === "number" ? parsed.averageDurationMs : 0,
      averagePathLength: typeof parsed.averagePathLength === "number" ? parsed.averagePathLength : 0,
      averageQuality: {
        ...fallback.averageQuality,
        ...parsed.averageQuality
      },
      familyCounts: {
        ...fallback.familyCounts,
        ...parsed.familyCounts
      },
      updatedAt: typeof parsed.updatedAt === "number" ? parsed.updatedAt : fallback.updatedAt
    };
  } catch {
    return fallback;
  }
}

function loadTutorialProfileStore(): TutorialProfileStore {
  const fallback = createEmptyTutorialProfileStore();

  try {
    const raw = window.localStorage.getItem(TUTORIAL_PROFILE_STORAGE_KEY);

    if (!raw) {
      return fallback;
    }

    return hydrateTutorialProfileStore(JSON.parse(raw) as Partial<TutorialProfileStore>);
  } catch {
    return fallback;
  }
}

function saveUserInputProfile(profile: UserInputProfile): void {
  try {
    window.localStorage.setItem(PROFILE_STORAGE_KEY, JSON.stringify(profile));
  } catch {
    // Ignore storage failures and keep the in-memory profile alive.
  }
}

function saveTutorialProfileStore(store: TutorialProfileStore): void {
  try {
    window.localStorage.setItem(TUTORIAL_PROFILE_STORAGE_KEY, JSON.stringify(store));
  } catch {
    // Ignore storage failures and keep the in-memory tutorial profile alive.
  }
}

function createTutorialOnboardingHook(options: {
  getBaseStrokes(): Stroke[];
  getOverlayStrokes(): Stroke[];
  getBaseSession(): StrokeSession;
  getOverlayRecords(): OverlayStrokeRecord[];
  canCaptureOperator(): boolean;
  getStore(): TutorialProfileStore;
  setStore(store: TutorialProfileStore): void;
}): TutorialOnboardingHook {
  const recordCapture = (request: TutorialCaptureRequest): TutorialCapture | null => {
    const strokes =
      request.strokes && request.strokes.length > 0
        ? request.strokes
        : request.kind === "family"
          ? options.getBaseStrokes()
          : options.getOverlayStrokes();

    if (strokes.length === 0) {
      return null;
    }

    let baseSnapshot: TutorialBaseSnapshot | undefined;
    let operatorContext: TutorialOperatorContext | undefined;

    if (request.kind === "operator") {
      if (!options.canCaptureOperator()) {
        return null;
      }

      const baseSession = options.getBaseSession();

      if (baseSession.strokes.length === 0) {
        return null;
      }

      const referenceFrame = createOverlayReferenceFrame(baseSession);
      const existingOperators = options
        .getOverlayRecords()
        .map((record) => record.recognition.operator)
        .filter((operator): operator is OverlayOperator => Boolean(operator));
      const stroke = strokes[strokes.length - 1];

      baseSnapshot = createTutorialBaseSnapshot(referenceFrame);
      operatorContext = createTutorialOperatorContext(stroke, {
        referenceFrame,
        existingOperators
      });
    }

    const nextStore = appendTutorialCapture(options.getStore(), {
      ...request,
      strokes,
      baseSnapshot,
      operatorContext,
      validation: request.validation
    });

    options.setStore(nextStore);
    return structuredClone(nextStore.captures[nextStore.captures.length - 1] ?? null);
  };

  return {
    getStore: () => structuredClone(options.getStore()),
    getProfile: () => structuredClone(options.getStore().shapeProfile),
    listCaptures: () => structuredClone(options.getStore().captures),
    recordCapture,
    captureBaseFamily: (expectedFamily, source) => recordCapture({ kind: "family", expectedFamily, source }),
    captureOverlayOperator: (expectedOperator, source) =>
      recordCapture({ kind: "operator", expectedOperator, source }),
    clear: () => {
      options.setStore(createEmptyTutorialProfileStore());
    }
  };
}

function exposeTutorialOnboardingHook(root: HTMLDivElement, hook: TutorialOnboardingHook): void {
  const host = root as TutorialHookHost;
  const tutorialWindow = window as TutorialHookWindow;

  host.__magicTutorialOnboardingHook__ = hook;
  tutorialWindow.__magicTutorialOnboardingHook__ = hook;
  root.dataset.tutorialOnboardingReady = "true";
  root.dispatchEvent(new CustomEvent("magic:tutorial-hook-ready", { detail: hook }));
}

function syncTutorialHookMetadata(root: HTMLDivElement, store: TutorialProfileStore): void {
  root.dataset.tutorialSampleCount = String(store.shapeProfile.tutorialSampleCount);
  root.dataset.tutorialUpdatedAt = String(store.updatedAt);
}

function syncTinyMlRuntimeMetadata(
  root: HTMLDivElement,
  baseResult?: RecognitionResult,
  overlayRecognition?: OverlayRecognition | null
): void {
  const metadata = buildTinyMlRuntimeMetadata(baseResult, overlayRecognition);

  for (const [key, value] of Object.entries(metadata)) {
    root.dataset[key] = value;
  }
}

export interface TutorialPersonalizationPolicyMetadata {
  canApply: boolean;
  canUseForShadow: boolean;
  needsBackfill: boolean;
  statusLabel: string;
  reasonLabels: string[];
}

export function buildTutorialPersonalizationPolicyMetadata(
  store: TutorialProfileStore
): TutorialPersonalizationPolicyMetadata {
  const signature = getBuiltInMagicCardSetSignature();
  const runtime = getTinyMlRuntimeStatus();
  const decision = evaluateTutorialThresholdBiasPolicy({
    storeVersion: store.version,
    cardSetId: store.cardSetId ?? store.shapeProfile.cardSetId,
    cardSetHash: store.cardSetHash ?? store.shapeProfile.cardSetHash,
    cardSignature: store.cardSignature ?? store.shapeProfile.cardSignature,
    currentCardSignature: {
      cardSetId: signature.cardSetId,
      cardSetHash: signature.cardSetHash,
      cardSignature: signature.cardSetHash
    },
    totalCaptureCount: store.shapeProfile.tutorialSampleCount,
    validatedCaptureCount: store.shapeProfile.validatedTutorialSampleCount ?? 0,
    feedbackOnlyCaptureCount: store.shapeProfile.feedbackOnlyTutorialSampleCount ?? 0,
    artifactCompatibility:
      runtime.baseShadowAvailable || runtime.operatorShadowAvailable
        ? "compatible"
        : "missing"
  });
  const reasonLabels = decision.reasons
    .filter((reason) => reason.severity !== "ok" || reason.code === "card_set_match")
    .map((reason) => reason.label)
    .slice(0, 4);

  return {
    canApply: decision.canApplyThresholdBias,
    canUseForShadow: decision.canUseForShadow,
    needsBackfill: decision.needsBackfill,
    statusLabel: decision.canApplyThresholdBias
      ? "실제 보정 가능"
      : decision.canUseForShadow
        ? "참고 표시만"
        : "보정 대기",
    reasonLabels: reasonLabels.length > 0 ? reasonLabels : ["연습 안전 상태 확인 전"]
  };
}

export function buildTinyMlRuntimeMetadata(
  baseResult?: RecognitionResult,
  overlayRecognition?: OverlayRecognition | null
): Record<string, string> {
  const runtime = getTinyMlRuntimeStatus();
  const operatorLabelValue = overlayRecognition?.operator ?? overlayRecognition?.topCandidate?.operator ?? "none";

  return {
    tinyMlShadowMode: runtime.shadowMode ? "shadow" : "off",
    tinyMlBaseArtifacts: runtime.baseShadowAvailable ? "ready" : "missing",
    tinyMlOperatorArtifacts: runtime.operatorShadowAvailable ? "ready" : "missing",
    tinyMlFeatureSpec: runtime.featureSpecAvailable ? "ready" : "missing",
    baseActualFamily: baseResult ? readCurrentFamily(baseResult) ?? "none" : "none",
    baseActualStatus: baseResult?.status ?? "waiting",
    basePersonalizationStage: baseResult?.personalization?.stage ?? "none",
    basePersonalizationMix: baseResult?.shadow?.personalizationMix?.toFixed(3) ?? baseResult?.personalization?.featureInjectionMix.toFixed(3) ?? "0.000",
    baseThresholdBias: baseResult?.personalization?.thresholdBias.toFixed(3) ?? "0.000",
    baseEffectiveThresholdBias: baseResult?.personalization?.effectiveThresholdBias?.toFixed(3) ?? "0.000",
    baseMlConfidenceGate: baseResult?.personalization?.mlConfidenceGate?.toFixed(3) ?? "1.000",
    baseMlActualGate: baseResult?.personalization?.mlActualGate ?? "none",
    baseShadowDecisionChanged: baseResult?.shadow ? String(baseResult.shadow.decisionChanged) : "false",
    baseShadowStatusChanged: baseResult?.shadow ? String(baseResult.shadow.statusChanged) : "false",
    basePersonalizedShadowTopLabel: baseResult?.shadow?.personalizedShadowTopLabel ?? "none",
    basePersonalizedShadowDecisionChanged: baseResult?.shadow
      ? String(baseResult.shadow.personalizedDecisionChanged ?? false)
      : "false",
    basePersonalizedShadowStatusChanged: baseResult?.shadow
      ? String(baseResult.shadow.personalizedStatusChanged ?? false)
      : "false",
    operatorActualLabel: operatorLabelValue,
    operatorActualStatus: overlayRecognition?.status ?? "waiting",
    operatorPersonalizationStage: overlayRecognition?.personalization?.stage ?? "none",
    operatorPersonalizationMix:
      overlayRecognition?.shadow?.personalizationMix?.toFixed(3) ??
      overlayRecognition?.personalization?.featureInjectionMix.toFixed(3) ??
      "0.000",
    operatorThresholdBias: overlayRecognition?.personalization?.thresholdBias.toFixed(3) ?? "0.000",
    operatorEffectiveThresholdBias: overlayRecognition?.personalization?.effectiveThresholdBias?.toFixed(3) ?? "0.000",
    operatorMlConfidenceGate: overlayRecognition?.personalization?.mlConfidenceGate?.toFixed(3) ?? "1.000",
    operatorMlActualGate: overlayRecognition?.personalization?.mlActualGate ?? "none",
    operatorShadowDecisionChanged: overlayRecognition?.shadow
      ? String(overlayRecognition.shadow.decisionChanged)
      : "false",
    operatorShadowStatusChanged: overlayRecognition?.shadow
      ? String(overlayRecognition.shadow.statusChanged)
      : "false",
    operatorPersonalizedShadowTopLabel: overlayRecognition?.shadow?.personalizedShadowTopLabel ?? "none",
    operatorPersonalizedShadowDecisionChanged: overlayRecognition?.shadow
      ? String(overlayRecognition.shadow.personalizedDecisionChanged ?? false)
      : "false",
    operatorPersonalizedShadowStatusChanged: overlayRecognition?.shadow
      ? String(overlayRecognition.shadow.personalizedStatusChanged ?? false)
      : "false"
  };
}

function pointFromEvent(canvas: HTMLCanvasElement, event: PointerEvent): { x: number; y: number } {
  const rect = canvas.getBoundingClientRect();
  const scaleX = canvas.width / rect.width;
  const scaleY = canvas.height / rect.height;

  return {
    x: (event.clientX - rect.left) * scaleX,
    y: (event.clientY - rect.top) * scaleY
  };
}

function distance(a: { x: number; y: number }, b: { x: number; y: number }): number {
  return Math.hypot(a.x - b.x, a.y - b.y);
}

function familyLabel(family: string): string {
  switch (family) {
    case "wind":
      return "바람";
    case "earth":
      return "땅";
    case "fire":
      return "불꽃";
    case "water":
      return "물";
    case "life":
      return "생명";
    default:
      return family;
  }
}

function operatorLabel(operator: string): string {
  switch (operator) {
    case "steel_brace":
      return "버팀 장식";
    case "electric_fork":
      return "갈래 번개";
    case "ice_bar":
      return "얼음 막대";
    case "soul_dot":
      return "혼 점";
    case "void_cut":
      return "공백 절단";
    case "martial_axis":
      return "축선 장식";
    default:
      return operator;
  }
}

function phaseTitleFor(phase: RitualPhase, baseSealed: boolean, overlayAuthoringStarted: boolean): string {
  if (phase === "base" && baseSealed) {
    return "기본 모양 고정";
  }

  if (phase === "overlay" && overlayAuthoringStarted) {
    return "추가 효과 그리는 중";
  }

  switch (phase) {
    case "base":
      return "기본 모양";
    case "overlay":
      return "추가 효과 준비";
    case "final":
      return "최종 결과";
    default:
      return phase;
  }
}

function phaseCopyFor(
  phase: RitualPhase,
  baseSealed: boolean,
  overlayAuthoringStarted: boolean,
  compiledResult: CompiledSealResult | null
): string {
  if (!baseSealed) {
    return "기본 모양을 그리고 먼저 종류를 고정합니다.";
  }

  if (phase === "base") {
    return "종류가 고정됐습니다. 같은 캔버스에서 추가 효과를 더해 보세요.";
  }

  if (phase === "overlay" && overlayAuthoringStarted) {
    return "원본 선은 그대로 두고, 읽힌 추가 효과만 따로 미리보기와 기록으로 확인합니다.";
  }

  if (compiledResult) {
    return "최종 결과가 확정됐습니다. 결과를 비교하거나 처음부터 다시 시작할 수 있습니다.";
  }

  return "추가 효과를 점검한 뒤 최종 결과 보기를 눌러 결과를 확정합니다.";
}

function phaseChipClass(active: boolean, ready: boolean): string {
  return ["phase-chip", active ? "active" : "", ready ? "ready" : ""].filter(Boolean).join(" ");
}

function drawCanvas(context: CanvasRenderingContext2D, state: CanvasRenderState): void {
  context.clearRect(0, 0, CANVAS_WIDTH, CANVAS_HEIGHT);
  context.fillStyle = "#f2efe6";
  context.fillRect(0, 0, CANVAS_WIDTH, CANVAS_HEIGHT);
  drawGrid(context);
  const baseSealed = Boolean(state.baseSealResult?.canonicalFamily);
  const referenceFrame = baseSealed ? createOverlayReferenceFrame(state.baseSession) : null;

  if (state.phase === "base") {
    drawStrokes(context, state.baseSession.strokes, "#203337", 4, 1);
  } else {
    drawStrokes(context, state.baseSession.strokes, "#6f817f", 3, 0.22);
    drawStrokes(context, state.overlaySession.strokes, "#17313b", 4, 1);
  }

  const baseDebug = state.baseSealResult ?? state.previewResult;

  if (state.analysisOverlay) {
    drawAxis(context, baseDebug.symmetryAxis, "#2b6777");
    drawAxis(context, baseDebug.closureLine, "#b14729");
    if (referenceFrame) {
      drawReferenceFrameDebug(
        context,
        referenceFrame,
        state.currentOverlayPreview?.anchorZoneId,
        phaseHudLabel(state.phase, baseSealed, state.overlayAuthoringStarted)
      );
    }

    if (referenceFrame && (state.phase !== "base" || baseSealed)) {
      for (const record of state.overlayRecords) {
        drawOverlayDebug(context, record.recognition, referenceFrame);
      }

      if (state.currentOverlayPreview?.strokeId) {
        drawOverlayDebug(context, state.currentOverlayPreview, referenceFrame, true);
      }
    }
  }

  if (state.whatIfPreview) {
    drawWhatIfPreview(context, state.whatIfPreview, referenceFrame);
  }

  const overlayCount = state.overlayRecords.filter(
    (record) => record.recognition.status === "recognized" && record.recognition.operator
  ).length;
  const hudStatus = state.phase === "base" ? state.previewResult.status : state.baseSealResult?.status ?? "invalid";
  const hudFamily = baseDebug.canonicalFamily
    ? familyLabel(baseDebug.canonicalFamily)
    : baseDebug.topCandidate
      ? familyLabel(baseDebug.topCandidate.family)
      : "none";
  const phaseHud = phaseHudLabel(state.phase, baseSealed, state.overlayAuthoringStarted);

  context.fillStyle = "rgba(18, 30, 34, 0.86)";
  context.fillRect(20, 18, 332, 110);
  context.fillStyle = "#f4efe3";
  context.font = "600 14px Manrope, 'Segoe UI', sans-serif";
  context.fillText(`단계: ${phaseHud}`, 32, 42);
  context.fillText(`기본 모양: ${hudFamily}`, 32, 61);
  context.font = "12px 'IBM Plex Mono', 'SFMono-Regular', monospace";
  context.fillText(`상태: ${statusLabel(hudStatus)}`, 32, 81);
  context.fillText(`추가 효과: ${overlayCount}`, 32, 100);
  context.fillText(
    baseSealed
      ? state.phase === "base"
        ? "다음: 추가 효과 그리기"
        : state.phase === "overlay"
          ? "다음: 최종 결과 보기"
          : "결과 고정"
      : "다음: 기본 모양 고정",
    32,
    119
  );
}

function drawStrokes(
  context: CanvasRenderingContext2D,
  strokes: Stroke[],
  color: string,
  width: number,
  alpha: number
): void {
  context.save();
  context.globalAlpha = alpha;
  context.lineWidth = width;
  context.lineCap = "round";
  context.lineJoin = "round";
  context.strokeStyle = color;

  for (const stroke of strokes) {
    if (stroke.points.length === 0) {
      continue;
    }

    context.beginPath();
    context.moveTo(stroke.points[0].x, stroke.points[0].y);

    for (const point of stroke.points.slice(1)) {
      context.lineTo(point.x, point.y);
    }

    context.stroke();
  }

  context.restore();
}

function drawGrid(context: CanvasRenderingContext2D): void {
  context.save();
  context.strokeStyle = "rgba(19, 42, 51, 0.08)";
  context.lineWidth = 1;

  for (let x = 0; x <= CANVAS_WIDTH; x += 60) {
    context.beginPath();
    context.moveTo(x, 0);
    context.lineTo(x, CANVAS_HEIGHT);
    context.stroke();
  }

  for (let y = 0; y <= CANVAS_HEIGHT; y += 60) {
    context.beginPath();
    context.moveTo(0, y);
    context.lineTo(CANVAS_WIDTH, y);
    context.stroke();
  }

  context.restore();
}

function drawAxis(context: CanvasRenderingContext2D, axis: AxisLine | undefined, color: string): void {
  if (!axis) {
    return;
  }

  context.save();
  context.strokeStyle = color;
  context.lineWidth = 2;
  context.setLineDash([10, 8]);
  context.beginPath();
  context.moveTo(axis.start.x, axis.start.y);
  context.lineTo(axis.end.x, axis.end.y);
  context.stroke();
  context.restore();
}

function drawOverlayDebug(
  context: CanvasRenderingContext2D,
  recognition: OverlayRecognition,
  referenceFrame: OverlayReferenceFrame,
  preview = false
): void {
  const color =
    recognition.status === "recognized"
      ? "#3d7d65"
      : recognition.status === "ambiguous"
        ? "#c49127"
        : "#b14729";
  const { bounds } = recognition;
  const label = recognition.operator ?? recognition.topCandidate?.operator ?? "overlay";

  context.save();
  context.strokeStyle = color;
  context.lineWidth = 2;
  context.setLineDash(preview ? [6, 6] : [10, 8]);
  context.strokeRect(bounds.minX - 8, bounds.minY - 8, bounds.width + 16, bounds.height + 16);

  if (recognition.debugAxis) {
    context.beginPath();
    context.moveTo(recognition.debugAxis.start.x, recognition.debugAxis.start.y);
    context.lineTo(recognition.debugAxis.end.x, recognition.debugAxis.end.y);
    context.stroke();
  }

  drawOperatorGhost(context, recognition, preview ? "rgba(191, 115, 75, 0.42)" : "rgba(43, 103, 119, 0.24)");
  if (recognition.anchorZoneId) {
    const anchorZone = referenceFrame.anchorZones.find((zone) => zone.id === recognition.anchorZoneId);
    if (anchorZone) {
      context.setLineDash([4, 6]);
      context.strokeStyle = color;
      context.beginPath();
      context.arc(anchorZone.center.x, anchorZone.center.y, anchorZone.radius, 0, Math.PI * 2);
      context.stroke();
    }
  }

  context.setLineDash([]);
  context.fillStyle = color;
  context.font = "11px 'IBM Plex Mono', 'SFMono-Regular', monospace";
  context.fillText(operatorLabel(label), bounds.minX, Math.max(bounds.minY - 12, 16));
  context.restore();
}

function drawReferenceFrameDebug(
  context: CanvasRenderingContext2D,
  referenceFrame: OverlayReferenceFrame,
  activeZoneId: OverlayAnchorZoneId | undefined,
  phaseLabel: string
): void {
  context.save();
  context.strokeStyle = "rgba(43, 103, 119, 0.22)";
  context.lineWidth = 1.5;
  context.setLineDash([8, 8]);
  drawAxis(context, referenceFrame.referenceLines.horizontal, "rgba(43, 103, 119, 0.18)");
  drawAxis(context, referenceFrame.referenceLines.vertical, "rgba(43, 103, 119, 0.18)");
  drawAxis(context, referenceFrame.referenceLines.ascendingDiagonal, "rgba(177, 71, 41, 0.15)");
  context.setLineDash([]);

  for (const zone of referenceFrame.anchorZones) {
    const active = zone.id === activeZoneId;
    context.beginPath();
    context.strokeStyle = active ? "rgba(191, 115, 75, 0.6)" : "rgba(23, 49, 59, 0.14)";
    context.fillStyle = active ? "rgba(191, 115, 75, 0.08)" : "rgba(23, 49, 59, 0.03)";
    context.arc(zone.center.x, zone.center.y, zone.radius, 0, Math.PI * 2);
    context.fill();
    context.stroke();
    context.fillStyle = active ? "#8f4d2d" : "rgba(23, 49, 59, 0.5)";
    context.font = "10px 'IBM Plex Mono', 'SFMono-Regular', monospace";
    context.fillText(zone.id.replace("_", "."), zone.center.x - zone.radius * 0.58, zone.center.y + 3);
  }

  context.fillStyle = "rgba(23, 49, 59, 0.74)";
  context.font = "11px 'IBM Plex Mono', 'SFMono-Regular', monospace";
  context.fillText(`analysis ${phaseLabel}`, referenceFrame.bounds.minX, Math.max(referenceFrame.bounds.minY - 20, 18));
  context.restore();
}

function drawWhatIfPreview(
  context: CanvasRenderingContext2D,
  preview: MagicWhatIfPreviewModel,
  referenceFrame: OverlayReferenceFrame | null
): void {
  context.save();
  context.setLineDash([8, 6]);

  for (const mark of preview.marks) {
    drawWhatIfMark(context, mark, referenceFrame);
  }

  context.restore();
}

function drawWhatIfMark(
  context: CanvasRenderingContext2D,
  mark: MagicWhatIfPreviewMark,
  referenceFrame: OverlayReferenceFrame | null
): void {
  switch (mark.kind) {
    case "ghost_stroke":
      if (mark.points && mark.points.length > 0) {
        drawWhatIfGhostStroke(context, mark.points, whatIfToneColor(mark.tone, 0.78));
      }
      return;
    case "anchor_zone":
      drawWhatIfAnchorZone(context, mark, referenceFrame);
      return;
    case "dependency_arrow":
      drawWhatIfArrow(context, mark);
      return;
    case "risk_label":
      if (mark.label) {
        drawWhatIfLabel(context, mark.label, mark.from ?? { x: 34, y: CANVAS_HEIGHT - 34 }, mark.tone);
      }
      return;
  }
}

function drawWhatIfGhostStroke(
  context: CanvasRenderingContext2D,
  points: readonly { x: number; y: number }[],
  strokeStyle: string
): void {
  if (points.length === 0) {
    return;
  }

  context.save();
  context.strokeStyle = strokeStyle;
  context.lineWidth = 3;
  context.lineCap = "round";
  context.lineJoin = "round";
  context.setLineDash([9, 7]);
  context.beginPath();
  context.moveTo(points[0].x, points[0].y);

  for (const point of points.slice(1)) {
    context.lineTo(point.x, point.y);
  }

  context.stroke();
  context.restore();
}

function drawWhatIfAnchorZone(
  context: CanvasRenderingContext2D,
  mark: MagicWhatIfPreviewMark,
  referenceFrame: OverlayReferenceFrame | null
): void {
  const zone = mark.anchorZoneId ? referenceFrame?.anchorZones.find((item) => item.id === mark.anchorZoneId) : undefined;
  const center = zone?.center ?? mark.from;

  if (!center) {
    return;
  }

  const radius = zone?.radius ?? 26;
  context.save();
  context.strokeStyle = whatIfToneColor(mark.tone, 0.74);
  context.fillStyle = whatIfToneColor(mark.tone, 0.1);
  context.lineWidth = 2;
  context.setLineDash([7, 5]);
  context.beginPath();
  context.arc(center.x, center.y, radius, 0, Math.PI * 2);
  context.fill();
  context.stroke();
  if (mark.label) {
    drawWhatIfLabel(context, mark.label, { x: center.x + radius + 8, y: center.y - 8 }, mark.tone);
  }
  context.restore();
}

function drawWhatIfArrow(context: CanvasRenderingContext2D, mark: MagicWhatIfPreviewMark): void {
  if (!mark.from || !mark.to) {
    return;
  }

  const color = whatIfToneColor(mark.tone, 0.82);
  const angle = Math.atan2(mark.to.y - mark.from.y, mark.to.x - mark.from.x);
  const head = 12;

  context.save();
  context.strokeStyle = color;
  context.fillStyle = color;
  context.lineWidth = 2;
  context.setLineDash([8, 6]);
  context.beginPath();
  context.moveTo(mark.from.x, mark.from.y);
  context.lineTo(mark.to.x, mark.to.y);
  context.stroke();
  context.setLineDash([]);
  context.beginPath();
  context.moveTo(mark.to.x, mark.to.y);
  context.lineTo(mark.to.x - head * Math.cos(angle - Math.PI / 6), mark.to.y - head * Math.sin(angle - Math.PI / 6));
  context.lineTo(mark.to.x - head * Math.cos(angle + Math.PI / 6), mark.to.y - head * Math.sin(angle + Math.PI / 6));
  context.closePath();
  context.fill();
  if (mark.label) {
    drawWhatIfLabel(
      context,
      mark.label,
      { x: (mark.from.x + mark.to.x) / 2 + 10, y: (mark.from.y + mark.to.y) / 2 - 10 },
      mark.tone
    );
  }
  context.restore();
}

function drawWhatIfLabel(
  context: CanvasRenderingContext2D,
  label: string,
  at: { x: number; y: number },
  tone: MagicWhatIfPreviewMark["tone"]
): void {
  context.save();
  context.font = "600 12px Manrope, 'Segoe UI', sans-serif";
  const width = Math.min(Math.max(context.measureText(label).width + 18, 96), 280);
  const x = Math.min(Math.max(at.x, 14), CANVAS_WIDTH - width - 14);
  const y = Math.min(Math.max(at.y, 22), CANVAS_HEIGHT - 18);
  context.fillStyle = whatIfToneColor(tone, 0.88);
  context.strokeStyle = "rgba(255, 255, 255, 0.76)";
  context.lineWidth = 1;
  context.beginPath();
  context.roundRect(x, y - 18, width, 24, 8);
  context.fill();
  context.stroke();
  context.fillStyle = "#fffaf0";
  context.fillText(label.length > 32 ? `${label.slice(0, 31)}…` : label, x + 9, y - 2);
  context.restore();
}

function whatIfToneColor(tone: MagicWhatIfPreviewMark["tone"], alpha: number): string {
  switch (tone) {
    case "risk":
      return `rgba(177, 71, 41, ${alpha})`;
    case "warning":
      return `rgba(200, 137, 42, ${alpha})`;
    default:
      return `rgba(43, 103, 119, ${alpha})`;
  }
}

function drawOperatorGhost(context: CanvasRenderingContext2D, recognition: OverlayRecognition, strokeStyle: string): void {
  const operator = recognition.operator ?? recognition.topCandidate?.operator;
  const template = OVERLAY_OPERATOR_TEMPLATES.find((item) => item.operator === operator);

  if (!template) {
    return;
  }

  const templatePoints = template.strokes.flatMap((stroke) => stroke.points);
  const templateBounds = boundsFromPoints(templatePoints);
  const targetBounds = {
    minX: recognition.bounds.minX,
    maxX: recognition.bounds.maxX,
    minY: recognition.bounds.minY,
    maxY: recognition.bounds.maxY,
    width: Math.max(recognition.bounds.width, 12),
    height: Math.max(recognition.bounds.height, 12)
  };

  context.save();
  context.strokeStyle = strokeStyle;
  context.lineWidth = 1.5;
  context.setLineDash([5, 5]);

  for (const stroke of template.strokes) {
    if (stroke.points.length === 0) {
      continue;
    }

    context.beginPath();
    const first = mapGhostPoint(stroke.points[0], templateBounds, targetBounds);
    context.moveTo(first.x, first.y);
    for (const point of stroke.points.slice(1)) {
      const mapped = mapGhostPoint(point, templateBounds, targetBounds);
      context.lineTo(mapped.x, mapped.y);
    }
    context.stroke();
  }

  context.restore();
}

function mapGhostPoint(
  point: { x: number; y: number },
  templateBounds: ReturnType<typeof boundsFromPoints>,
  targetBounds: { minX: number; minY: number; width: number; height: number }
): { x: number; y: number } {
  return {
    x: targetBounds.minX + ((point.x - templateBounds.minX) / templateBounds.width) * targetBounds.width,
    y: targetBounds.minY + ((point.y - templateBounds.minY) / templateBounds.height) * targetBounds.height
  };
}

function boundsFromPoints(points: Array<{ x: number; y: number }>): {
  minX: number;
  minY: number;
  width: number;
  height: number;
} {
  const xs = points.map((point) => point.x);
  const ys = points.map((point) => point.y);
  const minX = Math.min(...xs);
  const maxX = Math.max(...xs);
  const minY = Math.min(...ys);
  const maxY = Math.max(...ys);

  return {
    minX,
    minY,
    width: Math.max(maxX - minX, 0.0001),
    height: Math.max(maxY - minY, 0.0001)
  };
}

interface PersonalizationDemoModel {
  stripCopy: string;
  stripBadges: string;
  cardTitle: string;
  cardStatusLabel: string;
  cardStatusTone: "recognized" | "ready" | "waiting";
  cardCopy: string;
  cardBadges: string;
  metricRows: string;
  compareTitle: string;
  compareCopy: string;
  compareLanes: string;
  effectRows: string;
  profileTitle: string;
  profileCopy: string;
  profileSummary: string;
  profileDetails: string;
  exemplarTitle: string;
  exemplarCopy: string;
  exemplarGrid: string;
}

interface TutorialFlowModel {
  title: string;
  statusLabel: string;
  statusTone: "recognized" | "ready" | "waiting";
  copy: string;
  progressBadges: string;
  stepCard: string;
  stepList: string;
  startDisabled: boolean;
  captureDisabled: boolean;
  resultDisabled: boolean;
  clearDisabled: boolean;
  compareTitle: string;
  compareStatusLabel: string;
  compareStatusTone: "recognized" | "ready" | "waiting";
  compareCopy: string;
  compareGrid: string;
  compareEffects: string;
  compareMetrics: string;
}

function getTutorialStepState(
  step: TutorialDemoStep,
  baseSealResult: RecognitionResult | null,
  overlayRecords: OverlayStrokeRecord[],
  baseSession: StrokeSession,
  overlaySession: StrokeSession
): { ready: boolean; tone: "recognized" | "ready" | "waiting"; label: string; copy: string } {
  if (step.kind === "family") {
    const ready = baseSession.strokes.length > 0;
    return {
      ready,
      tone: ready ? "ready" : "waiting",
      label: ready ? "현재 입력 저장 가능" : "기본 모양 필요",
      copy: ready
        ? "캔버스에 그린 모양을 바로 연습에 저장할 수 있습니다."
        : "캔버스에 기본 모양을 한 번 그려 주세요."
    };
  }

  if (step.requiresSealedBase && !baseSealResult?.canonicalFamily) {
    return {
      ready: false,
      tone: "waiting",
      label: "기본 모양 고정 필요",
      copy: "캔버스에 기본 모양을 그리고 '기본 모양 고정'을 먼저 눌러 주세요."
    };
  }

  if (step.requiresExistingOperator) {
    const existingOperators = new Set(
      overlayRecords
        .map((record) => record.recognition.operator)
        .filter((operator): operator is OverlayOperator => Boolean(operator))
    );

    if (!existingOperators.has(step.requiresExistingOperator)) {
      return {
        ready: false,
        tone: "waiting",
        label: `${operatorLabel(step.requiresExistingOperator)} 필요`,
        copy: `${operatorLabel(step.requiresExistingOperator)}를 먼저 기록한 뒤 이 단계를 저장해 주세요.`
      };
    }
  }

  const hasOperatorStroke = overlayRecords.length > 0 || overlaySession.strokes.length > 0;
  return {
    ready: hasOperatorStroke,
    tone: hasOperatorStroke ? "ready" : "waiting",
    label: hasOperatorStroke ? "현재 입력 저장 가능" : "추가 효과 선 필요",
    copy: hasOperatorStroke
      ? "방금 그린 추가 효과를 연습에 저장할 수 있습니다."
      : "추가 효과 선을 한 번 그려 주세요."
  };
}

function buildTutorialFlowModel(args: {
  tutorialSteps: readonly TutorialDemoStep[];
  stepIndex: number;
  completedStepIds: string[];
  tutorialFlowActive: boolean;
  beforeSnapshot: TutorialComparisonSnapshot | null;
  afterSnapshot: TutorialComparisonSnapshot | null;
  baseSession: StrokeSession;
  overlaySession: StrokeSession;
  baseSealResult: RecognitionResult | null;
  overlayRecords: OverlayStrokeRecord[];
  tutorialStore: TutorialProfileStore;
}): TutorialFlowModel {
  const tutorialSteps = args.tutorialSteps.length > 0 ? args.tutorialSteps : TUTORIAL_DEMO_STEPS;
  const currentStep = tutorialSteps[args.stepIndex] ?? tutorialSteps[0];
  const stepState = getTutorialStepState(
    currentStep,
    args.baseSealResult,
    args.overlayRecords,
    args.baseSession,
    args.overlaySession
  );
  const completedCount = args.completedStepIds.length;
  const tutorialSamples = args.tutorialStore.shapeProfile.tutorialSampleCount;
  const stage = strongestPersonalizationStage(
    args.afterSnapshot?.baseResult.personalization?.stage,
    args.afterSnapshot?.overlayRecognition?.personalization?.stage
  );
  const targetId = currentStep.expectedFamily ?? currentStep.expectedOperator ?? null;
  const stepBadges = [
    renderPromiseBadge(`${completedCount}/${tutorialSteps.length} 단계`, completedCount > 0),
    renderPromiseBadge(stepState.label, stepState.ready),
    renderPromiseBadge(
      args.beforeSnapshot ? "연습 전 입력 고정됨" : "연습 전 입력 대기",
      Boolean(args.beforeSnapshot)
    ),
    renderPromiseBadge(`연습 입력 ${tutorialSamples}회`, tutorialSamples > 0)
  ].join("");
  const stepCard = `
    <article class="tutorial-step-focus ${stepState.ready ? "ready" : ""}">
      <div class="tutorial-step-copy">
        <p class="mini-label">이번에 그릴 것</p>
        <h4>${currentStep.title}</h4>
        <p>${currentStep.instruction}</p>
        <section class="tutorial-shape-guide">
          <p class="mini-label">그리는 방법</p>
          <strong>${currentStep.shapeSummary}</strong>
          <div class="tutorial-shape-pills">
            ${currentStep.shapeChecklist
              .map((item) => `<span class="tutorial-shape-pill">${item}</span>`)
              .join("")}
          </div>
        </section>
        <div class="promise-inline">${stepBadges}</div>
        <p class="tutorial-step-note">${stepState.copy}</p>
      </div>
      ${
        targetId
          ? `<div class="tutorial-step-exemplar">${renderExemplarChip(targetId, {
              active: true,
              hint: currentStep.kind === "family" ? "같은 모양을 유지한 채 한 번 더 또렷하게 그려 보세요." : "같은 위치와 길이 감각으로 한 획만 다시 붙여 보세요."
            })}</div>`
          : ""
      }
    </article>
  `;
  const stepList = tutorialSteps.map((step, index) => {
    const completed = args.completedStepIds.includes(step.id);
    const selected = step.id === currentStep.id;
    const locked =
      Boolean(step.requiresSealedBase && !args.baseSealResult?.canonicalFamily) ||
      Boolean(
        step.requiresExistingOperator &&
          !args.overlayRecords.some((record) => record.recognition.operator === step.requiresExistingOperator)
      );

    return `
      <button
        class="tutorial-step-chip ${selected ? "active" : ""} ${completed ? "completed" : ""} ${locked ? "locked" : ""}"
        type="button"
        data-tutorial-step-index="${index}"
      >
        <span>${index + 1}</span>
        <strong>${step.shortLabel}</strong>
      </button>
    `;
  }).join("");

  const compare = buildTutorialComparisonDisplay(args.beforeSnapshot, args.afterSnapshot);

  return {
    title: args.tutorialFlowActive ? "따라 그리기 진행 중" : "따라 그리기 시작",
    statusLabel: args.tutorialFlowActive ? "진행 중" : args.beforeSnapshot ? "준비됨" : "대기",
    statusTone: args.tutorialFlowActive ? "ready" : args.beforeSnapshot ? "recognized" : "waiting",
    copy: args.beforeSnapshot
      ? "지금 그린 입력을 기준으로 연습 전/후 변화를 확인합니다."
      : "먼저 캔버스에 모양을 그리고 연습을 시작하면 같은 입력을 다시 비교할 수 있습니다.",
    progressBadges: stepBadges,
    stepCard,
    stepList,
    startDisabled: args.baseSession.strokes.length === 0,
    captureDisabled: !stepState.ready,
    resultDisabled: !args.beforeSnapshot,
    clearDisabled:
      tutorialSamples === 0 &&
      !args.beforeSnapshot &&
      !args.afterSnapshot &&
      args.completedStepIds.length === 0,
    compareTitle: compare.title,
    compareStatusLabel: compare.statusLabel,
    compareStatusTone: compare.statusTone,
    compareCopy: compare.copy,
    compareGrid: compare.grid,
    compareEffects: compare.effects,
    compareMetrics: compare.metrics
  };
}

function buildTutorialComparisonDisplay(
  beforeSnapshot: TutorialComparisonSnapshot | null,
  afterSnapshot: TutorialComparisonSnapshot | null
): {
  title: string;
  statusLabel: string;
  statusTone: "recognized" | "ready" | "waiting";
  copy: string;
  grid: string;
  effects: string;
  metrics: string;
} {
  if (!beforeSnapshot) {
    return {
      title: "같은 입력 다시 보기",
      statusLabel: "대기",
      statusTone: "waiting",
      copy: "연습 시작을 누르면 지금 입력을 고정해 두고, 연습 후에 같은 입력을 다시 읽어 비교합니다.",
      grid: `<div class="empty-state">아직 연습 전 기준 입력이 없습니다.</div>`,
      effects: renderMetricNotes(["같은 입력을 먼저 고정하면 연습 후 차이를 같은 화면에서 읽을 수 있습니다."]),
      metrics: renderSummaryRows([["연습 입력 누적", "0회"], ["비교 상태", "전 기준 대기"], ["최종 판정", "규칙 기준 유지"]])
    };
  }

  if (!afterSnapshot) {
    return {
      title: "연습 결과 대기",
      statusLabel: "준비됨",
      statusTone: "ready",
      copy: "연습 결과 보기를 누르면 같은 입력을 다시 읽어 연습 전/후 차이를 비교합니다.",
      grid: `<div class="empty-state">연습 입력을 몇 번 저장한 뒤 결과 비교를 열어 보세요.</div>`,
      effects: renderMetricNotes([
        "연습 전 입력은 이미 고정됐습니다.",
        "연습 후에는 같은 입력을 다시 읽어 편향 보완 정도를 확인합니다."
      ]),
      metrics: renderSummaryRows([
        ["연습 전 입력", formatClockTime(beforeSnapshot.capturedAt)],
        ["연습 입력 누적", `${beforeSnapshot.tutorialSampleCount}회`],
        ["비교 상태", "연습 결과 대기"]
      ])
    };
  }

  const supportLane = buildShadowLane("보조 판독", "shadow", afterSnapshot.baseResult, afterSnapshot.overlayRecognition);
  const beforeLane = {
    label: "연습 전",
    title: "연습 전 현재 판정",
    statusLabel: statusLabel(beforeSnapshot.baseResult.status),
    statusTone: beforeSnapshot.baseResult.status,
    baseLabel: readCurrentFamily(beforeSnapshot.baseResult)
      ? familyLabel(readCurrentFamily(beforeSnapshot.baseResult) as GlyphFamily)
      : "아직 없음",
    overlayLabel: beforeSnapshot.overlayRecognition?.operator
      ? operatorLabel(beforeSnapshot.overlayRecognition.operator)
      : beforeSnapshot.overlayRecognition?.topCandidate?.operator
        ? operatorLabel(beforeSnapshot.overlayRecognition.topCandidate.operator)
        : "아직 없음",
    copy: "연습을 시작하기 전에 같은 입력을 읽은 결과입니다."
  };
  const afterLane = {
    label: "연습 후",
    title: "연습 후 입력 습관 반영",
    statusLabel: statusLabel(afterSnapshot.baseResult.status),
    statusTone: afterSnapshot.baseResult.status,
    baseLabel: readCurrentFamily(afterSnapshot.baseResult)
      ? familyLabel(readCurrentFamily(afterSnapshot.baseResult) as GlyphFamily)
      : "아직 없음",
    overlayLabel: afterSnapshot.overlayRecognition?.operator
      ? operatorLabel(afterSnapshot.overlayRecognition.operator)
      : afterSnapshot.overlayRecognition?.topCandidate?.operator
        ? operatorLabel(afterSnapshot.overlayRecognition.topCandidate.operator)
        : "아직 없음",
    copy: "같은 입력을 다시 읽어 본 연습 후 현재 판정입니다."
  };
  const compareGrid = [beforeLane, supportLane, afterLane]
    .map(
      (lane) => `
        <article class="personalization-lane ${lane.label === "연습 후" ? "active" : ""}">
          <div class="recent-seal-head">
            <div>
              <p class="mini-label">${lane.label}</p>
              <h4>${lane.title}</h4>
            </div>
            <span class="status-chip status-${lane.statusTone}">${lane.statusLabel}</span>
          </div>
          <div class="summary-grid">
            <div class="summary-row">
              <span>기본 모양</span>
              <strong>${lane.baseLabel}</strong>
            </div>
            <div class="summary-row">
              <span>추가 효과</span>
              <strong>${lane.overlayLabel}</strong>
            </div>
          </div>
          <p class="recent-seal-summary">${lane.copy}</p>
        </article>
      `
    )
    .join("");

  return {
    title: "같은 입력 전 / 후 비교 완료",
    statusLabel: "비교 가능",
    statusTone: "recognized",
    copy: "연습 전, 보조 판독 참고 계산, 연습 후 현재 판정을 같은 입력 기준으로 나란히 보여 줍니다.",
    grid: compareGrid,
    effects: renderMetricNotes(buildTutorialComparisonEffects(beforeSnapshot, afterSnapshot)),
    metrics: renderSummaryRows(buildTutorialComparisonMetrics(beforeSnapshot, afterSnapshot))
  };
}

function buildTutorialComparisonEffects(
  beforeSnapshot: TutorialComparisonSnapshot,
  afterSnapshot: TutorialComparisonSnapshot
): string[] {
  const effects: string[] = [];
  const beforeBaseLabel = readCurrentFamily(beforeSnapshot.baseResult);
  const afterBaseLabel = readCurrentFamily(afterSnapshot.baseResult);
  const beforeBaseGap = candidateGap(beforeSnapshot.baseResult.candidates);
  const afterBaseGap = candidateGap(afterSnapshot.baseResult.candidates);
  const beforeOverlayGap = candidateGap(beforeSnapshot.overlayRecognition?.candidates ?? []);
  const afterOverlayGap = candidateGap(afterSnapshot.overlayRecognition?.candidates ?? []);

  if (beforeBaseLabel === afterBaseLabel) {
    if (afterSnapshot.baseResult.status !== beforeSnapshot.baseResult.status) {
      effects.push(
        `기본 모양 상태가 ${statusLabel(beforeSnapshot.baseResult.status)}에서 ${statusLabel(afterSnapshot.baseResult.status)}로 바뀌었습니다.`
      );
    } else if (afterBaseGap > beforeBaseGap + 0.03) {
      effects.push("기본 모양 확정 여유가 커졌습니다.");
    } else {
      effects.push("기본 모양 후보는 전/후가 거의 같습니다.");
    }
  } else {
    effects.push("경합 구간에서 앞선 기본 모양 후보가 조금 달라졌습니다.");
  }

  if (!beforeSnapshot.overlayRecognition && afterSnapshot.overlayRecognition) {
    effects.push("추가 효과 위치 읽기가 새로 열렸습니다.");
  } else if (
    afterSnapshot.overlayRecognition?.status === "recognized" &&
    beforeSnapshot.overlayRecognition?.status !== "recognized"
  ) {
    effects.push("추가 효과 위치 읽기가 더 안정적으로 잡혔습니다.");
  } else if (
    afterSnapshot.overlayRecognition?.topCandidate?.blockedBy ||
    beforeSnapshot.overlayRecognition?.topCandidate?.blockedBy
  ) {
    effects.push("규칙 차단은 그대로 유지됩니다.");
  } else if (afterOverlayGap > beforeOverlayGap + 0.03) {
    effects.push("추가 효과 후보 차이도 조금 더 벌어졌습니다.");
  } else {
    effects.push("추가 효과 읽기는 큰 변화 없이 유지됩니다.");
  }

  effects.push("최종 판정은 여전히 규칙 기준으로 고정됩니다.");
  return effects;
}

function buildTutorialComparisonMetrics(
  beforeSnapshot: TutorialComparisonSnapshot,
  afterSnapshot: TutorialComparisonSnapshot
): Array<[string, string]> {
  const stage = strongestPersonalizationStage(
    afterSnapshot.baseResult.personalization?.stage,
    afterSnapshot.overlayRecognition?.personalization?.stage
  );

  return [
    [
      "기본 모양 후보 차이",
      `${candidateGap(beforeSnapshot.baseResult.candidates).toFixed(2)} -> ${candidateGap(afterSnapshot.baseResult.candidates).toFixed(2)}`
    ],
    [
      "추가 효과 후보 차이",
      `${candidateGap(beforeSnapshot.overlayRecognition?.candidates ?? []).toFixed(2)} -> ${candidateGap(afterSnapshot.overlayRecognition?.candidates ?? []).toFixed(2)}`
    ],
    ["연습 입력 누적", `${beforeSnapshot.tutorialSampleCount} -> ${afterSnapshot.tutorialSampleCount}`],
    ["현재 반영 단계", personalizationStageLabel(stage)]
  ];
}

function candidateGap<T extends { score: number }>(candidates: T[]): number {
  const top = candidates[0]?.score ?? 0;
  const second = candidates[1]?.score ?? 0;
  return Math.max(0, top - second);
}

function buildPersonalizationDemoModel(
  baseResult: RecognitionResult,
  overlayRecognition: OverlayRecognition | null,
  tutorialStore: TutorialProfileStore,
  demoView: DemoViewState
): PersonalizationDemoModel {
  const runtime = getTinyMlRuntimeStatus();
  const policy = buildTutorialPersonalizationPolicyMetadata(tutorialStore);
  const tutorialSamples = tutorialStore.shapeProfile.tutorialSampleCount;
  const familySamples = tutorialStore.shapeProfile.familyTutorialSampleCount ?? 0;
  const operatorSamples = tutorialStore.shapeProfile.operatorTutorialSampleCount ?? 0;
  const baseShadow = baseResult.shadow;
  const overlayShadow = overlayRecognition?.shadow;
  const stage = strongestPersonalizationStage(baseShadow?.personalizationStage, overlayShadow?.personalizationStage);
  const stageTone = stage === "enough_shot" ? "recognized" : stage === "few_shot" ? "ready" : "waiting";
  const shadowReady = runtime.baseShadowAvailable || runtime.operatorShadowAvailable;
  const stripBadges = [
    renderPromiseBadge(shadowReady ? "보조 판독 참고 계산" : "보조 판독 준비 중", shadowReady),
    renderPromiseBadge(`연습 입력 ${tutorialSamples}회`, tutorialSamples > 0),
    renderPromiseBadge(policy.statusLabel, policy.canApply),
    renderPromiseBadge(personalizationStageLabel(stage), stage !== "none")
  ].join("");
  const effectRows = renderMetricNotes([
    summarizeShadowEffect(baseResult, "base"),
    summarizeShadowEffect(overlayRecognition, "overlay"),
    "최종 판정은 계속 규칙 기준으로 고정됩니다."
  ]);
  const exemplarIds = resolveRelevantExemplarIds(baseResult, overlayRecognition).slice(
    0,
    demoView.viewPreset === "clean" ? 1 : 3
  );

  return {
    stripCopy:
      tutorialSamples > 0
        ? "같은 모양은 같은 종류로 유지한 채, 보조 판독과 연습 입력 반영은 참고 계산으로만 비교합니다."
        : "연습 입력이 아직 없어도 보조 판독 참고 계산은 확인할 수 있지만, 최종 판정은 그대로 유지합니다.",
    stripBadges,
    cardTitle: shadowReady ? "보조 판독 참고 계산 연결됨" : "보조 판독 준비 전",
    cardStatusLabel: stage === "none" ? (shadowReady ? "보조만" : "대기") : "반영 중",
    cardStatusTone: stageTone,
    cardCopy:
      tutorialSamples > 0
        ? "연습 입력은 입력 습관만 반영하고, 종류 의미나 규칙 자체는 바꾸지 않습니다."
        : "현재는 보조 판독만 참고 계산으로 보이고, 입력 습관 반영은 아직 시작 전입니다.",
    cardBadges: [
      renderPromiseBadge(runtime.baseShadowAvailable ? "기본 모양 보조 판독 준비" : "기본 모양 보조 판독 없음", runtime.baseShadowAvailable),
      renderPromiseBadge(
        runtime.operatorShadowAvailable ? "추가 효과 보조 판독 준비" : "추가 효과 보조 판독 없음",
        runtime.operatorShadowAvailable
      ),
      renderPromiseBadge(policy.needsBackfill ? "카드 정보 보강 필요" : policy.statusLabel, policy.canApply),
      renderPromiseBadge("최종 판정은 그대로 유지", true)
    ].join(""),
    metricRows: renderSummaryRows([
      ["연습 입력 누적", `${tutorialSamples}회`],
      ["반영 단계", personalizationStageLabel(stage)],
      ["개인화 안전 상태", policy.statusLabel],
      ["보조 판독 준비", shadowReady ? "연결됨" : "준비 중"]
    ]),
    compareTitle: "실제 판정 / 보조 판독 / 입력 습관 반영",
    compareCopy: "실제 판정은 그대로 두고, 보조 판독과 입력 습관 반영이 후보를 어떻게 다시 읽는지만 비교합니다.",
    compareLanes: renderPersonalizationCompare(baseResult, overlayRecognition),
    effectRows,
    profileTitle: `연습 입력 ${tutorialSamples}회`,
    profileCopy:
      tutorialSamples > 0
        ? `기본 모양 ${familySamples}회 / 추가 효과 ${operatorSamples}회 / 현재 단계 ${personalizationStageLabel(stage)}`
        : "연습 입력이 쌓이면 후보 여유와 위치 신호를 참고 계산에서 먼저 다시 봅니다.",
    profileSummary: renderSummaryRows([
      ["가장 최근 반영", formatClockTime(tutorialStore.updatedAt)],
      ["기본 모양 기준", `${Object.keys(tutorialStore.shapeProfile.familyPrototypes).length}종`],
      ["추가 효과 기준", `${Object.keys(tutorialStore.shapeProfile.operatorPrototypes).length}종`]
    ]),
    profileDetails: renderPracticeDetails(tutorialStore),
    exemplarTitle: "안정적으로 읽히는 기준",
    exemplarCopy:
      exemplarIds.length > 0
        ? "현재 화면과 가장 관련 있는 모범 선례만 골라 작게 보여 줍니다."
        : "모범 선례는 기본 모양이나 추가 효과 후보가 생기면 함께 보여 줍니다.",
    exemplarGrid:
      exemplarIds.length > 0
        ? exemplarIds
            .map((id, index) =>
              renderExemplarChip(id, {
                active: index === 0,
                hint: buildExemplarHint(id, baseResult, overlayRecognition)
              })
            )
            .join("")
        : `<div class="empty-state">아직 비교할 모범 선례가 없습니다.</div>`
  };
}

function renderPageSummaryBadges(
  page: DemoPage,
  baseResult: RecognitionResult,
  overlayRecognition: OverlayRecognition | null,
  store: TutorialProfileStore
): string {
  const baseLabel = readCurrentFamily(baseResult);
  const overlayLabel = overlayRecognition?.operator ?? overlayRecognition?.topCandidate?.operator ?? null;
  const samples = store.shapeProfile.tutorialSampleCount;
  const baseGate = baseResult.personalization?.mlActualGate ?? "none";
  const overlayGate = overlayRecognition?.personalization?.mlActualGate ?? "none";

  switch (page) {
    case "tutorial":
      return [
        renderPromiseBadge(`연습 입력 ${samples}회`, samples > 0),
        renderPromiseBadge(`검증 ${store.shapeProfile.validatedTutorialSampleCount ?? 0}회`, (store.shapeProfile.validatedTutorialSampleCount ?? 0) > 0),
        renderPromiseBadge(`참고 저장 ${store.shapeProfile.feedbackOnlyTutorialSampleCount ?? 0}회`, false)
      ].join("");
    case "ml":
      return [
        renderPromiseBadge(`기본 모양 반영 ${gateLabel(baseGate)}`, baseGate !== "none"),
        renderPromiseBadge(`추가 효과 반영 ${gateLabel(overlayGate)}`, overlayGate !== "none"),
        renderPromiseBadge(`믿음도 ${baseResult.personalization?.mlConfidenceGate?.toFixed(2) ?? "1.00"}`, true)
      ].join("");
    case "quality":
      return [
        renderPromiseBadge(`닫힘 ${baseResult.rawQuality.closure.toFixed(2)}`, baseResult.rawQuality.closure >= 0.7),
        renderPromiseBadge(`안정감 ${baseResult.rawQuality.stability.toFixed(2)}`, baseResult.rawQuality.stability >= 0.55),
        renderPromiseBadge("종류 고정 / 결과감 조정", true)
      ].join("");
    case "guide":
      return [
        renderPromiseBadge("같은 모양은 같은 종류", true),
        renderPromiseBadge("ML은 보조 판독", true),
        renderPromiseBadge("품질은 결과층", true)
      ].join("");
    case "logs":
      return [
        renderPromiseBadge(`연습 기록 ${samples}회`, samples > 0),
        renderPromiseBadge(`기본 ${baseLabel ? familyLabel(baseLabel) : "없음"}`, Boolean(baseLabel)),
        renderPromiseBadge(`추가 ${overlayLabel ? operatorLabel(overlayLabel) : "없음"}`, Boolean(overlayLabel))
      ].join("");
    case "dashboard":
      return [
        renderPromiseBadge("만든 입력으로 검사", true),
        renderPromiseBadge("여러 번 테스트", Boolean(baseResult.shadow)),
        renderPromiseBadge("실제 판정 로직 유지", true)
      ].join("");
    case "test":
    default:
      return [
        renderPromiseBadge(`기본 ${baseLabel ? familyLabel(baseLabel) : "대기"}`, Boolean(baseLabel)),
        renderPromiseBadge(`추가 ${overlayLabel ? operatorLabel(overlayLabel) : "대기"}`, Boolean(overlayLabel)),
        renderPromiseBadge(`상태 ${statusLabel(baseResult.status)}`, baseResult.status === "recognized")
      ].join("");
  }
}

function renderMlRows(rows: Array<[string, string]>): string {
  return rows
    .map(
      ([label, value]) => `
        <div class="metric-row">
          <span>${label}</span>
          <strong>${value}</strong>
        </div>
      `
    )
    .join("");
}

function readinessLabel(value: string): string {
  switch (value) {
    case "ready":
      return "준비됨";
    case "missing":
      return "없음";
    default:
      return value;
  }
}

function metadataFamilyLabel(value: string): string {
  return value === "none" ? "없음" : familyLabel(value);
}

function metadataOperatorLabel(value: string): string {
  return value === "none" ? "없음" : operatorLabel(value);
}

function booleanChangeLabel(value: string): string {
  switch (value) {
    case "true":
      return "변화 있음";
    case "false":
      return "변화 없음";
    default:
      return value;
  }
}

function gateLabel(value: string): string {
  switch (value) {
    case "confidence_guard":
      return "믿음도 낮아 완화 축소";
    case "suppression":
      return "오인식 위험 차단";
    case "none":
      return "없음";
    default:
      return value;
  }
}

function renderTutorialValidationSummary(store: TutorialProfileStore): string {
  return renderSummaryRows([
    ["전체 연습", `${store.shapeProfile.tutorialSampleCount}회`],
    ["반영된 연습", `${store.shapeProfile.validatedTutorialSampleCount ?? 0}회`],
    ["참고만 저장", `${store.shapeProfile.feedbackOnlyTutorialSampleCount ?? 0}회`],
    ["기본 / 추가", `${store.shapeProfile.familyTutorialSampleCount ?? 0} / ${store.shapeProfile.operatorTutorialSampleCount ?? 0}`]
  ]);
}

function renderTutorialValidationDetails(store: TutorialProfileStore): string {
  const policy = buildTutorialPersonalizationPolicyMetadata(store);
  const backfill = previewTutorialProfileCardBackfill(store);
  const policyRows = [
    `개인화 안전 상태: ${policy.statusLabel}`,
    ...policy.reasonLabels.map((label) => `안전 확인: ${label}`),
    `카드 메타데이터: ${backfill.userMessage}`,
    ...backfill.blockingDetails.map((detail) => `보강 확인: ${detail}`)
  ];
  const familyBiasRows = Object.entries(store.shapeProfile.familyThresholdBias ?? {}).map(
    ([family, bias]) => `${familyLabel(family)} 인식 기준 조정 ${Number(bias).toFixed(3)}`
  );
  const operatorBiasRows = Object.entries(store.shapeProfile.operatorThresholdBias ?? {}).map(
    ([operator, bias]) => `${operatorLabel(operator)} 인식 기준 조정 ${Number(bias).toFixed(3)}`
  );
  const reliabilityRows = [
    ...Object.entries(store.shapeProfile.familyPrototypeReliability ?? {}).map(
      ([family, reliability]) => `${familyLabel(family)} 내 손모양 신뢰도 ${Number(reliability).toFixed(2)}`
    ),
    ...Object.entries(store.shapeProfile.operatorPrototypeReliability ?? {}).map(
      ([operator, reliability]) => `${operatorLabel(operator)} 내 손모양 신뢰도 ${Number(reliability).toFixed(2)}`
    )
  ];
  const rows = [...policyRows, ...familyBiasRows, ...operatorBiasRows, ...reliabilityRows].slice(0, 10);

  if (rows.length === 0) {
    return renderMetricNotes(["아직 라벨별 인식 기준이나 내 손모양 신뢰도가 생성되지 않았습니다."]);
  }

  return renderMetricNotes(rows);
}

function renderDatacardShapePresetButtons(group: DatacardShapePresetGroup, activeId: DatacardShapeId): string {
  return listDatacardShapePresets()
    .filter((preset) => preset.group === group)
    .map((preset) => {
      const validation = validateDatacardShapePreset(preset);
      return `
        <button
          type="button"
          class="datacard-shape-preset ${preset.id === activeId ? "active" : ""}"
          data-datacard-shape-id="${escapeDashboardHtml(preset.id)}"
        >
          <strong>${escapeDashboardHtml(preset.label)}</strong>
          <span>${escapeDashboardHtml(preset.description)}</span>
          <small>${validation.valid ? "정의 확인됨" : "정의 확인 필요"}</small>
        </button>
      `;
    })
    .join("");
}

function renderDatacardShapeDefinition(
  preset: DatacardShapePreset,
  validation: ReturnType<typeof validateDatacardShapePreset>
): string {
  return `
    <div class="datacard-definition-row">
      <span>정의 표현</span>
      <code>${escapeDashboardHtml(preset.definition.expression)}</code>
    </div>
    <div class="datacard-definition-row">
      <span>그리기 기준</span>
      <strong>${escapeDashboardHtml(preset.definition.guide)}</strong>
    </div>
    <div class="datacard-definition-row">
      <span>카드 상태</span>
      <strong>${validation.valid ? "바로 테스트 가능" : validation.issues.map((issue) => issue.message).join(", ")}</strong>
    </div>
  `;
}

function renderDatacardShapeComparison(result: DatacardRecognitionResult): string {
  const selected = result.selectedCandidate;
  const beforeLabel = selected.captureCount > 0 ? "카드 기준" : "현재 기준";
  const afterLabel = selected.captureCount > 0 ? "연습 반영" : "연습 대기";

  return `
    <div class="datacard-comparison-lane">
      <span>${beforeLabel}</span>
      <strong>${datacardPercent(selected.baselineScore)}</strong>
      <small>예시 도형과 그리기 기준으로 비교</small>
    </div>
    <div class="datacard-comparison-lane">
      <span>${afterLabel}</span>
      <strong>${datacardPercent(selected.score)}</strong>
      <small>${selected.captureCount > 0 ? "저장된 연습 입력을 함께 반영" : "입력을 저장하면 차이를 비교"}</small>
    </div>
  `;
}

function datacardStatusTone(status: RecognitionStatus): "recognized" | "ambiguous" | "incomplete" | "invalid" {
  return status;
}

function datacardPercent(value: number): string {
  return `${Math.max(0, Math.min(100, value * 100)).toFixed(0)}%`;
}

const DATACARD_PRESET_IDS = ["family:fire", "family:water", "operator:void_cut", "operator:martial_axis"] as const;

function renderDatacardPresetButtons(): string {
  const cards = listBuiltInMagicCards();

  return DATACARD_PRESET_IDS.map((id) => {
    const card = cards.find((item) => item.id === id);

    if (!card) {
      return "";
    }

    return `
      <button type="button" class="datacard-preset" data-datacard-preset-id="${escapeDashboardHtml(id)}">
        <strong>${escapeDashboardHtml(card.label)}</strong>
        <span>${escapeDashboardHtml(card.tutorial.summary)}</span>
      </button>
    `;
  }).join("");
}

function buildDatacardPresetPatch(presetId: string): string {
  const card = listBuiltInMagicCards().find((item) => item.id === presetId) ?? listBuiltInMagicCards()[0];
  const tutorial = {
    ...card.tutorial,
    title: `${card.tutorial.title} preset`,
    instruction: `${card.tutorial.instruction} preset 미리보기에서는 실제 판정 기준은 유지됩니다.`,
    summary: `${card.tutorial.summary} 이 preset은 guide/tutorial/what-if 문구만 바꿉니다.`
  };

  return JSON.stringify({
    cardSetId: "web-ui/preset-preview",
    cards: [
      {
        ...card,
        tutorial
      }
    ]
  });
}

export interface DatacardAuthoringDisplay {
  statusLabel: string;
  statusTone: "recognized" | "ready" | "waiting" | "invalid";
  summaryRows: Array<[string, string]>;
  issueLines: string[];
}

export function buildDatacardAuthoringDisplay(result: MagicDatacardLoadResult | null): DatacardAuthoringDisplay {
  if (!result) {
    return {
      statusLabel: "대기",
      statusTone: "waiting",
      summaryRows: [
        ["적용 범위", "튜토리얼/what-if 미리보기"],
        ["현재 카드", "내장 datacard"],
        ["판정 영향", "없음"]
      ],
      issueLines: ["preset 미리보기를 선택하면 문구와 상상 비교 후보만 교체됩니다."]
    };
  }

  if (result.ok && result.registry) {
    const summary = result.registry.compatibility.coverage;
    const warnings = result.issues.filter((issue) => issue.severity === "warning").map((issue) => issue.message);

    return {
      statusLabel: "미리보기 준비",
      statusTone: "recognized",
      summaryRows: [
        ["카드 묶음", result.registry.cardSetId],
        ["카드 수", `${summary.cardCount}장`],
        ["기본/추가", `${summary.familyCount} / ${summary.operatorCount}`],
        ["불러오기", result.loadMode === "patch" ? "패치 병합" : "전체 교체"]
      ],
      issueLines: [
        result.registry.compatibility.userMessage,
        "실제 인식 라벨과 보정 기준은 변경하지 않습니다.",
        ...warnings
      ]
    };
  }

  return {
    statusLabel: "검증 필요",
    statusTone: "invalid",
    summaryRows: [
      ["불러오기", result.loadMode === "patch" ? "패치 병합" : "전체 교체"],
      ["오류", `${result.issues.filter((issue) => issue.severity === "error").length}건`],
      ["현재 카드", "내장 datacard 유지"]
    ],
    issueLines: result.issues.length > 0
      ? result.issues.slice(0, 6).map((issue) => `${issue.path}: ${issue.message}`)
      : [result.userMessage]
  };
}

function renderPromiseBadge(text: string, positive: boolean): string {
  return `<span class="inline-guarantee ${positive ? "positive" : "muted"}">${text}</span>`;
}

function renderSummaryRows(items: Array<[string, string]>): string {
  return items
    .map(
      ([label, value]) => `
        <div class="summary-row">
          <span>${label}</span>
          <strong>${value}</strong>
        </div>
      `
    )
    .join("");
}

function renderMetricNotes(lines: string[]): string {
  return lines
    .map(
      (line) => `
        <div class="metric-row">
          <span>${line}</span>
        </div>
      `
    )
    .join("");
}

function renderDashboardPresetButtons(activePresetId: string): string {
  return DASHBOARD_SCENARIO_PRESETS.map((preset) => {
    const active = preset.id === activePresetId;

    return `
      <button
        type="button"
        class="dashboard-preset ${active ? "active" : ""}"
        data-dashboard-preset-id="${escapeDashboardHtml(preset.id)}"
        aria-pressed="${active ? "true" : "false"}"
      >
        <strong>${escapeDashboardHtml(preset.label)}</strong>
        <span>${escapeDashboardHtml(preset.description)}</span>
      </button>
    `;
  }).join("");
}

function renderDashboardComparisonLanes(comparison: DashboardComparisonSummary): string {
  return comparison.lanes
    .map(
      (lane) => `
        <section class="dashboard-comparison-lane">
          <span>${escapeDashboardHtml(lane.label)}</span>
          <strong>${lane.recognizedRate.toFixed(1)}%</strong>
          <small>ambiguous ${lane.ambiguousRate.toFixed(1)}% · gap ${lane.averageScoreGap.toFixed(3)}</small>
        </section>
      `
    )
    .join("");
}

function renderDashboardComparisonTable(comparison: DashboardComparisonSummary): string {
  return `
    <div class="dashboard-comparison-metrics">
      ${comparison.lanes
        .map((lane) => {
          const tokens = [
            { label: "recognized", value: `${lane.recognizedRate.toFixed(1)}%`, tone: "recognized" },
            { label: "ambiguous", value: `${lane.ambiguousRate.toFixed(1)}%`, tone: "ambiguous" },
            { label: "incomplete", value: `${lane.incompleteRate.toFixed(1)}%`, tone: "incomplete" },
            { label: "invalid", value: `${lane.invalidRate.toFixed(1)}%`, tone: "invalid" },
            { label: "score gap", value: lane.averageScoreGap.toFixed(3), tone: "neutral" },
            { label: "tutorial delta", value: String(lane.personalizedChangedCount), tone: "neutral" }
          ];

          return `
            <section class="dashboard-comparison-metric-card">
              <div class="dashboard-comparison-metric-head">
                <span>${escapeDashboardHtml(lane.label)}</span>
                <strong>${lane.recognizedRate.toFixed(1)}%</strong>
              </div>
              <div class="dashboard-comparison-token-grid">
                ${tokens
                  .map(
                    (token) => `
                      <span class="dashboard-comparison-token tone-${token.tone}">
                        <small>${escapeDashboardHtml(token.label)}</small>
                        <strong>${escapeDashboardHtml(token.value)}</strong>
                      </span>
                    `
                  )
                  .join("")}
              </div>
            </section>
          `;
        })
        .join("")}
    </div>
  `;
}

function renderDashboardStrokeSvg(session: StrokeSession): string {
  const points = session.strokes
    .flatMap((stroke) => stroke.points)
    .filter((point) => Number.isFinite(point.x) && Number.isFinite(point.y));

  if (points.length === 0) {
    return `<div class="empty-state">아직 그릴 선 입력이 없습니다.</div>`;
  }

  const minX = Math.min(...points.map((point) => point.x));
  const maxX = Math.max(...points.map((point) => point.x));
  const minY = Math.min(...points.map((point) => point.y));
  const maxY = Math.max(...points.map((point) => point.y));
  const width = 360;
  const height = 240;
  const padding = 18;
  const boundsWidth = Math.max(maxX - minX, 1);
  const boundsHeight = Math.max(maxY - minY, 1);
  const scale = Math.min((width - padding * 2) / boundsWidth, (height - padding * 2) / boundsHeight);
  const offsetX = (width - boundsWidth * scale) / 2;
  const offsetY = (height - boundsHeight * scale) / 2;
  const project = (point: { x: number; y: number }): string => {
    const x = offsetX + (point.x - minX) * scale;
    const y = offsetY + (point.y - minY) * scale;
    return `${x.toFixed(1)},${y.toFixed(1)}`;
  };

  const polylines = session.strokes
    .filter((stroke) => stroke.points.length > 0)
    .map((stroke, index) => {
      const fadeIndex = Math.min(index, 7);
      return `<polyline class="dashboard-stroke-line dashboard-stroke-fade-${fadeIndex}" points="${stroke.points.map(project).join(" ")}" />`;
    })
    .join("");

  return `
    <svg class="dashboard-stroke-svg" viewBox="0 0 ${width} ${height}" role="img" aria-label="만든 선 입력 미리보기">
      <rect class="dashboard-stroke-paper" x="1" y="1" width="${width - 2}" height="${height - 2}" rx="16" />
      <path class="dashboard-stroke-guide" d="M${width / 2} ${padding} V${height - padding} M${padding} ${height / 2} H${width - padding}" />
      ${polylines}
    </svg>
  `;
}

function renderDashboardBars(items: DashboardPlotModel["statusBars"]): string {
  if (items.length === 0) {
    return renderEmptyDashboardPlot("분포 대기");
  }

  const qualityIds = new Set(["closure", "smoothness", "stability", "rotationBias"]);
  const isQuality = items.every((item) => qualityIds.has(item.id));

  return items
    .map((item) => {
      const ratio = Math.max(0, Math.min(1, item.ratio));
      const valueLabel = isQuality ? `${(item.value * 100).toFixed(0)}점` : `${item.value}회`;
      return `
        <div class="dashboard-bar-row">
          <div class="dashboard-bar-head">
            <span>${escapeDashboardHtml(item.label)}</span>
            <strong>${valueLabel}</strong>
          </div>
          <div class="dashboard-bar-track" aria-hidden="true">
            <span class="${percentClass(ratio)}"></span>
          </div>
        </div>
      `;
    })
    .join("");
}

function renderDashboardHeatmap(model: DashboardPlotModel): string {
  if (model.heatmap.length === 0) {
    return renderEmptyDashboardPlot("헷갈림 지도 대기");
  }

  const cells = model.heatmap.slice(0, 12).map((cell) => {
    const intensity = Math.max(0, Math.min(10, Math.round(cell.intensity * 10)));
    return `
      <div class="dashboard-heat-cell dashboard-heat-${intensity}">
        <span>${escapeDashboardHtml(cell.expected)} → ${escapeDashboardHtml(cell.actual)}</span>
        <strong>${cell.count}회</strong>
      </div>
    `;
  });
  const familyBars = model.familyBars.slice(0, 5).map(
    (item) => `
      <span class="dashboard-family-pill">
        ${escapeDashboardHtml(item.label)} ${item.value}회
      </span>
    `
  );

  return `
    <div class="dashboard-plot-heading">
      <span>기대한 모양과 실제로 읽힌 모양</span>
    </div>
    <div class="dashboard-heat-grid">${cells.join("")}</div>
    <div class="dashboard-family-row">${familyBars.join("")}</div>
  `;
}

function renderDashboardScatter(model: DashboardPlotModel): string {
  if (model.scorePoints.length === 0) {
    return renderEmptyDashboardPlot("점 분포 대기");
  }

  const width = 360;
  const height = 190;
  const padding = 24;
  const maxVisiblePoints = 420;
  const stride = Math.max(1, Math.ceil(model.scorePoints.length / maxVisiblePoints));
  const visiblePoints = model.scorePoints.filter((_, index) => index % stride === 0).slice(0, maxVisiblePoints);
  const hiddenCount = Math.max(0, model.scorePoints.length - visiblePoints.length);
  const dots = visiblePoints.map((point) => {
    const x = padding + Math.max(0, Math.min(1, point.x)) * (width - padding * 2);
    const y = padding + (1 - Math.max(0, Math.min(1, point.y))) * (height - padding * 2);
    return `
      <circle class="dashboard-dot status-${point.status}" cx="${x.toFixed(1)}" cy="${y.toFixed(1)}" r="3.2">
        <title>${escapeDashboardHtml(point.label)}</title>
      </circle>
    `;
  });

  return `
    <div class="dashboard-plot-heading">
      <span>가로: 닫힘 · 세로: 후보 점수</span>
      <strong>${model.scorePoints.length}점</strong>
    </div>
    <svg class="dashboard-scatter-svg" viewBox="0 0 ${width} ${height}" role="img" aria-label="닫힘과 후보 점수의 점 분포">
      <path class="dashboard-axis" d="M${padding} ${height - padding} H${width - padding} M${padding} ${padding} V${height - padding}" />
      <text x="${padding}" y="${height - 6}">닫힘 낮음</text>
      <text x="${width - padding - 54}" y="${height - 6}">닫힘 높음</text>
      <text x="${padding + 4}" y="${padding - 8}">점수 높음</text>
      ${dots.join("")}
    </svg>
    ${
      hiddenCount > 0
        ? `<p class="dashboard-plot-note">plot은 ${visiblePoints.length}개 대표 샘플로 표시하고, 전체 ${model.scorePoints.length}개 계산 결과는 위 분포 카드에 반영했습니다.</p>`
        : ""
    }
  `;
}

function renderDashboardFamilyDistribution(summary: DashboardFamilyMatrixSummary): string {
  const cards = summary.familySummaries
    .map((familySummary) => {
      const statusItems = [
        { label: "인정", status: "recognized" as RecognitionStatus, rate: familySummary.recognizedRate },
        { label: "헷갈림", status: "ambiguous" as RecognitionStatus, rate: familySummary.ambiguousRate },
        { label: "미완성", status: "incomplete" as RecognitionStatus, rate: familySummary.incompleteRate },
        { label: "미인식", status: "invalid" as RecognitionStatus, rate: familySummary.invalidRate }
      ];
      const statusSegments = statusItems
        .filter((item) => item.rate > 0)
        .map(
          (item) => `
            <span
              class="dashboard-stack-segment status-${dashboardStatusTone(item.status)}"
              ${dashboardWidthStyle(item.rate)}
              title="${escapeDashboardHtml(`${item.label} ${item.rate.toFixed(1)}%`)}"
            ></span>
          `
        )
        .join("");
      const statusLegend = statusItems
        .map(
          (item) => `
            <span class="dashboard-status-token status-${dashboardStatusTone(item.status)}">
              ${escapeDashboardHtml(item.label)} ${item.rate.toFixed(1)}%
            </span>
          `
        )
        .join("");
      const topActual = familySummary.topActualFamilies
        .map(
          (entry) => `
            <span class="dashboard-family-pill">
              ${escapeDashboardHtml(dashboardActualFamilyLabel(entry.family))} ${entry.rate.toFixed(1)}%
            </span>
          `
        )
        .join("");

      return `
        <section class="dashboard-family-card">
          <div class="dashboard-family-card-head">
            <span>${escapeDashboardHtml(dashboardFamilyName(familySummary.family))}</span>
            <strong>${familySummary.total}회</strong>
          </div>
          <div class="dashboard-stack" aria-label="${escapeDashboardHtml(dashboardFamilyName(familySummary.family))} 상태 분포">
            ${statusSegments || `<span class="dashboard-stack-segment status-invalid" ${dashboardWidthStyle(100)}></span>`}
          </div>
          <div class="dashboard-status-token-row">${statusLegend}</div>
          <div class="dashboard-mini-metrics">
            <span><small>평균 점수</small><strong>${familySummary.averageTopScore.toFixed(3)}</strong></span>
            <span><small>점수 차이</small><strong>${familySummary.averageScoreGap.toFixed(3)}</strong></span>
          </div>
          <div class="dashboard-family-actuals">
            ${topActual || `<span class="dashboard-family-pill">판정 없음</span>`}
          </div>
        </section>
      `;
    })
    .join("");

  return `
    <div class="dashboard-plot-heading">
      <span>모양별 생성 분포 plot</span>
      <strong>각 ${summary.iterationsPerFamily}회</strong>
    </div>
    <div class="dashboard-family-plot">${cards}</div>
  `;
}

function renderDashboardOverlapTable(summary: DashboardFamilyMatrixSummary): string {
  if (summary.overlapCells.length === 0 && summary.overlapSettings.length === 0) {
    return renderEmptyDashboardPlot("현재 설정에서는 두드러진 겹침이 없습니다.");
  }

  const overlapCards = summary.overlapCells
    .slice(0, 8)
    .map(
      (cell) => `
        <section class="dashboard-overlap-card">
          <div class="dashboard-overlap-card-head">
            <span>${escapeDashboardHtml(dashboardFamilyName(cell.expectedFamily))} → ${escapeDashboardHtml(dashboardActualFamilyLabel(cell.actualFamily))}</span>
            <span class="status-chip status-${dashboardStatusTone(cell.status)}">${escapeDashboardHtml(dashboardStatusLabel(cell.status))}</span>
          </div>
          <div class="dashboard-overlap-rate-row">
            <strong>${cell.count}건</strong>
            <span>${cell.rate.toFixed(1)}%</span>
          </div>
          <div class="dashboard-overlap-track" aria-hidden="true">
            <span class="status-${dashboardStatusTone(cell.status)}" ${dashboardWidthStyle(cell.rate)}></span>
          </div>
          <div class="dashboard-mini-metrics">
            <span><small>평균 점수</small><strong>${cell.averageTopScore.toFixed(3)}</strong></span>
            <span><small>점수 차이</small><strong>${cell.averageScoreGap.toFixed(3)}</strong></span>
          </div>
          <p>${escapeDashboardHtml(cell.settingHint)}</p>
        </section>
      `
    )
    .join("");
  const settingChips = summary.overlapSettings
    .slice(0, 6)
    .map((setting) => {
      const families = setting.expectedFamilies.map((family) => dashboardFamilyName(family)).join(", ");

      return `
        <span class="dashboard-setting-chip">
          <strong>${escapeDashboardHtml(setting.label)}</strong>
          <small>${setting.count}건 · ${escapeDashboardHtml(families)} · ${escapeDashboardHtml(setting.settingHint)}</small>
        </span>
      `;
    })
    .join("");

  return `
    <div class="dashboard-plot-heading">
      <span>겹침 특성 plot</span>
      <strong>${summary.overlapCells.reduce((sum, cell) => sum + cell.count, 0)}건</strong>
    </div>
    <div class="dashboard-overlap-plot">${overlapCards || `<div class="empty-state dashboard-empty">겹친 입력이 없습니다.</div>`}</div>
    <div class="dashboard-overlap-notes">
      ${settingChips || `<span class="dashboard-setting-chip"><small>공통으로 겹치는 설정 묶음은 아직 없습니다.</small></span>`}
    </div>
  `;
}

function renderDashboardBatchLog(log: DashboardFamilyMatrixSummary[], activeId?: string): string {
  if (log.length === 0) {
    return renderEmptyDashboardPlot("모양별 n회 생성 로그가 아직 없습니다.");
  }

  const items = log
    .map((summary, index) => {
      const activeClass = summary.id === activeId ? " is-active" : "";
      const overlapCount = summary.overlapCells.reduce((sum, cell) => sum + cell.count, 0);

      return `
        <button type="button" class="dashboard-log-item${activeClass}" data-dashboard-log-index="${index}">
          <span>${escapeDashboardHtml(formatDashboardLogTime(summary.createdAt))}</span>
          <strong>${summary.familySummaries.length}개 모양 · 각 ${summary.iterationsPerFamily}회</strong>
          <small>겹침/보류 ${overlapCount}건 · seed ${summary.seedStart}</small>
        </button>
      `;
    })
    .join("");

  return `
    <div class="dashboard-plot-heading">
      <span>누적 실행 로그</span>
      <strong>${log.length}개</strong>
    </div>
    <div class="dashboard-log-list">${items}</div>
  `;
}

function dashboardActualFamilyLabel(family: GlyphFamily | "none"): string {
  return family === "none" ? "판정 없음" : dashboardFamilyName(family);
}

function formatDashboardLogTime(timestamp: number): string {
  return new Date(timestamp).toLocaleTimeString("ko-KR", {
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit"
  });
}

function dashboardWidthStyle(value: number): string {
  const width = Math.max(0, Math.min(100, Number.isFinite(value) ? value : 0));
  return `style="width: ${width.toFixed(1)}%"`;
}

function renderEmptyDashboardPlot(label: string): string {
  return `<div class="empty-state dashboard-empty">${escapeDashboardHtml(label)}</div>`;
}

function buildDashboardReport(
  sample: DashboardBatchSample,
  summary: DashboardBatchSummary | null,
  fixture: DashboardFixtureParseResult | null,
  matrix: DashboardFamilyMatrixSummary | null = null
): Record<string, unknown> {
  return {
    generatedAt: new Date().toISOString(),
    preview: {
      expectedFamily: dashboardFamilyName(sample.expectedFamily),
      actualFamily: sample.actualFamily === "none" ? "아직 없음" : dashboardFamilyName(sample.actualFamily),
      status: dashboardStatusLabel(sample.status),
      topScore: Number(sample.topScore.toFixed(4)),
      scoreGap: Number(sample.scoreGap.toFixed(4)),
      quality: {
        closure: Number(sample.closure.toFixed(4)),
        smoothness: Number(sample.smoothness.toFixed(4)),
        stability: Number(sample.stability.toFixed(4)),
        rotationBias: Number(sample.rotationBias.toFixed(4))
      },
      shadowChanged: sample.shadowChanged,
      personalizedChanged: sample.personalizedChanged
    },
    batch: summary
      ? {
          total: summary.total,
          recipeLabel: summary.recipeLabel,
          statusCounts: summary.statusCounts,
          qualityAverages: summary.qualityAverages,
          shadowChangedCount: summary.shadowChangedCount,
          personalizedChangedCount: summary.personalizedChangedCount,
          confusionRows: summary.confusionRows.slice(0, 12)
        }
      : null,
    familyMatrix: matrix
      ? {
          id: matrix.id,
          createdAt: new Date(matrix.createdAt).toISOString(),
          iterationsPerFamily: matrix.iterationsPerFamily,
          total: matrix.samples.length,
          statusCounts: matrix.statusCounts,
          familySummaries: matrix.familySummaries.map((familySummary) => ({
            family: familySummary.family,
            total: familySummary.total,
            recognizedRate: familySummary.recognizedRate,
            ambiguousRate: familySummary.ambiguousRate,
            incompleteRate: familySummary.incompleteRate,
            invalidRate: familySummary.invalidRate,
            averageTopScore: familySummary.averageTopScore,
            averageScoreGap: familySummary.averageScoreGap,
            topActualFamilies: familySummary.topActualFamilies
          })),
          overlapCells: matrix.overlapCells.slice(0, 12),
          overlapSettings: matrix.overlapSettings.slice(0, 8)
        }
      : null,
    fixture: fixture
      ? {
          ok: fixture.ok,
          kind: fixture.kind,
          message: fixture.userMessage
        }
      : null,
    guardrails: ["survey 경로 미사용", "실제 판정 로직 변경 없음", "localStorage 자동 변경 없음"]
  };
}

function escapeDashboardHtml(value: string): string {
  return value
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#39;");
}

function percentClass(value: number): string {
  const bucket = Math.max(0, Math.min(100, Math.round(value * 20) * 5));
  return `width-${bucket}`;
}

function renderPersonalizationCompare(baseResult: RecognitionResult, overlayRecognition: OverlayRecognition | null): string {
  const lanes = [
    buildShadowLane("현재 판정", "actual", baseResult, overlayRecognition),
    buildShadowLane("보조 판독", "shadow", baseResult, overlayRecognition),
    buildShadowLane("입력 습관 반영", "personalized", baseResult, overlayRecognition)
  ];

  return lanes
    .map(
      (lane) => `
        <article class="personalization-lane ${lane.active ? "active" : ""}">
          <div class="recent-seal-head">
            <div>
              <p class="mini-label">${lane.label}</p>
              <h4>${lane.title}</h4>
            </div>
            <span class="status-chip status-${lane.statusTone}">${lane.statusLabel}</span>
          </div>
          <div class="summary-grid">
            <div class="summary-row">
              <span>기본 모양</span>
              <strong>${lane.baseLabel}</strong>
            </div>
            <div class="summary-row">
              <span>추가 효과</span>
              <strong>${lane.overlayLabel}</strong>
            </div>
          </div>
          <p class="recent-seal-summary">${lane.copy}</p>
        </article>
      `
    )
    .join("");
}

function buildShadowLane(
  label: string,
  mode: "actual" | "shadow" | "personalized",
  baseResult: RecognitionResult,
  overlayRecognition: OverlayRecognition | null
): {
  label: string;
  title: string;
  statusLabel: string;
  statusTone: string;
  baseLabel: string;
  overlayLabel: string;
  copy: string;
  active: boolean;
} {
  const actualBaseLabel = readCurrentFamily(baseResult);
  const actualBaseStatus = baseResult.status;
  const shadowBaseLabel = baseResult.shadow?.shadowTopLabel ?? actualBaseLabel;
  const shadowBaseStatus = baseResult.shadow?.shadowStatus ?? actualBaseStatus;
  const personalizedBaseLabel = baseResult.shadow?.personalizedShadowTopLabel ?? shadowBaseLabel;
  const personalizedBaseStatus = baseResult.shadow?.personalizedShadowStatus ?? shadowBaseStatus;
  const actualOverlayLabel = overlayRecognition?.operator ?? overlayRecognition?.topCandidate?.operator ?? null;
  const actualOverlayStatus = overlayRecognition?.status ?? "waiting";
  const shadowOverlayLabel = overlayRecognition?.shadow?.shadowTopLabel ?? actualOverlayLabel;
  const shadowOverlayStatus = overlayRecognition?.shadow?.shadowStatus ?? actualOverlayStatus;
  const personalizedOverlayLabel = overlayRecognition?.shadow?.personalizedShadowTopLabel ?? shadowOverlayLabel;
  const personalizedOverlayStatus = overlayRecognition?.shadow?.personalizedShadowStatus ?? shadowOverlayStatus;

  if (mode === "actual") {
    return {
      label,
      title: "현재 화면에서 확정된 값",
      statusLabel: statusLabel(actualBaseStatus),
      statusTone: actualBaseStatus,
      baseLabel: actualBaseLabel ? familyLabel(actualBaseLabel) : "아직 없음",
      overlayLabel: actualOverlayLabel ? operatorLabel(actualOverlayLabel) : "아직 없음",
      copy: "지금 화면에서 실제로 채택된 판정입니다.",
      active: true
    };
  }

  if (mode === "shadow") {
    return {
      label,
      title: "보조 판독이 다시 본 후보",
      statusLabel: statusLabel(shadowBaseStatus),
      statusTone: shadowBaseStatus,
      baseLabel: shadowBaseLabel ? familyLabel(shadowBaseLabel) : "변화 없음",
      overlayLabel: shadowOverlayLabel ? operatorLabel(shadowOverlayLabel) : "변화 없음",
      copy:
        shadowBaseLabel !== actualBaseLabel || shadowOverlayLabel !== actualOverlayLabel
          ? "보조 판독은 후보 순서를 다시 계산해 봤지만 최종 판정은 바꾸지 않았습니다."
          : "보조 판독을 켜도 현재 후보와 크게 다르지 않습니다.",
      active: false
    };
  }

  return {
    label,
    title: "연습 입력을 참고한 계산",
    statusLabel: statusLabel(personalizedBaseStatus),
    statusTone: personalizedBaseStatus,
    baseLabel: personalizedBaseLabel ? familyLabel(personalizedBaseLabel) : "변화 없음",
    overlayLabel: personalizedOverlayLabel ? operatorLabel(personalizedOverlayLabel) : "변화 없음",
    copy:
      personalizedBaseLabel !== shadowBaseLabel || personalizedOverlayLabel !== shadowOverlayLabel
        ? "연습 입력을 반영하면 후보 여유와 위치 신호가 조금 달라집니다. 그래도 최종 판정은 그대로입니다."
        : "연습 입력 반영은 아직 후보 여유만 조금 조정합니다.",
    active: false
  };
}

function renderPracticeDetails(store: TutorialProfileStore): string {
  const familyRows = Object.entries(store.shapeProfile.familyPrototypes)
    .map(([family, prototype]) => [familyLabel(family), `${prototype?.sampleCount ?? 0}회`])
    .slice(0, 5);
  const operatorRows = Object.entries(store.shapeProfile.operatorPrototypes)
    .map(([operator, prototype]) => [operatorLabel(operator), `${prototype?.sampleCount ?? 0}회`])
    .slice(0, 6);

  const rows = [...familyRows, ...operatorRows];

  if (rows.length === 0) {
    return `<div class="metric-row"><span>아직 연습 입력이 없습니다.</span></div>`;
  }

  return rows
    .map(
      ([label, value]) => `
        <div class="metric-row">
          <span>${label}</span>
          <strong>${value}</strong>
        </div>
      `
    )
    .join("");
}

export interface MagicWhatIfGuideCard {
  id: string;
  title: string;
  dimensionLabel: string;
  riskLabel: string;
  summary: string;
  nonMutating: true;
}

export function buildMagicWhatIfGuideModel(
  limit = 4,
  scenarios: readonly MagicWhatIfScenario[] = buildMagicWhatIfScenarios()
): MagicWhatIfGuideCard[] {
  const prioritizedScenarios = prioritizeWhatIfScenarios(scenarios).slice(0, limit);

  return prioritizedScenarios.map((scenario) => ({
    id: scenario.id,
    title: scenario.title,
    dimensionLabel: whatIfDimensionLabel(scenario.dimension),
    riskLabel: whatIfRiskLabel(scenario.impact.riskLevel),
    summary: summarizeWhatIfImpact(scenario),
    nonMutating: true
  }));
}

function renderMagicWhatIfGuide(
  activeScenarioId: string | null = null,
  overlayActive = false,
  scenarios: readonly MagicWhatIfScenario[] = buildMagicWhatIfScenarios()
): string {
  const cards = buildMagicWhatIfGuideModel(4, scenarios);

  if (cards.length === 0) {
    return renderMetricNotes(["아직 상상 비교 카드가 없습니다. 현재 판정은 그대로 유지됩니다."]);
  }

  return cards
    .map((card) => {
      const active = overlayActive && activeScenarioId === card.id;
      return `
        <button class="metric-row what-if-row ${active ? "active" : ""}" type="button" data-what-if-scenario-id="${card.id}">
          <span>${card.dimensionLabel} · ${card.riskLabel}<br /><strong>${card.title}</strong><br />${card.summary}</span>
          <strong>${active ? "캔버스 미리보기 켜짐" : "미리보기"}</strong>
        </button>
      `;
    })
    .join("");
}

function prioritizeWhatIfScenarios(scenarios: readonly MagicWhatIfScenario[]): MagicWhatIfScenario[] {
  const preferredIds = [
    "relation:martial_axis:requires-void_cut",
    "structure:fire:shape-mutation",
    "placement:void_cut:off-anchor-risk",
    "placement:soul_dot:underscale-risk"
  ];
  const byId = new Map(scenarios.map((scenario) => [scenario.id, scenario]));
  const preferred = preferredIds
    .map((id) => byId.get(id))
    .filter((scenario): scenario is MagicWhatIfScenario => Boolean(scenario));
  const remaining = scenarios.filter((scenario) => !preferredIds.includes(scenario.id));

  return [...preferred, ...remaining];
}

function whatIfDimensionLabel(dimension: MagicWhatIfScenario["dimension"]): string {
  switch (dimension) {
    case "structure":
      return "구조";
    case "relation":
      return "관계";
    case "placement":
      return "배치";
    default:
      return "상상 비교";
  }
}

function whatIfRiskLabel(risk: MagicWhatIfScenario["impact"]["riskLevel"]): string {
  switch (risk) {
    case "high":
      return "흔들림 큼";
    case "medium":
      return "주의";
    default:
      return "안정";
  }
}

function renderHciPrinciples(): string {
  return [
    "연습 입력 전/후를 같은 화면에서 비교합니다.",
    "최종 판정과 참고 계산을 나눠 보여 줍니다.",
    "같은 모양은 같은 종류로 유지합니다.",
    "원본 선은 그대로 두고 설명용 선만 덧댑니다.",
    "추가 효과는 위치와 길이도 함께 봅니다.",
    "연습 입력은 입력 습관만 반영하고 의미는 바꾸지 않습니다.",
    "경합과 미완성은 이유를 짧게 설명합니다.",
    "최근 변화와 이번 차이를 다시 그려 보지 않아도 읽을 수 있게 합니다."
  ]
    .map(
      (text) => `
        <div class="principle-row">
          <span>${text}</span>
        </div>
      `
    )
    .join("");
}

function buildExemplarHint(id: string, baseResult: RecognitionResult, overlayRecognition: OverlayRecognition | null): string {
  const spec = getExemplarSpec(id as GlyphFamily | OverlayOperator);

  if (!spec) {
    return "";
  }

  if (spec.category === "family") {
    if (baseResult.status === "incomplete") {
      return `${spec.caption} 지금은 닫힘이나 핵심 구조 한 줄을 더 보강하면 읽기 쉬워집니다.`;
    }

    if (baseResult.status === "ambiguous") {
      return `${spec.caption} 지금은 비슷한 후보와의 차이를 조금 더 벌려 주면 안정적입니다.`;
    }

    return spec.caption;
  }

  if (overlayRecognition?.status === "ambiguous" || overlayRecognition?.status === "incomplete") {
    return `${spec.caption} 지금은 위치와 길이를 조금 더 분명하게 보여 주는 편이 좋습니다.`;
  }

  return spec.caption;
}

function strongestPersonalizationStage(
  left: PersonalizationRuntimeSummary["stage"] | undefined,
  right: PersonalizationRuntimeSummary["stage"] | undefined
): PersonalizationRuntimeSummary["stage"] {
  const order = { none: 0, few_shot: 1, enough_shot: 2 } as const;
  const leftStage = left ?? "none";
  const rightStage = right ?? "none";
  return order[leftStage] >= order[rightStage] ? leftStage : rightStage;
}

function personalizationStageLabel(stage: PersonalizationRuntimeSummary["stage"]): string {
  switch (stage) {
    case "few_shot":
      return "조금 반영";
    case "enough_shot":
      return "충분히 반영";
    default:
      return "아직 반영 전";
  }
}

function summarizeShadowEffect(
  target: RecognitionResult | OverlayRecognition | null,
  kind: "base" | "overlay"
): string {
  const shadow = target?.shadow;

  if (!shadow) {
    return kind === "base" ? "기본 모양은 아직 참고 계산이 없습니다." : "추가 효과는 아직 참고 계산이 없습니다.";
  }

  const globalChanged = shadow.decisionChanged || shadow.statusChanged;
  const personalizedChanged = shadow.personalizedDecisionChanged || shadow.personalizedStatusChanged;

  if (kind === "base") {
    if (personalizedChanged) {
      return "연습 입력 반영 후에는 기본 모양 후보 순서가 조금 달라졌습니다.";
    }
    if (globalChanged) {
      return "보조 판독은 기본 모양 후보를 다시 계산했지만 최종 종류는 그대로입니다.";
    }
    return "기본 모양은 보조 판독을 켜도 현재 후보와 크게 다르지 않습니다.";
  }

  if (personalizedChanged) {
    return "연습 입력 반영 후에는 추가 효과의 위치 신호를 조금 다르게 읽습니다.";
  }
  if (globalChanged) {
    return "보조 판독은 추가 효과 후보를 다시 정렬해 보지만 규칙을 넘어서지는 않습니다.";
  }
  return "추가 효과는 입력 습관을 참고해도 현재 후보와 크게 다르지 않습니다.";
}

function renderQuality(quality: QualityVector): string {
  return QUALITY_VECTOR_KEYS.map((key) => {
    const clamped = Math.max(0, Math.min(quality[key], 1));
    return `
      <div class="quality-row">
        <div class="quality-head">
          <span>${qualityMetricLabel(key)}</span>
          <strong>${(clamped * 100).toFixed(0)}</strong>
        </div>
        <div class="quality-bar">
          <span class="${percentClass(clamped)}"></span>
        </div>
      </div>
    `;
  }).join("");
}

function renderQualitySummary(rawQuality: QualityVector, adjustedQuality: QualityVector): string {
  const rawAverage = averageQuality(rawQuality);
  const adjustedAverage = averageQuality(adjustedQuality);

  return `
    <div class="summary-row">
      <span>원본 평균</span>
      <strong>${(rawAverage * 100).toFixed(0)}</strong>
    </div>
    <div class="summary-row">
      <span>보정 평균</span>
      <strong>${(adjustedAverage * 100).toFixed(0)}</strong>
    </div>
    <div class="summary-row">
      <span>차이</span>
      <strong>${formatSigned(adjustedAverage - rawAverage, 3)}</strong>
    </div>
  `;
}

function renderOverlayPreviewMeta(recognition: OverlayRecognition | null): string {
  if (!recognition?.topCandidate) {
    return "";
  }

  return `
    <div class="summary-row">
      <span>위치 힌트</span>
      <strong>${recognition.anchorZoneId ?? recognition.topCandidate.anchorZoneId ?? "없음"}</strong>
    </div>
    <div class="summary-row">
      <span>가장 가까운 후보</span>
      <strong>${(recognition.topCandidate.score * 100).toFixed(1)}</strong>
    </div>
    <div class="summary-row">
      <span>모양 거리</span>
      <strong>${recognition.topCandidate.templateDistance.toFixed(3)}</strong>
    </div>
  `;
}

function renderProfileDelta(delta: UserInputProfileDelta): string {
  const metricRows = QUALITY_VECTOR_KEYS.map(
    (key) => `
      <div class="metric-row">
        <span>${qualityMetricLabel(key)}</span>
        <strong>${formatSigned(delta.averageQualityDelta[key], 3)}</strong>
      </div>
    `
  ).join("");

  return `
    <div class="metric-row">
      <span>최근 누적 종류</span>
      <strong>${delta.familyIncrement ? familyLabel(delta.familyIncrement) : "없음"}</strong>
    </div>
    ${metricRows}
  `;
}

function renderCompileSummary(compiled: CompiledSealResult): string {
  const overlays =
    compiled.overlayOperators.length > 0
      ? compiled.overlayOperators
          .map((recognition) => recognition.operator)
          .filter((operator): operator is OverlayOperator => Boolean(operator))
          .map((operator) => operatorLabel(operator))
          .join(" -> ")
      : "없음";

  return `
    <div class="summary-row">
      <span>기본 모양</span>
      <strong>${familyLabel(compiled.baseFamily)}</strong>
    </div>
    <div class="summary-row">
      <span>추가 효과</span>
      <strong>${overlays}</strong>
    </div>
    <div class="summary-row">
      <span>기본 품질</span>
      <strong>${(averageQuality(compiled.rawQuality) * 100).toFixed(0)}</strong>
    </div>
    <div class="summary-row">
      <span>반영 품질</span>
      <strong>${(averageQuality(compiled.adjustedQuality) * 100).toFixed(0)}</strong>
    </div>
    <div class="summary-row">
      <span>습관 누적</span>
      <strong>${compiled.profileDelta ? `${compiled.profileDelta.previousSampleCount} -> ${compiled.profileDelta.nextSampleCount}` : "없음"}</strong>
    </div>
  `;
}

function renderOutcomeCompare(compare: DemoOutcomeCompare, qualityInfluence: boolean): string {
  if (!compare.family) {
    return `<div class="empty-state">기본 모양을 먼저 고정하면 품질 전후 비교가 열립니다.</div>`;
  }

  const changedMetrics = getChangedOutcomeMetrics(compare);

  return `
    <section class="compare-lane ${qualityInfluence ? "" : "selected"}">
      ${renderOutcomeLane(compare.off, compare, !qualityInfluence)}
    </section>
    <section class="compare-lane ${qualityInfluence ? "selected" : ""}">
      ${renderOutcomeLane(compare.on, compare, qualityInfluence)}
    </section>
    <div class="compare-delta-row">
      ${
        changedMetrics.length > 0
          ? changedMetrics
        .map(
          (metric) => `
            <div class="delta-pill delta-active delta-${compare.delta[metric] >= 0 ? "up" : "down"}">
              <span>${outcomeMetricLabel(metric)}</span>
              <strong>${formatSigned(compare.delta[metric], 2)}</strong>
            </div>
          `
        )
        .join("")
          : `<div class="empty-state">현재 입력은 품질 반영 전후 차이가 아직 크지 않습니다.</div>`
      }
    </div>
  `;
}

function renderOutcomeLane(summary: DemoOutcomeSummary, compare: DemoOutcomeCompare, active: boolean): string {
  return `
    <div class="compare-head">
      <div>
        <p class="mini-label">${summary.qualityEnabled ? "품질 반영" : "기본 결과"}</p>
        <h4>${summary.family ? familyLabel(summary.family) : "종류 대기"}</h4>
      </div>
      <span class="status-chip status-${active ? "recognized" : "waiting"}">${summary.qualityEnabled ? "반영됨" : "기본값"}</span>
    </div>
    <p class="compare-copy">${summary.summary}</p>
    <div class="fingerprint-row">
      ${summary.fingerprint.map((tag) => `<span class="fingerprint-pill">${tag}</span>`).join("")}
    </div>
    <div class="outcome-metric-list">
      ${renderOutcomeMetricRows(summary, compare, summary.qualityEnabled)}
    </div>
  `;
}

function renderOutcomeMetricRows(
  summary: DemoOutcomeSummary,
  compare: DemoOutcomeCompare,
  qualityEnabled: boolean
): string {
  return (["output", "control", "stability", "risk"] as const)
    .map((metric) => {
      const value = summary[metric];
      const changed = Math.abs(compare.delta[metric]) >= 0.005;
      return `
        <div class="outcome-row ${changed ? "delta-active" : ""}">
          <div class="quality-head">
            <span>${outcomeMetricLabel(metric)}</span>
            <div class="outcome-head-values">
              ${qualityEnabled && changed ? `<em class="metric-delta delta-${compare.delta[metric] >= 0 ? "up" : "down"}">${formatSigned(compare.delta[metric], 2)}</em>` : ""}
              <strong>${Math.round(value * 100)}</strong>
            </div>
          </div>
          <div class="quality-bar outcome-bar">
            <span class="${percentClass(value)}"></span>
          </div>
        </div>
      `;
    })
    .join("");
}

function renderWhyPanel(
  baseDisplay: RecognitionResult,
  compare: DemoOutcomeCompare,
  qualityInfluence: boolean,
  overlayLive: OverlayRecognition | null,
  compilePreview: CompiledSealResult | null
): string {
  return buildExplainNotes(baseDisplay, compare, qualityInfluence, overlayLive, compilePreview)
    .map(
      (note) => `
        <div class="explain-note explain-${note.tone}">
          <p>${note.text}</p>
        </div>
      `
    )
    .join("");
}

function renderRecentSeals(
  items: RecentSealSnapshot[],
  qualityInfluence: boolean,
  current: { status: RecognitionResult["status"]; family: string | null; compare: DemoOutcomeCompare; overlayCount: number }
): string {
  const visibleItems = items.slice(0, 3);
  const placeholders = Array.from({ length: Math.max(0, 3 - visibleItems.length) }, (_, index) => index);

  return `
    ${renderQuickCompareSummary(current, items[0] ?? null, qualityInfluence)}
    <div class="recent-seal-grid">
      ${visibleItems
        .map((item, index) => {
          const summary = qualityInfluence ? item.compare.on : item.compare.off;
          return `
            <article class="recent-seal-card">
              <div class="recent-seal-head">
                <div>
                  <p class="mini-label">${recentSlotLabel(index)} · ${stageLabel(item.stage)}</p>
                  <h4>${item.family ? familyLabel(item.family) : "종류 대기"}</h4>
                </div>
                <span class="status-chip status-${item.status}">${statusLabel(item.status)}</span>
              </div>
              <div class="recent-thumb">${renderSealThumbnail(item.previewStrokes)}</div>
              <p class="recent-seal-copy">${formatClockTime(item.timestamp)} · 추가 효과 ${item.overlayCount}개</p>
              <p class="recent-seal-summary">${recentSealSummary(item, qualityInfluence)}</p>
              <div class="fingerprint-row">
                ${summary.fingerprint.map((tag) => `<span class="fingerprint-pill">${tag}</span>`).join("")}
              </div>
              <div class="recent-delta-row">
                ${renderRecentDeltaPills(item, qualityInfluence)}
              </div>
            </article>
          `;
        })
        .join("")}
      ${placeholders
        .map(
          (index) => `
            <article class="recent-seal-card recent-seal-placeholder">
              <div class="recent-seal-head">
                <div>
                  <p class="mini-label">${recentSlotLabel(visibleItems.length + index)}</p>
                  <h4>대기</h4>
                </div>
                <span class="status-chip status-waiting">대기</span>
              </div>
              <p class="recent-seal-copy">기본 모양 고정 또는 최종 결과 보기 이후 다음 비교가 여기에 추가됩니다.</p>
            </article>
          `
        )
        .join("")}
    </div>
  `;
}

function renderAnalysisLegend(enabled: boolean): string {
  if (!enabled) {
    return `<span class="analysis-pill passive">분석 안내선 꺼짐</span>`;
  }

  return `
    <span class="analysis-pill">축선</span>
    <span class="analysis-pill">닫힘</span>
    <span class="analysis-pill">위치 힌트</span>
    <span class="analysis-pill">가이드 모양</span>
  `;
}

function renderQuickCompareSummary(
  current: { status: RecognitionResult["status"]; family: string | null; compare: DemoOutcomeCompare; overlayCount: number },
  previous: RecentSealSnapshot | null,
  qualityInfluence: boolean
): string {
  const currentSummary = qualityInfluence ? current.compare.on : current.compare.off;

  if (!previous) {
    return `
      <article class="quick-compare-card">
        <p class="mini-label">현재 시도</p>
        <h4>${current.family ? familyLabel(current.family) : "종류 대기"}</h4>
        <p class="recent-seal-summary">${currentSummary.summary}</p>
        <div class="recent-delta-row">
          ${renderCurrentAttemptPills(current, qualityInfluence)}
        </div>
      </article>
    `;
  }

  const previousSummary = qualityInfluence ? previous.compare.on : previous.compare.off;

  return `
    <article class="quick-compare-card">
      <div class="recent-seal-head">
        <div>
          <p class="mini-label">현재 vs 직전</p>
          <h4>${renderQuickCompareFamily(current.family, previous.family)}</h4>
        </div>
        <span class="status-chip status-${current.status}">${statusLabel(current.status)}</span>
      </div>
      <p class="recent-seal-summary">${renderQuickCompareStatus(current.status, previous.status)}</p>
      <p class="recent-seal-copy">${renderQuickCompareOutcome(currentSummary, previousSummary)}</p>
      <div class="recent-delta-row">
        ${renderCurrentAttemptPills(current, qualityInfluence)}
      </div>
    </article>
  `;
}

function averageQuality(quality: QualityVector): number {
  return (
    QUALITY_VECTOR_KEYS.reduce((sum, key) => sum + quality[key], 0) / Math.max(QUALITY_VECTOR_KEYS.length, 1)
  );
}

function phaseHudLabel(phase: RitualPhase, baseSealed: boolean, overlayAuthoringStarted: boolean): string {
  if (!baseSealed) {
    return "기본 모양 입력";
  }

  if (phase === "base") {
    return "추가 효과 준비";
  }

  if (phase === "overlay" && overlayAuthoringStarted) {
    return "추가 효과 작성";
  }

  return "최종 결과 고정";
}

function formatSigned(value: number, digits: number): string {
  const fixed = value.toFixed(digits);
  return value > 0 ? `+${fixed}` : fixed;
}

function formatSignedMs(value: number): string {
  const rounded = Math.round(value);
  return rounded > 0 ? `+${rounded}ms` : `${rounded}ms`;
}

function sessionDiagonal(session: StrokeSession): number {
  const points = session.strokes.flatMap((stroke) => stroke.points);

  if (points.length === 0) {
    return 1;
  }

  const xs = points.map((point) => point.x);
  const ys = points.map((point) => point.y);

  return Math.max(Math.hypot(Math.max(...xs) - Math.min(...xs), Math.max(...ys) - Math.min(...ys)), 1);
}

function outcomeMetricLabel(metric: DemoOutcomeMetric): string {
  switch (metric) {
    case "output":
      return "출력감";
    case "control":
      return "제어감";
    case "stability":
      return "안정감";
    case "risk":
      return "위험도";
    default:
      return metric;
  }
}

function stageLabel(stage: "base" | "final"): string {
  return stage === "final" ? "최종 결과" : "기본 모양";
}

function recentSlotLabel(index: number): string {
  switch (index) {
    case 0:
      return "가장 최근";
    case 1:
      return "직전";
    case 2:
      return "그 이전";
    default:
      return `비교 ${index + 1}`;
  }
}

function recentSealSummary(item: RecentSealSnapshot, qualityInfluence: boolean): string {
  const selected = qualityInfluence ? item.compare.on : item.compare.off;
  const changedMetrics = strongestDeltaMetrics(item.compare);

  if (qualityInfluence && changedMetrics.length > 0) {
    return changedMetrics
      .map((metric) => `${outcomeMetricLabel(metric)} ${formatSigned(item.compare.delta[metric], 2)}`)
      .join(" / ");
  }

  return selected.summary;
}

function renderRecentDeltaPills(item: RecentSealSnapshot, qualityInfluence: boolean): string {
  const changedMetrics = strongestDeltaMetrics(item.compare);

  if (qualityInfluence && changedMetrics.length > 0) {
    return changedMetrics
      .map(
        (metric) => `
          <span>${outcomeMetricLabel(metric)} ${formatSigned(item.compare.delta[metric], 2)}</span>
        `
      )
      .join("");
  }

  return `<span>${(qualityInfluence ? item.compare.on : item.compare.off).fingerprint.join(" / ")}</span>`;
}

function renderCurrentAttemptPills(
  current: { status: RecognitionResult["status"]; family: string | null; compare: DemoOutcomeCompare; overlayCount: number },
  qualityInfluence: boolean
): string {
  const changedMetrics = strongestDeltaMetrics(current.compare);

  if (qualityInfluence && changedMetrics.length > 0) {
    return changedMetrics
      .map(
        (metric) => `
          <span>${outcomeMetricLabel(metric)} ${formatSigned(current.compare.delta[metric], 2)}</span>
        `
      )
      .join("");
  }

  return `<span>추가 효과 ${current.overlayCount}개</span><span>품질 ${qualityInfluence ? "반영" : "끄기"}</span>`;
}

function strongestDeltaMetrics(compare: DemoOutcomeCompare): DemoOutcomeMetric[] {
  return (Object.entries(compare.delta) as Array<[DemoOutcomeMetric, number]>)
    .filter(([, value]) => Math.abs(value) >= 0.005)
    .sort((left, right) => Math.abs(right[1]) - Math.abs(left[1]))
    .slice(0, 2)
    .map(([metric]) => metric);
}

function renderQuickCompareFamily(currentFamily: string | null, previousFamily: string | null): string {
  if (!currentFamily && !previousFamily) {
    return "종류를 아직 정하지 않음";
  }

  if (currentFamily && previousFamily && currentFamily === previousFamily) {
    return `같은 종류 유지: ${familyLabel(currentFamily)}`;
  }

  if (currentFamily && previousFamily) {
    return `${familyLabel(previousFamily)} -> ${familyLabel(currentFamily)}`;
  }

  if (currentFamily) {
    return `현재는 ${familyLabel(currentFamily)}형에 가까움`;
  }

  return `직전 결과는 ${familyLabel(previousFamily ?? "")}`;
}

function renderQuickCompareStatus(
  currentStatus: RecognitionResult["status"],
  previousStatus: RecentSealSnapshot["status"]
): string {
  if (currentStatus === previousStatus) {
    return `상태는 ${statusLabel(currentStatus)}로 유지됩니다.`;
  }

  return `상태가 ${statusLabel(previousStatus)}에서 ${statusLabel(currentStatus)}로 바뀌었습니다.`;
}

function renderQuickCompareOutcome(current: DemoOutcomeSummary, previous: DemoOutcomeSummary): string {
  const diffs = (["output", "control", "stability", "risk"] as const)
    .map((metric) => ({
      metric,
      diff: current[metric] - previous[metric]
    }))
    .filter((item) => Math.abs(item.diff) >= 0.03)
    .sort((left, right) => Math.abs(right.diff) - Math.abs(left.diff))
    .slice(0, 2);

  if (diffs.length === 0) {
    return "이번 결과감은 직전과 크게 다르지 않아 replay 없이도 같은 흐름으로 비교할 수 있습니다.";
  }

  return diffs
    .map((item, index) => {
      const direction = item.diff > 0 ? "더 큽니다" : "더 낮습니다";
      return `${index === 0 ? "이번 시도는" : "그리고"} ${outcomeMetricLabel(item.metric)}이 ${direction}`;
    })
    .join(" ")
    .concat(".");
}

function readCurrentFamily(result: RecognitionResult): string | null {
  return result.canonicalFamily ?? result.topCandidate?.family ?? null;
}

function statusLabel(
  status: RecognitionResult["status"] | "waiting" | "ready" | "authoring" | "compiled"
): string {
  switch (status) {
    case "recognized":
      return "확정";
    case "ambiguous":
      return "경합";
    case "incomplete":
      return "미완성";
    case "invalid":
      return "미인식";
    case "waiting":
      return "대기";
    case "ready":
      return "준비";
    case "authoring":
      return "작성 중";
    case "compiled":
      return "완료";
    default:
      return status;
  }
}

function renderSealThumbnail(strokes: Stroke[]): string {
  if (strokes.length === 0) {
    return `<div class="recent-thumb-empty">모양 없음</div>`;
  }

  const points = strokes.flatMap((stroke) => stroke.points);
  const xs = points.map((point) => point.x);
  const ys = points.map((point) => point.y);
  const minX = Math.min(...xs);
  const maxX = Math.max(...xs);
  const minY = Math.min(...ys);
  const maxY = Math.max(...ys);
  const width = Math.max(maxX - minX, 1);
  const height = Math.max(maxY - minY, 1);

  const polylines = strokes
    .filter((stroke) => stroke.points.length > 0)
    .map((stroke) => {
      const pointsAttr = stroke.points
        .map((point) => `${((point.x - minX) / width) * 88 + 4},${((point.y - minY) / height) * 88 + 4}`)
        .join(" ");
      return `<polyline points="${pointsAttr}" />`;
    })
    .join("");

  return `
    <svg viewBox="0 0 96 96" aria-hidden="true">
      ${polylines}
    </svg>
  `;
}

function qualityMetricLabel(key: keyof QualityVector): string {
  switch (key) {
    case "closure":
      return "닫힘";
    case "symmetry":
      return "균형";
    case "smoothness":
      return "매끄러움";
    case "tempo":
      return "속도";
    case "overshoot":
      return "흔들림";
    case "stability":
      return "안정감";
    case "rotationBias":
      return "기울기";
    default:
      return key;
  }
}

function formatClockTime(timestamp: number): string {
  return new Date(timestamp).toLocaleTimeString("ko-KR", {
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
    hour12: false
  });
}
