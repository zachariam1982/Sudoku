using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// View — reacts to game state changes and shows/hides the appropriate UI panels.
/// Attach to a child of Canvas that contains the state UI panels.
/// 
/// Scene structure expected:
///   DailyChallengeGameStateView (this script)
///   ├── WinPanel
///   │     ├── Star1, Star2, Star3     (Image components)
///   │     ├── TimeLabel               (TextMeshProUGUI)
///   │     ├── MistakesLabel           (TextMeshProUGUI)
///   │     ├── NewGameButton
///   │     └── Title                   (TextMeshProUGUI)
///   ├── LosePanel
///   │     ├── LivesLabel              (TextMeshProUGUI)
///   │     ├── RetryButton
///   │     └── NewGameButton
///   ├── PausePanel
///   │     ├── ResumeButton
///   │     └── NewGameButton
///   └── HUD (always visible during play)
///         ├── TimerLabel              (TextMeshProUGUI)
///         ├── LivesContainer
///         │     ├── Life1, Life2, Life3  (Image — heart icons)
///         └── PauseButton
/// </summary>
public class DailyChallengeGameStateView : MonoBehaviour
{
    [Header("TopBar Items")]
    [SerializeField] private GameObject Lives;
    [SerializeField] private GameObject Timer;
    [SerializeField] private GameObject Settings;
    [SerializeField] private TextMeshProUGUI Level;
    [Header("HUD — always visible during play")]
    [SerializeField] private TextMeshProUGUI timerLabel;
    [SerializeField] private GameObject      hudPanel;

    [Header("Blocker dialog")]
    [SerializeField] private GameObject      overlay;
    [Header("Win Panel")]
    [SerializeField] private GameObject      winPanel;
    [SerializeField] private Sprite           winLandscapeSprite;
    [SerializeField] private Sprite           winPortraitSprite;
    [SerializeField] private Button           winBackButton;
    [SerializeField] private RectTransform     winStats;
    [SerializeField] private TextMeshProUGUI   winStarsText;
    [SerializeField] private TextMeshProUGUI   winTimeText;
    [SerializeField] private TextMeshProUGUI   winDifficultyText;

    [Header("Lose Panel")]
    [SerializeField] private GameObject      losePanel;
    [SerializeField] private Sprite           loseLandscapeSprite;
    [SerializeField] private Sprite           losePortraitSprite;
    [SerializeField] private Button           loseBackButton;

    [Header("Pause Panel")]
    [SerializeField] private GameObject      pausePanel;

    [Header("Top Bar")]
    [SerializeField] private DailyChallengeTopBar      DailyChallengeTopBar;

    private BaseViewModel _vm;
    private HUD _hud;
    private VerseTicker _verseTicker;
    private Button _pauseResumeButton;
    private bool _lastResultLandscape;
    private int _lastResultScreenWidth = -1;
    private int _lastResultScreenHeight = -1;
    private bool _gameplayVisible;

    private void Start()
    {
        _lastResultLandscape = Screen.width > Screen.height;
        ApplyResultBackButtonSizing();
    }

    private void Update()
    {
        bool landscape = Screen.width > Screen.height;
        bool screenSizeChanged = Screen.width != _lastResultScreenWidth ||
                                 Screen.height != _lastResultScreenHeight;
        if (!screenSizeChanged) return;

        _lastResultScreenWidth = Screen.width;
        _lastResultScreenHeight = Screen.height;
        ApplyResultBackButtonSizing();

        if (landscape == _lastResultLandscape) return;
        _lastResultLandscape = landscape;
        if (_vm == null) return;

        if (_vm.IsWon.Value)
            ShowResultArt(true);
        else if (_vm is DailyChallengeViewModel dailyChallenge && dailyChallenge.IsLost.Value)
            ShowResultArt(false);
    }

    // ── Binding ───────────────────────────────────────────────────────────────

