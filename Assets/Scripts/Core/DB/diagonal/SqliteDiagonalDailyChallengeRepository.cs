using System;
using System.Collections.Generic;
using System.Globalization;
using SQLite;

/// <summary>Query and update operations for tables owned by DiagonalDailyChallengeDatabase.</summary>
public sealed class SqliteDiagonalDailyChallengeRepository : IDiagonalDailyChallengeRepository
{
    private readonly SqliteDatabaseConnection _database;
    private SQLiteConnection Db => _database.Connection;

    public SqliteDiagonalDailyChallengeRepository() : this(SqliteDatabaseConnection.Instance) { }

    public SqliteDiagonalDailyChallengeRepository(SqliteDatabaseConnection database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    public DiagonalDailyChallengeRecord GetOrCreate(DateTime date, int difficulty)
    {
        DateTime day=date.Date;
        string dateKey=day.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
        List<DiagonalDailyChallengeRecord> rows=Db.Query<DiagonalDailyChallengeRecord>(
            "SELECT * FROM diagonal_daily_game_challenge WHERE CurrentDate = ? ORDER BY Id DESC LIMIT 1",dateKey);
        if(rows.Count>0) return rows[0];
        var created=NewRecord(day,difficulty);
        Db.Insert(created);
        return created;
    }

    public int GetTotalChallengeCount()
    {
        return Db.ExecuteScalar<int>("SELECT COUNT(*) FROM diagonal_daily_game_challenge;");
    }

    public DiagonalDailyChallengeStatsSnapshot GetStatsSnapshot()
    {
        List<DiagonalDailyChallengeStatsSnapshot> rows = Db.Query<DiagonalDailyChallengeStatsSnapshot>(
            "SELECT CurrentStreak, COALESCE(BestTimeSeconds, -1) AS BestTimeSeconds, " +
            "GamesPlayed, GamesWon, BestTimeGameId, SimpleBestTimeGameId, " +
            "BeginnerBestTimeGameId, EasyBestTimeGameId, NoviceBestTimeGameId, " +
            "ModerateBestTimeGameId, AdvancedBestTimeGameId, HardBestTimeGameId, " +
            "ExpertBestTimeGameId, HardestBestTimeGameId " +
            "FROM diagonal_daily_game_challenge_stats WHERE Id = 1 LIMIT 1");
        return rows.Count == 0 ? new DiagonalDailyChallengeStatsSnapshot() : rows[0];
    }

    public List<DiagonalDailyChallengeRecord> GetAllRecords()
    {
        return Db.Query<DiagonalDailyChallengeRecord>(
            "SELECT * FROM diagonal_daily_game_challenge ORDER BY CurrentDate DESC, Id DESC");
    }

    public DiagonalDailyChallengeRecord GetBestOverallRecord(int? preferredGameId)
    {
        List<DiagonalDailyChallengeRecord> rows = Db.Query<DiagonalDailyChallengeRecord>(
            "SELECT * FROM diagonal_daily_game_challenge WHERE IsWon = 1 " +
            "ORDER BY CASE WHEN Id = ? THEN 0 ELSE 1 END, ElapsedSeconds, Id LIMIT 1",
            preferredGameId ?? -1);
        return rows.Count == 0 ? null : rows[0];
    }

    public List<DiagonalDailyChallengeRecord> GetBestTimeRecordsByDifficulty(DiagonalDailyChallengeStatsSnapshot stats)
    {
        if (stats == null) return new List<DiagonalDailyChallengeRecord>();
        int?[] ids = stats.GetDifficultyBestGameIds();
        List<object> args = new List<object>(ids.Length);
        foreach (int? id in ids) if (id.HasValue) args.Add(id.Value);
        if (args.Count == 0) return new List<DiagonalDailyChallengeRecord>();
        string[] placeholders = new string[args.Count];
        for (int i = 0; i < placeholders.Length; i++) placeholders[i] = "?";
        return Db.Query<DiagonalDailyChallengeRecord>(
            "SELECT * FROM diagonal_daily_game_challenge WHERE IsWon = 1 AND Id IN (" +
            string.Join(",", placeholders) + ")", args.ToArray());
    }

    public List<DateTime> GetWinningChallengeDates()
    {
        List<DiagonalDailyChallengeDateRow> rows = Db.Query<DiagonalDailyChallengeDateRow>(
            "SELECT CurrentDate FROM diagonal_daily_game_challenge WHERE IsWon = 1 " +
            "GROUP BY CurrentDate ORDER BY CurrentDate");
        List<DateTime> dates = new List<DateTime>(rows.Count);
        foreach (DiagonalDailyChallengeDateRow row in rows)
            if (DateTime.TryParse(row.CurrentDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                dates.Add(date.Date);
        return dates;
    }

    public void MarkAttempted(DiagonalDailyChallengeRecord record)
    {
        if(record==null)return;
        record.IsAttempted=true;
        record.Started=true;
        Update(record);
    }

    public void Update(DiagonalDailyChallengeRecord record)
    {
        if(record==null)return;
        Db.Update(record);
    }

    public void FinalizeUncompletedPreviousChallenges(DateTime today)
    {
        DateTime day=today.Date;
        string dateKey=day.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
        List<DiagonalDailyChallengeRecord> rows=Db.Query<DiagonalDailyChallengeRecord>(
            "SELECT * FROM diagonal_daily_game_challenge WHERE CurrentDate < ? AND (IsAttempted = 1 OR Started = 1) AND (CompletedAt IS NULL OR CompletedAt = '') ORDER BY CurrentDate, Id",dateKey);
        foreach(DiagonalDailyChallengeRecord previous in rows)
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
        Db.Execute("DELETE FROM diagonal_daily_game_challenge WHERE IsAttempted = 0 AND CurrentDate < ?",dateKey);
    }

    public int GetPuzzleSeed(DateTime date)
    {
        return int.Parse(date.ToString("yyyyMMdd",CultureInfo.InvariantCulture),CultureInfo.InvariantCulture);
    }


    private static DiagonalDailyChallengeRecord NewRecord(DateTime date,int difficulty)
    {
        return new DiagonalDailyChallengeRecord { CurrentDate=date, Difficulty=difficulty, IsAttempted=false, Started=false, LivesRemaining=3 };
    }
}
