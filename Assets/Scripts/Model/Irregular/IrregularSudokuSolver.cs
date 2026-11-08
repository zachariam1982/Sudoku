using System;
using System.Collections.Generic;

/// <summary>Solver and candidate service using all 27 Irregular Sudoku units.</summary>
public sealed class IrregularSudokuSolver : ISudokuSolver
{
    private const int RowOffset = 0;
    private const int ColumnOffset = 9;
    private const int BoxOffset = 18;

    public bool HasUniqueSolution(int[,] puzzle) => CountSolutions(puzzle, 2) == 1;

    public int CountSolutions(int[,] board, int limit = 2)
    {
        ValidateDimensions(board);
        if (limit <= 0) return 0;

        int[,] working = (int[,])board.Clone();
        int[] masks = new int[27];
        if (!InitializeMasks(working, masks)) return 0;
        int nodesRemaining = int.MaxValue;
        return CountSolutionsInPlace(working, masks, limit, ref nodesRemaining);
    }

    // An exhausted search cannot establish uniqueness: reject this clue removal.
    internal bool HasUniqueSolutionWithinBudget(int[,] board, int nodeBudget)
    {
        ValidateDimensions(board);
        int[,] working = (int[,])board.Clone();
        int[] masks = new int[27];
        if (!InitializeMasks(working, masks)) return false;
        int remaining = nodeBudget;
        int count = CountSolutionsInPlace(working, masks, 2, ref remaining);
        return remaining >= 0 && count == 1;
    }

    internal bool TryFillSolution(int[,] board, Random random, int nodeBudget)
    {
        ValidateDimensions(board);
        var masks = new int[27];
        return InitializeMasks(board, masks) && Fill(board, masks, random, ref nodeBudget);
    }

    private static bool Fill(int[,] board, int[] masks, Random random, ref int remaining)
    {
        if (--remaining < 0) return false;
        int bestRow = -1, bestCol = -1, bestMask = 0, bestCount = 10;
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
            {
                if (board[row, col] != 0) continue;
                int mask = GetCandidatesMask(masks, row, col, BoxOffset + IrregularSudokuRules.GetRegion(row, col));
                int count = CountBits(mask);
                if (count == 0) return false;
                if (count >= bestCount) continue;
                bestRow = row; bestCol = col; bestMask = mask; bestCount = count;
            }
        if (bestRow < 0) return true;
        int box = BoxOffset + IrregularSudokuRules.GetRegion(bestRow, bestCol);
        while (bestMask != 0)
        {
            int choice = random.Next(CountBits(bestMask));
            int value = 1;
            for (; value <= 9; value++)
                if ((bestMask & (1 << value)) != 0 && choice-- == 0) break;
            int bit = 1 << value;
            bestMask &= ~bit;
            board[bestRow, bestCol] = value;
            AddMasks(masks, bestRow, bestCol, box, bit);
            if (Fill(board, masks, random, ref remaining)) return true;
            RemoveMasks(masks, bestRow, bestCol, box, bit);
            board[bestRow, bestCol] = 0;
            if (remaining < 0) return false;
        }
        return false;
    }

    public List<int> GetCandidates(int[,] board, int row, int col)
    {
        ValidateDimensions(board);
        var candidates = new List<int>();
        int mask = IrregularSudokuRules.GetCandidateMask(board, row, col);
        for (int value = 1; value <= 9; value++)
            if ((mask & (1 << value)) != 0) candidates.Add(value);
        return candidates;
    }

    public bool IsValid(int[,] board, int row, int col, int value) =>
        IrregularSudokuRules.IsValidPlacement(board, row, col, value);

    private static bool InitializeMasks(int[,] board, int[] masks)
    {
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
            {
                int value = board[row, col];
                if (value == 0) continue;
                if (value < 1 || value > 9) return false;

                int bit = 1 << value;
                int box = BoxOffset + IrregularSudokuRules.GetRegion(row, col);
                if (HasMask(masks, row, col, box, bit)) return false;
                AddMasks(masks, row, col, box, bit);
            }
        return true;
    }

