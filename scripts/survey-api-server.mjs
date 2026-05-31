import { createServer } from "node:http";
import { createHash, randomUUID, timingSafeEqual } from "node:crypto";
import { mkdir, appendFile, readFile } from "node:fs/promises";
import { join, resolve } from "node:path";
import { pathToFileURL } from "node:url";

export const SURVEY_SCHEMA_VERSION = "magic-symbol-survey-v8";
export const TINYML_NOISY_EVAL_SCHEMA_VERSION = "tinyml-noisy-eval-v1";
export const TUTORIAL_THRESHOLD_EVAL_SCHEMA_VERSION = "tutorial-threshold-eval-v1";
export const SURVEY_EXPERIMENT_GROUPS = ["shape_only", "scent_effects", "tutorial_quality"];
export const SURVEY_HCI_PROBE_VARIANTS = ["log_discovery", "goal_scaffold"];
export const MAX_BODY_BYTES = 96 * 1024;
export const TINYML_NOISY_EVAL_MAX_BODY_BYTES = 1024 * 1024;
export const TUTORIAL_THRESHOLD_EVAL_MAX_BODY_BYTES = 1024 * 1024;
const SESSION_TTL_MS = 2 * 60 * 60 * 1000;

const SURVEY_PROMPT_WORDS = ["fire", "water", "wind"];
const SURVEY_GUESS_WORDS = ["fire", "water", "wind", "tree", "stone", "lightning"];
const SURVEY_CAPTURE_MODES = ["ideal", "fast", "comfortable"];
const FORBIDDEN_DRAWING_FIELDS = [
  "strokes",
  "strokeLimit",
  "strokeCount",
  "recognizedFamily",
  "recognitionStatus",
  "quality",
  "inputNote"
];
const FORBIDDEN_GUESS_TRIAL_FIELDS = ["trialId", "correct"];
const FORBIDDEN_RESPONSE_FIELDS = ["phone", "email", "raffleContact"];
const TINYML_TOPOLOGIES = ["closed", "open", "mixed"];
const TINYML_NOISE_RECIPE_IDS = [
  "stable",
  "jitter",
  "rotation_drift",
  "scale_offset",
  "open_gap",
  "stroke_merge",
  "point_dropout",
  "fast_compression",
  "personal_residual"
];

const DEFAULT_ALLOWED_ORIGINS = [
  "http://localhost:5173",
  "http://127.0.0.1:5173",
  "http://localhost:4173",
  "http://127.0.0.1:4173"
];

export function assignExperimentGroup(seed) {
  const hash = createHash("sha256").update(String(seed)).digest();
  return SURVEY_EXPERIMENT_GROUPS[hash[0] % SURVEY_EXPERIMENT_GROUPS.length];
}

export function assignBalancedExperimentGroup(groupCounts, seed) {
  const counts = createExperimentGroupCounts(groupCounts);
  let lowestCount = Infinity;
  const candidates = [];

  for (const group of SURVEY_EXPERIMENT_GROUPS) {
    const count = counts.get(group) ?? 0;

    if (count < lowestCount) {
      lowestCount = count;
      candidates.length = 0;
      candidates.push(group);
      continue;
    }

    if (count === lowestCount) {
      candidates.push(group);
    }
  }

  if (candidates.length === 1) {
    return candidates[0];
  }

  const hash = createHash("sha256").update(String(seed)).digest();
  return candidates[hash[0] % candidates.length];
}

export function assignBalancedHciProbeVariant(variantCounts, seed) {
  const counts = createHciProbeVariantCounts(variantCounts);
  let lowestCount = Infinity;
  const candidates = [];

  for (const variant of SURVEY_HCI_PROBE_VARIANTS) {
    const count = counts.get(variant) ?? 0;

    if (count < lowestCount) {
      lowestCount = count;
      candidates.length = 0;
      candidates.push(variant);
      continue;
    }

    if (count === lowestCount) {
      candidates.push(variant);
    }
  }

  if (candidates.length === 1) {
    return candidates[0];
  }

  const hash = createHash("sha256").update(String(seed)).digest();
  return candidates[hash[1] % candidates.length];
}

export function countExperimentGroupsFromResponseLog(text) {
  const counts = createExperimentGroupCounts();

  for (const line of String(text).split(/\r?\n/)) {
    if (!line.trim()) {
      continue;
    }

    try {
      const record = JSON.parse(line);
      const group = record?.payload?.experimentGroup;

      if (SURVEY_EXPERIMENT_GROUPS.includes(group)) {
        incrementExperimentGroupCount(counts, group);
      }
    } catch {
      // Ignore a malformed or partially written trailing line rather than blocking new sessions.
    }
  }

  return Object.fromEntries(counts);
}

export function countExperimentCellsFromResponseLog(text) {
  const counts = createExperimentCellCounts();

  for (const line of String(text).split(/\r?\n/)) {
    if (!line.trim()) {
      continue;
    }

    try {
      const record = JSON.parse(line);
      const group = record?.payload?.experimentGroup;
      const variant = record?.payload?.hciProbeVariant;

      if (SURVEY_EXPERIMENT_GROUPS.includes(group) && SURVEY_HCI_PROBE_VARIANTS.includes(variant)) {
        incrementExperimentCellCount(counts, group, variant);
      }
    } catch {
      // Ignore a malformed or partially written trailing line rather than blocking new sessions.
    }
  }

  return experimentCellCountsToObject(counts);
}

export function validateSurveyResponsePayload(payload) {
  const errors = [];

  if (!isRecord(payload)) {
    return ["payload must be an object"];
  }

  for (const field of FORBIDDEN_RESPONSE_FIELDS) {
    if (field in payload) {
      errors.push(`${field} must not be submitted with survey response`);
    }
  }

  requireString(payload, "schemaVersion", errors);
  requireString(payload, "submissionId", errors);
  requireString(payload, "sessionId", errors);

  if (payload.schemaVersion !== SURVEY_SCHEMA_VERSION) {
    errors.push(`schemaVersion must be ${SURVEY_SCHEMA_VERSION}`);
  }

  if (!SURVEY_EXPERIMENT_GROUPS.includes(payload.experimentGroup)) {
    errors.push("experimentGroup is invalid");
  }

  if (!SURVEY_HCI_PROBE_VARIANTS.includes(payload.hciProbeVariant)) {
    errors.push("hciProbeVariant is invalid");
  }

  if (payload.consentAccepted !== true) {
    errors.push("consentAccepted must be true");
  }

  validateCompactId(payload.submissionId, "submissionId", errors);
  validateStringLength(payload.sessionId, "sessionId", 16, 128, errors);
  validateDirectDrawings(payload.directDrawings, errors);
  validateGuessTrials(payload.wordGuessTrials, errors);
  validateTutorialCaptures(payload.tutorialCaptures, errors);
  validateEngineComparison(payload.engineComparison, errors);
  validateSelfReport(payload.selfReport, errors);
  validateInteractionMetrics(payload.interactionMetrics, errors);

  return errors;
}

export function validateSurveyRaffleContactPayload(payload) {
  const errors = [];

  if (!isRecord(payload)) {
    return ["raffleContact must be an object"];
  }

  requireString(payload, "schemaVersion", errors);
  requireString(payload, "submissionId", errors);
  requireString(payload, "sessionId", errors);

  if (payload.schemaVersion !== SURVEY_SCHEMA_VERSION) {
    errors.push(`schemaVersion must be ${SURVEY_SCHEMA_VERSION}`);
  }

  validateCompactId(payload.submissionId, "submissionId", errors);
  validateStringLength(payload.sessionId, "sessionId", 16, 128, errors);

  const phone = optionalTrimmedString(payload.phone);
  const email = optionalTrimmedString(payload.email);

  if (!phone && !email) {
    errors.push("phone or email is required for raffle contact");
  }

  if (phone && !/^[0-9+\-()\s]{8,30}$/.test(phone)) {
    errors.push("phone must contain 8-30 phone characters");
  }

  if (email && (email.length > 254 || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email))) {
    errors.push("email must be a valid email address");
  }

  return errors;
}

