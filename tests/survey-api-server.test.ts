import { mkdtemp, readFile, writeFile } from "node:fs/promises";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { afterEach, describe, expect, it } from "vitest";

import {
  SURVEY_SCHEMA_VERSION,
  TINYML_NOISY_EVAL_MAX_BODY_BYTES,
  TINYML_NOISY_EVAL_SCHEMA_VERSION,
  TUTORIAL_THRESHOLD_EVAL_SCHEMA_VERSION,
  assignBalancedExperimentGroup,
  assignBalancedHciProbeVariant,
  countExperimentCellsFromResponseLog,
  countExperimentGroupsFromResponseLog,
  createSurveyApiServer,
  validateTinyMlNoisyEvalPayload,
  validateTutorialThresholdEvalPayload,
  validateSurveyRaffleContactPayload,
  validateSurveyResponsePayload
} from "../scripts/survey-api-server.mjs";
import { makePayload } from "./survey-contract.test";

const openServers: Array<{ close(callback?: () => void): void }> = [];

afterEach(async () => {
  await Promise.all(
    openServers.splice(0).map(
      (server) =>
        new Promise<void>((resolve) => {
          server.close(() => resolve());
        })
    )
  );
});

describe("survey API server", () => {
  it("validates payloads before persistence", () => {
    expect(validateSurveyResponsePayload(makePayload())).toEqual([]);
    expect(
      assignBalancedExperimentGroup(
        {
          shape_only: 4,
          scent_effects: 1,
          tutorial_quality: 3
        },
        "session-a"
      )
    ).toBe("scent_effects");
    expect(assignBalancedHciProbeVariant({ log_discovery: 3, goal_scaffold: 1 }, "session-a")).toBe(
      "goal_scaffold"
    );
    expect(
      validateSurveyRaffleContactPayload({
        schemaVersion: SURVEY_SCHEMA_VERSION,
        submissionId: "submission_123456",
        sessionId: "session_1234567890abcdef",
        email: "survey@example.com"
      })
    ).toEqual([]);
    expect(validateSurveyResponsePayload({ ...makePayload(), sessionId: "" })).toContain(
      "sessionId must be a string with length 16-128"
    );
    expect(validateTinyMlNoisyEvalPayload(makeTinyMlPayload())).toEqual([]);
    expect(validateTinyMlNoisyEvalPayload({ ...makeTinyMlPayload(), schemaVersion: "old" })).toContain(
      `schemaVersion must be ${TINYML_NOISY_EVAL_SCHEMA_VERSION}`
    );
    expect(validateTinyMlNoisyEvalPayload({ ...makeTinyMlPayload(), trials: [] })).toContain(
      "trials must contain 1-500 items"
    );
    expect(validateTutorialThresholdEvalPayload(makeTutorialThresholdPayload())).toEqual([]);
    expect(validateTutorialThresholdEvalPayload({ ...makeTutorialThresholdPayload(), schemaVersion: "old" })).toContain(
      `schemaVersion must be ${TUTORIAL_THRESHOLD_EVAL_SCHEMA_VERSION}`
    );
    expect(validateTutorialThresholdEvalPayload({ ...makeTutorialThresholdPayload(), captures: [], evals: [] })).toContain(
      "captures or evals must contain at least 1 item"
    );
    expect(
      validateTutorialThresholdEvalPayload({
        ...makeTutorialThresholdPayload(),
        captures: [
          {
            ...(makeTutorialThresholdPayload().captures as Array<Record<string, unknown>>)[0],
            tinyMlCorrection: {
              ...tutorialTinyMlCorrection(),
              finalDecision: "maybe"
            }
          }
        ]
      })
    ).toContain("captures[0].tinyMlCorrection.finalDecision is invalid");
    expect(
      validateSurveyResponsePayload({
        ...makePayload(),
        wordGuessTrials: [
          {
            ...makePayload().wordGuessTrials[0],
            correct: true
          },
          ...makePayload().wordGuessTrials.slice(1)
        ]
      })
    ).toContain("wordGuessTrials[0].correct must not be submitted");
  });

  it("counts persisted response logs for balanced experiment assignment", async () => {
    const dataDir = await mkdtemp(join(tmpdir(), "magic-survey-"));
    const responsePath = join(dataDir, "survey-responses.ndjson");
    const persistedRecords = [
      makeStoredPayload("shape_only", "log_discovery"),
      makeStoredPayload("shape_only", "log_discovery"),
      makeStoredPayload("shape_only", "goal_scaffold"),
      makeStoredPayload("scent_effects", "log_discovery"),
      makeStoredPayload("tutorial_quality", "goal_scaffold")
    ];
    await writeFile(responsePath, `${persistedRecords.map((record) => JSON.stringify(record)).join("\n")}\n`, "utf8");
    expect(countExperimentGroupsFromResponseLog(await readFile(responsePath, "utf8"))).toEqual({
      shape_only: 3,
      scent_effects: 1,
      tutorial_quality: 1
    });
    expect(countExperimentCellsFromResponseLog(await readFile(responsePath, "utf8")).shape_only).toEqual({
      log_discovery: 2,
      goal_scaffold: 1
    });

    const api = createSurveyApiServer({
      dataDir,
      allowedOrigins: ["http://localhost:5173"],
      now: () => Date.parse("2026-05-05T00:00:00.000Z")
    });
    openServers.push(api.server);
    await listen(api.server);
    const address = api.server.address();
    const port = typeof address === "object" && address ? address.port : 0;
    const baseUrl = `http://127.0.0.1:${port}`;
    const sessions = await Promise.all(
      [0, 1].map(async () => {
        const response = await fetch(`${baseUrl}/api/survey-session`, {
          headers: { Origin: "http://localhost:5173" }
        });
        return (await response.json()) as {
          experimentGroup: "shape_only" | "scent_effects" | "tutorial_quality";
          hciProbeVariant: "log_discovery" | "goal_scaffold";
        };
      })
    );

    expect(new Set(sessions.map((session) => session.experimentGroup))).toEqual(
      new Set(["scent_effects", "tutorial_quality"])
    );
    expect(sessions.every((session) => ["log_discovery", "goal_scaffold"].includes(session.hciProbeVariant))).toBe(true);
    expect(api.experimentGroupCounts.completed.get("shape_only")).toBe(3);
    expect(api.experimentGroupCounts.active.get("shape_only")).toBe(0);
  });

  it("enforces CSRF and duplicate submission checks", async () => {
    const dataDir = await mkdtemp(join(tmpdir(), "magic-survey-"));
    const api = createSurveyApiServer({
      dataDir,
      allowedOrigins: ["http://localhost:5173"],
      now: () => Date.parse("2026-05-05T00:00:00.000Z")
    });
    openServers.push(api.server);
    await listen(api.server);
    const address = api.server.address();
    const port = typeof address === "object" && address ? address.port : 0;
    const baseUrl = `http://127.0.0.1:${port}`;
    const sessionResponse = await fetch(`${baseUrl}/api/survey-session`, {
      headers: { Origin: "http://localhost:5173" }
    });
    const cookie = sessionResponse.headers.get("set-cookie")?.split(";")[0] ?? "";
    const session = (await sessionResponse.json()) as {
      sessionId: string;
      csrfToken: string;
      experimentGroup: "shape_only" | "scent_effects" | "tutorial_quality";
      hciProbeVariant: "log_discovery" | "goal_scaffold";
    };
    const payload = makePayload({
      sessionId: session.sessionId,
      experimentGroup: session.experimentGroup,
      hciProbeVariant: session.hciProbeVariant
    });

    const csrfFailure = await fetch(`${baseUrl}/api/survey-responses`, {
      method: "POST",
      headers: {
        Origin: "http://localhost:5173",
        Cookie: cookie,
        "Content-Type": "application/json"
      },
      body: JSON.stringify(payload)
    });
    expect(csrfFailure.status).toBe(403);

    const success = await fetch(`${baseUrl}/api/survey-responses`, {
      method: "POST",
      headers: {
        Origin: "http://localhost:5173",
        Cookie: cookie,
        "Content-Type": "application/json",
        "X-CSRF-Token": session.csrfToken
      },
      body: JSON.stringify(payload)
    });
    expect(success.status).toBe(201);

    const duplicate = await fetch(`${baseUrl}/api/survey-responses`, {
      method: "POST",
      headers: {
        Origin: "http://localhost:5173",
        Cookie: cookie,
        "Content-Type": "application/json",
        "X-CSRF-Token": session.csrfToken
      },
      body: JSON.stringify(payload)
    });
    expect(duplicate.status).toBe(409);

    const raffleContact = {
      schemaVersion: SURVEY_SCHEMA_VERSION,
      submissionId: payload.submissionId,
      sessionId: payload.sessionId,
      phone: "010-1234-5678",
      email: "survey@example.com"
    };
    const raffleSuccess = await fetch(`${baseUrl}/api/survey-raffle-contact`, {
      method: "POST",
      headers: {
        Origin: "http://localhost:5173",
        Cookie: cookie,
        "Content-Type": "application/json",
        "X-CSRF-Token": session.csrfToken
      },
      body: JSON.stringify(raffleContact)
    });
    expect(raffleSuccess.status).toBe(201);

    const duplicateRaffle = await fetch(`${baseUrl}/api/survey-raffle-contact`, {
      method: "POST",
      headers: {
        Origin: "http://localhost:5173",
        Cookie: cookie,
        "Content-Type": "application/json",
        "X-CSRF-Token": session.csrfToken
      },
      body: JSON.stringify(raffleContact)
    });
    expect(duplicateRaffle.status).toBe(409);

    const stored = await readFile(api.responsePath, "utf8");
    expect(stored).toContain(payload.submissionId);
    expect(stored).not.toContain(raffleContact.phone);
    expect(stored).not.toContain(raffleContact.email);

    const raffleStored = await readFile(api.raffleContactPath, "utf8");
    expect(raffleStored).toContain(raffleContact.phone);
    expect(raffleStored).toContain(raffleContact.email);
  });

  it("stores tinyML noisy eval submissions with CSRF and duplicate checks", async () => {
    const dataDir = await mkdtemp(join(tmpdir(), "magic-survey-"));
    const api = createSurveyApiServer({
      dataDir,
      allowedOrigins: ["http://localhost:5173"],
      now: () => Date.parse("2026-05-05T00:00:00.000Z")
    });
    openServers.push(api.server);
    await listen(api.server);
    const address = api.server.address();
    const port = typeof address === "object" && address ? address.port : 0;
    const baseUrl = `http://127.0.0.1:${port}`;
    const sessionResponse = await fetch(`${baseUrl}/api/survey-session`, {
      headers: { Origin: "http://localhost:5173" }
    });
    const cookie = sessionResponse.headers.get("set-cookie")?.split(";")[0] ?? "";
    const session = (await sessionResponse.json()) as { sessionId: string; csrfToken: string };
    const payload = makeTinyMlPayload({ sessionId: session.sessionId });

    const csrfFailure = await fetch(`${baseUrl}/api/tinyml-noisy-eval-responses`, {
      method: "POST",
      headers: {
        Origin: "http://localhost:5173",
        Cookie: cookie,
        "Content-Type": "application/json"
      },
      body: JSON.stringify(payload)
    });
    expect(csrfFailure.status).toBe(403);

    const success = await fetch(`${baseUrl}/api/tinyml-noisy-eval-responses`, {
      method: "POST",
      headers: {
        Origin: "http://localhost:5173",
        Cookie: cookie,
        "Content-Type": "application/json",
        "X-CSRF-Token": session.csrfToken
      },
      body: JSON.stringify(payload)
    });
    expect(success.status).toBe(201);

    const duplicate = await fetch(`${baseUrl}/api/tinyml-noisy-eval-responses`, {
      method: "POST",
      headers: {
        Origin: "http://localhost:5173",
        Cookie: cookie,
        "Content-Type": "application/json",
        "X-CSRF-Token": session.csrfToken
      },
      body: JSON.stringify(payload)
    });
    expect(duplicate.status).toBe(409);

    const invalid = await fetch(`${baseUrl}/api/tinyml-noisy-eval-responses`, {
      method: "POST",
      headers: {
        Origin: "http://localhost:5173",
        Cookie: cookie,
        "Content-Type": "application/json",
        "X-CSRF-Token": session.csrfToken
      },
      body: JSON.stringify({ ...makeTinyMlPayload({ sessionId: session.sessionId, submissionId: "tinymlsub_654321" }), trials: [] })
    });
    expect(invalid.status).toBe(400);

    const stored = await readFile(api.tinyMlNoisyEvalResponsePath, "utf8");
    expect(stored).toContain(payload.submissionId);
    expect(stored).toContain("rawStrokes");
    expect(stored).toContain("noisyStrokes");
  });

  it("stores tutorial threshold eval submissions with CSRF and duplicate checks", async () => {
    const dataDir = await mkdtemp(join(tmpdir(), "magic-survey-"));
    const api = createSurveyApiServer({
      dataDir,
      allowedOrigins: ["http://localhost:5173"],
      now: () => Date.parse("2026-05-05T00:00:00.000Z")
    });
    openServers.push(api.server);
    await listen(api.server);
    const address = api.server.address();
    const port = typeof address === "object" && address ? address.port : 0;
    const baseUrl = `http://127.0.0.1:${port}`;
    const sessionResponse = await fetch(`${baseUrl}/api/survey-session`, {
      headers: { Origin: "http://localhost:5173" }
    });
    const cookie = sessionResponse.headers.get("set-cookie")?.split(";")[0] ?? "";
    const session = (await sessionResponse.json()) as { sessionId: string; csrfToken: string };
    const payload = makeTutorialThresholdPayload({ sessionId: session.sessionId });

    const csrfFailure = await fetch(`${baseUrl}/api/tutorial-threshold-eval-responses`, {
      method: "POST",
      headers: {
        Origin: "http://localhost:5173",
        Cookie: cookie,
        "Content-Type": "application/json"
      },
      body: JSON.stringify(payload)
    });
    expect(csrfFailure.status).toBe(403);

    const success = await fetch(`${baseUrl}/api/tutorial-threshold-eval-responses`, {
      method: "POST",
      headers: {
        Origin: "http://localhost:5173",
        Cookie: cookie,
        "Content-Type": "application/json",
        "X-CSRF-Token": session.csrfToken
      },
      body: JSON.stringify(payload)
    });
    expect(success.status).toBe(201);

    const duplicate = await fetch(`${baseUrl}/api/tutorial-threshold-eval-responses`, {
      method: "POST",
      headers: {
        Origin: "http://localhost:5173",
        Cookie: cookie,
        "Content-Type": "application/json",
        "X-CSRF-Token": session.csrfToken
      },
      body: JSON.stringify(payload)
    });
    expect(duplicate.status).toBe(409);

    const invalid = await fetch(`${baseUrl}/api/tutorial-threshold-eval-responses`, {
      method: "POST",
      headers: {
        Origin: "http://localhost:5173",
        Cookie: cookie,
        "Content-Type": "application/json",
        "X-CSRF-Token": session.csrfToken
      },
      body: JSON.stringify({
        ...makeTutorialThresholdPayload({ sessionId: session.sessionId, submissionId: "tutorial_threshold_2" }),
        captures: [],
        evals: []
      })
    });
    expect(invalid.status).toBe(400);

    const stored = await readFile(api.tutorialThresholdEvalResponsePath, "utf8");
    expect(stored).toContain(payload.submissionId);
    expect(stored).toContain("thresholdState");
    expect(stored).toContain("captures");
  });

  it("rejects oversized tinyML noisy eval submissions", async () => {
    const dataDir = await mkdtemp(join(tmpdir(), "magic-survey-"));
    const api = createSurveyApiServer({
      dataDir,
      allowedOrigins: ["http://localhost:5173"],
      now: () => Date.parse("2026-05-05T00:00:00.000Z")
    });
    openServers.push(api.server);
    await listen(api.server);
    const address = api.server.address();
    const port = typeof address === "object" && address ? address.port : 0;
    const baseUrl = `http://127.0.0.1:${port}`;
    const sessionResponse = await fetch(`${baseUrl}/api/survey-session`, {
      headers: { Origin: "http://localhost:5173" }
    });
    const cookie = sessionResponse.headers.get("set-cookie")?.split(";")[0] ?? "";
    const session = (await sessionResponse.json()) as { csrfToken: string };
    await expect(fetch(`${baseUrl}/api/tinyml-noisy-eval-responses`, {
      method: "POST",
      headers: {
        Origin: "http://localhost:5173",
        Cookie: cookie,
        "Content-Type": "application/json",
        "X-CSRF-Token": session.csrfToken
      },
      body: "x".repeat(TINYML_NOISY_EVAL_MAX_BODY_BYTES + 1)
    })).rejects.toThrow(/fetch failed|terminated|closed/i);
  });
});

