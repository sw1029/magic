using System;
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
                    $"failures={failures}에서 단계가 건너뛰었습니다: {previous} -> {current}");
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
            Assert.That(state.hintShown, Is.True);
        }

        [Test]
        public void HintShownFlagMatchesLevelForLogging()
        {
            foreach (var family in AllFamilies)
            {
                var silent = HintAssistance.ForAttempt(family, 0, success: true, null);
                Assert.That(silent.hintShown, Is.False, $"{family}: 무실패 성공에 hintShown이 켜졌습니다");

                var escalated = HintAssistance.ForAttempt(family, 1, success: false, null);
                Assert.That(escalated.hintShown, Is.True, $"{family}: 실패 후에도 hintShown이 꺼져 있습니다");
                Assert.That(escalated.AssistLevelNumber, Is.EqualTo((int)escalated.currentLevel));
            }
        }

        [Test]
        public void EveryFamilyHasThreeItemChecklist()
        {
            foreach (var family in AllFamilies)
            {
                var checklist = HintAssistance.ChecklistFor(family);
                Assert.That(checklist.Count, Is.EqualTo(3), $"{family} 체크리스트가 3항목이 아닙니다");
                Assert.That(checklist.All(item => !string.IsNullOrWhiteSpace(item)), Is.True,
                    $"{family} 체크리스트에 빈 항목이 있습니다");
            }
        }

        [Test]
        public void HintBodiesAreDistinctPerLevelAndNeverRevealOnFirstFailure()
        {
            foreach (var family in AllFamilies)
            {
                var reason = HintAssistance.ForAttempt(family, 0, success: false, null).body;
                var checklist = HintAssistance.ForAttempt(family, 1, success: false, null).body;
                var ghost = HintAssistance.ForAttempt(family, 2, success: false, null).body;

                Assert.That(reason, Is.Not.Empty);
                Assert.That(checklist, Is.Not.Empty);
                Assert.That(ghost, Is.Not.Empty);
                Assert.That(reason, Is.Not.EqualTo(checklist), $"{family}: 1단계와 2단계 힌트가 동일합니다");
                Assert.That(checklist, Is.Not.EqualTo(ghost), $"{family}: 2단계와 3단계 힌트가 동일합니다");
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
    }
}
