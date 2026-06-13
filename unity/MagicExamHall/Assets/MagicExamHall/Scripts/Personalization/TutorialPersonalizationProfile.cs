using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MagicExamHall
{
    public enum TutorialCaptureKind
    {
        BaseFamily,
        OverlayOperator
    }

    public enum TutorialDynamicDecision
    {
        None,
        Accept,
        Hold,
        Retry
    }

    [Serializable]
    public sealed class MagicShapeFeatureVector
    {
        public int strokeCount;
        public float closure;
        public int corners;
        public int endpointClusters;
        public float circularity;
        public float fillRatio;
        public float parallelism;
        public float rawAngleRadians;
    }

    [Serializable]
    public sealed class TutorialThresholdState
    {
        public int captureCount;
        public float globalMaturity;
        public float globalScoreLift;
        public float acceptThreshold;
        public float holdThreshold;
        public float unsafeLimit;
        public float flipLimit;
        public int targetSampleCount;
        public float targetMaturity;
        public float targetAcceptThreshold;
    }

    [Serializable]
    public sealed class TutorialPersonalizationSummary
    {
        public static readonly TutorialPersonalizationSummary Empty = new()
        {
            stage = "none",
            decision = TutorialDynamicDecision.None,
            reason = "no tutorial captures"
        };

        public int tutorialSampleCount;
        public int targetSampleCount;
        public float localModelScore;
        public float baselineConfidence;
        public float adjustedConfidence;
        public float thresholdBias;
        public float acceptThreshold;
        public float holdThreshold;
        public int repeatedCaseCount;
        public int repeatedCaseWindowCount;
        public int repeatedCaseFailureCount;
        public float repeatedCaseLift;
        public float repeatedCaseContrastPenalty;
        public string stage = "none";
        public TutorialDynamicDecision decision;
        public string reason = "";
        public bool promotedByPersonalization;
        public bool acceleratedByRepeatedCase;
        public bool extremeColdStartCorrection;
    }

    public sealed class TutorialCaptureRecord
    {
        public string id = "";
        public TutorialCaptureKind kind;
        public SpellFamily? family;
        public OverlayOperator? overlayOperator;
        public List<List<StrokeSample>> strokes = new();
        public List<Vector2> normalizedCloud = new();
        public MagicShapeFeatureVector features = new();
        public QualityVector quality;
        public float baselineScore;
        public float savedAt;
    }

    public sealed class TutorialColdStartCaseRecord
    {
        public SpellFamily family;
        public RecognitionStatus status;
        public SpellFamily? recognizedFamily;
        public List<Vector2> normalizedCloud = new();
        public MagicShapeFeatureVector features = new();
        public float baselineScore;
        public bool targetMatched;
        public bool failedForTarget;
        public float savedAt;
    }

    public sealed class TutorialPersonalizationStore
    {
        private const int MaxCaptures = 36;
        private const int MaxColdStartCases = 48;
        private const int RepeatedColdStartCaseThreshold = 3;
        private const int RepeatedColdStartWindowSize = 5;
        private const int RepeatedColdStartWindowMatchThreshold = 3;
        private const int RepeatedColdStartFailureRepeatThreshold = 3;
        private const int RepeatedColdStartFailureAcceptThreshold = 2;
        private const float RepeatedColdStartCloudSimilarity = 0.82f;
        private const float RepeatedColdStartFeatureSimilarity = 0.74f;
        private const float RepeatedColdStartMaximumLift = 0.08f;
        private const float RepeatedColdStartMaximumThresholdBias = 0.045f;
        private const float ExtremeColdStartMaximumLift = 0.18f;
        private const float ExtremeColdStartMaximumThresholdBias = 0.1f;
        private const float ColdStartContrastSimilarityThreshold = 0.78f;
        private const float ColdStartContrastMaximumPenalty = 0.06f;
        private readonly List<TutorialCaptureRecord> captures = new();
        private readonly List<TutorialColdStartCaseRecord> coldStartCases = new();

        public IReadOnlyList<TutorialCaptureRecord> Captures => captures;
        public int CaptureCount => captures.Count;
        public int ColdStartAttemptCount => coldStartCases.Count;

        public int CountBaseCaptures(SpellFamily family)
        {
            return captures.Count(capture => capture.kind == TutorialCaptureKind.BaseFamily && capture.family == family);
        }

        public void RecordColdStartAttempt(
            SpellFamily family,
            IReadOnlyList<IReadOnlyList<StrokeSample>> strokes,
            SpellResult result,
            float savedAt)
        {
            if (result == null ||
                CountBaseCaptures(family) >= 3 ||
                Sanitize(strokes).Count == 0)
            {
                return;
            }

            var targetMatched = IsColdStartTargetMatch(result.status, result.recognizedFamily, family);
            AppendColdStartCase(new TutorialColdStartCaseRecord
            {
                family = family,
                status = result.status,
                recognizedFamily = result.recognizedFamily,
                normalizedCloud = NormalizeStrokes(strokes).cloud,
                features = DeriveShapeFeatures(strokes),
                baselineScore = result.confidence,
                targetMatched = targetMatched,
                failedForTarget = !targetMatched,
                savedAt = savedAt
            });
        }

        public TutorialThresholdState CalculateThresholdState()
        {
            var captureCount = captures.Count;
            var globalMaturity = Clamp(captureCount / 12f, 0f, 1f);
            var scores = captures.Select(capture => capture.baselineScore).ToList();
            var averageScore = scores.Count == 0 ? 0f : scores.Average();
            var globalScoreLift = Clamp(globalMaturity * 0.08f + Mathf.Max(0f, averageScore - 0.64f) * 0.16f, 0f, 0.12f);
            var acceptThreshold = Clamp(0.76f - globalScoreLift, 0.58f, 0.82f);

            return new TutorialThresholdState
            {
                captureCount = captureCount,
                globalMaturity = Round(globalMaturity),
                globalScoreLift = Round(globalScoreLift),
                acceptThreshold = Round(acceptThreshold),
                holdThreshold = Round(Clamp(acceptThreshold - 0.13f, 0.45f, 0.7f)),
                unsafeLimit = Round(Clamp(0.24f + globalMaturity * 0.09f, 0.2f, 0.36f)),
                flipLimit = Round(Clamp(0.42f + globalMaturity * 0.08f, 0.36f, 0.56f))
            };
        }

        public TutorialPersonalizationSummary EvaluateBase(
            SpellFamily family,
            IReadOnlyList<IReadOnlyList<StrokeSample>> strokes,
            SpellResult result)
        {
            var targetCaptures = captures
                .Where(capture => capture.kind == TutorialCaptureKind.BaseFamily && capture.family == family)
                .ToList();
            return EvaluateAgainstCaptures(strokes, result.confidence, targetCaptures, result.status, family, result.recognizedFamily);
        }

        public TutorialPersonalizationSummary EvaluateOverlay(
            OverlayOperator op,
            IReadOnlyList<IReadOnlyList<StrokeSample>> strokes,
            OverlayRecognitionResult result)
        {
            var targetCaptures = captures
                .Where(capture => capture.kind == TutorialCaptureKind.OverlayOperator && capture.overlayOperator == op)
                .ToList();
            return EvaluateAgainstCaptures(strokes, result.score, targetCaptures, result.status);
        }

        public SpellResult ApplyBasePersonalization(
            SpellResult result,
            IReadOnlyList<IReadOnlyList<StrokeSample>> strokes,
            SpellFamily? personalizationTargetFamily = null)
        {
            var family = personalizationTargetFamily ?? result.recognizedFamily ?? result.targetFamily;
            var summary = EvaluateBase(family, strokes, result);
            result.personalization = summary;
            result.confidence = summary.adjustedConfidence;

            var canPromote = result.status == RecognitionStatus.Ambiguous ||
                summary.extremeColdStartCorrection ||
                summary.acceleratedByRepeatedCase &&
                result.status == RecognitionStatus.Recognized &&
                result.recognizedFamily != family;
            if (canPromote && summary.decision == TutorialDynamicDecision.Accept)
            {
                result.status = RecognitionStatus.Recognized;
                result.recognizedFamily = family;
                if (personalizationTargetFamily.HasValue)
                {
                    result.targetFamily = family;
                }

                result.success = result.targetFamily == family;
                result.feedbackReason = $"{result.feedbackReason} 이전 tutorial capture와도 안정적으로 맞아 개인화 기준에서 보강되었습니다.";
                summary.promotedByPersonalization = true;
            }

            return result;
        }

        public OverlayRecognitionResult ApplyOverlayPersonalization(
            OverlayRecognitionResult result,
            IReadOnlyList<IReadOnlyList<StrokeSample>> strokes)
        {
            if (!result.recognizedOperator.HasValue)
            {
                result.personalization = TutorialPersonalizationSummary.Empty;
                return result;
            }

            var summary = EvaluateOverlay(result.recognizedOperator.Value, strokes, result);
            result.personalization = summary;
            result.score = summary.adjustedConfidence;
            result.shapeConfidence = Mathf.Max(result.shapeConfidence, summary.localModelScore * 0.92f);

            if (result.status == RecognitionStatus.Ambiguous &&
                summary.decision == TutorialDynamicDecision.Accept &&
                result.scaleHint == OverlayScaleHint.None)
            {
                result.status = RecognitionStatus.Recognized;
                result.feedbackReason = $"{result.feedbackReason} 이전 tutorial capture와 맞아 장식 판정이 보강되었습니다.";
                summary.promotedByPersonalization = true;
            }

            return result;
        }

        public void RecordBaseCapture(
            SpellFamily family,
            IReadOnlyList<IReadOnlyList<StrokeSample>> strokes,
            SpellResult result,
            float savedAt)
        {
            if (result == null || result.status != RecognitionStatus.Recognized || result.recognizedFamily != family)
            {
                return;
            }

            Append(new TutorialCaptureRecord
            {
                id = $"base-{family}-{Guid.NewGuid():N}",
                kind = TutorialCaptureKind.BaseFamily,
                family = family,
                strokes = CloneStrokes(strokes),
                normalizedCloud = NormalizeStrokes(strokes).cloud,
                features = DeriveShapeFeatures(strokes),
                quality = result.quality,
                baselineScore = result.confidence,
                savedAt = savedAt
            });
        }

        public void RecordOverlayCapture(
            OverlayOperator op,
            IReadOnlyList<IReadOnlyList<StrokeSample>> strokes,
            OverlayRecognitionResult result,
            float savedAt)
        {
            if (result == null || !result.success)
            {
                return;
            }

            Append(new TutorialCaptureRecord
            {
                id = $"overlay-{op}-{Guid.NewGuid():N}",
                kind = TutorialCaptureKind.OverlayOperator,
                overlayOperator = op,
                strokes = CloneStrokes(strokes),
                normalizedCloud = NormalizeStrokes(strokes).cloud,
                features = DeriveShapeFeatures(strokes),
                baselineScore = result.score,
                savedAt = savedAt
            });
        }

        private TutorialPersonalizationSummary EvaluateAgainstCaptures(
            IReadOnlyList<IReadOnlyList<StrokeSample>> strokes,
            float baselineConfidence,
            IReadOnlyList<TutorialCaptureRecord> targetCaptures,
            RecognitionStatus status,
            SpellFamily? coldStartFamily = null,
            SpellFamily? coldStartRecognizedFamily = null)
        {
            var threshold = CalculateThresholdState();
            threshold.targetSampleCount = targetCaptures.Count;
            threshold.targetMaturity = Round(Clamp(targetCaptures.Count / 3f, 0f, 1f));
            threshold.targetAcceptThreshold = Round(Clamp(
                threshold.acceptThreshold - threshold.targetMaturity * 0.03f,
                0.56f,
                0.86f));
            var repeatedCase = coldStartFamily.HasValue
                ? EvaluateRepeatedColdStartCase(
                    coldStartFamily.Value,
                    strokes,
                    targetCaptures.Count,
                    status,
                    coldStartRecognizedFamily)
                : RepeatedColdStartCase.None;

            if (targetCaptures.Count == 0)
            {
                if (repeatedCase.active)
                {
                    var repeatAdjustedConfidence = Clamp(
                        baselineConfidence + repeatedCase.lift - repeatedCase.contrastPenalty,
                        0f,
                        1f);
                    var repeatAcceptThreshold = Round(Clamp(
                        threshold.targetAcceptThreshold - repeatedCase.thresholdBias + repeatedCase.contrastPenalty,
                        0.54f,
                        0.9f));
                    var repeatHoldThreshold = Round(Clamp(
                        threshold.holdThreshold - repeatedCase.thresholdBias * 0.5f + repeatedCase.contrastPenalty * 0.5f,
                        0.43f,
                        0.74f));
                    var repeatDecision = repeatedCase.extremeCorrection ||
                        repeatAdjustedConfidence >= repeatAcceptThreshold && repeatedCase.similarity >= 0.84f
                        ? TutorialDynamicDecision.Accept
                        : repeatAdjustedConfidence >= repeatHoldThreshold
                            ? TutorialDynamicDecision.Hold
                            : TutorialDynamicDecision.Retry;

                    if (!repeatedCase.extremeCorrection && (status == RecognitionStatus.Invalid || status == RecognitionStatus.Incomplete))
                    {
                        repeatDecision = repeatAdjustedConfidence >= repeatHoldThreshold ? TutorialDynamicDecision.Hold : TutorialDynamicDecision.Retry;
                    }

                    return new TutorialPersonalizationSummary
                    {
                        tutorialSampleCount = threshold.captureCount,
                        targetSampleCount = 0,
                        localModelScore = Round(repeatedCase.similarity),
                        baselineConfidence = Round(baselineConfidence),
                        adjustedConfidence = Round(repeatAdjustedConfidence),
                        thresholdBias = Round(repeatedCase.thresholdBias),
                        acceptThreshold = repeatAcceptThreshold,
                        holdThreshold = repeatHoldThreshold,
                        repeatedCaseCount = repeatedCase.count,
                        repeatedCaseWindowCount = repeatedCase.windowCount,
                        repeatedCaseFailureCount = repeatedCase.failureCount,
                        repeatedCaseLift = Round(repeatedCase.lift),
                        repeatedCaseContrastPenalty = Round(repeatedCase.contrastPenalty),
                        stage = repeatedCase.extremeCorrection ? "repeat_failure_override" : "repeat_cold_start",
                        decision = repeatDecision,
                        reason = $"repeat={repeatedCase.count}, window={repeatedCase.windowCount}/{RepeatedColdStartWindowSize}, failures={repeatedCase.failureCount}, similarity={repeatedCase.similarity:0.000}, lift={repeatedCase.lift:0.000}, contrast={repeatedCase.contrastPenalty:0.000}",
                        acceleratedByRepeatedCase = true,
                        extremeColdStartCorrection = repeatedCase.extremeCorrection
                    };
                }

                return new TutorialPersonalizationSummary
                {
                    tutorialSampleCount = threshold.captureCount,
                    targetSampleCount = 0,
                    localModelScore = 0f,
                    baselineConfidence = baselineConfidence,
                    adjustedConfidence = baselineConfidence,
                    thresholdBias = 0f,
                    acceptThreshold = threshold.targetAcceptThreshold,
                    holdThreshold = threshold.holdThreshold,
                    stage = "none",
                    decision = TutorialDynamicDecision.None,
                    reason = "no matching tutorial capture"
                };
            }

            var features = DeriveShapeFeatures(strokes);
            var normalized = NormalizeStrokes(strokes).cloud;
            var cloudScores = targetCaptures
                .Select(capture => Clamp(1f - PointCloudDistance(normalized, capture.normalizedCloud) / 0.72f, 0f, 1f))
                .OrderByDescending(score => score)
                .Take(3)
                .ToList();
            var featureScores = targetCaptures
                .Select(capture => ScoreFeatureSimilarity(features, capture.features))
                .OrderByDescending(score => score)
                .Take(3)
                .ToList();
            var localModelScore = Clamp(Average(cloudScores) * 0.72f + Average(featureScores) * 0.28f, 0f, 1f);
            var targetMaturity = Clamp(targetCaptures.Count / 3f, 0f, 1f);
            var thresholdBias = Clamp(
                threshold.globalScoreLift + targetMaturity * 0.03f + repeatedCase.thresholdBias,
                0f,
                0.16f);
            var adjustedConfidence = Clamp(
                baselineConfidence * 0.72f +
                localModelScore * 0.28f +
                Mathf.Min(targetCaptures.Count, 4) * 0.012f +
                repeatedCase.lift -
                repeatedCase.contrastPenalty,
                0f,
                1f);
            var acceptThreshold = Round(Clamp(
                threshold.targetAcceptThreshold - repeatedCase.thresholdBias + repeatedCase.contrastPenalty,
                0.54f,
                0.9f));
            var holdThreshold = Round(Clamp(
                threshold.holdThreshold - repeatedCase.thresholdBias * 0.5f + repeatedCase.contrastPenalty * 0.5f,
                0.43f,
                0.74f));
            var decision = repeatedCase.extremeCorrection ||
                adjustedConfidence >= acceptThreshold && localModelScore >= 0.78f
                ? TutorialDynamicDecision.Accept
                : adjustedConfidence >= holdThreshold
                    ? TutorialDynamicDecision.Hold
                    : TutorialDynamicDecision.Retry;

            if (!repeatedCase.extremeCorrection && (status == RecognitionStatus.Invalid || status == RecognitionStatus.Incomplete))
            {
                decision = adjustedConfidence >= holdThreshold ? TutorialDynamicDecision.Hold : TutorialDynamicDecision.Retry;
            }

            return new TutorialPersonalizationSummary
            {
                tutorialSampleCount = threshold.captureCount,
                targetSampleCount = targetCaptures.Count,
                localModelScore = Round(localModelScore),
                baselineConfidence = Round(baselineConfidence),
                adjustedConfidence = Round(adjustedConfidence),
                thresholdBias = Round(thresholdBias),
                acceptThreshold = acceptThreshold,
                holdThreshold = holdThreshold,
                repeatedCaseCount = repeatedCase.count,
                repeatedCaseWindowCount = repeatedCase.windowCount,
                repeatedCaseFailureCount = repeatedCase.failureCount,
                repeatedCaseLift = Round(repeatedCase.lift),
                repeatedCaseContrastPenalty = Round(repeatedCase.contrastPenalty),
                stage = repeatedCase.extremeCorrection
                    ? "repeat_failure_override"
                    : repeatedCase.active
                    ? targetCaptures.Count >= 3 ? "repeat_accelerated" : "repeat_few_shot"
                    : repeatedCase.contrastPenalty > 0f ? "contrast_adjusted"
                    : targetCaptures.Count >= 3 ? "enough_shot" : "few_shot",
                decision = decision,
                reason = repeatedCase.active || repeatedCase.contrastPenalty > 0f
                    ? $"local={localModelScore:0.000}, threshold={acceptThreshold:0.000}, captures={targetCaptures.Count}, repeat={repeatedCase.count}, window={repeatedCase.windowCount}/{RepeatedColdStartWindowSize}, failures={repeatedCase.failureCount}, lift={repeatedCase.lift:0.000}, contrast={repeatedCase.contrastPenalty:0.000}"
                    : $"local={localModelScore:0.000}, threshold={acceptThreshold:0.000}, captures={targetCaptures.Count}",
                acceleratedByRepeatedCase = repeatedCase.active,
                extremeColdStartCorrection = repeatedCase.extremeCorrection
            };
        }

        private RepeatedColdStartCase EvaluateRepeatedColdStartCase(
            SpellFamily family,
            IReadOnlyList<IReadOnlyList<StrokeSample>> strokes,
            int targetCaptureCount,
            RecognitionStatus currentStatus,
            SpellFamily? currentRecognizedFamily)
        {
            if (targetCaptureCount >= 3)
            {
                return RepeatedColdStartCase.None;
            }

            var valid = Sanitize(strokes);
            if (valid.Count == 0)
            {
                return RepeatedColdStartCase.None;
            }

            var normalized = NormalizeStrokes(valid).cloud;
            var features = DeriveShapeFeatures(valid);
            var matches = coldStartCases
                .Where(item => item.family == family)
                .Select(item => ScoreColdStartCase(normalized, features, item))
                .Where(IsRepeatedColdStartMatch)
                .OrderByDescending(match => match.score)
                .ToList();
            var recentWindowMatches = coldStartCases
                .Where(item => item.family == family)
                .OrderByDescending(item => item.savedAt)
                .Take(Mathf.Max(RepeatedColdStartWindowSize - 1, 1))
                .Select(item => ScoreColdStartCase(normalized, features, item))
                .Where(IsRepeatedColdStartMatch)
                .ToList();
            var count = matches.Count + 1;
            var windowCount = Mathf.Min(recentWindowMatches.Count + 1, RepeatedColdStartWindowSize);
            var currentFailed = !IsColdStartTargetMatch(currentStatus, currentRecognizedFamily, family);
            var failureCount = matches.Count(match => match.record.failedForTarget) + (currentFailed ? 1 : 0);
            var similarity = Average(matches
                .OrderByDescending(match => match.score)
                .Take(3)
                .Select(match => match.score)
                .ToList());
            var contrastScore = coldStartCases
                .Where(item => item.family != family)
                .Select(item => ScoreColdStartCase(normalized, features, item).score)
                .DefaultIfEmpty(0f)
                .Max();
            var contrastPenalty = CalculateContrastPenalty(similarity, contrastScore);
            var active = count >= RepeatedColdStartCaseThreshold ||
                windowCount >= RepeatedColdStartWindowMatchThreshold;
            var extremeCorrection = active &&
                count >= RepeatedColdStartFailureRepeatThreshold &&
                failureCount >= RepeatedColdStartFailureAcceptThreshold;

            if (!active)
            {
                return new RepeatedColdStartCase(
                    false,
                    count,
                    windowCount,
                    failureCount,
                    similarity,
                    0f,
                    0f,
                    contrastScore,
                    contrastPenalty,
                    false);
            }

            var excessRepeats = Mathf.Min(Mathf.Max(count, windowCount) - RepeatedColdStartCaseThreshold, 3);
            var liftBase = extremeCorrection ? 0.09f : 0.025f;
            var lift = Clamp(
                liftBase + excessRepeats * 0.018f + Mathf.Max(0f, similarity - 0.82f) * 0.14f,
                0f,
                extremeCorrection ? ExtremeColdStartMaximumLift : RepeatedColdStartMaximumLift);
            var thresholdBiasBase = extremeCorrection ? 0.055f : 0.018f;
            var thresholdBias = Clamp(
                thresholdBiasBase + excessRepeats * 0.012f,
                0f,
                extremeCorrection ? ExtremeColdStartMaximumThresholdBias : RepeatedColdStartMaximumThresholdBias);
            return new RepeatedColdStartCase(
                true,
                count,
                windowCount,
                failureCount,
                similarity,
                lift,
                thresholdBias,
                contrastScore,
                contrastPenalty,
                extremeCorrection);
        }

        private static ColdStartCaseSimilarity ScoreColdStartCase(
            IReadOnlyList<Vector2> normalized,
            MagicShapeFeatureVector features,
            TutorialColdStartCaseRecord record)
        {
            var cloud = Clamp(1f - PointCloudDistance(normalized, record.normalizedCloud) / 0.72f, 0f, 1f);
            var feature = ScoreFeatureSimilarity(features, record.features);
            return new ColdStartCaseSimilarity(record, cloud, feature, Clamp(cloud * 0.72f + feature * 0.28f, 0f, 1f));
        }

        private static bool IsRepeatedColdStartMatch(ColdStartCaseSimilarity similarity)
        {
            return similarity.cloud >= RepeatedColdStartCloudSimilarity &&
                similarity.feature >= RepeatedColdStartFeatureSimilarity;
        }

        private static bool IsColdStartTargetMatch(
            RecognitionStatus status,
            SpellFamily? recognizedFamily,
            SpellFamily targetFamily)
        {
            return status == RecognitionStatus.Recognized && recognizedFamily == targetFamily;
        }

        private static float CalculateContrastPenalty(float targetSimilarity, float contrastScore)
        {
            if (contrastScore < ColdStartContrastSimilarityThreshold)
            {
                return 0f;
            }

            var contrastLead = contrastScore - Mathf.Max(targetSimilarity, 0.65f);
            return Clamp((contrastLead + 0.08f) * 0.5f, 0f, ColdStartContrastMaximumPenalty);
        }

        private void AppendColdStartCase(TutorialColdStartCaseRecord attempt)
        {
            coldStartCases.Add(attempt);
            while (coldStartCases.Count > MaxColdStartCases)
            {
                coldStartCases.RemoveAt(0);
            }
        }

        private void Append(TutorialCaptureRecord capture)
        {
            captures.RemoveAll(existing => existing.id == capture.id);
            captures.Add(capture);
            while (captures.Count > MaxCaptures)
            {
                captures.RemoveAt(0);
            }
        }

        public static MagicShapeFeatureVector DeriveShapeFeatures(IReadOnlyList<IReadOnlyList<StrokeSample>> strokes)
        {
            var valid = Sanitize(strokes);
            if (valid.Count == 0)
            {
                return new MagicShapeFeatureVector();
            }

            var normalized = NormalizeStrokes(valid);
            var dominant = valid.OrderByDescending(PathLength).First();
            var allPoints = valid.SelectMany(stroke => stroke).Select(sample => sample.position).ToList();
            var min = new Vector2(allPoints.Min(point => point.x), allPoints.Min(point => point.y));
            var max = new Vector2(allPoints.Max(point => point.x), allPoints.Max(point => point.y));
            var diagonal = Mathf.Max(Vector2.Distance(min, max), 0.15f);
            var first = dominant[0].position;
            var last = dominant[^1].position;
            var closureGap = Vector2.Distance(first, last);
            var radii = normalized.cloud.Select(point => point.magnitude).ToList();
            var meanRadius = Average(radii);
            var variance = Average(radii.Select(radius => Mathf.Pow(radius - meanRadius, 2f)).ToList());

            return new MagicShapeFeatureVector
            {
                strokeCount = valid.Count,
                closure = Clamp(1f - closureGap / (diagonal * 0.32f), 0f, 1f),
                corners = CountCorners(dominant, Mathf.Max(diagonal * 0.05f, 0.012f)),
                endpointClusters = ClusterEndpointCount(valid, Mathf.Max(diagonal * 0.08f, 0.035f)),
                circularity = Clamp(1f - Mathf.Sqrt(variance) / Mathf.Max(meanRadius, 0.0001f) / 0.45f, 0f, 1f),
                fillRatio = CalculateFillRatio(dominant),
                parallelism = CalculateParallelism(valid),
                rawAngleRadians = NormalizeHalfPi(normalized.rawAngleRadians)
            };
        }

        private static float ScoreFeatureSimilarity(MagicShapeFeatureVector left, MagicShapeFeatureVector right)
        {
            return Average(new List<float>
            {
                Closeness(left.strokeCount, right.strokeCount, 2f),
                Closeness(left.closure, right.closure, 0.32f),
                Closeness(left.corners, right.corners, 4f),
                Closeness(left.endpointClusters, right.endpointClusters, 3f),
                Closeness(left.circularity, right.circularity, 0.38f),
                Closeness(left.fillRatio, right.fillRatio, 0.28f),
                Closeness(left.parallelism, right.parallelism, 0.42f),
                Closeness(Mathf.Abs(left.rawAngleRadians), Mathf.Abs(right.rawAngleRadians), Mathf.PI / 5f)
            });
        }

        private static NormalizedStrokeBundle NormalizeStrokes(IReadOnlyList<IReadOnlyList<StrokeSample>> strokes)
        {
            var valid = Sanitize(strokes);
            if (valid.Count == 0)
            {
                return new NormalizedStrokeBundle(new List<List<Vector2>>(), new List<Vector2>(), 0f);
            }

            var totalSamples = 96;
            var lengths = valid.Select(PathLength).ToList();
            var totalLength = Mathf.Max(lengths.Sum(), 0.001f);
            var normalizedStrokes = new List<List<Vector2>>();
            var rawCloud = new List<Vector2>();

            for (var index = 0; index < valid.Count; index++)
            {
                var sampleCount = Mathf.Max(10, Mathf.RoundToInt(lengths[index] / totalLength * totalSamples));
                var resampled = Resample(valid[index].Select(sample => sample.position).ToList(), sampleCount);
                rawCloud.AddRange(resampled);
            }

            var centroid = rawCloud.Count == 0
                ? Vector2.zero
                : new Vector2(rawCloud.Average(point => point.x), rawCloud.Average(point => point.y));
            var rawAngle = PrincipalAxisAngle(rawCloud);
            var rotated = rawCloud.Select(point => Rotate(point - centroid, -rawAngle)).ToList();
            var min = new Vector2(rotated.Min(point => point.x), rotated.Min(point => point.y));
            var max = new Vector2(rotated.Max(point => point.x), rotated.Max(point => point.y));
            var scale = Mathf.Max(max.x - min.x, max.y - min.y, 0.001f);
            var cloud = rotated.Select(point => point / scale).ToList();

            normalizedStrokes.Add(cloud);
            return new NormalizedStrokeBundle(normalizedStrokes, cloud, rawAngle);
        }

        private static List<List<StrokeSample>> Sanitize(IReadOnlyList<IReadOnlyList<StrokeSample>> strokes)
        {
            return strokes
                .Select(stroke => stroke.Where(sample => float.IsFinite(sample.position.x) && float.IsFinite(sample.position.y)).ToList())
                .Where(stroke => stroke.Count >= 2)
                .ToList();
        }

        private static List<List<StrokeSample>> CloneStrokes(IReadOnlyList<IReadOnlyList<StrokeSample>> strokes)
        {
            return Sanitize(strokes)
                .Select(stroke => stroke.Select(sample => new StrokeSample(sample.position, sample.time)).ToList())
                .ToList();
        }

        private static List<Vector2> Resample(IReadOnlyList<Vector2> points, int count)
        {
            if (points.Count == 0)
            {
                return new List<Vector2>();
            }

            if (points.Count == 1)
            {
                return Enumerable.Repeat(points[0], count).ToList();
            }

            var total = PathLength(points);
            var interval = total / Mathf.Max(count - 1, 1);
            var output = new List<Vector2> { points[0] };
            var distanceSinceLast = 0f;
            var previous = points[0];

            for (var index = 1; index < points.Count; index++)
            {
                var current = points[index];
                var segment = Vector2.Distance(previous, current);
                if (segment <= 0.0001f)
                {
                    continue;
                }

                while (distanceSinceLast + segment >= interval && output.Count < count)
                {
                    var t = (interval - distanceSinceLast) / segment;
                    var inserted = Vector2.Lerp(previous, current, t);
                    output.Add(inserted);
                    segment -= interval - distanceSinceLast;
                    previous = inserted;
                    distanceSinceLast = 0f;
                }

                distanceSinceLast += segment;
                previous = current;
            }

            while (output.Count < count)
            {
                output.Add(points[^1]);
            }

            return output;
        }

        private static float PointCloudDistance(IReadOnlyList<Vector2> left, IReadOnlyList<Vector2> right)
        {
            if (left.Count == 0 || right.Count == 0)
            {
                return 1f;
            }

            return (AverageNearestNeighborDistance(left, right) + AverageNearestNeighborDistance(right, left)) * 0.5f;
        }

        private static float AverageNearestNeighborDistance(IReadOnlyList<Vector2> left, IReadOnlyList<Vector2> right)
        {
            return left.Average(point => right.Min(candidate => Vector2.Distance(point, candidate)));
        }

        private static int CountCorners(IReadOnlyList<StrokeSample> stroke, float epsilon)
        {
            var points = stroke.Select(sample => sample.position).ToList();
            if (points.Count < 2)
            {
                return 0;
            }

            return Mathf.Max(RdpSimplify(points, epsilon).Count - 1, 0);
        }

        private static List<Vector2> RdpSimplify(IReadOnlyList<Vector2> points, float epsilon)
        {
            if (points.Count <= 2)
            {
                return points.ToList();
            }

            var maxDistance = 0f;
            var splitIndex = 0;
            for (var index = 1; index < points.Count - 1; index++)
            {
                var current = DistanceToSegment(points[index], points[0], points[^1]);
                if (current > maxDistance)
                {
                    maxDistance = current;
                    splitIndex = index;
                }
            }

            if (maxDistance <= epsilon)
            {
                return new List<Vector2> { points[0], points[^1] };
            }

            var left = RdpSimplify(points.Take(splitIndex + 1).ToList(), epsilon);
            var right = RdpSimplify(points.Skip(splitIndex).ToList(), epsilon);
            return left.Take(left.Count - 1).Concat(right).ToList();
        }

        private static int ClusterEndpointCount(IReadOnlyList<List<StrokeSample>> strokes, float threshold)
        {
            var endpoints = strokes
                .Where(stroke => stroke.Count > 0)
                .SelectMany(stroke => new[] { stroke[0].position, stroke[^1].position })
                .ToList();
            var clusters = new List<Vector2>();

            foreach (var endpoint in endpoints)
            {
                var index = clusters.FindIndex(cluster => Vector2.Distance(cluster, endpoint) <= threshold);
                if (index >= 0)
                {
                    clusters[index] = (clusters[index] + endpoint) * 0.5f;
                }
                else
                {
                    clusters.Add(endpoint);
                }
            }

            return clusters.Count;
        }

        private static float CalculateFillRatio(IReadOnlyList<StrokeSample> stroke)
        {
            var points = stroke.Select(sample => sample.position).ToList();
            if (points.Count < 3)
            {
                return 0f;
            }

            var min = new Vector2(points.Min(point => point.x), points.Min(point => point.y));
            var max = new Vector2(points.Max(point => point.x), points.Max(point => point.y));
            var diagonal = Mathf.Max(Vector2.Distance(min, max), 0.15f);
            var simplified = RdpSimplify(points, Mathf.Max(diagonal * 0.035f, 0.008f));
            var area = Mathf.Abs(PolygonArea(simplified));
            var boxArea = Mathf.Max((max.x - min.x) * (max.y - min.y), 0.0001f);
            return Clamp(area / boxArea, 0f, 1f);
        }

        private static float CalculateParallelism(IReadOnlyList<List<StrokeSample>> strokes)
        {
            var lines = strokes
                .Where(stroke => stroke.Count >= 2)
                .Select(stroke =>
                {
                    var points = stroke.Select(sample => sample.position).ToList();
                    return new
                    {
                        straightness = Vector2.Distance(points[0], points[^1]) / Mathf.Max(PathLength(points), 0.001f),
                        angle = NormalizeHalfPi(Mathf.Atan2(points[^1].y - points[0].y, points[^1].x - points[0].x))
                    };
                })
                .ToList();

            if (lines.Count == 0)
            {
                return 0f;
            }

            var x = lines.Sum(line => Mathf.Cos(line.angle * 2f));
            var y = lines.Sum(line => Mathf.Sin(line.angle * 2f));
            var averageAngle = Mathf.Atan2(y, x) * 0.5f;
            var meanDeviation = lines.Average(line => Mathf.Abs(NormalizeHalfPi(line.angle - averageAngle)));
            var angleScore = Clamp(1f - meanDeviation / (Mathf.PI / 6f), 0f, 1f);
            var straightnessScore = Clamp(lines.Average(line => line.straightness), 0f, 1f);
            return angleScore * 0.6f + straightnessScore * 0.4f;
        }

        private static float PrincipalAxisAngle(IReadOnlyList<Vector2> points)
        {
            if (points.Count < 2)
            {
                return 0f;
            }

            var center = new Vector2(points.Average(point => point.x), points.Average(point => point.y));
            var xx = 0f;
            var yy = 0f;
            var xy = 0f;

            foreach (var point in points)
            {
                var delta = point - center;
                xx += delta.x * delta.x;
                yy += delta.y * delta.y;
                xy += delta.x * delta.y;
            }

            return NormalizeHalfPi(0.5f * Mathf.Atan2(2f * xy, xx - yy));
        }

        private static Vector2 Rotate(Vector2 point, float angle)
        {
            var cosine = Mathf.Cos(angle);
            var sine = Mathf.Sin(angle);
            return new Vector2(point.x * cosine - point.y * sine, point.x * sine + point.y * cosine);
        }

        private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            var segment = end - start;
            if (segment.sqrMagnitude <= 0.0001f)
            {
                return Vector2.Distance(point, start);
            }

            var projection = Vector2.Dot(point - start, segment) / segment.sqrMagnitude;
            var clamped = Mathf.Clamp01(projection);
            return Vector2.Distance(point, start + segment * clamped);
        }

        private static float PolygonArea(IReadOnlyList<Vector2> points)
        {
            var sum = 0f;
            for (var index = 0; index < points.Count; index++)
            {
                var current = points[index];
                var next = points[(index + 1) % points.Count];
                sum += current.x * next.y - next.x * current.y;
            }

            return sum * 0.5f;
        }

        private static float PathLength(IReadOnlyList<StrokeSample> stroke)
        {
            return PathLength(stroke.Select(sample => sample.position).ToList());
        }

        private static float PathLength(IReadOnlyList<Vector2> points)
        {
            var total = 0f;
            for (var index = 1; index < points.Count; index++)
            {
                total += Vector2.Distance(points[index - 1], points[index]);
            }

            return total;
        }

        private static float NormalizeHalfPi(float angle)
        {
            while (angle > Mathf.PI / 2f)
            {
                angle -= Mathf.PI;
            }

            while (angle < -Mathf.PI / 2f)
            {
                angle += Mathf.PI;
            }

            return angle;
        }

        private static float Closeness(float actual, float expected, float tolerance)
        {
            return Clamp(1f - Mathf.Abs(actual - expected) / Mathf.Max(tolerance, 0.0001f), 0f, 1f);
        }

        private static float Average(IReadOnlyList<float> values)
        {
            return values.Count == 0 ? 0f : values.Average();
        }

        private static float Clamp(float value, float min, float max)
        {
            return Mathf.Clamp(value, min, max);
        }

        private static float Round(float value)
        {
            return Mathf.Round(value * 10000f) / 10000f;
        }

        private readonly struct RepeatedColdStartCase
        {
            public static readonly RepeatedColdStartCase None = new(false, 1, 1, 0, 0f, 0f, 0f, 0f, 0f, false);

            public readonly bool active;
            public readonly int count;
            public readonly int windowCount;
            public readonly int failureCount;
            public readonly float similarity;
            public readonly float lift;
            public readonly float thresholdBias;
            public readonly float contrastScore;
            public readonly float contrastPenalty;
            public readonly bool extremeCorrection;

            public RepeatedColdStartCase(
                bool active,
                int count,
                int windowCount,
                int failureCount,
                float similarity,
                float lift,
                float thresholdBias,
                float contrastScore,
                float contrastPenalty,
                bool extremeCorrection)
            {
                this.active = active;
                this.count = count;
                this.windowCount = windowCount;
                this.failureCount = failureCount;
                this.similarity = similarity;
                this.lift = lift;
                this.thresholdBias = thresholdBias;
                this.contrastScore = contrastScore;
                this.contrastPenalty = contrastPenalty;
                this.extremeCorrection = extremeCorrection;
            }
        }

        private readonly struct ColdStartCaseSimilarity
        {
            public readonly TutorialColdStartCaseRecord record;
            public readonly float cloud;
            public readonly float feature;
            public readonly float score;

            public ColdStartCaseSimilarity(TutorialColdStartCaseRecord record, float cloud, float feature, float score)
            {
                this.record = record;
                this.cloud = cloud;
                this.feature = feature;
                this.score = score;
            }
        }

        private readonly struct NormalizedStrokeBundle
        {
            public readonly List<List<Vector2>> strokes;
            public readonly List<Vector2> cloud;
            public readonly float rawAngleRadians;

            public NormalizedStrokeBundle(List<List<Vector2>> strokes, List<Vector2> cloud, float rawAngleRadians)
            {
                this.strokes = strokes;
                this.cloud = cloud;
                this.rawAngleRadians = rawAngleRadians;
            }
        }
    }
}
