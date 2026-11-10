using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>State-driven panels and top bar for Journey mode only.</summary>
public sealed class JourneyGameStateView : MonoBehaviour
{
    [Header("Scene Dependencies")]
    [SerializeField] private GameContext gameContext;
    [SerializeField] private HUD _hud;
    [SerializeField] private VerseTicker _verseTicker;
    [SerializeField] private Button _pauseResumeButton;

    [Header("Journey top bar")]
    [SerializeField] private GameObject Lives;
    [SerializeField] private GameObject Timer;
    [SerializeField] private GameObject Settings;
    [SerializeField] private TextMeshProUGUI Level;
    [SerializeField] private TextMeshProUGUI timerLabel;
    [SerializeField] private GameObject journeyHudPanel;
    [SerializeField] private JourneyTopBar journeyTopBar;

    [Header("Journey result and pause panels")]
    [SerializeField] private GameObject overlay;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private TextMeshProUGUI winTimeLabel;
    [SerializeField] private TextMeshProUGUI winPointsLabel;
    [SerializeField] private RectTransform journeyGameBanner;
    [SerializeField] private RectTransform winVictoryTrophyEmblem;
    [SerializeField] private RectTransform winPuzzleCompleteWordmark;
    [SerializeField] private RectTransform winStats;
    [SerializeField] private TextMeshProUGUI winDifficultyLabel;
    [SerializeField] private RectTransform winNextGameButton;
    [SerializeField] private GameObject losePanel;
    [SerializeField] private RectTransform loseStats;
    [SerializeField] private TextMeshProUGUI loseTimeLabel;
    [SerializeField] private TextMeshProUGUI loseDifficultyLabel;
    [SerializeField] private RectTransform loseJourneyGameBanner;
    [SerializeField] private RectTransform loseFailedEmblem;
    [SerializeField] private RectTransform losePuzzleFailedWordmark;
    [SerializeField] private RectTransform loseNextGameButton;
    [SerializeField] private RectTransform loseRetryButton;
    [SerializeField] private GameObject pausePanel;

    private JourneyViewModel _viewModel;
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
    private bool _boardHomeScaleCaptured;
    private RectTransform[] _winPanelElements;
    private Vector2[] _winPanelElementHomePositions;
    private RectTransform[] _losePanelElements;
    private Vector2[] _losePanelElementHomePositions;
    private int _lastWinPanelScreenWidth = -1;
    private int _lastWinPanelScreenHeight = -1;
    private int _lastLosePanelScreenWidth = -1;
    private int _lastLosePanelScreenHeight = -1;

    private const float ChromeSlideDuration = 0.55f;
    private const float BoardFadeDuration = 0.4f;
    private const float WinPanelElementDuration = 0.55f;

    private void Update()
    {
        if (_isResultTransitionRunning) return;
        bool landscape = Screen.width > Screen.height;

        if (winPanel != null && winPanel.activeSelf &&
            (_lastWinPanelScreenWidth != Screen.width || _lastWinPanelScreenHeight != Screen.height))
        {
            ApplyWinPanelLayout(landscape);
            CaptureWinPanelElementHomePositions();
        }

        if (losePanel != null && losePanel.activeSelf &&
            (_lastLosePanelScreenWidth != Screen.width || _lastLosePanelScreenHeight != Screen.height))
        {
            ApplyLosePanelLayout(landscape);
            CaptureLosePanelElementHomePositions();
        }
    }

    public void Bind(JourneyViewModel viewModel)
    {
        if (ReferenceEquals(_viewModel, viewModel))
        {
            if (viewModel != null)
            {
                _hud?.Bind(viewModel);
                if (_gameplayVisible) OnStateChanged(viewModel.CurrentStateName.Value);
            }
            return;
        }

        Unbind();
        _viewModel = viewModel;
        if (_viewModel == null) return;
        CacheTransitionElements();
        CacheWinPanelElements();
        CacheLosePanelElements();

        if (_pauseResumeButton != null)
            _pauseResumeButton.onClick.AddListener(OnPlayPressed);

        _viewModel.CurrentStateName.OnChanged += OnStateChanged;
        _viewModel.ElapsedSeconds.OnChanged += OnTimerChanged;
        _viewModel.LivesRemaining.OnChanged += OnLivesChanged;
        _hud?.Bind(_viewModel);

        SetAllPanelsHidden();
        if (_gameplayVisible) OnStateChanged(_viewModel.CurrentStateName.Value);
    }

