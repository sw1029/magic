import type {
  GlyphFamily,
  OverlayOperator,
  OverlayAnchorZoneId,
  Stroke,
  TutorialCaptureSource
} from "../../src/recognizer/types"

export const TUTORIAL_EXPORT_CONTRACT_VERSION: string
export const DEFAULT_TUTORIAL_USER_PARTITION_KEY: string
export const DEFAULT_SESSION_GAP_MINUTES: number
export const DEFAULT_ACCEPTANCE_EVERY: number
export const DEFAULT_ACCEPTANCE_MIN_GROUP_SIZE: number

export interface TutorialExportRecord {
  schemaVersion: string
  dataset: "tutorial"
  kind: "family" | "operator"
  label: GlyphFamily | OverlayOperator
  split: "adaptation" | "acceptance_eval"
  priority: "tutorial_primary"
  source: TutorialCaptureSource
  usage: {
    allowed: string[]
    forbidden: string[]
  }
  strokes: Array<Array<{ x: number; y: number; t: number }>>
  normalizedStrokes: Array<Array<{ x: number; y: number; t: number }>>
  metadata: {
    layerRole: "tutorial_primary"
    contractVersion: string
    captureId: string
    timestamp: number | null
    tutorialPriorityRank: number
    exportMode: "locked" | "auto_holdout"
    userPartitionKey: string
    sessionKey: string
    captureOrdinal: number
    sessionOrdinal: number
    inputOrdinal: number
    storeVersion: string | null
    storeUpdatedAt: number | null
    baseSnapshot: {
      centroid: { x: number; y: number }
      bounds: { minX: number; maxX: number; minY: number; maxY: number; width: number; height: number }
      diagonal: number
      axisAngleRadians: number
    } | null
    operatorContext: {
      stackIndex: number
      existingOperators: OverlayOperator[]
      strokeBounds: { minX: number; maxX: number; minY: number; maxY: number; width: number; height: number }
      anchorZoneId?: OverlayAnchorZoneId
      anchorScore?: number
      scaleRatio?: number
      angleRadians?: number
    } | null
  }
}

export interface TutorialExportManifest {
  artifactType: "tutorial_export_manifest"
  version: "v1"
  contractVersion: string
  datasetSchemaVersion: string
  inputPath: string | null
  outputPath: string | null
  outputs: {
    adaptationOut: string | null
    acceptanceOut: string | null
    manifestOut: string | null
  }
  exportMode: "locked" | "auto_holdout"
  lockedSplit: "adaptation" | "acceptance_eval" | null
  acceptancePolicy:
    | { mode: "locked"; split: "adaptation" | "acceptance_eval" | null }
    | { mode: "auto_holdout"; acceptanceEvery: number; acceptanceMinGroupSize: number; sessionGapMinutes: number }
  storeVersion: string | null
  storeUpdatedAt: number | null
  userPartitionKeys: string[]
  sessionKeys: string[]
  counts: {
    total: number
    bySplit: Record<string, number>
    byKind: Record<string, number>
    byLabel: Record<string, number>
    bySource: Record<string, number>
  }
}

export interface TutorialExportPlan {
  records: TutorialExportRecord[]
  outputs: {
    out: string | null
    adaptationOut?: string
    acceptanceOut?: string
    manifestOut?: string
  }
  manifest: TutorialExportManifest
}

export interface TutorialExportOptions {
  inputPath?: string
  outputPath?: string
  adaptationOut?: string
  acceptanceOut?: string
  manifestOut?: string
  split?: "adaptation" | "acceptance_eval"
  acceptanceEvery?: number
  acceptanceMinGroupSize?: number
  userPartitionKey?: string
  sessionKey?: string
  sessionGapMinutes?: number
}

export interface TutorialCaptureLike {
  id?: string
  kind: "family" | "operator"
  expectedFamily?: GlyphFamily
  expectedOperator?: OverlayOperator
  strokes: Stroke[]
  source?: TutorialCaptureSource
  timestamp?: number
  baseSnapshot?: TutorialExportRecord["metadata"]["baseSnapshot"]
  operatorContext?: TutorialExportRecord["metadata"]["operatorContext"]
}

export function runCli(argv: string[]): Promise<void>
export function buildTutorialExportPlan(
  payload: TutorialCaptureLike[] | { captures: TutorialCaptureLike[]; version?: string; updatedAt?: number; userPartitionKey?: string; sessionKey?: string; metadata?: Record<string, unknown> },
  options?: TutorialExportOptions
): TutorialExportPlan
