/// <summary>Briefly validates a completed board before declaring a Daily Challenge win.</summary>
public sealed class DailyChallengeValidatingState : IGameState
{
    private const float ValidationDuration = 0.8f;
    private readonly DailyChallengeStateMachine _machine;
    private readonly DailyChallengeViewModel _viewModel;
    private float _elapsed;

    public DailyChallengeValidatingState(
        DailyChallengeStateMachine machine,
        DailyChallengeViewModel viewModel)
    {
        _machine = machine;
        _viewModel = viewModel;
    }

    public void Enter()
    {
        _elapsed = 0f;
        _viewModel.IsValidating.Value = true;
    }

    public void Update(float deltaTime)
    {
        _elapsed += deltaTime;
        if (_elapsed < ValidationDuration) return;

        _viewModel.IsValidating.Value = false;
        if (_viewModel.IsBoardValid.Value)
        {
            _viewModel.Finish(true);
            _machine.TransitionTo(_machine.Win);
        }
        else
        {
            _machine.TransitionTo(_machine.Playing);
        }
    }

    public void Exit() => _viewModel.IsValidating.Value = false;
}
