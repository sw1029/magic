import type {
  GlyphFamily,
  QualityVector,
  RecognitionResult,
  UserInputProfile,
  UserInputProfileDelta
} from "./types";

export interface UserComfortBand {
  center: QualityVector;
  band: QualityVector;
  adjustmentStrength: number;
  personalizationMix: number;
}

export const QUALITY_VECTOR_KEYS: Array<keyof QualityVector> = [
  "closure",
  "symmetry",
  "smoothness",
  "tempo",
  "overshoot",
  "stability",
  "rotationBias"
];

const BASELINE_COMFORT_CENTER: QualityVector = {
  closure: 0.82,
  symmetry: 0.58,
  smoothness: 0.72,
  tempo: 0.76,
  overshoot: 0.62,
  stability: 0.72,
  rotationBias: 0.24
};

const BASELINE_COMFORT_BAND: QualityVector = {
  closure: 0.24,
  symmetry: 0.28,
  smoothness: 0.24,
  tempo: 0.34,
  overshoot: 0.28,
  stability: 0.26,
  rotationBias: 0.24
};

const MIN_PERSONALIZED_SAMPLES = 4;
const FULL_PERSONALIZED_SAMPLES = 18;
const MAX_ADJUSTMENT_STRENGTH = 0.3;

export function createEmptyQualityVector(): QualityVector {
  return {
    closure: 0,
    symmetry: 0,
    smoothness: 0,
    tempo: 0,
    overshoot: 0,
    stability: 0,
    rotationBias: 0
  };
}

export function createEmptyUserInputProfile(): UserInputProfile {
  return {
    version: "v1.5",
    sampleCount: 0,
    averageQuality: createEmptyQualityVector(),
    averageDurationMs: 0,
    averagePathLength: 0,
    familyCounts: createEmptyFamilyCounts(),
    updatedAt: Date.now()
  };
}

export function resolveUserComfortBand(profile?: UserInputProfile): UserComfortBand {
  const sampleCount = profile?.sampleCount ?? 0;
  const personalizationMix = clamp(
    (sampleCount - MIN_PERSONALIZED_SAMPLES) / Math.max(FULL_PERSONALIZED_SAMPLES - MIN_PERSONALIZED_SAMPLES, 1),
    0,
    1
  );
  const adjustmentStrength =
    clamp(sampleCount / Math.max(FULL_PERSONALIZED_SAMPLES, 1), 0, 1) * MAX_ADJUSTMENT_STRENGTH;
  const averageQuality = profile?.averageQuality ?? createEmptyQualityVector();
  const center = QUALITY_VECTOR_KEYS.reduce<QualityVector>((accumulator, key) => {
    accumulator[key] = mixScalar(BASELINE_COMFORT_CENTER[key], averageQuality[key], personalizationMix);
    return accumulator;
  }, createEmptyQualityVector());
  const band = QUALITY_VECTOR_KEYS.reduce<QualityVector>((accumulator, key) => {
    const drift = Math.abs(averageQuality[key] - BASELINE_COMFORT_CENTER[key]);
    const personalizedBand = clamp(BASELINE_COMFORT_BAND[key] + drift * 0.35, 0.14, 0.44);
    accumulator[key] = mixScalar(BASELINE_COMFORT_BAND[key], personalizedBand, personalizationMix);
    return accumulator;
  }, createEmptyQualityVector());

  return {
    center,
    band,
    adjustmentStrength,
    personalizationMix
  };
}

export function updateUserInputProfile(
  previousProfile: UserInputProfile | undefined,
  result: RecognitionResult
): { profile: UserInputProfile; delta: UserInputProfileDelta } {
  if (!result.canonicalFamily) {
    throw new Error("recognized canonical family is required to update the user input profile");
  }

  const profile = previousProfile ?? createEmptyUserInputProfile();
  const nextSampleCount = profile.sampleCount + 1;
  const averageQuality = blendAverage(profile.averageQuality, result.rawQuality, profile.sampleCount);
  const averageDurationMs = blendScalar(profile.averageDurationMs, result.features.durationMs, profile.sampleCount);
  const averagePathLength = blendScalar(profile.averagePathLength, result.features.pathLength, profile.sampleCount);
  const familyCounts = {
    ...profile.familyCounts,
    [result.canonicalFamily]: profile.familyCounts[result.canonicalFamily] + 1
  };

  const nextProfile: UserInputProfile = {
    version: "v1.5",
    sampleCount: nextSampleCount,
    averageQuality,
    averageDurationMs,
    averagePathLength,
    familyCounts,
    updatedAt: Date.now()
  };

  return {
    profile: nextProfile,
    delta: {
      previousSampleCount: profile.sampleCount,
      nextSampleCount,
      averageQualityDelta: subtractQualityVectors(averageQuality, profile.averageQuality),
      averageDurationDeltaMs: averageDurationMs - profile.averageDurationMs,
      averagePathLengthDelta: averagePathLength - profile.averagePathLength,
      familyIncrement: result.canonicalFamily
    }
  };
}

export function subtractQualityVectors(left: QualityVector, right: QualityVector): QualityVector {
  return QUALITY_VECTOR_KEYS.reduce<QualityVector>((accumulator, key) => {
    accumulator[key] = left[key] - right[key];
    return accumulator;
  }, createEmptyQualityVector());
}

function blendAverage(previous: QualityVector, next: QualityVector, sampleCount: number): QualityVector {
  return QUALITY_VECTOR_KEYS.reduce<QualityVector>((accumulator, key) => {
    accumulator[key] = blendScalar(previous[key], next[key], sampleCount);
    return accumulator;
  }, createEmptyQualityVector());
}

function blendScalar(previous: number, next: number, sampleCount: number): number {
  return (previous * sampleCount + next) / Math.max(sampleCount + 1, 1);
}

function mixScalar(left: number, right: number, amount: number): number {
  return left * (1 - amount) + right * amount;
}

function createEmptyFamilyCounts(): Record<GlyphFamily, number> {
  return {
    wind: 0,
    earth: 0,
    fire: 0,
    water: 0,
    life: 0
  };
}

function clamp(value: number, minimum: number, maximum: number): number {
  return Math.max(minimum, Math.min(maximum, value));
}
