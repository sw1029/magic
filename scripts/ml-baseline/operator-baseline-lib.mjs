import { spawn } from "node:child_process";
import { mkdir, rm, writeFile } from "node:fs/promises";
import path from "node:path";
import { createRequire } from "node:module";
import { fileURLToPath } from "node:url";

import {
  FAMILY_TARGETS,
  OPERATOR_TARGETS,
  applyCornerRounding,
  applyJitter,
  applyOvershoot,
  applyPartialTrim,
  clamp,
  hashStringToSeed,
  mulberry32,
  randomBetween,
  retimeStrokes,
  reverseStrokeDirections,
  rotateScaleTranslate,
  templateToStrokes
} from "../tutorial-dataset/common.mjs";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, "..", "..");
const require = createRequire(import.meta.url);

const ANCHOR_ZONES = [
  "upper_left",
  "upper",
  "upper_right",
  "left",
  "core",
  "right",
  "lower_left",
  "lower",
  "lower_right"
];

const OPERATOR_SCALE_RANGES = {
  steel_brace: [0.16, 0.48],
  electric_fork: [0.14, 0.4],
  ice_bar: [0.28, 0.52],
  soul_dot: [0.03, 0.16],
  void_cut: [0.14, 0.48],
  martial_axis: [0.14, 0.42]
};

const HARD_PAIR_IDS = {
  void_cut: "void_cut__electric_fork",
  electric_fork: "void_cut__electric_fork",
  ice_bar: "ice_bar__partial_stroke",
  steel_brace: "steel_brace__open_box_like",
  martial_axis: "martial_axis__blocked_by_void_cut"
};

const SCENARIOS_BY_SPLIT = {
  train: [
    { kind: "canonical", operators: ["steel_brace", "electric_fork", "ice_bar", "soul_dot", "void_cut"], count: 28 },
    { kind: "valid_with_stack", operators: ["martial_axis"], count: 18 },
    { kind: "hard_pair", operators: ["void_cut", "electric_fork"], count: 40 },
    { kind: "off_anchor", operators: ["steel_brace", "electric_fork", "void_cut", "martial_axis"], count: 14 },
    { kind: "wrong_scale", operators: ["steel_brace", "electric_fork", "ice_bar", "void_cut"], count: 14 },
    { kind: "partial_like", operators: ["ice_bar"], count: 24 },
    { kind: "open_box_like", operators: ["steel_brace"], count: 24 },
    { kind: "blocked", operators: ["martial_axis"], count: 28 }
  ],
  eval: [
    { kind: "canonical", operators: ["steel_brace", "electric_fork", "ice_bar", "soul_dot", "void_cut"], count: 12 },
    { kind: "valid_with_stack", operators: ["martial_axis"], count: 10 },
    { kind: "hard_pair", operators: ["void_cut", "electric_fork"], count: 18 },
    { kind: "off_anchor", operators: ["electric_fork", "void_cut", "martial_axis"], count: 8 },
    { kind: "wrong_scale", operators: ["ice_bar", "void_cut"], count: 8 },
    { kind: "partial_like", operators: ["ice_bar"], count: 10 },
    { kind: "open_box_like", operators: ["steel_brace"], count: 10 },
    { kind: "blocked", operators: ["martial_axis"], count: 12 }
  ],
  hard_negative_eval: [
    { kind: "hard_pair", operators: ["void_cut", "electric_fork"], count: 22 },
    { kind: "off_anchor", operators: ["electric_fork", "void_cut", "martial_axis"], count: 14 },
    { kind: "wrong_scale", operators: ["ice_bar", "void_cut", "electric_fork"], count: 14 },
    { kind: "partial_like", operators: ["ice_bar"], count: 18 },
    { kind: "open_box_like", operators: ["steel_brace"], count: 18 },
    { kind: "blocked", operators: ["martial_axis"], count: 20 }
  ]
};

