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
        public BaseRecognitionIntent baseIntent;
        public bool allowCustomShapes = true;
        public bool customShapesOnlyWhenSealActive;
        public bool hasCastCenter;
        public Vector2 castCenter;
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
        private const int StrongIntentColdStartCaptureLimit = 1;

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
            var center = context.hasCastCenter ? context.castCenter : session.GetWorldCenter();
            var strokeCount = strokes.Count;
            var seals = context.activeSeals ?? Array.Empty<CompiledSeal>();
            var hasActiveSeal = seals.Any(seal => context.now <= seal.expiresAt);
            var targetSeal = SpellCastingService.FindAttachableSeal(seals, center, context.now);
            var intentPreferredFamily = context.baseIntent?.IsActive == true
                ? context.baseIntent.family
                : (SpellFamily?)null;
            if (context.customShapesOnlyWhenSealActive && hasActiveSeal)
            {
                var customOnlyBase = RecognizeBaseCandidate(
                    strokes,
                    context.baseIntent,
                    targetSeal?.baseFamily ?? intentPreferredFamily,
                    context.allowCustomShapes,
                    recordColdStartAttempt: false,
                    now: context.now);
                if (!customOnlyBase.spell.isCustomShape)
                {
                    RejectNonCustomPostSealInput(customOnlyBase);
                }

                return new StrokeRecognitionResult
                {
                    kind = StrokeRecognitionKind.Base,
                    session = session,
                    baseResult = customOnlyBase,
                    center = center,
                    strokeCount = strokeCount,
                    personalization = customOnlyBase.spell.personalization
                };
            }

            if (targetSeal != null)
            {
                var overlayResult = OverlayRecognizer.Recognize(strokes, targetSeal);
                PersonalizationStore.ApplyOverlayPersonalization(overlayResult, strokes);
                var baseResult = RecognizeBaseCandidate(
                    strokes,
                    context.baseIntent,
                    targetSeal.baseFamily,
                    context.allowCustomShapes,
                    recordColdStartAttempt: false,
                    now: context.now);
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

            var recognizedBase = RecognizeBaseCandidate(
                strokes,
                context.baseIntent,
                intentPreferredFamily,
                context.allowCustomShapes,
                recordColdStartAttempt: context.baseIntent?.IsActive == true,
                now: context.now);
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

        private BaseRecognitionResult RecognizeBaseCandidate(
            List<List<StrokeSample>> strokes,
            BaseRecognitionIntent intent,
            SpellFamily? preferredCustomFamily = null,
            bool allowCustomShapes = true,
            bool recordColdStartAttempt = false,
            float now = 0f)
        {
            var preparedIntent = PrepareBaseIntent(intent);
            var baseResult = SpellRuntime.RecognizeBase(strokes, preparedIntent);
            var personalizationTarget = preparedIntent?.IsActive == true ? preparedIntent.family : (SpellFamily?)null;
            var rawColdStartSpell = recordColdStartAttempt &&
                preparedIntent?.IsActive == true &&
                preparedIntent.tutorialCaptureCount < 3
                    ? CloneColdStartSpellResult(baseResult.spell)
                    : null;
            PersonalizationStore.ApplyBasePersonalization(baseResult.spell, strokes, personalizationTarget);
            if (rawColdStartSpell != null)
            {
                PersonalizationStore.RecordColdStartAttempt(preparedIntent.family, strokes, rawColdStartSpell, now);
            }

            if (allowCustomShapes)
            {
                CustomShapeRecognition.ApplyToBaseResult(baseResult, strokes, CustomShapeStore, preferredCustomFamily);
            }

            return baseResult;
        }

        private static SpellResult CloneColdStartSpellResult(SpellResult source)
        {
            if (source == null)
            {
                return null;
            }

            return new SpellResult
            {
                status = source.status,
                recognizedFamily = source.recognizedFamily,
                targetFamily = source.targetFamily,
                confidence = source.confidence,
                quality = source.quality,
                feedbackReason = source.feedbackReason,
                nextHint = source.nextHint,
                success = source.success,
                isCustomShape = source.isCustomShape,
                customShapeId = source.customShapeId,
                customShapeLabel = source.customShapeLabel,
                customShapeToken = source.customShapeToken,
                mappedFamily = source.mappedFamily,
                customScore = source.customScore,
                defaultSimilarityScore = source.defaultSimilarityScore,
                customEventId = source.customEventId,
                customEventLabel = source.customEventLabel,
                customEventKind = source.customEventKind,
                customEventRole = source.customEventRole,
                customEventUsesDirection = source.customEventUsesDirection,
                customEventOperatorOnly = source.customEventOperatorOnly,
                customEventBlocks = source.customEventBlocks,
                customEventBlocked = source.customEventBlocked,
                customEventBlockedBy = source.customEventBlockedBy,
                customEventPersistence = source.customEventPersistence,
                customEventLifetimeSeconds = source.customEventLifetimeSeconds,
                customEventOrigin = source.customEventOrigin,
                customEventDirection = source.customEventDirection,
                customEventStartPoint = source.customEventStartPoint,
                customEventEndPoint = source.customEventEndPoint,
                intentFamily = source.intentFamily,
                intentGoalId = source.intentGoalId,
                intentSource = source.intentSource,
                intentStrength = source.intentStrength,
                intentSimilarityScore = source.intentSimilarityScore,
                intentWeakConsiderationApplied = source.intentWeakConsiderationApplied,
                intentStrongConsiderationApplied = source.intentStrongConsiderationApplied,
                intentScoreLift = source.intentScoreLift,
                preIntentFamily = source.preIntentFamily,
                preIntentConfidence = source.preIntentConfidence
            };
        }

        private BaseRecognitionIntent PrepareBaseIntent(BaseRecognitionIntent intent)
        {
            if (intent == null || !intent.IsActive)
            {
                return intent;
            }

            var targetCaptureCount = PersonalizationStore.CountBaseCaptures(intent.family);
            return new BaseRecognitionIntent
            {
                family = intent.family,
                goalId = intent.goalId,
                source = intent.source,
                distance = intent.distance,
                radius = intent.radius,
                strength = intent.strength,
                tutorialCaptureCount = targetCaptureCount,
                strongConsiderationEnabled = targetCaptureCount < StrongIntentColdStartCaptureLimit
            };
        }

        private static void RejectNonCustomPostSealInput(BaseRecognitionResult baseResult)
        {
            if (baseResult?.spell == null)
            {
                return;
            }

            baseResult.spell.status = RecognitionStatus.Invalid;
            baseResult.spell.recognizedFamily = null;
            baseResult.spell.success = false;
            baseResult.spell.feedbackReason = "활성 seal 이후의 추가 입력은 저장된 커스텀 도형만 사용합니다.";
            baseResult.spell.nextHint = "커스텀 도형 책에 도형을 저장한 뒤, seal이 떠 있는 동안 그 도형을 그려 보세요.";
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
