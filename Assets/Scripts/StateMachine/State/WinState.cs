using System;
/// <summary>
/// Win state — puzzle solved correctly.
/// Calculates star rating based on time and mistakes, then shows win screen.
/// 
/// Star rating rules:
///   3 stars — 0 or 1 mistakes AND completed under target time
///   2 stars — 2 mistakes OR over target time
///   1 star  — 3 mistakes (barely survived with last life)
/// </summary>
public class WinState : IGameState
{
    private readonly SudokuViewModel  _vm;
    private readonly GameStateMachine _machine;

    // Target time in seconds for 3 stars (treated as Medium difficulty)
    private const float TargetTimeSeconds = 600f; // 10 minutes

    public WinState(SudokuViewModel vm, GameStateMachine machine)
    {
        _vm      = vm;
        _machine = machine;
    }

    public void Enter()
    {
        // Stop the timer
        _vm.IsTimerRunning.Value = false;

        // Tell Views to show the win screen
        _vm.IsWon.Value = true;

        // Subscribe to new game request
        _vm.NewGameRequested.OnChanged += OnNewGameRequested;
    }

    public void Update(float deltaTime)
    {
        // Nothing to update — waiting for player to tap New Game
    }

    public void Exit()
    {
        _vm.IsWon.Value = false;
        _vm.NewGameRequested.Value = false;
        _vm.NewGameRequested.OnChanged -= OnNewGameRequested;
        _vm.ElapsedSeconds.Value = 0f;
    }

    private void OnNewGameRequested(bool requested)
    {
        if (requested){
            _vm?.NextLevel.Execute();
            _vm?.IncreaseDifficulty.Execute();
            if (_vm != null  && _vm.RetryGameData.id != -1)
            {
                _vm.RetryGameData = (-1,-1,-1,-1);
            }
            _machine.TransitionTo(_machine.Idle);
        }
    }
}