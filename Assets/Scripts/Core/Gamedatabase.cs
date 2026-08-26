#if UNITY_WEBGL && !UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine;

public static class GameDatabase
{
    public static void Init()
    {
        Debug.Log("[GameDatabase] SQLite disabled for WebGL.");
    }

    public static void RebuildGameStats()
    {
    }

    public static void Insert(GameRecord record)
    {
        Debug.Log("[GameDatabase] WebGL history storage not implemented yet.");
    }

    public static void Update(GameRecord record)
    {
    }

    public static GameStats GetGameStats()
    {
        return new GameStats
        {
            Id = 1,
            AggregateVersion = 1
        };
    }

    public static int GetTotalPossiblePoints(GameStats stats)
    {
        return 0;
    }

    public static List<GameRecord> GetAll()
    {
        return new List<GameRecord>();
    }

    public static List<GameRecord> GetNextSet(int offset)
    {
        return new List<GameRecord>();
    }

    public static List<GameRecord> GetLastNRecordByDate(int number)
    {
        return new List<GameRecord>();
    }

    public static GameRecord GetLastRecord()
    {
        return null;
    }

    public static GameRecord GetFastestWin()
    {
        return null;
    }

    public static int GetTotalPoints()
    {
        return 0;
    }

    public static int GetTotalWins()
    {
        return 0;
    }

    public static int GetTotalGamesPlayed()
    {
        return 0;
    }

    public static void LogGameStats()
    {
    }

    public static void FlushToDisk()
    {
    }

    public static void Close()
    {
    }
}

#else
using System;
using System.Collections.Generic;
using System.IO;
using SQLite;
using UnityEngine;

public static class GameDatabase
{
    private static SQLiteConnection _db;

    /*
     * Increase this if the definition/schema of game_stats
     * changes in a future release and you want to rebuild
     * the aggregate once.
     */
    private const int CurrentAggregateVersion = 1;

    private static string DbPath =>
        Path.Combine(
            Application.persistentDataPath,
            "game_history.db");

    // ============================================================
    // INITIALIZATION
    // ============================================================

    public static void Init()
    {
        try
        {
            Debug.Log(
                $"DB stored at {DbPath}");

            _db =
                new SQLiteConnection(DbPath);

            /*
             * Existing source-of-truth table.
             */
            _db.CreateTable<GameRecord>();

            /*
             * New single-row aggregate table.
             *
             * sqlite-net will create it if missing.
             */
            _db.CreateTable<GameStats>();

            CreateIndexes();

            /*
             * On the first app run after introducing
             * game_stats, build it from completed_games.
             *
             * Future launches do not scan completed_games
             * unless AggregateVersion changes.
             */
            InitializeGameStats();

            /*
             * Recreate triggers so the latest trigger
             * definitions are always installed.
             */
            RecreateGameStatsTriggers();

            Debug.Log(
                $"[GameDatabase] Initialised at {DbPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError(
                $"[GameDatabase] Init failed: " +
                $"{ex.GetType().Name}: {ex}");
        }
    }

    // ============================================================
    // INDEXES
    // ============================================================

