using System;

/// <summary>Creates services backed by Killer Sudoku rules and isolated tables.</summary>
public sealed class KillerGameVariantFactory : IGameVariantFactory
{
    private readonly SqliteDatabaseConnection _database;
    private readonly IGameHistoryRepository _historyRepository;

    public GameVariant Variant => GameVariant.Killer;
    public IGameHistoryRepository GameHistoryRepository => _historyRepository;
    public IGameResultRepository GameResultRepository { get; }
    public IDailyChallengeRepository DailyChallengeRepository { get; }
    public IGameSaveSystem GameSaveSystem { get; }

    public KillerGameVariantFactory(SqliteDatabaseConnection database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _historyRepository = new SqliteKillerGameHistoryRepository(database);
        GameResultRepository = new SqliteKillerGameResultRepository(database);
        DailyChallengeRepository = new SqliteKillerDailyChallengeRepository(database);
        GameSaveSystem = new KillerSaveSystem();
    }

    public ISudokuModel CreateSudokuModel() => new KillerSudokuModel(_historyRepository);

    public void InitializeDatabase()
    {
        if (_database.Connection == null)
            throw new InvalidOperationException("Open the shared database before initializing a variant.");
        KillerDatabase.Initialize(_database);
    }
}
