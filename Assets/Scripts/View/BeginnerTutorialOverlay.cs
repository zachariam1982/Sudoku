using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Guides the player through the live Sudoku grid, shared number picker, and
/// game HUD before revealing the variant selector.
/// </summary>
public sealed class BeginnerTutorialOverlay : MonoBehaviour, ICanvasRaycastFilter
{
    public const string TutorialSeenPreferenceKey = "BeginnerTutorialSeen";
    private static bool _shownThisSession;

    private static readonly Color BackdropColor = new Color(0.005f, 0.015f, 0.04f, 0.34f);
    private static readonly Color CardColor = new Color(0.02f, 0.075f, 0.13f, 0.98f);
    private static readonly Color GoldColor = new Color(1f, 0.73f, 0.22f, 1f);

    private readonly List<GameObject> _selectorObjects = new List<GameObject>();
    private readonly List<bool> _selectorObjectStates = new List<bool>();

    private GameContext _gameContext;
    private TutorialViewModel _tutorialViewModel;
    private HUD _tutorialHud;
    private Image _backdrop;
    private RectTransform _card;
    private TextMeshProUGUI _title;
    private TextMeshProUGUI _instruction;
    private Button _nextButton;
    private TextMeshProUGUI _nextLabel;
    private Button _skipButton;
    private TextMeshProUGUI _skipLabel;
    private RectTransform _raycastPassThrough;
    private readonly Vector3[] _gridWorldCorners = new Vector3[4];

    private int _step;
    private bool _lastLandscape;
    private bool _isClosing;
    private bool _firstPracticeCellPrepared;
    private bool _numberEntryDemoStarted;
    private bool _numberEntrySelectionComplete;
    private bool _numberEntryDemoComplete;
    private bool _errorDemoStarted;
    private bool _errorDemoSelectionComplete;
    private bool _errorDemoComplete;
    private bool _eraseDemoStarted;
    private bool _eraseButtonReady;
    private bool _eraseDemoComplete;
    private int _hudControlStage = -1;
    private bool _hudControlWaiting;
    private bool _hudControlSubscribed;
    private TutorialHudControl _expectedHudControl;
    private bool _candidateNotesCellHighlighted;
    private bool _renderAlignmentSubscribed;

    /// <summary>Starts the lesson on the first launch where the player opens the selector.</summary>
    public static void ShowOnce(Transform selector, GameContext gameContext)
    {
        if (_shownThisSession || selector == null ||
            PlayerPrefs.GetInt(TutorialSeenPreferenceKey, 0) == 1) return;
        _shownThisSession = true;

        var visibleSelectorObjects = new List<GameObject>();
        var previousStates = new List<bool>();
        for (int i = 0; i < selector.childCount; i++)
        {
            GameObject child = selector.GetChild(i).gameObject;
            visibleSelectorObjects.Add(child);
            previousStates.Add(child.activeSelf);
            child.SetActive(false);
        }

        var overlayObject = new GameObject("BeginnerTutorialOverlay", typeof(RectTransform));
        overlayObject.transform.SetParent(selector, false);
        overlayObject.transform.SetAsLastSibling();
        BeginnerTutorialOverlay overlay = overlayObject.AddComponent<BeginnerTutorialOverlay>();
        overlay.Initialize(gameContext, visibleSelectorObjects, previousStates);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSessionState()
    {
        _shownThisSession = false;
    }

    private void Initialize(GameContext gameContext, List<GameObject> selectorObjects,
        List<bool> selectorObjectStates)
    {
        _gameContext = gameContext;
        _selectorObjects.AddRange(selectorObjects);
        _selectorObjectStates.AddRange(selectorObjectStates);
    }

    private void Awake()
    {
        BuildInterface();
    }

    private IEnumerator Start()
    {
        // Let the scene finish its first layout/grid build before binding the
        // disposable tutorial model or opening the picker.
        yield return null;

        if (_gameContext == null)
        {
            Debug.LogError("Beginner tutorial requires a GameContext reference.", this);
            CloseTutorial(false);
            yield break;
        }

        _tutorialViewModel = new TutorialViewModel();
        _gameContext.BeginTutorial(_tutorialViewModel);
        _tutorialViewModel.LastEnteredCell.OnChanged += OnValueEntered;
        Canvas.willRenderCanvases += AlignLandscapeCardBeforeRender;
        _renderAlignmentSubscribed = true;
        ApplyResponsiveLayout(Screen.width > Screen.height);
        RenderStep();
    }

    private void Update()
    {
        bool landscape = Screen.width > Screen.height;
        if (landscape != _lastLandscape)
            ApplyResponsiveLayout(landscape);
    }

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        bool allowPassThrough = _step == 2 || _step == 3 || _step == 4 ||
                                (_step == 5 && _eraseButtonReady) ||
                                (_step == 6 && _hudControlWaiting);
        return !allowPassThrough || _raycastPassThrough == null ||
               !RectTransformUtility.RectangleContainsScreenPoint(
                   _raycastPassThrough, screenPoint, eventCamera);
    }