function makeStoredPayload(
  experimentGroup: "shape_only" | "scent_effects" | "tutorial_quality",
  hciProbeVariant: "log_discovery" | "goal_scaffold"
) {
  return {
    receivedAt: "2026-05-05T00:00:00.000Z",
    payload: makePayload({
      experimentGroup,
      hciProbeVariant
    })
  };
}

function listen(server: { listen(port: number, host: string, callback: () => void): void }): Promise<void> {
  return new Promise((resolve) => {
    server.listen(0, "127.0.0.1", resolve);
  });
}

function makeTinyMlPayload(overrides: Record<string, unknown> = {}) {
  return {
    schemaVersion: TINYML_NOISY_EVAL_SCHEMA_VERSION,
    submissionId: "tinymlsub_123456",
    sessionId: "session_1234567890abcdef",
    participantId: "pilot",
    consentAccepted: true,
    startedAtIso: "2026-05-05T00:00:00.000Z",
    completedAtIso: "2026-05-05T00:01:00.000Z",
    canvas: { width: 1000, height: 640 },
    trialPlan: [
      {
        id: "custom_eval_line3_open_gap",
        label: "line{3} / open_gap",
        targetPresetId: "custom:eval_line3",
        noiseRecipeId: "open_gap"
      }
    ],
    trials: [
      {
        trialId: "tinymltrial_123456",
        targetPresetId: "custom:eval_line3",
        targetPattern: "^(line){3}$",
        topology: "open",
        noiseRecipe: {
          id: "open_gap",
          settings: { openGapEnabled: true, openGapPx: 2 }
        },
        rawStrokes: [
          {
            id: "raw-a",
            points: [
              { x: 100, y: 100, t: 0, pressure: 0.5 },
              { x: 500, y: 100, t: 20, pressure: 0.5 }
            ]
          }
        ],
        noisyStrokes: [
          {
            id: "noisy-a",
            points: [
              { x: 101, y: 100, t: 0, pressure: 0.5 },
              { x: 502, y: 99, t: 20, pressure: 0.5 }
            ]
          }
        ],
        rawRecognition: tinyMlRecognitionSummary("custom:eval_line3"),
        noisyRecognition: tinyMlRecognitionSummary("custom:eval_line3"),
        contrast: {
          version: "datacard-contrast-v1",
          role: "rule_block",
          finalStatus: "ambiguous",
          finalCandidateId: "custom:eval_line3",
          actualCandidateId: "custom:eval_line3",
          shadow: {
            candidateId: "custom:eval_line3",
            confidence: 0.8,
            unsafeRisk: 0.2,
            flipRisk: 0.15,
            relationRisk: 0.1,
            suggestedAction: "hold",
            reasons: ["test"]
          },
          meaning: {
            candidateId: "custom:eval_line3",
            confidence: 0.7,
            correctionClass: "downgrade_risk",
            eligibleForActual: false,
            actualScoreLift: 0,
            reasons: ["test"]
          },
          blockedBy: ["repetition"],
          explanationCodes: ["blockers:repetition"]
        },
        elapsedMs: 1400,
        pointerType: "mouse",
        canvas: { width: 1000, height: 640 },
        userMarkedConfused: false,
        savedAtIso: "2026-05-05T00:00:30.000Z"
      }
    ],
    aggregate: {
      trialCount: 1,
      precisionProxy: 1,
      recallProxy: 1,
      unsafeAcceptCount: 0,
      priorityFlipCount: 0,
      avgUnsafeRisk: 0.2,
      avgFlipRisk: 0.15,
      blockerCounts: { repetition: 1 }
    },
    ...overrides
  };
}

