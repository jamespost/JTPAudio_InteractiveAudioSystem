using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A container that holds a dictionary of other containers, and plays one
/// based on the current value of a specified GameSwitch.
/// </summary>
[CreateAssetMenu(menuName = "Audio/Containers/Switch Container")]
public class SwitchContainer : BaseContainer
{
    [System.Serializable]
    public class SwitchMapping
    {
        public string switchValue; // e.g., "Grass", "Wood", "Metal"
        public BaseContainer container;
    }

    [Header("Switch Logic")]
    [Tooltip("The GameSwitch asset that this container listens to.")]
    public GameSwitch gameSwitch;

    [Tooltip("The list of containers to play based on the switch value.")]
    public List<SwitchMapping> mappings = new List<SwitchMapping>();

    [Tooltip("A default container to play if the current switch value doesn't match any mapping.")]
    public BaseContainer defaultContainer;

    public override void Play(AudioSource source)
    {
        // Get the current value of the switch from the AudioManager
        string currentSwitchValue = AudioManager.Instance.GetSwitchValue(gameSwitch.switchID);

        // Find the matching container
        foreach (var mapping in mappings)
        {
            if (mapping.switchValue == currentSwitchValue)
            {
                mapping.container.Play(source);
                return;
            }
        }

        // If no match was found, play the default
        if (defaultContainer != null)
        {
            defaultContainer.Play(source);
        }
        else
        {
            Debug.LogWarning($"SwitchContainer: No mapping found for switch '{gameSwitch.switchID}' with value '{currentSwitchValue}', and no default container is set.");
        }
    }
}
