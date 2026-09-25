/// <summary>Active play for New Game. No time, lives, scoring, or persistence.</summary>
public sealed class NewGamePlayingState : IGameState
{
    private readonly BaseViewModel _viewModel;
    private readonly NewGameStateMachine _machine;
    private bool _previousIsComplete;
    private bool _previousPauseRequested;

    public NewGamePlayingState(BaseViewModel viewModel, NewGameStateMachine machine)
    {
        _viewModel = viewModel;
        _machine = machine;
    }

    public void Enter()
    {
        _previousIsComplete = _viewModel.IsComplete.Value;
        _previousPauseRequested = _viewModel.PauseRequested.Value;
        _viewModel.IsComplete.OnChanged += OnBoardComplete;
        _viewModel.PauseRequested.OnChanged += OnPauseRequested;
    }

    public void Update(float deltaTime) { }

    public void Exit()
    {
        _viewModel.IsComplete.OnChanged -= OnBoardComplete;
        _viewModel.PauseRequested.OnChanged -= OnPauseRequested;
        _viewModel.IsComplete.Value = _previousIsComplete;
        _viewModel.PauseRequested.Value = _previousPauseRequested;
    }

    private void OnBoardComplete(bool isComplete)
    {
        if (isComplete) _machine.TransitionTo(_machine.Validating);
    }

    private void OnPauseRequested(bool requested)
    {
        if (requested) _machine.TransitionTo(_machine.Paused);
    }
}
