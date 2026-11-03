using System;
using SQLite;

[Serializable]
[Table("diagonal_new_game_results")]
public class DiagonalNewGameResult
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int Level { get; set; }
    public int Difficulty { get; set; }
    public int PuzzleSeed { get; set; }
    public int PencilUses { get; set; }
    public int SOSUses { get; set; }
    public int AutoFillUses { get; set; }
    public string RecordedAt { get; set; }
}

[Table("diagonal_new_game_result_completions")]
public class DiagonalNewGameResultCompletion
{
    [PrimaryKey]
    public int ResultId { get; set; }
}

[Table("diagonal_new_game_stats_by_difficulty")]
public class DiagonalNewGameDifficultyStats
{
    [PrimaryKey]
    public int Difficulty { get; set; }
    public int GamesStarted { get; set; }
    public int GamesCompleted { get; set; }
}

public sealed class DiagonalNewGameResultStats
{
    public int TotalGames;
    public int CompletedGames;
    public int IncompleteGames;

}

public sealed class DiagonalNewGameResultEntry
{
    public DiagonalNewGameResult Result;
    public bool IsCompleted;
    public DiagonalNewGameResultEntry(DiagonalNewGameResult result, bool isCompleted)
    {
        Result = result;
        IsCompleted = isCompleted;
    }
}
