using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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
    [SerializeField] private Button sosButton;
    [SerializeField] private Button eraseButton;

    [Header("Outer spacing")]
    [SerializeField] private float pickerPadding = 8f;
    [SerializeField] private float screenSidePadding = 16f;

    [Header("Portrait")]
    [SerializeField] private float portraitGapBelowGrid = 18f;
    [SerializeField] private float portraitButtonScale = 2.1f;
    [SerializeField] private float portraitGapScale = 0.32f;
    [SerializeField, Min(0f)] private float portraitUtilityGapScale = 0.2f;

    [Header("Landscape")]
    [SerializeField] private float landscapeGapBesideGrid = 24f;
    [SerializeField] private float landscapeButtonScale = 2.5f;
    [SerializeField] private float landscapeGapScale = 0.24f;

    [Header("Number label appearance")]
    [SerializeField, Range(0.5f, 1.25f)]
    private float numberFontSizeCellRatio = 0.86f;

    [Header("Animation")]
    [SerializeField] private float slideOffset = 50f;
    [SerializeField] private float slideInDuration = 0.25f;
    [SerializeField] private float slideOutDuration = 0.15f;

    private float cellSize = 56f;

    private BaseViewModel viewModel;
    private RectTransform selectedCellRT;

    private Vector2 lastPickerPos;
    private Coroutine activeAnimation;

    private readonly Vector3[] gridCorners = new Vector3[4];
    private readonly List<RaycastResult> pointerHits = new List<RaycastResult>();

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

        if (numberButtons != null)
        {
            for (int i = 0; i < numberButtons.Length; i++)
            {
                int number = i + 1;

                Button button = numberButtons[i];

                if (button == null) continue;

                // Keep the button face white and use the same per-number
                // accent color as digits on the Sudoku board.
                Image buttonBackground = button.targetGraphic as Image;
                if (buttonBackground == null)
                    buttonBackground = button.GetComponent<Image>();
                if (buttonBackground != null)
                    buttonBackground.color = new Color32(120, 83, 29, 255);

                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.gameObject.SetActive(true);
                    label.enabled = true;
                    label.text = number.ToString();
                    label.color = GetDigitColor(number);
                }

                button.onClick.AddListener(() =>
                {
                    if (viewModel?.IsPencilMode.Value == true)
                        viewModel.TogglePencilCandidateCommand.Execute(number);
                    else viewModel?.EnterValueCommand.Execute(number);
                });

            }
        }

        if (sosButton != null)
            sosButton.onClick.AddListener(ClosePickerAfterUtilityAction);
        if (eraseButton != null)
            eraseButton.onClick.AddListener(ClosePickerAfterUtilityAction);

        if (pickerPanel != null) pickerPanel.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!IsOpen || viewModel == null) return;
        if (!TryGetPointerDownPosition(out Vector2 screenPosition)) return;

        Canvas canvas = pickerPanel.GetComponentInParent<Canvas>();
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        if (RectTransformUtility.RectangleContainsScreenPoint( pickerPanel, screenPosition, uiCamera)) return;
        if (EventSystem.current != null)
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = screenPosition };

            pointerHits.Clear();
            EventSystem.current.RaycastAll( pointerData, pointerHits);

            if (pointerHits.Count > 0)
            {
                GameObject topHit = pointerHits[0].gameObject;
                SudokuCell cell = topHit.GetComponentInParent<SudokuCell>();

                if (cell != null && !cell.IsGiven && cell.Value == 0) return;

                // SOS and Erase operate on the selected cell. Dismiss only
                // the picker UI so their click handlers retain that selection.
                bool isSOSButton =
                    sosButton != null &&
                    (topHit == sosButton.gameObject ||
                     topHit.transform.IsChildOf(sosButton.transform));
                bool isEraseButton =
                    eraseButton != null &&
                    (topHit == eraseButton.gameObject ||
                     topHit.transform.IsChildOf(eraseButton.transform));

                if (isSOSButton || isEraseButton)
                {
                    viewModel.IsPickerOpen.Value = false;
                    return;
                }
            }
        }

        viewModel.CancelPickerCommand.Execute();
    }

    private bool TryGetPointerDownPosition( out Vector2 screenPosition)
    {
        // Android / mobile touch
        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;

            if (touch.press.wasPressedThisFrame)
            {
                screenPosition = touch.position.ReadValue();

                return true;
            }
        }

        // Editor / WebGL / Google Play Games PC
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPosition = Mouse.current.position.ReadValue();

            return true;
        }

        screenPosition = Vector2.zero;
        return false;
    }

    public void Bind(BaseViewModel vm)
    {
        if (ReferenceEquals(viewModel, vm)) return;
        if (viewModel != null)
        {
            viewModel.IsPickerOpen.OnChanged -= OnPickerOpenChanged;
            viewModel.SelectedCellTransform.OnChanged -= OnSelectedCellTransformChanged;
        }

        viewModel = vm;

        vm.IsPickerOpen.OnChanged += OnPickerOpenChanged;
        vm.SelectedCellTransform.OnChanged += OnSelectedCellTransformChanged;
    }

    private void OnDestroy()
    {
        if (sosButton != null)
            sosButton.onClick.RemoveListener(ClosePickerAfterUtilityAction);
        if (eraseButton != null)
            eraseButton.onClick.RemoveListener(ClosePickerAfterUtilityAction);

        if (viewModel == null) return;

        viewModel.IsPickerOpen.OnChanged -= OnPickerOpenChanged;
        viewModel.SelectedCellTransform.OnChanged -= OnSelectedCellTransformChanged;
    }

    private void ClosePickerAfterUtilityAction()
    {
        if (viewModel != null)
            viewModel.IsPickerOpen.Value = false;
    }

    private void OnSelectedCellTransformChanged( object cellTransform)
    {
        selectedCellRT = cellTransform as RectTransform;
    }

    public void SetSelectedCellTransform( RectTransform cellRT)
    {
        selectedCellRT = cellRT;
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
        float rowGap = cellSize * portraitGapScale;
        Rect parentBounds = GetParentBounds();
        float rowWidth = parentBounds.width - 2f * screenSidePadding;
        float availableHeight = parentBounds.height - 2f * screenSidePadding;

        if (gridPanel != null)
        {
            GetGridBounds(out float gridLeft, out float gridRight, out float gridBottom, out _);
            rowWidth = gridRight - gridLeft;
            availableHeight = Mathf.Min(
                availableHeight,
                gridBottom - portraitGapBelowGrid - (parentBounds.yMin + screenSidePadding));
        }

        float contentWidth = numberButtons.Length * buttonSize +
                             (numberButtons.Length - 1) * buttonGap;
        float contentHeight = 2f * buttonSize + rowGap;
        float scale = 1f;

        if (contentWidth > rowWidth && rowWidth > 0f)
            scale = Mathf.Min(scale, rowWidth / contentWidth);

        float maxContentHeight = availableHeight - 2f * pickerPadding;
        if (contentHeight > maxContentHeight && maxContentHeight > 0f)
            scale = Mathf.Min(scale, maxContentHeight / contentHeight);

        scale = Mathf.Clamp(scale, 0.05f, 1f);
        buttonSize *= scale;
        buttonGap *= scale;
        rowGap *= scale;

        float finalContentWidth = numberButtons.Length * buttonSize +
                                  (numberButtons.Length - 1) * buttonGap;
        if (numberButtons.Length > 1 && finalContentWidth < rowWidth)
        {
            buttonGap += (rowWidth - finalContentWidth) / (numberButtons.Length - 1);
            finalContentWidth = rowWidth;
        }

        pickerPanel.sizeDelta = new Vector2(
            finalContentWidth,
            2f * buttonSize + rowGap + 2f * pickerPadding);
        ResizeNumberLabels();

        float rowOffset = (buttonSize + rowGap) / 2f;
        float startX = -finalContentWidth / 2f + buttonSize / 2f;
        for (int i = 0; i < numberButtons.Length; i++)
        {
            PositionPickerButton(
                numberButtons[i], buttonSize,
                startX + i * (buttonSize + buttonGap), rowOffset);
        }

        float utilityGap = buttonSize * portraitUtilityGapScale;
        float utilityOffset = (buttonSize + utilityGap) / 2f;
        PositionPickerButton(eraseButton, buttonSize, -utilityOffset, -rowOffset);
        PositionPickerButton(sosButton, buttonSize, utilityOffset, -rowOffset);
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
            GetGridBounds(out float gridLeft, out float gridRight, out float gridBottom, out float gridTop);
            float rightSpace = parentBounds.xMax - screenSidePadding - gridRight - landscapeGapBesideGrid;
            float leftSpace = gridLeft - landscapeGapBesideGrid - (parentBounds.xMin + screenSidePadding);
            maxPanelWidth = Mathf.Max(rightSpace, leftSpace);
            maxPanelHeight = Mathf.Min(maxPanelHeight, gridTop - gridBottom);
        }

        float contentWidth = 3f * buttonSize + 2f * buttonGap;
        float contentHeight = 4f * buttonSize + 3f * buttonGap;
        float maxContentWidth = maxPanelWidth - 2f * pickerPadding;
        float maxContentHeight = maxPanelHeight - 2f * pickerPadding;
        float scale = 1f;

        if (maxContentWidth > 0f && contentWidth > maxContentWidth)
            scale = Mathf.Min(scale, maxContentWidth / contentWidth);
        if (maxContentHeight > 0f && contentHeight > maxContentHeight)
            scale = Mathf.Min(scale, maxContentHeight / contentHeight);

        scale = Mathf.Clamp(scale, 0.05f, 1f);
        buttonSize *= scale;
        buttonGap *= scale;
        float finalContentWidth = 3f * buttonSize + 2f * buttonGap;
        float finalContentHeight = 4f * buttonSize + 3f * buttonGap;
        pickerPanel.sizeDelta = new Vector2(
            finalContentWidth + 2f * pickerPadding,
            finalContentHeight + 2f * pickerPadding);
        ResizeNumberLabels();

        for (int i = 0; i < numberButtons.Length; i++)
        {
            int row = i / 3;
            int col = i % 3;
            float x = (col - 1) * (buttonSize + buttonGap);
            float y = (1 - row) * (buttonSize + buttonGap);
            PositionPickerButton(numberButtons[i], buttonSize, x, y);
        }

        float utilityY = -2f * (buttonSize + buttonGap);
        float sideX = buttonSize + buttonGap;
        PositionPickerButton(eraseButton, buttonSize, -sideX, utilityY);
        PositionPickerButton(sosButton, buttonSize, sideX, utilityY);
    }

    private void ResizeNumberLabels()
    {
        if (numberButtons == null) return;

        float fontSize = Mathf.Max(1f, cellSize * numberFontSizeCellRatio);

        foreach (Button button in numberButtons)
        {
            if (button == null) continue;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label == null) continue;

            label.enableAutoSizing = false;
            label.fontSize = fontSize;
            label.fontWeight = FontWeight.Bold;
            label.fontStyle = FontStyles.Normal;
        }
    }

    private static void PositionPickerButton(Button button, float size, float x, float y)
    {
        if (button == null) return;

        RectTransform rt = button.GetComponent<RectTransform>();
        if (rt == null) return;

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = new Vector2(x, y);
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

    private static Color GetDigitColor(int number)
    {
        switch (number)
        {
            case 1: return new Color32(39, 196, 238, 255);
            case 2: return new Color32(240, 75, 86, 255);
            case 3: return new Color32(255, 197, 61, 255);
            case 4: return new Color32(131, 201, 74, 255);
            case 5: return new Color32(66, 207, 239, 255);
            case 6: return new Color32(255, 138, 61, 255);
            case 7: return new Color32(244, 66, 138, 255);
            case 8: return new Color32(169, 103, 208, 255);
            case 9: return new Color32(132, 201, 74, 255);
            default: return Color.white;
        }
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
