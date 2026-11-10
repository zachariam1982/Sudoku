using System;
using UnityEngine;

/// <summary>Initializes Thermo Sudoku's isolated tables in the shared SQLite database.</summary>
public static class ThermoDatabase
{
    public static void Initialize(SqliteDatabaseConnection database)
    {
        if (database == null) throw new ArgumentNullException(nameof(database));
        if (database.Connection == null)
        {
            Debug.LogError("[ThermoDatabase] Initialization skipped because the shared database is not open.");
            return;
        }

        new SqliteThermoGameHistoryRepository(database).InitializeSchema();
        SqliteThermoGameResultRepository.InitializeSchema(database);
        ThermoDailyChallengeDatabase.Initialize(database);
    }
}
