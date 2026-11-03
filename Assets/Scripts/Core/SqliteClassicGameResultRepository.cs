using System.Collections.Generic;

/// <summary>SQLite-backed result repository for Classic Sudoku new games.</summary>
public sealed class SqliteClassicGameResultRepository : IGameResultRepository
{
    public void RecordAttempt(NewGameResult result, bool completed) => NewGameResultDatabase.RecordAttempt(result, completed);
    public void UpdateCompletedAttempt(NewGameResult result) => NewGameResultDatabase.UpdateCompletedAttempt(result);
    public int GetNextId() => NewGameResultDatabase.GetNextId();
    public NewGameResult GetLatest() => NewGameResultDatabase.GetLatest();
    public List<NewGameResult> GetRecent(int count) => NewGameResultDatabase.GetRecent(count);
    public List<NewGameResultEntry> GetRecentEntries(int offset, int count) => NewGameResultDatabase.GetRecentEntries(offset, count);
    public List<NewGameResultEntry> GetRecentEntries(int count) => NewGameResultDatabase.GetRecentEntries(count);
    public NewGameResultStats GetStats() => NewGameResultDatabase.GetStats();
    public NewGameDifficultyStats GetDifficultyStats(SudokuDifficulty difficulty) => NewGameResultDatabase.GetDifficultyStats(difficulty);
}
