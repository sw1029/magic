export type GlyphFamily = "wind" | "earth" | "fire" | "water" | "life";
export type OverlayOperator =
  | "steel_brace"
  | "electric_fork"
  | "ice_bar"
  | "soul_dot"
  | "void_cut"
  | "martial_axis";
export type TutorialTargetKind = "family" | "operator";
export type TutorialCaptureSource = "trace" | "recall" | "variation";
export type OverlayAnchorZoneId =
  | "upper_left"
  | "upper"
  | "upper_right"
  | "left"
  | "core"
  | "right"
  | "lower_left"
  | "lower"
  | "lower_right";
export type RecognitionStatus = "recognized" | "ambiguous" | "incomplete" | "invalid";
export type RitualPhase = "base" | "overlay" | "final";

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

export interface StrokeBounds {
  minX: number;
  maxX: number;
  minY: number;
  maxY: number;
  width: number;
  height: number;
}

export interface OverlayAnchorZone {
  id: OverlayAnchorZoneId;
  center: { x: number; y: number };
  radius: number;
  bounds: StrokeBounds;
}

export interface OverlayReferenceFrame {
  centroid: { x: number; y: number };
  bounds: StrokeBounds;
  diagonal: number;
  axisAngleRadians: number;
  anchorZones: OverlayAnchorZone[];
  referenceLines: {
    horizontal: AxisLine;
    vertical: AxisLine;
    ascendingDiagonal: AxisLine;
  };
}

export interface OverlayRecognitionContext {
  referenceFrame: OverlayReferenceFrame;
  existingOperators: OverlayOperator[];
  overlaySession?: StrokeSession;
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
  rawQuality: QualityVector;
  adjustedQuality: QualityVector;
  qualityAdjustment: QualityVector;
  features: RecognitionFeatures;
  candidates: RecognitionCandidate[];
  topCandidate?: RecognitionCandidate;
  canonicalFamily?: GlyphFamily;
  invalidReason?: string;
  normalizedStrokes: PointSample[][];
  symmetryAxis?: AxisLine;
  closureLine?: AxisLine;
}

export interface TutorialCapture {
  id: string;
  kind: TutorialTargetKind;
  expectedFamily?: GlyphFamily;
  expectedOperator?: OverlayOperator;
  strokes: Stroke[];
  source: TutorialCaptureSource;
  timestamp: number;
  baseSnapshot?: TutorialBaseSnapshot;
  operatorContext?: TutorialOperatorContext;
}

export interface TutorialBaseSnapshot {
  centroid: { x: number; y: number };
  bounds: StrokeBounds;
  diagonal: number;
  axisAngleRadians: number;
}

export interface TutorialOperatorContext {
  stackIndex: number;
  existingOperators: OverlayOperator[];
  strokeBounds: StrokeBounds;
  anchorZoneId?: OverlayAnchorZoneId;
  anchorScore?: number;
  scaleRatio?: number;
  angleRadians?: number;
}

export interface FamilyPrototype {
  family: GlyphFamily;
  normalizedClouds: PointSample[][];
  averageFeatures: Partial<RecognitionFeatures>;
  sampleCount: number;
}

export interface OperatorPrototype {
  operator: OverlayOperator;
  normalizedClouds: PointSample[][];
  sampleCount: number;
  averageAngleRadians?: number;
  averageScaleRatio?: number;
  averageAnchorZoneId?: OverlayAnchorZoneId;
  averageStraightness?: number;
  averageCorners?: number;
  averageClosure?: number;
  averageStackIndex?: number;
  existingOperatorBiases?: Partial<Record<OverlayOperator, number>>;
}

export interface TutorialConfusionPair {
  left: string;
  right: string;
  weight: number;
}

export interface UserShapeProfile {
  tutorialSampleCount: number;
  familyPrototypes: Partial<Record<GlyphFamily, FamilyPrototype>>;
  operatorPrototypes: Partial<Record<OverlayOperator, OperatorPrototype>>;
  confusionPairs: TutorialConfusionPair[];
  updatedAt: number;
}

export interface RecognitionCalibration {
  userPrototypeWeight: number;
  rerankStrength: number;
  confidenceBias: number;
}

export interface TutorialProfileStore {
  version: "v1.5";
  captures: TutorialCapture[];
  shapeProfile: UserShapeProfile;
  calibration: RecognitionCalibration;
  updatedAt: number;
}

export interface UserInputProfile {
  version: "v1.5";
  sampleCount: number;
  averageQuality: QualityVector;
  averageDurationMs: number;
  averagePathLength: number;
  familyCounts: Record<GlyphFamily, number>;
  updatedAt: number;
  tutorialProfile?: UserShapeProfile & {
    recognitionCalibration?: RecognitionCalibration;
    calibration?: RecognitionCalibration;
  };
  shapeProfile?: UserShapeProfile & {
    recognitionCalibration?: RecognitionCalibration;
    calibration?: RecognitionCalibration;
  };
  recognitionCalibration?: RecognitionCalibration;
  calibration?: RecognitionCalibration;
}

export interface UserInputProfileDelta {
  previousSampleCount: number;
  nextSampleCount: number;
  averageQualityDelta: QualityVector;
  averageDurationDeltaMs: number;
  averagePathLengthDelta: number;
  familyIncrement?: GlyphFamily;
}

export interface OverlayRecognitionCandidate {
  operator: OverlayOperator;
  score: number;
  templateDistance: number;
  notes: string[];
  anchorZoneId?: OverlayAnchorZoneId;
  blockedBy?: OverlayOperator;
  completenessHint?: string;
}

export interface OverlayRecognition {
  strokeId: string;
  status: RecognitionStatus;
  operator?: OverlayOperator;
  candidates: OverlayRecognitionCandidate[];
  topCandidate?: OverlayRecognitionCandidate;
  invalidReason?: string;
  normalizedStrokes: PointSample[][];
  bounds: StrokeBounds;
  debugAxis?: AxisLine;
  anchorZoneId?: OverlayAnchorZoneId;
}

export interface OverlayStrokeRecord {
  stroke: Stroke;
  recognition: OverlayRecognition;
}

export interface CompiledSealResult {
  phase: "final";
  baseFamily: GlyphFamily;
  baseResult: RecognitionResult;
  overlayOperators: OverlayRecognition[];
  rawQuality: QualityVector;
  adjustedQuality: QualityVector;
  qualityAdjustment: QualityVector;
  profileDelta?: UserInputProfileDelta;
  compiledAt: number;
  summary: string;
}

export interface BaseSealLogEntry {
  kind: "base_seal";
  id: string;
  timestamp: number;
  rawStrokeCount: number;
  rawStrokes: Stroke[];
  normalizedStrokes: PointSample[][];
  result: RecognitionResult;
  profileDelta?: UserInputProfileDelta;
}

export interface CompileSealLogEntry {
  kind: "compile_seal";
  id: string;
  timestamp: number;
  rawStrokeCount: number;
  rawStrokes: Stroke[];
  normalizedStrokes: PointSample[][];
  result: RecognitionResult;
  overlayRecords: OverlayStrokeRecord[];
  compiled: CompiledSealResult;
}

export type RecognitionLogEntry = BaseSealLogEntry | CompileSealLogEntry;
