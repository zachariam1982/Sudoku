using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Number picker.
///
/// Portrait:
///     1 2 3 4 5 6 7 8 9
///     displayed below the Sudoku grid.
///
/// Landscape:
///     1 2 3
///     4 5 6
///     7 8 9
///     displayed beside the Sudoku grid.
///
/// The picker is shared by normal and Pencil modes.
/// </summary>
public class NumberPicker : MonoBehaviour
{
    public static NumberPicker Instance { get; private set; }

    [Header("References")]
    [SerializeField] private RectTransform pickerPanel;
    [SerializeField] private Button[] numberButtons;
    [SerializeField] private Image overlayPanel;
    [SerializeField] private RectTransform gridPanel;

    [Header("Outer spacing")]
    [SerializeField] private float pickerPadding = 8f;
    [SerializeField] private float screenSidePadding = 16f;

    [Header("Portrait")]
    [SerializeField] private float portraitGapBelowGrid = 18f;
    [SerializeField] private float portraitButtonScale = 0.82f;
    [SerializeField] private float portraitGapScale = 0.32f;

    [Header("Landscape")]
    [SerializeField] private float landscapeGapBesideGrid = 24f;
    [SerializeField] private float landscapeButtonScale = 0.90f;
    [SerializeField] private float landscapeGapScale = 0.24f;

    [Header("Number border")]
    [SerializeField] private Color borderColor =
        new Color32(76, 201, 255, 255);

    [SerializeField] private float borderThickness = 3f;

    [Header("Animation")]
    [SerializeField] private float slideOffset = 50f;
    [SerializeField] private float slideInDuration = 0.25f;
    [SerializeField] private float slideOutDuration = 0.15f;

    private float cellSize = 56f;

    private SudokuViewModel viewModel;
    private RectTransform selectedCellRT;
    private SudokuCell _selectedCell;

    private Vector2 lastPickerPos;
    private Coroutine activeAnimation;

    private readonly Vector3[] gridCorners =
        new Vector3[4];

    // ---------------------------------------------------------------------
    // INITIALIZATION
    // ---------------------------------------------------------------------

    private void Awake()
    {
        Instance = this;

        if (pickerPanel != null)
        {
            HorizontalLayoutGroup oldLayout = pickerPanel.GetComponent<HorizontalLayoutGroup>();

            if (oldLayout != null) oldLayout.enabled = false;

            Image panelImage = pickerPanel.GetComponent<Image>();

            if (panelImage != null) panelImage.enabled = false;

            pickerPanel.anchorMin = new Vector2(0.5f, 0.5f);
            pickerPanel.anchorMax = new Vector2(0.5f, 0.5f);
            pickerPanel.pivot = new Vector2(0.5f, 0.5f);
        }

        if (overlayPanel != null)
        {
            overlayPanel.gameObject.SetActive(true);

            Color c = overlayPanel.color;
            c.a = 0f;
            overlayPanel.color = c;

            overlayPanel.raycastTarget = false;

            Button overlayButton = overlayPanel.GetComponent<Button>();

            if (overlayButton != null) overlayButton.enabled = false;
        }

        for (int i = 0; i < numberButtons.Length; i++)
        {
            int number = i + 1;

            Button button = numberButtons[i];

            if (button == null) continue;

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();

            if (label != null) label.text = number.ToString();

            button.onClick.AddListener(() =>
            {
                if (viewModel?.IsPencilMode.Value == true) _selectedCell?.TogglePencilNumber(number); 
                else viewModel?.EnterValueCommand.Execute(number); 
            });

            CreateSkyBlueBorder(button);
        }

        if (pickerPanel != null) pickerPanel.gameObject.SetActive(false);
    }


    public void Bind(SudokuViewModel vm)
    {
        viewModel = vm;

        vm.IsPickerOpen.OnChanged += OnPickerOpenChanged;
        vm.SelectedCellTransform.OnChanged += OnSelectedCellTransformChanged;
    }

    private void OnDestroy()
    {
        if (viewModel == null) return;

        viewModel.IsPickerOpen.OnChanged -= OnPickerOpenChanged;
        viewModel.SelectedCellTransform.OnChanged -= OnSelectedCellTransformChanged;
    }

