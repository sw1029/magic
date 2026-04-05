#!/usr/bin/env node

import { parseArgs, runCommand } from "./common.mjs"

const args = parseArgs(process.argv.slice(2))
const dataset = String(args.dataset || "all")

if (args.help) {
  printHelp()
  process.exit(0)
}

if (dataset === "all" || dataset === "quickdraw") {
  await runNodeScript("scripts/tutorial-dataset/download-quickdraw.mjs", [
    ...normalizeLabelsToArgs(args.label),
    ...(args.force ? ["--force"] : [])
  ])
}

if (dataset === "all" || dataset === "dollar") {
  await runNodeScript("scripts/tutorial-dataset/download-dollar-family.mjs", args.force ? ["--force"] : [])
}

if (dataset === "all" || dataset === "crohme") {
  await runNodeScript("scripts/tutorial-dataset/download-crohme.mjs", args.force ? ["--force"] : [])
}

function normalizeLabelsToArgs(value) {
  if (value === undefined) {
    return []
  }

  const labels = Array.isArray(value) ? value : [value]
  return labels.flatMap((label) => ["--label", String(label)])
}

async function runNodeScript(scriptPath, args = []) {
  await runCommand(process.execPath, [scriptPath, ...args])
}

function printHelp() {
  console.log(`Usage:
  node scripts/tutorial-dataset/download-all.mjs --dataset all

Options:
  --dataset all|quickdraw|dollar|crohme   default all
  --label <name>                          quickdraw labels override
  --force
  --help`)
}
