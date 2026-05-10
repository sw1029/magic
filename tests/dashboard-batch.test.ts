import { describe, expect, it } from "vitest";

import { runDashboardBatch } from "../src/demo/dashboard-batch";

describe("dashboard batch", () => {
  it("summarizes counts for generated recognitions", () => {
    const summary = runDashboardBatch({ recipe: { family: "fire", seed: 10, jitterPx: 2 }, iterations: 12 });
    const statusTotal = Object.values(summary.statusCounts).reduce((sum, value) => sum + value, 0);
    const familyTotal = Object.values(summary.familyCounts).reduce((sum, value) => sum + value, 0);

    expect(summary.total).toBe(12);
    expect(statusTotal).toBe(12);
    expect(familyTotal).toBe(12);
    expect(summary.confusionRows.length).toBeGreaterThan(0);
    expect(summary.userSummary).not.toMatch(/threshold|gate|rerank/i);
  });
});
