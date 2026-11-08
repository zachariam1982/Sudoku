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
    private bool hyperVariantActive;
    private bool killerVariantActive;
    private bool thermoVariantActive;
    private readonly ThermometerGraphic[] thermometerGraphics = new ThermometerGraphic[81];
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
        vm.Thermometers.OnChanged += OnThermometersChanged;
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
            RefreshThermometerLayout();
            UpdateCageSumBadge();
        }
    }

    public void SetThermoVariantActive(bool active)
    {
        thermoVariantActive = active;
        RefreshThermometerLayout();
    }

    private void OnThermometersChanged(Thermometer[] paths) => RefreshThermometerLayout();

    public void RefreshThermometerLayout()
    {
        if (!cellsReady) return;
        bool visible = thermoVariantActive && viewModel != null && viewModel.Thermometers.Value.Length > 0;
        if (!visible)
        {
            foreach (var graphic in thermometerGraphics)
                if (graphic != null) graphic.gameObject.SetActive(false);
            return;
        }
        var paths = viewModel.Thermometers.Value;
        var occupied = new bool[81];
        foreach (var path in paths)
            if (path?.Cells != null)
                foreach (int index in path.Cells)
                    if (index >= 0 && index < 81) occupied[index] = true;
        for (int index = 0; index < 81; index++)
        {
            var graphic = thermometerGraphics[index];
            if (!occupied[index])
            {
                if (graphic != null) graphic.gameObject.SetActive(false);
                continue;
            }
            Transform cell = cells[index / 9, index % 9].transform;
            if (graphic == null)
            {
                var go = new GameObject("Thermometer Underlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(ThermometerGraphic));
                go.transform.SetParent(cell, false);
                graphic = go.GetComponent<ThermometerGraphic>();
                thermometerGraphics[index] = graphic;
                var rect = graphic.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                graphic.raycastTarget = false;
            }
            // Draw after the cell tint but before numbers, pencil marks and glow.
            Transform overlay = cell.Find("CellStateOverlay");
            graphic.transform.SetAsFirstSibling();
            if (overlay != null) graphic.transform.SetSiblingIndex(overlay.GetSiblingIndex() + 1);
            graphic.gameObject.SetActive(true);
            graphic.SetPaths(paths, cells, index);
        }
    }

    private bool irregularVariantActive;
    public void SetIrregularVariantActive(bool active)
    {
        irregularVariantActive = active;
        RefreshHighlights();
    }

    private int GetIrregularRegion(int row, int col) =>
        viewModel != null ? viewModel.GetIrregularRegion(row, col) : row / 3 * 3 + col / 3;

    private void RefreshIrregularCell(SudokuCell cell, int row, int col)
    {
        var frame = cell.transform.parent.GetComponent<UnityEngine.UI.Graphic>();
        if (frame != null) frame.enabled = !irregularVariantActive;
        int region = GetIrregularRegion(row, col);
        bool[] edges = {
            col == 0 || region != GetIrregularRegion(row, col - 1),
            col == 8 || region != GetIrregularRegion(row, col + 1),
            row == 0 || region != GetIrregularRegion(row - 1, col),
            row == 8 || region != GetIrregularRegion(row + 1, col)
        };
        for (int edge = 0; edge < 4; edge++)
        {
            string name = "Irregular Region Edge " + edge;
            var existing = cell.transform.Find(name);
            if (existing == null && !irregularVariantActive) continue;
            if (existing == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
                go.transform.SetParent(cell.transform, false);
                var image = go.GetComponent<UnityEngine.UI.Image>();
                image.color = new Color(1f, 0.75f, 0.2f, 0.9f);
                image.raycastTarget = false;
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = edge < 2 ? new Vector2(edge, 0) : new Vector2(0, edge == 2 ? 1 : 0);
                rect.anchorMax = edge < 2 ? new Vector2(edge, 1) : new Vector2(1, edge == 2 ? 1 : 0);
                rect.pivot = edge < 2 ? new Vector2(edge, 0.5f) : new Vector2(0.5f, edge == 2 ? 1 : 0);
                rect.sizeDelta = edge < 2 ? new Vector2(2, 0) : new Vector2(0, 2);
                rect.anchoredPosition = Vector2.zero;
                existing = go.transform;
            }
            existing.gameObject.SetActive(irregularVariantActive && edges[edge]);
        }
    }

    public void SetHyperVariantActive(bool active)
    {
        hyperVariantActive = active;
        RefreshHighlights();
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
        viewModel.Thermometers.OnChanged -= OnThermometersChanged;
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
        foreach (var graphic in thermometerGraphics)
                if (graphic != null) graphic.gameObject.SetActive(false);
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
            new Color32(0, 114, 178, 255),    // blue
            new Color32(230, 159, 0, 255),    // orange
            new Color32(0, 158, 115, 255),    // green
            new Color32(204, 121, 167, 255),  // reddish purple
            new Color32(213, 94, 0, 255),     // vermillion
            new Color32(86, 180, 233, 255),   // sky blue
            new Color32(240, 228, 66, 255),   // yellow
            new Color32(122, 64, 180, 255),   // violet
            new Color32(0, 190, 210, 255),    // cyan
            new Color32(255, 70, 110, 255),   // rose
            new Color32(150, 205, 0, 255),    // lime
            new Color32(150, 90, 45, 255)     // brown
        };
        int[][] cageNeighbors = BuildCageNeighbors(cageAt, killerCages.Length);
        int[] cageColors = AssignCageColors(cageNeighbors, cagePalette);

        for (int cageIndex = 0; cageIndex < killerCages.Length; cageIndex++)
        {
            KillerCage cage = killerCages[cageIndex];
            if (cage == null || cage.Cells == null || cage.Cells.Length == 0) continue;

            foreach (int cellIndex in cage.Cells)
            {
                if (cellIndex < 0 || cellIndex >= 81 || cells[cellIndex / 9, cellIndex % 9] == null) continue;
                Color32 cageTint = cagePalette[cageColors[cageIndex]];
                cageTint.a = 38;
                cells[cellIndex / 9, cellIndex % 9].SetKillerCageTint(cageTint, true);
            }
        }

        RebuildKillerCageSumLabels();
        UpdateCageSumBadge();
    }

    private static int[][] BuildCageNeighbors(int[] cageAt, int cageCount)
    {
        var neighbors = new HashSet<int>[cageCount];
        for (int i = 0; i < cageCount; i++) neighbors[i] = new HashSet<int>();

        for (int cell = 0; cell < cageAt.Length; cell++)
        {
            int row = cell / 9;
            int col = cell % 9;
            int cage = cageAt[cell];
            if (cage < 0) continue;
            if (col < 8) AddCageNeighbor(cage, cageAt[cell + 1], neighbors);
            if (row < 8) AddCageNeighbor(cage, cageAt[cell + 9], neighbors);
        }

        var result = new int[cageCount][];
        for (int i = 0; i < cageCount; i++)
        {
            var list = new List<int>(neighbors[i]);
            result[i] = list.ToArray();
        }
        return result;
    }

    private static void AddCageNeighbor(int cage, int neighbor, HashSet<int>[] neighbors)
    {
        if (neighbor < 0 || neighbor == cage) return;
        neighbors[cage].Add(neighbor);
        neighbors[neighbor].Add(cage);
    }

    private static int[] AssignCageColors(int[][] cageNeighbors, Color32[] palette)
    {
        var order = new List<int>(cageNeighbors.Length);
        for (int i = 0; i < cageNeighbors.Length; i++) order.Add(i);
        order.Sort((left, right) => cageNeighbors[right].Length.CompareTo(cageNeighbors[left].Length));

        var cageColors = new int[cageNeighbors.Length];
        var colorUsage = new int[palette.Length];
        for (int i = 0; i < cageColors.Length; i++) cageColors[i] = -1;

        foreach (int cage in order)
        {
            var forbidden = new bool[palette.Length];
            bool hasColoredNeighbor = false;
            foreach (int neighbor in cageNeighbors[cage])
            {
                int neighborColor = cageColors[neighbor];
                if (neighborColor < 0) continue;
                forbidden[neighborColor] = true;
                hasColoredNeighbor = true;
            }

            int bestColor = -1;
            int bestDistance = -1;
            int bestUsage = int.MaxValue;
            for (int candidate = 0; candidate < palette.Length; candidate++)
            {
                if (hasColoredNeighbor && forbidden[candidate]) continue;

                int nearestDistance = int.MaxValue;
                foreach (int neighbor in cageNeighbors[cage])
                {
                    int neighborColor = cageColors[neighbor];
                    if (neighborColor < 0) continue;
                    int distance = GetColorDistanceSquared(palette[candidate], palette[neighborColor]);
                    if (distance < nearestDistance) nearestDistance = distance;
                }

                if (nearestDistance > bestDistance ||
                    (nearestDistance == bestDistance && colorUsage[candidate] < bestUsage))
                {
                    bestColor = candidate;
                    bestDistance = nearestDistance;
                    bestUsage = colorUsage[candidate];
                }
            }

            // If an unusually large cage touches every palette color, allow
            // reuse but still choose the most visually separated available hue.
            if (bestColor < 0)
            {
                for (int candidate = 0; candidate < palette.Length; candidate++)
                {
                    int nearestDistance = int.MaxValue;
                    foreach (int neighbor in cageNeighbors[cage])
                    {
                        int neighborColor = cageColors[neighbor];
                        if (neighborColor < 0) continue;
                        int distance = GetColorDistanceSquared(palette[candidate], palette[neighborColor]);
                        if (distance < nearestDistance) nearestDistance = distance;
                    }
                    if (nearestDistance > bestDistance ||
                        (nearestDistance == bestDistance && colorUsage[candidate] < bestUsage))
                    {
                        bestColor = candidate;
                        bestDistance = nearestDistance;
                        bestUsage = colorUsage[candidate];
                    }
                }
            }

            cageColors[cage] = bestColor;
            colorUsage[bestColor]++;
        }

        return cageColors;
    }

    private static int GetColorDistanceSquared(Color32 a, Color32 b)
    {
        int red = a.r - b.r;
        int green = a.g - b.g;
        int blue = a.b - b.b;
        return red * red + green * green + blue * blue;
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
        // Scale cage sums to each rendered cell so portrait boards stay
        // legible without the labels overwhelming the smaller cells.
        float fontSize = Mathf.Clamp(cellHeight * 0.32f, 12f, 35f);

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
                Mathf.Max(cellWidth * 0.7f, fontSize * 1.25f),
                Mathf.Max(cellHeight * 0.38f, fontSize * 1.05f));
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
                RefreshIrregularCell(cell, row, col);
                cell.SetHighlight(false);
                cell.SetRelatedHighlight(false);
                cell.SetDigitMatchHighlight(false);
                cell.SetDiagonalHighlight(isDiagonal);
                cell.SetHyperHighlight(hyperVariantActive && HyperSudokuRules.GetHyperRegion(row, col) >= 0);
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
        RefreshThermometerLayout();
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
