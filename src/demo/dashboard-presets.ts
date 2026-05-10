import {
  runDashboardBatch,
  summarizeDashboardBatch,
  type DashboardBatchSample,
  type DashboardConfusionRow,
  type DashboardBatchSummary
} from "./dashboard-batch";
import { dashboardFamilyName, dashboardStatusLabel } from "./dashboard-copy";
import { createSeededRandom, type SyntheticInputRecipe } from "./synthetic-input";
import { appendTutorialCapture, createEmptyTutorialProfileStore, mergeTutorializedUserProfile } from "../recognizer/tutorial-profile";
import { createEmptyUserInputProfile } from "../recognizer/user-profile";
import type { GlyphFamily, RecognitionStatus, TutorialProfileStore, UserInputProfile } from "../recognizer/types";

export interface NumericRange {
  min: number;
  max: number;
}

export interface SyntheticInputRange {
  family: GlyphFamily;
  seed: number;
  jitterPx: NumericRange;
  openGapRatio: NumericRange;
  rotationDeg: NumericRange;
  curveWarp: NumericRange;
  extraNoiseStrokeCount: NumericRange;
  pointDensity: number;
}

export interface DashboardScenarioPreset {
  id: string;
  label: string;
  description: string;
  range: SyntheticInputRange;
  iterations: number;
}

export interface DashboardComparisonLane {
  id: "baseline" | "tutorial" | "threshold_strict" | "threshold_loose";
  label: string;
  summary: DashboardBatchSummary;
  recognizedRate: number;
  ambiguousRate: number;
  incompleteRate: number;
  invalidRate: number;
  averageScoreGap: number;
  personalizedChangedCount: number;
  userSummary: string;
}

export interface DashboardComparisonSummary {
  presetId: string;
  recipe: SyntheticInputRecipe;
  iterations: number;
  lanes: DashboardComparisonLane[];
  userSummary: string;
}

export interface DashboardFamilyActualDistribution {
  family: GlyphFamily | "none";
  count: number;
  rate: number;
}

export interface DashboardFamilyRunSummary {
  family: GlyphFamily;
  total: number;
  statusCounts: Record<RecognitionStatus, number>;
  recognizedRate: number;
  ambiguousRate: number;
  incompleteRate: number;
  invalidRate: number;
  averageTopScore: number;
  averageScoreGap: number;
  topActualFamilies: DashboardFamilyActualDistribution[];
  summary: DashboardBatchSummary;
}

export interface DashboardOverlapCell {
  expectedFamily: GlyphFamily;
  actualFamily: GlyphFamily | "none";
  status: RecognitionStatus;
  count: number;
  rate: number;
  averageTopScore: number;
  averageScoreGap: number;
  averageClosure: number;
  averageRotationBias: number;
  averageJitterPx: number;
  averageOpenGapRatio: number;
  averageRotationDeg: number;
  averageCurveWarp: number;
  averageNoiseStrokeCount: number;
  settingHint: string;
}

export interface DashboardOverlapSettingSummary {
  id: string;
  label: string;
  expectedFamilies: GlyphFamily[];
  actualFamily: GlyphFamily | "none";
  status: RecognitionStatus;
  count: number;
  averageTopScore: number;
  averageScoreGap: number;
  settingHint: string;
}

export interface DashboardFamilyMatrixSummary {
  id: string;
  createdAt: number;
  iterationsPerFamily: number;
  seedStart: number;
  range: SyntheticInputRange;
  familySummaries: DashboardFamilyRunSummary[];
  samples: DashboardBatchSample[];
  statusCounts: Record<RecognitionStatus, number>;
  recognizedByFamily: Record<GlyphFamily, number>;
  confusionRows: DashboardConfusionRow[];
  overlapCells: DashboardOverlapCell[];
  overlapSettings: DashboardOverlapSettingSummary[];
  userSummary: string;
}