function makeTutorialThresholdPayload(overrides: Record<string, unknown> = {}) {
  const thresholdState = tutorialThresholdState();
  return {
    schemaVersion: TUTORIAL_THRESHOLD_EVAL_SCHEMA_VERSION,
    submissionId: "tutorial_threshold_123456",
    sessionId: "session_1234567890abcdef",
    participantId: "pilot",
    consentAccepted: true,
    startedAtIso: "2026-05-05T00:00:00.000Z",
    completedAtIso: "2026-05-05T00:01:00.000Z",
    notes: "pilot threshold pass",
    canvas: { width: 1000, height: 640 },
    thresholdState,
    tinyMlTwoTrackState: {
      correctionCount: 2,
      promoteCount: 1,
      shadowBlockCount: 0,
      avgDelta: 0.04,
      lastFinalDecision: "accept"
    },
    captures: [
      {
        captureId: "capture_123456",
        targetPresetId: "custom:eval_rect",
        targetPattern: "^(rect)$",
        topology: "closed",
        rawStrokes: [
          {
            id: "raw-a",
            points: [
              { x: 100, y: 100, t: 0, pressure: 0.5 },
              { x: 500, y: 100, t: 20, pressure: 0.5 }
            ]
          }
        ],
        recognition: tinyMlRecognitionSummary("custom:eval_rect"),
        contrast: {
          version: "datacard-contrast-v1",
          role: "all_agree",
          finalStatus: "recognized",
          finalCandidateId: "custom:eval_rect",
          actualCandidateId: "custom:eval_rect",
          shadow: {
            candidateId: "custom:eval_rect",
            confidence: 0.82,
            unsafeRisk: 0.08,
            flipRisk: 0.04,
            relationRisk: 0.03,
            suggestedAction: "accept_shadow",
            reasons: ["test"]
          },
          meaning: {
            candidateId: "custom:eval_rect",
            confidence: 0.8,
            correctionClass: "none",
            eligibleForActual: true,
            actualScoreLift: 0.05,
            reasons: ["test"]
          },
          blockedBy: [],
          explanationCodes: ["agree"]
        },
        thresholdBefore: thresholdState,
        thresholdAfter: thresholdState,
        tinyMlCorrection: tutorialTinyMlCorrection(),
        confusion: tutorialConfusion("custom:eval_rect"),
        elapsedMs: 1000,
        pointerType: "mouse",
        savedAtIso: "2026-05-05T00:00:30.000Z"
      }
    ],
    evals: [
      {
        trialId: "trial_123456",
        targetPresetId: "custom:eval_rect",
        targetPattern: "^(rect)$",
        topology: "closed",
        rawStrokes: [
          {
            id: "raw-b",
            points: [
              { x: 110, y: 120, t: 0, pressure: 0.5 },
              { x: 510, y: 120, t: 20, pressure: 0.5 }
            ]
          }
        ],
        recognition: tinyMlRecognitionSummary("custom:eval_rect"),
        contrast: {
          version: "datacard-contrast-v1",
          role: "all_agree",
          finalStatus: "recognized",
          finalCandidateId: "custom:eval_rect",
          actualCandidateId: "custom:eval_rect",
          shadow: {
            candidateId: "custom:eval_rect",
            confidence: 0.82,
            unsafeRisk: 0.08,
            flipRisk: 0.04,
            relationRisk: 0.03,
            suggestedAction: "accept_shadow",
            reasons: ["test"]
          },
          meaning: {
            candidateId: "custom:eval_rect",
            confidence: 0.8,
            correctionClass: "none",
            eligibleForActual: true,
            actualScoreLift: 0.05,
            reasons: ["test"]
          },
          blockedBy: [],
          explanationCodes: ["agree"]
        },
        thresholdState,
        dynamicDecision: "accept",
        dynamicReason: "score passes personalized threshold",
        tinyMlCorrection: tutorialTinyMlCorrection(),
        confusion: tutorialConfusion("custom:eval_rect"),
        elapsedMs: 1200,
        pointerType: "mouse",
        userMarkedConfused: false,
        savedAtIso: "2026-05-05T00:00:45.000Z"
      }
    ],
    aggregate: {
      acceptRate: 1,
      top1Rate: 1,
      avgUnsafeRisk: 0.08,
      avgConfusion: 0.05,
      tinyMlPromoteRate: 1,
      tinyMlBlockRate: 0
    },
    ...overrides
  };
}

