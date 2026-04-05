import type {
  GlyphFamily,
  OverlayOperator,
  QualityVector,
  RecognitionStatus,
  Stroke
} from "../recognizer/types";

export type DemoOutcomeMetric = "output" | "control" | "stability" | "risk";
export type DemoSealStage = "base" | "final";

export interface DemoOutcomeInput {
  family: GlyphFamily | null;
  status: RecognitionStatus;
  rawQuality: QualityVector;
  adjustedQuality: QualityVector;
  overlayOperators: OverlayOperator[];
}

export interface DemoOutcomeSummary {
  family: GlyphFamily | null;
  qualityEnabled: boolean;
  output: number;
  control: number;
  stability: number;
  risk: number;
  explanation: string[];
  summary: string;
  fingerprint: string[];
}

export interface DemoOutcomeCompare {
  family: GlyphFamily | null;
  off: DemoOutcomeSummary;
  on: DemoOutcomeSummary;
  delta: Record<DemoOutcomeMetric, number>;
}

export interface RecentSealSnapshot {
  id: string;
  stage: DemoSealStage;
  status: RecognitionStatus;
  family: GlyphFamily | null;
  overlayCount: number;
  timestamp: number;
  compare: DemoOutcomeCompare;
  previewStrokes: Stroke[];
}

const NEUTRAL_QUALITY: QualityVector = {
  closure: 0.5,
  symmetry: 0.5,
  smoothness: 0.5,
  tempo: 0.5,
  overshoot: 0.5,
  stability: 0.5,
  rotationBias: 0.5
};

const FAMILY_BASELINES: Record<GlyphFamily, Record<DemoOutcomeMetric, number>> = {
  wind: { output: 0.64, control: 0.56, stability: 0.5, risk: 0.38 },
  earth: { output: 0.48, control: 0.78, stability: 0.8, risk: 0.18 },
  fire: { output: 0.82, control: 0.46, stability: 0.42, risk: 0.66 },
  water: { output: 0.6, control: 0.7, stability: 0.72, risk: 0.28 },
  life: { output: 0.58, control: 0.66, stability: 0.64, risk: 0.3 }
};

const OVERLAY_OUTCOME_DELTAS: Record<OverlayOperator, Record<DemoOutcomeMetric, number>> = {
  steel_brace: { output: -0.03, control: 0.13, stability: 0.1, risk: -0.06 },
  electric_fork: { output: 0.14, control: -0.06, stability: -0.07, risk: 0.1 },
  ice_bar: { output: -0.02, control: 0.11, stability: 0.08, risk: -0.05 },
  soul_dot: { output: 0.05, control: 0.06, stability: 0.03, risk: -0.02 },
  void_cut: { output: 0.08, control: -0.09, stability: -0.06, risk: 0.13 },
  martial_axis: { output: 0.1, control: 0.04, stability: 0.02, risk: 0.04 }
};

export function buildDemoOutcomeCompare(input: DemoOutcomeInput): DemoOutcomeCompare {
  const off = buildDemoOutcomeSummary(input, false);
  const on = buildDemoOutcomeSummary(input, true);

  return {
    family: input.family,
    off,
    on,
    delta: {
      output: roundMetric(on.output - off.output),
      control: roundMetric(on.control - off.control),
      stability: roundMetric(on.stability - off.stability),
      risk: roundMetric(on.risk - off.risk)
    }
  };
}

export function createRecentSealSnapshot(
  id: string,
  stage: DemoSealStage,
  timestamp: number,
  input: DemoOutcomeInput,
  previewStrokes: Stroke[]
): RecentSealSnapshot {
  return {
    id,
    stage,
    status: input.status,
    family: input.family,
    overlayCount: input.overlayOperators.length,
    timestamp,
    compare: buildDemoOutcomeCompare(input),
    previewStrokes
  };
}

export function getChangedOutcomeMetrics(compare: DemoOutcomeCompare): DemoOutcomeMetric[] {
  return (["output", "control", "stability", "risk"] as const).filter(
    (metric) => Math.abs(compare.delta[metric]) >= 0.005
  );
}

function buildDemoOutcomeSummary(input: DemoOutcomeInput, qualityEnabled: boolean): DemoOutcomeSummary {
  const family = input.family;
  const baseline = family ? FAMILY_BASELINES[family] : { output: 0.5, control: 0.5, stability: 0.5, risk: 0.5 };
  const overlayDelta = sumOverlayDeltas(input.overlayOperators);
  const qualityDelta = qualityEnabled ? buildQualityOutcomeDelta(input.adjustedQuality) : zeroOutcomeDelta();
  const output = clampMetric(baseline.output + overlayDelta.output + qualityDelta.output);
  const control = clampMetric(baseline.control + overlayDelta.control + qualityDelta.control);
  const stability = clampMetric(baseline.stability + overlayDelta.stability + qualityDelta.stability);
  const risk = clampMetric(baseline.risk + overlayDelta.risk + qualityDelta.risk);
  const qualityDiff = buildQualityDelta(input.adjustedQuality, NEUTRAL_QUALITY);

  return {
    family,
    qualityEnabled,
    output,
    control,
    stability,
    risk,
    fingerprint: buildFingerprint({ output, control, stability, risk }),
    summary: buildOutcomeSummary(family, qualityEnabled, qualityDelta),
    explanation: buildExplanationLines(input, qualityEnabled, qualityDelta, qualityDiff)
  };
}

