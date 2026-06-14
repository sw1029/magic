using System;
using System.Collections.Generic;
using System.Linq;
using MagicExamHall;
using NUnit.Framework;

namespace MagicExamHall.Tests
{
    public sealed class HintAssistanceTests
    {
        private static readonly SpellFamily[] AllFamilies =
        {
            SpellFamily.Fire,
            SpellFamily.Water,
            SpellFamily.Wind,
            SpellFamily.Earth,
            SpellFamily.Life
        };

        [Test]
        public void FirstSuccessWithoutPriorFailuresStaysSilent()
        {
            Assert.That(HintAssistance.ResolveLevel(0, success: true), Is.EqualTo(AssistLevel.None));

            var state = HintAssistance.ForAttempt(SpellFamily.Fire, 0, success: true, null);
            Assert.That(state.hintShown, Is.False);
            Assert.That(state.assisted, Is.False);
            Assert.That(state.AssistLevelNumber, Is.EqualTo(0));
        }

        [Test]
        public void FailuresEscalateOneLevelPerFailureAndCapAtGhostTrace()
        {
            Assert.That(HintAssistance.ResolveLevel(0, success: false), Is.EqualTo(AssistLevel.ReasonHint));
            Assert.That(HintAssistance.ResolveLevel(1, success: false), Is.EqualTo(AssistLevel.Checklist));
            Assert.That(HintAssistance.ResolveLevel(2, success: false), Is.EqualTo(AssistLevel.GhostTrace));
            Assert.That(HintAssistance.ResolveLevel(3, success: false), Is.EqualTo(AssistLevel.GhostTrace));
            Assert.That(HintAssistance.ResolveLevel(99, success: false), Is.EqualTo(AssistLevel.GhostTrace));
        }

        [Test]
        public void EscalatorNeverSkipsALevel()
        {
            var previous = (int)HintAssistance.ResolveLevel(0, success: false);
            for (var failures = 1; failures < 6; failures++)
            {
                var current = (int)HintAssistance.ResolveLevel(failures, success: false);
                Assert.That(current - previous, Is.InRange(0, 1),
                    $"failures={failures}: escalator skipped a level ({previous} -> {current})");
                previous = current;
            }
        }

        [Test]
        public void SuccessAfterFailuresKeepsAssistedFeedbackAtPriorLevel()
        {
            Assert.That(HintAssistance.ResolveLevel(1, success: true), Is.EqualTo(AssistLevel.ReasonHint));
            Assert.That(HintAssistance.ResolveLevel(2, success: true), Is.EqualTo(AssistLevel.Checklist));
            Assert.That(HintAssistance.ResolveLevel(5, success: true), Is.EqualTo(AssistLevel.GhostTrace));

            var state = HintAssistance.ForAttempt(SpellFamily.Water, 2, success: true, null);
            Assert.That(state.assisted, Is.True);
            Assert.That(state.currentLevel, Is.EqualTo(AssistLevel.Checklist));
            Assert.That(state.hintShown, Is.True);
        }

        [Test]
        public void HintShownFlagMatchesLevelForLogging()
        {
            foreach (var family in AllFamilies)
            {
                for (var failures = 0; failures <= 4; failures++)
                {
                    var preview = HintAssistance.PreviewFor(family, failures);
                    Assert.That(preview.hintShown, Is.EqualTo(preview.currentLevel != AssistLevel.None),
                        $"{family}: hintShown does not match level after {failures} failures");
                    if (preview.currentLevel != AssistLevel.None)
                    {
                        Assert.That(preview.body, Is.Not.Empty);
                    }
                }
            }
        }

        [Test]
        public void EveryFamilyHasThreeShortChecklistItems()
        {
            foreach (var family in AllFamilies)
            {
                var checklist = HintAssistance.ChecklistFor(family);
                Assert.That(checklist.Count, Is.EqualTo(3), $"{family} checklist must have three items");
                Assert.That(checklist.All(item => !string.IsNullOrWhiteSpace(item)), Is.True);
                Assert.That(checklist.All(item => item.Trim().Length <= 30), Is.True,
                    $"{family} checklist lines should stay concise");
            }
        }

        [Test]
        public void HintBodiesAreDistinctPerLevel()
        {
            foreach (var family in AllFamilies)
            {
                var reason = HintAssistance.ForAttempt(family, 0, success: false, null).body;
                var checklist = HintAssistance.ForAttempt(family, 1, success: false, null).body;
                var ghost = HintAssistance.ForAttempt(family, 2, success: false, null).body;

                Assert.That(reason, Is.Not.Empty);
                Assert.That(checklist, Is.Not.Empty);
                Assert.That(ghost, Is.Not.Empty);
                Assert.That(reason, Is.Not.EqualTo(checklist));
                Assert.That(checklist, Is.Not.EqualTo(ghost));
            }
        }

        [Test]
        public void ChecklistLevelBodyJoinsTheFamilyChecklist()
        {
            var state = HintAssistance.PreviewFor(SpellFamily.Earth, priorFailures: 2);

            Assert.That(state.currentLevel, Is.EqualTo(AssistLevel.Checklist));
            foreach (var item in HintAssistance.ChecklistFor(SpellFamily.Earth))
            {
                Assert.That(state.body, Does.Contain(item));
            }
        }

        [Test]
        public void ReasonHintPrefersRecognizerNextHintWhenAvailable()
        {
            var result = new SpellResult { nextHint = "끝점을 시작점에 더 가깝게 두세요." };
            var state = HintAssistance.ForAttempt(SpellFamily.Water, 0, success: false, result);

            Assert.That(state.currentLevel, Is.EqualTo(AssistLevel.ReasonHint));
            Assert.That(state.body, Is.EqualTo(result.nextHint));
        }

        [Test]
        public void PreviewMatchesFailureEscalationWithoutMutatingCounts()
        {
            foreach (var failures in new[] { 0, 1, 2, 3, 7 })
            {
                var preview = HintAssistance.PreviewFor(SpellFamily.Earth, failures);
                Assert.That((int)preview.currentLevel, Is.EqualTo(Math.Min(failures, 3)));
                Assert.That(preview.failureCount, Is.EqualTo(failures));
            }
        }

        [Test]
        public void MentorProfilesAreDistinctPerFloorAndFallBackToFloorOne()
        {
            var names = new HashSet<string>();
            for (var floor = 1; floor <= 5; floor++)
            {
                var profile = MentorProfile.ForFloor(floor);
                Assert.That(profile.name, Is.Not.Empty);
                Assert.That(names.Add(profile.name), Is.True, $"floor {floor} mentor name must be unique");
            }

            Assert.That(MentorProfile.ForFloor(99).name, Is.EqualTo(MentorProfile.ForFloor(1).name));
            Assert.That(MentorProfile.ForFloor(-1).name, Is.EqualTo(MentorProfile.ForFloor(1).name));
        }

        [Test]
        public void MentorProfileMapsEveryMoodToASprite()
        {
            for (var floor = 1; floor <= 5; floor++)
            {
                var profile = MentorProfile.ForFloor(floor);
                foreach (MentorMood mood in Enum.GetValues(typeof(MentorMood)))
                {
                    Assert.That((int)profile.KindFor(mood), Is.GreaterThanOrEqualTo(0));
                }

                Assert.That(profile.KindFor(MentorMood.Happy), Is.Not.EqualTo(profile.KindFor(MentorMood.Frown)),
                    $"floor {floor} mentor needs distinct happy/frown sprites");
            }
        }
    }
}
