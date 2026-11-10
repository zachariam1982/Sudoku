/// <summary>Runs the Daily Challenge timer and listens for pause, completion, and loss.</summary>
public sealed class DailyChallengePlayingState : IGameState
{
    private readonly DailyChallengeStateMachine _machine;
    private readonly DailyChallengeViewModel _viewModel;

    public DailyChallengePlayingState(
        DailyChallengeStateMachine machine,
        DailyChallengeViewModel viewModel)
    {
        _machine = machine;
        _viewModel = viewModel;
    }

    public void Enter()
    {
        _viewModel.IsComplete.OnChanged += OnComplete;
        _viewModel.PauseRequested.OnChanged += OnPauseRequested;
        _viewModel.LivesRemaining.OnChanged += OnLivesRemainingChanged;
        _viewModel.IsTimerRunning.Value = true;
    }

    public void Update(float deltaTime) => _viewModel.ElapsedSeconds.Value += deltaTime;

    public void Exit()
    {
        _viewModel.IsComplete.OnChanged -= OnComplete;
        _viewModel.PauseRequested.OnChanged -= OnPauseRequested;
        _viewModel.LivesRemaining.OnChanged -= OnLivesRemainingChanged;
        _viewModel.IsTimerRunning.Value = false;
    }

    private void OnComplete(bool complete)
    {
        if (complete) _machine.TransitionTo(_machine.Validating);
    }

    private void OnPauseRequested(bool requested)
    {
        if (requested) _machine.TransitionTo(_machine.Paused);
    }

    private void OnLivesRemainingChanged(int lives)
    {
        if (lives <= 0) _machine.TransitionTo(_machine.Lose);
    }
}
