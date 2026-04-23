using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages puzzle logic for the 9x9 Sudoku grid.
/// Supports board state save/restore so GridBuilder can rebuild on rotation.
/// </summary>
public class SudokuGrid : MonoBehaviour
{
    private SudokuCell[,] cells        = new SudokuCell[9, 9];
    private bool          cellsReady   = false;
    private bool          interactable = true;

    // Saved board state so it survives a grid rebuild on rotation
    private int[,]  currentBoard = new int[9, 9];
    private bool[,] givenMask    = new bool[9, 9];
    private bool    hasExistingState = false;

    // Starting puzzle (0 = empty)
    private readonly int[,] startingBoard = new int[9, 9]
    {
        { 5, 3, 0,  0, 7, 0,  0, 0, 0 },
        { 6, 0, 0,  1, 9, 5,  0, 0, 0 },
        { 0, 9, 8,  0, 0, 0,  0, 6, 0 },

        { 8, 0, 0,  0, 6, 0,  0, 0, 3 },
        { 4, 0, 0,  8, 0, 3,  0, 0, 1 },
        { 7, 0, 0,  0, 2, 0,  0, 0, 6 },

        { 0, 6, 0,  0, 0, 0,  2, 8, 0 },
        { 0, 0, 0,  4, 1, 9,  0, 0, 5 },
        { 0, 0, 0,  0, 8, 0,  0, 7, 9 }
    };

    // ── Setup ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by GridBuilder after spawning all 81 cells.
    /// If no existing state, loads the starting puzzle.
    /// If rotating, restores the saved state automatically.
    /// </summary>
    public void SetCells(SudokuCell[] allCells)
    {
        cells = new SudokuCell[9, 9];

        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
            {
                cells[row, col] = allCells[row * 9 + col];
                cells[row, col].Init(row, col, this);
            }

        cellsReady = true;

        // First load: use starting puzzle. After rotation: restore saved state.
        if (!hasExistingState)
            LoadPuzzle(startingBoard);
        else
            RestoreBoard(currentBoard, givenMask);
    }

    private void LoadPuzzle(int[,] board)
    {
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
            {
                int  value = board[row, col];
                bool given = value != 0;

                currentBoard[row, col] = value;
                givenMask[row, col]    = given;

                cells[row, col].SetValue(value, isGiven: given);
            }

        hasExistingState = true;
    }

    private void RestoreBoard(int[,] board, bool[,] given)
    {
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
                cells[row, col].SetValue(board[row, col], isGiven: given[row, col]);
    }

    public void SaveCurrentState()
    {
        if (!cellsReady) return;

        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
            {
                currentBoard[row, col] = cells[row, col].Value;
                givenMask[row, col]    = cells[row, col].IsGiven;
            }
    }

    public void OnCellSelected(SudokuCell selectedCell)
    {
        if (!interactable) return;
        if (selectedCell.IsGiven) return;

        if (NumberPicker.Instance != null)
            NumberPicker.Instance.Show(selectedCell, this);
    }

    public void SetBoardInteractable(bool value)
    {
        interactable = value;

        foreach (SudokuCell cell in cells)
            cell.SetDimmed(!value);
    }

    // ── Validation ───────────────────────────────────────────────────────────

    public bool ValidateBoard()
    {
        if (!cellsReady) return false;

        for (int i = 0; i < 9; i++)
        {
            bool[] rowSeen = new bool[10];
            bool[] colSeen = new bool[10];
            for (int j = 0; j < 9; j++)
            {
                int rv = cells[i, j].Value;
                int cv = cells[j, i].Value;
                if (rv != 0) { if (rowSeen[rv]) return false; rowSeen[rv] = true; }
                if (cv != 0) { if (colSeen[cv]) return false; colSeen[cv] = true; }
            }
        }

        for (int boxRow = 0; boxRow < 3; boxRow++)
            for (int boxCol = 0; boxCol < 3; boxCol++)
            {
                bool[] seen = new bool[10];
                for (int r = 0; r < 3; r++)
                    for (int c = 0; c < 3; c++)
                    {
                        int v = cells[boxRow * 3 + r, boxCol * 3 + c].Value;
                        if (v != 0) { if (seen[v]) return false; seen[v] = true; }
                    }
            }

        return true;
    }

    public int GetCellValue(int row, int col) => cells[row, col].Value;
}