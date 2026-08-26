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

        if (ytGameWrapper != null) ytGameWrapper.SetOnPauseCallback( OnYouTubePause );
        else Debug.LogError("[YouTube] YTGameWrapper not found.");

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
        Debug.Log("[YouTube] Pause requested.");

        if (User.Instance?.ViewModel == null) return;

        SudokuViewModel vm = User.Instance.ViewModel;

        // Save immediately before YouTube may evict us.
        SaveSystem.Save(vm.GetSaveData());

        if (GameStateMachine.Instance != null && GameStateMachine.Instance.CurrentState is PlayingState)
        {
            vm.PauseCommand.Execute();
        }
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