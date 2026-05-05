import { describe, expect, it } from "vitest"

import {
  TUTORIAL_EXPORT_CONTRACT_VERSION,
  buildTutorialExportPlan
} from "../scripts/tutorial-dataset/convert-tutorial-captures.mjs"

interface TutorialExportRecord {
  kind: "family" | "operator"
  label: string
  split: "adaptation" | "acceptance_eval"
  metadata: {
    userPartitionKey: string
    sessionKey: string
    captureOrdinal: number
    baseSnapshot: unknown
    operatorContext: unknown
  }
}

describe("tutorial capture export helper", () => {
  it("derives adaptation and acceptance splits with deterministic holdout and provenance", () => {
    const captures = [
      makeFamilyCapture("fire", 1_000),
      makeFamilyCapture("fire", 2_000),
      makeFamilyCapture("fire", 3_000),
      makeFamilyCapture("fire", 4_000),
      makeFamilyCapture("fire", 5_000),
      makeOperatorCapture("void_cut", 2_100_000)
    ]

    const plan = buildTutorialExportPlan(
      {
        version: "v1.5",
        updatedAt: 2_100_000,
        userPartitionKey: "user-a",
        captures
      },
      {
        outputPath: "tmp/tutorial-dataset/tutorial-export.ndjson",
        adaptationOut: "tmp/tutorial-dataset/tutorial-adaptation.ndjson",
        acceptanceOut: "tmp/tutorial-dataset/tutorial-acceptance.ndjson",
        manifestOut: "tmp/tutorial-dataset/tutorial-export.manifest.json"
      }
    )

    expect(plan.records).toHaveLength(6)
    expect(plan.manifest.contractVersion).toBe(TUTORIAL_EXPORT_CONTRACT_VERSION)
    expect(plan.manifest.counts.bySplit.adaptation).toBe(5)
    expect(plan.manifest.counts.bySplit.acceptance_eval).toBe(1)
    expect(plan.manifest.userPartitionKeys).toEqual(["user-a"])
    expect(plan.manifest.sessionKeys).toEqual(["session-0001", "session-0002"])

    const acceptanceRecord = plan.records.find((record: TutorialExportRecord) => record.split === "acceptance_eval")
    expect(acceptanceRecord?.kind).toBe("family")
    expect(acceptanceRecord?.label).toBe("fire")
    expect(acceptanceRecord?.metadata.userPartitionKey).toBe("user-a")
    expect(acceptanceRecord?.metadata.sessionKey).toBe("session-0001")
    expect(acceptanceRecord?.metadata.captureOrdinal).toBe(5)

    const operatorRecord = plan.records.find((record: TutorialExportRecord) => record.kind === "operator")
    expect(operatorRecord?.split).toBe("adaptation")
    expect(operatorRecord?.metadata.baseSnapshot).toBeTruthy()
    expect(operatorRecord?.metadata.operatorContext).toBeTruthy()
  })

  it("supports locked split mode for all captures", () => {
    const plan = buildTutorialExportPlan(
      {
        captures: [makeFamilyCapture("water", 5_000), makeOperatorCapture("ice_bar", 6_000)]
      },
      {
        split: "adaptation",
        userPartitionKey: "manual-user",
        sessionKey: "session-fixed"
      }
    )

    expect(plan.records.every((record: TutorialExportRecord) => record.split === "adaptation")).toBe(true)
    expect(plan.manifest.exportMode).toBe("locked")
    expect(plan.manifest.lockedSplit).toBe("adaptation")
    expect(plan.manifest.userPartitionKeys).toEqual(["manual-user"])
    expect(plan.manifest.sessionKeys).toEqual(["session-fixed"])
  })
})

function makeFamilyCapture(expectedFamily: "fire" | "water", timestamp: number) {
  return {
    id: `${expectedFamily}-${timestamp}`,
    kind: "family" as const,
    expectedFamily,
    strokes: [
      {
        id: `${expectedFamily}-stroke`,
        points: [
          { x: 0, y: 0, t: 0 },
          { x: 10, y: 10, t: 16 },
          { x: 20, y: 0, t: 32 }
        ]
      }
    ],
    source: "trace" as const,
    timestamp
  }
}

function makeOperatorCapture(expectedOperator: "void_cut" | "ice_bar", timestamp: number) {
  return {
    id: `${expectedOperator}-${timestamp}`,
    kind: "operator" as const,
    expectedOperator,
    strokes: [
      {
        id: `${expectedOperator}-stroke`,
        points: [
          { x: 0, y: 0, t: 0 },
          { x: 12, y: -12, t: 16 }
        ]
      }
    ],
    source: "variation" as const,
    timestamp,
    baseSnapshot: {
      centroid: { x: 10, y: 10 },
      bounds: { minX: 0, maxX: 20, minY: 0, maxY: 20, width: 20, height: 20 },
      diagonal: 28.28,
      axisAngleRadians: 0
    },
    operatorContext: {
      stackIndex: 1,
      existingOperators: ["void_cut" as const],
      strokeBounds: { minX: 0, maxX: 12, minY: -12, maxY: 0, width: 12, height: 12 },
      anchorZoneId: "upper_right" as const,
      anchorScore: 0.84,
      scaleRatio: 0.33,
      angleRadians: -0.78
    }
  }
}
