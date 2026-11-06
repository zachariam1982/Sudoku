using System;
using System.Collections.Generic;

/// <summary>
/// MRV backtracking solver for classic Sudoku constraints plus Killer cage
/// sums and no-repeat rules. Cage layout data is compiled once per layout so
/// repeated uniqueness checks during puzzle generation stay inexpensive.
/// </summary>
public sealed class KillerSudokuSolver : ISudokuSolver
{
    private static readonly int[][][] CageDigitCombinations = BuildCageDigitCombinations();

    private KillerCage[] cachedCagesReference;
    private int[][] cachedCageCells = Array.Empty<int[]>();
    private int[] cachedCageTargets = Array.Empty<int>();
    private int[] cachedCellToCage = new int[81];
    private bool cachedLayoutValid;
    private bool hasCachedLayout;

    public bool HasUniqueSolution(int[,] puzzle) => CountSolutions(puzzle, 2) == 1;

    public bool HasUniqueSolution(int[,] puzzle, KillerCage[] cages) => CountSolutions(puzzle, cages, 2) == 1;

    public int CountSolutions(int[,] board, int limit = 2) => CountSolutions(board, null, limit);

    public int CountSolutions(int[,] board, KillerCage[] cages, int limit = 2)
    {
        ValidateDimensions(board);
        if (limit <= 0 || !GetCageLayout(cages)) return 0;

        var rowMasks = new int[9];
        var colMasks = new int[9];
        var boxMasks = new int[9];
        var cageMasks = new int[cachedCageCells.Length];
        var cageSums = new int[cachedCageCells.Length];
        var cageRemaining = new int[cachedCageCells.Length];
        for (int i = 0; i < cachedCageCells.Length; i++)
            cageRemaining[i] = cachedCageCells[i].Length;

        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
            {
                int value = board[row, col];
                if (value < 0 || value > 9) return 0;
                if (value == 0) continue;

                int bit = 1 << value;
                int box = row / 3 * 3 + col / 3;
                if ((rowMasks[row] & bit) != 0 || (colMasks[col] & bit) != 0 ||
                    (boxMasks[box] & bit) != 0)
                    return 0;

                rowMasks[row] |= bit;
                colMasks[col] |= bit;
                boxMasks[box] |= bit;

                int cageIndex = cachedCellToCage[row * 9 + col];
                if (cageIndex < 0) continue;
                if ((cageMasks[cageIndex] & bit) != 0) return 0;
                cageMasks[cageIndex] |= bit;
                cageSums[cageIndex] += value;
                cageRemaining[cageIndex]--;
                if (cageSums[cageIndex] > cachedCageTargets[cageIndex]) return 0;
            }

        for (int i = 0; i < cachedCageCells.Length; i++)
            if (!CanReachCageSum(cachedCageTargets[i], cageSums[i], cageMasks[i], cageRemaining[i]))
                return 0;

        int[,] working = (int[,])board.Clone();
        return CountSolutionsInPlace(
            working, limit, rowMasks, colMasks, boxMasks, cageMasks, cageSums, cageRemaining);
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

    private int CountSolutionsInPlace(
        int[,] board,
        int limit,
        int[] rowMasks,
        int[] colMasks,
        int[] boxMasks,
        int[] cageMasks,
        int[] cageSums,
        int[] cageRemaining)
    {
        int bestRow = -1, bestCol = -1, bestMask = 0, bestCount = 10;
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
            {
                if (board[row, col] != 0) continue;
                int mask = GetCandidateMask(
                    row, col, rowMasks, colMasks, boxMasks, cageMasks, cageSums, cageRemaining);
                int count = CountBits(mask);
                if (count == 0) return 0;
                if (count >= bestCount) continue;
                bestRow = row;
                bestCol = col;
                bestMask = mask;
                bestCount = count;
                if (count == 1) break;
            }

        if (bestRow < 0) return 1;

        int solutions = 0;
        int box = bestRow / 3 * 3 + bestCol / 3;
        int cageIndex = cachedCellToCage[bestRow * 9 + bestCol];
        for (int value = 1; value <= 9; value++)
        {
            int bit = 1 << value;
            if ((bestMask & bit) == 0) continue;

            board[bestRow, bestCol] = value;
            rowMasks[bestRow] |= bit;
            colMasks[bestCol] |= bit;
            boxMasks[box] |= bit;
            if (cageIndex >= 0)
            {
                cageMasks[cageIndex] |= bit;
                cageSums[cageIndex] += value;
                cageRemaining[cageIndex]--;
            }

            solutions += CountSolutionsInPlace(
                board, limit - solutions, rowMasks, colMasks, boxMasks,
                cageMasks, cageSums, cageRemaining);

            board[bestRow, bestCol] = 0;
            rowMasks[bestRow] &= ~bit;
            colMasks[bestCol] &= ~bit;
            boxMasks[box] &= ~bit;
            if (cageIndex >= 0)
            {
                cageMasks[cageIndex] &= ~bit;
                cageSums[cageIndex] -= value;
                cageRemaining[cageIndex]++;
            }
            if (solutions >= limit) return solutions;
        }
        return solutions;
    }

    private int GetCandidateMask(
        int row,
        int col,
        int[] rowMasks,
        int[] colMasks,
        int[] boxMasks,
        int[] cageMasks,
        int[] cageSums,
        int[] cageRemaining)
    {
        int box = row / 3 * 3 + col / 3;
        int mask = KillerSudokuRules.FullMask & ~(rowMasks[row] | colMasks[col] | boxMasks[box]);
        int cageIndex = cachedCellToCage[row * 9 + col];
        if (cageIndex < 0) return mask;

        mask &= ~cageMasks[cageIndex];
        int availableAfter = cageRemaining[cageIndex] - 1;
        for (int value = 1; value <= 9; value++)
        {
            int bit = 1 << value;
            if ((mask & bit) != 0 && !CanReachCageSum(
                    cachedCageTargets[cageIndex], cageSums[cageIndex] + value,
                    cageMasks[cageIndex] | bit, availableAfter))
                mask &= ~bit;
        }
        return mask;
    }

    private bool GetCageLayout(KillerCage[] cages)
    {
        if (hasCachedLayout && ReferenceEquals(cages, cachedCagesReference))
            return cachedLayoutValid;

        cachedCagesReference = cages;
        hasCachedLayout = true;
        for (int cell = 0; cell < cachedCellToCage.Length; cell++)
            cachedCellToCage[cell] = -1;
        if (cages == null || cages.Length == 0)
        {
            cachedCageCells = Array.Empty<int[]>();
            cachedCageTargets = Array.Empty<int>();
            cachedLayoutValid = cages == null || cages.Length == 0;
            return cachedLayoutValid;
        }

        cachedLayoutValid = KillerSudokuRules.ValidateCageLayout(cages);
        if (!cachedLayoutValid)
        {
            cachedCageCells = Array.Empty<int[]>();
            cachedCageTargets = Array.Empty<int>();
            return false;
        }

        cachedCageCells = new int[cages.Length][];
        cachedCageTargets = new int[cages.Length];
        for (int cageIndex = 0; cageIndex < cages.Length; cageIndex++)
        {
            cachedCageCells[cageIndex] = (int[])cages[cageIndex].Cells.Clone();
            cachedCageTargets[cageIndex] = cages[cageIndex].TargetSum;
            foreach (int cell in cachedCageCells[cageIndex]) cachedCellToCage[cell] = cageIndex;
        }
        return true;
    }

    private static bool CanReachCageSum(int target, int sum, int usedMask, int remaining)
    {
        if (remaining < 0 || remaining > 9) return false;
        int neededSum = target - sum;
        if (neededSum < 0 || neededSum > 45) return false;

        int[] combinations = CageDigitCombinations[remaining][neededSum];
        for (int i = 0; i < combinations.Length; i++)
            if ((combinations[i] & usedMask) == 0) return true;
        return false;
    }

    private static int[][][] BuildCageDigitCombinations()
    {
        var lists = new List<int>[10, 46];
        for (int mask = 0; mask < (1 << 10); mask++)
        {
            if ((mask & 1) != 0) continue;
            int count = CountBits(mask);
            if (count > 9) continue;
            int sum = 0;
            for (int value = 1; value <= 9; value++)
                if ((mask & (1 << value)) != 0) sum += value;
            if (lists[count, sum] == null) lists[count, sum] = new List<int>();
            lists[count, sum].Add(mask);
        }

        var table = new int[10][][];
        for (int count = 0; count <= 9; count++)
        {
            table[count] = new int[46][];
            for (int sum = 0; sum <= 45; sum++)
                table[count][sum] = lists[count, sum] == null
                    ? Array.Empty<int>()
                    : lists[count, sum].ToArray();
        }
        return table;
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
