using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Independent Daily Challenge statistics screen opened from its own top bar.</summary>
public sealed class DailyChallengeStatsPanel : MonoBehaviour
{
    private DailyChallengeViewModel _viewModel;
    [SerializeField] private Sprite portraitBackground;
    [SerializeField] private Sprite landscapeBackground;
    [SerializeField] private GameObject gameplayTools;
    [SerializeField] private GameObject dailyChallengeTopBar;
    [SerializeField] private Button backButton;

    [Header("Scene UI")]
    [SerializeField] private RectTransform overlay;
    [SerializeField] private Image overlayBackground;
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
    [SerializeField] private Sprite calendarCardImage;

    private ScrollRect _statsScrollRect;
    private RectTransform _statsViewport;
    private RectTransform _statsContent;
    private RectTransform _calendarCard;
    private TextMeshProUGUI _calendarStreak;
    private TextMeshProUGUI _calendarMonth;
    private Button _previousMonthButton;
    private Button _nextMonthButton;
    private readonly Image[] _calendarDayImages = new Image[42];
    private readonly Outline[] _calendarTodayOutlines = new Outline[42];
    private readonly TextMeshProUGUI[] _calendarDayNumbers = new TextMeshProUGUI[42];
    private readonly TextMeshProUGUI[] _calendarDayMarks = new TextMeshProUGUI[42];
    private readonly List<TextMeshProUGUI> _calendarTextElements = new List<TextMeshProUGUI>();
    private readonly List<float> _calendarBaseFontSizes = new List<float>();
    private readonly HashSet<DateTime> _completedCalendarDates = new HashSet<DateTime>();
    private readonly HashSet<DateTime> _winningCalendarDates = new HashSet<DateTime>();
    private readonly HashSet<DateTime> _losingCalendarDates = new HashSet<DateTime>();
    private DateTime _firstChallengeDate;
    private DateTime _displayMonth;
    private bool _hasChallengeDate;

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
    private float _lastCanvasScale;
    private bool _layoutReady;

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

