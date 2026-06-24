using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Slide-out stats sidebar — improved UI version.
///
/// HIERARCHY
/// ─────────────────────────────────────────────────────────────────────────
///  StatsSidebar          [this component, RectTransform stretching full canvas]
///  ├─ Backdrop           [full-screen Image (alpha 0) + Button — tap to close]
///  ├─ TabButton          [~20×54 px, anchor middle-right, pivot (1,0.5)]
///  │  └─ TabArrow        [TMP "◀" / "▶"]
///  └─ StatsPanel         [972×1920, anchor top-right, pivot (1,1)]
///     ├─ PanelBG         [Image – color #14142E]
///     ├─ Header          [HorizontalLayoutGroup]
///     │  ├─ TitleLabel   [TMP – "STATS", spaced, uppercased, 28 pt]
///     │  └─ CloseButton  [Button – "✕"]
///     └─ ScrollView → Viewport → Content [VerticalLayoutGroup]
///        │
///        ├─ ScoreRing                  [see ScoreRing section below]
///        │  ├─ RingBG                  [Image, Type=Filled, FillMethod=Radial360, color #1C1C3C]
///        │  ├─ RingFill                [Image, Type=Filled, FillMethod=Radial360, color #FFC832]
///        │  └─ RingCenter
///        │     ├─ ScoreValueLabel      [TMP mono, 72 pt]
///        │     └─ ScoreMaxLabel        [TMP "/ 1000", 22 pt, dimmed]
///        │
///        ├─ StatGrid                   [GridLayoutGroup, 2 cols, gap 8]
///        │  ├─ Pill_Level
///        │  ├─ Pill_Difficulty
///        │  ├─ Pill_Time
///        │  ├─ Pill_Lives              [contains LivesDots child, not a TMP value]
///        │  ├─ Pill_Mistakes
///        │  └─ Pill_SOS
///        │     Each Pill: [VerticalLayoutGroup] → KeyLabel (TMP 22pt) + ValueLabel (TMP 34pt)
///        │     Pill_Lives replaces ValueLabel with LivesDots [HorizontalLayoutGroup]
///        │       └─ three Dot images (circle sprites, 20×20)
///        │
///        ├─ Divider                    ["ALL TIME" label + decorative line]
///        │
///        ├─ WinRateRow                 [VerticalLayoutGroup]
///        │  ├─ WinRateHeader           [HorizontalLayoutGroup]
///        │  │  ├─ WinRateLabel         [TMP "Win rate"]
///        │  │  └─ WinRatePctLabel      [TMP "65.9%", green]
///        │  ├─ BarBG                   [Image, height 5, color #252550]
///        │  └─ BarFill                 [Image child of BarBG, anchored left,
///        │                              scale X animated 0→winRate%]
///        │
///        ├─ AllTimeGrid               [GridLayoutGroup, 3 cols, gap 8]
///        │  ├─ AtCard_Played
///        │  ├─ AtCard_Wins
///        │  └─ AtCard_BestTime
///        │     Each AtCard: [VerticalLayoutGroup] → ValueLabel (TMP 34pt) + KeyLabel (TMP 18pt)
///        │
///        └─ StreakChip                [HorizontalLayoutGroup, bg #1D2B47, border blue]
///           ├─ StreakIcon             [TMP "⚡", 32 pt]
///           └─ StreakTextCol          [VerticalLayoutGroup]
///              ├─ StreakNumber        [TMP mono, 42 pt, blue]
///              └─ StreakDesc          [TMP "Win streak — keep going", 20 pt, dimmed]
///
/// COLORS (match your Cell Prefab palette)
///   bg0 #0D0D23   bg1 #14142E   bg2 #1C1C3C   bg3 #252550
///   Blue  #4682FF   Gold #FFC832   Red #E04444   Green #1DB97A
///   text1 #E8E8F5   text2 #8A8AAF  text3 #4A4A72
///
/// BINDING
///   Call statsPanel.Bind(vm) alongside your other Bind() calls.
///   RecordWin / RecordGamePlayed are triggered automatically via IsWon / IsLost.
/// ─────────────────────────────────────────────────────────────────────────
/// </summary>
public class StatsPanel : MonoBehaviour
{
    // ── Inspector refs ────────────────────────────────────────────────────