function tutorialThresholdState() {
  return {
    captureCount: 1,
    globalMaturity: 0.2,
    globalScoreLift: 0.05,
    acceptThreshold: 0.66,
    holdThreshold: 0.48,
    unsafeLimit: 0.42,
    flipLimit: 0.34,
    targetRankLimit: 2,
    topGapFloor: 0.08,
    targetAdjustments: {
      "custom:eval_rect": {
        captureCount: 1,
        evalCount: 1,
        top1Rate: 1,
        confusionScore: 0.05,
        acceptThreshold: 0.66
      }
    }
  };
}

function tutorialTinyMlCorrection() {
  return {
    version: "tinyml-two-track-v1",
    shadowTrack: {
      track: "shadow_gate",
      label: "Shadow gate",
      adjustedScore: 0.81,
      threshold: 0.68,
      margin: 0.13,
      decision: "accept",
      correction: -0.01,
      reasons: ["risk 0.050"]
    },
    meaningTrack: {
      track: "meaning_recovery",
      label: "Meaning recovery",
      adjustedScore: 0.85,
      threshold: 0.64,
      margin: 0.21,
      decision: "accept",
      correction: 0.04,
      reasons: ["capture lift 0.050"]
    },
    agreement: "agree_accept",
    delta: 0.04,
    selectedTrack: "balanced",
    finalDecision: "accept",
    finalReason: "both tracks accept",
    promotePriority: true,
    blockPriorityFlip: false
  };
}

function tutorialConfusion(candidateId: string) {
  return {
    targetRank: 1,
    topPair: `${candidateId} / custom:eval_ellipse`,
    topGap: 0.2,
    targetInTop5: true,
    confusedWith: "custom:eval_ellipse",
    confusionScore: 0.05
  };
}

function tinyMlRecognitionSummary(candidateId: string) {
  return {
    selectedCandidateId: candidateId,
    finalCandidateId: candidateId,
    finalStatus: "ambiguous",
    score: 0.7,
    shadowConfidence: 0.8,
    meaningConfidence: 0.7,
    unsafeRisk: 0.2,
    flipRisk: 0.15,
    topCandidates: [
      {
        id: candidateId,
        label: candidateId,
        score: 0.7
      }
    ]
  };
}
