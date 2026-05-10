import {
  evaluateMagicCardSetCompatibility,
  getBuiltInMagicCardSetSignature,
  listBuiltInMagicCards,
  type MagicCard,
  type MagicCardId,
  type MagicCardSetCompatibility,
  type MagicFamilyCard,
  type MagicOperatorCard
} from "./datacards";
import {
  summarizeMagicDatacardValidation,
  validateMagicDatacard,
  validateMagicDatacardSet,
  type MagicDatacardSet,
  type MagicDatacardValidationIssue,
  type MagicDatacardValidationSummary
} from "./datacard-schema";

export type MagicDatacardRegistryMode = "built_in" | "preview";
export type MagicDatacardLoadMode = "full_set" | "patch";
export type MagicDatacardPreviewCompatibilityStatus =
  | "ready"
  | "invalid_labels"
  | "invalid_coverage"
  | "duplicate_target";

export interface MagicDatacardLoadInput {
  rawJson: string;
  sourceName?: string;
  loadMode?: MagicDatacardLoadMode;
}

export interface MagicDatacardPreviewCompatibility {
  ok: boolean;
  status: MagicDatacardPreviewCompatibilityStatus;
  userMessage: string;
  coverage: {
    familyCount: number;
    operatorCount: number;
    cardCount: number;
  };
}

export interface MagicDatacardPreviewRegistry {
  mode: "preview";
  cardSetId: string;
  cardSetHash?: string;
  sourceName?: string;
  cards: readonly MagicCard[];
  compatibility: MagicDatacardPreviewCompatibility;
  runtimeCompatibility: MagicCardSetCompatibility;
  validation: MagicDatacardValidationSummary;
}

export interface MagicDatacardLoadResult {
  ok: boolean;
  mode: MagicDatacardRegistryMode;
  loadMode: MagicDatacardLoadMode;
  registry: MagicDatacardPreviewRegistry | null;
  issues: readonly MagicDatacardValidationIssue[];
  userMessage: string;
}

export function loadMagicDatacardPreview(input: MagicDatacardLoadInput): MagicDatacardLoadResult {
  const loadMode = input.loadMode ?? "full_set";
  const parsed = parseJson(input.rawJson);

  if (!parsed.ok) {
    return {
      ok: false,
      mode: "built_in",
      loadMode,
      registry: null,
      issues: [createIssue("json", "invalid_json", "JSON을 읽을 수 없습니다. 쉼표와 따옴표를 확인해 주세요.")],
      userMessage: "JSON을 읽을 수 없습니다. 쉼표와 따옴표를 확인해 주세요."
    };
  }

  return loadMode === "patch"
    ? loadMagicDatacardPatchPreview(parsed.value, input.sourceName)
    : loadMagicDatacardFullSetPreview(parsed.value, input.sourceName);
}

export function evaluateMagicDatacardPreviewCompatibility(cards: readonly MagicCard[]): MagicDatacardPreviewCompatibility {
  const duplicateTarget = findDuplicateTarget(cards);

  if (duplicateTarget) {
    return buildPreviewCompatibility("duplicate_target", cards, `같은 대상을 두 번 정의했습니다: ${duplicateTarget}`);
  }

  const expected = getBuiltInMagicCardSetSignature();
  const familyLabels = cards.filter((card): card is MagicFamilyCard => card.kind === "family").map((card) => card.family);
  const operatorLabels = cards.filter((card): card is MagicOperatorCard => card.kind === "operator").map((card) => card.operator);
  const cardIds = cards.map((card) => card.id);
  const familyCoverage = arrayEquals(familyLabels, expected.familyLabels);
  const operatorCoverage = arrayEquals(operatorLabels, expected.operatorLabels);
  const cardIdCoverage = arrayEquals(cardIds, expected.cardIds);

  if (!familyCoverage || !operatorCoverage || !cardIdCoverage) {
    return buildPreviewCompatibility(
      "invalid_coverage",
      cards,
      "현재 판정판의 5개 기본 모양과 6개 추가 효과를 모두 포함해야 합니다."
    );
  }

  return buildPreviewCompatibility("ready", cards, "미리보기에서 안내 문구와 상상 비교만 바뀝니다. 실제 판정은 그대로입니다.");
}

