using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Defines a complete UI layout configuration for a specific game scene or state.
/// Designers can create different HUD layouts for different game modes, difficulty levels,
/// or scenes without modifying code or hierarchy.
/// </summary>
[CreateAssetMenu(fileName = "New HUD Layout", menuName = "UI System/HUD Layout Data")]
public class HUDLayoutData : ScriptableObject
{
    [Header("Layout Information")]
    [Tooltip("Name of this HUD layout for designer reference.")]
    public string layoutName;
    
    [Tooltip("Description of when this layout should be used.")]
    [TextArea(2, 3)]
    public string description;

    [Header("UI Elements")]
    [Tooltip("All UI elements that should be part of this HUD layout.")]
    public List<UIElementData> uiElements = new List<UIElementData>();

    [Header("Layout Settings")]
    [Tooltip("Should elements automatically scale with screen resolution?")]
    public bool scaleWithResolution = true;
    
    [Tooltip("Reference resolution for scaling calculations.")]
    public Vector2 referenceResolution = new Vector2(1920, 1080);
    
    [Tooltip("How should elements behave when screen aspect ratio changes?")]
    public ScreenMatchMode screenMatchMode = ScreenMatchMode.MatchWidthOrHeight;

    [Header("Animation Settings")]
    [Tooltip("Should the entire HUD fade in when activated?")]
    public bool fadeInOnActivate = true;
    
    [Tooltip("Duration for HUD activation animation.")]
    public float activationDuration = 0.5f;
    
    [Tooltip("Animation curve for HUD activation.")]
    public AnimationCurve activationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    /// <summary>
    /// Get a UI element by its ID.
    /// </summary>
    /// <param name="elementId">The ID of the element to find.</param>
    /// <returns>The UIElementData if found, null otherwise.</returns>
    public UIElementData GetElementById(string elementId)
    {
        return uiElements.Find(element => element.elementId == elementId);
    }

    /// <summary>
    /// Get all UI elements that should auto-update based on events.
    /// </summary>
    /// <returns>List of auto-updating UI elements.</returns>
    public List<UIElementData> GetAutoUpdateElements()
    {
        return uiElements.FindAll(element => element.autoUpdate);
    }

    /// <summary>
    /// Validate this layout configuration.
    /// </summary>
    /// <returns>True if valid, false if there are issues.</returns>
    public bool ValidateLayout()
    {
        HashSet<string> usedIds = new HashSet<string>();
        
        foreach (var element in uiElements)
        {
            if (element == null)
            {
                Debug.LogError($"HUD Layout '{layoutName}' contains null UI element reference.");
                return false;
            }
            
            if (string.IsNullOrEmpty(element.elementId))
            {
                Debug.LogError($"HUD Layout '{layoutName}' contains UI element with empty ID.");
                return false;
            }
            
            if (usedIds.Contains(element.elementId))
            {
                Debug.LogError($"HUD Layout '{layoutName}' contains duplicate element ID: '{element.elementId}'");
                return false;
            }
            
            usedIds.Add(element.elementId);
        }
        
        return true;
    }
}

/// <summary>
/// Defines how UI should adapt to different screen aspect ratios.
/// </summary>
public enum ScreenMatchMode
{
    MatchWidthOrHeight,
    Expand,
    Shrink
}
