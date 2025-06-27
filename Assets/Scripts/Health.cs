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
    private Text debugTextMesh;

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
            debugTextObject = new GameObject("HealthDebugCanvas");
            debugTextObject.transform.SetParent(transform);
            debugTextObject.transform.localPosition = new Vector3(0, 2, 0); // Position above the entity

            // Add a Canvas component for UI rendering
            Canvas canvas = debugTextObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            CanvasScaler canvasScaler = debugTextObject.AddComponent<CanvasScaler>();
            canvasScaler.dynamicPixelsPerUnit = 100f; // Adjust for crispness

            // Add a Text component for displaying health
            GameObject textObject = new GameObject("HealthText");
            textObject.transform.SetParent(debugTextObject.transform);
            Text text = textObject.AddComponent<Text>();
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.green; // Default color
            text.fontSize = 32;

            // Load the ShareTechMono-Regular font from the Fonts folder
            Font shareTechMonoFont = Resources.Load<Font>("Fonts/ShareTechMono-Regular");
            if (shareTechMonoFont != null)
            {
                text.font = shareTechMonoFont;
            }
            else
            {
                Debug.LogError("ShareTechMono-Regular font not found in Resources/Fonts folder.");
            }

            // Adjust RectTransform for proper scaling based on EntityData
            RectTransform rectTransform = text.GetComponent<RectTransform>();
            float sizeMultiplier = entityData != null ? entityData.healthDisplaySize : 1f; // Default to 1 if not set
            rectTransform.sizeDelta = new Vector2(200, 50); // Keep sizeDelta fixed
            rectTransform.localPosition = Vector3.zero;

            // Apply sizeMultiplier to the localScale of the debugTextObject
            debugTextObject.transform.localScale = Vector3.one * sizeMultiplier;

            debugTextMesh = text;
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

    /// <summary>
    /// Resets the entity's health to its maximum value.
    /// </summary>
    public void ResetHealth()
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

    private void Update()
    {
        if (debugMode && debugTextMesh != null)
        {
            // Update the health text to represent current health
            debugTextMesh.text = Mathf.Ceil(currentHealth).ToString();

            // Ensure the debug text canvas faces the camera
            if (Camera.main != null)
            {
                debugTextObject.transform.rotation = Quaternion.LookRotation(debugTextObject.transform.position - Camera.main.transform.position);
            }
        }
    }

    public float CurrentHealth
    {
        get { return currentHealth; }
    }
}