        BuildScrollableStatsArea();
        if (_statsViewport != null) _statsViewport.SetAsFirstSibling();
        _lastLandscape = Screen.width > Screen.height;
        ApplyLayout(_lastLandscape);
        if (overlay != null) overlay.gameObject.SetActive(false);
        if (backButton != null) backButton.gameObject.SetActive(false);
    }

    public void Bind(DailyChallengeViewModel viewModel) => _viewModel = viewModel;

    private void Update()
    {
        bool landscape = Screen.width > Screen.height;
        float canvasScale = GetCanvasScale();
        if (_layoutReady && landscape == _lastLandscape && Mathf.Approximately(canvasScale, _lastCanvasScale)) return;
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
        if (_statsScrollRect != null) _statsScrollRect.verticalNormalizedPosition = 1f;
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

    private void BuildScrollableStatsArea()
    {
        if (overlay == null) return;

        GameObject viewportObject = new GameObject(
            "DailyStatsViewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask), typeof(ScrollRect));
        _statsViewport = viewportObject.GetComponent<RectTransform>();
        _statsViewport.SetParent(overlay, false);
        _statsViewport.anchorMin = new Vector2(.02f, .02f);
        _statsViewport.anchorMax = new Vector2(.98f, .88f);
        _statsViewport.offsetMin = _statsViewport.offsetMax = Vector2.zero;

        Image viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = Color.white;
        viewportImage.raycastTarget = true;
        Mask mask = viewportObject.GetComponent<Mask>();
        mask.showMaskGraphic = false;

        _statsScrollRect = viewportObject.GetComponent<ScrollRect>();
        _statsScrollRect.viewport = _statsViewport;
        _statsScrollRect.horizontal = false;
        _statsScrollRect.vertical = true;
        _statsScrollRect.movementType = ScrollRect.MovementType.Elastic;
        _statsScrollRect.inertia = true;
        _statsScrollRect.scrollSensitivity = 45f;

        GameObject contentObject = new GameObject("DailyStatsContent", typeof(RectTransform));
        _statsContent = contentObject.GetComponent<RectTransform>();
        _statsContent.SetParent(_statsViewport, false);
        _statsContent.anchorMin = new Vector2(0f, 1f);
        _statsContent.anchorMax = new Vector2(1f, 1f);
        _statsContent.pivot = new Vector2(.5f, 1f);
        _statsContent.anchoredPosition = Vector2.zero;
        _statsContent.sizeDelta = Vector2.zero;
        _statsScrollRect.content = _statsContent;

        summaryCard.SetParent(_statsContent, false);
        personalBestCard.SetParent(_statsContent, false);
        bestTimesCard.SetParent(_statsContent, false);
        playedCard.SetParent(_statsContent, false);
        BuildCalendarCard();
    }

    private void BuildCalendarCard()
    {
        _calendarCard = CreateRect("DailyStreakCalendarCard", _statsContent);
        Image cardImage = _calendarCard.gameObject.AddComponent<Image>();
        cardImage.sprite = calendarCardImage;
        cardImage.type = Image.Type.Sliced;
        cardImage.color = new Color(1f, 1f, 1f, 184f / 255f);
        cardImage.raycastTarget = false;
        Outline cardOutline = _calendarCard.gameObject.AddComponent<Outline>();
        cardOutline.effectColor = new Color(1f, .76f, .30f, .9f);
        cardOutline.effectDistance = new Vector2(2f, -2f);

        _calendarStreak = CreateCalendarText("DailyPlayStreak", _calendarCard,
            string.Empty, 36f, _gold, TextAlignmentOptions.Center);
        Place(_calendarStreak.rectTransform, new Vector2(.04f, .88f), new Vector2(.96f, .98f));

        _calendarMonth = CreateCalendarText("CalendarMonth", _calendarCard,
            string.Empty, 34f, Color.white, TextAlignmentOptions.Center);
        Place(_calendarMonth.rectTransform, new Vector2(.14f, .76f), new Vector2(.86f, .87f));

        _previousMonthButton = CreateCalendarButton("PreviousCalendarMonth", _calendarCard, "<");
        Place(_previousMonthButton.GetComponent<RectTransform>(), new Vector2(.025f, .755f), new Vector2(.14f, .87f));
        _previousMonthButton.onClick.AddListener(() => ChangeCalendarMonth(-1));

        _nextMonthButton = CreateCalendarButton("NextCalendarMonth", _calendarCard, ">");
        Place(_nextMonthButton.GetComponent<RectTransform>(), new Vector2(.86f, .755f), new Vector2(.975f, .87f));
        _nextMonthButton.onClick.AddListener(() => ChangeCalendarMonth(1));

        string[] weekdays = { "M", "T", "W", "T", "F", "S", "S" };
        for (int column = 0; column < 7; column++)
        {
            TextMeshProUGUI weekday = CreateCalendarText("Weekday" + column, _calendarCard,
                weekdays[column], 27f, new Color(.88f, .9f, .95f), TextAlignmentOptions.Center);
            float left = .035f + column * .133f;
            Place(weekday.rectTransform, new Vector2(left, .685f), new Vector2(left + .12f, .75f));
        }

        for (int row = 0; row < 6; row++)
        {
            for (int column = 0; column < 7; column++)
            {
                int index = row * 7 + column;
                GameObject cell = new GameObject("CalendarDay" + index,
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
                RectTransform cellRect = cell.GetComponent<RectTransform>();
                cellRect.SetParent(_calendarCard, false);
                float left = .035f + column * .133f;
                float top = .665f - row * .078f;
                Place(cellRect, new Vector2(left, top - .072f), new Vector2(left + .12f, top));
                _calendarDayImages[index] = cell.GetComponent<Image>();
                _calendarDayImages[index].color = new Color(.04f, .09f, .16f, .78f);
                _calendarDayImages[index].raycastTarget = false;
                _calendarTodayOutlines[index] = cell.GetComponent<Outline>();
                _calendarTodayOutlines[index].effectColor = _gold;
                _calendarTodayOutlines[index].effectDistance = new Vector2(2f, -2f);
                _calendarTodayOutlines[index].enabled = false;

                _calendarDayNumbers[index] = CreateCalendarText(
                    "DayNumber", cellRect, string.Empty, 28f, Color.white, TextAlignmentOptions.Center);
                Place(_calendarDayNumbers[index].rectTransform,
                    new Vector2(.04f, .19f), new Vector2(.96f, .94f));
                _calendarDayMarks[index] = CreateCalendarText(
                    "DayStatus", cellRect, string.Empty, 20f, Color.white, TextAlignmentOptions.Center);
                Place(_calendarDayMarks[index].rectTransform,
                    new Vector2(.04f, .01f), new Vector2(.96f, .38f));
            }
        }

        TextMeshProUGUI legend = CreateCalendarText("CalendarLegend", _calendarCard,
            "<color=#45C878>W WON</color>     <color=#F25555>X FAILED</color>     <color=#AAB3C3>/ MISSED</color>     <color=#FFD45C>TODAY</color>",
            23f, Color.white, TextAlignmentOptions.Center);
        Place(legend.rectTransform, new Vector2(.035f, .085f), new Vector2(.965f, .155f));

        TextMeshProUGUI hint = CreateCalendarText("CalendarHint", _calendarCard,
            "Complete a Daily Challenge each day to keep your play streak going.",
            24f, new Color(.9f, .91f, .95f), TextAlignmentOptions.Center);
        hint.fontStyle = FontStyles.Italic;
        Place(hint.rectTransform, new Vector2(.045f, .015f), new Vector2(.955f, .075f));
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject rectObject = new GameObject(name, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);
        return (RectTransform)rectObject.transform;
    }

    private TextMeshProUGUI CreateCalendarText(
        string name, Transform parent, string value, float fontSize, Color color,
        TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = fontSize;
        text.fontSizeMin = Mathf.Max(12f, fontSize * .65f);
        text.fontSizeMax = fontSize;
        text.enableAutoSizing = true;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        _calendarTextElements.Add(text);
        _calendarBaseFontSizes.Add(fontSize);
        return text;
    }

    private Button CreateCalendarButton(string name, Transform parent, string label)
    {
        GameObject buttonObject = new GameObject(name,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(.05f, .11f, .19f, .9f);
        image.raycastTarget = true;
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        TextMeshProUGUI text = CreateCalendarText("Label", buttonObject.transform,
            label, 40f, new Color(1f, .81f, .4f), TextAlignmentOptions.Center);
        Stretch(text.rectTransform);
        return button;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private void ChangeCalendarMonth(int offset)
    {
        _displayMonth = _displayMonth.AddMonths(offset);
        RenderCalendar();
    }

    private void Refresh()
    {
        if (_viewModel == null) return;
        DailyChallengeStatsSnapshot stats = _viewModel.GetChallengeStats();
        DailyChallengeRecord bestOverall =
            _viewModel.GetBestOverallChallenge(stats.BestTimeGameId);
        List<DailyChallengeRecord> bestDifficultyRecords =
            _viewModel.GetBestChallengeTimes(stats);
        DailyChallengeRecord[] bestByDifficulty = new DailyChallengeRecord[difficultyNames.Length];
        foreach (DailyChallengeRecord record in bestDifficultyRecords)
            if (record.Difficulty >= 0 && record.Difficulty < bestByDifficulty.Length)
                bestByDifficulty[record.Difficulty] = record;

        summaryLabels[0].text = Metric("WIN STREAK", stats.CurrentStreak, "days");
        summaryLabels[1].text = Metric("BEST WIN STREAK",
            GetBestStreak(_viewModel.GetWinningChallengeDates()), "days");
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

        played.text = "CHALLENGES PLAYED     <b>" + stats.GamesPlayed.ToString(CultureInfo.InvariantCulture) +
            "</b>     <color=#FFBE4C>v CALENDAR</color>";
        for (int difficulty = 0; difficulty < difficultyNames.Length; difficulty++)
        {
            DailyChallengeRecord best = bestByDifficulty[difficulty];
            difficultyNames[difficulty].text = ((SudokuDifficulty)difficulty).ToString();
            difficultyNames[difficulty].color = _difficultyColors[difficulty];
            difficultyDates[difficulty].text = best == null
                ? "No win yet"
                : best.CurrentDate.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture);
            difficultyTimes[difficulty].text = best == null ? "--:--" : FormatTime(best.ElapsedSeconds);
        }

        RefreshCalendarData();
        RenderCalendar();
    }

    private void RefreshCalendarData()
    {
        _completedCalendarDates.Clear();
        _winningCalendarDates.Clear();
        _losingCalendarDates.Clear();
        _hasChallengeDate = false;

        DateTime? storedFirstChallengeDate = _viewModel.GetFirstChallengeDate();
        if (storedFirstChallengeDate.HasValue)
        {
            _firstChallengeDate = storedFirstChallengeDate.Value.Date;
            _hasChallengeDate = true;
        }

        List<DailyChallengeCalendarEntry> entries = _viewModel.GetCalendarEntries();
        foreach (DailyChallengeCalendarEntry entry in entries)
        {
            if (entry == null || !DateTime.TryParseExact(entry.CurrentDateText, "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                continue;

            DateTime date = parsedDate.Date;
            if (!_hasChallengeDate || date < _firstChallengeDate)
            {
                _firstChallengeDate = date;
                _hasChallengeDate = true;
            }

            if (string.IsNullOrWhiteSpace(entry.CompletedAt)) continue;
            _completedCalendarDates.Add(date);
            if (entry.IsWon)
            {
                _winningCalendarDates.Add(date);
                _losingCalendarDates.Remove(date);
            }
            else if (!_winningCalendarDates.Contains(date))
            {
                _losingCalendarDates.Add(date);
            }
        }

        DateTime today = DateTime.Today;
        DateTime firstMonth = _hasChallengeDate
            ? new DateTime(_firstChallengeDate.Year, _firstChallengeDate.Month, 1)
            : new DateTime(today.Year, today.Month, 1);
        DateTime currentMonth = new DateTime(today.Year, today.Month, 1);
        if (_displayMonth == default(DateTime) || _displayMonth < firstMonth || _displayMonth > currentMonth)
            _displayMonth = currentMonth;

        int playStreak = GetCurrentPlayStreak(today);
        _calendarStreak.text = "DAILY PLAY STREAK   <b>" +
            playStreak.ToString(CultureInfo.InvariantCulture) +
            (playStreak == 1 ? " DAY</b>" : " DAYS</b>");
    }

    private int GetCurrentPlayStreak(DateTime today)
    {
        if (_completedCalendarDates.Count == 0) return 0;

        DateTime cursor = _completedCalendarDates.Contains(today) ? today : today.AddDays(-1);
        if (_hasChallengeDate && cursor < _firstChallengeDate) return 0;

        int streak = 0;
        while (_completedCalendarDates.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }
        return streak;
    }

    private void RenderCalendar()
    {
        if (_calendarMonth == null) return;

        DateTime today = DateTime.Today;
        DateTime firstAvailableMonth = _hasChallengeDate
            ? new DateTime(_firstChallengeDate.Year, _firstChallengeDate.Month, 1)
            : new DateTime(today.Year, today.Month, 1);
        DateTime lastAvailableMonth = new DateTime(today.Year, today.Month, 1);
        _displayMonth = new DateTime(_displayMonth.Year, _displayMonth.Month, 1);
        _calendarMonth.text = _displayMonth.ToString("MMMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant();
        _previousMonthButton.interactable = _displayMonth > firstAvailableMonth;
        _nextMonthButton.interactable = _displayMonth < lastAvailableMonth;

        int leadingCells = ((int)_displayMonth.DayOfWeek + 6) % 7;
        int daysInMonth = DateTime.DaysInMonth(_displayMonth.Year, _displayMonth.Month);
        for (int index = 0; index < _calendarDayImages.Length; index++)
        {
            int dayNumber = index - leadingCells + 1;
            bool inMonth = dayNumber >= 1 && dayNumber <= daysInMonth;
            if (!inMonth)
            {
                _calendarDayImages[index].color = new Color(.025f, .055f, .1f, .42f);
                _calendarDayNumbers[index].text = string.Empty;
                _calendarDayMarks[index].text = string.Empty;
                _calendarTodayOutlines[index].enabled = false;
                continue;
            }

            DateTime date = new DateTime(_displayMonth.Year, _displayMonth.Month, dayNumber);
            bool isToday = date == today;
            bool isBeforeFirstChallenge = _hasChallengeDate && date < _firstChallengeDate;
            bool isFuture = date > today;
            bool won = _winningCalendarDates.Contains(date);
            bool lost = _losingCalendarDates.Contains(date);
            bool missed = !won && !lost && !isBeforeFirstChallenge && !isFuture && date < today;

            _calendarDayNumbers[index].text = dayNumber.ToString(CultureInfo.InvariantCulture);
            _calendarDayMarks[index].text = won ? "W" : lost ? "X" : missed ? "/" : isToday ? "TODAY" : string.Empty;
            _calendarTodayOutlines[index].enabled = isToday;

            if (won)
            {
                _calendarDayImages[index].color = new Color(.27f, .78f, .47f, 1f);
                _calendarDayNumbers[index].color = new Color(.06f, .09f, .14f, 1f);
                _calendarDayMarks[index].color = new Color(.06f, .09f, .14f, 1f);
            }
            else if (lost)
            {
                _calendarDayImages[index].color = new Color(.91f, .29f, .29f, 1f);
                _calendarDayNumbers[index].color = new Color(.08f, .09f, .13f, 1f);
                _calendarDayMarks[index].color = new Color(.08f, .09f, .13f, 1f);
            }
            else if (missed)
            {
                _calendarDayImages[index].color = new Color(.14f, .19f, .28f, .96f);
                _calendarDayNumbers[index].color = new Color(.68f, .72f, .8f, 1f);
                _calendarDayMarks[index].color = new Color(.77f, .81f, .88f, 1f);
            }
            else
            {
                _calendarDayImages[index].color = isToday
                    ? new Color(.04f, .09f, .16f, .98f)
                    : new Color(.035f, .08f, .14f, isFuture || isBeforeFirstChallenge ? .55f : .82f);
                _calendarDayNumbers[index].color = new Color(.9f, .92f, .96f, 1f);
                _calendarDayMarks[index].color = _gold;
            }
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

    private static int GetBestStreak(List<DateTime> wins)
    {
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
        if (overlay == null || overlayBackground == null || overlay.rect.height <= 0f) return;
        float canvasScale = GetCanvasScale();
        _lastCanvasScale = canvasScale;
        float typographyScale = landscape ? 1f / canvasScale : 1f;
        overlayBackground.sprite = landscape ? landscapeBackground : portraitBackground;

        float viewportBottom = .025f;
        float viewportTop = landscape ? .90f : .88f;
        _statsViewport.anchorMin = new Vector2(landscape ? .02f : .025f, viewportBottom);
        _statsViewport.anchorMax = new Vector2(landscape ? .98f : .975f, viewportTop);
        _statsViewport.offsetMin = _statsViewport.offsetMax = Vector2.zero;
        float contentHeight = overlay.rect.height * (landscape ? 2.24f : 1.95f);
        _statsContent.sizeDelta = new Vector2(0f, contentHeight);
        if (_open) _statsScrollRect.verticalNormalizedPosition = 1f;

        if (backButton != null)
            Place(backButton.transform as RectTransform, new Vector2(.025f, .905f), new Vector2(.12f, .985f));
        Place(title.rectTransform, landscape ? new Vector2(.15f, .89f) : new Vector2(.14f, .90f),
            landscape ? new Vector2(.85f, .985f) : new Vector2(.92f, .98f));

        if (landscape)
        {
            PlaceContentRect(summaryCard, .035f, .59f, .49f, .85f, viewportTop, contentHeight);
            PlaceContentRect(personalBestCard, .035f, .31f, .49f, .56f, viewportTop, contentHeight);
            PlaceContentRect(playedCard, .035f, .18f, .49f, .28f, viewportTop, contentHeight);
            PlaceContentRect(bestTimesCard, .50f, .10f, .975f, .89f, viewportTop, contentHeight);
            PlaceContentRect(_calendarCard, .035f, -1.02f, .965f, .10f, viewportTop, contentHeight);
        }
        else
        {
            PlaceContentRect(summaryCard, .04f, .72f, .96f, .865f, viewportTop, contentHeight);
            PlaceContentRect(personalBestCard, .04f, .55f, .96f, .695f, viewportTop, contentHeight);
            PlaceContentRect(bestTimesCard, .04f, .13f, .96f, .52f, viewportTop, contentHeight);
            PlaceContentRect(playedCard, .04f, .045f, .96f, .11f, viewportTop, contentHeight);
            PlaceContentRect(_calendarCard, .035f, -.75f, .965f, .02f, viewportTop, contentHeight);
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
            float top = (landscape ? .85f : .86f) - i * (landscape ? .09f : .092f);
            float bottom = top - (landscape ? .082f : .078f);
            float nameRight = landscape ? .34f : .39f;
            float dateRight = landscape ? .76f : .72f;
            Place(difficultyNames[i].rectTransform, new Vector2(.045f, bottom), new Vector2(nameRight, top));
            Place(difficultyDates[i].rectTransform, new Vector2(nameRight, bottom), new Vector2(dateRight, top));
            Place(difficultyTimes[i].rectTransform, new Vector2(dateRight, bottom), new Vector2(.96f, top));
            SetRowFontSize(difficultyNames[i], (landscape ? 34 : 30) * typographyScale,
                (landscape ? 30 : 26) * typographyScale);
            SetRowFontSize(difficultyDates[i], (landscape ? 31 : 26) * typographyScale,
                (landscape ? 27 : 22) * typographyScale);
            SetRowFontSize(difficultyTimes[i], (landscape ? 34 : 30) * typographyScale,
                (landscape ? 30 : 26) * typographyScale);
        }

        Place(difficultyHeading.rectTransform, new Vector2(.04f, .88f), new Vector2(.96f, .98f));
        if (landscape)
            SetRowFontSize(difficultyHeading, 34 * typographyScale, 30 * typographyScale);
        else
            SetRowFontSize(difficultyHeading, 31, 28);
        for (int i = 0; i < _calendarTextElements.Count; i++)
        {
            float baseSize = _calendarBaseFontSizes[i] * typographyScale;
            _calendarTextElements[i].fontSize = baseSize;
            _calendarTextElements[i].fontSizeMin = Mathf.Max(10f, baseSize * .65f);
            _calendarTextElements[i].fontSizeMax = baseSize;
        }
        _layoutReady = true;
    }

    private void PlaceContentRect(
        RectTransform rect, float xMin, float overlayYMin, float xMax, float overlayYMax,
        float viewportTop, float contentHeight)
    {
        float overlayHeight = overlay.rect.height;
        float minY = 1f - (viewportTop - overlayYMin) * overlayHeight / contentHeight;
        float maxY = 1f - (viewportTop - overlayYMax) * overlayHeight / contentHeight;
        Place(rect, new Vector2(xMin, minY), new Vector2(xMax, maxY));
    }

    private float GetCanvasScale()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        float scale = canvas != null ? canvas.scaleFactor : 1f;
        return scale > 0f ? scale : 1f;
    }

    private static void SetRowFontSize(TextMeshProUGUI label, float maxSize, float minSize)
    {
        label.enableAutoSizing = true;
        label.fontSizeMax = maxSize;
        label.fontSizeMin = minSize;
        label.fontSize = maxSize;
    }

    private static void Place(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
