using System;
using System.Collections.Generic;
using System.Globalization;
using SQLite;
using UnityEngine;

/// <summary>Persistence helpers for the existing daily challenge table. The table schema is owned by the game database.</summary>
public static class DailyChallengeRepository
{
#if UNITY_WEBGL && !UNITY_EDITOR
    private const string StoreKey = "sudoku_daily_challenges";
    [Serializable] private sealed class StoredRow { public int Id; public string CurrentDate; public int Difficulty; public float ElapsedSeconds; public int LivesRemaining; public int Stars; public bool IsWon; public bool Started; public string CompletedAt; public int UndoUses; public int PencilUses; public int EraseUses; public int SOSUses; public int AutofillUses; public bool IsAttempted; }
    [Serializable] private sealed class Store { public List<StoredRow> Rows = new List<StoredRow>(); public int NextId=1; }
    private static Store _store;
    private static Store Data
    {
        get
        {
            if (_store != null) return _store;
            string json=PlayerPrefs.GetString(StoreKey,string.Empty);
            try { _store=string.IsNullOrEmpty(json)?null:JsonUtility.FromJson<Store>(json); } catch { _store=null; }
            if (_store==null) _store=new Store();
            if (_store.Rows==null) _store.Rows=new List<StoredRow>();
            return _store;
        }
    }
    private static void Save() { PlayerPrefs.SetString(StoreKey,JsonUtility.ToJson(Data)); }
#else
    private static SQLiteConnection Db => GameDatabase.Connection;
#endif

    public static DailyChallengeRecord GetOrCreate(DateTime date, int difficulty)
    {
        DateTime day=date.Date;
#if UNITY_WEBGL && !UNITY_EDITOR
        StoredRow storedExisting=Data.Rows.Find(r=>DateTime.Parse(r.CurrentDate,CultureInfo.InvariantCulture).Date==day);
        DailyChallengeRecord existing=storedExisting==null?null:FromStored(storedExisting);
        if(existing!=null) return existing;
        var created=NewRecord(day,difficulty); created.Id=Data.NextId++; Data.Rows.Add(ToStored(created)); Save(); return created;
#else
        string dateKey=day.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
        List<DailyChallengeRecord> rows=Db.Query<DailyChallengeRecord>(
            "SELECT * FROM DailyGame_challenge WHERE CurrentDate = ? ORDER BY Id DESC LIMIT 1",dateKey);
        if(rows.Count>0) return rows[0];
        var created=NewRecord(day,difficulty);
        Db.Insert(created);
        return created;
#endif
    }

    public static int GetTotalChallengeCount()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return Data.Rows.Count;
#else
        return Db.ExecuteScalar<int>("SELECT COUNT(*) FROM DailyGame_challenge;");
#endif
    }

    public static DailyChallengeStatsSnapshot GetStatsSnapshot()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        float bestTime = -1f;
        int gamesPlayed = 0;
        int gamesWon = 0;
        foreach (StoredRow row in Data.Rows)
        {
            if (!string.IsNullOrEmpty(row.CompletedAt)) gamesPlayed++;
            if (!row.IsWon) continue;
            gamesWon++;
            if (bestTime < 0f || row.ElapsedSeconds < bestTime)
                bestTime = row.ElapsedSeconds;
        }

        DateTime checkDate = DateTime.Today;
        StoredRow today = Data.Rows.Find(r =>
            DateTime.Parse(r.CurrentDate, CultureInfo.InvariantCulture).Date == checkDate);
        if (today == null || !today.IsWon)
            checkDate = checkDate.AddDays(-1);

        int streak = 0;
        while (true)
        {
            StoredRow row = Data.Rows.Find(r =>
                DateTime.Parse(r.CurrentDate, CultureInfo.InvariantCulture).Date == checkDate);
            if (row == null || !row.IsWon)
                break;
            streak++;
            checkDate = checkDate.AddDays(-1);
        }

        return new DailyChallengeStatsSnapshot
        {
            CurrentStreak = streak,
            BestTimeSeconds = bestTime,
            GamesPlayed = gamesPlayed,
            GamesWon = gamesWon
        };
