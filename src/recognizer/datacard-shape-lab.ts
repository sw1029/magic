import {
  boundingBox,
  clusterEndpointCount,
  distance,
  lineAngle,
  normalizeAngleHalfPi,
  normalizeStrokes,
  pathLength,
  pointCloudDistance,
  rdpSimplify,
  strokeStraightness
} from "./geometry";
import { listBuiltInFamilyCards, type MagicFamilyCard } from "./datacards";
import { deriveRecognitionFeatureVectorV2 } from "./feature-v2";
import { buildGestureRecognitionSignals } from "./gesture-matcher";
import { recognizeSession } from "./recognize";
import { recognizeSealedBaseSession } from "./seal";
import { GLYPH_TEMPLATES } from "./templates";
import type {
  GlyphFamily,
  GestureRecognitionSignals,
  PointSample,
  RecognitionFeatureVectorV2,
  RecognitionResult,
  RecognitionStatus,
  SealDetection,
  Stroke,
  StrokeSession,
  UserInputProfile
} from "./types";

export type CustomGlyphFamilyId = `custom:${string}`;
export type DatacardShapeId = GlyphFamily | CustomGlyphFamilyId;
export type DatacardShapePresetKind = "built_in" | "custom";
export type DatacardShapePresetGroup = "basic" | "custom";
export type DatacardRecognizerActivationStatus =
  | "metadata"
  | "shape_definition"
  | "active_recognizer"
  | "blocked";

export interface DatacardShapeFeatureHints {
  strokeCount: readonly [number, number];
  closed: boolean;
  corners?: readonly [number, number];
  endpointClusters?: readonly [number, number];
  circularity?: readonly [number, number];
  fillRatio?: readonly [number, number];
  parallelism?: readonly [number, number];
}

export interface DatacardShapeDefinition {
  pattern: string;
  expression: string;
  guide: string;
  keywords: readonly string[];
  features: DatacardShapeFeatureHints;
  exampleTemplate: readonly Stroke[];
}

export interface DatacardShapePreset {
  id: DatacardShapeId;
  kind: DatacardShapePresetKind;
  group: DatacardShapePresetGroup;
  label: string;
  shortLabel: string;
  description: string;
  builtInFamily?: GlyphFamily;
  definition: DatacardShapeDefinition;
}

export interface DatacardShapeCapture {
  id: string;
  presetId: DatacardShapeId;
  strokes: Stroke[];
  normalizedCloud: PointSample[];
  features: DatacardShapeFeatureVector;
  featureV2?: RecognitionFeatureVectorV2;
  gestureSummary?: GestureRecognitionSignals;
  timestamp: number;
}

export interface DatacardShapeCaptureStore {
  captures: readonly DatacardShapeCapture[];
  updatedAt: number;
}

export interface DatacardRecognitionCandidate {
  id: DatacardShapeId;
  label: string;
  kind: DatacardShapePresetKind;
  score: number;
  baselineScore: number;
  localModelScore?: number;
  localModelLift: number;
  templateScore: number;
  hintScore: number;
  gestureScore?: number;
  captureCount: number;
  activationStatus?: DatacardRecognizerActivationStatus;
  promotionRisk?: number;
  status: RecognitionStatus;
  reason: string;
}

export interface DatacardRecognitionResult {
  selectedPresetId: DatacardShapeId;
  selectedCandidate: DatacardRecognitionCandidate;
  candidates: DatacardRecognitionCandidate[];
  baseResult: RecognitionResult;
  sessionUsed: StrokeSession;
  sealDetection?: SealDetection;
  features: DatacardShapeFeatureVector;
  registry?: DatacardRecognizerRegistry;
}

export interface DatacardShapeValidationIssue {
  path: string;
  code: string;
  message: string;
}

export interface DatacardShapeValidationResult {
  valid: boolean;
  issues: readonly DatacardShapeValidationIssue[];
  compiledPattern?: RegExp;
}

export interface DatacardShapeFeatureVector {
  strokeCount: number;
  closure: number;
  corners: number;
  endpointClusters: number;
  circularity: number;
  fillRatio: number;
  parallelism: number;
  rawAngleRadians: number;
}

export interface DatacardShapeRecognizerProfile {
  preset: DatacardShapePreset;
  activationStatus: DatacardRecognizerActivationStatus;
  activationReasons: readonly string[];
  replacementFor?: GlyphFamily;
  operatorCompatible: boolean;
  captureCount: number;
  captures: readonly DatacardShapeCapture[];
  confusionRisk: number;
  signature: string;
  featureV2?: RecognitionFeatureVectorV2;
  gestureSummary?: GestureRecognitionSignals;
}

export interface DatacardRecognizerRegistry {
  profiles: readonly DatacardShapeRecognizerProfile[];
  activeProfiles: readonly DatacardShapeRecognizerProfile[];
  replacedBy: Partial<Record<GlyphFamily, DatacardShapeId>>;
  signature: string;
  createdAt: number;
}

export interface DatacardShapeCompileOptions {
  activate?: boolean;
  qaPassed?: boolean;
  replacementFor?: GlyphFamily;
  operatorCompatible?: boolean;
  builtInConfusionLimit?: number;
  now?: number;
}