export function validateTinyMlNoisyEvalPayload(payload) {
  const errors = [];

  if (!isRecord(payload)) {
    return ["payload must be an object"];
  }

  requireString(payload, "schemaVersion", errors);
  requireString(payload, "submissionId", errors);
  requireString(payload, "sessionId", errors);

  if (payload.schemaVersion !== TINYML_NOISY_EVAL_SCHEMA_VERSION) {
    errors.push(`schemaVersion must be ${TINYML_NOISY_EVAL_SCHEMA_VERSION}`);
  }

  validateCompactId(payload.submissionId, "submissionId", errors);
  validateStringLength(payload.sessionId, "sessionId", 16, 128, errors);

  if (payload.participantId !== undefined) {
    validateStringLength(payload.participantId, "participantId", 0, 64, errors);
  }

  if (payload.consentAccepted !== true) {
    errors.push("consentAccepted must be true");
  }

  validateStringLength(payload.startedAtIso, "startedAtIso", 10, 40, errors);
  validateStringLength(payload.completedAtIso, "completedAtIso", 10, 40, errors);
  validateTinyMlCanvas(payload.canvas, "canvas", errors);
  validateTinyMlTrialPlan(payload.trialPlan, errors);
  validateTinyMlTrials(payload.trials, errors);
  validateTinyMlAggregate(payload.aggregate, errors);

  return errors;
}

export function validateTutorialThresholdEvalPayload(payload) {
  const errors = [];

  if (!isRecord(payload)) {
    return ["payload must be an object"];
  }

  requireString(payload, "schemaVersion", errors);
  requireString(payload, "submissionId", errors);
  requireString(payload, "sessionId", errors);

  if (payload.schemaVersion !== TUTORIAL_THRESHOLD_EVAL_SCHEMA_VERSION) {
    errors.push(`schemaVersion must be ${TUTORIAL_THRESHOLD_EVAL_SCHEMA_VERSION}`);
  }

  validateCompactId(payload.submissionId, "submissionId", errors);
  validateStringLength(payload.sessionId, "sessionId", 16, 128, errors);

  if (payload.participantId !== undefined) {
    validateStringLength(payload.participantId, "participantId", 0, 64, errors);
  }

  if (payload.notes !== undefined) {
    validateStringLength(payload.notes, "notes", 0, 1000, errors);
  }

  if (payload.consentAccepted !== true) {
    errors.push("consentAccepted must be true");
  }

  validateStringLength(payload.startedAtIso, "startedAtIso", 10, 40, errors);
  validateStringLength(payload.completedAtIso, "completedAtIso", 10, 40, errors);
  validateTinyMlCanvas(payload.canvas, "canvas", errors);
  validateTutorialThresholdEvalState(payload.thresholdState, "thresholdState", errors);
  validateTutorialTinyMlSessionState(payload.tinyMlTwoTrackState, "tinyMlTwoTrackState", errors);
  validateTutorialThresholdEvalCaptures(payload.captures, errors);
  validateTutorialThresholdEvalTrials(payload.evals, errors);
  validateTutorialThresholdEvalAggregate(payload.aggregate, errors);

  const captureCount = Array.isArray(payload.captures) ? payload.captures.length : 0;
  const evalCount = Array.isArray(payload.evals) ? payload.evals.length : 0;
  if (captureCount + evalCount < 1) {
    errors.push("captures or evals must contain at least 1 item");
  }

  return errors;
}

