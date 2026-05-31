using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MagicExamHall
{
    public enum SpellCastOutcomeKind
    {
        BaseFailed,
        BaseSucceeded,
        OverlayFailed,
        OverlaySucceeded,
        OverlayDuplicate,
        OverlayStackFull,
        DetachedOverlay
    }

    public sealed class SpellCastOutcome
    {
        public SpellCastOutcomeKind kind;
        public BaseRecognitionResult baseResult = null!;
        public OverlayRecognitionResult overlayResult = null!;
        public CompiledSeal targetSeal = null!;
        public CompiledSeal createdSeal = null!;
        public OverlayOperator? overlayOperator;
        public Vector2 center;
        public int strokeCount;
        public StrokeRecognitionResult recognitionResult = null!;
    }

    public sealed class SpellCastingService
    {
        public const int MaxOverlayStack = 3;
        public const float MinimumOverlayAttachRadius = 1.35f;
        public const float OverlayAttachScaleMultiplier = 0.95f;

        public SpellCastOutcome Process(
            List<List<StrokeSample>> strokes,
            Vector2 center,
            int strokeCount,
            IReadOnlyList<CompiledSeal> seals,
            float now)
        {
            if (strokes == null)
            {
                throw new ArgumentNullException(nameof(strokes));
            }

            if (seals == null)
            {
                throw new ArgumentNullException(nameof(seals));
            }

            var targetSeal = FindAttachableSeal(seals, center, now);
            if (targetSeal != null)
            {
                return ProcessNearSeal(strokes, center, strokeCount, targetSeal, now);
            }

            var detachedOverlay = FindDetachedOverlayCandidate(strokes, center, seals, now);
            if (detachedOverlay != null)
            {
                return new SpellCastOutcome
                {
                    kind = SpellCastOutcomeKind.DetachedOverlay,
                    overlayResult = detachedOverlay.result,
                    targetSeal = detachedOverlay.seal,
                    center = center,
                    strokeCount = strokeCount
                };
            }

            return ProcessBase(strokes, center, strokeCount, now);
        }

        public SpellCastOutcome ProcessRecognitionResult(StrokeRecognitionResult recognitionResult, float now)
        {
            if (recognitionResult == null)
            {
                throw new ArgumentNullException(nameof(recognitionResult));
            }

            var outcome = recognitionResult.kind switch
            {
                StrokeRecognitionKind.Base => ProcessBaseResult(
                    recognitionResult.baseResult,
                    recognitionResult.center,
                    recognitionResult.strokeCount,
                    now),
                StrokeRecognitionKind.Overlay => ProcessOverlayResult(
                    recognitionResult.overlayResult,
                    recognitionResult.targetSeal,
                    recognitionResult.center,
                    recognitionResult.strokeCount),
                StrokeRecognitionKind.DetachedOverlay => new SpellCastOutcome
                {
                    kind = SpellCastOutcomeKind.DetachedOverlay,
                    overlayResult = recognitionResult.overlayResult,
                    targetSeal = recognitionResult.targetSeal,
                    center = recognitionResult.center,
                    strokeCount = recognitionResult.strokeCount
                },
                _ => throw new ArgumentOutOfRangeException(nameof(recognitionResult.kind), recognitionResult.kind, "Unhandled recognition result.")
            };

            outcome.recognitionResult = recognitionResult;
            return outcome;
        }

        public static float AttachRadiusFor(CompiledSeal seal)
        {
            if (seal == null)
            {
                throw new ArgumentNullException(nameof(seal));
            }

            return Mathf.Max(MinimumOverlayAttachRadius, seal.worldScale * OverlayAttachScaleMultiplier);
        }

        /// <summary>
        /// Finds the active seal that an external input adapter should attach an overlay result to.
        /// </summary>
        public static CompiledSeal FindAttachableSeal(IReadOnlyList<CompiledSeal> seals, Vector2 center, float now)
        {
            if (seals == null)
            {
                throw new ArgumentNullException(nameof(seals));
            }

            return seals
                .Where(seal => now <= seal.expiresAt)
                .OrderBy(seal => Vector2.Distance(center, seal.worldCenter))
                .FirstOrDefault(seal => Vector2.Distance(center, seal.worldCenter) <= AttachRadiusFor(seal));
        }

        /// <summary>
        /// Converts a base recognition result from another input layer into the same game outcome used by world drawing.
        /// </summary>
        public SpellCastOutcome ProcessBaseResult(
            BaseRecognitionResult baseResult,
            Vector2 center,
            int strokeCount,
            float now)
        {
            if (baseResult == null)
            {
                throw new ArgumentNullException(nameof(baseResult));
            }

            baseResult.center = center;
            baseResult.bufferStrokeCount = strokeCount;

            if (baseResult.spell.status != RecognitionStatus.Recognized || !baseResult.spell.recognizedFamily.HasValue)
            {
                return new SpellCastOutcome
                {
                    kind = SpellCastOutcomeKind.BaseFailed,
                    baseResult = baseResult,
                    center = center,
                    strokeCount = strokeCount
                };
            }

            return new SpellCastOutcome
            {
                kind = SpellCastOutcomeKind.BaseSucceeded,
                baseResult = baseResult,
                createdSeal = SpellRuntime.CreateSeal(baseResult, now),
                center = center,
                strokeCount = strokeCount
            };
        }

        /// <summary>
        /// Applies an overlay recognition result from another input layer to an existing seal and reports duplicate/full-stack states.
        /// </summary>
        public SpellCastOutcome ProcessOverlayResult(
            OverlayRecognitionResult result,
            CompiledSeal seal,
            Vector2 center,
            int strokeCount)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (seal == null)
            {
                throw new ArgumentNullException(nameof(seal));
            }

            if (!result.success)
            {
                return new SpellCastOutcome
                {
                    kind = SpellCastOutcomeKind.OverlayFailed,
                    overlayResult = result,
                    targetSeal = seal,
                    center = center,
                    strokeCount = strokeCount
                };
            }

            var op = result.recognizedOperator!.Value;
            if (seal.overlayStack.Contains(op))
            {
                return new SpellCastOutcome
                {
                    kind = SpellCastOutcomeKind.OverlayDuplicate,
                    overlayResult = result,
                    targetSeal = seal,
                    overlayOperator = op,
                    center = center,
                    strokeCount = strokeCount
                };
            }

            if (seal.overlayStack.Count >= MaxOverlayStack)
            {
                return new SpellCastOutcome
                {
                    kind = SpellCastOutcomeKind.OverlayStackFull,
                    overlayResult = result,
                    targetSeal = seal,
                    overlayOperator = op,
                    center = center,
                    strokeCount = strokeCount
                };
            }

            seal.overlayStack.Add(op);
            return new SpellCastOutcome
            {
                kind = SpellCastOutcomeKind.OverlaySucceeded,
                overlayResult = result,
                targetSeal = seal,
                overlayOperator = op,
                center = center,
                strokeCount = strokeCount
            };
        }

        private SpellCastOutcome ProcessBase(
            List<List<StrokeSample>> strokes,
            Vector2 center,
            int strokeCount,
            float now)
        {
            var baseResult = SpellRuntime.RecognizeBase(strokes);
            return ProcessBaseResult(baseResult, center, strokeCount, now);
        }

        private SpellCastOutcome ProcessOverlay(
            List<List<StrokeSample>> strokes,
            Vector2 center,
            int strokeCount,
            CompiledSeal seal)
        {
            var result = OverlayRecognizer.Recognize(strokes, seal);
            return ProcessOverlayResult(result, seal, center, strokeCount);
        }

        private SpellCastOutcome ProcessNearSeal(
            List<List<StrokeSample>> strokes,
            Vector2 center,
            int strokeCount,
            CompiledSeal seal,
            float now)
        {
            var overlayResult = OverlayRecognizer.Recognize(strokes, seal);
            var baseResult = SpellRuntime.RecognizeBase(strokes);
            if (baseResult.spell.status == RecognitionStatus.Recognized && baseResult.spell.recognizedFamily.HasValue)
            {
                if (ShouldPreferOverlayNearSeal(overlayResult))
                {
                    return ProcessOverlayResult(overlayResult, seal, center, strokeCount);
                }

                return ProcessBaseResult(baseResult, center, strokeCount, now);
            }

            return ProcessOverlayResult(overlayResult, seal, center, strokeCount);
        }

        private static DetachedOverlayCandidate FindDetachedOverlayCandidate(
            List<List<StrokeSample>> strokes,
            Vector2 center,
            IReadOnlyList<CompiledSeal> seals,
            float now)
        {
            var nearestSeal = seals
                .Where(seal => now <= seal.expiresAt)
                .OrderBy(seal => Vector2.Distance(center, seal.worldCenter))
                .FirstOrDefault();
            if (nearestSeal == null)
            {
                return null;
            }

            var basePreview = SpellRuntime.RecognizeBase(strokes);
            if (basePreview.spell.status == RecognitionStatus.Recognized && basePreview.spell.recognizedFamily.HasValue)
            {
                return null;
            }

            var result = OverlayRecognizer.Recognize(strokes, nearestSeal);
            if (OverlayAttemptLooksIntentional(result))
            {
                return new DetachedOverlayCandidate(nearestSeal, result);
            }

            return null;
        }

        private static bool OverlayAttemptLooksIntentional(OverlayRecognitionResult result)
        {
            return result.success || result.score >= 0.52f || result.shapeConfidence >= 0.60f;
        }

        private static bool ShouldPreferOverlayNearSeal(OverlayRecognitionResult result)
        {
            if (result.success)
            {
                return true;
            }

            if (result.recognizedOperator.HasValue && result.scaleHint != OverlayScaleHint.None && result.score >= 0.70f && result.shapeConfidence >= 0.70f)
            {
                return true;
            }

            return result.status == RecognitionStatus.Ambiguous && result.score >= 0.72f && result.shapeConfidence >= 0.74f;
        }

        private sealed class DetachedOverlayCandidate
        {
            public readonly CompiledSeal seal;
            public readonly OverlayRecognitionResult result;

            public DetachedOverlayCandidate(CompiledSeal seal, OverlayRecognitionResult result)
            {
                this.seal = seal;
                this.result = result;
            }
        }
    }
}
