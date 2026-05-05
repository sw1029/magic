export const ASCII_TUTORIAL_SIZE = 50;

export const ASCII_TUTORIAL_CONTRACTS: Array<{
  spell: AsciiSpell;
  label: string;
  contract: string;
}> = [
  { spell: "fire", label: "불", contract: "나무 점화" },
  { spell: "water", label: "물", contract: "불 끄기" },
  { spell: "wind", label: "바람", contract: "밀기/확산" },
  { spell: "earth", label: "땅", contract: "벽 만들기" },
  { spell: "life", label: "생명", contract: "나무 성장" },
  { spell: "electric", label: "전기", contract: "물/금속 전도" },
  { spell: "ice", label: "얼음", contract: "물 얼리기" },
  { spell: "void", label: "절단", contract: "상태 끊기" }
];

export type AsciiSpell = "fire" | "water" | "wind" | "earth" | "life" | "electric" | "ice" | "void";
export type Direction = "north" | "east" | "south" | "west";
export type AsciiAction =
  | { type: "cast"; spell: AsciiSpell }
  | { type: "move"; direction: Direction }
  | { type: "wait" };

export interface AsciiTurnState {
  width: number;
  height: number;
  turn: number;
  terrainRows: string[];
  statusRows: string[];
  rows: string[];
  player: {
    row: number;
    column: number;
    facing: Direction;
  };
  lastAction: string;
  log: string[];
}

type Grid = string[][];
type Position = { row: number; column: number };

const EMPTY_STATUS = ".";
const PERSISTENT_STATUSES = new Set(["f"]);
const INITIAL_PLAYER = { row: 24, column: 7, facing: "east" as Direction };

export function createAsciiTutorialState(): AsciiTurnState {
  const terrain = createTerrainGrid();
  const status = makeGrid(EMPTY_STATUS);

  return createState(terrain, status, INITIAL_PLAYER, 0, "초기 상태", [
    "방향 버튼으로 이동하고, 속성 버튼은 바라보는 방향으로 작동합니다."
  ]);
}

export function advanceAsciiTutorial(state: AsciiTurnState, action: AsciiAction): AsciiTurnState {
  const terrain = rowsToGrid(state.terrainRows);
  const status = rowsToGrid(state.statusRows);
  const player = { ...state.player };
  const logs: string[] = [];

  decayTransientStatuses(status);

  if (action.type === "move") {
    movePlayer(terrain, player, action.direction, logs);
  } else if (action.type === "cast") {
    applySpell(terrain, status, player, action.spell, logs);
  } else {
    logs.push("대기: 한 턴이 지났습니다.");
  }

  spreadFire(terrain, status, logs, action.type === "cast" && action.spell === "wind" ? 14 : 5);

  const label =
    action.type === "move"
      ? `${directionLabel(action.direction)} 이동`
      : action.type === "cast"
        ? `${spellLabel(action.spell)} 상호작용`
        : "대기";

  return createState(terrain, status, player, state.turn + 1, label, trimLogs(logs));
}

export function renderAsciiRows(state: AsciiTurnState): string[] {
  return state.rows;
}

export function summarizeAsciiState(state: AsciiTurnState): string {
  const rendered = state.rows.join("");
  const burning = countChar(rendered, "f");
  const charged = countChar(rendered, "e");
  const wet = countChar(rendered, "w");
  const ice = countChar(rendered, "=") + countChar(rendered, "i");

  return `turn ${state.turn}: player ${state.player.row},${state.player.column} ${state.player.facing}, burning ${burning}, charge ${charged}, wet ${wet}, ice ${ice}`;
}

export function spellLabel(spell: AsciiSpell): string {
  return ASCII_TUTORIAL_CONTRACTS.find((item) => item.spell === spell)?.label ?? spell;
}

export function directionLabel(direction: Direction): string {
  switch (direction) {
    case "north":
      return "위";
    case "east":
      return "오른쪽";
    case "south":
      return "아래";
    case "west":
      return "왼쪽";
  }
}