const CUSTOM_SHAPE_PRESETS: readonly DatacardShapePreset[] = [
  {
    id: "custom:spiral",
    kind: "custom",
    group: "custom",
    label: "소용돌이",
    shortLabel: "소용돌이",
    description: "안쪽에서 바깥쪽으로 말려 나가는 한 줄 도형입니다.",
    definition: {
      pattern: "(spiral|coil|swirl|소용돌이)",
      expression: "spiral | coil | 소용돌이",
      guide: "중심에서 시작해 바깥으로 말려 나가는 곡선을 그립니다.",
      keywords: ["spiral", "coil", "swirl", "소용돌이"],
      features: {
        strokeCount: [1, 2],
        closed: false,
        corners: [2, 9],
        circularity: [0.24, 0.92],
        fillRatio: [0.02, 0.34]
      },
      exampleTemplate: [createStroke("custom-spiral-1", spiralPoints())]
    }
  },
  {
    id: "custom:star",
    kind: "custom",
    group: "custom",
    label: "별표",
    shortLabel: "별",
    description: "다섯 꼭짓점이 번갈아 연결되는 닫힌 도형입니다.",
    definition: {
      pattern: "(star|five[- ]?point|별)",
      expression: "star | five-point | 별",
      guide: "바깥 꼭짓점과 안쪽 꼭짓점을 번갈아 연결해 닫습니다.",
      keywords: ["star", "five-point", "별"],
      features: {
        strokeCount: [1, 2],
        closed: true,
        corners: [8, 12],
        fillRatio: [0.22, 0.62]
      },
      exampleTemplate: [createStroke("custom-star-1", starPoints())]
    }
  },
  {
    id: "custom:cross",
    kind: "custom",
    group: "custom",
    label: "십자",
    shortLabel: "십자",
    description: "세로선과 가로선이 중앙에서 만나는 열린 도형입니다.",
    definition: {
      pattern: "(cross|plus|십자)",
      expression: "cross | plus | 십자",
      guide: "세로선을 긋고 가운데를 가로지르는 선을 더합니다.",
      keywords: ["cross", "plus", "십자"],
      features: {
        strokeCount: [2, 2],
        closed: false,
        corners: [1, 4],
        endpointClusters: [4, 4],
        fillRatio: [0, 0.18]
      },
      exampleTemplate: [
        createStroke("custom-cross-1", [[0, -0.72], [0, 0.72]]),
        createStroke("custom-cross-2", [[-0.72, 0], [0.72, 0]])
      ]
    }
  },
  {
    id: "custom:y_shape",
    kind: "custom",
    group: "custom",
    label: "Y자형",
    shortLabel: "Y자형",
    description: "가운데 가지점에서 위쪽 두 갈래와 아래 줄기가 이어지는 열린 도형입니다.",
    definition: {
      pattern: "(y[- ]?shape|y[- ]?form|Y자형|와이형)",
      expression: "Y-shape | Y자형 | 와이형",
      guide: "가운데 지점에서 왼쪽 위, 오른쪽 위, 아래 방향으로 갈라지는 세 줄기를 그립니다.",
      keywords: ["Y-shape", "Y자형", "와이형", "branch"],
      features: {
        strokeCount: [1, 3],
        closed: false,
        corners: [1, 5],
        endpointClusters: [3, 4],
        fillRatio: [0, 0.2],
        parallelism: [0, 0.72]
      },
      exampleTemplate: [
        createStroke("custom-y-shape-1", [[0, 0], [-0.54, -0.68]]),
        createStroke("custom-y-shape-2", [[0, 0], [0.54, -0.68]]),
        createStroke("custom-y-shape-3", [[0, 0], [0, 0.74]])
      ]
    }
  },
  {
    id: "custom:crescent",
    kind: "custom",
    group: "custom",
    label: "초승달",
    shortLabel: "초승달",
    description: "바깥 곡선과 안쪽 곡선이 벌어진 달 모양입니다.",
    definition: {
      pattern: "(crescent|moon|초승달)",
      expression: "crescent | moon | 초승달",
      guide: "큰 곡선을 그리고 안쪽으로 얕은 곡선을 되돌려 그립니다.",
      keywords: ["crescent", "moon", "초승달"],
      features: {
        strokeCount: [1, 2],
        closed: false,
        corners: [2, 7],
        circularity: [0.28, 0.92],
        fillRatio: [0.05, 0.42]
      },
      exampleTemplate: [createStroke("custom-crescent-1", crescentPoints())]
    }
  },
  {
    id: "custom:diamond",
    kind: "custom",
    group: "custom",
    label: "마름모",
    shortLabel: "마름모",
    description: "네 꼭짓점이 위아래로 긴 닫힌 도형입니다.",
    definition: {
      pattern: "(diamond|rhombus|마름모)",
      expression: "diamond | rhombus | 마름모",
      guide: "위, 오른쪽, 아래, 왼쪽 꼭짓점을 순서대로 이어 닫습니다.",
      keywords: ["diamond", "rhombus", "마름모"],
      features: {
        strokeCount: [1, 2],
        closed: true,
        corners: [4, 5],
        fillRatio: [0.42, 0.72]
      },
      exampleTemplate: [
        createStroke("custom-diamond-1", [
          [0, -0.72],
          [0.66, 0],
          [0, 0.72],
          [-0.66, 0],
          [0, -0.72]
        ])
      ]
    }
  },
  {
    id: "custom:zigzag",
    kind: "custom",
    group: "custom",
    label: "번개선",
    shortLabel: "번개",
    description: "방향이 여러 번 꺾이는 열린 선 도형입니다.",
    definition: {
      pattern: "(zigzag|lightning|번개)",
      expression: "zigzag | lightning | 번개",
      guide: "짧은 대각선을 여러 번 꺾어 내려갑니다.",
      keywords: ["zigzag", "lightning", "번개"],
      features: {
        strokeCount: [1, 2],
        closed: false,
        corners: [4, 8],
        endpointClusters: [2, 4],
        fillRatio: [0, 0.24]
      },
      exampleTemplate: [
        createStroke("custom-zigzag-1", [
          [-0.45, -0.72],
          [0.26, -0.24],
          [-0.1, -0.08],
          [0.42, 0.24],
          [-0.28, 0.72]
        ])
      ]
    }
  }
];

