using UnityEngine;
using UnityEngine.UI;
using System;
using GAS;

/// <summary>
/// A universal health component for both players and enemies.
/// It is data-driven, referencing an EntityData ScriptableObject for max health.
/// </summary>
public class Health : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("The EntityData that defines the stats for this entity.")]
    public EntityData entityData;

    [Header("Entity Type")]
    [Tooltip("Is this health component attached to the player?")]
    public bool isPlayer = false;

    [Header("Debug Settings")]
    [Tooltip("Enable debug mode to display health as text above the entity.")]
    public bool debugMode = false;

    // --- Public Events ---
    public event Action<float, float> OnDamaged; // currentHealth, damageAmount
    public event Action OnDied;
    
    // --- Static Events for Player ---
    /// <summary>
    /// Static event that fires when the player's health changes.
    /// Parameters: currentHealth, maxHealth
    /// </summary>
    public static event Action<float, float> OnPlayerHealthChanged;

    // --- Private State ---
    private float currentHealth;
    private GameObject debugTextObject;
    private Text debugTextMesh;

    // GAS Integration
    private AbilitySystemComponent abilitySystemComponent;
    private GameplayAttribute healthAttribute;

    /// <summary>
    /// Initializes the health component.
    /// </summary>
    private void Awake()
    {
        abilitySystemComponent = GetComponent<AbilitySystemComponent>();

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
        // GAS Integration: Sync with Attribute
        if (abilitySystemComponent != null && abilitySystemComponent.AttributeSet != null)
        {
            healthAttribute = abilitySystemComponent.AttributeSet.GetAttribute("Health");
            if (healthAttribute != null)
            {
                // Initialize Attribute from EntityData
                healthAttribute.SetBaseValue(entityData.maxHealth);
                healthAttribute.CurrentValue = entityData.maxHealth;
                
                healthAttribute.OnAttributeChanged += HandleAttributeChanged;
            }
        }

        if (isPlayer)
        {
            currentHealth = entityData.maxHealth;
            OnPlayerHealthChanged?.Invoke(currentHealth, entityData.maxHealth);
        }

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

    private void HandleAttributeChanged(GameplayAttribute attr)
    {
        float previousHealth = currentHealth;
        currentHealth = attr.CurrentValue;
        
        // Calculate damage taken (if health decreased)
        if (currentHealth < previousHealth)
        {
            float damage = previousHealth - currentHealth;
            OnDamaged?.Invoke(currentHealth, damage);
        }
        
        if (isPlayer && entityData != null)
        {
            OnPlayerHealthChanged?.Invoke(currentHealth, entityData.maxHealth);
        }
        
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            OnDied?.Invoke();
        }
    }

    /// <summary>
    /// Reduces the entity's health and invokes the appropriate events.
    /// </summary>
    /// <param name="amount">The amount of damage to take.</param>
    public void TakeDamage(float amount)
    {
        Debug.Log($"TakeDamage called with amount: {amount}"); // Added debug log
        if (currentHealth <= 0 && amount > 0) return; // Already dead and taking damage

        if (healthAttribute != null)
        {
            // Modify attribute directly (GAS Integration)
            healthAttribute.CurrentValue -= amount;
            // HandleAttributeChanged will be called automatically
        }
        else
        {
            // Legacy Logic
            currentHealth -= amount;
            Debug.Log($"Current health after damage: {currentHealth}"); // Added debug log
            OnDamaged?.Invoke(currentHealth, amount);

            // Fire static player health event if this is the player
            if (isPlayer && entityData != null)
            {
                OnPlayerHealthChanged?.Invoke(currentHealth, entityData.maxHealth);
            }

            if (currentHealth <= 0)
            {
                currentHealth = 0;
                Debug.Log("OnDied event invoked"); // Added debug log
                OnDied?.Invoke();
                // Optionally, you can add logic here to disable the GameObject or trigger a death animation.
            }
        }
    }

    /// <summary>
    /// Heals the entity by a specified amount.
    /// </summary>
    /// <param name="amount">The amount to heal.</param>
    public void Heal(float amount)
    {
        if (healthAttribute != null)
        {
            healthAttribute.CurrentValue += amount;
            if (healthAttribute.CurrentValue > entityData.maxHealth)
            {
                healthAttribute.CurrentValue = entityData.maxHealth;
            }
        }
        else
        {
            currentHealth += amount;
            if (currentHealth > entityData.maxHealth)
            {
                currentHealth = entityData.maxHealth;
            }
            
            // Fire static player health event if this is the player
            if (isPlayer && entityData != null)
            {
                OnPlayerHealthChanged?.Invoke(currentHealth, entityData.maxHealth);
            }
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
            if (healthAttribute != null)
            {
                healthAttribute.CurrentValue = entityData.maxHealth;
            }
            else
            {
                currentHealth = entityData.maxHealth;
            }
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

// Note: This system could be improved by allowing the font and other settings to be assigned via the Inspector in the future.
