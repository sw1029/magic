import { createHash } from "node:crypto";
import { appendFileSync, existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { basename, dirname, join, resolve } from "node:path";

import {
  DASHBOARD_MATRIX_FAMILIES,
  DASHBOARD_SCENARIO_PRESETS,
  buildSyntheticRecipeFromRange,
  type SyntheticInputRange
} from "../src/demo/dashboard-presets";
import { buildSyntheticStrokeSession, createSeededRandom, type SyntheticInputRecipe } from "../src/demo/synthetic-input";
import { recognizeSession } from "../src/recognizer/recognize";
import { getDynamicThresholdTable } from "../src/recognizer/dynamic-gating";
import {
  appendTutorialCapture,
  createEmptyTutorialProfileStore,
  mergeTutorializedUserProfile
} from "../src/recognizer/tutorial-profile";
import { createEmptyUserInputProfile } from "../src/recognizer/user-profile";
import { promptWordFamily, type SurveyPromptWord } from "../src/survey/survey-contract";
import { detectSurveyOutlierRespondents, type SurveyOutlierInput } from "../src/survey/survey-outlier";
import type {
  DynamicRecognitionSourceHint,
  GlyphFamily,
  PointSample,
  QualityVector,
  RecognitionResult,
  RecognitionStatus,
  Stroke,
  StrokeSession,
  TutorialCaptureSource,
  TutorialProfileStore,
  UserInputProfile
} from "../src/recognizer/types";

type PolicyId = "baseline" | "tutorial_warmup" | "ml_first" | "stat_guardrail" | "ml_guardrail_final" | "ml_guardrail_dynamic";
type SourceType =
  | "random_stratified"
  | "boundary_sweep"
  | "survey_mutation"
  | "survey_mutation_valid"
  | "risk_boundary"
  | "confusion_repair"
  | "balanced_holdout";
type StressAxis = "random" | "open_gap" | "jitter_noise" | "rotation_curve" | "mixed_borderline";
type ExperimentSplit = "calibration_train" | "validation_holdout";

interface SurveyRecord {
  sourceFile: string;
  sourceLine: number;
  payload: Record<string, unknown>;
  fingerprint: string;
}

interface NormalizedInput {
  inputId: string;
  responseIndex: number;
  submissionId: string;
  experimentGroup: string;
  captureKind: "direct" | "tutorial";
  targetWord: SurveyPromptWord;
  expectedFamily: GlyphFamily;
  mode: string;
  sourceFile: string;
  sourceLine: number;
  session: StrokeSession;
}

interface ExperimentCase {
  caseId: string;
  sourceType: SourceType;
  split: ExperimentSplit;
  expectedFamily: GlyphFamily;
  generationAxis: StressAxis;
  presetId: string;
  level: number;
  seed: number;
  session: StrokeSession;
  knobs: DiagnosticKnobs;
  sourceInputId?: string;
}

interface DiagnosticKnobs {
  jitterPx: number;
  openGapRatio: number;
  rotationDeg: number;
  curveWarp: number;
  extraNoiseStrokeCount: number;
  pointDensity: number;
}

interface RuntimeSnapshot {
  status: RecognitionStatus;
  topFamily: GlyphFamily | "none";
  acceptedFamily: GlyphFamily | "none";
  topScore: number;
  scoreGap: number;
  confidence: number;
  mlConfidenceGate: number;
  shadowTopFamily: GlyphFamily | "none";
  shadowStatus: RecognitionStatus | "none";
  shadowScore: number;
  probabilityGap: number;
  effectiveThresholdBias: number;
  personalizationStage: string;
  result: RecognitionResult;
}

interface PolicyDecision {
  policy: PolicyId;
  status: RecognitionStatus;
  decisionFamily: GlyphFamily | "none";
  acceptedFamily: GlyphFamily | "none";
  confidence: number;
  reason: string;
  guardrailSeverity: "none" | "caution" | "block";
  guardrailReasons: string[];
  changedFromBaseline: boolean;
}

interface RunConfig {
  cases: number;
  warmupSample: number;
  outDir: string;
  focusedDynamic: boolean;
}

const SURVEY_INPUT_FILES = [
  "data/survey-responses.ndjson",
  "survey-export-cdb7ffdf75f092448f9fc5cd2836652002b063d05614d637/data/survey-responses.ndjson"
];
const DEFAULT_CASES = 100_000;
const DEFAULT_OUTPUT_ROOT = "survey-analysis-output";
const BASE_SEED = 7_300_000;
const CLOSED_FAMILIES = new Set<GlyphFamily>(["earth", "fire", "water", "life"]);
const STATUS_RANK: Record<RecognitionStatus, number> = {
  invalid: 0,
  incomplete: 1,
  ambiguous: 2,
  recognized: 3
};
const CASE_COLUMNS = [
  "case_id",
  "source_type",
  "split",
  "expected_family",
  "generation_axis",
  "preset_id",
  "level",
  "seed",
  "source_input_id",
  "jitter_px",
  "open_gap_ratio",
  "rotation_deg",
  "curve_warp",
  "extra_noise_stroke_count",
  "point_density",
  "stroke_count",
  "point_count",
  "duration_ms",
  "path_length",
  "closure_gap",
  "dominant_corners",
  "endpoint_clusters",
  "circularity",
  "fill_ratio",
  "parallelism",
  "closure",
  "symmetry",
  "smoothness",
  "tempo",
  "overshoot",
  "stability",
  "rotation_bias"
];
const POLICY_COLUMNS = [
  "case_id",
  "source_type",
  "split",
  "expected_family",
  "generation_axis",
  "policy",
  "status",
  "decision_family",
  "accepted_family",
  "top_score",
  "score_gap",
  "confidence",
  "ml_confidence_gate",
  "effective_threshold_bias",
  "personalization_stage",
  "shadow_top_family",
  "shadow_status",
  "shadow_score",
  "probability_gap",
  "guardrail_severity",
  "guardrail_reasons",
  "reason",
  "changed_from_baseline",
  "is_correct_accept",
  "is_unsafe_accept",
  "is_overlap"
];
const WARMUP_COLUMNS = [
  "case_id",
  "source_type",
  "expected_family",
  "warmup_stage",
  "profile_capture_count",
  "status",
  "decision_family",
  "accepted_family",
  "top_score",
  "score_gap",
  "confidence",
  "ml_confidence_gate",
  "effective_threshold_bias",
  "personalization_stage",
  "changed_from_baseline",
  "is_correct_accept",
  "is_unsafe_accept"
];
const SURVEY_POLICY_COLUMNS = [
  "input_id",
  "submission_id",
  "target_word",
  "experiment_group",
  "policy",
  "status",
  "decision_family",
  "accepted_family",
  "top_score",
  "score_gap",
  "confidence",
  "ml_confidence_gate",
  "effective_threshold_bias",
  "personalization_stage",
  "guardrail_severity",
  "guardrail_reasons",
  "changed_from_baseline",
  "is_correct_accept",
  "is_unsafe_accept"
];

async function main(): Promise<void> {
  const config = parseArgs();
  mkdirSync(config.outDir, { recursive: true });

  const rawRecords = readSurveyRecords(SURVEY_INPUT_FILES);
  const dedupRecords = dedupeRecords(rawRecords);
  const surveyInputs = dedupRecords.flatMap((record, index) => normalizeSurveyInputs(record, index));
  const outlierReports = detectSurveyOutlierRespondents(surveyInputs.map(toSurveyOutlierInput));
  const outlierSubmissionIds = new Set(outlierReports.map((report) => report.submissionId));
  const calibrationSurveyInputs = config.focusedDynamic
    ? surveyInputs.filter((input) => !outlierSubmissionIds.has(input.submissionId))
    : surveyInputs;
  const tutorialInputs = surveyInputs.filter((input) => input.captureKind === "tutorial");
  const warmupProfiles = buildWarmupProfiles(tutorialInputs);
  const fullProfile = warmupProfiles[warmupProfiles.length - 1]?.profile ?? createEmptyUserInputProfile();
  const respondentProfiles = buildRespondentProfiles(surveyInputs);
  const allocation = allocateCaseCounts(config.cases, config.focusedDynamic);

  const caseWriter = new CsvWriter(join(config.outDir, "experiment_cases.csv"), CASE_COLUMNS);
  const policyWriter = new CsvWriter(join(config.outDir, "policy_decisions.csv"), POLICY_COLUMNS);
  const warmupWriter = new CsvWriter(join(config.outDir, "warmup_decisions.csv"), WARMUP_COLUMNS);
  const surveyWriter = new CsvWriter(join(config.outDir, "survey_actual_policy_decisions.csv"), SURVEY_POLICY_COLUMNS);

  let caseOrdinal = 0;
  const sourceCounts: Record<SourceType, number> = {
    random_stratified: 0,
    boundary_sweep: 0,
    survey_mutation: 0,
    survey_mutation_valid: 0,
    risk_boundary: 0,
    confusion_repair: 0,
    balanced_holdout: 0
  };

  writeOutlierReports(join(config.outDir, "excluded_survey_outliers.csv"), outlierReports);
  writeJson(join(config.outDir, "dynamic_policy_thresholds.json"), getDynamicThresholdTable());

  for await (const testCase of generateExperimentCases(config.cases, allocation, calibrationSurveyInputs, config.focusedDynamic)) {
    const sourceHint = sourceHintFor(testCase.sourceType);
    const baselineResult = recognizeSession(testCase.session, { sealed: true, policyMode: "legacy", sourceHint });
    const warmupResult = recognizeSession(testCase.session, { sealed: true, profile: fullProfile, policyMode: "legacy", sourceHint });
    const dynamicResult = recognizeSession(testCase.session, { sealed: true, profile: fullProfile, policyMode: "dynamic", sourceHint });
    const baseline = snapshotFromResult(baselineResult);
    const warmup = snapshotFromResult(warmupResult);
    const dynamic = snapshotFromResult(dynamicResult);
    const decisions = buildPolicyDecisions(testCase.knobs, baseline, warmup, dynamic);

    await caseWriter.write(toCaseRow(testCase, baselineResult));
    for (const decision of decisions) {
      await policyWriter.write(toPolicyRow(testCase, baseline, warmup, dynamic, decision));
    }

    if (caseOrdinal < config.warmupSample) {
      for (const warmupProfile of warmupProfiles) {
        const result = recognizeSession(testCase.session, { sealed: true, profile: warmupProfile.profile, policyMode: "legacy", sourceHint });
        await warmupWriter.write(toWarmupRow(testCase, baseline, snapshotFromResult(result), warmupProfile));
      }
    }

    sourceCounts[testCase.sourceType] += 1;
    caseOrdinal += 1;
    if (caseOrdinal % 5_000 === 0 || caseOrdinal === config.cases) {
      console.error(`progress ${caseOrdinal}/${config.cases}`);
    }
  }

  await writeSurveyActualPolicyRows(surveyInputs, respondentProfiles, surveyWriter, outlierSubmissionIds);
  await caseWriter.close();
  await policyWriter.close();
  await warmupWriter.close();
  await surveyWriter.close();

  writeJson(join(config.outDir, "analysis_summary.json"), {
    generatedAt: new Date().toISOString(),
    requestedCaseCount: config.cases,
    actualCaseCount: caseOrdinal,
    warmupSampleCount: Math.min(config.warmupSample, caseOrdinal),
    sourceCounts,
    allocation,
    focusedDynamic: config.focusedDynamic,
    splitCounts: {
      calibration_train: config.focusedDynamic ? Math.round(config.cases * 0.7) : config.cases,
      validation_holdout: config.focusedDynamic ? config.cases - Math.round(config.cases * 0.7) : 0
    },
    excludedSurveyOutliers: outlierReports,
    rawSurveyRecords: rawRecords.length,
    dedupSurveyRecords: dedupRecords.length,
    surveyInputs: surveyInputs.length,
    tutorialInputs: tutorialInputs.length,
    warmupStages: warmupProfiles.map((profile) => ({
      stage: profile.stage,
      captureCount: profile.captureCount
    })),
    notes: [
      config.focusedDynamic
        ? "Focused dynamic run uses production dynamic recognizer policy with legacy comparisons."
        : "Production recognizer behavior is compared through legacy/dynamic policy lanes.",
      "Policy decisions do not use expectedFamily; expectedFamily is used only for metrics.",
      "ML-first is allowed to change status/family when shadow confidence gates pass.",
      "Full policy summaries and plots are produced by scripts/survey-guardrail-ml-plots.py."
    ]
  });

  console.log(JSON.stringify({ outDir: config.outDir, cases: caseOrdinal, warmupSample: Math.min(config.warmupSample, caseOrdinal) }));
}

function parseArgs(): RunConfig {
  const focusedDynamic = process.argv.includes("--dynamic-focused");
  const cases = readNumberArg("--cases", focusedDynamic ? 10_000 : DEFAULT_CASES);
  const warmupSample = readNumberArg("--warmup-sample", Math.min(10_000, cases));
  const outIndex = process.argv.indexOf("--out");
  const outDir = outIndex >= 0 && process.argv[outIndex + 1]
    ? resolve(process.argv[outIndex + 1])
    : resolve(
        DEFAULT_OUTPUT_ROOT,
        `${focusedDynamic ? "dynamic-gating-calibration" : "guardrail-ml-experiment"}-${timestampForPath(new Date())}`
      );

  return {
    cases: Math.max(1, Math.round(cases)),
    warmupSample: Math.max(0, Math.round(Math.min(warmupSample, cases))),
    outDir,
    focusedDynamic
  };
}

function readNumberArg(name: string, fallback: number): number {
  const index = process.argv.indexOf(name);
  const value = index >= 0 ? Number(process.argv[index + 1]) : fallback;
  return Number.isFinite(value) ? value : fallback;
}

function allocateCaseCounts(total: number, focusedDynamic: boolean): Record<SourceType, number> {
  if (focusedDynamic) {
    if (total === 10_000) {
      return {
        random_stratified: 0,
        boundary_sweep: 0,
        survey_mutation: 0,
        survey_mutation_valid: 5_000,
        risk_boundary: 2_500,
        confusion_repair: 1_500,
        balanced_holdout: 1_000
      };
    }
    const survey = Math.floor(total * 0.5);
    const risk = Math.floor(total * 0.25);
    const repair = Math.floor(total * 0.15);
    return {
      random_stratified: 0,
      boundary_sweep: 0,
      survey_mutation: 0,
      survey_mutation_valid: survey,
      risk_boundary: risk,
      confusion_repair: repair,
      balanced_holdout: total - survey - risk - repair
    };
  }
  const random = Math.floor(total * 0.4);
  const boundary = Math.floor(total * 0.4);
  return {
    random_stratified: random,
    boundary_sweep: boundary,
    survey_mutation: total - random - boundary,
    survey_mutation_valid: 0,
    risk_boundary: 0,
    confusion_repair: 0,
    balanced_holdout: 0
  };
}

async function* generateExperimentCases(
  total: number,
  allocation: Record<SourceType, number>,
  surveyInputs: NormalizedInput[],
  focusedDynamic = false
): AsyncGenerator<ExperimentCase> {
  let emitted = 0;
  const sourceEmitted: Record<SourceType, number> = {
    random_stratified: 0,
    boundary_sweep: 0,
    survey_mutation: 0,
    survey_mutation_valid: 0,
    risk_boundary: 0,
    confusion_repair: 0,
    balanced_holdout: 0
  };

  const generators = focusedDynamic
    ? [
        generateSurveyMutations(allocation.survey_mutation_valid, surveyInputs, "survey_mutation_valid"),
        generateRiskBoundary(allocation.risk_boundary),
        generateConfusionRepair(allocation.confusion_repair),
        generateBalancedHoldout(allocation.balanced_holdout)
      ]
    : [
        generateRandomStratified(allocation.random_stratified, "random_stratified"),
        generateBoundarySweep(allocation.boundary_sweep, "boundary_sweep"),
        generateSurveyMutations(allocation.survey_mutation, surveyInputs, "survey_mutation")
      ];

  for (const generator of generators) {
    for (const item of generator) {
      const sourceIndex = sourceEmitted[item.sourceType];
      const sourceTotal = allocation[item.sourceType] || total;
      const split: ExperimentSplit = focusedDynamic && sourceIndex >= Math.round(sourceTotal * 0.7) ? "validation_holdout" : "calibration_train";
      sourceEmitted[item.sourceType] += 1;
      emitted += 1;
      yield { ...item, split };
    }
  }

  if (emitted !== total) {
    throw new Error(`generated case count mismatch: expected ${total}, got ${emitted}`);
  }
}

function* generateRandomStratified(
  count: number,
  sourceType: "random_stratified" | "balanced_holdout"
): Generator<ExperimentCase> {
  const cells = DASHBOARD_SCENARIO_PRESETS.flatMap((preset, presetIndex) =>
    DASHBOARD_MATRIX_FAMILIES.map((family, familyIndex) => ({ preset, presetIndex, family, familyIndex }))
  );

  for (const [cellIndex, cell] of cells.entries()) {
    const cellCount = countForCell(count, cells.length, cellIndex);
    for (let index = 0; index < cellCount; index += 1) {
      const seed = BASE_SEED + cell.presetIndex * 1_000_000 + cell.familyIndex * 100_000 + index;
      const range = { ...cell.preset.range, family: cell.family, seed };
      const recipe = buildSyntheticRecipeFromRange(range, seed);
      yield {
        caseId: `${sourceType === "balanced_holdout" ? "balanced" : "random"}-${cell.preset.id}-${cell.family}-${index}`,
        sourceType,
        split: splitForCellIndex(index, cellCount),
        expectedFamily: cell.family,
        generationAxis: "random",
        presetId: cell.preset.id,
        level: -1,
        seed,
        session: buildSyntheticStrokeSession(recipe),
        knobs: knobsFromRecipe(recipe)
      };
    }
  }
}

function* generateBoundarySweep(
  count: number,
  sourceType: "boundary_sweep" | "risk_boundary" = "boundary_sweep",
  families: GlyphFamily[] = DASHBOARD_MATRIX_FAMILIES
): Generator<ExperimentCase> {
  const axes: Exclude<StressAxis, "random">[] = ["open_gap", "jitter_noise", "rotation_curve", "mixed_borderline"];
  const levels = Array.from({ length: 10 }, (_, index) => index);
  const cells = families.flatMap((family, familyIndex) =>
    axes.flatMap((axis, axisIndex) => levels.map((level) => ({ family, familyIndex, axis, axisIndex, level })))
  );

  for (const [cellIndex, cell] of cells.entries()) {
    const cellCount = countForCell(count, cells.length, cellIndex);
    for (let index = 0; index < cellCount; index += 1) {
      const seed = BASE_SEED + 10_000_000 + cell.familyIndex * 1_000_000 + cell.axisIndex * 100_000 + cell.level * 5_000 + index;
      const recipe = boundaryRecipe(cell.family, cell.axis, cell.level, seed);
      yield {
        caseId: `${sourceType}-${cell.family}-${cell.axis}-${cell.level}-${index}`,
        sourceType,
        split: splitForCellIndex(index, cellCount),
        expectedFamily: cell.family,
        generationAxis: cell.axis,
        presetId: "boundary_sweep",
        level: cell.level,
        seed,
        session: buildSyntheticStrokeSession(recipe),
        knobs: knobsFromRecipe(recipe)
      };
    }
  }
}

function* generateSurveyMutations(
  count: number,
  surveyInputs: NormalizedInput[],
  sourceType: "survey_mutation" | "survey_mutation_valid"
): Generator<ExperimentCase> {
  if (surveyInputs.length === 0) {
    return;
  }

  const axes: Exclude<StressAxis, "random">[] = ["open_gap", "jitter_noise", "rotation_curve", "mixed_borderline"];

  for (let index = 0; index < count; index += 1) {
    const source = surveyInputs[index % surveyInputs.length];
    const axis = axes[index % axes.length];
    const level = Math.floor(index / surveyInputs.length) % 10;
    const seed = BASE_SEED + 20_000_000 + index;
    const knobs = mutationKnobs(axis, level, seed);
    yield {
      caseId: `${sourceType}-${source.inputId}-${index}`,
      sourceType,
      split: splitForCellIndex(index, count),
      expectedFamily: source.expectedFamily,
      generationAxis: axis,
      presetId: "survey_mutation",
      level,
      seed,
      session: mutateSession(source.session, knobs, seed),
      knobs,
      sourceInputId: source.inputId
    };
  }
}

function* generateRiskBoundary(count: number): Generator<ExperimentCase> {
  yield* generateBoundarySweep(count, "risk_boundary", ["earth", "fire", "life"]);
}

function* generateConfusionRepair(count: number): Generator<ExperimentCase> {
  const families: GlyphFamily[] = ["earth", "fire", "life"];
  const axes: Exclude<StressAxis, "random">[] = ["jitter_noise", "rotation_curve", "mixed_borderline"];
  const levels = [3, 4, 5, 6, 7, 8, 9];
  const cells = families.flatMap((family, familyIndex) =>
    axes.flatMap((axis, axisIndex) => levels.map((level) => ({ family, familyIndex, axis, axisIndex, level })))
  );

  for (const [cellIndex, cell] of cells.entries()) {
    const cellCount = countForCell(count, cells.length, cellIndex);
    for (let index = 0; index < cellCount; index += 1) {
      const seed = BASE_SEED + 30_000_000 + cell.familyIndex * 1_000_000 + cell.axisIndex * 100_000 + cell.level * 5_000 + index;
      const recipe = confusionRepairRecipe(cell.family, cell.axis, cell.level, seed);
      yield {
        caseId: `confusion-repair-${cell.family}-${cell.axis}-${cell.level}-${index}`,
        sourceType: "confusion_repair",
        split: splitForCellIndex(index, cellCount),
        expectedFamily: cell.family,
        generationAxis: cell.axis,
        presetId: "confusion_repair",
        level: cell.level,
        seed,
        session: buildSyntheticStrokeSession(recipe),
        knobs: knobsFromRecipe(recipe)
      };
    }
  }
}

function* generateBalancedHoldout(count: number): Generator<ExperimentCase> {
  yield* generateRandomStratified(count, "balanced_holdout");
}

function boundaryRecipe(family: GlyphFamily, axis: Exclude<StressAxis, "random">, level: number, seed: number): SyntheticInputRecipe {
  const t = level / 9;
  const base: SyntheticInputRecipe = {
    family,
    seed,
    jitterPx: 1.5,
    openGapRatio: 0.02,
    rotationDeg: 0,
    curveWarp: 0.03,
    extraNoiseStrokeCount: 0,
    pointDensity: 5
  };

  switch (axis) {
    case "open_gap":
      return { ...base, openGapRatio: 0.02 + t * 0.55, jitterPx: 2 + t * 5 };
    case "jitter_noise":
      return { ...base, jitterPx: 2 + t * 30, extraNoiseStrokeCount: Math.round(t * 6), curveWarp: 0.04 + t * 0.12 };
    case "rotation_curve":
      return { ...base, rotationDeg: -45 + t * 90, curveWarp: 0.02 + t * 0.38 };
    case "mixed_borderline":
      return {
        ...base,
        jitterPx: 4 + t * 18,
        openGapRatio: 0.06 + t * 0.34,
        rotationDeg: -22 + t * 44,
        curveWarp: 0.04 + t * 0.22,
        extraNoiseStrokeCount: Math.round(t * 4)
      };
  }
}

function confusionRepairRecipe(family: GlyphFamily, axis: Exclude<StressAxis, "random">, level: number, seed: number): SyntheticInputRecipe {
  const recipe = boundaryRecipe(family, axis, level, seed);
  const t = level / 9;
  if (family === "earth") {
    return {
      ...recipe,
      openGapRatio: Math.max(recipe.openGapRatio ?? 0, 0.12 + t * 0.18),
      jitterPx: Math.max(recipe.jitterPx ?? 0, 7 + t * 12),
      extraNoiseStrokeCount: Math.max(recipe.extraNoiseStrokeCount ?? 0, Math.round(1 + t * 3))
    };
  }
  if (family === "fire") {
    return {
      ...recipe,
      curveWarp: Math.max(recipe.curveWarp ?? 0, 0.1 + t * 0.24),
      rotationDeg: (recipe.rotationDeg ?? 0) + (t > 0.5 ? 12 : -12),
      jitterPx: Math.max(recipe.jitterPx ?? 0, 6 + t * 10)
    };
  }
  return {
    ...recipe,
    openGapRatio: Math.max(recipe.openGapRatio ?? 0, 0.08 + t * 0.18),
    rotationDeg: (recipe.rotationDeg ?? 0) + (t > 0.5 ? 18 : -18),
    extraNoiseStrokeCount: Math.max(recipe.extraNoiseStrokeCount ?? 0, Math.round(t * 3))
  };
}

function mutationKnobs(axis: Exclude<StressAxis, "random">, level: number, seed: number): DiagnosticKnobs {
  const t = level / 9;
  const base = {
    jitterPx: 1,
    openGapRatio: 0,
    rotationDeg: 0,
    curveWarp: 0,
    extraNoiseStrokeCount: 0,
    pointDensity: 0
  };

  switch (axis) {
    case "open_gap":
      return { ...base, openGapRatio: 0.05 + t * 0.45, jitterPx: 1 + t * 4 };
    case "jitter_noise":
      return { ...base, jitterPx: 3 + t * 28, extraNoiseStrokeCount: Math.round(t * 5), curveWarp: 0.02 + t * 0.08 };
    case "rotation_curve":
      return { ...base, rotationDeg: -36 + t * 72, curveWarp: 0.02 + t * 0.28 };
    case "mixed_borderline":
      return {
        ...base,
        jitterPx: 4 + t * 18,
        openGapRatio: 0.06 + t * 0.32,
        rotationDeg: -18 + t * 36,
        curveWarp: 0.03 + t * 0.18,
        extraNoiseStrokeCount: Math.round(t * 4)
      };
  }
}

function mutateSession(session: StrokeSession, knobs: DiagnosticKnobs, seed: number): StrokeSession {
  const random = createSeededRandom(seed);
  const centroid = sessionCentroid(session);
  const angle = (knobs.rotationDeg / 180) * Math.PI;
  let strokes = session.strokes.map((stroke, strokeIndex) => {
    let points = stroke.points.map((point, pointIndex) => {
      const warped = applyCurveWarp(point, pointIndex, stroke.points.length, knobs.curveWarp, centroid);
      const rotated = rotateAround(warped, centroid, angle);
      return {
        ...rotated,
        t: point.t,
        pressure: point.pressure,
        x: rotated.x + (random.next() * 2 - 1) * knobs.jitterPx,
        y: rotated.y + (random.next() * 2 - 1) * knobs.jitterPx
      };
    });
    points = applyStrokeOpenGap(points, knobs.openGapRatio);
    return {
      id: `${stroke.id}-mut-${strokeIndex}`,
      points
    };
  });

  for (let index = 0; index < knobs.extraNoiseStrokeCount; index += 1) {
    strokes = [...strokes, buildMutationNoiseStroke(`survey-noise-${seed}-${index}`, centroid, random, index)];
  }

  const timestamps = strokes.flatMap((stroke) => stroke.points.map((point) => point.t ?? 0));
  return {
    strokes,
    startedAt: timestamps.length ? Math.min(...timestamps) : session.startedAt,
    endedAt: timestamps.length ? Math.max(...timestamps) : session.endedAt
  };
}

function buildPolicyDecisions(
  knobs: DiagnosticKnobs,
  baseline: RuntimeSnapshot,
  warmup: RuntimeSnapshot,
  dynamic: RuntimeSnapshot
): PolicyDecision[] {
  const baselineDecision = decisionFromSnapshot("baseline", baseline, baseline, "current recognizer");
  const warmupDecision = decisionFromSnapshot("tutorial_warmup", warmup, baseline, "tutorial warmup profile");
  const mlDecision = applyMlFirst(knobs, baseline, warmup);
  const statDecision = applyStatGuardrail("stat_guardrail", knobs, warmup, baseline, "statistical guardrail over tutorial warmup");
  const finalDecision = applyStatGuardrail("ml_guardrail_final", knobs, decisionSnapshot(mlDecision, warmup), baseline, "guardrail over ml-first");
  const dynamicDecision = dynamicDecisionFromSnapshot(dynamic, baseline);

  return [baselineDecision, warmupDecision, mlDecision, statDecision, finalDecision, dynamicDecision];
}

function applyMlFirst(knobs: DiagnosticKnobs, baseline: RuntimeSnapshot, warmup: RuntimeSnapshot): PolicyDecision {
  const guardrail = evaluateGuardrail(knobs, warmup);
  const shadowLabel = warmup.shadowTopFamily;
  const hasShadowLabel = shadowLabel !== "none";
  const canPromote =
    warmup.status !== "recognized" &&
    warmup.confidence >= 0.7 &&
    warmup.shadowScore >= 0.7 &&
    (warmup.scoreGap >= 0.08 || warmup.probabilityGap >= 0.08) &&
    hasShadowLabel;
  const canReplace =
    hasShadowLabel &&
    shadowLabel !== warmup.topFamily &&
    warmup.confidence >= 0.76 &&
    guardrail.severity !== "block";
  const shouldDowngrade =
    warmup.status === "recognized" && (warmup.confidence < 0.54 || warmup.mlConfidenceGate < 0.35);

  if (shouldDowngrade) {
    return {
      policy: "ml_first",
      status: "ambiguous",
      decisionFamily: warmup.topFamily,
      acceptedFamily: "none",
      confidence: warmup.confidence,
      reason: "ml_confidence_downgrade",
      guardrailSeverity: "none",
      guardrailReasons: [],
      changedFromBaseline: baseline.status !== "ambiguous" || baseline.acceptedFamily !== "none"
    };
  }

  if (canReplace) {
    const status = warmup.shadowStatus === "none" ? warmup.status : warmup.shadowStatus;
    return {
      policy: "ml_first",
      status,
      decisionFamily: shadowLabel,
      acceptedFamily: status === "recognized" ? shadowLabel : "none",
      confidence: warmup.confidence,
      reason: "ml_shadow_label_replace",
      guardrailSeverity: "none",
      guardrailReasons: [],
      changedFromBaseline: baseline.status !== status || baseline.acceptedFamily !== (status === "recognized" ? shadowLabel : "none")
    };
  }

  if (canPromote) {
    return {
      policy: "ml_first",
      status: "recognized",
      decisionFamily: shadowLabel,
      acceptedFamily: shadowLabel,
      confidence: warmup.confidence,
      reason: "ml_confident_promotion",
      guardrailSeverity: "none",
      guardrailReasons: [],
      changedFromBaseline: baseline.status !== "recognized" || baseline.acceptedFamily !== shadowLabel
    };
  }

  return {
    ...decisionFromSnapshot("ml_first", warmup, baseline, "ml_kept_tutorial_decision"),
    confidence: warmup.confidence
  };
}

function applyStatGuardrail(
  policy: "stat_guardrail" | "ml_guardrail_final",
  knobs: DiagnosticKnobs,
  snapshot: RuntimeSnapshot,
  baseline: RuntimeSnapshot,
  defaultReason: string
): PolicyDecision {
  const guardrail = evaluateGuardrail(knobs, snapshot);
  const shouldHold = snapshot.status === "recognized" && guardrail.severity !== "none";
  const status: RecognitionStatus = shouldHold ? "ambiguous" : snapshot.status;
  const acceptedFamily = status === "recognized" ? snapshot.acceptedFamily : "none";

  return {
    policy,
    status,
    decisionFamily: snapshot.topFamily,
    acceptedFamily,
    confidence: snapshot.confidence,
    reason: shouldHold ? `guardrail_${guardrail.severity}` : defaultReason,
    guardrailSeverity: guardrail.severity,
    guardrailReasons: guardrail.reasons,
    changedFromBaseline: baseline.status !== status || baseline.acceptedFamily !== acceptedFamily
  };
}

function dynamicDecisionFromSnapshot(snapshot: RuntimeSnapshot, baseline: RuntimeSnapshot): PolicyDecision {
  const summary = snapshot.result.dynamicPolicy;
  return {
    policy: "ml_guardrail_dynamic",
    status: snapshot.status,
    decisionFamily: snapshot.topFamily,
    acceptedFamily: snapshot.status === "recognized" ? snapshot.acceptedFamily : "none",
    confidence: snapshot.confidence,
    reason: summary?.reason ?? "dynamic_policy",
    guardrailSeverity: summary?.riskLevel ?? "none",
    guardrailReasons: summary?.riskReasons ?? [],
    changedFromBaseline: baseline.status !== snapshot.status || baseline.acceptedFamily !== snapshot.acceptedFamily
  };
}

function evaluateGuardrail(knobs: DiagnosticKnobs, snapshot: RuntimeSnapshot): { severity: "none" | "caution" | "block"; reasons: string[] } {
  const block: string[] = [];
  const caution: string[] = [];

  if (snapshot.scoreGap < 0.06) {
    block.push("score_gap_lt_0_06");
  }
  if (snapshot.topScore < 0.62) {
    block.push("top_score_lt_0_62");
  }
  if (snapshot.topFamily !== "none" && CLOSED_FAMILIES.has(snapshot.topFamily) && snapshot.result.rawQuality.closure < 0.25) {
    block.push("closed_family_low_closure");
  }
  if (knobs.extraNoiseStrokeCount >= 4 && snapshot.result.rawQuality.stability < 0.72) {
    block.push("noise_with_low_stability");
  }
  if (knobs.openGapRatio >= 0.3) {
    caution.push("open_gap_ge_0_30");
  }
  if (knobs.jitterPx >= 16) {
    caution.push("jitter_ge_16");
  }
  if (snapshot.result.rawQuality.rotationBias >= 0.75) {
    caution.push("rotation_bias_ge_0_75");
  }

  if (block.length > 0) {
    return { severity: "block", reasons: block };
  }
  if (caution.length > 0) {
    return { severity: "caution", reasons: caution };
  }
  return { severity: "none", reasons: [] };
}

function decisionFromSnapshot(policy: PolicyId, snapshot: RuntimeSnapshot, baseline: RuntimeSnapshot, reason: string): PolicyDecision {
  return {
    policy,
    status: snapshot.status,
    decisionFamily: snapshot.topFamily,
    acceptedFamily: snapshot.acceptedFamily,
    confidence: snapshot.confidence,
    reason,
    guardrailSeverity: "none",
    guardrailReasons: [],
    changedFromBaseline: baseline.status !== snapshot.status || baseline.acceptedFamily !== snapshot.acceptedFamily
  };
}

function decisionSnapshot(decision: PolicyDecision, reference: RuntimeSnapshot): RuntimeSnapshot {
  return {
    ...reference,
    status: decision.status,
    topFamily: decision.decisionFamily,
    acceptedFamily: decision.acceptedFamily,
    confidence: decision.confidence
  };
}

function snapshotFromResult(result: RecognitionResult): RuntimeSnapshot {
  const candidates = result.shadow?.personalizedCandidates ?? result.shadow?.candidates ?? [];
  const shadowTopFamily =
    result.shadow?.personalizedShadowTopLabel ??
    result.shadow?.shadowTopLabel ??
    result.topCandidate?.family ??
    "none";
  const shadowStatus = result.shadow?.personalizedShadowStatus ?? result.shadow?.shadowStatus ?? "none";
  const topShadowCandidate = candidates.find((candidate) => candidate.label === shadowTopFamily) ?? candidates[0];
  const probabilityGap = probabilityGapFor(candidates);
  const topFamily = result.topCandidate?.family ?? "none";
  const acceptedFamily = result.status === "recognized" ? (result.canonicalFamily ?? topFamily) : "none";
  const topScore = result.topCandidate?.score ?? 0;

  return {
    status: result.status,
    topFamily,
    acceptedFamily,
    topScore,
    scoreGap: scoreGapFor(result),
    confidence:
      result.shadow?.personalizedCalibratedConfidence ??
      result.shadow?.calibratedConfidence ??
      topScore,
    mlConfidenceGate: result.personalization?.mlConfidenceGate ?? 1,
    shadowTopFamily,
    shadowStatus,
    shadowScore: topShadowCandidate?.shadowScore ?? topScore,
    probabilityGap,
    effectiveThresholdBias: result.personalization?.effectiveThresholdBias ?? 0,
    personalizationStage: result.personalization?.stage ?? "none",
    result
  };
}

async function writeSurveyActualPolicyRows(
  surveyInputs: NormalizedInput[],
  respondentProfiles: Map<string, UserInputProfile>,
  writer: CsvWriter,
  outlierSubmissionIds: Set<string>
): Promise<void> {
  const directRows = surveyInputs.filter((input) => input.captureKind === "direct");

  for (const input of directRows) {
    const sourceHint = outlierSubmissionIds.has(input.submissionId) ? "survey_mutation" : "survey_mutation_valid";
    const baseline = snapshotFromResult(recognizeSession(input.session, { sealed: true, policyMode: "legacy", sourceHint }));
    const profile = respondentProfiles.get(input.submissionId) ?? createEmptyUserInputProfile();
    const warmup = snapshotFromResult(recognizeSession(input.session, { sealed: true, profile, policyMode: "legacy", sourceHint }));
    const dynamic = snapshotFromResult(recognizeSession(input.session, { sealed: true, profile, policyMode: "dynamic", sourceHint }));
    const decisions = buildPolicyDecisions(emptyKnobs(), baseline, warmup, dynamic);

    for (const decision of decisions) {
      const runtime = decision.policy === "baseline" ? baseline : decision.policy === "ml_guardrail_dynamic" ? dynamic : warmup;
      await writer.write({
        input_id: input.inputId,
        submission_id: input.submissionId,
        target_word: input.targetWord,
        experiment_group: input.experimentGroup,
        policy: decision.policy,
        status: decision.status,
        decision_family: decision.decisionFamily,
        accepted_family: decision.acceptedFamily,
        top_score: runtime.topScore,
        score_gap: runtime.scoreGap,
        confidence: decision.confidence,
        ml_confidence_gate: runtime.mlConfidenceGate,
        effective_threshold_bias: runtime.effectiveThresholdBias,
        personalization_stage: runtime.personalizationStage,
        guardrail_severity: decision.guardrailSeverity,
        guardrail_reasons: decision.guardrailReasons.join("|"),
        changed_from_baseline: decision.changedFromBaseline,
        is_correct_accept: decision.status === "recognized" && decision.acceptedFamily === input.expectedFamily,
        is_unsafe_accept: decision.status === "recognized" && decision.acceptedFamily !== input.expectedFamily
      });
    }
  }
}

function toCaseRow(testCase: ExperimentCase, result: RecognitionResult): Record<string, unknown> {
  return {
    case_id: testCase.caseId,
    source_type: testCase.sourceType,
    split: testCase.split,
    expected_family: testCase.expectedFamily,
    generation_axis: testCase.generationAxis,
    preset_id: testCase.presetId,
    level: testCase.level,
    seed: testCase.seed,
    source_input_id: testCase.sourceInputId ?? "",
    jitter_px: testCase.knobs.jitterPx,
    open_gap_ratio: testCase.knobs.openGapRatio,
    rotation_deg: testCase.knobs.rotationDeg,
    curve_warp: testCase.knobs.curveWarp,
    extra_noise_stroke_count: testCase.knobs.extraNoiseStrokeCount,
    point_density: testCase.knobs.pointDensity,
    stroke_count: result.features.strokeCount,
    point_count: result.features.pointCount,
    duration_ms: result.features.durationMs,
    path_length: result.features.pathLength,
    closure_gap: result.features.closureGap,
    dominant_corners: result.features.dominantCorners,
    endpoint_clusters: result.features.endpointClusters,
    circularity: result.features.circularity,
    fill_ratio: result.features.fillRatio,
    parallelism: result.features.parallelism,
    closure: result.rawQuality.closure,
    symmetry: result.rawQuality.symmetry,
    smoothness: result.rawQuality.smoothness,
    tempo: result.rawQuality.tempo,
    overshoot: result.rawQuality.overshoot,
    stability: result.rawQuality.stability,
    rotation_bias: result.rawQuality.rotationBias
  };
}

function toPolicyRow(
  testCase: ExperimentCase,
  baseline: RuntimeSnapshot,
  warmup: RuntimeSnapshot,
  dynamic: RuntimeSnapshot,
  decision: PolicyDecision
): Record<string, unknown> {
  const metrics = decisionMetrics(testCase.expectedFamily, decision);
  const runtime = decision.policy === "baseline" ? baseline : decision.policy === "ml_guardrail_dynamic" ? dynamic : warmup;

  return {
    case_id: testCase.caseId,
    source_type: testCase.sourceType,
    split: testCase.split,
    expected_family: testCase.expectedFamily,
    generation_axis: testCase.generationAxis,
    policy: decision.policy,
    status: decision.status,
    decision_family: decision.decisionFamily,
    accepted_family: decision.acceptedFamily,
    top_score: runtime.topScore,
    score_gap: runtime.scoreGap,
    confidence: decision.confidence,
    ml_confidence_gate: runtime.mlConfidenceGate,
    effective_threshold_bias: runtime.effectiveThresholdBias,
    personalization_stage: runtime.personalizationStage,
    shadow_top_family: runtime.shadowTopFamily,
    shadow_status: runtime.shadowStatus,
    shadow_score: runtime.shadowScore,
    probability_gap: runtime.probabilityGap,
    guardrail_severity: decision.guardrailSeverity,
    guardrail_reasons: decision.guardrailReasons.join("|"),
    reason: decision.reason,
    changed_from_baseline: decision.changedFromBaseline,
    ...metrics
  };
}

function toWarmupRow(
  testCase: ExperimentCase,
  baseline: RuntimeSnapshot,
  snapshot: RuntimeSnapshot,
  warmupProfile: { stage: string; captureCount: number; profile: UserInputProfile }
): Record<string, unknown> {
  const acceptedFamily = snapshot.status === "recognized" ? snapshot.acceptedFamily : "none";
  return {
    case_id: testCase.caseId,
    source_type: testCase.sourceType,
    expected_family: testCase.expectedFamily,
    warmup_stage: warmupProfile.stage,
    profile_capture_count: warmupProfile.captureCount,
    status: snapshot.status,
    decision_family: snapshot.topFamily,
    accepted_family: acceptedFamily,
    top_score: snapshot.topScore,
    score_gap: snapshot.scoreGap,
    confidence: snapshot.confidence,
    ml_confidence_gate: snapshot.mlConfidenceGate,
    effective_threshold_bias: snapshot.effectiveThresholdBias,
    personalization_stage: snapshot.personalizationStage,
    changed_from_baseline: baseline.status !== snapshot.status || baseline.acceptedFamily !== acceptedFamily,
    is_correct_accept: snapshot.status === "recognized" && acceptedFamily === testCase.expectedFamily,
    is_unsafe_accept: snapshot.status === "recognized" && acceptedFamily !== testCase.expectedFamily
  };
}

function decisionMetrics(expectedFamily: GlyphFamily, decision: PolicyDecision): Record<string, unknown> {
  const isCorrectAccept = decision.status === "recognized" && decision.acceptedFamily === expectedFamily;
  const isUnsafeAccept = decision.status === "recognized" && decision.acceptedFamily !== expectedFamily;
  return {
    is_correct_accept: isCorrectAccept,
    is_unsafe_accept: isUnsafeAccept,
    is_overlap: decision.status !== "recognized" || decision.acceptedFamily !== expectedFamily
  };
}

function probabilityGapFor(candidates: NonNullable<RecognitionResult["shadow"]>["candidates"]): number {
  const probabilities = candidates
    .map((candidate) => candidate.probability)
    .filter((value): value is number => value !== undefined && Number.isFinite(value))
    .sort((left, right) => right - left);

  return Math.max(0, (probabilities[0] ?? 0) - (probabilities[1] ?? 0));
}

function buildWarmupProfiles(tutorialInputs: NormalizedInput[]): Array<{ stage: string; captureCount: number; profile: UserInputProfile }> {
  const ordered = [...tutorialInputs].sort((left, right) => left.responseIndex - right.responseIndex || left.inputId.localeCompare(right.inputId));
  const counts = uniqueSorted([0, 6, 12, 24, ordered.length].map((count) => Math.min(count, ordered.length)));

  return counts.map((count) => ({
    stage: count === ordered.length ? "full" : String(count),
    captureCount: count,
    profile: buildProfileFromCaptures(ordered.slice(0, count))
  }));
}

function buildRespondentProfiles(inputs: NormalizedInput[]): Map<string, UserInputProfile> {
  const bySubmission = groupBy(
    inputs.filter((input) => input.captureKind === "tutorial"),
    (input) => input.submissionId
  );
  const profiles = new Map<string, UserInputProfile>();

  for (const [submissionId, captures] of bySubmission) {
    profiles.set(submissionId, buildProfileFromCaptures(captures));
  }

  return profiles;
}

function buildProfileFromCaptures(captures: NormalizedInput[]): UserInputProfile {
  let store: TutorialProfileStore = createEmptyTutorialProfileStore(1_700_000_000_000);

  captures.forEach((capture, index) => {
    const result = recognizeSession(capture.session, { sealed: true });
    store = appendTutorialCapture(store, {
      id: `${capture.submissionId}-${capture.inputId}`,
      kind: "family",
      expectedFamily: capture.expectedFamily,
      strokes: capture.session.strokes,
      source: sourceForTutorialMode(capture.mode, index),
      timestamp: 1_700_000_000_000 + capture.responseIndex * 100 + index,
      validation: {
        reliability: reliabilityForTutorialCapture(capture.expectedFamily, result),
        expectedLabel: capture.expectedFamily,
        actualTopLabel: result.topCandidate?.family,
        status: result.status,
        topScore: result.topCandidate?.score,
        margin: scoreGapFor(result),
        quality: result.rawQuality
      }
    });
  });

  return mergeTutorializedUserProfile(createEmptyUserInputProfile(), store);
}

function readSurveyRecords(paths: string[]): SurveyRecord[] {
  const records: SurveyRecord[] = [];

  for (const path of paths) {
    if (!existsSync(path)) {
      continue;
    }

    readFileSync(path, "utf8")
      .split(/\r?\n/)
      .forEach((line, index) => {
        if (!line.trim()) {
          return;
        }
        const parsed = JSON.parse(line) as Record<string, unknown>;
        const payload = (isRecord(parsed.payload) ? parsed.payload : parsed) as Record<string, unknown>;
        records.push({
          sourceFile: path,
          sourceLine: index + 1,
          payload,
          fingerprint: fingerprintPayload(payload)
        });
      });
  }

  return records;
}

function dedupeRecords(records: SurveyRecord[]): SurveyRecord[] {
  const seen = new Set<string>();
  const deduped: SurveyRecord[] = [];

  for (const record of records) {
    if (!seen.has(record.fingerprint)) {
      seen.add(record.fingerprint);
      deduped.push(record);
    }
  }

  return deduped;
}

function fingerprintPayload(payload: Record<string, unknown>): string {
  const clone = stableCopy(payload, new Set(["submissionId", "receivedAt", "completedAt", "startedAt", "interactionMetrics"]));
  return createHash("sha1").update(JSON.stringify(clone)).digest("hex");
}

function normalizeSurveyInputs(record: SurveyRecord, responseIndex: number): NormalizedInput[] {
  const payload = record.payload;
  const base = {
    responseIndex,
    submissionId: stringOrUndefined(payload.submissionId) ?? `missing-${responseIndex}`,
    experimentGroup: stringOrUndefined(payload.experimentGroup) ?? "unknown",
    sourceFile: record.sourceFile,
    sourceLine: record.sourceLine
  };
  const inputs: NormalizedInput[] = [];

  for (const [index, item] of arrayOfRecords(payload.directDrawings).entries()) {
    const normalized = normalizeCaptureRecord(base, item, "direct", "direct", index);
    if (normalized) {
      inputs.push(normalized);
    }
  }

  for (const [index, item] of arrayOfRecords(payload.tutorialCaptures).entries()) {
    const normalized = normalizeCaptureRecord(base, item, "tutorial", stringOrUndefined(item.mode) ?? `tutorial-${index}`, index);
    if (normalized) {
      inputs.push(normalized);
    }
  }

  return inputs;
}

function normalizeCaptureRecord(
  base: Pick<NormalizedInput, "responseIndex" | "submissionId" | "experimentGroup" | "sourceFile" | "sourceLine">,
  item: Record<string, unknown>,
  captureKind: "direct" | "tutorial",
  mode: string,
  index: number
): NormalizedInput | null {
  const targetWord = stringOrUndefined(item.targetWord);
  if (!isSurveyPromptWord(targetWord)) {
    return null;
  }

  const session = normalizeStrokeSession(item, `${base.submissionId}-${captureKind}-${index}`);
  if (!session) {
    return null;
  }

  return {
    ...base,
    inputId: `${base.responseIndex}-${captureKind}-${index}-${targetWord}-${mode}`,
    captureKind,
    targetWord,
    expectedFamily: promptWordFamily(targetWord),
    mode,
    session
  };
}

function normalizeStrokeSession(item: Record<string, unknown>, idPrefix: string): StrokeSession | null {
  const strokes = Array.isArray(item.strokes)
    ? normalizeStrokesFromObjects(item.strokes, idPrefix)
    : normalizeStrokesFromShapeTrace(item.shapeTrace, idPrefix);

  if (strokes.length === 0) {
    return null;
  }

  const timestamps = strokes.flatMap((stroke) => stroke.points.map((point) => point.t ?? 0));
  return {
    strokes,
    startedAt: timestamps.length ? Math.min(...timestamps) : 0,
    endedAt: timestamps.length ? Math.max(...timestamps) : 0
  };
}

function normalizeStrokesFromObjects(value: unknown[], idPrefix: string): Stroke[] {
  return value
    .map((stroke, strokeIndex) => {
      if (!isRecord(stroke) || !Array.isArray(stroke.points)) {
        return null;
      }
      const points = stroke.points
        .map((point, pointIndex) => {
          if (!isRecord(point)) {
            return null;
          }
          const x = numberOrUndefined(point.x);
          const y = numberOrUndefined(point.y);
          if (x === undefined || y === undefined) {
            return null;
          }
          return {
            x,
            y,
            t: numberOrUndefined(point.t) ?? pointIndex * 16,
            pressure: numberOrUndefined(point.pressure)
          };
        })
        .filter((point): point is PointSample => Boolean(point));
      return points.length > 0 ? { id: stringOrUndefined(stroke.id) ?? `${idPrefix}-${strokeIndex}`, points } : null;
    })
    .filter((stroke): stroke is Stroke => Boolean(stroke));
}

function normalizeStrokesFromShapeTrace(value: unknown, idPrefix: string): Stroke[] {
  if (!Array.isArray(value)) {
    return [];
  }

  return value
    .map((stroke, strokeIndex) => {
      if (!Array.isArray(stroke)) {
        return null;
      }
      const points = stroke
        .map((point, pointIndex) => {
          if (!Array.isArray(point)) {
            return null;
          }
          const x = numberOrUndefined(point[0]);
          const y = numberOrUndefined(point[1]);
          if (x === undefined || y === undefined) {
            return null;
          }
          return { x, y, t: numberOrUndefined(point[2]) ?? pointIndex * 16 };
        })
        .filter((point): point is PointSample => Boolean(point));
      return points.length > 0 ? { id: `${idPrefix}-trace-${strokeIndex}`, points } : null;
    })
    .filter((stroke): stroke is Stroke => Boolean(stroke));
}

class CsvWriter {
  private closed = false;
  private readonly buffer: string[] = [];

  constructor(private readonly path: string, private readonly columns: string[]) {
    mkdirSync(dirname(path), { recursive: true });
    writeFileSync(path, `${columns.join(",")}\n`, "utf8");
  }

  async write(row: Record<string, unknown>): Promise<void> {
    if (this.closed) {
      throw new Error("cannot write to closed CSV");
    }
    this.buffer.push(`${this.columns.map((column) => csvCell(row[column])).join(",")}\n`);
    if (this.buffer.length >= 1_000) {
      this.flush();
    }
  }

  async close(): Promise<void> {
    this.closed = true;
    this.flush();
  }

  private flush(): void {
    if (this.buffer.length === 0) {
      return;
    }
    appendFileSync(this.path, this.buffer.join(""), "utf8");
    this.buffer.length = 0;
  }
}

function writeCsv(path: string, rows: Record<string, unknown>[], columns: string[]): void {
  mkdirSync(dirname(path), { recursive: true });
  const lines = [
    `${columns.join(",")}\n`,
    ...rows.map((row) => `${columns.map((column) => csvCell(row[column])).join(",")}\n`)
  ];
  writeFileSync(path, lines.join(""), "utf8");
}

function knobsFromRecipe(recipe: SyntheticInputRecipe): DiagnosticKnobs {
  return {
    jitterPx: recipe.jitterPx ?? 0,
    openGapRatio: recipe.openGapRatio ?? 0,
    rotationDeg: recipe.rotationDeg ?? 0,
    curveWarp: recipe.curveWarp ?? 0,
    extraNoiseStrokeCount: recipe.extraNoiseStrokeCount ?? 0,
    pointDensity: recipe.pointDensity ?? 0
  };
}

function emptyKnobs(): DiagnosticKnobs {
  return {
    jitterPx: 0,
    openGapRatio: 0,
    rotationDeg: 0,
    curveWarp: 0,
    extraNoiseStrokeCount: 0,
    pointDensity: 0
  };
}

function sessionCentroid(session: StrokeSession): { x: number; y: number } {
  const points = session.strokes.flatMap((stroke) => stroke.points);
  return {
    x: points.reduce((sum, point) => sum + point.x, 0) / Math.max(points.length, 1),
    y: points.reduce((sum, point) => sum + point.y, 0) / Math.max(points.length, 1)
  };
}

function applyCurveWarp(point: PointSample, index: number, count: number, curveWarp: number, centroid: { x: number; y: number }): PointSample {
  const progress = index / Math.max(count - 1, 1);
  const bend = Math.sin(progress * Math.PI) * curveWarp * 42;
  return {
    ...point,
    x: point.x + bend * Math.sign(point.y - centroid.y || 1),
    y: point.y + bend * Math.sign(point.x - centroid.x || 1)
  };
}

function rotateAround(point: PointSample, center: { x: number; y: number }, angle: number): PointSample {
  const x = point.x - center.x;
  const y = point.y - center.y;
  const cos = Math.cos(angle);
  const sin = Math.sin(angle);
  return {
    ...point,
    x: center.x + x * cos - y * sin,
    y: center.y + x * sin + y * cos
  };
}

function applyStrokeOpenGap(points: PointSample[], openGapRatio: number): PointSample[] {
  if (openGapRatio <= 0 || points.length < 5) {
    return points;
  }
  const removeCount = Math.max(1, Math.floor(points.length * openGapRatio * 0.35));
  return points.slice(0, Math.max(2, points.length - removeCount));
}

function buildMutationNoiseStroke(id: string, center: { x: number; y: number }, random: ReturnType<typeof createSeededRandom>, index: number): Stroke {
  const angle = random.next() * Math.PI * 2;
  const radius = 40 + random.next() * 180;
  const length = 20 + random.next() * 60;
  const cx = center.x + Math.cos(angle) * radius;
  const cy = center.y + Math.sin(angle) * radius;
  const lineAngle = angle + Math.PI / 2;
  return {
    id,
    points: [
      { x: cx - Math.cos(lineAngle) * length, y: cy - Math.sin(lineAngle) * length, t: 10_000 + index * 30 },
      { x: cx + Math.cos(lineAngle) * length, y: cy + Math.sin(lineAngle) * length, t: 10_024 + index * 30 }
    ]
  };
}

function scoreGapFor(result: RecognitionResult): number {
  return Math.max(0, (result.topCandidate?.score ?? 0) - (result.candidates[1]?.score ?? 0));
}

function reliabilityForTutorialCapture(expectedFamily: GlyphFamily, result: RecognitionResult): "high" | "medium" | "unvalidated" {
  const topFamily = result.topCandidate?.family;
  const topScore = result.topCandidate?.score ?? 0;
  const gap = scoreGapFor(result);

  if (topFamily === expectedFamily && result.status === "recognized" && topScore >= 0.7 && gap >= 0.1) {
    return "high";
  }
  if (topFamily === expectedFamily && topScore >= 0.55) {
    return "medium";
  }
  return "unvalidated";
}

function sourceForTutorialMode(mode: string, index: number): TutorialCaptureSource {
  if (mode === "fast") {
    return "variation";
  }
  if (mode === "comfortable") {
    return "recall";
  }
  if (mode === "ideal") {
    return "trace";
  }
  return index % 3 === 1 ? "variation" : index % 3 === 2 ? "recall" : "trace";
}

function countForCell(total: number, cells: number, index: number): number {
  const base = Math.floor(total / cells);
  return base + (index < total % cells ? 1 : 0);
}

function splitForCellIndex(index: number, count: number): ExperimentSplit {
  return index < Math.round(count * 0.7) ? "calibration_train" : "validation_holdout";
}

function sourceHintFor(sourceType: SourceType): DynamicRecognitionSourceHint {
  switch (sourceType) {
    case "random_stratified":
    case "boundary_sweep":
    case "survey_mutation":
    case "survey_mutation_valid":
    case "risk_boundary":
    case "confusion_repair":
    case "balanced_holdout":
      return sourceType;
  }
}

function toSurveyOutlierInput(input: NormalizedInput): SurveyOutlierInput {
  const result = recognizeSession(input.session, { sealed: true, policyMode: "legacy" });
  return {
    submissionId: input.submissionId,
    inputId: input.inputId,
    captureKind: input.captureKind,
    targetWord: input.targetWord,
    strokeCount: result.features.strokeCount,
    pointCount: result.features.pointCount,
    topScore: result.topCandidate?.score ?? 0,
    scoreGap: scoreGapFor(result),
    closure: result.rawQuality.closure,
    smoothness: result.rawQuality.smoothness,
    stability: result.rawQuality.stability,
    rotationBias: result.rawQuality.rotationBias
  };
}

function writeOutlierReports(path: string, reports: ReturnType<typeof detectSurveyOutlierRespondents>): void {
  const columns = [
    "submission_id",
    "input_count",
    "reason",
    "input_ids",
    "avg_stroke_count",
    "avg_point_count",
    "avg_top_score",
    "avg_score_gap",
    "avg_closure",
    "avg_smoothness",
    "avg_stability",
    "avg_rotation_bias"
  ];
  const rows = reports.map((report) => ({
    submission_id: report.submissionId,
    input_count: report.inputCount,
    reason: report.reason,
    input_ids: report.inputIds.join("|"),
    avg_stroke_count: report.avgStrokeCount,
    avg_point_count: report.avgPointCount,
    avg_top_score: report.avgTopScore,
    avg_score_gap: report.avgScoreGap,
    avg_closure: report.avgClosure,
    avg_smoothness: report.avgSmoothness,
    avg_stability: report.avgStability,
    avg_rotation_bias: report.avgRotationBias
  }));
  writeCsv(path, rows, columns);
}

function uniqueSorted(values: number[]): number[] {
  return [...new Set(values)].sort((left, right) => left - right);
}

function groupBy<T>(items: T[], keyFor: (item: T) => string): Map<string, T[]> {
  const groups = new Map<string, T[]>();
  for (const item of items) {
    const key = keyFor(item);
    const group = groups.get(key) ?? [];
    group.push(item);
    groups.set(key, group);
  }
  return groups;
}

function stableCopy(value: unknown, omitKeys: Set<string>): unknown {
  if (Array.isArray(value)) {
    return value.map((item) => stableCopy(item, omitKeys));
  }
  if (isRecord(value)) {
    return Object.fromEntries(
      Object.entries(value)
        .filter(([key]) => !omitKeys.has(key))
        .sort(([left], [right]) => left.localeCompare(right))
        .map(([key, item]) => [key, stableCopy(item, omitKeys)])
    );
  }
  return value;
}

function writeJson(path: string, value: unknown): void {
  mkdirSync(dirname(path), { recursive: true });
  writeFileSync(path, `${JSON.stringify(value, null, 2)}\n`, "utf8");
}

function csvCell(value: unknown): string {
  if (value === undefined || value === null) {
    return "";
  }
  if (typeof value === "number") {
    return Number.isFinite(value) ? String(Number(value.toFixed(8))) : "";
  }
  if (typeof value === "boolean") {
    return value ? "true" : "false";
  }
  const text = String(value);
  return /[",\n\r]/.test(text) ? `"${text.replace(/"/g, '""')}"` : text;
}

function arrayOfRecords(value: unknown): Record<string, unknown>[] {
  return Array.isArray(value) ? value.filter(isRecord) : [];
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return Boolean(value) && typeof value === "object" && !Array.isArray(value);
}

function isSurveyPromptWord(value: unknown): value is SurveyPromptWord {
  return value === "fire" || value === "water" || value === "wind";
}

function stringOrUndefined(value: unknown): string | undefined {
  return typeof value === "string" ? value : undefined;
}

function numberOrUndefined(value: unknown): number | undefined {
  return typeof value === "number" && Number.isFinite(value) ? value : undefined;
}

function timestampForPath(date: Date): string {
  return [
    date.getFullYear(),
    String(date.getMonth() + 1).padStart(2, "0"),
    String(date.getDate()).padStart(2, "0"),
    "-",
    String(date.getHours()).padStart(2, "0"),
    String(date.getMinutes()).padStart(2, "0")
  ].join("");
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
