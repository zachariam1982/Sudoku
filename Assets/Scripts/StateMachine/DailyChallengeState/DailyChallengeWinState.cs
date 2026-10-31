/// <summary>Shows the result after the Daily Challenge has been won.</summary>
public sealed class DailyChallengeWinState : IGameState
{
    private readonly DailyChallengeViewModel _viewModel;

    public DailyChallengeWinState(DailyChallengeViewModel viewModel) => _viewModel = viewModel;

    public void Enter() => _viewModel.IsWon.Value = true;

    public void Update(float deltaTime) { }

    public void Exit() => _viewModel.IsWon.Value = false;
}
