using UnityEngine;

public class DatabaseLifecycleHandler : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void OnApplicationPause(bool isPaused)
    {
        if (isPaused)
        {
            GameDatabase.FlushToDisk();
            GameDatabase.Close(); 
        }
        else
        {
            GameDatabase.Init();
            // NewGameResultDatabase caches its own connection reference. The
            // game database is closed while suspended, so rebind the result
            // repository after GameDatabase has reopened the shared file.
            if (GameDatabase.Connection != null)
                NewGameResultDatabase.Init(GameDatabase.Connection);
        }
    }

    void OnApplicationQuit()
    {
        GameDatabase.Close();
    }
}