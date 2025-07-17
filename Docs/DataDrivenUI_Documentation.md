# Data-Driven UI System Documentation

## Overview

The Data-Driven UI System allows designers to create and configure UI elements entirely through ScriptableObject assets, without needing to modify code or manually set up TextMeshPro components in the hierarchy. This system provides a reactive UI that automatically updates based on game events.

## Key Features

- **Completely Data-Driven**: UI elements are defined in ScriptableObject assets
- **Event-Based Updates**: UI automatically updates when game events fire (health changes, ammo changes, wave changes)
- **Dynamic Styling**: Elements can change color based on value ranges (e.g., health bar turning red when low)
- **Resolution Independence**: UI scales properly across different screen resolutions
- **Animation Support**: Elements can fade in/out and animate when showing/hiding
- **Designer Friendly**: No code changes needed to create new UI elements or layouts

## How It Works

### 1. UI Element Data (`UIElementData`)
Defines a single UI element with properties like:
- `elementId`: Unique identifier for the element
- `textTemplate`: Display text with placeholders (e.g., "Health: {0}/{1}")
- `screenPosition`: Where to position the element
- `anchor`: How to anchor the element to the screen (TopLeft, BottomRight, etc.)
- `eventType`: What game event should trigger updates (HealthChanged, AmmoChanged, WaveChanged)
- `colorRanges`: Dynamic color changes based on value ranges

### 2. HUD Layout Data (`HUDLayoutData`)
Defines a complete UI layout containing multiple UI elements:
- `layoutName`: Name for the layout
- `uiElements`: List of UI elements to include
- `scaleWithResolution`: Whether to scale with screen resolution
- `fadeInOnActivate`: Whether to animate the HUD when it loads

### 3. Data-Driven UI Manager (`DataDrivenUIManager`)
The runtime system that:
- Loads HUD layouts and creates UI elements at runtime
- Automatically subscribes to game events
- Updates UI elements when events fire
- Handles positioning, scaling, and animations

## Getting Started

### Step 1: Create Example Assets
1. In Unity, go to the menu: **UI System → Create Example HUD Layout**
2. This creates example UI elements and a HUD layout in `Assets/UI_Data/`

### Step 2: Setup Your Scene
1. Go to **UI System → Setup Game Scene for Data-Driven UI**
2. This adds the `DataDrivenUIManager` to your scene's Canvas

### Step 3: Assign the Layout
1. Select the Canvas GameObject in your scene
2. In the `DataDrivenUIManager` component, assign the `DefaultGameHUD_Layout` asset to the "Current Layout" field

### Step 4: Configure Player Health Component
1. Find your Player GameObject in the scene
2. On the `Health` component, check the "Is Player" checkbox
3. This ensures player health changes trigger UI updates

## Creating Custom UI Elements

### Creating a New UI Element
1. Right-click in Project window
2. Create → UI System → UI Element Data
3. Configure the element properties:

```
Element ID: "score_display"
Display Name: "Player Score"
Text Template: "Score: {0}"
Screen Position: (10, -90)
Anchor: TopLeft
Event Type: Custom
Custom Event Name: "ScoreChanged"
Auto Update: true
```

### Creating a New HUD Layout
1. Right-click in Project window
2. Create → UI System → HUD Layout Data
3. Add your UI elements to the list
4. Configure layout properties

## Event Integration

The system automatically listens to these events:
- `Health.OnPlayerHealthChanged` - Fires when player health changes
- `WaveManager.OnWaveChanged` - Fires when wave number changes
- `WeaponController.OnAmmoChanged` - Fires when ammo changes

### Adding Custom Events
To add support for a custom event:

1. **Create the event in your script:**
```csharp
public static event System.Action<int> OnScoreChanged;

// Fire the event when score changes
OnScoreChanged?.Invoke(newScore);
```

2. **Subscribe in DataDrivenUIManager:**
```csharp
// In SubscribeToEvents()
YourScript.OnScoreChanged += HandleScoreChanged;

// Add handler method
private void HandleScoreChanged(int newScore)
{
    foreach (var kvp in _activeElements)
    {
        var element = kvp.Value;
        if (element.data.eventType == UIEventType.Custom && 
            element.data.customEventName == "ScoreChanged" && 
            element.data.autoUpdate)
        {
            UpdateElement(kvp.Key, newScore);
        }
    }
}
```

## Dynamic Color Ranges

UI elements can change color based on their values. For example, health that turns red when low:

```
Color Ranges:
- Min: 75, Max: 100, Color: Green, Name: "Healthy"
- Min: 25, Max: 74, Color: Yellow, Name: "Damaged"  
- Min: 0, Max: 24, Color: Red, Name: "Critical"
```

## Manual UI Updates

You can also manually update UI elements from code:

```csharp
// Update any UI element by ID
DataDrivenUIManager.UpdateUI("score_display", 1500);

// Show/hide elements
DataDrivenUIManager.SetUIVisible("boss_health", true);
```

## Best Practices

1. **Use Clear Element IDs**: Make IDs descriptive like "player_health" instead of "ui1"
2. **Group Related Elements**: Create different layouts for different game states
3. **Test at Different Resolutions**: Enable "Scale With Resolution" for responsive design
4. **Use Color Ranges Sparingly**: Only use dynamic colors when they provide gameplay feedback
5. **Keep Templates Simple**: Use clear text templates with meaningful placeholders

## Troubleshooting

**UI Elements Not Appearing:**
- Check that the HUD Layout is assigned to the DataDrivenUIManager
- Verify that "Visible On Start" is enabled for the UI elements
- Ensure the Canvas has a DataDrivenUIManager component

**Events Not Firing:**
- Check that the source script (Health, WeaponController, etc.) has the "Is Player" checkbox enabled where applicable
- Verify that Event Type is set correctly on UI elements
- Check that Auto Update is enabled

**Position Issues:**
- Verify the anchor settings match your intended positioning
- Check that Screen Position values are appropriate for your anchor choice
- Test at different resolutions if using resolution scaling

## Architecture Benefits

This system provides several advantages over traditional UI approaches:

1. **Separation of Concerns**: UI layout is separate from game logic
2. **Designer Empowerment**: Non-programmers can create and modify UI
3. **Data-Driven**: Easy to create multiple UI themes or layouts
4. **Event-Driven**: Loose coupling between UI and game systems
5. **Maintainable**: Changes to UI don't require code modifications
6. **Scalable**: Easy to add new UI elements or create variants