export function createSurveyApiServer(options = {}) {
  const sessions = new Map();
  const rateBuckets = new Map();
  const dataDir = resolve(options.dataDir ?? join(process.cwd(), "data"));
  const responsePath = resolve(options.responsePath ?? join(dataDir, "survey-responses.ndjson"));
  const raffleContactPath = resolve(options.raffleContactPath ?? join(dataDir, "survey-raffle-contacts.ndjson"));
  const tinyMlNoisyEvalResponsePath = resolve(
    options.tinyMlNoisyEvalResponsePath ?? join(dataDir, "tinyml-noisy-eval-responses.ndjson")
  );
  const tutorialThresholdEvalResponsePath = resolve(
    options.tutorialThresholdEvalResponsePath ?? join(dataDir, "tutorial-threshold-eval-responses.ndjson")
  );
  const allowedOrigins = new Set(options.allowedOrigins ?? readAllowedOrigins());
  const now = options.now ?? (() => Date.now());
  const completedGroupCounts = createExperimentGroupCounts(options.initialExperimentGroupCounts);
  const activeGroupCounts = createExperimentGroupCounts();
  const completedCellCounts = createExperimentCellCounts(options.initialExperimentCellCounts);
  const activeCellCounts = createExperimentCellCounts();
  let responseLogCountsLoaded = Boolean(options.initialExperimentGroupCounts && options.initialExperimentCellCounts);
  let responseLogCountsPromise = null;

  const server = createServer(async (request, response) => {
    try {
      const origin = request.headers.origin;
      const timestamp = now();

      if (!applyCors(request, response, allowedOrigins)) {
        sendJson(response, 403, { error: "origin_not_allowed" });
        return;
      }

      if (request.method === "OPTIONS") {
        response.writeHead(204);
        response.end();
        return;
      }

      if (!checkRateLimit(rateBuckets, clientKey(request), timestamp)) {
        sendJson(response, 429, { error: "rate_limited" });
        return;
      }

      const url = new URL(request.url ?? "/", "http://localhost");
      cleanupExpiredSessions(sessions, activeGroupCounts, activeCellCounts, timestamp);

      if (request.method === "GET" && url.pathname === "/api/survey-session") {
        await ensureResponseLogCountsLoaded();
        const sessionId = randomUUID();
        const csrfToken = randomUUID();
        const experimentGroup = assignBalancedExperimentGroup(
          mergeExperimentGroupCounts(completedGroupCounts, activeGroupCounts),
          sessionId
        );
        const hciProbeVariant = assignBalancedHciProbeVariant(
          mergeExperimentCellCountsForGroup(experimentGroup, completedCellCounts, activeCellCounts),
          sessionId
        );
        incrementExperimentGroupCount(activeGroupCounts, experimentGroup);
        incrementExperimentCellCount(activeCellCounts, experimentGroup, hciProbeVariant);

        sessions.set(sessionId, {
          csrfToken,
          experimentGroup,
          hciProbeVariant,
          createdAt: timestamp,
          assignmentCounted: true,
          completed: false,
          submissionIds: new Set(),
          raffleContactSubmissionIds: new Set(),
          tinyMlNoisyEvalSubmissionIds: new Set(),
          tutorialThresholdEvalSubmissionIds: new Set()
        });

        response.setHeader("Set-Cookie", [
          `survey_session=${sessionId}; HttpOnly; SameSite=Lax; Path=/; Max-Age=7200`
        ]);
        sendJson(response, 200, { sessionId, csrfToken, experimentGroup, hciProbeVariant });
        return;
      }

      if (request.method === "POST" && url.pathname === "/api/survey-responses") {
        const sessionId = readCookie(request.headers.cookie ?? "", "survey_session");
        const session = sessionId ? sessions.get(sessionId) : undefined;

        if (!session || timestamp - session.createdAt > SESSION_TTL_MS) {
          sendJson(response, 401, { error: "session_expired" });
          return;
        }

        const csrfToken = request.headers["x-csrf-token"];

        if (typeof csrfToken !== "string" || !safeEqual(csrfToken, session.csrfToken)) {
          sendJson(response, 403, { error: "csrf_failed" });
          return;
        }

        const bodyText = await readRequestBody(request, MAX_BODY_BYTES);
        const payload = JSON.parse(bodyText);
        const errors = validateSurveyResponsePayload(payload);

        if (payload.sessionId !== sessionId) {
          errors.push("sessionId does not match cookie");
        }

        if (payload.experimentGroup !== session.experimentGroup) {
          errors.push("experimentGroup does not match session");
        }

        if (payload.hciProbeVariant !== session.hciProbeVariant) {
          errors.push("hciProbeVariant does not match session");
        }

        if (errors.length > 0) {
          sendJson(response, 400, { error: "validation_failed", details: errors });
          return;
        }

        if (session.submissionIds.has(payload.submissionId)) {
          sendJson(response, 409, { error: "duplicate_submission" });
          return;
        }

        session.submissionIds.add(payload.submissionId);
        await mkdir(dataDir, { recursive: true });
        await appendFile(
          responsePath,
          `${JSON.stringify({ receivedAt: new Date(timestamp).toISOString(), payload })}\n`,
          "utf8"
        );
        markSessionCompleted(session, completedGroupCounts, activeGroupCounts, completedCellCounts, activeCellCounts);

        sendJson(response, 201, { ok: true });
        return;
      }

      if (request.method === "POST" && url.pathname === "/api/tinyml-noisy-eval-responses") {
        const sessionId = readCookie(request.headers.cookie ?? "", "survey_session");
        const session = sessionId ? sessions.get(sessionId) : undefined;

        if (!session || timestamp - session.createdAt > SESSION_TTL_MS) {
          sendJson(response, 401, { error: "session_expired" });
          return;
        }

        const csrfToken = request.headers["x-csrf-token"];

        if (typeof csrfToken !== "string" || !safeEqual(csrfToken, session.csrfToken)) {
          sendJson(response, 403, { error: "csrf_failed" });
          return;
        }

        const bodyText = await readRequestBody(request, TINYML_NOISY_EVAL_MAX_BODY_BYTES);
        const payload = JSON.parse(bodyText);
        const errors = validateTinyMlNoisyEvalPayload(payload);

        if (payload.sessionId !== sessionId) {
          errors.push("sessionId does not match cookie");
        }

        if (errors.length > 0) {
          sendJson(response, 400, { error: "validation_failed", details: errors });
          return;
        }

        if (session.tinyMlNoisyEvalSubmissionIds.has(payload.submissionId)) {
          sendJson(response, 409, { error: "duplicate_tinyml_noisy_eval_submission" });
          return;
        }

        session.tinyMlNoisyEvalSubmissionIds.add(payload.submissionId);
        await mkdir(dataDir, { recursive: true });
        await appendFile(
          tinyMlNoisyEvalResponsePath,
          `${JSON.stringify({ receivedAt: new Date(timestamp).toISOString(), payload })}\n`,
          "utf8"
        );
        markSessionCompleted(session, completedGroupCounts, activeGroupCounts, completedCellCounts, activeCellCounts);

        sendJson(response, 201, { ok: true });
        return;
      }

      if (request.method === "POST" && url.pathname === "/api/tutorial-threshold-eval-responses") {
        const sessionId = readCookie(request.headers.cookie ?? "", "survey_session");
        const session = sessionId ? sessions.get(sessionId) : undefined;

        if (!session || timestamp - session.createdAt > SESSION_TTL_MS) {
          sendJson(response, 401, { error: "session_expired" });
          return;
        }

        const csrfToken = request.headers["x-csrf-token"];

        if (typeof csrfToken !== "string" || !safeEqual(csrfToken, session.csrfToken)) {
          sendJson(response, 403, { error: "csrf_failed" });
          return;
        }

        const bodyText = await readRequestBody(request, TUTORIAL_THRESHOLD_EVAL_MAX_BODY_BYTES);
        const payload = JSON.parse(bodyText);
        const errors = validateTutorialThresholdEvalPayload(payload);

        if (payload.sessionId !== sessionId) {
          errors.push("sessionId does not match cookie");
        }

        if (errors.length > 0) {
          sendJson(response, 400, { error: "validation_failed", details: errors });
          return;
        }

        if (session.tutorialThresholdEvalSubmissionIds.has(payload.submissionId)) {
          sendJson(response, 409, { error: "duplicate_tutorial_threshold_eval_submission" });
          return;
        }

        session.tutorialThresholdEvalSubmissionIds.add(payload.submissionId);
        await mkdir(dataDir, { recursive: true });
        await appendFile(
          tutorialThresholdEvalResponsePath,
          `${JSON.stringify({ receivedAt: new Date(timestamp).toISOString(), payload })}\n`,
          "utf8"
        );
        markSessionCompleted(session, completedGroupCounts, activeGroupCounts, completedCellCounts, activeCellCounts);

        sendJson(response, 201, { ok: true });
        return;
      }

      if (request.method === "POST" && url.pathname === "/api/survey-raffle-contact") {
        const sessionId = readCookie(request.headers.cookie ?? "", "survey_session");
        const session = sessionId ? sessions.get(sessionId) : undefined;

        if (!session || timestamp - session.createdAt > SESSION_TTL_MS) {
          sendJson(response, 401, { error: "session_expired" });
          return;
        }

        const csrfToken = request.headers["x-csrf-token"];

        if (typeof csrfToken !== "string" || !safeEqual(csrfToken, session.csrfToken)) {
          sendJson(response, 403, { error: "csrf_failed" });
          return;
        }

        const bodyText = await readRequestBody(request, MAX_BODY_BYTES);
        const payload = JSON.parse(bodyText);
        const errors = validateSurveyRaffleContactPayload(payload);

        if (payload.sessionId !== sessionId) {
          errors.push("sessionId does not match cookie");
        }

        if (errors.length > 0) {
          sendJson(response, 400, { error: "validation_failed", details: errors });
          return;
        }

        if (session.raffleContactSubmissionIds.has(payload.submissionId)) {
          sendJson(response, 409, { error: "duplicate_raffle_contact" });
          return;
        }

        session.raffleContactSubmissionIds.add(payload.submissionId);
        await mkdir(dataDir, { recursive: true });
        await appendFile(
          raffleContactPath,
          `${JSON.stringify({
            receivedAt: new Date(timestamp).toISOString(),
            sessionId: payload.sessionId,
            submissionId: payload.submissionId,
            phone: optionalTrimmedString(payload.phone) || undefined,
            email: optionalTrimmedString(payload.email) || undefined
          })}\n`,
          "utf8"
        );

        sendJson(response, 201, { ok: true });
        return;
      }

      sendJson(response, 404, { error: "not_found" });
    } catch (error) {
      if (error instanceof PayloadTooLargeError) {
        sendJson(response, 413, { error: "payload_too_large" });
        return;
      }

      if (error instanceof SyntaxError) {
        sendJson(response, 400, { error: "invalid_json" });
        return;
      }

      sendJson(response, 500, { error: "internal_error" });
    }
  });

  return {
    server,
    sessions,
    responsePath,
    raffleContactPath,
    tinyMlNoisyEvalResponsePath,
    tutorialThresholdEvalResponsePath,
    experimentGroupCounts: {
      completed: completedGroupCounts,
      active: activeGroupCounts
    },
    experimentCellCounts: {
      completed: completedCellCounts,
      active: activeCellCounts
    }
  };

  async function ensureResponseLogCountsLoaded() {
    if (responseLogCountsLoaded) {
      return;
    }

    responseLogCountsPromise ??= readFile(responsePath, "utf8")
      .then((text) => {
        const parsedCounts = countExperimentGroupsFromResponseLog(text);
        const parsedCellCounts = countExperimentCellsFromResponseLog(text);

        for (const group of SURVEY_EXPERIMENT_GROUPS) {
          completedGroupCounts.set(group, parsedCounts[group] ?? 0);

          for (const variant of SURVEY_HCI_PROBE_VARIANTS) {
            completedCellCounts.get(group)?.set(variant, parsedCellCounts[group]?.[variant] ?? 0);
          }
        }
      })
      .catch((error) => {
        if (error?.code !== "ENOENT") {
          throw error;
        }
      })
      .finally(() => {
        responseLogCountsLoaded = true;
      });

    await responseLogCountsPromise;
  }
}

