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
using UnityEngine.UI;
using Object = UnityEngine.Object;

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
            Assert.That(controller.IsResultPanelVisible, Is.False);
            Assert.That(controller.VisibleOverlayGuideCountForTests, Is.EqualTo(0));
            Assert.That(controller.ActiveShelfGuideArrowCountForTests, Is.EqualTo(2));
            Assert.That(GameObject.Find("West Bookcase Guide Arrow"), Is.Not.Null);
            Assert.That(GameObject.Find("East Bookcase Guide Arrow"), Is.Not.Null);
            Assert.That(controller.VersionLabelForTests, Is.EqualTo(ExamGameController.BuildVersion));
            Assert.That(controller.IsFirstFloorLetterVisibleForTests, Is.True);
            Assert.That(controller.FirstFloorLetterTextForTests.Split('\n').Length, Is.EqualTo(10));
            Assert.That(controller.FirstFloorLetterTextForTests, Does.Contain("첫 번째 시험"));
            var letterOverlay = GameObject.Find("First Floor Letter Overlay")?.GetComponent<Image>();
            Assert.That(letterOverlay, Is.Not.Null);
            Assert.That(letterOverlay.color.a, Is.GreaterThan(0.75f));
            var closeButton = GameObject.Find("First Floor Letter Close Button")?.GetComponent<Button>();
            Assert.That(closeButton, Is.Not.Null);
            Assert.That(closeButton.GetComponentInChildren<Text>().text, Is.EqualTo("X"));
            Assert.That(controller.FirstFloorLetterCloseButtonColorForTests.r, Is.GreaterThan(0.70f));
            Assert.That(controller.FirstFloorLetterCloseButtonColorForTests.g, Is.LessThan(0.08f));
            Assert.That(controller.FirstFloorLetterCloseButtonColorForTests.b, Is.LessThan(0.08f));

            closeButton.onClick.Invoke();
            yield return null;

            Assert.That(controller.IsFirstFloorLetterVisibleForTests, Is.False);
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
            Assert.That(controller.ActiveShelfGuideArrowCountForTests, Is.EqualTo(2));
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
            Assert.That(GameObject.Find("Quest Scroll Top Roll"), Is.Not.Null);
            Assert.That(GameObject.Find("Quest Scroll Bottom Roll"), Is.Not.Null);

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
            Assert.That(controller.QuestChecklistSavedCompletedForTests, Is.EqualTo(1));
            Assert.That(controller.QuestChecklistSavedTotalForTests, Is.EqualTo(3));
            Assert.That(controller.QuestChecklistGlobalCompletedForTests, Is.EqualTo(1));
            Assert.That(controller.QuestChecklistGlobalTotalForTests, Is.EqualTo(7));
            Assert.That(controller.QuestChecklistTitleForTests, Does.Contain("층 2"));
            Assert.That(controller.QuestChecklistSnapshotSummaryForTests, Does.Contain("1층 1/3 - skip"));
            Assert.That(File.ReadAllText(Path.Combine(controller.OutputDirectory, "quest-checklist.csv")), Does.Contain("skip"));

            var floorTwoLabels = ActiveQuestLabels();
            Assert.That(floorTwoLabels.Any(label => label.Contains("책장", StringComparison.Ordinal)), Is.True);

            controller.LoadFloorForTests(2);
            yield return null;

            Assert.That(controller.QuestChecklistTitleForTests, Does.Contain("층 3"));
            Assert.That(controller.QuestChecklistTotalForTests, Is.EqualTo(5));
            Assert.That(ActiveQuestLabels().Any(label => label.Contains("강물을", StringComparison.Ordinal)), Is.True);
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

            Assert.That(controller.CurrentHealthHalfUnitsForTests, Is.EqualTo(4));
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
            var target = GameObject.Find("Training Target ice_training");
            var targetEntity = target?.GetComponent<ElementalEntity>();
            Assert.That(targetEntity, Is.Not.Null, "training target elemental entity");
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
            Assert.That(controller.HudCopyForTests, Does.Contain("Esc/Backspace"));
            Assert.That(controller.LastMagicNoteText, Does.Contain("표식 근처"));
            Assert.That(controller.LastMagicNoteText, Does.Contain("물은 닫힌 원"));
            Assert.That(controller.LastMagicNoteText, Does.Contain("바람"));
        }

        [UnityTest]
        public IEnumerator GoalLabelsUseVisualRequirementIconsOnFloorsOneToThree()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);

            AssertVisualGoalRequirementRows(controller, expectedPlusCount: 0);

            controller.LoadFloorForTests(1);
            yield return null;
            AssertVisualGoalRequirementRows(controller, expectedPlusCount: 5);
            Assert.That(GameObject.Find("Goal Requirement Icon custom_fire 2"), Is.Not.Null);
            Assert.That(GameObject.Find("Goal Requirement Icon custom_water 2"), Is.Not.Null);
            Assert.That(GameObject.Find("Goal Requirement Icon custom_wind 2"), Is.Not.Null);
            Assert.That(GameObject.Find("Goal Requirement Icon custom_earth 2"), Is.Not.Null);
            Assert.That(GameObject.Find("Goal Requirement Icon custom_life 2"), Is.Not.Null);

            controller.LoadFloorForTests(2);
            yield return null;
            AssertVisualGoalRequirementRows(controller, expectedPlusCount: 5);
            Assert.That(GameObject.Find("Goal Requirement Icon frozen_river 2"), Is.Not.Null);
            Assert.That(GameObject.Find("Goal Requirement Icon earth_stairs 2"), Is.Not.Null);
            Assert.That(GameObject.Find("Goal Requirement Icon living_bridge 2"), Is.Not.Null);
            Assert.That(GameObject.Find("Goal Requirement Icon living_bridge 3"), Is.Not.Null);
            Assert.That(GameObject.Find("Goal Requirement Icon wind_platform 2"), Is.Not.Null);
        }

        private static void AssertVisualGoalRequirementRows(ExamGameController controller, int expectedPlusCount)
        {
            var labels = Object.FindObjectsByType<Text>(FindObjectsSortMode.None)
                .Where(text => text.name == "Goal Label Text" && text.gameObject.activeInHierarchy)
                .ToList();

            Assert.That(labels.Count, Is.EqualTo(controller.ActiveGoalCount));
            foreach (var label in labels)
            {
                var rect = label.rectTransform.rect;
                Assert.That(label.text, Does.Not.Contain("+"));
                Assert.That(label.text.Count(ch => ch == '\n'), Is.EqualTo(0), label.text);
                Assert.That(rect.width, Is.GreaterThanOrEqualTo(180f), label.text);
                Assert.That(rect.height, Is.GreaterThanOrEqualTo(24f), label.text);
                Assert.That(label.horizontalOverflow, Is.EqualTo(HorizontalWrapMode.Overflow), label.text);
                Assert.That(label.preferredWidth, Is.LessThanOrEqualTo(rect.width + 18f), label.text);
                Assert.That(label.preferredHeight, Is.LessThanOrEqualTo(rect.height + 10f), label.text);
            }

            var rows = Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None)
                .Where(rect => rect.name.StartsWith("Goal Requirement Icon Row", StringComparison.Ordinal) && rect.gameObject.activeInHierarchy)
                .ToList();
            var icons = Object.FindObjectsByType<Image>(FindObjectsSortMode.None)
                .Where(image => image.name.StartsWith("Goal Requirement Icon ", StringComparison.Ordinal) &&
                                !image.name.StartsWith("Goal Requirement Icon Row", StringComparison.Ordinal) &&
                                image.gameObject.activeInHierarchy)
                .ToList();
            var pluses = Object.FindObjectsByType<Text>(FindObjectsSortMode.None)
                .Where(text => text.name.StartsWith("Goal Requirement Plus", StringComparison.Ordinal) && text.gameObject.activeInHierarchy)
                .ToList();

            Assert.That(rows.Count, Is.EqualTo(controller.ActiveGoalCount));
            Assert.That(icons.Count, Is.GreaterThanOrEqualTo(controller.ActiveGoalCount));
            Assert.That(icons.Select(image => image.sprite), Is.All.Not.Null);
            Assert.That(pluses.Count, Is.EqualTo(expectedPlusCount));
            Assert.That(pluses.Select(text => text.text), Is.All.EqualTo("+"));
        }

        private static List<string> ActiveQuestLabels()
        {
            return Object.FindObjectsByType<Text>(FindObjectsSortMode.None)
                .Where(text => text.name.StartsWith("Quest Checklist Label", StringComparison.Ordinal) && text.gameObject.activeInHierarchy)
                .Select(text => text.text)
                .ToList();
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

            var windGoal = controller.StageGoalPositionForTests("vane");
            var drawCenter = Vector2.zero;
            var strokes = Offset(GestureRecognizer.CreateCanonicalSamples(SpellFamily.Wind, 1.6f, 0.03f), drawCenter, 0.8f);

            controller.MovePlayerForTests(windGoal);
            var result = controller.CastRawBaseForTests(strokes, drawCenter, movePlayerToReference: false);
            yield return null;

            Assert.That(result.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(result.spell.recognizedFamily, Is.EqualTo(SpellFamily.Wind));
            Assert.That(result.spell.intentGoalId, Is.EqualTo("vane"));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(1));
            var seal = controller.GetActiveSealSnapshots().Single();
            Assert.That(Vector2.Distance(seal.worldCenter, windGoal), Is.LessThan(0.05f));
            Assert.That(Vector2.Distance(seal.worldCenter, drawCenter), Is.GreaterThan(1f));

            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;
            controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);
            controller.UseCustomShapeStoreForTests(profilePath);
            ClearCustomSlots(controller);

            var goalStrokes = Offset(GestureRecognizer.CreateCanonicalSamples(SpellFamily.Wind, 1.6f, 0.03f), windGoal, 0.8f);
            controller.MovePlayerForTests(drawCenter);
            var offTarget = controller.CastRawBaseForTests(goalStrokes, windGoal, movePlayerToReference: false);
            yield return null;

            Assert.That(offTarget.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(offTarget.spell.intentGoalId, Is.Empty);
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(0));

            ClearCustomSlots(controller);
            DeleteIfExists(profilePath);
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
            Assert.That(controller.LastSealLifetimeSecondsForTests, Is.EqualTo(SpellRuntime.DefaultSealDurationSeconds).Within(0.001f));
            Assert.That(controller.VisibleOverlayGuideCountForTests, Is.EqualTo(0));
            Assert.That(controller.IsDrawingPanelVisible, Is.False);
            Assert.That(controller.IsResultPanelVisible, Is.True);
            Assert.That(controller.LastResultPanelTextForTests, Does.Contain("base 성공"));
            Assert.That(controller.LastResultPanelTextForTests, Does.Contain("불꽃"));
            Assert.That(controller.LastResultPanelTextForTests, Does.Contain("품질"));
            Assert.That(controller.LastResultPanelTextForTests, Does.Contain("해석"));
            Assert.That(controller.LastResultPanelTextForTests, Does.Contain("이유"));
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

            yield return new WaitForSeconds(ExamGameController.DefaultSealFallbackDelaySeconds + 0.2f);

            Assert.That(controller.ActiveDefaultBarrierCountForTests, Is.EqualTo(1));
            var barrierColor = controller.LastDefaultBarrierColorForTests;
            Assert.That(barrierColor.r, Is.EqualTo(1f).Within(0.01f));
            Assert.That(barrierColor.g, Is.EqualTo(0.31f).Within(0.01f));
            Assert.That(barrierColor.b, Is.EqualTo(0.18f).Within(0.01f));
            Assert.That(GameObject.Find("Default Barrier " + controller.GetActiveSealSnapshots()[0].sealId), Is.Not.Null);
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
            Assert.That(controller.IsResultPanelVisible, Is.True);

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
            Assert.That(controller.IsResultPanelVisible, Is.True);

            var idleCancel = drawing.CancelBufferedInput();
            yield return null;

            Assert.That(idleCancel, Is.False);
            Assert.That(controller.IsResultPanelVisible, Is.True);
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
            Assert.That(controller.LastMagicNoteText, Does.Contain("커스텀 도형"));

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
            Assert.That(controller.VisibleOverlayGuideCountForTests, Is.EqualTo(0));
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

            controller.OpenCustomShapePenPopupForTests();
            yield return null;
            Assert.That(controller.IsCustomPenPopupVisibleForTests, Is.True);
            var penPopup = GameObject.Find("Custom Shape Pen Popup")?.GetComponent<RectTransform>();
            Assert.That(penPopup, Is.Not.Null);
            var penStart = penPopup.anchoredPosition;
            yield return new WaitForSecondsRealtime(0.45f);

            Assert.That(Mathf.Abs(penPopup.anchoredPosition.x - penStart.x), Is.LessThan(0.75f));
            Assert.That(Mathf.Abs(penPopup.anchoredPosition.y - penStart.y), Is.InRange(0.35f, 7f));
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
            Assert.That(editorRect.rect.width, Is.GreaterThan(850f));
            Assert.That(editorRect.rect.height, Is.GreaterThan(468f));
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
            Assert.That(eventLabel.text, Does.Contain("이벤트"));
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
        public IEnumerator CustomSlotCastCanCompleteMappedDefaultGoal()
        {
            SceneManager.LoadScene("MagicExamHall");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<ExamGameController>();
            Assert.That(controller, Is.Not.Null);
            var profilePath = TempCustomShapeProfilePath();
            controller.UseCustomShapeStoreForTests(profilePath);
            ClearCustomSlots(controller);
            var windGoal = new Vector2(5.5f, 2.6f);
            var gold = Samples(SpellFamily.Wind);
            Assert.That(controller.SaveCustomShapeSlotForTests(10, "목표 바람", "목표|바람|rect", "rect", SpellFamily.Wind, gold, out var message), Is.True, message);

            var worldStrokes = Offset(GestureRecognizer.CreateCanonicalSamples(SpellFamily.Wind, 1.6f, 0.03f), windGoal, 0.8f);
            var result = controller.CastRawBaseForTests(worldStrokes, windGoal);
            yield return null;

            Assert.That(result.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(result.spell.isCustomShape, Is.True);
            Assert.That(result.spell.customShapeLabel, Is.EqualTo("목표 바람"));
            Assert.That(result.spell.customShapeToken, Is.EqualTo("rect"));
            Assert.That(result.spell.customEventKind, Is.EqualTo(CustomShapeEventKind.WallEntity.ToString()));
            Assert.That(controller.LastCustomShapeEventKindForTests, Is.EqualTo(CustomShapeEventKind.WallEntity.ToString()));
            Assert.That(controller.CustomShapeEventObjectCountForTests, Is.GreaterThan(0));
            Assert.That(result.spell.recognizedFamily, Is.EqualTo(SpellFamily.Wind));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(1));
            Assert.That(controller.LastMagicNoteText, Does.Contain("커스텀 이벤트"));

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
            Assert.That(controller.HudCopyForTests, Does.Contain("좌측 책장"));
            Assert.That(controller.CustomReferenceCountForTests, Is.EqualTo(5));

            controller.MovePlayerForTests(new Vector2(-7.25f, 1.1f));
            yield return null;
            Assert.That(controller.IsCustomReferenceBubbleVisibleForTests, Is.True);
            AssertVisibleKoreanTextLooksUsable("도형 레퍼런스");

            controller.OpenCustomReferencePanelForTests();
            yield return null;
            Assert.That(controller.IsCustomReferencePanelOpenForTests, Is.True);
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

            controller.CastSyntheticBaseForTests(SpellFamily.Fire, new Vector2(-5.4f, 2.55f));
            yield return null;
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(1));
            Assert.That(controller.LastMagicNoteText, Does.Contain("커스텀 도형으로만"));

            var existingWaterSlot = Enumerable
                .Range(0, controller.CustomShapeSlotCountForTests)
                .First(index => controller.IsCustomShapeSlotOccupiedForTests(index) &&
                                controller.CustomShapeSlotMappedFamilyForTests(index) == SpellFamily.Water);
            controller.LoadFloorForTests(2);
            yield return null;
            Assert.That(controller.ImportCustomReferenceForTests(SpellFamily.Water, out var replacedWaterSlot, out var replaceMessage), Is.True, replaceMessage);
            Assert.That(replacedWaterSlot, Is.EqualTo(existingWaterSlot));
            Assert.That(controller.IsCustomShapeSlotOccupiedForTests(replacedWaterSlot), Is.True);
            Assert.That(controller.CustomShapeSlotMappedFamilyForTests(replacedWaterSlot), Is.EqualTo(SpellFamily.Water));

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

            Assert.That(controller.ImportCustomReferenceForTests(SpellFamily.Fire, out _, out var fireMessage), Is.True, fireMessage);
            var fireGoal = new Vector2(-5.4f, 2.55f);
            var customFire = controller.CastRawBaseForTests(controller.CustomReferenceStrokesForTests(SpellFamily.Fire, fireGoal), fireGoal);
            yield return null;
            Assert.That(customFire.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(customFire.spell.isCustomShape, Is.True, customFire.spell.feedbackReason);
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(1));
            Assert.That(controller.ActiveSealCount, Is.GreaterThan(0));

            ClearCustomSlots(controller);
            Assert.That(controller.ImportCustomReferenceForTests(SpellFamily.Water, out _, out var waterMessage), Is.True, waterMessage);
            var waterGoal = new Vector2(-2.7f, 3.0f);
            var customWater = controller.CastRawBaseForTests(controller.CustomReferenceStrokesForTests(SpellFamily.Water, waterGoal), waterGoal);
            yield return null;
            Assert.That(customWater.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(customWater.spell.isCustomShape, Is.True, customWater.spell.feedbackReason);
            Assert.That(customWater.spell.recognizedFamily, Is.EqualTo(SpellFamily.Water));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(2));

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

            Assert.That(controller.ActiveShelfGuideArrowCountForTests, Is.EqualTo(1));
            Assert.That(GameObject.Find("Crossing Reference Bookcase Guide Arrow"), Is.Not.Null);
            Assert.That(GameObject.Find("Start Stone Walkway Underside"), Is.Not.Null);
            Assert.That(GameObject.Find("River Vertical Channel Core"), Is.Not.Null);
            Assert.That(GameObject.Find("River Lower Drop Shadow"), Is.Not.Null);
            Assert.That(GameObject.Find("River Bank Left Cliff Face"), Is.Not.Null);
            Assert.That(GameObject.Find("Broken Floor Vertical Rupture Core"), Is.Not.Null);
            Assert.That(GameObject.Find("Broken Floor Lower Void"), Is.Not.Null);
            Assert.That(GameObject.Find("Broken Floor Inner Void"), Is.Not.Null);
            Assert.That(GameObject.Find("Chasm Vertical Shaft Core"), Is.Not.Null);
            Assert.That(GameObject.Find("Chasm Far Abyss"), Is.Not.Null);
            Assert.That(GameObject.Find("Chasm Left Cliff Wall"), Is.Not.Null);
            Assert.That(GameObject.Find("Wind Gap Vertical Shaft Core"), Is.Not.Null);
            Assert.That(GameObject.Find("Wind Gap Lower Depth"), Is.Not.Null);
            Assert.That(GameObject.Find("Wind Gap Mist"), Is.Not.Null);
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
            controller.MovePlayerForTests(controller.CustomReferenceShelfPositionForTests);
            yield return null;
            Assert.That(controller.IsCustomReferenceBubbleVisibleForTests, Is.True);

            CastCustomReferenceSpell(controller, SpellFamily.Life, SpellFamily.Life, Vector2.zero);
            yield return null;
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(0));
            Assert.That(controller.ActiveStageEffectVisualCountForTests, Is.EqualTo(0));

            var icePosition = controller.StageGoalPositionForTests("frozen_river");
            CastCustomReferenceSpell(controller, SpellFamily.Water, SpellFamily.Water, icePosition);
            yield return null;
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(1));
            Assert.That(controller.ActiveStageEntityCountForTests, Is.EqualTo(1));
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
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(2));
            Assert.That(controller.ActiveStageEntityCountForTests, Is.EqualTo(2));
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
            var combatIcePosition = new Vector2(-4.8f, 1.55f);
            CastCustomReferenceSpell(controller, SpellFamily.Water, SpellFamily.Water, combatIcePosition);
            yield return null;
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(1));
            Assert.That(controller.ActiveDamagePopupCountForTests, Is.GreaterThan(0));
            Assert.That(controller.LastDamagePopupTextForTests, Does.Contain("-"));

            ClearCustomSlots(controller);
            DeleteIfExists(profilePath);
        }

        [UnityTest]
        public IEnumerator BuffQueuesAndMapleStyleDamageNumbersAttachToEntities()
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
            var combatIcePosition = new Vector2(-4.8f, 1.55f);
            CastCustomReferenceSpell(controller, SpellFamily.Water, SpellFamily.Water, combatIcePosition);
            yield return null;

            Assert.That(controller.ActiveTargetBuffSlotCountForTests, Is.EqualTo(1));
            Assert.That(GameObject.Find("Buff Queue Training Target ice_training"), Is.Not.Null);
            Assert.That(GameObject.Find("Buff Slot 감속"), Is.Not.Null);
            Assert.That(controller.ActiveDamagePopupCountForTests, Is.GreaterThan(0));
            Assert.That(controller.LastDamagePopupTextForTests, Does.Match(@"^-\d+"));
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
            Assert.That(controller.LastMagicNoteText, Does.Contain("커스텀 도형"));
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

            controller.CastSyntheticBaseForTests(SpellFamily.Earth, Vector2.zero);
            var result = controller.CastSyntheticOverlayForTests(OverlayOperator.SoulDot, Vector2.zero, 1f);
            yield return null;

            Assert.That(result.success, Is.False);
            Assert.That(result.recognizedOperator, Is.EqualTo(OverlayOperator.SoulDot));
            Assert.That(result.scaleHint, Is.EqualTo(OverlayScaleHint.TooLarge));
            Assert.That(controller.LastMagicNoteText, Does.Contain("너무 커"));
            Assert.That(controller.LastHintText, Does.Contain("너무 큽니다"));
            Assert.That(controller.IsResultPanelVisible, Is.True);
            Assert.That(controller.LastResultPanelTextForTests, Does.Contain("overlay 실패"));
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
            Assert.That(controller.IsResultPanelVisible, Is.True);
            Assert.That(controller.LastResultPanelTextForTests, Does.Contain("base 실패"));
            Assert.That(controller.LastResultPanelTextForTests, Does.Contain("무효"));

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

            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(1));
            Assert.That(controller.LastMagicNoteText, Does.Contain("생명"));
            Assert.That(controller.LastMagicNoteText, Does.Contain("낭떠러지"));

            var electricPosition = new Vector2(-1.6f, 2.15f);
            controller.LoadFloorForTests(3);
            yield return null;
            Assert.That(controller.ImportCustomReferenceForTests(SpellFamily.Fire, out _, out var fireMessage), Is.True, fireMessage);
            CastCustomReferenceSpell(controller, SpellFamily.Fire, SpellFamily.Fire, electricPosition);
            yield return null;

            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(1));
            Assert.That(controller.LastMagicNoteText, Does.Contain("번개"));
            Assert.That(controller.ActiveDamagePopupCountForTests, Is.GreaterThan(0));

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

            controller.CastSyntheticBaseForTests(SpellFamily.Fire, new Vector2(-5.5f, 2.6f));
            yield return null;
            Assert.That(controller.IsResultPanelVisible, Is.True);

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
            Assert.That(controller.EndingReportTextForTests, Does.Contain("입학 시험"));
            Assert.That(controller.EndingReportTextForTests, Does.Contain("도달 상태"));
            Assert.That(controller.EndingReportTextForTests, Does.Contain("진엔딩 (6/6 완전 복구)"));
            Assert.That(controller.EndingReportTextForTests, Does.Contain("가장 많이 사용한 base"));
            Assert.That(controller.EndingReportTextForTests, Does.Contain("가장 많이 사용한 overlay"));
            Assert.That(controller.EndingReportTextForTests, Does.Contain("평균 문양 안정도"));
            Assert.That(controller.EndingReportTextForTests, Does.Contain("힌트 표시"));
            Assert.That(controller.EndingReportTextForTests, Does.Contain("문양 습관"));
            Assert.That(controller.EndingReportTextForTests, Does.Contain("보정 정책"));
            Assert.That(controller.EndingReportTextForTests, Does.Contain("자기 평가"));
            Assert.That(controller.EndingReportTextForTests, Does.Contain("MagicExamHallLogs"));
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

        private static List<List<StrokeSample>> Offset(List<List<StrokeSample>> strokes, Vector2 center, float canonicalCenter)
        {
            return strokes
                .Select(stroke => stroke.Select(sample => new StrokeSample(sample.position - Vector2.one * canonicalCenter + center, sample.time)).ToList())
                .ToList();
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
