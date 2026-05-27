import { describe, expect, it } from "vitest";

import {
  KNOWN_SURVEY_OUTLIER_SUBMISSION_IDS,
  detectSurveyOutlierRespondents
} from "../src/survey/survey-outlier";
import type { SurveyOutlierInput } from "../src/survey/survey-outlier";

describe("survey calibration outlier detection", () => {
  it("records the known repeated angular respondent for appendix exclusion", () => {
    const submissionId = "6edcac06-88dd-4701-be2c-11e37a2be62c";
    const reports = detectSurveyOutlierRespondents([
      row(submissionId, "fire", 0),
      row(submissionId, "wind", 1),
      row(submissionId, "water", 2),
      row("regular", "fire", 0, { rotationBias: 0.04, pointCount: 32 })
    ]);

    expect(KNOWN_SURVEY_OUTLIER_SUBMISSION_IDS.has(submissionId)).toBe(true);
    expect(reports).toHaveLength(1);
    expect(reports[0].submissionId).toBe(submissionId);
    expect(reports[0].reason).toBe("known_manual_outlier");
  });

  it("auto-flags repeated high-rotation low-complexity direct signatures", () => {
    const reports = detectSurveyOutlierRespondents([
      row("auto", "fire", 0),
      row("auto", "wind", 1, { rotationBias: 0.91 }),
      row("auto", "water", 2, { rotationBias: 0.905 })
    ]);

    expect(reports).toHaveLength(1);
    expect(reports[0].reason).toBe("repeated_high_rotation_angular_signature");
  });
});

function row(
  submissionId: string,
  targetWord: string,
  index: number,
  overrides: Partial<SurveyOutlierInput> = {}
): SurveyOutlierInput {
  return {
    submissionId,
    inputId: `${submissionId}-${index}`,
    captureKind: "direct",
    targetWord,
    strokeCount: 1,
    pointCount: 4,
    topScore: 0.84,
    scoreGap: 0.08,
    closure: 0,
    smoothness: 0.32,
    stability: 0.92,
    rotationBias: 0.9,
    ...overrides
  };
}
