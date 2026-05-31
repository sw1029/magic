using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MagicExamHall;
using NUnit.Framework;
using UnityEngine;

namespace MagicExamHall.Tests
{
    public sealed class CustomShapeRecognitionTests
    {
        [Test]
        public void InvalidRegexIsRejected()
        {
            var store = TempStore(out var path);
            try
            {
                var saved = store.TrySaveSlot(0, "star", "(", SpellFamily.Wind, Samples(SpellFamily.Wind), out var message);

                Assert.That(saved, Is.False);
                Assert.That(message, Does.Contain("정규식"));
            }
            finally
            {
                DeleteIfExists(path);
            }
        }

        [Test]
        public void RegexMustMatchLabelOrSupportedToken()
        {
            var store = TempStore(out var path);
            try
            {
                var saved = store.TrySaveSlot(0, "spiral", "nomatch", SpellFamily.Wind, Samples(SpellFamily.Wind), out var message);

                Assert.That(saved, Is.False);
                Assert.That(message, Does.Contain("매칭"));
            }
            finally
            {
                DeleteIfExists(path);
            }
        }

        [Test]
        public void GoldCaptureIsRequiredForOccupiedSlot()
        {
            var store = TempStore(out var path);
            try
            {
                var saved = store.TrySaveSlot(0, "freeform", "freeform", SpellFamily.Wind, new List<IReadOnlyList<StrokeSample>>(), out var message);

                Assert.That(saved, Is.False);
                Assert.That(message, Does.Contain("gold capture"));
                Assert.That(store.IsSlotOccupied(0), Is.False);
            }
            finally
            {
                DeleteIfExists(path);
            }
        }

        [Test]
        public void SlotWithoutGoldCaptureIsNotRecognitionCandidate()
        {
            var store = TempStore(out var path);
            try
            {
                var slot = store.GetSlot(0);
                slot.shapeId = "custom-slot-01";
                slot.label = "ghost";
                slot.regexPattern = "ghost";
                slot.mappedFamily = SpellFamily.Wind;
                var strokes = Samples(SpellFamily.Wind);
                var baseResult = SpellRuntime.RecognizeBase(strokes);

                var custom = CustomShapeRecognition.Recognize(strokes, baseResult, store);

                Assert.That(slot.IsOccupied, Is.False);
                Assert.That(custom, Is.Null);
            }
            finally
            {
                DeleteIfExists(path);
            }
        }

        [Test]
        public void SimilarGoldCapturePromotesCustomCandidate()
        {
            var store = TempStore(out var path);
            try
            {
                var strokes = Samples(SpellFamily.Wind);
                Assert.That(store.TrySaveSlot(0, "gale line", "gale|line", SpellFamily.Wind, strokes, out _), Is.True);
                var baseResult = SpellRuntime.RecognizeBase(strokes);

                var applied = CustomShapeRecognition.ApplyToBaseResult(baseResult, strokes, store);

                Assert.That(applied, Is.True);
                Assert.That(baseResult.spell.isCustomShape, Is.True);
                Assert.That(baseResult.spell.customShapeId, Is.EqualTo("custom-slot-01"));
                Assert.That(baseResult.spell.customShapeLabel, Is.EqualTo("gale line"));
                Assert.That(baseResult.spell.recognizedFamily, Is.EqualTo(SpellFamily.Wind));
                Assert.That(baseResult.spell.mappedFamily, Is.EqualTo(SpellFamily.Wind));
                Assert.That(baseResult.spell.customScore, Is.GreaterThan(0.7f));
            }
            finally
            {
                DeleteIfExists(path);
            }
        }

        [Test]
        public void ShadowGateHoldsWhenMappedFamilyConflictsWithStrongDefault()
        {
            var store = TempStore(out var path);
            try
            {
                var strokes = Samples(SpellFamily.Wind);
                Assert.That(store.TrySaveSlot(0, "red wind", "red|wind|line", SpellFamily.Fire, strokes, out _), Is.True);
                var baseResult = SpellRuntime.RecognizeBase(strokes);

                var applied = CustomShapeRecognition.ApplyToBaseResult(baseResult, strokes, store);

                Assert.That(applied, Is.True);
                Assert.That(baseResult.spell.isCustomShape, Is.True);
                Assert.That(baseResult.spell.status, Is.EqualTo(RecognitionStatus.Incomplete));
                Assert.That(baseResult.spell.success, Is.False);
                Assert.That(baseResult.spell.recognizedFamily, Is.Null);
                Assert.That(baseResult.spell.mappedFamily, Is.EqualTo(SpellFamily.Fire));
                Assert.That(baseResult.spell.defaultSimilarityScore, Is.GreaterThan(0.78f));
            }
            finally
            {
                DeleteIfExists(path);
            }
        }

        [Test]
        public void AcceptedCustomCaptureAdjustsThresholdSummary()
        {
            var store = TempStore(out var path);
            try
            {
                var strokes = Samples(SpellFamily.Water);
                Assert.That(store.TrySaveSlot(0, "round pool", "round|ellipse|pool", SpellFamily.Water, strokes, out _), Is.True);
                var baseResult = SpellRuntime.RecognizeBase(strokes);
                var before = CustomShapeRecognition.Recognize(strokes, baseResult, store);

                Assert.That(store.RecordAutoCapture("custom-slot-01", strokes, 0.91f), Is.True);
                var after = CustomShapeRecognition.Recognize(strokes, SpellRuntime.RecognizeBase(strokes), store);

                Assert.That(after, Is.Not.Null);
                Assert.That(after.summary.targetSampleCount, Is.EqualTo(1));
                Assert.That(after.acceptThreshold, Is.LessThan(before.acceptThreshold));
            }
            finally
            {
                DeleteIfExists(path);
            }
        }

        [Test]
        public void JsonSaveLoadKeepsSlotsRegexMappingAndCaptures()
        {
            var store = TempStore(out var path);
            try
            {
                var strokes = Samples(SpellFamily.Life);
                Assert.That(store.TrySaveSlot(3, "branch mark", "branch|freeform", SpellFamily.Life, strokes, out _), Is.True);
                Assert.That(store.RecordAutoCapture("custom-slot-04", strokes, 0.88f), Is.True);

                var loaded = CustomShapeProfileStore.LoadFromPath(path);
                var slot = loaded.GetSlot(3);

                Assert.That(loaded.Slots.Count, Is.EqualTo(CustomShapeProfileStore.SlotCount));
                Assert.That(slot.IsOccupied, Is.True);
                Assert.That(slot.label, Is.EqualTo("branch mark"));
                Assert.That(slot.regexPattern, Is.EqualTo("branch|freeform"));
                Assert.That(slot.mappedFamily, Is.EqualTo(SpellFamily.Life));
                Assert.That(slot.goldCaptures.Count, Is.EqualTo(1));
                Assert.That(slot.autoCaptures.Count, Is.EqualTo(1));
                Assert.That(slot.goldCaptures[0].ToStrokeSamples().Count, Is.GreaterThan(0));
            }
            finally
            {
                DeleteIfExists(path);
            }
        }

        private static CustomShapeProfileStore TempStore(out string path)
        {
            path = Path.Combine(Path.GetTempPath(), $"magic-custom-shapes-{Guid.NewGuid():N}.json");
            return new CustomShapeProfileStore(path);
        }

        private static IReadOnlyList<IReadOnlyList<StrokeSample>> Samples(SpellFamily family)
        {
            return GestureRecognizer.CreateCanonicalSamples(family, 1.6f, 0.03f)
                .Select(stroke => (IReadOnlyList<StrokeSample>)stroke)
                .ToList();
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
