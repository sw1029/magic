using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MagicExamHall
{
    public enum StrokeRecognitionKind
    {
        Base,
        Overlay,
        DetachedOverlay
    }

    public sealed class RecognitionContext
    {
        public IReadOnlyList<CompiledSeal> activeSeals = Array.Empty<CompiledSeal>();
        public float now;
    }

    public sealed class StrokeRecognitionResult
    {
        public StrokeRecognitionKind kind;
        public StrokeInputSession session = null!;
        public BaseRecognitionResult baseResult = null!;
        public OverlayRecognitionResult overlayResult = null!;
        public CompiledSeal targetSeal = null!;
        public Vector2 center;
        public int strokeCount;
        public TutorialPersonalizationSummary personalization = TutorialPersonalizationSummary.Empty;
    }

    public interface IStrokeRecognitionService
    {
        TutorialPersonalizationStore PersonalizationStore { get; }
        CustomShapeProfileStore CustomShapeStore { get; }
        StrokeRecognitionResult Recognize(StrokeInputSession session, RecognitionContext context);
        void RecordAcceptedResult(StrokeRecognitionResult result, float now);
    }

    public sealed class HeuristicStrokeRecognitionService : IStrokeRecognitionService
    {
        public HeuristicStrokeRecognitionService(
            TutorialPersonalizationStore personalizationStore = null,
            CustomShapeProfileStore customShapeStore = null)
        {
            PersonalizationStore = personalizationStore ?? new TutorialPersonalizationStore();
            CustomShapeStore = customShapeStore ?? new CustomShapeProfileStore();
        }

        public TutorialPersonalizationStore PersonalizationStore { get; }
        public CustomShapeProfileStore CustomShapeStore { get; }

        public StrokeRecognitionResult Recognize(StrokeInputSession session, RecognitionContext context)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            context ??= new RecognitionContext();
            var strokes = session.ToStrokeSamples();
            var center = session.GetWorldCenter();
            var strokeCount = strokes.Count;
            var seals = context.activeSeals ?? Array.Empty<CompiledSeal>();
            var targetSeal = SpellCastingService.FindAttachableSeal(seals, center, context.now);

            if (targetSeal != null)
            {
                var overlayResult = OverlayRecognizer.Recognize(strokes, targetSeal);
                PersonalizationStore.ApplyOverlayPersonalization(overlayResult, strokes);
                var baseResult = RecognizeBaseCandidate(strokes);
                if (baseResult.spell.status == RecognitionStatus.Recognized &&
                    baseResult.spell.recognizedFamily.HasValue &&
                    !SpellCastingService.ShouldPreferOverlayNearSeal(overlayResult))
                {
                    return new StrokeRecognitionResult
                    {
                        kind = StrokeRecognitionKind.Base,
                        session = session,
                        baseResult = baseResult,
                        center = center,
                        strokeCount = strokeCount,
                        personalization = baseResult.spell.personalization
                    };
                }

                return new StrokeRecognitionResult
                {
                    kind = StrokeRecognitionKind.Overlay,
                    session = session,
                    overlayResult = overlayResult,
                    targetSeal = targetSeal,
                    center = center,
                    strokeCount = strokeCount,
                    personalization = overlayResult.personalization
                };
            }

            var recognizedBase = RecognizeBaseCandidate(strokes);
            if (recognizedBase.spell.status == RecognitionStatus.Recognized && recognizedBase.spell.recognizedFamily.HasValue)
            {
                return new StrokeRecognitionResult
                {
                    kind = StrokeRecognitionKind.Base,
                    session = session,
                    baseResult = recognizedBase,
                    center = center,
                    strokeCount = strokeCount,
                    personalization = recognizedBase.spell.personalization
                };
            }

            var detachedOverlay = FindDetachedOverlayCandidate(strokes, center, seals, context.now);
            if (detachedOverlay != null)
            {
                PersonalizationStore.ApplyOverlayPersonalization(detachedOverlay.result, strokes);
                return new StrokeRecognitionResult
                {
                    kind = StrokeRecognitionKind.DetachedOverlay,
                    session = session,
                    overlayResult = detachedOverlay.result,
                    targetSeal = detachedOverlay.seal,
                    center = center,
                    strokeCount = strokeCount,
                    personalization = detachedOverlay.result.personalization
                };
            }

            return new StrokeRecognitionResult
            {
                kind = StrokeRecognitionKind.Base,
                session = session,
                baseResult = recognizedBase,
                center = center,
                strokeCount = strokeCount,
                personalization = recognizedBase.spell.personalization
            };
        }

        private BaseRecognitionResult RecognizeBaseCandidate(List<List<StrokeSample>> strokes)
        {
            var baseResult = SpellRuntime.RecognizeBase(strokes);
            PersonalizationStore.ApplyBasePersonalization(baseResult.spell, strokes);
            CustomShapeRecognition.ApplyToBaseResult(baseResult, strokes, CustomShapeStore);
            return baseResult;
        }

        public void RecordAcceptedResult(StrokeRecognitionResult result, float now)
        {
            if (result == null)
            {
                return;
            }

            var strokes = result.session.ToStrokeSamples();
            switch (result.kind)
            {
                case StrokeRecognitionKind.Base:
                {
                    var spell = result.baseResult?.spell;
                    if (spell?.isCustomShape == true && spell.success)
                    {
                        CustomShapeStore.RecordAutoCapture(spell.customShapeId, strokes, spell.customScore);
                    }

                    if (spell?.recognizedFamily.HasValue == true)
                    {
                        PersonalizationStore.RecordBaseCapture(spell.recognizedFamily.Value, strokes, spell, now);
                    }
                    break;
                }
                case StrokeRecognitionKind.Overlay:
                {
                    var overlay = result.overlayResult;
                    if (overlay?.recognizedOperator.HasValue == true)
                    {
                        PersonalizationStore.RecordOverlayCapture(overlay.recognizedOperator.Value, strokes, overlay, now);
                    }
                    break;
                }
            }
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

            var result = OverlayRecognizer.Recognize(strokes, nearestSeal);
            if (result.success || result.recognizedOperator.HasValue || result.score >= 0.48f || result.shapeConfidence >= 0.55f)
            {
                return new DetachedOverlayCandidate(nearestSeal, result);
            }

            return null;
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
