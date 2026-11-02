using System;
using System.Collections.Generic;
using System.IO;
using SQLite;
using UnityEngine;

public static class GameDatabase
{
    private static SQLiteConnection _db;
    internal static SQLiteConnection Connection => _db;

    /*
     * Increase this if the definition/schema of game_stats
     * changes in a future release and you want to rebuild
     * the aggregate once.
     */
    private const int CurrentAggregateVersion = 1;
    private const int CurrentDbVersion = 2;

    private static string DbPath =>
        Path.Combine(
            Application.persistentDataPath,
            "game_history.db");

    // ============================================================
    // INITIALIZATION
    // ============================================================

    public static void Init()
    {
        // User.Awake and DatabaseLifecycleHandler can both initialize the DB
        // during startup. Reuse an open connection instead of replacing it.
        if (_db != null)
            return;

        try
        {
            Debug.Log($"DB stored at {DbPath}");

            bool isNewDatabase = !File.Exists(DbPath);

            _db = new SQLiteConnection(DbPath);

            if (isNewDatabase)
            {
                _db.CreateTable<GameRecord>();

                SetDatabaseVersion(CurrentDbVersion);

                Debug.Log($"[GameDatabase] Created new database at version {CurrentDbVersion}.");
            }
            else
            {
                MigrateDatabase();
            }

            CreateDailyGameChallengeTable();
            CreateDailyGameChallengeStatsTable();
            CreateDailyGameChallengeCompletionTrigger();
            CreateDailyGameChallengeFirstRowTrigger();

            _db.CreateTable<GameStats>();

            CreateIndexes();

            InitializeGameStats();

            RecreateGameStatsTriggers();
        }
        catch (Exception ex)
        {
            try { _db?.Close(); }
            catch (Exception) { }
            _db = null;
            Debug.LogError($"[GameDatabase] Init failed: {ex.GetType().Name}: {ex}");
        }
    }

    // ============================================================
    // DATABASE MIGRATION
    // ============================================================

    private static void MigrateDatabase()
    {
        int version =
            GetDatabaseVersion();

        Debug.Log(
            $"[GameDatabase] Current DB version: {version}");

        if (version > CurrentDbVersion)
        {
            throw new InvalidOperationException(
                $"Database version {version} is newer than " +
                $"supported version {CurrentDbVersion}.");
        }

        //
        // Legacy schema -> usage-stat schema.
        //
        if (version < 1)
        {
            MigrateToVersion1();

            version = 1;
        }

        if (version < 2)
        {
            MigrateToVersion2();

            version = 2;
        }

        Debug.Log(
            $"[GameDatabase] Database ready at version " +
            $"{version}.");
    }

    private static void MigrateToVersion2()
    {
        Debug.Log("[GameDatabase] Migrating DB v1 -> v2.");

        _db.BeginTransaction();
        try
        {
            CreateDailyGameChallengeTable();
            SetDatabaseVersion(2);
            _db.Commit();
            Debug.Log("[GameDatabase] Migration v1 -> v2 complete.");
        }
        catch (Exception)
        {
            _db.Rollback();
            Debug.LogError("[GameDatabase] Migration v1 -> v2 failed.");
            throw;
        }
    }

    private static void MigrateToVersion1()
    {
        Debug.Log("[GameDatabase] Migrating DB v0 -> v1.");

        _db.BeginTransaction();

        try
        {
            _db.Execute(@"ALTER TABLE completed_games ADD COLUMN UndoUses INTEGER NOT NULL DEFAULT 0;");
            _db.Execute(@"ALTER TABLE completed_games ADD COLUMN PencilUses INTEGER NOT NULL DEFAULT 0;");
            _db.Execute(@"ALTER TABLE completed_games ADD COLUMN EraseUses INTEGER NOT NULL DEFAULT 0;");
            _db.Execute(@"ALTER TABLE completed_games ADD COLUMN SOSUses INTEGER NOT NULL DEFAULT 0;");
            _db.Execute(@"ALTER TABLE completed_games ADD COLUMN AutoFillUses INTEGER NOT NULL DEFAULT 0;");

            SetDatabaseVersion(1);

            _db.Commit();

            Debug.Log("[GameDatabase] Migration v0 -> v1 complete.");
        }
        catch (Exception)
        {
            _db.Rollback();

            Debug.LogError("[GameDatabase] Migration v0 -> v1 failed.");

            throw;
        }
    }
    
