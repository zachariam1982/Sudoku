using System;
using UnityEngine;

public class HUD : MonoBehaviour
{
    private BaseViewModel viewModel;
    private Action<bool>    hideHudAction;
    public void Bind(BaseViewModel arg)
    {
        if (ReferenceEquals(viewModel, arg)) return;
        if (viewModel != null && hideHudAction != null)
            viewModel.HideHUD.OnChanged -= hideHudAction;

        viewModel = arg;
        hideHudAction = (arg) => this.gameObject.SetActive(arg);
 
        viewModel.HideHUD.OnChanged     += hideHudAction;

        if(SOSAdDialog.Instance != null) SOSAdDialog.Instance.Bind(viewModel);
    }
    public void EraseButtonPressed() => viewModel?.SetEraseModeCommand.Execute();
    public void PencilButtonPressed() => viewModel?.SetPencilModeCommand.Execute();
    public void AutoFillCandidatesButtonPressed() => viewModel?.AutoFillCandidatesCommand.Execute();
    public void OnSOSPressed() => viewModel?.SOSCommand.Execute();
    public void UndoButtonPressed() => viewModel?.UndoCommand.Execute();
    public void PauseButtonPressed() => viewModel?.PauseCommand.Execute();
    private void OnDestroy()
    {
        if(viewModel == null) return;

        viewModel.HideHUD.OnChanged     -= hideHudAction;
    }
}
