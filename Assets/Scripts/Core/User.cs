using System.Collections;
using System.Threading;
using UnityEngine;

public class User : MonoBehaviour
{
    public static User Instance { get; private set;}
    public JourneyViewModel ViewModel { get; set; }
    public NewGameViewModel NewGameViewModel { get; set; }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    #if UNITY_WEBGL && !UNITY_EDITOR

    private bool _initialCloudLoadComplete = false;
    public bool InitialCloudLoadComplete => _initialCloudLoadComplete;

    #else

    public bool InitialCloudLoadComplete => true;

    #endif
    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
 
        Instance = this;
        DontDestroyOnLoad(gameObject);  // survive scene reloads
 
        GameDatabase.Init(); 
        StartCoroutine(SaveLoop());
    }
    public bool TryLoadSave()
    {
        SaveGameData data = SaveSystem.Load(SaveSlot.Journey);

        if (data != null)
        {
            ViewModel?.LoadSaveData(data);
            return true;
        }

        if (ViewModel == null) return false;

        bool recovered = ViewModel.RecoverFromHistory();

        if (recovered) SaveSystem.Save(ViewModel.GetSaveData(), SaveSlot.Journey);

        return recovered;
    }

    public bool TryLoadNewGameSave()
    {
        SaveGameData data = SaveSystem.Load(SaveSlot.NewGame);
        return data != null && NewGameViewModel != null && NewGameViewModel.LoadSaveData(data);
    }
    public void TryLoadSaveFromCloud(System.Action onCompleted)
    {
        #if UNITY_WEBGL && !UNITY_EDITOR

        _initialCloudLoadComplete = false;

        if (ViewModel == null)
        {
            Debug.LogWarning("[YouTube] ViewModel not available during cloud load.");

            _initialCloudLoadComplete = true;
            onCompleted?.Invoke();
            return;
        }

        if (YouTubePlatformManager.Instance == null)
        {
            Debug.LogWarning("[YouTube] Platform manager unavailable. Falling back to local save.");

            TryLoadSave();

            _initialCloudLoadComplete = true;
            onCompleted?.Invoke();
            return;
        }

        YouTubePlatformManager.Instance.LoadCloudData(
            json =>
            {
                bool loadedFromCloud = false;

                if (!string.IsNullOrWhiteSpace(json))
                {
                    try
                    {
                        SaveGameData data = JsonUtility.FromJson<SaveGameData>(json);

                        if (json.Contains("\"BoardFlat\"") &&
                            SaveSystem.IsValidSave(data, SaveSlot.Journey))
                        {
                            ViewModel.LoadSaveData(data);
                            SaveSystem.Save(data, SaveSlot.Journey);
                            loadedFromCloud = true;

                            Debug.Log("[YouTube] Game restored from cloud save.");
                        }
                        else
                        {
                            Debug.LogWarning("[YouTube] Cloud save was invalid.");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[YouTube] Cloud save could not be parsed: {ex.Message}");
                    }
                }

                if (!loadedFromCloud)
                {
                    bool loadedLocally = TryLoadSave();

                    Debug.Log(loadedLocally ? "[YouTube] Using local fallback save." : "[YouTube] No previous save found. Starting normally.");
                }

                _initialCloudLoadComplete = true;

                onCompleted?.Invoke();
            }
        );

        #else
        TryLoadSave();
        onCompleted?.Invoke();
        #endif
    }
    private IEnumerator SaveLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);
            TrySave();
        }
    }
    public void TrySave()
    {
        SaveNow();
    }
    public void SaveNow()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR

        if (!_initialCloudLoadComplete)
        {
            return;
        }

        #endif
        if (ViewModel == null)
        {
            Debug.LogWarning("[User] SaveLoop fired but ViewModel is not assigned.");
            return;
        }
 
        SaveGameData data = ViewModel.GetSaveData();
        SaveSystem.Save(data, SaveSlot.Journey);

        if (NewGameViewModel != null && NewGameViewModel.HasActiveGame)
            SaveSystem.Save(NewGameViewModel.GetSaveData(), SaveSlot.NewGame);

        #if UNITY_WEBGL && !UNITY_EDITOR
        YouTubePlatformManager.Instance?.SaveCloudData(data);
        #endif        
    }

    public void SaveNewGameNow()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (!_initialCloudLoadComplete) return;
#endif
        if (NewGameViewModel == null || !NewGameViewModel.HasActiveGame) return;
        SaveSystem.Save(NewGameViewModel.GetSaveData(), SaveSlot.NewGame);
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) SaveNow();
    }

    private void OnApplicationQuit()
    {
        SaveNow();
    }
}
