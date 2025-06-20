using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Defines which audio property a GameParameter will modulate.
/// </summary>
public enum ModulationTarget
{
    Volume,
    Pitch
}

/// <summary>
/// A single rule that links a GameParameter to an audio property via a curve.
/// As a sound designer, this curve gives you visual control over the modulation.
/// </summary>
[System.Serializable]
public class ParameterModulation
{
    [Tooltip("The GameParameter that will drive this modulation (e.g., 'PlayerHealth').")]
    public GameParameter parameter;

    [Tooltip("The audio property of the sound that will be affected (e.g., Volume, Pitch).")]
    public ModulationTarget targetProperty;

    [Tooltip("The mapping curve. X-axis is the GameParameter's value (normalized from 0 to 1), Y-axis is the final multiplier applied to the target property.")]
    public AnimationCurve mappingCurve = AnimationCurve.Linear(0, 1, 1, 1);
}

[System.Serializable]
public class AudioSourceSettings
{
    [Tooltip("The volume of the audio source.")]
    public float volume = 1.0f;

    [Tooltip("The pitch of the audio source.")]
    public float pitch = 1.0f;

    [Tooltip("The spatial blend of the audio source (0 = 2D, 1 = 3D).")]
    public float spatialBlend = 1.0f;

    [Tooltip("Whether the audio source should loop.")]
    public bool loop = false;

    [Tooltip("The Doppler level of the audio source.")]
    public float dopplerLevel = 1.0f;

    [Tooltip("The spread angle (in degrees) of a 3D stereo or multichannel sound.")]
    public float spread = 0.0f;

    [Tooltip("The rolloff mode of the audio source.")]
    public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;

    [Tooltip("The minimum distance at which the audio source will be heard at full volume.")]
    public float minDistance = 1.0f;

    [Tooltip("The maximum distance at which the audio source will no longer be heard.")]
    public float maxDistance = 500.0f;

    [Tooltip("The priority of the audio source (0 = highest priority, 256 = lowest priority).")]
    public int priority = 128;
}

/// <summary>
/// A ScriptableObject that defines a complete sound event, from what clips to play
/// to how it behaves in the game world.
/// </summary>
[CreateAssetMenu(menuName = "Audio/Audio Event")]
public class AudioEvent : ScriptableObject
{
    public enum EventPriority
    {
        LeastImportantCull = 0,
        Low = 64,
        Standard = 128,
        Critical = 192,
        MostImportantVIP = 255
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

    [Tooltip("Sets the importance of this sound for playback prioritization. Lower values (e.g., 'LeastImportantCull') are for ambient or background sounds that can be culled if too many sounds are playing. Higher values (e.g., 'MostImportantVIP') are for critical sounds like UI alerts or player actions that should always play.")]
    public EventPriority priority = EventPriority.Standard;

    [Header("Parameter Modulation (RTPC)")]
    [Tooltip("A list of rules that define how this sound reacts to real-time GameParameter changes.")]
    public List<ParameterModulation> modulations = new List<ParameterModulation>();

    [Header("Audio Source Settings")]
    [Tooltip("Settings to apply to the audio source when this event is played.")]
    public AudioSourceSettings sourceSettings = new AudioSourceSettings();
}
