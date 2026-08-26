using System;
using System.Collections.Generic;

public static class SudokuSolver
{
    public static bool HasUniqueSolution(int[,] puzzle)
    {
        int[,] copy = (int[,])puzzle.Clone();

        return CountSolutions(copy, 2) == 1;
    }

    public static int CountSolutions(
        int[,] board,
        int limit = 2)
    {
        int bestRow = -1;
        int bestCol = -1;

        List<int> bestCandidates = null;

        // Find empty cell with fewest possible candidates.
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (board[row, col] != 0)
                    continue;

                List<int> candidates =
                    GetCandidates(board, row, col);

                if (candidates.Count == 0)
                    return 0;

                if (bestCandidates == null ||
                    candidates.Count <
                    bestCandidates.Count)
                {
                    bestCandidates = candidates;
                    bestRow = row;
                    bestCol = col;

                    if (candidates.Count == 1)
                        break;
                }
            }

            if (bestCandidates != null &&
                bestCandidates.Count == 1)
            {
                break;
            }
        }

        // No empty cells: complete solution.
        if (bestRow == -1)
            return 1;

        int solutions = 0;

        foreach (int value in bestCandidates)
        {
            board[bestRow, bestCol] = value;

            solutions +=
                CountSolutions(
                    board,
                    limit - solutions);

            board[bestRow, bestCol] = 0;

            if (solutions >= limit)
                return solutions;
        }

        return solutions;
    }

    public static List<int> GetCandidates(
        int[,] board,
        int row,
        int col)
    {
        List<int> result =
            new List<int>();

        for (int value = 1;
             value <= 9;
             value++)
        {
            if (IsValid(
                    board,
                    row,
                    col,
                    value))
            {
                result.Add(value);
            }
        }

        return result;
    }

    public static bool IsValid(
        int[,] board,
        int row,
        int col,
        int value)
    {
        for (int i = 0; i < 9; i++)
        {
            if (board[row, i] == value)
                return false;

            if (board[i, col] == value)
                return false;
        }

        int startRow = (row / 3) * 3;
        int startCol = (col / 3) * 3;

        for (int r = startRow;
             r < startRow + 3;
             r++)
        {
            for (int c = startCol;
                 c < startCol + 3;
                 c++)
            {
                if (board[r, c] == value)
                    return false;
            }
        }

        return true;
    }
}