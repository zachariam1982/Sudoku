using System;
using System.Collections.Generic;
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
    public SudokuDifficulty Difficulty; // The analyzer-rated tier actually generated
    public KillerCage[] KillerCages; // Empty for non-Killer variants.
}
public class ScoringSystem : IScoringSystem
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
        IScorePenalties  penalties)
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
        IScorePenalties  penalties)
    {
        int diff    = DifficultyScore(difficulty);
        int time    = TimeScore(difficulty, elapsedSeconds);
        int pen     = penalties.TotalPenalty();
        int total   = Mathf.Max(0, diff + time - pen);
 
        return (total, diff, time, pen);
    }

    int IScoringSystem.GetAbsoluteMaximumScore(SudokuDifficulty difficulty) =>
        GetAbsoluteMaximumScore(difficulty);

    int IScoringSystem.Calculate(
        SudokuDifficulty difficulty, float elapsedSeconds, IScorePenalties penalties) =>
        Calculate(difficulty, elapsedSeconds, penalties);

    (int total, int diffScore, int timeScore, int penalty) IScoringSystem.CalculateDetailed(
        SudokuDifficulty difficulty, float elapsedSeconds, IScorePenalties penalties) =>
        CalculateDetailed(difficulty, elapsedSeconds, penalties);
}
public class ClassicSudokuModel : ISudokuModel
{
    private readonly ISudokuGenerator _generator;
    private readonly IGameHistoryRepository _historyRepository;

    public ClassicSudokuModel() : this(new ClassicSudokuGenerator(), new SqliteGameHistoryRepository()) { }

    public ClassicSudokuModel(ISudokuGenerator generator)
        : this(generator, new SqliteGameHistoryRepository()) { }

    public ClassicSudokuModel(IGameHistoryRepository historyRepository)
        : this(new ClassicSudokuGenerator(), historyRepository) { }

    public ClassicSudokuModel(ISudokuGenerator generator, IGameHistoryRepository historyRepository)
    {
        _generator = generator ?? new ClassicSudokuGenerator();
        _historyRepository = historyRepository ?? throw new ArgumentNullException(nameof(historyRepository));
    }

    public int[,]  Board     { get; private set; } = new int[9, 9];
    public bool[,] GivenMask { get; private set; } = new bool[9, 9];
    public int[,] PencilCandidateMasks { get; private set; } = new int[9, 9];
    private int _currentLevel = 1;
    private int _puzzleSeed = 1;
    private SudokuDifficulty _currentDifficulty = SudokuDifficulty.Easy;
    private SudokuResult ret;
    private const int _NoOfLastGames = 5;

    public SudokuDifficulty CurrentDifficulty { get { return _currentDifficulty; }}
    public int CurrentLevel { get {return _currentLevel;}}
    public int PuzzleSeed { get { return _puzzleSeed; } }
    public void SetLevel(int level)
    {
        _currentLevel = level;
        _puzzleSeed = level;
    }
    public void SetPuzzleSeed(int puzzleSeed) => _puzzleSeed = puzzleSeed;
    public void SetDifficulty(SudokuDifficulty difficulty) => _currentDifficulty = difficulty;
    // Both result paths use the same rules, including when the fifth result is a loss.
    public void increaseDifficulty() => UpdateDifficultyFromHistory();
    public void decreaseDifficulty(List<GameRecord> records = null) => UpdateDifficultyFromHistory(records);

