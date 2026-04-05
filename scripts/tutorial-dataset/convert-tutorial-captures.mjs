#!/usr/bin/env node

import { pathToFileURL } from "node:url"
import { writeFile } from "node:fs/promises"

import {
  DATASET_SCHEMA_VERSION,
  TUTORIAL_ALLOWED_USES,
  VALID_FAMILIES,
  VALID_OPERATORS,
  VALID_TUTORIAL_SOURCES,
  buildRecord,
  ensureParentDir,
  parseArgs,
  readJsonOrNdjson,
  sanitizeStrokes,
  writeNdjson
} from "./common.mjs"

export const TUTORIAL_EXPORT_CONTRACT_VERSION = "tutorial-adaptation-export-v1"
export const DEFAULT_TUTORIAL_USER_PARTITION_KEY = "local-default"
export const DEFAULT_SESSION_GAP_MINUTES = 30
export const DEFAULT_ACCEPTANCE_EVERY = 5
export const DEFAULT_ACCEPTANCE_MIN_GROUP_SIZE = 5

if (isMainModule(import.meta.url)) {
  await runCli(process.argv.slice(2))
}

export async function runCli(argv) {
  const args = parseArgs(argv)

  if (args.help || !args.input) {
    printHelp()
    process.exit(args.help ? 0 : 1)
  }

  const inputPath = String(args.input)
  const outputPath = typeof args.out === "string" ? String(args.out) : "tmp/tutorial-dataset/tutorial-captures.ndjson"
  const payload = await readJsonOrNdjson(inputPath)
  const plan = buildTutorialExportPlan(payload, {
    inputPath,
    split: typeof args.split === "string" ? String(args.split) : undefined,
    outputPath,
    adaptationOut: typeof args["adaptation-out"] === "string" ? String(args["adaptation-out"]) : undefined,
    acceptanceOut: typeof args["acceptance-out"] === "string" ? String(args["acceptance-out"]) : undefined,
    manifestOut: typeof args["manifest-out"] === "string" ? String(args["manifest-out"]) : undefined,
    acceptanceEvery: toPositiveInt(args["acceptance-every"], DEFAULT_ACCEPTANCE_EVERY),
    acceptanceMinGroupSize: toPositiveInt(args["acceptance-min-group-size"], DEFAULT_ACCEPTANCE_MIN_GROUP_SIZE),
    userPartitionKey: typeof args["user-partition-key"] === "string" ? String(args["user-partition-key"]) : undefined,
    sessionKey: typeof args["session-key"] === "string" ? String(args["session-key"]) : undefined,
    sessionGapMinutes: toPositiveInt(args["session-gap-minutes"], DEFAULT_SESSION_GAP_MINUTES)
  })

  const adaptationRecords = plan.records.filter((record) => record.split === "adaptation")
  const acceptanceRecords = plan.records.filter((record) => record.split === "acceptance_eval")

  await writeNdjson(outputPath, plan.records)

  if (plan.outputs.adaptationOut) {
    await writeNdjson(plan.outputs.adaptationOut, adaptationRecords)
  }

  if (plan.outputs.acceptanceOut) {
    await writeNdjson(plan.outputs.acceptanceOut, acceptanceRecords)
  }

  if (plan.outputs.manifestOut) {
    await writeJson(plan.outputs.manifestOut, plan.manifest)
  }

  console.log(`wrote ${plan.records.length} tutorial records to ${outputPath}`)
  if (plan.outputs.adaptationOut) {
    console.log(`wrote ${adaptationRecords.length} adaptation records to ${plan.outputs.adaptationOut}`)
  }
  if (plan.outputs.acceptanceOut) {
    console.log(`wrote ${acceptanceRecords.length} acceptance_eval records to ${plan.outputs.acceptanceOut}`)
  }
  if (plan.outputs.manifestOut) {
    console.log(`wrote tutorial export manifest to ${plan.outputs.manifestOut}`)
  }
}

