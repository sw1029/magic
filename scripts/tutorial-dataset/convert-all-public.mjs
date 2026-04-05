#!/usr/bin/env node

import { parseArgs, runCommand } from "./common.mjs"

const args = parseArgs(process.argv.slice(2))

if (args.help) {
  printHelp()
  process.exit(0)
}

await runNodeScript("scripts/tutorial-dataset/convert-quickdraw.mjs", [
  "--input",
  "external/quickdraw",
  "--out",
  "tmp/tutorial-dataset/quickdraw-public.ndjson"
])
await runNodeScript("scripts/tutorial-dataset/convert-dollar-family.mjs", [
  "--input",
  "external/dollar",
  "--out",
  "tmp/tutorial-dataset/dollar-public.ndjson"
])
await runNodeScript("scripts/tutorial-dataset/convert-crohme.mjs", [
  "--input",
  "external/crohme",
  "--out",
  "tmp/tutorial-dataset/crohme-public.ndjson"
])

function printHelp() {
  console.log(`Usage:
  node scripts/tutorial-dataset/convert-all-public.mjs
`)
}

async function runNodeScript(scriptPath, args = []) {
  await runCommand(process.execPath, [scriptPath, ...args])
}
