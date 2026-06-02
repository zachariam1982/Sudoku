/// <summary>
/// Pure data model for a Sudoku puzzle.
/// No Unity dependencies.
/// </summary>
using System;
using System.Collections.Generic;
using System.Linq;

public enum SudokuDifficulty
{   
    Infant,
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
public static class SudokuGenerator
{
    public static SudokuResult GenerateSudoku(int level, SudokuDifficulty difficulty)
    {
        // 1. Initialize Seeded Random
        Random rng = new Random(level);

        int[,] solution = new int[9, 9];
        FillBoard(solution, rng);

        // Create a deep copy for the puzzle
        int[,] puzzle = (int[,])solution.Clone();

        // 2. Determine how many clues to keep
        int targetClues = difficulty switch
        {
            SudokuDifficulty.Infant    => rng.Next(66, 72), // Too easy
            SudokuDifficulty.Beginner  => rng.Next(50, 56), // Very easy, almost full board
            SudokuDifficulty.Easy      => rng.Next(40, 46), // Standard easy
            SudokuDifficulty.Novice    => rng.Next(36, 40), // Bridge between Easy and Moderate
            SudokuDifficulty.Moderate  => rng.Next(32, 36), // Standard medium
            SudokuDifficulty.Advanced  => rng.Next(28, 32), // Bridge between Moderate and Hard
            SudokuDifficulty.Hard      => rng.Next(24, 28), // Standard hard
            SudokuDifficulty.Expert    => rng.Next(20, 24), // Demands advanced logic techniques
            SudokuDifficulty.Hardest   => rng.Next(17, 20), // Absolute minimum for unique puzzles
            _ => 30
        };

        // 3. Dig holes in the puzzle
        int holesToRemove = 81 - targetClues;
        while (holesToRemove > 0)
        {
            int row = rng.Next(0, 9);
            int col = rng.Next(0, 9);

            if (puzzle[row, col] != 0)
            {
                puzzle[row, col] = 0;
                holesToRemove--;
            }
        }

        return new SudokuResult
        {
            Puzzle = puzzle,
            Solution = solution
        };
    }

    private static bool FillBoard(int[,] board, Random rng)
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

    public SudokuDifficulty CurrentDifficulty { get { return _currentDifficulty; }}
    public int CurrentLevel { get {return _currentLevel;}}
    public void AddLevel(int increment)
    {
        _currentLevel = _currentLevel + increment;
    }
    public void increaseDifficulty()
    {
        _currentDifficulty = (SudokuDifficulty)Math.Min((int)SudokuDifficulty.Hardest, (int)_currentDifficulty + 1);
    }
    public void decreaseDifficulty()
    {
        _currentDifficulty = (SudokuDifficulty)Math.Max((int)SudokuDifficulty.Infant, (int)_currentDifficulty - 1);
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