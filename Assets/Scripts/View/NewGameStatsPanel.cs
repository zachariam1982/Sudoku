using System.Collections;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen statistics page dedicated to the standalone New Game mode.
/// Journey statistics remain owned by StatsPanel.
/// </summary>
public sealed class NewGameStatsPanel : MonoBehaviour
{
    [Header("Scene references")]
    [SerializeField] private VerseTicker verseTicker;
    [SerializeField] private GameContext gameContext;
    [SerializeField] private HomeScreenController homeScreen;
    [SerializeField] private NewGameScreenController newGameScreen;
    [Header("Record list")]
    [SerializeField] private Transform recordsContent;
    [SerializeField] private ScrollRect recordsScrollRect;
    [SerializeField] private NewGameRecordRow recordPrefab;
    [Header("Panel root")]
    [SerializeField] private RectTransform panelRT;
    [SerializeField] private CanvasGroup panelCG;
    [Header("Current game stats")]
    [SerializeField] private TextMeshProUGUI levelValue;
    [SerializeField] private TextMeshProUGUI difficultyValue;
    [SerializeField] private TextMeshProUGUI pencilUsesValue;
    [SerializeField] private TextMeshProUGUI sosUsesValue;
    [SerializeField] private TextMeshProUGUI autoFillUsesValue;
    [Header("Current game responsive layout")]
    [SerializeField] private RectTransform currentGameCard;
    [SerializeField] private LayoutElement currentGameSummaryRow;
    [SerializeField] private LayoutElement currentGameUsageRow;
    [SerializeField] private TextMeshProUGUI currentGameHeading;
    [Header("All-time stats")]
    [SerializeField] private TextMeshProUGUI allTimeTitle;
    [SerializeField] private TextMeshProUGUI allTimePlayedValue;
    [SerializeField] private TextMeshProUGUI allTimeWonValue;
    [SerializeField] private TextMeshProUGUI allTimeWinRateValue;
    [SerializeField] private TextMeshProUGUI allTimeTableHeader;
    [SerializeField] private TextMeshProUGUI allTimeTableDetails;
    [SerializeField] private RectTransform[] allTimeSummaryIcons;
    [SerializeField] private RectTransform[] allTimeSummaryLabels;
    [SerializeField] private RectTransform[] allTimeSummaryValues;
    [Header("Gameplay UI")]
    [SerializeField] private GameObject gameplayTools;
    [SerializeField] private GameObject gameplayGrid;
    [SerializeField] private GameObject newGameTopBar;
    [Header("Animation")]
    [SerializeField] private float slideDuration = 0.26f;

    private NewGameViewModel _viewModel;
    private bool _open;
    private bool _gameplayToolsWasActive;
    private bool _gameplayGridWasActive;
    private bool _newGameTopBarWasActive;
    private bool _tickerWasActive;
    private Coroutine _slideAnim;
    private const int RecordPageSize = 10;
    private int _recordOffset;
    private bool _recordLoading;
    private bool _allRecordsLoaded;
    private bool _lastLandscape;
    private TextMeshProUGUI[] _currentGameTexts;
    private float[] _currentGameBaseFontSizes;
    private float _currentGameHeadingBaseFontSize;

    private const float PortraitCurrentGameRowHeight = 112f;
    private const float LandscapeCurrentGameRowHeight = 148f;
    private const float LandscapeCurrentGameFontScale = 1.25f;

    private void Awake()
    {
        CacheCurrentGameTypography();
        if (recordsScrollRect != null)
            recordsScrollRect.onValueChanged.AddListener(OnRecordsScrolled);
        _lastLandscape = Screen.width > Screen.height;
        ApplyCurrentGameResponsiveLayout(_lastLandscape);
        ApplyAllTimeSummaryResponsiveLayout(_lastLandscape);
        StartCoroutine(RefreshAllTimeLayoutAfterCanvasBuild());
    }

    private void Update()
    {
        bool landscape = Screen.width > Screen.height;
        if (landscape == _lastLandscape) return;
        _lastLandscape = landscape;
        ApplyCurrentGameResponsiveLayout(landscape);
        ApplyAllTimeSummaryResponsiveLayout(landscape);
    }

    private void CacheCurrentGameTypography()
    {
        if (currentGameCard != null)
        {
            _currentGameTexts = currentGameCard.GetComponentsInChildren<TextMeshProUGUI>(true);
            _currentGameBaseFontSizes = new float[_currentGameTexts.Length];
            for (int i = 0; i < _currentGameTexts.Length; i++)
                _currentGameBaseFontSizes[i] = _currentGameTexts[i].fontSize;
        }

        if (currentGameHeading != null)
            _currentGameHeadingBaseFontSize = currentGameHeading.fontSize;
    }

