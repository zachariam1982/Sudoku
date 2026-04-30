using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

/// <summary>
/// View — a single Sudoku cell with animations and persistent conflict state.
/// </summary>
public class SudokuCell : MonoBehaviour
{
    [Header("References (auto-found if left empty)")]
    [SerializeField] private Image           background;
    [SerializeField] private Image        numberImage;
    [SerializeField] private Sprite[]     numberSprites; // drag Number_1 to Number_9 in Inspector
    [SerializeField] private GridLayoutGroup pencilGrid; 
    [SerializeField] public GameObject pencilCell;

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
        if (numberImage == null)
        {
            foreach (Image img in GetComponentsInChildren<Image>())
            {
                if (img.gameObject != gameObject)
                {
                    numberImage = img;
                    break;
                }
            }
        }
        
        Button[] btns = GetComponentsInChildren<Button>(true);
        foreach(Button btn in btns)
        {
            //Debug.Log(btn.transform.parent.name);
            if(btn.transform.parent.name == "PencilCell")
            {
                Button b = btn;
                btn.onClick.AddListener(() => OnPencilModeButtonClick(b));
            }
        }
    }

    private void OnPencilModeButtonClick(Button arg)
    {
        TMP_Text text = arg.GetComponentInChildren<TMP_Text>();
        if (text == null) return;

        Color32 targetGrey = new Color32(80,80,80,255);
        Color32 currentColor = text.color;

        if(currentColor.r == 80 && currentColor.g == 80 && currentColor.b == 80)
        {
            text.color = Color.white;
        }
        else
        {
            text.color = targetGrey;
        }
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

        if (numberImage != null)
        {
            numberImage.gameObject.SetActive(value != 0);
            if (value > 0 && value <= numberSprites.Length && numberSprites[value - 1] != null)
                numberImage.sprite = numberSprites[value - 1];
        }

        if (value == 0) isConflict = false;

        baseColor = isGiven ? givenColor : normalColor;

        // background is always the cell root Image — never reassigned
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
            background.color = baseColor;

        // Keep error tint on numberImage visible even while dimmed
        if (numberImage != null && !isConflict)
            numberImage.color = dimmed ? new Color(0.6f, 0.6f, 0.6f, 1f) : Color.white;
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

        if (conflict)
        {
            // Tint numberImage red so error is visible over the full-size sprite
            if (numberImage != null && numberImage.gameObject.activeSelf)
                numberImage.color = errorColor;
            else
                background.color = errorColor; // fallback for empty cells
        }
        else
        {
            // Restore numberImage to white so sprite shows true colors
            if (numberImage != null)
                numberImage.color = Color.white;
            if (background != null)
                background.color = isDimmed ? dimmedColor : (IsGiven ? givenColor : normalColor);
        }
    }

    // ── Animations ────────────────────────────────────────────────────────────

    public void PlayTapAnimation()
    {
        Debug.Log($"GameObject active: {gameObject.activeInHierarchy}, " +
              $"Component enabled: {enabled}");
        StartCoroutine(UIAnimator.ScalePunch(transform));
    }

    public void PlayEntryAnimation()
    {
        StartCoroutine(UIAnimator.ScaleBounce(transform));
    }

    public void PlayErrorAnimation()
    {
        StartCoroutine(PlayErrorSequence());
        StartCoroutine(UIAnimator.Shake(transform));
    }

    private System.Collections.IEnumerator PlayErrorSequence()
    {
        // Pulse numberImage between white and errorColor — this is what the player sees
        Image pulseTarget = (numberImage != null && numberImage.gameObject.activeSelf)
                            ? numberImage
                            : background;

        yield return UIAnimator.Pulse(pulseTarget, Color.white, errorColor, 3, 0.15f);

        // After pulse finishes, enforce final conflict state
        if (isConflict)
            pulseTarget.color = errorColor;
        else
            pulseTarget.color = Color.white;
    }

    public void PlayLockedAnimation()
    {
        StartCoroutine(UIAnimator.Wobble(transform));
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    public void OnClick()
    {
        if (viewModel == null || viewModel.IsEraseMode.Value) return;

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

    public void OnClickFromErase()
    {
        if(viewModel == null) return;

        if(IsGiven) return;

        viewModel.SelectCellCommand.Execute(
            new ValueTuple<int, int, object>(row, col, GetComponent<RectTransform>()));
        viewModel.EnterValueCommand.Execute( 0);
        viewModel.SetEraseModeCommand.Execute();
    }

    public void ResizePencilGrid(float newCellSize)
    {
        float padding = pencilGrid.padding.left + pencilGrid.padding.right;
        float spacing = pencilGrid.spacing.x * 2;
        float bSize = (newCellSize - padding - spacing) / 3f;

        pencilGrid.cellSize = new Vector2(bSize, bSize);
    }

}