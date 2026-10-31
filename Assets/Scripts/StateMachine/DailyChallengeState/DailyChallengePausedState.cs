/// <summary>Freezes play until the player resumes the Daily Challenge.</summary>
public sealed class DailyChallengePausedState : IGameState
{
    private readonly DailyChallengeStateMachine _machine;
    private readonly DailyChallengeViewModel _viewModel;

    public DailyChallengePausedState(
        DailyChallengeStateMachine machine,
        DailyChallengeViewModel viewModel)
    {
        _machine = machine;
        _viewModel = viewModel;
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
