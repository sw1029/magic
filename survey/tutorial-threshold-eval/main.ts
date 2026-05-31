import {
  appendDatacardShapeCapture,
  createDatacardRecognizerRegistry,
  createEmptyDatacardShapeCaptureStore,
  recognizeSessionWithDatacardRegistry,
  type DatacardRecognitionResult,
  type DatacardShapeCaptureStore,
  type DatacardShapeId,
  type DatacardShapePreset,
  type DatacardTinyMlContrastDecision
} from "../../src/recognizer/datacard-shape-lab";
import {
  calculateTinyMlTwoTrackCorrection,
  calculateTinyMlTwoTrackSessionState,
  calculateTutorialThresholdState,
  calculateTwoTrackAggregate,
  createDefaultTargetThresholdState,
  decideWithTwoTrackPersonalization,
  resolveTwoTrackConfusion,
  summarizeTwoTrackRecognition,
  type ConfusionSnapshot,
  type DynamicDecision,
  type RecognitionSummary,
  type TargetThresholdState,
  type ThresholdState,
  type TinyMlTrackCorrection,
  type TinyMlTwoTrackCorrection
} from "../../src/recognizer/two-track-personalization-engine";
import type { PointSample, Stroke, StrokeSession } from "../../src/recognizer/types";

const API_BASE_URL = import.meta.env.VITE_SURVEY_API_URL ?? `${location.protocol}//${location.hostname}:4174`;
const SCHEMA_VERSION = "tutorial-threshold-eval-v1";
const STORAGE_KEY = "tutorial-threshold-eval-draft-v1";
const CANVAS_WIDTH = 1000;
const CANVAS_HEIGHT = 640;
const TARGET_STROKE_COLORS = ["#5b67e8", "#0d9b85", "#d98200", "#a94ac2", "#ce3b46", "#4f7f12"];

type Topology = "closed" | "open" | "mixed";
type Risk = "low" | "med" | "high";
type EvalMode = "capture" | "eval";

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

interface TutorialCaptureRecord {
  captureId: string;
  targetPresetId: DatacardShapeId;
  targetPattern: string;
  topology: Topology;
  rawStrokes: Stroke[];
  recognition: RecognitionSummary;
  contrast: DatacardTinyMlContrastDecision;
  thresholdBefore: ThresholdState;
  thresholdAfter: ThresholdState;
  tinyMlCorrection?: TinyMlTwoTrackCorrection;
  confusion: ConfusionSnapshot;
  elapsedMs: number;
  pointerType: string;
  savedAtIso: string;
}

interface EvalRecord {
  trialId: string;
  targetPresetId: DatacardShapeId;
  targetPattern: string;
  topology: Topology;
  rawStrokes: Stroke[];
  recognition: RecognitionSummary;
  contrast: DatacardTinyMlContrastDecision;
  thresholdState: ThresholdState;
  dynamicDecision: DynamicDecision;
  dynamicReason: string;
  tinyMlCorrection?: TinyMlTwoTrackCorrection;
  confusion: ConfusionSnapshot;
  elapsedMs: number;
  pointerType: string;
  userMarkedConfused: boolean;
  savedAtIso: string;
}

interface CurrentAnalysis {
  strokes: Stroke[];
  result: DatacardRecognitionResult;
  summary: RecognitionSummary;
  threshold: ThresholdState;
  dynamicDecision: DynamicDecision;
  dynamicReason: string;
  tinyMlCorrection: TinyMlTwoTrackCorrection;
  confusion: ConfusionSnapshot;
  elapsedMs: number;
}

interface AppState {
  apiSession: ApiSession | null;
  apiError: string | null;
  participantId: string;
  selectedPresetId: DatacardShapeId;
  customPattern: string;
  customLabel: string;
  mode: EvalMode;
  startedAtIso: string;
  currentStartedAtMs: number;
  rawStrokes: Stroke[];
  currentStroke: Stroke | null;
  currentPointerType: string;
  captures: TutorialCaptureRecord[];
  evals: EvalRecord[];
  userMarkedConfused: boolean;
  notes: string;
  submitStatus: string;
}

const root = document.querySelector<HTMLDivElement>("#tutorial-threshold-app");

if (!root) {
  throw new Error("tutorial threshold root not found");
}

const appRoot = root;
let state = restoreDraft() ?? createInitialState();
let canvas: HTMLCanvasElement | null = null;
let ctx: CanvasRenderingContext2D | null = null;