export const DASHBOARD_MATRIX_FAMILIES: GlyphFamily[] = ["wind", "earth", "fire", "water", "life"];

export const DEFAULT_SYNTHETIC_RANGE: SyntheticInputRange = {
  family: "water",
  seed: 42,
  jitterPx: { min: 0, max: 3 },
  openGapRatio: { min: 0, max: 0.04 },
  rotationDeg: { min: -6, max: 6 },
  curveWarp: { min: 0, max: 0.06 },
  extraNoiseStrokeCount: { min: 0, max: 0 },
  pointDensity: 5
};

export const DASHBOARD_SCENARIO_PRESETS: DashboardScenarioPreset[] = [
  {
    id: "stable_baseline",
    label: "안정 baseline",
    description: "낮은 변형으로 기본 인정률과 점수 간격을 확인합니다.",
    range: DEFAULT_SYNTHETIC_RANGE,
    iterations: 80
  },
  {
    id: "open_gap_stress",
    label: "open gap stress",
    description: "닫힘이 벌어진 입력에서 incomplete/ambiguous 증가를 확인합니다.",
    range: {
      ...DEFAULT_SYNTHETIC_RANGE,
      openGapRatio: { min: 0.22, max: 0.52 },
      jitterPx: { min: 3, max: 12 }
    },
    iterations: 120
  },
  {
    id: "rotation_drift",
    label: "rotation drift",
    description: "기울기 변화가 family 판정과 분포에 주는 영향을 봅니다.",
    range: {
      ...DEFAULT_SYNTHETIC_RANGE,
      rotationDeg: { min: -42, max: 42 },
      curveWarp: { min: 0.04, max: 0.28 }
    },
    iterations: 120
  },
  {
    id: "jitter_noise_stress",
    label: "jitter/noise stress",
    description: "손떨림과 잡선이 threshold 변형에 어떻게 반응하는지 비교합니다.",
    range: {
      ...DEFAULT_SYNTHETIC_RANGE,
      jitterPx: { min: 10, max: 24 },
      extraNoiseStrokeCount: { min: 1, max: 5 }
    },
    iterations: 140
  },
  {
    id: "seal_ring_pass_fail",
    label: "감싸는 원 통과/실패",
    description: "기본 도형을 감싸는 원이 있을 때와 없을 때의 차이를 테스트합니다.",
    range: {
      ...DEFAULT_SYNTHETIC_RANGE,
      family: "water",
      openGapRatio: { min: 0, max: 0.08 },
      curveWarp: { min: 0, max: 0.1 }
    },
    iterations: 80
  },
  {
    id: "tutorial_contrast",
    label: "tutorial before/after",
    description: "동일 seed set에서 튜토리얼 반영 전후의 차이를 크게 보여 줍니다.",
    range: {
      ...DEFAULT_SYNTHETIC_RANGE,
      family: "earth",
      rotationDeg: { min: -22, max: 22 },
      jitterPx: { min: 4, max: 14 }
    },
    iterations: 120
  },
  {
    id: "threshold_variants",
    label: "threshold strict/loose",
    description: "같은 입력군에 엄격/완화 threshold simulation을 적용합니다.",
    range: {
      ...DEFAULT_SYNTHETIC_RANGE,
      family: "fire",
      openGapRatio: { min: 0.12, max: 0.36 },
      jitterPx: { min: 5, max: 16 }
    },
    iterations: 140
  },
  {
    id: "operator_order_smoke",
    label: "operator order smoke",
    description: "operator dependency/order 설명이 실제 입력 테스트와 같이 보이도록 묶습니다.",
    range: {
      ...DEFAULT_SYNTHETIC_RANGE,
      family: "life",
      rotationDeg: { min: -18, max: 18 },
      curveWarp: { min: 0.1, max: 0.32 }
    },
    iterations: 100
  }
];

