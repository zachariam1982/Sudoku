#if UNITY_WEBGL && !UNITY_EDITOR

using System.Runtime.InteropServices;
using UnityEngine;
using YTGameSDK;

public class YouTubePlatformManager : MonoBehaviour
{
    public static YouTubePlatformManager Instance
    {
        get;
        private set;
    }

    [DllImport("__Internal")]
    private static extern void YT_FirstFrameReady();

    [DllImport("__Internal")]
    private static extern void YT_GameReady();

    [System.NonSerialized]
    private YTGameWrapper ytGameWrapper;

    private bool _firstFrameSent;
    private bool _gameReadySent;

    // YouTube lifecycle state
    private bool _youtubeSystemPaused = false;

    // True only when YouTube itself moved Playing -> Paused.
    // Prevents onResume from resuming a game that the player
    // manually paused.
    private bool _pausedGameplayForYouTube = false;

    // Preserve Unity runtime state while YouTube owns the screen.
    private float _timeScaleBeforeYouTubePause = 1f;
    private bool _stateMachineWasEnabled = true;

    // YouTube audio state
    private bool _youtubeAudioEnabled = true;
    private float _normalAudioVolume = 1f;

    // Used when an SDK result arrives while the game is paused.
    // In particular this protects the rewarded-SOS flow.
    private System.Action _pendingAfterResume;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (ytGameWrapper == null) ytGameWrapper = FindFirstObjectByType<YTGameWrapper>();

        if (ytGameWrapper != null)
        {
            // Pause / resume
            ytGameWrapper.SetOnPauseCallback(OnYouTubePause);
            ytGameWrapper.SetOnResumeCallback(OnYouTubeResume);

            _normalAudioVolume = AudioListener.volume;

            ytGameWrapper.SetOnAudioEnabledChangeCallback(OnYouTubeAudioEnabledChanged);
            ApplyYouTubeAudioState(ytGameWrapper.IsYTGameAudioEnabled());
        }
        else
        {
            Debug.LogError("[YouTube] YTGameWrapper not found.");
        }

