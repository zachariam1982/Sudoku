/// <summary>Validates a completed New Game board before showing its win dialog.</summary>
public sealed class NewGameValidatingState : IGameState
{
    private const float ValidationDuration = 0.8f;
    private readonly BaseViewModel _viewModel;
    private readonly NewGameStateMachine _machine;
    private float _elapsed;

    public NewGameValidatingState(BaseViewModel viewModel, NewGameStateMachine machine)
    {
        _viewModel = viewModel;
        _machine = machine;
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

        _machine.TransitionTo(
            _viewModel.IsBoardValid.Value ? _machine.Win : _machine.Playing);
    }

    public void Exit() => _viewModel.IsValidating.Value = false;
}
