import type { QualityVector } from "../recognizer/types";
import type { SurveyPromptWord } from "./survey-contract";

export type ChemistryState = "dry" | "wet" | "burning" | "chilled" | "charged";

export interface ChemistryContractResult {
  before: string[];
  after: string[];
  events: string[];
}

export interface SurveyPhysicsState {
  spell: SurveyPromptWord;
  projectile: {
    x: number;
    y: number;
    vx: number;
    vy: number;
    radius: number;
  };
  dummy: {
    x: number;
    y: number;
    hp: number;
    lastDamage: number;
  };
  tiles: Array<"floor" | "water" | "brush" | "stone">;
  width: number;
  height: number;
  elapsedMs: number;
  resolved: boolean;
}

const CHEMISTRY_CHARS: Record<ChemistryState, string> = {
  dry: ".",
  wet: "~",
  burning: "f",
  chilled: "c",
  charged: "e"
};

const CHAR_STATES: Record<string, ChemistryState> = {
  ".": "dry",
  "~": "wet",
  "f": "burning",
  "c": "chilled",
  "e": "charged"
};

const CHEMISTRY_TRANSITIONS: Record<SurveyPromptWord, Record<ChemistryState, ChemistryState>> = {
  fire: {
    dry: "burning",
    wet: "dry",
    burning: "burning",
    chilled: "dry",
    charged: "burning"
  },
  water: {
    dry: "wet",
    wet: "chilled",
    burning: "wet",
    chilled: "chilled",
    charged: "wet"
  },
  wind: {
    dry: "charged",
    wet: "dry",
    burning: "charged",
    chilled: "dry",
    charged: "charged"
  }
};

export function applyChemistryContract(rows: string[], spell: SurveyPromptWord): ChemistryContractResult {
  const events: string[] = [];
  const after = rows.map((row, rowIndex) =>
    Array.from(row)
      .map((cell, columnIndex) => {
        const state = CHAR_STATES[cell];

        if (!state) {
          return cell;
        }

        const next = CHEMISTRY_TRANSITIONS[spell][state];

        if (next !== state) {
          events.push(`${rowIndex},${columnIndex}:${state}->${next}`);
        }

        return CHEMISTRY_CHARS[next];
      })
      .join("")
  );

  return {
    before: [...rows],
    after,
    events
  };
}

export function createSurveyPhysicsState(spell: SurveyPromptWord, quality: QualityVector): SurveyPhysicsState {
  const qualityAverage = averageQuality(quality);
  const speedBonus = spell === "wind" ? 0.16 : spell === "fire" ? 0.08 : -0.02;
  const stability = clamp(quality.stability * 0.2 + quality.smoothness * 0.12, 0, 0.28);

  return {
    spell,
    projectile: {
      x: 0.8,
      y: 2,
      vx: 0.34 + speedBonus + qualityAverage * 0.18,
      vy: spell === "water" ? 0.015 : spell === "fire" ? -0.012 : 0,
      radius: 0.22
    },
    dummy: {
      x: 8.2,
      y: 2,
      hp: 100,
      lastDamage: 0
    },
    tiles: createTileStrip(),
    width: 10,
    height: 4,
    elapsedMs: 0,
    resolved: false
  };
}

export function tickSurveyPhysics(state: SurveyPhysicsState, dtMs: number): SurveyPhysicsState {
  if (state.resolved) {
    return structuredClone(state);
  }

  const next = structuredClone(state);
  const tile = tileAt(next, next.projectile.x, next.projectile.y);
  const friction = tile === "water" ? 0.82 : tile === "brush" ? 0.9 : tile === "stone" ? 0.68 : 0.96;
  const spellAcceleration = next.spell === "wind" ? 0.012 : next.spell === "fire" ? 0.006 : -0.002;
  const dtScale = dtMs / 16.67;

  next.projectile.vx = (next.projectile.vx + spellAcceleration * dtScale) * friction;
  next.projectile.vy = (next.projectile.vy + gravityFor(next.spell) * dtScale) * friction;
  next.projectile.x += next.projectile.vx * dtScale;
  next.projectile.y = clamp(next.projectile.y + next.projectile.vy * dtScale, 0.6, next.height - 0.6);
  next.elapsedMs += dtMs;

  const hitDistance = Math.hypot(next.projectile.x - next.dummy.x, next.projectile.y - next.dummy.y);

  if (hitDistance <= 0.48) {
    const damage = damageFor(next.spell, Math.abs(next.projectile.vx));
    next.dummy.lastDamage = damage;
    next.dummy.hp = clamp(next.dummy.hp - damage, 0, 100);
    next.projectile.vx = -Math.abs(next.projectile.vx) * 0.25;
    next.resolved = true;
  }

  if (next.projectile.x >= next.width - 0.4 || next.elapsedMs > 2600) {
    next.resolved = true;
  }

  return next;
}

export function simulateDummyHit(spell: SurveyPromptWord, quality: QualityVector): SurveyPhysicsState {
  let state = createSurveyPhysicsState(spell, quality);

  for (let step = 0; step < 180 && !state.resolved; step += 1) {
    state = tickSurveyPhysics(state, 16.67);
  }

  return state;
}

export function renderPhysicsSummary(state: SurveyPhysicsState): string {
  const hpDelta = Math.round(100 - state.dummy.hp);
  const time = Math.round(state.elapsedMs);

  if (hpDelta > 0) {
    return `허수아비에 ${hpDelta} 피해, 도달 시간 ${time}ms`;
  }

  return `허수아비 미도달, 진행 시간 ${time}ms`;
}

function createTileStrip(): SurveyPhysicsState["tiles"] {
  return ["floor", "floor", "brush", "floor", "water", "water", "floor", "brush", "floor", "stone"];
}

function tileAt(state: SurveyPhysicsState, x: number, _y: number): SurveyPhysicsState["tiles"][number] {
  const tileIndex = clamp(Math.floor(x), 0, state.tiles.length - 1);
  return state.tiles[tileIndex];
}

function gravityFor(spell: SurveyPromptWord): number {
  switch (spell) {
    case "fire":
      return -0.0008;
    case "water":
      return 0.0012;
    case "wind":
      return -0.0002;
  }
}

function damageFor(spell: SurveyPromptWord, speed: number): number {
  const base = spell === "fire" ? 24 : spell === "water" ? 15 : 18;
  return Math.round(base + speed * 28);
}

function averageQuality(quality: QualityVector): number {
  const values = [
    quality.closure,
    quality.symmetry,
    quality.smoothness,
    quality.tempo,
    quality.overshoot,
    quality.stability,
    quality.rotationBias
  ];

  return values.reduce((sum, value) => sum + value, 0) / values.length;
}

function clamp(value: number, min: number, max: number): number {
  return Math.max(min, Math.min(max, value));
}
