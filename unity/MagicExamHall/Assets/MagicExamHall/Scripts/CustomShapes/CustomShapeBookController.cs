using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MagicExamHall
{
    public sealed class CustomShapeBookController
    {
        private readonly List<Button> slotButtons = new();
        private readonly List<Image> slotIcons = new();
        private readonly List<Button> shapeButtons = new();
        private readonly List<Image> shapeButtonIcons = new();
        private Canvas canvas = null!;
        private Camera mainCamera = null!;
        private Transform player = null!;
        private Font uiFont = null!;
        private CustomShapeProfileStore store = null!;
        private RectTransform penPopup = null!;
        private RectTransform modalRoot = null!;
        private RectTransform pagePanel = null!;
        private RectTransform wheelRoot = null!;
        private RectTransform bubble = null!;
        private Text bubbleText = null!;
        private RectTransform editorScrim = null!;
        private RectTransform editorShadow = null!;
        private RectTransform editorRoot = null!;
        private InputField labelInput = null!;
        private Text familyLabel = null!;
        private readonly List<Image> familyReelIcons = new();
        private readonly List<SpellFamily> familyReelFamilies = new();
        private RectTransform familyReelViewport = null!;
        private RectTransform familyReelContent = null!;
        private Text shapeEventLabel = null!;
        private Text editorStatus = null!;
        private CustomShapeCapturePad capturePad = null!;
        private Image captureTemplatePreview = null!;
        private Image captureStrokePreview = null!;
        private int selectedSlotIndex = -1;
        private SpellFamily editorFamily = SpellFamily.Wind;
        private string editorShapeToken = "line";
        private Vector2 penPopupVelocity;
        private float penFloatPhase;
        private int familyReelVirtualIndex;
        private float familyReelPosition;
        private float familyReelTarget;
        private float familyReelVelocity;
        private BubbleMode bubbleMode = BubbleMode.None;
        private static Sprite circleSprite = null!;
        private static readonly Vector2 BaseEditorPopupSize = new(940f, 522f);
        private static readonly Vector2 CapturePanelSize = new(480f, 310f);
        private static readonly Vector2 FamilyReelViewportPosition = new(128f, 12f);
        private static readonly Vector2 FamilyReelViewportSize = new(136f, 112f);
        private const float EditorTitleBarHeight = 54f;
        private const float FamilySlotSpacing = 34f;
        private const float FamilyReelTopPadding = 18f;
        private const int FamilyReelCycles = 3;
        private const float FamilyReelKickVelocity = 420f;
        private const float FamilyReelMaxVelocity = 820f;
        private const float FamilyReelSpring = 62f;
        private const float FamilyReelDamping = 8.5f;
        private const float FamilyReelFadeDistance = 58f;
        private const float SlotPreviewStrokeWidth = 2f;
        private const float SidePreviewStrokeWidth = 2f;
        private const int SavedSlotPreviewStrokeWidth = 2;
        private const float PenPopupHeightOffset = 74f;
        private const float PenPopupFloatAmplitude = 5f;
        private const float PenPopupFloatSpeed = 1.35f;
        private const float PenPopupSpring = 24f;
        private const float PenPopupDamping = 8.5f;
        private const float PenPopupMaxVelocity = 260f;

        private enum BubbleMode
        {
            None,
            Add,
            Delete
        }

        public bool IsPenPopupVisible => penPopup != null && penPopup.gameObject.activeSelf;
        public bool IsPageOpen => modalRoot != null && modalRoot.gameObject.activeSelf;
        public bool IsBubbleVisible => bubble != null && bubble.gameObject.activeSelf;
        public bool IsEditorOpen => editorRoot != null;
        public bool BlocksGameplayInput => IsPageOpen;
        public int SlotCount => CustomShapeProfileStore.SlotCount;

        public void Initialize(Canvas targetCanvas, Camera targetCamera, Transform targetPlayer, Font targetFont, CustomShapeProfileStore targetStore)
        {
            canvas = targetCanvas;
            mainCamera = targetCamera;
            player = targetPlayer;
            uiFont = targetFont;
            store = targetStore;
        }

        public void Tick()
        {
            if (canvas == null || player == null || mainCamera == null)
            {
                return;
            }

            if (IsPenPopupVisible)
            {
                UpdatePenPopupMotion(Time.unscaledDeltaTime);
            }

            if (IsEditorOpen)
            {
                UpdateFamilyCarouselAnimation(Time.unscaledDeltaTime);
            }

            if (!Input.GetMouseButtonDown(0) || PointerIsOverUi())
            {
                return;
            }

            if (ScreenPointHitsPlayer(Input.mousePosition))
            {
                TogglePenPopup();
            }
        }

        public bool IsSlotOccupied(int index)
        {
            return store != null && store.IsSlotOccupied(index);
        }

        public string SlotLabel(int index)
        {
            if (store == null)
            {
                return "";
            }

            var slot = store.GetSlot(index);
            return slot.IsOccupied ? slot.label : "";
        }

        public SpellFamily SlotMappedFamily(int index)
        {
            return store == null ? SpellFamily.Wind : store.GetSlot(index).mappedFamily;
        }

        public void OpenPenPopupForTests()
        {
            EnsurePenPopup();
            penPopup.gameObject.SetActive(true);
            UpdatePenPopupMotion(0f, true);
        }

        public void OpenPageForTests()
        {
            EnsurePenPopup();
            OpenPage();
        }

        public void RequestSlotForTests(int slotIndex)
        {
            EnsurePage();
            SelectSlot(slotIndex);
        }

        public void DeclineBubbleForTests()
        {
            HideBubble();
        }

        public void ConfirmBubbleForTests()
        {
            ConfirmBubble();
        }

        public bool SaveSlotForTests(
            int slotIndex,
            string label,
            string regexPattern,
            SpellFamily mappedFamily,
            IReadOnlyList<IReadOnlyList<StrokeSample>> goldStrokes,
            out string message)
        {
            var shapeToken = CustomShapeProfileStore.HelperTokens.FirstOrDefault(token => regexPattern?.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0) ?? "line";
            return SaveSlotForTests(slotIndex, label, regexPattern, shapeToken, mappedFamily, goldStrokes, out message);
        }

        public bool SaveSlotForTests(
            int slotIndex,
            string label,
            string regexPattern,
            string shapeToken,
            SpellFamily mappedFamily,
            IReadOnlyList<IReadOnlyList<StrokeSample>> goldStrokes,
            out string message)
        {
            var saved = store.TrySaveSlot(slotIndex, label, regexPattern, shapeToken, new[] { shapeToken }, mappedFamily, goldStrokes, out message);
            if (saved)
            {
                CloseEditor();
                RefreshSlots();
            }

            return saved;
        }

        public bool DeleteSlotForTests(int slotIndex)
        {
            var deleted = store.DeleteSlot(slotIndex);
            if (deleted)
            {
                RefreshSlots();
            }

            return deleted;
        }

        public void RefreshFromStoreForExternalChange()
        {
            RefreshSlots();
        }

        private void TogglePenPopup()
        {
            EnsurePenPopup();
            var show = !penPopup.gameObject.activeSelf;
            penPopup.gameObject.SetActive(show);
            if (show)
            {
                UpdatePenPopupMotion(0f, true);
            }
        }

        private void EnsurePenPopup()
        {
            if (penPopup != null)
            {
                return;
            }

            penPopup = CreatePanel("Custom Shape Pen Popup", canvas.transform, Vector2.zero, new Vector2(160f, 90f), UiAnchor.Center, new Color(0.055f, 0.06f, 0.082f, 0.98f));
            var button = penPopup.gameObject.AddComponent<Button>();
            button.targetGraphic = penPopup.GetComponent<Image>();
            button.onClick.AddListener(OpenPage);
            CreateImage("Pen Popup Top Edge", penPopup, new Vector2(0f, -5f), new Vector2(144f, 4f), UiAnchor.TopCenter, new Color(0.88f, 0.13f, 0.08f, 0.95f)).raycastTarget = false;
            CreateImage("Pen Popup Bottom Edge", penPopup, new Vector2(0f, 6f), new Vector2(144f, 3f), UiAnchor.BottomCenter, new Color(0.95f, 0.58f, 0.22f, 0.78f)).raycastTarget = false;
            DrawPenIcon(penPopup);
            penPopup.gameObject.SetActive(false);
        }

        private void UpdatePenPopupMotion(float deltaTime, bool snap = false)
        {
            if (penPopup == null || player == null || canvas == null)
            {
                return;
            }

            var dt = Mathf.Clamp(deltaTime, 0f, 0.05f);
            if (!snap)
            {
                penFloatPhase += dt * PenPopupFloatSpeed;
            }

            var screen = RectTransformUtility.WorldToScreenPoint(mainCamera, player.position + new Vector3(0f, 1.15f, 0f));
            screen += new Vector2(0f, PenPopupHeightOffset + Mathf.Sin(penFloatPhase) * PenPopupFloatAmplitude);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvas.transform as RectTransform,
                    screen,
                    canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                    out var target))
            {
                return;
            }

            if (snap)
            {
                penPopup.anchoredPosition = target;
                penPopupVelocity = Vector2.zero;
                penPopup.localRotation = Quaternion.identity;
                return;
            }

            var current = penPopup.anchoredPosition;
            var acceleration = (target - current) * PenPopupSpring - penPopupVelocity * PenPopupDamping;
            penPopupVelocity += acceleration * dt;
            penPopupVelocity = Vector2.ClampMagnitude(penPopupVelocity, PenPopupMaxVelocity);
            penPopup.anchoredPosition = current + penPopupVelocity * dt;
            penPopup.localRotation = Quaternion.identity;
        }

        private void DrawPenIcon(Transform root)
        {
            var shaft = CreateImage("Pen Shaft", root, new Vector2(0f, 0f), new Vector2(104f, 16f), UiAnchor.Center, new Color(0.92f, 0.1f, 0.08f, 1f));
            shaft.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -24f);
            var highlight = CreateImage("Pen Highlight", shaft.transform, new Vector2(-9f, 3.5f), new Vector2(70f, 3f), UiAnchor.Center, new Color(1f, 0.52f, 0.44f, 1f));
            highlight.raycastTarget = false;
            var nib = CreateImage("Pen Nib", root, new Vector2(49f, -20f), new Vector2(24f, 12f), UiAnchor.Center, new Color(0.08f, 0.08f, 0.09f, 1f));
            nib.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -24f);
            var cap = CreateImage("Pen Cap", root, new Vector2(-50f, 21f), new Vector2(26f, 18f), UiAnchor.Center, new Color(0.48f, 0.02f, 0.04f, 1f));
            cap.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -24f);
        }

        private void OpenPage()
        {
            EnsurePage();
            modalRoot.gameObject.SetActive(true);
            penPopup?.gameObject.SetActive(false);
            RefreshSlots();
        }

        private void EnsurePage()
        {
            if (modalRoot != null)
            {
                return;
            }

            modalRoot = CreatePanel("Custom Shape Modal Root", canvas.transform, Vector2.zero, Vector2.zero, UiAnchor.Stretch, new Color(0f, 0f, 0f, 0.7f));
            modalRoot.SetAsLastSibling();
            pagePanel = CreatePanel("Custom Shape Page", modalRoot, Vector2.zero, new Vector2(960f, 540f), UiAnchor.Center, new Color(0.032f, 0.04f, 0.058f, 0.995f));
            AddSimpleBorder(pagePanel, new Color(0.25f, 0.34f, 0.48f, 0.82f), 2f);
            CreateText("Custom Shape Title", pagePanel, "커스텀 도형", 24, FontStyle.Bold, new Vector2(24f, -18f), new Vector2(360f, 36f), UiAnchor.TopLeft);
            CreateButton("Close Custom Shape Page", pagePanel, "닫기", 15, new Vector2(-22f, -18f), new Vector2(72f, 34f), UiAnchor.TopRight, ClosePage);

            wheelRoot = CreatePanel("Custom Shape Wheel Root", pagePanel, new Vector2(-136f, -4f), new Vector2(600f, 438f), UiAnchor.Center, new Color(0f, 0f, 0f, 0f));
            wheelRoot.GetComponent<Image>().raycastTarget = false;
            BuildWheel();
            BuildSidePanel();
            BuildBubble();
            modalRoot.gameObject.SetActive(false);
        }

        private void BuildWheel()
        {
            var outerRing = CreateCircleImage("Custom Shape Wheel Outer Ring", wheelRoot, Vector2.zero, new Vector2(454f, 454f), UiAnchor.Center, new Color(0.012f, 0.018f, 0.034f, 0.98f));
            outerRing.raycastTarget = false;
            var innerVoid = CreateCircleImage("Custom Shape Wheel Inner Void", wheelRoot, Vector2.zero, new Vector2(306f, 306f), UiAnchor.Center, new Color(0.032f, 0.04f, 0.058f, 0.995f));
            innerVoid.raycastTarget = false;

            var center = CreateCircleImage("Custom Shape Wheel Center", wheelRoot, Vector2.zero, new Vector2(162f, 162f), UiAnchor.Center, new Color(0.06f, 0.15f, 0.1f, 0.98f));
            center.raycastTarget = false;
            var playerPreview = CreateImage("Custom Shape Wheel Player Preview", center.transform, new Vector2(0f, 4f), new Vector2(108f, 108f), UiAnchor.Center, Color.white);
            playerPreview.sprite = PixelArtFactory.CreateSprite(
                "Custom Shape Wheel Player Preview",
                new Color(0.95f, 0.92f, 0.78f, 1f),
                new Color(0.28f, 0.62f, 0.96f, 1f),
                PixelSpriteKind.Player);
            playerPreview.preserveAspect = true;
            playerPreview.raycastTarget = false;
            var previewLabel = CreateText("Custom Shape Wheel Preview", center.transform, "미리보기", 12, FontStyle.Bold, new Vector2(0f, -58f), new Vector2(144f, 24f), UiAnchor.Center);
            previewLabel.alignment = TextAnchor.MiddleCenter;
            var radius = 184f;
            for (var index = 0; index < CustomShapeProfileStore.SlotCount; index++)
            {
                var angle = Mathf.PI * 2f * index / CustomShapeProfileStore.SlotCount + Mathf.PI / 2f;
                var position = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
                var captured = index;
                var rim = CreateCircleImage($"Custom Shape Slot {index + 1:00} Rim", wheelRoot, position, new Vector2(90f, 90f), UiAnchor.Center, new Color(0.015f, 0.026f, 0.052f, 0.98f));
                rim.raycastTarget = false;
                var button = CreateButton($"Custom Shape Slot {index + 1:00}", wheelRoot, "", 13, position, new Vector2(82f, 82f), UiAnchor.Center, () => SelectSlot(captured));
                button.image.sprite = CircleSprite;
                button.image.preserveAspect = true;
                button.image.color = new Color(0.06f, 0.12f, 0.21f, 0.96f);
                var icon = CreateShapeIcon($"Custom Shape Slot {index + 1:00} Icon", button.transform, CustomShapeProfileStore.HelperTokens[index % CustomShapeProfileStore.HelperTokens.Length], Vector2.zero, new Vector2(50f, 50f), UiAnchor.Center, new Color(1f, 1f, 1f, 0.42f), SlotPreviewStrokeWidth);
                icon.raycastTarget = false;
                slotIcons.Add(icon);
                slotButtons.Add(button);
            }
        }

        private void BuildSidePanel()
        {
            var side = CreatePanel("Custom Shape Side Panel", pagePanel, new Vector2(-28f, 10f), new Vector2(248f, 426f), UiAnchor.RightCenter, new Color(0.05f, 0.06f, 0.085f, 0.96f));
            CreateText("Custom Shape Side Status", side, "슬롯을 선택하세요", 18, FontStyle.Bold, new Vector2(14f, -18f), new Vector2(220f, 32f), UiAnchor.TopLeft);
            BuildSideShapePreviewGrid(side);
            CreateText("Custom Shape Mapping Copy", side, "기본 효과 매핑\n바람 / 땅 / 불꽃 / 물 / 생명", 13, FontStyle.Normal, new Vector2(14f, 112f), new Vector2(220f, 74f), UiAnchor.BottomLeft);
        }

        private void BuildSideShapePreviewGrid(Transform side)
        {
            for (var index = 0; index < CustomShapeProfileStore.HelperTokens.Length; index++)
            {
                var column = index % 4;
                var row = index / 4;
                var icon = CreateShapeIcon(
                    $"Custom Shape Side Preview {index + 1:00}",
                    side,
                    CustomShapeProfileStore.HelperTokens[index],
                    new Vector2(28f + column * 48f, -78f - row * 42f),
                    new Vector2(34f, 30f),
                    UiAnchor.TopLeft,
                    new Color(0.92f, 0.96f, 1f, 0.78f),
                    SidePreviewStrokeWidth);
                icon.raycastTarget = false;
            }
        }

        private void BuildBubble()
        {
            bubble = CreateSpeechBubble("Custom Shape Bubble", pagePanel, new Vector2(-136f, 12f), new Vector2(430f, 116f), UiAnchor.BottomCenter, new Color(0.98f, 0.96f, 0.88f, 0.98f));
            bubbleText = CreateText("Custom Shape Bubble Text", bubble, "", 18, FontStyle.Bold, new Vector2(20f, -30f), new Vector2(390f, 34f), UiAnchor.TopLeft);
            bubbleText.color = new Color(0.05f, 0.045f, 0.035f, 1f);
            CreateButton("Custom Shape Bubble Yes", bubble, "예", 16, new Vector2(-64f, 18f), new Vector2(96f, 34f), UiAnchor.BottomCenter, ConfirmBubble);
            CreateButton("Custom Shape Bubble No", bubble, "아니오", 16, new Vector2(64f, 18f), new Vector2(96f, 34f), UiAnchor.BottomCenter, HideBubble);
            bubble.gameObject.SetActive(false);
        }

        private void SelectSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= CustomShapeProfileStore.SlotCount)
            {
                return;
            }

            selectedSlotIndex = slotIndex;
            var occupied = store.IsSlotOccupied(slotIndex);
            bubbleMode = occupied ? BubbleMode.Delete : BubbleMode.Add;
            bubbleText.text = occupied ? "도형을 삭제하시겠습니까?" : "도형을 작성하시겠습니까?";
            bubble.gameObject.SetActive(true);
        }

        private void ConfirmBubble()
        {
            if (selectedSlotIndex < 0)
            {
                HideBubble();
                return;
            }

            if (bubbleMode == BubbleMode.Delete)
            {
                store.DeleteSlot(selectedSlotIndex);
                HideBubble();
                RefreshSlots();
                return;
            }

            if (bubbleMode == BubbleMode.Add)
            {
                HideBubble();
                OpenEditor(selectedSlotIndex);
            }
        }

        private void HideBubble()
        {
            bubbleMode = BubbleMode.None;
            if (bubble != null)
            {
                bubble.gameObject.SetActive(false);
            }
        }

        private void OpenEditor(int slotIndex)
        {
            CloseEditor();
            var slot = store.GetSlot(slotIndex);
            editorFamily = slot.mappedFamily;
            editorShapeToken = string.IsNullOrWhiteSpace(slot.shapeToken) ? "line" : slot.shapeToken;
            var editorSize = GetEditorPopupSize();

            editorScrim = CreatePanel("Custom Shape Editor Scrim", pagePanel, Vector2.zero, Vector2.zero, UiAnchor.Stretch, new Color(0f, 0f, 0f, 0.52f));
            editorScrim.SetAsLastSibling();
            editorShadow = CreatePanel("Custom Shape Editor Shadow", pagePanel, new Vector2(12f, -14f), editorSize + new Vector2(14f, 16f), UiAnchor.Center, new Color(0f, 0f, 0f, 0.46f));
            editorShadow.GetComponent<Image>().raycastTarget = false;
            editorShadow.SetAsLastSibling();
            editorRoot = CreatePanel("Custom Shape Editor", pagePanel, Vector2.zero, editorSize, UiAnchor.Center, new Color(0.025f, 0.032f, 0.048f, 0.99f));
            editorRoot.SetAsLastSibling();
            var titleBar = CreatePanel("Custom Shape Editor Title Bar", editorRoot, Vector2.zero, new Vector2(editorSize.x, EditorTitleBarHeight), UiAnchor.TopCenter, new Color(0.075f, 0.105f, 0.16f, 1f));
            titleBar.GetComponent<Image>().raycastTarget = false;
            var activeAccent = CreatePanel("Custom Shape Editor Active Accent", editorRoot, new Vector2(0f, -EditorTitleBarHeight), new Vector2(editorSize.x, 3f), UiAnchor.TopCenter, new Color(0.85f, 0.24f, 0.14f, 1f));
            activeAccent.GetComponent<Image>().raycastTarget = false;
            CreateText("Custom Shape Editor Title", editorRoot, $"슬롯 {slotIndex + 1:00}", 22, FontStyle.Bold, new Vector2(22f, -18f), new Vector2(180f, 34f), UiAnchor.TopLeft);
            CreateButton("Close Custom Shape Editor", editorRoot, "X", 15, new Vector2(-16f, -9f), new Vector2(38f, 32f), UiAnchor.TopRight, CloseEditor);
            labelInput = CreateInputField("Custom Shape Label Input", editorRoot, "이름", new Vector2(22f, -70f), new Vector2(270f, 42f), UiAnchor.TopLeft);
            BuildShapePalette(editorRoot);

            BuildFamilyCarousel(editorRoot);

            var padRoot = CreatePanel("Custom Shape Capture Panel", editorRoot, new Vector2(-42f, -74f), CapturePanelSize, UiAnchor.TopRight, new Color(0.92f, 0.94f, 0.9f, 1f));
            captureTemplatePreview = CreateShapeIcon("Custom Shape Capture Template Preview", padRoot, editorShapeToken, Vector2.zero, new Vector2(250f, 210f), UiAnchor.Center, new Color(0.12f, 0.13f, 0.12f, 0.34f), 6f);
            captureTemplatePreview.raycastTarget = false;
            captureStrokePreview = CreateImage("Custom Shape Capture Stroke Preview", padRoot, Vector2.zero, new Vector2(250f, 210f), UiAnchor.Center, new Color(0.92f, 0.1f, 0.08f, 1f));
            captureStrokePreview.sprite = null;
            captureStrokePreview.preserveAspect = true;
            captureStrokePreview.raycastTarget = false;
            captureStrokePreview.enabled = false;
            var drawSurface = new GameObject("Custom Shape Capture Draw Surface");
            drawSurface.transform.SetParent(padRoot, false);
            var drawRect = drawSurface.AddComponent<RectTransform>();
            ApplyAnchor(drawRect, UiAnchor.Stretch);
            drawRect.offsetMin = Vector2.zero;
            drawRect.offsetMax = Vector2.zero;
            drawSurface.AddComponent<CanvasRenderer>();
            capturePad = drawSurface.AddComponent<CustomShapeCapturePad>();
            capturePad.SetStrokeColor(FamilyColor(editorFamily));
            capturePad.raycastTarget = true;
            capturePad.SetTemplate(editorShapeToken);
            capturePad.onStrokesChanged = UpdateCaptureStrokePreview;
            var watermark = CreateText("Custom Shape Capture Watermark", padRoot, "드래그로 도형 배치", 16, FontStyle.Bold, Vector2.zero, new Vector2(260f, 42f), UiAnchor.Center);
            watermark.color = new Color(0.18f, 0.19f, 0.17f, 0.34f);
            watermark.raycastTarget = false;
            AddSimpleBorder(padRoot, new Color(0.46f, 0.52f, 0.56f, 0.82f), 1.5f);

            editorStatus = CreateText("Custom Shape Editor Status", editorRoot, "", 13, FontStyle.Normal, new Vector2(22f, 26f), new Vector2(440f, 34f), UiAnchor.BottomLeft);
            CreateButton("Custom Shape Undo", editorRoot, "되돌리기", 15, new Vector2(-392f, 26f), new Vector2(92f, 38f), UiAnchor.BottomRight, () => capturePad.UndoLastShape());
            CreateButton("Custom Shape Clear", editorRoot, "지우기", 15, new Vector2(-284f, 26f), new Vector2(92f, 38f), UiAnchor.BottomRight, () => capturePad.Clear());
            CreateButton("Custom Shape Save", editorRoot, "저장", 15, new Vector2(-176f, 26f), new Vector2(92f, 38f), UiAnchor.BottomRight, SaveEditor);
            CreateButton("Custom Shape Cancel", editorRoot, "취소", 15, new Vector2(-68f, 26f), new Vector2(92f, 38f), UiAnchor.BottomRight, CloseEditor);
            AddSimpleBorder(editorRoot, new Color(0.68f, 0.78f, 0.92f, 0.96f), 2f);
            UpdateShapeSelection();
            UpdateFamilyLabel();
        }

        private void BuildShapePalette(Transform parent)
        {
            shapeButtons.Clear();
            shapeButtonIcons.Clear();
            var section = CreatePanel("Custom Shape Section", parent, new Vector2(18f, -122f), new Vector2(294f, 206f), UiAnchor.TopLeft, new Color(0.043f, 0.058f, 0.086f, 0.985f));
            AddSimpleBorder(section, new Color(0.18f, 0.27f, 0.39f, 0.86f), 1.5f);
            CreateText("Custom Shape Palette Label", section, "도형", 15, FontStyle.Bold, new Vector2(10f, -8f), new Vector2(92f, 24f), UiAnchor.TopLeft);
            shapeEventLabel = CreateText("Custom Shape Event Label", section, "", 12, FontStyle.Bold, new Vector2(-12f, -9f), new Vector2(184f, 24f), UiAnchor.TopRight);
            shapeEventLabel.alignment = TextAnchor.MiddleRight;
            shapeEventLabel.color = new Color(0.75f, 0.92f, 0.98f, 0.92f);
            var viewport = CreatePanel("Custom Shape Palette Scroll View", section, new Vector2(10f, -42f), new Vector2(268f, 150f), UiAnchor.TopLeft, new Color(0.030f, 0.041f, 0.062f, 0.98f));
            var mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.inertia = true;
            scroll.decelerationRate = 0.12f;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 18f;
            scroll.viewport = viewport;
            var rowCount = Mathf.CeilToInt(CustomShapeProfileStore.HelperTokens.Length / 4f);
            var contentHeight = Mathf.Max(150f, rowCount * 52f + 8f);
            var content = new GameObject("Custom Shape Palette Content");
            content.transform.SetParent(viewport, false);
            var contentRect = content.AddComponent<RectTransform>();
            ApplyAnchor(contentRect, UiAnchor.TopLeft);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(258f, contentHeight);
            scroll.content = contentRect;
            var rail = CreatePanel("Custom Shape Palette Scroll Rail", section, new Vector2(-9f, -42f), new Vector2(3f, 150f), UiAnchor.TopRight, new Color(0.22f, 0.29f, 0.39f, 0.5f));
            rail.GetComponent<Image>().raycastTarget = false;
            for (var index = 0; index < CustomShapeProfileStore.HelperTokens.Length; index++)
            {
                var token = CustomShapeProfileStore.HelperTokens[index];
                var column = index % 4;
                var row = index / 4;
                var button = CreateButton($"Custom Shape Palette {token}", contentRect, "", 12, new Vector2(8f + column * 62f, -6f - row * 52f), new Vector2(50f, 44f), UiAnchor.TopLeft, () => SelectShapeToken(token));
                var icon = CreateShapeIcon($"Custom Shape Palette {token} Icon", button.transform, token, Vector2.zero, new Vector2(34f, 30f), UiAnchor.Center, Color.white, 3.2f);
                icon.raycastTarget = false;
                shapeButtons.Add(button);
                shapeButtonIcons.Add(icon);
            }

            scroll.verticalNormalizedPosition = 1f;
        }

        private void SelectShapeToken(string shapeToken)
        {
            editorShapeToken = CustomShapeProfileStore.HelperTokens.Contains(shapeToken) ? shapeToken : "line";
            capturePad?.SetTemplate(editorShapeToken);
            if (captureTemplatePreview != null)
            {
                SetShapeIcon(captureTemplatePreview, editorShapeToken, new Color(0.12f, 0.13f, 0.12f, 0.34f));
                captureTemplatePreview.enabled = capturePad == null || capturePad.PlacedShapeCount == 0;
            }

            if (captureStrokePreview != null)
            {
                captureStrokePreview.sprite = null;
                captureStrokePreview.enabled = false;
            }

            UpdateShapeSelection();
        }

        private void UpdateShapeSelection()
        {
            for (var index = 0; index < shapeButtons.Count; index++)
            {
                var token = CustomShapeProfileStore.HelperTokens[index];
                var selected = string.Equals(token, editorShapeToken, StringComparison.OrdinalIgnoreCase);
                shapeButtons[index].image.color = selected
                    ? new Color(0.78f, 0.18f, 0.10f, 0.98f)
                    : new Color(0.10f, 0.15f, 0.22f, 0.98f);
                shapeButtonIcons[index].color = selected
                    ? Color.white
                    : new Color(1f, 1f, 1f, 0.66f);
            }

            if (shapeEventLabel != null)
            {
                shapeEventLabel.text = CustomShapeEventCatalog.UiSummary(editorShapeToken);
            }
        }

        private void UpdateCaptureStrokePreview(IReadOnlyList<IReadOnlyList<StrokeSample>> strokes)
        {
            if (strokes == null || strokes.Count == 0)
            {
                DisableCaptureStrokePreview();
                if (captureTemplatePreview != null)
                {
                    captureTemplatePreview.enabled = true;
                }

                return;
            }

            if (captureTemplatePreview != null)
            {
                captureTemplatePreview.enabled = false;
            }

            DisableCaptureStrokePreview();
        }

        private void DisableCaptureStrokePreview()
        {
            if (captureStrokePreview == null)
            {
                return;
            }

            captureStrokePreview.sprite = null;
            captureStrokePreview.enabled = false;
        }

        private void BuildFamilyCarousel(Transform parent)
        {
            var root = CreatePanel("Custom Shape Family Carousel", parent, new Vector2(18f, 30f), new Vector2(294f, 138f), UiAnchor.BottomLeft, new Color(0.014f, 0.086f, 0.088f, 0.985f));
            AddSimpleBorder(root, new Color(0.14f, 0.36f, 0.35f, 0.82f), 1.5f);
            familyLabel = CreateText("Custom Shape Family Label", root, "", 16, FontStyle.Bold, new Vector2(14f, -10f), new Vector2(120f, 28f), UiAnchor.TopLeft);
            CreateButton("Custom Shape Family Up", root, "▲", 16, new Vector2(18f, 54f), new Vector2(42f, 34f), UiAnchor.BottomLeft, () => CycleFamily(1));
            CreateButton("Custom Shape Family Down", root, "▼", 16, new Vector2(18f, 14f), new Vector2(42f, 34f), UiAnchor.BottomLeft, () => CycleFamily(-1));
            familyReelViewport = CreatePanel("Custom Shape Family Reel Viewport", root, FamilyReelViewportPosition, FamilyReelViewportSize, UiAnchor.BottomLeft, new Color(0.018f, 0.034f, 0.041f, 0.98f));
            var mask = familyReelViewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;
            AddSimpleBorder(familyReelViewport, new Color(0.10f, 0.20f, 0.23f, 0.76f), 1f);

            var values = FamilyValues();
            var contentHeight = FamilyReelTopPadding * 2f + values.Count * FamilyReelCycles * FamilySlotSpacing;
            var content = new GameObject("Custom Shape Family Reel Content");
            content.transform.SetParent(familyReelViewport, false);
            familyReelContent = content.AddComponent<RectTransform>();
            ApplyAnchor(familyReelContent, UiAnchor.TopLeft);
            familyReelContent.sizeDelta = new Vector2(FamilyReelViewportSize.x, contentHeight);
            familyReelIcons.Clear();
            familyReelFamilies.Clear();
            for (var cycle = 0; cycle < FamilyReelCycles; cycle++)
            {
                for (var index = 0; index < values.Count; index++)
                {
                    var family = values[index];
                    var virtualIndex = cycle * values.Count + index;
                    var icon = CreateShapeIcon(
                        $"Custom Shape Family Reel Icon {cycle + 1}-{family}",
                        familyReelContent,
                        FamilyShapeToken(family),
                        new Vector2(42f, -FamilyReelTopPadding - virtualIndex * FamilySlotSpacing),
                        new Vector2(70f, 42f),
                        UiAnchor.TopLeft,
                        FamilyColor(family),
                        4.1f);
                    icon.raycastTarget = false;
                    familyReelIcons.Add(icon);
                    familyReelFamilies.Add(family);
                }
            }

            var centerLine = CreatePanel("Custom Shape Family Reel Center Line", familyReelViewport, Vector2.zero, new Vector2(FamilyReelViewportSize.x - 18f, 2f), UiAnchor.Center, new Color(0.55f, 0.75f, 0.78f, 0.18f));
            centerLine.GetComponent<Image>().raycastTarget = false;
            centerLine.SetAsFirstSibling();
            familyReelViewport.Find($"{familyReelViewport.name} Border")?.SetAsLastSibling();
            ResetFamilyReelPosition();
        }

        private void SaveEditor()
        {
            if (!capturePad.HasPlacedShapes)
            {
                editorStatus.text = "도형을 드래그해서 1개 이상 배치하세요.";
                return;
            }

            var strokes = capturePad.CaptureStrokes();
            var regexPattern = CustomShapeProfileStore.BuildGeneratedRegex(labelInput.text, editorShapeToken);
            if (store.TrySaveSlot(selectedSlotIndex, labelInput.text, regexPattern, editorShapeToken, capturePad.CaptureShapeTokens(), editorFamily, strokes, out var message))
            {
                CloseEditor();
                RefreshSlots();
                return;
            }

            editorStatus.text = message;
        }

        private void CycleFamily(int direction)
        {
            if (direction == 0)
            {
                return;
            }

            direction = Math.Sign(direction);
            var values = FamilyValues();
            familyReelVirtualIndex += direction;
            editorFamily = values[PositiveModulo(familyReelVirtualIndex, values.Count)];
            familyReelTarget = FamilyReelTargetForIndex(familyReelVirtualIndex);
            familyReelVelocity = Mathf.Clamp(familyReelVelocity + direction * FamilyReelKickVelocity, -FamilyReelMaxVelocity, FamilyReelMaxVelocity);
            UpdateFamilyLabel();
        }

        private void UpdateFamilyLabel()
        {
            if (familyLabel != null)
            {
                familyLabel.text = $"매핑: {SpellLabels.Korean(editorFamily)}";
            }

            if (familyReelContent == null)
            {
                return;
            }

            UpdateFamilyReelIconSprites();
            ApplyFamilyReelVisuals();
            capturePad?.SetStrokeColor(FamilyColor(editorFamily));
        }

        private void UpdateFamilyCarouselAnimation(float deltaTime)
        {
            if (familyReelContent == null)
            {
                return;
            }

            if (Mathf.Abs(familyReelPosition - familyReelTarget) <= 0.05f && Mathf.Abs(familyReelVelocity) <= 0.05f)
            {
                familyReelPosition = familyReelTarget;
                familyReelVelocity = 0f;
                NormalizeFamilyReelIndex();
                ApplyFamilyReelVisuals();
                return;
            }

            var dt = Mathf.Clamp(deltaTime, 1f / 60f, 0.05f);
            var acceleration = (familyReelTarget - familyReelPosition) * FamilyReelSpring - familyReelVelocity * FamilyReelDamping;
            familyReelVelocity = Mathf.Clamp(familyReelVelocity + acceleration * dt, -FamilyReelMaxVelocity, FamilyReelMaxVelocity);
            familyReelPosition += familyReelVelocity * dt;
            ApplyFamilyReelVisuals();
        }

        private void ResetFamilyReelPosition()
        {
            var values = FamilyValues();
            var baseIndex = values.IndexOf(editorFamily);
            familyReelVirtualIndex = values.Count + Mathf.Max(0, baseIndex);
            familyReelPosition = FamilyReelTargetForIndex(familyReelVirtualIndex);
            familyReelTarget = familyReelPosition;
            familyReelVelocity = 0f;
            UpdateFamilyReelIconSprites();
            ApplyFamilyReelVisuals();
        }

        private void NormalizeFamilyReelIndex()
        {
            var values = FamilyValues();
            var normalized = values.Count + PositiveModulo(familyReelVirtualIndex, values.Count);
            if (normalized == familyReelVirtualIndex)
            {
                return;
            }

            familyReelVirtualIndex = normalized;
            familyReelTarget = FamilyReelTargetForIndex(familyReelVirtualIndex);
            familyReelPosition = familyReelTarget;
        }

        private void UpdateFamilyReelIconSprites()
        {
            for (var index = 0; index < familyReelIcons.Count; index++)
            {
                var family = familyReelFamilies[index];
                SetShapeIcon(familyReelIcons[index], FamilyShapeToken(family), FamilyColor(family));
            }
        }

        private void ApplyFamilyReelVisuals()
        {
            if (familyReelContent == null)
            {
                return;
            }

            familyReelContent.anchoredPosition = new Vector2(0f, familyReelPosition);
            var centerY = -FamilyReelViewportSize.y * 0.5f;
            for (var index = 0; index < familyReelIcons.Count; index++)
            {
                var icon = familyReelIcons[index];
                var rect = icon.rectTransform;
                var visualY = familyReelPosition + rect.anchoredPosition.y;
                var distanceFromCenter = Mathf.Abs(visualY - centerY);
                var centerWeight = 1f - Mathf.Clamp01(distanceFromCenter / FamilyReelFadeDistance);
                centerWeight = centerWeight * centerWeight * (3f - 2f * centerWeight);
                var alpha = Mathf.Lerp(0.30f, 1f, centerWeight);
                icon.color = WithAlpha(FamilyColor(familyReelFamilies[index]), alpha);
                rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.identity;
            }
        }

        private static List<SpellFamily> FamilyValues()
        {
            return Enum.GetValues(typeof(SpellFamily)).Cast<SpellFamily>().ToList();
        }

        private static int PositiveModulo(int value, int count)
        {
            return (value % count + count) % count;
        }

        private static float FamilyReelTargetForIndex(int virtualIndex)
        {
            return -FamilyReelViewportSize.y * 0.5f + FamilyReelTopPadding + virtualIndex * FamilySlotSpacing;
        }

        private void CloseEditor()
        {
            if (editorRoot != null)
            {
                UnityEngine.Object.Destroy(editorRoot.gameObject);
                editorRoot = null;
                capturePad = null!;
                captureTemplatePreview = null!;
                captureStrokePreview = null!;
                familyReelContent = null!;
                familyReelViewport = null!;
                familyReelIcons.Clear();
                familyReelFamilies.Clear();
                familyReelPosition = 0f;
                familyReelTarget = 0f;
                familyReelVelocity = 0f;
            }

            if (editorShadow != null)
            {
                UnityEngine.Object.Destroy(editorShadow.gameObject);
                editorShadow = null!;
            }

            if (editorScrim != null)
            {
                UnityEngine.Object.Destroy(editorScrim.gameObject);
                editorScrim = null!;
            }
        }

        private Vector2 GetEditorPopupSize()
        {
            var canvasSize = GetCanvasReferenceSize();
            var parentSize = pagePanel != null
                ? pagePanel.rect.size
                : new Vector2(960f, 540f);
            var horizontalScreenMargin = Mathf.Clamp(canvasSize.x * 0.026f, 20f, 42f);
            var verticalScreenMargin = Mathf.Clamp(canvasSize.y * 0.04f, 20f, 46f);
            var maxWidth = Mathf.Max(640f, Mathf.Min(parentSize.x - 36f, canvasSize.x - horizontalScreenMargin));
            var maxHeight = Mathf.Max(390f, Mathf.Min(parentSize.y - 22f, canvasSize.y - verticalScreenMargin));
            var canvasAspect = canvasSize.x / Mathf.Max(1f, canvasSize.y);
            var desired = canvasAspect < 1.55f
                ? new Vector2(880f, 520f)
                : BaseEditorPopupSize;
            var scale = Mathf.Min(maxWidth / desired.x, maxHeight / desired.y);
            scale = Mathf.Min(scale, 1.06f);
            return new Vector2(
                Mathf.Floor(desired.x * scale),
                Mathf.Floor(desired.y * scale));
        }

        private Vector2 GetCanvasReferenceSize()
        {
            var rect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
            var size = rect != null ? rect.rect.size : new Vector2(Screen.width, Screen.height);
            if (size.x < 1f || size.y < 1f)
            {
                size = new Vector2(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
            }

            return size;
        }

        private void ClosePage()
        {
            HideBubble();
            CloseEditor();
            if (modalRoot != null)
            {
                modalRoot.gameObject.SetActive(false);
            }
        }

        private void RefreshSlots()
        {
            if (slotButtons.Count == 0 || slotIcons.Count == 0 || store == null)
            {
                return;
            }

            for (var index = 0; index < slotButtons.Count; index++)
            {
                var slot = store.GetSlot(index);
                var label = slotButtons[index].GetComponentInChildren<Text>();
                var icon = slotIcons[index];
                if (slot.IsOccupied)
                {
                    slotButtons[index].image.color = FamilyColor(slot.mappedFamily);
                    label.text = "";
                    SetShapeIcon(
                        icon,
                        string.IsNullOrWhiteSpace(slot.shapeToken) ? "line" : slot.shapeToken,
                        Color.white,
                        slot.goldCaptures.FirstOrDefault()?.ToStrokeSamples(),
                        SavedSlotPreviewStrokeWidth);
                }
                else
                {
                    slotButtons[index].image.color = new Color(0.06f, 0.12f, 0.21f, 0.96f);
                    label.text = "";
                    ClearShapeIcon(icon);
                }
            }
        }

        private bool PointerIsOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private bool ScreenPointHitsPlayer(Vector2 screenPoint)
        {
            var playerScreen = RectTransformUtility.WorldToScreenPoint(mainCamera, player.position);
            return Vector2.Distance(screenPoint, playerScreen) <= 78f;
        }

        private static Color FamilyColor(SpellFamily family)
        {
            return family switch
            {
                SpellFamily.Wind => new Color(0.28f, 0.64f, 0.95f, 0.98f),
                SpellFamily.Earth => new Color(0.68f, 0.48f, 0.25f, 0.98f),
                SpellFamily.Fire => new Color(0.9f, 0.18f, 0.08f, 0.98f),
                SpellFamily.Water => new Color(0.12f, 0.36f, 0.9f, 0.98f),
                SpellFamily.Life => new Color(0.22f, 0.66f, 0.36f, 0.98f),
                _ => Color.gray
            };
        }

        private static string FamilyShapeToken(SpellFamily family)
        {
            return family switch
            {
                SpellFamily.Wind => "wave",
                SpellFamily.Earth => "diamond",
                SpellFamily.Fire => "triangle",
                SpellFamily.Water => "ellipse",
                SpellFamily.Life => "cross",
                _ => "line"
            };
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private static Sprite CircleSprite
        {
            get
            {
                if (circleSprite != null)
                {
                    return circleSprite;
                }

                const int size = 64;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };

                var center = (size - 1) * 0.5f;
                var radius = center - 1.5f;
                var pixels = new Color32[size * size];
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        var dx = x - center;
                        var dy = y - center;
                        pixels[y * size + x] = dx * dx + dy * dy <= radius * radius
                            ? Color.white
                            : new Color(1f, 1f, 1f, 0f);
                    }
                }

                texture.SetPixels32(pixels);
                texture.Apply();
                circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
                circleSprite.name = "Custom Shape UI Circle";
                return circleSprite;
            }
        }

        private Button CreateButton(string name, Transform parent, string content, int size, Vector2 anchoredPosition, Vector2 rectSize, UiAnchor anchor, Action onClick)
        {
            var image = CreateImage(name, parent, anchoredPosition, rectSize, anchor, new Color(0.1f, 0.15f, 0.22f, 0.98f));
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick?.Invoke());
            var label = CreateText($"{name} Text", image.transform, content, size, FontStyle.Bold, Vector2.zero, rectSize - new Vector2(8f, 8f), UiAnchor.Center);
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            return button;
        }

        private InputField CreateInputField(string name, Transform parent, string placeholder, Vector2 anchoredPosition, Vector2 rectSize, UiAnchor anchor)
        {
            var image = CreateImage(name, parent, anchoredPosition, rectSize, anchor, new Color(0.93f, 0.94f, 0.9f, 1f));
            var input = image.gameObject.AddComponent<InputField>();
            var text = CreateText($"{name} Text", image.transform, "", 15, FontStyle.Normal, new Vector2(10f, 0f), rectSize - new Vector2(18f, 8f), UiAnchor.LeftCenter);
            text.color = new Color(0.05f, 0.055f, 0.06f, 1f);
            text.alignment = TextAnchor.MiddleLeft;
            var holder = CreateText($"{name} Placeholder", image.transform, placeholder, 15, FontStyle.Italic, new Vector2(10f, 0f), rectSize - new Vector2(18f, 8f), UiAnchor.LeftCenter);
            holder.color = new Color(0.22f, 0.23f, 0.22f, 0.55f);
            holder.alignment = TextAnchor.MiddleLeft;
            input.textComponent = text;
            input.placeholder = holder;
            input.targetGraphic = image;
            input.caretColor = Color.black;
            input.selectionColor = new Color(0.2f, 0.42f, 0.95f, 0.36f);
            return input;
        }

        private Image CreateShapeIcon(string name, Transform parent, string shapeToken, Vector2 anchoredPosition, Vector2 size, UiAnchor anchor, Color color, float strokeWidth)
        {
            var icon = CreateImage(name, parent, anchoredPosition, size, anchor, color);
            icon.sprite = CustomShapeSpriteFactory.CreateShapeSprite(shapeToken, Mathf.Max(1, Mathf.RoundToInt(strokeWidth)));
            icon.preserveAspect = true;
            return icon;
        }

        private static void SetShapeIcon(Image icon, string shapeToken, Color color, IReadOnlyList<IReadOnlyList<StrokeSample>> strokes = null, int strokeWidth = 4)
        {
            if (icon == null)
            {
                return;
            }

            icon.sprite = strokes != null && strokes.Count > 0
                ? CustomShapeSpriteFactory.CreateStrokeSprite(strokes, strokeWidth)
                : CustomShapeSpriteFactory.CreateShapeSprite(shapeToken, strokeWidth);
            icon.enabled = true;
            icon.color = color;
            icon.preserveAspect = true;
        }

        private static void ClearShapeIcon(Image icon)
        {
            if (icon == null)
            {
                return;
            }

            icon.enabled = false;
            icon.sprite = null;
            icon.color = Color.clear;
        }

        private RectTransform CreateSpeechBubble(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, UiAnchor anchor, Color color)
        {
            var body = new GameObject(name);
            body.transform.SetParent(parent, false);
            var rect = body.AddComponent<RectTransform>();
            ApplyAnchor(rect, anchor);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            var tail = CreateImage($"{name} Tail", rect, new Vector2(0f, 42f), new Vector2(38f, 38f), UiAnchor.Center, color);
            tail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            tail.raycastTarget = false;
            var panel = CreateImage($"{name} Body", rect, new Vector2(0f, -10f), new Vector2(size.x, size.y - 20f), UiAnchor.Center, color);
            panel.raycastTarget = false;
            return rect;
        }

        private Image CreateCircleImage(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, UiAnchor anchor, Color color)
        {
            var image = CreateImage(name, parent, anchoredPosition, size, anchor, color);
            image.sprite = CircleSprite;
            image.preserveAspect = true;
            return image;
        }

        private RectTransform CreatePanel(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, UiAnchor anchor, Color color)
        {
            return CreateImage(name, parent, anchoredPosition, size, anchor, color).rectTransform;
        }

        private static void AddSimpleBorder(RectTransform target, Color color, float thickness)
        {
            if (target == null)
            {
                return;
            }

            var body = new GameObject($"{target.name} Border");
            body.transform.SetParent(target, false);
            var rect = body.AddComponent<RectTransform>();
            ApplyAnchor(rect, UiAnchor.Stretch);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var border = body.AddComponent<CustomShapeRectBorder>();
            border.color = color;
            border.thickness = thickness;
            border.material = PixelMaterialProvider.UiMaterial;
            border.raycastTarget = false;
            body.transform.SetAsLastSibling();
        }

        private Image CreateImage(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, UiAnchor anchor, Color color)
        {
            var body = new GameObject(name);
            body.transform.SetParent(parent, false);
            var rect = body.AddComponent<RectTransform>();
            ApplyAnchor(rect, anchor);
            rect.anchoredPosition = anchoredPosition;
            if (anchor == UiAnchor.Stretch)
            {
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            else
            {
                rect.sizeDelta = size;
            }

            var image = body.AddComponent<Image>();
            image.color = color;
            image.material = PixelMaterialProvider.UiMaterial;
            return image;
        }

        private Text CreateText(string name, Transform parent, string content, int size, FontStyle style, Vector2 anchoredPosition, Vector2 rectSize, UiAnchor anchor)
        {
            var body = new GameObject(name);
            body.transform.SetParent(parent, false);
            var rect = body.AddComponent<RectTransform>();
            ApplyAnchor(rect, anchor);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = rectSize;
            var text = body.AddComponent<Text>();
            text.font = uiFont;
            text.text = content;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = Color.white;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(9, size - 5);
            text.resizeTextMaxSize = size;
            return text;
        }

        private static void ApplyAnchor(RectTransform rect, UiAnchor anchor)
        {
            switch (anchor)
            {
                case UiAnchor.TopLeft:
                    rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0f, 1f);
                    break;
                case UiAnchor.TopRight:
                    rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
                    rect.pivot = new Vector2(1f, 1f);
                    break;
                case UiAnchor.TopCenter:
                    rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
                    rect.pivot = new Vector2(0.5f, 1f);
                    break;
                case UiAnchor.BottomLeft:
                    rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
                    rect.pivot = new Vector2(0f, 0f);
                    break;
                case UiAnchor.BottomRight:
                    rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
                    rect.pivot = new Vector2(1f, 0f);
                    break;
                case UiAnchor.BottomCenter:
                    rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
                    rect.pivot = new Vector2(0.5f, 0f);
                    break;
                case UiAnchor.RightCenter:
                    rect.anchorMin = rect.anchorMax = new Vector2(1f, 0.5f);
                    rect.pivot = new Vector2(1f, 0.5f);
                    break;
                case UiAnchor.LeftCenter:
                    rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
                    rect.pivot = new Vector2(0f, 0.5f);
                    break;
                case UiAnchor.Stretch:
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    break;
                default:
                    rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    break;
            }
        }

        private enum UiAnchor
        {
            Center,
            TopLeft,
            TopRight,
            TopCenter,
            BottomLeft,
            BottomRight,
            BottomCenter,
            LeftCenter,
            RightCenter,
            Stretch
        }
    }

    public sealed class CustomShapeRectBorder : MaskableGraphic
    {
        public float thickness = 2f;
        public override Texture mainTexture => Texture2D.whiteTexture;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = rectTransform.rect;
            var line = Mathf.Max(1f, thickness);
            AddRect(vh, new Rect(rect.xMin, rect.yMax - line, rect.width, line), color);
            AddRect(vh, new Rect(rect.xMin, rect.yMin, rect.width, line), color);
            AddRect(vh, new Rect(rect.xMin, rect.yMin, line, rect.height), color);
            AddRect(vh, new Rect(rect.xMax - line, rect.yMin, line, rect.height), color);
        }

        private static void AddRect(VertexHelper vh, Rect rect, Color color)
        {
            var vertex = vh.currentVertCount;
            vh.AddVert(new Vector3(rect.xMin, rect.yMin), color, Vector2.zero);
            vh.AddVert(new Vector3(rect.xMin, rect.yMax), color, Vector2.zero);
            vh.AddVert(new Vector3(rect.xMax, rect.yMax), color, Vector2.zero);
            vh.AddVert(new Vector3(rect.xMax, rect.yMin), color, Vector2.zero);
            vh.AddTriangle(vertex, vertex + 1, vertex + 2);
            vh.AddTriangle(vertex, vertex + 2, vertex + 3);
        }
    }

    internal static class CustomShapeSpriteFactory
    {
        private const int Size = 96;
        private static readonly Dictionary<string, Sprite> ShapeSprites = new();

        public static Sprite CreateShapeSprite(string token, int strokeWidth = 4)
        {
            token = string.IsNullOrWhiteSpace(token) ? "line" : token;
            strokeWidth = Mathf.Clamp(strokeWidth, 2, 10);
            var key = $"{token}:{strokeWidth}";
            if (ShapeSprites.TryGetValue(key, out var sprite) && sprite != null)
            {
                return sprite;
            }

            sprite = CreateSprite(CustomShapeUiDrawing.NormalizedStrokes(token), strokeWidth, $"CustomShape_{key}");
            ShapeSprites[key] = sprite;
            return sprite;
        }

        public static Sprite CreateStrokeSprite(IReadOnlyList<IReadOnlyList<StrokeSample>> strokes, int strokeWidth = 5)
        {
            strokeWidth = Mathf.Clamp(strokeWidth, 2, 10);
            var normalized = NormalizeStrokes(strokes);
            return CreateSprite(normalized, strokeWidth, "CustomShape_UserStroke");
        }

        private static Sprite CreateSprite(IEnumerable<IReadOnlyList<Vector2>> strokes, int strokeWidth, string name)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = name
            };
            var clear = Enumerable.Repeat(new Color32(0, 0, 0, 0), Size * Size).ToArray();
            texture.SetPixels32(clear);

            foreach (var stroke in strokes)
            {
                for (var index = 1; index < stroke.Count; index++)
                {
                    DrawLine(texture, stroke[index - 1], stroke[index], strokeWidth);
                }
            }

            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), Size);
            sprite.name = name;
            return sprite;
        }

        private static List<List<Vector2>> NormalizeStrokes(IReadOnlyList<IReadOnlyList<StrokeSample>> strokes)
        {
            var points = strokes?.SelectMany(stroke => stroke.Select(sample => sample.position)).ToList() ?? new List<Vector2>();
            if (points.Count == 0)
            {
                return CustomShapeUiDrawing.NormalizedStrokes("line");
            }

            var min = new Vector2(points.Min(point => point.x), points.Min(point => point.y));
            var max = new Vector2(points.Max(point => point.x), points.Max(point => point.y));
            var center = (min + max) * 0.5f;
            var span = new Vector2(Mathf.Max(max.x - min.x, 0.001f), Mathf.Max(max.y - min.y, 0.001f));
            var scale = 0.76f / Mathf.Max(span.x, span.y);
            return strokes!
                .Select(stroke => stroke
                    .Select(sample => new Vector2(
                        Mathf.Clamp01(0.5f + (sample.position.x - center.x) * scale),
                        Mathf.Clamp01(0.5f + (sample.position.y - center.y) * scale)))
                    .ToList())
                .ToList();
        }

        private static void DrawLine(Texture2D texture, Vector2 start, Vector2 end, int radius)
        {
            var startPixel = ToPixel(start);
            var endPixel = ToPixel(end);
            var distance = Vector2.Distance(startPixel, endPixel);
            var steps = Mathf.Max(1, Mathf.CeilToInt(distance));
            for (var step = 0; step <= steps; step++)
            {
                var point = Vector2.Lerp(startPixel, endPixel, step / (float)steps);
                DrawBrush(texture, Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y), radius);
            }
        }

        private static Vector2 ToPixel(Vector2 normalized)
        {
            return new Vector2(
                Mathf.Clamp01(normalized.x) * (Size - 1),
                Mathf.Clamp01(normalized.y) * (Size - 1));
        }

        private static void DrawBrush(Texture2D texture, int centerX, int centerY, int radius)
        {
            var squared = radius * radius;
            for (var y = -radius; y <= radius; y++)
            {
                for (var x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y > squared)
                    {
                        continue;
                    }

                    var px = centerX + x;
                    var py = centerY + y;
                    if (px < 0 || py < 0 || px >= Size || py >= Size)
                    {
                        continue;
                    }

                    texture.SetPixel(px, py, Color.white);
                }
            }
        }
    }

    internal static class CustomShapeUiDrawing
    {
        public static List<List<StrokeSample>> TemplateStrokes(string token, Rect rect)
        {
            var inset = Mathf.Min(rect.width, rect.height) * 0.18f;
            var content = Rect.MinMaxRect(rect.xMin + inset, rect.yMin + inset, rect.xMax - inset, rect.yMax - inset);
            return NormalizedStrokes(token).Select(stroke => stroke.Select(point => new StrokeSample(Map(point, content), 0f)).ToList()).ToList();
        }

        public static void AddShape(VertexHelper vh, Rect rect, string token, Color color, float width)
        {
            foreach (var stroke in TemplateStrokes(token, rect))
            {
                AddStroke(vh, stroke.Select(sample => sample.position).ToList(), color, width);
            }
        }

        public static void AddStrokes(VertexHelper vh, Rect rect, IReadOnlyList<IReadOnlyList<StrokeSample>> strokes, Color color, float width)
        {
            var points = strokes.SelectMany(stroke => stroke.Select(sample => sample.position)).ToList();
            if (points.Count == 0)
            {
                return;
            }

            var min = new Vector2(points.Min(point => point.x), points.Min(point => point.y));
            var max = new Vector2(points.Max(point => point.x), points.Max(point => point.y));
            var span = new Vector2(Mathf.Max(max.x - min.x, 0.001f), Mathf.Max(max.y - min.y, 0.001f));
            var scale = Mathf.Min(rect.width / span.x, rect.height / span.y) * 0.68f;
            var center = (min + max) * 0.5f;
            foreach (var stroke in strokes)
            {
                var mapped = stroke
                    .Select(sample => (sample.position - center) * scale)
                    .ToList();
                AddStroke(vh, mapped, color, width);
            }
        }

        public static void AddSegment(VertexHelper vh, Vector2 start, Vector2 end, Color color, float width)
        {
            var delta = end - start;
            if (delta.sqrMagnitude < 0.01f)
            {
                return;
            }

            var normal = new Vector2(-delta.y, delta.x).normalized * (width * 0.5f);
            var index = vh.currentVertCount;
            vh.AddVert(start - normal, color, Vector2.zero);
            vh.AddVert(start + normal, color, Vector2.zero);
            vh.AddVert(end + normal, color, Vector2.zero);
            vh.AddVert(end - normal, color, Vector2.zero);
            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index, index + 2, index + 3);
        }

        private static void AddStroke(VertexHelper vh, IReadOnlyList<Vector2> points, Color color, float width)
        {
            for (var index = 1; index < points.Count; index++)
            {
                AddSegment(vh, points[index - 1], points[index], color, width);
            }
        }

        private static Vector2 Map(Vector2 point, Rect rect)
        {
            return new Vector2(Mathf.Lerp(rect.xMin, rect.xMax, point.x), Mathf.Lerp(rect.yMin, rect.yMax, point.y));
        }

        public static List<List<Vector2>> NormalizedStrokes(string token)
        {
            token = string.IsNullOrWhiteSpace(token) ? "line" : token;
            return token switch
            {
                "line" => One(new[] { V(0.12f, 0.5f), V(0.88f, 0.5f) }),
                "arrow" => new List<List<Vector2>>
                {
                    new() { V(0.12f, 0.5f), V(0.84f, 0.5f) },
                    new() { V(0.62f, 0.72f), V(0.84f, 0.5f), V(0.62f, 0.28f) }
                },
                "rect" => Closed(V(0.18f, 0.2f), V(0.82f, 0.2f), V(0.82f, 0.8f), V(0.18f, 0.8f)),
                "roundRect" => RoundedRect(),
                "ellipse" => Ellipse(0.5f, 0.5f, 0.34f, 0.28f, 0f, Mathf.PI * 2f, 40),
                "triangle" => Closed(V(0.5f, 0.84f), V(0.16f, 0.2f), V(0.84f, 0.2f)),
                "diamond" => Closed(V(0.5f, 0.88f), V(0.86f, 0.5f), V(0.5f, 0.12f), V(0.14f, 0.5f)),
                "pentagon" => Polygon(5, -Mathf.PI * 0.5f),
                "hexagon" => Polygon(6, Mathf.PI / 6f),
                "star" => Star(),
                "arc" => Ellipse(0.5f, 0.44f, 0.36f, 0.30f, Mathf.PI * 0.12f, Mathf.PI * 0.88f, 22),
                "curve" => One(Bezier(V(0.14f, 0.28f), V(0.34f, 0.86f), V(0.66f, 0.14f), V(0.86f, 0.72f), 28)),
                "wave" => One(Enumerable.Range(0, 32).Select(i =>
                {
                    var t = i / 31f;
                    return V(0.10f + t * 0.80f, 0.5f + Mathf.Sin(t * Mathf.PI * 4f) * 0.22f);
                })),
                "brace" => One(new[] { V(0.66f, 0.88f), V(0.42f, 0.76f), V(0.48f, 0.58f), V(0.30f, 0.5f), V(0.48f, 0.42f), V(0.42f, 0.24f), V(0.66f, 0.12f) }),
                "cross" => new List<List<Vector2>>
                {
                    new() { V(0.22f, 0.22f), V(0.78f, 0.78f) },
                    new() { V(0.78f, 0.22f), V(0.22f, 0.78f) }
                },
                _ => One(new[] { V(0.12f, 0.5f), V(0.88f, 0.5f) })
            };
        }

        private static List<List<Vector2>> RoundedRect()
        {
            var points = new List<Vector2>();
            points.AddRange(ArcPoints(0.28f, 0.72f, 0.12f, Mathf.PI, Mathf.PI * 1.5f, 6));
            points.AddRange(ArcPoints(0.72f, 0.72f, 0.12f, Mathf.PI * 1.5f, Mathf.PI * 2f, 6));
            points.AddRange(ArcPoints(0.72f, 0.28f, 0.12f, 0f, Mathf.PI * 0.5f, 6));
            points.AddRange(ArcPoints(0.28f, 0.28f, 0.12f, Mathf.PI * 0.5f, Mathf.PI, 6));
            points.Add(points[0]);
            return One(points);
        }

        private static List<List<Vector2>> Polygon(int sides, float offset)
        {
            var points = Enumerable.Range(0, sides)
                .Select(i =>
                {
                    var angle = offset + Mathf.PI * 2f * i / sides;
                    return V(0.5f + Mathf.Cos(angle) * 0.36f, 0.5f + Mathf.Sin(angle) * 0.36f);
                })
                .ToList();
            points.Add(points[0]);
            return One(points);
        }

        private static List<List<Vector2>> Star()
        {
            var points = Enumerable.Range(0, 10)
                .Select(i =>
                {
                    var radius = i % 2 == 0 ? 0.38f : 0.17f;
                    var angle = -Mathf.PI * 0.5f + Mathf.PI * 2f * i / 10f;
                    return V(0.5f + Mathf.Cos(angle) * radius, 0.5f + Mathf.Sin(angle) * radius);
                })
                .ToList();
            points.Add(points[0]);
            return One(points);
        }

        private static List<List<Vector2>> Ellipse(float cx, float cy, float rx, float ry, float start, float end, int count)
        {
            return One(Enumerable.Range(0, count).Select(i =>
            {
                var t = count <= 1 ? 0f : i / (count - 1f);
                var angle = Mathf.Lerp(start, end, t);
                return V(cx + Mathf.Cos(angle) * rx, cy + Mathf.Sin(angle) * ry);
            }));
        }

        private static IEnumerable<Vector2> ArcPoints(float cx, float cy, float r, float start, float end, int count)
        {
            return Enumerable.Range(0, count).Select(i =>
            {
                var t = count <= 1 ? 0f : i / (count - 1f);
                var angle = Mathf.Lerp(start, end, t);
                return V(cx + Mathf.Cos(angle) * r, cy + Mathf.Sin(angle) * r);
            });
        }

        private static IEnumerable<Vector2> Bezier(Vector2 a, Vector2 b, Vector2 c, Vector2 d, int count)
        {
            return Enumerable.Range(0, count).Select(i =>
            {
                var t = count <= 1 ? 0f : i / (count - 1f);
                var u = 1f - t;
                return a * (u * u * u) + b * (3f * u * u * t) + c * (3f * u * t * t) + d * (t * t * t);
            });
        }

        private static List<List<Vector2>> Closed(params Vector2[] points)
        {
            var list = points.ToList();
            list.Add(points[0]);
            return One(list);
        }

        private static List<List<Vector2>> One(IEnumerable<Vector2> points)
        {
            return new List<List<Vector2>> { points.ToList() };
        }

        private static Vector2 V(float x, float y)
        {
            return new Vector2(x, y);
        }
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CustomShapeCapturePad : MaskableGraphic, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private const float MinimumDragSize = 18f;
        private const float LineShapeHeight = 34f;
        private const float HandleSize = 10f;
        private const float HandleHitRadius = 16f;
        private const float RotateHandleOffset = 34f;
        private const float RotationSnapRadians = 0.2617994f;
        private readonly List<PlacedShape> placedShapes = new();
        private string templateToken = "line";
        private Vector2 dragStart;
        private PlacedShape previewShape;
        private PadInteractionMode interactionMode = PadInteractionMode.None;
        private int selectedShapeIndex = -1;
        private int activeResizeHandle = -1;
        private Vector2 editPointerStart;
        private Vector2 editShapeStartCenter;
        private Vector2 editShapeStartSize;
        private Vector2 resizeAnchorLocal;
        private float editShapeStartRotation;
        private float editPointerStartAngle;
        public Color strokeColor = new(0.92f, 0.1f, 0.08f, 1f);
        public float strokeWidth = 7f;
        public Action<IReadOnlyList<IReadOnlyList<StrokeSample>>> onStrokesChanged;
        public override Texture mainTexture => Texture2D.whiteTexture;
        public bool HasPlacedShapes => placedShapes.Count > 0;
        public int PlacedShapeCount => placedShapes.Count;
        public int SelectedShapeIndexForTests => selectedShapeIndex;
        public Vector2 SelectedShapeCenterForTests => HasSelectedShape ? placedShapes[selectedShapeIndex].center : Vector2.zero;
        public Vector2 SelectedShapeSizeForTests => HasSelectedShape ? placedShapes[selectedShapeIndex].size : Vector2.zero;
        public float SelectedShapeRotationDegreesForTests => HasSelectedShape ? placedShapes[selectedShapeIndex].rotation * Mathf.Rad2Deg : 0f;
        private bool HasSelectedShape => selectedShapeIndex >= 0 && selectedShapeIndex < placedShapes.Count;

        public IReadOnlyList<IReadOnlyList<StrokeSample>> CaptureStrokes()
        {
            return CapturePlacedShapeStrokes();
        }

        public IReadOnlyList<string> CaptureShapeTokens()
        {
            return placedShapes
                .Where(shape => shape.IsLargeEnough)
                .Select(shape => shape.token)
                .DefaultIfEmpty(templateToken)
                .ToList();
        }

        public void SetTemplate(string shapeToken)
        {
            templateToken = string.IsNullOrWhiteSpace(shapeToken) ? "line" : shapeToken;
            SetVerticesDirty();
        }

        public void SetStrokeColor(Color color)
        {
            strokeColor = color;
            SetVerticesDirty();
        }

        public void Clear()
        {
            placedShapes.Clear();
            previewShape = null;
            selectedShapeIndex = -1;
            interactionMode = PadInteractionMode.None;
            NotifyChanged();
            SetVerticesDirty();
        }

        public bool UndoLastShape()
        {
            if (placedShapes.Count == 0)
            {
                return false;
            }

            placedShapes.RemoveAt(placedShapes.Count - 1);
            if (selectedShapeIndex >= placedShapes.Count)
            {
                selectedShapeIndex = placedShapes.Count - 1;
            }

            NotifyChanged();
            SetVerticesDirty();
            return true;
        }

        public bool TryGetSelectedResizeHandleLocalForTests(int handleIndex, out Vector2 local)
        {
            local = Vector2.zero;
            if (!HasSelectedShape || handleIndex < 0 || handleIndex > 3)
            {
                return false;
            }

            local = ResizeHandlePosition(placedShapes[selectedShapeIndex], handleIndex);
            return true;
        }

        public bool TryGetSelectedRotateHandleLocalForTests(out Vector2 local)
        {
            local = Vector2.zero;
            if (!HasSelectedShape || !SupportsRotation(placedShapes[selectedShapeIndex].token))
            {
                return false;
            }

            local = RotateHandlePosition(placedShapes[selectedShapeIndex]);
            return true;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (!TryLocalPoint(eventData, out dragStart))
            {
                return;
            }

            if (TryBeginRotate(dragStart) || TryBeginResize(dragStart) || TryBeginMove(dragStart))
            {
                SetVerticesDirty();
                return;
            }

            interactionMode = PadInteractionMode.Creating;
            selectedShapeIndex = -1;
            previewShape = CreateShapeFromDrag(templateToken, dragStart, dragStart, true);
            SetVerticesDirty();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (interactionMode == PadInteractionMode.None || !TryLocalPoint(eventData, out var local))
            {
                return;
            }

            switch (interactionMode)
            {
                case PadInteractionMode.Creating:
                    previewShape = CreateShapeFromDrag(templateToken, dragStart, local, true);
                    break;
                case PadInteractionMode.Moving:
                    UpdateMove(local);
                    break;
                case PadInteractionMode.Resizing:
                    UpdateResize(local);
                    break;
                case PadInteractionMode.Rotating:
                    UpdateRotate(local);
                    break;
            }

            SetVerticesDirty();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (interactionMode == PadInteractionMode.None)
            {
                return;
            }

            var finishedMode = interactionMode;
            interactionMode = PadInteractionMode.None;
            if (finishedMode == PadInteractionMode.Creating && TryLocalPoint(eventData, out var local))
            {
                var shape = CreateShapeFromDrag(templateToken, dragStart, local, false);
                if (shape != null && shape.IsLargeEnough)
                {
                    placedShapes.Add(shape);
                    selectedShapeIndex = placedShapes.Count - 1;
                    NotifyChanged();
                }
            }
            else if (finishedMode is PadInteractionMode.Moving or PadInteractionMode.Resizing or PadInteractionMode.Rotating)
            {
                NotifyChanged();
            }

            previewShape = null;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (placedShapes.Count == 0 && previewShape == null)
            {
                CustomShapeUiDrawing.AddShape(vh, rectTransform.rect, templateToken, new Color(0.18f, 0.19f, 0.17f, 0.24f), 5f);
                return;
            }

            foreach (var shape in placedShapes)
            {
                AddShape(vh, shape, strokeColor, strokeWidth);
            }

            if (previewShape != null)
            {
                AddShape(vh, previewShape, new Color(strokeColor.r, strokeColor.g, strokeColor.b, 0.52f), strokeWidth);
            }

            if (previewShape == null && HasSelectedShape)
            {
                AddSelectionFrame(vh, placedShapes[selectedShapeIndex]);
            }
        }

        private IReadOnlyList<IReadOnlyList<StrokeSample>> CapturePlacedShapeStrokes()
        {
            return placedShapes
                .SelectMany(shape => StrokesForShape(shape))
                .Select(stroke => (IReadOnlyList<StrokeSample>)stroke)
                .ToList();
        }

        private void NotifyChanged()
        {
            onStrokesChanged?.Invoke(CapturePlacedShapeStrokes());
        }

        private bool TryLocalPoint(PointerEventData eventData, out Vector2 local)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out local))
            {
                return false;
            }

            var rect = rectTransform.rect;
            local.x = Mathf.Clamp(local.x, rect.xMin, rect.xMax);
            local.y = Mathf.Clamp(local.y, rect.yMin, rect.yMax);
            return true;
        }

        private bool TryBeginMove(Vector2 local)
        {
            var hitIndex = HitShapeIndex(local);
            if (hitIndex < 0)
            {
                return false;
            }

            selectedShapeIndex = hitIndex;
            var shape = placedShapes[selectedShapeIndex];
            interactionMode = PadInteractionMode.Moving;
            editPointerStart = local;
            editShapeStartCenter = shape.center;
            return true;
        }

        private bool TryBeginResize(Vector2 local)
        {
            if (!HasSelectedShape)
            {
                return false;
            }

            var shape = placedShapes[selectedShapeIndex];
            for (var handle = 0; handle < 4; handle++)
            {
                if (Vector2.Distance(local, ResizeHandlePosition(shape, handle)) > HandleHitRadius)
                {
                    continue;
                }

                activeResizeHandle = handle;
                interactionMode = PadInteractionMode.Resizing;
                editShapeStartCenter = shape.center;
                editShapeStartSize = shape.size;
                editShapeStartRotation = shape.rotation;
                resizeAnchorLocal = LocalCorner(editShapeStartSize, OppositeHandle(handle));
                return true;
            }

            return false;
        }

        private bool TryBeginRotate(Vector2 local)
        {
            if (!HasSelectedShape)
            {
                return false;
            }

            var shape = placedShapes[selectedShapeIndex];
            if (!SupportsRotation(shape.token) || Vector2.Distance(local, RotateHandlePosition(shape)) > HandleHitRadius)
            {
                return false;
            }

            interactionMode = PadInteractionMode.Rotating;
            editShapeStartCenter = shape.center;
            editShapeStartRotation = shape.rotation;
            editPointerStartAngle = Mathf.Atan2(local.y - shape.center.y, local.x - shape.center.x);
            return true;
        }

        private void UpdateMove(Vector2 local)
        {
            if (!HasSelectedShape)
            {
                return;
            }

            var shape = placedShapes[selectedShapeIndex];
            shape.center = ClampCenter(editShapeStartCenter + (local - editPointerStart), shape.size);
        }

        private void UpdateResize(Vector2 local)
        {
            if (!HasSelectedShape)
            {
                return;
            }

            var shape = placedShapes[selectedShapeIndex];
            var currentLocal = ToShapeLocal(editShapeStartCenter, editShapeStartRotation, local);
            var delta = currentLocal - resizeAnchorLocal;
            var signX = delta.x >= 0f ? 1f : -1f;
            var signY = delta.y >= 0f ? 1f : -1f;
            currentLocal.x = resizeAnchorLocal.x + signX * Mathf.Max(Mathf.Abs(delta.x), MinimumDragSize);
            currentLocal.y = resizeAnchorLocal.y + signY * Mathf.Max(Mathf.Abs(delta.y), MinimumDragSize);

            var centerLocal = (resizeAnchorLocal + currentLocal) * 0.5f;
            shape.center = ClampCenter(editShapeStartCenter + Rotate(centerLocal, editShapeStartRotation), new Vector2(Mathf.Abs(currentLocal.x - resizeAnchorLocal.x), Mathf.Abs(currentLocal.y - resizeAnchorLocal.y)));
            shape.size = new Vector2(Mathf.Abs(currentLocal.x - resizeAnchorLocal.x), Mathf.Abs(currentLocal.y - resizeAnchorLocal.y));
            shape.rotation = editShapeStartRotation;
        }

        private void UpdateRotate(Vector2 local)
        {
            if (!HasSelectedShape)
            {
                return;
            }

            var shape = placedShapes[selectedShapeIndex];
            var currentAngle = Mathf.Atan2(local.y - shape.center.y, local.x - shape.center.x);
            var rawRotation = editShapeStartRotation + Mathf.DeltaAngle(editPointerStartAngle * Mathf.Rad2Deg, currentAngle * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            shape.rotation = Mathf.Round(rawRotation / RotationSnapRadians) * RotationSnapRadians;
        }

        private Vector2 ClampCenter(Vector2 center, Vector2 size)
        {
            var rect = rectTransform.rect;
            var margin = Mathf.Max(HandleHitRadius, Mathf.Min(size.x, size.y) * 0.12f);
            return new Vector2(
                Mathf.Clamp(center.x, rect.xMin + margin, rect.xMax - margin),
                Mathf.Clamp(center.y, rect.yMin + margin, rect.yMax - margin));
        }

        private int HitShapeIndex(Vector2 local)
        {
            for (var index = placedShapes.Count - 1; index >= 0; index--)
            {
                if (PointHitsShape(placedShapes[index], local))
                {
                    return index;
                }
            }

            return -1;
        }

        private static PlacedShape CreateShapeFromDrag(string token, Vector2 start, Vector2 end, bool preview)
        {
            var delta = end - start;
            token = string.IsNullOrWhiteSpace(token) ? "line" : token;
            if (token is "line" or "arrow")
            {
                var length = Mathf.Max(MinimumDragSize, delta.magnitude);
                var center = start + delta * 0.5f;
                var rotation = delta.sqrMagnitude <= 0.001f ? 0f : Mathf.Atan2(delta.y, delta.x);
                return new PlacedShape(token, center, new Vector2(length, LineShapeHeight), rotation, delta.magnitude >= MinimumDragSize, preview);
            }

            var min = Vector2.Min(start, end);
            var max = Vector2.Max(start, end);
            var size = new Vector2(Mathf.Max(max.x - min.x, MinimumDragSize), Mathf.Max(max.y - min.y, MinimumDragSize));
            var dragCenter = (min + max) * 0.5f;
            var valid = Mathf.Abs(delta.x) >= MinimumDragSize && Mathf.Abs(delta.y) >= MinimumDragSize;
            return new PlacedShape(token, dragCenter, size, 0f, valid, preview);
        }

        private static bool PointHitsShape(PlacedShape shape, Vector2 point)
        {
            var local = ToShapeLocal(shape.center, shape.rotation, point);
            return Mathf.Abs(local.x) <= shape.size.x * 0.5f + HandleHitRadius
                && Mathf.Abs(local.y) <= shape.size.y * 0.5f + HandleHitRadius;
        }

        private static Vector2 ToShapeLocal(Vector2 center, float rotation, Vector2 point)
        {
            return Rotate(point - center, -rotation);
        }

        private static int OppositeHandle(int handle)
        {
            return (handle + 2) % 4;
        }

        private static Vector2 ResizeHandlePosition(PlacedShape shape, int handle)
        {
            return shape.center + Rotate(LocalCorner(shape.size, handle), shape.rotation);
        }

        private static Vector2 RotateHandlePosition(PlacedShape shape)
        {
            return shape.center + Rotate(new Vector2(0f, shape.size.y * 0.5f + RotateHandleOffset), shape.rotation);
        }

        private static Vector2 LocalCorner(Vector2 size, int handle)
        {
            var half = size * 0.5f;
            return handle switch
            {
                0 => new Vector2(-half.x, half.y),
                1 => new Vector2(half.x, half.y),
                2 => new Vector2(half.x, -half.y),
                _ => new Vector2(-half.x, -half.y)
            };
        }

        private static Vector2 Rotate(Vector2 point, float rotation)
        {
            var sin = Mathf.Sin(rotation);
            var cos = Mathf.Cos(rotation);
            return new Vector2(point.x * cos - point.y * sin, point.x * sin + point.y * cos);
        }

        private static bool SupportsRotation(string token)
        {
            return token is "line" or "arrow" or "rect" or "roundRect" or "triangle" or "diamond" or "pentagon" or "hexagon" or "star" or "arc" or "curve" or "wave" or "brace" or "cross";
        }

        private static void AddSelectionFrame(VertexHelper vh, PlacedShape shape)
        {
            var frame = new Color(0.94f, 0.96f, 1f, 0.9f);
            var handleFill = new Color(0.96f, 0.97f, 1f, 1f);
            var handleStroke = new Color(0.22f, 0.28f, 0.38f, 1f);
            var corners = Enumerable.Range(0, 4).Select(index => ResizeHandlePosition(shape, index)).ToArray();
            for (var index = 0; index < 4; index++)
            {
                CustomShapeUiDrawing.AddSegment(vh, corners[index], corners[(index + 1) % 4], frame, 1.6f);
                AddHandleBox(vh, corners[index], HandleSize, handleFill);
                AddHandleBox(vh, corners[index], HandleSize + 2f, handleStroke, true);
            }

            if (!SupportsRotation(shape.token))
            {
                return;
            }

            var topMid = (corners[0] + corners[1]) * 0.5f;
            var rotate = RotateHandlePosition(shape);
            CustomShapeUiDrawing.AddSegment(vh, topMid, rotate, frame, 1.4f);
            AddHandleBox(vh, rotate, HandleSize + 2f, new Color(0.96f, 0.97f, 1f, 1f));
            CustomShapeUiDrawing.AddSegment(vh, rotate + new Vector2(-4f, 0f), rotate + new Vector2(4f, 0f), handleStroke, 1.4f);
            CustomShapeUiDrawing.AddSegment(vh, rotate + new Vector2(0f, -4f), rotate + new Vector2(0f, 4f), handleStroke, 1.4f);
        }

        private static void AddHandleBox(VertexHelper vh, Vector2 center, float size, Color color, bool outlineOnly = false)
        {
            var half = size * 0.5f;
            var min = center - new Vector2(half, half);
            var max = center + new Vector2(half, half);
            if (outlineOnly)
            {
                CustomShapeUiDrawing.AddSegment(vh, new Vector2(min.x, min.y), new Vector2(max.x, min.y), color, 1.2f);
                CustomShapeUiDrawing.AddSegment(vh, new Vector2(max.x, min.y), new Vector2(max.x, max.y), color, 1.2f);
                CustomShapeUiDrawing.AddSegment(vh, new Vector2(max.x, max.y), new Vector2(min.x, max.y), color, 1.2f);
                CustomShapeUiDrawing.AddSegment(vh, new Vector2(min.x, max.y), new Vector2(min.x, min.y), color, 1.2f);
                return;
            }

            var vertex = vh.currentVertCount;
            vh.AddVert(new Vector3(min.x, min.y), color, Vector2.zero);
            vh.AddVert(new Vector3(min.x, max.y), color, Vector2.zero);
            vh.AddVert(new Vector3(max.x, max.y), color, Vector2.zero);
            vh.AddVert(new Vector3(max.x, min.y), color, Vector2.zero);
            vh.AddTriangle(vertex, vertex + 1, vertex + 2);
            vh.AddTriangle(vertex, vertex + 2, vertex + 3);
        }

        private static void AddShape(VertexHelper vh, PlacedShape shape, Color color, float width)
        {
            foreach (var stroke in StrokesForShape(shape))
            {
                for (var index = 1; index < stroke.Count; index++)
                {
                    CustomShapeUiDrawing.AddSegment(vh, stroke[index - 1].position, stroke[index].position, color, width);
                }
            }
        }

        private static List<List<StrokeSample>> StrokesForShape(PlacedShape shape)
        {
            var sin = Mathf.Sin(shape.rotation);
            var cos = Mathf.Cos(shape.rotation);
            return CustomShapeUiDrawing.NormalizedStrokes(shape.token)
                .Select(stroke => stroke.Select(point =>
                {
                    var local = new Vector2((point.x - 0.5f) * shape.size.x, (point.y - 0.5f) * shape.size.y);
                    var rotated = new Vector2(local.x * cos - local.y * sin, local.x * sin + local.y * cos);
                    return new StrokeSample(shape.center + rotated, 0f);
                }).ToList())
                .ToList();
        }

        private sealed class PlacedShape
        {
            public readonly string token;
            public Vector2 center;
            public Vector2 size;
            public float rotation;
            public readonly bool valid;
            public readonly bool preview;

            public PlacedShape(string token, Vector2 center, Vector2 size, float rotation, bool valid, bool preview)
            {
                this.token = token;
                this.center = center;
                this.size = size;
                this.rotation = rotation;
                this.valid = valid;
                this.preview = preview;
            }

            public bool IsLargeEnough => valid;
        }

        private enum PadInteractionMode
        {
            None,
            Creating,
            Moving,
            Resizing,
            Rotating
        }

    }
}
