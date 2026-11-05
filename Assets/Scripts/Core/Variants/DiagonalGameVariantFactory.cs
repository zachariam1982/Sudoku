using System;

/// <summary>Creates services backed by Diagonal Sudoku rules and tables.</summary>
public sealed class DiagonalGameVariantFactory : IGameVariantFactory
{
    private readonly SqliteDatabaseConnection _database;
    private readonly IGameHistoryRepository _historyRepository;

    public GameVariant Variant => GameVariant.Diagonal;
    public IGameHistoryRepository GameHistoryRepository => _historyRepository;
    public IGameResultRepository GameResultRepository { get; }
    public IDailyChallengeRepository DailyChallengeRepository { get; }
    public IGameSaveSystem GameSaveSystem { get; }

    public DiagonalGameVariantFactory(SqliteDatabaseConnection database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _historyRepository = new SqliteDiagonalGameHistoryRepository(database);
        GameResultRepository = new SqliteDiagonalGameResultRepository(database);
        DailyChallengeRepository = new SqliteDiagonalDailyChallengeRepository(database);
        GameSaveSystem = new DiagonalSaveSystem();
    }

    public ISudokuModel CreateSudokuModel() => new DiagonalSudokuModel(_historyRepository);

    public void InitializeDatabase()
    {
        if (_database.Connection == null)
            throw new InvalidOperationException("Open the shared database before initializing a variant.");

        DiagonalDatabase.Initialize(_database);
    }
}
