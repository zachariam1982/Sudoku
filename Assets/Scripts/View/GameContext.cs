using System;
using System.Globalization;
using UnityEngine;

/// <summary>Creates and connects the independent gameplay modes.</summary>
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
    private JourneyGameStateView _journeyStateView;
    private NewGameGameStateView _newGameStateView;
    private StatsPanel _statsPanel;
    private NewGameStatsPanel _newGameStatsPanel;
    private DailyChallengeStatsPanel _dailyChallengeStatsPanel;
    private DailyChallengeGameStateView _dailyChallengeStateView;
    private bool _dailyChallengeActive;
    private bool _newGameActive;

    private void Awake()
    {
        _grid = GetComponent<SudokuGrid>();
        _gridBuilder = GetComponent<GridBuilder>();
        _error = GetComponent<ErrorMessage>();
        _picker = GetComponentInChildren<NumberPicker>();
        _journeyStateView = GetComponentInChildren<JourneyGameStateView>(true);
        _newGameStateView = GetComponentInChildren<NewGameGameStateView>(true);
        _dailyChallengeStateView = GetComponentInChildren<DailyChallengeGameStateView>(true);
        if (_dailyChallengeScreen == null)
            _dailyChallengeScreen = GetComponentInChildren<DailyChallengeScreenController>(true);

        _statsPanel = GetComponentInChildren<StatsPanel>(true);
        _newGameStatsPanel = GetComponentInChildren<NewGameStatsPanel>(true);
        _dailyChallengeStatsPanel = GetComponentInChildren<DailyChallengeStatsPanel>(true);
        if (_dailyChallengeStateMachine == null)
            _dailyChallengeStateMachine = GetComponent<DailyChallengeStateMachine>();
        if (_dailyChallengeStateMachine == null)
            _dailyChallengeStateMachine = gameObject.AddComponent<DailyChallengeStateMachine>();

        ConfigureVariant(User.Instance.VariantFactory);
        _dailyChallengeStateView?.SetGameplayVisible(false);
        StartDailyChallengeBackgroundJobs();
        // Startup is the mode selector, not an active Journey session.
        _gridBuilder?.SetVisible(false);
        _journeyStateView?.SetGameplayVisible(false);
        _newGameStateView?.SetGameplayVisible(false);
    }

    public bool SelectVariant(GameVariant variant)
    {
        if (User.Instance == null)
        {
            Debug.LogError("Cannot select a puzzle variant before User has initialized.", this);
            return false;
        }

        if (User.Instance.VariantFactory != null &&
            User.Instance.VariantFactory.Variant == variant)
            return true;

        _journeyStateMachine.SetSuspended(true);
        _newGameStateMachine.SetSuspended(true);
        _dailyChallengeStateMachine.SetSuspended(true);
        _gridBuilder?.SetVisible(false);
        _journeyStateView?.SetGameplayVisible(false);
        _newGameStateView?.SetGameplayVisible(false);
        _dailyChallengeStateView?.SetGameplayVisible(false);

        // Persist through the currently selected variant before replacing its
        // view models and save system with the destination variant's services.
        User.Instance.SaveNow();
        _newGameActive = false;
        _dailyChallengeActive = false;

        IGameVariantFactory factory = User.Instance.SelectVariant(variant);
        if (factory == null) return false;

        ConfigureVariant(factory);
        return true;
    }

    private void ConfigureVariant(IGameVariantFactory factory)
    {
        _grid?.SetDiagonalVariantActive(factory.Variant == GameVariant.Diagonal);
        _grid?.SetKillerVariantActive(factory.Variant == GameVariant.Killer);
        _grid?.SetThermoVariantActive(factory.Variant == GameVariant.Thermo);
        _grid?.SetHyperVariantActive(factory.Variant == GameVariant.Hyper);
        _grid?.SetIrregularVariantActive(factory.Variant == GameVariant.Irregular);

        if (NewGameViewModel != null)
        {
            NewGameViewModel.SaveRequested -= User.Instance.SaveNewGameNow;
            NewGameViewModel.GameCompleted -= OnNewGameCompleted;
        }
        if (DailyChallengeViewModel != null)
        {
            DailyChallengeViewModel.SaveRequested -= User.Instance.SaveDailyChallengeNow;
            DailyChallengeViewModel.GameCompleted -= ExitDailyChallenge;
        }

        IGameHistoryRepository historyRepository = factory.GameHistoryRepository;
        JourneyViewModel = new JourneyViewModel(
            new JourneyUsageStats(), new JourneyScorePenalties(),
            factory.CreateSudokuModel(), historyRepository);
        NewGameViewModel = new NewGameViewModel(
            new NewGameUsageStats(), new NewGameScorePenalties(),
            factory.CreateSudokuModel(), factory.GameResultRepository);
        DailyChallengeViewModel = new DailyChallengeViewModel(
            new JourneyUsageStats(), new JourneyScorePenalties(),
            factory.CreateSudokuModel(), factory.DailyChallengeRepository,
            factory.GameSaveSystem);

        User.Instance.ViewModel = JourneyViewModel;
        User.Instance.NewGameViewModel = NewGameViewModel;
        User.Instance.DailyChallengeViewModel = DailyChallengeViewModel;
        NewGameViewModel.SaveRequested += User.Instance.SaveNewGameNow;
        DailyChallengeViewModel.SaveRequested += User.Instance.SaveDailyChallengeNow;
        NewGameViewModel.GameCompleted += OnNewGameCompleted;
        DailyChallengeViewModel.GameCompleted += ExitDailyChallenge;

        _journeyStateMachine.Initialise(JourneyViewModel);
        _newGameStateMachine.Initialise(NewGameViewModel);
        _dailyChallengeStateMachine.Initialise(DailyChallengeViewModel);

        _journeyStateMachine.SetSuspended(true);
        _newGameStateMachine.SetSuspended(true);
        _dailyChallengeStateMachine.SetSuspended(true);

        BindGameplay(JourneyViewModel);
        _dailyChallengeStateView?.Bind(DailyChallengeViewModel);
        _statsPanel?.Bind(JourneyViewModel);
        _newGameStatsPanel?.Bind(NewGameViewModel);
        _dailyChallengeStatsPanel?.Bind(DailyChallengeViewModel);

        User.Instance.TryLoadSave();
        User.Instance.TryLoadNewGameSave();
        _homeScreen?.Initialize(JourneyViewModel, this);
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
        _dailyChallengeStateView?.SetGameplayVisible(false);
        _newGameStateView?.SetGameplayVisible(false);
        _journeyStateView?.SetGameplayVisible(true);
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
        _dailyChallengeStateView?.SetGameplayVisible(false);
        _journeyStateView?.SetGameplayVisible(false);
        _newGameStateView?.SetGameplayVisible(true);
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
        _dailyChallengeStateView?.SetGameplayVisible(false);
        _journeyStateView?.SetGameplayVisible(false);
        _newGameStateView?.SetGameplayVisible(true);
    }

    public void ExitNewGame()
    {
        if (!_newGameActive || NewGameViewModel == null) return;

        NewGameViewModel.RecordResult(false);
        _newGameStateMachine.SetSuspended(true);
        _newGameActive = false;
        NewGameViewModel.ClearActiveGame();
        User.Instance.GameSaveSystem.Delete(SaveSlot.NewGame);
        _gridBuilder?.SetVisible(false);
        _journeyStateView?.SetGameplayVisible(false);
        _newGameStateView?.SetGameplayVisible(false);
        _newGameScreen.Show();
    }

    public void SuspendActiveGame()
    {
        if (_dailyChallengeActive)
        {
            User.Instance?.SaveDailyChallengeNow();
            _dailyChallengeStateMachine.SetSuspended(true);
        }
        else if (_newGameActive)
        {
            _newGameStateMachine.SetSuspended(true);
        }
        else _journeyStateMachine.SetSuspended(true);

        _gridBuilder?.SetVisible(false);
        _journeyStateView?.SetGameplayVisible(false);
        _newGameStateView?.SetGameplayVisible(false);
        _dailyChallengeStateView?.SetGameplayVisible(false);
    }

    private void OnNewGameCompleted()
    {
        NewGameViewModel.RecordResult(true);
        _newGameStateMachine.SetSuspended(true);
        _newGameActive = false;
        NewGameViewModel.ClearActiveGame();
        User.Instance.GameSaveSystem.Delete(SaveSlot.NewGame);
        _gridBuilder?.SetVisible(false);
        _journeyStateView?.SetGameplayVisible(false);
        _newGameStateView?.SetGameplayVisible(false);
        _newGameScreen.Show();
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
        _dailyChallengeStateView?.SetGameplayVisible(false);
        _journeyStateView?.SetGameplayVisible(false);
        _newGameStateView?.SetGameplayVisible(true);
    }

    public void RefreshDailyChallengeScreen(DailyChallengeScreenController screen)
    {
        if (screen != null)
            _dailyChallengeScreen = screen;

        if (_dailyChallengeScreen != null)
            StartCoroutine(RefreshDailyChallengeScreenRoutine(_dailyChallengeScreen));
    }

    private System.Collections.IEnumerator RefreshDailyChallengeScreenRoutine(
        DailyChallengeScreenController screen)
    {
        DailyChallengeRecord today = null;
        yield return StartCoroutine(EnsureDailyChallengeForToday(record => today = record));
        if (today == null || screen == null)
            yield break;

        screen.SetChallengeData(
            today,
            DailyChallengeViewModel.GetChallengeStats(),
            DailyChallengeViewModel.GetTotalChallengeCount());
    }

    public void OpenDailyChallengeScreen(DailyChallengeScreenController screen)
    {
        if (screen != null)
            _dailyChallengeScreen = screen;

        if (_dailyChallengeScreen == null)
            return;

        // The ready page is standalone. Keep every gameplay view hidden here.
        _gridBuilder?.SetVisible(false);
        _journeyStateView?.SetGameplayVisible(false);
        _newGameStateView?.SetGameplayVisible(false);
        _dailyChallengeStateView?.SetGameplayVisible(false);
        _dailyChallengeScreen.SetLandscape(Screen.width > Screen.height);
        _dailyChallengeScreen.Show();
    }

    public void CloseDailyChallengeScreen()
    {
        _dailyChallengeScreen?.Hide();
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
            User.Instance.GameSaveSystem.Delete(SaveSlot.DailyChallenge);
            DailyChallengeViewModel.ShowMessage.Value = ("Challenge already attempted", "Today's challenge is already attempted.", "");
            _error?.Bind(DailyChallengeViewModel);
            yield break;
        }
        SaveGameData savedChallenge = User.Instance.GameSaveSystem.Load(SaveSlot.DailyChallenge);
        bool resumeSavedChallenge = savedChallenge != null &&
            savedChallenge.SaveMode == "DailyChallenge" &&
            savedChallenge.DailyChallengeRecordId == record.Id &&
            savedChallenge.DailyChallengeDate == record.CurrentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (savedChallenge != null && !resumeSavedChallenge)
            User.Instance.GameSaveSystem.Delete(SaveSlot.DailyChallenge);

        DailyChallengeViewModel.MarkChallengeAttempted(record);
        _dailyChallengeScreen?.Hide();
        _dailyChallengeActive = true;
        _newGameActive = false;
        _journeyStateMachine.SetSuspended(true);
        _newGameStateMachine.SetSuspended(true);
        BindDailyGameplay(DailyChallengeViewModel);
        bool restoredSavedChallenge = resumeSavedChallenge &&
            DailyChallengeViewModel.LoadSaveData(savedChallenge, record);
        if (resumeSavedChallenge && !restoredSavedChallenge)
        {
            User.Instance.GameSaveSystem.Delete(SaveSlot.DailyChallenge);
        }
        // Prepare generates a Sudoku puzzle. Only call it for a new challenge
        // (or if a saved challenge is invalid); a valid save already contains
        // the original puzzle, solution, and current board state.
        if (!restoredSavedChallenge)
            DailyChallengeViewModel.Prepare(record);
        _dailyChallengeStateMachine.SetSuspended(false);
        _gridBuilder?.SetVisible(true);
        _journeyStateView?.SetGameplayVisible(false);
        _newGameStateView?.SetGameplayVisible(false);
        _dailyChallengeStateView?.SetGameplayVisible(true);
    }

    public void ExitDailyChallenge()
    {
        if (!_dailyChallengeActive) return;
        User.Instance?.SaveDailyChallengeNow();
        _dailyChallengeStateMachine.SetSuspended(true);
        _dailyChallengeActive = false;
        _gridBuilder?.SetVisible(false);
        _dailyChallengeStateView?.SetGameplayVisible(false);
        BindGameplay(JourneyViewModel);
        _error?.Bind(JourneyViewModel);
        OpenDailyChallengeScreen(_dailyChallengeScreen);
    }

    private System.Collections.IEnumerator EnsureDailyChallengeForToday(
        Action<DailyChallengeRecord> onReady = null)
    {
        yield return null;
        DateTime today = DateTime.Today;
        DailyChallengeViewModel.FinalizeExpiredChallenges(today);
        int difficultyCount = System.Enum.GetValues(typeof(SudokuDifficulty)).Length;
        if (difficultyCount == 0)
        {
            Debug.LogError("Daily Challenge cannot be created because no Sudoku difficulties are defined.");
            yield break;
        }

        int journeyDifficulty = Mathf.Clamp(JourneyViewModel.GetDifficulty, 0, difficultyCount - 1);
        int minimumDailyDifficulty = Mathf.Max(0, journeyDifficulty - 2);
        int dailyDifficultyCount = journeyDifficulty - minimumDailyDifficulty + 1;
        int difficulty = minimumDailyDifficulty + ((today.DayOfYear - 1) % dailyDifficultyCount);
        DailyChallengeRecord record = DailyChallengeViewModel.GetOrCreateChallenge(today, difficulty);
        onReady?.Invoke(record);
    }

    private System.Collections.IEnumerator DeleteUnattemptedDailyChallenges()
    {
        yield return null;
        var interval = new WaitForSeconds(60f);
        while (true)
        {
            DailyChallengeViewModel.DeleteOldUnattemptedChallenges(DateTime.Today);
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
        if (viewModel is NewGameViewModel newGameViewModel)
            _newGameStateView?.Bind(newGameViewModel);
        else if (viewModel is JourneyViewModel journeyViewModel)
            _journeyStateView?.Bind(journeyViewModel);
    }

    private void OnDestroy()
    {
        if (DailyChallengeViewModel != null)
        {
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

    private System.Collections.IEnumerator Start()
    {
        yield return null;
        _homeScreen?.RefreshJourneyStatus();
    }
}
