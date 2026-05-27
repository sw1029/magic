import { createHash } from "node:crypto";
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { basename, dirname, join, resolve } from "node:path";

import { runDashboardBatch, summarizeDashboardBatch, type DashboardBatchSample } from "../src/demo/dashboard-batch";
import {
  DASHBOARD_MATRIX_FAMILIES,
  DASHBOARD_SCENARIO_PRESETS,
  buildSyntheticRecipeFromRange,
  type SyntheticInputRange
} from "../src/demo/dashboard-presets";
import { buildSyntheticStrokeSession } from "../src/demo/synthetic-input";
import { recognizeSession } from "../src/recognizer/recognize";
import {
  appendTutorialCapture,
  createEmptyTutorialProfileStore,
  mergeTutorializedUserProfile
} from "../src/recognizer/tutorial-profile";
import { createEmptyUserInputProfile } from "../src/recognizer/user-profile";
import { promptWordFamily, type SurveyPromptWord } from "../src/survey/survey-contract";
import type {
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

type SurveyRecord = {
  sourceFile: string;
  sourceLine: number;
  receivedAt?: string;
  payload: Record<string, unknown>;
  fingerprint: string;
};

type NormalizedInput = {
  inputId: string;
  responseIndex: number;
  submissionId: string;
  sessionId: string;
  schemaVersion: string;
  experimentGroup: string;
  hciProbeVariant: string;
  captureKind: "direct" | "tutorial";
  targetWord: SurveyPromptWord;
  expectedFamily: GlyphFamily;
  mode: string;
  elapsedMs?: number;
  expressionDifficulty?: number;
  expressionReason?: string;
  sourceFile: string;
  sourceLine: number;
  session: StrokeSession;
  storedRecognitionStatus?: string;
  storedRecognizedFamily?: string;
  storedQuality?: Partial<QualityVector>;
};

type RecognizedInputRow = NormalizedInput & {
  baseline: RecognitionResult;
  personalized?: RecognitionResult;
  profileSampleCount?: number;
  profileThresholdBias?: number;
  profileEffectiveThresholdBias?: number;
};

const SURVEY_INPUT_FILES = [
  "data/survey-responses.ndjson",
  "survey-export-cdb7ffdf75f092448f9fc5cd2836652002b063d05614d637/data/survey-responses.ndjson"
];

const OUTPUT_ARG = "--out";
const DEFAULT_OUTPUT_ROOT = "survey-analysis-output";
const SYNTHETIC_CASE_COUNT = 10_000;
const BASE_SYNTHETIC_SEED = 930_000;

const STATUS_RANK: Record<RecognitionStatus, number> = {
  invalid: 0,
  incomplete: 1,
  ambiguous: 2,
  recognized: 3
};

function main(): void {
  const outDir = resolveOutputDir();
  mkdirSync(outDir, { recursive: true });
  mkdirSync(join(outDir, "figures"), { recursive: true });

  const rawRecords = readSurveyRecords(SURVEY_INPUT_FILES);
  const duplicateClusters = buildDuplicateClusters(rawRecords);
  const dedupRecords = dedupeRecords(rawRecords);
  const surveyInputs = dedupRecords.flatMap((record, responseIndex) => normalizeSurveyInputs(record, responseIndex));
  const respondentProfiles = buildRespondentProfiles(surveyInputs);
  const aggregateProfile = buildAggregateProfile(surveyInputs);
  const recognizedSurveyInputs = recognizeSurveyInputs(surveyInputs, respondentProfiles);
  const syntheticRows = buildSyntheticRows(aggregateProfile);

  const syntheticSummary = summarizeSyntheticRows(syntheticRows);
  const surveySummary = summarizeSurveyRows(recognizedSurveyInputs);
  const thresholdSummary = summarizeThresholdRows(recognizedSurveyInputs, syntheticRows);
  const duplicateSummary = summarizeDuplicateClusters(duplicateClusters);

  writeCsv(join(outDir, "duplicate_clusters.csv"), duplicateSummary.rows);
  writeCsv(join(outDir, "survey_inputs.csv"), recognizedSurveyInputs.map(toSurveyInputCsvRow));
  writeCsv(join(outDir, "respondent_threshold_changes.csv"), recognizedSurveyInputs.map(toThresholdChangeCsvRow));
  writeCsv(join(outDir, "synthetic_cases.csv"), syntheticRows);
  writeCsv(join(outDir, "synthetic_summary_by_preset_family.csv"), syntheticSummary.byPresetFamily);
  writeCsv(join(outDir, "synthetic_overlap_cells.csv"), syntheticSummary.overlapCells);
  writeCsv(join(outDir, "survey_group_summary.csv"), surveySummary.groupRows);
  writeJson(join(outDir, "analysis_summary.json"), {
    generatedAt: new Date().toISOString(),
    rawRecordCount: rawRecords.length,
    dedupRecordCount: dedupRecords.length,
    duplicateClusterCount: duplicateSummary.duplicateClusterCount,
    duplicateRowsRemoved: rawRecords.length - dedupRecords.length,
    sourceFiles: SURVEY_INPUT_FILES.map((path) => ({
      path,
      exists: existsSync(path),
      rows: rawRecords.filter((record) => record.sourceFile === path).length
    })),
    survey: surveySummary,
    synthetic: {
      requestedCaseCount: SYNTHETIC_CASE_COUNT,
      actualCaseCount: syntheticRows.length,
      presetCount: DASHBOARD_SCENARIO_PRESETS.length,
      familyCount: DASHBOARD_MATRIX_FAMILIES.length,
      statusCounts: countBy(syntheticRows, (row) => String(row.baseline_status)),
      personalizedStatusCounts: countBy(syntheticRows, (row) => String(row.personalized_status)),
      changedByAggregateProfile: syntheticRows.filter((row) => row.profile_status_changed || row.profile_family_changed).length,
      averageEffectiveThresholdBias: average(syntheticRows.map((row) => Number(row.personalized_effective_threshold_bias))),
      topOverlapCells: syntheticSummary.overlapCells.slice(0, 12)
    },
    threshold: thresholdSummary,
    notes: [
      "Survey inferential statistics are descriptive/exploratory because dedup n is small.",
      "Deduplication excludes volatile submission/timing fields and interactionMetrics to avoid repeated-submit overcounting.",
      "Contact NDJSON files are intentionally excluded.",
      "tinyML is represented by recognizer shadow/gate fields and existing artifacts/ml compatibility."
    ]
  });

  console.log(JSON.stringify({ outDir, raw: rawRecords.length, dedup: dedupRecords.length, synthetic: syntheticRows.length }));
}

function resolveOutputDir(): string {
  const explicitIndex = process.argv.indexOf(OUTPUT_ARG);
  if (explicitIndex >= 0 && process.argv[explicitIndex + 1]) {
    return resolve(process.argv[explicitIndex + 1]);
  }

  return resolve(DEFAULT_OUTPUT_ROOT, `deep-dive-${timestampForPath(new Date())}`);
}

function readSurveyRecords(paths: string[]): SurveyRecord[] {
  const records: SurveyRecord[] = [];

  for (const path of paths) {
    if (!existsSync(path)) {
      continue;
    }

    const lines = readFileSync(path, "utf8").split(/\r?\n/);
    lines.forEach((line, index) => {
      if (!line.trim()) {
        return;
      }

      const parsed = JSON.parse(line) as Record<string, unknown>;
      const payload = (isRecord(parsed.payload) ? parsed.payload : parsed) as Record<string, unknown>;
      records.push({
        sourceFile: path,
        sourceLine: index + 1,
        receivedAt: stringOrUndefined(parsed.receivedAt),
        payload,
        fingerprint: fingerprintPayload(payload)
      });
    });
  }

  return records;
}

function fingerprintPayload(payload: Record<string, unknown>): string {
  const clone = stableCopy(
    payload,
    new Set(["submissionId", "receivedAt", "completedAt", "startedAt", "interactionMetrics"])
  );
  return createHash("sha1").update(JSON.stringify(clone)).digest("hex");
}

function buildDuplicateClusters(records: SurveyRecord[]): Map<string, SurveyRecord[]> {
  const clusters = new Map<string, SurveyRecord[]>();

  for (const record of records) {
    const items = clusters.get(record.fingerprint) ?? [];
    items.push(record);
    clusters.set(record.fingerprint, items);
  }

  return clusters;
}

function dedupeRecords(records: SurveyRecord[]): SurveyRecord[] {
  const seen = new Set<string>();
  const deduped: SurveyRecord[] = [];

  for (const record of records) {
    if (seen.has(record.fingerprint)) {
      continue;
    }

    seen.add(record.fingerprint);
    deduped.push(record);
  }

  return deduped;
}

function normalizeSurveyInputs(record: SurveyRecord, responseIndex: number): NormalizedInput[] {
  const payload = record.payload;
  const submissionId = stringOrUndefined(payload.submissionId) ?? `missing-submission-${responseIndex}`;
  const sessionId = stringOrUndefined(payload.sessionId) ?? "";
  const schemaVersion = stringOrUndefined(payload.schemaVersion) ?? "unknown";
  const experimentGroup = stringOrUndefined(payload.experimentGroup) ?? "unknown";
  const hciProbeVariant = stringOrUndefined(payload.hciProbeVariant) ?? "unknown";
  const inputs: NormalizedInput[] = [];

  for (const [index, item] of arrayOfRecords(payload.directDrawings).entries()) {
    const normalized = normalizeCaptureRecord({
      record,
      responseIndex,
      submissionId,
      sessionId,
      schemaVersion,
      experimentGroup,
      hciProbeVariant,
      captureKind: "direct",
      item,
      mode: "direct",
      index
    });

    if (normalized) {
      inputs.push(normalized);
    }
  }

  for (const [index, item] of arrayOfRecords(payload.tutorialCaptures).entries()) {
    const normalized = normalizeCaptureRecord({
      record,
      responseIndex,
      submissionId,
      sessionId,
      schemaVersion,
      experimentGroup,
      hciProbeVariant,
      captureKind: "tutorial",
      item,
      mode: stringOrUndefined(item.mode) ?? `tutorial-${index}`,
      index
    });

    if (normalized) {
      inputs.push(normalized);
    }
  }

  return inputs;
}

function normalizeCaptureRecord(input: {
  record: SurveyRecord;
  responseIndex: number;
  submissionId: string;
  sessionId: string;
  schemaVersion: string;
  experimentGroup: string;
  hciProbeVariant: string;
  captureKind: "direct" | "tutorial";
  item: Record<string, unknown>;
  mode: string;
  index: number;
}): NormalizedInput | null {
  const targetWord = stringOrUndefined(input.item.targetWord);

  if (!isSurveyPromptWord(targetWord)) {
    return null;
  }

  const session = normalizeStrokeSession(input.item, `${input.submissionId}-${input.captureKind}-${input.index}`);

  if (!session || session.strokes.length === 0) {
    return null;
  }

  return {
    inputId: `${input.responseIndex}-${input.captureKind}-${input.index}-${targetWord}-${input.mode}`,
    responseIndex: input.responseIndex,
    submissionId: input.submissionId,
    sessionId: input.sessionId,
    schemaVersion: input.schemaVersion,
    experimentGroup: input.experimentGroup,
    hciProbeVariant: input.hciProbeVariant,
    captureKind: input.captureKind,
    targetWord,
    expectedFamily: promptWordFamily(targetWord),
    mode: input.mode,
    elapsedMs: numberOrUndefined(input.item.elapsedMs),
    expressionDifficulty: numberOrUndefined(input.item.expressionDifficulty),
    expressionReason: stringOrUndefined(input.item.expressionReason),
    sourceFile: input.record.sourceFile,
    sourceLine: input.record.sourceLine,
    session,
    storedRecognitionStatus: stringOrUndefined(input.item.recognitionStatus),
    storedRecognizedFamily: stringOrUndefined(input.item.recognizedFamily),
    storedQuality: isRecord(input.item.quality) ? (input.item.quality as Partial<QualityVector>) : undefined
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
  const minTime = timestamps.length ? Math.min(...timestamps) : 0;
  const maxTime = timestamps.length ? Math.max(...timestamps) : minTime;

  return {
    strokes,
    startedAt: minTime,
    endedAt: maxTime
  };
}

function normalizeStrokesFromObjects(value: unknown[], idPrefix: string): Stroke[] {
  return value
    .map((stroke, strokeIndex) => {
      if (!isRecord(stroke) || !Array.isArray(stroke.points)) {
        return null;
      }

      const points = normalizePointObjects(stroke.points);
      if (points.length === 0) {
        return null;
      }

      return {
        id: stringOrUndefined(stroke.id) ?? `${idPrefix}-stroke-${strokeIndex}`,
        points
      };
    })
    .filter((stroke): stroke is Stroke => Boolean(stroke));
}

function normalizePointObjects(points: unknown[]): PointSample[] {
  return points
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

          return {
            x,
            y,
            t: numberOrUndefined(point[2]) ?? pointIndex * 16
          };
        })
        .filter((point): point is PointSample => Boolean(point));

      if (points.length === 0) {
        return null;
      }

      return {
        id: `${idPrefix}-trace-${strokeIndex}`,
        points
      };
    })
    .filter((stroke): stroke is Stroke => Boolean(stroke));
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

