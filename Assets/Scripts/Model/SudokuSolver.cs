using System;
using System.Collections.Generic;

public static class SudokuSolver
{
    private const int FullCandidateMask = 0x3FE; // Bits 1 through 9.
    private const int ColumnMaskOffset = 9;
    private const int BoxMaskOffset = 18;

    public static bool HasUniqueSolution(int[,] puzzle) =>
        CountSolutions(puzzle, 2) == 1;

    /// <summary>
    /// Counts solutions up to <paramref name="limit"/> without changing the caller's board.
    /// Candidate checks use row, column, and box bit masks to avoid allocating candidate
    /// lists at every recursive search node.
    /// </summary>
    public static int CountSolutions(int[,] board, int limit = 2)
    {
        if (board == null) throw new ArgumentNullException(nameof(board));
        if (board.GetLength(0) != 9 || board.GetLength(1) != 9)
            throw new ArgumentException("A Sudoku board must be 9 by 9.", nameof(board));
        if (limit <= 0) return 0;

        int[,] workingBoard = (int[,])board.Clone();
        int[] masks = new int[27];

        if (!InitializeMasks(workingBoard, masks))
            return 0;

        return CountSolutionsInPlace(workingBoard, masks, limit);
    }

    private static bool InitializeMasks(int[,] board, int[] masks)
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                int value = board[row, col];
                if (value == 0) continue;
                if (value < 1 || value > 9) return false;

                int bit = 1 << value;
                int rowIndex = row;
                int columnIndex = ColumnMaskOffset + col;
                int boxIndex = BoxMaskOffset + (row / 3) * 3 + col / 3;

                if ((masks[rowIndex] & bit) != 0 ||
                    (masks[columnIndex] & bit) != 0 ||
                    (masks[boxIndex] & bit) != 0)
                {
                    return false;
                }

                masks[rowIndex] |= bit;
                masks[columnIndex] |= bit;
                masks[boxIndex] |= bit;
            }
        }

        return true;
    }

    private static int CountSolutionsInPlace(int[,] board, int[] masks, int limit)
    {
        int bestRow = -1;
        int bestCol = -1;
        int bestCandidates = 0;
        int bestCandidateCount = 10;

        // Minimum remaining values: branch on the empty cell with fewest candidates.
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (board[row, col] != 0) continue;

                int boxIndex = BoxMaskOffset + (row / 3) * 3 + col / 3;
                int candidates = FullCandidateMask &
                    ~(masks[row] | masks[ColumnMaskOffset + col] | masks[boxIndex]);

                int candidateCount = CountBits(candidates);
                if (candidateCount == 0) return 0;

                if (candidateCount < bestCandidateCount)
                {
                    bestRow = row;
                    bestCol = col;
                    bestCandidates = candidates;
                    bestCandidateCount = candidateCount;

                    if (candidateCount == 1) break;
                }
            }

            if (bestCandidateCount == 1) break;
        }

        // No empty cells remain, so the givens and placements form one solution.
        if (bestRow < 0) return 1;

        int solutions = 0;
        int rowMaskIndex = bestRow;
        int columnMaskIndex = ColumnMaskOffset + bestCol;
        int boxMaskIndex = BoxMaskOffset + (bestRow / 3) * 3 + bestCol / 3;

        for (int value = 1; value <= 9; value++)
        {
            int bit = 1 << value;
            if ((bestCandidates & bit) == 0) continue;

            board[bestRow, bestCol] = value;
            masks[rowMaskIndex] |= bit;
            masks[columnMaskIndex] |= bit;
            masks[boxMaskIndex] |= bit;

            solutions += CountSolutionsInPlace(board, masks, limit - solutions);

            // Restore shared search state before returning or trying another value.
            masks[rowMaskIndex] &= ~bit;
            masks[columnMaskIndex] &= ~bit;
            masks[boxMaskIndex] &= ~bit;
            board[bestRow, bestCol] = 0;

            if (solutions >= limit) return solutions;
        }

        return solutions;
    }

    private static int CountBits(int mask)
    {
        int count = 0;
        while (mask != 0)
        {
            mask &= mask - 1;
            count++;
        }

        return count;
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
