import type { Stroke } from "../../src/recognizer/types";
import {
  SURVEY_CAPTURE_MODES,
  SURVEY_GUESS_WORDS,
  SURVEY_PROMPT_WORDS,
  SURVEY_SCHEMA_VERSION,
  experimentGroupLabel,
  guessWordLabel,
  promptWordLabel,
  validateSurveyRaffleContactPayload,
  validateSurveyResponsePayload
} from "../../src/survey/survey-contract";
import type {
  DirectDrawingRecord,
  EngineComparisonRecord,
  LikertScore,
  SurveyCaptureMode,
  SurveyGuessWord,
  SurveyPromptWord,
  SurveyResponsePayload,
  SurveyRaffleContactPayload,
  SurveySelfReportRecord,
  SurveySession,
  TutorialCaptureRecord,
  WordGuessTrialRecord
} from "../../src/survey/survey-contract";
import {
  ASCII_TUTORIAL_CONTRACTS,
  advanceAsciiTutorial,
  createAsciiTutorialState,
  directionLabel,
  renderAsciiRows,
  spellLabel,
  summarizeAsciiState
} from "./ascii-turn-engine";
import type { AsciiAction, AsciiSpell, AsciiTurnState, Direction } from "./ascii-turn-engine";

const API_BASE_URL = import.meta.env.VITE_SURVEY_API_URL ?? `${location.protocol}//${location.hostname}:4174`;
const STROKE_LIMIT = 3;
const CANVAS_WIDTH = 720;
const CANVAS_HEIGHT = 420;

type SurveyStage = "consent" | "draw" | "guess" | "tutorial" | "engine" | "self-report" | "submitted";

interface RaffleContactForm {
  phone: string;
  email: string;
}

interface AppState {
  stage: SurveyStage;
  session: SurveySession | null;
  stageStartedAt: number;
  directIndex: number;
  guessIndex: number;
  tutorialIndex: number;
  drawingStrokes: Stroke[];
  drawingStartedAt: number | null;
  currentStroke: Stroke | null;
  directDrawings: DirectDrawingRecord[];
  wordGuessTrials: WordGuessTrialRecord[];
  tutorialCaptures: TutorialCaptureRecord[];
  asciiTutorial: AsciiTurnState;
  engineComparison: EngineComparisonRecord | null;
  selfReport: SurveySelfReportRecord;
  raffleContact: RaffleContactForm;
  raffleContactSubmitted: boolean;
  effectPlayed: boolean;
  submitError: string | null;
}

const rootElement = document.querySelector<HTMLDivElement>("#survey-app");

if (!rootElement) {
  throw new Error("survey root not found");
}

const root = rootElement;
let state: AppState = createInitialState();
let canvas: HTMLCanvasElement | null = null;
let context: CanvasRenderingContext2D | null = null;
let audioContext: AudioContext | null = null;

render();

function createInitialState(): AppState {
  return {
    stage: "consent",
    session: null,
    stageStartedAt: performance.now(),
    directIndex: 0,
    guessIndex: 0,
    tutorialIndex: 0,
    drawingStrokes: [],
    drawingStartedAt: null,
    currentStroke: null,
    directDrawings: [],
    wordGuessTrials: [],
    tutorialCaptures: [],
    asciiTutorial: createAsciiTutorialState(),
    engineComparison: null,
    selfReport: {
      tutorialInstructionClarity: 3,
      tutorialLearningEfficiency: 3,
      scentHelpfulness: 3,
      overallClarity: 3,
      strengths: "",
      weaknesses: ""
    },
    raffleContact: {
      phone: "",
      email: ""
    },
    raffleContactSubmitted: false,
    effectPlayed: false,
    submitError: null
  };
}

function render(): void {
  root.textContent = "";
  root.append(createShell());

  canvas = root.querySelector<HTMLCanvasElement>("#survey-canvas");
  context = canvas?.getContext("2d") ?? null;

  if (canvas && context) {
    wireCanvas(canvas);
    drawInputCanvas();
  }
}

function createShell(): HTMLElement {
  const shell = element("div", "survey-shell");
  shell.append(createHeader(), createProgress(), createStageView());
  return shell;
}

function createHeader(): HTMLElement {
  const header = element("header", "survey-header");
  const titleWrap = element("div");
  titleWrap.append(
    element("p", "eyebrow", "Magic Symbol Survey"),
    element("h1", "", "도형 표현과 체험형 튜토리얼 설문"),
    paragraph("짧은 도형 표현, 단서 효과, 턴제 상호작용 튜토리얼이 응답 경험에 어떤 차이를 만드는지 확인합니다.", "header-copy")
  );
  const meta = element("div", "header-meta");
  meta.append(
    pill(state.session ? experimentGroupLabel(state.session.experimentGroup) : "세션 대기"),
    pill("익명 세션"),
    pill("외부 에셋 없음")
  );
  header.append(titleWrap, meta);
  return header;
}

function createProgress(): HTMLElement {
  const steps: Array<[SurveyStage, string]> = [
    ["consent", "동의"],
    ["draw", "표현"],
    ["guess", "추론"],
    ["tutorial", "튜토리얼"],
    ["engine", "상호작용"],
    ["self-report", "평가"],
    ["submitted", "제출"]
  ];
  const nav = element("nav", "progress-strip");

  for (const [stage, label] of steps) {
    const item = element("span", ["progress-item", stage === state.stage ? "active" : ""]);
    item.textContent = label;
    nav.append(item);
  }

  return nav;
}

function createStageView(): HTMLElement {
  switch (state.stage) {
    case "consent":
      return renderConsent();
    case "draw":
      return renderDrawingStage();
    case "guess":
      return renderGuessStage();
    case "tutorial":
      return renderTutorialStage();
    case "engine":
      return renderEngineStage();
    case "self-report":
      return renderSelfReportStage();
    case "submitted":
      return renderSubmittedStage();
  }
}

