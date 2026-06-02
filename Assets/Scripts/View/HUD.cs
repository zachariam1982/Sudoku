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

        if(SOSAdDialog.Instance != null) SOSAdDialog.Instance.Bind(viewModel);
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
    public void OnSOSPressed()
    {
        if (viewModel == null) return;

        viewModel.SOSCommand.Execute();
    }
    public void UndoButtonPressed()
    {
        if(viewModel == null) return;

        viewModel.UndoCommand.Execute();
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
