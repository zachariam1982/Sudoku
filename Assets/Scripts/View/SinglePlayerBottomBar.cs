using UnityEngine;

public class SinglePlayerBottomBar : MonoBehaviour
{
    private SudokuViewModel viewModel;
    public void Awake()
    {
        Debug.Log("Inside SinglePlayerBottomBar");
    }
    public void Bind(SudokuViewModel arg)
    {
        viewModel = arg;

        viewModel.IsEraseMode.OnChanged += OnEraseModeChange;
    }

    public void EraseButtonPressed()
    {
        if(viewModel == null) return;

        viewModel.SetEraseModeCommand.Execute();
    }

    public void PencilButtonPressed()
    {
        if(viewModel == null) return;

        viewModel.SetPencilModeCommand.Execute();
    }

    private void OnDestroy()
    {
        if(viewModel == null) return;

        viewModel.IsEraseMode.OnChanged -= OnEraseModeChange;
    }

    private void OnEraseModeChange(bool arg)
    {
        return;
    }
}