export function createEmptyDatacardShapeCaptureStore(updatedAt = Date.now()): DatacardShapeCaptureStore {
  return {
    captures: [],
    updatedAt
  };
}

export function listDatacardShapePresets(): readonly DatacardShapePreset[] {
  return [...listBuiltInShapePresets(), ...CUSTOM_SHAPE_PRESETS];
}

export function getDatacardShapePresetById(id: string): DatacardShapePreset | undefined {
  return listDatacardShapePresets().find((preset) => preset.id === id);
}

export function validateDatacardShapePreset(preset: DatacardShapePreset): DatacardShapeValidationResult {
  const issues: DatacardShapeValidationIssue[] = [];

  if (!preset.id) {
    issues.push({ path: "preset.id", code: "missing_id", message: "도형 카드 id가 필요합니다." });
  }

  if (preset.kind === "custom" && !preset.id.startsWith("custom:")) {
    issues.push({ path: "preset.id", code: "invalid_custom_id", message: "새 도형 id는 custom:으로 시작해야 합니다." });
  }

  if (!preset.definition.pattern.trim()) {
    issues.push({ path: "preset.definition.pattern", code: "missing_pattern", message: "정의 표현이 필요합니다." });
  }

  let compiledPattern: RegExp | undefined;
  try {
    compiledPattern = new RegExp(preset.definition.pattern, "iu");
  } catch {
    issues.push({ path: "preset.definition.pattern", code: "invalid_pattern", message: "정의 표현을 읽을 수 없습니다." });
  }

  if (compiledPattern && !compiledPattern.test(preset.definition.keywords.join(" "))) {
    issues.push({ path: "preset.definition.keywords", code: "pattern_not_matched", message: "정의 표현이 예시 키워드와 맞지 않습니다." });
  }

  if (preset.definition.exampleTemplate.length === 0) {
    issues.push({ path: "preset.definition.exampleTemplate", code: "missing_template", message: "예시 도형이 필요합니다." });
  }

  return issues.length === 0 ? { valid: true, issues, compiledPattern } : { valid: false, issues };
}

export function appendDatacardShapeCapture(
  store: DatacardShapeCaptureStore,
  presetId: DatacardShapeId,
  strokes: readonly Stroke[],
  timestamp = Date.now()
): DatacardShapeCaptureStore {
  const safeStrokes = cloneStrokes(strokes.filter((stroke) => stroke.points.length >= 2));
  const normalized = safeStrokes.length > 0 ? normalizeStrokes(safeStrokes) : null;
  const capture: DatacardShapeCapture = {
    id: globalThis.crypto?.randomUUID?.() ?? `datacard-capture-${timestamp}`,
    presetId,
    strokes: safeStrokes,
    normalizedCloud: normalized?.normalizedCloud.map((point) => ({ ...point })) ?? [],
    features: deriveShapeFeatures(safeStrokes),
    featureV2: deriveRecognitionFeatureVectorV2(safeStrokes),
    gestureSummary: resolveCaptureGestureSummary(presetId, safeStrokes),
    timestamp
  };

  return {
    captures: [...store.captures.filter((entry) => entry.id !== capture.id), capture].slice(-36),
    updatedAt: timestamp
  };
}

export function recognizeSessionWithDatacard(
  session: StrokeSession,
  preset: DatacardShapePreset,
  captures: DatacardShapeCaptureStore,
  baseProfile?: UserInputProfile
): DatacardRecognitionResult {
  const prepared = prepareDatacardRecognitionSession(session, baseProfile);
  const features = deriveShapeFeatures(prepared.sessionUsed.strokes);
  const candidates = listDatacardShapePresets()
    .map((candidatePreset) =>
      scoreDatacardPreset(candidatePreset, prepared.baseResult, prepared.sessionUsed, features, captures)
    )
    .sort((left, right) => right.score - left.score);
  const selectedCandidate =
    candidates.find((candidate) => candidate.id === preset.id) ??
    scoreDatacardPreset(preset, prepared.baseResult, prepared.sessionUsed, features, captures);

  return {
    selectedPresetId: preset.id,
    selectedCandidate,
    candidates,
    baseResult: prepared.baseResult,
    sessionUsed: prepared.sessionUsed,
    sealDetection: prepared.sealDetection,
    features
  };
}

