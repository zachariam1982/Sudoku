using System;

/// <summary>
/// Rates Diagonal Sudoku with diagonal-aware singles, then reuses the Classic
/// analyzer for techniques that do not depend on the extra diagonal units.
/// </summary>
public sealed class DiagonalSudokuDifficultyAnalyzer : ISudokuDifficultyAnalyzer
{
    public SudokuDifficultyResult Analyze(int[,] puzzle)
    {
        if (puzzle == null) throw new ArgumentNullException(nameof(puzzle));
        if (puzzle.GetLength(0) != 9 || puzzle.GetLength(1) != 9)
            throw new ArgumentException("A Sudoku board must be 9 by 9.", nameof(puzzle));
        if (!DiagonalSudokuRules.ValidateBoard(puzzle))
            throw new ArgumentException("Puzzle contains a duplicate or invalid value under Diagonal Sudoku rules.", nameof(puzzle));

        int[,] reducedPuzzle = (int[,])puzzle.Clone();
        int[] usedMasks = BuildUsedMasks(reducedPuzzle);
        int diagonalSteps = 0;
        SudokuTechnique diagonalTechnique = SudokuTechnique.NakedSingle;

        while (TryFindDiagonalAwareSingle(
            reducedPuzzle, usedMasks, out int row, out int col, out int value, out bool hidden))
        {
            reducedPuzzle[row, col] = value;
            AddUsedMask(usedMasks, row, col, value);
            diagonalSteps++;
            if (hidden) diagonalTechnique = SudokuTechnique.HiddenSingle;
        }

        SudokuDifficultyResult classicRating = ClassicSudokuDifficultyAnalyzer.Analyze(reducedPuzzle);
        SudokuTechnique hardest = (SudokuTechnique)Math.Max(
            (int)diagonalTechnique,
            (int)classicRating.HardestTechnique);
        SudokuDifficulty diagonalDifficulty = MapSingleDifficulty(diagonalTechnique, diagonalSteps);
        SudokuDifficulty difficulty = (SudokuDifficulty)Math.Max(
            (int)classicRating.Difficulty,
            (int)diagonalDifficulty);

        return new SudokuDifficultyResult
        {
            Difficulty = difficulty,
            HardestTechnique = hardest,
            SolveSteps = diagonalSteps + classicRating.SolveSteps,
            SolvedLogically = classicRating.SolvedLogically
        };
    }

    private static bool TryFindDiagonalAwareSingle(
        int[,] board, int[] usedMasks,
        out int foundRow, out int foundCol, out int foundValue, out bool hidden)
    {
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
            {
                if (board[row, col] != 0) continue;
                int candidates = GetCandidateMask(usedMasks, row, col);
                if (CountBits(candidates) != 1) continue;
                foundRow = row;
                foundCol = col;
                foundValue = FirstValue(candidates);
                hidden = false;
                return true;
            }

        foreach (int[] unit in DiagonalSudokuRules.AllUnits)
            for (int value = 1; value <= 9; value++)
            {
                int count = 0;
                int candidateIndex = -1;
                foreach (int index in unit)
                {
                    int row = index / 9;
                    int col = index % 9;
                    if (board[row, col] != 0 ||
                        (GetCandidateMask(usedMasks, row, col) & (1 << value)) == 0)
                        continue;
                    candidateIndex = index;
                    count++;
                    if (count > 1) break;
                }

                if (count != 1) continue;
                foundRow = candidateIndex / 9;
                foundCol = candidateIndex % 9;
                foundValue = value;
                hidden = true;
                return true;
            }

        foundRow = foundCol = foundValue = 0;
        hidden = false;
        return false;
    }

    private static int[] BuildUsedMasks(int[,] board)
    {
        var masks = new int[29];
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
            {
                int value = board[row, col];
                if (value < 1 || value > 9) continue;
                AddUsedMask(masks, row, col, value);
            }
        return masks;
    }

    private static void AddUsedMask(int[] masks, int row, int col, int value)
    {
        int bit = 1 << value;
        masks[row] |= bit;
        masks[9 + col] |= bit;
        masks[18 + row / 3 * 3 + col / 3] |= bit;
        if (row == col) masks[27] |= bit;
        if (row + col == 8) masks[28] |= bit;
    }

    private static int GetCandidateMask(int[] masks, int row, int col)
    {
        int used = masks[row] | masks[9 + col] | masks[18 + row / 3 * 3 + col / 3];
        if (row == col) used |= masks[27];
        if (row + col == 8) used |= masks[28];
        return DiagonalSudokuRules.FullMask & ~used;
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

    private static SudokuDifficulty MapSingleDifficulty(SudokuTechnique technique, int steps)
    {
        if (technique == SudokuTechnique.HiddenSingle) return SudokuDifficulty.Novice;
        if (steps <= 15) return SudokuDifficulty.Simple;
        if (steps <= 25) return SudokuDifficulty.Beginner;
        return SudokuDifficulty.Easy;
    }
}
