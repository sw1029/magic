import type { GlyphFamily } from "../recognizer/types";

export const SURVEY_SCHEMA_VERSION = "magic-symbol-survey-v8";

export const SURVEY_PROMPT_WORDS = ["fire", "water", "wind"] as const;
export type SurveyPromptWord = (typeof SURVEY_PROMPT_WORDS)[number];

export const SURVEY_GUESS_WORDS = ["fire", "water", "wind", "tree", "stone", "lightning"] as const;
export type SurveyGuessWord = (typeof SURVEY_GUESS_WORDS)[number];

export const SURVEY_EXPERIMENT_GROUPS = ["shape_only", "scent_effects", "tutorial_quality"] as const;
export type SurveyExperimentGroup = (typeof SURVEY_EXPERIMENT_GROUPS)[number];

export const SURVEY_HCI_PROBE_VARIANTS = ["log_discovery", "goal_scaffold"] as const;
export type SurveyHciProbeVariant = (typeof SURVEY_HCI_PROBE_VARIANTS)[number];

export const SURVEY_CAPTURE_MODES = ["ideal", "fast", "comfortable"] as const;
export type SurveyCaptureMode = (typeof SURVEY_CAPTURE_MODES)[number];

export type LikertScore = 1 | 2 | 3 | 4 | 5;
export type LikertOrNotApplicable = LikertScore | "not_applicable";
export type SurveyEffectHeard = "yes" | "no" | "not_applicable";
export type ShapeTracePoint = [x: number, y: number, tMs: number];
export type ShapeTrace = Array<Array<ShapeTracePoint>>;
export type SurveyStageName = "consent" | "draw" | "guess" | "tutorial" | "engine" | "self-report";

const FORBIDDEN_DRAWING_FIELDS = [
  "strokes",
  "strokeLimit",
  "strokeCount",
  "recognizedFamily",
  "recognitionStatus",
  "quality",
  "inputNote"
] as const;
const FORBIDDEN_GUESS_TRIAL_FIELDS = ["trialId", "correct"] as const;
const FORBIDDEN_RESPONSE_FIELDS = ["phone", "email", "raffleContact"] as const;

export interface SurveySession {
  sessionId: string;
  csrfToken: string;
  experimentGroup: SurveyExperimentGroup;
  hciProbeVariant: SurveyHciProbeVariant;
}

export interface DirectDrawingRecord {
  targetWord: SurveyPromptWord;
  shapeTrace: ShapeTrace;
  elapsedMs: number;
  expressionDifficulty: LikertScore;
  expressionReason: string;
}

export interface WordGuessTrialRecord {
  targetWord: SurveyPromptWord;
  answer: SurveyGuessWord;
  confidence: LikertScore;
  reactionMs: number;
  hintsEnabled: boolean;
  effectPlayed: boolean;
  effectPlayCount: number;
  effectHeard: SurveyEffectHeard;
}

export interface TutorialCaptureRecord {
  targetWord: SurveyPromptWord;
  mode: SurveyCaptureMode;
  shapeTrace: ShapeTrace;
  elapsedMs: number;
}

export interface EngineComparisonRecord {
  understandingBefore: LikertScore;
  understandingAfter: LikertScore;
  taskDifficulty: LikertScore;
  turnTutorialRating: LikertScore;
  contractClarityRating: LikertScore;
  preferredMode: "turn_tutorial" | "contract_notes" | "same";
  interactionSummary: string;
  asciiBefore: string[];
  asciiAfter: string[];
  actionLog: AsciiActionLogRecord[];
  goals: AsciiGoalRecord[];
  actionCount: number;
}

export interface SurveySelfReportRecord {
  tutorialInstructionClarity: LikertScore;
  tutorialLearningEfficiency: LikertScore;
  scentHelpfulness: LikertOrNotApplicable;
  overallClarity: LikertScore;
  workloadRating: LikertScore;
  strengths: string;
  weaknesses: string;
}

export interface AsciiActionLogRecord {
  turn: number;
  action: string;
  player: {
    row: number;
    column: number;
    facing: string;
  };
  result: string;
}

