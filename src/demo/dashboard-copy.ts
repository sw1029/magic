import type { GlyphFamily, OverlayOperator, RecognitionStatus } from "../recognizer/types";

export type DashboardTone = "recognized" | "ready" | "waiting" | "invalid" | "ambiguous" | "incomplete";

export function dashboardFamilyName(family?: GlyphFamily | string | null): string {
  switch (family) {
    case "wind":
      return "바람 모양";
    case "earth":
      return "땅 모양";
    case "fire":
      return "불꽃 모양";
    case "water":
      return "물 모양";
    case "life":
      return "생명 모양";
    case undefined:
    case null:
    case "none":
      return "아직 없음";
    default:
      return String(family);
  }
}

export function dashboardOperatorName(operator?: OverlayOperator | string | null): string {
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
    case undefined:
    case null:
    case "none":
      return "없음";
    default:
      return String(operator);
  }
}

export function dashboardStatusLabel(status?: RecognitionStatus | "waiting" | string | null): string {
  switch (status) {
    case "recognized":
      return "인정됨";
    case "ambiguous":
      return "헷갈림";
    case "incomplete":
      return "아직 부족함";
    case "invalid":
      return "읽기 어려움";
    case "waiting":
    case undefined:
    case null:
      return "대기";
    default:
      return String(status);
  }
}

export function dashboardStatusTone(status?: RecognitionStatus | "waiting" | string | null): DashboardTone {
  switch (status) {
    case "recognized":
      return "recognized";
    case "ambiguous":
      return "ambiguous";
    case "incomplete":
      return "incomplete";
    case "invalid":
      return "invalid";
    default:
      return "waiting";
  }
}

export function dashboardQualityName(key: string): string {
  switch (key) {
    case "closure":
      return "닫힘";
    case "symmetry":
      return "균형";
    case "smoothness":
      return "부드러움";
    case "tempo":
      return "속도감";
    case "overshoot":
      return "삐져나감";
    case "stability":
      return "안정감";
    case "rotationBias":
      return "기울기";
    default:
      return key;
  }
}

export function describeDashboardStatus(status: RecognitionStatus, topLabel?: GlyphFamily): string {
  switch (status) {
    case "recognized":
      return `${dashboardFamilyName(topLabel)}으로 안정적으로 읽혔습니다.`;
    case "ambiguous":
      return "여러 모양 후보가 가까워 헷갈리는 상태입니다.";
    case "incomplete":
      return "모양의 닫힘이나 선 정보가 아직 부족합니다.";
    case "invalid":
      return "입력이 너무 적거나 읽기 어려운 상태입니다.";
    default:
      return "입력을 기다리고 있습니다.";
  }
}

export function ensureDashboardUserCopy(copy: string): string {
  return copy
    .replace(/threshold/gi, "인정 기준")
    .replace(/rerank/gi, "후보 다시 보기")
    .replace(/gate/gi, "안전 보류")
    .replace(/shadow/gi, "보조 판독")
    .replace(/fixture/gi, "예시 데이터")
    .replace(/synthetic/gi, "만든 입력")
    .replace(/confusion matrix/gi, "헷갈림 지도")
    .replace(/histogram/gi, "분포 막대")
    .replace(/scatter plot/gi, "점 분포");
}
