using System;
using System.Collections.Generic;
using System.Globalization;
using SQLite;

/// <summary>Query and update operations for tables owned by KillerDailyChallengeDatabase.</summary>
public sealed class SqliteKillerDailyChallengeRepository : IDailyChallengeRepository
{
    private readonly SqliteDatabaseConnection _database;
    private SQLiteConnection Db => _database.Connection;

    public SqliteKillerDailyChallengeRepository() : this(SqliteDatabaseConnection.Instance) { }

    public SqliteKillerDailyChallengeRepository(SqliteDatabaseConnection database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    public DailyChallengeRecord GetOrCreate(DateTime date, int difficulty)
    {
        DateTime day=date.Date;
        string dateKey=day.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
        List<DailyChallengeRecord> rows=Db.Query<DailyChallengeRecord>(
            "SELECT * FROM killer_daily_game_challenge WHERE CurrentDate = ? ORDER BY Id DESC LIMIT 1",dateKey);
        if(rows.Count>0) return rows[0];
        var created=NewRecord(day,difficulty);
        Db.Execute(@"
            INSERT INTO killer_daily_game_challenge
                (CurrentDate, Difficulty, ElapsedSeconds, LivesRemaining, Stars, IsWon,
                 Started, CompletedAt, UndoUses, PencilUses, EraseUses, SOSUses, AutofillUses, IsAttempted)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);",
            created.CurrentDateText, created.Difficulty, created.ElapsedSeconds, created.LivesRemaining,
            created.Stars, created.IsWon, created.Started, created.CompletedAt, created.UndoUses,
            created.PencilUses, created.EraseUses, created.SOSUses, created.AutofillUses, created.IsAttempted);
        created.Id = Db.ExecuteScalar<int>("SELECT last_insert_rowid();");
        return created;
    }

    public int GetTotalChallengeCount()
    {
        return Db.ExecuteScalar<int>("SELECT COUNT(*) FROM killer_daily_game_challenge;");
    }

    public DailyChallengeStatsSnapshot GetStatsSnapshot()
    {
        List<DailyChallengeStatsSnapshot> rows = Db.Query<DailyChallengeStatsSnapshot>(
            "SELECT CurrentStreak, COALESCE(BestTimeSeconds, -1) AS BestTimeSeconds, " +
            "GamesPlayed, GamesWon, BestTimeGameId, SimpleBestTimeGameId, " +
            "BeginnerBestTimeGameId, EasyBestTimeGameId, NoviceBestTimeGameId, " +
            "ModerateBestTimeGameId, AdvancedBestTimeGameId, HardBestTimeGameId, " +
            "ExpertBestTimeGameId, HardestBestTimeGameId " +
            "FROM killer_daily_game_challenge_stats WHERE Id = 1 LIMIT 1");
        return rows.Count == 0 ? new DailyChallengeStatsSnapshot() : rows[0];
    }

    public List<DailyChallengeRecord> GetAllRecords()
    {
        return Db.Query<DailyChallengeRecord>(
            "SELECT * FROM killer_daily_game_challenge ORDER BY CurrentDate DESC, Id DESC");
    }

    public DailyChallengeRecord GetBestOverallRecord(int? preferredGameId)
    {
        List<DailyChallengeRecord> rows = Db.Query<DailyChallengeRecord>(
            "SELECT * FROM killer_daily_game_challenge WHERE IsWon = 1 " +
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
            "SELECT * FROM killer_daily_game_challenge WHERE IsWon = 1 AND Id IN (" +
            string.Join(",", placeholders) + ")", args.ToArray());
    }

    public List<DateTime> GetWinningChallengeDates()
    {
        List<DailyChallengeDateRow> rows = Db.Query<DailyChallengeDateRow>(
            "SELECT CurrentDate FROM killer_daily_game_challenge WHERE IsWon = 1 " +
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
        Db.Execute(@"
            UPDATE killer_daily_game_challenge
            SET CurrentDate = ?, Difficulty = ?, ElapsedSeconds = ?, LivesRemaining = ?, Stars = ?,
                IsWon = ?, Started = ?, CompletedAt = ?, UndoUses = ?, PencilUses = ?, EraseUses = ?,
                SOSUses = ?, AutofillUses = ?, IsAttempted = ?
            WHERE Id = ?;",
            record.CurrentDateText, record.Difficulty, record.ElapsedSeconds, record.LivesRemaining,
            record.Stars, record.IsWon, record.Started, record.CompletedAt, record.UndoUses,
            record.PencilUses, record.EraseUses, record.SOSUses, record.AutofillUses,
            record.IsAttempted, record.Id);
    }

    public void FinalizeUncompletedPreviousChallenges(DateTime today)
    {
        DateTime day=today.Date;
        string dateKey=day.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
        List<DailyChallengeRecord> rows=Db.Query<DailyChallengeRecord>(
            "SELECT * FROM killer_daily_game_challenge WHERE CurrentDate < ? AND (IsAttempted = 1 OR Started = 1) AND (CompletedAt IS NULL OR CompletedAt = '') ORDER BY CurrentDate, Id",dateKey);
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
        Db.Execute("DELETE FROM killer_daily_game_challenge WHERE IsAttempted = 0 AND CurrentDate < ?",dateKey);
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
