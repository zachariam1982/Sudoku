using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// View — number picker with slide-up appear and slide-down hide animations.
/// </summary>
public class NumberPicker : MonoBehaviour
{
    public static NumberPicker Instance { get; private set; }

    [Header("References")]
    [SerializeField] private RectTransform pickerPanel;
    [SerializeField] private Button[]      numberButtons;
    [SerializeField] private Image         overlayPanel;

    [Header("Positioning")]
    [SerializeField] private float yGap          = 10f;
    [SerializeField] private float pickerPadding = 8f;
    [SerializeField] private float screenSidePadding = 16f;

    [Header("Animation")]
    [SerializeField] private float slideOffset = 50f;
    [SerializeField] private float slideInDuration  = 0.25f;
    [SerializeField] private float slideOutDuration = 0.15f;
    [SerializeField] private float overlayAlpha     = 0.55f;

    [Header("Colors")]
    [SerializeField] private Color overlayColor = new Color(0.06f, 0.06f, 0.12f, 0.55f);

    private float gridHalfW = 310f;
    private float gridHalfH = 310f;
    private float cellSize  = 56f;

    private SudokuViewModel viewModel;
    private RectTransform   selectedCellRT;
    private Vector2         lastPickerPos;
    private Coroutine       activeAnimation;
    private SudokuCell      _selectedCell;

    void Awake()
    {
        Instance = this;

        for (int i = 0; i < numberButtons.Length; i++)
        {
            int number = i + 1;
            TextMeshProUGUI lbl = numberButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (lbl != null) lbl.text = number.ToString();
            numberButtons[i].onClick.AddListener(() =>
            {
                if (viewModel?.IsPencilMode.Value == true)
                    _selectedCell?.TogglePencilNumber(number);
                else
                    viewModel?.EnterValueCommand.Execute(number);
            });
        }

        // Start hidden without animation
        if (pickerPanel  != null) pickerPanel.gameObject.SetActive(false);
        if (overlayPanel != null) overlayPanel.gameObject.SetActive(false);
    }

    // ── Binding ───────────────────────────────────────────────────────────────

    public void Bind(SudokuViewModel vm)
    {
        viewModel = vm;
        vm.IsPickerOpen.OnChanged          += OnPickerOpenChanged;
        vm.SelectedCellTransform.OnChanged += OnSelectedCellTransformChanged;
    }

    private void OnDestroy()
    {
        if (viewModel == null) return;
        viewModel.IsPickerOpen.OnChanged          -= OnPickerOpenChanged;
        viewModel.SelectedCellTransform.OnChanged -= OnSelectedCellTransformChanged;
    }

    private void OnSelectedCellTransformChanged(object cellTransform)
    {
        selectedCellRT = cellTransform as RectTransform;
        _selectedCell  = selectedCellRT != null
                    ? selectedCellRT.GetComponent<SudokuCell>()
                    : null;
    }

    private void OnPickerOpenChanged(bool isOpen)
    {
        if (isOpen) ShowPicker();
        else        HideWithAnimation();
    }

    public void OnOverlayTapped()
    {
        viewModel?.CancelPickerCommand.Execute();
    }

    // ── Layout ────────────────────────────────────────────────────────────────

    public void UpdateGridBounds(float gridWidth, float gridHeight,
                                 float boxGap, float boxPadding, float cellGap)
    {
        gridHalfW = gridWidth  / 2f;
        gridHalfH = gridHeight / 2f;

        float boxSize = (gridWidth - 2f * boxGap) / 3f;
        cellSize      = (boxSize - 2f * boxPadding - 2f * cellGap) / 3f;

        ResizePickerButtons();
    }

    public void SetSelectedCellTransform(RectTransform cellRT)
    {
        selectedCellRT = cellRT;
        _selectedCell  = cellRT != null ? cellRT.GetComponent<SudokuCell>() : null;
    }