export async function buildOperatorBaselineDataset(options = {}) {
  const seed = Number.isFinite(Number(options.seed))
    ? Number(options.seed)
    : hashStringToSeed("operator-baseline-v1");
  const rng = mulberry32(seed);
  const runtime = await loadOverlayRuntime(options.runtimeCacheDir);
  const rows = [];
  const examples = [];

  for (const split of ["train", "eval", "hard_negative_eval"]) {
    for (const scenario of SCENARIOS_BY_SPLIT[split]) {
      for (const operator of scenario.operators) {
        const targetCount = scenario.count;
        let collected = 0;
        let attempts = 0;

        while (collected < targetCount && attempts < targetCount * 90) {
          attempts += 1;
          const example = generateExample({
            exampleId: `${split}:${scenario.kind}:${operator}:${collected}`,
            split,
            scenarioKind: scenario.kind,
            label: operator,
            rng,
            runtime
          });

          if (!acceptExample(example)) {
            continue;
          }

          examples.push(toExampleSummary(example));
          rows.push(...example.rows);
          collected += 1;
        }

        if (collected < targetCount) {
          throw new Error(
            `failed to synthesize enough operator examples for split=${split}, scenario=${scenario.kind}, operator=${operator}: ${collected}/${targetCount}`
          );
        }
      }
    }
  }

  return {
    seed,
    rows,
    summary: summarizeDataset(examples)
  };
}

export async function writeOperatorBaselineDataset(options = {}) {
  const outputPath = path.resolve(repoRoot, options.out || "tmp/ml-baseline/operator-baseline-rows.ndjson");
  const summaryPath = path.resolve(repoRoot, options.summaryOut || "tmp/ml-baseline/operator-baseline-summary.json");
  const dataset = await buildOperatorBaselineDataset(options);

  await mkdir(path.dirname(outputPath), { recursive: true });
  await writeFile(outputPath, `${dataset.rows.map((row) => JSON.stringify(row)).join("\n")}\n`, "utf8");
  await writeFile(summaryPath, `${JSON.stringify(dataset.summary, null, 2)}\n`, "utf8");

  return {
    outputPath,
    summaryPath,
    rowCount: dataset.rows.length,
    summary: dataset.summary,
    seed: dataset.seed
  };
}

function generateExample(params) {
  const { exampleId, split, scenarioKind, label, rng, runtime } = params;
  const { createOverlayReferenceFrame, recognizeOverlayStroke } = runtime;
  const baseSession = makeBaseSession(rng);
  const referenceFrame = createOverlayReferenceFrame(baseSession);
  const existingOperators = sampleExistingOperators(label, scenarioKind, rng);
  const stroke = buildOperatorStroke({
    label,
    scenarioKind,
    referenceFrame,
    rng
  });
  const overlaySession = makeSession([stroke]);
  const recognition = recognizeOverlayStroke(stroke, {
    referenceFrame,
    existingOperators,
    overlaySession
  });
  const candidates = Array.isArray(recognition.candidates) ? recognition.candidates.slice(0, 3) : [];
  const topScore = candidates[0]?.score ?? 0;
  const canonicalRow = candidates.find((candidate) => candidate.operator === label);
  const strongGate = !canonicalRow?.blockedBy && (canonicalRow?.anchorScore ?? 0) >= 0.34 && (canonicalRow?.scaleScore ?? 0) >= 0.34;

  const rows = candidates.map((candidate, candidateRank) => {
    const gateStrength = roundMetric(Math.min(candidate.anchorScore ?? 0, candidate.scaleScore ?? 0));
    const hardPairId = resolveHardPairId(label, scenarioKind, candidate.operator);
    const placement = candidate.placementAnchorZoneId ?? "core";
    const isCanonicalCandidate = candidate.operator === label;
    const targetPairwiseDelta = resolvePairwiseTarget({
      scenarioKind,
      label,
      candidate,
      canonicalRow,
      candidates
    });
    const targetFalsePositiveSuppression = resolveSuppressionTarget({
      scenarioKind,
      candidate,
      isCanonicalCandidate,
      strongGate
    });
    const topCandidate = candidates[0];
    const topIsCanonical = topCandidate?.operator === label;
    const allowRecognition = scenarioAllowsRecognition(scenarioKind, label);
    const targetOperatorConfidence =
      candidateRank === 0 && allowRecognition && topIsCanonical && strongGate ? 1 : candidateRank === 0 ? 0 : null;

    return {
      exampleId,
      split,
      scenarioKind,
      canonicalOperator: label,
      heuristicTopOperator: candidates[0]?.operator ?? null,
      heuristicStatus: recognition.status,
      candidateRank,
      operatorId: candidate.operator,
      hardPairId,
      heuristicScore: roundMetric(candidate.score),
      baseScore: roundMetric(candidate.baseScore ?? candidate.score),
      templateDistance: roundMetric(candidate.templateDistance ?? 0),
      shapeConfidence: roundMetric(candidate.shapeConfidence ?? 0),
      topScoreGap: roundMetric(Math.max(topScore - candidate.score, 0)),
      blockedByFlag: candidate.blockedBy ? 1 : 0,
      blockedByOperator: candidate.blockedBy ?? "none",
      anchorZoneId: candidate.anchorZoneId ?? "core",
      placement,
      anchorScore: roundMetric(candidate.anchorScore ?? 0),
      scaleScore: roundMetric(candidate.scaleScore ?? 0),
      gateStrength,
      angleRadians: roundMetric(candidate.angleRadians ?? 0),
      scaleRatio: roundMetric(candidate.scaleRatio ?? 0),
      straightness: roundMetric(candidate.straightness ?? 0),
      corners: roundMetric(candidate.corners ?? 0),
      closure: roundMetric(candidate.closure ?? 0),
      stackIndex: candidate.stackIndex ?? existingOperators.length,
      existingOperatorsCount: existingOperators.length,
      existingOperatorsMask: [...existingOperators],
      hasVoidCutInStack: existingOperators.includes("void_cut") ? 1 : 0,
      isCanonicalCandidate,
      topCandidateFlag: candidateRank === 0,
      allowRecognition,
      targetPairwiseDelta,
      targetFalsePositiveSuppression,
      targetOperatorConfidence,
      sampleWeight: resolveSampleWeight(scenarioKind)
    };
  });

  return {
    exampleId,
    split,
    scenarioKind,
    label,
    recognition,
    rows,
    canonicalRow,
    existingOperators
  };
}

