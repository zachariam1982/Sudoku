/// <summary>
/// Paused state — timer is frozen, board is dimmed.
/// Transitions to Playing when player resumes.
/// </summary>
public class PausedState : IGameState
{
    private readonly SudokuViewModel  _vm;
    private readonly GameStateMachine _machine;

    public PausedState(SudokuViewModel vm, GameStateMachine machine)
    {
        _vm      = vm;
        _machine = machine;
    }

    public void Enter()
    {
        _vm.ResumeRequested.OnChanged += OnResumeRequested;
    }

    public void Update(float deltaTime)
    {
        // Timer does not tick while paused
    }

    public void Exit()
    {
        _vm.ResumeRequested.OnChanged -= OnResumeRequested;
        _vm.PauseRequested.Value  = false;
        _vm.ResumeRequested.Value = false;
    }

    private void OnResumeRequested(bool requested)
    {
        if (requested)
            _machine.TransitionTo(_machine.Playing);
    }
}