    private static void CreateIndexes()
    {
        /*
         * Helps fastest-win queries.
         */
        _db.Execute(@"
            CREATE INDEX IF NOT EXISTS
                idx_completed_games_won_elapsed
            ON completed_games
            (
                IsWon,
                ElapsedSeconds
            );
        ");

        /*
         * Helps streak recalculation.
         */
        _db.Execute(@"
            CREATE INDEX IF NOT EXISTS
                idx_completed_games_won_id
            ON completed_games
            (
                IsWon,
                Id
            );
        ");
    }

    // ============================================================
    // INITIAL AGGREGATE BUILD / MIGRATION
    // ============================================================

    private static void InitializeGameStats()
    {
        GameStats existing =
            _db.Find<GameStats>(1);

        /*
         * Brand-new aggregate table, or aggregate schema/
         * calculation changed.
         */
        if (existing == null ||
            existing.AggregateVersion <
                CurrentAggregateVersion)
        {
            Debug.Log(
                "[GameDatabase] Building game_stats " +
                "from completed_games.");

            RebuildGameStats();
        }
    }

    /*
     * This is the ONLY operation that scans the whole
     * completed_games table.
     *
     * Normally it runs only once when game_stats is first
     * introduced or when AggregateVersion changes.
     */
    public static void RebuildGameStats()
    {
        string sql = $@"
            INSERT OR REPLACE INTO game_stats
            (
                Id,
                AggregateVersion,

                TotalGames,
                TotalWins,
                TotalPoints,
                FastestWinSeconds,
                CurrentStreak,

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

                {CurrentAggregateVersion},

                COUNT(*),

                COALESCE(
                    SUM(
                        CASE
                            WHEN IsWon = 1
                            THEN 1
                            ELSE 0
                        END
                    ),
                    0
                ),

                COALESCE(
                    SUM(Points),
                    0
                ),

                MIN(
                    CASE
                        WHEN IsWon = 1
                        THEN ElapsedSeconds
                        ELSE NULL
                    END
                ),

                /*
                 * Current streak =
                 * number of consecutive wins after the
                 * most recent loss.
                 */
                (
                    SELECT COUNT(*)
                    FROM completed_games
                    WHERE
                        Id >
                        COALESCE(
                            (
                                SELECT MAX(Id)
                                FROM completed_games
                                WHERE IsWon = 0
                            ),
                            0
                        )
                        AND IsWon = 1
                ),

                COALESCE(
                    SUM(
                        CASE
                            WHEN Difficulty = 0
                            THEN 1 ELSE 0
                        END
                    ),
                    0
                ),

                COALESCE(
                    SUM(
                        CASE
                            WHEN Difficulty = 1
                            THEN 1 ELSE 0
                        END
                    ),
                    0
                ),

                COALESCE(
                    SUM(
                        CASE
                            WHEN Difficulty = 2
                            THEN 1 ELSE 0
                        END
                    ),
                    0
                ),

                COALESCE(
                    SUM(
                        CASE
                            WHEN Difficulty = 3
                            THEN 1 ELSE 0
                        END
                    ),
                    0
                ),

                COALESCE(
                    SUM(
                        CASE
                            WHEN Difficulty = 4
                            THEN 1 ELSE 0
                        END
                    ),
                    0
                ),

                COALESCE(
                    SUM(
                        CASE
                            WHEN Difficulty = 5
                            THEN 1 ELSE 0
                        END
                    ),
                    0
                ),

                COALESCE(
                    SUM(
                        CASE
                            WHEN Difficulty = 6
                            THEN 1 ELSE 0
                        END
                    ),
                    0
                ),

                COALESCE(
                    SUM(
                        CASE
                            WHEN Difficulty = 7
                            THEN 1 ELSE 0
                        END
                    ),
                    0
                ),

                COALESCE(
                    SUM(
                        CASE
                            WHEN Difficulty = 8
                            THEN 1 ELSE 0
                        END
                    ),
                    0
                )

            FROM completed_games;
        ";

        _db.Execute(sql);

        Debug.Log(
            "[GameDatabase] game_stats rebuilt.");
    }

    // ============================================================
    // TRIGGERS
    // ============================================================

    private static void RecreateGameStatsTriggers()
    {
        /*
         * Important:
         *
         * CREATE TRIGGER IF NOT EXISTS will NOT replace an
         * existing trigger.
         *
         * Therefore drop the old definitions first.
         */

        _db.Execute(
            "DROP TRIGGER IF EXISTS " +
            "trg_completed_games_insert;");

        _db.Execute(
            "DROP TRIGGER IF EXISTS " +
            "trg_completed_games_update;");

        _db.Execute(
            "DROP TRIGGER IF EXISTS " +
            "trg_completed_games_delete;");

        CreateInsertStatsTrigger();
        CreateUpdateStatsTrigger();
        CreateDeleteStatsTrigger();
    }

    // ============================================================
    // INSERT TRIGGER
    // ============================================================

    private static void CreateInsertStatsTrigger()
    {
        string sql = @"
            CREATE TRIGGER
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
                        NEW.Points,

                    /*
                     * New winning game becomes fastest
                     * if there was no previous win or its
                     * time is faster.
                     */
                    FastestWinSeconds =
                        CASE

                            WHEN
                                NEW.IsWon = 1
                                AND
                                (
                                    FastestWinSeconds IS NULL

                                    OR

                                    NEW.ElapsedSeconds <
                                        FastestWinSeconds
                                )

                            THEN
                                NEW.ElapsedSeconds

                            ELSE
                                FastestWinSeconds

                        END,

                    /*
                     * New games affect the live streak.
                     */
                    CurrentStreak =
                        CASE

                            WHEN NEW.IsWon = 1
                            THEN CurrentStreak + 1

                            ELSE 0

                        END,

                    SimpleCount =
                        SimpleCount +
                        CASE
                            WHEN NEW.Difficulty = 0
                            THEN 1 ELSE 0
                        END,

                    BeginnerCount =
                        BeginnerCount +
                        CASE
                            WHEN NEW.Difficulty = 1
                            THEN 1 ELSE 0
                        END,

                    EasyCount =
                        EasyCount +
                        CASE
                            WHEN NEW.Difficulty = 2
                            THEN 1 ELSE 0
                        END,

                    NoviceCount =
                        NoviceCount +
                        CASE
                            WHEN NEW.Difficulty = 3
                            THEN 1 ELSE 0
                        END,

                    ModerateCount =
                        ModerateCount +
                        CASE
                            WHEN NEW.Difficulty = 4
                            THEN 1 ELSE 0
                        END,

                    AdvancedCount =
                        AdvancedCount +
                        CASE
                            WHEN NEW.Difficulty = 5
                            THEN 1 ELSE 0
                        END,

                    HardCount =
                        HardCount +
                        CASE
                            WHEN NEW.Difficulty = 6
                            THEN 1 ELSE 0
                        END,

                    ExpertCount =
                        ExpertCount +
                        CASE
                            WHEN NEW.Difficulty = 7
                            THEN 1 ELSE 0
                        END,

                    HardestCount =
                        HardestCount +
                        CASE
                            WHEN NEW.Difficulty = 8
                            THEN 1 ELSE 0
                        END

                WHERE Id = 1;

            END;
        ";

        _db.Execute(sql);
    }

    // ============================================================
    // UPDATE TRIGGER
    // ============================================================

    private static void CreateUpdateStatsTrigger()
    {
        string sql = @"
            CREATE TRIGGER
                trg_completed_games_update

            AFTER UPDATE OF
                IsWon,
                Points,
                ElapsedSeconds,
                Difficulty

            ON completed_games

            BEGIN

                UPDATE game_stats

                SET
                    /*
                     * TotalGames stays unchanged because
                     * this is an UPDATE, not a new game.
                     */

                    TotalWins =
                        TotalWins

                        - CASE
                            WHEN OLD.IsWon = 1
                            THEN 1
                            ELSE 0
                          END

                        + CASE
                            WHEN NEW.IsWon = 1
                            THEN 1
                            ELSE 0
                          END,

                    TotalPoints =
                        TotalPoints
                        - OLD.Points
                        + NEW.Points,

                    /*
                     * UPDATE can change the current fastest
                     * game, so recompute it.
                     *
                     * UPDATEs are rare compared with normal
                     * inserts.
                     */
                    FastestWinSeconds =
                    (
                        SELECT MIN(ElapsedSeconds)
                        FROM completed_games
                        WHERE IsWon = 1
                    ),

                    /*
                     * Important for historical retries:
                     * changing an old loss to a win can alter
                     * the sequence of wins/losses.
                     */
                    CurrentStreak =
                    (
                        SELECT COUNT(*)
                        FROM completed_games

                        WHERE
                            Id >
                            COALESCE(
                                (
                                    SELECT MAX(Id)
                                    FROM completed_games
                                    WHERE IsWon = 0
                                ),
                                0
                            )

                            AND IsWon = 1
                    ),

                    SimpleCount =
                        SimpleCount
                        - CASE
                            WHEN OLD.Difficulty = 0
                            THEN 1 ELSE 0
                          END
                        + CASE
                            WHEN NEW.Difficulty = 0
                            THEN 1 ELSE 0
                          END,

                    BeginnerCount =
                        BeginnerCount
                        - CASE
                            WHEN OLD.Difficulty = 1
                            THEN 1 ELSE 0
                          END
                        + CASE
                            WHEN NEW.Difficulty = 1
                            THEN 1 ELSE 0
                          END,

                    EasyCount =
                        EasyCount
                        - CASE
                            WHEN OLD.Difficulty = 2
                            THEN 1 ELSE 0
                          END
                        + CASE
                            WHEN NEW.Difficulty = 2
                            THEN 1 ELSE 0
                          END,

                    NoviceCount =
                        NoviceCount
                        - CASE
                            WHEN OLD.Difficulty = 3
                            THEN 1 ELSE 0
                          END
                        + CASE
                            WHEN NEW.Difficulty = 3
                            THEN 1 ELSE 0
                          END,

                    ModerateCount =
                        ModerateCount
                        - CASE
                            WHEN OLD.Difficulty = 4
                            THEN 1 ELSE 0
                          END
                        + CASE
                            WHEN NEW.Difficulty = 4
                            THEN 1 ELSE 0
                          END,

                    AdvancedCount =
                        AdvancedCount
                        - CASE
                            WHEN OLD.Difficulty = 5
                            THEN 1 ELSE 0
                          END
                        + CASE
                            WHEN NEW.Difficulty = 5
                            THEN 1 ELSE 0
                          END,

                    HardCount =
                        HardCount
                        - CASE
                            WHEN OLD.Difficulty = 6
                            THEN 1 ELSE 0
                          END
                        + CASE
                            WHEN NEW.Difficulty = 6
                            THEN 1 ELSE 0
                          END,

                    ExpertCount =
                        ExpertCount
                        - CASE
                            WHEN OLD.Difficulty = 7
                            THEN 1 ELSE 0
                          END
                        + CASE
                            WHEN NEW.Difficulty = 7
                            THEN 1 ELSE 0
                          END,

                    HardestCount =
                        HardestCount
                        - CASE
                            WHEN OLD.Difficulty = 8
                            THEN 1 ELSE 0
                          END
                        + CASE
                            WHEN NEW.Difficulty = 8
                            THEN 1 ELSE 0
                          END

                WHERE Id = 1;

            END;
        ";

        _db.Execute(sql);
    }

    // ============================================================
    // DELETE TRIGGER
    // ============================================================

    private static void CreateDeleteStatsTrigger()
    {
        string sql = @"
            CREATE TRIGGER
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
                            WHEN OLD.IsWon = 1
                            THEN 1
                            ELSE 0
                        END,

                    TotalPoints =
                        TotalPoints -
                        OLD.Points,

                    FastestWinSeconds =
                    (
                        SELECT MIN(ElapsedSeconds)
                        FROM completed_games
                        WHERE IsWon = 1
                    ),

                    CurrentStreak =
                    (
                        SELECT COUNT(*)
                        FROM completed_games

                        WHERE
                            Id >
                            COALESCE(
                                (
                                    SELECT MAX(Id)
                                    FROM completed_games
                                    WHERE IsWon = 0
                                ),
                                0
                            )

                            AND IsWon = 1
                    ),

                    SimpleCount =
                        SimpleCount -
                        CASE
                            WHEN OLD.Difficulty = 0
                            THEN 1 ELSE 0
                        END,

                    BeginnerCount =
                        BeginnerCount -
                        CASE
                            WHEN OLD.Difficulty = 1
                            THEN 1 ELSE 0
                        END,

                    EasyCount =
                        EasyCount -
                        CASE
                            WHEN OLD.Difficulty = 2
                            THEN 1 ELSE 0
                        END,

                    NoviceCount =
                        NoviceCount -
                        CASE
                            WHEN OLD.Difficulty = 3
                            THEN 1 ELSE 0
                        END,

                    ModerateCount =
                        ModerateCount -
                        CASE
                            WHEN OLD.Difficulty = 4
                            THEN 1 ELSE 0
                        END,

                    AdvancedCount =
                        AdvancedCount -
                        CASE
                            WHEN OLD.Difficulty = 5
                            THEN 1 ELSE 0
                        END,

                    HardCount =
                        HardCount -
                        CASE
                            WHEN OLD.Difficulty = 6
                            THEN 1 ELSE 0
                        END,

                    ExpertCount =
                        ExpertCount -
                        CASE
                            WHEN OLD.Difficulty = 7
                            THEN 1 ELSE 0
                        END,

                    HardestCount =
                        HardestCount -
                        CASE
                            WHEN OLD.Difficulty = 8
                            THEN 1 ELSE 0
                        END

                WHERE Id = 1;

            END;
        ";

        _db.Execute(sql);
    }

    // ============================================================
    // WRITE COMPLETED GAME
    // ============================================================

    public static void Insert(
        GameRecord record)
    {
        try
        {
            /*
             * The INSERT trigger automatically updates
             * game_stats.
             */
            _db.Insert(record);

            Debug.Log(
                $"[GameDatabase] Record saved — " +
                $"Level={record.Level} " +
                $"Difficulty={record.Difficulty} " +
                $"Won={record.IsWon} " +
                $"Time={record.ElapsedSeconds:F0}s");
        }
        catch (Exception ex)
        {
            Debug.LogError(
                $"[GameDatabase] Insert failed: " +
                $"{ex.Message}");
        }
    }

    public static void Update(
        GameRecord record)
    {
        try
        {
            /*
             * The UPDATE trigger automatically updates
             * game_stats.
             */
            _db.Update(record);

            Debug.Log(
                $"[GameDatabase] Record updated — " +
                $"Level={record.Level} " +
                $"Difficulty={record.Difficulty} " +
                $"Won={record.IsWon} " +
                $"Time={record.ElapsedSeconds:F0}s");
        }
        catch (Exception ex)
        {
            Debug.LogError(
                $"[GameDatabase] Update failed: " +
                $"{ex.Message}");
        }
    }

    // ============================================================
    // AGGREGATE READ
    // ============================================================

    public static GameStats GetGameStats()
    {
        try
        {
            GameStats stats =
                _db.Find<GameStats>(1);

            /*
             * Defensive recovery.
             */
            if (stats == null)
            {
                RebuildGameStats();

                stats =
                    _db.Find<GameStats>(1);
            }

            return stats;
        }
        catch (Exception ex)
        {
            Debug.LogError(
                $"[GameDatabase] GetGameStats failed: " +
                $"{ex.Message}");

            return null;
        }
    }

    /*
     * No completed_games scan is required.
     *
     * The number of games at each difficulty is already
     * stored in game_stats.
     */
    public static int GetTotalPossiblePoints(
        GameStats stats)
    {
        if (stats == null)
            return 0;

        return
            stats.SimpleCount *
            ScoringSystem.GetAbsoluteMaximumScore(
                SudokuDifficulty.Simple)

            +

            stats.BeginnerCount *
            ScoringSystem.GetAbsoluteMaximumScore(
                SudokuDifficulty.Beginner)

            +

            stats.EasyCount *
            ScoringSystem.GetAbsoluteMaximumScore(
                SudokuDifficulty.Easy)

            +

            stats.NoviceCount *
            ScoringSystem.GetAbsoluteMaximumScore(
                SudokuDifficulty.Novice)

            +

            stats.ModerateCount *
            ScoringSystem.GetAbsoluteMaximumScore(
                SudokuDifficulty.Moderate)

            +

            stats.AdvancedCount *
            ScoringSystem.GetAbsoluteMaximumScore(
                SudokuDifficulty.Advanced)

            +

            stats.HardCount *
            ScoringSystem.GetAbsoluteMaximumScore(
                SudokuDifficulty.Hard)

            +

            stats.ExpertCount *
            ScoringSystem.GetAbsoluteMaximumScore(
                SudokuDifficulty.Expert)

            +

            stats.HardestCount *
            ScoringSystem.GetAbsoluteMaximumScore(
                SudokuDifficulty.Hardest);
    }

    // ============================================================
    // EXISTING HISTORY READS
    // ============================================================

    public static List<GameRecord> GetAll()
    {
        try
        {
            string query =
                "SELECT * " +
                "FROM completed_games " +
                "ORDER BY Id";

            return _db.Query<GameRecord>(
                query);
        }
        catch (Exception ex)
        {
            Debug.LogError(
                $"[GameDatabase] GetAll failed: " +
                $"{ex.Message}");

            return new List<GameRecord>();
        }
    }

    public static List<GameRecord> GetNextSet(
        int offset)
    {
        try
        {
            string query =
                "SELECT * " +
                "FROM completed_games " +
                "ORDER BY Id DESC " +
                "LIMIT 10 OFFSET ?";

            return _db.Query<GameRecord>(
                query,
                offset);
        }
        catch (Exception ex)
        {
            Debug.LogError(
                $"[GameDatabase] GetNextSet failed: " +
                $"{ex.Message}");

            return new List<GameRecord>();
        }
    }

    public static List<GameRecord> GetLastNRecordByDate(int number)
    {
        try
        {
            string query = "SELECT *  FROM completed_games ORDER BY Id DESC LIMIT ?";

            return _db.Query<GameRecord>(query,number);
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
            string query =
                "SELECT * " +
                "FROM completed_games " +
                "ORDER BY Id DESC " +
                "LIMIT 1";

            var result =
                _db.Query<GameRecord>(
                    query);

            return
                result.Count == 0
                    ? null
                    : result[0];
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GameDatabase] GetLastRecord failed: " +
                $"{ex.Message}");

            return null;
        }
    }

    /*
     * Keeping these for compatibility with any existing
     * code. StatsPanel will no longer need them.
     */

    public static GameRecord GetFastestWin()
    {
        try
        {
            string query =
                "SELECT * " +
                "FROM completed_games " +
                "WHERE IsWon = 1 " +
                "ORDER BY ElapsedSeconds " +
                "LIMIT 1";

            var result =
                _db.Query<GameRecord>(
                    query);

            return
                result.Count == 0
                    ? null
                    : result[0];
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GameDatabase] GetFastestWin failed: " +
                $"{ex.Message}");

            return null;
        }
    }

    public static int GetTotalPoints()
    {
        GameStats stats =
            GetGameStats();

        return stats?.TotalPoints ?? 0;
    }

    public static int GetTotalWins()
    {
        GameStats stats =
            GetGameStats();

        return stats?.TotalWins ?? 0;
    }

    public static int GetTotalGamesPlayed()
    {
        GameStats stats =
            GetGameStats();

        return stats?.TotalGames ?? 0;
    }

    // ============================================================
    // DEBUG
    // ============================================================

    public static void LogGameStats()
    {
        GameStats stats =
            GetGameStats();

        if (stats == null)
        {
            Debug.Log(
                "[GameDatabase] No game_stats row.");

            return;
        }

        Debug.Log(
            "GAME STATS\n" +
            $"Games={stats.TotalGames}\n" +
            $"Wins={stats.TotalWins}\n" +
            $"Points={stats.TotalPoints}\n" +
            $"Fastest={stats.FastestWinSeconds}\n" +
            $"Streak={stats.CurrentStreak}\n" +
            $"Simple={stats.SimpleCount}\n" +
            $"Beginner={stats.BeginnerCount}\n" +
            $"Easy={stats.EasyCount}\n" +
            $"Novice={stats.NoviceCount}\n" +
            $"Moderate={stats.ModerateCount}\n" +
            $"Advanced={stats.AdvancedCount}\n" +
            $"Hard={stats.HardCount}\n" +
            $"Expert={stats.ExpertCount}\n" +
            $"Hardest={stats.HardestCount}"
        );
    }

    // ============================================================
    // TEARDOWN
    // ============================================================

    public static void FlushToDisk()
    {
        if (_db == null)
            return;

        try
        {
            _db.Execute(
                "PRAGMA wal_checkpoint(TRUNCATE);");
        }
        catch (Exception)
        {
        }
    }

    public static void Close()
    {
        if (_db == null)
            return;

        FlushToDisk();

        _db.Close();
        _db = null;
    }
}

#endif