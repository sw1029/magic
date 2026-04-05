#!/usr/bin/env node

import { spawn } from "node:child_process";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, "..", "..");
const args = parseArgs(process.argv.slice(2));

if (args.help) {
  printHelp();
  process.exit(0);
}

const rowsPath = args.rows || "tmp/ml-baseline/operator-baseline-rows.ndjson";
const summaryPath = args.summary || "tmp/ml-baseline/operator-baseline-summary.json";
const rerankArtifact = args["rerank-artifact"] || "artifacts/ml/operator-rerank-v1.json";
const confidenceArtifact = args["confidence-artifact"] || "artifacts/ml/operator-confidence-v1.json";
const seed = args.seed ? String(args.seed) : undefined;

await runNode([
  "scripts/ml-baseline/export-operator-baseline-dataset.mjs",
  "--out",
  rowsPath,
  "--summary-out",
  summaryPath,
  ...(seed ? ["--seed", seed] : [])
]);

await runProcess(resolvePythonCommand(), [
  "scripts/ml-baseline/train-operator-baseline.py",
  "--rows",
  rowsPath,
  "--summary",
  summaryPath,
  "--rerank-artifact",
  rerankArtifact,
  "--confidence-artifact",
  confidenceArtifact
]);

function resolvePythonCommand() {
  return process.env.PYTHON || "python";
}

function runNode(args) {
  return runProcess("node", args);
}

function runProcess(command, args) {
  return new Promise((resolve, reject) => {
    const child = spawn(command, args, {
      cwd: repoRoot,
      stdio: "inherit"
    });
    child.on("error", reject);
    child.on("close", (code) => {
      if (code === 0) {
        resolve();
        return;
      }
      reject(new Error(`${command} ${args.join(" ")} failed with code ${code}`));
    });
  });
}

function parseArgs(argv) {
  const parsed = {};

  for (let index = 0; index < argv.length; index += 1) {
    const token = argv[index];

    if (!token.startsWith("--")) {
      continue;
    }

    const key = token.slice(2);
    const next = argv[index + 1];
    const value = next && !next.startsWith("--") ? next : true;

    if (value !== true) {
      index += 1;
    }

    parsed[key] = value;
  }

  return parsed;
}

function printHelp() {
  console.log(`Usage:
  node scripts/ml-baseline/build-operator-baseline.mjs

Options:
  --rows <path>                  default tmp/ml-baseline/operator-baseline-rows.ndjson
  --summary <path>               default tmp/ml-baseline/operator-baseline-summary.json
  --rerank-artifact <path>       default artifacts/ml/operator-rerank-v1.json
  --confidence-artifact <path>   default artifacts/ml/operator-confidence-v1.json
  --seed <n>                     deterministic synthetic seed
  --help`);
}
