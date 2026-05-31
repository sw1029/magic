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
        public string stage = "none";
        public TutorialDynamicDecision decision;
        public string reason = "";
        public bool promotedByPersonalization;
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

    public sealed class TutorialPersonalizationStore
    {
        private const int MaxCaptures = 36;
        private readonly List<TutorialCaptureRecord> captures = new();

        public IReadOnlyList<TutorialCaptureRecord> Captures => captures;
        public int CaptureCount => captures.Count;

        public int CountBaseCaptures(SpellFamily family)
        {
            return captures.Count(capture => capture.kind == TutorialCaptureKind.BaseFamily && capture.family == family);
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
            return EvaluateAgainstCaptures(strokes, result.confidence, targetCaptures, result.status);
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
            IReadOnlyList<IReadOnlyList<StrokeSample>> strokes)
        {
            var family = result.recognizedFamily ?? result.targetFamily;
            var summary = EvaluateBase(family, strokes, result);
            result.personalization = summary;
            result.confidence = summary.adjustedConfidence;

            if (result.status == RecognitionStatus.Ambiguous && summary.decision == TutorialDynamicDecision.Accept)
            {
                result.status = RecognitionStatus.Recognized;
                result.recognizedFamily = family;
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
            RecognitionStatus status)
        {
            var threshold = CalculateThresholdState();
            threshold.targetSampleCount = targetCaptures.Count;
            threshold.targetMaturity = Round(Clamp(targetCaptures.Count / 3f, 0f, 1f));
            threshold.targetAcceptThreshold = Round(Clamp(
                threshold.acceptThreshold - threshold.targetMaturity * 0.03f,
                0.56f,
                0.86f));

            if (targetCaptures.Count == 0)
            {
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
            var thresholdBias = Clamp(threshold.globalScoreLift + targetMaturity * 0.03f, 0f, 0.12f);
            var adjustedConfidence = Clamp(
                baselineConfidence * 0.72f + localModelScore * 0.28f + Mathf.Min(targetCaptures.Count, 4) * 0.012f,
                0f,
                1f);
            var acceptThreshold = threshold.targetAcceptThreshold;
            var holdThreshold = threshold.holdThreshold;
            var decision = adjustedConfidence >= acceptThreshold && localModelScore >= 0.78f
                ? TutorialDynamicDecision.Accept
                : adjustedConfidence >= holdThreshold
                    ? TutorialDynamicDecision.Hold
                    : TutorialDynamicDecision.Retry;

            if (status == RecognitionStatus.Invalid || status == RecognitionStatus.Incomplete)
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
                stage = targetCaptures.Count >= 3 ? "enough_shot" : "few_shot",
                decision = decision,
                reason = $"local={localModelScore:0.000}, threshold={acceptThreshold:0.000}, captures={targetCaptures.Count}"
            };
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
            var diagonal = Mathf.Max(Vector2.Distance(min, max), 1f);
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
                corners = CountCorners(dominant, Mathf.Max(diagonal * 0.05f, 4f)),
                endpointClusters = ClusterEndpointCount(valid, Mathf.Max(diagonal * 0.08f, 0.14f)),
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

            var simplified = RdpSimplify(points, 6f);
            var area = Mathf.Abs(PolygonArea(simplified));
            var min = new Vector2(points.Min(point => point.x), points.Min(point => point.y));
            var max = new Vector2(points.Max(point => point.x), points.Max(point => point.y));
            var boxArea = Mathf.Max((max.x - min.x) * (max.y - min.y), 1f);
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
