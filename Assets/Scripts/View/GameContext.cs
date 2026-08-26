using UnityEngine;

/// <summary>
/// The composition root of the MVVM setup.
/// Creates the shared SudokuViewModel and injects it into every View.
/// Attach this to the Canvas alongside GridBuilder.
/// 
/// This is the ONLY place where the ViewModel is instantiated.
/// </summary>
public class GameContext : MonoBehaviour
{
    /// <summary>Shared ViewModel — accessible by GridBuilder after Awake.</summary>
    public SudokuViewModel ViewModel { get; private set; }
    public static int cnt = 0;

    void Awake()
    {
        // Create the ViewModel (loads starting puzzle automatically)
        ViewModel = new SudokuViewModel();
        
        SudokuGrid grid = GetComponent<SudokuGrid>();
        if (grid != null) grid.Bind(ViewModel);

        ErrorMessage error = GetComponent<ErrorMessage>();
        if (error != null) error.Bind(ViewModel);

        NumberPicker picker = GetComponentInChildren<NumberPicker>();
        if (picker != null) picker.Bind(ViewModel);

        GameStateView stateView = GetComponentInChildren<GameStateView>();
        if (stateView != null) stateView.Bind(ViewModel);

        StatsPanel stats = GetComponentInChildren<StatsPanel>();
        if (stats != null) stats.Bind(ViewModel);

        User.Instance.ViewModel = ViewModel;
        GameStateMachine.Instance.Initialise(ViewModel);
        User.Instance.TryLoadSave();

    }

    private void Start()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR

        if (YouTubePlatformManager.Instance != null) YouTubePlatformManager.Instance.SendGameReady();
        else Debug.LogError("[YouTube] YouTubePlatformManager not found.");

        #endif
    }
}