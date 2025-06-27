using UnityEngine;
using System;

/// <summary>
/// A universal health component for both players and enemies.
/// It is data-driven, referencing an EntityData ScriptableObject for max health.
/// </summary>
public class Health : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("The EntityData that defines the stats for this entity.")]
    public EntityData entityData;

    // --- Public Events ---
    public event Action<float> OnDamaged;
    public event Action OnDied;

    // --- Private State ---
    private float currentHealth;

    /// <summary>
    /// Initializes the health component.
    /// </summary>
    private void Awake()
    {
        if (entityData != null)
        {
            currentHealth = entityData.maxHealth;
        }
        else
        {
            Debug.LogError("EntityData is not assigned on " + gameObject.name, this);
        }
    }

    /// <summary>
    /// Reduces the entity's health and invokes the appropriate events.
    /// </summary>
    /// <param name="amount">The amount of damage to take.</param>
    public void TakeDamage(float amount)
    {
        if (currentHealth <= 0) return; // Already dead

        currentHealth -= amount;
        OnDamaged?.Invoke(currentHealth);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            OnDied?.Invoke();
            // Optionally, you can add logic here to disable the GameObject or trigger a death animation.
        }
    }

    /// <summary>
    /// Heals the entity by a specified amount.
    /// </summary>
    /// <param name="amount">The amount to heal.</param>
    public void Heal(float amount)
    {
        currentHealth += amount;
        if (currentHealth > entityData.maxHealth)
        {
            currentHealth = entityData.maxHealth;
        }
    }

    /// <summary>
    /// Gets the current health of the entity.
    /// </summary>
    /// <returns>The current health.</returns>
    public float GetCurrentHealth()
    {
        return currentHealth;
    }
}
