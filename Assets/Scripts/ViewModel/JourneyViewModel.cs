using System;
using System.Collections.Generic;

/// <summary>
/// Full progression view model. Journey-only concerns such as scoring, lives,
/// elapsed time, usage statistics, history, and persistence live here.
/// </summary>
public class JourneyViewModel : BaseViewModel
{
    private int _historyOffset;
    private JourneyStateMachine JourneyMachine => (JourneyStateMachine)StateMachine;

    public override bool IsJourney => true;
    public (int id, int level, int difficulty, int points) RetryGameData { get; set; } =
        (-1, -1, -1, -1);

    public BindableProperty<float> ElapsedSeconds { get; } = new BindableProperty<float>(0f);
    public BindableProperty<int> LivesRemaining { get; } = new BindableProperty<int>(3);
    public BindableProperty<bool> IsTimerRunning { get; } = new BindableProperty<bool>(false);
    public BindableProperty<bool> IsLost { get; } = new BindableProperty<bool>(false);
    public BindableProperty<bool> NewGameRequested { get; } = new BindableProperty<bool>(false);
    public BindableProperty<bool> RetryGameRequested { get; } = new BindableProperty<bool>(false);
    public BindableProperty<bool> RetryOlderGameRequested { get; } = new BindableProperty<bool>(false);
    public BindableProperty<List<GameRecord>> PastHistory { get; } =
        new BindableProperty<List<GameRecord>>(new List<GameRecord>());

    public ICommand NewGameCommand { get; }
    public ICommand RetryCommand { get; }
    public ICommand RetryOlderGameCommand { get; }
    public ICommand NextLevel { get; }
    public ICommand IncreaseDifficulty { get; }
    public ICommand DecreaseDifficulty { get; }
    public ICommand FetchHistoricalData { get; }
    public ICommand ResetHistoricalData { get; }

    public JourneyViewModel(
        IGameUsageStats usageStats,
        IScorePenalties penalties) : base(usageStats, penalties)
    {
        NewGameCommand = new RelayCommand(_ => NewGameRequested.Value = true);
        RetryCommand = new RelayCommand(_ => RetryGameRequested.Value = true);
        RetryOlderGameCommand = new RelayCommand(param =>
        {
            RetryGameData = ((int, int, int, int))param;
            RetryOlderGameRequested.Value = true;
        });
        NextLevel = new RelayCommand(_ =>
        {
            int level = GameDatabase.GetLastRecord()?.Level ?? Model.CurrentLevel;
            Model.SetLevel(level + 1);
        });
        IncreaseDifficulty = new RelayCommand(_ => Model.increaseDifficulty());
        DecreaseDifficulty = new RelayCommand(_ => Model.decreaseDifficulty());
        FetchHistoricalData = new RelayCommand(_ => FetchData());
        ResetHistoricalData = new RelayCommand(_ =>
        {
            _historyOffset = 0;
            PastHistory.Value.Clear();
        });
    }

    protected override void OnConflict()
    {
        LivesRemaining.Value--;
    }

    public override void ResetPuzzle()
    {
        base.ResetPuzzle();
        _historyOffset = 0;
        PastHistory.Value.Clear();
        ElapsedSeconds.Value = 0f;
    }

    public bool RecoverFromHistory()
    {
        GameRecord last = GameDatabase.GetLastRecord();
        if (last == null) return false;

        Model.SetLevel(last.Level + 1);
        Model.SetDifficulty((SudokuDifficulty)last.Difficulty);
        Model.UpdateDifficultyFromHistory();
        RetryGameData = (-1, -1, -1, -1);
        RetryOlderGameRequested.Value = false;
        ResetPuzzle();
        return true;
    }

    public ValueTuple<int, int, int> getPreviousValues()
    {
        return ReplacedValueStack.Count == 0
            ? new ValueTuple<int, int, int>(-1, -1, -1)
            : ReplacedValueStack.Pop();
    }