function createInitialState(): AppState {
  return {
    apiSession: null,
    apiError: null,
    participantId: "",
    selectedPresetId: "custom:eval_rect",
    customPattern: "custom-star-target",
    customLabel: "custom",
    mode: "capture",
    startedAtIso: new Date().toISOString(),
    currentStartedAtMs: performance.now(),
    rawStrokes: [],
    currentStroke: null,
    currentPointerType: "unknown",
    captures: [],
    evals: [],
    userMarkedConfused: false,
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
  appRoot.textContent = "";
  appRoot.append(createShell());
  canvas = appRoot.querySelector<HTMLCanvasElement>("#threshold-canvas");
  ctx = canvas?.getContext("2d") ?? null;

  if (canvas && ctx) {
    wireCanvas(canvas);
    drawCanvas();
  }
}

function createShell(): HTMLElement {
  const shell = el("div", "app-shell");
  shell.append(createTopbar(), createMainGrid(), createLogQueue());
  return shell;
}

function createTopbar(): HTMLElement {
  const topbar = el("header", "topbar");
  const brand = el("div", "brand");
  brand.append(el("span", ["brand-mark", "threshold-brand-mark"], "TH"), el("h1", "", "Tutorial Threshold Eval"));

  const threshold = calculateThresholdState();
  const meta = el("div", "top-meta");
  meta.append(
    metaItem("세션", localSessionId().slice(-12)),
    metaItem("캡처", String(state.captures.length)),
    metaItem("평가", String(state.evals.length)),
    metaItem("accept", threshold.acceptThreshold.toFixed(3))
  );

  const actions = el("div", "top-actions");
  actions.append(
    el("span", "api-dot", state.apiSession ? "API ready" : "API fallback"),
    button("JSON 저장", downloadJson, state.captures.length + state.evals.length === 0),
    button("CSV 내보내기", downloadCsv, state.captures.length + state.evals.length === 0),
    button("세션 제출", submitSession, !state.apiSession || state.captures.length + state.evals.length === 0, "primary-button")
  );

  topbar.append(brand, meta, actions);
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
    el("span", "muted", "capture는 target 하나에서 얻은 feature도 전역 threshold에 반영"),
    field("Participant ID", input("participant-id", state.participantId, (value) => {
      state.participantId = value.trim().slice(0, 64);
      persistDraft();
    }))
  );
  return footer;
}

function createWorkspace(): HTMLElement {
  const workspace = el("section", "workspace");
  workspace.append(createToolbar(), createCanvasCard(), createCapturePanel());
  return workspace;
}

function createToolbar(): HTMLElement {
  const toolbar = el("div", "toolbar");
  const tools = el("div", "tool-strip");
  tools.append(
    button("Capture", () => setMode("capture"), false, state.mode === "capture" ? "tool-button active" : "tool-button"),
    button("Eval", () => setMode("eval"), false, state.mode === "eval" ? "tool-button active" : "tool-button"),
    button("지우기", resetCurrentInput, state.rawStrokes.length === 0, "tool-button"),
    button("타겟 오버레이", drawCanvas, false, "tool-button")
  );

  const actions = el("div", "action-strip");
  actions.append(
    labelCheckbox("혼동됨", state.userMarkedConfused, (checked) => {
      state.userMarkedConfused = checked;
      persistDraft();
      render();
    }),
    button("Capture 저장", saveTutorialCapture, state.rawStrokes.length === 0, state.mode === "capture" ? "primary-button" : ""),
    button("Eval 저장", saveEvalTrial, state.rawStrokes.length === 0, state.mode === "eval" ? "primary-button" : "")
  );

  toolbar.append(tools, actions);
  return toolbar;
}

function createCanvasCard(): HTMLElement {
  const card = el("div", "canvas-card");
  const c = document.createElement("canvas");
  c.id = "threshold-canvas";
  c.width = CANVAS_WIDTH;
  c.height = CANVAS_HEIGHT;
  c.setAttribute("aria-label", "tutorial threshold drawing canvas");
  card.append(c);
  return card;
}

function createCapturePanel(): HTMLElement {
  const threshold = calculateThresholdState();
  const analysis = analyzeCurrentInput();
  const confusion = analysis?.confusion ?? emptyConfusion();
  const targetState = threshold.targetAdjustments[selectedPreset().id] ?? defaultTargetThresholdState();
  const panel = el("section", "capture-panel");
  panel.append(
    thresholdCard("Global maturity", threshold.globalMaturity, `${state.captures.length} captures / lift ${threshold.globalScoreLift.toFixed(3)}`),
    thresholdCard("Accept threshold", threshold.acceptThreshold, `hold ${threshold.holdThreshold.toFixed(3)} / target ${targetState.acceptThreshold.toFixed(3)}`),
    thresholdCard("Risk limits", Math.min(1, (threshold.unsafeLimit + threshold.flipLimit) / 0.9), `unsafe ${threshold.unsafeLimit.toFixed(3)} / flip ${threshold.flipLimit.toFixed(3)}`),
    thresholdCard("Current confusion", confusion.confusionScore, `${confusion.topPair} / gap ${confusion.topGap.toFixed(3)}`),
    tinyMlTrackCard(analysis?.tinyMlCorrection.shadowTrack, "Shadow tinyML", "risk gate"),
    tinyMlTrackCard(analysis?.tinyMlCorrection.meaningTrack, "Meaning tinyML", "semantic recovery")
  );
  return panel;
}

function thresholdCard(label: string, value: number, detail: string): HTMLElement {
  const card = el("section", "threshold-card");
  const meter = el("div", "threshold-meter");
  const fill = el("span");
  fill.style.width = `${Math.round(clamp(value, 0, 1) * 100)}%`;
  meter.append(fill);
  card.append(el("small", "", label), el("strong", "", value.toFixed(3)), meter, el("small", "", detail));
  return card;
}