function createTerrainGrid(): Grid {
  const grid = makeGrid(".");

  for (let index = 0; index < ASCII_TUTORIAL_SIZE; index += 1) {
    grid[0][index] = "#";
    grid[ASCII_TUTORIAL_SIZE - 1][index] = "#";
    grid[index][0] = "#";
    grid[index][ASCII_TUTORIAL_SIZE - 1] = "#";
  }

  for (let row = 4; row <= 45; row += 1) {
    const column = row % 6 === 0 ? 22 : 21;
    grid[row][column] = "~";
  }

  for (let row = 8; row <= 38; row += 1) {
    grid[row][35] = "M";
  }

  for (let row = 33; row <= 38; row += 1) {
    for (let column = 27; column <= 33; column += 1) {
      grid[row][column] = "=";
    }
  }

  for (const [row, column] of [
    [22, 12],
    [23, 12],
    [24, 12],
    [25, 12],
    [26, 13],
    [22, 14],
    [23, 15],
    [24, 15],
    [26, 16],
    [18, 28],
    [19, 29],
    [20, 30],
    [41, 16],
    [42, 17],
    [43, 17]
  ] as Array<[number, number]>) {
    grid[row][column] = "t";
  }

  for (let column = 9; column <= 17; column += 1) {
    grid[31][column] = "#";
  }

  return grid;
}

function movePlayer(terrain: Grid, player: AsciiTurnState["player"], direction: Direction, logs: string[]): void {
  player.facing = direction;
  const delta = directionDelta(direction);
  const next = { row: player.row + delta.row, column: player.column + delta.column };
  const cell = terrain[next.row]?.[next.column];

  if (!cell || cell === "#" || cell === "t" || cell === "M") {
    logs.push(`${directionLabel(direction)} 방향을 바라봅니다. 앞 칸은 막혀 있습니다.`);
    return;
  }

  player.row = next.row;
  player.column = next.column;
  logs.push(`${directionLabel(direction)}으로 한 칸 이동했습니다.`);
}

function applySpell(terrain: Grid, status: Grid, player: AsciiTurnState["player"], spell: AsciiSpell, logs: string[]): void {
  switch (spell) {
    case "fire":
      castFire(terrain, status, player, logs);
      return;
    case "water":
      castWater(terrain, status, player, logs);
      return;
    case "wind":
      castWind(terrain, status, player, logs);
      return;
    case "earth":
      castEarth(terrain, status, player, logs);
      return;
    case "life":
      castLife(terrain, status, player, logs);
      return;
    case "electric":
      castElectric(terrain, status, player, logs);
      return;
    case "ice":
      castIce(terrain, status, player, logs);
      return;
    case "void":
      castVoid(status, player, logs);
      return;
  }
}

function castFire(terrain: Grid, status: Grid, player: AsciiTurnState["player"], logs: string[]): void {
  for (const position of rayFromPlayer(player, 10)) {
    const terrainCell = terrain[position.row][position.column];

    if (terrainCell === "#") {
      logs.push("불: 벽에서 멈춥니다.");
      break;
    }

    if (terrainCell === "t") {
      status[position.row][position.column] = "f";
      logs.push("불: 나무가 f 상태로 바뀝니다.");
      continue;
    }

    if (terrainCell === "~") {
      status[position.row][position.column] = "s";
      logs.push("불+물: s 이펙트가 생기고 멈춥니다.");
      break;
    }

    if (terrainCell === "=") {
      terrain[position.row][position.column] = "~";
      status[position.row][position.column] = "s";
      logs.push("불+얼음: 얼음이 물로 바뀝니다.");
      continue;
    }

    status[position.row][position.column] = "*";
  }
}

function castWater(terrain: Grid, status: Grid, player: AsciiTurnState["player"], logs: string[]): void {
  let changed = 0;

  for (const position of rayFromPlayer(player, 8)) {
    if (terrain[position.row][position.column] === "#") {
      logs.push("물: 벽에서 멈춥니다.");
      break;
    }

    if (status[position.row][position.column] === "f") {
      status[position.row][position.column] = "w";
      changed += 1;
      logs.push("물+불: f 상태가 w로 낮아집니다.");
      continue;
    }

    if (terrain[position.row][position.column] === "=") {
      status[position.row][position.column] = "i";
      logs.push("물+얼음: i 상태가 표시됩니다.");
      continue;
    }

    status[position.row][position.column] = "w";
    changed += 1;
  }

  if (changed > 0) {
    logs.push(`물: ${changed}칸에 w 이펙트가 남습니다.`);
  }
}

function castWind(terrain: Grid, status: Grid, player: AsciiTurnState["player"], logs: string[]): void {
  const delta = directionDelta(player.facing);

  for (const position of rayFromPlayer(player, 9)) {
    const terrainCell = terrain[position.row][position.column];

    if (terrainCell === "#") {
      logs.push("바람: 벽에서 멈춥니다.");
      break;
    }

    if (terrainCell === "~") {
      const pushed = { row: position.row + delta.row, column: position.column + delta.column };

      if (terrain[pushed.row]?.[pushed.column] === ".") {
        terrain[pushed.row][pushed.column] = "~";
        terrain[position.row][position.column] = ".";
        status[pushed.row][pushed.column] = "*";
        logs.push("바람+물: 물이 한 칸 밀립니다.");
      }
      continue;
    }

    status[position.row][position.column] = "*";
  }

  logs.push("바람+불: 이번 턴의 확산 범위가 커집니다.");
}

