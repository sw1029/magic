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
            Assert.That(Object.FindFirstObjectByType<WorldDrawingController>(), Is.Not.Null);
            Assert.That(controller.OutputDirectory, Does.Contain("MagicExamHallLogs"));
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
            Assert.That(controller.IsDrawingPanelVisible, Is.False);
            Assert.That(controller.IsResultPanelVisible, Is.False);
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
            Assert.That(controller.LastMagicNoteText, Does.Contain("성좌심 완성"));
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
            Assert.That(controller.EndingReportTextForTests, Does.Contain("입학 시험 완료"));
            Assert.That(controller.EndingReportTextForTests, Does.Contain("가장 많이 사용한 base"));
            Assert.That(controller.EndingReportTextForTests, Does.Contain("가장 많이 사용한 overlay"));
            Assert.That(controller.EndingReportTextForTests, Does.Contain("평균 문양 안정도"));
            Assert.That(controller.EndingReportTextForTests, Does.Contain("MagicExamHallLogs"));
        }
    }
}
