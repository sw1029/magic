import { describe, expect, it } from "vitest";

import {
  advanceAsciiTutorial,
  createAsciiTutorialState,
  renderAsciiRows,
  summarizeAsciiState
} from "../survey/magic-symbol-tutorial/ascii-turn-engine";

describe("survey ASCII turn tutorial engine", () => {
  it("creates a deterministic 50x50 tutorial map", () => {
    const state = createAsciiTutorialState();

    expect(state.width).toBe(50);
    expect(state.height).toBe(50);
    expect(state.rows).toHaveLength(50);
    expect(state.rows.every((row) => row.length === 50)).toBe(true);
    expect(renderAsciiRows(state).join("")).toContain(">");
  });

  it("moves the player and keeps the facing direction visible in the map", () => {
    const initial = createAsciiTutorialState();
    const moved = advanceAsciiTutorial(initial, { type: "move", direction: "east" });
    const blocked = advanceAsciiTutorial(moved, { type: "move", direction: "west" });

    expect(moved.player.column).toBe(initial.player.column + 1);
    expect(renderAsciiRows(moved)[moved.player.row][moved.player.column]).toBe(">");
    expect(blocked.player.facing).toBe("west");
    expect(renderAsciiRows(blocked)[blocked.player.row][blocked.player.column]).toBe("<");
  });

  it("casts fire in the facing direction and spreads burning state on later turns", () => {
    const initial = createAsciiTutorialState();
    const afterFire = advanceAsciiTutorial(initial, { type: "cast", spell: "fire" });
    const afterWait = advanceAsciiTutorial(afterFire, { type: "wait" });

    const fireCount = count(afterFire.rows, "f");
    expect(fireCount).toBeGreaterThan(0);
    expect(count(afterWait.rows, "f")).toBeGreaterThanOrEqual(fireCount);
    expect(afterFire.log.some((entry) => entry.includes("나무"))).toBe(true);
  });

  it("applies water, electric, ice, and void contracts through facing interactions", () => {
    const afterFire = advanceAsciiTutorial(createAsciiTutorialState(), { type: "cast", spell: "fire" });
    const afterWater = advanceAsciiTutorial(afterFire, { type: "cast", spell: "water" });
    const afterElectric = advanceAsciiTutorial(afterWater, { type: "cast", spell: "electric" });
    const afterIce = advanceAsciiTutorial(afterElectric, { type: "cast", spell: "ice" });
    const afterVoid = advanceAsciiTutorial(afterIce, { type: "cast", spell: "void" });

    expect(afterWater.log.some((entry) => entry.includes("물+불"))).toBe(true);
    expect(afterElectric.rows.join("")).toContain("e");
    expect(afterIce.rows.join("")).toContain("i");
    expect(afterVoid.log.some((entry) => entry.includes("절단"))).toBe(true);
    expect(summarizeAsciiState(afterVoid)).toContain("turn");
  });
});

function count(rows: string[], char: string): number {
  return rows.join("").split("").filter((item) => item === char).length;
}