    public void Bind(BaseViewModel vm)
    {
        if (ReferenceEquals(_vm, vm))
        {
            // The game can be suspended while the Home screen is shown and then
            // resumed with the same view model. Rebind the HUD in case the SOS
            // dialog initialized after the first binding, then restore the UI.
            if (vm != null)
            {
                _hud?.Bind(vm);
                OnStateChanged(vm.CurrentStateName.Value);
            }
            return;
        }
        Unbind();
        _vm = vm;

        _pauseResumeButton = pausePanel != null
            ? pausePanel.GetComponentInChildren<Button>(true)
            : null;
        if (_pauseResumeButton != null)
            _pauseResumeButton.onClick.AddListener(OnPlayPressed);

        vm.CurrentStateName.OnChanged += OnStateChanged;
        DailyChallengeViewModel dailyChallenge = vm as DailyChallengeViewModel;
        if (dailyChallenge != null)
        {
            dailyChallenge.ElapsedSeconds.OnChanged += OnTimerChanged;
            dailyChallenge.LivesRemaining.OnChanged += OnLivesChanged;
            dailyChallenge.IsWon.OnChanged += OnDailyWonChanged;
            dailyChallenge.IsLost.OnChanged += OnDailyLostChanged;
            OnTimerChanged(dailyChallenge.ElapsedSeconds.Value);
        }
        _hud = hudPanel != null
            ? hudPanel.GetComponent<HUD>()
            : null;

        _hud?.Bind(vm);

        SetAllPanelsHidden();
        OnStateChanged(vm.CurrentStateName.Value);
    }

    private void OnDestroy()
    {
        Unbind();
    }

    private void Unbind()
    {
        if (_vm == null) return;

        if (_pauseResumeButton != null)
        {
            _pauseResumeButton.onClick.RemoveListener(OnPlayPressed);
            _pauseResumeButton = null;
        }

        _vm.CurrentStateName.OnChanged -= OnStateChanged;
        DailyChallengeViewModel dailyChallenge = _vm as DailyChallengeViewModel;
        if (dailyChallenge != null)
        {
            dailyChallenge.ElapsedSeconds.OnChanged -= OnTimerChanged;
            dailyChallenge.LivesRemaining.OnChanged -= OnLivesChanged;
            dailyChallenge.IsWon.OnChanged -= OnDailyWonChanged;
            dailyChallenge.IsLost.OnChanged -= OnDailyLostChanged;
        }
        _vm = null;
    }

    // ── State Change Handlers ─────────────────────────────────────────────────

    private void OnStateChanged(string stateName)
    {
        if (!_gameplayVisible)
        {
            if (hudPanel != null) hudPanel.SetActive(false);
            if (winPanel != null) winPanel.SetActive(false);
            if (losePanel != null) losePanel.SetActive(false);
            if (DailyChallengeTopBar != null) DailyChallengeTopBar.gameObject.SetActive(false);
            UpdateVerseTicker(string.Empty);
            return;
        }

        SetAllPanelsHidden();
        if (_vm is DailyChallengeViewModel dailyChallenge)
        {
            SetLevelText(((SudokuDifficulty)dailyChallenge.GetDifficulty).ToString());
            OnTimerChanged(dailyChallenge.ElapsedSeconds.Value);
        }

        switch (stateName)
        {
            case "DailyChallengeIdleState":
                if (hudPanel != null) hudPanel.SetActive(false);
                break;

            case "DailyChallengePlayingState":
            case "NewGamePlayingState":
                if (hudPanel != null) hudPanel.SetActive(true);
                if(_vm != null) SetLevelText(((SudokuDifficulty)_vm.GetDifficulty).ToString());
                break;

            case "DailyChallengePausedState":
                if(pausePanel != null)pausePanel.SetActive(true);                
                break;

            case "DailyChallengeValidatingState":
                if(hudPanel != null ) hudPanel.SetActive(false);
                break;

            case "DailyChallengeWinState":
                ShowDailyResult(true);
                break;

            case "DailyChallengeLoseState":
                ShowDailyResult(false);
                break;

        }

        ApplyModeVisibility(stateName);
        UpdateVerseTicker(stateName);
    }

