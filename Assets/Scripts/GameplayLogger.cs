using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines the category of a log message for filtering and color-coding.
/// As a sound designer, you'll primarily be interested in the 'Audio' category.
/// </summary>
public enum LogCategory
{
    System,   // For core system messages (e.g., initialization)
    Audio,    // For all audio-related events
    Gameplay, // For general gameplay events (e.g., player actions)
    AI,       // For artificial intelligence behavior
    Error     // For critical errors or warnings
}

/// <summary>
/// A struct that holds all the data for a single log entry.
/// </summary>
public struct LogEntry
{
    public string Timestamp;
    public string Message;
    public LogCategory Category;
}

/// <summary>
/// A central, singleton manager for handling all gameplay logging. Other systems
/// send messages here, and this class broadcasts them to any UI or other listeners.
/// </summary>
public class GameplayLogger : MonoBehaviour
{
    // --- Singleton Pattern ---
    public static GameplayLogger Instance { get; private set; }

    // This is the core of the decoupled design. Any script can subscribe to this
    // event to be notified when a new log message is created.
    public static event Action<LogEntry> OnMessageLogged;

    private void Awake()
    {
        // Standard singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);

        // Log the initialization of the system itself.
        Log("Gameplay Logger Initialized.", LogCategory.System);
    }

    /// <summary>
    /// The public method that any other script will call to log a new message.
    /// </summary>
    /// <param name="message">The text of the log message.</param>
    /// <param name="category">The category of the message for sorting and display.</param>
    public static void Log(string message, LogCategory category)
    {
        // Null reference check for the message
        if (string.IsNullOrEmpty(message)) return;

        // Create the log entry with a formatted timestamp.
        LogEntry entry = new LogEntry
        {
            Timestamp = $"[{Time.time:F2}s]", // Formats time to two decimal places
            Message = message,
            Category = category
        };

        // Fire the event, notifying all subscribers (like our UI) that a new message is ready.
        // The?.Invoke() is a safe way to call the event, doing nothing if there are no subscribers.
        OnMessageLogged?.Invoke(entry);
    }
}
