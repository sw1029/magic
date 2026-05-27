export const KNOWN_SURVEY_OUTLIER_SUBMISSION_IDS = new Set([
  "6edcac06-88dd-4701-be2c-11e37a2be62c"
]);

export interface SurveyOutlierInput {
  submissionId: string;
  inputId: string;
  captureKind: "direct" | "tutorial" | string;
  targetWord?: string;
  strokeCount: number;
  pointCount: number;
  topScore?: number;
  scoreGap?: number;
  closure?: number;
  smoothness?: number;
  stability?: number;
  rotationBias?: number;
}

export interface SurveyOutlierReport {
  submissionId: string;
  inputCount: number;
  reason: string;
  inputIds: string[];
  avgStrokeCount: number;
  avgPointCount: number;
  avgTopScore: number;
  avgScoreGap: number;
  avgClosure: number;
  avgSmoothness: number;
  avgStability: number;
  avgRotationBias: number;
}

export function detectSurveyOutlierRespondents(rows: SurveyOutlierInput[]): SurveyOutlierReport[] {
  const directBySubmission = new Map<string, SurveyOutlierInput[]>();
  for (const row of rows) {
    if (row.captureKind !== "direct") {
      continue;
    }
    const group = directBySubmission.get(row.submissionId) ?? [];
    group.push(row);
    directBySubmission.set(row.submissionId, group);
  }

  const reports: SurveyOutlierReport[] = [];
  for (const [submissionId, directRows] of directBySubmission) {
    const manual = KNOWN_SURVEY_OUTLIER_SUBMISSION_IDS.has(submissionId);
    const repeatedAngularSignature = isRepeatedAngularSignature(directRows);

    if (!manual && !repeatedAngularSignature) {
      continue;
    }

    reports.push(buildOutlierReport(directRows, manual ? "known_manual_outlier" : "repeated_high_rotation_angular_signature"));
  }

  return reports;
}

function isRepeatedAngularSignature(rows: SurveyOutlierInput[]): boolean {
  if (rows.length < 3) {
    return false;
  }
  const targets = new Set(rows.map((row) => row.targetWord).filter(Boolean));
  if (targets.size < 3) {
    return false;
  }

  const lowComplexity = rows.every((row) => row.strokeCount <= 1 && row.pointCount >= 4 && row.pointCount <= 6);
  const highRotation = average(rows.map((row) => row.rotationBias ?? 0)) >= 0.75;
  const stableSignature =
    standardDeviation(rows.map((row) => row.rotationBias ?? 0)) <= 0.025 &&
    standardDeviation(rows.map((row) => row.closure ?? 0)) <= 0.08 &&
    standardDeviation(rows.map((row) => row.smoothness ?? 0)) <= 0.05;

  return lowComplexity && highRotation && stableSignature;
}

function buildOutlierReport(rows: SurveyOutlierInput[], reason: string): SurveyOutlierReport {
  const submissionId = rows[0]?.submissionId ?? "";
  return {
    submissionId,
    inputCount: rows.length,
    reason,
    inputIds: rows.map((row) => row.inputId),
    avgStrokeCount: round(average(rows.map((row) => row.strokeCount))),
    avgPointCount: round(average(rows.map((row) => row.pointCount))),
    avgTopScore: round(average(rows.map((row) => row.topScore ?? 0))),
    avgScoreGap: round(average(rows.map((row) => row.scoreGap ?? 0))),
    avgClosure: round(average(rows.map((row) => row.closure ?? 0))),
    avgSmoothness: round(average(rows.map((row) => row.smoothness ?? 0))),
    avgStability: round(average(rows.map((row) => row.stability ?? 0))),
    avgRotationBias: round(average(rows.map((row) => row.rotationBias ?? 0)))
  };
}

function average(values: number[]): number {
  return values.reduce((sum, value) => sum + value, 0) / Math.max(values.length, 1);
}

function standardDeviation(values: number[]): number {
  const mean = average(values);
  return Math.sqrt(average(values.map((value) => (value - mean) ** 2)));
}

function round(value: number): number {
  return Math.round(value * 1_000_000) / 1_000_000;
}
