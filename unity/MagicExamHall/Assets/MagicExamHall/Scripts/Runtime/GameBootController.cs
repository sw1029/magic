using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MagicExamHall
{
    public enum GameBootState
    {
        Title,
        MainMenu,
        Options,
        Gameplay,
        Paused,
        Codex,
        Ending
    }

    [Serializable]
    public sealed class GameProgressSnapshot
    {
        public int floorNumber = 1;
        public int completedGoals;
        public int totalGoals;
        public string[] noteLines = Array.Empty<string>();
        public string savedAtUtc = "";
        public int slotIndex;
        public int discoveries;
        public string endingLabel = "";
    }

    public static class MagicExamSettings
    {
        private const string BgmVolumeKey = "MagicExamHall.BgmVolume";
        private const string SfxVolumeKey = "MagicExamHall.SfxVolume";
        private const string MouseSensitivityKey = "MagicExamHall.MouseSensitivity";
        private const string SwapMouseButtonsKey = "MagicExamHall.SwapMouseButtons";
        private const string MovementPresetKey = "MagicExamHall.MovementPreset";
        private const string TextScaleKey = "MagicExamHall.TextScale";
        private const string ColorAssistKey = "MagicExamHall.ColorAssist";
        private const string ObserverModeKey = "MagicExamHall.ObserverMode";
        private static bool loaded;
        private static float bgmVolume = 0.72f;
        private static float sfxVolume = 0.88f;
        private static float mouseSensitivity = 1f;
        private static bool swapMouseButtons;
        private static int movementPreset;
        private static float textScale = 1f;
        private static bool colorAssist;
        private static bool observerMode;

        public static float BgmVolume
        {
            get
            {
                EnsureLoaded();
                return bgmVolume;
            }
            set
            {
                EnsureLoaded();
                bgmVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(BgmVolumeKey, bgmVolume);
            }
        }

        public static float SfxVolume
        {
            get
            {
                EnsureLoaded();
                return sfxVolume;
            }
            set
            {
                EnsureLoaded();
                sfxVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
            }
        }

        public static float MouseSensitivity
        {
            get
            {
                EnsureLoaded();
                return mouseSensitivity;
            }
            set
            {
                EnsureLoaded();
                mouseSensitivity = Mathf.Clamp(value, 0.55f, 1.75f);
                PlayerPrefs.SetFloat(MouseSensitivityKey, mouseSensitivity);
            }
        }

        public static bool SwapMouseButtons
        {
            get
            {
                EnsureLoaded();
                return swapMouseButtons;
            }
            set
            {
                EnsureLoaded();
                swapMouseButtons = value;
                PlayerPrefs.SetInt(SwapMouseButtonsKey, swapMouseButtons ? 1 : 0);
            }
        }

        public static int DrawMouseButton => SwapMouseButtons ? 0 : 1;

        public static int MovementPreset
        {
            get
            {
                EnsureLoaded();
                return movementPreset;
            }
            set
            {
                EnsureLoaded();
                movementPreset = Mathf.Clamp(value, 0, 2);
                PlayerPrefs.SetInt(MovementPresetKey, movementPreset);
            }
        }

        public static float TextScale
        {
            get
            {
                EnsureLoaded();
                return textScale;
            }
            set
            {
                EnsureLoaded();
                textScale = Mathf.Clamp(value, 1f, 1.5f);
                PlayerPrefs.SetFloat(TextScaleKey, textScale);
            }
        }

        public static bool ColorAssist
        {
            get
            {
                EnsureLoaded();
                return colorAssist;
            }
            set
            {
                EnsureLoaded();
                colorAssist = value;
                PlayerPrefs.SetInt(ColorAssistKey, colorAssist ? 1 : 0);
            }
        }

        public static bool ObserverMode
        {
            get
            {
                EnsureLoaded();
                return observerMode;
            }
            set
            {
                EnsureLoaded();
                observerMode = value;
                PlayerPrefs.SetInt(ObserverModeKey, observerMode ? 1 : 0);
            }
        }

        public static Vector2 ReadMovementAxis()
        {
            EnsureLoaded();
            var keys = movementPreset switch
            {
                1 => new[] { KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow },
                2 => new[] { KeyCode.I, KeyCode.K, KeyCode.J, KeyCode.L },
                _ => new[] { KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D }
            };
            var horizontal = (Input.GetKey(keys[3]) ? 1f : 0f) - (Input.GetKey(keys[2]) ? 1f : 0f);
            var vertical = (Input.GetKey(keys[0]) ? 1f : 0f) - (Input.GetKey(keys[1]) ? 1f : 0f);
            var axisInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            var keyInput = new Vector2(horizontal, vertical);
            return keyInput.sqrMagnitude > 0.01f ? keyInput : axisInput;
        }

        public static string MovementPresetLabel => MovementPreset switch
        {
            1 => "방향키",
            2 => "IJKL",
            _ => "WASD"
        };

        public static void Save()
        {
            PlayerPrefs.Save();
        }

        private static void EnsureLoaded()
        {
            if (loaded)
            {
                return;
            }

            bgmVolume = PlayerPrefs.GetFloat(BgmVolumeKey, bgmVolume);
            sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, sfxVolume);
            mouseSensitivity = PlayerPrefs.GetFloat(MouseSensitivityKey, mouseSensitivity);
            swapMouseButtons = PlayerPrefs.GetInt(SwapMouseButtonsKey, swapMouseButtons ? 1 : 0) == 1;
            movementPreset = PlayerPrefs.GetInt(MovementPresetKey, movementPreset);
            textScale = PlayerPrefs.GetFloat(TextScaleKey, textScale);
            colorAssist = PlayerPrefs.GetInt(ColorAssistKey, colorAssist ? 1 : 0) == 1;
            observerMode = PlayerPrefs.GetInt(ObserverModeKey, observerMode ? 1 : 0) == 1;
            loaded = true;
        }
    }

    public sealed class GameBootController : MonoBehaviour
    {
        private const int SaveSlotCount = 3;
        private const string LegacySaveFileName = "save.json";
        private const string SaveFilePrefix = "save-slot-";

        private ExamGameController controller = null!;
        private Canvas canvas = null!;
        private Font uiFont = null!;
        private RectTransform overlayRoot = null!;
        private Image overlayRootImage = null!;
        private RectTransform titlePanel = null!;
        private RectTransform menuPanel = null!;
        private RectTransform optionsPanel = null!;
        private RectTransform pausePanel = null!;
        private RectTransform codexPanel = null!;
        private CanvasGroup codexPanelGroup = null!;
        private RectTransform codexTextContent = null!;
        private ScrollRect codexScrollRect = null!;
        private RectTransform endingPromptPanel = null!;
        private Image fadeCurtain = null!;
        private Image codexQuickImage = null!;
        private Button newGameButton = null!;
        private Button continueButton = null!;
        private Button practiceButton = null!;
        private Button codexQuickButton = null!;
        private Button optionsBackButton = null!;
        private Button resumeButton = null!;
        private Button codexCloseButton = null!;
        private Button[] slotButtons = Array.Empty<Button>();
        private Text saveSummaryText = null!;
        private Text codexText = null!;
        private Text volumeSummaryText = null!;
        private Text accessibilitySummaryText = null!;
        private Slider bgmSlider = null!;
        private Slider sfxSlider = null!;
        private Slider mouseSensitivitySlider = null!;
        private readonly Dictionary<Text, int> baseFontSizes = new();
        private GameBootState optionsReturnState = GameBootState.MainMenu;
        private MagicNoteCategory codexTab = MagicNoteCategory.FloorNote;
        private int activeSaveSlotIndex;
        private Coroutine transitionRoutine = null!;
        private int observedNoteCount;
        private float codexPulseUntil;
        private bool initialized;

        public GameBootState StateForTests { get; private set; } = GameBootState.Title;
        public string SavePath => SavePathForSlot(activeSaveSlotIndex);
        public int ActiveSaveSlotForTests => activeSaveSlotIndex + 1;
        public bool HasSaveForTests => File.Exists(SavePath);
        public string CodexTextForTests => codexText == null ? "" : codexText.text;
        public bool CodexQuickButtonVisibleForTests => codexQuickButton != null && codexQuickButton.gameObject.activeSelf;
        public Vector2 CodexQuickButtonPositionForTests => codexQuickButton == null ? Vector2.zero : ((RectTransform)codexQuickButton.transform).anchoredPosition;
        public Vector2 CodexQuickButtonSizeForTests => codexQuickButton == null ? Vector2.zero : ((RectTransform)codexQuickButton.transform).sizeDelta;
        public bool CodexPanelVisibleForTests => codexPanelGroup != null && codexPanelGroup.alpha > 0.5f && codexPanelGroup.blocksRaycasts;
        public string CodexPanelParentForTests => codexPanel != null && codexPanel.parent != null ? codexPanel.parent.name : "";
        public Vector2 CodexPanelPositionForTests => codexPanel == null ? Vector2.zero : codexPanel.anchoredPosition;
        public bool CodexBackdropBlocksRaycastsForTests => overlayRootImage != null && overlayRootImage.raycastTarget;
        public bool CodexPanelDrawsAboveBackdropForTests => codexPanel != null && overlayRoot != null && codexPanel.IsChildOf(overlayRoot);

        public void Initialize(ExamGameController gameController, Canvas targetCanvas, Font font)
        {
            if (initialized)
            {
                return;
            }

            controller = gameController;
            canvas = targetCanvas;
            uiFont = font;
            BuildUi();
            RegisterTextSizes(canvas.transform);
            ApplyGlobalTextScale();
            controller.ProgressCheckpointed += SaveProgress;
            initialized = true;
            ShowTitle();
        }

        private void OnDestroy()
        {
            if (controller != null)
            {
                controller.ProgressCheckpointed -= SaveProgress;
            }
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            TickCodexQuickButton();
            TickCodexPointerFallback();

            if (StateForTests == GameBootState.Title && Input.anyKeyDown)
            {
                ShowMainMenu();
                return;
            }

            if (StateForTests == GameBootState.Gameplay && controller.HasEndingReport)
            {
                ShowEndingPrompt();
                return;
            }

            if (StateForTests == GameBootState.Gameplay && Input.GetKeyDown(KeyCode.Escape))
            {
                ShowPause();
                return;
            }

            if (StateForTests == GameBootState.Gameplay && Input.GetKeyDown(KeyCode.Tab))
            {
                ShowCodex();
                return;
            }

            if (StateForTests == GameBootState.Paused && Input.GetKeyDown(KeyCode.Escape))
            {
                ResumeGameplay();
                return;
            }

            if (StateForTests == GameBootState.Codex && (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Escape)))
            {
                ResumeGameplay();
                return;
            }

            if (StateForTests == GameBootState.Options && Input.GetKeyDown(KeyCode.Escape))
            {
                ReturnFromOptions();
                return;
            }

            if (StateForTests == GameBootState.Ending && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(0)))
            {
                ShowTitleWithFade();
            }
        }

        public void StartNewGameForTests()
        {
            StartNewGameImmediate();
        }

        public void ContinueGameForTests()
        {
            ContinueGameImmediate();
        }

        public void SelectSaveSlotForTests(int slotNumber)
        {
            SelectSaveSlot(slotNumber - 1);
        }

        public string SavePathForSlotForTests(int slotNumber)
        {
            return SavePathForSlot(Mathf.Clamp(slotNumber - 1, 0, SaveSlotCount - 1));
        }

        public void ManualSaveForTests()
        {
            ManualSaveFromCodex();
        }

        public void ShowCodexForTests()
        {
            codexTab = MagicNoteCategory.FloorNote;
            ShowCodex();
        }

        public void ShowDiscoveryCodexForTests()
        {
            codexTab = MagicNoteCategory.Discovery;
            ShowCodex();
        }

        public void ShowPauseForTests()
        {
            ShowPause();
        }

        public void ResumeGameplayForTests()
        {
            ResumeGameplay();
        }

        public void ShowMainMenuForTests()
        {
            ShowMainMenu();
        }

        private void BuildUi()
        {
            overlayRoot = CreatePanel("Boot Overlay", canvas.transform, Vector2.zero, new Vector2(1280, 720), Anchor.Stretch, MagicExamUiTheme.DimOverlay);
            overlayRootImage = overlayRoot.GetComponent<Image>();
            var overlayCanvas = overlayRoot.gameObject.AddComponent<Canvas>();
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = 200;
            overlayRoot.gameObject.AddComponent<GraphicRaycaster>();
            overlayRoot.gameObject.SetActive(true);

            titlePanel = CreatePanel("Title Screen", overlayRoot, Vector2.zero, new Vector2(1280, 720), Anchor.Stretch, MagicExamUiTheme.DeepTowerSolid);
            CreateTowerSilhouette(titlePanel);
            var titleLogoArt = CreateImage("Title Logo Art", titlePanel, new Vector2(-180f, 166f), new Vector2(300f, 112f), Anchor.Center, Color.white);
            MagicExamUiFactory.ApplySprite(titleLogoArt, MagicExamUiSpriteId.TitleLogo, sliced: false);
            titleLogoArt.raycastTarget = false;
            var titleText = CreateText("Title", titlePanel, "MAGIC EXAM HALL", 48, FontStyle.Bold, new Vector2(-180f, 88f), new Vector2(650, 72), Anchor.Center, TextAnchor.MiddleCenter, MagicExamUiTheme.Gold);
            MagicExamUiFactory.StyleDarkText(titleText, emphasized: true);
            var subtitle = CreateText("Subtitle", titlePanel, "마법탑 입학 복구 시험", 20, FontStyle.Bold, new Vector2(-180f, 28f), new Vector2(520, 36), Anchor.Center, TextAnchor.MiddleCenter, MagicExamUiTheme.RuneBlue);
            MagicExamUiFactory.StyleDarkText(subtitle);
            var flavor = CreateText("Title Flavor", titlePanel, "떠 있는 탑은 오늘도 입학생의 문양을 기다린다.", 15, FontStyle.Italic, new Vector2(-180f, -122f), new Vector2(620, 30), Anchor.Center, TextAnchor.MiddleCenter, MagicExamUiTheme.TextOnDarkMuted);
            MagicExamUiFactory.StyleDarkText(flavor);
            var titlePrompt = MagicExamUiFactory.CreateFramedPanel(
                "Title Prompt Scroll",
                titlePanel,
                new Vector2(-180f, -194f),
                new Vector2(390f, 52f),
                MagicExamUiAnchor.Center,
                MagicExamUiSpriteId.ScrollPanel,
                Color.white,
                MagicExamUiTheme.BorderBrown,
                2f);
            var promptText = CreateText("Any Key", titlePrompt, "아무 키나 눌러 입장", 17, FontStyle.Bold, Vector2.zero, new Vector2(350, 34), Anchor.Center, TextAnchor.MiddleCenter, MagicExamUiTheme.ParchmentInk);
            MagicExamUiFactory.StyleParchmentText(promptText, emphasized: true);

            menuPanel = CreatePanel("Main Menu", overlayRoot, Vector2.zero, new Vector2(1280, 720), Anchor.Stretch, MagicExamUiTheme.DeepTowerSolid);
            CreateTowerSilhouette(menuPanel);
            var menuFrame = MagicExamUiFactory.CreateFramedPanel("Menu Commands", menuPanel, new Vector2(-388f, 0f), new Vector2(340f, 540f), MagicExamUiAnchor.Center, MagicExamUiSpriteId.DarkPanel, Color.white, MagicExamUiTheme.BorderGold, 2.4f);
            MagicExamUiFactory.AddAccentRail(menuFrame, MagicExamUiTheme.Gold, 6f);
            var slotFrame = MagicExamUiFactory.CreateFramedPanel("Save Ledger", menuPanel, new Vector2(180f, 0f), new Vector2(650f, 500f), MagicExamUiAnchor.Center, MagicExamUiSpriteId.BookPanel, Color.white, MagicExamUiTheme.BorderBrown, 3f);
            var menuTitle = CreateText("Menu Title", menuFrame, "MAGIC EXAM HALL", 28, FontStyle.Bold, new Vector2(0f, 218f), new Vector2(280, 44), Anchor.Center, TextAnchor.MiddleCenter, MagicExamUiTheme.Gold);
            MagicExamUiFactory.StyleDarkText(menuTitle, emphasized: true);
            var menuSubtitle = CreateText("Menu Subtitle", menuFrame, "입학 복구 기록", 14, FontStyle.Bold, new Vector2(0f, 184f), new Vector2(250, 28), Anchor.Center, TextAnchor.MiddleCenter, MagicExamUiTheme.RuneBlue);
            MagicExamUiFactory.StyleDarkText(menuSubtitle);
            newGameButton = CreateButton("New Game", menuFrame, "새 게임", new Vector2(0f, 112f), StartNewGame, MagicExamButtonStyle.Primary);
            continueButton = CreateButton("Continue", menuFrame, "이어하기", new Vector2(0f, 52f), ContinueGame);
            CreateButton("Options", menuFrame, "옵션", new Vector2(0f, -8f), () => ShowOptions(GameBootState.MainMenu));
            practiceButton = CreateButton("Practice", menuFrame, "연습장", new Vector2(0f, -68f), StartPracticeMode);
            CreateButton("Quit", menuFrame, "종료", new Vector2(0f, -128f), Application.Quit, MagicExamButtonStyle.Danger);
            var saveLabel = CreateText("Save Slot Label", slotFrame, "입학 기록 보관함", 23, FontStyle.Bold, new Vector2(0f, 205f), new Vector2(550, 34), Anchor.Center, TextAnchor.MiddleLeft, MagicExamUiTheme.ParchmentInk);
            MagicExamUiFactory.StyleParchmentText(saveLabel, emphasized: true);
            var saveRule = CreateImage("Save Ledger Rule", slotFrame, new Vector2(0f, 172f), new Vector2(560f, 2f), Anchor.Center, MagicExamUiTheme.BorderBrown);
            saveRule.raycastTarget = false;
            slotButtons = new Button[SaveSlotCount];
            for (var index = 0; index < SaveSlotCount; index++)
            {
                var capturedIndex = index;
                slotButtons[index] = CreateButton($"Save Slot {index + 1}", slotFrame, $"슬롯 {index + 1}", new Vector2(-190f + index * 190f, 118f), () => SelectSaveSlot(capturedIndex), MagicExamButtonStyle.Parchment);
            }
            saveSummaryText = CreateText("Save Summary", slotFrame, "", 16, FontStyle.Normal, new Vector2(0f, -58f), new Vector2(550, 230), Anchor.Center, TextAnchor.UpperLeft, MagicExamUiTheme.ParchmentInk);
            saveSummaryText.verticalOverflow = VerticalWrapMode.Truncate;
            MagicExamUiFactory.StyleParchmentText(saveSummaryText);

            optionsPanel = CreatePanel("Options Panel", overlayRoot, Vector2.zero, new Vector2(1280, 720), Anchor.Stretch, new Color(0f, 0f, 0f, 0f));
            var optionsBook = MagicExamUiFactory.CreateFramedPanel("Options Book", optionsPanel, Vector2.zero, new Vector2(940f, 580f), MagicExamUiAnchor.Center, MagicExamUiSpriteId.BookPanel, Color.white, MagicExamUiTheme.BorderBrown, 3f);
            var optionsTitle = CreateText("Options Title", optionsBook, "시험 환경 설정", 30, FontStyle.Bold, new Vector2(0f, 242f), new Vector2(800, 46), Anchor.Center, TextAnchor.MiddleLeft, MagicExamUiTheme.ParchmentInk);
            MagicExamUiFactory.StyleParchmentText(optionsTitle, emphasized: true);
            bgmSlider = CreateSlider("BGM Slider", optionsBook, "BGM", new Vector2(-10f, 150f), MagicExamSettings.BgmVolume, value => MagicExamSettings.BgmVolume = value);
            sfxSlider = CreateSlider("SFX Slider", optionsBook, "SFX", new Vector2(-10f, 88f), MagicExamSettings.SfxVolume, value => MagicExamSettings.SfxVolume = value);
            mouseSensitivitySlider = CreateSlider("Mouse Sensitivity Slider", optionsBook, "감도", new Vector2(-10f, 26f), NormalizeSensitivity(MagicExamSettings.MouseSensitivity), value =>
            {
                MagicExamSettings.MouseSensitivity = Mathf.Lerp(0.55f, 1.75f, value);
                UpdateOptionSummaries();
            });
            volumeSummaryText = CreateText("Volume Summary", optionsBook, "", 15, FontStyle.Normal, new Vector2(-10f, -22f), new Vector2(650, 30), Anchor.Center, TextAnchor.UpperLeft, MagicExamUiTheme.ParchmentInk);
            MagicExamUiFactory.StyleParchmentText(volumeSummaryText);
            CreateButton("Swap Mouse", optionsBook, "좌/우클릭", new Vector2(-265f, -88f), ToggleSwapMouse, MagicExamButtonStyle.Parchment);
            CreateButton("Movement Preset", optionsBook, "이동 키", new Vector2(0f, -88f), CycleMovementPreset, MagicExamButtonStyle.Parchment);
            CreateButton("Text Scale", optionsBook, "텍스트", new Vector2(265f, -88f), CycleTextScale, MagicExamButtonStyle.Parchment);
            CreateButton("Color Assist", optionsBook, "색 보조", new Vector2(-265f, -150f), ToggleColorAssist, MagicExamButtonStyle.Parchment);
            CreateButton("Observer Mode", optionsBook, "관찰 모드", new Vector2(0f, -150f), ToggleObserverMode, MagicExamButtonStyle.Parchment);
            accessibilitySummaryText = CreateText("Accessibility Summary", optionsBook, "", 14, FontStyle.Normal, new Vector2(265f, -166f), new Vector2(230, 72), Anchor.Center, TextAnchor.UpperLeft, MagicExamUiTheme.ParchmentInk);
            accessibilitySummaryText.verticalOverflow = VerticalWrapMode.Truncate;
            MagicExamUiFactory.StyleParchmentText(accessibilitySummaryText);
            optionsBackButton = CreateButton("Options Back", optionsBook, "돌아가기", new Vector2(-330f, -235f), ReturnFromOptions, MagicExamButtonStyle.Primary);

            pausePanel = CreatePanel("Pause Panel", overlayRoot, Vector2.zero, new Vector2(1280, 720), Anchor.Stretch, new Color(0f, 0f, 0f, 0f));
            var pauseScroll = MagicExamUiFactory.CreateFramedPanel("Pause Scroll", pausePanel, Vector2.zero, new Vector2(430f, 390f), MagicExamUiAnchor.Center, MagicExamUiSpriteId.ScrollPanel, Color.white, MagicExamUiTheme.BorderBrown, 3f);
            var pauseTitle = CreateText("Pause Title", pauseScroll, "시험 일시정지", 28, FontStyle.Bold, new Vector2(0f, 135f), new Vector2(360, 46), Anchor.Center, TextAnchor.MiddleCenter, MagicExamUiTheme.ParchmentInk);
            MagicExamUiFactory.StyleParchmentText(pauseTitle, emphasized: true);
            resumeButton = CreateButton("Resume", pauseScroll, "계속", new Vector2(0f, 55f), ResumeGameplay, MagicExamButtonStyle.Primary);
            CreateButton("Pause Options", pauseScroll, "옵션", new Vector2(0f, -10f), () => ShowOptions(GameBootState.Paused), MagicExamButtonStyle.Parchment);
            CreateButton("Back To Title", pauseScroll, "타이틀로", new Vector2(0f, -75f), ShowTitleWithFade, MagicExamButtonStyle.Danger);

            codexPanel = CreatePanel("Codex Panel", overlayRoot, Vector2.zero, new Vector2(1280, 720), Anchor.Stretch, new Color(0f, 0f, 0f, 0f));
            codexPanelGroup = codexPanel.gameObject.AddComponent<CanvasGroup>();
            var codexBook = MagicExamUiFactory.CreateFramedPanel("Codex Book", codexPanel, Vector2.zero, new Vector2(1060f, 620f), MagicExamUiAnchor.Center, MagicExamUiSpriteId.BookPanel, Color.white, MagicExamUiTheme.BorderBrown, 3f);
            var codexTitle = CreateText("Codex Title", codexBook, "마법 노트", 30, FontStyle.Bold, new Vector2(-250f, 255f), new Vector2(430, 44), Anchor.Center, TextAnchor.MiddleLeft, MagicExamUiTheme.ParchmentInk);
            MagicExamUiFactory.StyleParchmentText(codexTitle, emphasized: true);
            CreateButton("Codex Dialogue Tab", codexBook, "대사", new Vector2(-310f, 210f), () => SetCodexTab(MagicNoteCategory.Dialogue), MagicExamButtonStyle.Tab);
            CreateButton("Codex Floor Tab", codexBook, "층노트", new Vector2(-55f, 210f), () => SetCodexTab(MagicNoteCategory.FloorNote), MagicExamButtonStyle.Tab);
            CreateButton("Codex Discovery Tab", codexBook, "발견", new Vector2(200f, 210f), () => SetCodexTab(MagicNoteCategory.Discovery), MagicExamButtonStyle.Tab);
            var codexViewport = CreatePanel("Codex Viewport", codexBook, new Vector2(0f, -10f), new Vector2(920f, 380f), Anchor.Center, new Color(0f, 0f, 0f, 0f));
            codexViewport.gameObject.AddComponent<RectMask2D>();
            codexTextContent = CreatePanel("Codex Text Content", codexViewport, Vector2.zero, new Vector2(920f, 380f), Anchor.TopLeft, new Color(0f, 0f, 0f, 0f));
            codexTextContent.GetComponent<Image>().raycastTarget = false;
            codexText = CreateText("Codex Text", codexTextContent, "", 15, FontStyle.Normal, new Vector2(18f, -10f), new Vector2(884f, 360f), Anchor.TopLeft, TextAnchor.UpperLeft, MagicExamUiTheme.ParchmentInk);
            codexText.verticalOverflow = VerticalWrapMode.Overflow;
            MagicExamUiFactory.StyleParchmentText(codexText);
            codexScrollRect = codexViewport.gameObject.AddComponent<ScrollRect>();
            codexScrollRect.viewport = codexViewport;
            codexScrollRect.content = codexTextContent;
            codexScrollRect.horizontal = false;
            codexScrollRect.vertical = true;
            codexScrollRect.movementType = ScrollRect.MovementType.Clamped;
            codexScrollRect.scrollSensitivity = 28f;
            CreateButton("Codex Manual Save", codexBook, "수동 저장", new Vector2(150f, -260f), ManualSaveFromCodex, MagicExamButtonStyle.Parchment);
            codexCloseButton = CreateButton("Codex Close", codexBook, "닫기", new Vector2(400f, -260f), ResumeGameplay, MagicExamButtonStyle.Primary);

            endingPromptPanel = MagicExamUiFactory.CreateFramedPanel("Ending Prompt", overlayRoot, new Vector2(0f, -334f), new Vector2(520, 44), MagicExamUiAnchor.Center, MagicExamUiSpriteId.ScrollPanel, Color.white, MagicExamUiTheme.BorderBrown, 2f);
            var endingPromptText = CreateText("Ending Prompt Text", endingPromptPanel, "Enter 또는 클릭으로 타이틀 복귀", 14, FontStyle.Bold, Vector2.zero, new Vector2(480, 30), Anchor.Center, TextAnchor.MiddleCenter, MagicExamUiTheme.ParchmentInk);
            MagicExamUiFactory.StyleParchmentText(endingPromptText, emphasized: true);
            codexQuickButton = CreateQuickCodexButton(canvas.transform);
            codexQuickButton.gameObject.SetActive(false);
            fadeCurtain = CreateImage("Boot Fade Curtain", canvas.transform, Vector2.zero, new Vector2(1280, 720), Anchor.Stretch, new Color(0f, 0f, 0f, 0f));
            fadeCurtain.raycastTarget = true;
            fadeCurtain.gameObject.SetActive(false);

            HideAllPanels();
        }

        private void ShowTitle()
        {
            Time.timeScale = 1f;
            controller.PrepareForTitleScreen();
            StateForTests = GameBootState.Title;
            overlayRoot.gameObject.SetActive(true);
            SetOverlayBackdrop(true);
            HideAllPanels();
            titlePanel.gameObject.SetActive(true);
            SetQuickCodexVisible(false);
        }

        private void ShowMainMenu()
        {
            Time.timeScale = 1f;
            controller.SetGameplayInputEnabled(false);
            StateForTests = GameBootState.MainMenu;
            overlayRoot.gameObject.SetActive(true);
            SetOverlayBackdrop(true);
            HideAllPanels();
            RefreshSaveSummary();
            menuPanel.gameObject.SetActive(true);
            SetQuickCodexVisible(false);
            SelectButton(newGameButton);
        }

        private void StartNewGame()
        {
            BeginTransition(StartNewGameRoutine());
        }

        private void StartNewGameImmediate()
        {
            Time.timeScale = 1f;
            controller.StartNewGame();
            SaveProgress(controller.CreateProgressSnapshot(), activeSaveSlotIndex);
            EnterGameplay();
        }

        private void ContinueGame()
        {
            var snapshot = LoadProgress();
            if (snapshot == null)
            {
                RefreshSaveSummary();
                return;
            }

            BeginTransition(ContinueGameRoutine(snapshot));
        }

        private void ContinueGameImmediate()
        {
            var snapshot = LoadProgress();
            if (snapshot == null)
            {
                RefreshSaveSummary();
                return;
            }

            Time.timeScale = 1f;
            controller.LoadSavedProgress(snapshot.floorNumber, snapshot.noteLines);
            EnterGameplay();
        }

        private void EnterGameplay()
        {
            StateForTests = GameBootState.Gameplay;
            overlayRoot.gameObject.SetActive(false);
            controller.SetGameplayInputEnabled(true);
            observedNoteCount = controller.MagicNoteEntriesForTests.Count;
            SetQuickCodexVisible(true);
        }

        private void ShowPause()
        {
            Time.timeScale = 0f;
            controller.SetGameplayInputEnabled(false);
            StateForTests = GameBootState.Paused;
            overlayRoot.gameObject.SetActive(true);
            SetOverlayBackdrop(true);
            HideAllPanels();
            pausePanel.gameObject.SetActive(true);
            SetQuickCodexVisible(false);
            SelectButton(resumeButton);
        }

        private void ResumeGameplay()
        {
            Time.timeScale = 1f;
            StateForTests = GameBootState.Gameplay;
            SetCodexPanelVisible(false);
            overlayRoot.gameObject.SetActive(false);
            controller.SetGameplayInputEnabled(true);
            SetQuickCodexVisible(true);
        }

        private void ShowOptions(GameBootState returnState)
        {
            optionsReturnState = returnState;
            StateForTests = GameBootState.Options;
            overlayRoot.gameObject.SetActive(true);
            SetOverlayBackdrop(true);
            HideAllPanels();
            bgmSlider.value = MagicExamSettings.BgmVolume;
            sfxSlider.value = MagicExamSettings.SfxVolume;
            mouseSensitivitySlider.value = NormalizeSensitivity(MagicExamSettings.MouseSensitivity);
            UpdateOptionSummaries();
            optionsPanel.gameObject.SetActive(true);
            SetQuickCodexVisible(false);
            SelectButton(optionsBackButton);
        }

        private void ReturnFromOptions()
        {
            MagicExamSettings.Save();
            if (optionsReturnState == GameBootState.Paused)
            {
                ShowPause();
                return;
            }

            ShowMainMenu();
        }

        private void ShowCodex()
        {
            Time.timeScale = 0f;
            controller.SetGameplayInputEnabled(false);
            controller.HideBlockingLetterOverlayForCodex();
            StateForTests = GameBootState.Codex;
            overlayRoot.gameObject.SetActive(true);
            overlayRoot.SetAsLastSibling();
            SetOverlayBackdrop(true);
            HideAllPanels();
            SetCodexPanelVisible(true);
            codexPanel.SetAsLastSibling();
            SetCodexText(BuildCodexText(codexTab));
            SetQuickCodexVisible(false);
            SelectButton(codexCloseButton);
        }

        private void SetCodexTab(MagicNoteCategory category)
        {
            codexTab = category;
            if (StateForTests == GameBootState.Codex && codexText != null)
            {
                SetCodexText(BuildCodexText(codexTab));
            }
        }

        private void ShowEndingPrompt()
        {
            Time.timeScale = 1f;
            controller.SetGameplayInputEnabled(false);
            StateForTests = GameBootState.Ending;
            overlayRoot.gameObject.SetActive(true);
            SetOverlayBackdrop(false);
            HideAllPanels();
            endingPromptPanel.gameObject.SetActive(true);
            SetQuickCodexVisible(false);
        }

        private IEnumerator StartNewGameRoutine()
        {
            yield return FadeTo(1f, 0.6f);
            StartNewGameImmediate();
            yield return FadeTo(0f, 0.8f);
        }

        private IEnumerator ContinueGameRoutine(GameProgressSnapshot snapshot)
        {
            yield return FadeTo(1f, 0.45f);
            Time.timeScale = 1f;
            controller.LoadSavedProgress(snapshot.floorNumber, snapshot.noteLines);
            EnterGameplay();
            yield return FadeTo(0f, 0.65f);
        }

        private void ShowTitleWithFade()
        {
            BeginTransition(ShowTitleRoutine());
        }

        private IEnumerator ShowTitleRoutine()
        {
            yield return FadeTo(1f, 1f);
            ShowTitle();
            yield return FadeTo(0f, 0.25f);
        }

        private void RefreshSaveSummary()
        {
            var snapshot = LoadProgress(activeSaveSlotIndex);
            continueButton.interactable = snapshot != null;
            if (practiceButton != null)
            {
                practiceButton.interactable = AnyEndingReached();
            }
            RefreshSlotButtons();
            var rows = Enumerable.Range(0, SaveSlotCount)
                .Select(index =>
                {
                    var slotSnapshot = LoadProgress(index);
                    var prefix = index == activeSaveSlotIndex ? ">" : " ";
                    if (slotSnapshot == null)
                    {
                        return $"{prefix} 슬롯 {index + 1}: 비어 있음";
                    }

                    var endingSuffix = string.IsNullOrEmpty(slotSnapshot.endingLabel) ? "" : $", {slotSnapshot.endingLabel}";
                    return $"{prefix} 슬롯 {index + 1}: {slotSnapshot.floorNumber}/5층, 목표 {slotSnapshot.completedGoals}/{slotSnapshot.totalGoals}, 발견 {slotSnapshot.discoveries}{endingSuffix}";
                });
            saveSummaryText.text = string.Join("\n", rows) + "\n" + (snapshot == null ? "선택 슬롯에는 저장된 진행이 없습니다." : $"선택 저장: {snapshot.savedAtUtc}");
        }

        private bool AnyEndingReached()
        {
            return Enumerable.Range(0, SaveSlotCount)
                .Select(LoadProgress)
                .Any(slotSnapshot => slotSnapshot != null && !string.IsNullOrEmpty(slotSnapshot.endingLabel));
        }

        private void StartPracticeMode()
        {
            if (!AnyEndingReached())
            {
                return;
            }

            BeginTransition(StartPracticeRoutine());
        }

        private IEnumerator StartPracticeRoutine()
        {
            yield return FadeTo(1f, 0.45f);
            StartPracticeModeImmediate();
            yield return FadeTo(0f, 0.65f);
        }

        private void StartPracticeModeImmediate()
        {
            Time.timeScale = 1f;
            controller.StartPracticeMode();
            EnterGameplay();
        }

        public void StartPracticeModeForTests()
        {
            StartPracticeModeImmediate();
        }

        public bool PracticeUnlockedForTests => AnyEndingReached();

        private string BuildCodexText(MagicNoteCategory category)
        {
            var entries = controller.MagicNoteEntriesForTests;
            var filtered = entries.Where(entry => entry.category == category).ToList();
            if (category == MagicNoteCategory.Discovery)
            {
                return BuildDiscoveryCodex(filtered);
            }

            if (filtered.Count == 0)
            {
                return category == MagicNoteCategory.Dialogue ? "아직 기록된 대사가 없습니다." : "아직 기록된 층 노트가 없습니다.";
            }

            var recent = filtered.AsEnumerable().Reverse().Take(14).Reverse().Select(entry => entry.DisplayLine);
            return string.Join("\n", recent);
        }

        private string BuildDiscoveryCodex(IReadOnlyList<MagicNoteEntry> filtered)
        {
            var familyRows = Enum.GetValues(typeof(SpellFamily))
                .Cast<SpellFamily>()
                .Select(family => controller.DiscoveredFamiliesForTests.Contains(family) ? SpellLabels.Korean(family) : "???");
            var overlayRows = Enum.GetValues(typeof(OverlayOperator))
                .Cast<OverlayOperator>()
                .Select(op => controller.DiscoveredOverlaysForTests.Contains(op) ? SpellLabels.Korean(op) : "???");
            var notes = filtered.Count == 0
                ? "아직 발견 기록이 없습니다."
                : string.Join("\n", filtered.Reverse().Take(8).Reverse().Select(entry => entry.DisplayLine));
            return
                "Base family\n" +
                string.Join(" / ", familyRows) + "\n\n" +
                "Overlay operator\n" +
                string.Join(" / ", overlayRows) + "\n\n" +
                $"속성 반응 발견 {controller.DiscoveredReactionCountForTests}/10\n\n" +
                notes;
        }

        private void SaveProgress(GameProgressSnapshot snapshot)
        {
            SaveProgress(snapshot, activeSaveSlotIndex);
        }

        private void SaveProgress(GameProgressSnapshot snapshot, int slotIndex)
        {
            try
            {
                slotIndex = Mathf.Clamp(slotIndex, 0, SaveSlotCount - 1);
                snapshot.slotIndex = slotIndex + 1;
                var path = SavePathForSlot(slotIndex);
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllText(path, JsonUtility.ToJson(snapshot, prettyPrint: true));
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning($"Could not save Magic Exam Hall progress: {exception.Message}");
            }
        }

        private GameProgressSnapshot LoadProgress()
        {
            return LoadProgress(activeSaveSlotIndex);
        }

        private GameProgressSnapshot LoadProgress(int slotIndex)
        {
            try
            {
                slotIndex = Mathf.Clamp(slotIndex, 0, SaveSlotCount - 1);
                var path = SavePathForSlot(slotIndex);
                if (!File.Exists(path) && slotIndex == 0)
                {
                    path = Path.Combine(Application.persistentDataPath, LegacySaveFileName);
                }
                if (!File.Exists(path))
                {
                    return null;
                }

                var snapshot = JsonUtility.FromJson<GameProgressSnapshot>(File.ReadAllText(path));
                if (snapshot == null || snapshot.floorNumber < 1)
                {
                    return null;
                }

                snapshot.floorNumber = Mathf.Clamp(snapshot.floorNumber, 1, 5);
                snapshot.slotIndex = slotIndex + 1;
                if (snapshot.noteLines == null)
                {
                    snapshot.noteLines = Array.Empty<string>();
                }
                return snapshot;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
            {
                Debug.LogWarning($"Could not load Magic Exam Hall progress: {exception.Message}");
                return null;
            }
        }

        private void SelectSaveSlot(int slotIndex)
        {
            activeSaveSlotIndex = Mathf.Clamp(slotIndex, 0, SaveSlotCount - 1);
            RefreshSaveSummary();
            if (slotButtons != null && activeSaveSlotIndex < slotButtons.Length)
            {
                SelectButton(slotButtons[activeSaveSlotIndex]);
            }
        }

        private void ManualSaveFromCodex()
        {
            if (controller.IsPracticeMode)
            {
                SetCodexText(BuildCodexText(codexTab) + "\n\n연습장의 진행은 저장하지 않습니다.");
                return;
            }

            SaveProgress(controller.CreateProgressSnapshot(), activeSaveSlotIndex);
            SetCodexText(BuildCodexText(codexTab) + $"\n\n슬롯 {activeSaveSlotIndex + 1}에 저장했습니다.");
        }

        private string SavePathForSlot(int slotIndex)
        {
            return Path.Combine(Application.persistentDataPath, $"{SaveFilePrefix}{Mathf.Clamp(slotIndex, 0, SaveSlotCount - 1) + 1}.json");
        }

        private void RefreshSlotButtons()
        {
            if (slotButtons == null)
            {
                return;
            }

            for (var index = 0; index < slotButtons.Length; index++)
            {
                var button = slotButtons[index];
                if (button == null)
                {
                    continue;
                }

                var colors = button.colors;
                colors.normalColor = index == activeSaveSlotIndex
                    ? new Color(0.96f, 0.76f, 0.42f, 1f)
                    : new Color(0.84f, 0.63f, 0.36f, 0.98f);
                colors.highlightedColor = index == activeSaveSlotIndex
                    ? new Color(1f, 0.84f, 0.52f, 1f)
                    : new Color(0.94f, 0.72f, 0.42f, 1f);
                button.colors = colors;
            }
        }

        private void BeginTransition(IEnumerator routine)
        {
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
            }
            transitionRoutine = StartCoroutine(routine);
        }

        private IEnumerator FadeTo(float targetAlpha, float durationSeconds)
        {
            if (fadeCurtain == null)
            {
                yield break;
            }

            fadeCurtain.gameObject.SetActive(true);
            var startAlpha = fadeCurtain.color.a;
            var elapsed = 0f;
            while (elapsed < durationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                SetFadeAlpha(Mathf.Lerp(startAlpha, targetAlpha, durationSeconds <= 0f ? 1f : elapsed / durationSeconds));
                yield return null;
            }

            SetFadeAlpha(targetAlpha);
            if (targetAlpha <= 0.001f)
            {
                fadeCurtain.gameObject.SetActive(false);
            }
        }

        private void SetFadeAlpha(float alpha)
        {
            if (fadeCurtain != null)
            {
                fadeCurtain.color = new Color(0f, 0f, 0f, Mathf.Clamp01(alpha));
            }
        }

        private void TickCodexQuickButton()
        {
            if (codexQuickButton == null || controller == null)
            {
                return;
            }

            var noteCount = controller.MagicNoteEntriesForTests.Count;
            if (noteCount > observedNoteCount)
            {
                codexPulseUntil = Time.unscaledTime + 0.4f;
            }
            observedNoteCount = noteCount;

            if (codexQuickImage != null && codexQuickButton.gameObject.activeSelf)
            {
                codexQuickImage.color = Time.unscaledTime < codexPulseUntil
                    ? new Color(1f, 0.88f, 0.58f, 1f)
                    : Color.white;
            }
        }

        private void TickCodexPointerFallback()
        {
            if (StateForTests != GameBootState.Codex || codexCloseButton == null || !Input.GetMouseButtonDown(0))
            {
                return;
            }

            var closeRect = codexCloseButton.transform as RectTransform;
            if (closeRect == null)
            {
                return;
            }

            var eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            if (RectTransformUtility.RectangleContainsScreenPoint(closeRect, Input.mousePosition, eventCamera))
            {
                ResumeGameplay();
            }
        }

        private void SetQuickCodexVisible(bool visible)
        {
            if (codexQuickButton != null)
            {
                codexQuickButton.gameObject.SetActive(visible);
            }
        }

        private static void SelectButton(Button button)
        {
            if (button != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(button.gameObject);
            }
        }

        private void SetOverlayBackdrop(bool dimmed)
        {
            if (overlayRootImage == null)
            {
                return;
            }

            overlayRootImage.color = dimmed
                ? new Color(0.012f, 0.015f, 0.023f, 0.94f)
                : Color.clear;
            overlayRootImage.raycastTarget = dimmed;
        }

        private void SetCodexText(string text)
        {
            if (codexText == null)
            {
                return;
            }

            codexText.text = text;
            UpdateCodexTextLayout();
        }

        private void UpdateCodexTextLayout()
        {
            if (codexText == null || codexTextContent == null || codexScrollRect == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            var viewportWidth = codexScrollRect.viewport == null ? 920f : codexScrollRect.viewport.rect.width;
            var viewportHeight = codexScrollRect.viewport == null ? 380f : codexScrollRect.viewport.rect.height;
            var contentHeight = Mathf.Max(viewportHeight, Mathf.Ceil(codexText.preferredHeight) + 24f);
            codexTextContent.sizeDelta = new Vector2(viewportWidth, contentHeight);
            codexText.rectTransform.sizeDelta = new Vector2(Mathf.Max(120f, viewportWidth - 36f), Mathf.Max(viewportHeight - 20f, contentHeight - 20f));
            Canvas.ForceUpdateCanvases();
            codexScrollRect.verticalNormalizedPosition = 1f;
        }

        private void HideAllPanels()
        {
            titlePanel.gameObject.SetActive(false);
            menuPanel.gameObject.SetActive(false);
            optionsPanel.gameObject.SetActive(false);
            pausePanel.gameObject.SetActive(false);
            SetCodexPanelVisible(false);
            endingPromptPanel.gameObject.SetActive(false);
        }

        private void SetCodexPanelVisible(bool visible)
        {
            if (codexPanel == null || codexPanelGroup == null)
            {
                return;
            }

            codexPanel.gameObject.SetActive(true);
            codexPanelGroup.alpha = visible ? 1f : 0f;
            codexPanelGroup.interactable = visible;
            codexPanelGroup.blocksRaycasts = visible;
        }

        private void UpdateVolumeSummary()
        {
            if (volumeSummaryText != null)
            {
                volumeSummaryText.text = $"BGM {Mathf.RoundToInt(MagicExamSettings.BgmVolume * 100f)}%   SFX {Mathf.RoundToInt(MagicExamSettings.SfxVolume * 100f)}%   감도 {MagicExamSettings.MouseSensitivity:0.00}x";
            }
        }

        private void UpdateAccessibilitySummary()
        {
            if (accessibilitySummaryText == null)
            {
                return;
            }

            accessibilitySummaryText.text =
                $"그리기: {(MagicExamSettings.SwapMouseButtons ? "좌클릭" : "우클릭")}\n" +
                $"이동: {MagicExamSettings.MovementPresetLabel}\n" +
                $"텍스트: {MagicExamSettings.TextScale:0.00}x\n" +
                $"색 보조: {(MagicExamSettings.ColorAssist ? "ON" : "OFF")} / 관찰: {(MagicExamSettings.ObserverMode ? "ON" : "OFF")}";
        }

        private void UpdateOptionSummaries()
        {
            UpdateVolumeSummary();
            UpdateAccessibilitySummary();
        }

        private static float NormalizeSensitivity(float value)
        {
            return Mathf.InverseLerp(0.55f, 1.75f, value);
        }

        private void ToggleSwapMouse()
        {
            MagicExamSettings.SwapMouseButtons = !MagicExamSettings.SwapMouseButtons;
            UpdateOptionSummaries();
        }

        private void CycleMovementPreset()
        {
            MagicExamSettings.MovementPreset = (MagicExamSettings.MovementPreset + 1) % 3;
            UpdateOptionSummaries();
        }

        private void CycleTextScale()
        {
            var next = MagicExamSettings.TextScale < 1.12f ? 1.25f : MagicExamSettings.TextScale < 1.37f ? 1.5f : 1f;
            MagicExamSettings.TextScale = next;
            ApplyGlobalTextScale();
            UpdateOptionSummaries();
        }

        private void ToggleColorAssist()
        {
            MagicExamSettings.ColorAssist = !MagicExamSettings.ColorAssist;
            UpdateOptionSummaries();
        }

        private void ToggleObserverMode()
        {
            MagicExamSettings.ObserverMode = !MagicExamSettings.ObserverMode;
            UpdateOptionSummaries();
        }

        private Button CreateButton(
            string name,
            Transform parent,
            string label,
            Vector2 anchoredPosition,
            UnityEngine.Events.UnityAction action,
            MagicExamButtonStyle style = MagicExamButtonStyle.Secondary)
        {
            var body = CreatePanel(name, parent, anchoredPosition, new Vector2(236, 46), Anchor.Center, Color.white);
            var image = body.GetComponent<Image>();
            var button = body.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            var text = CreateText($"{name} Text", body, label, 18, FontStyle.Bold, Vector2.zero, new Vector2(210, 34), Anchor.Center, TextAnchor.MiddleCenter, Color.white);
            text.verticalOverflow = VerticalWrapMode.Truncate;
            MagicExamUiFactory.StyleButton(button, style);
            return button;
        }

        private Slider CreateSlider(string name, Transform parent, string label, Vector2 anchoredPosition, float value, UnityEngine.Events.UnityAction<float> action)
        {
            var sliderLabel = CreateText($"{name} Label", parent, label, 16, FontStyle.Bold, anchoredPosition + new Vector2(-220f, 0f), new Vector2(90, 28), Anchor.Center, TextAnchor.MiddleLeft, MagicExamUiTheme.ParchmentInk);
            MagicExamUiFactory.StyleParchmentText(sliderLabel, emphasized: true);
            var root = CreatePanel(name, parent, anchoredPosition, new Vector2(390, 28), Anchor.Center, Color.white);
            MagicExamUiFactory.ApplySprite(root.GetComponent<Image>(), MagicExamUiSpriteId.SliderTrack, sliced: true);
            var fillArea = CreatePanel($"{name} Fill Area", root, Vector2.zero, new Vector2(366, 12), Anchor.Center, new Color(0f, 0f, 0f, 0f));
            var fill = CreateImage($"{name} Fill", fillArea, Vector2.zero, new Vector2(366, 12), Anchor.TopLeft, new Color(0.28f, 0.66f, 0.82f, 0.86f));
            var handle = CreateImage($"{name} Handle", root, Vector2.zero, new Vector2(20, 32), Anchor.Center, MagicExamUiTheme.Gold);
            MagicExamUiFactory.ApplySprite(handle, MagicExamUiSpriteId.RuneCursor, sliced: false);
            var slider = root.gameObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = Mathf.Clamp01(value);
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.onValueChanged.AddListener(value =>
            {
                action(value);
                UpdateOptionSummaries();
            });
            return slider;
        }

        private Button CreateQuickCodexButton(Transform parent)
        {
            var body = CreatePanel("Codex Quick Button", parent, new Vector2(-24f, 92f), new Vector2(54, 54), Anchor.BottomRight, Color.white);
            codexQuickImage = body.GetComponent<Image>();
            var button = body.gameObject.AddComponent<Button>();
            button.targetGraphic = codexQuickImage;
            button.onClick.AddListener(ShowCodex);
            MagicExamUiFactory.StyleButton(button, MagicExamButtonStyle.Parchment);
            MagicExamUiFactory.ApplySprite(codexQuickImage, MagicExamUiSpriteId.NoteIcon, sliced: false);
            return button;
        }

        private void CreateTowerSilhouette(Transform parent)
        {
            CreateImage("Tower Sky Band", parent, Vector2.zero, Vector2.zero, Anchor.Stretch, new Color(0.012f, 0.022f, 0.038f, 0.94f)).raycastTarget = false;
            for (var index = 0; index < 18; index++)
            {
                var x = -570f + index * 67f;
                var y = 290f - (index % 5) * 54f;
                CreateImage($"Tower Star {index}", parent, new Vector2(x, y), new Vector2(index % 3 == 0 ? 4f : 2f, index % 3 == 0 ? 4f : 2f), Anchor.Center, new Color(0.55f, 0.84f, 1f, 0.28f + (index % 4) * 0.10f)).raycastTarget = false;
            }

            // Hand-drawn floating mage tower (scripts/gen-title-art.py). Placed on the
            // right, clear of the left-aligned title block. Replaces the old stack of
            // flat rectangles that read as a slot panel rather than a tower.
            var tower = CreateImage("Tower Art", parent, new Vector2(348f, -8f), new Vector2(264f, 528f), Anchor.Center, Color.white);
            var towerSprite = LoadTitleSprite("Sprites/UI/TitleTower");
            if (towerSprite != null)
            {
                tower.sprite = towerSprite;
                tower.type = Image.Type.Simple;
                tower.preserveAspect = true;
            }
            tower.raycastTarget = false;
        }

        private static Sprite LoadTitleSprite(string resourcePath)
        {
            var sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
            {
                return sprite;
            }

            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                return null;
            }

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        private RectTransform CreatePanel(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Anchor anchor, Color color)
        {
            return CreateImage(name, parent, anchoredPosition, size, anchor, color).rectTransform;
        }

        private Image CreateImage(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Anchor anchor, Color color)
        {
            return MagicExamUiFactory.CreateImage(name, parent, anchoredPosition, size, ToUiAnchor(anchor), color);
        }

        private static MagicExamUiAnchor ToUiAnchor(Anchor anchor)
        {
            return anchor switch
            {
                Anchor.Stretch => MagicExamUiAnchor.Stretch,
                Anchor.TopLeft => MagicExamUiAnchor.TopLeft,
                Anchor.TopRight => MagicExamUiAnchor.TopRight,
                Anchor.BottomRight => MagicExamUiAnchor.BottomRight,
                _ => MagicExamUiAnchor.Center
            };
        }

        private Text CreateText(string name, Transform parent, string content, int size, FontStyle style, Vector2 anchoredPosition, Vector2 rectSize, Anchor anchor, TextAnchor alignment, Color color)
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
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            baseFontSizes[text] = size;
            return text;
        }

        private void RegisterTextSizes(Transform root)
        {
            foreach (var text in root.GetComponentsInChildren<Text>(true))
            {
                if (!baseFontSizes.ContainsKey(text))
                {
                    baseFontSizes[text] = text.fontSize;
                }
            }
        }

        private void ApplyGlobalTextScale()
        {
            foreach (var item in baseFontSizes.Where(item => item.Key != null).ToList())
            {
                item.Key.fontSize = Mathf.RoundToInt(item.Value * MagicExamSettings.TextScale);
            }
        }

        private static void ApplyAnchor(RectTransform rect, Anchor anchor)
        {
            switch (anchor)
            {
                case Anchor.Stretch:
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                    break;
                case Anchor.TopLeft:
                    rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0f, 1f);
                    break;
                case Anchor.TopRight:
                    rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
                    rect.pivot = new Vector2(1f, 1f);
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

        private enum Anchor
        {
            Stretch,
            TopLeft,
            TopRight,
            BottomRight,
            Center
        }
    }
}
