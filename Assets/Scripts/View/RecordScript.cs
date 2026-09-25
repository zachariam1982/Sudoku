//using System;
//using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;
//using Microsoft.Unity.VisualStudio.Editor;

public class RecordScript : MonoBehaviour, IPointerClickHandler
{
    private static RecordScript _expandedRecord;

    private int _Id;
    private JourneyViewModel _vm;
    private Button _retakeBtn;

    [Header("DB Values")]
    [SerializeField] private TextMeshProUGUI Level;
    [SerializeField] private TextMeshProUGUI Difficulty;
    [SerializeField] private TextMeshProUGUI Points;
    [SerializeField] private TextMeshProUGUI Restart;
    [Header("Expanded Details")]
    [SerializeField] private GameObject DetailsPanel;
    [SerializeField] private TextMeshProUGUI DetailsText;


    public void OnPointerClick(PointerEventData eventData)
    {
        ToggleDetails();
    }

    public void Setup(JourneyViewModel viewModel, GameRecord record)
    {
        _Id = record.Id;
        _vm = viewModel;

        Level.text = record.Level.ToString();
        Difficulty.text = ((SudokuDifficulty)record.Difficulty).ToString();
        Points.text = record.Points.ToString();

        int totalSeconds = Mathf.FloorToInt(record.ElapsedSeconds);
        string completed = !string.IsNullOrEmpty(record.CompletedAt) && 
                            record.CompletedAt.Length >= 10 ? record.CompletedAt.Substring(0, 10) : record.CompletedAt;

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

        SudokuDifficulty difficulty = (SudokuDifficulty)record.Difficulty;

        Color color_1, color_2, color_3;
        if( UnityEngine.ColorUtility.TryParseHtmlString( ColorMap.Color_Map[difficulty].bg, out color_1) && 
            UnityEngine.ColorUtility.TryParseHtmlString( ColorMap.Color_Map[difficulty].outline, out color_2) && 
            UnityEngine.ColorUtility.TryParseHtmlString( ColorMap.Color_Map[difficulty].txtClr, out color_3))
        {
            var rowBackGround = gameObject.GetComponent<Image>();
            rowBackGround.color = color_1;

            var outline = gameObject.GetComponent<Outline>();
            outline.effectColor = color_2;

            Difficulty.color = color_3;
        }

        _retakeBtn = transform.Find("SummaryRow/Restart/Value").GetComponent<Button>();

        StateChange(_vm.CurrentStateName.Value);

        //Register for state change.
        _vm.CurrentStateName.OnChanged += StateChange;
        _retakeBtn.onClick.AddListener(() => 
        {
            Debug.Log($"RETRY FEATURE: Button clicked for Id = {record.Id}");
            _vm.RetryOlderGameCommand.Execute((record.Id, record.Level, record.Difficulty, record.Points));
        });
    }

    public void ToggleDetails()
    {
        if (DetailsPanel == null) return;

        bool shouldExpand = !DetailsPanel.activeSelf;

        if (shouldExpand)
        {
            if (_expandedRecord != null && _expandedRecord != this)
                _expandedRecord.CollapseDetails();

            DetailsPanel.SetActive(true);
            _expandedRecord = this;
        }
        else
        {
            CollapseDetails();
        }
    }

    private void CollapseDetails()
    {
        if (DetailsPanel != null)
            DetailsPanel.SetActive(false);

        if (_expandedRecord == this)
            _expandedRecord = null;
    }
    public void OnDestroy()
    {
        if (_expandedRecord == this)
            _expandedRecord = null;

        if (_vm != null)
            _vm.CurrentStateName.OnChanged -= StateChange;
    }

    private void StateChange(string stateName)
    {      
        if( _retakeBtn == null) return; 

        if(stateName == "JourneyIdleState")
        {
            _retakeBtn.interactable = true;
        }
        else
        {
            _retakeBtn.interactable = false;
        }
    }
}
