using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>Owns the dated puzzle and gameplay-only state for Daily Challenge.</summary>
public sealed class DailyChallengeViewModel : BaseViewModel
{
    private DailyChallengeStateMachine DailyMachine => (DailyChallengeStateMachine)StateMachine;
    private int _dateSeed;

    public DailyChallengeRecord ChallengeRecord { get; private set; }
    public bool HasActiveChallenge { get; private set; }

    public BindableProperty<float> ElapsedSeconds { get; } = new BindableProperty<float>(0f);
    public BindableProperty<int> LivesRemaining { get; } = new BindableProperty<int>(3);
    public BindableProperty<bool> IsTimerRunning { get; } = new BindableProperty<bool>(false);
    public BindableProperty<bool> IsLost { get; } = new BindableProperty<bool>(false);
    public ICommand NewGameCommand { get; }
    public ICommand RetryCommand { get; }

    public DailyChallengeViewModel(IGameUsageStats usageStats, IScorePenalties penalties)
        : base(usageStats, penalties)
    {
        NewGameCommand = new RelayCommand(_ => NotifyGameCompleted());
        RetryCommand = new RelayCommand(_ =>
            ShowMessage.Value = ("Challenge already attempted",
                "Today's challenge is already attempted.", ""));
    }

    public void Prepare(DailyChallengeRecord record)
    {
        if (record == null) throw new ArgumentNullException(nameof(record));
        ChallengeRecord = record;
        // Suppress bindable-property autosaves until the puzzle is fully prepared.
        HasActiveChallenge = false;
        _dateSeed = DailyChallengeRepository.GetPuzzleSeed(record.CurrentDate);
        SetGameLevelAndDifficulty(_dateSeed, (SudokuDifficulty)record.Difficulty);
        Model.SetPuzzleSeed(_dateSeed);
        ElapsedSeconds.Value = 0f;
        LivesRemaining.Value = 3;
        IsLost.Value = false;
        IsWon.Value = false;
        PauseRequested.Value = false;
        ResumeRequested.Value = false;
        ResetPuzzle();
        DailyMachine.StartChallenge();
        HasActiveChallenge = true;
        RequestSave();
    }

