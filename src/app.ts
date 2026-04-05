import { compileSealResult } from "./recognizer/compile";
import { OVERLAY_OPERATOR_TEMPLATES, createOverlayReferenceFrame, recognizeOverlayStroke } from "./recognizer/overlay";
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
import {
  buildDemoOutcomeCompare,
  createRecentSealSnapshot,
  getChangedOutcomeMetrics
} from "./demo/outcome-summary";
import { buildExplainNotes } from "./demo/explain";
import { recognizeSession } from "./recognizer/recognize";
import type {
  AxisLine,
  CompiledSealResult,
  OverlayAnchorZoneId,
  OverlayOperator,
  OverlayRecognition,
  OverlayReferenceFrame,
  OverlayStrokeRecord,
  QualityVector,
  RecognitionLogEntry,
  RecognitionResult,
  RitualPhase,
  Stroke,
  StrokeSession,
  UserInputProfile,
  UserInputProfileDelta
} from "./recognizer/types";
import type {
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
const CANVAS_HEIGHT = 620;
const PROFILE_STORAGE_KEY = "magic-recognizer-v1_5-profile";
const RECENT_SEAL_LIMIT = 6;
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
    label: "closure leak",
    title: "closure leak",
    prompt: "끝을 일부러 닫지 않아 왜 미완성으로 남는지 이유 설명 패널에서 바로 보여줍니다.",
    narration: "도형 끝을 열어 둔 채 기본 모양 고정을 눌러 닫힘이 부족하면 미완성으로 남는 흐름을 보여주세요."
  },
  rotation_bias: {
    label: "rotation bias",
    title: "rotation bias",
    prompt: "같은 모양을 기울여 그린 뒤 분석 안내선과 이유 설명으로 위험도 변화만 함께 설명합니다.",
    narration: "같은 모양을 기울여 그린 뒤 분석 안내선을 켜고 기울기가 결과 위험도에만 개입하는 모습을 설명하세요."
  }
};

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
            <span class="promise-badge">same shape, same family</span>
            <span class="promise-badge subtle">quality affects execution, not family</span>
          </div>
          <div class="legend">
            <span>기본 모양: 바람 / 땅 / 불꽃 / 물 / 생명</span>
            <span>추가 효과 예시: 버팀 / 번개 / 얼음 막대</span>
            <span>추가 효과 예시: 혼 점 / 공백 절단 / 축선 장식</span>
          </div>
        </div>
      </header>
      <section class="demo-rail">
        <div class="demo-rail-card demo-controls-card demo-rail-wide">
          <div class="control-stack">
            <div>
              <p class="panel-label">View Preset</p>
              <div class="chip-row">
                <button id="preset-clean-button" class="chip-button">clean</button>
                <button id="preset-explain-button" class="chip-button">explain</button>
                <button id="preset-workshop-button" class="chip-button">workshop</button>
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
              <span id="preset-chip" class="status-chip status-ready">clean</span>
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
      <main class="workspace">
        <section class="board-panel">
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
        <aside class="sidebar">
          <section class="card">
            <p class="panel-label">기본 모양 판정</p>
            <h3 id="base-family">대기 중</h3>
            <p id="base-status" class="status-chip status-invalid">미인식</p>
            <p id="base-reason" class="card-copy">아직 기본 모양 입력이 없습니다.</p>
            <ol id="candidate-list" class="candidate-list"></ol>
          </section>
          <section class="card">
            <p class="panel-label">추가 효과 미리보기</p>
            <h3 id="overlay-preview-title">추가 효과 전</h3>
            <p id="overlay-preview-status" class="status-chip status-waiting">대기</p>
            <p id="overlay-preview-reason" class="card-copy">기본 모양을 고정한 뒤 추가 효과 그리기를 누르면 미리보기가 열립니다.</p>
            <div id="overlay-preview-meta" class="summary-grid"></div>
            <ol id="overlay-preview-candidates" class="candidate-list"></ol>
          </section>
          <section class="card">
            <p class="panel-label">추가 효과 기록</p>
            <h3 id="overlay-title">추가 효과 0개</h3>
            <p id="overlay-status" class="status-chip status-waiting">대기</p>
            <p id="overlay-reason" class="card-copy">추가 효과를 그릴 때마다 기록이 쌓입니다.</p>
            <ol id="overlay-list" class="candidate-list"></ol>
          </section>
          <section class="card">
            <p class="panel-label">최종 결과</p>
            <h3 id="compile-title">최종 결과 전</h3>
            <p id="compile-status" class="status-chip status-waiting">대기</p>
            <p id="compile-reason" class="card-copy">기본 모양과 추가 효과를 함께 묶어 최종 결과를 보여 줍니다.</p>
            <div id="compile-summary" class="summary-grid"></div>
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
              <span class="inline-guarantee">same shape, same family</span>
              <span class="inline-guarantee">quality affects execution, not family</span>
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
            <p class="panel-label">입력 습관 프로필</p>
            <h3 id="profile-samples">0회 누적</h3>
            <p id="profile-delta" class="card-copy">기본 모양을 확정할 때마다 입력 습관이 누적됩니다.</p>
            <div id="profile-baseline-list" class="quality-list"></div>
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
  const presetChip = select<HTMLSpanElement>(root, "#preset-chip");
  const scenarioTitle = select<HTMLElement>(root, "#scenario-title");
  const scenarioCopy = select<HTMLParagraphElement>(root, "#scenario-copy");
  const narrationCopy = select<HTMLParagraphElement>(root, "#narration-copy");
  const scenarioChipButtons = Array.from(root.querySelectorAll<HTMLButtonElement>("[data-scenario-id]"));
  const canvasHint = select<HTMLParagraphElement>(root, "#canvas-hint");
  const analysisCopy = select<HTMLParagraphElement>(root, "#analysis-copy");
  const analysisLegend = select<HTMLDivElement>(root, "#analysis-legend");
  const phaseTitle = select<HTMLElement>(root, "#phase-title");
  const phaseCopy = select<HTMLParagraphElement>(root, "#phase-copy");
  const phaseBase = select<HTMLElement>(root, "#phase-base");
  const phaseOverlay = select<HTMLElement>(root, "#phase-overlay");
  const phaseFinal = select<HTMLElement>(root, "#phase-final");
  const baseFamily = select<HTMLElement>(root, "#base-family");
  const baseStatus = select<HTMLElement>(root, "#base-status");
  const baseReason = select<HTMLElement>(root, "#base-reason");
  const candidateList = select<HTMLOListElement>(root, "#candidate-list");
  const overlayPreviewTitle = select<HTMLElement>(root, "#overlay-preview-title");
  const overlayPreviewStatus = select<HTMLElement>(root, "#overlay-preview-status");
  const overlayPreviewReason = select<HTMLElement>(root, "#overlay-preview-reason");
  const overlayPreviewMeta = select<HTMLDivElement>(root, "#overlay-preview-meta");
  const overlayPreviewCandidates = select<HTMLOListElement>(root, "#overlay-preview-candidates");
  const overlayTitle = select<HTMLElement>(root, "#overlay-title");
  const overlayStatus = select<HTMLElement>(root, "#overlay-status");
  const overlayReason = select<HTMLElement>(root, "#overlay-reason");
  const overlayList = select<HTMLOListElement>(root, "#overlay-list");
  const compileTitle = select<HTMLElement>(root, "#compile-title");
  const compileStatus = select<HTMLElement>(root, "#compile-status");
  const compileReason = select<HTMLElement>(root, "#compile-reason");
  const compileSummary = select<HTMLDivElement>(root, "#compile-summary");
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
  let demoView = resolvePresetView(createDemoViewState("clean"), "clean");
  let userProfile = loadUserInputProfile();
  let latestProfileDelta: UserInputProfileDelta | undefined;
  let baseSession = createEmptySession();
  let overlaySession = createEmptySession();
  let currentStroke: Stroke | null = null;
  let previewResult = recognizeSession(baseSession, { sealed: false, profile: userProfile });
  let baseSealResult: RecognitionResult | null = null;
  let currentOverlayPreview: OverlayRecognition | null = null;
  let overlayRecords: OverlayStrokeRecord[] = [];
  let compiledResult: CompiledSealResult | null = null;
  let logs: RecognitionLogEntry[] = [];
  let recentSealSnapshots: RecentSealSnapshot[] = [];

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
      previewResult = recognizeSession(baseSession, { sealed: false, profile: userProfile });
    } else {
      currentOverlayPreview = recognizeOverlayStroke(
        currentStroke,
        createOverlayContext(baseSession, overlayRecords, overlaySession)
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
      previewResult = recognizeSession(baseSession, { sealed: false, profile: userProfile });
    } else {
      currentOverlayPreview = recognizeOverlayStroke(
        currentStroke,
        createOverlayContext(baseSession, overlayRecords, overlaySession)
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
      previewResult = recognizeSession(baseSession, { sealed: false, profile: userProfile });
    } else {
      overlaySession.endedAt = Date.now();
      const recognition = recognizeOverlayStroke(
        finishedStroke,
        createOverlayContext(baseSession, overlayRecords, overlaySession)
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
      latestProfileDelta = undefined;
      overlayAuthoringStarted = false;
      overlaySession = createEmptySession();
      overlayRecords = [];
      currentOverlayPreview = null;
      compiledResult = null;
      previewResult = recognizeSession(baseSession, { sealed: false, profile: userProfile });
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

  presetCleanButton.addEventListener("click", () => {
    applyPreset("clean");
  });

  presetExplainButton.addEventListener("click", () => {
    applyPreset("explain");
  });

  presetWorkshopButton.addEventListener("click", () => {
    applyPreset("workshop");
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

  render();

  function sealBasePhase(): void {
    if (baseSession.strokes.length === 0) {
      return;
    }

    const sealed = recognizeSession(baseSession, { sealed: true, profile: userProfile });
    const snapshotId = crypto.randomUUID();
    const timestamp = Date.now();
    previewResult = sealed;
    baseSealResult = sealed;
    let profileUpdate: { profile: UserInputProfile; delta: UserInputProfileDelta } | undefined;

    if (sealed.canonicalFamily) {
      profileUpdate = updateUserInputProfile(userProfile, sealed);
      userProfile = profileUpdate.profile;
      latestProfileDelta = profileUpdate.delta;
      saveUserInputProfile(userProfile);
      phase = "base";
      overlayAuthoringStarted = false;
      overlaySession = createEmptySession();
      overlayRecords = [];
      currentOverlayPreview = null;
      compiledResult = null;
    }

    logs = [
      {
        kind: "base_seal",
        id: snapshotId,
        timestamp,
        rawStrokeCount: baseSession.strokes.length,
        rawStrokes: structuredClone(baseSession.strokes),
        normalizedStrokes: sealed.normalizedStrokes,
        result: sealed,
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
    previewResult = recognizeSession(baseSession, { sealed: false, profile: userProfile });
    baseSealResult = null;
    currentOverlayPreview = null;
    overlayRecords = [];
    compiledResult = null;
    latestProfileDelta = undefined;
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
      analysisOverlay: demoView.analysisOverlay
    });
    updateSidebar();
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
    presetChip.textContent = demoView.viewPreset;
    presetChip.className = `status-chip status-${demoView.viewPreset === "workshop" ? "authoring" : "ready"}`;
    scenarioTitle.textContent = scenario.title;
    scenarioCopy.textContent = scenario.prompt;
    narrationCopy.textContent = buildNarrationCopy(scenario.narration, phase, baseSealed, demoView.analysisOverlay);
    syncScenarioButtons(scenarioChipButtons, demoView.selectedScenarioId);

    outcomeCard.hidden = !demoView.compareMode;
    whyCard.hidden = !demoView.explainResult;
    qualityCard.hidden = !demoView.showQualitySplit;
    recentSealsCard.hidden = !demoView.showRecentSeals;
    profileCard.hidden = !demoView.showProfilePanel;
    logCard.hidden = !demoView.showLogViewer;

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

    profileSamples.textContent = `${userProfile.sampleCount}회 누적`;
    profileDelta.textContent = latestProfileDelta
      ? `누적 횟수 ${latestProfileDelta.previousSampleCount} -> ${latestProfileDelta.nextSampleCount} / 평균 시간 ${formatSignedMs(
          latestProfileDelta.averageDurationDeltaMs
        )} / 평균 길이 ${formatSigned(latestProfileDelta.averagePathLengthDelta, 1)}`
      : "기본 모양을 확정할 때마다 입력 습관이 조금씩 누적됩니다.";
    profileBaselineList.innerHTML = renderQuality(userProfile.averageQuality);
    profileDeltaList.innerHTML = latestProfileDelta ? renderProfileDelta(latestProfileDelta) : "";

    logCount.textContent = `${logs.length}건`;
    logViewer.textContent = JSON.stringify(logs, null, 2);
    canvasHint.textContent =
      !baseSealed
        ? "1. 기본 모양을 그린 뒤 먼저 종류를 고정합니다."
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
        showProfilePanel: false,
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
  overlaySession?: StrokeSession
): {
  referenceFrame: ReturnType<typeof createOverlayReferenceFrame>;
  existingOperators: OverlayOperator[];
  overlaySession?: StrokeSession;
} {
  return {
    referenceFrame: createOverlayReferenceFrame(baseSession),
    existingOperators: overlayRecords
      .map((record) => record.recognition.operator)
      .filter((operator): operator is OverlayOperator => Boolean(operator)),
    overlaySession
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

function saveUserInputProfile(profile: UserInputProfile): void {
  try {
    window.localStorage.setItem(PROFILE_STORAGE_KEY, JSON.stringify(profile));
  } catch {
    // Ignore storage failures and keep the in-memory profile alive.
  }
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
          <span style="width:${clamped * 100}%"></span>
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
      <span>raw avg</span>
      <strong>${(rawAverage * 100).toFixed(0)}</strong>
    </div>
    <div class="summary-row">
      <span>adjusted avg</span>
      <strong>${(adjustedAverage * 100).toFixed(0)}</strong>
    </div>
    <div class="summary-row">
      <span>delta</span>
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
            <span style="width:${value * 100}%"></span>
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