    private void UpdateVerseTicker(string stateName)
    {
        if (_verseTicker == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                _verseTicker = canvas.GetComponent<VerseTicker>();
        }

        bool gameActive = stateName == "DailyChallengePlayingState" ||
                          stateName == "DailyChallengePausedState";

        if (_verseTicker != null)
            _verseTicker.SetGameActive(gameActive);
    }

    // ── Timer ─────────────────────────────────────────────────────────────────

    private void OnTimerChanged(float seconds)
    {
        if (timerLabel == null) return;

        int minutes = (int)(seconds / 60f);
        int secs    = (int)(seconds % 60f);
        timerLabel.text = $"{minutes:00}:{secs:00}";
    }

    // ── Lives ─────────────────────────────────────────────────────────────────

    private void OnLivesChanged(int lives)
    {
        Transform container = this.Lives.transform;
        if (container == null) return;

        for (int i = 0; i < container.childCount; i++)
        {
            container.GetChild(i).gameObject.SetActive(i < lives);
        }
    }

    private void OnDailyWonChanged(bool isWon)
    {
        if (isWon && _gameplayVisible && _vm is DailyChallengeViewModel)
            ShowDailyResult(true);
    }

    private void OnDailyLostChanged(bool isLost)
    {
        if (isLost && _gameplayVisible && _vm is DailyChallengeViewModel)
            ShowDailyResult(false);
    }

    // ── Win / Lose Panels ─────────────────────────────────────────────────────

    private void ShowResultArt(bool won)
    {
        GameObject panel = won ? winPanel : losePanel;
        if (panel == null) return;

        Image panelImage = panel.GetComponent<Image>();
        if (panelImage == null) return;

        bool landscape = Screen.width > Screen.height;
        panelImage.sprite = won
            ? (landscape ? winLandscapeSprite : winPortraitSprite)
            : (landscape ? loseLandscapeSprite : losePortraitSprite);
    }

    private void ShowDailyResult(bool won)
    {
        if (overlay != null) overlay.SetActive(true);

        GameObject resultPanel = won ? winPanel : losePanel;
        GameObject otherPanel = won ? losePanel : winPanel;
        if (otherPanel != null) otherPanel.SetActive(false);
        if (resultPanel == null) return;

        resultPanel.SetActive(true);
        resultPanel.transform.SetAsLastSibling();
        ShowResultArt(won);
        if (won) RefreshWinStats();
    }

    private void RefreshWinStats()
    {
        if (!(_vm is DailyChallengeViewModel daily) || daily.ChallengeRecord == null) return;
        if (winStarsText != null) winStarsText.text = $"{daily.ChallengeRecord.Stars} Stars";
        if (winTimeText != null)
        {
            int seconds = Mathf.Max(0, Mathf.FloorToInt(daily.ChallengeRecord.ElapsedSeconds));
            winTimeText.text = $"{seconds / 60:00}:{seconds % 60:00}";
        }
        if (winDifficultyText != null) winDifficultyText.text = ((SudokuDifficulty)daily.ChallengeRecord.Difficulty).ToString();
    }

