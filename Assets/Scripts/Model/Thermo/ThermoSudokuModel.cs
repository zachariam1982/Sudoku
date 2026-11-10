using System;
using System.Collections.Generic;

/// <summary>Gameplay model for standard Sudoku rules plus Thermo thermometers.</summary>
public sealed class ThermoSudokuModel : ISudokuModel
{
    private const int LastGamesForProgression = 5;
    private readonly ISudokuGenerator _generator;
    private readonly IGameHistoryRepository _historyRepository;
    private int _currentLevel = 1;
    private int _puzzleSeed = 1;
    private SudokuDifficulty _currentDifficulty = SudokuDifficulty.Easy;
    private SudokuResult _result;

    public ThermoSudokuModel() : this(new ThermoSudokuGenerator(), new SqliteThermoGameHistoryRepository()) { }
    public ThermoSudokuModel(ISudokuGenerator generator)
        : this(generator, new SqliteThermoGameHistoryRepository()) { }
    public ThermoSudokuModel(IGameHistoryRepository historyRepository)
        : this(new ThermoSudokuGenerator(), historyRepository) { }

    public ThermoSudokuModel(ISudokuGenerator generator, IGameHistoryRepository historyRepository)
    {
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));
        _historyRepository = historyRepository ?? throw new ArgumentNullException(nameof(historyRepository));
    }

    public int[,] Board { get; private set; } = new int[9, 9];
    public bool[,] GivenMask { get; private set; } = new bool[9, 9];
    public int[,] PencilCandidateMasks { get; private set; } = new int[9, 9];
    public Thermometer[] Thermometers { get; private set; } = Array.Empty<Thermometer>();
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
        Thermometers = CloneThermometers(_result.Thermometers);
        CopyPuzzleToBoard(_result.Puzzle);
    }

    public int[] GetOriginalPuzzleFlat() => Flatten(_result?.Puzzle);
    public int[] GetSolutionFlat() => Flatten(_result?.Solution);

    public bool TryLoadSavedPuzzle(int[] puzzleFlat, int[] solutionFlat) =>
        TryLoadSavedPuzzle(puzzleFlat, solutionFlat, Thermometers);

    public bool TryLoadSavedPuzzle(int[] puzzleFlat, int[] solutionFlat, Thermometer[] thermometers)
    {
        if (!IsValidSavedPuzzle(puzzleFlat, solutionFlat, thermometers)) return false;
        int[,] puzzle = Expand(puzzleFlat);
        _result = new SudokuResult
        {
            Puzzle = puzzle,
            Solution = Expand(solutionFlat),
            Difficulty = _currentDifficulty,
            Thermometers = CloneThermometers(thermometers)
        };
        Thermometers = CloneThermometers(thermometers);
        CopyPuzzleToBoard(puzzle);
        return true;
    }

    /// <summary>Validates saved grids when thermometer data is unavailable in the save payload.</summary>
    public static bool IsValidSavedPuzzle(int[] puzzleFlat, int[] solutionFlat)
    {
        return ClassicSudokuModel.IsValidSavedPuzzle(puzzleFlat, solutionFlat);
    }

    public static bool IsValidSavedPuzzle(int[] puzzleFlat, int[] solutionFlat, Thermometer[] thermometers)
    {
        if (puzzleFlat == null || solutionFlat == null || puzzleFlat.Length != 81 || solutionFlat.Length != 81)
            return false;
        if (!ThermoSudokuRules.ValidateThermometerLayout(thermometers)) return false;
        int[,] puzzle = Expand(puzzleFlat);
        int[,] solution = Expand(solutionFlat);
        if (!ThermoSudokuRules.ValidateBoard(puzzle, thermometers) ||
            !ThermoSudokuRules.ValidateCompleteSolution(solution, thermometers)) return false;
        for (int index = 0; index < 81; index++)
            if (puzzleFlat[index] != 0 && puzzleFlat[index] != solutionFlat[index]) return false;
        return true;
    }

    public static List<SaveGameThermometer> SerializeThermometers(Thermometer[] thermometers)
    {
        var saved = new List<SaveGameThermometer>();
        if (thermometers == null) return saved;
        foreach (Thermometer thermometer in thermometers)
            if (thermometer != null)
                saved.Add(new SaveGameThermometer
                {
                    Cells = thermometer.Cells == null ? Array.Empty<int>() : (int[])thermometer.Cells.Clone()
                });
        return saved;
    }

    public static Thermometer[] DeserializeThermometers(List<SaveGameThermometer> saved)
    {
        if (saved == null || saved.Count == 0) return null;
        var thermometers = new Thermometer[saved.Count];
        for (int i = 0; i < saved.Count; i++)
        {
            SaveGameThermometer thermometer = saved[i];
            if (thermometer == null) return null;
            thermometers[i] = new Thermometer(thermometer.Cells);
        }
        return thermometers;
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
                    ? ThermoSudokuRules.GetCandidateMask(Board, Thermometers, row, col)
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
        // Inequality constraints also eliminate other digits along the path.
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                if (Board[r, c] == 0)
                    PencilCandidateMasks[r, c] &= ThermoSudokuRules.GetCandidateMask(Board, Thermometers, r, c);
    }

    public void LoadPencilCandidateMasks(int[] masks)
    {
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
                PencilCandidateMasks[row, col] = masks != null && masks.Length == 81 && Board[row, col] == 0
                    ? masks[row * 9 + col] & ThermoSudokuRules.FullMask
                    : 0;
    }

    public void SetPencilCandidateMask(int row, int col, int mask)
    {
        if (!IsInBounds(row, col)) return;
        PencilCandidateMasks[row, col] = Board[row, col] == 0 ? mask & ThermoSudokuRules.FullMask : 0;
    }

    public bool IsGiven(int row, int col) => IsInBounds(row, col) && GivenMask[row, col];
    public int GetValue(int row, int col) => IsInBounds(row, col) ? Board[row, col] : 0;
    public int GetSolutionValue(int row, int col) =>
        !IsInBounds(row, col) || _result?.Solution == null ? 0 : _result.Solution[row, col];
    public bool IsCellEmpty(int row, int col) => IsInBounds(row, col) && Board[row, col] == 0;

    public bool HasConflict(int row, int col)
    {
        if (!IsInBounds(row, col) || Board[row, col] == 0) return false;
        return !ThermoSudokuRules.IsValidPlacement(Board, Thermometers, row, col, Board[row, col]);
    }

    public bool Validate() => ThermoSudokuRules.ValidateBoard(Board, Thermometers);

    public bool IsComplete()
    {
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
                if (Board[row, col] == 0) return false;
        return ThermoSudokuRules.ValidateCompleteSolution(Board, Thermometers);
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

    private static Thermometer[] CloneThermometers(Thermometer[] thermometers)
    {
        if (thermometers == null) return Array.Empty<Thermometer>();
        var copy = new Thermometer[thermometers.Length];
        for (int i = 0; i < thermometers.Length; i++)
            copy[i] = thermometers[i] == null ? null : new Thermometer(thermometers[i].Cells);
        return copy;
    }

    private static bool IsInBounds(int row, int col) => row >= 0 && row < 9 && col >= 0 && col < 9;
}