export function compileDatacardShapePreset(
  preset: DatacardShapePreset,
  captures: DatacardShapeCaptureStore,
  options: DatacardShapeCompileOptions = {}
): DatacardShapeRecognizerProfile {
  const validation = validateDatacardShapePreset(preset);
  const matchingCaptures = captures.captures.filter((capture) => capture.presetId === preset.id);
  const confusionRisk = estimateDatacardConfusionRisk(preset);
  const builtInConfusionLimit = options.builtInConfusionLimit ?? 0.32;
  const activationReasons: string[] = [];
  let activationStatus: DatacardRecognizerActivationStatus = "metadata";

  if (!validation.valid) {
    activationStatus = "blocked";
    activationReasons.push(...validation.issues.map((issue) => issue.code));
  } else if (preset.kind === "built_in") {
    activationStatus = "active_recognizer";
    activationReasons.push("built_in_family");
  } else {
    activationStatus = "shape_definition";
    activationReasons.push("valid_definition");

    if (preset.definition.exampleTemplate.length === 0) {
      activationStatus = "blocked";
      activationReasons.push("missing_template");
    } else if (confusionRisk > builtInConfusionLimit) {
      activationReasons.push("confusion_risk_shadow_only");
    } else if (matchingCaptures.length >= 3 || options.qaPassed || options.activate) {
      activationStatus = "active_recognizer";
      activationReasons.push(matchingCaptures.length >= 3 ? "capture_threshold_met" : "qa_override");
    } else {
      activationReasons.push("capture_threshold_pending");
    }
  }

  return {
    preset,
    activationStatus,
    activationReasons,
    replacementFor: options.replacementFor,
    operatorCompatible: options.operatorCompatible ?? preset.kind === "built_in",
    captureCount: matchingCaptures.length,
    captures: matchingCaptures.map(cloneDatacardCapture),
    confusionRisk: roundMetric(confusionRisk),
    signature: buildDatacardRecognizerSignature(preset, matchingCaptures),
    featureV2: deriveRecognitionFeatureVectorV2(cloneStrokes(preset.definition.exampleTemplate)),
    gestureSummary: averageCaptureGestureSummary(matchingCaptures)
  };
}

export function createDatacardRecognizerRegistry(
  presets: readonly DatacardShapePreset[] = listDatacardShapePresets(),
  captures: DatacardShapeCaptureStore = createEmptyDatacardShapeCaptureStore(),
  options: DatacardShapeCompileOptions = {}
): DatacardRecognizerRegistry {
  const profiles = presets.map((preset) => compileDatacardShapePreset(preset, captures, options));
  const activeProfiles = profiles.filter((profile) => profile.activationStatus === "active_recognizer");
  const replacedBy = profiles.reduce<Partial<Record<GlyphFamily, DatacardShapeId>>>((accumulator, profile) => {
    if (profile.replacementFor && profile.activationStatus === "active_recognizer") {
      accumulator[profile.replacementFor] = profile.preset.id;
    }

    return accumulator;
  }, {});
  const signature = [
    "datacard-registry-v1",
    ...profiles.map((profile) => `${profile.preset.id}:${profile.activationStatus}:${profile.captureCount}`)
  ].join("|");

  return {
    profiles,
    activeProfiles,
    replacedBy,
    signature,
    createdAt: options.now ?? Date.now()
  };
}

export function recognizeSessionWithDatacardRegistry(
  session: StrokeSession,
  registry: DatacardRecognizerRegistry,
  options: { selectedPresetId?: DatacardShapeId; baseProfile?: UserInputProfile } = {}
): DatacardRecognitionResult {
  const prepared = prepareDatacardRecognitionSession(session, options.baseProfile);
  const features = deriveShapeFeatures(prepared.sessionUsed.strokes);
  const syntheticStore: DatacardShapeCaptureStore = {
    captures: [],
    updatedAt: registry.createdAt
  };
  const candidates = registry.profiles
    .map((profile) =>
      scoreDatacardPreset(profile.preset, prepared.baseResult, prepared.sessionUsed, features, syntheticStore, profile)
    )
    .sort((left, right) => right.score - left.score);
  const selectedPresetId = options.selectedPresetId ?? candidates[0]?.id ?? registry.profiles[0]?.preset.id ?? "wind";
  const selectedCandidate =
    candidates.find((candidate) => candidate.id === selectedPresetId) ??
    candidates[0] ??
    scoreDatacardPreset(listDatacardShapePresets()[0], prepared.baseResult, prepared.sessionUsed, features, syntheticStore);

  return {
    selectedPresetId,
    selectedCandidate,
    candidates,
    baseResult: prepared.baseResult,
    sessionUsed: prepared.sessionUsed,
    sealDetection: prepared.sealDetection,
    features,
    registry
  };
}