export function findDashboardScenarioPreset(id: string | null | undefined): DashboardScenarioPreset {
  return DASHBOARD_SCENARIO_PRESETS.find((preset) => preset.id === id) ?? DASHBOARD_SCENARIO_PRESETS[0];
}

export function buildSyntheticRecipeFromRange(range: SyntheticInputRange, seed = range.seed): SyntheticInputRecipe {
  const random = createSeededRandom(seed);

  return {
    family: range.family,
    seed,
    jitterPx: randomInRange(range.jitterPx, random),
    openGapRatio: randomInRange(range.openGapRatio, random),
    rotationDeg: randomInRange(range.rotationDeg, random),
    curveWarp: randomInRange(range.curveWarp, random),
    extraNoiseStrokeCount: Math.round(randomInRange(range.extraNoiseStrokeCount, random)),
    pointDensity: range.pointDensity
  };
}

export function rangeFromRecipe(recipe: SyntheticInputRecipe): SyntheticInputRange {
  return {
    family: recipe.family,
    seed: recipe.seed ?? DEFAULT_SYNTHETIC_RANGE.seed,
    jitterPx: fixedRange(recipe.jitterPx ?? 0),
    openGapRatio: fixedRange(recipe.openGapRatio ?? 0),
    rotationDeg: fixedRange(recipe.rotationDeg ?? 0),
    curveWarp: fixedRange(recipe.curveWarp ?? 0),
    extraNoiseStrokeCount: fixedRange(recipe.extraNoiseStrokeCount ?? 0),
    pointDensity: recipe.pointDensity ?? DEFAULT_SYNTHETIC_RANGE.pointDensity
  };
}

export function runDashboardComparison(input: {
  presetId?: string;
  recipe: SyntheticInputRecipe;
  iterations: number;
  seedStart?: number;
  baselineProfile?: UserInputProfile;
}): DashboardComparisonSummary {
  const iterations = clampInteger(input.iterations, 1, 1000);
  const seedStart = input.seedStart ?? input.recipe.seed ?? 1;
  const baseline = runDashboardBatch({
    recipe: input.recipe,
    iterations,
    seedStart,
    profile: input.baselineProfile
  });
  const tutorialProfile = createDashboardSmokeTutorialProfile(input.recipe, seedStart);
  const tutorial = runDashboardBatch({
    recipe: input.recipe,
    iterations,
    seedStart,
    profile: tutorialProfile
  });
  const strict = summarizeDashboardBatch(input.recipe, baseline.samples.map((sample) => applyThresholdVariant(sample, "strict")));
  const loose = summarizeDashboardBatch(input.recipe, baseline.samples.map((sample) => applyThresholdVariant(sample, "loose")));
  const lanes = [
    buildLane("baseline", "기준선", baseline),
    buildLane("tutorial", "튜토리얼 후", tutorial),
    buildLane("threshold_strict", "threshold 엄격", strict),
    buildLane("threshold_loose", "threshold 완화", loose)
  ];

  return {
    presetId: input.presetId ?? "custom",
    recipe: input.recipe,
    iterations,
    lanes,
    userSummary: `${iterations}회 동일 seed set으로 기준선, 튜토리얼 후, threshold 엄격/완화 결과를 비교했습니다.`
  };
}

