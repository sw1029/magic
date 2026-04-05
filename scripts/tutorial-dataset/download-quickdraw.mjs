#!/usr/bin/env node

import path from "node:path"

import { loadPublicDatasetManifest, parseArgs, downloadToFile, ensureDir, pathExists } from "./common.mjs"

const args = parseArgs(process.argv.slice(2))

if (args.help) {
  printHelp()
  process.exit(0)
}

const manifest = await loadPublicDatasetManifest()
const entry = manifest.datasets.find((item) => item.id === "quickdraw-raw")

if (!entry) {
  throw new Error("quickdraw-raw manifest entry is missing")
}

const labels = normalizeLabels(args.label, entry.recommendedLabels)
await ensureDir(entry.targetDir)

for (const label of labels) {
  const encoded = encodeURIComponent(label)
  const url = `${entry.baseUrl}/${encoded}.ndjson`
  const outPath = path.join(entry.targetDir, `${label}.ndjson`)

  if (await pathExists(outPath) && !args.force) {
    console.log(`skip ${label}: ${outPath} already exists`)
    continue
  }

  const result = await downloadToFile(url, outPath)
  console.log(`downloaded ${label} -> ${result.filePath} (${result.bytes} bytes, sha256=${result.sha256})`)
}

function normalizeLabels(value, fallback) {
  if (value === undefined) {
    return fallback
  }

  return Array.isArray(value) ? value.map(String) : [String(value)]
}

function printHelp() {
  console.log(`Usage:
  node scripts/tutorial-dataset/download-quickdraw.mjs --label circle --label triangle

Options:
  --label <name>            one or more Quick, Draw category names
  --force                   overwrite existing files
  --help`)
}
