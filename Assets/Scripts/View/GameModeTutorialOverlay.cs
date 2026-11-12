using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Introduces New Game, Journey, and Daily Challenge after the first variant selection.</summary>
public sealed class GameModeTutorialOverlay : MonoBehaviour
{
    public const string SeenPreferenceKey = "GameModeTutorialSeen";
    private static bool shownThisSession;

    private static readonly Color PanelColor = new Color(0.015f, 0.07f, 0.12f, 0.98f);
    private static readonly Color GoldColor = new Color(1f, 0.72f, 0.16f, 1f);

    private HomeScreenController homeScreen;
    private RectTransform root;
    private RectTransform[] targets;
    private RectTransform highlight;
    private RectTransform panel;
    private TextMeshProUGUI title;
    private TextMeshProUGUI description;
    private TextMeshProUGUI skipLabel;
    private TextMeshProUGUI nextLabel;
    private int step;
    private bool closing;
    private bool lastLandscape;

    public static void ShowOnce(HomeScreenController targetHomeScreen)
    {
        if (shownThisSession || targetHomeScreen == null ||
            PlayerPrefs.GetInt(SeenPreferenceKey, 0) == 1)
            return;

        shownThisSession = true;
        var overlayObject = new GameObject("GameModeTutorialOverlay", typeof(RectTransform));
        overlayObject.transform.SetParent(targetHomeScreen.transform, false);
        overlayObject.transform.SetAsLastSibling();
        overlayObject.AddComponent<GameModeTutorialOverlay>().Initialize(targetHomeScreen);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSessionState() => shownThisSession = false;

    private void Initialize(HomeScreenController targetHomeScreen)
    {
        homeScreen = targetHomeScreen;
        root = (RectTransform)transform;
        targets = new[]
        {
            homeScreen.NewGameTutorialTarget,
            homeScreen.JourneyTutorialTarget,
            homeScreen.DailyChallengeTutorialTarget
        };

        if (targets[0] == null || targets[1] == null || targets[2] == null)
        {
            Debug.LogWarning("The game mode tutorial could not find all three Home screen cards.", this);
            Destroy(gameObject);
            return;
        }

        BuildInterface();
        RenderStep();
    }

    private void BuildInterface()
    {
        Stretch(root);
        Image backdrop = gameObject.AddComponent<Image>();
        backdrop.color = new Color(0.005f, 0.012f, 0.025f, 0.62f);
        backdrop.raycastTarget = true;

        highlight = CreateRect("ModeCardHighlight", root);
        AddGlowFrame(highlight);

        panel = CreateRect("GameModeTutorialCard", root);
        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = PanelColor;
        panelImage.raycastTarget = false;

        title = CreateText("GameModeTitle", panel, 54f, GoldColor,
            TextAlignmentOptions.Center);
        description = CreateText("GameModeDescription", panel, 38f, Color.white,
            TextAlignmentOptions.Center);

        Button skip = CreateButton("SkipGameModeTutorial", panel, "SKIP",
            new Color(0.08f, 0.14f, 0.22f, 1f));
        skipLabel = skip.GetComponentInChildren<TextMeshProUGUI>();
        SetAnchors(skip.GetComponent<RectTransform>(), new Vector2(0.05f, 0.04f),
            new Vector2(0.40f, 0.20f));
        skip.onClick.AddListener(() => Close(true));

        Button next = CreateButton("NextGameModeTutorial", panel, "NEXT", GoldColor);
        SetAnchors(next.GetComponent<RectTransform>(), new Vector2(0.44f, 0.04f),
            new Vector2(0.95f, 0.20f));
        next.onClick.AddListener(Advance);
        nextLabel = next.GetComponentInChildren<TextMeshProUGUI>();

        SetAnchors(title.rectTransform, new Vector2(0.05f, 0.70f),
            new Vector2(0.95f, 0.96f));
        SetAnchors(description.rectTransform, new Vector2(0.08f, 0.23f),
            new Vector2(0.92f, 0.68f));
    }

    private void RenderStep()
    {
        bool landscape = Screen.width > Screen.height;
        lastLandscape = landscape;
        if (landscape)
        {
            SetAnchors(panel, step == 0
                    ? new Vector2(0.03f, 0.18f)
                    : (step == 2 ? new Vector2(0.66f, 0.18f) : new Vector2(0.03f, 0.18f)),
                step == 2 ? new Vector2(0.97f, 0.88f) : new Vector2(0.34f, 0.88f));
            SetFontSize(title, 78f);
            SetFontSize(description, 68f);
            SetFontSize(skipLabel, 46f);
            SetFontSize(nextLabel, 46f);
            SetAnchors(description.rectTransform, new Vector2(0.07f, 0.22f),
                new Vector2(0.93f, 0.71f));
        }
        else
        {
            SetAnchors(panel, new Vector2(0.06f, 0.68f), new Vector2(0.94f, 0.96f));
            SetFontSize(title, 48f);
            SetFontSize(description, 34f);
            SetFontSize(skipLabel, 34f);
            SetFontSize(nextLabel, 34f);
            SetAnchors(description.rectTransform, new Vector2(0.08f, 0.23f),
                new Vector2(0.92f, 0.68f));
        }

        CopyRect(targets[step], highlight);
        highlight.SetAsLastSibling();
        panel.SetAsLastSibling();

        switch (step)
        {
            case 0:
                title.text = "NEW GAME";
                description.text = "Start a standalone puzzle. Choose a difficulty and play at your own pace.";
                break;
            case 1:
                title.text = "SUDOKU JOURNEY";
                description.text = "Progress through puzzles, earn points, and continue from your current level.";
                break;
            default:
                title.text = "DAILY CHALLENGE";
                description.text = "Play the daily puzzle, earn stars, and build your challenge streak.";
                break;
        }

        nextLabel.text = step == 2 ? "START PLAYING" : "NEXT";
    }

    private void Advance()
    {
        if (step < 2)
        {
            step++;
            RenderStep();
            return;
        }

        Close(true);
    }

    private void Update()
    {
        bool landscape = Screen.width > Screen.height;
        if (landscape != lastLandscape)
            RenderStep();
    }

    private void Close(bool markSeen)
    {
        if (closing) return;
        closing = true;
        if (markSeen)
        {
            PlayerPrefs.SetInt(SeenPreferenceKey, 1);
            PlayerPrefs.Save();
        }

        Destroy(gameObject);
    }

    private static Button CreateButton(string name, Transform parent, string label, Color color)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.GetComponent<Image>();
        image.color = color;
        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.16f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.15f);
        button.colors = colors;

