/// <summary>Waits for the player to tap a cell before starting the challenge timer.</summary>
public sealed class DailyChallengeIdleState : IGameState
{
    private readonly DailyChallengeStateMachine _machine;
    private readonly DailyChallengeViewModel _viewModel;

    public DailyChallengeIdleState(
        DailyChallengeStateMachine machine,
        DailyChallengeViewModel viewModel)
    {
        _machine = machine;
        _viewModel = viewModel;
    }

    public void Enter() => _viewModel.FirstCellTapped.OnChanged += OnFirstCellTapped;

    public void Update(float deltaTime) { }

    public void Exit()
    {
        _viewModel.FirstCellTapped.OnChanged -= OnFirstCellTapped;
        _viewModel.FirstCellTapped.Value = false;
    }

    private void OnFirstCellTapped(bool tapped)
    {
        if (tapped) _machine.TransitionTo(_machine.Playing);
    }
}
