using System;
using UnityEngine;

/// <summary>Initializes Killer Sudoku's isolated tables in the shared SQLite database.</summary>
public static class KillerDatabase
{
    public static void Initialize(SqliteDatabaseConnection database)
    {
        if (database == null) throw new ArgumentNullException(nameof(database));
        if (database.Connection == null)
        {
            Debug.LogError("[KillerDatabase] Initialization skipped because the shared database is not open.");
            return;
        }

        new SqliteKillerGameHistoryRepository(database).InitializeSchema();
        SqliteKillerGameResultRepository.InitializeSchema(database);
        KillerDailyChallengeDatabase.Initialize(database);
    }
}
