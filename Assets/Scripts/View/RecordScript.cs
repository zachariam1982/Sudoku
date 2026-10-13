using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class RecordScript : MonoBehaviour, IPointerClickHandler
{
    // These are normalized positions in the record row so the summary keeps
    // the same proportions as its width changes between portrait/landscape.
    private const string DifficultySummaryPosition = "20%";
    private const string PointsSummaryPosition = "57%";

    private static RecordScript _expandedRecord;

    private int _Id;
    private JourneyViewModel _vm;
    private Button _retakeBtn;
    private GameObject _retakeButtonContainer;

    [Header("Summary")]
    [SerializeField] private TextMeshProUGUI SummaryText;
    [Header("Expanded Details")]
    [SerializeField] private GameObject DetailsPanel;
    [SerializeField] private TextMeshProUGUI DetailsText;
    [SerializeField] private TextMeshProUGUI ExpandArrow;
    [SerializeField] private GameObject RetakeButtonContainer;

    public void OnPointerClick(PointerEventData eventData)
    {
        // The row expands only when its chevron is tapped.
        if (ExpandArrow == null ||
            eventData.pointerCurrentRaycast.gameObject != ExpandArrow.gameObject)
            return;

        ToggleDetails();
    }

    public void Setup(JourneyViewModel viewModel, GameRecord record)
    {
        _Id = record.Id;
        _vm = viewModel;

        SudokuDifficulty difficulty = (SudokuDifficulty)record.Difficulty;
        string difficultyColor = "F3F6FF";

        if (ColorUtility.TryParseHtmlString(ColorMap.Color_Map[difficulty].bg, out Color color1) &&
            ColorUtility.TryParseHtmlString(ColorMap.Color_Map[difficulty].outline, out Color color2) &&
            ColorUtility.TryParseHtmlString(ColorMap.Color_Map[difficulty].txtClr, out Color color3))
        {
            Image rowBackground = GetComponent<Image>();
            if (rowBackground != null) rowBackground.color = color1;

            Outline outline = GetComponent<Outline>();
            if (outline != null) outline.effectColor = color2;

            difficultyColor = ColorUtility.ToHtmlStringRGB(color3);
        }

        if (SummaryText != null)
        {
            SummaryText.text =
                $"<pos=4%><color=#4A82FF>{record.Level}</color>" +
                $"<pos={DifficultySummaryPosition}><color=#{difficultyColor}>{difficulty}</color>" +
                $"<pos={PointsSummaryPosition}><color=#FFD66B>{record.Points}</color>";
        }

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

        if (ExpandArrow == null)
        {
            Transform arrowTransform = transform.Find("SummaryRow/Restart/ExpandArrow");
            if (arrowTransform != null)
                ExpandArrow = arrowTransform.GetComponent<TextMeshProUGUI>();
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
    }

    private void CollapseDetails()
    {
        if (DetailsPanel != null) DetailsPanel.SetActive(false);
        if (_retakeButtonContainer != null) _retakeButtonContainer.SetActive(false);
        SetExpandedArrow(false);

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
    }

    private void StateChange(string stateName)
    {
        if (_retakeBtn == null) return;
        _retakeBtn.interactable = stateName == "JourneyIdleState";
    }
}
