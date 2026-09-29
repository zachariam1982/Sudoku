using TMPro;
using UnityEngine;

/// <summary>
/// Scrolls the scene-authored verse ticker while the gameplay HUD is visible.
/// </summary>
[DisallowMultipleComponent]
public sealed class VerseTicker : MonoBehaviour
{
    [Header("Unity-authored view")]
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform textRect;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private GameObject hudRoot;

    [Header("Ticker")]
    [SerializeField] private string verseText =
        "John 3:16 (WEB) — For God so loved the world, that he gave his one and only Son, that whoever believes in him should not perish, but have eternal life.";
    [SerializeField] private float scrollSpeed = 28f;
    [SerializeField] private float textPadding = 10f;

    private bool _gameActive;
    private float _start;
    private float _end;

    private void Awake()
    {
        RefreshVisibility(true);
    }

    public bool IsGameActive => _gameActive;

    public void SetGameActive(bool active)
    {
        _gameActive = active;
        RefreshVisibility(true);
    }

    private void Update()
    {
        bool hudVisible = hudRoot != null && hudRoot.activeInHierarchy;
        bool shouldBeVisible = _gameActive && hudVisible;

        if (viewport != null && viewport.gameObject.activeSelf != shouldBeVisible)
            RefreshVisibility(true);

        if (!shouldBeVisible || viewport == null || textRect == null) return;

        Vector2 position = textRect.anchoredPosition;
        position.x -= scrollSpeed * Time.unscaledDeltaTime;
        if (position.x <= _end) position.x = _start;
        textRect.anchoredPosition = position;
    }

    private void RefreshVisibility(bool resetPosition)
    {
        bool visible = _gameActive && hudRoot != null && hudRoot.activeInHierarchy;
        if (viewport != null)
            viewport.gameObject.SetActive(visible);

        if (viewport == null || textRect == null || label == null) return;

        label.text = verseText;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.ForceMeshUpdate();

        float width = Mathf.Max(label.preferredWidth, viewport.rect.width);
        textRect.sizeDelta = new Vector2(width, viewport.rect.height - 2f * textPadding);
        _start = viewport.rect.width * 0.5f + width * 0.5f;
        _end = -viewport.rect.width * 0.5f - width * 0.5f;

        if (resetPosition)
            textRect.anchoredPosition = new Vector2(_start, 0f);
    }
}
