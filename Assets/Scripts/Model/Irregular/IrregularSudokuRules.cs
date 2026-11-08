using System;
using System.Collections.Generic;

/// <summary>Per-puzzle row, column, and irregular rules for Irregular Sudoku.</summary>
internal sealed class IrregularSudokuRules
{
    internal const int Size = 9;
    internal const int FullMask = 0x3FE;
    private static readonly int[] Left = {11, 14, 38, 41, 65, 68};
    private static readonly int[] Right = {21, 24, 48, 51, 75, 78};
    internal const int CurrentLayoutVersion = 1;
    private readonly int[] regions;
    private readonly int[][] Units;
    private readonly int[,] witness;
    private static readonly int[] LegacyWitness = {
        1,2,3,4,5,6,7,8,9, 8,9,1,3,7,2,4,5,6, 4,6,7,5,2,8,9,1,3,
        2,1,4,9,6,7,5,3,8, 6,3,5,2,8,9,1,4,7, 5,7,9,8,1,4,3,6,2,
        3,4,2,6,9,1,8,7,5, 9,5,8,7,4,3,6,2,1, 7,8,6,1,3,5,2,9,4 };

    internal IrregularSudokuRules(int seed, int version = CurrentLayoutVersion)
    {
        if (version < 0 || version > CurrentLayoutVersion)
            throw new ArgumentOutOfRangeException(nameof(version));
        regions = new int[81];
        witness = new int[9, 9];
        for (int i = 0; i < 81; i++) witness[i / 9, i % 9] = LegacyWitness[i];
        for (int i = 0; i < 81; i++) regions[i] = i / 27 * 3 + i % 9 / 3;
        for (int i = 0; i < Left.Length; i++) Swap(Left[i], Right[i]);
        if (version == 1)
        {
            // Explicit PRNG: independent of System.Random/runtime and of puzzle retries/difficulty.
            uint state = unchecked((uint)seed) ^ 0x9E3779B9u;
            if (state == 0) state = 0xA341316Cu;
            var visited = new int[81];
            var queue = new int[81];
            int stamp = 0;
            var baseline = (int[])regions.Clone();
            // Bound topology work. Reject unsatisfiable or expensive layouts before clue generation.
            for (int candidate = 0; candidate < 8; candidate++)
            {
                Array.Copy(baseline, regions, 81);
                int accepted = 0;
                for (int attempt = 0; attempt < 2048 && accepted < 8 - candidate; attempt++)
                {
                    int a = (int)(Next(ref state) % 81);
                    int b = (int)(Next(ref state) % 81);
                    if (regions[a] == regions[b]) continue;
                    int first = regions[a], second = regions[b];
                    Swap(a, b);
                    if (!Connected(first, visited, queue, ++stamp) ||
                        !Connected(second, visited, queue, ++stamp)) Swap(a, b);
                    else accepted++;
                }
                Units = BuildUnits();
                var solver = new IrregularSudokuSolver(this);
                var solution = new int[9, 9];
                if (solver.TryFillSolution(solution, new LayoutRandom(Next(ref state)), 5000))
                { witness = solution; break; }
                // The original connected layout is known to be solvable and is a bounded fallback.
                Array.Copy(baseline, regions, 81);
            }
            int transform = (int)(Next(ref state) % 8);
            var copy = (int[])regions.Clone();
            var solutionCopy = (int[,])witness.Clone();
            for (int i = 0; i < 81; i++)
            {
                int row = i / 9, col = i % 9;
                if ((transform & 4) != 0) col = 8 - col;
                for (int turn = 0; turn < (transform & 3); turn++)
                { int oldRow = row; row = col; col = 8 - oldRow; }
                regions[row * 9 + col] = copy[i];
                witness[row, col] = solutionCopy[i / 9, i % 9];
            }
        }
        Units = BuildUnits();
    }

    // Reuse the proven solution instead of repeating an unbounded empty-board search.
    internal int[,] CreateSolution(Random random)
    {
        var digits = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        for (int i = 9; i > 1; i--)
        {
            int j = random.Next(i) + 1;
            int old = digits[i]; digits[i] = digits[j]; digits[j] = old;
        }
        var solution = new int[9, 9];
        for (int i = 0; i < 81; i++) solution[i / 9, i % 9] = digits[witness[i / 9, i % 9]];
        return solution;
    }

    private sealed class LayoutRandom : Random
    {
        private uint state;
        internal LayoutRandom(uint seed) { state = seed == 0 ? 1u : seed; }
        public override int Next(int maxValue) => (int)(IrregularSudokuRules.Next(ref state) % (uint)maxValue);
    }

    private static uint Next(ref uint state)
    {
        state ^= state << 13; state ^= state >> 17; state ^= state << 5;
        return state;
    }

    private void Swap(int a, int b)
    { int value = regions[a]; regions[a] = regions[b]; regions[b] = value; }

    private bool Connected(int region, int[] visited, int[] queue, int stamp)
    {
        int start = Array.IndexOf(regions, region), head = 0, tail = 1;
        queue[0] = start; visited[start] = stamp;
        while (head < tail)
        {
            int cell = queue[head++], row = cell / 9, col = cell % 9;
            for (int direction = 0; direction < 4; direction++)
            {
                int next = direction == 0 ? cell - 9 : direction == 1 ? cell + 9 :
                    direction == 2 ? cell - 1 : cell + 1;
                if ((direction == 0 && row == 0) || (direction == 1 && row == 8) ||
                    (direction == 2 && col == 0) || (direction == 3 && col == 8)) continue;
                if (regions[next] != region || visited[next] == stamp) continue;
                visited[next] = stamp; queue[tail++] = next;
            }
        }
        return tail == 9;
    }

    internal int GetRegion(int row, int col) => regions[row * 9 + col];
    internal int[][] AllUnits => Units;

    internal bool IsValidPlacement(int[,] board, int row, int col, int value)
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

    internal int GetCandidateMask(int[,] board, int row, int col)
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

    internal bool ValidateBoard(int[,] board)
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

    internal bool ValidateCompleteSolution(int[,] board)
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

    private int[][] BuildUnits()
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
