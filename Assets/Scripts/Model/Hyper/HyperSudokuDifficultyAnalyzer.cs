using System;
using System.Collections.Generic;

/// <summary>
/// Rates Hyper Sudoku using the same logical technique ladder as Classic,
/// applying unit-based techniques to all 31 units (rows, columns, boxes, and
/// four extra regions).
/// </summary>
public sealed class HyperSudokuDifficultyAnalyzer : ISudokuDifficultyAnalyzer
{
    private const int CellCount = 81;
    private static readonly int[][] Units = HyperSudokuRules.AllUnits;
    private static readonly int[][] Memberships = BuildMemberships();
    private static readonly int[][] CellPeers = BuildCellPeers();
    private static readonly Overlap[] Overlaps = BuildOverlaps();

    private readonly struct Overlap
    {
        public readonly int SourceUnit;
        public readonly int TargetUnit;
        public readonly bool[] ContainsCell;

        public Overlap(int sourceUnit, int targetUnit, bool[] containsCell)
        {
            SourceUnit = sourceUnit;
            TargetUnit = targetUnit;
            ContainsCell = containsCell;
        }
    }

    public SudokuDifficultyResult Analyze(int[,] puzzle)
    {
        if (puzzle == null) throw new ArgumentNullException(nameof(puzzle));
        if (puzzle.GetLength(0) != 9 || puzzle.GetLength(1) != 9)
            throw new ArgumentException("A Sudoku board must be 9 by 9.", nameof(puzzle));
        if (!HyperSudokuRules.ValidateBoard(puzzle))
            throw new ArgumentException(
                "Puzzle contains a duplicate or invalid value under Hyper Sudoku rules.",
                nameof(puzzle));

        var state = new SolverState(puzzle);
        int steps = 0;
        SudokuTechnique hardest = SudokuTechnique.NakedSingle;

        while (!state.IsComplete)
        {
            if (TryNakedSingle(state))
                hardest = RecordStep(SudokuTechnique.NakedSingle, ref steps, hardest);
            else if (TryHiddenSingle(state))
                hardest = RecordStep(SudokuTechnique.HiddenSingle, ref steps, hardest);
            else if (TryLockedCandidate(state))
                hardest = RecordStep(SudokuTechnique.LockedCandidate, ref steps, hardest);
            else if (TryNakedPair(state))
                hardest = RecordStep(SudokuTechnique.NakedPair, ref steps, hardest);
            else if (TryHiddenPair(state))
                hardest = RecordStep(SudokuTechnique.HiddenPair, ref steps, hardest);
            else if (TryNakedTriple(state))
                hardest = RecordStep(SudokuTechnique.NakedTriple, ref steps, hardest);
            else if (TryXWing(state))
                hardest = RecordStep(SudokuTechnique.XWing, ref steps, hardest);
            else
            {
                hardest = SudokuTechnique.SearchRequired;
                break;
            }
        }

        return new SudokuDifficultyResult
        {
            Difficulty = MapToDifficulty(hardest, steps),
            HardestTechnique = hardest,
            SolveSteps = steps,
            SolvedLogically = state.IsComplete
        };
    }

    private static SudokuTechnique RecordStep(
        SudokuTechnique technique, ref int steps, SudokuTechnique hardest)
    {
        steps++;
        return (SudokuTechnique)Math.Max((int)technique, (int)hardest);
    }

    private sealed class SolverState
    {
        private readonly int[] _board = new int[CellCount];
        private readonly int[] _candidates = new int[CellCount];

        public bool IsComplete
        {
            get
            {
                for (int i = 0; i < CellCount; i++)
                    if (_board[i] == 0) return false;
                return true;
            }
        }

        public SolverState(int[,] puzzle)
        {
            var usedByUnit = new int[Units.Length];
            for (int index = 0; index < CellCount; index++)
            {
                int value = puzzle[index / 9, index % 9];
                _board[index] = value;
                if (value == 0) continue;
                int bit = Bit(value);
                foreach (int unitIndex in Memberships[index]) usedByUnit[unitIndex] |= bit;
            }

            for (int index = 0; index < CellCount; index++)
            {
                if (_board[index] != 0) continue;
                int used = 0;
                foreach (int unitIndex in Memberships[index]) used |= usedByUnit[unitIndex];
                _candidates[index] = HyperSudokuRules.FullMask & ~used;
            }
        }

