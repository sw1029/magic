import {
  BUILT_IN_MAGIC_FAMILY_LABELS,
  BUILT_IN_MAGIC_OPERATOR_LABELS,
  type MagicCard,
  type MagicCardKind,
  type MagicFamilyCard,
  type MagicOperatorCard,
  type MagicTutorialMetadata
} from "./datacards";
import type { GlyphFamily, OverlayAnchorZoneId, OverlayOperator } from "./types";

export type MagicDatacardValidationSeverity = "error" | "warning";

export interface MagicDatacardValidationIssue {
  path: string;
  code: string;
  severity: MagicDatacardValidationSeverity;
  message: string;
}

export interface MagicDatacardValidationResult<TValue = unknown> {
  valid: boolean;
  issues: readonly MagicDatacardValidationIssue[];
  value?: TValue;
}

export interface MagicDatacardValidationSummary {
  valid: boolean;
  issueCount: number;
  errorCount: number;
  warningCount: number;
  codes: readonly string[];
  messages: readonly string[];
}

export interface MagicDatacardSet {
  cardSetId: string;
  cardSetHash?: string;
  cards: readonly MagicCard[];
}

const VALID_ANCHOR_ZONES = [
  "upper_left",
  "upper",
  "upper_right",
  "left",
  "core",
  "right",
  "lower_left",
  "lower",
  "lower_right"
] as const satisfies readonly OverlayAnchorZoneId[];

const VALID_FAMILY_LABELS = new Set<string>(BUILT_IN_MAGIC_FAMILY_LABELS);
const VALID_OPERATOR_LABELS = new Set<string>(BUILT_IN_MAGIC_OPERATOR_LABELS);
const VALID_ANCHOR_ZONE_LABELS = new Set<string>(VALID_ANCHOR_ZONES);
const EXPECTED_CARD_COUNT = BUILT_IN_MAGIC_FAMILY_LABELS.length + BUILT_IN_MAGIC_OPERATOR_LABELS.length;

export function validateMagicDatacard(input: unknown, path = "card"): MagicDatacardValidationResult<MagicCard> {
  const issues: MagicDatacardValidationIssue[] = [];
  const card = validateCard(input, path, issues);

  return buildResult(card, issues);
}

export function validateMagicDatacardSet(input: unknown, path = "cardSet"): MagicDatacardValidationResult<MagicDatacardSet> {
  const issues: MagicDatacardValidationIssue[] = [];

  if (!isRecord(input)) {
    addIssue(issues, path, "invalid_type", `${path} must be an object.`);
    return buildResult<MagicDatacardSet>(undefined, issues);
  }

  const cardSetId = requireNonEmptyString(input, "cardSetId", `${path}.cardSetId`, issues);
  const cardSetHash = readOptionalNonEmptyString(input, "cardSetHash", `${path}.cardSetHash`, issues);
  const rawCards = input.cards;
  const cards: MagicCard[] = [];

  if (!Array.isArray(rawCards)) {
    addIssue(issues, `${path}.cards`, "invalid_type", "cards must be an array.");
  } else {
    if (rawCards.length !== EXPECTED_CARD_COUNT) {
      addIssue(
        issues,
        `${path}.cards`,
        "invalid_card_count",
        `cards must contain exactly ${EXPECTED_CARD_COUNT} entries for the closed magic datacard label set.`
      );
    }

    validateNoDuplicateRawCardIds(rawCards, path, issues);
    validateNoDuplicateRawTargets(rawCards, path, issues);

    rawCards.forEach((entry, index) => {
      const card = validateCard(entry, `${path}.cards[${index}]`, issues);
      if (card) {
        cards.push(card);
      }
    });

    validateClosedCardSetCoverage(cards, path, issues);
  }

  const value = cardSetId && rawCards && Array.isArray(rawCards) && issues.every((issue) => issue.severity !== "error")
    ? { cardSetId, ...(cardSetHash ? { cardSetHash } : {}), cards }
    : undefined;

  return buildResult(value, issues);
}