function renderConsent(): HTMLElement {
  const section = card("survey-card wide");
  section.append(
    element("p", "eyebrow", "안내"),
    element("h2", "", "설문 참여 전 확인"),
    paragraph("경품 추첨 참여는 선택 사항입니다. 원하시는 경우 전화번호 또는 이메일 중 하나를 남겨 주세요."),
    paragraph("추첨을 통해 스타벅스 아메리카노 기프티콘을 드립니다."),
    paragraph("연락처는 경품 추첨과 발송에만 사용하며, 추첨 후 즉시 파기합니다.")
  );

  const raffleBox = element("div", "raffle-contact");
  raffleBox.append(
    element("h3", "", "경품 추첨 연락처"),
    paragraph("비워 두어도 설문 참여에는 영향이 없습니다.", "friendly-copy"),
    textInputField("전화번호", "raffle-phone", "tel", "예: 010-1234-5678", state.raffleContact.phone, 40),
    textInputField("이메일", "raffle-email", "email", "예: name@example.com", state.raffleContact.email, 254)
  );

  if (state.submitError) {
    raffleBox.append(paragraph(state.submitError, "error-text"));
  }

  const start = button("동의하고 시작", "primary");
  start.addEventListener("click", startSurvey);
  section.append(raffleBox, actions(start));
  return section;
}

function renderDrawingStage(): HTMLElement {
  const word = SURVEY_PROMPT_WORDS[state.directIndex];
  const section = element("main", "stage-grid");
  const board = drawingBoard(
    `${promptWordLabel(word)}을 3획 이내 도형으로 표현`,
    "사전 지식 없이 바로 떠오르는 모양을 그려 주세요. 이 단계에서는 해석이나 점수를 보여주지 않습니다."
  );
  const side = card("survey-card");

  side.append(
    element("p", "eyebrow", "표현 과제"),
    element("h2", "", `${state.directIndex + 1} / ${SURVEY_PROMPT_WORDS.length}`),
    paragraph(`현재 단어: ${promptWordLabel(word)}`),
    drawingStatusPanel("draw")
  );

  const reset = button("다시 그리기");
  reset.addEventListener("click", () => clearDrawing());
  const next = button(state.directIndex === SURVEY_PROMPT_WORDS.length - 1 ? "표현 저장 완료" : "다음 단어", "primary");
  next.disabled = state.drawingStrokes.length === 0;
  next.addEventListener("click", saveDirectDrawing);
  side.append(actions(previousItemButton(), reset, next));
  section.append(board, side);
  return section;
}

function renderGuessStage(): HTMLElement {
  const targetWord = SURVEY_PROMPT_WORDS[state.guessIndex];
  const group = state.session?.experimentGroup ?? "shape_only";
  const hintsEnabled = group === "scent_effects" || group === "tutorial_quality";
  const section = element("main", "stage-grid");
  const prompt = card("survey-card canvas-card");

  prompt.append(
    element("p", "eyebrow", "단어 추론"),
    element("h2", "", `${state.guessIndex + 1} / ${SURVEY_PROMPT_WORDS.length}`),
    paragraph(hintsEnabled ? "도형과 함께 제공되는 짧은 효과음을 참고해 단어를 골라 주세요." : "도형만 보고 어떤 단어인지 골라 주세요."),
    renderPromptGlyph(targetWord, hintsEnabled)
  );

  if (hintsEnabled) {
    const play = button("효과음 재생");
    play.addEventListener("click", () => {
      state.effectPlayed = true;
      void playSoundEffect(targetWord).catch((error: unknown) => {
        console.warn("sound effect playback failed", error);
      });
      render();
    });
    prompt.append(play);
  }

  const side = card("survey-card");
  side.append(element("p", "eyebrow", "응답"), element("h2", "", "가장 가까운 단어"));
  const choices = element("div", "choice-grid");

  for (const word of SURVEY_GUESS_WORDS) {
    const choice = button(guessWordLabel(word));
    choice.addEventListener("click", () => saveGuessTrial(word));
    choices.append(choice);
  }

  side.append(choices, paragraph("선택하면 바로 다음 화면으로 넘어갑니다."), actions(previousItemButton()));
  section.append(prompt, side);
  return section;
}

function renderTutorialStage(): HTMLElement {
  const mode = SURVEY_CAPTURE_MODES[state.tutorialIndex];
  const targetWord: SurveyPromptWord = "fire";
  const section = element("main", "stage-grid");
  const board = drawingBoard(tutorialModeTitle(mode), tutorialModeInstruction(mode));
  const side = card("survey-card");

  side.append(
    element("p", "eyebrow", "3분 튜토리얼"),
    element("h2", "", `${state.tutorialIndex + 1} / ${SURVEY_CAPTURE_MODES.length}`),
    paragraph("아래 예시 도형을 보고 왼쪽 빈 캔버스에 따라 그려 주세요."),
    renderTutorialExample(targetWord),
    drawingStatusPanel("tutorial")
  );

  const reset = button("다시 그리기");
  reset.addEventListener("click", () => clearDrawing());
  const save = button(state.tutorialIndex === SURVEY_CAPTURE_MODES.length - 1 ? "튜토리얼 완료" : "다음 예시", "primary");
  save.disabled = state.drawingStrokes.length === 0;
  save.addEventListener("click", saveTutorialCapture);
  side.append(actions(previousItemButton(), reset, save));
  section.append(board, side);
  return section;
}

