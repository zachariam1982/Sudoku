using System;
using System.Globalization;
using UnityEngine;

/// <summary>Creates and connects the independent Journey and New Game flows.</summary>
public class GameContext : MonoBehaviour
{
    public JourneyViewModel ViewModel => JourneyViewModel;
    public JourneyViewModel JourneyViewModel { get; private set; }
    public NewGameViewModel NewGameViewModel { get; private set; }
    public DailyChallengeViewModel DailyChallengeViewModel { get; private set; }
    public static int cnt = 0;

    [SerializeField] private HomeScreenController _homeScreen;
    [SerializeField] private NewGameScreenController _newGameScreen;
    [SerializeField] private JourneyStateMachine _journeyStateMachine;
    [SerializeField] private NewGameStateMachine _newGameStateMachine;
    [SerializeField] private DailyChallengeStateMachine _dailyChallengeStateMachine;
    [SerializeField] private DailyChallengeScreenController _dailyChallengeScreen;

    private SudokuGrid _grid;
    private GridBuilder _gridBuilder;
    private ErrorMessage _error;
    private NumberPicker _picker;
    private GameStateView _stateView;
    private StatsPanel _statsPanel;
    private NewGameStatsPanel _newGameStatsPanel;
    private DailyChallengeGameStateView _dailyChallengeStateView;
    private bool _dailyChallengeActive;
    private bool _dailyChallengeScreenRequested;
    private bool _newGameActive;

    private void Awake()
    {
        JourneyViewModel = new JourneyViewModel(
            new JourneyUsageStats(),
            new JourneyScorePenalties());
        NewGameViewModel = new NewGameViewModel(
            new NewGameUsageStats(),
            new NewGameScorePenalties());
        DailyChallengeViewModel = new DailyChallengeViewModel(
            new JourneyUsageStats(),
            new JourneyScorePenalties());

        _grid = GetComponent<SudokuGrid>();
        _gridBuilder = GetComponent<GridBuilder>();
        _error = GetComponent<ErrorMessage>();
        _picker = GetComponentInChildren<NumberPicker>();
        _stateView = GetComponentInChildren<GameStateView>(true);
        _dailyChallengeStateView = GetComponentInChildren<DailyChallengeGameStateView>(true);
        if (_dailyChallengeScreen == null)
            _dailyChallengeScreen = GetComponentInChildren<DailyChallengeScreenController>(true);

        _statsPanel = GetComponentInChildren<StatsPanel>(true);
        _newGameStatsPanel = GetComponentInChildren<NewGameStatsPanel>(true);
        BindGameplay(JourneyViewModel);
        _statsPanel?.Bind(JourneyViewModel);
        _newGameStatsPanel?.Bind(NewGameViewModel);

        User.Instance.ViewModel = JourneyViewModel;
        User.Instance.NewGameViewModel = NewGameViewModel;
        User.Instance.DailyChallengeViewModel = DailyChallengeViewModel;
        NewGameViewModel.SaveRequested += User.Instance.SaveNewGameNow;
        DailyChallengeViewModel.SaveRequested += User.Instance.SaveDailyChallengeNow;
        _journeyStateMachine.Initialise(JourneyViewModel);
        _newGameStateMachine.Initialise(NewGameViewModel);
        if (_dailyChallengeStateMachine == null)
            _dailyChallengeStateMachine = GetComponent<DailyChallengeStateMachine>();
        if (_dailyChallengeStateMachine == null)
            _dailyChallengeStateMachine = gameObject.AddComponent<DailyChallengeStateMachine>();
        _dailyChallengeStateMachine.Initialise(DailyChallengeViewModel);
        DailyChallengeViewModel.CurrentStateName.OnChanged += OnDailyChallengeStateChanged;
        _dailyChallengeStateView?.Bind(DailyChallengeViewModel);
        _dailyChallengeStateMachine.SetSuspended(true);
        _newGameStateMachine.SetSuspended(true);
        NewGameViewModel.GameCompleted += OnNewGameCompleted;
        DailyChallengeViewModel.GameCompleted += ExitDailyChallenge;

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
        _dailyChallengeStateView?.SetGameplayVisible(false);
        StartDailyChallengeBackgroundJobs();
        // Startup is the mode selector, not an active Journey session.
        _gridBuilder?.SetVisible(false);
        _stateView?.SetGameplayVisible(false);
    }

    public void ActivateJourney()
    {
        _dailyChallengeStateMachine.SetSuspended(true);
        _dailyChallengeActive = false;
        _newGameStateMachine.SetSuspended(true);
        _newGameActive = false;
        BindGameplay(JourneyViewModel);
        _journeyStateMachine.SetSuspended(false);
        _gridBuilder?.SetVisible(true);
        _stateView?.SetGameplayVisible(true);
    }

