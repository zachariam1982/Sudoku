using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Builds the Daily Challenge ready panel from Unity UI elements.</summary>
internal sealed class DailyChallengeReadyScreen
{
    private const string SampleDate = "OCT 19, 2026";
    private const string SampleChallenge = "CHALLENGE #293";
    private static readonly Color Gold = Hex("FFBE4C");
    private static readonly Color White = Hex("F5F4FF");
    private static readonly Color Purple = Hex("C66BFF");
    private static readonly Color PanelNavy = new Color(4f / 255f, 12f / 255f, 27f / 255f, 0.88f);

    private readonly RectTransform _root;
    private readonly RectTransform _safeArea;
    private readonly TMP_FontAsset _font;
    private readonly Sprite _homeIcon;
    private readonly Sprite _streakIcon;
    private readonly Sprite _frameSprite;
    private readonly Action _onHome;
    private readonly Action _onStart;

    public DailyChallengeReadyScreen(
        RectTransform parent,
        TMP_FontAsset font,
        Sprite homeIcon,
        Sprite streakIcon,
        Sprite frameSprite,
        Action onHome,
        Action onStart)
    {
        _font = font;
        _homeIcon = homeIcon;
        _streakIcon = streakIcon;
        _frameSprite = frameSprite;
        _onHome = onHome;
        _onStart = onStart;

        _root = MakeRect("DailyChallengeReadyScreen", parent);
        Stretch(_root);
        Image background = _root.gameObject.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0f);
        background.raycastTarget = true;