function acceptExample(example) {
  const canonicalRow = example.canonicalRow;

  if (!canonicalRow) {
    return false;
  }

  switch (example.scenarioKind) {
    case "canonical":
    case "valid_with_stack":
      return (
        example.rows[0]?.operatorId === example.label &&
        canonicalRow.blockedBy !== "void_cut" &&
        canonicalRow.anchorScore >= 0.4 &&
        canonicalRow.scaleScore >= 0.4
      );
    case "hard_pair": {
      const pairRows = example.rows.filter(
        (row) => row.operatorId === "void_cut" || row.operatorId === "electric_fork"
      );
      const canonicalPairRow = pairRows.find((row) => row.operatorId === example.label);
      const rivalPairRow = pairRows.find((row) => row.operatorId !== example.label);
      return Boolean(
        canonicalPairRow &&
          rivalPairRow &&
          canonicalPairRow.heuristicScore >= 0.62 &&
          rivalPairRow.heuristicScore >= 0.62 &&
          Math.abs(canonicalPairRow.heuristicScore - rivalPairRow.heuristicScore) <= 0.22
      );
    }
    case "off_anchor":
      return canonicalRow.anchorScore <= 0.22;
    case "wrong_scale":
      return canonicalRow.scaleScore <= 0.22;
    case "partial_like":
      return canonicalRow.scaleScore <= 0.46 || example.recognition.status !== "recognized";
    case "open_box_like":
      return (canonicalRow.corners ?? 0) <= 3.2 || example.recognition.status !== "recognized";
    case "blocked":
      return canonicalRow.blockedBy === "void_cut";
    default:
      return false;
  }
}

function resolvePairwiseTarget(params) {
  const { scenarioKind, label, candidate, canonicalRow, candidates } = params;
  const gateStrong = !candidate.blockedBy && (candidate.anchorScore ?? 0) >= 0.34 && (candidate.scaleScore ?? 0) >= 0.34;

  if (scenarioKind !== "hard_pair" || !gateStrong || !canonicalRow) {
    return 0;
  }

  const rival = candidates.find(
    (item) =>
      item.operator !== label &&
      (item.operator === "void_cut" || item.operator === "electric_fork")
  );

  if (!rival) {
    return 0;
  }

  if (candidate.operator === label) {
    return roundMetric(clamp(rival.score + 0.055 - candidate.score, 0, 0.08));
  }

  if (candidate.operator === rival.operator) {
    return roundMetric(-clamp(candidate.score - (canonicalRow.score - 0.055), 0, 0.08));
  }

  return 0;
}