    private void BuildInterface()
    {
        RectTransform root = (RectTransform)transform;
        Stretch(root, Vector2.zero, Vector2.one);

        _backdrop = gameObject.AddComponent<Image>();
        _backdrop.color = BackdropColor;
        _backdrop.raycastTarget = true;

        _card = CreateRect("TutorialCard", root);
        Image cardImage = _card.gameObject.AddComponent<Image>();
        cardImage.color = CardColor;
        cardImage.raycastTarget = false;

        _title = CreateText("TutorialTitle", _card, "WELCOME TO SUDOKU", 48f,
            GoldColor, TextAlignmentOptions.Center);
        _instruction = CreateText("TutorialInstruction", _card, string.Empty, 42f,
            Color.white, TextAlignmentOptions.Center);

        _skipButton = CreateButton("SkipTutorialButton", _card, "SKIP", 36f,
            new Color(0.12f, 0.19f, 0.27f, 1f));
        _skipLabel = _skipButton.GetComponentInChildren<TextMeshProUGUI>();
        _skipButton.onClick.AddListener(() => CloseTutorial(true));

        _nextButton = CreateButton("NextTutorialButton", _card, "NEXT", 36f, GoldColor);
        _nextLabel = _nextButton.GetComponentInChildren<TextMeshProUGUI>();
        _nextButton.onClick.AddListener(Advance);

        ApplyResponsiveLayout(Screen.width > Screen.height);
    }

    private void ApplyResponsiveLayout(bool landscape)
    {
        _lastLandscape = landscape;
        if (landscape)
        {
            if (!TryAlignLandscapeCardWithGrid())
                Stretch(_card, new Vector2(0.025f, 0.19f), new Vector2(0.285f, 0.96f));
            Stretch(_title.rectTransform, new Vector2(0.04f, 0.78f), new Vector2(0.96f, 0.97f));
            Stretch(_instruction.rectTransform, new Vector2(0.07f, 0.23f), new Vector2(0.93f, 0.76f));
            SetAnchors(_skipButton.GetComponent<RectTransform>(), new Vector2(0.06f, 0.035f), new Vector2(0.42f, 0.18f));
            SetAnchors(_nextButton.GetComponent<RectTransform>(), new Vector2(0.47f, 0.035f), new Vector2(0.94f, 0.18f));
            SetTextSize(_title, 72f);
            SetTextSize(_instruction, 62f);
            SetTextSize(_skipLabel, 50f);
            SetTextSize(_nextLabel, 50f);
        }
        else
        {
            Stretch(_card, new Vector2(0.04f, 0.80f), new Vector2(0.96f, 0.98f));
            Stretch(_title.rectTransform, new Vector2(0.03f, 0.76f), new Vector2(0.97f, 0.97f));
            Stretch(_instruction.rectTransform, new Vector2(0.05f, 0.21f), new Vector2(0.95f, 0.73f));
            SetAnchors(_skipButton.GetComponent<RectTransform>(), new Vector2(0.05f, 0.025f), new Vector2(0.40f, 0.18f));
            SetAnchors(_nextButton.GetComponent<RectTransform>(), new Vector2(0.60f, 0.025f), new Vector2(0.95f, 0.18f));
            SetTextSize(_title, 48f);
            SetTextSize(_instruction, 38f);
            SetTextSize(_skipLabel, 34f);
            SetTextSize(_nextLabel, 34f);
        }
    }