        _safeArea = MakeRect("SafeArea", _root);
        BuildScreen();
        Layout(Screen.width > Screen.height);
        Hide();
    }

    public void Show()
    {
        Layout(Screen.width > Screen.height);
        _root.gameObject.SetActive(true);
    }

    public void Hide() => _root.gameObject.SetActive(false);

    public void Layout(bool landscape)
    {
        Rect safe = Screen.safeArea;
        _safeArea.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        _safeArea.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        _safeArea.offsetMin = _safeArea.offsetMax = Vector2.zero;
        LayoutPortrait();
        if (landscape) LayoutLandscape();
    }

    private void BuildScreen()
    {
        Button homeButton = MakeButton("HomeButton", _safeArea, _onHome);
        RectTransform homeRect = (RectTransform)homeButton.transform;
        Place(homeRect, 0.08f, 0.94f, 82f, 82f);
        Image homeImage = homeButton.GetComponent<Image>();
        homeImage.sprite = _homeIcon;
        homeImage.preserveAspect = true;
        homeImage.color = Color.white;
        if (_homeIcon == null)
            MakeText(homeRect, "HomeFallback", "⌂", 64f, Gold, FontStyles.Normal);

        MakeText(_safeArea, "Title", "DAILY CHALLENGE", 62f, Gold, FontStyles.Bold);
        AddImage(_safeArea, "TitleRuleLeft", Gold, 0.13f, 0.75f, 100f, 4f);
        AddImage(_safeArea, "TitleRuleRight", Gold, 0.87f, 0.75f, 100f, 4f);
        MakeText(_safeArea, "Date", SampleDate, 36f, White, FontStyles.Normal);

        RectTransform challenge = MakeCard("ChallengeCard", _safeArea);
        MakeText(challenge, "ChallengeNumber", SampleChallenge, 54f, White, FontStyles.Bold);
        MakeText(challenge, "PuzzleLabel", "TODAY’S PUZZLE", 37f, White, FontStyles.Normal);
        RectTransform badge = MakeRect("DifficultyBadge", challenge);
        AddInsetFill(badge, PanelNavy, 12f);
        AddFrame(badge, Purple);
        MakeText(badge, "Difficulty", "HARD", 39f, Purple, FontStyles.Bold);

        RectTransform stats = MakeCard("StatsCard", _safeArea);
        AddClock(stats);
        MakeText(stats, "BestTimeLabel", "BEST TIME", 29f, White, FontStyles.Normal);
        MakeText(stats, "BestTimeValue", "--:--", 40f, White, FontStyles.Bold);
        AddImage(stats, "StatsDivider", Hex("9DA0AE"), 0.5f, 0.5f, 2f, 168f);

        RectTransform streakIcon = MakeRect("StreakIcon", stats);
        Image streakImage = streakIcon.gameObject.AddComponent<Image>();
        streakImage.sprite = _streakIcon;
        streakImage.preserveAspect = true;
        if (_streakIcon == null)
            MakeText(streakIcon, "StreakFallback", "♨", 45f, Gold, FontStyles.Bold);
        MakeText(stats, "StreakLabel", "DAILY STREAK", 29f, White, FontStyles.Normal);
        MakeText(stats, "StreakValue", "0", 43f, White, FontStyles.Bold);

        RectTransform start = MakeRect("StartChallengeButton", _safeArea);
        Image startImage = start.gameObject.AddComponent<Image>();
        startImage.color = Color.clear;
        AddInsetFill(start, new Color(2f / 255f, 7f / 255f, 18f / 255f, 0.96f), 14f);
        AddFrame(start, Gold);
        Button startButton = start.gameObject.AddComponent<Button>();
        startButton.targetGraphic = startImage;
        startButton.onClick.AddListener(() => _onStart?.Invoke());
        MakeText(start, "StartChallengeLabel", "START CHALLENGE", 46f, Gold, FontStyles.Bold);
    }

    private void LayoutPortrait()
    {
        Place(Find(_safeArea, "HomeButton"), 0.085f, 0.95f, 78f, 78f);
        Place(Find(_safeArea, "Title"), 0.5f, 0.75f, 820f, 90f);
        Place(Find(_safeArea, "TitleRuleLeft"), 0.13f, 0.75f, 100f, 4f);
        Place(Find(_safeArea, "TitleRuleRight"), 0.87f, 0.75f, 100f, 4f);
        Place(Find(_safeArea, "Date"), 0.5f, 0.68f, 780f, 62f);

        RectTransform challenge = Find(_safeArea, "ChallengeCard");
        RectTransform stats = Find(_safeArea, "StatsCard");
        RectTransform start = Find(_safeArea, "StartChallengeButton");
        Place(challenge, 0.5f, 0.57f, 850f, 340f);
        Place(stats, 0.5f, 0.405f, 850f, 220f);
        Place(start, 0.5f, 0.27f, 850f, 140f);

        Place(Find(challenge, "ChallengeNumber"), 0.5f, 0.77f, 740f, 72f);
        Place(Find(challenge, "PuzzleLabel"), 0.5f, 0.51f, 740f, 58f);
        Place(Find(challenge, "DifficultyBadge"), 0.5f, 0.23f, 340f, 72f);

        LayoutStats(stats, 280f, 30f, 42f, 52f);
        Place(Find(start, "StartChallengeLabel"), 0.5f, 0.5f, 740f, 90f);
    }

    private void LayoutLandscape()
    {
        Place(Find(_safeArea, "HomeButton"), 0.06f, 0.92f, 70f, 70f);
        Place(Find(_safeArea, "Title"), 0.5f, 0.82f, 1000f, 82f);
        Place(Find(_safeArea, "TitleRuleLeft"), 0.13f, 0.82f, 100f, 4f);
        Place(Find(_safeArea, "TitleRuleRight"), 0.87f, 0.82f, 100f, 4f);
        Place(Find(_safeArea, "Date"), 0.5f, 0.72f, 760f, 54f);

        RectTransform challenge = Find(_safeArea, "ChallengeCard");
        RectTransform stats = Find(_safeArea, "StatsCard");
        RectTransform start = Find(_safeArea, "StartChallengeButton");
        Place(challenge, 0.25f, 0.49f, 900f, 340f);
        Place(stats, 0.75f, 0.49f, 650f, 220f);
        Place(start, 0.5f, 0.14f, 1100f, 140f);

        Place(Find(challenge, "ChallengeNumber"), 0.5f, 0.77f, 870f, 72f);
        Place(Find(challenge, "PuzzleLabel"), 0.5f, 0.51f, 870f, 58f);
        Place(Find(challenge, "DifficultyBadge"), 0.5f, 0.23f, 340f, 72f);

        LayoutStats(stats, 220f, 26f, 36f, 44f);
        Place(Find(start, "StartChallengeLabel"), 0.5f, 0.5f, 1040f, 88f);
    }

    private void LayoutStats(RectTransform stats, float labelWidth, float labelSize,
        float valueSize, float iconSize)
    {
        Place(Find(stats, "ClockIcon"), 0.11f, 0.58f, iconSize, iconSize);
        Place(Find(stats, "BestTimeLabel"), 0.34f, 0.58f, labelWidth, 46f);
        Place(Find(stats, "BestTimeValue"), 0.34f, 0.27f, labelWidth, 52f);
        Place(Find(stats, "StreakIcon"), 0.61f, 0.58f, iconSize, iconSize);
        Place(Find(stats, "StreakLabel"), 0.82f, 0.58f, labelWidth, 46f);
        Place(Find(stats, "StreakValue"), 0.82f, 0.27f, labelWidth, 52f);
        Place(Find(stats, "StatsDivider"), 0.5f, 0.5f, 2f, stats.sizeDelta.y * 0.68f);

        Find(stats, "BestTimeLabel").GetComponent<TextMeshProUGUI>().fontSize = labelSize;
        Find(stats, "StreakLabel").GetComponent<TextMeshProUGUI>().fontSize = labelSize;
        Find(stats, "BestTimeValue").GetComponent<TextMeshProUGUI>().fontSize = valueSize;
        Find(stats, "StreakValue").GetComponent<TextMeshProUGUI>().fontSize = valueSize;
    }

    private void AddClock(RectTransform parent)
    {
        RectTransform clock = MakeRect("ClockIcon", parent);
        const int tickCount = 12;
        for (int i = 0; i < tickCount; i++)
        {
            float angle = i * Mathf.PI * 2f / tickCount;
            RectTransform tick = MakeRect("Tick", clock);
            Center(tick, Mathf.Sin(angle) * 21f, Mathf.Cos(angle) * 21f, 5f, 5f);
            tick.gameObject.AddComponent<Image>().color = Gold;
        }

        RectTransform hand = MakeRect("ClockHand", clock);
        Center(hand, 2f, 3f, 4f, 20f);
        hand.gameObject.AddComponent<Image>().color = Gold;
        RectTransform shortHand = MakeRect("ShortHand", clock);
        Center(shortHand, -3f, -3f, 4f, 15f);
        shortHand.localRotation = Quaternion.Euler(0f, 0f, -45f);
        shortHand.gameObject.AddComponent<Image>().color = Gold;
    }

    private RectTransform MakeCard(string name, RectTransform parent)
    {
        RectTransform panel = MakeRect(name, parent);
        RectTransform fill = MakeRect("PanelFill", panel);
        Stretch(fill);
        fill.offsetMin = new Vector2(10f, 10f);
        fill.offsetMax = new Vector2(-10f, -10f);
        fill.gameObject.AddComponent<Image>().color = PanelNavy;
        AddFrame(panel, Gold);
        return panel;
    }

    private void AddFrame(RectTransform parent, Color color)
    {
        RectTransform frame = MakeRect("Outline", parent);
        Stretch(frame);
        Image image = frame.gameObject.AddComponent<Image>();
        image.sprite = _frameSprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = color;
        image.raycastTarget = false;
    }

    private static void AddInsetFill(RectTransform parent, Color color, float inset)
    {
        RectTransform fill = MakeRect("Fill", parent);
        Stretch(fill);
        fill.offsetMin = new Vector2(inset, inset);
        fill.offsetMax = new Vector2(-inset, -inset);
        Image image = fill.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private TextMeshProUGUI MakeText(RectTransform parent, string name, string value,
        float fontSize, Color color, FontStyles style)
    {
        RectTransform rect = MakeRect(name, parent);
        Place(rect, 0.5f, 0.5f, 700f, 64f);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = _font;
        text.text = value;
        text.fontSize = fontSize;
        text.fontSizeMin = Mathf.Max(16f, fontSize * 0.65f);
        text.fontSizeMax = fontSize;
        text.enableAutoSizing = true;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = style;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private void AddImage(RectTransform parent, string name, Color color,
        float anchorX, float anchorY, float width, float height)
    {
        RectTransform rect = MakeRect(name, parent);
        Place(rect, anchorX, anchorY, width, height);
        rect.gameObject.AddComponent<Image>().color = color;
    }

    private static Button MakeButton(string name, RectTransform parent, Action action)
    {
        RectTransform rect = MakeRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => action?.Invoke());
        return button;
    }

    private static RectTransform MakeRect(string name, RectTransform parent)
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

    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(x, y);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void Center(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
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

    private static Color Hex(string value)
    {
        ColorUtility.TryParseHtmlString("#" + value, out Color color);
        return color;
    }
}
