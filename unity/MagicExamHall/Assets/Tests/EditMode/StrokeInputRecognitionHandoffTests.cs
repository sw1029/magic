using System.Collections.Generic;
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
    }
}