    private bool TryAlignLandscapeCardWithGrid()
    {
        RectTransform gridRect = _gameContext != null ? _gameContext.TutorialGridRect : null;
        if (gridRect == null || _card == null) return false;

        Canvas canvas = GetComponentInParent<Canvas>();
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
        gridRect.GetWorldCorners(_gridWorldCorners);

        Rect rootBounds = ((RectTransform)transform).rect;
        if (rootBounds.width <= 0f || rootBounds.height <= 0f) return false;
        float gridMinX = 1f;
        float gridMinY = 1f;
        float gridMaxY = 0f;
        for (int i = 0; i < _gridWorldCorners.Length; i++)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, _gridWorldCorners[i]);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)transform, screenPoint, eventCamera, out Vector2 localPoint))
                return false;

            float x = (localPoint.x - rootBounds.xMin) / rootBounds.width;
            float y = (localPoint.y - rootBounds.yMin) / rootBounds.height;
            gridMinX = Mathf.Min(gridMinX, x);
            gridMinY = Mathf.Min(gridMinY, y);
            gridMaxY = Mathf.Max(gridMaxY, y);
        }

        const float leftPadding = 0.025f;
        const float horizontalGap = 0.025f;
        float cardRight = gridMinX - horizontalGap;
        if (cardRight - leftPadding < 0.18f) return false;

        Vector2 anchorMin = new Vector2(leftPadding, Mathf.Clamp01(gridMinY));
        Vector2 anchorMax = new Vector2(Mathf.Clamp01(cardRight), Mathf.Clamp01(gridMaxY));
        const float anchorTolerance = 0.001f;
        if ((_card.anchorMin - anchorMin).sqrMagnitude > anchorTolerance * anchorTolerance ||
             (_card.anchorMax - anchorMax).sqrMagnitude > anchorTolerance * anchorTolerance)
            SetAnchors(_card, anchorMin, anchorMax);
        return true;
    }

    private void AlignLandscapeCardBeforeRender()
    {
        if (_isClosing || !isActiveAndEnabled || Screen.width <= Screen.height) return;

        // OrientationManager rebuilds the grid in LateUpdate. Aligning immediately
        // when the orientation changes can therefore use the grid's previous size.
        // willRenderCanvases runs after that rebuild, so these bounds match the
        // layout that is about to be displayed.
        TryAlignLandscapeCardWithGrid();
    }

    private void RenderStep()
    {
        switch (_step)
        {
            case 0:
                _title.text = "WELCOME TO SUDOKU";
                _instruction.text = "Fill the grid with numbers 1 through 9. This guided practice uses the actual game board and will not affect your saved games or scores.";
                _nextLabel.text = "START";
                break;
            case 1:
                _title.text = "THE SUDOKU RULES";
                _instruction.text = "Use each number once in every row, every column, and every 3×3 box. The values already on the board are clues.";
                _nextLabel.text = "NEXT";
                break;
            case 2:
                _title.text = "ENTER YOUR FIRST NUMBER";
                _instruction.text = "The empty practice cell is selected. Tap the glowing 1 in the number picker.";
                _nextLabel.text = "CHOOSE 1";
                break;
            case 3:
                _title.text = "THE NUMBER PICKER";
                _instruction.text = "Watch as we tap another empty cell, wait for the picker, and enter 4.";
                _nextLabel.text = "NEXT";
                break;
            case 4:
                _title.text = "SPOTTING A MISTAKE";
                _instruction.text = "Watch how Sudoku highlights duplicate numbers in the same row, column, or box.";
                _nextLabel.text = "NEXT";
                break;
            case 5:
                _title.text = "ERASE A NUMBER";
                _instruction.text = "Watch as we select the 4 we entered and press Erase to remove it.";
                _nextLabel.text = "NEXT";
                break;
            case 6:
                _title.text = "GAME TOOLS";
                _instruction.text = "Try the tools below on this practice board: Undo and Redo change your last move, Pencil adds notes, and Auto Candidates fills possible notes. Tap NEXT when finished.";
                _nextLabel.text = "NEXT";
                break;
            case 7:
                _title.text = "CLASSIC SUDOKU";
                _instruction.text = "Fill every row, column, and 3×3 box with the numbers 1 through 9. No number can repeat in any of them.";
                _nextLabel.text = "NEXT";
                break;
            case 8:
                _title.text = "DIAGONAL SUDOKU";
                _instruction.text = "Follow the two highlighted long diagonals. Each diagonal must also contain the numbers 1 through 9 without repeats.";
                _nextLabel.text = "NEXT";
                break;
            case 9:
                _title.text = "KILLER SUDOKU";
                _instruction.text = "Each colored cage has a target sum. Add its digits to reach that sum, without repeating a digit inside the cage.";
                _nextLabel.text = "NEXT";
                break;
            case 10:
                _title.text = "THERMO SUDOKU";
                _instruction.text = "Digits must increase from the thermometer bulb toward its tip.";
                _nextLabel.text = "NEXT";
                break;
            case 11:
                _title.text = "HYPER SUDOKU";
                _instruction.text = "The four highlighted extra 3×3 regions must each contain the numbers 1 through 9 without repeats.";
                _nextLabel.text = "NEXT";
                break;
            case 12:
                _title.text = "IRREGULAR SUDOKU";
                _instruction.text = "The thick outlines show the irregular regions that replace the 3×3 boxes. Each region must contain the numbers 1 through 9 without repeats.";
                _nextLabel.text = "NEXT";
                break;
            case 13:
                _title.text = "YOU'RE READY";
                _instruction.text = "You have seen all six variants. Choose one to start playing.";
                _nextLabel.text = "CHOOSE A VARIANT";
                break;
            default:
                _title.text = "YOU'RE READY";
                _instruction.text = "You have seen all six variants. Choose one to start playing.";
                _nextLabel.text = "CHOOSE A VARIANT";
                break;
        }

        if (_step >= 7 && _step <= 12)
        {
            _gameContext.SetTutorialVariantPreview((GameVariant)(_step - 7));
        }

        _backdrop.raycastTarget = true;
        _raycastPassThrough = _step == 2
            ? _gameContext.GetTutorialNumberButtonRect(1)
            : _step == 3
                ? _gameContext.GetTutorialNumberButtonRect(4)
                : _step == 4
                    ? _gameContext.GetTutorialNumberButtonRect(2)
                    : (_step == 5 && _eraseButtonReady
                        ? _gameContext.TutorialEraseButtonRect
                        : null);
        _nextButton.gameObject.SetActive(
            _step != 2 &&
            (_step != 3 || _numberEntryDemoComplete) &&
            (_step != 4 || _errorDemoComplete) &&
            (_step != 5 || _eraseDemoComplete));
        _nextButton.interactable =
            (_step != 3 || _numberEntrySelectionComplete) &&
            (_step != 4 || _errorDemoSelectionComplete) &&
            (_step != 5 || _eraseDemoComplete);
        bool showHud = _step == 6;
        _gameContext.SetTutorialChrome(showHud, false);

        if (_step == 6)
        {
            if (!_hudControlSubscribed)
            {
                _tutorialHud = _gameContext.TutorialHud;
                if (_tutorialHud != null)
                {
                    _tutorialHud.TutorialControlPressed += OnTutorialHudControlPressed;
                    _hudControlSubscribed = true;
                }
            }

            if (_hudControlStage < 0)
                BeginHudControlStage(0);
        }

        if (_step == 2 && !_firstPracticeCellPrepared)
        {
            _firstPracticeCellPrepared = true;
            if (_gameContext.SelectTutorialCell(0, 0))
                _gameContext.HighlightTutorialNumber(1);
            else
                Debug.LogWarning("The tutorial could not find the first practice cell on the live grid.", this);
        }
        else if (_step == 3 && !_numberEntryDemoStarted)
        {
            _numberEntryDemoStarted = true;
            StartCoroutine(PlayNumberEntryDemo());
        }
        else if (_step == 4 && !_errorDemoStarted)
        {
            _errorDemoStarted = true;
            StartCoroutine(PlayErrorDemo());
        }
        else if (_step == 5 && !_eraseDemoStarted)
        {
            _eraseDemoStarted = true;
            StartCoroutine(PlayEraseDemo());
        }
    }

    private void OnValueEntered((int row, int col, bool hasConflict) entry)
    {
        if (_step == 3 && _numberEntrySelectionComplete && entry.row == 0 && entry.col == 3)
        {
            if (_gameContext.GetTutorialCellValue(0, 3) != 4) return;

            _gameContext.ClearTutorialNumberHighlight();
            _numberEntryDemoComplete = true;
            _instruction.text = "You pressed 4 to enter it in the selected empty cell. Press NEXT to see how to erase it.";
            _nextButton.gameObject.SetActive(true);
            _nextButton.interactable = true;
            return;
        }

        if (_step == 4 && _errorDemoSelectionComplete && entry.row == 0 && entry.col == 4)
        {
            if (_gameContext.GetTutorialCellValue(0, 4) != 2) return;

            _gameContext.ClearTutorialNumberHighlight();
            _errorDemoComplete = true;
            _instruction.text = entry.hasConflict
                ? "Both 2s are highlighted as a conflict. Sudoku checks each row, column, and 3×3 box. Press NEXT to clear the example."
                : "You entered another 2. Sudoku checks each row, column, and 3×3 box. Press NEXT to continue.";
            _nextButton.gameObject.SetActive(true);
            _nextButton.interactable = true;
            return;
        }

        if (_step == 5 && _eraseButtonReady && entry.row == 0 && entry.col == 3 &&
            _gameContext.GetTutorialCellValue(0, 3) == 0)
        {
            _gameContext.ClearTutorialNumberHighlight();
            _eraseButtonReady = false;
            _eraseDemoComplete = true;
            _instruction.text = "You pressed Erase and the 4 disappeared. Press NEXT to continue.";
            _nextButton.gameObject.SetActive(true);
            _nextButton.interactable = true;
            return;
        }

        if (_step != 2 || entry.row != 0 || entry.col != 0) return;

        if (_tutorialViewModel.GetCellValue(0, 0) == 1)
        {
            _gameContext.ClearTutorialNumberHighlight();
            _step = 3;
            RenderStep();
            return;
        }

        _tutorialViewModel.ResetCell(0, 0);
        _firstPracticeCellPrepared = false;
        StartCoroutine(ReopenFirstPracticeCell());
    }

    private IEnumerator ReopenFirstPracticeCell()
    {
        // EnterValue closes the picker after publishing LastEnteredCell.
        yield return null;
        if (_isClosing || _step != 2) yield break;

        RenderStep();
        _instruction.text = "Try the glowing 1. It is the only number missing from this cell's row, column, and box.";
    }

    private IEnumerator PlayNumberEntryDemo()
    {
        _nextButton.interactable = false;
        RectTransform cell = _gameContext.GetTutorialCellRect(0, 3);
        bool cellSelected = false;
        yield return AnimatePress(cell, () => cellSelected = _gameContext.SelectTutorialCell(0, 3));
        if (!cellSelected)
        {
            Debug.LogWarning("The tutorial could not select the second practice cell.", this);
            _instruction.text = "We could not start the number picker demonstration. Press NEXT to continue.";
            _numberEntrySelectionComplete = true;
            _numberEntryDemoComplete = true;
            _nextButton.gameObject.SetActive(true);
            _nextButton.interactable = true;
            yield break;
        }

        _gameContext.HighlightTutorialNumber(4);
        _instruction.text = "We selected an empty cell and opened the number picker. Tap the glowing 4 to enter it.";
        _numberEntrySelectionComplete = true;
        _nextButton.interactable = false;
    }

    private IEnumerator PlayErrorDemo()
    {
        _nextButton.interactable = false;
        RectTransform cell = _gameContext.GetTutorialCellRect(0, 4);
        bool cellSelected = false;
        yield return AnimatePress(cell, () => cellSelected = _gameContext.SelectTutorialCell(0, 4));
        if (!cellSelected)
        {
            Debug.LogWarning("The tutorial could not select the error demonstration cell.", this);
            _instruction.text = "Sudoku marks duplicate numbers as errors. Press NEXT to continue.";
            _errorDemoSelectionComplete = true;
            _errorDemoComplete = true;
            _nextButton.gameObject.SetActive(true);
            _nextButton.interactable = true;
            yield break;
        }

        _gameContext.HighlightTutorialNumber(2);
        _instruction.text = "This row already contains a 2. Tap the glowing 2 to enter another 2 and see how Sudoku detects the error.";
        _errorDemoSelectionComplete = true;
        _nextButton.interactable = false;
    }

    private IEnumerator PlayEraseDemo()
    {
        _nextButton.gameObject.SetActive(false);
        _nextButton.interactable = false;
        _eraseButtonReady = false;
        if (_gameContext.GetTutorialCellValue(0, 3) != 4)
        {
            _instruction.text = "Erase removes a number from a filled cell. Press NEXT to continue.";
            _eraseDemoComplete = true;
            _nextButton.gameObject.SetActive(true);
            _nextButton.interactable = true;
            yield break;
        }

        RectTransform cell = _gameContext.GetTutorialCellRect(0, 3);
        bool cellSelected = false;
        yield return AnimatePress(cell, () => cellSelected = _gameContext.SelectTutorialCell(0, 3));
        if (!cellSelected)
        {
            Debug.LogWarning("The tutorial could not reselect the entered practice number.", this);
            _instruction.text = "Erase removes a number from a filled cell. Press NEXT to continue.";
            _eraseDemoComplete = true;
            _nextButton.gameObject.SetActive(true);
            _nextButton.interactable = true;
            yield break;
        }

        if (_gameContext.TutorialEraseButtonRect == null)
        {
            Debug.LogWarning("The tutorial could not find the number picker's Erase button.", this);
            _instruction.text = "Erase removes a number from a filled cell. Press NEXT to continue.";
            _eraseDemoComplete = true;
            _nextButton.gameObject.SetActive(true);
            _nextButton.interactable = true;
            yield break;
        }

        _gameContext.HighlightTutorialEraseButton();
        _instruction.text = "We selected the 4. Tap the glowing Erase button to remove it.";
        _eraseButtonReady = true;
        _raycastPassThrough = _gameContext.TutorialEraseButtonRect;
    }

    private void BeginHudControlStage(int stage)
    {
        _hudControlStage = stage;
        switch (stage)
        {
            case 0:
                _expectedHudControl = TutorialHudControl.Undo;
                _instruction.text = "Undo reverses the last move. Tap the glowing Undo control to restore the 4.";
                break;
            case 1:
                _expectedHudControl = TutorialHudControl.Redo;
                _instruction.text = "Redo reapplies an undone move. Tap the glowing Redo control to remove the 4 again.";
                break;
            case 2:
                _expectedHudControl = TutorialHudControl.Pencil;
                _instruction.text = "Pencil mode lets you add candidate notes. Tap the glowing Pencil control to turn it on.";
                break;
            case 3:
                _expectedHudControl = TutorialHudControl.Pencil;
                _instruction.text = "Tap Pencil again to turn note mode off before continuing.";
                break;
            default:
                _expectedHudControl = TutorialHudControl.AutoFill;
                _instruction.text = "Auto Fill adds possible candidate notes to empty cells. Tap the glowing Auto Fill control.";
                break;
        }

        if (_tutorialHud == null)
        {
            Debug.LogWarning("The tutorial could not find the New Game HUD.", this);
            _hudControlWaiting = false;
            _nextButton.gameObject.SetActive(true);
            _nextButton.interactable = true;
            return;
        }

        RectTransform controlRect = _tutorialHud.GetTutorialControlRect(_expectedHudControl);
        if (controlRect == null)
        {
            Debug.LogWarning($"The tutorial could not find HUD control {_expectedHudControl}.", this);
            _hudControlWaiting = false;
            _nextButton.gameObject.SetActive(true);
            _nextButton.interactable = true;
            return;
        }

        _hudControlWaiting = true;
        _nextButton.gameObject.SetActive(false);
        _nextButton.interactable = false;
        _raycastPassThrough = controlRect;
        _tutorialHud.HighlightTutorialControl(_expectedHudControl);
    }

    private void OnTutorialHudControlPressed(TutorialHudControl control)
    {
        if (_step != 6 || !_hudControlWaiting || control != _expectedHudControl) return;

        _tutorialHud.ClearTutorialHighlight();
        _hudControlWaiting = false;
        _raycastPassThrough = null;
        switch (_hudControlStage)
        {
            case 0:
                _instruction.text = "Undo restored the 4. Press NEXT to continue.";
                break;
            case 1:
                _instruction.text = "Redo reapplied the move. Press NEXT to continue.";
                break;
            case 2:
                _instruction.text = "Pencil mode is on. Press NEXT to turn it off.";
                break;
            case 3:
                _instruction.text = "Pencil mode is off. Press NEXT to try Auto Fill.";
                break;
            default:
                _instruction.text = "Auto Fill added candidate notes. Press NEXT when ready.";
                break;
        }

        _nextButton.gameObject.SetActive(true);
        _nextButton.interactable = true;
    }

    private static IEnumerator AnimatePress(RectTransform target, Action onPressed)
    {
        if (target == null)
        {
            onPressed?.Invoke();
            yield break;
        }

        Vector3 startScale = target.localScale;
        Vector3 pressedScale = startScale * 0.82f;
        const float pressDuration = 0.12f;

        float elapsed = 0f;
        while (elapsed < pressDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / pressDuration);
            target.localScale = Vector3.Lerp(startScale, pressedScale, t);
            yield return null;
        }

        target.localScale = pressedScale;
        onPressed?.Invoke();

        elapsed = 0f;
        while (elapsed < pressDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / pressDuration);
            target.localScale = Vector3.Lerp(pressedScale, startScale, t);
            yield return null;
        }

        if (target != null) target.localScale = startScale;
    }

    private void Advance()
    {
        if (_step == 6)
        {
            if (_hudControlWaiting) return;
            if (_hudControlStage < 4)
            {
                BeginHudControlStage(_hudControlStage + 1);
                return;
            }

            if (_hudControlStage == 4)
            {
                ShowCandidateNotesExplanation();
                return;
            }

            ClearCandidateNotesHighlight();
        }

        if (_step == 3)
        {
            if (!_numberEntrySelectionComplete || !_numberEntryDemoComplete) return;
        }
        if (_step == 4)
        {
            if (!_errorDemoSelectionComplete || !_errorDemoComplete) return;

            if (_gameContext.GetTutorialCellValue(0, 4) != 0)
                _tutorialViewModel.ResetCell(0, 4);
        }
        if (_step == 5 && !_eraseDemoComplete)
        {
            return;
        }

        if (_tutorialViewModel.IsPickerOpen.Value)
            _tutorialViewModel.CancelPickerCommand.Execute();

        if (_step < 13)
        {
            _step++;
            RenderStep();
        }
        else
        {
            CloseTutorial(true);
        }
    }

    private void CloseTutorial(bool markTutorialSeen)
    {
        if (_isClosing) return;
        _isClosing = true;
        if (_renderAlignmentSubscribed)
        {
            Canvas.willRenderCanvases -= AlignLandscapeCardBeforeRender;
            _renderAlignmentSubscribed = false;
        }

        if (markTutorialSeen)
        {
            PlayerPrefs.SetInt(TutorialSeenPreferenceKey, 1);
            PlayerPrefs.Save();
        }

        ClearCandidateNotesHighlight();

        if (_tutorialViewModel != null)
        {
            _tutorialViewModel.LastEnteredCell.OnChanged -= OnValueEntered;
            _gameContext?.EndTutorial(_tutorialViewModel);
        }

        if (_tutorialHud != null)
        {
            _tutorialHud.TutorialControlPressed -= OnTutorialHudControlPressed;
            _tutorialHud.ClearTutorialHighlight();
            _tutorialHud = null;
        }

        for (int i = 0; i < _selectorObjects.Count; i++)
        {
            if (_selectorObjects[i] != null)
                _selectorObjects[i].SetActive(_selectorObjectStates[i]);
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (_renderAlignmentSubscribed)
        {
            Canvas.willRenderCanvases -= AlignLandscapeCardBeforeRender;
            _renderAlignmentSubscribed = false;
        }
    }

    private void ShowCandidateNotesExplanation()
    {
        _hudControlStage = 5;
        _title.text = "CANDIDATE NOTES";
        _candidateNotesCellHighlighted =
            _gameContext.SetTutorialCellHighlight(0, 4, true);
        if (!_candidateNotesCellHighlighted)
            Debug.LogWarning("The tutorial could not highlight the candidate notes cell.", this);

        _instruction.text = "The small numbers in this highlighted cell are candidate notes: possible values that fit without breaking Sudoku rules. They are notes, not entered answers. Press NEXT to finish.";
        _nextButton.gameObject.SetActive(true);
        _nextButton.interactable = true;
    }

    private void ClearCandidateNotesHighlight()
    {
        if (!_candidateNotesCellHighlighted) return;

        _gameContext?.SetTutorialCellHighlight(0, 4, false);
        _candidateNotesCellHighlighted = false;
    }

    private static Button CreateButton(string name, Transform parent, string label, float fontSize, Color color)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.GetComponent<Image>();
        image.color = color;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.15f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.12f);
        button.colors = colors;

        TextMeshProUGUI labelText = CreateText("Label", buttonObject.transform, label, fontSize,
            Color.white, TextAlignmentOptions.Center);
        labelText.fontStyle = FontStyles.Bold;
        return button;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string value,
        float fontSize, Color color, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(name, parent);
        Stretch(rect, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f));

        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.enableAutoSizing = true;
        text.fontSizeMin = fontSize * 0.65f;
        text.fontSizeMax = fontSize;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    private static void SetTextSize(TextMeshProUGUI text, float size)
    {
        text.fontSize = size;
        text.fontSizeMin = size * 0.65f;
        text.fontSizeMax = size;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return (RectTransform)child.transform;
    }

    private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax) =>
        Stretch(rect, anchorMin, anchorMax);
}

