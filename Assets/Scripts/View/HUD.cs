using System;
using UnityEngine;

public class HUD : MonoBehaviour
{
    private SudokuViewModel viewModel;
    private Action<bool>    hideHudAction;
    public void Bind(SudokuViewModel arg)
    {
        viewModel = arg;
        hideHudAction = (arg) => this.gameObject.SetActive(arg);

        viewModel.IsEraseMode.OnChanged += OnEraseModeChange;
        viewModel.HideHUD.OnChanged     += hideHudAction;

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
        viewModel.HideHUD.OnChanged     -= hideHudAction;
    }

    private void OnEraseModeChange(bool arg)
    {
        return;
    }
}
