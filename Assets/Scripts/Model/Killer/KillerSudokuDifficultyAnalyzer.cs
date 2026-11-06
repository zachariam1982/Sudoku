using System;

/// <summary>
/// Rates Killer Sudoku with a Killer-native candidate engine. Candidate sets
/// include cage totals, no-repeat cage digits, and standard Sudoku peers.
/// </summary>
public sealed class KillerSudokuDifficultyAnalyzer : ISudokuDifficultyAnalyzer
{
    public SudokuDifficultyResult Analyze(int[,] puzzle) => Analyze(puzzle, null);

    public SudokuDifficultyResult Analyze(int[,] puzzle, KillerCage[] cages)
    {
        ValidateDimensions(puzzle);
        if (!KillerSudokuRules.ValidateBoard(puzzle, cages))
            throw new ArgumentException("Puzzle is invalid under Killer Sudoku constraints.", nameof(puzzle));

        return AnalyzeKnownValidPuzzle(puzzle, cages);
    }

    internal SudokuDifficultyResult AnalyzeKnownValidPuzzle(int[,] puzzle, KillerCage[] cages)
    {
        ValidateDimensions(puzzle);
        int[,] board = (int[,])puzzle.Clone();
        int steps = 0;
        SudokuTechnique hardest = SudokuTechnique.NakedSingle;
        while (!IsComplete(board))
        {
            if (TryNakedSingle(board, cages))
            {
                steps++;
                hardest = Max(hardest, SudokuTechnique.NakedSingle);
            }
            else if (TryHiddenSingle(board, cages))
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

        // For Killer Sudoku, the cage sums often make advanced boards solvable
        // with singles. Rate the selected tier by the visible givens so the
        // displayed difficulty stays aligned with the puzzle's clue density.
        return new SudokuDifficultyResult
        {
            Difficulty = MapToDifficulty(CountGivens(puzzle)),
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

    private static bool TryNakedSingle(int[,] board, KillerCage[] cages)
    {
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
            {
                if (board[row, col] != 0) continue;
                int mask = KillerSudokuRules.GetCandidateMask(board, cages, row, col);
                if (CountBits(mask) != 1) continue;
                board[row, col] = FirstValue(mask);
                return true;
            }
        return false;
    }

    private static bool TryHiddenSingle(int[,] board, KillerCage[] cages)
    {
        foreach (int[] unit in KillerSudokuRules.AllUnits)
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
                        (KillerSudokuRules.GetCandidateMask(board, cages, row, col) & bit) == 0) continue;
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

    private static SudokuDifficulty MapToDifficulty(int givens)
    {
        if (givens >= 50) return SudokuDifficulty.Simple;
        if (givens >= 43) return SudokuDifficulty.Beginner;
        if (givens >= 36) return SudokuDifficulty.Easy;
        if (givens >= 30) return SudokuDifficulty.Novice;
        if (givens >= 25) return SudokuDifficulty.Moderate;
        if (givens >= 21) return SudokuDifficulty.Advanced;
        if (givens >= 17) return SudokuDifficulty.Hard;
        if (givens >= 13) return SudokuDifficulty.Expert;
        return SudokuDifficulty.Hardest;
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
