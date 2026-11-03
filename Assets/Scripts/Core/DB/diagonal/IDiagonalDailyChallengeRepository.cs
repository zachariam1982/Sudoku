using System;
using System.Collections.Generic;

/// <summary>Database contract for Daily Challenge records and statistics.</summary>
public interface IDiagonalDailyChallengeRepository
{
    DiagonalDailyChallengeRecord GetOrCreate(DateTime date, int difficulty);
    int GetTotalChallengeCount();
    DiagonalDailyChallengeStatsSnapshot GetStatsSnapshot();
    List<DiagonalDailyChallengeRecord> GetAllRecords();
    DiagonalDailyChallengeRecord GetBestOverallRecord(int? preferredGameId);
    List<DiagonalDailyChallengeRecord> GetBestTimeRecordsByDifficulty(DiagonalDailyChallengeStatsSnapshot stats);
    List<DateTime> GetWinningChallengeDates();
    void MarkAttempted(DiagonalDailyChallengeRecord record);
    void Update(DiagonalDailyChallengeRecord record);
    void FinalizeUncompletedPreviousChallenges(DateTime today);
    void DeleteUnattemptedBefore(DateTime today);
    int GetPuzzleSeed(DateTime date);
}
