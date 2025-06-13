using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A container that plays AudioClips in a specified order.
/// </summary>
[CreateAssetMenu(menuName = "Audio/Containers/Sequence Container")]
public class SequenceContainer : BaseContainer
{
    public enum SequenceMode
    {
        StepOnTrigger, // Plays the next clip in the sequence each time the event is posted.
        PlayAll        // Plays the entire sequence of clips on a single event post.
    }

    [Header("Clips")]
    [Tooltip("The list of AudioClips to play in order.")]
    public List<AudioClip> clips = new List<AudioClip>();

    [Header("Playback Settings")]
    [Tooltip("StepOnTrigger: Plays the next clip on each trigger.\nPlayAll: Plays the entire sequence on one trigger.")]
    public SequenceMode mode = SequenceMode.StepOnTrigger;
    [Tooltip("If true, the sequence will loop back to the beginning after finishing.")]
    public bool loop = true;

    // --- Private State ---
    private int currentIndex = 0;

    /// <summary>
    /// This method is called by Unity when the scriptable object is loaded.
    /// We use it to reset the sequence state when the game starts.
    /// </summary>
    private void OnEnable()
    {
        currentIndex = 0;
    }

    public override void Play(AudioSource source)
    {
        if (clips.Count == 0)
        {
            Debug.LogWarning("SequenceContainer has no clips to play.");
            return;
        }

        // Handle wrapping the index for looping or stopping
        if (currentIndex >= clips.Count)
        {
            if (loop)
            {
                currentIndex = 0;
            }
            else
            {
                // Sequence is finished and not looping, so do nothing.
                return;
            }
        }

        source.clip = clips[currentIndex];
        source.Play();

        // Increment index for the next call
        currentIndex++;
    }

    // Note: The 'PlayAll' mode is more complex and would require starting a coroutine from the AudioManager.
    // For this implementation, we focus on the core 'StepOnTrigger' functionality.
}
