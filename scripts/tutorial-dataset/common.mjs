import { createHash } from "node:crypto"
import { mkdir, readdir, readFile, rm, stat, writeFile } from "node:fs/promises"
import { createWriteStream, existsSync } from "node:fs"
import path from "node:path"
import { spawn } from "node:child_process"
import { fileURLToPath } from "node:url"

export const DATASET_SCHEMA_VERSION = "tutorial-hybrid-v1"

export const FAMILY_TARGETS = {
  wind: {
    label: "wind",
    kind: "family",
    closed: false,
    confusionWith: ["invalid"],
    cues: ["stroke_count", "parallelism", "openness"],
    strokes: [
      [
        [-0.7, -0.35],
        [0.7, -0.35]
      ],
      [
        [-0.7, 0],
        [0.7, 0]
      ],
      [
        [-0.7, 0.35],
        [0.7, 0.35]
      ]
    ]
  },
  earth: {
    label: "earth",
    kind: "family",
    closed: true,
    confusionWith: ["fire"],
    cues: ["closure", "quad_structure", "fill_ratio"],
    strokes: [
      [
        [-0.65, 0.55],
        [-0.35, -0.5],
        [0.35, -0.5],
        [0.65, 0.55],
        [-0.65, 0.55]
      ]
    ]
  },
  fire: {
    label: "fire",
    kind: "family",
    closed: true,
    confusionWith: ["earth"],
    cues: ["closure", "triangle_apex", "upward_impression"],
    strokes: [
      [
        [0, -0.72],
        [0.72, 0.62],
        [-0.72, 0.62],
        [0, -0.72]
      ]
    ]
  },
  water: {
    label: "water",
    kind: "family",
    closed: true,
    confusionWith: ["life"],
    cues: ["closure", "circularity", "smoothness"],
    strokes: [circleCoords(28, 0.68, 0, 0)]
  },
  life: {
    label: "life",
    kind: "family",
    closed: false,
    confusionWith: ["water"],
    cues: ["branch_cue", "rooted_axis", "openness"],
    strokes: [
      [
        [0, 0.72],
        [0, 0.08],
        [-0.42, -0.56],
        [0, 0.08],
        [0.42, -0.56]
      ]
    ]
  }
}

export const OPERATOR_TARGETS = {
  steel_brace: {
    label: "steel_brace",
    kind: "operator",
    closed: false,
    confusionWith: ["partial_box"],
    cues: ["open_rectangular_brace", "aspect_ratio"],
    preferredAnchorZones: ["right", "lower_right", "upper_right"],
    strokes: [
      [
        [0.42, -0.72],
        [-0.4, -0.72],
        [-0.4, 0.72],
        [0.42, 0.72]
      ]
    ]
  },
  electric_fork: {
    label: "electric_fork",
    kind: "operator",
    closed: false,
    confusionWith: ["void_cut"],
    cues: ["zigzag", "fork_direction", "corner_count"],
    preferredAnchorZones: ["upper_right", "upper", "right"],
    strokes: [
      [
        [-0.52, 0.58],
        [-0.08, 0.02],
        [-0.44, 0.02],
        [0.06, -0.66],
        [0.5, 0.02]
      ]
    ]
  },
  ice_bar: {
    label: "ice_bar",
    kind: "operator",
    closed: false,
    confusionWith: ["partial_stroke"],
    cues: ["straightness", "horizontal_axis", "minimum_scale"],
    preferredAnchorZones: ["core", "left", "right"],
    strokes: [[[-0.78, 0], [0.78, 0]]]
  },
  soul_dot: {
    label: "soul_dot",
    kind: "operator",
    closed: true,
    confusionWith: ["noise_dot"],
    cues: ["small_circle", "closure", "smallness"],
    preferredAnchorZones: ["core", "upper_left", "upper_right", "lower_left", "lower_right"],
    strokes: [circleCoords(18, 0.2, 0, 0)]
  },
  void_cut: {
    label: "void_cut",
    kind: "operator",
    closed: false,
    confusionWith: ["electric_fork"],
    cues: ["ascending_slash", "angle", "single_axis"],
    preferredAnchorZones: ["upper_right", "core", "lower_left"],
    strokes: [[[-0.68, 0.68], [0.68, -0.68]]]
  },
  martial_axis: {
    label: "martial_axis",
    kind: "operator",
    closed: false,
    confusionWith: ["void_cut"],
    cues: ["vertical_axis", "crossbar"],
    preferredAnchorZones: ["lower_right", "core", "right"],
    requiresOperator: "void_cut",
    strokes: [
      [
        [0, -0.74],
        [0, 0.74],
        [0, 0],
        [0.5, 0],
        [-0.5, 0]
      ]
    ]
  }
}

