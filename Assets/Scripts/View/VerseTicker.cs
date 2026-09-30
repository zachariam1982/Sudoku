using TMPro;
using UnityEngine;

/// <summary>
/// Scrolls the fixed John 3:13-21 passage while the gameplay HUD is visible.
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
    [SerializeField] private float scrollSpeed = 28f;
    [SerializeField] private float textPadding = 10f;

    private const string VerseText =
        "John 3:13–21 (NCB) — " +
        "No one has gone up to heaven except the one who descended from heaven, the Son of Man. " +
        "And just as Moses lifted up the serpent in the desert, so must the Son of Man be lifted up, " +
        "in order that everyone who believes in him may have eternal life. " +
        "For God so loved the world that he gave his only Son, so that everyone who believes in him may not perish but may attain eternal life. " +
        "For God did not send his Son into the world to condemn the world but in order that the world might be saved through him. " +
        "Whoever believes in him is not condemned, but whoever does not believe in him already stands condemned, because he has not believed in the name of the only-begotten Son of God. " +
        "And the judgment is this: the light has come into the world, but people preferred darkness to light because their deeds were evil. " +
        "Everyone who does evil hates the light and avoids coming near the light so that his misdeeds may not be exposed. " +
        "However, whoever lives by the truth comes to the light so that it may be clearly seen that his deeds have been done in God.";

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

        label.text = VerseText;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.ForceMeshUpdate();

        float width = label.preferredWidth;
        textRect.sizeDelta = new Vector2(width, viewport.rect.height - 2f * textPadding);
        _start = viewport.rect.width * 0.5f + width * 0.5f;
        _end = -viewport.rect.width * 0.5f - width * 0.5f;

        if (resetPosition)
            textRect.anchoredPosition = new Vector2(_start, 0f);
    }
}
