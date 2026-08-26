using SQLite;

[Table("game_stats")]
public class GameStats
{
    // game_stats always contains exactly one row: Id = 1.
    [PrimaryKey]
    public int Id { get; set; }

    // Used for one-time migrations/rebuilds.
    public int AggregateVersion { get; set; }

    public int TotalGames { get; set; }

    public int TotalWins { get; set; }

    public int TotalPoints { get; set; }

    // NULL until the player wins a game.
    public double? FastestWinSeconds { get; set; }

    public int CurrentStreak { get; set; }

    // Number of games played at each difficulty.
    public int SimpleCount { get; set; }

    public int BeginnerCount { get; set; }

    public int EasyCount { get; set; }

    public int NoviceCount { get; set; }

    public int ModerateCount { get; set; }

    public int AdvancedCount { get; set; }

    public int HardCount { get; set; }

    public int ExpertCount { get; set; }

    public int HardestCount { get; set; }
}