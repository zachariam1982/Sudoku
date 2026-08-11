using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SQLite;
using UnityEngine;

public static class GameDatabase
{
    private static SQLiteConnection _db;
    private static string DbPath => Path.Combine(Application.persistentDataPath, "game_history.db");
    public static void Init()
    {
        try
        {
            Debug.Log($"DB stored at {DbPath}");
            
            _db = new SQLiteConnection(DbPath);
            
            _db.CreateTable<GameRecord>();
            _db.CreateTable<GameStats>();

            InitializeGameStats();
            CreateGameStatsTriggers();
            
            Debug.Log($"[GameDatabase] Initialised at {DbPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameDatabase] Init failed: {ex.GetType().Name}: {ex}");
        }
    }
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
    public static void Update(GameRecord record)
    {
        try
        {
            _db.Update(record);
            Debug.Log($"[GameDatabase] Record saved — Level={record.Level} " +
                      $"Difficulty={record.Difficulty} Won={record.IsWon} " +
                      $"Time={record.ElapsedSeconds:F0}s");            
        }
        catch(Exception ex)
        {
            Debug.LogError($"[GameDatabase] Update failed: {ex.Message}");            
        }
    }
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
            var tmp = _db.Query<GameRecord>(query);

            return tmp.Count == 0 ? null : tmp[0];
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
            string query = "SELECT * from completed_games order by Id desc limit ?";

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
            string query = "SELECT * from completed_games order by Id desc limit 1";
            var tmp = _db.Query<GameRecord>(query);

