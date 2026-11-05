using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>State-driven panels and top bar for Journey mode only.</summary>
public sealed class JourneyGameStateView : MonoBehaviour
{
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
    [SerializeField] private GameObject losePanel;
    [SerializeField] private GameObject pausePanel;

    private JourneyViewModel _viewModel;
    private HUD _hud;
    private VerseTicker _verseTicker;
    private Button _pauseResumeButton;
    private bool _gameplayVisible;

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

        _pauseResumeButton = pausePanel != null
            ? pausePanel.GetComponentInChildren<Button>(true)
            : null;
        if (_pauseResumeButton != null)
            _pauseResumeButton.onClick.AddListener(OnPlayPressed);

        _viewModel.CurrentStateName.OnChanged += OnStateChanged;
        _viewModel.IsWon.OnChanged += OnWonChanged;
        _viewModel.ElapsedSeconds.OnChanged += OnTimerChanged;
        _viewModel.LivesRemaining.OnChanged += OnLivesChanged;
        _viewModel.IsLost.OnChanged += OnLostChanged;
        _hud = journeyHudPanel != null ? journeyHudPanel.GetComponent<HUD>() : null;
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
        _viewModel.IsWon.OnChanged -= OnWonChanged;
        _viewModel.ElapsedSeconds.OnChanged -= OnTimerChanged;
        _viewModel.LivesRemaining.OnChanged -= OnLivesChanged;
        _viewModel.IsLost.OnChanged -= OnLostChanged;
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

        SetAllPanelsHidden();
        switch (stateName)
        {
            case "JourneyIdleState":
                SetLevelText(string.Empty);
                break;
            case "JourneyPlayingState":
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
                if (overlay != null) overlay.SetActive(true);
                if (winPanel != null) winPanel.SetActive(true);
                UpdateWinPanel();
                break;
            case "JourneyLoseState":
                if (overlay != null) overlay.SetActive(true);
                if (losePanel != null) losePanel.SetActive(true);
                break;
        }

        ApplyTopBarVisibility(stateName);
        UpdateVerseTicker(stateName == "JourneyPlayingState" || stateName == "JourneyPausedState");
    }

    private void ApplyTopBarVisibility(string stateName)
    {
        bool hideStatus = stateName == "JourneyValidatingState" ||
                          stateName == "JourneyWinState" ||
                          stateName == "JourneyLoseState";
        if (Lives != null) Lives.SetActive(!hideStatus);
        if (Timer != null) Timer.SetActive(!hideStatus);
        if (Settings != null) Settings.SetActive(true);
        if (journeyTopBar != null) journeyTopBar.SetNewGameMode(false);
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

    private void OnWonChanged(bool isWon)
    {
        if (!_gameplayVisible) return;
        if (overlay != null) overlay.SetActive(isWon);
        if (winPanel != null) winPanel.SetActive(isWon);
        if (isWon) UpdateWinPanel();
    }

    private void OnLostChanged(bool isLost)
    {
        if (!_gameplayVisible) return;
        if (overlay != null) overlay.SetActive(isLost);
        if (losePanel != null) losePanel.SetActive(isLost);
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

        SetAllPanelsHidden();
        if (journeyTopBar != null) journeyTopBar.gameObject.SetActive(false);
        UpdateVerseTicker(false);
    }

    private void HideResultPanels()
    {
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
