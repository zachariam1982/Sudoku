using UnityEngine;
using UnityEngine.UI; // Required for ScrollRect
using TMPro;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

public class InGameConsole : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI logDisplay;
    [SerializeField] private ScrollRect scrollRect; // Drag your ScrollRect here

    [Header("Settings")]
    [SerializeField] private int maxLines = 5000;

    private readonly ConcurrentQueue<string> incomingLogQueue = new ConcurrentQueue<string>();
    private readonly Queue<string> logHistory = new Queue<string>();
    private readonly StringBuilder stringBuilder = new StringBuilder();

    void OnEnable()
    {
        Application.logMessageReceivedThreaded += HandleLogThreaded;
    }

    void OnDisable()
    {
        Application.logMessageReceivedThreaded -= HandleLogThreaded;
    }

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

        string formattedLog = $"<color={color}>[{type}] {logString}</color>";
        incomingLogQueue.Enqueue(formattedLog);
    }

    void Update()
    {
        if (incomingLogQueue.IsEmpty) return;

        bool hasChanged = false;

        while (incomingLogQueue.TryDequeue(out string newLog))
        {
            logHistory.Enqueue(newLog);
            hasChanged = true;

            while (logHistory.Count > maxLines)
            {
                logHistory.Dequeue();
            }
        }

        if (hasChanged)
        {
            stringBuilder.Clear();
            foreach (string logLine in logHistory)
            {
                stringBuilder.AppendLine(logLine);
            }
            logDisplay.text = stringBuilder.ToString();

            // Trigger the auto-scroll behavior
            ScrollToBottom();
        }
    }

    private void ScrollToBottom()
    {
        if (scrollRect == null) return;

        // Force Unity UI to recalculate the size of the content layout right now
        Canvas.ForceUpdateCanvases();

        // 0f represents the absolute bottom of the ScrollRect vertical space
        scrollRect.verticalNormalizedPosition = 0f;
    }
}