export function summarizeMagicDatacardValidation(result: MagicDatacardValidationResult): MagicDatacardValidationSummary {
  const errorCount = result.issues.filter((issue) => issue.severity === "error").length;
  const warningCount = result.issues.filter((issue) => issue.severity === "warning").length;
  const codes = [...new Set(result.issues.map((issue) => issue.code))];

  return {
    valid: result.valid,
    issueCount: result.issues.length,
    errorCount,
    warningCount,
    codes,
    messages: result.issues.map((issue) => `${issue.path}: ${issue.message}`)
  };
}

function validateCard(input: unknown, path: string, issues: MagicDatacardValidationIssue[]): MagicCard | undefined {
  if (!isRecord(input)) {
    addIssue(issues, path, "invalid_type", `${path} must be an object.`);
    return undefined;
  }

  const kind = readKind(input, `${path}.kind`, issues);
  const id = requireNonEmptyString(input, "id", `${path}.id`, issues);
  const label = requireNonEmptyString(input, "label", `${path}.label`, issues);
  const shortLabel = requireNonEmptyString(input, "shortLabel", `${path}.shortLabel`, issues);
  const tutorial = validateTutorial(input.tutorial, `${path}.tutorial`, issues);
  const target = validateTarget(input.target, kind, `${path}.target`, issues);

  if (kind === "family") {
    const family = readFamilyLabel(input.family, `${path}.family`, issues);

    if (id && family && id !== `family:${family}`) {
      addIssue(issues, `${path}.id`, "id_target_mismatch", `id must be family:${family}.`);
    }

    if (target && family && target.label !== family) {
      addIssue(issues, `${path}.target.label`, "target_label_mismatch", "target.label must match family.");
    }

    const recognitionHints = validateRecognitionHints(input.recognitionHints, `${path}.recognitionHints`, issues);

    if (id && label && shortLabel && tutorial && target && family && recognitionHints && noErrorsAtOrBelow(issues, path)) {
      return {
        id: id as `family:${GlyphFamily}`,
        kind,
        family,
        label,
        shortLabel,
        target: { kind, label: target.label as GlyphFamily },
        tutorial,
        recognitionHints
      } satisfies MagicFamilyCard;
    }

    return undefined;
  }

  if (kind === "operator") {
    const operator = readOperatorLabel(input.operator, `${path}.operator`, issues);

    if (id && operator && id !== `operator:${operator}`) {
      addIssue(issues, `${path}.id`, "id_target_mismatch", `id must be operator:${operator}.`);
    }

    if (target && operator && target.label !== operator) {
      addIssue(issues, `${path}.target.label`, "target_label_mismatch", "target.label must match operator.");
    }

    const dependencies = validateOperatorLabelsArray(input.dependencies, `${path}.dependencies`, "invalid_dependency", issues);
    const anchorHints = validateAnchorHints(input.anchorHints, `${path}.anchorHints`, issues);

    if (id && label && shortLabel && tutorial && target && operator && dependencies && anchorHints && noErrorsAtOrBelow(issues, path)) {
      return {
        id: id as `operator:${OverlayOperator}`,
        kind,
        operator,
        label,
        shortLabel,
        target: { kind, label: target.label as OverlayOperator },
        tutorial,
        dependencies,
        anchorHints
      } satisfies MagicOperatorCard;
    }

    return undefined;
  }

  return undefined;
}

function validateTutorial(input: unknown, path: string, issues: MagicDatacardValidationIssue[]): MagicTutorialMetadata | undefined {
  if (!isRecord(input)) {
    addIssue(issues, path, "missing_required", "tutorial metadata is required.");
    return undefined;
  }

  const title = requireNonEmptyString(input, "title", `${path}.title`, issues);
  const instruction = requireNonEmptyString(input, "instruction", `${path}.instruction`, issues);
  const summary = requireNonEmptyString(input, "summary", `${path}.summary`, issues);
  const checklist = requireNonEmptyStringArray(input.checklist, `${path}.checklist`, issues);
  const emergentPrompts = requireNonEmptyStringArray(input.emergentPrompts, `${path}.emergentPrompts`, issues);
  const whatIfHints = requireNonEmptyStringArray(input.whatIfHints, `${path}.whatIfHints`, issues);

  if (!title || !instruction || !summary || !checklist || !emergentPrompts || !whatIfHints) {
    return undefined;
  }

  return { title, instruction, summary, checklist, emergentPrompts, whatIfHints };
}

