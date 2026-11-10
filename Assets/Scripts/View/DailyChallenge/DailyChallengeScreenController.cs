using TMPro;
using UnityEngine;

/// <summary>
/// Controls the ready-to-play page of the Daily Challenge screen and keeps
/// the paired landscape cards inside the available canvas width.
/// </summary>
public sealed class DailyChallengeScreenController : MonoBehaviour
{
    [Header("Scene Dependencies")]
    [SerializeField] private GameContext gameContext;
    [SerializeField] private Canvas canvas;

    [SerializeField] private GameObject readyPage;
    [SerializeField] private GameObject portraitLayout;
    [SerializeField] private GameObject landscapeLayout;

    [Header("Landscape Cards")]
    [SerializeField] private RectTransform landscapeRoot;
    [SerializeField] private RectTransform challengeCard;
    [SerializeField] private RectTransform statsCard;
    [SerializeField] private TextMeshProUGUI _bestTimeLabel;
    [SerializeField] private TextMeshProUGUI _bestTimeValue;
    [SerializeField] private TextMeshProUGUI _streakLabel;
    [SerializeField] private TextMeshProUGUI _streakValue;

    [Header("Ready Page Labels")]
    [SerializeField] private TextMeshProUGUI _portraitChallengeNumber;
    [SerializeField] private TextMeshProUGUI _portraitDate;
    [SerializeField] private TextMeshProUGUI _portraitDifficulty;
    [SerializeField] private TextMeshProUGUI _landscapeChallengeNumber;
    [SerializeField] private TextMeshProUGUI _landscapeDate;
    [SerializeField] private TextMeshProUGUI _landscapeDifficulty;
    [SerializeField] private TextMeshProUGUI _portraitBestTimeValue;
    [SerializeField] private TextMeshProUGUI _portraitStreakValue;
    [SerializeField] private TextMeshProUGUI _portraitStartChallengeLabel;
    [SerializeField] private TextMeshProUGUI _landscapeStartChallengeLabel;

    [Header("Responsive Buttons")]
    [SerializeField] private RectTransform settingsButton;
    [SerializeField] private RectTransform portraitHomeButton;
    [SerializeField] private RectTransform landscapeHomeButton;

    private float _baseLandscapeCardWidth;
    private bool _isLandscape;
    private bool _orientationInitialized;

    private const float LandscapeCardSideMargin = 48f;
    private const float MinimumLandscapeCardGap = 48f;
    private const float MaximumLandscapeCardGap = 280f;
    private const float LandscapeStatTextWidth = 320f;
    private const float LandscapeStatLabelHeight = 76f;
    private const float LandscapeStatValueHeight = 104f;

    private void Awake()
    {
        _baseLandscapeCardWidth = challengeCard != null && statsCard != null
            ? Mathf.Min(challengeCard.sizeDelta.x, statsCard.sizeDelta.x)
            : 0f;
    }

    private void OnEnable()
    {
        RefreshOrientation();
    }

    private void Update()
    {
        // Unlike a screen opened from the home menu, this page can stay open
        // while the device rotates. Detect the dimension change and swap the
        // paired layouts immediately.
        RefreshOrientation();
    }

    private void RefreshOrientation()
    {
        if (Screen.width <= 0 || Screen.height <= 0)
            return;

        bool landscape = Screen.width > Screen.height;
        if (!_orientationInitialized || landscape != _isLandscape)
            SetLandscape(landscape);
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

        gameContext?.RefreshDailyChallengeScreen(this);
    }

