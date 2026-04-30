using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Layout only — spawns and rebuilds the 9x9 grid on startup and rotation.
/// Gets the shared ViewModel from GameContext and passes it to each cell via SudokuGrid.
/// Contains zero game logic.
/// </summary>
public class GridBuilder : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject boxPrefab;
    [SerializeField] private GameObject cellPrefab;

    [Header("Scene References")]
    [SerializeField] private RectTransform gridPanel;
    [SerializeField] private SudokuGrid    sudokuGrid;
    [SerializeField] private GameContext   gameContext;

    [Header("Bar & Padding (canvas units — match OrientationManager)")]
    [SerializeField] private float topBarHeight    = 160f;
    [SerializeField] private float bottomBarHeight = 160f;

    [Header("Internal Cell Appearance")]
    [SerializeField] private float cellGap    = 3f;
    [SerializeField] private float boxGap     = 10f;
    [SerializeField] private float boxPadding = 8f;

    
    // Pre-cache these to avoid repeated searching
    private List<RectTransform> _cachedBoxes = new List<RectTransform>();
    private SudokuCell[] _allCells = new SudokuCell[81];

    public void Rebuild(float gridSize)
    {
        if (NumberPicker.Instance != null) NumberPicker.Instance.Hide();

        // 1. Initial Setup: Create objects ONLY if they don't exist
        if (_cachedBoxes.Count == 0)
        {
            InitializeGrid();
        }

        // 2. Reposition GridPanel
        gridPanel.sizeDelta = new Vector2(gridSize, gridSize);
        gridPanel.anchoredPosition = new Vector2(0f, (bottomBarHeight - topBarHeight) / 2f);

        float boxSize  = (gridSize - 2f * boxGap) / 3f;
        float cellSize = (boxSize - 2f * boxPadding - 2f * cellGap) / 3f;

        // 3. Update existing objects (No Instantiate, No Destroy)
        for (int b = 0; b < 9; b++)
        {
            int boxRow = b / 3;
            int boxCol = b % 3;

            RectTransform boxRT = _cachedBoxes[b];
            boxRT.sizeDelta = new Vector2(boxSize, boxSize);
            boxRT.anchoredPosition = new Vector2(
                (boxCol - 1) * (boxSize + boxGap),
                (1 - boxRow) * (boxSize + boxGap)
            );

            // Update the 9 cells inside this box
            for (int c = 0; c < 9; c++)
            {
                int cellRow = c / 3;
                int cellCol = c % 3;
                
                int globalIndex = (boxRow * 3 + cellRow) * 9 + (boxCol * 3 + cellCol);
                SudokuCell cell = _allCells[globalIndex];
                
                RectTransform cellRT = (RectTransform)cell.transform;
                cellRT.sizeDelta = new Vector2(cellSize, cellSize);
                cellRT.anchoredPosition = new Vector2(
                    (cellCol - 1) * (cellSize + cellGap),
                    (1 - cellRow) * (cellSize + cellGap)
                );
                cell.ResizePencilGrid(cellSize);

            }
        }

        sudokuGrid.SetCells(_allCells);

        if (NumberPicker.Instance != null)
            NumberPicker.Instance.UpdateGridBounds(gridSize, gridSize, boxGap, boxPadding, cellGap);
    }

    private void InitializeGrid()
    {
        // Create the 9 boxes and 81 cells once and cache them
        for (int b = 0; b < 9; b++)
        {
            GameObject boxGO = Instantiate(boxPrefab, gridPanel);
            _cachedBoxes.Add(boxGO.GetComponent<RectTransform>());

            for (int c = 0; c < 9; c++)
            {
                GameObject cellGO = Instantiate(cellPrefab, boxGO.transform);
                int boxRow = b / 3;
                int boxCol = b % 3;
                int cellRow = c / 3;
                int cellCol = c % 3;
                
                int index = (boxRow * 3 + cellRow) * 9 + (boxCol * 3 + cellCol);
                _allCells[index] = cellGO.GetComponent<SudokuCell>();
            }
        }
    }

}