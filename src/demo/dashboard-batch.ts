import { recognizeSession } from "../recognizer/recognize";
import type { GlyphFamily, QualityVector, RecognitionResult, RecognitionStatus, StrokeSession, UserInputProfile } from "../recognizer/types";
import { dashboardFamilyName, dashboardStatusLabel, describeDashboardStatus, ensureDashboardUserCopy } from "./dashboard-copy";
import { buildSyntheticStrokeSession, describeSyntheticRecipe, type SyntheticInputRecipe } from "./synthetic-input";

export interface DashboardBatchConfig {
  recipe: SyntheticInputRecipe;
  iterations: number;
  seedStart?: number;
  sealed?: boolean;
  profile?: UserInputProfile;
}

export interface DashboardBatchSample {
  index: number;
  expectedFamily: GlyphFamily;
  actualFamily: GlyphFamily | "none";
  status: RecognitionStatus;
  topScore: number;
  scoreGap: number;
  jitterPx: number;
  openGapRatio: number;
  rotationDeg: number;
  curveWarp: number;
  extraNoiseStrokeCount: number;
  closure: number;
  smoothness: number;
  stability: number;
  rotationBias: number;
  shadowChanged: boolean;
  personalizedChanged: boolean;
  session: StrokeSession;
  result: RecognitionResult;
}

export interface DashboardConfusionRow {
  expected: string;
  actual: string;
  count: number;
}

export interface DashboardBatchSummary {
  total: number;
  recipe: SyntheticInputRecipe;
  recipeLabel: string;
  statusCounts: Record<RecognitionStatus, number>;
  familyCounts: Record<string, number>;
  confusionRows: DashboardConfusionRow[];
  samples: DashboardBatchSample[];
  qualityAverages: Pick<QualityVector, "closure" | "smoothness" | "stability" | "rotationBias">;
  shadowChangedCount: number;
  personalizedChangedCount: number;
  userSummary: string;
}

export function runDashboardBatch(config: DashboardBatchConfig): DashboardBatchSummary {
  const iterations = clampInteger(config.iterations, 1, 5000);
  const seedStart = config.seedStart ?? config.recipe.seed ?? 1;
  const samples: DashboardBatchSample[] = [];

  for (let index = 0; index < iterations; index += 1) {
    const recipe = { ...config.recipe, seed: seedStart + index };
    const session = buildSyntheticStrokeSession(recipe);
    const result = recognizeSession(session, { sealed: config.sealed ?? true, profile: config.profile });
    samples.push(buildBatchSample(index, recipe.family, session, result, recipe));
  }

  return summarizeDashboardBatch(config.recipe, samples);
}

export function summarizeDashboardBatch(recipe: SyntheticInputRecipe, samples: DashboardBatchSample[]): DashboardBatchSummary {
  const statusCounts = createEmptyStatusCounts();
  const familyCounts: Record<string, number> = {};
  const confusion = new Map<string, DashboardConfusionRow>();
  let closure = 0;
  let smoothness = 0;
  let stability = 0;
  let rotationBias = 0;
  let shadowChangedCount = 0;
  let personalizedChangedCount = 0;

  for (const sample of samples) {
    statusCounts[sample.status] += 1;
    familyCounts[sample.actualFamily] = (familyCounts[sample.actualFamily] ?? 0) + 1;
    const actualLabel = sample.actualFamily === "none" ? "아직 없음" : dashboardFamilyName(sample.actualFamily);
    const expectedLabel = dashboardFamilyName(sample.expectedFamily);
    const key = `${expectedLabel}->${actualLabel}`;
    const row = confusion.get(key) ?? { expected: expectedLabel, actual: actualLabel, count: 0 };
    row.count += 1;
    confusion.set(key, row);
    closure += sample.closure;
    smoothness += sample.smoothness;
    stability += sample.stability;
    rotationBias += sample.rotationBias;
    shadowChangedCount += sample.shadowChanged ? 1 : 0;
    personalizedChangedCount += sample.personalizedChanged ? 1 : 0;
  }

  const total = samples.length;
  const recognized = statusCounts.recognized;
  const userSummary = ensureDashboardUserCopy(
    `${describeSyntheticRecipe(recipe)} ${total}회 테스트: ${recognized}회 인정됨, ${statusCounts.ambiguous}회 헷갈림, ${statusCounts.incomplete}회 아직 부족함.`
  );

  return {
    total,
    recipe,
    recipeLabel: describeSyntheticRecipe(recipe),
    statusCounts,
    familyCounts,
    confusionRows: [...confusion.values()].sort((left, right) => right.count - left.count),
    samples,
    qualityAverages: {
      closure: average(closure, total),
      smoothness: average(smoothness, total),
      stability: average(stability, total),
      rotationBias: average(rotationBias, total)
    },
    shadowChangedCount,
    personalizedChangedCount,
    userSummary
  };
}

