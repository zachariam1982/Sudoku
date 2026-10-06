using System.Collections.Generic;

/// <summary>Gameplay state and persistence for the independent New Game slot.</summary>
public sealed class NewGameViewModel : BaseViewModel
{
    private NewGameStateMachine NewGameMachine => (NewGameStateMachine)StateMachine;
    private bool _resultRecorded;

    public bool HasActiveGame { get; private set; }

    public NewGameViewModel(IGameUsageStats usageStats, IScorePenalties penalties)
        : base(usageStats, penalties) { }

    public override void ResetPuzzle()
    {
        _resultRecorded = false;
        HasActiveGame = true;
        base.ResetPuzzle();
    }

    public SaveGameData GetSaveData()
    {
        var data = new SaveGameData
        {
            SaveVersion = 2,
            SaveMode = "NewGame",
            GeneratorVersion = 1,
            Level = Model.CurrentLevel,
            Difficulty = (int)Model.CurrentDifficulty,
            PuzzleSeed = Model.PuzzleSeed,
            UndoUses = UsageStats.UndoUses,
            PencilUses = UsageStats.PencilUses,
            EraseUses = UsageStats.EraseUses,
            SOSUses = UsageStats.SOSUses,
            AutoFillUses = UsageStats.AutoFillUses,
            IsPencilMode = IsPencilMode.Value,
            HighlightedCandidateNumber = HighlightedCandidateNumber.Value,
            PauseRequested = PauseRequested.Value,
            IsWon = IsWon.Value,
            statename = CurrentStateName.Value
        };

        int[] originalPuzzle = Model.GetOriginalPuzzleFlat();
        int[] solution = Model.GetSolutionFlat();
        for (int i = 0; i < 81; i++)
        {
            int row = i / 9;
            int col = i % 9;
            data.BoardFlat[i] = Model.GetValue(row, col);
            data.PencilCandidateMasksFlat[i] = Model.PencilCandidateMasks[row, col];
            data.OriginalPuzzleFlat[i] = originalPuzzle[i];
            data.SolutionFlat[i] = solution[i];
        }

        AddHistoryToSave(UndoHistory, data.UndoHistory);
        AddHistoryToSave(RedoHistory, data.RedoHistory);
        return data;
    }

    public bool LoadSaveData(SaveGameData data)
    {
        if (data == null || data.SaveMode != "NewGame" || data.BoardFlat == null ||
            data.BoardFlat.Length != 81 || data.PencilCandidateMasksFlat == null ||
            data.PencilCandidateMasksFlat.Length != 81)
            return false;

        Model.SetLevel(data.Level);
        Model.SetPuzzleSeed(data.PuzzleSeed > 0 ? data.PuzzleSeed : data.Level);
        Model.SetDifficulty((SudokuDifficulty)data.Difficulty);
        if (!Model.TryLoadSavedPuzzle(data.OriginalPuzzleFlat, data.SolutionFlat))
            Model.LoadCurrentLevelPuzzle();

        for (int i = 0; i < 81; i++)
        {
            int row = i / 9;
            int col = i % 9;
            if (!Model.IsGiven(row, col))
                Model.SetValue(row, col, data.BoardFlat[i]);
        }
        Model.LoadPencilCandidateMasks(data.PencilCandidateMasksFlat);
        UsageStats.Load(data.UndoUses, data.PencilUses, data.EraseUses, data.SOSUses, data.AutoFillUses);

        UndoHistory.Clear();
        RedoHistory.Clear();
        LoadHistory(data.UndoHistory, UndoHistory);
        LoadHistory(data.RedoHistory, RedoHistory);

        PublishBoard();
        PublishPencilCandidates();
        IsBoardValid.Value = Model.Validate();
        IsComplete.Value = Model.IsComplete() && IsBoardValid.Value;
        IsPencilMode.Value = data.IsPencilMode;
        HighlightedCandidateNumber.Value = data.HighlightedCandidateNumber;
        IsWon.Value = data.IsWon;
        PauseRequested.Value = data.PauseRequested;
        IsPaused.Value = false;
        IsEraseMode.Value = false;
        IsSOSMode.Value = false;
        SelectedRow.Value = -1;
        SelectedCol.Value = -1;
        HasActiveGame = true;

        switch (data.statename)
        {
            case "NewGamePausedState":
                NewGameMachine.TransitionTo(NewGameMachine.Paused);
                break;
            case "NewGameValidatingState":
                NewGameMachine.TransitionTo(NewGameMachine.Validating);
                break;
            case "NewGameWinState":
                NewGameMachine.TransitionTo(NewGameMachine.Win);
                break;
            default:
                NewGameMachine.TransitionTo(NewGameMachine.Playing);
                UpdateConflictingCells();
                break;
        }
        RequestSave();
        return true;
    }

    public void ClearActiveGame() => HasActiveGame = false;

    public void RecordResult(bool completed)
    {
        if (_resultRecorded || !HasActiveGame) return;
        _resultRecorded = true;
        NewGameResultDatabase.RecordAttempt(new NewGameResult
        {
            Level = Model.CurrentLevel,
            Difficulty = (int)Model.CurrentDifficulty,
            PuzzleSeed = Model.PuzzleSeed,
            PencilUses = UsageStats.PencilUses,
            SOSUses = UsageStats.SOSUses,
            AutoFillUses = UsageStats.AutoFillUses,
            RecordedAt = System.DateTime.UtcNow.ToString("o")
        }, completed);
    }

    public void ReplayResult(NewGameResult result)
    {
        if (result == null) return;
        PauseRequested.Value = false;
        ResumeRequested.Value = false;
        IsPaused.Value = false;
        IsWon.Value = false;
        Model.SetLevel(result.Level);
        Model.SetDifficulty((SudokuDifficulty)result.Difficulty);
        Model.SetPuzzleSeed(result.PuzzleSeed > 0 ? result.PuzzleSeed : result.Level);
        ResetPuzzle();

        IsWon.Value = false;
        IsComplete.Value = false;
        IsBoardValid.Value = true;
        NewGameMachine.StartPlaying();
        RequestSave();
    }

    public void RestartCurrentGame()
    {
        ReplayResult(new NewGameResult
        {
            Level = Model.CurrentLevel,
            Difficulty = (int)Model.CurrentDifficulty,
            PuzzleSeed = Model.PuzzleSeed
        });
    }

    private static void AddHistoryToSave(
        Stack<SaveGameHistoryEntry> history,
        List<SaveGameHistoryEntry> savedHistory)
    {
        SaveGameHistoryEntry[] entries = history.ToArray();
        for (int i = entries.Length - 1; i >= 0; i--)
            savedHistory.Add(entries[i]);
    }

    private static void LoadHistory(
        List<SaveGameHistoryEntry> source,
        Stack<SaveGameHistoryEntry> destination)
    {
        if (source == null) return;
        foreach (SaveGameHistoryEntry entry in source)
            if (entry != null) destination.Push(entry);
    }
}