function createExperimentGroupCounts(source = {}) {
  const counts = new Map();

  for (const group of SURVEY_EXPERIMENT_GROUPS) {
    const value = source instanceof Map ? source.get(group) : source[group];
    counts.set(group, sanitizeCount(value));
  }

  return counts;
}

function createHciProbeVariantCounts(source = {}) {
  const counts = new Map();

  for (const variant of SURVEY_HCI_PROBE_VARIANTS) {
    const value = source instanceof Map ? source.get(variant) : source[variant];
    counts.set(variant, sanitizeCount(value));
  }

  return counts;
}

function createExperimentCellCounts(source = {}) {
  const counts = new Map();

  for (const group of SURVEY_EXPERIMENT_GROUPS) {
    const groupSource = source instanceof Map ? source.get(group) : source[group];
    counts.set(group, createHciProbeVariantCounts(groupSource));
  }

  return counts;
}

function experimentCellCountsToObject(counts) {
  const result = {};

  for (const group of SURVEY_EXPERIMENT_GROUPS) {
    result[group] = Object.fromEntries(counts.get(group) ?? createHciProbeVariantCounts());
  }

  return result;
}

function sanitizeCount(value) {
  const numberValue = Number(value);
  return Number.isFinite(numberValue) && numberValue > 0 ? Math.floor(numberValue) : 0;
}

function incrementExperimentGroupCount(counts, group) {
  counts.set(group, (counts.get(group) ?? 0) + 1);
}

function incrementExperimentCellCount(counts, group, variant) {
  const variantCounts = counts.get(group);
  variantCounts?.set(variant, (variantCounts.get(variant) ?? 0) + 1);
}

function decrementExperimentGroupCount(counts, group) {
  counts.set(group, Math.max(0, (counts.get(group) ?? 0) - 1));
}

function decrementExperimentCellCount(counts, group, variant) {
  const variantCounts = counts.get(group);
  variantCounts?.set(variant, Math.max(0, (variantCounts.get(variant) ?? 0) - 1));
}

function mergeExperimentGroupCounts(...sources) {
  const merged = createExperimentGroupCounts();

  for (const source of sources) {
    for (const group of SURVEY_EXPERIMENT_GROUPS) {
      merged.set(group, (merged.get(group) ?? 0) + (source.get(group) ?? 0));
    }
  }

  return merged;
}

function mergeExperimentCellCountsForGroup(group, ...sources) {
  const merged = createHciProbeVariantCounts();

  for (const source of sources) {
    const variantCounts = source.get(group);

    for (const variant of SURVEY_HCI_PROBE_VARIANTS) {
      merged.set(variant, (merged.get(variant) ?? 0) + (variantCounts?.get(variant) ?? 0));
    }
  }

  return merged;
}

function cleanupExpiredSessions(sessions, activeGroupCounts, activeCellCounts, timestamp) {
  for (const [sessionId, session] of sessions) {
    if (timestamp - session.createdAt <= SESSION_TTL_MS) {
      continue;
    }

    if (session.assignmentCounted) {
      decrementExperimentGroupCount(activeGroupCounts, session.experimentGroup);
      decrementExperimentCellCount(activeCellCounts, session.experimentGroup, session.hciProbeVariant);
      session.assignmentCounted = false;
    }

    sessions.delete(sessionId);
  }
}

function markSessionCompleted(session, completedGroupCounts, activeGroupCounts, completedCellCounts, activeCellCounts) {
  if (session.completed) {
    return;
  }

  if (session.assignmentCounted) {
    decrementExperimentGroupCount(activeGroupCounts, session.experimentGroup);
    decrementExperimentCellCount(activeCellCounts, session.experimentGroup, session.hciProbeVariant);
    session.assignmentCounted = false;
  }

  incrementExperimentGroupCount(completedGroupCounts, session.experimentGroup);
  incrementExperimentCellCount(completedCellCounts, session.experimentGroup, session.hciProbeVariant);
  session.completed = true;
}

function readAllowedOrigins() {
  const fromEnv = process.env.SURVEY_ALLOWED_ORIGINS?.split(",").map((origin) => origin.trim()).filter(Boolean);
  return fromEnv && fromEnv.length > 0 ? fromEnv : DEFAULT_ALLOWED_ORIGINS;
}

function applyCors(request, response, allowedOrigins) {
  const origin = request.headers.origin;

  if (!origin) {
    return true;
  }

  if (!allowedOrigins.has(origin)) {
    return false;
  }

  response.setHeader("Access-Control-Allow-Origin", origin);
  response.setHeader("Access-Control-Allow-Credentials", "true");
  response.setHeader("Access-Control-Allow-Headers", "Content-Type, X-CSRF-Token");
  response.setHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
  response.setHeader("Vary", "Origin");
  return true;
}

function checkRateLimit(rateBuckets, key, timestamp) {
  const windowMs = 60_000;
  const maxRequests = 90;
  const bucket = (rateBuckets.get(key) ?? []).filter((item) => timestamp - item < windowMs);

  if (bucket.length >= maxRequests) {
    rateBuckets.set(key, bucket);
    return false;
  }

  bucket.push(timestamp);
  rateBuckets.set(key, bucket);
  return true;
}

function clientKey(request) {
  return String(request.headers["x-forwarded-for"] ?? request.socket.remoteAddress ?? "local").split(",")[0].trim();
}

function readCookie(header, name) {
  return header
    .split(";")
    .map((part) => part.trim())
    .find((part) => part.startsWith(`${name}=`))
    ?.slice(name.length + 1);
}

function safeEqual(left, right) {
  const leftBuffer = Buffer.from(String(left));
  const rightBuffer = Buffer.from(String(right));

  return leftBuffer.length === rightBuffer.length && timingSafeEqual(leftBuffer, rightBuffer);
}

function readRequestBody(request, limit) {
  return new Promise((resolveBody, rejectBody) => {
    let total = 0;
    const chunks = [];

    request.on("data", (chunk) => {
      total += chunk.length;

      if (total > limit) {
        rejectBody(new PayloadTooLargeError());
        request.destroy();
        return;
      }

      chunks.push(chunk);
    });

    request.on("end", () => {
      resolveBody(Buffer.concat(chunks).toString("utf8"));
    });
    request.on("error", rejectBody);
  });
}

function sendJson(response, status, payload) {
  const body = JSON.stringify(payload);
  response.writeHead(status, {
    "Content-Type": "application/json; charset=utf-8",
    "Cache-Control": "no-store",
    "X-Content-Type-Options": "nosniff",
    "Content-Length": Buffer.byteLength(body)
  });
  response.end(body);
}