function castEarth(terrain: Grid, status: Grid, player: AsciiTurnState["player"], logs: string[]): void {
  let placed = 0;

  for (const position of rayFromPlayer(player, 3)) {
    const cell = terrain[position.row][position.column];

    if (cell === ".") {
      terrain[position.row][position.column] = "#";
      status[position.row][position.column] = "*";
      placed += 1;
      continue;
    }

    if (cell === "~") {
      status[position.row][position.column] = "w";
      logs.push("땅+물: 젖은 흔적이 남습니다.");
    }
    break;
  }

  logs.push(`땅: 벽 ${placed}칸을 만들었습니다.`);
}

function castLife(terrain: Grid, status: Grid, player: AsciiTurnState["player"], logs: string[]): void {
  let grown = 0;
  let recovered = 0;

  for (const position of rayFromPlayer(player, 7)) {
    if (status[position.row][position.column] === "f") {
      status[position.row][position.column] = "g";
      recovered += 1;
      continue;
    }

    if (terrain[position.row][position.column] === "." && hasNeighborTerrain(terrain, position, "~")) {
      terrain[position.row][position.column] = "t";
      status[position.row][position.column] = "g";
      grown += 1;
      continue;
    }

    if (terrain[position.row][position.column] === "#") {
      break;
    }
  }

  logs.push(`생명: 성장 ${grown}칸, 회복 ${recovered}칸.`);
}

function castElectric(terrain: Grid, status: Grid, player: AsciiTurnState["player"], logs: string[]): void {
  for (const position of rayFromPlayer(player, 18)) {
    const cell = terrain[position.row][position.column];

    if (cell === "#") {
      logs.push("전기: 벽에서 멈춥니다.");
      break;
    }

    if (cell === "~" || cell === "M") {
      const charged = chargeConnectedConductors(terrain, status, position);
      logs.push(`전기: 연결된 물/금속 ${charged}칸이 e로 표시됩니다.`);
      return;
    }

    status[position.row][position.column] = "*";
  }

  logs.push("전기: 닿은 전도체가 없어 * 이펙트만 남습니다.");
}

function castIce(terrain: Grid, status: Grid, player: AsciiTurnState["player"], logs: string[]): void {
  let frozen = 0;

  for (const position of rayFromPlayer(player, 18)) {
    const cell = terrain[position.row][position.column];

    if (cell === "#") {
      break;
    }

    if (cell === "~") {
      terrain[position.row][position.column] = "=";
      status[position.row][position.column] = "i";
      frozen += 1;
      continue;
    }

    status[position.row][position.column] = "*";
  }

  logs.push(`얼음: 물 ${frozen}칸을 = 지형으로 고정했습니다.`);
}

function castVoid(status: Grid, player: AsciiTurnState["player"], logs: string[]): void {
  let cut = 0;

  for (const position of rayFromPlayer(player, 8)) {
    if (status[position.row][position.column] !== EMPTY_STATUS) {
      cut += 1;
    }

    status[position.row][position.column] = "x";
  }

  logs.push(`절단: 진행 방향 8칸에 x 표시를 남기고 상태 ${cut}칸을 끊었습니다.`);
}

function chargeConnectedConductors(terrain: Grid, status: Grid, start: Position): number {
  const queue = [start];
  const visited = new Set<string>();
  let charged = 0;

  while (queue.length > 0 && charged < 70) {
    const current = queue.shift();

    if (!current) {
      break;
    }

    const key = `${current.row}:${current.column}`;
    if (visited.has(key)) {
      continue;
    }
    visited.add(key);

    const terrainCell = terrain[current.row]?.[current.column];
    if (terrainCell !== "~" && terrainCell !== "M") {
      continue;
    }

    status[current.row][current.column] = "e";
    charged += 1;
    queue.push(...neighbors(current));
  }

  return charged;
}

