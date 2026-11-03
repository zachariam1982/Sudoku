using System.Collections.Generic;

/// <summary>Variant-specific database contract for New Game history and statistics.</summary>
public interface IGameResultRepository
{
    void RecordAttempt(NewGameResult result, bool completed);
    void UpdateCompletedAttempt(NewGameResult result);
    int GetNextId();
    NewGameResult GetLatest();
    List<NewGameResult> GetRecent(int count);
    List<NewGameResultEntry> GetRecentEntries(int offset, int count);
    List<NewGameResultEntry> GetRecentEntries(int count);
    NewGameResultStats GetStats();
    NewGameDifficultyStats GetDifficultyStats(SudokuDifficulty difficulty);
}
