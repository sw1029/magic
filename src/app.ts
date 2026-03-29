import { recognizeSession } from "./recognizer/recognize";
import type {
  AxisLine,
  QualityVector,
  RecognitionLogEntry,
  RecognitionResult,
  Stroke,
  StrokeSession
} from "./recognizer/types";

const CANVAS_WIDTH = 900;
const CANVAS_HEIGHT = 620;

const EMPTY_QUALITY: QualityVector = {
  closure: 0,
  symmetry: 0,
  smoothness: 0,
  tempo: 0,
  overshoot: 0,
  stability: 0,
  rotationBias: 0
};

export function mountApp(root: HTMLDivElement): void {
  root.innerHTML = `
    <div class="shell">
      <header class="hero">
        <div>
          <p class="eyebrow">Magic Recognizer V1</p>
          <h1>초기 마법진 인식체계 프로토타입</h1>
          <p class="hero-copy">
            기본 5문양을 그리고 <span class="inline-pill">Seal</span> 하면 canonical 결과와 품질 벡터를 확정합니다.
            드로잉 중에는 후보군과 구조 힌트만 보여 줍니다.
          </p>
        </div>
        <div class="legend">
          <span>바람: 3개 평행 개방선</span>
          <span>땅: 폐합 사다리꼴</span>
          <span>불꽃: 폐합 삼각형</span>
          <span>물: 원형 폐합 루프</span>
          <span>생명: rooted Y</span>
        </div>
      </header>
      <main class="workspace">
        <section class="board-panel">
          <div class="board-head">
            <div>
              <p class="panel-label">Canvas</p>
              <h2>Draw Low-Level Input</h2>
            </div>
            <div class="toolbar">
              <button id="seal-button" class="primary">Seal</button>
              <button id="undo-button">Undo</button>
              <button id="reset-button">Reset</button>
              <button id="export-button">Export Logs</button>
            </div>
          </div>
          <div class="canvas-wrap">
            <canvas id="glyph-canvas" width="${CANVAS_WIDTH}" height="${CANVAS_HEIGHT}"></canvas>
          </div>
          <div class="board-foot">
            <label class="toggle">
              <input id="debug-toggle" type="checkbox" checked />
              <span>debug overlay</span>
            </label>
            <p id="canvas-hint" class="canvas-hint">입력을 시작하면 후보군과 구조 힌트를 표시합니다.</p>
          </div>
        </section>
        <aside class="sidebar">
          <section class="card">
            <p class="panel-label">Preview</p>
            <h3 id="preview-family">대기 중</h3>
            <p id="preview-status" class="status-chip status-invalid">invalid</p>
            <p id="preview-reason" class="card-copy">아직 입력이 없습니다.</p>
            <ol id="candidate-list" class="candidate-list"></ol>
          </section>
          <section class="card">
            <p class="panel-label">Final Result</p>
            <h3 id="final-family">seal 전</h3>
            <p id="final-status" class="status-chip status-invalid">invalid</p>
            <p id="final-reason" class="card-copy">Seal 후 canonical 결과와 로그가 남습니다.</p>
          </section>
          <section class="card">
            <p class="panel-label">Quality Vector</p>
            <div id="quality-list" class="quality-list"></div>
          </section>
          <section class="card log-card">
            <div class="log-head">
              <div>
                <p class="panel-label">Log Viewer</p>
                <h3>Recognition JSON</h3>
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

  const sealButton = select<HTMLButtonElement>(root, "#seal-button");
  const undoButton = select<HTMLButtonElement>(root, "#undo-button");
  const resetButton = select<HTMLButtonElement>(root, "#reset-button");
  const exportButton = select<HTMLButtonElement>(root, "#export-button");
  const debugToggle = select<HTMLInputElement>(root, "#debug-toggle");
  const canvasHint = select<HTMLParagraphElement>(root, "#canvas-hint");
  const previewFamily = select<HTMLElement>(root, "#preview-family");
  const previewStatus = select<HTMLElement>(root, "#preview-status");
  const previewReason = select<HTMLElement>(root, "#preview-reason");
  const finalFamily = select<HTMLElement>(root, "#final-family");
  const finalStatus = select<HTMLElement>(root, "#final-status");
  const finalReason = select<HTMLElement>(root, "#final-reason");
  const candidateList = select<HTMLOListElement>(root, "#candidate-list");
  const qualityList = select<HTMLDivElement>(root, "#quality-list");
  const logViewer = select<HTMLPreElement>(root, "#log-viewer");
  const logCount = select<HTMLSpanElement>(root, "#log-count");

  let session = createEmptySession();
  let currentStroke: Stroke | null = null;
  let previewResult = recognizeSession(session, { sealed: false });
  let finalResult: RecognitionResult | null = null;
  let logs: RecognitionLogEntry[] = [];
  let debugEnabled = true;

  canvas.addEventListener("pointerdown", (event) => {
    if (finalResult) {
      clearCurrentSession();
    }

    const point = pointFromEvent(canvas, event);
    const timestamp = Date.now();

    currentStroke = {
      id: crypto.randomUUID(),
      points: [{ ...point, t: timestamp, pressure: event.pressure || 0.5 }]
    };

    if (session.strokes.length === 0) {
      session.startedAt = timestamp;
    }

    session.strokes.push(currentStroke);
    canvas.setPointerCapture(event.pointerId);
    previewResult = recognizeSession(session, { sealed: false });
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

    previewResult = recognizeSession(session, { sealed: false });
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

    session.endedAt = Date.now();
    currentStroke = null;
    previewResult = recognizeSession(session, { sealed: false });
    render();
  };

  canvas.addEventListener("pointerup", stopStroke);
  canvas.addEventListener("pointercancel", stopStroke);

  sealButton.addEventListener("click", () => {
    if (session.strokes.length === 0) {
      return;
    }

    finalResult = recognizeSession(session, { sealed: true });
    logs = [
      {
        id: crypto.randomUUID(),
        timestamp: Date.now(),
        rawStrokeCount: session.strokes.length,
        rawStrokes: structuredClone(session.strokes),
        normalizedStrokes: finalResult.normalizedStrokes,
        result: finalResult
      },
      ...logs
    ];
    render();
  });

  undoButton.addEventListener("click", () => {
    if (session.strokes.length === 0) {
      return;
    }

    session.strokes = session.strokes.slice(0, -1);
    currentStroke = null;
    finalResult = null;
    session.endedAt = Date.now();
    previewResult = recognizeSession(session, { sealed: false });
    render();
  });

  resetButton.addEventListener("click", () => {
    clearCurrentSession();
    render();
  });

  exportButton.addEventListener("click", () => {
    const payload = JSON.stringify(logs, null, 2);
    const blob = new Blob([payload], { type: "application/json" });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = "magic-recognizer-v1-logs.json";
    anchor.click();
    URL.revokeObjectURL(url);
  });

  debugToggle.addEventListener("change", () => {
    debugEnabled = debugToggle.checked;
    render();
  });

  render();

  function clearCurrentSession(): void {
    session = createEmptySession();
    currentStroke = null;
    previewResult = recognizeSession(session, { sealed: false });
    finalResult = null;
  }

  function render(): void {
    drawCanvas(ctx, session, previewResult, finalResult, debugEnabled);
    updateSidebar();
  }

  function updateSidebar(): void {
    const previewTop = previewResult.topCandidate;
    previewFamily.textContent = previewTop ? familyLabel(previewTop.family) : "후보 없음";
    previewStatus.textContent = previewResult.status;
    previewStatus.className = `status-chip status-${previewResult.status}`;
    previewReason.textContent =
      previewResult.invalidReason ??
      (previewTop ? `top score ${(previewTop.score * 100).toFixed(1)} / distance ${previewTop.templateDistance.toFixed(3)}` : "아직 충분한 입력이 없습니다.");

    candidateList.innerHTML = previewResult.candidates
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

    if (finalResult?.canonicalFamily) {
      finalFamily.textContent = familyLabel(finalResult.canonicalFamily);
    } else {
      finalFamily.textContent = "seal 전";
    }

    finalStatus.textContent = finalResult?.status ?? "invalid";
    finalStatus.className = `status-chip status-${finalResult?.status ?? "invalid"}`;
    finalReason.textContent = finalResult
      ? finalResult.invalidReason ?? "canonical 결과가 저장되었습니다."
      : "Seal 후 canonical 결과와 로그가 남습니다.";

    const quality = (finalResult ?? previewResult).quality ?? EMPTY_QUALITY;
    qualityList.innerHTML = renderQuality(quality);

    logCount.textContent = `${logs.length} entries`;
    logViewer.textContent = JSON.stringify(logs, null, 2);
    canvasHint.textContent =
      finalResult?.status === "recognized"
        ? "Seal 완료: reset 하거나 바로 새 입력을 시작하면 현재 드로잉을 지우고 다시 시작합니다."
        : "debug overlay에서는 symmetry axis, closure gap, live status를 확인할 수 있습니다.";
  }
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

function drawCanvas(
  context: CanvasRenderingContext2D,
  session: StrokeSession,
  previewResult: RecognitionResult,
  finalResult: RecognitionResult | null,
  debugEnabled: boolean
): void {
  context.clearRect(0, 0, CANVAS_WIDTH, CANVAS_HEIGHT);
  context.fillStyle = "#f2efe6";
  context.fillRect(0, 0, CANVAS_WIDTH, CANVAS_HEIGHT);
  drawGrid(context);

  context.lineWidth = 4;
  context.lineCap = "round";
  context.lineJoin = "round";
  context.strokeStyle = "#203337";

  for (const stroke of session.strokes) {
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

  const liveResult = finalResult ?? previewResult;

  if (debugEnabled) {
    drawAxis(context, liveResult.symmetryAxis, "#2b6777");
    drawAxis(context, liveResult.closureLine, "#b14729");
  }

  context.fillStyle = "rgba(18, 30, 34, 0.84)";
  context.fillRect(20, 18, 220, 76);
  context.fillStyle = "#f4efe3";
  context.font = "600 14px Manrope, 'Segoe UI', sans-serif";
  context.fillText(`preview: ${liveResult.topCandidate ? familyLabel(liveResult.topCandidate.family) : "none"}`, 32, 44);
  context.font = "12px 'IBM Plex Mono', 'SFMono-Regular', monospace";
  context.fillText(`status: ${liveResult.status}`, 32, 63);
  context.fillText(
    liveResult.invalidReason ?? `closure ${(liveResult.quality.closure * 100).toFixed(0)} / symmetry ${(liveResult.quality.symmetry * 100).toFixed(0)}`,
    32,
    82
  );
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

function renderQuality(quality: QualityVector): string {
  const entries = Object.entries(quality);

  return entries
    .map(([key, value]) => {
      const clamped = Math.max(0, Math.min(value, 1));
      return `
        <div class="quality-row">
          <div class="quality-head">
            <span>${key}</span>
            <strong>${(clamped * 100).toFixed(0)}</strong>
          </div>
          <div class="quality-bar">
            <span style="width:${clamped * 100}%"></span>
          </div>
        </div>
      `;
    })
    .join("");
}
