using UnityEngine;

/// <summary>
/// Spawns and rebuilds the 9x9 Sudoku grid.
/// Called by OrientationManager on startup and on every rotation.
/// Puzzle state is preserved automatically via SudokuGrid.SaveCurrentState().
/// </summary>
public class GridBuilder : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject boxPrefab;
    [SerializeField] private GameObject cellPrefab;

    [Header("Scene References")]
    [SerializeField] private RectTransform gridPanel;
    [SerializeField] private SudokuGrid    sudokuGrid;

    [Header("Bar & Padding (canvas units — match OrientationManager)")]
    [SerializeField] private float topBarHeight    = 160f;
    [SerializeField] private float bottomBarHeight = 160f;

    [Header("Internal Cell Appearance")]
    [SerializeField] private float cellGap    = 3f;
    [SerializeField] private float boxGap     = 10f;
    [SerializeField] private float boxPadding = 8f;

    /// <summary>
    /// Destroys the existing grid and spawns a fresh one sized to gridSize.
    /// Puzzle state is saved first and restored automatically by SudokuGrid.SetCells().
    /// </summary>
    public void Rebuild(float gridSize)
    {
        // ── Save puzzle state before destroying cells ─────────────────────
        sudokuGrid.SaveCurrentState();

        // ── Close number picker if open ───────────────────────────────────
        if (NumberPicker.Instance != null)
            NumberPicker.Instance.Hide();

        // ── Destroy all existing box children ────────────────────────────
        foreach (Transform child in gridPanel)
            Destroy(child.gameObject);

        // ── Resize and reposition GridPanel ──────────────────────────────
        gridPanel.sizeDelta = new Vector2(gridSize, gridSize);
        gridPanel.anchoredPosition = new Vector2(0f, (bottomBarHeight - topBarHeight) / 2f);

        // ── Derive box and cell sizes ─────────────────────────────────────
        float boxSize  = (gridSize - 2f * boxGap)  / 3f;
        float cellSize = (boxSize  - 2f * boxPadding - 2f * cellGap) / 3f;

        Debug.Log($"[GridBuilder] gridSize={gridSize:F1} boxSize={boxSize:F1} cellSize={cellSize:F1}");

        // ── Spawn boxes and cells ─────────────────────────────────────────
        SudokuCell[] allCells = new SudokuCell[81];

        for (int boxRow = 0; boxRow < 3; boxRow++)
        {
            for (int boxCol = 0; boxCol < 3; boxCol++)
            {
                GameObject    boxGO = Instantiate(boxPrefab, gridPanel);
                boxGO.name = $"Box_{boxRow}_{boxCol}";

                RectTransform boxRT = boxGO.GetComponent<RectTransform>();
                boxRT.sizeDelta = new Vector2(boxSize, boxSize);
                boxRT.anchoredPosition = new Vector2(
                    (boxCol - 1) * (boxSize + boxGap),
                    (1 - boxRow) * (boxSize + boxGap)
                );

                for (int cellRow = 0; cellRow < 3; cellRow++)
                {
                    for (int cellCol = 0; cellCol < 3; cellCol++)
                    {
                        GameObject    cellGO = Instantiate(cellPrefab, boxGO.transform);
                        cellGO.name = $"Cell_{cellRow}_{cellCol}";

                        RectTransform cellRT = cellGO.GetComponent<RectTransform>();
                        cellRT.sizeDelta = new Vector2(cellSize, cellSize);
                        cellRT.anchoredPosition = new Vector2(
                            (cellCol - 1) * (cellSize + cellGap),
                            (1 - cellRow) * (cellSize + cellGap)
                        );

                        int index = (boxRow * 3 + cellRow) * 9 + (boxCol * 3 + cellCol);
                        allCells[index] = cellGO.GetComponent<SudokuCell>();
                    }
                }
            }
        }

        // SetCells() will auto-restore the saved puzzle state
        sudokuGrid.SetCells(allCells);

        // ── Update NumberPicker bounds ────────────────────────────────────
        if (NumberPicker.Instance != null)
            NumberPicker.Instance.UpdateGridBounds(gridSize, gridSize, boxGap, boxPadding, cellGap);
    }
}