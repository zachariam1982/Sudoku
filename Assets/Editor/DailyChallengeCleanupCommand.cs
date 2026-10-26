using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using SQLite;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Removes the newest Daily Challenge row and rebuilds the single-row stats.
/// Can be run from Unity's Tools menu or Unity Editor batch mode.
/// </summary>
public static class DailyChallengeCleanupCommand
{
    private static readonly Dictionary<int, string> BestTimeColumns = new Dictionary<int, string>
    {
        { 0, "SimpleBestTimeGameId" },
        { 1, "BeginnerBestTimeGameId" },
        { 2, "EasyBestTimeGameId" },
        { 3, "NoviceBestTimeGameId" },
        { 4, "ModerateBestTimeGameId" },
        { 5, "AdvancedBestTimeGameId" },
        { 6, "HardBestTimeGameId" },
        { 7, "ExpertBestTimeGameId" },
        { 8, "HardestBestTimeGameId" }
    };

    [MenuItem("Tools/Daily Challenge/Clean Latest Test Challenge")]
    public static void CleanFromEditorMenu()
    {
        string databasePath = ApplicationDatabasePath();
        if (!File.Exists(databasePath))
        {
            EditorUtility.DisplayDialog("Daily Challenge Cleanup", "Database not found:\n" + databasePath, "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Clean Latest Daily Challenge",
                "This deletes the newest DailyGame_challenge row and rebuilds DailyGameChallengeStats.\n\nDatabase:\n" + databasePath,
                "Delete Latest Row",
                "Cancel"))
            return;

        try
        {
            Clean(databasePath);
        }
        catch (Exception exception)
        {
            Debug.LogError("[DailyChallengeCleanup] " + exception);
            EditorUtility.DisplayDialog("Daily Challenge Cleanup Failed", exception.Message, "OK");
        }
    }

    /// <summary>
    /// Command line entry point. Example:
    /// Unity.exe -batchmode -quit -projectPath "...\\Sudoku" -executeMethod DailyChallengeCleanupCommand.RunFromCommandLine -dailyChallengeYes
    /// Add -dailyChallengeDb "C:\\path\\game_history.db" to override the default database path.
    /// </summary>
    public static void RunFromCommandLine()
    {
        string[] arguments = Environment.GetCommandLineArgs();
        if (!arguments.Contains("-dailyChallengeYes"))
        {
            Debug.LogError("[DailyChallengeCleanup] Batch mode requires -dailyChallengeYes to confirm deletion.");
            EditorApplication.Exit(2);
            return;
        }

        string databasePath = GetArgumentValue(arguments, "-dailyChallengeDb") ?? ApplicationDatabasePath();
        try
        {
            Clean(databasePath);
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError("[DailyChallengeCleanup] " + exception);
            EditorApplication.Exit(1);
        }
    }

    private static void Clean(string databasePath)
    {
        databasePath = Path.GetFullPath(databasePath);
        if (!File.Exists(databasePath))
            throw new FileNotFoundException("Database file not found", databasePath);

        using (var database = new SQLiteConnection(databasePath))
        {
            ValidateSchema(database);

            DailyChallengeRecord newest = database.Query<DailyChallengeRecord>(
                "SELECT * FROM DailyGame_challenge ORDER BY Id DESC LIMIT 1").FirstOrDefault();
            if (newest == null)
            {
                Debug.Log("[DailyChallengeCleanup] DailyGame_challenge is empty; nothing to clean.");
                return;
            }

            database.RunInTransaction(() =>
            {
                database.Execute("DELETE FROM DailyGame_challenge WHERE Id = ?", newest.Id);
                RebuildStats(database);
            });

            Debug.Log("[DailyChallengeCleanup] Deleted latest row Id=" + newest.Id +
                      " (CurrentDate=" + newest.CurrentDateText +
                      ", Difficulty=" + newest.Difficulty +
                      ", CompletedAt=" + (newest.CompletedAt ?? "null") + ").");
            Debug.Log("[DailyChallengeCleanup] DailyGameChallengeStats rebuilt from remaining completed rows.");
        }
    }

    private static void ValidateSchema(SQLiteConnection database)
    {
        RequireColumns(database, "DailyGame_challenge", new[]
        {
            "Id", "CurrentDate", "Difficulty", "ElapsedSeconds", "LivesRemaining", "Stars",
            "IsWon", "IsAttempted", "Started", "CompletedAt", "UndoUses", "PencilUses",
            "EraseUses", "SOSUses", "AutofillUses"
        });

        var statsColumns = new List<string>
        {
            "Id", "FirstStartedAt", "CurrentStreakStartedAt", "CurrentStreak",
            "LastPlayedAt", "LastLostAt", "GamesPlayed", "GamesWon",
            "BestTimeSeconds", "BestTimeGameId"
        };
        statsColumns.AddRange(BestTimeColumns.Values);
        RequireColumns(database, "DailyGameChallengeStats", statsColumns);
    }