        public int ValueAt(int index) => _board[index];
        public int CandidatesAt(int index) => _candidates[index];

        public bool Place(int index, int value)
        {
            if (_board[index] != 0 || (_candidates[index] & Bit(value)) == 0) return false;

            _board[index] = value;
            _candidates[index] = 0;
            foreach (int peer in CellPeers[index])
                if (_board[peer] == 0)
                    Remove(peer, value);
            return true;
        }

        public bool Remove(int index, int value)
        {
            int updated = _candidates[index] & ~Bit(value);
            if (updated == _candidates[index]) return false;
            _candidates[index] = updated;
            return true;
        }

        public bool Restrict(int index, int allowedMask)
        {
            int updated = _candidates[index] & allowedMask;
            if (updated == _candidates[index]) return false;
            _candidates[index] = updated;
            return true;
        }
    }

    private static bool TryNakedSingle(SolverState state)
    {
        for (int cell = 0; cell < CellCount; cell++)
        {
            int candidates = state.CandidatesAt(cell);
            if (state.ValueAt(cell) == 0 && CountBits(candidates) == 1)
                return state.Place(cell, FirstValue(candidates));
        }
        return false;
    }

    private static bool TryHiddenSingle(SolverState state)
    {
        foreach (int[] unit in Units)
            for (int value = 1; value <= 9; value++)
            {
                int found = -1;
                int count = 0;
                int bit = Bit(value);
                foreach (int cell in unit)
                    if (state.ValueAt(cell) == 0 &&
                        (state.CandidatesAt(cell) & bit) != 0)
                    {
                        found = cell;
                        if (++count > 1) break;
                    }

                if (count == 1) return state.Place(found, value);
            }
        return false;
    }

    /// <summary>
    /// Generalized locked candidates: if all candidates in one unit lie in
    /// its overlap with another unit, remove that digit from the other unit.
    /// This includes the center box/hyper interaction.
    /// </summary>
    private static bool TryLockedCandidate(SolverState state)
    {
        foreach (Overlap overlap in Overlaps)
            for (int value = 1; value <= 9; value++)
            {
                int bit = Bit(value);
                int candidateCount = 0;
                bool candidatesAreInOverlap = true;
                foreach (int cell in Units[overlap.SourceUnit])
                    if (state.ValueAt(cell) == 0 && (state.CandidatesAt(cell) & bit) != 0)
                    {
                        candidateCount++;
                        if (!overlap.ContainsCell[cell])
                            candidatesAreInOverlap = false;
                    }

                if (candidateCount < 2 || !candidatesAreInOverlap) continue;
                foreach (int cell in Units[overlap.TargetUnit])
                    if (!overlap.ContainsCell[cell] && state.ValueAt(cell) == 0 &&
                        state.Remove(cell, value))
                        return true;
            }
        return false;
    }

    private static bool TryNakedPair(SolverState state)
    {
        foreach (int[] unit in Units)
            for (int i = 0; i < 8; i++)
            {
                int first = unit[i];
                int pair = state.CandidatesAt(first);
                if (state.ValueAt(first) != 0 || CountBits(pair) != 2) continue;

                for (int j = i + 1; j < 9; j++)
                {
                    int second = unit[j];
                    if (state.ValueAt(second) != 0 || state.CandidatesAt(second) != pair) continue;
                    foreach (int cell in unit)
                        if (cell != first && cell != second && state.ValueAt(cell) == 0)
                            for (int value = 1; value <= 9; value++)
                                if ((pair & Bit(value)) != 0 && state.Remove(cell, value))
                                    return true;
                }
            }
        return false;
    }

