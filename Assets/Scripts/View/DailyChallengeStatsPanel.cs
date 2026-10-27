using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Independent Daily Challenge statistics screen opened from its own top bar.</summary>
public sealed class DailyChallengeStatsPanel : MonoBehaviour
{
    [SerializeField] private Sprite portraitBackground;
    [SerializeField] private Sprite landscapeBackground;
    [SerializeField] private GameObject gameplayTools;
    [SerializeField] private GameObject dailyChallengeTopBar;
    [SerializeField] private Button backButton;

    private readonly Color _gold = new Color(1f, .76f, .30f, 1f);
    private readonly Color _panel = new Color(.018f, .055f, .105f, .96f);
    private readonly Color[] _difficultyColors =
    {
        new Color(.72f, .89f, .42f), new Color(.38f, .72f, 1f),
        new Color(1f, .78f, .34f), new Color(.76f, .48f, 1f),
        new Color(.34f, .86f, .80f), new Color(1f, .62f, .32f),
        new Color(.98f, .45f, .45f), new Color(.83f, .53f, 1f),
        new Color(.94f, .86f, .52f)
    };

    private RectTransform _overlay;
    private Image _background;
    private TextMeshProUGUI _title;
    private readonly TextMeshProUGUI[] _summary = new TextMeshProUGUI[3];
    private TextMeshProUGUI _personalBest;
    private TextMeshProUGUI _played;
    private TextMeshProUGUI _difficultyHeading;
    private TextMeshProUGUI[] _difficultyNames;
    private TextMeshProUGUI[] _difficultyDates;
    private TextMeshProUGUI[] _difficultyTimes;
    private VerseTicker _verseTicker;
    private bool _open;
    private bool _toolsWereActive;
    private bool _topBarWasActive;
    private bool _tickerWasActive;
    private bool _lastLandscape;

    private void Awake()
    {
        BuildScreen();
        _lastLandscape = Screen.width > Screen.height;
        ApplyLayout(_lastLandscape);
        _overlay.gameObject.SetActive(false);
        if (backButton != null) backButton.gameObject.SetActive(false);
    }

    private void Update()
    {
        bool landscape = Screen.width > Screen.height;
        if (landscape == _lastLandscape) return;
        _lastLandscape = landscape;
        ApplyLayout(landscape);
        if (_open) Refresh();
    }

    public void TogglePanel()
    {
        if (_open) ClosePanel();
        else OpenPanel();
    }

    public void ClosePanel()
    {
        if (!_open) return;
        _open = false;
        _overlay.gameObject.SetActive(false);
        if (backButton != null) backButton.gameObject.SetActive(false);
        if (gameplayTools != null) gameplayTools.SetActive(_toolsWereActive);
        if (dailyChallengeTopBar != null) dailyChallengeTopBar.SetActive(_topBarWasActive);
        if (_verseTicker != null) _verseTicker.SetGameActive(_tickerWasActive);
    }

    private void OpenPanel()
    {
        _open = true;
        _toolsWereActive = gameplayTools != null && gameplayTools.activeSelf;
        _topBarWasActive = dailyChallengeTopBar != null && dailyChallengeTopBar.activeSelf;
        if (gameplayTools != null) gameplayTools.SetActive(false);
        if (dailyChallengeTopBar != null) dailyChallengeTopBar.SetActive(false);

        Canvas canvas = GetComponentInParent<Canvas>();
        if (_verseTicker == null && canvas != null)
            _verseTicker = canvas.GetComponent<VerseTicker>();
        _tickerWasActive = _verseTicker != null && _verseTicker.IsGameActive;
        if (_verseTicker != null) _verseTicker.SetGameActive(false);

        Refresh();
        _overlay.gameObject.SetActive(true);
        _overlay.SetAsLastSibling();
        if (backButton != null)
        {
            backButton.gameObject.SetActive(true);
            backButton.transform.SetAsLastSibling();
        }
    }

    private void Refresh()
    {
        List<DailyChallengeRecord> records = DailyChallengeRepository.GetAllRecords();
        DailyChallengeStatsSnapshot stats = DailyChallengeRepository.GetStatsSnapshot();
        DailyChallengeRecord bestOverall = records
            .Where(record => record.IsWon)
            .OrderBy(record => record.ElapsedSeconds)
            .FirstOrDefault(record => !stats.BestTimeGameId.HasValue || record.Id == stats.BestTimeGameId.Value)
            ?? records.Where(record => record.IsWon).OrderBy(record => record.ElapsedSeconds).FirstOrDefault();

        _summary[0].text = Metric("CURRENT STREAK", stats.CurrentStreak, "days");
        _summary[1].text = Metric("BEST STREAK", GetBestStreak(records), "days");
        _summary[2].text = Metric("GAMES WON", stats.GamesWon, "");

        if (bestOverall == null)
        {
            _personalBest.text = "PERSONAL BEST\n<size=115%>--:--</size>\nComplete a challenge to set a record";
        }
        else
        {
            _personalBest.text = "PERSONAL BEST\n<size=145%>" + FormatTime(bestOverall.ElapsedSeconds) +
                "</size>   " + (SudokuDifficulty)bestOverall.Difficulty + "\n" +
                bestOverall.CurrentDate.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture);
        }

