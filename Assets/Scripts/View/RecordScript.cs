using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class RecordScript : MonoBehaviour
{
    // Summary positions are normalized to the row width. Portrait rows use
    // wider spacing between difficulty and points to avoid text/icon overlap.
    private const float LandscapeDifficultyPosition = 0.20f;
    private const float LandscapePointsPosition = 0.57f;
    private const float PortraitDifficultyPosition = 0.23f;
    private const float PortraitPointsPosition = 0.70f;
    private const float SummaryIconGap = 4f;
    private const float LandscapeResultIconGap = 20f;
    private const float PortraitResultIconGap = 12f;
    private const float RestartInlineGap = 8f;
    private const float RestartRowHeight = 84f;

    private static RecordScript _expandedRecord;

    private int _Id;
    private JourneyViewModel _vm;
    private Button _retakeBtn;
    private Button _expandArrowButton;
    private GameObject _retakeButtonContainer;
    private RectTransform _difficultyIcon;
    private RectTransform _pointsIcon;
    private Image _resultIcon;
    private RectTransform _resultIconRect;
    private RectTransform _summaryRowRect;
    private RectTransform _retakeRect;
    private GameObject _retakeBelowRow;
    private LayoutElement _retakeLayoutElement;
    private TextMeshProUGUI _retakeText;
    private float _retakeButtonWidth;
    private bool _retakeIsBelowRow;
    private bool _hasAppliedRetakePlacement;
    private bool _lastRetakeLayoutWasPortrait;
    private float _lastRetakeRowWidth;
    private float _lastRetakeButtonWidth;
    private int _summaryLevel;
    private int _summaryPoints;
    private string _summaryDifficulty;
    private string _summaryDifficultyColor;
    private bool _hasAppliedSummaryLayout;
    private bool _lastSummaryLayoutWasPortrait;

    [Header("Summary")]
    [SerializeField] private TextMeshProUGUI SummaryText;
    [SerializeField] private Image ResultIcon;
    [SerializeField] private Sprite WonSprite;
    [SerializeField] private Sprite LostSprite;
    [Header("Expanded Details")]
    [SerializeField] private GameObject DetailsPanel;
    [SerializeField] private TextMeshProUGUI DetailsText;
    [SerializeField] private TextMeshProUGUI ExpandArrow;
    [SerializeField] private GameObject RetakeButtonContainer;

    public void Setup(JourneyViewModel viewModel, GameRecord record)
    {
        _Id = record.Id;
        _vm = viewModel;

        SudokuDifficulty difficulty = (SudokuDifficulty)record.Difficulty;
        string difficultyColor = "F3F6FF";
        _summaryLevel = record.Level;
        _summaryPoints = record.Points;
        _summaryDifficulty = difficulty.ToString();

        if (ColorUtility.TryParseHtmlString(ColorMap.Color_Map[difficulty].bg, out Color color1) &&
            ColorUtility.TryParseHtmlString(ColorMap.Color_Map[difficulty].outline, out Color color2) &&
            ColorUtility.TryParseHtmlString(ColorMap.Color_Map[difficulty].txtClr, out Color color3))
        {
            //Image rowBackground = GetComponent<Image>();
            //if (rowBackground != null) rowBackground.color = color1;

            Outline outline = GetComponent<Outline>();
            if (outline != null) outline.effectColor = color2;

            difficultyColor = ColorUtility.ToHtmlStringRGB(color3);
        }

        _summaryDifficultyColor = difficultyColor;
        _summaryRowRect = SummaryText != null ? SummaryText.rectTransform : null;
        _difficultyIcon = transform.Find("SummaryRow/DifficultyIcon") as RectTransform;
        _pointsIcon = transform.Find("SummaryRow/PointsIcon") as RectTransform;
        _resultIcon = ResultIcon != null
            ? ResultIcon
            : transform.Find("SummaryRow/ResultIcon")?.GetComponent<Image>();
        _resultIconRect = _resultIcon != null ? _resultIcon.rectTransform : null;
        if (_resultIcon != null)
        {
            _resultIcon.sprite = record.IsWon ? WonSprite : LostSprite;
            _resultIcon.gameObject.SetActive(_resultIcon.sprite != null);
        }
        ApplyResponsiveSummaryLayout(Screen.height > Screen.width);

        int totalSeconds = Mathf.FloorToInt(record.ElapsedSeconds);
        string completed = !string.IsNullOrEmpty(record.CompletedAt) &&
                            record.CompletedAt.Length >= 10
            ? record.CompletedAt.Substring(0, 10)
            : record.CompletedAt;

        if (DetailsText != null)
        {
            string labelColor = "#9FB8D8";
            string valueColor = "#FFD66A";
            string resultColor = record.IsWon ? "#83E6B1" : "#FF9B9B";
            string time = $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";

            // Use a fixed second-column start rather than runs of spaces, which
            // drift with the proportional font and make the details hard to scan.
            DetailsText.text =
                $"<pos=20%><color={labelColor}>RESULT</color>  <color={resultColor}><b>{(record.IsWon ? "WON" : "LOSS")}</b></color>" +
                $"<pos=60%><color={labelColor}>TIME</color>  <color={valueColor}><b>{time}</b></color>\n" +
                $"<pos=20%><color={labelColor}>LIVES</color>  <color={valueColor}><b>{record.LivesRemaining}</b></color>" +
                $"<pos=60%><color={labelColor}>UNDO</color>  <color={valueColor}><b>{record.UndoUses}</b></color>\n" +
                $"<pos=20%><color={labelColor}>PENCIL</color>  <color={valueColor}><b>{record.PencilUses}</b></color>" +
                $"<pos=60%><color={labelColor}>ERASE</color>  <color={valueColor}><b>{record.EraseUses}</b></color>\n" +
                $"<pos=20%><color={labelColor}>SOS</color>  <color={valueColor}><b>{record.SOSUses}</b></color>" +
                $"<pos=60%><color={labelColor}>AUTO-FILL</color>  <color={valueColor}><b>{record.AutoFillUses}</b></color>\n" +
                $"<align=center><color={labelColor}>COMPLETED</color>  <color={valueColor}><b>{completed}</b></color></align>";
            DetailsText.fontSize = 28f;
            DetailsText.fontSizeMax = 28f;
            DetailsText.lineSpacing = 3f;
        }

        Transform retakeTransform = transform.Find("SummaryRow/Restart/Value");
        _retakeButtonContainer = RetakeButtonContainer != null
            ? RetakeButtonContainer
            : retakeTransform != null ? retakeTransform.gameObject : null;
        _retakeBtn = retakeTransform != null
            ? retakeTransform.GetComponent<Button>()
            : null;
        if (retakeTransform != null)
        {
            _retakeRect = retakeTransform as RectTransform;
            _retakeLayoutElement = retakeTransform.GetComponent<LayoutElement>();
            _retakeText = retakeTransform.GetComponent<TextMeshProUGUI>();
            CreateRetakeBelowRow();
        }

        if (ExpandArrow == null)
        {
            Transform arrowTransform = transform.Find("SummaryRow/Restart/ExpandArrow");
            if (arrowTransform != null)
                ExpandArrow = arrowTransform.GetComponent<TextMeshProUGUI>();
        }

        if (ExpandArrow != null)
        {
            ExpandArrow.raycastTarget = true;
            _expandArrowButton = ExpandArrow.GetComponent<Button>();
            if (_expandArrowButton == null)
                _expandArrowButton = ExpandArrow.gameObject.AddComponent<Button>();

            _expandArrowButton.transition = Selectable.Transition.None;
            _expandArrowButton.targetGraphic = ExpandArrow;
            Navigation arrowNavigation = _expandArrowButton.navigation;
            arrowNavigation.mode = Navigation.Mode.None;
            _expandArrowButton.navigation = arrowNavigation;
            _expandArrowButton.onClick.AddListener(ToggleDetails);

            VerticalLayoutGroup restartLayout = ExpandArrow.GetComponentInParent<VerticalLayoutGroup>();
            if (restartLayout != null)
                restartLayout.childAlignment = TextAnchor.MiddleCenter;
        }

        if (DetailsPanel != null) DetailsPanel.SetActive(false);
        if (_retakeButtonContainer != null) _retakeButtonContainer.SetActive(true);
        SetExpandedArrow(false);

        StateChange(_vm.CurrentStateName.Value);
        _vm.CurrentStateName.OnChanged += StateChange;

        if (_retakeBtn != null)
        {
            _retakeBtn.onClick.AddListener(() =>
                _vm.RetryOlderGameCommand.Execute((record.Id, record.Level, record.Difficulty, record.Points)));
        }
    }

    private void LateUpdate()
    {
        if (SummaryText == null || _summaryDifficulty == null)
            return;

        bool isPortrait = Screen.height > Screen.width;
        if (!_hasAppliedSummaryLayout || isPortrait != _lastSummaryLayoutWasPortrait)
            ApplyResponsiveSummaryLayout(isPortrait);

        ApplyResponsiveRetakePlacement();
    }

    private void ApplyResponsiveSummaryLayout(bool isPortrait)
    {
        if (SummaryText == null || _summaryDifficulty == null)
            return;

        float difficultyPosition = isPortrait
            ? PortraitDifficultyPosition
            : LandscapeDifficultyPosition;
        float pointsPosition = isPortrait
            ? PortraitPointsPosition
            : LandscapePointsPosition;

        SummaryText.text =
            $"<pos=4%><color=#4A82FF>{_summaryLevel}</color>" +
            $"<pos={difficultyPosition * 100f:0}%><color=#{_summaryDifficultyColor}>{_summaryDifficulty}</color>" +
            $"<pos={pointsPosition * 100f:0}%><color=#FFD66B>{_summaryPoints}</color>";

        // Keep the landscape typography unchanged, and reduce only the
        // portrait summary slightly to preserve a clear gap between columns.
        float summaryFontSize = isPortrait ? 56f : 64f;
        SummaryText.fontSize = summaryFontSize;
        SummaryText.fontSizeMax = summaryFontSize;

        PositionSummaryIcon(_difficultyIcon, difficultyPosition);
        PositionSummaryIcon(_pointsIcon, pointsPosition);
        PositionResultIcon(isPortrait, pointsPosition);

        _lastSummaryLayoutWasPortrait = isPortrait;
        _hasAppliedSummaryLayout = true;
    }

    private void CreateRetakeBelowRow()
    {
        if (_retakeRect == null || _retakeBelowRow != null)
            return;

        _retakeBelowRow = new GameObject(
            "RetakeBelowRow",
            typeof(RectTransform),
            typeof(LayoutElement),
            typeof(HorizontalLayoutGroup));
        RectTransform rowRect = _retakeBelowRow.GetComponent<RectTransform>();
        rowRect.SetParent(transform, false);
        rowRect.SetSiblingIndex(_summaryRowRect != null
            ? _summaryRowRect.GetSiblingIndex() + 1
            : 1);

        LayoutElement rowElement = _retakeBelowRow.GetComponent<LayoutElement>();
        rowElement.minHeight = RestartRowHeight;
        rowElement.preferredHeight = RestartRowHeight;
        rowElement.flexibleHeight = 0f;
        rowElement.flexibleWidth = 1f;

        HorizontalLayoutGroup rowLayout = _retakeBelowRow.GetComponent<HorizontalLayoutGroup>();
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        _retakeBelowRow.SetActive(false);
    }

    private void ApplyResponsiveRetakePlacement()
    {
        if (_summaryRowRect == null || _retakeRect == null || _retakeBelowRow == null)
            return;

        float rowWidth = _summaryRowRect.rect.width;
        if (rowWidth <= 0f)
            return;

        // Keep Retake inline only when it clears the result icon with a
        // readable gap. Otherwise reserve a full row beneath the summary.
        bool isPortrait = Screen.height > Screen.width;
        if (_retakeText != null)
        {
            float fontSize = isPortrait ? 28f : 30f;
            if (!Mathf.Approximately(_retakeText.fontSize, fontSize))
            {
                _retakeText.fontSize = fontSize;
                _retakeText.fontSizeMax = fontSize;
            }
        }
        float pointsPosition = isPortrait ? PortraitPointsPosition : LandscapePointsPosition;
        float resultIconGap = isPortrait ? PortraitResultIconGap : LandscapeResultIconGap;
        float pointsWidth = SummaryText.GetPreferredValues("999").x;
        float iconWidth = _resultIconRect != null ? _resultIconRect.rect.width : 52f;
        float resultRight = (pointsPosition - 0.5f) * rowWidth +
                            pointsWidth + resultIconGap + iconWidth;
        Transform restartTransform = _expandArrowButton != null
            ? _expandArrowButton.transform.parent
            : transform.Find("SummaryRow/Restart");
        RectTransform restartRect = restartTransform as RectTransform;
        if (restartRect == null)
            return;

        float restartLeft = (restartRect.anchorMin.x - 0.5f) * rowWidth - restartRect.rect.width * 0.5f;
        float availableWidth = restartLeft - resultRight - RestartInlineGap * 2f;
        float preferredTextWidth = _retakeText != null
            ? _retakeText.GetPreferredValues("RETAKE").x
            : 96f;
        _retakeButtonWidth = Mathf.Max(88f, preferredTextWidth + 16f);
        bool needsBelowRow = availableWidth < _retakeButtonWidth;

        bool placementChanged = !_hasAppliedRetakePlacement ||
                                needsBelowRow != _retakeIsBelowRow ||
                                isPortrait != _lastRetakeLayoutWasPortrait ||
                                Mathf.Abs(rowWidth - _lastRetakeRowWidth) > 1f ||
                                Mathf.Abs(_retakeButtonWidth - _lastRetakeButtonWidth) > 1f;
        if (!placementChanged)
            return;

        if (needsBelowRow)
        {
            if (!_retakeIsBelowRow)
                _retakeRect.SetParent(_retakeBelowRow.transform, false);
            _retakeBelowRow.SetActive(true);
            ConfigureRetakeRect(_retakeBelowRow.GetComponent<RectTransform>(), _retakeButtonWidth, RestartRowHeight);
        }
        else
        {
            if (_retakeIsBelowRow)
            {
                _retakeBelowRow.SetActive(false);
                _retakeRect.SetParent(_summaryRowRect, false);
            }
            PositionRetakeInline(resultRight, restartLeft, availableWidth);
        }

        _retakeIsBelowRow = needsBelowRow;
        _hasAppliedRetakePlacement = true;
        _lastRetakeLayoutWasPortrait = isPortrait;
        _lastRetakeRowWidth = rowWidth;
        _lastRetakeButtonWidth = _retakeButtonWidth;
        LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
    }

    private void PositionRetakeInline(float resultRight, float restartLeft, float availableWidth)
    {
        float width = Mathf.Min(_retakeButtonWidth, availableWidth);
        ConfigureRetakeRect(_summaryRowRect, width, RestartRowHeight);
        _retakeRect.anchoredPosition = new Vector2((resultRight + restartLeft) * 0.5f, 0f);

    }

    private void ConfigureRetakeRect(RectTransform parent, float width, float height)
    {
        _retakeRect.SetParent(parent, false);
        _retakeRect.anchorMin = new Vector2(0.5f, 0.5f);
        _retakeRect.anchorMax = new Vector2(0.5f, 0.5f);
        _retakeRect.anchoredPosition = Vector2.zero;
        _retakeRect.sizeDelta = new Vector2(width, height);

        if (_retakeLayoutElement != null)
        {
            _retakeLayoutElement.minWidth = width;
            _retakeLayoutElement.preferredWidth = width;
            _retakeLayoutElement.flexibleWidth = 0f;
            _retakeLayoutElement.preferredHeight = height;
            _retakeLayoutElement.flexibleHeight = 0f;
        }
    }

    private void PositionResultIcon(bool isPortrait, float pointsPosition)
    {
        if (SummaryText == null || _resultIconRect == null || !_resultIcon.gameObject.activeSelf)
            return;

        // Reserve space for the widest expected score (three digits) so the
        // result icon stays aligned consistently across records.
        float pointsWidth = SummaryText.GetPreferredValues("999").x;
        float iconWidth = _resultIconRect.sizeDelta.x;
        Vector2 anchorMin = _resultIconRect.anchorMin;
        Vector2 anchorMax = _resultIconRect.anchorMax;
        anchorMin.x = pointsPosition;
        anchorMax.x = pointsPosition;
        _resultIconRect.anchorMin = anchorMin;
        _resultIconRect.anchorMax = anchorMax;
        _resultIconRect.anchoredPosition = new Vector2(
            pointsWidth + (isPortrait ? PortraitResultIconGap : LandscapeResultIconGap) + iconWidth * 0.5f,
            0f);
    }

    private static void PositionSummaryIcon(RectTransform icon, float textPosition)
    {
        if (icon == null)
            return;

        Vector2 anchorMin = icon.anchorMin;
        Vector2 anchorMax = icon.anchorMax;
        anchorMin.x = textPosition;
        anchorMax.x = textPosition;
        icon.anchorMin = anchorMin;
        icon.anchorMax = anchorMax;
        icon.anchoredPosition = new Vector2(-icon.rect.width * 0.5f - SummaryIconGap, 0f);
    }

    public void ToggleDetails()
    {
        if (DetailsPanel == null) return;

        if (!DetailsPanel.activeSelf)
        {
            if (_expandedRecord != null && _expandedRecord != this)
                _expandedRecord.CollapseDetails();

            DetailsPanel.SetActive(true);
            SetExpandedArrow(true);
            _expandedRecord = this;
        }
        else
        {
            CollapseDetails();
        }

        LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
    }

    private void CollapseDetails()
    {
        if (DetailsPanel != null) DetailsPanel.SetActive(false);
        SetExpandedArrow(false);

        LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);

        if (_expandedRecord == this)
            _expandedRecord = null;
    }

    private void SetExpandedArrow(bool expanded)
    {
        if (ExpandArrow != null)
            ExpandArrow.text = expanded ? "⌄" : ">";
    }

    private void OnDestroy()
    {
        if (_expandedRecord == this)
            _expandedRecord = null;

        if (_vm != null)
            _vm.CurrentStateName.OnChanged -= StateChange;

        if (_expandArrowButton != null)
            _expandArrowButton.onClick.RemoveListener(ToggleDetails);
    }

    private void StateChange(string stateName)
    {
        if (_retakeBtn == null) return;
        _retakeBtn.interactable = stateName == "JourneyIdleState";
    }
}
