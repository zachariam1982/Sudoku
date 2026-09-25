/// <summary>Pause state used only by New Game.</summary>
public sealed class NewGamePausedState : IGameState
{
    private readonly BaseViewModel _viewModel;
    private readonly NewGameStateMachine _machine;

    public NewGamePausedState(BaseViewModel viewModel, NewGameStateMachine machine)
    {
        _viewModel = viewModel;
        _machine = machine;
    }

    public void Enter() => _viewModel.ResumeRequested.OnChanged += OnResumeRequested;
    public void Update(float deltaTime) { }

    public void Exit()
    {
        _viewModel.ResumeRequested.OnChanged -= OnResumeRequested;
        _viewModel.PauseRequested.Value = false;
        _viewModel.ResumeRequested.Value = false;
    }

    private void OnResumeRequested(bool requested)
    {
        if (requested) _machine.TransitionTo(_machine.Playing);
    }
}
