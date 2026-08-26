using UnityEngine;

/// <summary>
/// Allows free rotation. Whenever the device orientation changes,
/// it recalculates the grid size for the new orientation and tells
/// GridBuilder to rebuild the board.
/// 
/// Attach this to the Canvas alongside GridBuilder.
/// </summary>
public class OrientationManager : MonoBehaviour
{
    [Header("Bar heights in Canvas reference units")]
    [SerializeField] private float topBarHeight    = 160f;
    [SerializeField] private float bottomBarHeight = 160f;
    [SerializeField] private float screenPadding   = 8f;

    [Header("References")]
    [SerializeField] private GridBuilder gridBuilder;

    // Track last known orientation so we only rebuild when it actually changes
    private ScreenOrientation lastOrientation;

    // Canvas reference resolution (must match Canvas Scaler settings)
    private const float RefW = 1080f;
    private const float RefH = 1920f;
    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;

    void Start()
    {
        // Allow all rotations
        Screen.autorotateToPortrait           = true;
        Screen.autorotateToPortraitUpsideDown = true;
        Screen.autorotateToLandscapeLeft      = true;
        Screen.autorotateToLandscapeRight     = true;
        Screen.orientation = ScreenOrientation.AutoRotation;

        lastOrientation = Screen.orientation;

        // Build for the current orientation immediately
        RebuildForCurrentOrientation();
    }

    void Update()
    {
        bool orientationChanged = Screen.orientation != lastOrientation;
        bool sizeChanged = Screen.width != lastScreenWidth || Screen.height != lastScreenHeight;

        if (!orientationChanged && !sizeChanged) return;

        lastOrientation = Screen.orientation;

        RebuildForCurrentOrientation();
    }

    /// <summary>
    /// Calculates the grid size for the current screen dimensions
    /// and triggers a full grid rebuild.
    /// </summary>
    public void RebuildForCurrentOrientation()
    {
        int screenW = Screen.width;
        int screenH = Screen.height;

        if (screenW <= 0 || screenH <= 0) return;

        lastScreenWidth = screenW;
        lastScreenHeight = screenH;

        float gridSize = CalculateGridSize(screenW, screenH);

        using (new Benchmark("Grid rebuild")){
            gridBuilder.Rebuild(gridSize);
        }
    }

    /// <summary>
    /// Converts screen pixels to canvas units and returns the largest
    /// square grid that fits between the top and bottom bars.
    /// </summary>
    private float CalculateGridSize(int screenW, int screenH)
    {
        // Scale factor: how many canvas units per pixel
        // Canvas Scaler uses "Match Width Or Height = 1" (match height)
        float scale = RefH / screenH;

        float canvasW = screenW * scale;
        float canvasH = RefH;

        float available = Mathf.Min(
            canvasW  - 2f * screenPadding,
            canvasH  - topBarHeight - bottomBarHeight - 2f * screenPadding
        );

        return Mathf.Max(available, 100f); // never go below 100 units
    }
}