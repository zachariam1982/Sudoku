using System;
using System.Collections.Generic;

/// <summary>
/// Plain serializable snapshot of everything needed to restore a game session.
/// No Unity or model dependencies — safe to JSON-serialize.
/// </summary>
[Serializable]
public class SaveGameCandidateChange
{
    public int Row;
    public int Col;
    public int BeforeMask;
    public int AfterMask;
}

[Serializable]
public class SaveGameHistoryEntry
{
    // Row and Col are -1 for candidate-only actions such as Auto Candidates.
    public int Row = -1;
    public int Col = -1;
    public int BeforeValue;
    public int AfterValue;
    public List<SaveGameCandidateChange> CandidateChanges = new List<SaveGameCandidateChange>();
}

[Serializable]
public class SaveGameKillerCage
{
    public int TargetSum;
    public int[] Cells = Array.Empty<int>();
}

[Serializable]
public class SaveGameThermometer
{
    public int[] Cells = Array.Empty<int>();
}

[Serializable]
public class SaveGameData
{
    // Zero identifies legacy fixed-layout saves; maps are regenerated from PuzzleSeed.
    public int IrregularLayoutVersion;
    // Version 2 stores the original puzzle and solution so saves remain valid
    // even if puzzle generation changes in a later app release.
    public int SaveVersion;
    public string SaveMode = "Journey";
    public string SaveVariant = "";
    public int GeneratorVersion = 1;

    // ── Puzzle identity ───────────────────────────────────────────────────────
    public int    Level      = 1;
    public int    Difficulty = 2; // maps to SudokuDifficulty enum ordinal
    public int    PuzzleSeed = 0; // 0 means a legacy save; use Level as its seed
    public int NewGameReplayResultId = 0; // 0 means a fresh New Game, not a retake
    public int DailyChallengeRecordId = 0;
    public string DailyChallengeDate = "";
    public int[] OriginalPuzzleFlat = new int[81];
    public int[] SolutionFlat = new int[81];
    public List<SaveGameThermometer> Thermometers = new List<SaveGameThermometer>();
    public List<SaveGameKillerCage> KillerCages = new List<SaveGameKillerCage>();

    // ── Board state ───────────────────────────────────────────────────────────
    /// <summary>Flat row-major array of 81 cell values (0 = empty).</summary>
    public int[]  BoardFlat  = new int[81];
    /// <summary>Bit mask of pencil candidates for each cell.</summary>
    public int[] PencilCandidateMasksFlat = new int[81];
    public bool IsPencilMode = false;
    public int HighlightedCandidateNumber = 0;

    // ── Session stats ─────────────────────────────────────────────────────────
    public float  ElapsedSeconds  = 0f;
    public int    LivesRemaining  = 3;

    // ── Undo stack ────────────────────────────────────────────────────────────
    /// <summary>
    /// Each entry encodes one undo frame as "row,col,value".
    /// Bottom of stack = index 0, top = last element.
    /// </summary>
    // Legacy value-only undo records are read for saves created by older versions.
    public List<string> UndoStack = new List<string>();
    public List<SaveGameHistoryEntry> UndoHistory = new List<SaveGameHistoryEntry>();
    public List<SaveGameHistoryEntry> RedoHistory = new List<SaveGameHistoryEntry>();
    public bool IsWon                    = false;
    public bool IsLost                   = false;
    public bool PauseRequested           = false;
    public bool RetryOlderGame           = false;
    public int RetryOlderGame_Id         = -1;
    public int RetryOlderGame_Level      = -1;
    public int RetryOlderGame_Difficulty = -1;
    public int RetryOlderGame_Points     = -1;
    public string statename              = "";
    // ── Penalties Stat ────────────────────────────────────────────────────────────
    public int Mistakes      = 0;
    public int SOSEmptyCells = 0; // empty cells SOS filled
    public int SOSWrongCells = 0; // wrong cells SOS fixed
    public int UndoUses     = 0;
    public int PencilUses   = 0;
    public int EraseUses    = 0;
    public int SOSUses      = 0;
    public int AutoFillUses = 0;
}