function buildAggregateProfile(inputs: NormalizedInput[]): UserInputProfile {
  return buildProfileFromCaptures(inputs.filter((input) => input.captureKind === "tutorial"));
}

function buildProfileFromCaptures(captures: NormalizedInput[]): UserInputProfile {
  let store: TutorialProfileStore = createEmptyTutorialProfileStore(1_700_000_000_000);

  captures.forEach((capture, index) => {
    const result = recognizeSession(capture.session, { sealed: true });
    const topFamily = result.topCandidate?.family;
    const scoreGap = scoreGapFor(result);

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
        actualTopLabel: topFamily,
        status: result.status,
        topScore: result.topCandidate?.score,
        margin: scoreGap,
        quality: result.rawQuality
      }
    });
  });

  return mergeTutorializedUserProfile(createEmptyUserInputProfile(), store);
}

function recognizeSurveyInputs(
  inputs: NormalizedInput[],
  profiles: Map<string, UserInputProfile>
): RecognizedInputRow[] {
  return inputs.map((input) => {
    const baseline = recognizeSession(input.session, { sealed: true });
    const profile = profiles.get(input.submissionId);
    const personalized = profile ? recognizeSession(input.session, { sealed: true, profile }) : undefined;

    return {
      ...input,
      baseline,
      personalized,
      profileSampleCount: profile?.tutorialProfile?.tutorialSampleCount,
      profileThresholdBias: personalized?.personalization?.thresholdBias,
      profileEffectiveThresholdBias: personalized?.personalization?.effectiveThresholdBias
    };
  });
}

