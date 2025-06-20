using UnityEngine;

/// <summary>
/// A simple helper component that you add to any animated GameObject. Its only
/// purpose is to provide a public method that Unity's Animation Event system
/// can call. This method then forwards the request to your AudioManager.
/// </summary>
public class AnimationEventProxy : MonoBehaviour
{
    /// <summary>
    /// This is the recommended, designer-friendly way to play a sound from an
    /// Animation Event. Drag the AudioEvent ScriptableObject directly into the
    /// event field in the Animation window.
    /// </summary>
    /// <param name="audioEvent">The AudioEvent asset to play.</param>
    public void PlaySound(AudioEvent audioEvent)
    {
        // Null reference check for the event itself.
        if (audioEvent == null)
        {
            Debug.LogWarning("PlaySound animation event was called with a null AudioEvent.", this.gameObject);
            return;
        }

        // Null reference check for the AudioManager instance.
        if (AudioManager.Instance != null)
        {
            // Post the event to the AudioManager, using the AudioEvent's ID
            // and this component's GameObject as the source of the sound.
            AudioManager.Instance.PostEvent(audioEvent.eventID, this.gameObject);
        }
        else
        {
            Debug.LogError("AudioManager instance not found! Cannot play sound from animation event.", this.gameObject);
        }
    }

    /// <summary>
    /// [Legacy] This method plays a sound based on its string ID. 
    /// It is kept for backward compatibility. For new work, please use the 
    /// PlaySound method that accepts an AudioEvent object directly.
    /// </summary>
    /// <param name="eventID">The unique string ID of the AudioEvent you want to play.</param>
    public void PlaySoundByID(string eventID)
    {
        // Null reference check for the event ID to prevent errors.
        if (string.IsNullOrEmpty(eventID))
        {
            Debug.LogWarning("PlaySoundByID animation event was called with an empty event ID.", this.gameObject);
            return;
        }

        // Null reference check for the AudioManager instance.
        if (AudioManager.Instance != null)
        {
            // Post the event to the AudioManager, using this component's GameObject
            // as the source of the sound for 3D positioning.
            AudioManager.Instance.PostEvent(eventID, this.gameObject);
        }
        else
        {
            Debug.LogError("AudioManager instance not found! Cannot play sound from animation event.", this.gameObject);
        }
    }
}
