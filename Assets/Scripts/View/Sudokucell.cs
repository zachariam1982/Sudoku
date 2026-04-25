using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// View — a single Sudoku cell with animations and persistent conflict state.
/// </summary>
public class SudokuCell : MonoBehaviour
{
    [Header("References (auto-found if left empty)")]
    [SerializeField] private Image           background;
    [SerializeField] private TextMeshProUGUI label;

    [Header("Colors — set in Cell Prefab Inspector")]
    public Color normalColor;
    public Color givenColor;
    public Color dimmedColor;
    public Color pickerHighlight;
    public Color highlightColor;
    public Color errorColor;

    // ── Internal state ────────────────────────────────────────────────────────
    private int             row;
    private int             col;
    private SudokuViewModel viewModel;
    private bool            isDimmed   = false;
    private bool            isConflict = false;
    private Color           baseColor;

    public int  Value   { get; private set; }
    public bool IsGiven { get; private set; }

    void Awake()
    {
        if (background == null) background = GetComponent<Image>();
        if (label == null)      label      = GetComponentInChildren<TextMeshProUGUI>();
    }

    // ── Binding ───────────────────────────────────────────────────────────────

    public void Bind(int cellRow, int cellCol, SudokuViewModel vm)
    {
        row       = cellRow;
        col       = cellCol;
        viewModel = vm;
    }

    // ── Render ────────────────────────────────────────────────────────────────

    public void SetValue(int value, bool isGiven)
    {
        Value   = value;
        IsGiven = isGiven;

        if (label != null)
            label.text = value == 0 ? "" : value.ToString();

        // Clearing a cell always removes its conflict state
        if (value == 0) isConflict = false;

        baseColor = isGiven ? givenColor : normalColor;

        if (background != null && !isDimmed)
            background.color = isConflict ? errorColor : baseColor;
    }

    public void SetHighlight(bool highlighted)
    {
        if (isDimmed || isConflict) return;
        baseColor = highlighted ? highlightColor : (IsGiven ? givenColor : normalColor);
        if (background != null)
            background.color = baseColor;
    }

    public void SetDimmed(bool dimmed)
    {
        isDimmed  = dimmed;
        baseColor = dimmed ? dimmedColor : (IsGiven ? givenColor : normalColor);
        if (background != null)
            background.color = isConflict ? errorColor : baseColor;
    }

    public void SetPickerHighlight(bool active)
    {
        if (isConflict && !active) return; // keep error color visible behind picker highlight
        baseColor = active
            ? pickerHighlight
            : (isDimmed ? dimmedColor : (IsGiven ? givenColor : normalColor));
        if (background != null)
            background.color = baseColor;
    }

    /// <summary>
    /// Persistently marks or clears the conflict error color.
    /// Called by SudokuGrid every time ConflictingCells changes.
    /// Stays red until the conflict is resolved — survives dim/undim cycles.
    /// </summary>
    public void SetConflict(bool conflict)
    {
        isConflict = conflict;
        if (background == null) return;

        if (conflict)
            background.color = errorColor;
        else
            background.color = isDimmed ? dimmedColor : (IsGiven ? givenColor : normalColor);
    }

    // ── Animations ────────────────────────────────────────────────────────────

    public void PlayTapAnimation()
    {
        StartCoroutine(UIAnimator.ScalePunch(transform));
    }

    public void PlayEntryAnimation()
    {
        StartCoroutine(UIAnimator.ScaleBounce(transform));
    }

    public void PlayErrorAnimation()
    {
        // Flash to error color and back — SetConflict keeps it red after the flash
        StartCoroutine(UIAnimator.Flash(background, errorColor, errorColor));
        StartCoroutine(UIAnimator.Shake(transform));
    }

    public void PlayLockedAnimation()
    {
        StartCoroutine(UIAnimator.Wobble(transform));
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    public void OnClick()
    {
        if (viewModel == null) return;

        if (IsGiven)
        {
            PlayLockedAnimation();
            return;
        }

        PlayTapAnimation();

        if (NumberPicker.Instance != null)
            NumberPicker.Instance.SetSelectedCellTransform(GetComponent<RectTransform>());

        viewModel.SelectCellCommand.Execute(
            new ValueTuple<int, int, object>(row, col, GetComponent<RectTransform>()));
    }
}