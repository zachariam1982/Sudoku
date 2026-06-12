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

    
    private readonly RectTransform[] _boxRects  = new RectTransform[9];
    private readonly RectTransform[] _cellRects = new RectTransform[81];  // cached to avoid repeated transform bridge calls
    private SudokuCell[] _allCells = new SudokuCell[81];
    public void Awake()
    {
        InitializeGrid();
    }
    public void Rebuild(float gridSize)
    {
        if (NumberPicker.Instance != null) NumberPicker.Instance.Hide();
 
        // Reposition GridPanel
        gridPanel.sizeDelta        = new Vector2(gridSize, gridSize);
        gridPanel.anchoredPosition = new Vector2(0f, (bottomBarHeight - topBarHeight) / 2f);
 
        float boxSize  = (gridSize - 2f * boxGap) / 3f;
        float cellSize = (boxSize  - 2f * boxPadding - 2f * cellGap) / 3f;
 
        // Update existing objects — no Instantiate, no Destroy, no GC pressure
        for (int b = 0; b < 9; b++)
        {
            int boxRow = b / 3;
            int boxCol = b % 3;
 
            RectTransform boxRT = _boxRects[b];
            boxRT.sizeDelta        = new Vector2(boxSize, boxSize);
            boxRT.anchoredPosition = new Vector2(
                (boxCol - 1) * (boxSize + boxGap),
                (1 - boxRow) * (boxSize + boxGap)
            );
 
            for (int c = 0; c < 9; c++)
            {
                int cellRow = c / 3;
                int cellCol = c % 3;
 
                int globalIndex = (boxRow * 3 + cellRow) * 9 + (boxCol * 3 + cellCol);
 
                RectTransform cellRT = _cellRects[globalIndex];
                cellRT.sizeDelta        = new Vector2(cellSize, cellSize);
                cellRT.anchoredPosition = new Vector2(
                    (cellCol - 1) * (cellSize + cellGap),
                    (1 - cellRow) * (cellSize + cellGap)
                );
                _allCells[globalIndex].ResizePencilGrid(cellSize);
            }
        }
 
        if (NumberPicker.Instance != null)
            NumberPicker.Instance.UpdateGridBounds(gridSize, gridSize, boxGap, boxPadding, cellGap);
    }
    private void InitializeGrid()
    {
        // Create the 9 boxes and 81 cells exactly once, cache every reference we'll need later
        for (int b = 0; b < 9; b++)
        {
            int boxRow = b / 3;
            int boxCol = b % 3;
 
            GameObject boxGO = Instantiate(boxPrefab, gridPanel);
            _boxRects[b] = boxGO.GetComponent<RectTransform>();
 
            for (int c = 0; c < 9; c++)
            {
                int cellRow = c / 3;
                int cellCol = c % 3;
                int index   = (boxRow * 3 + cellRow) * 9 + (boxCol * 3 + cellCol);
 
                GameObject cellGO = Instantiate(cellPrefab, boxGO.transform);
                _allCells[index]  = cellGO.GetComponent<SudokuCell>();
                _cellRects[index] = (RectTransform)cellGO.transform; // cast once, reused every Rebuild
            }
        }
 
        // Wire cells into SudokuGrid once — the array reference never changes
        sudokuGrid.SetCells(_allCells);
    }

}