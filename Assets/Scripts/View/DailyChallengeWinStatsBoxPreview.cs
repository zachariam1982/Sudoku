using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor preview of the Daily Challenge win screen statistics box.
/// The panel is created as child UI objects so its border, separators and text
/// can be inspected and adjusted directly in the Hierarchy.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class DailyChallengeWinStatsBoxPreview : MonoBehaviour
{
    private const string VisualRootName = "Win Stats Box Visuals";

    private RectTransform _root;
    private Vector2 _lastParentSize = new Vector2(-1f, -1f);
    private static Sprite _whiteSprite;

    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            // This scene object is a layout preview. Keep it out of live games
            // until the win-state flow is wired to a production panel.
            gameObject.SetActive(false);
            return;
        }

        EnsureVisuals();
        RefreshLayout();
    }

    private void Update()
    {
        if (!Application.isPlaying)
            RefreshLayout();
    }

    private void EnsureVisuals()
    {
        _root = transform as RectTransform;
        if (_root == null)
            return;

        Transform existing = transform.Find(VisualRootName);
        if (existing != null)
            return;

        GameObject visuals = CreateRectObject(VisualRootName, transform);
        RectTransform visualRoot = visuals.GetComponent<RectTransform>();
        Stretch(visualRoot);

        Image fill = AddImage(visualRoot, "Panel Fill", new Color32(8, 17, 42, 242));
        Stretch(fill.rectTransform);

        AddBorder(visualRoot, "Top Gold Border", true, false);
        AddBorder(visualRoot, "Bottom Gold Border", true, false);
        AddBorder(visualRoot, "Left Gold Border", false, false);
        AddBorder(visualRoot, "Right Gold Border", false, false);
        AddCorner(visualRoot, "Top Left Corner", -1f, 1f);
        AddCorner(visualRoot, "Top Right Corner", 1f, 1f);
        AddCorner(visualRoot, "Bottom Left Corner", -1f, -1f);
        AddCorner(visualRoot, "Bottom Right Corner", 1f, -1f);

        AddText(visualRoot, "Top Center Ornament", "✦", 24f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(40f, 30f),
            new Vector2(0f, 8f), new Color32(255, 207, 92, 255));
        AddText(visualRoot, "Bottom Center Ornament", "✦", 24f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(40f, 30f),
            new Vector2(0f, -8f), new Color32(255, 207, 92, 255));

        AddDivider(visualRoot, "First Divider", 31f);
        AddDivider(visualRoot, "Second Divider", -31f);

        AddText(visualRoot, "Time Label", "TIME", 20f, TextAlignmentOptions.Left,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(150f, 34f),
            Vector2.zero, new Color32(244, 218, 151, 255), new Vector2(30f, 0f));
        AddText(visualRoot, "Time Value", "08:42", 22f, TextAlignmentOptions.Right,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(150f, 34f),
            Vector2.zero, new Color32(255, 229, 167, 255), new Vector2(-30f, 0f));

        AddText(visualRoot, "Stars Label", "STARS", 20f, TextAlignmentOptions.Left,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(150f, 34f),
            new Vector2(0f, 0f), new Color32(244, 218, 151, 255), new Vector2(30f, 0f));
        AddText(visualRoot, "Stars Value", "3 / 3", 22f, TextAlignmentOptions.Right,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(150f, 34f),
            new Vector2(0f, 0f), new Color32(255, 229, 167, 255), new Vector2(-30f, 0f));

        AddText(visualRoot, "Difficulty Label", "DIFFICULTY", 18f, TextAlignmentOptions.Left,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(170f, 34f),
            new Vector2(0f, -62f), new Color32(244, 218, 151, 255), new Vector2(30f, 0f));
        AddText(visualRoot, "Difficulty Value", "ADVANCED", 18f, TextAlignmentOptions.Right,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(170f, 34f),
            new Vector2(0f, -62f), new Color32(255, 229, 167, 255), new Vector2(-30f, 0f));
    }

    private void RefreshLayout()
    {
        if (_root == null)
            _root = transform as RectTransform;
        if (_root == null || transform.parent is not RectTransform parent)
            return;

        Vector2 parentSize = parent.rect.size;
        if (parentSize.x <= 0f || parentSize.y <= 0f || parentSize == _lastParentSize)
            return;

        _lastParentSize = parentSize;
        float width = Mathf.Min(520f, parentSize.x * 0.86f);
        float height = Mathf.Min(250f, parentSize.y * 0.58f);
        _root.anchorMin = _root.anchorMax = new Vector2(0.5f, 0.5f);
        _root.pivot = new Vector2(0.5f, 0.5f);
        _root.anchoredPosition = Vector2.zero;
        _root.sizeDelta = new Vector2(width, height);

        RectTransform visuals = transform.Find(VisualRootName) as RectTransform;
        if (visuals == null)
            return;

        foreach (TextMeshProUGUI text in visuals.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            bool compact = width < 400f;
            text.fontSize = compact ? (text.name.Contains("Label") ? 15f : 17f) :
                (text.name.Contains("Label") ? 19f : 21f);
        }

        SetDividerWidth(visuals, "First Divider", width - 56f);
        SetDividerWidth(visuals, "Second Divider", width - 56f);
    }

    private static void AddBorder(Transform parent, string objectName, bool horizontal, bool unused)
    {
        Image image = AddImage(parent, objectName, new Color32(224, 158, 43, 255));
        RectTransform rect = image.rectTransform;
        if (horizontal)
        {
            rect.anchorMin = new Vector2(0f, objectName.StartsWith("Top") ? 1f : 0f);
            rect.anchorMax = new Vector2(1f, objectName.StartsWith("Top") ? 1f : 0f);
            rect.pivot = new Vector2(0.5f, objectName.StartsWith("Top") ? 1f : 0f);
            rect.sizeDelta = new Vector2(-14f, 2f);
            rect.anchoredPosition = new Vector2(0f, objectName.StartsWith("Top") ? -7f : 7f);
        }
        else
        {
            bool left = objectName.StartsWith("Left");
            rect.anchorMin = new Vector2(left ? 0f : 1f, 0f);
            rect.anchorMax = new Vector2(left ? 0f : 1f, 1f);
            rect.pivot = new Vector2(left ? 0f : 1f, 0.5f);
            rect.sizeDelta = new Vector2(2f, -14f);
            rect.anchoredPosition = new Vector2(left ? 7f : -7f, 0f);
        }
    }

    private static void AddCorner(Transform parent, string objectName, float x, float y)
    {
        Image image = AddImage(parent, objectName, new Color32(255, 207, 92, 255));
        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(x > 0f ? 1f : 0f, y > 0f ? 1f : 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(8f, 8f);
        rect.anchoredPosition = new Vector2(x * -7f, y * -7f);
        rect.localRotation = Quaternion.Euler(0f, 0f, 45f);
    }

    private static void AddDivider(Transform parent, string objectName, float y)
    {
        Image line = AddImage(parent, objectName, new Color32(179, 122, 37, 220));
        RectTransform lineRect = line.rectTransform;
        lineRect.anchorMin = lineRect.anchorMax = new Vector2(0.5f, 0.5f);
        lineRect.sizeDelta = new Vector2(420f, 1.5f);
        lineRect.anchoredPosition = new Vector2(0f, y);

        AddText(parent, objectName + " Center Sparkle", "✦", 16f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(26f, 24f),
            new Vector2(0f, y), new Color32(255, 207, 92, 255));
    }

    private static void SetDividerWidth(RectTransform parent, string name, float width)
    {
        Transform child = parent.Find(name);
        if (child is RectTransform rect)
            rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
    }

    private static Image AddImage(Transform parent, string objectName, Color color)
    {
        GameObject child = CreateRectObject(objectName, parent);
        Image image = child.AddComponent<Image>();
        image.sprite = WhiteSprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TextMeshProUGUI AddText(
        Transform parent, string objectName, string value, float fontSize,
        TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 size, Vector2 position, Color color, Vector2? offset = null)
    {
        GameObject child = CreateRectObject(objectName, parent);
        TextMeshProUGUI text = child.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(anchorMax.x, anchorMax.y);
        rect.sizeDelta = size;
        rect.anchoredPosition = position + (offset ?? Vector2.zero);
        return text;
    }

    private static GameObject CreateRectObject(string objectName, Transform parent)
    {
        GameObject child = new GameObject(objectName, typeof(RectTransform));
        child.layer = parent.gameObject.layer;
        child.transform.SetParent(parent, false);
        return child;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Sprite WhiteSprite
    {
        get
        {
            if (_whiteSprite != null)
                return _whiteSprite;

            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;
            _whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            _whiteSprite.hideFlags = HideFlags.HideAndDontSave;
            return _whiteSprite;
        }
    }
}
