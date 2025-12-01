using UnityEngine;

/// <summary>
/// A small helper component attached to pooled AudioSource GameObjects at runtime.
/// It holds runtime data about the sound currently being played, including its
/// final, calculated priority after considering any dynamic threat.
/// </summary>
public class ActiveSound : MonoBehaviour
{
    [Tooltip("The AudioSource component on this same GameObject.")]
    public AudioSource source;

    [Tooltip("The final priority of the sound, calculated by combining the event's base priority with any dynamic threat value.")]
    public int finalPriority;

    public string currentEventID;
    public GameObject sourceObject;

    private void Awake()
    {
        // Null reference check for the source component
        if (source == null)
        {
            source = GetComponent<AudioSource>();
        }
    }
}
