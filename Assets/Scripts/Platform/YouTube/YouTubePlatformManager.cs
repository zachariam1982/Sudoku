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

        if (!User.Instance.InitialCloudLoadComplete) return;
        
        SudokuViewModel vm = User.Instance.ViewModel;

        // Save immediately before YouTube may evict us.
        SaveGameData data = vm.GetSaveData();

        SaveSystem.Save(data);
        SaveCloudData(data);

        if (GameStateMachine.Instance != null && GameStateMachine.Instance.CurrentState is PlayingState)
        {
            vm.PauseCommand.Execute();
        }
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