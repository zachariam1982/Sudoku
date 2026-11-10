using System;

/// <summary>Creates services backed by Irregular Sudoku rules and isolated tables.</summary>
public sealed class IrregularGameVariantFactory : IGameVariantFactory
{
    private readonly SqliteDatabaseConnection _database;
    private readonly IGameHistoryRepository _historyRepository;

    public GameVariant Variant => GameVariant.Irregular;
    public IGameHistoryRepository GameHistoryRepository => _historyRepository;
    public IGameResultRepository GameResultRepository { get; }
    public IDailyChallengeRepository DailyChallengeRepository { get; }
    public IGameSaveSystem GameSaveSystem { get; }

    public IrregularGameVariantFactory(SqliteDatabaseConnection database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _historyRepository = new SqliteIrregularGameHistoryRepository(database);
        GameResultRepository = new SqliteIrregularGameResultRepository(database);
        DailyChallengeRepository = new SqliteIrregularDailyChallengeRepository(database);
        GameSaveSystem = new IrregularSaveSystem();
    }

    public ISudokuModel CreateSudokuModel() => new IrregularSudokuModel(_historyRepository);

    public void InitializeDatabase()
    {
        if (_database.Connection == null)
            throw new InvalidOperationException("Open the shared database before initializing a variant.");
        IrregularDatabase.Initialize(_database);
    }
}