function tinyMlTrackCard(track: TinyMlTrackCorrection | undefined, label: string, fallback: string): HTMLElement {
  const card = el("section", "threshold-card tinyml-track-card");
  const value = track?.adjustedScore ?? 0;
  const meter = el("div", "threshold-meter tinyml-meter");
  const fill = el("span");
  fill.style.width = `${Math.round(clamp(value, 0, 1) * 100)}%`;
  meter.append(fill);
  card.append(
    el("small", "", label),
    el("strong", "", track ? value.toFixed(3) : "-"),
    meter,
    el("small", "", track ? `${track.decision} / threshold ${track.threshold.toFixed(3)} / margin ${track.margin.toFixed(3)}` : fallback)
  );
  return card;
}

function createMetricsPanel(): HTMLElement {
  const panel = el("aside", "panel metrics-panel");
  const header = el("div", "panel-header");
  header.append(el("h2", "", "동적 threshold"), el("span", "live", "LIVE"));
  panel.append(header);

  const threshold = calculateThresholdState();
  const analysis = analyzeCurrentInput();
  const aggregate = calculateAggregate();
  const metricGrid = el("div", "metric-grid");
  metricGrid.append(
    metricCard("Accepted Proxy", aggregate.acceptRate, "precision"),
    metricCard("Target Top-1", aggregate.top1Rate, "recall"),
    metricCard("Unsafe Mean", aggregate.avgUnsafeRisk, "unsafe"),
    metricCard("Confusion Mean", aggregate.avgConfusion, "flip"),
    metricCard("Meaning Promote", aggregate.tinyMlPromoteRate, "recall"),
    metricCard("Shadow Block", aggregate.tinyMlBlockRate, "unsafe")
  );

  panel.append(
    createTargetPreviewPanel(selectedPreset()),
    metricGrid,
    createDecisionCard(analysis),
    createTinyMlContrastCard(analysis),
    createThresholdCard(threshold),
    createConfusionCard(),
    createNotes()
  );
  return panel;
}

function createDecisionCard(analysis: CurrentAnalysis | null): HTMLElement {
  const card = el("section", "decision-card");
  card.append(el("p", "block-label", "Current dynamic decision"));
  const strip = el("div", "decision-strip");
  const decision = analysis?.dynamicDecision ?? "hold";
  strip.append(
    el("span", ["decision-pill", decision], analysis ? decision.toUpperCase() : "NO INPUT"),
    el("span", ["status-pill", "tinyml-track-pill"], analysis?.tinyMlCorrection.finalDecision ?? "tinyML -"),
    el("span", "status-pill", analysis?.summary.finalStatus ?? "idle"),
    el("span", "status-pill", analysis?.confusion.targetRank ? `rank ${analysis.confusion.targetRank}` : "rank -")
  );
  card.append(strip);
  card.append(el("span", "muted", analysis?.dynamicReason ?? "캡처 또는 평가 입력을 그리면 동적 threshold 판정이 표시됩니다."));
  return card;
}

function createTinyMlContrastCard(analysis: CurrentAnalysis | null): HTMLElement {
  const card = el("section", "decision-card tinyml-contrast-card");
  card.append(el("p", "block-label", "TinyML two-track contrast"));

  if (!analysis) {
    card.append(el("span", "muted", "No input. Shadow gate and meaning recovery are calculated after drawing."));
    return card;
  }

  const correction = analysis.tinyMlCorrection;
  const rows = el("div", "tinyml-track-list");
  rows.append(tinyMlTrackRow(correction.shadowTrack), tinyMlTrackRow(correction.meaningTrack));
  const footer = el("div", "tinyml-contrast-footer");
  footer.append(
    el("span", ["decision-pill", correction.finalDecision], correction.finalDecision.toUpperCase()),
    el("span", "status-pill", correction.selectedTrack),
    el("span", "status-pill", `delta ${correction.delta.toFixed(3)}`)
  );
  card.append(rows, footer, el("span", "muted", correction.finalReason));
  return card;
}

function tinyMlTrackRow(track: TinyMlTrackCorrection): HTMLElement {
  const row = el("div", "tinyml-track-row");
  row.append(
    el("span", "", track.label),
    el("strong", "", track.adjustedScore.toFixed(3)),
    el("span", ["decision-pill", track.decision], track.decision),
    el("small", "", `thr ${track.threshold.toFixed(3)} / corr ${track.correction.toFixed(3)}`)
  );
  return row;
}

function createThresholdCard(threshold: ThresholdState): HTMLElement {
  const card = el("section", "decision-card");
  card.append(el("p", "block-label", "Global transfer state"));
  const row = el("div", "pair-row");
  row.append(el("strong", "", `capture ${threshold.captureCount}`), el("span", "", `lift ${threshold.globalScoreLift.toFixed(3)}`));
  card.append(row);
  card.append(el("span", "muted", `accept ${threshold.acceptThreshold.toFixed(3)} / hold ${threshold.holdThreshold.toFixed(3)} / top gap floor ${threshold.topGapFloor.toFixed(3)}`));
  return card;
}

