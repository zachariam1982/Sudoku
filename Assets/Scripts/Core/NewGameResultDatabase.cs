#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class NewGameResultDatabase
{
    private const string ResultsKey = "sudoku_new_game_results";
    private const string StatsKey = "sudoku_new_game_difficulty_stats";
    private const string CompletionsKey = "sudoku_new_game_result_completions";

    [Serializable] private class ResultsStore { public List<ResultRow> Rows = new List<ResultRow>(); }
    [Serializable] private class ResultRow
    {
        public int Id, Level, Difficulty, PuzzleSeed;
        public int PencilUses, SOSUses, AutoFillUses;
        public string RecordedAt;
    }
    [Serializable] private class CompletionStore { public List<int> ResultIds = new List<int>(); }
    [Serializable] private class StatsStore { public List<StatsRow> Rows = new List<StatsRow>(); }
    [Serializable] private class StatsRow { public int Difficulty, GamesStarted, GamesCompleted; }

    private static ResultsStore _results;
    private static StatsStore _stats;
    private static CompletionStore _completions;
    public static void Init() { Load(); }

    private static void Load()
    {
        if (_results == null)
        {
            string json = PlayerPrefs.GetString(ResultsKey, string.Empty);
            try { _results = string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<ResultsStore>(json); }
            catch (Exception ex) { Debug.LogWarning("[NewGameResultDatabase] Results load failed: " + ex.Message); }
            if (_results == null) _results = new ResultsStore();
            if (_results.Rows == null) _results.Rows = new List<ResultRow>();
        }
        if (_completions == null)
        {
            string json = PlayerPrefs.GetString(CompletionsKey, string.Empty);
            try { _completions = string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<CompletionStore>(json); }
            catch (Exception ex) { Debug.LogWarning("[NewGameResultDatabase] Completions load failed: " + ex.Message); }
            if (_completions == null) _completions = new CompletionStore();
            if (_completions.ResultIds == null) _completions.ResultIds = new List<int>();
        }
        if (_stats == null)
        {
            string json = PlayerPrefs.GetString(StatsKey, string.Empty);
            try { _stats = string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<StatsStore>(json); }
            catch (Exception ex) { Debug.LogWarning("[NewGameResultDatabase] Stats load failed: " + ex.Message); }
            if (_stats == null) _stats = new StatsStore();
            if (_stats.Rows == null) _stats.Rows = new List<StatsRow>();
        }
    }

    private static void Save()
    {
        Load();
        PlayerPrefs.SetString(ResultsKey, JsonUtility.ToJson(_results));
        PlayerPrefs.SetString(StatsKey, JsonUtility.ToJson(_stats));
        PlayerPrefs.SetString(CompletionsKey, JsonUtility.ToJson(_completions));
        PlayerPrefs.Save();
    }

    public static void RecordAttempt(NewGameResult result, bool completed)
    {
        if (result == null) return;
        Load();
        int nextId = _results.Rows.Count == 0 ? 1 : _results.Rows.Max(r => r.Id) + 1;
        result.Id = nextId;
        _results.Rows.Add(new ResultRow {
            Id = result.Id, Level = result.Level, Difficulty = result.Difficulty,
            PuzzleSeed = result.PuzzleSeed, PencilUses = result.PencilUses,
            SOSUses = result.SOSUses, AutoFillUses = result.AutoFillUses,
            RecordedAt = result.RecordedAt
        });
        if (completed && !_completions.ResultIds.Contains(result.Id))
            _completions.ResultIds.Add(result.Id);
        StatsRow stat = _stats.Rows.FirstOrDefault(r => r.Difficulty == result.Difficulty);
        if (stat == null) { stat = new StatsRow { Difficulty = result.Difficulty }; _stats.Rows.Add(stat); }
        stat.GamesStarted++;
        if (completed) stat.GamesCompleted++;
        Save();
    }

    public static NewGameResult GetLatest()
    {
        Load();
        ResultRow row = _results.Rows.OrderByDescending(r => r.Id).FirstOrDefault();
        return ToResult(row);
    }

    public static List<NewGameResult> GetRecent(int count)
    {
        Load();
        return _results.Rows.OrderByDescending(r => r.Id).Take(count).Select(ToResult).ToList();
    }

    public static List<NewGameResultEntry> GetRecentEntries(int offset, int count)
    {
        Load();
        return _results.Rows.OrderByDescending(r => r.Id).Skip(offset).Take(count)
            .Select(row => new NewGameResultEntry(ToResult(row), _completions.ResultIds.Contains(row.Id)))
            .ToList();
    }

    public static List<NewGameResultEntry> GetRecentEntries(int count) =>
        GetRecentEntries(0, count);

    public static NewGameResultStats GetStats()
    {
        Load();
        var result = new NewGameResultStats();
        foreach (StatsRow row in _stats.Rows)
        {
            result.TotalGames += row.GamesStarted;
            result.CompletedGames += row.GamesCompleted;
        }
        result.IncompleteGames = Math.Max(0, result.TotalGames - result.CompletedGames);
        return result;
    }

    public static NewGameDifficultyStats GetDifficultyStats(SudokuDifficulty difficulty)
    {
        Load();
        StatsRow row = _stats.Rows.FirstOrDefault(r => r.Difficulty == (int)difficulty);
        return new NewGameDifficultyStats {
            Difficulty = (int)difficulty,
            GamesStarted = row != null ? row.GamesStarted : 0,
            GamesCompleted = row != null ? row.GamesCompleted : 0
        };
    }

    private static NewGameResult ToResult(ResultRow row)
    {
        if (row == null) return null;
        return new NewGameResult {
            Id = row.Id, Level = row.Level, Difficulty = row.Difficulty,
            PuzzleSeed = row.PuzzleSeed, PencilUses = row.PencilUses,
            SOSUses = row.SOSUses, AutoFillUses = row.AutoFillUses,
            RecordedAt = row.RecordedAt
        };
    }
}
#else
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SQLite;
using UnityEngine;

