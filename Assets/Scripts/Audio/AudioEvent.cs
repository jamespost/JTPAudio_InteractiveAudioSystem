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

/// <summary>
/// A ScriptableObject that defines a complete sound event, from what clips to play
/// to how it behaves in the game world.
/// </summary>
[CreateAssetMenu(menuName = "Audio/Audio Event")]
public class AudioEvent : ScriptableObject
{
    public enum EventPriority
    {
        Ambience = 0,
        Player = 64,
        Standard = 128,
        Critical = 192,
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

    [Tooltip("The gameplay importance of this sound.")]
    public EventPriority priority = EventPriority.Standard;

    [Header("Parameter Modulation (RTPC)")]
    [Tooltip("A list of rules that define how this sound reacts to real-time GameParameter changes.")]
    public List<ParameterModulation> modulations = new List<ParameterModulation>();
}
