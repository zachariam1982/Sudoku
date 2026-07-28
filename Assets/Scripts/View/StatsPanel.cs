using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using System.Collections.Generic;

public class StatsPanel : MonoBehaviour
{

    [Header("Canvas")]
    [SerializeField] private RectTransform canvas;
    [Header("Controls")]
    [SerializeField] private Button          tabButton;
    [SerializeField] private Button          closeButton;
    [SerializeField] private Button          backdropButton;
    [SerializeField] private TextMeshProUGUI tabArrowLabel;

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

    private SudokuViewModel _vm;
    private bool            _open;
    private Coroutine       _slideAnim;
    private Coroutine       _barAnim;
    private float           _hiddenX;
    private bool _loading = false;
    private bool _allLoaded = false;
    private float loadThreshold = 0.08f;
    private List<GameObject> lstOfRecords = new List<GameObject>();
    private float statsPanelWidth = 972;
    
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
        if (scrollRect     != null) scrollRect.onValueChanged.AddListener(OnScroll);
        SetTabArrow(false);
    }
    void OnRectTransformDimensionsChange()
    {
        float minCanvasDimension = Mathf.Min(Mathf.Min(canvas.rect.width, canvas.rect.height), statsPanelWidth);
        Debug.Log($"Width for the stats panel is {minCanvasDimension}. Canvas width is {canvas.rect.width} and Canvas height is {canvas.rect.height} and width is {statsPanelWidth}");
        panelRT.sizeDelta = new Vector2(minCanvasDimension, panelRT.sizeDelta.y);        
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

    private void TogglePanel() { if (_open) ClosePanel(); else OpenPanel(); }

    private void OpenPanel()
    {
        _open = true;
        SetTabArrow(true);
        if (backdropButton != null) backdropButton.gameObject.SetActive(true);
        _vm.FetchHistoricalData.Execute();
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
        if (ringFill != null) {
            ringFill.fillAmount = Mathf.Clamp01(score / (float)max_score);
            int percentage = (score * 100)/max_score;
            string colorStr = "#FFC832";
            Color color;

            if(percentage < 35)
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
        int   games    = PlayerPrefs.GetInt(PlayerSettings.TotalGamePlayed, 0);
        int   wins     = PlayerPrefs.GetInt(PlayerSettings.TotalWins, 0);
        float bestSecs = PlayerPrefs.GetFloat(PlayerSettings.BestWinTime, -1f);
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

    private void SetTabArrow(bool open)
    {
        if (tabArrowLabel != null) tabArrowLabel.text = open ? ">>" : "<<";
    }

    private static void Set(TextMeshProUGUI tmp, string text)
    {
        if (tmp != null) tmp.text = text;
    }
}