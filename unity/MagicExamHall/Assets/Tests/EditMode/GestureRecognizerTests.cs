using System.Collections.Generic;
using System.Linq;
using MagicExamHall;
using NUnit.Framework;
using UnityEngine;

namespace MagicExamHall.Tests
{
    public sealed class GestureRecognizerTests
    {
        [TestCase(SpellFamily.Wind)]
        [TestCase(SpellFamily.Earth)]
        [TestCase(SpellFamily.Fire)]
        [TestCase(SpellFamily.Water)]
        [TestCase(SpellFamily.Life)]
        public void CanonicalSamplesRecognizeTheirFamilies(SpellFamily family)
        {
            var strokes = GestureRecognizer.CreateCanonicalSamples(family);
            var result = GestureRecognizer.Recognize(strokes, family);

            Assert.That(result.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(result.recognizedFamily, Is.EqualTo(family));
            Assert.That(result.success, Is.True);
            Assert.That(result.confidence, Is.GreaterThan(0.7f));
        }

        [TestCase(OverlayOperator.SteelBrace)]
        [TestCase(OverlayOperator.ElectricFork)]
        [TestCase(OverlayOperator.IceBar)]
        [TestCase(OverlayOperator.SoulDot)]
        [TestCase(OverlayOperator.VoidCut)]
        [TestCase(OverlayOperator.MartialAxis)]
        public void CanonicalOverlaySamplesRecognizeTheirOperators(OverlayOperator op)
        {
            var seal = CreateWorldSeal(op == OverlayOperator.MartialAxis ? new[] { OverlayOperator.VoidCut } : new OverlayOperator[0]);
            var strokes = OverlayRecognizer.CreateCanonicalSamples(op, seal.worldCenter, seal.worldScale * 0.24f);
            var result = OverlayRecognizer.Recognize(strokes, seal);

            Assert.That(
                result.status,
                Is.EqualTo(RecognitionStatus.Recognized),
                $"score={result.score:0.000}, shape={result.shapeConfidence:0.000}, scale={result.scaleRatio:0.000}, anchor={result.anchorZone}, reason={result.feedbackReason}");
            Assert.That(result.recognizedOperator, Is.EqualTo(op));
            Assert.That(result.success, Is.True);
            Assert.That(result.shapeConfidence, Is.GreaterThan(0.48f));
        }

        [Test]
        public void MartialAxisRequiresVoidCutInSealStack()
        {
            var seal = CreateWorldSeal();
            var strokes = OverlayRecognizer.CreateCanonicalSamples(OverlayOperator.MartialAxis, seal.worldCenter, seal.worldScale * 0.24f);
            var result = OverlayRecognizer.Recognize(strokes, seal);

            Assert.That(result.status, Is.Not.EqualTo(RecognitionStatus.Recognized));
            Assert.That(result.success, Is.False);
            Assert.That(result.recognizedOperator, Is.EqualTo(OverlayOperator.MartialAxis));
            Assert.That(result.feedbackReason, Does.Contain("절단").And.Contain("void_cut"));
        }

        [Test]
        public void DefaultSealLifetimeLeavesOverlaySetupTime()
        {
            var seal = CreateWorldSeal();

            Assert.That(seal.expiresAt - seal.createdAt, Is.EqualTo(SpellRuntime.DefaultSealDurationSeconds).Within(0.001f));
            Assert.That(SpellRuntime.DefaultSealDurationSeconds, Is.GreaterThanOrEqualTo(10f));
        }

        [Test]
        public void OpenTriangleIsIncompleteInsteadOfFalsePositive()
        {
            var stroke = new List<StrokeSample>
            {
                new(new Vector2(220, 70), 0f),
                new(new Vector2(400, 390), 0.12f),
                new(new Vector2(80, 390), 0.24f)
            };

            var result = GestureRecognizer.Recognize(new List<List<StrokeSample>> { stroke }, SpellFamily.Fire);

            Assert.That(result.status, Is.Not.EqualTo(RecognitionStatus.Recognized));
            Assert.That(result.success, Is.False);
            Assert.That(result.feedbackReason, Does.Contain("불꽃").And.Contain("틈"));
            Assert.That(result.nextHint, Does.Contain("마지막 선").And.Contain("삼각형"));
        }

        [Test]
        public void TwoLineWindIsIncomplete()
        {
            var strokes = new List<List<StrokeSample>>
            {
                MakeLine(70, 150, 390, 145, 0f),
                MakeLine(70, 240, 390, 235, 0.2f)
            };

            var result = GestureRecognizer.Recognize(strokes, SpellFamily.Wind);

            Assert.That(result.status, Is.EqualTo(RecognitionStatus.Incomplete).Or.EqualTo(RecognitionStatus.Ambiguous));
            Assert.That(result.success, Is.False);
            Assert.That(result.feedbackReason, Does.Contain("3획").And.Contain("위, 가운데, 아래"));
            Assert.That(result.nextHint, Does.Contain("세 번").And.Contain("평행선"));
        }

        [Test]
        public void UnevenWindLinesExplainSpacing()
        {
            var strokes = new List<List<StrokeSample>>
            {
                MakeLine(70, 120, 390, 118, 0f),
                MakeLine(70, 150, 390, 148, 0.2f),
                MakeLine(70, 330, 390, 328, 0.4f)
            };

            var result = GestureRecognizer.Recognize(strokes, SpellFamily.Wind);

            Assert.That(result.success, Is.False);
            Assert.That(result.feedbackReason, Does.Contain("간격"));
            Assert.That(result.nextHint, Does.Contain("간격").And.Contain("비슷"));
        }

        [Test]
        public void ExtraWindStrokeRemainsIncompleteWithActionHint()
        {
            var strokes = new List<List<StrokeSample>>
            {
                MakeLine(70, 120, 390, 118, 0f),
                MakeLine(70, 190, 390, 188, 0.2f),
                MakeLine(70, 260, 390, 258, 0.4f),
                MakeLine(70, 330, 390, 328, 0.6f)
            };

            var result = GestureRecognizer.Recognize(strokes, SpellFamily.Wind);

            Assert.That(result.status, Is.EqualTo(RecognitionStatus.Incomplete));
            Assert.That(result.success, Is.False);
            Assert.That(result.feedbackReason, Does.Contain("세 줄").And.Contain("획이 많"));
            Assert.That(result.nextHint, Does.Contain("추가 선").And.Contain("3획"));
        }

        [Test]
        public void LifeFailureDistinguishesStemAndBranches()
        {
            var strokes = new List<List<StrokeSample>>
            {
                MakeLine(220, 80, 220, 360, 0f)
            };

            var result = GestureRecognizer.Recognize(strokes, SpellFamily.Life);

            Assert.That(result.success, Is.False);
            Assert.That(result.feedbackReason, Does.Contain("줄기").And.Contain("가지"));
            Assert.That(result.nextHint, Does.Contain("가운데 줄기").And.Contain("왼쪽 가지").And.Contain("오른쪽 가지"));
        }

        [Test]
        public void SuccessfulLifeKeepsPositiveNextHint()
        {
            var result = GestureRecognizer.Recognize(GestureRecognizer.CreateCanonicalSamples(SpellFamily.Life), SpellFamily.Life);

            Assert.That(result.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(result.success, Is.True);
            Assert.That(result.nextHint, Does.Contain("좋습니다"));
            Assert.That(result.nextHint, Does.Not.Contain("가지가 갈라지게"));
        }

        [Test]
        public void EmptyBaseFailureUsesPlayerFacingCopy()
        {
            var result = GestureRecognizer.Recognize(new List<List<StrokeSample>>(), SpellFamily.Water);

            Assert.That(result.success, Is.False);
            Assert.That(result.feedbackReason, Does.Contain("바닥").And.Contain("선"));
            Assert.That(result.nextHint, Does.Contain("오른쪽 마우스"));
            Assert.That(result.feedbackReason, Does.Not.Contain("No stroke"));
        }

        [Test]
        public void FastAndSlowFireKeepFamilyButChangeTempo()
        {
            var fast = GestureRecognizer.CreateCanonicalSamples(SpellFamily.Fire, timeStep: 0.01f);
            var slow = GestureRecognizer.CreateCanonicalSamples(SpellFamily.Fire, timeStep: 0.08f);

            var fastResult = GestureRecognizer.Recognize(fast, SpellFamily.Fire);
            var slowResult = GestureRecognizer.Recognize(slow, SpellFamily.Fire);

            Assert.That(fastResult.recognizedFamily, Is.EqualTo(SpellFamily.Fire));
            Assert.That(slowResult.recognizedFamily, Is.EqualTo(SpellFamily.Fire));
            Assert.That(fastResult.quality.tempo, Is.GreaterThan(slowResult.quality.tempo + 0.12f));
        }

        [Test]
        public void LoggerWritesAttemptAndSurveyFiles()
        {
            var sessionId = "test-session-" + System.Guid.NewGuid().ToString("N");
            var logger = new ExamLogger(sessionId);
            logger.LogAttempt(new AttemptLog
            {
                sessionId = sessionId,
                trialId = "1-1",
                targetFamily = "fire",
                recognizedFamily = "fire",
                status = RecognitionStatus.Recognized.ToString(),
                confidence = 0.9f,
                closure = 1f,
                smoothness = 0.8f,
                tempo = 0.7f,
                stability = 0.9f,
                rotationBias = 0.1f,
                attemptIndex = 1,
                elapsedMs = 1200,
                feedbackViewed = true,
                success = true,
                hintShown = true,
                assistLevel = 2,
                assisted = true
            });
            logger.LogSurvey(new SurveyLog
            {
                sessionId = sessionId,
                clarity = 4,
                fairness = 4,
                feedbackHelpfulness = 5,
                controlFeeling = 4,
                immersion = 5,
                comment = "clear",
                completedTrials = 5,
                totalAttempts = 6
            });

            Assert.That(System.IO.File.Exists(System.IO.Path.Combine(logger.OutputDirectory, "attempts.csv")), Is.True);
            Assert.That(System.IO.File.Exists(System.IO.Path.Combine(logger.OutputDirectory, "survey.csv")), Is.True);
            var attemptsCsv = System.IO.File.ReadAllText(System.IO.Path.Combine(logger.OutputDirectory, "attempts.csv"));
            Assert.That(attemptsCsv, Does.Contain("phase,baseFamily,overlayStack,sealId,floorId,targetObject,worldEffect"));
            Assert.That(attemptsCsv, Does.Contain("hintShown,assistLevel,assisted"));
            AssertLastAttemptCsvFields(logger, success: true, hintShown: true, assistLevel: 2, assisted: true);
        }

        [Test]
        public void RepeatedFailuresEscalateAssistLevel()
        {
            var failedResult = GestureRecognizer.Recognize(new List<List<StrokeSample>>(), SpellFamily.Fire);

            var firstFailure = HintAssistance.ForAttempt(SpellFamily.Fire, 0, false, failedResult);
            var secondFailure = HintAssistance.ForAttempt(SpellFamily.Fire, 1, false, failedResult);
            var thirdFailure = HintAssistance.ForAttempt(SpellFamily.Fire, 2, false, failedResult);
            var laterFailure = HintAssistance.ForAttempt(SpellFamily.Fire, 5, false, failedResult);

            Assert.That(firstFailure.currentLevel, Is.EqualTo(AssistLevel.ReasonHint));
            Assert.That(secondFailure.currentLevel, Is.EqualTo(AssistLevel.Checklist));
            Assert.That(thirdFailure.currentLevel, Is.EqualTo(AssistLevel.GhostTrace));
            Assert.That(laterFailure.currentLevel, Is.EqualTo(AssistLevel.GhostTrace));
        }

        [TestCase(0, AssistLevel.ReasonHint, false)]
        [TestCase(1, AssistLevel.Checklist, true)]
        [TestCase(2, AssistLevel.GhostTrace, true)]
        [TestCase(7, AssistLevel.GhostTrace, true)]
        public void FailureEscalationStateCarriesStableMetadata(int priorFailures, AssistLevel expectedLevel, bool expectedAssisted)
        {
            var failedResult = GestureRecognizer.Recognize(new List<List<StrokeSample>>(), SpellFamily.Wind);
            var state = HintAssistance.ForAttempt(SpellFamily.Wind, priorFailures, false, failedResult);

            Assert.That(state.family, Is.EqualTo(SpellFamily.Wind));
            Assert.That(state.failureCount, Is.EqualTo(priorFailures));
            Assert.That(state.currentLevel, Is.EqualTo(expectedLevel));
            Assert.That(state.AssistLevelNumber, Is.EqualTo((int)expectedLevel));
            Assert.That(state.hintShown, Is.True);
            Assert.That(state.assisted, Is.EqualTo(expectedAssisted));
            Assert.That(state.body, Is.Not.Empty);

            if (expectedLevel == AssistLevel.ReasonHint)
            {
                Assert.That(state.body, Is.EqualTo(failedResult.nextHint));
            }
            else if (expectedLevel == AssistLevel.Checklist)
            {
                foreach (var checklistItem in HintAssistance.ChecklistFor(SpellFamily.Wind))
                {
                    Assert.That(state.body, Does.Contain(checklistItem));
                }
            }
            else
            {
                Assert.That(state.body, Is.Not.EqualTo(failedResult.nextHint));
                Assert.That(state.body, Does.Not.Contain(" · "));
            }
        }

        [TestCase(0, AssistLevel.None, false, false)]
        [TestCase(1, AssistLevel.ReasonHint, true, true)]
        [TestCase(2, AssistLevel.Checklist, true, true)]
        [TestCase(5, AssistLevel.GhostTrace, true, true)]
        public void SuccessAssistStateReflectsPriorFailures(int priorFailures, AssistLevel expectedLevel, bool expectedHintShown, bool expectedAssisted)
        {
            var successfulResult = GestureRecognizer.Recognize(GestureRecognizer.CreateCanonicalSamples(SpellFamily.Life), SpellFamily.Life);
            var state = HintAssistance.ForAttempt(SpellFamily.Life, priorFailures, true, successfulResult);

            Assert.That(state.currentLevel, Is.EqualTo(expectedLevel));
            Assert.That(state.hintShown, Is.EqualTo(expectedHintShown));
            Assert.That(state.assisted, Is.EqualTo(expectedAssisted));
            Assert.That(state.failureCount, Is.EqualTo(priorFailures));
            Assert.That(state.body, Is.Not.Empty);
        }

        [Test]
        public void ReasonHintFallsBackWhenRecognitionHintIsMissing()
        {
            var resultWithoutHint = new SpellResult
            {
                status = RecognitionStatus.Invalid,
                targetFamily = SpellFamily.Earth,
                nextHint = ""
            };

            var state = HintAssistance.ForAttempt(SpellFamily.Earth, 0, false, resultWithoutHint);

            Assert.That(state.currentLevel, Is.EqualTo(AssistLevel.ReasonHint));
            Assert.That(state.body, Does.Contain("사다리꼴"));
        }

        [TestCase(SpellFamily.Fire, "삼각형")]
        [TestCase(SpellFamily.Water, "원")]
        [TestCase(SpellFamily.Wind, "3획")]
        [TestCase(SpellFamily.Earth, "사다리꼴")]
        [TestCase(SpellFamily.Life, "가지")]
        public void RepeatedFailureCopyEscalatesWithFamilySpecificActions(SpellFamily family, string expectedWord)
        {
            var failedResult = GestureRecognizer.Recognize(new List<List<StrokeSample>>(), family);

            var checklist = HintAssistance.ForAttempt(family, 1, false, failedResult);
            var strong = HintAssistance.ForAttempt(family, 2, false, failedResult);

            Assert.That(checklist.body, Does.Contain(expectedWord));
            Assert.That(strong.body, Does.Contain(expectedWord));
            Assert.That(checklist.body, Is.Not.EqualTo(strong.body));
            Assert.That(checklist.body, Does.Not.Contain("closure"));
            Assert.That(checklist.body, Does.Not.Contain("Incomplete"));
            Assert.That(checklist.body, Does.Not.Contain("Invalid"));
            Assert.That(strong.body, Does.Not.Contain("closure"));
            Assert.That(strong.body, Does.Not.Contain("Incomplete"));
            Assert.That(strong.body, Does.Not.Contain("Invalid"));
        }

        [Test]
        public void SuccessAfterAssistIsLoggedAsAssisted()
        {
            var successfulResult = GestureRecognizer.Recognize(GestureRecognizer.CreateCanonicalSamples(SpellFamily.Fire), SpellFamily.Fire);
            var hintState = HintAssistance.ForAttempt(SpellFamily.Fire, 2, true, successfulResult);

            Assert.That(hintState.assisted, Is.True);
            Assert.That(hintState.hintShown, Is.True);
            Assert.That(hintState.currentLevel, Is.EqualTo(AssistLevel.Checklist));

            var sessionId = "assist-success-" + System.Guid.NewGuid().ToString("N");
            var logger = new ExamLogger(sessionId);
            logger.LogAttempt(new AttemptLog
            {
                sessionId = sessionId,
                trialId = "1-3",
                targetFamily = "fire",
                recognizedFamily = "fire",
                status = successfulResult.status.ToString(),
                confidence = successfulResult.confidence,
                closure = successfulResult.quality.closure,
                smoothness = successfulResult.quality.smoothness,
                tempo = successfulResult.quality.tempo,
                stability = successfulResult.quality.stability,
                rotationBias = successfulResult.quality.rotationBias,
                attemptIndex = 3,
                elapsedMs = 3000,
                feedbackViewed = true,
                success = true,
                hintShown = hintState.hintShown,
                assistLevel = hintState.AssistLevelNumber,
                assisted = hintState.assisted
            });

            AssertLastAttemptCsvFields(logger, success: true, hintShown: true, assistLevel: 2, assisted: true);
        }

        [Test]
        public void PreviewBeforeFailureDoesNotShowHint()
        {
            var preview = HintAssistance.PreviewFor(SpellFamily.Water, 0);

            Assert.That(preview.currentLevel, Is.EqualTo(AssistLevel.None));
            Assert.That(preview.hintShown, Is.False);
            Assert.That(preview.assisted, Is.False);
        }

        private static List<StrokeSample> MakeLine(float x1, float y1, float x2, float y2, float start)
        {
            return Enumerable.Range(0, 12)
                .Select(index =>
                {
                    var t = index / 11f;
                    return new StrokeSample(new Vector2(Mathf.Lerp(x1, x2, t), Mathf.Lerp(y1, y2, t)), start + index * 0.02f);
                })
                .ToList();
        }

        private static CompiledSeal CreateWorldSeal(params OverlayOperator[] overlays)
        {
            var baseSamples = Offset(GestureRecognizer.CreateCanonicalSamples(SpellFamily.Fire, 1.6f, 0.03f), Vector2.zero, 0.8f);
            var baseResult = SpellRuntime.RecognizeBase(baseSamples);
            var seal = SpellRuntime.CreateSeal(baseResult, 0f);
            foreach (var op in overlays)
            {
                seal.overlayStack.Add(op);
            }

            return seal;
        }

        private static List<List<StrokeSample>> Offset(List<List<StrokeSample>> strokes, Vector2 center, float canonicalCenter)
        {
            return strokes
                .Select(stroke => stroke.Select(sample => new StrokeSample(sample.position - Vector2.one * canonicalCenter + center, sample.time)).ToList())
                .ToList();
        }

        private static void AssertLastAttemptCsvFields(ExamLogger logger, bool success, bool hintShown, int assistLevel, bool assisted)
        {
            var csvPath = System.IO.Path.Combine(logger.OutputDirectory, "attempts.csv");
            var lastRow = System.IO.File.ReadAllLines(csvPath).Last();
            var fields = lastRow.Split(',');

            Assert.That(fields[^4], Is.EqualTo(success ? "true" : "false"));
            Assert.That(fields[^3], Is.EqualTo(hintShown ? "true" : "false"));
            Assert.That(fields[^2], Is.EqualTo(assistLevel.ToString()));
            Assert.That(fields[^1], Is.EqualTo(assisted ? "true" : "false"));
        }
    }
}
