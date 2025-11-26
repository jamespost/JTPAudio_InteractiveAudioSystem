using UnityEngine;

[RequireComponent(typeof(Health))]
public class PlayerDamageShake : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("The screen shake settings to use when taking damage.")]
    public ScreenShakeSettings damageShakeSettings;

    [Tooltip("Multiplier for the shake intensity based on damage amount.")]
    public float damageToShakeMultiplier = 0.1f;

    [Tooltip("Minimum damage required to trigger a shake.")]
    public float minDamageThreshold = 1.0f;

    [Tooltip("Maximum shake multiplier clamp.")]
    public float maxShakeMultiplier = 2.0f;

    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.OnDamaged += HandleDamaged;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnDamaged -= HandleDamaged;
        }
    }

    private void HandleDamaged(float currentHealth, float damageAmount)
    {
        if (damageAmount < minDamageThreshold) return;

        if (ScreenShakeManager.Instance != null && damageShakeSettings != null)
        {
            // Calculate multiplier based on damage
            float multiplier = Mathf.Clamp(damageAmount * damageToShakeMultiplier, 0.1f, maxShakeMultiplier);
            
            ScreenShakeManager.Instance.Shake(damageShakeSettings, multiplier);
        }
    }
}
