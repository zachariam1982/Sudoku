using System.Collections.Generic;
using System.Linq;
using SQLite;
using UnityEngine;

public static class NewGameResultDatabase
{
    public sealed class RecentEntryRow
    {
        public int Id { get; set; }
        public int Level { get; set; }
        public int Difficulty { get; set; }
        public int PuzzleSeed { get; set; }
        public int PencilUses { get; set; }
        public int SOSUses { get; set; }
        public int AutoFillUses { get; set; }
        public string RecordedAt { get; set; }
        public int IsCompleted { get; set; }
    }

    // New Game uses the same database file and lifecycle as GameDatabase.
    // Resolve the connection dynamically so a pause/resume reopen cannot leave
    // this repository holding a stale SQLiteConnection instance.
    private static SQLiteConnection Db => GameDatabase.Connection;

    public static void Init()
    {
        try
        {
            SQLiteConnection db = Db;
            if (db == null)
            {
                Debug.LogError("[NewGameResultDatabase] Init skipped because GameDatabase is not open.");
                return;
            }

            db.CreateTable<NewGameResult>();
            db.CreateTable<NewGameResultCompletion>();
            db.CreateTable<NewGameDifficultyStats>();
            EnsureUsageCountColumns();
            RecreateStatsTriggers();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[NewGameResultDatabase] Init failed: " + ex);
        }
    }

    public static void RecordAttempt(NewGameResult result, bool completed)
    {
        SQLiteConnection db = Db;
        if (result == null || db == null) return;

        db.BeginTransaction();
        try
        {
            db.Insert(result);
            if (completed)
                db.Insert(new NewGameResultCompletion { ResultId = result.Id });
            db.Commit();
        }
        catch (System.Exception ex)
        {
            db.Rollback();
            Debug.LogError("[NewGameResultDatabase] RecordAttempt failed: " + ex);
        }
    }


    public static void UpdateCompletedAttempt(NewGameResult result)
    {
        SQLiteConnection db = Db;
        if (result == null || result.Id <= 0 || db == null) return;

        db.BeginTransaction();
        try
        {
            NewGameResult existing = db.Find<NewGameResult>(result.Id);
            if (existing == null)
            {
                db.Rollback();
                Debug.LogWarning("[NewGameResultDatabase] Retake result no longer exists: " + result.Id);
                return;
            }

            // Preserve the original row and puzzle identity; replace usage stats.
            existing.PencilUses = result.PencilUses;
            existing.SOSUses = result.SOSUses;
            existing.AutoFillUses = result.AutoFillUses;
            existing.RecordedAt = result.RecordedAt;
            db.Update(existing);
            // The completion trigger increments wins only on the first success.
            db.Execute("INSERT OR IGNORE INTO new_game_result_completions (ResultId) VALUES (?);", existing.Id);
            db.Commit();
        }
        catch (System.Exception ex)
        {
            db.Rollback();
            Debug.LogError("[NewGameResultDatabase] UpdateCompletedAttempt failed: " + ex);
        }
    }

    private static void EnsureUsageCountColumns()
    {
        var columns = new System.Collections.Generic.HashSet<string>(
            Db.GetTableInfo("new_game_results").Select(column => column.Name));

        if (!columns.Contains(nameof(NewGameResult.PencilUses)))
            Db.Execute("ALTER TABLE new_game_results ADD COLUMN PencilUses INTEGER NOT NULL DEFAULT 0;");
        if (!columns.Contains(nameof(NewGameResult.SOSUses)))
            Db.Execute("ALTER TABLE new_game_results ADD COLUMN SOSUses INTEGER NOT NULL DEFAULT 0;");
        if (!columns.Contains(nameof(NewGameResult.AutoFillUses)))
            Db.Execute("ALTER TABLE new_game_results ADD COLUMN AutoFillUses INTEGER NOT NULL DEFAULT 0;");
    }

