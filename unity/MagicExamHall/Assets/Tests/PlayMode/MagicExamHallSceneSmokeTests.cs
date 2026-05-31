using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MagicExamHall;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MagicExamHall.Tests
{
    public sealed class MagicExamHallSceneSmokeTests
    {
        [UnityTest]
        public IEnumerator SceneLoadsWithWorldCastingGameObjects()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();

            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.FloorCount, Is.EqualTo(5));
            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(1));
            Assert.That(controller.ActiveGoalCount, Is.EqualTo(5));
            Assert.That(Object.FindFirstObjectByType<Canvas>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<EventSystem>(), Is.Not.Null);
            Assert.That(Camera.main, Is.Not.Null);
            Assert.That(Camera.main.clearFlags, Is.EqualTo(CameraClearFlags.SolidColor));
            Assert.That(Camera.main.orthographicSize, Is.EqualTo(ExamGameController.GameplayCameraOrthographicSize).Within(0.001f));
            var drawing = Object.FindFirstObjectByType<WorldDrawingController>();
            Assert.That(drawing, Is.Not.Null);
            Assert.That(drawing.bufferSeconds, Is.EqualTo(WorldDrawingController.DefaultBufferSeconds).Within(0.001f));
            Assert.That(drawing.minPointDistance, Is.EqualTo(WorldDrawingController.DefaultMinPointDistance).Within(0.001f));
            Assert.That(controller.OutputDirectory, Does.Contain("MagicExamHallLogs"));
        }

        [UnityTest]
        public IEnumerator FirstFloorShowsGoalLabelsAndDrawingLocationGuidance()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);

            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(1));
            Assert.That(controller.VisibleGoalLabelCountForTests, Is.EqualTo(controller.ActiveGoalCount));
            Assert.That(controller.HudCopyForTests, Does.Contain("표식 아래 라벨"));
            Assert.That(controller.HudCopyForTests, Does.Contain("남은 표식"));
            Assert.That(controller.LastMagicNoteText, Does.Contain("표식 근처"));
            Assert.That(controller.LastMagicNoteText, Does.Contain("물은 닫힌 원"));
            Assert.That(controller.LastMagicNoteText, Does.Contain("바람"));
        }

        [UnityTest]
        public IEnumerator RecognizedBaseAwayFromGoalExplainsTargetLocation()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);

            var result = controller.CastSyntheticBaseForTests(SpellFamily.Wind, Vector2.zero);
            yield return null;

            Assert.That(result.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(result.spell.recognizedFamily, Is.EqualTo(SpellFamily.Wind));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(0));
            Assert.That(controller.LastMagicNoteText, Does.Contain("바람 문양은 인식"));
            Assert.That(controller.LastMagicNoteText, Does.Contain("바람개비 표식 근처"));
            Assert.That(controller.LastMagicNoteText, Does.Contain("현재 거리"));
        }

        [UnityTest]
        public IEnumerator SyntheticBaseCastCreatesWorldSealWithoutPanel()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);

            var result = controller.CastSyntheticBaseForTests(SpellFamily.Fire, new Vector2(-5.5f, 2.6f));
            yield return null;

            Assert.That(result.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(result.spell.recognizedFamily, Is.EqualTo(SpellFamily.Fire));
            Assert.That(controller.ActiveSealCount, Is.EqualTo(1));
            Assert.That(controller.LastSealLifetimeSecondsForTests, Is.EqualTo(SpellRuntime.DefaultSealDurationSeconds).Within(0.001f));
            Assert.That(controller.IsDrawingPanelVisible, Is.False);
            Assert.That(controller.IsResultPanelVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator ExternalRecognitionFacadeAppliesSubmittedResults()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);

            var baseStrokes = Offset(GestureRecognizer.CreateCanonicalSamples(SpellFamily.Earth, 1.6f, 0.03f), Vector2.zero, 0.8f);
            var baseResult = SpellRuntime.RecognizeBase(baseStrokes);
            var submittedBase = controller.SubmitBaseRecognitionResult(baseResult, Vector2.zero, baseStrokes.Count);
            yield return null;

            Assert.That(submittedBase.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(controller.TrialCountForTests, Is.EqualTo(1));
            var snapshots = controller.GetActiveSealSnapshots();
            Assert.That(snapshots, Has.Count.EqualTo(1));
            Assert.That(snapshots[0].baseFamily, Is.EqualTo(SpellFamily.Earth));
            Assert.That(snapshots[0].sealId, Is.Not.Empty);
            Assert.That(snapshots[0].attachRadius, Is.GreaterThan(0f));

            var attachable = controller.FindAttachableSealSnapshot(Vector2.zero);
            Assert.That(attachable, Is.Not.Null);
            Assert.That(attachable.sealId, Is.EqualTo(snapshots[0].sealId));

            var overlayResult = new OverlayRecognitionResult
            {
                status = RecognitionStatus.Recognized,
                recognizedOperator = OverlayOperator.IceBar,
                score = 0.96f,
                shapeConfidence = 0.94f,
                scaleRatio = 0.24f,
                anchorZone = "upper",
                feedbackReason = "external input accepted"
            };
            var submittedOverlay = controller.SubmitOverlayRecognitionResult(overlayResult, snapshots[0].sealId, Vector2.zero, 1);
            yield return null;

            Assert.That(submittedOverlay.success, Is.True);
            Assert.That(controller.TrialCountForTests, Is.EqualTo(2));
            Assert.That(controller.LastOverlayStack, Does.Contain(OverlayOperator.IceBar));

            var farOverlayResult = new OverlayRecognitionResult
            {
                status = RecognitionStatus.Recognized,
                recognizedOperator = OverlayOperator.MartialAxis,
                score = 0.95f,
                shapeConfidence = 0.93f,
                scaleRatio = 0.23f,
                anchorZone = "upper",
                feedbackReason = "far external input rejected"
            };
            Assert.Throws<System.InvalidOperationException>(() =>
                controller.SubmitOverlayRecognitionResult(farOverlayResult, snapshots[0].sealId, new Vector2(6f, 0f), 1));
        }

        [UnityTest]
        public IEnumerator RecognizedBaseRetryNearExistingSealCreatesNewSeal()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);

            controller.CastSyntheticBaseForTests(SpellFamily.Earth, Vector2.zero);
            yield return null;
            Assert.That(controller.ActiveSealCount, Is.EqualTo(1));

            var retryStrokes = Offset(GestureRecognizer.CreateCanonicalSamples(SpellFamily.Wind, 1.6f, 0.03f), Vector2.zero, 0.8f);
            var retryResult = controller.CastRawBaseForTests(retryStrokes, Vector2.zero);
            yield return null;

            Assert.That(retryResult.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(retryResult.spell.recognizedFamily, Is.EqualTo(SpellFamily.Wind));
            Assert.That(controller.ActiveSealCount, Is.EqualTo(2));
            Assert.That(controller.LastOverlayStack, Is.Empty);
        }

        [UnityTest]
        public IEnumerator OverlayAttachesToSealStack()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);

            controller.CastSyntheticBaseForTests(SpellFamily.Earth, Vector2.zero);
            controller.CastSyntheticOverlayForTests(OverlayOperator.VoidCut, Vector2.zero);
            controller.CastSyntheticOverlayForTests(OverlayOperator.MartialAxis, Vector2.zero);
            yield return null;

            Assert.That(controller.LastOverlayStack.Contains(OverlayOperator.VoidCut), Is.True);
            Assert.That(controller.LastOverlayStack.Contains(OverlayOperator.MartialAxis), Is.True);
        }

        [UnityTest]
        public IEnumerator OverlayAndComboGoalsRequireNearbyWorldCasting()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);

            controller.LoadFloorForTests(1);
            controller.CastSyntheticBaseForTests(SpellFamily.Fire, Vector2.zero);
            var offTargetOverlay = controller.CastSyntheticOverlayForTests(OverlayOperator.IceBar, Vector2.zero);
            yield return null;
            Assert.That(offTargetOverlay.success, Is.True);
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(0));

            controller.CastSyntheticBaseForTests(SpellFamily.Fire, new Vector2(-0.65f, 3.0f));
            var onTargetOverlay = controller.CastSyntheticOverlayForTests(OverlayOperator.IceBar, new Vector2(-0.65f, 3.0f));
            yield return null;
            Assert.That(onTargetOverlay.success, Is.True);
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(1));

            controller.LoadFloorForTests(2);
            controller.CastSyntheticBaseForTests(SpellFamily.Earth, Vector2.zero);
            var offTargetCombo = controller.CastSyntheticOverlayForTests(OverlayOperator.SteelBrace, Vector2.zero);
            yield return null;
            Assert.That(offTargetCombo.success, Is.True);
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(0));

            controller.CastSyntheticBaseForTests(SpellFamily.Earth, new Vector2(-4.6f, 1.8f));
            var onTargetCombo = controller.CastSyntheticOverlayForTests(OverlayOperator.SteelBrace, new Vector2(-4.6f, 1.8f));
            yield return null;
            Assert.That(onTargetCombo.success, Is.True);
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DetachedOverlayShowsSealProximityHint()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);

            controller.CastSyntheticBaseForTests(SpellFamily.Earth, Vector2.zero);
            var result = controller.CastSyntheticOverlayForTests(OverlayOperator.IceBar, new Vector2(4.8f, 0f));
            yield return null;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.success, Is.False);
            Assert.That(result.recognizedOperator, Is.EqualTo(OverlayOperator.IceBar));
            Assert.That(controller.LastMagicNoteText, Does.Contain("seal에서 너무 멀"));
            Assert.That(controller.LastMagicNoteText, Does.Contain("빛나는 원"));
            Assert.That(controller.LastHintText, Does.Contain("빛나는 원"));
        }

        [UnityTest]
        public IEnumerator MartialAxisFailureUsesPlayerFacingDependencyHint()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);

            controller.CastSyntheticBaseForTests(SpellFamily.Earth, Vector2.zero);
            var result = controller.CastSyntheticOverlayForTests(OverlayOperator.MartialAxis, Vector2.zero);
            yield return null;

            Assert.That(result.success, Is.False);
            Assert.That(result.recognizedOperator, Is.EqualTo(OverlayOperator.MartialAxis));
            Assert.That(controller.LastMagicNoteText, Does.Contain("절단 장식"));
            Assert.That(controller.LastMagicNoteText, Does.Not.Contain("void_cut"));
            Assert.That(controller.LastHintText, Does.Contain("절단 장식"));
        }

        [UnityTest]
        public IEnumerator OversizedOverlayShowsScaleActionHint()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);

            controller.CastSyntheticBaseForTests(SpellFamily.Earth, Vector2.zero);
            var result = controller.CastSyntheticOverlayForTests(OverlayOperator.SoulDot, Vector2.zero, 1f);
            yield return null;

            Assert.That(result.success, Is.False);
            Assert.That(result.recognizedOperator, Is.EqualTo(OverlayOperator.SoulDot));
            Assert.That(result.scaleHint, Is.EqualTo(OverlayScaleHint.TooLarge));
            Assert.That(controller.LastMagicNoteText, Does.Contain("너무 커"));
            Assert.That(controller.LastHintText, Does.Contain("너무 큽니다"));
        }

        [UnityTest]
        public IEnumerator FailedBaseCastsEscalateMagicNoteHints()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);

            controller.CastRawBaseForTests(new List<List<StrokeSample>>(), Vector2.zero);
            yield return null;
            Assert.That(controller.CurrentAssistLevel, Is.EqualTo(1));
            Assert.That(controller.LastMagicNoteText, Does.Contain("짧은 힌트"));

            controller.CastRawBaseForTests(new List<List<StrokeSample>>(), Vector2.zero);
            yield return null;
            Assert.That(controller.CurrentAssistLevel, Is.EqualTo(2));
            Assert.That(controller.LastMagicNoteText, Does.Contain("체크리스트"));

            controller.CastRawBaseForTests(new List<List<StrokeSample>>(), Vector2.zero);
            yield return null;
            Assert.That(controller.CurrentAssistLevel, Is.EqualTo(3));
            Assert.That(controller.LastMagicNoteText, Does.Contain("강한 보조"));
            Assert.That(controller.LastHintText, Does.Contain("바람"));
        }

        [UnityTest]
        public IEnumerator SuccessAfterBaseHintKeepsAssistedFeedback()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);

            controller.CastRawBaseForTests(new List<List<StrokeSample>>(), Vector2.zero);
            controller.CastRawBaseForTests(new List<List<StrokeSample>>(), Vector2.zero);
            var result = controller.CastSyntheticBaseForTests(SpellFamily.Wind, new Vector2(5.5f, 2.6f));
            yield return null;

            Assert.That(result.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(controller.CurrentAssistLevel, Is.EqualTo(2));
            Assert.That(controller.LastMagicNoteText, Does.Contain("이전 힌트"));
            Assert.That(controller.ActiveSealCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SameComboReadsAsBridgeOnFloorThreeAndStabilizerOnFloorFour()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);

            controller.LoadFloorForTests(2);
            controller.CastSyntheticBaseForTests(SpellFamily.Earth, Vector2.zero);
            controller.CastSyntheticOverlayForTests(OverlayOperator.SteelBrace, Vector2.zero);
            yield return null;
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(0));

            var bridgePosition = new Vector2(-4.6f, 1.8f);
            controller.LoadFloorForTests(2);
            controller.CastSyntheticBaseForTests(SpellFamily.Earth, bridgePosition);
            controller.CastSyntheticOverlayForTests(OverlayOperator.SteelBrace, bridgePosition);
            yield return null;

            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(3));
            Assert.That(controller.LastMagicNoteText, Does.Contain("공중 다리"));
            Assert.That(controller.LastMagicNoteText, Does.Contain("흐름"));

            var stabilizerPosition = new Vector2(-5.2f, 2.4f);
            controller.LoadFloorForTests(3);
            controller.CastSyntheticBaseForTests(SpellFamily.Earth, stabilizerPosition);
            controller.CastSyntheticOverlayForTests(OverlayOperator.SteelBrace, stabilizerPosition);
            yield return null;

            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(4));
            Assert.That(controller.LastMagicNoteText, Does.Contain("균열"));
            Assert.That(controller.LastMagicNoteText, Does.Contain("안전 지점"));
            Assert.That(Vector2.Distance(controller.SafePositionForTests, stabilizerPosition), Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator FinalFloorCompletionShowsFinalSealCelebrationBeforeReport()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);

            controller.LoadFloorForTests(4);
            yield return null;

            controller.CompleteCurrentFloorForTests();
            yield return null;

            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(5));
            Assert.That(controller.HasEndingReport, Is.False);
            Assert.That(controller.LastMagicNoteText, Does.Contain("성좌심 완전 복구"));
            Assert.That(controller.ActivePulseCountForTests, Is.GreaterThan(controller.ActiveGoalCount));
        }

        [UnityTest]
        public IEnumerator FinalFloorShowsRemainingGoalGuideAndNextHint()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);

            controller.LoadFloorForTests(4);
            yield return null;

            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(5));
            Assert.That(controller.ActiveGoalCount, Is.EqualTo(6));
            Assert.That(controller.HudCopyForTests, Does.Contain("남은 요구"));
            Assert.That(controller.HudCopyForTests, Does.Contain("안정"));
            Assert.That(controller.HudCopyForTests, Does.Contain("정화"));
            Assert.That(controller.FloorProgressForTests, Does.Contain("다음 안정"));
            Assert.That(controller.LastMagicNoteText, Does.Contain("다음 목표"));
            Assert.That(controller.LastMagicNoteText, Does.Contain("땅 + 보강"));

            var stabilityPosition = new Vector2(-4.8f, 2.6f);
            controller.CastSyntheticBaseForTests(SpellFamily.Earth, stabilityPosition);
            controller.CastSyntheticOverlayForTests(OverlayOperator.SteelBrace, stabilityPosition);
            yield return null;

            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(1));
            Assert.That(controller.LastMagicNoteText, Does.Contain("다음 목표"));
            Assert.That(controller.LastMagicNoteText, Does.Contain("정화"));
            Assert.That(controller.LastMagicNoteText, Does.Contain("물"));
            Assert.That(controller.FloorProgressForTests, Does.Contain("다음 정화"));
        }

        [UnityTest]
        public IEnumerator FinalFloorCanPassAtFiveOfSixGoals()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);

            controller.LoadFloorForTests(4);
            yield return null;

            controller.CompleteCurrentGoalsForTests(ExamGameController.FinalFloorPassingGoalCount);
            yield return null;

            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(5));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(5));
            Assert.That(controller.HasEndingReport, Is.False);
            Assert.That(controller.LastMagicNoteText, Does.Contain("입학 시험 통과"));
            Assert.That(controller.FloorProgressForTests, Does.Contain("목표 5/6"));
            Assert.That(controller.PendingAdvanceSecondsForTests, Is.GreaterThan(ExamGameController.FinalFloorCompleteReportDelaySeconds));

            controller.AdvanceFloorForTests();
            yield return null;

            Assert.That(controller.HasEndingReport, Is.True);
            Assert.That(controller.EndingReportTextForTests, Does.Contain("통과 엔딩 (5/6)"));
        }

        [UnityTest]
        public IEnumerator FinalFloorFiveGoalPassCanUpgradeToTrueEndingBeforeReport()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);

            controller.LoadFloorForTests(4);
            yield return null;

            controller.CompleteCurrentGoalsForTests(ExamGameController.FinalFloorPassingGoalCount);
            yield return null;
            Assert.That(controller.LastMagicNoteText, Does.Contain("입학 시험 통과"));
            Assert.That(controller.HasEndingReport, Is.False);
            Assert.That(controller.PendingAdvanceSecondsForTests, Is.GreaterThan(ExamGameController.FinalFloorCompleteReportDelaySeconds));

            controller.CompleteCurrentGoalsForTests(1);
            yield return null;

            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(6));
            Assert.That(controller.LastMagicNoteText, Does.Contain("성좌심 완전 복구"));
            Assert.That(controller.HasEndingReport, Is.False);
            Assert.That(controller.PendingAdvanceSecondsForTests, Is.GreaterThan(0f));
            Assert.That(controller.PendingAdvanceSecondsForTests, Is.LessThan(ExamGameController.FinalFloorPassReportDelaySeconds));

            controller.AdvanceFloorForTests();
            yield return null;

            Assert.That(controller.HasEndingReport, Is.True);
            Assert.That(controller.EndingReportTextForTests, Does.Contain("진엔딩 (6/6 완전 복구)"));
        }

        [UnityTest]
        public IEnumerator FloorTransitionsHazardResetAndEndingReportWork()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);

            controller.CompleteCurrentFloorForTests();
            controller.AdvanceFloorForTests();
            yield return null;
            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(2));

            controller.LoadFloorForTests(3);
            controller.MovePlayerForTests(new Vector2(-3.1f, -0.4f));
            yield return null;
            Assert.That(Vector2.Distance(controller.PlayerPosition, new Vector2(0f, -4.05f)), Is.LessThan(0.2f));

            for (var index = controller.CurrentFloorNumber; index <= controller.FloorCount; index++)
            {
                controller.CompleteCurrentFloorForTests();
                controller.AdvanceFloorForTests();
                yield return null;
            }

            Assert.That(controller.HasEndingReport, Is.True);
            Assert.That(controller.EndingReportTextForTests, Does.Contain("입학 시험"));
            Assert.That(controller.EndingReportTextForTests, Does.Contain("도달 상태"));
            Assert.That(controller.EndingReportTextForTests, Does.Contain("진엔딩 (6/6 완전 복구)"));
            Assert.That(controller.EndingReportTextForTests, Does.Contain("가장 많이 사용한 base"));
            Assert.That(controller.EndingReportTextForTests, Does.Contain("가장 많이 사용한 overlay"));
            Assert.That(controller.EndingReportTextForTests, Does.Contain("평균 문양 안정도"));
            Assert.That(controller.EndingReportTextForTests, Does.Contain("자기 평가"));
            Assert.That(controller.EndingReportTextForTests, Does.Contain("MagicExamHallLogs"));
        }

        private static List<List<StrokeSample>> Offset(List<List<StrokeSample>> strokes, Vector2 center, float canonicalCenter)
        {
            return strokes
                .Select(stroke => stroke.Select(sample => new StrokeSample(sample.position - Vector2.one * canonicalCenter + center, sample.time)).ToList())
                .ToList();
        }
    }
}