export function buildTutorialExportPlan(payload, options = {}) {
  const resolved = resolveTutorialExportInput(payload, options)
  const mode = resolved.split ? "locked" : "auto_holdout"
  const sessionGapMs = resolved.sessionGapMinutes * 60_000
  const acceptedSplit = resolved.split

  if (acceptedSplit && acceptedSplit !== "adaptation" && acceptedSplit !== "acceptance_eval") {
    throw new Error(`split must be adaptation or acceptance_eval: ${acceptedSplit}`)
  }

  const records = resolved.entries.map((entry) =>
    tutorialCaptureToRecord(entry.capture, {
      split:
        acceptedSplit ??
        resolveTutorialSplit(entry, resolved.entries, {
          acceptanceEvery: resolved.acceptanceEvery,
          acceptanceMinGroupSize: resolved.acceptanceMinGroupSize
        }),
      userPartitionKey: entry.userPartitionKey,
      sessionKey: entry.sessionKey,
      captureOrdinal: entry.captureOrdinal,
      sessionOrdinal: entry.sessionOrdinal,
      inputOrdinal: entry.inputOrdinal,
      storeVersion: resolved.storeVersion,
      storeUpdatedAt: resolved.storeUpdatedAt,
      exportMode: mode
    })
  )

  return {
    records,
    outputs: {
      out: resolved.outputPath,
      adaptationOut: resolved.adaptationOut,
      acceptanceOut: resolved.acceptanceOut,
      manifestOut: resolved.manifestOut
    },
    manifest: buildTutorialExportManifest(records, {
      inputPath: resolved.inputPath,
      outputPath: resolved.outputPath,
      adaptationOut: resolved.adaptationOut,
      acceptanceOut: resolved.acceptanceOut,
      manifestOut: resolved.manifestOut,
      mode,
      split: acceptedSplit ?? null,
      sessionGapMinutes: resolved.sessionGapMinutes,
      acceptanceEvery: resolved.acceptanceEvery,
      acceptanceMinGroupSize: resolved.acceptanceMinGroupSize,
      storeVersion: resolved.storeVersion,
      storeUpdatedAt: resolved.storeUpdatedAt,
      userPartitionKeys: [...new Set(resolved.entries.map((entry) => entry.userPartitionKey))],
      sessionKeys: [...new Set(resolved.entries.map((entry) => entry.sessionKey))]
    })
  }
}

export function resolveTutorialExportInput(payload, options = {}) {
  const root = Array.isArray(payload) ? { captures: payload } : payload
  const captures = Array.isArray(root?.captures) ? root.captures : undefined

  if (!Array.isArray(captures)) {
    throw new Error("input must be a TutorialCapture array or an object with captures[]")
  }

  const metadata = isRecord(root?.metadata) ? root.metadata : {}
  const userPartitionKey =
    stringOrUndefined(options.userPartitionKey) ??
    stringOrUndefined(root?.userPartitionKey) ??
    stringOrUndefined(metadata.userPartitionKey) ??
    DEFAULT_TUTORIAL_USER_PARTITION_KEY
  const explicitSessionKey =
    stringOrUndefined(options.sessionKey) ??
    stringOrUndefined(root?.sessionKey) ??
    stringOrUndefined(metadata.sessionKey)
  const sorted = captures
    .map((capture, index) => ({
      capture,
      inputOrdinal: index + 1
    }))
    .sort((left, right) => {
      const leftTimestamp = finiteTimestamp(left.capture?.timestamp)
      const rightTimestamp = finiteTimestamp(right.capture?.timestamp)
      if (leftTimestamp !== rightTimestamp) {
        return leftTimestamp - rightTimestamp
      }
      return left.inputOrdinal - right.inputOrdinal
    })

  const sessionGapMinutes = toPositiveInt(options.sessionGapMinutes, DEFAULT_SESSION_GAP_MINUTES)
  const entries = assignTutorialSessionMetadata(sorted, {
    userPartitionKey,
    explicitSessionKey,
    sessionGapMs: sessionGapMinutes * 60_000
  })

  return {
    inputPath: stringOrUndefined(options.inputPath) ?? null,
    outputPath: stringOrUndefined(options.outputPath) ?? "tmp/tutorial-dataset/tutorial-captures.ndjson",
    adaptationOut: stringOrUndefined(options.adaptationOut),
    acceptanceOut: stringOrUndefined(options.acceptanceOut),
    manifestOut:
      stringOrUndefined(options.manifestOut) ??
      (stringOrUndefined(options.outputPath)
        ? replaceExtension(String(options.outputPath), ".manifest.json")
        : undefined),
    split: stringOrUndefined(options.split),
    acceptanceEvery: toPositiveInt(options.acceptanceEvery, DEFAULT_ACCEPTANCE_EVERY),
    acceptanceMinGroupSize: toPositiveInt(options.acceptanceMinGroupSize, DEFAULT_ACCEPTANCE_MIN_GROUP_SIZE),
    sessionGapMinutes,
    storeVersion: stringOrUndefined(root?.version) ?? null,
    storeUpdatedAt: finiteTimestamp(root?.updatedAt) || null,
    entries
  }
}