function listBuiltInShapePresets(): DatacardShapePreset[] {
  return listBuiltInFamilyCards()
    .filter((card) => card.family !== "life")
    .map(buildBuiltInShapePreset);
}

function buildBuiltInShapePreset(card: MagicFamilyCard): DatacardShapePreset {
  return {
    id: card.family,
    kind: "built_in",
    group: "basic",
    label: card.label,
    shortLabel: card.shortLabel,
    description: card.tutorial.summary,
    builtInFamily: card.family,
    definition: {
      pattern: card.recognitionHints.definitionPattern ?? buildKeywordPattern(card.recognitionHints.shapeKeywords),
      expression: card.recognitionHints.shapeKeywords.join(" | "),
      guide: card.tutorial.instruction,
      keywords: card.recognitionHints.shapeKeywords,
      features: {
        strokeCount: card.recognitionHints.strokeCount,
        closed: card.recognitionHints.closed,
        corners: card.recognitionHints.featureHints?.corners,
        endpointClusters: card.recognitionHints.featureHints?.endpointClusters,
        circularity: card.recognitionHints.featureHints?.circularity,
        fillRatio: card.recognitionHints.featureHints?.fillRatio,
        parallelism: card.recognitionHints.featureHints?.parallelism
      },
      exampleTemplate:
        card.recognitionHints.exampleTemplate ??
        GLYPH_TEMPLATES.find((template) => template.family === card.family)?.strokes ??
        []
    }
  };
}

function estimateDatacardConfusionRisk(preset: DatacardShapePreset): number {
  if (preset.kind === "built_in") {
    return 0;
  }

  const templateScores = GLYPH_TEMPLATES.map((template) =>
    scoreTemplate(preset.definition.exampleTemplate, template.strokes)
  );
  const hintOverlap = listBuiltInShapePresets()
    .map((builtInPreset) => scoreFeatureHintOverlap(preset.definition.features, builtInPreset.definition.features))
    .sort((left, right) => right - left)[0] ?? 0;

  return clamp((Math.max(...templateScores, 0) * 0.68 + hintOverlap * 0.32) * 0.8, 0, 1);
}

function scoreFeatureHintOverlap(left: DatacardShapeFeatureHints, right: DatacardShapeFeatureHints): number {
  const scores = [
    rangeOverlap(left.strokeCount, right.strokeCount),
    left.closed === right.closed ? 1 : 0
  ];

  if (left.corners && right.corners) {
    scores.push(rangeOverlap(left.corners, right.corners));
  }

  if (left.endpointClusters && right.endpointClusters) {
    scores.push(rangeOverlap(left.endpointClusters, right.endpointClusters));
  }

  if (left.circularity && right.circularity) {
    scores.push(rangeOverlap(left.circularity, right.circularity));
  }

  if (left.fillRatio && right.fillRatio) {
    scores.push(rangeOverlap(left.fillRatio, right.fillRatio));
  }

  if (left.parallelism && right.parallelism) {
    scores.push(rangeOverlap(left.parallelism, right.parallelism));
  }

  return average(scores);
}

function rangeOverlap(left: readonly [number, number], right: readonly [number, number]): number {
  const overlap = Math.max(0, Math.min(left[1], right[1]) - Math.max(left[0], right[0]));
  const union = Math.max(left[1], right[1]) - Math.min(left[0], right[0]);
  return union <= 0 ? 0 : clamp(overlap / union, 0, 1);
}

function buildDatacardRecognizerSignature(
  preset: DatacardShapePreset,
  captures: readonly DatacardShapeCapture[]
): string {
  return [
    "datacard-shape-v1",
    preset.id,
    preset.definition.pattern,
    preset.definition.exampleTemplate.length,
    captures.length
  ].join(":");
}

function resolveCaptureGestureSummary(
  presetId: DatacardShapeId,
  strokes: readonly Stroke[]
): GestureRecognitionSignals | undefined {
  const preset = getDatacardShapePresetById(presetId);

  if (!preset) {
    return undefined;
  }

  return buildGestureRecognitionSignals(strokes, preset.definition.exampleTemplate);
}

function averageCaptureGestureSummary(
  captures: readonly DatacardShapeCapture[]
): GestureRecognitionSignals | undefined {
  const gestureSummaries = captures
    .map((capture) => capture.gestureSummary)
    .filter((summary): summary is GestureRecognitionSignals => Boolean(summary));

  if (gestureSummaries.length === 0) {
    return undefined;
  }

  return {
    trajectorySimilarity: roundMetric(average(gestureSummaries.map((summary) => summary.trajectorySimilarity))),
    strokeOrderSimilarity: roundMetric(average(gestureSummaries.map((summary) => summary.strokeOrderSimilarity))),
    directionSequenceSimilarity: roundMetric(average(gestureSummaries.map((summary) => summary.directionSequenceSimilarity))),
    gestureScore: roundMetric(average(gestureSummaries.map((summary) => summary.gestureScore))),
    temporalScore: roundMetric(average(gestureSummaries.map((summary) => summary.temporalScore)))
  };
}