function renderEngineStage(): HTMLElement {
  const initialTutorial = createAsciiTutorialState();
  const section = element("main", "stage-grid engine-layout");
  const env = card("survey-card wide");
  env.append(
    element("p", "eyebrow", "TURN TUTORIAL"),
    element("h2", "", "50x50 ASCII 턴제 상호작용"),
    paragraph("버튼을 누르면 한 턴이 진행됩니다. 지형은 그대로 두고, 반응 상태만 화면에 우선 표시됩니다."),
    renderAsciiLegend(),
    renderContractList(),
    renderAsciiTutorial()
  );

  const form = card("survey-card wide");
  form.append(element("p", "eyebrow", "평가"), element("h2", "", "상호작용 안내 평가"));
  const turnRating = likertField("직접 눌러 보는 방식이 규칙 이해에 도움이 됨", "turn-rating");
  const contractRating = likertField("하단의 짧은 설명이 이해에 도움이 됨", "contract-rating");
  const preferred = selectField("더 도움이 된 단서", "preferred-mode", [
    ["turn_tutorial", "직접 조작"],
    ["contract_notes", "짧은 규칙 설명"],
    ["same", "비슷함"]
  ]);
  const next = button("평가 저장", "primary");
  next.addEventListener("click", () => {
    state.engineComparison = {
      turnTutorialRating: readLikert("turn-rating"),
      contractClarityRating: readLikert("contract-rating"),
      preferredMode: readSelect("preferred-mode") as EngineComparisonRecord["preferredMode"],
      interactionSummary: summarizeAsciiState(state.asciiTutorial),
      asciiBefore: renderAsciiRows(initialTutorial),
      asciiAfter: renderAsciiRows(state.asciiTutorial)
    };
    goToStage("self-report");
  });
  form.append(turnRating, contractRating, preferred, actions(previousItemButton(), next));

  section.append(env, form);
  return section;
}

function renderSelfReportStage(): HTMLElement {
  const section = card("survey-card wide");
  section.append(
    element("p", "eyebrow", "마무리"),
    element("h2", "", "튜토리얼과 안내 평가"),
    likertField("예시 도형 안내가 따라 그리기에 충분했나요?", "tutorial-instruction"),
    likertField("체험형 튜토리얼이 학습 효율을 높였나요?", "tutorial-efficiency"),
    likertField("효과음 단서가 단어 추론에 도움이 되었나요?", "scent-helpfulness"),
    likertField("전체 설문 흐름이 명확했나요?", "overall-clarity"),
    textAreaField("좋았던 점이 있었나요?", "strengths"),
    textAreaField("아쉬웠던 점이 있었나요?", "weaknesses")
  );

  const submit = button("응답 제출", "primary");
  submit.addEventListener("click", submitSurvey);
  const controls = actions(previousItemButton(), submit);

  if (state.submitError) {
    controls.append(paragraph(state.submitError, "error-text"));
  }

  section.append(controls);
  return section;
}

function renderSubmittedStage(): HTMLElement {
  const section = card("survey-card wide");
  section.append(
    element("p", "eyebrow", "제출 완료"),
    element("h2", "", "응답이 저장되었습니다."),
    paragraph("익명 세션 기준으로 설문 응답이 API 서버에 제출되었습니다."),
    metricRows([
      ["세션", state.session?.sessionId.slice(0, 8) ?? "unknown"],
      ["실험군", state.session ? experimentGroupLabel(state.session.experimentGroup) : "unknown"],
      ["경품 연락처", state.raffleContactSubmitted ? "별도 접수" : "미기재"]
    ])
  );
  return section;
}

function drawingBoard(title: string, copy: string): HTMLElement {
  const board = card("survey-card canvas-card");
  board.append(element("p", "eyebrow", "캔버스"), element("h2", "", title), paragraph(copy));
  const wrap = element("div", "canvas-wrap");
  const input = document.createElement("canvas");
  input.id = "survey-canvas";
  input.width = CANVAS_WIDTH;
  input.height = CANVAS_HEIGHT;
  wrap.append(input);
  board.append(wrap);
  const footer = element("div", "canvas-footer");
  footer.append(pill(state.drawingStrokes.length > 0 ? "입력됨" : "입력 전"), pill("마우스 또는 터치 입력"));
  board.append(footer);
  return board;
}

function currentElapsedMs(): number {
  return state.drawingStartedAt ? performance.now() - state.drawingStartedAt : 0;
}

function saveDirectDrawing(): void {
  const targetWord = SURVEY_PROMPT_WORDS[state.directIndex];
  state.directDrawings = state.directDrawings.slice(0, state.directIndex);
  state.directDrawings.push({
    targetWord,
    shapeTrace: createShapeTrace(state.drawingStrokes),
    elapsedMs: Math.round(currentElapsedMs())
  });
  state.directIndex += 1;
  clearDrawing(false);

  if (state.directIndex >= SURVEY_PROMPT_WORDS.length) {
    const group = state.session?.experimentGroup;
    goToStage(group === "tutorial_quality" ? "tutorial" : "guess");
    return;
  }

  render();
}

function saveGuessTrial(answer: SurveyGuessWord): void {
  const targetWord = SURVEY_PROMPT_WORDS[state.guessIndex];
  state.wordGuessTrials = state.wordGuessTrials.slice(0, state.guessIndex);
  state.wordGuessTrials.push({
    targetWord,
    answer,
    reactionMs: Math.round(performance.now() - state.stageStartedAt),
    hintsEnabled: state.session?.experimentGroup !== "shape_only",
    effectPlayed: state.effectPlayed
  });
  state.guessIndex += 1;
  state.effectPlayed = false;

  if (state.guessIndex >= SURVEY_PROMPT_WORDS.length) {
    goToStage(state.session?.experimentGroup === "tutorial_quality" ? "engine" : "tutorial");
    return;
  }

  state.stageStartedAt = performance.now();
  render();
}

function saveTutorialCapture(): void {
  const mode = SURVEY_CAPTURE_MODES[state.tutorialIndex];
  state.tutorialCaptures = state.tutorialCaptures.slice(0, state.tutorialIndex);
  state.tutorialCaptures.push({
    targetWord: "fire",
    mode,
    shapeTrace: createShapeTrace(state.drawingStrokes),
    elapsedMs: Math.round(currentElapsedMs())
  });
  state.tutorialIndex += 1;
  clearDrawing(false);

  if (state.tutorialIndex >= SURVEY_CAPTURE_MODES.length) {
    goToStage(state.session?.experimentGroup === "tutorial_quality" && state.wordGuessTrials.length === 0 ? "guess" : "engine");
    return;
  }

  render();
}

