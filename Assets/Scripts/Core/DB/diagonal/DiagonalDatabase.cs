using System;
using UnityEngine;

/// <summary>Initializes Diagonal Sudoku's isolated tables in the shared SQLite database.</summary>
public static class DiagonalDatabase
{
    public static void Initialize(SqliteDatabaseConnection database)
    {
        if (database == null) throw new ArgumentNullException(nameof(database));
        if (database.Connection == null)
        {
            Debug.LogError("[DiagonalDatabase] Initialization skipped because the shared database is not open.");
            return;
        }

        new SqliteDiagonalGameHistoryRepository(database).InitializeSchema();
        SqliteDiagonalGameResultRepository.InitializeSchema(database);
        DiagonalDailyChallengeDatabase.Initialize(database);
    }
}