function scoreDatacardPreset(
  preset: DatacardShapePreset,
  baseResult: RecognitionResult,
  session: StrokeSession,
  features: DatacardShapeFeatureVector,
  captures: DatacardShapeCaptureStore,
  recognizerProfile?: DatacardShapeRecognizerProfile
): DatacardRecognitionCandidate {
  const templateScore = scoreTemplate(session.strokes, preset.definition.exampleTemplate);
  const hintScore = scoreFeatureHints(features, preset.definition.features);
  const gesture = buildGestureRecognitionSignals(session.strokes, preset.definition.exampleTemplate);
  const baseCandidateScore = preset.builtInFamily
    ? baseResult.candidates.find((candidate) => candidate.family === preset.builtInFamily)?.score
    : undefined;
  const rawBaseline =
    preset.kind === "built_in"
      ? (baseCandidateScore ?? 0) * 0.58 + templateScore * 0.14 + hintScore * 0.2 + gesture.gestureScore * 0.08
      : templateScore * 0.42 + hintScore * 0.28 + gesture.gestureScore * 0.3;
  const baselineScore = clamp(preset.kind === "custom" ? rawBaseline * 0.88 : rawBaseline, 0, 1);
  const matchingCaptures = recognizerProfile?.captures ?? captures.captures.filter((capture) => capture.presetId === preset.id);
  const captureCount = recognizerProfile?.captureCount ?? matchingCaptures.length;
  const localModelScore = matchingCaptures.length > 0 ? scoreLocalModel(session.strokes, features, matchingCaptures) : undefined;
  const score =
    localModelScore === undefined
      ? baselineScore
      : clamp(baselineScore * 0.66 + localModelScore * 0.34 + Math.min(matchingCaptures.length, 4) * 0.012, 0, 1);
  const status = resolveDatacardStatus(score, baselineScore, captureCount, session.strokes.length);

  return {
    id: preset.id,
    label: preset.label,
    kind: preset.kind,
    score: roundMetric(score),
    baselineScore: roundMetric(baselineScore),
    localModelScore: localModelScore === undefined ? undefined : roundMetric(localModelScore),
    localModelLift: roundMetric(score - baselineScore),
    templateScore: roundMetric(templateScore),
    hintScore: roundMetric(hintScore),
    gestureScore: gesture.gestureScore,
    captureCount,
    activationStatus: recognizerProfile?.activationStatus ?? (preset.kind === "built_in" ? "active_recognizer" : "shape_definition"),
    promotionRisk: recognizerProfile ? roundMetric(recognizerProfile.confusionRisk + (recognizerProfile.activationStatus === "active_recognizer" ? 0 : 0.18)) : undefined,
    status,
    reason: buildDatacardReason(status, preset, score, captureCount)
  };
}

function prepareDatacardRecognitionSession(
  session: StrokeSession,
  profile?: UserInputProfile
): { sessionUsed: StrokeSession; baseResult: RecognitionResult; sealDetection?: SealDetection } {
  if (session.strokes.length >= 2) {
    const sealedBase = recognizeSealedBaseSession(session, { profile });

    if (sealedBase.sealDetection.ok) {
      return {
        sessionUsed: sealedBase.baseSession,
        baseResult: sealedBase.result,
        sealDetection: sealedBase.sealDetection
      };
    }
  }

  return {
    sessionUsed: {
      ...session,
      strokes: cloneStrokes(session.strokes.filter((stroke) => stroke.points.length >= 2))
    },
    baseResult: recognizeSession(session, { sealed: false, profile })
  };
}

function scoreTemplate(strokes: readonly Stroke[], template: readonly Stroke[]): number {
  const validStrokes = strokes.filter((stroke) => stroke.points.length >= 2);

  if (validStrokes.length === 0 || template.length === 0) {
    return 0;
  }

  const current = normalizeStrokes(cloneStrokes(validStrokes)).normalizedCloud;
  const expected = normalizeStrokes(cloneStrokes(template)).normalizedCloud;
  return clamp(1 - pointCloudDistance(current, expected) / 0.72, 0, 1);
}

function scoreLocalModel(
  strokes: readonly Stroke[],
  features: DatacardShapeFeatureVector,
  captures: readonly DatacardShapeCapture[]
): number {
  const validStrokes = cloneStrokes(strokes.filter((stroke) => stroke.points.length >= 2));

  if (validStrokes.length === 0 || captures.length === 0) {
    return 0;
  }

  const current = normalizeStrokes(validStrokes).normalizedCloud;
  const cloudScore = captures
    .map((capture) => clamp(1 - pointCloudDistance(current, capture.normalizedCloud) / 0.72, 0, 1))
    .sort((left, right) => right - left)
    .slice(0, 3);
  const featureScore = captures
    .map((capture) => scoreFeatureSimilarity(features, capture.features))
    .sort((left, right) => right - left)
    .slice(0, 3);
  const cloudAverage = average(cloudScore);
  const featureAverage = average(featureScore);

  return clamp(cloudAverage * 0.72 + featureAverage * 0.28, 0, 1);
}

