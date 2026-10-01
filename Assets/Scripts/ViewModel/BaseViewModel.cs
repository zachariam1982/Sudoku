using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared Sudoku play behaviour. This class deliberately contains no journey
/// scoring, lives, elapsed time, usage statistics, history, or persistence.
/// </summary>
public class BaseViewModel
{
    protected readonly SudokuModel Model = new SudokuModel();
    protected readonly Stack<SaveGameHistoryEntry> UndoHistory =
        new Stack<SaveGameHistoryEntry>();
    protected readonly Stack<SaveGameHistoryEntry> RedoHistory =
        new Stack<SaveGameHistoryEntry>();

    private bool _demoMode;
    protected ISudokuStateMachine StateMachine { get; private set; }

    public virtual bool IsJourney => false;
    public IGameUsageStats UsageStats { get; }
    public IScorePenalties Penalties { get; }
    public int GetLevel => Model.CurrentLevel;
    public int GetDifficulty => (int)Model.CurrentDifficulty;

    public BindableProperty<bool> HideHUD { get; } = new BindableProperty<bool>(false);
    public BindableProperty<int[,]> BoardValues { get; } = new BindableProperty<int[,]>();
    public BindableProperty<bool[,]> GivenMask { get; } = new BindableProperty<bool[,]>();
    public BindableProperty<int[,]> PencilCandidateMasks { get; } =
        new BindableProperty<int[,]>();
    public BindableProperty<int> SelectedRow { get; } = new BindableProperty<int>(-1);
    public BindableProperty<int> SelectedCol { get; } = new BindableProperty<int>(-1);
    public BindableProperty<int> SelectedDigit { get; } = new BindableProperty<int>(0);
    public BindableProperty<bool> IsPickerOpen { get; } = new BindableProperty<bool>(false);
    public BindableProperty<bool> IsBoardValid { get; } = new BindableProperty<bool>(true);
    public BindableProperty<bool> IsComplete { get; } = new BindableProperty<bool>(false);
    public BindableProperty<bool> IsEraseMode { get; } = new BindableProperty<bool>(false);
    public BindableProperty<bool> IsPencilMode { get; } = new BindableProperty<bool>(false);
    public BindableProperty<bool> IsSOSMode { get; } = new BindableProperty<bool>(false);
    public BindableProperty<bool> IsPaused { get; } = new BindableProperty<bool>(false);
    public BindableProperty<bool> IsValidating { get; } = new BindableProperty<bool>(false);
    public BindableProperty<bool> IsWon { get; } = new BindableProperty<bool>(false);
    public BindableProperty<bool> FirstCellTapped { get; } = new BindableProperty<bool>(false);
    public BindableProperty<bool> PauseRequested { get; } = new BindableProperty<bool>(false);
    public BindableProperty<bool> ResumeRequested { get; } = new BindableProperty<bool>(false);
    public BindableProperty<int> HighlightedCandidateNumber { get; } = new BindableProperty<int>(0);
    public BindableProperty<string> CurrentStateName { get; } = new BindableProperty<string>("");
    public BindableProperty<(int row, int col, bool hasConflict)> LastEnteredCell { get; } =
        new BindableProperty<(int, int, bool)>();
    public BindableProperty<HashSet<(int row, int col)>> ConflictingCells { get; } =
        new BindableProperty<HashSet<(int, int)>>(new HashSet<(int, int)>());
    public BindableProperty<object> SelectedCellTransform { get; } = new BindableProperty<object>();
    public BindableProperty<List<(int row, int col, int number)>> SOSChangedCells { get; } =
        new BindableProperty<List<(int, int, int)>>(new List<(int, int, int)>());
    public BindableProperty<(string title, string message, string status)> ShowMessage { get; } =
        new BindableProperty<(string, string, string)>(("", "", ""));

    public ICommand SelectCellCommand { get; }
    public ICommand EnterValueCommand { get; }
    public ICommand CancelPickerCommand { get; }
    public ICommand SetEraseModeCommand { get; }
    public ICommand SetPencilModeCommand { get; }
    public ICommand UndoCommand { get; }
    public ICommand RedoCommand { get; }
    public ICommand SOSCommand { get; }
    public ICommand AutoFillCandidatesCommand { get; }
    public ICommand TogglePencilCandidateCommand { get; }
    public ICommand ApplySOSCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand ResumeCommand { get; }

    public event Action GameCompleted;

