using System;
using System.Collections.Generic;
using System.IO;
using SQLite;
using UnityEngine;

/// <summary>
/// Manages a SQLite database of completed game sessions using sqlite-net-pcl.
/// Database file lives at Application.persistentDataPath/game_history.db
///
/// Setup:
///   1. Download SQLite.cs + SQLiteAsync.cs from https://github.com/praeclarum/sqlite-net
///      and drop both files anywhere inside your Assets/ folder.
///   2. Call GameDatabase.Init() once at app startup (e.g. User.Awake).
///
/// Usage:
///   GameDatabase.Insert(record);
///   List<GameRecord> all      = GameDatabase.GetAll();
///   List<GameRecord> bestWins = GameDatabase.GetWinsByDifficulty("Hard");
///   GameRecord       best     = GameDatabase.GetBestWin("Hard");
/// </summary>
public static class GameDatabase
{
    private static SQLiteConnection _db;

    private static string DbPath =>
        Path.Combine(Application.persistentDataPath, "game_history.db");

    // ── Schema ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Opens the database and creates the table if it doesn't exist.
    /// Safe to call every time the app starts — CreateTable is a no-op if
    /// the table already exists.
    /// </summary>
    public static void Init()
    {
        try
        {
            Debug.Log($"DB stored at {DbPath}");
            _db = new SQLiteConnection(DbPath);
            _db.CreateTable<GameRecord>();
            Debug.Log($"[GameDatabase] Initialised at {DbPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameDatabase] Init failed: {ex.Message}");
        }
    }

    // ── Write ─────────────────────────────────────────────────────────────────

    /// <summary>Inserts a completed game record into the database.</summary>
    public static void Insert(GameRecord record)
    {
        try
        {
            _db.Insert(record);
            Debug.Log($"[GameDatabase] Record saved — Level={record.Level} " +
                      $"Difficulty={record.Difficulty} Won={record.IsWon} " +
                      $"Time={record.ElapsedSeconds:F0}s");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameDatabase] Insert failed: {ex.Message}");
        }
    }

    // ── Read ──────────────────────────────────────────────────────────────────

    /// <summary>Returns all completed game records, newest first.</summary>
    public static List<GameRecord> GetAll()
    {
        try
        {
            return _db.Table<GameRecord>()
                      .OrderByDescending(r => r.Id)
                      .ToList();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameDatabase] GetAll failed: {ex.Message}");
            return new List<GameRecord>();
        }
    }

    /// <summary>
    /// Returns won games for a specific difficulty, fastest time first.
    /// Useful for a per-difficulty leaderboard or personal best list.
    /// </summary>
    public static List<GameRecord> GetWinsByDifficulty(int difficulty)
    {
        try
        {
            return _db.Table<GameRecord>()
                      .Where(r => r.IsWon && r.Difficulty == difficulty)
                      .OrderBy(r => r.ElapsedSeconds)
                      .ToList();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameDatabase] GetWinsByDifficulty failed: {ex.Message}");
            return new List<GameRecord>();
        }
    }

    /// <summary>
    /// Returns the single fastest win for a given difficulty, or null if none.
    /// </summary>
    public static GameRecord GetBestWin(int difficulty)
    {
        try
        {
            return _db.Table<GameRecord>()
                      .Where(r => r.IsWon && r.Difficulty == difficulty)
                      .OrderBy(r => r.ElapsedSeconds)
                      .FirstOrDefault();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameDatabase] GetBestWin failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>Returns the total number of games played.</summary>
    public static int GetTotalGamesPlayed()
    {
        try   { return _db.Table<GameRecord>().Count(); }
        catch (Exception ex)
        {
            Debug.LogError($"[GameDatabase] GetTotalGamesPlayed failed: {ex.Message}");
            return 0;
        }
    }
    public static List<GameRecord> GetLastNRecordByDate(int number)
    {
        try
        {
            return _db.Table<GameRecord>()
                    .OrderByDescending(r => r.CompletedAt) // Sort by ISO-8601 string descending
                    .Take(number)                               // Limit to the top N  records
                    .ToList();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameDatabase] GetLastNRecordByDate failed: {ex.Message}");
            return new List<GameRecord>();
        }
    }
    // ── Teardown ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Closes the connection. Call from OnApplicationQuit if you want a clean
    /// shutdown, though sqlite-net-pcl handles this gracefully without it.
    /// </summary>
    public static void Close()
    {
        _db?.Close();
        _db = null;
    }
}