export const TARGETS = {
  ...FAMILY_TARGETS,
  ...OPERATOR_TARGETS
}

export const VALID_FAMILIES = new Set(Object.keys(FAMILY_TARGETS))
export const VALID_OPERATORS = new Set(Object.keys(OPERATOR_TARGETS))
export const VALID_TUTORIAL_SOURCES = new Set(["trace", "recall", "variation"])

export const PUBLIC_ALLOWED_USES = [
  "normalization_regression",
  "stroke_encoder_pretrain",
  "denoising_prior",
  "sequence_representation"
]

export const PUBLIC_FORBIDDEN_USES = [
  "direct_family_classifier_training",
  "direct_operator_classifier_training",
  "semantic_label_override"
]

export const SYNTHETIC_ALLOWED_USES = [
  "prototype_bootstrap",
  "operator_bootstrap",
  "hard_negative_bootstrap",
  "normalization_regression"
]

export const TUTORIAL_ALLOWED_USES = [
  "user_adaptation",
  "prototype_bank",
  "rerank_calibration",
  "acceptance_eval"
]

export function parseArgs(argv) {
  const args = {}

  for (let index = 0; index < argv.length; index += 1) {
    const token = argv[index]

    if (!token.startsWith("--")) {
      continue
    }

    const key = token.slice(2)
    const next = argv[index + 1]
    const value = next && !next.startsWith("--") ? next : true

    if (value !== true) {
      index += 1
    }

    if (args[key] === undefined) {
      args[key] = value
      continue
    }

    if (Array.isArray(args[key])) {
      args[key].push(value)
      continue
    }

    args[key] = [args[key], value]
  }

  return args
}

export function asArray(value) {
  if (value === undefined) {
    return []
  }

  return Array.isArray(value) ? value : [value]
}

export function toNumber(value, fallback) {
  if (value === undefined || value === true) {
    return fallback
  }

  const parsed = Number(value)
  return Number.isFinite(parsed) ? parsed : fallback
}

export function toInteger(value, fallback) {
  return Math.trunc(toNumber(value, fallback))
}

export function mulberry32(seed) {
  let state = seed >>> 0

  return () => {
    state += 0x6d2b79f5
    let result = Math.imul(state ^ (state >>> 15), 1 | state)
    result ^= result + Math.imul(result ^ (result >>> 7), 61 | result)
    return ((result ^ (result >>> 14)) >>> 0) / 4294967296
  }
}

export function hashStringToSeed(value) {
  let hash = 2166136261

  for (const char of value) {
    hash ^= char.charCodeAt(0)
    hash = Math.imul(hash, 16777619)
  }

  return hash >>> 0
}

export function randomBetween(rng, min, max) {
  return min + (max - min) * rng()
}

export function randomInRange(rng, range) {
  return randomBetween(rng, range[0], range[1])
}

export function clamp(value, min, max) {
  return Math.max(min, Math.min(max, value))
}

export function pickWeighted(rng, weightedEntries) {
  const entries = Array.isArray(weightedEntries) ? weightedEntries : Object.entries(weightedEntries)
  const total = entries.reduce((sum, [, weight]) => sum + weight, 0)
  const threshold = rng() * total
  let cursor = 0

  for (const [value, weight] of entries) {
    cursor += weight
    if (threshold <= cursor) {
      return value
    }
  }

  return entries[entries.length - 1][0]
}

