using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Playing state — timer runs, player enters numbers.
/// Transitions to:
///   Validating — when all cells are filled
///   Paused     — when player taps pause
///   Lose       — when lives reach 0
/// </summary>
public class PlayingState : IGameState
{
    private readonly SudokuViewModel  _vm;
    private readonly GameStateMachine _machine;
    private bool prevIsComplete;
    private bool prevPauseRequested;
    private (int, int, bool) prevLastEnteredCell;

    public PlayingState(SudokuViewModel vm, GameStateMachine machine)
    {
        _vm      = vm;
        _machine = machine;
    }

    public void Enter()
    {
        // Save previous state;
        prevIsComplete                 = _vm.IsComplete.Value;
        prevLastEnteredCell            = _vm.LastEnteredCell.Value;
        prevPauseRequested             = _vm.PauseRequested.Value;
        // Subscribe to game events
        _vm.LastEnteredCell.OnChanged += OnCellValueEntered;
        _vm.IsComplete.OnChanged      += OnBoardComplete;
        _vm.PauseRequested.OnChanged  += OnPauseRequested;

    }

    public void Update(float deltaTime)
    {
        // Tick the timer every frame
        _vm.ElapsedSeconds.Value += deltaTime;
    }

    public void Exit()
    {
        _vm.LastEnteredCell.OnChanged -= OnCellValueEntered;
        _vm.IsComplete.OnChanged      -= OnBoardComplete;
        _vm.PauseRequested.OnChanged  -= OnPauseRequested;
        _vm.IsComplete.Value           = prevIsComplete;
        _vm.LastEnteredCell.Value      = prevLastEnteredCell;
        _vm.PauseRequested.Value       = prevPauseRequested;
    }

    // ── Event Handlers ────────────────────────────────────────────────────────

    private void OnCellValueEntered((int row, int col, bool hasConflict) entry)
    {
        if (!entry.hasConflict) return;

        if (_vm.LivesRemaining.Value <= 0){
            GameRecord val = new GameRecord
            {
                Level          = _vm.GetLevel /* expose _model.CurrentLevel via a property */,
                Difficulty     = _vm.GetDifficulty,
                ElapsedSeconds = _vm.ElapsedSeconds.Value,
                LivesRemaining = _vm.LivesRemaining.Value,
                IsWon          = false,
                Points         = 0,
                CompletedAt    = DateTime.Now.ToString("o"),
            };
            
            if(_vm.RetryGameData.id == -1) GameDatabase.Insert(val); //Add into DB the record if it is a new game and not a retry

            _machine.TransitionTo(_machine.Lose);
        }
    }

    private void OnBoardComplete(bool isComplete)
    {
        if (isComplete)
            _machine.TransitionTo(_machine.Validating);
    }

    private void OnPauseRequested(bool requested)
    {
        if (requested)
            _machine.TransitionTo(_machine.Paused);
    }

}