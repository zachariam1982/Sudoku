using System;

/// <summary>Creates services backed by Classic Sudoku rules and tables.</summary>
public sealed class ClassicGameVariantFactory : IGameVariantFactory
{
    private readonly SqliteDatabaseConnection _database;
    private readonly SqliteGameHistoryRepository _historyRepository;

    public GameVariant Variant => GameVariant.Classic;
    public IGameHistoryRepository GameHistoryRepository => _historyRepository;
    public IGameResultRepository GameResultRepository { get; }
    public IDailyChallengeRepository DailyChallengeRepository { get; }
    public IGameSaveSystem GameSaveSystem { get; }

    public ClassicGameVariantFactory(SqliteDatabaseConnection database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _historyRepository = new SqliteGameHistoryRepository(database);
        GameResultRepository = new SqliteClassicGameResultRepository(database);
        DailyChallengeRepository = new SqliteDailyChallengeRepository(database);
        GameSaveSystem = new ClassicSaveSystem();
    }

    public ISudokuModel CreateSudokuModel() => new ClassicSudokuModel(_historyRepository);

    public void InitializeDatabase()
    {
        if (_database.Connection == null)
            throw new InvalidOperationException("Open the shared database before initializing a variant.");

        _historyRepository.InitializeSchema();
        SqliteClassicGameResultRepository.InitializeSchema(_database);
        DailyChallengeDatabase.Initialize(_database);
    }
}
