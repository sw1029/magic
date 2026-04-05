import { getChangedOutcomeMetrics } from "./outcome-summary";
import type { DemoOutcomeCompare, DemoOutcomeMetric } from "./outcome-summary";
import type { CompiledSealResult, OverlayRecognition, QualityVector, RecognitionResult } from "../recognizer/types";

export interface ExplainNote {
  tone: "neutral" | "positive" | "caution";
  text: string;
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

const QUALITY_TO_OUTCOME_WEIGHTS: Record<DemoOutcomeMetric, Partial<Record<keyof QualityVector, number>>> = {
  output: {
    tempo: 0.34,
    smoothness: 0.12,
    overshoot: 0.12,
    closure: 0.08
  },
  control: {
    closure: 0.26,
    symmetry: 0.16,
    stability: 0.14,
    overshoot: -0.24,
    rotationBias: -0.14
  },
  stability: {
    stability: 0.28,
    smoothness: 0.14,
    closure: 0.08,
    tempo: -0.1,
    rotationBias: -0.22
  },
  risk: {
    stability: -0.28,
    closure: -0.1,
    tempo: 0.08,
    overshoot: 0.24,
    rotationBias: 0.22
  }
};

export function buildExplainNotes(
  result: RecognitionResult,
  compare: DemoOutcomeCompare,
  qualityInfluence: boolean,
  overlayLive: OverlayRecognition | null,
  compilePreview: CompiledSealResult | null
): ExplainNote[] {
  const notes = [...buildStatusNotes(result), ...buildEvidenceNotes(result)];

  if (qualityInfluence) {
    notes.push(...buildQualityNotes(result, compare));
  } else {
    notes.push({
      tone: "neutral",
      text: "품질 반영을 끄면 기본 결과감만 유지합니다."
    });
  }

  if (overlayLive?.operator && overlayLive.status === "recognized") {
    notes.push({
      tone: "neutral",
      text: `현재 추가 효과는 ${readableOperator(overlayLive.operator)}로 읽히고 있습니다.`
    });
  } else if (compilePreview && compilePreview.overlayOperators.length > 0) {
    notes.push({
      tone: "neutral",
      text: `최종 결과에는 같은 기본 모양 위에 추가 효과 ${compilePreview.overlayOperators.length}개가 유지됩니다.`
    });
  }

  return notes.slice(0, 5);
}

function buildStatusNotes(result: RecognitionResult): ExplainNote[] {
  const family = result.canonicalFamily ?? result.topCandidate?.family;

  switch (result.status) {
    case "recognized":
      return family
        ? [{ tone: "positive", text: `모양이 ${familyLabel(family)}형으로 안정적으로 고정됐습니다.` }]
        : [{ tone: "positive", text: "모양이 안정적인 종류로 고정됐습니다." }];
    case "ambiguous":
      return [
        {
          tone: "caution",
          text: family ? `${familyLabel(family)}형에 가깝지만 아직 다른 종류와 차이가 작습니다.` : "현재 모양은 아직 두 종류 사이에 걸쳐 있습니다."
        }
      ];
    case "incomplete":
      return [
        {
          tone: "caution",
          text:
            result.rawQuality.closure < 0.82
              ? "아직 닫힘이 부족해서 완성된 모양으로 보지 않습니다."
              : family
                ? `${familyLabel(family)}형에 가깝지만 핵심 구조 하나가 아직 빠져 있습니다.`
                : "모양은 가깝지만 핵심 구조 하나가 아직 빠져 있습니다."
        }
      ];
    case "invalid":
    default:
      return [
        {
          tone: "caution",
          text:
            result.rawQuality.closure < 0.55
              ? "닫힘이 많이 열려 있어서 아직 안정적인 모양으로 읽지 않습니다."
              : "현재 선은 아직 어떤 기본 모양에도 충분히 가깝지 않습니다."
        }
      ];
  }
}

function buildEvidenceNotes(result: RecognitionResult): ExplainNote[] {
  const topScore = result.topCandidate?.score;
  const margin = candidateMargin(result);
  const closure = result.rawQuality.closure;
  const notes: ExplainNote[] = [];

  if (topScore !== undefined) {
    if (result.status === "recognized") {
      notes.push({
        tone: "neutral",
        text: `가장 가까운 후보 점수 ${toFixed(topScore)}가 다음 후보와의 차이 ${toFixed(margin)}를 확보했습니다.`
      });
    } else if (result.status === "ambiguous") {
      notes.push({
        tone: "neutral",
        text: `가장 가까운 후보 점수는 ${toFixed(topScore)}지만, 다음 후보와의 차이 ${toFixed(margin)}가 아직 작습니다.`
      });
    } else if (result.status === "incomplete") {
      notes.push({
        tone: "neutral",
        text: `가장 가까운 후보 점수는 ${toFixed(topScore)}로 나쁘지 않지만, 모양이 아직 덜 닫혀 있습니다.`
      });
    } else {
      notes.push({
        tone: "neutral",
        text: `가장 가까운 후보 점수 ${toFixed(topScore)}가 아직 판정 기준에 못 미칩니다.`
      });
    }
  }

  notes.push({
    tone: closure >= 0.82 ? "positive" : closure >= 0.65 ? "neutral" : "caution",
    text:
      closure >= 0.82
        ? "닫힘이 충분해서 현재 판정을 안정적으로 받쳐 줍니다."
        : closure >= 0.65
          ? "닫힘은 거의 맞지만 끝부분이 조금 열려 있습니다."
          : "닫힘이 부족해서 recognizer가 보수적으로 판정합니다."
  });

  return notes;
}

function buildQualityNotes(result: RecognitionResult, compare: DemoOutcomeCompare): ExplainNote[] {
  const changedMetrics = getChangedOutcomeMetrics(compare);

  if (changedMetrics.length === 0) {
    return [
      {
        tone: "neutral",
        text: "품질 반영을 켰지만 현재 입력 습관이 기본 결과감과 거의 비슷합니다."
      }
    ];
  }

  const positiveMetric = pickStrongestMetric(compare, "positive");
  const negativeMetric = pickStrongestMetric(compare, "negative");
  const notes: ExplainNote[] = [];

  if (positiveMetric && negativeMetric) {
    notes.push({
      tone: "neutral",
      text: `품질 반영으로 ${metricLabel(positiveMetric)}은 올라가고 ${metricLabel(negativeMetric)}은 내려갔습니다.`
    });
  } else if (positiveMetric) {
    notes.push({
      tone: "positive",
      text: `품질 반영으로 ${metricLabel(positiveMetric)}이 올라갔습니다.`
    });
  } else if (negativeMetric) {
    notes.push({
      tone: "caution",
      text: `품질 반영으로 ${metricLabel(negativeMetric)}이 내려갔습니다.`
    });
  }

  const driverMetric = positiveMetric ?? negativeMetric ?? changedMetrics[0];
  const drivers = describeDrivers(result.adjustedQuality, driverMetric, compare.delta[driverMetric]);

  if (drivers) {
    notes.push({
      tone: "neutral",
      text: drivers
    });
  }

  return notes;
}

function describeDrivers(
  adjustedQuality: QualityVector,
  metric: DemoOutcomeMetric,
  directionValue: number
): string | null {
  const centered = centerQuality(adjustedQuality);
  const weights = QUALITY_TO_OUTCOME_WEIGHTS[metric];
  const expectedDirection = directionValue >= 0 ? 1 : -1;
  const drivers = (Object.entries(weights) as Array<[keyof QualityVector, number]>)
    .map(([qualityKey, weight]) => ({
      qualityKey,
      contribution: centered[qualityKey] * weight
    }))
    .filter((item) => Math.sign(item.contribution) === expectedDirection && Math.abs(item.contribution) >= 0.015)
    .sort((left, right) => Math.abs(right.contribution) - Math.abs(left.contribution))
    .slice(0, 2)
    .map((item) => qualityLabel(item.qualityKey));

  if (drivers.length === 0) {
    return null;
  }

  return `${joinWords(drivers)} 때문에 ${metricLabel(metric)}이 ${directionValue >= 0 ? "올라갔습니다" : "낮아졌습니다"}.`;
}

function centerQuality(quality: QualityVector): QualityVector {
  return {
    closure: quality.closure - NEUTRAL_QUALITY.closure,
    symmetry: quality.symmetry - NEUTRAL_QUALITY.symmetry,
    smoothness: quality.smoothness - NEUTRAL_QUALITY.smoothness,
    tempo: quality.tempo - NEUTRAL_QUALITY.tempo,
    overshoot: quality.overshoot - NEUTRAL_QUALITY.overshoot,
    stability: quality.stability - NEUTRAL_QUALITY.stability,
    rotationBias: quality.rotationBias - NEUTRAL_QUALITY.rotationBias
  };
}

function pickStrongestMetric(compare: DemoOutcomeCompare, direction: "positive" | "negative"): DemoOutcomeMetric | null {
  const entries = (Object.entries(compare.delta) as Array<[DemoOutcomeMetric, number]>).filter(([, value]) =>
    direction === "positive" ? value > 0.005 : value < -0.005
  );

  if (entries.length === 0) {
    return null;
  }

  return entries.sort((left, right) => Math.abs(right[1]) - Math.abs(left[1]))[0][0];
}

function qualityLabel(key: keyof QualityVector): string {
  switch (key) {
    case "closure":
      return "닫힘";
    case "symmetry":
      return "균형";
    case "smoothness":
      return "매끄러움";
    case "tempo":
      return "속도";
    case "overshoot":
      return "흔들림";
    case "stability":
      return "안정감";
    case "rotationBias":
      return "기울기";
    default:
      return key;
  }
}

function metricLabel(metric: DemoOutcomeMetric): string {
  switch (metric) {
    case "output":
      return "출력감";
    case "control":
      return "제어감";
    case "stability":
      return "안정감";
    case "risk":
      return "위험도";
    default:
      return metric;
  }
}

function familyLabel(family: string): string {
  switch (family) {
    case "wind":
      return "바람";
    case "earth":
      return "땅";
    case "fire":
      return "불꽃";
    case "water":
      return "물";
    case "life":
      return "생명";
    default:
      return family;
  }
}

function readableOperator(operator: string): string {
  switch (operator) {
    case "steel_brace":
      return "버팀 장식";
    case "electric_fork":
      return "갈래 번개";
    case "ice_bar":
      return "얼음 막대";
    case "soul_dot":
      return "혼 점";
    case "void_cut":
      return "공백 절단";
    case "martial_axis":
      return "축선 장식";
    default:
      return operator;
  }
}

function joinWords(words: string[]): string {
  if (words.length <= 1) {
    return words[0] ?? "";
  }

  if (words.length === 2) {
    return `${words[0]} and ${words[1]}`;
  }

  return `${words.slice(0, -1).join(", ")}, and ${words[words.length - 1]}`;
}

function candidateMargin(result: RecognitionResult): number {
  const top = result.candidates[0]?.score ?? 0;
  const second = result.candidates[1]?.score ?? 0;
  return top - second;
}

function toFixed(value: number): string {
  return value.toFixed(2);
}
