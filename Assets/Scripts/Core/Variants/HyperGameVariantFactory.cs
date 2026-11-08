using System;

/// <summary>Creates services backed by Hyper Sudoku rules and isolated tables.</summary>
public sealed class HyperGameVariantFactory : IGameVariantFactory
{
    private readonly SqliteDatabaseConnection _database;
    private readonly IGameHistoryRepository _historyRepository;

    public GameVariant Variant => GameVariant.Hyper;
    public IGameHistoryRepository GameHistoryRepository => _historyRepository;
    public IGameResultRepository GameResultRepository { get; }
    public IDailyChallengeRepository DailyChallengeRepository { get; }
    public IGameSaveSystem GameSaveSystem { get; }

    public HyperGameVariantFactory(SqliteDatabaseConnection database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _historyRepository = new SqliteHyperGameHistoryRepository(database);
        GameResultRepository = new SqliteHyperGameResultRepository(database);
        DailyChallengeRepository = new SqliteHyperDailyChallengeRepository(database);
        GameSaveSystem = new HyperSaveSystem();
    }

    public ISudokuModel CreateSudokuModel() => new HyperSudokuModel(_historyRepository);

    public void InitializeDatabase()
    {
        if (_database.Connection == null)
            throw new InvalidOperationException("Open the shared database before initializing a variant.");
        HyperDatabase.Initialize(_database);
    }
}
