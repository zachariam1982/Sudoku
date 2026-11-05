using System;
using System.Collections.Generic;

/// <summary>MRV backtracking solver that enforces Killer cage sums and no-repeat rules.</summary>
public sealed class KillerSudokuSolver : ISudokuSolver
{
    public bool HasUniqueSolution(int[,] puzzle) => CountSolutions(puzzle, 2) == 1;

    public bool HasUniqueSolution(int[,] puzzle, KillerCage[] cages) => CountSolutions(puzzle, cages, 2) == 1;

    public int CountSolutions(int[,] board, int limit = 2) => CountSolutions(board, null, limit);

    public int CountSolutions(int[,] board, KillerCage[] cages, int limit = 2)
    {
        ValidateDimensions(board);
        if (limit <= 0 || !KillerSudokuRules.ValidateBoard(board, cages)) return 0;
        int[,] working = (int[,])board.Clone();
        return CountSolutionsInPlace(working, cages, limit);
    }

    public List<int> GetCandidates(int[,] board, int row, int col) => GetCandidates(board, null, row, col);

    public List<int> GetCandidates(int[,] board, KillerCage[] cages, int row, int col)
    {
        ValidateDimensions(board);
        var candidates = new List<int>();
        int mask = KillerSudokuRules.GetCandidateMask(board, cages, row, col);
        for (int value = 1; value <= 9; value++)
            if ((mask & (1 << value)) != 0) candidates.Add(value);
        return candidates;
    }

    public bool IsValid(int[,] board, int row, int col, int value) =>
        KillerSudokuRules.IsValidPlacement(board, null, row, col, value);

    public bool IsValid(int[,] board, KillerCage[] cages, int row, int col, int value) =>
        KillerSudokuRules.IsValidPlacement(board, cages, row, col, value);

    private static int CountSolutionsInPlace(int[,] board, KillerCage[] cages, int limit)
    {
        int bestRow = -1, bestCol = -1, bestMask = 0, bestCount = 10;
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
            {
                if (board[row, col] != 0) continue;
                int mask = KillerSudokuRules.GetCandidateMask(board, cages, row, col);
                int count = CountBits(mask);
                if (count == 0) return 0;
                if (count >= bestCount) continue;
                bestRow = row;
                bestCol = col;
                bestMask = mask;
                bestCount = count;
                if (count == 1) break;
            }

        if (bestRow < 0) return KillerSudokuRules.ValidateCompleteSolution(board, cages) ? 1 : 0;

        int solutions = 0;
        for (int value = 1; value <= 9; value++)
        {
            if ((bestMask & (1 << value)) == 0) continue;
            board[bestRow, bestCol] = value;
            solutions += CountSolutionsInPlace(board, cages, limit - solutions);
            board[bestRow, bestCol] = 0;
            if (solutions >= limit) return solutions;
        }
        return solutions;
    }

    private static int CountBits(int mask)
    {
        int count = 0;
        while (mask != 0) { mask &= mask - 1; count++; }
        return count;
    }

    private static void ValidateDimensions(int[,] board)
    {
        if (board == null) throw new ArgumentNullException(nameof(board));
        if (board.GetLength(0) != 9 || board.GetLength(1) != 9)
            throw new ArgumentException("A Sudoku board must be 9 by 9.", nameof(board));
    }
}
