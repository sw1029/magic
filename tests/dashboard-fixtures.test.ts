import { describe, expect, it } from "vitest";

import { parseDashboardFixture } from "../src/demo/dashboard-fixtures";

describe("dashboard fixtures", () => {
  it("does not throw for invalid JSON", () => {
    const result = parseDashboardFixture("{ bad json");

    expect(result.ok).toBe(false);
    expect(result.userMessage).toContain("JSON");
  });

  it("detects stroke session fixtures", () => {
    const result = parseDashboardFixture(JSON.stringify({
      strokes: [{ id: "a", points: [{ x: 1, y: 2, t: 0 }, { x: 2, y: 3, t: 1 }] }],
      startedAt: 1
    }));

    expect(result.ok).toBe(true);
    expect(result.kind).toBe("stroke_session");
  });

  it("detects datacard patch fixtures", () => {
    const result = parseDashboardFixture(JSON.stringify({ cards: [] }));

    expect(result.kind).toBe("datacard_patch");
  });
});
