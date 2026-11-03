using System;
using System.Collections.Generic;

/// <summary>SQLite-backed repository for Daily Challenge records and statistics.</summary>
public sealed class SqliteDailyChallengeRepository : IDailyChallengeRepository
{
    public DailyChallengeRecord GetOrCreate(DateTime date, int difficulty) => DailyChallengeRepository.GetOrCreate(date, difficulty);
    public int GetTotalChallengeCount() => DailyChallengeRepository.GetTotalChallengeCount();
    public DailyChallengeStatsSnapshot GetStatsSnapshot() => DailyChallengeRepository.GetStatsSnapshot();
    public List<DailyChallengeRecord> GetAllRecords() => DailyChallengeRepository.GetAllRecords();
    public DailyChallengeRecord GetBestOverallRecord(int? preferredGameId) => DailyChallengeRepository.GetBestOverallRecord(preferredGameId);
    public List<DailyChallengeRecord> GetBestTimeRecordsByDifficulty(DailyChallengeStatsSnapshot stats) => DailyChallengeRepository.GetBestTimeRecordsByDifficulty(stats);
    public List<DateTime> GetWinningChallengeDates() => DailyChallengeRepository.GetWinningChallengeDates();
    public void MarkAttempted(DailyChallengeRecord record) => DailyChallengeRepository.MarkAttempted(record);
    public void Update(DailyChallengeRecord record) => DailyChallengeRepository.Update(record);
    public void FinalizeUncompletedPreviousChallenges(DateTime today) => DailyChallengeRepository.FinalizeUncompletedPreviousChallenges(today);
    public void DeleteUnattemptedBefore(DateTime today) => DailyChallengeRepository.DeleteUnattemptedBefore(today);
    public int GetPuzzleSeed(DateTime date) => DailyChallengeRepository.GetPuzzleSeed(date);
}
