using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(HorizontalLayoutGroup))]
public class TopBarSafeArea : MonoBehaviour
{
    [Header("Normal padding")]
    [SerializeField] private int baseLeft   = 24;
    [SerializeField] private int baseRight  = 24;
    [SerializeField] private int baseTop    = 16;
    [SerializeField] private int baseBottom = 16;

    private HorizontalLayoutGroup _layout;
    private RectTransform _rect;

    private Rect _lastSafeArea;
    private int _lastWidth;
    private int _lastHeight;

    private void Awake()
    {
        _layout = GetComponent<HorizontalLayoutGroup>();
        _rect   = GetComponent<RectTransform>();

        ApplySafeArea();
    }

    private void OnEnable()
    {
        ApplySafeArea();
    }

    private void Update()
    {
        if (_lastSafeArea != Screen.safeArea ||
            _lastWidth  != Screen.width ||
            _lastHeight != Screen.height)
        {
            ApplySafeArea();
        }
    }

    private void ApplySafeArea()
    {
        if (Screen.width <= 0 ||
            Screen.height <= 0)
        {
            return;
        }

        _lastSafeArea = Screen.safeArea;
        _lastWidth    = Screen.width;
        _lastHeight   = Screen.height;

        float canvasScale =
            1920f / Screen.height;

        float unsafeLeft =
            Screen.safeArea.xMin;

        float unsafeRight =
            Screen.width -
            Screen.safeArea.xMax;

        float unsafeTop =
            Screen.height -
            Screen.safeArea.yMax;

        _layout.padding.left =
            baseLeft +
            Mathf.RoundToInt(
                unsafeLeft *
                canvasScale);

        _layout.padding.right =
            baseRight +
            Mathf.RoundToInt(
                unsafeRight *
                canvasScale);

        _layout.padding.top =
            baseTop +
            Mathf.RoundToInt(
                unsafeTop *
                canvasScale);

        _layout.padding.bottom =
            baseBottom;

        LayoutRebuilder.MarkLayoutForRebuild(
            _rect);
    }
}