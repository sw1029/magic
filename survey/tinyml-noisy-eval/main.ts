import {
  createDatacardRecognizerRegistry,
  createEmptyDatacardShapeCaptureStore,
  recognizeSessionWithDatacardRegistry,
  type DatacardRecognitionResult,
  type DatacardShapeId,
  type DatacardShapePreset,
  type DatacardTinyMlContrastDecision
} from "../../src/recognizer/datacard-shape-lab";
import type { PointSample, Stroke, StrokeSession } from "../../src/recognizer/types";

const API_BASE_URL = import.meta.env.VITE_SURVEY_API_URL ?? `${location.protocol}//${location.hostname}:4174`;
const SCHEMA_VERSION = "tinyml-noisy-eval-v1";
const STORAGE_KEY = "tinyml-noisy-eval-draft-v1";
const CANVAS_WIDTH = 1000;
const CANVAS_HEIGHT = 640;
const MAX_TRIALS = 500;
const TARGET_STROKE_COLORS = ["#5b67e8", "#0d9b85", "#d98200", "#a94ac2", "#ce3b46", "#4f7f12"];

type Topology = "closed" | "open" | "mixed";
type Risk = "low" | "med" | "high";
type NoiseRecipeId =
  | "stable"
  | "jitter"
  | "rotation_drift"
  | "scale_offset"
  | "open_gap"
  | "stroke_merge"
  | "point_dropout"
  | "fast_compression"
  | "personal_residual";

interface EvalPreset extends DatacardShapePreset {
  topology: Topology;
  risk: Risk;
}

interface ApiSession {
  sessionId: string;
  csrfToken: string;
  experimentGroup: string;
  hciProbeVariant: string;
}

interface NoiseSettings {
  jitterEnabled: boolean;
  jitterPx: number;
  rotationEnabled: boolean;
  rotationDegrees: number;
  scaleOffsetEnabled: boolean;
  scaleOffset: number;
  openGapEnabled: boolean;
  openGapPx: number;
  strokeMergeEnabled: boolean;
  strokeMergeRatio: number;
  pointDropoutEnabled: boolean;
  pointDropoutRate: number;
  fastCompressionEnabled: boolean;
  fastCompressionMs: number;
  personalResidualEnabled: boolean;
  personalResidual: number;
}

interface RecognitionSummary {
  selectedCandidateId: string;
  finalCandidateId: string;
  finalStatus: string;
  score: number;
  shadowConfidence: number;
  meaningConfidence: number;
  unsafeRisk: number;
  flipRisk: number;
  topCandidates: Array<{ id: string; label: string; score: number; status: string }>;
}

interface EvalTrial {
  trialId: string;
  targetPresetId: string;
  targetPattern: string;
  topology: Topology;
  noiseRecipe: {
    id: NoiseRecipeId;
    settings: Record<string, number | boolean>;
  };
  rawStrokes: Stroke[];
  noisyStrokes: Stroke[];
  rawRecognition: RecognitionSummary;
  noisyRecognition: RecognitionSummary;
  contrast: DatacardTinyMlContrastDecision;
  elapsedMs: number;
  pointerType: string;
  canvas: { width: number; height: number };
  userMarkedConfused: boolean;
  savedAtIso: string;
  saveState: "local" | "submitted";
}

interface AggregateMetrics {
  trialCount: number;
  precisionProxy: number;
  recallProxy: number;
  unsafeAcceptCount: number;
  priorityFlipCount: number;
  avgUnsafeRisk: number;
  avgFlipRisk: number;
  blockerCounts: Record<string, number>;
}

interface AppState {
  apiSession: ApiSession | null;
  apiError: string | null;
  participantId: string;
  selectedPresetId: DatacardShapeId;
  customPattern: string;
  customLabel: string;
  startedAtIso: string;
  currentStartedAtMs: number;
  rawStrokes: Stroke[];
  currentStroke: Stroke | null;
  currentPointerType: string;
  noise: NoiseSettings;
  userMarkedConfused: boolean;
  trials: EvalTrial[];
  notes: string;
  submitStatus: string;
}

interface CurrentAnalysis {
  rawStrokes: Stroke[];
  noisyStrokes: Stroke[];
  rawResult: DatacardRecognitionResult;
  noisyResult: DatacardRecognitionResult;
  rawSummary: RecognitionSummary;
  noisySummary: RecognitionSummary;
  elapsedMs: number;
  latencyMs: number;
}

const rootElement = document.querySelector<HTMLDivElement>("#tinyml-eval-app");

if (!rootElement) {
  throw new Error("tinyml eval root not found");
}

const root = rootElement;
let state = restoreDraft() ?? createInitialState();
let canvas: HTMLCanvasElement | null = null;
let ctx: CanvasRenderingContext2D | null = null;
let saveTimer = 0;

function createInitialState(): AppState {
  return {
    apiSession: null,
    apiError: null,
    participantId: "",
    selectedPresetId: "custom:eval_rect",
    customPattern: "star5|custom",
    customLabel: "custom",
    startedAtIso: new Date().toISOString(),
    currentStartedAtMs: performance.now(),
    rawStrokes: [],
    currentStroke: null,
    currentPointerType: "unknown",
    noise: {
      jitterEnabled: true,
      jitterPx: 1.5,
      rotationEnabled: true,
      rotationDegrees: 0.6,
      scaleOffsetEnabled: false,
      scaleOffset: 0.04,
      openGapEnabled: true,
      openGapPx: 2,
      strokeMergeEnabled: false,
      strokeMergeRatio: 0.12,
      pointDropoutEnabled: false,
      pointDropoutRate: 0.08,
      fastCompressionEnabled: false,
      fastCompressionMs: 15,
      personalResidualEnabled: true,
      personalResidual: 0.8
    },
    userMarkedConfused: false,
    trials: [],
    notes: "",
    submitStatus: "API session pending"
  };
}

async function initializeApiSession(): Promise<void> {
  try {
    const response = await fetch(`${API_BASE_URL}/api/survey-session`, {
      credentials: "include",
      headers: { Accept: "application/json" }
    });

    if (!response.ok) {
      throw new Error(`session ${response.status}`);
    }

    state.apiSession = (await response.json()) as ApiSession;
    state.apiError = null;
    state.submitStatus = "API ready";
  } catch (error) {
    state.apiSession = null;
    state.apiError = error instanceof Error ? error.message : "API unavailable";
    state.submitStatus = "download fallback only";
  }

  persistDraft();
  render();
}

function render(): void {
  root.textContent = "";
  root.append(createShell());
  canvas = root.querySelector<HTMLCanvasElement>("#eval-canvas");
  ctx = canvas?.getContext("2d") ?? null;

  if (canvas && ctx) {
    wireCanvas(canvas);
    drawCanvas();
  }
}

function createShell(): HTMLElement {
  const shell = el("div", "app-shell");
  shell.append(createTopbar(), createMainGrid(), createTrialQueue());
  return shell;
}

function createTopbar(): HTMLElement {
  const topbar = el("header", "topbar");
  const brand = el("div", "brand");
  brand.append(el("span", "brand-mark", "ML"), el("h1", "", "TinyML Noisy Input Eval"));

  const meta = el("div", "top-meta");
  meta.append(
    metaItem("세션", state.apiSession?.sessionId.slice(0, 13) ?? localSessionId()),
    metaItem("모델", "contrast-v1"),
    metaItem("디바이스", navigator.platform || "browser"),
    metaItem("펌웨어", "web-eval")
  );

  const save = el("span", "meta-item");
  save.append(el("span", "save-dot"), document.createTextNode(state.submitStatus));

  const actions = el("div", "top-actions");
  actions.append(
    button("JSON 저장", downloadJson),
    button("CSV 내보내기", downloadCsv),
    button("세션 제출", submitSession, state.trials.length === 0 || !state.apiSession, "primary-button")
  );

  topbar.append(brand, meta, save, actions);
  return topbar;
}

function createMainGrid(): HTMLElement {
  const grid = el("main", "main-grid");
  grid.append(createShapePanel(), createWorkspace(), createMetricsPanel());
  return grid;
}

