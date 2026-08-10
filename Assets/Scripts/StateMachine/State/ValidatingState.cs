using System.Globalization;

/// <summary>
/// Validating state — all cells are filled.
/// Does a final validation check then transitions to Win or back to Playing.
/// This is a brief intermediary state that gives the game a moment to
/// check the solution and play a validation animation before showing the win screen.
/// </summary>
public class ValidatingState : IGameState
{
    private readonly SudokuViewModel  _vm;
    private readonly GameStateMachine _machine;

    private float _elapsed;
    private const float ValidationDuration = 0.8f; // seconds to show validation animation

    public ValidatingState(SudokuViewModel vm, GameStateMachine machine)
    {
        _vm      = vm;
        _machine = machine;
    }

    public void Enter()
    {
        _elapsed = 0f;
        _vm.IsValidating.Value = true;
    }

    public void Update(float deltaTime)
    {
        _elapsed += deltaTime;

        // Wait for validation animation to finish before transitioning
        if (_elapsed >= ValidationDuration)
        {
            bool isValid = _vm.IsBoardValid.Value;

            if (isValid){
                GameRecord val = new GameRecord
                {
                    Level          = _vm.GetLevel /* expose _model.CurrentLevel via a property */,
                    Difficulty     = _vm.GetDifficulty,
                    ElapsedSeconds = _vm.ElapsedSeconds.Value,
                    LivesRemaining = _vm.LivesRemaining.Value,
                    IsWon          = true,
                    Points         = ScoringSystem.Calculate( (SudokuDifficulty)_vm.GetDifficulty, _vm.ElapsedSeconds.Value, _vm.Penalties),
                    CompletedAt    = System.DateTime.Now.ToString("o"),
                };

                if(_vm != null && _vm.RetryGameData.id != -1 && _vm.RetryGameData.difficulty != -1 && _vm.RetryGameData.level != -1)
                {
                    val.Id         = _vm.RetryGameData.id;
                    val.Difficulty = _vm.RetryGameData.difficulty;
                    val.Level      = _vm.RetryGameData.level;
                    PlayerSettings.Instance.UpdateSettings(PlayerSettings.TotalPoints, _vm.RetryGameData.points, val.Points);
                    int wons = PlayerSettings.Instance.GetSetting(PlayerSettings.TotalWins);
                    PlayerSettings.Instance.UpdateSettings(PlayerSettings.TotalWins, wons, wons + 1);
                    
                    GameDatabase.Update(val);
                }
                else
                {
                    PlayerSettings.Instance.SavePlayerPref(val);
                    GameDatabase.Insert(val);
                }

                _machine.TransitionTo(_machine.Win);
            }
            else
            {
                // Board is complete but has errors — send back to Playing
                _vm.IsValidating.Value = false;
                _machine.TransitionTo(_machine.Playing);
            }
        }
    }

    public void Exit()
    {
        _vm.IsValidating.Value = false;
    }
}