import { describe, expect, it } from "vitest";

import {
  SURVEY_GUESS_WORDS,
  SURVEY_SCHEMA_VERSION,
  assignExperimentGroup,
  validateSurveyRaffleContactPayload,
  validateSurveyResponsePayload
} from "../src/survey/survey-contract";
import type { SurveyResponsePayload } from "../src/survey/survey-contract";

const BLANK_50 = Array.from({ length: 50 }, () => ".".repeat(50));

describe("survey response contract", () => {
  it("assigns experiment groups deterministically from a session seed", () => {
    expect(assignExperimentGroup("session-a")).toBe(assignExperimentGroup("session-a"));
    expect(["shape_only", "scent_effects", "tutorial_quality"]).toContain(assignExperimentGroup("session-b"));
  });

  it("accepts the complete survey payload shape", () => {
    expect(validateSurveyResponsePayload(makePayload())).toEqual([]);
  });

  it("allows expanded word guess candidates without changing prompt words", () => {
    expect(SURVEY_GUESS_WORDS.length).toBeGreaterThanOrEqual(6);
    expect(validateSurveyResponsePayload(makePayload())).toEqual([]);
    expect(
      validateSurveyResponsePayload({
        ...makePayload(),
        wordGuessTrials: [
          {
            ...makePayload().wordGuessTrials[0],
            answer: "stone"
          },
          ...makePayload().wordGuessTrials.slice(1)
        ]
      })
    ).toEqual([]);
    expect(
      validateSurveyResponsePayload({
        ...makePayload(),
        wordGuessTrials: [
          {
            ...makePayload().wordGuessTrials[0],
            answer: "cloud"
          },
          ...makePayload().wordGuessTrials.slice(1)
        ]
      })
    ).toContain("wordGuessTrials[0].answer is invalid");
  });

  it("keeps drawing responses free of raw strokes and recognition details", () => {
    const payload = makePayload();

    expect(payload).not.toHaveProperty("phone");
    expect(payload).not.toHaveProperty("email");
    expect(payload).not.toHaveProperty("raffleContact");
    expect(payload.directDrawings[0]).not.toHaveProperty("strokes");
    expect(payload.directDrawings[0]).not.toHaveProperty("strokeLimit");
    expect(payload.directDrawings[0]).not.toHaveProperty("strokeCount");
    expect(payload.directDrawings[0]).not.toHaveProperty("recognizedFamily");
    expect(payload.directDrawings[0]).not.toHaveProperty("recognitionStatus");
    expect(payload.directDrawings[0]).not.toHaveProperty("quality");
    expect(payload.directDrawings[0]).not.toHaveProperty("inputNote");
    expect(payload.tutorialCaptures[0]).not.toHaveProperty("strokes");
    expect(payload.tutorialCaptures[0]).not.toHaveProperty("quality");
    expect(payload.tutorialCaptures[0]).not.toHaveProperty("inputNote");
    expect(payload.wordGuessTrials[0]).not.toHaveProperty("trialId");
    expect(payload.wordGuessTrials[0]).not.toHaveProperty("correct");
    expect(payload.wordGuessTrials[0]).not.toHaveProperty("confidence");
  });

  it("requires relative timestamps in drawing trace points", () => {
    const payload = makePayload();

    expect(payload.directDrawings[0].shapeTrace[0][0]).toEqual([100, 120, 0]);
    expect(
      validateSurveyResponsePayload({
        ...payload,
        directDrawings: [
          {
            ...payload.directDrawings[0],
            shapeTrace: [[[100, 120]]]
          },
          ...payload.directDrawings.slice(1)
        ]
      })
    ).toContain(
      "directDrawings[0].shapeTrace[0][0] must be [x,y,tMs] integers with x/y 0-1000 and tMs 0-600000"
    );
  });

  it("rejects contact details in the survey response payload", () => {
    const errors = validateSurveyResponsePayload({
      ...makePayload(),
      email: "survey@example.com"
    });

    expect(errors).toContain("email must not be submitted with survey response");
  });

  it("rejects raw drawing details when a client tries to submit them", () => {
    const payload = makePayload({
      directDrawings: [
        {
          ...makePayload().directDrawings[0],
          strokes: [{ id: "raw", points: [{ x: 0, y: 0, t: 0 }] }]
        } as unknown as SurveyResponsePayload["directDrawings"][number],
        ...makePayload().directDrawings.slice(1)
      ]
    });

    const errors = validateSurveyResponsePayload(payload);

    expect(errors).toContain("directDrawings[0].strokes must not be submitted");
  });

  it("rejects missing consent and malformed arrays", () => {
    const payload = {
      ...makePayload(),
      consentAccepted: false,
      wordGuessTrials: []
    };

    const errors = validateSurveyResponsePayload(payload);

    expect(errors).toContain("consentAccepted must be true");
    expect(errors.some((error) => error.includes("wordGuessTrials"))).toBe(true);
  });

  it("requires 50x50 ASCII snapshots for the turn tutorial", () => {
    const payload = makePayload({
      engineComparison: {
        ...makePayload().engineComparison,
        asciiAfter: ["too-small"]
      }
    });

    const errors = validateSurveyResponsePayload(payload);

    expect(errors).toContain("engineComparison.asciiAfter must contain 50 rows");
  });

  it("validates optional raffle contact as a separate payload", () => {
    expect(
      validateSurveyRaffleContactPayload({
        schemaVersion: SURVEY_SCHEMA_VERSION,
        submissionId: "submission_123456",
        sessionId: "session_1234567890abcdef",
        phone: "010-1234-5678",
        email: "survey@example.com"
      })
    ).toEqual([]);
    expect(
      validateSurveyRaffleContactPayload({
        schemaVersion: SURVEY_SCHEMA_VERSION,
        submissionId: "submission_123456",
        sessionId: "session_1234567890abcdef"
      })
    ).toContain("phone or email is required for raffle contact");
    expect(
      validateSurveyRaffleContactPayload({
        schemaVersion: SURVEY_SCHEMA_VERSION,
        submissionId: "submission_123456",
        sessionId: "session_1234567890abcdef",
        email: "not-an-email"
      })
    ).toContain("email must be a valid email address");
  });
});

