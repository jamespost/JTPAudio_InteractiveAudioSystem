using UnityEngine;

/// <summary>
/// A simple helper component that you add to any animated GameObject. Its only
/// purpose is to provide a public method that Unity's Animation Event system
/// can call. This method then forwards the request to your AudioManager.
/// </summary>
public class AnimationEventProxy : MonoBehaviour
{
    /// <summary>
    /// This is the public function that you will select from the dropdown menu
    /// when creating an Animation Event in the Timeline or Animation window.
    /// </summary>
    /// <param name="eventID">The unique string ID of the AudioEvent you want to play.</param>
    public void PlaySound(string eventID)
    {
        // Null reference check for the event ID to prevent errors.
        if (string.IsNullOrEmpty(eventID))
        {
            Debug.LogWarning("PlaySound animation event was called with an empty event ID.", this.gameObject);
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