function sumOverlayDeltas(operators: OverlayOperator[]): Record<DemoOutcomeMetric, number> {
  return operators.reduce<Record<DemoOutcomeMetric, number>>(
    (accumulator, operator) => {
      const delta = OVERLAY_OUTCOME_DELTAS[operator];
      accumulator.output += delta.output;
      accumulator.control += delta.control;
      accumulator.stability += delta.stability;
      accumulator.risk += delta.risk;
      return accumulator;
    },
    zeroOutcomeDelta()
  );
}

function buildQualityOutcomeDelta(quality: QualityVector): Record<DemoOutcomeMetric, number> {
  const centered = buildQualityDelta(quality, NEUTRAL_QUALITY);

  return {
    output: roundMetric(centered.tempo * 0.34 + centered.smoothness * 0.12 + centered.overshoot * 0.12 + centered.closure * 0.08),
    control: roundMetric(
      centered.closure * 0.26 +
        centered.symmetry * 0.16 +
        centered.stability * 0.14 -
        centered.overshoot * 0.24 -
        centered.rotationBias * 0.14
    ),
    stability: roundMetric(
      centered.stability * 0.28 +
        centered.smoothness * 0.14 +
        centered.closure * 0.08 -
        centered.tempo * 0.1 -
        centered.rotationBias * 0.22
    ),
    risk: roundMetric(
      -centered.stability * 0.28 -
        centered.closure * 0.1 +
        centered.tempo * 0.08 +
        centered.overshoot * 0.24 +
        centered.rotationBias * 0.22
    )
  };
}

function buildQualityDelta(left: QualityVector, right: QualityVector): QualityVector {
  return {
    closure: roundMetric(left.closure - right.closure),
    symmetry: roundMetric(left.symmetry - right.symmetry),
    smoothness: roundMetric(left.smoothness - right.smoothness),
    tempo: roundMetric(left.tempo - right.tempo),
    overshoot: roundMetric(left.overshoot - right.overshoot),
    stability: roundMetric(left.stability - right.stability),
    rotationBias: roundMetric(left.rotationBias - right.rotationBias)
  };
}

function buildExplanationLines(
  input: DemoOutcomeInput,
  qualityEnabled: boolean,
  qualityDelta: Record<DemoOutcomeMetric, number>,
  qualityDiff: QualityVector
): string[] {
  const familyLine = input.family ? `종류 고정: ${input.family}` : "종류 대기";
  const overlayLine =
    input.overlayOperators.length > 0
      ? `추가 효과 ${input.overlayOperators.length}개가 함께 기록됩니다.`
      : "추가 효과는 아직 없습니다.";

  if (!qualityEnabled) {
    return [
      familyLine,
      "품질 반영을 끄면 기본 결과감만 보여 줍니다.",
      overlayLine
    ];
  }

  return [
    familyLine,
    `품질 반영으로 output ${formatSigned(qualityDelta.output)}, control ${formatSigned(qualityDelta.control)}, stability ${formatSigned(qualityDelta.stability)}, risk ${formatSigned(qualityDelta.risk)} 만큼 달라집니다.`,
    `닫힘 ${formatSigned(qualityDiff.closure)}, 속도 ${formatSigned(qualityDiff.tempo)}, 안정감 ${formatSigned(qualityDiff.stability)}, 기울기 ${formatSigned(qualityDiff.rotationBias)}가 이 변화에 영향을 줍니다.`,
    overlayLine
  ];
}

function buildOutcomeSummary(
  family: GlyphFamily | null,
  qualityEnabled: boolean,
  qualityDelta: Record<DemoOutcomeMetric, number>
): string {
  if (!family) {
    return "기본 모양을 먼저 고정하면 결과 비교가 열립니다.";
  }

  if (!qualityEnabled) {
    return "같은 모양은 같은 종류로 유지하고, 지금은 기본 결과감만 보여 줍니다.";
  }

  const strongest = strongestMetric(qualityDelta);
  const direction = strongest.value >= 0 ? "올리고" : "낮추고";
  return `같은 모양은 같은 종류로 유지하고, 품질 반영은 ${strongest.metric}만 ${direction} 결과감에 차이를 만듭니다.`;
}

function strongestMetric(delta: Record<DemoOutcomeMetric, number>): { metric: DemoOutcomeMetric; value: number } {
  return (Object.entries(delta) as Array<[DemoOutcomeMetric, number]>).reduce(
    (best, current) => (Math.abs(current[1]) > Math.abs(best.value) ? { metric: current[0], value: current[1] } : best),
    { metric: "output" as DemoOutcomeMetric, value: delta.output }
  );
}

function buildFingerprint(summary: Record<DemoOutcomeMetric, number>): string[] {
  return [
    summary.output >= 0.68 ? "sharp" : summary.output >= 0.5 ? "steady" : "soft",
    summary.control >= 0.68 ? "controlled" : summary.stability >= 0.64 ? "stable" : "loose",
    summary.risk >= 0.62 ? "risky" : summary.risk >= 0.42 ? "tense" : "safe"
  ];
}

function zeroOutcomeDelta(): Record<DemoOutcomeMetric, number> {
  return {
    output: 0,
    control: 0,
    stability: 0,
    risk: 0
  };
}

function clampMetric(value: number): number {
  return Math.max(0.05, Math.min(0.95, roundMetric(value)));
}

function roundMetric(value: number): number {
  return Math.round(value * 1000) / 1000;
}

function formatSigned(value: number): string {
  return value > 0 ? `+${value.toFixed(2)}` : value.toFixed(2);
}
