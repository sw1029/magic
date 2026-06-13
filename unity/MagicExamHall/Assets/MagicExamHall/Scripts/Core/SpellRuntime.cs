using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MagicExamHall
{
    public enum SpellPhase
    {
        Base,
        Overlay,
        Final
    }

    public enum OverlayOperator
    {
        SteelBrace,
        ElectricFork,
        IceBar,
        SoulDot,
        VoidCut,
        MartialAxis
    }

    public enum OverlayScaleHint
    {
        None,
        TooSmall,
        TooLarge
    }

    [Serializable]
    public sealed class OverlayRecognitionResult
    {
        public RecognitionStatus status;
        public OverlayOperator? recognizedOperator;
        public float score;
        public float shapeConfidence;
        public float scaleRatio;
        public OverlayScaleHint scaleHint;
        public string anchorZone = "";
        public string feedbackReason = "";
        public TutorialPersonalizationSummary personalization = TutorialPersonalizationSummary.Empty;
        public bool success => status == RecognitionStatus.Recognized && recognizedOperator.HasValue;

        public string OperatorText => recognizedOperator.HasValue ? SpellLabels.English(recognizedOperator.Value) : "none";
    }

    [Serializable]
    public sealed class CompiledSeal
    {
        public string sealId = "";
        public SpellFamily baseFamily;
        public readonly List<OverlayOperator> overlayStack = new();
        public QualityVector quality;
        public Vector2 worldCenter;
        public float worldScale = 1f;
        public float createdAt;
        public float expiresAt;
        public string customShapeId = "";
        public string customShapeLabel = "";
        public string customShapeToken = "";
        public string customEventId = "";
        public string customEventLabel = "";
        public string customEventKind = "";
        public string customEventRole = "";
        public Vector2 customEventOrigin;
        public Vector2 customEventDirection = Vector2.right;
        public CustomSpellEffectKind customEffectKind = CustomSpellEffectKind.None;
        public bool HasCustomEffectSeal => customEffectKind != CustomSpellEffectKind.None;

        public string Label
        {
            get
            {
                var customEffectLabel = HasCustomEffectSeal
                    ? CustomSpellEffectCatalog.Korean(customEffectKind)
                    : "";
                var baseLabel = !string.IsNullOrWhiteSpace(customEffectLabel)
                    ? $"{customEffectLabel} seal"
                    : string.IsNullOrWhiteSpace(customShapeLabel)
                    ? SpellLabels.Korean(baseFamily)
                    : $"{customShapeLabel} ({SpellLabels.Korean(baseFamily)})";
                if (overlayStack.Count == 0)
                {
                    return baseLabel;
                }

                return $"{baseLabel} + {string.Join(" + ", overlayStack.Select(SpellLabels.Korean))}";
            }
        }
    }

    public sealed class BaseRecognitionResult
    {
        public SpellResult spell = null!;
        public Vector2 center;
        public float worldScale = 1f;
        public int bufferStrokeCount;
        public BaseRecognitionIntent intent;
    }

    public static class SpellRuntime
    {
        public const float DefaultSealDurationSeconds = 11f;

        public static BaseRecognitionResult RecognizeBase(IReadOnlyList<IReadOnlyList<StrokeSample>> strokes)
        {
            return RecognizeBase(strokes, null);
        }

        public static BaseRecognitionResult RecognizeBase(
            IReadOnlyList<IReadOnlyList<StrokeSample>> strokes,
            BaseRecognitionIntent intent)
        {
            var candidates = Enum.GetValues(typeof(SpellFamily))
                .Cast<SpellFamily>()
                .Select(family => GestureRecognizer.Recognize(strokes, family, intent))
                .OrderByDescending(result => result.intentStrongConsiderationApplied ? 3 : result.success ? 2 : result.status == RecognitionStatus.Recognized ? 1 : 0)
                .ThenByDescending(result => result.confidence)
                .ToList();
            var best = candidates.First();
            var allPoints = strokes.SelectMany(stroke => stroke).Select(sample => sample.position).ToList();

            return new BaseRecognitionResult
            {
                spell = best,
                center = allPoints.Count == 0 ? Vector2.zero : new Vector2(allPoints.Average(point => point.x), allPoints.Average(point => point.y)),
                worldScale = EstimateWorldScale(allPoints),
                bufferStrokeCount = strokes.Count,
                intent = intent
            };
        }

        public static CompiledSeal CreateSeal(BaseRecognitionResult baseResult, float now, float durationSeconds = DefaultSealDurationSeconds)
        {
            return new CompiledSeal
            {
                sealId = Guid.NewGuid().ToString("N")[..8],
                baseFamily = baseResult.spell.recognizedFamily ?? baseResult.spell.targetFamily,
                quality = baseResult.spell.quality,
                worldCenter = baseResult.center,
                worldScale = Mathf.Max(baseResult.worldScale, 0.65f),
                createdAt = now,
                expiresAt = now + durationSeconds,
                customShapeId = baseResult.spell.customShapeId ?? "",
                customShapeLabel = baseResult.spell.customShapeLabel ?? "",
                customShapeToken = baseResult.spell.customShapeToken ?? "",
                customEventId = baseResult.spell.customEventId ?? "",
                customEventLabel = baseResult.spell.customEventLabel ?? "",
                customEventKind = baseResult.spell.customEventKind ?? "",
                customEventRole = baseResult.spell.customEventRole ?? "",
                customEventOrigin = baseResult.spell.customEventOrigin,
                customEventDirection = baseResult.spell.customEventDirection
            };
        }

        private static float EstimateWorldScale(IReadOnlyList<Vector2> points)
        {
            if (points.Count == 0)
            {
                return 1f;
            }

            var min = new Vector2(points.Min(point => point.x), points.Min(point => point.y));
            var max = new Vector2(points.Max(point => point.x), points.Max(point => point.y));
            return Mathf.Max(Vector2.Distance(min, max), 0.5f);
        }
    }

    public static class OverlayRecognizer
    {
        private const int CloudPointCount = 48;
        private static readonly IReadOnlyList<OverlayTemplate> Templates = BuildTemplates();

        public static OverlayRecognitionResult Recognize(
            IReadOnlyList<IReadOnlyList<StrokeSample>> rawStrokes,
            CompiledSeal seal)
        {
            var strokes = rawStrokes
                .Select(stroke => stroke.Where(sample => IsFinite(sample.position)).ToList())
                .Where(stroke => stroke.Count >= 2)
                .ToList();

            if (strokes.Count == 0)
            {
                return new OverlayRecognitionResult
                {
                    status = RecognitionStatus.Invalid,
                    feedbackReason = "overlay stroke가 충분히 남지 않았습니다."
                };
            }

            var features = OverlayFeatures.From(strokes, seal);
            var normalized = Normalize(strokes.Select(stroke => stroke.Select(sample => sample.position).ToList()).ToList());
            var scored = Templates
                .Select(template => ScoreTemplate(template, normalized.cloud, features, seal))
                .OrderByDescending(candidate => candidate.score)
                .ToList();
            var top = scored[0];
            var second = scored.Count > 1 ? scored[1] : top;
            var margin = top.score - second.score;
            var scaleHint = ScaleHintFor(features.scaleRatio, top);

            if (top.op == OverlayOperator.MartialAxis && !seal.overlayStack.Contains(OverlayOperator.VoidCut))
            {
                return new OverlayRecognitionResult
                {
                    status = top.score >= 0.52f ? RecognitionStatus.Incomplete : RecognitionStatus.Invalid,
                    recognizedOperator = top.op,
                    score = top.score,
                    shapeConfidence = top.shapeConfidence,
                    scaleRatio = features.scaleRatio,
                    scaleHint = scaleHint,
                    anchorZone = top.anchorZone,
                    feedbackReason = "축 장식은 먼저 절단 장식이 붙은 seal에서만 섭니다. 대각선 절단을 붙인 뒤 중심을 가르는 축을 그리세요."
                };
            }

            if (top.shapeConfidence >= 0.74f && ScaleIsFarOutside(features.scaleRatio, top))
            {
                return new OverlayRecognitionResult
                {
                    status = RecognitionStatus.Incomplete,
                    recognizedOperator = top.op,
                    score = top.score,
                    shapeConfidence = top.shapeConfidence,
                    scaleRatio = features.scaleRatio,
                    scaleHint = scaleHint,
                    anchorZone = top.anchorZone,
                    feedbackReason = BuildOverlayFailureReason(top, scaleHint, ambiguous: false)
                };
            }

            if (!OverlaySpecificGate(top.op, strokes.Count, features))
            {
                return new OverlayRecognitionResult
                {
                    status = RecognitionStatus.Invalid,
                    recognizedOperator = top.op,
                    score = top.score,
                    shapeConfidence = top.shapeConfidence,
                    scaleRatio = features.scaleRatio,
                    scaleHint = scaleHint,
                    anchorZone = top.anchorZone,
                    feedbackReason = BuildOverlayGateReason(top.op)
                };
            }

            var strongShapeMatch = top.score >= 0.72f && top.shapeConfidence >= 0.74f;
            if (top.score >= 0.68f && top.shapeConfidence >= 0.48f && (margin >= 0.02f || strongShapeMatch))
            {
                return new OverlayRecognitionResult
                {
                    status = RecognitionStatus.Recognized,
                    recognizedOperator = top.op,
                    score = top.score,
                    shapeConfidence = top.shapeConfidence,
                    scaleRatio = features.scaleRatio,
                    scaleHint = scaleHint,
                    anchorZone = top.anchorZone,
                    feedbackReason = $"{SpellLabels.Korean(top.op)} 장식이 seal에 붙었습니다."
                };
            }

            if (top.score >= 0.5f)
            {
                return new OverlayRecognitionResult
                {
                    status = RecognitionStatus.Ambiguous,
                    score = top.score,
                    shapeConfidence = top.shapeConfidence,
                    scaleRatio = features.scaleRatio,
                    scaleHint = scaleHint,
                    anchorZone = top.anchorZone,
                    feedbackReason = BuildOverlayFailureReason(top, scaleHint, ambiguous: true)
                };
            }

            return new OverlayRecognitionResult
            {
                status = RecognitionStatus.Invalid,
                score = top.score,
                shapeConfidence = top.shapeConfidence,
                scaleRatio = features.scaleRatio,
                scaleHint = scaleHint,
                anchorZone = top.anchorZone,
                feedbackReason = BuildOverlayFailureReason(top, scaleHint, ambiguous: false)
            };
        }

        public static List<List<StrokeSample>> CreateCanonicalSamples(
            OverlayOperator op,
            Vector2 center,
            float scale = 1.2f,
            float timeStep = 0.04f)
        {
            var template = Templates.First(item => item.op == op);
            var output = new List<List<StrokeSample>>();
            var time = 0f;

            foreach (var stroke in template.strokes)
            {
                var mapped = new List<StrokeSample>();
                foreach (var point in stroke)
                {
                    mapped.Add(new StrokeSample(center + point * scale, time));
                    time += timeStep;
                }
                output.Add(mapped);
                time += timeStep * 3f;
            }

            return output;
        }

        private static OverlayScore ScoreTemplate(
            OverlayTemplate template,
            IReadOnlyList<Vector2> cloud,
            OverlayFeatures features,
            CompiledSeal seal)
        {
            var templateDistance = PointCloudDistance(cloud, template.normalizedCloud);
            var templateScore = Mathf.Clamp01(1f - templateDistance / 0.72f);
            var anchorScore = AnchorScore(features.centroid, seal.worldCenter, seal.worldScale, template.preferredAnchor);
            var scaleScore = RangeScore(features.scaleRatio, template.minScale, template.maxScale);
            var openScore = 1f - features.closure;
            var score = templateScore;
            var shape = templateScore;

            switch (template.op)
            {
                case OverlayOperator.SteelBrace:
                {
                    var corner = Closeness(features.corners, 3f, 1.8f);
                    score = templateScore * 0.36f + corner * 0.24f + anchorScore * 0.16f + openScore * 0.14f + scaleScore * 0.10f;
                    shape = templateScore * 0.46f + corner * 0.34f + openScore * 0.20f;
                    break;
                }
                case OverlayOperator.ElectricFork:
                {
                    var corner = Closeness(features.corners, 4f, 2f);
                    score = templateScore * 0.36f + corner * 0.28f + anchorScore * 0.16f + openScore * 0.10f + scaleScore * 0.10f;
                    shape = templateScore * 0.40f + corner * 0.40f + openScore * 0.20f;
                    break;
                }
                case OverlayOperator.IceBar:
                {
                    var horizontal = Mathf.Clamp01(1f - Mathf.Abs(features.angleRadians) / (Mathf.PI / 8f));
                    score = features.straightness * 0.36f + horizontal * 0.26f + anchorScore * 0.16f + scaleScore * 0.14f + templateScore * 0.08f;
                    shape = features.straightness * 0.52f + horizontal * 0.36f + templateScore * 0.12f;
                    break;
                }
                case OverlayOperator.SoulDot:
                {
                    var compact = Mathf.Clamp01(1f - features.scaleRatio / 0.18f);
                    score = features.circularity * 0.30f + features.closure * 0.28f + anchorScore * 0.18f + compact * 0.14f + templateScore * 0.10f;
                    shape = features.circularity * 0.44f + features.closure * 0.44f + templateScore * 0.12f;
                    break;
                }
                case OverlayOperator.VoidCut:
                {
                    var diagonal = Mathf.Clamp01(1f - Mathf.Abs(Mathf.Abs(features.angleRadians) - Mathf.PI / 4f) / (Mathf.PI / 8f));
                    score = features.straightness * 0.32f + diagonal * 0.28f + anchorScore * 0.16f + scaleScore * 0.14f + templateScore * 0.10f;
                    shape = features.straightness * 0.48f + diagonal * 0.40f + templateScore * 0.12f;
                    break;
                }
                case OverlayOperator.MartialAxis:
                {
                    var corner = Closeness(features.corners, 4f, 2f);
                    var axis = Mathf.Max(features.horizontalAxisScore, features.verticalAxisScore);
                    score = templateScore * 0.42f + corner * 0.24f + axis * 0.16f + anchorScore * 0.08f + scaleScore * 0.10f;
                    shape = templateScore * 0.36f + corner * 0.34f + axis * 0.30f;
                    break;
                }
            }

            return new OverlayScore
            {
                op = template.op,
                score = Mathf.Clamp01(score),
                shapeConfidence = Mathf.Clamp01(shape),
                anchorZone = template.preferredAnchor,
                minScale = template.minScale,
                maxScale = template.maxScale
            };
        }

        private static string BuildOverlayFailureReason(OverlayScore top, OverlayScaleHint scaleHint, bool ambiguous)
        {
            if (scaleHint == OverlayScaleHint.TooSmall)
            {
                return $"{SpellLabels.Korean(top.op)} 장식처럼 보였지만 너무 작아 seal에 고정되지 않았습니다.";
            }

            if (scaleHint == OverlayScaleHint.TooLarge)
            {
                return $"{SpellLabels.Korean(top.op)} 장식처럼 보였지만 너무 커서 seal 안쪽 기준을 벗어났습니다.";
            }

            if (top.shapeConfidence >= 0.55f)
            {
                return $"{SpellLabels.Korean(top.op)} 장식 모양은 보였지만 위치가 {AnchorLabel(top.anchorZone)} 기준과 맞지 않았습니다.";
            }

            return ambiguous
                ? "장식 후보가 겹쳐 아직 seal에 붙이지 않았습니다. 한 번에 한 가지 장식만 더 단순하게 그려 보세요."
                : "장식의 모양과 위치가 seal 기준과 충분히 맞지 않았습니다.";
        }

        private static bool OverlaySpecificGate(OverlayOperator op, int strokeCount, OverlayFeatures features)
        {
            switch (op)
            {
                case OverlayOperator.SoulDot:
                    return strokeCount == 1 &&
                        features.closure >= 0.56f &&
                        features.circularity >= 0.48f &&
                        features.scaleRatio >= 0.025f &&
                        features.scaleRatio <= 0.22f;
                case OverlayOperator.IceBar:
                case OverlayOperator.VoidCut:
                    return strokeCount == 1 && features.straightness >= 0.62f;
                case OverlayOperator.SteelBrace:
                case OverlayOperator.ElectricFork:
                case OverlayOperator.MartialAxis:
                    return strokeCount <= 2 && features.corners >= 1;
                default:
                    return true;
            }
        }

        private static string BuildOverlayGateReason(OverlayOperator op)
        {
            return op switch
            {
                OverlayOperator.SoulDot => "집중 장식은 seal 중심에 작은 원 또는 점처럼 한 번에 닫아 그려야 합니다. 두 줄만 그은 모양은 집중으로 확정하지 않습니다.",
                OverlayOperator.IceBar => "얼음 장식은 한 획의 곧은 수평선으로 그려야 합니다.",
                OverlayOperator.VoidCut => "절단 장식은 한 획의 곧은 대각선으로 그려야 합니다.",
                OverlayOperator.SteelBrace => "보강 장식은 꺾인 ㄷ자 형태가 분명해야 합니다.",
                OverlayOperator.ElectricFork => "번개 장식은 갈라지는 꺾임이 분명해야 합니다.",
                OverlayOperator.MartialAxis => "축 장식은 중심을 지나는 십자 축 형태가 분명해야 합니다.",
                _ => "장식의 핵심 형태가 충분히 맞지 않았습니다."
            };
        }

        private static bool ScaleIsFarOutside(float scaleRatio, OverlayScore top)
        {
            return scaleRatio > 0f && (scaleRatio < top.minScale * 0.55f || scaleRatio > top.maxScale * 1.35f);
        }

        private static OverlayScaleHint ScaleHintFor(float scaleRatio, OverlayScore top)
        {
            if (scaleRatio > 0f && scaleRatio < top.minScale * 0.75f)
            {
                return OverlayScaleHint.TooSmall;
            }

            if (scaleRatio > top.maxScale * 1.15f)
            {
                return OverlayScaleHint.TooLarge;
            }

            return OverlayScaleHint.None;
        }

        private static string AnchorLabel(string anchorZone)
        {
            return anchorZone switch
            {
                "upper_right" => "오른쪽 위 가장자리",
                "right" => "오른쪽 가장자리",
                "lower_right" => "오른쪽 아래 가장자리",
                "upper" => "위쪽 가장자리",
                "left" => "왼쪽 가장자리",
                _ => "중심"
            };
        }

        private static IReadOnlyList<OverlayTemplate> BuildTemplates()
        {
            var templates = new List<OverlayTemplate>
            {
                new(OverlayOperator.SteelBrace, "right", 0.14f, 0.55f, new[]
                {
                    Poly(new Vector2(0.42f, -0.72f), new Vector2(-0.4f, -0.72f), new Vector2(-0.4f, 0.72f), new Vector2(0.42f, 0.72f))
                }),
                new(OverlayOperator.ElectricFork, "upper_right", 0.12f, 0.48f, new[]
                {
                    Poly(new Vector2(-0.52f, 0.58f), new Vector2(-0.08f, 0.02f), new Vector2(-0.44f, 0.02f), new Vector2(0.06f, -0.66f), new Vector2(0.5f, 0.02f))
                }),
                new(OverlayOperator.IceBar, "core", 0.22f, 0.62f, new[]
                {
                    Line(new Vector2(-0.78f, 0f), new Vector2(0.78f, 0f), 16)
                }),
                new(OverlayOperator.SoulDot, "core", 0.03f, 0.20f, new[]
                {
                    Ellipse(0.20f, 0.20f, 24)
                }),
                new(OverlayOperator.VoidCut, "core", 0.12f, 0.54f, new[]
                {
                    Line(new Vector2(-0.68f, 0.68f), new Vector2(0.68f, -0.68f), 18)
                }),
                new(OverlayOperator.MartialAxis, "core", 0.12f, 0.50f, new[]
                {
                    Poly(new Vector2(0f, -0.74f), new Vector2(0f, 0.74f), new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(-0.5f, 0f))
                })
            };

            foreach (var template in templates)
            {
                template.normalizedCloud = Normalize(template.strokes).cloud;
            }

            return templates;
        }

        private static float AnchorScore(Vector2 point, Vector2 center, float scale, string zone)
        {
            var offset = Mathf.Max(scale * 0.28f, 0.25f);
            var target = zone switch
            {
                "upper_right" => center + new Vector2(offset, offset),
                "right" => center + new Vector2(offset, 0f),
                "lower_right" => center + new Vector2(offset, -offset),
                "upper" => center + new Vector2(0f, offset),
                "left" => center + new Vector2(-offset, 0f),
                _ => center
            };
            return Mathf.Clamp01(1f - Vector2.Distance(point, target) / Mathf.Max(scale * 0.55f, 0.35f));
        }

        private static NormalizedGesture Normalize(IReadOnlyList<IReadOnlyList<Vector2>> strokes)
        {
            var points = strokes.SelectMany(stroke => stroke).ToList();
            if (points.Count == 0)
            {
                return new NormalizedGesture(new List<Vector2>());
            }

            var min = new Vector2(points.Min(point => point.x), points.Min(point => point.y));
            var max = new Vector2(points.Max(point => point.x), points.Max(point => point.y));
            var center = (min + max) * 0.5f;
            var scale = Mathf.Max(max.x - min.x, max.y - min.y, 0.001f);
            var normalized = strokes.Select(stroke => stroke.Select(point => (point - center) / scale).ToList()).ToList();
            return new NormalizedGesture(Resample(normalized.SelectMany(stroke => stroke).ToList(), CloudPointCount));
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
            var interval = total / Mathf.Max(count - 1f, 1f);
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

            var count = Mathf.Min(left.Count, right.Count);
            var forward = 0f;
            var reverse = 0f;
            for (var index = 0; index < count; index++)
            {
                forward += Vector2.Distance(left[index], right[index]);
                reverse += Vector2.Distance(left[index], right[count - 1 - index]);
            }

            return Mathf.Min(forward, reverse) / count;
        }

        private static List<Vector2> Line(Vector2 start, Vector2 end, int count)
        {
            return Enumerable.Range(0, count)
                .Select(index => Vector2.Lerp(start, end, index / Mathf.Max(count - 1f, 1f)))
                .ToList();
        }

        private static List<Vector2> Poly(params Vector2[] anchors)
        {
            var points = new List<Vector2>();
            for (var index = 1; index < anchors.Length; index++)
            {
                var line = Line(anchors[index - 1], anchors[index], 12);
                if (points.Count > 0)
                {
                    line.RemoveAt(0);
                }

                points.AddRange(line);
            }

            return points;
        }

        private static List<Vector2> Ellipse(float radiusX, float radiusY, int count)
        {
            return Enumerable.Range(0, count + 1)
                .Select(index =>
                {
                    var angle = index / (float)count * Mathf.PI * 2f;
                    return new Vector2(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY);
                })
                .ToList();
        }

        private static float RangeScore(float value, float minimum, float maximum)
        {
            if (value >= minimum && value <= maximum)
            {
                return 1f;
            }

            var distanceToRange = value < minimum ? minimum - value : value - maximum;
            return Mathf.Clamp01(1f - distanceToRange / 0.2f);
        }

        private static float Closeness(float value, float target, float tolerance)
        {
            return Mathf.Clamp01(1f - Mathf.Abs(value - target) / Mathf.Max(tolerance, 0.001f));
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

        private static bool IsFinite(Vector2 value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y);
        }

        private sealed class OverlayTemplate
        {
            public readonly OverlayOperator op;
            public readonly string preferredAnchor;
            public readonly float minScale;
            public readonly float maxScale;
            public readonly List<List<Vector2>> strokes;
            public List<Vector2> normalizedCloud = new();

            public OverlayTemplate(OverlayOperator op, string preferredAnchor, float minScale, float maxScale, IEnumerable<List<Vector2>> strokes)
            {
                this.op = op;
                this.preferredAnchor = preferredAnchor;
                this.minScale = minScale;
                this.maxScale = maxScale;
                this.strokes = strokes.ToList();
            }
        }

        private sealed class OverlayScore
        {
            public OverlayOperator op;
            public float score;
            public float shapeConfidence;
            public string anchorZone = "";
            public float minScale;
            public float maxScale;
        }

        private readonly struct NormalizedGesture
        {
            public readonly List<Vector2> cloud;

            public NormalizedGesture(List<Vector2> cloud)
            {
                this.cloud = cloud;
            }
        }

        private readonly struct OverlayFeatures
        {
            public readonly Vector2 centroid;
            public readonly float straightness;
            public readonly int corners;
            public readonly float closure;
            public readonly float circularity;
            public readonly float angleRadians;
            public readonly float scaleRatio;
            public readonly float horizontalAxisScore;
            public readonly float verticalAxisScore;

            private OverlayFeatures(
                Vector2 centroid,
                float straightness,
                int corners,
                float closure,
                float circularity,
                float angleRadians,
                float scaleRatio,
                float horizontalAxisScore,
                float verticalAxisScore)
            {
                this.centroid = centroid;
                this.straightness = straightness;
                this.corners = corners;
                this.closure = closure;
                this.circularity = circularity;
                this.angleRadians = angleRadians;
                this.scaleRatio = scaleRatio;
                this.horizontalAxisScore = horizontalAxisScore;
                this.verticalAxisScore = verticalAxisScore;
            }

            public static OverlayFeatures From(List<List<StrokeSample>> strokes, CompiledSeal seal)
            {
                var all = strokes.SelectMany(stroke => stroke).Select(sample => sample.position).ToList();
                var centroid = new Vector2(all.Average(point => point.x), all.Average(point => point.y));
                var min = new Vector2(all.Min(point => point.x), all.Min(point => point.y));
                var max = new Vector2(all.Max(point => point.x), all.Max(point => point.y));
                var diagonal = Mathf.Max(Vector2.Distance(min, max), 0.001f);
                var first = all[0];
                var last = all[^1];
                var closure = Mathf.Clamp01(1f - Vector2.Distance(first, last) / Mathf.Max(diagonal * 0.35f, 0.001f));
                var radii = all.Select(point => Vector2.Distance(point, centroid)).ToList();
                var meanRadius = radii.Average();
                var variance = radii.Average(radius => Mathf.Pow(radius - meanRadius, 2f));
                var circularity = Mathf.Clamp01(1f - Mathf.Sqrt(variance) / Mathf.Max(meanRadius * 0.45f, 0.001f));
                var straightness = Mathf.Clamp01(Vector2.Distance(first, last) / Mathf.Max(PathLength(all), 0.001f));
                var angle = NormalizeHalfPi(Mathf.Atan2(last.y - first.y, last.x - first.x));
                var horizontal = Mathf.Clamp01(1f - Mathf.Abs(centroid.y - seal.worldCenter.y) / Mathf.Max(seal.worldScale * 0.18f, 0.08f));
                var vertical = Mathf.Clamp01(1f - Mathf.Abs(centroid.x - seal.worldCenter.x) / Mathf.Max(seal.worldScale * 0.18f, 0.08f));

                return new OverlayFeatures(
                    centroid,
                    straightness,
                    CountCorners(strokes),
                    closure,
                    circularity,
                    angle,
                    diagonal / Mathf.Max(seal.worldScale, 0.1f),
                    horizontal,
                    vertical);
            }

            private static int CountCorners(List<List<StrokeSample>> strokes)
            {
                var points = strokes.SelectMany(stroke => stroke).Select(sample => sample.position).ToList();
                if (points.Count < 3)
                {
                    return 0;
                }

                var corners = 0;
                for (var index = 1; index < points.Count - 1; index++)
                {
                    var a = (points[index] - points[index - 1]).normalized;
                    var b = (points[index + 1] - points[index]).normalized;
                    if (Vector2.Angle(a, b) > 36f)
                    {
                        corners++;
                    }
                }

                return Mathf.Clamp(corners, 0, 8);
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
        }
    }

    public static class SpellLabels
    {
        public static string Korean(SpellFamily family)
        {
            return family switch
            {
                SpellFamily.Wind => "바람",
                SpellFamily.Earth => "땅",
                SpellFamily.Fire => "불꽃",
                SpellFamily.Water => "물",
                SpellFamily.Life => "생명",
                _ => family.ToString()
            };
        }

        public static string English(SpellFamily family)
        {
            return family.ToString().ToLowerInvariant();
        }

        public static string Korean(OverlayOperator op)
        {
            return op switch
            {
                OverlayOperator.SteelBrace => "보강",
                OverlayOperator.ElectricFork => "번개",
                OverlayOperator.IceBar => "얼음",
                OverlayOperator.SoulDot => "집중",
                OverlayOperator.VoidCut => "절단",
                OverlayOperator.MartialAxis => "축",
                _ => op.ToString()
            };
        }

        public static string English(OverlayOperator op)
        {
            return op switch
            {
                OverlayOperator.SteelBrace => "steel_brace",
                OverlayOperator.ElectricFork => "electric_fork",
                OverlayOperator.IceBar => "ice_bar",
                OverlayOperator.SoulDot => "soul_dot",
                OverlayOperator.VoidCut => "void_cut",
                OverlayOperator.MartialAxis => "martial_axis",
                _ => op.ToString().ToLowerInvariant()
            };
        }
    }
}