            return tmp.Count == 0 ? null : tmp[0];
        }
        catch(Exception ex)
        {
            Debug.LogError($"[GameDatabase] GetLastRecord failed: {ex.Message}");
            return null;
        }
    }
    public static void FlushToDisk()
    {
        if (_db == null) return;

        try
        {
            // Merges the temporary WAL file into the targeted main game_history.db file
            _db.Execute("PRAGMA wal_checkpoint(TRUNCATE);");
        }
        catch (Exception)
        {
        }
    }
    public static void Close()
    {
        if (_db != null)
        {
            FlushToDisk();
            _db.Close();
            _db = null;
        }
    }
    public static GameStats GetGameStats()
    {
        try
        {
            return _db.Find<GameStats>(1);
        }
        catch (Exception ex)
        {
            Debug.LogError( $"[GameDatabase] GetGameStats failed: " + $"{ex.Message}");
            return null;
        }
    }
    private static void InitializeGameStats()
    {
        string sql = @"
            INSERT OR IGNORE INTO game_stats
            (
                Id,
                TotalGames,
                TotalWins,
                TotalPoints,
                FastestWinSeconds,

                SimpleCount,
                BeginnerCount,
                EasyCount,
                NoviceCount,
                ModerateCount,
                AdvancedCount,
                HardCount,
                ExpertCount,
                HardestCount
            )
            SELECT
                1,

                COUNT(*),

                COALESCE(
                    SUM(
                        CASE
                            WHEN IsWon = 1 THEN 1
                            ELSE 0
                        END
                    ),
                    0
                ),

                COALESCE(
                    SUM(
                        CASE
                            WHEN IsWon = 1 THEN Points
                            ELSE 0
                        END
                    ),
                    0
                ),

                MIN(
                    CASE
                        WHEN IsWon = 1
                        THEN ElapsedSeconds
                        ELSE NULL
                    END
                ),

                SUM(CASE WHEN Difficulty = 0 THEN 1 ELSE 0 END),
                SUM(CASE WHEN Difficulty = 1 THEN 1 ELSE 0 END),
                SUM(CASE WHEN Difficulty = 2 THEN 1 ELSE 0 END),
                SUM(CASE WHEN Difficulty = 3 THEN 1 ELSE 0 END),
                SUM(CASE WHEN Difficulty = 4 THEN 1 ELSE 0 END),
                SUM(CASE WHEN Difficulty = 5 THEN 1 ELSE 0 END),
                SUM(CASE WHEN Difficulty = 6 THEN 1 ELSE 0 END),
                SUM(CASE WHEN Difficulty = 7 THEN 1 ELSE 0 END),
                SUM(CASE WHEN Difficulty = 8 THEN 1 ELSE 0 END)

            FROM completed_games;
        ";

        _db.Execute(sql);
    }
    private static void CreateGameStatsTriggers()
    {
        CreateInsertStatsTrigger();
        CreateUpdateStatsTrigger();
        CreateDeleteStatsTrigger();
    }
    private static void CreateInsertStatsTrigger()
    {
        string sql = @"
            CREATE TRIGGER IF NOT EXISTS
                trg_completed_games_insert

            AFTER INSERT ON completed_games

            BEGIN

                UPDATE game_stats

                SET
                    TotalGames =
                        TotalGames + 1,

                    TotalWins =
                        TotalWins +
                        CASE
                            WHEN NEW.IsWon = 1
                            THEN 1
                            ELSE 0
                        END,

                    TotalPoints =
                        TotalPoints +
                        CASE
                            WHEN NEW.IsWon = 1
                            THEN NEW.Points
                            ELSE 0
                        END,

                    FastestWinSeconds =
                        CASE
                            WHEN NEW.IsWon = 1
                                AND
                                (
                                    FastestWinSeconds IS NULL
                                    OR NEW.ElapsedSeconds <
                                    FastestWinSeconds
                                )
                            THEN NEW.ElapsedSeconds

                            ELSE FastestWinSeconds
                        END,

                    SimpleCount =
                        SimpleCount +
                        CASE
                            WHEN NEW.Difficulty = 0 THEN 1
                            ELSE 0
                        END,

                    BeginnerCount =
                        BeginnerCount +
                        CASE
                            WHEN NEW.Difficulty = 1 THEN 1
                            ELSE 0
                        END,

                    EasyCount =
                        EasyCount +
                        CASE
                            WHEN NEW.Difficulty = 2 THEN 1
                            ELSE 0
                        END,

                    NoviceCount =
                        NoviceCount +
                        CASE
                            WHEN NEW.Difficulty = 3 THEN 1
                            ELSE 0
                        END,

                    ModerateCount =
                        ModerateCount +
                        CASE
                            WHEN NEW.Difficulty = 4 THEN 1
                            ELSE 0
                        END,

                    AdvancedCount =
                        AdvancedCount +
                        CASE
                            WHEN NEW.Difficulty = 5 THEN 1
                            ELSE 0
                        END,

                    HardCount =
                        HardCount +
                        CASE
                            WHEN NEW.Difficulty = 6 THEN 1
                            ELSE 0
                        END,

                    ExpertCount =
                        ExpertCount +
                        CASE
                            WHEN NEW.Difficulty = 7 THEN 1
                            ELSE 0
                        END,

                    HardestCount =
                        HardestCount +
                        CASE
                            WHEN NEW.Difficulty = 8 THEN 1
                            ELSE 0
                        END

                WHERE Id = 1;

            END;
        ";

        _db.Execute(sql);
    }
    private static void CreateUpdateStatsTrigger()
    {
        string sql = @"
            CREATE TRIGGER IF NOT EXISTS
                trg_completed_games_update

            AFTER UPDATE ON completed_games

            BEGIN

                UPDATE game_stats

                SET
                    TotalWins =
                        TotalWins
                        - CASE
                            WHEN OLD.IsWon = 1 THEN 1
                            ELSE 0
                        END
                        + CASE
                            WHEN NEW.IsWon = 1 THEN 1
                            ELSE 0
                        END,

                    TotalPoints =
                        TotalPoints
                        - CASE
                            WHEN OLD.IsWon = 1
                            THEN OLD.Points
                            ELSE 0
                        END
                        + CASE
                            WHEN NEW.IsWon = 1
                            THEN NEW.Points
                            ELSE 0
                        END,

                    FastestWinSeconds =
                    (
                        SELECT MIN(ElapsedSeconds)
                        FROM completed_games
                        WHERE IsWon = 1
                    ),

                    SimpleCount =
                        SimpleCount
                        - CASE WHEN OLD.Difficulty = 0 THEN 1 ELSE 0 END
                        + CASE WHEN NEW.Difficulty = 0 THEN 1 ELSE 0 END,

                    BeginnerCount =
                        BeginnerCount
                        - CASE WHEN OLD.Difficulty = 1 THEN 1 ELSE 0 END
                        + CASE WHEN NEW.Difficulty = 1 THEN 1 ELSE 0 END,

                    EasyCount =
                        EasyCount
                        - CASE WHEN OLD.Difficulty = 2 THEN 1 ELSE 0 END
                        + CASE WHEN NEW.Difficulty = 2 THEN 1 ELSE 0 END,

                    NoviceCount =
                        NoviceCount
                        - CASE WHEN OLD.Difficulty = 3 THEN 1 ELSE 0 END
                        + CASE WHEN NEW.Difficulty = 3 THEN 1 ELSE 0 END,

                    ModerateCount =
                        ModerateCount
                        - CASE WHEN OLD.Difficulty = 4 THEN 1 ELSE 0 END
                        + CASE WHEN NEW.Difficulty = 4 THEN 1 ELSE 0 END,

                    AdvancedCount =
                        AdvancedCount
                        - CASE WHEN OLD.Difficulty = 5 THEN 1 ELSE 0 END
                        + CASE WHEN NEW.Difficulty = 5 THEN 1 ELSE 0 END,

                    HardCount =
                        HardCount
                        - CASE WHEN OLD.Difficulty = 6 THEN 1 ELSE 0 END
                        + CASE WHEN NEW.Difficulty = 6 THEN 1 ELSE 0 END,

                    ExpertCount =
                        ExpertCount
                        - CASE WHEN OLD.Difficulty = 7 THEN 1 ELSE 0 END
                        + CASE WHEN NEW.Difficulty = 7 THEN 1 ELSE 0 END,

                    HardestCount =
                        HardestCount
                        - CASE WHEN OLD.Difficulty = 8 THEN 1 ELSE 0 END
                        + CASE WHEN NEW.Difficulty = 8 THEN 1 ELSE 0 END

                WHERE Id = 1;

            END;
        ";

        _db.Execute(sql);
    }
    private static void CreateDeleteStatsTrigger()
    {
        string sql = @"
            CREATE TRIGGER IF NOT EXISTS
                trg_completed_games_delete

            AFTER DELETE ON completed_games

            BEGIN

                UPDATE game_stats

                SET
                    TotalGames =
                        TotalGames - 1,

                    TotalWins =
                        TotalWins -
                        CASE
                            WHEN OLD.IsWon = 1 THEN 1
                            ELSE 0
                        END,

                    TotalPoints =
                        TotalPoints -
                        CASE
                            WHEN OLD.IsWon = 1
                            THEN OLD.Points
                            ELSE 0
                        END,

                    FastestWinSeconds =
                    (
                        SELECT MIN(ElapsedSeconds)
                        FROM completed_games
                        WHERE IsWon = 1
                    ),

                    SimpleCount =
                        SimpleCount -
                        CASE WHEN OLD.Difficulty = 0 THEN 1 ELSE 0 END,

                    BeginnerCount =
                        BeginnerCount -
                        CASE WHEN OLD.Difficulty = 1 THEN 1 ELSE 0 END,

                    EasyCount =
                        EasyCount -
                        CASE WHEN OLD.Difficulty = 2 THEN 1 ELSE 0 END,

                    NoviceCount =
                        NoviceCount -
                        CASE WHEN OLD.Difficulty = 3 THEN 1 ELSE 0 END,

                    ModerateCount =
                        ModerateCount -
                        CASE WHEN OLD.Difficulty = 4 THEN 1 ELSE 0 END,

                    AdvancedCount =
                        AdvancedCount -
                        CASE WHEN OLD.Difficulty = 5 THEN 1 ELSE 0 END,

                    HardCount =
                        HardCount -
                        CASE WHEN OLD.Difficulty = 6 THEN 1 ELSE 0 END,

                    ExpertCount =
                        ExpertCount -
                        CASE WHEN OLD.Difficulty = 7 THEN 1 ELSE 0 END,

                    HardestCount =
                        HardestCount -
                        CASE WHEN OLD.Difficulty = 8 THEN 1 ELSE 0 END

                WHERE Id = 1;

            END;
        ";

        _db.Execute(sql);
    }
}