public static class NewGameResultDatabase
{
    private static SQLiteConnection _db;
    private static string DbPath =>
        Path.Combine(Application.persistentDataPath, "game_history.db");

    public static void Init(SQLiteConnection connection = null)
    {
        try
        {
            _db = connection ?? new SQLiteConnection(DbPath);
            _db.CreateTable<NewGameResult>();
            _db.CreateTable<NewGameResultCompletion>();
            _db.CreateTable<NewGameDifficultyStats>();
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
        SQLiteConnection db = _db;
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


    private static void EnsureUsageCountColumns()
    {
        var columns = new System.Collections.Generic.HashSet<string>(
            _db.GetTableInfo("new_game_results").Select(column => column.Name));

        if (!columns.Contains(nameof(NewGameResult.PencilUses)))
            _db.Execute("ALTER TABLE new_game_results ADD COLUMN PencilUses INTEGER NOT NULL DEFAULT 0;");
        if (!columns.Contains(nameof(NewGameResult.SOSUses)))
            _db.Execute("ALTER TABLE new_game_results ADD COLUMN SOSUses INTEGER NOT NULL DEFAULT 0;");
        if (!columns.Contains(nameof(NewGameResult.AutoFillUses)))
            _db.Execute("ALTER TABLE new_game_results ADD COLUMN AutoFillUses INTEGER NOT NULL DEFAULT 0;");
    }

    private static void RecreateStatsTriggers()
    {
        _db.Execute("DROP TRIGGER IF EXISTS trg_new_game_results_insert;");
        _db.Execute("DROP TRIGGER IF EXISTS trg_new_game_result_completions_insert;");

        _db.Execute(@"
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

        _db.Execute(@"
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

    public static NewGameResult GetLatest() =>
        _db == null ? null : _db.Table<NewGameResult>().OrderByDescending(r => r.Id).FirstOrDefault();

    public static List<NewGameResult> GetRecent(int count) =>
        _db == null ? new List<NewGameResult>() : _db.Table<NewGameResult>().OrderByDescending(r => r.Id).Take(count).ToList();

    public static List<NewGameResultEntry> GetRecentEntries(int offset, int count)
    {
        if (_db == null) return new List<NewGameResultEntry>();
        List<NewGameResult> results = _db.Table<NewGameResult>()
            .OrderByDescending(result => result.Id)
            .Skip(offset)
            .Take(count)
            .ToList();
        var completedIds = new System.Collections.Generic.HashSet<int>(
            _db.Table<NewGameResultCompletion>().ToList().Select(completion => completion.ResultId));
        return results.Select(result => new NewGameResultEntry(result, completedIds.Contains(result.Id))).ToList();
    }

    public static List<NewGameResultEntry> GetRecentEntries(int count) =>
        GetRecentEntries(0, count);

    public static NewGameResultStats GetStats()
    {
        var result = new NewGameResultStats();
        SQLiteConnection db = _db;
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
        SQLiteConnection db = _db;
        if (db == null) return new NewGameDifficultyStats { Difficulty = (int)difficulty };
        NewGameDifficultyStats row = db.Find<NewGameDifficultyStats>((int)difficulty);
        return row ?? new NewGameDifficultyStats { Difficulty = (int)difficulty };
    }
}
#endif