    public BaseViewModel(IGameUsageStats usageStats, IScorePenalties penalties)
    {
        UsageStats = usageStats ?? throw new ArgumentNullException(nameof(usageStats));
        Penalties = penalties ?? throw new ArgumentNullException(nameof(penalties));

        SelectCellCommand = new RelayCommand(param =>
        {
            var cell = (ValueTuple<int, int, object>)param;
            SelectCell((cell.Item1, cell.Item2, cell.Item3));
        });
        EnterValueCommand = new RelayCommand(param => EnterValue((int)param));
        CancelPickerCommand = new RelayCommand(_ => ClosePicker());
        SetEraseModeCommand = new RelayCommand(
            _ =>
            {
                UsageStats.AddErase();
                EnterValue(0);
            },
            _ => !IsPencilMode.Value && IsSelectedCellFilled(),
            new (Func<bool> fn, Action showMessage)[]
            {
                (() => IsPencilMode.Value, () => ShowMessage.Value = ("", "Pencil mode is set. Tap on Pencil again to enable Erase.", "")),
                (() => !IsSelectedCellFilled(), () => ShowMessage.Value = ("", "Select a filled cell before using Erase.", ""))
            });
        SetPencilModeCommand = new RelayCommand(
            _ =>
            {
                IsPencilMode.Value = !IsPencilMode.Value;
                if (!IsPencilMode.Value) HighlightedCandidateNumber.Value = 0;
                else UsageStats.AddPencil();
            },
            _ => !IsEraseMode.Value,
            new (Func<bool> fn, Action showMessage)[]
            {
                (() => IsEraseMode.Value, () => ShowMessage.Value = ("", "Erase mode is set. Tap on Erase again to enable Pencil mode.", ""))
            });
        UndoCommand = new RelayCommand(_ => Undo());
        RedoCommand = new RelayCommand(_ => Redo());
        SOSCommand = new RelayCommand(
            _ => IsSOSMode.Value = !IsSOSMode.Value,
            _ => !IsPencilMode.Value && !IsEraseMode.Value && IsSelectedCellEmpty(),
            new (Func<bool> fn, Action showMessage)[]
            {
                (() => IsPencilMode.Value, () => ShowMessage.Value = ("", "Pencil mode is set. Tap on Pencil again to enable SOS.", "")),
                (() => IsEraseMode.Value, () => ShowMessage.Value = ("", "Erase mode is set. Tap on Erase again to enable SOS.", "")),
                (() => !IsSelectedCellEmpty(), () => ShowMessage.Value = ("", "Select an empty cell before using SOS.", ""))
            });
        AutoFillCandidatesCommand = new RelayCommand(
            _ =>
            {
                UsageStats.AddAutoFill();
                if (!IsPencilMode.Value) IsPencilMode.Value = true;
                ClosePicker();
                int[] previousMasks = CopyPencilCandidateMasks();
                Model.AutoFillPencilCandidates();
                RecordHistory(-1, -1, 0, 0, previousMasks);
                PublishPencilCandidates();
                if (!FirstCellTapped.Value) FirstCellTapped.Value = true;
            },
            _ => !IsEraseMode.Value && !IsComplete.Value,
            new (Func<bool> fn, Action showMessage)[]
            {
                (() => IsEraseMode.Value, () => ShowMessage.Value = ("", "Erase mode is set. Turn off Erase before using Auto Candidates.", "")),
                (() => IsComplete.Value, () => ShowMessage.Value = ("", "The puzzle is already complete.", ""))
            });
        TogglePencilCandidateCommand = new RelayCommand(param =>
        {
            int row = SelectedRow.Value;
            int col = SelectedCol.Value;
            if (row < 0 || col < 0) return;

            if (Model.IsGiven(row, col)) return;

            int number = (int)param;
            int[] previousMasks = CopyPencilCandidateMasks();
            Model.TogglePencilCandidate(row, col, number);
            RecordHistory(-1, -1, 0, 0, previousMasks);
            HighlightedCandidateNumber.Value = number;
            PublishPencilCandidates();
        });
        ApplySOSCommand = new RelayCommand(
            _ => ApplySOSHint(),
            _ => !IsPencilMode.Value && !IsEraseMode.Value,
            new (Func<bool> fn, Action showMessage)[]
            {
                (() => IsPencilMode.Value, () => ShowMessage.Value = ("", "Pencil mode is set. Tap on Pencil again to enable SOS.", "")),
                (() => IsEraseMode.Value, () => ShowMessage.Value = ("", "Erase mode is set. Tap on Erase again to enable SOS.", ""))
            });
        PauseCommand = new RelayCommand(
            _ => PauseRequested.Value = !PauseRequested.Value,
            _ => StateMachine != null && StateMachine.IsPlaying && !IsEraseMode.Value && !IsPencilMode.Value,
            new (Func<bool> fn, Action showMessage)[]
            {
                (() => IsEraseMode.Value, () => ShowMessage.Value = ("", "Erase mode is set. Tap on Erase again to enable Pause.", "")),
                (() => IsPencilMode.Value, () => ShowMessage.Value = ("", "Pencil mode is set. Tap on Pencil again to enable Pause.", "")),
                (() => StateMachine != null && StateMachine.IsIdle, () => ShowMessage.Value = ("", "Game play is not started. Press an empty box to start the game.", ""))
            });
        ResumeCommand = new RelayCommand(_ => ResumeRequested.Value = true);
    }