        var textObject = new GameObject("Label", typeof(RectTransform));
        textObject.transform.SetParent(buttonObject.transform, false);
        var text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 34f;
        text.fontSizeMin = 22f;
        text.fontSizeMax = 34f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        Stretch((RectTransform)text.transform);
        return button;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, float fontSize,
        Color color, TextAlignmentOptions alignment)
    {
        var textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        var text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontSizeMin = fontSize * 0.65f;
        text.fontSizeMax = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return (RectTransform)child.transform;
    }

    private static void AddGlowFrame(RectTransform frame)
    {
        Color glowColor = new Color(GoldColor.r, GoldColor.g, GoldColor.b, 0.24f);
        AddFrameEdge(frame, "TopGlow", new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(-12f, -12f), new Vector2(12f, 12f), glowColor);
        AddFrameEdge(frame, "BottomGlow", Vector2.zero, new Vector2(1f, 0f),
            new Vector2(-12f, -12f), new Vector2(12f, 12f), glowColor);
        AddFrameEdge(frame, "LeftGlow", Vector2.zero, new Vector2(0f, 1f),
            new Vector2(-12f, -12f), new Vector2(12f, 12f), glowColor);
        AddFrameEdge(frame, "RightGlow", new Vector2(1f, 0f), Vector2.one,
            new Vector2(-12f, -12f), new Vector2(12f, 12f), glowColor);

        AddFrameEdge(frame, "TopBorder", new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(-8f, -2f), new Vector2(8f, 2f), GoldColor);
        AddFrameEdge(frame, "BottomBorder", Vector2.zero, new Vector2(1f, 0f),
            new Vector2(-8f, -2f), new Vector2(8f, 2f), GoldColor);
        AddFrameEdge(frame, "LeftBorder", Vector2.zero, new Vector2(0f, 1f),
            new Vector2(-2f, -8f), new Vector2(2f, 8f), GoldColor);
        AddFrameEdge(frame, "RightBorder", new Vector2(1f, 0f), Vector2.one,
            new Vector2(-2f, -8f), new Vector2(2f, 8f), GoldColor);
    }

    private static void AddFrameEdge(RectTransform frame, string name, Vector2 anchorMin,
        Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        RectTransform edge = CreateRect(name, frame);
        edge.anchorMin = anchorMin;
        edge.anchorMax = anchorMax;
        edge.offsetMin = offsetMin;
        edge.offsetMax = offsetMax;
        Image image = edge.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private static void CopyRect(RectTransform source, RectTransform destination)
    {
        destination.anchorMin = source.anchorMin;
        destination.anchorMax = source.anchorMax;
        destination.pivot = source.pivot;
        destination.anchoredPosition = source.anchoredPosition;
        destination.sizeDelta = source.sizeDelta;
        destination.offsetMin = source.offsetMin;
        destination.offsetMax = source.offsetMax;
        destination.localRotation = source.localRotation;
        destination.localScale = source.localScale;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetFontSize(TextMeshProUGUI text, float size)
    {
        text.fontSize = size;
        text.fontSizeMin = size * 0.65f;
        text.fontSizeMax = size;
    }
}
