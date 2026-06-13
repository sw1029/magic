using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MagicExamHall;
using NUnit.Framework;
using UnityEngine;

namespace MagicExamHall.Tests
{
    public sealed class StrokeInputRecognitionHandoffTests
    {
        [Test]
        public void StrokeInputSessionRoundTripsLegacyStrokeSamples()
        {
            var legacy = GestureRecognizer.CreateCanonicalSamples(SpellFamily.Wind, 1.6f, 0.03f);
            var session = StrokeInputSessionExtensions.FromStrokeSamples(legacy, "round-trip", 10f);

            var restored = session.ToStrokeSamples();

            Assert.That(restored.Count, Is.EqualTo(legacy.Count));
            Assert.That(session.CoordinateSpace, Is.EqualTo(InputCoordinateSpace.World));
            Assert.That(session.GetWorldCenter().x, Is.EqualTo(1.6f * 0.5f).Within(0.001f));
        }

        [Test]
        public void StrokeSessionBufferGroupsStrokesUntilTimeout()
        {
            var buffer = new StrokeSessionBuffer(1f);
            var completed = new List<StrokeInputSession>();
            buffer.SessionCompleted += completed.Add;

            buffer.PushCompletedStroke(MakeInputStroke("a", 0f), 0f);
            buffer.Tick(0.9f);
            buffer.PushCompletedStroke(MakeInputStroke("b", 0.3f), 0.9f);
            buffer.Tick(1.8f);
            buffer.Tick(2.0f);

            Assert.That(completed.Count, Is.EqualTo(1));
            Assert.That(completed[0].Strokes.Select(stroke => stroke.Id), Is.EqualTo(new[] { "a", "b" }));
        }

        [Test]
        public void StrokeSessionBufferWaitsWhileUserIsDrawingNextStroke()
        {
            var buffer = new StrokeSessionBuffer(1f);
            var completed = new List<StrokeInputSession>();
            buffer.SessionCompleted += completed.Add;

            buffer.PushCompletedStroke(MakeInputStroke("a", 0f), 0f);
            buffer.Tick(1.4f, inputInProgress: true);

            Assert.That(completed, Is.Empty);

            buffer.Tick(1.5f, inputInProgress: false);

            Assert.That(completed, Has.Count.EqualTo(1));
            Assert.That(completed[0].Strokes.Select(stroke => stroke.Id), Is.EqualTo(new[] { "a" }));
        }

        [Test]
        public void RecognitionServiceCanRecordAcceptedBaseAsTutorialCapture()
        {
            var service = new HeuristicStrokeRecognitionService();
            var strokes = GestureRecognizer.CreateCanonicalSamples(SpellFamily.Fire, timeStep: 0.03f);
            var session = StrokeInputSessionExtensions.FromStrokeSamples(strokes, "fire", 0f);

            var result = service.Recognize(session, new RecognitionContext { activeSeals = new List<CompiledSeal>(), now = 0f });
            service.RecordAcceptedResult(result, 0.2f);

            Assert.That(result.kind, Is.EqualTo(StrokeRecognitionKind.Base));
            Assert.That(result.baseResult.spell.recognizedFamily, Is.EqualTo(SpellFamily.Fire));
            Assert.That(service.PersonalizationStore.CaptureCount, Is.EqualTo(1));
        }

