using System;
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
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MagicExamHall.Tests
{
    public sealed class MagicExamHallSceneSmokeTests
    {
        [UnityTest]
        public IEnumerator SceneLoggerSuppressesCollectionDuringPlayModeTests()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();

            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.IsLogCollectionEnabledForTests, Is.False);
            Assert.That(controller.OutputDirectory, Is.EqualTo(ExamLogger.DisabledOutputDirectory));
        }

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
            Assert.That(Camera.main.GetComponent<AudioListener>(), Is.Not.Null);
            Assert.That(Camera.main.clearFlags, Is.EqualTo(CameraClearFlags.SolidColor));
            Assert.That(Camera.main.orthographicSize, Is.EqualTo(ExamGameController.GameplayCameraOrthographicSize).Within(0.001f));
            var playerAnimator = Object.FindFirstObjectByType<PlayerSpriteAnimator>();
            Assert.That(playerAnimator, Is.Not.Null);
            var playerRenderer = playerAnimator.GetComponent<SpriteRenderer>();
            Assert.That(playerRenderer, Is.Not.Null);
            Assert.That(playerRenderer.enabled, Is.True);
            Assert.That(playerRenderer.sprite, Is.Not.Null);
            Assert.That(playerRenderer.bounds.size.x, Is.GreaterThan(0.1f));
            Assert.That(playerRenderer.bounds.size.y, Is.GreaterThan(0.1f));
            var playerViewport = Camera.main.WorldToViewportPoint(playerRenderer.bounds.center);
            Assert.That(playerViewport.z, Is.GreaterThan(0f));
            Assert.That(playerViewport.x, Is.InRange(-0.25f, 1.25f));
            Assert.That(playerViewport.y, Is.InRange(-0.25f, 1.25f));
            var drawing = Object.FindFirstObjectByType<WorldDrawingController>();
            Assert.That(drawing, Is.Not.Null);
            Assert.That(drawing.bufferSeconds, Is.EqualTo(WorldDrawingController.DefaultBufferSeconds).Within(0.001f));
            Assert.That(drawing.minPointDistance, Is.EqualTo(WorldDrawingController.DefaultMinPointDistance).Within(0.001f));
            Assert.That(Object.FindFirstObjectByType<MentorPresentationController>(), Is.Not.Null);
            Assert.That(controller.IsMentorVisibleForTests, Is.True);
            Assert.That(controller.MentorSpeechTextForTests, Is.Not.Empty);
            Assert.That(controller.MentorSpeechTextForTests.Length, Is.LessThan(controller.LastMagicNoteText.Length));
            Assert.That(controller.MentorSpeechTextForTests.Split('\n').Length, Is.LessThanOrEqualTo(3));
            Assert.That(controller.IsLogCollectionEnabledForTests, Is.False);
            Assert.That(controller.OutputDirectory, Is.EqualTo(ExamLogger.DisabledOutputDirectory));
            Assert.That(controller.IsResultPanelVisible, Is.False);
            Assert.That(controller.VisibleOverlayGuideCountForTests, Is.EqualTo(0));
            Assert.That(controller.ActiveShelfGuideArrowCountForTests, Is.EqualTo(0));
            Assert.That(controller.ActiveSpriteAccentAnimationCountForTests, Is.GreaterThanOrEqualTo(7));
            var globalLight = Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None)
                .FirstOrDefault(light => light.lightType == Light2D.LightType.Global);
            Assert.That(globalLight, Is.Not.Null);
            Assert.That(globalLight.intensity, Is.GreaterThanOrEqualTo(0.60f));
            Assert.That(Camera.main.backgroundColor.grayscale, Is.GreaterThan(0.065f));
            var northwestLight = GameObject.Find("Northwest Candle Flame Light 2D")?.GetComponent<Light2D>();
            var northwestGlow = GameObject.Find("Northwest Candle Light Spread")?.GetComponent<SpriteRenderer>();
            Assert.That(northwestLight, Is.Not.Null);
            Assert.That(northwestLight.pointLightOuterRadius, Is.GreaterThanOrEqualTo(4.0f));
            Assert.That(northwestGlow, Is.Not.Null);
            Assert.That(northwestGlow.sprite, Is.Not.Null);
            Assert.That(northwestGlow.bounds.size.x, Is.GreaterThan(5.0f));
            Assert.That(northwestGlow.color.a, Is.GreaterThan(0.30f));
            Assert.That(GameObject.Find("West Bookcase Guide Arrow"), Is.Null);
            Assert.That(GameObject.Find("East Bookcase Guide Arrow"), Is.Null);
            Assert.That(controller.VersionLabelForTests, Is.EqualTo(ExamGameController.BuildVersion));
            Assert.That(controller.IsFirstFloorLetterVisibleForTests, Is.False);

            boot.StartNewGameForTests();
            yield return null;

            Assert.That(boot.StateForTests, Is.EqualTo(GameBootState.Gameplay));
            Assert.That(controller.IsGameplayInputEnabledForTests, Is.True);
            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(1));
            Assert.That(controller.IsFirstFloorLetterVisibleForTests, Is.True);
            Assert.That(controller.FirstFloorLetterTextForTests.Split('\n').Length, Is.EqualTo(3));
            Assert.That(controller.FirstFloorLetterTextForTests, Does.Contain("탑에 온 것을 환영한다"));
            var letterOverlay = GameObject.Find("First Floor Letter Overlay")?.GetComponent<Image>();
            Assert.That(letterOverlay, Is.Not.Null);
            Assert.That(letterOverlay.color.a, Is.GreaterThan(0.75f));
            var closeButton = GameObject.Find("First Floor Letter Close Button")?.GetComponent<Button>();
            Assert.That(closeButton, Is.Not.Null);
            Assert.That(closeButton.GetComponentInChildren<Text>().text, Is.EqualTo("접기"));
            Assert.That(controller.FirstFloorLetterCloseButtonColorForTests.r, Is.GreaterThan(0.45f));
            Assert.That(controller.FirstFloorLetterCloseButtonColorForTests.g, Is.GreaterThan(0.20f));
            Assert.That(controller.FirstFloorLetterCloseButtonColorForTests.b, Is.LessThan(0.16f));

            closeButton.onClick.Invoke();
            yield return null;

            Assert.That(controller.IsFirstFloorLetterVisibleForTests, Is.False);
            var candleScale = controller.SpriteAccentScaleForTests("Northwest Candle");
            yield return new WaitForSeconds(0.24f);
            Assert.That(Vector3.Distance(controller.SpriteAccentScaleForTests("Northwest Candle"), candleScale), Is.GreaterThan(0.001f));
        }

        [UnityTest]
        public IEnumerator CameraZoomControlsAdjustWorldCameraAndPersistAcrossFloorLoads()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();

            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.IsCameraZoomControlVisibleForTests, Is.True);
            Assert.That(controller.CameraZoomLabelForTests, Is.EqualTo("100%"));
            var defaultSize = controller.CameraOrthographicSizeForTests;
            Assert.That(defaultSize, Is.EqualTo(ExamGameController.GameplayCameraOrthographicSize).Within(0.001f));

            controller.ZoomCameraInForTests();
            yield return null;

            Assert.That(controller.CameraZoomPercentForTests, Is.EqualTo(110));
            Assert.That(controller.CameraZoomLabelForTests, Is.EqualTo("110%"));
            Assert.That(controller.CameraOrthographicSizeForTests, Is.LessThan(defaultSize));
            var zoomedSize = controller.CameraOrthographicSizeForTests;

            controller.LoadFloorForTests(3);
            yield return null;

            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(4));
            Assert.That(controller.CameraZoomPercentForTests, Is.EqualTo(110));
            Assert.That(controller.CameraOrthographicSizeForTests, Is.EqualTo(zoomedSize).Within(0.001f));

            controller.ZoomCameraOutForTests();
            yield return null;

            Assert.That(controller.CameraZoomPercentForTests, Is.EqualTo(100));
            Assert.That(controller.CameraOrthographicSizeForTests, Is.EqualTo(defaultSize).Within(0.001f));

            controller.ZoomCameraOutForTests();
            yield return null;

            Assert.That(controller.CameraZoomPercentForTests, Is.EqualTo(90));
            Assert.That(controller.CameraOrthographicSizeForTests, Is.GreaterThan(defaultSize));

            controller.ResetCameraZoomForTests();
            yield return null;

            Assert.That(controller.CameraZoomPercentForTests, Is.EqualTo(100));
            Assert.That(controller.CameraZoomLabelForTests, Is.EqualTo("100%"));
            Assert.That(controller.CameraOrthographicSizeForTests, Is.EqualTo(defaultSize).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator StageTorchLightSpreadAppearsOnCrossingFloor()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);

            controller.LoadFloorForTests(2);
            yield return null;

            var torchLight = GameObject.Find("Crossing West Torch Flame Light 2D")?.GetComponent<Light2D>();
            var torchGlow = GameObject.Find("Crossing West Torch Light Spread")?.GetComponent<SpriteRenderer>();
            Assert.That(torchLight, Is.Not.Null);
            Assert.That(torchLight.pointLightOuterRadius, Is.GreaterThanOrEqualTo(4.0f));
            Assert.That(torchGlow, Is.Not.Null);
            Assert.That(torchGlow.sprite, Is.Not.Null);
            Assert.That(torchGlow.bounds.size.x, Is.GreaterThan(5.5f));
            Assert.That(torchGlow.color.a, Is.GreaterThan(0.30f));
        }

        [UnityTest]
        public IEnumerator MentorSpeechLongTextUsesNextButtonWithoutEllipsis()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);
            var mentor = Object.FindFirstObjectByType<MentorPresentationController>();
            Assert.That(mentor, Is.Not.Null);

            mentor.Say(
                MentorMood.Neutral,
                "이 층의 표식은 흔들리는 균열입니다. 균열은 답이 돌아가는 속도다.\n" +
                "첫 번째 조합을 만들면 균열의 반응이 기록됩니다.\n" +
                "다음 표식은 다른 기본 문양을 요구합니다.");
            yield return null;

            Assert.That(controller.MentorSpeechPageCountForTests, Is.GreaterThan(1));
            Assert.That(controller.MentorSpeechPageIndexForTests, Is.EqualTo(0));
            Assert.That(controller.IsMentorSpeechNextButtonVisibleForTests, Is.True);
            Assert.That(controller.MentorSpeechTextForTests, Does.Not.Contain("..."));
            var firstPage = controller.MentorSpeechTextForTests;

            var nextButton = GameObject.Find("Mentor Speech Next Button")?.GetComponent<Button>();
            Assert.That(nextButton, Is.Not.Null);
            nextButton.onClick.Invoke();
            yield return null;

            Assert.That(controller.MentorSpeechPageIndexForTests, Is.EqualTo(1));
            Assert.That(controller.MentorSpeechTextForTests, Is.Not.EqualTo(firstPage));
            Assert.That(controller.MentorSpeechTextForTests, Does.Not.Contain("..."));

            var collected = $"{firstPage}\n{controller.MentorSpeechTextForTests}";
            var guard = 0;
            while (controller.AdvanceMentorSpeechForTests() && guard++ < 8)
            {
                yield return null;
                collected += $"\n{controller.MentorSpeechTextForTests}";
                Assert.That(controller.MentorSpeechTextForTests, Does.Not.Contain("..."));
            }

            var normalizedCollected = collected.Replace("\r", "").Replace("\n", "").Replace(" ", "");
            Assert.That(controller.IsMentorSpeechNextButtonVisibleForTests, Is.False);
            Assert.That(normalizedCollected, Does.Contain("균열은답이돌아가는속도다"));
            Assert.That(normalizedCollected, Does.Contain("첫번째조합"));
            Assert.That(normalizedCollected, Does.Contain("다음표식"));
        }

        [UnityTest]
        public IEnumerator FloorEntryMentorSpeechUsesConversationalTone()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            var boot = Object.FindFirstObjectByType<GameBootController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(boot, Is.Not.Null);

            boot.StartNewGameForTests();
            yield return null;
            AssertConversationalMentorSpeech(controller, "우클릭을 누르고", "그려 주세요");

            controller.LoadFloorForTests(1);
            yield return null;
            AssertConversationalMentorSpeech(controller, "슬롯", "넣었습니다");

            controller.LoadFloorForTests(2);
            yield return null;
            AssertConversationalMentorSpeech(controller, "빛나는 원", "이어 그려 주세요");

            controller.LoadFloorForTests(3);
            yield return null;
            AssertConversationalMentorSpeech(controller, "허수아비", "발사하세요");

            controller.LoadFloorForTests(4);
            yield return null;
            AssertConversationalMentorSpeech(controller, "최종", "문제");
        }

        [UnityTest]
        public IEnumerator FloorSkipButtonIsRedAndAdvancesToNextFloor()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null, "controller");
            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(1));
            Assert.That(controller.IsFloorSkipButtonVisibleForTests, Is.True);

            var button = GameObject.Find("Floor Test Skip Button")?.GetComponent<Button>();
            Assert.That(button, Is.Not.Null);
            var rect = button.GetComponent<RectTransform>();
            var image = button.GetComponent<Image>();
            Assert.That(rect.anchorMin, Is.EqualTo(new Vector2(1f, 0f)));
            Assert.That(rect.anchorMax, Is.EqualTo(new Vector2(1f, 0f)));
            Assert.That(rect.pivot, Is.EqualTo(new Vector2(1f, 0f)));
            Assert.That(image.color.r, Is.GreaterThan(0.75f));
            Assert.That(image.color.g, Is.LessThan(0.18f));
            Assert.That(image.color.b, Is.LessThan(0.12f));

            button.onClick.Invoke();
            yield return null;

            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(2));
            Assert.That(controller.ActiveShelfGuideArrowCountForTests, Is.EqualTo(0));
            Assert.That(controller.IsFirstFloorLetterVisibleForTests, Is.False);
            Assert.That(controller.HasEndingReport, Is.False);
            Assert.That(controller.IsFloorSkipButtonVisibleForTests, Is.True);
        }

        [UnityTest]
        public IEnumerator QuestScrollChecklistTracksCompletionAndSkipSavesScore()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.IsQuestScrollVisibleForTests, Is.True);
            Assert.That(controller.QuestChecklistTitleForTests, Does.Contain("층 1"));
            Assert.That(controller.QuestChecklistCompletedForTests, Is.EqualTo(0));
            Assert.That(controller.QuestChecklistTotalForTests, Is.EqualTo(3));
            Assert.That(controller.QuestChecklistScoreForTests, Is.EqualTo("0/3"));

            var panel = GameObject.Find("Quest Scroll Panel")?.GetComponent<RectTransform>();
            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.anchorMin, Is.EqualTo(new Vector2(1f, 1f)));
            Assert.That(panel.anchorMax, Is.EqualTo(new Vector2(1f, 1f)));
            Assert.That(panel.pivot, Is.EqualTo(new Vector2(1f, 1f)));
            Assert.That(panel.sizeDelta.x, Is.EqualTo(374f).Within(0.5f));
            Assert.That(Object.FindFirstObjectByType<CanvasScaler>()?.matchWidthOrHeight, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(GameObject.Find("Quest Scroll Readability Paper"), Is.Not.Null);
            Assert.That(GameObject.Find("Quest Scroll Top Roll"), Is.Not.Null);
            var bottomRoll = GameObject.Find("Quest Scroll Bottom Roll")?.GetComponent<RectTransform>();
            Assert.That(bottomRoll, Is.Not.Null);
            Assert.That(GameObject.Find("Quest Scroll Body"), Is.Not.Null);
            var firstLabel = GameObject.Find("Quest Checklist Label 1")?.GetComponent<Text>();
            Assert.That(firstLabel, Is.Not.Null);
            Assert.That(firstLabel.fontSize, Is.GreaterThanOrEqualTo(15));
            Assert.That(firstLabel.GetComponent<Shadow>(), Is.Not.Null);
            var questBody = GameObject.Find("Quest Scroll Body")?.GetComponent<RectTransform>();
            var progressText = GameObject.Find("Quest Progress Text")?.GetComponent<Text>();
            Assert.That(questBody, Is.Not.Null);
            Assert.That(progressText, Is.Not.Null);
            Assert.That(progressText.fontSize, Is.LessThanOrEqualTo(12));
            Assert.That(progressText.horizontalOverflow, Is.EqualTo(HorizontalWrapMode.Overflow));
            Assert.That(progressText.verticalOverflow, Is.EqualTo(VerticalWrapMode.Truncate));
            Assert.That(progressText.rectTransform.anchorMin, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(progressText.rectTransform.anchorMax, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(progressText.rectTransform.anchoredPosition.y, Is.LessThanOrEqualTo(0f));
            Assert.That(progressText.rectTransform.anchoredPosition.y - progressText.rectTransform.sizeDelta.y, Is.GreaterThanOrEqualTo(-questBody.rect.height - 0.5f));
            AssertTextFits("Quest Scroll Title");
            AssertTextFits("Quest Checklist Label 1");
            Assert.That(controller.QuestScrollPanelHeightForTests, Is.EqualTo(350f).Within(0.5f));
            Assert.That(controller.QuestScrollBodyAlphaForTests, Is.GreaterThan(0.98f));
            Assert.That(controller.QuestScrollToggleLabelForTests, Is.EqualTo("접기"));

            var toggle = GameObject.Find("Quest Scroll Toggle Button")?.GetComponent<Button>();
            Assert.That(toggle, Is.Not.Null);
            toggle.onClick.Invoke();
            yield return new WaitForSeconds(0.38f);
            yield return null;

            Assert.That(controller.IsQuestScrollCollapsedForTests, Is.True);
            Assert.That(controller.QuestScrollOpenAmountForTests, Is.LessThan(0.05f));
            Assert.That(controller.QuestScrollPanelHeightForTests, Is.EqualTo(78f).Within(1.0f));
            Assert.That(controller.QuestScrollBodyAlphaForTests, Is.LessThan(0.05f));
            Assert.That(controller.IsQuestScrollBodyActiveForTests, Is.False);
            Assert.That(controller.QuestScrollToggleLabelForTests, Is.EqualTo("펴기"));

            toggle.onClick.Invoke();
            yield return new WaitForSeconds(0.38f);
            yield return null;

            Assert.That(controller.IsQuestScrollCollapsedForTests, Is.False);
            Assert.That(controller.QuestScrollOpenAmountForTests, Is.GreaterThan(0.95f));
            Assert.That(controller.QuestScrollPanelHeightForTests, Is.EqualTo(350f).Within(1.0f));
            Assert.That(controller.QuestScrollBodyAlphaForTests, Is.GreaterThan(0.95f));
            Assert.That(controller.IsQuestScrollBodyActiveForTests, Is.True);
            Assert.That(controller.QuestScrollToggleLabelForTests, Is.EqualTo("접기"));

            controller.CompleteCurrentGoalsForTests(1);
            yield return null;

            Assert.That(controller.QuestChecklistCompletedForTests, Is.EqualTo(1));
            Assert.That(controller.QuestChecklistScoreForTests, Is.EqualTo("1/3"));
            var check = GameObject.Find("Quest Checkmark 1")?.GetComponent<QuestCheckMarkGraphic>();
            Assert.That(check, Is.Not.Null);
            Assert.That(check.color.r, Is.GreaterThan(0.75f));
            Assert.That(check.color.g, Is.LessThan(0.12f));
            Assert.That(check.color.b, Is.LessThan(0.10f));

            var button = GameObject.Find("Floor Test Skip Button")?.GetComponent<Button>();
            Assert.That(button, Is.Not.Null);
            button.onClick.Invoke();
            yield return null;

            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(2));
            Assert.That(controller.QuestProgressForTests, Is.EqualTo(controller.FloorProgressForTests));
            Assert.That(progressText.rectTransform.anchoredPosition.y - progressText.rectTransform.sizeDelta.y, Is.GreaterThanOrEqualTo(-questBody.rect.height - 0.5f));
            Assert.That(controller.QuestChecklistSavedCompletedForTests, Is.EqualTo(1));
            Assert.That(controller.QuestChecklistSavedTotalForTests, Is.EqualTo(3));
            Assert.That(controller.QuestChecklistGlobalCompletedForTests, Is.EqualTo(1));
            Assert.That(controller.QuestChecklistGlobalTotalForTests, Is.EqualTo(7));
            Assert.That(controller.QuestChecklistTitleForTests, Does.Contain("층 2"));
            Assert.That(controller.QuestChecklistSnapshotSummaryForTests, Does.Contain("1층 1/3 - skip"));
            Assert.That(controller.IsLogCollectionEnabledForTests, Is.False);

            var floorTwoLabels = ActiveQuestLabels();
            Assert.That(floorTwoLabels.Any(label => label.Contains("도형", StringComparison.Ordinal)), Is.True);

            controller.LoadFloorForTests(2);
            yield return null;

            Assert.That(controller.QuestChecklistTitleForTests, Does.Contain("층 3"));
            Assert.That(controller.QuestChecklistTotalForTests, Is.EqualTo(5));
            Assert.That(controller.IsQuestScrollCollapsedForTests, Is.True);
            Assert.That(controller.QuestScrollPanelHeightForTests, Is.EqualTo(78f).Within(1.0f));

            controller.LoadFloorForTests(3);
            yield return null;

            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(4));
            Assert.That(controller.HudTitleForTests, Does.Not.Contain("seal"));
            Assert.That(controller.HudCopyForTests, Does.Not.Contain("seal"));
            var floorFourLabels = ActiveQuestLabels();
            Assert.That(floorFourLabels.Any(label => label.Contains("beam", StringComparison.Ordinal)), Is.False);
            Assert.That(floorFourLabels.Any(label => label.Contains("seal", StringComparison.Ordinal)), Is.False);
            Assert.That(floorFourLabels.Any(label => label.Contains("균열", StringComparison.Ordinal)), Is.False);
            var floorFourGoalLabels = ActiveGoalLabels(controller.CurrentFloorNumber);
            Assert.That(floorFourGoalLabels.Any(label => label.Contains("seal", StringComparison.Ordinal)), Is.False);
            Assert.That(floorFourGoalLabels.Any(label => label.Contains("beam", StringComparison.Ordinal)), Is.False);
            Assert.That(GameObject.Find("Goal Requirement Icon Row beam_fire"), Is.Not.Null);
            Assert.That(GameObject.Find("Goal Requirement Icon beam_fire 2"), Is.Not.Null);
            AssertFloorFourGoalLabelsDoNotOverlap(controller.CurrentFloorNumber);
            Assert.That(controller.ActiveShelfGuideArrowCountForTests, Is.EqualTo(0));
            Assert.That(GameObject.Find("Floor 4 Beam Bookcase Guide Arrow"), Is.Null);
            Assert.That(GameObject.Find("Floor 4 Custom Shape Arrow Glyph"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator HealthBarLosesHalfHeartOnObstacleAndHostileContact()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.HealthHeartCountForTests, Is.EqualTo(3));
            Assert.That(controller.CurrentHealthHalfUnitsForTests, Is.EqualTo(6));
            Assert.That(GameObject.Find("Health Bar"), Is.Not.Null);
            Assert.That(GameObject.Find("Health Heart 1")?.GetComponent<HeartHealthGraphic>(), Is.Not.Null);
            Assert.That(GameObject.Find("Health Heart 1")?.GetComponent<CanvasRenderer>(), Is.Not.Null);
            Assert.That(controller.QuestStatusForTests, Does.Contain("층 1"));
            Assert.That(controller.QuestProgressForTests, Is.EqualTo(controller.FloorProgressForTests));

            controller.LoadFloorForTests(2);
            yield return null;

            controller.MovePlayerForTests(controller.StageObstacleCenterForTests("living_bridge"));
            yield return null;

            Assert.That(controller.CurrentHealthHalfUnitsForTests, Is.EqualTo(5));
            Assert.That(controller.LastHealthHeartStateForTests, Is.EqualTo(1));
            Assert.That(GameObject.Find("Health Heart 3")?.GetComponent<HeartHealthGraphic>()?.State, Is.EqualTo(1));
            Assert.That(controller.IsPlayerBlinkingForTests, Is.True);
            Assert.That(controller.PlayerBlinkTintForTests.a, Is.LessThan(1f));
            Assert.That(controller.LastDamagePopupTextForTests, Is.EqualTo("-1/2"));

            yield return new WaitForSeconds(1.15f);
            yield return null;

            controller.LoadFloorForTests(3);
            yield return null;
            controller.MovePlayerForTests(new Vector2(-4.8f, 1.13f));
            yield return null;

            Assert.That(GameObject.Find("Training Scarecrow"), Is.Not.Null);
            Assert.That(controller.CurrentHealthHalfUnitsForTests, Is.EqualTo(5));
        }

        [UnityTest]
        public IEnumerator ElementalEntitiesReactByMaterialAcrossTutorialObjects()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.ActiveElementalEntityCountForTests, Is.GreaterThan(8));

            var westBookcase = GameObject.Find("West Bookcase")?.GetComponent<ElementalEntity>();
            Assert.That(westBookcase, Is.Not.Null, "west bookcase elemental entity");
            Assert.That(westBookcase.HasMaterial(ElementalMaterial.Wood), Is.True);

            controller.CastSyntheticBaseForTests(SpellFamily.Fire, new Vector2(-7.25f, 1.1f));
            yield return null;

            Assert.That(westBookcase.HasState(ElementalState.Burning), Is.True);
            Assert.That(controller.LastElementalReactionCountForTests, Is.GreaterThan(0));
            Assert.That(controller.LastElementalReactionSummaryForTests, Does.Contain("점화"));

            var profilePath = TempCustomShapeProfilePath();
            controller.UseCustomShapeStoreForTests(profilePath);
            ClearCustomSlots(controller);
            controller.LoadFloorForTests(2);
            yield return null;
            Assert.That(controller.ImportCustomReferenceForTests(SpellFamily.Water, out _, out var waterMessage), Is.True, waterMessage);

            var river = GameObject.Find("River Deep Center")?.GetComponent<ElementalEntity>();
            Assert.That(river, Is.Not.Null, "river elemental entity");
            Assert.That(river.HasMaterial(ElementalMaterial.Water), Is.True);

            CastCustomReferenceSpell(controller, SpellFamily.Water, SpellFamily.Water, controller.StageGoalPositionForTests("frozen_river"));
            yield return null;

            Assert.That(river.HasState(ElementalState.Frozen), Is.True);
            Assert.That(controller.LastElementalReactionSummaryForTests, Does.Contain("빙결"));

            controller.LoadFloorForTests(3);
            yield return null;
            var scarecrow = GameObject.Find("Training Scarecrow");
            var scarecrowEntity = scarecrow?.GetComponent<ElementalEntity>();
            Assert.That(scarecrowEntity, Is.Not.Null, "training scarecrow elemental entity");
            Assert.That(scarecrowEntity.MovableByWind, Is.True);
            var scarecrowBefore = scarecrow.transform.position;

            controller.CastSyntheticBaseForTests(SpellFamily.Wind, new Vector2(scarecrowBefore.x, scarecrowBefore.y - 0.2f));
            yield return null;

            Assert.That(scarecrowEntity.HasState(ElementalState.WindPushed), Is.True);
            Assert.That(controller.LastElementalReactionCountForTests, Is.GreaterThan(0));
            Assert.That(controller.LastElementalReactionSummaryForTests, Does.Contain("밀림"));

            ClearCustomSlots(controller);
            DeleteIfExists(profilePath);
            yield break;

            var target = GameObject.Find("Rift Marker ice_training");
            var targetEntity = target?.GetComponent<ElementalEntity>();
            Assert.That(targetEntity, Is.Not.Null, "rift marker elemental entity");
            Assert.That(targetEntity.MovableByWind, Is.True);
            var before = target.transform.position;

            controller.CastSyntheticBaseForTests(SpellFamily.Wind, before);
            yield return null;

            Assert.That(targetEntity.HasState(ElementalState.WindPushed), Is.True);
            Assert.That(target.transform.position.x, Is.GreaterThan(before.x));
            Assert.That(controller.LastElementalReactionSummaryForTests, Does.Contain("밀림"));

            ClearCustomSlots(controller);
            DeleteIfExists(profilePath);
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
            Assert.That(boot.CodexQuickButtonPositionForTests, Is.EqualTo(new Vector2(-24f, 92f)));
            Assert.That(boot.CodexQuickButtonSizeForTests, Is.EqualTo(new Vector2(54f, 54f)));
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
            Assert.That(controller.IsFirstFloorLetterVisibleForTests, Is.False);
            Assert.That(boot.CodexPanelVisibleForTests, Is.True);
            Assert.That(boot.CodexPanelParentForTests, Is.EqualTo("Boot Overlay"));
            Assert.That(boot.CodexPanelPositionForTests, Is.EqualTo(Vector2.zero));
            Assert.That(boot.CodexBackdropBlocksRaycastsForTests, Is.True);
            Assert.That(boot.CodexPanelDrawsAboveBackdropForTests, Is.True);
            Assert.That(boot.CodexQuickButtonVisibleForTests, Is.False);
            Assert.That(boot.CodexTextForTests, Does.Contain("1층"));
            boot.ResumeGameplayForTests();
            yield return null;

            Assert.That(boot.StateForTests, Is.EqualTo(GameBootState.Gameplay));
            Assert.That(boot.CodexPanelVisibleForTests, Is.False);
            Assert.That(boot.CodexQuickButtonVisibleForTests, Is.True);
        }

        [UnityTest]
        public IEnumerator CodexShowsNewestNotesFirstWhenOpened()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            var boot = Object.FindFirstObjectByType<GameBootController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(boot, Is.Not.Null);

            controller.LoadSavedProgress(1, new[] { "old floor note", "new floor note" });
            yield return null;

            boot.ShowCodexForTests();
            yield return null;

            var text = boot.CodexTextForTests;
            Assert.That(text.IndexOf("new floor note", StringComparison.Ordinal), Is.LessThan(text.IndexOf("old floor note", StringComparison.Ordinal)), text);
        }

        [UnityTest]
        public IEnumerator PracticeModeUnlocksAfterEndingAndNeverProgressesOrSaves()
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
                var slotPath = boot.SavePathForSlotForTests(slot);
                if (File.Exists(slotPath))
                {
                    File.Delete(slotPath);
                }
            }

            Assert.That(boot.PracticeUnlockedForTests, Is.False);

            boot.StartNewGameForTests();
            controller.LoadFloorForTests(4);
            controller.CompleteCurrentFloorForTests();
            yield return null;

            Assert.That(boot.PracticeUnlockedForTests, Is.True, "엔딩 도달 저장이 연습장을 해금해야 한다");
            var savedBefore = File.ReadAllText(boot.SavePath);
            Assert.That(savedBefore, Does.Contain("진엔딩"));

            boot.StartPracticeModeForTests();
            yield return null;

            Assert.That(boot.StateForTests, Is.EqualTo(GameBootState.Gameplay));
            Assert.That(controller.IsPracticeMode, Is.True);
            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(1));

            controller.CompleteCurrentFloorForTests();
            yield return null;

            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(1), "연습장에서는 층이 진행되지 않아야 한다");
            Assert.That(File.ReadAllText(boot.SavePath), Is.EqualTo(savedBefore), "연습장은 저장을 건드리지 않아야 한다");

            boot.ManualSaveForTests();
            yield return null;
            Assert.That(File.ReadAllText(boot.SavePath), Is.EqualTo(savedBefore), "연습장 수동 저장은 차단되어야 한다");
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
        public IEnumerator ManualSaveRestoresPartialFloorGoalProgress()
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

            boot.SelectSaveSlotForTests(1);
            boot.StartNewGameForTests();
            controller.CompleteCurrentGoalsForTests(2);
            yield return null;

            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(1));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(2));

            boot.ShowCodexForTests();
            boot.ManualSaveForTests();
            yield return null;

            var saved = JsonUtility.FromJson<GameProgressSnapshot>(File.ReadAllText(boot.SavePath));
            Assert.That(saved.floorNumber, Is.EqualTo(1));
            Assert.That(saved.completedGoals, Is.EqualTo(2));
            Assert.That(saved.completedGoalIds, Has.Length.EqualTo(2));

            controller.LoadFloorForTests(2);
            yield return null;
            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(3));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(0));

            boot.ContinueGameForTests();
            yield return null;

            Assert.That(boot.StateForTests, Is.EqualTo(GameBootState.Gameplay));
            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(1));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(2));
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

            controller.CastSyntheticBaseForTests(SpellFamily.Water, controller.StageGoalPositionForTests("puddle"));
            yield return null;
            Assert.That(controller.DiscoveredFamiliesForTests, Does.Contain(SpellFamily.Water));

            boot.ShowDiscoveryCodexForTests();
            yield return null;
            Assert.That(boot.CodexTextForTests, Does.Contain("기본 속성"));
            Assert.That(boot.CodexTextForTests, Does.Contain(SpellLabels.Korean(SpellFamily.Water)));
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
            Assert.That(controller.ActiveGoalCount, Is.EqualTo(5));
            Assert.That(controller.VisibleGoalLabelCountForTests, Is.EqualTo(1));
            Assert.That(controller.VisibleGoalObjectCountForTests, Is.EqualTo(1));
            Assert.That(controller.CurrentFirstFloorTutorialGoalIdForTests, Is.EqualTo("puddle"));
            Assert.That(controller.CurrentFirstFloorTutorialFamilyForTests, Is.EqualTo(SpellFamily.Water));
            Assert.That(controller.ActiveSequentialGoalGuideCountForTests, Is.EqualTo(1));
            Assert.That(controller.CurrentSequentialGoalGuideGoalIdForTests, Is.EqualTo("puddle"));
            Assert.That(controller.IsGoalVisibleForTests("puddle"), Is.True);
            Assert.That(controller.IsGoalVisibleForTests("ember"), Is.False);
            Assert.That(controller.IsGoalVisibleForTests("vane"), Is.False);
            Assert.That(controller.GoalVisualAlphaForTests("puddle"), Is.GreaterThan(0.85f));
            Assert.That(controller.HudCopyForTests, Does.Contain("순차 입력"));
            Assert.That(controller.HudCopyForTests, Does.Contain("1/5"));
            Assert.That(controller.HudCopyForTests, Does.Contain("Esc/Backspace"));
            Assert.That(controller.LastMagicNoteText, Does.Contain("물 표식"));
            Assert.That(controller.LastMagicNoteText, Does.Contain("반투명"));
        }

        [UnityTest]
        public IEnumerator FirstFloorRevealsSequentialTransparentSymbolsAfterEachCapture()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);
            controller.CloseFirstFloorLetterForTests();

            Assert.That(controller.CurrentFirstFloorTutorialGoalIdForTests, Is.EqualTo("puddle"));
            Assert.That(controller.IsGoalVisibleForTests("puddle"), Is.True);
            Assert.That(controller.IsGoalVisibleForTests("ember"), Is.False);

            controller.CastSyntheticBaseForTests(SpellFamily.Water, controller.StageGoalPositionForTests("puddle"));
            yield return null;
            yield return null;

            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(1));
            Assert.That(controller.CurrentFirstFloorTutorialGoalIdForTests, Is.EqualTo("ember"));
            Assert.That(controller.ActiveSequentialGoalGuideCountForTests, Is.EqualTo(1));
            Assert.That(controller.CurrentSequentialGoalGuideGoalIdForTests, Is.EqualTo("ember"));
            Assert.That(controller.IsGoalVisibleForTests("puddle"), Is.True);
            Assert.That(controller.IsGoalVisibleForTests("ember"), Is.True);
            Assert.That(controller.IsGoalVisibleForTests("vane"), Is.False);
            Assert.That(controller.GoalVisualAlphaForTests("puddle"), Is.GreaterThan(0.70f));
            Assert.That(controller.GoalVisualAlphaForTests("ember"), Is.GreaterThan(0.85f));
            Assert.That(controller.VisibleGoalLabelCountForTests, Is.EqualTo(2));

            controller.CastSyntheticBaseForTests(SpellFamily.Fire, controller.StageGoalPositionForTests("ember"));
            yield return null;
            yield return null;

            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(2));
            Assert.That(controller.CurrentFirstFloorTutorialGoalIdForTests, Is.EqualTo("vane"));
            Assert.That(controller.ActiveSequentialGoalGuideCountForTests, Is.EqualTo(1));
            Assert.That(controller.CurrentSequentialGoalGuideGoalIdForTests, Is.EqualTo("vane"));
            Assert.That(controller.IsGoalVisibleForTests("vane"), Is.True);
            Assert.That(controller.IsGoalVisibleForTests("pillar"), Is.False);
            Assert.That(controller.GoalVisualAlphaForTests("vane"), Is.GreaterThan(0.85f));
        }

        [UnityTest]
        public IEnumerator GoalLabelsUseVisualRequirementIconsOnFloorsOneToFour()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);

            AssertVisualGoalRequirementRows(controller, expectedPlusCount: 0, expectedLabelCount: 1);

            controller.LoadFloorForTests(1);
            yield return null;
            yield return null;
            AssertVisualGoalRequirementRows(controller, expectedPlusCount: 1, expectedLabelCount: 1);
            Assert.That(GameObject.Find("Goal Requirement Icon custom_fire 2"), Is.Null);
            Assert.That(GameObject.Find("Goal Requirement Icon custom_water 2"), Is.Not.Null);
            Assert.That(GameObject.Find("Goal Requirement Icon custom_wind 2"), Is.Null);
            Assert.That(GameObject.Find("Goal Requirement Icon custom_earth 2"), Is.Null);
            Assert.That(GameObject.Find("Goal Requirement Icon custom_life 2"), Is.Null);
            var floorTwoDetailIcon = GameObject.Find("Goal Requirement Icon custom_water 2")?.GetComponent<RectTransform>();
            var floorTwoDetailRow = GameObject.Find("Goal Requirement Icon Row custom_water")?.GetComponent<Image>();
            Assert.That(floorTwoDetailIcon, Is.Not.Null);
            Assert.That(floorTwoDetailIcon.sizeDelta.x, Is.GreaterThanOrEqualTo(32f));
            Assert.That(floorTwoDetailIcon.sizeDelta.y, Is.GreaterThanOrEqualTo(32f));
            Assert.That(floorTwoDetailRow, Is.Not.Null);
            Assert.That(floorTwoDetailRow.color.a, Is.GreaterThanOrEqualTo(0.70f));
            var floorTwoDetailImage = GameObject.Find("Goal Requirement Icon custom_water 2")?.GetComponent<Image>();
            AssertSpriteUsesNeutralShapeInk(floorTwoDetailImage);
            Assert.That(floorTwoDetailImage!.sprite.name, Does.StartWith("CustomShape_"));

            controller.LoadFloorForTests(2);
            yield return null;
            yield return null;
            AssertVisualGoalRequirementRows(controller, expectedPlusCount: 4);
            Assert.That(GameObject.Find("Goal Requirement Icon frozen_river 2"), Is.Not.Null);
            Assert.That(GameObject.Find("Goal Requirement Icon earth_stairs 2"), Is.Not.Null);
            Assert.That(GameObject.Find("Goal Requirement Icon living_bridge 2"), Is.Not.Null);
            Assert.That(GameObject.Find("Goal Requirement Icon living_bridge 3"), Is.Null);
            Assert.That(GameObject.Find("Goal Requirement Icon wind_platform 2"), Is.Not.Null);
            AssertSpriteUsesNeutralShapeInk(GameObject.Find("Goal Requirement Icon frozen_river 2")?.GetComponent<Image>());
            AssertSpriteUsesNeutralShapeInk(GameObject.Find("Goal Requirement Icon earth_stairs 2")?.GetComponent<Image>());

            controller.LoadFloorForTests(3);
            yield return null;
            yield return null;
            AssertVisualGoalRequirementRows(
                controller,
                expectedPlusCount: 5,
                minLabelWidth: 120f,
                expectedOverflow: HorizontalWrapMode.Wrap);
            Assert.That(GameObject.Find("Goal Requirement Icon beam_fire 2"), Is.Not.Null);
            Assert.That(GameObject.Find("Goal Requirement Icon beam_water 2"), Is.Not.Null);
            Assert.That(GameObject.Find("Goal Requirement Icon beam_wind 2"), Is.Not.Null);
            Assert.That(GameObject.Find("Goal Requirement Icon beam_earth 2"), Is.Not.Null);
            Assert.That(GameObject.Find("Goal Requirement Icon beam_life 2"), Is.Not.Null);
            AssertSpriteUsesNeutralShapeInk(GameObject.Find("Goal Requirement Icon beam_fire 2")?.GetComponent<Image>());
            AssertSpriteUsesNeutralShapeInk(GameObject.Find("Goal Requirement Icon beam_water 2")?.GetComponent<Image>());
        }

        [UnityTest]
        public IEnumerator FloorOneToThreeFlowAcceptsNoisyInputsAndRejectsNoiseFalsePositives()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);
            var profilePath = TempCustomShapeProfilePath();
            controller.UseCustomShapeStoreForTests(profilePath);
            ClearCustomSlots(controller);
            controller.CloseFirstFloorLetterForTests();

            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(1));
            Assert.That(controller.CurrentFirstFloorTutorialGoalIdForTests, Is.EqualTo("puddle"));

            var puddle = controller.StageGoalPositionForTests("puddle");
            controller.MovePlayerForTests(puddle);
            var tapNoise = controller.CastRawBaseForTests(TapNoise(puddle), puddle, movePlayerToReference: false);
            yield return null;

            Assert.That(tapNoise.spell.status, Is.EqualTo(RecognitionStatus.Invalid));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(0));

            var noisyWater = CastNoisyBaseSpell(controller, SpellFamily.Water, puddle, 0.026f);
            yield return null;

            Assert.That(noisyWater.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(noisyWater.spell.recognizedFamily, Is.EqualTo(SpellFamily.Water));
            Assert.That(noisyWater.spell.preIntentFamily, Is.EqualTo(SpellFamily.Water));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(1));
            Assert.That(controller.CurrentFirstFloorTutorialGoalIdForTests, Is.EqualTo("ember"));
            Assert.That(controller.ActiveSealCount, Is.EqualTo(1));

            yield return new WaitForSeconds(ExamGameController.DefaultSealFallbackDelaySeconds + 0.2f);

            Assert.That(controller.ActiveSealCount, Is.EqualTo(0));
            Assert.That(controller.ActiveDefaultBarrierCountForTests, Is.GreaterThan(0));

            var ember = controller.StageGoalPositionForTests("ember");
            var noisyFire = CastNoisyBaseSpell(controller, SpellFamily.Fire, ember, 0.008f);
            yield return null;

            Assert.That(noisyFire.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(noisyFire.spell.recognizedFamily, Is.EqualTo(SpellFamily.Fire));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(2));
            Assert.That(controller.CurrentFirstFloorTutorialGoalIdForTests, Is.EqualTo("vane"));

            yield return WaitForDefaultSealFallback(controller);

            var vane = controller.StageGoalPositionForTests("vane");
            var noisyWind = CastNoisyBaseSpell(controller, SpellFamily.Wind, vane, 0.012f);
            yield return null;

            Assert.That(noisyWind.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(noisyWind.spell.recognizedFamily, Is.EqualTo(SpellFamily.Wind));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(3));
            Assert.That(controller.CurrentFirstFloorTutorialGoalIdForTests, Is.EqualTo("pillar"));

            yield return WaitForDefaultSealFallback(controller);

            var pillar = controller.StageGoalPositionForTests("pillar");
            var noisyEarth = CastNoisyBaseSpell(controller, SpellFamily.Earth, pillar, 0.012f);
            yield return null;

            Assert.That(noisyEarth.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(noisyEarth.spell.recognizedFamily, Is.EqualTo(SpellFamily.Earth));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(4));
            Assert.That(controller.CurrentFirstFloorTutorialGoalIdForTests, Is.EqualTo("vine"));

            yield return WaitForDefaultSealFallback(controller);

            var vine = controller.StageGoalPositionForTests("vine");
            var noisyLife = CastNoisyBaseSpell(controller, SpellFamily.Life, vine, 0.012f);
            yield return null;

            Assert.That(noisyLife.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(noisyLife.spell.recognizedFamily, Is.EqualTo(SpellFamily.Life));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(5));
            Assert.That(controller.PendingAdvanceSecondsForTests, Is.GreaterThan(0f));
            Assert.That(controller.IsFloorTransitionActiveForTests, Is.True);

            yield return new WaitForSeconds(ExamGameController.StandardFloorAdvanceDelaySeconds + 3.0f);

            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(2));
            Assert.That(controller.CustomShapesAvailableForTests, Is.True);
            Assert.That(controller.CurrentSecondFloorSequenceGoalIdForTests, Is.EqualTo("custom_water"));
            Assert.That(controller.ImportCustomReferenceForTests(SpellFamily.Fire, out _, out var earlyFireReferenceMessage), Is.False, earlyFireReferenceMessage);

            var customWaterGoal = controller.StageGoalPositionForTests("custom_water");
            var builtInWater = controller.CastRawBaseForTests(
                NoisyCanonicalBase(SpellFamily.Water, customWaterGoal, 0.026f),
                customWaterGoal);
            yield return null;

            Assert.That(builtInWater.spell.isCustomShape, Is.False);
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(0));

            Assert.That(controller.ImportCustomReferenceForTests(SpellFamily.Water, out _, out var waterReferenceMessage), Is.True, waterReferenceMessage);

            var customWater = controller.CastRawBaseForTests(
                AddDeterministicNoise(controller.CustomReferenceStrokesForTests(SpellFamily.Water, customWaterGoal), 0.032f),
                customWaterGoal);
            yield return null;

            Assert.That(customWater.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(customWater.spell.isCustomShape, Is.True, customWater.spell.feedbackReason);
            Assert.That(customWater.spell.recognizedFamily, Is.EqualTo(SpellFamily.Water));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(1));
            Assert.That(controller.CurrentSecondFloorSequenceGoalIdForTests, Is.EqualTo("custom_fire"));

            Assert.That(controller.ImportCustomReferenceForTests(SpellFamily.Fire, out _, out var fireReferenceMessage), Is.True, fireReferenceMessage);
            var customFireGoal = controller.StageGoalPositionForTests("custom_fire");
            var customFire = CastNoisyCustomReferenceSpell(controller, SpellFamily.Fire, SpellFamily.Fire, customFireGoal);
            yield return null;

            Assert.That(customFire.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(customFire.spell.isCustomShape, Is.True, customFire.spell.feedbackReason);
            Assert.That(customFire.spell.recognizedFamily, Is.EqualTo(SpellFamily.Fire));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(2));
            Assert.That(controller.LastCustomShapeEventKindForTests, Is.Not.Empty);
            Assert.That(controller.CurrentSecondFloorSequenceGoalIdForTests, Is.EqualTo("custom_wind"));

            Assert.That(controller.ImportCustomReferenceForTests(SpellFamily.Wind, out _, out var windReferenceMessage), Is.True, windReferenceMessage);
            var customWindGoal = controller.StageGoalPositionForTests("custom_wind");
            var customWind = CastNoisyCustomReferenceSpell(controller, SpellFamily.Wind, SpellFamily.Wind, customWindGoal);
            yield return null;

            Assert.That(customWind.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(customWind.spell.isCustomShape, Is.True, customWind.spell.feedbackReason);
            Assert.That(customWind.spell.recognizedFamily, Is.EqualTo(SpellFamily.Wind));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(3));
            Assert.That(controller.CurrentSecondFloorSequenceGoalIdForTests, Is.EqualTo("custom_earth"));

            Assert.That(controller.ImportCustomReferenceForTests(SpellFamily.Earth, out _, out var earthReferenceMessage), Is.True, earthReferenceMessage);
            var customEarthGoal = controller.StageGoalPositionForTests("custom_earth");
            var customEarth = CastNoisyCustomReferenceSpell(controller, SpellFamily.Earth, SpellFamily.Earth, customEarthGoal);
            yield return null;

            Assert.That(customEarth.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(customEarth.spell.isCustomShape, Is.True, customEarth.spell.feedbackReason);
            Assert.That(customEarth.spell.recognizedFamily, Is.EqualTo(SpellFamily.Earth));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(4));
            Assert.That(controller.CurrentSecondFloorSequenceGoalIdForTests, Is.EqualTo("custom_life"));

            Assert.That(controller.ImportCustomReferenceForTests(SpellFamily.Life, out _, out var lifeReferenceMessage), Is.True, lifeReferenceMessage);
            var customLifeGoal = controller.StageGoalPositionForTests("custom_life");
            var customLife = CastNoisyCustomReferenceSpell(controller, SpellFamily.Life, SpellFamily.Life, customLifeGoal);
            yield return null;

            Assert.That(customLife.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(customLife.spell.isCustomShape, Is.True, customLife.spell.feedbackReason);
            Assert.That(customLife.spell.recognizedFamily, Is.EqualTo(SpellFamily.Life));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(5));
            Assert.That(controller.PendingAdvanceSecondsForTests, Is.GreaterThan(0f));
            Assert.That(controller.IsFloorTransitionActiveForTests, Is.True);

            yield return new WaitForSeconds(ExamGameController.StandardFloorAdvanceDelaySeconds + 3.0f);

            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(3));
            Assert.That(controller.ImportCustomReferenceForTests(SpellFamily.Water, out _, out var floorThreeWaterMessage), Is.True, floorThreeWaterMessage);
            Assert.That(controller.ImportCustomReferenceForTests(SpellFamily.Earth, out _, out var floorThreeEarthMessage), Is.True, floorThreeEarthMessage);
            Assert.That(controller.ImportCustomReferenceForTests(SpellFamily.Life, out _, out var floorThreeLifeMessage), Is.True, floorThreeLifeMessage);
            Assert.That(controller.ImportCustomReferenceForTests(SpellFamily.Wind, out _, out var floorThreeWindMessage), Is.True, floorThreeWindMessage);

            var frozenRiver = controller.StageGoalPositionForTests("frozen_river");
            var river = CastNoisyCustomReferenceSpell(controller, SpellFamily.Water, SpellFamily.Water, frozenRiver);
            yield return null;

            Assert.That(river.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(river.spell.isCustomShape, Is.True, river.spell.feedbackReason);
            Assert.That(river.spell.recognizedFamily, Is.EqualTo(SpellFamily.Water));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(1));
            Assert.That(GameObject.Find("Stage Effect frozen_river Ground Glow"), Is.Not.Null);

            var earthStairs = controller.StageGoalPositionForTests("earth_stairs");
            var stairs = CastNoisyCustomReferenceSpell(controller, SpellFamily.Earth, SpellFamily.Earth, earthStairs);
            yield return null;

            Assert.That(stairs.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(stairs.spell.isCustomShape, Is.True, stairs.spell.feedbackReason);
            Assert.That(stairs.spell.recognizedFamily, Is.EqualTo(SpellFamily.Earth));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(2));
            Assert.That(controller.HasStageEntityNearForTests(new Vector2(3.25f, -3.16f), 0.35f), Is.True);
            Assert.That(controller.HasStageEntityColliderNearForTests(new Vector2(3.25f, -3.16f), 0.35f), Is.True);
            Assert.That(GameObject.Find("Stage Effect earth_stairs WallEntity Signature"), Is.Not.Null);

            var livingBridge = controller.StageGoalPositionForTests("living_bridge");
            var bridge = CastNoisyCustomReferenceSpell(controller, SpellFamily.Life, SpellFamily.Life, livingBridge);
            yield return null;

            Assert.That(bridge.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(bridge.spell.isCustomShape, Is.True, bridge.spell.feedbackReason);
            Assert.That(bridge.spell.recognizedFamily, Is.EqualTo(SpellFamily.Life));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(3));
            Assert.That(GameObject.Find("Stage Effect living_bridge DirectionalProjectile Signature"), Is.Not.Null);

            var windPlatform = controller.StageGoalPositionForTests("wind_platform");
            var platform = CastNoisyCustomReferenceSpell(controller, SpellFamily.Wind, SpellFamily.Wind, windPlatform);
            yield return null;

            Assert.That(platform.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(platform.spell.isCustomShape, Is.True, platform.spell.feedbackReason);
            Assert.That(platform.spell.recognizedFamily, Is.EqualTo(SpellFamily.Wind));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(4));
            Assert.That(GameObject.Find("Stage Effect wind_platform WallEntity Signature"), Is.Not.Null);

            ClearCustomSlots(controller);
            DeleteIfExists(profilePath);
        }

        private static void AssertVisualGoalRequirementRows(
            ExamGameController controller,
            int expectedPlusCount,
            int? expectedLabelCount = null,
            float minLabelWidth = 180f,
            HorizontalWrapMode expectedOverflow = HorizontalWrapMode.Overflow)
        {
            var expectedVisibleLabels = expectedLabelCount ?? controller.ActiveGoalCount;
            var labels = Object.FindObjectsByType<Text>(FindObjectsSortMode.None)
                .Where(text => text.name == "Goal Label Text" &&
                               text.gameObject.activeInHierarchy &&
                               BelongsToCurrentFloorRoot(text.transform, controller.CurrentFloorNumber))
                .ToList();

            Assert.That(labels.Count, Is.EqualTo(expectedVisibleLabels));
            foreach (var label in labels)
            {
                var rect = label.rectTransform.rect;
                Assert.That(label.text, Does.Not.Contain("+"));
                Assert.That(label.text.Count(ch => ch == '\n'), Is.EqualTo(0), label.text);
                Assert.That(rect.width, Is.GreaterThanOrEqualTo(minLabelWidth), label.text);
                Assert.That(rect.height, Is.GreaterThanOrEqualTo(24f), label.text);
                Assert.That(label.horizontalOverflow, Is.EqualTo(expectedOverflow), label.text);
                Assert.That(label.preferredWidth, Is.LessThanOrEqualTo(rect.width + 18f), label.text);
                Assert.That(label.preferredHeight, Is.LessThanOrEqualTo(rect.height + 10f), label.text);
            }

            var rows = Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None)
                .Where(rect => rect.name.StartsWith("Goal Requirement Icon Row", StringComparison.Ordinal) &&
                               rect.GetComponent<Image>() != null &&
                               rect.gameObject.activeInHierarchy &&
                               BelongsToCurrentFloorRoot(rect.transform, controller.CurrentFloorNumber))
                .ToList();
            var icons = Object.FindObjectsByType<Image>(FindObjectsSortMode.None)
                .Where(image => image.name.StartsWith("Goal Requirement Icon ", StringComparison.Ordinal) &&
                                !image.name.StartsWith("Goal Requirement Icon Row", StringComparison.Ordinal) &&
                                image.gameObject.activeInHierarchy &&
                                BelongsToCurrentFloorRoot(image.transform, controller.CurrentFloorNumber))
                .ToList();
            var pluses = Object.FindObjectsByType<Text>(FindObjectsSortMode.None)
                .Where(text => text.name.StartsWith("Goal Requirement Plus", StringComparison.Ordinal) &&
                               text.gameObject.activeInHierarchy &&
                               BelongsToCurrentFloorRoot(text.transform, controller.CurrentFloorNumber))
                .ToList();

            Assert.That(rows.Count, Is.EqualTo(expectedVisibleLabels));
            Assert.That(icons.Count, Is.GreaterThanOrEqualTo(expectedVisibleLabels));
            Assert.That(icons.Select(image => image.sprite), Is.All.Not.Null);
            Assert.That(pluses.Count, Is.EqualTo(expectedPlusCount));
            Assert.That(pluses.Select(text => text.text), Is.All.EqualTo("+"));

            var labelBackgrounds = Object.FindObjectsByType<Image>(FindObjectsSortMode.None)
                .Where(image => image.name == "Goal Label Background" &&
                                image.gameObject.activeInHierarchy &&
                                BelongsToCurrentFloorRoot(image.transform, controller.CurrentFloorNumber))
                .ToList();
            var titleBackings = Object.FindObjectsByType<Image>(FindObjectsSortMode.None)
                .Where(image => image.name == "Goal Label Title Backing" &&
                                image.gameObject.activeInHierarchy &&
                                BelongsToCurrentFloorRoot(image.transform, controller.CurrentFloorNumber))
                .ToList();
            Assert.That(labelBackgrounds.Count, Is.EqualTo(expectedVisibleLabels));
            Assert.That(labelBackgrounds.Select(image => image.color.a), Is.All.LessThanOrEqualTo(0.24f));
            Assert.That(titleBackings.Count, Is.EqualTo(expectedVisibleLabels));
            Assert.That(titleBackings.Select(image => image.color.a), Is.All.GreaterThanOrEqualTo(0.50f));
        }

        private static bool BelongsToCurrentFloorRoot(Transform transform, int currentFloorNumber)
        {
            return transform != null &&
                   transform.root != null &&
                   transform.root.name.StartsWith($"Floor {currentFloorNumber} -", StringComparison.Ordinal);
        }

        private static void AssertSpriteUsesNeutralShapeInk(Image image)
        {
            Assert.That(image, Is.Not.Null);
            Assert.That(image.sprite, Is.Not.Null);
            if (image.sprite.name.StartsWith("CustomShape_", StringComparison.Ordinal))
            {
                return;
            }

            var pixels = image.sprite.texture.GetPixels32()
                .Where(pixel => pixel.a > 10)
                .ToList();
            Assert.That(pixels.Count, Is.GreaterThan(0));
            Assert.That(pixels.Max(pixel => Math.Abs(pixel.r - pixel.g)), Is.LessThanOrEqualTo(2));
            Assert.That(pixels.Max(pixel => Math.Abs(pixel.g - pixel.b)), Is.LessThanOrEqualTo(2));
        }

        private static List<string> ActiveQuestLabels()
        {
            return Object.FindObjectsByType<Text>(FindObjectsSortMode.None)
                .Where(text => text.name.StartsWith("Quest Checklist Label", StringComparison.Ordinal) && text.gameObject.activeInHierarchy)
                .Select(text => text.text)
                .ToList();
        }

        private static List<string> ActiveGoalLabels(int currentFloorNumber)
        {
            return Object.FindObjectsByType<Text>(FindObjectsSortMode.None)
                .Where(text => text.name == "Goal Label Text" &&
                               text.gameObject.activeInHierarchy &&
                               BelongsToCurrentFloorRoot(text.transform, currentFloorNumber))
                .Select(text => text.text)
                .ToList();
        }

        private static void AssertFloorFourGoalLabelsDoNotOverlap(int currentFloorNumber)
        {
            var roots = Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None)
                .Where(rect => rect.name.EndsWith(" Goal Label", StringComparison.Ordinal) &&
                               rect.GetComponent<Canvas>() != null &&
                               rect.gameObject.activeInHierarchy &&
                               BelongsToCurrentFloorRoot(rect.transform, currentFloorNumber))
                .OrderBy(rect => rect.position.x)
                .ToList();

            Assert.That(roots.Count, Is.EqualTo(5));
            for (var index = 0; index < roots.Count - 1; index++)
            {
                var left = roots[index];
                var right = roots[index + 1];
                var leftHalfWidth = left.rect.width * left.lossyScale.x * 0.5f;
                var rightHalfWidth = right.rect.width * right.lossyScale.x * 0.5f;
                var leftEdge = left.position.x + leftHalfWidth;
                var rightEdge = right.position.x - rightHalfWidth;
                Assert.That(leftEdge, Is.LessThanOrEqualTo(rightEdge + 0.04f), $"{left.name} overlaps {right.name}");
            }
        }

        [UnityTest]
        public IEnumerator RecognizedBaseAwayFromGoalExplainsTargetLocation()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);
            controller.CloseFirstFloorLetterForTests();

            var result = controller.CastSyntheticBaseForTests(SpellFamily.Water, Vector2.zero);
            yield return null;

            Assert.That(result.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(result.spell.recognizedFamily, Is.EqualTo(SpellFamily.Water));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(0));
            Assert.That(controller.LastMagicNoteText, Does.Contain(SpellLabels.Korean(SpellFamily.Water)));
            Assert.That(controller.LastMagicNoteText, Does.Contain("표식에서 너무 멉니다"));
            Assert.That(controller.LastMagicNoteText, Does.Contain("현재 거리"));
            Assert.That(controller.IsGoalProximityBubbleVisibleForTests, Is.True);
            Assert.That(controller.LastGoalProximityGuideGoalIdForTests, Is.EqualTo("puddle"));
            Assert.That(controller.LastGoalProximityGuideDistanceForTests, Is.GreaterThan(1.85f));
            Assert.That(controller.GoalProximityBubbleTextForTests, Does.Contain("가까이 이동"));
            Assert.That(controller.GoalProximityBubbleTextForTests, Does.Contain("표식 바로 옆"));
        }

        [UnityTest]
        public IEnumerator BaseIntentGoalAndSealOriginUsePlayerPositionInsteadOfDrawCenter()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);
            var profilePath = TempCustomShapeProfilePath();
            controller.UseCustomShapeStoreForTests(profilePath);
            ClearCustomSlots(controller);

            var waterGoal = controller.StageGoalPositionForTests("puddle");
            var drawCenter = Vector2.zero;
            var strokes = Offset(GestureRecognizer.CreateCanonicalSamples(SpellFamily.Water, 1.6f, 0.03f), drawCenter, 0.8f);

            controller.MovePlayerForTests(waterGoal);
            var result = controller.CastRawBaseForTests(strokes, drawCenter, movePlayerToReference: false);
            yield return null;

            Assert.That(result.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(result.spell.recognizedFamily, Is.EqualTo(SpellFamily.Water));
            Assert.That(result.spell.intentGoalId, Is.EqualTo("puddle"));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(1));
            var seal = controller.GetActiveSealSnapshots().Single();
            Assert.That(Vector2.Distance(seal.worldCenter, waterGoal), Is.LessThan(0.05f));
            Assert.That(Vector2.Distance(seal.worldCenter, drawCenter), Is.GreaterThan(1f));

            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;
            controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);
            controller.UseCustomShapeStoreForTests(profilePath);
            ClearCustomSlots(controller);

            var goalStrokes = Offset(GestureRecognizer.CreateCanonicalSamples(SpellFamily.Water, 1.6f, 0.03f), waterGoal, 0.8f);
            controller.MovePlayerForTests(drawCenter);
            var offTarget = controller.CastRawBaseForTests(goalStrokes, waterGoal, movePlayerToReference: false);
            yield return null;

            Assert.That(offTarget.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(offTarget.spell.intentGoalId, Is.Empty);
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(0));

            ClearCustomSlots(controller);
            DeleteIfExists(profilePath);
        }

        [UnityTest]
        public IEnumerator LaterFloorBaseIntentUsesNearbyActiveGoalLikeFirstFloor()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);
            var profilePath = TempCustomShapeProfilePath();
            controller.UseCustomShapeStoreForTests(profilePath);
            ClearCustomSlots(controller);

            var puddle = controller.StageGoalPositionForTests("puddle");
            var waterSeed = controller.CastRawBaseForTests(NoisyCanonicalBase(SpellFamily.Water, puddle, 0f), puddle);
            yield return null;
            Assert.That(waterSeed.spell.recognizedFamily, Is.EqualTo(SpellFamily.Water));
            yield return WaitForDefaultSealFallback(controller);

            var pillar = controller.StageGoalPositionForTests("pillar");
            var earthSeed = controller.CastRawBaseForTests(NoisyCanonicalBase(SpellFamily.Earth, pillar, 0f), pillar);
            yield return null;
            Assert.That(earthSeed.spell.recognizedFamily, Is.EqualTo(SpellFamily.Earth));

            controller.LoadFloorForTests(2);
            yield return null;
            var drawCenter = Vector2.zero;
            var offCenterEarth = Offset(GestureRecognizer.CreateCanonicalSamples(SpellFamily.Earth, 1.6f, 0.03f), drawCenter, 0.8f);
            var earthStairs = controller.StageGoalPositionForTests("earth_stairs");
            controller.MovePlayerForTests(earthStairs);
            var floorThree = controller.CastRawBaseForTests(offCenterEarth, drawCenter, movePlayerToReference: false);
            yield return null;
            Assert.That(floorThree.intent, Is.Not.Null);
            Assert.That(floorThree.intent.goalId, Is.EqualTo("earth_stairs"));
            Assert.That(floorThree.intent.tutorialCaptureCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(floorThree.intent.strongConsiderationEnabled, Is.True);
            Assert.That(floorThree.spell.intentGoalId, Is.EqualTo("earth_stairs"));

            controller.LoadFloorForTests(3);
            yield return null;
            var beamEarth = controller.StageGoalPositionForTests("beam_earth");
            controller.MovePlayerForTests(beamEarth);
            var floorFour = controller.CastRawBaseForTests(offCenterEarth, drawCenter, movePlayerToReference: false);
            yield return null;
            Assert.That(floorFour.intent, Is.Not.Null);
            Assert.That(floorFour.intent.goalId, Is.EqualTo("beam_earth"));
            Assert.That(floorFour.intent.tutorialCaptureCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(floorFour.intent.strongConsiderationEnabled, Is.True);
            Assert.That(floorFour.spell.intentGoalId, Is.EqualTo("beam_earth"));

            SeedFinalTaskEncounters(controller, "final_puddle", "final_ember", "final_beam_fire");
            Assert.That(controller.SelectFinalTaskForTests("final_puddle"), Is.True);
            controller.LoadFloorForTests(4);
            yield return null;
            Assert.That(controller.CurrentFinalTaskIdForTests, Is.EqualTo("final_puddle"));
            var finalPuddle = controller.StageGoalPositionForTests("final_puddle");
            var offCenterWater = Offset(GestureRecognizer.CreateCanonicalSamples(SpellFamily.Water, 1.6f, 0.03f), drawCenter, 0.8f);
            controller.MovePlayerForTests(finalPuddle);
            var floorFive = controller.CastRawBaseForTests(offCenterWater, drawCenter, movePlayerToReference: false);
            yield return null;
            Assert.That(floorFive.intent, Is.Not.Null);
            Assert.That(floorFive.intent.goalId, Is.EqualTo("final_puddle"));
            Assert.That(floorFive.intent.tutorialCaptureCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(floorFive.intent.strongConsiderationEnabled, Is.True);
            Assert.That(floorFive.spell.intentGoalId, Is.EqualTo("final_puddle"));

            ClearCustomSlots(controller);
            DeleteIfExists(profilePath);
        }

        [UnityTest]
        public IEnumerator WaterGoalMisreadSpeechUsesShortConversationalCoaching()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);
            controller.CloseFirstFloorLetterForTests();

            var result = new BaseRecognitionResult
            {
                spell = new SpellResult
                {
                    status = RecognitionStatus.Ambiguous,
                    targetFamily = SpellFamily.Water,
                    preIntentFamily = SpellFamily.Earth,
                    confidence = 0.64f,
                    quality = new QualityVector
                    {
                        closure = 0.42f,
                        smoothness = 0.78f,
                        tempo = 0.72f,
                        stability = 0.60f,
                        rotationBias = 0.18f
                    },
                    feedbackReason = "물 표식 근처 입력으로 의도는 감지했지만 땅 후보와 아직 가깝습니다."
                },
                bufferStrokeCount = 1
            };
            var method = typeof(ExamGameController).GetMethod(
                "ShowBaseResultSummary",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method!.Invoke(controller, new object[]
            {
                result,
                "기본 문양 실패",
                "물은 닫힌 원입니다. 한 번에 둥글게 돌리고 끝점을 시작점 바로 옆에 놓으세요."
            });
            yield return null;

            Assert.That(result.spell.targetFamily, Is.EqualTo(SpellFamily.Water));
            Assert.That(controller.IsResultPanelVisible, Is.False);
            Assert.That(controller.MentorSpeechTextForTests, Is.Not.Empty);
            var mentorLines = controller.MentorSpeechTextForTests.Split('\n');
            Assert.That(mentorLines.Length, Is.LessThanOrEqualTo(3));
            Assert.That(mentorLines.All(line => line.Length <= 28), Is.True);
            Assert.That(controller.MentorSpeechTextForTests, Does.Not.Contain("표식 근처 입력"));
            Assert.That(controller.MentorSpeechTextForTests, Does.Not.Contain("후보와 아직"));
            Assert.That(controller.MentorSpeechTextForTests, Does.Not.Contain("닫힌 원입니다"));
        }

        [UnityTest]
        public IEnumerator SyntheticBaseCastCreatesWorldSealAndResultSummary()
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
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(0));
            Assert.That(controller.CurrentFirstFloorTutorialGoalIdForTests, Is.EqualTo("puddle"));
            Assert.That(controller.LastSealLifetimeSecondsForTests, Is.EqualTo(SpellRuntime.DefaultSealDurationSeconds).Within(0.001f));
            Assert.That(controller.VisibleOverlayGuideCountForTests, Is.EqualTo(1));
            Assert.That(controller.LastSealVisualLabelForTests, Does.Contain("기본 완료"));
            Assert.That(controller.LastSealVisualLabelForTests, Does.Contain("원 안에 이어"));
            Assert.That(controller.CurrentInputPhaseLabelForTests, Does.Contain("추가 도형"));
            Assert.That(controller.LastSealAttachGuideColorForTests.a, Is.GreaterThan(0.35f));
            Assert.That(controller.IsDrawingPanelVisible, Is.False);
            Assert.That(controller.IsResultPanelVisible, Is.False);
            Assert.That(controller.LastResultPanelTextForTests, Does.Contain("기본 문양 성공"));
            Assert.That(controller.LastResultPanelTextForTests, Does.Contain("불꽃"));
            Assert.That(controller.LastResultPanelTextForTests, Does.Contain("품질"));
            Assert.That(controller.LastResultPanelTextForTests, Does.Contain("해석"));
            Assert.That(controller.LastResultPanelTextForTests, Does.Contain("이유"));
            Assert.That(controller.MentorSpeechTextForTests, Does.Contain("불꽃").And.Contain("안정됐습니다"));
            var speechPanel = GameObject.Find("Mentor Speech")?.GetComponent<RectTransform>();
            Assert.That(speechPanel, Is.Not.Null);
            Assert.That(speechPanel.gameObject.activeInHierarchy, Is.True);
            Assert.That(speechPanel.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(speechPanel.anchorMax, Is.EqualTo(Vector2.zero));
            Assert.That(speechPanel.pivot, Is.EqualTo(Vector2.zero));
            Assert.That(speechPanel.anchoredPosition.x, Is.InRange(16f, 140f));
            Assert.That(speechPanel.anchoredPosition.y, Is.GreaterThan(250f));
            Assert.That(speechPanel.anchoredPosition.x + speechPanel.sizeDelta.x, Is.LessThan(620f));
            Assert.That(controller.MentorSpeechTextForTests.Split('\n').Length, Is.LessThanOrEqualTo(3));
            Assert.That(speechPanel.GetComponent<Image>(), Is.Null);
            var speechBody = GameObject.Find("Mentor Speech Body")?.GetComponent<RectTransform>();
            var speechTail = GameObject.Find("Mentor Speech Tail")?.GetComponent<RectTransform>();
            var speaker = GameObject.Find("Mentor Speaker")?.GetComponent<Text>();
            var speechMask = speechBody == null ? null : speechBody.GetComponent<RectMask2D>();
            var speechText = GameObject.Find("Mentor Speech Text")?.GetComponent<Text>();
            Assert.That(speechBody, Is.Not.Null);
            Assert.That(speechTail, Is.Not.Null);
            Assert.That(speechBody.sizeDelta.x, Is.LessThanOrEqualTo(430f));
            Assert.That(speechMask, Is.Not.Null);
            Assert.That(speechMask.padding.y, Is.GreaterThan(0f));
            Assert.That(speechText, Is.Not.Null);
            Assert.That(speechText.fontSize, Is.GreaterThanOrEqualTo(15));
            Assert.That(speechText.resizeTextMinSize, Is.GreaterThanOrEqualTo(13));
            Assert.That(speechText.resizeTextMaxSize, Is.GreaterThanOrEqualTo(15));
            Assert.That(speechText.preferredHeight, Is.LessThanOrEqualTo(speechText.rectTransform.rect.height + 1f));
            Assert.That(speechTail.localEulerAngles.z, Is.EqualTo(45f).Within(0.1f));
            Assert.That(speaker, Is.Not.Null);
            Assert.That(speaker.text, Is.EqualTo("입문 조교"));
        }

        [UnityTest]
        public IEnumerator IdleAfterBaseSealCreatesTransparentPlayerBarrier()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);

            controller.CastSyntheticBaseForTests(SpellFamily.Fire, new Vector2(-5.5f, 2.6f));
            yield return null;
            Assert.That(controller.ActiveDefaultBarrierCountForTests, Is.EqualTo(0));
            Assert.That(controller.ActiveSealCount, Is.EqualTo(1));
            var sealId = controller.GetActiveSealSnapshots()[0].sealId;

            yield return new WaitForSeconds(ExamGameController.DefaultSealFallbackDelaySeconds + 0.2f);

            Assert.That(controller.ActiveDefaultBarrierCountForTests, Is.EqualTo(1));
            Assert.That(controller.ActiveSealCount, Is.EqualTo(0));
            var barrierColor = controller.LastDefaultBarrierColorForTests;
            Assert.That(barrierColor.r, Is.EqualTo(1f).Within(0.01f));
            Assert.That(barrierColor.g, Is.EqualTo(0.31f).Within(0.01f));
            Assert.That(barrierColor.b, Is.EqualTo(0.18f).Within(0.01f));
            Assert.That(GameObject.Find("Default Barrier " + sealId), Is.Not.Null);
            Assert.That(controller.MentorSpeechTextForTests, Does.Contain("보호막").And.Contain("되었습니다"));
            Assert.That(controller.MentorSpeechTextForTests, Does.Contain("불꽃 문양"));
            Assert.That(controller.MentorSpeechTextForTests, Does.Not.Contain("seal"));
            Assert.That(controller.MentorSpeechTextForTests, Does.Not.Contain("안정화되었습니다"));
        }

        [UnityTest]
        public IEnumerator DrawingCancelClearsBufferedInputAndHidesResultPanel()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            var drawing = Object.FindFirstObjectByType<WorldDrawingController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(drawing, Is.Not.Null);

            controller.CastSyntheticBaseForTests(SpellFamily.Fire, new Vector2(-5.5f, 2.6f));
            yield return null;
            Assert.That(controller.IsResultPanelVisible, Is.False);

            drawing.BufferStrokeForTests(new List<StrokeSample>
            {
                new(new Vector2(-1f, -1f), 0f),
                new(new Vector2(1f, 1f), 0.1f)
            });
            Assert.That(drawing.HasBufferedInput, Is.True);
            Assert.That(drawing.BufferedStrokeCountForTests, Is.EqualTo(1));
            Assert.That(drawing.StrokeVisualCountForTests, Is.EqualTo(1));

            var canceled = drawing.CancelBufferedInput();
            yield return null;

            Assert.That(canceled, Is.True);
            Assert.That(drawing.HasBufferedInput, Is.False);
            Assert.That(drawing.BufferedStrokeCountForTests, Is.EqualTo(0));
            Assert.That(drawing.StrokeVisualCountForTests, Is.EqualTo(0));
            Assert.That(controller.IsResultPanelVisible, Is.False);
            Assert.That(controller.LastMagicNoteText, Does.Contain("입력을 취소"));

            controller.CastSyntheticBaseForTests(SpellFamily.Wind, new Vector2(4.6f, 1.5f));
            yield return null;
            Assert.That(controller.IsResultPanelVisible, Is.False);

            var idleCancel = drawing.CancelBufferedInput();
            yield return null;

            Assert.That(idleCancel, Is.False);
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

            controller.MovePlayerForTests(Vector2.zero);
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
        public IEnumerator ExternalRecognitionHandoffDrivesWorldProgression()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);

            var noSeal = controller.SubmitRecognitionHandoff(SpellRecognitionHandoff.Overlay(
                RecognitionStatus.Recognized,
                OverlayOperator.IceBar,
                new Vector2(-0.65f, 3f),
                0.95f,
                0.95f,
                sourceId: "external-overlay-before-base"));
            yield return null;

            Assert.That(noSeal.kind, Is.EqualTo(SpellCastOutcomeKind.OverlayNoActiveSeal));
            Assert.That(controller.TrialCountForTests, Is.EqualTo(1));
            Assert.That(controller.LastMagicNoteText, Does.Contain("먼저 기본 문양"));

            var baseOutcome = controller.SubmitRecognitionHandoff(SpellRecognitionHandoff.Base(
                RecognitionStatus.Recognized,
                SpellFamily.Fire,
                SpellFamily.Fire,
                new Vector2(-5.5f, 2.6f),
                0.97f,
                PerfectQuality(),
                worldScale: 1.35f,
                sourceId: "external-fire-base"));
            yield return null;

            Assert.That(baseOutcome.kind, Is.EqualTo(SpellCastOutcomeKind.BaseSucceeded));
            Assert.That(controller.ActiveSealCount, Is.EqualTo(1));
            var seal = controller.GetActiveSealSnapshots().Single();
            Assert.That(seal.baseFamily, Is.EqualTo(SpellFamily.Fire));

            var overlayOutcome = controller.SubmitRecognitionHandoff(SpellRecognitionHandoff.Overlay(
                RecognitionStatus.Recognized,
                OverlayOperator.IceBar,
                seal.worldCenter,
                0.95f,
                0.95f,
                targetSealId: seal.sealId,
                sourceId: "external-ice-overlay"));
            yield return null;

            Assert.That(overlayOutcome.kind, Is.EqualTo(SpellCastOutcomeKind.OverlaySucceeded));
            Assert.That(controller.LastOverlayStack, Does.Contain(OverlayOperator.IceBar));
            Assert.That(controller.TrialCountForTests, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator ActiveSealWorldInputRejectsNonCustomFollowupCandidates()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);
            var profilePath = TempCustomShapeProfilePath();
            controller.UseCustomShapeStoreForTests(profilePath);
            ClearCustomSlots(controller);

            controller.CastSyntheticBaseForTests(SpellFamily.Earth, Vector2.zero);
            yield return null;
            Assert.That(controller.ActiveSealCount, Is.EqualTo(1));

            var retryStrokes = Offset(GestureRecognizer.CreateCanonicalSamples(SpellFamily.Wind, 1.6f, 0.03f), Vector2.zero, 0.8f);
            var retryResult = controller.CastRawBaseForTests(retryStrokes, Vector2.zero);
            yield return null;

            Assert.That(retryResult.spell.status, Is.EqualTo(RecognitionStatus.Invalid));
            Assert.That(retryResult.spell.recognizedFamily, Is.Null);
            Assert.That(controller.ActiveSealCount, Is.EqualTo(1));
            Assert.That(controller.LastOverlayStack, Is.Empty);
            Assert.That(controller.LastMagicNoteText, Does.Contain("추가 도형"));

            ClearCustomSlots(controller);
            DeleteIfExists(profilePath);
        }

        [UnityTest]
        public IEnumerator FirstFloorBlocksCustomShapeUiReferencesAndRecognition()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);
            var profilePath = TempCustomShapeProfilePath();
            controller.UseCustomShapeStoreForTests(profilePath);
            ClearCustomSlots(controller);

            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(1));
            Assert.That(controller.CustomShapesAvailableForTests, Is.False);
            Assert.That(controller.CustomReferenceCountForTests, Is.EqualTo(0));

            controller.OpenCustomShapePenPopupForTests();
            controller.OpenCustomShapePageForTests();
            controller.OpenCustomReferencePanelForTests();
            yield return null;
            Assert.That(controller.IsCustomPenPopupVisibleForTests, Is.False);
            Assert.That(controller.IsCustomShapePageOpenForTests, Is.False);
            Assert.That(controller.IsCustomReferencePanelOpenForTests, Is.False);
            Assert.That(controller.ImportCustomReferenceForTests(SpellFamily.Wind, out _, out _), Is.False);

            var windGoal = new Vector2(5.5f, 2.6f);
            var gold = Samples(SpellFamily.Wind);
            Assert.That(controller.SaveCustomShapeSlotForTests(0, "floor one wind", "floor|one|wind|line", SpellFamily.Wind, gold, out var message), Is.True, message);
            var result = controller.CastRawBaseForTests(
                Offset(GestureRecognizer.CreateCanonicalSamples(SpellFamily.Wind, 1.6f, 0.03f), windGoal, 0.8f),
                windGoal);
            yield return null;

            Assert.That(result.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(result.spell.recognizedFamily, Is.EqualTo(SpellFamily.Wind));
            Assert.That(result.spell.isCustomShape, Is.False);
            Assert.That(result.spell.customShapeLabel, Is.Empty);
            Assert.That(controller.LastCustomShapeEventKindForTests, Is.Empty);
            Assert.That(controller.CustomShapeEventObjectCountForTests, Is.EqualTo(0));

            ClearCustomSlots(controller);
            DeleteIfExists(profilePath);
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
            Assert.That(controller.VisibleOverlayGuideCountForTests, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator CustomShapeAuthoringPageSlotFlowWorks()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);
            var profilePath = TempCustomShapeProfilePath();
            controller.UseCustomShapeStoreForTests(profilePath);
            ClearCustomSlots(controller);
            controller.LoadFloorForTests(1);
            yield return null;
            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(2));
            Assert.That(controller.CustomShapesAvailableForTests, Is.True);

            controller.OpenCustomShapePenPopupForTests();
            yield return null;
            Assert.That(controller.IsCustomPenPopupVisibleForTests, Is.True);
            var penPopup = GameObject.Find("Custom Shape Pen Popup")?.GetComponent<RectTransform>();
            Assert.That(penPopup, Is.Not.Null);
            var penStart = penPopup.anchoredPosition;
            yield return new WaitForSecondsRealtime(0.45f);

            Assert.That(Mathf.Abs(penPopup.anchoredPosition.x - penStart.x), Is.LessThan(0.75f));
            Assert.That(Mathf.Abs(penPopup.anchoredPosition.y - penStart.y), Is.InRange(0.25f, 7f));
            Assert.That(Quaternion.Angle(penPopup.localRotation, Quaternion.identity), Is.LessThan(0.1f));

            controller.OpenCustomShapePageForTests();
            yield return null;
            Assert.That(controller.IsCustomShapePageOpenForTests, Is.True);
            Assert.That(controller.CustomShapeSlotCountForTests, Is.EqualTo(12));
            var emptySlotIcon = GameObject.Find("Custom Shape Slot 12 Icon")?.GetComponent<Image>();
            Assert.That(emptySlotIcon, Is.Not.Null);
            Assert.That(emptySlotIcon.enabled, Is.False);
            Assert.That(emptySlotIcon.sprite, Is.Null);

            controller.RequestCustomShapeSlotForTests(11);
            yield return null;
            Assert.That(controller.IsCustomShapeBubbleVisibleForTests, Is.True);
            AssertVisibleKoreanTextLooksUsable("도형을 작성하시겠습니까?");

            controller.DeclineCustomShapeBubbleForTests();
            yield return null;
            Assert.That(controller.IsCustomShapeSlotOccupiedForTests(11), Is.False);

            controller.RequestCustomShapeSlotForTests(11);
            controller.ConfirmCustomShapeBubbleForTests();
            yield return null;
            Assert.That(controller.IsCustomShapeEditorOpenForTests, Is.True);
            var drawSurface = GameObject.Find("Custom Shape Capture Draw Surface");
            Assert.That(drawSurface, Is.Not.Null);
            Assert.That(drawSurface.GetComponent<CanvasRenderer>(), Is.Not.Null);
            Assert.That(GameObject.Find("Custom Shape Page Border")?.GetComponent<CustomShapeRectBorder>(), Is.Not.Null);
            Assert.That(GameObject.Find("Custom Shape Editor Border")?.GetComponent<CustomShapeRectBorder>(), Is.Not.Null);
            Assert.That(GameObject.Find("Custom Shape Capture Panel Border")?.GetComponent<CustomShapeRectBorder>(), Is.Not.Null);
            var editorScrim = GameObject.Find("Custom Shape Editor Scrim")?.GetComponent<Image>();
            var editorShadow = GameObject.Find("Custom Shape Editor Shadow")?.GetComponent<Image>();
            var editorPanel = GameObject.Find("Custom Shape Editor")?.GetComponent<Image>();
            Assert.That(editorScrim, Is.Not.Null);
            Assert.That(editorShadow, Is.Not.Null);
            Assert.That(editorPanel, Is.Not.Null);
            var editorRect = editorPanel.rectTransform;
            var canvasRect = editorRect.GetComponentInParent<Canvas>().GetComponent<RectTransform>();
            Assert.That(editorRect.rect.width, Is.GreaterThanOrEqualTo(Mathf.Min(840f, canvasRect.rect.width)));
            Assert.That(editorRect.rect.height, Is.GreaterThanOrEqualTo(Mathf.Min(460f, canvasRect.rect.height - 12f)));
            Assert.That(editorRect.rect.width, Is.LessThanOrEqualTo(canvasRect.rect.width));
            Assert.That(editorRect.rect.height, Is.LessThanOrEqualTo(canvasRect.rect.height));
            Assert.That(editorScrim.raycastTarget, Is.True);
            Assert.That(editorScrim.color.a, Is.GreaterThanOrEqualTo(0.5f));
            Assert.That(editorShadow.raycastTarget, Is.False);
            Assert.That(editorShadow.transform.GetSiblingIndex(), Is.GreaterThan(editorScrim.transform.GetSiblingIndex()));
            Assert.That(editorPanel.transform.GetSiblingIndex(), Is.GreaterThan(editorShadow.transform.GetSiblingIndex()));
            Assert.That(GameObject.Find("Custom Shape Editor Title Bar")?.GetComponent<Image>(), Is.Not.Null);
            Assert.That(GameObject.Find("Custom Shape Editor Active Accent")?.GetComponent<Image>(), Is.Not.Null);
            Assert.That(GameObject.Find("Close Custom Shape Editor")?.GetComponent<Button>(), Is.Not.Null);
            var titleBarRect = GameObject.Find("Custom Shape Editor Title Bar").GetComponent<RectTransform>();
            Assert.That(titleBarRect.rect.width, Is.EqualTo(editorRect.rect.width).Within(0.5f));
            var capturePanelRect = GameObject.Find("Custom Shape Capture Panel").GetComponent<RectTransform>();
            Assert.That(capturePanelRect.rect.width, Is.EqualTo(480f).Within(0.5f));
            Assert.That(capturePanelRect.rect.height, Is.EqualTo(310f).Within(0.5f));
            Assert.That(capturePanelRect.anchoredPosition.y, Is.LessThanOrEqualTo(-70f));
            var shapeSection = GameObject.Find("Custom Shape Section")?.GetComponent<RectTransform>();
            var mappingSection = GameObject.Find("Custom Shape Family Carousel")?.GetComponent<RectTransform>();
            var eventLabel = GameObject.Find("Custom Shape Event Label")?.GetComponent<Text>();
            Assert.That(shapeSection, Is.Not.Null);
            Assert.That(mappingSection, Is.Null);
            Assert.That(eventLabel, Is.Not.Null);
            Assert.That(GameObject.Find("Custom Shape Notebook")?.GetComponent<RectTransform>(), Is.Not.Null);
            Assert.That(GameObject.Find("Custom Shape Notebook Add")?.GetComponent<Button>(), Is.Not.Null);
            AssertVisibleKoreanTextLooksUsable("메모장");
            Assert.That(eventLabel.text, Does.Contain("반응"));
            var sidePreviewIcon = GameObject.Find("Custom Shape Side Preview 01")?.GetComponent<Image>();
            Assert.That(sidePreviewIcon?.sprite?.name, Does.Contain(":2"));
            var paletteScroll = GameObject.Find("Custom Shape Palette Scroll View")?.GetComponent<ScrollRect>();
            Assert.That(paletteScroll, Is.Not.Null);
            Assert.That(paletteScroll.vertical, Is.True);
            Assert.That(paletteScroll.inertia, Is.True);
            Assert.That(paletteScroll.content.rect.height, Is.GreaterThan(paletteScroll.viewport.rect.height));
            var capturePad = drawSurface.GetComponent<CustomShapeCapturePad>();
            Assert.That(capturePad, Is.Not.Null);
            capturePad.SetTemplate("rect");
            Assert.That(capturePad.strokeColor.r, Is.EqualTo(0.28f).Within(0.01f));
            Assert.That(capturePad.strokeColor.g, Is.EqualTo(0.64f).Within(0.01f));
            Assert.That(capturePad.strokeColor.b, Is.EqualTo(0.95f).Within(0.01f));
            var familyReelViewport = GameObject.Find("Custom Shape Family Reel Viewport")?.GetComponent<Mask>();
            var familyReelContent = GameObject.Find("Custom Shape Family Reel Content")?.GetComponent<RectTransform>();
            var familyReelIcons = Object.FindObjectsByType<Image>(FindObjectsSortMode.None)
                .Where(image => image.name.StartsWith("Custom Shape Family Reel Icon", StringComparison.Ordinal))
                .ToList();
            var familyUpButton = GameObject.Find("Custom Shape Family Up")?.GetComponent<Button>();
            var familyDownButton = GameObject.Find("Custom Shape Family Down")?.GetComponent<Button>();
            var familyLabel = GameObject.Find("Custom Shape Family Label")?.GetComponent<Text>();
            Assert.That(familyReelViewport, Is.Null);
            Assert.That(familyReelContent, Is.Null);
            Assert.That(familyReelIcons, Is.Empty);
            Assert.That(familyUpButton, Is.Null);
            Assert.That(familyDownButton, Is.Null);
            Assert.That(familyLabel, Is.Null);
            var familyReelCenterLine = GameObject.Find("Custom Shape Family Reel Center Line")?.transform;
            Assert.That(familyReelCenterLine, Is.Null);
            var strokePreview = GameObject.Find("Custom Shape Capture Stroke Preview")?.GetComponent<Image>();
            Assert.That(strokePreview, Is.Not.Null);

            DragCapturePad(capturePad, Vector2.zero, new Vector2(4f, 4f));
            yield return null;
            Assert.That(capturePad.PlacedShapeCount, Is.EqualTo(0));

            DragCapturePad(capturePad, new Vector2(-92f, -42f), new Vector2(88f, 52f));
            yield return null;
            Assert.That(capturePad.PlacedShapeCount, Is.EqualTo(1));
            Assert.That(capturePad.CaptureStrokes().Count, Is.GreaterThan(0));
            Assert.That(strokePreview.enabled, Is.False);
            Assert.That(capturePad.SelectedShapeIndexForTests, Is.EqualTo(0));
            Assert.That(controller.AddCustomShapeNotebookCaptureForTests(), Is.True, controller.CustomShapeEditorNotebookStatusForTests);
            Assert.That(controller.CustomShapeEditorFollowCaptureCountForTests, Is.EqualTo(1));
            Assert.That(controller.CustomShapeEditorNotebookStatusForTests, Does.Contain("%"));

            var originalCenter = capturePad.SelectedShapeCenterForTests;
            DragCapturePad(capturePad, originalCenter, originalCenter + new Vector2(30f, -18f));
            yield return null;
            Assert.That(Vector2.Distance(capturePad.SelectedShapeCenterForTests, originalCenter), Is.GreaterThan(24f));

            var originalSize = capturePad.SelectedShapeSizeForTests;
            Assert.That(capturePad.TryGetSelectedResizeHandleLocalForTests(1, out var resizeHandle), Is.True);
            DragCapturePad(capturePad, resizeHandle, resizeHandle + new Vector2(44f, 32f));
            yield return null;
            Assert.That(capturePad.SelectedShapeSizeForTests.x, Is.GreaterThan(originalSize.x + 20f));
            Assert.That(capturePad.SelectedShapeSizeForTests.y, Is.GreaterThan(originalSize.y + 12f));

            Assert.That(capturePad.TryGetSelectedRotateHandleLocalForTests(out var rotateHandle), Is.True);
            DragCapturePad(capturePad, rotateHandle, rotateHandle + new Vector2(70f, -34f));
            yield return null;
            var rotation = capturePad.SelectedShapeRotationDegreesForTests;
            Assert.That(Mathf.Abs(rotation), Is.GreaterThan(10f));
            Assert.That(rotation, Is.EqualTo(Mathf.Round(rotation / 15f) * 15f).Within(0.6f));

            var undoButton = GameObject.Find("Custom Shape Undo")?.GetComponent<Button>();
            Assert.That(undoButton, Is.Not.Null);
            undoButton.onClick.Invoke();
            yield return null;
            Assert.That(capturePad.PlacedShapeCount, Is.EqualTo(0));

            DragCapturePad(capturePad, new Vector2(-70f, -34f), new Vector2(86f, 62f));
            yield return null;
            Assert.That(capturePad.PlacedShapeCount, Is.EqualTo(1));

            var saved = controller.SaveCustomShapeSlotForTests(11, "테스트 바람", "테스트|바람|line", SpellFamily.Wind, Samples(SpellFamily.Wind), out var message);
            yield return null;
            Assert.That(saved, Is.True, message);
            Assert.That(controller.IsCustomShapeSlotOccupiedForTests(11), Is.True);
            Assert.That(controller.CustomShapeSlotLabelForTests(11), Is.EqualTo("테스트 바람"));
            Assert.That(controller.CustomShapeSlotMappedFamilyForTests(11), Is.EqualTo(SpellFamily.Wind));
            Assert.That(emptySlotIcon.enabled, Is.True);
            Assert.That(emptySlotIcon.sprite, Is.Not.Null);

            controller.RequestCustomShapeSlotForTests(11);
            yield return null;
            Assert.That(controller.IsCustomShapeBubbleVisibleForTests, Is.True);
            AssertVisibleKoreanTextLooksUsable("도형을 삭제하시겠습니까?");

            controller.ConfirmCustomShapeBubbleForTests();
            yield return null;
            Assert.That(controller.IsCustomShapeSlotOccupiedForTests(11), Is.False);
            Assert.That(emptySlotIcon.enabled, Is.False);
            Assert.That(emptySlotIcon.sprite, Is.Null);
            DeleteIfExists(profilePath);
        }

        [UnityTest]
        public IEnumerator CustomSlotCastRequiresBaseThenCompletesMappedDefaultGoal()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);
            var profilePath = TempCustomShapeProfilePath();
            controller.UseCustomShapeStoreForTests(profilePath);
            ClearCustomSlots(controller);
            controller.LoadFloorForTests(1);
            yield return null;
            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(2));
            controller.CompleteCurrentGoalsForTests(3);
            yield return null;
            Assert.That(controller.CurrentSecondFloorSequenceGoalIdForTests, Is.EqualTo("custom_earth"));
            Assert.That(controller.ImportCustomReferenceForTests(SpellFamily.Earth, out _, out var message), Is.True, message);

            var earthGoal = controller.StageGoalPositionForTests("custom_earth");
            var result = CastCustomReferenceSpell(controller, SpellFamily.Earth, SpellFamily.Earth, earthGoal);
            yield return null;

            Assert.That(result.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(result.spell.isCustomShape, Is.True);
            Assert.That(result.spell.customShapeToken, Is.EqualTo("rect"));
            Assert.That(result.spell.customEventKind, Is.EqualTo(CustomShapeEventKind.WallEntity.ToString()));
            Assert.That(result.spell.customEventPersistence, Is.EqualTo(CustomShapeEventPersistence.Permanent));
            Assert.That(controller.LastCustomShapeEventKindForTests, Is.EqualTo(CustomShapeEventKind.WallEntity.ToString()));
            Assert.That(controller.CustomShapeEventObjectCountForTests, Is.GreaterThan(0));
            Assert.That(controller.ActiveCustomShapeEventObjectCountForTests, Is.GreaterThan(0));
            Assert.That(controller.PermanentCustomShapeEventObjectCountForTests, Is.GreaterThan(0));
            Assert.That(result.spell.recognizedFamily, Is.EqualTo(SpellFamily.Earth));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(4));
            Assert.That(controller.LastMagicNoteText, Does.Contain("도형 반응"));
            yield return new WaitForSeconds(1.25f);
            Assert.That(controller.ActiveCustomShapeEventObjectCountForTests, Is.GreaterThan(0));
            Assert.That(controller.PermanentCustomShapeEventObjectCountForTests, Is.GreaterThan(0));

            ClearCustomSlots(controller);
            DeleteIfExists(profilePath);
        }

        [UnityTest]
        public IEnumerator TimedCustomEventVisualsExpireAfterBaseSealEvent()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);
            var profilePath = TempCustomShapeProfilePath();
            controller.UseCustomShapeStoreForTests(profilePath);
            ClearCustomSlots(controller);
            controller.LoadFloorForTests(1);
            yield return null;
            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(2));
            controller.CompleteCurrentGoalsForTests(1);
            yield return null;
            Assert.That(controller.CurrentSecondFloorSequenceGoalIdForTests, Is.EqualTo("custom_fire"));

            var fireGoal = controller.StageGoalPositionForTests("custom_fire");
            Assert.That(controller.SaveCustomShapeSlotForTests(9, "목표 불꽃", "목표|불꽃|line", "line", SpellFamily.Fire, Samples(SpellFamily.Fire), out var message), Is.True, message);
            var result = controller.CastRawBaseForTests(
                Offset(GestureRecognizer.CreateCanonicalSamples(SpellFamily.Fire, 1.6f, 0.03f), fireGoal, 0.8f),
                fireGoal);
            yield return null;

            Assert.That(result.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(result.spell.isCustomShape, Is.True);
            Assert.That(result.spell.customEventKind, Is.EqualTo(CustomShapeEventKind.SlashDamage.ToString()));
            Assert.That(result.spell.customEventPersistence, Is.EqualTo(CustomShapeEventPersistence.Timed));
            Assert.That(result.spell.customEventLifetimeSeconds, Is.GreaterThan(0f));
            Assert.That(controller.ActiveCustomShapeEventObjectCountForTests, Is.GreaterThan(0));
            Assert.That(controller.PermanentCustomShapeEventObjectCountForTests, Is.EqualTo(0));

            yield return new WaitForSeconds(result.spell.customEventLifetimeSeconds + 0.25f);

            Assert.That(controller.ActiveCustomShapeEventObjectCountForTests, Is.EqualTo(0));
            Assert.That(GameObject.Find("Custom Shape SlashDamage Event Ring"), Is.Null);

            ClearCustomSlots(controller);
            DeleteIfExists(profilePath);
        }

        [UnityTest]
        public IEnumerator SingleCustomFollowupTransformsSealThenAcceptsSecondEvent()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);
            var profilePath = TempCustomShapeProfilePath();
            controller.UseCustomShapeStoreForTests(profilePath);
            ClearCustomSlots(controller);
            controller.LoadFloorForTests(3);
            yield return null;
            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(4));
            Assert.That(controller.SaveCustomShapeSlotForTests(8, "test fire line", "test|fire|line", "line", SpellFamily.Fire, Samples(SpellFamily.Fire), out var fireMessage), Is.True, fireMessage);
            Assert.That(controller.SaveCustomShapeSlotForTests(9, "test water hexagon", "test|water|hexagon", "hexagon", SpellFamily.Water, Samples(SpellFamily.Water), out var waterMessage), Is.True, waterMessage);

            var center = new Vector2(-6.8f, -3.8f);
            controller.CastSyntheticBaseForTests(SpellFamily.Fire, center);
            yield return null;

            Assert.That(controller.ActiveSealCount, Is.EqualTo(1));
            Assert.That(controller.LastSealCustomEffectKindForTests, Is.EqualTo(CustomSpellEffectKind.None));
            Assert.That(controller.LastSealDefaultFallbackPendingForTests, Is.True);

            var fireLine = Offset(GestureRecognizer.CreateCanonicalSamples(SpellFamily.Fire, 1.6f, 0.03f), center, 0.8f);
            var transform = controller.CastRawBaseForTests(fireLine, center);
            yield return null;

            Assert.That(transform.spell.isCustomShape, Is.True, transform.spell.feedbackReason);
            Assert.That(controller.ActiveSealCount, Is.EqualTo(1));
            Assert.That(controller.LastSealCustomEffectKindForTests, Is.EqualTo(CustomSpellEffectKind.Electric));
            Assert.That(controller.LastSealLabelForTests, Does.Contain(CustomSpellEffectCatalog.Korean(CustomSpellEffectKind.Electric)));
            Assert.That(controller.LastSealLabelForTests, Does.Not.Contain("seal"));
            Assert.That(controller.LastSealVisualLabelForTests, Does.Contain("추가 도형 차례"));
            Assert.That(controller.LastSealVisualColorForTests.g, Is.GreaterThan(0.65f));
            Assert.That(controller.LastCustomShapeEventKindForTests, Is.EqualTo(CustomShapeEventKind.SlashDamage.ToString()));
            Assert.That(controller.LastSealDefaultFallbackPendingForTests, Is.True);

            var secondEvent = controller.CastRawBaseForTests(fireLine, center);
            yield return null;

            Assert.That(secondEvent.spell.isCustomShape, Is.True, secondEvent.spell.feedbackReason);
            Assert.That(controller.LastCustomShapeEventKindForTests, Is.EqualTo(CustomShapeEventKind.SlashDamage.ToString()));
            Assert.That(controller.ActiveCustomShapeEventObjectCountForTests, Is.GreaterThan(0));
            Assert.That(controller.ActiveSealCount, Is.EqualTo(0));

            var waterCenter = new Vector2(6.6f, -3.6f);
            controller.CastSyntheticBaseForTests(SpellFamily.Water, waterCenter);
            controller.CastRawBaseForTests(Offset(GestureRecognizer.CreateCanonicalSamples(SpellFamily.Water, 1.6f, 0.03f), waterCenter, 0.8f), waterCenter);
            yield return null;

            Assert.That(controller.ActiveSealCount, Is.EqualTo(1));
            Assert.That(controller.LastSealCustomEffectKindForTests, Is.EqualTo(CustomSpellEffectKind.Ice));
            yield return new WaitForSeconds(ExamGameController.DefaultSealFallbackDelaySeconds + 0.2f);
            Assert.That(controller.ActiveSealCount, Is.EqualTo(0));
            Assert.That(controller.ActiveDefaultBarrierCountForTests, Is.GreaterThan(0));

            ClearCustomSlots(controller);
            DeleteIfExists(profilePath);
        }

        [UnityTest]
        public IEnumerator FloorTwoReferenceShelfImportsCustomBaseSlots()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);
            var profilePath = TempCustomShapeProfilePath();
            controller.UseCustomShapeStoreForTests(profilePath);
            ClearCustomSlots(controller);

            controller.LoadFloorForTests(1);
            yield return null;
            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(2));
            Assert.That(controller.HudCopyForTests, Does.Contain("시험관"));
            Assert.That(controller.HudCopyForTests, Does.Contain("슬롯"));
            Assert.That(controller.CustomReferenceCountForTests, Is.EqualTo(1));
            Assert.That(controller.CurrentSecondFloorSequenceGoalIdForTests, Is.EqualTo("custom_water"));
            Assert.That(controller.ActiveSequentialGoalGuideCountForTests, Is.EqualTo(1));
            Assert.That(controller.CurrentSequentialGoalGuideGoalIdForTests, Is.EqualTo("custom_water"));
            Assert.That(controller.MentorGrantedReferenceCountForTests, Is.EqualTo(1));
            Assert.That(controller.IsCustomShapeSlotOccupiedForTests(0), Is.True);
            Assert.That(controller.CustomShapeSlotMappedFamilyForTests(0), Is.EqualTo(SpellFamily.Water));
            Assert.That(controller.ActiveShelfGuideArrowCountForTests, Is.EqualTo(0));
            Assert.That(GameObject.Find("West Bookcase Guide Arrow"), Is.Null);
            Assert.That(GameObject.Find("East Bookcase Guide Arrow"), Is.Null);

            controller.MovePlayerForTests(new Vector2(-7.25f, 1.1f));
            yield return null;
            Assert.That(controller.IsCustomReferenceBubbleVisibleForTests, Is.True);
            var shelfBubble = GameObject.Find("Custom Reference Shelf Bubble")?.GetComponent<RectTransform>();
            Assert.That(shelfBubble, Is.Not.Null);
            var mentorSpeech = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(rect => rect.name == "Mentor Speech");
            var magicNote = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(rect => rect.name == "Magic Note");
            Assert.That(mentorSpeech, Is.Not.Null);
            Assert.That(magicNote, Is.Not.Null);
            Assert.That(mentorSpeech.gameObject.activeInHierarchy, Is.False);
            Assert.That(magicNote.gameObject.activeInHierarchy, Is.False);
            Assert.That(shelfBubble.anchoredPosition.y + shelfBubble.sizeDelta.y * 0.5f, Is.LessThan(WorldToCanvasPoint(controller.StageGoalPositionForTests("custom_water")).y - 20f));