    public void AttachStateMachine(ISudokuStateMachine stateMachine) => StateMachine = stateMachine;
    public void SetDemoMode() => _demoMode = true;
    public void ResetDemoMode() => _demoMode = false;

    public void StartRandomGame(SudokuDifficulty difficulty)
    {
        PauseRequested.Value = false;
        ResumeRequested.Value = false;
        IsPaused.Value = false;
        IsWon.Value = false;
        SetGameLevelAndDifficulty((int)difficulty + 1, difficulty);
        Model.SetPuzzleSeed(UnityEngine.Random.Range(1, int.MaxValue));
        ResetPuzzle();
        StateMachine.StartPlaying();
    }

    public virtual void ResetPuzzle()
    {
        SelectedRow.Value = -1;
        SelectedCol.Value = -1;
        IsPencilMode.Value = false;
        IsEraseMode.Value = false;
        HighlightedCandidateNumber.Value = 0;
        SelectedDigit.Value = 0;
        UndoHistory.Clear();
        RedoHistory.Clear();
        ConflictingCells.Value.Clear();
        UsageStats.Reset();
        Penalties.Reset();
        if(Model.CurrentDifficulty == SudokuDifficulty.Hard || Model.CurrentDifficulty == SudokuDifficulty.Expert)
        {
            Model.LoadCurrentLevelPuzzle(100000);    
        }
        else
        {
            Model.LoadCurrentLevelPuzzle();
        }
        PublishBoard();
        PublishPencilCandidates();
        ClosePicker();
        IsBoardValid.Value = true;
        IsComplete.Value = false;
        ConflictingCells.Value = new HashSet<(int, int)>();
        FirstCellTapped.Value = false;
    }

    public void SetGameLevelAndDifficulty(int level, SudokuDifficulty difficulty)
    {
        Model.SetLevel(level);
        Model.SetDifficulty(difficulty);
    }

    public void RecordEraseUse() => UsageStats.AddErase();
    public void NotifyGameCompleted() => GameCompleted?.Invoke();

    protected virtual void OnConflict() { }

    protected void PublishBoard()
    {
        BoardValues.Value = Model.Board;
        GivenMask.Value = Model.GivenMask;
    }

    protected void PublishPencilCandidates()
    {
        PencilCandidateMasks.Value = Model.PencilCandidateMasks;
    }

    protected void UpdateConflictingCells()
    {
        var conflicts = new HashSet<(int, int)>();
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
                if (Model.HasConflict(row, col)) conflicts.Add((row, col));
        ConflictingCells.Value = conflicts;
    }

    protected void ClosePicker()
    {
        IsPickerOpen.Value = false;
        SelectedDigit.Value = 0;
        SelectedRow.Value = -1;
        SelectedCol.Value = -1;
        SelectedCellTransform.Value = null;
    }

    private void SelectCell((int row, int col, object cellTransform) cell)
    {
        if (Model.IsGiven(cell.row, cell.col))
        {
            // A given clue is inspectable, but must never open the value picker.
            if (IsPickerOpen.Value) ClosePicker();
            SelectedRow.Value = cell.row;
            SelectedCol.Value = cell.col;
            SelectedCellTransform.Value = cell.cellTransform;
            SelectedDigit.Value = Model.GetValue(cell.row, cell.col);
            return;
        }

        if (!FirstCellTapped.Value) FirstCellTapped.Value = true;
        SelectedDigit.Value = 0;
        SelectedRow.Value = cell.row;
        SelectedCol.Value = cell.col;
        SelectedCellTransform.Value = cell.cellTransform;
        if (!IsEraseMode.Value)
        {
            IsPickerOpen.Value = false;
            IsPickerOpen.Value = true;
        }
    }

