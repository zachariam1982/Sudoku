using System;
using UnityEngine;
using Unity.Services.LevelPlay;

public class AdManager : MonoBehaviour
{
    public static AdManager Instance { get; private set; }

    private string androidAppKey    = "268449ef5";
    private string iosAppKey        = "268454b8d";
    private string rewardedAdUnitId = "9a823d25w9b6odf8";

    // ── Private state ─────────────────────────────────────────────────────────
    private LevelPlayRewardedAd _rewardedAd;
    private Action              _onCompleted;
    private Action              _onFailed;
    private bool                _rewardGranted = false;
    private bool                _isInitialised = false;
    private bool _callbackInvoked;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
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

        LevelPlay.OnInitSuccess += OnLevelPlayInitSuccess;
        LevelPlay.OnInitFailed  += OnLevelPlayInitFailed;
        LevelPlay.Init(appKey);

        Debug.Log($"[AdManager] LevelPlay initialising. AppKey: {appKey}");
    }

    private void OnLevelPlayInitSuccess(LevelPlayConfiguration config)
    {
        LevelPlay.OnInitSuccess -= OnLevelPlayInitSuccess;
        LevelPlay.OnInitFailed  -= OnLevelPlayInitFailed;

        _isInitialised = true;
        LevelPlay.SetPauseGame(true);
        Debug.Log("[AdManager] LevelPlay init success.");

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
        DisposeRewardedAd();

        _rewardedAd = new LevelPlayRewardedAd(rewardedAdUnitId);

        _rewardedAd.OnAdLoaded        += OnAdLoaded;
        _rewardedAd.OnAdLoadFailed    += OnAdLoadFailed;
        _rewardedAd.OnAdDisplayed     += OnAdDisplayed;
        _rewardedAd.OnAdDisplayFailed += OnAdDisplayFailed;
        _rewardedAd.OnAdClicked       += OnAdClicked;
        _rewardedAd.OnAdClosed        += OnAdClosed;
        _rewardedAd.OnAdRewarded      += OnAdRewarded;

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
        => Debug.Log($"[AdManager] Rewarded ad loaded. Network: {adInfo.AdNetwork}");

    private void OnAdLoadFailed(LevelPlayAdError error)
    {
        Debug.LogWarning($"[AdManager] Rewarded ad load failed: {error.ErrorMessage}");
        Invoke(nameof(RetryLoadRewardedAd), 30f);
    }

    private void RetryLoadRewardedAd()
    {
        _rewardedAd?.LoadAd();
    }

    private void OnAdDisplayed(LevelPlayAdInfo adInfo)
        => Debug.Log("[AdManager] Rewarded ad displayed.");

    private void OnAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.LogWarning($"[AdManager] Rewarded ad display failed: {error.ErrorMessage}");
        CompleteOnce(false);
        if(_rewardedAd != null) _rewardedAd.LoadAd();
    }

    private void OnAdClicked(LevelPlayAdInfo adInfo)
        => Debug.Log("[AdManager] Rewarded ad clicked.");

    private void OnAdRewarded(LevelPlayAdInfo adInfo, LevelPlayReward reward)
    {
        Debug.Log($"[AdManager] Reward earned: {reward.Name} x{reward.Amount}");
        _rewardGranted = true;

        CompleteOnce(true);
    }

    private void OnAdClosed(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"[AdManager] Rewarded ad closed. Reward granted: {_rewardGranted}");

        CompleteOnce(false);

        if(_rewardedAd != null) _rewardedAd.LoadAd();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public bool IsAdReady() =>
        _isInitialised && _rewardedAd != null && _rewardedAd.IsAdReady();

    /// <summary>
    /// Shows the rewarded ad fullscreen.
    /// onCompleted fires when the user earns the reward.
    /// onFailed fires when the ad fails or the user skips without reward.
    /// </summary>
    public void PlayAd(Action onCompleted, Action onFailed)
    {
        _onCompleted   = onCompleted;
        _onFailed      = onFailed;
        _rewardGranted = false;
        _callbackInvoked = false;

        if (!_isInitialised)
        {
            Debug.LogWarning("[AdManager] SDK not initialised yet. Calling onFailed path");
            CompleteOnce(false);
            return;
        }

        if (IsAdReady())
        {
            _rewardedAd.ShowAd();
        }
        else
        {
            Debug.LogWarning("[AdManager] No ad ready. Calling onFailed path.");
            CompleteOnce(false);
        }
    }

    private void CompleteOnce(bool rewarded)
    {
        if (_callbackInvoked) return;

        _callbackInvoked = true;

        Action callback = rewarded ? _onCompleted : _onFailed;

        _onCompleted = null;
        _onFailed = null;

        callback?.Invoke();
    }
}