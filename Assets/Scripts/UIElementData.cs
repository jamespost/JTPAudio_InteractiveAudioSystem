using UnityEngine;
using TMPro;
using System;

/// <summary>
/// Defines the configuration for a single UI element in a data-driven way.
/// Designers can create instances of this ScriptableObject to define UI elements
/// without touching code or manually setting up TextMeshPro components.
/// </summary>
[CreateAssetMenu(fileName = "New UI Element", menuName = "UI System/UI Element Data")]
public class UIElementData : ScriptableObject
{
    [Header("Basic Configuration")]
    [Tooltip("Unique identifier for this UI element. Used to reference it in code.")]
    public string elementId;
    
    [Tooltip("Display name for the element (for designer reference).")]
    public string displayName;
    
    [Tooltip("Initial text to display. Use {0}, {1}, etc. for dynamic values.")]
    [TextArea(2, 4)]
    public string textTemplate = "Health: {0}";

    [Header("Position & Layout")]
    [Tooltip("Screen position for this UI element.")]
    public Vector2 screenPosition = new Vector2(10, 10);
    
    [Tooltip("How the element should be anchored to the screen.")]
    public UIAnchor anchor = UIAnchor.TopLeft;
    
    [Tooltip("Size of the UI element.")]
    public Vector2 size = new Vector2(200, 30);

    [Header("Text Style")]
    [Tooltip("Font size for the text.")]
    [Range(8, 72)]
    public int fontSize = 18;
    
    [Tooltip("Color of the text.")]
    public Color textColor = Color.white;
    
    [Tooltip("Text alignment within the element.")]
    public TextAlignmentOptions alignment = TextAlignmentOptions.Left;
    
    [Tooltip("Font asset to use. Leave null for default.")]
    public TMP_FontAsset fontAsset;

    [Header("Behavior")]
    [Tooltip("Should this element be visible at game start?")]
    public bool visibleOnStart = true;
    
    [Tooltip("Fade in/out duration for show/hide animations.")]
    public float fadeDuration = 0.3f;
    
    [Tooltip("Should this element update automatically when related events fire?")]
    public bool autoUpdate = true;

    [Header("Event Binding")]
    [Tooltip("What type of event should trigger updates to this UI element.")]
    public UIEventType eventType = UIEventType.None;
    
    [Tooltip("For custom events, specify the event name.")]
    public string customEventName;

    [Header("Dynamic Styling")]
    [Tooltip("Enable dynamic color changes based on value ranges.")]
    public bool useDynamicColors = false;
    
    [Tooltip("Color ranges for dynamic styling (e.g., health bar colors).")]
    public UIColorRange[] colorRanges = new UIColorRange[0];
}

/// <summary>
/// Defines how UI elements should be anchored to the screen.
/// </summary>
public enum UIAnchor
{
    TopLeft,
    TopCenter,
    TopRight,
    MiddleLeft,
    MiddleCenter,
    MiddleRight,
    BottomLeft,
    BottomCenter,
    BottomRight
}

/// <summary>
/// Defines what type of game event should trigger UI updates.
/// </summary>
public enum UIEventType
{
    None,
    HealthChanged,
    AmmoChanged,
    WaveChanged,
    ScoreChanged,
    Custom
}

/// <summary>
/// Defines a color range for dynamic UI styling.
/// </summary>
[Serializable]
public class UIColorRange
{
    [Tooltip("Minimum value for this color range.")]
    public float minValue;
    
    [Tooltip("Maximum value for this color range.")]
    public float maxValue;
    
    [Tooltip("Color to use when value is in this range.")]
    public Color color = Color.white;
    
    [Tooltip("Name for this color range (for designer reference).")]
    public string rangeName;
}
