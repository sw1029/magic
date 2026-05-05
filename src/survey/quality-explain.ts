import type { QualityVector } from "../recognizer/types";

export interface QualityExplanation {
  key: keyof QualityVector;
  label: string;
  value: number;
  summary: string;
}

export const SURVEY_QUALITY_KEYS: Array<keyof QualityVector> = [
  "closure",
  "smoothness",
  "tempo",
  "stability",
  "rotationBias"
];

export function buildQualityExplanations(quality: QualityVector): QualityExplanation[] {
  return SURVEY_QUALITY_KEYS.map((key) => ({
    key,
    label: qualityMetricLabel(key),
    value: clamp(quality[key], 0, 1),
    summary: qualityMetricSummary(key, quality[key])
  }));
}

export function qualityMetricLabel(key: keyof QualityVector): string {
  switch (key) {
    case "closure":
      return "닫힘";
    case "symmetry":
      return "균형";
    case "smoothness":
      return "매끄러움";
    case "tempo":
      return "속도감";
    case "overshoot":
      return "불필요한 흔들림 억제";
    case "stability":
      return "안정성";
    case "rotationBias":
      return "기울기 영향";
  }
}

function qualityMetricSummary(key: keyof QualityVector, value: number): string {
  const high = value >= 0.68;

  switch (key) {
    case "closure":
      return high ? "끝점이 잘 맞아 닫힌 도형으로 읽힙니다." : "끝점 간격이 커서 미완성처럼 보일 수 있습니다.";
    case "smoothness":
      return high ? "선 흐름이 안정적이라 의도한 모양을 따라가기 쉽습니다." : "급격한 꺾임이나 떨림이 많아 판정 여지가 커집니다.";
    case "tempo":
      return high ? "빠르게 입력되어 실행감이 강하게 반영됩니다." : "느린 입력이라 출력감보다 제어감 중심으로 읽힙니다.";
    case "stability":
      return high ? "속도 변화가 작아 재현 가능한 입력으로 보입니다." : "멈춤과 속도 변화가 커서 위험도 설명이 필요합니다.";
    case "rotationBias":
      return high ? "기울기가 커서 결과 위험도나 방향감에 영향을 줍니다." : "기울기 영향이 작아 종류 판정에는 큰 부담이 없습니다.";
    case "symmetry":
    case "overshoot":
      return high ? "보조 품질 값이 안정 범위입니다." : "보조 품질 값이 낮아 결과감 조정에 영향을 줍니다.";
  }
}

function clamp(value: number, min: number, max: number): number {
  return Math.max(min, Math.min(max, value));
}
