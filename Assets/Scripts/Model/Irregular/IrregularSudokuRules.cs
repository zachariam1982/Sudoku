using System;
using System.Collections.Generic;

/// <summary>Shared row, column, box, and irregular rules for Irregular Sudoku.</summary>
internal static class IrregularSudokuRules
{
    internal const int Size = 9;
    internal const int FullMask = 0x3FE;
    private static readonly int[] Left = {11, 14, 38, 41, 65, 68};
    private static readonly int[] Right = {21, 24, 48, 51, 75, 78};
    private static readonly int[][] Units = BuildUnits();

    internal static int GetRegion(int row, int col)
    {
        int index = row * 9 + col;
        for (int i = 0; i < Left.Length; i++)
        {
            if (index == Left[i]) return (Right[i] / 27) * 3 + Right[i] % 9 / 3;
            if (index == Right[i]) return (Left[i] / 27) * 3 + Left[i] % 9 / 3;
        }
        return row / 3 * 3 + col / 3;
    }

    internal static int[][] AllUnits => Units;

    internal static bool IsValidPlacement(int[,] board, int row, int col, int value)
    {
        if (board == null || board.GetLength(0) != Size || board.GetLength(1) != Size ||
            row < 0 || row >= Size || col < 0 || col >= Size || value < 1 || value > 9)
            return false;

        for (int i = 0; i < Size; i++)
        {
            if ((i != col && board[row, i] == value) ||
                (i != row && board[i, col] == value))
                return false;
        }

        foreach (int index in Units[18 + GetRegion(row, col)])
            if ((index / 9 != row || index % 9 != col) && board[index / 9, index % 9] == value) return false;

        return true;
    }

    internal static int GetCandidateMask(int[,] board, int row, int col)
    {
        if (board == null || board.GetLength(0) != Size || board.GetLength(1) != Size ||
            row < 0 || row >= Size || col < 0 || col >= Size || board[row, col] != 0)
            return 0;

        int used = 0;
        for (int i = 0; i < Size; i++)
        {
            used |= ValueBit(board[row, i]);
            used |= ValueBit(board[i, col]);
        }

        foreach (int index in Units[18 + GetRegion(row, col)]) used |= ValueBit(board[index / 9, index % 9]);

        return FullMask & ~used;
    }

    internal static bool ValidateBoard(int[,] board)
    {
        if (board == null || board.GetLength(0) != Size || board.GetLength(1) != Size)
            return false;

        foreach (int[] unit in Units)
        {
            int used = 0;
            foreach (int index in unit)
            {
                int value = board[index / Size, index % Size];
                if (value < 0 || value > 9) return false;
                if (value == 0) continue;
                int bit = 1 << value;
                if ((used & bit) != 0) return false;
                used |= bit;
            }
        }

        return true;
    }

    internal static bool ValidateCompleteSolution(int[,] board)
    {
        if (!ValidateBoard(board)) return false;
        foreach (int[] unit in Units)
        {
            int used = 0;
            foreach (int index in unit)
            {
                int value = board[index / Size, index % Size];
                if (value == 0) return false;
                used |= 1 << value;
            }
            if (used != FullMask) return false;
        }
        return true;
    }

    private static int ValueBit(int value) => value >= 1 && value <= 9 ? 1 << value : 0;

    private static int[][] BuildUnits()
    {
        var units = new int[27][];
        int unitIndex = 0;

        for (int row = 0; row < Size; row++)
        {
            units[unitIndex] = new int[Size];
            for (int col = 0; col < Size; col++) units[unitIndex][col] = row * Size + col;
            unitIndex++;
        }

        for (int col = 0; col < Size; col++)
        {
            units[unitIndex] = new int[Size];
            for (int row = 0; row < Size; row++) units[unitIndex][row] = row * Size + col;
            unitIndex++;
        }

        var regions = new List<int>[9];
        for (int i = 0; i < 9; i++) regions[i] = new List<int>(9);
        for (int i = 0; i < 81; i++) regions[GetRegion(i / 9, i % 9)].Add(i);
        for (int i = 0; i < 9; i++) units[18 + i] = regions[i].ToArray();

        return units;
    }
}