    public void UpdateDifficultyFromHistory(List<GameRecord> records = null)
    {
        records = records ?? _historyRepository.GetLastNRecordByDate(_NoOfLastGames);
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
    public void LoadCurrentLevelPuzzle(int maxAttempts = 100000)
    {
        this.ret = _generator.Generate(_puzzleSeed, _currentDifficulty, maxAttempts);
        _currentDifficulty = ret.Difficulty;

        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
            {
                int value       = ret.Puzzle[row, col];
                Board[row, col]     = value;
                GivenMask[row, col] = value != 0;
                PencilCandidateMasks[row, col] = 0;
            }
    }

    public int[] GetOriginalPuzzleFlat()
    {
        var flat = new int[81];
        if (ret?.Puzzle == null) return flat;
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
                flat[row * 9 + col] = ret.Puzzle[row, col];
        return flat;
    }

    public int[] GetSolutionFlat()
    {
        var flat = new int[81];
        if (ret?.Solution == null) return flat;
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
                flat[row * 9 + col] = ret.Solution[row, col];
        return flat;
    }

    public bool TryLoadSavedPuzzle(int[] puzzleFlat, int[] solutionFlat)
    {
        if (!IsValidSavedPuzzle(puzzleFlat, solutionFlat)) return false;

        var puzzle = new int[9, 9];
        var solution = new int[9, 9];
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
            {
                int index = row * 9 + col;
                puzzle[row, col] = puzzleFlat[index];
                solution[row, col] = solutionFlat[index];
                Board[row, col] = puzzle[row, col];
                GivenMask[row, col] = puzzle[row, col] != 0;
                PencilCandidateMasks[row, col] = 0;
            }

        ret = new SudokuResult
        {
            Puzzle = puzzle,
            Solution = solution,
            Difficulty = _currentDifficulty
        };
        return true;
    }

    public static bool IsValidSavedPuzzle(int[] puzzle, int[] solution)
    {
        if (puzzle == null || solution == null || puzzle.Length != 81 || solution.Length != 81)
            return false;

        for (int index = 0; index < 81; index++)
            if (puzzle[index] < 0 || puzzle[index] > 9 || solution[index] < 1 ||
                solution[index] > 9 || (puzzle[index] != 0 && puzzle[index] != solution[index]))
                return false;

        for (int row = 0; row < 9; row++)
        {
            int rowMask = 0;
            int colMask = 0;
            for (int i = 0; i < 9; i++)
            {
                int rowBit = 1 << solution[row * 9 + i];
                int colBit = 1 << solution[i * 9 + row];
                if ((rowMask & rowBit) != 0 || (colMask & colBit) != 0) return false;
                rowMask |= rowBit;
                colMask |= colBit;
            }
        }

        for (int boxRow = 0; boxRow < 3; boxRow++)
            for (int boxCol = 0; boxCol < 3; boxCol++)
            {
                int mask = 0;
                for (int row = 0; row < 3; row++)
                    for (int col = 0; col < 3; col++)
                    {
                        int value = solution[(boxRow * 3 + row) * 9 + boxCol * 3 + col];
                        int bit = 1 << value;
                        if ((mask & bit) != 0) return false;
                        mask |= bit;
                    }
            }
        return true;
    }
    public bool SetValue(int row, int col, int value)
    {
        if (GivenMask[row, col]) return false;
        Board[row, col] = value;
        if (value != 0) PencilCandidateMasks[row, col] = 0;
        return true;
    }

    public void TogglePencilCandidate(int row, int col, int number)
    {
        if (row < 0 || row >= 9 || col < 0 || col >= 9 ||
            number < 1 || number > 9 || Board[row, col] != 0)
        {
            return;
        }

        PencilCandidateMasks[row, col] ^= 1 << number;
    }

    public void AutoFillPencilCandidates()
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (Board[row, col] != 0)
                {
                    PencilCandidateMasks[row, col] = 0;
                    continue;
                }

                int mask = 0;
                for (int number = 1; number <= 9; number++)
                    if (CanPlaceNumber(row, col, number)) mask |= 1 << number;

                PencilCandidateMasks[row, col] = mask;
            }
        }
    }

    public void RemovePencilCandidateFromPeers(int enteredRow, int enteredCol, int number)
    {
        int bit = ~(1 << number);

        for (int col = 0; col < 9; col++)
            if (col != enteredCol) PencilCandidateMasks[enteredRow, col] &= bit;

        for (int row = 0; row < 9; row++)
            if (row != enteredRow) PencilCandidateMasks[row, enteredCol] &= bit;

        int boxStartRow = (enteredRow / 3) * 3;
        int boxStartCol = (enteredCol / 3) * 3;
        for (int row = boxStartRow; row < boxStartRow + 3; row++)
            for (int col = boxStartCol; col < boxStartCol + 3; col++)
                if (row != enteredRow || col != enteredCol)
                    PencilCandidateMasks[row, col] &= bit;
    }

    public void LoadPencilCandidateMasks(int[] masks)
    {
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
                PencilCandidateMasks[row, col] =
                    masks != null && masks.Length == 81 && Board[row, col] == 0
                        ? masks[row * 9 + col]
                        : 0;
    }

    public void SetPencilCandidateMask(int row, int col, int mask)
    {
        if (row < 0 || row >= 9 || col < 0 || col >= 9) return;
        PencilCandidateMasks[row, col] = Board[row, col] == 0 ? mask & 0x3FE : 0;
    }

    private bool CanPlaceNumber(int targetRow, int targetCol, int number)
    {
        for (int col = 0; col < 9; col++)
            if (Board[targetRow, col] == number) return false;

        for (int row = 0; row < 9; row++)
            if (Board[row, targetCol] == number) return false;

        int boxStartRow = (targetRow / 3) * 3;
        int boxStartCol = (targetCol / 3) * 3;
        for (int row = boxStartRow; row < boxStartRow + 3; row++)
            for (int col = boxStartCol; col < boxStartCol + 3; col++)
                if (Board[row, col] == number) return false;

        return true;
    }
    public bool IsGiven(int row, int col) => GivenMask[row, col];
    public int  GetValue(int row, int col) => Board[row, col];
    public int  GetSolutionValue(int row, int col) => ret.Solution[row, col];
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
