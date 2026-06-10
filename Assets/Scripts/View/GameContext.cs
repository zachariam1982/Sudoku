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

    void Awake()
    {
        // Create the ViewModel (loads starting puzzle automatically)
        ViewModel = new SudokuViewModel();
        
        GetComponent<SudokuGrid>()?.Bind(ViewModel);
        GetComponent<ErrorMessage>()?.Bind(ViewModel);
        GetComponentInChildren<NumberPicker>()?.Bind(ViewModel);
        GetComponentInChildren<GameStateView>()?.Bind(ViewModel);

        User.Instance.ViewModel = ViewModel;
        GameStateMachine.Instance.Initialise(ViewModel);
        User.Instance.TryLoadSave();
    }
}