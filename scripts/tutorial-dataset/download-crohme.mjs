#!/usr/bin/env node

import path from "node:path"

import {
  loadPublicDatasetManifest,
  parseArgs,
  downloadToFile,
  ensureDir,
  extractArchive,
  pathExists,
  resolveArtifactUrlFromPage
} from "./common.mjs"

const args = parseArgs(process.argv.slice(2))

if (args.help) {
  printHelp()
  process.exit(0)
}

const manifest = await loadPublicDatasetManifest()
const entry = manifest.datasets.find((item) => item.dataset === "crohme")

if (!entry) {
  throw new Error("crohme manifest entry is missing")
}

await ensureDir(entry.targetDir)
const resolvedUrl =
  entry.kind === "page-artifact" ? await resolveArtifactUrlFromPage(entry.pageUrl, entry.artifactName) : entry.url
const archiveName =
  entry.kind === "page-artifact" ? entry.artifactName : path.basename(new URL(resolvedUrl).pathname)
const archivePath = path.join(entry.targetDir, archiveName)

if (!(await pathExists(archivePath)) || args.force) {
  const result = await downloadToFile(resolvedUrl, archivePath)
  console.log(`downloaded ${entry.id} -> ${result.filePath} (${result.bytes} bytes, sha256=${result.sha256})`)
} else {
  console.log(`skip ${entry.id}: ${archivePath} already exists`)
}

if (entry.extract === "zip") {
  await extractArchive(archivePath, entry.targetDir)
  console.log(`extracted ${archivePath} -> ${entry.targetDir}`)
}

function printHelp() {
  console.log(`Usage:
  node scripts/tutorial-dataset/download-crohme.mjs

Options:
  --force                   overwrite existing files
  --help`)
}
