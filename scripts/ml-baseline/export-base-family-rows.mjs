#!/usr/bin/env node

import { mkdtemp, readFile, rm, writeFile, readdir } from "node:fs/promises";
import os from "node:os";
import path from "node:path";
import { spawn } from "node:child_process";
import { fileURLToPath, pathToFileURL } from "node:url";

const BASE_SUPPORTED_PAIRS = ["earth__fire", "water__life", "recognized__ambiguous", "other"];
const MAX_TOP_K = 3;
const MAX_TOP_SCORE_GAP = 0.18;
const RECOGNIZED_MARGIN = 0.15;
const RECOGNIZED_SCORE = 0.7;
const AMBIGUOUS_SCORE = 0.55;

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "..", "..");

const args = parseArgs(process.argv.slice(2));

if (args.help || !args.out || !args.input || asArray(args.input).length === 0) {
  printHelp();
  process.exit(args.help ? 0 : 1);
}

const outputPath = path.resolve(REPO_ROOT, String(args.out));
const inputPaths = asArray(args.input).map((value) => path.resolve(REPO_ROOT, String(value)));

const tempDir = await mkdtemp(path.join(os.tmpdir(), "magic-base-family-rows-"));

try {
  const compiledDir = path.join(tempDir, "compiled");
  await compileRecognizer(compiledDir);
  await rewriteRelativeImports(compiledDir);

  const { recognizeSession } = await import(
    pathToFileURL(path.join(compiledDir, "src", "recognizer", "recognize.js")).href
  );

  const rows = [];

  for (const inputPath of inputPaths) {
    const contents = await readFile(inputPath, "utf8");
    const records = contents
      .split("\n")
      .map((line) => line.trim())
      .filter(Boolean)
      .map((line) => JSON.parse(line));

    const relativeInputPath = path.relative(REPO_ROOT, inputPath) || path.basename(inputPath);

    records.forEach((record, lineIndex) => {
      if (record?.kind !== "family" || typeof record?.label !== "string") {
        return;
      }

      const session = recordToSession(record, lineIndex);
      const result = recognizeSession(session, { sealed: true });
      const candidates = Array.isArray(result.candidates) ? result.candidates : [];

      if (candidates.length === 0) {
        return;
      }

      const topCandidate = candidates[0];
      const secondCandidate = candidates[1];
      const topScore = topCandidate.score;
      const topMargin = topCandidate.score - (secondCandidate?.score ?? 0);
      const selectedCandidates = candidates.filter(
        (candidate, index) => index < MAX_TOP_K && topScore - candidate.score <= MAX_TOP_SCORE_GAP
      );
      const selectedFamilies = new Set(selectedCandidates.map((candidate) => candidate.family));
      const candidatePairId = resolveCandidatePairId(candidates, topScore, topMargin);
      const goldCandidate = selectedCandidates.find((candidate) => candidate.family === record.label);
      const goldScore = goldCandidate?.score ?? null;
      const heuristicTopMatchesLabel = topCandidate.family === record.label;
      const heuristicStatus = resolveHeuristicStatus(topCandidate.score, topMargin, topCandidate.completenessHint);
      const targetRecognizedConfidence = heuristicTopMatchesLabel ? 1 : 0;
      const targetAmbiguityProbability = resolveAmbiguityTarget({
        label: record.label,
        selectedFamilies,
        topScore,
        topMargin
      });
      const sampleId = `${relativeInputPath}:${lineIndex + 1}`;

      selectedCandidates.forEach((candidate, index) => {
        rows.push({
          rowSpec: "base_candidate_row_v1",
          sampleId,
          inputPath: relativeInputPath,
          split: record.split,
          dataset: record.dataset,
          label: record.label,
          source: record.source ?? null,
          preset: record.metadata?.preset ?? null,
          syntheticPriority: record.metadata?.syntheticPriority ?? null,
          candidateFamilyId: candidate.family,
          candidatePairId,
          candidateRank: index,
          heuristicScore: roundNumber(candidate.score),
          templateDistance: roundNumber(candidate.templateDistance),
          topScoreGap: roundNumber(topScore - candidate.score),
          top1MinusTop2Margin: roundNumber(topMargin),
          strokeCount: result.features.strokeCount,
          pointCount: result.features.pointCount,
          durationMs: roundNumber(result.features.durationMs),
          pathLength: roundNumber(result.features.pathLength),
          closureGap: roundNumber(result.features.closureGap),
          dominantCorners: result.features.dominantCorners,
          endpointClusters: result.features.endpointClusters,
          circularity: roundNumber(result.features.circularity),
          fillRatio: roundNumber(result.features.fillRatio),
          parallelism: roundNumber(result.features.parallelism),
          rawAngleRadians: roundNumber(result.features.rawAngleRadians),
          qualityClosure: roundNumber(result.quality.closure),
          qualitySmoothness: roundNumber(result.quality.smoothness),
          qualityStability: roundNumber(result.quality.stability),
          qualityRotationBias: roundNumber(result.quality.rotationBias),
          heuristicTopFamilyId: topCandidate.family,
          heuristicTopScore: roundNumber(topScore),
          heuristicTopMargin: roundNumber(topMargin),
          heuristicStatus,
          heuristicTopMatchesLabel,
          targetIsCanonicalCandidate: candidate.family === record.label,
          targetRerankDelta: roundNumber(
            deriveTargetRerankDelta({
              candidate,
              candidateIndex: index,
              label: record.label,
              topScore,
              topMargin,
              goldScore,
              candidatePairId
            })
          ),
          targetRecognizedConfidence,
          targetAmbiguityProbability,
          topWindowSize: selectedCandidates.length
        });
      });
    });
  }

  await writeFile(outputPath, rows.map((row) => JSON.stringify(row)).join("\n").concat("\n"), "utf8");
  console.log(`wrote ${rows.length} base family candidate rows to ${path.relative(REPO_ROOT, outputPath)}`);
} finally {
  await rm(tempDir, { recursive: true, force: true });
}

