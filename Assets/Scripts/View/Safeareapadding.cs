using UnityEngine;

/// <summary>
/// Attach this to any GameObject that needs to be nudged away from
/// the notch / home indicator / rounded corners at runtime.
///
/// Instead of touching anchors or offsets on the parent bar,
/// this script shifts THIS object's anchoredPosition by the safe
/// area inset on the edge you care about.
///
/// Usage examples:
///   - Pause button (top-right)  -> applyTop = true,    applyRight = true
///   - Timer label  (top-center) -> applyTop = true
///   - Hearts       (top-left)   -> applyTop = true,    applyLeft  = true
///   - Erase button (bottom)     -> applyBottom = true
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaPadding : MonoBehaviour
{
    [Header("Which edges should this object be nudged away from?")]
    [SerializeField] private bool applyTop    = false;
    [SerializeField] private bool applyBottom = false;
    [SerializeField] private bool applyLeft   = false;
    [SerializeField] private bool applyRight  = false;

    private RectTransform _rt;
    private Vector2       _originalPosition;   // position set in the editor

    // Cache to avoid recalculating every frame unnecessarily
    private Rect    _lastSafeArea   = Rect.zero;
    private Vector2 _lastScreenSize = Vector2.zero;

    void Awake()
    {
        _rt               = GetComponent<RectTransform>();
        _originalPosition = _rt.anchoredPosition;
    }

    void OnEnable()
    {
        // Always re-apply when the object is re-enabled
        // (e.g. HUD toggling on/off between states)
        Apply();
    }

    void Update()
    {
        // Re-apply only when orientation or resolution changes
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        if (_lastSafeArea != Screen.safeArea || _lastScreenSize != screenSize)
            Apply();
    }

    private void Apply()
    {
        if (Screen.width == 0 || Screen.height == 0) return;

        _lastSafeArea   = Screen.safeArea;
        _lastScreenSize = new Vector2(Screen.width, Screen.height);

        // How many screen pixels are unsafe on each edge
        float insetLeft   = _lastSafeArea.xMin;
        float insetBottom = _lastSafeArea.yMin;
        float insetRight  = Screen.width  - _lastSafeArea.xMax;
        float insetTop    = Screen.height - _lastSafeArea.yMax;

        // Convert screen pixels to canvas units
        // Canvas Scaler: Match Height, reference resolution 1920
        float scale = 1920f / Screen.height;

        float nudgeX = 0f;
        float nudgeY = 0f;

        // Horizontal nudge — left wins if both are set (unlikely)
        if (applyLeft)  nudgeX =  insetLeft  * scale;
        if (applyRight) nudgeX = -insetRight  * scale;

        // Vertical nudge
        if (applyBottom) nudgeY =  insetBottom * scale;
        if (applyTop)    nudgeY = -insetTop    * scale;

        _rt.anchoredPosition = _originalPosition + new Vector2(nudgeX, nudgeY);
    }

    // Lets you preview the result in the editor without entering Play mode
#if UNITY_EDITOR
    void OnValidate()
    {
        if (_rt == null) _rt = GetComponent<RectTransform>();
        if (!Application.isPlaying) return;
        _originalPosition = _rt.anchoredPosition;
        Apply();
    }
#endif
}