    public SaveGameData GetSaveData()
    {
        if (!HasActiveChallenge || ChallengeRecord == null)
            return null;

        var data = new SaveGameData
        {
            SaveVersion = 2,
            SaveMode = "DailyChallenge",
            GeneratorVersion = 1,
            DailyChallengeRecordId = ChallengeRecord.Id,
            DailyChallengeDate = ChallengeRecord.CurrentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
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
            UndoUses = UsageStats.UndoUses,
            PencilUses = UsageStats.PencilUses,
            EraseUses = UsageStats.EraseUses,
            SOSUses = UsageStats.SOSUses,
            AutoFillUses = UsageStats.AutoFillUses
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

    public bool LoadSaveData(SaveGameData data, DailyChallengeRecord record)
    {
        if (data == null || record == null || data.SaveMode != "DailyChallenge" ||
            data.DailyChallengeRecordId != record.Id ||
            data.DailyChallengeDate != record.CurrentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ||
            data.BoardFlat == null || data.BoardFlat.Length != 81 ||
            data.PencilCandidateMasksFlat == null || data.PencilCandidateMasksFlat.Length != 81)
            return false;

        ChallengeRecord = record;
        HasActiveChallenge = true;
        _dateSeed = DailyChallengeRepository.GetPuzzleSeed(record.CurrentDate);
        Model.SetLevel(_dateSeed);
        Model.SetDifficulty((SudokuDifficulty)record.Difficulty);
        Model.SetPuzzleSeed(_dateSeed);
        if (!Model.TryLoadSavedPuzzle(data.OriginalPuzzleFlat, data.SolutionFlat))
            return false;

        for (int i = 0; i < 81; i++)
        {
            int row = i / 9;
            int col = i % 9;
            if (!Model.IsGiven(row, col))
                Model.SetValue(row, col, data.BoardFlat[i]);
        }
        Model.LoadPencilCandidateMasks(data.PencilCandidateMasksFlat);

        ElapsedSeconds.Value = data.ElapsedSeconds;
        LivesRemaining.Value = data.LivesRemaining;
        IsLost.Value = data.IsLost;
        IsWon.Value = data.IsWon;
        IsPencilMode.Value = data.IsPencilMode;
        HighlightedCandidateNumber.Value = data.HighlightedCandidateNumber;
        PauseRequested.Value = false;
        ResumeRequested.Value = false;
        IsPaused.Value = false;
        IsEraseMode.Value = false;
        IsSOSMode.Value = false;
        IsValidating.Value = false;
        ClosePicker();

        Penalties.Load(data.Mistakes, data.SOSEmptyCells, data.SOSWrongCells);
        UsageStats.Load(data.UndoUses, data.PencilUses, data.EraseUses, data.SOSUses, data.AutoFillUses);

        UndoHistory.Clear();
        RedoHistory.Clear();
        LoadHistory(data.UndoHistory, UndoHistory);
        LoadHistory(data.RedoHistory, RedoHistory);

        PublishBoard();
        PublishPencilCandidates();
        IsBoardValid.Value = Model.Validate();
        IsComplete.Value = Model.IsComplete() && IsBoardValid.Value;
        UpdateConflictingCells();

        string stateName = data.statename ?? string.Empty;
        switch (stateName)
        {
            case "DailyChallengeIdleState":
                DailyMachine.TransitionTo(DailyMachine.Idle);
                FirstCellTapped.Value = false;
                break;
            case "DailyChallengePausedState":
                DailyMachine.TransitionTo(DailyMachine.Paused);
                FirstCellTapped.Value = true;
                break;
            case "DailyChallengeValidatingState":
                DailyMachine.TransitionTo(DailyMachine.Validating);
                FirstCellTapped.Value = true;
                break;
            default:
                DailyMachine.TransitionTo(DailyMachine.Playing);
                FirstCellTapped.Value = true;
                break;
        }

        RequestSave();
        return true;
    }

    public override void ResetPuzzle()
    {
        if (_dateSeed <= 0) return;
        Model.SetPuzzleSeed(_dateSeed);
        base.ResetPuzzle();
        ElapsedSeconds.Value = 0f;
        LivesRemaining.Value = 3;
        IsLost.Value = false;
        IsWon.Value = false;
    }

    protected override void OnConflict()
    {
        LivesRemaining.Value = Mathf.Max(0, LivesRemaining.Value - 1);
    }

    public void Finish(bool won)
    {
        if (ChallengeRecord == null) return;

        if (!string.IsNullOrEmpty(ChallengeRecord.CompletedAt))
        {
            HasActiveChallenge = false;
            SaveSystem.Delete(SaveSlot.DailyChallenge);
            return;
        }

        ChallengeRecord.IsWon = won;
        ChallengeRecord.ElapsedSeconds = ElapsedSeconds.Value;
        ChallengeRecord.LivesRemaining = LivesRemaining.Value;
        ChallengeRecord.Stars = won ? 3 : 0;
        ChallengeRecord.CompletedAt = DateTime.Now.ToString("o");
        ChallengeRecord.UndoUses = UsageStats.UndoUses;
        ChallengeRecord.PencilUses = UsageStats.PencilUses;
        ChallengeRecord.EraseUses = UsageStats.EraseUses;
        ChallengeRecord.SOSUses = UsageStats.SOSUses;
        ChallengeRecord.AutofillUses = UsageStats.AutoFillUses;
        try
        {
            DailyChallengeRepository.Update(ChallengeRecord);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DailyChallenge] Could not save the completed challenge: {ex}");
        }
        finally
        {
            HasActiveChallenge = false;
            SaveSystem.Delete(SaveSlot.DailyChallenge);
        }
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