    private void OnSelectedCellTransformChanged( object cellTransform)
    {
        selectedCellRT = cellTransform as RectTransform;
        _selectedCell = selectedCellRT != null ? selectedCellRT.GetComponent<SudokuCell>() : null;
    }

    public void SetSelectedCellTransform( RectTransform cellRT)
    {
        selectedCellRT = cellRT;
        _selectedCell = cellRT != null ? cellRT.GetComponent<SudokuCell>() : null;
    }

    private void OnPickerOpenChanged(bool isOpen)
    {
        if (isOpen) ShowPicker();
        else HideWithAnimation();
    }

    public void UpdateGridBounds( float gridWidth, float gridHeight, float boxGap, float boxPadding, float cellGap)
    {
        float boxSize = (gridWidth - 2f * boxGap) / 3f;

        cellSize = (boxSize - 2f * boxPadding - 2f * cellGap) / 3f;
        ConfigurePickerLayout();
    }

    private bool IsLandscape()
    {
        return Screen.width > Screen.height;
    }

    // ---------------------------------------------------------------------
    // LAYOUT
    // ---------------------------------------------------------------------

    private void ConfigurePickerLayout()
    {
        if (pickerPanel == null || numberButtons == null || numberButtons.Length == 0) return;


        if (IsLandscape()) ConfigureLandscapeLayout();
        else ConfigurePortraitLayout();
    }

    private void ConfigurePortraitLayout()
    {
        float buttonSize = cellSize * portraitButtonScale;
        float buttonGap = cellSize * portraitGapScale;

        Rect parentBounds = GetParentBounds();

        float maxPanelWidth = parentBounds.width - 2f * screenSidePadding;
        float contentWidth = 9f * buttonSize + 8f * buttonGap;
        float maxContentWidth = maxPanelWidth - 2f * pickerPadding;
        float scale = 1f;

        if (contentWidth > maxContentWidth && maxContentWidth > 0f) scale = Mathf.Min( scale, maxContentWidth / contentWidth);

        if (gridPanel != null)
        {
            GetGridBounds( out _, out _, out float gridBottom, out _);

            float availableBelow = gridBottom - portraitGapBelowGrid - (parentBounds.yMin + screenSidePadding);
            float requiredHeight = buttonSize + 2f * pickerPadding;

            if (availableBelow > 0f && requiredHeight > availableBelow)
            {
                float usable = Mathf.Max( 1f, availableBelow - 2f * pickerPadding);

                scale = Mathf.Min( scale, usable / buttonSize);
            }
        }

        scale = Mathf.Clamp(scale, 0.05f, 1f);

        buttonSize *= scale;
        buttonGap *= scale;

        float finalContentWidth = 9f * buttonSize + 8f * buttonGap;
        float totalWidth = finalContentWidth + 2f * pickerPadding;
        float totalHeight = buttonSize + 2f * pickerPadding;

        pickerPanel.sizeDelta = new Vector2( totalWidth, totalHeight);

        float startX = -finalContentWidth / 2f + buttonSize / 2f;

        for (int i = 0; i < numberButtons.Length; i++)
        {
            RectTransform rt = numberButtons[i].GetComponent<RectTransform>();

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2( buttonSize, buttonSize);
            rt.anchoredPosition = new Vector2( startX + i * (buttonSize + buttonGap), 0f);
            ResizeLabel(numberButtons[i], buttonSize);
        }
    }

