using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Builds the Daily Challenge ready screen from native Unity UI.</summary>
internal sealed class DailyChallengeReadyScreen
{
    private static readonly Color Gold = Hex("FFBE4C");
    private static readonly Color White = Hex("F5F4FF");
    private static readonly Color Purple = Hex("A983FF");
    private static readonly Color Navy = new Color(5f / 255f, 13f / 255f, 34f / 255f, 0.91f);

    private readonly RectTransform _root;
    private readonly RectTransform _safeArea;
    private readonly TMP_FontAsset _font;
    private readonly Sprite _homeIcon;
    private readonly Sprite _streakIcon;
    private readonly Action _onHome;
    private readonly Action _onStart;

    public DailyChallengeReadyScreen(
        RectTransform parent,
        TMP_FontAsset font,
        Sprite homeIcon,
        Sprite streakIcon,
        Action onHome,
        Action onStart)
    {
        _font = font;
        _homeIcon = homeIcon;
        _streakIcon = streakIcon;
        _onHome = onHome;
        _onStart = onStart;

        _root = CreateRect("DailyChallengeReady", parent);
        Stretch(_root);
        Image hitArea = _root.gameObject.AddComponent<Image>();
        hitArea.color = new Color(0f, 0f, 0f, 0f);
        hitArea.raycastTarget = true;

        _safeArea = CreateRect("SafeArea", _root);
        BuildContent();
        Layout(IsLandscape());
        Hide();
    }

    public void Show()
    {
        Layout(IsLandscape());
        _root.gameObject.SetActive(true);
    }

    public void Hide() => _root.gameObject.SetActive(false);

    public void Layout(bool landscape)
    {
        if (_safeArea == null) return;

        Rect safe = Screen.safeArea;
        _safeArea.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        _safeArea.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        _safeArea.offsetMin = _safeArea.offsetMax = Vector2.zero;

        LayoutPortrait();
        if (landscape)
            LayoutLandscape();
    }

    private void BuildContent()
    {
        Button home = MakeButton("Home", _safeArea, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(28f, -28f), new Vector2(76f, 76f), null, _onHome);
        Image homeImage = home.GetComponent<Image>();
        homeImage.sprite = _homeIcon;
        homeImage.color = Color.white;
        homeImage.preserveAspect = true;

        Text(_safeArea, "DAILY CHALLENGE", 0.5f, 0.94f, 900f, 86f, 52f, Gold, FontStyles.Bold);
        Text(_safeArea, "Oct 19, 2026", 0.5f, 0.875f, 800f, 52f, 30f, White, FontStyles.Normal);

        RectTransform challengeCard = Card("ChallengeCard", _safeArea);
        Text(challengeCard, "CHALLENGE #293", 0.5f, 0.77f, 860f, 68f, 34f, Gold, FontStyles.Bold);
        Text(challengeCard, "TODAY’S PUZZLE", 0.5f, 0.48f, 860f, 64f, 37f, White, FontStyles.Bold);
        RectTransform badge = CreateRect("DifficultyBadge", challengeCard);
        Centered(badge, 0f, -115f, 260f, 70f);
        Image badgeFill = badge.gameObject.AddComponent<Image>();
        badgeFill.color = new Color(0.12f, 0.08f, 0.23f, 0.92f);
        Outline badgeOutline = badge.gameObject.AddComponent<Outline>();
        badgeOutline.effectColor = Purple;
        badgeOutline.effectDistance = new Vector2(2f, -2f);
        Text(badge, "HARD", 0.5f, 0.5f, 240f, 56f, 28f, Purple, FontStyles.Bold);

        RectTransform statsCard = Card("DailyStatsCard", _safeArea);
        AddStat(statsCard, "BestTime", "BEST TIME", "--:--", 0.27f, null);
        AddStat(statsCard, "DailyStreak", "DAILY STREAK", "0", 0.73f, _streakIcon);

        RectTransform startButtonRect = CreateRect("StartChallengeButton", _safeArea);
        Centered(startButtonRect, 0f, 0.14f, 900f, 132f);
        Image startBorder = startButtonRect.gameObject.AddComponent<Image>();
        startBorder.color = Gold;
        Button start = startButtonRect.gameObject.AddComponent<Button>();
        start.targetGraphic = startBorder;
        start.onClick.AddListener(() => _onStart?.Invoke());
        RectTransform buttonInset = CreateRect("ButtonInset", startButtonRect);
        Stretch(buttonInset);
        buttonInset.offsetMin = new Vector2(3f, 3f);
        buttonInset.offsetMax = new Vector2(-3f, -3f);
        Image insetImage = buttonInset.gameObject.AddComponent<Image>();
        insetImage.color = new Color(7f / 255f, 16f / 255f, 36f / 255f, 0.97f);
        insetImage.raycastTarget = false;
        Text(buttonInset, "START CHALLENGE", 0.5f, 0.5f, 860f, 118f, 38f, Gold, FontStyles.Bold);
    }