export function tutorialCaptureToRecord(capture, exportMetadata) {
  const kind = capture.kind
  if (kind !== "family" && kind !== "operator") {
    throw new Error(`invalid tutorial capture kind: ${kind}`)
  }

  const label = kind === "family" ? capture.expectedFamily : capture.expectedOperator
  if (!label) {
    throw new Error("tutorial capture is missing expected label")
  }

  if (kind === "family" && !VALID_FAMILIES.has(label)) {
    throw new Error(`unknown tutorial family: ${label}`)
  }

  if (kind === "operator" && !VALID_OPERATORS.has(label)) {
    throw new Error(`unknown tutorial operator: ${label}`)
  }

  if (capture.source && !VALID_TUTORIAL_SOURCES.has(capture.source)) {
    throw new Error(`invalid tutorial source: ${capture.source}`)
  }

  const sanitized = sanitizeStrokes(capture.strokes, capture.id || `tutorial-${exportMetadata.captureOrdinal}`)
  if (sanitized.length === 0) {
    throw new Error(`tutorial capture ${capture.id || exportMetadata.captureOrdinal} has no valid strokes`)
  }

  return buildRecord({
    dataset: "tutorial",
    kind,
    label,
    split: exportMetadata.split,
    priority: "tutorial_primary",
    source: capture.source || "trace",
    allowedUses: TUTORIAL_ALLOWED_USES,
    strokes: sanitized,
    metadata: {
      layerRole: "tutorial_primary",
      contractVersion: TUTORIAL_EXPORT_CONTRACT_VERSION,
      captureId: capture.id || `tutorial-${exportMetadata.captureOrdinal}`,
      timestamp: finiteTimestamp(capture.timestamp) || null,
      tutorialPriorityRank: 0,
      exportMode: exportMetadata.exportMode,
      userPartitionKey: exportMetadata.userPartitionKey,
      sessionKey: exportMetadata.sessionKey,
      captureOrdinal: exportMetadata.captureOrdinal,
      sessionOrdinal: exportMetadata.sessionOrdinal,
      inputOrdinal: exportMetadata.inputOrdinal,
      storeVersion: exportMetadata.storeVersion,
      storeUpdatedAt: exportMetadata.storeUpdatedAt,
      baseSnapshot: kind === "operator" ? capture.baseSnapshot ?? null : null,
      operatorContext: kind === "operator" ? capture.operatorContext ?? null : null
    }
  })
}

export function buildTutorialExportManifest(records, options) {
  const bySplit = countBy(records, (record) => record.split)
  const byKind = countBy(records, (record) => record.kind)
  const byLabel = countBy(records, (record) => `${record.kind}:${record.label}`)
  const bySource = countBy(records, (record) => String(record.source))

  return {
    artifactType: "tutorial_export_manifest",
    version: "v1",
    contractVersion: TUTORIAL_EXPORT_CONTRACT_VERSION,
    datasetSchemaVersion: DATASET_SCHEMA_VERSION,
    inputPath: options.inputPath,
    outputPath: options.outputPath,
    outputs: {
      adaptationOut: options.adaptationOut ?? null,
      acceptanceOut: options.acceptanceOut ?? null,
      manifestOut: options.manifestOut ?? null
    },
    exportMode: options.mode,
    lockedSplit: options.split,
    acceptancePolicy:
      options.mode === "locked"
        ? {
            mode: "locked",
            split: options.split
          }
        : {
            mode: "auto_holdout",
            acceptanceEvery: options.acceptanceEvery,
            acceptanceMinGroupSize: options.acceptanceMinGroupSize,
            sessionGapMinutes: options.sessionGapMinutes
          },
    storeVersion: options.storeVersion,
    storeUpdatedAt: options.storeUpdatedAt,
    userPartitionKeys: options.userPartitionKeys,
    sessionKeys: options.sessionKeys,
    counts: {
      total: records.length,
      bySplit,
      byKind,
      byLabel,
      bySource
    }
  }
}

