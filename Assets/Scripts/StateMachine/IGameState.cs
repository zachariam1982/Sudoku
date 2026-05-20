/// <summary>
/// Interface every game state must implement.
/// The GameStateMachine calls these at the right time.
/// </summary>
public interface IGameState
{
    /// <summary>Called once when this state becomes active.</summary>
    void Enter();

    /// <summary>Called every frame while this state is active.</summary>
    void Update(float deltaTime);

    /// <summary>Called once when leaving this state.</summary>
    void Exit();
}