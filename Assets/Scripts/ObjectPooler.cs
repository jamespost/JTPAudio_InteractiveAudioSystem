// ObjectPooler.cs
// This script manages object pooling to optimize performance by reusing objects instead of instantiating and destroying them repeatedly.
//
// How to Use:
// 1. Attach this script to a GameObject in your scene.
// 2. In the Inspector, define the pools by specifying a unique tag, the prefab to pool, and the pool size.
// 3. Use the SpawnFromPool method to retrieve objects from the pool by tag, position, and rotation.
// 4. Ensure the prefab is properly set up to handle being activated and deactivated.
//
// Example:
// ObjectPooler.Instance.SpawnFromPool("Bullet", spawnPosition, spawnRotation);

using System.Collections.Generic;
using UnityEngine;

public class ObjectPooler : MonoBehaviour
{
    [System.Serializable]
    public class Pool
    {
        [Tooltip("Unique identifier for the pool.")]
        public string tag;

        [Tooltip("Prefab to be instantiated and pooled.")]
        public GameObject prefab;

        [Tooltip("Number of objects to pre-instantiate in the pool.")]
        public int size;
    }

    [Tooltip("List of pools to initialize.")]
    public List<Pool> pools;

    // Dictionary to store queues of pooled objects, categorized by their tags.
    public Dictionary<string, Queue<GameObject>> poolDictionary;

    void Awake()
    {
        // Initialize the pool dictionary.
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (Pool pool in pools)
        {
            // Create a new queue for each pool.
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.size; i++)
            {
                // Instantiate the prefab and deactivate it before adding to the queue.
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false);
                objectPool.Enqueue(obj);
            }

            // Add the queue to the dictionary with the corresponding tag.
            poolDictionary.Add(pool.tag, objectPool);
        }
    }

    /// <summary>
    /// Spawns an object from the pool with the specified tag.
    /// </summary>
    /// <param name="tag">The tag of the pool to spawn from.</param>
    /// <param name="position">The position to place the spawned object.</param>
    /// <param name="rotation">The rotation to apply to the spawned object.</param>
    /// <returns>The spawned GameObject, or null if the tag does not exist.</returns>
    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
            return null;
        }

        // Retrieve the next object from the pool.
        GameObject objectToSpawn = poolDictionary[tag].Dequeue();

        // Activate and position the object.
        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;

        // Re-enqueue the object to the pool for future use.
        poolDictionary[tag].Enqueue(objectToSpawn);

        return objectToSpawn;
    }
}