export function runDashboardFamilyMatrix(input: {
  range: SyntheticInputRange;
  iterations: number;
  seedStart?: number;
  profile?: UserInputProfile;
  families?: GlyphFamily[];
}): DashboardFamilyMatrixSummary {
  const iterations = clampInteger(input.iterations, 1, 1000);
  const seedStart = input.seedStart ?? input.range.seed ?? 1;
  const families = input.families?.length ? input.families : DASHBOARD_MATRIX_FAMILIES;
  const familySummaries: DashboardFamilyRunSummary[] = [];
  const samples: DashboardBatchSample[] = [];

  families.forEach((family, familyIndex) => {
    const familySeed = seedStart + familyIndex * 10_000;
    const familyRange: SyntheticInputRange = {
      ...input.range,
      family,
      seed: familySeed
    };
    const recipe = buildSyntheticRecipeFromRange(familyRange, familySeed);
    const summary = runDashboardBatch({
      recipe,
      iterations,
      seedStart: familySeed,
      profile: input.profile
    });
    const offset = familyIndex * iterations;

    familySummaries.push(buildFamilyRunSummary(family, summary));
    samples.push(...summary.samples.map((sample) => ({ ...sample, index: offset + sample.index })));
  });

  const statusCounts = createEmptyStatusCounts();
  const recognizedByFamily = createEmptyFamilyCounts();

  for (const sample of samples) {
    statusCounts[sample.status] += 1;
    if (sample.status === "recognized" && sample.actualFamily !== "none") {
      recognizedByFamily[sample.actualFamily] += 1;
    }
  }

  const total = Math.max(samples.length, 1);
  const overlapCells = buildDashboardOverlapCells(samples);
  const overlapSettings = buildDashboardOverlapSettings(samples);
  const confusionRows = buildMatrixConfusionRows(samples);
  const recognizedRate = roundRate(statusCounts.recognized / total);
  const overlapCount = overlapCells.reduce((sum, cell) => sum + cell.count, 0);

  return {
    id: `matrix-${seedStart}-${iterations}-${families.join("-")}-${Date.now()}`,
    createdAt: Date.now(),
    iterationsPerFamily: iterations,
    seedStart,
    range: { ...input.range, seed: seedStart },
    familySummaries,
    samples,
    statusCounts,
    recognizedByFamily,
    confusionRows,
    overlapCells,
    overlapSettings,
    userSummary: `${families.length}개 모양을 각각 ${iterations}회 생성했습니다. 전체 인정률은 ${recognizedRate}%이고, 겹치거나 보류된 입력은 ${overlapCount}건입니다.`
  };
}

export function accumulateDashboardFamilyMatrixSummaries(
  summaries: readonly DashboardFamilyMatrixSummary[]
): DashboardFamilyMatrixSummary | null {
  const matrixRuns = summaries.filter((summary) => summary.samples.length > 0);

  if (matrixRuns.length === 0) {
    return null;
  }

  if (matrixRuns.length === 1) {
    return matrixRuns[0];
  }

  const newest = matrixRuns[0];
  const oldest = matrixRuns[matrixRuns.length - 1];
  const samples: DashboardBatchSample[] = [];
  let sampleOffset = 0;

  for (const summary of [...matrixRuns].reverse()) {
    samples.push(
      ...summary.samples.map((sample, index) => ({
        ...sample,
        index: sampleOffset + index
      }))
    );
    sampleOffset += summary.samples.length;
  }

  const families = DASHBOARD_MATRIX_FAMILIES.filter((family) =>
    samples.some((sample) => sample.expectedFamily === family)
  );
  const statusCounts = createEmptyStatusCounts();
  const recognizedByFamily = createEmptyFamilyCounts();

  for (const sample of samples) {
    statusCounts[sample.status] += 1;
    if (sample.status === "recognized" && sample.actualFamily !== "none") {
      recognizedByFamily[sample.actualFamily] += 1;
    }
  }

  const familySummaries = families.map((family) => {
    const familySamples = samples.filter((sample) => sample.expectedFamily === family);
    const familyRange: SyntheticInputRange = {
      ...newest.range,
      family,
      seed: newest.seedStart
    };
    const summary = summarizeDashboardBatch(buildSyntheticRecipeFromRange(familyRange, newest.seedStart), familySamples);

    return buildFamilyRunSummary(family, summary);
  });
  const total = Math.max(samples.length, 1);
  const overlapCells = buildDashboardOverlapCells(samples);
  const overlapSettings = buildDashboardOverlapSettings(samples);
  const confusionRows = buildMatrixConfusionRows(samples);
  const recognizedRate = roundRate(statusCounts.recognized / total);
  const overlapCount = overlapCells.reduce((sum, cell) => sum + cell.count, 0);
  const iterationsPerFamily = Math.round(samples.length / Math.max(familySummaries.length, 1));

  return {
    id: `matrix-cumulative-${oldest.seedStart}-${newest.seedStart}-${matrixRuns.length}`,
    createdAt: newest.createdAt,
    iterationsPerFamily,
    seedStart: newest.seedStart,
    range: { ...newest.range },
    familySummaries,
    samples,
    statusCounts,
    recognizedByFamily,
    confusionRows,
    overlapCells,
    overlapSettings,
    userSummary: `${matrixRuns.length}개 실행을 누적해 ${familySummaries.length}개 모양 ${samples.length}건을 비교했습니다. 전체 인정률은 ${recognizedRate}%이고, 겹치거나 보류된 입력은 ${overlapCount}건입니다.`
  };
}

