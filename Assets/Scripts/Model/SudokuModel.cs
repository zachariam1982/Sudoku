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
    public static SudokuResult GenerateSudoku(int level, SudokuDifficulty difficulty)
    {
        // 1. Initialize Seeded Random
        System.Random rng = new System.Random(level);

        int[,] solution = new int[9, 9];
        FillBoard(solution, rng);

        // Create a deep copy for the puzzle
        int[,] puzzle = (int[,])solution.Clone();

        // 2. Determine how many clues to keep
        int targetClues = difficulty switch
        {
            SudokuDifficulty.Simple    => rng.Next(66, 72), // Too easy
            SudokuDifficulty.Beginner  => rng.Next(60, 66), // Very easy, almost full board
            SudokuDifficulty.Easy      => rng.Next(54, 60), // Standard easy
            SudokuDifficulty.Novice    => rng.Next(48, 54), // Bridge between Easy and Moderate
            SudokuDifficulty.Moderate  => rng.Next(42, 48), // Standard medium
            SudokuDifficulty.Advanced  => rng.Next(36, 42), // Bridge between Moderate and Hard
            SudokuDifficulty.Hard      => rng.Next(30, 36), // Standard hard
            SudokuDifficulty.Expert    => rng.Next(24, 30), // Demands advanced logic techniques
            SudokuDifficulty.Hardest   => rng.Next(17, 24), // Absolute minimum for unique puzzles
            _ => 30
        };

        // 3. Dig holes in the puzzle
        int holesToMake = 81 - targetClues;
        while (holesToMake > 0)
        {
            int row = rng.Next(0, 9);
            int col = rng.Next(0, 9);

            if (puzzle[row, col] != 0)
            {
                puzzle[row, col] = 0;
                holesToMake--;
            }
        }

        return new SudokuResult
        {
            Puzzle = puzzle,
            Solution = solution
        };
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
    private const float _NoOfLastGamesFloat = _NoOfLastGames;

    public SudokuDifficulty CurrentDifficulty { get { return _currentDifficulty; }}
    public int CurrentLevel { get {return _currentLevel;}}
    public void SetLevel(int level)
    {
        _currentLevel = level;
    }
    public void SetDifficulty(SudokuDifficulty difficulty) => _currentDifficulty = difficulty;
    public void increaseDifficulty() 
    {
        var lst = GameDatabase.GetLastNRecordByDate(_NoOfLastGames);
        bool AllDifficultySame = true;
        int wins = 0;
        float efficiencySum = 0f;

        if (lst == null || lst.Count < _NoOfLastGames) return;

        Debug.Log($"Last {_NoOfLastGames} points");
        for(int i = 0; i < lst.Count; i++)
        {
            SudokuDifficulty matchDifficulty = (SudokuDifficulty)lst[i].Difficulty;
            int maxScore = ScoringSystem.GetAbsoluteMaximumScore(matchDifficulty);

            Debug.Log($" {((float)lst[0].Points / maxScore)}");
            if(i < (_NoOfLastGames - 1) && lst[i].Difficulty != lst[i + 1].Difficulty) AllDifficultySame = false;
            if(lst[i].IsWon) ++wins;
            
            efficiencySum += (float)lst[i].Points / maxScore;
        }

        if (AllDifficultySame) 
        {
            float finalEfficiency = efficiencySum / _NoOfLastGamesFloat;

            if (finalEfficiency >= 0.80f && wins >= 4)
            {
                var prev = _currentDifficulty;
                _currentDifficulty = (SudokuDifficulty)Math.Min((int)SudokuDifficulty.Hardest, (int)_currentDifficulty + 1);
                Debug.Log($"CONGRATS!!!! Moving to next tier. Current Difficulty: {prev} promoting to {_currentDifficulty}");
            }
            else if (wins <= 2 || finalEfficiency < 0.45f)
            {
                decreaseDifficulty(lst);
            }
        }
        else
        {
            _currentDifficulty = (SudokuDifficulty)lst[0].Difficulty;
        }
    }
    public void decreaseDifficulty(List<GameRecord> lst = null)
    {
        bool AllDifficultySame = true;
        int wins = 0;
        float efficiencySum = 0f;

        lst = lst ?? GameDatabase.GetLastNRecordByDate(_NoOfLastGames);
        if (lst == null || lst.Count < _NoOfLastGames) return;

        Debug.Log($"Last {_NoOfLastGames} points");
        for(int i = 0; i < lst.Count; i++)
        {
            SudokuDifficulty matchDifficulty = (SudokuDifficulty)lst[i].Difficulty;
            int maxScore = ScoringSystem.GetAbsoluteMaximumScore(matchDifficulty);

            Debug.Log($" {((float)lst[0].Points / maxScore)}");
            if(i < (_NoOfLastGames - 1) && lst[i].Difficulty != lst[i + 1].Difficulty) AllDifficultySame = false;
            if(lst[i].IsWon) ++wins;
            
            efficiencySum += (float)lst[i].Points / maxScore;
        }

        if (AllDifficultySame) 
        {
            float finalEfficiency = efficiencySum / _NoOfLastGamesFloat;

            if (wins <= 2 || finalEfficiency < 0.45f)
            {
                var prev = _currentDifficulty;
                _currentDifficulty = (SudokuDifficulty)Math.Max((int)SudokuDifficulty.Simple, (int)_currentDifficulty - 1);
                Debug.Log($"Moving to below tier. Current Difficulty: {prev} demoting to {_currentDifficulty}");
            }
        }
        else
        {
            _currentDifficulty = (SudokuDifficulty)lst[0].Difficulty;
        }
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