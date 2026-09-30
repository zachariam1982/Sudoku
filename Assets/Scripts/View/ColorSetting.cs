using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ColorSetting : MonoBehaviour
{
    [SerializeField] private SudokuDifficulty difficulty = SudokuDifficulty.Easy;
    [SerializeField] private TextMeshProUGUI Label;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Color color_1, color_2, color_3;
        if( UnityEngine.ColorUtility.TryParseHtmlString( ColorMap.Color_Map[difficulty].bg, out color_1) && 
            UnityEngine.ColorUtility.TryParseHtmlString( ColorMap.Color_Map[difficulty].outline, out color_2) && 
            UnityEngine.ColorUtility.TryParseHtmlString( ColorMap.Color_Map[difficulty].txtClr, out color_3))
        {
            var rowBackGround = gameObject.GetComponent<Image>();
            rowBackGround.color = color_1;

            var outline = gameObject.GetComponent<Outline>();
            outline.effectColor = color_2;

            Label.color = color_3;
        }
    }
}