function createShapePanel(): HTMLElement {
  const panel = el("aside", "panel");
  const header = el("div", "panel-header");
  header.append(el("h2", "", "Regex Shape Set"));
  const actions = el("div", "small-actions");
  actions.append(button("+", selectCustomPreset, false, "icon-button"), button("설정", focusCustomPattern, false, "icon-button"));
  header.append(actions);

  const table = el("div", "shape-table");
  const head = el("div", "shape-head");
  head.append(el("span", "", ""), el("span", "", "이름"), el("span", "", "Topology"), el("span", "", "Risk"));
  table.append(head);

  getEvalPresets().forEach((preset, index) => {
    const row = el("button", ["shape-row", preset.id === state.selectedPresetId ? "active" : ""]);
    row.type = "button";
    row.addEventListener("click", () => selectPreset(preset.id));
    row.append(
      el("span", "shape-index", String(index + 1).padStart(2, "0")),
      createShapeName(preset),
      el("span", ["topology", preset.topology], preset.topology),
      el("span", ["risk", preset.risk], preset.risk.toUpperCase())
    );
    table.append(row);
  });

  panel.append(header, table, createCustomEditor(), createLeftFooter());
  return panel;
}

function createShapeName(preset: EvalPreset): HTMLElement {
  const wrap = el("span", "shape-name");
  wrap.append(miniShape(preset), el("span", "", ""));
  const text = wrap.lastElementChild as HTMLElement;
  text.append(el("strong", "", preset.shortLabel), el("code", "", preset.definition.pattern));
  return wrap;
}

function createCustomEditor(): HTMLElement {
  const editor = el("section", "custom-editor");
  editor.append(
    el("p", "block-label", "Custom regex"),
    field("Label", input("custom-label", state.customLabel, (value) => {
      state.customLabel = value.slice(0, 40) || "custom";
      persistDraft();
      render();
    })),
    field("Pattern", input("custom-pattern", state.customPattern, (value) => {
      state.customPattern = value.slice(0, 140) || "custom";
      persistDraft();
      render();
    }))
  );
  return editor;
}

function createLeftFooter(): HTMLElement {
  const footer = el("div", "left-footer");
  footer.append(
    el("span", "muted", `총 ${getEvalPresets().length}개 도형`),
    el("span", "muted", "closed=폐쇄, open=개방, mixed=조합"),
    field("Participant ID", input("participant-id", state.participantId, (value) => {
      state.participantId = value.trim().slice(0, 64);
      persistDraft();
    }))
  );
  return footer;
}

function createWorkspace(): HTMLElement {
  const workspace = el("section", "workspace");
  workspace.append(createToolbar(), createCanvasCard(), createNoisePanel());
  return workspace;
}

function createToolbar(): HTMLElement {
  const toolbar = el("div", "toolbar");
  const tools = el("div", "tool-strip");
  tools.append(
    button("펜", undefined, false, "tool-button active"),
    button("지우개", resetCurrentInput, state.rawStrokes.length === 0, "tool-button"),
    button("타겟 오버레이", drawCanvas, false, "tool-button")
  );

  const actions = el("div", "action-strip");
  actions.append(
    labelCheckbox("혼동됨", state.userMarkedConfused, (checked) => {
      state.userMarkedConfused = checked;
      persistDraft();
      render();
    }),
    button("입력 초기화", resetCurrentInput, state.rawStrokes.length === 0),
    button("Trial 저장", saveCurrentTrial, state.rawStrokes.length === 0, "primary-button")
  );

  toolbar.append(tools, actions);
  return toolbar;
}

function createCanvasCard(): HTMLElement {
  const card = el("div", "canvas-card");
  const c = document.createElement("canvas");
  c.id = "eval-canvas";
  c.width = CANVAS_WIDTH;
  c.height = CANVAS_HEIGHT;
  c.setAttribute("aria-label", "tinyml noisy input drawing canvas");
  card.append(c);
  return card;
}

function createNoisePanel(): HTMLElement {
  const panel = el("section", "noise-panel");
  panel.append(
    noiseControl("지터", state.noise.jitterEnabled, state.noise.jitterPx, 0, 5, 0.1, "px", (enabled, value) => {
      state.noise.jitterEnabled = enabled;
      state.noise.jitterPx = value;
    }),
    noiseControl("회전 드리프트", state.noise.rotationEnabled, state.noise.rotationDegrees, 0, 5, 0.1, "deg", (enabled, value) => {
      state.noise.rotationEnabled = enabled;
      state.noise.rotationDegrees = value;
    }),
    noiseControl("스케일/오프셋", state.noise.scaleOffsetEnabled, state.noise.scaleOffset, 0, 0.2, 0.01, "", (enabled, value) => {
      state.noise.scaleOffsetEnabled = enabled;
      state.noise.scaleOffset = value;
    }),
    noiseControl("오픈 갭", state.noise.openGapEnabled, state.noise.openGapPx, 0, 12, 0.5, "px", (enabled, value) => {
      state.noise.openGapEnabled = enabled;
      state.noise.openGapPx = value;
    }),
    noiseControl("스트로크 병합", state.noise.strokeMergeEnabled, state.noise.strokeMergeRatio, 0, 1, 0.01, "", (enabled, value) => {
      state.noise.strokeMergeEnabled = enabled;
      state.noise.strokeMergeRatio = value;
    }),
    noiseControl("포인트 드롭", state.noise.pointDropoutEnabled, state.noise.pointDropoutRate, 0, 0.5, 0.01, "", (enabled, value) => {
      state.noise.pointDropoutEnabled = enabled;
      state.noise.pointDropoutRate = value;
    }),
    noiseControl("빠른 압축", state.noise.fastCompressionEnabled, state.noise.fastCompressionMs, 5, 100, 1, "ms", (enabled, value) => {
      state.noise.fastCompressionEnabled = enabled;
      state.noise.fastCompressionMs = value;
    }),
    noiseControl("개인 잔차", state.noise.personalResidualEnabled, state.noise.personalResidual, 0, 2, 0.05, "", (enabled, value) => {
      state.noise.personalResidualEnabled = enabled;
      state.noise.personalResidual = value;
    })
  );
  return panel;
}

function createMetricsPanel(): HTMLElement {
  const panel = el("aside", "panel metrics-panel");
  const header = el("div", "panel-header");
  header.append(el("h2", "", "실시간 성능"), el("span", "live", "LIVE"));
  panel.append(header);

  const aggregate = calculateAggregate();
  const analysis = analyzeCurrentInput();
  const noisySummary = analysis?.noisySummary;
  const contrast = analysis?.noisyResult.contrast;
  const confusion = resolveConfusionPair(analysis);

  const grid = el("div", "metric-grid");
  grid.append(
    metricCard("Precision Proxy", aggregate.precisionProxy, "precision"),
    metricCard("Recall Proxy", aggregate.recallProxy, "recall"),
    metricCard("Unsafe Risk", noisySummary?.unsafeRisk ?? aggregate.avgUnsafeRisk, "unsafe"),
    metricCard("Flip Risk", noisySummary?.flipRisk ?? aggregate.avgFlipRisk, "flip")
  );

  panel.append(createTargetPreviewPanel(selectedPreset()), grid, createPairCard(confusion), createDecisionCard(contrast, noisySummary), createBlockerCard(contrast), createNotes());
  return panel;
}

function createPairCard(pair: { label: string; probability: number }): HTMLElement {
  const card = el("section", "decision-card");
  card.append(el("p", "block-label", "혼동 Top Pair"));
  const row = el("div", "pair-row");
  row.append(el("strong", "", pair.label), el("span", "", pair.probability.toFixed(2)));
  card.append(row);
  return card;
}

function createDecisionCard(contrast?: DatacardTinyMlContrastDecision, summary?: RecognitionSummary): HTMLElement {
  const card = el("section", "decision-card");
  card.append(el("p", "block-label", "Shadow vs Meaning"));
  const versus = el("div", "versus");
  versus.append(
    decisionBox("Shadow Top", contrast?.shadow.candidateId ?? "-", contrast?.shadow.confidence ?? 0),
    el("strong", "", "vs"),
    decisionBox("Meaning Top", contrast?.meaning.candidateId ?? summary?.finalCandidateId ?? "-", contrast?.meaning.confidence ?? 0)
  );
  card.append(versus);
  card.append(el("span", "match-pill", contrast?.role ?? "no input"));
  return card;
}

