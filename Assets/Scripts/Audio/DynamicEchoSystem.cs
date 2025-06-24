using UnityEngine;
using System.Collections.Generic;

public class DynamicEchoSystem : MonoBehaviour
{
    private AudioManager audioManager;
    private LayerMask reflectionLayerMask;

    [Header("Dynamic Echo System Tuning")]
    [Tooltip("Number of rays to cast per sound event (spread in all directions). Lower for less clutter, higher for more complex spaces.")]
    public int raysPerEvent = 12;
    [Tooltip("Maximum number of bounces (reflections) per ray.")]
    public int maxReflections = 2;
    [Tooltip("Maximum total echoes per sound event (hard cap).")]
    public int maxTotalEchoes = 24;
    [Tooltip("Maximum distance for a single ray.")]
    public float maxDistance = 50f;

    [Header("Attenuation & Filtering")]
    [Tooltip("Curve mapping normalized distance (0=close, 1=far) to low-pass filter cutoff frequency.")]
    public AnimationCurve distanceToCutoff = AnimationCurve.EaseInOut(0, 18000, 1, 800);
    [Tooltip("How quickly echoes lose energy with distance. Higher = more loss.")]
    public float energyLossPerMeter = 2.0f;

    [Tooltip("Minimum distance from the audio listener for a reflection to be allowed (in meters).")]
    public float minReflectionDistanceFromListener = 4.0f;

    public int debugEchoCount = 0;
    private int currentEchoCount = 0;

    public void Initialize(AudioManager manager, LayerMask layerMask)
    {
        audioManager = manager;
        reflectionLayerMask = layerMask;
    }

    public void GenerateEchoes(AudioEvent audioEvent, Vector3 sourcePosition)
    {
        Debug.Log($"[DynamicEchoSystem] GenerateEchoes called for event: {audioEvent.name}");
        debugEchoCount = 0;
        currentEchoCount = 0;
        for (int i = 0; i < raysPerEvent; i++)
        {
            if (currentEchoCount >= maxTotalEchoes) break;
            Vector3 direction = Random.onUnitSphere;
            CastReflectionRay(sourcePosition, direction, 0, 0, audioEvent);
        }
        Debug.Log($"[DynamicEchoSystem] Echoes generated: {debugEchoCount}");
    }

    private void CastReflectionRay(Vector3 origin, Vector3 direction, int reflectionCount, float totalDistance, AudioEvent audioEvent)
    {
        if (reflectionCount >= maxReflections || currentEchoCount >= maxTotalEchoes)
        {
            return;
        }

        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, maxDistance, reflectionLayerMask))
        {
            // Check distance from listener
            if (AudioManager.Instance != null && AudioManager.Instance.Listener != null)
            {
                float distToListener = Vector3.Distance(hit.point, AudioManager.Instance.Listener.transform.position);
                if (distToListener < minReflectionDistanceFromListener)
                {
                    // Skip this reflection if too close to listener
                    return;
                }
            }
            debugEchoCount++;
            currentEchoCount++;
            Debug.DrawLine(origin, hit.point, Color.yellow, 1.0f);
            float distance = hit.distance;
            float fullPath = totalDistance + distance;
            float delay = fullPath / 343f;
            float normDist = Mathf.Clamp01(fullPath / maxDistance);
            float cutoff = distanceToCutoff.Evaluate(normDist);
            float attenuation = 1.0f / (1.0f + fullPath * energyLossPerMeter);
            Debug.Log($"[DynamicEchoSystem] Echo at {hit.point} | Delay: {delay:F3}s | Attenuation: {attenuation:F3} | Cutoff: {cutoff:F0}");
            audioManager.PlayEchoWithFilter(audioEvent, hit.point, delay, attenuation, cutoff);
            Vector3 newDirection = Vector3.Reflect(direction, hit.normal);
            CastReflectionRay(hit.point, newDirection, reflectionCount + 1, fullPath, audioEvent);
        }
    }
}
