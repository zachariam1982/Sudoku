using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum SudokuDifficulty
{   
    Simple,
    Beginner,
    Easy,
    Novice,
    Moderate,
    Advanced,
    Hard,
    Expert,
    Hardest
}
public class SudokuResult
{
    public int[,] Puzzle;   // The grid with holes (0 represents empty)
    public int[,] Solution; // The completed grid
}
public static class ScoringSystem
{
    // ── Penalty constants ─────────────────────────────────────────────────────
    public const int PenaltyPerMistake        = 2;  // wrong manual entry (conflict)
    public const int PenaltySOSFillsEmpty     = 5;  // SOS filled a blank cell
    public const int PenaltySOSFixesWrong     = 7;  // SOS corrected a wrong entry

    private const int minutes                 = 60;
 
    // ── Par times (seconds) ───────────────────────────────────────────────────
    private static readonly int[] ParSeconds =
    {
         2 * minutes,  // Simple    
         4 * minutes,  // Beginner 
         6 * minutes,  // Easy     
         8 * minutes,  // Novice    
        10 * minutes,  // Moderate  
        13 * minutes,  // Advanced 
        18 * minutes,  // Hard    
        30 * minutes,  // Expert  
        50 * minutes,  // Hardest 
    };
 
    private static int DifficultyScore(SudokuDifficulty difficulty)
    {
        int ordinal = (int)difficulty;
        return Mathf.RoundToInt((ordinal + 1) / 9f * 100f);
    }

    private const float DecayRate = 0.002f;
 
    private static int TimeScore(SudokuDifficulty difficulty, float elapsedSeconds)
    {
        int par = ParSeconds[(int)difficulty];
        if (elapsedSeconds <= par) return 100;
        float overtime = elapsedSeconds - par;
        return Mathf.Max(0, Mathf.RoundToInt(100f * Mathf.Exp(-DecayRate * overtime)));
    }
 
    // ── Public API ────────────────────────────────────────────────────────────
 
    public static int GetAbsoluteMaximumScore(SudokuDifficulty difficulty) => 100 + DifficultyScore(difficulty);
    /// <summary>Returns final score out of 200, floored at 0.</summary>
    public static int Calculate(
        SudokuDifficulty difficulty,
        float            elapsedSeconds,
        ScorePenalties   penalties)
    {
        var (total, _, _, _) = CalculateDetailed(difficulty, elapsedSeconds, penalties);
        return total;
    }
 
    /// <summary>
    /// Returns a full breakdown: total, difficulty pts, time pts, total penalty.
    /// Total is floored at 0.
    /// </summary>
    public static (int total, int diffScore, int timeScore, int penalty) CalculateDetailed(
        SudokuDifficulty difficulty,
        float            elapsedSeconds,
        ScorePenalties   penalties)
    {
        int diff    = DifficultyScore(difficulty);
        int time    = TimeScore(difficulty, elapsedSeconds);
        int pen     = penalties.TotalPenalty();
        int total   = Mathf.Max(0, diff + time - pen);
 
        return (total, diff, time, pen);
    }
}

public class ScorePenalties
{
    public int Mistakes      { get; set; } // wrong manual entries
    public int SOSEmptyCells { get; set; } // empty cells SOS filled
    public int SOSWrongCells { get; set; } // wrong cells SOS fixed

    public ScorePenalties(int arg1, int arg2, int arg3)
    {
        Mistakes = arg1;
        SOSEmptyCells = arg2;
        SOSWrongCells = arg3;
    }
    public void AddMistake()       => Mistakes++;
    public void AddSOSEmptyCell()  => SOSEmptyCells++;
    public void AddSOSWrongCell()  => SOSWrongCells++;
 
    public int TotalPenalty() =>
        Mistakes      * ScoringSystem.PenaltyPerMistake    +
        SOSEmptyCells * ScoringSystem.PenaltySOSFillsEmpty +
        SOSWrongCells * ScoringSystem.PenaltySOSFixesWrong;
 
