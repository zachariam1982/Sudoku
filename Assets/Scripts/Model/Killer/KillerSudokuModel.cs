using System;
using System.Collections.Generic;

/// <summary>Gameplay model for standard Sudoku rules plus Killer cages.</summary>
public sealed class KillerSudokuModel : ISudokuModel
{
    private const int LastGamesForProgression = 5;
    private readonly ISudokuGenerator _generator;
    private readonly IGameHistoryRepository _historyRepository;
    private int _currentLevel = 1;
    private int _puzzleSeed = 1;
    private SudokuDifficulty _currentDifficulty = SudokuDifficulty.Easy;
    private SudokuResult _result;

    public KillerSudokuModel() : this(new KillerSudokuGenerator(), new SqliteKillerGameHistoryRepository()) { }
    public KillerSudokuModel(ISudokuGenerator generator)
        : this(generator, new SqliteKillerGameHistoryRepository()) { }
    public KillerSudokuModel(IGameHistoryRepository historyRepository)
        : this(new KillerSudokuGenerator(), historyRepository) { }

    public KillerSudokuModel(ISudokuGenerator generator, IGameHistoryRepository historyRepository)
    {
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));
        _historyRepository = historyRepository ?? throw new ArgumentNullException(nameof(historyRepository));
    }

    public int[,] Board { get; private set; } = new int[9, 9];
    public bool[,] GivenMask { get; private set; } = new bool[9, 9];
    public int[,] PencilCandidateMasks { get; private set; } = new int[9, 9];
    public KillerCage[] Cages { get; private set; } = Array.Empty<KillerCage>();
    public SudokuDifficulty CurrentDifficulty => _currentDifficulty;
    public int CurrentLevel => _currentLevel;
    public int PuzzleSeed => _puzzleSeed;

    public void SetLevel(int level) { _currentLevel = level; _puzzleSeed = level; }
    public void SetPuzzleSeed(int puzzleSeed) => _puzzleSeed = puzzleSeed;
    public void SetDifficulty(SudokuDifficulty difficulty) => _currentDifficulty = difficulty;
    public void increaseDifficulty() => UpdateDifficultyFromHistory();
    public void decreaseDifficulty(List<GameRecord> records = null) => UpdateDifficultyFromHistory(records);

    public void UpdateDifficultyFromHistory(List<GameRecord> records = null)
    {
        records = records ?? _historyRepository.GetLastNRecordByDate(LastGamesForProgression);
        _currentDifficulty = ClassicSudokuModel.CalculateNextDifficulty(records, _currentDifficulty);
    }

    public void LoadCurrentLevelPuzzle(int maxAttempts = 100000)
    {
        // Generation is synchronous on the Unity thread; keep the model bounded
        // even when callers use the large interface default.
        _result = _generator.Generate(_puzzleSeed, _currentDifficulty, Math.Min(maxAttempts, 100));
        _currentDifficulty = _result.Difficulty;
        Cages = CloneCages(_result.KillerCages);
        CopyPuzzleToBoard(_result.Puzzle);
    }

    public int[] GetOriginalPuzzleFlat() => Flatten(_result?.Puzzle);
    public int[] GetSolutionFlat() => Flatten(_result?.Solution);

    public bool TryLoadSavedPuzzle(int[] puzzleFlat, int[] solutionFlat) =>
        TryLoadSavedPuzzle(puzzleFlat, solutionFlat, Cages);

    public bool TryLoadSavedPuzzle(int[] puzzleFlat, int[] solutionFlat, KillerCage[] cages)
    {
        if (!IsValidSavedPuzzle(puzzleFlat, solutionFlat, cages)) return false;
        int[,] puzzle = Expand(puzzleFlat);
        _result = new SudokuResult
        {
            Puzzle = puzzle,
            Solution = Expand(solutionFlat),
            Difficulty = _currentDifficulty,
            KillerCages = CloneCages(cages)
        };
        Cages = CloneCages(cages);
        CopyPuzzleToBoard(puzzle);
        return true;
    }

    /// <summary>Validates saved grids when cage data is unavailable in the save payload.</summary>
    public static bool IsValidSavedPuzzle(int[] puzzleFlat, int[] solutionFlat)
    {
        return ClassicSudokuModel.IsValidSavedPuzzle(puzzleFlat, solutionFlat);
    }

    public static bool IsValidSavedPuzzle(int[] puzzleFlat, int[] solutionFlat, KillerCage[] cages)
    {
        if (puzzleFlat == null || solutionFlat == null || puzzleFlat.Length != 81 || solutionFlat.Length != 81)
            return false;
        int[,] puzzle = Expand(puzzleFlat);
        int[,] solution = Expand(solutionFlat);
        if (!KillerSudokuRules.ValidateBoard(puzzle, cages) ||
            !KillerSudokuRules.ValidateCompleteSolution(solution, cages)) return false;
        for (int index = 0; index < 81; index++)
            if (puzzleFlat[index] != 0 && puzzleFlat[index] != solutionFlat[index]) return false;
        return true;
    }

    public bool SetValue(int row, int col, int value)
    {
        if (!IsInBounds(row, col) || value < 0 || value > 9 || GivenMask[row, col]) return false;
        Board[row, col] = value;
        if (value != 0) PencilCandidateMasks[row, col] = 0;
        return true;
    }

    public void TogglePencilCandidate(int row, int col, int number)
    {
        if (!IsInBounds(row, col) || number < 1 || number > 9 || Board[row, col] != 0) return;
        PencilCandidateMasks[row, col] ^= 1 << number;
    }

    public void AutoFillPencilCandidates()
    {
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
                PencilCandidateMasks[row, col] = Board[row, col] == 0
                    ? KillerSudokuRules.GetCandidateMask(Board, Cages, row, col)
                    : 0;
    }

    public void RemovePencilCandidateFromPeers(int enteredRow, int enteredCol, int number)
    {
        if (!IsInBounds(enteredRow, enteredCol) || number < 1 || number > 9) return;
        int bit = ~(1 << number);
        for (int index = 0; index < 9; index++)
        {
            if (index != enteredCol) PencilCandidateMasks[enteredRow, index] &= bit;
            if (index != enteredRow) PencilCandidateMasks[index, enteredCol] &= bit;
            int boxRow = enteredRow / 3 * 3 + index / 3;
            int boxCol = enteredCol / 3 * 3 + index % 3;
            if (boxRow != enteredRow || boxCol != enteredCol)
                PencilCandidateMasks[boxRow, boxCol] &= bit;
        }
        KillerCage cage = KillerSudokuRules.FindCage(Cages, enteredRow * 9 + enteredCol);
        if (cage != null)
            foreach (int cell in cage.Cells)
                if (cell != enteredRow * 9 + enteredCol)
                    PencilCandidateMasks[cell / 9, cell % 9] &= bit;
    }

    public void LoadPencilCandidateMasks(int[] masks)
    {
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
                PencilCandidateMasks[row, col] = masks != null && masks.Length == 81 && Board[row, col] == 0
                    ? masks[row * 9 + col] & KillerSudokuRules.FullMask
                    : 0;
    }

    public void SetPencilCandidateMask(int row, int col, int mask)
    {
        if (!IsInBounds(row, col)) return;
        PencilCandidateMasks[row, col] = Board[row, col] == 0 ? mask & KillerSudokuRules.FullMask : 0;
    }

    public bool IsGiven(int row, int col) => IsInBounds(row, col) && GivenMask[row, col];
    public int GetValue(int row, int col) => IsInBounds(row, col) ? Board[row, col] : 0;
    public int GetSolutionValue(int row, int col) =>
        !IsInBounds(row, col) || _result?.Solution == null ? 0 : _result.Solution[row, col];
    public bool IsCellEmpty(int row, int col) => IsInBounds(row, col) && Board[row, col] == 0;

    public bool HasConflict(int row, int col)
    {
        if (!IsInBounds(row, col) || Board[row, col] == 0) return false;
        return !KillerSudokuRules.IsValidPlacement(Board, Cages, row, col, Board[row, col]);
    }

    public bool Validate() => KillerSudokuRules.ValidateBoard(Board, Cages);

    public bool IsComplete()
    {
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
                if (Board[row, col] == 0) return false;
        return KillerSudokuRules.ValidateCompleteSolution(Board, Cages);
    }

    private void CopyPuzzleToBoard(int[,] puzzle)
    {
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
            {
                Board[row, col] = puzzle[row, col];
                GivenMask[row, col] = puzzle[row, col] != 0;
                PencilCandidateMasks[row, col] = 0;
            }
    }

    private static int[] Flatten(int[,] board)
    {
        var flat = new int[81];
        if (board == null) return flat;
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++) flat[row * 9 + col] = board[row, col];
        return flat;
    }

    private static int[,] Expand(int[] flat)
    {
        var board = new int[9, 9];
        for (int index = 0; index < 81; index++) board[index / 9, index % 9] = flat[index];
        return board;
    }

    private static KillerCage[] CloneCages(KillerCage[] cages)
    {
        if (cages == null) return Array.Empty<KillerCage>();
        var copy = new KillerCage[cages.Length];
        for (int i = 0; i < cages.Length; i++)
            copy[i] = cages[i] == null ? null : new KillerCage(cages[i].TargetSum, cages[i].Cells);
        return copy;
    }

    private static bool IsInBounds(int row, int col) => row >= 0 && row < 9 && col >= 0 && col < 9;
}
