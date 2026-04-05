#!/usr/bin/env node

import {
  TUTORIAL_ALLOWED_USES,
  VALID_FAMILIES,
  VALID_OPERATORS,
  VALID_TUTORIAL_SOURCES,
  buildRecord,
  parseArgs,
  readJsonOrNdjson,
  sanitizeStrokes,
  writeNdjson
} from "./common.mjs"

const args = parseArgs(process.argv.slice(2))

if (args.help || !args.input) {
  printHelp()
  process.exit(args.help ? 0 : 1)
}

const inputPath = String(args.input)
const outputPath = String(args.out || "tmp/tutorial-dataset/tutorial-captures.ndjson")
const split = String(args.split || "adaptation")
const payload = await readJsonOrNdjson(inputPath)
const captures = Array.isArray(payload) ? payload : payload?.captures

if (!Array.isArray(captures)) {
  throw new Error("input must be a TutorialCapture array or an object with captures[]")
}

const records = captures.map((capture, index) => tutorialCaptureToRecord(capture, index, split))
await writeNdjson(outputPath, records)

console.log(`wrote ${records.length} tutorial records to ${outputPath}`)

function tutorialCaptureToRecord(capture, index, split) {
  const kind = capture.kind
  if (kind !== "family" && kind !== "operator") {
    throw new Error(`capture[${index}] has invalid kind: ${kind}`)
  }

  const label = kind === "family" ? capture.expectedFamily : capture.expectedOperator
  if (!label) {
    throw new Error(`capture[${index}] is missing expected label`)
  }

  if (kind === "family" && !VALID_FAMILIES.has(label)) {
    throw new Error(`capture[${index}] has unknown family: ${label}`)
  }

  if (kind === "operator" && !VALID_OPERATORS.has(label)) {
    throw new Error(`capture[${index}] has unknown operator: ${label}`)
  }

  if (capture.source && !VALID_TUTORIAL_SOURCES.has(capture.source)) {
    throw new Error(`capture[${index}] has invalid source: ${capture.source}`)
  }

  return buildRecord({
    dataset: "tutorial",
    kind,
    label,
    split,
    priority: "tutorial_primary",
    source: capture.source || "trace",
    allowedUses: TUTORIAL_ALLOWED_USES,
    strokes: sanitizeStrokes(capture.strokes, `tutorial-${index + 1}`),
    metadata: {
      layerRole: "tutorial_primary",
      captureId: capture.id || `tutorial-${index + 1}`,
      timestamp: capture.timestamp || null,
      tutorialPriorityRank: 0
    }
  })
}

function printHelp() {
  console.log(`Usage:
  node scripts/tutorial-dataset/convert-tutorial-captures.mjs --input export/tutorial.json --out tmp/tutorial-dataset/tutorial.ndjson

Accepted input shapes:
  1. TutorialCapture[]
  2. { captures: TutorialCapture[] }

Options:
  --input <path>            required
  --out <path>              default tmp/tutorial-dataset/tutorial-captures.ndjson
  --split adaptation|eval   default adaptation
  --help`)
}
