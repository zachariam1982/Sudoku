public interface ISudokuStateMachine
{
    IGameState CurrentState { get; }
    bool IsPlaying { get; }
    bool IsIdle { get; }
    void StartPlaying();
}