export function createDashboardSmokeTutorialProfile(recipe: SyntheticInputRecipe, seedStart = recipe.seed ?? 1): UserInputProfile {
  let store: TutorialProfileStore = createEmptyTutorialProfileStore(1_700_000_000_000 + seedStart);

  for (let index = 0; index < 6; index += 1) {
    const sampleRecipe: SyntheticInputRecipe = {
      ...recipe,
      seed: seedStart + index,
      jitterPx: Math.max(0, (recipe.jitterPx ?? 0) * 0.55),
      openGapRatio: Math.max(0, (recipe.openGapRatio ?? 0) * 0.45),
      rotationDeg: (recipe.rotationDeg ?? 0) * 0.5,
      extraNoiseStrokeCount: 0
    };
    const summary = runDashboardBatch({ recipe: sampleRecipe, iterations: 1, seedStart: sampleRecipe.seed });
    const sample = summary.samples[0];

    if (!sample) {
      continue;
    }

    store = appendTutorialCapture(store, {
      id: `dashboard-smoke-${recipe.family}-${seedStart}-${index}`,
      kind: "family",
      expectedFamily: recipe.family,
      strokes: sample.session.strokes,
      source: index % 2 === 0 ? "trace" : "variation",
      timestamp: 1_700_000_000_000 + seedStart + index,
      validation: {
        reliability: "high",
        expectedLabel: recipe.family,
        actualTopLabel: recipe.family,
        status: "recognized",
        topScore: Math.max(sample.topScore, 0.86),
        margin: Math.max(sample.scoreGap, 0.18),
        quality: sample.result.rawQuality
      }
    });
  }

  return mergeTutorializedUserProfile(createEmptyUserInputProfile(), store);
}

function buildLane(
  id: DashboardComparisonLane["id"],
  label: string,
  summary: DashboardBatchSummary
): DashboardComparisonLane {
  const total = Math.max(summary.total, 1);

  return {
    id,
    label,
    summary,
    recognizedRate: roundRate(summary.statusCounts.recognized / total),
    ambiguousRate: roundRate(summary.statusCounts.ambiguous / total),
    incompleteRate: roundRate(summary.statusCounts.incomplete / total),
    invalidRate: roundRate(summary.statusCounts.invalid / total),
    averageScoreGap: roundMetric(
      summary.samples.reduce((sum, sample) => sum + sample.scoreGap, 0) / total
    ),
    personalizedChangedCount: summary.personalizedChangedCount,
    userSummary: summary.userSummary
  };
}

function applyThresholdVariant(sample: DashboardBatchSample, mode: "strict" | "loose"): DashboardBatchSample {
  const nextStatus = resolveThresholdVariantStatus(sample, mode);

  return {
    ...sample,
    status: nextStatus,
    actualFamily: nextStatus === "recognized" ? sample.actualFamily : sample.actualFamily
  };
}