    private void ApplyCurrentGameResponsiveLayout(bool landscape)
    {
        ApplyStatsContentWidth(landscape);

        float rowHeight = landscape ? LandscapeCurrentGameRowHeight : PortraitCurrentGameRowHeight;
        SetRowHeight(currentGameSummaryRow, rowHeight);
        SetRowHeight(currentGameUsageRow, rowHeight);

        float fontScale = landscape ? LandscapeCurrentGameFontScale : 1f;
        if (_currentGameTexts != null)
        {
            for (int i = 0; i < _currentGameTexts.Length; i++)
            {
                if (_currentGameTexts[i] != null)
                    _currentGameTexts[i].fontSize = _currentGameBaseFontSizes[i] * fontScale;
            }
        }

        if (currentGameHeading != null)
            currentGameHeading.fontSize = _currentGameHeadingBaseFontSize * fontScale;

        if (currentGameCard != null)
            LayoutRebuilder.MarkLayoutForRebuild(currentGameCard);
    }

    private void ApplyStatsContentWidth(bool landscape)
    {
        RectTransform scrollViewRect = recordsScrollRect != null
            ? recordsScrollRect.transform as RectTransform
            : null;

        if (scrollViewRect != null)
        {
            Vector2 anchorMin = scrollViewRect.anchorMin;
            Vector2 anchorMax = scrollViewRect.anchorMax;
            anchorMin.x = landscape ? 0.15f : 0.02f;
            anchorMax.x = landscape ? 0.85f : 0.98f;
            scrollViewRect.anchorMin = anchorMin;
            scrollViewRect.anchorMax = anchorMax;
            LayoutRebuilder.MarkLayoutForRebuild(scrollViewRect);
        }

        RectTransform contentRect = recordsContent as RectTransform;
        if (contentRect != null)
        {
            Vector2 anchorMin = contentRect.anchorMin;
            Vector2 anchorMax = contentRect.anchorMax;
            anchorMin.x = landscape ? 0.14f : 0.02f;
            anchorMax.x = landscape ? 0.86f : 0.98f;
            contentRect.anchorMin = anchorMin;
            contentRect.anchorMax = anchorMax;
            LayoutRebuilder.MarkLayoutForRebuild(contentRect);
        }
    }

    private static void SetRowHeight(LayoutElement row, float height)
    {
        if (row == null) return;
        row.minHeight = height;
        row.preferredHeight = height;
    }

    private void OnDestroy()
    {
        if (recordsScrollRect != null)
            recordsScrollRect.onValueChanged.RemoveListener(OnRecordsScrolled);
    }

