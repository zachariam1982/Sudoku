using UnityEngine;

public class DatabaseLifecycleHandler : MonoBehaviour
{
    void Awake()
    {
        // Ensure this hidden listener carries over between scenes
        DontDestroyOnLoad(gameObject);
    }

    // Fires when the player hits the home button, switches apps, or locks their screen
    void OnApplicationPause(bool isPaused)
    {
        if (isPaused)
        {
            // Safe point to flush data before Android targets the app for backup
            Debug.Log($"Game is paused. Flusing the DB.");
            GameDatabase.FlushToDisk();
            GameDatabase.Close(); 
        }
        else
        {
            // Re-open the database link automatically when the player returns to the game
            Debug.Log($"Game is unpaused. Re-open the connection to DB.");
            GameDatabase.Init();
        }
    }

    // Fires when the player manually swipes the game closed from the multitasking screen
    void OnApplicationQuit()
    {
        Debug.Log($"Application is being closed.");
        GameDatabase.Close();
    }
}
