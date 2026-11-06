using System.Collections.Generic;
//using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    private bool diagonalVariantActive;
    private bool killerVariantActive;
    private KillerCage[] killerCages;
    private GameObject[] killerCellOverlays = new GameObject[81];


    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    void Start()
    {
        if (gridBackground != null)
            gridBackground.color = new Color(0.05f, 0.08f, 0.14f, 0.08f);

    }

    // ── Binding ───────────────────────────────────────────────────────────────

    public void Bind(BaseViewModel vm)
    {
        if (ReferenceEquals(viewModel, vm)) return;
        Unbind();
        viewModel = vm;

        vm.BoardValues.OnChanged      += OnBoardChanged;
        vm.GivenMask.OnChanged        += OnBoardChanged;
        vm.KillerCages.OnChanged      += OnKillerCagesChanged;
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
            OnKillerCagesChanged(viewModel.KillerCages.Value);
        }
    }

    public void SetDiagonalVariantActive(bool active)
    {
        if (diagonalVariantActive == active) return;

        diagonalVariantActive = active;
        RefreshHighlights();
    }

    public void SetKillerVariantActive(bool active)
    {
        killerVariantActive = active;
        RefreshKillerCages();
    }

    public void RefreshKillerCageLayout() => RefreshKillerCages();


    private void OnDestroy()
    {
        Unbind();
    }

    private void Unbind()
    {
        if (viewModel == null) return;

        viewModel.BoardValues.OnChanged      -= OnBoardChanged;
        viewModel.GivenMask.OnChanged        -= OnBoardChanged;
        viewModel.KillerCages.OnChanged      -= OnKillerCagesChanged;
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

    private void OnKillerCagesChanged(KillerCage[] cages)
    {
        if (ReferenceEquals(killerCages, cages)) return;
        killerCages = cages;
        RefreshKillerCages();
    }

    private void RefreshKillerCages()
    {
        if (!cellsReady) return;
        bool hasCages = killerVariantActive && killerCages != null && killerCages.Length > 0;
        if (!hasCages)
        {
            for (int i = 0; i < killerCellOverlays.Length; i++)
                if (killerCellOverlays[i] != null) killerCellOverlays[i].SetActive(false);
            return;
        }

        for (int i = 0; i < killerCellOverlays.Length; i++)
        {
            if (killerCellOverlays[i] != null) Destroy(killerCellOverlays[i]);
            killerCellOverlays[i] = null;
        }

        var cageAt = new int[81];
        for (int i = 0; i < cageAt.Length; i++) cageAt[i] = -1;
        for (int cageIndex = 0; cageIndex < killerCages.Length; cageIndex++)
        {
            int[] cageCells = killerCages[cageIndex]?.Cells;
            if (cageCells == null) continue;
            foreach (int cellIndex in cageCells)
                if (cellIndex >= 0 && cellIndex < 81) cageAt[cellIndex] = cageIndex;
        }

        Color32[] cagePalette =
        {
            new Color32(59, 159, 199, 255),
            new Color32(108, 183, 131, 255),
            new Color32(178, 130, 204, 255),
            new Color32(226, 171, 76, 255),
            new Color32(75, 132, 205, 255),
            new Color32(209, 116, 139, 255),
            new Color32(81, 184, 177, 255),
            new Color32(192, 157, 91, 255)
        };
        int[] cageColors = new int[killerCages.Length];
        for (int i = 0; i < cageColors.Length; i++) cageColors[i] = -1;

        // Adjacent cages get different colors, while non-adjacent cages can
        // reuse a color. This makes the cage layout easy to parse at a glance.
        for (int cageIndex = 0; cageIndex < killerCages.Length; cageIndex++)
        {
            bool[] usedColors = new bool[cagePalette.Length];
            int[] cageCells = killerCages[cageIndex]?.Cells;
            if (cageCells != null)
            {
                foreach (int cellIndex in cageCells)
                {
                    if (cellIndex < 0 || cellIndex >= 81) continue;
                    int row = cellIndex / 9;
                    int col = cellIndex % 9;
                    MarkNeighborColor(row > 0 ? cellIndex - 9 : -1, cageIndex, cageAt, cageColors, usedColors);
                    MarkNeighborColor(col < 8 ? cellIndex + 1 : -1, cageIndex, cageAt, cageColors, usedColors);
                    MarkNeighborColor(row < 8 ? cellIndex + 9 : -1, cageIndex, cageAt, cageColors, usedColors);
                    MarkNeighborColor(col > 0 ? cellIndex - 1 : -1, cageIndex, cageAt, cageColors, usedColors);
                }
            }

            int colorIndex = 0;
            while (colorIndex < usedColors.Length && usedColors[colorIndex]) colorIndex++;
            cageColors[cageIndex] = colorIndex < usedColors.Length
                ? colorIndex
                : cageIndex % cagePalette.Length;
        }

        for (int cageIndex = 0; cageIndex < killerCages.Length; cageIndex++)
        {
            KillerCage cage = killerCages[cageIndex];
            if (cage == null || cage.Cells == null || cage.Cells.Length == 0) continue;
            int labelCell = 81;
            foreach (int cellIndex in cage.Cells)
                if (cellIndex >= 0 && cellIndex < labelCell) labelCell = cellIndex;

            foreach (int cellIndex in cage.Cells)
            {
                if (cellIndex < 0 || cellIndex >= 81 || cells[cellIndex / 9, cellIndex % 9] == null) continue;
                Color32 cageColor = cagePalette[cageColors[cageIndex]];
                cageColor.a = 72;
                killerCellOverlays[cellIndex] = CreateKillerCellOverlay(
                    cells[cellIndex / 9, cellIndex % 9], cage.TargetSum,
                    cellIndex == labelCell, cageColor);
            }
        }
    }

    private static void MarkNeighborColor(
        int neighborCell, int currentCage, int[] cageAt, int[] cageColors, bool[] usedColors)
    {
        if (neighborCell < 0) return;
        int neighborCage = cageAt[neighborCell];
        if (neighborCage >= 0 && neighborCage < currentCage && cageColors[neighborCage] >= 0)
            usedColors[cageColors[neighborCage]] = true;
    }

    private static GameObject CreateKillerCellOverlay(
        SudokuCell cell, int targetSum, bool showTarget, Color fillColor)
    {
        var overlay = new GameObject("Killer Cage Cell Tint", typeof(RectTransform), typeof(Image));
        RectTransform root = overlay.GetComponent<RectTransform>();
        root.SetParent(cell.transform, false);
        overlay.layer = cell.gameObject.layer;
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        root.SetAsFirstSibling();

        RectTransform cellRect = cell.GetComponent<RectTransform>();
        Image tint = overlay.GetComponent<Image>();
        tint.color = fillColor;
        tint.raycastTarget = false;

        if (showTarget)
        {
            var labelObject = new GameObject("Cage Sum", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.SetParent(root, false);
            labelObject.layer = cell.gameObject.layer;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(1f, 1f);
            labelRect.offsetMax = new Vector2(-1f, -1f);
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = targetSum.ToString();
            label.font = cell.NumberFont != null ? cell.NumberFont : TMP_Settings.defaultFontAsset;
            // Watermark-sized cage sums sit behind normal Sudoku digits.
            label.fontSize = Mathf.Clamp(cellRect.rect.width * 0.76f, 14f, 42f);
            label.fontStyle = FontStyles.Bold;
            label.color = new Color32(255, 228, 169, 112);
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            label.enabled = true;
            label.gameObject.layer = cell.gameObject.layer;
            label.ForceMeshUpdate(true, true);
        }
        return overlay;
    }

    // ── Binding Handlers ──────────────────────────────────────────────────────
    private void OnSelectedRowOrColumnChanged(int _)
    {
        RefreshHighlights();
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
                if (cell == null) continue;

                cell.SetPencilModeActive(isPencilMode);
                if (cell.pencilCell == null) continue;

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

        int selectedRow = viewModel.SelectedRow.Value;
        int selectedCol = viewModel.SelectedCol.Value;
        bool hasSelection = selectedRow >= 0 && selectedRow < 9
                         && selectedCol >= 0 && selectedCol < 9;

        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
            {
                bool isSelected = hasSelection && row == selectedRow && col == selectedCol;
                bool isRelated = hasSelection && !isSelected
                    && (row == selectedRow || col == selectedCol);
                bool isDiagonal = diagonalVariantActive && (row == col || row + col == 8);

                SudokuCell cell = cells[row, col];
                cell.SetHighlight(false);
                cell.SetRelatedHighlight(false);
                cell.SetDigitMatchHighlight(false);
                cell.SetDiagonalHighlight(isDiagonal);
                cell.SetDimmed(isRelated);
                cell.SetPickerHighlight(isSelected);
            }

        // Keep matching digits highlighted independently of the row/column selection.
        RefreshMatchingDigitHighlights();
    }

    private void RefreshMatchingDigitHighlights()
    {
        if (!cellsReady || viewModel == null) return;

        int selectedRow = viewModel.SelectedRow.Value;
        int selectedCol = viewModel.SelectedCol.Value;
        bool hasSelection = selectedRow >= 0 && selectedRow < 9
                         && selectedCol >= 0 && selectedCol < 9;
        bool selectedCellHasValue = hasSelection && cells[selectedRow, selectedCol].Value > 0;
        int selectedDigit = selectedCellHasValue
            ? cells[selectedRow, selectedCol].Value
            : viewModel.SelectedDigit.Value;
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
            {
                SudokuCell cell = cells[row, col];
                bool isDigitMatch = selectedDigit > 0 && cell.Value == selectedDigit;
                cell.SetDigitMatchHighlight(isDigitMatch);

                if (selectedCellHasValue)
                {
                    bool isSelected = row == selectedRow && col == selectedCol;
                    bool isRelated = !isSelected
                        && (row == selectedRow || col == selectedCol);

                    // Matching values use their digit-match color even when they
                    // also share the selected cell's row, column, or box.
                    cell.SetDimmed(isRelated && !isDigitMatch);
                    cell.SetPickerHighlight(isSelected);
                }
            }
    }

    private void OnPickerOpenChanged(bool _)
    {
        if (!cellsReady) return;

        // Picker visibility no longer changes the board palette: selection
        // highlights stay limited to the selected cell and its related cells.
        RefreshHighlights();
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
        RefreshKillerCages();

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
}
