using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Deterministic, bounded generator for uniquely solvable Diagonal Sudoku.</summary>
public sealed class DiagonalSudokuGenerator : ISudokuGenerator
{
    private readonly DiagonalSudokuSolver _solver = new DiagonalSudokuSolver();
    private readonly DiagonalSudokuDifficultyAnalyzer _analyzer = new DiagonalSudokuDifficultyAnalyzer();

    public SudokuResult Generate(int level, SudokuDifficulty requestedDifficulty, int maxAttempts = 100) =>
        GenerateSudoku(level, requestedDifficulty, maxAttempts);

    public SudokuResult GenerateSudoku(int level, SudokuDifficulty requestedDifficulty, int maxAttempts = 100)
    {
        if (!Enum.IsDefined(typeof(SudokuDifficulty), requestedDifficulty))
            throw new ArgumentOutOfRangeException(nameof(requestedDifficulty));
        if (maxAttempts <= 0) throw new ArgumentOutOfRangeException(nameof(maxAttempts));

        for (int tier = (int)requestedDifficulty; tier >= (int)SudokuDifficulty.Simple; tier--)
        {
            SudokuDifficulty target = (SudokuDifficulty)tier;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                int seed = unchecked(level * 397 ^ ((tier + 1) * 7919) ^ attempt * 104729 ^ 0xD1A60A1);
                var random = new Random(seed);
                int[,] solution = new int[9, 9];
                if (!FillSolution(solution, random)) continue;

                int[,] puzzle = (int[,])solution.Clone();
                int[] cells = Enumerable.Range(0, 81).OrderBy(_ => random.Next()).ToArray();
                int clueCount = 81;
                (int minClues, int maxClues) = GetClueRange(target);

                foreach (int cell in cells)
                {
                    if (clueCount <= minClues) break;
                    int row = cell / 9;
                    int col = cell % 9;
                    int oldValue = puzzle[row, col];
                    puzzle[row, col] = 0;
                    if (!_solver.HasUniqueSolution(puzzle))
                    {
                        puzzle[row, col] = oldValue;
                        continue;
                    }

                    clueCount--;
                    if (clueCount > maxClues) continue;
                    if (_analyzer.Analyze(puzzle).Difficulty != target) continue;

                    var result = new SudokuResult
                    {
                        Puzzle = (int[,])puzzle.Clone(),
                        Solution = solution,
                        Difficulty = target
                    };
                    Debug.Log($"[DiagonalSudokuGenerator] Generated solution (level {level}, difficulty {target}):\n{FormatBoard(solution)}");
                    return result;
                }
            }
        }

        throw new InvalidOperationException(
            $"Could not generate a Diagonal Sudoku at or below {requestedDifficulty} within {maxAttempts} attempts per tier.");
    }

    private bool FillSolution(int[,] board, Random random)
    {
        int bestRow = -1, bestCol = -1, bestMask = 0, bestCount = 10;
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
            {
                if (board[row, col] != 0) continue;
                int mask = DiagonalSudokuRules.GetCandidateMask(board, row, col);
                int count = CountBits(mask);
                if (count == 0) return false;
                if (count >= bestCount) continue;
                bestRow = row;
                bestCol = col;
                bestMask = mask;
                bestCount = count;
            }

        if (bestRow < 0) return true;

        int[] candidates = Values(bestMask).OrderBy(_ => random.Next()).ToArray();
        foreach (int value in candidates)
        {
            board[bestRow, bestCol] = value;
            if (FillSolution(board, random)) return true;
            board[bestRow, bestCol] = 0;
        }
        return false;
    }

    private static (int min, int max) GetClueRange(SudokuDifficulty difficulty) => difficulty switch
    {
        SudokuDifficulty.Simple => (66, 71),
        SudokuDifficulty.Beginner => (56, 65),
        SudokuDifficulty.Easy => (45, 55),
        SudokuDifficulty.Novice => (36, 52),
        SudokuDifficulty.Moderate => (30, 48),
        SudokuDifficulty.Advanced => (27, 44),
        SudokuDifficulty.Hard => (24, 40),
        SudokuDifficulty.Expert => (21, 36),
        SudokuDifficulty.Hardest => (17, 32),
        _ => throw new ArgumentOutOfRangeException(nameof(difficulty))
    };

    private static IEnumerable<int> Values(int mask)
    {
        for (int value = 1; value <= 9; value++)
            if ((mask & (1 << value)) != 0) yield return value;
    }

    private static int CountBits(int mask)
    {
        int count = 0;
        while (mask != 0) { mask &= mask - 1; count++; }
        return count;
    }

    private static string FormatBoard(int[,] board)
    {
        var output = new System.Text.StringBuilder();
        for (int row = 0; row < 9; row++)
        {
            if (row > 0) output.AppendLine();
            for (int col = 0; col < 9; col++)
            {
                if (col > 0) output.Append(col % 3 == 0 ? " | " : " ");
                output.Append(board[row, col]);
            }
            if (row == 2 || row == 5)
            {
                output.AppendLine();
                output.Append("------+-------+------");
            }
        }
        return output.ToString();
    }
}
