using TMPro;
using UnityEngine;

/// <summary>
/// Animates the scene-authored verse ticker views.
/// </summary>
[DisallowMultipleComponent]
public sealed class VerseTicker : MonoBehaviour
{
    [Header("Unity-authored views")]
    [SerializeField] private RectTransform portraitViewport;
    [SerializeField] private RectTransform portraitTextRect;
    [SerializeField] private TextMeshProUGUI portraitLabel;
    [SerializeField] private RectTransform landscapeViewport;
    [SerializeField] private RectTransform landscapeTextRect;
    [SerializeField] private TextMeshProUGUI landscapeLabel;
    [SerializeField] private GameObject hudRoot;

    [Header("Ticker")]
    [SerializeField] private string verseText =
        "John 3:16 (WEB) — For God so loved the world, that he gave his one and only Son, that whoever believes in him should not perish, but have eternal life.";
    [SerializeField] private float scrollSpeed = 28f;
    [SerializeField] private float textPadding = 10f;

    private bool _gameActive;
    public bool IsGameActive => _gameActive;
    private bool _isLandscape;
    private float _start;
    private float _end;

    private void Awake()
    {
        ConfigureLayout(true);
    }

    public void SetGameActive(bool active)
    {
        _gameActive = active;
        ConfigureLayout(true);
    }

    private void Update()
    {
        bool landscape = Screen.width > Screen.height;
        RectTransform viewport = landscape ? landscapeViewport : portraitViewport;
        bool hudVisible = hudRoot != null && hudRoot.activeInHierarchy;
        bool shouldBeVisible = _gameActive && hudVisible;

        if (landscape != _isLandscape ||
            (viewport != null && viewport.gameObject.activeSelf != shouldBeVisible))
        {
            ConfigureLayout(true);
        }

        if (!_gameActive || !hudVisible) return;

        RectTransform textRect = _isLandscape ? landscapeTextRect : portraitTextRect;
        viewport = _isLandscape ? landscapeViewport : portraitViewport;
        if (textRect == null || viewport == null || !viewport.gameObject.activeSelf) return;

        Vector2 position = textRect.anchoredPosition;
        if (_isLandscape)
        {
            position.y += scrollSpeed * Time.unscaledDeltaTime;
            if (position.y >= _end) position.y = _start;
        }
        else
        {
            position.x -= scrollSpeed * Time.unscaledDeltaTime;
            if (position.x <= _end) position.x = _start;
        }

        textRect.anchoredPosition = position;
    }

    private void ConfigureLayout(bool resetPosition)
    {
        _isLandscape = Screen.width > Screen.height;

        bool hudVisible = hudRoot != null && hudRoot.activeInHierarchy;
        bool shouldBeVisible = _gameActive && hudVisible;

        if (portraitViewport != null)
            portraitViewport.gameObject.SetActive(shouldBeVisible && !_isLandscape);
        if (landscapeViewport != null)
            landscapeViewport.gameObject.SetActive(shouldBeVisible && _isLandscape);

        if (_isLandscape)
        {
            ConfigureLandscape(resetPosition);
        }
        else
        {
            ConfigurePortrait(resetPosition);
        }
    }

    private void ConfigurePortrait(bool resetPosition)
    {
        if (portraitViewport == null || portraitTextRect == null || portraitLabel == null) return;

        portraitLabel.text = verseText;
        portraitLabel.textWrappingMode = TextWrappingModes.NoWrap;
        portraitLabel.alignment = TextAlignmentOptions.MidlineLeft;
        portraitLabel.ForceMeshUpdate();

        float width = Mathf.Max(portraitLabel.preferredWidth, portraitViewport.rect.width);
        portraitTextRect.sizeDelta = new Vector2(width, portraitViewport.rect.height - 2f * textPadding);
        _start = portraitViewport.rect.width * 0.5f + width * 0.5f;
        _end = -portraitViewport.rect.width * 0.5f - width * 0.5f;
        if (resetPosition)
            portraitTextRect.anchoredPosition = new Vector2(_start, 0f);
    }

    private void ConfigureLandscape(bool resetPosition)
    {
        if (landscapeViewport == null || landscapeTextRect == null || landscapeLabel == null) return;

        landscapeLabel.text = verseText;
        landscapeLabel.textWrappingMode = TextWrappingModes.Normal;
        landscapeLabel.alignment = TextAlignmentOptions.Center;
        landscapeTextRect.sizeDelta = new Vector2(landscapeViewport.rect.width - 2f * textPadding, 1000f);
        landscapeLabel.ForceMeshUpdate();

        float height = Mathf.Max(landscapeLabel.preferredHeight, landscapeViewport.rect.height);
        landscapeTextRect.sizeDelta = new Vector2(landscapeViewport.rect.width - 2f * textPadding, height);
        _start = -landscapeViewport.rect.height * 0.5f - height * 0.5f;
        _end = landscapeViewport.rect.height * 0.5f + height * 0.5f;
        if (resetPosition)
            landscapeTextRect.anchoredPosition = new Vector2(0f, _start);
    }
}