function parseArgs(argv) {
  const result = {};

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

    if (result[key] === undefined) {
      result[key] = value;
      continue;
    }

    if (Array.isArray(result[key])) {
      result[key].push(value);
      continue;
    }

    result[key] = [result[key], value];
  }

  return result;
}

function asArray(value) {
  if (value === undefined) {
    return [];
  }

  return Array.isArray(value) ? value : [value];
}

async function compileRecognizer(outputDir) {
  const recognizerFiles = (await readdir(path.join(REPO_ROOT, "src", "recognizer")))
    .filter((entry) => entry.endsWith(".ts"))
    .map((entry) => path.join("src", "recognizer", entry));

  await runCommand(
    process.execPath,
    [
      path.join("node_modules", "typescript", "bin", "tsc"),
      "--ignoreConfig",
      "--target",
      "ES2020",
      "--module",
      "ES2020",
      "--moduleResolution",
      "Bundler",
      "--skipLibCheck",
      "--rootDir",
      ".",
      "--outDir",
      outputDir,
      ...recognizerFiles
    ],
    { cwd: REPO_ROOT }
  );
}

async function rewriteRelativeImports(outputDir) {
  const files = await collectFiles(outputDir);
  await Promise.all(
    files
      .filter((filePath) => filePath.endsWith(".js"))
      .map(async (filePath) => {
        const original = await readFile(filePath, "utf8");
        const rewritten = original
          .replace(/(from\s+["'])(\.\.?\/[^"'.]+)(["'])/g, "$1$2.js$3")
          .replace(/(import\(\s*["'])(\.\.?\/[^"'.]+)(["']\s*\))/g, "$1$2.js$3");

        if (rewritten !== original) {
          await writeFile(filePath, rewritten, "utf8");
        }
      })
  );
}

async function collectFiles(rootDir) {
  const entries = await readdir(rootDir, { withFileTypes: true });
  const files = [];

  for (const entry of entries) {
    const entryPath = path.join(rootDir, entry.name);
    if (entry.isDirectory()) {
      files.push(...(await collectFiles(entryPath)));
      continue;
    }
    files.push(entryPath);
  }

  return files;
}

function recordToSession(record, lineIndex) {
  const strokes = (record.strokes || [])
    .map((strokePoints, strokeIndex) => ({
      id: `${record.label || "sample"}-${lineIndex + 1}-${strokeIndex + 1}`,
      points: Array.isArray(strokePoints)
        ? strokePoints.map((point, pointIndex) => ({
            x: Number(point.x),
            y: Number(point.y),
            t: Number.isFinite(Number(point.t)) ? Number(point.t) : pointIndex * 16
          }))
        : []
    }))
    .filter((stroke) => stroke.points.length >= 2);

  const timestamps = strokes.flatMap((stroke) => stroke.points.map((point) => point.t));
  const startedAt = timestamps.length > 0 ? Math.min(...timestamps) : 0;
  const endedAt = timestamps.length > 0 ? Math.max(...timestamps) : startedAt;

  return {
    strokes,
    startedAt,
    endedAt
  };
}