async function startSurvey(): Promise<void> {
  state.submitError = null;
  state.raffleContact = readRaffleContactForm();

  const contactError = validateRaffleContactForm(state.raffleContact);

  if (contactError) {
    state.submitError = contactError;
    render();
    return;
  }

  try {
    const response = await fetch(`${API_BASE_URL}/api/survey-session`, {
      method: "GET",
      credentials: "include",
      headers: { Accept: "application/json" }
    });

    if (!response.ok) {
      throw new Error(`session request failed: ${response.status}`);
    }

    state.session = (await response.json()) as SurveySession;
    goToStage("draw");
  } catch {
    state.submitError = "설문 API 세션을 만들지 못했습니다. API 서버가 실행 중인지 확인해 주세요.";
    render();
  }
}

async function submitSurvey(): Promise<void> {
  if (!state.session || !state.engineComparison) {
    state.submitError = "세션 또는 상호작용 평가 응답이 없습니다.";
    render();
    return;
  }

  state.selfReport = {
    tutorialInstructionClarity: readLikert("tutorial-instruction"),
    tutorialLearningEfficiency: readLikert("tutorial-efficiency"),
    scentHelpfulness: readLikert("scent-helpfulness"),
    overallClarity: readLikert("overall-clarity"),
    strengths: readTextArea("strengths"),
    weaknesses: readTextArea("weaknesses")
  };

  const submissionId = createClientId();
  const payload: SurveyResponsePayload = {
    schemaVersion: SURVEY_SCHEMA_VERSION,
    submissionId,
    sessionId: state.session.sessionId,
    experimentGroup: state.session.experimentGroup,
    consentAccepted: true,
    directDrawings: state.directDrawings,
    wordGuessTrials: state.wordGuessTrials,
    tutorialCaptures: state.tutorialCaptures,
    engineComparison: state.engineComparison,
    selfReport: state.selfReport
  };

  const validationErrors = validateSurveyResponsePayload(payload);

  if (validationErrors.length > 0) {
    state.submitError = validationErrors.slice(0, 3).join(" / ");
    render();
    return;
  }

  try {
    const response = await fetch(`${API_BASE_URL}/api/survey-responses`, {
      method: "POST",
      credentials: "include",
      headers: {
        "Content-Type": "application/json",
        "X-CSRF-Token": state.session.csrfToken
      },
      body: JSON.stringify(payload)
    });

    if (!response.ok) {
      throw new Error(`submit failed: ${response.status}`);
    }

    state.raffleContactSubmitted = await submitRaffleContact(submissionId);
    goToStage("submitted");
  } catch {
    state.submitError = "응답 제출에 실패했습니다. API 서버 상태와 네트워크를 확인해 주세요.";
    render();
  }
}

async function submitRaffleContact(submissionId: string): Promise<boolean> {
  if (!state.session) {
    return false;
  }

  const contact = normalizeRaffleContact(state.raffleContact);

  if (!contact.phone && !contact.email) {
    return false;
  }

  const payload: SurveyRaffleContactPayload = {
    schemaVersion: SURVEY_SCHEMA_VERSION,
    submissionId,
    sessionId: state.session.sessionId,
    ...contact
  };
  const errors = validateSurveyRaffleContactPayload(payload);

  if (errors.length > 0) {
    console.warn("raffle contact skipped", errors);
    return false;
  }

  const response = await fetch(`${API_BASE_URL}/api/survey-raffle-contact`, {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-Token": state.session.csrfToken
    },
    body: JSON.stringify(payload)
  });

  if (!response.ok) {
    console.warn("raffle contact submit failed", response.status);
    return false;
  }

  return true;
}

function clearDrawing(shouldRender = true): void {
  state.drawingStrokes = [];
  state.currentStroke = null;
  state.drawingStartedAt = null;

  if (shouldRender) {
    render();
  }
}

function goToStage(stage: SurveyStage, options: { resetEngine?: boolean } = {}): void {
  state.stage = stage;
  state.stageStartedAt = performance.now();

  if (stage === "engine" && options.resetEngine !== false) {
    state.asciiTutorial = createAsciiTutorialState();
  }

  clearDrawing(false);
  render();
}

function canGoToPreviousItem(): boolean {
  switch (state.stage) {
    case "consent":
    case "submitted":
      return false;
    case "draw":
      return state.directIndex > 0;
    case "guess":
      return state.guessIndex > 0 || state.directDrawings.length > 0 || state.tutorialCaptures.length > 0;
    case "tutorial":
      return state.tutorialIndex > 0 || state.directDrawings.length > 0 || state.wordGuessTrials.length > 0;
    case "engine":
      return state.wordGuessTrials.length > 0 || state.tutorialCaptures.length > 0;
    case "self-report":
      return true;
  }
}

function goToPreviousItem(): void {
  state.submitError = null;

  switch (state.stage) {
    case "draw":
      if (state.directIndex > 0) {
        rewindDirectDrawingTo(state.directIndex - 1);
      }
      return;
    case "guess":
      if (state.guessIndex > 0) {
        rewindGuessTo(state.guessIndex - 1);
        return;
      }

      if (state.session?.experimentGroup === "tutorial_quality" && state.tutorialCaptures.length > 0) {
        rewindTutorialTo(SURVEY_CAPTURE_MODES.length - 1);
        return;
      }

      rewindDirectDrawingTo(SURVEY_PROMPT_WORDS.length - 1);
      return;
    case "tutorial":
      if (state.tutorialIndex > 0) {
        rewindTutorialTo(state.tutorialIndex - 1);
        return;
      }

      if (state.wordGuessTrials.length > 0) {
        rewindGuessTo(SURVEY_PROMPT_WORDS.length - 1);
        return;
      }

      rewindDirectDrawingTo(SURVEY_PROMPT_WORDS.length - 1);
      return;
    case "engine":
      if (state.session?.experimentGroup === "tutorial_quality" && state.wordGuessTrials.length > 0) {
        rewindGuessTo(SURVEY_PROMPT_WORDS.length - 1);
        return;
      }

      if (state.tutorialCaptures.length > 0) {
        rewindTutorialTo(SURVEY_CAPTURE_MODES.length - 1);
        return;
      }

      rewindGuessTo(SURVEY_PROMPT_WORDS.length - 1);
      return;
    case "self-report":
      goToStage("engine", { resetEngine: false });
      return;
    case "consent":
    case "submitted":
      return;
  }
}

