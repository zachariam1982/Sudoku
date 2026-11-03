using System.Collections.Generic;

/// <summary>Variant-specific database contract for New Game history and statistics.</summary>
public interface IDiagonalGameResultRepository
{
    void RecordAttempt(DiagonalNewGameResult result, bool completed);
    void UpdateCompletedAttempt(DiagonalNewGameResult result);
    int GetNextId();
    DiagonalNewGameResult GetLatest();
    List<DiagonalNewGameResult> GetRecent(int count);
    List<DiagonalNewGameResultEntry> GetRecentEntries(int offset, int count);
    List<DiagonalNewGameResultEntry> GetRecentEntries(int count);
    DiagonalNewGameResultStats GetStats();
    DiagonalNewGameDifficultyStats GetDifficultyStats(SudokuDifficulty difficulty);
}