    private void AddStat(RectTransform parent, string name, string label,
        string value, float x, Sprite iconSprite)
    {
        if (iconSprite != null)
        {
            RectTransform iconRect = CreateRect(name + "Icon", parent);
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(x, 0.73f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(52f, 52f);
            Image iconImage = iconRect.gameObject.AddComponent<Image>();
            iconImage.sprite = iconSprite;
            iconImage.preserveAspect = true;
        }
        else
        {
            BuildClockIcon(parent, x);
        }

        Text(parent, label, x, 0.47f, 440f, 50f, 23f, White, FontStyles.Bold);
        Text(parent, value, x, 0.24f, 440f, 58f, 34f, Gold, FontStyles.Bold);
    }

    private void BuildClockIcon(RectTransform parent, float x)
    {
        RectTransform clock = CreateRect("ClockIcon", parent);
        clock.anchorMin = clock.anchorMax = new Vector2(x, 0.73f);
        clock.anchoredPosition = Vector2.zero;
        clock.sizeDelta = new Vector2(54f, 54f);

        // Small UI marks form a clean clock face without relying on font glyph coverage.
        const int tickCount = 12;
        for (int i = 0; i < tickCount; i++)
        {
            float angle = i * Mathf.PI * 2f / tickCount;
            RectTransform tick = CreateRect("ClockTick", clock);
            Centered(tick, Mathf.Sin(angle) * 22f, Mathf.Cos(angle) * 22f, 4f, 4f);
            tick.gameObject.AddComponent<Image>().color = Gold;
        }

        RectTransform minuteHand = CreateRect("MinuteHand", clock);
        Centered(minuteHand, 2f, 4f, 3f, 18f);
        minuteHand.localRotation = Quaternion.Euler(0f, 0f, 0f);
        minuteHand.gameObject.AddComponent<Image>().color = Gold;

        RectTransform hourHand = CreateRect("HourHand", clock);
        Centered(hourHand, -4f, -2f, 3f, 13f);
        hourHand.localRotation = Quaternion.Euler(0f, 0f, -45f);
        hourHand.gameObject.AddComponent<Image>().color = Gold;
    }

    private void LayoutPortrait()
    {
        // Position the panel around the moon, leaving visible sky above and lake below.
        PlaceAtAnchor(Find(_safeArea, "DAILY CHALLENGE"), 0.5f, 0.94f, 780f, 86f);
        PlaceAtAnchor(Find(_safeArea, "Oct 19, 2026"), 0.5f, 0.875f, 760f, 52f);
        RectTransform challenge = Find(_safeArea, "ChallengeCard");
        RectTransform stats = Find(_safeArea, "DailyStatsCard");
        RectTransform start = Find(_safeArea, "StartChallengeButton");
        PlaceAtAnchor(challenge, 0.5f, 0.60f, 800f, 340f);
        PlaceAtAnchor(stats, 0.5f, 0.36f, 800f, 255f);
        PlaceAtAnchor(start, 0.5f, 0.15f, 800f, 124f);
        Find(challenge, "CHALLENGE #293").sizeDelta = new Vector2(740f, 68f);
        Find(challenge, "TODAY’S PUZZLE").sizeDelta = new Vector2(740f, 64f);
        Find(stats, "BEST TIME").sizeDelta = new Vector2(360f, 50f);
        Find(stats, "--:--").sizeDelta = new Vector2(360f, 58f);
        Find(stats, "DAILY STREAK").sizeDelta = new Vector2(360f, 50f);
        Find(stats, "0").sizeDelta = new Vector2(360f, 58f);
        RectTransform inset = Find(start, "ButtonInset");
        Find(inset, "START CHALLENGE").sizeDelta = new Vector2(740f, 118f);
    }

    private void LayoutLandscape()
    {
        RectTransform title = Find(_safeArea, "DAILY CHALLENGE");
        RectTransform date = Find(_safeArea, "Oct 19, 2026");
        RectTransform challenge = Find(_safeArea, "ChallengeCard");
        RectTransform stats = Find(_safeArea, "DailyStatsCard");
        RectTransform start = Find(_safeArea, "StartChallengeButton");
        PlaceAtAnchor(title, 0.5f, 0.90f, 840f, 76f);
        PlaceAtAnchor(date, 0.5f, 0.81f, 700f, 48f);
        PlaceAtAnchor(challenge, 0.38f, 0.54f, 940f, 340f);
        PlaceAtAnchor(stats, 0.62f, 0.54f, 650f, 255f);
        PlaceAtAnchor(start, 0.5f, 0.16f, 1200f, 118f);

        Find(challenge, "CHALLENGE #293").sizeDelta = new Vector2(860f, 68f);
        Find(challenge, "TODAY’S PUZZLE").sizeDelta = new Vector2(860f, 64f);
        Find(stats, "BEST TIME").sizeDelta = new Vector2(260f, 50f);
        Find(stats, "--:--").sizeDelta = new Vector2(260f, 58f);
        Find(stats, "DAILY STREAK").sizeDelta = new Vector2(260f, 50f);
        Find(stats, "0").sizeDelta = new Vector2(260f, 58f);
        RectTransform inset = Find(start, "ButtonInset");
        Find(inset, "START CHALLENGE").sizeDelta = new Vector2(1120f, 106f);
    }

    private RectTransform Card(string name, RectTransform parent)
    {
        RectTransform border = CreateRect(name, parent);
        Image borderImage = border.gameObject.AddComponent<Image>();
        borderImage.color = Gold;
        RectTransform inset = CreateRect("Fill", border);
        Stretch(inset);
        inset.offsetMin = new Vector2(2f, 2f);
        inset.offsetMax = new Vector2(-2f, -2f);
        Image fill = inset.gameObject.AddComponent<Image>();
        fill.color = Navy;
        return border;
    }

    private TextMeshProUGUI Text(RectTransform parent, string name, float anchorX, float anchorY,
        float width, float height, float size, Color color, FontStyles style)
    {
        RectTransform rect = CreateRect(name, parent);
        Centered(rect, 0f, 0f, width, height);
        rect.anchorMin = rect.anchorMax = new Vector2(anchorX, anchorY);
        rect.anchoredPosition = Vector2.zero;
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = _font;
        text.text = name;
        text.fontSize = size;
        text.fontSizeMin = Mathf.Max(18f, size * 0.65f);
        text.fontSizeMax = size;
        text.enableAutoSizing = true;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = style;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private Button MakeButton(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 position, Vector2 size, Sprite sprite, Action action)
    {
        RectTransform rect = CreateRect(name, parent);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = sprite == null ? Gold : Color.white;
        image.preserveAspect = true;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => action?.Invoke());
        return button;
    }

    private static RectTransform CreateRect(string name, RectTransform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void Centered(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void PlaceAtAnchor(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(x, y);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(width, height);
    }

    private static RectTransform Find(RectTransform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            RectTransform child = parent.GetChild(i) as RectTransform;
            if (child != null && child.name == name) return child;
        }
        return null;
    }

    private static bool IsLandscape() => Screen.width > Screen.height;

    private static Color Hex(string value)
    {
        ColorUtility.TryParseHtmlString("#" + value, out Color result);
        return result;
    }
}
