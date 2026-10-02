using System;
using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    private BaseViewModel viewModel;
    private Action<bool>    hideHudAction;
    private Action<bool>    pencilModeChangedAction;
    private Button          pencilModeButton;
    private ColorBlock      defaultPencilButtonColors;

    public void Bind(BaseViewModel arg)
    {
        if (ReferenceEquals(viewModel, arg)) return;
        if (viewModel != null)
        {
            if (hideHudAction != null)
                viewModel.HideHUD.OnChanged -= hideHudAction;
            if (pencilModeChangedAction != null)
                viewModel.IsPencilMode.OnChanged -= pencilModeChangedAction;
        }

        viewModel = arg;
        hideHudAction = (arg) => this.gameObject.SetActive(arg);
        pencilModeButton = transform.Find("PencilBlock")?.GetComponent<Button>();
        if (pencilModeButton != null)
            defaultPencilButtonColors = pencilModeButton.colors;

        viewModel.HideHUD.OnChanged     += hideHudAction;
        pencilModeChangedAction = UpdatePencilModeVisual;
        viewModel.IsPencilMode.OnChanged += pencilModeChangedAction;
        UpdatePencilModeVisual(viewModel.IsPencilMode.Value);

        if(SOSAdDialog.Instance != null) SOSAdDialog.Instance.Bind(viewModel);
    }

    private void UpdatePencilModeVisual(bool isActive)
    {
        if (pencilModeButton == null) return;

        ColorBlock colors = defaultPencilButtonColors;
        if (isActive)
        {
            // Keep Pencil's active state visible after the button press ends.
            colors.normalColor = new Color32(255, 197, 61, 255);
            colors.highlightedColor = new Color32(255, 220, 125, 255);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color32(220, 151, 27, 255);
        }
        pencilModeButton.colors = colors;
    }

    public void EraseButtonPressed() => viewModel?.SetEraseModeCommand.Execute();
    public void PencilButtonPressed() => viewModel?.SetPencilModeCommand.Execute();
    public void AutoFillCandidatesButtonPressed() => viewModel?.AutoFillCandidatesCommand.Execute();
    public void OnSOSPressed() => viewModel?.SOSCommand.Execute();
    public void UndoButtonPressed() => viewModel?.UndoCommand.Execute();
    public void RedoButtonPressed() => viewModel?.RedoCommand.Execute();
    public void PauseButtonPressed() => viewModel?.PauseCommand.Execute();
    private void OnDestroy()
    {
        if (viewModel == null) return;

        if (hideHudAction != null)
            viewModel.HideHUD.OnChanged -= hideHudAction;
        if (pencilModeChangedAction != null)
            viewModel.IsPencilMode.OnChanged -= pencilModeChangedAction;
    }
}