function buildSyntheticRows(aggregateProfile: UserInputProfile): Array<Record<string, string | number | boolean>> {
  const rows: Array<Record<string, string | number | boolean>> = [];
  const presets = DASHBOARD_SCENARIO_PRESETS;
  const families = DASHBOARD_MATRIX_FAMILIES;
  const casesPerCell = SYNTHETIC_CASE_COUNT / (presets.length * families.length);

  if (!Number.isInteger(casesPerCell)) {
    throw new Error("synthetic case count must divide evenly across preset/family strata");
  }

  for (const [presetIndex, preset] of presets.entries()) {
    for (const [familyIndex, family] of families.entries()) {
      for (let caseIndex = 0; caseIndex < casesPerCell; caseIndex += 1) {
        const seed = BASE_SYNTHETIC_SEED + presetIndex * 1_000_000 + familyIndex * 100_000 + caseIndex;
        const range: SyntheticInputRange = {
          ...preset.range,
          family,
          seed
        };
        const recipe = buildSyntheticRecipeFromRange(range, seed);
        const session = buildSyntheticStrokeSession(recipe);
        const baseline = recognizeSession(session, { sealed: true });
        const personalized = recognizeSession(session, { sealed: true, profile: aggregateProfile });
        const strictStatus = thresholdVariantStatus(baseline, "strict");
        const looseStatus = thresholdVariantStatus(baseline, "loose");
        const baselineActual = actualFamilyFor(baseline);
        const personalizedActual = actualFamilyFor(personalized);

        rows.push({
          case_id: `${preset.id}-${family}-${caseIndex}`,
          preset_id: preset.id,
          preset_label: preset.label,
          expected_family: family,
          seed,
          jitter_px: recipe.jitterPx ?? 0,
          open_gap_ratio: recipe.openGapRatio ?? 0,
          rotation_deg: recipe.rotationDeg ?? 0,
          curve_warp: recipe.curveWarp ?? 0,
          extra_noise_stroke_count: recipe.extraNoiseStrokeCount ?? 0,
          point_density: recipe.pointDensity ?? 0,
          baseline_status: baseline.status,
          baseline_actual_family: baselineActual,
          baseline_top_family: baseline.topCandidate?.family ?? "none",
          baseline_top_score: baseline.topCandidate?.score ?? 0,
          baseline_score_gap: scoreGapFor(baseline),
          strict_status: strictStatus,
          loose_status: looseStatus,
          personalized_status: personalized.status,
          personalized_actual_family: personalizedActual,
          personalized_top_family: personalized.topCandidate?.family ?? "none",
          personalized_top_score: personalized.topCandidate?.score ?? 0,
          personalized_score_gap: scoreGapFor(personalized),
          profile_status_changed: baseline.status !== personalized.status,
          profile_family_changed: baselineActual !== personalizedActual,
          personalized_stage: personalized.personalization?.stage ?? "none",
          personalized_threshold_bias: personalized.personalization?.thresholdBias ?? 0,
          personalized_effective_threshold_bias: personalized.personalization?.effectiveThresholdBias ?? 0,
          personalized_ml_confidence_gate: personalized.personalization?.mlConfidenceGate ?? 0,
          shadow_mode: personalized.shadow?.mode ?? "none",
          shadow_decision_changed: Boolean(personalized.shadow?.decisionChanged),
          shadow_status_changed: Boolean(personalized.shadow?.statusChanged),
          closure: baseline.rawQuality.closure,
          symmetry: baseline.rawQuality.symmetry,
          smoothness: baseline.rawQuality.smoothness,
          tempo: baseline.rawQuality.tempo,
          overshoot: baseline.rawQuality.overshoot,
          stability: baseline.rawQuality.stability,
          rotation_bias: baseline.rawQuality.rotationBias,
          stroke_count: baseline.features.strokeCount,
          point_count: baseline.features.pointCount,
          duration_ms: baseline.features.durationMs,
          path_length: baseline.features.pathLength,
          closure_gap: baseline.features.closureGap,
          dominant_corners: baseline.features.dominantCorners,
          endpoint_clusters: baseline.features.endpointClusters,
          circularity: baseline.features.circularity,
          fill_ratio: baseline.features.fillRatio,
          parallelism: baseline.features.parallelism,
          is_overlap: isOverlap(family, baseline.status, baselineActual)
        });
      }
    }
  }

  return rows;
}

