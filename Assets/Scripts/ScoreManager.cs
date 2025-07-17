using UnityEngine;
using System;

/// <summary>
/// Example script showing how to integrate a new system with the Data-Driven UI.
/// This demonstrates how to create events that the UI system can listen to.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    [Header("Score Settings")]
    [Tooltip("Points awarded for killing an enemy.")]
    public int enemyKillPoints = 100;
    
    [Tooltip("Points awarded for completing a wave.")]
    public int waveCompletePoints = 500;

    // --- Static Events for UI System ---
    /// <summary>
    /// Event fired when the player's score changes.
    /// Parameter: newScore
    /// </summary>
    public static event Action<int> OnScoreChanged;
    
    /// <summary>
    /// Event fired when the player achieves a new high score.
    /// Parameters: newScore, previousHighScore
    /// </summary>
    public static event Action<int, int> OnHighScoreAchieved;

    // --- Private State ---
    private int currentScore = 0;
    private int highScore = 0;
    private static ScoreManager _instance;
    public static ScoreManager Instance => _instance;

    private void Awake()
    {
        // Singleton pattern
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        
        // Load high score from PlayerPrefs
        highScore = PlayerPrefs.GetInt("HighScore", 0);
    }

    private void OnEnable()
    {
        // Subscribe to game events to award points
        EventManager.OnEnemyDied += HandleEnemyDied;
        WaveManager.OnWaveChanged += HandleWaveComplete;
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        EventManager.OnEnemyDied -= HandleEnemyDied;
        WaveManager.OnWaveChanged -= HandleWaveComplete;
    }

    /// <summary>
    /// Add points to the player's score.
    /// </summary>
    /// <param name="points">Points to add.</param>
    public void AddScore(int points)
    {
        int previousScore = currentScore;
        currentScore += points;
        
        // Fire score changed event
        OnScoreChanged?.Invoke(currentScore);
        
        // Check for high score
        if (currentScore > highScore)
        {
            int previousHighScore = highScore;
            highScore = currentScore;
            
            // Save new high score
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();
            
            // Fire high score event
            OnHighScoreAchieved?.Invoke(currentScore, previousHighScore);
        }
        
        Debug.Log($"Score updated: {previousScore} → {currentScore} (+{points})");
    }

    /// <summary>
    /// Reset the score to zero.
    /// </summary>
    public void ResetScore()
    {
        currentScore = 0;
        OnScoreChanged?.Invoke(currentScore);
        Debug.Log("Score reset to 0");
    }

    /// <summary>
    /// Get the current score.
    /// </summary>
    public int GetCurrentScore() => currentScore;
    
    /// <summary>
    /// Get the high score.
    /// </summary>
    public int GetHighScore() => highScore;

    // Event handlers
    private void HandleEnemyDied(Vector3 position)
    {
        AddScore(enemyKillPoints);
    }

    private void HandleWaveComplete(int waveNumber)
    {
        // Award points for completing a wave (only when wave increases)
        if (waveNumber > 1) // Skip the first wave as it's just starting
        {
            AddScore(waveCompletePoints);
        }
    }

    // Static methods for easy access
    public static void AddPoints(int points)
    {
        if (Instance != null)
        {
            Instance.AddScore(points);
        }
    }

    public static void Reset()
    {
        if (Instance != null)
        {
            Instance.ResetScore();
        }
    }
}

/*
 * TO INTEGRATE THIS WITH THE DATA-DRIVEN UI SYSTEM:
 * 
 * 1. Create UI Elements for score display:
 *    - Element ID: "current_score"
 *    - Text Template: "Score: {0}"
 *    - Event Type: Custom
 *    - Custom Event Name: "ScoreChanged"
 * 
 * 2. Add to DataDrivenUIManager.SubscribeToEvents():
 *    ScoreManager.OnScoreChanged += HandleScoreChanged;
 * 
 * 3. Add to DataDrivenUIManager.UnsubscribeFromEvents():
 *    ScoreManager.OnScoreChanged -= HandleScoreChanged;
 * 
 * 4. Add handler method to DataDrivenUIManager:
 *    private void HandleScoreChanged(int newScore)
 *    {
 *        foreach (var kvp in _activeElements)
 *        {
 *            var element = kvp.Value;
 *            if (element.data.eventType == UIEventType.Custom && 
 *                element.data.customEventName == "ScoreChanged" && 
 *                element.data.autoUpdate)
 *            {
 *                UpdateElement(kvp.Key, newScore);
 *            }
 *        }
 *    }
 */