/// <summary>A disposable view model that lets the tutorial use real gameplay controls safely.</summary>
public sealed class TutorialViewModel : BaseViewModel
{
    private static readonly string[] SolutionRows =
    {
        "123456789", "456789123", "789123456",
        "234567891", "567891234", "891234567",
        "345678912", "678912345", "912345678"
    };
    private static int[] tutorialIrregularRegions;
    private static IrregularSudokuRules tutorialIrregularRules;

    public TutorialViewModel()
        : base(new NewGameUsageStats(), new JourneyScorePenalties(),
            new ClassicSudokuModel(new SqliteGameHistoryRepository()))
    {
        SetDemoMode();
        LoadSamplePuzzle();
    }

    public int GetCellValue(int row, int column) => Model.GetValue(row, column);

    public void ResetCell(int row, int column)
    {
        Model.SetValue(row, column, 0);
        PublishBoard();
        UpdateConflictingCells();
        IsBoardValid.Value = Model.Validate();
        IsComplete.Value = false;
    }

    public void SetVariantPreviewData(GameVariant variant)
    {
        LoadVariantPreviewBoard(variant);
        KillerCages.Value = variant == GameVariant.Killer
            ? new[]
            {
                new KillerCage(3, new[] { 0, 1 }),
                new KillerCage(9, new[] { 2, 11 }),
                new KillerCage(9, new[] { 9, 10 })
            }
            : Array.Empty<KillerCage>();
        Thermometers.Value = variant == GameVariant.Thermo
            ? new[] { new Thermometer(new[] { 0, 1, 2, 11, 20 }) }
            : Array.Empty<Thermometer>();
    }

