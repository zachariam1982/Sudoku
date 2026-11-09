using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>State-driven panels and top bar for New Game mode only.</summary>
public sealed class NewGameGameStateView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI levelLabel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject overlay;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private Image winPanelBackground;
    [SerializeField] private Sprite winLandscapeBackground;
    [SerializeField] private Sprite winPortraitBackground;
    [SerializeField] private RectTransform winNewGameBanner;
    [SerializeField] private RectTransform winPuzzleCompleteWordmark;
    [SerializeField] private RectTransform winVictoryTrophyEmblem;
    [SerializeField] private RectTransform winBackToHomeButton;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private NewGameTopBar topBar;

    private NewGameViewModel _viewModel;
    private HUD _hud;
    private VerseTicker _verseTicker;
    private Button _pauseResumeButton;
    private bool _gameplayVisible;
    private bool _hasWinLayout;
    private bool _lastWinLandscape;
    private Vector2 _lastWinPanelSize;
    private Coroutine _winTransition;
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
    private const float WinElementDuration = 0.55f;

    private void Update()
    {
        if (_winTransition != null || !_gameplayVisible || _viewModel == null || winPanel == null ||
            !winPanel.activeSelf || _viewModel.CurrentStateName.Value != "NewGameWinState")
        {
            return;
        }

        bool isLandscape = Screen.width > Screen.height;
        Vector2 panelSize = GetWinPanelSize();
        if (_hasWinLayout && _lastWinLandscape == isLandscape &&
            Mathf.Abs(_lastWinPanelSize.x - panelSize.x) < 0.5f &&
            Mathf.Abs(_lastWinPanelSize.y - panelSize.y) < 0.5f) return;
        ApplyWinPanelLayout(isLandscape);
    }

    public void Bind(NewGameViewModel viewModel)
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

        _pauseResumeButton = pausePanel != null
            ? pausePanel.GetComponentInChildren<Button>(true)
            : null;
        if (_pauseResumeButton != null)
            _pauseResumeButton.onClick.AddListener(OnPlayPressed);

        _viewModel.CurrentStateName.OnChanged += OnStateChanged;
        _hud = hudPanel != null ? hudPanel.GetComponent<HUD>() : null;
        _hud?.Bind(_viewModel);

        SetAllPanelsHidden();
        if (_gameplayVisible) OnStateChanged(_viewModel.CurrentStateName.Value);
    }

    private void OnDestroy()
    {
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
        _viewModel = null;
    }

    private void OnStateChanged(string stateName)
    {
        if (!_gameplayVisible)
        {
            if (hudPanel != null) hudPanel.SetActive(false);
            if (winPanel != null) winPanel.SetActive(false);
            if (topBar != null) topBar.gameObject.SetActive(false);
            UpdateVerseTicker(false);
            return;
        }

        SetAllPanelsHidden();
        switch (stateName)
        {
            case "NewGamePlayingState":
                RestoreGameplayLayout();
                if (hudPanel != null) hudPanel.SetActive(true);
                if (_viewModel != null && levelLabel != null)
                    levelLabel.text = ((SudokuDifficulty)_viewModel.GetDifficulty).ToString();
                break;
            case "NewGamePausedState":
                if (pausePanel != null) pausePanel.SetActive(true);
                break;
            case "NewGameValidatingState":
                if (hudPanel != null) hudPanel.SetActive(false);
                break;
            case "NewGameWinState":
                if (overlay != null) overlay.SetActive(false);
                if (winPanel != null) winPanel.SetActive(false);
                ApplyWinPanelLayout(Screen.width > Screen.height);
                if (_winTransition != null) StopCoroutine(_winTransition);
                _winTransition = StartCoroutine(PlayWinTransition());
                break;
        }

        if (topBar != null) topBar.SetNewGameMode(true);
        UpdateVerseTicker(stateName == "NewGamePlayingState" || stateName == "NewGamePausedState");
    }

    private void CacheTransitionElements()
    {
        if (_gameBoard != null) return;
        GameContext context = GetComponentInParent<GameContext>();
        if (context == null) context = FindObjectOfType<GameContext>();
        _gameBoard = context != null ? context.GridPanel : null;
        _topBarRect = topBar != null ? topBar.GetComponent<RectTransform>() : null;
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
            if (_boardCanvasGroup == null) _boardCanvasGroup = _gameBoard.gameObject.AddComponent<CanvasGroup>();
        }
    }

    private IEnumerator PlayWinTransition()
    {
        CacheTransitionElements();
        RectTransform chromeParent = _topBarRect != null ? _topBarRect.parent as RectTransform : null;
        float chromeWidth = chromeParent != null ? chromeParent.rect.width : Screen.width;

        // The validating state hides the HUD. Restore both pieces at their original
        // positions, then move them out together so the exit begins on the win frame.
        Vector2 topBarEnd = _topBarHomePosition;
        Vector2 hudEnd = _hudHomePosition;
        if (topBar != null) topBar.gameObject.SetActive(true);
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
                _topBarRect.anchoredPosition = Vector2.LerpUnclamped(topBarEnd,
                    new Vector2(chromeWidth + _topBarRect.rect.width, topBarEnd.y), t);
            if (_hudRect != null)
                _hudRect.anchoredPosition = Vector2.LerpUnclamped(hudEnd,
                    new Vector2(-chromeWidth - _hudRect.rect.width, hudEnd.y), t);
            yield return null;
        }
        if (topBar != null) topBar.gameObject.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(false);

        if (_gameBoard != null && _boardCanvasGroup != null)
        {
            _gameBoard.gameObject.SetActive(true);
            _boardCanvasGroup.alpha = 1f;
            Vector3 startScale = _gameBoard.localScale;
            yield return FadeBoard(startScale);
            _gameBoard.gameObject.SetActive(false);
        }

        RectTransform[] elements = {
            winNewGameBanner, winVictoryTrophyEmblem,
            winPuzzleCompleteWordmark, winBackToHomeButton
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
            float direction = i < 2 ? -1f : 1f;
            starts[i] = targets[i] + new Vector2(direction * (width + element.rect.width), 0f);
            element.anchoredPosition = starts[i];
        }

        // Reveal only after the board has disappeared. The panel's background and
        // sprites are enabled with the sprites already offscreen, avoiding a flash
        // of the completed layout before their entrance starts.
        if (overlay != null) overlay.SetActive(true);
        if (winPanel != null) winPanel.SetActive(true);

        float elapsed = 0f;
        while (elapsed < WinElementDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / WinElementDuration));
            for (int i = 0; i < elements.Length; i++)
                if (elements[i] != null)
                    elements[i].anchoredPosition = Vector2.LerpUnclamped(starts[i], targets[i], t);
            yield return null;
        }
        for (int i = 0; i < elements.Length; i++)
            if (elements[i] != null) elements[i].anchoredPosition = targets[i];

        _winTransition = null;
    }

    private static IEnumerator Slide(RectTransform rect, Vector2 start, Vector2 end, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            rect.anchoredPosition = Vector2.LerpUnclamped(start, end, t);
            yield return null;
        }
        rect.anchoredPosition = end;
    }

    private IEnumerator FadeBoard(Vector3 startScale)
    {
        float elapsed = 0f;
        while (elapsed < BoardFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / BoardFadeDuration));
            _boardCanvasGroup.alpha = 1f - t;
            _gameBoard.localScale = Vector3.LerpUnclamped(startScale, startScale * 0.88f, t);
            yield return null;
        }
        _boardCanvasGroup.alpha = 0f;
        _gameBoard.localScale = startScale * 0.88f;
    }

    private void CancelWinTransition()
    {
        CacheTransitionElements();
        if (_topBarRect != null) _topBarRect.anchoredPosition = _topBarHomePosition;
        if (_hudRect != null) _hudRect.anchoredPosition = _hudHomePosition;
        if (topBar != null) topBar.gameObject.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(false);
        if (_gameBoard != null)
        {
            _gameBoard.localScale = _boardHomeScale;
            if (_boardCanvasGroup != null) _boardCanvasGroup.alpha = 1f;
        }
    }

    private void RestoreGameplayLayout()
    {
        CacheTransitionElements();
        if (_winTransition != null)
        {
            StopCoroutine(_winTransition);
            _winTransition = null;
        }
        if (_topBarRect != null) _topBarRect.anchoredPosition = _topBarHomePosition;
        if (_hudRect != null) _hudRect.anchoredPosition = _hudHomePosition;
        if (topBar != null) topBar.gameObject.SetActive(true);
        if (hudPanel != null) hudPanel.SetActive(true);
        if (_gameBoard != null)
        {
            _gameBoard.gameObject.SetActive(true);
            _gameBoard.localScale = _boardHomeScale;
            if (_boardCanvasGroup != null) _boardCanvasGroup.alpha = 1f;
        }
    }

    private void UpdateVerseTicker(bool gameActive)
    {
        if (_verseTicker == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null) _verseTicker = canvas.GetComponent<VerseTicker>();
        }
        _verseTicker?.SetGameActive(gameActive);
    }

    public void OnWinAcknowledged()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (overlay != null) overlay.SetActive(false);
        _viewModel?.NotifyGameCompleted();
    }

    private void ApplyWinPanelLayout(bool isLandscape)
    {
        Vector2 panelSize = GetWinPanelSize();
        float width = panelSize.x;
        float height = panelSize.y;
        if (width <= 0f || height <= 0f) return;

        if (winPanelBackground != null)
        {
            Sprite background = isLandscape ? winLandscapeBackground : winPortraitBackground;
            if (background != null) winPanelBackground.sprite = background;
        }

        if (isLandscape)
        {
            // Size every element from the panel bounds. The gap is also a width
            // percentage, so the trophy and wordmark stay separated at any resolution.
            float trophySize = Mathf.Min(width * 0.29f, height * 0.58f);
            float wordmarkWidth = width * 0.30f;
            float gap = width * 0.035f;
            float trophyCenterX = 0.47f;
            float wordmarkCenterX = trophyCenterX +
                (trophySize * 0.5f + gap + wordmarkWidth * 0.5f) / width;

            SetWinElementLayout(winNewGameBanner, new Vector2(0.15f, 0.77f),
                new Vector2(width * 0.30f, width * 0.30f * (230f / 980f)));
            SetWinElementLayout(winVictoryTrophyEmblem, new Vector2(trophyCenterX, 0.62f),
                new Vector2(trophySize, trophySize));
            SetWinElementLayout(winPuzzleCompleteWordmark, new Vector2(wordmarkCenterX, 0.70f),
                new Vector2(wordmarkWidth, wordmarkWidth * (440f / 1180f)));
            SetWinElementLayout(winBackToHomeButton, new Vector2(0.50f, 0.12f),
                new Vector2(width * 0.68f, height * 0.15f));
        }
        else
        {
            // Stack the three title/emblem sprites in a single portrait column.
            float bannerWidth = width * 0.72f;
            float wordmarkWidth = width * 0.90f;
            float trophySize = Mathf.Min(width * 0.70f, height * 0.42f);
            SetWinElementLayout(winNewGameBanner, new Vector2(0.5f, 0.93f),
                new Vector2(bannerWidth, bannerWidth * (230f / 980f)));
            SetWinElementLayout(winPuzzleCompleteWordmark, new Vector2(0.5f, 0.80f),
                new Vector2(wordmarkWidth, wordmarkWidth * (440f / 1180f)));
            SetWinElementLayout(winVictoryTrophyEmblem, new Vector2(0.5f, 0.47f),
                new Vector2(trophySize, trophySize));
            SetWinElementLayout(winBackToHomeButton, new Vector2(0.5f, 0.10f),
                new Vector2(width * 0.88f, height * 0.10f));
        }

        _hasWinLayout = true;
        _lastWinLandscape = isLandscape;
        _lastWinPanelSize = panelSize;
    }

    private Vector2 GetWinPanelSize()
    {
        RectTransform panelRect = winPanel != null ? winPanel.GetComponent<RectTransform>() : null;
        if (panelRect != null)
        {
            Vector2 size = panelRect.rect.size;
            if (size.x > 0f && size.y > 0f) return size;
        }

        return new Vector2(Screen.width, Screen.height);
    }

    private static void SetWinElementLayout(RectTransform element, Vector2 anchor, Vector2 size)
    {
        if (element == null) return;
        element.anchorMin = anchor;
        element.anchorMax = anchor;
        element.anchoredPosition = Vector2.zero;
        element.sizeDelta = size;
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
        if (!visible && _winTransition != null)
        {
            StopCoroutine(_winTransition);
            _winTransition = null;
            CancelWinTransition();
        }
        if (visible && _viewModel != null)
        {
            OnStateChanged(_viewModel.CurrentStateName.Value);
            return;
        }

        SetAllPanelsHidden();
        if (topBar != null) topBar.gameObject.SetActive(false);
        UpdateVerseTicker(false);
    }

    private void SetAllPanelsHidden()
    {
        if (hudPanel != null) hudPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (overlay != null) overlay.SetActive(false);
    }
}
