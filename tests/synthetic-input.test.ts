import { describe, expect, it } from "vitest";

import { buildSyntheticStrokeSession } from "../src/demo/synthetic-input";

function firstPoint(session: ReturnType<typeof buildSyntheticStrokeSession>) {
  return session.strokes[0].points[0];
}

function endpointGap(session: ReturnType<typeof buildSyntheticStrokeSession>): number {
  const points = session.strokes[0].points;
  const first = points[0];
  const last = points[points.length - 1];
  return Math.hypot(first.x - last.x, first.y - last.y);
}

describe("synthetic input", () => {
  it("is deterministic for the same seed", () => {
    const left = buildSyntheticStrokeSession({ family: "fire", seed: 7, jitterPx: 3, rotationDeg: 12 });
    const right = buildSyntheticStrokeSession({ family: "fire", seed: 7, jitterPx: 3, rotationDeg: 12 });

    expect(left).toEqual(right);
  });

  it("jitter changes generated coordinates", () => {
    const calm = buildSyntheticStrokeSession({ family: "fire", seed: 8, jitterPx: 0 });
    const shaky = buildSyntheticStrokeSession({ family: "fire", seed: 8, jitterPx: 10 });

    expect(firstPoint(calm).x).not.toBeCloseTo(firstPoint(shaky).x, 3);
  });

  it("open gap increases endpoint distance for closed shapes", () => {
    const closed = buildSyntheticStrokeSession({ family: "fire", seed: 9, openGapRatio: 0 });
    const open = buildSyntheticStrokeSession({ family: "fire", seed: 9, openGapRatio: 0.5 });

    expect(endpointGap(open)).toBeGreaterThan(endpointGap(closed));
  });
});
