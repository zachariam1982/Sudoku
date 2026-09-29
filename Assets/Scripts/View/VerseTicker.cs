using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a clipped scripture ticker in the existing unused space around the
/// Sudoku board. The board itself is never resized or repositioned.
/// </summary>
[DisallowMultipleComponent]
public sealed class VerseTicker : MonoBehaviour
{
    private const string SampleVerse =
        "John 3:16 (WEB) — For God so loved the world, that he gave his one and only Son, " +
        "that whoever believes in him should not perish, but have eternal life.";

    [Header("Ticker")]
    [SerializeField] private string verseText = SampleVerse;
    [SerializeField] private float scrollSpeed = 28f;
    [SerializeField] private float portraitHeight = 44f;
    [SerializeField] private float landscapeWidth = 86f;
    [SerializeField] private float edgePadding = 8f;
    [SerializeField] private float textPadding = 10f;

    private Canvas _canvas;
    private RectTransform _canvasRect;
    private RectTransform _topBarRect;
    private RectTransform _gridRect;
    private RectTransform _viewportRect;
    private RectTransform _textRect;
    private Image _background;
    private TextMeshProUGUI _label;
    private bool _initialized;
    private bool _isLandscape;
    private float _nextLayoutCheck;
    private float _start;
    private float _end;

    /// <summary>
    /// Adds the ticker to the Canvas at runtime so no Sudoku cell or board
    /// layout object needs to be changed.
    /// </summary>
    public static void EnsureCreated(Canvas canvas)
    {
        if (canvas == null) return;

        VerseTicker ticker = canvas.GetComponent<VerseTicker>();
        if (ticker == null)
            ticker = canvas.gameObject.AddComponent<VerseTicker>();

        ticker.Initialize(canvas);
    }

    private void Initialize(Canvas canvas)
    {
        if (_initialized) return;

        _canvas = canvas;
        _canvasRect = canvas.transform as RectTransform;
        _topBarRect = FindRectTransform(canvas.transform, "TopBar");
        _gridRect = FindRectTransform(canvas.transform, "GridPanel");

        if (_canvasRect == null || _topBarRect == null || _gridRect == null)
        {
            Debug.LogWarning("[VerseTicker] Could not find Canvas, TopBar, or GridPanel.");
            enabled = false;
            return;
        }

        CreateVisuals();
        _initialized = true;
        RefreshLayout(true);
    }

    private void CreateVisuals()
    {
        GameObject viewport = new GameObject(
            "VerseTicker",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(RectMask2D));

        viewport.transform.SetParent(_canvas.transform, false);
        viewport.transform.SetAsLastSibling();

        _viewportRect = viewport.GetComponent<RectTransform>();
        _viewportRect.anchorMin = new Vector2(0.5f, 0.5f);
        _viewportRect.anchorMax = new Vector2(0.5f, 0.5f);
        _viewportRect.pivot = new Vector2(0.5f, 0.5f);

        _background = viewport.GetComponent<Image>();
        _background.color = new Color(0.055f, 0.075f, 0.11f, 0.92f);
        _background.raycastTarget = false;

        GameObject textObject = new GameObject(
            "VerseText",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));

        textObject.transform.SetParent(viewport.transform, false);
        _textRect = textObject.GetComponent<RectTransform>();
        _label = textObject.GetComponent<TextMeshProUGUI>();
        _label.text = verseText;
        _label.fontSize = 19f;
        _label.color = new Color(1f, 0.91f, 0.68f, 1f);
        _label.alignment = TextAlignmentOptions.Center;
        _label.raycastTarget = false;
        _label.overflowMode = TextOverflowModes.Overflow;

