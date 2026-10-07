using System.Collections;
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
    [Header("Session stat pills")]
    [SerializeField] private TextMeshProUGUI levelValue;
    [SerializeField] private TextMeshProUGUI difficultyValue;
    [Header("All-time stats")]
    [SerializeField] private TextMeshProUGUI winRatePctLabel;
    [SerializeField] private RectTransform winRateBarFill;
    [SerializeField] private TextMeshProUGUI totalGamesValue;
    [SerializeField] private TextMeshProUGUI winsValue;
    [SerializeField] private TextMeshProUGUI simpleStats;
    [SerializeField] private TextMeshProUGUI beginnerStats;
    [SerializeField] private TextMeshProUGUI easyStats;
    [SerializeField] private TextMeshProUGUI noviceStats;
    [SerializeField] private TextMeshProUGUI moderateStats;
    [SerializeField] private TextMeshProUGUI advancedStats;
    [SerializeField] private TextMeshProUGUI hardStats;
    [SerializeField] private TextMeshProUGUI expertStats;
    [SerializeField] private TextMeshProUGUI hardestStats;
    [Header("Gameplay UI")]
    [SerializeField] private GameObject gameplayTools;
    [Header("Animation")]
    [SerializeField] private float slideDuration = 0.26f;
    [SerializeField] private float barDuration = 0.8f;

    private NewGameViewModel _viewModel;
    private bool _open;
    private bool _gameplayToolsWasActive;
    private bool _tickerWasActive;
    private Coroutine _slideAnim;
    private Coroutine _barAnim;
    private const int RecordPageSize = 10;
    private int _recordOffset;
    private bool _recordLoading;
    private bool _allRecordsLoaded;
    private bool _layoutInitialized;
    private bool _lastLandscape;

    private void Awake()
    {
        if (recordsScrollRect != null)
            recordsScrollRect.onValueChanged.AddListener(OnRecordsScrolled);
        ApplyResponsiveLayout(Screen.width > Screen.height);
    }

    private void Update()
    {
        bool landscape = Screen.width > Screen.height;
        if (!_layoutInitialized || landscape != _lastLandscape)
            ApplyResponsiveLayout(landscape);
    }

    private void ApplyResponsiveLayout(bool landscape)
    {
        _lastLandscape = landscape;
        _layoutInitialized = true;
        if (recordsScrollRect == null) return;

        RectTransform scrollRect = recordsScrollRect.GetComponent<RectTransform>();
        if (scrollRect == null) return;

        scrollRect.anchorMin = new Vector2(landscape ? 0.08f : 0.045f, 0.04f);
        scrollRect.anchorMax = new Vector2(landscape ? 0.92f : 0.955f, 0.82f);
        scrollRect.anchoredPosition = Vector2.zero;
        scrollRect.sizeDelta = Vector2.zero;
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

        NewGameResultStats stats = NewGameResultDatabase.GetStats();
        float rate = stats.TotalGames > 0 ? stats.CompletedGames / (float)stats.TotalGames : 0f;
        Set(totalGamesValue, stats.TotalGames.ToString());
        Set(winsValue, stats.CompletedGames.ToString());
        if (winRatePctLabel != null) winRatePctLabel.text = $"{rate * 100f:F1}%";
        if (_barAnim != null) StopCoroutine(_barAnim);
        _barAnim = StartCoroutine(AnimateBar(rate));

        RefreshDifficultySummary(stats);
        RefreshRecords();
    }

    private void RefreshDifficultySummary(NewGameResultStats stats)
    {
        NewGameDifficultyStats tier;

        tier = NewGameResultDatabase.GetDifficultyStats(SudokuDifficulty.Simple);
        simpleStats.text = tier.GamesCompleted.ToString() + "/" + tier.GamesStarted.ToString();
        tier = NewGameResultDatabase.GetDifficultyStats(SudokuDifficulty.Beginner);
        beginnerStats.text = tier.GamesCompleted.ToString() + "/" + tier.GamesStarted.ToString();
        tier = NewGameResultDatabase.GetDifficultyStats(SudokuDifficulty.Easy);
        easyStats.text = tier.GamesCompleted.ToString() + "/" + tier.GamesStarted.ToString();
        tier = NewGameResultDatabase.GetDifficultyStats(SudokuDifficulty.Novice);
        noviceStats.text = tier.GamesCompleted.ToString() + "/" + tier.GamesStarted.ToString();
        tier = NewGameResultDatabase.GetDifficultyStats(SudokuDifficulty.Moderate);
        moderateStats.text = tier.GamesCompleted.ToString() + "/" + tier.GamesStarted.ToString();
        tier = NewGameResultDatabase.GetDifficultyStats(SudokuDifficulty.Advanced);
        advancedStats.text = tier.GamesCompleted.ToString() + "/" + tier.GamesStarted.ToString();
        tier = NewGameResultDatabase.GetDifficultyStats(SudokuDifficulty.Hard);
        hardStats.text = tier.GamesCompleted.ToString() + "/" + tier.GamesStarted.ToString();
        tier = NewGameResultDatabase.GetDifficultyStats(SudokuDifficulty.Expert);
        expertStats.text = tier.GamesCompleted.ToString() + "/" + tier.GamesStarted.ToString();
        tier = NewGameResultDatabase.GetDifficultyStats(SudokuDifficulty.Hardest);
        hardestStats.text = tier.GamesCompleted.ToString() + "/" + tier.GamesStarted.ToString();
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
        if (verseTicker != null)
        {
            bool anotherScreenIsOpen = (homeScreen != null && homeScreen.gameObject.activeInHierarchy) ||
                                       (newGameScreen != null && newGameScreen.gameObject.activeInHierarchy);
            verseTicker.SetGameActive(_tickerWasActive && !anotherScreenIsOpen);
        }
    }

    private IEnumerator AnimateBar(float targetRate)
    {
        if (winRateBarFill == null) yield break;
        RectTransform parent = winRateBarFill.parent.GetComponent<RectTransform>();
        if (parent == null) yield break;
        float from = winRateBarFill.sizeDelta.x;
        float to = parent.rect.width * targetRate;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / barDuration;
            float e = EaseOut(Mathf.Clamp01(t));
            winRateBarFill.sizeDelta = new Vector2(Mathf.Lerp(from, to, e), winRateBarFill.sizeDelta.y);
            yield return null;
        }
        winRateBarFill.sizeDelta = new Vector2(to, winRateBarFill.sizeDelta.y);
    }

    private static float EaseOut(float t) => 1f - Mathf.Pow(1f - t, 3f);
    private static void Set(TextMeshProUGUI label, string value) { if (label != null) label.text = value; }

}
