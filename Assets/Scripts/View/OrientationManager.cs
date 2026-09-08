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

    private RectTransform canvasRect;
    private Canvas canvas;
    private Vector2 lastScreenSize;
    private Vector2 lastCanvasSize;
    private Rect lastSafeArea;
    private bool layoutReady;

    void Start()
    {
        // Allow all rotations
        Screen.autorotateToPortrait           = true;
        Screen.autorotateToPortraitUpsideDown = true;
        Screen.autorotateToLandscapeLeft      = true;
        Screen.autorotateToLandscapeRight     = true;
        Screen.orientation = ScreenOrientation.AutoRotation;

        canvas = GetComponent<Canvas>();
        canvasRect = GetComponent<RectTransform>();

        // Build for the current orientation immediately
        RebuildForCurrentOrientation();
    }

    void LateUpdate()
    {
        // AutoRotation and Editor window resizing need not change the orientation
        // enum. Android may also deliver the new dimensions on a later frame.
        if (!layoutReady ||
            lastScreenSize != new Vector2(Screen.width, Screen.height) ||
            lastCanvasSize != canvasRect.rect.size ||
            lastSafeArea != Screen.safeArea)
        {
            RebuildForCurrentOrientation();
        }
    }

    /// <summary>
    /// Calculates the grid size for the current screen dimensions
    /// and triggers a full grid rebuild.
    /// </summary>
    public void RebuildForCurrentOrientation()
    {
        if (gridBuilder == null || canvas == null || canvasRect == null ||
            Screen.width <= 0 || Screen.height <= 0) return;

        Canvas.ForceUpdateCanvases();
        Vector2 size = canvasRect.rect.size;
        if (size.x <= 0f || size.y <= 0f || canvas.scaleFactor <= 0f) return;

        Rect safeArea = Screen.safeArea;
        float unsafeWidth = Screen.width - safeArea.width;
        float unsafeHeight = Screen.height - safeArea.height;
        float gridSize = CalculateGridSize(
            size.x - unsafeWidth / canvas.scaleFactor,
            size.y - unsafeHeight / canvas.scaleFactor);

        using (new Benchmark("Grid rebuild")){
            gridBuilder.Rebuild(gridSize);
        }

        lastScreenSize = new Vector2(Screen.width, Screen.height);
        lastCanvasSize = size;
        lastSafeArea = safeArea;
        layoutReady = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[M-01 Layout] screen={Screen.width}x{Screen.height}, " +
                  $"canvas={size}, safeArea={safeArea}, grid={gridSize}, " +
                  $"orientation={Screen.orientation}");
#endif
    }

    /// <summary>
    /// Converts screen pixels to canvas units and returns the largest
    /// square grid that fits between the top and bottom bars.
    /// </summary>
    private float CalculateGridSize(float canvasW, float canvasH)
    {
        float available = Mathf.Min(
            canvasW  - 2f * screenPadding,
            canvasH  - topBarHeight - bottomBarHeight - 2f * screenPadding
        );

        return Mathf.Max(available, 1f);
    }
}
