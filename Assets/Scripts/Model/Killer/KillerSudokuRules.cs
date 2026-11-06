using System;
using System.Collections.Generic;

/// <summary>Standard Sudoku constraints plus Killer cage uniqueness and sums.</summary>
internal static class KillerSudokuRules
{
    internal const int Size = 9;
    internal const int FullMask = 0x3FE;
    private static readonly int[][] Units = BuildUnits();
    internal static int[][] AllUnits => Units;

    internal static bool IsValidPlacement(int[,] board, KillerCage[] cages, int row, int col, int value)
    {
        if (!HasDimensions(board) || row < 0 || row >= Size || col < 0 || col >= Size || value < 1 || value > 9)
            return false;
        int[,] candidateBoard = (int[,])board.Clone();
        candidateBoard[row, col] = 0;
        return (GetCandidateMask(candidateBoard, cages, row, col) & Bit(value)) != 0;
    }

    internal static int GetCandidateMask(int[,] board, KillerCage[] cages, int row, int col)
    {
        if (!HasDimensions(board) || row < 0 || row >= Size || col < 0 || col >= Size || board[row, col] != 0)
            return 0;

        int mask = GetClassicCandidateMask(board, row, col);
        KillerCage cage = FindCage(cages, row * Size + col);
        if (cage == null) return mask;

        int allowed = 0;
        for (int value = 1; value <= Size; value++)
            if ((mask & Bit(value)) != 0 && HasFeasibleCageCompletion(board, cage, row * Size + col, value))
                allowed |= Bit(value);
        return allowed;
    }

    internal static bool ValidateBoard(int[,] board, KillerCage[] cages)
    {
        if (!HasDimensions(board)) return false;
        for (int row = 0; row < Size; row++)
            for (int col = 0; col < Size; col++)
            {
                int value = board[row, col];
                if (value < 0 || value > 9) return false;
                if (value != 0 && HasClassicConflict(board, row, col, value)) return false;
            }

        if (cages != null && cages.Length > 0)
        {
            if (!ValidateCageLayout(cages)) return false;
            foreach (KillerCage cage in cages)
                if (!HasFeasibleCageCompletion(board, cage, -1, 0)) return false;
        }
        return true;
    }

    internal static bool ValidateCompleteSolution(int[,] board, KillerCage[] cages)
    {
        if (!ValidateBoard(board, cages)) return false;
        for (int row = 0; row < Size; row++)
            for (int col = 0; col < Size; col++)
                if (board[row, col] == 0) return false;
        if (cages == null || cages.Length == 0) return true;
        foreach (KillerCage cage in cages)
        {
            int sum = 0;
            int used = 0;
            foreach (int cell in cage.Cells)
            {
                int value = board[cell / Size, cell % Size];
                if ((used & Bit(value)) != 0) return false;
                used |= Bit(value);
                sum += value;
            }
            if (sum != cage.TargetSum) return false;
        }
        return true;
    }

    internal static bool ValidateCageLayout(KillerCage[] cages)
    {
        if (cages == null || cages.Length == 0) return false;
        bool[] covered = new bool[Size * Size];
        foreach (KillerCage cage in cages)
        {
            if (cage == null || cage.Cells == null || cage.Cells.Length == 0 || cage.Cells.Length > Size)
                return false;
            int min = cage.Cells.Length * (cage.Cells.Length + 1) / 2;
            int max = cage.Cells.Length * (19 - cage.Cells.Length) / 2;
            if (cage.TargetSum < min || cage.TargetSum > max) return false;
            foreach (int cell in cage.Cells)
            {
                if (cell < 0 || cell >= covered.Length || covered[cell]) return false;
                covered[cell] = true;
            }
            if (!IsOrthogonallyConnected(cage.Cells)) return false;
        }
        foreach (bool cellCovered in covered) if (!cellCovered) return false;
        return true;
    }

    internal static int GetClassicCandidateMask(int[,] board, int row, int col)
    {
        if (!HasDimensions(board) || row < 0 || row >= Size || col < 0 || col >= Size || board[row, col] != 0)
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
            for (int c = boxCol; c < boxCol + 3; c++) used |= ValueBit(board[r, c]);
        return FullMask & ~used;
    }

    internal static KillerCage FindCage(KillerCage[] cages, int cell)
    {
        if (cages == null) return null;
        foreach (KillerCage cage in cages)
            if (cage != null && cage.Cells != null)
                foreach (int candidate in cage.Cells)
                    if (candidate == cell) return cage;
        return null;
    }

    private static bool HasFeasibleCageCompletion(int[,] board, KillerCage cage, int overrideCell, int overrideValue)
    {
        int originalValue = 0;
        if (overrideCell >= 0)
        {
            int row = overrideCell / Size;
            int col = overrideCell % Size;
            originalValue = board[row, col];
            board[row, col] = overrideValue;
        }

        int sum = 0;
        int used = 0;
        foreach (int cell in cage.Cells)
        {
            int value = board[cell / Size, cell % Size];
            if (value == 0) continue;
            if (value < 1 || value > 9 || (used & Bit(value)) != 0)
            {
                if (overrideCell >= 0) board[overrideCell / Size, overrideCell % Size] = originalValue;
                return false;
            }
            used |= Bit(value);
            sum += value;
        }

        int remaining = 0;
        foreach (int cell in cage.Cells)
            if (board[cell / Size, cell % Size] == 0) remaining++;
        bool feasible = CanCompleteCage(board, cage, cage.TargetSum, sum, used, remaining);
        if (overrideCell >= 0) board[overrideCell / Size, overrideCell % Size] = originalValue;
        return feasible;
    }