    [Header("Controls")]
    [SerializeField] private Button          tabButton;
    [SerializeField] private Button          closeButton;
    [SerializeField] private Button          backdropButton;
    [SerializeField] private TextMeshProUGUI tabArrowLabel;      // "◀" / "▶"

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
    [SerializeField] private Image[]         liveDots;           // 3 dot Images: on=red, off=bg3
    [SerializeField] private TextMeshProUGUI mistakesValue;
    [SerializeField] private TextMeshProUGUI sosValue;

    [Header("Win-rate bar")]
    [SerializeField] private TextMeshProUGUI winRatePctLabel;
    [SerializeField] private RectTransform   winRateBarFill;     // child of BarBG, anchored left

    [Header("All-time stat cards — value labels")]
    [SerializeField] private TextMeshProUGUI totalGamesValue;
    [SerializeField] private TextMeshProUGUI winsValue;
    [SerializeField] private TextMeshProUGUI bestTimeValue;

    [Header("Streak chip")]
    [SerializeField] private TextMeshProUGUI streakNumber;
    [SerializeField] private TextMeshProUGUI streakDesc;

    [Header("Colors")]
    [SerializeField] private Color colorGold    = new Color(1.00f, 0.78f, 0.20f);
    [SerializeField] private Color colorRed     = new Color(0.88f, 0.27f, 0.27f);
    [SerializeField] private Color colorGreen   = new Color(0.11f, 0.73f, 0.48f);
    [SerializeField] private Color colorDotOn   = new Color(0.88f, 0.27f, 0.27f);
    [SerializeField] private Color colorDotOff  = new Color(0.15f, 0.15f, 0.31f);

    [Header("Animation")]
    [SerializeField] private float slideDuration = 0.26f;
    [SerializeField] private float barDuration   = 0.80f;

    // ── PlayerPrefs keys ──────────────────────────────────────────────────

    private const string PK_GAMES       = "sp_games";
    private const string PK_WINS        = "sp_wins";
    private const string PK_BEST_TIME   = "sp_best_time";
    private const string PK_TOTAL_SCORE = "sp_total_score";
    private const string PK_STREAK      = "sp_streak";
    private const string PK_BEST_STREAK = "sp_best_streak";

    // ── Runtime ───────────────────────────────────────────────────────────

    private SudokuViewModel _vm;
    private bool            _open;
    private Coroutine       _slideAnim;
    private Coroutine       _barAnim;
    private float           _hiddenX;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    void Awake()
    {
        _hiddenX = panelRT.sizeDelta.x;
        panelRT.anchoredPosition = new Vector2(_hiddenX, panelRT.anchoredPosition.y);

        panelCG.alpha          = 0f;
        panelCG.blocksRaycasts = false;
        panelCG.interactable   = false;

        if (backdropButton != null) backdropButton.gameObject.SetActive(false);

        tabButton.onClick.AddListener(TogglePanel);
        if (closeButton    != null) closeButton.onClick.AddListener(ClosePanel);
        if (backdropButton != null) backdropButton.onClick.AddListener(ClosePanel);

        SetTabArrow(false);
    }

    // ── Public API ────────────────────────────────────────────────────────

    public void Bind(SudokuViewModel vm)
    {
        _vm = vm;
        vm.ElapsedSeconds.OnChanged += _ => { if (_open) RefreshSession(); };
        vm.LivesRemaining.OnChanged += _ => { if (_open) RefreshSession(); };
        vm.IsWon.OnChanged          += OnGameWon;
        vm.IsLost.OnChanged         += OnGameLost;
    }

    // ── Open / Close ──────────────────────────────────────────────────────

    private void TogglePanel() { if (_open) ClosePanel(); else OpenPanel(); }

