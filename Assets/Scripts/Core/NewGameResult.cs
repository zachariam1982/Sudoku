using System;
#if !UNITY_WEBGL || UNITY_EDITOR
using SQLite;
#endif

[Serializable]
#if !UNITY_WEBGL || UNITY_EDITOR
[Table("new_game_results")]
#endif
public class NewGameResult
{
#if !UNITY_WEBGL || UNITY_EDITOR
    [PrimaryKey, AutoIncrement]
#endif
    public int Id { get; set; }
    public int Level { get; set; }
    public int Difficulty { get; set; }
    public int PuzzleSeed { get; set; }
    public int PencilUses { get; set; }
    public int SOSUses { get; set; }
    public int AutoFillUses { get; set; }
    public string RecordedAt { get; set; }
}

#if !UNITY_WEBGL || UNITY_EDITOR
[Table("new_game_result_completions")]
#endif
public class NewGameResultCompletion
{
#if !UNITY_WEBGL || UNITY_EDITOR
    [PrimaryKey]
#endif
    public int ResultId { get; set; }
}

#if !UNITY_WEBGL || UNITY_EDITOR
[Table("new_game_stats_by_difficulty")]
#endif
public class NewGameDifficultyStats
{
#if !UNITY_WEBGL || UNITY_EDITOR
    [PrimaryKey]
#endif
    public int Difficulty { get; set; }
    public int GamesStarted { get; set; }
    public int GamesCompleted { get; set; }
}

public sealed class NewGameResultStats
{
    public int TotalGames;
    public int CompletedGames;
    public int IncompleteGames;

    public int GamesForDifficulty(SudokuDifficulty difficulty) =>
        NewGameResultDatabase.GetDifficultyStats(difficulty).GamesStarted;

    public int WinsForDifficulty(SudokuDifficulty difficulty) =>
        NewGameResultDatabase.GetDifficultyStats(difficulty).GamesCompleted;
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
