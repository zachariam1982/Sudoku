using System.Collections.Generic;

/// <summary>Database contract for a game's history and aggregate statistics.</summary>
public interface IDiagonalGameHistoryRepository
{
    void Insert(DiagonalGameRecord record);
    void Update(DiagonalGameRecord record);
    DiagonalGameStats GetDiagonalGameStats();
    int GetTotalPossiblePoints(DiagonalGameStats stats);
    List<DiagonalGameRecord> GetAll();
    List<DiagonalGameRecord> GetNextSet(int offset);
    List<DiagonalGameRecord> GetLastNRecordByDate(int count);
    DiagonalGameRecord GetLastRecord();
    DiagonalGameRecord GetFastestWin();
    int GetTotalPoints();
    int GetTotalWins();
    int GetTotalGamesPlayed();
    void RebuildDiagonalGameStats();
}