export interface AsciiGoalRecord {
  id: "move" | "ignite" | "observe";
  label: string;
  completed: boolean;
  completedTurn?: number;
}

export interface SurveyInteractionMetricsRecord {
  promptOrder: SurveyPromptWord[];
  stageDurationsMs: Partial<Record<SurveyStageName, number>>;
  previousClicks: number;
  resetCounts: {
    directDrawing: number;
    tutorialCapture: number;
    asciiTutorial: number;
  };
}

export interface SurveyResponsePayload {
  schemaVersion: typeof SURVEY_SCHEMA_VERSION;
  submissionId: string;
  sessionId: string;
  experimentGroup: SurveyExperimentGroup;
  hciProbeVariant: SurveyHciProbeVariant;
  consentAccepted: boolean;
  directDrawings: DirectDrawingRecord[];
  wordGuessTrials: WordGuessTrialRecord[];
  tutorialCaptures: TutorialCaptureRecord[];
  engineComparison: EngineComparisonRecord;
  selfReport: SurveySelfReportRecord;
  interactionMetrics: SurveyInteractionMetricsRecord;
}

export interface SurveyRaffleContactPayload {
  schemaVersion: typeof SURVEY_SCHEMA_VERSION;
  submissionId: string;
  sessionId: string;
  phone?: string;
  email?: string;
}

export function assignExperimentGroup(seed: string): SurveyExperimentGroup {
  let hash = 2166136261;

  for (let index = 0; index < seed.length; index += 1) {
    hash ^= seed.charCodeAt(index);
    hash = Math.imul(hash, 16777619);
  }

  return SURVEY_EXPERIMENT_GROUPS[Math.abs(hash) % SURVEY_EXPERIMENT_GROUPS.length];
}

export function assignHciProbeVariant(seed: string): SurveyHciProbeVariant {
  let hash = 2166136261;

  for (let index = 0; index < seed.length; index += 1) {
    hash ^= seed.charCodeAt(index);
    hash = Math.imul(hash, 16777619);
  }

  return SURVEY_HCI_PROBE_VARIANTS[Math.abs(hash) % SURVEY_HCI_PROBE_VARIANTS.length];
}

export function promptWordLabel(word: SurveyPromptWord): string {
  switch (word) {
    case "fire":
      return "불";
    case "water":
      return "물";
    case "wind":
      return "바람";
  }
}

export function guessWordLabel(word: SurveyGuessWord): string {
  switch (word) {
    case "fire":
    case "water":
    case "wind":
      return promptWordLabel(word);
    case "tree":
      return "나무";
    case "stone":
      return "돌";
    case "lightning":
      return "번개";
  }
}

export function promptWordFamily(word: SurveyPromptWord): GlyphFamily {
  switch (word) {
    case "fire":
      return "fire";
    case "water":
      return "water";
    case "wind":
      return "wind";
  }
}

export function experimentGroupLabel(group: SurveyExperimentGroup): string {
  switch (group) {
    case "shape_only":
      return "A: 도형만";
    case "scent_effects":
      return "B: 도형 + 효과";
    case "tutorial_quality":
      return "C: 튜토리얼 + 짧은 안내";
  }
}

export function hciProbeVariantLabel(variant: SurveyHciProbeVariant): string {
  switch (variant) {
    case "log_discovery":
      return "로그 탐색형";
    case "goal_scaffold":
      return "목표 스캐폴드형";
  }
}

