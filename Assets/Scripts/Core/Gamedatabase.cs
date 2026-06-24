using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            string query = "select * from completed_games order by Id";

            return _db.Query<GameRecord>(query);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameDatabase] GetAll failed: {ex.Message}");
            return new List<GameRecord>();
        }
    }

    public static List<GameRecord> GetNextSet(int offset)
    {
        try
        {
            string query = "SELECT * FROM completed_games ORDER BY Id DESC LIMIT 10 OFFSET ?";

            return _db.Query<GameRecord>(query, offset);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameDatabase] GetNextSet failed: {ex.Message}");
            return new List<GameRecord>();
        }
    }

    public static GameRecord GetFastestWin()
    {
        try
        {
            string query = "SELECT * from completed_games where IsWon = true order by ElapsedSeconds limit 1";

            return _db.Query<GameRecord>(query)[0];
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameDatabase] GetFastestWin failed: {ex.Message}");
            return null;
        }
    }
    public static int GetTotalPoints()
    {
        try
        {
            string query = "SELECT SUM(Points) from completed_games where IsWon = true";

            return _db.ExecuteScalar<int>(query);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameDatabase] GetFastestWin failed: {ex.Message}");
            return 0;
        }  
    }

    public static int GetTotalWins()
    {
        try
        {
            string query = "select count(*) from completed_games where IsWon = true";

            return _db.ExecuteScalar<int>(query);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameDatabase] GetFastestWin failed: {ex.Message}");
            return 0;
        }  
    }
    /// <summary>Returns the total number of games played.</summary>
    public static int GetTotalGamesPlayed()
    {
        try   
        {
            string query = "SELECT count(*) from completed_games";

            return _db.ExecuteScalar<int>(query); 
        }
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
            string query = "SELECT * from completed_games where IsWon = true order by Id limit ?";

            return _db.Query<GameRecord>(query, number);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameDatabase] GetLastNRecordByDate failed: {ex.Message}");
            return new List<GameRecord>();
        }
    }
    public static GameRecord GetLastRecord()
    {
        try
        {
            string query = "SELECT * from completed_games order by Id limit 1";

            return _db.Query<GameRecord>(query)[0];
        }
        catch(Exception ex)
        {
            Debug.LogError($"[GameDatabase] GetLastRecord failed: {ex.Message}");
            return null;
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