    public void Bind(NewGameViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    public void TogglePanel()
    {
        if (_open) ClosePanel();
        else OpenPanel();
    }

    private void OpenPanel()
    {
        if (_viewModel == null || panelRT == null || panelCG == null) return;
        _open = true;
        _tickerWasActive = verseTicker != null && verseTicker.IsGameActive;
        if (verseTicker != null) verseTicker.SetGameActive(false);
        if (gameplayTools != null)
        {
            _gameplayToolsWasActive = gameplayTools.activeSelf;
            gameplayTools.SetActive(false);
        }
        if (gameplayGrid != null)
        {
            _gameplayGridWasActive = gameplayGrid.activeSelf;
            gameplayGrid.SetActive(false);
        }
        if (newGameTopBar != null)
        {
            _newGameTopBarWasActive = newGameTopBar.activeSelf;
            newGameTopBar.SetActive(false);
        }
        Refresh();
        if (_slideAnim != null) StopCoroutine(_slideAnim);
        _slideAnim = StartCoroutine(SlideIn());
    }

    public void ClosePanel()
    {
        if (!_open) return;
        _open = false;
        if (_slideAnim != null) StopCoroutine(_slideAnim);
        _slideAnim = StartCoroutine(SlideOut());
    }

    private void Refresh()
    {
        if (_viewModel == null) return;
        Set(levelValue, _viewModel.GetLevel.ToString());
        Set(difficultyValue, ((SudokuDifficulty)_viewModel.GetDifficulty).ToString());
        Set(pencilUsesValue, _viewModel.UsageStats.PencilUses.ToString());
        Set(sosUsesValue, _viewModel.UsageStats.SOSUses.ToString());
        Set(autoFillUsesValue, _viewModel.UsageStats.AutoFillUses.ToString());

        NewGameResultStats stats = NewGameResultDatabase.GetStats();
        RefreshDifficultySummary(stats);
        RefreshRecords();
    }

    private void RefreshDifficultySummary(NewGameResultStats stats)
    {
        float winRate = stats.TotalGames > 0
            ? stats.CompletedGames * 100f / stats.TotalGames
            : 0f;
        Set(allTimeTitle, "ALL TIME");
        Set(allTimePlayedValue, stats.TotalGames.ToString());
        Set(allTimeWonValue, stats.CompletedGames.ToString());
        Set(allTimeWinRateValue, winRate.ToString("0.0", CultureInfo.InvariantCulture) + "%");

        if (allTimeTableHeader != null)
            allTimeTableHeader.text = "<color=#AFC5E9>DIFFICULTY<pos=42%>PLAYED<pos=62%>WON<pos=79%>WIN RATE</color>";

        StringBuilder rows = new StringBuilder();
        AppendDifficultyRow(rows, "SIMPLE", SudokuDifficulty.Simple);
        AppendDifficultyRow(rows, "BEGINNER", SudokuDifficulty.Beginner);
        AppendDifficultyRow(rows, "EASY", SudokuDifficulty.Easy);
        AppendDifficultyRow(rows, "NOVICE", SudokuDifficulty.Novice);
        AppendDifficultyRow(rows, "MODERATE", SudokuDifficulty.Moderate);
        AppendDifficultyRow(rows, "ADVANCED", SudokuDifficulty.Advanced);
        AppendDifficultyRow(rows, "HARD", SudokuDifficulty.Hard);
        AppendDifficultyRow(rows, "EXPERT", SudokuDifficulty.Expert);
        AppendDifficultyRow(rows, "HARDEST", SudokuDifficulty.Hardest);
        if (allTimeTableDetails != null)
            allTimeTableDetails.text = rows.ToString();
    }

    private static void AppendDifficultyRow(StringBuilder table, string label, SudokuDifficulty difficulty)
    {
        NewGameDifficultyStats tier = NewGameResultDatabase.GetDifficultyStats(difficulty);
        float winRate = tier.GamesStarted > 0
            ? tier.GamesCompleted * 100f / tier.GamesStarted
            : 0f;

        if (table.Length > 0) table.Append('\n');
        table.Append("<color=#F3F6FF>").Append(label).Append("</color>")
            .Append("<pos=42%><color=#FFD66B>").Append(tier.GamesStarted).Append("</color>")
            .Append("<pos=62%><color=#FFD66B>").Append(tier.GamesCompleted).Append("</color>")
            .Append("<pos=79%><color=#FFD66B>")
            .Append(winRate.ToString("0.#", CultureInfo.InvariantCulture)).Append("%</color>");
    }

    private void ApplyAllTimeSummaryResponsiveLayout(bool landscape)
    {
        if (allTimeSummaryIcons == null || allTimeSummaryLabels == null || allTimeSummaryValues == null ||
            allTimeSummaryIcons.Length < 3 || allTimeSummaryLabels.Length < 3 || allTimeSummaryValues.Length < 3)
            return;

        for (int i = 0; i < 3; i++)
        {
            RectTransform icon = allTimeSummaryIcons[i];
            RectTransform label = allTimeSummaryLabels[i];
            RectTransform value = allTimeSummaryValues[i];
            if (icon == null || label == null || value == null) continue;

            float shift = landscape ? (i == 2 ? 0.03f : 0.06f) : (i == 2 ? 0.02f : 0.04f);
            float minX = 0.135f + i * 0.33f - shift;
            float maxX = 0.315f + i * 0.33f - shift;
            label.anchorMin = new Vector2(minX, label.anchorMin.y);
            label.anchorMax = new Vector2(maxX, label.anchorMax.y);
            value.anchorMin = new Vector2(minX, value.anchorMin.y);
            value.anchorMax = new Vector2(maxX, value.anchorMax.y);

            if (landscape)
            {
                RectTransform summary = icon.parent as RectTransform;
                if (summary == null || summary.rect.width <= 0f || summary.rect.height <= 0f) continue;
                float bottom = Mathf.Min(label.anchorMin.y, value.anchorMin.y);
                float top = Mathf.Max(label.anchorMax.y, value.anchorMax.y);
                float size = (top - bottom) * summary.rect.height;
                float centerX = (0.135f + i * 0.33f) - (size * 0.5f + 8f) / summary.rect.width;
                icon.anchorMin = icon.anchorMax = new Vector2(centerX, (bottom + top) * 0.5f);
                icon.anchoredPosition = Vector2.zero;
                icon.sizeDelta = new Vector2(size, size);
                TextMeshProUGUI valueText = value.GetComponent<TextMeshProUGUI>();
                if (valueText != null) valueText.fontSize = 34f;
            }
            else
            {
                float x = 0.075f + i * 0.33f;
                icon.anchorMin = icon.anchorMax = new Vector2(x, 0.7825f);
                icon.anchoredPosition = Vector2.zero;
                icon.sizeDelta = new Vector2(50.4f, 50.4f);
                TextMeshProUGUI valueText = value.GetComponent<TextMeshProUGUI>();
                if (valueText != null) valueText.fontSize = 34f;
            }
        }
        RectTransform summaryRoot = allTimeSummaryIcons[0].parent as RectTransform;
        if (summaryRoot != null)
            LayoutRebuilder.MarkLayoutForRebuild(summaryRoot);
    }

    private IEnumerator RefreshAllTimeLayoutAfterCanvasBuild()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        ApplyAllTimeSummaryResponsiveLayout(Screen.width > Screen.height);
    }

