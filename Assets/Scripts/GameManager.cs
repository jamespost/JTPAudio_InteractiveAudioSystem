// GameManager.cs
// Summary: This script manages the overall game state, including transitions between MAIN_MENU, PLAYING, and GAME_OVER states. It is responsible for resetting the player, handling player death, and triggering game state changes. This version integrates wave-based gameplay management using WaveManager and supports designer-editable waves via LevelData assets.
//
// TODO: Ensure all enemy spawning is handled through the ObjectPooler for performance optimization.

using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Add this for UI components

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

    private GameObject gameStateTextObject;
    private Text gameStateText;
    
    public enum GameState { MAIN_MENU, LEVEL_LOADING, IN_GAME, PAUSED, GAME_OVER }
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

        // Create a GameObject for displaying the game state
        gameStateTextObject = new GameObject("GameStateText");
        gameStateTextObject.transform.SetParent(null); // Make it a root object
        DontDestroyOnLoad(gameStateTextObject);

        // Add a Canvas component for UI rendering
        Canvas canvas2 = gameStateTextObject.AddComponent<Canvas>();
        canvas2.renderMode = RenderMode.ScreenSpaceOverlay;

        // Add a Text component for displaying the game state
        gameStateText = gameStateTextObject.AddComponent<Text>();
        gameStateText.alignment = TextAnchor.MiddleLeft;
        gameStateText.color = Color.white;
        gameStateText.fontSize = 32;

        // Load the ShareTechMono-Regular font from the Resources folder
        Font shareTechMonoFont = Resources.Load<Font>("Fonts/ShareTechMono-Regular");
        if (shareTechMonoFont != null)
        {
            gameStateText.font = shareTechMonoFont;
        }
        else
        {
            Debug.LogError("ShareTechMono-Regular font not found in Resources/Fonts folder.");
        }

        // Adjust RectTransform for proper positioning
        RectTransform rectTransform2 = gameStateText.GetComponent<RectTransform>();
        rectTransform2.sizeDelta = new Vector2(400, 50);
        rectTransform2.anchoredPosition = new Vector2(-Screen.width / 2 + 200, 0); // Center-left of the screen

        SetGameState(GameState.MAIN_MENU);
    }

    private void OnDestroy()
    {
        UnsubscribeFromPlayerEvents();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnsubscribeFromPlayerEvents();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UnsubscribeFromPlayerEvents(); // Ensure no stale subscriptions

        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            playerTransform = playerController.transform;
            playerHealth = playerController.GetComponent<Health>();

            SubscribeToPlayerEvents();
        }
    }

    private void SubscribeToPlayerEvents()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDied -= HandlePlayerDeath; // Unsubscribe first to prevent duplicates
            playerHealth.OnDied += HandlePlayerDeath;
        }
    }

    private void UnsubscribeFromPlayerEvents()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDied -= HandlePlayerDeath;
        }
    }

    public void SetGameState(GameState newState)
    {
        Debug.Log($"SetGameState called. Changing state from {CurrentState} to {newState}");
        CurrentState = newState;
        Debug.Log($"GameState is now {CurrentState}"); // Added debug log
        EventManager.TriggerGameStateChanged(newState);

        switch (newState)
        {
            case GameState.MAIN_MENU:
                Debug.Log("GameState is now MAIN_MENU");
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                SceneManager.LoadScene("MainMenu"); // Ensure this is the correct scene name
                break;

            case GameState.LEVEL_LOADING:
                Debug.Log("GameState is now LEVEL_LOADING");
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                SceneManager.sceneLoaded += OnGameSceneLoaded;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name); // Reload current level
                break;

            case GameState.IN_GAME:
                Debug.Log("GameState is now IN_GAME");
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                if (waveManager != null)
                {
                    waveManager.StartWaves();
                }
                break;

            case GameState.PAUSED:
                Debug.Log("GameState is now PAUSED");
                Time.timeScale = 0f; // Freeze the game
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;

            case GameState.GAME_OVER:
                Debug.Log("GameState is now GAME_OVER");
                if (waveManager != null)
                {
                    waveManager.StopWaves();
                }
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
        }

        if (newState != GameState.PAUSED && Time.timeScale == 0f)
        {
            Time.timeScale = 1f; // Resume the game if coming out of pause
        }

        // Update the game state text
        if (gameStateText != null)
        {
            gameStateText.text = "Game State: " + newState.ToString();
            Debug.Log($"GameStateText updated to: {gameStateText.text}");
        }
        else
        {
            Debug.LogWarning("GameStateText is null. Unable to update text.");
        }
    }

    public void StartGame()
    {
        Debug.Log("StartGame method called!");
        SceneManager.sceneLoaded += OnGameSceneLoaded;
        SetGameState(GameState.LEVEL_LOADING);
        SceneManager.LoadScene("Game"); // Replace "Game" with your actual game scene name
    }

    private void OnGameSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Game") // Replace "Game" with your actual game scene name
        {
            Debug.Log("Game scene loaded.");
            ResetPlayer();
            SetGameState(GameState.IN_GAME);

            // Re-find the Text component in the persistent gameStateTextObject
            if (gameStateTextObject != null)
            {
                gameStateText = gameStateTextObject.GetComponent<Text>();
            }
        }
        else if (scene.name != "MainMenu") // Handle any other game scenes that might be loaded
        {
            Debug.Log($"Scene {scene.name} loaded.");
            ResetPlayer();
            SetGameState(GameState.IN_GAME);

            // Re-find the Text component in the persistent gameStateTextObject
            if (gameStateTextObject != null)
            {
                gameStateText = gameStateTextObject.GetComponent<Text>();
            }
        }
        SceneManager.sceneLoaded -= OnGameSceneLoaded; // Unsubscribe to avoid duplicate calls
    }

    public void ResumeGame()
    {
        Debug.Log("Resuming game...");
        SetGameState(GameState.IN_GAME);
        Time.timeScale = 1f; // Unpause the game
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false; // Hide the cursor during gameplay
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
        // Re-find the player after scene reload if needed
        if (playerTransform == null)
        {
            PlayerController playerController = FindObjectOfType<PlayerController>();
            if (playerController != null)
            {
                playerTransform = playerController.transform;
                playerHealth = playerController.GetComponent<Health>();
                
                // Re-subscribe to player death event
                if (playerHealth != null)
                {
                    playerHealth.OnDied += HandlePlayerDeath;
                }
            }
        }

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
