using System;
using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    private BaseViewModel viewModel;
    private Action<bool>    hideHudAction;
    private Action<bool>    pencilModeChangedAction;
    private Button          pencilModeButton;
    private Button          undoButton;
    private Button          redoButton;
    private bool            undoButtonWasInteractable;
    private bool            redoButtonWasInteractable;
    private ColorBlock      defaultPencilButtonColors;
    private bool            controlDefaultsCaptured;

    public void Bind(BaseViewModel arg)
    {
        if (ReferenceEquals(viewModel, arg))
        {
            if (arg != null)
                UpdatePencilModeVisual(arg.IsPencilMode.Value);
            return;
        }
        Unbind();

        viewModel = arg;
        if (viewModel == null)
            return;

        hideHudAction = (arg) => this.gameObject.SetActive(arg);
        pencilModeButton = transform.Find("PencilBlock")?.GetComponent<Button>();
        undoButton = transform.Find("UndoBlock/Undo")?.GetComponent<Button>();
        redoButton = transform.Find("RedoBlock/Redo")?.GetComponent<Button>();
        if (!controlDefaultsCaptured)
        {
            if (pencilModeButton != null)
                defaultPencilButtonColors = pencilModeButton.colors;
            undoButtonWasInteractable = undoButton != null && undoButton.interactable;
            redoButtonWasInteractable = redoButton != null && redoButton.interactable;
            controlDefaultsCaptured = true;
        }

        viewModel.HideHUD.OnChanged     += hideHudAction;
        pencilModeChangedAction = UpdatePencilModeVisual;
        viewModel.IsPencilMode.OnChanged += pencilModeChangedAction;
        UpdatePencilModeVisual(viewModel.IsPencilMode.Value);

        if(SOSAdDialog.Instance != null) SOSAdDialog.Instance.Bind(viewModel);
    }

    public void Unbind()
    {
        if (viewModel == null)
            return;

        if (hideHudAction != null)
            viewModel.HideHUD.OnChanged -= hideHudAction;
        if (pencilModeChangedAction != null)
            viewModel.IsPencilMode.OnChanged -= pencilModeChangedAction;

        viewModel = null;
        hideHudAction = null;
        pencilModeChangedAction = null;
    }

    private void UpdatePencilModeVisual(bool isActive)
    {
        if (pencilModeButton != null)
        {
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

        if (undoButton != null)
            undoButton.interactable = !isActive && undoButtonWasInteractable;
        if (redoButton != null)
            redoButton.interactable = !isActive && redoButtonWasInteractable;
    }

    public void EraseButtonPressed() => viewModel?.SetEraseModeCommand.Execute();
    public void PencilButtonPressed() => viewModel?.SetPencilModeCommand.Execute();
    public void AutoFillCandidatesButtonPressed() => viewModel?.AutoFillCandidatesCommand.Execute();
    public void OnSOSPressed() => NumberPicker.Instance?.OnSOSButtonPressed();
    public void UndoButtonPressed() => viewModel?.UndoCommand.Execute();
    public void RedoButtonPressed() => viewModel?.RedoCommand.Execute();
    public void PauseButtonPressed() => viewModel?.PauseCommand.Execute();
    private void OnDestroy() => Unbind();
}
