// Assets/Scripts/Data/SpawnGroup.cs
using UnityEngine;

[System.Serializable]
public class SpawnGroup
{
    [Tooltip("The enemy prefab to spawn.")]
    public GameObject whatToSpawn;

    [Tooltip("The number of enemies to spawn.")]
    public int count;

    [Tooltip("The transform of the spawn point.")]
    public Transform spawnPoint;

    [Tooltip("The delay before this group starts spawning.")]
    public float delay;
}