function validateEngineComparison(value, errors) {
  if (!isRecord(value)) {
    errors.push("engineComparison must be an object");
    return;
  }

  validateLikert(value.understandingBefore, "engineComparison.understandingBefore", errors);
  validateLikert(value.understandingAfter, "engineComparison.understandingAfter", errors);
  validateLikert(value.taskDifficulty, "engineComparison.taskDifficulty", errors);
  validateLikert(value.turnTutorialRating, "engineComparison.turnTutorialRating", errors);
  validateLikert(value.contractClarityRating, "engineComparison.contractClarityRating", errors);

  if (!["turn_tutorial", "contract_notes", "same"].includes(String(value.preferredMode))) {
    errors.push("engineComparison.preferredMode is invalid");
  }

  validateStringLength(value.interactionSummary, "engineComparison.interactionSummary", 1, 300, errors);
  validateAsciiRows(value.asciiBefore, "engineComparison.asciiBefore", errors);
  validateAsciiRows(value.asciiAfter, "engineComparison.asciiAfter", errors);
  validateAsciiActionLog(value.actionLog, errors);
  validateAsciiGoals(value.goals, errors);
  validateNumber(value.actionCount, "engineComparison.actionCount", 0, 200, errors);
}

function validateDirectDrawings(value, errors) {
  validateArray(value, "directDrawings", 3, 12, errors);

  if (!Array.isArray(value)) {
    return;
  }

  for (const word of SURVEY_PROMPT_WORDS) {
    if (!value.some((item) => isRecord(item) && item.targetWord === word)) {
      errors.push(`directDrawings missing ${word}`);
    }
  }

  value.forEach((item, index) => {
    if (!isRecord(item)) {
      errors.push(`directDrawings[${index}] must be an object`);
      return;
    }

    rejectForbiddenDrawingFields(item, `directDrawings[${index}]`, errors);
    validatePromptWord(item.targetWord, `directDrawings[${index}].targetWord`, errors);
    validateShapeTrace(item.shapeTrace, `directDrawings[${index}].shapeTrace`, errors);
    validateNumber(item.elapsedMs, `directDrawings[${index}].elapsedMs`, 0, 600000, errors);
    validateLikert(item.expressionDifficulty, `directDrawings[${index}].expressionDifficulty`, errors);
    validateStringLength(item.expressionReason, `directDrawings[${index}].expressionReason`, 0, 240, errors);
  });
}

function validateTutorialCaptures(value, errors) {
  validateArray(value, "tutorialCaptures", 3, 12, errors);

  if (!Array.isArray(value)) {
    return;
  }

  for (const mode of SURVEY_CAPTURE_MODES) {
    if (!value.some((item) => isRecord(item) && item.mode === mode)) {
      errors.push(`tutorialCaptures missing ${mode}`);
    }
  }

  value.forEach((item, index) => {
    if (!isRecord(item)) {
      errors.push(`tutorialCaptures[${index}] must be an object`);
      return;
    }

    rejectForbiddenDrawingFields(item, `tutorialCaptures[${index}]`, errors);
    validatePromptWord(item.targetWord, `tutorialCaptures[${index}].targetWord`, errors);

    if (!SURVEY_CAPTURE_MODES.includes(item.mode)) {
      errors.push(`tutorialCaptures[${index}].mode is invalid`);
    }

    validateShapeTrace(item.shapeTrace, `tutorialCaptures[${index}].shapeTrace`, errors);
    validateNumber(item.elapsedMs, `tutorialCaptures[${index}].elapsedMs`, 0, 600000, errors);
  });
}

function validateShapeTrace(value, path, errors) {
  if (!Array.isArray(value) || value.length === 0 || value.length > 8) {
    errors.push(`${path} must contain 1-8 simplified strokes`);
    return;
  }

  value.forEach((stroke, strokeIndex) => {
    if (!Array.isArray(stroke) || stroke.length < 1 || stroke.length > 64) {
      errors.push(`${path}[${strokeIndex}] must contain 1-64 points`);
      return;
    }

    stroke.forEach((point, pointIndex) => {
      if (
        !Array.isArray(point) ||
        point.length !== 3 ||
        !isCoordinate(point[0]) ||
        !isCoordinate(point[1]) ||
        !isRelativeTimestamp(point[2])
      ) {
        errors.push(
          `${path}[${strokeIndex}][${pointIndex}] must be [x,y,tMs] integers with x/y 0-1000 and tMs 0-600000`
        );
      }
    });
  });
}

function isCoordinate(value) {
  return Number.isInteger(value) && Number(value) >= 0 && Number(value) <= 1000;
}

function isRelativeTimestamp(value) {
  return Number.isInteger(value) && Number(value) >= 0 && Number(value) <= 600000;
}

function validateGuessTrials(value, errors) {
  validateArray(value, "wordGuessTrials", 3, 12, errors);

  if (!Array.isArray(value)) {
    return;
  }

  value.forEach((item, index) => {
    if (!isRecord(item)) {
      errors.push(`wordGuessTrials[${index}] must be an object`);
      return;
    }

    rejectForbiddenGuessTrialFields(item, `wordGuessTrials[${index}]`, errors);
    validatePromptWord(item.targetWord, `wordGuessTrials[${index}].targetWord`, errors);
    validateGuessWord(item.answer, `wordGuessTrials[${index}].answer`, errors);
    validateLikert(item.confidence, `wordGuessTrials[${index}].confidence`, errors);
    validateNumber(item.reactionMs, `wordGuessTrials[${index}].reactionMs`, 0, 600000, errors);

    if (typeof item.hintsEnabled !== "boolean") {
      errors.push(`wordGuessTrials[${index}].hintsEnabled must be boolean`);
    }

    if (typeof item.effectPlayed !== "boolean") {
      errors.push(`wordGuessTrials[${index}].effectPlayed must be boolean`);
    }

    validateNumber(item.effectPlayCount, `wordGuessTrials[${index}].effectPlayCount`, 0, 20, errors);
    validateEffectHeard(item.effectHeard, `wordGuessTrials[${index}].effectHeard`, errors);
  });
}

function validateSelfReport(value, errors) {
  if (!isRecord(value)) {
    errors.push("selfReport must be an object");
    return;
  }

  for (const key of [
    "tutorialInstructionClarity",
    "tutorialLearningEfficiency",
    "overallClarity",
    "workloadRating"
  ]) {
    validateLikert(value[key], `selfReport.${key}`, errors);
  }

  validateLikertOrNotApplicable(value.scentHelpfulness, "selfReport.scentHelpfulness", errors);
  validateStringLength(value.strengths, "selfReport.strengths", 0, 1000, errors);
  validateStringLength(value.weaknesses, "selfReport.weaknesses", 0, 1000, errors);
}

function validateAsciiActionLog(value, errors) {
  if (!Array.isArray(value) || value.length > 120) {
    errors.push("engineComparison.actionLog must contain 0-120 items");
    return;
  }

  value.forEach((item, index) => {
    if (!isRecord(item)) {
      errors.push(`engineComparison.actionLog[${index}] must be an object`);
      return;
    }

    validateNumber(item.turn, `engineComparison.actionLog[${index}].turn`, 0, 200, errors);
    validateStringLength(item.action, `engineComparison.actionLog[${index}].action`, 1, 40, errors);
    validateStringLength(item.result, `engineComparison.actionLog[${index}].result`, 0, 300, errors);

    if (!isRecord(item.player)) {
      errors.push(`engineComparison.actionLog[${index}].player must be an object`);
      return;
    }

    validateNumber(item.player.row, `engineComparison.actionLog[${index}].player.row`, 0, 49, errors);
    validateNumber(item.player.column, `engineComparison.actionLog[${index}].player.column`, 0, 49, errors);
    validateStringLength(item.player.facing, `engineComparison.actionLog[${index}].player.facing`, 2, 12, errors);
  });
}

