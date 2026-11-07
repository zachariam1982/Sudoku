using System;
using System.Collections.Generic;
public static class ThermoSudokuRules
{
    public const int FullMask = 0x3FE;
    public static bool ValidateThermometerLayout(Thermometer[] paths)
    {
        if (paths == null || paths.Length == 0) return false;
        foreach (var path in paths)
        {
            if (path == null || path.Cells == null || path.Cells.Length < 2 || path.Cells.Length > 9) return false;
            var seen = new HashSet<int>();
            for (int i = 0; i < path.Cells.Length; i++)
            {
                int cell = path.Cells[i];
                if (cell < 0 || cell >= 81 || !seen.Add(cell)) return false;
                if (i > 0 && Math.Abs(cell / 9 - path.Cells[i-1] / 9) + Math.Abs(cell % 9 - path.Cells[i-1] % 9) != 1) return false;
            }
        }
        return true;
    }
    public static int GetCandidateMask(int[,] board, Thermometer[] paths, int row, int col)
    {
        if (board[row,col] != 0) return 0;
        int mask = 0;
        for (int value = 1; value <= 9; value++)
            if (IsValidPlacement(board, paths, row, col, value)) mask |= 1 << value;
        return mask;
    }
    public static bool IsValidPlacement(int[,] board, Thermometer[] paths, int row, int col, int value)
    {
        if (value < 1 || value > 9) return false;
        for (int i = 0; i < 9; i++)
        {
            if (i != col && board[row,i] == value || i != row && board[i,col] == value) return false;
            int r = row / 3 * 3 + i / 3, c = col / 3 * 3 + i % 3;
            if ((r != row || c != col) && board[r,c] == value) return false;
        }
        if (paths == null) return true;
        foreach (var path in paths)
        {
            int position = Array.IndexOf(path.Cells, row * 9 + col);
            if (position < 0) continue;
            if (value < position + 1 || value > 9 - (path.Cells.Length - 1 - position)) return false;
            for (int j = 0; j < path.Cells.Length; j++)
            {
                int other = board[path.Cells[j]/9,path.Cells[j]%9];
                if (j == position || other == 0) continue;
                if (j < position && value - other < position - j || j > position && other - value < j - position) return false;
            }
        }
        return true;
    }
    public static bool ValidateBoard(int[,] board, Thermometer[] paths)
    {
        if (board == null || board.GetLength(0) != 9 || board.GetLength(1) != 9 || !ValidateThermometerLayout(paths)) return false;
        for (int r = 0; r < 9; r++) for (int c = 0; c < 9; c++)
            if (board[r,c] != 0 && !IsValidPlacement(board, paths, r, c, board[r,c])) return false;
        return true;
    }
    public static bool ValidateCompleteSolution(int[,] board, Thermometer[] paths)
    {
        if (!ValidateBoard(board, paths)) return false;
        foreach (int value in board) if (value == 0) return false;
        return true;
    }
}
