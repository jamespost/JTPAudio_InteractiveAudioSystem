using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class WaveManager : MonoBehaviour
{
    public static event Action<int> OnWaveChanged;

    public LevelData currentLevelData;
    private int _currentWaveIndex = 0;
    private int _enemiesRemainingInWave;

    // Add serialized fields for designer-friendly spawn areas
    [SerializeField]
    private List<Transform> spawnAreas; // Designer-defined spawn areas

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
            OnWaveChanged?.Invoke(_currentWaveIndex + 1);
            WaveData currentWave = currentLevelData.waves[_currentWaveIndex];
            yield return StartCoroutine(SpawnWave(currentWave));
            
            // Wait until all enemies in the wave are defeated
            while (_enemiesRemainingInWave > 0)
            {
                yield return null;
            }

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
        Debug.Log("Starting Wave: " + (_currentWaveIndex + 1));
        _enemiesRemainingInWave = 0;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PostEvent("WaveStart", gameObject);
        }

        foreach (var group in waveData.spawnGroups)
        {
            _enemiesRemainingInWave += group.count;
        }

        // Add debug log to check the pool tag being used
        foreach (var group in waveData.spawnGroups)
        {
            yield return new WaitForSeconds(group.delay);
            for (int i = 0; i < group.count; i++)
            {
                Vector3 spawnPosition;
                if (group.spawnPoint != null)
                {
                    spawnPosition = group.spawnPoint.position;
                }
                else
                {
                    spawnPosition = GetRandomSpawnPosition();
                }

                Debug.Log($"Attempting to spawn from pool with tag: {group.poolTag}");

                if (ObjectPooler.Instance != null)
                {
                    GameObject enemy = ObjectPooler.Instance.SpawnFromPool(group.poolTag, spawnPosition, Quaternion.identity);
                    if (enemy != null)
                    {
                        Health enemyHealth = enemy.GetComponent<Health>();
                        if (enemyHealth != null)
                        {
                            enemyHealth.OnDied += HandleEnemyDied;
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to spawn object from pool with tag: {group.poolTag}");
                    }
                }
                else
                {
                    Debug.LogError("ObjectPooler instance not found.");
                }
                yield return new WaitForSeconds(0.5f); // Stagger spawns slightly
            }
        }
    }

    private void HandleEnemyDied()
    {
        _enemiesRemainingInWave--;
    }

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
        return viewportPoint.x >= 0 && viewportPoint.x <= 1 && viewportPoint.y >= 0 && viewportPoint.y <= 1 && viewportPoint.z > 0;
    }
}
