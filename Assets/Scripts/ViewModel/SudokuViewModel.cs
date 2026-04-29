using System;
using System.Collections.Generic;

/// <summary>
/// ViewModel for the Sudoku game.
/// Exposes BindableProperties for board state, selection, picker, animations and conflicts.
/// </summary>
public class SudokuViewModel
{
    private readonly SudokuModel _model = new SudokuModel();

    // ── Bindable Properties ───────────────────────────────────────────────────

    public BindableProperty<int[,]>  BoardValues  { get; } = new BindableProperty<int[,]>();
    public BindableProperty<bool[,]> GivenMask    { get; } = new BindableProperty<bool[,]>();
    public BindableProperty<int>     SelectedRow  { get; } = new BindableProperty<int>(-1);
    public BindableProperty<int>     SelectedCol  { get; } = new BindableProperty<int>(-1);
    public BindableProperty<bool>    IsPickerOpen { get; } = new BindableProperty<bool>(false);
    public BindableProperty<bool>    IsBoardValid { get; } = new BindableProperty<bool>(true);
    public BindableProperty<bool>    IsComplete   { get; } = new BindableProperty<bool>(false);

    public BindableProperty<bool>    IsEraseMode   { get; } = new BindableProperty<bool>(false);

    /// <summary>
    /// Fires after every number entry.
    /// Carries (row, col, hasConflict) so SudokuGrid knows which animation to play.
    /// </summary>
    public BindableProperty<(int row, int col, bool hasConflict)> LastEnteredCell { get; }
        = new BindableProperty<(int, int, bool)>();

    /// <summary>
    /// All cells currently in conflict — persists until the conflict is resolved.
    /// SudokuGrid subscribes to keep error color visible.
    /// </summary>
    public BindableProperty<HashSet<(int row, int col)>> ConflictingCells { get; }
        = new BindableProperty<HashSet<(int row, int col)>>(new HashSet<(int, int)>());

    /// <summary>
    /// RectTransform of selected cell carried opaquely for NumberPicker positioning.
    /// </summary>
    public BindableProperty<object> SelectedCellTransform { get; }
        = new BindableProperty<object>();

    // ── Commands ──────────────────────────────────────────────────────────────

    public ICommand SelectCellCommand   { get; }
    public ICommand EnterValueCommand   { get; }
    public ICommand CancelPickerCommand { get; }

    public ICommand SetEraseModeCommand { get; }

    // ── Constructor ───────────────────────────────────────────────────────────

    public SudokuViewModel()
    {
        SelectCellCommand = new RelayCommand(
            execute: param =>
            {
                var t = (ValueTuple<int, int, object>)param;
                OnSelectCell((t.Item1, t.Item2, t.Item3));
            },
            canExecute: _ => !IsPickerOpen.Value
        );

        EnterValueCommand = new RelayCommand(
            execute: param => OnEnterValue((int)param)
        );

        CancelPickerCommand = new RelayCommand(
            execute: _ => ClosePicker()
        );

        SetEraseModeCommand = new RelayCommand(
            execute: _ => IsEraseMode.Value = !IsEraseMode.Value
        );

        _model.LoadStartingPuzzle();
        PublishBoard();
    }

    // ── Command Handlers ──────────────────────────────────────────────────────

    private void OnSelectCell((int row, int col, object cellTransform) cell)
    {
        if (_model.IsGiven(cell.row, cell.col)) return;

        SelectedRow.Value           = cell.row;
        SelectedCol.Value           = cell.col;
        SelectedCellTransform.Value = cell.cellTransform;
        if(!IsEraseMode.Value)
            IsPickerOpen.Value          = true;
    }

    private void OnEnterValue(int value)
    {
        int row = SelectedRow.Value;
        int col = SelectedCol.Value;

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
        IsComplete.Value   = _model.IsComplete() && IsBoardValid.Value;

        ClosePicker();
    }

    private void ClosePicker()
    {
        IsPickerOpen.Value          = false;
        SelectedRow.Value           = -1;
        SelectedCol.Value           = -1;
        SelectedCellTransform.Value = null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void PublishBoard()
    {
        BoardValues.Value = _model.Board;
        GivenMask.Value   = _model.GivenMask;
    }

    /// <summary>
    /// Scans every non-given cell and builds a fresh set of conflicting positions.
    /// Publishing this fires OnConflictsChanged in SudokuGrid.
    /// </summary>
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
        _model.LoadStartingPuzzle();
        PublishBoard();
        ClosePicker();
        IsBoardValid.Value     = true;
        IsComplete.Value       = false;
        ConflictingCells.Value = new HashSet<(int, int)>();
    }
}