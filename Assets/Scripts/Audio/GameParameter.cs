using UnityEngine;

/// <summary>
/// Defines a continuous game parameter that can be used to drive real-time changes
/// in audio properties (like volume and pitch) or AudioMixer effects.
/// This is the custom equivalent of an RTPC in audio middleware.
/// </summary>
[CreateAssetMenu(menuName = "Audio/Game Parameter")]
public class GameParameter : ScriptableObject
{
    [Tooltip("The unique ID used to identify this parameter in code (e.g., 'PlayerHealth', 'VehicleSpeed').")]
    public string parameterID;

    [Tooltip("The minimum value this parameter can have.")]
    public float minValue = 0f;

    [Tooltip("The maximum value this parameter can have.")]
    public float maxValue = 1f;

    [Tooltip("The value this parameter will be initialized with when the game starts.")]
    public float defaultValue = 0f;

    [Header("Mixer Integration (Optional)")]
    [Tooltip("The exact name of a parameter exposed on the main AudioMixer. If set, changing this GameParameter will also change the mixer parameter.")]
    public string exposedMixerParameter;
}

