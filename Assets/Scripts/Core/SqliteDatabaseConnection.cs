using System;
using System.IO;
using SQLite;
using UnityEngine;

/// <summary>Owns the application's single SQLite connection and its lifecycle.</summary>
public sealed class SqliteDatabaseConnection
{
    private static readonly SqliteDatabaseConnection _instance = new SqliteDatabaseConnection();
    private SQLiteConnection _connection;

    public static SqliteDatabaseConnection Instance => _instance;
    public SQLiteConnection Connection => _connection;
    public bool WasCreated { get; private set; }

    private SqliteDatabaseConnection() { }

    public void Open()
    {
        if (_connection != null) return;

        string path = Path.Combine(Application.persistentDataPath, "game_history.db");
        try
        {
            WasCreated = !File.Exists(path);
            Debug.Log($"DB stored at {path}");
            _connection = new SQLiteConnection(path);
        }
        catch (Exception ex)
        {
            _connection = null;
            Debug.LogError($"[SqliteDatabaseConnection] Open failed: {ex.GetType().Name}: {ex}");
        }
    }

    public void FlushToDisk()
    {
        if (_connection == null) return;
        try { _connection.Execute("PRAGMA wal_checkpoint(TRUNCATE);"); }
        catch (Exception) { }
    }

    public void Close()
    {
        if (_connection == null) return;

        FlushToDisk();
        _connection.Close();
        _connection = null;
        WasCreated = false;
    }
}
