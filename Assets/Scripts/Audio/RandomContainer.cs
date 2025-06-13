using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A container that plays a randomly selected AudioClip from a list,
/// with options for volume/pitch randomization and preventing immediate repeats.
/// Volume is handled in Decibels (dB) and Pitch in Semitones for intuitive sound design.
/// </summary>
[CreateAssetMenu(menuName = "Audio/Containers/Random Container")]
public class RandomContainer : BaseContainer
{
    [Header("Clips")]
    [Tooltip("The list of AudioClips to choose from.")]
    public List<AudioClip> clips = new List<AudioClip>();

    [Header("Playback Settings")]
    [Tooltip("If true, the container will not play the same clip twice in a row.")]
    public bool avoidRepeat = true;

    [Header("Sound Designer Controls")]
    [Tooltip("The minimum volume adjustment in Decibels (dB). -80 is silent, 0 is original volume.")]
    [Range(-80f, 6f)] public float minVolumeDB = 0f;
    [Tooltip("The maximum volume adjustment in Decibels (dB). -80 is silent, 0 is original volume.")]
    [Range(-80f, 6f)] public float maxVolumeDB = 0f;

    [Tooltip("The minimum pitch shift in Semitones. -12 is one octave down, +12 is one octave up.")]
    [Range(-36f, 36f)] public float minPitchSemitones = 0f;
    [Tooltip("The maximum pitch shift in Semitones. -12 is one octave down, +12 is one octave up.")]
    [Range(-36f, 36f)] public float maxPitchSemitones = 0f;

    // --- Private State ---
    private int lastPlayedIndex = -1;

    public override void Play(AudioSource source)
    {
        if (clips.Count == 0)
        {
            Debug.LogWarning("RandomContainer has no clips to play.");
            return;
        }

        // --- Select a clip ---
        int index;
        if (avoidRepeat && clips.Count > 1)
        {
            do
            {
                index = Random.Range(0, clips.Count);
            } while (index == lastPlayedIndex);
        }
        else
        {
            index = Random.Range(0, clips.Count);
        }

        lastPlayedIndex = index;
        AudioClip clipToPlay = clips[index];
        source.clip = clipToPlay;

        // --- Apply Randomization with Pro Audio Scaling ---

        // 1. Volume (Decibels to Linear Amplitude)
        // Formula: amplitude = 10^(dB / 20)
        float randomDB = Random.Range(minVolumeDB, maxVolumeDB);
        source.volume = Mathf.Pow(10f, randomDB / 20f);

        // 2. Pitch (Semitones to Pitch Multiplier)
        // Formula: pitch = 2^(semitones / 12)
        float randomSemitones = Random.Range(minPitchSemitones, maxPitchSemitones);
        source.pitch = Mathf.Pow(2f, randomSemitones / 12f);

        source.Play();
    }

    /// <summary>
    /// This method is called by Unity in the editor whenever a value is changed.
    /// We use it to ensure the min/max values are logical.
    /// </summary>
    private void OnValidate()
    {
        if (minVolumeDB > maxVolumeDB)
        {
            minVolumeDB = maxVolumeDB;
        }

        if (minPitchSemitones > maxPitchSemitones)
        {
            minPitchSemitones = maxPitchSemitones;
        }
    }
}
