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
  parseXmlAttributes,
} from "./common.mjs"

const args = parseArgs(process.argv.slice(2))

if (args.help || !args.input) {
  printHelp()
  process.exit(args.help ? 0 : 1)
}

const inputPath = String(args.input)
const outputPath = String(args.out || "tmp/tutorial-dataset/dollar-family.ndjson")
const split = String(args.split || "pretrain")
const limit = args.limit ? Number(args.limit) : Infinity
const nameFilter = args.name ? String(args.name) : null
const files = await collectFiles(inputPath, [".xml"])
const writer = await createNdjsonWriter(outputPath)
let recordCount = 0

for (const filePath of files) {
  if (recordCount >= limit) {
    break
  }

  const xml = await readFile(filePath, "utf8")
  const gestureMatch = xml.match(/<Gesture\b([^>]*)>/i)
  const gestureAttrs = gestureMatch ? parseXmlAttributes(gestureMatch[1]) : {}
  const gestureName = gestureAttrs.Name || gestureAttrs.name || path.basename(filePath, ".xml")

  if (nameFilter && gestureName !== nameFilter) {
    continue
  }

  const strokes = parseDollarStrokes(xml)
  if (strokes.length === 0) {
    continue
  }

  await writer.write(
    buildRecord({
      dataset: "dollar_family",
      kind: "auxiliary",
      label: null,
      split,
      priority: "public_auxiliary",
      source: "dollar_family",
      allowedUses: PUBLIC_ALLOWED_USES,
      forbiddenUses: PUBLIC_FORBIDDEN_USES,
      strokes,
      metadata: {
        layerRole: "public_auxiliary",
        sourceLabel: gestureName,
        sourcePath: filePath
      }
    })
  )
  recordCount += 1
}

await writer.close()
console.log(`wrote ${recordCount} $-family records to ${outputPath}`)

function parseDollarStrokes(xml) {
  const strokeBlocks = [...xml.matchAll(/<Stroke\b[^>]*>([\s\S]*?)<\/Stroke>/gi)]

  if (strokeBlocks.length > 0) {
    return strokeBlocks
      .map((match, strokeIndex) => parseDollarPoints(match[1], `dollar-${strokeIndex + 1}`))
      .filter((stroke) => stroke.points.length >= 2)
  }

  const fallback = parseDollarPoints(xml, "dollar-1")
  return fallback.points.length >= 2 ? [fallback] : []
}

function parseDollarPoints(fragment, strokeId) {
  const points = []
  let fallbackTime = 0

  for (const match of fragment.matchAll(/<Point\b([^>]*)\/?>/gi)) {
    const attrs = parseXmlAttributes(match[1])
    const x = Number(attrs.X ?? attrs.x)
    const y = Number(attrs.Y ?? attrs.y)

    if (!Number.isFinite(x) || !Number.isFinite(y)) {
      continue
    }

    points.push({
      x,
      y,
      t: Number.isFinite(Number(attrs.T)) ? Number(attrs.T) : fallbackTime
    })

    fallbackTime += 16
  }

  return {
    id: strokeId,
    points
  }
}

function printHelp() {
  console.log(`Usage:
  node scripts/tutorial-dataset/convert-dollar-family.mjs --input external/dollar --out tmp/tutorial-dataset/dollar.ndjson

Expected input:
  Directory or XML file from a $-family gesture dataset. The converter reads <Gesture> and <Point> tags,
  and treats each example as auxiliary representation-learning data only.

Options:
  --input <path>            required file or directory
  --out <path>              default tmp/tutorial-dataset/dollar-family.ndjson
  --name <gestureName>      optional source label filter
  --limit <n>
  --split <name>            default pretrain
  --help`)
}
