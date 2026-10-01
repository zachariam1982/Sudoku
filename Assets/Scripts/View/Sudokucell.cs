using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// View — a single Sudoku cell with animations and persistent conflict state.
/// </summary>
public class SudokuCell : MonoBehaviour
{
    [Header("References (auto-found if left empty)")]
    [SerializeField] private Image           background;
    [SerializeField] private Image        numberImage;
    [SerializeField] private TMP_Text     numberText;
    [SerializeField] private GridLayoutGroup pencilGrid; 
    [SerializeField] public GameObject pencilCell;

    [Header("Colors — set in Cell Prefab Inspector")]
    public Color normalColor;
    public Color givenColor;
    public Color dimmedColor;
    public Color pickerHighlight;
    public Color highlightColor;
    public Color errorColor;

    [Header("Pencil Candidate Highlight")]
    [SerializeField] private Color candidateHighlightColor = new Color32(255,200,50,255);
    [SerializeField] private Color matchingDigitHighlightColor = new Color32(255, 200, 50, 255);

    // ── Internal state ────────────────────────────────────────────────────────
    private int             row;
    private int             col;
    private BaseViewModel viewModel;
    private bool            isDimmed   = false;
    private bool            isConflict = false;
    private bool            isDigitMatchHighlighted = false;
    private Color           baseColor;
    private Button[] _pencilButtons;
    private int _pencilCandidateMask;
    private static readonly Color32 PencilInactiveColor =
        new Color32(
            80,
            80,
            80,
            255);

    private static readonly Color32 PencilActiveColor =
        new Color32(
            255,
            255,
            255,
            255);

    // Keep the board digits in the same bright, playful palette as the
    // variant-selection artwork. The sprite sheet supplies the shading;
    // these colors provide the consistent variant accent for each digit.
    private static readonly Color32[] MockupDigitColors =
    {
        new Color32(39, 196, 238, 255),  // 1 cyan
        new Color32(240, 75, 86, 255),   // 2 coral
        new Color32(255, 197, 61, 255),  // 3 gold
        new Color32(131, 201, 74, 255),  // 4 lime
        new Color32(66, 207, 239, 255),  // 5 cyan
        new Color32(255, 138, 61, 255),  // 6 orange
        new Color32(244, 66, 138, 255),  // 7 pink
        new Color32(169, 103, 208, 255), // 8 purple
        new Color32(132, 201, 74, 255)   // 9 green
    };
    public int  Value   { get; private set; }
    public bool IsGiven { get; private set; }
    private Vector3 _originalScale;
    private Vector3 _originalPosition;
    private Quaternion _originalRotation;

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
        if (numberText == null && numberImage != null)
            numberText = numberImage.GetComponent<TMP_Text>();
        
        Button[] btns = GetComponentsInChildren<Button>(true);
        var pencilBtnList = new List<Button>();
        foreach(Button btn in btns)
        {
            //Debug.Log(btn.transform.parent.name);
            if(btn.transform.parent.name == "PencilCell")
            {
                Button b = btn;
                btn.onClick.AddListener(() => OnPencilModeButtonClick(b));
                pencilBtnList.Add(b);
            }
        }
        _pencilButtons = pencilBtnList.ToArray();

