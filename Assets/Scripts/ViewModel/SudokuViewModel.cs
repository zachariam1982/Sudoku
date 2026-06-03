using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;


/// <summary>
/// ViewModel for the Sudoku game.
/// Exposes BindableProperties for board state, selection, picker, animations and conflicts.
/// </summary>
public class SudokuViewModel
{
    private readonly SudokuModel _model = new SudokuModel();
    private Stack<ValueTuple<int,int,int>> replacedValueStack = new Stack<ValueTuple<int, int, int>>();
    private bool demoMode = false;
    public void SetDemoMode() => demoMode = true;
    public void ResetDemoMode() => demoMode = false;    


    public BindableProperty<bool>    HideHUD      { get; }    = new BindableProperty<bool>(false);
    public BindableProperty<int[,]>  BoardValues  { get; }    = new BindableProperty<int[,]>();
    public BindableProperty<bool[,]> GivenMask    { get; }    = new BindableProperty<bool[,]>();
    public BindableProperty<int>     SelectedRow  { get; }    = new BindableProperty<int>(-1);
    public BindableProperty<int>     SelectedCol  { get; }    = new BindableProperty<int>(-1);
    public BindableProperty<bool>    IsPickerOpen { get; }    = new BindableProperty<bool>(false);
    public BindableProperty<bool>    IsBoardValid { get; }    = new BindableProperty<bool>(true);
    public BindableProperty<bool>    IsComplete   { get; }    = new BindableProperty<bool>(false);
    public BindableProperty<bool>    IsEraseMode  { get; }    = new BindableProperty<bool>(false);
    public BindableProperty<bool>    IsPencilMode { get; }    = new BindableProperty<bool>(false);
    /// <summary>Name of the current state — Views use this to show/hide panels.</summary>
    public BindableProperty<string> CurrentStateName { get; } = new BindableProperty<string>("");
    /// <summary>Timer value in seconds. PlayingState increments this every frame.</summary>
    public BindableProperty<float>  ElapsedSeconds   { get; } = new BindableProperty<float>(0f);
    public BindableProperty<int>    LivesRemaining   { get; } = new BindableProperty<int>(3);
    /// <summary>True while the timer is running.</summary>
    public BindableProperty<bool>   IsTimerRunning   { get; } = new BindableProperty<bool>(false);
    public BindableProperty<bool>   IsSOSMode        { get; } = new BindableProperty<bool>(false);
    /// <summary>True while game is paused.</summary>
    public BindableProperty<bool>   IsPaused         { get; } = new BindableProperty<bool>(false);
    /// <summary>True during the brief validation animation before win screen.</summary>
    public BindableProperty<bool>   IsValidating     { get; } = new BindableProperty<bool>(false);
    /// <summary>True when puzzle is won — drives win screen visibility.</summary>
    public BindableProperty<bool>   IsWon            { get; } = new BindableProperty<bool>(false);
    /// <summary>True when lives run out — drives lose screen visibility.</summary>
    public BindableProperty<bool>   IsLost           { get; } = new BindableProperty<bool>(false);
    public BindableProperty<bool> FirstCellTapped    { get; } = new BindableProperty<bool>(false);
    /// <summary>Set to true by pause button — triggers Playing → Paused.</summary>
    public BindableProperty<bool> PauseRequested     { get; } = new BindableProperty<bool>(false);
    /// <summary>Set to true by resume button — triggers Paused → Playing.</summary>
    public BindableProperty<bool> ResumeRequested    { get; } = new BindableProperty<bool>(false);
    /// <summary>Set to true by New Game button — triggers any state → Idle.</summary>
    public BindableProperty<bool> NewGameRequested   { get; } = new BindableProperty<bool>(false);
    /// <summary>Set to true by Retry button on lose screen — triggers Lose → Idle.</summary>
    public BindableProperty<bool> RetryGameRequested { get; } = new BindableProperty<bool>(false);
    public BindableProperty<(int row, int col, bool hasConflict)> LastEnteredCell { get; } 
        = new BindableProperty<(int, int, bool)>();
    public BindableProperty<HashSet<(int row, int col)>> ConflictingCells { get; }
        = new BindableProperty<HashSet<(int row, int col)>>(new HashSet<(int, int)>());
    public BindableProperty<object> SelectedCellTransform { get; }
        = new BindableProperty<object>();
    public BindableProperty<List<(int row, int col, int number)>> SOSChangedCells { get; }
        = new BindableProperty<List<(int, int, int)>>(new List<(int, int, int)>());