export function ensurePoint(point, fallbackT = 0) {
  return {
    x: Number(point.x),
    y: Number(point.y),
    t: Number.isFinite(Number(point.t)) ? Number(point.t) : fallbackT
  }
}

export function sanitizeStrokes(strokes, prefix = "stroke") {
  if (!Array.isArray(strokes)) {
    return []
  }

  return strokes
    .map((stroke, strokeIndex) => {
      const points = Array.isArray(stroke.points)
        ? stroke.points.map((point, pointIndex) => ensurePoint(point, pointIndex * 16))
        : []

      return {
        id: stroke.id || `${prefix}-${strokeIndex + 1}`,
        points
      }
    })
    .filter((stroke) => stroke.points.length >= 2)
}

export function cloneStrokes(strokes) {
  return strokes.map((stroke) => ({
    id: stroke.id,
    points: stroke.points.map((point) => ({ ...point }))
  }))
}

export function templateToStrokes(label, coords) {
  return coords.map((strokeCoords, strokeIndex) => ({
    id: `${label}-${strokeIndex + 1}`,
    points: strokeCoords.map(([x, y], pointIndex) => ({
      x,
      y,
      t: pointIndex * 16
    }))
  }))
}

export function rotateScaleTranslate(strokes, { scaleX, scaleY, rotationRad, translateX, translateY }) {
  const cos = Math.cos(rotationRad)
  const sin = Math.sin(rotationRad)

  return strokes.map((stroke) => ({
    ...stroke,
    points: stroke.points.map((point) => {
      const scaledX = point.x * scaleX
      const scaledY = point.y * scaleY
      const x = scaledX * cos - scaledY * sin + translateX
      const y = scaledX * sin + scaledY * cos + translateY

      return {
        ...point,
        x,
        y
      }
    })
  }))
}

export function reverseStrokeOrder(strokes) {
  return strokes
    .slice()
    .reverse()
    .map((stroke, index) => ({
      id: `${stroke.id}-reordered-${index + 1}`,
      points: stroke.points.map((point) => ({ ...point }))
    }))
}

export function reverseStrokeDirections(strokes, rng, probability = 0.5) {
  return strokes.map((stroke) => {
    if (rng() > probability) {
      return {
        ...stroke,
        points: stroke.points.map((point) => ({ ...point }))
      }
    }

    return {
      ...stroke,
      points: stroke.points
        .slice()
        .reverse()
        .map((point) => ({ ...point }))
    }
  })
}

export function applyJitter(strokes, rng, amplitude) {
  if (amplitude <= 0) {
    return cloneStrokes(strokes)
  }

  return strokes.map((stroke) => ({
    ...stroke,
    points: stroke.points.map((point, pointIndex) => {
      if (pointIndex === 0 || pointIndex === stroke.points.length - 1) {
        return { ...point }
      }

      return {
        ...point,
        x: point.x + randomBetween(rng, -amplitude, amplitude),
        y: point.y + randomBetween(rng, -amplitude, amplitude)
      }
    })
  }))
}

export function applyOvershoot(strokes, amount) {
  if (amount <= 0) {
    return cloneStrokes(strokes)
  }

  return strokes.map((stroke) => {
    if (stroke.points.length < 2) {
      return {
        ...stroke,
        points: stroke.points.map((point) => ({ ...point }))
      }
    }

    const points = stroke.points.map((point) => ({ ...point }))
    const head = extendEndpoint(points[0], points[1], amount)
    const tail = extendEndpoint(points[points.length - 1], points[points.length - 2], amount)
    points[0] = head
    points[points.length - 1] = tail
    return {
      ...stroke,
      points
    }
  })
}

function extendEndpoint(endpoint, neighbor, amount) {
  const dx = endpoint.x - neighbor.x
  const dy = endpoint.y - neighbor.y
  const length = Math.hypot(dx, dy) || 1

  return {
    ...endpoint,
    x: endpoint.x + (dx / length) * amount,
    y: endpoint.y + (dy / length) * amount
  }
}

