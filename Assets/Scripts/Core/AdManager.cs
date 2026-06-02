using System;
using UnityEngine;
using UnityEngine.Video;
using Unity.Services.LevelPlay;

public class AdManager : MonoBehaviour
{
    public static AdManager Instance { get; private set; }

    [Header("LevelPlay Credentials — from platform.ironsrc.com")]
    [Tooltip("Your App Key from LevelPlay dashboard → App Settings")]
    [SerializeField] private string androidAppKey = "your-android-app-key";
    [SerializeField] private string iosAppKey     = "your-ios-app-key";

    [Tooltip("Rewarded Ad Unit ID from LevelPlay dashboard → Ad Units")]
    [SerializeField] private string rewardedAdUnitId = "your-rewarded-ad-unit-id";

    [Header("In-Dialog Video (local video shown inside RawImage)")]
    [SerializeField] private VideoClip     videoClip;
    [SerializeField] private string        videoUrl  = "";

    [Header("Render Target")]
    [Tooltip("Create a RenderTexture asset and assign here AND on your dialog RawImage")]
    [SerializeField] private RenderTexture renderTexture;

    [Header("Settings")]
    [SerializeField] private bool testMode = true;

    // ── Private state ─────────────────────────────────────────────────────────
    private LevelPlayRewardedAd _rewardedAd;
    private VideoPlayer         _videoPlayer;
    private Action              _onCompleted;
    private Action              _onFailed;
    private bool                _rewardGranted  = false;
    private bool                _realAdWasShown = false;
    private bool                _isInitialised  = false;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetupVideoPlayer();
    }

    void Start()
    {
        InitialiseLevelPlay();
    }

    void OnDestroy()
    {
        DisposeRewardedAd();
    }

    // ── Initialisation ────────────────────────────────────────────────────────

