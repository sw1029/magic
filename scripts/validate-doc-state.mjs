import { readFile, readdir } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const rootDir = path.resolve(__dirname, "..");
const docsDir = path.join(rootDir, "docs");
const taskRoot = path.join(docsDir, "30_tasks");
const queuePath = path.join(docsDir, "20_queue", "work-queue.md");

const allowedStatuses = new Set(["todo", "ready", "in_progress", "blocked", "done", "backlog"]);

async function walkTaskFiles(dir) {
  const entries = await readdir(dir, { withFileTypes: true });
  const files = [];

  for (const entry of entries) {
    const entryPath = path.join(dir, entry.name);

    if (entry.isDirectory()) {
      files.push(...(await walkTaskFiles(entryPath)));
      continue;
    }

    if (entry.name.endsWith(".md") && entry.name !== "README.md") {
      files.push(entryPath);
    }
  }

  return files;
}

function parseFrontmatter(markdown, filePath) {
  const match = markdown.match(/^- id: (?<id>T\d\d-\d\d)$/m);

  if (!match?.groups?.id) {
    throw new Error(`task id를 찾지 못했습니다: ${filePath}`);
  }

  const get = (key) => {
    const item = markdown.match(new RegExp(`^- ${key}: (.+)$`, "m"));
    if (!item) {
      throw new Error(`${filePath} 에 ${key} 메타가 없습니다.`);
    }
    return item[1].trim();
  };

  const status = get("status");

  if (!allowedStatuses.has(status)) {
    throw new Error(`${filePath} 의 status '${status}' 는 허용되지 않습니다.`);
  }

  return {
    id: match.groups.id,
    status,
    dependsOn: get("depends_on"),
    blocks: get("blocks"),
    filePath
  };
}

function parseQueueRows(markdown) {
  const rowRegex =
    /\|\s*(P\d)\s*\|\s*(\w+)\s*\|\s*\[(T\d\d-\d\d)[^\]]*\]\([^)]+\)\s*\|\s*([^|]+?)\s*\|\s*([^|]+?)\s*\|/g;
  const rows = new Map();
  let match;

  while ((match = rowRegex.exec(markdown)) !== null) {
    rows.set(match[3], {
      priority: match[1],
      status: match[2],
      dependsOn: match[4].trim(),
      blocks: match[5].trim()
    });
  }

  return rows;
}

function dependencyList(value) {
  if (!value || value === "-") {
    return [];
  }
  return value.split(",").map((item) => item.trim());
}

async function main() {
  const taskFiles = await walkTaskFiles(taskRoot);
  const tasks = new Map();

  for (const filePath of taskFiles) {
    const markdown = await readFile(filePath, "utf8");
    const frontmatter = parseFrontmatter(markdown, filePath);
    tasks.set(frontmatter.id, frontmatter);
  }

  const queueMarkdown = await readFile(queuePath, "utf8");
  const queueRows = parseQueueRows(queueMarkdown);
  const errors = [];

  for (const [id, task] of tasks) {
    const row = queueRows.get(id);

    if (!row) {
      errors.push(`work-queue.md 에 ${id} 행이 없습니다.`);
      continue;
    }

    if (row.status !== task.status) {
      errors.push(`${id}: queue status=${row.status}, task status=${task.status}`);
    }

    if (row.dependsOn !== task.dependsOn) {
      errors.push(`${id}: queue depends_on='${row.dependsOn}', task depends_on='${task.dependsOn}'`);
    }

    if (row.blocks !== task.blocks) {
      errors.push(`${id}: queue blocks='${row.blocks}', task blocks='${task.blocks}'`);
    }

    const deps = dependencyList(task.dependsOn);

    if (task.status === "ready") {
      const unresolved = deps.filter((depId) => tasks.get(depId)?.status !== "done");
      if (unresolved.length > 0) {
        errors.push(`${id}: ready 이지만 선행 task가 done이 아닙니다 -> ${unresolved.join(", ")}`);
      }
    }

    if (task.status === "blocked") {
      const unresolved = deps.filter((depId) => tasks.get(depId)?.status !== "done");
      if (deps.length > 0 && unresolved.length === 0) {
        errors.push(`${id}: blocked 이지만 모든 선행 task가 done 입니다.`);
      }
    }
  }

  for (const id of queueRows.keys()) {
    if (!tasks.has(id)) {
      errors.push(`work-queue.md 의 ${id} 는 task 파일이 없습니다.`);
    }
  }

  if (errors.length > 0) {
    for (const error of errors) {
      console.error(`- ${error}`);
    }
    process.exit(1);
  }

  console.log(`validated ${tasks.size} task documents against work queue`);
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