export function applyClosureLeak(strokes, amount) {
  if (amount <= 0 || strokes.length === 0) {
    return cloneStrokes(strokes)
  }

  return strokes.map((stroke, strokeIndex) => {
    if (strokeIndex !== 0 || stroke.points.length < 3) {
      return {
        ...stroke,
        points: stroke.points.map((point) => ({ ...point }))
      }
    }

    const points = stroke.points.map((point) => ({ ...point }))
    const firstPoint = points[0]
    const lastIndex = points.length - 1
    const previousPoint = points[lastIndex - 1]
    const dx = firstPoint.x - previousPoint.x
    const dy = firstPoint.y - previousPoint.y
    const length = Math.hypot(dx, dy) || 1

    points[lastIndex] = {
      ...points[lastIndex],
      x: firstPoint.x - (dx / length) * amount,
      y: firstPoint.y - (dy / length) * amount
    }

    return {
      ...stroke,
      points
    }
  })
}

export function applyCornerRounding(strokes, factor) {
  if (factor <= 0) {
    return cloneStrokes(strokes)
  }

  return strokes.map((stroke) => ({
    ...stroke,
    points: stroke.points.map((point, pointIndex) => {
      if (pointIndex === 0 || pointIndex === stroke.points.length - 1) {
        return { ...point }
      }

      const previousPoint = stroke.points[pointIndex - 1]
      const nextPoint = stroke.points[pointIndex + 1]
      return {
        ...point,
        x: point.x * (1 - factor) + ((previousPoint.x + nextPoint.x) / 2) * factor,
        y: point.y * (1 - factor) + ((previousPoint.y + nextPoint.y) / 2) * factor
      }
    })
  }))
}

export function applyPartialTrim(strokes, rng, trimRatio, minPoints = 2) {
  if (trimRatio <= 0) {
    return cloneStrokes(strokes)
  }

  return strokes.map((stroke) => {
    if (stroke.points.length <= minPoints) {
      return {
        ...stroke,
        points: stroke.points.map((point) => ({ ...point }))
      }
    }

    const availableTrim = Math.max(0, stroke.points.length - minPoints)
    const maxTrim = Math.min(availableTrim, Math.max(1, Math.floor((stroke.points.length - minPoints) * trimRatio)))
    const trimStart = Math.min(maxTrim, Math.floor(rng() * (maxTrim + 1)))
    const trimEnd = Math.min(maxTrim - trimStart, Math.floor(rng() * (maxTrim - trimStart + 1)))
    const kept = stroke.points.slice(trimStart, stroke.points.length - trimEnd)

    return {
      ...stroke,
      points: (kept.length >= minPoints ? kept : stroke.points.slice(0, minPoints)).map((point) => ({ ...point }))
    }
  })
}

export function retimeStrokes(strokes, rng, pointGapRange = [10, 24]) {
  return strokes.map((stroke) => {
    let cursor = 0

    return {
      ...stroke,
      points: stroke.points.map((point) => {
        const nextPoint = {
          ...point,
          t: cursor
        }

        cursor += Math.round(randomInRange(rng, pointGapRange))
        return nextPoint
      })
    }
  })
}

export function getBounds(strokes) {
  const points = strokes.flatMap((stroke) => stroke.points)
  if (points.length === 0) {
    return {
      minX: 0,
      maxX: 0,
      minY: 0,
      maxY: 0,
      width: 1,
      height: 1
    }
  }

  const xs = points.map((point) => point.x)
  const ys = points.map((point) => point.y)
  const minX = Math.min(...xs)
  const maxX = Math.max(...xs)
  const minY = Math.min(...ys)
  const maxY = Math.max(...ys)

  return {
    minX,
    maxX,
    minY,
    maxY,
    width: Math.max(maxX - minX, 1e-6),
    height: Math.max(maxY - minY, 1e-6)
  }
}