    private static bool CanCompleteCage(
        int[,] board, KillerCage cage, int target, int sum, int used, int remaining)
    {
        if (remaining == 0) return sum == target;
        int minAvailable = MinimumAvailableSum(used, remaining);
        int maxAvailable = MaximumAvailableSum(used, remaining);
        if (sum >= target || minAvailable == int.MaxValue || maxAvailable == int.MinValue ||
            sum + minAvailable > target || sum + maxAvailable < target) return false;

        int bestCell = -1;
        int candidates = 0;
        int bestCount = 10;
        foreach (int cell in cage.Cells)
        {
            int row = cell / Size;
            int col = cell % Size;
            if (board[row, col] != 0) continue;
            int cellCandidates = GetClassicCandidateMask(board, row, col) & ~used;
            int count = CountBits(cellCandidates);
            if (count == 0) return false;
            if (count >= bestCount) continue;
            bestCell = cell;
            candidates = cellCandidates;
            bestCount = count;
            if (count == 1) break;
        }
        if (bestCell < 0) return sum == target;

        int row = bestCell / Size;
        int col = bestCell % Size;
        for (int value = 1; value <= 9; value++)
        {
            int bit = Bit(value);
            if ((candidates & bit) == 0) continue;
            board[row, col] = value;
            bool completes = CanCompleteCage(board, cage, target, sum + value, used | bit, remaining - 1);
            board[row, col] = 0;
            if (completes) return true;
        }
        return false;
    }

    private static int CountBits(int mask)
    {
        int count = 0;
        while (mask != 0) { mask &= mask - 1; count++; }
        return count;
    }

    private static int MinimumAvailableSum(int used, int count)
    {
        int sum = 0;
        for (int value = 1; value <= 9 && count > 0; value++)
            if ((used & Bit(value)) == 0) { sum += value; count--; }
        return count == 0 ? sum : int.MaxValue;
    }

    private static int MaximumAvailableSum(int used, int count)
    {
        int sum = 0;
        for (int value = 9; value >= 1 && count > 0; value--)
            if ((used & Bit(value)) == 0) { sum += value; count--; }
        return count == 0 ? sum : int.MinValue;
    }

    private static bool HasClassicConflict(int[,] board, int row, int col, int value)
    {
        for (int i = 0; i < Size; i++)
            if ((i != col && board[row, i] == value) || (i != row && board[i, col] == value))
                return true;
        int boxRow = row / 3 * 3;
        int boxCol = col / 3 * 3;
        for (int r = boxRow; r < boxRow + 3; r++)
            for (int c = boxCol; c < boxCol + 3; c++)
                if ((r != row || c != col) && board[r, c] == value) return true;
        return false;
    }

    private static bool IsOrthogonallyConnected(int[] cells)
    {
        var set = new HashSet<int>(cells);
        var reached = new HashSet<int> { cells[0] };
        var pending = new Queue<int>();
        pending.Enqueue(cells[0]);
        while (pending.Count > 0)
        {
            int cell = pending.Dequeue();
            int row = cell / Size;
            int col = cell % Size;
            int[] neighbors = { row > 0 ? cell - Size : -1, row < 8 ? cell + Size : -1,
                                col > 0 ? cell - 1 : -1, col < 8 ? cell + 1 : -1 };
            foreach (int neighbor in neighbors)
                if (set.Contains(neighbor) && reached.Add(neighbor)) pending.Enqueue(neighbor);
        }
        return reached.Count == set.Count;
    }

    private static int[][] BuildUnits()
    {
        var units = new int[27][];
        int index = 0;
        for (int row = 0; row < Size; row++)
        {
            units[index] = new int[Size];
            for (int col = 0; col < Size; col++) units[index][col] = row * Size + col;
            index++;
        }
        for (int col = 0; col < Size; col++)
        {
            units[index] = new int[Size];
            for (int row = 0; row < Size; row++) units[index][row] = row * Size + col;
            index++;
        }
        for (int boxRow = 0; boxRow < 3; boxRow++)
            for (int boxCol = 0; boxCol < 3; boxCol++)
            {
                units[index] = new int[Size];
                int cell = 0;
                for (int row = 0; row < 3; row++)
                    for (int col = 0; col < 3; col++)
                        units[index][cell++] = (boxRow * 3 + row) * Size + boxCol * 3 + col;
                index++;
            }
        return units;
    }

    private static bool HasDimensions(int[,] board) =>
        board != null && board.GetLength(0) == Size && board.GetLength(1) == Size;
    private static int Bit(int value) => 1 << value;
    private static int ValueBit(int value) => value >= 1 && value <= 9 ? Bit(value) : 0;
}