    private static int CountSolutionsInPlace(int[,] board, int[] masks, int limit, ref int nodesRemaining)
    {
        if (--nodesRemaining < 0) return limit;

        // Propagate naked and hidden singles before branching. The trail is
        // undone on return so sibling searches reuse the same board and masks.
        int[] forcedCells = new int[81];
        int[] forcedValues = new int[81];
        int forcedCount = 0;
        bool contradiction = false;
        while (true)
        {
            int forcedCell = -1;
            int forcedValue = 0;
            for (int row = 0; row < 9 && forcedCell < 0; row++)
                for (int col = 0; col < 9; col++)
                {
                    if (board[row, col] != 0) continue;
                    int mask = GetCandidatesMask(masks, row, col, BoxOffset + IrregularSudokuRules.GetRegion(row, col));
                    if (mask == 0) { contradiction = true; break; }
                    if (CountBits(mask) == 1)
                    {
                        forcedCell = row * 9 + col;
                        forcedValue = FirstValue(mask);
                        break;
                    }
                }
            if (contradiction) break;

            if (forcedCell < 0)
            {
                foreach (int[] unit in IrregularSudokuRules.AllUnits)
                {
                    int present = 0;
                    foreach (int index in unit)
                    {
                        int value = board[index / 9, index % 9];
                        if (value > 0) present |= 1 << value;
                    }
                    for (int value = 1; value <= 9 && forcedCell < 0; value++)
                    {
                        int bit = 1 << value;
                        if ((present & bit) != 0) continue;
                        int found = -1;
                        int count = 0;
                        foreach (int index in unit)
                        {
                            int row = index / 9, col = index % 9;
                            if (board[row, col] == 0 &&
                                (GetCandidatesMask(masks, row, col, BoxOffset + IrregularSudokuRules.GetRegion(row, col)) & bit) != 0)
                            { found = index; if (++count > 1) break; }
                        }
                        if (count == 0) { contradiction = true; break; }
                        if (count == 1) { forcedCell = found; forcedValue = value; }
                    }
                    if (contradiction || forcedCell >= 0) break;
                }
            }
            if (contradiction || forcedCell < 0) break;

            int r = forcedCell / 9, c = forcedCell % 9;
            int bitValue = 1 << forcedValue;
            board[r, c] = forcedValue;
            AddMasks(masks, r, c, BoxOffset + IrregularSudokuRules.GetRegion(r, c), bitValue);
            forcedCells[forcedCount] = forcedCell;
            forcedValues[forcedCount++] = forcedValue;
        }

        int solutions = 0;
        if (!contradiction)
        {
            int bestRow = -1, bestCol = -1, bestMask = 0, bestCount = 10;
            for (int row = 0; row < 9; row++)
                for (int col = 0; col < 9; col++)
                {
                    if (board[row, col] != 0) continue;
                    int candidates = GetCandidatesMask(masks, row, col, BoxOffset + IrregularSudokuRules.GetRegion(row, col));
                    int count = CountBits(candidates);
                    if (count == 0) { contradiction = true; break; }
                    if (count < bestCount)
                    { bestRow = row; bestCol = col; bestMask = candidates; bestCount = count; }
                }

            if (!contradiction)
            {
                if (bestRow < 0) solutions = 1;
                else
                {
                    int box = BoxOffset + IrregularSudokuRules.GetRegion(bestRow, bestCol);
                    for (int value = 1; value <= 9; value++)
                    {
                        int bit = 1 << value;
                        if ((bestMask & bit) == 0) continue;
                        board[bestRow, bestCol] = value;
                        AddMasks(masks, bestRow, bestCol, box, bit);
                        solutions += CountSolutionsInPlace(board, masks, limit - solutions, ref nodesRemaining);
                        RemoveMasks(masks, bestRow, bestCol, box, bit);
                        board[bestRow, bestCol] = 0;
                        if (solutions >= limit) break;
                    }
                }
            }
        }

        for (int i = forcedCount - 1; i >= 0; i--)
        {
            int cell = forcedCells[i], row = cell / 9, col = cell % 9;
            int value = forcedValues[i];
            RemoveMasks(masks, row, col, BoxOffset + IrregularSudokuRules.GetRegion(row, col), 1 << value);
            board[row, col] = 0;
        }
        return solutions;
    }

    private static int FirstValue(int mask)
    {
        for (int value = 1; value <= 9; value++) if ((mask & (1 << value)) != 0) return value;
        return 0;
    }

    private static int GetCandidatesMask(int[] masks, int row, int col, int box)
    {
        int used = masks[RowOffset + row] | masks[ColumnOffset + col] | masks[box];
        return IrregularSudokuRules.FullMask & ~used;
    }

    private static bool HasMask(int[] masks, int row, int col, int box, int bit) =>
        (GetCandidatesMask(masks, row, col, box) & bit) == 0;

    private static void AddMasks(int[] masks, int row, int col, int box, int bit)
    {
        masks[RowOffset + row] |= bit;
        masks[ColumnOffset + col] |= bit;
        masks[box] |= bit;
    }

    private static void RemoveMasks(int[] masks, int row, int col, int box, int bit)
    {
        masks[RowOffset + row] &= ~bit;
        masks[ColumnOffset + col] &= ~bit;
        masks[box] &= ~bit;
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
