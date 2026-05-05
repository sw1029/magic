import { mkdtemp, readFile } from "node:fs/promises";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { afterEach, describe, expect, it } from "vitest";

import {
  SURVEY_SCHEMA_VERSION,
  createSurveyApiServer,
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
    };
    const payload = makePayload({
      sessionId: session.sessionId,
      experimentGroup: session.experimentGroup
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
});

function listen(server: { listen(port: number, host: string, callback: () => void): void }): Promise<void> {
  return new Promise((resolve) => {
    server.listen(0, "127.0.0.1", resolve);
  });
}
