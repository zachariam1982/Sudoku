using UnityEngine;

public class HUD : MonoBehaviour
{
    private SudokuViewModel viewModel;
    public void Awake()
    {
        Debug.Log("Inside HUD");
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
    public void UndoButtonPressed()
    {
        if(viewModel == null) return;


        var t = viewModel.getPreviousValues();
        if(t.Item1 == -1 || t.Item2 == -1 || t.Item3 == -1) return;

        viewModel.UndoCommand.Execute(t);
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
