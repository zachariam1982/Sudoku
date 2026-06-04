using System;
using System.Collections.Generic;

/// <summary>
/// Plain serializable snapshot of everything needed to restore a game session.
/// No Unity or model dependencies — safe to JSON-serialize.
/// </summary>
[Serializable]
public class SaveGameData
{
    // ── Puzzle identity ───────────────────────────────────────────────────────
    public int    Level      = 1;
    public int    Difficulty = 2; // maps to SudokuDifficulty enum ordinal

    // ── Board state ───────────────────────────────────────────────────────────
    /// <summary>Flat row-major array of 81 cell values (0 = empty).</summary>
    public int[]  BoardFlat  = new int[81];

    // ── Session stats ─────────────────────────────────────────────────────────
    public float  ElapsedSeconds  = 0f;
    public int    LivesRemaining  = 3;

    // ── Undo stack ────────────────────────────────────────────────────────────
    /// <summary>
    /// Each entry encodes one undo frame as "row,col,value".
    /// Bottom of stack = index 0, top = last element.
    /// </summary>
    public List<string> UndoStack = new List<string>();
}