function validateAsciiGoals(value, errors) {
  if (!Array.isArray(value) || value.length < 1 || value.length > 6) {
    errors.push("engineComparison.goals must contain 1-6 items");
    return;
  }

  value.forEach((item, index) => {
    if (!isRecord(item)) {
      errors.push(`engineComparison.goals[${index}] must be an object`);
      return;
    }

    if (!["move", "ignite", "observe"].includes(String(item.id))) {
      errors.push(`engineComparison.goals[${index}].id is invalid`);
    }
    validateStringLength(item.label, `engineComparison.goals[${index}].label`, 1, 80, errors);
    if (typeof item.completed !== "boolean") {
      errors.push(`engineComparison.goals[${index}].completed must be boolean`);
    }
    if (item.completedTurn !== undefined) {
      validateNumber(item.completedTurn, `engineComparison.goals[${index}].completedTurn`, 0, 200, errors);
    }
  });
}

function validateInteractionMetrics(value, errors) {
  if (!isRecord(value)) {
    errors.push("interactionMetrics must be an object");
    return;
  }

  if (!Array.isArray(value.promptOrder) || value.promptOrder.length !== SURVEY_PROMPT_WORDS.length) {
    errors.push("interactionMetrics.promptOrder must include prompt words");
  } else {
    for (const word of SURVEY_PROMPT_WORDS) {
      if (!value.promptOrder.includes(word)) {
        errors.push(`interactionMetrics.promptOrder missing ${word}`);
      }
    }
  }

  if (!isRecord(value.stageDurationsMs)) {
    errors.push("interactionMetrics.stageDurationsMs must be an object");
  } else {
    for (const [stage, duration] of Object.entries(value.stageDurationsMs)) {
      if (!["consent", "draw", "guess", "tutorial", "engine", "self-report"].includes(stage)) {
        errors.push(`interactionMetrics.stageDurationsMs.${stage} is invalid`);
        continue;
      }
      validateNumber(duration, `interactionMetrics.stageDurationsMs.${stage}`, 0, 600000, errors);
    }
  }

  validateNumber(value.previousClicks, "interactionMetrics.previousClicks", 0, 200, errors);

  if (!isRecord(value.resetCounts)) {
    errors.push("interactionMetrics.resetCounts must be an object");
    return;
  }

  validateNumber(value.resetCounts.directDrawing, "interactionMetrics.resetCounts.directDrawing", 0, 200, errors);
  validateNumber(value.resetCounts.tutorialCapture, "interactionMetrics.resetCounts.tutorialCapture", 0, 200, errors);
  validateNumber(value.resetCounts.asciiTutorial, "interactionMetrics.resetCounts.asciiTutorial", 0, 200, errors);
}

function validateTinyMlTrialPlan(value, errors) {
  validateArray(value, "trialPlan", 1, 80, errors);

  if (!Array.isArray(value)) {
    return;
  }

  value.forEach((item, index) => {
    if (!isRecord(item)) {
      errors.push(`trialPlan[${index}] must be an object`);
      return;
    }

    validateStringLength(item.id, `trialPlan[${index}].id`, 1, 80, errors);
    validateStringLength(item.label, `trialPlan[${index}].label`, 1, 120, errors);
    validateStringLength(item.targetPresetId, `trialPlan[${index}].targetPresetId`, 1, 128, errors);
    validateStringLength(item.noiseRecipeId, `trialPlan[${index}].noiseRecipeId`, 1, 80, errors);
  });
}

function validateTinyMlTrials(value, errors) {
  validateArray(value, "trials", 1, 500, errors);

  if (!Array.isArray(value)) {
    return;
  }

  value.forEach((item, index) => {
    const path = `trials[${index}]`;

    if (!isRecord(item)) {
      errors.push(`${path} must be an object`);
      return;
    }

    validateCompactId(item.trialId, `${path}.trialId`, errors);
    validateStringLength(item.targetPresetId, `${path}.targetPresetId`, 1, 128, errors);
    validateStringLength(item.targetPattern, `${path}.targetPattern`, 1, 160, errors);

    if (!TINYML_TOPOLOGIES.includes(item.topology)) {
      errors.push(`${path}.topology is invalid`);
    }

    validateTinyMlNoiseRecipe(item.noiseRecipe, `${path}.noiseRecipe`, errors);
    validateTinyMlStrokeArray(item.rawStrokes, `${path}.rawStrokes`, errors);
    validateTinyMlStrokeArray(item.noisyStrokes, `${path}.noisyStrokes`, errors);
    validateTinyMlRecognitionSummary(item.rawRecognition, `${path}.rawRecognition`, errors);
    validateTinyMlRecognitionSummary(item.noisyRecognition, `${path}.noisyRecognition`, errors);

    if (!isRecord(item.contrast)) {
      errors.push(`${path}.contrast must be an object`);
    }

    validateNumber(item.elapsedMs, `${path}.elapsedMs`, 0, 600000, errors);
    validateTinyMlCanvas(item.canvas, `${path}.canvas`, errors);

    if (typeof item.userMarkedConfused !== "boolean") {
      errors.push(`${path}.userMarkedConfused must be boolean`);
    }

    if (item.pointerType !== undefined) {
      validateStringLength(item.pointerType, `${path}.pointerType`, 0, 32, errors);
    }
  });
}

function validateTinyMlNoiseRecipe(value, path, errors) {
  if (!isRecord(value)) {
    errors.push(`${path} must be an object`);
    return;
  }

  if (!TINYML_NOISE_RECIPE_IDS.includes(value.id)) {
    errors.push(`${path}.id is invalid`);
  }

  if (!isRecord(value.settings)) {
    errors.push(`${path}.settings must be an object`);
  }
}

function validateTinyMlStrokeArray(value, path, errors) {
  validateArray(value, path, 1, 16, errors);

  if (!Array.isArray(value)) {
    return;
  }

  value.forEach((stroke, strokeIndex) => {
    if (!isRecord(stroke)) {
      errors.push(`${path}[${strokeIndex}] must be an object`);
      return;
    }

    validateStringLength(stroke.id, `${path}[${strokeIndex}].id`, 1, 128, errors);

    if (!Array.isArray(stroke.points) || stroke.points.length < 1 || stroke.points.length > 512) {
      errors.push(`${path}[${strokeIndex}].points must contain 1-512 points`);
      return;
    }

    stroke.points.forEach((point, pointIndex) => {
      if (!isRecord(point)) {
        errors.push(`${path}[${strokeIndex}].points[${pointIndex}] must be an object`);
        return;
      }

      validateNumber(point.x, `${path}[${strokeIndex}].points[${pointIndex}].x`, -2000, 3000, errors);
      validateNumber(point.y, `${path}[${strokeIndex}].points[${pointIndex}].y`, -2000, 3000, errors);
      validateNumber(point.t, `${path}[${strokeIndex}].points[${pointIndex}].t`, 0, 600000, errors);

      if (point.pressure !== undefined) {
        validateNumber(point.pressure, `${path}[${strokeIndex}].points[${pointIndex}].pressure`, 0, 1, errors);
      }
    });
  });
}

function validateTinyMlRecognitionSummary(value, path, errors) {
  if (!isRecord(value)) {
    errors.push(`${path} must be an object`);
    return;
  }

  validateStringLength(value.selectedCandidateId, `${path}.selectedCandidateId`, 1, 128, errors);
  validateStringLength(value.finalCandidateId, `${path}.finalCandidateId`, 1, 128, errors);

  if (!["recognized", "ambiguous", "incomplete", "invalid"].includes(String(value.finalStatus))) {
    errors.push(`${path}.finalStatus is invalid`);
  }

  validateNumber(value.score, `${path}.score`, 0, 1, errors);
  validateNumber(value.shadowConfidence, `${path}.shadowConfidence`, 0, 1, errors);
  validateNumber(value.meaningConfidence, `${path}.meaningConfidence`, 0, 1, errors);
  validateNumber(value.unsafeRisk, `${path}.unsafeRisk`, 0, 1, errors);
  validateNumber(value.flipRisk, `${path}.flipRisk`, 0, 1, errors);

  if (!Array.isArray(value.topCandidates) || value.topCandidates.length < 1 || value.topCandidates.length > 5) {
    errors.push(`${path}.topCandidates must contain 1-5 items`);
    return;
  }

  value.topCandidates.forEach((candidate, index) => {
    if (!isRecord(candidate)) {
      errors.push(`${path}.topCandidates[${index}] must be an object`);
      return;
    }

    validateStringLength(candidate.id, `${path}.topCandidates[${index}].id`, 1, 128, errors);
    validateNumber(candidate.score, `${path}.topCandidates[${index}].score`, 0, 1, errors);
  });
}

