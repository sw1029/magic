using System;
using UnityEngine;

namespace MagicExamHall
{
    [Serializable]
    public sealed class SpellRecognitionHandoff
    {
        public string sourceId = "";
        public SpellPhase phase = SpellPhase.Base;
        public RecognitionStatus status = RecognitionStatus.Invalid;
        public SpellFamily targetFamily = SpellFamily.Wind;
        [SerializeField] private bool hasRecognizedFamily;
        [SerializeField] private SpellFamily recognizedFamilyValue;
        [SerializeField] private bool hasRecognizedOperator;
        [SerializeField] private OverlayOperator recognizedOperatorValue;
        public string targetSealId = "";
        public Vector2 center;
        public float worldScale = 1f;
        public int strokeCount = 1;
        public float confidence;
        public float shapeConfidence;
        public float scaleRatio;
        public OverlayScaleHint scaleHint;
        public string anchorZone = "";
        public QualityVector quality;
        public string feedbackReason = "";
        public string nextHint = "";

        public SpellFamily? recognizedFamily
        {
            get => hasRecognizedFamily ? recognizedFamilyValue : null;
            set
            {
                hasRecognizedFamily = value.HasValue;
                if (value.HasValue)
                {
                    recognizedFamilyValue = value.Value;
                }
            }
        }

        public OverlayOperator? recognizedOperator
        {
            get => hasRecognizedOperator ? recognizedOperatorValue : null;
            set
            {
                hasRecognizedOperator = value.HasValue;
                if (value.HasValue)
                {
                    recognizedOperatorValue = value.Value;
                }
            }
        }

        public static SpellRecognitionHandoff Base(
            RecognitionStatus status,
            SpellFamily targetFamily,
            SpellFamily? recognizedFamily,
            Vector2 center,
            float confidence,
            QualityVector quality,
            string feedbackReason = "",
            string nextHint = "",
            float worldScale = 1f,
            int strokeCount = 1,
            string sourceId = "")
        {
            return new SpellRecognitionHandoff
            {
                sourceId = sourceId,
                phase = SpellPhase.Base,
                status = status,
                targetFamily = targetFamily,
                recognizedFamily = recognizedFamily,
                center = center,
                confidence = confidence,
                quality = quality,
                feedbackReason = feedbackReason,
                nextHint = nextHint,
                worldScale = worldScale,
                strokeCount = strokeCount
            };
        }

        public static SpellRecognitionHandoff Overlay(
            RecognitionStatus status,
            OverlayOperator? recognizedOperator,
            Vector2 center,
            float score,
            float shapeConfidence,
            string targetSealId = "",
            string feedbackReason = "",
            float scaleRatio = 0f,
            OverlayScaleHint scaleHint = OverlayScaleHint.None,
            string anchorZone = "",
            int strokeCount = 1,
            string sourceId = "")
        {
            return new SpellRecognitionHandoff
            {
                sourceId = sourceId,
                phase = SpellPhase.Overlay,
                status = status,
                recognizedOperator = recognizedOperator,
                targetSealId = targetSealId,
                center = center,
                confidence = score,
                shapeConfidence = shapeConfidence,
                scaleRatio = scaleRatio,
                scaleHint = scaleHint,
                anchorZone = anchorZone,
                feedbackReason = feedbackReason,
                strokeCount = strokeCount
            };
        }

        public BaseRecognitionResult ToBaseResult()
        {
            return new BaseRecognitionResult
            {
                spell = new SpellResult
                {
                    status = status,
                    recognizedFamily = recognizedFamily,
                    targetFamily = targetFamily,
                    confidence = Mathf.Clamp01(confidence),
                    quality = quality,
                    feedbackReason = string.IsNullOrWhiteSpace(feedbackReason) ? DefaultBaseReason() : feedbackReason,
                    nextHint = string.IsNullOrWhiteSpace(nextHint) ? DefaultBaseHint() : nextHint,
                    success = status == RecognitionStatus.Recognized && recognizedFamily.HasValue
                },
                center = center,
                worldScale = Mathf.Max(worldScale, 0.1f),
                bufferStrokeCount = Mathf.Max(strokeCount, 0)
            };
        }

        public OverlayRecognitionResult ToOverlayResult()
        {
            return new OverlayRecognitionResult
            {
                status = status,
                recognizedOperator = recognizedOperator,
                score = Mathf.Clamp01(confidence),
                shapeConfidence = Mathf.Clamp01(shapeConfidence),
                scaleRatio = scaleRatio,
                scaleHint = scaleHint,
                anchorZone = anchorZone,
                feedbackReason = string.IsNullOrWhiteSpace(feedbackReason) ? DefaultOverlayReason() : feedbackReason
            };
        }

        private string DefaultBaseReason()
        {
            if (status == RecognitionStatus.Recognized && recognizedFamily.HasValue)
            {
                return $"{SpellLabels.Korean(recognizedFamily.Value)} 문양으로 인식되었습니다.";
            }

            return $"{SpellLabels.Korean(targetFamily)} 문양으로 확정되지 않았습니다.";
        }

        private string DefaultBaseHint()
        {
            return status == RecognitionStatus.Recognized
                ? "좋습니다. 같은 문양을 유지하면 다음 시험으로 이어갈 수 있습니다."
                : $"{SpellLabels.Korean(targetFamily)}의 시작점과 끝점을 맞춰 다시 그려 보세요.";
        }

        private string DefaultOverlayReason()
        {
            if (status == RecognitionStatus.Recognized && recognizedOperator.HasValue)
            {
                return $"{SpellLabels.Korean(recognizedOperator.Value)} 양식으로 인식되었습니다.";
            }

            return "장식 후보를 문양에 안정적으로 붙이지 못했습니다.";
        }
    }
}
