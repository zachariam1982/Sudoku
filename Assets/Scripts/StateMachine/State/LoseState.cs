using System;
/// <summary>
/// Lose state — player ran out of lives.
/// Shows the lose screen with options to retry or new game.
/// </summary>
public class LoseState : IGameState
{
    private readonly SudokuViewModel  _vm;
    private readonly GameStateMachine _machine;

    public LoseState(SudokuViewModel vm, GameStateMachine machine)
    {
        _vm      = vm;
        _machine = machine;
    }

    public void Enter()
    {
        // Stop the timer
        _vm.IsTimerRunning.Value = false;

        // Tell Views to show lose screen
        _vm.IsLost.Value = true;
        // Subscribe to player choices
        _vm.NewGameRequested.OnChanged   += OnNewGameRequested;
        _vm.RetryGameRequested.OnChanged += OnRetryRequested;
    }

    public void Update(float deltaTime)
    {
        // Nothing to update — waiting for player input
    }

    public void Exit()
    {
        _vm.NewGameRequested.OnChanged   -= OnNewGameRequested;
        _vm.RetryGameRequested.OnChanged -= OnRetryRequested;

        _vm.IsLost.Value = false;
        _vm.NewGameRequested.Value   = false;
        _vm.RetryGameRequested.Value = false;
        _vm.ElapsedSeconds.Value = 0f;
    }

    private void OnNewGameRequested(bool requested)
    {
        if (requested){
            _vm?.AddLevel.Execute();
            _machine.TransitionTo(_machine.Idle);
        }
    }

    private void OnRetryRequested(bool requested)
    {
        if (requested)
        {
            // Retry restarts the same puzzle from scratch
            _vm?.DecreaseDifficulty.Execute();
            _machine.TransitionTo(_machine.Idle);
        }
    }
}