        _played.text = "CHALLENGES PLAYED     <b>" + stats.GamesPlayed.ToString(CultureInfo.InvariantCulture) + "</b>";
        for (int difficulty = 0; difficulty < _difficultyNames.Length; difficulty++)
        {
            DailyChallengeRecord best = records
                .Where(record => record.IsWon && record.Difficulty == difficulty)
                .OrderBy(record => record.ElapsedSeconds)
                .FirstOrDefault();
            _difficultyNames[difficulty].text = ((SudokuDifficulty)difficulty).ToString();
            _difficultyNames[difficulty].color = _difficultyColors[difficulty];
            _difficultyDates[difficulty].text = best == null
                ? "No win yet"
                : best.CurrentDate.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture);
            _difficultyTimes[difficulty].text = best == null ? "--:--" : FormatTime(best.ElapsedSeconds);
        }
    }

    private static string Metric(string label, int value, string suffix)
    {
        string number = value.ToString(CultureInfo.InvariantCulture);
        if (!string.IsNullOrEmpty(suffix)) number += " " + suffix;
        return "<color=#F7C96B>✦</color>\n<size=82%>" + label + "</size>\n<size=145%><b>" + number + "</b></size>";
    }

    private static string FormatTime(float seconds)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
        return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", total / 60, total % 60);
    }

    private static int GetBestStreak(List<DailyChallengeRecord> records)
    {
        List<DateTime> wins = records.Where(record => record.IsWon)
            .Select(record => record.CurrentDate.Date).Distinct().OrderBy(date => date).ToList();
        int best = 0;
        int current = 0;
        DateTime previous = DateTime.MinValue;
        foreach (DateTime date in wins)
        {
            current = previous != DateTime.MinValue && date == previous.AddDays(1) ? current + 1 : 1;
            if (current > best) best = current;
            previous = date;
        }
        return best;
    }

    private void BuildScreen()
    {
        _overlay = MakeRect("DailyChallengeStatsOverlay", transform as RectTransform);
        Stretch(_overlay);
        Image overlayImage = _overlay.gameObject.AddComponent<Image>();
        overlayImage.color = Color.black;
        overlayImage.raycastTarget = true;

        _background = MakeImage("Background", _overlay, Color.white);
        Stretch(_background.rectTransform);
        _background.preserveAspect = false;

        Image shade = MakeImage("NightTint", _overlay, new Color(.005f, .025f, .07f, .62f));
        Stretch(shade.rectTransform);
        shade.raycastTarget = false;

        Transform summaryContent = FindOrCreateCard("SummaryCard").Find("Inner");
        Transform bestContent = FindOrCreateCard("PersonalBestCard").Find("Inner");
        Transform difficultyContent = FindOrCreateCard("BestTimesCard").Find("Inner");
        Transform playedContent = FindOrCreateCard("PlayedCard").Find("Inner");

        _title = MakeText("Title", _overlay, "DAILY CHALLENGE STATS", 50, _gold, TextAlignmentOptions.Center);
        for (int i = 0; i < _summary.Length; i++)
            _summary[i] = MakeText("Summary " + i, summaryContent, string.Empty, 34, Color.white, TextAlignmentOptions.Center);
        _personalBest = MakeText("Personal Best", bestContent, string.Empty, 39, Color.white, TextAlignmentOptions.Center);
        _played = MakeText("Challenges Played", playedContent, string.Empty, 34, _gold, TextAlignmentOptions.Center);
        _difficultyHeading = MakeText("BestTimesHeading", difficultyContent, "BEST TIMES BY DIFFICULTY", 29, _gold, TextAlignmentOptions.Center);

        _difficultyNames = new TextMeshProUGUI[9];
        _difficultyDates = new TextMeshProUGUI[9];
        _difficultyTimes = new TextMeshProUGUI[9];
        for (int i = 0; i < 9; i++)
        {
            _difficultyNames[i] = MakeText("Difficulty " + i, difficultyContent, string.Empty, 27, Color.white, TextAlignmentOptions.Left);
            _difficultyDates[i] = MakeText("Record Date " + i, difficultyContent, string.Empty, 22, new Color(.81f, .84f, .9f), TextAlignmentOptions.Center);
            _difficultyTimes[i] = MakeText("Record Time " + i, difficultyContent, string.Empty, 29, Color.white, TextAlignmentOptions.Right);
        }
    }

    private void ApplyLayout(bool landscape)
    {
        if (_overlay == null) return;
        _background.sprite = landscape ? landscapeBackground : portraitBackground;
        if (backButton != null)
            Place(backButton.transform as RectTransform, new Vector2(.025f, .905f), new Vector2(.12f, .985f));
        Place(_title.rectTransform, landscape ? new Vector2(.15f, .89f) : new Vector2(.14f, .90f),
            landscape ? new Vector2(.85f, .985f) : new Vector2(.92f, .98f));

        RectTransform summaryCard = FindOrCreateCard("SummaryCard");
        RectTransform bestCard = FindOrCreateCard("PersonalBestCard");
        RectTransform difficultyCard = FindOrCreateCard("BestTimesCard");
        RectTransform playedCard = FindOrCreateCard("PlayedCard");
        if (landscape)
        {
            Place(summaryCard, new Vector2(.035f, .59f), new Vector2(.49f, .85f));
            Place(bestCard, new Vector2(.035f, .31f), new Vector2(.49f, .56f));
            Place(playedCard, new Vector2(.035f, .18f), new Vector2(.49f, .28f));
            Place(difficultyCard, new Vector2(.52f, .18f), new Vector2(.965f, .85f));
        }
        else
        {
            Place(summaryCard, new Vector2(.04f, .72f), new Vector2(.96f, .865f));
            Place(bestCard, new Vector2(.04f, .55f), new Vector2(.96f, .695f));
            Place(difficultyCard, new Vector2(.04f, .13f), new Vector2(.96f, .52f));
            Place(playedCard, new Vector2(.04f, .045f), new Vector2(.96f, .11f));
        }

        for (int i = 0; i < 3; i++)
        {
            float left = i / 3f;
            float right = (i + 1) / 3f;
            Place(_summary[i].rectTransform, new Vector2(left + .01f, .08f), new Vector2(right - .01f, .93f));
            _summary[i].fontSize = landscape ? 31 : 33;
        }

        Place(_personalBest.rectTransform, new Vector2(.035f, .05f), new Vector2(.965f, .95f));
        _personalBest.fontSize = landscape ? 36 : 35;
        Place(_played.rectTransform, new Vector2(.04f, .08f), new Vector2(.96f, .92f));
        _played.fontSize = landscape ? 31 : 30;

        for (int i = 0; i < 9; i++)
        {
            float top = .86f - i * .092f;
            float bottom = top - .078f;
            Place(_difficultyNames[i].rectTransform, new Vector2(.045f, bottom), new Vector2(.39f, top));
            Place(_difficultyDates[i].rectTransform, new Vector2(.39f, bottom), new Vector2(.72f, top));
            Place(_difficultyTimes[i].rectTransform, new Vector2(.72f, bottom), new Vector2(.96f, top));
            _difficultyNames[i].fontSize = landscape ? 25 : 24;
            _difficultyDates[i].fontSize = landscape ? 21 : 20;
            _difficultyTimes[i].fontSize = landscape ? 27 : 24;
        }

        // Add a small section heading as the first row above the records.
        Place(_difficultyHeading.rectTransform, new Vector2(.04f, .88f), new Vector2(.96f, .98f));
        _difficultyHeading.fontSize = landscape ? 27 : 25;
    }

    private RectTransform FindOrCreateCard(string name)
    {
        Transform existing = _overlay.Find(name);
        if (existing != null) return existing as RectTransform;
        RectTransform card = MakeRect(name, _overlay);
        Image border = card.gameObject.AddComponent<Image>();
        border.color = _gold;
        RectTransform inner = MakeRect("Inner", card);
        inner.anchorMin = Vector2.zero;
        inner.anchorMax = Vector2.one;
        inner.offsetMin = new Vector2(2f, 2f);
        inner.offsetMax = new Vector2(-2f, -2f);
        Image fill = inner.gameObject.AddComponent<Image>();
        fill.color = _panel;
        fill.raycastTarget = false;
        return card;
    }

    private TextMeshProUGUI MakeText(string name, Transform parent, string value, float size, Color color, TextAlignmentOptions alignment)
    {
        RectTransform rect = MakeRect(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        return text;
    }

    private static Image MakeImage(string name, Transform parent, Color color)
    {
        RectTransform rect = MakeRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static RectTransform MakeRect(string name, Transform parent)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void Place(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
