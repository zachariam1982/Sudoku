using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResponsiveTopBar : MonoBehaviour
{
    [Header("TopBar Items")]
    [SerializeField] private RectTransform homeBlock;
    [SerializeField] private RectTransform livesContainer;
    [SerializeField] private RectTransform level;
    [SerializeField] private RectTransform timer;
    [SerializeField] private RectTransform pauseBlock;
    [SerializeField] private RectTransform settingsBlock;
    [SerializeField] private Sprite exitButtonSprite;


    [Header("Scaling")]
    [SerializeField] [Range(1f, 2f)] private float landscapeScale = 1.4f;
    [SerializeField] private float portraitScale = 1f;

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
    private RectTransform _exitButtonBlock;

    private void Awake()
    {
        _topBar = GetComponent<RectTransform>();
        _layout = GetComponent<HorizontalLayoutGroup>();
        CreateExitButton();

        if (_layout != null)
        {
            _layout.childScaleWidth = true;
            _layout.childScaleHeight = true;
            _originalSpacing = _layout.spacing;
            _originalAlignment = _layout.childAlignment;
            _originalExpandWidth = _layout.childForceExpandWidth;
            _originalControlWidth = _layout.childControlWidth;

            RectTransform[] items = new RectTransform[]
            {
                homeBlock, livesContainer, level, timer, pauseBlock, settingsBlock,
                _exitButtonBlock
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

        ApplyScale(
            _exitButtonBlock,
            targetScale);


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

        if (isLandscape)
        {
            // Give every visible item the same slot. This keeps the title and
            // controls evenly distributed on wide screens, like the bottom HUD.
            _layout.spacing = 0;
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
        if (_exitButtonBlock == null) return;
        if (_exitButtonBlock.gameObject.activeSelf != isNewGame)
            _exitButtonBlock.gameObject.SetActive(isNewGame);

        if (_topBar != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(_topBar);
    }

    private void CreateExitButton()
    {
        var buttonObject = new GameObject(
            "ExitNewGameBlock",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement));
        _exitButtonBlock = buttonObject.GetComponent<RectTransform>();
        _exitButtonBlock.SetParent(transform, false);
        _exitButtonBlock.sizeDelta = new Vector2(100f, 100f);

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = exitButtonSprite;
        image.type = Image.Type.Sliced;
        image.preserveAspect = false;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        button.colors = colors;
        button.onClick.AddListener(ExitNewGame);

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        layoutElement.minWidth = 93.33f;
        layoutElement.preferredWidth = 100f;
        layoutElement.preferredHeight = 84f;

        var labelObject = new GameObject(
            "ExitLabel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(_exitButtonBlock, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 4f);
        labelRect.offsetMax = new Vector2(-8f, -4f);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = "EXIT";
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = 30f;
        label.fontStyle = FontStyles.Bold;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;

        buttonObject.SetActive(false);
    }

    private void ExitNewGame()
    {
        GameContext context = FindObjectOfType<GameContext>();
        if (context != null)
            context.ExitNewGame();
    }
}