    private void EnterValue(int value)
    {
        int row = SelectedRow.Value;
        int col = SelectedCol.Value;
        if (row < 0 || col < 0 || Model.IsGiven(row, col)) return;

        int previousValue = Model.GetValue(row, col);
        int[] previousMasks = CopyPencilCandidateMasks();
        Model.SetValue(row, col, value);
        PublishBoard();

        bool hasConflict = Model.HasConflict(row, col);
        if (hasConflict && !_demoMode)
        {
            Penalties.AddMistake();
            OnConflict();
        }
        if (value > 0 && !hasConflict)
            Model.RemovePencilCandidateFromPeers(row, col, value);
        RecordHistory(row, col, previousValue, value, previousMasks);
        PublishPencilCandidates();
        LastEnteredCell.Value = (row, col, hasConflict);
        UpdateConflictingCells();
        IsBoardValid.Value = Model.Validate();
        IsComplete.Value = Model.IsComplete() && IsBoardValid.Value;
        ClosePicker();
        SelectedDigit.Value = value > 0 ? value : 0;
    }

    private int[] CopyPencilCandidateMasks()
    {
        var masks = new int[81];
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
                masks[row * 9 + col] = Model.PencilCandidateMasks[row, col];
        return masks;
    }

    private void RecordHistory(int row, int col, int beforeValue, int afterValue, int[] beforeMasks)
    {
        var entry = new SaveGameHistoryEntry
        {
            Row = row,
            Col = col,
            BeforeValue = beforeValue,
            AfterValue = afterValue
        };

        for (int cellRow = 0; cellRow < 9; cellRow++)
            for (int cellCol = 0; cellCol < 9; cellCol++)
            {
                int index = cellRow * 9 + cellCol;
                int afterMask = Model.PencilCandidateMasks[cellRow, cellCol];
                if (beforeMasks[index] != afterMask)
                {
                    entry.CandidateChanges.Add(new SaveGameCandidateChange
                    {
                        Row = cellRow,
                        Col = cellCol,
                        BeforeMask = beforeMasks[index],
                        AfterMask = afterMask
                    });
                }
            }

        if (beforeValue == afterValue && entry.CandidateChanges.Count == 0) return;
        UndoHistory.Push(entry);
        RedoHistory.Clear();
    }

    private void Undo()
    {
        if (UndoHistory.Count == 0) return;
        SaveGameHistoryEntry entry = UndoHistory.Pop();
        RedoHistory.Push(entry);
        UsageStats.AddUndo();
        ApplyHistory(entry, undo: true);
    }

    private void Redo()
    {
        if (RedoHistory.Count == 0) return;
        SaveGameHistoryEntry entry = RedoHistory.Pop();
        UndoHistory.Push(entry);
        ApplyHistory(entry, undo: false);
    }

    private void ApplyHistory(SaveGameHistoryEntry entry, bool undo)
    {
        if (entry.Row >= 0 && entry.Row < 9 && entry.Col >= 0 && entry.Col < 9)
            Model.SetValue(entry.Row, entry.Col, undo ? entry.BeforeValue : entry.AfterValue);

        if (entry.CandidateChanges != null)
            foreach (SaveGameCandidateChange change in entry.CandidateChanges)
                if (change != null)
                    Model.SetPencilCandidateMask(
                        change.Row, change.Col, undo ? change.BeforeMask : change.AfterMask);

        PublishBoard();
        PublishPencilCandidates();
        if (entry.Row >= 0 && entry.Row < 9 && entry.Col >= 0 && entry.Col < 9)
        {
            bool hasConflict = Model.HasConflict(entry.Row, entry.Col);
            LastEnteredCell.Value = (entry.Row, entry.Col, hasConflict);
        }
        UpdateConflictingCells();
        IsBoardValid.Value = Model.Validate();
        IsComplete.Value = Model.IsComplete() && IsBoardValid.Value;
    }

    private bool IsSelectedCellFilled()
    {
        int row = SelectedRow.Value;
        int col = SelectedCol.Value;
        return row >= 0 && col >= 0 && !Model.IsGiven(row, col) && !Model.IsCellEmpty(row, col);
    }

    private bool IsSelectedCellEmpty()
    {
        int row = SelectedRow.Value;
        int col = SelectedCol.Value;
        return row >= 0 && col >= 0 && !Model.IsGiven(row, col) && Model.IsCellEmpty(row, col);
    }

    private void ApplySOSHint()
    {
        int selectedRow = SelectedRow.Value;
        int selectedCol = SelectedCol.Value;
        var changedCells = new List<(int row, int col, int number)>();

        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (Model.IsGiven(row, col)) continue;
                int current = Model.GetValue(row, col);
                int correct = Model.GetSolutionValue(row, col);
                if (current != 0 && current != correct)
                {
                    changedCells.Add((row, col, correct));
                    Penalties.AddSOSWrongCell();
                }
            }
        }

        changedCells.Add((selectedRow, selectedCol, Model.GetSolutionValue(selectedRow, selectedCol)));
        Penalties.AddSOSEmptyCell();
        UsageStats.AddSOS();
        SOSChangedCells.Value = changedCells;
    }
}
