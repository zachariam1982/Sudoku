using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System.Globalization;

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
    [SerializeField] private TextMeshProUGUI livesValue;

    [Header("All-time summary values")]
    [SerializeField] private TextMeshProUGUI allTimeTitle;
    [SerializeField] private TextMeshProUGUI allTimePlayedValue;
    [SerializeField] private TextMeshProUGUI allTimeWonValue;
    [SerializeField] private TextMeshProUGUI allTimeWinRateValue;

    [Header("All-time difficulty table")]
    [SerializeField] private TextMeshProUGUI allTimeTableHeader;
    [SerializeField] private TextMeshProUGUI allTimeTableDetails;

    [Header("All-time win-rate bar")]
    [SerializeField] private RectTransform winRateBarFill;

    [Header("Streak chip")]
    [SerializeField] private TextMeshProUGUI streakNumber;
    [SerializeField] private TextMeshProUGUI streakDesc;
    [Header("Progression")]
    [SerializeField] private TextMeshProUGUI[] recentGameLabels;
    [SerializeField] private TextMeshProUGUI currentDifficultyText;
    [SerializeField] private TextMeshProUGUI progressionText;
    [SerializeField] private TextMeshProUGUI progressionCountText;
    [SerializeField] private RectTransform progressionBarFill;
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
    [SerializeField] private RectTransform currentJourneyColumns;
    [SerializeField] private RectTransform[] allTimeSummaryIcons;
    [SerializeField] private RectTransform[] allTimeSummaryLabels;
    [SerializeField] private RectTransform[] allTimeSummaryValues;
    [SerializeField] private float landscapeSummaryIconGap = 8f;
    [SerializeField] private float landscapeSummaryTextShift = 0.06f;
    [SerializeField] private float landscapeWinRateTextShift = 0.03f;
    [SerializeField] private float landscapeSummaryValueFontSize = 34f;
    [SerializeField] private float portraitSummaryTextShift = 0.04f;
    [SerializeField] private float portraitWinRateTextShift = 0.02f;
    [Header("Gameplay UI")]
    [SerializeField] private GameObject gameplayTools;
    [SerializeField] private GameObject gameplayGrid;
    [SerializeField] private GameObject journeyTopBar;

    private bool _gameplayToolsWasActive;
    private bool _gameplayGridWasActive;
    private bool _journeyTopBarWasActive;
    private bool _tickerWasActive;
    private VerseTicker _verseTicker;


    private JourneyViewModel _vm;
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
    private int _lastScreenWidth = -1;
    private int _lastScreenHeight = -1;
    private Coroutine _responsiveLayoutRefresh;
    
    void Awake()
    {
        _hiddenX = canvas != null ? canvas.rect.width : panelRT.rect.width;
        panelRT.anchoredPosition = new Vector2(_hiddenX, panelRT.anchoredPosition.y);

        panelCG.alpha          = 0f;
        panelCG.blocksRaycasts = false;
        panelCG.interactable   = false;
        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;
        ApplyResponsiveStatsContentWidth();

        if (backdropButton != null) backdropButton.gameObject.SetActive(false);

        if (closeButton    != null) closeButton.onClick.AddListener(ClosePanel);
        if (backdropButton != null) backdropButton.onClick.AddListener(ClosePanel);
        if (scrollRect     != null) scrollRect.onValueChanged.AddListener(OnScroll);
    }

    private void LateUpdate()
    {
        if (_lastScreenWidth == Screen.width && _lastScreenHeight == Screen.height) return;

        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;

        // Update anchors immediately, then let the canvas and parent layout settle
        // before measuring the three columns.
        ApplyResponsiveStatsContentWidth();
        ScheduleResponsiveLayoutRefresh();
    }

    private void ScheduleResponsiveLayoutRefresh()
    {
        if (_responsiveLayoutRefresh == null)
            _responsiveLayoutRefresh = StartCoroutine(RefreshResponsiveLayoutAfterCanvasResize());
    }

    private IEnumerator RefreshResponsiveLayoutAfterCanvasResize()
    {
        ApplyResponsiveStatsContentWidth();

        float previousWidth = -1f;
        float previousHeight = -1f;
        int stableFrames = 0;
        for (int frame = 0; frame < 8 && stableFrames < 2; frame++)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();

            float width = currentJourneyColumns != null
                ? currentJourneyColumns.rect.width
                : canvas != null ? canvas.rect.width : 0f;
            float height = currentJourneyColumns != null
                ? currentJourneyColumns.rect.height
                : canvas != null ? canvas.rect.height : 0f;

            if (Mathf.Abs(width - previousWidth) < 0.5f &&
                Mathf.Abs(height - previousHeight) < 0.5f)
                stableFrames++;
            else
                stableFrames = 0;

            previousWidth = width;
            previousHeight = height;
        }

        // Re-read the actual resized RectTransform bounds after the canvas settles.
        ApplyResponsiveStatsContentWidth();
        Canvas.ForceUpdateCanvases();
        ApplyCurrentJourneyResponsiveLayout();
        ApplyAllTimeSummaryResponsiveLayout();
        Canvas.ForceUpdateCanvases();

        if (currentJourneyColumns != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(currentJourneyColumns);

        _responsiveLayoutRefresh = null;
    }

    private void Update()
    {
        if (!_open || panelRT == null) return;

        if (!TryGetPointerDownPosition(out Vector2 screenPosition)) return;

        Canvas parentCanvas = panelRT.GetComponentInParent<Canvas>();

        Camera uiCamera = parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? parentCanvas.worldCamera : null;

        bool clickedInsidePanel = RectTransformUtility.RectangleContainsScreenPoint( panelRT, screenPosition, uiCamera);

        if (!clickedInsidePanel) ClosePanel();
    }
    private bool TryGetPointerDownPosition(out Vector2 screenPosition)
    {
        // Touch / Android
        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;

            if (touch.press.wasPressedThisFrame)
            {
                screenPosition = touch.position.ReadValue();

                return true;
            }
        }

        // Mouse / Editor / WebGL / PC
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPosition = Mouse.current.position.ReadValue();

            return true;
        }

        screenPosition = Vector2.zero;
        return false;
    }
    void OnRectTransformDimensionsChange()
    {
        if (canvas == null || panelRT == null) return;

        _hiddenX = canvas.rect.width;
        ApplyResponsiveStatsContentWidth();
        ScheduleResponsiveLayoutRefresh();
        if (!_open)
            panelRT.anchoredPosition = new Vector2(_hiddenX, panelRT.anchoredPosition.y);
    }

    private void ApplyResponsiveStatsContentWidth()
    {
        if (canvas == null) return;

        bool landscape = Screen.width > Screen.height;

        RectTransform viewport = scrollRect != null
            ? scrollRect.transform as RectTransform
            : null;
        SetHorizontalAnchors(viewport,
            landscape ? 0.15f : 0.02f,
            landscape ? 0.85f : 0.98f);

        SetHorizontalAnchors(parentScroll as RectTransform,
            landscape ? 0.14f : 0.02f,
            landscape ? 0.86f : 0.98f);

        // Keep portrait cards close to the screen edges while preserving a visible
        // margin for the glass border. Landscape retains the roomier authored inset.
        VerticalLayoutGroup contentLayout = parentScroll != null
            ? parentScroll.GetComponent<VerticalLayoutGroup>()
            : null;
        if (contentLayout != null)
        {
            int horizontalPadding = landscape ? 36 : 12;
            RectOffset padding = contentLayout.padding;
            padding.left = horizontalPadding;
            padding.right = horizontalPadding;
            contentLayout.padding = padding;
        }

        ApplyCurrentJourneyResponsiveLayout();
        ApplyAllTimeSummaryResponsiveLayout();
    }

    private void ApplyAllTimeSummaryResponsiveLayout()
    {
        if (allTimeSummaryIcons == null || allTimeSummaryLabels == null || allTimeSummaryValues == null ||
            allTimeSummaryIcons.Length < 3 || allTimeSummaryLabels.Length < 3 || allTimeSummaryValues.Length < 3)
            return;

        bool landscape = Screen.width > Screen.height;
        for (int i = 0; i < 3; i++)
        {
            RectTransform icon = allTimeSummaryIcons[i];
            RectTransform label = allTimeSummaryLabels[i];
            RectTransform value = allTimeSummaryValues[i];
            if (icon == null || label == null || value == null) continue;

            if (!landscape)
            {
                // Keep the approved portrait presentation exactly as authored in the scene.
                float portraitX = 0.075f + i * 0.33f;
                icon.anchorMin = new Vector2(portraitX, 0.7825f);
                icon.anchorMax = new Vector2(portraitX, 0.7825f);
                icon.anchoredPosition = Vector2.zero;
                icon.sizeDelta = new Vector2(50.4f, 50.4f);
                float portraitTextShift = i == 2 ? portraitWinRateTextShift : portraitSummaryTextShift;
                SetAllTimeSummaryTextAnchors(label, value, i, portraitTextShift);
                SetAllTimeSummaryValueFontSize(i, 34f);
                continue;
            }

            RectTransform summary = icon.parent as RectTransform;
            if (summary == null || summary.rect.width <= 0f || summary.rect.height <= 0f) continue;

            // Size the icon to span both text rows, including their inter-row gap.
            float bottom = Mathf.Min(label.anchorMin.y, value.anchorMin.y);
            float top = Mathf.Max(label.anchorMax.y, value.anchorMax.y);
            float iconSize = (top - bottom) * summary.rect.height;
            float centerY = (bottom + top) * 0.5f;
            float originalTextLeft = 0.135f + i * 0.33f;
            float centerX = originalTextLeft - (iconSize * 0.5f + landscapeSummaryIconGap) / summary.rect.width;

            icon.anchorMin = new Vector2(centerX, centerY);
            icon.anchorMax = new Vector2(centerX, centerY);
            icon.anchoredPosition = Vector2.zero;
            icon.sizeDelta = new Vector2(iconSize, iconSize);
            float textShift = i == 2 ? landscapeWinRateTextShift : landscapeSummaryTextShift;
            SetAllTimeSummaryTextAnchors(label, value, i, textShift);
            SetAllTimeSummaryValueFontSize(i, landscapeSummaryValueFontSize);
        }
    }

    private void SetAllTimeSummaryTextAnchors(RectTransform label, RectTransform value, int index, float shift)
    {
        float minX = 0.135f + index * 0.33f - shift;
        float maxX = 0.315f + index * 0.33f - shift;
        label.anchorMin = new Vector2(minX, label.anchorMin.y);
        label.anchorMax = new Vector2(maxX, label.anchorMax.y);
        value.anchorMin = new Vector2(minX, value.anchorMin.y);
        value.anchorMax = new Vector2(maxX, value.anchorMax.y);
    }

    private void SetAllTimeSummaryValueFontSize(int index, float fontSize)
    {
        TextMeshProUGUI value = index == 0 ? allTimePlayedValue :
            index == 1 ? allTimeWonValue : allTimeWinRateValue;
        if (value != null) value.fontSize = fontSize;
    }

    private void ApplyCurrentJourneyResponsiveLayout()
    {
        if (currentJourneyColumns == null || currentJourneyColumns.childCount < 2) return;

        var horizontal = currentJourneyColumns.GetComponent<HorizontalLayoutGroup>();
        var vertical = currentJourneyColumns.GetComponent<VerticalLayoutGroup>();
        if (horizontal != null)
        {
            horizontal.enabled = true;
            horizontal.spacing = 12f;
        }
        if (vertical != null) vertical.enabled = false;

        var bodyLayout = currentJourneyColumns.GetComponent<LayoutElement>();
        if (bodyLayout != null) bodyLayout.preferredHeight = 250f;

        var cardLayout = currentJourneyColumns.parent != null
            ? currentJourneyColumns.parent.GetComponent<LayoutElement>()
            : null;
        if (cardLayout != null) cardLayout.preferredHeight = 330f;

        var grid = currentJourneyColumns.GetChild(1).GetComponent<GridLayoutGroup>();
        if (grid == null) return;

        grid.constraintCount = 2;
        grid.spacing = new Vector2(12f, 12f);
        grid.cellSize = new Vector2(300f, 72f);

        // Use the measured body width after its parent scroll content has resized.
        // The orientation refresh waits for this RectTransform to settle before applying
        // the equal-width columns.
        float columnsWidth = currentJourneyColumns.rect.width;
        if (columnsWidth <= 1f)
            columnsWidth = canvas.rect.width * (Screen.width > Screen.height ? 0.72f : 0.96f);

        float horizontalPadding = horizontal != null
            ? horizontal.padding.left + horizontal.padding.right
            : 0f;
        float groupSpacing = horizontal != null ? horizontal.spacing : 0f;
        float columnWidth = Mathf.Max(0f,
            (columnsWidth - horizontalPadding - groupSpacing - grid.spacing.x) / 3f);

        var ringLayout = currentJourneyColumns.GetChild(0).GetComponent<LayoutElement>();
        if (ringLayout != null)
        {
            ringLayout.preferredWidth = columnWidth;
            ringLayout.preferredHeight = 250f;
            ringLayout.flexibleWidth = 0f;
        }

        var gridLayout = grid.GetComponent<LayoutElement>();
        if (gridLayout != null)
        {
            gridLayout.preferredWidth = columnWidth * 2f + grid.spacing.x;
            gridLayout.preferredHeight = grid.cellSize.y * 3f + grid.spacing.y * 2f;
            gridLayout.flexibleWidth = 0f;
        }

        grid.cellSize = new Vector2(columnWidth, 72f);
        LayoutRebuilder.MarkLayoutForRebuild(grid.GetComponent<RectTransform>());
        LayoutRebuilder.MarkLayoutForRebuild(currentJourneyColumns);
        RectTransform cardRect = currentJourneyColumns.parent as RectTransform;
        if (cardRect != null)
            LayoutRebuilder.MarkLayoutForRebuild(cardRect);
    }

    private static void SetHorizontalAnchors(RectTransform rect, float min, float max)
    {
        if (rect == null) return;

        Vector2 anchorMin = rect.anchorMin;
        Vector2 anchorMax = rect.anchorMax;
        if (Mathf.Approximately(anchorMin.x, min) && Mathf.Approximately(anchorMax.x, max))
            return;

        anchorMin.x = min;
        anchorMax.x = max;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        LayoutRebuilder.MarkLayoutForRebuild(rect);
    }

    public void Bind(JourneyViewModel vm)
    {
        _vm = vm;
        vm.ElapsedSeconds.OnChanged += OnElapsedSecondsChanged;
        vm.LivesRemaining.OnChanged += OnLivesRemainingChanged;
        vm.PastHistory.OnChanged += OnPastHistoryChanged;
        vm.RetryOlderGameRequested.OnChanged += OnRetryOlderGameRequested;
    }
    private void OnElapsedSecondsChanged(float seconds)
    {
        if (_open) RefreshSession();
    }

    private void OnLivesRemainingChanged(int lives)
    {
        if (_open) RefreshSession();
    }

    private void OnPastHistoryChanged(List<GameRecord> records)
    {
        StartCoroutine(AddRecords(records));
    }
    private void OnRetryOlderGameRequested(bool requested)
    {
        if (!requested) return;
        if (_open) ClosePanel();
    }
    private void OnDestroy()
    {
        if (_vm == null) return;

        _vm.ElapsedSeconds.OnChanged -= OnElapsedSecondsChanged;
        _vm.LivesRemaining.OnChanged -= OnLivesRemainingChanged;
        _vm.PastHistory.OnChanged -= OnPastHistoryChanged;
        _vm.RetryOlderGameRequested.OnChanged -= OnRetryOlderGameRequested;
    }
    private IEnumerator AddRecords(List<GameRecord> arg)
    {
        _loading = true;
        for(int i = 0; i < arg.Count; i++)
        {
            var newRow = Instantiate(this.recordScroll, this.parentScroll);

            var recordScript = newRow.GetComponent<RecordScript>();
            
            if (recordScript != null) recordScript.Setup(_vm, arg[i]);

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
        ApplyResponsiveStatsContentWidth();
        Canvas.ForceUpdateCanvases();
        ApplyAllTimeSummaryResponsiveLayout();

        if (_verseTicker == null && canvas != null)
            _verseTicker = canvas.GetComponent<VerseTicker>();
        _tickerWasActive = _verseTicker != null && _verseTicker.IsGameActive;
        if (_verseTicker != null) _verseTicker.SetGameActive(false);

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
        if (journeyTopBar != null)
        {
            _journeyTopBarWasActive = journeyTopBar.activeSelf;
            journeyTopBar.SetActive(false);
        }

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
        Set(livesValue, lives.ToString());
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
        float rate     = games > 0 ? wins / (float)games : 0f;

        ApplyAllTimeColorProfile();
        Set(allTimePlayedValue, games.ToString(CultureInfo.InvariantCulture));
        Set(allTimeWonValue, wins.ToString(CultureInfo.InvariantCulture));
        Set(allTimeWinRateValue, (rate * 100f).ToString("0.0", CultureInfo.InvariantCulture) + "%");

        int[] gamesByDifficulty = new int[9];
        int[] winsByDifficulty = new int[9];
        List<GameRecord> records = GameDatabase.GetAll();
        if (records != null)
        {
            foreach (GameRecord record in records)
            {
                if (record == null || record.Difficulty < 0 || record.Difficulty >= gamesByDifficulty.Length)
                    continue;

                gamesByDifficulty[record.Difficulty]++;
                if (record.IsWon) winsByDifficulty[record.Difficulty]++;
            }
        }

        Set(allTimeTableHeader, "<color=#AFC5E9>DIFFICULTY<pos=42%>PLAYED<pos=62%>WON<pos=79%>WIN RATE</color>");

        string[] difficultyRows = new string[gamesByDifficulty.Length];
        for (int i = 0; i < gamesByDifficulty.Length; i++)
        {
            float difficultyRate = gamesByDifficulty[i] > 0
                ? winsByDifficulty[i] * 100f / gamesByDifficulty[i]
                : 0f;
            difficultyRows[i] = $"<color=#F3F6FF>{((SudokuDifficulty)i)}</color><pos=42%><color=#FFD66B>{gamesByDifficulty[i].ToString(CultureInfo.InvariantCulture)}</color>" +
                $"<pos=62%><color=#FFD66B>{winsByDifficulty[i].ToString(CultureInfo.InvariantCulture)}</color>" +
                $"<pos=79%><color=#FFD66B>{difficultyRate.ToString("0.#", CultureInfo.InvariantCulture)}%</color>";
        }

        Set(allTimeTableDetails, string.Join("\n", difficultyRows));

        // Streak chip
        if (streakNumber != null) streakNumber.text = $"{streak}";
        if (streakDesc   != null) streakDesc.text   = "GAME WIN STREAK";
    }

    private void ApplyAllTimeColorProfile()
    {
        Color gold = new Color32(255, 214, 107, 255);       // #FFD66B
        Color mutedBlue = new Color32(175, 197, 233, 255);  // #AFC5E9

        if (allTimeTitle != null) allTimeTitle.color = gold;
        if (allTimeTableHeader != null) allTimeTableHeader.color = mutedBlue;

        if (allTimeSummaryLabels != null)
        {
            foreach (RectTransform labelTransform in allTimeSummaryLabels)
            {
                TextMeshProUGUI label = labelTransform != null
                    ? labelTransform.GetComponent<TextMeshProUGUI>()
                    : null;
                if (label != null) label.color = mutedBlue;
            }
        }

        if (allTimeSummaryValues != null)
        {
            foreach (RectTransform valueTransform in allTimeSummaryValues)
            {
                TextMeshProUGUI value = valueTransform != null
                    ? valueTransform.GetComponent<TextMeshProUGUI>()
                    : null;
                if (value != null) value.color = gold;
            }
        }
    }

    private void RefreshProgression()
    {
        const int WindowSize = 5;
        const int RequiredWins = 4;
        const float RequiredEfficiency = 0.80f;

        List<GameRecord> recent = GameDatabase.GetLastNRecordByDate(WindowSize);
        SudokuDifficulty currentDifficulty = (SudokuDifficulty)_vm.GetDifficulty;

        if (currentDifficultyText != null)
        {
            currentDifficultyText.text =
                $"<color=#AFC5E9>Current:</color>\n" +
                $"<size=140%><b><color=#F3F6FF>{currentDifficulty.ToString().ToUpperInvariant()}</color></b></size>";
        }

        if (currentDifficulty == SudokuDifficulty.Hardest)
        {
            Set(progressionText, "<color=#FFD66B>TOP DIFFICULTY REACHED</color>");
            Set(progressionCountText, "<color=#AFC5E9>MAX</color>");
            SetProgressionBarFill(1f);
            return;
        }

        SudokuDifficulty nextDifficulty = (SudokuDifficulty)((int)currentDifficulty + 1);
        int maxScore = ScoringSystem.GetAbsoluteMaximumScore(currentDifficulty);
        List<GameRecord> tierGames = new List<GameRecord>();
        int wins = 0;
        int totalPoints = 0;

        if (recent != null)
        {
            foreach (GameRecord record in recent)
            {
                if (record == null || record.Difficulty != (int)currentDifficulty) break;
                tierGames.Add(record);
                if (record.IsWon) wins++;
                totalPoints += record.Points;
            }
        }

        int gamesPlayed = tierGames.Count;
        int gamesRemaining = Mathf.Max(0, WindowSize - gamesPlayed);
        int winsNeeded = Mathf.Max(0, RequiredWins - wins);
        float averageEfficiency = maxScore > 0
            ? totalPoints / (float)(WindowSize * maxScore)
            : 0f;

        string status;
        if (gamesPlayed == WindowSize && wins >= RequiredWins &&
            averageEfficiency >= RequiredEfficiency)
        {
            status = "PROMOTION TARGET REACHED";
        }
        else if (winsNeeded > gamesRemaining)
        {
            status = "BUILD A NEW 5-GAME RUN";
        }
        else if (winsNeeded > 0)
        {
            status = $"{winsNeeded} MORE WIN{(winsNeeded == 1 ? "" : "S")} TO {nextDifficulty.ToString().ToUpperInvariant()}";
        }
        else if (gamesPlayed < WindowSize)
        {
            status = $"WIN TARGET MET • KEEP {RequiredEfficiency * 100f:0}%+ EFFICIENCY";
        }
        else
        {
            status = $"KEEP {RequiredEfficiency * 100f:0}%+ EFFICIENCY TO {nextDifficulty.ToString().ToUpperInvariant()}";
        }

        Set(progressionText, $"<color=#FFD66B>{status}</color>");
        Set(progressionCountText,
            $"<color=#FFD66B>{wins}</color><color=#AFC5E9>/{RequiredWins}</color>");
        SetProgressionBarFill(wins / (float)RequiredWins);
    }

    private void SetProgressionBarFill(float fillAmount)
    {
        if (progressionBarFill == null) return;

        float fill = Mathf.Clamp01(fillAmount);
        progressionBarFill.anchorMin = new Vector2(0f, 0f);
        progressionBarFill.anchorMax = new Vector2(fill, 1f);
        progressionBarFill.offsetMin = Vector2.zero;
        progressionBarFill.offsetMax = Vector2.zero;
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
        if (gameplayTools != null) gameplayTools.SetActive(_gameplayToolsWasActive);
        if (gameplayGrid != null) gameplayGrid.SetActive(_gameplayGridWasActive);
        if (journeyTopBar != null) journeyTopBar.SetActive(_journeyTopBarWasActive);
        _vm.ResetHistoricalData.Execute();
        for(int i = 0; i < lstOfRecords.Count; i++)
            Destroy(lstOfRecords[i]);
        
        lstOfRecords.Clear();

        HomeScreenController home = canvas != null
            ? canvas.GetComponentInChildren<HomeScreenController>(true)
            : null;
        NewGameScreenController newGame = canvas != null
            ? canvas.GetComponentInChildren<NewGameScreenController>(true)
            : null;
        bool anotherScreenIsOpen =
            (home != null && home.gameObject.activeInHierarchy) ||
            (newGame != null && newGame.gameObject.activeInHierarchy);

        if (_verseTicker != null)
            _verseTicker.SetGameActive(_tickerWasActive && !anotherScreenIsOpen);
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
