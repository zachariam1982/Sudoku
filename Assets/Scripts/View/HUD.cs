using System;
using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    [SerializeField] private Button sosButton;
    [SerializeField] private Button eraseButton;

    private SudokuViewModel viewModel;
    private Action<bool>    hideHudAction;
    public void Bind(SudokuViewModel arg)
    {
        viewModel = arg;
        hideHudAction = (arg) => this.gameObject.SetActive(arg);

        viewModel.IsEraseMode.OnChanged += OnEraseModeChange;
        viewModel.IsPencilMode.OnChanged += OnPencilModeChange;
        viewModel.SelectedRow.OnChanged += OnSelectedCellChange;
        viewModel.SelectedCol.OnChanged += OnSelectedCellChange;
        viewModel.BoardValues.OnChanged += OnBoardValuesChange;
        viewModel.HideHUD.OnChanged     += hideHudAction;

        UpdateActionButtonStates();

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

        viewModel.IsEraseMode.OnChanged -= OnEraseModeChange;
        viewModel.IsPencilMode.OnChanged -= OnPencilModeChange;
        viewModel.SelectedRow.OnChanged -= OnSelectedCellChange;
        viewModel.SelectedCol.OnChanged -= OnSelectedCellChange;
        viewModel.BoardValues.OnChanged -= OnBoardValuesChange;
        viewModel.HideHUD.OnChanged     -= hideHudAction;
    }

    private void OnEraseModeChange(bool arg)
    {
        UpdateActionButtonStates();
    }

    private void OnPencilModeChange(bool arg)
    {
        UpdateActionButtonStates();
    }

    private void OnSelectedCellChange(int arg)
    {
        UpdateActionButtonStates();
    }

    private void OnBoardValuesChange(int[,] arg)
    {
        UpdateActionButtonStates();
    }

    private void UpdateActionButtonStates()
    {
        if (viewModel == null) return;

        int row = viewModel.SelectedRow.Value;
        int col = viewModel.SelectedCol.Value;
        int[,] board = viewModel.BoardValues.Value;
        bool hasEditableSelection =
            row >= 0 && col >= 0 &&
            board != null;

        if (sosButton != null)
        {
            sosButton.interactable =
                hasEditableSelection &&
                board[row, col] == 0 &&
                !viewModel.IsEraseMode.Value &&
                !viewModel.IsPencilMode.Value;
        }

        if (eraseButton != null)
        {
            eraseButton.interactable =
                hasEditableSelection &&
                board[row, col] != 0 &&
                !viewModel.IsPencilMode.Value;
        }
    }
}
