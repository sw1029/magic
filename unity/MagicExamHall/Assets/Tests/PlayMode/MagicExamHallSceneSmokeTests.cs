using System.Collections;
using System.Collections.Generic;
using System.IO;
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
            var boot = Object.FindFirstObjectByType<GameBootController>();

            Assert.That(controller, Is.Not.Null);
            Assert.That(boot, Is.Not.Null);
            Assert.That(boot.StateForTests, Is.EqualTo(GameBootState.Title));
            Assert.That(controller.IsGameplayInputEnabledForTests, Is.False);
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
            Assert.That(Object.FindFirstObjectByType<MentorPresentationController>(), Is.Not.Null);
            Assert.That(controller.IsMentorVisibleForTests, Is.True);
            Assert.That(controller.CurrentMentorNameForTests, Is.EqualTo("발착층 조교"));
            Assert.That(controller.MentorSpeechTextForTests, Is.EqualTo(controller.LastMagicNoteText));
            Assert.That(controller.OutputDirectory, Does.Contain("MagicExamHallLogs"));
        }

        [UnityTest]
        public IEnumerator BootFlowStartsNewGameAndPausesWorldInput()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            var boot = Object.FindFirstObjectByType<GameBootController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(boot, Is.Not.Null);
            if (File.Exists(boot.SavePath))
            {
                File.Delete(boot.SavePath);
            }

            boot.StartNewGameForTests();
            yield return null;

            Assert.That(boot.StateForTests, Is.EqualTo(GameBootState.Gameplay));
            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(1));
            Assert.That(controller.IsGameplayInputEnabledForTests, Is.True);
            Assert.That(boot.CodexQuickButtonVisibleForTests, Is.True);
            Assert.That(File.Exists(boot.SavePath), Is.True);
            Assert.That(controller.MagicNoteEntriesForTests.Count, Is.GreaterThanOrEqualTo(1));

            boot.ShowPauseForTests();
            yield return null;

            Assert.That(boot.StateForTests, Is.EqualTo(GameBootState.Paused));
            Assert.That(controller.IsGameplayInputEnabledForTests, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(0f));
            Assert.That(boot.CodexQuickButtonVisibleForTests, Is.False);

            boot.ResumeGameplayForTests();
            yield return null;

            Assert.That(boot.StateForTests, Is.EqualTo(GameBootState.Gameplay));
            Assert.That(controller.IsGameplayInputEnabledForTests, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(boot.CodexQuickButtonVisibleForTests, Is.True);
        }

        [UnityTest]
        public IEnumerator BootFlowAutoSavesCompletedFloorAndContinueRestoresProgress()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            var boot = Object.FindFirstObjectByType<GameBootController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(boot, Is.Not.Null);
            if (File.Exists(boot.SavePath))
            {
                File.Delete(boot.SavePath);
            }

            boot.StartNewGameForTests();
            controller.CompleteCurrentFloorForTests();
            yield return null;

            Assert.That(File.Exists(boot.SavePath), Is.True);
            var saved = JsonUtility.FromJson<GameProgressSnapshot>(File.ReadAllText(boot.SavePath));
            Assert.That(saved.floorNumber, Is.EqualTo(2));
            Assert.That(saved.completedGoals, Is.EqualTo(5));
            Assert.That(saved.noteLines, Is.Not.Empty);

            controller.LoadFloorForTests(4);
            yield return null;
            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(5));

            boot.ContinueGameForTests();
            yield return null;

            Assert.That(boot.StateForTests, Is.EqualTo(GameBootState.Gameplay));
            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(2));
            Assert.That(controller.MagicNoteEntriesForTests.Count, Is.GreaterThanOrEqualTo(1));

            boot.ShowCodexForTests();
            yield return null;

            Assert.That(boot.StateForTests, Is.EqualTo(GameBootState.Codex));
            Assert.That(boot.CodexTextForTests, Does.Contain("1층"));
            boot.ResumeGameplayForTests();
        }

        [UnityTest]
        public IEnumerator SaveSlotsAndManualCodexSaveStayIndependent()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            var boot = Object.FindFirstObjectByType<GameBootController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(boot, Is.Not.Null);
            for (var slot = 1; slot <= 3; slot++)
            {
                var path = boot.SavePathForSlotForTests(slot);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }

            boot.SelectSaveSlotForTests(2);
            boot.StartNewGameForTests();
            controller.CompleteCurrentFloorForTests();
            yield return null;

            var slotTwo = JsonUtility.FromJson<GameProgressSnapshot>(File.ReadAllText(boot.SavePathForSlotForTests(2)));
            Assert.That(slotTwo.slotIndex, Is.EqualTo(2));
            Assert.That(slotTwo.floorNumber, Is.EqualTo(2));

            boot.SelectSaveSlotForTests(3);
            controller.LoadFloorForTests(4);
            boot.ShowCodexForTests();
            boot.ManualSaveForTests();
            yield return null;

            var slotThree = JsonUtility.FromJson<GameProgressSnapshot>(File.ReadAllText(boot.SavePathForSlotForTests(3)));
            Assert.That(slotThree.slotIndex, Is.EqualTo(3));
            Assert.That(slotThree.floorNumber, Is.EqualTo(5));
            Assert.That(boot.CodexTextForTests, Does.Contain("슬롯 3에 저장"));

            boot.SelectSaveSlotForTests(2);
            boot.ContinueGameForTests();
            yield return null;

            Assert.That(boot.ActiveSaveSlotForTests, Is.EqualTo(2));
            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator FirstFloorGhostTutorialAndDiscoveryCodexWork()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            var boot = Object.FindFirstObjectByType<GameBootController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(boot, Is.Not.Null);

            boot.StartNewGameForTests();
            controller.TriggerFirstFloorGhostForTests();
            yield return null;

            Assert.That(controller.ActiveGhostTraceCountForTests, Is.GreaterThan(0));
            Assert.That(controller.LastMagicNoteText, Does.Contain("흐릿한 선"));

            controller.CastSyntheticBaseForTests(SpellFamily.Fire, new Vector2(-5.5f, 2.6f));
            yield return null;
            Assert.That(controller.DiscoveredFamiliesForTests, Does.Contain(SpellFamily.Fire));

            boot.ShowDiscoveryCodexForTests();
            yield return null;
            Assert.That(boot.CodexTextForTests, Does.Contain("Base family"));
            Assert.That(boot.CodexTextForTests, Does.Contain("불"));
            boot.ResumeGameplayForTests();
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
        public IEnumerator MentorChangesProfileByFloorAndMirrorsNotes()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);

            controller.LoadFloorForTests(1);
            yield return null;

            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(2));
            Assert.That(controller.CurrentMentorNameForTests, Is.EqualTo("벽화 연구원"));
            Assert.That(controller.MentorMoodForTests, Is.EqualTo(MentorMood.Neutral));
            Assert.That(controller.MentorSpeechTextForTests, Is.EqualTo(controller.LastMagicNoteText));
            Assert.That(controller.MentorSpeechTextForTests, Does.Contain("장식"));
        }

        [UnityTest]
        public IEnumerator MentorReactsToSuccessAndFailure()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);

            controller.CastSyntheticBaseForTests(SpellFamily.Fire, new Vector2(-5.5f, 2.6f));
            Assert.That(controller.MentorMoodForTests, Is.EqualTo(MentorMood.Happy));
            Assert.That(controller.MentorSpeechTextForTests, Is.EqualTo(controller.LastMagicNoteText));
            Assert.That(controller.MentorSpeechTextForTests, Does.Contain("불씨"));

            controller.CastRawBaseForTests(new List<List<StrokeSample>>(), Vector2.zero);
            Assert.That(controller.MentorMoodForTests, Is.EqualTo(MentorMood.Frown));
            Assert.That(controller.MentorSpeechTextForTests, Is.EqualTo(controller.LastMagicNoteText));
            Assert.That(controller.MentorSpeechTextForTests, Does.Contain("짧은 힌트"));
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
        public IEnumerator ExternalRecognitionHandoffDrivesSameWorldProgression()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);

            var baseOutcome = controller.SubmitRecognitionHandoff(SpellRecognitionHandoff.Base(
                RecognitionStatus.Recognized,
                SpellFamily.Water,
                SpellFamily.Water,
                new Vector2(0f, 3f),
                0.97f,
                PerfectQuality(),
                "external water",
                worldScale: 1.35f,
                strokeCount: 1,
                sourceId: "sw1029"));
            yield return null;

            Assert.That(baseOutcome.kind, Is.EqualTo(SpellCastOutcomeKind.BaseSucceeded));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(1));
            Assert.That(controller.LastMagicNoteText, Does.Contain("물"));

            controller.LoadFloorForTests(1);
            yield return null;

            var noSeal = controller.SubmitRecognitionHandoff(SpellRecognitionHandoff.Overlay(
                RecognitionStatus.Recognized,
                OverlayOperator.IceBar,
                new Vector2(-0.65f, 3f),
                0.95f,
                0.95f,
                sourceId: "sw1029"));
            yield return null;

            Assert.That(noSeal.kind, Is.EqualTo(SpellCastOutcomeKind.OverlayNoActiveSeal));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(0));
            Assert.That(controller.LastMagicNoteText, Does.Contain("overlay는 단독"));

            var sealOutcome = controller.SubmitRecognitionHandoff(SpellRecognitionHandoff.Base(
                RecognitionStatus.Recognized,
                SpellFamily.Fire,
                SpellFamily.Fire,
                new Vector2(-0.65f, 3f),
                0.96f,
                PerfectQuality(),
                "external fire seal",
                worldScale: 1.4f,
                strokeCount: 1,
                sourceId: "sw1029"));
            var overlayOutcome = controller.SubmitRecognitionHandoff(SpellRecognitionHandoff.Overlay(
                RecognitionStatus.Recognized,
                OverlayOperator.IceBar,
                new Vector2(-0.65f, 3f),
                0.95f,
                0.95f,
                targetSealId: sealOutcome.createdSeal.sealId,
                sourceId: "sw1029"));
            yield return null;

            Assert.That(overlayOutcome.kind, Is.EqualTo(SpellCastOutcomeKind.OverlaySucceeded));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(1));
            Assert.That(controller.LastMagicNoteText, Does.Contain("얼음"));
        }

        [UnityTest]
        public IEnumerator FullSyntheticCastingPlaythroughReachesTrueEndingReport()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);

            CastBaseGoal(controller, SpellFamily.Fire, new Vector2(-5.5f, 2.6f));
            CastBaseGoal(controller, SpellFamily.Water, new Vector2(0f, 3.0f));
            CastBaseGoal(controller, SpellFamily.Wind, new Vector2(5.5f, 2.6f));
            CastBaseGoal(controller, SpellFamily.Earth, new Vector2(-3.2f, -2.45f));
            CastBaseGoal(controller, SpellFamily.Life, new Vector2(3.2f, -2.45f));
            yield return null;
            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(1));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(5));

            controller.AdvanceFloorForTests();
            yield return null;
            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(2));

            CastOverlayGoal(controller, OverlayOperator.SteelBrace, new Vector2(-5.8f, 2.7f));
            CastOverlayGoal(controller, OverlayOperator.ElectricFork, new Vector2(-3.2f, 3.0f));
            CastOverlayGoal(controller, OverlayOperator.IceBar, new Vector2(-0.65f, 3.0f));
            CastOverlayGoal(controller, OverlayOperator.SoulDot, new Vector2(1.9f, 3.0f));
            CastOverlayGoal(controller, OverlayOperator.VoidCut, new Vector2(4.45f, 3.0f));
            CastOverlayGoal(controller, OverlayOperator.MartialAxis, new Vector2(6.4f, 2.7f));
            yield return null;
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(6));

            controller.AdvanceFloorForTests();
            yield return null;
            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(3));

            CastComboGoal(controller, SpellFamily.Earth, OverlayOperator.SteelBrace, new Vector2(-4.6f, 1.8f));
            CastComboGoal(controller, SpellFamily.Wind, OverlayOperator.MartialAxis, new Vector2(4.6f, 1.8f));
            CastComboGoal(controller, SpellFamily.Life, OverlayOperator.SoulDot, new Vector2(-3.2f, -2.3f));
            CastComboGoal(controller, SpellFamily.Water, OverlayOperator.IceBar, new Vector2(3.2f, -2.3f));
            yield return null;
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(4));

            controller.AdvanceFloorForTests();
            yield return null;
            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(4));

            CastComboGoal(controller, SpellFamily.Earth, OverlayOperator.SteelBrace, new Vector2(-5.2f, 2.4f));
            CastOverlayGoal(controller, OverlayOperator.IceBar, new Vector2(-1.7f, 2.9f));
            CastOverlayGoal(controller, OverlayOperator.VoidCut, new Vector2(1.8f, 2.9f));
            CastOverlayGoal(controller, OverlayOperator.ElectricFork, new Vector2(5.2f, 2.4f));
            yield return null;
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(4));
            Assert.That(Vector2.Distance(controller.SafePositionForTests, new Vector2(5.2f, 2.4f)), Is.LessThan(0.01f));

            controller.AdvanceFloorForTests();
            yield return null;
            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(5));

            CastComboGoal(controller, SpellFamily.Earth, OverlayOperator.SteelBrace, new Vector2(-4.8f, 2.6f));
            CastBaseGoal(controller, SpellFamily.Water, new Vector2(-1.6f, 3.0f));
            CastComboGoal(controller, SpellFamily.Life, OverlayOperator.SoulDot, new Vector2(1.6f, 3.0f));
            CastOverlayGoal(controller, OverlayOperator.VoidCut, new Vector2(4.8f, 2.6f));
            CastOverlayGoal(controller, OverlayOperator.SoulDot, new Vector2(-2.2f, -2.5f));
            CastBaseGoal(controller, SpellFamily.Wind, new Vector2(2.2f, -2.5f));
            yield return null;
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(6));
            Assert.That(controller.LastMagicNoteText, Does.Contain("성좌심 완전 복구"));

            controller.AdvanceFloorForTests();
            yield return null;

            Assert.That(controller.HasEndingReport, Is.True);
            Assert.That(controller.EndingReportTextForTests, Does.Contain("진엔딩 (6/6 완전 복구)"));
            Assert.That(controller.EndingReportTextForTests, Does.Contain("base 성공/실패"));
            Assert.That(controller.EndingReportTextForTests, Does.Contain("overlay 성공/실패"));
            Assert.That(controller.EndingReportTextForTests, Does.Contain("층별 완료"));
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

        private static void CastBaseGoal(ExamGameController controller, SpellFamily family, Vector2 position)
        {
            controller.CastSyntheticBaseForTests(family, position);
        }

        private static void CastOverlayGoal(ExamGameController controller, OverlayOperator op, Vector2 position)
        {
            controller.CastSyntheticBaseForTests(SpellFamily.Fire, position);
            if (op == OverlayOperator.MartialAxis)
            {
                controller.CastSyntheticOverlayForTests(OverlayOperator.VoidCut, position);
            }

            controller.CastSyntheticOverlayForTests(op, position);
        }

        private static void CastComboGoal(ExamGameController controller, SpellFamily family, OverlayOperator op, Vector2 position)
        {
            controller.CastSyntheticBaseForTests(family, position);
            if (op == OverlayOperator.MartialAxis)
            {
                controller.CastSyntheticOverlayForTests(OverlayOperator.VoidCut, position);
            }

            controller.CastSyntheticOverlayForTests(op, position);
        }

        private static QualityVector PerfectQuality()
        {
            return new QualityVector
            {
                closure = 1f,
                smoothness = 1f,
                tempo = 1f,
                stability = 1f,
                rotationBias = 0f
            };
        }
    }
}