        [Test]
        public void ActiveSealCustomOnlyContextRejectsBuiltInOverlayCandidate()
        {
            var service = new HeuristicStrokeRecognitionService();
            var baseStrokes = GestureRecognizer.CreateCanonicalSamples(SpellFamily.Earth, timeStep: 0.03f);
            var seal = SpellRuntime.CreateSeal(SpellRuntime.RecognizeBase(baseStrokes), 0f);
            var overlayStrokes = OverlayRecognizer.CreateCanonicalSamples(OverlayOperator.IceBar, seal.worldCenter, seal.worldScale * 0.24f, 0.03f);
            var session = StrokeInputSessionExtensions.FromStrokeSamples(overlayStrokes, "overlay", 0.2f);

            var result = service.Recognize(session, new RecognitionContext
            {
                activeSeals = new List<CompiledSeal> { seal },
                customShapesOnlyWhenSealActive = true,
                now = 0.2f
            });

            Assert.That(result.kind, Is.EqualTo(StrokeRecognitionKind.Base));
            Assert.That(result.overlayResult, Is.Null);
            Assert.That(result.baseResult.spell.isCustomShape, Is.False);
            Assert.That(result.baseResult.spell.status, Is.EqualTo(RecognitionStatus.Invalid));
            Assert.That(result.baseResult.spell.recognizedFamily, Is.Null);
        }