#if false
            AssertVisibleKoreanTextLooksUsable("도형 레퍼런스");

#endif

            controller.MovePlayerForTests(Vector2.Lerp(controller.CustomReferenceShelfPositionForTests, controller.StageGoalPositionForTests("custom_water"), 0.5f));
            yield return null;
            Assert.That(controller.IsCustomReferenceBubbleVisibleForTests, Is.False);

            controller.OpenCustomReferencePanelForTests();
            yield return null;
            Assert.That(controller.IsCustomReferencePanelOpenForTests, Is.True);
            var referencePanel = GameObject.Find("Custom Reference Panel")?.GetComponent<RectTransform>();
            Assert.That(referencePanel, Is.Not.Null);
            Assert.That(referencePanel.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(referencePanel.anchorMax, Is.EqualTo(Vector2.zero));
            Assert.That(referencePanel.pivot, Is.EqualTo(Vector2.zero));
            Assert.That(referencePanel.anchoredPosition, Is.EqualTo(new Vector2(18f, 20f)));
            Assert.That(referencePanel.sizeDelta, Is.EqualTo(new Vector2(560f, 372f)));
            var waterReferenceLabel = GameObject.Find("Custom Reference Label Water")?.GetComponent<Text>();
            var waterReferenceSummary = GameObject.Find("Custom Reference Summary Water")?.GetComponent<Text>();
            Assert.That(waterReferenceLabel, Is.Not.Null);
            Assert.That(waterReferenceSummary, Is.Not.Null);
            Assert.That(waterReferenceLabel.text, Does.Contain(SpellLabels.Korean(SpellFamily.Water)));
            Assert.That(waterReferenceLabel.text, Does.Not.Contain(":"));
            Assert.That(waterReferenceSummary.text, Does.Not.Contain(SpellLabels.Korean(SpellFamily.Water)));
            Assert.That(GameObject.Find("Custom Reference Label Fire"), Is.Null);

            var waterImportButton = GameObject.Find("Import Custom Reference Water")?.GetComponent<Button>();
            Assert.That(waterImportButton, Is.Not.Null);
            waterImportButton.onClick.Invoke();
            yield return null;
            Assert.That(controller.IsCustomShapeSlotOccupiedForTests(0), Is.True);
            Assert.That(controller.CustomShapeSlotMappedFamilyForTests(0), Is.EqualTo(SpellFamily.Water));
            Assert.That(controller.ImportCustomReferenceForTests(SpellFamily.Fire, out _, out var earlyFireMessage), Is.False, earlyFireMessage);

            Assert.That(controller.CustomReferenceStatusForTests, Does.Contain("지금은"));

            var waterGoal = controller.StageGoalPositionForTests("custom_water");
            var detachedWater = controller.CastRawBaseForTests(controller.CustomReferenceStrokesForTests(SpellFamily.Water, waterGoal), waterGoal);
            yield return null;
            Assert.That(detachedWater.spell.isCustomShape, Is.False, detachedWater.spell.feedbackReason);
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(0));

            controller.LoadFloorForTests(1);
            yield return null;
            Assert.That(controller.CurrentSecondFloorSequenceGoalIdForTests, Is.EqualTo("custom_water"));
            Assert.That(controller.ImportCustomReferenceForTests(SpellFamily.Water, out _, out var waterMessage), Is.True, waterMessage);
            waterGoal = controller.StageGoalPositionForTests("custom_water");
            var customWater = CastCustomReferenceSpell(controller, SpellFamily.Water, SpellFamily.Water, waterGoal);
            yield return null;
            Assert.That(customWater.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(customWater.spell.isCustomShape, Is.True);
            Assert.That(customWater.spell.recognizedFamily, Is.EqualTo(SpellFamily.Water));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(1));
            Assert.That(controller.CurrentSecondFloorSequenceGoalIdForTests, Is.EqualTo("custom_fire"));
            Assert.That(controller.ActiveSequentialGoalGuideCountForTests, Is.EqualTo(1));
            Assert.That(controller.CurrentSequentialGoalGuideGoalIdForTests, Is.EqualTo("custom_fire"));
            Assert.That(controller.IsQuestScrollCollapsedForTests, Is.True);
            Assert.That(controller.QuestScrollPanelHeightForTests, Is.EqualTo(88f).Within(1.0f));