function summarizeSyntheticRows(rows: Array<Record<string, string | number | boolean>>): {
  byPresetFamily: Array<Record<string, string | number>>;
  overlapCells: Array<Record<string, string | number>>;
} {
  const byPresetFamily = [...groupBy(rows, (row) => `${row.preset_id}:${row.expected_family}`).entries()].map(
    ([key, group]) => {
      const [presetId, expectedFamily] = key.split(":");
      return {
        preset_id: presetId,
        expected_family: expectedFamily,
        n: group.length,
        recognized_rate: rate(group.filter((row) => row.baseline_status === "recognized").length, group.length),
        ambiguous_rate: rate(group.filter((row) => row.baseline_status === "ambiguous").length, group.length),
        incomplete_rate: rate(group.filter((row) => row.baseline_status === "incomplete").length, group.length),
        invalid_rate: rate(group.filter((row) => row.baseline_status === "invalid").length, group.length),
        overlap_rate: rate(group.filter((row) => Boolean(row.is_overlap)).length, group.length),
        avg_top_score: average(group.map((row) => Number(row.baseline_top_score))),
        avg_score_gap: average(group.map((row) => Number(row.baseline_score_gap))),
        avg_effective_threshold_bias: average(group.map((row) => Number(row.personalized_effective_threshold_bias))),
        profile_changed_rate: rate(
          group.filter((row) => Boolean(row.profile_status_changed) || Boolean(row.profile_family_changed)).length,
          group.length
        )
      };
    }
  );

  const overlapCells = [...groupBy(
    rows.filter((row) => Boolean(row.is_overlap)),
    (row) => `${row.preset_id}:${row.expected_family}:${row.baseline_actual_family}:${row.baseline_status}`
  ).entries()]
    .map(([key, group]) => {
      const [presetId, expectedFamily, actualFamily, status] = key.split(":");
      return {
        preset_id: presetId,
        expected_family: expectedFamily,
        actual_family: actualFamily,
        status,
        n: group.length,
        avg_top_score: average(group.map((row) => Number(row.baseline_top_score))),
        avg_score_gap: average(group.map((row) => Number(row.baseline_score_gap))),
        avg_jitter_px: average(group.map((row) => Number(row.jitter_px))),
        avg_open_gap_ratio: average(group.map((row) => Number(row.open_gap_ratio))),
        avg_rotation_deg: average(group.map((row) => Number(row.rotation_deg))),
        avg_curve_warp: average(group.map((row) => Number(row.curve_warp))),
        avg_noise_stroke_count: average(group.map((row) => Number(row.extra_noise_stroke_count))),
        avg_closure: average(group.map((row) => Number(row.closure))),
        avg_smoothness: average(group.map((row) => Number(row.smoothness))),
        avg_stability: average(group.map((row) => Number(row.stability))),
        avg_rotation_bias: average(group.map((row) => Number(row.rotation_bias)))
      };
    })
    .sort((left, right) => Number(right.n) - Number(left.n));

  return { byPresetFamily, overlapCells };
}

