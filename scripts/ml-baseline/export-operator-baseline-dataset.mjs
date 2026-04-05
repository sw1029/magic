#!/usr/bin/env node

import { writeOperatorBaselineDataset } from "./operator-baseline-lib.mjs";

const args = parseArgs(process.argv.slice(2));

if (args.help) {
  printHelp();
  process.exit(0);
}

const result = await writeOperatorBaselineDataset({
  out: args.out,
  summaryOut: args["summary-out"],
  seed: args.seed,
  runtimeCacheDir: args["runtime-cache-dir"]
});

console.log(`wrote ${result.rowCount} operator baseline rows to ${result.outputPath}`);
console.log(`wrote operator baseline summary to ${result.summaryPath}`);
console.log(`seed=${result.seed}`);

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
  node scripts/ml-baseline/export-operator-baseline-dataset.mjs

Options:
  --out <path>                default tmp/ml-baseline/operator-baseline-rows.ndjson
  --summary-out <path>        default tmp/ml-baseline/operator-baseline-summary.json
  --seed <n>                  deterministic synthetic seed
  --runtime-cache-dir <path>  default .tmp/ml-baseline-runtime
  --help`);
}
