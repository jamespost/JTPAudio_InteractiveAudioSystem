using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Audio/Audio Event")]
public class AudioEvent : ScriptableObject
{
    public string eventID;
    public BaseContainer container;
    public AudioMixerGroup mixerGroup;
    // Add basic volume/pitch later
}