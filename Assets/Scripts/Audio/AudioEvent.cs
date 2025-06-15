using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// A ScriptableObject that defines a complete sound event, from what clips to play
/// to how it behaves in the game world.
/// </summary>
[CreateAssetMenu(menuName = "Audio/Audio Event")]
public class AudioEvent : ScriptableObject
{
    // Enum for defining the gameplay importance of an event.
    public enum EventPriority
    {
        // For background loops and non-critical sounds that can be culled first.
        Ambience = 0,
        // For the player's own Foley sounds (footsteps, jumps).
        Player = 64,
        // The default for most world interactions and general UI.
        Standard = 128,
        // For important gameplay cues like enemy ability "tells".
        Critical = 192,
        // For sounds that absolutely must be heard (dialogue, immediate lethal threats).
        ImmediateThreat = 255
    }

    [Tooltip("The unique string ID used to call this event from code.")]
    public string eventID;

    [Tooltip("The container holding the AudioClip(s) and playback logic.")]
    public BaseContainer container;

    [Tooltip("The AudioMixerGroup to route this sound to.")]
    public AudioMixerGroup mixerGroup;

    [Header("Behavior Settings")]
    [Tooltip("If true, the sound will follow the source GameObject.")]
    public bool attachToSource = false;

    [Tooltip("The gameplay importance of this sound. Higher values will cut off lower values if the system runs out of voices.")]
    public EventPriority priority = EventPriority.Standard;
}
