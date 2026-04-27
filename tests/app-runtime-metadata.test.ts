import { describe, expect, it } from "vitest";

import { buildTinyMlRuntimeMetadata, resolveWebUiPageFromHash } from "../src/app";
import type { OverlayRecognition, RecognitionResult } from "../src/recognizer/types";

describe("app tiny ML runtime metadata", () => {
  it("resolves hash routes for split web UI pages", () => {
    expect(resolveWebUiPageFromHash("")).toBe("test");
    expect(resolveWebUiPageFromHash("#/tutorial")).toBe("tutorial");
    expect(resolveWebUiPageFromHash("#ml")).toBe("ml");
    expect(resolveWebUiPageFromHash("#/quality?detail=1")).toBe("quality");
    expect(resolveWebUiPageFromHash("#/unknown")).toBe("test");
  });

  it("keeps actual decisions separate from shadow deltas", () => {
    const baseResult = {
      status: "recognized",
      canonicalFamily: "fire",
      topCandidate: { family: "fire", score: 0.86 },
      personalization: {
        stage: "few_shot",
        tutorialSampleCount: 8,
        featureInjectionMix: 0.32,
        thresholdBias: 0.021,
        effectiveThresholdBias: 0.018,
        mlConfidenceGate: 0.86,
        mlActualGate: "confidence_guard"
      },
      shadow: {
        mode: "shadow",
        heuristicTopLabel: "fire",
        shadowTopLabel: "earth",
        personalizedShadowTopLabel: "fire",
        actualTopLabel: "fire",
        actualStatus: "recognized",
        shadowStatus: "ambiguous",
        personalizedShadowStatus: "recognized",
        decisionChanged: true,
        statusChanged: true,
        personalizedDecisionChanged: false,
        personalizedStatusChanged: false,
        personalizationStage: "few_shot",
        personalizationMix: 0.32,
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
        thresholdBias: 0.019,
        effectiveThresholdBias: 0.012,
        mlConfidenceGate: 0.64,
        mlActualGate: "suppression"
      },
      shadow: {
        mode: "shadow",
        heuristicTopLabel: "martial_axis",
        shadowTopLabel: "void_cut",
        personalizedShadowTopLabel: "martial_axis",
        actualTopLabel: "martial_axis",
        actualStatus: "incomplete",
        shadowStatus: "ambiguous",
        personalizedShadowStatus: "incomplete",
        decisionChanged: true,
        statusChanged: true,
        personalizedDecisionChanged: false,
        personalizedStatusChanged: false,
        personalizationStage: "few_shot",
        personalizationMix: 0.28,
        candidates: []
      }
    } as unknown as OverlayRecognition;

    const metadata = buildTinyMlRuntimeMetadata(baseResult, overlayRecognition);

    expect(metadata.baseActualFamily).toBe("fire");
    expect(metadata.baseActualStatus).toBe("recognized");
    expect(metadata.baseShadowDecisionChanged).toBe("true");
    expect(metadata.baseShadowStatusChanged).toBe("true");
    expect(metadata.basePersonalizationMix).toBe("0.320");
    expect(metadata.baseEffectiveThresholdBias).toBe("0.018");
    expect(metadata.baseMlConfidenceGate).toBe("0.860");
    expect(metadata.baseMlActualGate).toBe("confidence_guard");
    expect(metadata.basePersonalizedShadowTopLabel).toBe("fire");
    expect(metadata.basePersonalizedShadowDecisionChanged).toBe("false");
    expect(metadata.basePersonalizedShadowStatusChanged).toBe("false");
    expect(metadata.operatorActualLabel).toBe("martial_axis");
    expect(metadata.operatorActualStatus).toBe("incomplete");
    expect(metadata.operatorShadowDecisionChanged).toBe("true");
    expect(metadata.operatorShadowStatusChanged).toBe("true");
    expect(metadata.operatorPersonalizationMix).toBe("0.280");
    expect(metadata.operatorEffectiveThresholdBias).toBe("0.012");
    expect(metadata.operatorMlConfidenceGate).toBe("0.640");
    expect(metadata.operatorMlActualGate).toBe("suppression");
    expect(metadata.operatorPersonalizedShadowTopLabel).toBe("martial_axis");
    expect(metadata.operatorPersonalizedShadowDecisionChanged).toBe("false");
    expect(metadata.operatorPersonalizedShadowStatusChanged).toBe("false");
  });
});
