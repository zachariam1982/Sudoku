using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class NewGameRecordRow : MonoBehaviour, IPointerClickHandler
{
    private static NewGameRecordRow _expandedRow;

    [SerializeField] private TextMeshProUGUI levelLabel;
    [SerializeField] private TextMeshProUGUI difficultyLabel;
    [SerializeField] private TextMeshProUGUI statusLabel;
    [SerializeField] private TextMeshProUGUI retakeLabel;
    [SerializeField] private Button retakeButton;
    [SerializeField] private GameObject detailsPanel;
    [SerializeField] private TextMeshProUGUI detailsText;

    private NewGameStatsPanel _owner;
    private NewGameResult _result;

    public void Setup(NewGameResultEntry entry, NewGameStatsPanel owner)
    {
        if (entry == null || entry.Result == null) return;
        _owner = owner;
        _result = entry.Result;
        if (levelLabel != null) levelLabel.text = _result.Id.ToString();
        if (difficultyLabel != null) difficultyLabel.text = ((SudokuDifficulty)_result.Difficulty).ToString();
        if (statusLabel != null) statusLabel.text = entry.IsCompleted ? "COMPLETED" : "INCOMPLETE";
        if (retakeLabel != null) retakeLabel.text = "RETAKE";
        if (detailsText != null)
        {
            detailsText.text = $"Pencil activations: {_result.PencilUses}\n" +
                               $"SOS uses: {_result.SOSUses}\n" +
                               $"Auto-fill uses: {_result.AutoFillUses}";
        }

        if (detailsPanel != null) detailsPanel.SetActive(false);
        if (retakeButton != null)
        {
            retakeButton.onClick.RemoveListener(Retake);
            retakeButton.onClick.AddListener(Retake);
        }

        SudokuDifficulty difficulty = (SudokuDifficulty)_result.Difficulty;
        if (ColorMap.Color_Map.TryGetValue(difficulty, out var colorMap) &&
            ColorUtility.TryParseHtmlString(colorMap.bg, out var background) &&
            ColorUtility.TryParseHtmlString(colorMap.outline, out var outlineColor) &&
            ColorUtility.TryParseHtmlString(colorMap.txtClr, out var textColor))
        {
            Image image = GetComponent<Image>();
            Outline outline = GetComponent<Outline>();
            if (image != null) image.color = background;
            if (outline != null) outline.effectColor = outlineColor;
            if (difficultyLabel != null) difficultyLabel.color = textColor;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.pointerPress != null && eventData.pointerPress.GetComponentInParent<Button>() != null)
            return;
        ToggleDetails();
    }

    private void ToggleDetails()
    {
        if (detailsPanel == null) return;
        if (!detailsPanel.activeSelf)
        {
            if (_expandedRow != null && _expandedRow != this && _expandedRow.detailsPanel != null)
                _expandedRow.detailsPanel.SetActive(false);
            detailsPanel.SetActive(true);
            _expandedRow = this;
        }
        else
        {
            detailsPanel.SetActive(false);
            if (_expandedRow == this) _expandedRow = null;
        }
    }

    private void Retake()
    {
        if (_owner != null && _result != null)
            _owner.RetakeResult(_result);
    }

    private void OnDestroy()
    {
        if (_expandedRow == this) _expandedRow = null;
    }
}