    private void OnDestroy()
    {
        if (_resultTransition != null)
        {
            StopCoroutine(_resultTransition);
            _resultTransition = null;
        }
        Unbind();
        _hud?.Unbind();
    }

    private void Unbind()
    {
        if (_viewModel == null) return;
        if (_pauseResumeButton != null)
        {
            _pauseResumeButton.onClick.RemoveListener(OnPlayPressed);
            _pauseResumeButton = null;
        }

        _viewModel.CurrentStateName.OnChanged -= OnStateChanged;
        _viewModel.ElapsedSeconds.OnChanged -= OnTimerChanged;
        _viewModel.LivesRemaining.OnChanged -= OnLivesChanged;
        _viewModel = null;
    }

    private void OnStateChanged(string stateName)
    {
        if (!_gameplayVisible)
        {
            if (journeyHudPanel != null) journeyHudPanel.SetActive(false);
            if (winPanel != null) winPanel.SetActive(false);
            if (losePanel != null) losePanel.SetActive(false);
            UpdateVerseTicker(false);
            return;
        }

        bool isResultState = stateName == "JourneyWinState" || stateName == "JourneyLoseState";
        if (_isResultTransitionRunning && !isResultState)
            CancelResultTransition();

        SetAllPanelsHidden();
        switch (stateName)
        {
            case "JourneyIdleState":
                RestoreBoardLayout();
                SetLevelText(string.Empty);
                break;
            case "JourneyPlayingState":
                RestoreGameplayLayout();
                if (journeyHudPanel != null) journeyHudPanel.SetActive(true);
                SetLevelText(((SudokuDifficulty)_viewModel.GetDifficulty).ToString());
                break;
            case "JourneyPausedState":
                if (pausePanel != null) pausePanel.SetActive(true);
                break;
            case "JourneyValidatingState":
                if (journeyHudPanel != null) journeyHudPanel.SetActive(false);
                break;
            case "JourneyWinState":
                BeginResultTransition(true);
                break;
            case "JourneyLoseState":
                BeginResultTransition(false);
                break;
        }

        ApplyTopBarVisibility(stateName);
        UpdateVerseTicker(stateName == "JourneyPlayingState" || stateName == "JourneyPausedState");
    }

    private void ApplyTopBarVisibility(string stateName)
    {
        bool hideStatus = stateName == "JourneyValidatingState";
        if (Lives != null) Lives.SetActive(!hideStatus);
        if (Timer != null) Timer.SetActive(!hideStatus);
        if (Settings != null) Settings.SetActive(true);
        if (journeyTopBar != null) journeyTopBar.SetNewGameMode(false);
    }

    private void UpdateVerseTicker(bool gameActive)
    {
        _verseTicker?.SetGameActive(gameActive);
    }

    private void OnTimerChanged(float seconds)
    {
        if (timerLabel == null) return;
        int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
        timerLabel.text = $"{total / 60:00}:{total % 60:00}";
    }

    private void OnLivesChanged(int lives)
    {
        if (Lives == null) return;
        Transform container = Lives.transform;
        for (int i = 0; i < container.childCount; i++)
            container.GetChild(i).gameObject.SetActive(i < lives);
    }