export function makePayload(overrides: Partial<SurveyResponsePayload> = {}): SurveyResponsePayload {
  return {
    schemaVersion: SURVEY_SCHEMA_VERSION,
    submissionId: "submission_123456",
    sessionId: "session_1234567890abcdef",
    experimentGroup: "shape_only",
    consentAccepted: true,
    directDrawings: ["fire", "water", "wind"].map((targetWord) => ({
      targetWord: targetWord as "fire" | "water" | "wind",
      shapeTrace: [
        [
          [100, 120, 0],
          [360, 520, 320],
          [620, 120, 780]
        ]
      ],
      elapsedMs: 1200
    })),
    wordGuessTrials: ["fire", "water", "wind"].map((targetWord) => ({
      targetWord: targetWord as "fire" | "water" | "wind",
      answer: targetWord as "fire" | "water" | "wind",
      reactionMs: 900,
      hintsEnabled: false,
      effectPlayed: false
    })),
    tutorialCaptures: ["ideal", "fast", "comfortable"].map((mode) => ({
      targetWord: "fire",
      mode: mode as "ideal" | "fast" | "comfortable",
      shapeTrace: [
        [
          [100, 120, 0],
          [360, 520, 260],
          [620, 120, 650]
        ]
      ],
      elapsedMs: 1000
    })),
    engineComparison: {
      turnTutorialRating: 3,
      contractClarityRating: 4,
      preferredMode: "turn_tutorial",
      interactionSummary: "turn 2: burning 4, charge 0, water 42, ice 0",
      asciiBefore: [...BLANK_50],
      asciiAfter: [...BLANK_50]
    },
    selfReport: {
      tutorialInstructionClarity: 4,
      tutorialLearningEfficiency: 4,
      scentHelpfulness: 3,
      overallClarity: 4,
      strengths: "clear",
      weaknesses: ""
    },
    ...overrides
  };
}
