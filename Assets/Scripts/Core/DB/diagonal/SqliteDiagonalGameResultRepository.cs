using System.Collections.Generic;
using System.Linq;
using SQLite;
using UnityEngine;

public sealed class SqliteDiagonalGameResultRepository : IDiagonalGameResultRepository
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

    private readonly SqliteDatabaseConnection _database;
    private SQLiteConnection Db => _database.Connection;

    public SqliteDiagonalGameResultRepository() : this(SqliteDatabaseConnection.Instance) { }

    public SqliteDiagonalGameResultRepository(SqliteDatabaseConnection database)
    {
        _database = database ?? throw new System.ArgumentNullException(nameof(database));
    }

    public static void InitializeSchema(SqliteDatabaseConnection database)
    {
        if (database == null) throw new System.ArgumentNullException(nameof(database));
        new SqliteDiagonalGameResultRepository(database).InitializeSchema();
    }

    private void InitializeSchema()
    {
        try
        {
            SQLiteConnection db = Db;
            if (db == null)
            {
                Debug.LogError("[SqliteDiagonalGameResultRepository] Init skipped because the shared database connection is not open.");
                return;
            }

            db.CreateTable<DiagonalNewGameResult>();
            db.CreateTable<DiagonalNewGameResultCompletion>();
            db.CreateTable<DiagonalNewGameDifficultyStats>();
            EnsureUsageCountColumns();
            RecreateStatsTriggers();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[SqliteDiagonalGameResultRepository] Init failed: " + ex);
        }
    }

    public void RecordAttempt(DiagonalNewGameResult result, bool completed)
    {
        SQLiteConnection db = Db;
        if (result == null || db == null) return;

        db.BeginTransaction();
        try
        {
            db.Insert(result);
            if (completed)
                db.Insert(new DiagonalNewGameResultCompletion { ResultId = result.Id });
            db.Commit();
        }
        catch (System.Exception ex)
        {
            db.Rollback();
            Debug.LogError("[SqliteDiagonalGameResultRepository] RecordAttempt failed: " + ex);
        }
    }


    public void UpdateCompletedAttempt(DiagonalNewGameResult result)
    {
        SQLiteConnection db = Db;
        if (result == null || result.Id <= 0 || db == null) return;

        db.BeginTransaction();
        try
        {
            DiagonalNewGameResult existing = db.Find<DiagonalNewGameResult>(result.Id);
            if (existing == null)
            {
                db.Rollback();
                Debug.LogWarning("[SqliteDiagonalGameResultRepository] Retake result no longer exists: " + result.Id);
                return;
            }

            // Preserve the original row and puzzle identity; replace usage stats.
            existing.PencilUses = result.PencilUses;
            existing.SOSUses = result.SOSUses;
            existing.AutoFillUses = result.AutoFillUses;
            existing.RecordedAt = result.RecordedAt;
            db.Update(existing);
            // The completion trigger increments wins only on the first success.
            db.Execute("INSERT OR IGNORE INTO diagonal_new_game_result_completions (ResultId) VALUES (?);", existing.Id);
            db.Commit();
        }
        catch (System.Exception ex)
        {
            db.Rollback();
            Debug.LogError("[SqliteDiagonalGameResultRepository] UpdateCompletedAttempt failed: " + ex);
        }
    }

    private void EnsureUsageCountColumns()
    {
        var columns = new System.Collections.Generic.HashSet<string>(
            Db.GetTableInfo("diagonal_new_game_results").Select(column => column.Name));

        if (!columns.Contains(nameof(DiagonalNewGameResult.PencilUses)))
            Db.Execute("ALTER TABLE diagonal_new_game_results ADD COLUMN PencilUses INTEGER NOT NULL DEFAULT 0;");
        if (!columns.Contains(nameof(DiagonalNewGameResult.SOSUses)))
            Db.Execute("ALTER TABLE diagonal_new_game_results ADD COLUMN SOSUses INTEGER NOT NULL DEFAULT 0;");
        if (!columns.Contains(nameof(DiagonalNewGameResult.AutoFillUses)))
            Db.Execute("ALTER TABLE diagonal_new_game_results ADD COLUMN AutoFillUses INTEGER NOT NULL DEFAULT 0;");
    }

    private void RecreateStatsTriggers()
    {
        Db.Execute("DROP TRIGGER IF EXISTS trg_diagonal_new_game_results_insert;");
        Db.Execute("DROP TRIGGER IF EXISTS trg_diagonal_new_game_result_completions_insert;");

        Db.Execute(@"
            CREATE TRIGGER trg_diagonal_new_game_results_insert
            AFTER INSERT ON diagonal_new_game_results
            BEGIN
                INSERT OR IGNORE INTO diagonal_new_game_stats_by_difficulty
                    (Difficulty, GamesStarted, GamesCompleted)
                VALUES
                    (NEW.Difficulty, 0, 0);

                UPDATE diagonal_new_game_stats_by_difficulty
                SET GamesStarted = GamesStarted + 1
                WHERE Difficulty = NEW.Difficulty;
            END;
        ");

        Db.Execute(@"
            CREATE TRIGGER trg_diagonal_new_game_result_completions_insert
            AFTER INSERT ON diagonal_new_game_result_completions
            BEGIN
                UPDATE diagonal_new_game_stats_by_difficulty
                SET GamesCompleted = GamesCompleted + 1
                WHERE Difficulty = (
                    SELECT Difficulty
                    FROM diagonal_new_game_results
                    WHERE Id = NEW.ResultId
                );
            END;
        ");
    }

    public int GetNextId()
    {
        if (Db == null) return 1;
        DiagonalNewGameResult latest = Db.Table<DiagonalNewGameResult>().OrderByDescending(result => result.Id).FirstOrDefault();
        return latest == null ? 1 : latest.Id + 1;
    }

    public DiagonalNewGameResult GetLatest() =>
        Db == null ? null : Db.Table<DiagonalNewGameResult>().OrderByDescending(r => r.Id).FirstOrDefault();

    public List<DiagonalNewGameResult> GetRecent(int count) =>
        Db == null ? new List<DiagonalNewGameResult>() : Db.Table<DiagonalNewGameResult>().OrderByDescending(r => r.Id).Take(count).ToList();

    public List<DiagonalNewGameResultEntry> GetRecentEntries(int offset, int count)
    {
        if (Db == null) return new List<DiagonalNewGameResultEntry>();
        List<RecentEntryRow> rows = Db.Query<RecentEntryRow>(
            "SELECT results.Id, results.Level, results.Difficulty, results.PuzzleSeed, " +
            "results.PencilUses, results.SOSUses, results.AutoFillUses, results.RecordedAt, " +
            "CASE WHEN completions.ResultId IS NULL THEN 0 ELSE 1 END AS IsCompleted " +
            "FROM diagonal_new_game_results AS results " +
            "LEFT JOIN diagonal_new_game_result_completions AS completions " +
            "ON completions.ResultId = results.Id " +
            "ORDER BY results.Id DESC LIMIT ? OFFSET ?", count, offset);

        return rows.Select(row => new DiagonalNewGameResultEntry(new DiagonalNewGameResult
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

    public List<DiagonalNewGameResultEntry> GetRecentEntries(int count) =>
        GetRecentEntries(0, count);

    public DiagonalNewGameResultStats GetStats()
    {
        var result = new DiagonalNewGameResultStats();
        SQLiteConnection db = Db;
        if (db == null) return result;
        foreach (DiagonalNewGameDifficultyStats stat in db.Table<DiagonalNewGameDifficultyStats>())
        {
            result.TotalGames += stat.GamesStarted;
            result.CompletedGames += stat.GamesCompleted;
        }
        result.IncompleteGames = System.Math.Max(0, result.TotalGames - result.CompletedGames);
        return result;
    }

    public DiagonalNewGameDifficultyStats GetDifficultyStats(SudokuDifficulty difficulty)
    {
        SQLiteConnection db = Db;
        if (db == null) return new DiagonalNewGameDifficultyStats { Difficulty = (int)difficulty };
        DiagonalNewGameDifficultyStats row = db.Find<DiagonalNewGameDifficultyStats>((int)difficulty);
        return row ?? new DiagonalNewGameDifficultyStats { Difficulty = (int)difficulty };
    }
}
