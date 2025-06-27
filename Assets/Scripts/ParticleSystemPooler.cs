using UnityEngine;

/// <summary>
/// Handles returning particle systems to the object pool when they finish playing.
/// Attach this script to particle system prefabs used with the ObjectPooler.
/// 
/// Setup Instructions:
/// 1. Ensure the particle system prefab has this script attached.
/// 2. Set the "Stop Action" of the ParticleSystem component to "Callback" in the Unity Editor.
///    - This ensures the OnParticleSystemStopped method is triggered when the particle system finishes playing.
/// 3. Assign a unique tag to the particle system prefab that matches the tag defined in the ObjectPooler.
/// 4. Verify that the ObjectPooler is properly set up in the scene and includes a pool for the particle system's tag.
/// </summary>
public class ParticleSystemPooler : MonoBehaviour
{
    private string poolTag;
    private ObjectPooler objectPooler;

    private void Awake()
    {
        // Initialize the ObjectPooler reference
        objectPooler = FindObjectOfType<ObjectPooler>();
        if (objectPooler == null)
        {
            Debug.LogError("[ParticleSystemPooler] ObjectPooler not found in the scene. Ensure an ObjectPooler is set up.");
        }
    }

    private void OnEnable()
    {
        // Cache the pool tag if needed (e.g., set it when spawning).
        poolTag = gameObject.tag;

        // Optionally, ensure the particle system is reset.
        var particleSystem = GetComponent<ParticleSystem>();
        if (particleSystem != null)
        {
            particleSystem.Play();
        }
    }

    private void OnParticleSystemStopped()
    {
        if (objectPooler != null)
        {
            Debug.Log($"[ParticleSystemPooler] Returning particle system with tag '{poolTag}' to the pool.");
            // Return the particle system to the pool when it stops playing.
            objectPooler.ReturnToPool(poolTag, gameObject);
        }
        else
        {
            Debug.LogWarning("[ParticleSystemPooler] ObjectPooler is not initialized. Cannot return particle system to pool.");
        }
    }
}