    public ICommand SelectCellCommand    { get; }
    public ICommand EnterValueCommand    { get; }
    public ICommand CancelPickerCommand  { get; }
    public ICommand SetEraseModeCommand  { get; }
    public ICommand SetPencilModeCommand { get; }
    public ICommand SOSCommand           { get; }
    public ICommand ApplySOSCommand      { get; }
    public ICommand UndoCommand          { get; }
    public ICommand PauseCommand         { get; }
    public ICommand ResumeCommand        { get; }
    public ICommand NewGameCommand       { get; }
    public ICommand RetryCommand         { get; }
    public ICommand AddLevel             { get; }
    public ICommand IncreaseDifficulty   { get; }
    public ICommand DecreaseDifficulty   { get; }

    public SudokuViewModel()
    {
        SelectCellCommand = new RelayCommand(
            execute: param =>
            {
                var t = (ValueTuple<int, int, object>)param;
                OnSelectCell((t.Item1, t.Item2, t.Item3));
            }//,
            //canExecute: _ => !IsPickerOpen.Value
        );
        EnterValueCommand = new RelayCommand(
            execute: param => OnEnterValue((int)param)
        );
        CancelPickerCommand = new RelayCommand(
            execute: _ => ClosePicker()
        );
        SetEraseModeCommand = new RelayCommand(
            execute: _ => IsEraseMode.Value = !IsEraseMode.Value,
            canExecute: _ => IsPencilMode.Value == false && replacedValueStack.Count != 0 
        );
        SetPencilModeCommand = new RelayCommand(
            execute: _ => IsPencilMode.Value = !IsPencilMode.Value,
            canExecute: _ => IsEraseMode.Value == false
        );
        UndoCommand = new RelayCommand(
            execute: _ =>
            {
                var t = getPreviousValues();
                if(t.Item1 == -1 || t.Item2 == -1 || t.Item3 == -1) return;
                OnEnterValueForUndoOperation(t.Item1, t.Item2, t.Item3);
            },
            canExecute: _ => IsPencilMode.Value == false && IsEraseMode.Value == false
        );
        SOSCommand = new RelayCommand(
            execute: _ => 
            {
                IsSOSMode.Value = !IsSOSMode.Value;
            },
            canExecute: _ => IsPencilMode.Value == false && IsEraseMode.Value == false
        );
        ApplySOSCommand = new RelayCommand(
            execute: _ => ApplySOSHint(),
            canExecute: _ => IsPencilMode.Value == false && IsEraseMode.Value == false
        );
        PauseCommand = new RelayCommand(
            execute: _ => PauseRequested.Value = true,
            canExecute: _ => GameStateMachine.Instance?.CurrentState
                             is PlayingState && IsEraseMode.Value == false && IsPencilMode.Value == false
        );
 
        ResumeCommand = new RelayCommand(
            execute: _ => ResumeRequested.Value = true
        );
 
        NewGameCommand = new RelayCommand(
            execute: _ => {
                NewGameRequested.Value = true;
            }
        );
 
        RetryCommand = new RelayCommand(
            execute: _ => RetryGameRequested.Value = true
        );

        AddLevel = new RelayCommand(
            execute: _ => _model?.AddLevel((int)1)
        );

        IncreaseDifficulty = new RelayCommand(
            execute: _ => _model?.increaseDifficulty()
        );

        DecreaseDifficulty = new RelayCommand(
            execute: _ => _model?.decreaseDifficulty()
        );
    }
    private void OnSelectCell((int row, int col, object cellTransform) cell)
    {
        if (_model.IsGiven(cell.row, cell.col)) return;

        // Signal first tap so IdleState can transition to Playing
        if (!FirstCellTapped.Value)
            FirstCellTapped.Value = true;

        SelectedRow.Value           = cell.row;
        SelectedCol.Value           = cell.col;
        SelectedCellTransform.Value = cell.cellTransform;
        if(!IsEraseMode.Value){
            IsPickerOpen.Value          = false;
            IsPickerOpen.Value          = true;
        }
    }
    private void OnEnterValue(int value)
    {
        int row = SelectedRow.Value;
        int col = SelectedCol.Value;

        if (row < 0 || col < 0) return;

        replacedValueStack.Push(new ValueTuple<int, int, int>(row, col, _model.GetValue(row, col)));
        _model.SetValue(row, col, value);
        PublishBoard();

        // Check conflict on entered cell
        bool hasConflict = _model.HasConflict(row, col);

        if (hasConflict && !this.demoMode)
        {
            this.LivesRemaining.Value--;
        }

        // Notify SudokuGrid to play entry or error animation on entered cell
        LastEnteredCell.Value = (row, col, hasConflict);

        // Recalculate ALL conflicting cells across the whole board
        // and publish — SudokuGrid will set persistent error color on each
        UpdateConflictingCells();

        IsBoardValid.Value = _model.Validate();
        IsComplete.Value   = _model.IsComplete() && IsBoardValid.Value;

        ClosePicker();
    }
    private void OnEnterValueForUndoOperation(int row, int col, int value)
    {
        if (row < 0 || col < 0) return;

        _model.SetValue(row, col, value);
        PublishBoard();

        // Check conflict on entered cell
        bool hasConflict = _model.HasConflict(row, col);

        // Notify SudokuGrid to play entry or error animation on entered cell
        LastEnteredCell.Value = (row, col, hasConflict);

        // Recalculate ALL conflicting cells across the whole board
        // and publish — SudokuGrid will set persistent error color on each
        UpdateConflictingCells();

        IsBoardValid.Value = _model.Validate();
    }
    private void ClosePicker()
    {
        IsPickerOpen.Value          = false;
        SelectedRow.Value           = -1;
        SelectedCol.Value           = -1;
        SelectedCellTransform.Value = null;
    }
    private void ApplySOSHint()
    {
        var changedCells = new List<(int row, int col, int number)>();
 
        // ── Step 1: fix all wrong entries ─────────────────────────────────────
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (_model.IsGiven(row, col)) continue;
 
                int current = _model.GetValue(row, col);
                int correct = _model.GetSolutionValue(row, col);
 
                if (current != 0 && current != correct)
                    changedCells.Add((row, col, correct));
            }
        }

        // ── Step 2: fix one empty entry ─────────────────────────────────────
        bool fillEmpty = false;
        for (int row = 0; row < 9 && !fillEmpty; row++)
        {
            for (int col = 0; col < 9 && !fillEmpty; col++)
            {
                if (_model.IsGiven(row, col)) continue;
 
                if (_model.IsCellEmpty(row, col))
                {
                    int correct = _model.GetSolutionValue(row, col);

                    changedCells.Add((row, col, correct));
                    fillEmpty = true;
                }
            }
        }
 
        if (changedCells.Count == 0) return;
 
        SOSChangedCells.Value = changedCells;
    }
    private void PublishBoard()
    {
        BoardValues.Value = _model.Board;
        GivenMask.Value   = _model.GivenMask;
    }
    private void UpdateConflictingCells()
    {
        var conflicts = new HashSet<(int, int)>();

        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
                //if (!_model.IsGiven(row, col) && _model.HasConflict(row, col))
                if (_model.HasConflict(row, col))
                    conflicts.Add((row, col));

        ConflictingCells.Value = conflicts;
    }
    public void ResetPuzzle()
    {
        this.SelectedRow.Value = -1;
        this.SelectedCol.Value = -1;
        replacedValueStack.Clear();
        ConflictingCells.Value.Clear();
        _model.LoadCurrentLevelPuzzle();
        PublishBoard();
        ClosePicker();
        IsBoardValid.Value     = true;
        IsComplete.Value       = false;
        ConflictingCells.Value = new HashSet<(int, int)>();
        this.FirstCellTapped.Value = false;
        this.ElapsedSeconds.Value = 0f;
    }
    public ValueTuple<int,int,int> getPreviousValues()
    {
        if(this.replacedValueStack.Count == 0)
        {
            return new ValueTuple<int, int, int>(-1,-1,-1);
        }
        return this.replacedValueStack.Pop();
    }
}