function spreadFire(terrain: Grid, status: Grid, logs: string[], limit: number): void {
  const next = rowsToGrid(gridToRows(status));
  let spread = 0;

  forEachCell(status, (position) => {
    if (status[position.row][position.column] !== "f") {
      return;
    }

    for (const neighbor of neighbors(position)) {
      if (spread >= limit) {
        return;
      }

      if (terrain[neighbor.row]?.[neighbor.column] === "t" && status[neighbor.row][neighbor.column] !== "f") {
        next[neighbor.row][neighbor.column] = "f";
        spread += 1;
      }
    }
  });

  forEachCell(status, (position) => {
    status[position.row][position.column] = next[position.row][position.column];
  });

  if (spread > 0) {
    logs.push(`불: 인접 나무 ${spread}칸으로 옮겨붙습니다.`);
  }
}

function decayTransientStatuses(status: Grid): void {
  forEachCell(status, (position) => {
    const current = status[position.row][position.column];

    if (current !== EMPTY_STATUS && !PERSISTENT_STATUSES.has(current)) {
      status[position.row][position.column] = EMPTY_STATUS;
    }
  });
}

function createState(
  terrain: Grid,
  status: Grid,
  player: AsciiTurnState["player"],
  turn: number,
  lastAction: string,
  log: string[]
): AsciiTurnState {
  const terrainRows = gridToRows(terrain);
  const statusRows = gridToRows(status);

  return {
    width: ASCII_TUTORIAL_SIZE,
    height: ASCII_TUTORIAL_SIZE,
    turn,
    terrainRows,
    statusRows,
    player: { ...player },
    rows: renderRows(terrainRows, statusRows, player),
    lastAction,
    log
  };
}

function renderRows(terrainRows: string[], statusRows: string[], player: AsciiTurnState["player"]): string[] {
  return terrainRows.map((row, rowIndex) => {
    return row
      .split("")
      .map((terrainCell, columnIndex) => {
        if (rowIndex === player.row && columnIndex === player.column) {
          return playerSymbol(player.facing);
        }

        const statusCell = statusRows[rowIndex]?.[columnIndex] ?? EMPTY_STATUS;
        return statusCell === EMPTY_STATUS ? terrainCell : statusCell;
      })
      .join("");
  });
}

function rayFromPlayer(player: AsciiTurnState["player"], length: number): Position[] {
  const delta = directionDelta(player.facing);
  const cells: Position[] = [];

  for (let step = 1; step <= length; step += 1) {
    const row = player.row + delta.row * step;
    const column = player.column + delta.column * step;

    if (row < 0 || column < 0 || row >= ASCII_TUTORIAL_SIZE || column >= ASCII_TUTORIAL_SIZE) {
      break;
    }

    cells.push({ row, column });
  }

  return cells;
}

function directionDelta(direction: Direction): Position {
  switch (direction) {
    case "north":
      return { row: -1, column: 0 };
    case "east":
      return { row: 0, column: 1 };
    case "south":
      return { row: 1, column: 0 };
    case "west":
      return { row: 0, column: -1 };
  }
}

function playerSymbol(direction: Direction): string {
  switch (direction) {
    case "north":
      return "^";
    case "east":
      return ">";
    case "south":
      return "v";
    case "west":
      return "<";
  }
}

function hasNeighborTerrain(terrain: Grid, position: Position, target: string): boolean {
  return neighbors(position).some((neighbor) => terrain[neighbor.row]?.[neighbor.column] === target);
}

function makeGrid(fill: string): Grid {
  return Array.from({ length: ASCII_TUTORIAL_SIZE }, () => Array.from({ length: ASCII_TUTORIAL_SIZE }, () => fill));
}

function rowsToGrid(rows: string[]): Grid {
  return rows.map((row) => row.split(""));
}

function gridToRows(grid: Grid): string[] {
  return grid.map((row) => row.join(""));
}

function neighbors(position: Position): Position[] {
  return [
    { row: position.row - 1, column: position.column },
    { row: position.row + 1, column: position.column },
    { row: position.row, column: position.column - 1 },
    { row: position.row, column: position.column + 1 }
  ].filter((item) => item.row >= 0 && item.column >= 0 && item.row < ASCII_TUTORIAL_SIZE && item.column < ASCII_TUTORIAL_SIZE);
}

function forEachCell(grid: Grid, callback: (position: Position) => void): void {
  for (let row = 0; row < grid.length; row += 1) {
    for (let column = 0; column < grid[row].length; column += 1) {
      callback({ row, column });
    }
  }
}

function countChar(value: string, char: string): number {
  return value.split("").filter((item) => item === char).length;
}

function trimLogs(logs: string[]): string[] {
  const unique = Array.from(new Set(logs.filter(Boolean)));
  return unique.length > 0 ? unique.slice(0, 5) : ["변화 없음."];
}
