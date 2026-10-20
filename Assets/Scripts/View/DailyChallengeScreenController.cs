using UnityEngine;

/// <summary>
/// Controls the ready-to-play page of the Daily Challenge screen and keeps
/// the paired landscape cards inside the available canvas width.
/// </summary>
public sealed class DailyChallengeScreenController : MonoBehaviour
{
    [SerializeField] private GameObject readyPage;
    [SerializeField] private GameObject portraitLayout;
    [SerializeField] private GameObject landscapeLayout;

    private RectTransform _landscapeRoot;
    private RectTransform _challengeCard;
    private RectTransform _statsCard;
    private float _baseLandscapeCardWidth;
    private bool _isLandscape;

    private const float LandscapeCardSideMargin = 48f;
    private const float MinimumLandscapeCardGap = 48f;
    private const float MaximumLandscapeCardGap = 280f;

    private void Awake()
    {
        CacheLandscapeCards();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (_isLandscape)
            ApplyLandscapeCardLayout();
    }

    public void Show()
    {
        gameObject.SetActive(true);
        if (readyPage != null) readyPage.SetActive(true);
    }

    public void Hide() => gameObject.SetActive(false);

    public void SetLandscape(bool landscape)
    {
        _isLandscape = landscape;

        if (portraitLayout != null) portraitLayout.SetActive(!landscape);
        if (landscapeLayout != null) landscapeLayout.SetActive(landscape);

        if (landscape)
            ApplyLandscapeCardLayout();
    }

    private void CacheLandscapeCards()
    {
        if (landscapeLayout == null)
            return;

        _landscapeRoot = landscapeLayout.transform as RectTransform;
        RectTransform[] descendants = landscapeLayout.GetComponentsInChildren<RectTransform>(true);
        foreach (RectTransform candidate in descendants)
        {
            if (candidate.name == "ChallengeCard_LandscapeLayout")
                _challengeCard = candidate;
            else if (candidate.name == "StatsCard_LandscapeLayout")
                _statsCard = candidate;
        }

        if (_challengeCard != null && _statsCard != null)
        {
            _baseLandscapeCardWidth = Mathf.Min(
                _challengeCard.sizeDelta.x,
                _statsCard.sizeDelta.x);
        }
    }

    private void ApplyLandscapeCardLayout()
    {
        if (landscapeLayout == null)
            return;

        if (_landscapeRoot == null || _challengeCard == null || _statsCard == null)
            CacheLandscapeCards();

        if (_landscapeRoot == null || _challengeCard == null || _statsCard == null ||
            _baseLandscapeCardWidth <= 0f)
            return;

        float availableWidth = _landscapeRoot.rect.width;
        if (availableWidth <= 0f)
            return;

        float leftoverAtBaseWidth =
            availableWidth - (2f * _baseLandscapeCardWidth) - (2f * LandscapeCardSideMargin);
        float gap = Mathf.Clamp(
            leftoverAtBaseWidth * 0.5f,
            MinimumLandscapeCardGap,
            MaximumLandscapeCardGap);
        float maximumCardWidth =
            (availableWidth - (2f * LandscapeCardSideMargin) - gap) * 0.5f;
        float cardWidth = Mathf.Min(_baseLandscapeCardWidth, maximumCardWidth);
        if (cardWidth <= 0f)
            return;

        // Use equal widths and symmetric centers so the cards cannot overlap
        // on narrower landscape screens or tablets.
        SetCenteredHorizontally(_challengeCard, -((cardWidth + gap) * 0.5f), cardWidth);
        SetCenteredHorizontally(_statsCard, (cardWidth + gap) * 0.5f, cardWidth);
    }

    private static void SetCenteredHorizontally(RectTransform target, float x, float width)
    {
        Vector2 anchorMin = target.anchorMin;
        Vector2 anchorMax = target.anchorMax;
        anchorMin.x = 0.5f;
        anchorMax.x = 0.5f;
        target.anchorMin = anchorMin;
        target.anchorMax = anchorMax;

        Vector2 size = target.sizeDelta;
        size.x = width;
        target.sizeDelta = size;

        Vector2 position = target.anchoredPosition;
        position.x = x;
        target.anchoredPosition = position;
    }
}
