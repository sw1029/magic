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
        public const float GameplayCameraOrthographicSize = PixelRenderSetup.ReferenceResolutionY / (2f * PixelRenderSetup.AssetsPixelsPerUnit);
        public const int FinalFloorPassingGoalCount = 5;
        public const float StandardFloorAdvanceDelaySeconds = 1.4f;
        public const float StageFloorAdvanceDelaySeconds = 2.6f;
        public const float FinalFloorPassReportDelaySeconds = 4.8f;
        public const float FinalFloorCompleteReportDelaySeconds = 1.9f;
        public const float DefaultSealFallbackDelaySeconds = 1.35f;
        public const string BuildVersion = "Magic Exam Hall 0.6.0-dev";
        private const string FloorThreeStageResourcePath = "StageDefinitions/Floor3Crossing";
        private const int ResultPanelCompactScreenWidth = 1100;
        private const float GoalIntentRadiusMultiplier = 1.35f;
        private const float GoalIntentRadiusPadding = 0.35f;
        private const float EarlyTutorialSymbolActivationRadius = 1.85f;
        private const float GoalProximityBubbleSeconds = 3.2f;
        private const float StageGatePlayerCenterVerticalPadding = 1.05f;
        private const int CustomReferenceFloorNumber = 2;
        private const float CustomReferenceShelfRadius = 1.85f;
        private const int MaxPlayerHealthHalfUnits = 6;
        private const float PlayerDamageInvulnerabilitySeconds = 1.05f;
        private const float PlayerDamageBlinkSeconds = 0.9f;
        private const float QuestScrollWidth = 430f;
        private const float QuestScrollExpandedHeight = 392f;
        private const float QuestScrollCollapsedHeight = 88f;
        private const float QuestScrollAnimationSeconds = 0.32f;
        private const float QuestScrollBodyTopOffset = 76f;
        private const float QuestScrollContentInset = 26f;
        private const float QuestScrollContentWidth = QuestScrollWidth - QuestScrollContentInset * 2f;
        private const float KeyboardMovementPulseSeconds = 0.18f;
        private const float PlatformMoveSpeed = 5.4f;
        private const float PlatformMoveAcceleration = 42f;
        private const float PlatformJumpVelocity = 7.6f;
        private const string FirstFloorLetterBody =
            "수험생에게, 첫 번째 시험의 목표를 남깁니다.\n" +
            "1. 눈앞의 다섯 표식을 차례로 살피세요.\n" +
            "2. 표식 아래 이름과 가까운 위치를 확인하세요.\n" +
            "3. 바닥에 직접 기본 문양을 천천히 그리세요.\n" +
            "4. 불꽃은 뾰족한 삼각형으로 시작합니다.\n" +
            "5. 물은 둥근 원형 흐름으로 안정시킵니다.\n" +
            "6. 바람은 같은 방향의 세 줄로 읽힙니다.\n" +
            "7. 땅은 단단한 사각형으로 고정됩니다.\n" +
            "8. 생명은 부드럽게 갈라지는 줄기로 이어집니다.\n" +
            "9. 다섯 표식이 밝아지면 다음 층으로 오르세요.";
        private static readonly Vector2 WestBookcasePosition = new(-7.25f, 1.1f);
        private static readonly IReadOnlyList<CustomShapeReferenceDefinition> FloorTwoCustomShapeReferences = new List<CustomShapeReferenceDefinition>
        {
            new(SpellFamily.Fire, "불꽃 직선", "line", new[] { "line" }, "직선으로 뻗는 불꽃 반응을 만듭니다."),
            new(SpellFamily.Water, "물 보호막", "ellipse", new[] { "ellipse" }, "둥근 물막으로 보호 반응을 펼칩니다."),
            new(SpellFamily.Wind, "바람 화살표", "arrow", new[] { "arrow" }, "끝점 방향으로 바람을 날려 보내는 화살표 도형입니다."),
            new(SpellFamily.Earth, "사각 방벽", "rect", new[] { "rect" }, "사각 판으로 땅의 구조물 반응을 고정합니다."),
            new(SpellFamily.Life, "생명 연결선", "brace", new[] { "brace" }, "생명력을 묶어 이어 주는 반응을 만듭니다.")
        };
        private static readonly IReadOnlyList<CustomShapeReferenceDefinition> FloorThreeCustomShapeReferences = new List<CustomShapeReferenceDefinition>
        {
            new(SpellFamily.Water, "얼음 결정", "hexagon", new[] { "hexagon" }, "강물을 얼음 타일로 굳혀 올라탈 수 있게 합니다."),
            new(SpellFamily.Earth, "구멍 메움판", "rect", new[] { "rect" }, "깨진 바닥 구멍을 암반 판으로 메웁니다."),
            new(SpellFamily.Life, "덩굴 다리", "rect", new[] { "arrow", "rect" }, "화살 방향으로 덩굴 다리를 뻗어 낭떠러지를 잇습니다."),
            new(SpellFamily.Wind, "바람 발판", "rect", new[] { "rect" }, "바람으로 사각 발판을 띄워 빈 공간을 건넙니다.")
        };
        private static readonly IReadOnlyList<CustomShapeReferenceDefinition> FloorFourCustomShapeReferences = new List<CustomShapeReferenceDefinition>
        {
            new(SpellFamily.Water, "얼음 결정", "hexagon", new[] { "hexagon" }, "표적의 움직임을 늦추는 얼음 결정을 만듭니다."),
            new(SpellFamily.Fire, "번개 직선", "line", new[] { "line" }, "직선 경로로 번개 타격을 뻗습니다."),
            new(SpellFamily.Water, "정화 물막", "ellipse", new[] { "ellipse" }, "둥근 물막으로 오염을 씻어 냅니다."),
            new(SpellFamily.Earth, "사각 방벽", "rect", new[] { "rect" }, "표적 앞에 암반 방벽을 세웁니다.")
        };

        [Header("Scene References")]
        public Camera mainCamera = null!;
        public Transform player = null!;
        public Canvas canvas = null!;

        private readonly List<SealView> seals = new();
        private readonly List<ParticlePulse> pulses = new();
        private readonly List<CharacterBarrierView> defaultBarriers = new();
        private readonly List<DamagePopupView> damagePopups = new();
        private readonly List<BuffQueueView> buffQueues = new();
        private readonly List<ElementalEntity> elementalEntities = new();
        private readonly List<HeartHealthGraphic> healthHearts = new();
        private readonly List<SpriteRenderer> playerBlinkRenderers = new();
        private readonly List<SpriteAccentAnimation> spriteAccentAnimations = new();
        private readonly List<FloatingGuideArrow> shelfGuideArrows = new();
        private readonly List<StageGate> activeStageGates = new();
        private readonly List<GameObject> stageEntityObjects = new();
        private readonly List<GameObject> stageEffectObjects = new();
        private readonly List<GhostTraceView> ghostTraces = new();
        private readonly List<GameObject> floorObjects = new();
        private readonly List<GameObject> customReferenceCards = new();
        private readonly List<QuestChecklistItemView> questChecklistViews = new();
        private readonly List<WorldStateGoal> activeGoals = new();
        private readonly List<HazardZone> activeHazards = new();
        private readonly Dictionary<SpellFamily, int> baseFailureCounts = new();
        private readonly Dictionary<int, QuestChecklistSnapshot> questChecklistSnapshots = new();
        private readonly HashSet<string> questImportedReferenceIdsThisFloor = new();
        private readonly HashSet<SpellFamily> discoveredFamilies = new();
        private readonly HashSet<OverlayOperator> discoveredOverlays = new();

        private ExamLogger logger = null!;
        private WorldDrawingController worldDrawing = null!;
        private PlayerSpriteAnimator playerAnimator;
        private readonly HashSet<ElementalReactionKind> discoveredReactions = new();
        private bool practiceMode;
        private FloorController floorController = null!;
        private MagicNote magicNote = null!;
        private EndingReport endingReport = null!;
        private SpellCastingService spellCasting = null!;
        private IStrokeRecognitionService recognitionService = null!;
        private CustomShapeProfileStore customShapeStore = null!;
        private CustomShapeBookController customShapeBook = null!;
        private FloorGoalSystem floorGoals = null!;
        private FloorStageDefinition activeStageDefinition = null!;
        private Rigidbody2D playerBody = null!;
        private CapsuleCollider2D playerCollider = null!;
        private MentorPresentationController mentor = null!;
        private GameBootController bootController = null!;
        private AudioDirector audioDirector = null!;
        private RectTransform hudPanel = null!;
        private RectTransform healthPanel = null!;
        private RectTransform notePanel = null!;
        private RectTransform resultPanel = null!;
        private RectTransform reportPanel = null!;
        private RectTransform toastPanel = null!;
        private RectTransform firstFloorLetterOverlay = null!;
        private RectTransform customReferenceBubble = null!;
        private RectTransform customReferencePanel = null!;
        private RectTransform goalProximityBubble = null!;
        private RectTransform questScrollPanel = null!;
        private RectTransform questScrollBodyRoot = null!;
        private RectTransform questScrollBottomRoll = null!;
        private CanvasGroup questScrollBodyGroup = null!;
        private Text customReferenceStatus = null!;
        private Text goalProximityBubbleText = null!;
        private Button floorSkipButton = null!;
        private Button questScrollToggleButton = null!;
        private Text hudTitle = null!;
        private Text hudCopy = null!;
        private Text floorProgress = null!;
        private Text noteText = null!;
        private Text resultText = null!;
        private Text reportText = null!;
        private Text toastText = null!;
        private Text firstFloorLetterText = null!;
        private Text questTitleText = null!;
        private Text questScoreText = null!;
        private Text questScrollToggleText = null!;
        private Text questStatusText = null!;
        private Text questProgressText = null!;
        private Button firstFloorLetterCloseButton = null!;
        private Image toastBackground = null!;
        private Image toastAccent = null!;
        private Text versionText = null!;
        private Font uiFont = null!;
        private QuestChecklistState currentQuestChecklist = null!;
        private WorldStateGoal goalProximityBubbleGoal = null!;
        private string sessionId = "";
        private int trialCounter;
        private int playerHealthHalfUnits = MaxPlayerHealthHalfUnits;
        private float floorStartedAt;
        private float pendingAdvanceAt = -1f;
        private float playerDamageInvulnerableUntil = -1f;
        private float playerBlinkUntil = -1f;
        private Vector2 velocity;
        private Vector2 safePosition;
        private bool finalCompletionCelebrated;
        private bool finalTrueEnding;
        private bool resultPanelCompact;
        private bool platformMotionActive;
        private bool firstFloorLetterShownThisSession;
        private bool questReferencePanelOpenedThisFloor;
        private bool questScrollCollapsed;
        private bool fallbackLeftHeld;
        private bool fallbackRightHeld;
        private bool fallbackDownHeld;
        private bool fallbackUpHeld;
        private float questScrollOpenAmount = 1f;
        private float questScrollTargetOpenAmount = 1f;
        private float toastTtl;
        private float fallbackLeftPulseUntil = -1f;
        private float fallbackRightPulseUntil = -1f;
        private float fallbackDownPulseUntil = -1f;
        private float fallbackUpPulseUntil = -1f;
        private float fallbackJumpPulseUntil = -1f;
        private float platformHorizontalVelocity;
        private float goalProximityBubbleUntil = -1f;
        private float lastGoalProximityGuideDistance;
        private string customReferenceLastStatus = "";
        private string lastGoalProximityGuideGoalId = "";
        private bool gameplayInputEnabled = true;
        private float floorEnteredAt;
        private int castsOnCurrentFloor;
        private bool firstFloorGhostShown;
        private bool firstFloorLongSilenceShown;

        public event Action<GameProgressSnapshot> ProgressCheckpointed = delegate { };

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
        public bool IsFirstFloorLetterVisibleForTests => firstFloorLetterOverlay != null && firstFloorLetterOverlay.gameObject.activeSelf;
        public Color FirstFloorLetterCloseButtonColorForTests => firstFloorLetterCloseButton?.targetGraphic is Graphic graphic ? graphic.color : Color.clear;
        public int CurrentAssistLevel { get; private set; }
        public string LastHintText { get; private set; } = "";
        public string LastMagicNoteText => magicNote?.Text ?? "";
        public string LastResultPanelTextForTests => resultText == null ? "" : resultText.text;
        public string CurrentMentorNameForTests => mentor == null ? "" : mentor.CurrentMentorName;
        public string MentorSpeechTextForTests => mentor == null ? "" : mentor.SpeechText;
        public MentorMood MentorMoodForTests => mentor == null ? MentorMood.Neutral : mentor.CurrentMood;
        public bool IsMentorVisibleForTests => mentor != null && mentor.IsVisible;
        public string HudCopyForTests => hudCopy == null ? "" : hudCopy.text;
        public string FirstFloorLetterTextForTests => firstFloorLetterText == null ? "" : firstFloorLetterText.text;
        public string FloorProgressForTests => floorProgress == null ? "" : floorProgress.text;
        public string EndingReportTextForTests => reportText == null ? "" : reportText.text;
        public int TrialCountForTests => trialCounter;
        public string VersionLabelForTests => versionText == null ? "" : versionText.text;
        public bool IsGameplayInputEnabledForTests => gameplayInputEnabled;
        public IReadOnlyList<MagicNoteEntry> MagicNoteEntriesForTests => magicNote?.Entries ?? Array.Empty<MagicNoteEntry>();
        public IReadOnlyCollection<SpellFamily> DiscoveredFamiliesForTests => discoveredFamilies;
        public IReadOnlyCollection<OverlayOperator> DiscoveredOverlaysForTests => discoveredOverlays;
        public int ActiveGhostTraceCountForTests => ghostTraces.Count;
        public int ActivePulseCountForTests => pulses.Count;
        public int ActiveDefaultBarrierCountForTests => defaultBarriers.Count;
        public Color LastDefaultBarrierColorForTests => defaultBarriers.Count == 0 ? Color.clear : defaultBarriers[^1].Color;
        public int ActiveStageGateCountForTests => activeStageGates.Count;
        public int ActiveStageInteractionCountForTests => activeStageGates.Count;
        public int ActiveStageEntityCountForTests => stageEntityObjects.Count;
        public int ActiveStageEffectVisualCountForTests => stageEffectObjects.Count;
        public int ActiveDamagePopupCountForTests => damagePopups.Count;
        public string LastDamagePopupTextForTests { get; private set; } = "";
        public int CurrentHealthHalfUnitsForTests => playerHealthHalfUnits;
        public int HealthHeartCountForTests => healthHearts.Count;
        public int LastHealthHeartStateForTests => healthHearts.Count == 0 ? -1 : healthHearts[^1].State;
        public bool IsPlayerBlinkingForTests => Time.time < playerBlinkUntil;
        public Color PlayerBlinkTintForTests => playerBlinkRenderers.Count == 0 ? Color.clear : playerBlinkRenderers[0].color;
        public int ActiveBuffQueueCountForTests => buffQueues.Count(queue => queue.IsActive);
        public int ActiveBuffSlotCountForTests => buffQueues.Sum(queue => queue.ActiveBuffCount);
        public int ActivePlayerBuffSlotCountForTests => buffQueues.Where(queue => queue.OwnerKind == BuffOwnerKind.Player).Sum(queue => queue.ActiveBuffCount);
        public int ActiveTargetBuffSlotCountForTests => buffQueues.Where(queue => queue.OwnerKind == BuffOwnerKind.Target).Sum(queue => queue.ActiveBuffCount);
        public int ActiveElementalEntityCountForTests => elementalEntities.Count(entity => entity != null);
        public int ActiveSpriteAccentAnimationCountForTests => spriteAccentAnimations.Count(animation => animation.IsActive);
        public int LastElementalReactionCountForTests { get; private set; }
        public string LastElementalReactionSummaryForTests { get; private set; } = "";
        public string LastBuffLabelForTests { get; private set; } = "";
        public float FirstBuffCooldownFillForTests => buffQueues.FirstOrDefault(queue => queue.ActiveBuffCount > 0)?.FirstFillAmount ?? 0f;
        public string LastCustomShapeEventKindForTests { get; private set; } = "";
        public string LastCustomShapeEventLabelForTests { get; private set; } = "";
        public Vector2 LastCustomShapeEventDirectionForTests { get; private set; } = Vector2.right;
        public int CustomShapeEventObjectCountForTests { get; private set; }
        public int VisibleGoalLabelCountForTests => activeGoals.Count(goal => goal.label != null);
        public int VisibleOverlayGuideCountForTests => seals.Count(seal => seal.HasAttachGuide);
        public int ActiveShelfGuideArrowCountForTests => shelfGuideArrows.Count(arrow => arrow.IsActive);
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
        public bool IsGoalProximityBubbleVisibleForTests => goalProximityBubble != null && goalProximityBubble.gameObject.activeInHierarchy;
        public string GoalProximityBubbleTextForTests => goalProximityBubbleText == null ? "" : goalProximityBubbleText.text;
        public string LastGoalProximityGuideGoalIdForTests => lastGoalProximityGuideGoalId;
        public float LastGoalProximityGuideDistanceForTests => lastGoalProximityGuideDistance;
        public int CustomReferenceCountForTests => CurrentCustomShapeReferences().Count;
        public string CustomReferenceStatusForTests => customReferenceLastStatus;
        public bool IsPlatformMotionActiveForTests => platformMotionActive;
        public Vector2 CurrentStageSafePositionForTests => safePosition;
        public Vector2 CustomReferenceShelfPositionForTests => CurrentCustomReferencePosition();
        public bool IsQuestScrollVisibleForTests => questScrollPanel != null && questScrollPanel.gameObject.activeInHierarchy;
        public bool IsQuestScrollCollapsedForTests => questScrollCollapsed;
        public bool IsQuestScrollBodyActiveForTests => questScrollBodyRoot != null && questScrollBodyRoot.gameObject.activeSelf;
        public float QuestScrollOpenAmountForTests => questScrollOpenAmount;
        public float QuestScrollPanelHeightForTests => questScrollPanel == null ? 0f : questScrollPanel.sizeDelta.y;
        public float QuestScrollBodyAlphaForTests => questScrollBodyGroup == null ? 0f : questScrollBodyGroup.alpha;
        public string QuestScrollToggleLabelForTests => questScrollToggleText == null ? "" : questScrollToggleText.text;
        public int QuestChecklistCompletedForTests => currentQuestChecklist?.CompletedCount ?? 0;
        public int QuestChecklistTotalForTests => currentQuestChecklist?.TotalCount ?? 0;
        public int QuestChecklistGlobalCompletedForTests => QuestChecklistGlobalCompleted(includeCurrent: true);
        public int QuestChecklistGlobalTotalForTests => QuestChecklistGlobalTotal(includeCurrent: true);
        public int QuestChecklistSavedCompletedForTests => QuestChecklistGlobalCompleted(includeCurrent: false);
        public int QuestChecklistSavedTotalForTests => QuestChecklistGlobalTotal(includeCurrent: false);
        public string QuestChecklistTitleForTests => questTitleText == null ? "" : questTitleText.text;
        public string QuestChecklistScoreForTests => questScoreText == null ? "" : questScoreText.text;
        public string QuestStatusForTests => questStatusText == null ? "" : questStatusText.text;
        public string QuestProgressForTests => questProgressText == null ? "" : questProgressText.text;
        public string QuestChecklistSnapshotSummaryForTests => BuildQuestChecklistSnapshotSummary();
        public TutorialPersonalizationSummary LastPersonalizationSummaryForTests { get; private set; } = TutorialPersonalizationSummary.Empty;
        public static Vector2 BuildMovementInputForTests(
            float horizontalAxis,
            float verticalAxis,
            bool leftHeld,
            bool rightHeld,
            bool downHeld,
            bool upHeld)
        {
            return BuildMovementInput(horizontalAxis, verticalAxis, leftHeld, rightHeld, downHeld, upHeld);
        }
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
            RefreshPlayerBlinkRenderers();
            BuildUi();
            ConfigureAudio();
            ConfigureMentor();
            customShapeBook = new CustomShapeBookController();
            customShapeBook.Initialize(canvas, mainCamera, player, uiFont, customShapeStore);
            ConfigureWorldDrawing();
            LoadFloor(0);
            ConfigureBoot();
        }

        private void Update()
        {
            customShapeBook?.Tick();
            TickCustomReferenceShelf();
            TickGoalProximityBubble();
            if (IsFirstFloorLetterVisibleForTests || customShapeBook?.BlocksGameplayInput == true || IsCustomReferencePanelOpenForTests)
            {
                ClearMovementInputFallback();
                velocity = Vector2.zero;
                platformHorizontalVelocity = 0f;
                if (platformMotionActive && playerBody != null)
                {
                    playerBody.linearVelocity = new Vector2(0f, playerBody.linearVelocity.y);
                }
            }
            else
            {
                TickGameplayCancelInput();
                TickPlayer();
                TickStageGates();
            }

            TickSeals();
            TickPulses();
            TickShelfGuideArrows();
            TickSpriteAccentAnimations();
            TickDefaultBarriers();
            TickDamagePopups();
            TickBuffQueues();
            TickHazards();
            TickHostileEntityContacts();
            TickPlayerBlink();
            TickGhostTraces();
            TickFirstFloorOnboarding();
            TickFloorAdvance();
            TickQuestChecklist();
            TickQuestScrollAnimation();
            magicNote.Tick(Time.deltaTime);
            mentor?.Tick(Time.time);
            TickToast();
            UpdateHud();
        }

        private void OnGUI()
        {
            var current = Event.current;
            if (current == null ||
                (current.type != EventType.KeyDown && current.type != EventType.KeyUp))
            {
                return;
            }

            CaptureKeyboardFallback(current.keyCode, current.type == EventType.KeyDown);
        }

        private void TickGameplayCancelInput()
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
            {
                worldDrawing?.CancelBufferedInput();
            }
        }

        public BaseRecognitionResult CastSyntheticBaseForTests(SpellFamily family, Vector2 worldCenter, bool movePlayerToReference = true)
        {
            if (movePlayerToReference)
            {
                MovePlayerForTests(worldCenter);
            }

            var strokes = Offset(GestureRecognizer.CreateCanonicalSamples(family, 1.6f, 0.03f), worldCenter, 0.8f);
            var result = SpellRuntime.RecognizeBase(strokes);
            return SubmitBaseRecognitionResult(result, CurrentMagicCastOrigin(worldCenter), strokes.Count);
        }

        public BaseRecognitionResult CastRawBaseForTests(List<List<StrokeSample>> strokes, Vector2 worldCenter, bool movePlayerToReference = true)
        {
            if (movePlayerToReference)
            {
                MovePlayerForTests(worldCenter);
            }

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

        public SpellCastOutcome SubmitRecognitionHandoff(SpellRecognitionHandoff handoff)
        {
            if (handoff == null)
            {
                throw new ArgumentNullException(nameof(handoff));
            }

            var outcome = spellCasting.ProcessHandoff(handoff, seals.Select(view => view.seal).ToList(), Time.time);
            ApplySubmittedSpellOutcome(outcome);
            return outcome;
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

        public void CloseFirstFloorLetterForTests()
        {
            CloseFirstFloorLetter();
        }

        public void LoadFloorForTests(int index)
        {
            LoadFloor(index);
        }

        public void MovePlayerForTests(Vector2 worldPosition)
        {
            player.position = worldPosition;
            if (playerBody != null)
            {
                playerBody.position = worldPosition;
                playerBody.linearVelocity = Vector2.zero;
            }

            platformHorizontalVelocity = 0f;
        }

        public void SetMovementInputFallbackForTests(
            bool leftHeld,
            bool rightHeld,
            bool downHeld,
            bool upHeld,
            bool jumpPressed = false)
        {
            fallbackLeftHeld = leftHeld;
            fallbackRightHeld = rightHeld;
            fallbackDownHeld = downHeld;
            fallbackUpHeld = upHeld;
            fallbackLeftPulseUntil = leftHeld ? Time.unscaledTime + KeyboardMovementPulseSeconds : -1f;
            fallbackRightPulseUntil = rightHeld ? Time.unscaledTime + KeyboardMovementPulseSeconds : -1f;
            fallbackDownPulseUntil = downHeld ? Time.unscaledTime + KeyboardMovementPulseSeconds : -1f;
            fallbackUpPulseUntil = upHeld ? Time.unscaledTime + KeyboardMovementPulseSeconds : -1f;
            fallbackJumpPulseUntil = jumpPressed ? Time.unscaledTime + KeyboardMovementPulseSeconds : -1f;
        }

        private Vector2 CurrentMagicCastOrigin(Vector2 fallback)
        {
            return player == null ? fallback : (Vector2)player.position;
        }

        public Vector2 StageGoalPositionForTests(string goalId)
        {
            return activeGoals.FirstOrDefault(goal => string.Equals(goal.id, goalId, StringComparison.OrdinalIgnoreCase))?.position ?? Vector2.zero;
        }

        public Vector3 SpriteAccentScaleForTests(string objectName)
        {
            return spriteAccentAnimations.FirstOrDefault(animation => animation.Name == objectName)?.CurrentScale ?? Vector3.zero;
        }

        public Vector2 SpriteAccentPositionForTests(string objectName)
        {
            return spriteAccentAnimations.FirstOrDefault(animation => animation.Name == objectName)?.CurrentPosition ?? Vector2.zero;
        }

        public Vector2 StageObstacleCenterForTests(string goalId)
        {
            return activeStageDefinition?.FindObstacle(goalId)?.center ?? Vector2.zero;
        }

        public Vector2 StageObstacleResetPositionForTests(string goalId)
        {
            return activeStageDefinition?.FindObstacle(goalId)?.resetPosition ?? Vector2.zero;
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

            var castCenter = CurrentMagicCastOrigin(worldCenter);
            return ApplySubmittedSpellOutcome(spellCasting.ProcessBaseResult(result, castCenter, strokeCount, Time.time)).baseResult;
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
            var references = CurrentCustomShapeReferences();
            var reference = references.FirstOrDefault(item => item.family == family);
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
            var references = CurrentCustomShapeReferences();
            var reference = references.FirstOrDefault(item => item.family == family) ?? references.FirstOrDefault();
            if (reference == null)
            {
                return BuildReferenceStrokes("line", worldCenter, 1.6f);
            }

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

        public void TriggerFirstFloorGhostForTests()
        {
            floorEnteredAt = Time.time - 8.1f;
            firstFloorGhostShown = false;
            castsOnCurrentFloor = 0;
            TickFirstFloorOnboarding();
        }

        public void SetGameplayInputEnabled(bool enabled)
        {
            gameplayInputEnabled = enabled;
            velocity = Vector2.zero;
            if (worldDrawing != null)
            {
                worldDrawing.enabled = enabled;
            }
        }

        public bool IsPracticeMode => practiceMode;
        public int DiscoveredReactionCountForTests => discoveredReactions.Count;

        public void StartNewGame()
        {
            ResetRunState();
            LoadFloor(0);
        }

        /// <summary>
        /// Post-ending sandbox: floor 1 with every reaction active but no
        /// goal progression, floor advance, or save checkpoints.
        /// </summary>
        public void StartPracticeMode()
        {
            ResetRunState();
            practiceMode = true;
            LoadFloor(0);
            ShowMagicNote("연습장: 목표 진행 없이 모든 문양과 반응을 자유롭게 실험할 수 있습니다.", MentorMood.Neutral);
        }

        public void LoadSavedProgress(int floorNumber, IEnumerable<string> noteLines)
        {
            ResetRunState();
            LoadFloor(Mathf.Clamp(floorNumber - 1, 0, FloorCount - 1));
            magicNote.Restore(noteLines ?? Array.Empty<string>(), CurrentFloorNumber);
            if (noteText != null)
            {
                noteText.text = magicNote.Text;
            }
        }

        public void PrepareForTitleScreen()
        {
            pendingAdvanceAt = -1f;
            if (reportPanel != null)
            {
                reportPanel.gameObject.SetActive(false);
            }
            if (notePanel != null)
            {
                notePanel.gameObject.SetActive(false);
            }
            SetGameplayInputEnabled(false);
        }

        public GameProgressSnapshot CreateProgressSnapshot(int resumeFloorNumber = 0)
        {
            var floorNumber = resumeFloorNumber <= 0 ? CurrentFloorNumber : Mathf.Clamp(resumeFloorNumber, 1, FloorCount);
            return new GameProgressSnapshot
            {
                floorNumber = floorNumber,
                completedGoals = activeGoals.Count(goal => goal.completed),
                totalGoals = activeGoals.Count,
                noteLines = magicNote?.Lines.ToArray() ?? Array.Empty<string>(),
                savedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                discoveries = endingReport?.DiscoveryCount ?? 0,
                endingLabel = finalTrueEnding ? "진엔딩" : finalCompletionCelebrated ? "통과 엔딩" : ""
            };
        }

        private void ResetRunState()
        {
            sessionId = $"unity-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6]}";
            logger = new ExamLogger(sessionId);
            endingReport = new EndingReport();
            trialCounter = 0;
            castsOnCurrentFloor = 0;
            CurrentAssistLevel = 0;
            LastHintText = "";
            pendingAdvanceAt = -1f;
            finalCompletionCelebrated = false;
            finalTrueEnding = false;
            baseFailureCounts.Clear();
            discoveredFamilies.Clear();
            discoveredOverlays.Clear();
            discoveredReactions.Clear();
            practiceMode = false;
            PlayerPrefs.SetInt("MagicExamHall.FirstFloorGhostSeen", 0);
            magicNote.Clear();
            if (reportPanel != null)
            {
                reportPanel.gameObject.SetActive(false);
            }
            if (toastPanel != null)
            {
                toastTtl = 0f;
                toastPanel.gameObject.SetActive(false);
            }
        }

        private void PublishProgressCheckpoint(int resumeFloorNumber)
        {
            if (practiceMode)
            {
                return;
            }

            ProgressCheckpointed(CreateProgressSnapshot(resumeFloorNumber));
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
            playerAnimator = player.GetComponent<PlayerSpriteAnimator>() ?? player.gameObject.AddComponent<PlayerSpriteAnimator>();
            EnsurePlayerPhysics();

            if (canvas == null)
            {
                var canvasObject = new GameObject("Exam Canvas");
                canvasObject.AddComponent<RectTransform>();
                canvas = canvasObject.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
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

        private void ConfigureAudio()
        {
            audioDirector = gameObject.GetComponent<AudioDirector>() ?? gameObject.AddComponent<AudioDirector>();
            audioDirector.Initialize();
        }

        private void ConfigureBoot()
        {
            bootController = gameObject.GetComponent<GameBootController>() ?? gameObject.AddComponent<GameBootController>();
            bootController.Initialize(this, canvas, uiFont);
        }

        private void ConfigureMentor()
        {
            mentor = gameObject.GetComponent<MentorPresentationController>() ?? gameObject.AddComponent<MentorPresentationController>();
            mentor.Initialize(canvas, uiFont);
        }

        private static void ConfigureMainCamera(Camera camera)
        {
            PixelRenderSetup.ConfigureCamera(camera, GameplayCameraOrthographicSize, new Color(0.035f, 0.043f, 0.055f));
        }

        private void BuildUi()
        {
            ClearChildren(canvas.transform);
            hudPanel = CreatePanel("HUD", canvas.transform, new Vector2(20, -20), new Vector2(560, 132), Anchor.TopLeft, new Color(0.04f, 0.055f, 0.075f, 0.88f));
            hudTitle = CreateText("HUD Title", hudPanel, "Magic Exam Hall", 24, FontStyle.Bold, new Vector2(16, -12), new Vector2(520, 28), Anchor.TopLeft);
            hudCopy = CreateText("HUD Copy", hudPanel, "", 15, FontStyle.Normal, new Vector2(16, -46), new Vector2(520, 60), Anchor.TopLeft);
            floorProgress = CreateText("Floor Progress", hudPanel, "", 15, FontStyle.Bold, new Vector2(16, 12), new Vector2(520, 24), Anchor.BottomLeft);
            hudPanel.gameObject.SetActive(false);
            BuildHealthUi();

            notePanel = CreatePanel("Magic Note", canvas.transform, new Vector2(20, 20), new Vector2(560, 112), Anchor.BottomLeft, new Color(0.04f, 0.055f, 0.075f, 0.84f));
            noteText = CreateText("Note Text", notePanel, "", 14, FontStyle.Normal, new Vector2(14, -12), new Vector2(530, 88), Anchor.TopLeft);

            resultPanel = CreatePanel("Spell Result", canvas.transform, new Vector2(-20, -20), new Vector2(430, 178), Anchor.TopRight, new Color(0.04f, 0.055f, 0.075f, 0.88f));
            resultText = CreateText("Result Text", resultPanel, "", 13, FontStyle.Normal, new Vector2(14, -12), new Vector2(402, 152), Anchor.TopLeft);
            UpdateResultPanelLayout();
            resultPanel.gameObject.SetActive(false);
            BuildQuestScrollUi();
            UpdateResultPanelLayout();

            reportPanel = CreatePanel("Ending Report", canvas.transform, Vector2.zero, new Vector2(760, 520), Anchor.Center, new Color(0.035f, 0.045f, 0.065f, 0.96f));
            reportText = CreateText("Report Text", reportPanel, "", 17, FontStyle.Normal, new Vector2(28, -28), new Vector2(704, 464), Anchor.TopLeft);
            reportPanel.gameObject.SetActive(false);

            toastPanel = CreatePanel("Action Toast", canvas.transform, new Vector2(-20, -20), new Vector2(500, 54), Anchor.TopRight, new Color(0.018f, 0.024f, 0.038f, 0.94f));
            toastBackground = toastPanel.GetComponent<Image>();
            toastAccent = CreateImage("Toast Accent", toastPanel, new Vector2(0f, 0f), new Vector2(6f, 54f), Anchor.TopLeft, new Color(1f, 0.82f, 0.38f, 1f));
            toastAccent.raycastTarget = false;
            toastText = CreateText("Toast Text", toastPanel, "", 16, FontStyle.Bold, new Vector2(18, -13), new Vector2(464, 28), Anchor.TopLeft);
            toastText.alignment = TextAnchor.MiddleLeft;
            toastPanel.gameObject.SetActive(false);

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
            BuildGoalProximityBubbleUi();
            BuildFirstFloorLetterUi();
        }

        private void BuildHealthUi()
        {
            healthPanel = CreatePanel("Health Bar", canvas.transform, new Vector2(20f, -20f), new Vector2(158f, 48f), Anchor.TopLeft, new Color(0.035f, 0.025f, 0.030f, 0.72f));
            AddPanelBorder(healthPanel, new Color(0.18f, 0.06f, 0.055f, 0.95f), 1.6f);
            healthHearts.Clear();
            for (var index = 0; index < 3; index++)
            {
                var heartObject = new GameObject($"Health Heart {index + 1}");
                heartObject.transform.SetParent(healthPanel, false);
                var rect = heartObject.AddComponent<RectTransform>();
                ApplyAnchor(rect, Anchor.TopLeft);
                rect.anchoredPosition = new Vector2(11f + index * 47f, -5f);
                rect.sizeDelta = new Vector2(38f, 36f);
                var heart = heartObject.AddComponent<HeartHealthGraphic>();
                heart.color = new Color(0.92f, 0.035f, 0.045f, 1f);
                heart.raycastTarget = false;
                healthHearts.Add(heart);
            }

            RefreshHealthUi();
        }

        private void BuildQuestScrollUi()
        {
            questScrollPanel = CreatePanel("Quest Scroll Panel", canvas.transform, new Vector2(-18f, -18f), new Vector2(QuestScrollWidth, QuestScrollExpandedHeight), Anchor.TopRight, new Color(0.88f, 0.69f, 0.42f, 0.995f));
            questScrollPanel.gameObject.AddComponent<RectMask2D>();
            AddPanelBorder(questScrollPanel, new Color(0.38f, 0.20f, 0.07f, 0.98f), 3.2f);
            CreateImage("Quest Scroll Readability Paper", questScrollPanel, new Vector2(16f, -52f), new Vector2(QuestScrollWidth - 32f, QuestScrollExpandedHeight - 82f), Anchor.TopLeft, new Color(0.92f, 0.74f, 0.46f, 0.88f)).raycastTarget = false;
            CreateImage("Quest Scroll Top Roll", questScrollPanel, new Vector2(24f, -12f), new Vector2(382f, 30f), Anchor.TopLeft, new Color(0.96f, 0.78f, 0.46f, 0.995f)).raycastTarget = false;
            var bottomRoll = CreateImage("Quest Scroll Bottom Roll", questScrollPanel, new Vector2(24f, 14f), new Vector2(382f, 30f), Anchor.BottomLeft, new Color(0.52f, 0.30f, 0.12f, 0.98f));
            bottomRoll.raycastTarget = false;
            questScrollBottomRoll = bottomRoll.rectTransform;
            CreateImage("Quest Scroll Left Cap", questScrollPanel, new Vector2(10f, -12f), new Vector2(28f, 30f), Anchor.TopLeft, new Color(0.60f, 0.34f, 0.13f, 0.99f)).raycastTarget = false;
            CreateImage("Quest Scroll Right Cap", questScrollPanel, new Vector2(-10f, -12f), new Vector2(28f, 30f), Anchor.TopRight, new Color(0.60f, 0.34f, 0.13f, 0.99f)).raycastTarget = false;

            questTitleText = CreateText("Quest Scroll Title", questScrollPanel, "퀘스트", 21, FontStyle.Bold, new Vector2(26f, -42f), new Vector2(258f, 34f), Anchor.TopLeft);
            ApplyQuestScrollReadableText(questTitleText, new Color(0.15f, 0.070f, 0.025f, 1f), emphasized: true);
            questTitleText.alignment = TextAnchor.MiddleLeft;
            questTitleText.raycastTarget = false;

            questScoreText = CreateText("Quest Scroll Score", questScrollPanel, "", 14, FontStyle.Bold, new Vector2(-100f, -45f), new Vector2(74f, 28f), Anchor.TopRight);
            ApplyQuestScrollReadableText(questScoreText, new Color(0.17f, 0.075f, 0.025f, 1f), emphasized: false);
            questScoreText.alignment = TextAnchor.MiddleRight;
            questScoreText.raycastTarget = false;

            questScrollToggleButton = CreateButton(
                "Quest Scroll Toggle Button",
                questScrollPanel,
                "접기",
                14,
                FontStyle.Bold,
                new Vector2(-24f, -45f),
                new Vector2(62f, 28f),
                Anchor.TopRight,
                new Color(0.52f, 0.26f, 0.095f, 0.96f),
                ToggleQuestScrollCollapsed);
            questScrollToggleButton.GetComponent<Image>().raycastTarget = true;
            questScrollToggleText = questScrollToggleButton.GetComponentInChildren<Text>();
            if (questScrollToggleText != null)
            {
                questScrollToggleText.color = new Color(0.98f, 0.86f, 0.60f, 1f);
            }

            questScrollBodyRoot = CreatePanel("Quest Scroll Body", questScrollPanel, new Vector2(0f, -QuestScrollBodyTopOffset), new Vector2(QuestScrollWidth, QuestScrollExpandedHeight - QuestScrollBodyTopOffset), Anchor.TopLeft, new Color(0.80f, 0.64f, 0.42f, 0f));
            questScrollBodyRoot.GetComponent<Image>().raycastTarget = false;
            questScrollBodyRoot.pivot = new Vector2(0f, 1f);
            questScrollBodyGroup = questScrollBodyRoot.gameObject.AddComponent<CanvasGroup>();

            CreateImage("Quest Log Divider", questScrollBodyRoot, new Vector2(QuestScrollContentInset, -208f), new Vector2(QuestScrollContentWidth, 2.5f), Anchor.TopLeft, new Color(0.33f, 0.16f, 0.055f, 0.62f)).raycastTarget = false;
            questStatusText = CreateText("Quest Status Text", questScrollBodyRoot, "", 13, FontStyle.Bold, new Vector2(QuestScrollContentInset, -218f), new Vector2(QuestScrollContentWidth, 58f), Anchor.TopLeft);
            ApplyQuestScrollReadableText(questStatusText, new Color(0.13f, 0.055f, 0.020f, 1f), emphasized: false);
            questStatusText.alignment = TextAnchor.UpperLeft;
            questStatusText.verticalOverflow = VerticalWrapMode.Truncate;
            questStatusText.raycastTarget = false;

            questProgressText = CreateText("Quest Progress Text", questScrollBodyRoot, "", 13, FontStyle.Bold, new Vector2(QuestScrollContentInset, -282f), new Vector2(QuestScrollContentWidth, 28f), Anchor.TopLeft);
            ApplyQuestScrollReadableText(questProgressText, new Color(0.16f, 0.070f, 0.025f, 1f), emphasized: false);
            questProgressText.alignment = TextAnchor.MiddleLeft;
            questProgressText.raycastTarget = false;

            ApplyQuestScrollAnimationState();
        }

        private static void ApplyQuestScrollReadableText(Text text, Color color, bool emphasized)
        {
            if (text == null)
            {
                return;
            }

            text.color = color;
            text.lineSpacing = 1.06f;

            var shadow = text.gameObject.GetComponent<Shadow>() ?? text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = emphasized
                ? new Color(0.98f, 0.78f, 0.42f, 0.42f)
                : new Color(0.98f, 0.78f, 0.42f, 0.30f);
            shadow.effectDistance = emphasized ? new Vector2(1.2f, -1.2f) : new Vector2(0.8f, -0.8f);
            shadow.useGraphicAlpha = true;
        }

        private void ToggleQuestScrollCollapsed()
        {
            SetQuestScrollCollapsed(!questScrollCollapsed);
        }

        private void SetQuestScrollCollapsed(bool collapsed, bool immediate = false)
        {
            questScrollCollapsed = collapsed;
            questScrollTargetOpenAmount = collapsed ? 0f : 1f;
            if (!collapsed && questScrollBodyRoot != null)
            {
                questScrollBodyRoot.gameObject.SetActive(true);
            }

            if (immediate)
            {
                questScrollOpenAmount = questScrollTargetOpenAmount;
            }

            ApplyQuestScrollAnimationState();
        }

        private void TickQuestScrollAnimation()
        {
            if (questScrollPanel == null || Mathf.Approximately(questScrollOpenAmount, questScrollTargetOpenAmount))
            {
                return;
            }

            var step = Time.unscaledDeltaTime / Mathf.Max(0.01f, QuestScrollAnimationSeconds);
            questScrollOpenAmount = Mathf.MoveTowards(questScrollOpenAmount, questScrollTargetOpenAmount, step);
            ApplyQuestScrollAnimationState();
        }

        private void ApplyQuestScrollAnimationState()
        {
            if (questScrollPanel == null)
            {
                return;
            }

            var eased = questScrollOpenAmount * questScrollOpenAmount * (3f - 2f * questScrollOpenAmount);
            questScrollPanel.sizeDelta = new Vector2(
                QuestScrollWidth,
                Mathf.Lerp(QuestScrollCollapsedHeight, QuestScrollExpandedHeight, eased));

            if (questScrollBodyRoot != null)
            {
                var shouldShowBody = eased > 0.01f || questScrollTargetOpenAmount > 0f;
                questScrollBodyRoot.gameObject.SetActive(shouldShowBody);
                questScrollBodyRoot.localScale = new Vector3(1f, Mathf.Lerp(0.08f, 1f, eased), 1f);
            }

            if (questScrollBodyGroup != null)
            {
                questScrollBodyGroup.alpha = eased;
                questScrollBodyGroup.interactable = eased > 0.96f;
                questScrollBodyGroup.blocksRaycasts = eased > 0.96f;
            }

            if (questScrollBottomRoll != null)
            {
                questScrollBottomRoll.sizeDelta = new Vector2(382f, Mathf.Lerp(38f, 30f, eased));
            }

            if (questScrollToggleText != null)
            {
                questScrollToggleText.text = questScrollCollapsed ? "펴기" : "접기";
            }
        }

        private void BuildFirstFloorLetterUi()
        {
            var overlayImage = CreateImage("First Floor Letter Overlay", canvas.transform, Vector2.zero, Vector2.zero, Anchor.Center, new Color(0.004f, 0.004f, 0.006f, 0.82f));
            overlayImage.raycastTarget = true;
            firstFloorLetterOverlay = overlayImage.rectTransform;
            firstFloorLetterOverlay.anchorMin = Vector2.zero;
            firstFloorLetterOverlay.anchorMax = Vector2.one;
            firstFloorLetterOverlay.pivot = new Vector2(0.5f, 0.5f);
            firstFloorLetterOverlay.offsetMin = Vector2.zero;
            firstFloorLetterOverlay.offsetMax = Vector2.zero;
            firstFloorLetterOverlay.anchoredPosition = Vector2.zero;
            firstFloorLetterOverlay.sizeDelta = Vector2.zero;

            var parchment = CreatePanel("First Floor Letter Scroll", firstFloorLetterOverlay, Vector2.zero, new Vector2(760f, 540f), Anchor.Center, new Color(0.78f, 0.64f, 0.43f, 0.98f));
            AddPanelBorder(parchment, new Color(0.40f, 0.23f, 0.10f, 0.94f), 3f);
            CreateImage("First Floor Letter Top Roll", parchment, new Vector2(32f, -22f), new Vector2(696f, 34f), Anchor.TopLeft, new Color(0.92f, 0.76f, 0.48f, 0.98f));
            CreateImage("First Floor Letter Bottom Roll", parchment, new Vector2(32f, 22f), new Vector2(696f, 34f), Anchor.BottomLeft, new Color(0.58f, 0.39f, 0.20f, 0.96f));
            CreateImage("First Floor Letter Wax Seal", parchment, new Vector2(-72f, 72f), new Vector2(54f, 54f), Anchor.BottomRight, new Color(0.56f, 0.05f, 0.04f, 0.96f));

            var title = CreateText("First Floor Letter Title", parchment, "입학 안내 편지", 29, FontStyle.Bold, new Vector2(48f, -58f), new Vector2(560f, 40f), Anchor.TopLeft);
            title.color = new Color(0.21f, 0.11f, 0.04f, 1f);
            title.alignment = TextAnchor.MiddleLeft;
            title.raycastTarget = false;

            firstFloorLetterText = CreateText("First Floor Letter Text", parchment, FirstFloorLetterBody, 18, FontStyle.Normal, new Vector2(52f, -112f), new Vector2(650f, 332f), Anchor.TopLeft);
            firstFloorLetterText.color = new Color(0.18f, 0.10f, 0.045f, 1f);
            firstFloorLetterText.lineSpacing = 1.12f;
            firstFloorLetterText.raycastTarget = false;

            var signature = CreateText("First Floor Letter Signature", parchment, "마법 시험관의 인장", 16, FontStyle.Italic, new Vector2(-244f, 58f), new Vector2(170f, 28f), Anchor.BottomRight);
            signature.color = new Color(0.28f, 0.12f, 0.05f, 0.88f);
            signature.alignment = TextAnchor.MiddleRight;
            signature.raycastTarget = false;

            firstFloorLetterCloseButton = CreateButton(
                "First Floor Letter Close Button",
                parchment,
                "X",
                20,
                FontStyle.Bold,
                new Vector2(-18f, -18f),
                new Vector2(42f, 42f),
                Anchor.TopRight,
                new Color(0.78f, 0.03f, 0.02f, 0.98f),
                CloseFirstFloorLetter);

            firstFloorLetterOverlay.gameObject.SetActive(false);
        }

        private void ShowFirstFloorLetter()
        {
            if (firstFloorLetterOverlay == null)
            {
                return;
            }

            firstFloorLetterShownThisSession = true;
            firstFloorLetterOverlay.SetAsLastSibling();
            firstFloorLetterOverlay.gameObject.SetActive(true);
        }

        private void CloseFirstFloorLetter()
        {
            HideFirstFloorLetter();
        }

        private void HideFirstFloorLetter()
        {
            if (firstFloorLetterOverlay != null)
            {
                firstFloorLetterOverlay.gameObject.SetActive(false);
            }
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
                new Vector2(18f, -118f),
                new Vector2(560f, 410f),
                Anchor.TopLeft,
                new Color(0.025f, 0.035f, 0.055f, 0.98f));
            AddPanelBorder(customReferencePanel, new Color(0.65f, 0.78f, 0.95f, 0.82f), 2f);
            var title = CreateText("Custom Reference Panel Title", customReferencePanel, "커스텀 도형 레퍼런스", 23, FontStyle.Bold, new Vector2(24f, -18f), new Vector2(500f, 34f), Anchor.TopLeft);
            title.color = new Color(0.95f, 0.99f, 1f, 1f);
            title.fontSize = 21;
            title.rectTransform.anchoredPosition = new Vector2(22f, -16f);
            title.rectTransform.sizeDelta = new Vector2(392f, 32f);
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

            customReferenceStatus = CreateText(
                "Custom Reference Status",
                customReferencePanel,
                "",
                14,
                FontStyle.Bold,
                new Vector2(22f, 20f),
                new Vector2(516f, 24f),
                Anchor.BottomLeft);
            customReferenceStatus.color = new Color(0.80f, 0.92f, 1f, 0.92f);
            customReferencePanel.gameObject.SetActive(false);
        }

        private void CreateCustomReferenceCard(CustomShapeReferenceDefinition reference, int index)
        {
            var y = -58f - index * 56f;
            var card = CreatePanel(
                $"Custom Reference Card {reference.family}",
                customReferencePanel,
                new Vector2(22f, y),
                new Vector2(516f, 48f),
                Anchor.TopLeft,
                new Color(0.045f, 0.065f, 0.095f, 0.96f));
            customReferenceCards.Add(card.gameObject);
            AddPanelBorder(card, new Color(1f, 1f, 1f, 0.12f), 1f);
            var familyColor = FamilyColor(reference.family);
            var swatch = CreateImage($"Custom Reference Swatch {reference.family}", card, new Vector2(10f, -8f), new Vector2(36f, 36f), Anchor.TopLeft, new Color(familyColor.r, familyColor.g, familyColor.b, 0.70f));
            swatch.sprite = CustomShapeSpriteFactory.CreateShapeSprite(reference.shapeToken, 2);
            swatch.preserveAspect = true;
            var label = CreateText(
                $"Custom Reference Label {reference.family}",
                card,
                ReferenceShapeTitle(reference),
                15,
                FontStyle.Bold,
                new Vector2(58f, -7f),
                new Vector2(184f, 21f),
                Anchor.TopLeft);
            label.color = Color.Lerp(familyColor, Color.white, 0.55f);
            var summary = CreateText(
                $"Custom Reference Summary {reference.family}",
                card,
                ReferenceShapeSummary(reference),
                12,
                FontStyle.Normal,
                new Vector2(58f, -29f),
                new Vector2(322f, 17f),
                Anchor.TopLeft);
            summary.color = new Color(0.82f, 0.88f, 0.94f, 0.90f);
            var capturedReference = reference;
            CreateButton(
                $"Import Custom Reference {reference.family}",
                card,
                "들여오기",
                13,
                FontStyle.Bold,
                new Vector2(404f, -7f),
                new Vector2(92f, 34f),
                Anchor.TopLeft,
                new Color(0.10f, 0.24f, 0.38f, 0.98f),
                () =>
                {
                    ImportCustomReference(capturedReference, out _, out _);
                });
        }

        private static string ReferenceShapeTitle(CustomShapeReferenceDefinition reference)
        {
            return ShapeTokenTitle(reference?.shapeToken);
        }

        private static string ReferenceShapeSummary(CustomShapeReferenceDefinition reference)
        {
            return ShapeTokenSummary(reference?.shapeToken);
        }

        private static string ShapeTokenTitle(string token)
        {
            return NormalizeShapeToken(token) switch
            {
                "line" => "직선",
                "ellipse" => "타원",
                "arrow" => "화살표",
                "rect" => "사각형",
                "brace" => "꺾쇠",
                "hexagon" => "육각형",
                "star" => "별",
                "wave" => "물결",
                _ => "자유 도형"
            };
        }

        private static string ShapeTokenSummary(string token)
        {
            return NormalizeShapeToken(token) switch
            {
                "line" => "시작점에서 끝점까지 한 획으로 곧게 긋습니다.",
                "ellipse" => "둥근 고리를 한 바퀴 닫아 그립니다.",
                "arrow" => "몸통 선을 긋고 끝에 화살촉을 붙입니다.",
                "rect" => "네 변을 이어 닫힌 사각형을 만듭니다.",
                "brace" => "양끝이 벌어진 꺾쇠 모양으로 휘어 그립니다.",
                "hexagon" => "여섯 변을 이어 닫힌 결정형을 만듭니다.",
                "star" => "뾰족한 꼭짓점을 반복해 별 모양을 만듭니다.",
                "wave" => "좌우로 흐르는 물결선을 한 획으로 그립니다.",
                _ => "예시와 같은 외곽선을 천천히 따라 그립니다."
            };
        }

        private static string NormalizeShapeToken(string token)
        {
            return string.IsNullOrWhiteSpace(token) ? "line" : token.ToLowerInvariant();
        }

        private void BeginQuestChecklistForCurrentFloor()
        {
            var floor = floorController.Current;
            currentQuestChecklist = new QuestChecklistState(
                floor.number,
                floor.title,
                BuildQuestChecklistDefinitions(floor).ToList());
            RebuildQuestChecklistRows();
            TickQuestChecklist(forceRefresh: true);
        }

        private IEnumerable<QuestChecklistItemDefinition> BuildQuestChecklistDefinitions(FloorDefinition floor)
        {
            return floor.number switch
            {
                1 => new[]
                {
                    QuestChecklistItemDefinition.GoalsAtLeast("floor1_first", "첫 표식 하나 깨우기", 1),
                    QuestChecklistItemDefinition.GoalsAtLeast("floor1_three", "서로 다른 기본 문양 세 개 성공", 3),
                    QuestChecklistItemDefinition.AllGoals("floor1_all", "다섯 기본 표식 모두 깨우기")
                },
                2 => new[]
                {
                    QuestChecklistItemDefinition.ReferencePanel("floor2_shelf", "책장 프리셋 창 열기"),
                    QuestChecklistItemDefinition.ReferenceImports("floor2_import", "책장에서 도형 하나 가져오기", 1),
                    QuestChecklistItemDefinition.GoalsAtLeast("floor2_three", "커스텀 표식 세 개 깨우기", 3),
                    QuestChecklistItemDefinition.AllGoals("floor2_all", "다섯 커스텀 표식 모두 깨우기")
                },
                3 => new[]
                {
                    QuestChecklistItemDefinition.ReferenceImports("floor3_import", "3층 책장 도형 하나 가져오기", 1),
                    QuestChecklistItemDefinition.Goal("floor3_river", "강물을 얼음길로 바꾸기", "frozen_river"),
                    QuestChecklistItemDefinition.Goal("floor3_hole", "바닥 구멍 메우기", "earth_stairs"),
                    QuestChecklistItemDefinition.Goal("floor3_cliff", "낭떠러지를 다리로 잇기", "living_bridge"),
                    QuestChecklistItemDefinition.Goal("floor3_gap", "바람 발판으로 마지막 빈 공간 건너기", "wind_platform")
                },
                4 => new[]
                {
                    QuestChecklistItemDefinition.ReferenceImports("floor4_import", "전투 도형 하나 가져오기", 1),
                    QuestChecklistItemDefinition.GoalsAtLeast("floor4_two", "훈련 표적 둘 반응시키기", 2),
                    QuestChecklistItemDefinition.AllGoals("floor4_all", "네 표적 모두 반응시키기")
                },
                _ => new[]
                {
                    QuestChecklistItemDefinition.GoalsAtLeast("floor5_three", "마지막 마법진 요구 세 개 채우기", 3),
                    QuestChecklistItemDefinition.GoalsAtLeast("floor5_pass", "통과 기준 다섯 요구 채우기", FinalFloorPassingGoalCount),
                    QuestChecklistItemDefinition.AllGoals("floor5_all", "여섯 요구 모두 채우기")
                }
            };
        }

        private void RebuildQuestChecklistRows()
        {
            foreach (var view in questChecklistViews)
            {
                view.Destroy();
            }
            questChecklistViews.Clear();

            if (questScrollPanel == null || currentQuestChecklist == null)
            {
                return;
            }

            questTitleText.text = $"층 {currentQuestChecklist.floorNumber} 퀘스트";
            var y = -10f;
            for (var index = 0; index < currentQuestChecklist.entries.Count; index++)
            {
                questChecklistViews.Add(CreateQuestChecklistRow(currentQuestChecklist.entries[index], index, y));
                y -= 38f;
            }
        }

        private QuestChecklistItemView CreateQuestChecklistRow(QuestChecklistEntry entry, int index, float y)
        {
            var row = CreatePanel(
                $"Quest Checklist Row {index + 1}",
                questScrollBodyRoot == null ? questScrollPanel : questScrollBodyRoot,
                new Vector2(QuestScrollContentInset, y),
                new Vector2(QuestScrollContentWidth, 34f),
                Anchor.TopLeft,
                new Color(0.82f, 0.60f, 0.34f, 0.32f));
            row.GetComponent<Image>().raycastTarget = false;

            var box = CreateImage(
                $"Quest Checkbox {index + 1}",
                row,
                new Vector2(5f, -5f),
                new Vector2(24f, 24f),
                Anchor.TopLeft,
                new Color(0.96f, 0.82f, 0.54f, 0.72f));
            box.raycastTarget = false;
            AddPanelBorder(box.rectTransform, new Color(0.27f, 0.12f, 0.035f, 0.96f), 1.7f);

            var checkObject = new GameObject($"Quest Checkmark {index + 1}");
            checkObject.transform.SetParent(box.transform, false);
            var checkRect = checkObject.AddComponent<RectTransform>();
            checkRect.anchorMin = Vector2.zero;
            checkRect.anchorMax = Vector2.one;
            checkRect.pivot = new Vector2(0.5f, 0.5f);
            checkRect.offsetMin = new Vector2(1.5f, 1.5f);
            checkRect.offsetMax = new Vector2(-1.5f, -1.5f);
            checkObject.AddComponent<CanvasRenderer>();
            var check = checkObject.AddComponent<QuestCheckMarkGraphic>();
            check.color = new Color(0.82f, 0.04f, 0.035f, 0.98f);
            check.raycastTarget = false;
            checkObject.SetActive(false);

            var label = CreateText(
                $"Quest Checklist Label {index + 1}",
                row,
                entry.definition.label,
                15,
                FontStyle.Bold,
                new Vector2(39f, -2f),
                new Vector2(QuestScrollContentWidth - 46f, 30f),
                Anchor.TopLeft);
            ApplyQuestScrollReadableText(label, new Color(0.12f, 0.050f, 0.016f, 1f), emphasized: false);
            label.alignment = TextAnchor.MiddleLeft;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;

            return new QuestChecklistItemView(row.gameObject, box, check, label);
        }

        private void TickQuestChecklist(bool forceRefresh = false)
        {
            if (currentQuestChecklist == null)
            {
                return;
            }

            var changed = false;
            foreach (var entry in currentQuestChecklist.entries)
            {
                var completed = EvaluateQuestChecklistEntry(entry.definition);
                if (entry.completed == completed)
                {
                    continue;
                }

                entry.completed = completed;
                changed = true;
            }

            if (changed || forceRefresh)
            {
                RefreshQuestChecklistView();
            }
        }

        private bool EvaluateQuestChecklistEntry(QuestChecklistItemDefinition definition)
        {
            return definition.kind switch
            {
                QuestChecklistConditionKind.GoalCompleted => activeGoals.Any(goal =>
                    goal.completed && string.Equals(goal.id, definition.goalId, StringComparison.OrdinalIgnoreCase)),
                QuestChecklistConditionKind.GoalsCompletedAtLeast => activeGoals.Count(goal => goal.completed) >= definition.threshold,
                QuestChecklistConditionKind.AllGoalsCompleted => activeGoals.Count > 0 && activeGoals.All(goal => goal.completed),
                QuestChecklistConditionKind.ReferencePanelOpened => questReferencePanelOpenedThisFloor,
                QuestChecklistConditionKind.ReferenceImportsAtLeast => questImportedReferenceIdsThisFloor.Count >= definition.threshold,
                _ => false
            };
        }

        private void RefreshQuestChecklistView()
        {
            if (currentQuestChecklist == null)
            {
                return;
            }

            questTitleText.text = $"층 {currentQuestChecklist.floorNumber} 퀘스트";
            questScoreText.text = $"{QuestChecklistGlobalCompleted(includeCurrent: true)}/{QuestChecklistGlobalTotal(includeCurrent: true)}";
            for (var index = 0; index < currentQuestChecklist.entries.Count && index < questChecklistViews.Count; index++)
            {
                questChecklistViews[index].Refresh(currentQuestChecklist.entries[index].completed);
            }
        }

        private void SaveCurrentQuestChecklistScore(string reason)
        {
            if (currentQuestChecklist == null || currentQuestChecklist.entries.Count == 0)
            {
                return;
            }

            TickQuestChecklist(forceRefresh: true);
            var snapshot = CaptureCurrentQuestChecklistSnapshot(reason);
            var shouldLog = !questChecklistSnapshots.TryGetValue(snapshot.floorNumber, out var previous) ||
                            previous.completedCount != snapshot.completedCount ||
                            previous.totalCount != snapshot.totalCount ||
                            !string.Equals(previous.reason, snapshot.reason, StringComparison.Ordinal);
            questChecklistSnapshots[snapshot.floorNumber] = snapshot;
            endingReport.RecordQuestChecklist(
                BuildQuestChecklistSnapshotSummary(),
                QuestChecklistGlobalCompleted(includeCurrent: false),
                QuestChecklistGlobalTotal(includeCurrent: false));
            if (shouldLog)
            {
                logger.LogQuestChecklist(new QuestChecklistLog
                {
                    sessionId = sessionId,
                    floorId = snapshot.floorNumber.ToString(CultureInfo.InvariantCulture),
                    floorTitle = snapshot.floorTitle,
                    reason = snapshot.reason,
                    completed = snapshot.completedCount,
                    total = snapshot.totalCount,
                    globalCompleted = QuestChecklistGlobalCompleted(includeCurrent: false),
                    globalTotal = QuestChecklistGlobalTotal(includeCurrent: false),
                    elapsedMs = snapshot.elapsedMs,
                    items = snapshot.items
                });
            }
        }

        private QuestChecklistSnapshot CaptureCurrentQuestChecklistSnapshot(string reason)
        {
            return new QuestChecklistSnapshot
            {
                floorNumber = currentQuestChecklist.floorNumber,
                floorTitle = currentQuestChecklist.floorTitle,
                completedCount = currentQuestChecklist.CompletedCount,
                totalCount = currentQuestChecklist.TotalCount,
                reason = reason,
                elapsedMs = Mathf.RoundToInt((Time.time - floorStartedAt) * 1000f),
                items = string.Join(" | ", currentQuestChecklist.entries.Select(entry =>
                    $"{entry.definition.id}:{(entry.completed ? "done" : "open")}"))
            };
        }

        private int QuestChecklistGlobalCompleted(bool includeCurrent)
        {
            var total = questChecklistSnapshots.Values
                .Where(snapshot => !includeCurrent || currentQuestChecklist == null || snapshot.floorNumber != currentQuestChecklist.floorNumber)
                .Sum(snapshot => snapshot.completedCount);
            if (includeCurrent && currentQuestChecklist != null)
            {
                total += currentQuestChecklist.CompletedCount;
            }

            return total;
        }

        private int QuestChecklistGlobalTotal(bool includeCurrent)
        {
            var total = questChecklistSnapshots.Values
                .Where(snapshot => !includeCurrent || currentQuestChecklist == null || snapshot.floorNumber != currentQuestChecklist.floorNumber)
                .Sum(snapshot => snapshot.totalCount);
            if (includeCurrent && currentQuestChecklist != null)
            {
                total += currentQuestChecklist.TotalCount;
            }

            return total;
        }

        private string BuildQuestChecklistSnapshotSummary()
        {
            if (questChecklistSnapshots.Count == 0)
            {
                return "아직 저장된 퀘스트 점수가 없습니다.";
            }

            return string.Join(
                "\n",
                questChecklistSnapshots.Values
                    .OrderBy(snapshot => snapshot.floorNumber)
                    .Select(snapshot => $"{snapshot.floorNumber}층 {snapshot.completedCount}/{snapshot.totalCount} - {snapshot.reason}"));
        }

        private void LoadFloor(int index, bool saveCurrentQuestScore = true)
        {
            if (saveCurrentQuestScore)
            {
                SaveCurrentQuestChecklistScore("floor_change");
            }

            pendingAdvanceAt = -1f;
            finalCompletionCelebrated = false;
            finalTrueEnding = false;
            reportPanel.gameObject.SetActive(false);
            resultPanel.gameObject.SetActive(false);
            if (resultText != null)
            {
                resultText.text = "";
            }

            floorSkipButton.gameObject.SetActive(true);
            questScrollPanel.gameObject.SetActive(true);
            CloseCustomReferenceUi();
            HideGoalProximityBubble();
            ClearFloorObjects();
            floorController.Load(index);
            questReferencePanelOpenedThisFloor = false;
            questImportedReferenceIdsThisFloor.Clear();
            activeStageDefinition = LoadStageDefinitionForFloor(floorController.Current.number);
            ConfigurePlatformMotion(activeStageDefinition != null);
            ApplyFloorTheme(floorController.Current);
            audioDirector?.PlayForFloor(floorController.Current.number);
            safePosition = activeStageDefinition == null ? new Vector2(0f, -4.05f) : activeStageDefinition.playerStart;
            MovePlayerTo(safePosition);
            floorStartedAt = Time.time;
            floorEnteredAt = Time.time;
            castsOnCurrentFloor = 0;
            firstFloorGhostShown = PlayerPrefs.GetInt("MagicExamHall.FirstFloorGhostSeen", 0) == 1;
            firstFloorLongSilenceShown = false;
            activeGoals.Clear();
            activeGoals.AddRange(floorController.Current.goals.Select(goal => goal.Clone()));
            ApplyStageGoalOverrides();
            activeHazards.Clear();
            activeHazards.AddRange(floorController.Current.hazards.Select(hazard => hazard.Clone()));
            BuildFloorArt(floorController.Current);
            BeginQuestChecklistForCurrentFloor();
            SetQuestScrollCollapsed(floorController.Current.number == 3, immediate: true);
            TickQuestChecklist(forceRefresh: true);
            UpdateHud();
            UpdateResultPanelLayout();
            mentor?.ConfigureFloor(floorController.Current.number);
            ShowMagicNote(BuildFloorEntryNote(floorController.Current), MentorMood.Neutral);
            if (floorController.Current.number == 1 && !firstFloorLetterShownThisSession)
            {
                ShowFirstFloorLetter();
            }
            else
            {
                HideFirstFloorLetter();
            }
        }

        private static FloorStageDefinition LoadStageDefinitionForFloor(int floorNumber)
        {
            if (floorNumber != 3)
            {
                return null;
            }

            return Resources.Load<FloorStageDefinition>(FloorThreeStageResourcePath) ??
                   FloorStageDefinition.CreateFallbackFloorThree();
        }

        private void ApplyStageGoalOverrides()
        {
            if (activeStageDefinition?.obstacles == null)
            {
                return;
            }

            foreach (var goal in activeGoals)
            {
                var obstacle = activeStageDefinition.FindObstacle(goal.id);
                if (obstacle == null)
                {
                    continue;
                }

                goal.position = obstacle.goalPosition;
                goal.radius = obstacle.goalRadius <= 0f ? goal.radius : obstacle.goalRadius;
            }
        }

        private void ConfigurePlatformMotion(bool enabled)
        {
            platformMotionActive = enabled;
            EnsurePlayerPhysics();
            if (playerBody == null || playerCollider == null)
            {
                return;
            }

            playerBody.simulated = enabled;
            playerCollider.enabled = enabled;
            playerBody.linearVelocity = Vector2.zero;
            platformHorizontalVelocity = 0f;
            velocity = Vector2.zero;
            if (!enabled && mainCamera != null)
            {
                mainCamera.transform.position = new Vector3(0f, 0f, -10f);
                mainCamera.orthographicSize = GameplayCameraOrthographicSize;
            }
        }

        private void SkipCurrentFloorForDebug()
        {
            if (HasEndingReport)
            {
                return;
            }

            pendingAdvanceAt = -1f;
            resultPanel.gameObject.SetActive(false);
            SaveCurrentQuestChecklistScore("skip");
            if (floorController.CurrentFloorIndex < floorController.FloorCount - 1)
            {
                LoadFloor(floorController.CurrentFloorIndex + 1, saveCurrentQuestScore: false);
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

            var shelfPosition = CurrentCustomReferencePosition();
            var hasReferences = CurrentCustomShapeReferences().Count > 0;
            var closeToShelf = hasReferences && Vector2.Distance(player.position, shelfPosition) <= CustomReferenceShelfRadius;
            var shouldShowBubble = closeToShelf &&
                                   !IsCustomReferencePanelOpenForTests &&
                                   customShapeBook?.BlocksGameplayInput != true &&
                                   !HasEndingReport;
            customReferenceBubble.gameObject.SetActive(shouldShowBubble);
            if (!shouldShowBubble)
            {
                return;
            }

            customReferenceBubble.anchoredPosition = WorldToCanvasPosition(shelfPosition + new Vector2(0.88f, 1.22f));
        }

        private void BuildGoalProximityBubbleUi()
        {
            goalProximityBubble = CreatePanel(
                "Goal Proximity Bubble",
                canvas.transform,
                Vector2.zero,
                new Vector2(246f, 78f),
                Anchor.Center,
                new Color(0.045f, 0.055f, 0.070f, 0.96f));
            AddPanelBorder(goalProximityBubble, new Color(1f, 0.82f, 0.34f, 0.78f), 2f);
            var tail = CreateImage("Goal Proximity Bubble Tail", goalProximityBubble, new Vector2(108f, -60f), new Vector2(26f, 26f), Anchor.TopLeft, new Color(0.045f, 0.055f, 0.070f, 0.96f));
            tail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            tail.raycastTarget = false;

            goalProximityBubbleText = CreateText(
                "Goal Proximity Bubble Text",
                goalProximityBubble,
                "가까이 이동\n이 표식 옆에서 그리세요",
                13,
                FontStyle.Bold,
                new Vector2(14f, -10f),
                new Vector2(218f, 56f),
                Anchor.TopLeft);
            goalProximityBubbleText.alignment = TextAnchor.MiddleCenter;
            goalProximityBubbleText.color = new Color(1f, 0.93f, 0.66f, 1f);
            goalProximityBubbleText.raycastTarget = false;
            goalProximityBubble.gameObject.SetActive(false);
        }

        private void TickGoalProximityBubble()
        {
            if (goalProximityBubble == null)
            {
                return;
            }

            var shouldShow = goalProximityBubbleGoal != null &&
                             !goalProximityBubbleGoal.completed &&
                             Time.time < goalProximityBubbleUntil &&
                             !HasEndingReport &&
                             !IsFirstFloorLetterVisibleForTests &&
                             customShapeBook?.BlocksGameplayInput != true &&
                             !IsCustomReferencePanelOpenForTests;
            goalProximityBubble.gameObject.SetActive(shouldShow);
            if (!shouldShow)
            {
                return;
            }

            var bob = Mathf.Sin(Time.time * 5.2f) * 0.06f;
            goalProximityBubble.anchoredPosition = WorldToCanvasPosition(goalProximityBubbleGoal.position + new Vector2(0f, 1.18f + bob));
            goalProximityBubble.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 7.4f) * 0.025f);
            if (resultPanel == null || !resultPanel.gameObject.activeInHierarchy)
            {
                goalProximityBubble.SetAsLastSibling();
            }
        }

        private void ShowGoalProximityBubble(WorldStateGoal goal, float distance, float radius)
        {
            if (goal == null)
            {
                return;
            }

            goalProximityBubbleGoal = goal;
            goalProximityBubbleUntil = Time.time + GoalProximityBubbleSeconds;
            lastGoalProximityGuideGoalId = goal.id;
            lastGoalProximityGuideDistance = distance;
            if (goalProximityBubbleText != null)
            {
                goalProximityBubbleText.text = $"{goal.title} 가까이 이동\n표식 바로 옆에서 그리세요";
            }

            pulses.Add(new ParticlePulse(goal.position, goal.color, weak: true, scaleMultiplier: 0.88f, durationSeconds: 0.85f, sortingOrder: 34));
            TickGoalProximityBubble();
        }

        private void HideGoalProximityBubble()
        {
            goalProximityBubbleGoal = null;
            goalProximityBubbleUntil = -1f;
            if (goalProximityBubble != null)
            {
                goalProximityBubble.gameObject.SetActive(false);
            }
        }

        private Vector2 CurrentCustomReferencePosition()
        {
            return activeStageDefinition == null ? WestBookcasePosition : activeStageDefinition.customReferencePosition;
        }

        private IReadOnlyList<CustomShapeReferenceDefinition> CurrentCustomShapeReferences()
        {
            return floorController?.Current.number switch
            {
                2 => FloorTwoCustomShapeReferences,
                3 => FloorThreeCustomShapeReferences,
                4 => FloorFourCustomShapeReferences,
                _ => Array.Empty<CustomShapeReferenceDefinition>()
            };
        }

        private void OpenCustomReferencePanel()
        {
            if (customReferencePanel == null)
            {
                return;
            }

            RebuildCustomReferenceCards();
            customReferencePanel.gameObject.SetActive(true);
            customReferencePanel.SetAsLastSibling();
            questReferencePanelOpenedThisFloor = true;
            TickQuestChecklist(forceRefresh: true);
            if (customReferenceBubble != null)
            {
                customReferenceBubble.gameObject.SetActive(false);
            }

            var floorNumber = floorController?.Current.number ?? 0;
            SetCustomReferenceStatus($"{floorNumber}층 책장의 프리셋만 표시됩니다. 필요한 도형을 빈 슬롯으로 들여오세요.");
        }

        private void CloseCustomReferencePanel()
        {
            if (customReferencePanel != null)
            {
                customReferencePanel.gameObject.SetActive(false);
            }
        }

        private void RebuildCustomReferenceCards()
        {
            foreach (var card in customReferenceCards)
            {
                if (card != null)
                {
                    card.SetActive(false);
                    if (Application.isPlaying)
                    {
                        Destroy(card);
                    }
                    else
                    {
                        DestroyImmediate(card);
                    }
                }
            }

            customReferenceCards.Clear();
            var references = CurrentCustomShapeReferences();
            for (var index = 0; index < references.Count; index++)
            {
                CreateCustomReferenceCard(references[index], index);
            }

            if (customReferenceStatus != null)
            {
                customReferenceStatus.transform.SetAsLastSibling();
            }
        }

        private void EnsurePlayerPhysics()
        {
            if (player == null)
            {
                return;
            }

            playerBody = player.GetComponent<Rigidbody2D>();
            if (playerBody == null)
            {
                playerBody = player.gameObject.AddComponent<Rigidbody2D>();
            }
            playerBody.bodyType = RigidbodyType2D.Dynamic;
            playerBody.gravityScale = 3.25f;
            playerBody.freezeRotation = true;
            playerBody.interpolation = RigidbodyInterpolation2D.Interpolate;
            playerBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            playerBody.simulated = platformMotionActive;

            playerCollider = player.GetComponent<CapsuleCollider2D>();
            if (playerCollider == null)
            {
                playerCollider = player.gameObject.AddComponent<CapsuleCollider2D>();
            }
            playerCollider.size = new Vector2(0.55f, 0.82f);
            playerCollider.offset = new Vector2(0f, -0.05f);
            playerCollider.enabled = platformMotionActive;
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

            var replacingExistingFamilySlot = false;
            for (var index = 0; index < CustomShapeProfileStore.SlotCount; index++)
            {
                if (!customShapeStore.IsSlotOccupied(index))
                {
                    continue;
                }

                if (customShapeStore.GetSlot(index).mappedFamily != reference.family)
                {
                    continue;
                }

                slotIndex = index;
                replacingExistingFamilySlot = true;
                break;
            }

            if (slotIndex < 0)
            {
                for (var index = 0; index < CustomShapeProfileStore.SlotCount; index++)
                {
                    if (!customShapeStore.IsSlotOccupied(index))
                    {
                        slotIndex = index;
                        break;
                    }
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
                questImportedReferenceIdsThisFloor.Add($"{floorController.Current.number}:{reference.family}:{reference.shapeToken}");
                TickQuestChecklist(forceRefresh: true);
                message = replacingExistingFamilySlot
                    ? $"{ReferenceShapeTitle(reference)} 도형으로 슬롯 {slotIndex + 1:00}을 갱신했습니다."
                    : $"{ReferenceShapeTitle(reference)} 도형을 슬롯 {slotIndex + 1:00}에 가져왔습니다.";
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

        private void ApplyFloorTheme(FloorDefinition floor)
        {
            var baseBackground = new Color(0.035f, 0.043f, 0.055f);
            if (mainCamera != null)
            {
                mainCamera.backgroundColor = Color.Lerp(baseBackground, floor.accentColor, 0.10f);
            }

            if (hudPanel != null)
            {
                var image = hudPanel.GetComponent<Image>();
                if (image != null)
                {
                    image.color = Color.Lerp(new Color(0.04f, 0.055f, 0.075f, 0.88f), floor.accentColor, 0.08f);
                }
            }

            if (notePanel != null)
            {
                var image = notePanel.GetComponent<Image>();
                if (image != null)
                {
                    image.color = Color.Lerp(new Color(0.04f, 0.055f, 0.075f, 0.84f), floor.accentColor, 0.06f);
                }
            }

            if (hudTitle != null)
            {
                hudTitle.color = Color.Lerp(floor.accentColor, Color.white, 0.48f);
            }
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
            if (floor.number == CustomReferenceFloorNumber)
            {
                CreateBookcaseGuideArrow("West Bookcase Guide Arrow", WestBookcasePosition, floor.accentColor, emphasized: true, floorRoot.transform);
                CreateBookcaseGuideArrow("East Bookcase Guide Arrow", new Vector2(7.25f, 1.1f), floor.accentColor, emphasized: false, floorRoot.transform);
            }
            var northwestCandle = CreateWorldSprite("Northwest Candle", new Vector2(-6.85f, 3.65f), Vector3.one * 0.85f, new Color(0.63f, 0.57f, 0.44f), new Color(1f, 0.56f, 0.15f), PixelSpriteKind.Candle, 2, false, Vector2.one, floorRoot.transform);
            var northeastCandle = CreateWorldSprite("Northeast Candle", new Vector2(6.85f, 3.65f), Vector3.one * 0.85f, new Color(0.63f, 0.57f, 0.44f), new Color(1f, 0.56f, 0.15f), PixelSpriteKind.Candle, 2, false, Vector2.one, floorRoot.transform);
            RegisterSpriteAccent(northwestCandle, SpriteAccentAnimationKind.CandleFlicker, 0.15f);
            RegisterSpriteAccent(northeastCandle, SpriteAccentAnimationKind.CandleFlicker, 1.04f);

            if (floor.number == 3)
            {
                BuildFloorThreeStageArt(floorRoot.transform);
            }
            else if (floor.number == 4)
            {
                BuildFloorFourCombatArt(floorRoot.transform);
            }

            var goalIndex = 0;
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
                RegisterSpriteAccent(body, SpriteAccentAnimationKind.RuneIdle, goalIndex * 0.53f);
                goalIndex++;
            }

            foreach (var hazard in activeHazards)
            {
                var body = CreateWorldSprite(hazard.title, hazard.position, Vector3.one * hazard.radius, hazard.color, new Color(1f, 1f, 1f, 0.6f), PixelSpriteKind.Pulse, 1, false, Vector2.one, floorRoot.transform);
                hazard.body = body;
            }

            RegisterExistingElementalSprites();
        }

        private void BuildFloorThreeStageArt(Transform parent)
        {
            var definition = activeStageDefinition ?? FloorStageDefinition.CreateFallbackFloorThree();
            CreateStageBackdrop(definition, parent);
            foreach (var prop in definition.props ?? Array.Empty<StagePropDefinition>())
            {
                var body = CreateStageProp(prop, parent);
                if (prop.hasCollider)
                {
                    AddPlatformCollider(body, prop.size);
                }
            }

            foreach (var obstacle in definition.obstacles ?? Array.Empty<StageObstacleDefinition>())
            {
                AddStageGate(obstacle, parent);
            }

            var exitPortal = CreateWorldSprite(
                "Stage Exit Portal",
                new Vector2(definition.stageMax.x - 1.35f, 0.1f),
                Vector3.one * 1.2f,
                new Color(0.95f, 0.92f, 0.38f),
                floorController.Current.accentColor,
                PixelSpriteKind.Portal,
                3,
                false,
                Vector2.one,
                parent);
            RegisterSpriteAccent(exitPortal, SpriteAccentAnimationKind.PortalShimmer, 0.32f);
        }

        private void CreateBookcaseGuideArrow(string name, Vector2 bookcasePosition, Color accentColor, bool emphasized, Transform parent)
        {
            var anchor = bookcasePosition + new Vector2(-0.38f, 1.52f);
            var scale = emphasized ? 0.78f : 0.64f;
            var alpha = emphasized ? 1f : 0.68f;
            var primary = emphasized
                ? new Color(1f, 0.78f, 0.20f, 1f)
                : new Color(0.78f, 0.64f, 0.34f, 1f);
            var secondary = Color.Lerp(accentColor, Color.white, emphasized ? 0.54f : 0.34f);
            var body = CreateWorldSprite(name, anchor, Vector3.one * scale, primary, secondary, PixelSpriteKind.GuideArrow, 9, false, Vector2.one, parent);
            var phase = shelfGuideArrows.Count * 0.61f;
            shelfGuideArrows.Add(new FloatingGuideArrow(body, anchor, phase, scale, alpha));
        }

        private void TickShelfGuideArrows()
        {
            for (var index = shelfGuideArrows.Count - 1; index >= 0; index--)
            {
                var arrow = shelfGuideArrows[index];
                if (!arrow.IsActive)
                {
                    shelfGuideArrows.RemoveAt(index);
                    continue;
                }

                arrow.Tick(Time.time, Time.deltaTime);
            }
        }

        private void RegisterSpriteAccent(GameObject body, SpriteAccentAnimationKind kind, float phase, bool replaceExisting = true)
        {
            if (body == null)
            {
                return;
            }

            if (replaceExisting)
            {
                spriteAccentAnimations.RemoveAll(animation => animation.TargetEquals(body));
            }

            spriteAccentAnimations.Add(new SpriteAccentAnimation(body, kind, phase));
        }

        private void TickSpriteAccentAnimations()
        {
            for (var index = spriteAccentAnimations.Count - 1; index >= 0; index--)
            {
                var animation = spriteAccentAnimations[index];
                if (!animation.IsActive)
                {
                    spriteAccentAnimations.RemoveAt(index);
                    continue;
                }

                animation.Tick(Time.time, Time.deltaTime);
            }
        }

        private void CreateStageBackdrop(FloorStageDefinition definition, Transform parent)
        {
            var center = (definition.stageMin + definition.stageMax) * 0.5f;
            var size = definition.stageMax - definition.stageMin;
            CreateWorldSprite("Crossing Dungeon Backdrop", center, Vector3.one, new Color(0.045f, 0.052f, 0.067f), new Color(0.035f, 0.04f, 0.052f), PixelSpriteKind.FloorTile, -10, true, new Vector2(size.x, size.y), parent);
            CreateWorldSprite("Crossing Lower Abyss Wash", new Vector2(center.x, definition.stageMin.y + 0.82f), Vector3.one, new Color(0.020f, 0.024f, 0.035f, 1f), new Color(0.06f, 0.07f, 0.10f, 1f), PixelSpriteKind.Rubble, -9, true, new Vector2(size.x, 1.65f), parent);
            CreateWorldSprite("Crossing Distant Upper Ledge", new Vector2(center.x, 2.52f), Vector3.one, new Color(0.12f, 0.115f, 0.14f, 1f), new Color(0.30f, 0.25f, 0.22f, 1f), PixelSpriteKind.WallTrim, -8, true, new Vector2(size.x, 0.42f), parent);
            CreateWorldSprite("Crossing Distant Cliff Face", new Vector2(center.x, 1.75f), Vector3.one, new Color(0.070f, 0.066f, 0.082f, 1f), new Color(0.18f, 0.16f, 0.18f, 1f), PixelSpriteKind.CliffFace, -9, true, new Vector2(size.x, 1.20f), parent);
            CreateWorldSprite("Crossing North Wall", new Vector2(center.x, definition.stageMax.y - 0.55f), Vector3.one, new Color(0.20f, 0.19f, 0.23f), floorController.Current.accentColor, PixelSpriteKind.WallTrim, -5, true, new Vector2(size.x, 1.1f), parent);
            CreateWorldSprite("Crossing South Trim", new Vector2(center.x, definition.stageMin.y + 0.22f), Vector3.one, new Color(0.16f, 0.14f, 0.13f), new Color(0.45f, 0.36f, 0.22f), PixelSpriteKind.WallTrim, -2, true, new Vector2(size.x, 0.42f), parent);
            CreateStageRouteBoundaryCues(definition, parent);
            CreateWorldSprite("Crossing Reference Bookcase", definition.customReferencePosition, Vector3.one * 1.15f, new Color(0.42f, 0.23f, 0.12f), floorController.Current.accentColor, PixelSpriteKind.Bookshelf, 2, false, Vector2.one, parent);
            CreateBookcaseGuideArrow("Crossing Reference Bookcase Guide Arrow", definition.customReferencePosition, floorController.Current.accentColor, emphasized: true, parent);
            var crossingTorch = CreateWorldSprite("Crossing West Torch", definition.customReferencePosition + new Vector2(1.6f, 1.1f), Vector3.one * 0.78f, new Color(0.63f, 0.57f, 0.44f), new Color(1f, 0.56f, 0.15f), PixelSpriteKind.Candle, 4, false, Vector2.one, parent);
            RegisterSpriteAccent(crossingTorch, SpriteAccentAnimationKind.CandleFlicker, 1.72f);
        }

        private void CreateStageRouteBoundaryCues(FloorStageDefinition definition, Transform parent)
        {
            if (definition == null)
            {
                return;
            }

            var center = (definition.stageMin + definition.stageMax) * 0.5f;
            var size = definition.stageMax - definition.stageMin;
            var upperLipY = -1.78f;
            var upperWallTopY = definition.stageMax.y - 0.82f;
            var upperWallHeight = Mathf.Max(0.24f, upperWallTopY - upperLipY);
            var lowerLipY = definition.stageMin.y + 0.72f;
            var lowerBottomY = definition.stageMin.y + 0.10f;
            var lowerWallHeight = Mathf.Max(0.20f, lowerLipY - lowerBottomY);

            CreateWorldSprite("Crossing Route Upper Cliff Mass", new Vector2(center.x, (upperWallTopY + upperLipY) * 0.5f), Vector3.one, new Color(0.060f, 0.058f, 0.073f, 1f), new Color(0.18f, 0.16f, 0.17f, 1f), PixelSpriteKind.CliffFace, -9, true, new Vector2(size.x, upperWallHeight), parent);
            CreateWorldSprite("Crossing Route Upper Guard Wall", new Vector2(center.x, upperLipY), Vector3.one, new Color(0.18f, 0.16f, 0.15f, 1f), new Color(0.54f, 0.43f, 0.28f, 1f), PixelSpriteKind.WallTrim, -1, true, new Vector2(size.x, 0.22f), parent);
            CreateWorldSprite("Crossing Route Upper Foot Shadow", new Vector2(center.x, upperLipY - 0.17f), Vector3.one, new Color(0.035f, 0.032f, 0.038f, 1f), new Color(0.14f, 0.12f, 0.11f, 1f), PixelSpriteKind.WallTrim, -3, true, new Vector2(size.x, 0.10f), parent);
            CreateWorldSprite("Crossing Route Lower Drop Wall", new Vector2(center.x, (lowerLipY + lowerBottomY) * 0.5f), Vector3.one, new Color(0.028f, 0.027f, 0.035f, 1f), new Color(0.13f, 0.105f, 0.085f, 1f), PixelSpriteKind.CliffFace, -7, true, new Vector2(size.x, lowerWallHeight), parent);
            CreateWorldSprite("Crossing Route Lower Warning Edge", new Vector2(center.x, lowerLipY), Vector3.one, new Color(0.20f, 0.16f, 0.12f, 1f), new Color(0.66f, 0.45f, 0.24f, 1f), PixelSpriteKind.WallTrim, -1, true, new Vector2(size.x, 0.12f), parent);
            CreateWorldSprite("Crossing Route Lower Abyss Rim Shadow", new Vector2(center.x, lowerLipY - 0.22f), Vector3.one, new Color(0.012f, 0.013f, 0.020f, 1f), new Color(0.060f, 0.052f, 0.050f, 1f), PixelSpriteKind.WallTrim, -6, true, new Vector2(size.x, 0.16f), parent);
        }

        private GameObject CreateStageProp(StagePropDefinition prop, Transform parent)
        {
            var body = CreateWorldSprite(
                string.IsNullOrWhiteSpace(prop.title) ? "Stage Prop" : prop.title,
                prop.position,
                Vector3.one,
                prop.primaryColor,
                prop.secondaryColor,
                prop.spriteKind,
                prop.sortingOrder,
                prop.tiled,
                prop.size,
                parent);
            ApplySpriteOverride(body, prop.spriteOverride);
            CreateRaisedPlatformDepth(prop, parent);
            return body;
        }

        private void CreateRaisedPlatformDepth(StagePropDefinition prop, Transform parent)
        {
            if (prop == null || !prop.hasCollider || prop.spriteKind != PixelSpriteKind.FloorTile)
            {
                return;
            }

            var topY = prop.position.y + prop.size.y * 0.5f;
            var bottomY = prop.position.y - prop.size.y * 0.5f;
            CreateWorldSprite($"{prop.title} Top Highlight", new Vector2(prop.position.x, topY + 0.035f), Vector3.one, new Color(0.62f, 0.54f, 0.42f, 1f), new Color(0.92f, 0.78f, 0.52f, 1f), PixelSpriteKind.WallTrim, -1, true, new Vector2(prop.size.x + 0.14f, 0.08f), parent);
            CreateWorldSprite($"{prop.title} Underside", new Vector2(prop.position.x, bottomY - 0.35f), Vector3.one, new Color(0.085f, 0.074f, 0.070f, 1f), new Color(0.24f, 0.20f, 0.17f, 1f), PixelSpriteKind.CliffFace, -7, true, new Vector2(prop.size.x + 0.18f, 0.70f), parent);
            CreateWorldSprite($"{prop.title} Bottom Shadow", new Vector2(prop.position.x, bottomY - 0.74f), Vector3.one, new Color(0.025f, 0.026f, 0.034f, 1f), new Color(0.08f, 0.07f, 0.07f, 1f), PixelSpriteKind.WallTrim, -8, true, new Vector2(prop.size.x + 0.20f, 0.16f), parent);
            CreateWorldSprite($"{prop.title} Left Broken Edge", new Vector2(prop.position.x - prop.size.x * 0.5f - 0.05f, prop.position.y - 0.08f), Vector3.one, new Color(0.11f, 0.09f, 0.08f, 1f), new Color(0.42f, 0.32f, 0.22f, 1f), PixelSpriteKind.Rubble, -1, false, Vector2.one, parent);
            CreateWorldSprite($"{prop.title} Right Broken Edge", new Vector2(prop.position.x + prop.size.x * 0.5f + 0.05f, prop.position.y - 0.10f), Vector3.one, new Color(0.12f, 0.095f, 0.08f, 1f), new Color(0.46f, 0.34f, 0.24f, 1f), PixelSpriteKind.Rubble, -1, false, Vector2.one, parent);
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

        private void AddStageGate(StageObstacleDefinition obstacle, Transform parent)
        {
            CreateStageHazardVisualCues(obstacle, parent);
            var body = CreateWorldSprite(
                $"Stage Gate {obstacle.title}",
                obstacle.center,
                Vector3.one,
                obstacle.lockedColor,
                Color.Lerp(obstacle.lockedColor, Color.white, 0.22f),
                LockedSpriteKindForObstacle(obstacle),
                -3,
                true,
                obstacle.size,
                parent);
            activeStageGates.Add(new StageGate(obstacle.requiredGoalId, obstacle.center, obstacle.size, obstacle.resetPosition, obstacle.lockedNote, body));
        }

        private static PixelSpriteKind LockedSpriteKindForObstacle(StageObstacleDefinition obstacle)
        {
            return obstacle?.requiredGoalId switch
            {
                "frozen_river" => PixelSpriteKind.WaterHazard,
                "earth_stairs" => PixelSpriteKind.CliffFace,
                "living_bridge" => PixelSpriteKind.CliffFace,
                "wind_platform" => PixelSpriteKind.CliffFace,
                _ => obstacle == null ? PixelSpriteKind.FloorTile : obstacle.lockedSpriteKind
            };
        }

        private void CreateStageHazardVisualCues(StageObstacleDefinition obstacle, Transform parent)
        {
            if (obstacle == null)
            {
                return;
            }

            var id = obstacle.requiredGoalId ?? "";
            if (id == "frozen_river")
            {
                CreateVerticalObstacleCutaway("River Vertical Channel", obstacle, parent, PixelSpriteKind.WaterHazard, new Color(0.012f, 0.070f, 0.120f, 1f), new Color(0.18f, 0.36f, 0.66f, 1f), new Color(0.060f, 0.064f, 0.075f, 1f), new Color(0.22f, 0.19f, 0.15f, 1f), new Color(0.56f, 0.86f, 1f, 1f));
                CreateObstacleNoBypassCues("River", obstacle, parent, new Color(0.052f, 0.058f, 0.070f, 1f), new Color(0.18f, 0.17f, 0.16f, 1f), new Color(0.46f, 0.78f, 1f, 1f));
                CreateWorldSprite("River Lower Drop Shadow", obstacle.center + new Vector2(0f, -0.58f), Vector3.one, new Color(0.004f, 0.018f, 0.034f, 1f), new Color(0.035f, 0.11f, 0.20f, 1f), PixelSpriteKind.CliffFace, -8, true, obstacle.size + new Vector2(0.42f, 0.82f), parent);
                var riverDeep = CreateWorldSprite("River Deep Center", obstacle.center + new Vector2(0f, -0.10f), Vector3.one, new Color(0.02f, 0.12f, 0.22f, 1f), new Color(0.20f, 0.44f, 0.82f, 1f), PixelSpriteKind.WaterHazard, -4, true, obstacle.size + new Vector2(0.20f, 0.20f), parent);
                RegisterSpriteAccent(riverDeep, SpriteAccentAnimationKind.WaterFlow, 0.22f);
                CreateWorldSprite("River Bank Left Cliff Face", obstacle.center + new Vector2(-obstacle.size.x * 0.52f, -0.20f), Vector3.one, new Color(0.075f, 0.070f, 0.080f, 1f), new Color(0.22f, 0.19f, 0.15f, 1f), PixelSpriteKind.CliffFace, -2, true, new Vector2(0.26f, obstacle.size.y + 0.56f), parent);
                CreateWorldSprite("River Bank Right Cliff Face", obstacle.center + new Vector2(obstacle.size.x * 0.52f, -0.20f), Vector3.one, new Color(0.075f, 0.070f, 0.080f, 1f), new Color(0.22f, 0.19f, 0.15f, 1f), PixelSpriteKind.CliffFace, -2, true, new Vector2(0.26f, obstacle.size.y + 0.56f), parent);
                CreateWorldSprite("River Whitewater Edge North", obstacle.center + new Vector2(0f, obstacle.size.y * 0.42f), Vector3.one, new Color(0.72f, 0.92f, 1f, 1f), new Color(0.24f, 0.48f, 0.86f, 1f), PixelSpriteKind.WallTrim, -1, true, new Vector2(obstacle.size.x + 0.28f, 0.12f), parent);
                CreateWorldSprite("River Whitewater Edge South", obstacle.center + new Vector2(0f, -obstacle.size.y * 0.42f), Vector3.one, new Color(0.42f, 0.74f, 1f, 1f), new Color(0.08f, 0.25f, 0.46f, 1f), PixelSpriteKind.WallTrim, -1, true, new Vector2(obstacle.size.x + 0.18f, 0.10f), parent);
                var riverFlowA = CreateWorldSprite("River Flow Streak A", obstacle.center + new Vector2(-0.42f, 0.10f), Vector3.one, new Color(0.50f, 0.82f, 1f, 1f), Color.white, PixelSpriteKind.WindPlatformTile, -1, true, new Vector2(obstacle.size.x * 0.70f, 0.10f), parent);
                var riverFlowB = CreateWorldSprite("River Flow Streak B", obstacle.center + new Vector2(0.38f, -0.28f), Vector3.one, new Color(0.34f, 0.62f, 0.92f, 1f), Color.white, PixelSpriteKind.WindPlatformTile, -1, true, new Vector2(obstacle.size.x * 0.56f, 0.08f), parent);
                RegisterSpriteAccent(riverFlowA, SpriteAccentAnimationKind.WaterFlow, 0.78f);
                RegisterSpriteAccent(riverFlowB, SpriteAccentAnimationKind.WaterFlow, 1.44f);
                CreateStageRim("River Bank Warning", obstacle.center, obstacle.size + new Vector2(0.18f, 0.10f), new Color(0.56f, 0.86f, 1f, 1f), parent);
                return;
            }

            if (id == "earth_stairs")
            {
                CreateVerticalObstacleCutaway("Broken Floor Vertical Rupture", obstacle, parent, PixelSpriteKind.CliffFace, new Color(0.010f, 0.007f, 0.005f, 1f), new Color(0.090f, 0.055f, 0.032f, 1f), new Color(0.135f, 0.095f, 0.065f, 1f), new Color(0.70f, 0.48f, 0.26f, 1f), new Color(0.86f, 0.58f, 0.28f, 1f));
                CreateObstacleNoBypassCues("Broken Floor", obstacle, parent, new Color(0.075f, 0.052f, 0.036f, 1f), new Color(0.46f, 0.31f, 0.18f, 1f), new Color(0.86f, 0.58f, 0.28f, 1f));
                CreateWorldSprite("Broken Floor Lower Void", obstacle.center + new Vector2(0f, -0.58f), Vector3.one, new Color(0.006f, 0.004f, 0.004f, 1f), new Color(0.050f, 0.032f, 0.022f, 1f), PixelSpriteKind.CliffFace, -8, true, obstacle.size + new Vector2(0.58f, 0.90f), parent);
                CreateWorldSprite("Broken Floor Pit Shadow", obstacle.center + new Vector2(0f, -0.15f), Vector3.one, new Color(0.025f, 0.018f, 0.014f, 1f), new Color(0.11f, 0.075f, 0.05f, 1f), PixelSpriteKind.Rubble, -5, true, obstacle.size + new Vector2(0.36f, 0.22f), parent);
                CreateWorldSprite("Broken Floor Inner Void", obstacle.center + new Vector2(0f, -0.02f), Vector3.one, new Color(0.012f, 0.008f, 0.006f, 1f), new Color(0.09f, 0.055f, 0.032f, 1f), PixelSpriteKind.CliffFace, -3, true, obstacle.size + new Vector2(-0.18f, -0.12f), parent);
                CreateWorldSprite("Broken Floor Fill Silhouette", obstacle.solutionPosition, Vector3.one, new Color(0.10f, 0.065f, 0.040f, 1f), new Color(0.82f, 0.62f, 0.34f, 1f), PixelSpriteKind.EarthStep, -2, true, obstacle.solutionSize + new Vector2(0.35f, 0.18f), parent);
                CreateStageRim("Broken Floor Warning", obstacle.center, obstacle.size + new Vector2(0.26f, 0.16f), new Color(0.86f, 0.58f, 0.28f, 1f), parent);
                CreateWorldSprite("Broken Floor North Jagged Lip", obstacle.center + new Vector2(0f, obstacle.size.y * 0.50f), Vector3.one, new Color(0.24f, 0.18f, 0.12f, 1f), new Color(0.88f, 0.66f, 0.36f, 1f), PixelSpriteKind.CliffFace, -1, true, new Vector2(obstacle.size.x + 0.30f, 0.20f), parent);
                CreateWorldSprite("Broken Floor South Jagged Lip", obstacle.center + new Vector2(0f, -obstacle.size.y * 0.47f), Vector3.one, new Color(0.16f, 0.105f, 0.065f, 1f), new Color(0.66f, 0.44f, 0.24f, 1f), PixelSpriteKind.CliffFace, -1, true, new Vector2(obstacle.size.x + 0.22f, 0.18f), parent);
                CreateWorldSprite("Broken Floor Rubble Left", obstacle.center + new Vector2(-obstacle.size.x * 0.42f, obstacle.size.y * 0.30f), Vector3.one * 0.44f, new Color(0.50f, 0.36f, 0.22f, 1f), new Color(0.86f, 0.66f, 0.38f, 1f), PixelSpriteKind.Rubble, -1, false, Vector2.one, parent);
                CreateWorldSprite("Broken Floor Rubble Right", obstacle.center + new Vector2(obstacle.size.x * 0.38f, -obstacle.size.y * 0.24f), Vector3.one * 0.36f, new Color(0.45f, 0.30f, 0.18f, 1f), new Color(0.78f, 0.56f, 0.32f, 1f), PixelSpriteKind.Rubble, -1, false, Vector2.one, parent);
                return;
            }

            if (id == "living_bridge")
            {
                CreateVerticalObstacleCutaway("Chasm Vertical Shaft", obstacle, parent, PixelSpriteKind.CliffFace, new Color(0.004f, 0.005f, 0.012f, 1f), new Color(0.050f, 0.052f, 0.075f, 1f), new Color(0.090f, 0.070f, 0.095f, 1f), new Color(0.36f, 0.27f, 0.44f, 1f), new Color(0.64f, 0.34f, 0.95f, 1f));
                CreateObstacleNoBypassCues("Chasm", obstacle, parent, new Color(0.048f, 0.044f, 0.060f, 1f), new Color(0.27f, 0.21f, 0.32f, 1f), new Color(0.64f, 0.34f, 0.95f, 1f));
                CreateWorldSprite("Chasm Far Abyss", obstacle.center + new Vector2(0f, -0.88f), Vector3.one, new Color(0.002f, 0.003f, 0.008f, 1f), new Color(0.025f, 0.026f, 0.040f, 1f), PixelSpriteKind.CliffFace, -9, true, obstacle.size + new Vector2(0.90f, 1.95f), parent);
                CreateWorldSprite("Chasm Depth", obstacle.center + new Vector2(0f, -0.45f), Vector3.one, new Color(0.010f, 0.012f, 0.020f, 1f), new Color(0.06f, 0.07f, 0.11f, 1f), PixelSpriteKind.Rubble, -7, true, obstacle.size + new Vector2(0.50f, 1.25f), parent);
                CreateWorldSprite("Chasm Left Cliff Wall", obstacle.center + new Vector2(-obstacle.size.x * 0.54f, -0.36f), Vector3.one, new Color(0.075f, 0.060f, 0.078f, 1f), new Color(0.36f, 0.27f, 0.44f, 1f), PixelSpriteKind.CliffFace, -2, true, new Vector2(0.34f, obstacle.size.y + 1.02f), parent);
                CreateWorldSprite("Chasm Right Cliff Wall", obstacle.center + new Vector2(obstacle.size.x * 0.54f, -0.36f), Vector3.one, new Color(0.075f, 0.060f, 0.078f, 1f), new Color(0.36f, 0.27f, 0.44f, 1f), PixelSpriteKind.CliffFace, -2, true, new Vector2(0.34f, obstacle.size.y + 1.02f), parent);
                CreateStageRim("Chasm Warning", obstacle.center, obstacle.size + new Vector2(0.34f, 0.22f), new Color(0.64f, 0.34f, 0.95f, 1f), parent);
                CreateWorldSprite("Chasm North Broken Lip", obstacle.center + new Vector2(0f, obstacle.size.y * 0.52f), Vector3.one, new Color(0.16f, 0.12f, 0.14f, 1f), new Color(0.50f, 0.36f, 0.55f, 1f), PixelSpriteKind.CliffFace, -2, true, new Vector2(obstacle.size.x + 0.55f, 0.28f), parent);
                CreateWorldSprite("Chasm South Broken Lip", obstacle.center + new Vector2(0f, -obstacle.size.y * 0.52f), Vector3.one, new Color(0.10f, 0.08f, 0.10f, 1f), new Color(0.42f, 0.30f, 0.48f, 1f), PixelSpriteKind.CliffFace, -2, true, new Vector2(obstacle.size.x + 0.55f, 0.24f), parent);
                var chasmMist = CreateWorldSprite("Chasm Bottom Mist", obstacle.center + new Vector2(0.18f, -obstacle.size.y * 0.62f), Vector3.one, new Color(0.12f, 0.14f, 0.20f, 1f), new Color(0.32f, 0.30f, 0.44f, 1f), PixelSpriteKind.WindPlatformTile, -1, true, new Vector2(obstacle.size.x * 0.78f, 0.10f), parent);
                RegisterSpriteAccent(chasmMist, SpriteAccentAnimationKind.MistDrift, 0.46f);
                return;
            }

            if (id == "wind_platform")
            {
                CreateVerticalObstacleCutaway("Wind Gap Vertical Shaft", obstacle, parent, PixelSpriteKind.CliffFace, new Color(0.006f, 0.018f, 0.024f, 1f), new Color(0.070f, 0.145f, 0.175f, 1f), new Color(0.055f, 0.074f, 0.080f, 1f), new Color(0.24f, 0.36f, 0.40f, 1f), new Color(0.76f, 0.94f, 1f, 1f));
                CreateObstacleNoBypassCues("Wind Gap", obstacle, parent, new Color(0.036f, 0.058f, 0.064f, 1f), new Color(0.18f, 0.30f, 0.34f, 1f), new Color(0.76f, 0.94f, 1f, 1f));
                CreateWorldSprite("Wind Gap Lower Depth", obstacle.center + new Vector2(0f, -0.62f), Vector3.one, new Color(0.006f, 0.014f, 0.018f, 1f), new Color(0.052f, 0.12f, 0.14f, 1f), PixelSpriteKind.CliffFace, -9, true, obstacle.size + new Vector2(0.55f, 1.10f), parent);
                CreateWorldSprite("Wind Gap Void", obstacle.center + new Vector2(0f, -0.16f), Vector3.one, new Color(0.018f, 0.032f, 0.038f, 1f), new Color(0.11f, 0.22f, 0.27f, 1f), PixelSpriteKind.Rubble, -7, true, obstacle.size + new Vector2(0.28f, 0.42f), parent);
                CreateWorldSprite("Wind Gap Left Drop Face", obstacle.center + new Vector2(-obstacle.size.x * 0.52f, -0.20f), Vector3.one, new Color(0.055f, 0.074f, 0.080f, 1f), new Color(0.24f, 0.36f, 0.40f, 1f), PixelSpriteKind.CliffFace, -2, true, new Vector2(0.26f, obstacle.size.y + 0.62f), parent);
                CreateWorldSprite("Wind Gap Right Drop Face", obstacle.center + new Vector2(obstacle.size.x * 0.52f, -0.20f), Vector3.one, new Color(0.055f, 0.074f, 0.080f, 1f), new Color(0.24f, 0.36f, 0.40f, 1f), PixelSpriteKind.CliffFace, -2, true, new Vector2(0.26f, obstacle.size.y + 0.62f), parent);
                CreateStageRim("Wind Gap Warning", obstacle.center, obstacle.size + new Vector2(0.22f, 0.18f), new Color(0.76f, 0.94f, 1f, 1f), parent);
                var windGuideUpper = CreateWorldSprite("Wind Guide Upper", obstacle.center + new Vector2(-0.15f, obstacle.size.y * 0.30f), Vector3.one, new Color(0.54f, 0.80f, 0.92f, 1f), Color.white, PixelSpriteKind.WindPlatformTile, -1, true, new Vector2(obstacle.size.x * 0.88f, 0.18f), parent);
                var windGuideLower = CreateWorldSprite("Wind Guide Lower", obstacle.center + new Vector2(0.22f, -obstacle.size.y * 0.24f), Vector3.one, new Color(0.38f, 0.62f, 0.74f, 1f), Color.white, PixelSpriteKind.WindPlatformTile, -1, true, new Vector2(obstacle.size.x * 0.70f, 0.14f), parent);
                var windGapMist = CreateWorldSprite("Wind Gap Mist", obstacle.center + new Vector2(0.06f, -obstacle.size.y * 0.55f), Vector3.one, new Color(0.33f, 0.58f, 0.68f, 1f), Color.white, PixelSpriteKind.WindPlatformTile, -1, true, new Vector2(obstacle.size.x * 0.82f, 0.10f), parent);
                RegisterSpriteAccent(windGuideUpper, SpriteAccentAnimationKind.MistDrift, 1.08f);
                RegisterSpriteAccent(windGuideLower, SpriteAccentAnimationKind.MistDrift, 1.64f);
                RegisterSpriteAccent(windGapMist, SpriteAccentAnimationKind.MistDrift, 2.18f);
            }
        }

        private void CreateVerticalObstacleCutaway(
            string name,
            StageObstacleDefinition obstacle,
            Transform parent,
            PixelSpriteKind coreKind,
            Color corePrimary,
            Color coreSecondary,
            Color wallPrimary,
            Color wallSecondary,
            Color rimColor)
        {
            var stage = activeStageDefinition ?? FloorStageDefinition.CreateFallbackFloorThree();
            var topY = Mathf.Min(stage.stageMax.y - 1.15f, obstacle.center.y + 3.10f);
            var bottomY = Mathf.Max(stage.stageMin.y + 0.35f, obstacle.center.y - obstacle.size.y * 0.72f);
            var height = Mathf.Max(obstacle.size.y + 1.85f, topY - bottomY);
            var centerY = (topY + bottomY) * 0.5f;
            var width = obstacle.size.x + 0.62f;
            var center = new Vector2(obstacle.center.x, centerY);

            CreateWorldSprite($"{name} Core", center, Vector3.one, corePrimary, coreSecondary, coreKind, -8, true, new Vector2(width, height), parent);
            CreateWorldSprite($"{name} Left Wall", center + new Vector2(-width * 0.5f, 0f), Vector3.one, wallPrimary, wallSecondary, PixelSpriteKind.CliffFace, -6, true, new Vector2(0.30f, height + 0.36f), parent);
            CreateWorldSprite($"{name} Right Wall", center + new Vector2(width * 0.5f, 0f), Vector3.one, wallPrimary, wallSecondary, PixelSpriteKind.CliffFace, -6, true, new Vector2(0.30f, height + 0.36f), parent);
            CreateWorldSprite($"{name} Upper Break", new Vector2(obstacle.center.x, topY), Vector3.one, wallSecondary, rimColor, PixelSpriteKind.WallTrim, -5, true, new Vector2(width + 0.45f, 0.22f), parent);
            CreateWorldSprite($"{name} Lower Break", new Vector2(obstacle.center.x, bottomY), Vector3.one, wallPrimary, rimColor, PixelSpriteKind.WallTrim, -5, true, new Vector2(width + 0.35f, 0.18f), parent);
        }

        private void CreateObstacleNoBypassCues(string name, StageObstacleDefinition obstacle, Transform parent, Color wallPrimary, Color wallSecondary, Color rimColor)
        {
            var stage = activeStageDefinition ?? FloorStageDefinition.CreateFallbackFloorThree();
            var topY = Mathf.Min(stage.stageMax.y - 1.15f, obstacle.center.y + 3.10f);
            var bottomY = Mathf.Max(stage.stageMin.y + 0.35f, obstacle.center.y - obstacle.size.y * 0.72f);
            var stageTopY = stage.stageMax.y - 0.82f;
            var stageBottomY = stage.stageMin.y + 0.10f;
            var width = obstacle.size.x + 0.92f;
            var upperHeight = Mathf.Max(0.24f, stageTopY - topY);
            var lowerHeight = Mathf.Max(0.20f, bottomY - stageBottomY);
            var shadedWall = Color.Lerp(wallPrimary, Color.black, 0.18f);
            var brightRim = Color.Lerp(rimColor, Color.white, 0.26f);

            CreateWorldSprite($"{name} No Bypass Upper Wall", new Vector2(obstacle.center.x, (stageTopY + topY) * 0.5f), Vector3.one, shadedWall, wallSecondary, PixelSpriteKind.CliffFace, -8, true, new Vector2(width, upperHeight), parent);
            CreateWorldSprite($"{name} No Bypass Upper Lip", new Vector2(obstacle.center.x, topY + 0.10f), Vector3.one, wallSecondary, brightRim, PixelSpriteKind.WallTrim, -2, true, new Vector2(width + 0.34f, 0.16f), parent);
            CreateWorldSprite($"{name} No Bypass Lower Drop", new Vector2(obstacle.center.x, (bottomY + stageBottomY) * 0.5f), Vector3.one, Color.Lerp(wallPrimary, Color.black, 0.32f), wallSecondary, PixelSpriteKind.CliffFace, -8, true, new Vector2(width, lowerHeight), parent);
            CreateWorldSprite($"{name} No Bypass Lower Lip", new Vector2(obstacle.center.x, bottomY - 0.08f), Vector3.one, wallPrimary, brightRim, PixelSpriteKind.WallTrim, -2, true, new Vector2(width + 0.26f, 0.14f), parent);
        }

        private void CreateStageRim(string name, Vector2 center, Vector2 size, Color color, Transform parent)
        {
            var rimColor = new Color(color.r, color.g, color.b, 0.95f);
            var glowColor = Color.Lerp(color, Color.white, 0.40f);
            CreateWorldSprite($"{name} North", center + new Vector2(0f, size.y * 0.5f), Vector3.one, rimColor, glowColor, PixelSpriteKind.WallTrim, -1, true, new Vector2(size.x, 0.10f), parent);
            CreateWorldSprite($"{name} South", center + new Vector2(0f, -size.y * 0.5f), Vector3.one, rimColor, glowColor, PixelSpriteKind.WallTrim, -1, true, new Vector2(size.x, 0.10f), parent);
            CreateWorldSprite($"{name} West", center + new Vector2(-size.x * 0.5f, 0f), Vector3.one, rimColor, glowColor, PixelSpriteKind.WallTrim, -1, true, new Vector2(0.10f, size.y), parent);
            CreateWorldSprite($"{name} East", center + new Vector2(size.x * 0.5f, 0f), Vector3.one, rimColor, glowColor, PixelSpriteKind.WallTrim, -1, true, new Vector2(0.10f, size.y), parent);
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
                goal.entityBody = dummy;
            }
        }

        private void OnSpellBuffered(List<List<StrokeSample>> strokes, Vector2 center, int strokeCount)
        {
            if (HasEndingReport)
            {
                return;
            }

            if (strokeCount > 0)
            {
                playerAnimator?.PlayCast();
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
            ShowMagicNote("입력을 취소했습니다. 우클릭 hold로 다시 그리세요.", MentorMood.Frown);
        }

        private void ShowMagicNote(string text, MentorMood mentorMood)
        {
            magicNote.Show(text, InferNoteCategory(text), CurrentFloorNumber);
            mentor?.Say(mentorMood, text);
            if (!string.IsNullOrWhiteSpace(text))
            {
                audioDirector?.PlaySfx(AudioCue.NoteUnlock, 0.42f);
                audioDirector?.PlaySfx(AudioCue.NpcAppear, mentorMood == MentorMood.Neutral ? 0.18f : 0.28f);
            }
        }

        private static MagicNoteCategory InferNoteCategory(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return MagicNoteCategory.Discovery;
            }

            if (text.StartsWith("노트:", StringComparison.Ordinal) ||
                text.Contains("다음 목표", StringComparison.Ordinal) ||
                text.Contains("목표:", StringComparison.Ordinal) ||
                text.Contains("완료", StringComparison.Ordinal) ||
                text.Contains("통과", StringComparison.Ordinal))
            {
                return MagicNoteCategory.FloorNote;
            }

            return MagicNoteCategory.Discovery;
        }

        private void ShowToast(string message, Color accent, bool strong = false)
        {
            if (toastPanel == null || toastText == null)
            {
                return;
            }

            toastText.text = message ?? "";
            toastText.color = Color.Lerp(accent, Color.white, 0.34f);
            if (toastAccent != null)
            {
                toastAccent.color = accent;
            }

            if (toastBackground != null)
            {
                toastBackground.color = strong
                    ? Color.Lerp(new Color(0.018f, 0.024f, 0.038f, 0.96f), accent, 0.18f)
                    : new Color(0.018f, 0.024f, 0.038f, 0.94f);
            }

            toastTtl = strong ? 2.35f : 1.65f;
            toastPanel.gameObject.SetActive(!string.IsNullOrWhiteSpace(message));
        }

        private void TickToast()
        {
            if (toastPanel == null || !toastPanel.gameObject.activeSelf)
            {
                return;
            }

            toastTtl -= Time.deltaTime;
            if (toastTtl <= 0f)
            {
                toastPanel.gameObject.SetActive(false);
            }
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
            castsOnCurrentFloor++;
            var now = Time.time;
            var hadActiveSeal = HasActiveSeal(now);
            if (hadActiveSeal)
            {
                MarkPostSealInputSeen(now);
            }

            var castCenter = CurrentMagicCastOrigin(session.GetWorldCenter());
            var baseIntent = ResolveBaseIntent(castCenter);
            var recognition = recognitionService.Recognize(session, new RecognitionContext
            {
                activeSeals = seals.Select(view => view.seal).ToList(),
                baseIntent = baseIntent,
                customShapesOnlyWhenSealActive = true,
                hasCastCenter = true,
                castCenter = castCenter,
                now = now
            });
            LastPersonalizationSummaryForTests = recognition.personalization ?? TutorialPersonalizationSummary.Empty;
            if (hadActiveSeal &&
                !IsCustomShapeBaseGoalInput(recognition) &&
                TryApplyCustomShapeFollowup(recognition, out var customFollowup))
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

        private bool IsCustomShapeBaseGoalInput(StrokeRecognitionResult recognition)
        {
            if (recognition.kind != StrokeRecognitionKind.Base ||
                recognition.baseResult?.spell?.isCustomShape != true)
            {
                return false;
            }

            var spell = recognition.baseResult.spell;
            var family = spell.recognizedFamily ?? spell.mappedFamily ?? spell.targetFamily;
            var castCenter = CurrentMagicCastOrigin(recognition.center);
            return activeGoals.Any(goal =>
                !goal.completed &&
                goal.requiresCustomShape &&
                !goal.requiredCustomSpell.HasValue &&
                goal.MatchesBase(family, castCenter));
        }

        private bool TryApplyCustomShapeFollowup(StrokeRecognitionResult recognition, out ProcessedSpell processed)
        {
            processed = null;
            if (recognition.kind != StrokeRecognitionKind.Base ||
                recognition.baseResult?.spell?.isCustomShape != true)
            {
                return false;
            }

            var castCenter = CurrentMagicCastOrigin(recognition.center);
            var sealView = FindAttachableSeal(castCenter);
            if (sealView == null)
            {
                CurrentAssistLevel = 1;
                LastHintText = "커스텀 도형은 먼저 만든 기본 문양의 빛나는 원 안에 얹어야 합니다.";
                endingReport.RecordHintShown(1);
                ShowMagicNote(LastHintText, MentorMood.Frown);
                ShowBaseResultSummary(recognition.baseResult, "커스텀 부착 실패", LastHintText);
                LogBaseAttempt(recognition.baseResult, null, "custom_followup_detached");
                processed = new ProcessedSpell { baseResult = recognition.baseResult };
                return true;
            }

            processed = ApplyCustomShapeFollowup(sealView, recognition.baseResult, castCenter);
            return true;
        }

        private ProcessedSpell ApplyCustomShapeFollowup(SealView sealView, BaseRecognitionResult result, Vector2 center)
        {
            var seal = sealView.seal;
            var customEffect = ResolveCustomSpellEffect(seal.baseFamily, result.spell);
            if (!customEffect.IsValid)
            {
                CurrentAssistLevel = 1;
                LastHintText =
                    $"{SpellLabels.Korean(seal.baseFamily)} 문양 위에서 지금 도형 조합은 특별한 반응을 만들지 못했습니다.\n" +
                    "표식에 적힌 조합처럼 기본 문양을 먼저 만들고, 그 위에 맞는 커스텀 도형을 얹으세요.";
                endingReport.RecordHintShown(1);
                ShowMagicNote(LastHintText, MentorMood.Frown);
                ShowBaseResultSummary(result, "커스텀 반응 실패", LastHintText);
                pulses.Add(new ParticlePulse(center, FamilyColor(seal.baseFamily), weak: true));
                LogBaseAttempt(result, seal, "custom_effect_unmatched");
                return new ProcessedSpell { baseResult = result };
            }

            var goalEffect = ApplyCustomSpellToGoals(seal, customEffect, center, result.spell);
            var eventNote = ApplyCustomShapeEvent(result, seal, center);
            var elementalNote = ApplyElementalInteractions(seal.baseFamily, result.spell, center, result.spell.customEventDirection, customEffect.displayName);
            var note = $"{SpellLabels.Korean(seal.baseFamily)} 문양에 {result.spell.customShapeLabel}을 얹었습니다.\n{customEffect.note}";
            if (!string.IsNullOrWhiteSpace(goalEffect.note))
            {
                note += $"\n{goalEffect.note}";
            }

            if (!string.IsNullOrWhiteSpace(eventNote))
            {
                note += $"\n{eventNote}";
            }

            if (!string.IsNullOrWhiteSpace(elementalNote))
            {
                note += $"\n{elementalNote}";
            }

            CurrentAssistLevel = 0;
            LastHintText = "";
            ShowMagicNote(note, MentorMood.Happy);
            ShowBaseResultSummary(result, $"{customEffect.displayName} 반응", note);
            pulses.Add(new ParticlePulse(center, FamilyColor(seal.baseFamily)));
            LogBaseAttempt(result, seal, $"{goalEffect.worldEffect}|{customEffect.kind}");
            EvaluateFloorCompletion();
            ConsumeSeal(sealView);
            return new ProcessedSpell { baseResult = result };
        }

        private CustomSpellEffectDefinition ResolveCustomSpellEffect(SpellFamily baseFamily, SpellResult spell)
        {
            if (activeStageDefinition != null &&
                activeStageDefinition.TryResolveEffect(baseFamily, spell, out var stageEffect))
            {
                return stageEffect.ToRuntimeDefinition();
            }

            return CustomSpellEffectCatalog.Resolve(baseFamily, spell);
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
            castsOnCurrentFloor++;
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
                SpellCastOutcomeKind.OverlayNoActiveSeal => ApplyOverlayNoActiveSeal(outcome),
                SpellCastOutcomeKind.OverlaySucceeded => ApplyOverlaySuccess(outcome),
                SpellCastOutcomeKind.DetachedOverlay => ApplyDetachedOverlay(outcome),
                _ => throw new ArgumentOutOfRangeException(nameof(outcome.kind), outcome.kind, "Unhandled spell cast outcome.")
            };
        }

        private ProcessedSpell ApplyBaseFailure(SpellCastOutcome outcome)
        {
            var baseResult = outcome.baseResult;
            var feedbackFamily = baseResult.spell.recognizedFamily ?? baseResult.spell.targetFamily;
            var priorFailures = Mathf.Max(GetBaseFailureCount(feedbackFamily), MagicExamSettings.ObserverMode ? 1 : 0);
            var hintState = HintAssistance.ForAttempt(feedbackFamily, priorFailures, false, baseResult.spell);
            baseFailureCounts[feedbackFamily] = priorFailures + 1;
            CurrentAssistLevel = hintState.AssistLevelNumber;
            LastHintText = hintState.body;
            endingReport.RecordAssist(hintState);
            ShowMagicNote(BuildBaseFailureNote(baseResult.spell, hintState), MentorMood.Frown);
            audioDirector?.PlaySfx(baseResult.spell.status == RecognitionStatus.Incomplete ? AudioCue.CastIncomplete : AudioCue.CastInvalid);
            worldDrawing?.MarkLastBufferedStrokesInvalid();
            ShowToast("문양 불안정 - 노트를 확인", new Color(0.92f, 0.72f, 0.34f));
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
            discoveredFamilies.Add(seal.baseFamily);
            var effect = ApplyBaseToGoals(baseResult, seal.baseFamily, outcome.center);
            var customEventNote = ApplyCustomShapeEvent(baseResult, seal, outcome.center);
            var elementalNote = ApplyElementalInteractions(seal.baseFamily, baseResult.spell, outcome.center, baseResult.spell.customEventDirection, "base");
            var eventEffect = string.IsNullOrWhiteSpace(customEventNote)
                ? effect
                : new GoalEffect($"{effect.note}\n{customEventNote}", $"{effect.worldEffect}|{baseResult.spell.customEventId}");
            if (!string.IsNullOrWhiteSpace(elementalNote))
            {
                eventEffect = new GoalEffect($"{eventEffect.note}\n{elementalNote}", $"{eventEffect.worldEffect}|elemental");
            }
            ShowMagicNote(BuildBaseSuccessNote(seal, eventEffect, successHintState), MentorMood.Happy);
            audioDirector?.PlayBaseSuccess(seal.baseFamily, seal.quality);
            worldDrawing?.MarkLastBufferedStrokesRecognized(FamilyColor(seal.baseFamily));
            var baseToastStrong = eventEffect.worldEffect != "base_off_target" && eventEffect.worldEffect != "seal_only";
            var toastMessage = eventEffect.worldEffect == "base_off_target"
                ? $"{SpellLabels.Korean(seal.baseFamily)} 인식 - 목표 근처로 이동"
                : baseToastStrong ? "목표 반응 적용" : $"{SpellLabels.Korean(seal.baseFamily)} seal 생성";
            ShowToast(toastMessage, eventEffect.worldEffect == "base_off_target" ? new Color(0.92f, 0.72f, 0.34f) : FamilyColor(seal.baseFamily), baseToastStrong);
            ShowBaseResultSummary(baseResult, "base 성공", resultSummary: eventEffect.note);
            pulses.Add(new ParticlePulse(outcome.center, FamilyColor(seal.baseFamily)));
            GlowPulse.Flash(outcome.center, FamilyColor(seal.baseFamily), 1.9f, 25);
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
            ShowMagicNote(BuildOverlayFailureNote(result, seal), MentorMood.Frown);
            audioDirector?.PlaySfx(result.recognizedOperator == OverlayOperator.MartialAxis && !seal.overlayStack.Contains(OverlayOperator.VoidCut)
                ? AudioCue.CastDependencyMissing
                : result.status == RecognitionStatus.Incomplete ? AudioCue.CastIncomplete : AudioCue.CastInvalid);
            worldDrawing?.MarkLastBufferedStrokesInvalid();
            ShowToast("overlay 불안정 - seal 위치 확인", new Color(0.92f, 0.72f, 0.34f));
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
            ShowMagicNote($"{SpellLabels.Korean(op)} 장식은 이미 이 seal에 붙어 있습니다.", MentorMood.Frown);
            audioDirector?.PlaySfx(AudioCue.CastInvalid);
            worldDrawing?.MarkLastBufferedStrokesInvalid();
            ShowToast("중복 overlay - 다른 장식 필요", OverlayColor(op));
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
            ShowMagicNote($"하나의 seal에는 overlay를 {SpellCastingService.MaxOverlayStack}개까지만 안정적으로 붙일 수 있습니다.", MentorMood.Frown);
            audioDirector?.PlaySfx(AudioCue.CastInvalid);
            worldDrawing?.MarkLastBufferedStrokesInvalid();
            ShowToast("overlay stack full - 새 base 필요", OverlayColor(op));
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
            discoveredOverlays.Add(op);
            var effect = ApplyOverlayToGoals(seal, op, outcome.center);
            CurrentAssistLevel = 0;
            LastHintText = "";
            ShowMagicNote(BuildOverlaySuccessNote(seal, op, effect), MentorMood.Happy);
            audioDirector?.PlayOverlaySuccess(op);
            worldDrawing?.MarkLastBufferedStrokesRecognized(OverlayColor(op));
            var overlayToastStrong = effect.worldEffect != "overlay_stack";
            ShowToast(overlayToastStrong ? "목표 반응 적용" : $"{SpellLabels.Korean(op)} overlay 연결", OverlayColor(op), overlayToastStrong);
            ShowOverlayResultSummary(result, seal, "overlay 성공", effect.note);
            LogOverlayAttempt(result, seal, outcome.center, outcome.strokeCount, effect.worldEffect);
            pulses.Add(new ParticlePulse(outcome.center, OverlayColor(op)));
            EvaluateFloorCompletion();
            return new ProcessedSpell { overlayResult = result };
        }

        private ProcessedSpell ApplyOverlayNoActiveSeal(SpellCastOutcome outcome)
        {
            var result = outcome.overlayResult;
            result.status = RecognitionStatus.Invalid;
            if (string.IsNullOrWhiteSpace(result.feedbackReason))
            {
                result.feedbackReason = "활성 base seal이 없습니다.";
            }

            CurrentAssistLevel = 1;
            LastHintText = "먼저 base 문양으로 seal을 만든 뒤, 그 원 안쪽이나 가장자리 바로 옆에 장식을 붙여 보세요.";
            endingReport.RecordHintShown(1);
            ShowMagicNote($"노트: {result.feedbackReason}\n다음: {LastHintText}", MentorMood.Frown);
            audioDirector?.PlaySfx(AudioCue.CastDependencyMissing);
            worldDrawing?.MarkLastBufferedStrokesInvalid();
            ShowToast("base seal 먼저", new Color(0.92f, 0.72f, 0.34f), strong: true);
            ShowOverlayNoSealResultSummary(result, "overlay 부착 실패", LastHintText);
            pulses.Add(new ParticlePulse(outcome.center, new Color(0.75f, 0.75f, 0.82f), weak: true));
            LogOverlayNoSealAttempt(result, outcome.center, outcome.strokeCount, "overlay_no_active_seal");
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
            ShowMagicNote(BuildDetachedOverlayFailureNote(result, seal), MentorMood.Frown);
            audioDirector?.PlaySfx(AudioCue.CastInvalid);
            worldDrawing?.MarkLastBufferedStrokesInvalid();
            ShowToast("seal 가까이 다시 그리기", new Color(0.92f, 0.72f, 0.34f));
            ShowOverlayResultSummary(result, seal, "overlay 거리 오류", LastHintText);
            pulses.Add(new ParticlePulse(outcome.center, new Color(0.75f, 0.75f, 0.82f), weak: true));
            LogOverlayAttempt(result, seal, outcome.center, outcome.strokeCount, "detached_overlay");
            return new ProcessedSpell { overlayResult = result };
        }

        private void ShowBaseResultSummary(BaseRecognitionResult result, string title, string resultSummary)
        {
            SetQuestScrollCollapsed(true, immediate: true);
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
            resultPanel.SetAsLastSibling();
        }

        private void ShowOverlayResultSummary(OverlayRecognitionResult result, CompiledSeal seal, string title, string resultSummary)
        {
            SetQuestScrollCollapsed(true, immediate: true);
            UpdateResultPanelLayout();
            var op = result.recognizedOperator.HasValue ? SpellLabels.Korean(result.recognizedOperator.Value) : "미확정";
            resultText.text =
                $"{title}: {op}\n" +
                $"대상 seal: {ShortLine(seal.Label, ResultLineLength(30, 24))}\n" +
                $"판정 {StatusLabel(result.status)}  점수 {Percent(result.score)}  모양 {Percent(result.shapeConfidence)}\n" +
                $"크기 {result.scaleRatio:0.00}x  위치 {AnchorLabel(result.anchorZone)}\n" +
                $"다음: {ShortLine(resultSummary, ResultLineLength(56, 46))}";
            resultPanel.gameObject.SetActive(true);
            resultPanel.SetAsLastSibling();
        }

        private void ShowOverlayNoSealResultSummary(OverlayRecognitionResult result, string title, string resultSummary)
        {
            UpdateResultPanelLayout();
            var op = result.recognizedOperator.HasValue ? SpellLabels.Korean(result.recognizedOperator.Value) : "미확정";
            resultText.text =
                $"{title}: {op}\n" +
                $"대상 seal: 없음\n" +
                $"판정 {StatusLabel(result.status)}  점수 {Percent(result.score)}  모양 {Percent(result.shapeConfidence)}\n" +
                $"이유: {ShortLine(result.feedbackReason, ResultLineLength(52, 42))}\n" +
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
            var questHeight = questScrollPanel == null ? 246f : questScrollPanel.sizeDelta.y;
            resultPanel.anchoredPosition = new Vector2(-20, -(questHeight + 40f));
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
                if (TryBuildEarlyTutorialSymbolDistanceEffect(resolution.goal, center, family, out var blockedEffect))
                {
                    return blockedEffect;
                }

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
                if (TryBuildEarlyTutorialSymbolDistanceEffect(resolution.targetGoal, center, family, out var blockedEffect))
                {
                    return blockedEffect;
                }

                return new GoalEffect(BuildBaseOffTargetGoalNote(family, resolution.targetGoal, resolution.distance, resolution.radius), resolution.worldEffect);
            }

            return new GoalEffect($"{SpellLabels.Korean(family)} seal이 바닥에 잠깐 고정되었습니다.", "seal_only");
        }

        private GoalEffect ApplyCustomSpellToGoals(
            CompiledSeal seal,
            CustomSpellEffectDefinition customEffect,
            Vector2 center,
            SpellResult spell)
        {
            var resolution = floorGoals.ResolveBase(activeGoals, seal.baseFamily, center, true, customEffect.kind);
            if (resolution.kind == GoalResolutionKind.Completed)
            {
                if (TryBuildEarlyTutorialSymbolDistanceEffect(resolution.goal, center, seal.baseFamily, out var blockedEffect))
                {
                    return blockedEffect;
                }

                ActivateGoal(resolution.goal, customEffect.kind.ToString().ToLowerInvariant(), spell);
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
                if (TryBuildEarlyTutorialSymbolDistanceEffect(resolution.targetGoal, center, seal.baseFamily, out var blockedEffect))
                {
                    return blockedEffect;
                }

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
            var origin = center;
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
                        AddBuffToEntity(player, BuffOwnerKind.Player, "보호막", color, CustomShapeEventKind.Barrier, 5.2f);
                        CustomShapeEventObjectCountForTests++;
                    }
                    break;
                case CustomShapeEventKind.Trap:
                    RegisterCustomEventObject(CreateWorldSprite("Custom Shape Trap Event", origin, Vector3.one * 0.76f, color, Color.white, PixelSpriteKind.Target, 23));
                    break;
                case CustomShapeEventKind.Stun:
                case CustomShapeEventKind.PiercingMark:
                    RegisterCustomEventObject(CreateWorldSprite($"Custom Shape {eventKind} Event", origin, Vector3.one * 0.62f, color, Color.white, PixelSpriteKind.Pulse, 24));
                    break;
                case CustomShapeEventKind.BuffDispel:
                case CustomShapeEventKind.RandomBuffDispel:
                    ClearBuffsForOwner(player);
                    AddBuffToEntity(player, BuffOwnerKind.Player, "해제", color, eventKind, 2.2f);
                    RegisterCustomEventObject(CreateWorldSprite($"Custom Shape {eventKind} Event", origin, Vector3.one * 0.62f, color, Color.white, PixelSpriteKind.Pulse, 24));
                    break;
                case CustomShapeEventKind.AttackBuff:
                case CustomShapeEventKind.MoveSpeedBuff:
                case CustomShapeEventKind.SpecialAttackBoost:
                case CustomShapeEventKind.MagicAmplify:
                case CustomShapeEventKind.GuardBuff:
                    AddBuffToEntity(player, BuffOwnerKind.Player, BuffLabelFor(eventKind), color, eventKind, BuffDurationFor(eventKind));
                    RegisterCustomEventObject(CreateWorldSprite($"Custom Shape {eventKind} Event", origin, Vector3.one * 0.62f, color, Color.white, PixelSpriteKind.Pulse, 24));
                    break;
                case CustomShapeEventKind.EventBlock:
                    RegisterCustomEventObject(CreateWorldSprite($"Custom Shape {eventKind} Event", origin, Vector3.one * 0.62f, color, Color.white, PixelSpriteKind.Pulse, 24));
                    break;
            }

            CreateCustomShapeEventAccent(spell, eventKind, origin, direction, color);

            return string.IsNullOrWhiteSpace(spell.customEventLabel)
                ? ""
                : $"커스텀 이벤트: {spell.customEventLabel}";
        }

        private void CreateCustomShapeEventAccent(
            SpellResult spell,
            CustomShapeEventKind eventKind,
            Vector2 origin,
            Vector2 direction,
            Color color)
        {
            if (eventKind == CustomShapeEventKind.None)
            {
                return;
            }

            var accent = Color.Lerp(color, Color.white, 0.48f);
            RegisterCustomEventObject(CreateWorldSprite(
                $"Custom Shape {eventKind} Event Ring",
                origin,
                Vector3.one * 0.72f,
                WithAlpha(color, 0.62f),
                WithAlpha(accent, 0.92f),
                PixelSpriteKind.RuneCircle,
                25));

            if (UsesDirectionalSignature(eventKind))
            {
                direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
                var trail = CreateWorldSprite(
                    $"Custom Shape {eventKind} Event Trail",
                    origin + direction * 0.46f,
                    new Vector3(0.13f, eventKind == CustomShapeEventKind.AttributeLaser ? 1.45f : 1.04f, 1f),
                    WithAlpha(color, 0.76f),
                    WithAlpha(accent, 0.95f),
                    PixelSpriteKind.Rug,
                    23);
                trail.transform.rotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.up, direction));
                RegisterCustomEventObject(trail);
                RegisterCustomEventObject(CreateWorldSprite(
                    $"Custom Shape {eventKind} Event Impact",
                    origin + direction * 0.92f,
                    Vector3.one * 0.42f,
                    accent,
                    Color.white,
                    PixelSpriteKind.Pulse,
                    26));
                return;
            }

            var label = string.IsNullOrWhiteSpace(spell?.customEventLabel) ? eventKind.ToString() : spell.customEventLabel;
            RegisterCustomEventObject(CreateWorldSprite(
                $"Custom Shape {label} Event Signature",
                origin + new Vector2(0f, 0.38f),
                Vector3.one * 0.48f,
                color,
                accent,
                EventSignatureSpriteKind(eventKind, CustomSpellEffectKind.None),
                26));
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

        private string ApplyElementalInteractions(
            SpellFamily family,
            SpellResult spell,
            Vector2 center,
            Vector2 direction,
            string sourceLabel)
        {
            var eventKind = TryParseCustomEventKind(spell, out var parsedEvent)
                ? parsedEvent
                : CustomShapeEventKind.None;
            var customEffect = CustomSpellEffectKind.None;
            if (spell?.isCustomShape == true)
            {
                var resolved = ResolveCustomSpellEffect(family, spell);
                customEffect = resolved.IsValid ? resolved.kind : CustomSpellEffectKind.None;
            }

            var radius = ElementalInteractionSystem.SpellRadiusFor(family, customEffect, eventKind);
            var context = new ElementalInteractionContext(
                family,
                customEffect,
                eventKind,
                center,
                direction,
                radius,
                sourceLabel);
            var reports = ElementalInteractionSystem.Apply(elementalEntities, context);
            LastElementalReactionCountForTests = reports.Count;
            LastElementalReactionSummaryForTests = ElementalInteractionSystem.BuildSummary(reports);
            foreach (var report in reports.Take(8))
            {
                pulses.Add(new ParticlePulse(report.position, ElementalReactionColor(report.reactionKind), weak: true, scaleMultiplier: 0.72f, durationSeconds: 0.55f, sortingOrder: 35));
            }
            RecordElementalDiscoveries(reports);

            return string.IsNullOrWhiteSpace(LastElementalReactionSummaryForTests)
                ? ""
                : $"속성 반응: {LastElementalReactionSummaryForTests}";
        }

        /// <summary>
        /// First time each elemental reaction kind fires in a run it becomes a
        /// hidden discovery: one codex observation line plus an ending-report
        /// discovery entry. Reactions are never required to pass a floor.
        /// </summary>
        private void RecordElementalDiscoveries(IReadOnlyList<ElementalReactionReport> reports)
        {
            foreach (var report in reports)
            {
                if (report.reactionKind == ElementalReactionKind.None || !discoveredReactions.Add(report.reactionKind))
                {
                    continue;
                }

                var observation = ElementalObservationLine(report.reactionKind);
                endingReport.RecordDiscovery($"elemental_{report.reactionKind}", observation);
                magicNote.Show(observation, MagicNoteCategory.Discovery, CurrentFloorNumber);
                audioDirector?.PlaySfx(AudioCue.NoteUnlock, 0.4f);
            }
        }

        private static string ElementalObservationLine(ElementalReactionKind kind)
        {
            return kind switch
            {
                ElementalReactionKind.Wet => "물기가 스며들어 표면이 어두워졌다.",
                ElementalReactionKind.Ignite => "마른 것이 불씨를 받아 타오르기 시작했다.",
                ElementalReactionKind.Extinguish => "물에 닿은 불이 꺼졌다.",
                ElementalReactionKind.Freeze => "젖은 것이 얼어붙어 단단해졌다.",
                ElementalReactionKind.Melt => "얼음이 열기에 녹아 물이 되었다.",
                ElementalReactionKind.Steam => "불과 물이 만나 증기가 피어올랐다.",
                ElementalReactionKind.Push => "바람이 가벼운 것을 밀어냈다.",
                ElementalReactionKind.Conduct => "전기가 젖은 길을 따라 흘렀다.",
                ElementalReactionKind.Grow => "생명의 기운이 마른 것을 깨웠다.",
                ElementalReactionKind.Stabilize => "흔들리던 것이 단단히 고정되었다.",
                _ => "탑이 낯선 반응을 기록했다."
            };
        }

        private bool TryBuildEarlyTutorialSymbolDistanceEffect(WorldStateGoal goal, Vector2 center, SpellFamily family, out GoalEffect effect)
        {
            effect = default;
            if (!RequiresEarlyTutorialSymbolProximity(goal))
            {
                return false;
            }

            var origin = player == null ? center : (Vector2)player.position;
            var radius = Mathf.Min(goal.radius, EarlyTutorialSymbolActivationRadius);
            var distance = Vector2.Distance(origin, goal.position);
            if (distance <= radius)
            {
                return false;
            }

            var note = BuildEarlyTutorialSymbolDistanceNote(family, goal, distance, radius);
            CurrentAssistLevel = 1;
            LastHintText = note;
            endingReport.RecordHintShown(1);
            ShowGoalProximityBubble(goal, distance, radius);
            effect = new GoalEffect(note, "symbol_distance_blocked");
            return true;
        }

        private bool RequiresEarlyTutorialSymbolProximity(WorldStateGoal goal)
        {
            return goal != null &&
                   floorController != null &&
                   floorController.Current.number <= CustomReferenceFloorNumber &&
                   goal.requiredBase.HasValue &&
                   !goal.requiredCustomSpell.HasValue;
        }

        private string BuildEarlyTutorialSymbolDistanceNote(SpellFamily family, WorldStateGoal target, float distance, float radius)
        {
            return
                $"{SpellLabels.Korean(family)} 마법은 보였지만 {target.title} 표식에서 너무 멉니다.\n" +
                $"{target.title} 바로 옆으로 이동한 뒤 다시 그리세요. 현재 거리 {distance:0.0}, 목표 반경 {radius:0.0}.";
        }

        private static Color ElementalReactionColor(ElementalReactionKind reactionKind)
        {
            return reactionKind switch
            {
                ElementalReactionKind.Ignite => new Color(1f, 0.34f, 0.08f),
                ElementalReactionKind.Freeze => new Color(0.56f, 0.90f, 1f),
                ElementalReactionKind.Wet => new Color(0.24f, 0.52f, 1f),
                ElementalReactionKind.Extinguish => new Color(0.38f, 0.70f, 1f),
                ElementalReactionKind.Melt => new Color(0.76f, 0.92f, 1f),
                ElementalReactionKind.Steam => new Color(0.78f, 0.82f, 0.86f),
                ElementalReactionKind.Push => new Color(0.74f, 0.86f, 0.92f),
                ElementalReactionKind.Conduct => new Color(1f, 0.88f, 0.18f),
                ElementalReactionKind.Grow => new Color(0.34f, 0.92f, 0.42f),
                ElementalReactionKind.Stabilize => new Color(0.82f, 0.66f, 0.38f),
                _ => Color.white
            };
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

        private void ActivateGoal(WorldStateGoal goal, string effect, SpellResult spell = null)
        {
            goal.completed = true;
            if (ReferenceEquals(goalProximityBubbleGoal, goal))
            {
                HideGoalProximityBubble();
            }

            if (goal.renderer != null)
            {
                goal.renderer.sprite = PixelArtFactory.CreateSprite($"{goal.title} Active", Color.white, goal.color, goal.kind);
                goal.renderer.sharedMaterial = PixelMaterialProvider.SpriteMaterial;
            }
            if (goal.body != null)
            {
                goal.body.transform.localScale *= 1.15f;
                RegisterSpriteAccent(goal.body, SpriteAccentAnimationKind.RuneActive, 0.21f);
            }
            if (goal.label != null)
            {
                goal.label.text = $"완료: {goal.title}";
                goal.label.color = Color.Lerp(goal.color, Color.white, 0.6f);
                goal.label.fontStyle = FontStyle.Bold;
            }
            ApplyGoalReaction(goal, spell);
            endingReport.RecordDiscovery(goal.id, effect);
            audioDirector?.PlaySfx(AudioCue.GoalSatisfied, 0.82f);
            pulses.Add(new ParticlePulse(goal.position, goal.color));
            GlowPulse.Flash(goal.position, goal.color, 1.7f, 26);
            TickQuestChecklist(forceRefresh: true);
            UpdateHud();
            UpdateResultPanelLayout();
        }

        private void ApplyGoalReaction(WorldStateGoal goal, SpellResult spell = null)
        {
            if (TryCreateStageEnvironmentReaction(goal, spell))
            {
                return;
            }

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
                    CreateEarthGapFillReaction(goal);
                    break;
                case WorldReactionKind.WindPlatform:
                    CreateWindPlatformReaction(goal);
                    break;
                case WorldReactionKind.CombatHit:
                    CreateCombatHitReaction(goal);
                    break;
            }
        }

        private bool TryCreateStageEnvironmentReaction(WorldStateGoal goal, SpellResult spell)
        {
            if (activeStageDefinition == null || !goal.requiredCustomSpell.HasValue)
            {
                return false;
            }

            var obstacle = activeStageDefinition.FindObstacle(goal.id) ??
                           activeStageDefinition.FindObstacleForEffect(goal.requiredCustomSpell.Value);
            var effect = activeStageDefinition.FindEffect(goal.requiredCustomSpell.Value, goal.requiredBase);
            if (obstacle == null || effect == null)
            {
                return false;
            }

            var bodies = CreateStageEntityForObstacle(goal, obstacle, effect);
            CreateStageEffectVisuals(goal, obstacle, effect, spell, bodies);
            if (obstacle.safePositionAfterSolved.sqrMagnitude > 0.001f)
            {
                safePosition = obstacle.safePositionAfterSolved;
            }

            pulses.Add(new ParticlePulse(obstacle.solutionPosition, goal.color, scaleMultiplier: 1.35f, durationSeconds: 1.1f, sortingOrder: 8));
            return true;
        }

        private IReadOnlyList<GameObject> CreateStageEntityForObstacle(
            WorldStateGoal goal,
            StageObstacleDefinition obstacle,
            StageEnvironmentEffect effect)
        {
            var bodies = new List<GameObject>();
            var entity = effect.entity ?? new StageEntityDefinition();
            if (entity.createsSteps)
            {
                var stepCount = Mathf.Max(1, entity.stepCount);
                var stepSize = entity.stepSize.sqrMagnitude <= 0.001f ? obstacle.solutionSize : entity.stepSize;
                for (var index = 0; index < stepCount; index++)
                {
                    var position = obstacle.solutionPosition + entity.stepStartOffset + entity.stepSpacing * index;
                    var body = CreateStageEntityBody(
                        string.IsNullOrWhiteSpace(entity.entityName) ? $"{effect.displayName} Step {index + 1}" : $"{entity.entityName} {index + 1}",
                        position,
                        stepSize,
                        entity,
                        goal.color);
                    bodies.Add(body);
                    AddPlatformCollider(body, stepSize);
                }

                CreateStageNode(goal, obstacle.goalPosition + new Vector2(0.38f, 0.16f), goal.kind);
                return bodies;
            }

            var size = obstacle.solutionSize.sqrMagnitude <= 0.001f ? entity.size : obstacle.solutionSize;
            var single = CreateStageEntityBody(
                string.IsNullOrWhiteSpace(entity.entityName) ? effect.displayName : entity.entityName,
                obstacle.solutionPosition + entity.offset,
                size,
                entity,
                goal.color);
            bodies.Add(single);
            if (entity.hasCollider)
            {
                AddPlatformCollider(single, size);
            }

            CreateStageNode(goal, obstacle.goalPosition + new Vector2(0.38f, 0.16f), goal.kind);
            return bodies;
        }

        private GameObject CreateStageEntityBody(
            string title,
            Vector2 position,
            Vector2 size,
            StageEntityDefinition entity,
            Color fallbackColor)
        {
            var primary = entity.primaryColor == default ? fallbackColor : entity.primaryColor;
            var body = CreateWorldSprite(
                title,
                position,
                Vector3.one,
                primary,
                entity.secondaryColor,
                entity.spriteKind,
                entity.sortingOrder,
                entity.tiled,
                size);
            ApplySpriteOverride(body, entity.spriteOverride);
            stageEntityObjects.Add(body);
            floorObjects.Add(body);
            return body;
        }

        private void CreateStageEffectVisuals(
            WorldStateGoal goal,
            StageObstacleDefinition obstacle,
            StageEnvironmentEffect effect,
            SpellResult spell,
            IReadOnlyList<GameObject> bodies)
        {
            var visual = effect.visual ?? new StageEffectVisualDefinition();
            if (!visual.enabled)
            {
                return;
            }

            var entity = effect.entity ?? new StageEntityDefinition();
            ResolveStageEffectSpan(obstacle, entity, out var center, out var size);
            var primary = visual.primaryColor == default ? goal.color : visual.primaryColor;
            var secondary = visual.secondaryColor == default ? Color.Lerp(primary, Color.white, 0.46f) : visual.secondaryColor;
            var sortingOrder = visual.sortingOrder == 0 ? Mathf.Max(7, entity.sortingOrder + 3) : visual.sortingOrder;
            var glowPadding = visual.glowPadding == default ? new Vector2(0.55f, 0.30f) : visual.glowPadding;

            if (visual.showGroundGlow)
            {
                RegisterStageEffectObject(CreateWorldSprite(
                    $"Stage Effect {goal.id} Ground Glow",
                    center,
                    Vector3.one,
                    WithAlpha(primary, 0.46f),
                    WithAlpha(secondary, 0.82f),
                    PixelSpriteKind.Rug,
                    sortingOrder - 3,
                    true,
                    size + glowPadding));

                CreateStageEffectRim(
                    $"Stage Effect {goal.id} Interaction Rim",
                    center,
                    size + glowPadding * 0.72f,
                    WithAlpha(secondary, 0.90f),
                    sortingOrder - 1);
            }

            if (visual.showEntityWake)
            {
                RegisterStageEffectObject(CreateWorldSprite(
                    $"Stage Effect {goal.id} Surface Wake",
                    center + EffectWakeOffset(effect.customEffect),
                    Vector3.one,
                    WithAlpha(Color.Lerp(primary, Color.white, 0.18f), 0.72f),
                    WithAlpha(secondary, 0.96f),
                    EffectSurfaceSpriteKind(effect.customEffect, entity.spriteKind),
                    sortingOrder,
                    true,
                    new Vector2(Mathf.Max(0.38f, size.x * 0.94f), Mathf.Max(0.18f, size.y * 0.68f))));
            }

            if (visual.showAnchorGlyphs)
            {
                var glyphScale = visual.glyphScale <= 0.001f ? 0.46f : visual.glyphScale;
                var anchorSprite = FamilyRuneKind(goal.requiredBase ?? effect.baseFamily);
                var anchorYOffset = Mathf.Clamp(size.y * 0.50f + 0.16f, 0.18f, 0.72f);
                var left = center + new Vector2(-size.x * 0.48f, anchorYOffset);
                var right = center + new Vector2(size.x * 0.48f, anchorYOffset);
                RegisterStageEffectObject(CreateWorldSprite($"Stage Effect {goal.id} Left Anchor", left, Vector3.one * glyphScale, primary, secondary, anchorSprite, sortingOrder + 1));
                RegisterStageEffectObject(CreateWorldSprite($"Stage Effect {goal.id} Right Anchor", right, Vector3.one * glyphScale, primary, secondary, anchorSprite, sortingOrder + 1));
            }

            if (visual.showEventSignature)
            {
                CreateStageEventSignature(goal, obstacle, effect, spell, primary, secondary, sortingOrder + 2);
            }

            if (bodies != null && bodies.Count > 1)
            {
                for (var index = 0; index < bodies.Count; index++)
                {
                    var body = bodies[index];
                    if (body == null)
                    {
                        continue;
                    }

                    RegisterStageEffectObject(CreateWorldSprite(
                        $"Stage Effect {goal.id} Step Wake {index + 1}",
                        body.transform.position + new Vector3(0f, -0.08f, 0f),
                        Vector3.one,
                        WithAlpha(primary, 0.50f),
                        WithAlpha(secondary, 0.82f),
                        PixelSpriteKind.WallTrim,
                        sortingOrder + 1,
                        true,
                        new Vector2(Mathf.Max(0.46f, entity.stepSize.x * 0.92f), 0.08f)));
                }
            }
        }

        private void CreateStageEventSignature(
            WorldStateGoal goal,
            StageObstacleDefinition obstacle,
            StageEnvironmentEffect effect,
            SpellResult spell,
            Color primary,
            Color secondary,
            int sortingOrder)
        {
            var eventKind = TryParseCustomEventKind(spell, out var parsed) ? parsed : CustomShapeEventKind.None;
            var displayKind = eventKind == CustomShapeEventKind.None ? effect.customEffect.ToString() : eventKind.ToString();
            var direction = spell != null && spell.customEventDirection.sqrMagnitude > 0.0001f
                ? spell.customEventDirection.normalized
                : Vector2.right;
            var origin = obstacle.goalPosition + new Vector2(0f, 0.38f);
            var signature = CreateWorldSprite(
                $"Stage Effect {goal.id} {displayKind} Signature",
                origin,
                Vector3.one * 0.54f,
                primary,
                secondary,
                EventSignatureSpriteKind(eventKind, effect.customEffect),
                sortingOrder);
            if (UsesDirectionalSignature(eventKind))
            {
                signature.transform.rotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.up, direction));
                RegisterStageEffectObject(CreateDirectionalStageTrail(goal.id, origin, direction, primary, secondary, sortingOrder - 1));
                RegisterStageEffectObject(CreateWorldSprite(
                    $"Stage Effect {goal.id} Event Impact",
                    origin + direction * 0.56f,
                    Vector3.one * 0.34f,
                    WithAlpha(secondary, 0.92f),
                    Color.white,
                    PixelSpriteKind.Pulse,
                    sortingOrder + 1));
            }

            RegisterStageEffectObject(signature);
        }

        private GameObject CreateDirectionalStageTrail(string goalId, Vector2 origin, Vector2 direction, Color primary, Color secondary, int sortingOrder)
        {
            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            var trail = CreateWorldSprite(
                $"Stage Effect {goalId} Direction Trail",
                origin + direction * 0.34f,
                new Vector3(0.12f, 0.86f, 1f),
                WithAlpha(primary, 0.70f),
                WithAlpha(secondary, 0.88f),
                PixelSpriteKind.Rug,
                sortingOrder);
            trail.transform.rotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.up, direction));
            return trail;
        }

        private void CreateStageEffectRim(string name, Vector2 center, Vector2 size, Color color, int sortingOrder)
        {
            var glowColor = Color.Lerp(color, Color.white, 0.38f);
            RegisterStageEffectObject(CreateWorldSprite($"{name} North", center + new Vector2(0f, size.y * 0.5f), Vector3.one, color, glowColor, PixelSpriteKind.WallTrim, sortingOrder, true, new Vector2(size.x, 0.08f)));
            RegisterStageEffectObject(CreateWorldSprite($"{name} South", center + new Vector2(0f, -size.y * 0.5f), Vector3.one, color, glowColor, PixelSpriteKind.WallTrim, sortingOrder, true, new Vector2(size.x, 0.08f)));
            RegisterStageEffectObject(CreateWorldSprite($"{name} West", center + new Vector2(-size.x * 0.5f, 0f), Vector3.one, color, glowColor, PixelSpriteKind.WallTrim, sortingOrder, true, new Vector2(0.08f, size.y)));
            RegisterStageEffectObject(CreateWorldSprite($"{name} East", center + new Vector2(size.x * 0.5f, 0f), Vector3.one, color, glowColor, PixelSpriteKind.WallTrim, sortingOrder, true, new Vector2(0.08f, size.y)));
        }

        private void RegisterStageEffectObject(GameObject body)
        {
            if (body == null)
            {
                return;
            }

            stageEffectObjects.Add(body);
            floorObjects.Add(body);
            RegisterSpriteAccent(body, SpriteAccentAnimationKind.StageEffectGlow, stageEffectObjects.Count * 0.37f);
        }

        private static void ResolveStageEffectSpan(StageObstacleDefinition obstacle, StageEntityDefinition entity, out Vector2 center, out Vector2 size)
        {
            if (entity != null && entity.createsSteps)
            {
                var stepCount = Mathf.Max(1, entity.stepCount);
                var stepSize = entity.stepSize.sqrMagnitude <= 0.001f ? obstacle.solutionSize : entity.stepSize;
                var min = obstacle.solutionPosition + entity.stepStartOffset - stepSize * 0.5f;
                var max = obstacle.solutionPosition + entity.stepStartOffset + stepSize * 0.5f;
                for (var index = 1; index < stepCount; index++)
                {
                    var stepCenter = obstacle.solutionPosition + entity.stepStartOffset + entity.stepSpacing * index;
                    min = Vector2.Min(min, stepCenter - stepSize * 0.5f);
                    max = Vector2.Max(max, stepCenter + stepSize * 0.5f);
                }

                center = (min + max) * 0.5f;
                size = new Vector2(Mathf.Max(0.42f, max.x - min.x), Mathf.Max(0.20f, max.y - min.y));
                return;
            }

            var safeEntity = entity ?? new StageEntityDefinition();
            center = obstacle.solutionPosition + safeEntity.offset;
            size = obstacle.solutionSize.sqrMagnitude <= 0.001f ? safeEntity.size : obstacle.solutionSize;
            size = new Vector2(Mathf.Max(0.42f, size.x), Mathf.Max(0.20f, size.y));
        }

        private static Vector2 EffectWakeOffset(CustomSpellEffectKind kind)
        {
            return kind switch
            {
                CustomSpellEffectKind.Ice => new Vector2(0f, 0.05f),
                CustomSpellEffectKind.Stability => new Vector2(0f, -0.02f),
                CustomSpellEffectKind.LivingBridge => new Vector2(0f, 0.08f),
                CustomSpellEffectKind.WindPlatform => new Vector2(0f, 0.16f),
                _ => Vector2.zero
            };
        }

        private static PixelSpriteKind EffectSurfaceSpriteKind(CustomSpellEffectKind kind, PixelSpriteKind fallback)
        {
            return kind switch
            {
                CustomSpellEffectKind.Ice => PixelSpriteKind.IceBridge,
                CustomSpellEffectKind.Stability => PixelSpriteKind.EarthStep,
                CustomSpellEffectKind.LivingBridge => PixelSpriteKind.VineBridge,
                CustomSpellEffectKind.WindPlatform => PixelSpriteKind.WindPlatformTile,
                _ => fallback
            };
        }

        private static PixelSpriteKind EventSignatureSpriteKind(CustomShapeEventKind eventKind, CustomSpellEffectKind effectKind)
        {
            return eventKind switch
            {
                CustomShapeEventKind.WallEntity => PixelSpriteKind.WallTrim,
                CustomShapeEventKind.Barrier or CustomShapeEventKind.GuardBuff => PixelSpriteKind.RuneCircle,
                CustomShapeEventKind.Trap or CustomShapeEventKind.Stun or CustomShapeEventKind.PiercingMark => PixelSpriteKind.Target,
                CustomShapeEventKind.DirectionalProjectile or CustomShapeEventKind.AttributeLaser or CustomShapeEventKind.CurveProjectile or CustomShapeEventKind.SlashDamage => PixelSpriteKind.Rug,
                CustomShapeEventKind.EventBlock or CustomShapeEventKind.BuffDispel or CustomShapeEventKind.RandomBuffDispel => PixelSpriteKind.Portal,
                _ => effectKind switch
                {
                    CustomSpellEffectKind.Ice => PixelSpriteKind.WaterRune,
                    CustomSpellEffectKind.Stability => PixelSpriteKind.EarthRune,
                    CustomSpellEffectKind.LivingBridge => PixelSpriteKind.LifeRune,
                    CustomSpellEffectKind.WindPlatform => PixelSpriteKind.WindRune,
                    _ => PixelSpriteKind.Pulse
                }
            };
        }

        private static bool UsesDirectionalSignature(CustomShapeEventKind eventKind)
        {
            return eventKind is CustomShapeEventKind.DirectionalProjectile or
                CustomShapeEventKind.AttributeLaser or
                CustomShapeEventKind.CurveProjectile or
                CustomShapeEventKind.SlashDamage;
        }

        private static bool TryParseCustomEventKind(SpellResult spell, out CustomShapeEventKind eventKind)
        {
            return Enum.TryParse(spell?.customEventKind, out eventKind);
        }

        private static PixelSpriteKind FamilyRuneKind(SpellFamily family)
        {
            return family switch
            {
                SpellFamily.Fire => PixelSpriteKind.FireRune,
                SpellFamily.Water => PixelSpriteKind.WaterRune,
                SpellFamily.Wind => PixelSpriteKind.WindRune,
                SpellFamily.Earth => PixelSpriteKind.EarthRune,
                SpellFamily.Life => PixelSpriteKind.LifeRune,
                _ => PixelSpriteKind.RuneCircle
            };
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
            CreateStagePath(goal, "덩굴 다리", new Vector2(0f, -2.45f), new Vector2(5.8f, 0.46f), new Color(0.16f, 0.52f, 0.28f), PixelSpriteKind.VineBridge);
            CreateStageNode(goal, goal.position + new Vector2(0.62f, 0.12f), PixelSpriteKind.LifeRune);
        }

        private void CreateFrozenRiverReaction(WorldStateGoal goal)
        {
            CreateStagePath(goal, "얼음길", new Vector2(0f, -0.62f), new Vector2(5.9f, 0.50f), new Color(0.48f, 0.84f, 1f), PixelSpriteKind.IceBridge);
            CreateStageNode(goal, goal.position + new Vector2(0.58f, 0.12f), PixelSpriteKind.WaterRune);
        }

        private void CreateEarthGapFillReaction(WorldStateGoal goal)
        {
            CreateStagePath(goal, "구멍 메움판", new Vector2(2.3f, -2.75f), new Vector2(1.35f, 0.42f), new Color(0.58f, 0.42f, 0.24f), PixelSpriteKind.EarthStep);
            CreateStageNode(goal, goal.position + new Vector2(0.48f, 0.1f), PixelSpriteKind.EarthRune);
        }

        private void CreateWindPlatformReaction(WorldStateGoal goal)
        {
            CreateStagePath(goal, "바람 발판", new Vector2(0f, 2.78f), new Vector2(3.8f, 0.38f), new Color(0.54f, 0.80f, 0.92f), PixelSpriteKind.WindPlatformTile);
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
            var target = CombatTargetTransform(goal);
            var label = impact > 0 ? $"-{impact}" : "효과";
            var damageColor = DamageColorFor(goal.requiredCustomSpell);
            ShowDamagePopup(CombatTargetTopPosition(goal, target), label, damageColor);
            AddCombatStatusBuff(goal, target);
            pulses.Add(new ParticlePulse(goal.position, goal.color, scaleMultiplier: 0.9f, durationSeconds: 0.55f, sortingOrder: 20));
        }

        private Transform CombatTargetTransform(WorldStateGoal goal)
        {
            if (goal?.entityBody != null)
            {
                return goal.entityBody.transform;
            }

            return goal?.body != null ? goal.body.transform : null;
        }

        private static Vector2 CombatTargetTopPosition(WorldStateGoal goal, Transform target)
        {
            if (target != null)
            {
                return (Vector2)target.position + new Vector2(0f, 0.98f);
            }

            return goal.position + new Vector2(0f, 0.74f);
        }

        private void AddCombatStatusBuff(WorldStateGoal goal, Transform target)
        {
            if (goal == null || target == null || !goal.requiredCustomSpell.HasValue)
            {
                return;
            }

            var effect = goal.requiredCustomSpell.Value;
            AddBuffToEntity(
                target,
                BuffOwnerKind.Target,
                CombatStatusLabel(effect),
                CombatStatusColor(effect, goal.color),
                CombatStatusEventKind(effect),
                CombatStatusDuration(effect));
        }

        private void EvaluateFloorCompletion()
        {
            if (HasEndingReport || practiceMode)
            {
                return;
            }

            if (!IsFinalFloor)
            {
                if (!activeGoals.All(goal => goal.completed) || pendingAdvanceAt > 0f)
                {
                    return;
                }

                ShowMagicNote(BuildFloorCompletionNote(), MentorMood.Happy);
                audioDirector?.PlaySfx(AudioCue.FloorComplete, 0.92f);
                ShowToast($"{floorController.CurrentFloorNumber}층 완료 - 다음 층 개방", floorController.Current.accentColor, strong: true);
                pendingAdvanceAt = Time.time + CurrentFloorAdvanceDelaySeconds();
                PublishProgressCheckpoint(Mathf.Min(floorController.CurrentFloorNumber + 1, FloorCount));
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
                ShowMagicNote(BuildFloorCompletionNote(), MentorMood.Happy);
                audioDirector?.PlaySfx(AudioCue.CastFinalEffect, 0.95f);
                ShowToast("성좌심 완전 복구 - 보고서 준비", floorController.Current.accentColor, strong: true);
                pendingAdvanceAt = Time.time + FinalFloorCompleteReportDelaySeconds;
                PublishProgressCheckpoint(floorController.CurrentFloorNumber);
                return;
            }

            if (pendingAdvanceAt > 0f)
            {
                return;
            }

            ShowMagicNote(BuildFloorCompletionNote(), MentorMood.Happy);
            audioDirector?.PlaySfx(AudioCue.CastFinalEffect, 0.80f);
            ShowToast("입학 시험 통과 - 보고서 준비", floorController.Current.accentColor, strong: true);
            pendingAdvanceAt = Time.time + FinalFloorPassReportDelaySeconds;
            PublishProgressCheckpoint(floorController.CurrentFloorNumber);
        }

        private float CurrentFloorAdvanceDelaySeconds()
        {
            return floorController?.Current.number == 3
                ? StageFloorAdvanceDelaySeconds
                : StandardFloorAdvanceDelaySeconds;
        }

        private string BuildFloorCompletionNote()
        {
            if (!IsFinalFloor)
            {
                return $"{floorController.Current.completeNote}\n{FloorCompletionLore(floorController.Current.number)}";
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
            if (HasEndingReport || !gameplayInputEnabled)
            {
                return;
            }

            if (platformMotionActive)
            {
                TickPlatformPlayer();
                return;
            }

            var input = ReadMovementInput();

            velocity = Vector2.Lerp(velocity, input * 4.2f, Time.deltaTime * 12f);
            player.position += (Vector3)(velocity * Time.deltaTime);
            player.position = new Vector3(Mathf.Clamp(player.position.x, -7.35f, 7.35f), Mathf.Clamp(player.position.y, -4.25f, 4.25f), 0f);
            playerAnimator?.SetMotion(input, velocity);
        }

        private void TickPlatformPlayer()
        {
            EnsurePlayerPhysics();
            if (playerBody == null || activeStageDefinition == null)
            {
                return;
            }

            var inputX = ReadHorizontalMovementInput();
            platformHorizontalVelocity = Mathf.MoveTowards(
                platformHorizontalVelocity,
                inputX * PlatformMoveSpeed,
                PlatformMoveAcceleration * Time.deltaTime);

            var bodyVelocity = playerBody.linearVelocity;
            bodyVelocity.x = 0f;
            if (ReadJumpPressed() && IsPlatformGrounded())
            {
                bodyVelocity.y = PlatformJumpVelocity;
            }

            playerBody.linearVelocity = bodyVelocity;
            if (Mathf.Abs(platformHorizontalVelocity) > 0.001f)
            {
                var position = playerBody.position;
                position.x += platformHorizontalVelocity * Time.deltaTime;
                playerBody.position = position;
                if (player != null)
                {
                    player.position = position;
                }
            }

            ClampPlatformPlayer();
            TickPlatformCamera();
            playerAnimator?.SetMotion(new Vector2(inputX, 0f), new Vector2(platformHorizontalVelocity, playerBody.linearVelocity.y));
        }

        private Vector2 ReadMovementInput()
        {
            return BuildMovementInput(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical"),
                Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) || IsFallbackActive(fallbackLeftHeld, fallbackLeftPulseUntil),
                Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) || IsFallbackActive(fallbackRightHeld, fallbackRightPulseUntil),
                Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) || IsFallbackActive(fallbackDownHeld, fallbackDownPulseUntil),
                Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) || IsFallbackActive(fallbackUpHeld, fallbackUpPulseUntil));
        }

        private float ReadHorizontalMovementInput()
        {
            return ResolveAxisWithKeys(
                Input.GetAxisRaw("Horizontal"),
                Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) || IsFallbackActive(fallbackLeftHeld, fallbackLeftPulseUntil),
                Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) || IsFallbackActive(fallbackRightHeld, fallbackRightPulseUntil));
        }

        private bool ReadJumpPressed()
        {
            var fallbackJumpPressed = IsFallbackActive(false, fallbackJumpPulseUntil);
            if (fallbackJumpPressed)
            {
                fallbackJumpPulseUntil = -1f;
            }

            return Input.GetButtonDown("Jump") ||
                   Input.GetKeyDown(KeyCode.Space) ||
                   Input.GetKeyDown(KeyCode.W) ||
                   Input.GetKeyDown(KeyCode.UpArrow) ||
                   fallbackJumpPressed;
        }

        private void CaptureKeyboardFallback(KeyCode keyCode, bool pressed)
        {
            switch (keyCode)
            {
                case KeyCode.LeftArrow:
                case KeyCode.A:
                    SetFallbackKey(ref fallbackLeftHeld, ref fallbackLeftPulseUntil, pressed);
                    break;
                case KeyCode.RightArrow:
                case KeyCode.D:
                    SetFallbackKey(ref fallbackRightHeld, ref fallbackRightPulseUntil, pressed);
                    break;
                case KeyCode.DownArrow:
                case KeyCode.S:
                    SetFallbackKey(ref fallbackDownHeld, ref fallbackDownPulseUntil, pressed);
                    break;
                case KeyCode.UpArrow:
                case KeyCode.W:
                    SetFallbackKey(ref fallbackUpHeld, ref fallbackUpPulseUntil, pressed);
                    if (pressed)
                    {
                        fallbackJumpPulseUntil = Time.unscaledTime + KeyboardMovementPulseSeconds;
                    }

                    break;
                case KeyCode.Space:
                    if (pressed)
                    {
                        fallbackJumpPulseUntil = Time.unscaledTime + KeyboardMovementPulseSeconds;
                    }

                    break;
            }
        }

        private void ClearMovementInputFallback()
        {
            fallbackLeftHeld = false;
            fallbackRightHeld = false;
            fallbackDownHeld = false;
            fallbackUpHeld = false;
            fallbackLeftPulseUntil = -1f;
            fallbackRightPulseUntil = -1f;
            fallbackDownPulseUntil = -1f;
            fallbackUpPulseUntil = -1f;
            fallbackJumpPulseUntil = -1f;
        }

        private static void SetFallbackKey(ref bool held, ref float pulseUntil, bool pressed)
        {
            held = pressed;
            if (pressed)
            {
                pulseUntil = Time.unscaledTime + KeyboardMovementPulseSeconds;
            }
        }

        private static bool IsFallbackActive(bool held, float pulseUntil)
        {
            return held || Time.unscaledTime <= pulseUntil;
        }

        private static Vector2 BuildMovementInput(
            float horizontalAxis,
            float verticalAxis,
            bool leftHeld,
            bool rightHeld,
            bool downHeld,
            bool upHeld)
        {
            var input = new Vector2(
                ResolveAxisWithKeys(horizontalAxis, leftHeld, rightHeld),
                ResolveAxisWithKeys(verticalAxis, downHeld, upHeld));
            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }

            return input;
        }

        private static float ResolveAxisWithKeys(float axis, bool negativeHeld, bool positiveHeld)
        {
            if (negativeHeld || positiveHeld)
            {
                return (positiveHeld ? 1f : 0f) - (negativeHeld ? 1f : 0f);
            }

            return Mathf.Clamp(axis, -1f, 1f);
        }

        private bool IsPlatformGrounded()
        {
            if (player == null)
            {
                return false;
            }

            var hits = Physics2D.OverlapBoxAll((Vector2)player.position + new Vector2(0f, -0.54f), new Vector2(0.46f, 0.12f), 0f);
            return hits.Any(hit => hit != null && hit.transform != player && !hit.isTrigger);
        }

        private void ClampPlatformPlayer()
        {
            if (activeStageDefinition == null || playerBody == null)
            {
                return;
            }

            var position = playerBody.position;
            if (position.y < activeStageDefinition.killY)
            {
                TakePlayerDamage("낭떠러지 추락", position);
                ResetPlayerToSafePosition("발판 아래로 떨어졌습니다. 직전 안정 지점에서 다시 시도하세요.");
                return;
            }

            position.x = Mathf.Clamp(position.x, activeStageDefinition.stageMin.x, activeStageDefinition.stageMax.x);
            playerBody.position = position;
        }

        private void TickPlatformCamera()
        {
            if (mainCamera == null || activeStageDefinition == null || player == null)
            {
                return;
            }

            var targetX = Mathf.Clamp(player.position.x, activeStageDefinition.cameraXRange.x, activeStageDefinition.cameraXRange.y);
            mainCamera.transform.position = new Vector3(targetX, activeStageDefinition.cameraY, -10f);
        }

        private void ResetPlayerToSafePosition(string note)
        {
            MovePlayerTo(safePosition);
            if (!string.IsNullOrWhiteSpace(note))
            {
                ShowMagicNote(note, MentorMood.Frown);
            }
        }

        private void MovePlayerTo(Vector2 worldPosition)
        {
            if (player != null)
            {
                player.position = worldPosition;
            }

            if (playerBody != null)
            {
                playerBody.position = worldPosition;
                playerBody.linearVelocity = Vector2.zero;
            }

            velocity = Vector2.zero;
            platformHorizontalVelocity = 0f;
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

                TakePlayerDamage("장애물 접촉", gate.center);
                MovePlayerTo(gate.resetPosition);
                ShowMagicNote(gate.lockedNote, MentorMood.Frown);
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

        private void TickBuffQueues()
        {
            for (var index = buffQueues.Count - 1; index >= 0; index--)
            {
                var queue = buffQueues[index];
                if (!queue.Tick(Time.time))
                {
                    queue.Destroy();
                    buffQueues.RemoveAt(index);
                }
            }
        }

        private void AddBuffToEntity(
            Transform owner,
            BuffOwnerKind ownerKind,
            string label,
            Color color,
            CustomShapeEventKind eventKind,
            float durationSeconds)
        {
            if (owner == null)
            {
                return;
            }

            var queue = buffQueues.FirstOrDefault(item => item.IsFor(owner));
            if (queue == null)
            {
                queue = new BuffQueueView(owner, ownerKind, uiFont);
                buffQueues.Add(queue);
            }

            queue.Add(label, color, EventSignatureSpriteKind(eventKind, CustomSpellEffectKind.None), Mathf.Max(durationSeconds, 0.5f), Time.time);
            LastBuffLabelForTests = label;
        }

        private void ClearBuffsForOwner(Transform owner)
        {
            if (owner == null)
            {
                return;
            }

            foreach (var queue in buffQueues.Where(item => item.IsFor(owner)))
            {
                queue.Clear();
            }
        }

        private static string BuffLabelFor(CustomShapeEventKind eventKind)
        {
            return eventKind switch
            {
                CustomShapeEventKind.AttackBuff => "공격",
                CustomShapeEventKind.MoveSpeedBuff => "이속",
                CustomShapeEventKind.SpecialAttackBoost => "특공",
                CustomShapeEventKind.MagicAmplify => "강화",
                CustomShapeEventKind.GuardBuff => "방어",
                CustomShapeEventKind.Barrier => "보호",
                _ => "버프"
            };
        }

        private static float BuffDurationFor(CustomShapeEventKind eventKind)
        {
            return eventKind switch
            {
                CustomShapeEventKind.AttackBuff => 7.0f,
                CustomShapeEventKind.SpecialAttackBoost => 6.2f,
                CustomShapeEventKind.MoveSpeedBuff => 5.8f,
                CustomShapeEventKind.MagicAmplify => 6.6f,
                CustomShapeEventKind.GuardBuff => 7.4f,
                CustomShapeEventKind.Barrier => 5.2f,
                _ => 4.8f
            };
        }

        private static string CombatStatusLabel(CustomSpellEffectKind effect)
        {
            return effect switch
            {
                CustomSpellEffectKind.Ice => "감속",
                CustomSpellEffectKind.Electric => "감전",
                CustomSpellEffectKind.Cleanse => "정화",
                CustomSpellEffectKind.Focus => "표식",
                CustomSpellEffectKind.Flow => "흐름",
                CustomSpellEffectKind.Connection => "속박",
                CustomSpellEffectKind.Stability => "방벽",
                _ => "상태"
            };
        }

        private static CustomShapeEventKind CombatStatusEventKind(CustomSpellEffectKind effect)
        {
            return effect switch
            {
                CustomSpellEffectKind.Ice => CustomShapeEventKind.Stun,
                CustomSpellEffectKind.Electric => CustomShapeEventKind.SlashDamage,
                CustomSpellEffectKind.Cleanse => CustomShapeEventKind.Barrier,
                CustomSpellEffectKind.Focus => CustomShapeEventKind.MagicAmplify,
                CustomSpellEffectKind.Flow => CustomShapeEventKind.MoveSpeedBuff,
                CustomSpellEffectKind.Connection => CustomShapeEventKind.AttackBuff,
                CustomSpellEffectKind.Stability => CustomShapeEventKind.GuardBuff,
                _ => CustomShapeEventKind.PiercingMark
            };
        }

        private static float CombatStatusDuration(CustomSpellEffectKind effect)
        {
            return effect switch
            {
                CustomSpellEffectKind.Ice => 5.8f,
                CustomSpellEffectKind.Electric => 4.4f,
                CustomSpellEffectKind.Cleanse => 3.6f,
                CustomSpellEffectKind.Focus => 6.0f,
                CustomSpellEffectKind.Stability => 6.5f,
                _ => 5.0f
            };
        }

        private static Color CombatStatusColor(CustomSpellEffectKind effect, Color fallback)
        {
            return effect switch
            {
                CustomSpellEffectKind.Ice => new Color(0.48f, 0.84f, 1f),
                CustomSpellEffectKind.Electric => new Color(1f, 0.88f, 0.18f),
                CustomSpellEffectKind.Cleanse => new Color(0.42f, 0.74f, 1f),
                CustomSpellEffectKind.Focus => new Color(1f, 0.58f, 0.18f),
                CustomSpellEffectKind.Stability => new Color(0.74f, 0.55f, 0.32f),
                _ => fallback
            };
        }

        private static Color DamageColorFor(CustomSpellEffectKind? effect)
        {
            return effect switch
            {
                CustomSpellEffectKind.Electric => new Color(1f, 0.80f, 0.18f),
                CustomSpellEffectKind.Ice => new Color(0.65f, 0.92f, 1f),
                CustomSpellEffectKind.Focus => new Color(1f, 0.46f, 0.18f),
                CustomSpellEffectKind.Stability => new Color(0.95f, 0.64f, 0.32f),
                _ => new Color(1f, 0.88f, 0.30f)
            };
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
                    TakePlayerDamage("위험 지대 접촉", hazard.position);
                    player.position = safePosition;
                    velocity = Vector2.zero;
                    ShowMagicNote(
                        "균열이 몸을 밀어냈습니다. 가까운 안전 지점에서 다시 시작합니다.\n" +
                        "다음: 아직 완료하지 않은 고정 목표를 닿기 쉬운 안전 지점 앞에서 다시 겨냥하세요.",
                        MentorMood.Frown);
                    audioDirector?.PlaySfx(AudioCue.HazardReset, 0.95f);
                    ShowToast("균열 접촉 - 안전 지점 복귀", hazard.color, strong: true);
                    pulses.Add(new ParticlePulse(hazard.position, hazard.color, weak: true));
                    return;
                }
            }
        }

        private void TickFirstFloorOnboarding()
        {
            if (!gameplayInputEnabled || HasEndingReport || floorController.Current.number != 1 || castsOnCurrentFloor > 0)
            {
                return;
            }

            var elapsed = Time.time - floorEnteredAt;
            if (!firstFloorGhostShown && elapsed >= 8f)
            {
                firstFloorGhostShown = true;
                PlayerPrefs.SetInt("MagicExamHall.FirstFloorGhostSeen", 1);
                var goal = activeGoals.FirstOrDefault(item => !item.completed && item.requiredBase.HasValue) ?? activeGoals.FirstOrDefault(item => item.requiredBase.HasValue);
                if (goal != null && goal.requiredBase.HasValue)
                {
                    ShowMagicNote("우클릭을 누른 채 표식 근처 바닥에 선을 그어 보세요. 흐릿한 선을 따라 첫 문양을 완성하면 됩니다.", MentorMood.Neutral);
                    PlayGhostGesture(goal.requiredBase.Value, goal.position);
                }
            }

            if (!firstFloorLongSilenceShown && elapsed >= 300f)
            {
                firstFloorLongSilenceShown = true;
                ShowMagicNote("아직 시전하지 않았다면 목표 표식 바로 옆에서 시작하세요. 물은 닫힌 원, 바람은 평행한 세 줄입니다.", MentorMood.Neutral);
            }
        }

        private void PlayGhostGesture(SpellFamily family, Vector2 position)
        {
            var strokes = Offset(GestureRecognizer.CreateCanonicalSamples(family, 1.35f, 0.03f), position, 0.8f);
            foreach (var stroke in strokes)
            {
                if (stroke.Count < 2)
                {
                    continue;
                }

                var body = new GameObject($"Ghost Gesture {family}");
                body.transform.SetParent(transform, true);
                var line = body.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = stroke.Count;
                line.startWidth = 0.055f;
                line.endWidth = 0.055f;
                line.material = new Material(Shader.Find("Sprites/Default"));
                line.startColor = new Color(1f, 1f, 1f, 0f);
                line.endColor = new Color(1f, 1f, 1f, 0f);
                line.sortingOrder = 37;
                for (var index = 0; index < stroke.Count; index++)
                {
                    line.SetPosition(index, new Vector3(stroke[index].position.x, stroke[index].position.y, -0.24f));
                }

                ghostTraces.Add(new GhostTraceView(body, line, FamilyColor(family)));
            }

            pulses.Add(new ParticlePulse(position, FamilyColor(family), scaleMultiplier: 0.85f, durationSeconds: 0.8f, sortingOrder: 31));
        }

        private void TickGhostTraces()
        {
            for (var index = ghostTraces.Count - 1; index >= 0; index--)
            {
                var trace = ghostTraces[index];
                trace.age += Time.deltaTime;
                var t = Mathf.Clamp01(trace.age / 0.8f);
                var reveal = Mathf.Sin(t * Mathf.PI);
                if (trace.line != null)
                {
                    var color = Color.Lerp(Color.white, trace.tint, 0.38f);
                    color.a = 0.68f * reveal;
                    trace.line.startColor = color;
                    trace.line.endColor = color;
                }

                if (trace.age >= 0.8f)
                {
                    if (trace.body != null)
                    {
                        Destroy(trace.body);
                    }
                    ghostTraces.RemoveAt(index);
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

        private void TickHostileEntityContacts()
        {
            if (floorController.Current.number != 4 || player == null)
            {
                return;
            }

            foreach (var goal in activeGoals)
            {
                if (goal.completed || goal.entityBody == null)
                {
                    continue;
                }

                var targetPosition = (Vector2)goal.entityBody.transform.position;
                if (Vector2.Distance(player.position, targetPosition) > 0.78f)
                {
                    continue;
                }

                TakePlayerDamage("적대적 entity 접촉", targetPosition);
                MovePlayerTo(safePosition);
                ShowMagicNote("적대적 대상에 너무 가까이 닿았습니다. 거리를 두고 다시 시도하세요.", MentorMood.Frown);
                pulses.Add(new ParticlePulse(targetPosition, goal.color, weak: true, scaleMultiplier: 1.1f, durationSeconds: 0.8f, sortingOrder: 32));
                return;
            }
        }

        private bool TakePlayerDamage(string reason, Vector2 sourcePosition)
        {
            if (Time.time < playerDamageInvulnerableUntil || playerHealthHalfUnits <= 0)
            {
                return false;
            }

            playerHealthHalfUnits = Mathf.Max(0, playerHealthHalfUnits - 1);
            playerDamageInvulnerableUntil = Time.time + PlayerDamageInvulnerabilitySeconds;
            playerBlinkUntil = Time.time + PlayerDamageBlinkSeconds;
            RefreshPlayerBlinkRenderers();
            RefreshHealthUi();
            ShowDamagePopup((Vector2)player.position + new Vector2(0f, 0.95f), "-1/2", new Color(1f, 0.20f, 0.18f));
            pulses.Add(new ParticlePulse(sourcePosition, new Color(1f, 0.18f, 0.14f), weak: true, scaleMultiplier: 0.9f, durationSeconds: 0.7f, sortingOrder: 34));
            if (playerHealthHalfUnits == 0)
            {
                ShowMagicNote($"{reason}: 체력이 모두 사라졌습니다. 안전 지점에서 다시 움직임을 정리하세요.", MentorMood.Frown);
            }

            return true;
        }

        private void RefreshHealthUi()
        {
            for (var index = 0; index < healthHearts.Count; index++)
            {
                healthHearts[index].State = Mathf.Clamp(playerHealthHalfUnits - index * 2, 0, 2);
            }
        }

        private void RefreshPlayerBlinkRenderers()
        {
            playerBlinkRenderers.Clear();
            if (player == null)
            {
                return;
            }

            playerBlinkRenderers.AddRange(player.GetComponentsInChildren<SpriteRenderer>(includeInactive: true));
        }

        private void TickPlayerBlink()
        {
            if (playerBlinkRenderers.Count == 0)
            {
                RefreshPlayerBlinkRenderers();
            }

            if (Time.time >= playerBlinkUntil)
            {
                if (playerBlinkUntil > 0f)
                {
                    SetPlayerBlinkAlpha(1f);
                    playerBlinkUntil = -1f;
                }
                return;
            }

            var wave = (Mathf.Sin(Time.time * 34f) + 1f) * 0.5f;
            SetPlayerBlinkAlpha(Mathf.Lerp(0.34f, 0.96f, wave));
        }

        private void SetPlayerBlinkAlpha(float alpha)
        {
            foreach (var renderer in playerBlinkRenderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                var tint = renderer.color;
                tint.a = alpha;
                renderer.color = tint;
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
                var pulseColor = MagicExamSettings.ColorAssist ? Color.Lerp(pulse.color, Color.white, 0.42f) : pulse.color;
                renderer.color = new Color(pulseColor.r, pulseColor.g, pulseColor.b, Mathf.Lerp(0.8f, 0f, t));
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
            ShowMagicNote($"{SpellLabels.Korean(seal.seal.baseFamily)} seal이 기본 보호막으로 안정화되었습니다.", MentorMood.Neutral);
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
                RefreshQuestLogText();
                return;
            }

            hudTitle.text = practiceMode ? $"연습장 - {floor.title}" : $"층 {floor.number}: {floor.title}";
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
                else if (floor.number == 3)
                {
                    hudCopy.text = $"{floor.objective}\nA/D 또는 ←/→ 이동, Space 또는 ↑ 점프. 시작 책장에서 3층 프리셋을 가져온 뒤 강물/구멍/낭떠러지/빈 공간 표식 위에 도형을 얹으세요.";
                }
                else
                {
                    hudCopy.text = $"{floor.objective}\nWASD 또는 방향키 이동 / 우클릭 hold로 바닥에 직접 문양을 그리세요. Esc/Backspace 취소.";
                }
                floorProgress.text = $"탑 진행 {floorController.CurrentFloorNumber}/{floorController.FloorCount}   목표 {completed}/{activeGoals.Count}   seal {seals.Count}";
            }
            notePanel.gameObject.SetActive(magicNote.Visible);
            noteText.text = magicNote.Text;
            RefreshQuestLogText();
        }

        private void RefreshQuestLogText()
        {
            if (questStatusText == null || questProgressText == null)
            {
                return;
            }

            questStatusText.text = $"{hudTitle.text}\n{ShortLine(hudCopy.text, 86)}";
            questProgressText.text = floorProgress.text;
        }

        private string BuildFloorEntryNote(FloorDefinition floor)
        {
            var lore = FloorEntryLore(floor.number);
            if (floor.number == 1)
            {
                return $"{floor.entryNote}\n{lore}\n{BuildFirstFloorGoalHint()}";
            }

            if (floor.number == CustomReferenceFloorNumber)
            {
                return $"{floor.entryNote}\n{lore}\n좌측 책장 근처에서 말풍선의 보기 버튼을 누르면 base별 커스텀 도형을 슬롯에 들여올 수 있습니다.";
            }

            if (floor.number == 3)
            {
                return $"{floor.entryNote}\n{lore}\n시작 구간의 책장에서 3층 프리셋 도형을 가져온 뒤 강물, 깨진 구멍, 낭떠러지, 빈 공간 표식 근처에서 기본 문양과 커스텀 도형을 순서대로 사용하세요.";
            }

            return IsFinalFloor
                ? $"{floor.entryNote}\n{lore}\n{BuildNextFinalGoalHint()}"
                : $"{floor.entryNote}\n{lore}";
        }

        private static string FloorEntryLore(int floorNumber)
        {
            return floorNumber switch
            {
                1 => "이 바닥의 홈은 수천 번의 첫 획이 남긴 자국이다.",
                2 => "벽화의 장식 문양은 먼저 지나간 입학생들의 서명이다.",
                3 => "다리는 오래전에 끊겼고, 아무도 같은 방법으로 건너지 않았다.",
                4 => "균열은 탑이 늙어가는 속도다.",
                _ => "성좌심은 통과한 이름들을 별자리로 기억한다."
            };
        }

        private static string FloorCompletionLore(int floorNumber)
        {
            return floorNumber switch
            {
                1 => "이 층이 이렇게 밝은 것은 오랜만이다.",
                2 => "벽화가 새 서명을 기다리기 시작했다.",
                3 => "탑이 너의 길을 지도에 더했다.",
                4 => "탑의 통증이 한 칸 줄었다.",
                _ => ""
            };
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
            SaveCurrentQuestChecklistScore("ending");
            reportPanel.gameObject.SetActive(true);
            notePanel.gameObject.SetActive(false);
            resultPanel.gameObject.SetActive(false);
            floorSkipButton.gameObject.SetActive(false);
            questScrollPanel.gameObject.SetActive(false);
            audioDirector?.PlaySfx(AudioCue.EndingReportOpened, 0.88f);
            mentor?.Say(MentorMood.Neutral, "");
            if (toastPanel != null)
            {
                toastTtl = 0f;
                toastPanel.gameObject.SetActive(false);
            }
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
            reportText.text = endingReport.BuildText(
                trialCounter,
                OutputDirectory,
                finalTrueEnding,
                completedFinalGoals,
                activeGoals.Count,
                magicNote.DiscoveryExcerpts(3));
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

        private void LogOverlayNoSealAttempt(OverlayRecognitionResult result, Vector2 center, int strokeCount, string worldEffect)
        {
            logger.LogAttempt(new AttemptLog
            {
                sessionId = sessionId,
                trialId = trialCounter.ToString(CultureInfo.InvariantCulture),
                targetFamily = "",
                recognizedFamily = result.OperatorText,
                phase = SpellPhase.Overlay.ToString(),
                baseFamily = "",
                overlayStack = "",
                sealId = "",
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
                success = false,
                hintShown = true,
                assistLevel = CurrentAssistLevel,
                assisted = false
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
            foreach (var queue in buffQueues)
            {
                queue.Destroy();
            }
            buffQueues.Clear();
            spriteAccentAnimations.Clear();
            elementalEntities.Clear();
            shelfGuideArrows.Clear();
            activeStageGates.Clear();
            stageEntityObjects.Clear();
            stageEffectObjects.Clear();
            CustomShapeEventObjectCountForTests = 0;
            LastElementalReactionCountForTests = 0;
            LastElementalReactionSummaryForTests = "";
            LastDamagePopupTextForTests = "";
            LastBuffLabelForTests = "";
            LastCustomShapeEventKindForTests = "";
            LastCustomShapeEventLabelForTests = "";
            LastCustomShapeEventDirectionForTests = Vector2.right;
            foreach (var ghostTrace in ghostTraces)
            {
                if (ghostTrace.body != null)
                {
                    Destroy(ghostTrace.body);
                }
            }
            ghostTraces.Clear();
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
            PixelRenderSetup.ConfigureSpriteLight(body, kind, primary, secondary, name);
            RegisterElementalEntityForSprite(body, name, kind, scale, tiled, pixelSprite.tiledSize);
            return body;
        }

        private void RegisterElementalEntityForSprite(
            GameObject body,
            string entityName,
            PixelSpriteKind kind,
            Vector3 scale,
            bool tiled,
            Vector2 tiledSize)
        {
            if (body == null || !ElementalInteractionSystem.IsPhysicalElementalSprite(entityName, kind))
            {
                return;
            }

            var material = ElementalInteractionSystem.InferMaterial(entityName, kind);
            if (material == ElementalMaterial.None)
            {
                return;
            }

            var renderer = body.GetComponent<SpriteRenderer>();
            var entity = body.GetComponent<ElementalEntity>() ?? body.AddComponent<ElementalEntity>();
            entity.Configure(
                entityName,
                material,
                ElementalInteractionSystem.InferResponseRadius(scale, tiled, tiledSize),
                ElementalInteractionSystem.InferWindMovable(entityName, kind, tiled),
                renderer);
            if (!elementalEntities.Contains(entity))
            {
                elementalEntities.Add(entity);
            }
        }

        private void RegisterExistingElementalSprites()
        {
            foreach (var sprite in FindObjectsByType<PixelSpriteView>(FindObjectsSortMode.None))
            {
                if (sprite == null)
                {
                    continue;
                }

                RegisterElementalEntityForSprite(
                    sprite.gameObject,
                    sprite.name,
                    sprite.kind,
                    sprite.transform.localScale,
                    sprite.tiled,
                    sprite.tiledSize);
            }
        }

        private static void AddPlatformCollider(GameObject body, Vector2 size)
        {
            if (body == null)
            {
                return;
            }

            var platformCollider = body.GetComponent<BoxCollider2D>();
            if (platformCollider == null)
            {
                platformCollider = body.AddComponent<BoxCollider2D>();
            }
            platformCollider.size = size.sqrMagnitude <= 0.001f ? Vector2.one : size;
            platformCollider.offset = Vector2.zero;
            platformCollider.isTrigger = false;
        }

        private static void ApplySpriteOverride(GameObject body, Sprite spriteOverride)
        {
            if (body == null || spriteOverride == null)
            {
                return;
            }

            var renderer = body.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sprite = spriteOverride;
            }
        }

        private Text CreateGoalLabel(WorldStateGoal goal, Transform parent)
        {
            var stageLabel = floorController?.Current.number == 3;
            var visualRequirement = floorController != null && floorController.Current.number <= 3;
            var labelSize = stageLabel ? new Vector2(210f, 66f) : new Vector2(220f, 88f);
            var canvasObject = new GameObject($"{goal.title} Goal Label");
            canvasObject.transform.SetParent(parent, false);
            canvasObject.transform.position = goal.position + (stageLabel ? new Vector2(0f, 0.78f) : new Vector2(0f, -0.86f));
            var worldCanvas = canvasObject.AddComponent<Canvas>();
            worldCanvas.renderMode = RenderMode.WorldSpace;
            worldCanvas.overrideSorting = true;
            worldCanvas.sortingOrder = 42;
            var rect = canvasObject.GetComponent<RectTransform>() ?? canvasObject.AddComponent<RectTransform>();
            rect.sizeDelta = labelSize;
            canvasObject.transform.localScale = Vector3.one * (stageLabel ? 0.014f : 0.016f);

            var backgroundAlpha = visualRequirement ? 0.16f : 0.86f;
            var background = CreateImage("Goal Label Background", canvasObject.transform, Vector2.zero, labelSize, Anchor.Center, new Color(0.02f, 0.025f, 0.04f, backgroundAlpha));
            background.raycastTarget = false;
            var textPosition = visualRequirement
                ? new Vector2(0f, stageLabel ? 15f : 24f)
                : Vector2.zero;
            var textSize = visualRequirement
                ? new Vector2(labelSize.x - 12f, stageLabel ? 28f : 32f)
                : labelSize;
            if (visualRequirement)
            {
                var titleBacking = CreateImage(
                    "Goal Label Title Backing",
                    canvasObject.transform,
                    textPosition,
                    new Vector2(textSize.x - 18f, stageLabel ? 30f : 34f),
                    Anchor.Center,
                    new Color(0.006f, 0.010f, 0.018f, stageLabel ? 0.54f : 0.58f));
                titleBacking.raycastTarget = false;
            }

            var text = CreateText("Goal Label Text", canvasObject.transform, visualRequirement ? goal.title : goal.OpenLabel, stageLabel ? 22 : 24, FontStyle.Bold, textPosition, textSize, Anchor.Center);
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.Lerp(goal.color, Color.white, 0.45f);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.lineSpacing = 0.88f;
            text.raycastTarget = false;
            if (visualRequirement)
            {
                CreateGoalRequirementIconRow(goal, canvasObject.transform, stageLabel);
            }

            return text;
        }

        private void CreateGoalRequirementIconRow(WorldStateGoal goal, Transform parent, bool stageLabel)
        {
            var glyphs = BuildGoalRequirementGlyphs(goal);
            if (glyphs.Count == 0)
            {
                return;
            }

            var iconSize = stageLabel ? 24f : 32f;
            var plusWidth = stageLabel ? 13f : 18f;
            var gap = stageLabel ? 4f : 6f;
            var totalWidth = glyphs.Count * iconSize + Mathf.Max(0, glyphs.Count - 1) * (plusWidth + gap * 2f);
            var row = CreatePanel(
                $"Goal Requirement Icon Row {goal.id}",
                parent,
                new Vector2(0f, stageLabel ? -16f : -22f),
                new Vector2(totalWidth + (stageLabel ? 10f : 16f), iconSize + (stageLabel ? 8f : 12f)),
                Anchor.Center,
                new Color(0.005f, 0.008f, 0.015f, stageLabel ? 0.68f : 0.78f));
            row.GetComponent<Image>().raycastTarget = false;
            AddPanelBorder(row, Color.Lerp(goal.color, Color.white, 0.28f), stageLabel ? 1.2f : 1.6f);

            var x = -totalWidth * 0.5f + iconSize * 0.5f;
            for (var index = 0; index < glyphs.Count; index++)
            {
                if (index > 0)
                {
                    var plus = CreateText(
                        $"Goal Requirement Plus {goal.id} {index}",
                        row,
                        "+",
                        stageLabel ? 20 : 26,
                        FontStyle.Bold,
                        new Vector2(x - iconSize * 0.5f - gap - plusWidth * 0.5f, 0f),
                        new Vector2(plusWidth, iconSize),
                        Anchor.Center);
                    plus.alignment = TextAnchor.MiddleCenter;
                    plus.color = new Color(1f, 1f, 1f, 0.82f);
                    plus.raycastTarget = false;
                }

                var glyph = glyphs[index];
                var image = CreateImage(
                    $"Goal Requirement Icon {goal.id} {index + 1}",
                    row,
                    new Vector2(x, 0f),
                    new Vector2(iconSize, iconSize),
                    Anchor.Center,
                    Color.white);
                image.sprite = PixelArtFactory.CreateSprite($"Goal Requirement {goal.id} {index + 1}", glyph.primary, glyph.secondary, glyph.kind);
                image.preserveAspect = true;
                image.raycastTarget = false;
                x += iconSize + plusWidth + gap * 2f;
            }
        }

        private static List<GoalRequirementGlyph> BuildGoalRequirementGlyphs(WorldStateGoal goal)
        {
            var glyphs = new List<GoalRequirementGlyph>();
            if (goal.comboBase.HasValue)
            {
                glyphs.Add(FamilyRequirementGlyph(goal.comboBase.Value));
                if (goal.comboOverlay.HasValue)
                {
                    glyphs.Add(OverlayRequirementGlyph(goal.comboOverlay.Value));
                }

                return glyphs;
            }

            if (goal.requiredBase.HasValue)
            {
                glyphs.Add(FamilyRequirementGlyph(goal.requiredBase.Value));
                if (goal.requiresCustomShape || goal.requiredCustomSpell.HasValue)
                {
                    foreach (var token in GoalRequirementShapeTokens(goal))
                    {
                        glyphs.Add(ShapeRequirementGlyph(token, goal.color));
                    }
                }

                return glyphs;
            }

            if (goal.requiredOverlay.HasValue)
            {
                glyphs.Add(OverlayRequirementGlyph(goal.requiredOverlay.Value));
            }

            return glyphs;
        }

        private static IReadOnlyList<string> GoalRequirementShapeTokens(WorldStateGoal goal)
        {
            if (goal.requirementShapeTokens.Count > 0)
            {
                return goal.requirementShapeTokens;
            }

            if (!goal.requiredCustomSpell.HasValue)
            {
                return Array.Empty<string>();
            }

            return goal.requiredCustomSpell.Value switch
            {
                CustomSpellEffectKind.Ice => new[] { "hexagon" },
                CustomSpellEffectKind.Electric => new[] { "line" },
                CustomSpellEffectKind.Cleanse => new[] { "ellipse" },
                CustomSpellEffectKind.Focus => new[] { "star" },
                CustomSpellEffectKind.Flow => new[] { "wave" },
                CustomSpellEffectKind.Connection => new[] { "brace" },
                CustomSpellEffectKind.Stability => new[] { "rect" },
                CustomSpellEffectKind.LivingBridge => new[] { "arrow", "rect" },
                CustomSpellEffectKind.WindPlatform => new[] { "rect" },
                _ => Array.Empty<string>()
            };
        }

        private static GoalRequirementGlyph FamilyRequirementGlyph(SpellFamily family)
        {
            var color = FamilyColor(family);
            return new GoalRequirementGlyph(FamilyRuneKind(family), color, Color.Lerp(color, Color.white, 0.48f));
        }

        private static GoalRequirementGlyph OverlayRequirementGlyph(OverlayOperator op)
        {
            var color = OverlayColor(op);
            var kind = op switch
            {
                OverlayOperator.SteelBrace => PixelSpriteKind.ShapeRect,
                OverlayOperator.ElectricFork => PixelSpriteKind.ShapeLine,
                OverlayOperator.IceBar => PixelSpriteKind.ShapeLine,
                OverlayOperator.SoulDot => PixelSpriteKind.ShapeEllipse,
                OverlayOperator.VoidCut => PixelSpriteKind.ShapeLine,
                OverlayOperator.MartialAxis => PixelSpriteKind.ShapeCross,
                _ => PixelSpriteKind.RuneCircle
            };
            return new GoalRequirementGlyph(kind, color, Color.Lerp(color, Color.white, 0.52f));
        }

        private static GoalRequirementGlyph ShapeRequirementGlyph(string token, Color fallbackColor)
        {
            var primary = new Color(0.96f, 0.96f, 0.96f, 1f);
            var secondary = new Color(0.68f, 0.68f, 0.68f, 1f);
            return new GoalRequirementGlyph(ShapeTokenSpriteKind(token), primary, secondary);
        }

        private static PixelSpriteKind ShapeTokenSpriteKind(string token)
        {
            return (token ?? "").Trim().ToLowerInvariant() switch
            {
                "line" => PixelSpriteKind.ShapeLine,
                "arrow" => PixelSpriteKind.ShapeArrow,
                "rect" or "roundrect" => PixelSpriteKind.ShapeRect,
                "ellipse" => PixelSpriteKind.ShapeEllipse,
                "hexagon" => PixelSpriteKind.ShapeHexagon,
                "brace" or "curve" or "arc" or "wave" => PixelSpriteKind.ShapeBrace,
                "cross" => PixelSpriteKind.ShapeCross,
                _ => PixelSpriteKind.RuneCircle
            };
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
            body.AddComponent<CanvasRenderer>();
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

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
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

        private enum BuffOwnerKind
        {
            Player,
            Target
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

        private enum QuestChecklistConditionKind
        {
            GoalCompleted,
            GoalsCompletedAtLeast,
            AllGoalsCompleted,
            ReferencePanelOpened,
            ReferenceImportsAtLeast
        }

        private sealed class QuestChecklistItemDefinition
        {
            public readonly string id;
            public readonly string label;
            public readonly QuestChecklistConditionKind kind;
            public readonly string goalId;
            public readonly int threshold;

            private QuestChecklistItemDefinition(string id, string label, QuestChecklistConditionKind kind, string goalId = "", int threshold = 0)
            {
                this.id = id;
                this.label = label;
                this.kind = kind;
                this.goalId = goalId;
                this.threshold = threshold;
            }

            public static QuestChecklistItemDefinition Goal(string id, string label, string goalId)
            {
                return new QuestChecklistItemDefinition(id, label, QuestChecklistConditionKind.GoalCompleted, goalId);
            }

            public static QuestChecklistItemDefinition GoalsAtLeast(string id, string label, int threshold)
            {
                return new QuestChecklistItemDefinition(id, label, QuestChecklistConditionKind.GoalsCompletedAtLeast, threshold: threshold);
            }

            public static QuestChecklistItemDefinition AllGoals(string id, string label)
            {
                return new QuestChecklistItemDefinition(id, label, QuestChecklistConditionKind.AllGoalsCompleted);
            }

            public static QuestChecklistItemDefinition ReferencePanel(string id, string label)
            {
                return new QuestChecklistItemDefinition(id, label, QuestChecklistConditionKind.ReferencePanelOpened);
            }

            public static QuestChecklistItemDefinition ReferenceImports(string id, string label, int threshold)
            {
                return new QuestChecklistItemDefinition(id, label, QuestChecklistConditionKind.ReferenceImportsAtLeast, threshold: threshold);
            }
        }

        private sealed class QuestChecklistEntry
        {
            public readonly QuestChecklistItemDefinition definition;
            public bool completed;

            public QuestChecklistEntry(QuestChecklistItemDefinition definition)
            {
                this.definition = definition;
            }
        }

        private sealed class QuestChecklistState
        {
            public readonly int floorNumber;
            public readonly string floorTitle;
            public readonly List<QuestChecklistEntry> entries;

            public QuestChecklistState(int floorNumber, string floorTitle, IReadOnlyList<QuestChecklistItemDefinition> definitions)
            {
                this.floorNumber = floorNumber;
                this.floorTitle = floorTitle;
                entries = definitions.Select(definition => new QuestChecklistEntry(definition)).ToList();
            }

            public int CompletedCount => entries.Count(entry => entry.completed);
            public int TotalCount => entries.Count;
        }

        private sealed class QuestChecklistItemView
        {
            private readonly GameObject row;
            private readonly Image box;
            private readonly QuestCheckMarkGraphic check;
            private readonly Text label;

            public QuestChecklistItemView(GameObject row, Image box, QuestCheckMarkGraphic check, Text label)
            {
                this.row = row;
                this.box = box;
                this.check = check;
                this.label = label;
            }

            public void Refresh(bool completed)
            {
                if (box != null)
                {
                    box.color = completed
                        ? new Color(0.96f, 0.82f, 0.54f, 0.72f)
                        : new Color(0.92f, 0.78f, 0.52f, 0.55f);
                }

                if (check != null)
                {
                    check.gameObject.SetActive(completed);
                    check.SetVerticesDirty();
                }

                if (label != null)
                {
                    label.color = completed
                        ? new Color(0.13f, 0.07f, 0.035f, 0.94f)
                        : new Color(0.18f, 0.09f, 0.035f, 0.96f);
                }
            }

            public void Destroy()
            {
                if (row == null)
                {
                    return;
                }

                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(row);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(row);
                }
            }
        }

        private sealed class QuestChecklistSnapshot
        {
            public int floorNumber;
            public string floorTitle = "";
            public int completedCount;
            public int totalCount;
            public string reason = "";
            public int elapsedMs;
            public string items = "";
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

        private readonly struct GoalRequirementGlyph
        {
            public readonly PixelSpriteKind kind;
            public readonly Color primary;
            public readonly Color secondary;

            public GoalRequirementGlyph(PixelSpriteKind kind, Color primary, Color secondary)
            {
                this.kind = kind;
                this.primary = primary;
                this.secondary = secondary;
            }
        }

        private sealed class ProcessedSpell
        {
            public BaseRecognitionResult baseResult = null;
            public OverlayRecognitionResult overlayResult = null;
        }

        private enum SpriteAccentAnimationKind
        {
            RuneIdle,
            RuneActive,
            CandleFlicker,
            WaterFlow,
            MistDrift,
            PortalShimmer,
            StageEffectGlow
        }

        private sealed class SpriteAccentAnimation
        {
            private readonly GameObject body;
            private readonly SpriteRenderer renderer;
            private readonly SpriteAccentAnimationKind kind;
            private readonly Vector3 baseScale;
            private readonly Vector2 anchor;
            private readonly Color baseColor;
            private readonly Quaternion baseRotation;
            private readonly float phase;

            public SpriteAccentAnimation(GameObject body, SpriteAccentAnimationKind kind, float phase)
            {
                this.body = body;
                this.kind = kind;
                this.phase = phase;
                renderer = body == null ? null : body.GetComponent<SpriteRenderer>();
                baseScale = body == null ? Vector3.one : body.transform.localScale;
                anchor = body == null ? Vector2.zero : body.transform.position;
                baseColor = renderer == null ? Color.white : renderer.color;
                baseRotation = body == null ? Quaternion.identity : body.transform.rotation;
            }

            public bool IsActive => body != null;
            public string Name => body == null ? "" : body.name;
            public Vector3 CurrentScale => body == null ? Vector3.zero : body.transform.localScale;
            public Vector2 CurrentPosition => body == null ? Vector2.zero : body.transform.position;

            public bool TargetEquals(GameObject target)
            {
                return body == target;
            }

            public void Tick(float time, float deltaTime)
            {
                if (body == null)
                {
                    return;
                }

                var slow = Mathf.Sin(time * 2.05f + phase);
                var fast = Mathf.Sin(time * 8.6f + phase * 1.7f);
                var alpha = 1f;
                var scale = baseScale;
                var position = anchor;
                var rotation = baseRotation;

                switch (kind)
                {
                    case SpriteAccentAnimationKind.RuneIdle:
                        scale = baseScale * (1f + slow * 0.028f);
                        alpha = 0.86f + (slow + 1f) * 0.07f;
                        break;
                    case SpriteAccentAnimationKind.RuneActive:
                        scale = baseScale * (1.015f + slow * 0.045f);
                        alpha = 0.94f + (slow + 1f) * 0.03f;
                        break;
                    case SpriteAccentAnimationKind.CandleFlicker:
                        scale = baseScale * (1f + slow * 0.030f + fast * 0.018f);
                        alpha = 0.78f + (fast + 1f) * 0.10f + (slow + 1f) * 0.04f;
                        break;
                    case SpriteAccentAnimationKind.WaterFlow:
                        position = anchor + new Vector2(Mathf.Sin(time * 1.15f + phase) * 0.075f, Mathf.Sin(time * 2.3f + phase) * 0.018f);
                        scale = new Vector3(baseScale.x * (1f + slow * 0.025f), baseScale.y, baseScale.z);
                        alpha = 0.76f + (slow + 1f) * 0.10f;
                        break;
                    case SpriteAccentAnimationKind.MistDrift:
                        position = anchor + new Vector2(Mathf.Sin(time * 0.88f + phase) * 0.115f, Mathf.Sin(time * 1.46f + phase) * 0.028f);
                        scale = baseScale * (1f + slow * 0.035f);
                        alpha = 0.54f + (slow + 1f) * 0.13f;
                        break;
                    case SpriteAccentAnimationKind.PortalShimmer:
                        scale = baseScale * (1f + slow * 0.042f);
                        rotation = baseRotation * Quaternion.Euler(0f, 0f, Mathf.Sin(time * 1.55f + phase) * 2.4f);
                        alpha = 0.86f + (slow + 1f) * 0.07f;
                        break;
                    case SpriteAccentAnimationKind.StageEffectGlow:
                        scale = baseScale * (1f + slow * 0.030f);
                        alpha = 0.70f + (slow + 1f) * 0.12f;
                        break;
                }

                body.transform.position = position;
                body.transform.localScale = scale;
                body.transform.rotation = rotation;
                if (renderer != null)
                {
                    renderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Clamp01(baseColor.a * alpha));
                }
            }
        }

        private sealed class FloatingGuideArrow
        {
            private readonly GameObject body;
            private readonly Vector2 anchor;
            private readonly float phase;
            private readonly float baseScale;
            private readonly float baseAlpha;
            private float verticalOffset;
            private float verticalVelocity;

            public FloatingGuideArrow(GameObject body, Vector2 anchor, float phase, float baseScale, float baseAlpha)
            {
                this.body = body;
                this.anchor = anchor;
                this.phase = phase;
                this.baseScale = baseScale;
                this.baseAlpha = baseAlpha;
            }

            public bool IsActive => body != null;

            public void Tick(float time, float deltaTime)
            {
                if (body == null)
                {
                    return;
                }

                var targetOffset = Mathf.Sin(time * 2.25f + phase) * 0.18f;
                verticalVelocity += (targetOffset - verticalOffset) * 30f * deltaTime;
                verticalVelocity *= Mathf.Exp(-4.8f * deltaTime);
                verticalOffset += verticalVelocity * deltaTime;

                body.transform.position = anchor + new Vector2(0f, verticalOffset);
                body.transform.localScale = Vector3.one * baseScale * (1f + Mathf.Sin(time * 3.15f + phase) * 0.025f);
                body.transform.rotation = Quaternion.identity;

                var renderer = body.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.color = new Color(1f, 1f, 1f, baseAlpha * (0.82f + Mathf.Sin(time * 2.8f + phase) * 0.12f));
                }
            }
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
                halfSize = new Vector2(size.x * 0.5f, size.y * 0.5f + StageGatePlayerCenterVerticalPadding);
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

        private sealed class BuffQueueView
        {
            private const int MaxVisibleBuffs = 6;
            private readonly Transform owner;
            private readonly RectTransform rootRect;
            private readonly Font font;
            private readonly List<BuffSlotView> slots = new();

            public BuffQueueView(Transform owner, BuffOwnerKind ownerKind, Font font)
            {
                this.owner = owner;
                OwnerKind = ownerKind;
                this.font = font;
                root = new GameObject(ownerKind == BuffOwnerKind.Player ? "Buff Queue Player" : $"Buff Queue {owner.name}");
                root.transform.SetParent(owner, false);
                root.transform.localPosition = ownerKind == BuffOwnerKind.Player
                    ? new Vector3(0f, -0.76f, 0f)
                    : new Vector3(0f, -0.68f, 0f);
                root.transform.localScale = Vector3.one * 0.0135f;
                var canvas = root.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.overrideSorting = true;
                canvas.sortingOrder = 76;
                rootRect = root.GetComponent<RectTransform>() ?? root.AddComponent<RectTransform>();
                rootRect.sizeDelta = new Vector2(164f, 28f);
            }

            public readonly GameObject root;
            public BuffOwnerKind OwnerKind { get; }
            public bool IsActive => root != null && owner != null;
            public int ActiveBuffCount => slots.Count(slot => !slot.Expired(Time.time));
            public float FirstFillAmount => slots.FirstOrDefault(slot => !slot.Expired(Time.time))?.FillAmount ?? 0f;

            public bool IsFor(Transform candidate)
            {
                return candidate != null && ReferenceEquals(owner, candidate);
            }

            public void Add(string label, Color color, PixelSpriteKind iconKind, float durationSeconds, float now)
            {
                for (var index = slots.Count - 1; index >= 0; index--)
                {
                    if (string.Equals(slots[index].Label, label, StringComparison.Ordinal))
                    {
                        slots[index].Destroy();
                        slots.RemoveAt(index);
                    }
                }

                while (slots.Count >= MaxVisibleBuffs)
                {
                    slots[0].Destroy();
                    slots.RemoveAt(0);
                }

                slots.Add(new BuffSlotView(rootRect, label, color, iconKind, durationSeconds, now, font));
                Layout();
            }

            public void Clear()
            {
                foreach (var slot in slots)
                {
                    slot.Destroy();
                }

                slots.Clear();
            }

            public bool Tick(float now)
            {
                if (!IsActive)
                {
                    return false;
                }

                for (var index = slots.Count - 1; index >= 0; index--)
                {
                    var slot = slots[index];
                    if (slot.Expired(now))
                    {
                        slot.Destroy();
                        slots.RemoveAt(index);
                        continue;
                    }

                    slot.Tick(now);
                }

                Layout();
                return slots.Count > 0;
            }

            public void Destroy()
            {
                Clear();
                if (root != null)
                {
                    UnityEngine.Object.Destroy(root);
                }
            }

            private void Layout()
            {
                var totalWidth = slots.Count * 24f;
                var startX = -totalWidth * 0.5f + 12f;
                for (var index = 0; index < slots.Count; index++)
                {
                    slots[index].SetPosition(new Vector2(startX + index * 24f, 0f));
                }
            }
        }

        private sealed class BuffSlotView
        {
            private readonly GameObject root;
            private readonly BuffCooldownClockGraphic cooldown;
            private readonly Text labelText;
            private readonly float createdAt;
            private readonly float durationSeconds;

            public BuffSlotView(
                Transform parent,
                string label,
                Color color,
                PixelSpriteKind iconKind,
                float durationSeconds,
                float now,
                Font font)
            {
                Label = label;
                this.durationSeconds = Mathf.Max(durationSeconds, 0.1f);
                createdAt = now;

                root = new GameObject($"Buff Slot {label}");
                root.transform.SetParent(parent, false);
                var rect = root.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(22f, 22f);
                var background = root.AddComponent<Image>();
                background.color = Color.Lerp(color, Color.black, 0.18f);
                background.material = PixelMaterialProvider.UiMaterial;
                background.raycastTarget = false;
                AddUiBorder(rect, new Color(0.03f, 0.035f, 0.045f, 0.92f), 1.25f);

                var iconObject = new GameObject("Buff Slot Icon");
                iconObject.transform.SetParent(root.transform, false);
                var iconRect = iconObject.AddComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.offsetMin = new Vector2(3f, 3f);
                iconRect.offsetMax = new Vector2(-3f, -3f);
                var icon = iconObject.AddComponent<Image>();
                icon.sprite = PixelArtFactory.CreateSprite($"Buff Icon {label}", Color.Lerp(color, Color.white, 0.12f), Color.white, iconKind);
                icon.material = PixelMaterialProvider.UiMaterial;
                icon.preserveAspect = true;
                icon.raycastTarget = false;

                var cooldownObject = new GameObject("Buff Cooldown Clock Fill");
                cooldownObject.transform.SetParent(root.transform, false);
                var cooldownRect = cooldownObject.AddComponent<RectTransform>();
                cooldownRect.anchorMin = Vector2.zero;
                cooldownRect.anchorMax = Vector2.one;
                cooldownRect.offsetMin = Vector2.zero;
                cooldownRect.offsetMax = Vector2.zero;
                cooldown = cooldownObject.AddComponent<BuffCooldownClockGraphic>();
                cooldown.color = new Color(0f, 0f, 0f, 0.58f);
                cooldown.raycastTarget = false;

                var labelObject = new GameObject("Buff Slot Label");
                labelObject.transform.SetParent(root.transform, false);
                var labelRect = labelObject.AddComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(0f, -1f);
                labelRect.offsetMax = Vector2.zero;
                labelText = labelObject.AddComponent<Text>();
                labelText.font = font;
                labelText.fontSize = 7;
                labelText.fontStyle = FontStyle.Bold;
                labelText.alignment = TextAnchor.LowerCenter;
                labelText.color = Color.white;
                labelText.text = ShortBuffLabel(label);
                labelText.raycastTarget = false;
            }

            public string Label { get; }
            public float FillAmount => cooldown == null ? 0f : cooldown.FillAmount;

            public bool Expired(float now)
            {
                return now >= createdAt + durationSeconds;
            }

            public void Tick(float now)
            {
                if (cooldown == null)
                {
                    return;
                }

                cooldown.FillAmount = Mathf.Clamp01((now - createdAt) / durationSeconds);
            }

            public void SetPosition(Vector2 anchoredPosition)
            {
                if (root == null)
                {
                    return;
                }

                root.GetComponent<RectTransform>().anchoredPosition = anchoredPosition;
            }

            public void Destroy()
            {
                if (root != null)
                {
                    UnityEngine.Object.Destroy(root);
                }
            }

            private static string ShortBuffLabel(string label)
            {
                return string.IsNullOrWhiteSpace(label)
                    ? ""
                    : label.Length <= 2 ? label : label[..2];
            }

            private static void AddUiBorder(RectTransform target, Color color, float thickness)
            {
                var borderObject = new GameObject($"{target.name} Border");
                borderObject.transform.SetParent(target, false);
                var rect = borderObject.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                var border = borderObject.AddComponent<CustomShapeRectBorder>();
                border.color = color;
                border.thickness = thickness;
                border.raycastTarget = false;
            }
        }

        private sealed class DamagePopupView
        {
            private readonly Text mainText;
            private readonly List<Text> shadowTexts = new();
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
                rect.sizeDelta = new Vector2(128f, 54f);
                root.transform.localScale = Vector3.one * 0.017f;

                shadowTexts.Add(CreateDamageText("Damage Popup Shadow NW", value, font, new Vector2(-2.2f, 2f), new Color(0f, 0f, 0f, 0.82f), 36));
                shadowTexts.Add(CreateDamageText("Damage Popup Shadow SE", value, font, new Vector2(2.2f, -2f), new Color(0f, 0f, 0f, 0.78f), 36));
                shadowTexts.Add(CreateDamageText("Damage Popup Shadow Drop", value, font, new Vector2(0f, -3.6f), new Color(0.18f, 0.02f, 0.01f, 0.62f), 36));
                mainText = CreateDamageText("Damage Popup Main Text", value, font, Vector2.zero, Color.Lerp(color, Color.white, 0.22f), 38);
                var outline = mainText.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.08f, 0.015f, 0.005f, 0.90f);
                outline.effectDistance = new Vector2(1.4f, -1.4f);
            }

            public readonly GameObject root;

            private Text CreateDamageText(string name, string value, Font font, Vector2 offset, Color textColor, int fontSize)
            {
                var textObject = new GameObject(name);
                textObject.transform.SetParent(root.transform, false);
                var textRect = textObject.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = offset;
                textRect.offsetMax = offset;
                var text = textObject.AddComponent<Text>();
                text.font = font;
                text.fontSize = fontSize;
                text.fontStyle = FontStyle.Bold;
                text.alignment = TextAnchor.MiddleCenter;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                text.color = textColor;
                text.text = value;
                text.raycastTarget = false;
                return text;
            }

            public bool Tick(float deltaTime)
            {
                if (root == null)
                {
                    return false;
                }

                age += deltaTime;
                var t = Mathf.Clamp01(age / 1.15f);
                var bounce = Mathf.Sin(Mathf.Clamp01(age / 0.22f) * Mathf.PI) * 0.16f;
                root.transform.position += Vector3.up * (deltaTime * (0.82f + bounce));
                root.transform.localScale = Vector3.one * Mathf.Lerp(0.018f, 0.024f, Mathf.Clamp01(age / 0.24f)) * Mathf.Lerp(1f, 0.88f, t);
                var alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01((t - 0.68f) / 0.32f));
                mainText.color = new Color(color.r, color.g, color.b, alpha);
                foreach (var shadow in shadowTexts)
                {
                    var shadowColor = shadow.color;
                    shadow.color = new Color(shadowColor.r, shadowColor.g, shadowColor.b, alpha * shadowColor.a);
                }
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

        private sealed class GhostTraceView
        {
            public readonly GameObject body;
            public readonly LineRenderer line;
            public readonly Color tint;
            public float age;

            public GhostTraceView(GameObject body, LineRenderer line, Color tint)
            {
                this.body = body;
                this.line = line;
                this.tint = tint;
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

    public sealed class BuffCooldownClockGraphic : MaskableGraphic
    {
        [Range(0f, 1f)]
        [SerializeField]
        private float fillAmount;

        public float FillAmount
        {
            get => fillAmount;
            set
            {
                var next = Mathf.Clamp01(value);
                if (Mathf.Approximately(fillAmount, next))
                {
                    return;
                }

                fillAmount = next;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            if (fillAmount <= 0.001f)
            {
                return;
            }

            var rect = GetPixelAdjustedRect();
            if (fillAmount >= 0.999f)
            {
                AddFullRect(vertexHelper, rect, color);
                return;
            }

            var points = new List<Vector2> { rect.center, ClockPoint(rect, 0f) };
            var stops = new[] { 0.125f, 0.375f, 0.625f, 0.875f };
            foreach (var stop in stops)
            {
                if (fillAmount > stop)
                {
                    points.Add(ClockPoint(rect, stop));
                }
            }
            points.Add(ClockPoint(rect, fillAmount));

            var color32 = (Color32)color;
            for (var index = 0; index < points.Count; index++)
            {
                vertexHelper.AddVert(points[index], color32, Vector2.zero);
            }

            for (var index = 1; index < points.Count - 1; index++)
            {
                vertexHelper.AddTriangle(0, index, index + 1);
            }
        }

        private static Vector2 ClockPoint(Rect rect, float amount)
        {
            amount = Mathf.Repeat(amount, 1f);
            var perimeter = rect.width * 2f + rect.height * 2f;
            var distance = amount * perimeter;
            var halfWidth = rect.width * 0.5f;

            if (distance <= halfWidth)
            {
                return new Vector2(rect.center.x + distance, rect.yMax);
            }

            distance -= halfWidth;
            if (distance <= rect.height)
            {
                return new Vector2(rect.xMax, rect.yMax - distance);
            }

            distance -= rect.height;
            if (distance <= rect.width)
            {
                return new Vector2(rect.xMax - distance, rect.yMin);
            }

            distance -= rect.width;
            if (distance <= rect.height)
            {
                return new Vector2(rect.xMin, rect.yMin + distance);
            }

            distance -= rect.height;
            return new Vector2(rect.xMin + distance, rect.yMax);
        }

        private static void AddFullRect(VertexHelper vertexHelper, Rect rect, Color fillColor)
        {
            var color32 = (Color32)fillColor;
            vertexHelper.AddVert(new Vector2(rect.xMin, rect.yMin), color32, Vector2.zero);
            vertexHelper.AddVert(new Vector2(rect.xMin, rect.yMax), color32, Vector2.zero);
            vertexHelper.AddVert(new Vector2(rect.xMax, rect.yMax), color32, Vector2.zero);
            vertexHelper.AddVert(new Vector2(rect.xMax, rect.yMin), color32, Vector2.zero);
            vertexHelper.AddTriangle(0, 1, 2);
            vertexHelper.AddTriangle(2, 3, 0);
        }
    }

    public sealed class HeartHealthGraphic : MaskableGraphic
    {
        private int state = 2;

        public int State
        {
            get => state;
            set
            {
                var next = Mathf.Clamp(value, 0, 2);
                if (state == next)
                {
                    return;
                }

                state = next;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            var rect = GetPixelAdjustedRect();
            var points = HeartPoints();
            var empty = new Color(0.22f, 0.035f, 0.045f, 0.66f);
            var broken = new Color(0.12f, 0.025f, 0.030f, 0.82f);
            var outline = new Color(0.08f, 0.010f, 0.015f, 0.92f);
            AddPolygon(vertexHelper, Project(points, rect), empty);

            if (state == 2)
            {
                AddPolygon(vertexHelper, Project(points, rect), color);
            }
            else if (state == 1)
            {
                AddPolygon(vertexHelper, Project(ClipByX(points, 0.52f, keepLeft: true), rect), color);
                AddPolygon(vertexHelper, Project(ClipByX(points, 0.48f, keepLeft: false), rect), broken);
                AddZigzag(vertexHelper, rect, outline);
            }
            else
            {
                AddZigzag(vertexHelper, rect, outline);
                AddZigzag(vertexHelper, rect, WithAlpha(color, 0.24f), new Vector2(1.6f, -1.4f));
            }

            AddOutline(vertexHelper, Project(points, rect), outline);
        }

        private static List<Vector2> HeartPoints()
        {
            return new List<Vector2>
            {
                new(0.50f, 0.08f),
                new(0.15f, 0.34f),
                new(0.07f, 0.58f),
                new(0.15f, 0.78f),
                new(0.32f, 0.90f),
                new(0.50f, 0.77f),
                new(0.68f, 0.90f),
                new(0.85f, 0.78f),
                new(0.93f, 0.58f),
                new(0.85f, 0.34f)
            };
        }

        private static List<Vector2> ClipByX(IReadOnlyList<Vector2> source, float clipX, bool keepLeft)
        {
            var output = new List<Vector2>();
            if (source.Count == 0)
            {
                return output;
            }

            var previous = source[^1];
            var previousInside = Inside(previous, clipX, keepLeft);
            foreach (var current in source)
            {
                var currentInside = Inside(current, clipX, keepLeft);
                if (currentInside != previousInside)
                {
                    output.Add(IntersectX(previous, current, clipX));
                }

                if (currentInside)
                {
                    output.Add(current);
                }

                previous = current;
                previousInside = currentInside;
            }

            return output;
        }

        private static bool Inside(Vector2 point, float clipX, bool keepLeft)
        {
            return keepLeft ? point.x <= clipX : point.x >= clipX;
        }

        private static Vector2 IntersectX(Vector2 start, Vector2 end, float clipX)
        {
            var delta = end - start;
            if (Mathf.Abs(delta.x) < 0.0001f)
            {
                return new Vector2(clipX, start.y);
            }

            var t = Mathf.Clamp01((clipX - start.x) / delta.x);
            return Vector2.Lerp(start, end, t);
        }

        private static List<Vector2> Project(IReadOnlyList<Vector2> points, Rect rect)
        {
            var projected = new List<Vector2>(points.Count);
            foreach (var point in points)
            {
                projected.Add(new Vector2(rect.xMin + rect.width * point.x, rect.yMin + rect.height * point.y));
            }

            return projected;
        }

        private static void AddPolygon(VertexHelper vertexHelper, IReadOnlyList<Vector2> points, Color fillColor)
        {
            if (points.Count < 3)
            {
                return;
            }

            var center = Vector2.zero;
            foreach (var point in points)
            {
                center += point;
            }
            center /= points.Count;

            var color32 = (Color32)fillColor;
            var start = vertexHelper.currentVertCount;
            vertexHelper.AddVert(center, color32, Vector2.zero);
            for (var index = 0; index < points.Count; index++)
            {
                vertexHelper.AddVert(points[index], color32, Vector2.zero);
            }

            for (var index = 1; index <= points.Count; index++)
            {
                var next = index == points.Count ? 1 : index + 1;
                vertexHelper.AddTriangle(start, start + index, start + next);
            }
        }

        private static void AddOutline(VertexHelper vertexHelper, IReadOnlyList<Vector2> points, Color outlineColor)
        {
            for (var index = 0; index < points.Count; index++)
            {
                AddStroke(vertexHelper, points[index], points[(index + 1) % points.Count], 2.2f, outlineColor);
            }
        }

        private static void AddZigzag(VertexHelper vertexHelper, Rect rect, Color strokeColor, Vector2 offset = default)
        {
            var points = new[]
            {
                new Vector2(rect.xMin + rect.width * 0.53f, rect.yMin + rect.height * 0.82f),
                new Vector2(rect.xMin + rect.width * 0.43f, rect.yMin + rect.height * 0.66f),
                new Vector2(rect.xMin + rect.width * 0.56f, rect.yMin + rect.height * 0.51f),
                new Vector2(rect.xMin + rect.width * 0.45f, rect.yMin + rect.height * 0.34f),
                new Vector2(rect.xMin + rect.width * 0.54f, rect.yMin + rect.height * 0.16f)
            };
            for (var index = 0; index < points.Length - 1; index++)
            {
                AddStroke(vertexHelper, points[index] + offset, points[index + 1] + offset, 3.1f, strokeColor);
            }
        }

        private static void AddStroke(VertexHelper vertexHelper, Vector2 start, Vector2 end, float thickness, Color strokeColor)
        {
            var direction = end - start;
            if (direction.sqrMagnitude < 0.001f)
            {
                return;
            }

            var normal = new Vector2(-direction.y, direction.x).normalized * (thickness * 0.5f);
            var color32 = (Color32)strokeColor;
            var index = vertexHelper.currentVertCount;
            vertexHelper.AddVert(start - normal, color32, Vector2.zero);
            vertexHelper.AddVert(start + normal, color32, Vector2.zero);
            vertexHelper.AddVert(end + normal, color32, Vector2.zero);
            vertexHelper.AddVert(end - normal, color32, Vector2.zero);
            vertexHelper.AddTriangle(index, index + 1, index + 2);
            vertexHelper.AddTriangle(index + 2, index + 3, index);
        }

        private static Color WithAlpha(Color source, float alpha)
        {
            source.a *= alpha;
            return source;
        }
    }

    public sealed class QuestCheckMarkGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            var rect = GetPixelAdjustedRect();
            var a = new Vector2(rect.xMin + rect.width * 0.17f, rect.yMin + rect.height * 0.48f);
            var b = new Vector2(rect.xMin + rect.width * 0.39f, rect.yMin + rect.height * 0.24f);
            var c = new Vector2(rect.xMin + rect.width * 0.84f, rect.yMin + rect.height * 0.76f);
            AddPencilStroke(vertexHelper, a, b, 4.2f, color, Vector2.zero);
            AddPencilStroke(vertexHelper, b, c, 4.2f, color, Vector2.zero);
            AddPencilStroke(vertexHelper, a, b, 1.6f, WithAlpha(color, 0.38f), new Vector2(0.8f, 1.1f));
            AddPencilStroke(vertexHelper, b, c, 1.6f, WithAlpha(color, 0.38f), new Vector2(0.8f, 1.1f));
            AddPencilStroke(vertexHelper, a, b, 1.2f, WithAlpha(color, 0.30f), new Vector2(-0.9f, -0.7f));
            AddPencilStroke(vertexHelper, b, c, 1.2f, WithAlpha(color, 0.30f), new Vector2(-0.9f, -0.7f));
        }

        private static void AddPencilStroke(VertexHelper vertexHelper, Vector2 start, Vector2 end, float thickness, Color strokeColor, Vector2 offset)
        {
            var direction = end - start;
            if (direction.sqrMagnitude < 0.001f)
            {
                return;
            }

            var normal = new Vector2(-direction.y, direction.x).normalized * (thickness * 0.5f);
            start += offset;
            end += offset;
            var color32 = (Color32)strokeColor;
            var index = vertexHelper.currentVertCount;
            vertexHelper.AddVert(start - normal, color32, Vector2.zero);
            vertexHelper.AddVert(start + normal, color32, Vector2.zero);
            vertexHelper.AddVert(end + normal, color32, Vector2.zero);
            vertexHelper.AddVert(end - normal, color32, Vector2.zero);
            vertexHelper.AddTriangle(index, index + 1, index + 2);
            vertexHelper.AddTriangle(index + 2, index + 3, index);
        }

        private static Color WithAlpha(Color source, float alpha)
        {
            source.a *= alpha;
            return source;
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

    public enum MagicNoteCategory
    {
        Dialogue,
        FloorNote,
        Discovery
    }

    public sealed class MagicNoteEntry
    {
        public MagicNoteCategory category;
        public int floorNumber;
        public string text = "";
        public float timestamp;

        public string DisplayLine => $"[{CategoryLabel(category)}] {floorNumber}층 - {text.Replace("\n", " / ")}";

        private static string CategoryLabel(MagicNoteCategory category)
        {
            return category switch
            {
                MagicNoteCategory.Dialogue => "대사",
                MagicNoteCategory.FloorNote => "층노트",
                _ => "발견"
            };
        }
    }

    public sealed class MagicNote
    {
        private readonly List<MagicNoteEntry> entries = new();
        private float ttl;
        public string Text { get; private set; } = "";
        public bool Visible => ttl > 0f && !string.IsNullOrWhiteSpace(Text);
        public IReadOnlyList<MagicNoteEntry> Entries => entries;
        public IReadOnlyList<string> Lines => entries.Select(entry => entry.DisplayLine).ToList();

        public void Show(string text, MagicNoteCategory category, int floorNumber)
        {
            Text = text;
            ttl = 4.4f;
            if (!string.IsNullOrWhiteSpace(text))
            {
                entries.Add(new MagicNoteEntry
                {
                    category = category,
                    floorNumber = Mathf.Max(1, floorNumber),
                    text = text,
                    timestamp = Time.time
                });
            }
        }

        public void Restore(IEnumerable<string> lines, int floorNumber)
        {
            Clear();
            foreach (var line in lines ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                entries.Add(new MagicNoteEntry
                {
                    category = MagicNoteCategory.FloorNote,
                    floorNumber = Mathf.Max(1, floorNumber),
                    text = line,
                    timestamp = Time.time
                });
            }

            Text = entries.Count == 0 ? "" : entries[^1].text;
            ttl = entries.Count == 0 ? 0f : 4.4f;
        }

        public void Clear()
        {
            entries.Clear();
            Text = "";
            ttl = 0f;
        }

        public string[] DiscoveryExcerpts(int count)
        {
            return entries
                .Where(entry => entry.category == MagicNoteCategory.Discovery)
                .Select(entry => entry.text.Replace("\n", " / "))
                .Distinct()
                .Reverse()
                .Take(Mathf.Max(0, count))
                .Reverse()
                .ToArray();
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
        private string questChecklistSummary = "아직 저장된 퀘스트 점수가 없습니다.";
        private int questChecklistCompleted;
        private int questChecklistTotal;
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

        public void RecordQuestChecklist(string summary, int completed, int total)
        {
            questChecklistSummary = string.IsNullOrWhiteSpace(summary) ? "아직 저장된 퀘스트 점수가 없습니다." : summary;
            questChecklistCompleted = Math.Max(0, completed);
            questChecklistTotal = Math.Max(0, total);
        }

        public string BuildText(int totalAttempts, string outputDirectory, bool trueEnding, int completedFinalGoals, int totalFinalGoals, IReadOnlyList<string> noteExcerpts)
        {
            var favoriteBase = baseUse.Count == 0 ? "없음" : SpellLabels.Korean(baseUse.OrderByDescending(item => item.Value).First().Key);
            var favoriteOverlay = overlayUse.Count == 0 ? "없음" : SpellLabels.Korean(overlayUse.OrderByDescending(item => item.Value).First().Key);
            var averageQuality = qualityScores.Count == 0 ? 0f : qualityScores.Average() * 100f;
            var endingName = trueEnding ? "진엔딩 (6/6 완전 복구)" : $"통과 엔딩 ({completedFinalGoals}/{totalFinalGoals})";
            var header = trueEnding ? "입학 시험 완전 통과 - 성좌심 완전 복구 보고서" : "입학 시험 통과 - 성좌심 복구 보고서";
            var excerptLine = noteExcerpts == null || noteExcerpts.Count == 0
                ? "대표 관찰문: 기록 없음"
                : "대표 관찰문:\n- " + string.Join("\n- ", noteExcerpts);
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
                $"퀘스트 체크: {questChecklistCompleted}/{questChecklistTotal}\n" +
                $"{questChecklistSummary}\n" +
                $"{BuildProfileSummary()}\n" +
                $"{excerptLine}\n" +
                "보정 정책: profile은 성공/실패 판정을 뒤집지 않고 품질 설명과 다음 연습 방향에만 사용됩니다.\n\n" +
                BuildReflectionLine(favoriteBase, favoriteOverlay, discoveries.Count) + "\n" +
                (trueEnding ? "이제 탑의 별자리에 당신의 이름이 있습니다." : "탑은 당신의 문양을 기억 속에 새겼습니다.") + "\n\n" +
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
                        WorldStateGoal.CustomBase("custom_fire", "불꽃 직선", SpellFamily.Fire, new Vector2(-5.4f, 2.55f), new Color(1f, 0.31f, 0.18f), "불꽃 직선이 직선 불꽃 반응으로 새겨집니다.").WithRequirementShapes("line"),
                        WorldStateGoal.CustomBase("custom_water", "물 보호막", SpellFamily.Water, new Vector2(-2.7f, 3.0f), new Color(0.24f, 0.48f, 0.86f), "물 보호막이 방어막 반응으로 안정화됩니다.").WithRequirementShapes("ellipse"),
                        WorldStateGoal.CustomBase("custom_wind", "바람 화살표", SpellFamily.Wind, new Vector2(0f, 3.05f), new Color(0.74f, 0.86f, 0.92f), "바람 화살표가 끝점 방향 사출 반응을 깨웁니다.").WithRequirementShapes("arrow"),
                        WorldStateGoal.CustomBase("custom_earth", "사각 방벽", SpellFamily.Earth, new Vector2(2.7f, 3.0f), new Color(0.74f, 0.55f, 0.32f), "사각 방벽이 구조물 생성 반응을 고정합니다.").WithRequirementShapes("rect"),
                        WorldStateGoal.CustomBase("custom_life", "생명 연결선", SpellFamily.Life, new Vector2(5.4f, 2.55f), new Color(0.35f, 0.86f, 0.42f), "생명 연결선이 지속 버프 반응으로 이어집니다.").WithRequirementShapes("brace")
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
                        WorldStateGoal.CustomSpell("frozen_river", "강물 얼리기", SpellFamily.Water, CustomSpellEffectKind.Ice, new Vector2(-1.75f, -1.18f), new Color(0.48f, 0.84f, 1f), "얼음 결정이 강물을 얼려 안전한 얼음길로 만듭니다.").WithRequirementShapes("hexagon").WithReaction(WorldReactionKind.FreezeRiver),
                        WorldStateGoal.CustomSpell("earth_stairs", "구멍 메우기", SpellFamily.Earth, CustomSpellEffectKind.Stability, new Vector2(1.75f, 0.42f), new Color(0.74f, 0.55f, 0.32f), "구멍 메움판이 깨진 바닥 구멍을 채워 지나갈 길을 만듭니다.").WithRequirementShapes("rect").WithReaction(WorldReactionKind.EarthStairs),
                        WorldStateGoal.CustomSpell("living_bridge", "덩굴 다리", SpellFamily.Life, CustomSpellEffectKind.LivingBridge, new Vector2(2.85f, -3.02f), new Color(0.35f, 0.86f, 0.42f), "덩굴 다리가 낭떠러지를 이어 줍니다.").WithRequirementShapes("arrow", "rect").WithReaction(WorldReactionKind.LivingBridge),
                        WorldStateGoal.CustomSpell("wind_platform", "바람 발판", SpellFamily.Wind, CustomSpellEffectKind.WindPlatform, new Vector2(4.65f, 2.15f), new Color(0.74f, 0.86f, 0.92f), "바람 발판이 마지막 빈 공간을 건널 수 있게 합니다.").WithRequirementShapes("rect").WithReaction(WorldReactionKind.WindPlatform)
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
                        WorldStateGoal.CustomSpell("ice_training", "얼음 제압", SpellFamily.Water, CustomSpellEffectKind.Ice, new Vector2(-4.8f, 1.55f), new Color(0.48f, 0.84f, 1f), "얼음 결정이 표적의 움직임을 늦춥니다.").WithReaction(WorldReactionKind.CombatHit),
                        WorldStateGoal.CustomSpell("electric_training", "번개 타격", SpellFamily.Fire, CustomSpellEffectKind.Electric, new Vector2(-1.6f, 2.15f), new Color(1f, 0.9f, 0.22f), "번개 직선이 표적을 빠르게 때립니다.").WithReaction(WorldReactionKind.CombatHit),
                        WorldStateGoal.CustomSpell("cleanse_training", "정화 수막", SpellFamily.Water, CustomSpellEffectKind.Cleanse, new Vector2(1.6f, 2.15f), new Color(0.24f, 0.48f, 0.86f), "둥근 수막이 표적의 오염 효과를 씻어 냅니다.").WithReaction(WorldReactionKind.CombatHit),
                        WorldStateGoal.CustomSpell("stable_training", "사각 방벽", SpellFamily.Earth, CustomSpellEffectKind.Stability, new Vector2(4.8f, 1.55f), new Color(0.74f, 0.55f, 0.32f), "사각 방벽이 표적 앞에 엄폐물을 세웁니다.").WithReaction(WorldReactionKind.CombatHit)
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
        public IReadOnlyList<string> requirementShapeTokens = Array.Empty<string>();
        public string discoveryNote;
        public WorldReactionKind reactionKind;
        public bool requiresCustomShape;
        public bool completed;
        public float radius = 2.15f;
        public float visualScale = 1f;
        public GameObject body;
        public GameObject entityBody;
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

        public WorldStateGoal WithRequirementShapes(params string[] tokens)
        {
            requirementShapeTokens = (tokens ?? Array.Empty<string>())
                .Where(token => !string.IsNullOrWhiteSpace(token))
                .Select(token => token.Trim())
                .ToArray();
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
                requirementShapeTokens = requirementShapeTokens?.ToArray() ?? Array.Empty<string>(),
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
