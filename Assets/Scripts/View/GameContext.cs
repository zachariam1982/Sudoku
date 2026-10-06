using UnityEngine;

/// <summary>Creates and connects the independent Journey and New Game flows.</summary>
public class GameContext : MonoBehaviour
{
    public JourneyViewModel ViewModel => JourneyViewModel;
    public JourneyViewModel JourneyViewModel { get; private set; }
    public NewGameViewModel NewGameViewModel { get; private set; }
    public static int cnt = 0;

    [SerializeField] private HomeScreenController _homeScreen;
    [SerializeField] private NewGameScreenController _newGameScreen;
    [SerializeField] private JourneyStateMachine _journeyStateMachine;
    [SerializeField] private NewGameStateMachine _newGameStateMachine;

    private SudokuGrid _grid;
    private ErrorMessage _error;
    private NumberPicker _picker;
    private GameStateView _stateView;
    private StatsPanel _statsPanel;
    private NewGameStatsPanel _newGameStatsPanel;
    private bool _newGameActive;

    private void Awake()
    {
        JourneyViewModel = new JourneyViewModel(
            new JourneyUsageStats(),
            new JourneyScorePenalties());
        NewGameViewModel = new NewGameViewModel(
            new NewGameUsageStats(),
            new NewGameScorePenalties());

        _grid = GetComponent<SudokuGrid>();
        _error = GetComponent<ErrorMessage>();
        _picker = GetComponentInChildren<NumberPicker>();
        _stateView = GetComponentInChildren<GameStateView>();

        _statsPanel = GetComponentInChildren<StatsPanel>(true);
        _newGameStatsPanel = GetComponentInChildren<NewGameStatsPanel>(true);
        BindGameplay(JourneyViewModel);
        _statsPanel?.Bind(JourneyViewModel);
        _newGameStatsPanel?.Bind(NewGameViewModel);

        User.Instance.ViewModel = JourneyViewModel;
        User.Instance.NewGameViewModel = NewGameViewModel;
        NewGameViewModel.SaveRequested += User.Instance.SaveNewGameNow;
        _journeyStateMachine.Initialise(JourneyViewModel);
        _newGameStateMachine.Initialise(NewGameViewModel);
        _newGameStateMachine.SetSuspended(true);
        NewGameViewModel.GameCompleted += OnNewGameCompleted;

#if UNITY_WEBGL && !UNITY_EDITOR
        User.Instance.TryLoadSave();
        User.Instance.TryLoadNewGameSave();
        if (YouTubePlatformManager.Instance != null)
            YouTubePlatformManager.Instance.SendGameReady();
#else
        User.Instance.TryLoadSave();
        User.Instance.TryLoadNewGameSave();
#endif

        _journeyStateMachine.SetSuspended(true);
        _homeScreen.Initialize(JourneyViewModel, this);
    }

    public void ActivateJourney()
    {
        _newGameStateMachine.SetSuspended(true);
        _newGameActive = false;
        BindGameplay(JourneyViewModel);
        _journeyStateMachine.SetSuspended(false);
    }

    public void ActivateNewGame(SudokuDifficulty difficulty)
    {
        _journeyStateMachine.SetSuspended(true);
        _newGameActive = true;
        BindGameplay(NewGameViewModel);
        NewGameViewModel.StartRandomGame(difficulty);
        _newGameStateMachine.SetSuspended(false);
    }

    public bool HasSavedNewGame => NewGameViewModel != null && NewGameViewModel.HasActiveGame;

    public void ActivateSavedNewGame()
    {
        if (!HasSavedNewGame) return;
        _journeyStateMachine.SetSuspended(true);
        _newGameActive = true;
        BindGameplay(NewGameViewModel);
        _newGameStateMachine.SetSuspended(false);
    }

    public void ExitNewGame()
    {
        if (!_newGameActive || NewGameViewModel == null) return;

        NewGameViewModel.RecordResult(false);
        _newGameStateMachine.SetSuspended(true);
        _newGameActive = false;
        NewGameViewModel.ClearActiveGame();
        SaveSystem.Delete(SaveSlot.NewGame);
        _stateView?.SetTopBarMode(false);
        _newGameScreen.Show();
    }

    public void SuspendActiveGame()
    {
        if (_newGameActive)
        {
            _newGameStateMachine.SetSuspended(true);
            _stateView?.SetTopBarMode(false);
        }
        else _journeyStateMachine.SetSuspended(true);
    }

    private void OnNewGameCompleted()
    {
        NewGameViewModel.RecordResult(true);
        _newGameStateMachine.SetSuspended(true);
        _newGameActive = false;
        NewGameViewModel.ClearActiveGame();
        SaveSystem.Delete(SaveSlot.NewGame);
        _stateView?.SetTopBarMode(false);
        _newGameScreen.Show();
    }

    public void RetryLatestNewGameResult()
    {
        NewGameResult latest = NewGameResultDatabase.GetLatest();
        if (latest != null)
            RetryNewGameResult(latest);
        else if (NewGameViewModel != null && NewGameViewModel.HasActiveGame)
            NewGameViewModel.RestartCurrentGame();
    }

    public void RetryNewGameResult(NewGameResult result)
    {
        if (result == null) return;

        if (_newGameActive && NewGameViewModel != null && NewGameViewModel.HasActiveGame)
            NewGameViewModel.RecordResult(false);

        _journeyStateMachine.SetSuspended(true);
        _newGameActive = true;
        BindGameplay(NewGameViewModel);
        if (_newGameScreen != null) _newGameScreen.gameObject.SetActive(false);
        NewGameViewModel.ReplayResult(result);
        _newGameStateMachine.SetSuspended(false);
    }

    private void BindGameplay(BaseViewModel viewModel)
    {
        _grid?.Bind(viewModel);
        _error?.Bind(viewModel);
        _picker?.Bind(viewModel);
        _stateView?.Bind(viewModel);
    }

    private void OnDestroy()
    {
        if (NewGameViewModel != null)
        {
            NewGameViewModel.GameCompleted -= OnNewGameCompleted;
            if (User.Instance != null)
                NewGameViewModel.SaveRequested -= User.Instance.SaveNewGameNow;
        }
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    private System.Collections.IEnumerator Start()
    {
        yield return null;
        User.Instance.TryLoadSaveFromCloud(() =>
        {
            Debug.Log("[YouTube] Initial cloud synchronization completed.");
            _homeScreen?.RefreshJourneyStatus();
        });
    }
#endif
}
