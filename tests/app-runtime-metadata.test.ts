import { describe, expect, it } from "vitest";

import { buildTinyMlRuntimeMetadata } from "../src/app";
import type { OverlayRecognition, RecognitionResult } from "../src/recognizer/types";

describe("app tiny ML runtime metadata", () => {
  it("keeps actual decisions separate from shadow deltas", () => {
    const baseResult = {
      status: "recognized",
      canonicalFamily: "fire",
      topCandidate: { family: "fire", score: 0.86 },
      personalization: {
        stage: "few_shot",
        tutorialSampleCount: 8,
        featureInjectionMix: 0.32,
        thresholdBias: 0.021
      },
      shadow: {
        mode: "shadow",
        heuristicTopLabel: "fire",
        shadowTopLabel: "earth",
        actualTopLabel: "fire",
        actualStatus: "recognized",
        shadowStatus: "ambiguous",
        decisionChanged: true,
        statusChanged: true,
        candidates: []
      }
    } as unknown as RecognitionResult;
    const overlayRecognition = {
      status: "incomplete",
      topCandidate: {
        operator: "martial_axis",
        score: 0.74
      },
      personalization: {
        stage: "few_shot",
        tutorialSampleCount: 5,
        featureInjectionMix: 0.28,
        thresholdBias: 0.019
      },
      shadow: {
        mode: "shadow",
        heuristicTopLabel: "martial_axis",
        shadowTopLabel: "void_cut",
        actualTopLabel: "martial_axis",
        actualStatus: "incomplete",
        shadowStatus: "ambiguous",
        decisionChanged: true,
        statusChanged: true,
        candidates: []
      }
    } as unknown as OverlayRecognition;

    const metadata = buildTinyMlRuntimeMetadata(baseResult, overlayRecognition);

    expect(metadata.baseActualFamily).toBe("fire");
    expect(metadata.baseActualStatus).toBe("recognized");
    expect(metadata.baseShadowDecisionChanged).toBe("true");
    expect(metadata.baseShadowStatusChanged).toBe("true");
    expect(metadata.operatorActualLabel).toBe("martial_axis");
    expect(metadata.operatorActualStatus).toBe("incomplete");
    expect(metadata.operatorShadowDecisionChanged).toBe("true");
    expect(metadata.operatorShadowStatusChanged).toBe("true");
  });
});