private void InitialiseLevelPlay()
    {
        string appKey = Application.platform == RuntimePlatform.IPhonePlayer
            ? iosAppKey : androidAppKey;

        if (string.IsNullOrEmpty(appKey) || appKey.StartsWith("your-"))
        {
            Debug.LogWarning("[AdManager] App key not set in Inspector.");
            return;
        }

        // Subscribe to SDK init callback before calling init
        LevelPlay.OnInitSuccess += OnLevelPlayInitSuccess;
        LevelPlay.OnInitFailed  += OnLevelPlayInitFailed;

        // FIXED WAY: No LevelPlayInitRequest builder object is required in Unity C#.
        // Just pass your App Key directly as a string.
        LevelPlay.Init(appKey);

        Debug.Log($"[AdManager] LevelPlay initialising. AppKey: {appKey}");
    }

    private void OnLevelPlayInitSuccess(LevelPlayConfiguration config)
    {
        LevelPlay.OnInitSuccess -= OnLevelPlayInitSuccess;
        LevelPlay.OnInitFailed  -= OnLevelPlayInitFailed;

        _isInitialised = true;
        Debug.Log("[AdManager] LevelPlay init success.");

        // NEW WAY: Enforce automatic engine pausing during fullscreen ads instead of overriding OnApplicationPause manually
        LevelPlay.SetPauseGame(true);

        // Create and load the rewarded ad unit now that SDK is ready
        CreateAndLoadRewardedAd();
    }

    private void OnLevelPlayInitFailed(LevelPlayInitError error)
    {
        LevelPlay.OnInitSuccess -= OnLevelPlayInitSuccess;
        LevelPlay.OnInitFailed  -= OnLevelPlayInitFailed;

        Debug.LogError($"[AdManager] LevelPlay init failed: {error.ErrorMessage}");
    }

    // ── Rewarded Ad Setup ─────────────────────────────────────────────────────

    private void CreateAndLoadRewardedAd()
    {
        DisposeRewardedAd(); // clean up any previous instance

        _rewardedAd = new LevelPlayRewardedAd(rewardedAdUnitId);

        // Subscribe to all rewarded ad events
        _rewardedAd.OnAdLoaded          += OnAdLoaded;
        _rewardedAd.OnAdLoadFailed      += OnAdLoadFailed;
        _rewardedAd.OnAdDisplayed       += OnAdDisplayed;
        _rewardedAd.OnAdDisplayFailed   += OnAdDisplayFailed;
        _rewardedAd.OnAdClicked         += OnAdClicked;
        _rewardedAd.OnAdClosed          += OnAdClosed;
        _rewardedAd.OnAdRewarded        += OnAdRewarded;

        // Start loading — SDK downloads the ad creative in the background
        _rewardedAd.LoadAd();
        Debug.Log("[AdManager] Loading rewarded ad…");
    }

    private void DisposeRewardedAd()
    {
        if (_rewardedAd == null) return;

        _rewardedAd.OnAdLoaded        -= OnAdLoaded;
        _rewardedAd.OnAdLoadFailed    -= OnAdLoadFailed;
        _rewardedAd.OnAdDisplayed     -= OnAdDisplayed;
        _rewardedAd.OnAdDisplayFailed -= OnAdDisplayFailed;
        _rewardedAd.OnAdClicked       -= OnAdClicked;
        _rewardedAd.OnAdClosed        -= OnAdClosed;
        _rewardedAd.OnAdRewarded      -= OnAdRewarded;

        _rewardedAd = null;
    }

    // ── Rewarded Ad Event Handlers ────────────────────────────────────────────

    private void OnAdLoaded(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"[AdManager] Rewarded ad loaded. Network: {adInfo.AdNetwork}");
    }

    private void OnAdLoadFailed(LevelPlayAdError error)
    {
        Debug.LogWarning($"[AdManager] Rewarded ad load failed: {error.ErrorMessage}");
        // Retry loading after 30 seconds
        Invoke(nameof(CreateAndLoadRewardedAd), 30f);
    }

    private void OnAdDisplayed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("[AdManager] Rewarded ad displayed fullscreen.");
        _realAdWasShown = true;
    }

    private void OnAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        // Replaced error.Error.ErrorMessage with direct error.ErrorMessage access
        Debug.LogWarning($"[AdManager] Rewarded ad display failed: {error.ErrorMessage}");
        _realAdWasShown = false;
        // Local video fallback — its OnLocalVideoCompleted will fire _onCompleted
    }

    private void OnAdClicked(LevelPlayAdInfo adInfo)
    {
        Debug.Log("[AdManager] Rewarded ad clicked.");
    }

    private void OnAdRewarded(LevelPlayAdInfo adInfo, LevelPlayReward reward)
    {
        // Fires BEFORE OnAdClosed when the user earns the reward
        Debug.Log($"[AdManager] Reward earned: {reward.Name} x{reward.Amount}");
        _rewardGranted = true;
    }

    private void OnAdClosed(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"[AdManager] Rewarded ad closed. Reward granted: {_rewardGranted}");

        if (_rewardGranted)
        {
            _onCompleted?.Invoke();
        }
        else
        {
            // User closed before earning reward
            _onFailed?.Invoke();
        }

        // Reset flags and pre-load next ad
        _rewardGranted  = false;
        _realAdWasShown = false;
        _onCompleted    = null;
        _onFailed       = null;

        // Pre-load the next ad immediately so it is ready for next time
        CreateAndLoadRewardedAd();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Returns the RenderTexture for the dialog's RawImage to display.</summary>
    public RenderTexture GetRenderTexture() => renderTexture;

    /// <summary>True when a rewarded ad is loaded and ready to show.</summary>
    public bool IsAdReady() =>
        _rewardedAd != null && _rewardedAd.IsAdReady();

    /// <summary>
    /// Called by PencilAdDialog.
    /// Plays local video in the RawImage and shows the real ad fullscreen.
    /// </summary>
    public void PlayAd(Action onCompleted, Action onFailed)
    {
        _onCompleted    = onCompleted;
        _onFailed       = onFailed;
        _rewardGranted  = false;
        _realAdWasShown = false;

        // Always play local video in the dialog's RawImage area
        PlayLocalVideo();

        if (!_isInitialised)
        {
            Debug.LogWarning("[AdManager] SDK not initialised yet — using local video only.");
            return;
        }

        if (IsAdReady())
        {
            _rewardedAd.ShowAd();
        }
        else
        {
            Debug.LogWarning("[AdManager] No ad ready — using local video only as fallback.");
        }
    }

    /// <summary>Called when dialog close button is pressed mid-ad.</summary>
    public void StopAd()
    {
        _videoPlayer.prepareCompleted -= OnLocalVideoPrepared; // cancel any pending prepare

        if (_videoPlayer != null && _videoPlayer.isPlaying)
            _videoPlayer.Stop();

        _videoPlayer.time  = 0;
        _videoPlayer.frame = 0;

        ClearRenderTexture();
        _rewardGranted  = false;
        _realAdWasShown = false;
        _onCompleted    = null;
        _onFailed       = null;
    }

    public float GetVideoDuration()
    {
        if (_videoPlayer == null) return 0f;
        if (_videoPlayer.clip != null) return (float)_videoPlayer.clip.length;
        if (_videoPlayer.frameRate > 0 && _videoPlayer.frameCount > 0)
            return _videoPlayer.frameCount / _videoPlayer.frameRate;
        return 0f;
    }

    public float GetCurrentTime() =>
        _videoPlayer != null ? (float)_videoPlayer.time : 0f;

    // ── Local VideoPlayer ─────────────────────────────────────────────────────

    private void SetupVideoPlayer()
    {
        if (renderTexture == null)
            Debug.LogError("[AdManager] RenderTexture is NULL — assign it in Inspector!");

        _videoPlayer = gameObject.AddComponent<VideoPlayer>();
        _videoPlayer.playOnAwake       = false;
        _videoPlayer.waitForFirstFrame = true;
        _videoPlayer.isLooping         = false;
        _videoPlayer.renderMode        = VideoRenderMode.RenderTexture;
        _videoPlayer.targetTexture     = renderTexture;
        _videoPlayer.audioOutputMode   = VideoAudioOutputMode.Direct;

        _videoPlayer.loopPointReached += OnLocalVideoCompleted;
        _videoPlayer.errorReceived    += OnLocalVideoError;
    }

    private void PlayLocalVideo()
    {
        if (_videoPlayer.isPlaying)
            _videoPlayer.Stop();

        _videoPlayer.prepareCompleted -= OnLocalVideoPrepared;

        ClearRenderTexture();

        if (videoClip != null)
        {
            _videoPlayer.source = VideoSource.VideoClip;
            _videoPlayer.clip   = videoClip;
        }
        else if (!string.IsNullOrEmpty(videoUrl))
        {
            _videoPlayer.source = VideoSource.Url;
            _videoPlayer.url    = videoUrl;
        }
        else
        {
            Debug.LogWarning("[AdManager] No VideoClip or URL assigned.");
            return;
        }

        _videoPlayer.time = 0;

        if (_videoPlayer.isPrepared)
        {
            // Already prepared — just play directly
            _videoPlayer.Play();
        }
        else
        {
            _videoPlayer.prepareCompleted += OnLocalVideoPrepared;
            _videoPlayer.Prepare();
        }
    }
    private void OnLocalVideoPrepared(VideoPlayer vp)
    {
        vp.prepareCompleted -= OnLocalVideoPrepared;
        vp.Play();
    }

    private void OnLocalVideoCompleted(VideoPlayer vp)
    {
        // Only grant reward via local video if NO real ad was shown
        if (!_realAdWasShown)
        {
            Debug.Log("[AdManager] Local video completed — granting reward as fallback.");
            _onCompleted?.Invoke();
            _onCompleted = null;
            _onFailed    = null;
        }
    }

    private void OnLocalVideoError(VideoPlayer vp, string message)
    {
        Debug.LogError($"[AdManager] Local video error: {message}");
        if (!_realAdWasShown)
        {
            _onFailed?.Invoke();
            _onCompleted = null;
            _onFailed    = null;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ClearRenderTexture()
    {
        if (renderTexture == null) return;
        RenderTexture prev   = RenderTexture.active;
        RenderTexture.active = renderTexture;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = prev;
    }
}