import {
  DATASET_SCHEMA_VERSION,
  FAMILY_TARGETS,
  OPERATOR_TARGETS,
  PUBLIC_ALLOWED_USES,
  PUBLIC_FORBIDDEN_USES,
  SYNTHETIC_ALLOWED_USES,
  TUTORIAL_ALLOWED_USES
} from "./common.mjs"
import { SYNTHETIC_PRESETS, SYNTHETIC_PRIORITY_BY_PRESET } from "./synthetic-presets.mjs"

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
]

const TUTORIAL_SOURCES = ["trace", "recall", "variation"]
const BASE_SUPPORTED_PAIRS = ["earth__fire", "water__life", "recognized__ambiguous", "other"]
const OPERATOR_SUPPORTED_PAIRS = [
  "void_cut__electric_fork",
  "ice_bar__partial_stroke",
  "steel_brace__open_box_like",
  "martial_axis__blocked_by_void_cut",
  "other"
]

export const DATASET_SPLIT_ARTIFACT_VERSION = "tiny-ml-dataset-split-v1"
export const FEATURE_SPEC_ARTIFACT_VERSION = "tiny-ml-feature-spec-v1"

export const ML_LAYER_ROLE_DEFINITIONS = Object.freeze({
  public_auxiliary: {
    role: "public_auxiliary",
    description: "encoder pretrain, normalization prior, denoising prior",
    priorities: ["public_auxiliary"],
    splits: ["pretrain"],
    usage: {
      allowed: PUBLIC_ALLOWED_USES,
      forbidden: PUBLIC_FORBIDDEN_USES
    },
    directSemanticSupervision: false
  },
  synthetic_primary: {
    role: "synthetic_primary",
    description: "domain supervision mainline for base family/operator rerank and confidence calibration",
    priorities: Object.values(SYNTHETIC_PRIORITY_BY_PRESET),
    splits: ["train", "eval", "hard_negative_eval"],
    usage: {
      allowed: SYNTHETIC_ALLOWED_USES,
      forbidden: []
    },
    directSemanticSupervision: true
  },
  tutorial_primary: {
    role: "tutorial_primary",
    description: "user adaptation, prototype bank, personalized rerank calibration, acceptance evaluation",
    priorities: ["tutorial_primary"],
    splits: ["adaptation", "acceptance_eval"],
    usage: {
      allowed: TUTORIAL_ALLOWED_USES,
      forbidden: []
    },
    directSemanticSupervision: true
  }
})

export const ML_SPLIT_POLICY = Object.freeze({
  public_auxiliary: {
    requiredSplits: ["pretrain"],
    directSemanticSupervision: false,
    notes: [
      "public auxiliary stays in auxiliary-only pretrain lanes",
      "direct family/operator supervision remains forbidden"
    ]
  },
  synthetic_primary: {
    requiredSplits: ["train", "eval", "hard_negative_eval"],
    defaultPresets: Object.entries(SYNTHETIC_PRESETS).map(([presetName, preset]) => ({
      preset: presetName,
      defaultSplit: preset.split,
      priority: SYNTHETIC_PRIORITY_BY_PRESET[presetName]
    })),
    directSemanticSupervision: true
  },
  tutorial_primary: {
    requiredSplits: ["adaptation", "acceptance_eval"],
    activation: "next-phase vector tutorial capture",
    directSemanticSupervision: true
  }
})

export const QUICKDRAW_DOMINANCE_POLICY = Object.freeze({
  reason: "Quick Draw raw count dominates current public auxiliary inventory and must not drown out dollar_family/CROHME priors.",
  appliesToRole: "public_auxiliary",
  appliesToDataset: "quickdraw",
  publicAuxiliaryContributionCap: 0.35,
  syntheticSupervisedContributionFloor: 0.65,
  quickdrawMaxShareWithinPublicAuxiliary: 0.6,
  quickdrawMaxShareAcrossCombinedOfflineBudget: 0.21,
  quickdrawMaxSamplesPerLabelPerPretrainSplit: 6000,
  quickdrawRecommendedLabels: ["circle", "triangle", "line", "square"],
  reweightStrategy: [
    "cap Quick Draw per label before pretrain feature extraction",
    "rebalance public auxiliary by source after the cap",
    "reuse public outputs only as frozen normalization/embedding features",
    "never feed public labels into direct family/operator supervision losses"
  ]
})

