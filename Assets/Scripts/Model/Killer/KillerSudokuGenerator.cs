using System;
using System.Collections.Generic;

/// <summary>Generates Killer puzzles and rates them with KillerSudokuDifficultyAnalyzer.</summary>
public sealed class KillerSudokuGenerator : ISudokuGenerator
{
    private readonly KillerSudokuSolver _solver = new KillerSudokuSolver();
    private readonly KillerSudokuDifficultyAnalyzer _analyzer = new KillerSudokuDifficultyAnalyzer();

    public SudokuResult Generate(int level, SudokuDifficulty requestedDifficulty, int maxAttempts = 100) =>
        GenerateKillerSudoku(level, requestedDifficulty, maxAttempts);

    public SudokuResult GenerateKillerSudoku(int level, SudokuDifficulty requestedDifficulty, int maxAttempts = 100)
    {
        if (!Enum.IsDefined(typeof(SudokuDifficulty), requestedDifficulty))
            throw new ArgumentOutOfRangeException(nameof(requestedDifficulty));
        if (maxAttempts <= 0) throw new ArgumentOutOfRangeException(nameof(maxAttempts));

        SudokuResult fallback = null;
        for (int tier = (int)requestedDifficulty; tier >= (int)SudokuDifficulty.Simple; tier--)
        {
            SudokuDifficulty target = (SudokuDifficulty)tier;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                int seed = unchecked(level * 397 ^ ((tier + 1) * 7919) ^ attempt * 104729 ^ 0xC1A55E7);
                var random = new System.Random(seed);
                int[,] solution = CreateSolvedGrid(random);
                KillerCage[] cages = CreateCages(solution, random);
                int[,] puzzle = (int[,])solution.Clone();
                List<int> removalOrder = new List<int>(81);
                for (int cell = 0; cell < 81; cell++) removalOrder.Add(cell);
                Shuffle(removalOrder, random);

                foreach (int cell in removalOrder)
                {
                    int row = cell / 9;
                    int col = cell % 9;
                    int saved = puzzle[row, col];
                    puzzle[row, col] = 0;
                    if (!_solver.HasUniqueSolution(puzzle, cages))
                    {
                        puzzle[row, col] = saved;
                        continue;
                    }

                    SudokuDifficultyResult rating = _analyzer.AnalyzeKnownValidPuzzle(puzzle, cages);
                    if (rating.Difficulty == target)
                    {
                        return new SudokuResult
                        {
                            Puzzle = (int[,])puzzle.Clone(),
                            Solution = (int[,])solution.Clone(),
                            Difficulty = rating.Difficulty,
                            KillerCages = CloneCages(cages)
                        };
                    }
                    if ((int)rating.Difficulty <= (int)requestedDifficulty)
                    {
                        // Keep the latest fallback snapshot; avoid cloning the
                        // solution and cage layout for every accepted clue removal.
                        fallback = new SudokuResult
                        {
                            Puzzle = (int[,])puzzle.Clone(),
                            Solution = solution,
                            Difficulty = rating.Difficulty,
                            KillerCages = cages
                        };
                    }
                }
            }

            // Once this tier has exhausted its attempts, a valid result at or
            // below the requested difficulty is preferable to repeating the
            // same removal work for every easier tier.
            if (fallback != null) return fallback;
        }

        if (fallback != null) return fallback;

        // A fully-clued grid is always uniquely solvable and still carries a complete cage layout.
        var finalRandom = new System.Random(unchecked(level * 397 ^ 0xC1A55E7));
        int[,] finalSolution = CreateSolvedGrid(finalRandom);
        KillerCage[] finalCages = CreateCages(finalSolution, finalRandom);
        return new SudokuResult
        {
            Puzzle = (int[,])finalSolution.Clone(),
            Solution = (int[,])finalSolution.Clone(),
            Difficulty = _analyzer.AnalyzeKnownValidPuzzle(finalSolution, finalCages).Difficulty,
            KillerCages = finalCages
        };
    }

    private static int[,] CreateSolvedGrid(System.Random random)
    {
        int[] digits = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        Shuffle(digits, random);
        int[] rowOrder = BuildAxisOrder(random);
        int[] colOrder = BuildAxisOrder(random);
        var board = new int[9, 9];
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
            {
                int pattern = (rowOrder[row] * 3 + rowOrder[row] / 3 + colOrder[col]) % 9;
                board[row, col] = digits[pattern];
            }
        return board;
    }

    private static int[] BuildAxisOrder(System.Random random)
    {
        int[] bands = { 0, 1, 2 };
        Shuffle(bands, random);
        var order = new List<int>(9);
        foreach (int band in bands)
        {
            int[] within = { 0, 1, 2 };
            Shuffle(within, random);
            foreach (int offset in within) order.Add(band * 3 + offset);
        }
        return order.ToArray();
    }

    private static KillerCage[] CreateCages(int[,] solution, System.Random random)
    {
        var unassigned = new HashSet<int>();
        for (int cell = 0; cell < 81; cell++) unassigned.Add(cell);
        var cages = new List<KillerCage>();
        while (unassigned.Count > 0)
        {
            var remaining = new List<int>(unassigned);
            int start = remaining[random.Next(remaining.Count)];
            int desiredSize = random.Next(2, 5);
            var cells = new List<int> { start };
            int usedValues = 1 << solution[start / 9, start % 9];
            unassigned.Remove(start);

            while (cells.Count < desiredSize)
            {
                var frontier = new List<int>();
                foreach (int cell in cells)
                    foreach (int neighbor in GetNeighbors(cell))
                    {
                        if (!unassigned.Contains(neighbor)) continue;
                        int bit = 1 << solution[neighbor / 9, neighbor % 9];
                        if ((usedValues & bit) == 0 && !frontier.Contains(neighbor)) frontier.Add(neighbor);
                    }
                if (frontier.Count == 0) break;
                int next = frontier[random.Next(frontier.Count)];
                cells.Add(next);
                unassigned.Remove(next);
                usedValues |= 1 << solution[next / 9, next % 9];
            }

            int sum = 0;
            foreach (int cell in cells) sum += solution[cell / 9, cell % 9];
            cages.Add(new KillerCage(sum, cells.ToArray()));
        }
        return cages.ToArray();
    }

    private static IEnumerable<int> GetNeighbors(int cell)
    {
        int row = cell / 9;
        int col = cell % 9;
        if (row > 0) yield return cell - 9;
        if (row < 8) yield return cell + 9;
        if (col > 0) yield return cell - 1;
        if (col < 8) yield return cell + 1;
    }

    private static KillerCage[] CloneCages(KillerCage[] cages)
    {
        var copy = new KillerCage[cages.Length];
        for (int i = 0; i < cages.Length; i++) copy[i] = new KillerCage(cages[i].TargetSum, cages[i].Cells);
        return copy;
    }

    private static void Shuffle<T>(IList<T> values, System.Random random)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            T value = values[i];
            values[i] = values[j];
            values[j] = value;
        }
    }
}
