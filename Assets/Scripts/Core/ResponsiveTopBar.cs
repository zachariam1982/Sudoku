using UnityEngine;
using UnityEngine.UI;

public class ResponsiveTopBar : MonoBehaviour
{
    [Header("TopBar Items")]
    [SerializeField]
    private RectTransform livesContainer;

    [SerializeField]
    private RectTransform level;

    [SerializeField]
    private RectTransform timer;

    [SerializeField]
    private RectTransform pauseBlock;


    [Header("Scaling")]
    [SerializeField]
    [Range(1f, 2f)]
    private float landscapeScale = 1.4f;

    [SerializeField]
    private float portraitScale = 1f;


    private bool? _lastLandscape;

    private RectTransform _topBar;


    private void Awake()
    {
        _topBar =
            GetComponent<RectTransform>();

        /*
         * Make sure the layout system takes
         * child scaling into account.
         */
        HorizontalLayoutGroup layout =
            GetComponent<HorizontalLayoutGroup>();

        if (layout != null)
        {
            layout.childScaleWidth = true;
            layout.childScaleHeight = true;
        }
    }


    private void OnEnable()
    {
        _lastLandscape = null;

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
        if (_lastLandscape.HasValue &&
            _lastLandscape.Value ==
            isLandscape)
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

        float scale =
            isLandscape
                ? landscapeScale
                : portraitScale;

        Vector3 targetScale =
            new Vector3(
                scale,
                scale,
                1f);

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


    private static void ApplyScale(
        RectTransform target,
        Vector3 scale)
    {
        if (target == null)
            return;

        target.localScale =
            scale;
    }
}