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
            SqliteDatabaseConnection.Instance.Close();
        }
        else
        {
            SqliteDatabaseConnection.Instance.Open();
        }
    }

    void OnApplicationQuit()
    {
        SqliteDatabaseConnection.Instance.Close();
    }
}
