using System;
using System.Linq;
using System.Collections;
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
    internal struct CandidateSet : IEnumerable<int>
    {
        public const int FullMask = 0x3FE; // Candidate digits 1 through 9.
        private int _mask;

        public CandidateSet(int mask) => _mask = mask;
        public int Count => CountBits(_mask);

        public void Clear() => _mask = 0;

        public bool Add(int value)
        {
            int bit = 1 << value;
            bool added = (_mask & bit) == 0;
            _mask |= bit;
            return added;
        }

        public bool Remove(int value)
        {
            int bit = 1 << value;
            bool removed = (_mask & bit) != 0;
            _mask &= ~bit;
            return removed;
        }

        public bool Contains(int value) =>
            value >= 1 && value <= 9 && (_mask & (1 << value)) != 0;

        public bool SetEquals(CandidateSet other) => _mask == other._mask;

        public void UnionWith(CandidateSet other) => _mask |= other._mask;

        public int First()
        {
            if (_mask == 0)
                throw new InvalidOperationException("The candidate set is empty.");

            for (int value = 1; value <= 9; value++)
                if ((_mask & (1 << value)) != 0)
                    return value;

            throw new InvalidOperationException("The candidate set is empty.");
        }

        public bool Any(Func<int, bool> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            for (int value = 1; value <= 9; value++)
                if ((_mask & (1 << value)) != 0 && predicate(value))
                    return true;

            return false;
        }

        public int[] ToArray()
        {
            int[] values = new int[Count];
            int index = 0;

            for (int value = 1; value <= 9; value++)
                if ((_mask & (1 << value)) != 0)
                    values[index++] = value;

            return values;
        }

        public Enumerator GetEnumerator() => new Enumerator(_mask);
        IEnumerator<int> IEnumerable<int>.GetEnumerator() => new Enumerator(_mask);
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(_mask);

        public struct Enumerator : IEnumerator<int>
        {
            private readonly int _mask;
            private int _nextValue;
            private int _current;

            public Enumerator(int mask)
            {
                _mask = mask;
                _nextValue = 1;
                _current = 0;
            }

            public int Current => _current;
            object IEnumerator.Current => _current;

            public bool MoveNext()
            {
                while (_nextValue <= 9)
                {
                    int value = _nextValue++;
                    if ((_mask & (1 << value)) == 0) continue;

                    _current = value;
                    return true;
                }

                return false;
            }

            public void Reset()
            {
                _nextValue = 1;
                _current = 0;
            }

            public void Dispose() { }
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

    internal sealed class SolverState
    {
        public int[,] Board { get; }
        public CandidateSet[,] Candidates { get; }

        public SolverState(int[,] puzzle)
        {
            Board = (int[,])puzzle.Clone();

            Candidates = new CandidateSet[9, 9];

            int[] rowUsed = new int[9];
            int[] columnUsed = new int[9];
            int[] boxUsed = new int[9];

            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    int value = Board[row, col];
                    if (value < 1 || value > 9) continue;

                    int bit = 1 << value;
                    rowUsed[row] |= bit;
                    columnUsed[col] |= bit;
                    boxUsed[(row / 3) * 3 + col / 3] |= bit;
                }
            }

            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    if (Board[row, col] != 0)
                    {
                        Candidates[row, col] = new CandidateSet(0);
                        continue;
                    }

                    int box = (row / 3) * 3 + col / 3;
                    int candidates = CandidateSet.FullMask &
                        ~(rowUsed[row] | columnUsed[col] | boxUsed[box]);
                    Candidates[row, col] = new CandidateSet(candidates);
                }
            }
        }

        public void Place(int row,int col,int value)
        {
            Board[row, col] = value;
            Candidates[row, col] = new CandidateSet(0);

            // Remove from row / column.
            for (int i = 0; i < 9; i++)
            {
                if (Board[row, i] == 0)
                    RemoveCandidate(row, i, value);

                if (Board[i, col] == 0)
                    RemoveCandidate(i, col, value);
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
                        RemoveCandidate(r, c, value);
                    }
                }
            }
        }

        public bool RemoveCandidate(int row, int col, int value)
        {
            CandidateSet candidates = Candidates[row, col];
            if (!candidates.Remove(value)) return false;

            Candidates[row, col] = candidates;
            return true;
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

            if (TryHiddenPair(state))
            {
                hardest = Max(
                    hardest,
                    SudokuTechnique.HiddenPair);

                steps++;
                continue;
            }

            if (TryNakedTriple(state))
            {
                hardest = Max(
                    hardest,
                    SudokuTechnique.NakedTriple);

                steps++;
                continue;
            }

            if (TryXWing(state))
            {
                hardest = Max(
                    hardest,
                    SudokuTechnique.XWing);

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

                            if (state.RemoveCandidate(commonRow, col, number))
                                changed = true;
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

                            if (state.RemoveCandidate(row, commonCol, number))
                                changed = true;
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

            CandidateSet firstCandidates =
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

                CandidateSet secondCandidates =
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

                    if (state.RemoveCandidate(cell.row, cell.col, pair[0]))
                        changed = true;

                    if (state.RemoveCandidate(cell.row, cell.col, pair[1]))
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
    private static bool TryHiddenPair(
        SolverState state)
    {
        // =====================================================
        // ROWS
        // =====================================================

        for (int row = 0; row < 9; row++)
        {
            var cells =
                new List<(int row, int col)>();

            for (int col = 0; col < 9; col++)
            {
                if (state.Board[row, col] == 0)
                    cells.Add((row, col));
            }

            if (TryHiddenPairInUnit(
                    state,
                    cells))
            {
                return true;
            }
        }

        // =====================================================
        // COLUMNS
        // =====================================================

        for (int col = 0; col < 9; col++)
        {
            var cells =
                new List<(int row, int col)>();

            for (int row = 0; row < 9; row++)
            {
                if (state.Board[row, col] == 0)
                    cells.Add((row, col));
            }

            if (TryHiddenPairInUnit(
                    state,
                    cells))
            {
                return true;
            }
        }

        // =====================================================
        // 3x3 BOXES
        // =====================================================

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

                if (TryHiddenPairInUnit(
                        state,
                        cells))
                {
                    return true;
                }
            }
        }

        return false;
    }
    private static bool TryHiddenPairInUnit(
        SolverState state,
        List<(int row, int col)> cells)
    {
        /*
        * Look for two numbers that both appear
        * as candidates in exactly the same two cells.
        *
        * Example:
        *
        * Cell A = {1, 3, 7}
        * Cell B = {2, 3, 7}
        *
        * If 3 and 7 occur nowhere else in this unit:
        *
        * Cell A -> {3,7}
        * Cell B -> {3,7}
        */

        for (int number1 = 1;
            number1 <= 8;
            number1++)
        {
            var number1Cells =
                cells.Where(
                    cell =>
                        state.Candidates[
                            cell.row,
                            cell.col]
                        .Contains(number1))
                .ToList();

            if (number1Cells.Count != 2)
                continue;

            for (int number2 = number1 + 1;
                number2 <= 9;
                number2++)
            {
                var number2Cells =
                    cells.Where(
                        cell =>
                            state.Candidates[
                                cell.row,
                                cell.col]
                            .Contains(number2))
                    .ToList();

                if (number2Cells.Count != 2)
                    continue;

                /*
                * Both numbers must occur in
                * exactly the same two cells.
                */
                bool sameCells =
                    number1Cells.All(
                        cell =>
                            number2Cells.Contains(cell));

                if (!sameCells)
                    continue;

                bool changed = false;

                foreach (var cell
                        in number1Cells)
                {
                    CandidateSet candidates =
                        state.Candidates[
                            cell.row,
                            cell.col];

                    int[] values =
                        candidates.ToArray();

                    foreach (int value in values)
                    {
                        if (value == number1 ||
                            value == number2)
                        {
                            continue;
                        }

                        if (state.RemoveCandidate(cell.row, cell.col, value))
                            changed = true;
                    }
                }

                if (changed)
                    return true;
            }
        }

        return false;
    }
    private static bool TryNakedTriple(
        SolverState state)
    {
        // =====================================================
        // ROWS
        // =====================================================

        for (int row = 0; row < 9; row++)
        {
            var cells =
                new List<(int row, int col)>();

            for (int col = 0; col < 9; col++)
            {
                if (state.Board[row, col] == 0)
                    cells.Add((row, col));
            }

            if (TryNakedTripleInUnit(
                    state,
                    cells))
            {
                return true;
            }
        }

        // =====================================================
        // COLUMNS
        // =====================================================

        for (int col = 0; col < 9; col++)
        {
            var cells =
                new List<(int row, int col)>();

            for (int row = 0; row < 9; row++)
            {
                if (state.Board[row, col] == 0)
                    cells.Add((row, col));
            }

            if (TryNakedTripleInUnit(
                    state,
                    cells))
            {
                return true;
            }
        }

        // =====================================================
        // BOXES
        // =====================================================

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

                if (TryNakedTripleInUnit(
                        state,
                        cells))
                {
                    return true;
                }
            }
        }

        return false;
    }
    private static bool TryNakedTripleInUnit(
        SolverState state,
        List<(int row, int col)> cells)
    {
        /*
        * Naked Triple:
        *
        * Pick three cells whose combined candidate
        * set contains exactly three numbers.
        *
        * Example:
        *
        * A = {2,7}
        * B = {2,8}
        * C = {7,8}
        *
        * Union = {2,7,8}
        *
        * Therefore 2,7,8 can be eliminated from
        * every other cell in the unit.
        */

        var possibleCells =
            cells.Where(
                cell =>
                {
                    int count =
                        state.Candidates[
                            cell.row,
                            cell.col].Count;

                    return
                        count >= 2 &&
                        count <= 3;
                })
            .ToList();

        for (int i = 0;
            i < possibleCells.Count - 2;
            i++)
        {
            for (int j = i + 1;
                j < possibleCells.Count - 1;
                j++)
            {
                for (int k = j + 1;
                    k < possibleCells.Count;
                    k++)
                {
                    var first =
                        possibleCells[i];

                    var second =
                        possibleCells[j];

                    var third =
                        possibleCells[k];

                    CandidateSet triple =
                        state.Candidates[first.row, first.col];

                    triple.UnionWith(
                        state.Candidates[second.row, second.col]);

                    triple.UnionWith(
                        state.Candidates[third.row, third.col]);

                    /*
                    * Three cells must collectively
                    * contain exactly three candidates.
                    */
                    if (triple.Count != 3)
                        continue;

                    /*
                    * Defensive check:
                    * don't create an empty candidate set
                    * in another cell.
                    */
                    bool unsafeTriple = false;

                    foreach (var cell in cells)
                    {
                        if (cell == first ||
                            cell == second ||
                            cell == third)
                        {
                            continue;
                        }

                        CandidateSet candidates =
                            state.Candidates[
                                cell.row,
                                cell.col];

                        if (candidates.Count == 0)
                            continue;

                        bool intersects =
                            candidates.Any(
                                value =>
                                    triple.Contains(value));

                        if (!intersects)
                            continue;

                        bool hasCandidateOutsideTriple =
                            candidates.Any(
                                value =>
                                    !triple.Contains(value));

                        if (!hasCandidateOutsideTriple)
                        {
                            unsafeTriple = true;
                            break;
                        }
                    }

                    if (unsafeTriple)
                        continue;

                    bool changed = false;

                    foreach (var cell in cells)
                    {
                        if (cell == first ||
                            cell == second ||
                            cell == third)
                        {
                            continue;
                        }

                        CandidateSet candidates =
                            state.Candidates[
                                cell.row,
                                cell.col];

                        foreach (int value
                                in triple)
                        {
                            if (state.RemoveCandidate(cell.row, cell.col, value))
                                changed = true;
                        }
                    }

                    if (changed)
                        return true;
                }
            }
        }

        return false;
    }
    private static bool TryXWing(
        SolverState state)
    {
        for (int number = 1;
            number <= 9;
            number++)
        {
            if (TryXWingRows(
                    state,
                    number))
            {
                return true;
            }

            if (TryXWingColumns(
                    state,
                    number))
            {
                return true;
            }
        }

        return false;
    }