    private static bool TryHiddenPair(SolverState state)
    {
        foreach (int[] unit in Units)
            for (int firstValue = 1; firstValue <= 8; firstValue++)
                for (int secondValue = firstValue + 1; secondValue <= 9; secondValue++)
                {
                    int firstCell1 = -1, firstCell2 = -1, firstCount = 0;
                    int secondCell1 = -1, secondCell2 = -1, secondCount = 0;
                    foreach (int cell in unit)
                    {
                        if (state.ValueAt(cell) != 0) continue;
                        int candidates = state.CandidatesAt(cell);
                        if ((candidates & Bit(firstValue)) != 0)
                        {
                            if (firstCount++ == 0) firstCell1 = cell;
                            else if (firstCount == 2) firstCell2 = cell;
                        }
                        if ((candidates & Bit(secondValue)) != 0)
                        {
                            if (secondCount++ == 0) secondCell1 = cell;
                            else if (secondCount == 2) secondCell2 = cell;
                        }
                    }

                    if (firstCount != 2 || secondCount != 2 ||
                        firstCell1 != secondCell1 || firstCell2 != secondCell2)
                        continue;

                    int pair = Bit(firstValue) | Bit(secondValue);
                    if (state.Restrict(firstCell1, pair) || state.Restrict(firstCell2, pair))
                        return true;
                }
        return false;
    }

    private static bool TryNakedTriple(SolverState state)
    {
        foreach (int[] unit in Units)
            for (int i = 0; i < 7; i++)
            {
                int first = unit[i];
                int firstMask = state.CandidatesAt(first);
                int firstCount = CountBits(firstMask);
                if (state.ValueAt(first) != 0 || firstCount < 2 || firstCount > 3) continue;

                for (int j = i + 1; j < 8; j++)
                {
                    int second = unit[j];
                    int secondMask = state.CandidatesAt(second);
                    int secondCount = CountBits(secondMask);
                    if (state.ValueAt(second) != 0 || secondCount < 2 || secondCount > 3) continue;

                    for (int k = j + 1; k < 9; k++)
                    {
                        int third = unit[k];
                        int thirdMask = state.CandidatesAt(third);
                        int thirdCount = CountBits(thirdMask);
                        if (state.ValueAt(third) != 0 || thirdCount < 2 || thirdCount > 3) continue;

                        int triple = firstMask | secondMask | thirdMask;
                        if (CountBits(triple) != 3) continue;
                        foreach (int cell in unit)
                            if (cell != first && cell != second && cell != third && state.ValueAt(cell) == 0)
                                for (int value = 1; value <= 9; value++)
                                    if ((triple & Bit(value)) != 0 && state.Remove(cell, value))
                                        return true;
                    }
                }
            }
        return false;
    }

    private static bool TryXWing(SolverState state)
    {
        for (int value = 1; value <= 9; value++)
            if (TryXWingRows(state, value) || TryXWingColumns(state, value)) return true;
        return false;
    }

    private static bool TryXWingRows(SolverState state, int value)
    {
        for (int row1 = 0; row1 < 8; row1++)
        {
            int columns = CandidatePositionsInRow(state, row1, value);
            if (CountBits(columns) != 2) continue;
            for (int row2 = row1 + 1; row2 < 9; row2++)
            {
                if (CandidatePositionsInRow(state, row2, value) != columns) continue;
                for (int col = 0; col < 9; col++)
                    if ((columns & (1 << col)) != 0)
                        for (int row = 0; row < 9; row++)
                            if (row != row1 && row != row2 && state.ValueAt(row * 9 + col) == 0 &&
                                state.Remove(row * 9 + col, value))
                                return true;
            }
        }
        return false;
    }

    private static bool TryXWingColumns(SolverState state, int value)
    {
        for (int col1 = 0; col1 < 8; col1++)
        {
            int rows = CandidatePositionsInColumn(state, col1, value);
            if (CountBits(rows) != 2) continue;
            for (int col2 = col1 + 1; col2 < 9; col2++)
            {
                if (CandidatePositionsInColumn(state, col2, value) != rows) continue;
                for (int row = 0; row < 9; row++)
                    if ((rows & (1 << row)) != 0)
                        for (int col = 0; col < 9; col++)
                            if (col != col1 && col != col2 && state.ValueAt(row * 9 + col) == 0 &&
                                state.Remove(row * 9 + col, value))
                                return true;
            }
        }
        return false;
    }

