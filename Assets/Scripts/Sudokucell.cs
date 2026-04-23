using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A single Sudoku cell. Supports normal, highlighted, dimmed, and picker-highlight states.
/// </summary>
public class SudokuCell : MonoBehaviour
{
    [Header("References (auto-found if left empty)")]
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI label;

    public Color normalColor ;
    public Color dimmedColor;
    public Color pickerHighlight;

    public int  Row     { get; private set; }
    public int  Col     { get; private set; }
    public int  Value   { get; private set; }
    public bool IsGiven { get; private set; }

    private SudokuGrid grid;
    private bool isDimmed = false;

    void Awake()
    {
        if (background == null) background = GetComponent<Image>();
        if (label == null)      label      = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void Init(int row, int col, SudokuGrid gridManager)
    {
        Row  = row;
        Col  = col;
        grid = gridManager;
    }

    public void SetValue(int value, bool isGiven)
    {
        Value   = value;
        IsGiven = isGiven;

        label.text      = value == 0 ? "" : value.ToString();

        if (background != null)
            background.color = normalColor;
    }

    public void EnterValue(int value)
    {
        if (IsGiven) return;
        SetValue(value, isGiven: false);
    }

    public void SetDimmed(bool dimmed)
    {
        isDimmed = dimmed;
        if (background != null)
            background.color = dimmed ? dimmedColor : normalColor;
    }

    public void SetPickerHighlight(bool active)
    {
        if (background != null)
            background.color = active ? pickerHighlight : (isDimmed ? dimmedColor : normalColor);
    }

    public void OnClick()
    {
        if (grid != null)
            grid.OnCellSelected(this);
    }
}