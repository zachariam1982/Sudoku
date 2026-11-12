using System;
using System.Collections.Generic;

/// <summary>Database contract for Daily Challenge records and statistics.</summary>
public interface IDailyChallengeRepository
{
    DailyChallengeRecord GetOrCreate(DateTime date, int difficulty);
    int GetTotalChallengeCount();
    DailyChallengeStatsSnapshot GetStatsSnapshot();
    List<DailyChallengeRecord> GetAllRecords();
    List<DailyChallengeCalendarEntry> GetCalendarEntries();
    DateTime? GetFirstChallengeDate();
    DailyChallengeRecord GetBestOverallRecord(int? preferredGameId);
    List<DailyChallengeRecord> GetBestTimeRecordsByDifficulty(DailyChallengeStatsSnapshot stats);
    List<DateTime> GetWinningChallengeDates();
    void MarkAttempted(DailyChallengeRecord record);
    void Update(DailyChallengeRecord record);
    void FinalizeUncompletedPreviousChallenges(DateTime today);
    void DeleteUnattemptedBefore(DateTime today);
    int GetPuzzleSeed(DateTime date);
}