function rewindDirectDrawingTo(index: number): void {
  state.directIndex = clampIndex(index, SURVEY_PROMPT_WORDS.length);
  state.directDrawings = state.directDrawings.slice(0, state.directIndex);
  goToStage("draw");
}

function rewindGuessTo(index: number): void {
  state.guessIndex = clampIndex(index, SURVEY_PROMPT_WORDS.length);
  state.wordGuessTrials = state.wordGuessTrials.slice(0, state.guessIndex);
  state.effectPlayed = false;
  goToStage("guess");
}

function rewindTutorialTo(index: number): void {
  state.tutorialIndex = clampIndex(index, SURVEY_CAPTURE_MODES.length);
  state.tutorialCaptures = state.tutorialCaptures.slice(0, state.tutorialIndex);
  goToStage("tutorial");
}

function clampIndex(index: number, length: number): number {
  return Math.max(0, Math.min(index, length - 1));
}

function wireCanvas(input: HTMLCanvasElement): void {
  input.addEventListener("pointerdown", (event) => {
    if (state.drawingStrokes.length >= STROKE_LIMIT) {
      return;
    }

    const point = pointFromEvent(input, event);
    const now = performance.now();

    if (!state.drawingStartedAt) {
      state.drawingStartedAt = now;
    }

    state.currentStroke = {
      id: createClientId(),
      points: [{ ...point, t: Math.round(now), pressure: event.pressure || 0.5 }]
    };
    state.drawingStrokes.push(state.currentStroke);
    input.setPointerCapture(event.pointerId);
    drawInputCanvas();
  });

  input.addEventListener("pointermove", (event) => {
    if (!state.currentStroke) {
      return;
    }

    const point = pointFromEvent(input, event);
    const last = state.currentStroke.points[state.currentStroke.points.length - 1];

    if (Math.hypot(point.x - last.x, point.y - last.y) < 1.4) {
      return;
    }

    state.currentStroke.points.push({
      ...point,
      t: Math.round(performance.now()),
      pressure: event.pressure || 0.5
    });
    drawInputCanvas();
  });

  const stop = (event: PointerEvent) => {
    if (!state.currentStroke) {
      return;
    }

    const point = pointFromEvent(input, event);
    const last = state.currentStroke.points[state.currentStroke.points.length - 1];

    if (Math.hypot(point.x - last.x, point.y - last.y) >= 1.4) {
      state.currentStroke.points.push({
        ...point,
        t: Math.round(performance.now()),
        pressure: event.pressure || 0.5
      });
    }

    state.currentStroke = null;
    render();
  };

  input.addEventListener("pointerup", stop);
  input.addEventListener("pointercancel", stop);
}

function drawInputCanvas(): void {
  if (!canvas || !context) {
    return;
  }

  context.clearRect(0, 0, canvas.width, canvas.height);
  context.fillStyle = "#ffffff";
  context.fillRect(0, 0, canvas.width, canvas.height);
  context.strokeStyle = "#d7dde3";
  context.lineWidth = 1;

  for (let x = 60; x < canvas.width; x += 60) {
    context.beginPath();
    context.moveTo(x, 0);
    context.lineTo(x, canvas.height);
    context.stroke();
  }

  for (let y = 60; y < canvas.height; y += 60) {
    context.beginPath();
    context.moveTo(0, y);
    context.lineTo(canvas.width, y);
    context.stroke();
  }

  context.strokeStyle = "#16232e";
  context.lineWidth = 4;
  context.lineCap = "round";
  context.lineJoin = "round";

  for (const stroke of state.drawingStrokes) {
    drawStroke(context, stroke);
  }
}

function createClientId(): string {
  if (typeof crypto.randomUUID === "function") {
    return crypto.randomUUID();
  }

  const bytes = new Uint8Array(16);
  crypto.getRandomValues(bytes);
  bytes[6] = (bytes[6] & 0x0f) | 0x40;
  bytes[8] = (bytes[8] & 0x3f) | 0x80;
  const hex = Array.from(bytes, (byte) => byte.toString(16).padStart(2, "0"));

  return [
    hex.slice(0, 4).join(""),
    hex.slice(4, 6).join(""),
    hex.slice(6, 8).join(""),
    hex.slice(8, 10).join(""),
    hex.slice(10, 16).join("")
  ].join("-");
}

function renderPromptGlyph(word: SurveyPromptWord, withEffect: boolean): HTMLElement {
  const wrap = element("div", ["prompt-glyph", withEffect ? `effect-${word}` : ""]);
  const glyph = document.createElement("canvas");
  glyph.width = 360;
  glyph.height = 240;
  const ctx = glyph.getContext("2d");

  if (ctx) {
    ctx.fillStyle = "#ffffff";
    ctx.fillRect(0, 0, glyph.width, glyph.height);
    ctx.strokeStyle = withEffect ? effectColor(word) : "#17202a";
    ctx.lineWidth = 7;
    ctx.lineCap = "round";
    ctx.lineJoin = "round";
    drawCanonicalGlyph(ctx, word, glyph.width, glyph.height);
  }

  wrap.append(glyph);
  return wrap;
}

