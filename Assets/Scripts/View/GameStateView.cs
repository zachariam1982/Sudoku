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

    private SudokuViewModel _vm;
    private HUD _hud;


    // ── Binding ───────────────────────────────────────────────────────────────

    public void Bind(SudokuViewModel vm)
    {
        _vm = vm;

        vm.CurrentStateName.OnChanged += OnStateChanged;
        vm.ElapsedSeconds.OnChanged   += OnTimerChanged;
        vm.LivesRemaining.OnChanged   += OnLivesChanged;
        vm.IsWon.OnChanged            += OnWonChanged;
        vm.IsLost.OnChanged           += OnLostChanged;
        _hud = hudPanel != null
            ? hudPanel.GetComponent<HUD>()
            : null;

        _hud?.Bind(vm);

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
    }

    // ── State Change Handlers ─────────────────────────────────────────────────

    private void OnStateChanged(string stateName)
    {
        SetAllPanelsHidden();

        switch (stateName)
        {
            case "IdleState":
                if (hudPanel != null) hudPanel.SetActive(false);
                if (Lives != null) Lives.SetActive(true);
                if (Timer != null) Timer.SetActive(true);
                break;

            case "PlayingState":
                if (hudPanel != null) hudPanel.SetActive(true);
                if(_vm != null) Level.text = ((SudokuDifficulty)_vm.GetDifficulty).ToString();
                break;

            case "PausedState":
                if(pausePanel != null)pausePanel.SetActive(true);                
                Button btn = this.pausePanel.GetComponentInChildren<Button>();
                btn.onClick.AddListener(OnPlayPressed);
                break;

            case "ValidatingState":
                if(hudPanel != null ) hudPanel.SetActive(false);
                if (Lives != null) Lives.SetActive(false);
                if (Timer != null) Timer.SetActive(false);
                break;

            case "WinState":
                if (winPanel != null)
                {
                    winPanel.SetActive(true);
                    UpdateWinPanel();
                }
                break;

            case "LoseState":
                if (Lives != null) Lives.SetActive(false);
                if (Timer != null) Timer.SetActive(false);
                if (losePanel != null) losePanel.SetActive(true);
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
        if(isWon == false) return;
        if (winTimeLabel != null)
        {
            int minutes = (int)(_vm.ElapsedSeconds.Value / 60f);
            int secs    = (int)(_vm.ElapsedSeconds.Value % 60f);
            winTimeLabel.text = $"{minutes:00}:{secs:00}";
        }
        if (winPointsLabel != null)
        {
            var total = ScoringSystem.Calculate( (SudokuDifficulty)_vm.GetDifficulty, _vm.ElapsedSeconds.Value, _vm.Penalties);
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
        _vm?.NewGameCommand.Execute();
    }
    public void OnRetryPressed()    
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        if (overlay != null) overlay.SetActive(false);
        _vm?.RetryCommand.Execute();
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
    }
}