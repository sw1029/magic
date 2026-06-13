using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace MagicExamHall
{
    [Serializable]
    public sealed class AttemptLog
    {
        public string sessionId = "";
        public string trialId = "";
        public string targetFamily = "";
        public string recognizedFamily = "";
        public string phase = "";
        public string baseFamily = "";
        public string overlayStack = "";
        public string sealId = "";
        public string floorId = "";
        public string targetObject = "";
        public string worldEffect = "";
        public string customShapeId = "";
        public string customShapeLabel = "";
        public string customShapeToken = "";
        public string mappedFamily = "";
        public string customEventId = "";
        public string customEventLabel = "";
        public string customEventKind = "";
        public string customEventRole = "";
        public bool customEventUsesDirection;
        public bool customEventOperatorOnly;
        public bool customEventBlocks;
        public bool customEventBlocked;
        public string customEventBlockedBy = "";
        public float customEventOriginX;
        public float customEventOriginY;
        public float customEventDirectionX;
        public float customEventDirectionY;
        public string status = "";
        public float confidence;
        public float customScore;
        public float defaultSimilarityScore;
        public string intentFamily = "";
        public string intentGoalId = "";
        public string intentSource = "";
        public float intentStrength;
        public float intentSimilarityScore;
        public bool intentWeakConsiderationApplied;
        public int intentTutorialCaptureCount;
        public bool intentStrongConsiderationEnabled;
        public string preIntentFamily = "";
        public float preIntentConfidence;
        public bool intentStrongConsiderationApplied;
        public float intentScoreLift;
        public float closure;
        public float smoothness;
        public float tempo;
        public float stability;
        public float rotationBias;
        public float worldX;
        public float worldY;
        public int bufferStrokeCount;
        public int attemptIndex;
        public int elapsedMs;
        public bool feedbackViewed;
        public bool success;
        public bool hintShown;
        public int assistLevel;
        public bool assisted;
    }

    [Serializable]
    public sealed class SurveyLog
    {
        public string sessionId = "";
        public int clarity;
        public int fairness;
        public int feedbackHelpfulness;
        public int controlFeeling;
        public int immersion;
        public string comment = "";
        public int completedTrials;
        public int totalAttempts;
    }

    [Serializable]
    public sealed class QuestChecklistLog
    {
        public string sessionId = "";
        public string floorId = "";
        public string floorTitle = "";
        public string reason = "";
        public int completed;
        public int total;
        public int globalCompleted;
        public int globalTotal;
        public int elapsedMs;
        public string items = "";
    }

    [Serializable]
    public sealed class ActionEventLog
    {
        public string sessionId = "";
        public string eventId = "";
        public string utc = "";
        public int elapsedMs;
        public int floorElapsedMs;
        public string floorId = "";
        public string floorTitle = "";
        public string eventType = "";
        public string actor = "";
        public string phase = "";
        public string targetId = "";
        public string targetLabel = "";
        public float positionX;
        public float positionY;
        public string inputSessionId = "";
        public string strokeId = "";
        public int strokePointCount;
        public string value = "";
        public string payloadJson = "";
    }

    [Serializable]
    public sealed class CertificateLog
    {
        public string sessionId = "";
        public string issuedAtUtc = "";
        public string buildVersion = "";
        public string title = "";
        public string recipient = "";
        public string status = "";
        public string certificateNote = "";
        public string finalTasks = "";
        public int completedFinalGoals;
        public int totalFinalGoals;
        public int totalElapsedMs;
        public int totalAttempts;
        public int totalSuccess;
        public float successRate;
        public float questCompletionRate;
        public string outputDirectory = "";
    }

    [Serializable]
    public sealed class EnrollmentLog
    {
        public string sessionId = "";
        public string issuedAtUtc = "";
        public string buildVersion = "";
        public string status = "";
        public int currentFloor;
        public string currentFloorTitle = "";
        public int completedGoals;
        public int totalGoals;
        public int globalCompletedGoals;
        public int globalTotalGoals;
        public int completedFinalGoals;
        public int totalFinalGoals;
        public string currentFinalTask = "";
        public int totalElapsedMs;
        public int totalAttempts;
        public string lastEventType = "";
        public string lastEventUtc = "";
        public string outputDirectory = "";
    }

    [Serializable]
    public sealed class SessionResultContextLog
    {
        public string sessionId = "";
        public string buildVersion = "";
        public string conditionId = "";
        public string generatedAtUtc = "";
        public int floorCount;
        public string[] floorTitles = Array.Empty<string>();
        public int totalElapsedMs;
        public bool trueEnding;
        public int completedFinalGoals;
        public int totalFinalGoals;
        public int discoveryCount;
    }

    [Serializable]
    public sealed class SessionResultLog
    {
        public string sessionId = "";
        public string buildVersion = "";
        public string conditionId = "";
        public string generatedAtUtc = "";
        public string metricModelVersion = "gqm-hci-v1";
        public string gqmGoal = "Evaluate intuitive learning of symbolic magic words and rules through shape expression, hints, tutorials, and interactions.";
        public string questionACoverage = "A: shape expression and word inference from recognition attempts, first success, confidence, and quality.";
        public string questionBCoverage = "B: explanation burden and learning support from hints, assist, quest checklist, elapsed time, and redraw/failure pressure.";
        public bool trueEnding;
        public int floorCount;
        public int floorsVisited;
        public int floorsCompleted;
        public int totalElapsedMs;
        public int totalAttempts;
        public int totalSuccess;
        public int totalFailures;
        public int totalBaseAttempts;
        public int totalOverlayAttempts;
        public int totalCustomAttempts;
        public int totalCustomAcceptedAttempts;
        public int totalHintShown;
        public int totalAssistedSuccess;
        public int maxAssistLevel;
        public int completedFinalGoals;
        public int totalFinalGoals;
        public int totalQuestCompleted;
        public int totalQuestTotal;
        public int discoveryCount;
        public float successRate;
        public float firstAttemptSuccessRate;
        public float hintRate;
        public float assistedSuccessRate;
        public float questCompletionRate;
        public float averageConfidence;
        public float averageQuality;
        public float averageClosure;
        public float averageSmoothness;
        public float averageTempo;
        public float averageStability;
        public float averageRotationBias;
        public float averageTimeToFirstSuccessMs;
        public float gqmA1ShapeDifficultyScore;
        public float gqmA2WordInferenceAccuracy;
        public float gqmB1UnderstandingDeltaProxy;
        public float gqmB2LearningBurdenScore;
        public string coverageNotes = "";
        public HciFloorResultLog[] floors = Array.Empty<HciFloorResultLog>();
    }

    [Serializable]
    public sealed class HciFloorResultLog
    {
        public string sessionId = "";
        public int floorId;
        public string floorTitle = "";
        public string exitReason = "";
        public int elapsedMs;
        public int firstAttemptElapsedMs;
        public int timeToFirstSuccessMs;
        public int totalGoals;
        public int completedGoals;
        public int questCompleted;
        public int questTotal;
        public int attempts;
        public int successes;
        public int failures;
        public int baseAttempts;
        public int overlayAttempts;
        public int customAttempts;
        public int customAcceptedAttempts;
        public int hintShown;
        public int assistedSuccess;
        public int maxAssistLevel;
        public int sameTargetFailureStreakMax;
        public bool firstAttemptSuccess;
        public float goalCompletionRate;
        public float questCompletionRate;
        public float successRate;
        public float hintRate;
        public float averageConfidence;
        public float averageQuality;
        public float averageClosure;
        public float averageSmoothness;
        public float averageTempo;
        public float averageStability;
        public float averageRotationBias;
        public float gqmA1ShapeDifficultyScore;
        public float gqmA2WordInferenceAccuracy;
        public float gqmB1UnderstandingDeltaProxy;
        public float gqmB2LearningBurdenScore;
        public string firstAttemptTarget = "";
        public string weakestQuality = "";
        public string dominantPhase = "";
        public string coverageNotes = "";
    }

    public sealed class ExamLogger
    {
        public const string DisabledOutputDirectory = "log collection disabled";

        public string OutputDirectory { get; }
        public string CertificateOutputDirectory { get; }
        public bool IsCollectionEnabled { get; }
        public string SessionResultJsonPath => sessionResultJsonPath;
        public string SessionResultCsvPath => sessionResultCsvPath;
        public string FloorResultsCsvPath => floorResultsCsvPath;
        public string ActionEventsJsonPath => actionEventsJsonPath;
        public string ActionEventsCsvPath => actionEventsCsvPath;
        public string CertificateCsvPath => certificateCsvPath;
        public string EnrollmentCsvPath => enrollmentCsvPath;
        private readonly string attemptsJsonPath;
        private readonly string attemptsCsvPath;
        private readonly string surveyJsonPath;
        private readonly string surveyCsvPath;
        private readonly string questChecklistJsonPath;
        private readonly string questChecklistCsvPath;
        private readonly string actionEventsJsonPath;
        private readonly string actionEventsCsvPath;
        private readonly string sessionResultJsonPath;
        private readonly string sessionResultCsvPath;
        private readonly string floorResultsCsvPath;
        private readonly string certificateCsvPath;
        private readonly string enrollmentCsvPath;
        private readonly string globalSessionResultsCsvPath;
        private readonly List<AttemptLog> attemptHistory = new();
        private readonly List<SurveyLog> surveyHistory = new();
        private readonly List<QuestChecklistLog> questChecklistHistory = new();
        private readonly List<ActionEventLog> actionEventHistory = new();
        private bool sessionResultAppended;

        public ExamLogger(string sessionId)
            : this(sessionId, "", null)
        {
        }

        public ExamLogger(string sessionId, string outputRoot, bool? enableCollection = null, string certificateOutputRoot = "")
        {
            sessionId = string.IsNullOrWhiteSpace(sessionId) ? "session" : sessionId;
            IsCollectionEnabled = enableCollection ?? !ShouldSuppressCollection(sessionId);
            if (!IsCollectionEnabled)
            {
                OutputDirectory = DisabledOutputDirectory;
                CertificateOutputDirectory = DisabledOutputDirectory;
                attemptsJsonPath = "";
                attemptsCsvPath = "";
                surveyJsonPath = "";
                surveyCsvPath = "";
                questChecklistJsonPath = "";
                questChecklistCsvPath = "";
                actionEventsJsonPath = "";
                actionEventsCsvPath = "";
                sessionResultJsonPath = "";
                sessionResultCsvPath = "";
                floorResultsCsvPath = "";
                certificateCsvPath = "";
                enrollmentCsvPath = "";
                globalSessionResultsCsvPath = "";
                return;
            }

            var usesDefaultOutputRoot = string.IsNullOrWhiteSpace(outputRoot);
            var root = usesDefaultOutputRoot
                ? Path.Combine(Application.persistentDataPath, "MagicExamHallLogs")
                : outputRoot;
            OutputDirectory = Path.Combine(root, sessionId);
            Directory.CreateDirectory(OutputDirectory);
            CertificateOutputDirectory = string.IsNullOrWhiteSpace(certificateOutputRoot)
                ? usesDefaultOutputRoot ? ResolveExecutableDirectory() : OutputDirectory
                : certificateOutputRoot;
            Directory.CreateDirectory(CertificateOutputDirectory);
            attemptsJsonPath = Path.Combine(OutputDirectory, "attempts.jsonl");
            attemptsCsvPath = Path.Combine(OutputDirectory, "attempts.csv");
            surveyJsonPath = Path.Combine(OutputDirectory, "survey.jsonl");
            surveyCsvPath = Path.Combine(OutputDirectory, "survey.csv");
            questChecklistJsonPath = Path.Combine(OutputDirectory, "quest-checklist.jsonl");
            questChecklistCsvPath = Path.Combine(OutputDirectory, "quest-checklist.csv");
            actionEventsJsonPath = Path.Combine(OutputDirectory, "action-events.jsonl");
            actionEventsCsvPath = Path.Combine(OutputDirectory, "action-events.csv");
            sessionResultJsonPath = Path.Combine(OutputDirectory, "session-result.json");
            sessionResultCsvPath = Path.Combine(OutputDirectory, "session-result.csv");
            floorResultsCsvPath = Path.Combine(OutputDirectory, "floor-results.csv");
            certificateCsvPath = Path.Combine(CertificateOutputDirectory, "\uC218\uB8CC\uC99D.csv");
            enrollmentCsvPath = Path.Combine(CertificateOutputDirectory, "\uC7AC\uD559\uC99D\uC11C.csv");
            globalSessionResultsCsvPath = Path.Combine(root, "session-results.csv");
            EnsureAttemptHeader();
            EnsureSurveyHeader();
            EnsureQuestChecklistHeader();
            EnsureActionEventHeader();
        }

        private static string ResolveExecutableDirectory()
        {
            var dataPath = Application.dataPath;
            if (!string.IsNullOrWhiteSpace(dataPath))
            {
                var dataDirectory = new DirectoryInfo(dataPath);
                if (dataDirectory.Exists && dataDirectory.Name.EndsWith("_Data", StringComparison.OrdinalIgnoreCase))
                {
                    return dataDirectory.Parent?.FullName ?? dataDirectory.FullName;
                }

                if (Application.isEditor)
                {
                    return dataDirectory.Parent?.FullName ?? dataDirectory.FullName;
                }
            }

            var baseDirectory = AppContext.BaseDirectory;
            return string.IsNullOrWhiteSpace(baseDirectory)
                ? Directory.GetCurrentDirectory()
                : Path.GetFullPath(baseDirectory);
        }

        public void LogAttempt(AttemptLog log)
        {
            if (!IsCollectionEnabled)
            {
                return;
            }

            attemptHistory.Add(log);
            File.AppendAllText(attemptsJsonPath, JsonUtility.ToJson(log) + Environment.NewLine, Encoding.UTF8);
            File.AppendAllText(attemptsCsvPath, string.Join(",",
                Csv(log.sessionId),
                Csv(log.trialId),
                Csv(log.targetFamily),
                Csv(log.recognizedFamily),
                Csv(log.phase),
                Csv(log.baseFamily),
                Csv(log.overlayStack),
                Csv(log.sealId),
                Csv(log.floorId),
                Csv(log.targetObject),
                Csv(log.worldEffect),
                Csv(log.customShapeId),
                Csv(log.customShapeLabel),
                Csv(log.customShapeToken),
                Csv(log.mappedFamily),
                Csv(log.customEventId),
                Csv(log.customEventLabel),
                Csv(log.customEventKind),
                Csv(log.customEventRole),
                Bool(log.customEventUsesDirection),
                Bool(log.customEventOperatorOnly),
                Bool(log.customEventBlocks),
                Bool(log.customEventBlocked),
                Csv(log.customEventBlockedBy),
                Float(log.customEventOriginX),
                Float(log.customEventOriginY),
                Float(log.customEventDirectionX),
                Float(log.customEventDirectionY),
                Csv(log.status),
                Float(log.confidence),
                Float(log.customScore),
                Float(log.defaultSimilarityScore),
                Csv(log.intentFamily),
                Csv(log.intentGoalId),
                Csv(log.intentSource),
                Float(log.intentStrength),
                Float(log.intentSimilarityScore),
                Bool(log.intentWeakConsiderationApplied),
                log.intentTutorialCaptureCount.ToString(CultureInfo.InvariantCulture),
                Bool(log.intentStrongConsiderationEnabled),
                Csv(log.preIntentFamily),
                Float(log.preIntentConfidence),
                Bool(log.intentStrongConsiderationApplied),
                Float(log.intentScoreLift),
                Float(log.closure),
                Float(log.smoothness),
                Float(log.tempo),
                Float(log.stability),
                Float(log.rotationBias),
                Float(log.worldX),
                Float(log.worldY),
                log.bufferStrokeCount,
                log.attemptIndex,
                log.elapsedMs,
                Bool(log.feedbackViewed),
                Bool(log.success),
                Bool(log.hintShown),
                log.assistLevel,
                Bool(log.assisted)) + Environment.NewLine, Encoding.UTF8);
        }

        public void LogSurvey(SurveyLog log)
        {
            if (!IsCollectionEnabled)
            {
                return;
            }

            surveyHistory.Add(log);
            File.AppendAllText(surveyJsonPath, JsonUtility.ToJson(log) + Environment.NewLine, Encoding.UTF8);
            File.AppendAllText(surveyCsvPath, string.Join(",",
                Csv(log.sessionId),
                log.clarity,
                log.fairness,
                log.feedbackHelpfulness,
                log.controlFeeling,
                log.immersion,
                Csv(log.comment),
                log.completedTrials,
                log.totalAttempts) + Environment.NewLine, Encoding.UTF8);
        }

        public void LogQuestChecklist(QuestChecklistLog log)
        {
            if (!IsCollectionEnabled)
            {
                return;
            }

            questChecklistHistory.Add(log);
            File.AppendAllText(questChecklistJsonPath, JsonUtility.ToJson(log) + Environment.NewLine, Encoding.UTF8);
            File.AppendAllText(questChecklistCsvPath, string.Join(",",
                Csv(log.sessionId),
                Csv(log.floorId),
                Csv(log.floorTitle),
                Csv(log.reason),
                log.completed,
                log.total,
                log.globalCompleted,
                log.globalTotal,
                log.elapsedMs,
                Csv(log.items)) + Environment.NewLine, Encoding.UTF8);
        }

        public void LogActionEvent(ActionEventLog log)
        {
            if (!IsCollectionEnabled)
            {
                return;
            }

            log ??= new ActionEventLog();
            if (string.IsNullOrWhiteSpace(log.eventId))
            {
                log.eventId = Guid.NewGuid().ToString("N");
            }

            if (string.IsNullOrWhiteSpace(log.utc))
            {
                log.utc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            }

            actionEventHistory.Add(log);
            File.AppendAllText(actionEventsJsonPath, JsonUtility.ToJson(log) + Environment.NewLine, Encoding.UTF8);
            File.AppendAllText(actionEventsCsvPath, string.Join(",",
                Csv(log.sessionId),
                Csv(log.eventId),
                Csv(log.utc),
                log.elapsedMs,
                log.floorElapsedMs,
                Csv(log.floorId),
                Csv(log.floorTitle),
                Csv(log.eventType),
                Csv(log.actor),
                Csv(log.phase),
                Csv(log.targetId),
                Csv(log.targetLabel),
                Float(log.positionX),
                Float(log.positionY),
                Csv(log.inputSessionId),
                Csv(log.strokeId),
                log.strokePointCount,
                Csv(log.value),
                Csv(log.payloadJson)) + Environment.NewLine, Encoding.UTF8);
        }

        public SessionResultLog WriteSessionResult(SessionResultContextLog context)
        {
            if (!IsCollectionEnabled)
            {
                return null;
            }

            context ??= new SessionResultContextLog();
            var result = BuildSessionResult(context);
            File.WriteAllText(sessionResultJsonPath, JsonUtility.ToJson(result, true), Encoding.UTF8);
            WriteSessionResultCsv(result);
            WriteFloorResultsCsv(result.floors);
            AppendGlobalSessionResultCsv(result);
            return result;
        }

        public string WriteCertificateCsv(CertificateLog certificate)
        {
            if (!IsCollectionEnabled)
            {
                return "";
            }

            certificate ??= new CertificateLog();
            if (string.IsNullOrWhiteSpace(certificate.issuedAtUtc))
            {
                certificate.issuedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            }

            if (string.IsNullOrWhiteSpace(certificate.outputDirectory) ||
                string.Equals(certificate.outputDirectory, OutputDirectory, StringComparison.Ordinal))
            {
                certificate.outputDirectory = CertificateOutputDirectory;
            }

            File.WriteAllText(
                certificateCsvPath,
                CertificateCsvHeader() + Environment.NewLine + CertificateCsvRow(certificate) + Environment.NewLine,
                Encoding.UTF8);
            return certificateCsvPath;
        }

        public string WriteEnrollmentCsv(EnrollmentLog enrollment)
        {
            if (!IsCollectionEnabled)
            {
                return "";
            }

            enrollment ??= new EnrollmentLog();
            if (string.IsNullOrWhiteSpace(enrollment.issuedAtUtc))
            {
                enrollment.issuedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            }

            if (string.IsNullOrWhiteSpace(enrollment.outputDirectory) ||
                string.Equals(enrollment.outputDirectory, OutputDirectory, StringComparison.Ordinal))
            {
                enrollment.outputDirectory = CertificateOutputDirectory;
            }

            File.WriteAllText(
                enrollmentCsvPath,
                EnrollmentCsvHeader() + Environment.NewLine + EnrollmentCsvRow(enrollment) + Environment.NewLine,
                Encoding.UTF8);
            return enrollmentCsvPath;
        }

        private void EnsureAttemptHeader()
        {
            if (!File.Exists(attemptsCsvPath))
            {
                File.WriteAllText(
                    attemptsCsvPath,
                    "sessionId,trialId,targetFamily,recognizedFamily,phase,baseFamily,overlayStack,sealId,floorId,targetObject,worldEffect,customShapeId,customShapeLabel,customShapeToken,mappedFamily,customEventId,customEventLabel,customEventKind,customEventRole,customEventUsesDirection,customEventOperatorOnly,customEventBlocks,customEventBlocked,customEventBlockedBy,customEventOriginX,customEventOriginY,customEventDirectionX,customEventDirectionY,status,confidence,customScore,defaultSimilarityScore,intentFamily,intentGoalId,intentSource,intentStrength,intentSimilarityScore,intentWeakConsiderationApplied,intentTutorialCaptureCount,intentStrongConsiderationEnabled,preIntentFamily,preIntentConfidence,intentStrongConsiderationApplied,intentScoreLift,closure,smoothness,tempo,stability,rotationBias,worldX,worldY,bufferStrokeCount,attemptIndex,elapsedMs,feedbackViewed,success,hintShown,assistLevel,assisted" + Environment.NewLine,
                    Encoding.UTF8);
            }
        }

        private void EnsureSurveyHeader()
        {
            if (!File.Exists(surveyCsvPath))
            {
                File.WriteAllText(surveyCsvPath, "sessionId,clarity,fairness,feedbackHelpfulness,controlFeeling,immersion,comment,completedTrials,totalAttempts" + Environment.NewLine, Encoding.UTF8);
            }
        }

        private void EnsureQuestChecklistHeader()
        {
            if (!File.Exists(questChecklistCsvPath))
            {
                File.WriteAllText(questChecklistCsvPath, "sessionId,floorId,floorTitle,reason,completed,total,globalCompleted,globalTotal,elapsedMs,items" + Environment.NewLine, Encoding.UTF8);
            }
        }

        private void EnsureActionEventHeader()
        {
            if (!File.Exists(actionEventsCsvPath))
            {
                File.WriteAllText(actionEventsCsvPath, "sessionId,eventId,utc,elapsedMs,floorElapsedMs,floorId,floorTitle,eventType,actor,phase,targetId,targetLabel,positionX,positionY,inputSessionId,strokeId,strokePointCount,value,payloadJson" + Environment.NewLine, Encoding.UTF8);
            }
        }

        private SessionResultLog BuildSessionResult(SessionResultContextLog context)
        {
            var floorCount = Math.Max(context.floorCount, HighestObservedFloorId());
            var floors = new List<HciFloorResultLog>();
            for (var floorId = 1; floorId <= Math.Max(1, floorCount); floorId++)
            {
                floors.Add(BuildFloorResult(context, floorId));
            }

            var totalAttempts = floors.Sum(floor => floor.attempts);
            var totalSuccess = floors.Sum(floor => floor.successes);
            var totalHintShown = floors.Sum(floor => floor.hintShown);
            var totalAssistedSuccess = floors.Sum(floor => floor.assistedSuccess);
            var totalQuestCompleted = floors.Sum(floor => floor.questCompleted);
            var totalQuestTotal = floors.Sum(floor => floor.questTotal);
            var averageTimeToFirstSuccess = AveragePositive(floors.Select(floor => floor.timeToFirstSuccessMs));
            var allAttempts = attemptHistory.ToArray();
            var qualities = allAttempts.Where(HasQuality).ToArray();
            var result = new SessionResultLog
            {
                sessionId = FirstNonEmpty(context.sessionId, attemptHistory.LastOrDefault()?.sessionId, surveyHistory.LastOrDefault()?.sessionId),
                buildVersion = context.buildVersion ?? "",
                conditionId = context.conditionId ?? "",
                generatedAtUtc = string.IsNullOrWhiteSpace(context.generatedAtUtc)
                    ? DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)
                    : context.generatedAtUtc,
                trueEnding = context.trueEnding,
                floorCount = Math.Max(1, floorCount),
                floorsVisited = floors.Count(floor => floor.attempts > 0 || floor.questTotal > 0 || floor.elapsedMs > 0),
                floorsCompleted = floors.Count(floor => floor.totalGoals > 0 && floor.completedGoals >= floor.totalGoals),
                totalElapsedMs = Math.Max(context.totalElapsedMs, floors.Sum(floor => Math.Max(0, floor.elapsedMs))),
                totalAttempts = totalAttempts,
                totalSuccess = totalSuccess,
                totalFailures = floors.Sum(floor => floor.failures),
                totalBaseAttempts = floors.Sum(floor => floor.baseAttempts),
                totalOverlayAttempts = floors.Sum(floor => floor.overlayAttempts),
                totalCustomAttempts = floors.Sum(floor => floor.customAttempts),
                totalCustomAcceptedAttempts = floors.Sum(floor => floor.customAcceptedAttempts),
                totalHintShown = totalHintShown,
                totalAssistedSuccess = totalAssistedSuccess,
                maxAssistLevel = floors.Count == 0 ? 0 : floors.Max(floor => floor.maxAssistLevel),
                completedFinalGoals = Math.Max(0, context.completedFinalGoals),
                totalFinalGoals = Math.Max(0, context.totalFinalGoals),
                totalQuestCompleted = totalQuestCompleted,
                totalQuestTotal = totalQuestTotal,
                discoveryCount = Math.Max(context.discoveryCount, 0),
                successRate = Ratio(totalSuccess, totalAttempts),
                firstAttemptSuccessRate = Ratio(floors.Count(floor => floor.attempts > 0 && floor.firstAttemptSuccess), floors.Count(floor => floor.attempts > 0)),
                hintRate = Ratio(totalHintShown, totalAttempts),
                assistedSuccessRate = Ratio(totalAssistedSuccess, totalSuccess),
                questCompletionRate = Ratio(totalQuestCompleted, totalQuestTotal),
                averageConfidence = AverageOrZero(allAttempts.Select(attempt => attempt.confidence)),
                averageQuality = AverageOrZero(qualities.Select(QualityAverage)),
                averageClosure = AverageOrZero(qualities.Select(attempt => attempt.closure)),
                averageSmoothness = AverageOrZero(qualities.Select(attempt => attempt.smoothness)),
                averageTempo = AverageOrZero(qualities.Select(attempt => attempt.tempo)),
                averageStability = AverageOrZero(qualities.Select(attempt => attempt.stability)),
                averageRotationBias = AverageOrZero(qualities.Select(attempt => attempt.rotationBias)),
                averageTimeToFirstSuccessMs = averageTimeToFirstSuccess,
                gqmB1UnderstandingDeltaProxy = BuildUnderstandingDelta(allAttempts),
                floors = floors.ToArray(),
                coverageNotes = "raw_strokes=true; action_events=true; test_sessions_excluded=true; attempt+quest+survey+ending_summary=true; movement logged as key/action state changes"
            };
            result.gqmA1ShapeDifficultyScore = ShapeDifficultyScore(
                result.successRate,
                result.hintRate,
                result.averageQuality,
                totalAttempts,
                Math.Max(1, floors.Sum(floor => Math.Max(floor.totalGoals, floor.questTotal))));
            result.gqmA2WordInferenceAccuracy = result.successRate;
            result.gqmB2LearningBurdenScore = BurdenScore(
                result.successRate,
                result.hintRate,
                result.assistedSuccessRate,
                result.averageTimeToFirstSuccessMs,
                Math.Max(1, floors.Count(floor => floor.attempts > 0)));
            return result;
        }

        private HciFloorResultLog BuildFloorResult(SessionResultContextLog context, int floorId)
        {
            var attempts = attemptHistory
                .Where(attempt => TryParseFloorId(attempt.floorId) == floorId)
                .OrderBy(attempt => attempt.attemptIndex)
                .ThenBy(attempt => attempt.elapsedMs)
                .ToArray();
            var quest = questChecklistHistory
                .Where(item => TryParseFloorId(item.floorId) == floorId)
                .OrderBy(item => item.elapsedMs)
                .LastOrDefault();
            var qualities = attempts.Where(HasQuality).ToArray();
            var successes = attempts.Count(attempt => attempt.success);
            var firstAttempt = attempts.FirstOrDefault();
            var firstSuccess = attempts.FirstOrDefault(attempt => attempt.success);
            var totalGoals = quest?.total ?? 0;
            var completedGoals = quest?.completed ?? 0;
            var floorTitle = FirstNonEmpty(
                quest?.floorTitle,
                context.floorTitles != null && floorId - 1 >= 0 && floorId - 1 < context.floorTitles.Length ? context.floorTitles[floorId - 1] : "",
                $"Floor {floorId}");
            var successRate = Ratio(successes, attempts.Length);
            var hintRate = Ratio(attempts.Count(attempt => attempt.hintShown), attempts.Length);
            var averageQuality = AverageOrZero(qualities.Select(QualityAverage));
            var result = new HciFloorResultLog
            {
                sessionId = FirstNonEmpty(context.sessionId, attempts.LastOrDefault()?.sessionId, quest?.sessionId),
                floorId = floorId,
                floorTitle = floorTitle,
                exitReason = quest?.reason ?? "",
                elapsedMs = Math.Max(quest?.elapsedMs ?? 0, attempts.Length == 0 ? 0 : attempts.Max(attempt => attempt.elapsedMs)),
                firstAttemptElapsedMs = firstAttempt?.elapsedMs ?? 0,
                timeToFirstSuccessMs = firstSuccess?.elapsedMs ?? 0,
                totalGoals = totalGoals,
                completedGoals = completedGoals,
                questCompleted = quest?.completed ?? 0,
                questTotal = quest?.total ?? 0,
                attempts = attempts.Length,
                successes = successes,
                failures = attempts.Length - successes,
                baseAttempts = attempts.Count(attempt => string.Equals(attempt.phase, "Base", StringComparison.OrdinalIgnoreCase)),
                overlayAttempts = attempts.Count(attempt => string.Equals(attempt.phase, "Overlay", StringComparison.OrdinalIgnoreCase)),
                customAttempts = attempts.Count(attempt => !string.IsNullOrWhiteSpace(attempt.customShapeId) || !string.IsNullOrWhiteSpace(attempt.customShapeToken)),
                customAcceptedAttempts = attempts.Count(attempt => attempt.success && (!string.IsNullOrWhiteSpace(attempt.customShapeId) || !string.IsNullOrWhiteSpace(attempt.customShapeToken))),
                hintShown = attempts.Count(attempt => attempt.hintShown),
                assistedSuccess = attempts.Count(attempt => attempt.success && attempt.assisted),
                maxAssistLevel = attempts.Length == 0 ? 0 : attempts.Max(attempt => attempt.assistLevel),
                sameTargetFailureStreakMax = SameTargetFailureStreakMax(attempts),
                firstAttemptSuccess = firstAttempt?.success ?? false,
                goalCompletionRate = Ratio(completedGoals, totalGoals),
                questCompletionRate = Ratio(quest?.completed ?? 0, quest?.total ?? 0),
                successRate = successRate,
                hintRate = hintRate,
                averageConfidence = AverageOrZero(attempts.Select(attempt => attempt.confidence)),
                averageQuality = averageQuality,
                averageClosure = AverageOrZero(qualities.Select(attempt => attempt.closure)),
                averageSmoothness = AverageOrZero(qualities.Select(attempt => attempt.smoothness)),
                averageTempo = AverageOrZero(qualities.Select(attempt => attempt.tempo)),
                averageStability = AverageOrZero(qualities.Select(attempt => attempt.stability)),
                averageRotationBias = AverageOrZero(qualities.Select(attempt => attempt.rotationBias)),
                gqmB1UnderstandingDeltaProxy = BuildUnderstandingDelta(attempts),
                firstAttemptTarget = firstAttempt == null ? "" : AttemptTargetKey(firstAttempt),
                weakestQuality = WeakestQualityName(qualities),
                dominantPhase = DominantPhase(attempts),
                coverageNotes = "derived_from_attempts_and_quest_checklist"
            };
            result.gqmA1ShapeDifficultyScore = ShapeDifficultyScore(
                result.successRate,
                result.hintRate,
                result.averageQuality,
                result.attempts,
                Math.Max(1, Math.Max(result.totalGoals, result.questTotal)));
            result.gqmA2WordInferenceAccuracy = result.successRate;
            result.gqmB2LearningBurdenScore = BurdenScore(
                result.successRate,
                result.hintRate,
                Ratio(result.assistedSuccess, result.successes),
                result.timeToFirstSuccessMs,
                1);
            return result;
        }

        private void WriteSessionResultCsv(SessionResultLog result)
        {
            File.WriteAllText(
                sessionResultCsvPath,
                SessionResultCsvHeader() + Environment.NewLine + SessionResultCsvRow(result) + Environment.NewLine,
                Encoding.UTF8);
        }

        private void WriteFloorResultsCsv(IReadOnlyList<HciFloorResultLog> floors)
        {
            var builder = new StringBuilder();
            builder.AppendLine(FloorResultCsvHeader());
            foreach (var floor in floors)
            {
                builder.AppendLine(FloorResultCsvRow(floor));
            }

            File.WriteAllText(floorResultsCsvPath, builder.ToString(), Encoding.UTF8);
        }

        private void AppendGlobalSessionResultCsv(SessionResultLog result)
        {
            if (sessionResultAppended)
            {
                return;
            }

            if (!File.Exists(globalSessionResultsCsvPath))
            {
                File.WriteAllText(globalSessionResultsCsvPath, SessionResultCsvHeader() + Environment.NewLine, Encoding.UTF8);
            }

            File.AppendAllText(globalSessionResultsCsvPath, SessionResultCsvRow(result) + Environment.NewLine, Encoding.UTF8);
            sessionResultAppended = true;
        }

        private static string SessionResultCsvHeader()
        {
            return "sessionId,buildVersion,conditionId,generatedAtUtc,metricModelVersion,trueEnding,floorCount,floorsVisited,floorsCompleted,totalElapsedMs,totalAttempts,totalSuccess,totalFailures,totalBaseAttempts,totalOverlayAttempts,totalCustomAttempts,totalCustomAcceptedAttempts,totalHintShown,totalAssistedSuccess,maxAssistLevel,completedFinalGoals,totalFinalGoals,totalQuestCompleted,totalQuestTotal,discoveryCount,successRate,firstAttemptSuccessRate,hintRate,assistedSuccessRate,questCompletionRate,averageConfidence,averageQuality,averageClosure,averageSmoothness,averageTempo,averageStability,averageRotationBias,averageTimeToFirstSuccessMs,gqmA1ShapeDifficultyScore,gqmA2WordInferenceAccuracy,gqmB1UnderstandingDeltaProxy,gqmB2LearningBurdenScore,coverageNotes";
        }

        private static string SessionResultCsvRow(SessionResultLog result)
        {
            return string.Join(",",
                Csv(result.sessionId),
                Csv(result.buildVersion),
                Csv(result.conditionId),
                Csv(result.generatedAtUtc),
                Csv(result.metricModelVersion),
                Bool(result.trueEnding),
                result.floorCount,
                result.floorsVisited,
                result.floorsCompleted,
                result.totalElapsedMs,
                result.totalAttempts,
                result.totalSuccess,
                result.totalFailures,
                result.totalBaseAttempts,
                result.totalOverlayAttempts,
                result.totalCustomAttempts,
                result.totalCustomAcceptedAttempts,
                result.totalHintShown,
                result.totalAssistedSuccess,
                result.maxAssistLevel,
                result.completedFinalGoals,
                result.totalFinalGoals,
                result.totalQuestCompleted,
                result.totalQuestTotal,
                result.discoveryCount,
                Float(result.successRate),
                Float(result.firstAttemptSuccessRate),
                Float(result.hintRate),
                Float(result.assistedSuccessRate),
                Float(result.questCompletionRate),
                Float(result.averageConfidence),
                Float(result.averageQuality),
                Float(result.averageClosure),
                Float(result.averageSmoothness),
                Float(result.averageTempo),
                Float(result.averageStability),
                Float(result.averageRotationBias),
                Float(result.averageTimeToFirstSuccessMs),
                Float(result.gqmA1ShapeDifficultyScore),
                Float(result.gqmA2WordInferenceAccuracy),
                Float(result.gqmB1UnderstandingDeltaProxy),
                Float(result.gqmB2LearningBurdenScore),
                Csv(result.coverageNotes));
        }

        private static string FloorResultCsvHeader()
        {
            return "sessionId,floorId,floorTitle,exitReason,elapsedMs,firstAttemptElapsedMs,timeToFirstSuccessMs,totalGoals,completedGoals,questCompleted,questTotal,attempts,successes,failures,baseAttempts,overlayAttempts,customAttempts,customAcceptedAttempts,hintShown,assistedSuccess,maxAssistLevel,sameTargetFailureStreakMax,firstAttemptSuccess,goalCompletionRate,questCompletionRate,successRate,hintRate,averageConfidence,averageQuality,averageClosure,averageSmoothness,averageTempo,averageStability,averageRotationBias,gqmA1ShapeDifficultyScore,gqmA2WordInferenceAccuracy,gqmB1UnderstandingDeltaProxy,gqmB2LearningBurdenScore,firstAttemptTarget,weakestQuality,dominantPhase,coverageNotes";
        }

        private static string FloorResultCsvRow(HciFloorResultLog floor)
        {
            return string.Join(",",
                Csv(floor.sessionId),
                floor.floorId,
                Csv(floor.floorTitle),
                Csv(floor.exitReason),
                floor.elapsedMs,
                floor.firstAttemptElapsedMs,
                floor.timeToFirstSuccessMs,
                floor.totalGoals,
                floor.completedGoals,
                floor.questCompleted,
                floor.questTotal,
                floor.attempts,
                floor.successes,
                floor.failures,
                floor.baseAttempts,
                floor.overlayAttempts,
                floor.customAttempts,
                floor.customAcceptedAttempts,
                floor.hintShown,
                floor.assistedSuccess,
                floor.maxAssistLevel,
                floor.sameTargetFailureStreakMax,
                Bool(floor.firstAttemptSuccess),
                Float(floor.goalCompletionRate),
                Float(floor.questCompletionRate),
                Float(floor.successRate),
                Float(floor.hintRate),
                Float(floor.averageConfidence),
                Float(floor.averageQuality),
                Float(floor.averageClosure),
                Float(floor.averageSmoothness),
                Float(floor.averageTempo),
                Float(floor.averageStability),
                Float(floor.averageRotationBias),
                Float(floor.gqmA1ShapeDifficultyScore),
                Float(floor.gqmA2WordInferenceAccuracy),
                Float(floor.gqmB1UnderstandingDeltaProxy),
                Float(floor.gqmB2LearningBurdenScore),
                Csv(floor.firstAttemptTarget),
                Csv(floor.weakestQuality),
                Csv(floor.dominantPhase),
                Csv(floor.coverageNotes));
        }

        private static string CertificateCsvHeader()
        {
            return "sessionId,issuedAtUtc,buildVersion,title,recipient,status,certificateNote,finalTasks,completedFinalGoals,totalFinalGoals,totalElapsedMs,totalAttempts,totalSuccess,successRate,questCompletionRate,outputDirectory";
        }

        private static string EnrollmentCsvHeader()
        {
            return "sessionId,issuedAtUtc,buildVersion,status,currentFloor,currentFloorTitle,completedGoals,totalGoals,globalCompletedGoals,globalTotalGoals,completedFinalGoals,totalFinalGoals,currentFinalTask,totalElapsedMs,totalAttempts,lastEventType,lastEventUtc,outputDirectory";
        }

        private static string CertificateCsvRow(CertificateLog certificate)
        {
            return string.Join(",",
                Csv(certificate.sessionId),
                Csv(certificate.issuedAtUtc),
                Csv(certificate.buildVersion),
                Csv(certificate.title),
                Csv(certificate.recipient),
                Csv(certificate.status),
                Csv(certificate.certificateNote),
                Csv(certificate.finalTasks),
                certificate.completedFinalGoals,
                certificate.totalFinalGoals,
                certificate.totalElapsedMs,
                certificate.totalAttempts,
                certificate.totalSuccess,
                Float(certificate.successRate),
                Float(certificate.questCompletionRate),
                Csv(certificate.outputDirectory));
        }

        private static string EnrollmentCsvRow(EnrollmentLog enrollment)
        {
            return string.Join(",",
                Csv(enrollment.sessionId),
                Csv(enrollment.issuedAtUtc),
                Csv(enrollment.buildVersion),
                Csv(enrollment.status),
                enrollment.currentFloor,
                Csv(enrollment.currentFloorTitle),
                enrollment.completedGoals,
                enrollment.totalGoals,
                enrollment.globalCompletedGoals,
                enrollment.globalTotalGoals,
                enrollment.completedFinalGoals,
                enrollment.totalFinalGoals,
                Csv(enrollment.currentFinalTask),
                enrollment.totalElapsedMs,
                enrollment.totalAttempts,
                Csv(enrollment.lastEventType),
                Csv(enrollment.lastEventUtc),
                Csv(enrollment.outputDirectory));
        }

        private int HighestObservedFloorId()
        {
            var max = 0;
            foreach (var attempt in attemptHistory)
            {
                max = Math.Max(max, TryParseFloorId(attempt.floorId));
            }

            foreach (var quest in questChecklistHistory)
            {
                max = Math.Max(max, TryParseFloorId(quest.floorId));
            }

            return max;
        }

        private static int TryParseFloorId(string floorId)
        {
            return int.TryParse(floorId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? Math.Max(0, parsed)
                : 0;
        }

        private static bool HasQuality(AttemptLog attempt)
        {
            return attempt.closure > 0f ||
                   attempt.smoothness > 0f ||
                   attempt.tempo > 0f ||
                   attempt.stability > 0f ||
                   attempt.rotationBias > 0f;
        }

        private static float QualityAverage(AttemptLog attempt)
        {
            return (attempt.closure + attempt.smoothness + attempt.tempo + attempt.stability + Mathf.Clamp01(1f - attempt.rotationBias)) / 5f;
        }

        private static float BuildUnderstandingDelta(IReadOnlyList<AttemptLog> attempts)
        {
            if (attempts == null || attempts.Count < 2)
            {
                return 0f;
            }

            var midpoint = Mathf.Max(1, attempts.Count / 2);
            var early = attempts.Take(midpoint).ToArray();
            var late = attempts.Skip(midpoint).ToArray();
            return Ratio(late.Count(attempt => attempt.success), late.Length) -
                   Ratio(early.Count(attempt => attempt.success), early.Length);
        }

        private static int SameTargetFailureStreakMax(IReadOnlyList<AttemptLog> attempts)
        {
            var max = 0;
            var current = 0;
            var lastKey = "";
            foreach (var attempt in attempts)
            {
                if (attempt.success)
                {
                    current = 0;
                    lastKey = "";
                    continue;
                }

                var key = AttemptTargetKey(attempt);
                current = string.Equals(key, lastKey, StringComparison.Ordinal) ? current + 1 : 1;
                lastKey = key;
                max = Math.Max(max, current);
            }

            return max;
        }

        private static string AttemptTargetKey(AttemptLog attempt)
        {
            return FirstNonEmpty(
                attempt.intentGoalId,
                attempt.targetObject,
                attempt.worldEffect,
                attempt.targetFamily,
                attempt.recognizedFamily,
                attempt.phase);
        }

        private static string WeakestQualityName(IReadOnlyList<AttemptLog> attempts)
        {
            if (attempts == null || attempts.Count == 0)
            {
                return "";
            }

            var metrics = new[]
            {
                new KeyValuePair<string, float>("closure", AverageOrZero(attempts.Select(attempt => attempt.closure))),
                new KeyValuePair<string, float>("smoothness", AverageOrZero(attempts.Select(attempt => attempt.smoothness))),
                new KeyValuePair<string, float>("tempo", AverageOrZero(attempts.Select(attempt => attempt.tempo))),
                new KeyValuePair<string, float>("stability", AverageOrZero(attempts.Select(attempt => attempt.stability))),
                new KeyValuePair<string, float>("rotation_control", AverageOrZero(attempts.Select(attempt => Mathf.Clamp01(1f - attempt.rotationBias))))
            };
            return metrics.OrderBy(metric => metric.Value).First().Key;
        }

        private static string DominantPhase(IReadOnlyList<AttemptLog> attempts)
        {
            if (attempts == null || attempts.Count == 0)
            {
                return "";
            }

            return attempts
                .GroupBy(attempt => string.IsNullOrWhiteSpace(attempt.phase) ? "unknown" : attempt.phase)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key, StringComparer.Ordinal)
                .First()
                .Key;
        }

        private static float ShapeDifficultyScore(float successRate, float hintRate, float averageQuality, int attempts, int goals)
        {
            var attemptsPerGoalPressure = Mathf.Clamp01((attempts / Mathf.Max(1f, goals) - 1f) / 3f);
            return Mathf.Clamp01((1f - successRate) * 0.42f + hintRate * 0.24f + (1f - averageQuality) * 0.22f + attemptsPerGoalPressure * 0.12f);
        }

        private static float BurdenScore(float successRate, float hintRate, float assistedSuccessRate, float timeToFirstSuccessMs, int floorDenominator)
        {
            var timePressure = Mathf.Clamp01(timeToFirstSuccessMs / Mathf.Max(1f, floorDenominator) / 90000f);
            return Mathf.Clamp01((1f - successRate) * 0.32f + hintRate * 0.28f + assistedSuccessRate * 0.18f + timePressure * 0.22f);
        }

        private static float Ratio(int numerator, int denominator)
        {
            return denominator <= 0 ? 0f : Mathf.Clamp01(numerator / (float)denominator);
        }

        private static float AverageOrZero(IEnumerable<float> values)
        {
            var list = values?.Where(value => !float.IsNaN(value) && !float.IsInfinity(value)).ToArray() ?? Array.Empty<float>();
            return list.Length == 0 ? 0f : list.Average();
        }

        private static float AveragePositive(IEnumerable<int> values)
        {
            var list = values?.Where(value => value > 0).ToArray() ?? Array.Empty<int>();
            return list.Length == 0 ? 0f : (float)list.Average();
        }

        private static string FirstNonEmpty(params string[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return "";
        }

        private static bool ShouldSuppressCollection(string sessionId)
        {
            if (sessionId.StartsWith("test-", StringComparison.OrdinalIgnoreCase) ||
                sessionId.StartsWith("test_", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var explicitDisable = Environment.GetEnvironmentVariable("MAGIC_EXAM_HALL_DISABLE_LOG_COLLECTION");
            if (string.Equals(explicitDisable, "1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(explicitDisable, "true", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var args = Environment.GetCommandLineArgs();
            foreach (var arg in args)
            {
                if (string.Equals(arg, "-runTests", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(arg, "-testResults", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            var stack = Environment.StackTrace;
            if (stack.IndexOf("NUnit.Framework", StringComparison.OrdinalIgnoreCase) >= 0 ||
                stack.IndexOf("UnityEngine.TestTools", StringComparison.OrdinalIgnoreCase) >= 0 ||
                stack.IndexOf("UnityEditor.TestTools", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return IsNUnitTestContextActive();
        }

        private static bool IsNUnitTestContextActive()
        {
            try
            {
                var contextType = Type.GetType("NUnit.Framework.TestContext, nunit.framework");
                var currentContext = contextType?.GetProperty("CurrentContext", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                var test = currentContext?.GetType().GetProperty("Test", BindingFlags.Public | BindingFlags.Instance)?.GetValue(currentContext);
                var id = test?.GetType().GetProperty("ID", BindingFlags.Public | BindingFlags.Instance)?.GetValue(test)?.ToString();
                var name = test?.GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.Instance)?.GetValue(test)?.ToString();
                var fullName = test?.GetType().GetProperty("FullName", BindingFlags.Public | BindingFlags.Instance)?.GetValue(test)?.ToString();
                var hasMeaningfulName = !string.IsNullOrWhiteSpace(name) &&
                    !string.Equals(name, "Unknown", StringComparison.OrdinalIgnoreCase);
                return !string.IsNullOrWhiteSpace(id) &&
                    (hasMeaningfulName || !string.IsNullOrWhiteSpace(fullName));
            }
            catch
            {
                return false;
            }
        }

        private static string Csv(string value)
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string Float(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string Bool(bool value)
        {
            return value ? "true" : "false";
        }
    }
}
