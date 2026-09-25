using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// View — reacts to game state changes and shows/hides the appropriate UI panels.
/// Attach to a child of Canvas that contains the state UI panels.
/// 
/// Scene structure expected:
///   GameStateView (this script)
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
public class GameStateView : MonoBehaviour
{
    [Header("TopBar Items")]
    [SerializeField] private GameObject Lives;
    [SerializeField] private GameObject Timer;
    [SerializeField] private GameObject Settings;
    [SerializeField] private TextMeshProUGUI Level;
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
    [SerializeField] private TextMeshProUGUI winTimeLabel;
    [SerializeField] private TextMeshProUGUI winPointsLabel;

    [Header("Lose Panel")]
    [SerializeField] private GameObject      losePanel;
    [SerializeField] private TextMeshProUGUI loseMistakesLabel;

    [Header("Pause Panel")]
    [SerializeField] private GameObject      pausePanel;

    [Header("New Game Win Panel")]
    [SerializeField] private GameObject      newGameWinPanel;

    private BaseViewModel _vm;
    private HUD _hud;


    // ── Binding ───────────────────────────────────────────────────────────────

    public void Bind(BaseViewModel vm)
    {
        if (ReferenceEquals(_vm, vm)) return;
        Unbind();
        _vm = vm;

        vm.CurrentStateName.OnChanged += OnStateChanged;
        vm.IsWon.OnChanged            += OnWonChanged;
        JourneyViewModel journey = vm as JourneyViewModel;
        if (journey != null)
        {
            journey.ElapsedSeconds.OnChanged += OnTimerChanged;
            journey.LivesRemaining.OnChanged += OnLivesChanged;
            journey.IsLost.OnChanged += OnLostChanged;
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

        _vm.CurrentStateName.OnChanged -= OnStateChanged;
        _vm.IsWon.OnChanged            -= OnWonChanged;
        JourneyViewModel journey = _vm as JourneyViewModel;
        if (journey != null)
        {
            journey.ElapsedSeconds.OnChanged -= OnTimerChanged;
            journey.LivesRemaining.OnChanged -= OnLivesChanged;
            journey.IsLost.OnChanged -= OnLostChanged;
        }
        _vm = null;
    }

    // ── State Change Handlers ─────────────────────────────────────────────────

    private void OnStateChanged(string stateName)
    {
        SetAllPanelsHidden();

        switch (stateName)
        {
            case "JourneyIdleState":
                if (hudPanel != null) hudPanel.SetActive(false);
                if(_vm != null) Level.text = "        ";
                break;

            case "JourneyPlayingState":
            case "NewGamePlayingState":
                if (hudPanel != null) hudPanel.SetActive(true);
                if(_vm != null) Level.text = ((SudokuDifficulty)_vm.GetDifficulty).ToString();
                break;

            case "JourneyPausedState":
            case "NewGamePausedState":
                if(pausePanel != null)pausePanel.SetActive(true);                
                Button btn = this.pausePanel.GetComponentInChildren<Button>();
                btn.onClick.AddListener(OnPlayPressed);
                break;

            case "JourneyValidatingState":
            case "NewGameValidatingState":
                if(hudPanel != null ) hudPanel.SetActive(false);
                break;

            case "JourneyWinState":
                if (winPanel != null)
                {
                    if (overlay != null) overlay.SetActive(true);
                    winPanel.SetActive(true);
                    UpdateWinPanel();
                }
                break;

            case "JourneyLoseState":
                if (overlay != null) overlay.SetActive(true);
                if (losePanel != null) losePanel.SetActive(true);
                break;

            case "NewGameWinState":
                if (overlay != null) overlay.SetActive(true);
                if (newGameWinPanel != null) newGameWinPanel.SetActive(true);
                break;
        }

        ApplyModeVisibility(stateName);
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

    // ── Stars ─────────────────────────────────────────────────────────────────

    private void OnStarRatingChanged(int stars)
    {

    }

    // ── Win / Lose Panels ─────────────────────────────────────────────────────

    private void OnWonChanged(bool isWon)
    {
        if (!(_vm is JourneyViewModel)) return;
        if (overlay != null) overlay.SetActive(isWon);
        if (winPanel != null) winPanel.SetActive(isWon);
        if(isWon == false) return;
        JourneyViewModel journey = _vm as JourneyViewModel;
        if (journey == null) return;
        if (winTimeLabel != null)
        {
            int minutes = (int)(journey.ElapsedSeconds.Value / 60f);
            int secs    = (int)(journey.ElapsedSeconds.Value % 60f);
            winTimeLabel.text = $"{minutes:00}:{secs:00}";
        }
        if (winPointsLabel != null)
        {
            var total = ScoringSystem.Calculate(
                (SudokuDifficulty)_vm.GetDifficulty,
                journey.ElapsedSeconds.Value,
                journey.Penalties);
            winPointsLabel.text = $"Points: {total}/200";
        }
    }

    private void OnLostChanged(bool isLost)
    {
        if (overlay != null) overlay.SetActive(isLost);
        if (losePanel != null) losePanel.SetActive(isLost);
    }

    private void UpdateWinPanel()
    {

    }

    public void OnNewGamePressed() 
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        if (overlay != null) overlay.SetActive(false);
        (_vm as JourneyViewModel)?.NewGameCommand.Execute();
    }
    public void OnRetryPressed()    
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        if (overlay != null) overlay.SetActive(false);
        (_vm as JourneyViewModel)?.RetryCommand.Execute();
    }
    public void OnPlayPressed()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        _vm?.ResumeCommand.Execute();
    }
    public void OnResumePressed()   => _vm?.ResumeCommand.Execute();

    public void OnNewGameWinAcknowledged()
    {
        if (newGameWinPanel != null) newGameWinPanel.SetActive(false);
        if (overlay != null) overlay.SetActive(false);
        _vm?.NotifyGameCompleted();
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private void SetAllPanelsHidden()
    {
        if (hudPanel   != null) hudPanel.SetActive(false);
        if (winPanel   != null) winPanel.SetActive(false);
        if (losePanel  != null) losePanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (newGameWinPanel != null) newGameWinPanel.SetActive(false);
        if (overlay    != null) overlay.SetActive(false);
    }

    private void ApplyModeVisibility(string stateName)
    {
        bool journeyMode = _vm is JourneyViewModel;
        bool hideStatus = stateName == "JourneyValidatingState" ||
                          stateName == "NewGameValidatingState" ||
                          stateName == "JourneyWinState" ||
                          stateName == "NewGameWinState" ||
                          stateName == "JourneyLoseState";

        if (Lives != null) Lives.SetActive(journeyMode && !hideStatus);
        if (Timer != null) Timer.SetActive(journeyMode && !hideStatus);
        if (Settings != null) Settings.SetActive(journeyMode);
        if (winTimeLabel != null) winTimeLabel.gameObject.SetActive(journeyMode);
        if (winPointsLabel != null) winPointsLabel.gameObject.SetActive(journeyMode);
    }
}
