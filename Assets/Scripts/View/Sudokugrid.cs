using System.Collections.Generic;
//using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// View — manages the 9x9 grid of SudokuCell views.
/// Handles board rendering, highlights, dimming, entry animations and persistent error colors.
/// </summary>
public class SudokuGrid : MonoBehaviour
{
    [Header("Background")]
    [SerializeField] private Image gridBackground;

    private SudokuCell[,]  cells      = new SudokuCell[9, 9];
    private bool           cellsReady = false;
    private SudokuViewModel viewModel;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    void Start()
    {
        if (gridBackground != null)
            gridBackground.color = new Color(0.1f, 0.1f, 0.18f, 1f);

    }

    // ── Binding ───────────────────────────────────────────────────────────────

    public void Bind(SudokuViewModel vm)
    {
        viewModel = vm;

        vm.BoardValues.OnChanged      += OnBoardChanged;
        vm.GivenMask.OnChanged        += OnBoardChanged;
        vm.SelectedRow.OnChanged      += _ => RefreshHighlights();
        vm.SelectedCol.OnChanged      += _ => RefreshHighlights();
        vm.IsPickerOpen.OnChanged     += OnPickerOpenChanged;
        vm.LastEnteredCell.OnChanged  += OnCellValueEntered;
        vm.ConflictingCells.OnChanged += OnConflictsChanged;
        vm.IsEraseMode.OnChanged      += OnEraseModeChanged;
        vm.IsPencilMode.OnChanged     += OnPencilModeChanged;
    }


    private void OnDestroy()
    {
        if (viewModel == null) return;

        viewModel.BoardValues.OnChanged      -= OnBoardChanged;
        viewModel.GivenMask.OnChanged        -= OnBoardChanged;
        viewModel.SelectedRow.OnChanged      -= _ => RefreshHighlights();
        viewModel.SelectedCol.OnChanged      -= _ => RefreshHighlights();
        viewModel.IsPickerOpen.OnChanged     -= OnPickerOpenChanged;
        viewModel.LastEnteredCell.OnChanged  -= OnCellValueEntered;
        viewModel.ConflictingCells.OnChanged -= OnConflictsChanged;
        viewModel.IsEraseMode.OnChanged      -= OnEraseModeChanged;
        viewModel.IsPencilMode.OnChanged     -= OnPencilModeChanged;
    }

    // ── Binding Handlers ──────────────────────────────────────────────────────
    private void OnPencilModeChanged(bool arg)
    {
        for(int row = 0; row < 9; row++)
            for( int col = 0; col < 9; col++)
                if( cells[row, col].Value == 0)
                {
                    cells[row, col].pencilCell.SetActive(arg);
                }        
    }
    private void OnEraseModeChanged(bool arg)
    {
        for(int row = 0; row < 9; row++)
            for( int col = 0; col < 9; col++)
                if(cells[row, col].IsGiven == false)
                {
                    GameObject obj = cells[row,col].transform.Find("Erase").gameObject;
                    if(arg == false)
                        obj.SetActive(arg); 
                    else if(arg == true && cells[row,col].Value != 0)
                        obj.SetActive(arg);          
                }
    }

    private void OnBoardChanged<T>(T _)
    {
        if (!cellsReady || viewModel == null) return;

        int[,]  board = viewModel.BoardValues.Value;
        bool[,] given = viewModel.GivenMask.Value;

        if (board == null || given == null) return;

        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
                cells[row, col].SetValue(board[row, col], given[row, col]);
    }

    private void RefreshHighlights()
    {
        if (!cellsReady || viewModel == null) return;

        int selRow = viewModel.SelectedRow.Value;
        int selCol = viewModel.SelectedCol.Value;

        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
                cells[row, col].SetHighlight(row == selRow && col == selCol);
    }

    private void OnPickerOpenChanged(bool isOpen)
    {
        if (!cellsReady) return;

        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
            {
                bool isSelected = row == viewModel.SelectedRow.Value
                               && col == viewModel.SelectedCol.Value;
                if (isOpen)
                {
                    if (isSelected) cells[row, col].SetPickerHighlight(true);
                    else            cells[row, col].SetDimmed(true);
                }
                else
                {
                    cells[row, col].SetDimmed(false);
                    cells[row, col].SetPickerHighlight(false);
                }
            }
    }

    /// <summary>
    /// Fires after a number is entered.
    /// Plays bounce on clean entry, shake+flash on conflict.
    /// Persistent error color is handled separately by OnConflictsChanged.
    /// </summary>
    private void OnCellValueEntered((int row, int col, bool hasConflict) entry)
    {
        if (!cellsReady) return;

        SudokuCell enteredCell = cells[entry.row, entry.col];

        if (!entry.hasConflict)
            enteredCell.PlayEntryAnimation();
        else
            enteredCell.PlayErrorAnimation();
    }

    /// <summary>
    /// Fires every time the conflict set changes.
    /// Sets persistent error color on all conflicting cells,
    /// and clears it on cells that are no longer conflicting.
    /// </summary>
    private void OnConflictsChanged(HashSet<(int row, int col)> conflicts)
    {
        Debug.Log($"OnConflictsChanged — conflict count: {conflicts.Count}");
        if (!cellsReady) return;

        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
                cells[row, col].SetConflict(conflicts.Contains((row, col)));
    }

    // ── Cell Setup ────────────────────────────────────────────────────────────

    public void SetCells(SudokuCell[] allCells)
    {
        cells = new SudokuCell[9, 9];

        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
            {
                cells[row, col] = allCells[row * 9 + col];
                cells[row, col].Bind(row, col, viewModel);
            }

        cellsReady = true;

        if (viewModel != null)
            OnBoardChanged<int[,]>(null);
    }

    public void SaveCurrentState() { }
}