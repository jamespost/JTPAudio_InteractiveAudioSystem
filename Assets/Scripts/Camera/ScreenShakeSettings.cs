using UnityEngine;

[CreateAssetMenu(fileName = "NewScreenShakeSettings", menuName = "Camera/Screen Shake Settings")]
public class ScreenShakeSettings : ScriptableObject
{
    [Header("Timing")]
    [Tooltip("How long the shake lasts in seconds.")]
    public float duration = 0.5f;

    [Header("Position Shake")]
    [Tooltip("Maximum positional offset.")]
    public float positionStrength = 0.5f;
    [Tooltip("How fast the position shakes.")]
    public float positionFrequency = 10f;

    [Header("Rotation Shake")]
    [Tooltip("Maximum rotational offset in degrees.")]
    public float rotationStrength = 2.0f;
    [Tooltip("How fast the rotation shakes.")]
    public float rotationFrequency = 10f;

    [Header("Falloff")]
    [Tooltip("Curve defining how the shake strength fades over time (0 to 1).")]
    public AnimationCurve falloffCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0));

    [Header("Noise")]
    [Tooltip("If true, uses Perlin noise for smoother shake. If false, uses random noise.")]
    public bool usePerlinNoise = true;
    [Tooltip("Seed for Perlin noise scrolling.")]
    public float noiseScrollSpeed = 10f;
}