    private void UpdateWinPanel()
    {
        if (_viewModel == null) return;
        if (winTimeLabel != null)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(_viewModel.ElapsedSeconds.Value));
            winTimeLabel.text = $"{total / 60:00}:{total % 60:00}";
        }
        if (winPointsLabel != null)
        {
            int points = ScoringSystem.Calculate(
                (SudokuDifficulty)_viewModel.GetDifficulty,
                _viewModel.ElapsedSeconds.Value,
                _viewModel.Penalties);
            winPointsLabel.text = $"Points: {points}/200";
        }
        if (winDifficultyLabel != null)
            winDifficultyLabel.text = ((SudokuDifficulty)_viewModel.GetDifficulty).ToString();
    }

    private void UpdateLosePanel()
    {
        if (_viewModel == null) return;
        int total = Mathf.Max(0, Mathf.FloorToInt(_viewModel.ElapsedSeconds.Value));
        if (loseTimeLabel != null)
            loseTimeLabel.text = $"{total / 60:00}:{total % 60:00}";
        if (loseDifficultyLabel != null)
            loseDifficultyLabel.text = ((SudokuDifficulty)_viewModel.GetDifficulty).ToString();
    }

    public void OnNewGamePressed()
    {
        HideResultPanels();
        _viewModel?.NewGameCommand.Execute();
    }

    public void OnRetryPressed()
    {
        HideResultPanels();
        _viewModel?.RetryCommand.Execute();
    }

    public void OnPlayPressed()
    {
        if (!_gameplayVisible) return;
        if (pausePanel != null) pausePanel.SetActive(false);
        _viewModel?.ResumeCommand.Execute();
    }

    public void SetGameplayVisible(bool visible)
    {
        _gameplayVisible = visible;
        if (visible && _viewModel != null)
        {
            OnStateChanged(_viewModel.CurrentStateName.Value);
            return;
        }

        CancelResultTransition();
        SetAllPanelsHidden();
        // The grid is shared by all modes. Restoring it while Journey is hidden
        // makes the live board leak through the Home screen.
        if (_gameBoard != null) _gameBoard.gameObject.SetActive(false);
        if (journeyTopBar != null) journeyTopBar.gameObject.SetActive(false);
        UpdateVerseTicker(false);
    }

    private void CacheTransitionElements()
    {
        if (_gameBoard == null) _gameBoard = gameContext != null ? gameContext.GridPanel : null;
        if (_topBarRect == null && journeyTopBar != null)
            _topBarRect = journeyTopBar.transform as RectTransform;
        if (_hudRect == null && journeyHudPanel != null)
            _hudRect = journeyHudPanel.transform as RectTransform;

        if (!_homePositionsCaptured && _topBarRect != null && _hudRect != null)
        {
            _topBarHomePosition = _topBarRect.anchoredPosition;
            _hudHomePosition = _hudRect.anchoredPosition;
            _homePositionsCaptured = true;
        }

        if (_gameBoard != null && !_boardHomeScaleCaptured)
        {
            _boardHomeScale = _gameBoard.localScale;
            _boardHomeScaleCaptured = true;
        }
        if (_boardCanvasGroup == null && gameContext != null)
            _boardCanvasGroup = gameContext.GridCanvasGroup;
    }

    private void BeginResultTransition(bool won)
    {
        if (_isResultTransitionRunning) return;
        _isResultTransitionRunning = true;
        _resultTransition = StartCoroutine(PlayResultTransition(won));
    }

    /// <summary>Slides Journey chrome away, fades and shrinks the board, then reveals the result panel.</summary>
    private IEnumerator PlayResultTransition(bool won)
    {
        CacheTransitionElements();
        RectTransform chromeParent = _topBarRect != null
            ? _topBarRect.parent as RectTransform
            : null;
        float chromeWidth = chromeParent != null ? chromeParent.rect.width : Screen.width;

        if (journeyTopBar != null)
        {
            journeyTopBar.SetNewGameMode(false);
            journeyTopBar.gameObject.SetActive(true);
        }
        if (journeyHudPanel != null) journeyHudPanel.SetActive(true);
        if (_topBarRect != null) _topBarRect.anchoredPosition = _topBarHomePosition;
        if (_hudRect != null) _hudRect.anchoredPosition = _hudHomePosition;

        float chromeElapsed = 0f;
        while (chromeElapsed < ChromeSlideDuration && (_topBarRect != null || _hudRect != null))
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

        if (journeyTopBar != null) journeyTopBar.gameObject.SetActive(false);
        if (journeyHudPanel != null) journeyHudPanel.SetActive(false);

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

        if (won)
        {
            UpdateWinPanel();
            yield return PlayWinPanelEntrance();
        }
        else
        {
            UpdateLosePanel();
            yield return PlayLosePanelEntrance();
        }

        _resultTransition = null;
        _isResultTransitionRunning = false;
    }

    private void CancelResultTransition()
    {
        if (_resultTransition != null)
        {
            StopCoroutine(_resultTransition);
            _resultTransition = null;
        }
        _isResultTransitionRunning = false;

        CacheTransitionElements();
        if (_topBarRect != null) _topBarRect.anchoredPosition = _topBarHomePosition;
        if (_hudRect != null) _hudRect.anchoredPosition = _hudHomePosition;
        RestoreWinPanelElementPositions();
        RestoreLosePanelElementPositions();
        RestoreBoardLayout();
    }

    private void CacheWinPanelElements()
    {
        if (_winPanelElements != null || winPanel == null) return;

        var elements = new System.Collections.Generic.List<RectTransform>();
        for (int i = 0; i < winPanel.transform.childCount; i++)
        {
            RectTransform element = winPanel.transform.GetChild(i) as RectTransform;
            if (element != null && element.gameObject.activeSelf)
                elements.Add(element);
        }
        _winPanelElements = elements.ToArray();
        _winPanelElementHomePositions = new Vector2[_winPanelElements.Length];
        CaptureWinPanelElementHomePositions();
    }

    private IEnumerator PlayWinPanelEntrance()
    {
        CacheWinPanelElements();
        if (winPanel == null || _winPanelElements == null)
        {
            if (overlay != null) overlay.SetActive(true);
            if (winPanel != null) winPanel.SetActive(true);
            yield break;
        }
        ApplyWinPanelLayout(Screen.width > Screen.height);
        CaptureWinPanelElementHomePositions();
        RestoreWinPanelElementPositions();
        Vector2[] startPositions = new Vector2[_winPanelElements.Length];

        for (int i = 0; i < _winPanelElements.Length; i++)
        {
            RectTransform element = _winPanelElements[i];
            if (element == null) continue;

            RectTransform parent = element.parent as RectTransform;
            float width = parent != null ? parent.rect.width : Screen.width;
            float height = parent != null ? parent.rect.height : Screen.height;
            Vector2 target = _winPanelElementHomePositions[i];
            Vector2 offset = i == 0 || i == 1
                ? new Vector2(-(width + element.rect.width), 0f)
                : i == _winPanelElements.Length - 1
                    ? new Vector2(0f, -(height + element.rect.height))
                    : new Vector2(width + element.rect.width, 0f);
            startPositions[i] = target + offset;
            element.anchoredPosition = startPositions[i];
        }

        if (overlay != null) overlay.SetActive(true);
        if (winPanel != null) winPanel.SetActive(true);

        float elapsed = 0f;
        while (elapsed < WinPanelElementDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / WinPanelElementDuration));
            for (int i = 0; i < _winPanelElements.Length; i++)
            {
                if (_winPanelElements[i] != null)
                    _winPanelElements[i].anchoredPosition = Vector2.LerpUnclamped(
                        startPositions[i], _winPanelElementHomePositions[i], t);
            }
            yield return null;
        }

        RestoreWinPanelElementPositions();
    }

    private void RestoreWinPanelElementPositions()
    {
        CacheWinPanelElements();
        if (_winPanelElements == null || _winPanelElementHomePositions == null) return;
        for (int i = 0; i < _winPanelElements.Length; i++)
            if (_winPanelElements[i] != null)
                _winPanelElements[i].anchoredPosition = _winPanelElementHomePositions[i];
    }

    private void CaptureWinPanelElementHomePositions()
    {
        CacheWinPanelElements();
        if (_winPanelElements == null || _winPanelElementHomePositions == null) return;
        for (int i = 0; i < _winPanelElements.Length; i++)
            if (_winPanelElements[i] != null)
                _winPanelElementHomePositions[i] = _winPanelElements[i].anchoredPosition;
    }

    private void CacheLosePanelElements()
    {
        if (_losePanelElements != null || losePanel == null) return;

        var elements = new System.Collections.Generic.List<RectTransform>();
        for (int i = 0; i < losePanel.transform.childCount; i++)
        {
            RectTransform element = losePanel.transform.GetChild(i) as RectTransform;
            if (element != null && element.gameObject.activeSelf)
                elements.Add(element);
        }

        _losePanelElements = elements.ToArray();
        _losePanelElementHomePositions = new Vector2[_losePanelElements.Length];
        CaptureLosePanelElementHomePositions();
    }

    private IEnumerator PlayLosePanelEntrance()
    {
        CacheLosePanelElements();
        if (losePanel == null || _losePanelElements == null)
        {
            if (overlay != null) overlay.SetActive(true);
            if (losePanel != null) losePanel.SetActive(true);
            yield break;
        }

        ApplyLosePanelLayout(Screen.width > Screen.height);
        CaptureLosePanelElementHomePositions();
        RestoreLosePanelElementPositions();
        Vector2[] startPositions = new Vector2[_losePanelElements.Length];

        for (int i = 0; i < _losePanelElements.Length; i++)
        {
            RectTransform element = _losePanelElements[i];
            if (element == null) continue;

            RectTransform parent = element.parent as RectTransform;
            float width = parent != null ? parent.rect.width : Screen.width;
            float height = parent != null ? parent.rect.height : Screen.height;
            Vector2 target = _losePanelElementHomePositions[i];
            string elementName = element.name;
            Vector2 offset = elementName.Contains("Stats")
                ? new Vector2(width + element.rect.width, 0f)
                : elementName.Contains("Banner") || elementName.Contains("Wordmark")
                    ? new Vector2(-(width + element.rect.width), 0f)
                    : new Vector2(0f, -(height + element.rect.height));
            startPositions[i] = target + offset;
            element.anchoredPosition = startPositions[i];
        }

        if (overlay != null) overlay.SetActive(true);
        losePanel.SetActive(true);

        float elapsed = 0f;
        while (elapsed < WinPanelElementDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / WinPanelElementDuration));
            for (int i = 0; i < _losePanelElements.Length; i++)
            {
                if (_losePanelElements[i] != null)
                    _losePanelElements[i].anchoredPosition = Vector2.LerpUnclamped(
                        startPositions[i], _losePanelElementHomePositions[i], t);
            }
            yield return null;
        }

        RestoreLosePanelElementPositions();
    }

    private void RestoreLosePanelElementPositions()
    {
        CacheLosePanelElements();
        if (_losePanelElements == null || _losePanelElementHomePositions == null) return;
        for (int i = 0; i < _losePanelElements.Length; i++)
            if (_losePanelElements[i] != null)
                _losePanelElements[i].anchoredPosition = _losePanelElementHomePositions[i];
    }

    private void CaptureLosePanelElementHomePositions()
    {
        CacheLosePanelElements();
        if (_losePanelElements == null || _losePanelElementHomePositions == null) return;
        for (int i = 0; i < _losePanelElements.Length; i++)
            if (_losePanelElements[i] != null)
                _losePanelElementHomePositions[i] = _losePanelElements[i].anchoredPosition;
    }

    private void ApplyWinPanelLayout(bool landscape)
    {
        RectTransform panel = winPanel != null ? winPanel.GetComponent<RectTransform>() : null;
        if (panel == null) return;
        Canvas.ForceUpdateCanvases();
        float width = panel.rect.width;
        float height = panel.rect.height;
        if (width <= 0f || height <= 0f) return;

        Image wordmarkImage = winPuzzleCompleteWordmark != null
            ? winPuzzleCompleteWordmark.GetComponent<Image>()
            : null;
        Rect spriteRect = wordmarkImage != null && wordmarkImage.sprite != null
            ? wordmarkImage.sprite.rect
            : new Rect(0f, 0f, 1180f, 440f);
        float wordmarkAspect = spriteRect.height > 0f
            ? spriteRect.width / spriteRect.height
            : 1180f / 440f;

        if (landscape)
        {
            float bannerWidth = width * 0.30f;
            float emblemSize = Mathf.Min(width * 0.29f, height * 0.58f);
            float wordmarkWidth = width * 0.32f;
            SetWinElementLayout(journeyGameBanner, new Vector2(0.15f, 0.77f),
                new Vector2(bannerWidth, bannerWidth * (230f / 980f)));
            SetWinElementLayout(winVictoryTrophyEmblem, new Vector2(0.47f, 0.62f),
                new Vector2(emblemSize, emblemSize));
            SetWinElementLayout(winPuzzleCompleteWordmark, new Vector2(0.16f, 0.50f),
                new Vector2(wordmarkWidth, wordmarkWidth / wordmarkAspect));
            ApplyWinStatsLayout(landscape, width, height);
            FitWinNextGameButton(landscape, width);
        }
        else
        {
            float bannerWidth = width * 0.72f;
            float wordmarkWidth = width * 0.90f;
            float emblemSize = Mathf.Min(width * 0.58f, height * 0.30f);
            SetWinElementLayout(journeyGameBanner, new Vector2(0.5f, 0.93f),
                new Vector2(bannerWidth, bannerWidth * (230f / 980f)));
            SetWinElementLayout(winPuzzleCompleteWordmark, new Vector2(0.5f, 0.80f),
                new Vector2(wordmarkWidth, wordmarkWidth / wordmarkAspect));
            SetWinElementLayout(winVictoryTrophyEmblem, new Vector2(0.5f, 0.57f),
                new Vector2(emblemSize, emblemSize));
            ApplyWinStatsLayout(landscape, width, height);
            FitWinNextGameButton(landscape, width);
        }

        _lastWinPanelScreenWidth = Screen.width;
        _lastWinPanelScreenHeight = Screen.height;
    }

    private void ApplyLosePanelLayout(bool landscape)
    {
        RectTransform panel = losePanel != null ? losePanel.GetComponent<RectTransform>() : null;
        if (panel == null) return;

        panel.anchorMin = Vector2.zero;
        panel.anchorMax = Vector2.one;
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = Vector2.zero;
        Canvas.ForceUpdateCanvases();
        float width = panel.rect.width;
        float height = panel.rect.height;
        if (width <= 0f || height <= 0f) return;

        Image wordmarkImage = losePuzzleFailedWordmark != null
            ? losePuzzleFailedWordmark.GetComponent<Image>()
            : null;
        Rect spriteRect = wordmarkImage != null && wordmarkImage.sprite != null
            ? wordmarkImage.sprite.rect
            : new Rect(0f, 0f, 1180f, 440f);
        float wordmarkAspect = spriteRect.height > 0f
            ? spriteRect.width / spriteRect.height
            : 1180f / 440f;

        if (landscape)
        {
            float bannerWidth = width * 0.30f;
            float emblemSize = Mathf.Min(width * 0.29f, height * 0.58f);
            float wordmarkWidth = width * 0.32f;
            SetWinElementLayout(loseJourneyGameBanner, new Vector2(0.15f, 0.77f),
                new Vector2(bannerWidth, bannerWidth * (230f / 980f)));
            SetWinElementLayout(losePuzzleFailedWordmark, new Vector2(0.16f, 0.50f),
                new Vector2(wordmarkWidth, wordmarkWidth / wordmarkAspect));
            SetWinElementLayout(loseFailedEmblem, new Vector2(0.47f, 0.62f),
                new Vector2(emblemSize, emblemSize));
            ApplyLoseStatsLayout(true, width, height);
            FitLoseButtons(true, width);
        }
        else
        {
            float bannerWidth = width * 0.72f;
            float wordmarkWidth = width * 0.90f;
            float emblemSize = Mathf.Min(width * 0.58f, height * 0.30f);
            SetWinElementLayout(loseJourneyGameBanner, new Vector2(0.5f, 0.93f),
                new Vector2(bannerWidth, bannerWidth * (230f / 980f)));
            SetWinElementLayout(losePuzzleFailedWordmark, new Vector2(0.5f, 0.80f),
                new Vector2(wordmarkWidth, wordmarkWidth / wordmarkAspect));
            SetWinElementLayout(loseFailedEmblem, new Vector2(0.5f, 0.57f),
                new Vector2(emblemSize, emblemSize));
            ApplyLoseStatsLayout(false, width, height);
            FitLoseButtons(false, width);
        }

        _lastLosePanelScreenWidth = Screen.width;
        _lastLosePanelScreenHeight = Screen.height;
    }

    private void ApplyLoseStatsLayout(bool landscape, float screenWidth, float screenHeight)
    {
        if (loseStats == null) return;
        loseStats.anchorMin = loseStats.anchorMax = landscape
            ? new Vector2(0.82f, 0.55f)
            : new Vector2(0.5f, 0.36f);
        loseStats.anchoredPosition = Vector2.zero;
        loseStats.sizeDelta = landscape ? new Vector2(620f, 360f) : new Vector2(900f, 250f);

        float referenceWidth = landscape ? 1920f : 1080f;
        float referenceHeight = landscape ? 1080f : 1920f;
        float scale = Mathf.Min(screenWidth / referenceWidth, screenHeight / referenceHeight);
        loseStats.localScale = Vector3.one * Mathf.Clamp(scale, 0.65f, 1.35f);

        TextMeshProUGUI[] labels = { loseTimeLabel, loseDifficultyLabel };
        string[] iconNames = { "TimeIcon", "DifficultyIcon" };
        for (int i = 0; i < labels.Length; i++)
        {
            RectTransform icon = loseStats.Find(iconNames[i]) as RectTransform;
            if (icon == null || labels[i] == null) continue;
            float rowY = landscape ? 78f - i * 156f : 32f;
            icon.sizeDelta = landscape ? new Vector2(88f, 88f) : new Vector2(64f, 64f);
            icon.anchoredPosition = landscape
                ? new Vector2(-220f, rowY)
                : new Vector2(i == 0 ? -180f : 180f, rowY);
            labels[i].rectTransform.sizeDelta = landscape
                ? new Vector2(390f, 96f)
                : new Vector2(300f, 72f);
            labels[i].rectTransform.anchoredPosition = landscape
                ? new Vector2(55f, rowY)
                : new Vector2(i == 0 ? -180f : 180f, -42f);
            labels[i].fontSize = landscape ? 48f : 32f;
            labels[i].alignment = landscape
                ? TextAlignmentOptions.Left
                : TextAlignmentOptions.Center;
        }
    }

    private void FitLoseButtons(bool landscape, float screenWidth)
    {
        if (loseRetryButton == null || loseNextGameButton == null) return;
        float buttonWidth = screenWidth * (landscape ? 0.22f : 0.55f);
        SetResultButtonSize(loseRetryButton, buttonWidth);
        SetResultButtonSize(loseNextGameButton, buttonWidth);

        if (landscape)
        {
            SetWinElementLayout(loseRetryButton, new Vector2(0.38f, 0.12f), loseRetryButton.sizeDelta);
            SetWinElementLayout(loseNextGameButton, new Vector2(0.62f, 0.12f), loseNextGameButton.sizeDelta);
        }
        else
        {
            SetWinElementLayout(loseRetryButton, new Vector2(0.5f, 0.18f), loseRetryButton.sizeDelta);
            SetWinElementLayout(loseNextGameButton, new Vector2(0.5f, 0.075f), loseNextGameButton.sizeDelta);
        }
    }

    private static void SetResultButtonSize(RectTransform button, float width)
    {
        Image image = button.GetComponent<Image>();
        Rect rect = image != null && image.sprite != null
            ? image.sprite.rect
            : new Rect(0f, 0f, 3f, 1f);
        float aspect = rect.height > 0f ? rect.width / rect.height : 3f;
        button.sizeDelta = new Vector2(width, width / aspect);
    }

    private void ApplyWinStatsLayout(bool landscape, float screenWidth, float screenHeight)
    {
        if (winStats == null) return;

        RectTransform screen = winStats.parent as RectTransform;
        if (screen == null) return;
        winStats.sizeDelta = landscape ? new Vector2(680f, 500f) : new Vector2(900f, 300f);
        winStats.anchoredPosition = landscape
            ? new Vector2(screenWidth * 0.29f, screenHeight * 0.02f)
            : new Vector2(0f, -screenHeight * 0.24f);

        float referenceWidth = landscape ? 1920f : 1080f;
        float referenceHeight = landscape ? 1080f : 1920f;
        float scale = Mathf.Min(screenWidth / referenceWidth, screenHeight / referenceHeight);
        winStats.localScale = Vector3.one * Mathf.Clamp(scale, 0.65f, 1.35f);

        string[] iconNames = { "StarsIcon", "TimeIcon", "DifficultyIcon" };
        TextMeshProUGUI[] labels = { winPointsLabel, winTimeLabel, winDifficultyLabel };
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
            labels[i].fontSize = i == 0
                ? (landscape ? 42f : 28f)
                : (landscape ? 54f : 48f);
            labels[i].alignment = landscape
                ? TextAlignmentOptions.Left
                : TextAlignmentOptions.Center;
        }
    }

    private void FitWinNextGameButton(bool landscape, float screenWidth)
    {
        if (winNextGameButton == null) return;
        Image image = winNextGameButton.GetComponent<Image>();
        Rect spriteRect = image != null && image.sprite != null
            ? image.sprite.rect
            : new Rect(0f, 0f, 3f, 1f);
        float aspect = spriteRect.height > 0f ? spriteRect.width / spriteRect.height : 3f;
        float buttonWidth = screenWidth * (landscape ? 0.24f : 0.52f);
        SetWinElementLayout(winNextGameButton, new Vector2(0.5f, 0.12f),
            new Vector2(buttonWidth, buttonWidth / aspect));
    }

    private static void SetWinElementLayout(RectTransform element, Vector2 anchor, Vector2 size)
    {
        if (element == null) return;
        element.anchorMin = anchor;
        element.anchorMax = anchor;
        element.anchoredPosition = Vector2.zero;
        element.sizeDelta = size;
    }

    private void RestoreBoardLayout()
    {
        CacheTransitionElements();
        if (_gameBoard == null) return;
        _gameBoard.gameObject.SetActive(true);
        _gameBoard.localScale = _boardHomeScale;
        if (_boardCanvasGroup != null) _boardCanvasGroup.alpha = 1f;
    }

    private void RestoreGameplayLayout()
    {
        CacheTransitionElements();
        if (_resultTransition != null)
        {
            StopCoroutine(_resultTransition);
            _resultTransition = null;
        }
        _isResultTransitionRunning = false;
        if (_topBarRect != null) _topBarRect.anchoredPosition = _topBarHomePosition;
        if (_hudRect != null) _hudRect.anchoredPosition = _hudHomePosition;
        if (journeyTopBar != null)
        {
            journeyTopBar.SetNewGameMode(false);
            journeyTopBar.gameObject.SetActive(true);
        }
        if (journeyHudPanel != null) journeyHudPanel.SetActive(true);
        if (_gameBoard != null)
        {
            _gameBoard.gameObject.SetActive(true);
            _gameBoard.localScale = _boardHomeScale;
            if (_boardCanvasGroup != null) _boardCanvasGroup.alpha = 1f;
        }
    }

    private void HideResultPanels()
    {
        CancelResultTransition();
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        if (overlay != null) overlay.SetActive(false);
    }

    private void SetAllPanelsHidden()
    {
        if (journeyHudPanel != null) journeyHudPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (overlay != null) overlay.SetActive(false);
    }

    private void SetLevelText(string value)
    {
        if (Level != null) Level.text = value;
    }
}
