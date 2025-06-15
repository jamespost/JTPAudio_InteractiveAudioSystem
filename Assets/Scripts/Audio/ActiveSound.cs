using UnityEngine;

/// <summary>
/// A small helper component attached to pooled AudioSource GameObjects at runtime.
/// It holds runtime data about the sound currently being played, like its priority,
/// so the AudioManager can make intelligent decisions about voice stealing.
/// </summary>
public class ActiveSound : MonoBehaviour
{
    // The AudioSource component on this same GameObject.
    public AudioSource source;

    // The priority of the AudioEvent currently being played.
    public AudioEvent.EventPriority priority;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
    }
}
