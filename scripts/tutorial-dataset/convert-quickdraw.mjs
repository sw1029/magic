#!/usr/bin/env node

import { createReadStream } from "node:fs"
import { createInterface } from "node:readline"
import path from "node:path"

import {
  PUBLIC_ALLOWED_USES,
  PUBLIC_FORBIDDEN_USES,
  buildRecord,
  collectFiles,
  createNdjsonWriter,
  parseArgs,
  readJsonOrNdjson,
} from "./common.mjs"

const args = parseArgs(process.argv.slice(2))

if (args.help || !args.input) {
  printHelp()
  process.exit(args.help ? 0 : 1)
}

const inputPath = String(args.input)
const outputPath = String(args.out || "tmp/tutorial-dataset/quickdraw.ndjson")
const split = String(args.split || "pretrain")
const limit = args.limit ? Number(args.limit) : Infinity
const wordFilter = args.word ? String(args.word) : null

const writer = await createNdjsonWriter(outputPath)
let recordCount = 0

for await (const entry of iterateQuickdrawEntries(inputPath)) {
  if (recordCount >= limit) {
    break
  }

  if (wordFilter && entry.word !== wordFilter) {
    continue
  }

  const strokes = drawingToStrokes(entry.drawing)
  if (strokes.length === 0) {
    continue
  }

  await writer.write(
    buildRecord({
      dataset: "quickdraw",
      kind: "auxiliary",
      label: null,
      split,
      priority: "public_auxiliary",
      source: "quickdraw",
      allowedUses: PUBLIC_ALLOWED_USES,
      forbiddenUses: PUBLIC_FORBIDDEN_USES,
      strokes,
      metadata: {
        sourceLabel: entry.word || null,
        sourceFile: entry.__sourceFile ?? null,
        recognized: entry.recognized ?? null,
        countrycode: entry.countrycode || null,
        keyId: entry.key_id || null
      }
    })
  )
  recordCount += 1
}

await writer.close()
console.log(`wrote ${recordCount} quickdraw records to ${outputPath}`)

function drawingToStrokes(drawing) {
  if (!Array.isArray(drawing)) {
    return []
  }

  return drawing
    .map((stroke, strokeIndex) => {
      if (!Array.isArray(stroke) || stroke.length < 2) {
        return null
      }

      const xs = stroke[0]
      const ys = stroke[1]
      const ts = Array.isArray(stroke[2]) ? stroke[2] : []

      if (!Array.isArray(xs) || !Array.isArray(ys) || xs.length !== ys.length || xs.length < 2) {
        return null
      }

      return {
        id: `quickdraw-${strokeIndex + 1}`,
        points: xs.map((x, pointIndex) => ({
          x: Number(x),
          y: Number(ys[pointIndex]),
          t: Number.isFinite(Number(ts[pointIndex])) ? Number(ts[pointIndex]) : pointIndex * 16
        }))
      }
    })
    .filter(Boolean)
}

async function* iterateQuickdrawEntries(inputPath) {
  const files = await collectFiles(inputPath, [".ndjson", ".json"])

  for (const filePath of files) {
    if (path.extname(filePath).toLowerCase() === ".ndjson") {
      const input = createReadStream(filePath, "utf8")
      const lineReader = createInterface({ input, crlfDelay: Infinity })

      for await (const line of lineReader) {
        const trimmed = line.trim()

        if (!trimmed) {
          continue
        }

        const entry = JSON.parse(trimmed)
        entry.__sourceFile = filePath
        yield entry
      }

      continue
    }

    const entries = await readJsonOrNdjson(filePath)

    for (const entry of entries) {
      entry.__sourceFile = filePath
      yield entry
    }
  }
}

function printHelp() {
  console.log(`Usage:
  node scripts/tutorial-dataset/convert-quickdraw.mjs --input data/circle.ndjson --out tmp/tutorial-dataset/quickdraw-circle.ndjson

Expected input:
  Quick, Draw NDJSON or JSON array where each item has a drawing field in the form
  [[x...], [y...]] or [[x...], [y...], [t...]]

Options:
  --input <path>            required
  --out <path>              default tmp/tutorial-dataset/quickdraw.ndjson
  --word <label>            optional source label filter
  --limit <n>
  --split <name>            default pretrain
  --help`)
}