function resolveThresholdVariantStatus(sample: DashboardBatchSample, mode: "strict" | "loose"): RecognitionStatus {
  if (mode === "strict") {
    if (sample.status === "recognized" && (sample.topScore < 0.78 || sample.scoreGap < 0.18)) {
      return "ambiguous";
    }
    if (sample.status === "ambiguous" && sample.topScore < 0.58) {
      return "invalid";
    }
    return sample.status;
  }

  if (sample.status === "ambiguous" && sample.topScore >= 0.62 && sample.scoreGap >= 0.05) {
    return "recognized";
  }
  if (sample.status === "incomplete" && sample.topScore >= 0.7 && sample.scoreGap >= 0.1) {
    return "ambiguous";
  }
  return sample.status;
}

function buildFamilyRunSummary(family: GlyphFamily, summary: DashboardBatchSummary): DashboardFamilyRunSummary {
  const total = Math.max(summary.total, 1);
  const actualCounts = new Map<GlyphFamily | "none", number>();
  let topScore = 0;
  let scoreGap = 0;

  for (const sample of summary.samples) {
    actualCounts.set(sample.actualFamily, (actualCounts.get(sample.actualFamily) ?? 0) + 1);
    topScore += sample.topScore;
    scoreGap += sample.scoreGap;
  }

  return {
    family,
    total: summary.total,
    statusCounts: summary.statusCounts,
    recognizedRate: roundRate(summary.statusCounts.recognized / total),
    ambiguousRate: roundRate(summary.statusCounts.ambiguous / total),
    incompleteRate: roundRate(summary.statusCounts.incomplete / total),
    invalidRate: roundRate(summary.statusCounts.invalid / total),
    averageTopScore: roundMetric(topScore / total),
    averageScoreGap: roundMetric(scoreGap / total),
    topActualFamilies: [...actualCounts.entries()]
      .map(([actualFamily, count]) => ({
        family: actualFamily,
        count,
        rate: roundRate(count / total)
      }))
      .sort((left, right) => right.count - left.count)
      .slice(0, 3),
    summary
  };
}

function buildDashboardOverlapCells(samples: DashboardBatchSample[]): DashboardOverlapCell[] {
  const expectedTotals = new Map<GlyphFamily, number>();
  const groups = new Map<string, DashboardOverlapAccumulator>();

  for (const sample of samples) {
    expectedTotals.set(sample.expectedFamily, (expectedTotals.get(sample.expectedFamily) ?? 0) + 1);

    if (!isOverlapSample(sample)) {
      continue;
    }

    const key = `${sample.expectedFamily}:${sample.actualFamily}:${sample.status}`;
    const group = groups.get(key) ?? createOverlapAccumulator(sample.expectedFamily, sample.actualFamily, sample.status);
    addOverlapSample(group, sample);
    groups.set(key, group);
  }

  return [...groups.values()]
    .map((group) => {
      const count = group.count;
      const expectedTotal = Math.max(expectedTotals.get(group.expectedFamily) ?? count, 1);

      return {
        expectedFamily: group.expectedFamily,
        actualFamily: group.actualFamily,
        status: group.status,
        count,
        rate: roundRate(count / expectedTotal),
        averageTopScore: roundMetric(group.topScore / count),
        averageScoreGap: roundMetric(group.scoreGap / count),
        averageClosure: roundMetric(group.closure / count),
        averageRotationBias: roundMetric(group.rotationBias / count),
        averageJitterPx: roundMetric(group.jitterPx / count),
        averageOpenGapRatio: roundMetric(group.openGapRatio / count),
        averageRotationDeg: roundMetric(group.rotationDeg / count),
        averageCurveWarp: roundMetric(group.curveWarp / count),
        averageNoiseStrokeCount: roundMetric(group.extraNoiseStrokeCount / count),
        settingHint: formatSettingHint(group, count)
      };
    })
    .sort((left, right) => right.count - left.count || right.averageTopScore - left.averageTopScore)
    .slice(0, 12);
}