function createConfusionCard(): HTMLElement {
  const card = el("section", "confusion-card");
  card.append(el("p", "block-label", "Shape confusion"));
  const list = el("div", "confusion-list");
  const rows = confusionRows().slice(0, 8);

  if (rows.length === 0) {
    list.append(el("span", "muted", "아직 confusion 로그가 없습니다."));
  }

  for (const row of rows) {
    const item = el("div", "confusion-row");
    item.append(el("span", "", `${row.target} -> ${row.confusedWith}`), el("strong", "", row.score.toFixed(3)));
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
  textarea.placeholder = "튜토리얼 capture 후 threshold 변화, 도형별 혼동, 한붓/병합 입력 등을 기록";
  textarea.addEventListener("input", () => {
    state.notes = textarea.value.slice(0, 1200);
    persistDraft();
  });
  wrap.append(textarea);
  return wrap;
}

function createLogQueue(): HTMLElement {
  const card = el("section", "queue-card");
  const header = el("div", "queue-header");
  const title = el("div", "queue-meta");
  title.append(el("h2", "", "Capture / Eval Log"), el("span", "count-pill", `${state.captures.length} C / ${state.evals.length} E`));
  const actions = el("div", "queue-actions");
  actions.append(
    button("마지막 삭제", removeLastLog, state.captures.length + state.evals.length === 0),
    button("로그 정리", clearLogs, state.captures.length + state.evals.length === 0, "danger-button"),
    button("JSON 저장", downloadJson, state.captures.length + state.evals.length === 0),
    button("CSV 내보내기", downloadCsv, state.captures.length + state.evals.length === 0)
  );
  header.append(title, actions);

  const table = document.createElement("table");
  table.className = "queue-table threshold-log-table";
  table.append(createLogHead(), createLogBody());
  const footer = el("div", "footer-line");
  footer.append(
    el("span", "", "평가 기준: tutorial capture가 전역 threshold를 낮추고, 도형별 confusion이 다시 target threshold를 보정"),
    el("span", "", state.apiError ? `API: ${state.apiError}` : `API + local export ready / ${state.submitStatus}`)
  );

  card.append(header, table, footer);
  return card;
}

function createLogHead(): HTMLElement {
  const thead = document.createElement("thead");
  const row = document.createElement("tr");
  ["#", "Type", "Target", "Decision", "TinyML 2T", "Score", "Rank / Pair", "Threshold", "Elapsed", "State"].forEach((label) => {
    const th = document.createElement("th");
    th.textContent = label;
    row.append(th);
  });
  thead.append(row);
  return thead;
}

function createLogBody(): HTMLElement {
  const tbody = document.createElement("tbody");
  const logs = [
    ...state.captures.map((record) => ({ type: "capture" as const, record, at: record.savedAtIso })),
    ...state.evals.map((record) => ({ type: "eval" as const, record, at: record.savedAtIso }))
  ].sort((left, right) => left.at.localeCompare(right.at)).slice(-8);

  if (logs.length === 0) {
    const row = document.createElement("tr");
    const td = document.createElement("td");
    td.colSpan = 10;
    td.className = "muted";
    td.textContent = "아직 저장된 capture/eval 로그가 없습니다. 캔버스에 입력 후 Capture 저장 또는 Eval 저장을 누르세요.";
    row.append(td);
    tbody.append(row);
    return tbody;
  }

  logs.forEach((log, index) => {
    const row = document.createElement("tr");
    if (index === logs.length - 1) row.className = "active";
    const record = log.record;
    const threshold = log.type === "capture"
      ? (record as TutorialCaptureRecord).thresholdAfter
      : (record as EvalRecord).thresholdState;
    const decision = log.type === "capture" ? "capture" : (record as EvalRecord).dynamicDecision;
    const tinyMl = record.tinyMlCorrection;
    const cells = [
      String(index + 1).padStart(3, "0"),
      log.type,
      shortPresetId(record.targetPresetId),
      decision,
      tinyMl ? `${tinyMl.finalDecision} / ${tinyMl.selectedTrack}` : "-",
      record.recognition.score.toFixed(3),
      `${record.confusion.targetRank ?? "-"} / ${record.confusion.topPair}`,
      threshold.acceptThreshold.toFixed(3),
      `${record.elapsedMs} ms`
    ];

    for (const cell of cells) {
      const td = document.createElement("td");
      td.textContent = cell;
      row.append(td);
    }

    const stateCell = document.createElement("td");
    const badge = el("span", ["status-pill", log.type === "capture" ? "capture-badge" : "eval-badge"], log.type);
    stateCell.append(badge);
    row.append(stateCell);
    tbody.append(row);
  });

  return tbody;
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
    if (!state.currentStroke) return;
    event.preventDefault();
    state.currentStroke.points.push(samplePoint(canvasPoint(event), event));
    drawCanvas();
  });

  const stop = (event: PointerEvent) => {
    if (!state.currentStroke) return;
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
  if (!ctx) return;
  ctx.clearRect(0, 0, CANVAS_WIDTH, CANVAS_HEIGHT);
  drawGrid();
  drawTargetOverlay(selectedPreset());
  drawStrokes([...state.rawStrokes, ...(state.currentStroke ? [state.currentStroke] : [])], "#0f7c8e", 0.96, 3.4);
  drawCanvasHud(analyzeCurrentInput());
}