    public void Reset()
    {
        Mistakes = SOSEmptyCells = SOSWrongCells = 0;
    }
}
public static class SudokuGenerator
{
    private static ( int minClues, int maxClues) GetSearchRange( SudokuDifficulty difficulty)
    {
        return difficulty switch
        {
            SudokuDifficulty.Simple => (66, 71),
            SudokuDifficulty.Beginner => (56, 65),
            SudokuDifficulty.Easy => (45, 55),
            SudokuDifficulty.Novice => (36, 52),
            SudokuDifficulty.Moderate => (30, 48),
            SudokuDifficulty.Advanced => (27, 44),
            SudokuDifficulty.Hard => (24, 40),
            SudokuDifficulty.Expert => (21, 36),
            SudokuDifficulty.Hardest => (17, 32),
            _ => (25, 55)
        };
    }
    public static SudokuResult GenerateSudoku( int level, SudokuDifficulty requestedDifficulty)
    {
        const int MaxAttempts = 100;
        var (minClues, maxClues) = GetSearchRange(requestedDifficulty);

        for (int attempt = 0;attempt < MaxAttempts;attempt++)
        {
            int seed = unchecked(level * 397 ^ ((int)requestedDifficulty + 1) * 7919 ^ attempt * 104729);
            System.Random rng = new System.Random(seed);
            int[,] solution = new int[9, 9];

            FillBoard( solution, rng);

            int[,] puzzle = (int[,])solution.Clone();
            List<int> cells = Enumerable.Range(0, 81).OrderBy(_ => rng.Next()).ToList();
            int clueCount = 81;

            foreach (int index in cells)
            {
                int row = index / 9;
                int col = index % 9;
                int previous = puzzle[row, col];

                puzzle[row, col] = 0;

                if (!SudokuSolver.HasUniqueSolution(puzzle))
                {
                    puzzle[row, col] = previous;
                    continue;
                }

                clueCount--;

                if (clueCount > maxClues) continue;
                if (clueCount < minClues) break;

                SudokuDifficultyResult rating = SudokuDifficultyAnalyzer.Analyze(puzzle);

                if (requestedDifficulty == SudokuDifficulty.Simple || 
                    requestedDifficulty == SudokuDifficulty.Beginner ||
                    (int)rating.Difficulty >= (int)requestedDifficulty || 
                    (int)rating.Difficulty - 1 == (int)requestedDifficulty)
                {
                    return new SudokuResult
                    {
                        Puzzle = (int[,])puzzle.Clone(),
                        Solution = (int[,])solution.Clone()
                    };
                }
            }
        }

        throw new InvalidOperationException( $"Could not generate a " + $"{requestedDifficulty} Sudoku " + $"after {MaxAttempts} generation paths.");
    }
    private static bool FillBoard(int[,] board, System.Random rng)
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (board[row, col] == 0)
                {
                    // Shuffle numbers 1-9 deterministically using our seeded RNG
                    List<int> numbers = Enumerable.Range(1, 9).OrderBy(x => rng.Next()).ToList();

                    foreach (int num in numbers)
                    {
                        if (IsValid(board, row, col, num))
                        {
                            board[row, col] = num;
                            if (FillBoard(board, rng)) return true;
                            board[row, col] = 0;
                        }
                    }
                    return false;
                }
            }
        }
        return true;
    }
    private static bool IsValid(int[,] board, int row, int col, int num)
    {
        for (int i = 0; i < 9; i++)
        {
            // Check Row and Column
            if (board[row, i] == num || board[i, col] == num) return false;

            // Check 3x3 Box
            int startRow = (row / 3) * 3;
            int startCol = (col / 3) * 3;
            if (board[startRow + (i / 3), startCol + (i % 3)] == num) return false;
        }
        return true;
    }
}
public class SudokuModel
{
    public int[,]  Board     { get; private set; } = new int[9, 9];
    public bool[,] GivenMask { get; private set; } = new bool[9, 9];
    private int _currentLevel = 1;
    private SudokuDifficulty _currentDifficulty = SudokuDifficulty.Easy;
    private SudokuResult ret;
    private const int _NoOfLastGames = 5;

    public SudokuDifficulty CurrentDifficulty { get { return _currentDifficulty; }}
    public int CurrentLevel { get {return _currentLevel;}}
    public void SetLevel(int level) => _currentLevel = level;
    public void SetDifficulty(SudokuDifficulty difficulty) => _currentDifficulty = difficulty;
    // Both result paths use the same rules, including when the fifth result is a loss.
    public void increaseDifficulty() => UpdateDifficultyFromHistory();
    public void decreaseDifficulty(List<GameRecord> records = null) => UpdateDifficultyFromHistory(records);

    public void UpdateDifficultyFromHistory(List<GameRecord> records = null)
    {
        records = records ?? GameDatabase.GetLastNRecordByDate(_NoOfLastGames);
        SudokuDifficulty previous = _currentDifficulty;
        _currentDifficulty = CalculateNextDifficulty(records, _currentDifficulty);

        if (previous != _currentDifficulty)
            Debug.Log($"Progression: {previous} -> {_currentDifficulty}, latest result ID: {records[0].Id}");
    }

