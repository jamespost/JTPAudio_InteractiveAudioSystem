using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// WaveManager handles the spawning and management of enemy waves in the game.
/// 
/// Key Features:
/// - NavMesh-based spawning: Automatically finds valid spawn positions on the NavMesh
/// - Visibility-based spawning: Ensures enemies spawn in locations visible to the player
/// - Anti-overlap system: Prevents enemies from spawning too close to each other
/// - Fallback system: Uses legacy spawn areas if NavMesh spawning fails
/// - Audio integration: Triggers wave start/complete audio events
/// - Designer-friendly: Configurable spawn distances, spacing, and preferences
/// 
/// How it works:
/// 1. For each wave, the system finds valid NavMesh positions within camera view
/// 2. Positions are checked for visibility and proper spacing from other enemies
/// 3. Enemies are spawned using the ObjectPooler system
/// 4. Wave progression waits for all enemies to be defeated before continuing
/// </summary>

public class WaveManager : MonoBehaviour
{
    public static event Action<int> OnWaveChanged;

    public LevelData currentLevelData;
    private int _currentWaveIndex = 0;
    private int _enemiesRemainingInWave;
    private List<Vector3> _currentWaveSpawnPositions = new List<Vector3>(); // Track spawn positions for current wave

    // NavMesh-based spawning configuration
    [Header("NavMesh Spawning Settings")]
    [SerializeField, Tooltip("Maximum distance from camera to search for spawn positions")]
    private float maxSpawnDistance = 50f;
    
    [SerializeField, Tooltip("Minimum distance from camera to spawn enemies")]
    private float minSpawnDistance = 10f;
    
    [SerializeField, Tooltip("Minimum distance between spawned enemies to prevent overlap")]
    private float enemySpacing = 2f;
    
    [SerializeField, Tooltip("Maximum attempts to find a valid spawn position")]
    private int maxSpawnAttempts = 50;
    
    [SerializeField, Tooltip("NavMesh area mask for valid spawn areas")]
    private int navMeshAreaMask = -1; // -1 means all areas
    
    [SerializeField, Tooltip("Prefer spawning behind cover or obstacles")]
    private bool preferCoverSpawning = true;
    
    [SerializeField, Tooltip("Search radius for finding NavMesh positions")]
    private float navMeshSearchRadius = 5f;

    // Add serialized fields for designer-friendly spawn areas
    [SerializeField]
    private List<Transform> spawnAreas; // Designer-defined spawn areas (kept for fallback)

    [Header("Debug Settings")]
    [SerializeField, Tooltip("Enable or disable debug messages for WaveManager.")]
    private bool debugMode = true;

    public void Initialize(LevelData levelData)
    {
        currentLevelData = levelData;
        _currentWaveIndex = 0;
    }

    public void StartWaves()
    {
        if (currentLevelData != null)
        {
            StartCoroutine(SpawnWaves());
        }
        else
        {
            Debug.LogError("No LevelData assigned to WaveManager.");
        }
    }

    public void StopWaves()
    {
        StopAllCoroutines();
    }

    private IEnumerator SpawnWaves()
    {
        while (_currentWaveIndex < currentLevelData.waves.Length)
        {
            LogDebug($"Starting wave {_currentWaveIndex + 1}, invoking OnWaveChanged event");
            OnWaveChanged?.Invoke(_currentWaveIndex + 1);
            WaveData currentWave = currentLevelData.waves[_currentWaveIndex];
            yield return StartCoroutine(SpawnWave(currentWave));
            
            LogDebug($"Wave spawning complete. Waiting for {_enemiesRemainingInWave} enemies to be defeated...");
            
            // Wait until all enemies in the wave are defeated
            while (_enemiesRemainingInWave > 0)
            {
                yield return null;
            }

            LogDebug("All enemies defeated! Wave complete.");

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PostEvent("WaveComplete", gameObject);
            }

            // Optional: Add a delay between waves
            yield return new WaitForSeconds(3f); 

            _currentWaveIndex++;
        }