    private void LoadVariantPreviewBoard(GameVariant variant)
    {
        // Leave the interactive tutorial controls in a neutral state before the
        // read-only variant examples appear. This also refreshes the shared HUD
        // button and removes the grid's pencil-mode visuals.
        IsPencilMode.Value = false;
        IsEraseMode.Value = false;
        IsSOSMode.Value = false;
        HighlightedCandidateNumber.Value = 0;
        CancelPickerCommand.Execute();

        int[,] solution = CreateVariantPreviewSolution(variant);
        // Show a completed, valid example for each ruleset. Fully filled cells also
        // prevent stale pencil candidates from appearing in the preview grid.
        for (int row = 0; row < 9; row++)
        {
            for (int column = 0; column < 9; column++)
            {
                Model.Board[row, column] = solution[row, column];
                Model.GivenMask[row, column] = true;
                Model.PencilCandidateMasks[row, column] = 0;
            }
        }

        PublishBoard();
        PublishPencilCandidates();
        ConflictingCells.Value = new HashSet<(int, int)>();
        IsBoardValid.Value = true;
        IsComplete.Value = false;
        FirstCellTapped.Value = false;
    }

    private int[,] CreateVariantPreviewSolution(GameVariant variant)
    {
        if (variant == GameVariant.Irregular)
        {
            if (tutorialIrregularRules == null)
                tutorialIrregularRules = new IrregularSudokuRules(1);
            return tutorialIrregularRules.CreateSolution(new System.Random(1));
        }

        if (variant == GameVariant.Diagonal || variant == GameVariant.Hyper)
        {
            var solution = new int[9, 9];
            var random = new System.Random(variant == GameVariant.Diagonal ? 17 : 29);
            int nodesRemaining = 200000;
            bool filled = FillVariantPreview(solution, variant, random, ref nodesRemaining);
            if (!filled)
                Debug.LogWarning($"Could not generate a complete {variant} tutorial preview; using the classic example.");
            else
                return solution;
        }

        // The standard example also satisfies Killer and Thermo: its tutorial cage
        // sums and thermometer path are derived from these values.
        var classic = new int[9, 9];
        for (int row = 0; row < 9; row++)
            for (int column = 0; column < 9; column++)
                classic[row, column] = SolutionRows[row][column] - '0';
        return classic;
    }

