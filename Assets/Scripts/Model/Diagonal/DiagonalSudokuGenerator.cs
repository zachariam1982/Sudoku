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

        long generationStartedAt = System.Diagnostics.Stopwatch.GetTimestamp();
        for (int tier = (int)requestedDifficulty; tier >= (int)SudokuDifficulty.Simple; tier--)
        {
            SudokuDifficulty target = (SudokuDifficulty)tier;
            long tierStartedAt = System.Diagnostics.Stopwatch.GetTimestamp();
            double fillSolutionMs = 0d;
            double uniquenessMs = 0d;
            double analyzerMs = 0d;
            int uniquenessChecks = 0;
            int analyzerCalls = 0;
            int acceptedRemovals = 0;
            int fillFailures = 0;
            int slowestAttempt = 0;
            int slowestSeed = 0;
            double slowestAttemptMs = 0d;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                long attemptStartedAt = System.Diagnostics.Stopwatch.GetTimestamp();
                int seed = unchecked(level * 397 ^ ((tier + 1) * 7919) ^ attempt * 104729 ^ 0xD1A60A1);
                var random = new System.Random(seed);
                int[,] solution = new int[9, 9];

                long stageStartedAt = System.Diagnostics.Stopwatch.GetTimestamp();
                bool filledSolution = FillSolution(solution, random);
                fillSolutionMs += ElapsedMilliseconds(stageStartedAt);
                if (!filledSolution)
                {
                    fillFailures++;
                    RecordSlowestAttempt(attempt, seed, attemptStartedAt,
                        ref slowestAttempt, ref slowestSeed, ref slowestAttemptMs);
                    continue;
                }

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
                    uniquenessChecks++;
                    stageStartedAt = System.Diagnostics.Stopwatch.GetTimestamp();
                    bool hasUniqueSolution = _solver.HasUniqueSolution(puzzle);
                    uniquenessMs += ElapsedMilliseconds(stageStartedAt);
                    if (!hasUniqueSolution)
                    {
                        puzzle[row, col] = oldValue;
                        continue;
                    }

                    clueCount--;
                    acceptedRemovals++;
                    if (clueCount > maxClues) continue;

                    analyzerCalls++;
                    stageStartedAt = System.Diagnostics.Stopwatch.GetTimestamp();
                    SudokuDifficulty analyzedDifficulty = _analyzer.Analyze(puzzle).Difficulty;
                    analyzerMs += ElapsedMilliseconds(stageStartedAt);
                    if (analyzedDifficulty != target) continue;

                    RecordSlowestAttempt(attempt, seed, attemptStartedAt,
                        ref slowestAttempt, ref slowestSeed, ref slowestAttemptMs);
                    LogTierDiagnostics(
                        level, target, "matched", attempt + 1, maxAttempts, tierStartedAt,
                        fillSolutionMs, uniquenessMs, uniquenessChecks,
                        analyzerMs, analyzerCalls, acceptedRemovals, fillFailures,
                        slowestAttempt, slowestSeed, slowestAttemptMs);

                    var result = new SudokuResult
                    {
                        Puzzle = (int[,])puzzle.Clone(),
                        Solution = solution,
                        Difficulty = target
                    };
                    Debug.Log(
                        $"[DiagonalSudokuGenerator] level={level} requested={requestedDifficulty} actual={target} " +
                        $"total={ElapsedMilliseconds(generationStartedAt):F2}ms");
                    Debug.Log($"[DiagonalSudokuGenerator] Generated solution (level {level}, difficulty {target}):\n{FormatBoard(solution)}");
                    return result;
                }

                RecordSlowestAttempt(attempt, seed, attemptStartedAt,
                    ref slowestAttempt, ref slowestSeed, ref slowestAttemptMs);
            }

            LogTierDiagnostics(
                level, target, "no-match", maxAttempts, maxAttempts, tierStartedAt,
                fillSolutionMs, uniquenessMs, uniquenessChecks,
                analyzerMs, analyzerCalls, acceptedRemovals, fillFailures,
                slowestAttempt, slowestSeed, slowestAttemptMs);
        }

        throw new InvalidOperationException(
            $"Could not generate a Diagonal Sudoku at or below {requestedDifficulty} within {maxAttempts} attempts per tier.");
    }

    private static double ElapsedMilliseconds(long startedAt) =>
        (System.Diagnostics.Stopwatch.GetTimestamp() - startedAt) *
        1000d / System.Diagnostics.Stopwatch.Frequency;

    private static void RecordSlowestAttempt(
        int attempt, int seed, long attemptStartedAt,
        ref int slowestAttempt, ref int slowestSeed, ref double slowestAttemptMs)
    {
        double attemptMs = ElapsedMilliseconds(attemptStartedAt);
        if (attemptMs <= slowestAttemptMs) return;

        slowestAttempt = attempt + 1;
        slowestSeed = seed;
        slowestAttemptMs = attemptMs;
    }

    private static void LogTierDiagnostics(
        int level,
        SudokuDifficulty difficulty,
        string outcome,
        int attempts,
        int maxAttempts,
        long tierStartedAt,
        double fillSolutionMs,
        double uniquenessMs,
        int uniquenessChecks,
        double analyzerMs,
        int analyzerCalls,
        int acceptedRemovals,
        int fillFailures,
        int slowestAttempt,
        int slowestSeed,
        double slowestAttemptMs)
    {
        double totalMs = ElapsedMilliseconds(tierStartedAt);
        double otherMs = Math.Max(0d, totalMs - fillSolutionMs - uniquenessMs - analyzerMs);

        Debug.Log(
            $"[DiagonalSudokuGenerator] level={level} tier={difficulty} outcome={outcome} " +
            $"attempts={attempts}/{maxAttempts} total={totalMs:F2}ms " +
            $"fill={fillSolutionMs:F2}ms uniqueness={uniquenessMs:F2}ms/{uniquenessChecks} checks " +
            $"analyzer={analyzerMs:F2}ms/{analyzerCalls} calls other={otherMs:F2}ms " +
            $"acceptedRemovals={acceptedRemovals} fillFailures={fillFailures} " +
            $"slowestAttempt={slowestAttempt} slowestSeed={slowestSeed} slowestMs={slowestAttemptMs:F2}");
    }

    private bool FillSolution(int[,] board, System.Random random)
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