function summarizeSurveyRows(rows: RecognizedInputRow[]): {
  rawInputCount: number;
  directInputCount: number;
  tutorialInputCount: number;
  statusCounts: Record<string, number>;
  personalizedDirectChangedCount: number;
  groupRows: Array<Record<string, string | number>>;
} {
  const directRows = rows.filter((row) => row.captureKind === "direct");
  const groupRows = [...groupBy(rows, (row) => `${row.experimentGroup}:${row.captureKind}`).entries()].map(([key, group]) => {
    const [experimentGroup, captureKind] = key.split(":");
    return {
      experiment_group: experimentGroup,
      capture_kind: captureKind,
      n: group.length,
      recognized_rate: rate(group.filter((row) => row.baseline.status === "recognized").length, group.length),
      ambiguous_rate: rate(group.filter((row) => row.baseline.status === "ambiguous").length, group.length),
      avg_top_score: average(group.map((row) => row.baseline.topCandidate?.score ?? 0)),
      avg_score_gap: average(group.map((row) => scoreGapFor(row.baseline))),
      avg_elapsed_ms: average(group.map((row) => row.elapsedMs ?? 0)),
      avg_closure: average(group.map((row) => row.baseline.rawQuality.closure)),
      avg_smoothness: average(group.map((row) => row.baseline.rawQuality.smoothness)),
      avg_stability: average(group.map((row) => row.baseline.rawQuality.stability))
    };
  });

  return {
    rawInputCount: rows.length,
    directInputCount: directRows.length,
    tutorialInputCount: rows.length - directRows.length,
    statusCounts: countBy(rows, (row) => row.baseline.status),
    personalizedDirectChangedCount: directRows.filter(
      (row) =>
        row.personalized &&
        (row.baseline.status !== row.personalized.status || actualFamilyFor(row.baseline) !== actualFamilyFor(row.personalized))
    ).length,
    groupRows
  };
}