const BASE_FEATURE_COLUMNS = [
  categoricalFeature("candidateFamilyId", "candidate family id in the heuristic top-k window", Object.keys(FAMILY_TARGETS)),
  categoricalFeature("candidatePairId", "top confusion pair id for the current candidate window", BASE_SUPPORTED_PAIRS),
  numericFeature("candidateRank", "candidate rank within the heuristic window", integerClip(0, 2)),
  numericFeature("heuristicScore", "raw heuristic candidate score", clampRange(0, 1)),
  numericFeature("templateDistance", "distance to the family template cloud", clampRange(0, 1.25)),
  numericFeature("topScoreGap", "top-1 score minus this candidate score", clampRange(0, 1)),
  numericFeature("top1MinusTop2Margin", "top-1 minus top-2 score margin for the sample", clampRange(-1, 1)),
  numericFeature("strokeCount", "number of strokes in the input", integerClip(0, 8)),
  numericFeature("pointCount", "number of sampled points in the input", log1pClip(256)),
  numericFeature("durationMs", "elapsed drawing duration in milliseconds", log1pClip(4000)),
  numericFeature("pathLength", "total stroke path length", log1pClip(4096)),
  numericFeature("closureGap", "normalized closure gap feature", clampRange(0, 1.5)),
  numericFeature("dominantCorners", "corner count derived from simplified dominant points", integerClip(0, 8)),
  numericFeature("endpointClusters", "clustered endpoint count", integerClip(0, 8)),
  numericFeature("circularity", "loop circularity estimate", clampRange(0, 1)),
  numericFeature("fillRatio", "fill ratio for dominant points", clampRange(0, 1)),
  numericFeature("parallelism", "parallel stroke estimate", clampRange(0, 1)),
  numericFeature("rawAngleRadians", "principal raw angle in normalized space", clampRange(-Math.PI / 2, Math.PI / 2)),
  numericFeature("qualityClosure", "closure quality scalar", clampRange(0, 1)),
  numericFeature("qualitySmoothness", "smoothness quality scalar", clampRange(0, 1)),
  numericFeature("qualityStability", "stability quality scalar", clampRange(0, 1)),
  numericFeature("qualityRotationBias", "rotation bias quality scalar", clampRange(0, 1))
]

const OPERATOR_FEATURE_COLUMNS = [
  categoricalFeature("operatorId", "overlay operator id for the candidate row", Object.keys(OPERATOR_TARGETS)),
  categoricalFeature("hardPairId", "hard-negative pair id for the candidate row", OPERATOR_SUPPORTED_PAIRS),
  numericFeature("candidateRank", "candidate rank within the heuristic operator window", integerClip(0, 2)),
  numericFeature("heuristicScore", "final heuristic candidate score before ML delta", clampRange(0, 1)),
  numericFeature("baseScore", "base operator score before rerank", clampRange(0, 1)),
  numericFeature("templateDistance", "distance to the operator template cloud", clampRange(0, 1.25)),
  numericFeature("shapeConfidence", "shape-only confidence scalar", clampRange(0, 1)),
  numericFeature("topScoreGap", "top-1 operator score minus this candidate score", clampRange(0, 1)),
  booleanFeature("blockedByFlag", "1 when rule gates block this operator regardless of ML output"),
  categoricalFeature("blockedByOperator", "blocking operator required by the rule gate", ["none", "void_cut"]),
  categoricalFeature("anchorZoneId", "best anchor zone for the candidate's preferred anchor bundle", ANCHOR_ZONES),
  categoricalFeature(
    "placementAnchorZoneId",
    "best anchor zone across the full overlay frame for placement-aware rerank",
    ANCHOR_ZONES
  ),
  numericFeature("anchorScore", "anchor placement score", clampRange(0, 1)),
  numericFeature("scaleScore", "scale gate score", clampRange(0, 1)),
  numericFeature("gateStrength", "min(anchorScore, scaleScore) for weak-gate delta caps", clampRange(0, 1)),
  numericFeature("angleRadians", "operator line angle in normalized space", clampRange(-Math.PI / 2, Math.PI / 2)),
  numericFeature("scaleRatio", "operator stroke diagonal relative to the base frame diagonal", clampRange(0, 2)),
  numericFeature("straightness", "stroke straightness score", clampRange(0, 1)),
  numericFeature("corners", "simplified corner count", integerClip(0, 8)),
  numericFeature("closure", "operator closure score", clampRange(0, 1)),
  numericFeature("stackIndex", "operator insertion index within the overlay stack", integerClip(0, 8)),
  numericFeature("existingOperatorsCount", "number of existing operators in the stack", integerClip(0, 8)),
  multiHotFeature("existingOperatorsMask", "multi-hot representation of existing operators in the stack", Object.keys(OPERATOR_TARGETS)),
  booleanFeature("hasVoidCutInStack", "1 when void_cut already exists in the current stack")
]

