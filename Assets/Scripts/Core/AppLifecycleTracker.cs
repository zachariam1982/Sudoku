using UnityEngine;

public class AppLifecycleTracker : MonoBehaviour
{
    private const string LastVersionKey = "LastAppVersion";
    private const string LastBuildKey = "LastBuildNumber"; // optional, for finer granularity

    void Awake()
    {
        string currentVersion = Application.version;
        string lastVersion = PlayerPrefs.GetString(LastVersionKey, "");

        if (string.IsNullOrEmpty(lastVersion))
        {
            OnFreshInstall(currentVersion);
        }
        else if (lastVersion != currentVersion)
        {
            OnAppUpdated(lastVersion, currentVersion);
        }
        else
        {
            // Normal launch, nothing changed
        }

        // Always update stored version after checking
        PlayerPrefs.SetString(LastVersionKey, currentVersion);
        PlayerPrefs.Save();
    }

    void OnFreshInstall(string version)
    {
        Debug.Log($"[Lifecycle] Fresh install detected. Version {version}");
    }

    void OnAppUpdated(string oldVersion, string newVersion)
    {
        Debug.Log($"[Lifecycle] App updated from {oldVersion} to {newVersion}");
        // Remove all the playerPrefs except PlayerID.
        PlayerPrefs.DeleteKey(PlayerSettings.TotalGamePlayed);
        PlayerPrefs.DeleteKey(PlayerSettings.TotalPoints);
        PlayerPrefs.DeleteKey(PlayerSettings.TotalWins);
        PlayerPrefs.DeleteKey(PlayerSettings.BestWinTime);
        PlayerPrefs.DeleteKey(PlayerSettings.CurrentStreak);
        PlayerPrefs.DeleteKey(PlayerSettings.TotalPossiblePoints);
    }
}