function drawGrid(): void {
  if (!ctx) return;
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
  if (!ctx) return;
  const context = ctx;
  context.save();
  context.lineWidth = 2.3;
  context.setLineDash([7, 7]);

  preset.definition.exampleTemplate.forEach((stroke, index) => {
    const points = stroke.points.map(templateToCanvasPoint);
    context.strokeStyle = targetStrokeColor(index);
    context.globalAlpha = 0.48;
    drawPath(points);

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

function drawStrokes(strokes: readonly Stroke[], color: string, alpha: number, width: number): void {
  if (!ctx) return;
  ctx.save();
  ctx.strokeStyle = color;
  ctx.lineWidth = width;
  ctx.lineCap = "round";
  ctx.lineJoin = "round";
  ctx.globalAlpha = alpha;
  for (const stroke of strokes) drawPath(stroke.points);
  ctx.restore();
}

function drawPath(points: readonly PointSample[]): void {
  if (!ctx || points.length === 0) return;
  ctx.beginPath();
  ctx.moveTo(points[0].x, points[0].y);
  for (const point of points.slice(1)) ctx.lineTo(point.x, point.y);
  ctx.stroke();
}

function drawCanvasHud(analysis: CurrentAnalysis | null): void {
  if (!ctx) return;
  const preset = selectedPreset();
  const threshold = analysis?.threshold ?? calculateThresholdState();
  ctx.save();
  ctx.fillStyle = "rgba(255,255,255,0.9)";
  ctx.strokeStyle = "#d8e1ea";
  roundRect(ctx, 690, 530, 286, 86, 8);
  ctx.fill();
  ctx.stroke();
  ctx.fillStyle = "#13202b";
  ctx.font = "700 14px Segoe UI, Arial";
  ctx.fillText(`Target: ${preset.shortLabel}`, 708, 558);
  ctx.fillStyle = "#667484";
  ctx.font = "12px Segoe UI, Arial";
  ctx.fillText(`Decision: ${analysis?.dynamicDecision ?? "-"} / score ${analysis?.summary.score.toFixed(3) ?? "-"}`, 708, 579);
  ctx.fillText(`TinyML: ${analysis?.tinyMlCorrection.finalDecision ?? "-"} / ${analysis?.tinyMlCorrection.selectedTrack ?? "-"}`, 708, 600);
  ctx.fillText(`Threshold: accept ${threshold.acceptThreshold.toFixed(3)} hold ${threshold.holdThreshold.toFixed(3)}`, 708, 612);
  ctx.restore();
}

function analyzeCurrentInput(): CurrentAnalysis | null {
  const strokes = [...state.rawStrokes, ...(state.currentStroke ? [state.currentStroke] : [])].filter((stroke) => stroke.points.length >= 2);
  if (strokes.length === 0) return null;

  const startedAt = performance.now();
  const preset = selectedPreset();
  const registry = createDatacardRecognizerRegistry(getEvalPresets(), buildCaptureStore());
  const result = recognizeSessionWithDatacardRegistry(toSession(strokes), registry, { selectedPresetId: preset.id });
  const summary = summarizeTwoTrackRecognition(result);
  const threshold = calculateThresholdState();
  const confusion = resolveTwoTrackConfusion(summary, preset.id);
  const tinyMlCorrection = calculateTinyMlTwoTrackCorrection({
    summary,
    contrast: result.contrast,
    threshold,
    confusion,
    targetPresetId: preset.id
  });
  const { decision, reason } = decideWithTwoTrackPersonalization({
    summary,
    threshold,
    confusion,
    tinyMl: tinyMlCorrection,
    targetPresetId: preset.id
  });

  return {
    strokes: strokes.map(cloneStroke),
    result,
    summary,
    threshold,
    dynamicDecision: decision,
    dynamicReason: reason,
    tinyMlCorrection,
    confusion,
    elapsedMs: Math.max(0, Math.round(performance.now() - startedAt + performance.now() - state.currentStartedAtMs))
  };
}

function saveTutorialCapture(): void {
  const analysis = analyzeCurrentInput();
  if (!analysis || !analysis.result.contrast) return;

  const preset = selectedPreset();
  const before = calculateThresholdState();
  const recordBase = {
    captureId: `capture_${compactId()}`,
    targetPresetId: preset.id,
    targetPattern: preset.definition.pattern,
    topology: preset.topology,
    rawStrokes: analysis.strokes.map(cloneStroke),
    recognition: analysis.summary,
    contrast: analysis.result.contrast,
    thresholdBefore: before,
    tinyMlCorrection: analysis.tinyMlCorrection,
    confusion: analysis.confusion,
    elapsedMs: analysis.elapsedMs,
    pointerType: state.currentPointerType,
    savedAtIso: new Date().toISOString()
  };

  state.captures = [...state.captures, { ...recordBase, thresholdAfter: before }];
  const after = calculateThresholdState();
  state.captures = state.captures.map((capture) =>
    capture.captureId === recordBase.captureId ? { ...capture, thresholdAfter: after } : capture
  );
  resetAfterSave("capture saved");
}

function saveEvalTrial(): void {
  const analysis = analyzeCurrentInput();
  if (!analysis || !analysis.result.contrast) return;

  const preset = selectedPreset();
  const record: EvalRecord = {
    trialId: `trial_${compactId()}`,
    targetPresetId: preset.id,
    targetPattern: preset.definition.pattern,
    topology: preset.topology,
    rawStrokes: analysis.strokes.map(cloneStroke),
    recognition: analysis.summary,
    contrast: analysis.result.contrast,
    thresholdState: analysis.threshold,
    dynamicDecision: analysis.dynamicDecision,
    dynamicReason: analysis.dynamicReason,
    tinyMlCorrection: analysis.tinyMlCorrection,
    confusion: analysis.confusion,
    elapsedMs: analysis.elapsedMs,
    pointerType: state.currentPointerType,
    userMarkedConfused: state.userMarkedConfused,
    savedAtIso: new Date().toISOString()
  };

  state.evals = [...state.evals, record];
  resetAfterSave("eval saved");
}

function resetAfterSave(status: string): void {
  state.rawStrokes = [];
  state.currentStroke = null;
  state.userMarkedConfused = false;
  state.currentStartedAtMs = performance.now();
  state.submitStatus = status;
  persistDraft();
  render();
}

function calculateThresholdState(): ThresholdState {
  return calculateTutorialThresholdState({
    captures: state.captures,
    evals: state.evals,
    targetPresetIds: getEvalPresets().map((preset) => preset.id)
  });
}

function buildCaptureStore(): DatacardShapeCaptureStore {
  let store = createEmptyDatacardShapeCaptureStore();

  for (const capture of state.captures) {
    store = appendDatacardShapeCapture(store, capture.targetPresetId, capture.rawStrokes, Date.parse(capture.savedAtIso));
  }

  return store;
}

function calculateAggregate() {
  return calculateTwoTrackAggregate(state.captures, state.evals);
}

function calculateTinyMlSessionState() {
  return calculateTinyMlTwoTrackSessionState([...state.captures, ...state.evals]);
}

function confusionRows(): Array<{ target: string; confusedWith: string; score: number }> {
  const rows = new Map<string, { target: string; confusedWith: string; scores: number[] }>();

  for (const item of [...state.captures, ...state.evals]) {
    const key = `${item.targetPresetId}->${item.confusion.confusedWith}`;
    const current = rows.get(key) ?? {
      target: shortPresetId(item.targetPresetId),
      confusedWith: shortPresetId(item.confusion.confusedWith),
      scores: []
    };
    current.scores.push(item.confusion.confusionScore);
    rows.set(key, current);
  }

  return [...rows.values()]
    .map((row) => ({ target: row.target, confusedWith: row.confusedWith, score: average(row.scores) || 0 }))
    .sort((left, right) => right.score - left.score);
}

function submitSession(): void {
  void (async () => {
    if (!state.apiSession || state.captures.length + state.evals.length === 0) return;
    const payload = buildPayload();
    state.submitStatus = "submitting";
    render();

    try {
      const response = await fetch(`${API_BASE_URL}/api/tutorial-threshold-eval-responses`, {
        method: "POST",
        credentials: "include",
        headers: {
          "Content-Type": "application/json",
          "X-CSRF-Token": state.apiSession.csrfToken
        },
        body: JSON.stringify(payload)
      });

      if (!response.ok) {
        throw new Error(`submit ${response.status}`);
      }

      state.submitStatus = "submitted";
    } catch (error) {
      state.submitStatus = error instanceof Error ? error.message : "submit failed";
    }

    persistDraft();
    render();
  })();
}

function buildPayload() {
  return {
    schemaVersion: SCHEMA_VERSION,
    submissionId: `tutorial_threshold_${compactId()}`,
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
    thresholdState: calculateThresholdState(),
    tinyMlTwoTrackState: calculateTinyMlSessionState(),
    captures: state.captures,
    evals: state.evals,
    aggregate: calculateAggregate()
  };
}

function downloadJson(): void {
  downloadBlob(`${localSessionId()}_tutorial-threshold-eval.json`, JSON.stringify(buildPayload(), null, 2), "application/json");
}

function downloadCsv(): void {
  const rows = [
    [
      "id",
      "type",
      "targetPresetId",
      "decision",
      "tinyMlFinal",
      "tinyMlSelectedTrack",
      "shadowScore",
      "meaningScore",
      "tinyMlDelta",
      "score",
      "unsafeRisk",
      "flipRisk",
      "targetRank",
      "topPair",
      "confusionScore",
      "acceptThreshold",
      "elapsedMs",
      "markedConfused"
    ],
    ...state.captures.map((capture) => [
      capture.captureId,
      "capture",
      capture.targetPresetId,
      "capture",
      capture.tinyMlCorrection?.finalDecision ?? "",
      capture.tinyMlCorrection?.selectedTrack ?? "",
      capture.tinyMlCorrection?.shadowTrack.adjustedScore ?? "",
      capture.tinyMlCorrection?.meaningTrack.adjustedScore ?? "",
      capture.tinyMlCorrection?.delta ?? "",
      capture.recognition.score,
      capture.recognition.unsafeRisk,
      capture.recognition.flipRisk,
      capture.confusion.targetRank ?? "",
      capture.confusion.topPair,
      capture.confusion.confusionScore,
      capture.thresholdAfter.acceptThreshold,
      capture.elapsedMs,
      ""
    ]),
    ...state.evals.map((trial) => [
      trial.trialId,
      "eval",
      trial.targetPresetId,
      trial.dynamicDecision,
      trial.tinyMlCorrection?.finalDecision ?? "",
      trial.tinyMlCorrection?.selectedTrack ?? "",
      trial.tinyMlCorrection?.shadowTrack.adjustedScore ?? "",
      trial.tinyMlCorrection?.meaningTrack.adjustedScore ?? "",
      trial.tinyMlCorrection?.delta ?? "",
      trial.recognition.score,
      trial.recognition.unsafeRisk,
      trial.recognition.flipRisk,
      trial.confusion.targetRank ?? "",
      trial.confusion.topPair,
      trial.confusion.confusionScore,
      trial.thresholdState.acceptThreshold,
      trial.elapsedMs,
      trial.userMarkedConfused
    ])
  ];
  downloadBlob(`${localSessionId()}_tutorial-threshold-eval-summary.csv`, csvText(rows), "text/csv");
}

function removeLastLog(): void {
  const lastCapture = state.captures[state.captures.length - 1];
  const lastEval = state.evals[state.evals.length - 1];

  if (!lastCapture && !lastEval) return;

  if (!lastEval || (lastCapture && lastCapture.savedAtIso > lastEval.savedAtIso)) {
    state.captures = state.captures.slice(0, -1);
  } else {
    state.evals = state.evals.slice(0, -1);
  }

  state.submitStatus = "last log removed";
  persistDraft();
  render();
}

function clearLogs(): void {
  if (state.captures.length + state.evals.length === 0) return;
  if (!window.confirm("현재 tutorial capture/eval local log를 모두 정리할까요? API에 이미 저장된 파일은 삭제하지 않습니다.")) return;
  state.captures = [];
  state.evals = [];
  state.submitStatus = "logs cleared";
  persistDraft();
  render();
}

function resetCurrentInput(): void {
  state.rawStrokes = [];
  state.currentStroke = null;
  state.currentStartedAtMs = performance.now();
  persistDraft();
  render();
}

function setMode(mode: EvalMode): void {
  state.mode = mode;
  persistDraft();
  render();
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
  selectPreset("custom:eval_custom");
  window.setTimeout(() => document.querySelector<HTMLInputElement>("#custom-pattern")?.focus(), 0);
}

function selectedPreset(): EvalPreset {
  return getEvalPresets().find((preset) => preset.id === state.selectedPresetId) ?? getEvalPresets()[0];
}

function getEvalPresets(): EvalPreset[] {
  return [...BASE_PRESETS, customPreset()];
}

const BASE_PRESETS: EvalPreset[] = [
  makePreset("custom:eval_rect", "rect", "rect", "^(rect)$", "closed", "low", [rectStroke()]),
  makePreset("custom:eval_ellipse", "ellipse", "ellipse", "^(ellipse)$", "closed", "low", [ellipseStroke()]),
  makePreset("custom:eval_triangle", "triangle", "triangle", "^(triangle)$", "closed", "low", [triangleStroke()]),
  makePreset("custom:eval_line", "line", "line", "^(line)$", "open", "low", [lineStroke("line", -0.72, 0, 0.72, 0)]),
  makePreset("custom:eval_line3", "line{3}", "line{3}", "^(line){3}$", "open", "med", [
    lineStroke("line-a", -0.72, -0.32, 0.72, -0.32),
    lineStroke("line-b", -0.72, 0, 0.72, 0),
    lineStroke("line-c", -0.72, 0.32, 0.72, 0.32)
  ]),
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
    description: `${label} tutorial threshold eval target`,
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

  return {
    strokeCount,
    closed: false,
    corners: [0, 10] as const,
    endpointClusters: [2, 8] as const,
    fillRatio: [0, 0.35] as const,
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

function createTargetPreviewPanel(preset: EvalPreset): HTMLElement {
  const panel = el("aside", "target-preview");
  const heading = el("div", "target-preview-heading");
  heading.append(el("strong", "", "Target Preview"), el("span", "", `${preset.topology} / ${preset.definition.exampleTemplate.length} stroke`));
  const pattern = el("code", "target-preview-pattern", preset.definition.pattern);
  const meta = el("div", "target-preview-meta");
  meta.append(el("span", "", preset.shortLabel), el("span", "", preset.risk.toUpperCase()));
  panel.append(heading, targetPreviewSvg(preset), meta, pattern, targetStrokeLegend(preset));
  return panel;
}

function targetPreviewSvg(preset: EvalPreset): SVGSVGElement {
  const svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
  svg.setAttribute("viewBox", "0 0 220 132");
  svg.setAttribute("class", "target-preview-svg");
  svg.setAttribute("aria-hidden", "true");
  preset.definition.exampleTemplate.forEach((strokeValue, index) => {
    const path = document.createElementNS("http://www.w3.org/2000/svg", "path");
    path.setAttribute("fill", "none");
    path.setAttribute("stroke", targetStrokeColor(index));
    path.setAttribute("stroke-width", "4");
    path.setAttribute("stroke-linecap", "round");
    path.setAttribute("stroke-linejoin", "round");
    path.setAttribute("d", svgPathFor(strokeValue, 110, 66, 78, 50));
    svg.append(path);
  });
  return svg;
}

function targetStrokeLegend(preset: EvalPreset): HTMLElement {
  const legend = el("div", "target-stroke-legend");
  preset.definition.exampleTemplate.forEach((strokeValue, index) => {
    const item = el("span", "target-stroke-item");
    const swatch = el("span", "target-stroke-swatch");
    swatch.style.background = targetStrokeColor(index);
    item.append(swatch, document.createTextNode(`${index + 1}:${strokeValue.id}`));
    legend.append(item);
  });
  return legend;
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

function templateToCanvasPoint(point: PointSample): PointSample {
  return {
    ...point,
    x: CANVAS_WIDTH / 2 + point.x * 250,
    y: CANVAS_HEIGHT / 2 + point.y * 250
  };
}

function targetStrokeColor(index: number): string {
  return TARGET_STROKE_COLORS[index % TARGET_STROKE_COLORS.length];
}

function canvasPoint(event: PointerEvent): { x: number; y: number } {
  if (!canvas) return { x: 0, y: 0 };
  const rect = canvas.getBoundingClientRect();
  return {
    x: clamp(((event.clientX - rect.left) / rect.width) * CANVAS_WIDTH, 0, CANVAS_WIDTH),
    y: clamp(((event.clientY - rect.top) / rect.height) * CANVAS_HEIGHT, 0, CANVAS_HEIGHT)
  };
}

function samplePoint(point: { x: number; y: number }, event: PointerEvent): PointSample {
  return {
    x: round(point.x, 2),
    y: round(point.y, 2),
    t: Math.max(0, Math.round(performance.now() - state.currentStartedAtMs)),
    pressure: event.pressure || 0.5
  };
}

function toSession(strokes: readonly Stroke[]): StrokeSession {
  return {
    strokes: strokes.map(cloneStroke),
    startedAt: state.currentStartedAtMs,
    endedAt: performance.now()
  };
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

function metricCard(label: string, value: number, kind: "precision" | "recall" | "unsafe" | "flip"): HTMLElement {
  const card = el("section", "metric-card");
  const valueClass = kind === "unsafe" ? "warn" : kind === "flip" ? "danger" : "";
  card.append(el("span", "metric-label", label), el("span", ["metric-value", valueClass], value.toFixed(3)), sparkline(kind));
  return card;
}

function sparkline(kind: string): HTMLElement {
  const svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
  svg.setAttribute("viewBox", "0 0 100 28");
  svg.setAttribute("aria-hidden", "true");
  const path = document.createElementNS("http://www.w3.org/2000/svg", "path");
  const offset = kind === "unsafe" ? 8 : kind === "flip" ? 14 : 0;
  const d = Array.from({ length: 9 }, (_, index) => {
    const x = 8 + index * 10;
    const y = 18 - Math.sin(index * 1.7 + offset) * 5 - (kind === "recall" ? index * 0.2 : 0);
    return `${index === 0 ? "M" : "L"} ${round(x, 1)} ${round(y, 1)}`;
  }).join(" ");
  path.setAttribute("d", d);
  path.setAttribute("fill", "none");
  path.setAttribute("stroke", kind === "flip" ? "#cf2f36" : kind === "unsafe" ? "#d88904" : "#0f7c8e");
  path.setAttribute("stroke-width", "2");
  svg.append(path);
  return svg as unknown as HTMLElement;
}

function button(label: string, onClick?: () => void, disabled = false, className = ""): HTMLButtonElement {
  const btn = document.createElement("button");
  btn.type = "button";
  btn.textContent = label;
  btn.disabled = disabled;
  btn.className = className;
  if (onClick) btn.addEventListener("click", onClick);
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

function restoreDraft(): AppState | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as AppState;
    if (!Array.isArray(parsed.captures) || !Array.isArray(parsed.evals)) return null;
    return {
      ...createInitialState(),
      ...parsed,
      apiSession: null,
      apiError: null,
      submitStatus: "API session pending",
      currentStroke: null,
      currentStartedAtMs: performance.now()
    };
  } catch {
    return null;
  }
}

function persistDraft(): void {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
  } catch {
    // Local export still works if draft storage is unavailable.
  }
}

function defaultTargetThresholdState(): TargetThresholdState {
  return createDefaultTargetThresholdState();
}

function emptyConfusion(): ConfusionSnapshot {
  return {
    targetRank: null,
    topPair: "no input",
    topGap: 0,
    targetInTop5: false,
    confusedWith: "none",
    confusionScore: 0
  };
}

function cloneStroke(strokeValue: Stroke): Stroke {
  return {
    ...strokeValue,
    points: strokeValue.points.map((point) => ({ ...point }))
  };
}

function localSessionId(): string {
  return `local_${state.startedAtIso.replace(/[^0-9]/g, "").slice(0, 14)}`;
}

function compactId(): string {
  return (globalThis.crypto?.randomUUID?.() ?? `${Date.now()}_${Math.random()}`)
    .replace(/[^a-zA-Z0-9_-]/g, "")
    .slice(0, 28);
}

function average(values: readonly number[]): number {
  if (values.length === 0) return 0;
  return values.reduce((sum, value) => sum + value, 0) / values.length;
}

function shortPresetId(value: string): string {
  return value.replace("custom:eval_", "");
}

function clamp(value: number, min: number, max: number): number {
  return Math.max(min, Math.min(max, value));
}

function round(value: number, precision: number): number {
  const factor = 10 ** precision;
  return Math.round(value * factor) / factor;
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

function el<K extends keyof HTMLElementTagNameMap>(
  tag: K,
  className?: string | string[],
  text?: string
): HTMLElementTagNameMap[K] {
  const node = document.createElement(tag);
  const classes = Array.isArray(className) ? className.filter(Boolean).join(" ") : className;
  if (classes) node.className = classes;
  if (text !== undefined) node.textContent = text;
  return node;
}

void initializeApiSession();
render();
