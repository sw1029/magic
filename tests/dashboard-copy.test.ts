import { describe, expect, it } from "vitest";

import { dashboardFamilyName, dashboardStatusLabel, ensureDashboardUserCopy } from "../src/demo/dashboard-copy";

describe("dashboard copy", () => {
  it("uses non-expert labels", () => {
    expect(dashboardFamilyName("fire")).toBe("불꽃 모양");
    expect(dashboardStatusLabel("ambiguous")).toBe("헷갈림");
  });

  it("replaces internal wording in user-facing copy", () => {
    const copy = ensureDashboardUserCopy("threshold gate rerank shadow fixture synthetic confusion matrix histogram scatter plot");
    expect(copy).not.toMatch(/threshold|gate|rerank|shadow|fixture|synthetic/i);
    expect(copy).toContain("인정 기준");
    expect(copy).toContain("헷갈림 지도");
  });
});