    private static void RequireColumns(SQLiteConnection database, string tableName, IEnumerable<string> required)
    {
        HashSet<string> existing = new HashSet<string>(
            database.GetTableInfo(tableName).Select(column => column.Name),
            StringComparer.OrdinalIgnoreCase);
        if (existing.Count == 0)
            throw new InvalidOperationException("Table not found: " + tableName);

        string[] missing = required.Where(column => !existing.Contains(column)).ToArray();
        if (missing.Length > 0)
            throw new InvalidOperationException(
                "Table " + tableName + " is missing columns: " + string.Join(", ", missing) + ". No changes were made.");
    }

    private static void RebuildStats(SQLiteConnection database)
    {
        database.Execute("INSERT OR IGNORE INTO DailyGameChallengeStats (Id) VALUES (1)");
        string firstStartedAt = database.ExecuteScalar<string>(
            "SELECT FirstStartedAt FROM DailyGameChallengeStats WHERE Id = 1");

        // FirstStartedAt is historical; preserve it while any challenge rows remain.
        // Once all rows are removed, clear it so a fresh test run starts cleanly.
        int remainingRows = database.ExecuteScalar<int>("SELECT COUNT(*) FROM DailyGame_challenge");
        if (remainingRows == 0)
            firstStartedAt = null;

        List<DailyChallengeRecord> completed = database.Query<DailyChallengeRecord>(
            "SELECT * FROM DailyGame_challenge " +
            "WHERE CompletedAt IS NOT NULL AND TRIM(CAST(CompletedAt AS TEXT)) <> '' " +
            "ORDER BY Id ASC");

        int gamesPlayed = completed.Count;
        int gamesWon = completed.Count(row => row.IsWon);
        string lastPlayedAt = completed.Count == 0 ? null : completed[completed.Count - 1].CompletedAt;
        string lastLostAt = null;
        int currentStreak = 0;
        string currentStreakStartedAt = null;

        foreach (DailyChallengeRecord row in completed)
        {
            DateTime parsedCompletedAt = ParseCompletedAt(row);
            if (row.IsWon)
            {
                if (currentStreak == 0)
                    currentStreakStartedAt = row.CompletedAt;
                currentStreak++;
            }
            else
            {
                currentStreak = 0;
                currentStreakStartedAt = null;
                lastLostAt = parsedCompletedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
        }

        List<DailyChallengeRecord> wins = completed.Where(row => row.IsWon).ToList();
        DailyChallengeRecord bestOverall = wins
            .OrderBy(row => row.ElapsedSeconds)
            .ThenBy(row => row.Id)
            .FirstOrDefault();
        float? bestTimeSeconds = bestOverall == null ? (float?)null : bestOverall.ElapsedSeconds;
        int? bestTimeGameId = bestOverall == null ? (int?)null : bestOverall.Id;

        var values = new List<object>
        {
            firstStartedAt,
            currentStreakStartedAt,
            currentStreak,
            lastPlayedAt,
            lastLostAt,
            gamesPlayed,
            gamesWon,
            bestTimeSeconds,
            bestTimeGameId
        };

        foreach (KeyValuePair<int, string> difficulty in BestTimeColumns)
        {
            DailyChallengeRecord bestForDifficulty = wins
                .Where(row => row.Difficulty == difficulty.Key)
                .OrderBy(row => row.ElapsedSeconds)
                .ThenBy(row => row.Id)
                .FirstOrDefault();
            values.Add(bestForDifficulty == null ? (int?)null : bestForDifficulty.Id);
        }

        string[] columns =
        {
            "FirstStartedAt", "CurrentStreakStartedAt", "CurrentStreak", "LastPlayedAt",
            "LastLostAt", "GamesPlayed", "GamesWon", "BestTimeSeconds", "BestTimeGameId"
        };
        columns = columns.Concat(BestTimeColumns.Values).ToArray();
        string assignments = string.Join(", ", columns.Select(column => column + " = ?"));
        if (database.Execute("UPDATE DailyGameChallengeStats SET " + assignments + " WHERE Id = 1",
                values.ToArray()) != 1)
            throw new InvalidOperationException("Could not update DailyGameChallengeStats row Id=1.");
    }

    private static DateTime ParseCompletedAt(DailyChallengeRecord row)
    {
        DateTime parsed;
        if (!DateTime.TryParse(row.CompletedAt, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out parsed))
            throw new InvalidOperationException(
                "Could not parse CompletedAt for DailyGame_challenge row Id=" + row.Id + ".");
        return parsed;
    }

    private static string ApplicationDatabasePath()
    {
        return Path.Combine(Application.persistentDataPath, "game_history.db");
    }

    private static string GetArgumentValue(string[] arguments, string name)
    {
        for (int i = 0; i < arguments.Length; i++)
        {
            if (arguments[i] == name && i + 1 < arguments.Length)
                return arguments[i + 1];
            if (arguments[i].StartsWith(name + "=", StringComparison.Ordinal))
                return arguments[i].Substring(name.Length + 1).Trim('"');
        }
        return null;
    }
}
