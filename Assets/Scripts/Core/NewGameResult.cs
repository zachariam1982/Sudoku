using System;
using SQLite;

[Serializable]
[Table("new_game_results")]
public class NewGameResult
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

[Table("new_game_result_completions")]
public class NewGameResultCompletion
{
    [PrimaryKey]
    public int ResultId { get; set; }
}

[Table("new_game_stats_by_difficulty")]
public class NewGameDifficultyStats
{
    [PrimaryKey]
    public int Difficulty { get; set; }
    public int GamesStarted { get; set; }
    public int GamesCompleted { get; set; }
}

public sealed class NewGameResultStats
{
    public int TotalGames;
    public int CompletedGames;
    public int IncompleteGames;

}

public sealed class NewGameResultEntry
{
    public NewGameResult Result;
    public bool IsCompleted;
    public NewGameResultEntry(NewGameResult result, bool isCompleted)
    {
        Result = result;
        IsCompleted = isCompleted;
    }
}
