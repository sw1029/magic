(function () {
  "use strict";

  const SCHEMA_VERSION = "shape-composition-tracing-survey-v1";
  const STORAGE_KEY = "shape-composition-tracing-survey-draft-v1";
  const CANVAS_WIDTH = 1000;
  const CANVAS_HEIGHT = 640;
  const MAX_SHAPES = 12;
  const TOTAL_COMPOSITIONS = 2;
  const MIN_SHAPE_SIZE = 18;
  const HANDLE_RADIUS = 6;
  const TRIAL_PLAN = buildTrialPlan();
  const EXPECTED_TRIALS = TOTAL_COMPOSITIONS * TRIAL_PLAN.length;

  const SHAPE_TOOLS = [
    { id: "select", label: "선택", icon: "cursor" },
    { id: "line", label: "선", icon: "line" },
    { id: "arrow", label: "화살표", icon: "arrow" },
    { id: "rect", label: "사각형", icon: "rect" },
    { id: "roundRect", label: "둥근 사각형", icon: "roundRect" },
    { id: "ellipse", label: "타원", icon: "ellipse" },
    { id: "triangle", label: "삼각형", icon: "triangle" },
    { id: "diamond", label: "마름모", icon: "diamond" },
    { id: "elbow", label: "꺾은 선", icon: "elbow" },
    { id: "rightArrow", label: "블록 화살표", icon: "rightArrow" },
    { id: "downArrow", label: "아래 화살표", icon: "downArrow" },
    { id: "arc", label: "호", icon: "arc" },
    { id: "curve", label: "곡선", icon: "curve" },
    { id: "wave", label: "물결", icon: "wave" },
    { id: "braceL", label: "왼쪽 중괄호", icon: "braceL" },
    { id: "braceR", label: "오른쪽 중괄호", icon: "braceR" }
  ];

  const canvas = document.querySelector("#work-canvas");
  const ctx = canvas.getContext("2d");
  const shapeToolsEl = document.querySelector("#shape-tools");
  const actionsEl = document.querySelector("#context-actions");
  const inspectorEl = document.querySelector("#inspector");
  const phaseTitleEl = document.querySelector("#phase-title");
  const phaseCopyEl = document.querySelector("#phase-copy");
  const progressTextEl = document.querySelector("#progress-text");
  const progressBarEl = document.querySelector("#progress-bar");
  const canvasStatusEl = document.querySelector("#canvas-status");
  const autosaveStatusEl = document.querySelector("#autosave-status");
  const sessionChipEl = document.querySelector("#session-chip");
  const phaseChipEl = document.querySelector("#phase-chip");
  const participantInput = document.querySelector("#participant-id");
  const downloadJsonButton = document.querySelector("#download-json");
  const downloadCsvButton = document.querySelector("#download-csv");
  const newSessionButton = document.querySelector("#new-session");

  let state = createInitialState();
  let interaction = null;
  let previewShape = null;
  let saveTimer = 0;

  const restored = restoreDraft();
  if (restored) {
    state = restored;
    state.autosaveRestored = true;
  }

  participantInput.value = state.participantId;
  wireToolbar();
  wireControls();
  wireCanvas();
  resizeCanvas();
  updateUI();
  renderCanvas();

  function buildTrialPlan() {
    const blocks = [
      { id: "free", label: "마음대로", count: 3 },
      { id: "straight", label: "똑바로", count: 7 },
      { id: "comfortable", label: "편하게", count: 7 },
      { id: "fast", label: "빠르게", count: 7 },
      { id: "final_straight", label: "최종 똑바로", count: 3 }
    ];
    const plan = [];

    for (const block of blocks) {
      for (let index = 0; index < block.count; index += 1) {
        plan.push({
          blockId: block.id,
          blockLabel: block.label,
          blockTrialIndex: index + 1,
          blockTrialCount: block.count
        });
      }
    }

    return plan;
  }

  function createInitialState() {
    const sessionId = compactId();
    return {
      schemaVersion: SCHEMA_VERSION,
      sessionId,
      participantId: "",
      startedAtIso: new Date().toISOString(),
      completedAtIso: null,
      phase: "compose",
      activeTool: "select",
      currentCompositionIndex: 0,
      trialIndexInComposition: 0,
      compositionStartedAtMs: performance.now(),
      trialStartedAtMs: null,
      selectedShapeId: null,
      compositions: Array.from({ length: TOTAL_COMPOSITIONS }, (_, index) => ({
        compositionId: `${sessionId}-composition-${index + 1}`,
        index,
        startedAtIso: index === 0 ? new Date().toISOString() : null,
        finalizedAtIso: null,
        shapes: [],
        editEvents: []
      })),
      currentTraceStrokes: [],
      currentStroke: null,
      trials: [],
      resetEvents: [],
      downloadEvents: [],
      autosaveRestored: false
    };
  }

  function wireToolbar() {
    shapeToolsEl.textContent = "";
    for (const tool of SHAPE_TOOLS) {
      const button = document.createElement("button");
      button.type = "button";
      button.className = "tool-button";
      button.dataset.tool = tool.id;
      button.title = tool.label;
      button.setAttribute("aria-label", tool.label);
      button.innerHTML = iconSvg(tool.icon);
      button.addEventListener("click", () => {
        state.activeTool = tool.id;
        if (tool.id !== "select") {
          state.selectedShapeId = null;
        }
        updateUI();
        renderCanvas();
      });
      shapeToolsEl.append(button);
    }
  }

  function wireControls() {
    participantInput.addEventListener("input", () => {
      state.participantId = participantInput.value.trim().slice(0, 64);
      persistDraft();
    });

    downloadJsonButton.addEventListener("click", () => downloadJson());
    downloadCsvButton.addEventListener("click", () => downloadCsv());
    newSessionButton.addEventListener("click", () => {
      if (!window.confirm("현재 임시 저장 내용을 지우고 새 세션을 시작할까요?")) {
        return;
      }
      localStorage.removeItem(STORAGE_KEY);
      state = createInitialState();
      interaction = null;
      previewShape = null;
      participantInput.value = "";
      resizeCanvas();
      updateUI();
      renderCanvas();
      persistDraft();
    });

    window.addEventListener("resize", () => {
      resizeCanvas();
      renderCanvas();
    });

    window.addEventListener("keydown", (event) => {
      if (event.target instanceof HTMLInputElement) {
        return;
      }
      handleKeyboard(event);
    });
  }

  function wireCanvas() {
    canvas.addEventListener("pointerdown", (event) => {
      event.preventDefault();
      const point = pointFromEvent(event);
      canvas.setPointerCapture(event.pointerId);

      if (state.phase === "compose") {
        onComposePointerDown(point, event);
        return;
      }

      if (state.phase === "trace") {
        onTracePointerDown(point, event);
      }
    });

    canvas.addEventListener("pointermove", (event) => {
      if (!interaction) {
        return;
      }
      event.preventDefault();
      const point = pointFromEvent(event);

      if (state.phase === "compose") {
        onComposePointerMove(point);
        return;
      }

      if (state.phase === "trace") {
        onTracePointerMove(point, event);
      }
    });

    const stop = (event) => {
      if (!interaction) {
        return;
      }
      event.preventDefault();
      const point = pointFromEvent(event);

      if (state.phase === "compose") {
        onComposePointerUp(point);
        return;
      }

      if (state.phase === "trace") {
        onTracePointerUp(point, event);
      }
    };

    canvas.addEventListener("pointerup", stop);
    canvas.addEventListener("pointercancel", stop);
  }

  function onComposePointerDown(point) {
    const composition = activeComposition();
    const selected = selectedShape();
    const handle = selected ? handleAtPoint(selected, point) : null;

    if (handle) {
      interaction = {
        kind: handle.kind === "rotate" ? "rotate" : "resize",
        handleId: handle.id,
        shapeId: selected.id,
        startPoint: point,
        startShape: clone(selected),
        startedAtMs: performance.now()
      };
      return;
    }

    const hit = shapeAtPoint(point);

    if (state.activeTool === "select") {
      if (hit) {
        state.selectedShapeId = hit.id;
        interaction = {
          kind: "move",
          shapeId: hit.id,
          startPoint: point,
          startShape: clone(hit),
          startedAtMs: performance.now()
        };
      } else {
        state.selectedShapeId = null;
      }
      updateUI();
      renderCanvas();
      return;
    }

    if (composition.shapes.length >= MAX_SHAPES) {
      setCanvasStatus(`도형은 최대 ${MAX_SHAPES}개까지 지정할 수 있습니다.`);
      return;
    }

    interaction = {
      kind: "create",
      tool: state.activeTool,
      startPoint: point,
      currentPoint: point,
      startedAtMs: performance.now()
    };
    previewShape = createShapeFromDrag(state.activeTool, point, point, true);
    renderCanvas();
  }

  function onComposePointerMove(point) {
    if (!interaction) {
      return;
    }

    if (interaction.kind === "create") {
      interaction.currentPoint = point;
      previewShape = createShapeFromDrag(interaction.tool, interaction.startPoint, point, true);
      renderCanvas();
      return;
    }

    const shape = findShape(interaction.shapeId);
    if (!shape) {
      return;
    }

    if (interaction.kind === "move") {
      shape.x = clamp(interaction.startShape.x + point.x - interaction.startPoint.x, -shape.w + 10, CANVAS_WIDTH - 10);
      shape.y = clamp(interaction.startShape.y + point.y - interaction.startPoint.y, -shape.h + 10, CANVAS_HEIGHT - 10);
    }

    if (interaction.kind === "resize") {
      applyResize(shape, interaction.startShape, interaction.handleId, point);
    }

    if (interaction.kind === "rotate") {
      const center = centerOf(interaction.startShape);
      shape.rotation = Math.atan2(point.y - center.y, point.x - center.x) + Math.PI / 2;
    }

    updateUI();
    renderCanvas();
  }

  function onComposePointerUp(point) {
    if (!interaction) {
      return;
    }

    const composition = activeComposition();

    if (interaction.kind === "create") {
      const shape = createShapeFromDrag(interaction.tool, interaction.startPoint, point, false);
      if (shape && shape.w >= MIN_SHAPE_SIZE && shape.h >= MIN_SHAPE_SIZE) {
        composition.shapes.push(shape);
        state.selectedShapeId = shape.id;
        state.activeTool = "select";
        recordEditEvent("shape:create", { shape: clone(shape) });
      }
      previewShape = null;
    }

    if (["move", "resize", "rotate"].includes(interaction.kind)) {
      const shape = findShape(interaction.shapeId);
      if (shape && shapeChanged(shape, interaction.startShape)) {
        recordEditEvent(`shape:${interaction.kind}`, {
          shapeId: shape.id,
          before: clone(interaction.startShape),
          after: clone(shape)
        });
      }
    }

    interaction = null;
    persistDraft();
    updateUI();
    renderCanvas();
  }

  function onTracePointerDown(point, event) {
    const now = performance.now();
    if (state.trialStartedAtMs === null) {
      state.trialStartedAtMs = now;
    }
    const stroke = {
      strokeId: compactId(),
      pointerType: event.pointerType || "unknown",
      startedAtMs: Math.round(now - state.trialStartedAtMs),
      samples: [traceSample(point, event, now)]
    };
    state.currentStroke = stroke;
    state.currentTraceStrokes.push(stroke);
    interaction = { kind: "draw", pointerId: event.pointerId };
    renderCanvas();
  }

  function onTracePointerMove(point, event) {
    if (!state.currentStroke) {
      return;
    }
    const last = state.currentStroke.samples[state.currentStroke.samples.length - 1];
    if (Math.hypot(point.x - last.x, point.y - last.y) < 1.2) {
      return;
    }
    state.currentStroke.samples.push(traceSample(point, event, performance.now()));
    renderCanvas();
  }

  function onTracePointerUp(point, event) {
    if (state.currentStroke) {
      const last = state.currentStroke.samples[state.currentStroke.samples.length - 1];
      if (Math.hypot(point.x - last.x, point.y - last.y) >= 1.2) {
        state.currentStroke.samples.push(traceSample(point, event, performance.now()));
      }
      state.currentStroke.endedAtMs = Math.round(performance.now() - state.trialStartedAtMs);
    }
    state.currentStroke = null;
    interaction = null;
    updateUI();
    renderCanvas();
  }

  function traceSample(point, event, now) {
    return {
      x: round(point.x, 2),
      y: round(point.y, 2),
      x1000: clamp(Math.round((point.x / CANVAS_WIDTH) * 1000), 0, 1000),
      y1000: clamp(Math.round((point.y / CANVAS_HEIGHT) * 1000), 0, 1000),
      tMs: Math.max(0, Math.round(now - state.trialStartedAtMs)),
      pressure: Number.isFinite(event.pressure) ? round(event.pressure, 3) : 0.5
    };
  }

  function finishComposition() {
    const composition = activeComposition();
    if (composition.shapes.length === 0) {
      setCanvasStatus("도형을 1개 이상 지정해야 다음 단계로 넘어갈 수 있습니다.");
      return;
    }

    composition.finalizedAtIso = new Date().toISOString();
    recordEditEvent("composition:finalize", { shapeCount: composition.shapes.length });
    state.phase = "trace";
    state.activeTool = "select";
    state.selectedShapeId = null;
    state.trialIndexInComposition = 0;
    state.trialStartedAtMs = performance.now();
    state.currentTraceStrokes = [];
    state.currentStroke = null;
    persistDraft();
    updateUI();
    renderCanvas();
  }

  function resetComposition() {
    if (!window.confirm("현재 도형 조합을 비울까요?")) {
      return;
    }
    const composition = activeComposition();
    recordEditEvent("composition:clear", { previousShapes: clone(composition.shapes) });
    composition.shapes = [];
    composition.finalizedAtIso = null;
    state.selectedShapeId = null;
    persistDraft();
    updateUI();
    renderCanvas();
  }

  function deleteSelectedShape() {
    const composition = activeComposition();
    const index = composition.shapes.findIndex((shape) => shape.id === state.selectedShapeId);
    if (index < 0) {
      return;
    }
    const [removed] = composition.shapes.splice(index, 1);
    recordEditEvent("shape:delete", { shape: removed });
    state.selectedShapeId = null;
    persistDraft();
    updateUI();
    renderCanvas();
  }

  function duplicateSelectedShape() {
    const shape = selectedShape();
    const composition = activeComposition();
    if (!shape || composition.shapes.length >= MAX_SHAPES) {
      return;
    }
    const copy = {
      ...clone(shape),
      id: compactId(),
      x: clamp(shape.x + 24, 0, CANVAS_WIDTH - shape.w),
      y: clamp(shape.y + 24, 0, CANVAS_HEIGHT - shape.h),
      createdAtMs: Math.round(performance.now() - state.compositionStartedAtMs),
      updatedAtMs: Math.round(performance.now() - state.compositionStartedAtMs)
    };
    composition.shapes.push(copy);
    state.selectedShapeId = copy.id;
    recordEditEvent("shape:duplicate", { sourceShapeId: shape.id, shape: clone(copy) });
    persistDraft();
    updateUI();
    renderCanvas();
  }

  function saveTrial() {
    if (state.phase !== "trace" || state.currentTraceStrokes.length === 0) {
      setCanvasStatus("현재 시도에서 그린 입력이 있어야 저장할 수 있습니다.");
      return;
    }

    const plan = TRIAL_PLAN[state.trialIndexInComposition];
    const composition = activeComposition();
    const now = performance.now();
    const globalTrialIndex = state.currentCompositionIndex * TRIAL_PLAN.length + state.trialIndexInComposition;
    const record = {
      trialId: `${state.sessionId}-trial-${globalTrialIndex + 1}`,
      compositionId: composition.compositionId,
      compositionIndex: state.currentCompositionIndex,
      trialGlobalIndex: globalTrialIndex,
      trialInComposition: state.trialIndexInComposition,
      blockId: plan.blockId,
      blockLabel: plan.blockLabel,
      blockTrialIndex: plan.blockTrialIndex,
      blockTrialCount: plan.blockTrialCount,
      startedAtIso: new Date(Date.now() - Math.max(0, now - state.trialStartedAtMs)).toISOString(),
      endedAtIso: new Date().toISOString(),
      elapsedMs: Math.round(now - state.trialStartedAtMs),
      canvas: { width: CANVAS_WIDTH, height: CANVAS_HEIGHT },
      targetShapes: clone(composition.shapes),
      strokes: clone(state.currentTraceStrokes),
      shapeTrace: createShapeTrace(state.currentTraceStrokes)
    };

    state.trials.push(record);
    state.trialIndexInComposition += 1;
    state.currentTraceStrokes = [];
    state.currentStroke = null;

    if (state.trialIndexInComposition >= TRIAL_PLAN.length) {
      if (state.currentCompositionIndex + 1 < TOTAL_COMPOSITIONS) {
        state.currentCompositionIndex += 1;
        const nextComposition = activeComposition();
        nextComposition.startedAtIso = new Date().toISOString();
        state.phase = "compose";
        state.activeTool = "select";
        state.selectedShapeId = null;
        state.trialIndexInComposition = 0;
        state.trialStartedAtMs = null;
        state.compositionStartedAtMs = performance.now();
      } else {
        state.phase = "complete";
        state.completedAtIso = new Date().toISOString();
        state.trialStartedAtMs = null;
      }
    } else {
      state.trialStartedAtMs = performance.now();
    }

    persistDraft();
    updateUI();
    renderCanvas();
  }

  function resetTrial() {
    if (state.phase !== "trace") {
      return;
    }
    state.resetEvents.push({
      compositionIndex: state.currentCompositionIndex,
      trialInComposition: state.trialIndexInComposition,
      atIso: new Date().toISOString(),
      elapsedMs: state.trialStartedAtMs === null ? 0 : Math.round(performance.now() - state.trialStartedAtMs),
      strokeCountBeforeReset: state.currentTraceStrokes.length
    });
    state.currentTraceStrokes = [];
    state.currentStroke = null;
    state.trialStartedAtMs = performance.now();
    persistDraft();
    updateUI();
    renderCanvas();
  }

  function undoStroke() {
    if (state.phase !== "trace" || state.currentTraceStrokes.length === 0) {
      return;
    }
    state.currentTraceStrokes.pop();
    state.currentStroke = null;
    updateUI();
    renderCanvas();
  }

  function updateUI() {
    const composition = activeComposition();
    sessionChipEl.textContent = state.sessionId;
    phaseChipEl.textContent =
      state.phase === "complete"
        ? "complete"
        : `composition ${state.currentCompositionIndex + 1} / ${TOTAL_COMPOSITIONS}`;

    for (const button of shapeToolsEl.querySelectorAll("button")) {
      button.classList.toggle("active", button.dataset.tool === state.activeTool);
      button.disabled = state.phase !== "compose";
    }

    actionsEl.textContent = "";
    if (state.phase === "compose") {
      actionsEl.append(
        commandButton("복제", duplicateSelectedShape, !selectedShape() || composition.shapes.length >= MAX_SHAPES),
        commandButton("삭제", deleteSelectedShape, !selectedShape()),
        commandButton("비우기", resetComposition, composition.shapes.length === 0),
        commandButton("구성 완료", finishComposition, composition.shapes.length === 0, "primary-button")
      );
    } else if (state.phase === "trace") {
      actionsEl.append(
        commandButton("되돌리기", undoStroke, state.currentTraceStrokes.length === 0),
        commandButton("다시 그리기", resetTrial, state.currentTraceStrokes.length === 0),
        commandButton("저장 후 다음", saveTrial, state.currentTraceStrokes.length === 0, "primary-button")
      );
    } else {
      actionsEl.append(commandButton("JSON 저장", downloadJson, false, "primary-button"), commandButton("CSV 요약 저장", downloadCsv));
    }

    if (state.phase === "compose") {
      phaseTitleEl.textContent = `도형 조합 ${state.currentCompositionIndex + 1} 지정`;
      phaseCopyEl.textContent = `도형은 최대 ${MAX_SHAPES}개까지 배치할 수 있습니다. 선택한 도형을 캔버스에 드래그한 뒤 이동, 크기, 회전을 조정하세요.`;
      progressBarEl.style.width = `${Math.round((state.trials.length / EXPECTED_TRIALS) * 100)}%`;
      progressTextEl.textContent = `${state.trials.length} / ${EXPECTED_TRIALS} trials saved`;
    } else if (state.phase === "trace") {
      const plan = TRIAL_PLAN[state.trialIndexInComposition];
      const globalDone = state.currentCompositionIndex * TRIAL_PLAN.length + state.trialIndexInComposition;
      phaseTitleEl.textContent = `${plan.blockLabel} 따라그리기`;
      phaseCopyEl.textContent = `현재 블록 ${plan.blockTrialIndex} / ${plan.blockTrialCount}, 도형 조합 ${state.currentCompositionIndex + 1} 기준으로 그립니다.`;
      progressBarEl.style.width = `${Math.round((globalDone / EXPECTED_TRIALS) * 100)}%`;
      progressTextEl.textContent = `${globalDone} / ${EXPECTED_TRIALS} trials saved`;
    } else {
      phaseTitleEl.textContent = "수집 완료";
      phaseCopyEl.textContent = "두 도형 조합의 모든 따라그리기 입력이 저장되었습니다.";
      progressBarEl.style.width = "100%";
      progressTextEl.textContent = `${state.trials.length} / ${EXPECTED_TRIALS} trials saved`;
    }

    canvasStatusEl.textContent = statusText();
    autosaveStatusEl.textContent = state.autosaveRestored ? "draft restored" : "draft saved";
    updateInspector();
  }

  function updateInspector() {
    inspectorEl.textContent = "";

    if (state.phase === "compose") {
      const composition = activeComposition();
      inspectorEl.append(blockLabel("도형 정보"));
      inspectorEl.append(metricList([
        ["현재 도형 수", `${composition.shapes.length} / ${MAX_SHAPES}`],
        ["선택 도구", toolLabel(state.activeTool)],
        ["선택 도형", selectedShape() ? selectedShape().type : "-"]
      ]));

      const shape = selectedShape();
      if (shape) {
        const grid = document.createElement("div");
        grid.className = "mini-grid";
        grid.append(
          numericField("X", shape.x, (value) => updateSelectedNumber("x", value)),
          numericField("Y", shape.y, (value) => updateSelectedNumber("y", value)),
          numericField("W", shape.w, (value) => updateSelectedNumber("w", Math.max(MIN_SHAPE_SIZE, value))),
          numericField("H", shape.h, (value) => updateSelectedNumber("h", Math.max(MIN_SHAPE_SIZE, value))),
          numericField("회전", radiansToDegrees(shape.rotation), (value) => updateSelectedRotation(value)),
          numericField("굵기", shape.lineWidth, (value) => updateSelectedNumber("lineWidth", clamp(value, 1, 12)))
        );
        inspectorEl.append(grid);
      }

      const hint = document.createElement("p");
      hint.className = "hint-box";
      hint.textContent = "선택 도형의 모서리 핸들은 크기, 위쪽 원형 핸들은 회전, 도형 내부 드래그는 위치를 바꿉니다.";
      inspectorEl.append(hint);
      return;
    }

    if (state.phase === "trace") {
      const plan = TRIAL_PLAN[state.trialIndexInComposition];
      inspectorEl.append(blockLabel("입력 정보"));
      inspectorEl.append(metricList([
        ["블록", plan.blockLabel],
        ["블록 내 순서", `${plan.blockTrialIndex} / ${plan.blockTrialCount}`],
        ["현재 stroke", `${state.currentTraceStrokes.length}`],
        ["현재 point", `${pointCount(state.currentTraceStrokes)}`]
      ]));
      return;
    }

    inspectorEl.append(blockLabel("결과"));
    inspectorEl.append(metricList([
      ["도형 조합", `${state.compositions.filter((item) => item.finalizedAtIso).length} / ${TOTAL_COMPOSITIONS}`],
      ["저장 trial", `${state.trials.length} / ${EXPECTED_TRIALS}`],
      ["총 stroke", `${state.trials.reduce((sum, trial) => sum + trial.strokes.length, 0)}`],
      ["총 point", `${state.trials.reduce((sum, trial) => sum + pointCount(trial.strokes), 0)}`]
    ]));
  }

  function updateSelectedNumber(key, value) {
    const shape = selectedShape();
    if (!shape || !Number.isFinite(value)) {
      return;
    }
    const before = clone(shape);
    shape[key] = round(value, key === "lineWidth" ? 1 : 2);
    shape.updatedAtMs = Math.round(performance.now() - state.compositionStartedAtMs);
    recordEditEvent("shape:inspect", { shapeId: shape.id, field: key, before, after: clone(shape) });
    persistDraft();
    updateUI();
    renderCanvas();
  }

  function updateSelectedRotation(degrees) {
    const shape = selectedShape();
    if (!shape || !Number.isFinite(degrees)) {
      return;
    }
    const before = clone(shape);
    shape.rotation = degreesToRadians(degrees);
    shape.updatedAtMs = Math.round(performance.now() - state.compositionStartedAtMs);
    recordEditEvent("shape:inspect", { shapeId: shape.id, field: "rotation", before, after: clone(shape) });
    persistDraft();
    updateUI();
    renderCanvas();
  }

  function renderCanvas() {
    ctx.clearRect(0, 0, CANVAS_WIDTH, CANVAS_HEIGHT);
    ctx.fillStyle = "#ffffff";
    ctx.fillRect(0, 0, CANVAS_WIDTH, CANVAS_HEIGHT);
    drawGrid();

    if (state.phase === "compose") {
      drawComposition(activeComposition().shapes, { selected: true, alpha: 1 });
      if (previewShape) {
        drawShape(previewShape, { preview: true });
      }
      if (activeComposition().shapes.length === 0 && !previewShape) {
        drawCanvasMessage("도형 툴을 선택하고 드래그하여 조합을 만듭니다.");
      }
      return;
    }

    if (state.phase === "trace") {
      drawComposition(activeComposition().shapes, { target: true, alpha: 0.34 });
      drawTraceStrokes(state.currentTraceStrokes);
      if (state.currentTraceStrokes.length === 0) {
        const plan = TRIAL_PLAN[state.trialIndexInComposition];
        drawCanvasMessage(`${plan.blockLabel}: 같은 캔버스 위에 따라 그리세요.`);
      }
      return;
    }

    drawComposition(state.compositions[0].shapes, { target: true, alpha: 0.22, offsetX: -180 });
    drawComposition(state.compositions[1].shapes, { target: true, alpha: 0.22, offsetX: 180 });
    drawCanvasMessage("수집 완료. JSON 저장 버튼으로 원자료를 저장하세요.");
  }

  function drawGrid() {
    ctx.save();
    ctx.strokeStyle = "#edf1f5";
    ctx.lineWidth = 1;
    for (let x = 50; x < CANVAS_WIDTH; x += 50) {
      ctx.beginPath();
      ctx.moveTo(x, 0);
      ctx.lineTo(x, CANVAS_HEIGHT);
      ctx.stroke();
    }
    for (let y = 50; y < CANVAS_HEIGHT; y += 50) {
      ctx.beginPath();
      ctx.moveTo(0, y);
      ctx.lineTo(CANVAS_WIDTH, y);
      ctx.stroke();
    }
    ctx.restore();
  }

  function drawComposition(shapes, options = {}) {
    ctx.save();
    ctx.globalAlpha = options.alpha ?? 1;
    ctx.translate(options.offsetX ?? 0, options.offsetY ?? 0);
    for (const shape of shapes) {
      drawShape(shape, options);
    }
    ctx.restore();

    if (options.selected) {
      const selected = selectedShape();
      if (selected) {
        drawSelection(selected);
      }
    }
  }

  function drawShape(shape, options = {}) {
    ctx.save();
    const center = centerOf(shape);
    ctx.translate(center.x, center.y);
    ctx.rotate(shape.rotation);
    ctx.lineWidth = options.target ? Math.max(2, shape.lineWidth) : shape.lineWidth;
    ctx.lineCap = "round";
    ctx.lineJoin = "round";
    ctx.strokeStyle = options.preview ? "#126e82" : options.target ? "#31414c" : shape.stroke;
    ctx.fillStyle = options.target ? "rgba(49, 65, 76, 0.04)" : shape.fill;
    if (options.preview) {
      ctx.setLineDash([8, 7]);
    }

    drawShapePath(ctx, shape.type, shape.w, shape.h);

    if (shape.type === "line" || shape.type === "arrow" || shape.type === "elbow" || shape.type === "arc" || shape.type === "curve" || shape.type === "wave" || shape.type === "braceL" || shape.type === "braceR") {
      ctx.stroke();
    } else {
      ctx.fill();
      ctx.stroke();
    }
    ctx.restore();
  }

  function drawShapePath(context, type, w, h) {
    const left = -w / 2;
    const right = w / 2;
    const top = -h / 2;
    const bottom = h / 2;
    context.beginPath();

    switch (type) {
      case "line":
        context.moveTo(left, 0);
        context.lineTo(right, 0);
        break;
      case "arrow":
        context.moveTo(left, 0);
        context.lineTo(right - 18, 0);
        context.moveTo(right - 18, -10);
        context.lineTo(right, 0);
        context.lineTo(right - 18, 10);
        break;
      case "rect":
        context.rect(left, top, w, h);
        break;
      case "roundRect":
        roundRectPath(context, left, top, w, h, Math.min(22, w / 4, h / 4));
        break;
      case "ellipse":
        context.ellipse(0, 0, w / 2, h / 2, 0, 0, Math.PI * 2);
        break;
      case "triangle":
        context.moveTo(0, top);
        context.lineTo(right, bottom);
        context.lineTo(left, bottom);
        context.closePath();
        break;
      case "diamond":
        context.moveTo(0, top);
        context.lineTo(right, 0);
        context.lineTo(0, bottom);
        context.lineTo(left, 0);
        context.closePath();
        break;
      case "elbow":
        context.moveTo(left, top);
        context.lineTo(left, 0);
        context.lineTo(right, 0);
        break;
      case "rightArrow":
        context.moveTo(left, top + h * 0.25);
        context.lineTo(right - w * 0.28, top + h * 0.25);
        context.lineTo(right - w * 0.28, top);
        context.lineTo(right, 0);
        context.lineTo(right - w * 0.28, bottom);
        context.lineTo(right - w * 0.28, bottom - h * 0.25);
        context.lineTo(left, bottom - h * 0.25);
        context.closePath();
        break;
      case "downArrow":
        context.moveTo(left + w * 0.25, top);
        context.lineTo(right - w * 0.25, top);
        context.lineTo(right - w * 0.25, bottom - h * 0.28);
        context.lineTo(right, bottom - h * 0.28);
        context.lineTo(0, bottom);
        context.lineTo(left, bottom - h * 0.28);
        context.lineTo(left + w * 0.25, bottom - h * 0.28);
        context.closePath();
        break;
      case "arc":
        context.arc(0, 0, Math.min(w, h) / 2, Math.PI * 0.12, Math.PI * 1.42);
        break;
      case "curve":
        context.moveTo(left, bottom * 0.3);
        context.bezierCurveTo(left + w * 0.24, top, right - w * 0.28, bottom, right, top * 0.2);
        break;
      case "wave":
        context.moveTo(left, 0);
        context.bezierCurveTo(left + w * 0.18, top, left + w * 0.32, bottom, left + w * 0.5, 0);
        context.bezierCurveTo(left + w * 0.68, top, left + w * 0.82, bottom, right, 0);
        break;
      case "braceL":
        context.moveTo(right, top);
        context.bezierCurveTo(left, top, right, -h * 0.25, left, 0);
        context.bezierCurveTo(right, h * 0.25, left, bottom, right, bottom);
        break;
      case "braceR":
        context.moveTo(left, top);
        context.bezierCurveTo(right, top, left, -h * 0.25, right, 0);
        context.bezierCurveTo(left, h * 0.25, right, bottom, left, bottom);
        break;
      default:
        context.rect(left, top, w, h);
    }
  }

  function drawSelection(shape) {
    const handles = selectionHandles(shape);
    const corners = ["nw", "ne", "se", "sw"].map((id) => handles.find((handle) => handle.id === id));

    ctx.save();
    ctx.strokeStyle = "#126e82";
    ctx.lineWidth = 1.5;
    ctx.setLineDash([6, 5]);
    ctx.beginPath();
    corners.forEach((handle, index) => {
      if (index === 0) {
        ctx.moveTo(handle.x, handle.y);
      } else {
        ctx.lineTo(handle.x, handle.y);
      }
    });
    ctx.closePath();
    ctx.stroke();
    ctx.setLineDash([]);

    const top = handles.find((handle) => handle.id === "n");
    const rotate = handles.find((handle) => handle.id === "rotate");
    ctx.beginPath();
    ctx.moveTo(top.x, top.y);
    ctx.lineTo(rotate.x, rotate.y);
    ctx.stroke();

    for (const handle of handles) {
      ctx.beginPath();
      ctx.fillStyle = handle.kind === "rotate" ? "#126e82" : "#ffffff";
      ctx.strokeStyle = "#126e82";
      ctx.lineWidth = 1.6;
      ctx.arc(handle.x, handle.y, handle.kind === "rotate" ? HANDLE_RADIUS + 1 : HANDLE_RADIUS, 0, Math.PI * 2);
      ctx.fill();
      ctx.stroke();
    }
    ctx.restore();
  }

  function drawTraceStrokes(strokes) {
    ctx.save();
    ctx.strokeStyle = "#111820";
    ctx.lineWidth = 4.2;
    ctx.lineCap = "round";
    ctx.lineJoin = "round";
    for (const stroke of strokes) {
      if (stroke.samples.length === 0) {
        continue;
      }
      ctx.beginPath();
      ctx.moveTo(stroke.samples[0].x, stroke.samples[0].y);
      for (const sample of stroke.samples.slice(1)) {
        ctx.lineTo(sample.x, sample.y);
      }
      ctx.stroke();
    }
    ctx.restore();
  }

  function drawCanvasMessage(text) {
    ctx.save();
    ctx.fillStyle = "rgba(255,255,255,0.88)";
    ctx.strokeStyle = "#d9e0e7";
    roundRectPath(ctx, 240, 278, 520, 84, 8);
    ctx.fill();
    ctx.stroke();
    ctx.fillStyle = "#425160";
    ctx.font = '700 18px "Segoe UI", Arial, sans-serif';
    ctx.textAlign = "center";
    ctx.textBaseline = "middle";
    ctx.fillText(text, CANVAS_WIDTH / 2, CANVAS_HEIGHT / 2);
    ctx.restore();
  }

  function roundRectPath(context, x, y, w, h, r) {
    context.beginPath();
    context.moveTo(x + r, y);
    context.lineTo(x + w - r, y);
    context.quadraticCurveTo(x + w, y, x + w, y + r);
    context.lineTo(x + w, y + h - r);
    context.quadraticCurveTo(x + w, y + h, x + w - r, y + h);
    context.lineTo(x + r, y + h);
    context.quadraticCurveTo(x, y + h, x, y + h - r);
    context.lineTo(x, y + r);
    context.quadraticCurveTo(x, y, x + r, y);
    context.closePath();
  }

  function createShapeFromDrag(type, start, end, preview) {
    const dx = end.x - start.x;
    const dy = end.y - start.y;

    if (type === "line" || type === "arrow") {
      const length = Math.max(MIN_SHAPE_SIZE, Math.hypot(dx, dy));
      const center = { x: start.x + dx / 2, y: start.y + dy / 2 };
      return baseShape(type, center.x - length / 2, center.y - 16, length, 32, Math.atan2(dy, dx), preview);
    }

    const x = Math.min(start.x, end.x);
    const y = Math.min(start.y, end.y);
    const w = Math.max(MIN_SHAPE_SIZE, Math.abs(dx));
    const h = Math.max(MIN_SHAPE_SIZE, Math.abs(dy));
    return baseShape(type, x, y, w, h, 0, preview);
  }

  function baseShape(type, x, y, w, h, rotation, preview) {
    const nowMs = Math.round(performance.now() - state.compositionStartedAtMs);
    return {
      id: preview ? "preview" : compactId(),
      type,
      x: round(clamp(x, -w + 10, CANVAS_WIDTH - 10), 2),
      y: round(clamp(y, -h + 10, CANVAS_HEIGHT - 10), 2),
      w: round(w, 2),
      h: round(h, 2),
      rotation,
      stroke: "#18222c",
      fill: "rgba(255,255,255,0)",
      lineWidth: type === "rightArrow" || type === "downArrow" ? 3 : 3.5,
      createdAtMs: nowMs,
      updatedAtMs: nowMs
    };
  }

  function applyResize(shape, startShape, handleId, point) {
    const center = centerOf(startShape);
    const local = worldToLocal(startShape, point);
    let left = -startShape.w / 2;
    let right = startShape.w / 2;
    let top = -startShape.h / 2;
    let bottom = startShape.h / 2;

    if (handleId.includes("w")) {
      left = Math.min(right - MIN_SHAPE_SIZE, local.x);
    }
    if (handleId.includes("e")) {
      right = Math.max(left + MIN_SHAPE_SIZE, local.x);
    }
    if (handleId.includes("n")) {
      top = Math.min(bottom - MIN_SHAPE_SIZE, local.y);
    }
    if (handleId.includes("s")) {
      bottom = Math.max(top + MIN_SHAPE_SIZE, local.y);
    }

    const localCenter = { x: (left + right) / 2, y: (top + bottom) / 2 };
    const worldCenter = rotatePoint(localCenter, startShape.rotation);
    shape.w = round(right - left, 2);
    shape.h = round(bottom - top, 2);
    shape.x = round(center.x + worldCenter.x - shape.w / 2, 2);
    shape.y = round(center.y + worldCenter.y - shape.h / 2, 2);
    shape.updatedAtMs = Math.round(performance.now() - state.compositionStartedAtMs);
  }

  function shapeAtPoint(point) {
    const shapes = activeComposition().shapes;
    for (let index = shapes.length - 1; index >= 0; index -= 1) {
      if (pointInShape(shapes[index], point)) {
        return shapes[index];
      }
    }
    return null;
  }

  function pointInShape(shape, point) {
    const local = worldToLocal(shape, point);
    const tolerance = Math.max(10, shape.lineWidth + 6);
    return Math.abs(local.x) <= shape.w / 2 + tolerance && Math.abs(local.y) <= shape.h / 2 + tolerance;
  }

  function selectionHandles(shape) {
    const points = [
      ["nw", -shape.w / 2, -shape.h / 2, "resize"],
      ["n", 0, -shape.h / 2, "resize"],
      ["ne", shape.w / 2, -shape.h / 2, "resize"],
      ["e", shape.w / 2, 0, "resize"],
      ["se", shape.w / 2, shape.h / 2, "resize"],
      ["s", 0, shape.h / 2, "resize"],
      ["sw", -shape.w / 2, shape.h / 2, "resize"],
      ["w", -shape.w / 2, 0, "resize"],
      ["rotate", 0, -shape.h / 2 - 38, "rotate"]
    ];
    const center = centerOf(shape);
    return points.map(([id, x, y, kind]) => {
      const rotated = rotatePoint({ x, y }, shape.rotation);
      return { id, kind, x: center.x + rotated.x, y: center.y + rotated.y };
    });
  }

  function handleAtPoint(shape, point) {
    for (const handle of selectionHandles(shape).reverse()) {
      if (Math.hypot(handle.x - point.x, handle.y - point.y) <= HANDLE_RADIUS + 7) {
        return handle;
      }
    }
    return null;
  }

  function pointFromEvent(event) {
    const rect = canvas.getBoundingClientRect();
    return {
      x: clamp(((event.clientX - rect.left) / rect.width) * CANVAS_WIDTH, 0, CANVAS_WIDTH),
      y: clamp(((event.clientY - rect.top) / rect.height) * CANVAS_HEIGHT, 0, CANVAS_HEIGHT)
    };
  }

  function createShapeTrace(strokes) {
    const drawableStrokes = strokes.filter((stroke) => stroke.samples.length > 0);
    const startedAtMs = drawableStrokes[0]?.samples[0]?.tMs ?? 0;

    return drawableStrokes.map((stroke) =>
      stroke.samples.map((sample) => [sample.x1000, sample.y1000, Math.max(0, sample.tMs - startedAtMs)])
    );
  }

  function recordEditEvent(type, payload) {
    activeComposition().editEvents.push({
      type,
      tMs: Math.max(0, Math.round(performance.now() - state.compositionStartedAtMs)),
      atIso: new Date().toISOString(),
      ...payload
    });
  }

  function buildPayload() {
    return {
      schemaVersion: SCHEMA_VERSION,
      sessionId: state.sessionId,
      participantId: state.participantId || undefined,
      startedAtIso: state.startedAtIso,
      completedAtIso: state.completedAtIso,
      userAgent: navigator.userAgent,
      locale: navigator.language,
      timezone: Intl.DateTimeFormat().resolvedOptions().timeZone,
      canvas: { width: CANVAS_WIDTH, height: CANVAS_HEIGHT },
      limits: {
        compositionCount: TOTAL_COMPOSITIONS,
        maxShapesPerComposition: MAX_SHAPES,
        trialsPerComposition: TRIAL_PLAN.length,
        expectedTotalTrials: EXPECTED_TRIALS
      },
      trialPlan: TRIAL_PLAN,
      compositions: clone(state.compositions),
      trials: clone(state.trials),
      interactionMetrics: {
        savedTrialCount: state.trials.length,
        resetEvents: clone(state.resetEvents),
        downloadEvents: clone(state.downloadEvents),
        autosaveRestored: state.autosaveRestored
      }
    };
  }

  function downloadJson() {
    state.downloadEvents.push({ type: "json", atIso: new Date().toISOString(), trialCount: state.trials.length });
    persistDraft();
    const payload = buildPayload();
    downloadBlob(
      `${state.sessionId}_shape-composition-survey.json`,
      JSON.stringify(payload, null, 2),
      "application/json"
    );
  }

  function downloadCsv() {
    state.downloadEvents.push({ type: "csv", atIso: new Date().toISOString(), trialCount: state.trials.length });
    persistDraft();
    const payload = buildPayload();
    const rows = [
      [
        "sessionId",
        "participantId",
        "compositionIndex",
        "trialGlobalIndex",
        "trialInComposition",
        "blockId",
        "blockLabel",
        "blockTrialIndex",
        "elapsedMs",
        "strokeCount",
        "pointCount",
        "startedAtIso",
        "endedAtIso"
      ]
    ];

    for (const trial of payload.trials) {
      rows.push([
        payload.sessionId,
        payload.participantId ?? "",
        trial.compositionIndex + 1,
        trial.trialGlobalIndex + 1,
        trial.trialInComposition + 1,
        trial.blockId,
        trial.blockLabel,
        trial.blockTrialIndex,
        trial.elapsedMs,
        trial.strokes.length,
        pointCount(trial.strokes),
        trial.startedAtIso,
        trial.endedAtIso
      ]);
    }

    downloadBlob(`${state.sessionId}_shape-composition-survey-summary.csv`, csvText(rows), "text/csv;charset=utf-8");
  }

  function downloadBlob(filename, text, type) {
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

  function csvText(rows) {
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

  function persistDraft() {
    window.clearTimeout(saveTimer);
    saveTimer = window.setTimeout(() => {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
      autosaveStatusEl.textContent = `saved ${new Date().toLocaleTimeString()}`;
    }, 80);
  }

  function restoreDraft() {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) {
        return null;
      }
      const parsed = JSON.parse(raw);
      if (parsed?.schemaVersion !== SCHEMA_VERSION || !Array.isArray(parsed.compositions)) {
        return null;
      }
      parsed.currentStroke = null;
      parsed.compositionStartedAtMs = performance.now();
      parsed.trialStartedAtMs = parsed.phase === "trace" ? performance.now() : null;
      return parsed;
    } catch {
      return null;
    }
  }

  function handleKeyboard(event) {
    const shape = selectedShape();
    if (event.key === "Escape") {
      state.activeTool = "select";
      state.selectedShapeId = null;
      updateUI();
      renderCanvas();
      return;
    }

    if (state.phase !== "compose" || !shape) {
      return;
    }

    if (event.key === "Delete" || event.key === "Backspace") {
      event.preventDefault();
      deleteSelectedShape();
      return;
    }

    const delta = event.shiftKey ? 10 : 1;
    const moves = {
      ArrowLeft: [-delta, 0],
      ArrowRight: [delta, 0],
      ArrowUp: [0, -delta],
      ArrowDown: [0, delta]
    };

    if (moves[event.key]) {
      event.preventDefault();
      const before = clone(shape);
      shape.x = round(clamp(shape.x + moves[event.key][0], -shape.w + 10, CANVAS_WIDTH - 10), 2);
      shape.y = round(clamp(shape.y + moves[event.key][1], -shape.h + 10, CANVAS_HEIGHT - 10), 2);
      shape.updatedAtMs = Math.round(performance.now() - state.compositionStartedAtMs);
      recordEditEvent("shape:keyboard-move", { shapeId: shape.id, before, after: clone(shape) });
      persistDraft();
      updateUI();
      renderCanvas();
    }
  }

  function activeComposition() {
    return state.compositions[state.currentCompositionIndex];
  }

  function selectedShape() {
    return findShape(state.selectedShapeId);
  }

  function findShape(id) {
    if (!id) {
      return null;
    }
    return activeComposition().shapes.find((shape) => shape.id === id) ?? null;
  }

  function centerOf(shape) {
    return { x: shape.x + shape.w / 2, y: shape.y + shape.h / 2 };
  }

  function worldToLocal(shape, point) {
    const center = centerOf(shape);
    return rotatePoint({ x: point.x - center.x, y: point.y - center.y }, -shape.rotation);
  }

  function rotatePoint(point, angle) {
    const cos = Math.cos(angle);
    const sin = Math.sin(angle);
    return {
      x: point.x * cos - point.y * sin,
      y: point.x * sin + point.y * cos
    };
  }

  function shapeChanged(left, right) {
    return (
      round(left.x, 2) !== round(right.x, 2) ||
      round(left.y, 2) !== round(right.y, 2) ||
      round(left.w, 2) !== round(right.w, 2) ||
      round(left.h, 2) !== round(right.h, 2) ||
      round(left.rotation, 4) !== round(right.rotation, 4)
    );
  }

  function resizeCanvas() {
    const dpr = window.devicePixelRatio || 1;
    canvas.width = Math.round(CANVAS_WIDTH * dpr);
    canvas.height = Math.round(CANVAS_HEIGHT * dpr);
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
  }

  function commandButton(text, onClick, disabled = false, className = "") {
    const button = document.createElement("button");
    button.type = "button";
    button.textContent = text;
    button.disabled = disabled;
    if (className) {
      button.className = className;
    }
    button.addEventListener("click", onClick);
    return button;
  }

  function numericField(label, value, onChange) {
    const field = document.createElement("label");
    field.className = "field";
    const span = document.createElement("span");
    span.textContent = label;
    const input = document.createElement("input");
    input.type = "number";
    input.step = label === "회전" ? "1" : "0.5";
    input.value = String(round(value, label === "회전" ? 0 : 1));
    input.addEventListener("change", () => onChange(Number(input.value)));
    field.append(span, input);
    return field;
  }

  function blockLabel(text) {
    const label = document.createElement("p");
    label.className = "block-label";
    label.textContent = text;
    return label;
  }

  function metricList(rows) {
    const list = document.createElement("div");
    list.className = "metric-list";
    for (const [label, value] of rows) {
      const row = document.createElement("div");
      row.className = "metric-row";
      const labelEl = document.createElement("span");
      labelEl.textContent = label;
      const valueEl = document.createElement("strong");
      valueEl.textContent = value;
      row.append(labelEl, valueEl);
      list.append(row);
    }
    return list;
  }

  function statusText() {
    if (state.phase === "compose") {
      const shape = selectedShape();
      return shape ? `${shape.type} selected` : "composition edit mode";
    }
    if (state.phase === "trace") {
      return `${state.currentTraceStrokes.length} strokes in current trial`;
    }
    return "complete";
  }

  function setCanvasStatus(text) {
    canvasStatusEl.textContent = text;
  }

  function toolLabel(id) {
    return SHAPE_TOOLS.find((tool) => tool.id === id)?.label ?? id;
  }

  function pointCount(strokes) {
    return strokes.reduce((sum, stroke) => sum + stroke.samples.length, 0);
  }

  function compactId() {
    if (crypto.randomUUID) {
      return crypto.randomUUID().replace(/-/g, "").slice(0, 18);
    }
    return `${Date.now().toString(36)}${Math.random().toString(36).slice(2, 10)}`;
  }

  function clamp(value, min, max) {
    return Math.max(min, Math.min(max, value));
  }

  function round(value, digits) {
    const factor = 10 ** digits;
    return Math.round(value * factor) / factor;
  }

  function clone(value) {
    return JSON.parse(JSON.stringify(value));
  }

  function degreesToRadians(degrees) {
    return (degrees / 180) * Math.PI;
  }

  function radiansToDegrees(radians) {
    return (radians / Math.PI) * 180;
  }

  function iconSvg(name) {
    const common = 'viewBox="0 0 24 24" fill="none" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"';
    const icons = {
      cursor: `<svg ${common}><path d="M5 3l8 17 2-7 6-3z"/></svg>`,
      line: `<svg ${common}><path d="M4 18L20 6"/></svg>`,
      arrow: `<svg ${common}><path d="M4 18L18 6"/><path d="M10 6h8v8"/></svg>`,
      rect: `<svg ${common}><rect x="4" y="6" width="16" height="12"/></svg>`,
      roundRect: `<svg ${common}><rect x="4" y="6" width="16" height="12" rx="3"/></svg>`,
      ellipse: `<svg ${common}><ellipse cx="12" cy="12" rx="8" ry="5"/></svg>`,
      triangle: `<svg ${common}><path d="M12 4l9 16H3z"/></svg>`,
      diamond: `<svg ${common}><path d="M12 3l9 9-9 9-9-9z"/></svg>`,
      elbow: `<svg ${common}><path d="M6 5v9h12"/><path d="M14 10l4 4-4 4"/></svg>`,
      rightArrow: `<svg ${common}><path d="M3 9h11V5l7 7-7 7v-4H3z"/></svg>`,
      downArrow: `<svg ${common}><path d="M9 3h6v10h4l-7 8-7-8h4z"/></svg>`,
      arc: `<svg ${common}><path d="M19 16a7 7 0 1 0-12 0"/></svg>`,
      curve: `<svg ${common}><path d="M4 17C8 4 16 20 20 7"/></svg>`,
      wave: `<svg ${common}><path d="M3 13c3-6 6 6 9 0s6 6 9 0"/></svg>`,
      braceL: `<svg ${common}><path d="M15 4c-5 0-2 6-6 8 4 2 1 8 6 8"/></svg>`,
      braceR: `<svg ${common}><path d="M9 4c5 0 2 6 6 8-4 2-1 8-6 8"/></svg>`
    };
    return icons[name] ?? icons.rect;
  }
})();