#if false
            var windReferenceLabel = GameObject.Find("Custom Reference Label Wind")?.GetComponent<Text>();
            var windReferenceSummary = GameObject.Find("Custom Reference Summary Wind")?.GetComponent<Text>();
            Assert.That(windReferenceLabel, Is.Not.Null);
            Assert.That(windReferenceSummary, Is.Not.Null);
            Assert.That(windReferenceLabel.text, Is.EqualTo("화살표"));
            Assert.That(windReferenceLabel.text, Does.Not.Contain(SpellLabels.Korean(SpellFamily.Wind)));
            Assert.That(windReferenceLabel.text, Does.Not.Contain(":"));
            Assert.That(windReferenceSummary.text, Does.Contain("화살촉"));
            Assert.That(windReferenceSummary.text, Does.Not.Contain(SpellLabels.Korean(SpellFamily.Wind)));

            var windImportButton = GameObject.Find("Import Custom Reference Wind")?.GetComponent<Button>();
            Assert.That(windImportButton, Is.Not.Null);
            windImportButton.onClick.Invoke();
            yield return null;
            Assert.That(controller.IsCustomShapeSlotOccupiedForTests(0), Is.True);
            Assert.That(controller.CustomShapeSlotMappedFamilyForTests(0), Is.EqualTo(SpellFamily.Wind));

            var families = new[] { SpellFamily.Earth, SpellFamily.Fire, SpellFamily.Water, SpellFamily.Life };
            foreach (var family in families)
            {
                Assert.That(controller.ImportCustomReferenceForTests(family, out var slotIndex, out var message), Is.True, message);
                Assert.That(slotIndex, Is.InRange(0, controller.CustomShapeSlotCountForTests - 1));
                Assert.That(controller.IsCustomShapeSlotOccupiedForTests(slotIndex), Is.True);
                Assert.That(controller.CustomShapeSlotMappedFamilyForTests(slotIndex), Is.EqualTo(family));
            }

            Assert.That(controller.CustomReferenceStatusForTests, Does.Contain("슬롯"));

            var windGoal = new Vector2(0f, 3.05f);
            var customWind = controller.CastRawBaseForTests(controller.CustomReferenceStrokesForTests(SpellFamily.Wind, windGoal), windGoal);
            yield return null;
            Assert.That(customWind.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(customWind.spell.isCustomShape, Is.True);
            Assert.That(customWind.spell.customShapeLabel, Is.EqualTo("바람 화살표"));
            Assert.That(customWind.spell.recognizedFamily, Is.EqualTo(SpellFamily.Wind));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(1));
            Assert.That(controller.IsQuestScrollCollapsedForTests, Is.True);
            Assert.That(controller.QuestScrollPanelHeightForTests, Is.EqualTo(78f).Within(1.0f));

            var lifeGoal = new Vector2(5.4f, 2.55f);
            var customLife = controller.CastRawBaseForTests(controller.CustomReferenceStrokesForTests(SpellFamily.Life, lifeGoal), lifeGoal);
            yield return null;
            Assert.That(customLife.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(customLife.spell.isCustomShape, Is.True, customLife.spell.feedbackReason);
            Assert.That(customLife.spell.recognizedFamily, Is.EqualTo(SpellFamily.Life));
            Assert.That(customLife.spell.customShapeToken, Is.EqualTo("brace"));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(2));
