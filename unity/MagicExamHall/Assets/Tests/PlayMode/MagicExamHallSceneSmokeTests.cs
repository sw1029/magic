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
            Assert.That(controller.VersionLabelForTests, Is.EqualTo(ExamGameController.BuildVersion));
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
            Assert.That(controller.VisibleOverlayGuideCountForTests, Is.EqualTo(1));
            Assert.That(controller.IsDrawingPanelVisible, Is.False);
            Assert.That(controller.IsResultPanelVisible, Is.True);
            Assert.That(controller.LastResultPanelTextForTests, Does.Contain("base 성공"));
            Assert.That(controller.LastResultPanelTextForTests, Does.Contain("불꽃"));
            Assert.That(controller.LastResultPanelTextForTests, Does.Contain("품질"));
            Assert.That(controller.LastResultPanelTextForTests, Does.Contain("해석"));
            Assert.That(controller.LastResultPanelTextForTests, Does.Contain("이유"));
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

            controller.OpenCustomShapePenPopupForTests();
            yield return null;
            Assert.That(controller.IsCustomPenPopupVisibleForTests, Is.True);

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
            Assert.That(shapeSection, Is.Not.Null);
            Assert.That(mappingSection, Is.Not.Null);
            var shapeImage = shapeSection.GetComponent<Image>();
            var mappingImage = mappingSection.GetComponent<Image>();
            Assert.That(Mathf.Abs(shapeImage.color.r - mappingImage.color.r) + Mathf.Abs(shapeImage.color.g - mappingImage.color.g), Is.GreaterThan(0.04f));
            var sidePreviewIcon = GameObject.Find("Custom Shape Side Preview 01")?.GetComponent<Image>();
            Assert.That(sidePreviewIcon?.sprite?.name, Does.Contain(":2"));
            var shapeCorners = new Vector3[4];
            var mappingCorners = new Vector3[4];
            shapeSection.GetWorldCorners(shapeCorners);
            mappingSection.GetWorldCorners(mappingCorners);
            Assert.That(shapeCorners[0].y, Is.GreaterThan(mappingCorners[1].y + 4f));
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
            Assert.That(familyReelViewport, Is.Not.Null);
            Assert.That(familyReelViewport.showMaskGraphic, Is.True);
            Assert.That(familyReelContent, Is.Not.Null);
            Assert.That(familyReelIcons.Count, Is.GreaterThan(3));
            Assert.That(familyReelIcons.Select(image => image.rectTransform.localScale.x), Is.All.EqualTo(1f).Within(0.001f));
            Assert.That(familyReelIcons.Select(image => image.rectTransform.localScale.y), Is.All.EqualTo(1f).Within(0.001f));
            var familyReelCenterLine = GameObject.Find("Custom Shape Family Reel Center Line")?.transform;
            Assert.That(familyReelCenterLine, Is.Not.Null);
            Assert.That(familyReelCenterLine.GetSiblingIndex(), Is.LessThan(familyReelContent.GetSiblingIndex()));
            Assert.That(familyUpButton, Is.Not.Null);
            var familyReelRestY = familyReelContent.anchoredPosition.y;
            familyUpButton.onClick.Invoke();
            yield return null;
            Assert.That(Mathf.Abs(familyReelContent.anchoredPosition.y - familyReelRestY), Is.GreaterThan(2f));
            var earlyReelY = familyReelContent.anchoredPosition.y;
            for (var frame = 0; frame < 20; frame++)
            {
                yield return null;
            }

            Assert.That(Mathf.Abs(familyReelContent.anchoredPosition.y - earlyReelY), Is.GreaterThan(8f));
            Assert.That(familyReelIcons.Select(image => image.rectTransform.localScale.x), Is.All.EqualTo(1f).Within(0.001f));
            Assert.That(familyReelIcons.Select(image => image.rectTransform.localScale.y), Is.All.EqualTo(1f).Within(0.001f));
            for (var frame = 0; frame < 100; frame++)
            {
                yield return null;
            }

            Assert.That(familyReelContent.anchoredPosition.y, Is.EqualTo(familyReelRestY + 34f).Within(2f));
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
            Assert.That(controller.SaveCustomShapeSlotForTests(10, "목표 바람", "목표|바람|line", SpellFamily.Wind, gold, out var message), Is.True, message);

            var worldStrokes = Offset(GestureRecognizer.CreateCanonicalSamples(SpellFamily.Wind, 1.6f, 0.03f), windGoal, 0.8f);
            var result = controller.CastRawBaseForTests(worldStrokes, windGoal);
            yield return null;

            Assert.That(result.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(result.spell.isCustomShape, Is.True);
            Assert.That(result.spell.customShapeLabel, Is.EqualTo("목표 바람"));
            Assert.That(result.spell.recognizedFamily, Is.EqualTo(SpellFamily.Wind));
            Assert.That(controller.CompletedGoalCountForTests, Is.EqualTo(1));

            ClearCustomSlots(controller);
            DeleteIfExists(profilePath);
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

            controller.CastSyntheticBaseForTests(SpellFamily.Fire, new Vector2(-5.5f, 2.6f));
            yield return null;
            Assert.That(controller.IsResultPanelVisible, Is.True);

            controller.CompleteCurrentFloorForTests();
            controller.AdvanceFloorForTests();
            yield return null;
            Assert.That(controller.CurrentFloorNumber, Is.EqualTo(2));
            Assert.That(controller.IsResultPanelVisible, Is.False);

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
