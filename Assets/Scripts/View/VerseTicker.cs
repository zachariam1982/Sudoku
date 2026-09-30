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
        "John 3:13–21 (KJV) — " +
        "13 And no man hath ascended up to heaven, but he that came down from heaven, even the Son of man which is in heaven. " +
        "14 And as Moses lifted up the serpent in the wilderness, even so must the Son of man be lifted up: " +
        "15 That whosoever believeth in him should not perish, but have eternal life. " +
        "16 For God so loved the world, that he gave his only begotten Son, that whosoever believeth in him should not perish, but have everlasting life. " +
        "17 For God sent not his Son into the world to condemn the world; but that the world through him might be saved. " +
        "18 He that believeth on him is not condemned: but he that believeth not is condemned already, because he hath not believed in the name of the only begotten Son of God. " +
        "19 And this is the condemnation, that light is come into the world, and men loved darkness rather than light, because their deeds were evil. " +
        "20 For every one that doeth evil hateth the light, neither cometh to the light, lest his deeds should be reproved. " +
        "21 But he that doeth truth cometh to the light, that his deeds may be made manifest, that they are wrought in God.";

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
