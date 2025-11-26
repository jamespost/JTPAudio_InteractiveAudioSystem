using UnityEngine;
using System.Collections.Generic;

public class ScreenShakeManager : MonoBehaviour
{
    public static ScreenShakeManager Instance { get; private set; }

    [Header("Target")]
    [Tooltip("The camera to shake. If null, will try to find Camera.main.")]
    public Transform cameraTransform;

    private Vector3 initialLocalPosition;
    private List<ActiveShake> activeShakes = new List<ActiveShake>();

    private class ActiveShake
    {
        public ScreenShakeSettings settings;
        public float timer;
        public float amplitudeMultiplier;
        public Vector2 noiseOffset;

        public ActiveShake(ScreenShakeSettings settings, float multiplier)
        {
            this.settings = settings;
            this.amplitudeMultiplier = multiplier;
            this.timer = 0f;
            this.noiseOffset = new Vector2(Random.Range(0f, 100f), Random.Range(0f, 100f));
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (cameraTransform != null)
        {
            initialLocalPosition = cameraTransform.localPosition;
        }
    }

    /// <summary>
    /// Triggers a screen shake with the given settings.
    /// </summary>
    /// <param name="settings">The shake settings asset.</param>
    /// <param name="multiplier">Optional multiplier for the shake strength (default 1).</param>
    public void Shake(ScreenShakeSettings settings, float multiplier = 1.0f)
    {
        if (settings == null) return;
        activeShakes.Add(new ActiveShake(settings, multiplier));
    }

    /// <summary>
    /// Triggers a simple procedural shake.
    /// </summary>
    public void Shake(float duration, float strength)
    {
        // Create a temporary settings object for simple shakes
        ScreenShakeSettings simpleSettings = ScriptableObject.CreateInstance<ScreenShakeSettings>();
        simpleSettings.duration = duration;
        simpleSettings.positionStrength = strength;
        simpleSettings.rotationStrength = strength * 2f; // Arbitrary ratio
        simpleSettings.falloffCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0));
        
        Shake(simpleSettings, 1.0f);
    }

    /// <summary>
    /// Triggers a screen shake based on distance from a point (e.g., explosion).
    /// </summary>
    /// <param name="point">The world position of the impact.</param>
    /// <param name="maxDistance">The maximum distance at which the shake is felt.</param>
    /// <param name="settings">The shake settings.</param>
    /// <param name="maxMultiplier">The multiplier at zero distance.</param>
    public void ShakeFromPoint(Vector3 point, float maxDistance, ScreenShakeSettings settings, float maxMultiplier = 1.0f)
    {
        if (cameraTransform == null) return;

        float distance = Vector3.Distance(cameraTransform.position, point);
        if (distance < maxDistance)
        {
            // Linear falloff
            float distanceFactor = 1.0f - (distance / maxDistance);
            Shake(settings, distanceFactor * maxMultiplier);
        }
    }

    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        Vector3 posOffset = Vector3.zero;
        Vector3 rotOffset = Vector3.zero;

        for (int i = activeShakes.Count - 1; i >= 0; i--)
        {
            ActiveShake shake = activeShakes[i];
            shake.timer += Time.deltaTime;

            if (shake.timer >= shake.settings.duration)
            {
                activeShakes.RemoveAt(i);
                continue;
            }

            float progress = shake.timer / shake.settings.duration;
            float strength = shake.settings.falloffCurve.Evaluate(progress) * shake.amplitudeMultiplier;

            if (shake.settings.usePerlinNoise)
            {
                float time = Time.time * shake.settings.noiseScrollSpeed;
                
                // Position Noise
                float x = (Mathf.PerlinNoise(shake.noiseOffset.x + time, 0) - 0.5f) * 2f;
                float y = (Mathf.PerlinNoise(0, shake.noiseOffset.y + time) - 0.5f) * 2f;
                
                posOffset += new Vector3(x, y, 0) * shake.settings.positionStrength * strength;

                // Rotation Noise
                float rotZ = (Mathf.PerlinNoise(shake.noiseOffset.x + time + 100f, shake.noiseOffset.y + time + 100f) - 0.5f) * 2f;
                rotOffset += new Vector3(0, 0, rotZ) * shake.settings.rotationStrength * strength;
            }
            else
            {
                // Random Noise
                Vector3 randomPos = Random.insideUnitSphere * shake.settings.positionStrength * strength;
                posOffset += randomPos;

                float randomRot = Random.Range(-1f, 1f) * shake.settings.rotationStrength * strength;
                rotOffset += new Vector3(0, 0, randomRot);
            }
        }

        // Apply offsets
        // We reset to initialLocalPosition to prevent drift, then add the offset
        cameraTransform.localPosition = initialLocalPosition + posOffset;
        
        // For rotation, we apply it on top of the current rotation (which is set by PlayerController)
        cameraTransform.localRotation *= Quaternion.Euler(rotOffset);
    }
}