export function validateSurveyResponsePayload(value: unknown): string[] {
  const errors: string[] = [];

  if (!isRecord(value)) {
    return ["payload must be an object"];
  }

  for (const field of FORBIDDEN_RESPONSE_FIELDS) {
    if (field in value) {
      errors.push(`${field} must not be submitted with survey response`);
    }
  }

  requireString(value, "schemaVersion", errors);
  requireString(value, "submissionId", errors);
  requireString(value, "sessionId", errors);

  if (value.schemaVersion !== SURVEY_SCHEMA_VERSION) {
    errors.push(`schemaVersion must be ${SURVEY_SCHEMA_VERSION}`);
  }

  if (!isExperimentGroup(value.experimentGroup)) {
    errors.push("experimentGroup is invalid");
  }

  if (!isHciProbeVariant(value.hciProbeVariant)) {
    errors.push("hciProbeVariant is invalid");
  }

  if (value.consentAccepted !== true) {
    errors.push("consentAccepted must be true");
  }

  validateSubmissionId(value.submissionId, errors);
  validateStringLength(value.sessionId, "sessionId", 16, 128, errors);

  validateDirectDrawings(value.directDrawings, errors);
  validateGuessTrials(value.wordGuessTrials, errors);
  validateTutorialCaptures(value.tutorialCaptures, errors);
  validateEngineComparison(value.engineComparison, errors);
  validateSelfReport(value.selfReport, errors);
  validateInteractionMetrics(value.interactionMetrics, errors);

  return errors;
}

export function validateSurveyRaffleContactPayload(value: unknown): string[] {
  const errors: string[] = [];

  if (!isRecord(value)) {
    return ["raffleContact must be an object"];
  }

  requireString(value, "schemaVersion", errors);
  requireString(value, "submissionId", errors);
  requireString(value, "sessionId", errors);

  if (value.schemaVersion !== SURVEY_SCHEMA_VERSION) {
    errors.push(`schemaVersion must be ${SURVEY_SCHEMA_VERSION}`);
  }

  validateSubmissionId(value.submissionId, errors);
  validateStringLength(value.sessionId, "sessionId", 16, 128, errors);

  const phone = optionalTrimmedString(value.phone);
  const email = optionalTrimmedString(value.email);

  if (!phone && !email) {
    errors.push("phone or email is required for raffle contact");
  }

  if (phone && !/^[0-9+\-()\s]{8,30}$/.test(phone)) {
    errors.push("phone must contain 8-30 phone characters");
  }

  if (email && (email.length > 254 || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email))) {
    errors.push("email must be a valid email address");
  }

  return errors;
}

function validateDirectDrawings(value: unknown, errors: string[]): void {
  if (!Array.isArray(value) || value.length < SURVEY_PROMPT_WORDS.length) {
    errors.push("directDrawings must include each prompt word");
    return;
  }

  for (const word of SURVEY_PROMPT_WORDS) {
    if (!value.some((item) => isRecord(item) && item.targetWord === word)) {
      errors.push(`directDrawings missing ${word}`);
    }
  }

  value.forEach((item, index) => {
    if (!isRecord(item)) {
      errors.push(`directDrawings[${index}] must be an object`);
      return;
    }
    rejectForbiddenDrawingFields(item, `directDrawings[${index}]`, errors);
    validatePromptWord(item.targetWord, `directDrawings[${index}].targetWord`, errors);
    validateShapeTrace(item.shapeTrace, `directDrawings[${index}].shapeTrace`, errors);
    validateNumber(item.elapsedMs, `directDrawings[${index}].elapsedMs`, 0, 600000, errors);
    validateLikert(item.expressionDifficulty, `directDrawings[${index}].expressionDifficulty`, errors);
    validateStringLength(item.expressionReason, `directDrawings[${index}].expressionReason`, 0, 240, errors);
  });
}

function validateGuessTrials(value: unknown, errors: string[]): void {
  if (!Array.isArray(value) || value.length < SURVEY_PROMPT_WORDS.length) {
    errors.push("wordGuessTrials must include each prompt word");
    return;
  }

  value.forEach((item, index) => {
    if (!isRecord(item)) {
      errors.push(`wordGuessTrials[${index}] must be an object`);
      return;
    }
    rejectForbiddenGuessTrialFields(item, `wordGuessTrials[${index}]`, errors);
    validatePromptWord(item.targetWord, `wordGuessTrials[${index}].targetWord`, errors);
    validateGuessWord(item.answer, `wordGuessTrials[${index}].answer`, errors);
    validateLikert(item.confidence, `wordGuessTrials[${index}].confidence`, errors);
    if (typeof item.hintsEnabled !== "boolean") {
      errors.push(`wordGuessTrials[${index}].hintsEnabled must be boolean`);
    }
    if (typeof item.effectPlayed !== "boolean") {
      errors.push(`wordGuessTrials[${index}].effectPlayed must be boolean`);
    }
    validateNumber(item.effectPlayCount, `wordGuessTrials[${index}].effectPlayCount`, 0, 20, errors);
    validateEffectHeard(item.effectHeard, `wordGuessTrials[${index}].effectHeard`, errors);
    validateNumber(item.reactionMs, `wordGuessTrials[${index}].reactionMs`, 0, 600000, errors);
  });
}

