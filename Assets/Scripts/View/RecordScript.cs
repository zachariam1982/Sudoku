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
    private const float RestartInlineGap = 12f;
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
    private RectTransform _restartRect;
    private RectTransform _summaryRowRect;
    private Transform _originalRestartParent;
    private GameObject _restartBelowRow;
    private LayoutElement _restartLayoutElement;
    private LayoutElement _restartBelowRowLayoutElement;
    private Vector2 _inlineRestartAnchorMin;
    private Vector2 _inlineRestartAnchorMax;
    private Vector2 _inlineRestartAnchoredPosition;
    private Vector2 _inlineRestartSizeDelta;
    private float _originalRestartPreferredWidth;
    private float _originalRestartPreferredHeight;
    private float _originalRestartFlexibleWidth;
    private float _originalRestartFlexibleHeight;
    private bool _restartIsBelowRow;
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
            DetailsText.text =
                $"Result: {(record.IsWon ? "Win" : "Loss")}" +
                $"      Time: {totalSeconds / 60:00}:{totalSeconds % 60:00}" +
                $"      Lives: {record.LivesRemaining}\n" +
                $"Undo: {record.UndoUses}" +
                $"      Pencil: {record.PencilUses}" +
                $"      Erase: {record.EraseUses}\n" +
                $"SOS: {record.SOSUses}" +
                $"      Auto Fill: {record.AutoFillUses}\n" +
                $"Completed: {completed}";
        }

        Transform retakeTransform = transform.Find("SummaryRow/Restart/Value");
        _retakeButtonContainer = RetakeButtonContainer != null
            ? RetakeButtonContainer
            : retakeTransform != null ? retakeTransform.gameObject : null;
        _retakeBtn = retakeTransform != null
            ? retakeTransform.GetComponent<Button>()
            : null;

        Transform restartTransform = transform.Find("SummaryRow/Restart");
        if (restartTransform != null)
        {
            _restartRect = restartTransform as RectTransform;
            _originalRestartParent = restartTransform.parent;
            _restartLayoutElement = restartTransform.GetComponent<LayoutElement>();
            if (_restartRect != null)
            {
                _inlineRestartAnchorMin = _restartRect.anchorMin;
                _inlineRestartAnchorMax = _restartRect.anchorMax;
                _inlineRestartAnchoredPosition = _restartRect.anchoredPosition;
                _inlineRestartSizeDelta = _restartRect.sizeDelta;
            }
            if (_restartLayoutElement != null)
            {
                _originalRestartPreferredWidth = _restartLayoutElement.preferredWidth;
                _originalRestartPreferredHeight = _restartLayoutElement.preferredHeight;
                _originalRestartFlexibleWidth = _restartLayoutElement.flexibleWidth;
                _originalRestartFlexibleHeight = _restartLayoutElement.flexibleHeight;
            }

            CreateRestartBelowRow();
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
        }

        if (DetailsPanel != null) DetailsPanel.SetActive(false);
        if (_retakeButtonContainer != null) _retakeButtonContainer.SetActive(false);
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

        ApplyResponsiveRestartPlacement();
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

    private void CreateRestartBelowRow()
    {
        if (_restartRect == null || _restartBelowRow != null)
            return;

        _restartBelowRow = new GameObject(
            "RestartBelowRow",
            typeof(RectTransform),
            typeof(LayoutElement),
            typeof(HorizontalLayoutGroup));
        RectTransform rowRect = _restartBelowRow.GetComponent<RectTransform>();
        rowRect.SetParent(transform, false);
        rowRect.SetSiblingIndex(_summaryRowRect != null
            ? _summaryRowRect.GetSiblingIndex() + 1
            : 1);

        _restartBelowRowLayoutElement = _restartBelowRow.GetComponent<LayoutElement>();
        _restartBelowRowLayoutElement.minHeight = RestartRowHeight;
        _restartBelowRowLayoutElement.preferredHeight = RestartRowHeight;
        _restartBelowRowLayoutElement.flexibleHeight = 0f;
        _restartBelowRowLayoutElement.flexibleWidth = 1f;

        HorizontalLayoutGroup rowLayout = _restartBelowRow.GetComponent<HorizontalLayoutGroup>();
        rowLayout.padding = new RectOffset(0, 28, 0, 0);
        rowLayout.childAlignment = TextAnchor.MiddleRight;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        _restartBelowRow.SetActive(false);
    }

    private void ApplyResponsiveRestartPlacement()
    {
        if (_summaryRowRect == null || _restartRect == null || _restartBelowRow == null)
            return;

        float rowWidth = _summaryRowRect.rect.width;
        if (rowWidth <= 0f)
            return;

        // Keep Restart inline only when it clears the result icon with a
        // readable gap. Otherwise reserve a full row beneath the summary.
        bool isPortrait = Screen.height > Screen.width;
        float pointsPosition = isPortrait ? PortraitPointsPosition : LandscapePointsPosition;
        float resultIconGap = isPortrait ? PortraitResultIconGap : LandscapeResultIconGap;
        float pointsWidth = SummaryText.GetPreferredValues("999").x;
        float iconWidth = _resultIconRect != null ? _resultIconRect.rect.width : 52f;
        float resultRight = (pointsPosition - 0.5f) * rowWidth +
                            pointsWidth + resultIconGap + iconWidth;
        float restartWidth = _inlineRestartSizeDelta.x;
        float restartLeft = (_inlineRestartAnchorMin.x - 0.5f) * rowWidth - restartWidth * 0.5f;
        bool needsBelowRow = restartLeft - resultRight < RestartInlineGap;

        if (needsBelowRow == _restartIsBelowRow)
            return;

        if (needsBelowRow)
        {
            _restartRect.SetParent(_restartBelowRow.transform, false);
            _restartBelowRow.SetActive(true);
            _restartRect.anchorMin = new Vector2(0.5f, 0.5f);
            _restartRect.anchorMax = new Vector2(0.5f, 0.5f);
            _restartRect.anchoredPosition = Vector2.zero;
            _restartRect.sizeDelta = _inlineRestartSizeDelta;
            if (_restartLayoutElement != null)
            {
                _restartLayoutElement.preferredWidth = _inlineRestartSizeDelta.x;
                _restartLayoutElement.preferredHeight = RestartRowHeight;
                _restartLayoutElement.flexibleWidth = 0f;
                _restartLayoutElement.flexibleHeight = 0f;
            }
        }
        else
        {
            _restartBelowRow.SetActive(false);
            _restartRect.SetParent(_originalRestartParent, false);
            _restartRect.anchorMin = _inlineRestartAnchorMin;
            _restartRect.anchorMax = _inlineRestartAnchorMax;
            _restartRect.anchoredPosition = _inlineRestartAnchoredPosition;
            _restartRect.sizeDelta = _inlineRestartSizeDelta;
            if (_restartLayoutElement != null)
            {
                _restartLayoutElement.preferredWidth = _originalRestartPreferredWidth;
                _restartLayoutElement.preferredHeight = _originalRestartPreferredHeight;
                _restartLayoutElement.flexibleWidth = _originalRestartFlexibleWidth;
                _restartLayoutElement.flexibleHeight = _originalRestartFlexibleHeight;
            }
        }

        _restartIsBelowRow = needsBelowRow;
        LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
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
            if (_retakeButtonContainer != null) _retakeButtonContainer.SetActive(true);
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
        if (_retakeButtonContainer != null) _retakeButtonContainer.SetActive(false);
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