    public void SetChallengeData(
        DailyChallengeRecord today,
        DailyChallengeStatsSnapshot stats,
        int challengeCount)
    {
        if (today == null)
            return;

        if (stats == null)
            stats = new DailyChallengeStatsSnapshot();

        string number = $"CHALLENGE #{challengeCount}";
        string date = today.CurrentDate.ToString("MMM dd, yyyy", System.Globalization.CultureInfo.InvariantCulture)
            .ToUpperInvariant();
        string difficulty = ((SudokuDifficulty)today.Difficulty).ToString().ToUpperInvariant();
        string bestTime = FormatBestTime(stats.BestTimeSeconds);
        string streak = stats.CurrentStreak.ToString(System.Globalization.CultureInfo.InvariantCulture);

        SetText(_portraitChallengeNumber, number);
        SetText(_landscapeChallengeNumber, number);
        SetText(_portraitDate, date);
        SetText(_landscapeDate, date);
        SetText(_portraitDifficulty, difficulty);
        SetText(_landscapeDifficulty, difficulty);
        SetText(_portraitBestTimeValue, bestTime);
        SetText(_portraitStreakValue, streak);
        SetText(_bestTimeValue, bestTime);
        SetText(_streakValue, streak);
        string challengeAction = !string.IsNullOrWhiteSpace(today.CompletedAt)
            ? "CHALLENGE COMPLETED"
            : today.Started || today.IsAttempted
                ? "RESUME CHALLENGE"
                : "START CHALLENGE";
        SetText(_portraitStartChallengeLabel, challengeAction);
        SetText(_landscapeStartChallengeLabel, challengeAction);
        ApplyLandscapeStatsTypography();
    }

    private static void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
            text.text = value;
    }

    private static string FormatBestTime(float seconds)
    {
        if (seconds < 0f || float.IsNaN(seconds) || float.IsInfinity(seconds))
            return "--:--";

        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int minutes = totalSeconds / 60;
        int remainingSeconds = totalSeconds % 60;
        return string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "{0:00}:{1:00}",
            minutes,
            remainingSeconds);
    }

    public void Hide() => gameObject.SetActive(false);

    public void StartChallenge()
    {
        gameContext?.StartDailyChallenge(this);
    }

    public void SetLandscape(bool landscape)
    {
        _orientationInitialized = true;
        _isLandscape = landscape;

        if (portraitLayout != null) portraitLayout.SetActive(!landscape);
        if (landscapeLayout != null) landscapeLayout.SetActive(landscape);
        AlignSettingsButtonWithHome(landscape);

        if (landscape)
            ApplyLandscapeCardLayout();
    }

    private void AlignSettingsButtonWithHome(bool landscape)
    {
        if (settingsButton == null)
            return;

        RectTransform homeButton = landscape ? landscapeHomeButton : portraitHomeButton;
        if (homeButton == null)
            return;

        settingsButton.anchorMin = new Vector2(1f - homeButton.anchorMin.x, homeButton.anchorMin.y);
        settingsButton.anchorMax = new Vector2(1f - homeButton.anchorMax.x, homeButton.anchorMax.y);
        settingsButton.anchoredPosition = new Vector2(
            -homeButton.anchoredPosition.x,
            homeButton.anchoredPosition.y);
    }

    private void ApplyLandscapeCardLayout()
    {
        if (landscapeLayout == null)
            return;

        if (landscapeRoot == null || challengeCard == null || statsCard == null ||
            _baseLandscapeCardWidth <= 0f)
            return;

        float availableWidth = landscapeRoot.rect.width;
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
        SetCenteredHorizontally(challengeCard, -((cardWidth + gap) * 0.5f), cardWidth);
        SetCenteredHorizontally(statsCard, (cardWidth + gap) * 0.5f, cardWidth);
        ApplyLandscapeStatsTypography();
    }

    private void ApplyLandscapeStatsTypography()
    {
        float canvasScale = canvas != null ? canvas.scaleFactor : 1f;
        if (canvasScale <= 0f)
            canvasScale = 1f;

        SetLandscapeStatText(_bestTimeLabel, 30f / canvasScale,
            LandscapeStatTextWidth, LandscapeStatLabelHeight);
        SetLandscapeStatText(_streakLabel, 30f / canvasScale,
            LandscapeStatTextWidth, LandscapeStatLabelHeight);
        SetLandscapeStatText(_bestTimeValue, 42f / canvasScale,
            LandscapeStatTextWidth, LandscapeStatValueHeight);
        SetLandscapeStatText(_streakValue, 42f / canvasScale,
            LandscapeStatTextWidth, LandscapeStatValueHeight);
    }

    private static void SetLandscapeStatText(
        TextMeshProUGUI text, float fontSize, float width, float height)
    {
        if (text == null)
            return;

        text.enableAutoSizing = true;
        text.fontSizeMin = fontSize * 0.85f;
        text.fontSizeMax = fontSize;
        text.fontSize = fontSize;

        RectTransform rect = text.rectTransform;
        Vector2 size = rect.sizeDelta;
        size.x = width;
        size.y = height;
        rect.sizeDelta = size;
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
