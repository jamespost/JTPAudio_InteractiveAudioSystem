using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using System;

/// <summary>
/// Data-driven UI Manager that creates and manages UI elements based on ScriptableObject configurations.
/// This system allows designers to create and modify UI layouts without touching code or hierarchy.
/// 
/// Key Features:
/// - Completely data-driven through ScriptableObject assets
/// - Automatic event subscription based on UI element configuration
/// - Dynamic color changes and animations
/// - Resolution-independent positioning and scaling
/// - Runtime UI element creation and management
/// </summary>
public class DataDrivenUIManager : MonoBehaviour
{
    private static DataDrivenUIManager _instance;
    public static DataDrivenUIManager Instance => _instance;

    [Header("Configuration")]
    [Tooltip("The HUD layout to use for this scene.")]
    public HUDLayoutData currentLayout;
    
    [Tooltip("Canvas to create UI elements on. If null, will find one automatically.")]
    public Canvas targetCanvas;
    
    [Tooltip("Default font to use when UI elements don't specify one.")]
    public TMP_FontAsset defaultFont;

    [Header("Debug")]
    [Tooltip("Enable debug logging for UI operations.")]
    public bool debugMode = true;

    // Runtime data
    private Dictionary<string, UIElementInstance> _activeElements = new Dictionary<string, UIElementInstance>();
    private CanvasScaler _canvasScaler;
    private float _currentScaleFactor = 1f;

    private void Awake()
    {
        // Singleton pattern
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        // Find canvas if not assigned
        if (targetCanvas == null)
        {
            targetCanvas = FindObjectOfType<Canvas>();
            if (targetCanvas == null)
            {
                Debug.LogError("DataDrivenUIManager: No Canvas found in scene!");
                return;
            }
        }

        _canvasScaler = targetCanvas.GetComponent<CanvasScaler>();
        
        // Setup canvas for UI scaling if needed
        SetupCanvasScaling();
    }

    private void Start()
    {
        if (currentLayout != null)
        {
            LoadHUDLayout(currentLayout);
        }

        // Ensure ammo UI is updated at the start of the level
        WeaponController weaponController = FindObjectOfType<WeaponController>();
        if (weaponController != null)
        {
            weaponController.TriggerInitialAmmoUpdate();
        }
    }

    private void OnEnable()
    {
        // Subscribe to events that UI elements might need
        SubscribeToEvents();
    }

    private void OnDisable()
    {
        // Unsubscribe from all events
        UnsubscribeFromEvents();
    }

    /// <summary>
    /// Load and activate a HUD layout.
    /// </summary>
    /// <param name="layout">The layout data to load.</param>
    public void LoadHUDLayout(HUDLayoutData layout)
    {
        if (layout == null)
        {
            Debug.LogError("DataDrivenUIManager: Cannot load null layout!");
            return;
        }

        if (!layout.ValidateLayout())
        {
            Debug.LogError($"DataDrivenUIManager: Layout '{layout.layoutName}' failed validation!");
            return;
        }

        // Clear existing UI
        ClearAllElements();

        currentLayout = layout;
        
        if (debugMode)
            Debug.Log($"Loading HUD Layout: {layout.layoutName}");

        // Create UI elements from the layout
        foreach (var elementData in layout.uiElements)
        {
            CreateUIElement(elementData);
        }

        // Animate HUD in if configured
        if (layout.fadeInOnActivate)
        {
            StartCoroutine(AnimateHUDIn());
        }
    }