function validateTutorialCaptures(value: unknown, errors: string[]): void {
  if (!Array.isArray(value) || value.length < SURVEY_CAPTURE_MODES.length) {
    errors.push("tutorialCaptures must include the three capture modes");
    return;
  }

  for (const mode of SURVEY_CAPTURE_MODES) {
    if (!value.some((item) => isRecord(item) && item.mode === mode)) {
      errors.push(`tutorialCaptures missing ${mode}`);
    }
  }

  value.forEach((item, index) => {
    if (!isRecord(item)) {
      errors.push(`tutorialCaptures[${index}] must be an object`);
      return;
    }
    rejectForbiddenDrawingFields(item, `tutorialCaptures[${index}]`, errors);
    validatePromptWord(item.targetWord, `tutorialCaptures[${index}].targetWord`, errors);
    if (!SURVEY_CAPTURE_MODES.includes(item.mode as SurveyCaptureMode)) {
      errors.push(`tutorialCaptures[${index}].mode is invalid`);
    }
    validateShapeTrace(item.shapeTrace, `tutorialCaptures[${index}].shapeTrace`, errors);
    validateNumber(item.elapsedMs, `tutorialCaptures[${index}].elapsedMs`, 0, 600000, errors);
  });
}

function validateShapeTrace(value: unknown, path: string, errors: string[]): void {
  if (!Array.isArray(value) || value.length === 0 || value.length > 8) {
    errors.push(`${path} must contain 1-8 simplified strokes`);
    return;
  }

  value.forEach((stroke, strokeIndex) => {
    if (!Array.isArray(stroke) || stroke.length < 1 || stroke.length > 64) {
      errors.push(`${path}[${strokeIndex}] must contain 1-64 points`);
      return;
    }

    stroke.forEach((point, pointIndex) => {
      if (
        !Array.isArray(point) ||
        point.length !== 3 ||
        !isCoordinate(point[0]) ||
        !isCoordinate(point[1]) ||
        !isRelativeTimestamp(point[2])
      ) {
        errors.push(
          `${path}[${strokeIndex}][${pointIndex}] must be [x,y,tMs] integers with x/y 0-1000 and tMs 0-600000`
        );
      }
    });
  });
}

function isCoordinate(value: unknown): boolean {
  return Number.isInteger(value) && Number(value) >= 0 && Number(value) <= 1000;
}

function isRelativeTimestamp(value: unknown): boolean {
  return Number.isInteger(value) && Number(value) >= 0 && Number(value) <= 600000;
}

function validateEngineComparison(value: unknown, errors: string[]): void {
  if (!isRecord(value)) {
    errors.push("engineComparison must be an object");
    return;
  }
  validateLikert(value.understandingBefore, "engineComparison.understandingBefore", errors);
  validateLikert(value.understandingAfter, "engineComparison.understandingAfter", errors);
  validateLikert(value.taskDifficulty, "engineComparison.taskDifficulty", errors);
  validateLikert(value.turnTutorialRating, "engineComparison.turnTutorialRating", errors);
  validateLikert(value.contractClarityRating, "engineComparison.contractClarityRating", errors);
  if (!["turn_tutorial", "contract_notes", "same"].includes(String(value.preferredMode))) {
    errors.push("engineComparison.preferredMode is invalid");
  }
  validateStringLength(value.interactionSummary, "engineComparison.interactionSummary", 1, 300, errors);
  validateAsciiRows(value.asciiBefore, "engineComparison.asciiBefore", errors);
  validateAsciiRows(value.asciiAfter, "engineComparison.asciiAfter", errors);
  validateAsciiActionLog(value.actionLog, errors);
  validateAsciiGoals(value.goals, errors);
  validateNumber(value.actionCount, "engineComparison.actionCount", 0, 200, errors);
}