function scoreFeatureHints(features: DatacardShapeFeatureVector, hints: DatacardShapeFeatureHints): number {
  const scores = [
    rangeScore(features.strokeCount, hints.strokeCount[0], hints.strokeCount[1]),
    hints.closed ? features.closure : 1 - features.closure
  ];

  if (hints.corners) {
    scores.push(rangeScore(features.corners, hints.corners[0], hints.corners[1]));
  }

  if (hints.endpointClusters) {
    scores.push(rangeScore(features.endpointClusters, hints.endpointClusters[0], hints.endpointClusters[1]));
  }

  if (hints.circularity) {
    scores.push(rangeScore(features.circularity, hints.circularity[0], hints.circularity[1]));
  }

  if (hints.fillRatio) {
    scores.push(rangeScore(features.fillRatio, hints.fillRatio[0], hints.fillRatio[1]));
  }

  if (hints.parallelism) {
    scores.push(rangeScore(features.parallelism, hints.parallelism[0], hints.parallelism[1]));
  }

  return average(scores);
}

function scoreFeatureSimilarity(left: DatacardShapeFeatureVector, right: DatacardShapeFeatureVector): number {
  return average([
    closeness(left.strokeCount, right.strokeCount, 2),
    closeness(left.closure, right.closure, 0.32),
    closeness(left.corners, right.corners, 4),
    closeness(left.endpointClusters, right.endpointClusters, 3),
    closeness(left.circularity, right.circularity, 0.38),
    closeness(left.fillRatio, right.fillRatio, 0.28),
    closeness(left.parallelism, right.parallelism, 0.42),
    closeness(Math.abs(left.rawAngleRadians), Math.abs(right.rawAngleRadians), Math.PI / 5)
  ]);
}

function deriveShapeFeatures(strokes: readonly Stroke[]): DatacardShapeFeatureVector {
  const validStrokes = cloneStrokes(strokes.filter((stroke) => stroke.points.length >= 2));

  if (validStrokes.length === 0) {
    return {
      strokeCount: 0,
      closure: 0,
      corners: 0,
      endpointClusters: 0,
      circularity: 0,
      fillRatio: 0,
      parallelism: 0,
      rawAngleRadians: 0
    };
  }

  const normalized = normalizeStrokes(validStrokes);
  const dominantStroke = [...validStrokes].sort((left, right) => pathLength(right.points) - pathLength(left.points))[0];
  const dominantPoints = dominantStroke?.points ?? [];
  const bounds = boundingBox(validStrokes.flatMap((stroke) => stroke.points));
  const diagonal = Math.max(Math.hypot(bounds.width, bounds.height), 1);
  const radiusSamples = normalized.normalizedCloud.map((point) => Math.hypot(point.x, point.y));
  const meanRadius = average(radiusSamples);
  const variance = average(radiusSamples.map((radius) => (radius - meanRadius) ** 2));
  const firstPoint = dominantPoints[0];
  const lastPoint = dominantPoints[dominantPoints.length - 1];
  const closureGap = firstPoint && lastPoint ? distance(firstPoint, lastPoint) : diagonal;

  return {
    strokeCount: validStrokes.length,
    closure: clamp(1 - closureGap / (diagonal * 0.32), 0, 1),
    corners: countCorners(dominantPoints, Math.max(diagonal * 0.05, 4)),
    endpointClusters: clusterEndpointCount(validStrokes, Math.max(diagonal * 0.08, 14)),
    circularity: clamp(1 - Math.sqrt(variance) / Math.max(meanRadius, 0.0001) / 0.45, 0, 1),
    fillRatio: calculateFillRatio(dominantPoints),
    parallelism: calculateParallelism(validStrokes),
    rawAngleRadians: normalizeAngleHalfPi(normalized.rawAngleRadians)
  };
}

function resolveDatacardStatus(
  score: number,
  baselineScore: number,
  captureCount: number,
  strokeCount: number
): RecognitionStatus {
  if (strokeCount === 0) {
    return "invalid";
  }

  const recognizedThreshold = captureCount > 0 ? 0.64 : baselineScore >= 0.82 ? 0.72 : 0.76;

  if (score >= recognizedThreshold) {
    return "recognized";
  }

  if (score >= 0.55) {
    return "ambiguous";
  }

  return "invalid";
}

function buildDatacardReason(
  status: RecognitionStatus,
  preset: DatacardShapePreset,
  score: number,
  captureCount: number
): string {
  if (status === "recognized") {
    return captureCount > 0
      ? `${preset.shortLabel} 카드와 저장된 연습 입력이 잘 맞습니다.`
      : `${preset.shortLabel} 카드의 그리기 기준과 현재 입력이 잘 맞습니다.`;
  }

  if (status === "ambiguous") {
    return `${preset.shortLabel} 카드와 일부 기준은 맞지만 더 많은 연습 입력이 있으면 구분이 쉬워집니다.`;
  }

  if (score === 0) {
    return "아직 비교할 입력이 없습니다.";
  }

  return `${preset.shortLabel} 카드 기준과 현재 입력의 차이가 큽니다.`;
}

function buildKeywordPattern(keywords: readonly string[]): string {
  return keywords.map(escapeRegExp).join("|");
}

function createStroke(id: string, points: Array<[number, number]>): Stroke {
  return {
    id,
    points: points.map(([x, y], index): PointSample => ({ x, y, t: index * 16 }))
  };
}

