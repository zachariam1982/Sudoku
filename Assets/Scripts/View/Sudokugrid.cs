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
        vm.HighlightedCandidateNumber.OnChanged += OnCandidateHighlightChanged;
        vm.AutoFillCandidatesRequested.OnChanged += OnAutoFillCandidatesRequested;
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
        viewModel.HighlightedCandidateNumber.OnChanged -= OnCandidateHighlightChanged;
        viewModel.AutoFillCandidatesRequested.OnChanged += OnAutoFillCandidatesRequested;

    }

    // ── Binding Handlers ──────────────────────────────────────────────────────

    private void OnAutoFillCandidatesRequested(int requestNumber)
    {
        if (!cellsReady || viewModel == null) return;

        int[,] board = viewModel.BoardValues.Value;

        if (board == null) return;

        for (int row = 0;row < 9;row++)
        {
            for (int col = 0;col < 9;col++)
            {
                if (board[row, col] != 0)
                {
                    cells[row, col].ClearAllPencilCandidates();
                    continue;
                }

                List<int> candidates = CalculateCandidates(board,row,col);
                cells[row, col].SetPencilCandidates(candidates);
            }
        }
    }

    private static List<int> CalculateCandidates(int[,] board,int targetRow,int targetCol)
    {
        List<int> result = new List<int>();

        if (board == null) return result;
        if (board[targetRow,targetCol] != 0) return result; 

        bool[] used = new bool[10];

        for (int col = 0;col < 9;col++)
        {
            int number = board[targetRow,col];

            if (number >= 1 && number <= 9) used[number] = true;
        }

        for (int row = 0;row < 9;row++)
        {
            int number = board[row,targetCol];

            if (number >= 1 && number <= 9) used[number] = true;
        }
        int boxStartRow = (targetRow / 3) * 3;

        int boxStartCol = (targetCol / 3) * 3;

        for (int row = boxStartRow; row < boxStartRow + 3; row++)
        {
            for (int col = boxStartCol; col < boxStartCol + 3; col++)
            {
                int number = board[row,col];

                if (number >= 1 && number <= 9)  used[number] = true;
            }
        }

        for (int number = 1; number <= 9; number++)
        {
            if (!used[number]) result.Add(number);
        }

        return result;
    }
    private void OnCandidateHighlightChanged(
        int number)
    {
        if (!cellsReady) return;

        for (int row = 0;row < 9;row++)
        {
            for (int col = 0;col < 9;col++)
            {
                cells[row, col].SetCandidateHighlight(number);
            }
        }
    }
    private void OnPencilModeChanged(bool isPencilMode)
    {
        if (!cellsReady) return;

        for (int row = 0;row < 9;row++)
        {
            for (int col = 0;col < 9;col++)
            {
                if (cells[row, col].Value == 0)
                {
                    cells[row, col].pencilCell.SetActive(isPencilMode);
                }
            }
        }

        if (!isPencilMode)
        {
            OnCandidateHighlightChanged(0);
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
    private void OnCellValueEntered((int row,int col,bool hasConflict) entry)
    {
        if (!cellsReady) return;

        SudokuCell enteredCell = cells[entry.row,entry.col];

        if (entry.hasConflict)
        {
            enteredCell.PlayErrorAnimation();
            return;
        }

        enteredCell.PlayEntryAnimation();

        int value = enteredCell.Value;

        if (value <= 0) return;

        RemoveCandidateFromPeers(entry.row,entry.col,value);
    }
    private void RemoveCandidateFromPeers(int enteredRow,int enteredCol,int number)
    {
        if (!cellsReady) return;

        for (int col = 0;col < 9;col++)
        {
            if (col == enteredCol) continue;

            SudokuCell cell = cells[enteredRow,col];

            if (cell.Value != 0) continue;

            cell.RemovePencilCandidate(number);
        }

        for (int row = 0;row < 9;row++)
        {
            if (row == enteredRow) continue;

            SudokuCell cell = cells[row,enteredCol];

            if (cell.Value != 0) continue;

            cell.RemovePencilCandidate(number);
        }

        int boxStartRow = (enteredRow / 3) * 3;
        int boxStartCol = (enteredCol / 3) * 3;

        for (int row = boxStartRow;row < boxStartRow + 3;row++)
        {
            for (int col = boxStartCol;col < boxStartCol + 3;col++)
            {
                if (row == enteredRow && col == enteredCol) continue;

                SudokuCell cell = cells[row,col];

                if (cell.Value != 0) continue;

                cell.RemovePencilCandidate(number);
            }
        }
    }
    /// <summary>
    /// Fires every time the conflict set changes.
    /// Sets persistent error color on all conflicting cells,
    /// and clears it on cells that are no longer conflicting.
    /// </summary>
    private void OnConflictsChanged(HashSet<(int row, int col)> conflicts)
    {
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
        {
            OnBoardChanged<int[,]>(null);
            OnConflictsChanged(viewModel.ConflictingCells.Value);
        }
    }

    public void SaveCurrentState() { }
}