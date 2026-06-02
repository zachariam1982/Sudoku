using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SOSAdDialog : MonoBehaviour
{
    public static SOSAdDialog Instance { get; private set; }
    private GameObject dialogPanel;

    [Header("Video Display")]
    [SerializeField] private RawImage        videoArea;
    [SerializeField] private Texture         placeholderTex;

    [Header("Progress")]
    [SerializeField] private RectTransform   progressBarFill;
    [SerializeField] private TextMeshProUGUI timerLabel;

    [Header("Close Button")]
    [SerializeField] private Button          closeButton;

    [Header("Status")]
    [SerializeField] private TextMeshProUGUI statusLabel;
    [SerializeField] private GameObject      loadingSpinner;

    [Header("Animation")]
    [SerializeField] private float fadeInDuration  = 0.2f;
    [SerializeField] private float fadeOutDuration = 0.15f;

    private CanvasGroup     _cg;
    private SudokuViewModel _vm;
    private bool            _adCompleted  = false;
    private bool            _adInProgress = false;
    private Coroutine       _progressCoroutine;
    void Awake()
    {
        Instance = this;
        dialogPanel = this.gameObject;

        _cg = dialogPanel.GetComponent<CanvasGroup>();
        if (_cg == null) _cg = dialogPanel.AddComponent<CanvasGroup>();

        dialogPanel.SetActive(false);

        closeButton.onClick.AddListener(OnClosePressed);
        closeButton.gameObject.SetActive(false);
    }

    public void Bind(SudokuViewModel vm)
    { 
        _vm = vm;
        _vm.IsSOSMode.OnChanged += Show;
    }

    // ── Show ──────────────────────────────────────────────────────────────────

    public void Show(bool arg)
    {
        dialogPanel.SetActive(arg);

        if ( arg == false) return;
        //if (_adDialog == true) return;        
        if (_adInProgress) return;

        ResetDialog();
        _cg.alpha = 0f;

        StopAllCoroutines();
        StartCoroutine(FadeTo(1f, fadeInDuration, onDone: StartAd));
    }

    private void ResetDialog()
    {
        _adCompleted  = false;
        _adInProgress = false;
        //_adDialog = true;


        SetStatus("Loading…");
        SetSpinner(true);
        closeButton.gameObject.SetActive(false);

        if (videoArea != null && placeholderTex != null)
            videoArea.texture = placeholderTex;

        SetProgressFill(0f);
        SetTimerText(0f, 0f);
    }

    // ── Ad Playback ───────────────────────────────────────────────────────────

    private void StartAd()
    {
        if (AdManager.Instance == null)
        {
            SetStatus("Ad unavailable.");
            SetSpinner(false);
            ShowCloseButton();
            return;
        }

        _adInProgress = true;

        if (videoArea != null)
            videoArea.texture = AdManager.Instance.GetRenderTexture();

        AdManager.Instance.PlayAd(
            onCompleted: OnAdCompleted,
            onFailed:    OnAdFailed
        );

        SetStatus("");
        SetSpinner(false);

        if (_progressCoroutine != null) StopCoroutine(_progressCoroutine);
        _progressCoroutine = StartCoroutine(UpdateProgress());
    }

    // ── Progress ──────────────────────────────────────────────────────────────

    private IEnumerator UpdateProgress()
    {
        while (_adInProgress && AdManager.Instance != null)
        {
            float duration = AdManager.Instance.GetVideoDuration();
            float current  = AdManager.Instance.GetCurrentTime();

            if (duration > 0f)
            {
                SetProgressFill(Mathf.Clamp01(current / duration));
                SetTimerText(current, duration);
            }

            yield return null;
        }
    }

    private void SetProgressFill(float ratio)
    {
        if (progressBarFill == null) return;
        Vector3 s = progressBarFill.localScale;
        s.x = ratio;
        progressBarFill.localScale = s;
    }

    private void SetTimerText(float current, float duration)
    {
        if (timerLabel == null) return;
        float remaining = Mathf.Max(0f, duration - current);
        timerLabel.text = $"0:{Mathf.CeilToInt(remaining):00} remaining";
    }

    // ── Ad Callbacks ──────────────────────────────────────────────────────────

    private void OnAdCompleted()
    {
        _adInProgress = false;
        _adCompleted  = true;

        if (_progressCoroutine != null) StopCoroutine(_progressCoroutine);
        SetProgressFill(1f);
        SetTimerText(0f, 0f);
        SetStatus("Done! Close to enable pencil mode.");
        ShowCloseButton();
    }

    private void OnAdFailed()
    {
        _adInProgress = false;

        if (_progressCoroutine != null) StopCoroutine(_progressCoroutine);
        SetStatus("Ad unavailable. Try again later.");
        ShowCloseButton();
    }

    // ── Close ─────────────────────────────────────────────────────────────────

    private void OnClosePressed()
    {
        if (_progressCoroutine != null) StopCoroutine(_progressCoroutine);
        
        // Always stop/reset the AdManager, not just when mid-play
        AdManager.Instance?.StopAd();
        _adInProgress = false;

        if (_adCompleted && _vm != null)
            _vm.ApplySOSHint();

        Hide();
    }

    private void Hide()
    {
        //_adDialog = false;
        StopAllCoroutines();
        StartCoroutine(FadeTo(0f, fadeOutDuration, onDone: () =>
            dialogPanel.SetActive(false)));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ShowCloseButton()       => closeButton.gameObject.SetActive(true);
    private void SetStatus(string msg)   { if (statusLabel    != null) statusLabel.text = msg; }
    private void SetSpinner(bool visible){ if (loadingSpinner  != null) loadingSpinner.SetActive(visible); }

    private IEnumerator FadeTo(float target, float duration, System.Action onDone = null)
    {
        float start = _cg.alpha, elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed   += Time.deltaTime;
            _cg.alpha  = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        _cg.alpha = target;
        onDone?.Invoke();
    }
}