function createBlockerCard(contrast?: DatacardTinyMlContrastDecision): HTMLElement {
  const card = el("section", "blocker-card");
  card.append(el("p", "block-label", "블로커"));
  const list = el("div", "blocker-list");
  const blockers = contrast?.blockedBy.length ? contrast.blockedBy : ["none"];

  for (const blocker of blockers) {
    const item = el("div", ["blocker-item", blocker === "none" ? "" : blocker === "repetition" || blocker === "closure" ? "danger" : "warn"]);
    item.append(el("strong", "", blocker), el("span", "", blockerDescription(blocker)));
    list.append(item);
  }

  card.append(list);
  return card;
}

function createNotes(): HTMLElement {
  const wrap = el("section", "session-notes");
  wrap.append(el("p", "block-label", "평가 메모"));
  const textarea = document.createElement("textarea");
  textarea.value = state.notes;
  textarea.placeholder = "혼동된 이유, 손 떨림, 의도한 도형 등을 기록";
  textarea.addEventListener("input", () => {
    state.notes = textarea.value.slice(0, 1200);
    persistDraft();
  });
  wrap.append(textarea);
  return wrap;
}

function createTrialQueue(): HTMLElement {
  const card = el("section", "queue-card");
  const header = el("div", "queue-header");
  const title = el("div", "queue-meta");
  title.append(el("h2", "", "Trial Queue"), el("span", "count-pill", `${state.trials.length} / ${MAX_TRIALS}`));
  const actions = el("div", "queue-actions");
  actions.append(
    button("마지막 삭제", removeLastTrial, state.trials.length === 0),
    button("오입력 정리", clearTrialQueue, state.trials.length === 0, "danger-button"),
    button("JSON 저장", downloadJson, state.trials.length === 0),
    button("CSV 내보내기", downloadCsv, state.trials.length === 0),
    button("컬럼 설정", undefined)
  );
  header.append(title, actions);

  const table = document.createElement("table");
  table.className = "queue-table";
  table.append(createQueueHead(), createQueueBody());
  const footer = el("div", "footer-line");
  footer.append(
    el("span", "", "평가 기준: target regex를 GT로 둔 proxy metric"),
    el("span", "", state.apiError ? `API: ${state.apiError}` : "API + local export ready")
  );

  card.append(header, table, footer);
  return card;
}

function createQueueHead(): HTMLElement {
  const thead = document.createElement("thead");
  const row = document.createElement("tr");
  ["#", "Block", "Expected Regex", "Actual Top", "Shadow Top", "Meaning Class", "Risk", "Elapsed", "Save / Clean"].forEach((label) => {
    const th = document.createElement("th");
    th.textContent = label;
    row.append(th);
  });
  thead.append(row);
  return thead;
}

function createQueueBody(): HTMLElement {
  const tbody = document.createElement("tbody");
  const rows = state.trials.slice(-8);

  if (rows.length === 0) {
    const row = document.createElement("tr");
    const td = document.createElement("td");
    td.colSpan = 9;
    td.className = "muted";
    td.textContent = "아직 저장된 trial이 없습니다. 캔버스에 입력 후 Trial 저장을 누르세요.";
    row.append(td);
    tbody.append(row);
    return tbody;
  }

  rows.forEach((trial, index) => {
    const row = document.createElement("tr");
    if (index === rows.length - 1) {
      row.className = "active";
    }
    const cells = [
      String(state.trials.indexOf(trial) + 1).padStart(3, "0"),
      trial.noiseRecipe.id,
      trial.targetPattern,
      `${trial.noisyRecognition.finalCandidateId} (${trial.noisyRecognition.finalStatus})`,
      `${trial.contrast.shadow.candidateId} (${trial.contrast.shadow.confidence.toFixed(3)})`,
      trial.contrast.meaning.correctionClass,
      `U ${trial.noisyRecognition.unsafeRisk.toFixed(2)} / F ${trial.noisyRecognition.flipRisk.toFixed(2)}`,
      `${trial.elapsedMs} ms`
    ];

    for (const cell of cells) {
      const td = document.createElement("td");
      td.textContent = cell;
      row.append(td);
    }

    const actionCell = document.createElement("td");
    const rowActions = el("div", "queue-row-actions");
    rowActions.append(el("span", "status-pill", trial.saveState), button("삭제", () => removeTrial(trial.trialId), false, "queue-delete-button"));
    actionCell.append(rowActions);
    row.append(actionCell);

    tbody.append(row);
  });

  return tbody;
}

function removeTrial(trialId: string): void {
  const beforeCount = state.trials.length;
  state.trials = state.trials.filter((trial) => trial.trialId !== trialId);

  if (state.trials.length !== beforeCount) {
    state.submitStatus = "trial removed from local queue";
    persistDraft();
    render();
  }
}

function removeLastTrial(): void {
  const last = state.trials[state.trials.length - 1];

  if (!last) {
    return;
  }

  removeTrial(last.trialId);
}

function clearTrialQueue(): void {
  if (state.trials.length === 0) {
    return;
  }

  if (!window.confirm("현재 Trial Queue의 local trial을 모두 정리할까요? API에 이미 저장된 파일은 삭제하지 않습니다.")) {
    return;
  }

  state.trials = [];
  state.submitStatus = "trial queue cleared";
  persistDraft();
  render();
}

function wireCanvas(target: HTMLCanvasElement): void {
  target.addEventListener("pointerdown", (event) => {
    event.preventDefault();
    target.setPointerCapture(event.pointerId);
    const point = canvasPoint(event);
    state.currentPointerType = event.pointerType || "unknown";
    state.currentStartedAtMs = state.rawStrokes.length === 0 ? performance.now() : state.currentStartedAtMs;
    state.currentStroke = {
      id: `stroke_${compactId()}`,
      points: [samplePoint(point, event)]
    };
    drawCanvas();
  });

  target.addEventListener("pointermove", (event) => {
    if (!state.currentStroke) {
      return;
    }

    event.preventDefault();
    state.currentStroke.points.push(samplePoint(canvasPoint(event), event));
    drawCanvas();
  });

  const stop = (event: PointerEvent) => {
    if (!state.currentStroke) {
      return;
    }

    event.preventDefault();
    const point = samplePoint(canvasPoint(event), event);
    const last = state.currentStroke.points[state.currentStroke.points.length - 1];

    if (!last || Math.hypot(last.x - point.x, last.y - point.y) > 1) {
      state.currentStroke.points.push(point);
    }

    state.rawStrokes = [...state.rawStrokes, cloneStroke(state.currentStroke)];
    state.currentStroke = null;
    persistDraft();
    render();
  };

  target.addEventListener("pointerup", stop);
  target.addEventListener("pointercancel", stop);
}

function drawCanvas(): void {
  if (!ctx) {
    return;
  }

  ctx.clearRect(0, 0, CANVAS_WIDTH, CANVAS_HEIGHT);
  drawGrid();
  drawTargetOverlay(selectedPreset());
  const analysis = analyzeCurrentInput();
  const noisy = analysis?.noisyStrokes ?? [];
  drawStrokes(noisy, "#e28a05", 0.5, 3, [8, 8]);
  drawStrokes([...state.rawStrokes, ...(state.currentStroke ? [state.currentStroke] : [])], "#0f7c8e", 0.95, 3.4);
  drawCanvasHud(analysis);
}