    private void ResizePickerButtons()
    {
        if (pickerPanel == null || numberButtons == null || numberButtons.Length == 0) return;

        float buttonSize = cellSize * 0.82f;
        float buttonGap  = cellSize * 0.40f;

        Canvas canvas = pickerPanel.GetComponentInParent<Canvas>();
        RectTransform canvasRT = canvas.GetComponent<RectTransform>();

        float maxPickerWidth = canvasRT.rect.width - (2f * screenSidePadding);
        float desiredContentWidth = numberButtons.Length * buttonSize + (numberButtons.Length - 1) * buttonGap;
        float availableContentWidth = maxPickerWidth - (2f * pickerPadding);

        // If needed, shrink buttons AND gaps proportionally.
        if (desiredContentWidth > availableContentWidth)
        {
            float scale = availableContentWidth / desiredContentWidth;

            buttonSize *= scale;
            buttonGap  *= scale;
        }

        float totalWidth = numberButtons.Length * buttonSize + (numberButtons.Length - 1) * buttonGap + 2f * pickerPadding;
        float totalHeight = buttonSize + 2f * pickerPadding;

        pickerPanel.sizeDelta = new Vector2(totalWidth, totalHeight);

        // Let HorizontalLayoutGroup own child positioning.
        HorizontalLayoutGroup layout = pickerPanel.GetComponent<HorizontalLayoutGroup>();

        if (layout != null) layout.spacing = buttonGap;

        for (int i = 0; i < numberButtons.Length; i++)
        {
            RectTransform rt = numberButtons[i].GetComponent<RectTransform>();

            rt.sizeDelta = new Vector2(buttonSize, buttonSize);

            TextMeshProUGUI lbl = numberButtons[i].GetComponentInChildren<TextMeshProUGUI>();

            if (lbl != null) lbl.fontSize = Mathf.Round(buttonSize * 0.52f);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate( pickerPanel );
    }

    // ── Show / Hide ───────────────────────────────────────────────────────────

    private void ShowPicker()
    {
        if (selectedCellRT == null) return;

        lastPickerPos = CalculatePickerPosition();

        // Cancel any running hide animation
        if (activeAnimation != null) StopCoroutine(activeAnimation);

        // Fade in overlay
        StartCoroutine(UIAnimator.FadeIn(overlayPanel, overlayAlpha, 0.2f));

        // Slide picker in
        activeAnimation = StartCoroutine(
            UIAnimator.SlideIn(pickerPanel, lastPickerPos, slideOffset, slideInDuration));
    }

    private void HideWithAnimation()
    {
        if (activeAnimation != null) StopCoroutine(activeAnimation);

        if (overlayPanel != null) overlayPanel.gameObject.SetActive(false);

        if (pickerPanel != null && pickerPanel.gameObject.activeSelf)
            activeAnimation = StartCoroutine(
                UIAnimator.SlideOut(pickerPanel, lastPickerPos, slideOffset * 0.6f, slideOutDuration));
    }

    public void Hide()
    {
        if (activeAnimation != null) StopCoroutine(activeAnimation);
        if (pickerPanel  != null) pickerPanel.gameObject.SetActive(false);
        if (overlayPanel != null) overlayPanel.gameObject.SetActive(false);
        selectedCellRT = null;
    }

    private Vector2 CalculatePickerPosition()
    {
        Canvas        canvas   = pickerPanel.GetComponentInParent<Canvas>();
        RectTransform canvasRT = canvas.GetComponent<RectTransform>();

        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                          ? null : canvas.worldCamera;

        Vector2 cellScreenPos = RectTransformUtility.WorldToScreenPoint(uiCamera, selectedCellRT.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT, cellScreenPos, uiCamera, out Vector2 cellLocalPos);

        float cellHalfH   = selectedCellRT.rect.height / 2f;
        float pickerHalfH = pickerPanel.sizeDelta.y    / 2f;
        float pickerHalfW = pickerPanel.sizeDelta.x    / 2f;

        float belowY  = cellLocalPos.y - cellHalfH - yGap - pickerHalfH;
        float aboveY  = cellLocalPos.y + cellHalfH + yGap;// + pickerHalfH;
        float pickerY = (belowY - pickerHalfH < -gridHalfH) ? aboveY : belowY;

        float pickerX = Mathf.Clamp(cellLocalPos.x, -gridHalfW + pickerHalfW, gridHalfW - pickerHalfW);
        pickerY       = Mathf.Clamp(pickerY,        -gridHalfH + pickerHalfH, gridHalfH - pickerHalfH);

        return new Vector2(pickerX, pickerY);
    }

    public bool IsOpen => pickerPanel != null && pickerPanel.gameObject.activeSelf;
}