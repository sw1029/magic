#!/usr/bin/env node

import path from "node:path"

import { loadPublicDatasetManifest, parseArgs, downloadToFile, ensureDir, extractArchive, pathExists } from "./common.mjs"

const args = parseArgs(process.argv.slice(2))

if (args.help) {
  printHelp()
  process.exit(0)
}

const manifest = await loadPublicDatasetManifest()
const entries = manifest.datasets.filter((item) => item.dataset === "dollar_family")

for (const entry of entries) {
  await ensureDir(entry.targetDir)
  const archiveName = path.basename(new URL(entry.url).pathname)
  const archivePath = path.join(entry.targetDir, archiveName)

  if (!(await pathExists(archivePath)) || args.force) {
    const result = await downloadToFile(entry.url, archivePath)
    console.log(`downloaded ${entry.id} -> ${result.filePath} (${result.bytes} bytes, sha256=${result.sha256})`)
  } else {
    console.log(`skip ${entry.id}: ${archivePath} already exists`)
  }

  if (entry.extract === "zip") {
    await extractArchive(archivePath, entry.targetDir)
    console.log(`extracted ${archivePath} -> ${entry.targetDir}`)
  }
}

function printHelp() {
  console.log(`Usage:
  node scripts/tutorial-dataset/download-dollar-family.mjs

Options:
  --force                   overwrite existing files
  --help`)
}
