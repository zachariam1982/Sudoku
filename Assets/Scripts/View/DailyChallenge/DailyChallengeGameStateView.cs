using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

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
    [SerializeField] private RectTransform     dailyChallengeGameBanner;
    [SerializeField] private RectTransform     winVictoryTrophyEmblem;
    [SerializeField] private RectTransform     winPuzzleCompleteWordmark;
    [SerializeField] private RectTransform     loseDailyChallengeGameBanner;
    [SerializeField] private RectTransform     loseFailedEmblem;
    [SerializeField] private RectTransform     losePuzzleFailedWordmark;
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
    private Coroutine _resultTransition;
    private bool _isResultTransitionRunning;
    private RectTransform _gameBoard;
    private RectTransform _topBarRect;
    private RectTransform _hudRect;
    private Vector2 _topBarHomePosition;
    private Vector2 _hudHomePosition;
    private bool _homePositionsCaptured;
    private CanvasGroup _boardCanvasGroup;
    private Vector3 _boardHomeScale = Vector3.one;

    private const float ChromeSlideDuration = 0.55f;
    private const float BoardFadeDuration = 0.4f;
    private const float ResultPanelDuration = 0.55f;

    private void Start()
    {
        _lastResultLandscape = Screen.width > Screen.height;
        ApplyResultBackButtonSizing();
        CacheTransitionElements();
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
        CacheTransitionElements();

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
                BeginResultTransition(true);
                break;

            case "DailyChallengeLoseState":
                BeginResultTransition(false);
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

    private void BeginResultTransition(bool won)
    {
        if (_isResultTransitionRunning) return;
        _isResultTransitionRunning = true;
        _resultTransition = StartCoroutine(PlayResultTransition(won));
    }

    /// <summary>
    /// Uses the New Game win choreography for both Daily Challenge outcomes:
    /// slide the HUD and top bar away, fade and shrink the board, then reveal
    /// the Daily Challenge's own result artwork and stats.
    /// </summary>
    private IEnumerator PlayResultTransition(bool won)
    {
        CacheTransitionElements();
        RectTransform chromeParent = _topBarRect != null
            ? _topBarRect.parent as RectTransform
            : null;
        float chromeWidth = chromeParent != null ? chromeParent.rect.width : Screen.width;

        if (DailyChallengeTopBar != null)
            DailyChallengeTopBar.SetDailyMode(true);
        if (hudPanel != null) hudPanel.SetActive(true);
        if (_topBarRect != null) _topBarRect.anchoredPosition = _topBarHomePosition;
        if (_hudRect != null) _hudRect.anchoredPosition = _hudHomePosition;

        float chromeElapsed = 0f;
        while (chromeElapsed < ChromeSlideDuration &&
               (_topBarRect != null || _hudRect != null))
        {
            chromeElapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(chromeElapsed / ChromeSlideDuration));
            if (_topBarRect != null)
                _topBarRect.anchoredPosition = Vector2.LerpUnclamped(
                    _topBarHomePosition,
                    new Vector2(chromeWidth + _topBarRect.rect.width, _topBarHomePosition.y), t);
            if (_hudRect != null)
                _hudRect.anchoredPosition = Vector2.LerpUnclamped(
                    _hudHomePosition,
                    new Vector2(-chromeWidth - _hudRect.rect.width, _hudHomePosition.y), t);
            yield return null;
        }

        if (DailyChallengeTopBar != null)
            DailyChallengeTopBar.SetDailyMode(false);
        if (hudPanel != null) hudPanel.SetActive(false);

        if (_gameBoard != null && _boardCanvasGroup != null)
        {
            _gameBoard.gameObject.SetActive(true);
            _boardCanvasGroup.alpha = 1f;
            Vector3 startScale = _gameBoard.localScale;
            float boardElapsed = 0f;
            while (boardElapsed < BoardFadeDuration)
            {
                boardElapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(boardElapsed / BoardFadeDuration));
                _boardCanvasGroup.alpha = 1f - t;
                _gameBoard.localScale = Vector3.LerpUnclamped(startScale, startScale * 0.88f, t);
                yield return null;
            }
            _boardCanvasGroup.alpha = 0f;
            _gameBoard.localScale = startScale * 0.88f;
            _gameBoard.gameObject.SetActive(false);
        }

        ShowDailyResult(won);
        yield return PlayDailyResultEntrance(won);
        _resultTransition = null;
        _isResultTransitionRunning = false;
        ApplyModeVisibility(_vm != null ? _vm.CurrentStateName.Value : string.Empty);
    }

    private void CacheTransitionElements()
    {
        GameContext context = GetComponentInParent<GameContext>();
        if (context == null) context = FindFirstObjectByType<GameContext>();
        if (_gameBoard != null) return;

        _gameBoard = context != null ? context.GridPanel : null;
        _topBarRect = DailyChallengeTopBar != null
            ? DailyChallengeTopBar.GetComponent<RectTransform>()
            : null;
        _hudRect = hudPanel != null ? hudPanel.GetComponent<RectTransform>() : null;

        if (!_homePositionsCaptured)
        {
            if (_topBarRect != null) _topBarHomePosition = _topBarRect.anchoredPosition;
            if (_hudRect != null) _hudHomePosition = _hudRect.anchoredPosition;
            _homePositionsCaptured = true;
        }

        if (_gameBoard != null)
        {
            _boardHomeScale = _gameBoard.localScale;
            _boardCanvasGroup = _gameBoard.GetComponent<CanvasGroup>();
            if (_boardCanvasGroup == null)
                _boardCanvasGroup = _gameBoard.gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void CancelResultTransition()
    {
        if (_resultTransition != null)
        {
            StopCoroutine(_resultTransition);
            _resultTransition = null;
        }
        _isResultTransitionRunning = false;

        if (_topBarRect != null) _topBarRect.anchoredPosition = _topBarHomePosition;
        if (_hudRect != null) _hudRect.anchoredPosition = _hudHomePosition;
        if (hudPanel != null) hudPanel.SetActive(false);
        if (DailyChallengeTopBar != null) DailyChallengeTopBar.SetDailyMode(false);
        if (_gameBoard != null)
        {
            _gameBoard.gameObject.SetActive(true);
            _gameBoard.localScale = _boardHomeScale;
            if (_boardCanvasGroup != null) _boardCanvasGroup.alpha = 1f;
        }

        RestoreResultPanel(winPanel);
        RestoreResultPanel(losePanel);
    }

    private static void RestoreResultPanel(GameObject panel)
    {
        if (panel == null) return;
        RectTransform rect = panel.GetComponent<RectTransform>();
        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }
        if (rect != null) rect.localScale = Vector3.one;
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

        ApplyWinArtLayout(landscape);
        ApplyLoseArtLayout(landscape);

        if (winStats == null) return;
        RectTransform screen = winStats.parent as RectTransform;
        if (screen == null) return;
        winStats.sizeDelta = landscape ? new Vector2(680f, 500f) : new Vector2(900f, 300f);
        winStats.anchoredPosition = landscape
            ? new Vector2(screen.rect.width * 0.29f, screen.rect.height * 0.02f)
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

            float rowY = landscape ? 145f - i * 145f : 48f;
            icon.sizeDelta = landscape ? new Vector2(92f, 92f) : new Vector2(70f, 70f);
            icon.anchoredPosition = landscape
                ? new Vector2(-265f, rowY)
                : new Vector2((i - 1) * 290f, rowY);
            labels[i].rectTransform.sizeDelta = landscape
                ? new Vector2(460f, 100f)
                : new Vector2(250f, 68f);
            labels[i].rectTransform.anchoredPosition = landscape
                ? new Vector2(50f, rowY)
                : new Vector2((i - 1) * 290f, -58f);
            labels[i].fontSize = landscape ? 54f : 48f;
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

    private void ApplyWinArtLayout(bool landscape)
    {
        ApplyResultArtLayout(landscape, dailyChallengeGameBanner,
            winVictoryTrophyEmblem, winPuzzleCompleteWordmark);
    }

    private void ApplyLoseArtLayout(bool landscape)
    {
        ApplyResultArtLayout(landscape, loseDailyChallengeGameBanner,
            loseFailedEmblem, losePuzzleFailedWordmark);
    }

    private void ApplyResultArtLayout(
        bool landscape,
        RectTransform banner,
        RectTransform emblem,
        RectTransform wordmark)
    {
        RectTransform panelRect = banner != null ? banner.parent as RectTransform : null;
        if (panelRect == null && winPanel != null)
            panelRect = winPanel.GetComponent<RectTransform>();
        float width = panelRect != null ? panelRect.rect.width : Screen.width;
        float height = panelRect != null ? panelRect.rect.height : Screen.height;
        if (width <= 0f || height <= 0f) return;
        Image wordmarkImage = wordmark != null ? wordmark.GetComponent<Image>() : null;
        Rect wordmarkRect = wordmarkImage != null && wordmarkImage.sprite != null
            ? wordmarkImage.sprite.rect
            : new Rect(0f, 0f, 1180f, 440f);
        float wordmarkHeightRatio = wordmarkRect.width > 0f
            ? wordmarkRect.height / wordmarkRect.width
            : 440f / 1180f;

        if (landscape)
        {
            float trophySize = Mathf.Min(width * 0.29f, height * 0.58f);
            float wordmarkWidth = width * 0.32f;
            float trophyCenterX = 0.47f;
            SetWinElementLayout(banner, new Vector2(0.15f, 0.77f),
                new Vector2(width * 0.30f, width * 0.30f * (230f / 980f)));
            SetWinElementLayout(emblem, new Vector2(trophyCenterX, 0.62f),
                new Vector2(trophySize, trophySize));
            SetWinElementLayout(wordmark, new Vector2(0.16f, 0.50f),
                new Vector2(wordmarkWidth, wordmarkWidth * wordmarkHeightRatio));
        }
        else
        {
            float bannerWidth = width * 0.72f;
            float wordmarkWidth = width * 0.90f;
            // Keep the emblem above the Daily stats card and Back button.
            float trophySize = Mathf.Min(width * 0.58f, height * 0.30f);
            SetWinElementLayout(banner, new Vector2(0.5f, 0.93f),
                new Vector2(bannerWidth, bannerWidth * (230f / 980f)));
            SetWinElementLayout(wordmark, new Vector2(0.5f, 0.80f),
                new Vector2(wordmarkWidth, wordmarkWidth * wordmarkHeightRatio));
            SetWinElementLayout(emblem, new Vector2(0.5f, 0.57f),
                new Vector2(trophySize, trophySize));
        }
    }

    private static void SetWinElementLayout(RectTransform element, Vector2 anchor, Vector2 size)
    {
        if (element == null) return;
        element.anchorMin = anchor;
        element.anchorMax = anchor;
        element.anchoredPosition = Vector2.zero;
        element.sizeDelta = size;
    }

    private IEnumerator PlayDailyResultEntrance(bool won)
    {
        bool landscape = Screen.width > Screen.height;
        ApplyWinArtLayout(landscape);
        ApplyLoseArtLayout(landscape);
        RectTransform[] elements = won
            ? new[]
            {
                dailyChallengeGameBanner,
                winVictoryTrophyEmblem,
                winPuzzleCompleteWordmark,
                winStats,
                winBackButton != null ? winBackButton.transform as RectTransform : null
            }
            : new[]
            {
                loseDailyChallengeGameBanner,
                loseFailedEmblem,
                losePuzzleFailedWordmark,
                loseBackButton != null ? loseBackButton.transform as RectTransform : null
            };
        Vector2[] targets = new Vector2[elements.Length];
        Vector2[] starts = new Vector2[elements.Length];
        for (int i = 0; i < elements.Length; i++)
        {
            RectTransform element = elements[i];
            if (element == null) continue;

            targets[i] = element.anchoredPosition;
            RectTransform parent = element.parent as RectTransform;
            float width = parent != null ? parent.rect.width : Screen.width;
            float height = parent != null ? parent.rect.height : Screen.height;
            if (i < 2)
                starts[i] = targets[i] + new Vector2(-(width + element.rect.width), 0f);
            else if (i == 2)
                starts[i] = targets[i] + new Vector2(width + element.rect.width, 0f);
            else
                starts[i] = targets[i] + new Vector2(0f, -(height + element.rect.height));
            element.anchoredPosition = starts[i];
        }

        float elapsed = 0f;
        while (elapsed < ResultPanelDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / ResultPanelDuration));
            for (int i = 0; i < elements.Length; i++)
                if (elements[i] != null)
                    elements[i].anchoredPosition = Vector2.LerpUnclamped(starts[i], targets[i], t);
            yield return null;
        }

        for (int i = 0; i < elements.Length; i++)
            if (elements[i] != null)
                elements[i].anchoredPosition = targets[i];
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
            DailyChallengeTopBar.SetDailyMode(
                dailyChallengeMode && (!dailyResultVisible || _isResultTransitionRunning));
    }

    /// <summary>Shows gameplay HUD only while a game mode is active.</summary>
    public void SetGameplayVisible(bool visible)
    {
        _gameplayVisible = visible;
        if (!visible && _isResultTransitionRunning)
            CancelResultTransition();
        if (visible)
        {
            // The result transition hides the board and leaves its CanvasGroup
            // faded out. Restore it before a later challenge renders its puzzle.
            if (_isResultTransitionRunning)
                CancelResultTransition();
            else
                RestoreGameplayLayout();

            if (_vm != null)
                OnStateChanged(_vm.CurrentStateName.Value);
            return;
        }

        SetAllPanelsHidden();
        if (DailyChallengeTopBar != null) DailyChallengeTopBar.gameObject.SetActive(false);
        UpdateVerseTicker(string.Empty);
    }

    private void RestoreGameplayLayout()
    {
        CacheTransitionElements();

        if (_topBarRect != null) _topBarRect.anchoredPosition = _topBarHomePosition;
        if (_hudRect != null) _hudRect.anchoredPosition = _hudHomePosition;
        if (_gameBoard == null) return;

        _gameBoard.gameObject.SetActive(true);
        _gameBoard.localScale = _boardHomeScale;
        if (_boardCanvasGroup != null) _boardCanvasGroup.alpha = 1f;
    }

    private void SetLevelText(string text)
    {
        if (Level != null) Level.text = text;
    }
}
