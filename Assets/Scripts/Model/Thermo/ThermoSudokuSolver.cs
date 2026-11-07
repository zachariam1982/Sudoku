using System;
using System.Collections.Generic;
/// <summary>Native Thermo MRV search. Every branch respects bulb-to-tip inequalities.</summary>
public sealed class ThermoSudokuSolver : ISudokuSolver
{
    public bool HasUniqueSolution(int[,] puzzle) => CountSolutions(puzzle) == 1;
    public bool HasUniqueSolution(int[,] puzzle, Thermometer[] paths) => CountSolutions(puzzle, paths, 2) == 1;
    public int CountSolutions(int[,] board, int limit = 2) => CountSolutions(board, null, limit);
    public int CountSolutions(int[,] board, Thermometer[] paths, int limit = 2)
    {
        if (!ThermoSudokuRules.ValidateBoard(board, paths) || limit < 1) return 0;
        return Search((int[,])board.Clone(), paths, limit);
    }
    private static int Search(int[,] board, Thermometer[] paths, int limit)
    {
        int row = -1, col = -1, mask = 0, best = 10;
        for (int r = 0; r < 9; r++) for (int c = 0; c < 9; c++)
        {
            if (board[r,c] != 0) continue;
            int candidates = ThermoSudokuRules.GetCandidateMask(board, paths, r, c), count = 0;
            for (int bits = candidates; bits != 0; bits &= bits-1) count++;
            if (count == 0) return 0;
            if (count < best) { row = r; col = c; mask = candidates; best = count; }
        }
        if (row < 0) return 1;
        int solutions = 0;
        for (int value = 1; value <= 9 && solutions < limit; value++)
            if ((mask & (1 << value)) != 0)
            {
                board[row,col] = value;
                solutions += Search(board, paths, limit - solutions);
            }
        board[row,col] = 0;
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