function drawGrid(): void {
  if (!ctx) {
    return;
  }

  ctx.save();
  ctx.fillStyle = "#ffffff";
  ctx.fillRect(0, 0, CANVAS_WIDTH, CANVAS_HEIGHT);
  ctx.strokeStyle = "#eef3f7";
  ctx.lineWidth = 1;

  for (let x = 0; x <= CANVAS_WIDTH; x += 40) {
    ctx.beginPath();
    ctx.moveTo(x, 0);
    ctx.lineTo(x, CANVAS_HEIGHT);
    ctx.stroke();
  }

  for (let y = 0; y <= CANVAS_HEIGHT; y += 40) {
    ctx.beginPath();
    ctx.moveTo(0, y);
    ctx.lineTo(CANVAS_WIDTH, y);
    ctx.stroke();
  }

  ctx.strokeStyle = "#dfe7ee";
  ctx.setLineDash([6, 8]);
  ctx.beginPath();
  ctx.moveTo(CANVAS_WIDTH / 2, 0);
  ctx.lineTo(CANVAS_WIDTH / 2, CANVAS_HEIGHT);
  ctx.moveTo(0, CANVAS_HEIGHT / 2);
  ctx.lineTo(CANVAS_WIDTH, CANVAS_HEIGHT / 2);
  ctx.stroke();
  ctx.restore();
}

function drawTargetOverlay(preset: EvalPreset): void {
  if (!ctx) {
    return;
  }

  const context = ctx;
  context.save();
  context.fillStyle = "rgba(19,32,43,0.03)";
  context.lineWidth = 2.2;
  context.setLineDash([7, 7]);

  preset.definition.exampleTemplate.forEach((stroke, index) => {
    const points = stroke.points.map(templateToCanvasPoint);
    context.strokeStyle = targetStrokeColor(index);
    context.globalAlpha = 0.52;
    drawPath(points, false);

    const anchor = points[0];
    if (anchor) {
      context.globalAlpha = 0.86;
      context.setLineDash([]);
      context.fillStyle = targetStrokeColor(index);
      context.beginPath();
      context.arc(anchor.x, anchor.y, 8, 0, Math.PI * 2);
      context.fill();
      context.fillStyle = "#ffffff";
      context.font = "700 10px Segoe UI, Arial";
      context.textAlign = "center";
      context.textBaseline = "middle";
      context.fillText(String(index + 1), anchor.x, anchor.y + 0.5);
      context.setLineDash([7, 7]);
    }
  });

  context.restore();
}

function drawStrokes(strokes: readonly Stroke[], color: string, alpha: number, width: number, dash: number[] = []): void {
  if (!ctx) {
    return;
  }

  ctx.save();
  ctx.strokeStyle = color;
  ctx.lineWidth = width;
  ctx.lineCap = "round";
  ctx.lineJoin = "round";
  ctx.globalAlpha = alpha;
  ctx.setLineDash(dash);

  for (const stroke of strokes) {
    drawPath(stroke.points, false);
  }

  ctx.restore();
}

function drawPath(points: readonly PointSample[], close: boolean): void {
  if (!ctx || points.length === 0) {
    return;
  }

  ctx.beginPath();
  ctx.moveTo(points[0].x, points[0].y);

  for (const point of points.slice(1)) {
    ctx.lineTo(point.x, point.y);
  }

  if (close) {
    ctx.closePath();
  }

  ctx.stroke();
}

function drawCanvasHud(analysis: CurrentAnalysis | null): void {
  if (!ctx) {
    return;
  }

  const preset = selectedPreset();
  ctx.save();
  ctx.fillStyle = "rgba(255,255,255,0.88)";
  ctx.strokeStyle = "#d8e1ea";
  roundRect(ctx, 710, 548, 264, 66, 8);
  ctx.fill();
  ctx.stroke();
  ctx.fillStyle = "#13202b";
  ctx.font = "700 14px Segoe UI, Arial";
  ctx.fillText(`Target: ${preset.shortLabel}`, 728, 575);
  ctx.fillStyle = "#667484";
  ctx.font = "12px Segoe UI, Arial";
  ctx.fillText(`Noisy: ${analysis?.noisySummary.finalCandidateId ?? "-"} / ${analysis?.noisySummary.finalStatus ?? "-"}`, 728, 596);
  ctx.restore();
}

function samplePoint(point: { x: number; y: number }, event: PointerEvent): PointSample {
  return {
    x: round(point.x, 2),
    y: round(point.y, 2),
    t: Math.max(0, Math.round(performance.now() - state.currentStartedAtMs)),
    pressure: event.pressure || 0.5
  };
}

function canvasPoint(event: PointerEvent): { x: number; y: number } {
  const rect = (event.currentTarget as HTMLCanvasElement).getBoundingClientRect();
  return {
    x: clamp(((event.clientX - rect.left) / rect.width) * CANVAS_WIDTH, 0, CANVAS_WIDTH),
    y: clamp(((event.clientY - rect.top) / rect.height) * CANVAS_HEIGHT, 0, CANVAS_HEIGHT)
  };
}

function analyzeCurrentInput(): CurrentAnalysis | null {
  const rawStrokes = [...state.rawStrokes, ...(state.currentStroke ? [state.currentStroke] : [])]
    .map(cloneStroke)
    .filter((stroke) => stroke.points.length > 0);

  if (rawStrokes.length === 0) {
    return null;
  }

  const start = performance.now();
  const noisyStrokes = injectNoise(rawStrokes, state.noise, selectedPreset());
  const rawResult = recognize(rawStrokes);
  const noisyResult = recognize(noisyStrokes);
  const latencyMs = performance.now() - start;

  return {
    rawStrokes,
    noisyStrokes,
    rawResult,
    noisyResult,
    rawSummary: summarizeRecognition(rawResult),
    noisySummary: summarizeRecognition(noisyResult),
    elapsedMs: Math.max(0, Math.round(performance.now() - state.currentStartedAtMs)),
    latencyMs: round(latencyMs, 2)
  };
}

function recognize(strokes: readonly Stroke[]): DatacardRecognitionResult {
  const registry = createDatacardRecognizerRegistry(getEvalPresets(), createEmptyDatacardShapeCaptureStore(1), {
    activate: true,
    qaPassed: true,
    builtInConfusionLimit: 1,
    now: 1
  });
  const session: StrokeSession = {
    startedAt: 1,
    endedAt: 2,
    strokes: strokes.map(cloneStroke)
  };
  return recognizeSessionWithDatacardRegistry(session, registry, { selectedPresetId: state.selectedPresetId });
}

function summarizeRecognition(result: DatacardRecognitionResult): RecognitionSummary {
  const selected = result.selectedCandidate;
  const contrast = result.contrast;
  return {
    selectedCandidateId: selected.id,
    finalCandidateId: contrast?.finalCandidateId ?? selected.id,
    finalStatus: contrast?.finalStatus ?? selected.status,
    score: selected.score,
    shadowConfidence: contrast?.shadow.confidence ?? selected.contrastScore ?? 0,
    meaningConfidence: contrast?.meaning.confidence ?? selected.meaningScore ?? 0,
    unsafeRisk: contrast?.shadow.unsafeRisk ?? selected.shadowRisk ?? 0,
    flipRisk: contrast?.shadow.flipRisk ?? 0,
    topCandidates: result.candidates.slice(0, 5).map((candidate) => ({
      id: candidate.id,
      label: candidate.label,
      score: candidate.score,
      status: candidate.status
    }))
  };
}

function saveCurrentTrial(): void {
  const analysis = analyzeCurrentInput();

  if (!analysis || !analysis.noisyResult.contrast || state.trials.length >= MAX_TRIALS) {
    return;
  }

  const preset = selectedPreset();
  const trial: EvalTrial = {
    trialId: `trial_${compactId()}`,
    targetPresetId: preset.id,
    targetPattern: preset.definition.pattern,
    topology: preset.topology,
    noiseRecipe: {
      id: primaryNoiseRecipeId(),
      settings: serializeNoiseSettings()
    },
    rawStrokes: analysis.rawStrokes.map(cloneStroke),
    noisyStrokes: analysis.noisyStrokes.map(cloneStroke),
    rawRecognition: analysis.rawSummary,
    noisyRecognition: analysis.noisySummary,
    contrast: analysis.noisyResult.contrast,
    elapsedMs: analysis.elapsedMs,
    pointerType: state.currentPointerType,
    canvas: { width: CANVAS_WIDTH, height: CANVAS_HEIGHT },
    userMarkedConfused: state.userMarkedConfused,
    savedAtIso: new Date().toISOString(),
    saveState: "local"
  };

  state.trials = [...state.trials, trial];
  state.rawStrokes = [];
  state.currentStroke = null;
  state.userMarkedConfused = false;
  state.currentStartedAtMs = performance.now();
  state.submitStatus = "local changes";
  persistDraft();
  render();
}