function validateTarget(
  input: unknown,
  parentKind: MagicCardKind | undefined,
  path: string,
  issues: MagicDatacardValidationIssue[]
): { kind: MagicCardKind; label: GlyphFamily | OverlayOperator } | undefined {
  if (!isRecord(input)) {
    addIssue(issues, path, "missing_required", "target is required.");
    return undefined;
  }

  const kind = readKind(input, `${path}.kind`, issues);

  if (parentKind && kind && kind !== parentKind) {
    addIssue(issues, `${path}.kind`, "target_kind_mismatch", "target.kind must match card kind.");
  }

  if (kind === "family") {
    const label = readFamilyLabel(input.label, `${path}.label`, issues);
    return label ? { kind, label } : undefined;
  }

  if (kind === "operator") {
    const label = readOperatorLabel(input.label, `${path}.label`, issues);
    return label ? { kind, label } : undefined;
  }

  return undefined;
}

function validateRecognitionHints(
  input: unknown,
  path: string,
  issues: MagicDatacardValidationIssue[]
): MagicFamilyCard["recognitionHints"] | undefined {
  if (!isRecord(input)) {
    addIssue(issues, path, "missing_required", "recognitionHints are required for family cards.");
    return undefined;
  }

  const strokeCount = input.strokeCount;
  const closed = input.closed;
  const shapeKeywords = requireNonEmptyStringArray(input.shapeKeywords, `${path}.shapeKeywords`, issues);
  const definitionPattern = readOptionalNonEmptyString(input, "definitionPattern", `${path}.definitionPattern`, issues);
  const featureHints = validateRecognitionFeatureHints(input.featureHints, `${path}.featureHints`, issues);

  if (!Array.isArray(strokeCount) || strokeCount.length !== 2 || !strokeCount.every(isNonNegativeInteger)) {
    addIssue(issues, `${path}.strokeCount`, "invalid_type", "strokeCount must be a [min, max] non-negative integer tuple.");
  } else if (strokeCount[0] > strokeCount[1]) {
    addIssue(issues, `${path}.strokeCount`, "invalid_range", "strokeCount minimum must not exceed maximum.");
  }

  if (typeof closed !== "boolean") {
    addIssue(issues, `${path}.closed`, "invalid_type", "closed must be a boolean.");
  }

  if (definitionPattern) {
    try {
      new RegExp(definitionPattern, "iu");
    } catch {
      addIssue(issues, `${path}.definitionPattern`, "invalid_definition_pattern", "definitionPattern must be a valid regular expression.");
    }
  }

  if (!Array.isArray(strokeCount) || strokeCount.length !== 2 || !strokeCount.every(isNonNegativeInteger) || strokeCount[0] > strokeCount[1]) {
    return undefined;
  }

  if (typeof closed !== "boolean" || !shapeKeywords || featureHints === false || !noErrorsAtOrBelow(issues, path)) {
    return undefined;
  }

  return {
    strokeCount: [strokeCount[0], strokeCount[1]],
    closed,
    shapeKeywords,
    ...(definitionPattern ? { definitionPattern } : {}),
    ...(featureHints ? { featureHints } : {})
  };
}

function validateRecognitionFeatureHints(
  input: unknown,
  path: string,
  issues: MagicDatacardValidationIssue[]
): MagicFamilyCard["recognitionHints"]["featureHints"] | false | undefined {
  if (input === undefined) {
    return undefined;
  }

  if (!isRecord(input)) {
    addIssue(issues, path, "invalid_type", "featureHints must be an object.");
    return false;
  }

  const validKeys = ["corners", "endpointClusters", "circularity", "fillRatio", "parallelism"] as const;
  const featureHints: NonNullable<MagicFamilyCard["recognitionHints"]["featureHints"]> = {};

  for (const key of validKeys) {
    const value = input[key];

    if (value === undefined) {
      continue;
    }

    if (!Array.isArray(value) || value.length !== 2 || !value.every(isFiniteNumber)) {
      addIssue(issues, `${path}.${key}`, "invalid_type", `${key} must be a [min, max] number tuple.`);
      return false;
    }

    if (value[0] > value[1]) {
      addIssue(issues, `${path}.${key}`, "invalid_range", `${key} minimum must not exceed maximum.`);
      return false;
    }

    featureHints[key] = [value[0], value[1]];
  }

  return Object.keys(featureHints).length > 0 ? featureHints : undefined;
}

