using UnityEngine;

/// <summary>
/// Owns all game states and manages transitions between them.
/// Attach to the Canvas alongside GameContext.
/// 
/// The ViewModel is the single source of truth for state data.
/// The StateMachine is the single source of truth for state transitions.
/// </summary>
public class GameStateMachine : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static GameStateMachine Instance { get; private set; }

    // ── State instances ───────────────────────────────────────────────────────
    public IdleState       Idle       { get; private set; }
    public PlayingState    Playing    { get; private set; }
    public PausedState     Paused     { get; private set; }
    public ValidatingState Validating { get; private set; }
    public WinState        Win        { get; private set; }
    public LoseState       Lose       { get; private set; }

    // ── Current state ─────────────────────────────────────────────────────────
    private IGameState _currentState;
    public  IGameState CurrentState => _currentState;

    // ── Dependencies ──────────────────────────────────────────────────────────
    private SudokuViewModel _viewModel;

    // ── Initialise ────────────────────────────────────────────────────────────

    void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Called by GameContext after ViewModel is created.
    /// Creates all states and starts in Idle.
    /// </summary>
    public void Initialise(SudokuViewModel viewModel)
    {
        _viewModel = viewModel;

        // Create all state instances, passing ViewModel and machine reference
        Idle       = new IdleState(viewModel, this);
        Playing    = new PlayingState(viewModel, this);
        Paused     = new PausedState(viewModel, this);
        Validating = new ValidatingState(viewModel, this);
        Win        = new WinState(viewModel, this);
        Lose       = new LoseState(viewModel, this);

        // Start in Idle
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

    void Update()
    {
        _currentState?.Update(Time.deltaTime);
    }
}