    /// <summary>
    /// Create a UI element from data configuration.
    /// </summary>
    /// <param name="elementData">The element configuration.</param>
    private void CreateUIElement(UIElementData elementData)
    {
        // Create GameObject
        GameObject uiObject = new GameObject(elementData.elementId);
        uiObject.transform.SetParent(targetCanvas.transform, false);

        // Add RectTransform
        RectTransform rectTransform = uiObject.AddComponent<RectTransform>();
        
        // Add TextMeshPro component
        TextMeshProUGUI textComponent = uiObject.AddComponent<TextMeshProUGUI>();
        
        // Configure the element
        ConfigureUIElement(elementData, rectTransform, textComponent);

        // Create instance data
        UIElementInstance instance = new UIElementInstance
        {
            data = elementData,
            gameObject = uiObject,
            rectTransform = rectTransform,
            textComponent = textComponent,
            isVisible = elementData.visibleOnStart
        };

        // Store in dictionary
        _activeElements[elementData.elementId] = instance;

        // Set initial visibility
        uiObject.SetActive(elementData.visibleOnStart);

        if (debugMode)
            Debug.Log($"Created UI Element: {elementData.elementId}");
    }

    /// <summary>
    /// Configure a UI element based on its data.
    /// </summary>
    private void ConfigureUIElement(UIElementData data, RectTransform rectTransform, TextMeshProUGUI textComponent)
    {
        // Set up RectTransform
        SetupRectTransform(rectTransform, data);
        
        // Configure text component
        textComponent.text = data.textTemplate;
        textComponent.fontSize = data.fontSize;
        textComponent.color = data.textColor;
        textComponent.alignment = data.alignment;
        textComponent.font = data.fontAsset != null ? data.fontAsset : defaultFont;
    }

    /// <summary>
    /// Set up RectTransform positioning and anchoring.
    /// </summary>
    private void SetupRectTransform(RectTransform rectTransform, UIElementData data)
    {
        // Set anchor based on UIAnchor enum
        Vector2 anchorMin, anchorMax, pivot;
        GetAnchorValues(data.anchor, out anchorMin, out anchorMax, out pivot);
        
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        
        // Set size and position
        rectTransform.sizeDelta = data.size * _currentScaleFactor;
        rectTransform.anchoredPosition = data.screenPosition * _currentScaleFactor;
    }

    /// <summary>
    /// Get anchor values for different anchor types.
    /// </summary>
    private void GetAnchorValues(UIAnchor anchor, out Vector2 anchorMin, out Vector2 anchorMax, out Vector2 pivot)
    {
        switch (anchor)
        {
            case UIAnchor.TopLeft:
                anchorMin = anchorMax = pivot = new Vector2(0, 1);
                break;
            case UIAnchor.TopCenter:
                anchorMin = anchorMax = pivot = new Vector2(0.5f, 1);
                break;
            case UIAnchor.TopRight:
                anchorMin = anchorMax = pivot = new Vector2(1, 1);
                break;
            case UIAnchor.MiddleLeft:
                anchorMin = anchorMax = pivot = new Vector2(0, 0.5f);
                break;
            case UIAnchor.MiddleCenter:
                anchorMin = anchorMax = pivot = new Vector2(0.5f, 0.5f);
                break;
            case UIAnchor.MiddleRight:
                anchorMin = anchorMax = pivot = new Vector2(1, 0.5f);
                break;
            case UIAnchor.BottomLeft:
                anchorMin = anchorMax = pivot = new Vector2(0, 0);
                break;
            case UIAnchor.BottomCenter:
                anchorMin = anchorMax = pivot = new Vector2(0.5f, 0);
                break;
            case UIAnchor.BottomRight:
                anchorMin = anchorMax = pivot = new Vector2(1, 0);
                break;
            default:
                anchorMin = anchorMax = pivot = new Vector2(0, 1);
                break;
        }
    }

