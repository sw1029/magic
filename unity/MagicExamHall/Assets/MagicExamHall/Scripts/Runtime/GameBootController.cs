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
        private RectTransform titlePanel = null!;
        private RectTransform menuPanel = null!;
        private RectTransform optionsPanel = null!;
        private RectTransform pausePanel = null!;
        private RectTransform codexPanel = null!;
        private RectTransform endingPromptPanel = null!;
        private Image fadeCurtain = null!;
        private Image codexQuickImage = null!;
        private Button newGameButton = null!;
        private Button continueButton = null!;
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
            overlayRoot = CreatePanel("Boot Overlay", canvas.transform, Vector2.zero, new Vector2(1280, 720), Anchor.Stretch, new Color(0.012f, 0.015f, 0.023f, 0.94f));
            overlayRoot.gameObject.SetActive(true);

            titlePanel = CreatePanel("Title Screen", overlayRoot, Vector2.zero, new Vector2(1280, 720), Anchor.Stretch, new Color(0f, 0f, 0f, 0f));
            CreateText("Title", titlePanel, "Magic Exam Hall", 54, FontStyle.Bold, new Vector2(0f, 92f), new Vector2(920, 72), Anchor.Center, TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.48f));
            CreateText("Subtitle", titlePanel, "Magic Recognizer Playable", 21, FontStyle.Bold, new Vector2(0f, 32f), new Vector2(640, 36), Anchor.Center, TextAnchor.MiddleCenter, new Color(0.72f, 0.88f, 1f));
            CreateTowerSilhouette(titlePanel);
            CreateText("Any Key", titlePanel, "아무 키나 눌러 시작", 18, FontStyle.Bold, new Vector2(0f, -210f), new Vector2(420, 34), Anchor.Center, TextAnchor.MiddleCenter, new Color(0.92f, 0.95f, 1f));

            menuPanel = CreatePanel("Main Menu", overlayRoot, Vector2.zero, new Vector2(1280, 720), Anchor.Stretch, new Color(0f, 0f, 0f, 0f));
            CreateText("Menu Title", menuPanel, "Magic Exam Hall", 42, FontStyle.Bold, new Vector2(-288f, 190f), new Vector2(520, 56), Anchor.Center, TextAnchor.MiddleLeft, new Color(1f, 0.86f, 0.48f));
            newGameButton = CreateButton("New Game", menuPanel, "새 게임", new Vector2(-310f, 92f), StartNewGame);
            continueButton = CreateButton("Continue", menuPanel, "이어하기", new Vector2(-310f, 34f), ContinueGame);
            CreateButton("Options", menuPanel, "옵션", new Vector2(-310f, -24f), () => ShowOptions(GameBootState.MainMenu));
            CreateButton("Quit", menuPanel, "종료", new Vector2(-310f, -82f), Application.Quit);
            CreateText("Save Slot Label", menuPanel, "저장 슬롯", 16, FontStyle.Bold, new Vector2(128f, 92f), new Vector2(520, 28), Anchor.Center, TextAnchor.MiddleLeft, new Color(1f, 0.86f, 0.48f));
            slotButtons = new Button[SaveSlotCount];
            for (var index = 0; index < SaveSlotCount; index++)
            {
                var capturedIndex = index;
                slotButtons[index] = CreateButton($"Save Slot {index + 1}", menuPanel, $"슬롯 {index + 1}", new Vector2(128f + index * 168f, 42f), () => SelectSaveSlot(capturedIndex));
            }
            saveSummaryText = CreateText("Save Summary", menuPanel, "", 15, FontStyle.Normal, new Vector2(128f, -110f), new Vector2(520, 120), Anchor.Center, TextAnchor.UpperLeft, new Color(0.86f, 0.92f, 1f));

            optionsPanel = CreatePanel("Options Panel", overlayRoot, Vector2.zero, new Vector2(1280, 720), Anchor.Stretch, new Color(0f, 0f, 0f, 0f));
            CreateText("Options Title", optionsPanel, "옵션", 34, FontStyle.Bold, new Vector2(-285f, 150f), new Vector2(360, 46), Anchor.Center, TextAnchor.MiddleLeft, new Color(1f, 0.86f, 0.48f));
            bgmSlider = CreateSlider("BGM Slider", optionsPanel, "BGM", new Vector2(-120f, 82f), MagicExamSettings.BgmVolume, value => MagicExamSettings.BgmVolume = value);
            sfxSlider = CreateSlider("SFX Slider", optionsPanel, "SFX", new Vector2(-120f, 24f), MagicExamSettings.SfxVolume, value => MagicExamSettings.SfxVolume = value);
            mouseSensitivitySlider = CreateSlider("Mouse Sensitivity Slider", optionsPanel, "감도", new Vector2(-120f, -34f), NormalizeSensitivity(MagicExamSettings.MouseSensitivity), value =>
            {
                MagicExamSettings.MouseSensitivity = Mathf.Lerp(0.55f, 1.75f, value);
                UpdateOptionSummaries();
            });
            volumeSummaryText = CreateText("Volume Summary", optionsPanel, "", 15, FontStyle.Normal, new Vector2(-120f, -82f), new Vector2(520, 30), Anchor.Center, TextAnchor.UpperLeft, new Color(0.86f, 0.92f, 1f));
            CreateButton("Swap Mouse", optionsPanel, "좌/우클릭", new Vector2(-310f, -126f), ToggleSwapMouse);
            CreateButton("Movement Preset", optionsPanel, "이동 키", new Vector2(-62f, -126f), CycleMovementPreset);
            CreateButton("Text Scale", optionsPanel, "텍스트", new Vector2(186f, -126f), CycleTextScale);
            CreateButton("Color Assist", optionsPanel, "색 보조", new Vector2(-310f, -184f), ToggleColorAssist);
            CreateButton("Observer Mode", optionsPanel, "관찰 모드", new Vector2(-62f, -184f), ToggleObserverMode);
            accessibilitySummaryText = CreateText("Accessibility Summary", optionsPanel, "", 14, FontStyle.Normal, new Vector2(186f, -190f), new Vector2(300, 74), Anchor.Center, TextAnchor.UpperLeft, new Color(0.86f, 0.92f, 1f));
            optionsBackButton = CreateButton("Options Back", optionsPanel, "돌아가기", new Vector2(-310f, -260f), ReturnFromOptions);

            pausePanel = CreatePanel("Pause Panel", overlayRoot, Vector2.zero, new Vector2(1280, 720), Anchor.Stretch, new Color(0f, 0f, 0f, 0f));
            CreateText("Pause Title", pausePanel, "일시정지", 34, FontStyle.Bold, new Vector2(-286f, 120f), new Vector2(380, 46), Anchor.Center, TextAnchor.MiddleLeft, new Color(1f, 0.86f, 0.48f));
            resumeButton = CreateButton("Resume", pausePanel, "계속", new Vector2(-310f, 38f), ResumeGameplay);
            CreateButton("Pause Options", pausePanel, "옵션", new Vector2(-310f, -20f), () => ShowOptions(GameBootState.Paused));
            CreateButton("Back To Title", pausePanel, "타이틀로", new Vector2(-310f, -78f), ShowTitleWithFade);

            codexPanel = CreatePanel("Codex Panel", overlayRoot, Vector2.zero, new Vector2(1280, 720), Anchor.Stretch, new Color(0f, 0f, 0f, 0f));
            CreateText("Codex Title", codexPanel, "마법 노트", 32, FontStyle.Bold, new Vector2(-300f, 220f), new Vector2(420, 44), Anchor.Center, TextAnchor.MiddleLeft, new Color(1f, 0.86f, 0.48f));
            CreateButton("Codex Dialogue Tab", codexPanel, "대사", new Vector2(-300f, 164f), () => SetCodexTab(MagicNoteCategory.Dialogue));
            CreateButton("Codex Floor Tab", codexPanel, "층노트", new Vector2(-52f, 164f), () => SetCodexTab(MagicNoteCategory.FloorNote));
            CreateButton("Codex Discovery Tab", codexPanel, "발견", new Vector2(196f, 164f), () => SetCodexTab(MagicNoteCategory.Discovery));
            codexText = CreateText("Codex Text", codexPanel, "", 15, FontStyle.Normal, new Vector2(0f, 12f), new Vector2(820, 380), Anchor.Center, TextAnchor.UpperLeft, new Color(0.93f, 0.96f, 1f));
            CreateButton("Codex Manual Save", codexPanel, "수동 저장", new Vector2(82f, -230f), ManualSaveFromCodex);
            codexCloseButton = CreateButton("Codex Close", codexPanel, "닫기", new Vector2(330f, -230f), ResumeGameplay);

            endingPromptPanel = CreatePanel("Ending Prompt", overlayRoot, new Vector2(0f, -292f), new Vector2(640, 64), Anchor.Center, new Color(0.025f, 0.032f, 0.047f, 0.94f));
            CreateText("Ending Prompt Text", endingPromptPanel, "Enter 또는 클릭으로 타이틀 복귀", 17, FontStyle.Bold, Vector2.zero, new Vector2(600, 38), Anchor.Center, TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.48f));
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
            HideAllPanels();
            pausePanel.gameObject.SetActive(true);
            SetQuickCodexVisible(false);
            SelectButton(resumeButton);
        }

        private void ResumeGameplay()
        {
            Time.timeScale = 1f;
            StateForTests = GameBootState.Gameplay;
            overlayRoot.gameObject.SetActive(false);
            controller.SetGameplayInputEnabled(true);
            SetQuickCodexVisible(true);
        }

        private void ShowOptions(GameBootState returnState)
        {
            optionsReturnState = returnState;
            StateForTests = GameBootState.Options;
            overlayRoot.gameObject.SetActive(true);
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
            StateForTests = GameBootState.Codex;
            overlayRoot.gameObject.SetActive(true);
            HideAllPanels();
            codexText.text = BuildCodexText(codexTab);
            codexPanel.gameObject.SetActive(true);
            SetQuickCodexVisible(false);
            SelectButton(codexCloseButton);
        }

        private void SetCodexTab(MagicNoteCategory category)
        {
            codexTab = category;
            if (StateForTests == GameBootState.Codex && codexText != null)
            {
                codexText.text = BuildCodexText(codexTab);
            }
        }

        private void ShowEndingPrompt()
        {
            Time.timeScale = 1f;
            controller.SetGameplayInputEnabled(false);
            StateForTests = GameBootState.Ending;
            overlayRoot.gameObject.SetActive(true);
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
            RefreshSlotButtons();
            var rows = Enumerable.Range(0, SaveSlotCount)
                .Select(index =>
                {
                    var slotSnapshot = LoadProgress(index);
                    var prefix = index == activeSaveSlotIndex ? ">" : " ";
                    return slotSnapshot == null
                        ? $"{prefix} 슬롯 {index + 1}: 비어 있음"
                        : $"{prefix} 슬롯 {index + 1}: {slotSnapshot.floorNumber}/5층, 목표 {slotSnapshot.completedGoals}/{slotSnapshot.totalGoals}";
                });
            saveSummaryText.text = string.Join("\n", rows) + "\n" + (snapshot == null ? "선택 슬롯에는 저장된 진행이 없습니다." : $"선택 저장: {snapshot.savedAtUtc}");
        }

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
            SaveProgress(controller.CreateProgressSnapshot(), activeSaveSlotIndex);
            codexText.text = BuildCodexText(codexTab) + $"\n\n슬롯 {activeSaveSlotIndex + 1}에 저장했습니다.";
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
                    ? new Color(0.095f, 0.130f, 0.180f, 0.98f)
                    : new Color(0.045f, 0.058f, 0.085f, 0.96f);
                colors.highlightedColor = index == activeSaveSlotIndex
                    ? new Color(0.120f, 0.170f, 0.230f, 1f)
                    : new Color(0.078f, 0.105f, 0.150f, 0.98f);
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
                    ? new Color(0.18f, 0.28f, 0.38f, 0.98f)
                    : new Color(0.045f, 0.058f, 0.085f, 0.96f);
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

        private void HideAllPanels()
        {
            titlePanel.gameObject.SetActive(false);
            menuPanel.gameObject.SetActive(false);
            optionsPanel.gameObject.SetActive(false);
            pausePanel.gameObject.SetActive(false);
            codexPanel.gameObject.SetActive(false);
            endingPromptPanel.gameObject.SetActive(false);
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

        private Button CreateButton(string name, Transform parent, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action)
        {
            var body = CreatePanel(name, parent, anchoredPosition, new Vector2(236, 44), Anchor.Center, new Color(0.045f, 0.058f, 0.085f, 0.96f));
            var image = body.GetComponent<Image>();
            var button = body.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            var colors = button.colors;
            colors.normalColor = new Color(0.045f, 0.058f, 0.085f, 0.96f);
            colors.highlightedColor = new Color(0.078f, 0.105f, 0.150f, 0.98f);
            colors.pressedColor = new Color(0.020f, 0.030f, 0.045f, 1f);
            colors.disabledColor = new Color(0.025f, 0.028f, 0.034f, 0.72f);
            button.colors = colors;
            CreateImage($"{name} Accent", body, Vector2.zero, new Vector2(5, 44), Anchor.TopLeft, new Color(1f, 0.82f, 0.38f, 0.84f));
            CreateText($"{name} Text", body, label, 18, FontStyle.Bold, Vector2.zero, new Vector2(210, 32), Anchor.Center, TextAnchor.MiddleCenter, Color.white);
            return button;
        }

        private Slider CreateSlider(string name, Transform parent, string label, Vector2 anchoredPosition, float value, UnityEngine.Events.UnityAction<float> action)
        {
            CreateText($"{name} Label", parent, label, 16, FontStyle.Bold, anchoredPosition + new Vector2(-190f, 0f), new Vector2(80, 28), Anchor.Center, TextAnchor.MiddleLeft, Color.white);
            var root = CreatePanel(name, parent, anchoredPosition, new Vector2(360, 26), Anchor.Center, new Color(0.020f, 0.026f, 0.040f, 0.92f));
            var fillArea = CreatePanel($"{name} Fill Area", root, Vector2.zero, new Vector2(340, 12), Anchor.Center, new Color(0f, 0f, 0f, 0f));
            var fill = CreateImage($"{name} Fill", fillArea, Vector2.zero, new Vector2(340, 12), Anchor.TopLeft, new Color(0.48f, 0.84f, 1f, 0.92f));
            var handle = CreateImage($"{name} Handle", root, Vector2.zero, new Vector2(18, 28), Anchor.Center, new Color(1f, 0.86f, 0.48f, 0.98f));
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
            var body = CreatePanel("Codex Quick Button", parent, new Vector2(-74f, -30f), new Vector2(118, 42), Anchor.TopRight, new Color(0.045f, 0.058f, 0.085f, 0.96f));
            codexQuickImage = body.GetComponent<Image>();
            var button = body.gameObject.AddComponent<Button>();
            button.targetGraphic = codexQuickImage;
            button.onClick.AddListener(ShowCodex);
            var colors = button.colors;
            colors.normalColor = new Color(0.045f, 0.058f, 0.085f, 0.96f);
            colors.highlightedColor = new Color(0.078f, 0.105f, 0.150f, 0.98f);
            colors.pressedColor = new Color(0.020f, 0.030f, 0.045f, 1f);
            button.colors = colors;
            CreateImage("Codex Quick Accent", body, Vector2.zero, new Vector2(5, 42), Anchor.TopLeft, new Color(1f, 0.82f, 0.38f, 0.84f));
            CreateText("Codex Quick Text", body, "노트", 17, FontStyle.Bold, Vector2.zero, new Vector2(96, 30), Anchor.Center, TextAnchor.MiddleCenter, Color.white);
            return button;
        }

        private void CreateTowerSilhouette(Transform parent)
        {
            CreateImage("Tower Body", parent, new Vector2(250f, -10f), new Vector2(150, 320), Anchor.Center, new Color(0.08f, 0.12f, 0.18f, 0.74f));
            CreateImage("Tower Roof", parent, new Vector2(250f, 178f), new Vector2(210, 42), Anchor.Center, new Color(0.15f, 0.10f, 0.17f, 0.82f));
            CreateImage("Tower Door", parent, new Vector2(250f, -162f), new Vector2(46, 72), Anchor.Center, new Color(0.90f, 0.68f, 0.24f, 0.58f));
            for (var index = 0; index < 5; index++)
            {
                CreateImage($"Tower Window {index}", parent, new Vector2(250f, 98f - index * 52f), new Vector2(42, 12), Anchor.Center, new Color(0.48f, 0.84f, 1f, 0.42f));
            }
        }

        private RectTransform CreatePanel(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Anchor anchor, Color color)
        {
            return CreateImage(name, parent, anchoredPosition, size, anchor, color).rectTransform;
        }

        private Image CreateImage(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Anchor anchor, Color color)
        {
            var body = new GameObject(name);
            body.transform.SetParent(parent, false);
            var rect = body.AddComponent<RectTransform>();
            ApplyAnchor(rect, anchor);
            if (anchor != Anchor.Stretch)
            {
                rect.anchoredPosition = anchoredPosition;
                rect.sizeDelta = size;
            }
            var image = body.AddComponent<Image>();
            image.color = color;
            image.material = PixelMaterialProvider.UiMaterial;
            return image;
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
            Center
        }
    }
}
