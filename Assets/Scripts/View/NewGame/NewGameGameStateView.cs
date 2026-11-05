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
    [SerializeField] private GameObject landscapeWinPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private NewGameTopBar topBar;

    private NewGameViewModel _viewModel;
    private HUD _hud;
    private VerseTicker _verseTicker;
    private OrientationManager _orientationManager;
    private Button _pauseResumeButton;
    private bool _gameplayVisible;
    private bool _landscapeOrientationLocked;

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
        RestoreAutomaticOrientation();
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
            RestoreAutomaticOrientation();
            if (hudPanel != null) hudPanel.SetActive(false);
            if (winPanel != null) winPanel.SetActive(false);
            if (landscapeWinPanel != null) landscapeWinPanel.SetActive(false);
            if (topBar != null) topBar.gameObject.SetActive(false);
            UpdateVerseTicker(false);
            return;
        }

        if (stateName != "NewGameWinState")
            RestoreAutomaticOrientation();

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
                ShowLandscapeWinPanel();
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
        RestoreAutomaticOrientation();
        if (winPanel != null) winPanel.SetActive(false);
        if (landscapeWinPanel != null) landscapeWinPanel.SetActive(false);
        if (overlay != null) overlay.SetActive(false);
        _viewModel?.NotifyGameCompleted();
    }

    private void ShowLandscapeWinPanel()
    {
        if (landscapeWinPanel == null)
        {
            if (winPanel != null) winPanel.SetActive(true);
            return;
        }

        LockToLandscapeOrientation();
        if (winPanel != null) winPanel.SetActive(false);
        landscapeWinPanel.SetActive(true);
    }

    private void LockToLandscapeOrientation()
    {
        if (_landscapeOrientationLocked) return;

        if (_orientationManager == null)
            _orientationManager = GetComponentInParent<OrientationManager>();

        if (_orientationManager != null)
            _orientationManager.LockToLandscape();
        else
        {
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;
        }

        _landscapeOrientationLocked = true;
    }

    private void RestoreAutomaticOrientation()
    {
        if (!_landscapeOrientationLocked) return;

        if (_orientationManager == null)
            _orientationManager = GetComponentInParent<OrientationManager>();

        if (_orientationManager != null)
            _orientationManager.RestoreAutoRotation();
        else
        {
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = true;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;
        }

        _landscapeOrientationLocked = false;
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

        RestoreAutomaticOrientation();
        SetAllPanelsHidden();
        if (topBar != null) topBar.gameObject.SetActive(false);
        UpdateVerseTicker(false);
    }

    private void SetAllPanelsHidden()
    {
        if (hudPanel != null) hudPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
        if (landscapeWinPanel != null) landscapeWinPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (overlay != null) overlay.SetActive(false);
    }
}
