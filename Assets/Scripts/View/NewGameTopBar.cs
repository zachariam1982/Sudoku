using UnityEngine;
using UnityEngine.UI;

public class NewGameTopBar : MonoBehaviour
{
    [Header("TopBar Items")]
    [SerializeField] private RectTransform homeBlock;
    [SerializeField] private RectTransform livesContainer;
    [SerializeField] private RectTransform level;
    [SerializeField] private RectTransform timer;
    [SerializeField] private RectTransform pauseBlock;
    [SerializeField] private RectTransform settingsBlock;
    [SerializeField] private Button exitButton;
    [SerializeField] private bool isNewGameTopBar;


    [Header("Scaling")]
    [SerializeField] [Range(1f, 2f)] private float landscapeScale = 1.4f;
    [SerializeField] private float portraitScale = 1f;


    private struct RectTransformState
    {
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        public Vector2 pivot;

        public RectTransformState(RectTransform rect)
        {
            anchorMin = rect.anchorMin;
            anchorMax = rect.anchorMax;
            anchoredPosition = rect.anchoredPosition;
            sizeDelta = rect.sizeDelta;
            pivot = rect.pivot;
        }

        public void Restore(RectTransform rect)
        {
            if (rect == null) return;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            rect.pivot = pivot;
        }
    }
    private bool _layoutApplied;
    private bool _lastLandscape;
    private RectTransform _topBar;
    private HorizontalLayoutGroup _layout;
    private LayoutElement[] _itemLayouts;
    private float[] _originalMinWidths;
    private float[] _originalPreferredWidths;
    private float[] _originalFlexibleWidths;
    private float _originalSpacing;
    private TextAnchor _originalAlignment;
    private bool _originalExpandWidth;
    private bool _originalControlWidth;
    private bool _originalLayoutEnabled;
    private RectTransformState _homeState;
    private RectTransformState _levelState;
    private RectTransformState _settingsState;
    private RectTransformState _exitState;
    private void Awake()
    {
        _topBar = GetComponent<RectTransform>();
        _layout = GetComponent<HorizontalLayoutGroup>();

        if (exitButton != null)
            exitButton.onClick.AddListener(ExitNewGame);

        if (_layout != null)
        {
            _originalLayoutEnabled = _layout.enabled;
            _layout.childScaleWidth = true;
            _layout.childScaleHeight = true;
            _originalSpacing = _layout.spacing;
            _originalAlignment = _layout.childAlignment;
            _originalExpandWidth = _layout.childForceExpandWidth;
            _originalControlWidth = _layout.childControlWidth;

            _homeState = new RectTransformState(homeBlock);
            _levelState = new RectTransformState(level);
            _settingsState = new RectTransformState(settingsBlock);
            _exitState = new RectTransformState(exitButton != null
                ? exitButton.GetComponent<RectTransform>()
                : null);

            RectTransform[] items = new RectTransform[]
            {
                homeBlock, livesContainer, level, timer, pauseBlock, settingsBlock,
                exitButton != null ? exitButton.GetComponent<RectTransform>() : null
            };
            _itemLayouts = new LayoutElement[items.Length];
            _originalMinWidths = new float[items.Length];
            _originalPreferredWidths = new float[items.Length];
            _originalFlexibleWidths = new float[items.Length];

            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null) continue;

                _itemLayouts[i] = items[i].GetComponent<LayoutElement>();
                if (_itemLayouts[i] == null)
                    _itemLayouts[i] = items[i].gameObject.AddComponent<LayoutElement>();

                _originalMinWidths[i] = _itemLayouts[i].minWidth;
                _originalPreferredWidths[i] = _itemLayouts[i].preferredWidth;
                _originalFlexibleWidths[i] = _itemLayouts[i].flexibleWidth;
            }
        }
    }


    private void OnEnable()
    {
        _layoutApplied = false;

        Refresh();
    }


    private void Update()
    {
        bool isLandscape =
            Screen.width >
            Screen.height;

        /*
         * Don't rebuild every frame.
         */
        if (_layoutApplied &&
            _lastLandscape == isLandscape)
        {
            return;
        }

        Refresh();
    }


    private void Refresh()
    {
        bool isLandscape =
            Screen.width >
            Screen.height;

        _lastLandscape =
            isLandscape;
        _layoutApplied = true;

        float scale =
            isLandscape
                ? landscapeScale
                : portraitScale;

        ApplySpacingMode(isLandscape);

        Vector3 targetScale =
            new Vector3(
                scale,
                scale,
                1f);

        ApplyScale(
            homeBlock,
            targetScale);

        ApplyScale(
            livesContainer,
            targetScale);

        ApplyScale(
            level,
            targetScale);

        ApplyScale(
            timer,
            targetScale);

        ApplyScale(
            pauseBlock,
            targetScale);

        ApplyScale(
            settingsBlock,
            targetScale);

        ApplyScale(exitButton != null ? exitButton.GetComponent<RectTransform>() : null, targetScale);


        /*
         * Tell Unity to recalculate positions
         * immediately after rotation.
         */
        if (_topBar != null)
        {
            LayoutRebuilder
                .ForceRebuildLayoutImmediate(
                    _topBar);
        }
    }

    private void ApplySpacingMode(bool isLandscape)
    {
        if (_layout == null || _itemLayouts == null) return;

        if (isNewGameTopBar)
        {
            // Keep the level label centered. Place the Home control on the left
            // and spread Settings and Exit across the right side of the bar.
            _layout.enabled = false;
            SetAnchoredSlot(homeBlock, 0.07f, 100f);
            SetAnchoredSlot(level, 0.50f, 280f);
            SetAnchoredSlot(settingsBlock, 0.78f, 100f);
            if (exitButton != null)
                SetAnchoredSlot(exitButton.GetComponent<RectTransform>(), 0.93f, 100f);
            return;
        }

        _layout.enabled = _originalLayoutEnabled;

        if (isNewGameTopBar)
        {
            _homeState.Restore(homeBlock);
            _levelState.Restore(level);
            _settingsState.Restore(settingsBlock);
            _exitState.Restore(exitButton != null
                ? exitButton.GetComponent<RectTransform>()
                : null);
        }


        if (isLandscape)
        {
            // Journey uses equal slots for each visible item. New Game keeps
            // fixed icon slots around a flexible, centered level label.
            _layout.spacing = 0f;
            _layout.childAlignment = TextAnchor.MiddleCenter;
            _layout.childForceExpandWidth = false;
            _layout.childControlWidth = true;

            for (int i = 0; i < _itemLayouts.Length; i++)
            {
                LayoutElement item = _itemLayouts[i];
                if (item == null) continue;

                item.minWidth = 0f;
                item.preferredWidth = 0f;
                item.flexibleWidth = 1f;
            }

        }
        else
        {
            _layout.spacing = _originalSpacing;
            _layout.childAlignment = _originalAlignment;
            _layout.childForceExpandWidth = _originalExpandWidth;
            _layout.childControlWidth = _originalControlWidth;

            for (int i = 0; i < _itemLayouts.Length; i++)
            {
                LayoutElement item = _itemLayouts[i];
                if (item == null) continue;

                item.minWidth = _originalMinWidths[i];
                item.preferredWidth = _originalPreferredWidths[i];
                item.flexibleWidth = _originalFlexibleWidths[i];
            }
        }

        if (isNewGameTopBar)
        {
            SetFixedWidth(homeBlock, 100f, 100f);
            SetFixedWidth(settingsBlock, 100f, 100f);
            SetFlexibleWidth(level);
            if (exitButton != null)
                SetFixedWidth(exitButton.GetComponent<RectTransform>(), 93.33f, 100f);
        }
    }

    private static void SetAnchoredSlot(RectTransform target, float anchorX, float width)
    {
        if (target == null) return;
        target.anchorMin = new Vector2(anchorX, 0.5f);
        target.anchorMax = new Vector2(anchorX, 0.5f);
        target.pivot = new Vector2(0.5f, 0.5f);
        target.anchoredPosition = Vector2.zero;
        target.sizeDelta = new Vector2(width, 100f);
    }

    private static void SetFixedWidth(RectTransform target, float minWidth, float preferredWidth)
    {
        LayoutElement layoutElement = target != null ? target.GetComponent<LayoutElement>() : null;
        if (layoutElement == null) return;
        layoutElement.minWidth = minWidth;
        layoutElement.preferredWidth = preferredWidth;
        layoutElement.flexibleWidth = 0f;
    }

    private static void SetFlexibleWidth(RectTransform target)
    {
        LayoutElement layoutElement = target != null ? target.GetComponent<LayoutElement>() : null;
        if (layoutElement == null) return;
        layoutElement.minWidth = 0f;
        layoutElement.preferredWidth = 0f;
        layoutElement.flexibleWidth = 1f;
    }


    private static void ApplyScale(
        RectTransform target,
        Vector3 scale)
    {
        if (target == null)
            return;

        target.localScale =
            scale;
    }

    public void SetNewGameMode(bool isNewGame)
    {
        bool shouldBeVisible = isNewGameTopBar == isNewGame;
        if (gameObject.activeSelf != shouldBeVisible)
            gameObject.SetActive(shouldBeVisible);

        if (_topBar != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(_topBar);
    }

    private void ExitNewGame()
    {
        GameContext context = FindFirstObjectByType<GameContext>();
        if (context != null)
            context.ExitNewGame();
    }

    private void OnDestroy()
    {
        if (exitButton != null)
            exitButton.onClick.RemoveListener(ExitNewGame);
    }
}
