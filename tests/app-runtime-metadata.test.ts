import { describe, expect, it } from "vitest";

import { buildDatacardAuthoringDisplay, buildMagicWhatIfGuideModel, buildTinyMlRuntimeMetadata, buildTutorialPersonalizationPolicyMetadata, resolveWebUiPageFromHash } from "../src/app";
import { listBuiltInMagicCards } from "../src/recognizer/datacards";
import { loadMagicDatacardPreview } from "../src/recognizer/datacard-loader";
import { createEmptyTutorialProfileStore, hydrateTutorialProfileStore } from "../src/recognizer/tutorial-profile";
import type { OverlayRecognition, RecognitionResult } from "../src/recognizer/types";

describe("app tiny ML runtime metadata", () => {
  it("resolves hash routes for split web UI pages", () => {
    expect(resolveWebUiPageFromHash("")).toBe("test");
    expect(resolveWebUiPageFromHash("#/tutorial")).toBe("tutorial");
    expect(resolveWebUiPageFromHash("#ml")).toBe("ml");
    expect(resolveWebUiPageFromHash("#/quality?detail=1")).toBe("quality");
    expect(resolveWebUiPageFromHash("#/dashboard")).toBe("dashboard");
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
  it("summarizes tutorial personalization safety without raw gate wording", () => {
    const fresh = buildTutorialPersonalizationPolicyMetadata(createEmptyTutorialProfileStore());
    const legacy = buildTutorialPersonalizationPolicyMetadata(
      hydrateTutorialProfileStore({ version: "v1.5", captures: [], updatedAt: 1 })
    );

    expect(fresh.reasonLabels).toContain("카드 세트 일치");
    expect(legacy.needsBackfill).toBe(true);
    expect(legacy.reasonLabels.join(" ")).toContain("카드 정보");
    expect([...fresh.reasonLabels, ...legacy.reasonLabels, fresh.statusLabel, legacy.statusLabel].join(" ")).not.toMatch(/threshold|gate|rerank/i);
  });


  it("summarizes datacard authoring without changing recognition wording", () => {
    const waiting = buildDatacardAuthoringDisplay(null);

    expect(waiting.statusTone).toBe("waiting");
    expect(waiting.issueLines.join(" ")).toContain("미리보기");

    const fireCard = listBuiltInMagicCards().find((card) => card.kind === "family" && card.family === "fire");
    expect(fireCard).toBeTruthy();

    const result = loadMagicDatacardPreview({
      rawJson: JSON.stringify({
        cards: [
          {
            ...fireCard!,
            tutorial: {
              ...fireCard!.tutorial,
              title: "세 꼭짓점의 불꽃",
              summary: "압축과 회전에 따른 상상 비교를 우선합니다."
            }
          }
        ]
      }),
      loadMode: "patch"
    });
    const ready = buildDatacardAuthoringDisplay(result);
    const copy = [...ready.summaryRows.flat(), ...ready.issueLines].join(" ");

    expect(ready.statusTone).toBe("recognized");
    expect(copy).toContain("패치 병합");
    expect(copy).toContain("실제 인식");
    expect(copy).not.toMatch(/threshold|gate|rerank/i);
  });

  it("builds a non-mutating what-if guide for structure relation and placement", () => {
    const cards = buildMagicWhatIfGuideModel();
    const copy = cards.map((card) => `${card.dimensionLabel} ${card.title} ${card.summary}`).join(" ");

    expect(cards.length).toBeGreaterThanOrEqual(3);
    expect(cards.every((card) => card.nonMutating)).toBe(true);
    expect(copy).toContain("관계");
    expect(copy).toContain("구조");
    expect(copy).toContain("배치");
    expect(copy).toContain("현재 판정");
    expect(copy).not.toMatch(/threshold|gate|rerank/i);
  });

});