function submitSession(): void {
  void (async () => {
    if (!state.apiSession || state.trials.length === 0) {
      return;
    }

    const payload = buildPayload();
    state.submitStatus = "submitting";
    render();

    try {
      const response = await fetch(`${API_BASE_URL}/api/tinyml-noisy-eval-responses`, {
        method: "POST",
        credentials: "include",
        headers: {
          "Content-Type": "application/json",
          "X-CSRF-Token": state.apiSession.csrfToken
        },
        body: JSON.stringify(payload)
      });

      if (!response.ok) {
        const body = await response.json().catch(() => ({}));
        throw new Error(`${response.status} ${JSON.stringify(body)}`);
      }

      state.trials = state.trials.map((trial) => ({ ...trial, saveState: "submitted" }));
      state.submitStatus = "submitted";
    } catch (error) {
      state.submitStatus = error instanceof Error ? `submit failed: ${error.message}` : "submit failed";
    }

    persistDraft();
    render();
  })();
}

function buildPayload(): Record<string, unknown> {
  return {
    schemaVersion: SCHEMA_VERSION,
    submissionId: `tinyml_${compactId()}`,
    sessionId: state.apiSession?.sessionId ?? localSessionId(),
    participantId: state.participantId || undefined,
    consentAccepted: true,
    startedAtIso: state.startedAtIso,
    completedAtIso: new Date().toISOString(),
    userAgent: navigator.userAgent,
    locale: navigator.language,
    timezone: Intl.DateTimeFormat().resolvedOptions().timeZone,
    notes: state.notes,
    canvas: { width: CANVAS_WIDTH, height: CANVAS_HEIGHT },
    trialPlan: buildTrialPlan(),
    trials: state.trials.map(({ saveState: _saveState, ...trial }) => trial),
    aggregate: calculateAggregate()
  };
}

function buildTrialPlan(): Array<{ id: string; label: string; targetPresetId: string; noiseRecipeId: string }> {
  return getEvalPresets().flatMap((preset) =>
    ["stable", "jitter", "stroke_merge", "open_gap", "personal_residual"].map((noiseRecipeId, index) => ({
      id: `${preset.id.replace(/[^a-z0-9_-]/gi, "_")}_${noiseRecipeId}_${index + 1}`,
      label: `${preset.shortLabel} / ${noiseRecipeId}`,
      targetPresetId: preset.id,
      noiseRecipeId
    }))
  );
}

function downloadJson(): void {
  if (state.trials.length === 0) {
    return;
  }

  downloadBlob(`${localSessionId()}_tinyml-noisy-eval.json`, JSON.stringify(buildPayload(), null, 2), "application/json");
}

function downloadCsv(): void {
  if (state.trials.length === 0) {
    return;
  }

  const rows = [
    [
      "trialId",
      "targetPresetId",
      "targetPattern",
      "topology",
      "noiseRecipe",
      "rawTop",
      "noisyTop",
      "finalStatus",
      "shadowTop",
      "meaningClass",
      "unsafeRisk",
      "flipRisk",
      "elapsedMs",
      "userMarkedConfused",
      "saveState"
    ],
    ...state.trials.map((trial) => [
      trial.trialId,
      trial.targetPresetId,
      trial.targetPattern,
      trial.topology,
      trial.noiseRecipe.id,
      trial.rawRecognition.finalCandidateId,
      trial.noisyRecognition.finalCandidateId,
      trial.noisyRecognition.finalStatus,
      trial.contrast.shadow.candidateId,
      trial.contrast.meaning.correctionClass,
      trial.noisyRecognition.unsafeRisk,
      trial.noisyRecognition.flipRisk,
      trial.elapsedMs,
      trial.userMarkedConfused,
      trial.saveState
    ])
  ];

  downloadBlob(`${localSessionId()}_tinyml-noisy-eval-summary.csv`, csvText(rows), "text/csv;charset=utf-8");
}

function calculateAggregate(): AggregateMetrics {
  const trials = state.trials;
  const accepted = trials.filter((trial) => trial.noisyRecognition.finalStatus === "recognized");
  const correctAccepted = accepted.filter((trial) => trial.noisyRecognition.finalCandidateId === trial.targetPresetId);
  const unsafeAcceptCount = accepted.length - correctAccepted.length;
  const priorityFlipCount = trials.filter(
    (trial) => trial.rawRecognition.finalCandidateId !== trial.noisyRecognition.finalCandidateId
  ).length;
  const blockerCounts: Record<string, number> = {};

  for (const trial of trials) {
    const blockers = trial.contrast.blockedBy.length > 0 ? trial.contrast.blockedBy : ["none"];

    for (const blocker of blockers) {
      blockerCounts[blocker] = (blockerCounts[blocker] ?? 0) + 1;
    }
  }

  return {
    trialCount: trials.length,
    precisionProxy: round(accepted.length === 0 ? 0 : correctAccepted.length / accepted.length, 4),
    recallProxy: round(trials.length === 0 ? 0 : correctAccepted.length / trials.length, 4),
    unsafeAcceptCount,
    priorityFlipCount,
    avgUnsafeRisk: round(average(trials.map((trial) => trial.noisyRecognition.unsafeRisk)), 4),
    avgFlipRisk: round(average(trials.map((trial) => trial.noisyRecognition.flipRisk)), 4),
    blockerCounts
  };
}

function injectNoise(strokes: readonly Stroke[], settings: NoiseSettings, preset: EvalPreset): Stroke[] {
  let output = strokes.map(cloneStroke);
  output = output.map((stroke, strokeIndex) => ({
    ...stroke,
    points: stroke.points.map((point, pointIndex) => transformPoint(point, strokeIndex, pointIndex, settings))
  }));

  if (settings.openGapEnabled) {
    output = applyOpenGap(output, settings.openGapPx, preset.topology);
  }

  if (settings.pointDropoutEnabled) {
    output = applyPointDropout(output, settings.pointDropoutRate);
  }

  if (settings.strokeMergeEnabled) {
    output = applyStrokeMerge(output, settings.strokeMergeRatio);
  }

  if (settings.fastCompressionEnabled) {
    output = output.map((stroke) => ({
      ...stroke,
      points: stroke.points.map((point, index) => ({ ...point, t: index * settings.fastCompressionMs }))
    }));
  }

  return output.map((stroke) => ({
    ...stroke,
    points: stroke.points.map((point) => ({
      ...point,
      x: round(point.x, 2),
      y: round(point.y, 2),
      t: Math.max(0, Math.round(point.t))
    }))
  }));
}

function transformPoint(point: PointSample, strokeIndex: number, pointIndex: number, settings: NoiseSettings): PointSample {
  const center = { x: CANVAS_WIDTH / 2, y: CANVAS_HEIGHT / 2 };
  let x = point.x;
  let y = point.y;

  if (settings.jitterEnabled) {
    x += Math.sin(pointIndex * 12.989 + strokeIndex * 78.23) * settings.jitterPx;
    y += Math.cos(pointIndex * 4.133 + strokeIndex * 31.17) * settings.jitterPx;
  }

  if (settings.rotationEnabled) {
    const angle = (settings.rotationDegrees * Math.PI) / 180;
    const dx = x - center.x;
    const dy = y - center.y;
    x = center.x + dx * Math.cos(angle) - dy * Math.sin(angle);
    y = center.y + dx * Math.sin(angle) + dy * Math.cos(angle);
  }

  if (settings.scaleOffsetEnabled) {
    const scale = 1 + settings.scaleOffset;
    x = center.x + (x - center.x) * scale + settings.scaleOffset * 90;
    y = center.y + (y - center.y) * (1 - settings.scaleOffset * 0.5) - settings.scaleOffset * 60;
  }

  if (settings.personalResidualEnabled) {
    x += Math.sin((point.y + pointIndex * 13) / 42) * settings.personalResidual;
    y += Math.cos((point.x + strokeIndex * 17) / 48) * settings.personalResidual;
  }

  return { ...point, x: clamp(x, -2000, 3000), y: clamp(y, -2000, 3000) };
}