function summarizeThresholdRows(
  surveyRows: RecognizedInputRow[],
  syntheticRows: Array<Record<string, string | number | boolean>>
): Record<string, unknown> {
  const directRows = surveyRows.filter((row) => row.captureKind === "direct");
  const surveyDeltas = directRows.map((row) => ({
    statusDelta: row.personalized ? STATUS_RANK[row.personalized.status] - STATUS_RANK[row.baseline.status] : 0,
    topScoreDelta: (row.personalized?.topCandidate?.score ?? 0) - (row.baseline.topCandidate?.score ?? 0),
    effectiveThresholdBias: row.personalized?.personalization?.effectiveThresholdBias ?? 0,
    mlConfidenceGate: row.personalized?.personalization?.mlConfidenceGate ?? 0
  }));

  return {
    surveyDirectRows: directRows.length,
    surveyChangedRows: directRows.filter(
      (row) =>
        row.personalized &&
        (row.baseline.status !== row.personalized.status || actualFamilyFor(row.baseline) !== actualFamilyFor(row.personalized))
    ).length,
    surveyAverageStatusDelta: average(surveyDeltas.map((row) => row.statusDelta)),
    surveyAverageTopScoreDelta: average(surveyDeltas.map((row) => row.topScoreDelta)),
    surveyAverageEffectiveThresholdBias: average(surveyDeltas.map((row) => row.effectiveThresholdBias)),
    surveyAverageMlConfidenceGate: average(surveyDeltas.map((row) => row.mlConfidenceGate)),
    syntheticChangedRows: syntheticRows.filter(
      (row) => Boolean(row.profile_status_changed) || Boolean(row.profile_family_changed)
    ).length,
    syntheticAverageEffectiveThresholdBias: average(
      syntheticRows.map((row) => Number(row.personalized_effective_threshold_bias))
    ),
    syntheticAverageMlConfidenceGate: average(syntheticRows.map((row) => Number(row.personalized_ml_confidence_gate)))
  };
}

