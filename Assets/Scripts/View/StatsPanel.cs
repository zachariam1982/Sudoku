using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class StatsPanel : MonoBehaviour
{

    [Header("Canvas")]
    [SerializeField] private RectTransform canvas;
    [Header("Controls")]
    [SerializeField] private Button          closeButton;
    [SerializeField] private Button          backdropButton;

    [Header("Panel root")]
    [SerializeField] private RectTransform   panelRT;
    [SerializeField] private CanvasGroup     panelCG;

    [Header("Score ring")]
    [SerializeField] private Image           ringFill;           // Type=Filled, Radial360
    [SerializeField] private TextMeshProUGUI scoreValueLabel;
    [SerializeField] private TextMeshProUGUI scoreMaxLabel;

    [Header("Session stat pills — value labels")]
    [SerializeField] private TextMeshProUGUI levelValue;
    [SerializeField] private TextMeshProUGUI difficultyValue;
    [SerializeField] private TextMeshProUGUI timeValue;
    [SerializeField] private Image[]         liveDots;
    [SerializeField] private TextMeshProUGUI mistakesValue;
    [SerializeField] private TextMeshProUGUI sosValue;

    [Header("Win-rate bar")]
    [SerializeField] private TextMeshProUGUI winRatePctLabel;
    [SerializeField] private RectTransform   winRateBarFill;

    [Header("All-time stat cards — value labels")]
    [SerializeField] private TextMeshProUGUI totalGamesValue;
    [SerializeField] private TextMeshProUGUI winsValue;
    [SerializeField] private TextMeshProUGUI bestTimeValue;

    [Header("Streak chip")]
    [SerializeField] private TextMeshProUGUI streakNumber;
    [SerializeField] private TextMeshProUGUI streakDesc;
    [Header("Progression")]
    [SerializeField] private TextMeshProUGUI[] recentGameLabels;
    [SerializeField] private TextMeshProUGUI progressionText;
    [Header("Colors")]
    [SerializeField] private Color colorGold    = new Color(1.00f, 0.78f, 0.20f);
    [SerializeField] private Color colorRed     = new Color(0.88f, 0.27f, 0.27f);
    [SerializeField] private Color colorGreen   = new Color(0.11f, 0.73f, 0.48f);
    [SerializeField] private Color colorDotOn   = new Color(0.88f, 0.27f, 0.27f);
    [SerializeField] private Color colorDotOff  = new Color(0.15f, 0.15f, 0.31f);

    [Header("Animation")]
    [SerializeField] private float slideDuration = 0.26f;
    [SerializeField] private float barDuration   = 0.80f;

    [Header("ScrollView")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform parentScroll;
    [SerializeField] private GameObject recordScroll;
    [Header("Responsive Layout")]
    [SerializeField] private RectTransform contentRT;

    private const float StatsDesignWidth = 972f;
    private const float StatsDesignHeight = 1920f;

    private SudokuViewModel _vm;
    private bool            _open;
    private Coroutine       _slideAnim;
    private Coroutine       _barAnim;
    private float           _hiddenX;
    private bool _loading = false;
    private bool _allLoaded = false;
    private float loadThreshold = 0.08f;
    private List<GameObject> lstOfRecords = new List<GameObject>();
    private GameStats _gameStats;
    private int _totalPossiblePoints;
    
    void Awake()
    {
        _hiddenX = panelRT.sizeDelta.x;
        panelRT.anchoredPosition = new Vector2(_hiddenX, panelRT.anchoredPosition.y);

        panelCG.alpha          = 0f;
        panelCG.blocksRaycasts = false;
        panelCG.interactable   = false;

        if (backdropButton != null) backdropButton.gameObject.SetActive(false);

        if (closeButton    != null) closeButton.onClick.AddListener(ClosePanel);
        if (backdropButton != null) backdropButton.onClick.AddListener(ClosePanel);
        if (scrollRect     != null) scrollRect.onValueChanged.AddListener(OnScroll);
    }
    void OnRectTransformDimensionsChange()
    {
        if (canvas == null || panelRT == null) return;

        float availableWidth = canvas.rect.width;
        float availableHeight = canvas.rect.height;

        // Sidebar itself fills the available height,
        // but never grows wider than its 972-unit design width.
        float panelWidth = Mathf.Min(availableWidth, StatsDesignWidth);

        panelRT.sizeDelta =  new Vector2( panelWidth, availableHeight);

        if (contentRT != null)
        {
            // Scale the original 972x1920 layout down only when needed.
            float widthScale = panelWidth / StatsDesignWidth;
            float heightScale = availableHeight / StatsDesignHeight;
            float scale = Mathf.Min( 1f, Mathf.Min(widthScale, heightScale));

            contentRT.sizeDelta = new Vector2( StatsDesignWidth, StatsDesignHeight);
            contentRT.localScale = new Vector3(scale, scale, 1f);
            // Keep scaled content attached to top-right.
            contentRT.anchoredPosition = Vector2.zero;
        }

        // IMPORTANT: panel width can change after orientation changes.
        _hiddenX = panelWidth;

        if (!_open) panelRT.anchoredPosition = new Vector2( _hiddenX, panelRT.anchoredPosition.y);
    }

    public void Bind(SudokuViewModel vm)
    {
        _vm = vm;
        vm.ElapsedSeconds.OnChanged += _ => { if (_open) RefreshSession(); };
        vm.LivesRemaining.OnChanged += _ => { if (_open) RefreshSession(); };
        vm.PastHistory.OnChanged    += param => { StartCoroutine(AddRecords(param)); };
    }

    private IEnumerator AddRecords(List<GameRecord> arg)
    {
        _loading = true;
        for(int i = 0; i < arg.Count; i++)
        {
            var newRow = Instantiate(this.recordScroll, this.parentScroll);

            var recordScript = newRow.GetComponent<RecordScript>();
            if (recordScript != null) recordScript.Setup(_vm, arg[i].Id, (int)arg[i].Level, (int)arg[i].Difficulty, (int)arg[i].Points);
            lstOfRecords.Add(newRow);
            yield return new WaitForEndOfFrame();
        }
        _loading = false;
        yield return true;
    }
    private void OnScroll(Vector2 pos)
    {
        if(pos.y == 1 || pos.y == 0) return;

        if (!_loading && !_allLoaded && pos.y <= loadThreshold)
        {
            _vm.FetchHistoricalData.Execute();
        }
    }

    public void TogglePanel() { if (_open) ClosePanel(); else OpenPanel(); }

    private void OpenPanel()
    {
        _open = true;
        if (backdropButton != null) backdropButton.gameObject.SetActive(true);
        _vm.FetchHistoricalData.Execute();

        _gameStats = GameDatabase.GetGameStats();
        _totalPossiblePoints = GameDatabase.GetTotalPossiblePoints(_gameStats);

        RefreshSession();
        RefreshAllTime();
        RefreshProgression();
        if (_slideAnim != null) StopCoroutine(_slideAnim);
        _slideAnim = StartCoroutine(SlideIn());
    }

    private void ClosePanel()
    {
        _open = false;
        if (backdropButton != null) backdropButton.gameObject.SetActive(false);
        if (_slideAnim != null) StopCoroutine(_slideAnim);
        _slideAnim = StartCoroutine(SlideOut());
    }

    // ── Session refresh ───────────────────────────────────────────────────

    private void RefreshSession()
    {
        if (_vm == null) return;

        int total   = Mathf.FloorToInt(_vm.ElapsedSeconds.Value);
        int minutes = total / 60;
        int seconds = total % 60;

        string   diff  = ((SudokuDifficulty)_vm.GetDifficulty).ToString();

        // Score ring
        int score = _gameStats?.TotalPoints ?? 0;;
        int max_score = _totalPossiblePoints;
        Set(scoreValueLabel, score.ToString());
        Set(scoreMaxLabel, " / " + max_score.ToString());
        if (ringFill != null) {
            float ratio = max_score > 0 ? score / (float)max_score : 0f;

            ringFill.fillAmount = Mathf.Clamp01(ratio);
            int percentage = max_score > 0 ? Mathf.RoundToInt(ratio * 100f) : 0;
            string colorStr = "#FFC832";
            Color color;

            if(percentage <= 20)
            {
                colorStr = "#FF4D4D";
            }
            else if (percentage >= 80)
            {
                colorStr = "#2ECC71";
            }
            if(UnityEngine.ColorUtility.TryParseHtmlString( colorStr, out color))
            {
                ringFill.color = color;
            }
        }

        // Pills
        Set(levelValue, $"{_vm.GetLevel}");

        if (difficultyValue != null)
        {
            difficultyValue.text  = diff;
            difficultyValue.color = colorGold;
        }

        Set(timeValue, $"{minutes:00}:{seconds:00}");

        // Lives dots
        int lives = _vm.LivesRemaining.Value;
        for (int i = 0; i < liveDots.Length; i++)
            if (liveDots[i] != null)
                liveDots[i].color = i < lives ? colorDotOn : colorDotOff;

        if (mistakesValue != null)
        {
            int m = _vm.Penalties.Mistakes;
            mistakesValue.text  = m.ToString();
            mistakesValue.color = m > 0 ? colorRed : Color.white;
        }

        int sos = _vm.Penalties.SOSEmptyCells + _vm.Penalties.SOSWrongCells;
        Set(sosValue, sos.ToString());
    }

    // ── All-time refresh ──────────────────────────────────────────────────

    private void RefreshAllTime()
    {
        int games = _gameStats?.TotalGames ?? 0;
        int wins = _gameStats?.TotalWins ?? 0;
        int streak = _gameStats?.CurrentStreak ?? 0;
        float bestSecs = _gameStats?.FastestWinSeconds != null ? (float) _gameStats.FastestWinSeconds.Value : -1f;
        float rate     = games > 0 ? wins / (float)games : 0f;
        string bestStr = bestSecs >= 0 ? $"{(int)bestSecs / 60:00}:{(int)bestSecs % 60:00}" : "--:--";

        Set(totalGamesValue, $"{games}");
        Set(winsValue, $"{wins}");
        Set(bestTimeValue, bestStr);

        if (winRatePctLabel != null)
            winRatePctLabel.text = $"{rate * 100f:F1}%";

        // Animate the win-rate bar fill
        if (_barAnim != null) StopCoroutine(_barAnim);
        _barAnim = StartCoroutine(AnimateBar(rate));

        // Streak chip
        if (streakNumber != null) streakNumber.text = $"{streak}";
        if (streakDesc   != null) streakDesc.text   = streak > 1
            ? $"Win streak — keep it up"
            : streak == 1 ? "On a roll" : "Start a streak";
    }
    private void RefreshProgression()
    {
        const int ProgressionWindow = 5;

        // SQLite database is the source of truth.
        // Results are returned newest -> oldest.
        List<GameRecord> recent = GameDatabase.GetLastNRecordByDate( ProgressionWindow);

        RefreshRecentGameResults(recent);

        if (progressionText != null)
        {
            SudokuDifficulty currentDifficulty = (SudokuDifficulty)_vm.GetDifficulty;            
            progressionText.text = BuildProgressionText(recent, currentDifficulty);
        }
    }
    private void RefreshRecentGameResults(List<GameRecord> recent)
    {
        if (recentGameLabels == null) return;

        // Clear all five positions first.
        for (int i = 0; i < recentGameLabels.Length; i++)
        {
            if (recentGameLabels[i] == null) continue;

            recentGameLabels[i].text = "—";
            recentGameLabels[i].color = colorDotOff;
        }

        if (recent == null || recent.Count == 0) return;

        int count = Mathf.Min( recent.Count, recentGameLabels.Length);
        int startPosition = recentGameLabels.Length - count;

        for (int i = 0; i < count; i++)
        {
            GameRecord record = recent[count - 1 - i];

            int uiIndex = startPosition + i;

            TextMeshProUGUI label = recentGameLabels[uiIndex];

            if (label == null) continue;

            if (record.IsWon)
            {
                label.text = "W";
                label.color = colorGreen;
            }
            else
            {
                label.text = "L";
                label.color = colorRed;
            }
        }
    }

    private string BuildProgressionText( List<GameRecord> recent, SudokuDifficulty difficulty)
    {
        const int WindowSize = 5;
        const int RequiredWins = 4;
        const float RequiredEfficiency = 0.80f;
        int requiredPercent = 0;

        if (recent == null || recent.Count == 0) return "Complete games to begin your progression. Advancement requires 4 wins in 5 games with at least 80% average efficiency.";

        if (difficulty == SudokuDifficulty.Hardest) return $"Current: {difficulty}\nYou are at the highest difficulty.";

        SudokuDifficulty nextDifficulty = (SudokuDifficulty)( (int)difficulty + 1);
        int maxScore = ScoringSystem.GetAbsoluteMaximumScore( difficulty);

        if (maxScore <= 0) return $"Current: {difficulty}";

        List<GameRecord> tierGames = new List<GameRecord>();

        foreach (GameRecord record in recent)
        {
            if (record.Difficulty != (int)difficulty) break;

            tierGames.Add(record);
        }

        int gamesPlayed = tierGames.Count;
        int wins = 0;
        int totalPoints = 0;

        foreach (GameRecord record in tierGames)
        {
            if (record.IsWon) wins++;

            totalPoints += record.Points;
        }


        if (gamesPlayed < WindowSize)
        {
            int gamesRemaining = WindowSize - gamesPlayed;
            int winsNeeded = Mathf.Max( 0, RequiredWins - wins );
            int requiredTotalPoints = Mathf.CeilToInt( WindowSize * RequiredEfficiency * maxScore );
            int pointsStillNeeded = Mathf.Max( 0, requiredTotalPoints - totalPoints );
            float requiredAverage = pointsStillNeeded / (float)( gamesRemaining * maxScore );

            if (winsNeeded > gamesRemaining || requiredAverage > 1f)
            {
                return
                    $"Current: {difficulty}\n" +
                    $"Progress: {wins}/{gamesPlayed} wins. " +
                    $"This 5-game window can no longer " +
                    $"reach the promotion target. " +
                    $"Keep winning to build a stronger " +
                    $"rolling window toward {nextDifficulty}.";
            }

            requiredPercent = Mathf.CeilToInt( requiredAverage * 100f);
            string winRequirement;

            if (winsNeeded == 0)
            {
                winRequirement ="You already have enough wins";
            }
            else if (winsNeeded == 1)
            {
                winRequirement = $"You need 1 more win";
            }
            else
            {
                winRequirement = $"You need {winsNeeded} more wins";
            }

            return
                $"Current: {difficulty}\n" +
                $"{gamesPlayed}/5 games completed • " +
                $"{wins} win{(wins == 1 ? "" : "s")}.\n" +
                $"To reach {nextDifficulty}: " +
                $"{winRequirement}, with about " +
                $"{requiredPercent}% average efficiency " +
                $"across the remaining " +
                $"{gamesRemaining} game" +
                $"{(gamesRemaining == 1 ? "" : "s")}.";
        }

        float averageEfficiency = totalPoints / (float)(WindowSize * maxScore);


        if (wins >= RequiredWins && averageEfficiency >= RequiredEfficiency)
        {
            return
                $"Current: {difficulty}\n" +
                $"Last 5: {wins}/5 wins • " +
                $"{averageEfficiency * 100f:F0}% efficiency.\n" +
                $"Promotion target reached — " +
                $"you qualify for {nextDifficulty}.";
        }

        int retainedWins = 0;
        int retainedPoints = 0;
        int gamesToRetain = Mathf.Min(4, tierGames.Count);

        for (int i = 0; i < gamesToRetain; i++)
        {
            GameRecord record = tierGames[i];

            if (record.IsWon) retainedWins++;

            retainedPoints += record.Points;
        }

        int requiredWinsFromNextGame = RequiredWins - retainedWins;
        int promotionPointTarget = Mathf.CeilToInt( WindowSize * RequiredEfficiency * maxScore);
        int pointsNeededNextGame = Mathf.Max( 0, promotionPointTarget - retainedPoints);

        /*
        * One game cannot fix this window.
        */
        if (requiredWinsFromNextGame > 1 || pointsNeededNextGame > maxScore)
        {
            return
                $"Current: {difficulty}\n" +
                $"Last 5: {wins}/5 wins • " +
                $"{averageEfficiency * 100f:F0}% efficiency.\n" +
                $"To reach {nextDifficulty}, build toward " +
                $"4 wins in a rolling 5-game window " +
                $"while maintaining 80%+ average efficiency.";
        }

        requiredPercent = Mathf.CeilToInt( pointsNeededNextGame / (float)maxScore * 100f );

        /*
        * Next game must specifically be a win.
        */
        if (requiredWinsFromNextGame == 1)
        {
            return
                $"Current: {difficulty}\n" +
                $"Last 5: {wins}/5 wins • " +
                $"{averageEfficiency * 100f:F0}% efficiency.\n" +
                $"To reach {nextDifficulty} on the next " +
                $"evaluation, win your next game with at " +
                $"least {pointsNeededNextGame} points " +
                $"(~{requiredPercent}% efficiency).";
        }

        /*
        * The retained four already contain
        * at least four wins.
        */
        return
            $"Current: {difficulty}\n" +
            $"Last 5: {wins}/5 wins • " +
            $"{averageEfficiency * 100f:F0}% efficiency.\n" +
            $"Your win requirement is already satisfied. " +
            $"Score at least {pointsNeededNextGame} points " +
            $"(~{requiredPercent}% efficiency) in your next game " +
            $"to reach {nextDifficulty}.";
    }
    // ── Animations ────────────────────────────────────────────────────────

    private IEnumerator SlideIn()
    {
        panelCG.interactable   = true;
        panelCG.blocksRaycasts = true;

        float fromX = panelRT.anchoredPosition.x;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / slideDuration;
            float e = EaseOut(Mathf.Clamp01(t));
            panelRT.anchoredPosition = new Vector2(Mathf.Lerp(fromX, 0f, e), panelRT.anchoredPosition.y);
            panelCG.alpha = e;
            yield return null;
        }
        panelRT.anchoredPosition = new Vector2(0f, panelRT.anchoredPosition.y);
        panelCG.alpha = 1f;
    }

    private IEnumerator SlideOut()
    {
        panelCG.interactable   = false;
        panelCG.blocksRaycasts = false;

        float fromX = panelRT.anchoredPosition.x;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / slideDuration;
            float e = EaseOut(Mathf.Clamp01(t));
            panelRT.anchoredPosition = new Vector2(Mathf.Lerp(fromX, _hiddenX, e), panelRT.anchoredPosition.y);
            panelCG.alpha = 1f - e;
            yield return null;
        }
        panelRT.anchoredPosition = new Vector2(_hiddenX, panelRT.anchoredPosition.y);
        panelCG.alpha = 0f;
        _vm.ResetHistoricalData.Execute();
        for(int i = 0; i < lstOfRecords.Count; i++)
            Destroy(lstOfRecords[i]);
        
        lstOfRecords.Clear();
    }

    private IEnumerator AnimateBar(float targetRate)
    {
        if (winRateBarFill == null) yield break;
        float parentWidth = winRateBarFill.parent.GetComponent<RectTransform>().rect.width;
        float fromW  = winRateBarFill.sizeDelta.x;
        float toW    = parentWidth * targetRate;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / barDuration;
            float e = EaseOut(Mathf.Clamp01(t));
            winRateBarFill.sizeDelta = new Vector2(Mathf.Lerp(fromW, toW, e), winRateBarFill.sizeDelta.y);
            yield return null;
        }
        winRateBarFill.sizeDelta = new Vector2(toW, winRateBarFill.sizeDelta.y);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static float EaseOut(float t) => 1f - Mathf.Pow(1f - t, 3f);  // cubic ease-out

    private static void Set(TextMeshProUGUI tmp, string text)
    {
        if (tmp != null) tmp.text = text;
    }
}