    private void RefreshRecords()
    {
        if (recordsContent == null || recordPrefab == null) return;

        for (int i = recordsContent.childCount - 1; i >= 0; i--)
        {
            NewGameRecordRow existing = recordsContent.GetChild(i).GetComponent<NewGameRecordRow>();
            if (existing != null)
            {
                existing.gameObject.SetActive(false);
                Destroy(existing.gameObject);
            }
        }

        _recordOffset = 0;
        _recordLoading = false;
        _allRecordsLoaded = false;
        LoadNextRecordPage();

        if (recordsScrollRect != null)
            recordsScrollRect.verticalNormalizedPosition = 1f;
    }

    private void OnRecordsScrolled(Vector2 position)
    {
        if (position.y <= 0.08f)
            LoadNextRecordPage();
    }

    private void LoadNextRecordPage()
    {
        if (_recordLoading || _allRecordsLoaded || recordsContent == null || recordPrefab == null)
            return;

        _recordLoading = true;
        var entries = NewGameResultDatabase.GetRecentEntries(_recordOffset, RecordPageSize);
        foreach (NewGameResultEntry entry in entries)
        {
            NewGameRecordRow row = Instantiate(recordPrefab, recordsContent, false);
            row.Setup(entry, this);
        }

        _recordOffset += entries.Count;
        if (entries.Count < RecordPageSize)
            _allRecordsLoaded = true;
        _recordLoading = false;

        RectTransform contentRect = recordsContent as RectTransform;
        if (contentRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
    }

    public void RetakeResult(NewGameResult result)
    {
        if (gameContext == null || result == null) return;
        ClosePanel();
        gameContext.RetryNewGameResult(result);
    }

    private IEnumerator SlideIn()
    {
        panelCG.interactable = true;
        panelCG.blocksRaycasts = true;
        float from = panelRT.anchoredPosition.x;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / slideDuration;
            float e = EaseOut(Mathf.Clamp01(t));
            panelRT.anchoredPosition = new Vector2(Mathf.Lerp(from, 0f, e), panelRT.anchoredPosition.y);
            panelCG.alpha = e;
            yield return null;
        }
        panelRT.anchoredPosition = new Vector2(0f, panelRT.anchoredPosition.y);
        panelCG.alpha = 1f;
    }

    private IEnumerator SlideOut()
    {
        panelCG.interactable = false;
        panelCG.blocksRaycasts = false;
        float from = panelRT.anchoredPosition.x;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / slideDuration;
            float e = EaseOut(Mathf.Clamp01(t));
            panelRT.anchoredPosition = new Vector2(Mathf.Lerp(from, panelRT.sizeDelta.x, e), panelRT.anchoredPosition.y);
            panelCG.alpha = 1f - e;
            yield return null;
        }
        panelRT.anchoredPosition = new Vector2(panelRT.sizeDelta.x, panelRT.anchoredPosition.y);
        panelCG.alpha = 0f;
        if (gameplayTools != null) gameplayTools.SetActive(_gameplayToolsWasActive);
        if (gameplayGrid != null) gameplayGrid.SetActive(_gameplayGridWasActive);
        if (newGameTopBar != null) newGameTopBar.SetActive(_newGameTopBarWasActive);
        if (verseTicker != null)
        {
            bool anotherScreenIsOpen = (homeScreen != null && homeScreen.gameObject.activeInHierarchy) ||
                                       (newGameScreen != null && newGameScreen.gameObject.activeInHierarchy);
            verseTicker.SetGameActive(_tickerWasActive && !anotherScreenIsOpen);
        }
    }

    private static float EaseOut(float t) => 1f - Mathf.Pow(1f - t, 3f);
    private static void Set(TextMeshProUGUI label, string value) { if (label != null) label.text = value; }

}
