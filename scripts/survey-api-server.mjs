import { createServer } from "node:http";
import { createHash, randomUUID, timingSafeEqual } from "node:crypto";
import { mkdir, appendFile, readFile } from "node:fs/promises";
import { join, resolve } from "node:path";
import { pathToFileURL } from "node:url";

export const SURVEY_SCHEMA_VERSION = "magic-symbol-survey-v3";
export const SURVEY_EXPERIMENT_GROUPS = ["shape_only", "scent_effects", "tutorial_quality"];
export const MAX_BODY_BYTES = 32 * 1024;
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
const FORBIDDEN_GUESS_TRIAL_FIELDS = ["trialId", "correct", "confidence"];
const FORBIDDEN_RESPONSE_FIELDS = ["phone", "email", "raffleContact"];

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

export function createSurveyApiServer(options = {}) {
  const sessions = new Map();
  const rateBuckets = new Map();
  const dataDir = resolve(options.dataDir ?? join(process.cwd(), "data"));
  const responsePath = resolve(options.responsePath ?? join(dataDir, "survey-responses.ndjson"));
  const raffleContactPath = resolve(options.raffleContactPath ?? join(dataDir, "survey-raffle-contacts.ndjson"));
  const allowedOrigins = new Set(options.allowedOrigins ?? readAllowedOrigins());
  const now = options.now ?? (() => Date.now());
  const completedGroupCounts = createExperimentGroupCounts(options.initialExperimentGroupCounts);
  const activeGroupCounts = createExperimentGroupCounts();
  let responseLogCountsLoaded = Boolean(options.initialExperimentGroupCounts);
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
      cleanupExpiredSessions(sessions, activeGroupCounts, timestamp);

      if (request.method === "GET" && url.pathname === "/api/survey-session") {
        await ensureResponseLogCountsLoaded();
        const sessionId = randomUUID();
        const csrfToken = randomUUID();
        const experimentGroup = assignBalancedExperimentGroup(
          mergeExperimentGroupCounts(completedGroupCounts, activeGroupCounts),
          sessionId
        );
        incrementExperimentGroupCount(activeGroupCounts, experimentGroup);

        sessions.set(sessionId, {
          csrfToken,
          experimentGroup,
          createdAt: timestamp,
          assignmentCounted: true,
          completed: false,
          submissionIds: new Set(),
          raffleContactSubmissionIds: new Set()
        });

        response.setHeader("Set-Cookie", [
          `survey_session=${sessionId}; HttpOnly; SameSite=Lax; Path=/; Max-Age=7200`
        ]);
        sendJson(response, 200, { sessionId, csrfToken, experimentGroup });
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
        markSessionCompleted(session, completedGroupCounts, activeGroupCounts);

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
    experimentGroupCounts: {
      completed: completedGroupCounts,
      active: activeGroupCounts
    }
  };

  async function ensureResponseLogCountsLoaded() {
    if (responseLogCountsLoaded) {
      return;
    }

    responseLogCountsPromise ??= readFile(responsePath, "utf8")
      .then((text) => {
        const parsedCounts = countExperimentGroupsFromResponseLog(text);

        for (const group of SURVEY_EXPERIMENT_GROUPS) {
          completedGroupCounts.set(group, parsedCounts[group] ?? 0);
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

function sanitizeCount(value) {
  const numberValue = Number(value);
  return Number.isFinite(numberValue) && numberValue > 0 ? Math.floor(numberValue) : 0;
}

function incrementExperimentGroupCount(counts, group) {
  counts.set(group, (counts.get(group) ?? 0) + 1);
}

function decrementExperimentGroupCount(counts, group) {
  counts.set(group, Math.max(0, (counts.get(group) ?? 0) - 1));
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

function cleanupExpiredSessions(sessions, activeGroupCounts, timestamp) {
  for (const [sessionId, session] of sessions) {
    if (timestamp - session.createdAt <= SESSION_TTL_MS) {
      continue;
    }

    if (session.assignmentCounted) {
      decrementExperimentGroupCount(activeGroupCounts, session.experimentGroup);
      session.assignmentCounted = false;
    }

    sessions.delete(sessionId);
  }
}

function markSessionCompleted(session, completedGroupCounts, activeGroupCounts) {
  if (session.completed) {
    return;
  }

  if (session.assignmentCounted) {
    decrementExperimentGroupCount(activeGroupCounts, session.experimentGroup);
    session.assignmentCounted = false;
  }

  incrementExperimentGroupCount(completedGroupCounts, session.experimentGroup);
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

  validateLikert(value.turnTutorialRating, "engineComparison.turnTutorialRating", errors);
  validateLikert(value.contractClarityRating, "engineComparison.contractClarityRating", errors);

  if (!["turn_tutorial", "contract_notes", "same"].includes(String(value.preferredMode))) {
    errors.push("engineComparison.preferredMode is invalid");
  }

  validateStringLength(value.interactionSummary, "engineComparison.interactionSummary", 1, 300, errors);
  validateAsciiRows(value.asciiBefore, "engineComparison.asciiBefore", errors);
  validateAsciiRows(value.asciiAfter, "engineComparison.asciiAfter", errors);
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
        point.length !== 2 ||
        !point.every((coordinate) => Number.isInteger(coordinate) && coordinate >= 0 && coordinate <= 1000)
      ) {
        errors.push(`${path}[${strokeIndex}][${pointIndex}] must be [x,y] integers between 0 and 1000`);
      }
    });
  });
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
    validateNumber(item.reactionMs, `wordGuessTrials[${index}].reactionMs`, 0, 600000, errors);

    if (typeof item.hintsEnabled !== "boolean") {
      errors.push(`wordGuessTrials[${index}].hintsEnabled must be boolean`);
    }

    if (typeof item.effectPlayed !== "boolean") {
      errors.push(`wordGuessTrials[${index}].effectPlayed must be boolean`);
    }
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
    "scentHelpfulness",
    "overallClarity"
  ]) {
    validateLikert(value[key], `selfReport.${key}`, errors);
  }

  validateStringLength(value.strengths, "selfReport.strengths", 0, 1000, errors);
  validateStringLength(value.weaknesses, "selfReport.weaknesses", 0, 1000, errors);
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
  const { server, responsePath, raffleContactPath } = createSurveyApiServer();

  server.listen(port, host, () => {
    console.log(`survey api listening on http://${host}:${port}`);
    console.log(`survey responses append to ${responsePath}`);
    console.log(`survey raffle contacts append to ${raffleContactPath}`);
  });
}