    private void ConfigureLandscapeLayout()
    {
        float buttonSize = cellSize * landscapeButtonScale;
        float buttonGap = cellSize * landscapeGapScale;

        Rect parentBounds = GetParentBounds();

        float maxPanelWidth = parentBounds.width * 0.35f;
        float maxPanelHeight = parentBounds.height - 2f * screenSidePadding;

        if (gridPanel != null)
        {
            GetGridBounds( out float gridLeft, out float gridRight, out float gridBottom, out float gridTop);

            float rightSpace = parentBounds.xMax - screenSidePadding - gridRight - landscapeGapBesideGrid;
            float leftSpace = gridLeft - landscapeGapBesideGrid - (parentBounds.xMin + screenSidePadding);

            maxPanelWidth = Mathf.Max( rightSpace, leftSpace);
            maxPanelHeight = Mathf.Min( maxPanelHeight, gridTop - gridBottom);
        }

        float contentSize = 3f * buttonSize + 2f * buttonGap;
        float maxContentWidth = maxPanelWidth - 2f * pickerPadding;
        float maxContentHeight = maxPanelHeight - 2f * pickerPadding;
        float scale = 1f;

        if (maxContentWidth > 0f && contentSize > maxContentWidth) scale = Mathf.Min( scale, maxContentWidth / contentSize);
        if (maxContentHeight > 0f && contentSize > maxContentHeight) scale = Mathf.Min( scale, maxContentHeight / contentSize);

        scale = Mathf.Clamp(scale, 0.05f, 1f);
        buttonSize *= scale;
        buttonGap *= scale;

        float finalContentSize = 3f * buttonSize + 2f * buttonGap;
        float totalSize = finalContentSize + 2f * pickerPadding;

        pickerPanel.sizeDelta = new Vector2( totalSize, totalSize);

        for (int i = 0; i < numberButtons.Length; i++)
        {
            int row = i / 3;
            int col = i % 3;

            RectTransform rt = numberButtons[i].GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2( buttonSize, buttonSize);

            float x = (col - 1) * (buttonSize + buttonGap);
            float y = (1 - row) * (buttonSize + buttonGap);

            rt.anchoredPosition = new Vector2(x, y);
            ResizeLabel(numberButtons[i], buttonSize);
        }
    }

    private void ResizeLabel( Button button, float buttonSize)
    {
        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();

        if (label != null) label.fontSize = Mathf.Round( buttonSize * 0.52f);
    }

    // ---------------------------------------------------------------------
    // POSITION
    // ---------------------------------------------------------------------

    private Vector2 CalculatePickerPosition()
    {
        if (gridPanel == null) return Vector2.zero;

        GetGridBounds( out float gridLeft, out float gridRight, out float gridBottom, out float gridTop);

        Rect parentBounds = GetParentBounds();

        float halfWidth = pickerPanel.sizeDelta.x / 2f;
        float halfHeight = pickerPanel.sizeDelta.y / 2f;

        Vector2 target;

        if (!IsLandscape()) target = new Vector2( (gridLeft + gridRight) / 2f, gridBottom - portraitGapBelowGrid - halfHeight); 
        else
        {
            float gridCenterY = (gridBottom + gridTop) / 2f;
            float rightX = gridRight + landscapeGapBesideGrid + halfWidth;
            float leftX = gridLeft - landscapeGapBesideGrid - halfWidth;
            bool fitsRight = rightX + halfWidth <= parentBounds.xMax - screenSidePadding;
            bool fitsLeft = leftX - halfWidth >= parentBounds.xMin + screenSidePadding;
            float x;

            if (fitsRight)
            {
                x = rightX;
            }
            else if (fitsLeft)
            {
                x = leftX;
            }
            else
            {
                float rightSpace = parentBounds.xMax - gridRight;
                float leftSpace = gridLeft - parentBounds.xMin;

                x = rightSpace >= leftSpace ? rightX : leftX;
            }

            target = new Vector2( x, gridCenterY);
        }

        return ClampPickerToParent( target, parentBounds);
    }

    private Vector2 ClampPickerToParent( Vector2 target, Rect parentBounds)
    {
        float halfWidth = pickerPanel.sizeDelta.x / 2f;
        float halfHeight = pickerPanel.sizeDelta.y / 2f;
        float minX = parentBounds.xMin + screenSidePadding + halfWidth;
        float maxX = parentBounds.xMax - screenSidePadding - halfWidth;
        float minY = parentBounds.yMin + screenSidePadding + halfHeight;
        float maxY = parentBounds.yMax - screenSidePadding - halfHeight;

        if (minX <= maxX) target.x = Mathf.Clamp( target.x, minX, maxX);
        if (minY <= maxY) target.y = Mathf.Clamp( target.y, minY, maxY);

        return target;
    }

