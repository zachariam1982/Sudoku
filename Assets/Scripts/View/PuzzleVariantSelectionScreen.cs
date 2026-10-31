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
    [SerializeField] private RectTransform[] variantIcons;
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
            float xMin = landscape ? 0.03f + (i % 3) * 0.32f : 0.05f;
            float xMax = landscape ? xMin + 0.30f : 0.95f;
            float yMin = landscape
                ? (i < 3 ? 0.37f : 0.04f)
                : 0.05f + (5 - i) * 0.115f;
            float yMax = landscape ? yMin + 0.30f : yMin + 0.105f;
            SetStretched(variantCards[i], xMin, yMin, xMax, yMax);

            if (variantIcons != null && i < variantIcons.Length && variantIcons[i] != null)
            {
                if (landscape)
                    SetStretched(variantIcons[i], 0.015f, 0.05f, 0.45f, 0.95f);
                else
                    SetStretched(variantIcons[i], 0.02f, 0.05f, 0.25f, 0.95f);
            }

            if (variantLabels != null && i < variantLabels.Length && variantLabels[i] != null)
            {
                SetStretched(variantLabels[i].rectTransform,
                    landscape ? 0.46f : 0.28f,
                    0.04f,
                    0.98f,
                    0.96f);
                variantLabels[i].fontSizeMax = landscape ? 58f : 52f;
            }
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
