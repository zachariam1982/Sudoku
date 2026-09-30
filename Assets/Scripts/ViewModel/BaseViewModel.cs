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
    protected readonly Stack<ValueTuple<int, int, int>> ReplacedValueStack =
        new Stack<ValueTuple<int, int, int>>();

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
        UndoCommand = new RelayCommand(
            _ =>
            {
                var previous = GetPreviousValue();
                if (previous.Item1 < 0) return;
                UsageStats.AddUndo();
                EnterValueForUndo(previous.Item1, previous.Item2, previous.Item3);
            },
            _ => !IsPencilMode.Value && !IsEraseMode.Value,
            new (Func<bool> fn, Action showMessage)[]
            {
                (() => IsPencilMode.Value, () => ShowMessage.Value = ("", "Pencil mode is set. Tap on Pencil again to enable undo.", "")),
                (() => IsEraseMode.Value, () => ShowMessage.Value = ("", "Erase mode is set. Tap on Erase again to enable undo.", ""))
            });
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
                Model.AutoFillPencilCandidates();
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

            int number = (int)param;
            Model.TogglePencilCandidate(row, col, number);
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
        ReplacedValueStack.Clear();
        ConflictingCells.Value.Clear();
        UsageStats.Reset();
        Penalties.Reset();
        Model.LoadCurrentLevelPuzzle();
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
        SelectedRow.Value = -1;
        SelectedCol.Value = -1;
        SelectedCellTransform.Value = null;
    }

    private void SelectCell((int row, int col, object cellTransform) cell)
    {
        if (Model.IsGiven(cell.row, cell.col)) return;
        if (!FirstCellTapped.Value) FirstCellTapped.Value = true;
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
        if (row < 0 || col < 0) return;

        ReplacedValueStack.Push((row, col, Model.GetValue(row, col)));
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
        PublishPencilCandidates();
        LastEnteredCell.Value = (row, col, hasConflict);
        UpdateConflictingCells();
        IsBoardValid.Value = Model.Validate();
        IsComplete.Value = Model.IsComplete() && IsBoardValid.Value;
        ClosePicker();
    }

    private void EnterValueForUndo(int row, int col, int value)
    {
        Model.SetValue(row, col, value);
        PublishBoard();
        bool hasConflict = Model.HasConflict(row, col);
        if (value > 0 && !hasConflict)
            Model.RemovePencilCandidateFromPeers(row, col, value);
        PublishPencilCandidates();
        LastEnteredCell.Value = (row, col, hasConflict);
        UpdateConflictingCells();
        IsBoardValid.Value = Model.Validate();
        IsComplete.Value = Model.IsComplete() && IsBoardValid.Value;
    }

    private ValueTuple<int, int, int> GetPreviousValue()
    {
        return ReplacedValueStack.Count == 0
            ? new ValueTuple<int, int, int>(-1, -1, -1)
            : ReplacedValueStack.Pop();
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