    public SaveGameData GetSaveData()
    {
        var data = new SaveGameData
        {
            Level = Model.CurrentLevel,
            Difficulty = (int)Model.CurrentDifficulty,
            PuzzleSeed = Model.PuzzleSeed,
            ElapsedSeconds = ElapsedSeconds.Value,
            LivesRemaining = LivesRemaining.Value,
            IsWon = IsWon.Value,
            IsLost = IsLost.Value,
            PauseRequested = PauseRequested.Value,
            IsPencilMode = IsPencilMode.Value,
            HighlightedCandidateNumber = HighlightedCandidateNumber.Value,
            statename = CurrentStateName.Value,
            Mistakes = Penalties.Mistakes,
            SOSEmptyCells = Penalties.SOSEmptyCells,
            SOSWrongCells = Penalties.SOSWrongCells,
            RetryOlderGame = RetryOlderGameRequested.Value,
            RetryOlderGame_Id = RetryGameData.id,
            RetryOlderGame_Level = RetryGameData.level,
            RetryOlderGame_Difficulty = RetryGameData.difficulty,
            UndoUses = UsageStats.UndoUses,
            PencilUses = UsageStats.PencilUses,
            EraseUses = UsageStats.EraseUses,
            SOSUses = UsageStats.SOSUses,
            AutoFillUses = UsageStats.AutoFillUses,
            RetryOlderGame_Points = RetryGameData.points
        };

        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
            {
                data.BoardFlat[row * 9 + col] = Model.GetValue(row, col);
                data.PencilCandidateMasksFlat[row * 9 + col] =
                    Model.PencilCandidateMasks[row, col];
            }

        var stackArray = ReplacedValueStack.ToArray();
        for (int i = stackArray.Length - 1; i >= 0; i--)
        {
            var value = stackArray[i];
            data.UndoStack.Add($"{value.Item1},{value.Item2},{value.Item3}");
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        data.GameHistory = GameDatabase.ExportHistory();
#endif
        return data;
    }

    public void LoadSaveData(SaveGameData data)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        GameDatabase.ImportHistory(data.GameHistory);
#endif
        Model.SetLevel(data.Level);
        Model.SetPuzzleSeed(data.PuzzleSeed > 0 ? data.PuzzleSeed : data.Level);
        Model.SetDifficulty((SudokuDifficulty)data.Difficulty);
        Model.LoadCurrentLevelPuzzle();

        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
                if (!Model.IsGiven(row, col))
                    Model.SetValue(row, col, data.BoardFlat[row * 9 + col]);

        Model.LoadPencilCandidateMasks(data.PencilCandidateMasksFlat);

        ElapsedSeconds.Value = data.ElapsedSeconds;
        LivesRemaining.Value = data.LivesRemaining;
        ReplacedValueStack.Clear();
        foreach (string entry in data.UndoStack)
        {
            string[] parts = entry.Split(',');
            if (parts.Length == 3 &&
                int.TryParse(parts[0], out int row) &&
                int.TryParse(parts[1], out int col) &&
                int.TryParse(parts[2], out int value))
            {
                ReplacedValueStack.Push((row, col, value));
            }
        }

        PublishBoard();
        PublishPencilCandidates();
        IsBoardValid.Value = Model.Validate();
        IsComplete.Value = Model.IsComplete() && IsBoardValid.Value;
        IsWon.Value = data.IsWon;
        IsLost.Value = data.IsLost;
        PauseRequested.Value = data.PauseRequested;
        IsPencilMode.Value = data.IsPencilMode;
        HighlightedCandidateNumber.Value = data.HighlightedCandidateNumber;
        Penalties.Load(data.Mistakes, data.SOSEmptyCells, data.SOSWrongCells);
        RetryOlderGameRequested.Value = data.RetryOlderGame;
        RetryGameData = (
            data.RetryOlderGame_Id,
            data.RetryOlderGame_Level,
            data.RetryOlderGame_Difficulty,
            data.RetryOlderGame_Points);
        UsageStats.Load(
            data.UndoUses,
            data.PencilUses,
            data.EraseUses,
            data.SOSUses,
            data.AutoFillUses);

        switch (data.statename)
        {
            case "IdleState":
            case "JourneyIdleState":
                JourneyMachine.TransitionTo(JourneyMachine.Idle);
                break;
            case "PlayingState":
            case "JourneyPlayingState":
                JourneyMachine.TransitionTo(JourneyMachine.Playing);
                UpdateConflictingCells();
                break;
            case "PausedState":
            case "JourneyPausedState":
                JourneyMachine.TransitionTo(JourneyMachine.Paused);
                break;
            case "ValidatingState":
            case "JourneyValidatingState":
                JourneyMachine.TransitionTo(JourneyMachine.Validating);
                break;
            case "WinState":
            case "JourneyWinState":
                JourneyMachine.TransitionTo(JourneyMachine.Win);
                break;
            case "LoseState":
            case "JourneyLoseState":
                JourneyMachine.TransitionTo(JourneyMachine.Lose);
                break;
        }
    }

    private void FetchData()
    {
        List<GameRecord> records = GameDatabase.GetNextSet(_historyOffset);
        if (records.Count == 0) return;
        PastHistory.Value = records;
        _historyOffset += 10;
    }
}