function resolveSuppressionTarget(params) {
  const { scenarioKind, candidate, isCanonicalCandidate, strongGate } = params;

  if (candidate.blockedBy) {
    return 1;
  }

  if (scenarioKind === "off_anchor" || scenarioKind === "wrong_scale") {
    return isCanonicalCandidate ? roundMetric(clamp(0.92 + (0.3 - Math.min(candidate.anchorScore, candidate.scaleScore)) * 0.2, 0, 1)) : 0.12;
  }

  if (scenarioKind === "partial_like" || scenarioKind === "open_box_like") {
    return isCanonicalCandidate ? 0.76 : 0.1;
  }

  if (!strongGate && isCanonicalCandidate) {
    return 0.58;
  }

  return 0;
}

function resolveSampleWeight(scenarioKind) {
  switch (scenarioKind) {
    case "hard_pair":
      return 3;
    case "off_anchor":
    case "wrong_scale":
    case "blocked":
      return 2.5;
    case "partial_like":
    case "open_box_like":
      return 2;
    default:
      return 1;
  }
}

function scenarioAllowsRecognition(scenarioKind, label) {
  if (scenarioKind === "blocked" || scenarioKind === "off_anchor" || scenarioKind === "wrong_scale") {
    return false;
  }

  if (scenarioKind === "partial_like" || scenarioKind === "open_box_like") {
    return false;
  }

  return label !== "martial_axis" || scenarioKind === "valid_with_stack";
}

function resolveHardPairId(label, scenarioKind, candidateOperator) {
  if (scenarioKind === "hard_pair" || label === "void_cut" || label === "electric_fork") {
    if (candidateOperator === "void_cut" || candidateOperator === "electric_fork") {
      return "void_cut__electric_fork";
    }
  }

  if ((scenarioKind === "partial_like" || scenarioKind === "wrong_scale") && label === "ice_bar") {
    return "ice_bar__partial_stroke";
  }

  if ((scenarioKind === "open_box_like" || scenarioKind === "wrong_scale") && label === "steel_brace") {
    return "steel_brace__open_box_like";
  }

  if ((scenarioKind === "blocked" || label === "martial_axis") && candidateOperator === "martial_axis") {
    return "martial_axis__blocked_by_void_cut";
  }

  return HARD_PAIR_IDS[candidateOperator] && scenarioKind === "hard_pair" ? HARD_PAIR_IDS[candidateOperator] : "other";
}

function sampleExistingOperators(label, scenarioKind, rng) {
  const allOperators = Object.keys(OPERATOR_TARGETS).filter((operator) => operator !== label);
  const existingOperators = [];

  if (label === "martial_axis" && scenarioKind !== "blocked") {
    existingOperators.push("void_cut");
  }

  const optional = shuffleInPlace(
    allOperators.filter((operator) => operator !== "martial_axis" && operator !== "void_cut"),
    rng
  ).slice(0, Math.floor(randomBetween(rng, 0, 2.99)));

  for (const operator of optional) {
    if (!existingOperators.includes(operator)) {
      existingOperators.push(operator);
    }
  }

  return existingOperators;
}

function buildOperatorStroke(params) {
  const { label, scenarioKind, referenceFrame, rng } = params;
  const baseCoords = resolveScenarioCoords(label, scenarioKind, rng);
  let strokes = templateToStrokes(`${label}-${scenarioKind}`, [baseCoords]);
  strokes = reverseStrokeDirections(strokes, rng, scenarioKind === "hard_pair" ? 0.55 : 0.2);
  strokes = applyCornerRounding(strokes, resolveCornerRounding(scenarioKind, rng));
  strokes = applyPartialTrim(strokes, rng, resolveTrimRatio(scenarioKind, label, rng), minimumPointsFor(label));
  strokes = applyOvershoot(strokes, resolveOvershoot(scenarioKind, rng));
  strokes = applyJitter(strokes, rng, resolveJitter(scenarioKind, rng));
  strokes = placeStroke({ strokes, label, scenarioKind, referenceFrame, rng });
  strokes = retimeStrokes(strokes, rng, [12, 26]);
  return strokes[0];
}

