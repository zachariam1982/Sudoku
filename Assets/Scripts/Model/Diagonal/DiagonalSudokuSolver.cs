using System;
using System.Collections.Generic;

/// <summary>Solver and candidate service using all 29 Diagonal Sudoku units.</summary>
public sealed class DiagonalSudokuSolver : ISudokuSolver
{
    private const int RowOffset = 0;
    private const int ColumnOffset = 9;
    private const int BoxOffset = 18;
    private const int MainDiagonalIndex = 27;
    private const int AntiDiagonalIndex = 28;

    public bool HasUniqueSolution(int[,] puzzle) => CountSolutions(puzzle, 2) == 1;

    public int CountSolutions(int[,] board, int limit = 2)
    {
        ValidateDimensions(board);
        if (limit <= 0) return 0;

        int[,] working = (int[,])board.Clone();
        int[] masks = new int[29];
        if (!InitializeMasks(working, masks)) return 0;
        return CountSolutionsInPlace(working, masks, limit);
    }

    public List<int> GetCandidates(int[,] board, int row, int col)
    {
        ValidateDimensions(board);
        var candidates = new List<int>();
        int mask = DiagonalSudokuRules.GetCandidateMask(board, row, col);
        for (int value = 1; value <= 9; value++)
            if ((mask & (1 << value)) != 0) candidates.Add(value);
        return candidates;
    }

    public bool IsValid(int[,] board, int row, int col, int value) =>
        DiagonalSudokuRules.IsValidPlacement(board, row, col, value);

    private static bool InitializeMasks(int[,] board, int[] masks)
    {
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
            {
                int value = board[row, col];
                if (value == 0) continue;
                if (value < 1 || value > 9) return false;

                int bit = 1 << value;
                int box = BoxOffset + row / 3 * 3 + col / 3;
                if (HasMask(masks, row, col, box, bit)) return false;
                AddMasks(masks, row, col, box, bit);
            }
        return true;
    }

    private static int CountSolutionsInPlace(int[,] board, int[] masks, int limit)
    {
        int bestRow = -1, bestCol = -1, bestCandidates = 0, bestCount = 10;
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
            {
                if (board[row, col] != 0) continue;
                int box = BoxOffset + row / 3 * 3 + col / 3;
                int candidates = GetCandidatesMask(masks, row, col, box);
                int count = CountBits(candidates);
                if (count == 0) return 0;
                if (count >= bestCount) continue;
                bestRow = row;
                bestCol = col;
                bestCandidates = candidates;
                bestCount = count;
                if (count == 1) break;
            }

        if (bestRow < 0) return 1;

        int boxIndex = BoxOffset + bestRow / 3 * 3 + bestCol / 3;
        int solutions = 0;
        for (int value = 1; value <= 9; value++)
        {
            int bit = 1 << value;
            if ((bestCandidates & bit) == 0) continue;
            board[bestRow, bestCol] = value;
            AddMasks(masks, bestRow, bestCol, boxIndex, bit);
            solutions += CountSolutionsInPlace(board, masks, limit - solutions);
            RemoveMasks(masks, bestRow, bestCol, boxIndex, bit);
            board[bestRow, bestCol] = 0;
            if (solutions >= limit) return solutions;
        }
        return solutions;
    }

    private static int GetCandidatesMask(int[] masks, int row, int col, int box)
    {
        int used = masks[RowOffset + row] | masks[ColumnOffset + col] | masks[box];
        if (row == col) used |= masks[MainDiagonalIndex];
        if (row + col == 8) used |= masks[AntiDiagonalIndex];
        return DiagonalSudokuRules.FullMask & ~used;
    }

    private static bool HasMask(int[] masks, int row, int col, int box, int bit) =>
        (masks[row] & bit) != 0 ||
        (masks[ColumnOffset + col] & bit) != 0 ||
        (masks[box] & bit) != 0 ||
        (row == col && (masks[MainDiagonalIndex] & bit) != 0) ||
        (row + col == 8 && (masks[AntiDiagonalIndex] & bit) != 0);

    private static void AddMasks(int[] masks, int row, int col, int box, int bit)
    {
        masks[RowOffset + row] |= bit;
        masks[ColumnOffset + col] |= bit;
        masks[box] |= bit;
        if (row == col) masks[MainDiagonalIndex] |= bit;
        if (row + col == 8) masks[AntiDiagonalIndex] |= bit;
    }

    private static void RemoveMasks(int[] masks, int row, int col, int box, int bit)
    {
        masks[RowOffset + row] &= ~bit;
        masks[ColumnOffset + col] &= ~bit;
        masks[box] &= ~bit;
        if (row == col) masks[MainDiagonalIndex] &= ~bit;
        if (row + col == 8) masks[AntiDiagonalIndex] &= ~bit;
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