function applyOpenGap(strokes: Stroke[], gapPx: number, topology: Topology): Stroke[] {
  if (topology === "open" || gapPx <= 0) {
    return strokes;
  }

  return strokes.map((stroke) => {
    if (stroke.points.length < 3) {
      return stroke;
    }

    const points = stroke.points.map((point) => ({ ...point }));
    const first = points[0];
    const last = points[points.length - 1];
    const dx = last.x - first.x;
    const dy = last.y - first.y;
    const length = Math.max(Math.hypot(dx, dy), 1);
    last.x += (dx / length) * gapPx + gapPx;
    last.y += (dy / length) * gapPx - gapPx * 0.5;
    return { ...stroke, points };
  });
}

function applyPointDropout(strokes: Stroke[], rate: number): Stroke[] {
  if (rate <= 0) {
    return strokes;
  }

  const step = Math.max(3, Math.round(1 / rate));
  return strokes.map((stroke) => {
    if (stroke.points.length <= 3) {
      return stroke;
    }

    const points = stroke.points.filter((_, index) => index === 0 || index === stroke.points.length - 1 || index % step !== 0);
    return { ...stroke, points };
  });
}

function applyStrokeMerge(strokes: Stroke[], ratio: number): Stroke[] {
  if (strokes.length < 2 || ratio <= 0) {
    return strokes;
  }

  const mergedPoints = strokes.flatMap((stroke, strokeIndex) =>
    stroke.points.map((point, pointIndex) => ({
      ...point,
      t: point.t + strokeIndex * Math.max(1, Math.round(40 * ratio)) + pointIndex
    }))
  );
  return [{ id: `merged_${compactId()}`, points: mergedPoints }];
}

function primaryNoiseRecipeId(): NoiseRecipeId {
  if (state.noise.personalResidualEnabled) return "personal_residual";
  if (state.noise.strokeMergeEnabled) return "stroke_merge";
  if (state.noise.fastCompressionEnabled) return "fast_compression";
  if (state.noise.openGapEnabled) return "open_gap";
  if (state.noise.rotationEnabled) return "rotation_drift";
  if (state.noise.scaleOffsetEnabled) return "scale_offset";
  if (state.noise.pointDropoutEnabled) return "point_dropout";
  if (state.noise.jitterEnabled) return "jitter";
  return "stable";
}

function serializeNoiseSettings(): Record<string, number | boolean> {
  return { ...state.noise };
}

function getEvalPresets(): EvalPreset[] {
  return [...BASE_PRESETS, customPreset()];
}

function selectedPreset(): EvalPreset {
  return getEvalPresets().find((preset) => preset.id === state.selectedPresetId) ?? getEvalPresets()[0];
}

function selectPreset(id: DatacardShapeId): void {
  state.selectedPresetId = id;
  state.rawStrokes = [];
  state.currentStroke = null;
  state.currentStartedAtMs = performance.now();
  persistDraft();
  render();
}

function selectCustomPreset(): void {
  selectPreset("custom:eval_custom");
}

function focusCustomPattern(): void {
  selectCustomPreset();
  window.setTimeout(() => document.querySelector<HTMLInputElement>("#custom-pattern")?.focus(), 0);
}

function resetCurrentInput(): void {
  state.rawStrokes = [];
  state.currentStroke = null;
  state.currentStartedAtMs = performance.now();
  persistDraft();
  render();
}

function restoreDraft(): AppState | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);

    if (!raw) {
      return null;
    }

    const parsed = JSON.parse(raw) as AppState;

    if (!Array.isArray(parsed.trials) || !parsed.noise) {
      return null;
    }

    return {
      ...createInitialState(),
      ...parsed,
      apiSession: null,
      apiError: null,
      submitStatus: "API session pending",
      currentStroke: null,
      rawStrokes: [],
      currentStartedAtMs: performance.now()
    };
  } catch {
    return null;
  }
}

function persistDraft(): void {
  window.clearTimeout(saveTimer);
  saveTimer = window.setTimeout(() => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
  }, 80);
}

const BASE_PRESETS: EvalPreset[] = [
  makePreset("custom:eval_rect", "rect", "rect", "^(rect)$", "closed", "low", [rectStroke()]),
  makePreset("custom:eval_ellipse", "ellipse", "ellipse", "^(ellipse)$", "closed", "low", [ellipseStroke()]),
  makePreset("custom:eval_triangle", "triangle", "triangle", "^(triangle)$", "closed", "low", [triangleStroke()]),
  makePreset("custom:eval_line", "line", "line", "^(line)$", "open", "low", [lineStroke("line", -0.72, 0, 0.72, 0)]),
  makePreset(
    "custom:eval_line3",
    "line{3}",
    "line{3}",
    "^(line){3}$",
    "open",
    "med",
    [
      lineStroke("line-a", -0.72, -0.32, 0.72, -0.32),
      lineStroke("line-b", -0.72, 0, 0.72, 0),
      lineStroke("line-c", -0.72, 0.32, 0.72, 0.32)
    ]
  ),
  makePreset("custom:eval_arc_rect", "arc+rect", "arc+rect", "^(arc\\+rect)$", "mixed", "high", [
    arcStroke(),
    scaledStroke(rectStroke(), 0, 0.16, 0.78, 0.62)
  ]),
  makePreset("custom:eval_ellipse_line", "ellipse&line", "ellipse&line", "^(ellipse&line)$", "mixed", "med", [
    ellipseStroke(),
    lineStroke("cross-line", -0.72, 0.02, 0.72, 0.02)
  ]),
  makePreset("custom:eval_rect_line", "rect&line", "rect&line", "^(rect&line)$", "mixed", "med", [
    rectStroke(),
    lineStroke("inner-line", -0.62, 0.04, 0.62, 0.04)
  ]),
  makePreset("custom:eval_wave_line3", "wave+line{3}", "wave+line{3}", "^(wave\\+line{3})$", "open", "high", [
    waveStroke(),
    lineStroke("wave-line-a", -0.72, -0.3, 0.72, -0.3),
    lineStroke("wave-line-b", -0.72, 0.3, 0.72, 0.3)
  ])
];

function customPreset(): EvalPreset {
  return makePreset(
    "custom:eval_custom",
    state.customLabel || "custom",
    state.customLabel || "custom",
    state.customPattern || "custom",
    "closed",
    "med",
    [starStroke()]
  );
}

function makePreset(
  id: DatacardShapeId,
  label: string,
  shortLabel: string,
  pattern: string,
  topology: Topology,
  risk: Risk,
  template: readonly Stroke[]
): EvalPreset {
  return {
    id,
    kind: "custom",
    group: "custom",
    label,
    shortLabel,
    description: `${label} noisy tinyML eval target`,
    topology,
    risk,
    definition: {
      pattern,
      expression: pattern,
      guide: label,
      keywords: [label, shortLabel],
      features: featureHintsFor(topology, template),
      exampleTemplate: template
    }
  };
}

function featureHintsFor(topology: Topology, template: readonly Stroke[]) {
  const strokeCount = [template.length, template.length] as const;

  if (topology === "closed") {
    return {
      strokeCount,
      closed: true,
      corners: [2, 12] as const,
      circularity: [0.12, 1] as const,
      fillRatio: [0.08, 0.82] as const
    };
  }

  if (topology === "mixed") {
    return {
      strokeCount,
      closed: false,
      corners: [1, 12] as const,
      endpointClusters: [2, 8] as const,
      fillRatio: [0.02, 0.72] as const,
      parallelism: [0, 1] as const
    };
  }

  return {
    strokeCount,
    closed: false,
    corners: [0, 8] as const,
    endpointClusters: [2, 8] as const,
    fillRatio: [0, 0.22] as const,
    parallelism: [0, 1] as const
  };
}

