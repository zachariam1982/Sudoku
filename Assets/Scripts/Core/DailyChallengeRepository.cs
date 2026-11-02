using System;
using System.Collections.Generic;
using System.Globalization;
using SQLite;
using UnityEngine;

/// <summary>Persistence helpers for the existing daily challenge table. The table schema is owned by the game database.</summary>
public static class DailyChallengeRepository
{
    private static SQLiteConnection Db => GameDatabase.Connection;

    public static DailyChallengeRecord GetOrCreate(DateTime date, int difficulty)
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

    public static int GetTotalChallengeCount()
    {
        return Db.ExecuteScalar<int>("SELECT COUNT(*) FROM DailyGame_challenge;");
    }

    public static DailyChallengeStatsSnapshot GetStatsSnapshot()
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

    public static List<DailyChallengeRecord> GetAllRecords()
    {
        return Db.Query<DailyChallengeRecord>(
            "SELECT * FROM DailyGame_challenge ORDER BY CurrentDate DESC, Id DESC");
    }

    public static DailyChallengeRecord GetBestOverallRecord(int? preferredGameId)
    {
        List<DailyChallengeRecord> rows = Db.Query<DailyChallengeRecord>(
            "SELECT * FROM DailyGame_challenge WHERE IsWon = 1 " +
            "ORDER BY CASE WHEN Id = ? THEN 0 ELSE 1 END, ElapsedSeconds, Id LIMIT 1",
            preferredGameId ?? -1);
        return rows.Count == 0 ? null : rows[0];
    }

    public static List<DailyChallengeRecord> GetBestTimeRecordsByDifficulty(DailyChallengeStatsSnapshot stats)
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

    public static List<DateTime> GetWinningChallengeDates()
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

    public static void MarkAttempted(DailyChallengeRecord record)
    {
        if(record==null)return;
        record.IsAttempted=true;
        record.Started=true;
        Update(record);
    }

    public static void Update(DailyChallengeRecord record)
    {
        if(record==null)return;
        Db.Update(record);
    }

    public static void FinalizeUncompletedPreviousChallenges(DateTime today)
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

    public static void DeleteUnattemptedBefore(DateTime today)
    {
        DateTime day=today.Date;
        string dateKey=day.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
        Db.Execute("DELETE FROM DailyGame_challenge WHERE IsAttempted = 0 AND CurrentDate < ?",dateKey);
    }

    public static int GetPuzzleSeed(DateTime date)
    {
        return int.Parse(date.ToString("yyyyMMdd",CultureInfo.InvariantCulture),CultureInfo.InvariantCulture);
    }


    private static DailyChallengeRecord NewRecord(DateTime date,int difficulty)
    {
        return new DailyChallengeRecord { CurrentDate=date, Difficulty=difficulty, IsAttempted=false, Started=false, LivesRemaining=3 };
    }
}

/// <summary>Snapshot of the single-row daily challenge statistics table.</summary>
public sealed class DailyChallengeStatsSnapshot
{
    public int CurrentStreak { get; set; }
    public float BestTimeSeconds { get; set; } = -1f;
    public int GamesPlayed { get; set; }
    public int GamesWon { get; set; }
    public int? BestTimeGameId { get; set; }
    public int? SimpleBestTimeGameId { get; set; }
    public int? BeginnerBestTimeGameId { get; set; }
    public int? EasyBestTimeGameId { get; set; }
    public int? NoviceBestTimeGameId { get; set; }
    public int? ModerateBestTimeGameId { get; set; }
    public int? AdvancedBestTimeGameId { get; set; }
    public int? HardBestTimeGameId { get; set; }
    public int? ExpertBestTimeGameId { get; set; }
    public int? HardestBestTimeGameId { get; set; }

    public int?[] GetDifficultyBestGameIds() => new[]
    {
        SimpleBestTimeGameId, BeginnerBestTimeGameId, EasyBestTimeGameId,
        NoviceBestTimeGameId, ModerateBestTimeGameId, AdvancedBestTimeGameId,
        HardBestTimeGameId, ExpertBestTimeGameId, HardestBestTimeGameId
    };
}

internal sealed class DailyChallengeDateRow
{
    public string CurrentDate { get; set; }
}

/// <summary>SQLite mapping for a daily challenge row, including both lifecycle flags.</summary>
[Table("DailyGame_challenge")]
public sealed class DailyChallengeRecord
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Ignore] public DateTime CurrentDate
    {
        get
        {
            if (DateTime.TryParseExact(CurrentDateText, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateTime parsed))
                return parsed;

            return DateTime.Parse(CurrentDateText, CultureInfo.InvariantCulture).Date;
        }
        set { CurrentDateText = value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture); }
    }
    [NotNull, Column("CurrentDate")] public string CurrentDateText { get; set; }
    [NotNull] public int Difficulty { get; set; }
    [NotNull] public float ElapsedSeconds { get; set; }
    [NotNull] public int LivesRemaining { get; set; }
    [NotNull] public int Stars { get; set; }
    [NotNull] public bool IsWon { get; set; }
    [NotNull] public bool Started { get; set; }
    public string CompletedAt { get; set; }
    [NotNull] public int UndoUses { get; set; }
    [NotNull] public int PencilUses { get; set; }
    [NotNull] public int EraseUses { get; set; }
    [NotNull] public int SOSUses { get; set; }
    [NotNull] public int AutofillUses { get; set; }
    [NotNull] public bool IsAttempted { get; set; }
}
