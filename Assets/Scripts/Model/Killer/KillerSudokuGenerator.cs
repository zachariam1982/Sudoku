using System;
using System.Collections.Generic;

/// <summary>Generates Killer puzzles and rates them with KillerSudokuDifficultyAnalyzer.</summary>
public sealed class KillerSudokuGenerator : ISudokuGenerator
{
    private readonly KillerSudokuSolver _solver = new KillerSudokuSolver();
    private readonly KillerSudokuDifficultyAnalyzer _analyzer = new KillerSudokuDifficultyAnalyzer();

    // Killer difficulty is intentionally driven by visible givens. The bands
    // are disjoint so each tier has fewer clues than the tier before it.
    private static (int minClues, int maxClues) GetClueRange(SudokuDifficulty difficulty) => difficulty switch
    {
        SudokuDifficulty.Simple => (50, 58),
        SudokuDifficulty.Beginner => (43, 49),
        SudokuDifficulty.Easy => (36, 42),
        SudokuDifficulty.Novice => (30, 35),
        SudokuDifficulty.Moderate => (25, 29),
        SudokuDifficulty.Advanced => (21, 24),
        SudokuDifficulty.Hard => (17, 20),
        SudokuDifficulty.Expert => (13, 16),
        SudokuDifficulty.Hardest => (9, 12),
        _ => (30, 35)
    };

    private static (int minSize, int maxSize) GetCageSizeRange(SudokuDifficulty difficulty) => difficulty switch
    {
        SudokuDifficulty.Simple => (2, 3),
        SudokuDifficulty.Beginner => (2, 4),
        SudokuDifficulty.Easy => (3, 4),
        SudokuDifficulty.Novice => (3, 5),
        SudokuDifficulty.Moderate => (4, 5),
        SudokuDifficulty.Advanced => (4, 6),
        SudokuDifficulty.Hard => (5, 6),
        SudokuDifficulty.Expert => (5, 7),
        SudokuDifficulty.Hardest => (6, 8),
        _ => (3, 5)
    };

    public SudokuResult Generate(int level, SudokuDifficulty requestedDifficulty, int maxAttempts = 100) =>
        GenerateKillerSudoku(level, requestedDifficulty, maxAttempts);

    public SudokuResult GenerateKillerSudoku(int level, SudokuDifficulty requestedDifficulty, int maxAttempts = 100)
    {
        if (!Enum.IsDefined(typeof(SudokuDifficulty), requestedDifficulty))
            throw new ArgumentOutOfRangeException(nameof(requestedDifficulty));
        if (maxAttempts <= 0) throw new ArgumentOutOfRangeException(nameof(maxAttempts));

        (int minClues, int maxClues) = GetClueRange(requestedDifficulty);
        (int minCageSize, int maxCageSize) = GetCageSizeRange(requestedDifficulty);
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int seed = unchecked(level * 397 ^ (((int)requestedDifficulty + 1) * 7919) ^ attempt * 104729 ^ 0xC1A55E7);
            var random = new System.Random(seed);
            int[,] solution = CreateSolvedGrid(random);
            KillerCage[] cages = CreateCages(solution, random, minCageSize, maxCageSize);
            int[,] puzzle = (int[,])solution.Clone();
            int clueCount = 81;
            List<int> removalOrder = new List<int>(81);
            for (int cell = 0; cell < 81; cell++) removalOrder.Add(cell);
            Shuffle(removalOrder, random);

            foreach (int cell in removalOrder)
            {
                if (clueCount <= minClues) break;

                int row = cell / 9;
                int col = cell % 9;
                int saved = puzzle[row, col];
                puzzle[row, col] = 0;
                if (!_solver.HasUniqueSolution(puzzle, cages))
                {
                    puzzle[row, col] = saved;
                    continue;
                }

                clueCount--;
                if (clueCount > maxClues) continue;

                SudokuDifficultyResult rating = _analyzer.AnalyzeKnownValidPuzzle(puzzle, cages);
                if (rating.Difficulty != requestedDifficulty) continue;

                return new SudokuResult
                {
                    Puzzle = (int[,])puzzle.Clone(),
                    Solution = (int[,])solution.Clone(),
                    Difficulty = requestedDifficulty,
                    KillerCages = CloneCages(cages)
                };
            }
        }

        throw new InvalidOperationException(
            $"Could not generate a uniquely solvable Killer Sudoku at {requestedDifficulty} " +
            $"({minClues}-{maxClues} givens, cages {minCageSize}-{maxCageSize} cells) " +
            $"within {maxAttempts} attempts.");
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

    private static KillerCage[] CreateCages(int[,] solution, System.Random random, int minSize, int maxSize)
    {
        var unassigned = new HashSet<int>();
        for (int cell = 0; cell < 81; cell++) unassigned.Add(cell);
        var cages = new List<KillerCage>();
        while (unassigned.Count > 0)
        {
            var remaining = new List<int>(unassigned);
            int start = remaining[random.Next(remaining.Count)];
            int desiredSize = random.Next(minSize, maxSize + 1);
            int startBox = GetBoxIndex(start);
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

                // Larger Killer cages should visually span 3x3 box boundaries.
                // Prefer an adjacent cell outside the starting box whenever
                // the cage has begun growing and such a cell is available.
                if (cells.Count >= 2)
                {
                    var outsideStartingBox = new List<int>();
                    foreach (int candidate in frontier)
                        if (GetBoxIndex(candidate) != startBox)
                            outsideStartingBox.Add(candidate);
                    if (outsideStartingBox.Count > 0) frontier = outsideStartingBox;
                }

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

    private static int GetBoxIndex(int cell) => (cell / 27) * 3 + (cell % 9) / 3;

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