function validateSelfReport(value: unknown, errors: string[]): void {
  if (!isRecord(value)) {
    errors.push("selfReport must be an object");
    return;
  }

  for (const key of [
    "tutorialInstructionClarity",
    "tutorialLearningEfficiency",
    "overallClarity",
    "workloadRating"
  ]) {
    validateLikert(value[key], `selfReport.${key}`, errors);
  }

  validateLikertOrNotApplicable(value.scentHelpfulness, "selfReport.scentHelpfulness", errors);
  validateStringLength(value.strengths, "selfReport.strengths", 0, 1000, errors);
  validateStringLength(value.weaknesses, "selfReport.weaknesses", 0, 1000, errors);
}

function validateAsciiActionLog(value: unknown, errors: string[]): void {
  if (!Array.isArray(value) || value.length > 120) {
    errors.push("engineComparison.actionLog must contain 0-120 items");
    return;
  }

  value.forEach((item, index) => {
    if (!isRecord(item)) {
      errors.push(`engineComparison.actionLog[${index}] must be an object`);
      return;
    }

    validateNumber(item.turn, `engineComparison.actionLog[${index}].turn`, 0, 200, errors);
    validateStringLength(item.action, `engineComparison.actionLog[${index}].action`, 1, 40, errors);
    validateStringLength(item.result, `engineComparison.actionLog[${index}].result`, 0, 300, errors);

    if (!isRecord(item.player)) {
      errors.push(`engineComparison.actionLog[${index}].player must be an object`);
      return;
    }

    validateNumber(item.player.row, `engineComparison.actionLog[${index}].player.row`, 0, 49, errors);
    validateNumber(item.player.column, `engineComparison.actionLog[${index}].player.column`, 0, 49, errors);
    validateStringLength(item.player.facing, `engineComparison.actionLog[${index}].player.facing`, 2, 12, errors);
  });
}

function validateAsciiGoals(value: unknown, errors: string[]): void {
  if (!Array.isArray(value) || value.length < 1 || value.length > 6) {
    errors.push("engineComparison.goals must contain 1-6 items");
    return;
  }

  value.forEach((item, index) => {
    if (!isRecord(item)) {
      errors.push(`engineComparison.goals[${index}] must be an object`);
      return;
    }

    if (!["move", "ignite", "observe"].includes(String(item.id))) {
      errors.push(`engineComparison.goals[${index}].id is invalid`);
    }
    validateStringLength(item.label, `engineComparison.goals[${index}].label`, 1, 80, errors);
    if (typeof item.completed !== "boolean") {
      errors.push(`engineComparison.goals[${index}].completed must be boolean`);
    }
    if (item.completedTurn !== undefined) {
      validateNumber(item.completedTurn, `engineComparison.goals[${index}].completedTurn`, 0, 200, errors);
    }
  });
}

function validateInteractionMetrics(value: unknown, errors: string[]): void {
  if (!isRecord(value)) {
    errors.push("interactionMetrics must be an object");
    return;
  }

  if (!Array.isArray(value.promptOrder) || value.promptOrder.length !== SURVEY_PROMPT_WORDS.length) {
    errors.push("interactionMetrics.promptOrder must include prompt words");
  } else {
    for (const word of SURVEY_PROMPT_WORDS) {
      if (!value.promptOrder.includes(word)) {
        errors.push(`interactionMetrics.promptOrder missing ${word}`);
      }
    }
  }

  if (!isRecord(value.stageDurationsMs)) {
    errors.push("interactionMetrics.stageDurationsMs must be an object");
  } else {
    for (const [stage, duration] of Object.entries(value.stageDurationsMs)) {
      if (!["consent", "draw", "guess", "tutorial", "engine", "self-report"].includes(stage)) {
        errors.push(`interactionMetrics.stageDurationsMs.${stage} is invalid`);
        continue;
      }
      validateNumber(duration, `interactionMetrics.stageDurationsMs.${stage}`, 0, 600000, errors);
    }
  }

  validateNumber(value.previousClicks, "interactionMetrics.previousClicks", 0, 200, errors);

  if (!isRecord(value.resetCounts)) {
    errors.push("interactionMetrics.resetCounts must be an object");
    return;
  }

  validateNumber(value.resetCounts.directDrawing, "interactionMetrics.resetCounts.directDrawing", 0, 200, errors);
  validateNumber(value.resetCounts.tutorialCapture, "interactionMetrics.resetCounts.tutorialCapture", 0, 200, errors);
  validateNumber(value.resetCounts.asciiTutorial, "interactionMetrics.resetCounts.asciiTutorial", 0, 200, errors);
}

