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
    public static SudokuDifficultyResult Analyze(int[,] puzzle)
    {
        int[,] board = (int[,])puzzle.Clone();
        int steps = 0;

        SudokuTechnique hardest = SudokuTechnique.NakedSingle;

        while (!IsComplete(board))
        {
            if (TryNakedSingle(board))
            {
                hardest = Max(hardest,SudokuTechnique.NakedSingle);
                steps++;
                continue;
            }

            if (TryHiddenSingle(board))
            {
                hardest = Max(hardest,SudokuTechnique.HiddenSingle);
                steps++;
                continue;
            }

            if (TryLockedCandidate(board))
            {
                hardest = Max(
                    hardest,
                    SudokuTechnique.LockedCandidate);

                steps++;
                continue;
            }

            if (TryNakedPair(board))
            {
                hardest = Max(
                    hardest,
                    SudokuTechnique.NakedPair);

                steps++;
                continue;
            }

            hardest = SudokuTechnique.SearchRequired;

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
                IsComplete(board)
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
    private static bool TryNakedSingle(int[,] board)
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (board[row, col] != 0)
                    continue;

                List<int> candidates = SudokuSolver.GetCandidates(board, row, col);

                if (candidates.Count == 1)
                {
                    board[row, col] = candidates[0];

                    return true;
                }
            }
        }

        return false;
    }
    private static bool TryHiddenSingle(int[,] board)
    {
        // Rows
        for (int row = 0; row < 9; row++)
        {
            for (int number = 1;
                number <= 9;
                number++)
            {
                int foundCol = -1;
                int locations = 0;

                for (int col = 0; col < 9; col++)
                {
                    if (board[row, col] != 0)
                        continue;

                    var candidates =
                        SudokuSolver.GetCandidates(
                            board,
                            row,
                            col);

                    if (candidates.Contains(number))
                    {
                        locations++;
                        foundCol = col;
                    }
                }

                if (locations == 1)
                {
                    board[row, foundCol] = number;
                    return true;
                }
            }
        }

        // Columns
        for (int col = 0; col < 9; col++)
        {
            for (int number = 1;
                number <= 9;
                number++)
            {
                int foundRow = -1;
                int locations = 0;

                for (int row = 0; row < 9; row++)
                {
                    if (board[row, col] != 0)
                        continue;

                    var candidates =
                        SudokuSolver.GetCandidates(
                            board,
                            row,
                            col);

                    if (candidates.Contains(number))
                    {
                        locations++;
                        foundRow = row;
                    }
                }

                if (locations == 1)
                {
                    board[foundRow, col] = number;
                    return true;
                }
            }
        }

        // 3x3 boxes
        for (int boxRow = 0;
            boxRow < 3;
            boxRow++)
        {
            for (int boxCol = 0;
                boxCol < 3;
                boxCol++)
            {
                for (int number = 1;
                    number <= 9;
                    number++)
                {
                    int foundRow = -1;
                    int foundCol = -1;
                    int locations = 0;

                    for (int r = 0; r < 3; r++)
                    {
                        for (int c = 0; c < 3; c++)
                        {
                            int row =
                                boxRow * 3 + r;

                            int col =
                                boxCol * 3 + c;

                            if (board[row, col] != 0)
                                continue;

                            var candidates =
                                SudokuSolver
                                    .GetCandidates(
                                        board,
                                        row,
                                        col);

                            if (candidates
                                .Contains(number))
                            {
                                locations++;
                                foundRow = row;
                                foundCol = col;
                            }
                        }
                    }

                    if (locations == 1)
                    {
                        board[
                            foundRow,
                            foundCol] = number;

                        return true;
                    }
                }
            }
        }

        return false;
    }
    private static bool TryLockedCandidate(int[,] board)
    {
        // Process each 3x3 box.
        for (int boxRow = 0; boxRow < 3; boxRow++)
        {
            for (int boxCol = 0; boxCol < 3; boxCol++)
            {
                int startRow = boxRow * 3;
                int startCol = boxCol * 3;

                for (int number = 1; number <= 9; number++)
                {
                    var candidateCells = new List<(int row, int col)>();

                    // Find all locations for this number inside the box.
                    for (int r = 0; r < 3; r++)
                    {
                        for (int c = 0; c < 3; c++)
                        {
                            int row = startRow + r;
                            int col = startCol + c;

                            if (board[row, col] != 0) continue;

                            var candidates = SudokuSolver.GetCandidates(board,row,col);

                            if (candidates.Contains(number)) candidateCells.Add((row, col));
                        }
                    }

                    if (candidateCells.Count < 2) continue;

                    int commonRow = candidateCells[0].row;

                    bool sameRow = candidateCells.All(cell => cell.row == commonRow);

                    if (sameRow)
                    {
                        for (int col = 0; col < 9; col++)
                        {
                            if (col >= startCol && col < startCol + 3) continue;
                            if (board[commonRow, col] != 0) continue;

                            var candidates = SudokuSolver.GetCandidates(board,commonRow,col);

                            if (!candidates.Contains(number)) continue;

                            candidates.Remove(number);

                            if (candidates.Count == 1)
                            {
                                board[commonRow, col] = candidates[0];

                                return true;
                            }
                        }
                    }

                    int commonCol = candidateCells[0].col;
                    bool sameCol = candidateCells.All(cell => cell.col == commonCol);

                    if (sameCol)
                    {
                        for (int row = 0; row < 9; row++)
                        {
                            if (row >= startRow && row < startRow + 3) continue;
                            if (board[row, commonCol] != 0) continue;

                            var candidates = SudokuSolver.GetCandidates(board,row,commonCol);

                            if (!candidates.Contains(number))continue;

                            candidates.Remove(number);

                            if (candidates.Count == 1)
                            {
                                board[row, commonCol] = candidates[0];

                                return true;
                            }
                        }
                    }
                }
            }
        }

        return false;
    }
    private static bool TryNakedPair(int[,] board)
    {
        // Rows
        for (int row = 0; row < 9; row++)
        {
            var cells = new List<(int row, int col)>();

            for (int col = 0; col < 9; col++)
            {
                if (board[row, col] == 0) cells.Add((row, col));
            }

            if (TryNakedPairInUnit(board, cells))
                return true;
        }

        // Columns
        for (int col = 0; col < 9; col++)
        {
            var cells = new List<(int row, int col)>();

            for (int row = 0; row < 9; row++)
            {
                if (board[row, col] == 0) cells.Add((row, col));
            }

            if (TryNakedPairInUnit(board, cells))
                return true;
        }

        // 3x3 boxes
        for (int boxRow = 0; boxRow < 3; boxRow++)
        {
            for (int boxCol = 0; boxCol < 3; boxCol++)
            {
                var cells = new List<(int row, int col)>();
                int startRow = boxRow * 3;
                int startCol = boxCol * 3;

                for (int r = 0; r < 3; r++)
                {
                    for (int c = 0; c < 3; c++)
                    {
                        int row = startRow + r;
                        int col = startCol + c;

                        if (board[row, col] == 0)
                        {
                            cells.Add((row, col));
                        }
                    }
                }

                if (TryNakedPairInUnit(board, cells))
                    return true;
            }
        }

        return false;
    }
    private static bool TryNakedPairInUnit(int[,] board,List<(int row, int col)> cells)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            var first = cells[i];

            List<int> firstCandidates = SudokuSolver.GetCandidates(board,first.row,first.col);

            if (firstCandidates.Count != 2) continue;

            for (int j = i + 1;j < cells.Count;j++)
            {
                var second = cells[j];
                List<int> secondCandidates = SudokuSolver.GetCandidates(board,second.row,second.col);

                if (secondCandidates.Count != 2) continue;

                // Same exact pair?
                if (!HaveSameCandidates(firstCandidates,secondCandidates)) continue;

                int candidate1 = firstCandidates[0];
                int candidate2 = firstCandidates[1];

                foreach (var cell in cells)
                {
                    if (cell == first || cell == second) continue;

                    List<int> candidates = SudokuSolver.GetCandidates(board,cell.row,cell.col);
                    bool changed = false;

                    if (candidates.Remove(candidate1)) changed = true;
                    if (candidates.Remove(candidate2)) changed = true;

                    if (changed && candidates.Count == 1)
                    {
                        board[cell.row, cell.col] = candidates[0];

                        return true;
                    }
                }
            }
        }

        return false;
    }
    private static bool HaveSameCandidates(List<int> a, List<int> b)
    {
        if (a.Count != b.Count) return false;

        for (int i = 0; i < a.Count; i++)
        {
            if (!b.Contains(a[i])) return false;
        }

        return true;
    }
}