export function normalizeStrokes(strokes) {
  const bounds = getBounds(strokes)
  const centerX = (bounds.minX + bounds.maxX) / 2
  const centerY = (bounds.minY + bounds.maxY) / 2
  const scale = Math.max(bounds.width, bounds.height) / 2 || 1

  return strokes.map((stroke) => ({
    ...stroke,
    points: stroke.points.map((point) => ({
      ...point,
      x: (point.x - centerX) / scale,
      y: (point.y - centerY) / scale
    }))
  }))
}

export function toSerializableStrokes(strokes) {
  return strokes.map((stroke) =>
    stroke.points.map((point) => ({
      x: roundNumber(point.x),
      y: roundNumber(point.y),
      t: Math.round(point.t)
    }))
  )
}

export function buildRecord({
  dataset,
  kind,
  label = null,
  split,
  priority,
  source = null,
  allowedUses,
  forbiddenUses = [],
  strokes,
  metadata = {}
}) {
  const sanitized = sanitizeStrokes(strokes, label || dataset)
  const normalized = normalizeStrokes(sanitized)

  return {
    schemaVersion: DATASET_SCHEMA_VERSION,
    dataset,
    kind,
    label,
    split,
    priority,
    source,
    usage: {
      allowed: allowedUses,
      forbidden: forbiddenUses
    },
    strokes: toSerializableStrokes(sanitized),
    normalizedStrokes: toSerializableStrokes(normalized),
    metadata
  }
}

export async function ensureParentDir(filePath) {
  await mkdir(path.dirname(path.resolve(filePath)), { recursive: true })
}

export async function ensureDir(dirPath) {
  await mkdir(path.resolve(dirPath), { recursive: true })
}

export async function writeNdjson(filePath, records) {
  await ensureParentDir(filePath)
  const body = `${records.map((record) => JSON.stringify(record)).join("\n")}\n`
  await writeFile(filePath, body, "utf8")
}

export async function createNdjsonWriter(filePath) {
  await ensureParentDir(filePath)
  const stream = createWriteStream(filePath, { encoding: "utf8" })

  return {
    async write(record) {
      await new Promise((resolve, reject) => {
        stream.write(`${JSON.stringify(record)}\n`, (error) => {
          if (error) {
            reject(error)
            return
          }

          resolve(undefined)
        })
      })
    },
    async close() {
      await new Promise((resolve, reject) => {
        stream.end((error) => {
          if (error) {
            reject(error)
            return
          }

          resolve(undefined)
        })
      })
    }
  }
}

export async function loadPublicDatasetManifest() {
  const manifestPath = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "manifest/public-datasets.json")
  return JSON.parse(await readFile(manifestPath, "utf8"))
}

export async function downloadToFile(url, outputPath) {
  await ensureParentDir(outputPath)
  let buffer

  try {
    const response = await fetch(url, {
      headers: {
        "user-agent": "magic-recognizer-dataset-helper/0.1"
      }
    })

    if (!response.ok) {
      throw new Error(`download failed: ${url} -> ${response.status} ${response.statusText}`)
    }

    buffer = Buffer.from(await response.arrayBuffer())
    await writeFile(outputPath, buffer)
  } catch (error) {
    await rm(outputPath, { force: true }).catch(() => {})
    await downloadViaCurl(url, outputPath)
    buffer = await readFile(outputPath)
  }

  const sha256 = createHash("sha256").update(buffer).digest("hex")

  return {
    filePath: outputPath,
    bytes: buffer.byteLength,
    sha256
  }
}

export async function resolveArtifactUrlFromPage(pageUrl, artifactName) {
  const html = await fetchText(pageUrl)
  const escaped = artifactName.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")
  const match =
    html.match(new RegExp(`<a[^>]+href="([^"]+)"[^>]*>\\s*${escaped}\\s*</a>`, "i")) ??
    html.match(new RegExp(`<a[^>]+href='([^']+)'[^>]*>\\s*${escaped}\\s*</a>`, "i")) ??
    html.match(new RegExp(`href="([^"]*${escaped}[^"]*)"`, "i")) ??
    html.match(new RegExp(`href='([^']*${escaped}[^']*)'`, "i"))

  if (!match) {
    throw new Error(`artifact link not found on page: ${artifactName} @ ${pageUrl}`)
  }

  return new URL(match[1], pageUrl).toString()
}