    private void OpenPanel()
    {
        _open = true;
        SetTabArrow(true);
        if (backdropButton != null) backdropButton.gameObject.SetActive(true);
        RefreshSession();
        RefreshAllTime();
        if (_slideAnim != null) StopCoroutine(_slideAnim);
        _slideAnim = StartCoroutine(SlideIn());
    }

    private void ClosePanel()
    {
        _open = false;
        SetTabArrow(false);
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
        int score = PlayerPrefs.GetInt(PlayerSettings.TotalPoints);
        int max_score = PlayerPrefs.GetInt(PlayerSettings.TotalPossiblePoints);
        Set(scoreValueLabel, score.ToString());
        Set(scoreMaxLabel, " / " + max_score.ToString());
        if (ringFill != null)
            ringFill.fillAmount = Mathf.Clamp01(score / (float)max_score);

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
        int   games    = PlayerPrefs.GetInt(PlayerSettings.TotalGamePlayed, 0);
        int   wins     = PlayerPrefs.GetInt(PlayerSettings.TotalWins, 0);
        float bestSecs = PlayerPrefs.GetFloat(PlayerSettings.BestWinTime, -1f);
        //int   totScore = PlayerPrefs.GetInt(PK_TOTAL_SCORE, 0);
        int   streak   = PlayerPrefs.GetInt(PlayerSettings.CurrentStreak, 0);

        float rate     = games > 0 ? wins / (float)games : 0f;
        string bestStr = bestSecs >= 0
            ? $"{(int)bestSecs / 60:00}:{(int)bestSecs % 60:00}"
            : "--:--";

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

    // ── Persistence ───────────────────────────────────────────────────────

    private void RecordWin()
    {
        if (_vm == null) return;
        int   games    = PlayerPrefs.GetInt(PlayerSettings.TotalGamePlayed, 0) + 1;
        int   wins     = PlayerPrefs.GetInt(PlayerSettings.TotalWins, 0) + 1;
        int   streak   = PlayerPrefs.GetInt(PlayerSettings.CurrentStreak, 0) + 1;
        float best_t   = PlayerPrefs.GetFloat(PlayerSettings.BestWinTime, float.MaxValue);
        int   totScore = PlayerPrefs.GetInt(PK_TOTAL_SCORE, 0) + CalculateScore();
        float elapsed  = _vm.ElapsedSeconds.Value;

        PlayerPrefs.SetInt(PK_GAMES, games);
        PlayerPrefs.SetInt(PK_WINS, wins);
        PlayerPrefs.SetInt(PK_STREAK, streak);
        PlayerPrefs.SetInt(PK_TOTAL_SCORE, totScore);
        if (elapsed < best_t) PlayerPrefs.SetFloat(PK_BEST_TIME, elapsed);
        PlayerPrefs.Save();
    }

    private void RecordLoss()
    {
        PlayerPrefs.SetInt(PK_GAMES, PlayerPrefs.GetInt(PK_GAMES, 0) + 1);
        PlayerPrefs.SetInt(PK_STREAK, 0);   // reset streak on loss
        PlayerPrefs.Save();
    }

    private void OnGameWon(bool won)  { if (won)  RecordWin(); }
    private void OnGameLost(bool lost) { if (lost) RecordLoss(); }

    // ── Score formula ─────────────────────────────────────────────────────

    private int CalculateScore()
    {
        if (_vm == null) return 0;
        int raw = 1000
                  - Mathf.FloorToInt(_vm.ElapsedSeconds.Value / 10)
                  - (_vm.Penalties.Mistakes * 50)
                  - ((_vm.Penalties.SOSEmptyCells + _vm.Penalties.SOSWrongCells) * 100);
        return Mathf.Max(0, raw);
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

    private void SetTabArrow(bool open)
    {
        if (tabArrowLabel != null) tabArrowLabel.text = open ? "▶" : "◀";
    }

    private static void Set(TextMeshProUGUI tmp, string text)
    {
        if (tmp != null) tmp.text = text;
    }
}