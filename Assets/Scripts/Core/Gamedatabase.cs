#if UNITY_WEBGL && !UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class GameDatabase
{
    private const string HistoryKey = "sudoku_game_history";

    [Serializable]
    private class StoredGameRecord
    {
        public int Id;
        public int Level;
        public int Difficulty;
        public float ElapsedSeconds;
        public int LivesRemaining;
        public int Points;
        public bool IsWon;
        public string CompletedAt;
        public int UndoUses;
        public int PencilUses;
        public int EraseUses;
        public int SOSUses;
        public int AutoFillUses;
    }

    [Serializable]
    private class HistoryStore
    {
        public int NextId = 1;

        public List<StoredGameRecord> Records = new List<StoredGameRecord>();
    }

    private static HistoryStore _store;

    // ============================================================
    // INITIALIZATION
    // ============================================================

    public static void Init()
    {
        LoadStore();
    }

    private static void LoadStore()
    {
        if (_store != null) return;

        string json = PlayerPrefs.GetString( HistoryKey, string.Empty );

        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                _store = JsonUtility.FromJson<HistoryStore>( json );
            }
            catch (Exception ex)
            {
                Debug.LogWarning( $"[GameDatabase] Could not load WebGL history: {ex.Message}" );
            }
        }

        if (_store == null) _store = new HistoryStore();
        if (_store.Records == null) _store.Records = new List<StoredGameRecord>();

        int highestId = 0;

        foreach (StoredGameRecord record in _store.Records)
            if (record.Id > highestId) highestId = record.Id;

        if (_store.NextId <= highestId) _store.NextId = highestId + 1;
    }

    private static void SaveStore()
    {
        LoadStore();

        string json = JsonUtility.ToJson(_store);

        PlayerPrefs.SetString( HistoryKey, json );
        PlayerPrefs.Save();
    }

    // ============================================================
    // RECORD CONVERSION
    // ============================================================

    private static StoredGameRecord ToStored( GameRecord record )
    {
        return new StoredGameRecord
        {
            Id = record.Id,
            Level = record.Level,
            Difficulty = record.Difficulty,
            ElapsedSeconds = record.ElapsedSeconds,
            LivesRemaining = record.LivesRemaining,
            Points = record.Points,
            IsWon = record.IsWon,
            CompletedAt = record.CompletedAt,
            UndoUses = record.UndoUses,
            PencilUses = record.PencilUses,
            EraseUses = record.EraseUses,
            SOSUses = record.SOSUses,
            AutoFillUses = record.AutoFillUses
        };
    }

    private static GameRecord ToGameRecord( StoredGameRecord record )
    {
        return new GameRecord
        {
            Id = record.Id,
            Level = record.Level,
            Difficulty = record.Difficulty,
            ElapsedSeconds = record.ElapsedSeconds,
            LivesRemaining = record.LivesRemaining,
            Points = record.Points,
            IsWon = record.IsWon,
            CompletedAt = record.CompletedAt,
            UndoUses = record.UndoUses,
            PencilUses = record.PencilUses,
            EraseUses = record.EraseUses,
            SOSUses = record.SOSUses,
            AutoFillUses = record.AutoFillUses
        };
    }

    // ============================================================
    // WRITE
    // ============================================================

    public static void Insert( GameRecord record )
    {
        if (record == null) return;

        LoadStore();
        record.Id = _store.NextId++;
        _store.Records.Add( ToStored(record) );
        SaveStore();
    }

    public static void Update( GameRecord record )
    {
        if (record == null) return;

        LoadStore();

        int index = _store.Records.FindIndex( r => r.Id == record.Id );

        if (index < 0)
            return;

        _store.Records[index] = ToStored(record);

        SaveStore();
    }

    // ============================================================
    // HISTORY READS
    // ============================================================

    public static List<GameRecord> GetAll()
    {
        LoadStore();

        return _store.Records
            .OrderBy(r => r.Id)
            .Select(ToGameRecord)
            .ToList();
    }

    public static List<GameRecord> GetNextSet( int offset )
    {
        LoadStore();

        return _store.Records
            .OrderByDescending(r => r.Id)
            .Skip(offset)
            .Take(10)
            .Select(ToGameRecord)
            .ToList();
    }

    public static List<GameRecord> GetLastNRecordByDate( int number )
    {
        LoadStore();

        return _store.Records
            .OrderByDescending(r => r.Id)
            .Take(number)
            .Select(ToGameRecord)
            .ToList();
    }

    public static GameRecord GetLastRecord()
    {
        LoadStore();

        StoredGameRecord record = _store.Records
                .OrderByDescending(r => r.Id)
                .FirstOrDefault();

        return record == null ? null : ToGameRecord(record);
    }

    public static GameRecord GetFastestWin()
    {
        LoadStore();

        StoredGameRecord record = _store.Records
                .Where(r => r.IsWon)
                .OrderBy(r => r.ElapsedSeconds)
                .FirstOrDefault();

        return record == null ? null : ToGameRecord(record);
    }

    // ============================================================
    // STATS
    // ============================================================

    public static GameStats GetGameStats()
    {
        LoadStore();

        GameStats stats = new GameStats
                            {
                                Id = 1,
                                AggregateVersion = 1
                            };

        foreach ( StoredGameRecord record in _store.Records )
        {
            stats.TotalGames++;
            stats.TotalPoints += record.Points;

            if (record.IsWon)
            {
                stats.TotalWins++;

                if ( stats.FastestWinSeconds == null || record.ElapsedSeconds < stats.FastestWinSeconds.Value )
                {
                    stats.FastestWinSeconds = record.ElapsedSeconds;
                }
            }

            switch ( (SudokuDifficulty) record.Difficulty )
            {
                case SudokuDifficulty.Simple:
                    stats.SimpleCount++;
                    break;

                case SudokuDifficulty.Beginner:
                    stats.BeginnerCount++;
                    break;

                case SudokuDifficulty.Easy:
                    stats.EasyCount++;
                    break;

                case SudokuDifficulty.Novice:
                    stats.NoviceCount++;
                    break;

                case SudokuDifficulty.Moderate:
                    stats.ModerateCount++;
                    break;

                case SudokuDifficulty.Advanced:
                    stats.AdvancedCount++;
                    break;

                case SudokuDifficulty.Hard:
                    stats.HardCount++;
                    break;

                case SudokuDifficulty.Expert:
                    stats.ExpertCount++;
                    break;

                case SudokuDifficulty.Hardest:
                    stats.HardestCount++;
                    break;
            }
        }

        foreach ( StoredGameRecord record in _store.Records.OrderByDescending(r => r.Id))
        {
            if (!record.IsWon)
                break;

            stats.CurrentStreak++;
        }

        return stats;
    }

    public static int GetTotalPossiblePoints( GameStats stats )
    {
        if (stats == null) return 0;

        return stats.SimpleCount * ScoringSystem .GetAbsoluteMaximumScore( SudokuDifficulty.Simple ) + 
               stats.BeginnerCount * ScoringSystem.GetAbsoluteMaximumScore( SudokuDifficulty.Beginner ) +
               stats.EasyCount * ScoringSystem.GetAbsoluteMaximumScore( SudokuDifficulty.Easy ) + 
               stats.NoviceCount * ScoringSystem.GetAbsoluteMaximumScore( SudokuDifficulty.Novice ) + 
               stats.ModerateCount * ScoringSystem.GetAbsoluteMaximumScore( SudokuDifficulty.Moderate ) + 
               stats.AdvancedCount * ScoringSystem.GetAbsoluteMaximumScore( SudokuDifficulty.Advanced ) + 
               stats.HardCount * ScoringSystem.GetAbsoluteMaximumScore( SudokuDifficulty.Hard ) + 
               stats.ExpertCount * ScoringSystem.GetAbsoluteMaximumScore( SudokuDifficulty.Expert ) + 
               stats.HardestCount * ScoringSystem.GetAbsoluteMaximumScore( SudokuDifficulty.Hardest );
    }

    public static int GetTotalPoints()
    {
        return GetGameStats().TotalPoints;
    }

    public static int GetTotalWins()
    {
        return GetGameStats().TotalWins;
    }

    public static int GetTotalGamesPlayed()
    {
        return GetGameStats().TotalGames;
    }

    public static void RebuildGameStats()
    {
        //
        // No aggregate database exists on WebGL.
        // Stats are calculated from history on demand.
        //
    }

    public static void LogGameStats()
    {
        GameStats stats = GetGameStats();

        Debug.Log(
            "GAME STATS\n" +
            $"Games={stats.TotalGames}\n" +
            $"Wins={stats.TotalWins}\n" +
            $"Points={stats.TotalPoints}\n" +
            $"Fastest={stats.FastestWinSeconds}\n" +
            $"Streak={stats.CurrentStreak}"
        );
    }
    // ============================================================
    // CLOUD HISTORY
    // ============================================================

    public static List<SaveGameRecord> ExportHistory()
    {
        LoadStore();

        return _store.Records
            .OrderBy(r => r.Id)
            .Select(
                r => new SaveGameRecord
                {
                    Id = r.Id,
                    Level = r.Level,
                    Difficulty = r.Difficulty,
                    ElapsedSeconds = r.ElapsedSeconds,
                    LivesRemaining = r.LivesRemaining,
                    Points = r.Points,
                    IsWon = r.IsWon,
                    CompletedAt = r.CompletedAt,

                    UndoUses = r.UndoUses,
                    PencilUses = r.PencilUses,
                    EraseUses = r.EraseUses,
                    SOSUses = r.SOSUses,
                    AutoFillUses = r.AutoFillUses
                }
            )
            .ToList();
    }

    public static void ImportHistory( List<SaveGameRecord> history )
    {
        LoadStore();

        //
        // YouTube cloud is the source of truth.
        //
        _store.Records.Clear();

        int highestId = 0;

        if (history != null)
        {
            foreach ( SaveGameRecord record in history )
            {
                if (record == null) continue;

                _store.Records.Add(
                    new StoredGameRecord
                    {
                        Id = record.Id,
                        Level = record.Level,
                        Difficulty = record.Difficulty,
                        ElapsedSeconds = record.ElapsedSeconds,
                        LivesRemaining = record.LivesRemaining,
                        Points = record.Points,
                        IsWon = record.IsWon,
                        CompletedAt = record.CompletedAt,

                        UndoUses = record.UndoUses,
                        PencilUses = record.PencilUses,
                        EraseUses = record.EraseUses,
                        SOSUses = record.SOSUses,
                        AutoFillUses = record.AutoFillUses
                    }
                );

                if (record.Id > highestId)
                {
                    highestId = record.Id;
                }
            }
        }

        //
        // Restore our SQLite-like AutoIncrement counter.
        //
        _store.NextId = Mathf.Max( 1, highestId + 1 );

        //
        // Cache the cloud state locally too.
        //
        SaveStore();
    }
    
    public static void FlushToDisk()
    {
        PlayerPrefs.Save();
    }

    public static void Close()
    {
        PlayerPrefs.Save();
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
    private const int CurrentDbVersion = 1;

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

            _db.CreateTable<GameStats>();

            CreateIndexes();

            InitializeGameStats();

            RecreateGameStatsTriggers();
        }
        catch (Exception ex)
        {
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

        Debug.Log(
            $"[GameDatabase] Database ready at version " +
            $"{version}.");
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