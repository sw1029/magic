#!/usr/bin/env node

import { writeFile } from "node:fs/promises"

import { ensureParentDir, loadPublicDatasetManifest, parseArgs } from "./common.mjs"
import {
  buildTinyMlDatasetSplitManifest,
  buildTinyMlFeatureSpec,
  validateTinyMlArtifacts
} from "./ml-contract.mjs"

const args = parseArgs(process.argv.slice(2))

if (args.help) {
  printHelp()
  process.exit(0)
}

const splitOut = String(args["split-out"] || "artifacts/ml/dataset-split-v1.json")
const featureOut = String(args["feature-out"] || "artifacts/ml/feature-spec-v1.json")
const publicManifest = await loadPublicDatasetManifest()

const splitManifest = buildTinyMlDatasetSplitManifest({
  publicManifest,
  splitArtifactPath: splitOut
})
const featureSpec = buildTinyMlFeatureSpec({
  splitArtifactPath: splitOut
})

validateTinyMlArtifacts({ splitManifest, featureSpec })

await writeJson(splitOut, splitManifest)
await writeJson(featureOut, featureSpec)

console.log(`wrote tiny ML dataset split manifest to ${splitOut}`)
console.log(`wrote tiny ML feature spec to ${featureOut}`)

async function writeJson(filePath, payload) {
  await ensureParentDir(filePath)
  await writeFile(filePath, `${JSON.stringify(payload, null, 2)}\n`, "utf8")
}

function printHelp() {
  console.log(`Usage:
  node scripts/tutorial-dataset/build-ml-baseline-manifests.mjs

Options:
  --split-out <path>        default artifacts/ml/dataset-split-v1.json
  --feature-out <path>      default artifacts/ml/feature-spec-v1.json
  --help`)
}