function resolveCandidatePairId(candidates, topScore, topMargin) {
  const topFamilies = candidates.slice(0, 2).map((candidate) => candidate.family).sort();

  if (topFamilies.length === 2) {
    const pairId = `${topFamilies[0]}__${topFamilies[1]}`;
    if (BASE_SUPPORTED_PAIRS.includes(pairId)) {
      return pairId;
    }
  }

  if (topScore >= AMBIGUOUS_SCORE && (topScore < RECOGNIZED_SCORE || topMargin < RECOGNIZED_MARGIN)) {
    return "recognized__ambiguous";
  }

  return "other";
}

function resolveHeuristicStatus(topScore, topMargin, completenessHint) {
  if (topScore >= AMBIGUOUS_SCORE && completenessHint) {
    return "incomplete";
  }

  if (topScore >= RECOGNIZED_SCORE && topMargin >= RECOGNIZED_MARGIN) {
    return "recognized";
  }

  if (topScore >= AMBIGUOUS_SCORE) {
    return "ambiguous";
  }

  return "invalid";
}

function resolveAmbiguityTarget({ label, selectedFamilies, topScore, topMargin }) {
  if (selectedFamilies.has(label) && (topMargin < RECOGNIZED_MARGIN || topScore < RECOGNIZED_SCORE)) {
    return 1;
  }

  return 0;
}

function deriveTargetRerankDelta({
  candidate,
  candidateIndex,
  label,
  topScore,
  topMargin,
  goldScore,
  candidatePairId
}) {
  const candidateIsGold = candidate.family === label;
  const pairWeight =
    candidatePairId === "earth__fire" || candidatePairId === "water__life"
      ? 1
      : candidatePairId === "recognized__ambiguous"
        ? 0.7
        : 0.35;
  const proximity = clamp(1 - (topScore - candidate.score) / MAX_TOP_SCORE_GAP, 0, 1);
  const marginPressure = clamp((RECOGNIZED_MARGIN - topMargin) / RECOGNIZED_MARGIN, 0, 1);

  if (candidateIsGold) {
    const neededLift = goldScore === null ? 0.02 : Math.max(0, topScore - candidate.score + 0.03);
    return clamp((neededLift + 0.03 * marginPressure) * (0.65 + 0.35 * proximity), 0, 0.18);
  }

  if (goldScore === null) {
    return 0;
  }

  const neededSuppression = Math.max(0, candidate.score - goldScore + 0.03);
  const rankWeight = candidateIndex === 0 ? 1 : candidateIndex === 1 ? 0.85 : 0.65;
  return -clamp(neededSuppression * pairWeight * rankWeight * (0.55 + 0.45 * proximity + 0.2 * marginPressure), 0, 0.18);
}

function roundNumber(value) {
  return Number(Number(value).toFixed(6));
}

function clamp(value, minimum, maximum) {
  return Math.max(minimum, Math.min(maximum, value));
}

function printHelp() {
  console.log(`Usage:
  node scripts/ml-baseline/export-base-family-rows.mjs --input <file> [--input <file> ...] --out <file>

Examples:
  node scripts/ml-baseline/export-base-family-rows.mjs \\
    --input tmp/tutorial-dataset/synthetic-bootstrap-train.ndjson \\
    --input tmp/tutorial-dataset/synthetic-hard-negative-eval.ndjson \\
    --out tmp/ml-baseline/base-family-rows.jsonl`);
}

function runCommand(command, commandArgs, options = {}) {
  return new Promise((resolve, reject) => {
    const child = spawn(command, commandArgs, {
      cwd: options.cwd || REPO_ROOT,
      stdio: "inherit",
      env: process.env
    });

    child.on("error", reject);
    child.on("close", (code) => {
      if (code === 0) {
        resolve(undefined);
        return;
      }

      reject(new Error(`${command} ${commandArgs.join(" ")} exited with code ${code}`));
    });
  });
}
