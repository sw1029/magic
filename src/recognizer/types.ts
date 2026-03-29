export type GlyphFamily = "wind" | "earth" | "fire" | "water" | "life";
export type RecognitionStatus = "recognized" | "ambiguous" | "incomplete" | "invalid";

export interface PointSample {
  x: number;
  y: number;
  t: number;
  pressure?: number;
}

export interface Stroke {
  id: string;
  points: PointSample[];
}

export interface StrokeSession {
  strokes: Stroke[];
  startedAt: number;
  endedAt?: number;
}

export interface AxisLine {
  start: { x: number; y: number };
  end: { x: number; y: number };
}

export interface RecognitionFeatures {
  strokeCount: number;
  pointCount: number;
  durationMs: number;
  pathLength: number;
  closureGap: number;
  dominantCorners: number;
  endpointClusters: number;
  circularity: number;
  fillRatio: number;
  parallelism: number;
  rawAngleRadians: number;
}

export interface RecognitionCandidate {
  family: GlyphFamily;
  score: number;
  templateDistance: number;
  notes: string[];
  completenessHint?: string;
}

export interface QualityVector {
  closure: number;
  symmetry: number;
  smoothness: number;
  tempo: number;
  overshoot: number;
  stability: number;
  rotationBias: number;
}

export interface RecognitionResult {
  status: RecognitionStatus;
  sealed: boolean;
  quality: QualityVector;
  features: RecognitionFeatures;
  candidates: RecognitionCandidate[];
  topCandidate?: RecognitionCandidate;
  canonicalFamily?: GlyphFamily;
  invalidReason?: string;
  normalizedStrokes: PointSample[][];
  symmetryAxis?: AxisLine;
  closureLine?: AxisLine;
}

export interface RecognitionLogEntry {
  id: string;
  timestamp: number;
  rawStrokeCount: number;
  rawStrokes: Stroke[];
  normalizedStrokes: PointSample[][];
  result: RecognitionResult;
}
