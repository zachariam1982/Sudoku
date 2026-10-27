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

    [Header("Scene UI")]
    [SerializeField] private RectTransform overlay;
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private RectTransform summaryCard;
    [SerializeField] private RectTransform personalBestCard;
    [SerializeField] private RectTransform bestTimesCard;
    [SerializeField] private RectTransform playedCard;
    [SerializeField] private TextMeshProUGUI[] summaryLabels;
    [SerializeField] private Image[] summaryIcons;
    [SerializeField] private TextMeshProUGUI personalBest;
    [SerializeField] private Image personalBestIcon;
    [SerializeField] private TextMeshProUGUI played;
    [SerializeField] private TextMeshProUGUI difficultyHeading;
    [SerializeField] private TextMeshProUGUI[] difficultyNames;
    [SerializeField] private TextMeshProUGUI[] difficultyDates;
    [SerializeField] private TextMeshProUGUI[] difficultyTimes;

    [SerializeField] private Sprite currentStreakIcon;
    [SerializeField] private Sprite bestStreakIcon;
    [SerializeField] private Sprite gamesWonIcon;
    [SerializeField] private Sprite bestTimeIcon;

    private readonly Color _gold = new Color(1f, .76f, .30f, 1f);
    private readonly Color[] _difficultyColors =
    {
        new Color(.72f, .89f, .42f), new Color(.38f, .72f, 1f),
        new Color(1f, .78f, .34f), new Color(.76f, .48f, 1f),
        new Color(.34f, .86f, .80f), new Color(1f, .62f, .32f),
        new Color(.98f, .45f, .45f), new Color(.83f, .53f, 1f),
        new Color(.94f, .86f, .52f)
    };

    private VerseTicker _verseTicker;
    private bool _open;
    private bool _toolsWereActive;
    private bool _topBarWasActive;
    private bool _tickerWasActive;
    private bool _lastLandscape;

    private void Awake()
    {
        if (summaryIcons != null)
        {
            Sprite[] icons = { currentStreakIcon, bestStreakIcon, gamesWonIcon };
            for (int i = 0; i < summaryIcons.Length && i < icons.Length; i++)
                if (summaryIcons[i] != null) summaryIcons[i].sprite = icons[i];
        }
        if (personalBestIcon != null) personalBestIcon.sprite = bestTimeIcon;
        ApplyTextStyle(title, _gold, TextAlignmentOptions.Center);
        ApplyTextStyle(personalBest, Color.white, TextAlignmentOptions.Left);
        ApplyTextStyle(played, _gold, TextAlignmentOptions.Center);
        ApplyTextStyle(difficultyHeading, _gold, TextAlignmentOptions.Center);
        for (int i = 0; i < summaryLabels.Length; i++)
            ApplyTextStyle(summaryLabels[i], Color.white, TextAlignmentOptions.Center);
        for (int i = 0; i < difficultyDates.Length; i++)
            ApplyTextStyle(difficultyDates[i], new Color(.81f, .84f, .9f), TextAlignmentOptions.Center);
        for (int i = 0; i < difficultyNames.Length; i++)
            ApplyTextStyle(difficultyNames[i], _difficultyColors[i], TextAlignmentOptions.Left);
        for (int i = 0; i < difficultyTimes.Length; i++)
            ApplyTextStyle(difficultyTimes[i], Color.white, TextAlignmentOptions.Right);

        _lastLandscape = Screen.width > Screen.height;
        ApplyLayout(_lastLandscape);
        if (overlay != null) overlay.gameObject.SetActive(false);
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
        if (overlay != null) overlay.gameObject.SetActive(false);
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
        if (overlay != null)
        {
            overlay.gameObject.SetActive(true);
            overlay.SetAsLastSibling();
        }
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

        summaryLabels[0].text = Metric("CURRENT STREAK", stats.CurrentStreak, "days");
        summaryLabels[1].text = Metric("BEST STREAK", GetBestStreak(records), "days");
        summaryLabels[2].text = Metric("GAMES WON", stats.GamesWon, "");

        if (bestOverall == null)
        {
            personalBest.text = "PERSONAL BEST\n<size=115%>--:--</size>\nComplete a challenge to set a record";
        }
        else
        {
            personalBest.text = "PERSONAL BEST\n<size=145%>" + FormatTime(bestOverall.ElapsedSeconds) +
                "</size>   " + (SudokuDifficulty)bestOverall.Difficulty + "\n" +
                bestOverall.CurrentDate.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture);
        }

        played.text = "CHALLENGES PLAYED     <b>" + stats.GamesPlayed.ToString(CultureInfo.InvariantCulture) + "</b>";
        for (int difficulty = 0; difficulty < difficultyNames.Length; difficulty++)
        {
            DailyChallengeRecord best = records
                .Where(record => record.IsWon && record.Difficulty == difficulty)
                .OrderBy(record => record.ElapsedSeconds)
                .FirstOrDefault();
            difficultyNames[difficulty].text = ((SudokuDifficulty)difficulty).ToString();
            difficultyNames[difficulty].color = _difficultyColors[difficulty];
            difficultyDates[difficulty].text = best == null
                ? "No win yet"
                : best.CurrentDate.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture);
            difficultyTimes[difficulty].text = best == null ? "--:--" : FormatTime(best.ElapsedSeconds);
        }
    }

    private static string Metric(string label, int value, string suffix)
    {
        string number = value.ToString(CultureInfo.InvariantCulture);
        if (!string.IsNullOrEmpty(suffix)) number += " " + suffix;
        return "<size=82%>" + label + "</size>\n<size=145%><b>" + number + "</b></size>";
    }

    private static string FormatTime(float seconds)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
        return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", total / 60, total % 60);
    }

    private static void ApplyTextStyle(
        TextMeshProUGUI label,
        Color color,
        TextAlignmentOptions alignment)
    {
        if (label == null) return;
        label.color = color;
        label.alignment = alignment;
        label.raycastTarget = false;
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

    private void ApplyLayout(bool landscape)
    {
        if (overlay == null || background == null) return;
        background.sprite = landscape ? landscapeBackground : portraitBackground;
        if (backButton != null)
            Place(backButton.transform as RectTransform, new Vector2(.025f, .905f), new Vector2(.12f, .985f));
        Place(title.rectTransform, landscape ? new Vector2(.15f, .89f) : new Vector2(.14f, .90f),
            landscape ? new Vector2(.85f, .985f) : new Vector2(.92f, .98f));

        if (landscape)
        {
            Place(summaryCard, new Vector2(.035f, .59f), new Vector2(.49f, .85f));
            Place(personalBestCard, new Vector2(.035f, .31f), new Vector2(.49f, .56f));
            Place(playedCard, new Vector2(.035f, .18f), new Vector2(.49f, .28f));
            Place(bestTimesCard, new Vector2(.52f, .18f), new Vector2(.965f, .85f));
        }
        else
        {
            Place(summaryCard, new Vector2(.04f, .72f), new Vector2(.96f, .865f));
            Place(personalBestCard, new Vector2(.04f, .55f), new Vector2(.96f, .695f));
            Place(bestTimesCard, new Vector2(.04f, .13f), new Vector2(.96f, .52f));
            Place(playedCard, new Vector2(.04f, .045f), new Vector2(.96f, .11f));
        }

        for (int i = 0; i < 3; i++)
        {
            float left = i / 3f;
            float right = (i + 1) / 3f;
            float iconCenter = (left + right) * .5f;
            Place(summaryIcons[i].rectTransform, new Vector2(iconCenter - .07f, .62f), new Vector2(iconCenter + .07f, .94f));
            Place(summaryLabels[i].rectTransform, new Vector2(left + .01f, .07f), new Vector2(right - .01f, .60f));
            summaryLabels[i].fontSize = landscape ? 31 : 33;
        }

        Place(personalBestIcon.rectTransform, new Vector2(.035f, .18f), new Vector2(.23f, .82f));
        Place(personalBest.rectTransform, new Vector2(.25f, .05f), new Vector2(.965f, .95f));
        personalBest.fontSize = landscape ? 36 : 35;
        Place(played.rectTransform, new Vector2(.04f, .08f), new Vector2(.96f, .92f));
        played.fontSize = landscape ? 31 : 30;

        for (int i = 0; i < 9; i++)
        {
            float top = .86f - i * .092f;
            float bottom = top - .078f;
            Place(difficultyNames[i].rectTransform, new Vector2(.045f, bottom), new Vector2(.39f, top));
            Place(difficultyDates[i].rectTransform, new Vector2(.39f, bottom), new Vector2(.72f, top));
            Place(difficultyTimes[i].rectTransform, new Vector2(.72f, bottom), new Vector2(.96f, top));
            difficultyNames[i].fontSize = landscape ? 25 : 24;
            difficultyDates[i].fontSize = landscape ? 21 : 20;
            difficultyTimes[i].fontSize = landscape ? 27 : 24;
        }

        Place(difficultyHeading.rectTransform, new Vector2(.04f, .88f), new Vector2(.96f, .98f));
        difficultyHeading.fontSize = landscape ? 27 : 25;
    }

    private static void Place(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