function rectStroke(): Stroke {
  return stroke("rect", [
    [-0.62, -0.42],
    [0.62, -0.42],
    [0.62, 0.42],
    [-0.62, 0.42],
    [-0.62, -0.42]
  ]);
}

function ellipseStroke(): Stroke {
  return stroke(
    "ellipse",
    Array.from({ length: 40 }, (_, index): [number, number] => {
      const angle = (index / 39) * Math.PI * 2;
      return [Math.cos(angle) * 0.66, Math.sin(angle) * 0.44];
    })
  );
}

function triangleStroke(): Stroke {
  return stroke("triangle", [
    [0, -0.68],
    [0.68, 0.54],
    [-0.68, 0.54],
    [0, -0.68]
  ]);
}

function starStroke(): Stroke {
  const points: Array<[number, number]> = [];

  for (let index = 0; index <= 10; index += 1) {
    const radius = index % 2 === 0 ? 0.72 : 0.32;
    const angle = -Math.PI / 2 + index * (Math.PI / 5);
    points.push([Math.cos(angle) * radius, Math.sin(angle) * radius]);
  }

  return stroke("star", points);
}

function arcStroke(): Stroke {
  return stroke(
    "arc",
    Array.from({ length: 24 }, (_, index): [number, number] => {
      const angle = Math.PI * 1.05 + (index / 23) * Math.PI * 0.9;
      return [Math.cos(angle) * 0.58, Math.sin(angle) * 0.42 - 0.05];
    })
  );
}

function waveStroke(): Stroke {
  return stroke(
    "wave",
    Array.from({ length: 26 }, (_, index): [number, number] => {
      const ratio = index / 25;
      return [-0.72 + ratio * 1.44, Math.sin(ratio * Math.PI * 4) * 0.22];
    })
  );
}

function lineStroke(id: string, x1: number, y1: number, x2: number, y2: number): Stroke {
  return stroke(id, [
    [x1, y1],
    [(x1 + x2) / 2, (y1 + y2) / 2],
    [x2, y2]
  ]);
}

function scaledStroke(base: Stroke, offsetX: number, offsetY: number, scaleX: number, scaleY: number): Stroke {
  return {
    ...base,
    id: `${base.id}-scaled`,
    points: base.points.map((point) => ({
      ...point,
      x: offsetX + point.x * scaleX,
      y: offsetY + point.y * scaleY
    }))
  };
}

function stroke(id: string, points: Array<[number, number]>): Stroke {
  return {
    id,
    points: points.map(([x, y], index) => ({ x, y, t: index * 16 }))
  };
}

function templateToCanvasPoint(point: PointSample): PointSample {
  return {
    ...point,
    x: CANVAS_WIDTH / 2 + point.x * 250,
    y: CANVAS_HEIGHT / 2 + point.y * 250
  };
}

function createTargetPreviewPanel(preset: EvalPreset): HTMLElement {
  const panel = el("aside", "target-preview");
  const heading = el("div", "target-preview-heading");
  heading.append(el("strong", "", "Target Preview"), el("span", "", `${preset.topology} / ${preset.definition.exampleTemplate.length} stroke`));

  const pattern = el("code", "target-preview-pattern", preset.definition.pattern);
  const meta = el("div", "target-preview-meta");
  meta.append(el("span", "", preset.shortLabel), el("span", "", preset.risk.toUpperCase()));

  panel.append(heading, targetPreviewSvg(preset), meta, pattern, targetStrokeLegend(preset));

  if (preset.id === "custom:eval_custom") {
    panel.append(el("p", "target-preview-note", "Custom regex currently uses this target template for capture and scoring."));
  }

  return panel;
}

function targetPreviewSvg(preset: EvalPreset): SVGSVGElement {
  const svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
  svg.setAttribute("viewBox", "0 0 220 132");
  svg.setAttribute("class", "target-preview-svg");
  svg.setAttribute("aria-hidden", "true");

  const bg = document.createElementNS("http://www.w3.org/2000/svg", "rect");
  bg.setAttribute("x", "0");
  bg.setAttribute("y", "0");
  bg.setAttribute("width", "220");
  bg.setAttribute("height", "132");
  bg.setAttribute("rx", "8");
  bg.setAttribute("fill", "#ffffff");
  svg.append(bg);

  for (let x = 22; x < 220; x += 22) {
    const line = document.createElementNS("http://www.w3.org/2000/svg", "line");
    line.setAttribute("x1", String(x));
    line.setAttribute("y1", "0");
    line.setAttribute("x2", String(x));
    line.setAttribute("y2", "132");
    line.setAttribute("stroke", "#edf3f8");
    line.setAttribute("stroke-width", "1");
    svg.append(line);
  }

  for (let y = 22; y < 132; y += 22) {
    const line = document.createElementNS("http://www.w3.org/2000/svg", "line");
    line.setAttribute("x1", "0");
    line.setAttribute("y1", String(y));
    line.setAttribute("x2", "220");
    line.setAttribute("y2", String(y));
    line.setAttribute("stroke", "#edf3f8");
    line.setAttribute("stroke-width", "1");
    svg.append(line);
  }

  preset.definition.exampleTemplate.forEach((stroke, index) => {
    const path = document.createElementNS("http://www.w3.org/2000/svg", "path");
    path.setAttribute("fill", "none");
    path.setAttribute("stroke", targetStrokeColor(index));
    path.setAttribute("stroke-width", "4");
    path.setAttribute("stroke-linecap", "round");
    path.setAttribute("stroke-linejoin", "round");
    path.setAttribute("d", svgPathFor(stroke, 110, 66, 78, 50));
    svg.append(path);

    const anchor = stroke.points[0];
    if (anchor) {
      const marker = document.createElementNS("http://www.w3.org/2000/svg", "circle");
      marker.setAttribute("cx", String(round(110 + anchor.x * 78, 1)));
      marker.setAttribute("cy", String(round(66 + anchor.y * 50, 1)));
      marker.setAttribute("r", "5");
      marker.setAttribute("fill", targetStrokeColor(index));
      svg.append(marker);
    }
  });

  return svg;
}

function targetStrokeLegend(preset: EvalPreset): HTMLElement {
  const legend = el("div", "target-stroke-legend");

  preset.definition.exampleTemplate.forEach((stroke, index) => {
    const item = el("span", "target-stroke-item");
    const swatch = el("span", "target-stroke-swatch");
    swatch.style.background = targetStrokeColor(index);
    item.append(swatch, document.createTextNode(`${index + 1}:${stroke.id}`));
    legend.append(item);
  });

  return legend;
}

function targetStrokeColor(index: number): string {
  return TARGET_STROKE_COLORS[index % TARGET_STROKE_COLORS.length];
}

function miniShape(preset: EvalPreset): HTMLElement {
  const wrap = el("span", "mini-shape");
  const svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
  svg.setAttribute("viewBox", "0 0 60 44");
  svg.setAttribute("width", "38");
  svg.setAttribute("height", "32");
  svg.setAttribute("aria-hidden", "true");
  const path = document.createElementNS("http://www.w3.org/2000/svg", "path");
  path.setAttribute("fill", "none");
  path.setAttribute("stroke", "#13202b");
  path.setAttribute("stroke-width", "2.2");
  path.setAttribute("stroke-linecap", "round");
  path.setAttribute("stroke-linejoin", "round");
  path.setAttribute("d", svgPathFor(preset.definition.exampleTemplate[0] ?? lineStroke("fallback", -0.5, 0, 0.5, 0)));
  svg.append(path);
  wrap.append(svg);
  return wrap;
}

function svgPathFor(strokeValue: Stroke, centerX = 30, centerY = 22, scaleX = 22, scaleY = 18): string {
  const points = strokeValue.points.map((point) => ({
    x: centerX + point.x * scaleX,
    y: centerY + point.y * scaleY
  }));

  return points.map((point, index) => `${index === 0 ? "M" : "L"} ${round(point.x, 1)} ${round(point.y, 1)}`).join(" ");
}