function renderTutorialExample(word: SurveyPromptWord): HTMLElement {
  const wrap = element("div", "tutorial-example");
  wrap.append(element("span", "mini-label", "예시 도형"));
  const glyph = document.createElement("canvas");
  glyph.width = 300;
  glyph.height = 190;
  const ctx = glyph.getContext("2d");

  if (ctx) {
    ctx.fillStyle = "#ffffff";
    ctx.fillRect(0, 0, glyph.width, glyph.height);
    ctx.strokeStyle = "#17202a";
    ctx.lineWidth = 6;
    ctx.lineCap = "round";
    ctx.lineJoin = "round";
    drawCanonicalGlyph(ctx, word, glyph.width, glyph.height);
  }

  wrap.append(glyph);
  return wrap;
}

function drawCanonicalGlyph(ctx: CanvasRenderingContext2D, word: SurveyPromptWord, width: number, height: number): void {
  const cx = width / 2;
  const cy = height / 2;

  if (word === "fire") {
    ctx.beginPath();
    ctx.moveTo(cx, 44);
    ctx.lineTo(cx + 82, height - 52);
    ctx.lineTo(cx - 82, height - 52);
    ctx.closePath();
    ctx.stroke();
    return;
  }

  if (word === "water") {
    ctx.beginPath();
    ctx.ellipse(cx, cy, 82, 70, 0, 0, Math.PI * 2);
    ctx.stroke();
    return;
  }

  for (const offset of [-46, 0, 46]) {
    ctx.beginPath();
    ctx.moveTo(cx - 104, cy + offset);
    ctx.lineTo(cx + 104, cy + offset - 10);
    ctx.stroke();
  }
}

async function playSoundEffect(word: SurveyPromptWord): Promise<void> {
  const AudioContextConstructor = window.AudioContext ?? getWebkitAudioContext();

  if (!AudioContextConstructor) {
    console.warn("AudioContext is not available in this browser.");
    return;
  }

  audioContext ??= new AudioContextConstructor();
  const ctx = audioContext;
  if (ctx.state === "suspended") {
    await ctx.resume();
  }

  const now = ctx.currentTime;

  if (word === "fire") {
    playFireSound(ctx, now);
    return;
  }

  if (word === "water") {
    playWaterSound(ctx, now);
    return;
  }

  playWindSound(ctx, now);
}

