using System.Collections.Generic;

/// <summary>Mutable puzzle state and operations used by gameplay view models.</summary>
public interface ISudokuModel
{
    int[,] Board { get; }
    bool[,] GivenMask { get; }
    int[,] PencilCandidateMasks { get; }
    SudokuDifficulty CurrentDifficulty { get; }
    int CurrentLevel { get; }
    int PuzzleSeed { get; }

    void SetLevel(int level);
    void SetPuzzleSeed(int puzzleSeed);
    void SetDifficulty(SudokuDifficulty difficulty);
    void increaseDifficulty();
    void decreaseDifficulty(List<GameRecord> records = null);
    void UpdateDifficultyFromHistory(List<GameRecord> records = null);
    void LoadCurrentLevelPuzzle(int maxAttempts = 100000);
    int[] GetOriginalPuzzleFlat();
    int[] GetSolutionFlat();
    bool TryLoadSavedPuzzle(int[] puzzleFlat, int[] solutionFlat);
    bool SetValue(int row, int col, int value);
    void TogglePencilCandidate(int row, int col, int number);
    void AutoFillPencilCandidates();
    void RemovePencilCandidateFromPeers(int enteredRow, int enteredCol, int number);
    void LoadPencilCandidateMasks(int[] masks);
    void SetPencilCandidateMask(int row, int col, int mask);
    bool IsGiven(int row, int col);
    int GetValue(int row, int col);
    int GetSolutionValue(int row, int col);
    bool IsCellEmpty(int row, int col);
    bool HasConflict(int row, int col);
    bool Validate();
    bool IsComplete();
}

public interface ISudokuGenerator
{
    SudokuResult Generate(int level, SudokuDifficulty requestedDifficulty, int maxAttempts = 100);
}

public interface ISudokuSolver
{
    bool HasUniqueSolution(int[,] puzzle);
    int CountSolutions(int[,] board, int limit = 2);
    List<int> GetCandidates(int[,] board, int row, int col);
    bool IsValid(int[,] board, int row, int col, int value);
}

public interface ISudokuDifficultyAnalyzer
{
    SudokuDifficultyResult Analyze(int[,] puzzle);
}

public interface IScoringSystem
{
    int GetAbsoluteMaximumScore(SudokuDifficulty difficulty);
    int Calculate(SudokuDifficulty difficulty, float elapsedSeconds, IScorePenalties penalties);
    (int total, int diffScore, int timeScore, int penalty) CalculateDetailed(
        SudokuDifficulty difficulty, float elapsedSeconds, IScorePenalties penalties);
}
