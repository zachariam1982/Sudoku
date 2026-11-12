using System;
using System.Collections.Generic;
using System.Globalization;
using SQLite;

/// <summary>Query and update operations for tables owned by DailyChallengeDatabase.</summary>
public sealed class SqliteDailyChallengeRepository : IDailyChallengeRepository
{
    private readonly SqliteDatabaseConnection _database;
    private SQLiteConnection Db => _database.Connection;

    public SqliteDailyChallengeRepository() : this(SqliteDatabaseConnection.Instance) { }

    public SqliteDailyChallengeRepository(SqliteDatabaseConnection database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    public DailyChallengeRecord GetOrCreate(DateTime date, int difficulty)
    {
        DateTime day=date.Date;
        string dateKey=day.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
        List<DailyChallengeRecord> rows=Db.Query<DailyChallengeRecord>(
            "SELECT * FROM DailyGame_challenge WHERE CurrentDate = ? ORDER BY Id DESC LIMIT 1",dateKey);
        if(rows.Count>0) return rows[0];
        var created=NewRecord(day,difficulty);
        Db.Insert(created);
        return created;
    }

    public int GetTotalChallengeCount()
    {
        return Db.ExecuteScalar<int>("SELECT COUNT(*) FROM DailyGame_challenge;");
    }

    public DailyChallengeStatsSnapshot GetStatsSnapshot()
    {
        List<DailyChallengeStatsSnapshot> rows = Db.Query<DailyChallengeStatsSnapshot>(
            "SELECT CurrentStreak, COALESCE(BestTimeSeconds, -1) AS BestTimeSeconds, " +
            "GamesPlayed, GamesWon, BestTimeGameId, SimpleBestTimeGameId, " +
            "BeginnerBestTimeGameId, EasyBestTimeGameId, NoviceBestTimeGameId, " +
            "ModerateBestTimeGameId, AdvancedBestTimeGameId, HardBestTimeGameId, " +
            "ExpertBestTimeGameId, HardestBestTimeGameId " +
            "FROM DailyGameChallengeStats WHERE Id = 1 LIMIT 1");
        return rows.Count == 0 ? new DailyChallengeStatsSnapshot() : rows[0];
    }

    public List<DailyChallengeRecord> GetAllRecords()
    {
        return Db.Query<DailyChallengeRecord>(
            "SELECT * FROM DailyGame_challenge ORDER BY CurrentDate DESC, Id DESC");
    }

    public List<DailyChallengeCalendarEntry> GetCalendarEntries() =>
        DailyChallengeCalendarQueries.GetEntries(Db);

    public DateTime? GetFirstChallengeDate() =>
        DailyChallengeCalendarQueries.GetFirstChallengeDate(Db);

    public DailyChallengeRecord GetBestOverallRecord(int? preferredGameId)
    {
        List<DailyChallengeRecord> rows = Db.Query<DailyChallengeRecord>(
            "SELECT * FROM DailyGame_challenge WHERE IsWon = 1 " +
            "ORDER BY CASE WHEN Id = ? THEN 0 ELSE 1 END, ElapsedSeconds, Id LIMIT 1",
            preferredGameId ?? -1);
        return rows.Count == 0 ? null : rows[0];
    }

    public List<DailyChallengeRecord> GetBestTimeRecordsByDifficulty(DailyChallengeStatsSnapshot stats)
    {
        if (stats == null) return new List<DailyChallengeRecord>();
        int?[] ids = stats.GetDifficultyBestGameIds();
        List<object> args = new List<object>(ids.Length);
        foreach (int? id in ids) if (id.HasValue) args.Add(id.Value);
        if (args.Count == 0) return new List<DailyChallengeRecord>();
        string[] placeholders = new string[args.Count];
        for (int i = 0; i < placeholders.Length; i++) placeholders[i] = "?";
        return Db.Query<DailyChallengeRecord>(
            "SELECT * FROM DailyGame_challenge WHERE IsWon = 1 AND Id IN (" +
            string.Join(",", placeholders) + ")", args.ToArray());
    }

    public List<DateTime> GetWinningChallengeDates()
    {
        List<DailyChallengeDateRow> rows = Db.Query<DailyChallengeDateRow>(
            "SELECT CurrentDate FROM DailyGame_challenge WHERE IsWon = 1 " +
            "GROUP BY CurrentDate ORDER BY CurrentDate");
        List<DateTime> dates = new List<DateTime>(rows.Count);
        foreach (DailyChallengeDateRow row in rows)
            if (DateTime.TryParse(row.CurrentDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                dates.Add(date.Date);
        return dates;
    }

    public void MarkAttempted(DailyChallengeRecord record)
    {
        if(record==null)return;
        record.IsAttempted=true;
        record.Started=true;
        Update(record);
    }

    public void Update(DailyChallengeRecord record)
    {
        if(record==null)return;
        Db.Update(record);
    }

    public void FinalizeUncompletedPreviousChallenges(DateTime today)
    {
        DateTime day=today.Date;
        string dateKey=day.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
        List<DailyChallengeRecord> rows=Db.Query<DailyChallengeRecord>(
            "SELECT * FROM DailyGame_challenge WHERE CurrentDate < ? AND (IsAttempted = 1 OR Started = 1) AND (CompletedAt IS NULL OR CompletedAt = '') ORDER BY CurrentDate, Id",dateKey);
        foreach(DailyChallengeRecord previous in rows)
        {
            DateTime endOfRowDay=DateTime.SpecifyKind(
                previous.CurrentDate.Date.AddDays(1).AddSeconds(-1),DateTimeKind.Local);
            previous.CompletedAt=endOfRowDay.ToString("o",CultureInfo.InvariantCulture);
            previous.IsWon=false;
            previous.Stars=0;
            Update(previous);
        }
    }

    public void DeleteUnattemptedBefore(DateTime today)
    {
        DateTime day=today.Date;
        string dateKey=day.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
        Db.Execute("DELETE FROM DailyGame_challenge WHERE IsAttempted = 0 AND CurrentDate < ?",dateKey);
    }

    public int GetPuzzleSeed(DateTime date)
    {
        return int.Parse(date.ToString("yyyyMMdd",CultureInfo.InvariantCulture),CultureInfo.InvariantCulture);
    }


    private static DailyChallengeRecord NewRecord(DateTime date,int difficulty)
    {
        return new DailyChallengeRecord { CurrentDate=date, Difficulty=difficulty, IsAttempted=false, Started=false, LivesRemaining=3 };
    }
}

/// <summary>Minimal cross-variant daily completion record used by the streak calendar.</summary>
public sealed class DailyChallengeCalendarEntry
{
    [Column("CurrentDate")] public string CurrentDateText { get; set; }
    public bool IsWon { get; set; }
    public string CompletedAt { get; set; }
}

/// <summary>Reads daily completion dates from whichever variant tables exist in the shared database.</summary>
public static class DailyChallengeCalendarQueries
{
    private static readonly string[] ChallengeTables =
    {
        "DailyGame_challenge",
        "diagonal_daily_game_challenge",
        "hyper_daily_game_challenge",
        "killer_daily_game_challenge",
        "thermo_daily_game_challenge",
        "irregular_daily_game_challenge"
    };

    private static readonly string[] StatsTables =
    {
        "DailyGameChallengeStats",
        "diagonal_daily_game_challenge_stats",
        "hyper_daily_game_challenge_stats",
        "killer_daily_game_challenge_stats",
        "thermo_daily_game_challenge_stats",
        "irregular_daily_game_challenge_stats"
    };

    public static List<DailyChallengeCalendarEntry> GetEntries(SQLiteConnection database)
    {
        var entries = new List<DailyChallengeCalendarEntry>();
        if (database == null) return entries;

        foreach (string table in ChallengeTables)
        {
            int tableExists = database.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = ?", table);
            if (tableExists == 0) continue;

            entries.AddRange(database.Query<DailyChallengeCalendarEntry>(
                "SELECT CurrentDate, IsWon, CompletedAt FROM " + table + " ORDER BY CurrentDate"));
        }

        return entries;
    }

    public static DateTime? GetFirstChallengeDate(SQLiteConnection database)
    {
        if (database == null) return null;

        DateTime? firstDate = null;
        foreach (string table in StatsTables)
        {
            int tableExists = database.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = ?", table);
            if (tableExists == 0) continue;

            string storedDate = database.ExecuteScalar<string>(
                "SELECT FirstStartedAt FROM " + table + " WHERE Id = 1 LIMIT 1");
            if (!DateTime.TryParse(storedDate, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal, out DateTime parsedDate))
                continue;

            DateTime date = parsedDate.Date;
            if (!firstDate.HasValue || date < firstDate.Value)
                firstDate = date;
        }

        return firstDate;
    }
}
