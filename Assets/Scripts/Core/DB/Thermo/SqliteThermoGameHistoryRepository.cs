using System;
using System.Collections.Generic;
using System.Linq;
using SQLite;
using UnityEngine;

/// <summary>Journey history and aggregate statistics stored in SQLite.</summary>
public sealed class SqliteThermoGameHistoryRepository : IGameHistoryRepository
{
    private readonly SqliteDatabaseConnection _database;
    private SQLiteConnection Db => _database.Connection;

    private const int CurrentAggregateVersion = 1;

    public SqliteThermoGameHistoryRepository() : this(SqliteDatabaseConnection.Instance) { }

    public SqliteThermoGameHistoryRepository(SqliteDatabaseConnection database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    public void InitializeSchema()
    {
        SQLiteConnection db = Db;
        if (db == null)
        {
            Debug.LogError("[SqliteThermoGameHistoryRepository] Schema initialization skipped because the database is not open.");
            return;
        }

        try
        {
            Db.Execute(@"
                CREATE TABLE IF NOT EXISTS thermo_completed_games (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Level INTEGER NOT NULL,
                    Difficulty INTEGER NOT NULL,
                    ElapsedSeconds REAL NOT NULL,
                    LivesRemaining INTEGER NOT NULL,
                    Points INTEGER NOT NULL,
                    IsWon INTEGER NOT NULL,
                    CompletedAt TEXT NOT NULL,
                    UndoUses INTEGER NOT NULL DEFAULT 0,
                    PencilUses INTEGER NOT NULL DEFAULT 0,
                    EraseUses INTEGER NOT NULL DEFAULT 0,
                    SOSUses INTEGER NOT NULL DEFAULT 0,
                    AutoFillUses INTEGER NOT NULL DEFAULT 0
                );");
            Db.Execute(@"
                CREATE TABLE IF NOT EXISTS thermo_game_stats (
                    Id INTEGER PRIMARY KEY,
                    AggregateVersion INTEGER NOT NULL DEFAULT 0,
                    TotalGames INTEGER NOT NULL DEFAULT 0,
                    TotalWins INTEGER NOT NULL DEFAULT 0,
                    TotalPoints INTEGER NOT NULL DEFAULT 0,
                    FastestWinSeconds REAL,
                    CurrentStreak INTEGER NOT NULL DEFAULT 0,
                    SimpleCount INTEGER NOT NULL DEFAULT 0,
                    BeginnerCount INTEGER NOT NULL DEFAULT 0,
                    EasyCount INTEGER NOT NULL DEFAULT 0,
                    NoviceCount INTEGER NOT NULL DEFAULT 0,
                    ModerateCount INTEGER NOT NULL DEFAULT 0,
                    AdvancedCount INTEGER NOT NULL DEFAULT 0,
                    HardCount INTEGER NOT NULL DEFAULT 0,
                    ExpertCount INTEGER NOT NULL DEFAULT 0,
                    HardestCount INTEGER NOT NULL DEFAULT 0
                );");
            CreateIndexes();
            InitializeGameStats();
            RecreateGameStatsTriggers();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SqliteThermoGameHistoryRepository] Schema initialization failed: {ex.GetType().Name}: {ex}");
        }
    }

    // ============================================================
    // INDEXES
    // ============================================================

    private void CreateIndexes()
    {
        /*
         * Helps fastest-win queries.
         */
        Db.Execute(@"
            CREATE INDEX IF NOT EXISTS
                idx_thermo_completed_games_won_elapsed
            ON thermo_completed_games
            (
                IsWon,
                ElapsedSeconds
            );
        ");

        /*
         * Helps streak recalculation.
         */
        Db.Execute(@"
            CREATE INDEX IF NOT EXISTS
                idx_thermo_completed_games_won_id
            ON thermo_completed_games
            (
                IsWon,
                Id
            );
        ");
    }

    // ============================================================
    // INITIAL AGGREGATE BUILD / MIGRATION
    // ============================================================

    private void InitializeGameStats()
    {
        GameStats existing = Db.Query<GameStats>(
            "SELECT * FROM thermo_game_stats WHERE Id = 1 LIMIT 1").FirstOrDefault();

        /*
         * Brand-new aggregate table, or aggregate schema/
         * calculation changed.
         */
        if (existing == null ||
            existing.AggregateVersion <
                CurrentAggregateVersion)
        {
            Debug.Log(
                "[SqliteThermoGameHistoryRepository] Building thermo_game_stats " +
                "from thermo_completed_games.");

            RebuildGameStats();
        }
    }

    /*
     * This is the ONLY operation that scans the whole
     * thermo_completed_games table.
     *
     * Normally it runs only once when thermo_game_stats is first
     * introduced or when AggregateVersion changes.
     */
    public void RebuildGameStats()
    {
        string sql = $@"
            INSERT OR REPLACE INTO thermo_game_stats
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
                    FROM thermo_completed_games
                    WHERE
                        Id >
                        COALESCE(
                            (
                                SELECT MAX(Id)
                                FROM thermo_completed_games
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

            FROM thermo_completed_games;
        ";

        Db.Execute(sql);

        Debug.Log(
            "[SqliteThermoGameHistoryRepository] thermo_game_stats rebuilt.");
    }

    // ============================================================
    // TRIGGERS
    // ============================================================

    private void RecreateGameStatsTriggers()
    {
        /*
         * Important:
         *
         * CREATE TRIGGER IF NOT EXISTS will NOT replace an
         * existing trigger.
         *
         * Therefore drop the old definitions first.
         */

        Db.Execute(
            "DROP TRIGGER IF EXISTS " +
            "trg_thermo_completed_games_insert;");

        Db.Execute(
            "DROP TRIGGER IF EXISTS " +
            "trg_thermo_completed_games_update;");

        Db.Execute(
            "DROP TRIGGER IF EXISTS " +
            "trg_thermo_completed_games_delete;");

        CreateInsertStatsTrigger();
        CreateUpdateStatsTrigger();
        CreateDeleteStatsTrigger();
    }

    // ============================================================
    // INSERT TRIGGER
    // ============================================================

    private void CreateInsertStatsTrigger()
    {
        string sql = @"
            CREATE TRIGGER
                trg_thermo_completed_games_insert

            AFTER INSERT ON thermo_completed_games

            BEGIN

                UPDATE thermo_game_stats

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

        Db.Execute(sql);
    }

    // ============================================================
    // UPDATE TRIGGER
    // ============================================================

    private void CreateUpdateStatsTrigger()
    {
        string sql = @"
            CREATE TRIGGER
                trg_thermo_completed_games_update

            AFTER UPDATE OF
                IsWon,
                Points,
                ElapsedSeconds,
                Difficulty

            ON thermo_completed_games

            BEGIN

                UPDATE thermo_game_stats

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
                        FROM thermo_completed_games
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
                        FROM thermo_completed_games

                        WHERE
                            Id >
                            COALESCE(
                                (
                                    SELECT MAX(Id)
                                    FROM thermo_completed_games
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

        Db.Execute(sql);
    }

    // ============================================================
    // DELETE TRIGGER
    // ============================================================

    private void CreateDeleteStatsTrigger()
    {
        string sql = @"
            CREATE TRIGGER
                trg_thermo_completed_games_delete

            AFTER DELETE ON thermo_completed_games

            BEGIN

                UPDATE thermo_game_stats

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
                        FROM thermo_completed_games
                        WHERE IsWon = 1
                    ),

                    CurrentStreak =
                    (
                        SELECT COUNT(*)
                        FROM thermo_completed_games

                        WHERE
                            Id >
                            COALESCE(
                                (
                                    SELECT MAX(Id)
                                    FROM thermo_completed_games
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

        Db.Execute(sql);
    }

    // ============================================================
    // WRITE COMPLETED GAME
    // ============================================================

    public void Insert(
        GameRecord record)
    {
        try
        {
            /*
             * The INSERT trigger automatically updates
             * thermo_game_stats.
             */
            Db.Execute(@"
                INSERT INTO thermo_completed_games
                    (Level, Difficulty, ElapsedSeconds, LivesRemaining, Points, IsWon,
                     CompletedAt, UndoUses, PencilUses, EraseUses, SOSUses, AutoFillUses)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);",
                record.Level, record.Difficulty, record.ElapsedSeconds, record.LivesRemaining,
                record.Points, record.IsWon, record.CompletedAt, record.UndoUses, record.PencilUses,
                record.EraseUses, record.SOSUses, record.AutoFillUses);
            record.Id = (int)Db.ExecuteScalar<long>("SELECT last_insert_rowid();");

            Debug.Log(
                $"[SqliteThermoGameHistoryRepository] Record saved — " +
                $"Level={record.Level} " +
                $"Difficulty={record.Difficulty} " +
                $"Won={record.IsWon} " +
                $"Time={record.ElapsedSeconds:F0}s");
        }
        catch (Exception ex)
        {
            Debug.LogError(
                $"[SqliteThermoGameHistoryRepository] Insert failed: " +
                $"{ex.Message}");
        }
    }

    public void Update(
        GameRecord record)
    {
        try
        {
            /*
             * The UPDATE trigger automatically updates
             * thermo_game_stats.
             */
            Db.Execute(@"
                UPDATE thermo_completed_games
                SET Level = ?, Difficulty = ?, ElapsedSeconds = ?, LivesRemaining = ?, Points = ?,
                    IsWon = ?, CompletedAt = ?, UndoUses = ?, PencilUses = ?, EraseUses = ?,
                    SOSUses = ?, AutoFillUses = ?
                WHERE Id = ?;",
                record.Level, record.Difficulty, record.ElapsedSeconds, record.LivesRemaining,
                record.Points, record.IsWon, record.CompletedAt, record.UndoUses, record.PencilUses,
                record.EraseUses, record.SOSUses, record.AutoFillUses, record.Id);

            Debug.Log(
                $"[SqliteThermoGameHistoryRepository] Record updated — " +
                $"Level={record.Level} " +
                $"Difficulty={record.Difficulty} " +
                $"Won={record.IsWon} " +
                $"Time={record.ElapsedSeconds:F0}s");
        }
        catch (Exception ex)
        {
            Debug.LogError(
                $"[SqliteThermoGameHistoryRepository] Update failed: " +
                $"{ex.Message}");
        }
    }

    // ============================================================
    // AGGREGATE READ
    // ============================================================

    public GameStats GetGameStats()
    {
        try
        {
            if (Db == null)
            {
                Debug.LogError("[SqliteThermoGameHistoryRepository] GetGameStats skipped because the database is not open.");
                return null;
            }

            GameStats stats = Db.Query<GameStats>(
                "SELECT * FROM thermo_game_stats WHERE Id = 1 LIMIT 1").FirstOrDefault();

            /*
             * Defensive recovery.
             */
            if (stats == null)
            {
                RebuildGameStats();

                stats = Db.Query<GameStats>(
                    "SELECT * FROM thermo_game_stats WHERE Id = 1 LIMIT 1").FirstOrDefault();
            }

            return stats;
        }
        catch (Exception ex)
        {
            Debug.LogError(
                $"[SqliteThermoGameHistoryRepository] GetGameStats failed: " +
                $"{ex.Message}");

            return null;
        }
    }

    /*
     * No thermo_completed_games scan is required.
     *
     * The number of games at each difficulty is already
     * stored in thermo_game_stats.
     */
    public int GetTotalPossiblePoints(GameStats stats)
    {
        if (stats == null) return 0;

        return
            stats.SimpleCount   * ScoringSystem.GetAbsoluteMaximumScore(SudokuDifficulty.Simple) +
            stats.BeginnerCount * ScoringSystem.GetAbsoluteMaximumScore(SudokuDifficulty.Beginner) +
            stats.EasyCount     * ScoringSystem.GetAbsoluteMaximumScore(SudokuDifficulty.Easy) +
            stats.NoviceCount   * ScoringSystem.GetAbsoluteMaximumScore(SudokuDifficulty.Novice) +
            stats.ModerateCount * ScoringSystem.GetAbsoluteMaximumScore(SudokuDifficulty.Moderate) +
            stats.AdvancedCount * ScoringSystem.GetAbsoluteMaximumScore(SudokuDifficulty.Advanced) +
            stats.HardCount     * ScoringSystem.GetAbsoluteMaximumScore(SudokuDifficulty.Hard) +
            stats.ExpertCount   * ScoringSystem.GetAbsoluteMaximumScore(SudokuDifficulty.Expert) +
            stats.HardestCount  * ScoringSystem.GetAbsoluteMaximumScore(SudokuDifficulty.Hardest);
    }

    // ============================================================
    // EXISTING HISTORY READS
    // ============================================================

    public List<GameRecord> GetAll()
    {
        try
        {
            string query =
                "SELECT * " +
                "FROM thermo_completed_games " +
                "ORDER BY Id";

            return Db.Query<GameRecord>(
                query);
        }
        catch (Exception ex)
        {
            Debug.LogError(
                $"[SqliteThermoGameHistoryRepository] GetAll failed: " +
                $"{ex.Message}");

            return new List<GameRecord>();
        }
    }

    public List<GameRecord> GetNextSet(
        int offset)
    {
        try
        {
            string query =
                "SELECT * " +
                "FROM thermo_completed_games " +
                "ORDER BY Id DESC " +
                "LIMIT 10 OFFSET ?";

            return Db.Query<GameRecord>(
                query,
                offset);
        }
        catch (Exception ex)
        {
            Debug.LogError(
                $"[SqliteThermoGameHistoryRepository] GetNextSet failed: " +
                $"{ex.Message}");

            return new List<GameRecord>();
        }
    }

    public List<GameRecord> GetLastNRecordByDate(int number)
    {
        try
        {
            string query = "SELECT *  FROM thermo_completed_games ORDER BY Id DESC LIMIT ?";

            return Db.Query<GameRecord>(query,number);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SqliteThermoGameHistoryRepository] GetLastNRecordByDate failed: {ex.Message}");

            return new List<GameRecord>();
        }
    }

    public GameRecord GetLastRecord()
    {
        try
        {
            string query =
                "SELECT * " +
                "FROM thermo_completed_games " +
                "ORDER BY Id DESC " +
                "LIMIT 1";

            var result =
                Db.Query<GameRecord>(
                    query);

            return
                result.Count == 0
                    ? null
                    : result[0];
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[SqliteThermoGameHistoryRepository] GetLastRecord failed: " +
                $"{ex.Message}");

            return null;
        }
    }

    /*
     * Keeping these for compatibility with any existing
     * code. StatsPanel will no longer need them.
     */

    public GameRecord GetFastestWin()
    {
        try
        {
            string query =
                "SELECT * " +
                "FROM thermo_completed_games " +
                "WHERE IsWon = 1 " +
                "ORDER BY ElapsedSeconds " +
                "LIMIT 1";

            var result =
                Db.Query<GameRecord>(
                    query);

            return
                result.Count == 0
                    ? null
                    : result[0];
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[SqliteThermoGameHistoryRepository] GetFastestWin failed: " +
                $"{ex.Message}");

            return null;
        }
    }

    public int GetTotalPoints()
    {
        GameStats stats =
            GetGameStats();

        return stats?.TotalPoints ?? 0;
    }

    public int GetTotalWins()
    {
        GameStats stats =
            GetGameStats();

        return stats?.TotalWins ?? 0;
    }

    public int GetTotalGamesPlayed()
    {
        GameStats stats =
            GetGameStats();

        return stats?.TotalGames ?? 0;
    }

    // ============================================================
    // DEBUG
    // ============================================================

    public void LogGameStats()
    {
        GameStats stats =
            GetGameStats();

        if (stats == null)
        {
            Debug.Log(
                "[SqliteThermoGameHistoryRepository] No thermo_game_stats row.");

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

}
