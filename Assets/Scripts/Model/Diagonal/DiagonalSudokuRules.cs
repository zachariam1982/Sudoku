using System;
using System.Collections.Generic;

/// <summary>Shared row, column, box, and diagonal rules for Diagonal Sudoku.</summary>
internal static class DiagonalSudokuRules
{
    internal const int Size = 9;
    internal const int FullMask = 0x3FE;
    private static readonly int[][] Units = BuildUnits();

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

        int boxRow = row / 3 * 3;
        int boxCol = col / 3 * 3;
        for (int r = boxRow; r < boxRow + 3; r++)
            for (int c = boxCol; c < boxCol + 3; c++)
                if ((r != row || c != col) && board[r, c] == value)
                    return false;

        if (row == col)
            for (int i = 0; i < Size; i++)
                if (i != row && board[i, i] == value)
                    return false;

        if (row + col == Size - 1)
            for (int i = 0; i < Size; i++)
            {
                int diagonalCol = Size - 1 - i;
                if (i != row && board[i, diagonalCol] == value)
                    return false;
            }

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

        int boxRow = row / 3 * 3;
        int boxCol = col / 3 * 3;
        for (int r = boxRow; r < boxRow + 3; r++)
            for (int c = boxCol; c < boxCol + 3; c++)
                used |= ValueBit(board[r, c]);

        if (row == col)
            for (int i = 0; i < Size; i++)
                used |= ValueBit(board[i, i]);

        if (row + col == Size - 1)
            for (int i = 0; i < Size; i++)
                used |= ValueBit(board[i, Size - 1 - i]);

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
        var units = new int[29][];
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

        for (int boxRow = 0; boxRow < 3; boxRow++)
            for (int boxCol = 0; boxCol < 3; boxCol++)
            {
                units[unitIndex] = new int[Size];
                int index = 0;
                for (int r = 0; r < 3; r++)
                    for (int c = 0; c < 3; c++)
                        units[unitIndex][index++] = (boxRow * 3 + r) * Size + boxCol * 3 + c;
                unitIndex++;
            }

        units[unitIndex] = new int[Size];
        units[unitIndex + 1] = new int[Size];
        for (int i = 0; i < Size; i++)
        {
            units[unitIndex][i] = i * Size + i;
            units[unitIndex + 1][i] = i * Size + Size - 1 - i;
        }

        return units;
    }
}
