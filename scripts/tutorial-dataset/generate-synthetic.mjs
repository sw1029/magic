#!/usr/bin/env node

import {
  OPERATOR_TARGETS,
  FAMILY_TARGETS,
  SYNTHETIC_ALLOWED_USES,
  TARGETS,
  applyClosureLeak,
  applyCornerRounding,
  applyJitter,
  applyOvershoot,
  applyPartialTrim,
  asArray,
  buildRecord,
  clamp,
  hashStringToSeed,
  mulberry32,
  parseArgs,
  pickWeighted,
  randomBetween,
  randomInRange,
  retimeStrokes,
  reverseStrokeDirections,
  reverseStrokeOrder,
  rotateScaleTranslate,
  templateToStrokes,
  toInteger,
  toNumber,
  writeNdjson
} from "./common.mjs"
import { resolveSyntheticPriority, SYNTHETIC_PRESETS } from "./synthetic-presets.mjs"

const args = parseArgs(process.argv.slice(2))

if (args.help) {
  printHelp()
  process.exit(0)
}

const presetName = String(args.preset || "bootstrap")
const preset = SYNTHETIC_PRESETS[presetName]

if (!preset) {
  throw new Error(`unknown preset: ${presetName}`)
}

const targetMode = String(args.target || "all")
const countPerLabel = toInteger(args.count, 24)
const split = String(args.split || preset.split)
const outputPath = String(args.out || `tmp/tutorial-dataset/synthetic-${presetName}.ndjson`)
const seed = toInteger(args.seed, hashStringToSeed(`${presetName}:${targetMode}:${countPerLabel}`))

const selectedLabels = selectLabels(targetMode, asArray(args.label))
const rng = mulberry32(seed)
const records = []

for (const label of selectedLabels) {
  const target = TARGETS[label]

  for (let sampleIndex = 0; sampleIndex < countPerLabel; sampleIndex += 1) {
    records.push(generateRecord(target, presetName, preset, sampleIndex, split, rng))
  }
}

await writeNdjson(outputPath, records)

const summary = selectedLabels.map((label) => `${label}:${countPerLabel}`).join(", ")
console.log(
  `wrote ${records.length} synthetic records to ${outputPath} using preset=${presetName}, seed=${seed} (${summary})`
)

function generateRecord(target, presetName, preset, sampleIndex, split, rng) {
  const source = pickWeighted(rng, preset.sourceWeights)
  let strokes = templateToStrokes(target.label, target.strokes)

  if (rng() < preset.reverseStrokeOrderProbability && strokes.length > 1) {
    strokes = reverseStrokeOrder(strokes)
  }

  strokes = reverseStrokeDirections(strokes, rng, preset.reverseStrokeDirectionProbability)

  const rotationRad = (randomInRange(rng, preset.rotationDeg) * Math.PI) / 180
  const scaleX = randomInRange(rng, preset.scale) * randomInRange(rng, preset.stretch)
  const scaleY = randomInRange(rng, preset.scale) / randomInRange(rng, preset.stretch)
  const translateX = randomInRange(rng, preset.translate)
  const translateY = randomInRange(rng, preset.translate)

  strokes = rotateScaleTranslate(strokes, {
    scaleX,
    scaleY,
    rotationRad,
    translateX: translateX + anchorOffset(target, rng, preset.anchorShift),
    translateY
  })

  const targeted = targetedDistortion(target, presetName, rng)
  strokes = rotateScaleTranslate(strokes, targeted.affine)
  strokes = applyOvershoot(strokes, randomInRange(rng, targeted.overshoot))
  strokes = applyJitter(strokes, rng, randomInRange(rng, targeted.jitter))

  if (target.closed) {
    strokes = applyClosureLeak(strokes, randomInRange(rng, targeted.closureLeak))
  }

  strokes = applyPartialTrim(strokes, rng, randomInRange(rng, targeted.partialTrim), minPointsForTarget(target))
  strokes = applyCornerRounding(strokes, randomInRange(rng, targeted.cornerRounding))
  strokes = retimeStrokes(strokes, rng, preset.pointGap)

  return buildRecord({
    dataset: "synthetic",
    kind: target.kind,
    label: target.label,
    split,
    priority: resolveSyntheticPriority(presetName),
    source,
    allowedUses: SYNTHETIC_ALLOWED_USES,
    strokes,
    metadata: {
      layerRole: "synthetic_primary",
      seedHint: sampleIndex,
      preset: presetName,
      syntheticPriority: resolveSyntheticPriority(presetName),
      cues: target.cues,
      confusionWith: target.confusionWith,
      requiresOperator: target.requiresOperator || null,
      transform: {
        base: {
          rotationDeg: degrees(rotationRad),
          scaleX,
          scaleY,
          translateX,
          translateY
        },
        targeted
      }
    }
  })
}