        Debug.Log("All waves completed!");
        // Optionally, trigger a game over or victory condition here
    }

    public IEnumerator SpawnWave(WaveData waveData)
    {
        LogDebug("Starting Wave: " + (_currentWaveIndex + 1));
        _currentWaveSpawnPositions.Clear(); // Clear previous wave spawn positions
        
        // CRITICAL FIX: Reset enemy count for this wave
        _enemiesRemainingInWave = 0;

        if (AudioManager.Instance != null)
        {
            LogDebug("Posting WaveStart event to AudioManager.");
            AudioManager.Instance.PostEvent("WaveStart", gameObject);
        }

        foreach (var group in waveData.spawnGroups)
        {
            _enemiesRemainingInWave += group.count;
        }
        
        LogDebug($"Wave will spawn {_enemiesRemainingInWave} total enemies");

        // Add debug log to check the pool tag being used
        foreach (var group in waveData.spawnGroups)
        {
            yield return new WaitForSeconds(group.delay);
            for (int i = 0; i < group.count; i++)
            {
                Vector3 spawnPosition = GetNavMeshSpawnPosition();
                if (spawnPosition == Vector3.zero)
                {
                    Debug.LogWarning($"Could not find valid NavMesh spawn position for enemy {i + 1} in group with tag: {group.poolTag}");
                    continue; // Skip this enemy if no valid position found
                }
                
                // Add the spawn position to our tracking list
                _currentWaveSpawnPositions.Add(spawnPosition);

                LogDebug($"Attempting to spawn from pool with tag: {group.poolTag}");

                if (ObjectPooler.Instance != null)
                {
                    GameObject enemy = ObjectPooler.Instance.SpawnFromPool(group.poolTag, spawnPosition, Quaternion.identity);
                    if (enemy != null)
                    {
                        LogDebug($"Successfully spawned enemy from pool with tag: {group.poolTag}");
                        Health enemyHealth = enemy.GetComponent<Health>();
                        if (enemyHealth != null)
                        {
                            enemyHealth.OnDied += HandleEnemyDied;
                        }
                    }
                    else
                    {
                        LogDebug($"Failed to spawn object from pool with tag: {group.poolTag}");
                    }
                }
                else
                {
                    LogDebug("ObjectPooler instance not found.");
                }
                yield return new WaitForSeconds(0.5f); // Stagger spawns slightly
            }
        }
    }

    private void HandleEnemyDied()
    {
        _enemiesRemainingInWave--;
        LogDebug($"Enemy died. Enemies remaining in wave: {_enemiesRemainingInWave}");
        
        if (_enemiesRemainingInWave <= 0)
        {
            LogDebug("All enemies in wave defeated!");
        }
    }

    /// <summary>
    /// Finds a valid spawn position on the NavMesh that is visible to the player
    /// </summary>
    /// <returns>Valid spawn position or Vector3.zero if no valid position found</returns>
    private Vector3 GetNavMeshSpawnPosition()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main camera not found for NavMesh spawn positioning.");
            return Vector3.zero;
        }

        Vector3 cameraPosition = mainCamera.transform.position;
        
        LogDebug("Testing NavMesh spawn positions...");
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            // Generate a random direction and distance from camera
            Vector3 randomDirection = UnityEngine.Random.insideUnitSphere.normalized;
            float randomDistance = UnityEngine.Random.Range(minSpawnDistance, maxSpawnDistance);
            Vector3 targetPosition = cameraPosition + randomDirection * randomDistance;
            
            // Find the nearest point on the NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPosition, out hit, navMeshSearchRadius, navMeshAreaMask))
            {
                Vector3 navMeshPosition = hit.position;
                
                // Check if position is visible to the player
                if (IsPositionVisibleToPlayer(navMeshPosition))
                {
                    // Check if position is far enough from other enemies (using persistent wave tracking)
                    if (IsPositionValidForSpawning(navMeshPosition, _currentWaveSpawnPositions))
                    {
                        LogDebug($"Found valid NavMesh spawn position: {navMeshPosition}");
                        return navMeshPosition;
                    }
                }
            }
        }
        
        LogDebug($"Could not find valid NavMesh spawn position after {maxSpawnAttempts} attempts. Falling back to legacy spawn method.");
        return GetRandomSpawnPosition(); // Fallback to old method
    }
    
    /// <summary>
    /// Checks if a position is valid for spawning (not too close to other enemies)
    /// </summary>
    private bool IsPositionValidForSpawning(Vector3 position, List<Vector3> usedPositions)
    {
        foreach (Vector3 usedPosition in usedPositions)
        {
            if (Vector3.Distance(position, usedPosition) < enemySpacing)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Legacy spawn method - kept as fallback for when NavMesh spawning fails
    /// </summary>
    private Vector3 GetRandomSpawnPosition()
    {
        if (spawnAreas == null || spawnAreas.Count == 0)
        {
            Debug.LogError("No spawn areas defined in WaveManager.");
            return Vector3.zero;
        }

        // Select a random spawn area
        Transform selectedArea = spawnAreas[UnityEngine.Random.Range(0, spawnAreas.Count)];

        // Ensure the selected area is within the player's view
        Vector3 randomPosition = selectedArea.position + new Vector3(
            UnityEngine.Random.Range(-5f, 5f), // Adjust range as needed
            0,
            UnityEngine.Random.Range(-5f, 5f)
        );

        if (IsPositionVisibleToPlayer(randomPosition))
        {
            Debug.Log($"Random spawn position selected: {randomPosition}");
            return randomPosition;
        }
        else
        {
            Debug.LogWarning("Random position is not visible to the player. Retrying...");
            return GetRandomSpawnPosition(); // Retry if not visible
        }
    }

    private bool IsPositionVisibleToPlayer(Vector3 position)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main camera not found.");
            return false;
        }

        Vector3 viewportPoint = mainCamera.WorldToViewportPoint(position);
        bool isInViewport = viewportPoint.x >= 0 && viewportPoint.x <= 1 && viewportPoint.y >= 0 && viewportPoint.y <= 1 && viewportPoint.z > 0;
        
        if (!isInViewport)
        {
            return false;
        }
        
        // For enemy spawning, we want positions that are visible but not directly in front of the player
        // This creates more interesting gameplay where enemies appear from the sides or at distance
        Vector3 cameraPosition = mainCamera.transform.position;
        Vector3 cameraForward = mainCamera.transform.forward;
        Vector3 directionToSpawn = (position - cameraPosition).normalized;
        
        // Calculate the angle between camera forward and spawn direction
        float angle = Vector3.Angle(cameraForward, directionToSpawn);
        
        // Prefer spawning at the edges of the screen or behind some distance
        float distanceToSpawn = Vector3.Distance(cameraPosition, position);
        bool isAtGoodDistance = distanceToSpawn >= minSpawnDistance;
        
        // Optional: Check for obstacles if preferCoverSpawning is enabled
        if (preferCoverSpawning)
        {
            RaycastHit hit;
            if (Physics.Raycast(cameraPosition, directionToSpawn, out hit, distanceToSpawn))
            {
                // If there's partial cover, it's actually good for spawning
                if (hit.distance > distanceToSpawn * 0.7f) // At least 70% of the way there
                {
                    return isAtGoodDistance;
                }
            }
        }
        
        return isAtGoodDistance;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor-only method to visualize spawn areas and test spawn positions
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (Camera.main == null) return;
        
        Vector3 cameraPosition = Camera.main.transform.position;
        
        // Draw spawn distance rings
        UnityEditor.Handles.color = Color.green;
        UnityEditor.Handles.DrawWireDisc(cameraPosition, Vector3.up, minSpawnDistance);
        UnityEditor.Handles.color = Color.red;
        UnityEditor.Handles.DrawWireDisc(cameraPosition, Vector3.up, maxSpawnDistance);
        
        // Draw current wave spawn positions
        Gizmos.color = Color.yellow;
        foreach (Vector3 spawnPos in _currentWaveSpawnPositions)
        {
            Gizmos.DrawWireSphere(spawnPos, enemySpacing);
        }
    }
    
    /// <summary>
    /// Test method to find and visualize potential spawn positions
    /// </summary>
    [ContextMenu("Test NavMesh Spawn Positions")]
    public void TestNavMeshSpawnPositions()
    {
        Debug.Log("Testing NavMesh spawn positions...");
        for (int i = 0; i < 10; i++)
        {
            Vector3 testPos = GetNavMeshSpawnPosition();
            if (testPos != Vector3.zero)
            {
                Debug.Log($"Test spawn position {i + 1}: {testPos}");
            }
        }
    }
    #endif

    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log(message);
        }
    }

    private void OnEnable()
    {
        EventManager.OnPlayerReady += HandlePlayerReady;
    }

    private void OnDisable()
    {
        EventManager.OnPlayerReady -= HandlePlayerReady;
    }

    private void HandlePlayerReady()
    {
        if (currentLevelData != null)
        {
            StartWaves();
        }
    }
}