function validateTinyMlAggregate(value, errors) {
  if (!isRecord(value)) {
    errors.push("aggregate must be an object");
    return;
  }

  for (const key of [
    "trialCount",
    "precisionProxy",
    "recallProxy",
    "unsafeAcceptCount",
    "priorityFlipCount",
    "avgUnsafeRisk",
    "avgFlipRisk"
  ]) {
    validateNumber(value[key], `aggregate.${key}`, 0, key.endsWith("Count") || key === "trialCount" ? 10000 : 1, errors);
  }

  if (!isRecord(value.blockerCounts)) {
    errors.push("aggregate.blockerCounts must be an object");
  }
}

function validateTutorialThresholdEvalCaptures(value, errors) {
  validateArray(value, "captures", 0, 500, errors);

  if (!Array.isArray(value)) {
    return;
  }

  value.forEach((item, index) => {
    const path = `captures[${index}]`;

    if (!isRecord(item)) {
      errors.push(`${path} must be an object`);
      return;
    }

    validateCompactId(item.captureId, `${path}.captureId`, errors);
    validateTutorialThresholdEvalShapeFields(item, path, errors);
    validateTinyMlStrokeArray(item.rawStrokes, `${path}.rawStrokes`, errors);
    validateTinyMlRecognitionSummary(item.recognition, `${path}.recognition`, errors);

    if (!isRecord(item.contrast)) {
      errors.push(`${path}.contrast must be an object`);
    }

    validateTutorialThresholdEvalState(item.thresholdBefore, `${path}.thresholdBefore`, errors);
    validateTutorialThresholdEvalState(item.thresholdAfter, `${path}.thresholdAfter`, errors);
    validateTutorialTinyMlCorrection(item.tinyMlCorrection, `${path}.tinyMlCorrection`, errors);
    validateTutorialThresholdEvalConfusion(item.confusion, `${path}.confusion`, errors);
    validateNumber(item.elapsedMs, `${path}.elapsedMs`, 0, 600000, errors);
    validateStringLength(item.pointerType, `${path}.pointerType`, 0, 32, errors);
    validateStringLength(item.savedAtIso, `${path}.savedAtIso`, 10, 40, errors);
  });
}

function validateTutorialThresholdEvalTrials(value, errors) {
  validateArray(value, "evals", 0, 500, errors);

  if (!Array.isArray(value)) {
    return;
  }

  value.forEach((item, index) => {
    const path = `evals[${index}]`;

    if (!isRecord(item)) {
      errors.push(`${path} must be an object`);
      return;
    }

    validateCompactId(item.trialId, `${path}.trialId`, errors);
    validateTutorialThresholdEvalShapeFields(item, path, errors);
    validateTinyMlStrokeArray(item.rawStrokes, `${path}.rawStrokes`, errors);
    validateTinyMlRecognitionSummary(item.recognition, `${path}.recognition`, errors);

    if (!isRecord(item.contrast)) {
      errors.push(`${path}.contrast must be an object`);
    }

    validateTutorialThresholdEvalState(item.thresholdState, `${path}.thresholdState`, errors);
    validateTutorialTinyMlCorrection(item.tinyMlCorrection, `${path}.tinyMlCorrection`, errors);

    if (!["accept", "hold", "retry"].includes(item.dynamicDecision)) {
      errors.push(`${path}.dynamicDecision is invalid`);
    }

    validateStringLength(item.dynamicReason, `${path}.dynamicReason`, 1, 240, errors);
    validateTutorialThresholdEvalConfusion(item.confusion, `${path}.confusion`, errors);
    validateNumber(item.elapsedMs, `${path}.elapsedMs`, 0, 600000, errors);
    validateStringLength(item.pointerType, `${path}.pointerType`, 0, 32, errors);

    if (typeof item.userMarkedConfused !== "boolean") {
      errors.push(`${path}.userMarkedConfused must be boolean`);
    }

    validateStringLength(item.savedAtIso, `${path}.savedAtIso`, 10, 40, errors);
  });
}

function validateTutorialThresholdEvalShapeFields(item, path, errors) {
  validateStringLength(item.targetPresetId, `${path}.targetPresetId`, 1, 128, errors);
  validateStringLength(item.targetPattern, `${path}.targetPattern`, 1, 240, errors);

  if (!TINYML_TOPOLOGIES.includes(item.topology)) {
    errors.push(`${path}.topology is invalid`);
  }
}

function validateTutorialThresholdEvalState(value, path, errors) {
  if (!isRecord(value)) {
    errors.push(`${path} must be an object`);
    return;
  }

  validateNumber(value.captureCount, `${path}.captureCount`, 0, 10000, errors);
  validateNumber(value.globalMaturity, `${path}.globalMaturity`, 0, 1, errors);
  validateNumber(value.globalScoreLift, `${path}.globalScoreLift`, 0, 1, errors);
  validateNumber(value.acceptThreshold, `${path}.acceptThreshold`, 0, 1, errors);
  validateNumber(value.holdThreshold, `${path}.holdThreshold`, 0, 1, errors);
  validateNumber(value.unsafeLimit, `${path}.unsafeLimit`, 0, 1, errors);
  validateNumber(value.flipLimit, `${path}.flipLimit`, 0, 1, errors);
  validateNumber(value.targetRankLimit, `${path}.targetRankLimit`, 1, 5, errors);
  validateNumber(value.topGapFloor, `${path}.topGapFloor`, 0, 1, errors);

  if (!isRecord(value.targetAdjustments)) {
    errors.push(`${path}.targetAdjustments must be an object`);
    return;
  }

  for (const [id, targetState] of Object.entries(value.targetAdjustments).slice(0, 80)) {
    const targetPath = `${path}.targetAdjustments.${id}`;

    if (!isRecord(targetState)) {
      errors.push(`${targetPath} must be an object`);
      continue;
    }

    validateNumber(targetState.captureCount, `${targetPath}.captureCount`, 0, 10000, errors);
    validateNumber(targetState.evalCount, `${targetPath}.evalCount`, 0, 10000, errors);
    validateNumber(targetState.top1Rate, `${targetPath}.top1Rate`, 0, 1, errors);
    validateNumber(targetState.confusionScore, `${targetPath}.confusionScore`, 0, 1, errors);
    validateNumber(targetState.acceptThreshold, `${targetPath}.acceptThreshold`, 0, 1, errors);
  }

  if (Object.keys(value.targetAdjustments).length > 80) {
    errors.push(`${path}.targetAdjustments must contain 0-80 items`);
  }
}

function validateTutorialTinyMlSessionState(value, path, errors) {
  if (value === undefined) {
    return;
  }

  if (!isRecord(value)) {
    errors.push(`${path} must be an object`);
    return;
  }

  validateNumber(value.correctionCount, `${path}.correctionCount`, 0, 10000, errors);
  validateNumber(value.promoteCount, `${path}.promoteCount`, 0, 10000, errors);
  validateNumber(value.shadowBlockCount, `${path}.shadowBlockCount`, 0, 10000, errors);
  validateNumber(value.avgDelta, `${path}.avgDelta`, -1, 1, errors);
  validateStringLength(value.lastFinalDecision, `${path}.lastFinalDecision`, 1, 16, errors);
}