    private static bool FillVariantPreview(int[,] board, GameVariant variant, System.Random random, ref int nodesRemaining)
    {
        if (--nodesRemaining < 0) return false;

        int bestRow = -1, bestColumn = -1;
        var bestCandidates = new List<int>(9);
        for (int row = 0; row < 9; row++)
        {
            for (int column = 0; column < 9; column++)
            {
                if (board[row, column] != 0) continue;
                var candidates = new List<int>(9);
                for (int value = 1; value <= 9; value++)
                    if (IsVariantPreviewPlacementValid(board, row, column, value, variant))
                        candidates.Add(value);
                if (candidates.Count == 0) return false;
                if (bestRow >= 0 && candidates.Count >= bestCandidates.Count) continue;
                bestRow = row;
                bestColumn = column;
                bestCandidates = candidates;
                if (candidates.Count == 1) break;
            }
        }

        if (bestRow < 0) return true;
        for (int i = bestCandidates.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            int value = bestCandidates[i];
            bestCandidates[i] = bestCandidates[j];
            bestCandidates[j] = value;
        }
        foreach (int value in bestCandidates)
        {
            board[bestRow, bestColumn] = value;
            if (FillVariantPreview(board, variant, random, ref nodesRemaining)) return true;
            board[bestRow, bestColumn] = 0;
            if (nodesRemaining <= 0) return false;
        }
        return false;
    }