export async function extractArchive(archivePath, targetDir) {
  await ensureDir(targetDir)
  const extension = path.extname(archivePath).toLowerCase()

  if (extension === ".zip") {
    await runCommand("unzip", ["-o", "-q", archivePath, "-d", targetDir])
    return
  }

  if (archivePath.endsWith(".tar.gz") || archivePath.endsWith(".tgz")) {
    await runCommand("tar", ["-xzf", archivePath, "-C", targetDir])
    return
  }

  throw new Error(`unsupported archive format: ${archivePath}`)
}

export async function runCommand(command, args, options = {}) {
  await new Promise((resolve, reject) => {
    const child = spawn(command, args, {
      stdio: "inherit",
      ...options
    })

    child.on("error", reject)
    child.on("exit", (code) => {
      if (code === 0) {
        resolve(undefined)
        return
      }

      reject(new Error(`${command} exited with code ${code}`))
    })
  })
}

export async function runCommandCapture(command, args, options = {}) {
  return await new Promise((resolve, reject) => {
    let stdout = ""
    let stderr = ""
    const child = spawn(command, args, {
      stdio: ["ignore", "pipe", "pipe"],
      ...options
    })

    child.stdout.on("data", (chunk) => {
      stdout += chunk.toString()
    })
    child.stderr.on("data", (chunk) => {
      stderr += chunk.toString()
    })
    child.on("error", reject)
    child.on("exit", (code) => {
      if (code === 0) {
        resolve({ stdout, stderr })
        return
      }

      reject(new Error(`${command} exited with code ${code}: ${stderr.trim()}`))
    })
  })
}

export async function pathExists(targetPath) {
  return existsSync(path.resolve(targetPath))
}

async function fetchText(url) {
  try {
    const response = await fetch(url, {
      headers: {
        "user-agent": "magic-recognizer-dataset-helper/0.1"
      }
    })

    if (!response.ok) {
      throw new Error(`page fetch failed: ${url} -> ${response.status} ${response.statusText}`)
    }

    return await response.text()
  } catch {
    const result = await runCommandCapture("curl", ["-fsSL", "--insecure", url])
    return result.stdout
  }
}

async function downloadViaCurl(url, outputPath) {
  await runCommand("curl", ["-fsSL", "--insecure", url, "-o", outputPath])
}

export async function collectFiles(inputPath, extensions = []) {
  const resolved = path.resolve(inputPath)
  const info = await stat(resolved)

  if (info.isFile()) {
    return [resolved]
  }

  const entries = await readdir(resolved, { withFileTypes: true })
  const nested = await Promise.all(
    entries.map((entry) => collectFiles(path.join(resolved, entry.name), extensions).catch(() => []))
  )

  return nested
    .flat()
    .filter((filePath) => extensions.length === 0 || extensions.includes(path.extname(filePath).toLowerCase()))
}

export async function readJsonOrNdjson(filePath) {
  const raw = await readFile(filePath, "utf8")
  const trimmed = raw.trim()

  if (trimmed.startsWith("[")) {
    return JSON.parse(trimmed)
  }

  if (trimmed.startsWith("{")) {
    return [JSON.parse(trimmed)]
  }

  return raw
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter(Boolean)
    .map((line) => JSON.parse(line))
}

export function parseXmlAttributes(fragment) {
  const attributes = {}

  for (const match of fragment.matchAll(/([A-Za-z_:][-A-Za-z0-9_:.]*)="([^"]*)"/g)) {
    attributes[match[1]] = match[2]
  }

  return attributes
}

export function roundNumber(value) {
  return Number(value.toFixed(6))
}

function circleCoords(count, radius, cx, cy) {
  const points = []

  for (let index = 0; index <= count; index += 1) {
    const angle = (index / count) * Math.PI * 2
    points.push([cx + Math.cos(angle) * radius, cy + Math.sin(angle) * radius])
  }

  return points
}
