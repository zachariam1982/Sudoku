using System;
using System.Collections.Generic;
using System.Globalization;
using SQLite;

/// <summary>Snapshot of the single-row daily challenge statistics table.</summary>
public sealed class DiagonalDailyChallengeStatsSnapshot
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

internal sealed class DiagonalDailyChallengeDateRow
{
    public string CurrentDate { get; set; }
}

/// <summary>SQLite mapping for a daily challenge row, including both lifecycle flags.</summary>
[Table("diagonal_daily_game_challenge")]
public sealed class DiagonalDailyChallengeRecord
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
