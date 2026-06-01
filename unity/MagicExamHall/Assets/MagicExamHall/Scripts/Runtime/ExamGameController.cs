using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MagicExamHall
{
    public sealed class ExamGameController : MonoBehaviour
    {
        public const float GameplayCameraOrthographicSize = 5.55f;
        public const int FinalFloorPassingGoalCount = 5;
        public const float StandardFloorAdvanceDelaySeconds = 1.4f;
        public const float FinalFloorPassReportDelaySeconds = 4.8f;
        public const float FinalFloorCompleteReportDelaySeconds = 1.9f;
        public const float DefaultSealFallbackDelaySeconds = 1.35f;
        public const string BuildVersion = "Magic Exam Hall 0.6.0-dev";
        private const int ResultPanelCompactScreenWidth = 1100;
        private const float GoalIntentRadiusMultiplier = 1.35f;
        private const float GoalIntentRadiusPadding = 0.35f;
        private const int CustomReferenceFloorNumber = 2;
        private const float CustomReferenceShelfRadius = 1.85f;
        private static readonly Vector2 WestBookcasePosition = new(-7.25f, 1.1f);
        private static readonly IReadOnlyList<CustomShapeReferenceDefinition> CustomShapeReferences = new List<CustomShapeReferenceDefinition>
        {
            new(SpellFamily.Life, "생명 다리", "rect", new[] { "arrow", "rect" }, "화살표 방향으로 발판을 뻗어 낭떠러지를 잇습니다."),
            new(SpellFamily.Water, "얼음 결정", "hexagon", new[] { "hexagon" }, "육각 결정으로 물을 얼려 지나갈 수 있게 합니다."),
            new(SpellFamily.Earth, "대지 계단", "rect", new[] { "rect" }, "사각 구조물을 쌓아 경사를 오를 계단을 만듭니다."),
            new(SpellFamily.Wind, "바람 발판", "rect", new[] { "rect" }, "사각 발판을 바람으로 띄워 건너갈 길을 만듭니다."),
            new(SpellFamily.Fire, "전기 직선", "line", new[] { "line" }, "직선 경로로 전류 타격을 만듭니다."),
            new(SpellFamily.Water, "정화 원", "ellipse", new[] { "ellipse" }, "둥근 물막으로 오염을 씻어 냅니다."),
            new(SpellFamily.Fire, "집중 별", "star", new[] { "star" }, "별 모양 초점으로 타격을 한곳에 모읍니다."),
            new(SpellFamily.Life, "연결 가새", "brace", new[] { "brace" }, "떨어진 대상을 생명력으로 묶어 연결합니다.")
        };

        [Header("Scene References")]
        public Camera mainCamera = null!;
        public Transform player = null!;
        public Canvas canvas = null!;

        private readonly List<SealView> seals = new();
        private readonly List<ParticlePulse> pulses = new();
        private readonly List<CharacterBarrierView> defaultBarriers = new();
        private readonly List<DamagePopupView> damagePopups = new();
        private readonly List<StageGate> activeStageGates = new();
        private readonly List<GameObject> floorObjects = new();
        private readonly List<WorldStateGoal> activeGoals = new();
        private readonly List<HazardZone> activeHazards = new();
        private readonly Dictionary<SpellFamily, int> baseFailureCounts = new();

        private ExamLogger logger = null!;
        private WorldDrawingController worldDrawing = null!;
        private FloorController floorController = null!;
        private MagicNote magicNote = null!;
        private EndingReport endingReport = null!;
        private SpellCastingService spellCasting = null!;
        private IStrokeRecognitionService recognitionService = null!;
        private CustomShapeProfileStore customShapeStore = null!;
        private CustomShapeBookController customShapeBook = null!;
        private FloorGoalSystem floorGoals = null!;
        private RectTransform hudPanel = null!;
        private RectTransform notePanel = null!;
        private RectTransform resultPanel = null!;
        private RectTransform reportPanel = null!;
        private RectTransform customReferenceBubble = null!;
        private RectTransform customReferencePanel = null!;
        private Text customReferenceStatus = null!;
        private Button floorSkipButton = null!;
        private Text hudTitle = null!;
        private Text hudCopy = null!;
        private Text floorProgress = null!;
        private Text noteText = null!;
        private Text resultText = null!;
        private Text reportText = null!;
        private Text versionText = null!;
        private Font uiFont = null!;
        private string sessionId = "";
        private int trialCounter;
        private float floorStartedAt;
        private float pendingAdvanceAt = -1f;
        private Vector2 velocity;
        private Vector2 safePosition;
        private bool finalCompletionCelebrated;
        private bool finalTrueEnding;
        private bool resultPanelCompact;
        private string customReferenceLastStatus = "";

        public int CurrentFloorNumber => floorController?.CurrentFloorNumber ?? 1;
        public int FloorCount => floorController?.FloorCount ?? 5;
        public int ActiveSealCount => seals.Count;
        public int ActiveGoalCount => activeGoals.Count;
        public int CompletedGoalCountForTests => activeGoals.Count(goal => goal.completed);
        public Vector2 PlayerPosition => player == null ? Vector2.zero : player.position;
        public Vector2 SafePositionForTests => safePosition;
        public bool HasEndingReport => reportPanel != null && reportPanel.gameObject.activeSelf;
        public bool IsDrawingPanelVisible => false;
        public bool IsResultPanelVisible => resultPanel != null && resultPanel.gameObject.activeSelf;
        public int CurrentAssistLevel { get; private set; }
        public string LastHintText { get; private set; } = "";
        public string LastMagicNoteText => magicNote?.Text ?? "";
        public string LastResultPanelTextForTests => resultText == null ? "" : resultText.text;
        public string HudCopyForTests => hudCopy == null ? "" : hudCopy.text;
        public string FloorProgressForTests => floorProgress == null ? "" : floorProgress.text;
        public string EndingReportTextForTests => reportText == null ? "" : reportText.text;
        public int TrialCountForTests => trialCounter;
        public string VersionLabelForTests => versionText == null ? "" : versionText.text;
        public int ActivePulseCountForTests => pulses.Count;
        public int ActiveDefaultBarrierCountForTests => defaultBarriers.Count;
        public Color LastDefaultBarrierColorForTests => defaultBarriers.Count == 0 ? Color.clear : defaultBarriers[^1].Color;
        public int ActiveStageGateCountForTests => activeStageGates.Count;
        public int ActiveDamagePopupCountForTests => damagePopups.Count;
        public string LastDamagePopupTextForTests { get; private set; } = "";
        public string LastCustomShapeEventKindForTests { get; private set; } = "";
        public string LastCustomShapeEventLabelForTests { get; private set; } = "";
        public Vector2 LastCustomShapeEventDirectionForTests { get; private set; } = Vector2.right;
        public int CustomShapeEventObjectCountForTests { get; private set; }
        public int VisibleGoalLabelCountForTests => activeGoals.Count(goal => goal.label != null);
        public int VisibleOverlayGuideCountForTests => seals.Count(seal => seal.HasAttachGuide);
        public bool IsFloorSkipButtonVisibleForTests => floorSkipButton != null && floorSkipButton.gameObject.activeInHierarchy;
        public string OutputDirectory => logger?.OutputDirectory ?? "";
        public float PendingAdvanceSecondsForTests => pendingAdvanceAt < 0f ? -1f : pendingAdvanceAt - Time.time;
        public float LastSealLifetimeSecondsForTests => seals.Count == 0 ? 0f : seals[^1].seal.expiresAt - seals[^1].seal.createdAt;
        public IReadOnlyList<OverlayOperator> LastOverlayStack => seals.Count == 0 ? Array.Empty<OverlayOperator>() : seals[^1].seal.overlayStack;
        public int PersonalizationCaptureCountForTests => recognitionService?.PersonalizationStore.CaptureCount ?? 0;
        public int CustomShapeSlotCountForTests => customShapeBook?.SlotCount ?? 0;
        public bool IsCustomPenPopupVisibleForTests => customShapeBook?.IsPenPopupVisible ?? false;
        public bool IsCustomShapePageOpenForTests => customShapeBook?.IsPageOpen ?? false;
        public bool IsCustomShapeBubbleVisibleForTests => customShapeBook?.IsBubbleVisible ?? false;
        public bool IsCustomShapeEditorOpenForTests => customShapeBook?.IsEditorOpen ?? false;
        public bool IsCustomReferenceBubbleVisibleForTests => customReferenceBubble != null && customReferenceBubble.gameObject.activeInHierarchy;
        public bool IsCustomReferencePanelOpenForTests => customReferencePanel != null && customReferencePanel.gameObject.activeInHierarchy;
        public int CustomReferenceCountForTests => CustomShapeReferences.Count;
        public string CustomReferenceStatusForTests => customReferenceLastStatus;
        public TutorialPersonalizationSummary LastPersonalizationSummaryForTests { get; private set; } = TutorialPersonalizationSummary.Empty;
        private bool IsFinalFloor => floorController.CurrentFloorIndex >= floorController.FloorCount - 1;

        private void Awake()
        {
            sessionId = $"unity-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6]}";
            logger = new ExamLogger(sessionId);
            uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 18);
            floorController = new FloorController();
            magicNote = new MagicNote();
            endingReport = new EndingReport();
            spellCasting = new SpellCastingService();
            customShapeStore = CustomShapeProfileStore.LoadDefault();
            recognitionService = new HeuristicStrokeRecognitionService(null, customShapeStore);
            floorGoals = new FloorGoalSystem();
            ResolveSceneReferences();
            BuildUi();
            customShapeBook = new CustomShapeBookController();
            customShapeBook.Initialize(canvas, mainCamera, player, uiFont, customShapeStore);
            ConfigureWorldDrawing();
            LoadFloor(0);
        }

        private void Update()
        {
            customShapeBook?.Tick();
            TickCustomReferenceShelf();
            if (customShapeBook?.BlocksGameplayInput == true || IsCustomReferencePanelOpenForTests)
            {
                velocity = Vector2.zero;
            }
            else
            {
                TickGameplayCancelInput();
                TickPlayer();
                TickStageGates();
            }

            TickSeals();
            TickPulses();
            TickDefaultBarriers();
            TickDamagePopups();
            TickHazards();
            TickFloorAdvance();
            magicNote.Tick(Time.deltaTime);
            UpdateHud();
        }

        private void TickGameplayCancelInput()
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
            {
                worldDrawing?.CancelBufferedInput();
            }
        }

        public BaseRecognitionResult CastSyntheticBaseForTests(SpellFamily family, Vector2 worldCenter)
        {
            var strokes = Offset(GestureRecognizer.CreateCanonicalSamples(family, 1.6f, 0.03f), worldCenter, 0.8f);
            var result = SpellRuntime.RecognizeBase(strokes);
            return SubmitBaseRecognitionResult(result, worldCenter, strokes.Count);
        }

        public BaseRecognitionResult CastRawBaseForTests(List<List<StrokeSample>> strokes, Vector2 worldCenter)
        {
            return ProcessSpellGroup(strokes, worldCenter, strokes.Count).baseResult;
        }

        public OverlayRecognitionResult CastSyntheticOverlayForTests(OverlayOperator op, Vector2 worldCenter, float sealScaleRatio = 0.24f)
        {
            var nearestSeal = FindAttachableSeal(worldCenter);
            if (nearestSeal == null)
            {
                return new OverlayRecognitionResult
                {
                    status = RecognitionStatus.Invalid,
                    recognizedOperator = op,
                    feedbackReason = "No active seal is close enough for a synthetic overlay submission."
                };
            }

            var scale = nearestSeal.seal.worldScale * sealScaleRatio;
            var strokes = OverlayRecognizer.CreateCanonicalSamples(op, worldCenter, scale, 0.03f);
            var result = OverlayRecognizer.Recognize(strokes, nearestSeal.seal);
            return ApplySubmittedSpellOutcome(spellCasting.ProcessOverlayResult(result, nearestSeal.seal, worldCenter, strokes.Count)).overlayResult;
        }

        public void CompleteCurrentFloorForTests()
        {
            foreach (var goal in activeGoals)
            {
                ActivateGoal(goal, "test_completion");
            }
            EvaluateFloorCompletion();
        }

        public void CompleteCurrentGoalsForTests(int count)
        {
            foreach (var goal in activeGoals.Where(goal => !goal.completed).Take(count))
            {
                ActivateGoal(goal, "test_completion");
            }
            EvaluateFloorCompletion();
        }

        public void AdvanceFloorForTests()
        {
            if (floorController.CurrentFloorIndex < floorController.FloorCount - 1)
            {
                LoadFloor(floorController.CurrentFloorIndex + 1);
            }
            else
            {
                ShowEndingReport();
            }
        }

        public void LoadFloorForTests(int index)
        {
            LoadFloor(index);
        }

        public void MovePlayerForTests(Vector2 worldPosition)
        {
            player.position = worldPosition;
        }

        public IReadOnlyList<SpellSealSnapshot> GetActiveSealSnapshots()
        {
            return seals
                .Where(view => Time.time <= view.seal.expiresAt)
                .Select(view => SpellSealSnapshot.From(view.seal, Time.time))
                .ToList();
        }

        public SpellSealSnapshot FindAttachableSealSnapshot(Vector2 worldCenter)
        {
            var seal = SpellCastingService.FindAttachableSeal(seals.Select(view => view.seal).ToList(), worldCenter, Time.time);
            return seal == null ? null : SpellSealSnapshot.From(seal, Time.time);
        }

        public BaseRecognitionResult SubmitBaseRecognitionResult(BaseRecognitionResult result, Vector2 worldCenter, int strokeCount)
        {
            if (HasEndingReport)
            {
                return result;
            }

            return ApplySubmittedSpellOutcome(spellCasting.ProcessBaseResult(result, worldCenter, strokeCount, Time.time)).baseResult;
        }

        public OverlayRecognitionResult SubmitOverlayRecognitionResult(OverlayRecognitionResult result, string sealId, Vector2 worldCenter, int strokeCount)
        {
            if (HasEndingReport)
            {
                return result;
            }

            var seal = FindActiveSealById(sealId);
            if (seal == null)
            {
                throw new ArgumentException($"Active seal was not found: {sealId}", nameof(sealId));
            }

            if (!OverlayCenterIsAttachable(seal, worldCenter))
            {
                throw new InvalidOperationException("Overlay result center is outside the active seal attach radius.");
            }

            return ApplySubmittedSpellOutcome(spellCasting.ProcessOverlayResult(result, seal, worldCenter, strokeCount)).overlayResult;
        }

        public OverlayRecognitionResult SubmitOverlayRecognitionResult(OverlayRecognitionResult result, Vector2 worldCenter, int strokeCount)
        {
            if (HasEndingReport)
            {
                return result;
            }

            var snapshot = FindAttachableSealSnapshot(worldCenter);
            if (snapshot == null)
            {
                throw new InvalidOperationException("No active seal is close enough for an overlay result.");
            }

            return SubmitOverlayRecognitionResult(result, snapshot.sealId, worldCenter, strokeCount);
        }

        public void OpenCustomShapePenPopupForTests()
        {
            customShapeBook?.OpenPenPopupForTests();
        }

        public void OpenCustomShapePageForTests()
        {
            customShapeBook?.OpenPageForTests();
        }

        public void RequestCustomShapeSlotForTests(int slotIndex)
        {
            customShapeBook?.RequestSlotForTests(slotIndex);
        }

        public void DeclineCustomShapeBubbleForTests()
        {
            customShapeBook?.DeclineBubbleForTests();
        }

        public void ConfirmCustomShapeBubbleForTests()
        {
            customShapeBook?.ConfirmBubbleForTests();
        }

        public bool IsCustomShapeSlotOccupiedForTests(int slotIndex)
        {
            return customShapeBook?.IsSlotOccupied(slotIndex) ?? false;
        }

        public string CustomShapeSlotLabelForTests(int slotIndex)
        {
            return customShapeBook?.SlotLabel(slotIndex) ?? "";
        }

        public SpellFamily CustomShapeSlotMappedFamilyForTests(int slotIndex)
        {
            return customShapeBook?.SlotMappedFamily(slotIndex) ?? SpellFamily.Wind;
        }

        public bool SaveCustomShapeSlotForTests(
            int slotIndex,
            string label,
            string regexPattern,
            SpellFamily mappedFamily,
            IReadOnlyList<IReadOnlyList<StrokeSample>> goldStrokes,
            out string message)
        {
            if (customShapeBook == null)
            {
                message = "custom shape controller unavailable";
                return false;
            }

            return customShapeBook.SaveSlotForTests(slotIndex, label, regexPattern, mappedFamily, goldStrokes, out message);
        }

        public bool SaveCustomShapeSlotForTests(
            int slotIndex,
            string label,
            string regexPattern,
            string shapeToken,
            SpellFamily mappedFamily,
            IReadOnlyList<IReadOnlyList<StrokeSample>> goldStrokes,
            out string message)
        {
            if (customShapeBook == null)
            {
                message = "custom shape controller unavailable";
                return false;
            }

            return customShapeBook.SaveSlotForTests(slotIndex, label, regexPattern, shapeToken, mappedFamily, goldStrokes, out message);
        }

        public bool DeleteCustomShapeSlotForTests(int slotIndex)
        {
            return customShapeBook?.DeleteSlotForTests(slotIndex) ?? false;
        }

        public void OpenCustomReferencePanelForTests()
        {
            OpenCustomReferencePanel();
        }

        public bool ImportCustomReferenceForTests(SpellFamily family, out int slotIndex, out string message)
        {
            var reference = CustomShapeReferences.FirstOrDefault(item => item.family == family);
            if (reference == null)
            {
                slotIndex = -1;
                message = "reference not found";
                return false;
            }

            return ImportCustomReference(reference, out slotIndex, out message);
        }

        public List<List<StrokeSample>> CustomReferenceStrokesForTests(SpellFamily family, Vector2 worldCenter)
        {
            var reference = CustomShapeReferences.FirstOrDefault(item => item.family == family) ?? CustomShapeReferences[0];
            return BuildReferenceStrokes(reference.shapeToken, worldCenter, 1.6f);
        }

        public void UseCustomShapeStoreForTests(string storagePath)
        {
            customShapeStore = new CustomShapeProfileStore(storagePath);
            var personalizationStore = recognitionService?.PersonalizationStore ?? new TutorialPersonalizationStore();
            recognitionService = new HeuristicStrokeRecognitionService(personalizationStore, customShapeStore);
            customShapeBook = new CustomShapeBookController();
            customShapeBook.Initialize(canvas, mainCamera, player, uiFont, customShapeStore);
        }

        private void ResolveSceneReferences()
        {
            mainCamera ??= Camera.main;
            if (mainCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(0f, 0f, -10f);
                mainCamera = cameraObject.AddComponent<Camera>();
            }
            ConfigureMainCamera(mainCamera);

            if (player == null)
            {
                var playerObject = new GameObject("Apprentice");
                playerObject.transform.position = new Vector3(0f, -4.05f, 0f);
                playerObject.transform.localScale = Vector3.one * 0.78f;
                playerObject.AddComponent<SpriteRenderer>();
                var sprite = playerObject.AddComponent<PixelSpriteView>();
                sprite.kind = PixelSpriteKind.Player;
                sprite.primary = new Color(0.95f, 0.92f, 0.78f);
                sprite.secondary = new Color(0.28f, 0.62f, 0.96f);
                sprite.sortingOrder = 30;
                player = playerObject.transform;
            }

            if (canvas == null)
            {
                var canvasObject = new GameObject("Exam Canvas");
                canvasObject.AddComponent<RectTransform>();
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280, 720);
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
            }
        }

        private void ConfigureWorldDrawing()
        {
            worldDrawing = gameObject.GetComponent<WorldDrawingController>() ?? gameObject.AddComponent<WorldDrawingController>();
            worldDrawing.mainCamera = mainCamera;
            worldDrawing.ApplyPlayableDefaults();
            worldDrawing.StrokeSessionCompleted += OnStrokeSessionCompleted;
            worldDrawing.InputCancelled += OnDrawingCancelled;
        }

        private static void ConfigureMainCamera(Camera camera)
        {
            camera.orthographic = true;
            camera.orthographicSize = GameplayCameraOrthographicSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.043f, 0.055f);
        }

        private void BuildUi()
        {
            ClearChildren(canvas.transform);
            hudPanel = CreatePanel("HUD", canvas.transform, new Vector2(20, -20), new Vector2(560, 132), Anchor.TopLeft, new Color(0.04f, 0.055f, 0.075f, 0.88f));
            hudTitle = CreateText("HUD Title", hudPanel, "Magic Exam Hall", 24, FontStyle.Bold, new Vector2(16, -12), new Vector2(520, 28), Anchor.TopLeft);
            hudCopy = CreateText("HUD Copy", hudPanel, "", 15, FontStyle.Normal, new Vector2(16, -46), new Vector2(520, 60), Anchor.TopLeft);
            floorProgress = CreateText("Floor Progress", hudPanel, "", 15, FontStyle.Bold, new Vector2(16, 12), new Vector2(520, 24), Anchor.BottomLeft);

            notePanel = CreatePanel("Magic Note", canvas.transform, new Vector2(20, 20), new Vector2(560, 112), Anchor.BottomLeft, new Color(0.04f, 0.055f, 0.075f, 0.84f));
            noteText = CreateText("Note Text", notePanel, "", 14, FontStyle.Normal, new Vector2(14, -12), new Vector2(530, 88), Anchor.TopLeft);

            resultPanel = CreatePanel("Spell Result", canvas.transform, new Vector2(-20, -20), new Vector2(430, 178), Anchor.TopRight, new Color(0.04f, 0.055f, 0.075f, 0.88f));
            resultText = CreateText("Result Text", resultPanel, "", 13, FontStyle.Normal, new Vector2(14, -12), new Vector2(402, 152), Anchor.TopLeft);
            UpdateResultPanelLayout();
            resultPanel.gameObject.SetActive(false);

            reportPanel = CreatePanel("Ending Report", canvas.transform, Vector2.zero, new Vector2(760, 520), Anchor.Center, new Color(0.035f, 0.045f, 0.065f, 0.96f));
            reportText = CreateText("Report Text", reportPanel, "", 17, FontStyle.Normal, new Vector2(28, -28), new Vector2(704, 464), Anchor.TopLeft);
            reportPanel.gameObject.SetActive(false);

            versionText = CreateText("Build Version", canvas.transform, BuildVersion, 11, FontStyle.Normal, new Vector2(-14, 10), new Vector2(300, 20), Anchor.BottomRight);
            versionText.alignment = TextAnchor.MiddleRight;
            versionText.color = new Color(1f, 1f, 1f, 0.62f);

            floorSkipButton = CreateButton(
                "Floor Test Skip Button",
                canvas.transform,
                "스킵",
                15,
                FontStyle.Bold,
                new Vector2(-18f, 42f),
                new Vector2(94f, 40f),
                Anchor.BottomRight,
                new Color(0.82f, 0.06f, 0.04f, 0.94f),
                SkipCurrentFloorForDebug);

            BuildCustomReferenceUi();
        }

        private void BuildCustomReferenceUi()
        {
            customReferenceBubble = CreatePanel(
                "Custom Reference Shelf Bubble",
                canvas.transform,
                Vector2.zero,
                new Vector2(276f, 92f),
                Anchor.Center,
                new Color(0.035f, 0.055f, 0.07f, 0.95f));
            AddPanelBorder(customReferenceBubble, new Color(0.72f, 0.84f, 1f, 0.74f), 2f);
            var tail = CreateImage("Custom Reference Shelf Bubble Tail", customReferenceBubble, new Vector2(32f, -72f), new Vector2(28f, 28f), Anchor.TopLeft, new Color(0.035f, 0.055f, 0.07f, 0.95f));
            tail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            tail.raycastTarget = false;
            var bubbleText = CreateText("Custom Reference Shelf Bubble Text", customReferenceBubble, "도형 레퍼런스\n책장에서 커스텀 도형을 가져올 수 있습니다.", 13, FontStyle.Bold, new Vector2(16f, -12f), new Vector2(164f, 66f), Anchor.TopLeft);
            bubbleText.color = new Color(0.92f, 0.98f, 1f, 1f);
            bubbleText.raycastTarget = false;
            CreateButton(
                "Custom Reference Shelf Open Button",
                customReferenceBubble,
                "보기",
                14,
                FontStyle.Bold,
                new Vector2(190f, -28f),
                new Vector2(70f, 40f),
                Anchor.TopLeft,
                new Color(0.12f, 0.24f, 0.38f, 0.98f),
                OpenCustomReferencePanel);
            customReferenceBubble.gameObject.SetActive(false);

            customReferencePanel = CreatePanel(
                "Custom Reference Panel",
                canvas.transform,
                Vector2.zero,
                new Vector2(760f, 610f),
                Anchor.Center,
                new Color(0.025f, 0.035f, 0.055f, 0.98f));
            AddPanelBorder(customReferencePanel, new Color(0.65f, 0.78f, 0.95f, 0.82f), 2f);
            var title = CreateText("Custom Reference Panel Title", customReferencePanel, "커스텀 도형 레퍼런스", 23, FontStyle.Bold, new Vector2(24f, -18f), new Vector2(500f, 34f), Anchor.TopLeft);
            title.color = new Color(0.95f, 0.99f, 1f, 1f);
            CreateButton(
                "Custom Reference Panel Close Button",
                customReferencePanel,
                "닫기",
                14,
                FontStyle.Bold,
                new Vector2(-82f, -18f),
                new Vector2(60f, 34f),
                Anchor.TopRight,
                new Color(0.10f, 0.18f, 0.30f, 0.96f),
                CloseCustomReferencePanel);

            for (var index = 0; index < CustomShapeReferences.Count; index++)
            {
                CreateCustomReferenceCard(CustomShapeReferences[index], index);
            }

            customReferenceStatus = CreateText(
                "Custom Reference Status",
                customReferencePanel,
                "",
                14,
                FontStyle.Bold,
                new Vector2(24f, 22f),
                new Vector2(712f, 24f),
                Anchor.BottomLeft);
            customReferenceStatus.color = new Color(0.80f, 0.92f, 1f, 0.92f);
            customReferencePanel.gameObject.SetActive(false);
        }

        private void CreateCustomReferenceCard(CustomShapeReferenceDefinition reference, int index)
        {
            var y = -64f - index * 60f;
            var card = CreatePanel(
                $"Custom Reference Card {reference.family}",
                customReferencePanel,
                new Vector2(24f, y),
                new Vector2(712f, 52f),
                Anchor.TopLeft,
                new Color(0.045f, 0.065f, 0.095f, 0.96f));
            AddPanelBorder(card, new Color(1f, 1f, 1f, 0.12f), 1f);
            var familyColor = FamilyColor(reference.family);
            var swatch = CreateImage($"Custom Reference Swatch {reference.family}", card, new Vector2(10f, -9f), new Vector2(40f, 40f), Anchor.TopLeft, new Color(familyColor.r, familyColor.g, familyColor.b, 0.70f));
            swatch.sprite = CustomShapeSpriteFactory.CreateShapeSprite(reference.shapeToken, 2);
            swatch.preserveAspect = true;
            var label = CreateText(
                $"Custom Reference Label {reference.family}",
                card,
                $"{SpellLabels.Korean(reference.family)}: {reference.label}",
                15,
                FontStyle.Bold,
                new Vector2(62f, -8f),
                new Vector2(250f, 22f),
                Anchor.TopLeft);
            label.color = Color.Lerp(familyColor, Color.white, 0.55f);
            var summary = CreateText(
                $"Custom Reference Summary {reference.family}",
                card,
                reference.summary,
                12,
                FontStyle.Normal,
                new Vector2(62f, -31f),
                new Vector2(470f, 18f),
                Anchor.TopLeft);
            summary.color = new Color(0.82f, 0.88f, 0.94f, 0.90f);
            var capturedReference = reference;
            CreateButton(
                $"Import Custom Reference {reference.family}",
                card,
                "들여오기",
                13,
                FontStyle.Bold,
                new Vector2(592f, -8f),
                new Vector2(104f, 38f),
                Anchor.TopLeft,
                new Color(0.10f, 0.24f, 0.38f, 0.98f),
                () =>
                {
                    ImportCustomReference(capturedReference, out _, out _);
                });
        }

        private void LoadFloor(int index)
        {
            pendingAdvanceAt = -1f;
            finalCompletionCelebrated = false;
            finalTrueEnding = false;
            reportPanel.gameObject.SetActive(false);
            resultPanel.gameObject.SetActive(false);
            floorSkipButton.gameObject.SetActive(true);
            CloseCustomReferenceUi();
            ClearFloorObjects();
            floorController.Load(index);
            safePosition = new Vector2(0f, -4.05f);
            player.position = safePosition;
            floorStartedAt = Time.time;
            activeGoals.Clear();
            activeGoals.AddRange(floorController.Current.goals.Select(goal => goal.Clone()));
            activeHazards.Clear();
            activeHazards.AddRange(floorController.Current.hazards.Select(hazard => hazard.Clone()));
            BuildFloorArt(floorController.Current);
            magicNote.Show(BuildFloorEntryNote(floorController.Current));
        }

        private void SkipCurrentFloorForDebug()
        {
            if (HasEndingReport)
            {
                return;
            }

            pendingAdvanceAt = -1f;
            resultPanel.gameObject.SetActive(false);
            if (floorController.CurrentFloorIndex < floorController.FloorCount - 1)
            {
                LoadFloor(floorController.CurrentFloorIndex + 1);
                return;
            }

            ShowEndingReport();
        }

        private void TickCustomReferenceShelf()
        {
            if (customReferenceBubble == null || player == null || floorController == null)
            {
                return;
            }

            var onReferenceFloor = floorController.Current.number >= CustomReferenceFloorNumber && floorController.Current.number <= 4;
            var closeToShelf = onReferenceFloor && Vector2.Distance(player.position, WestBookcasePosition) <= CustomReferenceShelfRadius;
            var shouldShowBubble = closeToShelf &&
                                   !IsCustomReferencePanelOpenForTests &&
                                   customShapeBook?.BlocksGameplayInput != true &&
                                   !HasEndingReport;
            customReferenceBubble.gameObject.SetActive(shouldShowBubble);
            if (!shouldShowBubble)
            {
                return;
            }

            customReferenceBubble.anchoredPosition = WorldToCanvasPosition(WestBookcasePosition + new Vector2(0.88f, 1.22f));
        }

        private void OpenCustomReferencePanel()
        {
            if (customReferencePanel == null)
            {
                return;
            }

            customReferencePanel.gameObject.SetActive(true);
            if (customReferenceBubble != null)
            {
                customReferenceBubble.gameObject.SetActive(false);
            }

            SetCustomReferenceStatus("각 base 레퍼런스를 빈 커스텀 슬롯으로 들여올 수 있습니다.");
        }

        private void CloseCustomReferencePanel()
        {
            if (customReferencePanel != null)
            {
                customReferencePanel.gameObject.SetActive(false);
            }
        }

        private void CloseCustomReferenceUi()
        {
            if (customReferenceBubble != null)
            {
                customReferenceBubble.gameObject.SetActive(false);
            }

            CloseCustomReferencePanel();
            SetCustomReferenceStatus("");
        }

        private bool ImportCustomReference(CustomShapeReferenceDefinition reference, out int slotIndex, out string message)
        {
            slotIndex = -1;
            if (customShapeStore == null)
            {
                message = "custom shape store unavailable";
                SetCustomReferenceStatus(message);
                return false;
            }

            for (var index = 0; index < CustomShapeProfileStore.SlotCount; index++)
            {
                if (!customShapeStore.IsSlotOccupied(index))
                {
                    slotIndex = index;
                    break;
                }
            }

            if (slotIndex < 0)
            {
                message = "빈 커스텀 슬롯이 필요합니다.";
                SetCustomReferenceStatus(message);
                return false;
            }

            var regexPattern = CustomShapeProfileStore.BuildGeneratedRegex(reference.label, reference.shapeToken);
            var gold = BuildReferenceStrokes(reference.shapeToken, Vector2.zero, 1.6f);
            var saved = customShapeStore.TrySaveSlot(
                slotIndex,
                reference.label,
                regexPattern,
                reference.shapeToken,
                reference.eventShapeTokens,
                reference.family,
                gold,
                out message);
            if (saved)
            {
                customShapeBook?.RefreshFromStoreForExternalChange();
                message = $"{reference.label} 도형을 슬롯 {slotIndex + 1:00}에 들여왔습니다.";
            }

            SetCustomReferenceStatus(message);
            return saved;
        }

        private void SetCustomReferenceStatus(string message)
        {
            customReferenceLastStatus = message ?? "";
            if (customReferenceStatus != null)
            {
                customReferenceStatus.text = customReferenceLastStatus;
            }
        }

        private Vector2 WorldToCanvasPosition(Vector2 worldPosition)
        {
            var canvasRect = canvas.GetComponent<RectTransform>();
            var screenPoint = RectTransformUtility.WorldToScreenPoint(mainCamera, worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out var localPoint);
            var half = canvasRect.rect.size * 0.5f;
            return new Vector2(
                Mathf.Clamp(localPoint.x, -half.x + 158f, half.x - 158f),
                Mathf.Clamp(localPoint.y, -half.y + 66f, half.y - 66f));
        }

        private static List<List<StrokeSample>> BuildReferenceStrokes(string token, Vector2 center, float scale)
        {
            var elapsed = 0f;
            return CustomShapeUiDrawing.NormalizedStrokes(token)
                .Select(stroke =>
                {
                    var samples = stroke
                        .Select(point =>
                        {
                            elapsed += 0.03f;
                            return new StrokeSample((point - new Vector2(0.5f, 0.5f)) * scale + center, elapsed);
                        })
                        .ToList();
                    elapsed += 0.05f;
                    return samples;
                })
                .ToList();
        }

        private void BuildFloorArt(FloorDefinition floor)
        {
            var floorRoot = new GameObject($"Floor {floor.number} - {floor.title}");
            floorObjects.Add(floorRoot);
            CreateWorldSprite("Exam Hall Backdrop", Vector2.zero, Vector3.one, new Color(0.045f, 0.052f, 0.067f), new Color(0.035f, 0.04f, 0.052f), PixelSpriteKind.FloorTile, -9, true, new Vector2(20.5f, 11.6f), floorRoot.transform);
            CreateWorldSprite("Stone Tile Floor", Vector2.zero, Vector3.one, new Color(0.15f, 0.17f, 0.22f), new Color(0.09f, 0.11f, 0.15f), PixelSpriteKind.FloorTile, -7, true, new Vector2(16.4f, 10f), floorRoot.transform);
            CreateWorldSprite("North Carved Wall", new Vector2(0f, 4.95f), Vector3.one, new Color(0.22f, 0.20f, 0.27f), floor.accentColor, PixelSpriteKind.WallTrim, -4, true, new Vector2(16.4f, 1.15f), floorRoot.transform);
            CreateWorldSprite("South Carved Wall", new Vector2(0f, -4.95f), Vector3.one, new Color(0.18f, 0.17f, 0.22f), new Color(0.50f, 0.40f, 0.20f), PixelSpriteKind.WallTrim, -4, true, new Vector2(16.4f, 0.8f), floorRoot.transform);
            CreateWorldSprite("Center Runner", new Vector2(0f, 0.12f), Vector3.one, floor.rugColor, floor.accentColor, PixelSpriteKind.Rug, -5, true, new Vector2(2.2f, 7.6f), floorRoot.transform);
            CreateWorldSprite("West Bookcase", WestBookcasePosition, Vector3.one * 1.15f, new Color(0.42f, 0.23f, 0.12f), floor.accentColor, PixelSpriteKind.Bookshelf, -1, false, Vector2.one, floorRoot.transform);
            CreateWorldSprite("East Bookcase", new Vector2(7.25f, 1.1f), Vector3.one * 1.15f, new Color(0.42f, 0.23f, 0.12f), floor.accentColor, PixelSpriteKind.Bookshelf, -1, false, Vector2.one, floorRoot.transform);
            CreateWorldSprite("Northwest Candle", new Vector2(-6.85f, 3.65f), Vector3.one * 0.85f, new Color(0.63f, 0.57f, 0.44f), new Color(1f, 0.56f, 0.15f), PixelSpriteKind.Candle, 2, false, Vector2.one, floorRoot.transform);
            CreateWorldSprite("Northeast Candle", new Vector2(6.85f, 3.65f), Vector3.one * 0.85f, new Color(0.63f, 0.57f, 0.44f), new Color(1f, 0.56f, 0.15f), PixelSpriteKind.Candle, 2, false, Vector2.one, floorRoot.transform);

            if (floor.number == 3)
            {
                BuildFloorThreeStageArt(floorRoot.transform);
            }
            else if (floor.number == 4)
            {
                BuildFloorFourCombatArt(floorRoot.transform);
            }

            foreach (var goal in activeGoals)
            {
                var body = CreateWorldSprite(goal.title, goal.position, Vector3.one * goal.visualScale, goal.color, Color.white, goal.kind, 3, false, Vector2.one, floorRoot.transform);
                goal.body = body;
                goal.renderer = body.GetComponent<SpriteRenderer>();
                if (goal.kind == PixelSpriteKind.RuneCircle)
                {
                    body.transform.localScale *= 1.45f;
                }
                goal.label = CreateGoalLabel(goal, floorRoot.transform);
            }

            foreach (var hazard in activeHazards)
            {
                var body = CreateWorldSprite(hazard.title, hazard.position, Vector3.one * hazard.radius, hazard.color, new Color(1f, 1f, 1f, 0.6f), PixelSpriteKind.Pulse, 1, false, Vector2.one, floorRoot.transform);
                hazard.body = body;
            }
        }

        private void BuildFloorThreeStageArt(Transform parent)
        {
            AddStageGate(
                "living_bridge",
                "낭떠러지",
                new Vector2(0f, -2.45f),
                new Vector2(15.0f, 0.72f),
                new Vector2(0f, -3.25f),
                new Color(0.025f, 0.030f, 0.045f, 1f),
                "앞의 낭떠러지는 발판 없이는 건널 수 없습니다. 생명 문양 위에 화살표와 사각 발판 도형을 얹으세요.",
                parent);
            AddStageGate(
                "frozen_river",
                "강",
                new Vector2(0f, -0.62f),
                new Vector2(15.0f, 0.64f),
                new Vector2(0f, -1.34f),
                new Color(0.05f, 0.23f, 0.38f, 1f),
                "강물은 얼린 뒤 지나갈 수 있습니다. 물 문양 위에 육각형 도형을 얹으세요.",
                parent);
            AddStageGate(
                "earth_stairs",
                "가파른 길",
                new Vector2(0f, 1.08f),
                new Vector2(15.0f, 0.72f),
                new Vector2(0f, 0.30f),
                new Color(0.27f, 0.17f, 0.11f, 1f),
                "가파른 길에는 계단이 필요합니다. 땅 문양 위에 사각형 도형을 얹으세요.",
                parent);
            AddStageGate(
                "wind_platform",
                "먼 발판",
                new Vector2(0f, 2.78f),
                new Vector2(15.0f, 0.64f),
                new Vector2(0f, 2.05f),
                new Color(0.06f, 0.12f, 0.16f, 1f),
                "마지막 빈 공간은 떠 있는 발판으로 건넙니다. 바람 문양 위에 사각형 도형을 얹으세요.",
                parent);
        }

        private void AddStageGate(
            string requiredGoalId,
            string title,
            Vector2 center,
            Vector2 size,
            Vector2 resetPosition,
            Color color,
            string lockedNote,
            Transform parent)
        {
            var body = CreateWorldSprite(
                $"Stage Gate {title}",
                center,
                Vector3.one,
                color,
                Color.Lerp(color, Color.white, 0.22f),
                PixelSpriteKind.FloorTile,
                -3,
                true,
                size,
                parent);
            activeStageGates.Add(new StageGate(requiredGoalId, center, size, resetPosition, lockedNote, body));
        }

        private void BuildFloorFourCombatArt(Transform parent)
        {
            foreach (var goal in activeGoals)
            {
                var dummy = CreateWorldSprite(
                    $"Training Target {goal.id}",
                    goal.position + new Vector2(0f, -0.42f),
                    Vector3.one * 0.78f,
                    new Color(0.46f, 0.42f, 0.34f),
                    goal.color,
                    PixelSpriteKind.Target,
                    1,
                    false,
                    Vector2.one,
                    parent);
                var renderer = dummy.GetComponent<SpriteRenderer>();
                renderer.color = Color.Lerp(goal.color, Color.white, 0.34f);
            }
        }

        private void OnSpellBuffered(List<List<StrokeSample>> strokes, Vector2 center, int strokeCount)
        {
            if (HasEndingReport)
            {
                return;
            }

            ProcessSpellGroup(strokes, center, strokeCount);
        }

        private void OnStrokeSessionCompleted(StrokeInputSession session)
        {
            ProcessStrokeSession(session);
        }

        private void OnDrawingCancelled()
        {
            if (HasEndingReport)
            {
                return;
            }

            resultPanel.gameObject.SetActive(false);
            magicNote.Show("입력을 취소했습니다. 우클릭 hold로 다시 그리세요.");
        }

        private ProcessedSpell ProcessSpellGroup(List<List<StrokeSample>> strokes, Vector2 center, int strokeCount)
        {
            var session = StrokeInputSessionExtensions.FromStrokeSamples(
                strokes,
                $"legacy-{Guid.NewGuid():N}",
                Time.time,
                InputCoordinateSpace.World);
            return ProcessStrokeSession(session);
        }

        private ProcessedSpell ProcessStrokeSession(StrokeInputSession session)
        {
            trialCounter++;
            var now = Time.time;
            var hadActiveSeal = HasActiveSeal(now);
            if (hadActiveSeal)
            {
                MarkPostSealInputSeen(now);
            }

            var baseIntent = ResolveBaseIntent(session.GetWorldCenter());
            var recognition = recognitionService.Recognize(session, new RecognitionContext
            {
                activeSeals = seals.Select(view => view.seal).ToList(),
                baseIntent = baseIntent,
                customShapesOnlyWhenSealActive = true,
                now = now
            });
            LastPersonalizationSummaryForTests = recognition.personalization ?? TutorialPersonalizationSummary.Empty;
            if (hadActiveSeal && TryApplyCustomShapeFollowup(recognition, out var customFollowup))
            {
                if (recognition.baseResult?.spell?.success == true)
                {
                    recognitionService.RecordAcceptedResult(recognition, now);
                }

                return customFollowup;
            }

            var outcome = spellCasting.ProcessRecognitionResult(recognition, now);
            var processed = ApplySpellOutcome(outcome);
            if (outcome.kind == SpellCastOutcomeKind.BaseSucceeded || outcome.kind == SpellCastOutcomeKind.OverlaySucceeded)
            {
                recognitionService.RecordAcceptedResult(recognition, now);
            }

            return processed;
        }

        private bool TryApplyCustomShapeFollowup(StrokeRecognitionResult recognition, out ProcessedSpell processed)
        {
            processed = null;
            if (recognition.kind != StrokeRecognitionKind.Base ||
                recognition.baseResult?.spell?.isCustomShape != true)
            {
                return false;
            }

            var sealView = FindAttachableSeal(recognition.center);
            if (sealView == null)
            {
                CurrentAssistLevel = 1;
                LastHintText = "커스텀 도형은 먼저 만든 기본 문양의 빛나는 원 안에 얹어야 합니다.";
                endingReport.RecordHintShown(1);
                magicNote.Show(LastHintText);
                ShowBaseResultSummary(recognition.baseResult, "커스텀 부착 실패", LastHintText);
                LogBaseAttempt(recognition.baseResult, null, "custom_followup_detached");
                processed = new ProcessedSpell { baseResult = recognition.baseResult };
                return true;
            }

            processed = ApplyCustomShapeFollowup(sealView, recognition.baseResult, recognition.center);
            return true;
        }

        private ProcessedSpell ApplyCustomShapeFollowup(SealView sealView, BaseRecognitionResult result, Vector2 center)
        {
            var seal = sealView.seal;
            var customEffect = CustomSpellEffectCatalog.Resolve(seal.baseFamily, result.spell);
            if (!customEffect.IsValid)
            {
                CurrentAssistLevel = 1;
                LastHintText =
                    $"{SpellLabels.Korean(seal.baseFamily)} 문양 위에서 지금 도형 조합은 특별한 반응을 만들지 못했습니다.\n" +
                    "표식에 적힌 조합처럼 기본 문양을 먼저 만들고, 그 위에 맞는 커스텀 도형을 얹으세요.";
                endingReport.RecordHintShown(1);
                magicNote.Show(LastHintText);
                ShowBaseResultSummary(result, "커스텀 반응 실패", LastHintText);
                pulses.Add(new ParticlePulse(center, FamilyColor(seal.baseFamily), weak: true));
                LogBaseAttempt(result, seal, "custom_effect_unmatched");
                return new ProcessedSpell { baseResult = result };
            }

            var goalEffect = ApplyCustomSpellToGoals(seal, customEffect, center);
            var eventNote = ApplyCustomShapeEvent(result, seal, center);
            var note = $"{SpellLabels.Korean(seal.baseFamily)} 문양에 {result.spell.customShapeLabel}을 얹었습니다.\n{customEffect.note}";
            if (!string.IsNullOrWhiteSpace(goalEffect.note))
            {
                note += $"\n{goalEffect.note}";
            }

            if (!string.IsNullOrWhiteSpace(eventNote))
            {
                note += $"\n{eventNote}";
            }

            CurrentAssistLevel = 0;
            LastHintText = "";
            magicNote.Show(note);
            ShowBaseResultSummary(result, $"{customEffect.displayName} 반응", note);
            pulses.Add(new ParticlePulse(center, FamilyColor(seal.baseFamily)));
            LogBaseAttempt(result, seal, $"{goalEffect.worldEffect}|{customEffect.kind}");
            EvaluateFloorCompletion();
            ConsumeSeal(sealView);
            return new ProcessedSpell { baseResult = result };
        }

        private bool HasActiveSeal(float now)
        {
            return seals.Any(view => now <= view.seal.expiresAt);
        }

        private void MarkPostSealInputSeen(float now)
        {
            foreach (var seal in seals)
            {
                seal.MarkPostSealInputSeen(now);
            }
        }

        private BaseRecognitionIntent ResolveBaseIntent(Vector2 center)
        {
            var candidate = activeGoals
                .Where(goal => !goal.completed && (goal.requiredBase.HasValue || goal.comboBase.HasValue))
                .Select(goal =>
                {
                    var family = goal.requiredBase.HasValue ? goal.requiredBase.Value : goal.comboBase.Value;
                    var intentRadius = Mathf.Max(goal.radius * GoalIntentRadiusMultiplier, goal.radius + GoalIntentRadiusPadding);
                    var distance = Vector2.Distance(center, goal.position);
                    return new
                    {
                        goal,
                        family,
                        distance,
                        intentRadius,
                        strength = Mathf.Clamp01(1f - distance / Mathf.Max(intentRadius, 0.001f))
                    };
                })
                .Where(item => item.distance <= item.intentRadius)
                .OrderByDescending(item => item.strength)
                .ThenBy(item => item.distance)
                .FirstOrDefault();

            if (candidate == null)
            {
                return null;
            }

            return new BaseRecognitionIntent
            {
                family = candidate.family,
                goalId = candidate.goal.id,
                source = "near_goal_symbol",
                distance = candidate.distance,
                radius = candidate.intentRadius,
                strength = candidate.strength
            };
        }

        private ProcessedSpell ApplySubmittedSpellOutcome(SpellCastOutcome outcome)
        {
            trialCounter++;
            var now = Time.time;
            if (HasActiveSeal(now))
            {
                MarkPostSealInputSeen(now);
            }

            return ApplySpellOutcome(outcome);
        }

        private ProcessedSpell ApplySpellOutcome(SpellCastOutcome outcome)
        {
            return outcome.kind switch
            {
                SpellCastOutcomeKind.BaseFailed => ApplyBaseFailure(outcome),
                SpellCastOutcomeKind.BaseSucceeded => ApplyBaseSuccess(outcome),
                SpellCastOutcomeKind.OverlayFailed => ApplyOverlayFailure(outcome),
                SpellCastOutcomeKind.OverlayDuplicate => ApplyOverlayDuplicate(outcome),
                SpellCastOutcomeKind.OverlayStackFull => ApplyOverlayStackFull(outcome),
                SpellCastOutcomeKind.OverlaySucceeded => ApplyOverlaySuccess(outcome),
                SpellCastOutcomeKind.DetachedOverlay => ApplyDetachedOverlay(outcome),
                _ => throw new ArgumentOutOfRangeException(nameof(outcome.kind), outcome.kind, "Unhandled spell cast outcome.")
            };
        }

        private ProcessedSpell ApplyBaseFailure(SpellCastOutcome outcome)
        {
            var baseResult = outcome.baseResult;
            var feedbackFamily = baseResult.spell.recognizedFamily ?? baseResult.spell.targetFamily;
            var priorFailures = GetBaseFailureCount(feedbackFamily);
            var hintState = HintAssistance.ForAttempt(feedbackFamily, priorFailures, false, baseResult.spell);
            baseFailureCounts[feedbackFamily] = priorFailures + 1;
            CurrentAssistLevel = hintState.AssistLevelNumber;
            LastHintText = hintState.body;
            endingReport.RecordAssist(hintState);
            magicNote.Show(BuildBaseFailureNote(baseResult.spell, hintState));
            ShowBaseResultSummary(baseResult, "base 실패", resultSummary: hintState.body);
            pulses.Add(new ParticlePulse(outcome.center, new Color(0.75f, 0.75f, 0.82f), weak: true));
            LogBaseAttempt(baseResult, null, "failed", hintState);
            return new ProcessedSpell { baseResult = baseResult };
        }

        private ProcessedSpell ApplyBaseSuccess(SpellCastOutcome outcome)
        {
            var baseResult = outcome.baseResult;
            var seal = outcome.createdSeal;
            var priorFailures = GetBaseFailureCount(seal.baseFamily);
            var successHintState = HintAssistance.ForAttempt(seal.baseFamily, priorFailures, true, baseResult.spell);
            baseFailureCounts[seal.baseFamily] = 0;
            CurrentAssistLevel = successHintState.AssistLevelNumber;
            LastHintText = successHintState.assisted ? successHintState.body : "";
            var view = CreateSealView(seal);
            seals.Add(view);
            endingReport.RecordBase(seal.baseFamily, seal.quality, success: true, successHintState);
            var effect = ApplyBaseToGoals(baseResult, seal.baseFamily, outcome.center);
            var customEventNote = ApplyCustomShapeEvent(baseResult, seal, outcome.center);
            var eventEffect = string.IsNullOrWhiteSpace(customEventNote)
                ? effect
                : new GoalEffect($"{effect.note}\n{customEventNote}", $"{effect.worldEffect}|{baseResult.spell.customEventId}");
            magicNote.Show(BuildBaseSuccessNote(seal, eventEffect, successHintState));
            ShowBaseResultSummary(baseResult, "base 성공", resultSummary: eventEffect.note);
            pulses.Add(new ParticlePulse(outcome.center, FamilyColor(seal.baseFamily)));
            LogBaseAttempt(baseResult, seal, eventEffect.worldEffect, successHintState);
            EvaluateFloorCompletion();
            return new ProcessedSpell { baseResult = baseResult };
        }

        private ProcessedSpell ApplyOverlayFailure(SpellCastOutcome outcome)
        {
            var result = outcome.overlayResult;
            var seal = outcome.targetSeal;
            CurrentAssistLevel = 1;
            LastHintText = OverlayActionHint(result, seal);
            endingReport.RecordHintShown(1);
            magicNote.Show(BuildOverlayFailureNote(result, seal));
            ShowOverlayResultSummary(result, seal, "overlay 실패", LastHintText);
            pulses.Add(new ParticlePulse(outcome.center, new Color(0.75f, 0.75f, 0.82f), weak: true));
            LogOverlayAttempt(result, seal, outcome.center, outcome.strokeCount, "failed");
            return new ProcessedSpell { overlayResult = result };
        }

        private ProcessedSpell ApplyOverlayDuplicate(SpellCastOutcome outcome)
        {
            var result = outcome.overlayResult;
            var seal = outcome.targetSeal;
            var op = outcome.overlayOperator!.Value;
            CurrentAssistLevel = 1;
            LastHintText = "같은 장식 대신 아직 비어 있는 다른 장식을 seal 위에 그려 보세요.";
            endingReport.RecordHintShown(1);
            magicNote.Show($"{SpellLabels.Korean(op)} 장식은 이미 이 seal에 붙어 있습니다.");
            ShowOverlayResultSummary(result, seal, "overlay 중복", LastHintText);
            pulses.Add(new ParticlePulse(outcome.center, OverlayColor(op)));
            LogOverlayAttempt(result, seal, outcome.center, outcome.strokeCount, "duplicate_overlay");
            return new ProcessedSpell { overlayResult = result };
        }

        private ProcessedSpell ApplyOverlayStackFull(SpellCastOutcome outcome)
        {
            var result = outcome.overlayResult;
            var seal = outcome.targetSeal;
            var op = outcome.overlayOperator!.Value;
            CurrentAssistLevel = 1;
            LastHintText = "새 base seal을 만든 뒤 남은 장식을 붙여 보세요.";
            endingReport.RecordHintShown(1);
            magicNote.Show($"하나의 seal에는 overlay를 {SpellCastingService.MaxOverlayStack}개까지만 안정적으로 붙일 수 있습니다.");
            ShowOverlayResultSummary(result, seal, "overlay 초과", LastHintText);
            pulses.Add(new ParticlePulse(outcome.center, OverlayColor(op)));
            LogOverlayAttempt(result, seal, outcome.center, outcome.strokeCount, "overlay_stack_full");
            return new ProcessedSpell { overlayResult = result };
        }

        private ProcessedSpell ApplyOverlaySuccess(SpellCastOutcome outcome)
        {
            var result = outcome.overlayResult;
            var seal = outcome.targetSeal;
            var op = outcome.overlayOperator!.Value;
            var sealView = FindSealView(seal);
            if (sealView != null)
            {
                sealView.RefreshLabel(uiFont);
                sealView.AddOverlayMark(op);
            }
            endingReport.RecordOverlay(op);
            var effect = ApplyOverlayToGoals(seal, op, outcome.center);
            CurrentAssistLevel = 0;
            LastHintText = "";
            magicNote.Show(BuildOverlaySuccessNote(seal, op, effect));
            ShowOverlayResultSummary(result, seal, "overlay 성공", effect.note);
            LogOverlayAttempt(result, seal, outcome.center, outcome.strokeCount, effect.worldEffect);
            pulses.Add(new ParticlePulse(outcome.center, OverlayColor(op)));
            EvaluateFloorCompletion();
            return new ProcessedSpell { overlayResult = result };
        }

        private ProcessedSpell ApplyDetachedOverlay(SpellCastOutcome outcome)
        {
            var result = outcome.overlayResult;
            var seal = outcome.targetSeal;
            result.status = RecognitionStatus.Invalid;
            result.feedbackReason = BuildDetachedOverlayReason(result, seal, outcome.center);
            CurrentAssistLevel = 1;
            LastHintText = DetachedOverlayActionHint(seal);
            endingReport.RecordHintShown(1);
            magicNote.Show(BuildDetachedOverlayFailureNote(result, seal));
            ShowOverlayResultSummary(result, seal, "overlay 거리 오류", LastHintText);
            pulses.Add(new ParticlePulse(outcome.center, new Color(0.75f, 0.75f, 0.82f), weak: true));
            LogOverlayAttempt(result, seal, outcome.center, outcome.strokeCount, "detached_overlay");
            return new ProcessedSpell { overlayResult = result };
        }

        private void ShowBaseResultSummary(BaseRecognitionResult result, string title, string resultSummary)
        {
            UpdateResultPanelLayout();
            var family = result.spell.recognizedFamily.HasValue
                ? SpellLabels.Korean(result.spell.recognizedFamily.Value)
                : SpellLabels.Korean(result.spell.targetFamily);
            var label = result.spell.isCustomShape && !string.IsNullOrWhiteSpace(result.spell.customShapeLabel)
                ? $"{result.spell.customShapeLabel} ({family})"
                : family;
            var customLine = result.spell.isCustomShape
                ? $"커스텀 {Percent(result.spell.customScore)}  기본 유사 {Percent(result.spell.defaultSimilarityScore)}\n"
                : "";
            var eventLine = result.spell.isCustomShape && !string.IsNullOrWhiteSpace(result.spell.customEventLabel)
                ? $"이벤트 {result.spell.customEventLabel}  역할 {result.spell.customEventRole}\n"
                : "";
            resultText.text =
                $"{title}: {label}\n" +
                customLine +
                eventLine +
                $"판정 {StatusLabel(result.spell.status)}  신뢰 {Percent(result.spell.confidence)}  획 {result.bufferStrokeCount}\n" +
                $"{QualityLine(result.spell.quality)}\n" +
                $"해석: {ShortLine(QualityCoachLine(result.spell.quality), ResultLineLength(52, 42))}\n" +
                $"이유: {ShortLine(result.spell.feedbackReason, ResultLineLength(52, 42))}\n" +
                $"다음: {ShortLine(resultSummary, ResultLineLength(56, 46))}";
            resultPanel.gameObject.SetActive(true);
        }

        private void ShowOverlayResultSummary(OverlayRecognitionResult result, CompiledSeal seal, string title, string resultSummary)
        {
            UpdateResultPanelLayout();
            var op = result.recognizedOperator.HasValue ? SpellLabels.Korean(result.recognizedOperator.Value) : "미확정";
            resultText.text =
                $"{title}: {op}\n" +
                $"대상 seal: {ShortLine(seal.Label, ResultLineLength(30, 24))}\n" +
                $"판정 {StatusLabel(result.status)}  점수 {Percent(result.score)}  모양 {Percent(result.shapeConfidence)}\n" +
                $"크기 {result.scaleRatio:0.00}x  위치 {AnchorLabel(result.anchorZone)}\n" +
                $"다음: {ShortLine(resultSummary, ResultLineLength(56, 46))}";
            resultPanel.gameObject.SetActive(true);
        }

        private void UpdateResultPanelLayout()
        {
            if (resultPanel == null || resultText == null)
            {
                return;
            }

            resultPanelCompact = Screen.width > 0 && Screen.width < ResultPanelCompactScreenWidth;
            resultPanel.anchoredPosition = resultPanelCompact ? new Vector2(-20, -166) : new Vector2(-20, -20);
            resultPanel.sizeDelta = resultPanelCompact ? new Vector2(360, 188) : new Vector2(430, 206);
            resultText.fontSize = resultPanelCompact ? 12 : 13;
            resultText.rectTransform.anchoredPosition = new Vector2(14, -12);
            resultText.rectTransform.sizeDelta = resultPanelCompact ? new Vector2(332, 162) : new Vector2(402, 180);
        }

        private int ResultLineLength(int wideLength, int compactLength)
        {
            return resultPanelCompact ? compactLength : wideLength;
        }

        private SealView FindAttachableSeal(Vector2 center)
        {
            return seals
                .Where(seal => Time.time <= seal.seal.expiresAt)
                .OrderBy(seal => Vector2.Distance(center, seal.seal.worldCenter))
                .FirstOrDefault(seal => Vector2.Distance(center, seal.seal.worldCenter) <= SpellCastingService.AttachRadiusFor(seal.seal));
        }

        private SealView FindSealView(CompiledSeal seal)
        {
            return seals.FirstOrDefault(view => ReferenceEquals(view.seal, seal) || view.seal.sealId == seal.sealId);
        }

        private CompiledSeal FindActiveSealById(string sealId)
        {
            return seals
                .Select(view => view.seal)
                .FirstOrDefault(seal => seal.sealId == sealId && Time.time <= seal.expiresAt);
        }

        private static bool OverlayCenterIsAttachable(CompiledSeal seal, Vector2 center)
        {
            return Vector2.Distance(center, seal.worldCenter) <= SpellCastingService.AttachRadiusFor(seal);
        }

        private void ConsumeSeal(SealView sealView)
        {
            if (sealView == null)
            {
                return;
            }

            seals.Remove(sealView);
            if (sealView.root != null)
            {
                Destroy(sealView.root);
            }
        }

        private GoalEffect ApplyBaseToGoals(BaseRecognitionResult result, SpellFamily family, Vector2 center)
        {
            var resolution = floorGoals.ResolveBase(activeGoals, family, center, result?.spell?.isCustomShape == true);
            if (resolution.kind == GoalResolutionKind.Completed)
            {
                ActivateGoal(resolution.goal, resolution.worldEffect);
                return new GoalEffect(BuildGoalDiscoveryNote(resolution.goal), resolution.goal.id);
            }

            if (resolution.kind == GoalResolutionKind.CustomRequired)
            {
                return new GoalEffect(
                    $"{resolution.targetGoal.title} 표식은 커스텀 도형으로만 반응합니다.\n좌측 책장에서 레퍼런스 도형을 슬롯으로 들여온 뒤 같은 표식 근처에 다시 그리세요.",
                    "custom_required");
            }

            if (resolution.kind == GoalResolutionKind.CustomEffectRequired)
            {
                return new GoalEffect(
                    $"{resolution.targetGoal.title} 표식은 기본 문양만으로는 열리지 않습니다.\n" +
                    $"{resolution.targetGoal.RequirementLabel} 조합이 되도록 기본 문양 위에 커스텀 도형을 얹으세요.",
                    "custom_effect_required");
            }

            if (resolution.kind == GoalResolutionKind.BaseOffTarget)
            {
                return new GoalEffect(BuildBaseOffTargetGoalNote(family, resolution.targetGoal, resolution.distance, resolution.radius), resolution.worldEffect);
            }

            return new GoalEffect($"{SpellLabels.Korean(family)} seal이 바닥에 잠깐 고정되었습니다.", "seal_only");
        }

        private GoalEffect ApplyCustomSpellToGoals(
            CompiledSeal seal,
            CustomSpellEffectDefinition customEffect,
            Vector2 center)
        {
            var resolution = floorGoals.ResolveBase(activeGoals, seal.baseFamily, center, true, customEffect.kind);
            if (resolution.kind == GoalResolutionKind.Completed)
            {
                ActivateGoal(resolution.goal, customEffect.kind.ToString().ToLowerInvariant());
                return new GoalEffect(BuildGoalDiscoveryNote(resolution.goal), resolution.goal.id);
            }

            if (resolution.kind == GoalResolutionKind.CustomEffectRequired)
            {
                return new GoalEffect(
                    $"{resolution.targetGoal.title}에는 {resolution.targetGoal.RequirementLabel} 조합이 필요합니다.",
                    "custom_effect_mismatch");
            }

            if (resolution.kind == GoalResolutionKind.BaseOffTarget)
            {
                return new GoalEffect(BuildBaseOffTargetGoalNote(seal.baseFamily, resolution.targetGoal, resolution.distance, resolution.radius), resolution.worldEffect);
            }

            return new GoalEffect($"{customEffect.displayName} 반응이 만들어졌지만 아직 맞는 표식에 닿지 않았습니다.", "custom_effect_only");
        }

        private string ApplyCustomShapeEvent(BaseRecognitionResult result, CompiledSeal seal, Vector2 center)
        {
            var spell = result.spell;
            if (spell == null || !spell.isCustomShape || string.IsNullOrWhiteSpace(spell.customEventId))
            {
                return "";
            }

            var color = FamilyColor(seal.baseFamily);
            var eventKind = Enum.TryParse<CustomShapeEventKind>(spell.customEventKind, out var parsed)
                ? parsed
                : CustomShapeEventKind.None;
            var direction = spell.customEventDirection.sqrMagnitude > 0.0001f
                ? spell.customEventDirection.normalized
                : Vector2.right;
            var origin = spell.customEventOrigin.sqrMagnitude > 0.0001f ? spell.customEventOrigin : center;
            LastCustomShapeEventKindForTests = eventKind.ToString();
            LastCustomShapeEventLabelForTests = spell.customEventLabel ?? "";
            LastCustomShapeEventDirectionForTests = direction;

            switch (eventKind)
            {
                case CustomShapeEventKind.DirectionalProjectile:
                case CustomShapeEventKind.AttributeLaser:
                case CustomShapeEventKind.CurveProjectile:
                    RegisterCustomEventObject(CreateDirectionalEventSprite(spell.customEventLabel, origin, direction, color, eventKind));
                    pulses.Add(new ParticlePulse(origin + direction * 0.7f, color, scaleMultiplier: 0.72f, durationSeconds: 0.45f, sortingOrder: 31));
                    break;
                case CustomShapeEventKind.SlashDamage:
                    RegisterCustomEventObject(CreateDirectionalEventSprite(spell.customEventLabel, origin, direction, color, eventKind));
                    pulses.Add(new ParticlePulse(origin, color, scaleMultiplier: 0.62f, durationSeconds: 0.36f, sortingOrder: 31));
                    break;
                case CustomShapeEventKind.WallEntity:
                    RegisterCustomEventObject(CreateWorldSprite("Custom Shape Wall Event", origin, new Vector3(1.3f, 0.38f, 1f), color, Color.white, PixelSpriteKind.WallTrim, 23));
                    break;
                case CustomShapeEventKind.Barrier:
                    if (player != null)
                    {
                        defaultBarriers.Add(new CharacterBarrierView(player, $"custom-{seal.sealId}", color, 4.5f));
                        CustomShapeEventObjectCountForTests++;
                    }
                    break;
                case CustomShapeEventKind.Trap:
                    RegisterCustomEventObject(CreateWorldSprite("Custom Shape Trap Event", origin, Vector3.one * 0.76f, color, Color.white, PixelSpriteKind.Target, 23));
                    break;
                case CustomShapeEventKind.Stun:
                case CustomShapeEventKind.MagicAmplify:
                case CustomShapeEventKind.AttackBuff:
                case CustomShapeEventKind.MoveSpeedBuff:
                case CustomShapeEventKind.SpecialAttackBoost:
                case CustomShapeEventKind.BuffDispel:
                case CustomShapeEventKind.RandomBuffDispel:
                case CustomShapeEventKind.PiercingMark:
                case CustomShapeEventKind.GuardBuff:
                case CustomShapeEventKind.EventBlock:
                    RegisterCustomEventObject(CreateWorldSprite($"Custom Shape {eventKind} Event", origin, Vector3.one * 0.62f, color, Color.white, PixelSpriteKind.Pulse, 24));
                    break;
            }

            return string.IsNullOrWhiteSpace(spell.customEventLabel)
                ? ""
                : $"커스텀 이벤트: {spell.customEventLabel}";
        }

        private GameObject CreateDirectionalEventSprite(string eventLabel, Vector2 origin, Vector2 direction, Color color, CustomShapeEventKind eventKind)
        {
            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            var length = eventKind == CustomShapeEventKind.AttributeLaser ? 1.8f : 1.25f;
            var width = eventKind == CustomShapeEventKind.SlashDamage ? 0.20f : 0.16f;
            var name = string.IsNullOrWhiteSpace(eventLabel) ? $"Custom Shape {eventKind} Event" : $"Custom Shape {eventLabel} Event";
            var body = CreateWorldSprite(
                name,
                origin + direction * (length * 0.42f),
                new Vector3(width, length, 1f),
                color,
                Color.white,
                PixelSpriteKind.Rug,
                24);
            body.transform.rotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.up, direction));
            return body;
        }

        private void RegisterCustomEventObject(GameObject body)
        {
            if (body == null)
            {
                return;
            }

            floorObjects.Add(body);
            CustomShapeEventObjectCountForTests++;
        }

        private string BuildBaseOffTargetGoalNote(SpellFamily family, WorldStateGoal target, float distance, float radius)
        {
            return
                $"{SpellLabels.Korean(family)} 문양은 인식됐지만 {target.title} 표식 근처가 아닙니다.\n" +
                $"{target.title} 아래 라벨과 빛나는 표식 가까이에서 다시 그리세요. 현재 거리 {distance:0.0}, 목표 반경 {radius:0.0}.";
        }

        private GoalEffect ApplyOverlayToGoals(CompiledSeal seal, OverlayOperator op, Vector2 center)
        {
            var resolution = floorGoals.ResolveOverlay(activeGoals, seal, op, center);
            if (resolution.kind == GoalResolutionKind.Completed)
            {
                ActivateGoal(resolution.goal, resolution.worldEffect);
                return new GoalEffect(BuildGoalDiscoveryNote(resolution.goal), resolution.goal.id);
            }

            return new GoalEffect($"{seal.Label}: overlay stack이 빛났습니다.", "overlay_stack");
        }

        private int GetBaseFailureCount(SpellFamily family)
        {
            return baseFailureCounts.TryGetValue(family, out var count) ? count : 0;
        }

        private static string BuildBaseFailureNote(SpellResult result, HintState hintState)
        {
            return
                $"노트: {SpellLabels.Korean(hintState.family)} 문양이 아직 안정되지 않았습니다.\n" +
                $"{result.feedbackReason}\n" +
                $"{hintState.title}: {hintState.body}";
        }

        private static string BuildBaseSuccessNote(CompiledSeal seal, GoalEffect effect, HintState hintState)
        {
            var assisted = hintState.assisted ? " 이전 힌트가 이번 시전에 도움이 되었습니다." : "";
            return $"{SpellLabels.Korean(seal.baseFamily)} seal 성공.{assisted}\n{effect.note}";
        }

        private static string BuildOverlayFailureNote(OverlayRecognitionResult result, CompiledSeal seal)
        {
            return
                "노트: 장식이 seal에 안정적으로 붙지 않았습니다.\n" +
                $"{result.feedbackReason}\n" +
                $"다음: {OverlayActionHint(result, seal)}";
        }

        private static string BuildOverlaySuccessNote(CompiledSeal seal, OverlayOperator op, GoalEffect effect)
        {
            return $"{SpellLabels.Korean(op)} 장식이 seal 가장자리에 붙었습니다.\n현재 seal: {seal.Label}\n{effect.note}";
        }

        private static string BuildDetachedOverlayFailureNote(OverlayRecognitionResult result, CompiledSeal seal)
        {
            return
                "노트: 장식이 seal에 안정적으로 붙지 않았습니다.\n" +
                $"{result.feedbackReason}\n" +
                $"다음: {DetachedOverlayActionHint(seal)}";
        }

        private static string OverlayActionHint(OverlayRecognitionResult result, CompiledSeal seal)
        {
            if (result.recognizedOperator == OverlayOperator.MartialAxis && !seal.overlayStack.Contains(OverlayOperator.VoidCut))
            {
                return "먼저 같은 seal에 대각선 절단 장식을 붙인 뒤, 중심을 가르는 축을 다시 그리세요.";
            }

            if (result.scaleHint == OverlayScaleHint.TooSmall)
            {
                return "장식이 너무 작습니다. seal 중심을 기준으로 조금 더 크게 그려 보세요.";
            }

            if (result.scaleHint == OverlayScaleHint.TooLarge)
            {
                return "장식이 너무 큽니다. seal 안쪽에 들어오도록 작게 줄여 보세요.";
            }

            return AnchorHint(result.anchorZone);
        }

        private static string BuildDetachedOverlayReason(OverlayRecognitionResult result, CompiledSeal seal, Vector2 center)
        {
            var operatorName = result.recognizedOperator.HasValue ? SpellLabels.Korean(result.recognizedOperator.Value) : "장식";
            var distance = Vector2.Distance(center, seal.worldCenter);
            return $"{operatorName} 모양은 보였지만 seal에서 너무 멀어 붙지 않았습니다. 현재 거리 {distance:0.0}, seal 중심 가까이에서 다시 그리세요.";
        }

        private static string DetachedOverlayActionHint(CompiledSeal seal)
        {
            return $"{SpellLabels.Korean(seal.baseFamily)} seal의 빛나는 원 안쪽이나 가장자리 바로 옆에 장식을 다시 그리세요.";
        }

        private static string AnchorHint(string anchorZone)
        {
            return anchorZone switch
            {
                "upper_right" => "seal의 오른쪽 위 가장자리에서 짧고 또렷하게 다시 그려 보세요.",
                "right" => "seal의 오른쪽 가장자리 옆에 붙이듯 다시 그려 보세요.",
                "lower_right" => "seal의 오른쪽 아래 가장자리에서 다시 그려 보세요.",
                "upper" => "seal의 위쪽 가장자리 가까이에서 다시 그려 보세요.",
                "left" => "seal의 왼쪽 가장자리 가까이에서 다시 그려 보세요.",
                _ => "seal 중심 가까이에 작게, 한 가지 장식만 다시 그려 보세요."
            };
        }

        private void ActivateGoal(WorldStateGoal goal, string effect)
        {
            goal.completed = true;
            if (goal.renderer != null)
            {
                goal.renderer.sprite = PixelArtFactory.CreateSprite($"{goal.title} Active", Color.white, goal.color, goal.kind);
                goal.renderer.sharedMaterial = PixelMaterialProvider.SpriteMaterial;
            }
            if (goal.body != null)
            {
                goal.body.transform.localScale *= 1.15f;
            }
            if (goal.label != null)
            {
                goal.label.text = $"완료: {goal.title}";
                goal.label.color = Color.Lerp(goal.color, Color.white, 0.6f);
                goal.label.fontStyle = FontStyle.Bold;
            }
            ApplyGoalReaction(goal);
            endingReport.RecordDiscovery(goal.id, effect);
            pulses.Add(new ParticlePulse(goal.position, goal.color));
        }

        private void ApplyGoalReaction(WorldStateGoal goal)
        {
            switch (goal.reactionKind)
            {
                case WorldReactionKind.BridgeFlow:
                    CreateBridgeReaction(goal);
                    break;
                case WorldReactionKind.HazardStabilizer:
                    StabilizeHazardReaction(goal);
                    break;
                case WorldReactionKind.LivingBridge:
                    CreateLivingBridgeReaction(goal);
                    break;
                case WorldReactionKind.FreezeRiver:
                    CreateFrozenRiverReaction(goal);
                    break;
                case WorldReactionKind.EarthStairs:
                    CreateEarthStairsReaction(goal);
                    break;
                case WorldReactionKind.WindPlatform:
                    CreateWindPlatformReaction(goal);
                    break;
                case WorldReactionKind.CombatHit:
                    CreateCombatHitReaction(goal);
                    break;
            }
        }

        private void CreateBridgeReaction(WorldStateGoal goal)
        {
            var direction = goal.position.sqrMagnitude < 0.01f ? Vector2.up : goal.position.normalized;
            var midpoint = goal.position * 0.5f;
            var span = CreateWorldSprite(
                $"Flow Bridge {goal.id}",
                midpoint,
                new Vector3(0.42f, Mathf.Max(goal.position.magnitude, 1f), 1f),
                new Color(0.10f, 0.36f, 0.46f),
                Color.Lerp(goal.color, Color.white, 0.35f),
                PixelSpriteKind.Rug,
                -3);
            span.transform.rotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.up, direction));
            floorObjects.Add(span);

            var node = CreateWorldSprite(
                $"Flow Node {goal.id}",
                goal.position,
                Vector3.one * 0.72f,
                Color.Lerp(goal.color, Color.white, 0.15f),
                Color.white,
                PixelSpriteKind.Pulse,
                5);
            floorObjects.Add(node);
        }

        private void StabilizeHazardReaction(WorldStateGoal goal)
        {
            safePosition = goal.position;
            var orderedHazards = activeHazards
                .OrderBy(hazard => Vector2.Distance(hazard.position, goal.position))
                .ToList();

            for (var index = 0; index < orderedHazards.Count; index++)
            {
                var hazard = orderedHazards[index];
                hazard.Stabilize(index == 0 ? 0.74f : 0.90f);
                pulses.Add(new ParticlePulse(hazard.position, Color.Lerp(hazard.color, goal.color, 0.35f), weak: index > 0));
            }

            var pin = CreateWorldSprite(
                $"Stability Pin {goal.id}",
                goal.position,
                Vector3.one * 0.68f,
                goal.color,
                Color.white,
                PixelSpriteKind.Target,
                6);
            floorObjects.Add(pin);
        }

        private void CreateLivingBridgeReaction(WorldStateGoal goal)
        {
            CreateStagePath(goal, "생명 다리", new Vector2(0f, -2.45f), new Vector2(5.8f, 0.46f), new Color(0.16f, 0.52f, 0.28f), PixelSpriteKind.Rug);
            CreateStageNode(goal, goal.position + new Vector2(0.62f, 0.12f), PixelSpriteKind.LifeRune);
        }

        private void CreateFrozenRiverReaction(WorldStateGoal goal)
        {
            CreateStagePath(goal, "얼음길", new Vector2(0f, -0.62f), new Vector2(5.9f, 0.50f), new Color(0.48f, 0.84f, 1f), PixelSpriteKind.FloorTile);
            CreateStageNode(goal, goal.position + new Vector2(0.58f, 0.12f), PixelSpriteKind.WaterRune);
        }

        private void CreateEarthStairsReaction(WorldStateGoal goal)
        {
            for (var index = 0; index < 5; index++)
            {
                var step = CreateWorldSprite(
                    $"Earth Step {index + 1}",
                    new Vector2(-1.0f + index * 0.52f, 0.86f + index * 0.10f),
                    Vector3.one,
                    new Color(0.58f, 0.42f, 0.24f),
                    Color.Lerp(goal.color, Color.white, 0.35f),
                    PixelSpriteKind.WallTrim,
                    -2,
                    true,
                    new Vector2(0.72f, 0.22f));
                floorObjects.Add(step);
            }

            CreateStageNode(goal, goal.position + new Vector2(0.48f, 0.1f), PixelSpriteKind.EarthRune);
        }

        private void CreateWindPlatformReaction(WorldStateGoal goal)
        {
            CreateStagePath(goal, "바람 발판", new Vector2(0f, 2.78f), new Vector2(3.8f, 0.38f), new Color(0.54f, 0.80f, 0.92f), PixelSpriteKind.Rug);
            CreateStageNode(goal, goal.position + new Vector2(0.50f, 0.08f), PixelSpriteKind.WindRune);
        }

        private void CreateStagePath(WorldStateGoal goal, string title, Vector2 position, Vector2 size, Color color, PixelSpriteKind kind)
        {
            var path = CreateWorldSprite(
                title,
                position,
                Vector3.one,
                color,
                Color.Lerp(color, Color.white, 0.42f),
                kind,
                -1,
                true,
                size);
            floorObjects.Add(path);
            pulses.Add(new ParticlePulse(position, goal.color, scaleMultiplier: 1.4f, durationSeconds: 1.1f, sortingOrder: 8));
        }

        private void CreateStageNode(WorldStateGoal goal, Vector2 position, PixelSpriteKind kind)
        {
            var node = CreateWorldSprite(
                $"Stage Node {goal.id}",
                position,
                Vector3.one * 0.52f,
                goal.color,
                Color.white,
                kind,
                6);
            floorObjects.Add(node);
        }

        private void CreateCombatHitReaction(WorldStateGoal goal)
        {
            var impact = goal.requiredCustomSpell.HasValue
                ? CustomSpellEffectCatalog.For(goal.requiredCustomSpell.Value).impact
                : 20;
            var label = impact > 0 ? $"-{impact}" : "효과";
            ShowDamagePopup(goal.position + new Vector2(0f, 0.56f), label, goal.color);
            pulses.Add(new ParticlePulse(goal.position, goal.color, scaleMultiplier: 0.9f, durationSeconds: 0.55f, sortingOrder: 20));
        }

        private void EvaluateFloorCompletion()
        {
            if (HasEndingReport)
            {
                return;
            }

            if (!IsFinalFloor)
            {
                if (!activeGoals.All(goal => goal.completed) || pendingAdvanceAt > 0f)
                {
                    return;
                }

                magicNote.Show(BuildFloorCompletionNote());
                pendingAdvanceAt = Time.time + StandardFloorAdvanceDelaySeconds;
                return;
            }

            var completed = activeGoals.Count(goal => goal.completed);
            if (completed < FinalFloorPassingGoalCount)
            {
                return;
            }

            var fullyCompleted = completed >= activeGoals.Count;
            if (fullyCompleted && !finalTrueEnding)
            {
                finalTrueEnding = true;
                magicNote.Show(BuildFloorCompletionNote());
                pendingAdvanceAt = Time.time + FinalFloorCompleteReportDelaySeconds;
                return;
            }

            if (pendingAdvanceAt > 0f)
            {
                return;
            }

            magicNote.Show(BuildFloorCompletionNote());
            pendingAdvanceAt = Time.time + FinalFloorPassReportDelaySeconds;
        }

        private string BuildFloorCompletionNote()
        {
            if (!IsFinalFloor)
            {
                return floorController.Current.completeNote;
            }

            CelebrateFinalCompletion(finalTrueEnding);
            if (finalTrueEnding)
            {
                return
                    "성좌심 완전 복구.\n" +
                    "여섯 요구치가 하나의 마법진으로 닫혔고, 탑이 당신의 문양 언어를 완전히 기억합니다.";
            }

            return
                "입학 시험 통과.\n" +
                "다섯 요구치가 마법진을 다시 일으켰습니다. 남은 조각까지 채우면 성좌심이 완전히 닫힙니다.";
        }

        private void CelebrateFinalCompletion(bool trueEnding)
        {
            if (finalCompletionCelebrated)
            {
                if (trueEnding)
                {
                    pulses.Add(new ParticlePulse(Vector2.zero, new Color(1f, 1f, 0.82f), scaleMultiplier: 2.55f, durationSeconds: 1.95f, sortingOrder: 36));
                }
                return;
            }

            finalCompletionCelebrated = true;
            pulses.Add(new ParticlePulse(Vector2.zero, new Color(1f, 0.92f, 0.45f), scaleMultiplier: 2.15f, durationSeconds: 1.65f, sortingOrder: 34));
            pulses.Add(new ParticlePulse(Vector2.zero, new Color(0.48f, 0.84f, 1f), scaleMultiplier: 1.55f, durationSeconds: 1.25f, sortingOrder: 33));
            foreach (var goal in activeGoals)
            {
                if (goal.body != null)
                {
                    goal.body.transform.localScale *= 1.08f;
                }
                pulses.Add(new ParticlePulse(goal.position, Color.Lerp(goal.color, Color.white, 0.25f), scaleMultiplier: 1.28f, durationSeconds: 1.2f, sortingOrder: 32));
            }

            if (trueEnding)
            {
                pulses.Add(new ParticlePulse(Vector2.zero, new Color(1f, 1f, 0.82f), scaleMultiplier: 2.55f, durationSeconds: 1.95f, sortingOrder: 36));
            }
        }

        private void TickFloorAdvance()
        {
            if (pendingAdvanceAt < 0f || Time.time < pendingAdvanceAt)
            {
                return;
            }

            pendingAdvanceAt = -1f;
            if (floorController.CurrentFloorIndex < floorController.FloorCount - 1)
            {
                LoadFloor(floorController.CurrentFloorIndex + 1);
                return;
            }

            ShowEndingReport();
        }

        private void TickPlayer()
        {
            if (HasEndingReport)
            {
                return;
            }

            var input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }

            velocity = Vector2.Lerp(velocity, input * 4.2f, Time.deltaTime * 12f);
            player.position += (Vector3)(velocity * Time.deltaTime);
            player.position = new Vector3(Mathf.Clamp(player.position.x, -7.35f, 7.35f), Mathf.Clamp(player.position.y, -4.25f, 4.25f), 0f);
        }

        private void TickStageGates()
        {
            if (floorController.Current.number != 3 || activeStageGates.Count == 0)
            {
                return;
            }

            foreach (var gate in activeStageGates)
            {
                var goal = activeGoals.FirstOrDefault(item => item.id == gate.requiredGoalId);
                if (goal?.completed == true)
                {
                    gate.Open();
                    continue;
                }

                if (!gate.Contains(player.position))
                {
                    continue;
                }

                player.position = gate.resetPosition;
                velocity = Vector2.zero;
                magicNote.Show(gate.lockedNote);
                pulses.Add(new ParticlePulse(gate.center, new Color(0.72f, 0.88f, 1f), weak: true));
                return;
            }
        }

        private void TickDamagePopups()
        {
            for (var index = damagePopups.Count - 1; index >= 0; index--)
            {
                var popup = damagePopups[index];
                if (!popup.Tick(Time.deltaTime))
                {
                    popup.Destroy();
                    damagePopups.RemoveAt(index);
                }
            }
        }

        private void TickHazards()
        {
            if (floorController.Current.number != 4)
            {
                return;
            }

            foreach (var hazard in activeHazards)
            {
                hazard.Tick(Time.time);
                if (Vector2.Distance(player.position, hazard.position) <= hazard.radius * 0.58f)
                {
                    player.position = safePosition;
                    velocity = Vector2.zero;
                    magicNote.Show("균열이 몸을 밀어냈습니다. 가까운 안전 지점에서 다시 시작합니다.");
                    pulses.Add(new ParticlePulse(hazard.position, hazard.color, weak: true));
                    return;
                }
            }
        }

        private void TickSeals()
        {
            for (var index = seals.Count - 1; index >= 0; index--)
            {
                var seal = seals[index];
                var remaining = seal.seal.expiresAt - Time.time;
                if (remaining <= 0f)
                {
                    Destroy(seal.root);
                    seals.RemoveAt(index);
                    continue;
                }

                seal.Tick(Time.time, remaining / Mathf.Max(seal.seal.expiresAt - seal.seal.createdAt, 0.1f));
                if (seal.TryTriggerDefaultFallback(Time.time))
                {
                    TriggerDefaultSealFallback(seal);
                }
            }
        }

        private void TickPulses()
        {
            for (var index = pulses.Count - 1; index >= 0; index--)
            {
                var pulse = pulses[index];
                pulse.age += Time.deltaTime;
                if (pulse.body == null)
                {
                    pulse.body = CreateWorldSprite("Spell Pulse", pulse.position, Vector3.one * (pulse.weak ? 0.22f : 0.35f) * pulse.scaleMultiplier, pulse.color, Color.white, PixelSpriteKind.Pulse, pulse.sortingOrder);
                }

                var duration = pulse.durationSeconds > 0f ? pulse.durationSeconds : pulse.weak ? 0.7f : 0.95f;
                var t = pulse.age / duration;
                pulse.body.transform.localScale = Vector3.one * Mathf.Lerp(pulse.weak ? 0.35f : 0.45f, pulse.weak ? 1.4f : 2.5f, t) * pulse.scaleMultiplier;
                var renderer = pulse.body.GetComponent<SpriteRenderer>();
                renderer.sharedMaterial = PixelMaterialProvider.SpriteMaterial;
                renderer.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.8f, 0f, t));
                if (t >= 1f)
                {
                    Destroy(pulse.body);
                    pulses.RemoveAt(index);
                }
            }
        }

        private void TickDefaultBarriers()
        {
            for (var index = defaultBarriers.Count - 1; index >= 0; index--)
            {
                var barrier = defaultBarriers[index];
                if (!barrier.Tick(Time.deltaTime, Time.time))
                {
                    barrier.Destroy();
                    defaultBarriers.RemoveAt(index);
                }
            }
        }

        private void TriggerDefaultSealFallback(SealView seal)
        {
            var color = FamilyColor(seal.seal.baseFamily);
            var duration = Mathf.Max(1.2f, seal.seal.expiresAt - Time.time);
            defaultBarriers.Add(new CharacterBarrierView(player, seal.seal.sealId, color, duration));
            pulses.Add(new ParticlePulse(player.position, color, weak: true, scaleMultiplier: 1.05f, durationSeconds: 0.8f, sortingOrder: 31));
            magicNote.Show($"{SpellLabels.Korean(seal.seal.baseFamily)} seal이 기본 보호막으로 안정화되었습니다.");
        }

        private void UpdateHud()
        {
            if (HasEndingReport)
            {
                return;
            }

            UpdateResultPanelLayout();
            var floor = floorController.Current;
            if (finalCompletionCelebrated && IsFinalFloor && pendingAdvanceAt > 0f)
            {
                var completedFinal = activeGoals.Count(goal => goal.completed);
                hudTitle.text = finalTrueEnding ? "성좌심 완전 복구" : "입학 시험 통과";
                hudCopy.text = finalTrueEnding
                    ? "여섯 요구치가 하나의 마법진으로 닫혔습니다.\n곧 완전 복구 보고서가 열립니다."
                    : "다섯 요구치로 입학 마법진이 다시 섰습니다.\n곧 통과 보고서가 열립니다.";
                floorProgress.text = $"탑 진행 {floorController.CurrentFloorNumber}/{floorController.FloorCount}   목표 {completedFinal}/{activeGoals.Count}   final seal";
                notePanel.gameObject.SetActive(magicNote.Visible);
                noteText.text = magicNote.Text;
                return;
            }

            hudTitle.text = $"층 {floor.number}: {floor.title}";
            var completed = activeGoals.Count(goal => goal.completed);
            if (IsFinalFloor)
            {
                hudCopy.text = $"{floor.objective}\n남은 요구: {BuildRemainingFinalGoalSummary()}";
                floorProgress.text = $"탑 진행 {floorController.CurrentFloorNumber}/{floorController.FloorCount}   목표 {completed}/{activeGoals.Count}   다음 {BuildNextFinalGoalShortLabel()}";
            }
            else
            {
                if (floor.number == 1)
                {
                    hudCopy.text = $"{floor.objective}\n{BuildFirstFloorGoalSummary()}\n표식 아래 라벨을 보고 목표 근처에 우클릭 hold로 그리세요. Esc/Backspace 취소.";
                }
                else if (floor.number == CustomReferenceFloorNumber)
                {
                    hudCopy.text = $"{floor.objective}\n좌측 책장 접근 -> 레퍼런스 보기 -> 들여오기 후 표식 근처에 커스텀 도형을 그리세요.";
                }
                else
                {
                    hudCopy.text = $"{floor.objective}\nWASD 이동 / 우클릭 hold로 바닥에 직접 문양을 그리세요. Esc/Backspace 취소.";
                }
                floorProgress.text = $"탑 진행 {floorController.CurrentFloorNumber}/{floorController.FloorCount}   목표 {completed}/{activeGoals.Count}   seal {seals.Count}";
            }
            notePanel.gameObject.SetActive(magicNote.Visible);
            noteText.text = magicNote.Text;
        }

        private string BuildFloorEntryNote(FloorDefinition floor)
        {
            if (floor.number == 1)
            {
                return $"{floor.entryNote}\n{BuildFirstFloorGoalHint()}";
            }

            if (floor.number == CustomReferenceFloorNumber)
            {
                return $"{floor.entryNote}\n좌측 책장 근처에서 말풍선의 보기 버튼을 누르면 base별 커스텀 도형을 슬롯에 들여올 수 있습니다.";
            }

            return IsFinalFloor ? $"{floor.entryNote}\n{BuildNextFinalGoalHint()}" : floor.entryNote;
        }

        private string BuildFirstFloorGoalHint()
        {
            return
                "1층 목표: 표식 아래에 적힌 문양을 그 표식 근처에 그리세요.\n" +
                "물은 닫힌 원, 바람은 위/가운데/아래 3개의 평행선입니다.";
        }

        private string BuildFirstFloorGoalSummary()
        {
            var remaining = activeGoals.Where(goal => !goal.completed).ToList();
            if (remaining.Count == 0)
            {
                return "남은 표식: 모두 완료";
            }

            var shown = remaining.Take(3).Select(goal => $"{goal.title}({goal.RequirementLabel})");
            var suffix = remaining.Count > 3 ? $" 외 {remaining.Count - 3}" : "";
            return "남은 표식: " + string.Join(" / ", shown) + suffix;
        }

        private string BuildGoalDiscoveryNote(WorldStateGoal goal)
        {
            if (!IsFinalFloor)
            {
                return goal.discoveryNote;
            }

            return $"{goal.discoveryNote}\n{BuildNextFinalGoalHint()}";
        }

        private string BuildRemainingFinalGoalSummary()
        {
            var remaining = activeGoals.Where(goal => !goal.completed).ToList();
            if (remaining.Count == 0)
            {
                return "모든 요구치 완료";
            }

            var shown = remaining.Take(2).Select(goal => $"{goal.title}({goal.RequirementLabel})");
            var suffix = remaining.Count > 2 ? $" 외 {remaining.Count - 2}" : "";
            return string.Join(" / ", shown) + suffix;
        }

        private string BuildNextFinalGoalHint()
        {
            var next = activeGoals.FirstOrDefault(goal => !goal.completed);
            if (next == null)
            {
                return "다음 목표: 모든 요구치가 채워졌습니다.";
            }

            return $"다음 목표: {next.title} - {next.RequirementLabel}을 목표 표식 근처에서 완성하세요.";
        }

        private string BuildNextFinalGoalShortLabel()
        {
            var next = activeGoals.FirstOrDefault(goal => !goal.completed);
            return next == null ? "완료" : $"{next.title}({next.RequirementLabel})";
        }

        private void ShowEndingReport()
        {
            reportPanel.gameObject.SetActive(true);
            notePanel.gameObject.SetActive(false);
            resultPanel.gameObject.SetActive(false);
            floorSkipButton.gameObject.SetActive(false);
            var completedFinalGoals = IsFinalFloor ? activeGoals.Count(goal => goal.completed) : activeGoals.Count;
            hudTitle.text = finalTrueEnding ? "입학 시험 완전 통과" : "입학 시험 통과";
            hudCopy.text = finalTrueEnding ? "입학 마법진이 완전히 밝아졌습니다." : "입학 마법진이 다시 밝아졌습니다.";
            logger.LogSurvey(new SurveyLog
            {
                sessionId = sessionId,
                clarity = 5,
                fairness = 5,
                feedbackHelpfulness = 5,
                controlFeeling = 5,
                immersion = 5,
                comment = "auto ending report",
                completedTrials = endingReport.DiscoveryCount,
                totalAttempts = trialCounter
            });
            reportText.text = endingReport.BuildText(trialCounter, OutputDirectory, finalTrueEnding, completedFinalGoals, activeGoals.Count);
        }

        private SealView CreateSealView(CompiledSeal seal)
        {
            var root = CreateWorldSprite($"Seal {seal.sealId}", seal.worldCenter, Vector3.one * Mathf.Max(seal.worldScale * 1.08f, 0.9f), FamilyColor(seal.baseFamily), Color.white, PixelSpriteKind.RuneCircle, 18);
            var guide = CreateOverlayAttachGuide(seal, root.transform);
            guide.SetActive(false);
            var labelObject = new GameObject("Rune Label");
            labelObject.transform.SetParent(root.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0.78f, 0f);
            var canvasObject = new GameObject("Rune Label Canvas");
            canvasObject.transform.SetParent(labelObject.transform, false);
            var worldCanvas = canvasObject.AddComponent<Canvas>();
            worldCanvas.renderMode = RenderMode.WorldSpace;
            worldCanvas.sortingOrder = 40;
            var rect = canvasObject.GetComponent<RectTransform>() ?? canvasObject.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(2.7f, 0.45f);
            canvasObject.transform.localScale = Vector3.one * 0.012f;
            var textObject = new GameObject("Text");
            textObject.transform.SetParent(canvasObject.transform, false);
            var textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textObject.AddComponent<Text>();
            text.font = uiFont;
            text.fontSize = 26;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = seal.Label;
            var defaultFallbackAt = string.IsNullOrWhiteSpace(seal.customShapeId)
                ? seal.createdAt + DefaultSealFallbackDelaySeconds
                : float.PositiveInfinity;
            return new SealView(root, seal, text, guide, SpellCastingService.AttachRadiusFor(seal), defaultFallbackAt);
        }

        private GameObject CreateOverlayAttachGuide(CompiledSeal seal, Transform parent)
        {
            var guide = new GameObject("Overlay Attach Guide");
            guide.transform.SetParent(parent, false);
            guide.transform.localPosition = Vector3.zero;
            guide.transform.localScale = Vector3.one * SpellCastingService.AttachRadiusFor(seal);
            var renderer = guide.AddComponent<SpriteRenderer>();
            var color = Color.Lerp(FamilyColor(seal.baseFamily), Color.white, 0.18f);
            color.a = 0.34f;
            renderer.sprite = PixelArtFactory.CreateSprite($"Attach Guide {seal.sealId}", color, Color.white, PixelSpriteKind.Pulse);
            renderer.sharedMaterial = PixelMaterialProvider.SpriteMaterial;
            renderer.color = color;
            renderer.sortingOrder = 17;
            return guide;
        }

        private void LogBaseAttempt(BaseRecognitionResult result, CompiledSeal seal, string worldEffect, HintState hintState = null)
        {
            var success = result.spell.status == RecognitionStatus.Recognized;
            logger.LogAttempt(new AttemptLog
            {
                sessionId = sessionId,
                trialId = trialCounter.ToString(CultureInfo.InvariantCulture),
                targetFamily = "",
                recognizedFamily = result.spell.RecognizedFamilyText,
                phase = SpellPhase.Base.ToString(),
                baseFamily = result.spell.RecognizedFamilyText,
                overlayStack = "",
                sealId = seal?.sealId ?? "",
                floorId = floorController.Current.number.ToString(CultureInfo.InvariantCulture),
                targetObject = worldEffect,
                worldEffect = worldEffect,
                customShapeId = result.spell.customShapeId ?? "",
                customShapeLabel = result.spell.customShapeLabel ?? "",
                customShapeToken = result.spell.customShapeToken ?? "",
                mappedFamily = result.spell.mappedFamily.HasValue ? SpellLabels.English(result.spell.mappedFamily.Value) : "",
                customEventId = result.spell.customEventId ?? "",
                customEventLabel = result.spell.customEventLabel ?? "",
                customEventKind = result.spell.customEventKind ?? "",
                customEventRole = result.spell.customEventRole ?? "",
                customEventUsesDirection = result.spell.customEventUsesDirection,
                customEventOperatorOnly = result.spell.customEventOperatorOnly,
                customEventBlocks = result.spell.customEventBlocks,
                customEventBlocked = result.spell.customEventBlocked,
                customEventBlockedBy = result.spell.customEventBlockedBy ?? "",
                customEventOriginX = result.spell.customEventOrigin.x,
                customEventOriginY = result.spell.customEventOrigin.y,
                customEventDirectionX = result.spell.customEventDirection.x,
                customEventDirectionY = result.spell.customEventDirection.y,
                status = result.spell.status.ToString(),
                confidence = result.spell.confidence,
                customScore = result.spell.customScore,
                defaultSimilarityScore = result.spell.defaultSimilarityScore,
                intentFamily = result.spell.intentFamily.HasValue ? SpellLabels.English(result.spell.intentFamily.Value) : "",
                intentGoalId = result.spell.intentGoalId ?? "",
                intentSource = result.spell.intentSource ?? "",
                intentStrength = result.spell.intentStrength,
                intentSimilarityScore = result.spell.intentSimilarityScore,
                intentWeakConsiderationApplied = result.spell.intentWeakConsiderationApplied,
                intentTutorialCaptureCount = result.intent?.tutorialCaptureCount ?? 0,
                intentStrongConsiderationEnabled = result.intent?.strongConsiderationEnabled ?? false,
                preIntentFamily = result.spell.preIntentFamily.HasValue ? SpellLabels.English(result.spell.preIntentFamily.Value) : "",
                preIntentConfidence = result.spell.preIntentConfidence,
                intentStrongConsiderationApplied = result.spell.intentStrongConsiderationApplied,
                intentScoreLift = result.spell.intentScoreLift,
                closure = result.spell.quality.closure,
                smoothness = result.spell.quality.smoothness,
                tempo = result.spell.quality.tempo,
                stability = result.spell.quality.stability,
                rotationBias = result.spell.quality.rotationBias,
                worldX = result.center.x,
                worldY = result.center.y,
                bufferStrokeCount = result.bufferStrokeCount,
                attemptIndex = trialCounter,
                elapsedMs = Mathf.RoundToInt((Time.time - floorStartedAt) * 1000f),
                feedbackViewed = true,
                success = success,
                hintShown = hintState?.hintShown ?? !success,
                assistLevel = hintState?.AssistLevelNumber ?? (success ? 0 : 1),
                assisted = hintState?.assisted ?? false
            });
        }

        private void LogOverlayAttempt(OverlayRecognitionResult result, CompiledSeal seal, Vector2 center, int strokeCount, string worldEffect)
        {
            logger.LogAttempt(new AttemptLog
            {
                sessionId = sessionId,
                trialId = trialCounter.ToString(CultureInfo.InvariantCulture),
                targetFamily = "",
                recognizedFamily = result.OperatorText,
                phase = SpellPhase.Overlay.ToString(),
                baseFamily = SpellLabels.English(seal.baseFamily),
                overlayStack = string.Join(">", seal.overlayStack.Select(SpellLabels.English)),
                sealId = seal.sealId,
                floorId = floorController.Current.number.ToString(CultureInfo.InvariantCulture),
                targetObject = worldEffect,
                worldEffect = worldEffect,
                status = result.status.ToString(),
                confidence = result.score,
                closure = 0f,
                smoothness = result.shapeConfidence,
                tempo = 0f,
                stability = 0f,
                rotationBias = result.scaleRatio,
                worldX = center.x,
                worldY = center.y,
                bufferStrokeCount = strokeCount,
                attemptIndex = trialCounter,
                elapsedMs = Mathf.RoundToInt((Time.time - floorStartedAt) * 1000f),
                feedbackViewed = true,
                success = result.success,
                hintShown = !result.success,
                assistLevel = result.success ? 0 : CurrentAssistLevel,
                assisted = false
            });
        }

        private void ClearFloorObjects()
        {
            foreach (var body in floorObjects)
            {
                if (body != null)
                {
                    Destroy(body);
                }
            }
            floorObjects.Clear();
            foreach (var seal in seals)
            {
                if (seal.root != null)
                {
                    Destroy(seal.root);
                }
            }
            seals.Clear();
            foreach (var barrier in defaultBarriers)
            {
                barrier.Destroy();
            }
            defaultBarriers.Clear();
            foreach (var popup in damagePopups)
            {
                popup.Destroy();
            }
            damagePopups.Clear();
            activeStageGates.Clear();
            CustomShapeEventObjectCountForTests = 0;
            LastDamagePopupTextForTests = "";
            LastCustomShapeEventKindForTests = "";
            LastCustomShapeEventLabelForTests = "";
            LastCustomShapeEventDirectionForTests = Vector2.right;
        }

        private void ShowDamagePopup(Vector2 position, string text, Color color)
        {
            var popup = new DamagePopupView(position, text, color, uiFont);
            damagePopups.Add(popup);
            LastDamagePopupTextForTests = text;
        }

        private GameObject CreateWorldSprite(string name, Vector2 position, Vector3 scale, Color primary, Color secondary, PixelSpriteKind kind, int sortingOrder, bool tiled = false, Vector2 tiledSize = default, Transform parent = null)
        {
            var body = new GameObject(name);
            body.transform.SetParent(parent, true);
            body.transform.position = position;
            body.transform.localScale = scale;
            body.AddComponent<SpriteRenderer>();
            var pixelSprite = body.AddComponent<PixelSpriteView>();
            pixelSprite.kind = kind;
            pixelSprite.primary = primary;
            pixelSprite.secondary = secondary;
            pixelSprite.sortingOrder = sortingOrder;
            pixelSprite.tiled = tiled;
            pixelSprite.tiledSize = tiledSize == default ? Vector2.one : tiledSize;
            pixelSprite.Apply();
            return body;
        }

        private Text CreateGoalLabel(WorldStateGoal goal, Transform parent)
        {
            var labelSize = new Vector2(220f, 64f);
            var canvasObject = new GameObject($"{goal.title} Goal Label");
            canvasObject.transform.SetParent(parent, false);
            canvasObject.transform.position = goal.position + new Vector2(0f, -0.86f);
            var worldCanvas = canvasObject.AddComponent<Canvas>();
            worldCanvas.renderMode = RenderMode.WorldSpace;
            worldCanvas.overrideSorting = true;
            worldCanvas.sortingOrder = 42;
            var rect = canvasObject.GetComponent<RectTransform>() ?? canvasObject.AddComponent<RectTransform>();
            rect.sizeDelta = labelSize;
            canvasObject.transform.localScale = Vector3.one * 0.016f;

            var background = CreateImage("Goal Label Background", canvasObject.transform, Vector2.zero, labelSize, Anchor.Center, new Color(0.02f, 0.025f, 0.04f, 0.86f));
            background.raycastTarget = false;
            var text = CreateText("Goal Label Text", canvasObject.transform, goal.OpenLabel, 24, FontStyle.Bold, Vector2.zero, labelSize, Anchor.Center);
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.Lerp(goal.color, Color.white, 0.45f);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.lineSpacing = 0.88f;
            text.raycastTarget = false;
            return text;
        }

        private Image CreateImage(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Anchor anchor, Color color)
        {
            var body = new GameObject(name);
            body.transform.SetParent(parent, false);
            var rect = body.AddComponent<RectTransform>();
            ApplyAnchor(rect, anchor);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            var image = body.AddComponent<Image>();
            image.color = color;
            image.material = PixelMaterialProvider.UiMaterial;
            return image;
        }

        private static void AddPanelBorder(RectTransform target, Color color, float thickness)
        {
            var body = new GameObject($"{target.name} Border");
            body.transform.SetParent(target, false);
            var rect = body.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var border = body.AddComponent<CustomShapeRectBorder>();
            border.color = color;
            border.thickness = thickness;
            border.raycastTarget = false;
        }

        private RectTransform CreatePanel(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Anchor anchor, Color color)
        {
            return CreateImage(name, parent, anchoredPosition, size, anchor, color).rectTransform;
        }

        private Text CreateText(string name, Transform parent, string content, int size, FontStyle style, Vector2 anchoredPosition, Vector2 rectSize, Anchor anchor)
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
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private Button CreateButton(
            string name,
            Transform parent,
            string label,
            int fontSize,
            FontStyle style,
            Vector2 anchoredPosition,
            Vector2 size,
            Anchor anchor,
            Color color,
            UnityEngine.Events.UnityAction onClick)
        {
            var image = CreateImage(name, parent, anchoredPosition, size, anchor, color);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            var text = CreateText($"{name} Text", image.transform, label, fontSize, style, Vector2.zero, size, Anchor.Center);
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            return button;
        }

        private static void ClearChildren(Transform parent)
        {
            for (var index = parent.childCount - 1; index >= 0; index--)
            {
                DestroyImmediate(parent.GetChild(index).gameObject);
            }
        }

        private static void ApplyAnchor(RectTransform rect, Anchor anchor)
        {
            switch (anchor)
            {
                case Anchor.TopLeft:
                    rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0f, 1f);
                    break;
                case Anchor.TopRight:
                    rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
                    rect.pivot = new Vector2(1f, 1f);
                    break;
                case Anchor.BottomLeft:
                    rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
                    rect.pivot = new Vector2(0f, 0f);
                    break;
                case Anchor.BottomRight:
                    rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
                    rect.pivot = new Vector2(1f, 0f);
                    break;
                case Anchor.Center:
                    rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    break;
            }
        }

        private static Color FamilyColor(SpellFamily family)
        {
            return family switch
            {
                SpellFamily.Fire => new Color(1f, 0.31f, 0.18f),
                SpellFamily.Water => new Color(0.24f, 0.48f, 0.86f),
                SpellFamily.Wind => new Color(0.74f, 0.86f, 0.92f),
                SpellFamily.Earth => new Color(0.74f, 0.55f, 0.32f),
                SpellFamily.Life => new Color(0.35f, 0.86f, 0.42f),
                _ => Color.white
            };
        }

        private static Color OverlayColor(OverlayOperator op)
        {
            return op switch
            {
                OverlayOperator.SteelBrace => new Color(0.78f, 0.82f, 0.86f),
                OverlayOperator.ElectricFork => new Color(1f, 0.9f, 0.22f),
                OverlayOperator.IceBar => new Color(0.48f, 0.84f, 1f),
                OverlayOperator.SoulDot => new Color(0.95f, 0.62f, 1f),
                OverlayOperator.VoidCut => new Color(0.58f, 0.42f, 0.92f),
                OverlayOperator.MartialAxis => new Color(1f, 0.58f, 0.34f),
                _ => Color.white
            };
        }

        private static string StatusLabel(RecognitionStatus status)
        {
            return status switch
            {
                RecognitionStatus.Recognized => "인식",
                RecognitionStatus.Incomplete => "불완전",
                RecognitionStatus.Ambiguous => "모호",
                RecognitionStatus.Invalid => "무효",
                _ => status.ToString()
            };
        }

        private static string QualityLine(QualityVector quality)
        {
            return $"품질 닫힘 {Percent(quality.closure)} / 선 {Percent(quality.smoothness)} / 속도 {Percent(quality.tempo)} / 안정 {Percent(quality.stability)}";
        }

        private static string QualityCoachLine(QualityVector quality)
        {
            var strongestName = "닫힘";
            var strongestValue = quality.closure;
            var weakestName = "닫힘";
            var weakestValue = quality.closure;

            CompareQualityMetric("선", quality.smoothness, ref strongestName, ref strongestValue, ref weakestName, ref weakestValue);
            CompareQualityMetric("속도", quality.tempo, ref strongestName, ref strongestValue, ref weakestName, ref weakestValue);
            CompareQualityMetric("안정", quality.stability, ref strongestName, ref strongestValue, ref weakestName, ref weakestValue);
            CompareQualityMetric("기울기 억제", 1f - quality.rotationBias, ref strongestName, ref strongestValue, ref weakestName, ref weakestValue);

            return $"{strongestName}이 강점이고 {weakestName}을 조금 더 보완하면 품질이 오릅니다.";
        }

        private static void CompareQualityMetric(
            string name,
            float value,
            ref string strongestName,
            ref float strongestValue,
            ref string weakestName,
            ref float weakestValue)
        {
            if (value > strongestValue)
            {
                strongestName = name;
                strongestValue = value;
            }

            if (value < weakestValue)
            {
                weakestName = name;
                weakestValue = value;
            }
        }

        private static string Percent(float value)
        {
            return $"{Mathf.RoundToInt(Mathf.Clamp01(value) * 100f)}%";
        }

        private static string AnchorLabel(string anchorZone)
        {
            return anchorZone switch
            {
                "upper_right" => "오른쪽 위",
                "right" => "오른쪽",
                "lower_right" => "오른쪽 아래",
                "upper" => "위쪽",
                "left" => "왼쪽",
                "" => "미확정",
                _ => anchorZone
            };
        }

        private static string ShortLine(string text, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "기록 없음";
            }

            var line = text.Replace("\r", " ").Replace("\n", " ").Trim();
            return line.Length <= maxLength ? line : line[..Mathf.Max(1, maxLength - 1)] + "…";
        }

        private static List<List<StrokeSample>> Offset(List<List<StrokeSample>> strokes, Vector2 center, float canonicalCenter)
        {
            return strokes
                .Select(stroke => stroke.Select(sample => new StrokeSample(sample.position - Vector2.one * canonicalCenter + center, sample.time)).ToList())
                .ToList();
        }

        private enum Anchor
        {
            Center,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }

        private sealed class CustomShapeReferenceDefinition
        {
            public readonly SpellFamily family;
            public readonly string label;
            public readonly string shapeToken;
            public readonly IReadOnlyList<string> eventShapeTokens;
            public readonly string summary;

            public CustomShapeReferenceDefinition(
                SpellFamily family,
                string label,
                string shapeToken,
                IReadOnlyList<string> eventShapeTokens,
                string summary)
            {
                this.family = family;
                this.label = label;
                this.shapeToken = shapeToken;
                this.eventShapeTokens = eventShapeTokens ?? Array.Empty<string>();
                this.summary = summary;
            }
        }

        private readonly struct GoalEffect
        {
            public readonly string note;
            public readonly string worldEffect;

            public GoalEffect(string note, string worldEffect)
            {
                this.note = note;
                this.worldEffect = worldEffect;
            }
        }

        private sealed class ProcessedSpell
        {
            public BaseRecognitionResult baseResult = null;
            public OverlayRecognitionResult overlayResult = null;
        }

        private sealed class ParticlePulse
        {
            public readonly Vector2 position;
            public readonly Color color;
            public readonly bool weak;
            public readonly float scaleMultiplier;
            public readonly float durationSeconds;
            public readonly int sortingOrder;
            public GameObject body;
            public float age;

            public ParticlePulse(Vector2 position, Color color, bool weak = false, float scaleMultiplier = 1f, float durationSeconds = 0f, int sortingOrder = 28)
            {
                this.position = position;
                this.color = color;
                this.weak = weak;
                this.scaleMultiplier = scaleMultiplier;
                this.durationSeconds = durationSeconds;
                this.sortingOrder = sortingOrder;
            }
        }

        private sealed class StageGate
        {
            private readonly Vector2 halfSize;
            private bool opened;

            public StageGate(string requiredGoalId, Vector2 center, Vector2 size, Vector2 resetPosition, string lockedNote, GameObject body)
            {
                this.requiredGoalId = requiredGoalId;
                this.center = center;
                this.resetPosition = resetPosition;
                this.lockedNote = lockedNote;
                this.body = body;
                halfSize = size * 0.5f;
            }

            public readonly string requiredGoalId;
            public readonly Vector2 center;
            public readonly Vector2 resetPosition;
            public readonly string lockedNote;
            public readonly GameObject body;

            public bool Contains(Vector2 position)
            {
                return Mathf.Abs(position.x - center.x) <= halfSize.x &&
                       Mathf.Abs(position.y - center.y) <= halfSize.y;
            }

            public void Open()
            {
                if (opened || body == null)
                {
                    return;
                }

                opened = true;
                var renderer = body.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.color = new Color(1f, 1f, 1f, 0.36f);
                }
            }
        }

        private sealed class DamagePopupView
        {
            private readonly Text text;
            private readonly Color color;
            private float age;

            public DamagePopupView(Vector2 position, string value, Color color, Font font)
            {
                this.color = color;
                root = new GameObject("Damage Popup");
                root.transform.position = position;
                var canvas = root.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.overrideSorting = true;
                canvas.sortingOrder = 64;
                var rect = root.GetComponent<RectTransform>() ?? root.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(96f, 42f);
                root.transform.localScale = Vector3.one * 0.017f;

                var textObject = new GameObject("Damage Popup Text");
                textObject.transform.SetParent(root.transform, false);
                var textRect = textObject.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
                text = textObject.AddComponent<Text>();
                text.font = font;
                text.fontSize = 28;
                text.fontStyle = FontStyle.Bold;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.Lerp(color, Color.white, 0.18f);
                text.text = value;
                text.raycastTarget = false;
            }

            public readonly GameObject root;

            public bool Tick(float deltaTime)
            {
                if (root == null)
                {
                    return false;
                }

                age += deltaTime;
                var t = Mathf.Clamp01(age / 0.95f);
                root.transform.position += Vector3.up * (deltaTime * 0.62f);
                root.transform.localScale = Vector3.one * Mathf.Lerp(0.017f, 0.022f, t);
                text.color = new Color(color.r, color.g, color.b, Mathf.Lerp(1f, 0f, t));
                return t < 1f;
            }

            public void Destroy()
            {
                if (root != null)
                {
                    UnityEngine.Object.Destroy(root);
                }
            }
        }

        private sealed class CharacterBarrierView
        {
            private readonly float durationSeconds;
            private readonly SpriteRenderer renderer;
            private float age;

            public CharacterBarrierView(Transform player, string sealId, Color color, float durationSeconds)
            {
                this.durationSeconds = Mathf.Max(durationSeconds, 0.1f);
                Color = color;
                root = new GameObject($"Default Barrier {sealId}");
                root.transform.SetParent(player, false);
                root.transform.localPosition = Vector3.zero;
                root.transform.localScale = Vector3.one * 1.58f;
                renderer = root.AddComponent<SpriteRenderer>();
                var spriteColor = new Color(color.r, color.g, color.b, 0.24f);
                renderer.sprite = PixelArtFactory.CreateSprite($"Default Barrier {sealId}", spriteColor, Color.white, PixelSpriteKind.Pulse);
                renderer.sharedMaterial = PixelMaterialProvider.SpriteMaterial;
                renderer.sortingOrder = 29;
            }

            public readonly GameObject root;
            public Color Color { get; }

            public bool Tick(float deltaTime, float time)
            {
                if (root == null)
                {
                    return false;
                }

                age += deltaTime;
                var lifetime = Mathf.Clamp01(1f - age / durationSeconds);
                var alpha = Mathf.Clamp01(Mathf.Lerp(0.04f, 0.18f, lifetime) + Mathf.Sin(time * 2.4f) * 0.018f);
                renderer.color = new Color(Color.r, Color.g, Color.b, alpha);
                root.transform.localScale = Vector3.one * (1.52f + Mathf.Sin(time * 1.65f) * 0.045f);
                return age < durationSeconds;
            }

            public void Destroy()
            {
                if (root != null)
                {
                    UnityEngine.Object.Destroy(root);
                }
            }
        }

        private sealed class SealView
        {
            public readonly GameObject root;
            public readonly CompiledSeal seal;
            private readonly Text label;
            private readonly GameObject attachGuide;
            private readonly float attachRadius;
            private readonly float defaultFallbackAt;
            private readonly List<GameObject> overlayMarks = new();
            private bool defaultFallbackTriggered;
            private bool postSealInputSeen;
            public bool HasAttachGuide => attachGuide != null && attachGuide.activeInHierarchy;

            public SealView(GameObject root, CompiledSeal seal, Text label, GameObject attachGuide, float attachRadius, float defaultFallbackAt)
            {
                this.root = root;
                this.seal = seal;
                this.label = label;
                this.attachGuide = attachGuide;
                this.attachRadius = attachRadius;
                this.defaultFallbackAt = defaultFallbackAt;
            }

            public void RefreshLabel(Font font)
            {
                label.font = font;
                label.text = seal.Label;
            }

            public void AddOverlayMark(OverlayOperator op)
            {
                var index = overlayMarks.Count;
                var mark = new GameObject($"Overlay Mark {op}");
                mark.transform.SetParent(root.transform, false);
                var angle = index * Mathf.PI * 2f / 3f + Mathf.PI / 4f;
                mark.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.52f, Mathf.Sin(angle) * 0.52f, -0.1f);
                mark.transform.localScale = Vector3.one * 0.22f;
                var renderer = mark.AddComponent<SpriteRenderer>();
                renderer.sprite = PixelArtFactory.CreateSprite($"Overlay {op}", OverlayColor(op), Color.white, PixelSpriteKind.Pulse);
                renderer.sharedMaterial = PixelMaterialProvider.SpriteMaterial;
                renderer.sortingOrder = 24;
                overlayMarks.Add(mark);
            }

            public void MarkPostSealInputSeen(float time)
            {
                if (time < defaultFallbackAt)
                {
                    postSealInputSeen = true;
                }
            }

            public bool TryTriggerDefaultFallback(float time)
            {
                if (defaultFallbackTriggered || postSealInputSeen || time < defaultFallbackAt)
                {
                    return false;
                }

                defaultFallbackTriggered = true;
                return true;
            }

            public void Tick(float time, float normalizedLifetime)
            {
                if (root == null)
                {
                    return;
                }
                root.transform.localScale = Vector3.one * Mathf.Lerp(0.72f, 1f, normalizedLifetime) * (1f + Mathf.Sin(time * 4f) * 0.025f);
                var renderer = root.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.color = new Color(1f, 1f, 1f, Mathf.Clamp01(normalizedLifetime + 0.16f));
                }
                if (attachGuide != null)
                {
                    var rootScale = Mathf.Max(root.transform.localScale.x, 0.01f);
                    attachGuide.transform.localScale = Vector3.one * (attachRadius / rootScale) * (1f + Mathf.Sin(time * 2.1f) * 0.018f);
                    var guideRenderer = attachGuide.GetComponent<SpriteRenderer>();
                    if (guideRenderer != null)
                    {
                        guideRenderer.color = new Color(1f, 1f, 1f, Mathf.Clamp01(normalizedLifetime * 0.42f));
                    }
                }
            }
        }
    }

    public sealed class SpellSealSnapshot
    {
        public string sealId = "";
        public SpellFamily baseFamily;
        public IReadOnlyList<OverlayOperator> overlayStack = Array.Empty<OverlayOperator>();
        public Vector2 worldCenter;
        public float worldScale;
        public float attachRadius;
        public float remainingSeconds;
        public string label = "";

        internal static SpellSealSnapshot From(CompiledSeal seal, float now)
        {
            return new SpellSealSnapshot
            {
                sealId = seal.sealId,
                baseFamily = seal.baseFamily,
                overlayStack = seal.overlayStack.ToList(),
                worldCenter = seal.worldCenter,
                worldScale = seal.worldScale,
                attachRadius = SpellCastingService.AttachRadiusFor(seal),
                remainingSeconds = Mathf.Max(0f, seal.expiresAt - now),
                label = seal.Label
            };
        }
    }

    public sealed class MagicNote
    {
        private float ttl;
        public string Text { get; private set; } = "";
        public bool Visible => ttl > 0f && !string.IsNullOrWhiteSpace(Text);

        public void Show(string text)
        {
            Text = text;
            ttl = 4.4f;
        }

        public void Tick(float deltaTime)
        {
            ttl = Mathf.Max(0f, ttl - deltaTime);
        }
    }

    public sealed class EndingReport
    {
        private readonly Dictionary<SpellFamily, int> baseUse = new();
        private readonly Dictionary<OverlayOperator, int> overlayUse = new();
        private readonly HashSet<string> discoveries = new();
        private readonly List<float> qualityScores = new();
        private readonly List<QualityVector> successfulQualities = new();
        private int hintShownCount;
        private int assistedSuccessCount;
        private int maxAssistLevel;

        public int DiscoveryCount => discoveries.Count;

        public void RecordBase(SpellFamily family, QualityVector quality, bool success, HintState hintState = null)
        {
            baseUse[family] = baseUse.TryGetValue(family, out var count) ? count + 1 : 1;
            if (success)
            {
                qualityScores.Add(quality.Average());
                successfulQualities.Add(quality);
                if (hintState?.assisted == true)
                {
                    assistedSuccessCount++;
                }
            }
        }

        public void RecordAssist(HintState hintState)
        {
            if (hintState == null)
            {
                return;
            }

            if (hintState.hintShown)
            {
                RecordHintShown(hintState.AssistLevelNumber);
            }
        }

        public void RecordHintShown(int assistLevel)
        {
            hintShownCount++;
            maxAssistLevel = Math.Max(maxAssistLevel, assistLevel);
        }

        public void RecordOverlay(OverlayOperator op)
        {
            overlayUse[op] = overlayUse.TryGetValue(op, out var count) ? count + 1 : 1;
        }

        public void RecordDiscovery(string id, string effect)
        {
            discoveries.Add($"{id}:{effect}");
        }

        public string BuildText(int totalAttempts, string outputDirectory, bool trueEnding, int completedFinalGoals, int totalFinalGoals)
        {
            var favoriteBase = baseUse.Count == 0 ? "없음" : SpellLabels.Korean(baseUse.OrderByDescending(item => item.Value).First().Key);
            var favoriteOverlay = overlayUse.Count == 0 ? "없음" : SpellLabels.Korean(overlayUse.OrderByDescending(item => item.Value).First().Key);
            var averageQuality = qualityScores.Count == 0 ? 0f : qualityScores.Average() * 100f;
            var endingName = trueEnding ? "진엔딩 (6/6 완전 복구)" : $"통과 엔딩 ({completedFinalGoals}/{totalFinalGoals})";
            var header = trueEnding ? "입학 시험 완전 통과 - 성좌심 완전 복구 보고서" : "입학 시험 통과 - 성좌심 복구 보고서";
            return
                $"{header}\n" +
                $"도달 상태: {endingName}\n\n" +
                "당신은 정답표를 따라간 것이 아니라, 탑이 알아들을 수 있는 문법을 끝까지 조립했습니다.\n\n" +
                "플레이 기록\n" +
                $"전체 시도: {totalAttempts}회\n" +
                $"가장 많이 사용한 base: {favoriteBase}\n" +
                $"가장 많이 사용한 overlay: {favoriteOverlay}\n" +
                $"발견한 세계 반응: {discoveries.Count}개\n" +
                $"평균 문양 안정도: {averageQuality:0}%\n" +
                $"힌트 표시: {hintShownCount}회 / 최대 {AssistLevelLabel(maxAssistLevel)} / 힌트 후 성공 {assistedSuccessCount}회\n" +
                $"{BuildProfileSummary()}\n" +
                "보정 정책: profile은 성공/실패 판정을 뒤집지 않고 품질 설명과 다음 연습 방향에만 사용됩니다.\n\n" +
                BuildReflectionLine(favoriteBase, favoriteOverlay, discoveries.Count) + "\n\n" +
                "자기 평가\n" +
                "1. 어떤 문양이 가장 내 손에 잘 맞았나요?\n" +
                "2. 실패했을 때 다음에 고칠 점이 보였나요?\n" +
                "3. base와 overlay 조합을 스스로 예측할 수 있었나요?\n" +
                "4. 직접 마법을 시전한다는 느낌이 있었나요?\n\n" +
                $"로그 저장 위치:\n{outputDirectory}";
        }

        private static string BuildReflectionLine(string favoriteBase, string favoriteOverlay, int discoveryCount)
        {
            if (discoveryCount == 0)
            {
                return "마지막 보고서는 아직 발견되지 않은 반응을 남겨 둡니다. 다음 시도에서는 탑의 상태 변화를 더 넓게 관찰해 보세요.";
            }

            if (favoriteBase == "없음")
            {
                return $"{favoriteOverlay} 장식을 중심으로 흐름을 조절했습니다. 이제 같은 장식을 다른 base와 묶으면 더 많은 해석이 열립니다.";
            }

            if (favoriteOverlay == "없음")
            {
                return $"{favoriteBase} base로 탑의 언어를 안정시켰습니다. overlay를 더하면 같은 문양도 다른 의도를 갖게 됩니다.";
            }

            return $"{favoriteBase} base와 {favoriteOverlay} 장식을 가장 자주 실험했습니다. 탑은 그 반복을 단순한 성공이 아니라 당신만의 문법으로 기록했습니다.";
        }

        private string BuildProfileSummary()
        {
            if (successfulQualities.Count == 0)
            {
                return "문양 습관: 아직 성공한 base 표본이 부족합니다.";
            }

            var metrics = BuildMetricScores();
            var strongest = metrics.OrderByDescending(metric => metric.score).First();
            var weakest = metrics.OrderBy(metric => metric.score).First();
            return $"문양 습관: 강점 {strongest.name} {strongest.Percent}, 보완 {weakest.name} {weakest.Percent}.";
        }

        private List<ProfileMetric> BuildMetricScores()
        {
            return new List<ProfileMetric>
            {
                new("닫힘", successfulQualities.Average(quality => quality.closure)),
                new("선", successfulQualities.Average(quality => quality.smoothness)),
                new("속도", successfulQualities.Average(quality => quality.tempo)),
                new("안정", successfulQualities.Average(quality => quality.stability)),
                new("기울기 억제", successfulQualities.Average(quality => 1f - quality.rotationBias))
            };
        }

        private static string AssistLevelLabel(int level)
        {
            return level switch
            {
                0 => "자율",
                1 => "짧은 힌트",
                2 => "체크리스트",
                3 => "강한 보조선",
                _ => $"레벨 {level}"
            };
        }

        private readonly struct ProfileMetric
        {
            public readonly string name;
            public readonly float score;

            public ProfileMetric(string name, float score)
            {
                this.name = name;
                this.score = Mathf.Clamp01(score);
            }

            public string Percent => $"{Mathf.RoundToInt(score * 100f)}%";
        }
    }

    public sealed class FloorController
    {
        private readonly List<FloorDefinition> floors;
        public int CurrentFloorIndex { get; private set; }
        public int CurrentFloorNumber => CurrentFloorIndex + 1;
        public int FloorCount => floors.Count;
        public FloorDefinition Current => floors[CurrentFloorIndex];

        public FloorController()
        {
            floors = FloorDefinition.BuildAll();
        }

        public void Load(int index)
        {
            CurrentFloorIndex = Mathf.Clamp(index, 0, floors.Count - 1);
        }
    }

    public sealed class FloorDefinition
    {
        public int number;
        public string title = "";
        public string objective = "";
        public string entryNote = "";
        public string completeNote = "";
        public Color accentColor = Color.white;
        public Color rugColor = new(0.52f, 0.12f, 0.18f);
        public readonly List<WorldStateGoal> goals = new();
        public readonly List<HazardZone> hazards = new();

        public static List<FloorDefinition> BuildAll()
        {
            return new List<FloorDefinition>
            {
                new()
                {
                    number = 1,
                    title = "발착층",
                    objective = "다섯 base 문양으로 시험장의 반응 오브젝트를 깨우세요.",
                    entryNote = "노트: 바닥에 직접 그린 선은 탑이 읽는 말이 된다.",
                    completeNote = "승강 룬이 깨어났습니다. 탑이 다음 층을 열어 줍니다.",
                    accentColor = new Color(0.96f, 0.68f, 0.28f),
                    rugColor = new Color(0.54f, 0.12f, 0.18f),
                    goals =
                    {
                        WorldStateGoal.Base("ember", "불씨", SpellFamily.Fire, new Vector2(-5.5f, 2.6f), new Color(1f, 0.31f, 0.18f), "불씨가 살아나며 오래된 룬을 데웁니다."),
                        WorldStateGoal.Base("puddle", "물웅덩이", SpellFamily.Water, new Vector2(0f, 3.0f), new Color(0.24f, 0.48f, 0.86f), "물길이 맑아지며 바닥 홈을 채웁니다."),
                        WorldStateGoal.Base("vane", "바람개비", SpellFamily.Wind, new Vector2(5.5f, 2.6f), new Color(0.74f, 0.86f, 0.92f), "바람개비가 돌며 승강 룬에 숨을 넣습니다."),
                        WorldStateGoal.Base("pillar", "돌기둥", SpellFamily.Earth, new Vector2(-3.2f, -2.45f), new Color(0.74f, 0.55f, 0.32f), "돌기둥이 제자리를 잡아 시험장을 고정합니다."),
                        WorldStateGoal.Base("vine", "마른 덩굴", SpellFamily.Life, new Vector2(3.2f, -2.45f), new Color(0.35f, 0.86f, 0.42f), "마른 덩굴에 초록 빛이 돌아옵니다.")
                    }
                },
                new()
                {
                    number = 2,
                    title = "반응층",
                    objective = "좌측 책장에서 base별 커스텀 레퍼런스를 슬롯으로 들여온 뒤, 커스텀 도형으로 반응 표식을 깨우세요.",
                    entryNote = "노트: 이 층은 저장된 커스텀 도형의 gold capture를 기준으로 반응합니다. 좌측 책장에 가까이 가면 레퍼런스를 볼 수 있습니다.",
                    completeNote = "다섯 커스텀 반응 표식이 모두 안정화되었습니다.",
                    accentColor = new Color(0.65f, 0.48f, 0.92f),
                    rugColor = new Color(0.18f, 0.18f, 0.42f),
                    goals =
                    {
                        WorldStateGoal.CustomBase("custom_fire", "불꽃별", SpellFamily.Fire, new Vector2(-5.4f, 2.55f), new Color(1f, 0.31f, 0.18f), "커스텀 불꽃별이 다음 마법을 강화하는 반응으로 새겨집니다."),
                        WorldStateGoal.CustomBase("custom_water", "물막원", SpellFamily.Water, new Vector2(-2.7f, 3.0f), new Color(0.24f, 0.48f, 0.86f), "커스텀 물막원이 방어막 반응으로 안정화됩니다."),
                        WorldStateGoal.CustomBase("custom_wind", "질풍화살", SpellFamily.Wind, new Vector2(0f, 3.05f), new Color(0.74f, 0.86f, 0.92f), "커스텀 질풍화살이 끝점 방향 사출 반응을 깨웁니다."),
                        WorldStateGoal.CustomBase("custom_earth", "대지벽", SpellFamily.Earth, new Vector2(2.7f, 3.0f), new Color(0.74f, 0.55f, 0.32f), "커스텀 대지벽이 구조물 생성 반응을 고정합니다."),
                        WorldStateGoal.CustomBase("custom_life", "생명가새", SpellFamily.Life, new Vector2(5.4f, 2.55f), new Color(0.35f, 0.86f, 0.42f), "커스텀 생명가새가 지속 버프 반응으로 이어집니다.")
                    }
                },
                new()
                {
                    number = 3,
                    title = "건널목층",
                    objective = "기본 문양 위에 커스텀 도형을 얹어 네 구간의 길을 직접 만드세요.",
                    entryNote = "노트: 먼저 기본 문양을 만들고, 그 빛나는 원 안에 필요한 도형을 얹으면 길이 생긴다.",
                    completeNote = "네 구간의 길이 모두 열렸습니다.",
                    accentColor = new Color(0.48f, 0.8f, 0.92f),
                    rugColor = new Color(0.12f, 0.34f, 0.42f),
                    goals =
                    {
                        WorldStateGoal.CustomSpell("living_bridge", "낭떠러지 다리", SpellFamily.Life, CustomSpellEffectKind.LivingBridge, new Vector2(2.85f, -3.02f), new Color(0.35f, 0.86f, 0.42f), "생명 사출 발판이 낭떠러지를 이어 줍니다.").WithReaction(WorldReactionKind.LivingBridge),
                        WorldStateGoal.CustomSpell("frozen_river", "강 얼리기", SpellFamily.Water, CustomSpellEffectKind.Ice, new Vector2(-1.75f, -1.18f), new Color(0.48f, 0.84f, 1f), "강물이 얼어 안전한 얼음길이 됩니다.").WithReaction(WorldReactionKind.FreezeRiver),
                        WorldStateGoal.CustomSpell("earth_stairs", "계단 만들기", SpellFamily.Earth, CustomSpellEffectKind.Stability, new Vector2(1.75f, 0.42f), new Color(0.74f, 0.55f, 0.32f), "대지 구조물이 가파른 길 위에 계단처럼 쌓입니다.").WithReaction(WorldReactionKind.EarthStairs),
                        WorldStateGoal.CustomSpell("wind_platform", "바람 발판", SpellFamily.Wind, CustomSpellEffectKind.WindPlatform, new Vector2(4.65f, 2.15f), new Color(0.74f, 0.86f, 0.92f), "바람이 사각 발판을 띄워 마지막 빈 공간을 건널 수 있게 합니다.").WithReaction(WorldReactionKind.WindPlatform)
                    }
                },
                new()
                {
                    number = 4,
                    title = "타격층",
                    objective = "훈련 표적에 기본 문양과 커스텀 도형 조합을 사용해 반응과 피해 표시를 확인하세요.",
                    entryNote = "노트: 표적은 어려운 말을 쓰지 않는다. 필요한 조합을 만들면 바로 반응과 숫자로 보여 준다.",
                    completeNote = "네 표적이 모두 반응했습니다.",
                    accentColor = new Color(1f, 0.42f, 0.28f),
                    rugColor = new Color(0.42f, 0.10f, 0.16f),
                    goals =
                    {
                        WorldStateGoal.CustomSpell("ice_training", "얼음 제압", SpellFamily.Water, CustomSpellEffectKind.Ice, new Vector2(-4.8f, 1.55f), new Color(0.48f, 0.84f, 1f), "얼음 반응이 표적의 움직임을 늦춥니다.").WithReaction(WorldReactionKind.CombatHit),
                        WorldStateGoal.CustomSpell("electric_training", "전기 타격", SpellFamily.Fire, CustomSpellEffectKind.Electric, new Vector2(-1.6f, 2.15f), new Color(1f, 0.9f, 0.22f), "직선 전류가 표적을 빠르게 때립니다.").WithReaction(WorldReactionKind.CombatHit),
                        WorldStateGoal.CustomSpell("cleanse_training", "정화 해제", SpellFamily.Water, CustomSpellEffectKind.Cleanse, new Vector2(1.6f, 2.15f), new Color(0.24f, 0.48f, 0.86f), "둥근 물막이 표적의 오염 효과를 씻어 냅니다.").WithReaction(WorldReactionKind.CombatHit),
                        WorldStateGoal.CustomSpell("stable_training", "방벽 시험", SpellFamily.Earth, CustomSpellEffectKind.Stability, new Vector2(4.8f, 1.55f), new Color(0.74f, 0.55f, 0.32f), "사각 구조물이 표적 앞에 엄폐물을 세웁니다.").WithReaction(WorldReactionKind.CombatHit)
                    }
                },
                new()
                {
                    number = 5,
                    title = "성좌심",
                    objective = "대형 입학 마법진의 여섯 요구치를 어떤 조합으로든 채우세요.",
                    entryNote = "노트: 마지막 시험은 정답을 묻지 않는다. 탑이 요구하는 상태를 채워라.",
                    completeNote = "대형 마법진이 복구되었습니다. 입학 시험이 끝났습니다.",
                    accentColor = new Color(0.95f, 0.75f, 0.34f),
                    rugColor = new Color(0.18f, 0.16f, 0.32f),
                    goals =
                    {
                        WorldStateGoal.Combo("stability", "안정", SpellFamily.Earth, OverlayOperator.SteelBrace, new Vector2(-4.8f, 2.6f), new Color(0.74f, 0.55f, 0.32f), "안정의 조각이 고정됩니다."),
                        WorldStateGoal.Base("cleanse", "정화", SpellFamily.Water, new Vector2(-1.6f, 3.0f), new Color(0.24f, 0.48f, 0.86f), "정화의 조각이 맑아집니다."),
                        WorldStateGoal.Combo("connection", "연결", SpellFamily.Life, OverlayOperator.SoulDot, new Vector2(1.6f, 3.0f), new Color(0.35f, 0.86f, 0.42f), "연결의 조각이 새싹처럼 이어집니다."),
                        WorldStateGoal.Overlay("cut", "절단", OverlayOperator.VoidCut, new Vector2(4.8f, 2.6f), new Color(0.58f, 0.42f, 0.92f), "절단의 조각이 오염을 분리합니다."),
                        WorldStateGoal.Overlay("focus", "집중", OverlayOperator.SoulDot, new Vector2(-2.2f, -2.5f), new Color(0.95f, 0.62f, 1f), "집중의 조각이 심장을 밝힙니다."),
                        WorldStateGoal.Base("flow", "흐름", SpellFamily.Wind, new Vector2(2.2f, -2.5f), new Color(0.74f, 0.86f, 0.92f), "흐름의 조각이 원을 다시 돌립니다.")
                    }
                }
            };
        }
    }

    public sealed class WorldStateGoal
    {
        private const float OverlayGoalRadius = 1.45f;
        private const float ComboGoalRadius = 2.05f;

        public string id;
        public string title;
        public Vector2 position;
        public Color color;
        public PixelSpriteKind kind;
        public SpellFamily? requiredBase;
        public OverlayOperator? requiredOverlay;
        public SpellFamily? comboBase;
        public OverlayOperator? comboOverlay;
        public CustomSpellEffectKind? requiredCustomSpell;
        public string discoveryNote;
        public WorldReactionKind reactionKind;
        public bool requiresCustomShape;
        public bool completed;
        public float radius = 2.15f;
        public float visualScale = 1f;
        public GameObject body;
        public SpriteRenderer renderer;
        public Text label;

        private WorldStateGoal(string id, string title, Vector2 position, Color color, PixelSpriteKind kind, string discoveryNote)
        {
            this.id = id;
            this.title = title;
            this.position = position;
            this.color = color;
            this.kind = kind;
            this.discoveryNote = discoveryNote;
        }

        public static WorldStateGoal Base(string id, string title, SpellFamily family, Vector2 position, Color color, string note)
        {
            return new WorldStateGoal(id, title, position, color, KindForFamily(family), note)
            {
                requiredBase = family,
                visualScale = 1.0f
            };
        }

        public static WorldStateGoal CustomBase(string id, string title, SpellFamily family, Vector2 position, Color color, string note)
        {
            var goal = Base(id, title, family, position, color, note);
            goal.requiresCustomShape = true;
            goal.visualScale = 0.92f;
            return goal;
        }

        public static WorldStateGoal CustomSpell(string id, string title, SpellFamily family, CustomSpellEffectKind effect, Vector2 position, Color color, string note)
        {
            var goal = Base(id, title, family, position, color, note);
            goal.requiredCustomSpell = effect;
            goal.visualScale = 0.92f;
            return goal;
        }

        private static PixelSpriteKind KindForFamily(SpellFamily family)
        {
            return family switch
            {
                SpellFamily.Fire => PixelSpriteKind.FireRune,
                SpellFamily.Water => PixelSpriteKind.WaterRune,
                SpellFamily.Wind => PixelSpriteKind.WindRune,
                SpellFamily.Earth => PixelSpriteKind.EarthRune,
                SpellFamily.Life => PixelSpriteKind.LifeRune,
                _ => PixelSpriteKind.Target
            };
        }

        public static WorldStateGoal Overlay(string id, string title, OverlayOperator op, Vector2 position, Color color, string note)
        {
            return new WorldStateGoal(id, title, position, color, PixelSpriteKind.RuneCircle, note)
            {
                requiredOverlay = op,
                radius = OverlayGoalRadius,
                visualScale = 0.75f
            };
        }

        public static WorldStateGoal Combo(string id, string title, SpellFamily family, OverlayOperator op, Vector2 position, Color color, string note)
        {
            return new WorldStateGoal(id, title, position, color, PixelSpriteKind.RuneCircle, note)
            {
                comboBase = family,
                comboOverlay = op,
                radius = ComboGoalRadius,
                visualScale = 0.85f
            };
        }

        public string RequirementLabel
        {
            get
            {
                if (comboBase.HasValue && comboOverlay.HasValue)
                {
                    return $"{SpellLabels.Korean(comboBase.Value)} + {SpellLabels.Korean(comboOverlay.Value)}";
                }

                if (requiredBase.HasValue)
                {
                    if (requiredCustomSpell.HasValue)
                    {
                        return CustomSpellEffectCatalog.RequirementLabel(requiredCustomSpell.Value);
                    }

                    return requiresCustomShape
                        ? $"커스텀 {SpellLabels.Korean(requiredBase.Value)}"
                        : SpellLabels.Korean(requiredBase.Value);
                }

                if (requiredOverlay.HasValue)
                {
                    return SpellLabels.Korean(requiredOverlay.Value);
                }

                return "관찰";
            }
        }

        public string OpenLabel => $"{title}\n{RequirementLabel}";

        public WorldStateGoal WithReaction(WorldReactionKind reactionKind)
        {
            this.reactionKind = reactionKind;
            return this;
        }

        public bool MatchesBase(SpellFamily family, Vector2 center)
        {
            return requiredBase == family && Vector2.Distance(center, position) <= radius;
        }

        public bool MatchesOverlay(CompiledSeal seal, OverlayOperator op, Vector2 center)
        {
            if (!CastTouchedGoalArea(seal, center))
            {
                return false;
            }

            if (requiredOverlay == op)
            {
                return true;
            }

            return comboBase == seal.baseFamily && comboOverlay == op;
        }

        private bool CastTouchedGoalArea(CompiledSeal seal, Vector2 center)
        {
            return Vector2.Distance(center, position) <= radius ||
                Vector2.Distance(seal.worldCenter, position) <= radius;
        }

        public WorldStateGoal Clone()
        {
            return new WorldStateGoal(id, title, position, color, kind, discoveryNote)
            {
                requiredBase = requiredBase,
                requiredOverlay = requiredOverlay,
                comboBase = comboBase,
                comboOverlay = comboOverlay,
                requiredCustomSpell = requiredCustomSpell,
                reactionKind = reactionKind,
                requiresCustomShape = requiresCustomShape,
                radius = radius,
                visualScale = visualScale
            };
        }
    }

    public enum WorldReactionKind
    {
        None,
        BridgeFlow,
        HazardStabilizer,
        LivingBridge,
        FreezeRiver,
        EarthStairs,
        WindPlatform,
        CombatHit
    }

    public sealed class HazardZone
    {
        public string title;
        public Vector2 position;
        public float radius;
        public Color color;
        public GameObject body;

        public HazardZone(string title, Vector2 position, float radius, Color color)
        {
            this.title = title;
            this.position = position;
            this.radius = radius;
            this.color = color;
        }

        public HazardZone Clone()
        {
            return new HazardZone(title, position, radius, color);
        }

        public void Stabilize(float radiusMultiplier)
        {
            radius = Mathf.Max(0.58f, radius * radiusMultiplier);
            color = Color.Lerp(color, new Color(0.46f, 0.30f, 0.28f), 0.32f);
            if (body == null)
            {
                return;
            }

            var renderer = body.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = new Color(1f, 1f, 1f, 0.68f);
            }
        }

        public void Tick(float time)
        {
            if (body == null)
            {
                return;
            }

            body.transform.localScale = Vector3.one * radius * (1f + Mathf.Sin(time * 4f) * 0.08f);
        }
    }
}
