using UnityEngine;

/// <summary>State machine used only by the standalone New Game mode.</summary>
public sealed class NewGameStateMachine : MonoBehaviour, ISudokuStateMachine
{
    public NewGamePlayingState Playing { get; private set; }
    public NewGameValidatingState Validating { get; private set; }
    public NewGamePausedState Paused { get; private set; }
    public NewGameWinState Win { get; private set; }

    public IGameState CurrentState { get; private set; }
    public bool IsPlaying => CurrentState is NewGamePlayingState;
    public bool IsIdle => false;

    private BaseViewModel _viewModel;
    private bool _isSuspended = true;

    public void Initialise(BaseViewModel viewModel)
    {
        _viewModel = viewModel;
        viewModel.AttachStateMachine(this);
        Playing = new NewGamePlayingState(viewModel, this);
        Validating = new NewGameValidatingState(viewModel, this);
        Paused = new NewGamePausedState(viewModel, this);
        Win = new NewGameWinState(viewModel);
    }

    public void StartPlaying() => TransitionTo(Playing);

    public void TransitionTo(IGameState newState)
    {
        CurrentState?.Exit();
        CurrentState = newState;
        CurrentState.Enter();
        _viewModel.CurrentStateName.Value = CurrentState.GetType().Name;
    }

    public void SetSuspended(bool isSuspended) => _isSuspended = isSuspended;

    private void Update()
    {
        if (!_isSuspended) CurrentState?.Update(Time.deltaTime);
    }
}