    private void ApplyResultBackButtonSizing()
    {
        // Screen dimensions can change before stretched RectTransforms have
        // recalculated. Flush the layout first so each rotation uses the new
        // result-panel dimensions instead of the previous orientation's.
        Canvas.ForceUpdateCanvases();
        bool landscape = Screen.width > Screen.height;
        FitResultBackButton(winBackButton, landscape);
        FitResultBackButton(loseBackButton, landscape);

        if (winStats == null) return;
        RectTransform screen = winStats.parent as RectTransform;
        if (screen == null) return;
        winStats.sizeDelta = landscape ? new Vector2(560f, 320f) : new Vector2(900f, 300f);
        winStats.anchoredPosition = landscape
            ? new Vector2(screen.rect.width * 0.30f, screen.rect.height * 0.14f)
            : new Vector2(0f, -screen.rect.height * 0.24f);
        float referenceWidth = landscape ? 1920f : 1080f;
        float referenceHeight = landscape ? 1080f : 1920f;
        float scale = Mathf.Min(screen.rect.width / referenceWidth, screen.rect.height / referenceHeight);
        winStats.localScale = Vector3.one * Mathf.Clamp(scale, 0.65f, 1.35f);

        string[] iconNames = { "StarsIcon", "TimeIcon", "DifficultyIcon" };
        TextMeshProUGUI[] labels = { winStarsText, winTimeText, winDifficultyText };
        for (int i = 0; i < labels.Length; i++)
        {
            RectTransform icon = winStats.Find(iconNames[i]) as RectTransform;
            if (icon == null || labels[i] == null) continue;

            float rowY = landscape ? 100f - i * 100f : 48f;
            icon.sizeDelta = landscape ? new Vector2(60f, 60f) : new Vector2(70f, 70f);
            icon.anchoredPosition = landscape
                ? new Vector2(-205f, rowY)
                : new Vector2((i - 1) * 290f, rowY);
            labels[i].rectTransform.sizeDelta = landscape
                ? new Vector2(330f, 70f)
                : new Vector2(250f, 68f);
            labels[i].rectTransform.anchoredPosition = landscape
                ? new Vector2(25f, rowY)
                : new Vector2((i - 1) * 290f, -58f);
            labels[i].fontSize = landscape ? 38f : 48f;
            labels[i].alignment = landscape
                ? TextAlignmentOptions.Left
                : TextAlignmentOptions.Center;
        }
    }

    private static void FitResultBackButton(Button button, bool landscape)
    {
        if (button == null) return;

        RectTransform buttonRect = button.transform as RectTransform;
        RectTransform screenRect = button.transform.parent as RectTransform;
        if (buttonRect == null || screenRect == null) return;

        Image image = button.targetGraphic as Image;
        Rect spriteRect = image != null && image.sprite != null
            ? image.sprite.rect
            : new Rect(0f, 0f, 5f, 1f);
        float aspect = spriteRect.height > 0f ? spriteRect.width / spriteRect.height : 5f;
        float width = screenRect.rect.width * (landscape ? 0.24f : 0.52f);
        float height = width / aspect;

        buttonRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        buttonRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
    }

    public void OnBackToDailyChallengePressed()
    {
        // GameContext handles the mode exit and reopens DailyChallengeReadyPage.
        _vm?.NotifyGameCompleted();
    }

    public void OnPlayPressed()
    {
        if (!_gameplayVisible) return;
        if (pausePanel != null) pausePanel.SetActive(false);
        _vm?.ResumeCommand.Execute();
    }
    // ── Helper ────────────────────────────────────────────────────────────────

    private void SetAllPanelsHidden()
    {
        if (hudPanel   != null) hudPanel.SetActive(false);
        if (winPanel   != null) winPanel.SetActive(false);
        if (losePanel  != null) losePanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (overlay    != null) overlay.SetActive(false);
    }

    private void ApplyModeVisibility(string stateName)
    {
        bool dailyChallengeMode = _vm is DailyChallengeViewModel;
        bool hideStatus = stateName == "DailyChallengeValidatingState" ||
                          stateName == "DailyChallengeWinState" ||
                          stateName == "DailyChallengeLoseState";

        if (Lives != null) Lives.SetActive(dailyChallengeMode && !hideStatus);
        if (Timer != null) Timer.SetActive(dailyChallengeMode && !hideStatus);
        if (Settings != null) Settings.SetActive(dailyChallengeMode);
        bool dailyResultVisible = stateName == "DailyChallengeWinState" ||
                                  stateName == "DailyChallengeLoseState";
        if (DailyChallengeTopBar != null)
            DailyChallengeTopBar.SetDailyMode(dailyChallengeMode && !dailyResultVisible);
    }

    /// <summary>Shows gameplay HUD only while a game mode is active.</summary>
    public void SetGameplayVisible(bool visible)
    {
        _gameplayVisible = visible;
        if (visible)
        {
            if (_vm != null)
                OnStateChanged(_vm.CurrentStateName.Value);
            return;
        }

        SetAllPanelsHidden();
        if (DailyChallengeTopBar != null) DailyChallengeTopBar.gameObject.SetActive(false);
        UpdateVerseTicker(string.Empty);
    }

    private void SetLevelText(string text)
    {
        if (Level != null) Level.text = text;
    }
}
