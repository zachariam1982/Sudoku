using UnityEngine;

/// <summary>
/// Owns all game states and manages transitions between them.
/// Attach to the Canvas alongside GameContext.
/// 
/// The ViewModel is the single source of truth for state data.
/// The StateMachine is the single source of truth for state transitions.
/// </summary>
public class JourneyStateMachine : MonoBehaviour, ISudokuStateMachine
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static JourneyStateMachine Instance { get; private set; }

    // ── State instances ───────────────────────────────────────────────────────
    public JourneyIdleState       Idle       { get; private set; }
    public JourneyPlayingState    Playing    { get; private set; }
    public JourneyPausedState     Paused     { get; private set; }
    public JourneyValidatingState Validating { get; private set; }
    public JourneyWinState        Win        { get; private set; }
    public JourneyLoseState       Lose       { get; private set; }

    // ── Current state ─────────────────────────────────────────────────────────
    private IGameState _currentState;
    public  IGameState CurrentState => _currentState;
    public bool IsPlaying => _currentState is JourneyPlayingState;
    public bool IsIdle => _currentState is JourneyIdleState;
    private bool _isSuspended;

    // ── Dependencies ──────────────────────────────────────────────────────────
    private JourneyViewModel _viewModel;

    // ── Initialise ────────────────────────────────────────────────────────────

    void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Called by GameContext after ViewModel is created.
    /// Creates all states and starts in Idle.
    /// </summary>
    public void Initialise(JourneyViewModel viewModel)
    {
        _viewModel = viewModel;
        viewModel.AttachStateMachine(this);

        // Create all state instances, passing ViewModel and machine reference
        Idle       = new JourneyIdleState(viewModel, this);
        Playing    = new JourneyPlayingState(viewModel, this);
        Paused     = new JourneyPausedState(viewModel, this);
        Validating = new JourneyValidatingState(viewModel, this);
        Win        = new JourneyWinState(viewModel, this);
        Lose       = new JourneyLoseState(viewModel, this);

        TransitionTo(Idle);
    }

    /// <summary>
    /// Transitions to a new state.
    /// Calls Exit() on current state and Enter() on new state.
    /// </summary>
    public void TransitionTo(IGameState newState)
    {
        if (_currentState != null)
        {
            _currentState.Exit();
        }

        _currentState = newState;
        _currentState.Enter();

        // Publish current state name to ViewModel so Views can react
        _viewModel.CurrentStateName.Value = _currentState.GetType().Name;
    }

    public void SetSuspended(bool isSuspended)
    {
        _isSuspended = isSuspended;
    }

    public void StartPlaying() => TransitionTo(Playing);

    void Update()
    {
        if (_isSuspended) return;

        _currentState?.Update(Time.deltaTime);
    }
}