    public void ActivateNewGame(SudokuDifficulty difficulty)
    {
        _dailyChallengeStateMachine.SetSuspended(true);
        _dailyChallengeActive = false;
        _journeyStateMachine.SetSuspended(true);
        _newGameActive = true;
        BindGameplay(NewGameViewModel);
        NewGameViewModel.StartRandomGame(difficulty);
        _newGameStateMachine.SetSuspended(false);
        _gridBuilder?.SetVisible(true);
        _stateView?.SetGameplayVisible(true);
    }

    private void StartDailyChallengeBackgroundJobs()
    {
        StartCoroutine(EnsureDailyChallengeForToday());
        StartCoroutine(DeleteUnattemptedDailyChallenges());
    }

    public bool HasSavedNewGame => NewGameViewModel != null && NewGameViewModel.HasActiveGame;

    public void ActivateSavedNewGame()
    {
        if (!HasSavedNewGame) return;
        _dailyChallengeStateMachine.SetSuspended(true);
        _dailyChallengeActive = false;
        _journeyStateMachine.SetSuspended(true);
        _newGameActive = true;
        BindGameplay(NewGameViewModel);
        _newGameStateMachine.SetSuspended(false);
        _gridBuilder?.SetVisible(true);
        _stateView?.SetGameplayVisible(true);
    }

    public void ExitNewGame()
    {
        if (!_newGameActive || NewGameViewModel == null) return;

        NewGameViewModel.RecordResult(false);
        _newGameStateMachine.SetSuspended(true);
        _newGameActive = false;
        NewGameViewModel.ClearActiveGame();
        SaveSystem.Delete(SaveSlot.NewGame);
        _gridBuilder?.SetVisible(false);
        _stateView?.SetGameplayVisible(false);
        _newGameScreen.Show();
    }

    public void SuspendActiveGame()
    {
        if (_dailyChallengeActive)
        {
            User.Instance?.SaveDailyChallengeNow();
            _dailyChallengeStateMachine.SetSuspended(true);
            _dailyChallengeScreenRequested = false;
            _dailyChallengeStateMachine.TransitionTo(_dailyChallengeStateMachine.Idle);
        }
        else if (_newGameActive)
        {
            _newGameStateMachine.SetSuspended(true);
            _stateView?.SetTopBarMode(false);
        }
        else _journeyStateMachine.SetSuspended(true);

        _gridBuilder?.SetVisible(false);
        _stateView?.SetGameplayVisible(false);
        _dailyChallengeStateView?.SetGameplayVisible(false);
    }

    private void OnNewGameCompleted()
    {
        NewGameViewModel.RecordResult(true);
        _newGameStateMachine.SetSuspended(true);
        _newGameActive = false;
        NewGameViewModel.ClearActiveGame();
        SaveSystem.Delete(SaveSlot.NewGame);
        _gridBuilder?.SetVisible(false);
        _stateView?.SetGameplayVisible(false);
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

        _dailyChallengeStateMachine.SetSuspended(true);
        _dailyChallengeActive = false;
        if (_newGameActive && NewGameViewModel != null && NewGameViewModel.HasActiveGame)
            NewGameViewModel.RecordResult(false);

        _journeyStateMachine.SetSuspended(true);
        _newGameActive = true;
        BindGameplay(NewGameViewModel);
        if (_newGameScreen != null) _newGameScreen.gameObject.SetActive(false);
        NewGameViewModel.ReplayResult(result);
        _newGameStateMachine.SetSuspended(false);
        _gridBuilder?.SetVisible(true);
        _stateView?.SetGameplayVisible(true);
    }

    public void OpenDailyChallengeScreen(DailyChallengeScreenController screen)
    {
        if (screen != null)
            _dailyChallengeScreen = screen;

        if (_dailyChallengeScreen == null ||
            _dailyChallengeStateMachine == null ||
            !_dailyChallengeStateMachine.IsIdle)
        {
            _dailyChallengeScreen?.Hide();
            return;
        }

        _dailyChallengeScreenRequested = true;
        _dailyChallengeScreen.SetLandscape(Screen.width > Screen.height);
        _dailyChallengeScreen.Show();
    }

    public void CloseDailyChallengeScreen()
    {
        _dailyChallengeScreenRequested = false;
        _dailyChallengeScreen?.Hide();
    }

    private void OnDailyChallengeStateChanged(string stateName)
    {
        if (stateName != "DailyChallengeIdleState")
        {
            _dailyChallengeScreenRequested = false;
            _dailyChallengeScreen?.Hide();
            return;
        }

        if (_dailyChallengeScreenRequested)
            _dailyChallengeScreen?.Show();
    }

