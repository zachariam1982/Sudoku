using System.Collections.Generic;

/// <summary>SQLite-backed history used by the Classic Sudoku progression.</summary>
public sealed class SqliteGameHistoryRepository : IGameHistoryRepository
{
    public void Insert(GameRecord record) => GameDatabase.Insert(record);
    public void Update(GameRecord record) => GameDatabase.Update(record);
    public GameStats GetGameStats() => GameDatabase.GetGameStats();
    public int GetTotalPossiblePoints(GameStats stats) => GameDatabase.GetTotalPossiblePoints(stats);
    public List<GameRecord> GetAll() => GameDatabase.GetAll();
    public List<GameRecord> GetNextSet(int offset) => GameDatabase.GetNextSet(offset);
    public List<GameRecord> GetLastNRecordByDate(int count) => GameDatabase.GetLastNRecordByDate(count);
    public GameRecord GetLastRecord() => GameDatabase.GetLastRecord();
    public GameRecord GetFastestWin() => GameDatabase.GetFastestWin();
    public int GetTotalPoints() => GameDatabase.GetTotalPoints();
    public int GetTotalWins() => GameDatabase.GetTotalWins();
    public int GetTotalGamesPlayed() => GameDatabase.GetTotalGamesPlayed();
    public void RebuildGameStats() => GameDatabase.RebuildGameStats();
}
