using System.Collections.Generic;
//using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// View — manages the 9x9 grid of SudokuCell views.
/// Handles board rendering, highlights, dimming, entry animations and persistent error colors.
/// </summary>
public class SudokuGrid : MonoBehaviour
{
    [Header("Background")]
    [SerializeField] private Image gridBackground;

    private SudokuCell[,]  cells      = new SudokuCell[9, 9];
    private bool           cellsReady = false;
    private BaseViewModel viewModel;

    private const int LightDashesPerArm = 28;
    private static readonly Vector2[] LightArmDirections =
    {
        Vector2.right, Vector2.left, Vector2.up, Vector2.down
    };
    private readonly Image[,] lightDashes = new Image[4, LightDashesPerArm];
    private RectTransform lightOverlay;
    private Vector2 lightOrigin;
    private float lightElapsed;
    private float lightDuration;
    private bool lightEffectActive;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    void Start()
    {
        if (gridBackground != null)
            gridBackground.color = new Color(0.1f, 0.1f, 0.18f, 1f);

    }

    private void LateUpdate()
    {
        if (!lightEffectActive || lightOverlay == null) return;

        lightElapsed += Time.unscaledDeltaTime;
        RenderSelectionLight();

        if (lightElapsed >= lightDuration)
            StopSelectionLight();
    }

    private void EnsureSelectionLightOverlay()
    {
        if (lightOverlay != null || cells[0, 0] == null) return;

        Transform gridPanel = cells[0, 0].transform.parent.parent;
        GameObject overlayObject = new GameObject("SelectionLightOverlay", typeof(RectTransform));
        lightOverlay = overlayObject.GetComponent<RectTransform>();
        lightOverlay.SetParent(gridPanel, false);
        lightOverlay.anchorMin = Vector2.zero;
        lightOverlay.anchorMax = Vector2.one;
        lightOverlay.pivot = new Vector2(0.5f, 0.5f);
        lightOverlay.offsetMin = Vector2.zero;
        lightOverlay.offsetMax = Vector2.zero;
        lightOverlay.SetAsLastSibling();

        for (int arm = 0; arm < LightArmDirections.Length; arm++)
        {
            for (int i = 0; i < LightDashesPerArm; i++)
            {
                GameObject dashObject = new GameObject(
                    "LightDash",
                    typeof(RectTransform),
                    typeof(Image));
                dashObject.transform.SetParent(lightOverlay, false);

                RectTransform dashRect = dashObject.GetComponent<RectTransform>();
                dashRect.anchorMin = new Vector2(0.5f, 0.5f);
                dashRect.anchorMax = new Vector2(0.5f, 0.5f);
                dashRect.pivot = new Vector2(0.5f, 0.5f);

                Image dashImage = dashObject.GetComponent<Image>();
                dashImage.raycastTarget = false;
                dashImage.enabled = false;
                lightDashes[arm, i] = dashImage;
            }
        }
    }

    private void StartSelectionLight(SudokuCell cell)
    {
        if (cell == null) return;

        EnsureSelectionLightOverlay();
        if (lightOverlay == null) return;

        lightOrigin = lightOverlay.InverseTransformPoint(cell.transform.position);
        lightElapsed = 0f;
        lightEffectActive = true;
        RenderSelectionLight();
    }

    private void RenderSelectionLight()
    {
        Rect bounds = lightOverlay.rect;
        float boardSize = Mathf.Min(bounds.width, bounds.height);
        float dashLength = Mathf.Clamp(boardSize / 64f, 8f, 16f);
        float dashThickness = Mathf.Clamp(dashLength * 0.25f, 2f, 4f);
        float dashSpacing = dashLength * 1.8f;
        float speed = Mathf.Max(boardSize * 0.9f, 1f);
        float longestArm = 0f;

        for (int arm = 0; arm < LightArmDirections.Length; arm++)
        {
            Vector2 direction = LightArmDirections[arm];
            float maxDistance = direction.x > 0f ? bounds.xMax - lightOrigin.x :
                               direction.x < 0f ? lightOrigin.x - bounds.xMin :
                               direction.y > 0f ? bounds.yMax - lightOrigin.y :
                                                  lightOrigin.y - bounds.yMin;
            maxDistance = Mathf.Max(0f, maxDistance);
            longestArm = Mathf.Max(longestArm, maxDistance);

            for (int i = 0; i < LightDashesPerArm; i++)
            {
                Image dash = lightDashes[arm, i];
                float distance = i * dashSpacing + lightElapsed * speed;
                bool visible = distance <= maxDistance + dashLength;

                dash.enabled = visible;
                if (!visible) continue;

                RectTransform dashRect = dash.rectTransform;
                dashRect.anchoredPosition = lightOrigin + direction * distance;
                dashRect.sizeDelta = direction.x != 0f
                    ? new Vector2(dashLength, dashThickness)
                    : new Vector2(dashThickness, dashLength);

                float edgeFade = 1f - 0.3f * Mathf.Clamp01(distance / Mathf.Max(maxDistance, 1f));
                dash.color = new Color(0.72f, 0.92f, 1f, 0.88f * edgeFade);
            }
        }

        lightDuration = longestArm / speed + 0.08f;
    }

