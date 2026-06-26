using UnityEngine;
using TMPro;
using System.Collections.Concurrent;
using System.Text;

public class InGameConsole : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI logDisplay;
    [SerializeField] private int maxCharacters = 5000;

    // A thread-safe queue to collect logs from any thread
    private ConcurrentQueue<string> logQueue = new ConcurrentQueue<string>();
    private StringBuilder stringBuilder = new StringBuilder();

    void OnEnable()
    {
        // Listen to logs across all threads
        Application.logMessageReceivedThreaded += HandleLogThreaded;
    }

    void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        Application.logMessageReceivedThreaded -= HandleLogThreaded;
    }

    // This method runs on whatever thread threw the log
    private void HandleLogThreaded(string logString, string stackTrace, LogType type)
    {
        string color = "white";

        switch (type)
        {
            case LogType.Error:
            case LogType.Exception:
                color = "red";
                break;
            case LogType.Warning:
                color = "yellow";
                break;
        }

        // Format the message with rich text color codes
        string formattedLog = $"<color={color}>[{type}] {logString}</color>\n";
        
        // Push safely to the queue
        logQueue.Enqueue(formattedLog);
    }

    void Update()
    {
        // Only process if the queue has messages (Runs on the Main Thread)
        if (!logQueue.IsEmpty)
        {
            // Initialize the builder with current UI text
            stringBuilder.Clear();
            stringBuilder.Append(logDisplay.text);

            // Dequeue all pending items collected since the last frame
            while (logQueue.TryDequeue(out string newLog))
            {
                stringBuilder.Append(newLog);
            }

            // Truncate older entries if text size limits are breached
            if (stringBuilder.Length > maxCharacters)
            {
                int excessLength = stringBuilder.Length - maxCharacters;
                stringBuilder.Remove(0, excessLength);
            }

            // Push the final concatenated string to the UI
            logDisplay.text = stringBuilder.ToString();
        }
    }
}