function buildDashboardOverlapSettings(samples: DashboardBatchSample[]): DashboardOverlapSettingSummary[] {
  const groups = new Map<string, DashboardOverlapSettingAccumulator>();

  for (const sample of samples) {
    if (!isOverlapSample(sample)) {
      continue;
    }

    const bucket = buildSettingBucket(sample);
    const key = `${sample.actualFamily}:${sample.status}:${bucket}`;
    const group = groups.get(key) ?? {
      actualFamily: sample.actualFamily,
      status: sample.status,
      bucket,
      expectedFamilies: new Set<GlyphFamily>(),
      count: 0,
      topScore: 0,
      scoreGap: 0,
      jitterPx: 0,
      openGapRatio: 0,
      rotationDeg: 0,
      curveWarp: 0,
      extraNoiseStrokeCount: 0
    };

    group.expectedFamilies.add(sample.expectedFamily);
    group.count += 1;
    group.topScore += sample.topScore;
    group.scoreGap += sample.scoreGap;
    group.jitterPx += sample.jitterPx;
    group.openGapRatio += sample.openGapRatio;
    group.rotationDeg += sample.rotationDeg;
    group.curveWarp += sample.curveWarp;
    group.extraNoiseStrokeCount += sample.extraNoiseStrokeCount;
    groups.set(key, group);
  }

  return [...groups.values()]
    .map((group) => {
      const count = Math.max(group.count, 1);
      const expectedFamilies = [...group.expectedFamilies];
      const actualLabel = group.actualFamily === "none" ? "판정 없음" : `${dashboardFamilyName(group.actualFamily)} 판정`;

      return {
        id: `${group.actualFamily}-${group.status}-${group.bucket}`,
        label: `${actualLabel} · ${dashboardStatusLabel(group.status)}`,
        expectedFamilies,
        actualFamily: group.actualFamily,
        status: group.status,
        count: group.count,
        averageTopScore: roundMetric(group.topScore / count),
        averageScoreGap: roundMetric(group.scoreGap / count),
        settingHint: `${group.bucket} · ${formatSettingHint(group, count)}`
      };
    })
    .sort(
      (left, right) =>
        right.expectedFamilies.length - left.expectedFamilies.length ||
        right.count - left.count ||
        left.averageScoreGap - right.averageScoreGap
    )
    .slice(0, 8);
}

function buildMatrixConfusionRows(samples: DashboardBatchSample[]): DashboardConfusionRow[] {
  const confusion = new Map<string, DashboardConfusionRow>();

  for (const sample of samples) {
    const expected = dashboardFamilyName(sample.expectedFamily);
    const actual = sample.actualFamily === "none" ? "판정 없음" : dashboardFamilyName(sample.actualFamily);
    const key = `${expected}->${actual}`;
    const row = confusion.get(key) ?? { expected, actual, count: 0 };
    row.count += 1;
    confusion.set(key, row);
  }

  return [...confusion.values()].sort((left, right) => right.count - left.count);
}

function isOverlapSample(sample: DashboardBatchSample): boolean {
  return sample.status !== "recognized" || sample.actualFamily !== sample.expectedFamily;
}

interface DashboardOverlapAccumulator {
  expectedFamily: GlyphFamily;
  actualFamily: GlyphFamily | "none";
  status: RecognitionStatus;
  count: number;
  topScore: number;
  scoreGap: number;
  closure: number;
  rotationBias: number;
  jitterPx: number;
  openGapRatio: number;
  rotationDeg: number;
  curveWarp: number;
  extraNoiseStrokeCount: number;
}

interface DashboardOverlapSettingAccumulator {
  actualFamily: GlyphFamily | "none";
  status: RecognitionStatus;
  bucket: string;
  expectedFamilies: Set<GlyphFamily>;
  count: number;
  topScore: number;
  scoreGap: number;
  jitterPx: number;
  openGapRatio: number;
  rotationDeg: number;
  curveWarp: number;
  extraNoiseStrokeCount: number;
}