#else
        List<DailyChallengeStatsSnapshot> rows = Db.Query<DailyChallengeStatsSnapshot>(
            "SELECT CurrentStreak, COALESCE(BestTimeSeconds, -1) AS BestTimeSeconds, " +
            "GamesPlayed, GamesWon, BestTimeGameId " +
            "FROM DailyGameChallengeStats WHERE Id = 1 LIMIT 1");
        return rows.Count == 0 ? new DailyChallengeStatsSnapshot() : rows[0];
#endif
    }

    public static List<DailyChallengeRecord> GetAllRecords()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        List<DailyChallengeRecord> records = new List<DailyChallengeRecord>(Data.Rows.Count);
        foreach (StoredRow row in Data.Rows) records.Add(FromStored(row));
        return records;
#else
        return Db.Query<DailyChallengeRecord>(
            "SELECT * FROM DailyGame_challenge ORDER BY CurrentDate DESC, Id DESC");
#endif
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
#if UNITY_WEBGL && !UNITY_EDITOR
        StoredRow existing=Data.Rows.Find(r=>r.Id==record.Id);
        if(existing!=null) Data.Rows[Data.Rows.IndexOf(existing)]=ToStored(record);
        Save();
#else
        Db.Update(record);
#endif
    }

    public static void FinalizeUncompletedPreviousChallenges(DateTime today)
    {
        DateTime day=today.Date;
#if UNITY_WEBGL && !UNITY_EDITOR
        bool changed=false;
        foreach(StoredRow row in Data.Rows)
        {
            DateTime rowDate=DateTime.Parse(row.CurrentDate,CultureInfo.InvariantCulture).Date;
            if(rowDate>=day || (!row.IsAttempted && !row.Started) || !string.IsNullOrEmpty(row.CompletedAt))
                continue;

            DateTime endOfRowDay=DateTime.SpecifyKind(
                rowDate.AddDays(1).AddSeconds(-1),DateTimeKind.Local);
            row.CompletedAt=endOfRowDay.ToString("o",CultureInfo.InvariantCulture);
            row.IsWon=false;
            row.Stars=0;
            changed=true;
        }
        if(changed) Save();
#else
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
#endif
    }

    public static void DeleteUnattemptedBefore(DateTime today)
    {
        DateTime day=today.Date;
#if UNITY_WEBGL && !UNITY_EDITOR
        Data.Rows.RemoveAll(r=>!r.IsAttempted && DateTime.Parse(r.CurrentDate,CultureInfo.InvariantCulture).Date<day);
        Save();
#else
        string dateKey=day.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
        Db.Execute("DELETE FROM DailyGame_challenge WHERE IsAttempted = 0 AND CurrentDate < ?",dateKey);
#endif
    }

    public static int GetPuzzleSeed(DateTime date)
    {
        return int.Parse(date.ToString("yyyyMMdd",CultureInfo.InvariantCulture),CultureInfo.InvariantCulture);
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    private static StoredRow ToStored(DailyChallengeRecord r) { return new StoredRow { Id=r.Id, CurrentDate=r.CurrentDate.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture), Difficulty=r.Difficulty, ElapsedSeconds=r.ElapsedSeconds, LivesRemaining=r.LivesRemaining, Stars=r.Stars, IsWon=r.IsWon, Started=r.Started, CompletedAt=r.CompletedAt, UndoUses=r.UndoUses, PencilUses=r.PencilUses, EraseUses=r.EraseUses, SOSUses=r.SOSUses, AutofillUses=r.AutofillUses, IsAttempted=r.IsAttempted }; }
    private static DailyChallengeRecord FromStored(StoredRow r) { return new DailyChallengeRecord { Id=r.Id, CurrentDate=DateTime.Parse(r.CurrentDate,CultureInfo.InvariantCulture), Difficulty=r.Difficulty, ElapsedSeconds=r.ElapsedSeconds, LivesRemaining=r.LivesRemaining, Stars=r.Stars, IsWon=r.IsWon, Started=r.Started, CompletedAt=r.CompletedAt, UndoUses=r.UndoUses, PencilUses=r.PencilUses, EraseUses=r.EraseUses, SOSUses=r.SOSUses, AutofillUses=r.AutofillUses, IsAttempted=r.IsAttempted }; }

#endif

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