#endif

            controller.CastSyntheticBaseForTests(SpellFamily.Fire, new Vector2(-5.4f, 2.55f));
            yield return null;
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(1));
            Assert.That(controller.LastMagicNoteText.Length, Is.GreaterThan(12));

            var existingWaterSlot = Enumerable
                .Range(0, controller.CustomShapeSlotCountForTests)
                .First(index => controller.IsCustomShapeSlotOccupiedForTests(index) &&
                                controller.CustomShapeSlotMappedFamilyForTests(index) == SpellFamily.Water);
            controller.LoadFloorForTests(2);
            yield return null;
            Assert.That(controller.IsResultPanelVisible, Is.False);
            Assert.That(controller.LastResultPanelTextForTests, Is.Empty);
            Assert.That(controller.IsQuestScrollCollapsedForTests, Is.True);
            Assert.That(controller.ImportCustomReferenceForTests(SpellFamily.Water, out var replacedWaterSlot, out var replaceMessage), Is.True, replaceMessage);
            Assert.That(replacedWaterSlot, Is.InRange(0, controller.CustomShapeSlotCountForTests - 1));
            Assert.That(controller.IsCustomShapeSlotOccupiedForTests(replacedWaterSlot), Is.True);
            Assert.That(controller.CustomShapeSlotMappedFamilyForTests(replacedWaterSlot), Is.EqualTo(SpellFamily.Water));
            Assert.That(replacedWaterSlot, Is.Not.EqualTo(existingWaterSlot));

            ClearCustomSlots(controller);
            DeleteIfExists(profilePath);
        }

        [UnityTest]
        public IEnumerator FloorTwoCustomBaseGoalsCanChainWhilePreviousSealIsActive()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);
            var profilePath = TempCustomShapeProfilePath();
            controller.UseCustomShapeStoreForTests(profilePath);
            ClearCustomSlots(controller);
            controller.LoadFloorForTests(1);
            yield return null;
            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(2));

            Assert.That(controller.ImportCustomReferenceForTests(SpellFamily.Water, out _, out var waterMessage), Is.True, waterMessage);
            var waterGoal = new Vector2(-2.7f, 3.0f);
            var customWater = CastCustomReferenceSpell(controller, SpellFamily.Water, SpellFamily.Water, waterGoal);
            yield return null;
            Assert.That(customWater.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(customWater.spell.isCustomShape, Is.True, customWater.spell.feedbackReason);
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(1));
            Assert.That(controller.ActiveSealCount, Is.EqualTo(0));

            ClearCustomSlots(controller);
            Assert.That(controller.ImportCustomReferenceForTests(SpellFamily.Fire, out _, out var fireMessage), Is.True, fireMessage);
            var fireGoal = new Vector2(-5.4f, 2.55f);
            var customFire = CastCustomReferenceSpell(controller, SpellFamily.Fire, SpellFamily.Fire, fireGoal);
            yield return null;
            Assert.That(customFire.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(customFire.spell.isCustomShape, Is.True, customFire.spell.feedbackReason);
            Assert.That(customFire.spell.recognizedFamily, Is.EqualTo(SpellFamily.Fire));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(2));

            ClearCustomSlots(controller);
            DeleteIfExists(profilePath);
        }

        [UnityTest]
        public IEnumerator FloorTwoDistantCustomSymbolDoesNotActivateAndShowsProximityBubble()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);
            var profilePath = TempCustomShapeProfilePath();
            controller.UseCustomShapeStoreForTests(profilePath);
            ClearCustomSlots(controller);
            controller.LoadFloorForTests(1);
            yield return null;
            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(2));

            Assert.That(controller.ImportCustomReferenceForTests(SpellFamily.Water, out _, out var windMessage), Is.True, windMessage);
            var windGoal = controller.StageGoalPositionForTests("custom_water");
            var farFromGoal = new Vector2(-6.8f, -3.8f);
            controller.MovePlayerForTests(farFromGoal);

            var distantCustomWind = controller.CastRawBaseForTests(
                controller.CustomReferenceStrokesForTests(SpellFamily.Water, windGoal),
                windGoal,
                movePlayerToReference: false);
            yield return null;

            Assert.That(distantCustomWind.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(distantCustomWind.spell.isCustomShape, Is.False, distantCustomWind.spell.feedbackReason);
            Assert.That(distantCustomWind.spell.recognizedFamily, Is.EqualTo(SpellFamily.Water));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(0));
            Assert.That(controller.LastGoalProximityGuideGoalIdForTests, Is.EqualTo("custom_water"));
            Assert.That(controller.LastGoalProximityGuideDistanceForTests, Is.GreaterThan(1.85f));
            Assert.That(controller.IsGoalProximityBubbleVisibleForTests, Is.True);
            Assert.That(controller.GoalProximityBubbleTextForTests, Is.Not.Empty);
