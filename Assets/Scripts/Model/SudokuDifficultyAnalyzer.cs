using System;
using System.Linq;
using System.Collections.Generic;

public enum SudokuTechnique
{
    NakedSingle = 0,
    HiddenSingle = 1,
    LockedCandidate = 2,
    NakedPair = 3,
    HiddenPair = 4,
    NakedTriple = 5,
    XWing = 6,
    Swordfish = 7,
    SearchRequired = 8
}

public class SudokuDifficultyResult
{
    public SudokuDifficulty Difficulty;
    public SudokuTechnique HardestTechnique;
    public int SolveSteps;
    public bool SolvedLogically;
}

public static class SudokuDifficultyAnalyzer
{
    private sealed class SolverState
    {
        public int[,] Board { get; }
        public HashSet<int>[,] Candidates { get; }

        public SolverState(int[,] puzzle)
        {
            Board = (int[,])puzzle.Clone();

            Candidates = new HashSet<int>[9, 9];

            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    Candidates[row, col] = new HashSet<int>();

                    if (Board[row, col] != 0) continue;

                    foreach (int candidate in SudokuSolver.GetCandidates(Board,row,col))
                        Candidates[row, col].Add(candidate);
                }
            }
        }

        public void Place(int row,int col,int value)
        {
            Board[row, col] = value;
            Candidates[row, col].Clear();

            // Remove from row / column.
            for (int i = 0; i < 9; i++)
            {
                if (Board[row, i] == 0)
                    Candidates[row, i].Remove(value);

                if (Board[i, col] == 0)
                    Candidates[i, col].Remove(value);
            }

            // Remove from box.
            int startRow = (row / 3) * 3;

            int startCol = (col / 3) * 3;

            for (int r = startRow; r < startRow + 3; r++)
            {
                for (int c = startCol; c < startCol + 3; c++)
                {
                    if (Board[r, c] == 0)
                    {
                        Candidates[r, c].Remove(value);
                    }
                }
            }
        }
    }
    public static SudokuDifficultyResult Analyze(int[,] puzzle)
    {
        SolverState state = new SolverState(puzzle);
        int steps = 0;
        SudokuTechnique hardest = SudokuTechnique.NakedSingle;

        while (!IsComplete(state.Board))
        {
            if (TryNakedSingle(state))
            {
                hardest = Max(
                    hardest,
                    SudokuTechnique.NakedSingle);

                steps++;
                continue;
            }

            if (TryHiddenSingle(state))
            {
                hardest = Max(
                    hardest,
                    SudokuTechnique.HiddenSingle);

                steps++;
                continue;
            }

            if (TryLockedCandidate(state))
            {
                hardest = Max(
                    hardest,
                    SudokuTechnique.LockedCandidate);

                steps++;
                continue;
            }

            if (TryNakedPair(state))
            {
                hardest = Max(
                    hardest,
                    SudokuTechnique.NakedPair);

                steps++;
                continue;
            }

            hardest =
                SudokuTechnique.SearchRequired;

            break;
        }

        return new SudokuDifficultyResult
        {
            Difficulty =
                MapToDifficulty(
                    hardest,
                    steps),

            HardestTechnique = hardest,

            SolveSteps = steps,

            SolvedLogically =
                IsComplete(state.Board)
        };
    }
    private static SudokuTechnique Max(SudokuTechnique a,SudokuTechnique b) => (SudokuTechnique) Math.Max((int)a, (int)b);
    private static bool IsComplete(int[,] board)
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (board[row, col] == 0)
                    return false;
            }
        }

        return true;
    }
    private static SudokuDifficulty MapToDifficulty(SudokuTechnique technique,int steps)
    {
        switch (technique)
        {
            case SudokuTechnique.NakedSingle:
            {
                if (steps <= 15) return SudokuDifficulty.Simple;
                if (steps <= 25) return SudokuDifficulty.Beginner;

                return SudokuDifficulty.Easy;
            }

            case SudokuTechnique.HiddenSingle:
                return SudokuDifficulty.Novice;

            case SudokuTechnique.LockedCandidate:
                return SudokuDifficulty.Moderate;

            case SudokuTechnique.NakedPair:
            case SudokuTechnique.HiddenPair:
                return SudokuDifficulty.Advanced;

            case SudokuTechnique.NakedTriple:
                return SudokuDifficulty.Hard;

            case SudokuTechnique.XWing:
            case SudokuTechnique.Swordfish:
                return SudokuDifficulty.Expert;

            case SudokuTechnique.SearchRequired:
                return SudokuDifficulty.Hardest;

            default:
                return SudokuDifficulty.Easy;
        }
    }
    private static bool TryNakedSingle(
        SolverState state)
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (state.Board[row, col] != 0)
                    continue;

                if (state.Candidates[row, col].Count != 1)
                    continue;

                int value =
                    state.Candidates[row, col].First();

                state.Place(
                    row,
                    col,
                    value);

                return true;
            }
        }

        return false;
    }    
    private static bool TryHiddenSingle(
        SolverState state)
    {
        // --------------------
        // ROWS
        // --------------------

        for (int row = 0; row < 9; row++)
        {
            for (int number = 1;
                number <= 9;
                number++)
            {
                int foundCol = -1;
                int count = 0;

                for (int col = 0;
                    col < 9;
                    col++)
                {
                    if (state.Board[row, col] != 0)
                        continue;

                    if (state.Candidates[
                            row,
                            col]
                        .Contains(number))
                    {
                        foundCol = col;
                        count++;
                    }
                }

                if (count == 1)
                {
                    state.Place(
                        row,
                        foundCol,
                        number);

                    return true;
                }
            }
        }

        // --------------------
        // COLUMNS
        // --------------------

        for (int col = 0; col < 9; col++)
        {
            for (int number = 1;
                number <= 9;
                number++)
            {
                int foundRow = -1;
                int count = 0;

                for (int row = 0;
                    row < 9;
                    row++)
                {
                    if (state.Board[row, col] != 0)
                        continue;

                    if (state.Candidates[
                            row,
                            col]
                        .Contains(number))
                    {
                        foundRow = row;
                        count++;
                    }
                }

                if (count == 1)
                {
                    state.Place(
                        foundRow,
                        col,
                        number);

                    return true;
                }
            }
        }

        // --------------------
        // 3x3 BOXES
        // --------------------

        for (int boxRow = 0;
            boxRow < 3;
            boxRow++)
        {
            for (int boxCol = 0;
                boxCol < 3;
                boxCol++)
            {
                int startRow =
                    boxRow * 3;

                int startCol =
                    boxCol * 3;

                for (int number = 1;
                    number <= 9;
                    number++)
                {
                    int foundRow = -1;
                    int foundCol = -1;
                    int count = 0;

                    for (int r = 0; r < 3; r++)
                    {
                        for (int c = 0; c < 3; c++)
                        {
                            int row =
                                startRow + r;

                            int col =
                                startCol + c;

                            if (state.Board[
                                    row,
                                    col] != 0)
                            {
                                continue;
                            }

                            if (state.Candidates[
                                    row,
                                    col]
                                .Contains(number))
                            {
                                foundRow = row;
                                foundCol = col;
                                count++;
                            }
                        }
                    }

                    if (count == 1)
                    {
                        state.Place(
                            foundRow,
                            foundCol,
                            number);

                        return true;
                    }
                }
            }
        }

        return false;
    }
    private static bool TryLockedCandidate(
        SolverState state)
    {
        for (int boxRow = 0;
            boxRow < 3;
            boxRow++)
        {
            for (int boxCol = 0;
                boxCol < 3;
                boxCol++)
            {
                int startRow =
                    boxRow * 3;

                int startCol =
                    boxCol * 3;

                for (int number = 1;
                    number <= 9;
                    number++)
                {
                    var cells =
                        new List<(int row, int col)>();

                    /*
                    * Find all candidate positions for
                    * this number inside this box.
                    */
                    for (int r = 0; r < 3; r++)
                    {
                        for (int c = 0; c < 3; c++)
                        {
                            int row =
                                startRow + r;

                            int col =
                                startCol + c;

                            if (state.Board[
                                    row,
                                    col] != 0)
                            {
                                continue;
                            }

                            if (state.Candidates[
                                    row,
                                    col]
                                .Contains(number))
                            {
                                cells.Add(
                                    (row, col));
                            }
                        }
                    }

                    if (cells.Count < 2)
                        continue;

                    // =========================
                    // POINTING ROW
                    // =========================

                    int commonRow =
                        cells[0].row;

                    bool sameRow =
                        cells.All(
                            cell =>
                                cell.row ==
                                commonRow);

                    if (sameRow)
                    {
                        bool changed = false;

                        for (int col = 0;
                            col < 9;
                            col++)
                        {
                            // Skip the current box.
                            if (col >= startCol &&
                                col < startCol + 3)
                            {
                                continue;
                            }

                            if (state.Board[
                                    commonRow,
                                    col] != 0)
                            {
                                continue;
                            }

                            if (state.Candidates[
                                    commonRow,
                                    col]
                                .Remove(number))
                            {
                                changed = true;
                            }
                        }

                        if (changed)
                            return true;
                    }

                    // =========================
                    // POINTING COLUMN
                    // =========================

                    int commonCol =
                        cells[0].col;

                    bool sameCol =
                        cells.All(
                            cell =>
                                cell.col ==
                                commonCol);

                    if (sameCol)
                    {
                        bool changed = false;

                        for (int row = 0;
                            row < 9;
                            row++)
                        {
                            // Skip the current box.
                            if (row >= startRow &&
                                row < startRow + 3)
                            {
                                continue;
                            }

                            if (state.Board[
                                    row,
                                    commonCol] != 0)
                            {
                                continue;
                            }

                            if (state.Candidates[
                                    row,
                                    commonCol]
                                .Remove(number))
                            {
                                changed = true;
                            }
                        }

                        if (changed)
                            return true;
                    }
                }
            }
        }

        return false;
    }
    private static bool TryNakedPair(
        SolverState state)
    {
        // --------------------
        // ROWS
        // --------------------

        for (int row = 0; row < 9; row++)
        {
            var cells =
                new List<(int row, int col)>();

            for (int col = 0;
                col < 9;
                col++)
            {
                if (state.Board[row, col] == 0)
                {
                    cells.Add((row, col));
                }
            }

            if (TryNakedPairInUnit(
                    state,
                    cells))
            {
                return true;
            }
        }

        // --------------------
        // COLUMNS
        // --------------------

        for (int col = 0; col < 9; col++)
        {
            var cells =
                new List<(int row, int col)>();

            for (int row = 0;
                row < 9;
                row++)
            {
                if (state.Board[row, col] == 0)
                {
                    cells.Add((row, col));
                }
            }

            if (TryNakedPairInUnit(
                    state,
                    cells))
            {
                return true;
            }
        }

        // --------------------
        // BOXES
        // --------------------

        for (int boxRow = 0;
            boxRow < 3;
            boxRow++)
        {
            for (int boxCol = 0;
                boxCol < 3;
                boxCol++)
            {
                var cells =
                    new List<(int row, int col)>();

                int startRow =
                    boxRow * 3;

                int startCol =
                    boxCol * 3;

                for (int r = 0; r < 3; r++)
                {
                    for (int c = 0; c < 3; c++)
                    {
                        int row =
                            startRow + r;

                        int col =
                            startCol + c;

                        if (state.Board[
                                row,
                                col] == 0)
                        {
                            cells.Add(
                                (row, col));
                        }
                    }
                }

                if (TryNakedPairInUnit(
                        state,
                        cells))
                {
                    return true;
                }
            }
        }

        return false;
    }
    private static bool TryNakedPairInUnit(
        SolverState state,
        List<(int row, int col)> cells)
    {
        for (int i = 0;
            i < cells.Count;
            i++)
        {
            var first = cells[i];

            HashSet<int> firstCandidates =
                state.Candidates[
                    first.row,
                    first.col];

            if (firstCandidates.Count != 2)
                continue;

            for (int j = i + 1;
                j < cells.Count;
                j++)
            {
                var second = cells[j];

                HashSet<int> secondCandidates =
                    state.Candidates[
                        second.row,
                        second.col];

                if (secondCandidates.Count != 2)
                    continue;

                if (!firstCandidates.SetEquals(
                        secondCandidates))
                {
                    continue;
                }

                int[] pair =
                    firstCandidates.ToArray();

                bool changed = false;

                foreach (var cell in cells)
                {
                    if (cell == first ||
                        cell == second)
                    {
                        continue;
                    }

                    HashSet<int> candidates =
                        state.Candidates[
                            cell.row,
                            cell.col];

                    if (candidates.Remove(pair[0]))
                        changed = true;

                    if (candidates.Remove(pair[1]))
                        changed = true;
                }

                /*
                * Important:
                *
                * We return true when candidates
                * were eliminated — we DON'T require
                * the elimination to immediately
                * solve a cell.
                */
                if (changed)
                    return true;
            }
        }

        return false;
    }
}