function createOverlapAccumulator(
  expectedFamily: GlyphFamily,
  actualFamily: GlyphFamily | "none",
  status: RecognitionStatus
): DashboardOverlapAccumulator {
  return {
    expectedFamily,
    actualFamily,
    status,
    count: 0,
    topScore: 0,
    scoreGap: 0,
    closure: 0,
    rotationBias: 0,
    jitterPx: 0,
    openGapRatio: 0,
    rotationDeg: 0,
    curveWarp: 0,
    extraNoiseStrokeCount: 0
  };
}

function addOverlapSample(group: DashboardOverlapAccumulator, sample: DashboardBatchSample): void {
  group.count += 1;
  group.topScore += sample.topScore;
  group.scoreGap += sample.scoreGap;
  group.closure += sample.closure;
  group.rotationBias += sample.rotationBias;
  group.jitterPx += sample.jitterPx;
  group.openGapRatio += sample.openGapRatio;
  group.rotationDeg += sample.rotationDeg;
  group.curveWarp += sample.curveWarp;
  group.extraNoiseStrokeCount += sample.extraNoiseStrokeCount;
}

function buildSettingBucket(sample: DashboardBatchSample): string {
  const parts: string[] = [];

  if (sample.jitterPx >= 10) {
    parts.push("떨림 큼");
  }
  if (sample.openGapRatio >= 0.18) {
    parts.push("열린 틈 큼");
  }
  if (Math.abs(sample.rotationDeg) >= 24) {
    parts.push("회전 큼");
  }
  if (sample.curveWarp >= 0.18) {
    parts.push("곡선 변형 큼");
  }
  if (sample.extraNoiseStrokeCount >= 1) {
    parts.push("잡선 포함");
  }

  return parts.length > 0 ? parts.join(" + ") : "기본 범위";
}

function formatSettingHint(
  group: Pick<
    DashboardOverlapAccumulator | DashboardOverlapSettingAccumulator,
    "jitterPx" | "openGapRatio" | "rotationDeg" | "curveWarp" | "extraNoiseStrokeCount"
  >,
  count: number
): string {
  const divisor = Math.max(count, 1);

  return [
    `떨림 ${(group.jitterPx / divisor).toFixed(1)}px`,
    `열린 틈 ${((group.openGapRatio / divisor) * 100).toFixed(0)}%`,
    `회전 ${(group.rotationDeg / divisor).toFixed(0)}도`,
    `곡선 ${((group.curveWarp / divisor) * 100).toFixed(0)}%`,
    `잡선 ${(group.extraNoiseStrokeCount / divisor).toFixed(1)}개`
  ].join(" · ");
}

function createEmptyStatusCounts(): Record<RecognitionStatus, number> {
  return {
    recognized: 0,
    ambiguous: 0,
    incomplete: 0,
    invalid: 0
  };
}

function createEmptyFamilyCounts(): Record<GlyphFamily, number> {
  return {
    wind: 0,
    earth: 0,
    fire: 0,
    water: 0,
    life: 0
  };
}

function randomInRange(range: NumericRange, random: ReturnType<typeof createSeededRandom>): number {
  const minimum = Math.min(range.min, range.max);
  const maximum = Math.max(range.min, range.max);
  return minimum + (maximum - minimum) * random.next();
}

function fixedRange(value: number): NumericRange {
  return { min: value, max: value };
}

function roundRate(value: number): number {
  return Math.round(value * 1000) / 10;
}

function roundMetric(value: number): number {
  return Math.round(value * 1000) / 1000;
}

function clampInteger(value: number, minimum: number, maximum: number): number {
  return Math.max(minimum, Math.min(maximum, Math.round(Number.isFinite(value) ? value : minimum)));
}