function summarizeDuplicateClusters(clusters: Map<string, SurveyRecord[]>): {
  duplicateClusterCount: number;
  rows: Array<Record<string, string | number>>;
} {
  const rows = [...clusters.entries()]
    .filter(([, items]) => items.length > 1)
    .map(([fingerprint, items], index) => ({
      cluster_id: index + 1,
      fingerprint,
      raw_rows: items.length,
      kept_submission_id: stringOrUndefined(items[0]?.payload.submissionId) ?? "",
      submissions: items.map((item) => stringOrUndefined(item.payload.submissionId) ?? "").join("|"),
      source_lines: items.map((item) => `${basename(item.sourceFile)}:${item.sourceLine}`).join("|")
    }));

  return {
    duplicateClusterCount: rows.length,
    rows
  };
}

function toSurveyInputCsvRow(row: RecognizedInputRow): Record<string, string | number | boolean> {
  const personalized = row.personalized;

  return {
    input_id: row.inputId,
    response_index: row.responseIndex,
    submission_id: row.submissionId,
    schema_version: row.schemaVersion,
    experiment_group: row.experimentGroup,
    hci_probe_variant: row.hciProbeVariant,
    capture_kind: row.captureKind,
    target_word: row.targetWord,
    expected_family: row.expectedFamily,
    mode: row.mode,
    elapsed_ms: row.elapsedMs ?? "",
    expression_difficulty: row.expressionDifficulty ?? "",
    stroke_count: row.baseline.features.strokeCount,
    point_count: row.baseline.features.pointCount,
    duration_ms: row.baseline.features.durationMs,
    path_length: row.baseline.features.pathLength,
    stored_status: row.storedRecognitionStatus ?? "",
    stored_family: row.storedRecognizedFamily ?? "",
    baseline_status: row.baseline.status,
    baseline_actual_family: actualFamilyFor(row.baseline),
    baseline_top_family: row.baseline.topCandidate?.family ?? "none",
    baseline_top_score: row.baseline.topCandidate?.score ?? 0,
    baseline_score_gap: scoreGapFor(row.baseline),
    personalized_status: personalized?.status ?? "",
    personalized_actual_family: personalized ? actualFamilyFor(personalized) : "",
    personalized_top_score: personalized?.topCandidate?.score ?? "",
    personalized_score_gap: personalized ? scoreGapFor(personalized) : "",
    profile_status_changed: personalized ? row.baseline.status !== personalized.status : false,
    profile_family_changed: personalized ? actualFamilyFor(row.baseline) !== actualFamilyFor(personalized) : false,
    personalization_stage: personalized?.personalization?.stage ?? "",
    threshold_bias: personalized?.personalization?.thresholdBias ?? "",
    effective_threshold_bias: personalized?.personalization?.effectiveThresholdBias ?? "",
    ml_confidence_gate: personalized?.personalization?.mlConfidenceGate ?? "",
    shadow_mode: personalized?.shadow?.mode ?? "",
    shadow_decision_changed: Boolean(personalized?.shadow?.decisionChanged),
    closure: row.baseline.rawQuality.closure,
    symmetry: row.baseline.rawQuality.symmetry,
    smoothness: row.baseline.rawQuality.smoothness,
    tempo: row.baseline.rawQuality.tempo,
    overshoot: row.baseline.rawQuality.overshoot,
    stability: row.baseline.rawQuality.stability,
    rotation_bias: row.baseline.rawQuality.rotationBias,
    closure_gap: row.baseline.features.closureGap,
    dominant_corners: row.baseline.features.dominantCorners,
    endpoint_clusters: row.baseline.features.endpointClusters,
    circularity: row.baseline.features.circularity,
    fill_ratio: row.baseline.features.fillRatio,
    parallelism: row.baseline.features.parallelism,
    source_file: row.sourceFile,
    source_line: row.sourceLine
  };
}

