// Assets/Scripts/Data/SpawnGroup.cs
using UnityEngine;
using UnityEditor;

[CreateAssetMenu(fileName = "NewSpawnGroup", menuName = "Data/SpawnGroup")]
public class SpawnGroup : ScriptableObject
{
    [Tooltip("The enemy prefab to spawn.")]
    public GameObject whatToSpawn;

    [Tooltip("The number of enemies to spawn.")]
    public int count;

    [Tooltip("The transform of the spawn point.")]
    public Transform spawnPoint;

    [Tooltip("The delay before this group starts spawning.")]
    public float delay;

    [Tooltip("The tag used to identify the object pool.")]
    public string poolTag;
}
