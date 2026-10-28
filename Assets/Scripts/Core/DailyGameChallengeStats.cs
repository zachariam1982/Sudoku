using System;
using SQLite;

/// <summary>
/// Single-row aggregate statistics for daily challenges.
/// The per-difficulty game-id fields point to the fastest winning challenge
/// for that difficulty; its elapsed time is stored in DailyGame_challenge.
/// </summary>
[Table("DailyGameChallengeStats")]
public class DailyGameChallengeStats
{
    [PrimaryKey]
    public int Id { get; set; }

    public DateTime? FirstStartedAt { get; set; }

    public DateTime? CurrentStreakStartedAt { get; set; }

    public int CurrentStreak { get; set; }

    public DateTime? LastPlayedAt { get; set; }

    public DateTime? LastLostAt { get; set; }

    public int GamesPlayed { get; set; }

    public int GamesWon { get; set; }

    public float? BestTimeSeconds { get; set; }

    /// <summary>Id of the overall fastest winning DailyGame_challenge row.</summary>
    public int? BestTimeGameId { get; set; }

    /// <summary>Id of the fastest winning challenge at Simple difficulty.</summary>
    public int? SimpleBestTimeGameId { get; set; }

    /// <summary>Id of the fastest winning challenge at Beginner difficulty.</summary>
    public int? BeginnerBestTimeGameId { get; set; }

    /// <summary>Id of the fastest winning challenge at Easy difficulty.</summary>
    public int? EasyBestTimeGameId { get; set; }

    /// <summary>Id of the fastest winning challenge at Novice difficulty.</summary>
    public int? NoviceBestTimeGameId { get; set; }

    /// <summary>Id of the fastest winning challenge at Moderate difficulty.</summary>
    public int? ModerateBestTimeGameId { get; set; }

    /// <summary>Id of the fastest winning challenge at Advanced difficulty.</summary>
    public int? AdvancedBestTimeGameId { get; set; }

    /// <summary>Id of the fastest winning challenge at Hard difficulty.</summary>
    public int? HardBestTimeGameId { get; set; }

    /// <summary>Id of the fastest winning challenge at Expert difficulty.</summary>
    public int? ExpertBestTimeGameId { get; set; }

    /// <summary>Id of the fastest winning challenge at Hardest difficulty.</summary>
    public int? HardestBestTimeGameId { get; set; }
}