function resolveScenarioCoords(label, scenarioKind, rng) {
  const own = OPERATOR_TARGETS[label].strokes[0];

  if (scenarioKind !== "hard_pair") {
    return own.map((point) => [...point]);
  }

  const rival = label === "void_cut" ? "electric_fork" : "void_cut";
  const alpha = randomBetween(rng, 0.22, 0.46);
  return interpolateTemplates(own, OPERATOR_TARGETS[rival].strokes[0], alpha, 18);
}

function placeStroke(params) {
  const { strokes, label, scenarioKind, referenceFrame, rng } = params;
  const [minimumScale, maximumScale] = OPERATOR_SCALE_RANGES[label];
  const targetZoneId = selectTargetZone(label, scenarioKind, rng);
  const targetZone = referenceFrame.anchorZones.find((zone) => zone.id === targetZoneId) ?? referenceFrame.anchorZones[4];
  const ratio = selectScaleRatio([minimumScale, maximumScale], scenarioKind, rng);
  const desiredAngle = selectRotation(label, scenarioKind, rng);
  const currentBounds = boundingBox(strokes[0].points);
  const currentDiagonal = Math.max(
    Math.hypot(currentBounds.maxX - currentBounds.minX, currentBounds.maxY - currentBounds.minY),
    0.0001
  );
  const desiredDiagonal = ratio * Math.max(referenceFrame.diagonal, 1);
  const scale = desiredDiagonal / currentDiagonal;
  const currentCenter = centroid(strokes[0].points);
  const positioned = rotateScaleTranslate(strokes, {
    scaleX: scale,
    scaleY: scale * selectStretch(scenarioKind, rng),
    rotationRad: desiredAngle,
    translateX: -currentCenter.x * scale,
    translateY: -currentCenter.y * scale
  });
  const displacedCenter = centroid(positioned[0].points);
  const offsetRadius = targetZone.radius * (scenarioKind === "off_anchor" ? 0.08 : 0.18);
  return rotateScaleTranslate(positioned, {
    scaleX: 1,
    scaleY: 1,
    rotationRad: 0,
    translateX: targetZone.center.x - displacedCenter.x + randomBetween(rng, -offsetRadius, offsetRadius),
    translateY: targetZone.center.y - displacedCenter.y + randomBetween(rng, -offsetRadius, offsetRadius)
  });
}

function selectTargetZone(label, scenarioKind, rng) {
  const preferred = OPERATOR_TARGETS[label].preferredAnchorZones ?? ["core"];

  if (scenarioKind === "off_anchor") {
    const forbidden = ANCHOR_ZONES.filter((zoneId) => !preferred.includes(zoneId));
    return forbidden[Math.floor(randomBetween(rng, 0, forbidden.length - 0.001))];
  }

  return preferred[Math.floor(randomBetween(rng, 0, preferred.length - 0.001))];
}

function selectScaleRatio(scaleRange, scenarioKind, rng) {
  const [minimumScale, maximumScale] = scaleRange;

  if (scenarioKind === "wrong_scale") {
    return rng() < 0.7 ? randomBetween(rng, minimumScale * 0.42, minimumScale * 0.78) : randomBetween(rng, maximumScale * 1.24, maximumScale * 1.52);
  }

  if (scenarioKind === "partial_like") {
    return randomBetween(rng, minimumScale * 0.52, minimumScale * 0.92);
  }

  if (scenarioKind === "hard_pair") {
    return randomBetween(rng, Math.max(minimumScale, 0.22), Math.min(maximumScale, 0.34));
  }

  return randomBetween(rng, minimumScale + 0.02, maximumScale - 0.02);
}

function selectRotation(label, scenarioKind, rng) {
  const magnitude =
    scenarioKind === "hard_pair" ? 0.2 : scenarioKind === "open_box_like" || scenarioKind === "partial_like" ? 0.14 : 0.08;
  const bias = label === "void_cut" ? 0.02 : 0;
  return bias + randomBetween(rng, -magnitude, magnitude);
}

function selectStretch(scenarioKind, rng) {
  if (scenarioKind === "wrong_scale") {
    return randomBetween(rng, 0.82, 1.22);
  }

  if (scenarioKind === "partial_like" || scenarioKind === "open_box_like") {
    return randomBetween(rng, 0.86, 1.12);
  }

  return randomBetween(rng, 0.92, 1.08);
}

