#!/usr/bin/env node

import { readFile } from "node:fs/promises"
import path from "node:path"

import {
  PUBLIC_ALLOWED_USES,
  PUBLIC_FORBIDDEN_USES,
  buildRecord,
  collectFiles,
  createNdjsonWriter,
  parseArgs,
} from "./common.mjs"

const args = parseArgs(process.argv.slice(2))

if (args.help || !args.input) {
  printHelp()
  process.exit(args.help ? 0 : 1)
}

const inputPath = String(args.input)
const outputPath = String(args.out || "tmp/tutorial-dataset/crohme.ndjson")
const split = String(args.split || "pretrain")
const limit = args.limit ? Number(args.limit) : Infinity
const files = await collectFiles(inputPath, [".inkml"])
const writer = await createNdjsonWriter(outputPath)
let recordCount = 0

for (const filePath of files) {
  if (recordCount >= limit) {
    break
  }

  const inkml = await readFile(filePath, "utf8")
  const strokes = parseInkmlTraces(inkml)

  if (strokes.length === 0) {
    continue
  }

  await writer.write(
    buildRecord({
      dataset: "crohme",
      kind: "auxiliary",
      label: null,
      split,
      priority: "public_auxiliary",
      source: "crohme",
      allowedUses: PUBLIC_ALLOWED_USES,
      forbiddenUses: PUBLIC_FORBIDDEN_USES,
      strokes,
      metadata: {
        sourceLabel: extractInkmlLabel(inkml),
        sourcePath: filePath,
        traceCount: strokes.length
      }
    })
  )
  recordCount += 1
}

await writer.close()
console.log(`wrote ${recordCount} CROHME records to ${outputPath}`)

function parseInkmlTraces(inkml) {
  const traces = []

  for (const match of inkml.matchAll(/<trace\b[^>]*id="([^"]+)"[^>]*>([\s\S]*?)<\/trace>/gi)) {
    const traceId = match[1]
    const body = match[2]
    const points = body
      .split(",")
      .map((token) => token.trim())
      .filter(Boolean)
      .map((token, index) => {
        const parts = token.split(/\s+/).map(Number)
        const [x, y, t] = parts

        if (!Number.isFinite(x) || !Number.isFinite(y)) {
          return null
        }

        return {
          x,
          y,
          t: Number.isFinite(t) ? t : index * 16
        }
      })
      .filter(Boolean)

    if (points.length >= 2) {
      traces.push({
        id: `crohme-${traceId}`,
        points
      })
    }
  }

  return traces
}

function extractInkmlLabel(inkml) {
  const truthMatch = inkml.match(/<annotation\b[^>]*type="truth"[^>]*>([\s\S]*?)<\/annotation>/i)
  if (truthMatch) {
    return truthMatch[1].trim()
  }

  const genericMatch = inkml.match(/<annotation\b[^>]*>([\s\S]*?)<\/annotation>/i)
  return genericMatch ? genericMatch[1].trim() : "unknown"
}

function printHelp() {
  console.log(`Usage:
  node scripts/tutorial-dataset/convert-crohme.mjs --input external/crohme --out tmp/tutorial-dataset/crohme.ndjson

Expected input:
  InkML files with <trace> elements. This converter keeps source labels in metadata only and
  marks every record as auxiliary public data.

Options:
  --input <path>            required file or directory
  --out <path>              default tmp/tutorial-dataset/crohme.ndjson
  --limit <n>
  --split <name>            default pretrain
  --help`)
}
