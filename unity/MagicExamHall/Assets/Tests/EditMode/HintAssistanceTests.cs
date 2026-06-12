using System;
using MagicExamHall;
using NUnit.Framework;

namespace MagicExamHall.Tests
{
    public sealed class HintAssistanceTests
    {
        [Test]
        public void FirstFailureStartsAtReasonHintAndEscalatesToGhostTraceCap()
        {
            Assert.That(HintAssistance.ResolveLevel(priorFailures: 0, success: false), Is.EqualTo(AssistLevel.ReasonHint));
            Assert.That(HintAssistance.ResolveLevel(priorFailures: 1, success: false), Is.EqualTo(AssistLevel.Checklist));
            Assert.That(HintAssistance.ResolveLevel(priorFailures: 2, success: false), Is.EqualTo(AssistLevel.GhostTrace));
            Assert.That(HintAssistance.ResolveLevel(priorFailures: 7, success: false), Is.EqualTo(AssistLevel.GhostTrace), "escalator must cap at GhostTrace");
        }

        [Test]
        public void CleanSuccessNeverShowsAssistance()
        {
            Assert.That(HintAssistance.ResolveLevel(priorFailures: 0, success: true), Is.EqualTo(AssistLevel.None));

            var state = HintAssistance.ForAttempt(SpellFamily.Fire, priorFailures: 0, success: true, result: null);
            Assert.That(state.hintShown, Is.False);
            Assert.That(state.assisted, Is.False);
            Assert.That(state.currentLevel, Is.EqualTo(AssistLevel.None));
        }

        [Test]
        public void SuccessAfterFailuresKeepsAssistedFlagAndReachedLevel()
        {
            var state = HintAssistance.ForAttempt(SpellFamily.Water, priorFailures: 2, success: true, result: null);

            Assert.That(state.assisted, Is.True, "a success that needed prior failures counts as assisted");
            Assert.That(state.currentLevel, Is.EqualTo(AssistLevel.Checklist));
            Assert.That(state.hintShown, Is.True);
        }

        [Test]
        public void HintShownAlwaysMatchesLevel()
        {
            foreach (SpellFamily family in Enum.GetValues(typeof(SpellFamily)))
            {
                for (var failures = 0; failures <= 4; failures++)
                {
                    var state = HintAssistance.PreviewFor(family, failures);
                    Assert.That(state.hintShown, Is.EqualTo(state.currentLevel != AssistLevel.None),
                        $"hintShown must mirror level for {family} with {failures} failures");
                    Assert.That(state.body, Is.Not.Empty, $"every hint state needs body text ({family}, {failures})");
                }
            }
        }

        [Test]
        public void EveryFamilyHasAThreeItemChecklistWithActionVerbs()
        {
            foreach (SpellFamily family in Enum.GetValues(typeof(SpellFamily)))
            {
                var checklist = HintAssistance.ChecklistFor(family);
                Assert.That(checklist.Count, Is.EqualTo(3), $"{family} checklist must have 3 steps");
                foreach (var item in checklist)
                {
                    Assert.That(item.Trim(), Is.Not.Empty);
                    Assert.That(item.Trim().Length, Is.LessThanOrEqualTo(30), $"checklist lines stay short: '{item}'");
                }
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
        public void MentorProfilesAreDistinctPerFloorAndFallBackToFloorOne()
        {
            var names = new System.Collections.Generic.HashSet<string>();
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
