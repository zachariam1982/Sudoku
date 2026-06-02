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
    [SerializeField] private TextMeshProUGUI winMistakesLabel;

    [Header("Lose Panel")]
    [SerializeField] private GameObject      losePanel;
    [SerializeField] private TextMeshProUGUI loseMistakesLabel;

    [Header("Pause Panel")]
    [SerializeField] private GameObject      pausePanel;

    private SudokuViewModel _vm;

    // ── Binding ───────────────────────────────────────────────────────────────

    public void Bind(SudokuViewModel vm)
    {
        _vm = vm;

        vm.CurrentStateName.OnChanged += OnStateChanged;
        vm.ElapsedSeconds.OnChanged   += OnTimerChanged;
        vm.LivesRemaining.OnChanged   += OnLivesChanged;
        vm.IsWon.OnChanged            += OnWonChanged;
        vm.IsLost.OnChanged           += OnLostChanged;
        vm.IsPaused.OnChanged         += OnPausedChanged;

        // Start with all panels hidden
        SetAllPanelsHidden();
    }

    private void OnDestroy()
    {
        if (_vm == null) return;

        _vm.CurrentStateName.OnChanged -= OnStateChanged;
        _vm.ElapsedSeconds.OnChanged   -= OnTimerChanged;
        _vm.LivesRemaining.OnChanged   -= OnLivesChanged;
        _vm.IsWon.OnChanged            -= OnWonChanged;
        _vm.IsLost.OnChanged           -= OnLostChanged;
        _vm.IsPaused.OnChanged         -= OnPausedChanged;
    }

    // ── State Change Handlers ─────────────────────────────────────────────────

    private void OnStateChanged(string stateName)
    {
        SetAllPanelsHidden();

        switch (stateName)
        {
            case "IdleState":
                hudPanel?.SetActive(false);
                hudPanel?.GetComponent<HUD>()?.Bind(_vm);
                Lives?.SetActive(true);
                Timer?.SetActive(true);
                break;

            case "PlayingState":
                hudPanel?.SetActive(true);
                break;

            case "PausedState":
                hudPanel?.SetActive(true);
                pausePanel?.SetActive(true);
                break;

            case "ValidatingState":
                hudPanel?.SetActive(false);
                Lives?.SetActive(false);
                Timer?.SetActive(false);
                break;

            case "WinState":
                if (winPanel != null)
                {
                    winPanel.SetActive(true);
                    UpdateWinPanel();
                }
                break;

            case "LoseState":
                Lives?.SetActive(false);
                Timer?.SetActive(false);
                losePanel?.SetActive(true);
                break;
        }
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
        if (overlay != null) overlay.SetActive(isWon);
        if (winPanel != null) winPanel.SetActive(isWon);
        if (isWon) UpdateWinPanel();
    }

    private void OnLostChanged(bool isLost)
    {
        if (overlay != null) overlay.SetActive(isLost);
        if (losePanel != null) losePanel.SetActive(isLost);
    }

    private void OnPausedChanged(bool isPaused)
    {
        if (pausePanel != null) pausePanel.SetActive(isPaused);
    }

    private void UpdateWinPanel()
    {
        if (winTimeLabel != null)
        {
            int minutes = (int)(_vm.ElapsedSeconds.Value / 60f);
            int secs    = (int)(_vm.ElapsedSeconds.Value % 60f);
            winTimeLabel.text = $"Time: {minutes:00}:{secs:00}";
        }
    }

    public void OnNewGamePressed() 
    {
        winPanel?.SetActive(false);
        losePanel?.SetActive(false);
        overlay?.SetActive(false);
        _vm?.NewGameCommand.Execute();
    }
    public void OnRetryPressed()    
    {
        winPanel?.SetActive(false);
        losePanel?.SetActive(false);
        overlay?.SetActive(false);
        _vm?.RetryCommand.Execute();
    }
    public void OnPausePressed()
    { 
        _vm?.PauseCommand.Execute();
    }
    public void OnResumePressed()   => _vm?.ResumeCommand.Execute();

    // ── Helper ────────────────────────────────────────────────────────────────

    private void SetAllPanelsHidden()
    {
        if (hudPanel   != null) hudPanel.SetActive(false);
        if (winPanel   != null) winPanel.SetActive(false);
        if (losePanel  != null) losePanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
    }
}