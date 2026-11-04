/// <summary>Creates the model, repositories, and save system for one game variant.</summary>
public interface IGameVariantFactory
{
    GameVariant Variant { get; }
    IGameHistoryRepository GameHistoryRepository { get; }
    IGameResultRepository GameResultRepository { get; }
    IDailyChallengeRepository DailyChallengeRepository { get; }
    IGameSaveSystem GameSaveSystem { get; }

    ISudokuModel CreateSudokuModel();
    void InitializeDatabase();
}
