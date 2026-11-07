using System;
using System.Collections.Generic;
/// <summary>Native Thermo MRV search. Every branch respects bulb-to-tip inequalities.</summary>
public sealed class ThermoSudokuSolver : ISudokuSolver
{
    public bool HasUniqueSolution(int[,] puzzle) => CountSolutions(puzzle) == 1;
    public bool HasUniqueSolution(int[,] puzzle, Thermometer[] paths) => CountSolutions(puzzle, paths, 2) == 1;
    public int CountSolutions(int[,] board, int limit = 2) => CountSolutions(board, null, limit);
    private Thermometer[] cachedPaths;
    private List<int[]>[] pathsByCell;

    // Exhausting this budget rejects the removal; it never certifies uniqueness.
    internal bool HasUniqueSolutionWithinBudget(int[,] board, Thermometer[] paths, int nodeBudget)
    {
        bool exhausted;
        return Count(board, paths, 2, nodeBudget, out exhausted) == 1 && !exhausted;
    }

    public int CountSolutions(int[,] board, Thermometer[] paths, int limit = 2)
    {
        bool exhausted;
        return Count(board, paths, limit, int.MaxValue, out exhausted);
    }

    private int Count(int[,] board, Thermometer[] paths, int limit, int budget, out bool exhausted)
    {
        exhausted = false;
        if (!ThermoSudokuRules.ValidateBoard(board, paths) || limit < 1) return 0;
        if (!ReferenceEquals(cachedPaths, paths) || pathsByCell == null)
        {
            pathsByCell = new List<int[]>[81];
            for (int i = 0; i < 81; i++) pathsByCell[i] = new List<int[]>();
            foreach (var path in paths)
                foreach (int cell in path.Cells) pathsByCell[cell].Add(path.Cells);
            cachedPaths = paths;
        }
        var values = new int[81];
        var rows = new int[9]; var cols = new int[9]; var boxes = new int[9];
        for (int i = 0; i < 81; i++)
        {
            int value = board[i / 9, i % 9]; values[i] = value;
            if (value == 0) continue;
            int bit = 1 << value;
            rows[i / 9] |= bit; cols[i % 9] |= bit; boxes[i / 27 * 3 + i % 9 / 3] |= bit;
        }
        return Search(values, rows, cols, boxes, limit, ref budget, ref exhausted);
    }

    private int Search(int[] values, int[] rows, int[] cols, int[] boxes,
        int limit, ref int budget, ref bool exhausted)
    {
        if (budget-- <= 0) { exhausted = true; return 0; }
        int chosen = -1, mask = 0, best = 10;
        for (int cell = 0; cell < 81; cell++)
        {
            if (values[cell] != 0) continue;
            int row = cell / 9, col = cell % 9;
            int candidates = ThermoSudokuRules.FullMask & ~(rows[row] | cols[col] | boxes[row / 3 * 3 + col / 3]);
            int lower = 1, upper = 9;
            foreach (int[] path in pathsByCell[cell])
            {
                int position = Array.IndexOf(path, cell);
                lower = Math.Max(lower, position + 1);
                upper = Math.Min(upper, 9 - (path.Length - 1 - position));
                for (int j = 0; j < path.Length; j++)
                {
                    int value = values[path[j]];
                    if (value == 0) continue;
                    if (j < position) lower = Math.Max(lower, value + position - j);
                    if (j > position) upper = Math.Min(upper, value - (j - position));
                }
            }
            if (lower > upper) return 0;
            candidates &= ((1 << (upper + 1)) - 1) & ~((1 << lower) - 1);
            int count = 0;
            for (int bits = candidates; bits != 0; bits &= bits - 1) count++;
            if (count == 0) return 0;
            if (count < best) { chosen = cell; mask = candidates; best = count; }
            if (count == 1) break;
        }
        if (chosen < 0) return 1;
        int r = chosen / 9, c = chosen % 9, box = r / 3 * 3 + c / 3, solutions = 0;
        for (int value = 1; value <= 9 && solutions < limit; value++)
        {
            int bit = 1 << value;
            if ((mask & bit) == 0) continue;
            values[chosen] = value; rows[r] |= bit; cols[c] |= bit; boxes[box] |= bit;
            solutions += Search(values, rows, cols, boxes, limit - solutions, ref budget, ref exhausted);
            values[chosen] = 0; rows[r] &= ~bit; cols[c] &= ~bit; boxes[box] &= ~bit;
            if (exhausted) break;
        }
        return solutions;
    }
    public List<int> GetCandidates(int[,] board, int row, int col) => GetCandidates(board, null, row, col);
    public List<int> GetCandidates(int[,] board, Thermometer[] paths, int row, int col)
    {
        var values = new List<int>();
        int mask = ThermoSudokuRules.GetCandidateMask(board, paths, row, col);
        for (int v = 1; v <= 9; v++) if ((mask & (1 << v)) != 0) values.Add(v);
        return values;
    }
    public bool IsValid(int[,] board, int row, int col, int value) => ThermoSudokuRules.IsValidPlacement(board, null, row, col, value);
}
