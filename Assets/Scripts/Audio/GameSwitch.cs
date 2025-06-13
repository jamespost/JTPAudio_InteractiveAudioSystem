using UnityEngine;

/// <summary>
/// Defines a Game Switch, which is a named state with a set of possible values.
/// Example: A "SurfaceType" switch could have values "Grass", "Gravel", "Wood".
/// </summary>
[CreateAssetMenu(menuName = "Audio/Game Switch")]
public class GameSwitch : ScriptableObject
{
    [Tooltip("The unique ID for this switch used by the AudioManager (e.g., 'SurfaceType').")]
    public string switchID;

    [Tooltip("The initial/default value for this switch when the game starts.")]
    public string defaultValue;
}
