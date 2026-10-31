using TMPro;
using UnityEngine;

/// <summary>
/// Layout and navigation for the scene-authored puzzle variant selector.
/// Cards, labels, icons, and button wiring remain serialized in SudokuGame.unity.
/// </summary>
public sealed class PuzzleVariantSelectionScreen : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private RectTransform[] variantCards;
    [SerializeField] private TextMeshProUGUI[] variantLabels;
    [SerializeField] private GameObject classicHomeScreen;

    private bool _layoutApplied;
    private bool _lastLandscape;

    private void OnEnable()
    {
        _layoutApplied = false;
        ApplyLayout();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (isActiveAndEnabled) ApplyLayout();
    }

    public void OpenClassic()
    {
        if (classicHomeScreen != null) classicHomeScreen.SetActive(true);
        gameObject.SetActive(false);
    }

    private void ApplyLayout()
    {
        if (headerText == null || variantCards == null || variantCards.Length != 6) return;

        bool landscape = Screen.width > Screen.height;
        if (_layoutApplied && _lastLandscape == landscape) return;

        _layoutApplied = true;
        _lastLandscape = landscape;

        if (landscape)
        {
            SetStretched(headerText.rectTransform, 0.08f, 0.70f, 0.92f, 0.98f);
            headerText.text =
                "<color=#FFBE4C><size=36><b>YOUR NEXT PUZZLE</b></size></color>\n" +
                "<size=150><b>S U D O K U</b></size>\n" +
                "<size=30><color=#AAACCD>Choose a puzzle variant</color></size>";
        }
        else
        {
            SetStretched(headerText.rectTransform, 0.05f, 0.78f, 0.95f, 0.98f);
            headerText.text =
                "<color=#FFBE4C><size=24><b>YOUR NEXT PUZZLE</b></size></color>\n" +
                "<size=82><b>SUDOKU</b></size>\n" +
                "<size=26><color=#AAACCD>Choose a puzzle variant</color></size>";
        }

        for (int i = 0; i < variantCards.Length; i++)
        {
            int column = landscape ? i % 3 : i % 2;
            int row = landscape ? i / 3 : i / 2;

            float xMin = landscape ? 0.03f + column * 0.32f : 0.035f + column * 0.48f;
            float xMax = landscape ? xMin + 0.30f : xMin + 0.45f;
            float yMin = landscape ? (row == 0 ? 0.37f : 0.04f) : 0.52f - row * 0.24f;
            float yMax = landscape ? yMin + 0.30f : yMin + 0.23f;
            SetStretched(variantCards[i], xMin, yMin, xMax, yMax);

            if (variantLabels != null && i < variantLabels.Length && variantLabels[i] != null)
                variantLabels[i].fontSizeMax = landscape ? 44f : 38f;
        }
    }

    private static void SetStretched(RectTransform target, float xMin, float yMin, float xMax, float yMax)
    {
        target.anchorMin = new Vector2(xMin, yMin);
        target.anchorMax = new Vector2(xMax, yMax);
        target.offsetMin = Vector2.zero;
        target.offsetMax = Vector2.zero;
    }
}