    /// <summary>
    /// Update a UI element with new data.
    /// </summary>
    /// <param name="elementId">ID of the element to update.</param>
    /// <param name="values">Values to substitute into the text template.</param>
    public void UpdateElement(string elementId, params object[] values)
    {
        if (!_activeElements.ContainsKey(elementId))
        {
            if (debugMode)
                Debug.LogWarning($"DataDrivenUIManager: Element '{elementId}' not found for update.");
            return;
        }

        var instance = _activeElements[elementId];
        string newText = string.Format(instance.data.textTemplate, values);
        instance.textComponent.text = newText;

        // Handle dynamic colors if enabled
        if (instance.data.useDynamicColors && values.Length > 0 && values[0] is float)
        {
            UpdateElementColor(instance, (float)values[0]);
        }

        if (debugMode)
            Debug.Log($"Updated UI Element '{elementId}': {newText}");
    }

    /// <summary>
    /// Update element color based on dynamic color ranges.
    /// </summary>
    private void UpdateElementColor(UIElementInstance instance, float value)
    {
        foreach (var colorRange in instance.data.colorRanges)
        {
            if (value >= colorRange.minValue && value <= colorRange.maxValue)
            {
                instance.textComponent.color = colorRange.color;
                return;
            }
        }
    }

    /// <summary>
    /// Show or hide a UI element.
    /// </summary>
    /// <param name="elementId">ID of the element.</param>
    /// <param name="visible">Whether to show or hide.</param>
    /// <param name="animated">Whether to animate the transition.</param>
    public void SetElementVisible(string elementId, bool visible, bool animated = true)
    {
        if (!_activeElements.ContainsKey(elementId))
            return;

        var instance = _activeElements[elementId];
        
        if (animated && instance.data.fadeDuration > 0)
        {
            StartCoroutine(AnimateElementVisibility(instance, visible));
        }
        else
        {
            instance.gameObject.SetActive(visible);
            instance.isVisible = visible;
        }
    }

    /// <summary>
    /// Animate element visibility with fade.
    /// </summary>
    private IEnumerator AnimateElementVisibility(UIElementInstance instance, bool visible)
    {
        if (visible && !instance.gameObject.activeInHierarchy)
        {
            instance.gameObject.SetActive(true);
        }

        CanvasGroup canvasGroup = instance.gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = instance.gameObject.AddComponent<CanvasGroup>();
        }

        float startAlpha = visible ? 0f : 1f;
        float endAlpha = visible ? 1f : 0f;
        float elapsed = 0f;

        canvasGroup.alpha = startAlpha;

