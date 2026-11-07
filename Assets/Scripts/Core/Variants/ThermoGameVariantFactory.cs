using System;

/// <summary>Creates services backed by Thermo Sudoku rules and isolated tables.</summary>
public sealed class ThermoGameVariantFactory : IGameVariantFactory
{
    private readonly SqliteDatabaseConnection _database;
    private readonly IGameHistoryRepository _historyRepository;

    public GameVariant Variant => GameVariant.Thermo;
    public IGameHistoryRepository GameHistoryRepository => _historyRepository;
    public IGameResultRepository GameResultRepository { get; }
    public IDailyChallengeRepository DailyChallengeRepository { get; }
    public IGameSaveSystem GameSaveSystem { get; }

    public ThermoGameVariantFactory(SqliteDatabaseConnection database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _historyRepository = new SqliteThermoGameHistoryRepository(database);
        GameResultRepository = new SqliteThermoGameResultRepository(database);
        DailyChallengeRepository = new SqliteThermoDailyChallengeRepository(database);
        GameSaveSystem = new ThermoSaveSystem();
    }

    public ISudokuModel CreateSudokuModel() => new ThermoSudokuModel(_historyRepository);

    public void InitializeDatabase()
    {
        if (_database.Connection == null)
            throw new InvalidOperationException("Open the shared database before initializing a variant.");
        ThermoDatabase.Initialize(_database);
    }
}
