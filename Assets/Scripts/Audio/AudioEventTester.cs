using UnityEngine;

/// <summary>
/// A simple component to allow designers to test AudioEvents directly from the Inspector
/// on any GameObject in the scene. Requires the AudioEventTesterEditor script.
/// </summary>
public class AudioEventTester : MonoBehaviour
{
    [Tooltip("The AudioEvent to trigger when the test button is pressed.")]
    public AudioEvent eventToTest;
}
