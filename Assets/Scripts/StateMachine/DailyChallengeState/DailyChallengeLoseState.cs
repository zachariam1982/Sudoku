/// <summary>Completes the Daily Challenge as a loss and shows its result screen.</summary>
public sealed class DailyChallengeLoseState : IGameState
{
    private readonly DailyChallengeViewModel _viewModel;

    public DailyChallengeLoseState(DailyChallengeViewModel viewModel) => _viewModel = viewModel;

    public void Enter()
    {
        _viewModel.Finish(false);
        _viewModel.IsLost.Value = true;
    }

    public void Update(float deltaTime) { }

    public void Exit() => _viewModel.IsLost.Value = false;
}