function getWebkitAudioContext(): typeof AudioContext | undefined {
  return (window as Window & { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
}

function playFireSound(ctx: AudioContext, now: number): void {
  const noise = createNoiseSource(ctx, 0.32);
  const filter = ctx.createBiquadFilter();
  const gain = ctx.createGain();
  const ember = ctx.createOscillator();
  const emberGain = ctx.createGain();

  filter.type = "bandpass";
  filter.frequency.setValueAtTime(1600, now);
  filter.Q.setValueAtTime(0.9, now);

  gain.gain.setValueAtTime(0.0001, now);
  gain.gain.exponentialRampToValueAtTime(0.045, now + 0.02);
  gain.gain.exponentialRampToValueAtTime(0.012, now + 0.12);
  gain.gain.exponentialRampToValueAtTime(0.0001, now + 0.32);

  ember.type = "triangle";
  ember.frequency.setValueAtTime(95, now);
  ember.frequency.exponentialRampToValueAtTime(62, now + 0.18);
  emberGain.gain.setValueAtTime(0.0001, now);
  emberGain.gain.exponentialRampToValueAtTime(0.025, now + 0.025);
  emberGain.gain.exponentialRampToValueAtTime(0.0001, now + 0.22);

  noise.connect(filter);
  filter.connect(gain);
  gain.connect(ctx.destination);
  ember.connect(emberGain);
  emberGain.connect(ctx.destination);
  noise.start(now);
  noise.stop(now + 0.32);
  ember.start(now);
  ember.stop(now + 0.22);
}

function playWaterSound(ctx: AudioContext, now: number): void {
  const splash = createNoiseSource(ctx, 0.34);
  const filter = ctx.createBiquadFilter();
  const splashGain = ctx.createGain();
  const drop = ctx.createOscillator();
  const dropGain = ctx.createGain();

  filter.type = "highpass";
  filter.frequency.setValueAtTime(760, now);
  splashGain.gain.setValueAtTime(0.0001, now);
  splashGain.gain.exponentialRampToValueAtTime(0.035, now + 0.015);
  splashGain.gain.exponentialRampToValueAtTime(0.0001, now + 0.3);

  drop.type = "sine";
  drop.frequency.setValueAtTime(720, now + 0.04);
  drop.frequency.exponentialRampToValueAtTime(330, now + 0.2);
  dropGain.gain.setValueAtTime(0.0001, now);
  dropGain.gain.exponentialRampToValueAtTime(0.04, now + 0.045);
  dropGain.gain.exponentialRampToValueAtTime(0.0001, now + 0.26);

  splash.connect(filter);
  filter.connect(splashGain);
  splashGain.connect(ctx.destination);
  drop.connect(dropGain);
  dropGain.connect(ctx.destination);
  splash.start(now);
  splash.stop(now + 0.34);
  drop.start(now + 0.04);
  drop.stop(now + 0.26);
}

function playWindSound(ctx: AudioContext, now: number): void {
  const wind = createNoiseSource(ctx, 0.48);
  const filter = ctx.createBiquadFilter();
  const gain = ctx.createGain();

  filter.type = "bandpass";
  filter.frequency.setValueAtTime(520, now);
  filter.frequency.exponentialRampToValueAtTime(1500, now + 0.42);
  filter.Q.setValueAtTime(0.65, now);
  gain.gain.setValueAtTime(0.0001, now);
  gain.gain.exponentialRampToValueAtTime(0.038, now + 0.08);
  gain.gain.exponentialRampToValueAtTime(0.0001, now + 0.48);

  wind.connect(filter);
  filter.connect(gain);
  gain.connect(ctx.destination);
  wind.start(now);
  wind.stop(now + 0.48);
}

function createNoiseSource(ctx: AudioContext, durationSeconds: number): AudioBufferSourceNode {
  const frameCount = Math.max(1, Math.floor(ctx.sampleRate * durationSeconds));
  const buffer = ctx.createBuffer(1, frameCount, ctx.sampleRate);
  const data = buffer.getChannelData(0);

  for (let index = 0; index < frameCount; index += 1) {
    data[index] = Math.random() * 2 - 1;
  }

  const source = ctx.createBufferSource();
  source.buffer = buffer;
  return source;
}

function pointFromEvent(input: HTMLCanvasElement, event: PointerEvent): { x: number; y: number } {
  const rect = input.getBoundingClientRect();
  return {
    x: ((event.clientX - rect.left) / rect.width) * input.width,
    y: ((event.clientY - rect.top) / rect.height) * input.height
  };
}

function drawingStatusPanel(context: "draw" | "tutorial"): HTMLElement {
  const panel = element("div", "friendly-note");
  const hasInput = state.drawingStrokes.length > 0;
  const title = hasInput ? "입력됨" : "입력 전";
  const note = hasInput
    ? context === "tutorial"
      ? "예시 도형을 따라 그린 입력이 준비되었습니다."
      : "입력이 준비되었습니다. 다음 단어로 넘어갈 수 있습니다."
    : context === "tutorial"
      ? "예시 도형을 보고 캔버스에 따라 그려 주세요."
      : "떠오르는 모양을 그대로 그려 주세요.";

  panel.append(element("strong", "", title), paragraph(note, "friendly-copy"));
  return panel;
}

function createShapeTrace(strokes: Stroke[]): Array<Array<[number, number]>> {
  return strokes
    .filter((stroke) => stroke.points.length >= 1)
    .map((stroke) => {
      const count = Math.min(stroke.points.length, 32);

      return Array.from({ length: count }, (_, index) => {
        const sourceIndex = count <= 1 ? 0 : Math.round((index / (count - 1)) * (stroke.points.length - 1));
        const point = stroke.points[sourceIndex];
        return [
          Math.round((point.x / CANVAS_WIDTH) * 1000),
          Math.round((point.y / CANVAS_HEIGHT) * 1000)
        ];
      });
    });
}

function renderAsciiTutorial(): HTMLElement {
  const wrap = element("div", "turn-tutorial");
  wrap.append(
    metricRows([
      ["현재 턴", `${state.asciiTutorial.turn}`],
      ["위치", `${state.asciiTutorial.player.row}, ${state.asciiTutorial.player.column}`],
      ["방향", directionLabel(state.asciiTutorial.player.facing)],
      ["최근 입력", state.asciiTutorial.lastAction]
    ]),
    renderMoveControls(),
    renderSpellControls(),
    asciiBlock("50x50 map", renderAsciiRows(state.asciiTutorial), "large"),
    renderTurnLogs(state.asciiTutorial.log)
  );
  return wrap;
}

function renderMoveControls(): HTMLElement {
  const group = element("div", "turn-control-group");
  group.append(element("span", "mini-label", "이동"));
  const pad = element("div", "direction-pad");
  const controls: Array<[string, Direction, string]> = [
    ["↑", "north", "위로 이동"],
    ["←", "west", "왼쪽으로 이동"],
    ["→", "east", "오른쪽으로 이동"],
    ["↓", "south", "아래로 이동"]
  ];

  for (const [label, direction, title] of controls) {
    const control = button(label);
    control.title = title;
    control.addEventListener("click", () => runAsciiAction({ type: "move", direction }));
    pad.append(control);
  }

  group.append(pad);
  return group;
}

function renderSpellControls(): HTMLElement {
  const group = element("div", "turn-control-group");
  group.append(element("span", "mini-label", "상호작용"));
  const controls = element("div", "spell-controls");
  const spells: AsciiSpell[] = ["fire", "water", "wind", "earth", "life", "electric", "ice", "void"];

  for (const spell of spells) {
    const control = button(spellLabel(spell));
    control.title = `${spellLabel(spell)}: 현재 바라보는 방향으로 적용`;
    control.addEventListener("click", () => runAsciiAction({ type: "cast", spell }));
    controls.append(control);
  }

  const wait = button("대기");
  wait.addEventListener("click", () => runAsciiAction({ type: "wait" }));
  const reset = button("초기화");
  reset.addEventListener("click", () => {
    state.asciiTutorial = createAsciiTutorialState();
    render();
  });
  controls.append(wait, reset);
  group.append(controls);
  return group;
}

function runAsciiAction(action: AsciiAction): void {
  state.asciiTutorial = advanceAsciiTutorial(state.asciiTutorial, action);
  render();
}

function renderAsciiLegend(): HTMLElement {
  const legend = element("div", "legend-grid");
  const items: Array<[string, string]> = [
    ["^ > v <", "플레이어"],
    ["t", "나무"],
    ["f", "불붙음"],
    ["~ / =", "물 / 얼음"],
    ["M", "금속"],
    ["#", "벽"],
    ["*", "진행 이펙트"],
    ["w/e/i/s/g/x", "반응 상태"]
  ];

  for (const [symbol, label] of items) {
    const item = element("span", "legend-item");
    item.append(element("strong", "", symbol), document.createTextNode(label));
    legend.append(item);
  }

  return legend;
}

function renderContractList(): HTMLElement {
  const list = element("div", "contract-list");

  for (const item of ASCII_TUTORIAL_CONTRACTS) {
    const row = element("div", "contract-item");
    row.append(element("strong", "", item.label), element("span", "", item.contract));
    list.append(row);
  }

  return list;
}

function asciiBlock(label: string, rows: string[], sizeClass = ""): HTMLElement {
  const wrap = element("div", ["ascii-block", sizeClass]);
  wrap.append(element("span", "mini-label", label));
  const pre = element("pre");
  pre.textContent = rows.join("\n");
  wrap.append(pre);
  return wrap;
}

function renderTurnLogs(logs: string[]): HTMLElement {
  const list = element("ul", "turn-log");

  for (const log of logs) {
    const item = element("li");
    item.textContent = log;
    list.append(item);
  }

  return list;
}

function metricRows(rows: Array<[string, string]>): HTMLElement {
  const list = element("div", "metric-list");

  for (const [label, value] of rows) {
    const row = element("div", "metric-row");
    row.append(element("span", "", label), element("strong", "", value));
    list.append(row);
  }

  return list;
}

function likertField(label: string, id: string): HTMLElement {
  const field = element("label", "field");
  field.append(element("span", "", label));
  const select = document.createElement("select");
  select.id = id;

  for (const score of [1, 2, 3, 4, 5]) {
    const option = document.createElement("option");
    option.value = String(score);
    option.textContent = `${score}`;
    option.selected = score === 3;
    select.append(option);
  }

  field.append(select);
  return field;
}

function textAreaField(label: string, id: string): HTMLElement {
  const field = element("label", "field");
  field.append(element("span", "", label));
  const textarea = document.createElement("textarea");
  textarea.id = id;
  textarea.maxLength = 1000;
  textarea.rows = 4;
  field.append(textarea);
  return field;
}

function textInputField(
  label: string,
  id: string,
  type: "email" | "tel" | "text",
  placeholder: string,
  value: string,
  maxLength: number
): HTMLElement {
  const field = element("label", "field");
  field.append(element("span", "", label));
  const input = document.createElement("input");
  input.id = id;
  input.type = type;
  input.placeholder = placeholder;
  input.value = value;
  input.maxLength = maxLength;
  input.autocomplete = type === "email" ? "email" : "tel";
  field.append(input);
  return field;
}

function selectField(label: string, id: string, options: Array<[string, string]>): HTMLElement {
  const field = element("label", "field");
  field.append(element("span", "", label));
  const select = document.createElement("select");
  select.id = id;

  for (const [value, text] of options) {
    const option = document.createElement("option");
    option.value = value;
    option.textContent = text;
    select.append(option);
  }

  field.append(select);
  return field;
}

function readLikert(id: string): LikertScore {
  const value = Number(root.querySelector<HTMLSelectElement>(`#${id}`)?.value ?? "3");
  return [1, 2, 3, 4, 5].includes(value) ? (value as LikertScore) : 3;
}

function readSelect(id: string): string {
  return root.querySelector<HTMLSelectElement>(`#${id}`)?.value ?? "";
}

function readTextArea(id: string): string {
  return root.querySelector<HTMLTextAreaElement>(`#${id}`)?.value.trim().slice(0, 1000) ?? "";
}

function readTextInput(id: string, maxLength: number): string {
  return root.querySelector<HTMLInputElement>(`#${id}`)?.value.trim().slice(0, maxLength) ?? "";
}

function readRaffleContactForm(): RaffleContactForm {
  return normalizeRaffleContact({
    phone: readTextInput("raffle-phone", 40),
    email: readTextInput("raffle-email", 254)
  });
}

function normalizeRaffleContact(contact: RaffleContactForm): RaffleContactForm {
  return {
    phone: contact.phone.trim().slice(0, 40),
    email: contact.email.trim().slice(0, 254)
  };
}

function validateRaffleContactForm(contact: RaffleContactForm): string | null {
  if (!contact.phone && !contact.email) {
    return null;
  }

  if (contact.phone && !/^[0-9+\-()\s]{8,30}$/.test(contact.phone)) {
    return "전화번호 형식을 확인해 주세요. 비워 두어도 설문 참여가 가능합니다.";
  }

  if (contact.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(contact.email)) {
    return "이메일 형식을 확인해 주세요. 비워 두어도 설문 참여가 가능합니다.";
  }

  return null;
}

function actions(...children: HTMLElement[]): HTMLElement {
  const wrap = element("div", "actions");
  wrap.append(...children);
  return wrap;
}

function previousItemButton(): HTMLButtonElement {
  const control = button("이전 항목");
  control.disabled = !canGoToPreviousItem();
  control.addEventListener("click", goToPreviousItem);
  return control;
}

function card(className = "survey-card"): HTMLElement {
  return element("section", className);
}

function paragraph(text: string, className = "copy"): HTMLParagraphElement {
  return element("p", className, text) as HTMLParagraphElement;
}

function pill(text: string): HTMLElement {
  return element("span", "pill", text);
}

function button(text: string, className = ""): HTMLButtonElement {
  const control = document.createElement("button");
  control.type = "button";
  control.className = className;
  control.textContent = text;
  return control;
}

function element(tag: string, className?: string | string[], text?: string): HTMLElement {
  const node = document.createElement(tag);

  if (Array.isArray(className)) {
    node.className = className.filter(Boolean).join(" ");
  } else if (className) {
    node.className = className;
  }

  if (text !== undefined) {
    node.textContent = text;
  }

  return node;
}

function drawStroke(ctx: CanvasRenderingContext2D, stroke: Stroke): void {
  if (stroke.points.length === 0) {
    return;
  }

  ctx.beginPath();
  ctx.moveTo(stroke.points[0].x, stroke.points[0].y);

  for (const point of stroke.points.slice(1)) {
    ctx.lineTo(point.x, point.y);
  }

  ctx.stroke();
}

function tutorialModeTitle(mode: SurveyCaptureMode): string {
  switch (mode) {
    case "ideal":
      return "제시된 예시 도형을 따라 그리시오";
    case "fast":
      return "같은 예시 도형을 빠르게 따라 그리시오";
    case "comfortable":
      return "같은 예시 도형을 편하게 따라 그리시오";
  }
}

function tutorialModeInstruction(mode: SurveyCaptureMode): string {
  switch (mode) {
    case "ideal":
      return "예시 도형을 보고 가능한 한 비슷하게 따라 그려 주세요.";
    case "fast":
      return "같은 예시를 보며 이번에는 조금 빠르게 따라 그려 주세요.";
    case "comfortable":
      return "같은 예시를 보며 이번에는 편한 속도로 따라 그려 주세요.";
  }
}

function effectColor(word: SurveyPromptWord): string {
  switch (word) {
    case "fire":
      return "#b3261e";
    case "water":
      return "#0b69a3";
    case "wind":
      return "#207561";
  }
}