    private static bool IsVariantPreviewPlacementValid(int[,] board, int row, int column, int value, GameVariant variant)
    {
        switch (variant)
        {
            case GameVariant.Diagonal:
                return DiagonalSudokuRules.IsValidPlacement(board, row, column, value);
            case GameVariant.Hyper:
                return HyperSudokuRules.IsValidPlacement(board, row, column, value);
            default:
                for (int i = 0; i < 9; i++)
                    if (board[row, i] == value || board[i, column] == value)
                        return false;
                int boxRow = row / 3 * 3;
                int boxColumn = column / 3 * 3;
                for (int r = boxRow; r < boxRow + 3; r++)
                    for (int c = boxColumn; c < boxColumn + 3; c++)
                        if (board[r, c] == value)
                            return false;
                return true;
        }
    }

    public int[] GetTutorialIrregularRegions()
    {
        if (tutorialIrregularRegions == null)
        {
            if (tutorialIrregularRules == null)
                tutorialIrregularRules = new IrregularSudokuRules(1);
            tutorialIrregularRegions = new int[81];
            for (int row = 0; row < 9; row++)
                for (int column = 0; column < 9; column++)
                    tutorialIrregularRegions[row * 9 + column] = tutorialIrregularRules.GetRegion(row, column);
        }

        return (int[])tutorialIrregularRegions.Clone();
    }

