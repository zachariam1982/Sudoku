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
    
    [Header("Pencil Candidate Highlight")]
    [SerializeField] private Color candidateHighlightColor = new Color32(255,200,50,255);

    // ── Internal state ────────────────────────────────────────────────────────
    private int             row;
    private int             col;
    private SudokuViewModel viewModel;
    private bool            isDimmed   = false;
    private bool            isConflict = false;
    private Color           baseColor;
    private HashSet<TMP_Text> btnTextInPencilMode = new HashSet<TMP_Text>();


    private Button[] _pencilButtons;
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
        if (viewModel != null && viewModel.IsPencilMode.Value)
        {
            if (NumberPicker.Instance != null)
                NumberPicker.Instance.SetSelectedCellTransform(GetComponent<RectTransform>());
 
            viewModel.SelectCellCommand.Execute(
                new ValueTuple<int, int, object>(row, col, GetComponent<RectTransform>()));
            return;
        }

        TMP_Text text = arg.GetComponentInChildren<TMP_Text>();

        if (text == null) return;

        bool isActive = btnTextInPencilMode.Contains(text);

        if (isActive)
        {
            btnTextInPencilMode.Remove(text);
            text.color = PencilInactiveColor;
        }
        else
        {
            btnTextInPencilMode.Add(text);
            text.color = PencilActiveColor;
        }

        SetCandidateHighlight(viewModel != null ? viewModel.HighlightedCandidateNumber.Value : 0);
    }

    private void OnStateChanged(string stateName)
    {
        if (stateName != "IdleState") return;

        foreach (TMP_Text text in btnTextInPencilMode)
        {
            if (text != null)
            {
                text.color = PencilInactiveColor;
            }
        }

        btnTextInPencilMode.Clear();

        SetCandidateHighlight(0);
    }
    // ── Binding ───────────────────────────────────────────────────────────────

    public void Bind(int cellRow, int cellCol, SudokuViewModel vm)
    {
        row       = cellRow;
        col       = cellCol;
        viewModel = vm;
        if( viewModel != null) viewModel.CurrentStateName.OnChanged += OnStateChanged;
    }

    private void OnDestroy()
    {
        if( viewModel != null) viewModel.CurrentStateName.OnChanged -= OnStateChanged;
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


            bool isActive = btnTextInPencilMode.Contains(text);

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
    public void TogglePencilNumber(int number)
    {
        if (_pencilButtons == null || number < 1 || number > 9) return;
        
        int idx = number - 1;

        if (idx >= _pencilButtons.Length) return;

        TMP_Text text = _pencilButtons[idx].GetComponentInChildren<TMP_Text>();

        if (text == null) return;

        bool isActive = btnTextInPencilMode.Contains(text);

        if (isActive)
        {
            btnTextInPencilMode.Remove(text);
            text.color = PencilInactiveColor;
        }
        else
        {
            btnTextInPencilMode.Add(text);
            text.color = PencilActiveColor;
        }

        if (viewModel != null)
        {
            viewModel.HighlightedCandidateNumber.Value = number;
        }
    }
    public bool RemovePencilCandidate(int number)
    {
        if (_pencilButtons == null || number < 1 || number > 9) return false;

        int index = number - 1;

        if (index >= _pencilButtons.Length) return false;

        Button button = _pencilButtons[index];

        if (button == null) return false;

        TMP_Text text = button.GetComponentInChildren<TMP_Text>();

        if (text == null) return false;

        if (!btnTextInPencilMode.Contains(text)) return false;

        btnTextInPencilMode.Remove(text);

        text.color = PencilInactiveColor;

        return true;
    }

    public void ClearAllPencilCandidates()
    {
        if (_pencilButtons == null) return;

        foreach (TMP_Text text in btnTextInPencilMode)
        {
            if (text != null)
            {
                text.color = PencilInactiveColor;
            }
        }

        btnTextInPencilMode.Clear();
    }

    public void SetPencilCandidates(IEnumerable<int> candidates)
    {
        if (_pencilButtons == null) return;

        HashSet<int> candidateSet = new HashSet<int>(candidates);
        btnTextInPencilMode.Clear();
        Color32 inactiveColor = new Color32(80,80,80,255);

        for (int i = 0;i < _pencilButtons.Length && i < 9;i++)
        {
            Button button = _pencilButtons[i];

            if (button == null) continue;

            TMP_Text text = button.GetComponentInChildren<TMP_Text>();

            if (text == null) continue;

            int number = i + 1;

            if (candidateSet.Contains(number))
            {
                btnTextInPencilMode.Add(text);
                text.color = Color.white;
            }
            else
            {
                text.color = inactiveColor;
            }
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
        baseColor = active ? pickerHighlight : 
                             (isDimmed ? dimmedColor : (IsGiven ? givenColor : normalColor));
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
        StartCoroutine(UIAnimator.Wobble(transform, _originalRotation));
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