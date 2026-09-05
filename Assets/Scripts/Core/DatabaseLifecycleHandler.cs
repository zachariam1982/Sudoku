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
        }
    }

    void OnApplicationQuit()
    {
        GameDatabase.Close();
    }
}
