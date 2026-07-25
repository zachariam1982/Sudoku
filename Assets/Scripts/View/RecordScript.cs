using System;
using Unity.VisualScripting;
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

    public void Setup(SudokuViewModel viewModel, int Id, string arg1, SudokuDifficulty arg2, string arg3)
    {
        _Id = Id;
        Level.text = arg1;
        Difficulty.text = arg2.ToString();
        Points.text = arg3;
        _vm = viewModel;

        Color color_1, color_2, color_3;
        if( UnityEngine.ColorUtility.TryParseHtmlString( RecordScript.ColorMap_3[arg2].bg, out color_1) && 
            UnityEngine.ColorUtility.TryParseHtmlString( RecordScript.ColorMap_3[arg2].outline, out color_2) && 
            UnityEngine.ColorUtility.TryParseHtmlString( RecordScript.ColorMap_3[arg2].txtClr, out color_3))
        {
            var rowBackGround = gameObject.GetComponent<Image>();
            rowBackGround.color = color_1;

            var outline = gameObject.GetComponent<Outline>();
            outline.effectColor = color_2;

            Difficulty.color = color_3;
        }

        _retakeBtn = transform.Find("Restart/Value").GetComponent<Button>();

        StateChange(_vm.CurrentStateName.Value);

        //Register for state change.
        _vm.CurrentStateName.OnChanged += StateChange;
    }

    public void OnDestroy()
    {
        _vm.CurrentStateName.OnChanged -= StateChange;
    }

    private void StateChange(string stateName)
    {      
        if(stateName == "IdleState")
        {
            if( _retakeBtn != null) _retakeBtn.interactable = true;
        }
        else
        {
            if( _retakeBtn != null) _retakeBtn.interactable = false;
        }
    }
}