    public void StartDailyChallenge(DailyChallengeScreenController screen = null)
    {
        if (screen != null)
            _dailyChallengeScreen = screen;
        StartCoroutine(StartDailyChallengeRoutine());
    }

    private System.Collections.IEnumerator StartDailyChallengeRoutine()
    {
        DailyChallengeRecord record = null;
        yield return StartCoroutine(EnsureDailyChallengeForToday(created => record = created));
        if (record == null)
        {
            DailyChallengeViewModel.ShowMessage.Value = ("Daily Challenge unavailable", "Today's challenge could not be loaded. Please try again.", "");
            _error?.Bind(DailyChallengeViewModel);
            yield break;
        }
        if (!string.IsNullOrEmpty(record.CompletedAt))
        {
            SaveSystem.Delete(SaveSlot.DailyChallenge);
            DailyChallengeViewModel.ShowMessage.Value = ("Challenge already attempted", "Today's challenge is already attempted.", "");
            _error?.Bind(DailyChallengeViewModel);
            yield break;
        }
        SaveGameData savedChallenge = SaveSystem.Load(SaveSlot.DailyChallenge);
        bool resumeSavedChallenge = savedChallenge != null &&
            savedChallenge.SaveMode == "DailyChallenge" &&
            savedChallenge.DailyChallengeRecordId == record.Id &&
            savedChallenge.DailyChallengeDate == record.CurrentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (savedChallenge != null && !resumeSavedChallenge)
            SaveSystem.Delete(SaveSlot.DailyChallenge);

        DailyChallengeRepository.MarkAttempted(record);
        _dailyChallengeScreenRequested = false;
        _dailyChallengeScreen?.Hide();
        _dailyChallengeActive = true;
        _newGameActive = false;
        _journeyStateMachine.SetSuspended(true);
        _newGameStateMachine.SetSuspended(true);
        BindDailyGameplay(DailyChallengeViewModel);
        DailyChallengeViewModel.Prepare(record);
        if (resumeSavedChallenge && !DailyChallengeViewModel.LoadSaveData(savedChallenge, record))
        {
            SaveSystem.Delete(SaveSlot.DailyChallenge);
            DailyChallengeViewModel.Prepare(record);
        }
        _dailyChallengeStateMachine.SetSuspended(false);
        _gridBuilder?.SetVisible(true);
        _stateView?.SetGameplayVisible(false);
        _dailyChallengeStateView?.SetGameplayVisible(true);
    }

    public void ExitDailyChallenge()
    {
        if (!_dailyChallengeActive) return;
        User.Instance?.SaveDailyChallengeNow();
        _dailyChallengeStateMachine.SetSuspended(true);
        _dailyChallengeActive = false;
        _gridBuilder?.SetVisible(false);
        _dailyChallengeScreenRequested = true;
        _dailyChallengeStateMachine.StartChallenge();
        _dailyChallengeStateView?.SetGameplayVisible(false);
        _dailyChallengeScreen?.Show();
        BindGameplay(JourneyViewModel);
        _error?.Bind(JourneyViewModel);
    }

    private System.Collections.IEnumerator EnsureDailyChallengeForToday(
        Action<DailyChallengeRecord> onReady = null)
    {
        yield return null;
        DateTime today = DateTime.Today;
        DailyChallengeRepository.FinalizeUncompletedPreviousChallenges(today);
        int difficultyCount = System.Enum.GetValues(typeof(SudokuDifficulty)).Length;
        int difficulty = difficultyCount == 0 ? 0 : (today.DayOfYear - 1) % difficultyCount;
        DailyChallengeRecord record = DailyChallengeRepository.GetOrCreate(today, difficulty);
        onReady?.Invoke(record);
    }

    private System.Collections.IEnumerator DeleteUnattemptedDailyChallenges()
    {
        yield return null;
        var interval = new WaitForSeconds(60f);
        while (true)
        {
            DailyChallengeRepository.DeleteUnattemptedBefore(DateTime.Today);
            yield return interval;
        }
    }

    private void BindDailyGameplay(BaseViewModel viewModel)
    {
        _grid?.Bind(viewModel);
        _error?.Bind(viewModel);
        _picker?.Bind(viewModel);
        _dailyChallengeStateView?.Bind(viewModel);
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
        if (DailyChallengeViewModel != null)
        {
            DailyChallengeViewModel.CurrentStateName.OnChanged -= OnDailyChallengeStateChanged;
            DailyChallengeViewModel.GameCompleted -= ExitDailyChallenge;
            if (User.Instance != null)
                DailyChallengeViewModel.SaveRequested -= User.Instance.SaveDailyChallengeNow;
        }
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
