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

    private static readonly Color Gold = new Color32(242, 198, 108, 255);
    private static readonly Color White = new Color32(241, 244, 252, 255);
    private static readonly Color IncompleteAmber = new Color32(232, 151, 83, 255);

    public void Setup(NewGameResultEntry entry, NewGameStatsPanel owner)
    {
        if (entry == null || entry.Result == null) return;
        _owner = owner;
        _result = entry.Result;

        if (levelLabel != null)
        {
            levelLabel.text = $"LEVEL {_result.Id:00}";
            levelLabel.color = Gold;
        }

        if (difficultyLabel != null)
        {
            difficultyLabel.text = ((SudokuDifficulty)_result.Difficulty).ToString().ToUpperInvariant();
            difficultyLabel.color = White;
        }

        if (statusLabel != null)
        {
            statusLabel.text = entry.IsCompleted ? "COMPLETED" : "INCOMPLETE";
            statusLabel.color = entry.IsCompleted ? Gold : IncompleteAmber;
        }

        if (retakeLabel != null)
        {
            retakeLabel.text = "RETAKE";
            retakeLabel.color = Gold;
        }

        if (detailsText != null)
            detailsText.text = BuildUsageDetails(_result);

        if (detailsPanel != null)
            detailsPanel.SetActive(false);

        if (retakeButton != null)
        {
            retakeButton.onClick.RemoveListener(Retake);
            retakeButton.onClick.AddListener(Retake);
        }
    }

    private static string BuildUsageDetails(NewGameResult result)
    {
        return $"<align=left><size=140%><color=#F2C66C>{result.PencilUses}</color>" +
               $"<pos=36%><color=#F2C66C>{result.SOSUses}</color>" +
               $"<pos=70%><color=#F2C66C>{result.AutoFillUses}</color></size>\n" +
               "<size=68%><color=#C2CEE3>PENCIL ACTIVATIONS</color>" +
               "<pos=36%>SOS USES<pos=70%>AUTO-FILL USES</color></size></align>";
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
        bool show = !detailsPanel.activeSelf;
        if (show && _expandedRow != null && _expandedRow != this && _expandedRow.detailsPanel != null)
            _expandedRow.SetDetailsVisible(false);
        SetDetailsVisible(show);
        _expandedRow = show ? this : null;
    }

    private void SetDetailsVisible(bool visible)
    {
        if (detailsPanel == null) return;
        detailsPanel.SetActive(visible);
        LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
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
