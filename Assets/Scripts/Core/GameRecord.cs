using SQLite;

/// <summary>
/// Represents a single completed game session stored in the SQLite database.
/// Attributes are read by sqlite-net-pcl to map properties to columns.
/// </summary>
[Table("completed_games")]
public class GameRecord
{
    [PrimaryKey, AutoIncrement]
    public int    Id             { get; set; }

    [NotNull]
    public int    Level          { get; set; }

    [NotNull]
    public int    Difficulty     { get; set; }

    [NotNull]
    public float  ElapsedSeconds { get; set; }

    [NotNull]
    public int    LivesRemaining { get; set; }

    [NotNull]
    public int    Points         { get; set; }

    [NotNull]
    public bool   IsWon          { get; set; }

    /// <summary>ISO-8601 timestamp e.g. "2025-06-04T14:32:00"</summary>
    [NotNull]
    public string CompletedAt    { get; set; }
}