function loadMagicDatacardFullSetPreview(input: unknown, sourceName?: string): MagicDatacardLoadResult {
  const result = validateMagicDatacardSet(input);
  const validation = summarizeMagicDatacardValidation(result);

  if (!result.valid || !result.value) {
    return {
      ok: false,
      mode: "built_in",
      loadMode: "full_set",
      registry: null,
      issues: result.issues,
      userMessage: summarizeLoadFailure(result.issues)
    };
  }

  return buildLoadSuccess(result.value, validation, "full_set", sourceName);
}

function loadMagicDatacardPatchPreview(input: unknown, sourceName?: string): MagicDatacardLoadResult {
  if (!isRecord(input)) {
    return buildPatchFailure([createIssue("cardSet", "invalid_type", "카드 묶음은 객체여야 합니다.")]);
  }

  const rawCards = input.cards;
  if (!Array.isArray(rawCards) || rawCards.length === 0) {
    return buildPatchFailure([createIssue("cardSet.cards", "missing_required", "patch에는 하나 이상의 카드가 필요합니다.")]);
  }

  const issues: MagicDatacardValidationIssue[] = [];
  const patchCards: MagicCard[] = [];

  rawCards.forEach((rawCard, index) => {
    const cardResult = validateMagicDatacard(rawCard, `cardSet.cards[${index}]`);
    issues.push(...cardResult.issues);
    if (cardResult.valid && cardResult.value) {
      patchCards.push(cardResult.value);
    }
  });

  const duplicateTarget = findDuplicateTarget(patchCards);
  if (duplicateTarget) {
    issues.push(createIssue("cardSet.cards", "duplicate_target", `같은 대상을 두 번 정의했습니다: ${duplicateTarget}`));
  }

  if (issues.some((issue) => issue.severity === "error")) {
    return buildPatchFailure(issues);
  }

  const cardSetId = typeof input.cardSetId === "string" && input.cardSetId.trim().length > 0
    ? input.cardSetId
    : "local-authoring/patch-preview";
  const cardSetHash = typeof input.cardSetHash === "string" && input.cardSetHash.trim().length > 0 ? input.cardSetHash : undefined;
  const mergedSet: MagicDatacardSet = {
    cardSetId,
    ...(cardSetHash ? { cardSetHash } : {}),
    cards: mergePatchCards(patchCards)
  };
  const fullSetResult = validateMagicDatacardSet(mergedSet);
  const validation = summarizeMagicDatacardValidation({
    valid: fullSetResult.valid,
    issues: [...issues, ...fullSetResult.issues]
  });

  if (!fullSetResult.valid || !fullSetResult.value) {
    return {
      ok: false,
      mode: "built_in",
      loadMode: "patch",
      registry: null,
      issues: validation.messages.map((message, index) => createIssue(`patch[${index}]`, "patch_merge_failed", message)),
      userMessage: "patch를 현재 카드 묶음에 합친 뒤 다시 확인해야 합니다."
    };
  }

  return buildLoadSuccess(fullSetResult.value, validation, "patch", sourceName);
}

