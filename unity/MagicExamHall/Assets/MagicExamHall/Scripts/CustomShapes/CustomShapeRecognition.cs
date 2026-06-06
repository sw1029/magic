using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MagicExamHall
{
    public sealed class CustomShapeRecognitionResult
    {
        public CustomShapeSlot slot = null!;
        public bool accepted;
        public bool held;
        public bool defaultConflict;
        public SpellFamily defaultFamily;
        public float customScore;
        public float goldScore;
        public float autoScore;
        public float featureScore;
        public float defaultSimilarityScore;
        public float acceptThreshold;
        public float holdThreshold;
        public TutorialPersonalizationSummary summary = TutorialPersonalizationSummary.Empty;
    }

    public static class CustomShapeRecognition
    {
        private const int CloudPointCount = 64;

        public static CustomShapeRecognitionResult Recognize(
            IReadOnlyList<IReadOnlyList<StrokeSample>> strokes,
            BaseRecognitionResult baseResult,
            CustomShapeProfileStore store,
            SpellFamily? preferredMappedFamily = null)
        {
            if (store == null || baseResult?.spell == null)
            {
                return null;
            }

            var drawable = Sanitize(strokes);
            if (drawable.Count == 0)
            {
                return null;
            }

            var normalized = Normalize(drawable);
            var features = ShapeFeatures.From(drawable, normalized);
            var defaultFamily = baseResult.spell.recognizedFamily ?? baseResult.spell.targetFamily;
            var defaultSimilarity = Mathf.Clamp01(baseResult.spell.confidence);
            var best = store.Slots
                .Where(slot => slot.IsOccupied)
                .Select(slot => ScoreSlot(slot, drawable, normalized.cloud, features, defaultFamily, defaultSimilarity, preferredMappedFamily))
                .Where(result => result.customScore >= result.holdThreshold)
                .OrderByDescending(result => result.accepted ? 2 : result.held ? 1 : 0)
                .ThenByDescending(result => preferredMappedFamily.HasValue && result.slot.mappedFamily == preferredMappedFamily.Value ? 1 : 0)
                .ThenByDescending(result => result.customScore)
                .FirstOrDefault();

            return best;
        }

        public static bool ApplyToBaseResult(
            BaseRecognitionResult baseResult,
            IReadOnlyList<IReadOnlyList<StrokeSample>> strokes,
            CustomShapeProfileStore store,
            SpellFamily? preferredMappedFamily = null)
        {
            var result = Recognize(strokes, baseResult, store, preferredMappedFamily);
            if (result == null || baseResult?.spell == null)
            {
                return false;
            }

            var spell = baseResult.spell;

            if (!result.accepted)
            {
                if (!result.defaultConflict && spell.status == RecognitionStatus.Recognized)
                {
                    return false;
                }

                ApplyCustomMetadata(spell, result, strokes);
                spell.status = result.defaultConflict ? RecognitionStatus.Incomplete : RecognitionStatus.Ambiguous;
                spell.recognizedFamily = null;
                spell.targetFamily = result.slot.mappedFamily;
                spell.success = false;
                spell.confidence = Mathf.Max(spell.confidence, result.customScore);
                spell.feedbackReason = result.defaultConflict
                    ? $"{result.slot.label} 도형은 보였지만 {SpellLabels.Korean(result.defaultFamily)} 기본 도형과 너무 가까워 보류했습니다."
                    : $"{result.slot.label} 커스텀 도형이 아직 안정 기준에 조금 부족합니다.";
                spell.nextHint = "같은 슬롯의 gold capture와 비슷한 크기와 획 순서로 한 번 더 그려 보세요.";
                return true;
            }

            ApplyCustomMetadata(spell, result, strokes);
            spell.status = RecognitionStatus.Recognized;
            spell.recognizedFamily = result.slot.mappedFamily;
            spell.targetFamily = result.slot.mappedFamily;
            spell.mappedFamily = result.slot.mappedFamily;
            spell.success = true;
            spell.confidence = Mathf.Clamp01(Mathf.Max(spell.confidence, result.customScore));
            spell.feedbackReason =
                $"{result.slot.label} 커스텀 도형이 {SpellLabels.Korean(result.slot.mappedFamily)} 효과로 안정화되었습니다.";
            spell.nextHint =
                $"기본 유사도 {result.defaultSimilarityScore:0.00}, custom {result.customScore:0.00}. 이후 입력은 이 슬롯 보정에 반영됩니다.";
            return true;
        }

        private static void ApplyCustomMetadata(
            SpellResult spell,
            CustomShapeRecognitionResult result,
            IReadOnlyList<IReadOnlyList<StrokeSample>> strokes)
        {
            spell.isCustomShape = true;
            spell.customShapeId = result.slot.shapeId;
            spell.customShapeLabel = result.slot.label;
            spell.customShapeToken = result.slot.shapeToken;
            spell.mappedFamily = result.slot.mappedFamily;
            spell.customScore = Round(result.customScore);
            spell.defaultSimilarityScore = Round(result.defaultSimilarityScore);
            spell.personalization = result.summary;
            var customEvent = CustomShapeEventCatalog.BuildPayload(result.slot.eventShapeTokens, strokes);
            spell.customEventId = customEvent.eventId;
            spell.customEventLabel = customEvent.displayName;
            spell.customEventKind = customEvent.eventKind.ToString();
            spell.customEventRole = customEvent.role.ToString();
            spell.customEventUsesDirection = customEvent.usesDirection;
            spell.customEventOperatorOnly = customEvent.operatorOnly;
            spell.customEventBlocks = customEvent.blocksEvent;
            spell.customEventBlocked = customEvent.eventBlocked;
            spell.customEventBlockedBy = customEvent.blockedByToken;
            spell.customEventOrigin = customEvent.origin;
            spell.customEventDirection = customEvent.direction;
            spell.customEventStartPoint = customEvent.startPoint;
            spell.customEventEndPoint = customEvent.endPoint;
        }

        private static CustomShapeRecognitionResult ScoreSlot(
            CustomShapeSlot slot,
            IReadOnlyList<IReadOnlyList<StrokeSample>> strokes,
            IReadOnlyList<Vector2> cloud,
            ShapeFeatures features,
            SpellFamily defaultFamily,
            float defaultSimilarity,
            SpellFamily? preferredMappedFamily)
        {
            var goldScores = slot.goldCaptures
                .Select(capture => ScoreCapture(capture, cloud, features))
                .Where(score => score > 0f)
                .ToList();
            var autoScores = slot.autoCaptures
                .Select(capture => ScoreCapture(capture, cloud, features))
                .Where(score => score > 0f)
                .OrderByDescending(score => score)
                .Take(3)
                .ToList();
            var goldScore = goldScores.Count == 0 ? 0f : goldScores.Max();
            var autoScore = autoScores.Count == 0 ? 0f : autoScores.Average();
            var featureScore = ScoreFeatureSimilarity(features, AverageCaptureFeatures(slot));
            var maturity = Mathf.Clamp01((slot.goldCaptures.Count + slot.autoCaptures.Count) / 6f);
            var customScore = Mathf.Clamp01(goldScore * 0.58f + featureScore * 0.22f + autoScore * 0.15f + maturity * 0.05f);
            var preferredSlot = preferredMappedFamily.HasValue && slot.mappedFamily == preferredMappedFamily.Value;
            var acceptThreshold = Mathf.Clamp(0.73f - Mathf.Min(slot.autoCaptures.Count, 8) * 0.01f, 0.64f, 0.78f);
            if (preferredSlot)
            {
                acceptThreshold = Mathf.Clamp(acceptThreshold - 0.07f, 0.62f, 0.78f);
            }

            var holdThreshold = Mathf.Clamp(acceptThreshold - 0.13f, 0.48f, 0.68f);
            var conflict = !preferredSlot &&
                           defaultSimilarity >= 0.78f &&
                           defaultFamily != slot.mappedFamily &&
                           customScore < defaultSimilarity + 0.08f;
            var accepted = customScore >= acceptThreshold && !conflict;
            var held = !accepted && (customScore >= holdThreshold || conflict);

            return new CustomShapeRecognitionResult
            {
                slot = slot,
                accepted = accepted,
                held = held,
                defaultConflict = conflict,
                defaultFamily = defaultFamily,
                customScore = Round(customScore),
                goldScore = Round(goldScore),
                autoScore = Round(autoScore),
                featureScore = Round(featureScore),
                defaultSimilarityScore = Round(defaultSimilarity),
                acceptThreshold = Round(acceptThreshold),
                holdThreshold = Round(holdThreshold),
                summary = new TutorialPersonalizationSummary
                {
                    tutorialSampleCount = slot.goldCaptures.Count + slot.autoCaptures.Count,
                    targetSampleCount = slot.autoCaptures.Count,
                    localModelScore = Round(customScore),
                    baselineConfidence = Round(defaultSimilarity),
                    adjustedConfidence = Round(customScore),
                    thresholdBias = Round(Mathf.Clamp01(0.73f - acceptThreshold)),
                    acceptThreshold = Round(acceptThreshold),
                    holdThreshold = Round(holdThreshold),
                    stage = slot.autoCaptures.Count >= 3 ? "custom_adapted" : "custom_gold",
                    decision = accepted ? TutorialDynamicDecision.Accept : held ? TutorialDynamicDecision.Hold : TutorialDynamicDecision.Retry,
                    reason = conflict
                        ? $"shadow conflict with {defaultFamily}"
                        : $"gold={goldScore:0.000}, auto={autoScore:0.000}, feature={featureScore:0.000}"
                }
            };
        }

        private static float ScoreCapture(CustomShapeCaptureRecord capture, IReadOnlyList<Vector2> cloud, ShapeFeatures features)
        {
            var strokes = capture.ToStrokeSamples();
            if (strokes.Count == 0)
            {
                return 0f;
            }

            var normalized = Normalize(strokes);
            var orderedDistance = Mathf.Min(
                PointCloudDistance(cloud, normalized.cloud),
                PointCloudDistance(cloud, normalized.cloud, reverseRight: true));
            var orderedCloudScore = Mathf.Clamp01(1f - orderedDistance / 0.72f);
            var unorderedDistance = SymmetricNearestPointDistance(cloud, normalized.cloud);
            var unorderedCloudScore = Mathf.Clamp01(1f - unorderedDistance / 0.42f);
            var cloudScore = Mathf.Max(orderedCloudScore, unorderedCloudScore * 0.96f);
            var featureScore = ScoreFeatureSimilarity(features, ShapeFeatures.From(strokes, normalized));
            return Mathf.Clamp01(cloudScore * 0.74f + featureScore * 0.26f);
        }

        private static ShapeFeatures AverageCaptureFeatures(CustomShapeSlot slot)
        {
            var values = slot.AllCaptures()
                .Select(capture => capture.ToStrokeSamples())
                .Where(strokes => strokes.Count > 0)
                .Select(strokes =>
                {
                    var normalized = Normalize(strokes);
                    return ShapeFeatures.From(strokes, normalized);
                })
                .ToList();

            if (values.Count == 0)
            {
                return new ShapeFeatures();
            }

            return new ShapeFeatures
            {
                strokeCount = Mathf.RoundToInt((float)values.Average(value => value.strokeCount)),
                closure = values.Average(value => value.closure),
                corners = Mathf.RoundToInt((float)values.Average(value => value.corners)),
                endpointClusters = Mathf.RoundToInt((float)values.Average(value => value.endpointClusters)),
                circularity = values.Average(value => value.circularity),
                fillRatio = values.Average(value => value.fillRatio),
                parallelism = values.Average(value => value.parallelism),
                rawAngleRadians = values.Average(value => value.rawAngleRadians)
            };
        }

        private static float ScoreFeatureSimilarity(ShapeFeatures left, ShapeFeatures right)
        {
            return Mathf.Clamp01(
                Close(left.strokeCount, right.strokeCount, 3f) * 0.16f +
                Close(left.closure, right.closure, 0.45f) * 0.15f +
                Close(left.corners, right.corners, 5f) * 0.14f +
                Close(left.endpointClusters, right.endpointClusters, 5f) * 0.10f +
                Close(left.circularity, right.circularity, 0.45f) * 0.13f +
                Close(left.fillRatio, right.fillRatio, 0.45f) * 0.12f +
                Close(left.parallelism, right.parallelism, 0.55f) * 0.12f +
                Close(Mathf.Abs(NormalizeHalfPi(left.rawAngleRadians - right.rawAngleRadians)), 0f, Mathf.PI / 4f) * 0.08f);
        }

        private static float Close(float actual, float expected, float tolerance)
        {
            return Mathf.Clamp01(1f - Mathf.Abs(actual - expected) / Mathf.Max(tolerance, 0.0001f));
        }

        private static List<List<StrokeSample>> Sanitize(IReadOnlyList<IReadOnlyList<StrokeSample>> source)
        {
            return (source ?? Array.Empty<IReadOnlyList<StrokeSample>>())
                .Select(stroke => stroke
                    .Where(sample => float.IsFinite(sample.position.x) && float.IsFinite(sample.position.y))
                    .Select(sample => new StrokeSample(sample.position, sample.time))
                    .ToList())
                .Where(stroke => stroke.Count >= 2)
                .ToList();
        }

        private static NormalizedShape Normalize(IReadOnlyList<IReadOnlyList<StrokeSample>> strokes)
        {
            var points = strokes.SelectMany(stroke => stroke.Select(sample => sample.position)).ToList();
            if (points.Count == 0)
            {
                return new NormalizedShape(new List<Vector2>(), 0f);
            }

            var center = new Vector2(points.Average(point => point.x), points.Average(point => point.y));
            var min = new Vector2(points.Min(point => point.x), points.Min(point => point.y));
            var max = new Vector2(points.Max(point => point.x), points.Max(point => point.y));
            var scale = Mathf.Max(max.x - min.x, max.y - min.y, 0.001f);
            var rawAngle = PrincipalAxisAngle(points);
            var resampled = strokes
                .SelectMany(stroke => Resample(stroke.Select(sample => sample.position).ToList(), Mathf.Max(4, CloudPointCount / strokes.Count)))
                .Select(point => Rotate((point - center) / scale, -rawAngle))
                .Take(CloudPointCount)
                .ToList();

            while (resampled.Count < CloudPointCount && resampled.Count > 0)
            {
                resampled.Add(resampled[^1]);
            }

            return new NormalizedShape(resampled, rawAngle);
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

            var totalLength = PathLength(points);
            if (totalLength <= 0.0001f)
            {
                return Enumerable.Repeat(points[0], count).ToList();
            }

            var output = new List<Vector2> { points[0] };
            var targetStep = totalLength / Mathf.Max(count - 1, 1);
            var accumulated = 0f;
            var nextDistance = targetStep;

            for (var index = 1; index < points.Count; index++)
            {
                var previous = points[index - 1];
                var current = points[index];
                var segment = Vector2.Distance(previous, current);
                while (segment > 0f && accumulated + segment >= nextDistance && output.Count < count)
                {
                    var ratio = (nextDistance - accumulated) / segment;
                    output.Add(Vector2.Lerp(previous, current, ratio));
                    nextDistance += targetStep;
                }

                accumulated += segment;
            }

            while (output.Count < count)
            {
                output.Add(points[^1]);
            }

            return output;
        }

        private static float PointCloudDistance(IReadOnlyList<Vector2> left, IReadOnlyList<Vector2> right, bool reverseRight = false)
        {
            if (left.Count == 0 || right.Count == 0)
            {
                return 1f;
            }

            var count = Mathf.Min(left.Count, right.Count);
            var total = 0f;
            for (var index = 0; index < count; index++)
            {
                var rightIndex = reverseRight ? right.Count - 1 - index : index;
                total += Vector2.Distance(left[index], right[rightIndex]);
            }

            return total / count;
        }

        private static float SymmetricNearestPointDistance(IReadOnlyList<Vector2> left, IReadOnlyList<Vector2> right)
        {
            if (left.Count == 0 || right.Count == 0)
            {
                return 1f;
            }

            return (AverageNearestPointDistance(left, right) + AverageNearestPointDistance(right, left)) * 0.5f;
        }

        private static float AverageNearestPointDistance(IReadOnlyList<Vector2> source, IReadOnlyList<Vector2> target)
        {
            var total = 0f;
            for (var sourceIndex = 0; sourceIndex < source.Count; sourceIndex++)
            {
                var nearestSqr = float.MaxValue;
                for (var targetIndex = 0; targetIndex < target.Count; targetIndex++)
                {
                    nearestSqr = Mathf.Min(nearestSqr, (source[sourceIndex] - target[targetIndex]).sqrMagnitude);
                }

                total += Mathf.Sqrt(nearestSqr);
            }

            return total / source.Count;
        }

        private static int CountCorners(IReadOnlyList<StrokeSample> stroke)
        {
            if (stroke.Count < 4)
            {
                return 0;
            }

            var corners = 0;
            for (var index = 1; index < stroke.Count - 1; index++)
            {
                var before = (stroke[index].position - stroke[index - 1].position).normalized;
                var after = (stroke[index + 1].position - stroke[index].position).normalized;
                if (Vector2.Angle(before, after) > 38f)
                {
                    corners++;
                }
            }

            return corners;
        }

        private static int ClusterEndpointCount(IReadOnlyList<IReadOnlyList<StrokeSample>> strokes)
        {
            var endpoints = strokes
                .Where(stroke => stroke.Count > 0)
                .SelectMany(stroke => new[] { stroke[0].position, stroke[^1].position })
                .ToList();
            if (endpoints.Count == 0)
            {
                return 0;
            }

            var min = new Vector2(endpoints.Min(point => point.x), endpoints.Min(point => point.y));
            var max = new Vector2(endpoints.Max(point => point.x), endpoints.Max(point => point.y));
            var threshold = Mathf.Max(Vector2.Distance(min, max) * 0.14f, 0.08f);
            var clusters = new List<Vector2>();
            foreach (var endpoint in endpoints)
            {
                if (clusters.All(cluster => Vector2.Distance(cluster, endpoint) > threshold))
                {
                    clusters.Add(endpoint);
                }
            }

            return clusters.Count;
        }

        private static float EstimateCircularity(IReadOnlyList<Vector2> cloud)
        {
            if (cloud.Count == 0)
            {
                return 0f;
            }

            var center = new Vector2(cloud.Average(point => point.x), cloud.Average(point => point.y));
            var distances = cloud.Select(point => Vector2.Distance(point, center)).ToList();
            var mean = Mathf.Max(distances.Average(), 0.0001f);
            var variance = distances.Average(distance => (distance - mean) * (distance - mean));
            return Mathf.Clamp01(1f - Mathf.Sqrt(variance) / mean / 0.45f);
        }

        private static float EstimateFillRatio(IReadOnlyList<IReadOnlyList<StrokeSample>> strokes)
        {
            var points = strokes.SelectMany(stroke => stroke.Select(sample => sample.position)).ToList();
            if (points.Count < 3)
            {
                return 0f;
            }

            var min = new Vector2(points.Min(point => point.x), points.Min(point => point.y));
            var max = new Vector2(points.Max(point => point.x), points.Max(point => point.y));
            var boxArea = Mathf.Max((max.x - min.x) * (max.y - min.y), 0.0001f);
            return Mathf.Clamp01(PolygonArea(points) / boxArea);
        }

        private static float EstimateParallelism(IReadOnlyList<IReadOnlyList<StrokeSample>> strokes)
        {
            var lines = strokes
                .Where(stroke => stroke.Count >= 2)
                .Select(stroke => NormalizeHalfPi(Mathf.Atan2(
                    stroke[^1].position.y - stroke[0].position.y,
                    stroke[^1].position.x - stroke[0].position.x)))
                .ToList();
            if (lines.Count < 2)
            {
                return 0f;
            }

            var average = lines.Average();
            var deviation = lines.Average(angle => Mathf.Abs(NormalizeHalfPi(angle - average)));
            return Mathf.Clamp01(1f - deviation / (Mathf.PI / 5f));
        }

        private static float PrincipalAxisAngle(IReadOnlyList<Vector2> points)
        {
            var center = new Vector2(points.Average(point => point.x), points.Average(point => point.y));
            var xx = 0f;
            var yy = 0f;
            var xy = 0f;
            foreach (var point in points)
            {
                var dx = point.x - center.x;
                var dy = point.y - center.y;
                xx += dx * dx;
                yy += dy * dy;
                xy += dx * dy;
            }

            return NormalizeHalfPi(0.5f * Mathf.Atan2(2f * xy, xx - yy));
        }

        private static Vector2 Rotate(Vector2 point, float angle)
        {
            var cos = Mathf.Cos(angle);
            var sin = Mathf.Sin(angle);
            return new Vector2(point.x * cos - point.y * sin, point.x * sin + point.y * cos);
        }

        private static float PathLength(IReadOnlyList<Vector2> points)
        {
            var length = 0f;
            for (var index = 1; index < points.Count; index++)
            {
                length += Vector2.Distance(points[index - 1], points[index]);
            }

            return length;
        }

        private static float PolygonArea(IReadOnlyList<Vector2> points)
        {
            var area = 0f;
            for (var index = 0; index < points.Count; index++)
            {
                var next = (index + 1) % points.Count;
                area += points[index].x * points[next].y - points[next].x * points[index].y;
            }

            return Mathf.Abs(area) * 0.5f;
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

        private static float Round(float value)
        {
            return Mathf.Round(value * 1000f) / 1000f;
        }

        private readonly struct NormalizedShape
        {
            public readonly List<Vector2> cloud;
            public readonly float rawAngleRadians;

            public NormalizedShape(List<Vector2> cloud, float rawAngleRadians)
            {
                this.cloud = cloud;
                this.rawAngleRadians = rawAngleRadians;
            }
        }

        private struct ShapeFeatures
        {
            public int strokeCount;
            public float closure;
            public int corners;
            public int endpointClusters;
            public float circularity;
            public float fillRatio;
            public float parallelism;
            public float rawAngleRadians;

            public static ShapeFeatures From(IReadOnlyList<IReadOnlyList<StrokeSample>> strokes, NormalizedShape normalized)
            {
                var points = strokes.SelectMany(stroke => stroke.Select(sample => sample.position)).ToList();
                var closure = 0f;
                if (points.Count >= 2)
                {
                    var min = new Vector2(points.Min(point => point.x), points.Min(point => point.y));
                    var max = new Vector2(points.Max(point => point.x), points.Max(point => point.y));
                    var diagonal = Mathf.Max(Vector2.Distance(min, max), 0.0001f);
                    closure = Mathf.Clamp01(1f - Vector2.Distance(points[0], points[^1]) / (diagonal * 0.32f));
                }

                return new ShapeFeatures
                {
                    strokeCount = strokes.Count,
                    closure = closure,
                    corners = strokes.Sum(CountCorners),
                    endpointClusters = ClusterEndpointCount(strokes),
                    circularity = EstimateCircularity(normalized.cloud),
                    fillRatio = EstimateFillRatio(strokes),
                    parallelism = EstimateParallelism(strokes),
                    rawAngleRadians = normalized.rawAngleRadians
                };
            }
        }
    }
}