        while (elapsed < instance.data.fadeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / instance.data.fadeDuration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, progress);
            yield return null;
        }

        canvasGroup.alpha = endAlpha;
        
        if (!visible)
        {
            instance.gameObject.SetActive(false);
        }
        
        instance.isVisible = visible;
    }

    /// <summary>
    /// Animate the entire HUD in.
    /// </summary>
    private IEnumerator AnimateHUDIn()
    {
        if (currentLayout == null) yield break;

        CanvasGroup hudGroup = targetCanvas.GetComponent<CanvasGroup>();
        if (hudGroup == null)
        {
            hudGroup = targetCanvas.gameObject.AddComponent<CanvasGroup>();
        }

        float elapsed = 0f;
        hudGroup.alpha = 0f;

        while (elapsed < currentLayout.activationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / currentLayout.activationDuration;
            float curveValue = currentLayout.activationCurve.Evaluate(progress);
            hudGroup.alpha = curveValue;
            yield return null;
        }

        hudGroup.alpha = 1f;
    }

    /// <summary>
    /// Clear all active UI elements.
    /// </summary>
    public void ClearAllElements()
    {
        foreach (var kvp in _activeElements)
        {
            if (kvp.Value.gameObject != null)
            {
                DestroyImmediate(kvp.Value.gameObject);
            }
        }
        _activeElements.Clear();
    }

    /// <summary>
    /// Setup canvas scaling based on layout settings.
    /// </summary>
    private void SetupCanvasScaling()
    {
        if (currentLayout == null || !currentLayout.scaleWithResolution)
            return;

        if (_canvasScaler == null)
        {
            _canvasScaler = targetCanvas.gameObject.AddComponent<CanvasScaler>();
        }

        _canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _canvasScaler.referenceResolution = currentLayout.referenceResolution;
        
        switch (currentLayout.screenMatchMode)
        {
            case ScreenMatchMode.MatchWidthOrHeight:
                _canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                break;
            case ScreenMatchMode.Expand:
                _canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                break;
            case ScreenMatchMode.Shrink:
                _canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;
                break;
        }

        // Calculate current scale factor
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        Vector2 referenceSize = currentLayout.referenceResolution;
        _currentScaleFactor = Mathf.Min(screenSize.x / referenceSize.x, screenSize.y / referenceSize.y);
    }

    /// <summary>
    /// Subscribe to game events for automatic UI updates.
    /// </summary>
    private void SubscribeToEvents()
    {
        // Subscribe to common game events that UI elements might listen to
        Health.OnPlayerHealthChanged += HandleHealthChanged;
        WaveManager.OnWaveChanged += HandleWaveChanged;
        WeaponController.OnAmmoChanged += HandleAmmoChanged;
        
        // Add more event subscriptions as needed based on your game's events
    }

    /// <summary>
    /// Unsubscribe from game events.
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        Health.OnPlayerHealthChanged -= HandleHealthChanged;
        WaveManager.OnWaveChanged -= HandleWaveChanged;
        WeaponController.OnAmmoChanged -= HandleAmmoChanged;
    }

    // Event handlers that automatically update UI elements
    private void HandleHealthChanged(float newHealth, float maxHealth)
    {
        // Find all health-related UI elements and update them
        foreach (var kvp in _activeElements)
        {
            var element = kvp.Value;
            if (element.data.eventType == UIEventType.HealthChanged && element.data.autoUpdate)
            {
                UpdateElement(kvp.Key, newHealth, maxHealth);
            }
        }
    }

    private void HandleWaveChanged(int newWave)
    {
        if (debugMode)
            Debug.Log($"DataDrivenUIManager: HandleWaveChanged called with wave {newWave}");
            
        foreach (var kvp in _activeElements)
        {
            var element = kvp.Value;
            if (element.data.eventType == UIEventType.WaveChanged && element.data.autoUpdate)
            {
                if (debugMode)
                    Debug.Log($"DataDrivenUIManager: Updating wave element '{kvp.Key}' to wave {newWave}");
                UpdateElement(kvp.Key, newWave);
            }
        }
        
        if (debugMode)
            Debug.Log($"DataDrivenUIManager: Found {_activeElements.Count} total active elements");
    }

    private void HandleAmmoChanged(int currentAmmo, int maxAmmo)
    {
        foreach (var kvp in _activeElements)
        {
            var element = kvp.Value;
            if (element.data.eventType == UIEventType.AmmoChanged && element.data.autoUpdate)
            {
                UpdateElement(kvp.Key, currentAmmo, maxAmmo);
            }
        }
    }

    /// <summary>
    /// Public method to manually update any UI element by ID.
    /// </summary>
    /// <param name="elementId">The ID of the element to update.</param>
    /// <param name="values">Values to pass to the element's text template.</param>
    public static void UpdateUI(string elementId, params object[] values)
    {
        if (Instance != null)
        {
            Instance.UpdateElement(elementId, values);
        }
    }

    /// <summary>
    /// Public method to show/hide UI elements.
    /// </summary>
    /// <param name="elementId">The ID of the element.</param>
    /// <param name="visible">Whether to show or hide.</param>
    public static void SetUIVisible(string elementId, bool visible)
    {
        if (Instance != null)
        {
            Instance.SetElementVisible(elementId, visible);
        }
    }
}

/// <summary>
/// Runtime instance data for a UI element.
/// </summary>
[Serializable]
public class UIElementInstance
{
    public UIElementData data;
    public GameObject gameObject;
    public RectTransform rectTransform;
    public TextMeshProUGUI textComponent;
    public bool isVisible;
}
