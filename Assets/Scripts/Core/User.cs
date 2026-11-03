using System.Collections;
using UnityEngine;

public class User : MonoBehaviour
{
    public static User Instance { get; private set;}
    public JourneyViewModel ViewModel { get; set; }
    public NewGameViewModel NewGameViewModel { get; set; }
    public DailyChallengeViewModel DailyChallengeViewModel { get; set; }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
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
        NewGameResultDatabase.Init();
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
        if (ViewModel == null)
        {
            Debug.LogWarning("[User] SaveLoop fired but ViewModel is not assigned.");
            return;
        }
 
        SaveGameData data = ViewModel.GetSaveData();
        SaveSystem.Save(data, SaveSlot.Journey);

        if (NewGameViewModel != null && NewGameViewModel.HasActiveGame)
            SaveSystem.Save(NewGameViewModel.GetSaveData(), SaveSlot.NewGame);

        SaveDailyChallengeNow();

    }

    public void SaveNewGameNow()
    {
        if (NewGameViewModel == null || !NewGameViewModel.HasActiveGame) return;
        SaveSystem.Save(NewGameViewModel.GetSaveData(), SaveSlot.NewGame);
    }

    public void SaveDailyChallengeNow()
    {
        if (DailyChallengeViewModel == null || !DailyChallengeViewModel.HasActiveChallenge) return;
        SaveGameData data = DailyChallengeViewModel.GetSaveData();
        if (data != null)
            SaveSystem.Save(data, SaveSlot.DailyChallenge);
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
