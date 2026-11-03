using System;
using System.Collections.Generic;

/// <summary>Default SQLite adapter for Classic/Journey history.</summary>
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

/// <summary>Default SQLite adapter for Daily Challenge.</summary>
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

/// <summary>Default SQLite adapter for New Game results.</summary>
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
