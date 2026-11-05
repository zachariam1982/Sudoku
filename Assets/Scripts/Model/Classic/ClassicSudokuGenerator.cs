using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ClassicSudokuGenerator : ISudokuGenerator
{
    private static ( int minClues, int maxClues) GetSearchRange( SudokuDifficulty difficulty)
    {
        return difficulty switch
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
            _ => (25, 55)
        };
    }
    public static SudokuResult GenerateSudoku( int level, SudokuDifficulty requestedDifficulty, int maxAttempts = 100)
    {
        if (!Enum.IsDefined(typeof(SudokuDifficulty), requestedDifficulty))
            throw new ArgumentOutOfRangeException(nameof(requestedDifficulty), requestedDifficulty, "Unknown Sudoku difficulty.");

        long generationStartedAt = System.Diagnostics.Stopwatch.GetTimestamp();

        // Try the requested tier first. If this seed cannot produce one, step down
        // one tier at a time so players never receive a harder puzzle than selected.
        SudokuResult result = GenerateWithDifficultyFallback(
            requestedDifficulty,
            tier => TryGenerateAtDifficulty(level, tier, maxAttempts));

        UnityEngine.Debug.Log(
            $"[ClassicSudokuGenerator] level={level} requested={requestedDifficulty} actual={result.Difficulty} " +
            $"total={ElapsedMilliseconds(generationStartedAt):F2}ms");
        UnityEngine.Debug.Log(
            $"[ClassicSudokuGenerator] Generated solution (level {level}, difficulty {result.Difficulty}):\n" +
            FormatBoard(result.Solution));

        return result;
    }

    SudokuResult ISudokuGenerator.Generate(
        int level, SudokuDifficulty requestedDifficulty, int maxAttempts) =>
        GenerateSudoku(level, requestedDifficulty, maxAttempts);

    internal static SudokuResult GenerateWithDifficultyFallback(
        SudokuDifficulty requestedDifficulty,
        Func<SudokuDifficulty, SudokuResult> generateAtDifficulty)
    {
        for (int tier = (int)requestedDifficulty; tier >= (int)SudokuDifficulty.Simple; tier--)
        {
            SudokuDifficulty actualDifficulty = (SudokuDifficulty)tier;
            SudokuResult result = generateAtDifficulty(actualDifficulty);
            if (result == null) continue;

            result.Difficulty = actualDifficulty;
            return result;
        }

        throw new InvalidOperationException(
            $"Could not generate a {requestedDifficulty} Sudoku or any lower difficulty.");
    }

    private static double ElapsedMilliseconds(long startedAt) =>
        (System.Diagnostics.Stopwatch.GetTimestamp() - startedAt) *
        1000d / System.Diagnostics.Stopwatch.Frequency;

    private static void LogTierDiagnostics(
        int level,
        SudokuDifficulty difficulty,
        string outcome,
        int attempts,
        int maxAttempts,
        long tierStartedAt,
        double fillBoardMs,
        double uniquenessMs,
        int uniquenessChecks,
        double analyzerMs,
        int analyzerCalls,
        int acceptedRemovals,
        int slowestAttempt,
        int slowestSeed,
        double slowestAttemptMs)
    {
        double totalMs = ElapsedMilliseconds(tierStartedAt);
        double otherMs = Math.Max(0d, totalMs - fillBoardMs - uniquenessMs - analyzerMs);

        UnityEngine.Debug.Log(
            $"[ClassicSudokuGenerator] level={level} tier={difficulty} outcome={outcome} " +
            $"attempts={attempts}/{maxAttempts} total={totalMs:F2}ms " +
            $"fill={fillBoardMs:F2}ms uniqueness={uniquenessMs:F2}ms/{uniquenessChecks} checks " +
            $"analyzer={analyzerMs:F2}ms/{analyzerCalls} calls other={otherMs:F2}ms " +
            $"acceptedRemovals={acceptedRemovals} slowestAttempt={slowestAttempt} " +
            $"slowestSeed={slowestSeed} slowestMs={slowestAttemptMs:F2}");
    }

    private static SudokuResult TryGenerateAtDifficulty(int level, SudokuDifficulty difficulty, int maxAttempts = 100)
    {
        var (minClues, maxClues) = GetSearchRange(difficulty);

        long tierStartedAt = System.Diagnostics.Stopwatch.GetTimestamp();
        double fillBoardMs = 0d;
        double uniquenessMs = 0d;
        double analyzerMs = 0d;
        int uniquenessChecks = 0;
        int analyzerCalls = 0;
        int acceptedRemovals = 0;
        int slowestAttempt = 0;
        int slowestSeed = 0;
        double slowestAttemptMs = 0d;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            long attemptStartedAt = System.Diagnostics.Stopwatch.GetTimestamp();
            int seed = unchecked(level * 397 ^ ((int)difficulty + 1) * 7919 ^ attempt * 104729);
            System.Random rng = new System.Random(seed);
            int[,] solution = new int[9, 9];

            long stageStartedAt = System.Diagnostics.Stopwatch.GetTimestamp();
            FillBoard(solution, rng);
            fillBoardMs += ElapsedMilliseconds(stageStartedAt);

            int[,] puzzle = (int[,])solution.Clone();
            List<int> cells = Enumerable.Range(0, 81).OrderBy(_ => rng.Next()).ToList();
            int clueCount = 81;

            foreach (int index in cells)
            {
                int row = index / 9;
                int col = index % 9;
                int previous = puzzle[row, col];

                puzzle[row, col] = 0;

                uniquenessChecks++;
                stageStartedAt = System.Diagnostics.Stopwatch.GetTimestamp();
                bool hasUniqueSolution = ClassicSudokuSolver.HasUniqueSolution(puzzle);
                uniquenessMs += ElapsedMilliseconds(stageStartedAt);

                if (!hasUniqueSolution)
                {
                    puzzle[row, col] = previous;
                    continue;
                }

                clueCount--;
                acceptedRemovals++;

                if (clueCount > maxClues) continue;
                if (clueCount < minClues) break;

                analyzerCalls++;
                stageStartedAt = System.Diagnostics.Stopwatch.GetTimestamp();
                SudokuDifficultyResult rating = ClassicSudokuDifficultyAnalyzer.Analyze(puzzle);
                analyzerMs += ElapsedMilliseconds(stageStartedAt);

                if (rating.Difficulty == difficulty)
                {
                    double attemptMs = ElapsedMilliseconds(attemptStartedAt);
                    if (attemptMs > slowestAttemptMs)
                    {
                        slowestAttempt = attempt + 1;
                        slowestSeed = seed;
                        slowestAttemptMs = attemptMs;
                    }

                    LogTierDiagnostics(
                        level, difficulty, "matched", attempt + 1, maxAttempts, tierStartedAt,
                        fillBoardMs, uniquenessMs, uniquenessChecks,
                        analyzerMs, analyzerCalls, acceptedRemovals,
                        slowestAttempt, slowestSeed, slowestAttemptMs);

                    return new SudokuResult
                    {
                        Puzzle = (int[,])puzzle.Clone(),
                        Solution = (int[,])solution.Clone(),
                        Difficulty = difficulty
                    };
                }
            }

            double completedAttemptMs = ElapsedMilliseconds(attemptStartedAt);
            if (completedAttemptMs > slowestAttemptMs)
            {
                slowestAttempt = attempt + 1;
                slowestSeed = seed;
                slowestAttemptMs = completedAttemptMs;
            }
        }

        LogTierDiagnostics(
            level, difficulty, "no-match", maxAttempts, maxAttempts, tierStartedAt,
            fillBoardMs, uniquenessMs, uniquenessChecks,
            analyzerMs, analyzerCalls, acceptedRemovals,
            slowestAttempt, slowestSeed, slowestAttemptMs);

        return null;
    }
    private static bool FillBoard(int[,] board, System.Random rng)
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (board[row, col] == 0)
                {
                    // Shuffle numbers 1-9 deterministically using our seeded RNG
                    List<int> numbers = Enumerable.Range(1, 9).OrderBy(x => rng.Next()).ToList();

                    foreach (int num in numbers)
                    {
                        if (IsValid(board, row, col, num))
                        {
                            board[row, col] = num;
                            if (FillBoard(board, rng)) return true;
                            board[row, col] = 0;
                        }
                    }
                    return false;
                }
            }
        }
        return true;
    }
    private static bool IsValid(int[,] board, int row, int col, int num)
    {
        for (int i = 0; i < 9; i++)
        {
            // Check Row and Column
            if (board[row, i] == num || board[i, col] == num) return false;

            // Check 3x3 Box
            int startRow = (row / 3) * 3;
            int startCol = (col / 3) * 3;
            if (board[startRow + (i / 3), startCol + (i % 3)] == num) return false;
        }
        return true;
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
