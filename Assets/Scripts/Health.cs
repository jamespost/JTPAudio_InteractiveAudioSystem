using UnityEngine;
using UnityEngine.UI;
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

    [Header("Debug Settings")]
    [Tooltip("Enable debug mode to display health as text above the entity.")]
    public bool debugMode = false;

    // --- Public Events ---
    public event Action<float> OnDamaged;
    public event Action OnDied;

    // --- Private State ---
    private float currentHealth;
    private GameObject debugTextObject;
    private TextMesh debugTextMesh;

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

    private void Start()
    {
        if (debugMode)
        {
            // Create a new GameObject for the debug text
            debugTextObject = new GameObject("HealthDebugText");
            debugTextObject.transform.SetParent(transform);
            debugTextObject.transform.localPosition = new Vector3(0, 2, 0); // Position above the entity

            // Add a TextMesh component for displaying text
            debugTextMesh = debugTextObject.AddComponent<TextMesh>();
            debugTextMesh.fontSize = 32;
            debugTextMesh.alignment = TextAlignment.Center;
            debugTextMesh.anchor = TextAnchor.MiddleCenter;
            debugTextMesh.color = Color.green; // Default color
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

    private void Update()
    {
        if (debugMode && debugTextMesh != null)
        {
            // Update the text to display current health
            debugTextMesh.text = $"Health: {currentHealth}/{entityData.maxHealth}";

            // Change color based on health percentage
            float healthPercentage = currentHealth / entityData.maxHealth;
            if (healthPercentage > 0.5f)
            {
                debugTextMesh.color = Color.green;
            }
            else if (healthPercentage > 0.2f)
            {
                debugTextMesh.color = Color.yellow;
            }
            else
            {
                debugTextMesh.color = Color.red;
            }

            // Ensure the text faces the camera
            if (Camera.main != null)
            {
                debugTextObject.transform.rotation = Quaternion.LookRotation(debugTextObject.transform.position - Camera.main.transform.position);
            }
        }
    }
}
