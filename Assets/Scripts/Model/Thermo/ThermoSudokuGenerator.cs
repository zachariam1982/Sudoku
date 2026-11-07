using System;
using System.Collections.Generic;
public sealed class ThermoSudokuGenerator : ISudokuGenerator
{
    public SudokuResult Generate(int level, SudokuDifficulty requestedDifficulty, int maxAttempts = 100)
    {
        if (!Enum.IsDefined(typeof(SudokuDifficulty), requestedDifficulty) || maxAttempts < 1) throw new ArgumentOutOfRangeException();
        var solver = new ThermoSudokuSolver();
        var analyzer = new ThermoSudokuDifficultyAnalyzer();
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var random = new Random(unchecked(level * 397 ^ attempt * 7919 ^ (int)requestedDifficulty * 104729));
            var solution = CreateSolvedGrid(random);
            var paths = new List<Thermometer>();
            var used = new HashSet<int>();
            var starts = new List<int>();
            for (int i = 0; i < 81; i++) starts.Add(i);
            Shuffle(starts, random);
            foreach (int start in starts)
            {
                if (used.Contains(start)) continue;
                var cells = new List<int> { start };
                int current = start;
                int length = random.Next(3, 7);
                while (cells.Count < length)
                {
                    var next = new List<int>();
                    for (int n = 0; n < 81; n++)
                        if (!used.Contains(n) && !cells.Contains(n) && Math.Abs(n/9-current/9)+Math.Abs(n%9-current%9)==1 && solution[n/9,n%9] > solution[current/9,current%9]) next.Add(n);
                    if (next.Count == 0) break;
                    current = next[random.Next(next.Count)]; cells.Add(current);
                }
                if (cells.Count < 3) continue;
                paths.Add(new Thermometer(cells.ToArray()));
                foreach (int cell in cells) used.Add(cell);
                if (paths.Count == 8) break;
            }
            if (paths.Count == 0) continue;
            var thermometers = paths.ToArray();
            var puzzle = (int[,])solution.Clone();
            Shuffle(starts, random);
            int givens = 81;
            int maxGivens = new[] { 66, 56, 45, 40, 35, 31, 28, 25, 22 }[(int)requestedDifficulty];
            foreach (int cell in starts)
            {
                int value = puzzle[cell/9,cell%9]; puzzle[cell/9,cell%9] = 0;
                if (!solver.HasUniqueSolution(puzzle, thermometers)) { puzzle[cell/9,cell%9] = value; continue; }
                givens--;
                if (givens > maxGivens) continue;
                var rating = analyzer.Analyze(puzzle, thermometers);
                if (rating.Difficulty == requestedDifficulty)
                    return new SudokuResult { Puzzle = puzzle, Solution = solution, Difficulty = rating.Difficulty, Thermometers = thermometers };
            }
        }
        throw new InvalidOperationException("Unable to generate the requested Thermo difficulty within the attempt budget.");
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
