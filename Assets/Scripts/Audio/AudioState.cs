using UnityEngine;
using UnityEngine.Audio; // Required for AudioMixerSnapshot

/// <summary>
/// A ScriptableObject that defines a global audio state for the game.
/// This is used to trigger broad changes in the mix, primarily by transitioning
/// to different AudioMixer Snapshots.
/// </summary>
[CreateAssetMenu(menuName = "Audio/Audio State")]
public class AudioState : ScriptableObject
{
    [Tooltip("The unique string ID used to identify this state in code (e.g., 'Combat', 'MainMenu', 'Paused').")]
    public string stateID;

    [Tooltip("The AudioMixer Snapshot to transition to when this state becomes active.")]
    public AudioMixerSnapshot snapshot;

    [Tooltip("The time in seconds it should take to fade from the previous snapshot to this one.")]
    [Range(0f, 10f)]
    public float transitionTime = 0.5f;
}
