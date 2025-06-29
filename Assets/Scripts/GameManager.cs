// GameManager.cs
// Summary: This script manages the overall game state, including transitions between MAIN_MENU, PLAYING, and GAME_OVER states. It is responsible for resetting the player, handling player death, and triggering game state changes. This version integrates wave-based gameplay management using WaveManager and supports designer-editable waves via LevelData assets.
//
// TODO: Ensure all enemy spawning is handled through the ObjectPooler for performance optimization.

using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
     private Health playerHealth;

    [Tooltip("Reference to the player's transform for resetting position.")]
    [SerializeField] private Transform playerTransform;

    [Tooltip("Initial position to reset the player to.")]
    [SerializeField] private Vector3 initialPlayerPosition;

    [Tooltip("Reference to the WaveManager for managing waves.")]
    [SerializeField] private WaveManager waveManager;

    [Tooltip("Level data containing wave configurations.")]
    [SerializeField] private LevelData levelData;
    
    public enum GameState { MAIN_MENU, PLAYING, GAME_OVER }
    public GameState CurrentState { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Automatically find the player's transform if not assigned
        if (playerTransform == null)
        {
            PlayerController playerController = FindObjectOfType<PlayerController>();
            if (playerController != null)
            {
                playerTransform = playerController.transform;
                playerHealth = playerController.GetComponent<Health>();
            }
            else
            {
                Debug.LogWarning("GameManager: PlayerController not found in the scene. Ensure a PlayerController is present.");
            }
        }
    }

    private void Start()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDied += HandlePlayerDeath;
        }

        if (waveManager != null && levelData != null)
        {
            waveManager.Initialize(levelData);
        }

        SetGameState(GameState.MAIN_MENU);
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDied -= HandlePlayerDeath;
        }
    }

    public void SetGameState(GameState newState)
    {
        CurrentState = newState;
        EventManager.TriggerGameStateChanged(newState);

        if (newState == GameState.PLAYING)
        {
            ResetPlayer();
            if (waveManager != null)
            {
                waveManager.StartWaves();
            }
        }
        else if (newState == GameState.GAME_OVER)
        {
            if (waveManager != null)
            {
                waveManager.StopWaves();
            }
        }
    }

    private void HandlePlayerDeath()
    {
        SetGameState(GameState.GAME_OVER);

        // Disable player movement and interactions
        PlayerController playerController = playerTransform.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = false;
        }
    }

    private void ResetPlayer()
    {
        if (playerTransform != null)
        {
            playerTransform.position = initialPlayerPosition;
        }

        if (playerHealth != null)
        {
            playerHealth.ResetHealth();
        }
    }
}
