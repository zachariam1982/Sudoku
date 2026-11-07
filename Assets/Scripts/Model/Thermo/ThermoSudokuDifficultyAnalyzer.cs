using System;

/// <summary>
/// Rates Thermo Sudoku with a Thermo-native candidate engine. Candidate sets
/// include strict thermometer ordering, and standard Sudoku peers.
/// </summary>
public sealed class ThermoSudokuDifficultyAnalyzer : ISudokuDifficultyAnalyzer
{
    private static readonly int[][] Units = BuildUnits();
    private static int[][] BuildUnits()
    {
        var units = new int[27][];
        for (int u = 0; u < 27; u++)
        {
            units[u] = new int[9];
            for (int i = 0; i < 9; i++)
                units[u][i] = u < 9 ? u*9+i : u < 18 ? i*9+u-9 : ((u-18)/3*3+i/3)*9+(u-18)%3*3+i%3;
        }
        return units;
    }
    public SudokuDifficultyResult Analyze(int[,] puzzle) => Analyze(puzzle, null);

    public SudokuDifficultyResult Analyze(int[,] puzzle, Thermometer[] paths)
    {
        ValidateDimensions(puzzle);
        if (!ThermoSudokuRules.ValidateBoard(puzzle, paths))
            throw new ArgumentException("Puzzle is invalid under Thermo Sudoku constraints.", nameof(puzzle));

        return AnalyzeKnownValidPuzzle(puzzle, paths);
    }

    internal SudokuDifficultyResult AnalyzeKnownValidPuzzle(int[,] puzzle, Thermometer[] paths)
    {
        ValidateDimensions(puzzle);
        int[,] board = (int[,])puzzle.Clone();
        int steps = 0;
        SudokuTechnique hardest = SudokuTechnique.NakedSingle;
        while (!IsComplete(board))
        {
            if (TryNakedSingle(board, paths))
            {
                steps++;
                hardest = Max(hardest, SudokuTechnique.NakedSingle);
            }
            else if (TryHiddenSingle(board, paths))
            {
                steps++;
                hardest = Max(hardest, SudokuTechnique.HiddenSingle);
            }
            else
            {
                hardest = SudokuTechnique.SearchRequired;
                break;
            }
        }

        return new SudokuDifficultyResult
        {
            Difficulty = MapToDifficulty(hardest, steps, CountGivens(board)),
            HardestTechnique = hardest,
            SolveSteps = steps,
            SolvedLogically = IsComplete(board)
        };
    }

    private static void ValidateDimensions(int[,] puzzle)
    {
        if (puzzle == null) throw new ArgumentNullException(nameof(puzzle));
        if (puzzle.GetLength(0) != 9 || puzzle.GetLength(1) != 9)
            throw new ArgumentException("A Sudoku board must be 9 by 9.", nameof(puzzle));
    }

    private static bool TryNakedSingle(int[,] board, Thermometer[] paths)
    {
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
            {
                if (board[row, col] != 0) continue;
                int mask = ThermoSudokuRules.GetCandidateMask(board, paths, row, col);
                if (CountBits(mask) != 1) continue;
                board[row, col] = FirstValue(mask);
                return true;
            }
        return false;
    }

    private static bool TryHiddenSingle(int[,] board, Thermometer[] paths)
    {
        foreach (int[] unit in Units)
            for (int value = 1; value <= 9; value++)
            {
                int found = -1;
                int count = 0;
                int bit = 1 << value;
                foreach (int cell in unit)
                {
                    int row = cell / 9;
                    int col = cell % 9;
                    if (board[row, col] != 0 ||
                        (ThermoSudokuRules.GetCandidateMask(board, paths, row, col) & bit) == 0) continue;
                    found = cell;
                    if (++count > 1) break;
                }
                if (count == 1)
                {
                    board[found / 9, found % 9] = value;
                    return true;
                }
            }
        return false;
    }

    private static SudokuDifficulty MapToDifficulty(SudokuTechnique technique, int steps, int filled)
    {
        if (technique == SudokuTechnique.SearchRequired)
        {
            int remaining = 81 - filled;
            if (remaining <= 8) return SudokuDifficulty.Moderate;
            if (remaining <= 16) return SudokuDifficulty.Advanced;
            if (remaining <= 25) return SudokuDifficulty.Hard;
            if (remaining <= 36) return SudokuDifficulty.Expert;
            return SudokuDifficulty.Hardest;
        }
        if (technique == SudokuTechnique.HiddenSingle) return SudokuDifficulty.Novice;
        return steps <= 15 ? SudokuDifficulty.Simple : steps <= 25 ? SudokuDifficulty.Beginner : SudokuDifficulty.Easy;
    }

    private static bool IsComplete(int[,] board)
    {
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
                if (board[row, col] == 0) return false;
        return true;
    }

    private static int CountGivens(int[,] puzzle)
    {
        int count = 0;
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
                if (puzzle[row, col] != 0)
                    count++;
        return count;
    }

    private static int CountBits(int mask)
    {
        int count = 0;
        while (mask != 0) { mask &= mask - 1; count++; }
        return count;
    }

    private static int FirstValue(int mask)
    {
        for (int value = 1; value <= 9; value++)
            if ((mask & (1 << value)) != 0) return value;
        return 0;
    }

    private static SudokuTechnique Max(SudokuTechnique a, SudokuTechnique b) =>
        (SudokuTechnique)Math.Max((int)a, (int)b);
}