    private void StopSelectionLight()
    {
        lightEffectActive = false;
        for (int arm = 0; arm < LightArmDirections.Length; arm++)
            for (int i = 0; i < LightDashesPerArm; i++)
                lightDashes[arm, i].enabled = false;
    }

    // ── Binding ───────────────────────────────────────────────────────────────

    public void Bind(BaseViewModel vm)
    {
        if (ReferenceEquals(viewModel, vm)) return;
        Unbind();
        viewModel = vm;

        vm.BoardValues.OnChanged      += OnBoardChanged;
        vm.GivenMask.OnChanged        += OnBoardChanged;
        vm.SelectedRow.OnChanged      += OnSelectedRowOrColumnChanged;
        vm.SelectedCol.OnChanged      += OnSelectedRowOrColumnChanged;
        vm.SelectedDigit.OnChanged    += OnSelectedDigitChanged;
        vm.IsPickerOpen.OnChanged     += OnPickerOpenChanged;
        vm.LastEnteredCell.OnChanged  += OnCellValueEntered;
        vm.ConflictingCells.OnChanged += OnConflictsChanged;
        vm.IsEraseMode.OnChanged      += OnEraseModeChanged;
        vm.IsPencilMode.OnChanged     += OnPencilModeChanged;
        vm.HighlightedCandidateNumber.OnChanged += OnCandidateHighlightChanged;
        vm.PencilCandidateMasks.OnChanged += OnPencilCandidatesChanged;

        if (cellsReady)
        {
            for (int row = 0; row < 9; row++)
                for (int col = 0; col < 9; col++)
                    cells[row, col].Bind(row, col, viewModel);

            OnBoardChanged<int[,]>(null);
            RefreshHighlights();
            RefreshMatchingDigitHighlights();
            OnConflictsChanged(viewModel.ConflictingCells.Value);
            OnPencilCandidatesChanged(viewModel.PencilCandidateMasks.Value);
            OnPencilModeChanged(viewModel.IsPencilMode.Value);
            OnCandidateHighlightChanged(viewModel.HighlightedCandidateNumber.Value);
        }
    }


    private void OnDestroy()
    {
        Unbind();
    }

    private void Unbind()
    {
        if (viewModel == null) return;

        viewModel.BoardValues.OnChanged      -= OnBoardChanged;
        viewModel.GivenMask.OnChanged        -= OnBoardChanged;
        viewModel.SelectedRow.OnChanged      -= OnSelectedRowOrColumnChanged;
        viewModel.SelectedCol.OnChanged      -= OnSelectedRowOrColumnChanged;
        viewModel.SelectedDigit.OnChanged    -= OnSelectedDigitChanged;
        viewModel.IsPickerOpen.OnChanged     -= OnPickerOpenChanged;
        viewModel.LastEnteredCell.OnChanged  -= OnCellValueEntered;
        viewModel.ConflictingCells.OnChanged -= OnConflictsChanged;
        viewModel.IsEraseMode.OnChanged      -= OnEraseModeChanged;
        viewModel.IsPencilMode.OnChanged     -= OnPencilModeChanged;
        viewModel.HighlightedCandidateNumber.OnChanged -= OnCandidateHighlightChanged;
        viewModel.PencilCandidateMasks.OnChanged -= OnPencilCandidatesChanged;
        viewModel = null;
    }

    // ── Binding Handlers ──────────────────────────────────────────────────────
    private void OnSelectedRowOrColumnChanged(int _)
    {
        RefreshHighlights();

        int row = viewModel.SelectedRow.Value;
        int col = viewModel.SelectedCol.Value;
        if (row >= 0 && row < 9 && col >= 0 && col < 9)
            StartSelectionLight(cells[row, col]);
    }