function validateOperatorLabelsArray(
  input: unknown,
  path: string,
  code: string,
  issues: MagicDatacardValidationIssue[]
): readonly OverlayOperator[] | undefined {
  if (!Array.isArray(input)) {
    addIssue(issues, path, "missing_required", "dependencies must be an array.");
    return undefined;
  }

  const labels: OverlayOperator[] = [];

  input.forEach((entry, index) => {
    const label = readOperatorLabel(entry, `${path}[${index}]`, issues, code);
    if (label) {
      labels.push(label);
    }
  });

  return labels.length === input.length ? labels : undefined;
}

function validateAnchorHints(
  input: unknown,
  path: string,
  issues: MagicDatacardValidationIssue[]
): readonly OverlayAnchorZoneId[] | undefined {
  if (!Array.isArray(input)) {
    addIssue(issues, path, "missing_required", "anchorHints must be an array.");
    return undefined;
  }

  const labels: OverlayAnchorZoneId[] = [];

  input.forEach((entry, index) => {
    if (typeof entry !== "string" || !VALID_ANCHOR_ZONE_LABELS.has(entry)) {
      addIssue(issues, `${path}[${index}]`, "invalid_anchor_zone", `Unknown anchor zone: ${String(entry)}.`);
      return;
    }

    labels.push(entry as OverlayAnchorZoneId);
  });

  return labels.length === input.length ? labels : undefined;
}

function validateNoDuplicateRawCardIds(rawCards: readonly unknown[], path: string, issues: MagicDatacardValidationIssue[]): void {
  const seen = new Map<string, number>();

  rawCards.forEach((rawCard, index) => {
    if (!isRecord(rawCard) || typeof rawCard.id !== "string") {
      return;
    }

    const previousIndex = seen.get(rawCard.id);
    if (previousIndex !== undefined) {
      addIssue(issues, `${path}.cards[${index}].id`, "duplicate_id", `Duplicate card id also appears at cards[${previousIndex}].`);
      return;
    }

    seen.set(rawCard.id, index);
  });
}

function validateNoDuplicateRawTargets(rawCards: readonly unknown[], path: string, issues: MagicDatacardValidationIssue[]): void {
  const seen = new Map<string, number>();

  rawCards.forEach((rawCard, index) => {
    if (!isRecord(rawCard) || !isRecord(rawCard.target)) {
      return;
    }

    const targetKind = rawCard.target.kind;
    const targetLabel = rawCard.target.label;

    if (typeof targetKind !== "string" || typeof targetLabel !== "string") {
      return;
    }

    const key = `${targetKind}:${targetLabel}`;
    const previousIndex = seen.get(key);
    if (previousIndex !== undefined) {
      addIssue(
        issues,
        `${path}.cards[${index}].target`,
        "duplicate_target",
        `Duplicate target ${key} also appears at cards[${previousIndex}].`
      );
      return;
    }

    seen.set(key, index);
  });
}

function validateClosedCardSetCoverage(cards: readonly MagicCard[], path: string, issues: MagicDatacardValidationIssue[]): void {
  const familyLabels = new Set(cards.filter((card): card is MagicFamilyCard => card.kind === "family").map((card) => card.family));
  const operatorLabels = new Set(cards.filter((card): card is MagicOperatorCard => card.kind === "operator").map((card) => card.operator));

  BUILT_IN_MAGIC_FAMILY_LABELS.forEach((label) => {
    if (!familyLabels.has(label)) {
      addIssue(issues, `${path}.cards`, "missing_target", `Missing required family target: ${label}.`);
    }
  });

  BUILT_IN_MAGIC_OPERATOR_LABELS.forEach((label) => {
    if (!operatorLabels.has(label)) {
      addIssue(issues, `${path}.cards`, "missing_target", `Missing required operator target: ${label}.`);
    }
  });
}

