using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A horizontal row of 9 number buttons that pops up near the tapped cell.
/// - Always stays within the GridPanel bounds
/// - Appears BELOW the cell normally, flips ABOVE for bottom rows
/// - Board dims and ignores taps while picker is open
/// - Grid bounds are set dynamically by GridBuilder at runtime
/// </summary>
public class NumberPicker : MonoBehaviour
{
    public static NumberPicker Instance { get; private set; }

    [Header("References")]
    [SerializeField] private RectTransform pickerPanel;
    [SerializeField] private Button[]      numberButtons;
    [SerializeField] private Image         overlayPanel;

    [Header("Positioning")]
    [SerializeField] private float yGap = 10f;
    [SerializeField] private float pickerPadding = 8f;

    [Header("Colors")]
    [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 0.45f);

    // Set at runtime by GridBuilder
    private float gridHalfW = 310f;
    private float gridHalfH = 310f;
    private float cellSize  = 56f;

    private SudokuCell targetCell;
    private SudokuGrid sudokuGrid;
    private bool       isOpen = false;

    void Awake()
    {
        Instance = this;

        for (int i = 0; i < numberButtons.Length; i++)
        {
            int number = i + 1;
            TextMeshProUGUI label = numberButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = number.ToString();
            numberButtons[i].onClick.AddListener(() => OnNumberSelected(number));
        }

        Hide();
    }

    /// <summary>Called by GridBuilder once it knows the real grid size.</summary>
    public void UpdateGridBounds(float gridWidth, float gridHeight,
                                float boxGap, float boxPadding, float cellGap)
    {
        gridHalfW = gridWidth  / 2f;
        gridHalfH = gridHeight / 2f;
    
        float boxSize = (gridWidth - 2f * boxGap) / 3f;
        cellSize      = (boxSize - 2f * boxPadding - 2f * cellGap) / 3f;
    
        ResizePickerButtons();
    }

private void ResizePickerButtons()
{
    if (pickerPanel == null || numberButtons == null) return;

    float buttonSize  = cellSize;
    float buttonGap   = 4f;
    float totalWidth  = 9f * buttonSize + 8f * buttonGap + 2f * pickerPadding;
    float totalHeight = buttonSize + 2f * pickerPadding;

    pickerPanel.sizeDelta = new Vector2(totalWidth, totalHeight);

    float startX = -(totalWidth / 2f) + pickerPadding + buttonSize / 2f;

    for (int i = 0; i < numberButtons.Length; i++)
    {
        RectTransform rt = numberButtons[i].GetComponent<RectTransform>();
        rt.sizeDelta        = new Vector2(buttonSize, buttonSize);
        rt.anchoredPosition = new Vector2(startX + i * (buttonSize + buttonGap), 0f);

        TextMeshProUGUI label = numberButtons[i].GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
            label.fontSize = Mathf.Round(buttonSize * 0.5f);
    }
}
    public void Show(SudokuCell cell, SudokuGrid grid)
    {
        targetCell = cell;
        sudokuGrid = grid;
        isOpen     = true;

        // ── Get cell position in Canvas local space ───────────────────────
        RectTransform cellRT   = cell.GetComponent<RectTransform>();
        Canvas        canvas   = pickerPanel.GetComponentInParent<Canvas>();
        RectTransform canvasRT = canvas.GetComponent<RectTransform>();

        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                          ? null : canvas.worldCamera;

        Vector2 cellScreenPos = RectTransformUtility.WorldToScreenPoint(uiCamera, cellRT.position);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT, cellScreenPos, uiCamera, out Vector2 cellLocalPos);

        float cellHalfH  = cellRT.rect.height / 2f;
        float pickerHalfH = pickerPanel.rect.height / 2f;
        float pickerHalfW = pickerPanel.rect.width / 2f;

        // ── Decide: show BELOW or ABOVE ───────────────────────────────────
        float belowY = cellLocalPos.y - cellHalfH - yGap - pickerHalfH;
        float aboveY = cellLocalPos.y + cellHalfH + yGap + pickerHalfH;

        float pickerY = (belowY - pickerHalfH < -gridHalfH) ? aboveY : belowY;

        // ── Clamp X within grid ───────────────────────────────────────────
        float pickerX = Mathf.Clamp(cellLocalPos.x,
                                    -gridHalfW + pickerHalfW,
                                     gridHalfW - pickerHalfW);

        // ── Clamp Y within grid ───────────────────────────────────────────
        pickerY = Mathf.Clamp(pickerY,
                              -gridHalfH + pickerHalfH,
                               gridHalfH - pickerHalfH);

        pickerPanel.anchoredPosition = new Vector2(pickerX, pickerY);

        // ── Show ──────────────────────────────────────────────────────────
        overlayPanel.gameObject.SetActive(true);
        overlayPanel.color = overlayColor;
        pickerPanel.gameObject.SetActive(true);

        sudokuGrid.SetBoardInteractable(false);
        cell.SetPickerHighlight(true);
    }

    private void OnNumberSelected(int number)
    {
        if (targetCell != null)
            targetCell.EnterValue(number);
        Hide();
    }

    public void Hide()
    {
        isOpen = false;

        if (pickerPanel  != null) pickerPanel.gameObject.SetActive(false);
        if (overlayPanel != null) overlayPanel.gameObject.SetActive(false);

        if (targetCell != null) targetCell.SetPickerHighlight(false);
        if (sudokuGrid != null) sudokuGrid.SetBoardInteractable(true);

        targetCell = null;
        sudokuGrid = null;
    }

    public bool IsOpen => isOpen;
}