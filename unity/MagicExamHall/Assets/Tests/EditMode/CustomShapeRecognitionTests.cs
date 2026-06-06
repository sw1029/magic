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
        public void DefaultShapeTokensHaveEventDefinitions()
        {
            foreach (var token in CustomShapeProfileStore.HelperTokens)
            {
                var definition = CustomShapeEventCatalog.ForToken(token);

                Assert.That(definition.token, Is.EqualTo(token));
                Assert.That(definition.eventKind, Is.Not.EqualTo(CustomShapeEventKind.None));
                Assert.That(definition.eventId, Is.Not.Empty);
                Assert.That(definition.uiSummary, Does.Contain(":"));
            }

            Assert.That(CustomShapeEventCatalog.ForToken("arrow").role, Is.EqualTo(CustomShapeEventRole.Operator));
            Assert.That(CustomShapeEventCatalog.ForToken("cross").role, Is.EqualTo(CustomShapeEventRole.Operator));
        }

        [Test]
        public void CustomSpellEffectsResolveFromBaseAndIncludedShape()
        {
            AssertEffect(SpellFamily.Water, "hexagon", CustomShapeEventKind.Stun, CustomSpellEffectKind.Ice);
            AssertEffect(SpellFamily.Fire, "line", CustomShapeEventKind.SlashDamage, CustomSpellEffectKind.Electric);
            AssertEffect(SpellFamily.Water, "ellipse", CustomShapeEventKind.Barrier, CustomSpellEffectKind.Cleanse);
            AssertEffect(SpellFamily.Fire, "star", CustomShapeEventKind.MagicAmplify, CustomSpellEffectKind.Focus);
            AssertEffect(SpellFamily.Wind, "wave", CustomShapeEventKind.MoveSpeedBuff, CustomSpellEffectKind.Flow);
            AssertEffect(SpellFamily.Life, "brace", CustomShapeEventKind.AttackBuff, CustomSpellEffectKind.Connection);
            AssertEffect(SpellFamily.Earth, "rect", CustomShapeEventKind.WallEntity, CustomSpellEffectKind.Stability);
        }

        [Test]
        public void ArrowOperatorOnlyUsesEndpointDirectionAsAttributeLaser()
        {
            var strokes = LineStrokes(Vector2.zero, new Vector2(2f, 0f));

            var payload = CustomShapeEventCatalog.BuildPayload("arrow", strokes);

            Assert.That(payload.operatorOnly, Is.True);
            Assert.That(payload.eventKind, Is.EqualTo(CustomShapeEventKind.AttributeLaser));
            Assert.That(payload.usesDirection, Is.True);
            Assert.That(payload.emitsFromEndPoint, Is.True);
            Assert.That(payload.origin.x, Is.EqualTo(2f).Within(0.001f));
            Assert.That(payload.direction.x, Is.GreaterThan(0.99f));
            Assert.That(Mathf.Abs(payload.direction.y), Is.LessThan(0.01f));
        }

        [Test]
        public void CrossOperatorBlocksOverlappedTargetEvent()
        {
            var cross = LineStrokes(new Vector2(-1f, -1f), new Vector2(1f, 1f));
            var target = LineStrokes(new Vector2(-0.5f, 0f), new Vector2(0.5f, 0f));

            var payload = CustomShapeEventCatalog.ComposeWithOperator("cross", "star", cross, target, overlaps: true);

            Assert.That(payload.eventKind, Is.EqualTo(CustomShapeEventKind.EventBlock));
            Assert.That(payload.eventBlocked, Is.True);
            Assert.That(payload.blockedByToken, Is.EqualTo("cross"));
        }

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
        public void GeneratedBraceReferencePromotesLifeCustomCandidate()
        {
            var store = TempStore(out var path);
            try
            {
                var gold = BraceReferenceStrokes(Vector2.zero);
                Assert.That(
                    store.TrySaveSlot(0, "life brace", "life|brace", "brace", new[] { "brace" }, SpellFamily.Life, gold, out var message),
                    Is.True,
                    message);

                var strokes = BraceReferenceStrokes(new Vector2(5.4f, 2.55f));
                var baseResult = SpellRuntime.RecognizeBase(strokes, new BaseRecognitionIntent
                {
                    family = SpellFamily.Life,
                    goalId = "custom_life",
                    source = "test",
                    radius = 1f,
                    strength = 1f
                });

                var applied = CustomShapeRecognition.ApplyToBaseResult(baseResult, strokes, store, SpellFamily.Life);

                Assert.That(applied, Is.True);
                Assert.That(baseResult.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
                Assert.That(baseResult.spell.isCustomShape, Is.True, baseResult.spell.feedbackReason);
                Assert.That(baseResult.spell.customShapeToken, Is.EqualTo("brace"));
                Assert.That(baseResult.spell.recognizedFamily, Is.EqualTo(SpellFamily.Life));
                Assert.That(baseResult.spell.customScore, Is.GreaterThan(0.68f));
            }
            finally
            {
                DeleteIfExists(path);
            }
        }

        [Test]
        public void AcceptedCustomCandidateCarriesShapeEventMetadata()
        {
            var store = TempStore(out var path);
            try
            {
                var strokes = Samples(SpellFamily.Wind);
                var baseResult = SpellRuntime.RecognizeBase(strokes);
                var mappedFamily = baseResult.spell.recognizedFamily ?? baseResult.spell.targetFamily;
                Assert.That(store.TrySaveSlot(0, "stun hex", "stun|hexagon", "hexagon", mappedFamily, strokes, out _), Is.True);

                var applied = CustomShapeRecognition.ApplyToBaseResult(baseResult, strokes, store);

                Assert.That(applied, Is.True);
                Assert.That(baseResult.spell.isCustomShape, Is.True);
                Assert.That(baseResult.spell.customShapeToken, Is.EqualTo("hexagon"));
                Assert.That(baseResult.spell.customEventKind, Is.EqualTo(CustomShapeEventKind.Stun.ToString()));
                Assert.That(baseResult.spell.customEventLabel, Is.EqualTo("스턴"));
            }
            finally
            {
                DeleteIfExists(path);
            }
        }

        [Test]
        public void ArrowAndTargetTokenSequenceEmitsTargetEventFromArrowEndpoint()
        {
            var store = TempStore(out var path);
            try
            {
                var strokes = LineStrokes(Vector2.zero, new Vector2(2f, 0f));
                var baseResult = SpellRuntime.RecognizeBase(strokes);
                var mappedFamily = baseResult.spell.recognizedFamily ?? baseResult.spell.targetFamily;
                Assert.That(store.TrySaveSlot(0, "arrow wall", "arrow|rect|wall", "arrow", new[] { "arrow", "rect" }, mappedFamily, strokes, out _), Is.True);

                var applied = CustomShapeRecognition.ApplyToBaseResult(baseResult, strokes, store);

                Assert.That(applied, Is.True);
                Assert.That(baseResult.spell.customShapeToken, Is.EqualTo("arrow"));
                Assert.That(baseResult.spell.customEventKind, Is.EqualTo(CustomShapeEventKind.DirectionalProjectile.ToString()));
                Assert.That(baseResult.spell.customEventUsesDirection, Is.True);
                Assert.That(baseResult.spell.customEventOrigin.x, Is.EqualTo(2f).Within(0.001f));
                Assert.That(baseResult.spell.customEventDirection.x, Is.GreaterThan(0.99f));
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
                Assert.That(slot.eventShapeTokens, Does.Contain("line"));
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

        private static void AssertEffect(
            SpellFamily baseFamily,
            string token,
            CustomShapeEventKind eventKind,
            CustomSpellEffectKind expected)
        {
            var spell = new SpellResult
            {
                isCustomShape = true,
                customShapeToken = token,
                customEventId = $"{token}_{eventKind.ToString().ToLowerInvariant()}",
                customEventKind = eventKind.ToString()
            };

            var effect = CustomSpellEffectCatalog.Resolve(baseFamily, spell);

            Assert.That(effect.kind, Is.EqualTo(expected));
            Assert.That(effect.requirementLabel, Is.Not.Empty);
        }

        private static IReadOnlyList<IReadOnlyList<StrokeSample>> Samples(SpellFamily family)
        {
            return GestureRecognizer.CreateCanonicalSamples(family, 1.6f, 0.03f)
                .Select(stroke => (IReadOnlyList<StrokeSample>)stroke)
                .ToList();
        }

        private static IReadOnlyList<IReadOnlyList<StrokeSample>> LineStrokes(Vector2 start, Vector2 end)
        {
            return new List<IReadOnlyList<StrokeSample>>
            {
                new List<StrokeSample>
                {
                    new(start, 0f),
                    new(end, 0.1f)
                }
            };
        }

        private static IReadOnlyList<IReadOnlyList<StrokeSample>> BraceReferenceStrokes(Vector2 center)
        {
            var elapsed = 0f;
            var points = new[]
            {
                new Vector2(0.66f, 0.88f),
                new Vector2(0.42f, 0.76f),
                new Vector2(0.48f, 0.58f),
                new Vector2(0.30f, 0.50f),
                new Vector2(0.48f, 0.42f),
                new Vector2(0.42f, 0.24f),
                new Vector2(0.66f, 0.12f)
            };

            return new List<IReadOnlyList<StrokeSample>>
            {
                points
                    .Select(point =>
                    {
                        elapsed += 0.03f;
                        return new StrokeSample((point - new Vector2(0.5f, 0.5f)) * 1.6f + center, elapsed);
                    })
                    .ToList()
            };
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