export function buildTinyMlTrainingManifest(splitArtifactPath = "artifacts/ml/dataset-split-v1.json") {
  return {
    splitManifestArtifact: splitArtifactPath,
    phases: [
      {
        phase: "public_auxiliary_pretrain",
        roles: ["public_auxiliary"],
        splits: ["pretrain"],
        outputs: ["stroke_encoder_embedding", "normalization_prior", "sequence_representation"],
        directSemanticSupervision: false,
        reweightPolicy: QUICKDRAW_DOMINANCE_POLICY
      },
      {
        phase: "base_family_supervised",
        primaryRoles: ["synthetic_primary"],
        optionalRoles: ["tutorial_primary"],
        splits: {
          synthetic_primary: ["train", "eval", "hard_negative_eval"],
          tutorial_primary: ["adaptation", "acceptance_eval"]
        },
        rowSpec: "base_candidate_row_v1",
        outputs: ["rerank_delta", "recognized_confidence", "ambiguity_probability"],
        publicFeatureReuse: "frozen_normalization_or_embedding_only"
      },
      {
        phase: "operator_supervised",
        primaryRoles: ["synthetic_primary"],
        optionalRoles: ["tutorial_primary"],
        splits: {
          synthetic_primary: ["train", "eval", "hard_negative_eval"],
          tutorial_primary: ["adaptation", "acceptance_eval"]
        },
        rowSpec: "operator_candidate_row_v1",
        outputs: ["pairwise_rerank_delta", "operator_confidence", "false_positive_suppression"],
        publicFeatureReuse: "frozen_normalization_or_embedding_only"
      }
    ]
  }
}

export function buildTinyMlDatasetSplitManifest({ publicManifest, splitArtifactPath } = {}) {
  const trainingManifest = buildTinyMlTrainingManifest(splitArtifactPath)
  const publicSources = (publicManifest?.datasets || []).map((dataset) => ({
    id: dataset.id,
    dataset: dataset.dataset,
    layerRole: dataset.layerRole || "public_auxiliary",
    recommendedSplit: dataset.recommendedSplit || "pretrain",
    usage: dataset.usage,
    recommendedLabels: dataset.recommendedLabels || [],
    targetDir: dataset.targetDir
  }))

  return {
    artifactType: "dataset_split_manifest",
    version: "v1",
    schemaVersion: DATASET_SPLIT_ARTIFACT_VERSION,
    datasetSchemaVersion: DATASET_SCHEMA_VERSION,
    roles: ML_LAYER_ROLE_DEFINITIONS,
    splitPolicy: ML_SPLIT_POLICY,
    publicSources,
    syntheticSources: Object.entries(SYNTHETIC_PRESETS).map(([presetName, preset]) => ({
      preset: presetName,
      layerRole: "synthetic_primary",
      priority: SYNTHETIC_PRIORITY_BY_PRESET[presetName],
      defaultSplit: preset.split,
      targetKinds: presetName === "placement-shift" ? ["operator"] : ["family", "operator"]
    })),
    tutorialSources: TUTORIAL_SOURCES.map((source) => ({
      source,
      layerRole: "tutorial_primary"
    })),
    labelSummary: {
      families: Object.keys(FAMILY_TARGETS),
      operators: Object.keys(OPERATOR_TARGETS)
    },
    trainingManifest,
    reweightPolicy: QUICKDRAW_DOMINANCE_POLICY,
    invariants: [
      "public auxiliary never becomes direct family supervision",
      "public auxiliary never becomes direct operator supervision",
      "synthetic primary carries canonical base/operator rerank supervision",
      "tutorial primary activates user adaptation and acceptance evaluation only when tutorial vector capture exists"
    ]
  }
}

