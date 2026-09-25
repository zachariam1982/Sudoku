/// <summary>Waits for the player to acknowledge the New Game completion dialog.</summary>
public sealed class NewGameWinState : IGameState
{
    private readonly BaseViewModel _viewModel;

    public NewGameWinState(BaseViewModel viewModel) => _viewModel = viewModel;

    public void Enter() => _viewModel.IsWon.Value = true;
    public void Update(float deltaTime) { }
    public void Exit() => _viewModel.IsWon.Value = false;
}
