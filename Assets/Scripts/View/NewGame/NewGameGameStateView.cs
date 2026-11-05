using System;
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

    private void Update()
    {
        if (!_gameplayVisible || _viewModel == null || winPanel == null ||
            !winPanel.activeSelf || _viewModel.CurrentStateName.Value != "NewGameWinState")
        {
            return;
        }

        bool isLandscape = Screen.width > Screen.height;
        if (_hasWinLayout && _lastWinLandscape == isLandscape) return;
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
                if (overlay != null) overlay.SetActive(true);
                ApplyWinPanelLayout(Screen.width > Screen.height);
                if (winPanel != null) winPanel.SetActive(true);
                break;
        }

        if (topBar != null) topBar.SetNewGameMode(true);
        UpdateVerseTicker(stateName == "NewGamePlayingState" || stateName == "NewGamePausedState");
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
        if (winPanelBackground != null)
        {
            Sprite background = isLandscape ? winLandscapeBackground : winPortraitBackground;
            if (background != null) winPanelBackground.sprite = background;
        }

        if (isLandscape)
        {
            SetWinElementLayout(winNewGameBanner, new Vector2(0.17f, 0.74f), new Vector2(980f, 230f));
            SetWinElementLayout(winVictoryTrophyEmblem, new Vector2(0.47f, 0.66f), new Vector2(1080f, 980f));
            SetWinElementLayout(winPuzzleCompleteWordmark, new Vector2(0.78f, 0.70f), new Vector2(1180f, 440f));
            SetWinElementLayout(winBackToHomeButton, new Vector2(0.49f, 0.16f), new Vector2(1540f, 320f));
        }
        else
        {
            // Stack the three title/emblem sprites in a single portrait column.
            SetWinElementLayout(winNewGameBanner, new Vector2(0.5f, 0.92f), new Vector2(560f, 160f));
            SetWinElementLayout(winPuzzleCompleteWordmark, new Vector2(0.5f, 0.79f), new Vector2(700f, 330f));
            SetWinElementLayout(winVictoryTrophyEmblem, new Vector2(0.5f, 0.54f), new Vector2(650f, 650f));
            SetWinElementLayout(winBackToHomeButton, new Vector2(0.5f, 0.10f), new Vector2(800f, 200f));
        }

        _hasWinLayout = true;
        _lastWinLandscape = isLandscape;
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