function rejectForbiddenDrawingFields(value: Record<string, unknown>, path: string, errors: string[]): void {
  for (const field of FORBIDDEN_DRAWING_FIELDS) {
    if (field in value) {
      errors.push(`${path}.${field} must not be submitted`);
    }
  }
}

function rejectForbiddenGuessTrialFields(value: Record<string, unknown>, path: string, errors: string[]): void {
  for (const field of FORBIDDEN_GUESS_TRIAL_FIELDS) {
    if (field in value) {
      errors.push(`${path}.${field} must not be submitted`);
    }
  }
}

function validateAsciiRows(value: unknown, path: string, errors: string[]): void {
  if (!Array.isArray(value) || value.length !== 50) {
    errors.push(`${path} must contain 50 rows`);
    return;
  }

  value.forEach((row, index) => {
    validateStringLength(row, `${path}[${index}]`, 50, 50, errors);
  });
}

function validatePromptWord(value: unknown, path: string, errors: string[]): void {
  if (!SURVEY_PROMPT_WORDS.includes(value as SurveyPromptWord)) {
    errors.push(`${path} is invalid`);
  }
}

function validateGuessWord(value: unknown, path: string, errors: string[]): void {
  if (!SURVEY_GUESS_WORDS.includes(value as SurveyGuessWord)) {
    errors.push(`${path} is invalid`);
  }
}

function validateSubmissionId(value: unknown, errors: string[]): void {
  if (typeof value !== "string" || !/^[a-zA-Z0-9_-]{8,128}$/.test(value)) {
    errors.push("submissionId must be a compact id");
  }
}

function validateLikert(value: unknown, path: string, errors: string[]): void {
  if (![1, 2, 3, 4, 5].includes(Number(value))) {
    errors.push(`${path} must be a 1-5 score`);
  }
}

function validateLikertOrNotApplicable(value: unknown, path: string, errors: string[]): void {
  if (value === "not_applicable") {
    return;
  }

  validateLikert(value, path, errors);
}

function validateEffectHeard(value: unknown, path: string, errors: string[]): void {
  if (!["yes", "no", "not_applicable"].includes(String(value))) {
    errors.push(`${path} must be yes, no, or not_applicable`);
  }
}

function validateNumber(value: unknown, path: string, min: number, max: number, errors: string[]): void {
  if (typeof value !== "number" || !Number.isFinite(value) || value < min || value > max) {
    errors.push(`${path} must be a number between ${min} and ${max}`);
  }
}

function validateStringLength(value: unknown, path: string, min: number, max: number, errors: string[]): void {
  if (typeof value !== "string" || value.length < min || value.length > max) {
    errors.push(`${path} must be a string with length ${min}-${max}`);
  }
}

function requireString(
  value: Record<string, unknown>,
  key: string,
  errors: string[],
  prefix?: string
): void {
  const path = prefix ? `${prefix}.${key}` : key;
  if (typeof value[key] !== "string" || value[key].length === 0) {
    errors.push(`${path} is required`);
  }
}

function optionalTrimmedString(value: unknown): string {
  return typeof value === "string" ? value.trim() : "";
}

function isExperimentGroup(value: unknown): value is SurveyExperimentGroup {
  return SURVEY_EXPERIMENT_GROUPS.includes(value as SurveyExperimentGroup);
}

function isHciProbeVariant(value: unknown): value is SurveyHciProbeVariant {
  return SURVEY_HCI_PROBE_VARIANTS.includes(value as SurveyHciProbeVariant);
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return Boolean(value) && typeof value === "object" && !Array.isArray(value);
}