    // ---------------------------------------------------------------------
    // COORDINATE HELPERS
    // ---------------------------------------------------------------------

    private void GetGridBounds( out float left, out float right, out float bottom, out float top)
    {
        gridPanel.GetWorldCorners(gridCorners);

        Vector2 bottomLeft = WorldToPickerSpace(gridCorners[0]);
        Vector2 topLeft = WorldToPickerSpace(gridCorners[1]);
        Vector2 topRight = WorldToPickerSpace(gridCorners[2]);
        Vector2 bottomRight = WorldToPickerSpace(gridCorners[3]);

        left = Mathf.Min( bottomLeft.x, topLeft.x);
        right = Mathf.Max( bottomRight.x, topRight.x);
        bottom = Mathf.Min( bottomLeft.y, bottomRight.y);
        top = Mathf.Max( topLeft.y, topRight.y);
    }

    private Vector2 WorldToPickerSpace( Vector3 worldPoint)
    {
        RectTransform parent = pickerPanel.parent as RectTransform;
        Vector3 local = parent.InverseTransformPoint( worldPoint);
        Vector2 anchorReference = GetAnchorReference(parent);

        return new Vector2( local.x - anchorReference.x, local.y - anchorReference.y);
    }

    private Rect GetParentBounds()
    {
        RectTransform parent = pickerPanel.parent as RectTransform;
        Rect r = parent.rect;
        Vector2 anchorReference = GetAnchorReference(parent);

        return Rect.MinMaxRect( r.xMin - anchorReference.x, r.yMin - anchorReference.y, r.xMax - anchorReference.x, r.yMax - anchorReference.y);
    }

    private Vector2 GetAnchorReference( RectTransform parent)
    {
        Rect r = parent.rect;

        return new Vector2( 
                        Mathf.Lerp( r.xMin, r.xMax, pickerPanel.anchorMin.x),
                        Mathf.Lerp( r.yMin, r.yMax, pickerPanel.anchorMin.y));
    }

    private void CreateSkyBlueBorder( Button button)
    {
        CreateBorderLine(
            button.transform,
            "PickerBorderTop",
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, borderThickness));

        CreateBorderLine(
            button.transform,
            "PickerBorderBottom",
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, borderThickness));

        CreateBorderLine(
            button.transform,
            "PickerBorderLeft",
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(0f, 0.5f),
            new Vector2(borderThickness, 0f));

        CreateBorderLine(
            button.transform,
            "PickerBorderRight",
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0.5f),
            new Vector2(borderThickness, 0f));
    }

    private void CreateBorderLine( Transform parent, string objectName, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta)
    {
        if (parent.Find(objectName) != null) return;

        GameObject line = new GameObject( objectName, typeof(RectTransform), typeof(Image));

        line.transform.SetParent( parent, false);

        RectTransform rt = line.GetComponent<RectTransform>();

        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = sizeDelta;

        Image image = line.GetComponent<Image>();

        image.color = borderColor;
        image.raycastTarget = false;
    }

    private void ShowPicker()
    {
        if (selectedCellRT == null || pickerPanel == null) return; 

        ConfigurePickerLayout();
        lastPickerPos = CalculatePickerPosition();

        if (activeAnimation != null) StopCoroutine(activeAnimation);

        activeAnimation = StartCoroutine( UIAnimator.SlideIn( pickerPanel, lastPickerPos, slideOffset, slideInDuration));
    }

    private void HideWithAnimation()
    {
        if (activeAnimation != null) StopCoroutine(activeAnimation);

        if (pickerPanel != null && pickerPanel.gameObject.activeSelf)
        {
            activeAnimation = StartCoroutine( UIAnimator.SlideOut( pickerPanel, lastPickerPos, slideOffset * 0.6f, slideOutDuration));
        }
    }

    public void Hide()
    {
        if (activeAnimation != null) StopCoroutine(activeAnimation);
        if (pickerPanel != null) pickerPanel.gameObject.SetActive(false);

        selectedCellRT = null;
    }

    public bool IsOpen =>
        pickerPanel != null &&
        pickerPanel.gameObject.activeSelf;
}