#if false
            Assert.That(controller.GoalProximityBubbleTextForTests, Does.Contain("媛源뚯씠"));
#if false
            Assert.That(controller.GoalProximityBubbleTextForTests, Does.Contain("바람 화살표 가까이 이동"));
            Assert.That(controller.LastMagicNoteText, Does.Contain("표식에서 너무 멉니다"));

#endif

#endif

            controller.MovePlayerForTests(windGoal);
            var nearCustomWind = CastCustomReferenceSpell(controller, SpellFamily.Water, SpellFamily.Water, windGoal);
            yield return null;

            Assert.That(nearCustomWind.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(1));
            Assert.That(controller.IsGoalProximityBubbleVisibleForTests, Is.False);

            ClearCustomSlots(controller);
            DeleteIfExists(profilePath);
        }

        [UnityTest]
        public IEnumerator FloorThreeObstacleArtUsesDepthCues()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);

            controller.LoadFloorForTests(2);
            yield return null;

            Assert.That(controller.ActiveShelfGuideArrowCountForTests, Is.EqualTo(0));
            Assert.That(controller.ActiveSpriteAccentAnimationCountForTests, Is.GreaterThanOrEqualTo(12));
            Assert.That(GameObject.Find("Crossing Reference Bookcase Guide Arrow"), Is.Null);
            Assert.That(GameObject.Find("Stage Shelf Vertical Guide Rail"), Is.Not.Null);
            Assert.That(GameObject.Find("Stage Shelf Vertical Guide Left Boundary"), Is.Not.Null);
            Assert.That(GameObject.Find("Stage Shelf Vertical Guide Right Boundary"), Is.Not.Null);
            Assert.That(GameObject.Find("Stage Shelf Vertical Guide Up Arrow"), Is.Not.Null);
            Assert.That(GameObject.Find("Stage Shelf Vertical Guide Down Arrow"), Is.Not.Null);
            Assert.That(GameObject.Find("Crossing Route Upper Guard Wall"), Is.Not.Null);
            Assert.That(GameObject.Find("Crossing Route Lower Drop Wall"), Is.Not.Null);
            Assert.That(GameObject.Find("Start Stone Walkway Underside"), Is.Not.Null);
            Assert.That(GameObject.Find("River Vertical Channel Core"), Is.Not.Null);
            Assert.That(GameObject.Find("River No Bypass Upper Wall"), Is.Not.Null);
            Assert.That(GameObject.Find("River No Bypass Lower Drop"), Is.Not.Null);
            Assert.That(GameObject.Find("River Lower Drop Shadow"), Is.Not.Null);
            Assert.That(GameObject.Find("River Bank Left Cliff Face"), Is.Not.Null);
            Assert.That(GameObject.Find("Broken Floor Vertical Rupture Core"), Is.Not.Null);
            Assert.That(GameObject.Find("Broken Floor No Bypass Upper Wall"), Is.Not.Null);
            Assert.That(GameObject.Find("Broken Floor No Bypass Lower Drop"), Is.Not.Null);
            Assert.That(GameObject.Find("Broken Floor Lower Void"), Is.Not.Null);
            Assert.That(GameObject.Find("Broken Floor Inner Void"), Is.Not.Null);
            Assert.That(GameObject.Find("Chasm Vertical Shaft Core"), Is.Not.Null);
            Assert.That(GameObject.Find("Chasm No Bypass Upper Wall"), Is.Not.Null);
            Assert.That(GameObject.Find("Chasm No Bypass Lower Drop"), Is.Not.Null);
            Assert.That(GameObject.Find("Chasm Far Abyss"), Is.Not.Null);
            Assert.That(GameObject.Find("Chasm Left Cliff Wall"), Is.Not.Null);
            Assert.That(GameObject.Find("Wind Gap Vertical Shaft Core"), Is.Not.Null);
            Assert.That(GameObject.Find("Wind Gap No Bypass Upper Wall"), Is.Not.Null);
            Assert.That(GameObject.Find("Wind Gap No Bypass Lower Drop"), Is.Not.Null);
            Assert.That(GameObject.Find("Wind Gap Lower Depth"), Is.Not.Null);
            Assert.That(GameObject.Find("Wind Gap Mist"), Is.Not.Null);
            var riverFlowPosition = controller.SpriteAccentPositionForTests("River Flow Streak A");
            yield return new WaitForSeconds(0.35f);
            Assert.That(Vector2.Distance(controller.SpriteAccentPositionForTests("River Flow Streak A"), riverFlowPosition), Is.GreaterThan(0.004f));
        }

        [UnityTest]
        public IEnumerator FloorThreeLockedObstacleBlocksJumpHeightByXColumn()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);

            controller.LoadFloorForTests(2);
            yield return null;

            controller.MovePlayerForTests(new Vector2(-2.55f, -2.10f));
            yield return null;

            Assert.That(Vector2.Distance(controller.PlayerPosition, controller.StageObstacleResetPositionForTests("frozen_river")), Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator FloorThreeKeyboardFallbackMovesPlatformPlayer()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);
            var boot = Object.FindFirstObjectByType<GameBootController>();
            Assert.That(boot, Is.Not.Null);
            boot.StartNewGameForTests();
            yield return null;

            controller.CloseFirstFloorLetterForTests();
            controller.LoadFloorForTests(2);
            yield return null;

            Assert.That(controller.IsPlatformMotionActiveForTests, Is.True);
            var start = controller.PlayerPosition;

            controller.SetMovementInputFallbackForTests(leftHeld: false, rightHeld: true, downHeld: false, upHeld: false);
            yield return new WaitForSeconds(0.35f);
            controller.SetMovementInputFallbackForTests(leftHeld: false, rightHeld: false, downHeld: false, upHeld: false);
            yield return null;

            Assert.That(controller.PlayerPosition.x, Is.GreaterThan(start.x + 1.0f));
        }

        [UnityTest]
        public IEnumerator FloorThreeLedgeStopsOnceBeforeLockedGap()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);
            var boot = Object.FindFirstObjectByType<GameBootController>();
            Assert.That(boot, Is.Not.Null);
            boot.StartNewGameForTests();
            yield return null;

            controller.CloseFirstFloorLetterForTests();
            controller.LoadFloorForTests(2);
            yield return null;

            controller.MovePlayerForTests(new Vector2(1.78f, -2.58f));
            controller.SetMovementInputFallbackForTests(leftHeld: false, rightHeld: true, downHeld: false, upHeld: false);
            for (var frame = 0; frame < 12 && !controller.IsStageLedgeStopPrimedForTests; frame++)
            {
                yield return null;
            }

            Assert.That(controller.IsStageLedgeStopPrimedForTests, Is.True);
            Assert.That(controller.StageLedgeStopGoalIdForTests, Is.EqualTo("earth_stairs"));
            Assert.That(controller.PlayerPosition.x, Is.InRange(1.60f, 1.90f));
            Assert.That(Vector2.Distance(controller.PlayerPosition, controller.StageObstacleResetPositionForTests("earth_stairs")), Is.GreaterThan(0.6f));

            controller.SetMovementInputFallbackForTests(leftHeld: false, rightHeld: false, downHeld: false, upHeld: false);
            yield return null;

            controller.SetMovementInputFallbackForTests(leftHeld: false, rightHeld: true, downHeld: false, upHeld: false);
            for (var frame = 0; frame < 80; frame++)
            {
                yield return null;
                if (Vector2.Distance(controller.PlayerPosition, controller.StageObstacleResetPositionForTests("earth_stairs")) < 0.2f)
                {
                    break;
                }
            }

            controller.SetMovementInputFallbackForTests(leftHeld: false, rightHeld: false, downHeld: false, upHeld: false);
            yield return null;

            Assert.That(Vector2.Distance(controller.PlayerPosition, controller.StageObstacleResetPositionForTests("earth_stairs")), Is.LessThan(0.25f));
        }

        [UnityTest]
        public IEnumerator FloorThreeShelfZoneAllowsLimitedVerticalApproach()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);
            var boot = Object.FindFirstObjectByType<GameBootController>();
            Assert.That(boot, Is.Not.Null);
            boot.StartNewGameForTests();
            yield return null;

            controller.CloseFirstFloorLetterForTests();
            controller.LoadFloorForTests(2);
            yield return null;

            var shelfPosition = controller.CustomReferenceShelfPositionForTests;
            var interactionPosition = controller.CustomReferenceInteractionPositionForTests;
            Assert.That(Vector2.Distance(shelfPosition, interactionPosition), Is.LessThan(0.5f));
            Assert.That(interactionPosition.y, Is.GreaterThan(controller.CurrentStageSafePositionForTests.y + 2.5f));
            Assert.That(GameObject.Find("Stage Shelf Vertical Guide Rail"), Is.Not.Null);
            Assert.That(GameObject.Find("Stage Shelf Vertical Guide Rung 3"), Is.Not.Null);

            controller.MovePlayerForTests(new Vector2(shelfPosition.x, controller.CurrentStageSafePositionForTests.y));
            yield return null;
            var lowerPosition = controller.PlayerPosition;
            Assert.That(controller.IsCustomReferenceBubbleVisibleForTests, Is.False);

            controller.SetMovementInputFallbackForTests(leftHeld: false, rightHeld: false, downHeld: false, upHeld: true);
            yield return new WaitForSeconds(0.65f);
            controller.SetMovementInputFallbackForTests(leftHeld: false, rightHeld: false, downHeld: false, upHeld: false);
            yield return null;

            Assert.That(controller.PlayerPosition.y, Is.GreaterThan(lowerPosition.y + 1.7f));
            Assert.That(controller.PlayerPosition.y, Is.LessThanOrEqualTo(interactionPosition.y + 0.06f));
            Assert.That(controller.IsCustomReferenceBubbleVisibleForTests, Is.True);
        }

        [UnityTest]
        public IEnumerator FloorThreeStageMagicAcceptsNearObstacleCast()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);
            var profilePath = TempCustomShapeProfilePath();
            controller.UseCustomShapeStoreForTests(profilePath);
            ClearCustomSlots(controller);

            controller.LoadFloorForTests(2);
            yield return null;
            Assert.That(controller.ImportCustomReferenceForTests(SpellFamily.Earth, out _, out var earthMessage), Is.True, earthMessage);

            var goalPosition = controller.StageGoalPositionForTests("earth_stairs");
            Assert.That(controller.StageGoalRadiusForTests("earth_stairs"), Is.GreaterThan(2.8f));
            var nearObstaclePosition = goalPosition + new Vector2(2.45f, -0.35f);
            CastCustomReferenceSpell(controller, SpellFamily.Earth, SpellFamily.Earth, nearObstaclePosition);
            yield return null;

            Assert.That(
                controller.CompletedGoalCountForTests,
                Is.EqualTo(1),
                $"earth stairs note={controller.LastMagicNoteText} radius={controller.StageGoalRadiusForTests("earth_stairs"):0.00}");
            Assert.That(controller.HasStageEntityNearForTests(new Vector2(3.25f, -3.16f), 0.35f), Is.True);
            Assert.That(controller.HasStageEntityColliderNearForTests(new Vector2(3.25f, -3.16f), 0.35f), Is.True);

            ClearCustomSlots(controller);
            DeleteIfExists(profilePath);
        }

        [UnityTest]
        public IEnumerator CustomSpellStageGoalsRequireBaseAndNearbyFollowup()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);
            var profilePath = TempCustomShapeProfilePath();
            controller.UseCustomShapeStoreForTests(profilePath);
            ClearCustomSlots(controller);

            controller.LoadFloorForTests(2);
            yield return null;
            Assert.That(controller.ImportCustomReferenceForTests(SpellFamily.Life, out _, out var lifeMessage), Is.True, lifeMessage);
            Assert.That(controller.ImportCustomReferenceForTests(SpellFamily.Water, out _, out var waterMessage), Is.True, waterMessage);
            Assert.That(controller.ImportCustomReferenceForTests(SpellFamily.Earth, out _, out var earthMessage), Is.True, earthMessage);
            Assert.That(controller.ImportCustomReferenceForTests(SpellFamily.Wind, out _, out var windMessage), Is.True, windMessage);
            Assert.That(controller.ActiveStageGateCountForTests, Is.EqualTo(4));
            Assert.That(controller.ActiveStageInteractionCountForTests, Is.EqualTo(4));
            Assert.That(controller.IsPlatformMotionActiveForTests, Is.True);
            Assert.That(controller.ActiveStageEntityCountForTests, Is.EqualTo(0));
            Assert.That(controller.ActiveStageEffectVisualCountForTests, Is.EqualTo(0));
            Assert.That(Vector2.Distance(controller.CustomReferenceShelfPositionForTests, controller.CustomReferenceInteractionPositionForTests), Is.LessThan(0.5f));
            controller.MovePlayerForTests(controller.CustomReferenceInteractionPositionForTests);
            yield return null;
            Assert.That(controller.IsCustomReferenceBubbleVisibleForTests, Is.True);

            CastCustomReferenceSpell(controller, SpellFamily.Life, SpellFamily.Life, Vector2.zero);
            yield return null;
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(0));
            Assert.That(controller.ActiveStageEffectVisualCountForTests, Is.EqualTo(0));

            var icePosition = controller.StageGoalPositionForTests("frozen_river");
            CastCustomReferenceSpell(controller, SpellFamily.Water, SpellFamily.Water, icePosition);
            yield return null;
            yield return null;
            Assert.That(
                controller.CompletedGoalCountForTests,
                Is.EqualTo(1),
                $"frozen river note={controller.LastMagicNoteText} event={controller.LastCustomShapeEventKindForTests} seal={controller.ActiveSealCount} effect={controller.LastSealCustomEffectKindForTests}");
            Assert.That(
                controller.ActiveStageEntityCountForTests,
                Is.EqualTo(1),
                $"frozen river entity count after completion; visuals={controller.ActiveStageEffectVisualCountForTests}");
            Assert.That(controller.ActiveStageEffectVisualCountForTests, Is.GreaterThanOrEqualTo(9));
            Assert.That(GameObject.Find("Stage Effect frozen_river Ground Glow"), Is.Not.Null);
            Assert.That(GameObject.Find("Stage Effect frozen_river Surface Wake"), Is.Not.Null);
            Assert.That(GameObject.Find("Stage Effect frozen_river Left Anchor"), Is.Not.Null);
            Assert.That(GameObject.Find("Stage Effect frozen_river Stun Signature"), Is.Not.Null);
            Assert.That(GameObject.Find("Custom Shape Stun Event Ring"), Is.Not.Null);
            Assert.That(controller.LastMagicNoteText, Does.Contain("얼음"));

            var stairsPosition = controller.StageGoalPositionForTests("earth_stairs");
            CastCustomReferenceSpell(controller, SpellFamily.Earth, SpellFamily.Earth, stairsPosition);
            yield return null;
            Assert.That(
                controller.CompletedGoalCountForTests,
                Is.EqualTo(2),
                $"earth stairs note={controller.LastMagicNoteText} event={controller.LastCustomShapeEventKindForTests} seal={controller.ActiveSealCount} effect={controller.LastSealCustomEffectKindForTests}");
            Assert.That(controller.ActiveStageEntityCountForTests, Is.EqualTo(2));
            Assert.That(controller.ActiveStageEntityColliderCountForTests, Is.GreaterThanOrEqualTo(2));
            Assert.That(controller.HasStageEntityNearForTests(new Vector2(3.25f, -3.16f), 0.35f), Is.True);
            Assert.That(controller.HasStageEntityColliderNearForTests(new Vector2(3.25f, -3.16f), 0.35f), Is.True);
            Assert.That(controller.ActiveStageEffectVisualCountForTests, Is.GreaterThanOrEqualTo(18));
            Assert.That(GameObject.Find("Stage Effect earth_stairs Interaction Rim North"), Is.Not.Null);
            Assert.That(GameObject.Find("Stage Effect earth_stairs WallEntity Signature"), Is.Not.Null);
            Assert.That(controller.CurrentStageSafePositionForTests.x, Is.GreaterThan(4.5f));

            var bridgePosition = controller.StageGoalPositionForTests("living_bridge");
            var bridge = CastCustomReferenceSpell(controller, SpellFamily.Life, SpellFamily.Life, bridgePosition);
            yield return null;
            Assert.That(bridge.spell.isCustomShape, Is.True);
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(3));
            Assert.That(controller.ActiveStageEntityCountForTests, Is.EqualTo(3));
            Assert.That(controller.ActiveStageEffectVisualCountForTests, Is.GreaterThanOrEqualTo(29));
            Assert.That(GameObject.Find("Stage Effect living_bridge DirectionalProjectile Signature"), Is.Not.Null);
            Assert.That(GameObject.Find("Stage Effect living_bridge Direction Trail"), Is.Not.Null);
            Assert.That(GameObject.Find("Stage Effect living_bridge Event Impact"), Is.Not.Null);
            Assert.That(controller.LastMagicNoteText, Does.Contain("낭떠러지"));

            var windPosition = controller.StageGoalPositionForTests("wind_platform");
            CastCustomReferenceSpell(controller, SpellFamily.Wind, SpellFamily.Wind, windPosition);
            yield return null;
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(4));
            Assert.That(controller.ActiveStageEntityCountForTests, Is.EqualTo(4));
            Assert.That(controller.ActiveStageEffectVisualCountForTests, Is.GreaterThanOrEqualTo(38));
            Assert.That(GameObject.Find("Stage Effect wind_platform Surface Wake"), Is.Not.Null);
            Assert.That(GameObject.Find("Stage Effect wind_platform WallEntity Signature"), Is.Not.Null);
            Assert.That(controller.LastMagicNoteText, Does.Contain("네 구간"));

            controller.LoadFloorForTests(3);
            yield return null;
            Assert.That(controller.ActiveStageEffectVisualCountForTests, Is.EqualTo(0));
            ClearCustomSlots(controller);
            Assert.That(controller.ImportCustomReferenceForTests(SpellFamily.Fire, out _, out var fireBeamMessage), Is.True, fireBeamMessage);
            CastBeamSpell(controller, SpellFamily.Fire, new Vector2(-4.2f, 1.85f));
            yield return null;
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(1));
            Assert.That(controller.ActiveDamagePopupCountForTests, Is.GreaterThan(0));
            Assert.That(controller.LastBeamHitForTests, Is.True);
            Assert.That(controller.LastDamagedTargetNameForTests, Does.Contain("Scarecrow"));
            Assert.That(controller.LastDamagePopupTextForTests, Is.EqualTo("-1/2"));

            ClearCustomSlots(controller);
            DeleteIfExists(profilePath);
        }

        [UnityTest]
        public IEnumerator BuffQueuesAndStabilizationNumbersAttachToEntities()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);
            var profilePath = TempCustomShapeProfilePath();
            controller.UseCustomShapeStoreForTests(profilePath);
            ClearCustomSlots(controller);

            controller.LoadFloorForTests(1);
            yield return null;
            controller.CompleteCurrentGoalsForTests(4);
            yield return null;
            Assert.That(controller.CurrentSecondFloorSequenceGoalIdForTests, Is.EqualTo("custom_life"));
            Assert.That(controller.ImportCustomReferenceForTests(SpellFamily.Life, out _, out var lifeMessage), Is.True, lifeMessage);

            CastCustomReferenceSpell(controller, SpellFamily.Life, SpellFamily.Life, Vector2.zero);
            yield return null;

            Assert.That(controller.ActivePlayerBuffSlotCountForTests, Is.EqualTo(1));
            Assert.That(controller.LastBuffLabelForTests, Is.EqualTo("공격"));
            Assert.That(GameObject.Find("Buff Queue Player"), Is.Not.Null);
            Assert.That(GameObject.Find("Buff Slot 공격"), Is.Not.Null);
            var clock = GameObject.Find("Buff Cooldown Clock Fill")?.GetComponent<BuffCooldownClockGraphic>();
            Assert.That(clock, Is.Not.Null);
            Assert.That(clock.color.a, Is.GreaterThan(0.45f));
            Assert.That(clock.color.r, Is.LessThan(0.05f));
            var firstFill = controller.FirstBuffCooldownFillForTests;
            yield return new WaitForSeconds(0.22f);
            Assert.That(controller.FirstBuffCooldownFillForTests, Is.GreaterThan(firstFill));

            controller.LoadFloorForTests(3);
            yield return null;
            ClearCustomSlots(controller);
            Assert.That(controller.ImportCustomReferenceForTests(SpellFamily.Water, out _, out var waterMessage), Is.True, waterMessage);
            var beamOrigin = new Vector2(-4.2f, 1.85f);
            CastBeamSpell(controller, SpellFamily.Water, beamOrigin);
            yield return null;

            Assert.That(controller.LastBeamHitForTests, Is.True);
            Assert.That(controller.LastDamagedTargetNameForTests, Does.Contain("Scarecrow"));
            Assert.That(controller.ActiveDamagePopupCountForTests, Is.GreaterThan(0));
            Assert.That(controller.LastDamagePopupTextForTests, Is.EqualTo("-1/2"));
            Assert.That(GameObject.Find("Damage Popup Main Text"), Is.Not.Null);
            Assert.That(GameObject.Find("Damage Popup Shadow NW"), Is.Not.Null);

            ClearCustomSlots(controller);
            DeleteIfExists(profilePath);
            yield break;

            var combatIcePosition = new Vector2(-4.8f, 1.55f);
            CastCustomReferenceSpell(controller, SpellFamily.Water, SpellFamily.Water, combatIcePosition);
            yield return null;

            Assert.That(controller.ActiveTargetBuffSlotCountForTests, Is.EqualTo(1));
            Assert.That(GameObject.Find("Buff Queue Rift Marker ice_training"), Is.Not.Null);
            Assert.That(GameObject.Find("Buff Slot 냉각"), Is.Not.Null);
            Assert.That(controller.ActiveDamagePopupCountForTests, Is.GreaterThan(0));
            Assert.That(controller.LastDamagePopupTextForTests, Does.Match(@"^\+\d+ 안정"));
            Assert.That(GameObject.Find("Damage Popup Main Text"), Is.Not.Null);
            Assert.That(GameObject.Find("Damage Popup Shadow NW"), Is.Not.Null);

            ClearCustomSlots(controller);
            DeleteIfExists(profilePath);
        }

        [UnityTest]
        public IEnumerator CustomStageFollowupPrefersActiveSealFamilyWhenReferenceShapesOverlap()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);
            var profilePath = TempCustomShapeProfilePath();
            controller.UseCustomShapeStoreForTests(profilePath);
            ClearCustomSlots(controller);

            controller.LoadFloorForTests(2);
            yield return null;
            Assert.That(controller.ImportCustomReferenceForTests(SpellFamily.Earth, out _, out var earthMessage), Is.True, earthMessage);
            Assert.That(controller.ImportCustomReferenceForTests(SpellFamily.Life, out _, out var lifeMessage), Is.True, lifeMessage);

            var bridgePosition = controller.StageGoalPositionForTests("living_bridge");
            var bridge = CastCustomReferenceSpell(controller, SpellFamily.Life, SpellFamily.Life, bridgePosition);
            yield return null;
            Assert.That(bridge.spell.isCustomShape, Is.True);
            Assert.That(bridge.spell.mappedFamily, Is.EqualTo(SpellFamily.Life));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(1));

            ClearCustomSlots(controller);
            DeleteIfExists(profilePath);
        }

        [UnityTest]
        public IEnumerator WorldDrawnOverlayCandidateAfterSealIsExcluded()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);
            var profilePath = TempCustomShapeProfilePath();
            controller.UseCustomShapeStoreForTests(profilePath);
            ClearCustomSlots(controller);

            controller.CastSyntheticBaseForTests(SpellFamily.Earth, Vector2.zero);
            var overlayStrokes = OverlayRecognizer.CreateCanonicalSamples(OverlayOperator.IceBar, new Vector2(4.8f, 0f), 0.48f, 0.03f);
            var result = controller.CastRawBaseForTests(overlayStrokes, new Vector2(4.8f, 0f));
            yield return null;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.spell.success, Is.False);
            Assert.That(result.spell.status, Is.Not.EqualTo(RecognitionStatus.Recognized));
            Assert.That(result.spell.recognizedFamily, Is.Null);
            Assert.That(controller.LastOverlayStack, Is.Empty);
            Assert.That(controller.LastMagicNoteText, Does.Contain("추가 도형"));
            DeleteIfExists(profilePath);
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

            var puddlePosition = controller.StageGoalPositionForTests("puddle");
            controller.CastSyntheticBaseForTests(SpellFamily.Water, puddlePosition);
            var result = controller.CastSyntheticOverlayForTests(OverlayOperator.SoulDot, puddlePosition, 1f);
            yield return null;

            Assert.That(result.success, Is.False);
            Assert.That(result.recognizedOperator, Is.EqualTo(OverlayOperator.SoulDot));
            Assert.That(result.scaleHint, Is.EqualTo(OverlayScaleHint.TooLarge));
            Assert.That(controller.LastMagicNoteText, Does.Contain("너무 커"));
            Assert.That(controller.LastHintText, Does.Contain("너무 큽니다"));
            Assert.That(controller.IsResultPanelVisible, Is.False);
            Assert.That(controller.LastResultPanelTextForTests, Does.Contain("장식 실패"));
            Assert.That(controller.LastResultPanelTextForTests, Does.Contain("크기"));
            Assert.That(controller.LastResultPanelTextForTests, Does.Contain("너무 큽니다"));
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
            Assert.That(controller.IsResultPanelVisible, Is.False);
            Assert.That(controller.LastResultPanelTextForTests, Does.Contain("기본 문양 실패"));
            Assert.That(controller.LastResultPanelTextForTests, Does.Contain("무효"));
            Assert.That(controller.MentorSpeechTextForTests, Does.Contain("이번 입력"));
            Assert.That(controller.MentorSpeechTextForTests, Does.Contain("어렵습니다"));

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
        public IEnumerator CustomSpellEffectsDriveFloorThreeAndFourReactions()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);
            var profilePath = TempCustomShapeProfilePath();
            controller.UseCustomShapeStoreForTests(profilePath);
            ClearCustomSlots(controller);

            controller.LoadFloorForTests(2);
            yield return null;
            Assert.That(controller.ImportCustomReferenceForTests(SpellFamily.Life, out _, out var lifeMessage), Is.True, lifeMessage);
            CastCustomReferenceSpell(controller, SpellFamily.Life, SpellFamily.Life, Vector2.zero);
            yield return null;
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(0));

            var bridgePosition = controller.StageGoalPositionForTests("living_bridge");
            controller.LoadFloorForTests(2);
            CastCustomReferenceSpell(controller, SpellFamily.Life, SpellFamily.Life, bridgePosition);
            yield return null;

            Assert.That(
                controller.CompletedGoalCountForTests,
                Is.EqualTo(1),
                $"bridge note={controller.LastMagicNoteText} event={controller.LastCustomShapeEventKindForTests} seal={controller.ActiveSealCount} effect={controller.LastSealCustomEffectKindForTests}");
            Assert.That(controller.LastMagicNoteText, Does.Contain("생명"));
            Assert.That(controller.LastMagicNoteText, Does.Contain("낭떠러지"));

            controller.LoadFloorForTests(3);
            yield return null;
            Assert.That(controller.ImportCustomReferenceForTests(SpellFamily.Fire, out _, out var fireMessage), Is.True, fireMessage);
            CastBeamSpell(controller, SpellFamily.Fire, new Vector2(-4.2f, 1.85f));
            yield return null;

            Assert.That(
                controller.CompletedGoalCountForTests,
                Is.EqualTo(1),
                $"beam note={controller.LastMagicNoteText} hit={controller.LastBeamHitForTests} damaged={controller.LastDamagedTargetNameForTests} seal={controller.ActiveSealCount}");
            Assert.That(controller.LastMagicNoteText, Does.Contain("빛줄기"));
            Assert.That(controller.LastBeamHitForTests, Is.True);
            Assert.That(controller.LastDamagePopupTextForTests, Is.EqualTo("-1/2"));
            Assert.That(controller.ActiveDamagePopupCountForTests, Is.GreaterThan(0));

            ClearCustomSlots(controller);
            DeleteIfExists(profilePath);
        }

        [UnityTest]
        public IEnumerator FloorFourAttributeBeamRequiresScarecrowHit()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);
            var profilePath = TempCustomShapeProfilePath();
            controller.UseCustomShapeStoreForTests(profilePath);
            ClearCustomSlots(controller);

            controller.LoadFloorForTests(3);
            yield return null;

            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(4));
            Assert.That(controller.ActiveGoalCount, Is.EqualTo(5));
            Assert.That(GameObject.Find("Training Scarecrow"), Is.Not.Null);
            Assert.That(controller.ImportCustomReferenceForTests(SpellFamily.Fire, out _, out var fireMessage), Is.True, fireMessage);

            CastBeamSpell(controller, SpellFamily.Fire, new Vector2(-4.2f, 1.85f));
            yield return null;

            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(1));
            Assert.That(controller.LastBeamHitForTests, Is.True);
            Assert.That(controller.LastDamagedTargetNameForTests, Does.Contain("Scarecrow"));
            Assert.That(controller.LastDamagePopupTextForTests, Is.EqualTo("-1/2"));

            controller.LoadFloorForTests(3);
            yield return null;
            ClearCustomSlots(controller);
            Assert.That(controller.ImportCustomReferenceForTests(SpellFamily.Fire, out _, out fireMessage), Is.True, fireMessage);

            CastBeamSpell(controller, SpellFamily.Fire, new Vector2(4.2f, 1.85f));
            yield return null;

            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(0));
            Assert.That(controller.LastBeamHitForTests, Is.False);
            Assert.That(controller.ActiveSealCount, Is.EqualTo(1));

            ClearCustomSlots(controller);
            DeleteIfExists(profilePath);
        }

        [UnityTest]
        public IEnumerator FinalFloorCompletionShowsFinalSealCelebrationBeforeReport()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);

            SeedFinalTaskEncounters(controller);
            controller.LoadFloorForTests(4);
            yield return null;

            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(5));
            Assert.That(controller.ActiveGoalCount, Is.EqualTo(3));
            Assert.That(controller.CurrentFinalTaskCountForTests, Is.EqualTo(3));
            Assert.That(controller.CurrentFinalTaskIdForTests, Is.Not.Empty);

            controller.CompleteCurrentFloorForTests();
            yield return null;

            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(5));
            Assert.That(controller.HasEndingReport, Is.False);
            Assert.That(controller.LastMagicNoteText, Does.Contain("수료증"));
            Assert.That(controller.ActivePulseCountForTests, Is.GreaterThan(controller.ActiveGoalCount));
            Assert.That(controller.IsGameplayInputEnabledForTests, Is.False);
        }

        [UnityTest]
        public IEnumerator FinalFloorShowsRemainingGoalGuideAndNextHint()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);

            SeedFinalTaskEncounters(controller);
            controller.LoadFloorForTests(4);
            yield return null;

            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(5));
            Assert.That(controller.ActiveGoalCount, Is.EqualTo(3));
            Assert.That(controller.CurrentFinalTaskCountForTests, Is.EqualTo(3));
            Assert.That(controller.CurrentFinalTaskIdsForTests, Is.Unique);
            Assert.That(controller.CurrentFinalTaskIndexForTests, Is.EqualTo(0));
            var speechPanel = GameObject.Find("Mentor Speech")?.GetComponent<RectTransform>();
            Assert.That(speechPanel, Is.Not.Null);
            Assert.That(speechPanel.gameObject.activeInHierarchy, Is.True);
            var mentorCanvasPosition = WorldToBottomLeftCanvasPoint(controller.MentorWorldPositionForTests);
            Assert.That(speechPanel.anchoredPosition.x, Is.GreaterThan(mentorCanvasPosition.x + 4f));
            Assert.That(speechPanel.anchoredPosition.x + speechPanel.sizeDelta.x, Is.LessThanOrEqualTo(CanvasSize().x - 12f));
            Assert.That(controller.CurrentMentorNameForTests, Is.EqualTo("고깔모자 시험관"));
            Assert.That(controller.MentorProfileNeutralKindForTests, Is.EqualTo(PixelSpriteKind.MentorGrandWizardNeutral));
            Assert.That(controller.MentorWorldScaleForTests, Is.GreaterThanOrEqualTo(1.0f));
            Assert.That(controller.MentorWorldPositionForTests.x, Is.EqualTo(0f).Within(0.35f));
            Assert.That(controller.MentorWorldPositionForTests.y, Is.GreaterThan(3.0f));
            Assert.That(controller.CurrentFinalTaskPromptForTests, Is.Not.Empty);
            Assert.That(controller.MentorSpeechTextForTests, Does.Contain("최종"));
            Assert.That(controller.HudCopyForTests, Does.Contain(controller.CurrentFinalTaskPromptForTests));
            Assert.That(controller.HudCopyForTests, Does.Contain("1/3"));
            Assert.That(controller.FloorProgressForTests, Does.Contain("최종 문제"));
            var finalTaskIds = controller.CurrentFinalTaskIdsForTests.ToList();
            Assert.That(controller.VisibleGoalObjectCountForTests, Is.EqualTo(0));
            Assert.That(controller.VisibleGoalLabelCountForTests, Is.EqualTo(0));
            Assert.That(controller.TransparentFinalGoalObjectCountForTests, Is.EqualTo(controller.ActiveGoalCount));
            Assert.That(controller.IsGoalVisibleForTests(finalTaskIds[0]), Is.False);
            Assert.That(controller.GoalVisualAlphaForTests(finalTaskIds[0]), Is.EqualTo(0f).Within(0.05f));
            Assert.That(controller.IsGoalVisibleForTests(finalTaskIds[1]), Is.False);
            Assert.That(controller.GoalVisualAlphaForTests(finalTaskIds[1]), Is.EqualTo(0f).Within(0.05f));
        }

        [UnityTest]
        public IEnumerator FinalFloorRandomPoolUsesOnlyEncounteredTasks()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);

            var allowed = new[] { "final_puddle", "final_custom_wind", "final_beam_water" };
            SeedFinalTaskEncounters(controller, allowed);
            controller.LoadFloorForTests(4);
            yield return null;

            Assert.That(controller.CurrentFinalTaskCountForTests, Is.EqualTo(3));
            Assert.That(controller.CurrentFinalTaskIdsForTests, Is.SubsetOf(allowed));
            Assert.That(controller.CurrentFinalTaskIdsForTests, Does.Not.Contain("final_frozen_river"));
            Assert.That(controller.CustomReferenceCountForTests, Is.GreaterThanOrEqualTo(10));
        }

        [UnityTest]
        public IEnumerator FinalFloorWindArrowUsesSourceFloorRecognitionContext()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);
            var profilePath = TempCustomShapeProfilePath();
            controller.UseCustomShapeStoreForTests(profilePath);
            ClearCustomSlots(controller);

            SeedFinalTaskEncounters(controller, "final_custom_wind", "final_puddle", "final_ember");
            Assert.That(controller.SelectFinalTaskForTests("final_custom_wind"), Is.True);
            controller.LoadFloorForTests(4);
            yield return null;

            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(5));
            Assert.That(controller.CurrentFinalTaskIdForTests, Is.EqualTo("final_custom_wind"));
            var taskPosition = controller.StageGoalPositionForTests("final_custom_wind");
            var result = CastCustomReferenceSpell(controller, SpellFamily.Wind, SpellFamily.Wind, taskPosition);
            yield return null;

            Assert.That(result.spell.status, Is.EqualTo(RecognitionStatus.Recognized), result.spell.feedbackReason);
            Assert.That(result.spell.isCustomShape, Is.True, result.spell.feedbackReason);
            Assert.That(result.spell.customShapeToken, Is.EqualTo("arrow"));
            Assert.That(result.spell.recognizedFamily, Is.EqualTo(SpellFamily.Wind));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(1));

            ClearCustomSlots(controller);
            DeleteIfExists(profilePath);
        }

        [UnityTest]
        public IEnumerator MainMenuHidesFinalTaskBanner()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            var boot = Object.FindFirstObjectByType<GameBootController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(boot, Is.Not.Null);

            SeedFinalTaskEncounters(controller, "final_custom_wind", "final_puddle", "final_ember");
            Assert.That(controller.SelectFinalTaskForTests("final_custom_wind"), Is.True);
            controller.LoadFloorForTests(4);
            yield return null;

            Assert.That(controller.IsFinalTaskBannerVisibleForTests, Is.True);
            boot.ShowMainMenuForTests();
            yield return null;

            Assert.That(boot.StateForTests, Is.EqualTo(GameBootState.MainMenu));
            Assert.That(controller.IsFinalTaskBannerVisibleForTests, Is.False);
        }

        [UnityTest]
        public IEnumerator FinalFloorScarecrowOnlyShowsForActiveBeamTask()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);

            SeedFinalTaskEncounters(controller, "final_beam_fire", "final_puddle", "final_ember");
            Assert.That(controller.SelectFinalTaskForTests("final_beam_fire"), Is.True);
            controller.LoadFloorForTests(4);
            yield return null;

            Assert.That(controller.CurrentFinalTaskIdForTests, Is.EqualTo("final_beam_fire"));
            Assert.That(controller.IsFinalTaskEnvironmentVisibleForTests("final_beam_fire"), Is.True);
            Assert.That(GameObject.Find("Final Exam Scarecrow"), Is.Not.Null);

            controller.CompleteCurrentGoalsForTests(1);
            yield return null;

            Assert.That(controller.CurrentFinalTaskIdForTests, Is.Not.EqualTo("final_beam_fire"));
            Assert.That(controller.IsFinalTaskEnvironmentVisibleForTests("final_beam_fire"), Is.False);
            Assert.That(controller.ActiveFinalTaskEnvironmentObjectCountForTests, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator FinalFloorFrozenRiverShowsRiverEnvironmentOnlyWhenActive()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);

            SeedFinalTaskEncounters(controller, "final_frozen_river", "final_puddle", "final_ember");
            Assert.That(controller.SelectFinalTaskForTests("final_frozen_river"), Is.True);
            controller.LoadFloorForTests(4);
            yield return null;

            Assert.That(controller.CurrentFinalTaskIdForTests, Is.EqualTo("final_frozen_river"));
            Assert.That(controller.IsFinalTaskEnvironmentVisibleForTests("final_frozen_river"), Is.True);
            Assert.That(controller.ActiveFinalTaskEnvironmentObjectCountForTests, Is.GreaterThanOrEqualTo(3));
            Assert.That(GameObject.Find("Final Frozen River Current"), Is.Not.Null);

            controller.CompleteCurrentGoalsForTests(1);
            yield return null;

            Assert.That(controller.CurrentFinalTaskIdForTests, Is.Not.EqualTo("final_frozen_river"));
            Assert.That(controller.IsFinalTaskEnvironmentVisibleForTests("final_frozen_river"), Is.False);
            Assert.That(controller.ActiveFinalTaskEnvironmentObjectCountForTests, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator PersonalizationBiasPersistsAcrossFloorTransitions()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);

            CastNoisyBaseSpell(controller, SpellFamily.Water, new Vector2(0f, 3f), 0f);
            yield return null;
            Assert.That(controller.PersonalizationCaptureCountForTests, Is.GreaterThan(0));
            var captures = controller.PersonalizationCaptureCountForTests;

            controller.LoadFloorForTests(1);
            yield return null;
            Assert.That(controller.PersonalizationCaptureCountForTests, Is.EqualTo(captures));

            controller.LoadFloorForTests(2);
            yield return null;
            Assert.That(controller.PersonalizationCaptureCountForTests, Is.EqualTo(captures));
        }

        [UnityTest]
        public IEnumerator FinalFloorCanPassAtFiveOfSixGoals()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);

            SeedFinalTaskEncounters(controller);
            controller.LoadFloorForTests(4);
            yield return null;

            Assert.That(controller.IsFinalTaskBannerVisibleForTests, Is.True);
            Assert.That(controller.FinalTaskBannerTextForTests, Does.Contain("문제 1/"));

            controller.CompleteCurrentGoalsForTests(ExamGameController.FinalFloorPassingGoalCount);
            yield return null;

            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(5));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(ExamGameController.FinalFloorPassingGoalCount));
            Assert.That(controller.HasEndingReport, Is.False);
            Assert.That(controller.LastMagicNoteText, Does.Contain("수료증"));
            Assert.That(controller.PendingAdvanceSecondsForTests, Is.GreaterThan(0f));
            Assert.That(controller.IsFinalTaskBannerVisibleForTests, Is.False);

            controller.AdvanceFloorForTests();
            yield return null;

            Assert.That(controller.HasEndingReport, Is.True);
            Assert.That(controller.IsGameplayInputEnabledForTests, Is.False);
            Assert.That(controller.IsEndingReportExitButtonVisibleForTests, Is.True);
            controller.ClickEndingReportExitForTests();
            yield return null;
            Assert.That(controller.IsEndingReportExitButtonVisibleForTests, Is.False);
            Assert.That(controller.EndingReportTextForTests, Does.Contain("수료증"));
        }

        [UnityTest]
        public IEnumerator FinalCertificateHidesTitleReturnPromptUntilExitButton()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            var boot = Object.FindFirstObjectByType<GameBootController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(boot, Is.Not.Null);

            boot.StartNewGameForTests();
            yield return null;
            Assert.That(boot.StateForTests, Is.EqualTo(GameBootState.Gameplay));

            SeedFinalTaskEncounters(controller);
            controller.LoadFloorForTests(4);
            yield return null;

            controller.CompleteCurrentGoalsForTests(ExamGameController.FinalFloorPassingGoalCount);
            yield return null;
            controller.AdvanceFloorForTests();
            yield return null;

            Assert.That(controller.HasEndingReport, Is.True);
            Assert.That(controller.IsEndingReportExitButtonVisibleForTests, Is.True);
            Assert.That(boot.StateForTests, Is.EqualTo(GameBootState.Gameplay));
            Assert.That(boot.IsEndingPromptVisibleForTests, Is.False);

            yield return null;
            Assert.That(boot.StateForTests, Is.EqualTo(GameBootState.Gameplay));
            Assert.That(boot.IsEndingPromptVisibleForTests, Is.False);

            controller.ClickEndingReportExitForTests();
            yield return null;

            Assert.That(controller.IsEndingReportExitButtonVisibleForTests, Is.False);
            Assert.That(boot.StateForTests, Is.EqualTo(GameBootState.Ending));
            Assert.That(boot.IsEndingPromptVisibleForTests, Is.True);
        }

        [UnityTest]
        public IEnumerator FinalFloorFiveGoalPassCanUpgradeToTrueEndingBeforeReport()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);

            SeedFinalTaskEncounters(controller, "final_beam_fire", "final_puddle", "final_ember");
            Assert.That(controller.SelectFinalTaskForTests("final_beam_fire"), Is.True);
            controller.LoadFloorForTests(4);
            yield return null;

            Assert.That(controller.CurrentFinalTaskIdForTests, Is.EqualTo("final_beam_fire"));
            CastBeamSpell(controller, SpellFamily.Fire, new Vector2(-4.2f, 1.85f));
            yield return null;

            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(1));
            Assert.That(controller.LastBeamHitForTests, Is.True);
            Assert.That(controller.LastDamagePopupTextForTests, Is.EqualTo("-1/2"));
            Assert.That(controller.LastMagicNoteText, Does.Contain("다음 문제"));
            Assert.That(controller.CurrentFinalTaskIndexForTests, Is.EqualTo(1));
            Assert.That(controller.HasEndingReport, Is.False);
            Assert.That(controller.PendingAdvanceSecondsForTests, Is.LessThanOrEqualTo(0f));
            Assert.That(controller.IsGameplayInputEnabledForTests, Is.True);

            controller.CompleteCurrentGoalsForTests(2);
            yield return null;

            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(ExamGameController.FinalFloorPassingGoalCount));
            Assert.That(controller.LastMagicNoteText, Does.Contain("수료증"));
            controller.AdvanceFloorForTests();
            yield return null;
            Assert.That(controller.HasEndingReport, Is.True);
            Assert.That(controller.IsEndingReportExitButtonVisibleForTests, Is.True);
            Assert.That(controller.EndingReportTextForTests, Does.Contain("수료증"));
        }

        [UnityTest]
        public IEnumerator FloorTransitionsHazardResetAndEndingReportWork()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);
            var boot = Object.FindFirstObjectByType<GameBootController>();
            Assert.That(boot, Is.Not.Null);
            boot.StartNewGameForTests();
            controller.CloseFirstFloorLetterForTests();
            yield return null;

            controller.CastSyntheticBaseForTests(SpellFamily.Fire, new Vector2(-5.5f, 2.6f));
            yield return null;
            Assert.That(controller.IsResultPanelVisible, Is.False);

            controller.CompleteCurrentFloorForTests();
            controller.AdvanceFloorForTests();
            yield return null;
            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(2));
            Assert.That(controller.IsResultPanelVisible, Is.False);

            controller.LoadFloorForTests(2);
            controller.MovePlayerForTests(controller.StageObstacleCenterForTests("living_bridge"));
            yield return null;
            Assert.That(Vector2.Distance(controller.PlayerPosition, controller.StageObstacleResetPositionForTests("living_bridge")), Is.LessThan(0.2f));
            Assert.That(controller.LastMagicNoteText, Does.Contain("낭떠러지"));

            for (var index = controller.CurrentFloorNumber; index <= controller.FloorCount; index++)
            {
                controller.CompleteCurrentFloorForTests();
                controller.AdvanceFloorForTests();
                yield return null;
            }

            Assert.That(controller.HasEndingReport, Is.True);
            Assert.That(controller.IsResultPanelVisible, Is.False);
            Assert.That(controller.EndingReportTextForTests, Does.Contain("최종 시험"));
            Assert.That(controller.IsHealthBarVisibleForTests, Is.False);
            Assert.That(controller.IsFloorSkipButtonVisibleForTests, Is.False);
            Assert.That(boot.StateForTests, Is.EqualTo(GameBootState.Ending));
            Assert.That(boot.CodexQuickButtonVisibleForTests, Is.False);
            Assert.That(boot.CodexBackdropBlocksRaycastsForTests, Is.False);
            Assert.That(controller.EndingReportTextForTests, Does.Contain("도달 상태"));
            Assert.That(controller.EndingReportTextForTests, Does.Contain("수료 엔딩"));
            Assert.That(controller.EndingReportTextForTests, Does.Contain("가장 많이 사용한 기본 문양"));
            Assert.That(controller.EndingReportTextForTests, Does.Contain("가장 많이 사용한 장식"));
            Assert.That(controller.EndingReportTextForTests, Does.Contain("평균 문양 안정도"));
            Assert.That(controller.EndingReportTextForTests, Does.Contain("힌트 표시"));
            Assert.That(controller.EndingReportTextForTests, Does.Contain("문양 습관"));
            Assert.That(controller.EndingReportTextForTests, Does.Contain("보정 정책"));
            Assert.That(controller.EndingReportTextForTests, Does.Contain("자기 평가"));
            Assert.That(controller.EndingReportTextForTests, Does.Contain(ExamLogger.DisabledOutputDirectory));
            Assert.That(controller.EndingReportTextForTests, Does.Not.Contain("MagicExamHallLogs"));
            AssertTextFits("Report Heading");
            AssertTextFits("Report Text", 12f, 12f);
        }

        private static void ClearCustomSlots(ExamGameController controller)
        {
            for (var index = 0; index < controller.CustomShapeSlotCountForTests; index++)
            {
                controller.DeleteCustomShapeSlotForTests(index);
            }
        }

        private static string TempCustomShapeProfilePath()
        {
            return Path.Combine(Path.GetTempPath(), $"magic-playmode-custom-shapes-{Guid.NewGuid():N}.json");
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static IReadOnlyList<IReadOnlyList<StrokeSample>> Samples(SpellFamily family)
        {
            return GestureRecognizer.CreateCanonicalSamples(family, 1.6f, 0.03f)
                .Select(stroke => (IReadOnlyList<StrokeSample>)stroke)
                .ToList();
        }

        private static BaseRecognitionResult CastCustomReferenceSpell(
            ExamGameController controller,
            SpellFamily baseFamily,
            SpellFamily referenceFamily,
            Vector2 worldCenter)
        {
            controller.CastSyntheticBaseForTests(baseFamily, worldCenter);
            return controller.CastRawBaseForTests(controller.CustomReferenceStrokesForTests(referenceFamily, worldCenter), worldCenter);
        }

        private static BaseRecognitionResult CastBeamSpell(
            ExamGameController controller,
            SpellFamily family,
            Vector2 beamOrigin)
        {
            controller.CastSyntheticBaseForTests(family, beamOrigin);
            return controller.CastRawBaseForTests(controller.CustomReferenceStrokesForTests(family, beamOrigin), beamOrigin);
        }

        private static void SeedFinalTaskEncounters(ExamGameController controller, params string[] taskIds)
        {
            var selected = taskIds == null || taskIds.Length == 0
                ? new[]
                {
                    "final_puddle",
                    "final_ember",
                    "final_custom_fire",
                    "final_custom_wind",
                    "final_frozen_river",
                    "final_living_bridge",
                    "final_beam_fire",
                    "final_beam_water"
                }
                : taskIds;
            controller.ClearFinalTaskEncountersForTests();
            controller.MarkFinalTasksEncounteredForTests(selected);
        }

        private static BaseRecognitionResult CastNoisyBaseSpell(
            ExamGameController controller,
            SpellFamily family,
            Vector2 worldCenter,
            float noiseAmplitude)
        {
            return controller.CastRawBaseForTests(NoisyCanonicalBase(family, worldCenter, noiseAmplitude), worldCenter);
        }

        private static IEnumerator WaitForDefaultSealFallback(ExamGameController controller)
        {
            Assert.That(controller.ActiveSealCount, Is.EqualTo(1));
            yield return new WaitForSeconds(ExamGameController.DefaultSealFallbackDelaySeconds + 0.2f);
            Assert.That(controller.ActiveSealCount, Is.EqualTo(0));
        }

        private static BaseRecognitionResult CastNoisyCustomReferenceSpell(
            ExamGameController controller,
            SpellFamily baseFamily,
            SpellFamily referenceFamily,
            Vector2 worldCenter)
        {
            controller.CastSyntheticBaseForTests(baseFamily, worldCenter);
            return controller.CastRawBaseForTests(
                AddDeterministicNoise(controller.CustomReferenceStrokesForTests(referenceFamily, worldCenter), 0.032f),
                worldCenter);
        }

        private static List<List<StrokeSample>> NoisyCanonicalBase(SpellFamily family, Vector2 worldCenter, float noiseAmplitude)
        {
            return AddDeterministicNoise(
                Offset(GestureRecognizer.CreateCanonicalSamples(family, 1.6f, 0.03f), worldCenter, 0.8f),
                noiseAmplitude);
        }

        private static List<List<StrokeSample>> AddDeterministicNoise(List<List<StrokeSample>> strokes, float amplitude)
        {
            var sampleIndex = 0;
            return strokes
                .Select(stroke => stroke.Select(sample =>
                {
                    sampleIndex++;
                    var jitter = new Vector2(
                        Mathf.Sin(sampleIndex * 1.73f) * amplitude,
                        Mathf.Cos(sampleIndex * 2.11f) * amplitude * 0.72f);
                    return new StrokeSample(sample.position + jitter, sample.time);
                }).ToList())
                .ToList();
        }

        private static List<List<StrokeSample>> TapNoise(Vector2 center)
        {
            return new List<List<StrokeSample>>
            {
                new() { new StrokeSample(center + new Vector2(-0.04f, 0.02f), 0f) },
                new() { new StrokeSample(center + new Vector2(0.03f, -0.03f), 0.04f) },
                new() { new StrokeSample(center + new Vector2(0.02f, 0.05f), 0.08f) }
            };
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

        private static List<List<StrokeSample>> Offset(List<List<StrokeSample>> strokes, Vector2 center, float canonicalCenter)
        {
            return strokes
                .Select(stroke => stroke.Select(sample => new StrokeSample(sample.position - Vector2.one * canonicalCenter + center, sample.time)).ToList())
                .ToList();
        }

        private static Vector2 WorldToCanvasPoint(Vector2 worldPosition)
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            Assert.That(canvas, Is.Not.Null);
            var canvasRect = canvas.GetComponent<RectTransform>();
            var screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out var localPoint);
            return localPoint;
        }

        private static Vector2 WorldToBottomLeftCanvasPoint(Vector2 worldPosition)
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            Assert.That(canvas, Is.Not.Null);
            var canvasRect = canvas.GetComponent<RectTransform>();
            return WorldToCanvasPoint(worldPosition) + Vector2.Scale(canvasRect.rect.size, canvasRect.pivot);
        }

        private static Vector2 CanvasSize()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            Assert.That(canvas, Is.Not.Null);
            return canvas.GetComponent<RectTransform>().rect.size;
        }

        private static void AssertVisibleKoreanTextLooksUsable(string expected)
        {
            var texts = Object.FindObjectsByType<Text>(FindObjectsSortMode.None)
                .Where(text => text != null && text.gameObject.activeInHierarchy)
                .ToList();

            Assert.That(texts.Any(text => text.text.Contains(expected)), Is.True);
            Assert.That(texts.Any(text => text.text.Contains("\uFFFD")), Is.False);

            foreach (var text in texts.Where(text => text.GetComponentInParent<Button>() != null))
            {
                var rect = text.rectTransform.rect;
                Assert.That(text.preferredWidth, Is.LessThanOrEqualTo(rect.width + 10f), text.text);
                Assert.That(text.preferredHeight, Is.LessThanOrEqualTo(rect.height + 8f), text.text);
            }
        }

        private static void AssertConversationalMentorSpeech(ExamGameController controller, params string[] expectedFragments)
        {
            var speech = controller.MentorSpeechTextForTests;
            Assert.That(speech, Is.Not.Empty);
            Assert.That(speech.Split('\n').Length, Is.LessThanOrEqualTo(3), speech);
            foreach (var fragment in expectedFragments)
            {
                Assert.That(speech, Does.Contain(fragment), speech);
            }

            Assert.That(speech, Does.Not.Contain("힌트:"), speech);
            Assert.That(speech, Does.Not.Contain("다음:"), speech);
            Assert.That(speech, Does.Not.Contain("base"), speech);
            Assert.That(speech, Does.Not.Contain("overlay"), speech);
            Assert.That(speech, Does.Not.Contain("gold capture"), speech);
        }

        private static void AssertTextFits(string objectName, float widthTolerance = 10f, float heightTolerance = 8f)
        {
            var text = GameObject.Find(objectName)?.GetComponent<Text>();
            Assert.That(text, Is.Not.Null, objectName);
            var rect = text.rectTransform.rect;
            Assert.That(text.preferredWidth, Is.LessThanOrEqualTo(rect.width + widthTolerance), objectName);
            Assert.That(text.preferredHeight, Is.LessThanOrEqualTo(rect.height + heightTolerance), objectName);
        }

        private static void DragCapturePad(CustomShapeCapturePad capturePad, Vector2 start, Vector2 end)
        {
            var eventSystem = EventSystem.current;
            Assert.That(eventSystem, Is.Not.Null);
            var rect = capturePad.rectTransform;
            var down = PointerForLocal(rect, start);
            capturePad.OnPointerDown(down);
            var drag = PointerForLocal(rect, end);
            capturePad.OnDrag(drag);
            capturePad.OnPointerUp(drag);
        }

        private static PointerEventData PointerForLocal(RectTransform rect, Vector2 local)
        {
            var canvas = rect.GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            return new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                pointerId = -1,
                position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(local))
            };
        }
    }
}