function spiralPoints(): Array<[number, number]> {
  return Array.from({ length: 34 }, (_, index) => {
    const ratio = index / 33;
    const angle = ratio * Math.PI * 4.55 - Math.PI / 2;
    const radius = 0.08 + ratio * 0.64;
    return [Math.cos(angle) * radius, Math.sin(angle) * radius];
  });
}

function starPoints(): Array<[number, number]> {
  const points: Array<[number, number]> = [];

  for (let index = 0; index <= 10; index += 1) {
    const radius = index % 2 === 0 ? 0.72 : 0.31;
    const angle = -Math.PI / 2 + index * (Math.PI / 5);
    points.push([Math.cos(angle) * radius, Math.sin(angle) * radius]);
  }

  return points;
}

function crescentPoints(): Array<[number, number]> {
  const outer = Array.from({ length: 18 }, (_, index): [number, number] => {
    const angle = -Math.PI * 0.72 + (index / 17) * Math.PI * 1.44;
    return [Math.cos(angle) * 0.58 - 0.1, Math.sin(angle) * 0.72];
  });
  const inner = Array.from({ length: 16 }, (_, index): [number, number] => {
    const angle = Math.PI * 0.65 - (index / 15) * Math.PI * 1.3;
    return [Math.cos(angle) * 0.34 + 0.2, Math.sin(angle) * 0.58];
  });

  return [...outer, ...inner];
}

function countCorners(points: readonly PointSample[], epsilon: number): number {
  if (points.length < 2) {
    return 0;
  }

  return Math.max(rdpSimplify([...points], epsilon).length - 1, 0);
}

function calculateFillRatio(points: readonly PointSample[]): number {
  if (points.length < 3) {
    return 0;
  }

  const simplified = rdpSimplify([...points], 6);
  const area = Math.abs(polygonArea(simplified));
  const box = boundingBox([...points]);
  const boxArea = Math.max(box.width * box.height, 1);

  return clamp(area / boxArea, 0, 1);
}

function calculateParallelism(strokes: readonly Stroke[]): number {
  const linearStrokes = strokes
    .filter((stroke) => stroke.points.length >= 2)
    .map((stroke) => ({
      straightness: strokeStraightness(stroke),
      angle: lineAngle(stroke)
    }));

  if (linearStrokes.length === 0) {
    return 0;
  }

  const vector = linearStrokes.reduce(
    (accumulator, item) => ({
      x: accumulator.x + Math.cos(item.angle * 2),
      y: accumulator.y + Math.sin(item.angle * 2),
      straightness: accumulator.straightness + item.straightness
    }),
    { x: 0, y: 0, straightness: 0 }
  );
  const averageAngle = Math.atan2(vector.y, vector.x) / 2;
  const meanDeviation =
    linearStrokes.reduce((sum, item) => sum + Math.abs(normalizeAngleHalfPi(item.angle - averageAngle)), 0) /
    linearStrokes.length;
  const angleScore = clamp(1 - meanDeviation / (Math.PI / 6), 0, 1);
  const straightnessScore = clamp(vector.straightness / linearStrokes.length, 0, 1);

  return angleScore * 0.6 + straightnessScore * 0.4;
}

function polygonArea(points: readonly PointSample[]): number {
  let sum = 0;

  for (let index = 0; index < points.length; index += 1) {
    const current = points[index];
    const next = points[(index + 1) % points.length];
    sum += current.x * next.y - next.x * current.y;
  }

  return sum / 2;
}

function rangeScore(value: number, minimum: number, maximum: number): number {
  if (value >= minimum && value <= maximum) {
    return 1;
  }

  const distanceToRange = value < minimum ? minimum - value : value - maximum;
  return clamp(1 - distanceToRange / Math.max(maximum - minimum, 1), 0, 1);
}

function closeness(actual: number, expected: number, tolerance: number): number {
  return clamp(1 - Math.abs(actual - expected) / Math.max(tolerance, 0.0001), 0, 1);
}

function average(values: readonly number[]): number {
  if (values.length === 0) {
    return 0;
  }

  return values.reduce((sum, value) => sum + value, 0) / values.length;
}

function cloneStrokes(strokes: readonly Stroke[]): Stroke[] {
  return strokes.map((stroke) => ({
    ...stroke,
    points: stroke.points.map((point) => ({ ...point }))
  }));
}

function cloneDatacardCapture(capture: DatacardShapeCapture): DatacardShapeCapture {
  return {
    ...capture,
    strokes: cloneStrokes(capture.strokes),
    normalizedCloud: capture.normalizedCloud.map((point) => ({ ...point })),
    features: { ...capture.features },
    featureV2: capture.featureV2
      ? {
          ...capture.featureV2,
          curvatureHistogram: [...capture.featureV2.curvatureHistogram] as [number, number, number]
        }
      : undefined,
    gestureSummary: capture.gestureSummary ? { ...capture.gestureSummary } : undefined
  };
}

function clamp(value: number, minimum: number, maximum: number): number {
  return Math.max(minimum, Math.min(maximum, value));
}

function roundMetric(value: number): number {
  return Number(value.toFixed(4));
}

function escapeRegExp(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}