    private static void CreateDailyGameChallengeTable()
    {
        _db.Execute(@"
            CREATE TABLE IF NOT EXISTS DailyGame_challenge
            (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CurrentDate TEXT NOT NULL DEFAULT (date('now', 'localtime')),
                Difficulty INTEGER NOT NULL,
                ElapsedSeconds FLOAT NOT NULL DEFAULT 0,
                LivesRemaining INTEGER NOT NULL DEFAULT 0,
                Stars INTEGER NOT NULL DEFAULT 0 CHECK (Stars BETWEEN 0 AND 3),
                IsWon BOOLEAN NOT NULL DEFAULT 0,
                IsAttempted BOOLEAN NOT NULL DEFAULT 0 CHECK (IsAttempted IN (0, 1)),
                Started BOOLEAN NOT NULL DEFAULT 0 CHECK (Started IN (0, 1)),
                CompletedAt VARCHAR,
                UndoUses INTEGER NOT NULL DEFAULT 0,
                PencilUses INTEGER NOT NULL DEFAULT 0,
                EraseUses INTEGER NOT NULL DEFAULT 0,
                SOSUses INTEGER NOT NULL DEFAULT 0,
                AutofillUses INTEGER NOT NULL DEFAULT 0
            );
        ");
    }

    private static void CreateDailyGameChallengeStatsTable()
    {
        _db.Execute(@"
            CREATE TABLE IF NOT EXISTS DailyGameChallengeStats
            (
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                FirstStartedAt TEXT,
                CurrentStreakStartedAt DATETIME,
                CurrentStreak INTEGER NOT NULL DEFAULT 0 CHECK (CurrentStreak >= 0),
                LastPlayedAt DATETIME,
                LastLostAt DATETIME,
                GamesPlayed INTEGER NOT NULL DEFAULT 0 CHECK (GamesPlayed >= 0),
                GamesWon INTEGER NOT NULL DEFAULT 0 CHECK (GamesWon >= 0 AND GamesWon <= GamesPlayed),
                BestTimeSeconds FLOAT,
                BestTimeGameId INTEGER,
                SimpleBestTimeGameId INTEGER,
                BeginnerBestTimeGameId INTEGER,
                EasyBestTimeGameId INTEGER,
                NoviceBestTimeGameId INTEGER,
                ModerateBestTimeGameId INTEGER,
                AdvancedBestTimeGameId INTEGER,
                HardBestTimeGameId INTEGER,
                ExpertBestTimeGameId INTEGER,
                HardestBestTimeGameId INTEGER
            );
        ");

        _db.Execute(
            "INSERT OR IGNORE INTO DailyGameChallengeStats (Id) VALUES (1);");
    }

    private static void CreateDailyGameChallengeCompletionTrigger()
    {
        _db.Execute("DROP TRIGGER IF EXISTS trg_daily_game_challenge_complete_win;");
        _db.Execute("DROP TRIGGER IF EXISTS trg_daily_game_challenge_complete_loss;");

        _db.Execute(@"
            CREATE TRIGGER trg_daily_game_challenge_complete_win
            AFTER UPDATE OF CompletedAt ON DailyGame_challenge
            WHEN COALESCE(TRIM(OLD.CompletedAt), '') = ''
             AND COALESCE(TRIM(NEW.CompletedAt), '') <> ''
             AND NEW.IsWon = 1
            BEGIN
                UPDATE DailyGameChallengeStats
                SET
                    GamesPlayed = GamesPlayed + 1,
                    LastPlayedAt = NEW.CompletedAt,
                    CurrentStreak = CurrentStreak + 1,
                    CurrentStreakStartedAt = CASE
                        WHEN CurrentStreak = 0 THEN NEW.CompletedAt
                        ELSE CurrentStreakStartedAt
                    END,
                    GamesWon = GamesWon + 1,
                    BestTimeSeconds = CASE
                        WHEN BestTimeSeconds IS NULL OR NEW.ElapsedSeconds < BestTimeSeconds
                        THEN NEW.ElapsedSeconds
                        ELSE BestTimeSeconds
                    END,
                    BestTimeGameId = CASE
                        WHEN BestTimeSeconds IS NULL OR NEW.ElapsedSeconds < BestTimeSeconds
                        THEN NEW.Id
                        ELSE BestTimeGameId
                    END,
                    SimpleBestTimeGameId = CASE
                        WHEN NEW.Difficulty = 0
                         AND (SimpleBestTimeGameId IS NULL OR NEW.ElapsedSeconds < COALESCE(
                             (SELECT ElapsedSeconds FROM DailyGame_challenge WHERE Id = (SELECT SimpleBestTimeGameId FROM DailyGameChallengeStats WHERE Id = 1)),
                             1.0e99))
                        THEN NEW.Id ELSE SimpleBestTimeGameId
                    END,
                    BeginnerBestTimeGameId = CASE
                        WHEN NEW.Difficulty = 1
                         AND (BeginnerBestTimeGameId IS NULL OR NEW.ElapsedSeconds < COALESCE(
                             (SELECT ElapsedSeconds FROM DailyGame_challenge WHERE Id = (SELECT BeginnerBestTimeGameId FROM DailyGameChallengeStats WHERE Id = 1)),
                             1.0e99))
                        THEN NEW.Id ELSE BeginnerBestTimeGameId
                    END,
                    EasyBestTimeGameId = CASE
                        WHEN NEW.Difficulty = 2
                         AND (EasyBestTimeGameId IS NULL OR NEW.ElapsedSeconds < COALESCE(
                             (SELECT ElapsedSeconds FROM DailyGame_challenge WHERE Id = (SELECT EasyBestTimeGameId FROM DailyGameChallengeStats WHERE Id = 1)),
                             1.0e99))
                        THEN NEW.Id ELSE EasyBestTimeGameId
                    END,
                    NoviceBestTimeGameId = CASE
                        WHEN NEW.Difficulty = 3
                         AND (NoviceBestTimeGameId IS NULL OR NEW.ElapsedSeconds < COALESCE(
                             (SELECT ElapsedSeconds FROM DailyGame_challenge WHERE Id = (SELECT NoviceBestTimeGameId FROM DailyGameChallengeStats WHERE Id = 1)),
                             1.0e99))
                        THEN NEW.Id ELSE NoviceBestTimeGameId
                    END,
                    ModerateBestTimeGameId = CASE
                        WHEN NEW.Difficulty = 4
                         AND (ModerateBestTimeGameId IS NULL OR NEW.ElapsedSeconds < COALESCE(
                             (SELECT ElapsedSeconds FROM DailyGame_challenge WHERE Id = (SELECT ModerateBestTimeGameId FROM DailyGameChallengeStats WHERE Id = 1)),
                             1.0e99))
                        THEN NEW.Id ELSE ModerateBestTimeGameId
                    END,
                    AdvancedBestTimeGameId = CASE
                        WHEN NEW.Difficulty = 5
                         AND (AdvancedBestTimeGameId IS NULL OR NEW.ElapsedSeconds < COALESCE(
                             (SELECT ElapsedSeconds FROM DailyGame_challenge WHERE Id = (SELECT AdvancedBestTimeGameId FROM DailyGameChallengeStats WHERE Id = 1)),
                             1.0e99))
                        THEN NEW.Id ELSE AdvancedBestTimeGameId
                    END,
                    HardBestTimeGameId = CASE
                        WHEN NEW.Difficulty = 6
                         AND (HardBestTimeGameId IS NULL OR NEW.ElapsedSeconds < COALESCE(
                             (SELECT ElapsedSeconds FROM DailyGame_challenge WHERE Id = (SELECT HardBestTimeGameId FROM DailyGameChallengeStats WHERE Id = 1)),
                             1.0e99))
                        THEN NEW.Id ELSE HardBestTimeGameId
                    END,
                    ExpertBestTimeGameId = CASE
                        WHEN NEW.Difficulty = 7
                         AND (ExpertBestTimeGameId IS NULL OR NEW.ElapsedSeconds < COALESCE(
                             (SELECT ElapsedSeconds FROM DailyGame_challenge WHERE Id = (SELECT ExpertBestTimeGameId FROM DailyGameChallengeStats WHERE Id = 1)),
                             1.0e99))
                        THEN NEW.Id ELSE ExpertBestTimeGameId
                    END,
                    HardestBestTimeGameId = CASE
                        WHEN NEW.Difficulty = 8
                         AND (HardestBestTimeGameId IS NULL OR NEW.ElapsedSeconds < COALESCE(
                             (SELECT ElapsedSeconds FROM DailyGame_challenge WHERE Id = (SELECT HardestBestTimeGameId FROM DailyGameChallengeStats WHERE Id = 1)),
                             1.0e99))
                        THEN NEW.Id ELSE HardestBestTimeGameId
                    END
                WHERE Id = 1;
            END;
        ");

        _db.Execute(@"
            CREATE TRIGGER trg_daily_game_challenge_complete_loss
            AFTER UPDATE OF CompletedAt ON DailyGame_challenge
            WHEN COALESCE(TRIM(OLD.CompletedAt), '') = ''
             AND COALESCE(TRIM(NEW.CompletedAt), '') <> ''
             AND NEW.IsWon = 0
            BEGIN
                UPDATE DailyGameChallengeStats
                SET
                    GamesPlayed = GamesPlayed + 1,
                    LastPlayedAt = NEW.CompletedAt,
                    LastLostAt = substr(CAST(NEW.CompletedAt AS TEXT), 1, 10),
                    CurrentStreak = 0,
                    CurrentStreakStartedAt = NULL
                WHERE Id = 1;
            END;
        ");
    }

    private static void CreateDailyGameChallengeFirstRowTrigger()
    {
        _db.Execute("DROP TRIGGER IF EXISTS trg_daily_game_challenge_first_row;");

        _db.Execute(@"
            CREATE TRIGGER trg_daily_game_challenge_first_row
            AFTER INSERT ON DailyGame_challenge
            WHEN (SELECT COUNT(*) FROM DailyGame_challenge) = 1
            BEGIN
                UPDATE DailyGameChallengeStats
                SET FirstStartedAt = date('now', 'localtime')
                WHERE Id = 1
                  AND FirstStartedAt IS NULL;
            END;
        ");
    }

    private static int GetDatabaseVersion()
    {
        return _db.ExecuteScalar<int>("PRAGMA user_version;");
    }

    private static void SetDatabaseVersion(int version)
    {
        _db.Execute($"PRAGMA user_version = {version};");
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
            if (_db == null)
                Init();

            if (_db == null)
            {
                Debug.LogError("[GameDatabase] GetGameStats skipped because the database could not be initialized.");
                return null;
            }

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
    public static int GetTotalPossiblePoints(GameStats stats)
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