private static bool TryXWingRows(
    SolverState state,
    int number)
{
    /*
     * Find two rows where 'number' can occur
     * in exactly the same two columns.
     *
     * Example:
     *
     *          C2    C7
     *
     * Row 1     X     X
     * Row 5     X     X
     *
     * number must occupy opposite corners.
     *
     * Therefore remove number from C2 and C7
     * in every OTHER row.
     */

    for (int row1 = 0;
         row1 < 8;
         row1++)
    {
        List<int> cols1 =
            new List<int>();

        for (int col = 0;
             col < 9;
             col++)
        {
            if (state.Board[
                    row1,
                    col] == 0 &&
                state.Candidates[
                    row1,
                    col]
                .Contains(number))
            {
                cols1.Add(col);
            }
        }

        if (cols1.Count != 2)
            continue;

        for (int row2 = row1 + 1;
             row2 < 9;
             row2++)
        {
            List<int> cols2 =
                new List<int>();

            for (int col = 0;
                 col < 9;
                 col++)
            {
                if (state.Board[
                        row2,
                        col] == 0 &&
                    state.Candidates[
                        row2,
                        col]
                    .Contains(number))
                {
                    cols2.Add(col);
                }
            }

            if (cols2.Count != 2)
                continue;

            if (cols1[0] != cols2[0] ||
                cols1[1] != cols2[1])
            {
                continue;
            }

            bool changed = false;

            int col1 = cols1[0];
            int col2 = cols1[1];

            for (int row = 0;
                 row < 9;
                 row++)
            {
                if (row == row1 ||
                    row == row2)
                {
                    continue;
                }

                if (state.Board[
                        row,
                        col1] == 0)
                {
                    if (state.RemoveCandidate(row, col1, number))
                        changed = true;
                }

                if (state.Board[
                        row,
                        col2] == 0)
                {
                    if (state.RemoveCandidate(row, col2, number))
                        changed = true;
                }
            }

            if (changed)
                return true;
        }
    }

    return false;
}
private static bool TryXWingColumns(
    SolverState state,
    int number)
{
    /*
     * Same idea, rotated 90 degrees.
     *
     * Find two columns where 'number'
     * appears as a candidate in exactly
     * the same two rows.
     */

    for (int col1 = 0;
         col1 < 8;
         col1++)
    {
        List<int> rows1 =
            new List<int>();

        for (int row = 0;
             row < 9;
             row++)
        {
            if (state.Board[
                    row,
                    col1] == 0 &&
                state.Candidates[
                    row,
                    col1]
                .Contains(number))
            {
                rows1.Add(row);
            }
        }

        if (rows1.Count != 2)
            continue;

        for (int col2 = col1 + 1;
             col2 < 9;
             col2++)
        {
            List<int> rows2 =
                new List<int>();

            for (int row = 0;
                 row < 9;
                 row++)
            {
                if (state.Board[
                        row,
                        col2] == 0 &&
                    state.Candidates[
                        row,
                        col2]
                    .Contains(number))
                {
                    rows2.Add(row);
                }
            }

            if (rows2.Count != 2)
                continue;

            if (rows1[0] != rows2[0] ||
                rows1[1] != rows2[1])
            {
                continue;
            }

            bool changed = false;

            int row1 = rows1[0];
            int row2 = rows1[1];

            for (int col = 0;
                 col < 9;
                 col++)
            {
                if (col == col1 ||
                    col == col2)
                {
                    continue;
                }

                if (state.Board[
                        row1,
                        col] == 0)
                {
                    if (state.RemoveCandidate(row1, col, number))
                        changed = true;
                }

                if (state.Board[
                        row2,
                        col] == 0)
                {
                    if (state.RemoveCandidate(row2, col, number))
                        changed = true;
                }
            }

            if (changed)
                return true;
        }
    }

    return false;
}
}

