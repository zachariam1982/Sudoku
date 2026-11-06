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
    private int[] killerCageByCell = new int[81];
    private RectTransform gridPanelRect;
    private RectTransform cageSumBadgeRect;
    private TextMeshProUGUI cageSumBadgeText;
    private readonly List<RectTransform> cageSumLabelRects = new List<RectTransform>();
    private readonly List<TextMeshProUGUI> cageSumLabelTexts = new List<TextMeshProUGUI>();


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
            UpdateCageSumBadge();
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
        UpdateCageSumBadge();
    }

    public void RefreshKillerCageLayout()
    {
        RefreshKillerCages();
        PositionCageSumBadge();
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
        if (cageSumBadgeRect != null) cageSumBadgeRect.gameObject.SetActive(false);
    }

    private void OnKillerCagesChanged(KillerCage[] cages)
    {
        if (ReferenceEquals(killerCages, cages)) return;
        killerCages = cages;
        RefreshKillerCages();
        UpdateCageSumBadge();
    }

    private void RefreshKillerCages()
    {
        if (!cellsReady) return;
        bool hasCages = killerVariantActive && killerCages != null && killerCages.Length > 0;
        for (int i = 0; i < killerCageByCell.Length; i++)
        {
            killerCageByCell[i] = -1;
            if (cells[i / 9, i % 9] != null)
                cells[i / 9, i % 9].SetKillerCageTint(Color.clear, false);
        }
        if (!hasCages)
        {
            ClearKillerCageSumLabels();
            UpdateCageSumBadge();
            return;
        }

        var cageAt = new int[81];
        for (int i = 0; i < cageAt.Length; i++) cageAt[i] = -1;
        for (int cageIndex = 0; cageIndex < killerCages.Length; cageIndex++)
        {
            int[] cageCells = killerCages[cageIndex]?.Cells;
            if (cageCells == null) continue;
            foreach (int cellIndex in cageCells)
                if (cellIndex >= 0 && cellIndex < 81)
                {
                    cageAt[cellIndex] = cageIndex;
                    killerCageByCell[cellIndex] = cageIndex;
                }
        }

        Color32[] cagePalette =
        {
            new Color32(77, 225, 255, 255),  // bright cyan
            new Color32(255, 138, 77, 255),   // orange
            new Color32(190, 120, 255, 255),  // violet
            new Color32(184, 239, 73, 255),   // lime
            new Color32(255, 92, 153, 255),   // rose
            new Color32(84, 145, 255, 255),   // blue
            new Color32(255, 213, 68, 255),   // yellow
            new Color32(72, 218, 163, 255)    // mint
        };
        int[] cageColors = new int[killerCages.Length];
        for (int i = 0; i < cageColors.Length; i++) cageColors[i] = -1;
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
            cageColors[cageIndex] = colorIndex < usedColors.Length ? colorIndex : cageIndex % cagePalette.Length;
        }

        for (int cageIndex = 0; cageIndex < killerCages.Length; cageIndex++)
        {
            KillerCage cage = killerCages[cageIndex];
            if (cage == null || cage.Cells == null || cage.Cells.Length == 0) continue;

            foreach (int cellIndex in cage.Cells)
            {
                if (cellIndex < 0 || cellIndex >= 81 || cells[cellIndex / 9, cellIndex % 9] == null) continue;
                Color32 cageTint = cagePalette[cageColors[cageIndex]];
                cageTint.a = 48;
                cells[cellIndex / 9, cellIndex % 9].SetKillerCageTint(cageTint, true);
            }
        }

        RebuildKillerCageSumLabels();
        UpdateCageSumBadge();
    }

    private static void MarkNeighborColor(
        int neighborCell, int currentCage, int[] cageAt, int[] cageColors, bool[] usedColors)
    {
        if (neighborCell < 0) return;
        int neighborCage = cageAt[neighborCell];
        if (neighborCage >= 0 && neighborCage < currentCage && cageColors[neighborCage] >= 0)
            usedColors[cageColors[neighborCage]] = true;
    }

    private void OnSelectedCageCellChanged()
    {
        UpdateCageSumBadge();
    }

    private void UpdateCageSumBadge()
    {
        if (!killerVariantActive || viewModel == null || killerCages == null)
        {
            if (cageSumBadgeRect != null) cageSumBadgeRect.gameObject.SetActive(false);
            return;
        }

        int row = viewModel.SelectedRow.Value;
        int col = viewModel.SelectedCol.Value;
        if (row < 0 || row >= 9 || col < 0 || col >= 9)
        {
            SetKillerCageSumLabelsVisible(!viewModel.IsPencilMode.Value);
            if (cageSumBadgeRect != null) cageSumBadgeRect.gameObject.SetActive(false);
            return;
        }

        SetKillerCageSumLabelsVisible(false);
        int cageIndex = killerCageByCell[row * 9 + col];
        if (cageIndex < 0 || cageIndex >= killerCages.Length || killerCages[cageIndex] == null)
        {
            if (cageSumBadgeRect != null) cageSumBadgeRect.gameObject.SetActive(false);
            return;
        }

        EnsureCageSumBadge();
        if (cageSumBadgeRect == null || cageSumBadgeText == null) return;
        cageSumBadgeText.text = $"CAGE SUM  {killerCages[cageIndex].TargetSum}";
        PositionCageSumBadge();
        cageSumBadgeRect.gameObject.SetActive(true);
    }

    private void RebuildKillerCageSumLabels()
    {
        ClearKillerCageSumLabels();
        if (!killerVariantActive || killerCages == null || killerCages.Length == 0 || !cellsReady)
            return;

        EnsureCageSumBadge();
        if (gridPanelRect == null || cells[0, 0] == null) return;

        for (int cageIndex = 0; cageIndex < killerCages.Length; cageIndex++)
        {
            KillerCage cage = killerCages[cageIndex];
            if (cage == null || cage.Cells == null || cage.Cells.Length == 0) continue;

            int topLeftCell = -1;
            foreach (int cellIndex in cage.Cells)
            {
                if (cellIndex < 0 || cellIndex >= 81) continue;
                if (topLeftCell < 0 || cellIndex / 9 < topLeftCell / 9 ||
                    (cellIndex / 9 == topLeftCell / 9 && cellIndex % 9 < topLeftCell % 9))
                    topLeftCell = cellIndex;
            }
            if (topLeftCell < 0) continue;

            var labelObject = new GameObject(
                $"Killer Cage {cageIndex + 1} Sum", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.layer = gameObject.layer;
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.SetParent(gridPanelRect, false);
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.pivot = new Vector2(0f, 1f);

            TextMeshProUGUI labelText = labelObject.GetComponent<TextMeshProUGUI>();
            labelText.text = cage.TargetSum.ToString();
            labelText.font = cells[0, 0].NumberFont != null
                ? cells[0, 0].NumberFont
                : TMP_Settings.defaultFontAsset;
            labelText.fontSize = 35f;
            labelText.fontStyle = FontStyles.Bold;
            labelText.color = new Color32(255, 220, 150, 230);
            labelText.alignment = TextAlignmentOptions.TopLeft;
            labelText.raycastTarget = false;

            cageSumLabelRects.Add(labelRect);
            cageSumLabelTexts.Add(labelText);
        }

        PositionKillerCageSumLabels();
    }

    private void ClearKillerCageSumLabels()
    {
        foreach (RectTransform labelRect in cageSumLabelRects)
            if (labelRect != null) Destroy(labelRect.gameObject);
        cageSumLabelRects.Clear();
        cageSumLabelTexts.Clear();
    }

    private void SetKillerCageSumLabelsVisible(bool visible)
    {
        foreach (RectTransform labelRect in cageSumLabelRects)
            if (labelRect != null) labelRect.gameObject.SetActive(visible);
    }

    private void PositionKillerCageSumLabels()
    {
        if (gridPanelRect == null || killerCages == null) return;
        float cellWidth = gridPanelRect.rect.width / 9f;
        float cellHeight = gridPanelRect.rect.height / 9f;
        // Keep the requested 35-unit minimum while scaling cage sums with the
        // rendered cell size on larger boards and devices.
        float fontSize = Mathf.Max(35f, cellHeight * 0.4f);

        int labelIndex = 0;
        for (int cageIndex = 0; cageIndex < killerCages.Length && labelIndex < cageSumLabelRects.Count; cageIndex++)
        {
            KillerCage cage = killerCages[cageIndex];
            if (cage == null || cage.Cells == null || cage.Cells.Length == 0) continue;

            int topLeftCell = -1;
            foreach (int cellIndex in cage.Cells)
            {
                if (cellIndex < 0 || cellIndex >= 81) continue;
                if (topLeftCell < 0 || cellIndex / 9 < topLeftCell / 9 ||
                    (cellIndex / 9 == topLeftCell / 9 && cellIndex % 9 < topLeftCell % 9))
                    topLeftCell = cellIndex;
            }
            if (topLeftCell < 0) continue;

            int row = topLeftCell / 9;
            int col = topLeftCell % 9;
            RectTransform labelRect = cageSumLabelRects[labelIndex];
            labelRect.sizeDelta = new Vector2(
                Mathf.Max(cellWidth * 1.2f, fontSize * 1.5f),
                Mathf.Max(cellHeight * 0.75f, fontSize * 1.2f));
            labelRect.anchoredPosition = new Vector2(col * cellWidth + 2f, -row * cellHeight - 1f);
            if (labelIndex < cageSumLabelTexts.Count)
                cageSumLabelTexts[labelIndex].fontSize = fontSize;
            labelIndex++;
        }
    }

    private void EnsureCageSumBadge()
    {
        if (cageSumBadgeRect != null || !cellsReady || cells[0, 0] == null) return;
        gridPanelRect = cells[0, 0].transform.parent.parent as RectTransform;
        if (gridPanelRect == null || gridPanelRect.parent == null) return;

        var badge = new GameObject("Killer Cage Sum Readout", typeof(RectTransform), typeof(Image));
        cageSumBadgeRect = badge.GetComponent<RectTransform>();
        cageSumBadgeRect.SetParent(gridPanelRect.parent, false);
        badge.layer = gameObject.layer;
        cageSumBadgeRect.anchorMin = new Vector2(0.5f, 0.5f);
        cageSumBadgeRect.anchorMax = new Vector2(0.5f, 0.5f);
        cageSumBadgeRect.pivot = new Vector2(0.5f, 0.5f);
        cageSumBadgeRect.sizeDelta = new Vector2(190f, 34f);
        Image background = badge.GetComponent<Image>();
        background.color = new Color32(6, 17, 34, 225);
        background.raycastTarget = false;

        var textObject = new GameObject("Cage Sum Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(cageSumBadgeRect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 1f);
        textRect.offsetMax = new Vector2(-8f, -1f);
        textObject.layer = gameObject.layer;
        cageSumBadgeText = textObject.GetComponent<TextMeshProUGUI>();
        cageSumBadgeText.font = cells[0, 0].NumberFont != null
            ? cells[0, 0].NumberFont
            : TMP_Settings.defaultFontAsset;
        cageSumBadgeText.fontSize = 18f;
        cageSumBadgeText.fontStyle = FontStyles.Bold;
        cageSumBadgeText.color = new Color32(255, 220, 150, 255);
        cageSumBadgeText.alignment = TextAlignmentOptions.Center;
        cageSumBadgeText.raycastTarget = false;
        badge.SetActive(false);
    }

    private void PositionCageSumBadge()
    {
        if (cageSumBadgeRect == null || gridPanelRect == null) return;
        float cellSize = gridPanelRect.rect.width / 9f;
        float badgeHeight = Mathf.Clamp(cellSize * 0.86f, 28f, 42f);
        cageSumBadgeRect.sizeDelta = new Vector2(
            Mathf.Clamp(cellSize * 5.2f, 165f, 245f), badgeHeight);
        if (cageSumBadgeText != null)
            cageSumBadgeText.fontSize = badgeHeight * 0.5f;
        cageSumBadgeRect.anchoredPosition = gridPanelRect.anchoredPosition +
            new Vector2(0f, gridPanelRect.rect.height * 0.5f + cageSumBadgeRect.rect.height * 0.5f + 8f);
        PositionKillerCageSumLabels();
    }

    // ── Binding Handlers ──────────────────────────────────────────────────────
    private void OnSelectedRowOrColumnChanged(int _)
    {
        RefreshHighlights();
        OnSelectedCageCellChanged();
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

        UpdateCageSumBadge();
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