export function buildTinyMlFeatureSpec({ splitArtifactPath } = {}) {
  const trainingManifest = buildTinyMlTrainingManifest(splitArtifactPath)
  const featureRows = {
    base_candidate_row_v1: {
      id: "base_candidate_row_v1",
      rowUnit: "one row per heuristic family candidate inside the rerank window",
      rowSelection: {
        topK: 3,
        maxTopScoreGap: 0.18,
        candidateSource: "recognize.ts family candidates"
      },
      supervision: {
        allowedRoles: ["synthetic_primary", "tutorial_primary"],
        forbiddenRoles: ["public_auxiliary"],
        publicReuseMode: "frozen_normalization_or_embedding_only"
      },
      optionalAugmentations: [
        "public_normalization_prior_v1",
        "public_stroke_encoder_embedding_v1"
      ],
      targets: [
        targetField("targetIsCanonicalCandidate", "boolean", "1 when this candidate matches the canonical family"),
        targetField("targetRerankDelta", "numeric", "candidate score delta learned by the rerank model", {
          range: [-1, 1]
        }),
        targetField("targetRecognizedConfidence", "numeric", "calibrated recognized confidence for the top candidate", {
          range: [0, 1]
        }),
        targetField("targetAmbiguityProbability", "numeric", "ambiguity probability for close candidate windows", {
          range: [0, 1]
        })
      ],
      columns: BASE_FEATURE_COLUMNS,
      supportedPairs: BASE_SUPPORTED_PAIRS
    },
    operator_candidate_row_v1: {
      id: "operator_candidate_row_v1",
      rowUnit: "one row per heuristic operator candidate inside the rerank window",
      rowSelection: {
        topK: 3,
        hardPairWindow: 2,
        minPairGate: 0.3,
        candidateSource: "operators.ts overlay candidates"
      },
      supervision: {
        allowedRoles: ["synthetic_primary", "tutorial_primary"],
        forbiddenRoles: ["public_auxiliary"],
        publicReuseMode: "frozen_normalization_or_embedding_only"
      },
      optionalAugmentations: [
        "public_normalization_prior_v1",
        "public_stroke_encoder_embedding_v1"
      ],
      targets: [
        targetField("targetPairwiseDelta", "numeric", "pairwise rerank delta for hard-negative conflicts", {
          range: [-1, 1]
        }),
        targetField("targetOperatorConfidence", "numeric", "calibrated operator confidence for the top candidate", {
          range: [0, 1]
        }),
        targetField(
          "targetFalsePositiveSuppression",
          "numeric",
          "suppression score for off-anchor, wrong-scale, or blocked candidates",
          { range: [0, 1] }
        )
      ],
      columns: OPERATOR_FEATURE_COLUMNS,
      supportedPairs: OPERATOR_SUPPORTED_PAIRS
    }
  }

  return {
    artifactType: "feature_spec",
    modelType: "feature_spec_manifest",
    version: "v1",
    schemaVersion: FEATURE_SPEC_ARTIFACT_VERSION,
    datasetSchemaVersion: DATASET_SCHEMA_VERSION,
    featureOrder: Object.fromEntries(
      Object.entries(featureRows).map(([rowId, row]) => [rowId, row.columns.map((column) => column.name)])
    ),
    featureNormalization: Object.fromEntries(
      Object.entries(featureRows).map(([rowId, row]) => [
        rowId,
        Object.fromEntries(row.columns.map((column) => [column.name, column.normalization]))
      ])
    ),
    labelSpace: {
      baseFamily: Object.keys(FAMILY_TARGETS),
      operator: Object.keys(OPERATOR_TARGETS),
      anchorZoneId: ANCHOR_ZONES,
      tutorialSource: TUTORIAL_SOURCES
    },
    supportedPairs: {
      base: BASE_SUPPORTED_PAIRS,
      operator: OPERATOR_SUPPORTED_PAIRS
    },
    trainingManifest,
    gatePolicy: {
      canonicalSemanticAuthority: "rules_first",
      baseFamily: {
        directCanonicalPredictionForbidden: true,
        allowedOutputs: ["rerank_delta", "recognized_confidence", "ambiguity_probability"],
        acceptance: {
          familyFlipIncrease: 0
        }
      },
      operator: {
        directCanonicalPredictionForbidden: true,
        blockedByHardStop: true,
        weakGateUsesCap: {
          sourceFeatures: ["anchorScore", "scaleScore", "gateStrength"],
          offAnchorRescueForbidden: true,
          wrongScaleRescueForbidden: true
        },
        allowedOutputs: ["pairwise_rerank_delta", "operator_confidence", "false_positive_suppression"]
      },
      public_auxiliary: {
        directFamilyClassifierTraining: "forbidden",
        directOperatorClassifierTraining: "forbidden",
        semanticLabelOverride: "forbidden"
      }
    },
    featureRows,
    auxiliaryAugmentationContract: {
      role: "public_auxiliary",
      outputs: ["stroke_encoder_embedding", "normalization_prior", "sequence_representation"],
      injectionMode: "frozen_feature_only",
      directSemanticSupervision: false
    }
  }
}

