using UnityEngine;

/// <summary>
/// A simple component to allow designers to test AudioEvents directly from the Inspector
/// on any GameObject in the scene. Requires the AudioEventTesterEditor script.
/// </summary>
public class AudioEventTester : MonoBehaviour
{
    [Tooltip("The AudioEvent to trigger when the test button is pressed.")]
    public AudioEvent eventToTest;

    [Header("Repeating Test")]
    [Tooltip("Enable to repeatedly trigger the event at a set interval.")]
    public bool enableRepeat = false;

    [Tooltip("The time in seconds between each repeated event trigger.")]
    [Range(0.1f, 10f)]
    public float repeatInterval = 1.0f;

    // Internal state for the editor script
    [HideInInspector] public bool isRepeating = false;
    [HideInInspector] public double nextPlayTime = 0;
}
