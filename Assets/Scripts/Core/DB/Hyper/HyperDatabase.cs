using System;
using UnityEngine;

/// <summary>Initializes Hyper Sudoku's isolated tables in the shared SQLite database.</summary>
public static class HyperDatabase
{
    public static void Initialize(SqliteDatabaseConnection database)
    {
        if (database == null) throw new ArgumentNullException(nameof(database));
        if (database.Connection == null)
        {
            Debug.LogError("[HyperDatabase] Initialization skipped because the shared database is not open.");
            return;
        }

        new SqliteHyperGameHistoryRepository(database).InitializeSchema();
        SqliteHyperGameResultRepository.InitializeSchema(database);
        HyperDailyChallengeDatabase.Initialize(database);
    }
}
