using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    private BaseViewModel viewModel;
    private Action<bool>    hideHudAction;
    [SerializeField] private Sprite redoSprite;
    private GameObject redoBlock;
    public void Bind(BaseViewModel arg)
    {
        if (ReferenceEquals(viewModel, arg)) return;
        if (viewModel != null && hideHudAction != null)
            viewModel.HideHUD.OnChanged -= hideHudAction;

        viewModel = arg;
        EnsureRedoButton();
        hideHudAction = (arg) => this.gameObject.SetActive(arg);
 
        viewModel.HideHUD.OnChanged     += hideHudAction;

        if(SOSAdDialog.Instance != null) SOSAdDialog.Instance.Bind(viewModel);
    }
    private void EnsureRedoButton()
    {
        Transform undoBlock = transform.Find("UndoBlock");
        if (undoBlock == null || redoSprite == null) return;

        Transform existingRedo = transform.Find("RedoBlock");
        if (existingRedo != null)
        {
            redoBlock = existingRedo.gameObject;
            return;
        }

        Button undoButton = undoBlock.GetComponent<Button>();
        if (undoButton == null) return;

        redoBlock = Instantiate(undoBlock.gameObject, undoBlock.parent, false);
        redoBlock.name = "RedoBlock";
        redoBlock.transform.SetSiblingIndex(undoBlock.GetSiblingIndex() + 1);

        Button redoButton = redoBlock.GetComponent<Button>();
        redoButton.onClick.RemoveAllListeners();
        redoButton.onClick.AddListener(RedoButtonPressed);

        Image icon = redoBlock.transform.GetChild(0).GetComponent<Image>();
        if (icon != null) icon.sprite = redoSprite;

        TMP_Text label = redoBlock.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = "Redo";
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
        if(viewModel == null) return;

        viewModel.HideHUD.OnChanged     -= hideHudAction;
    }
}