    private void LoadSamplePuzzle()
    {
        var solution = new int[81];
        var puzzle = new int[81];
        for (int row = 0; row < 9; row++)
            for (int column = 0; column < 9; column++)
                solution[row * 9 + column] = SolutionRows[row][column] - '0';

        // The top-left box contains 2 through 9, leaving 1 as a simple first move.
        SetGiven(puzzle, solution, 0, 1);
        SetGiven(puzzle, solution, 0, 2);
        SetGiven(puzzle, solution, 1, 0);
        SetGiven(puzzle, solution, 1, 1);
        SetGiven(puzzle, solution, 1, 2);
        SetGiven(puzzle, solution, 2, 0);
        SetGiven(puzzle, solution, 2, 1);
        SetGiven(puzzle, solution, 2, 2);
        SetGiven(puzzle, solution, 3, 3);
        SetGiven(puzzle, solution, 3, 4);
        SetGiven(puzzle, solution, 3, 5);
        SetGiven(puzzle, solution, 4, 3);
        SetGiven(puzzle, solution, 4, 4);
        SetGiven(puzzle, solution, 4, 5);
        SetGiven(puzzle, solution, 5, 3);
        SetGiven(puzzle, solution, 5, 4);
        SetGiven(puzzle, solution, 5, 5);
        SetGiven(puzzle, solution, 6, 6);
        SetGiven(puzzle, solution, 6, 7);
        SetGiven(puzzle, solution, 6, 8);
        SetGiven(puzzle, solution, 7, 6);
        SetGiven(puzzle, solution, 7, 7);
        SetGiven(puzzle, solution, 7, 8);
        SetGiven(puzzle, solution, 8, 6);
        SetGiven(puzzle, solution, 8, 7);
        SetGiven(puzzle, solution, 8, 8);

        Model.SetLevel(1);
        Model.SetDifficulty(SudokuDifficulty.Simple);
        if (!Model.TryLoadSavedPuzzle(puzzle, solution))
        {
            Debug.LogError("The built-in beginner tutorial puzzle is invalid.");
            return;
        }

        PublishBoard();
        PublishPencilCandidates();
        IsBoardValid.Value = true;
        IsComplete.Value = false;
        FirstCellTapped.Value = false;
    }

    private static void SetGiven(int[] puzzle, int[] solution, int row, int column)
    {
        int index = row * 9 + column;
        puzzle[index] = solution[index];
    }
}