    /// <summary>
    /// Records must be newest first (database ID order). Derive the next tier from
    /// the results, never from a temporary RETAKE tier or an already adjusted tier.
    /// Re-evaluating unchanged history is therefore idempotent.
    /// </summary>
    public static SudokuDifficulty CalculateNextDifficulty(
        IReadOnlyList<GameRecord> records, SudokuDifficulty fallback)
    {
        if (records == null || records.Count == 0 || records[0] == null)
            return fallback;

        int tier = records[0].Difficulty;
        if (tier < (int)SudokuDifficulty.Simple || tier > (int)SudokuDifficulty.Hardest)
            return fallback;

        SudokuDifficulty baseline = (SudokuDifficulty)tier;
        // Even a short history must restore normal progression after RETAKE.
        if (records.Count < _NoOfLastGames) return baseline;

        int wins = 0;
        long totalPoints = 0;
        for (int i = 0; i < _NoOfLastGames; i++)
        {
            GameRecord record = records[i];
            // A new tier needs its own five-result window before another change.
            if (record == null || record.Difficulty != tier) return baseline;
            if (record.IsWon) wins++;
            totalPoints += record.Points;
        }

        long possiblePoints = (long)ScoringSystem.GetAbsoluteMaximumScore(baseline) * _NoOfLastGames;
        // Integer comparisons keep the 80% and 45% boundaries exact.
        if (wins >= 4 && totalPoints * 5 >= possiblePoints * 4)
            return (SudokuDifficulty)Math.Min((int)SudokuDifficulty.Hardest, tier + 1);

        if (wins <= 2 || totalPoints * 20 < possiblePoints * 9)
            return (SudokuDifficulty)Math.Max((int)SudokuDifficulty.Simple, tier - 1);

        return baseline;
    }
    public void LoadCurrentLevelPuzzle()
    {
        this.ret = SudokuGenerator.GenerateSudoku(_currentLevel, _currentDifficulty);

        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
            {
                int value       = ret.Puzzle[row, col];
                Board[row, col]     = value;
                GivenMask[row, col] = value != 0;
            }
    }
    public bool SetValue(int row, int col, int value)
    {
        if (GivenMask[row, col]) return false;
        Board[row, col] = value;
        return true;
    }
    public bool IsGiven(int row, int col) => GivenMask[row, col];
    public int  GetValue(int row, int col) => Board[row, col];
    public int  GetSolutionValue(int row, int col) => ret.Solution[row, col];
    public bool IsCellCorrect(int row, int col)
    {
        int current = Board[row, col];
        if (current == 0) return true;
        return current == ret.Solution[row, col];
    }
    public bool IsCellEmpty(int row, int col) => Board[row, col] == 0;
    public bool HasConflict(int row, int col)
    {
        int value = Board[row, col];
        if (value == 0) return false;

        // Check row
        for (int c = 0; c < 9; c++)
            if (c != col && Board[row, c] == value) return true;

        // Check column
        for (int r = 0; r < 9; r++)
            if (r != row && Board[r, col] == value) return true;

        // Check 3x3 box
        int boxRow = (row / 3) * 3;
        int boxCol = (col / 3) * 3;
        for (int r = boxRow; r < boxRow + 3; r++)
            for (int c = boxCol; c < boxCol + 3; c++)
                if ((r != row || c != col) && Board[r, c] == value) return true;

        return false;
    }
    public bool Validate()
    {
        for (int i = 0; i < 9; i++)
        {
            bool[] rowSeen = new bool[10];
            bool[] colSeen = new bool[10];
            for (int j = 0; j < 9; j++)
            {
                int rv = Board[i, j]; int cv = Board[j, i];
                if (rv != 0) { if (rowSeen[rv]) return false; rowSeen[rv] = true; }
                if (cv != 0) { if (colSeen[cv]) return false; colSeen[cv] = true; }
            }
        }
        for (int br = 0; br < 3; br++)
            for (int bc = 0; bc < 3; bc++)
            {
                bool[] seen = new bool[10];
                for (int r = 0; r < 3; r++)
                    for (int c = 0; c < 3; c++)
                    {
                        int v = Board[br * 3 + r, bc * 3 + c];
                        if (v != 0) { if (seen[v]) return false; seen[v] = true; }
                    }
            }
        return true;
    }
    public bool IsComplete()
    {
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
                if (Board[row, col] == 0) return false;
        return true;
    }
}