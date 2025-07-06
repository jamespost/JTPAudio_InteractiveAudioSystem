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

    private IEnumerator SpawnWave(WaveData waveData)
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

        foreach (var group in waveData.spawnGroups)
        {
            yield return new WaitForSeconds(group.delay);
            for (int i = 0; i < group.count; i++)
            {
                if (ObjectPooler.Instance != null)
                {
                    GameObject enemy = ObjectPooler.Instance.SpawnFromPool(group.poolTag, group.spawnPoint.position, group.spawnPoint.rotation);
                    if (enemy != null)
                    {
                        Health enemyHealth = enemy.GetComponent<Health>();
                        if (enemyHealth != null)
                        {
                            enemyHealth.OnDied += HandleEnemyDied;
                        }
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
}