        [Test]
        public void ActiveSealCustomOnlyContextAllowsSavedCustomShape()
        {
            var profilePath = Path.Combine(Path.GetTempPath(), $"custom-shape-{Guid.NewGuid():N}.json");
            try
            {
                var store = new CustomShapeProfileStore(profilePath);
                var gold = GestureRecognizer.CreateCanonicalSamples(SpellFamily.Wind, timeStep: 0.03f);
                Assert.That(store.TrySaveSlot(0, "test wind", "test|wind|line", SpellFamily.Wind, gold, out var message), Is.True, message);
                var service = new HeuristicStrokeRecognitionService(null, store);
                var seal = SpellRuntime.CreateSeal(SpellRuntime.RecognizeBase(GestureRecognizer.CreateCanonicalSamples(SpellFamily.Earth, timeStep: 0.03f)), 0f);
                var session = StrokeInputSessionExtensions.FromStrokeSamples(gold, "custom", 0.2f);

                var result = service.Recognize(session, new RecognitionContext
                {
                    activeSeals = new List<CompiledSeal> { seal },
                    customShapesOnlyWhenSealActive = true,
                    now = 0.2f
                });

                Assert.That(result.kind, Is.EqualTo(StrokeRecognitionKind.Base));
                Assert.That(result.baseResult.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
                Assert.That(result.baseResult.spell.isCustomShape, Is.True);
                Assert.That(result.baseResult.spell.customShapeLabel, Is.EqualTo("test wind"));
                Assert.That(result.baseResult.spell.recognizedFamily, Is.EqualTo(SpellFamily.Wind));
            }
            finally
            {
                if (File.Exists(profilePath))
                {
                    File.Delete(profilePath);
                }
            }
        }

        [Test]
        public void ActiveSealCustomOnlyContextCanDisableSavedCustomShapes()
        {
            var profilePath = Path.Combine(Path.GetTempPath(), $"custom-shape-{Guid.NewGuid():N}.json");
            try
            {
                var store = new CustomShapeProfileStore(profilePath);
                var gold = GestureRecognizer.CreateCanonicalSamples(SpellFamily.Wind, timeStep: 0.03f);
                Assert.That(store.TrySaveSlot(0, "test wind", "test|wind|line", SpellFamily.Wind, gold, out var message), Is.True, message);
                var service = new HeuristicStrokeRecognitionService(null, store);
                var seal = SpellRuntime.CreateSeal(SpellRuntime.RecognizeBase(GestureRecognizer.CreateCanonicalSamples(SpellFamily.Earth, timeStep: 0.03f)), 0f);
                var session = StrokeInputSessionExtensions.FromStrokeSamples(gold, "custom-disabled", 0.2f);

                var result = service.Recognize(session, new RecognitionContext
                {
                    activeSeals = new List<CompiledSeal> { seal },
                    allowCustomShapes = false,
                    customShapesOnlyWhenSealActive = true,
                    now = 0.2f
                });

                Assert.That(result.kind, Is.EqualTo(StrokeRecognitionKind.Base));
                Assert.That(result.baseResult.spell.isCustomShape, Is.False);
                Assert.That(result.baseResult.spell.status, Is.EqualTo(RecognitionStatus.Invalid));
                Assert.That(result.baseResult.spell.recognizedFamily, Is.Null);
            }
            finally
            {
                if (File.Exists(profilePath))
                {
                    File.Delete(profilePath);
                }
            }
        }

        [Test]
        public void NearGoalIntentDoesNotApplyWhenNormalRecognitionAlreadySucceeds()
        {
            var service = new HeuristicStrokeRecognitionService();
            var strokes = GestureRecognizer.CreateCanonicalSamples(SpellFamily.Fire, timeStep: 0.03f);
            var session = StrokeInputSessionExtensions.FromStrokeSamples(strokes, "fire", 0f);
            var intent = new BaseRecognitionIntent
            {
                family = SpellFamily.Fire,
                goalId = "ember",
                source = "near_goal_symbol",
                radius = 2f,
                strength = 1f
            };

            var result = service.Recognize(session, new RecognitionContext { baseIntent = intent, activeSeals = new List<CompiledSeal>(), now = 0f });
            service.RecordAcceptedResult(result, 0.2f);

            Assert.That(result.baseResult.spell.recognizedFamily, Is.EqualTo(SpellFamily.Fire));
            Assert.That(result.baseResult.spell.intentStrongConsiderationApplied, Is.False);
            Assert.That(service.PersonalizationStore.CaptureCount, Is.EqualTo(1));
        }

        [Test]
        public void TargetCandidateDoesNotReportWindWhenTargetFamilyDiffers()
        {
            var wind = GestureRecognizer.CreateCanonicalSamples(SpellFamily.Wind, timeStep: 0.03f);

            var fireCandidate = GestureRecognizer.Recognize(wind, SpellFamily.Fire);

            Assert.That(fireCandidate.success, Is.False);
            Assert.That(fireCandidate.recognizedFamily, Is.Null);
            Assert.That(fireCandidate.targetFamily, Is.EqualTo(SpellFamily.Fire));
        }

        [Test]
        public void ActiveSealDoesNotForceDistantInputIntoPostSealCustomPhase()
        {
            var service = new HeuristicStrokeRecognitionService();
            var waterSeal = SpellRuntime.CreateSeal(
                SpellRuntime.RecognizeBase(GestureRecognizer.CreateCanonicalSamples(SpellFamily.Water, timeStep: 0.03f)),
                0f);
            var distantFire = Offset(
                GestureRecognizer.CreateCanonicalSamples(SpellFamily.Fire, timeStep: 0.03f),
                waterSeal.worldCenter + new Vector2(1200f, 0f));
            var session = StrokeInputSessionExtensions.FromStrokeSamples(distantFire, "distant-fire", 0.2f);

            var result = service.Recognize(session, new RecognitionContext
            {
                activeSeals = new[] { waterSeal },
                customShapesOnlyWhenSealActive = true,
                now = 0.2f
            });

            Assert.That(result.kind, Is.EqualTo(StrokeRecognitionKind.Base));
            Assert.That(result.baseResult.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(result.baseResult.spell.recognizedFamily, Is.EqualTo(SpellFamily.Fire));
            Assert.That(result.baseResult.spell.isCustomShape, Is.False);
        }

        [Test]
        public void CustomShapeRequiresAttachableSealWhenCustomOnlyModeIsEnabled()
        {
            var profilePath = Path.Combine(Path.GetTempPath(), $"custom-shape-{Guid.NewGuid():N}.json");
            try
            {
                var store = new CustomShapeProfileStore(profilePath);
                var gold = GestureRecognizer.CreateCanonicalSamples(SpellFamily.Wind, timeStep: 0.03f);
                Assert.That(store.TrySaveSlot(0, "test wind", "test|wind|line", SpellFamily.Wind, gold, out var message), Is.True, message);
                var service = new HeuristicStrokeRecognitionService(null, store);
                var session = StrokeInputSessionExtensions.FromStrokeSamples(gold, "custom-without-seal", 0.2f);

                var result = service.Recognize(session, new RecognitionContext
                {
                    activeSeals = Array.Empty<CompiledSeal>(),
                    allowCustomShapes = true,
                    customShapesOnlyWhenSealActive = true,
                    now = 0.2f
                });

                Assert.That(result.kind, Is.EqualTo(StrokeRecognitionKind.Base));
                Assert.That(result.baseResult.spell.isCustomShape, Is.False);
                Assert.That(result.baseResult.spell.customShapeLabel, Is.Empty);
            }
            finally
            {
                if (File.Exists(profilePath))
                {
                    File.Delete(profilePath);
                }
            }
        }

        [Test]
        public void NearGoalWeakIntentAddsSmallLiftWithoutStrongConsideration()
        {
            var wind = GestureRecognizer.CreateCanonicalSamples(SpellFamily.Wind, timeStep: 0.03f);
            var noIntent = GestureRecognizer.Recognize(wind, SpellFamily.Fire);
            var intent = new BaseRecognitionIntent
            {
                family = SpellFamily.Fire,
                goalId = "ember",
                source = "near_goal_symbol",
                radius = 2f,
                strength = 0.6f
            };

            var withIntent = GestureRecognizer.Recognize(wind, SpellFamily.Fire, intent);

            Assert.That(withIntent.intentWeakConsiderationApplied, Is.True);
            Assert.That(withIntent.intentStrongConsiderationApplied, Is.False);
            Assert.That(withIntent.intentScoreLift, Is.GreaterThan(0f).And.LessThan(0.04f));
            Assert.That(withIntent.confidence, Is.GreaterThan(noIntent.confidence));
        }

        [Test]
        public void NearGoalIntentRequiresShapeSimilarity()
        {
            var wind = GestureRecognizer.CreateCanonicalSamples(SpellFamily.Wind, timeStep: 0.03f);
            var intent = new BaseRecognitionIntent
            {
                family = SpellFamily.Fire,
                goalId = "ember",
                source = "near_goal_symbol",
                radius = 2f,
                strength = 1f
            };

            var result = SpellRuntime.RecognizeBase(wind, intent);

            Assert.That(result.spell.recognizedFamily, Is.EqualTo(SpellFamily.Wind));
            Assert.That(result.spell.intentStrongConsiderationApplied, Is.False);
        }

        [Test]
        public void NearGoalIntentHoldsPlausibleColdStartShapeForTargetFamily()
        {
            var strokes = OpenFireSamples();
            var intent = new BaseRecognitionIntent
            {
                family = SpellFamily.Fire,
                goalId = "ember",
                source = "near_goal_symbol",
                radius = 2f,
                strength = 1f
            };

            var result = SpellRuntime.RecognizeBase(strokes, intent);

            Assert.That(result.spell.targetFamily, Is.EqualTo(SpellFamily.Fire));
            Assert.That(result.spell.intentStrongConsiderationApplied, Is.True);
            Assert.That(result.spell.intentSimilarityScore, Is.GreaterThanOrEqualTo(0.50f));
            Assert.That(result.spell.recognizedFamily, Is.Not.EqualTo(SpellFamily.Wind));
        }

        [Test]
        public void NearGoalStrongIntentDisablesAfterFirstTutorialCapture()
        {
            var service = new HeuristicStrokeRecognitionService();
            var canonical = GestureRecognizer.CreateCanonicalSamples(SpellFamily.Fire, timeStep: 0.03f);
            var seedSession = StrokeInputSessionExtensions.FromStrokeSamples(canonical, "seed", 0f);
            var seedResult = service.Recognize(seedSession, new RecognitionContext { activeSeals = new List<CompiledSeal>(), now = 0f });
            service.RecordAcceptedResult(seedResult, 0.2f);

            var intent = new BaseRecognitionIntent
            {
                family = SpellFamily.Fire,
                goalId = "ember",
                source = "near_goal_symbol",
                radius = 2f,
                strength = 1f
            };
            var current = StrokeInputSessionExtensions.FromStrokeSamples(OpenFireSamples(), "current", 10f);

            var result = service.Recognize(current, new RecognitionContext { baseIntent = intent, activeSeals = new List<CompiledSeal>(), now = 10f });

            Assert.That(result.baseResult.intent.tutorialCaptureCount, Is.EqualTo(1));
            Assert.That(result.baseResult.intent.strongConsiderationEnabled, Is.False);
            Assert.That(result.baseResult.spell.intentWeakConsiderationApplied, Is.True);
            Assert.That(result.baseResult.spell.intentStrongConsiderationApplied, Is.False);
            Assert.That(service.PersonalizationStore.CaptureCount, Is.EqualTo(1));
        }

        [TestCase(SpellFamily.Earth, "custom_earth")]
        [TestCase(SpellFamily.Earth, "earth_stairs")]
        [TestCase(SpellFamily.Earth, "beam_earth")]
        [TestCase(SpellFamily.Water, "final_puddle")]
        public void LaterFloorGoalIntentKeepsStrongConsiderationAfterEarlierBaseCapture(SpellFamily family, string goalId)
        {
            var service = new HeuristicStrokeRecognitionService();
            var seed = GestureRecognizer.CreateCanonicalSamples(family, timeStep: 0.03f);
            var seedSession = StrokeInputSessionExtensions.FromStrokeSamples(seed, "base-seed", 0f);
            var seedResult = service.Recognize(seedSession, new RecognitionContext { activeSeals = new List<CompiledSeal>(), now = 0f });
            service.RecordAcceptedResult(seedResult, 0.2f);

            var intent = new BaseRecognitionIntent
            {
                family = family,
                goalId = goalId,
                source = "near_goal_symbol",
                radius = 3f,
                strength = 1f
            };
            var strokes = family == SpellFamily.Earth
                ? EarthTrapezoidSamples()
                : GestureRecognizer.CreateCanonicalSamples(family, timeStep: 0.03f);
            var current = StrokeInputSessionExtensions.FromStrokeSamples(strokes, "later-floor-goal", 10f);

            var result = service.Recognize(current, new RecognitionContext { baseIntent = intent, activeSeals = new List<CompiledSeal>(), now = 10f });

            Assert.That(result.baseResult.intent.tutorialCaptureCount, Is.EqualTo(1));
            Assert.That(result.baseResult.intent.strongConsiderationEnabled, Is.True);
            Assert.That(result.baseResult.spell.recognizedFamily, Is.EqualTo(family));
            Assert.That(result.baseResult.spell.intentFamily, Is.EqualTo(family));
            Assert.That(result.baseResult.spell.intentGoalId, Is.EqualTo(goalId));
        }

        [Test]
        public void PersonalizationSummaryAppearsAfterTutorialCapture()
        {
            var service = new HeuristicStrokeRecognitionService();
            for (var index = 0; index < 3; index++)
            {
                var seed = Offset(GestureRecognizer.CreateCanonicalSamples(SpellFamily.Water, timeStep: 0.03f), new Vector2(index * 2f, 0f));
                var seedSession = StrokeInputSessionExtensions.FromStrokeSamples(seed, $"seed-{index}", index);
                var seedResult = service.Recognize(seedSession, new RecognitionContext { activeSeals = new List<CompiledSeal>(), now = index });
                service.RecordAcceptedResult(seedResult, index);
            }

            var current = StrokeInputSessionExtensions.FromStrokeSamples(
                GestureRecognizer.CreateCanonicalSamples(SpellFamily.Water, timeStep: 0.03f),
                "current",
                10f);
            var result = service.Recognize(current, new RecognitionContext { activeSeals = new List<CompiledSeal>(), now = 10f });

            Assert.That(result.personalization.targetSampleCount, Is.EqualTo(3));
            Assert.That(result.personalization.stage, Is.EqualTo("enough_shot"));
            Assert.That(result.baseResult.spell.confidence, Is.GreaterThanOrEqualTo(result.personalization.baselineConfidence));
        }

        [Test]
        public void RecognitionServiceCollectsColdStartAttemptsEvenWhenIntentSucceeds()
        {
            var service = new HeuristicStrokeRecognitionService();
            var strokes = GestureRecognizer.CreateCanonicalSamples(SpellFamily.Water, timeStep: 0.03f);
            var session = StrokeInputSessionExtensions.FromStrokeSamples(strokes, "water-cold-start", 0f);
            var intent = new BaseRecognitionIntent
            {
                family = SpellFamily.Water,
                goalId = "puddle",
                source = "near_goal_symbol",
                radius = 2f,
                strength = 1f
            };

            var result = service.Recognize(session, new RecognitionContext
            {
                baseIntent = intent,
                activeSeals = new List<CompiledSeal>(),
                now = 0f
            });

            Assert.That(result.baseResult.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(service.PersonalizationStore.ColdStartAttemptCount, Is.EqualTo(1));
        }

        [Test]
        public void RepeatedColdStartCaseAcceleratesAmbiguousBiasCorrection()
        {
            var store = new TutorialPersonalizationStore();
            var strokes = GestureRecognizer.CreateCanonicalSamples(SpellFamily.Water, timeStep: 0.03f);
            var first = ColdStartAmbiguousResult(SpellFamily.Water, 0.73f);

            store.ApplyBasePersonalization(first, strokes, SpellFamily.Water);
            store.RecordColdStartAttempt(SpellFamily.Water, strokes, first, 0f);
            store.RecordColdStartAttempt(
                SpellFamily.Water,
                Offset(strokes, new Vector2(0.025f, -0.015f)),
                ColdStartAmbiguousResult(SpellFamily.Water, 0.73f),
                0.5f);

            var current = ColdStartAmbiguousResult(SpellFamily.Water, 0.73f);
            store.ApplyBasePersonalization(current, Offset(strokes, new Vector2(0.015f, 0.01f)), SpellFamily.Water);

            Assert.That(first.personalization.acceleratedByRepeatedCase, Is.False);
            Assert.That(current.personalization.acceleratedByRepeatedCase, Is.True);
            Assert.That(current.personalization.repeatedCaseCount, Is.GreaterThanOrEqualTo(3));
            Assert.That(current.personalization.repeatedCaseLift, Is.GreaterThan(0f));
            Assert.That(current.personalization.adjustedConfidence, Is.GreaterThan(current.personalization.baselineConfidence));
            Assert.That(current.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(current.recognizedFamily, Is.EqualTo(SpellFamily.Water));
            Assert.That(current.success, Is.True);
            Assert.That(current.personalization.promotedByPersonalization, Is.True);
        }

        [Test]
        public void RepeatedColdStartFailuresUseExtremeCorrectionForInvalidInput()
        {
            var store = new TutorialPersonalizationStore();
            var strokes = GestureRecognizer.CreateCanonicalSamples(SpellFamily.Water, timeStep: 0.03f);
            store.RecordColdStartAttempt(SpellFamily.Water, strokes, ColdStartInvalidResult(SpellFamily.Water, 0.44f), 0f);
            store.RecordColdStartAttempt(
                SpellFamily.Water,
                Offset(strokes, new Vector2(0.018f, -0.012f)),
                ColdStartInvalidResult(SpellFamily.Water, 0.44f),
                0.4f);

            var current = ColdStartInvalidResult(SpellFamily.Water, 0.44f);
            store.ApplyBasePersonalization(current, Offset(strokes, new Vector2(-0.01f, 0.016f)), SpellFamily.Water);

            Assert.That(current.personalization.extremeColdStartCorrection, Is.True);
            Assert.That(current.personalization.repeatedCaseFailureCount, Is.GreaterThanOrEqualTo(3));
            Assert.That(current.personalization.repeatedCaseLift, Is.GreaterThan(0.08f));
            Assert.That(current.personalization.decision, Is.EqualTo(TutorialDynamicDecision.Accept));
            Assert.That(current.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(current.recognizedFamily, Is.EqualTo(SpellFamily.Water));
            Assert.That(current.success, Is.True);
        }

        [Test]
        public void RepeatedColdStartCaseCanCorrectWrongRecognizedFamilyTowardIntent()
        {
            var store = new TutorialPersonalizationStore();
            var strokes = GestureRecognizer.CreateCanonicalSamples(SpellFamily.Water, timeStep: 0.03f);
            store.RecordColdStartAttempt(SpellFamily.Water, strokes, WrongRecognizedResult(SpellFamily.Earth, 0.74f), 0f);
            store.RecordColdStartAttempt(
                SpellFamily.Water,
                Offset(strokes, new Vector2(-0.02f, 0.015f)),
                WrongRecognizedResult(SpellFamily.Earth, 0.74f),
                0.5f);

            var current = WrongRecognizedResult(SpellFamily.Earth, 0.74f);
            store.ApplyBasePersonalization(current, Offset(strokes, new Vector2(0.012f, -0.008f)), SpellFamily.Water);

            Assert.That(current.personalization.acceleratedByRepeatedCase, Is.True);
            Assert.That(current.personalization.decision, Is.EqualTo(TutorialDynamicDecision.Accept));
            Assert.That(current.recognizedFamily, Is.EqualTo(SpellFamily.Water));
            Assert.That(current.targetFamily, Is.EqualTo(SpellFamily.Water));
            Assert.That(current.success, Is.True);
            Assert.That(current.personalization.promotedByPersonalization, Is.True);
        }

        [Test]
        public void ColdStartContrastRaisesThresholdForOtherFamilyCluster()
        {
            var store = new TutorialPersonalizationStore();
            var water = GestureRecognizer.CreateCanonicalSamples(SpellFamily.Water, timeStep: 0.03f);
            var fire = GestureRecognizer.CreateCanonicalSamples(SpellFamily.Fire, timeStep: 0.03f);
            var waterSuccess = GestureRecognizer.Recognize(water, SpellFamily.Water);
            store.RecordBaseCapture(SpellFamily.Water, water, waterSuccess, 0f);

            for (var index = 0; index < 3; index++)
            {
                store.RecordColdStartAttempt(
                    SpellFamily.Fire,
                    Offset(fire, new Vector2(index * 0.012f, -index * 0.008f)),
                    GestureRecognizer.Recognize(fire, SpellFamily.Fire),
                    index + 1f);
            }

            var current = ColdStartAmbiguousResult(SpellFamily.Water, 0.74f);
            store.ApplyBasePersonalization(current, fire, SpellFamily.Water);

            Assert.That(current.personalization.stage, Is.EqualTo("contrast_adjusted"));
            Assert.That(current.personalization.repeatedCaseContrastPenalty, Is.GreaterThan(0f));
            Assert.That(current.personalization.acceleratedByRepeatedCase, Is.False);
            Assert.That(current.personalization.decision, Is.Not.EqualTo(TutorialDynamicDecision.Accept));
            Assert.That(current.status, Is.EqualTo(RecognitionStatus.Ambiguous));
        }

        [Test]
        public void PersonalizationDoesNotPromoteInvalidInputToSuccess()
        {
            var store = new TutorialPersonalizationStore();
            var canonical = GestureRecognizer.CreateCanonicalSamples(SpellFamily.Fire, 1.6f, 0.03f);
            var success = GestureRecognizer.Recognize(canonical, SpellFamily.Fire);

            for (var index = 0; index < 3; index++)
            {
                store.RecordBaseCapture(SpellFamily.Fire, canonical, success, index);
            }

            var invalid = GestureRecognizer.Recognize(new List<List<StrokeSample>>(), SpellFamily.Fire);
            store.ApplyBasePersonalization(invalid, new List<List<StrokeSample>>());

            Assert.That(invalid.status, Is.EqualTo(RecognitionStatus.Invalid));
            Assert.That(invalid.success, Is.False);
            Assert.That(invalid.personalization.decision, Is.Not.EqualTo(TutorialDynamicDecision.Accept));
        }

        private static StrokeInputStroke MakeInputStroke(string id, float startTime)
        {
            return new StrokeInputStroke(id, new[]
            {
                new StrokeInputPoint(Vector2.zero, startTime),
                new StrokeInputPoint(Vector2.right, startTime + 0.1f)
            });
        }

        private static List<List<StrokeSample>> Offset(List<List<StrokeSample>> strokes, Vector2 offset)
        {
            return strokes
                .Select(stroke => stroke.Select(sample => new StrokeSample(sample.position + offset, sample.time)).ToList())
                .ToList();
        }

        private static SpellResult ColdStartAmbiguousResult(SpellFamily family, float confidence)
        {
            return new SpellResult
            {
                status = RecognitionStatus.Ambiguous,
                targetFamily = family,
                confidence = confidence,
                quality = StableQuality(),
                feedbackReason = "ambiguous cold-start",
                nextHint = "retry"
            };
        }

        private static SpellResult ColdStartInvalidResult(SpellFamily family, float confidence)
        {
            return new SpellResult
            {
                status = RecognitionStatus.Invalid,
                targetFamily = family,
                confidence = confidence,
                quality = StableQuality(),
                feedbackReason = "invalid cold-start",
                nextHint = "retry"
            };
        }

        private static SpellResult WrongRecognizedResult(SpellFamily recognizedFamily, float confidence)
        {
            return new SpellResult
            {
                status = RecognitionStatus.Recognized,
                recognizedFamily = recognizedFamily,
                targetFamily = recognizedFamily,
                confidence = confidence,
                quality = StableQuality(),
                success = true,
                feedbackReason = "wrong cold-start family",
                nextHint = "retry"
            };
        }

        private static QualityVector StableQuality()
        {
            return new QualityVector
            {
                closure = 0.86f,
                smoothness = 0.82f,
                tempo = 0.75f,
                stability = 0.8f,
                rotationBias = 0.05f
            };
        }

        private static List<List<StrokeSample>> OpenFireSamples()
        {
            return new List<List<StrokeSample>>
            {
                Polyline(
                    new Vector2(0f, -0.65f),
                    new Vector2(0.62f, 0.58f),
                    new Vector2(-0.62f, 0.58f),
                    new Vector2(-0.18f, -0.26f))
            };
        }

        private static List<List<StrokeSample>> EarthTrapezoidSamples()
        {
            return new List<List<StrokeSample>>
            {
                Polyline(
                    new Vector2(-0.25f, -0.32f),
                    new Vector2(0.25f, -0.32f),
                    new Vector2(0.42f, 0.28f),
                    new Vector2(-0.40f, 0.30f),
                    new Vector2(-0.25f, -0.32f))
            };
        }

        private static List<StrokeSample> Polyline(params Vector2[] points)
        {
            var samples = new List<StrokeSample>();
            var time = 0f;
            for (var index = 0; index < points.Length - 1; index++)
            {
                var start = points[index];
                var end = points[index + 1];
                for (var step = 0; step < 10; step++)
                {
                    var t = step / 9f;
                    samples.Add(new StrokeSample(Vector2.Lerp(start, end, t), time));
                    time += 0.03f;
                }
            }

            return samples;
        }
    }
}
