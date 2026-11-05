using System;
using SQLite;
using UnityEngine;

/// <summary>Owns the Daily Challenge tables and their statistics triggers.</summary>
public sealed class DiagonalDailyChallengeDatabase
{
    private readonly SqliteDatabaseConnection _database;
    private SQLiteConnection Db => _database.Connection;

    private DiagonalDailyChallengeDatabase(SqliteDatabaseConnection database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    public static void Initialize(SqliteDatabaseConnection database)
    {
        new DiagonalDailyChallengeDatabase(database).InitializeSchema();
    }

    private void InitializeSchema()
    {
        SQLiteConnection db = Db;
        if (db == null)
        {
            Debug.LogError("[DiagonalDailyChallengeDatabase] Init skipped because the shared database connection is not open.");
            return;
        }

        try
        {
            CreateDailyGameChallengeTable();
            CreateDailyGameChallengeStatsTable();
            CreateDailyGameChallengeCompletionTrigger();
            CreateDailyGameChallengeFirstRowTrigger();
        }
        catch (Exception ex)
        {
            Debug.LogError("[DiagonalDailyChallengeDatabase] Init failed: " + ex);
        }
    }

    private void CreateDailyGameChallengeTable()
    {
        Db.Execute(@"
            CREATE TABLE IF NOT EXISTS diagonal_daily_game_challenge
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

    private void CreateDailyGameChallengeStatsTable()
    {
        Db.Execute(@"
            CREATE TABLE IF NOT EXISTS diagonal_daily_game_challenge_stats
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

        Db.Execute(
            "INSERT OR IGNORE INTO diagonal_daily_game_challenge_stats (Id) VALUES (1);");
    }

    private void CreateDailyGameChallengeCompletionTrigger()
    {
        Db.Execute("DROP TRIGGER IF EXISTS trg_diagonal_daily_game_challenge_complete_win;");
        Db.Execute("DROP TRIGGER IF EXISTS trg_diagonal_daily_game_challenge_complete_loss;");

        Db.Execute(@"
            CREATE TRIGGER trg_diagonal_daily_game_challenge_complete_win
            AFTER UPDATE OF CompletedAt ON diagonal_daily_game_challenge
            WHEN COALESCE(TRIM(OLD.CompletedAt), '') = ''
             AND COALESCE(TRIM(NEW.CompletedAt), '') <> ''
             AND NEW.IsWon = 1
            BEGIN
                UPDATE diagonal_daily_game_challenge_stats
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
                             (SELECT ElapsedSeconds FROM diagonal_daily_game_challenge WHERE Id = (SELECT SimpleBestTimeGameId FROM diagonal_daily_game_challenge_stats WHERE Id = 1)),
                             1.0e99))
                        THEN NEW.Id ELSE SimpleBestTimeGameId
                    END,
                    BeginnerBestTimeGameId = CASE
                        WHEN NEW.Difficulty = 1
                         AND (BeginnerBestTimeGameId IS NULL OR NEW.ElapsedSeconds < COALESCE(
                             (SELECT ElapsedSeconds FROM diagonal_daily_game_challenge WHERE Id = (SELECT BeginnerBestTimeGameId FROM diagonal_daily_game_challenge_stats WHERE Id = 1)),
                             1.0e99))
                        THEN NEW.Id ELSE BeginnerBestTimeGameId
                    END,
                    EasyBestTimeGameId = CASE
                        WHEN NEW.Difficulty = 2
                         AND (EasyBestTimeGameId IS NULL OR NEW.ElapsedSeconds < COALESCE(
                             (SELECT ElapsedSeconds FROM diagonal_daily_game_challenge WHERE Id = (SELECT EasyBestTimeGameId FROM diagonal_daily_game_challenge_stats WHERE Id = 1)),
                             1.0e99))
                        THEN NEW.Id ELSE EasyBestTimeGameId
                    END,
                    NoviceBestTimeGameId = CASE
                        WHEN NEW.Difficulty = 3
                         AND (NoviceBestTimeGameId IS NULL OR NEW.ElapsedSeconds < COALESCE(
                             (SELECT ElapsedSeconds FROM diagonal_daily_game_challenge WHERE Id = (SELECT NoviceBestTimeGameId FROM diagonal_daily_game_challenge_stats WHERE Id = 1)),
                             1.0e99))
                        THEN NEW.Id ELSE NoviceBestTimeGameId
                    END,
                    ModerateBestTimeGameId = CASE
                        WHEN NEW.Difficulty = 4
                         AND (ModerateBestTimeGameId IS NULL OR NEW.ElapsedSeconds < COALESCE(
                             (SELECT ElapsedSeconds FROM diagonal_daily_game_challenge WHERE Id = (SELECT ModerateBestTimeGameId FROM diagonal_daily_game_challenge_stats WHERE Id = 1)),
                             1.0e99))
                        THEN NEW.Id ELSE ModerateBestTimeGameId
                    END,
                    AdvancedBestTimeGameId = CASE
                        WHEN NEW.Difficulty = 5
                         AND (AdvancedBestTimeGameId IS NULL OR NEW.ElapsedSeconds < COALESCE(
                             (SELECT ElapsedSeconds FROM diagonal_daily_game_challenge WHERE Id = (SELECT AdvancedBestTimeGameId FROM diagonal_daily_game_challenge_stats WHERE Id = 1)),
                             1.0e99))
                        THEN NEW.Id ELSE AdvancedBestTimeGameId
                    END,
                    HardBestTimeGameId = CASE
                        WHEN NEW.Difficulty = 6
                         AND (HardBestTimeGameId IS NULL OR NEW.ElapsedSeconds < COALESCE(
                             (SELECT ElapsedSeconds FROM diagonal_daily_game_challenge WHERE Id = (SELECT HardBestTimeGameId FROM diagonal_daily_game_challenge_stats WHERE Id = 1)),
                             1.0e99))
                        THEN NEW.Id ELSE HardBestTimeGameId
                    END,
                    ExpertBestTimeGameId = CASE
                        WHEN NEW.Difficulty = 7
                         AND (ExpertBestTimeGameId IS NULL OR NEW.ElapsedSeconds < COALESCE(
                             (SELECT ElapsedSeconds FROM diagonal_daily_game_challenge WHERE Id = (SELECT ExpertBestTimeGameId FROM diagonal_daily_game_challenge_stats WHERE Id = 1)),
                             1.0e99))
                        THEN NEW.Id ELSE ExpertBestTimeGameId
                    END,
                    HardestBestTimeGameId = CASE
                        WHEN NEW.Difficulty = 8
                         AND (HardestBestTimeGameId IS NULL OR NEW.ElapsedSeconds < COALESCE(
                             (SELECT ElapsedSeconds FROM diagonal_daily_game_challenge WHERE Id = (SELECT HardestBestTimeGameId FROM diagonal_daily_game_challenge_stats WHERE Id = 1)),
                             1.0e99))
                        THEN NEW.Id ELSE HardestBestTimeGameId
                    END
                WHERE Id = 1;
            END;
        ");

        Db.Execute(@"
            CREATE TRIGGER trg_diagonal_daily_game_challenge_complete_loss
            AFTER UPDATE OF CompletedAt ON diagonal_daily_game_challenge
            WHEN COALESCE(TRIM(OLD.CompletedAt), '') = ''
             AND COALESCE(TRIM(NEW.CompletedAt), '') <> ''
             AND NEW.IsWon = 0
            BEGIN
                UPDATE diagonal_daily_game_challenge_stats
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

    private void CreateDailyGameChallengeFirstRowTrigger()
    {
        Db.Execute("DROP TRIGGER IF EXISTS trg_diagonal_daily_game_challenge_first_row;");

        Db.Execute(@"
            CREATE TRIGGER trg_diagonal_daily_game_challenge_first_row
            AFTER INSERT ON diagonal_daily_game_challenge
            WHEN (SELECT COUNT(*) FROM diagonal_daily_game_challenge) = 1
            BEGIN
                UPDATE diagonal_daily_game_challenge_stats
                SET FirstStartedAt = date('now', 'localtime')
                WHERE Id = 1
                  AND FirstStartedAt IS NULL;
            END;
        ");
    }

}