    private static int CandidatePositionsInRow(SolverState state, int row, int value)
    {
        int positions = 0;
        for (int col = 0; col < 9; col++)
        {
            int cell = row * 9 + col;
            if (state.ValueAt(cell) == 0 && (state.CandidatesAt(cell) & Bit(value)) != 0)
                positions |= 1 << col;
        }
        return positions;
    }

    private static int CandidatePositionsInColumn(SolverState state, int col, int value)
    {
        int positions = 0;
        for (int row = 0; row < 9; row++)
        {
            int cell = row * 9 + col;
            if (state.ValueAt(cell) == 0 && (state.CandidatesAt(cell) & Bit(value)) != 0)
                positions |= 1 << row;
        }
        return positions;
    }

    private static SudokuDifficulty MapToDifficulty(SudokuTechnique technique, int steps)
    {
        switch (technique)
        {
            case SudokuTechnique.NakedSingle:
                if (steps <= 15) return SudokuDifficulty.Simple;
                if (steps <= 25) return SudokuDifficulty.Beginner;
                return SudokuDifficulty.Easy;
            case SudokuTechnique.HiddenSingle: return SudokuDifficulty.Novice;
            case SudokuTechnique.LockedCandidate: return SudokuDifficulty.Moderate;
            case SudokuTechnique.NakedPair:
            case SudokuTechnique.HiddenPair: return SudokuDifficulty.Advanced;
            case SudokuTechnique.NakedTriple: return SudokuDifficulty.Hard;
            case SudokuTechnique.XWing:
            case SudokuTechnique.Swordfish: return SudokuDifficulty.Expert;
            case SudokuTechnique.SearchRequired: return SudokuDifficulty.Hardest;
            default: return SudokuDifficulty.Easy;
        }
    }

    private static int[][] BuildMemberships()
    {
        var memberships = new List<int>[CellCount];
        for (int cell = 0; cell < CellCount; cell++) memberships[cell] = new List<int>(5);
        for (int unit = 0; unit < Units.Length; unit++)
            foreach (int cell in Units[unit]) memberships[cell].Add(unit);
        var result = new int[CellCount][];
        for (int cell = 0; cell < CellCount; cell++) result[cell] = memberships[cell].ToArray();
        return result;
    }

    private static int[][] BuildCellPeers()
    {
        var result = new int[CellCount][];
        for (int cell = 0; cell < CellCount; cell++)
        {
            var seen = new bool[CellCount];
            var peers = new List<int>(32);
            foreach (int unitIndex in Memberships[cell])
                foreach (int peer in Units[unitIndex])
                    if (peer != cell && !seen[peer])
                    {
                        seen[peer] = true;
                        peers.Add(peer);
                    }
            result[cell] = peers.ToArray();
        }
        return result;
    }

    private static Overlap[] BuildOverlaps()
    {
        var overlaps = new List<Overlap>();
        for (int source = 0; source < Units.Length; source++)
            for (int target = 0; target < Units.Length; target++)
            {
                if (source == target) continue;
                var shared = new bool[CellCount];
                int sharedCount = 0;
                foreach (int cell in Units[source])
                    if (Contains(Units[target], cell))
                    {
                        shared[cell] = true;
                        sharedCount++;
                    }
                if (sharedCount >= 2)
                    overlaps.Add(new Overlap(source, target, shared));
            }
        return overlaps.ToArray();
    }

    private static bool Contains(int[] values, int value)
    {
        for (int i = 0; i < values.Length; i++)
            if (values[i] == value) return true;
        return false;
    }

    private static int Bit(int value) => 1 << value;

    private static int FirstValue(int mask)
    {
        for (int value = 1; value <= 9; value++)
            if ((mask & Bit(value)) != 0) return value;
        return 0;
    }

    private static int CountBits(int mask)
    {
        int count = 0;
        while (mask != 0)
        {
            mask &= mask - 1;
            count++;
        }
        return count;
    }
}
