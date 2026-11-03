using System.Collections.Generic;

/// <summary>Database contract for a game's history and aggregate statistics.</summary>
public interface IGameHistoryRepository
{
    void Insert(GameRecord record);
    void Update(GameRecord record);
    GameStats GetGameStats();
    int GetTotalPossiblePoints(GameStats stats);
    List<GameRecord> GetAll();
    List<GameRecord> GetNextSet(int offset);
    List<GameRecord> GetLastNRecordByDate(int count);
    GameRecord GetLastRecord();
    GameRecord GetFastestWin();
    int GetTotalPoints();
    int GetTotalWins();
    int GetTotalGamesPlayed();
    void RebuildGameStats();
}