function buildLoadSuccess(
  value: MagicDatacardSet,
  validation: MagicDatacardValidationSummary,
  loadMode: MagicDatacardLoadMode,
  sourceName?: string
): MagicDatacardLoadResult {
  const compatibility = evaluateMagicDatacardPreviewCompatibility(value.cards);

  if (!compatibility.ok) {
    return {
      ok: false,
      mode: "built_in",
      loadMode,
      registry: null,
      issues: [createIssue("cardSet.cards", compatibility.status, compatibility.userMessage)],
      userMessage: compatibility.userMessage
    };
  }

  return {
    ok: true,
    mode: "preview",
    loadMode,
    registry: {
      mode: "preview",
      cardSetId: value.cardSetId,
      cardSetHash: value.cardSetHash,
      sourceName,
      cards: cloneCards(value.cards),
      compatibility,
      runtimeCompatibility: evaluateMagicCardSetCompatibility({
        cardSetId: value.cardSetId,
        cardSetHash: value.cardSetHash,
        familyLabels: value.cards.filter((card): card is MagicFamilyCard => card.kind === "family").map((card) => card.family),
        operatorLabels: value.cards.filter((card): card is MagicOperatorCard => card.kind === "operator").map((card) => card.operator),
        cardIds: value.cards.map((card) => card.id)
      }),
      validation
    },
    issues: [],
    userMessage: compatibility.userMessage
  };
}

function buildPatchFailure(issues: readonly MagicDatacardValidationIssue[]): MagicDatacardLoadResult {
  return {
    ok: false,
    mode: "built_in",
    loadMode: "patch",
    registry: null,
    issues,
    userMessage: summarizeLoadFailure(issues)
  };
}

function summarizeLoadFailure(issues: readonly MagicDatacardValidationIssue[]): string {
  if (issues.some((issue) => issue.code === "invalid_json")) {
    return "JSON을 읽을 수 없습니다. 쉼표와 따옴표를 확인해 주세요.";
  }

  if (issues.some((issue) => issue.code === "invalid_family_label" || issue.code === "invalid_operator_label" || issue.code === "invalid_dependency")) {
    return "현재 판정판에 없는 도형/장식 이름입니다.";
  }

  if (issues.some((issue) => issue.code === "duplicate_target")) {
    return "같은 도형/장식을 두 카드가 동시에 가리키고 있습니다.";
  }

  if (issues.some((issue) => issue.code === "invalid_card_count" || issue.code === "missing_target")) {
    return "현재 판정판의 5개 기본 모양과 6개 추가 효과를 모두 포함해야 합니다.";
  }

  return "카드에 필요한 안내 문구나 대상 정보가 빠져 있습니다.";
}

function mergePatchCards(patchCards: readonly MagicCard[]): MagicCard[] {
  const patchById = new Map<MagicCardId, MagicCard>(patchCards.map((card) => [card.id, card]));
  return listBuiltInMagicCards().map((card) => cloneCard(patchById.get(card.id) ?? card));
}

function findDuplicateTarget(cards: readonly MagicCard[]): string | undefined {
  const seen = new Set<string>();
  for (const card of cards) {
    const key = `${card.target.kind}:${card.target.label}`;
    if (seen.has(key)) {
      return key;
    }
    seen.add(key);
  }
  return undefined;
}

function buildPreviewCompatibility(
  status: MagicDatacardPreviewCompatibilityStatus,
  cards: readonly MagicCard[],
  userMessage: string
): MagicDatacardPreviewCompatibility {
  return {
    ok: status === "ready",
    status,
    userMessage,
    coverage: {
      familyCount: cards.filter((card) => card.kind === "family").length,
      operatorCount: cards.filter((card) => card.kind === "operator").length,
      cardCount: cards.length
    }
  };
}

function parseJson(rawJson: string): { ok: true; value: unknown } | { ok: false } {
  try {
    return { ok: true, value: JSON.parse(rawJson) as unknown };
  } catch {
    return { ok: false };
  }
}

function cloneCards(cards: readonly MagicCard[]): MagicCard[] {
  return cards.map(cloneCard);
}

function cloneCard(card: MagicCard): MagicCard {
  return structuredClone(card) as MagicCard;
}

function createIssue(path: string, code: string, message: string): MagicDatacardValidationIssue {
  return { path, code, severity: "error", message };
}

function arrayEquals(left: readonly string[], right: readonly string[]): boolean {
  return left.length === right.length && left.every((value, index) => value === right[index]);
}

function isRecord(input: unknown): input is Record<string, unknown> {
  return typeof input === "object" && input !== null && !Array.isArray(input);
}