function selectLabels(targetMode, explicitLabels) {
  const familyLabels = Object.keys(FAMILY_TARGETS)
  const operatorLabels = Object.keys(OPERATOR_TARGETS)

  if (explicitLabels.length > 0) {
    const unknown = explicitLabels.filter((label) => !TARGETS[label])
    if (unknown.length > 0) {
      throw new Error(`unknown labels: ${unknown.join(", ")}`)
    }
    return explicitLabels
  }

  if (targetMode === "family") {
    return familyLabels
  }

  if (targetMode === "operator") {
    return operatorLabels
  }

  if (targetMode !== "all") {
    throw new Error(`unknown target mode: ${targetMode}`)
  }

  return [...familyLabels, ...operatorLabels]
}

function anchorOffset(target, rng, range) {
  if (target.kind !== "operator") {
    return 0
  }

  return randomBetween(rng, -range[1], range[1])
}

function minPointsForTarget(target) {
  const maxPoints = Math.max(...target.strokes.map((stroke) => stroke.length))

  if (target.closed) {
    return Math.min(4, maxPoints)
  }

  if (target.label === "life" || target.label === "steel_brace" || target.label === "electric_fork") {
    return Math.min(4, maxPoints)
  }

  if (target.label === "martial_axis") {
    return Math.min(5, maxPoints)
  }

  return Math.min(2, maxPoints)
}

function targetedDistortion(target, presetName, rng) {
  const severity =
    presetName === "hard-negative" ? 1 : presetName === "placement-shift" ? 0.85 : presetName === "tutorial-like" ? 0.75 : 0.55
  const base = {
    affine: {
      scaleX: 1,
      scaleY: 1,
      rotationRad: 0,
      translateX: 0,
      translateY: 0
    },
    overshoot: [0, 0.04 * severity],
    jitter: [0.004 * severity, 0.018 * severity],
    closureLeak: [0, 0.08 * severity],
    partialTrim: [0, 0.08 * severity],
    cornerRounding: [0, 0.2 * severity]
  }

  switch (target.label) {
    case "earth":
      return withAffine(base, {
        scaleX: 1 + 0.22 * severity,
        scaleY: 1 - 0.14 * severity
      })
    case "fire":
      return withAffine(base, {
        scaleX: 1 + 0.14 * severity,
        scaleY: 1 - 0.18 * severity
      })
    case "water":
      return withAffine(base, {
        scaleX: 1 + 0.12 * severity,
        scaleY: 1 - 0.08 * severity
      })
    case "life":
      return withAffine(base, {
        scaleX: 1 - 0.12 * severity,
        scaleY: 1 + 0.08 * severity
      })
    case "wind":
      return withAffine(base, {
        rotationRad: ((randomBetween(rng, -6, 6) * severity) * Math.PI) / 180
      })
    case "electric_fork":
      return withAffine(base, {
        scaleX: 1 - 0.18 * severity,
        rotationRad: ((randomBetween(rng, -8, 8) * severity) * Math.PI) / 180
      })
    case "void_cut":
      return withAffine(base, {
        scaleY: 1 + 0.14 * severity,
        rotationRad: ((randomBetween(rng, -10, 10) * severity) * Math.PI) / 180
      })
    case "steel_brace":
      return withAffine(base, {
        scaleX: 1 + 0.1 * severity,
        scaleY: 1 - 0.16 * severity
      })
    case "ice_bar":
      return withAffine(base, {
        scaleX: clamp(1 - 0.18 * severity, 0.74, 1.2)
      })
    case "soul_dot":
      return withAffine(base, {
        scaleX: 1 + 0.08 * severity,
        scaleY: 1 - 0.08 * severity
      })
    case "martial_axis":
      return withAffine(base, {
        rotationRad: ((randomBetween(rng, -5, 5) * severity) * Math.PI) / 180
      })
    default:
      return base
  }
}

function withAffine(base, override) {
  return {
    ...base,
    affine: {
      ...base.affine,
      ...override
    }
  }
}

function degrees(radians) {
  return Number(((radians * 180) / Math.PI).toFixed(4))
}

function printHelp() {
  console.log(`Usage:
  node scripts/tutorial-dataset/generate-synthetic.mjs --target all --preset tutorial-like --count 24 --out tmp/tutorial-dataset/synthetic.ndjson

Options:
  --target family|operator|all
  --label <label>           repeatable, limits generation to specific labels
  --preset bootstrap|tutorial-like|hard-negative|placement-shift
  --count <n>               samples per label, default 24
  --split <name>            default comes from preset
  --seed <n>
  --out <path>
  --help`)
}
