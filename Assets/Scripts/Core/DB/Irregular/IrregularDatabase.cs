using System;
using UnityEngine;

/// <summary>Initializes Irregular Sudoku's isolated tables in the shared SQLite database.</summary>
public static class IrregularDatabase
{
    public static void Initialize(SqliteDatabaseConnection database)
    {
        if (database == null) throw new ArgumentNullException(nameof(database));
        if (database.Connection == null)
        {
            Debug.LogError("[IrregularDatabase] Initialization skipped because the shared database is not open.");
            return;
        }

        new SqliteIrregularGameHistoryRepository(database).InitializeSchema();
        SqliteIrregularGameResultRepository.InitializeSchema(database);
        IrregularDailyChallengeDatabase.Initialize(database);
    }
}