function resolveCornerRounding(scenarioKind, rng) {
  if (scenarioKind === "hard_pair") {
    return randomBetween(rng, 0.08, 0.22);
  }

  if (scenarioKind === "partial_like" || scenarioKind === "open_box_like") {
    return randomBetween(rng, 0.12, 0.28);
  }

  return randomBetween(rng, 0, 0.08);
}

function resolveTrimRatio(scenarioKind, label, rng) {
  if (scenarioKind === "wrong_scale") {
    return randomBetween(rng, 0.08, 0.22);
  }

  if (scenarioKind === "partial_like") {
    return label === "ice_bar" ? randomBetween(rng, 0.2, 0.4) : randomBetween(rng, 0.12, 0.24);
  }

  if (scenarioKind === "open_box_like") {
    return randomBetween(rng, 0.1, 0.22);
  }

  if (scenarioKind === "hard_pair") {
    return randomBetween(rng, 0.02, 0.14);
  }

  return randomBetween(rng, 0, 0.05);
}

function resolveOvershoot(scenarioKind, rng) {
  if (scenarioKind === "hard_pair") {
    return randomBetween(rng, 0.01, 0.045);
  }

  if (scenarioKind === "partial_like" || scenarioKind === "open_box_like") {
    return randomBetween(rng, 0, 0.025);
  }

  return randomBetween(rng, 0, 0.02);
}

function resolveJitter(scenarioKind, rng) {
  if (scenarioKind === "hard_pair") {
    return randomBetween(rng, 0.01, 0.032);
  }

  if (scenarioKind === "off_anchor" || scenarioKind === "wrong_scale") {
    return randomBetween(rng, 0.008, 0.024);
  }

  return randomBetween(rng, 0.004, 0.016);
}

function minimumPointsFor(label) {
  if (label === "martial_axis") {
    return 5;
  }

  if (label === "electric_fork" || label === "steel_brace" || label === "soul_dot") {
    return 4;
  }

  return 2;
}

function makeBaseSession(rng) {
  const families = Object.keys(FAMILY_TARGETS);
  const family = families[Math.floor(randomBetween(rng, 0, families.length - 0.001))];
  const coords = FAMILY_TARGETS[family].strokes;
  const strokes = templateToStrokes(family, coords);
  const scale = randomBetween(rng, 176, 236);
  const rotate = randomBetween(rng, -0.38, 0.38);
  const translated = rotateScaleTranslate(strokes, {
    scaleX: scale * randomBetween(rng, 0.94, 1.08),
    scaleY: scale * randomBetween(rng, 0.94, 1.08),
    rotationRad: rotate,
    translateX: randomBetween(rng, 260, 340),
    translateY: randomBetween(rng, 236, 314)
  });
  return makeSession(translated);
}

function makeSession(strokes) {
  const timestamps = strokes.flatMap((stroke) => stroke.points.map((point) => point.t));
  return {
    strokes,
    startedAt: Math.min(...timestamps, 0),
    endedAt: Math.max(...timestamps, 0)
  };
}

function centroid(points) {
  const total = points.reduce(
    (accumulator, point) => ({
      x: accumulator.x + point.x,
      y: accumulator.y + point.y
    }),
    { x: 0, y: 0 }
  );
  return {
    x: total.x / Math.max(points.length, 1),
    y: total.y / Math.max(points.length, 1)
  };
}

function boundingBox(points) {
  return points.reduce(
    (accumulator, point) => ({
      minX: Math.min(accumulator.minX, point.x),
      maxX: Math.max(accumulator.maxX, point.x),
      minY: Math.min(accumulator.minY, point.y),
      maxY: Math.max(accumulator.maxY, point.y)
    }),
    {
      minX: Number.POSITIVE_INFINITY,
      maxX: Number.NEGATIVE_INFINITY,
      minY: Number.POSITIVE_INFINITY,
      maxY: Number.NEGATIVE_INFINITY
    }
  );
}

function interpolateTemplates(leftCoords, rightCoords, alpha, count) {
  const left = resamplePolyline(leftCoords, count);
  const right = resamplePolyline(rightCoords, count);
  return left.map((point, index) => [
    roundMetric(point[0] * (1 - alpha) + right[index][0] * alpha),
    roundMetric(point[1] * (1 - alpha) + right[index][1] * alpha)
  ]);
}

