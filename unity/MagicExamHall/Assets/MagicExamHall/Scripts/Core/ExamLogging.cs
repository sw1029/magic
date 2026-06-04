using System;
using System.Globalization;
using System.IO;
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

    public sealed class ExamLogger
    {
        public string OutputDirectory { get; }
        private readonly string attemptsJsonPath;
        private readonly string attemptsCsvPath;
        private readonly string surveyJsonPath;
        private readonly string surveyCsvPath;
        private readonly string questChecklistJsonPath;
        private readonly string questChecklistCsvPath;

        public ExamLogger(string sessionId)
        {
            OutputDirectory = Path.Combine(Application.persistentDataPath, "MagicExamHallLogs", sessionId);
            Directory.CreateDirectory(OutputDirectory);
            attemptsJsonPath = Path.Combine(OutputDirectory, "attempts.jsonl");
            attemptsCsvPath = Path.Combine(OutputDirectory, "attempts.csv");
            surveyJsonPath = Path.Combine(OutputDirectory, "survey.jsonl");
            surveyCsvPath = Path.Combine(OutputDirectory, "survey.csv");
            questChecklistJsonPath = Path.Combine(OutputDirectory, "quest-checklist.jsonl");
            questChecklistCsvPath = Path.Combine(OutputDirectory, "quest-checklist.csv");
            EnsureAttemptHeader();
            EnsureSurveyHeader();
            EnsureQuestChecklistHeader();
        }

        public void LogAttempt(AttemptLog log)
        {
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