    private static void RecreateStatsTriggers()
    {
        Db.Execute("DROP TRIGGER IF EXISTS trg_new_game_results_insert;");
        Db.Execute("DROP TRIGGER IF EXISTS trg_new_game_result_completions_insert;");

        Db.Execute(@"
            CREATE TRIGGER trg_new_game_results_insert
            AFTER INSERT ON new_game_results
            BEGIN
                INSERT OR IGNORE INTO new_game_stats_by_difficulty
                    (Difficulty, GamesStarted, GamesCompleted)
                VALUES
                    (NEW.Difficulty, 0, 0);

                UPDATE new_game_stats_by_difficulty
                SET GamesStarted = GamesStarted + 1
                WHERE Difficulty = NEW.Difficulty;
            END;
        ");

        Db.Execute(@"
            CREATE TRIGGER trg_new_game_result_completions_insert
            AFTER INSERT ON new_game_result_completions
            BEGIN
                UPDATE new_game_stats_by_difficulty
                SET GamesCompleted = GamesCompleted + 1
                WHERE Difficulty = (
                    SELECT Difficulty
                    FROM new_game_results
                    WHERE Id = NEW.ResultId
                );
            END;
        ");
    }

    public static int GetNextId()
    {
        if (Db == null) return 1;
        NewGameResult latest = Db.Table<NewGameResult>().OrderByDescending(result => result.Id).FirstOrDefault();
        return latest == null ? 1 : latest.Id + 1;
    }

    public static NewGameResult GetLatest() =>
        Db == null ? null : Db.Table<NewGameResult>().OrderByDescending(r => r.Id).FirstOrDefault();

    public static List<NewGameResult> GetRecent(int count) =>
        Db == null ? new List<NewGameResult>() : Db.Table<NewGameResult>().OrderByDescending(r => r.Id).Take(count).ToList();

    public static List<NewGameResultEntry> GetRecentEntries(int offset, int count)
    {
        if (Db == null) return new List<NewGameResultEntry>();
        List<RecentEntryRow> rows = Db.Query<RecentEntryRow>(
            "SELECT results.Id, results.Level, results.Difficulty, results.PuzzleSeed, " +
            "results.PencilUses, results.SOSUses, results.AutoFillUses, results.RecordedAt, " +
            "CASE WHEN completions.ResultId IS NULL THEN 0 ELSE 1 END AS IsCompleted " +
            "FROM new_game_results AS results " +
            "LEFT JOIN new_game_result_completions AS completions " +
            "ON completions.ResultId = results.Id " +
            "ORDER BY results.Id DESC LIMIT ? OFFSET ?", count, offset);

        return rows.Select(row => new NewGameResultEntry(new NewGameResult
        {
            Id = row.Id,
            Level = row.Level,
            Difficulty = row.Difficulty,
            PuzzleSeed = row.PuzzleSeed,
            PencilUses = row.PencilUses,
            SOSUses = row.SOSUses,
            AutoFillUses = row.AutoFillUses,
            RecordedAt = row.RecordedAt
        }, row.IsCompleted != 0)).ToList();
    }

    public static List<NewGameResultEntry> GetRecentEntries(int count) =>
        GetRecentEntries(0, count);

    public static NewGameResultStats GetStats()
    {
        var result = new NewGameResultStats();
        SQLiteConnection db = Db;
        if (db == null) return result;
        foreach (NewGameDifficultyStats stat in db.Table<NewGameDifficultyStats>())
        {
            result.TotalGames += stat.GamesStarted;
            result.CompletedGames += stat.GamesCompleted;
        }
        result.IncompleteGames = System.Math.Max(0, result.TotalGames - result.CompletedGames);
        return result;
    }

    public static NewGameDifficultyStats GetDifficultyStats(SudokuDifficulty difficulty)
    {
        SQLiteConnection db = Db;
        if (db == null) return new NewGameDifficultyStats { Difficulty = (int)difficulty };
        NewGameDifficultyStats row = db.Find<NewGameDifficultyStats>((int)difficulty);
        return row ?? new NewGameDifficultyStats { Difficulty = (int)difficulty };
    }
}