function resamplePolyline(points, count) {
  if (points.length <= 1) {
    return points.map((point) => [...point]);
  }

  const segments = [];
  let total = 0;

  for (let index = 1; index < points.length; index += 1) {
    const start = points[index - 1];
    const end = points[index];
    const length = Math.hypot(end[0] - start[0], end[1] - start[1]);
    segments.push({ start, end, length, total });
    total += length;
  }

  if (total <= 0.0001) {
    return Array.from({ length: count }, () => [...points[0]]);
  }

  return Array.from({ length: count }, (_, index) => {
    const target = (index / Math.max(count - 1, 1)) * total;
    const segment =
      segments.find((item) => target >= item.total && target <= item.total + item.length) ?? segments[segments.length - 1];
    const ratio = segment.length <= 0.0001 ? 0 : (target - segment.total) / segment.length;
    return [
      roundMetric(segment.start[0] + (segment.end[0] - segment.start[0]) * ratio),
      roundMetric(segment.start[1] + (segment.end[1] - segment.start[1]) * ratio)
    ];
  });
}

function shuffleInPlace(items, rng) {
  const clone = [...items];

  for (let index = clone.length - 1; index > 0; index -= 1) {
    const next = Math.floor(randomBetween(rng, 0, index + 0.999));
    const current = clone[index];
    clone[index] = clone[next];
    clone[next] = current;
  }

  return clone;
}

function summarizeDataset(examples) {
  const bySplit = {};
  const byScenario = {};
  const byOperator = {};

  for (const example of examples) {
    bySplit[example.split] = (bySplit[example.split] ?? 0) + 1;
    byScenario[example.scenarioKind] = (byScenario[example.scenarioKind] ?? 0) + 1;
    byOperator[example.canonicalOperator] = (byOperator[example.canonicalOperator] ?? 0) + 1;
  }

  return {
    totalExamples: examples.length,
    bySplit,
    byScenario,
    byOperator
  };
}

function toExampleSummary(example) {
  return {
    exampleId: example.exampleId,
    split: example.split,
    scenarioKind: example.scenarioKind,
    canonicalOperator: example.label,
    heuristicStatus: example.recognition.status,
    heuristicTopOperator: example.rows[0]?.operatorId ?? null
  };
}

async function loadOverlayRuntime(runtimeCacheDir) {
  const compileDir = path.resolve(repoRoot, runtimeCacheDir || ".tmp/ml-baseline-runtime");
  await rm(compileDir, { recursive: true, force: true });
  await mkdir(compileDir, { recursive: true });
  await writeFile(path.join(compileDir, "package.json"), `${JSON.stringify({ type: "commonjs" }, null, 2)}\n`, "utf8");

  const tscArgs = [
    "./node_modules/.bin/tsc",
    "--ignoreConfig",
    "--ignoreDeprecations",
    "6.0",
    "--outDir",
    compileDir,
    "--rootDir",
    ".",
    "--module",
    "commonjs",
    "--target",
    "es2020",
    "--moduleResolution",
    "node",
    "--esModuleInterop",
    "true",
    "--skipLibCheck",
    "true",
    "src/recognizer/operators.ts",
    "src/recognizer/operator-templates.ts",
    "src/recognizer/geometry.ts",
    "src/recognizer/rerank.ts",
    "src/recognizer/types.ts"
  ];

  await spawnProcess("node", tscArgs, repoRoot);
  const operatorsPath = path.join(compileDir, "src/recognizer/operators.js");
  return require(operatorsPath);
}

function spawnProcess(command, args, cwd) {
  return new Promise((resolve, reject) => {
    const child = spawn(command, args, {
      cwd,
      stdio: ["ignore", "pipe", "pipe"]
    });
    let stderr = "";
    let stdout = "";
    child.stdout.on("data", (chunk) => {
      stdout += chunk.toString();
    });
    child.stderr.on("data", (chunk) => {
      stderr += chunk.toString();
    });
    child.on("error", reject);
    child.on("close", (code) => {
      if (code === 0) {
        resolve({ stdout, stderr });
        return;
      }
      reject(new Error(`${command} ${args.join(" ")} failed with code ${code}\n${stdout}\n${stderr}`));
    });
  });
}

function roundMetric(value) {
  return Number(value.toFixed(6));
}