function resolveTutorialSplit(entry, entries, policy) {
  const group = entries.filter(
    (candidate) =>
      candidate.userPartitionKey === entry.userPartitionKey &&
      candidate.capture.kind === entry.capture.kind &&
      expectedLabel(candidate.capture) === expectedLabel(entry.capture)
  )

  if (group.length < policy.acceptanceMinGroupSize) {
    return "adaptation"
  }

  const groupIndex = group.findIndex((candidate) => candidate.captureOrdinal === entry.captureOrdinal)
  if (groupIndex < 0) {
    return "adaptation"
  }

  return (groupIndex + 1) % policy.acceptanceEvery === 0 ? "acceptance_eval" : "adaptation"
}

function assignTutorialSessionMetadata(sortedCaptures, options) {
  let sessionNumber = 1
  let previousTimestamp = null
  let sessionOrdinal = 0

  return sortedCaptures.map(({ capture, inputOrdinal }, index) => {
    const timestamp = finiteTimestamp(capture?.timestamp)

    if (!options.explicitSessionKey && previousTimestamp !== null && timestamp > 0) {
      if (timestamp - previousTimestamp > options.sessionGapMs) {
        sessionNumber += 1
        sessionOrdinal = 0
      }
    }

    if (previousTimestamp === null && options.explicitSessionKey) {
      sessionNumber = 1
    }

    if (timestamp > 0) {
      previousTimestamp = timestamp
    }

    sessionOrdinal += 1

    return {
      capture,
      inputOrdinal,
      userPartitionKey: options.userPartitionKey,
      sessionKey: options.explicitSessionKey ?? `session-${String(sessionNumber).padStart(4, "0")}`,
      captureOrdinal: index + 1,
      sessionOrdinal
    }
  })
}

function expectedLabel(capture) {
  return capture.kind === "family" ? capture.expectedFamily : capture.expectedOperator
}

function countBy(records, keyFn) {
  return records.reduce((accumulator, record) => {
    const key = keyFn(record)
    accumulator[key] = (accumulator[key] ?? 0) + 1
    return accumulator
  }, {})
}

function isMainModule(moduleUrl) {
  const entry = process.argv[1]
  if (!entry) {
    return false
  }

  return pathToFileURL(entry).href === moduleUrl
}

function stringOrUndefined(value) {
  return typeof value === "string" && value.length > 0 ? value : undefined
}

function finiteTimestamp(value) {
  return typeof value === "number" && Number.isFinite(value) ? value : 0
}

function isRecord(value) {
  return Boolean(value) && typeof value === "object" && !Array.isArray(value)
}

function toPositiveInt(value, fallback) {
  const parsed = Number(value)
  if (!Number.isFinite(parsed) || parsed < 1) {
    return fallback
  }
  return Math.trunc(parsed)
}

function replaceExtension(filePath, nextExtension) {
  const normalized = String(filePath)
  const lastDot = normalized.lastIndexOf(".")
  if (lastDot === -1) {
    return `${normalized}${nextExtension}`
  }
  return `${normalized.slice(0, lastDot)}${nextExtension}`
}

async function writeJson(filePath, payload) {
  await ensureParentDir(filePath)
  await writeFile(filePath, `${JSON.stringify(payload, null, 2)}\n`, "utf8")
}

function printHelp() {
  console.log(`Usage:
  node scripts/tutorial-dataset/convert-tutorial-captures.mjs --input exports/tutorial-captures.json --out tmp/tutorial-dataset/tutorial.ndjson

Accepted input shapes:
  1. TutorialCapture[]
  2. { captures: TutorialCapture[] }
  3. TutorialProfileStore-like object with captures[], version, updatedAt

Options:
  --input <path>                       required
  --out <path>                         default tmp/tutorial-dataset/tutorial-captures.ndjson
  --split adaptation|acceptance_eval   locked split mode, writes all records to one split
  --adaptation-out <path>              optional adaptation-only NDJSON output
  --acceptance-out <path>              optional acceptance_eval-only NDJSON output
  --manifest-out <path>                optional manifest JSON output
  --acceptance-every <n>               default 5 in auto holdout mode
  --acceptance-min-group-size <n>      default 5 in auto holdout mode
  --user-partition-key <key>           default local-default
  --session-key <key>                  force a single session key for all captures
  --session-gap-minutes <n>            default 30 for auto session derivation
  --help`)
}