function readKind(input: Record<string, unknown>, path: string, issues: MagicDatacardValidationIssue[]): MagicCardKind | undefined {
  const value = input.kind;

  if (value !== "family" && value !== "operator") {
    addIssue(issues, path, "invalid_kind", "kind must be family or operator.");
    return undefined;
  }

  return value;
}

function readFamilyLabel(input: unknown, path: string, issues: MagicDatacardValidationIssue[]): GlyphFamily | undefined {
  if (typeof input !== "string" || !VALID_FAMILY_LABELS.has(input)) {
    addIssue(issues, path, "invalid_family_label", `Unknown family label: ${String(input)}.`);
    return undefined;
  }

  return input as GlyphFamily;
}

function readOperatorLabel(
  input: unknown,
  path: string,
  issues: MagicDatacardValidationIssue[],
  code = "invalid_operator_label"
): OverlayOperator | undefined {
  if (typeof input !== "string" || !VALID_OPERATOR_LABELS.has(input)) {
    addIssue(issues, path, code, `Unknown operator label: ${String(input)}.`);
    return undefined;
  }

  return input as OverlayOperator;
}

function requireNonEmptyString(
  input: Record<string, unknown>,
  key: string,
  path: string,
  issues: MagicDatacardValidationIssue[]
): string | undefined {
  return readRequiredNonEmptyString(input[key], path, issues);
}

function readOptionalNonEmptyString(
  input: Record<string, unknown>,
  key: string,
  path: string,
  issues: MagicDatacardValidationIssue[]
): string | undefined {
  const value = input[key];

  if (value === undefined) {
    return undefined;
  }

  return readRequiredNonEmptyString(value, path, issues);
}

function readRequiredNonEmptyString(value: unknown, path: string, issues: MagicDatacardValidationIssue[]): string | undefined {
  if (typeof value !== "string" || value.trim().length === 0) {
    addIssue(issues, path, "missing_required", `${path} must be a non-empty string.`);
    return undefined;
  }

  return value;
}

function requireNonEmptyStringArray(
  input: unknown,
  path: string,
  issues: MagicDatacardValidationIssue[]
): readonly string[] | undefined {
  if (!Array.isArray(input) || input.length === 0) {
    addIssue(issues, path, "missing_required", `${path} must be a non-empty string array.`);
    return undefined;
  }

  const values: string[] = [];

  input.forEach((entry, index) => {
    if (typeof entry !== "string" || entry.trim().length === 0) {
      addIssue(issues, `${path}[${index}]`, "invalid_type", `${path}[${index}] must be a non-empty string.`);
      return;
    }

    values.push(entry);
  });

  return values.length === input.length ? values : undefined;
}

function buildResult<TValue>(value: TValue | undefined, issues: readonly MagicDatacardValidationIssue[]): MagicDatacardValidationResult<TValue> {
  const valid = issues.every((issue) => issue.severity !== "error");
  return value !== undefined && valid ? { valid, issues, value } : { valid: false, issues };
}

function addIssue(
  issues: MagicDatacardValidationIssue[],
  path: string,
  code: string,
  message: string,
  severity: MagicDatacardValidationSeverity = "error"
): void {
  issues.push({ path, code, severity, message });
}

function noErrorsAtOrBelow(issues: readonly MagicDatacardValidationIssue[], path: string): boolean {
  return !issues.some((issue) => issue.severity === "error" && (issue.path === path || issue.path.startsWith(`${path}.`)));
}

function isRecord(input: unknown): input is Record<string, unknown> {
  return typeof input === "object" && input !== null && !Array.isArray(input);
}

function isNonNegativeInteger(input: unknown): input is number {
  return typeof input === "number" && Number.isInteger(input) && input >= 0;
}

function isFiniteNumber(input: unknown): input is number {
  return typeof input === "number" && Number.isFinite(input);
}
