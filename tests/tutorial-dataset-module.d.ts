declare module "../scripts/tutorial-dataset/convert-tutorial-captures.mjs" {
  import type {
    GlyphFamily,
    OverlayAnchorZoneId,
    OverlayOperator,
    Stroke,
    TutorialCaptureSource
  } from "../src/recognizer/types"

  export const TUTORIAL_EXPORT_CONTRACT_VERSION: string

  export interface TutorialExportRecord {
    dataset: "tutorial"
    kind: "family" | "operator"
    label: GlyphFamily | OverlayOperator
    split: "adaptation" | "acceptance_eval"
    source: TutorialCaptureSource
    metadata: {
      userPartitionKey: string
      sessionKey: string
      captureOrdinal: number
      baseSnapshot: unknown
      operatorContext: unknown
    }
  }

  export interface TutorialExportManifest {
    contractVersion: string
    exportMode: "locked" | "auto_holdout"
    lockedSplit: "adaptation" | "acceptance_eval" | null
    userPartitionKeys: string[]
    sessionKeys: string[]
    counts: {
      bySplit: Record<string, number>
    }
  }

  export interface TutorialCaptureLike {
    id?: string
    kind: "family" | "operator"
    expectedFamily?: GlyphFamily
    expectedOperator?: OverlayOperator
    strokes: Stroke[]
    source?: TutorialCaptureSource
    timestamp?: number
    baseSnapshot?: {
      centroid: { x: number; y: number }
      bounds: { minX: number; maxX: number; minY: number; maxY: number; width: number; height: number }
      diagonal: number
      axisAngleRadians: number
    } | null
    operatorContext?: {
      stackIndex: number
      existingOperators: OverlayOperator[]
      strokeBounds: { minX: number; maxX: number; minY: number; maxY: number; width: number; height: number }
      anchorZoneId?: OverlayAnchorZoneId
      anchorScore?: number
      scaleRatio?: number
      angleRadians?: number
    } | null
  }

  export interface TutorialExportPlan {
    records: TutorialExportRecord[]
    manifest: TutorialExportManifest
  }

  export function buildTutorialExportPlan(
    payload: TutorialCaptureLike[] | { captures: TutorialCaptureLike[]; version?: string; updatedAt?: number; userPartitionKey?: string; sessionKey?: string },
    options?: {
      outputPath?: string
      adaptationOut?: string
      acceptanceOut?: string
      manifestOut?: string
      split?: "adaptation" | "acceptance_eval"
      userPartitionKey?: string
      sessionKey?: string
    }
  ): TutorialExportPlan
}
