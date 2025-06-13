using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Containers/Simple Container")]
public class SimpleContainer : BaseContainer
{
    public AudioClip clip;

    public override void Play(AudioSource source)
    {
        source.clip = clip;
        source.Play();
    }
}