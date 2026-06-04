using System;
//using System.Diagnostics;
using UnityEngine;

/// <summary>
/// Idle state — puzzle is loaded and displayed but timer has not started.
/// Transitions to Playing the moment the player taps any cell.
/// This gives the player time to study the board before the clock starts.
/// </summary>
public class IdleState : IGameState
{
    private readonly SudokuViewModel _vm;
    private readonly GameStateMachine _machine;

    public IdleState(SudokuViewModel vm, GameStateMachine machine)
    {
        _vm      = vm;
        _machine = machine;
    }

    public void Enter()
    {
        using (new Benchmark("Creating a new Sudoku puzzle")){
            _vm.ResetPuzzle();
        }
        _vm.LivesRemaining.Value = 3;        
        _vm.FirstCellTapped.OnChanged += OnFirstCellTapped;
    }

    public void Update(float deltaTime)
    {
        // Timer does not run in Idle — player studies the board
    }

    public void Exit()
    {
        _vm.FirstCellTapped.OnChanged -= OnFirstCellTapped;
        _vm.FirstCellTapped.Value = false;
        // Reset all game tracking values for a fresh puzzle
        _vm.ElapsedSeconds.Value = 0f;
    }

    private void OnFirstCellTapped(bool tapped)
    {
        if (tapped)
            _machine.TransitionTo(_machine.Playing);
    }
}