export function buildDashboardSingleResult(recipe: SyntheticInputRecipe, profile?: UserInputProfile): DashboardBatchSample {
  const session = buildSyntheticStrokeSession(recipe);
  return buildDashboardSampleFromSession(recipe.family, session, profile, recipe);
}

export function buildDashboardSampleFromSession(
  expectedFamily: GlyphFamily,
  session: StrokeSession,
  profile?: UserInputProfile,
  recipe?: Partial<SyntheticInputRecipe>
): DashboardBatchSample {
  const result = recognizeSession(session, { sealed: true, profile });
  return buildBatchSample(0, expectedFamily, session, result, recipe);
}

function buildBatchSample(
  index: number,
  expectedFamily: GlyphFamily,
  session: StrokeSession,
  result: RecognitionResult,
  recipe?: Partial<SyntheticInputRecipe>
): DashboardBatchSample {
  const topScore = result.topCandidate?.score ?? 0;
  const secondScore = result.candidates[1]?.score ?? 0;
  const actualFamily = result.canonicalFamily ?? result.topCandidate?.family ?? "none";

  return {
    index,
    expectedFamily,
    actualFamily,
    status: result.status,
    topScore,
    scoreGap: Math.max(0, topScore - secondScore),
    jitterPx: recipe?.jitterPx ?? 0,
    openGapRatio: recipe?.openGapRatio ?? 0,
    rotationDeg: recipe?.rotationDeg ?? 0,
    curveWarp: recipe?.curveWarp ?? 0,
    extraNoiseStrokeCount: recipe?.extraNoiseStrokeCount ?? 0,
    closure: result.rawQuality.closure,
    smoothness: result.rawQuality.smoothness,
    stability: result.rawQuality.stability,
    rotationBias: result.rawQuality.rotationBias,
    shadowChanged: Boolean(result.shadow?.decisionChanged || result.shadow?.statusChanged),
    personalizedChanged: Boolean(result.shadow?.personalizedDecisionChanged || result.shadow?.personalizedStatusChanged),
    session,
    result
  };
}

function createEmptyStatusCounts(): Record<RecognitionStatus, number> {
  return {
    recognized: 0,
    ambiguous: 0,
    incomplete: 0,
    invalid: 0
  };
}

function average(sum: number, count: number): number {
  return count > 0 ? Number((sum / count).toFixed(4)) : 0;
}

function clampInteger(value: number, minimum: number, maximum: number): number {
  return Math.max(minimum, Math.min(maximum, Math.round(Number.isFinite(value) ? value : minimum)));
}

export function describeDashboardSample(sample: DashboardBatchSample): string {
  return ensureDashboardUserCopy(
    `${dashboardStatusLabel(sample.status)} · ${describeDashboardStatus(sample.status, sample.actualFamily === "none" ? undefined : sample.actualFamily)} 후보 점수 ${(sample.topScore * 100).toFixed(1)}점.`
  );
}