        _originalScale = transform.localScale;
        _originalPosition = transform.localPosition;
        _originalRotation = transform.localRotation;
    }

    private void OnPencilModeButtonClick(Button arg)
    {
        if (viewModel == null || !viewModel.IsPencilMode.Value) return;

        if (NumberPicker.Instance != null)
            NumberPicker.Instance.SetSelectedCellTransform(GetComponent<RectTransform>());

        viewModel.SelectCellCommand.Execute(
            new ValueTuple<int, int, object>(row, col, GetComponent<RectTransform>()));
    }
    // ── Binding ───────────────────────────────────────────────────────────────

    public void Bind(int cellRow, int cellCol, BaseViewModel vm)
    {
        row       = cellRow;
        col       = cellCol;
        viewModel = vm;
    }

    public void SetCandidateHighlight(int number)
    {
        if (_pencilButtons == null) return;

        for (int i = 0;i < _pencilButtons.Length && i < 9;i++)
        {
            Button button = _pencilButtons[i];

            if (button == null) continue;

            TMP_Text text = button.GetComponentInChildren<TMP_Text>();

            if (text == null) continue;


            bool isActive = (_pencilCandidateMask & (1 << (i + 1))) != 0;

            if (!isActive)
            {

                text.color = PencilInactiveColor;
                continue;
            }

            int candidateNumber = i + 1;

            if (number > 0 && candidateNumber == number)
            {

                text.color = candidateHighlightColor;
            }
            else
            {
                text.color = PencilActiveColor;
            }
        }
    }
    public void SetPencilCandidates(int candidateMask)
    {
        if (_pencilButtons == null) return;

        _pencilCandidateMask = candidateMask;

        for (int i = 0;i < _pencilButtons.Length && i < 9;i++)
        {
            Button button = _pencilButtons[i];

            if (button == null) continue;

            TMP_Text text = button.GetComponentInChildren<TMP_Text>();

            if (text == null) continue;

            bool isActive = (candidateMask & (1 << (i + 1))) != 0;
            text.color = isActive ? PencilActiveColor : PencilInactiveColor;
        }

        if (viewModel != null)
        {
            SetCandidateHighlight(viewModel.HighlightedCandidateNumber.Value);
        }
    }
    // ── Render ────────────────────────────────────────────────────────────────

    public void SetValue(int value, bool isGiven)
    {
        Value   = value;
        IsGiven = isGiven;

        if (numberImage != null)
        {
            numberImage.gameObject.SetActive(value != 0);
            numberImage.enabled = numberText == null;
            if (numberText != null)
            {
                numberText.text = value == 0 ? string.Empty : value.ToString();
                UpdateDigitColor();
            }
            if (numberText == null)
                numberImage.color = GetDigitColor(value);
        }

        if (value == 0) isConflict = false;

        baseColor = isGiven ? givenColor : normalColor;

        // Keep conflict feedback on the cell background, not the digit.
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

        // Keep the digit palette unchanged while the cell is in conflict.
        if (!isConflict) UpdateDigitColor();
    }

    public void SetPickerHighlight(bool active)
    {
        if (isConflict)
        {
            if (background != null) background.color = errorColor;
            return;
        }

        baseColor = active ? pickerHighlight :
                             (isDimmed ? dimmedColor : (IsGiven ? givenColor : normalColor));
        if (background != null)
            background.color = baseColor;
    }

    /// <summary>
    /// Marks or clears a conflict using the cell background while preserving
    /// the digit's current color.
    /// </summary>
    public void SetConflict(bool conflict)
    {
        isConflict = conflict;

        if (conflict)
        {
            if (background != null)
                background.color = errorColor;
        }
        else
        {
            UpdateDigitColor();

            if (background != null)
                background.color = isDimmed ? dimmedColor : (IsGiven ? givenColor : normalColor);
        }
    }

    public void SetDigitMatchHighlight(bool highlighted)
    {
        isDigitMatchHighlighted = highlighted;
        UpdateDigitColor();
    }

    private void UpdateDigitColor()
    {
        Color digitColor = GetDigitColor(Value);
        if (isDimmed)
            digitColor = Color.Lerp(digitColor, Color.gray, 0.45f);
        if (isDigitMatchHighlighted && Value > 0)
            digitColor = matchingDigitHighlightColor;

        if (numberText != null) numberText.color = digitColor;
        else if (numberImage != null) numberImage.color = digitColor;
    }

    private static Color GetDigitColor(int value)
    {
        if (value < 1 || value > MockupDigitColors.Length)
            return Color.white;

        return MockupDigitColors[value - 1];
    }

    void OnDisable()
    {
        transform.localScale = Vector3.one;
    }

    // ── Animations ────────────────────────────────────────────────────────────
    public void PlayTapAnimation()
    {
        StartCoroutine(UIAnimator.ScalePunch(transform, _originalScale));
    }
    public void PlayEntryAnimation()
    {
        StartCoroutine(UIAnimator.ScaleBounce(transform, _originalScale));
    }
    public void PlayErrorAnimation()
    {
        StartCoroutine(PlayErrorSequence());
    }

    private System.Collections.IEnumerator PlayErrorSequence()
    {
        // Pulse the cell background so the digit keeps its normal color.
        Image pulseTarget = background != null ? background : numberImage;
        if (pulseTarget == null) yield break;

        yield return UIAnimator.Pulse(pulseTarget, Color.white, errorColor, 3, 0.15f);

        if (isConflict)
            pulseTarget.color = errorColor;
        else
            pulseTarget.color = isDimmed ? dimmedColor : baseColor;
    }

    public void PlayLockedAnimation()
    {
        StartCoroutine(UIAnimator.Wobble(transform, _originalRotation));
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    public void OnClick()
    {
        if (viewModel == null) return;

        bool pickerAlreadyOpen = NumberPicker.Instance != null && NumberPicker.Instance.IsOpen;

        if (IsGiven)
        {
            if (pickerAlreadyOpen) viewModel.CancelPickerCommand.Execute();

            PlayLockedAnimation();
            viewModel.SelectCellCommand.Execute(
                new ValueTuple<int, int, object>(row, col, GetComponent<RectTransform>()));
            return;
        }

        if (viewModel.IsEraseMode.Value)
        {
            if (pickerAlreadyOpen) viewModel.CancelPickerCommand.Execute();

            return;
        }

        if (pickerAlreadyOpen && Value != 0)
        {
            viewModel.CancelPickerCommand.Execute();
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
        if(Value == 0) return;

        viewModel.RecordEraseUse();
        viewModel.SelectCellCommand.Execute(new ValueTuple<int, int, object>(row, col, GetComponent<RectTransform>()));
        viewModel.EnterValueCommand.Execute(0);
        viewModel.SetEraseModeCommand.Execute();
    }

    [Header("Responsive Number Size")]
    [SerializeField, Range(0.5f, 1.25f)]
    private float numberFontSizeCellRatio = 0.86f;

    public void ResizeCellContent(float newCellSize)
    {
        if (numberText != null)
        {
            numberText.enableAutoSizing = false;
            numberText.fontSize = Mathf.Max(1f, newCellSize * numberFontSizeCellRatio);
        }

        if (pencilGrid == null) return;

        float padding = pencilGrid.padding.left + pencilGrid.padding.right;
        float spacing = pencilGrid.spacing.x * 2;
        float bSize = (newCellSize - padding - spacing) / 3f;

        pencilGrid.cellSize = new Vector2(bSize, bSize);
    }

}
