using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor utilities for the Data-Driven UI System.
/// Provides helper methods to create and manage UI assets.
/// </summary>
public static class UISystemEditorUtils
{
    [MenuItem("UI System/Create Example HUD Layout")]
    public static void CreateExampleHUDLayout()
    {
        // Create directory if it doesn't exist
        string assetPath = "Assets/UI_Data";
        if (!AssetDatabase.IsValidFolder(assetPath))
        {
            AssetDatabase.CreateFolder("Assets", "UI_Data");
        }

        // Create health UI element
        UIElementData healthElement = ScriptableObject.CreateInstance<UIElementData>();
        healthElement.elementId = "player_health";
        healthElement.displayName = "Player Health";
        healthElement.textTemplate = "Health: {0}/{1}";
        healthElement.screenPosition = new Vector2(10, -10);
        healthElement.anchor = UIAnchor.TopLeft;
        healthElement.size = new Vector2(200, 30);
        healthElement.fontSize = 18;
        healthElement.textColor = Color.white;
        healthElement.eventType = UIEventType.HealthChanged;
        healthElement.autoUpdate = true;
        healthElement.useDynamicColors = true;
        healthElement.colorRanges = new UIColorRange[]
        {
            new UIColorRange { minValue = 75, maxValue = 100, color = Color.green, rangeName = "Healthy" },
            new UIColorRange { minValue = 25, maxValue = 74, color = Color.yellow, rangeName = "Damaged" },
            new UIColorRange { minValue = 0, maxValue = 24, color = Color.red, rangeName = "Critical" }
        };

        AssetDatabase.CreateAsset(healthElement, $"{assetPath}/PlayerHealth_UIElement.asset");

        // Create ammo UI element
        UIElementData ammoElement = ScriptableObject.CreateInstance<UIElementData>();
        ammoElement.elementId = "player_ammo";
        ammoElement.displayName = "Player Ammo";
        ammoElement.textTemplate = "Ammo: {0}/{1}";
        ammoElement.screenPosition = new Vector2(10, -50);
        ammoElement.anchor = UIAnchor.TopLeft;
        ammoElement.size = new Vector2(200, 30);
        ammoElement.fontSize = 18;
        ammoElement.textColor = Color.white;
        ammoElement.eventType = UIEventType.AmmoChanged;
        ammoElement.autoUpdate = true;
        ammoElement.useDynamicColors = true;
        ammoElement.colorRanges = new UIColorRange[]
        {
            new UIColorRange { minValue = 75, maxValue = 100, color = Color.cyan, rangeName = "Full" },
            new UIColorRange { minValue = 25, maxValue = 74, color = Color.yellow, rangeName = "Medium" },
            new UIColorRange { minValue = 0, maxValue = 24, color = Color.red, rangeName = "Low" }
        };

        AssetDatabase.CreateAsset(ammoElement, $"{assetPath}/PlayerAmmo_UIElement.asset");

        // Create wave UI element
        UIElementData waveElement = ScriptableObject.CreateInstance<UIElementData>();
        waveElement.elementId = "current_wave";
        waveElement.displayName = "Current Wave";
        waveElement.textTemplate = "Wave: {0}";
        waveElement.screenPosition = new Vector2(-10, -10);
        waveElement.anchor = UIAnchor.TopRight;
        waveElement.size = new Vector2(150, 30);
        waveElement.fontSize = 20;
        waveElement.textColor = Color.yellow;
        waveElement.eventType = UIEventType.WaveChanged;
        waveElement.autoUpdate = true;

        AssetDatabase.CreateAsset(waveElement, $"{assetPath}/CurrentWave_UIElement.asset");

        // Create HUD Layout
        HUDLayoutData hudLayout = ScriptableObject.CreateInstance<HUDLayoutData>();
        hudLayout.layoutName = "Default Game HUD";
        hudLayout.description = "Standard HUD layout for the main game with health, ammo, and wave information.";
        hudLayout.uiElements.Add(healthElement);
        hudLayout.uiElements.Add(ammoElement);
        hudLayout.uiElements.Add(waveElement);
        hudLayout.scaleWithResolution = true;
        hudLayout.referenceResolution = new Vector2(1920, 1080);
        hudLayout.fadeInOnActivate = true;
        hudLayout.activationDuration = 0.5f;

        AssetDatabase.CreateAsset(hudLayout, $"{assetPath}/DefaultGameHUD_Layout.asset");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Example HUD Layout created successfully! Check the Assets/UI_Data folder.");
        
        // Select the created layout in the project window
        Selection.activeObject = hudLayout;
        EditorGUIUtility.PingObject(hudLayout);
    }

    [MenuItem("UI System/Setup Game Scene for Data-Driven UI")]
    public static void SetupGameSceneForDataDrivenUI()
    {
        // Find existing Canvas
        Canvas existingCanvas = Object.FindObjectOfType<Canvas>();
        
        if (existingCanvas == null)
        {
            // Create a new Canvas
            GameObject canvasObject = new GameObject("UI Canvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
            existingCanvas = canvas;
        }

        // Check if DataDrivenUIManager already exists
        DataDrivenUIManager existingManager = Object.FindObjectOfType<DataDrivenUIManager>();
        
        if (existingManager == null)
        {
            // Add DataDrivenUIManager to the canvas
            DataDrivenUIManager uiManager = existingCanvas.gameObject.AddComponent<DataDrivenUIManager>();
            uiManager.targetCanvas = existingCanvas;
            
            Debug.Log("DataDrivenUIManager added to the scene! Assign a HUD Layout in the inspector.");
            
            // Select the UI Manager in the hierarchy
            Selection.activeGameObject = existingCanvas.gameObject;
            EditorGUIUtility.PingObject(existingCanvas.gameObject);
        }
        else
        {
            Debug.Log("DataDrivenUIManager already exists in the scene.");
            Selection.activeGameObject = existingManager.gameObject;
            EditorGUIUtility.PingObject(existingManager.gameObject);
        }
    }
}
