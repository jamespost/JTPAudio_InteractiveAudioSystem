using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// A ScriptableObject that defines a complete sound event, from what clips to play
/// to how it behaves in the game world.
/// </summary>
[CreateAssetMenu(menuName = "Audio/Audio Event")]
public class AudioEvent : ScriptableObject
{
    [Tooltip("The unique string ID used to call this event from code.")]
    public string eventID;

    [Tooltip("The container holding the AudioClip(s) and playback logic.")]
    public BaseContainer container;

    [Tooltip("The AudioMixerGroup to route this sound to.")]
    public AudioMixerGroup mixerGroup;

    [Header("3D Sound Settings")]
    [Tooltip("If true, the sound will follow the source GameObject. If false, it will play at the location where it was triggered and remain there.")]
    public bool attachToSource = false;
}
