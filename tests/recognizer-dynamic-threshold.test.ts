import { describe, expect, it } from "vitest";

import { recognizeSession } from "../src/recognizer/recognize";
import type { StrokeSession } from "../src/recognizer/types";

describe("recognizer dynamic policy mode", () => {
  it("uses dynamic policy by default", () => {
    const result = recognizeSession(emptySession(), { sealed: true });

    expect(result.status).toBe("invalid");
    expect(result.dynamicPolicy?.mode).toBe("dynamic");
    expect(result.dynamicPolicy?.sourceProfile).toBe("live_user_survey_like");
  });

  it("preserves legacy opt-out mode", () => {
    const result = recognizeSession(emptySession(), { sealed: true, policyMode: "legacy" });

    expect(result.status).toBe("invalid");
    expect(result.dynamicPolicy?.mode).toBe("legacy");
  });
});

function emptySession(): StrokeSession {
  return {
    strokes: [],
    startedAt: 0,
    endedAt: 0
  };
}