function validateTutorialTinyMlCorrection(value, path, errors) {
  if (value === undefined) {
    return;
  }

  if (!isRecord(value)) {
    errors.push(`${path} must be an object`);
    return;
  }

  validateStringLength(value.version, `${path}.version`, 1, 40, errors);
  validateTutorialTinyMlTrack(value.shadowTrack, `${path}.shadowTrack`, errors);
  validateTutorialTinyMlTrack(value.meaningTrack, `${path}.meaningTrack`, errors);

  if (!["agree_accept", "agree_hold", "agree_retry", "contrast"].includes(value.agreement)) {
    errors.push(`${path}.agreement is invalid`);
  }

  validateNumber(value.delta, `${path}.delta`, -1, 1, errors);

  if (!["shadow_gate", "meaning_recovery", "balanced"].includes(value.selectedTrack)) {
    errors.push(`${path}.selectedTrack is invalid`);
  }

  if (!["accept", "hold", "retry"].includes(value.finalDecision)) {
    errors.push(`${path}.finalDecision is invalid`);
  }

  validateStringLength(value.finalReason, `${path}.finalReason`, 1, 260, errors);

  if (typeof value.promotePriority !== "boolean") {
    errors.push(`${path}.promotePriority must be boolean`);
  }

  if (typeof value.blockPriorityFlip !== "boolean") {
    errors.push(`${path}.blockPriorityFlip must be boolean`);
  }
}

function validateTutorialTinyMlTrack(value, path, errors) {
  if (!isRecord(value)) {
    errors.push(`${path} must be an object`);
    return;
  }

  if (!["shadow_gate", "meaning_recovery"].includes(value.track)) {
    errors.push(`${path}.track is invalid`);
  }

  validateStringLength(value.label, `${path}.label`, 1, 80, errors);
  validateNumber(value.adjustedScore, `${path}.adjustedScore`, 0, 1, errors);
  validateNumber(value.threshold, `${path}.threshold`, 0, 1, errors);
  validateNumber(value.margin, `${path}.margin`, -1, 1, errors);

  if (!["accept", "hold", "retry"].includes(value.decision)) {
    errors.push(`${path}.decision is invalid`);
  }

  validateNumber(value.correction, `${path}.correction`, -1, 1, errors);

  if (!Array.isArray(value.reasons) || value.reasons.length > 8) {
    errors.push(`${path}.reasons must contain 0-8 items`);
    return;
  }

  value.reasons.forEach((reason, index) => {
    validateStringLength(reason, `${path}.reasons[${index}]`, 1, 120, errors);
  });
}

function validateTutorialThresholdEvalConfusion(value, path, errors) {
  if (!isRecord(value)) {
    errors.push(`${path} must be an object`);
    return;
  }

  if (value.targetRank !== null) {
    validateNumber(value.targetRank, `${path}.targetRank`, 1, 50, errors);
  }

  validateStringLength(value.topPair, `${path}.topPair`, 1, 260, errors);
  validateNumber(value.topGap, `${path}.topGap`, 0, 1, errors);

  if (typeof value.targetInTop5 !== "boolean") {
    errors.push(`${path}.targetInTop5 must be boolean`);
  }

  validateStringLength(value.confusedWith, `${path}.confusedWith`, 1, 128, errors);
  validateNumber(value.confusionScore, `${path}.confusionScore`, 0, 1, errors);
}

function validateTutorialThresholdEvalAggregate(value, errors) {
  if (!isRecord(value)) {
    errors.push("aggregate must be an object");
    return;
  }

  for (const key of ["acceptRate", "top1Rate", "avgUnsafeRisk", "avgConfusion"]) {
    validateNumber(value[key], `aggregate.${key}`, 0, 1, errors);
  }
}

function validateTinyMlCanvas(value, path, errors) {
  if (!isRecord(value)) {
    errors.push(`${path} must be an object`);
    return;
  }

  validateNumber(value.width, `${path}.width`, 100, 4000, errors);
  validateNumber(value.height, `${path}.height`, 100, 4000, errors);
}

function rejectForbiddenDrawingFields(value, path, errors) {
  for (const field of FORBIDDEN_DRAWING_FIELDS) {
    if (field in value) {
      errors.push(`${path}.${field} must not be submitted`);
    }
  }
}

function rejectForbiddenGuessTrialFields(value, path, errors) {
  for (const field of FORBIDDEN_GUESS_TRIAL_FIELDS) {
    if (field in value) {
      errors.push(`${path}.${field} must not be submitted`);
    }
  }
}

function validateArray(value, path, min, max, errors) {
  if (!Array.isArray(value) || value.length < min || value.length > max) {
    errors.push(`${path} must contain ${min}-${max} items`);
  }
}

function validateAsciiRows(value, path, errors) {
  if (!Array.isArray(value) || value.length !== 50) {
    errors.push(`${path} must contain 50 rows`);
    return;
  }

  value.forEach((row, index) => {
    validateStringLength(row, `${path}[${index}]`, 50, 50, errors);
  });
}

function validateLikert(value, path, errors) {
  if (![1, 2, 3, 4, 5].includes(Number(value))) {
    errors.push(`${path} must be a 1-5 score`);
  }
}

function validateLikertOrNotApplicable(value, path, errors) {
  if (value === "not_applicable") {
    return;
  }

  validateLikert(value, path, errors);
}

function validateEffectHeard(value, path, errors) {
  if (!["yes", "no", "not_applicable"].includes(String(value))) {
    errors.push(`${path} must be yes, no, or not_applicable`);
  }
}

function validatePromptWord(value, path, errors) {
  if (!SURVEY_PROMPT_WORDS.includes(value)) {
    errors.push(`${path} is invalid`);
  }
}

function validateGuessWord(value, path, errors) {
  if (!SURVEY_GUESS_WORDS.includes(value)) {
    errors.push(`${path} is invalid`);
  }
}

function validateNumber(value, path, min, max, errors) {
  if (typeof value !== "number" || !Number.isFinite(value) || value < min || value > max) {
    errors.push(`${path} must be a number between ${min} and ${max}`);
  }
}

function validateCompactId(value, path, errors) {
  if (typeof value !== "string" || !/^[a-zA-Z0-9_-]{8,128}$/.test(value)) {
    errors.push(`${path} must be a compact id`);
  }
}

function validateStringLength(value, path, min, max, errors) {
  if (typeof value !== "string" || value.length < min || value.length > max) {
    errors.push(`${path} must be a string with length ${min}-${max}`);
  }
}

function requireString(value, key, errors) {
  if (typeof value[key] !== "string" || value[key].length === 0) {
    errors.push(`${key} is required`);
  }
}

function optionalTrimmedString(value) {
  return typeof value === "string" ? value.trim() : "";
}

function isRecord(value) {
  return Boolean(value) && typeof value === "object" && !Array.isArray(value);
}

class PayloadTooLargeError extends Error {}

if (import.meta.url === pathToFileURL(process.argv[1] ?? "").href) {
  const port = Number(process.env.SURVEY_API_PORT ?? 4174);
  const host = process.env.SURVEY_API_HOST ?? "127.0.0.1";
  const { server, responsePath, raffleContactPath, tinyMlNoisyEvalResponsePath, tutorialThresholdEvalResponsePath } =
    createSurveyApiServer();

  server.listen(port, host, () => {
    console.log(`survey api listening on http://${host}:${port}`);
    console.log(`survey responses append to ${responsePath}`);
    console.log(`survey raffle contacts append to ${raffleContactPath}`);
    console.log(`tinyml noisy eval responses append to ${tinyMlNoisyEvalResponsePath}`);
    console.log(`tutorial threshold eval responses append to ${tutorialThresholdEvalResponsePath}`);
  });
}
