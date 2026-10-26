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
    [SerializeField] private TextMeshProUGUI newGameLevel;
    [Header("HUD — always visible during play")]
    [SerializeField] private TextMeshProUGUI timerLabel;
    [SerializeField] private Image[]         lifeIcons;       // 3 heart images
    [SerializeField] private Color           lifeActiveColor  = new Color(0.92f, 0.27f, 0.38f, 1f);
    [SerializeField] private Color           lifeEmptyColor   = new Color(0.3f,  0.3f,  0.4f,  1f);
    [SerializeField] private GameObject      hudPanel;

    [Header("Blocker dialog")]
    [SerializeField] private GameObject      overlay;
    [Header("Win Panel")]
    [SerializeField] private GameObject      winPanel;
    [SerializeField] private Sprite           winLandscapeSprite;
    [SerializeField] private Sprite           winPortraitSprite;

    [Header("Lose Panel")]
    [SerializeField] private GameObject      losePanel;
    [SerializeField] private Sprite           loseLandscapeSprite;
    [SerializeField] private Sprite           losePortraitSprite;

    [Header("Pause Panel")]
    [SerializeField] private GameObject      pausePanel;

    [Header("Top Bar")]
    [SerializeField] private DailyChallengeTopBar      DailyChallengeTopBar;

    private BaseViewModel _vm;
    private HUD _hud;
    private VerseTicker _verseTicker;
    private Button _pauseResumeButton;
    private bool _lastResultLandscape;

    private void Start()
    {
        _lastResultLandscape = Screen.width > Screen.height;
    }

    private void Update()
    {
        bool landscape = Screen.width > Screen.height;
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
            // resumed with the same view model. Reapply its current UI state so
            // mode-specific controls (including the top bar) become visible again.
            if (vm != null)
                OnStateChanged(vm.CurrentStateName.Value);
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
            case "NewGamePausedState":
                if(pausePanel != null)pausePanel.SetActive(true);                
                break;

            case "DailyChallengeValidatingState":
            case "NewGameValidatingState":
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
                          stateName == "NewGamePlayingState" ||
                          stateName == "DailyChallengePausedState" ||
                          stateName == "NewGamePausedState";

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
        if (isWon && _vm is DailyChallengeViewModel)
            ShowDailyResult(true);
    }

    private void OnDailyLostChanged(bool isLost)
    {
        if (isLost && _vm is DailyChallengeViewModel)
            ShowDailyResult(false);
    }

    // ── Stars ─────────────────────────────────────────────────────────────────

    private void OnStarRatingChanged(int stars)
    {

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
    }

    public void OnNewGamePressed() 
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        if (overlay != null) overlay.SetActive(false);
        (_vm as DailyChallengeViewModel)?.NewGameCommand.Execute();
    }
    public void OnRetryPressed()    
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        if (overlay != null) overlay.SetActive(false);
        (_vm as DailyChallengeViewModel)?.RetryCommand.Execute();
    }
    public void OnPlayPressed()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        _vm?.ResumeCommand.Execute();
    }
    public void OnResumePressed()   => _vm?.ResumeCommand.Execute();

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
        bool journeyMode = _vm is DailyChallengeViewModel;
        bool hideStatus = stateName == "DailyChallengeValidatingState" ||
                          stateName == "NewGameValidatingState" ||
                          stateName == "DailyChallengeWinState" ||
                          stateName == "NewGameWinState" ||
                          stateName == "DailyChallengeLoseState";

        if (Lives != null) Lives.SetActive(journeyMode && !hideStatus);
        if (Timer != null) Timer.SetActive(journeyMode && !hideStatus);
        if (Settings != null) Settings.SetActive(journeyMode);
        SetTopBarMode(_vm is DailyChallengeViewModel);
    }

    public void SetTopBarMode(bool isDailyChallenge)
    {
        if (DailyChallengeTopBar != null)
            DailyChallengeTopBar.SetDailyMode(isDailyChallenge);
    }

    /// <summary>Shows gameplay HUD only while a game mode is active.</summary>
    public void SetGameplayVisible(bool visible)
    {
        if (visible)
        {
            if (_vm != null)
                OnStateChanged(_vm.CurrentStateName.Value);
            return;
        }

        SetAllPanelsHidden();
        if (DailyChallengeTopBar != null) DailyChallengeTopBar.gameObject.SetActive(false);
    }

    private void SetLevelText(string text)
    {
        if (Level != null) Level.text = text;
        if (newGameLevel != null) newGameLevel.text = text;
    }
}
