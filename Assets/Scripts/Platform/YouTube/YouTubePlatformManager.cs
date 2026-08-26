using UnityEngine;
using YTGameSDK;

public class YouTubePlatformManager : MonoBehaviour
{
    public static YouTubePlatformManager Instance
    {
        get;
        private set;
    }

    [System.NonSerialized]
    private YTGameWrapper ytGameWrapper;

    private bool _firstFrameSent;
    private bool _gameReadySent;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        YTGameWrapper wrapper = GetWrapper();

        if (wrapper != null)
        {
            wrapper.SetOnPauseCallback(OnYouTubePause);
        }
        else
        {
            Debug.LogError(
                "[YouTube] YTGameWrapper not found."
            );
        }

        SendFirstFrameReady();
    }

    private YTGameWrapper GetWrapper()
    {
        if (ytGameWrapper == null)
        {
            ytGameWrapper =
                FindFirstObjectByType<YTGameWrapper>();
        }

        return ytGameWrapper;
    }

    public void SendFirstFrameReady()
    {
        if (_firstFrameSent)
            return;

        YTGameWrapper wrapper = GetWrapper();

        if (wrapper == null)
        {
            Debug.LogError(
                "[YouTube] Cannot send firstFrameReady - YTGameWrapper not found."
            );

            return;
        }

        wrapper.SendGameFirstFrameReady();

        _firstFrameSent = true;

        Debug.Log(
            "[YouTube] firstFrameReady sent."
        );
    }

    public void SendGameReady()
    {
        if (_gameReadySent)
            return;

        if (!_firstFrameSent)
        {
            SendFirstFrameReady();
        }

        YTGameWrapper wrapper = GetWrapper();

        if (wrapper == null)
        {
            Debug.LogError(
                "[YouTube] Cannot send gameReady - YTGameWrapper not found."
            );

            return;
        }

        wrapper.SendGameIsReady();

        _gameReadySent = true;

        Debug.Log(
            "[YouTube] gameReady sent."
        );
    }

    private void OnYouTubePause()
    {
        Debug.Log(
            "[YouTube] Pause requested."
        );

        if (User.Instance?.ViewModel == null)
            return;

        SudokuViewModel vm =
            User.Instance.ViewModel;

        // Save immediately before YouTube may evict us.
        SaveSystem.Save(
            vm.GetSaveData()
        );

        if (
            GameStateMachine.Instance != null &&
            GameStateMachine.Instance.CurrentState
                is PlayingState
        )
        {
            vm.PauseCommand.Execute();
        }
    }
}