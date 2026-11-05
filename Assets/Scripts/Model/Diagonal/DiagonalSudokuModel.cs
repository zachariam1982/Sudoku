using System;
using System.Collections.Generic;

/// <summary>Gameplay model for Classic rules plus both main-diagonal constraints.</summary>
public sealed class DiagonalSudokuModel : ISudokuModel
{
    private const int LastGamesForProgression = 5;
    private readonly ISudokuGenerator _generator;
    private readonly IGameHistoryRepository _historyRepository;
    private int _currentLevel = 1;
    private int _puzzleSeed = 1;
    private SudokuDifficulty _currentDifficulty = SudokuDifficulty.Easy;
    private SudokuResult _result;

    public DiagonalSudokuModel() : this(new DiagonalSudokuGenerator(), new SqliteGameHistoryRepository()) { }

    public DiagonalSudokuModel(ISudokuGenerator generator)
        : this(generator, new SqliteGameHistoryRepository()) { }

    public DiagonalSudokuModel(IGameHistoryRepository historyRepository)
        : this(new DiagonalSudokuGenerator(), historyRepository) { }

    public DiagonalSudokuModel(ISudokuGenerator generator, IGameHistoryRepository historyRepository)
    {
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));
        _historyRepository = historyRepository ?? throw new ArgumentNullException(nameof(historyRepository));
    }

    public int[,] Board { get; private set; } = new int[9, 9];
    public bool[,] GivenMask { get; private set; } = new bool[9, 9];
    public int[,] PencilCandidateMasks { get; private set; } = new int[9, 9];
    public SudokuDifficulty CurrentDifficulty => _currentDifficulty;
    public int CurrentLevel => _currentLevel;
    public int PuzzleSeed => _puzzleSeed;

    public void SetLevel(int level)
    {
        _currentLevel = level;
        _puzzleSeed = level;
    }

    public void SetPuzzleSeed(int puzzleSeed) => _puzzleSeed = puzzleSeed;
    public void SetDifficulty(SudokuDifficulty difficulty) => _currentDifficulty = difficulty;

    public void increaseDifficulty() => UpdateDifficultyFromHistory();
    public void decreaseDifficulty(List<GameRecord> records = null) => UpdateDifficultyFromHistory(records);

    public void UpdateDifficultyFromHistory(List<GameRecord> records = null)
    {
        if (records == null)
            records = _historyRepository.GetLastNRecordByDate(LastGamesForProgression);
        _currentDifficulty = ClassicSudokuModel.CalculateNextDifficulty(records, _currentDifficulty);
    }

    public void LoadCurrentLevelPuzzle(int maxAttempts = 100000)
    {
        _result = _generator.Generate(_puzzleSeed, _currentDifficulty, maxAttempts);
        _currentDifficulty = _result.Difficulty;
        CopyPuzzleToBoard(_result.Puzzle);
    }

    public int[] GetOriginalPuzzleFlat() => Flatten(_result?.Puzzle);
    public int[] GetSolutionFlat() => Flatten(_result?.Solution);

    public bool TryLoadSavedPuzzle(int[] puzzleFlat, int[] solutionFlat)
    {
        if (!IsValidSavedPuzzle(puzzleFlat, solutionFlat)) return false;
        int[,] puzzle = Expand(puzzleFlat);
        _result = new SudokuResult
        {
            Puzzle = puzzle,
            Solution = Expand(solutionFlat),
            Difficulty = _currentDifficulty
        };
        CopyPuzzleToBoard(puzzle);
        return true;
    }

    public static bool IsValidSavedPuzzle(int[] puzzleFlat, int[] solutionFlat)
    {
        if (puzzleFlat == null || solutionFlat == null ||
            puzzleFlat.Length != 81 || solutionFlat.Length != 81)
            return false;

        int[,] puzzle = new int[9, 9];
        int[,] solution = new int[9, 9];
        for (int index = 0; index < 81; index++)
        {
            int given = puzzleFlat[index];
            int answer = solutionFlat[index];
            if (given < 0 || given > 9 || answer < 1 || answer > 9 ||
                (given != 0 && given != answer))
                return false;
            puzzle[index / 9, index % 9] = given;
            solution[index / 9, index % 9] = answer;
        }

        return DiagonalSudokuRules.ValidateBoard(puzzle) &&
               DiagonalSudokuRules.ValidateCompleteSolution(solution);
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
                    ? DiagonalSudokuRules.GetCandidateMask(Board, row, col)
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
            if (enteredRow == enteredCol && index != enteredRow)
                PencilCandidateMasks[index, index] &= bit;
            if (enteredRow + enteredCol == 8 && index != enteredRow)
                PencilCandidateMasks[index, 8 - index] &= bit;
        }
    }

    public void LoadPencilCandidateMasks(int[] masks)
    {
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
                PencilCandidateMasks[row, col] = masks != null && masks.Length == 81 && Board[row, col] == 0
                    ? masks[row * 9 + col] & DiagonalSudokuRules.FullMask
                    : 0;
    }

    public void SetPencilCandidateMask(int row, int col, int mask)
    {
        if (!IsInBounds(row, col)) return;
        PencilCandidateMasks[row, col] = Board[row, col] == 0
            ? mask & DiagonalSudokuRules.FullMask
            : 0;
    }

    public bool IsGiven(int row, int col) => IsInBounds(row, col) && GivenMask[row, col];
    public int GetValue(int row, int col) => IsInBounds(row, col) ? Board[row, col] : 0;
    public int GetSolutionValue(int row, int col)
    {
        if (!IsInBounds(row, col) || _result?.Solution == null) return 0;
        return _result.Solution[row, col];
    }
    public bool IsCellEmpty(int row, int col) => IsInBounds(row, col) && Board[row, col] == 0;

    public bool HasConflict(int row, int col)
    {
        if (!IsInBounds(row, col) || Board[row, col] == 0) return false;
        return !DiagonalSudokuRules.IsValidPlacement(Board, row, col, Board[row, col]);
    }

    public bool Validate() => DiagonalSudokuRules.ValidateBoard(Board);

    public bool IsComplete()
    {
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
                if (Board[row, col] == 0) return false;
        return true;
    }

    private void CopyPuzzleToBoard(int[,] puzzle)
    {
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
            {
                int value = puzzle[row, col];
                Board[row, col] = value;
                GivenMask[row, col] = value != 0;
                PencilCandidateMasks[row, col] = 0;
            }
    }

    private static int[] Flatten(int[,] board)
    {
        var flat = new int[81];
        if (board == null) return flat;
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
                flat[row * 9 + col] = board[row, col];
        return flat;
    }

    private static int[,] Expand(int[] flat)
    {
        var board = new int[9, 9];
        for (int index = 0; index < 81; index++) board[index / 9, index % 9] = flat[index];
        return board;
    }

    private static bool IsInBounds(int row, int col) => row >= 0 && row < 9 && col >= 0 && col < 9;
}