function toThresholdChangeCsvRow(row: RecognizedInputRow): Record<string, string | number | boolean> {
  const personalized = row.personalized;

  return {
    input_id: row.inputId,
    submission_id: row.submissionId,
    capture_kind: row.captureKind,
    target_word: row.targetWord,
    mode: row.mode,
    experiment_group: row.experimentGroup,
    baseline_status: row.baseline.status,
    personalized_status: personalized?.status ?? "",
    status_delta: personalized ? STATUS_RANK[personalized.status] - STATUS_RANK[row.baseline.status] : 0,
    baseline_family: actualFamilyFor(row.baseline),
    personalized_family: personalized ? actualFamilyFor(personalized) : "",
    family_changed: personalized ? actualFamilyFor(row.baseline) !== actualFamilyFor(personalized) : false,
    baseline_top_score: row.baseline.topCandidate?.score ?? 0,
    personalized_top_score: personalized?.topCandidate?.score ?? "",
    top_score_delta: personalized ? (personalized.topCandidate?.score ?? 0) - (row.baseline.topCandidate?.score ?? 0) : 0,
    baseline_score_gap: scoreGapFor(row.baseline),
    personalized_score_gap: personalized ? scoreGapFor(personalized) : "",
    score_gap_delta: personalized ? scoreGapFor(personalized) - scoreGapFor(row.baseline) : 0,
    profile_sample_count: row.profileSampleCount ?? "",
    threshold_bias: row.profileThresholdBias ?? "",
    effective_threshold_bias: row.profileEffectiveThresholdBias ?? "",
    ml_confidence_gate: personalized?.personalization?.mlConfidenceGate ?? "",
    personalization_stage: personalized?.personalization?.stage ?? ""
  };
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

function thresholdVariantStatus(result: RecognitionResult, mode: "strict" | "loose"): RecognitionStatus {
  const topScore = result.topCandidate?.score ?? 0;
  const gap = scoreGapFor(result);

  if (mode === "strict") {
    if (result.status === "recognized" && (topScore < 0.78 || gap < 0.18)) {
      return "ambiguous";
    }
    if (result.status === "ambiguous" && topScore < 0.58) {
      return "invalid";
    }
    return result.status;
  }

  if (result.status === "ambiguous" && topScore >= 0.62 && gap >= 0.05) {
    return "recognized";
  }
  if (result.status === "incomplete" && topScore >= 0.7 && gap >= 0.1) {
    return "ambiguous";
  }
  return result.status;
}

function scoreGapFor(result: RecognitionResult): number {
  const top = result.topCandidate?.score ?? 0;
  const second = result.candidates[1]?.score ?? 0;
  return Math.max(0, top - second);
}

function actualFamilyFor(result: RecognitionResult): GlyphFamily | "none" {
  return result.canonicalFamily ?? result.topCandidate?.family ?? "none";
}

function isOverlap(expectedFamily: GlyphFamily, status: RecognitionStatus, actualFamily: GlyphFamily | "none"): boolean {
  return status !== "recognized" || actualFamily !== expectedFamily;
}

function writeCsv(path: string, rows: Array<Record<string, unknown>>): void {
  mkdirSync(dirname(path), { recursive: true });
  const columns = [...rows.reduce((set, row) => {
    Object.keys(row).forEach((key) => set.add(key));
    return set;
  }, new Set<string>())];
  const text = [
    columns.join(","),
    ...rows.map((row) => columns.map((column) => csvCell(row[column])).join(","))
  ].join("\n");
  writeFileSync(path, `${text}\n`, "utf8");
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

function countBy<T>(items: T[], keyFor: (item: T) => string): Record<string, number> {
  return Object.fromEntries(
    [...groupBy(items, keyFor).entries()].map(([key, group]) => [key, group.length]).sort(([left], [right]) => left.localeCompare(right))
  );
}

function average(values: number[]): number {
  const finite = values.filter((value) => Number.isFinite(value));
  return finite.length > 0 ? Number((finite.reduce((sum, value) => sum + value, 0) / finite.length).toFixed(6)) : 0;
}

function rate(numerator: number, denominator: number): number {
  return denominator > 0 ? Number((numerator / denominator).toFixed(6)) : 0;
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
  const parts = [
    date.getFullYear(),
    String(date.getMonth() + 1).padStart(2, "0"),
    String(date.getDate()).padStart(2, "0"),
    "-",
    String(date.getHours()).padStart(2, "0"),
    String(date.getMinutes()).padStart(2, "0")
  ];

  return parts.join("");
}

main();