function metricCard(label: string, value: number, kind: "precision" | "recall" | "unsafe" | "flip"): HTMLElement {
  const card = el("section", "metric-card");
  const valueClass = kind === "unsafe" ? "warn" : kind === "flip" ? "danger" : "";
  card.append(el("span", "metric-label", label), el("span", ["metric-value", valueClass], value.toFixed(3)), sparkline(kind));
  return card;
}

function sparkline(kind: string): HTMLElement {
  const svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
  svg.setAttribute("class", "sparkline");
  svg.setAttribute("viewBox", "0 0 120 22");
  const path = document.createElementNS("http://www.w3.org/2000/svg", "path");
  const offset = kind.length * 3;
  const d = Array.from({ length: 18 }, (_, index) => {
    const x = 4 + index * 6.5;
    const y = 12 + Math.sin(index * 0.9 + offset) * 4 + Math.cos(index * 1.7) * 2;
    return `${index === 0 ? "M" : "L"}${round(x, 1)},${round(y, 1)}`;
  }).join(" ");
  path.setAttribute("d", d);
  path.setAttribute("fill", "none");
  path.setAttribute("stroke", kind === "flip" ? "#cf2f36" : kind === "unsafe" ? "#d88904" : "#0f7c8e");
  path.setAttribute("stroke-width", "1.7");
  svg.append(path);
  return svg as unknown as HTMLElement;
}

function decisionBox(label: string, candidate: string, confidence: number): HTMLElement {
  const box = el("div", "versus-box");
  box.append(el("span", "versus-label", label), el("strong", "", candidate), el("span", "muted", confidence.toFixed(3)));
  return box;
}

function resolveConfusionPair(analysis: CurrentAnalysis | null): { label: string; probability: number } {
  if (!analysis) {
    return { label: "no input", probability: 0 };
  }

  const target = selectedPreset().id;
  const top = analysis.noisySummary.topCandidates[0];
  const second = analysis.noisySummary.topCandidates[1];

  if (!top) {
    return { label: "no candidate", probability: 0 };
  }

  if (top.id !== target) {
    return { label: `${target} -> ${top.id}`, probability: top.score };
  }

  return { label: `${top.id} <-> ${second?.id ?? "none"}`, probability: Math.max(0, top.score - (second?.score ?? 0)) };
}

function blockerDescription(blocker: string): string {
  switch (blocker) {
    case "repetition":
      return "반복 개방구간으로 priority flip 위험이 큼";
    case "closure":
      return "폐쇄/개방 조건이 target과 어긋남";
    case "relation":
      return "조합 관계가 target feature와 불안정";
    case "topology":
      return "형태 skeleton 일치도가 낮음";
    case "noise":
      return "입력 샘플이 sparse하거나 과도하게 짧음";
    case "signature":
      return "개인 capture 또는 활성화 조건 미충족";
    case "holdout":
      return "confusion risk가 holdout cap 이상";
    default:
      return "현재 hard blocker 없음";
  }
}

function labelCheckbox(label: string, checked: boolean, onChange: (checked: boolean) => void): HTMLElement {
  const wrap = el("label", "field");
  const inputEl = document.createElement("input");
  inputEl.type = "checkbox";
  inputEl.checked = checked;
  inputEl.addEventListener("change", () => onChange(inputEl.checked));
  wrap.append(el("span", "", label), inputEl);
  return wrap;
}

function noiseControl(
  label: string,
  enabled: boolean,
  value: number,
  min: number,
  max: number,
  step: number,
  unit: string,
  onChange: (enabled: boolean, value: number) => void
): HTMLElement {
  const wrap = el("div", "noise-control");
  const top = el("div", "noise-top");
  const labelEl = document.createElement("label");
  labelEl.textContent = label;
  const toggle = document.createElement("input");
  toggle.type = "checkbox";
  toggle.className = "toggle";
  toggle.checked = enabled;
  top.append(labelEl, toggle);

  const range = document.createElement("input");
  range.type = "range";
  range.min = String(min);
  range.max = String(max);
  range.step = String(step);
  range.value = String(value);
  range.disabled = !enabled;
  const valueText = el("span", "noise-value", `${value}${unit ? ` ${unit}` : ""}`);

  const update = () => {
    const nextEnabled = toggle.checked;
    const nextValue = Number(range.value);
    range.disabled = !nextEnabled;
    valueText.textContent = `${round(nextValue, 2)}${unit ? ` ${unit}` : ""}`;
    onChange(nextEnabled, nextValue);
    persistDraft();
    drawCanvas();
    render();
  };

  toggle.addEventListener("change", update);
  range.addEventListener("input", update);
  wrap.append(top, range, valueText);
  return wrap;
}

function field(label: string, control: HTMLElement): HTMLElement {
  const wrap = el("label", "field");
  wrap.append(el("span", "", label), control);
  return wrap;
}

function input(id: string, value: string, onInput: (value: string) => void): HTMLInputElement {
  const inputEl = document.createElement("input");
  inputEl.id = id;
  inputEl.type = "text";
  inputEl.value = value;
  inputEl.addEventListener("input", () => onInput(inputEl.value));
  return inputEl;
}

function button(label: string, onClick?: () => void, disabled = false, className = ""): HTMLButtonElement {
  const btn = document.createElement("button");
  btn.type = "button";
  btn.textContent = label;
  btn.disabled = disabled;
  btn.className = className;

  if (onClick) {
    btn.addEventListener("click", onClick);
  }

  return btn;
}

function metaItem(label: string, value: string): HTMLElement {
  const item = el("span", "meta-item");
  item.append(document.createTextNode(label), el("span", "dark-chip", value));
  return item;
}

function downloadBlob(filename: string, text: string, type: string): void {
  const blob = new Blob([text], { type });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = filename;
  document.body.append(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

function csvText(rows: unknown[][]): string {
  return rows
    .map((row) =>
      row
        .map((cell) => {
          const value = String(cell);
          return /[",\n]/.test(value) ? `"${value.replace(/"/g, '""')}"` : value;
        })
        .join(",")
    )
    .join("\n");
}

function roundRect(context: CanvasRenderingContext2D, x: number, y: number, width: number, height: number, radius: number): void {
  context.beginPath();
  context.moveTo(x + radius, y);
  context.lineTo(x + width - radius, y);
  context.quadraticCurveTo(x + width, y, x + width, y + radius);
  context.lineTo(x + width, y + height - radius);
  context.quadraticCurveTo(x + width, y + height, x + width - radius, y + height);
  context.lineTo(x + radius, y + height);
  context.quadraticCurveTo(x, y + height, x, y + height - radius);
  context.lineTo(x, y + radius);
  context.quadraticCurveTo(x, y, x + radius, y);
  context.closePath();
}

function cloneStroke(strokeValue: Stroke): Stroke {
  return {
    ...strokeValue,
    points: strokeValue.points.map((point) => ({ ...point }))
  };
}

function compactId(): string {
  return (globalThis.crypto?.randomUUID?.() ?? `${Date.now()}_${Math.random()}`)
    .replace(/[^a-zA-Z0-9_-]/g, "")
    .slice(0, 28);
}

function localSessionId(): string {
  return `local_${state.startedAtIso.replace(/[^0-9]/g, "").slice(0, 14)}`;
}

function average(values: readonly number[]): number {
  if (values.length === 0) {
    return 0;
  }

  return values.reduce((sum, value) => sum + value, 0) / values.length;
}

function clamp(value: number, min: number, max: number): number {
  return Math.max(min, Math.min(max, value));
}

function round(value: number, precision: number): number {
  const factor = 10 ** precision;
  return Math.round(value * factor) / factor;
}

function el<K extends keyof HTMLElementTagNameMap>(
  tag: K,
  className?: string | string[],
  text?: string
): HTMLElementTagNameMap[K] {
  const node = document.createElement(tag);
  const classes = Array.isArray(className) ? className.filter(Boolean).join(" ") : className;

  if (classes) {
    node.className = classes;
  }

  if (text !== undefined) {
    node.textContent = text;
  }

  return node;
}

void initializeApiSession();
render();
