//using System;
//using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
//using Microsoft.Unity.VisualStudio.Editor;

public class RecordScript : MonoBehaviour
{
    private int _Id;
    private SudokuViewModel _vm;
    private Button _retakeBtn;

    [Header("DB Values")]
    [SerializeField] private TextMeshProUGUI Level;
    [SerializeField] private TextMeshProUGUI Difficulty;
    [SerializeField] private TextMeshProUGUI Points;
    [SerializeField] private TextMeshProUGUI Restart;
    [Header("Expanded Details")]
    [SerializeField] private GameObject DetailsPanel;
    [SerializeField] private TextMeshProUGUI DetailsText;
    private static Dictionary<SudokuDifficulty, (string bg, string outline, string txtClr)> ColorMap_1 = new Dictionary<SudokuDifficulty, (string bg, string outline, string textClr)>()
    {
        { SudokuDifficulty.Simple,    ("#1D2438", "#2E6FA380", "#4682FF") },
        { SudokuDifficulty.Beginner,  ("#1D283F", "#3580B880", "#4682FF") },      
        { SudokuDifficulty.Easy,      ("#1D2B47", "#3E92CC80", "#4682FF") },
        { SudokuDifficulty.Novice,    ("#1D3040", "#4FB0A580", "#4682FF") },
        { SudokuDifficulty.Moderate,  ("#1D3630", "#5FBF6B80", "#4682FF") },
        { SudokuDifficulty.Advanced,  ("#2B2E20", "#C9C24C80", "#4682FF") },
        { SudokuDifficulty.Hard,      ("#332615", "#E39A3D80", "#4682FF") },
        { SudokuDifficulty.Expert,    ("#3B221D", "#FF624680", "#4682FF") },
        { SudokuDifficulty.Hardest,   ("#3A1A20", "#E23B5C80", "#4682FF") },
    };
    private static Dictionary<SudokuDifficulty, (string bg, string outline, string txtClr)> ColorMap_2 = new Dictionary<SudokuDifficulty, (string bg, string outline, string textClr)>()
    {
        { SudokuDifficulty.Simple,    ("#1D2B47", "#4A90E280", "#4A90E280") },
        { SudokuDifficulty.Beginner,  ("#1D2B47", "#3AADB380", "#3AADB380") },      
        { SudokuDifficulty.Easy,      ("#1D2B47", "#44BB8680", "#44BB8680") },
        { SudokuDifficulty.Novice,    ("#1D2B47", "#52C41A80", "#52C41A80") },
        { SudokuDifficulty.Moderate,  ("#1D2B47", "#D4B10680", "#D4B10680") },
        { SudokuDifficulty.Advanced,  ("#1D2B47", "#E67E2280", "#E67E2280") },
        { SudokuDifficulty.Hard,      ("#1D2B47", "#E74C3C80", "#E74C3C80") },
        { SudokuDifficulty.Expert,    ("#1D2B47", "#C0392B80", "#C0392B80") },
        { SudokuDifficulty.Hardest,   ("#1D2B47", "#9B59B680", "#9B59B680") },
    };
    private static Dictionary<SudokuDifficulty, (string bg, string outline, string txtClr)> ColorMap_3 = new Dictionary<SudokuDifficulty, (string bg, string outline, string textClr)>()
    {
        { SudokuDifficulty.Simple,    ("#1D2438", "#2E6FA380", "#4A90E280") },
        { SudokuDifficulty.Beginner,  ("#1D283F", "#3580B880", "#3AADB380") },      
        { SudokuDifficulty.Easy,      ("#1D2B47", "#3E92CC80", "#44BB8680") },
        { SudokuDifficulty.Novice,    ("#1D3040", "#4FB0A580", "#52C41A80") },
        { SudokuDifficulty.Moderate,  ("#1D3630", "#5FBF6B80", "#D4B10680") },
        { SudokuDifficulty.Advanced,  ("#2B2E20", "#C9C24C80", "#E67E2280") },
        { SudokuDifficulty.Hard,      ("#332615", "#E39A3D80", "#E74C3C80") },
        { SudokuDifficulty.Expert,    ("#3B221D", "#FF624680", "#C0392B80") },
        { SudokuDifficulty.Hardest,   ("#3A1A20", "#E23B5C80", "#9B59B680") },
    };

    public void Setup(SudokuViewModel viewModel, GameRecord record)
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
        if( UnityEngine.ColorUtility.TryParseHtmlString( RecordScript.ColorMap_3[difficulty].bg, out color_1) && 
            UnityEngine.ColorUtility.TryParseHtmlString( RecordScript.ColorMap_3[difficulty].outline, out color_2) && 
            UnityEngine.ColorUtility.TryParseHtmlString( RecordScript.ColorMap_3[difficulty].txtClr, out color_3))
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
        if (DetailsPanel != null)
            DetailsPanel.SetActive(!DetailsPanel.activeSelf);
    }
    public void OnDestroy()
    {
        _vm.CurrentStateName.OnChanged -= StateChange;
    }

    private void StateChange(string stateName)
    {      
        if( _retakeBtn == null) return; 

        if(stateName == "IdleState")
        {
            _retakeBtn.interactable = true;
        }
        else
        {
            _retakeBtn.interactable = false;
        }
    }
}