    private void OnSelectedDigitChanged(int _)
    {
        RefreshMatchingDigitHighlights();
    }
    private void OnPencilCandidatesChanged(int[,] candidateMasks)
    {
        if (!cellsReady || viewModel == null) return;

        for (int row = 0;row < 9;row++)
            for (int col = 0;col < 9;col++)
                cells[row, col].SetPencilCandidates(candidateMasks == null ? 0 : candidateMasks[row, col]);
    }
    private void OnCandidateHighlightChanged(
        int number)
    {
        if (!cellsReady) return;

        for (int row = 0;row < 9;row++)
        {
            for (int col = 0;col < 9;col++)
            {
                cells[row, col].SetCandidateHighlight(number);
            }
        }
    }
    private void OnPencilModeChanged(bool isPencilMode)
    {
        if (!cellsReady) return;

        for (int row = 0;row < 9;row++)
        {
            for (int col = 0;col < 9;col++)
            {
                SudokuCell cell = cells[row, col];
                if (cell == null || cell.pencilCell == null) continue;

                // Always write the active state. Otherwise a pencil panel
                // left visible by the previous game mode can leak into the
                // newly bound Journey board when that cell is filled.
                cell.pencilCell.SetActive(isPencilMode && cell.Value == 0);
            }
        }

        if (!isPencilMode)
        {
            OnCandidateHighlightChanged(0);
        }
    }
    private void OnEraseModeChanged(bool arg)
    {
        if (!cellsReady) return;

        for(int row = 0; row < 9; row++)
            for( int col = 0; col < 9; col++)
                if(cells[row, col] != null && cells[row, col].IsGiven == false)
                {
                    Transform eraseTransform = cells[row, col].transform.Find("Erase");
                    if (eraseTransform == null) continue;

                    GameObject obj = eraseTransform.gameObject;
                    if(arg == false)
                        obj.SetActive(arg); 
                    else if(arg == true && cells[row,col].Value != 0)
                        obj.SetActive(arg);          
                }
    }

    private void OnBoardChanged<T>(T _)
    {
        if (!cellsReady || viewModel == null) return;

        int[,]  board = viewModel.BoardValues.Value;
        bool[,] given = viewModel.GivenMask.Value;

        if (board == null || given == null) return;

        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
                cells[row, col].SetValue(board[row, col], given[row, col]);

        RefreshHighlights();
    }

    private void RefreshHighlights()
    {
        if (!cellsReady || viewModel == null) return;

        int selRow = viewModel.SelectedRow.Value;
        int selCol = viewModel.SelectedCol.Value;

        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
                cells[row, col].SetHighlight(row == selRow && col == selCol);

        RefreshMatchingDigitHighlights();
    }

    private void RefreshMatchingDigitHighlights()
    {
        if (!cellsReady || viewModel == null) return;

        int selectedDigit = viewModel.SelectedDigit.Value;
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
                cells[row, col].SetDigitMatchHighlight(
                    selectedDigit > 0 && cells[row, col].Value == selectedDigit);
    }

    private void OnPickerOpenChanged(bool isOpen)
    {
        if (!cellsReady) return;

        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
            {
                bool isSelected = row == viewModel.SelectedRow.Value
                               && col == viewModel.SelectedCol.Value;
                if (isOpen)
                {
                    if (isSelected) cells[row, col].SetPickerHighlight(true);
                    else            cells[row, col].SetDimmed(true);
                }
                else
                {
                    cells[row, col].SetDimmed(false);
                    cells[row, col].SetPickerHighlight(false);
                }
            }
    }

    /// <summary>
    /// Fires after a number is entered.
    /// Plays bounce on clean entry, shake+flash on conflict.
    /// Persistent error color is handled separately by OnConflictsChanged.
    /// </summary>
    private void OnCellValueEntered((int row,int col,bool hasConflict) entry)
    {
        if (!cellsReady) return;

        SudokuCell enteredCell = cells[entry.row,entry.col];
        StartSelectionLight(enteredCell);

        if (entry.hasConflict)
        {
            enteredCell.PlayErrorAnimation();
            return;
        }

        enteredCell.PlayEntryAnimation();

    }
    /// <summary>
    /// Fires every time the conflict set changes.
    /// Sets persistent error color on all conflicting cells,
    /// and clears it on cells that are no longer conflicting.
    /// </summary>
    private void OnConflictsChanged(HashSet<(int row, int col)> conflicts)
    {
        if (!cellsReady) return;

        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
                cells[row, col].SetConflict(conflicts.Contains((row, col)));
    }

    // ── Cell Setup ────────────────────────────────────────────────────────────

    public void SetCells(SudokuCell[] allCells)
    {
        cells = new SudokuCell[9, 9];

        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
            {
                cells[row, col] = allCells[row * 9 + col];
                cells[row, col].Bind(row, col, viewModel);
            }

        cellsReady = true;
        EnsureSelectionLightOverlay();

        if (viewModel != null)
        {
            OnBoardChanged<int[,]>(null);
            RefreshHighlights();
            RefreshMatchingDigitHighlights();
            OnConflictsChanged(viewModel.ConflictingCells.Value);
            OnPencilCandidatesChanged(viewModel.PencilCandidateMasks.Value);
            OnPencilModeChanged(viewModel.IsPencilMode.Value);
            OnCandidateHighlightChanged(viewModel.HighlightedCandidateNumber.Value);
        }
    }

    public void SaveCurrentState() { }
}