export function validateTinyMlArtifacts({ splitManifest, featureSpec }) {
  const publicPhaseLeak = featureSpec.trainingManifest.phases.some((phase) =>
    Array.isArray(phase.primaryRoles) ? phase.primaryRoles.includes("public_auxiliary") : false
  )

  if (publicPhaseLeak) {
    throw new Error("public_auxiliary leaked into a direct supervision phase")
  }

  const publicRole = splitManifest.roles.public_auxiliary
  if (!publicRole || publicRole.directSemanticSupervision !== false) {
    throw new Error("public_auxiliary must remain auxiliary-only")
  }

  const publicGate = featureSpec.gatePolicy.public_auxiliary
  if (
    publicGate?.directFamilyClassifierTraining !== "forbidden" ||
    publicGate?.directOperatorClassifierTraining !== "forbidden"
  ) {
    throw new Error("public_auxiliary gate policy must forbid direct supervision")
  }
}

function numericFeature(name, description, normalization) {
  return {
    name,
    type: "numeric",
    description,
    normalization
  }
}

function categoricalFeature(name, description, values) {
  return {
    name,
    type: "categorical",
    description,
    normalization: {
      type: "one_hot",
      values
    }
  }
}

function multiHotFeature(name, description, values) {
  return {
    name,
    type: "categorical_multi",
    description,
    normalization: {
      type: "multi_hot",
      values
    }
  }
}

function booleanFeature(name, description) {
  return {
    name,
    type: "boolean",
    description,
    normalization: {
      type: "binary"
    }
  }
}

function targetField(name, type, description, extra = {}) {
  return {
    name,
    type,
    description,
    ...extra
  }
}

function clampRange(min, max) {
  return {
    type: "clamp",
    min,
    max
  }
}

function integerClip(min, max) {
  return {
    type: "integer_clip",
    min,
    max
  }
}

function log1pClip(max) {
  return {
    type: "log1p_clip",
    max
  }
}