        // Reuse the project's existing TMP font so the ticker matches the UI.
        TextMeshProUGUI[] existingLabels =
            _canvas.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < existingLabels.Length; i++)
        {
            if (existingLabels[i] == _label || existingLabels[i].font == null) continue;
            _label.font = existingLabels[i].font;
            break;
        }
    }

    private void Update()
    {
        if (!_initialized) return;

        if (Time.unscaledTime >= _nextLayoutCheck)
        {
            _nextLayoutCheck = Time.unscaledTime + 0.2f;
            RefreshLayout(false);
        }

        if (!_viewportRect.gameObject.activeSelf) return;

        Vector2 position = _textRect.anchoredPosition;
        if (_isLandscape)
        {
            position.y += scrollSpeed * Time.unscaledDeltaTime;
            if (position.y >= _end)
                position.y = _start;
        }
        else
        {
            position.x -= scrollSpeed * Time.unscaledDeltaTime;
            if (position.x <= _end)
                position.x = _start;
        }

        _textRect.anchoredPosition = position;
    }

    private void RefreshLayout(bool force)
    {
        bool landscape = Screen.width > Screen.height;
        Bounds gridBounds = GetCanvasBounds(_gridRect);
        Bounds topBarBounds = GetCanvasBounds(_topBarRect);
        Rect canvasBounds = _canvasRect.rect;

        if (!force &&
            landscape == _isLandscape &&
            Mathf.Approximately(_viewportRect.sizeDelta.x, landscape
                ? Mathf.Min(landscapeWidth, gridBounds.min.x - canvasBounds.xMin - 2f * edgePadding)
                : canvasBounds.width - 2f * edgePadding) &&
            Mathf.Approximately(_viewportRect.sizeDelta.y, landscape
                ? topBarBounds.min.y - canvasBounds.yMin - 2f * edgePadding
                : Mathf.Min(portraitHeight, topBarBounds.min.y - gridBounds.max.y - 2f * edgePadding)))
        {
            return;
        }

        _isLandscape = landscape;
        _label.text = verseText;
        _label.enableWordWrapping = landscape;

        if (landscape)
        {
            float availableWidth = gridBounds.min.x - canvasBounds.xMin - 2f * edgePadding;
            float width = Mathf.Min(landscapeWidth, availableWidth);
            float height = topBarBounds.min.y - canvasBounds.yMin - 2f * edgePadding;

            if (width < 32f || height < 48f)
            {
                _viewportRect.gameObject.SetActive(false);
                return;
            }

            _viewportRect.gameObject.SetActive(true);
            _viewportRect.sizeDelta = new Vector2(width, height);
            _viewportRect.anchoredPosition = new Vector2(
                canvasBounds.xMin + edgePadding + width * 0.5f,
                canvasBounds.yMin + edgePadding + height * 0.5f);

            _textRect.anchorMin = new Vector2(0.5f, 0.5f);
            _textRect.anchorMax = new Vector2(0.5f, 0.5f);
            _textRect.pivot = new Vector2(0.5f, 0.5f);
            _textRect.sizeDelta = new Vector2(width - 2f * textPadding, 1000f);
            _label.alignment = TextAlignmentOptions.Center;
            _label.ForceMeshUpdate();
            float textHeight = Mathf.Max(_label.preferredHeight, height);
            _textRect.sizeDelta = new Vector2(width - 2f * textPadding, textHeight);
            _start = -height * 0.5f - textHeight * 0.5f;
            _end = height * 0.5f + textHeight * 0.5f;
            _textRect.anchoredPosition = new Vector2(0f, _start);
        }
        else
        {
            float gap = topBarBounds.min.y - gridBounds.max.y;
            float width = canvasBounds.width - 2f * edgePadding;
            float height = Mathf.Min(portraitHeight, gap - 2f * edgePadding);

            if (height < 24f || width < 80f)
            {
                _viewportRect.gameObject.SetActive(false);
                return;
            }

            _viewportRect.gameObject.SetActive(true);
            _viewportRect.sizeDelta = new Vector2(width, height);
            _viewportRect.anchoredPosition = new Vector2(
                canvasBounds.center.x,
                (topBarBounds.min.y + gridBounds.max.y) * 0.5f);

            _textRect.anchorMin = new Vector2(0.5f, 0.5f);
            _textRect.anchorMax = new Vector2(0.5f, 0.5f);
            _textRect.pivot = new Vector2(0.5f, 0.5f);
            _label.alignment = TextAlignmentOptions.Midline;
            _label.enableWordWrapping = false;
            _textRect.sizeDelta = new Vector2(4000f, height - 2f * textPadding);
            _label.ForceMeshUpdate();
            float textWidth = Mathf.Max(_label.preferredWidth, width);
            _textRect.sizeDelta = new Vector2(textWidth, height - 2f * textPadding);
            _start = width * 0.5f + textWidth * 0.5f;
            _end = -width * 0.5f - textWidth * 0.5f;
            _textRect.anchoredPosition = new Vector2(_start, 0f);
        }
    }

    private Bounds GetCanvasBounds(RectTransform target)
    {
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        Vector3 first = _canvasRect.InverseTransformPoint(corners[0]);
        Bounds bounds = new Bounds(first, Vector3.zero);

        for (int i = 1; i < corners.Length; i++)
            bounds.Encapsulate(_canvasRect.InverseTransformPoint(corners[i]));

        return bounds;
    }

    private static RectTransform FindRectTransform(Transform root, string objectName)
    {
        RectTransform[] transforms = root.GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == objectName)
                return transforms[i];
        }

        return null;
    }
}
