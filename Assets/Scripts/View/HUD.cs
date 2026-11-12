using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum TutorialHudControl
{
    Undo,
    Redo,
    Pencil,
    AutoFill
}

public class HUD : MonoBehaviour
{
    private BaseViewModel viewModel;
    private Action<bool>    hideHudAction;
    private Action<bool>    pencilModeChangedAction;
    private Button          pencilModeButton;
    private Button          undoButton;
    private Button          redoButton;
    private Button          autoFillButton;
    private bool            undoButtonWasInteractable;
    private bool            redoButtonWasInteractable;
    private ColorBlock      defaultPencilButtonColors;
    private bool            controlDefaultsCaptured;
    private Button          highlightedTutorialButton;
    private Outline         tutorialOutline;
    private bool            createdTutorialOutline;
    private bool            originalTutorialOutlineEnabled;
    private Color           originalTutorialOutlineColor;
    private Vector2         originalTutorialOutlineDistance;
    private Vector3         originalTutorialButtonScale;
    private Coroutine       tutorialHighlightAnimation;

    public event Action<TutorialHudControl> TutorialControlPressed;

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
        // The Button components are on the control blocks; the nested Undo/Redo
        // objects only contain their artwork.
        undoButton = transform.Find("UndoBlock")?.GetComponent<Button>();
        redoButton = transform.Find("RedoBlock")?.GetComponent<Button>();
        autoFillButton = transform.Find("AutoFillBlock")?.GetComponent<Button>();
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

    public RectTransform GetTutorialControlRect(TutorialHudControl control)
    {
        Button button = GetTutorialControlButton(control);
        return button != null ? button.GetComponent<RectTransform>() : null;
    }

    public void HighlightTutorialControl(TutorialHudControl control)
    {
        ClearTutorialHighlight();
        highlightedTutorialButton = GetTutorialControlButton(control);
        if (highlightedTutorialButton == null || highlightedTutorialButton.targetGraphic == null)
        {
            highlightedTutorialButton = null;
            return;
        }

        originalTutorialButtonScale = highlightedTutorialButton.transform.localScale;
        Graphic targetGraphic = highlightedTutorialButton.targetGraphic;
        tutorialOutline = targetGraphic.GetComponent<Outline>();
        createdTutorialOutline = tutorialOutline == null;
        if (createdTutorialOutline)
            tutorialOutline = targetGraphic.gameObject.AddComponent<Outline>();
        else
        {
            originalTutorialOutlineEnabled = tutorialOutline.enabled;
            originalTutorialOutlineColor = tutorialOutline.effectColor;
            originalTutorialOutlineDistance = tutorialOutline.effectDistance;
        }

        tutorialOutline.enabled = true;
        tutorialOutline.effectColor = new Color(1f, 0.72f, 0.16f, 1f);
        tutorialOutline.effectDistance = new Vector2(5f, 5f);
        tutorialHighlightAnimation = StartCoroutine(PulseTutorialHighlight());
    }

    public void ClearTutorialHighlight()
    {
        if (tutorialHighlightAnimation != null)
        {
            StopCoroutine(tutorialHighlightAnimation);
            tutorialHighlightAnimation = null;
        }

        if (highlightedTutorialButton != null)
            highlightedTutorialButton.transform.localScale = originalTutorialButtonScale;
        if (tutorialOutline != null)
        {
            if (createdTutorialOutline)
                Destroy(tutorialOutline);
            else
            {
                tutorialOutline.enabled = originalTutorialOutlineEnabled;
                tutorialOutline.effectColor = originalTutorialOutlineColor;
                tutorialOutline.effectDistance = originalTutorialOutlineDistance;
            }
        }

        highlightedTutorialButton = null;
        tutorialOutline = null;
        createdTutorialOutline = false;
        originalTutorialOutlineEnabled = false;
    }

    private Button GetTutorialControlButton(TutorialHudControl control)
    {
        switch (control)
        {
            case TutorialHudControl.Undo: return undoButton;
            case TutorialHudControl.Redo: return redoButton;
            case TutorialHudControl.Pencil: return pencilModeButton;
            case TutorialHudControl.AutoFill: return autoFillButton;
            default: return null;
        }
    }

    private IEnumerator PulseTutorialHighlight()
    {
        const float pulseSpeed = 4.5f;
        while (highlightedTutorialButton != null && tutorialOutline != null)
        {
            float pulse = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f;
            highlightedTutorialButton.transform.localScale = originalTutorialButtonScale *
                Mathf.Lerp(1.06f, 1.2f, pulse);
            tutorialOutline.effectColor = new Color(1f, 0.58f + pulse * 0.34f,
                0.08f, 0.75f + pulse * 0.25f);
            tutorialOutline.effectDistance = Vector2.one * (4f + pulse * 3f);
            yield return null;
        }
    }

    public void PencilButtonPressed()
    {
        viewModel?.SetPencilModeCommand.Execute();
        TutorialControlPressed?.Invoke(TutorialHudControl.Pencil);
    }

    public void AutoFillCandidatesButtonPressed()
    {
        viewModel?.AutoFillCandidatesCommand.Execute();
        TutorialControlPressed?.Invoke(TutorialHudControl.AutoFill);
    }

    public void UndoButtonPressed()
    {
        viewModel?.UndoCommand.Execute();
        TutorialControlPressed?.Invoke(TutorialHudControl.Undo);
    }

    public void RedoButtonPressed()
    {
        viewModel?.RedoCommand.Execute();
        TutorialControlPressed?.Invoke(TutorialHudControl.Redo);
    }

    public void PauseButtonPressed() => viewModel?.PauseCommand.Execute();
    private void OnDestroy()
    {
        ClearTutorialHighlight();
        Unbind();
    }
}
