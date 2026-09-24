using TMPro;
using UnityEngine;

/// <summary>
/// Controls the HomeScreen hierarchy configured in the Unity scene.
/// All layout, buttons, colours and text objects are serialized in the scene.
/// </summary>
public sealed class HomeScreenController : MonoBehaviour
{
    private const string PortraitHeader =
        "<color=#FFBE4C><size=24><b>YOUR NEXT PUZZLE</b></size></color>\n" +
        "<size=82><b>SUDOKU</b></size>\n" +
        "<size=28><color=#AAACCD>Choose how you want to play</color></size>";

    private const string LandscapeHeader =
        "<color=#FFBE4C><size=36><b>     YOUR NEXT PUZZLE</b></size></color>\n   " +
        "<size=150><b>S U D O K U</b></size>   \n" +
        "<size=38><color=#AAACCD>        Choose how you want to play</color></size>";

    [Header("Responsive Layout")]
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private RectTransform dailyChallengeButton;
    [SerializeField] private RectTransform newGameButton;
    [SerializeField] private RectTransform sudokuJourneyButton;
    [SerializeField] private RectTransform footer;
    [SerializeField] private TextMeshProUGUI dailyChallengeLabel;
    [SerializeField] private TextMeshProUGUI newGameLabel;

    [Header("Journey Status")]
    [SerializeField] private TextMeshProUGUI journeyLabel;
    
    private SudokuViewModel _viewModel;
    private bool _layoutApplied;
    private bool _lastLandscape;

    private void OnEnable()
    {
        _layoutApplied = false;
        ApplyResponsiveLayout();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (isActiveAndEnabled) ApplyResponsiveLayout();
    }

    public void Initialize(SudokuViewModel viewModel)
    {
        _viewModel = viewModel;
        ApplyResponsiveLayout();
        RefreshJourneyStatus();
    }

    public void RefreshJourneyStatus()
    {
        if (_viewModel == null) return;

        bool hasProgress = SaveSystem.HasSave() || GameDatabase.GetLastRecord() != null;
        int difficultyIndex = Mathf.Clamp(_viewModel.GetDifficulty, 0, 8);
        int totalPoints = GameDatabase.GetTotalPoints();

        journeyLabel.text = $"<color=#9771FF><b>{(hasProgress ? "CONTINUE JOURNEY" : "SUDOKU JOURNEY")}</b></color>\n<size=24><color=#AAACCD>Level {_viewModel.GetLevel}  •  {(SudokuDifficulty)difficultyIndex}  •  {totalPoints:N0} Points</color></size>";
    }

    public void OpenJourney()
    {
        gameObject.SetActive(false);

        bool isSavedGameInProgress =
            _viewModel.ElapsedSeconds.Value > 0f &&
            !_viewModel.IsWon.Value &&
            !_viewModel.IsLost.Value;

        if (isSavedGameInProgress && GameStateMachine.Instance.CurrentState is IdleState)
            _viewModel.FirstCellTapped.Value = true;

        GameStateMachine.Instance.SetSuspended(false);
    }

    public void OpenHome()
    {
        GameStateMachine.Instance.SetSuspended(true);
        RefreshJourneyStatus();
        gameObject.SetActive(true);
    }

    private void ApplyResponsiveLayout()
    {
        Rect rootRect = ((RectTransform)transform).rect;
        bool landscape = rootRect.width > rootRect.height;

        if (_layoutApplied && _lastLandscape == landscape) return;

        _layoutApplied = true;
        _lastLandscape = landscape;

        if (landscape)
        {
            SetStretched(headerText.rectTransform, 0.04f, 0.69f, 0.96f, 0.97f);
            SetStretched(dailyChallengeButton, 0.02f, 0.08f, 0.32f, 0.64f);
            SetStretched(newGameButton, 0.35f, 0.08f, 0.65f, 0.64f);
            SetStretched(sudokuJourneyButton, 0.68f, 0.08f, 0.98f, 0.64f);
            SetStretched(footer, 0.35f, 0.01f, 0.65f, 0.07f);

            headerText.text = LandscapeHeader;
            headerText.fontSizeMax = 150f;
            dailyChallengeLabel.fontSizeMax = 72f;
            newGameLabel.fontSizeMax = 72f;
            journeyLabel.fontSizeMax = 72f;
            return;
        }

        SetCentered(headerText.rectTransform, 0f, 570f, 940f, 360f);
        SetCentered(dailyChallengeButton, 0f, 190f, 900f, 240f);
        SetCentered(newGameButton, 0f, -100f, 900f, 240f);
        SetCentered(sudokuJourneyButton, 0f, -390f, 900f, 240f);
        SetCentered(footer, 0f, -630f, 900f, 80f);

        headerText.text = PortraitHeader;
        headerText.fontSizeMax = 82f;
        dailyChallengeLabel.fontSizeMax = 38f;
        newGameLabel.fontSizeMax = 38f;
        journeyLabel.fontSizeMax = 38f;
    }

    private static void SetStretched(
        RectTransform target, float xMin, float yMin, float xMax, float yMax)
    {
        target.anchorMin = new Vector2(xMin, yMin);
        target.anchorMax = new Vector2(xMax, yMax);
        target.offsetMin = Vector2.zero;
        target.offsetMax = Vector2.zero;
    }

    private static void SetCentered(
        RectTransform target, float x, float y, float width, float height)
    {
        target.anchorMin = target.anchorMax = new Vector2(0.5f, 0.5f);
        target.anchoredPosition = new Vector2(x, y);
        target.sizeDelta = new Vector2(width, height);
    }
}