        SendFirstFrameReady();
    }

    public void SendFirstFrameReady()
    {
        if (_firstFrameSent) return;

        _firstFrameSent = true;

        YT_FirstFrameReady();

        Debug.Log("[YouTube] firstFrameReady sent.");
    }

    public void SendGameReady()
    {
        if (_gameReadySent) return;

        if (!_firstFrameSent) SendFirstFrameReady();

        _gameReadySent = true;

        YT_GameReady();

        Debug.Log("[YouTube] gameReady sent.");
    }

    private void OnYouTubePause()
    {
        if (_youtubeSystemPaused) return;

        _youtubeSystemPaused = true;
        SudokuViewModel vm = User.Instance?.ViewModel;

        //
        // 1. Save before YouTube may evict the game.
        //
        if (vm != null && User.Instance.InitialCloudLoadComplete)
        {
            SaveGameData data = vm.GetSaveData();

            SaveSystem.Save(data);
            SaveCloudData(data);
        }

        //
        // 2. Remember whether WE caused the gameplay pause.
        //
        _pausedGameplayForYouTube = vm != null && GameStateMachine.Instance != null && GameStateMachine.Instance.CurrentState is PlayingState;

        if (_pausedGameplayForYouTube) vm.PauseCommand.Execute();

        //
        // 3. Stop normal Unity game execution.
        //
        _timeScaleBeforeYouTubePause = Time.timeScale;
        Time.timeScale = 0f;

        if (GameStateMachine.Instance != null)
        {
            _stateMachineWasEnabled = GameStateMachine.Instance.enabled;
            GameStateMachine.Instance.enabled = false;
        }

        //
        // 4. Stop audio execution as well.
        //
        AudioListener.pause = true;
        Debug.Log("[YouTube] Game execution paused.");
    }

    private void OnYouTubeResume()
    {
        if (!_youtubeSystemPaused) return;

        Time.timeScale = _timeScaleBeforeYouTubePause;

        if (GameStateMachine.Instance != null) GameStateMachine.Instance.enabled = _stateMachineWasEnabled;

        AudioListener.pause = false;
        _youtubeSystemPaused = false;

        SudokuViewModel vm = User.Instance?.ViewModel;

        if (_pausedGameplayForYouTube && vm != null && GameStateMachine.Instance != null && GameStateMachine.Instance.CurrentState is PausedState)
            vm.ResumeCommand.Execute();

        _pausedGameplayForYouTube = false;

        System.Action pending = _pendingAfterResume;
        _pendingAfterResume = null;
        pending?.Invoke();

        Debug.Log("[YouTube] Game execution resumed.");
    }

    private void OnYouTubeAudioEnabledChanged(bool isAudioEnabled)
    {
        Debug.Log($"[YouTube] Audio enabled changed: {isAudioEnabled}");

        ApplyYouTubeAudioState(isAudioEnabled);
    }

    private void ApplyYouTubeAudioState(bool isAudioEnabled)
    {
        _youtubeAudioEnabled = isAudioEnabled;

        AudioListener.volume = isAudioEnabled ? _normalAudioVolume : 0f;
    }

    public void LoadCloudData(System.Action<string> onLoaded)
    {
        if (ytGameWrapper == null) ytGameWrapper = FindFirstObjectByType<YTGameWrapper>();

        if (ytGameWrapper == null)
        {
            onLoaded?.Invoke(null);
            return;
        }

        // When running directly in a normal browser,
        // fall back to the existing local save.
        if (!ytGameWrapper.InPlayablesEnv())
        {   
            onLoaded?.Invoke(null);
            return;
        }

        ytGameWrapper.LoadGameSaveData(
            data =>
            {
                int byteCount = System.Text.Encoding.UTF8.GetByteCount(data ?? string.Empty);

                onLoaded?.Invoke(data);
            }
        );
    }
    public void SaveCloudData(SaveGameData data)
    {
        if (data == null || ytGameWrapper == null)
            return;

        string json = JsonUtility.ToJson(
            data,
            prettyPrint: false
        );

        int byteCount =
            System.Text.Encoding.UTF8.GetByteCount(json);

        const int MaxSaveBytes =
            3 * 1024 * 1024;

        if (byteCount >= MaxSaveBytes)
        {
            Debug.LogError(
                $"[YouTube] Save data too large: {byteCount} bytes"
            );

            return;
        }

        ytGameWrapper.SendGameSaveData(json);

        Debug.Log(
            $"[YouTube] Cloud save sent: {byteCount} bytes"
        );
    }

    public void SendScore(int score)
    {
        if (ytGameWrapper == null)
            return;

        ytGameWrapper.SendGameScore(score);

        Debug.Log(
            $"[YouTube] Score sent: {score}"
        );
    }
    public void RequestSOSRewardedAd(System.Action<bool> onCompleted)
    {
        if (ytGameWrapper == null)
            ytGameWrapper = FindFirstObjectByType<YTGameWrapper>();

        if (ytGameWrapper == null)
        {
            onCompleted?.Invoke(false);
            return;
        }

        ytGameWrapper.RequestRewardedAd("sudoku-sos-hint", rewardEarned =>
            {
                System.Action deliverResult = () => { onCompleted?.Invoke( rewardEarned ); };

                if (_youtubeSystemPaused)
                    _pendingAfterResume += deliverResult;
                else
                    deliverResult();
            }
        );
    }
}

#else

using UnityEngine;

public class YouTubePlatformManager : MonoBehaviour
{
    public static YouTubePlatformManager Instance
    {
        get;
        private set;
    }

    private void Awake()
    {
        Instance = this;
    }

    public void SendFirstFrameReady()
    {
    }

    public void SendGameReady()
    {
    }
}

#endif