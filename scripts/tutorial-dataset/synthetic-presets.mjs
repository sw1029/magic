export const SYNTHETIC_PRIORITY_BY_PRESET = Object.freeze({
  bootstrap: "synthetic_bootstrap",
  "tutorial-like": "synthetic_tutorial_like",
  "hard-negative": "synthetic_hard_negative",
  "placement-shift": "synthetic_placement_shift"
})

export const SYNTHETIC_PRESETS = Object.freeze({
  bootstrap: {
    split: "train",
    sourceWeights: {
      trace: 0.45,
      recall: 0.35,
      variation: 0.2
    },
    rotationDeg: [-9, 9],
    scale: [0.9, 1.12],
    stretch: [0.9, 1.12],
    translate: [-0.08, 0.08],
    jitter: [0.006, 0.03],
    overshoot: [0, 0.08],
    closureLeak: [0, 0.08],
    partialTrim: [0, 0.08],
    cornerRounding: [0, 0.22],
    anchorShift: [0, 0.08],
    reverseStrokeOrderProbability: 0.18,
    reverseStrokeDirectionProbability: 0.35,
    pointGap: [10, 22]
  },
  "tutorial-like": {
    split: "train",
    sourceWeights: {
      trace: 0.4,
      recall: 0.4,
      variation: 0.2
    },
    rotationDeg: [-12, 12],
    scale: [0.88, 1.15],
    stretch: [0.86, 1.18],
    translate: [-0.1, 0.1],
    jitter: [0.01, 0.04],
    overshoot: [0, 0.11],
    closureLeak: [0.01, 0.12],
    partialTrim: [0, 0.12],
    cornerRounding: [0.02, 0.28],
    anchorShift: [0.01, 0.1],
    reverseStrokeOrderProbability: 0.24,
    reverseStrokeDirectionProbability: 0.45,
    pointGap: [8, 24]
  },
  "hard-negative": {
    split: "hard_negative_eval",
    sourceWeights: {
      recall: 0.4,
      variation: 0.6
    },
    rotationDeg: [-16, 16],
    scale: [0.84, 1.18],
    stretch: [0.8, 1.25],
    translate: [-0.12, 0.12],
    jitter: [0.02, 0.06],
    overshoot: [0.03, 0.14],
    closureLeak: [0.03, 0.18],
    partialTrim: [0.04, 0.18],
    cornerRounding: [0.08, 0.34],
    anchorShift: [0.04, 0.14],
    reverseStrokeOrderProbability: 0.28,
    reverseStrokeDirectionProbability: 0.55,
    pointGap: [6, 26]
  },
  "placement-shift": {
    split: "train",
    sourceWeights: {
      trace: 0.2,
      recall: 0.25,
      variation: 0.55
    },
    rotationDeg: [-10, 10],
    scale: [0.86, 1.1],
    stretch: [0.88, 1.16],
    translate: [-0.14, 0.14],
    jitter: [0.008, 0.032],
    overshoot: [0, 0.09],
    closureLeak: [0, 0.08],
    partialTrim: [0, 0.1],
    cornerRounding: [0.02, 0.24],
    anchorShift: [0.08, 0.22],
    reverseStrokeOrderProbability: 0.22,
    reverseStrokeDirectionProbability: 0.4,
    pointGap: [8, 24]
  }
})

export function resolveSyntheticPriority(presetName) {
  return SYNTHETIC_PRIORITY_BY_PRESET[presetName] || SYNTHETIC_PRIORITY_BY_PRESET.bootstrap
}
