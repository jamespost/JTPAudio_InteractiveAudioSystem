using UnityEngine;
using UnityEngine.SceneManagement; // Add this for scene management
using System.Collections.Generic;

/// <summary>
/// A complete, code-driven pause menu for 'Project Resonance'.
/// This script handles UI drawing via OnGUI and a dynamic particle sphere background via GL lines.
/// It requires no Canvas elements in the scene.
///
/// --- SETUP INSTRUCTIONS ---
/// 1. Create this C# script in your Unity project.
/// 2. Create a simple "Unlit/Color" material for the particle system.
///    - In the Project window, right-click -> Create -> Material.
///    - Name it something like "PauseMenuGLMaterial".
///    - In the Inspector for the material, select the Shader dropdown and choose "Unlit/Color".
///    - Leave the color as white.
/// 3. Attach this script to your Main Camera GameObject.
/// 4. In the Inspector for the camera, you will see the script's public fields.
///    - Drag the "Share Tech Mono" font file from your project into the "UI Font" field.
///    - Drag the "PauseMenuGLMaterial" you created into the "GL Line Material" field.
/// 5. Press Play to see the menu. The menu will appear when you set the 'isPaused' variable to true
///    (e.g., by pressing 'Escape'). I have included basic Escape key handling to toggle the menu.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    // --- PUBLIC CONFIGURATION ---
    // These fields will be visible in the Unity Inspector for easy tweaking.

    [Header("Menu State")]
    [Tooltip("Controls whether the pause menu is currently visible and active.")]
    public bool isPaused = false;

    [Header("UI & Styling")]
    [Tooltip("The font used for all text elements in the pause menu.")]
    public Font uiFont;

    [Tooltip("The main color for the UI text.")]
    public Color primaryColor = new Color(1.0f, 0.72f, 0.0f); // Hex: #FFB800

    [Tooltip("The color of the UI text when hovered over.")]
    public Color hoverColor = Color.white;

    [Header("Particle Sphere Configuration")]
    [Tooltip("The material used to draw the particle sphere. Use a simple Unlit/Color material.")]
    public Material glLineMaterial;

    [Tooltip("The number of particles in the sphere simulation.")]
    [Range(50, 500)]
    public int particleCount = 250;

    [Tooltip("The maximum distance between particles to draw a connecting line.")]
    public float maxConnectionDistance = 1.5f;

    [Tooltip("The overall size of the particle sphere.")]
    public float sphereRadius = 5.0f;

    [Tooltip("The speed at which particles move.")]
    public float particleSpeed = 0.1f;

    [Tooltip("The distance of the sphere from the camera.")]
    public float sphereDistance = 15.0f;

    [Tooltip("The delay in seconds before the mouse can be moved after entering the pause menu.")]
    [Range(0f, 1f)]
    public float mouseUnlockDelay = 0.1f; // Default to 0.1 seconds

    [Header("Menu Context")]
    [Tooltip("Set this to true if this menu is used as the main menu.")]
    public bool isMainMenu = false;

    private bool isGameOver = false; // Track if the game is in a game over state


    // --- PRIVATE FIELDS ---
    private List<Particle> particles;
    private GUIStyle headerStyle, footerStyle, menuItemStyle;
    private int selectedIndex = 0;
    private string[] menuItems = { "Resume", "Restart Level", "Exit to Main Menu", "Exit to Desktop" };


    /// <summary>
    /// Represents a single particle in the background sphere animation.
    /// </summary>
    private class Particle
    {
        public Vector3 position;
        public Vector3 velocity;
        public float sphereRadius;

        public Particle(float radius)
        {
            // Start at a random point within the sphere's volume
            position = Random.insideUnitSphere * radius;
            velocity = Random.insideUnitSphere * 0.1f;
            sphereRadius = radius;
        }

        public void Update(float speed)
        {
            position += velocity * speed * Time.unscaledDeltaTime;

            // If the particle goes outside the sphere, invert its velocity to "bounce" back in.
            if (position.magnitude > sphereRadius)
            {
                velocity = -velocity;
            }
        }
    }

    /// <summary>
    /// Called when the script instance is being loaded.
    /// </summary>
    void Start()
    {
        InitializeParticles();
    }

    /// <summary>
    /// Initializes the list of particles for the background animation.
    /// </summary>
    void InitializeParticles()
    {
        particles = new List<Particle>();
        for (int i = 0; i < particleCount; i++)
        {
            particles.Add(new Particle(sphereRadius));
        }
    }

    /// <summary>
    /// Creates and configures the GUIStyles needed for the menu text.
    /// This is called from OnGUI to ensure it's ready for rendering.
    /// </summary>
    void SetupGUIStyles()
    {
        // Header Style ("Project Resonance")
        headerStyle = new GUIStyle(GUI.skin.label)
        {
            font = uiFont,
            fontSize = 20,
            alignment = TextAnchor.UpperLeft,
            normal = { textColor = primaryColor }
        };

        // Footer Style
        footerStyle = new GUIStyle(GUI.skin.label)
        {
            font = uiFont,
            fontSize = 14,
            alignment = TextAnchor.LowerRight,
            normal = { textColor = new Color(primaryColor.r, primaryColor.g, primaryColor.b, 0.75f) }
        };

        // Menu Item Style
        menuItemStyle = new GUIStyle(GUI.skin.label)
        {
            font = uiFont,
            fontSize = 50,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = primaryColor }
        };
    }

    /// <summary>
    /// Update is called once per frame.
    /// </summary>
    void Update()
    {
        // Simple input to toggle the pause menu
        if (!isMainMenu && Input.GetKeyDown(KeyCode.Escape))
        {
            isPaused = !isPaused;

            // Notify the GameManager of the state change
            if (isPaused)
            {
                GameManager.Instance.SetGameState(GameManager.GameState.PAUSED);
            }
            else
            {
                GameManager.Instance.SetGameState(GameManager.GameState.IN_GAME);
            }

            // Freeze time when paused, unfreeze when resumed
            Time.timeScale = isPaused ? 0f : 1f;

            // Manage cursor lock state and visibility
            if (isPaused)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false; // Hide the cursor during gameplay
            }
        }

        // Only update particles if the menu is paused and they exist
        if ((isPaused || isGameOver) && particles != null)
        {
            foreach (var p in particles)
            {
                p.Update(particleSpeed);
            }
        }
    }

    public void TriggerGameOverMenu()
    {
        isGameOver = true;
        isPaused = true;
        Time.timeScale = 0f;
        GameManager.Instance.SetGameState(GameManager.GameState.GAME_OVER);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// OnGUI is called for rendering and handling GUI events.
    /// </summary>
    void OnGUI()
    {
        if (!isPaused && !isGameOver && !isMainMenu) return;

        // Null check for the font to prevent errors if not assigned.
        if (uiFont == null)
        {
            Debug.LogError("UI Font is not assigned in the PauseMenuController Inspector!");
            GUI.Label(new Rect(10, 10, 400, 30), "ERROR: UI Font not assigned.");
            return;
        }

        // Setup styles on-the-fly. OnGUI can be called multiple times per frame.
        SetupGUIStyles();

        float safeMargin = Screen.width * 0.05f; // 5% margin

        // --- DRAW UI TEXT ---
        // Header
        GUI.Label(new Rect(safeMargin, safeMargin, 400, 50), isMainMenu ? "Main Menu" : "Project Resonance", headerStyle);

        // Footer
        if (!isMainMenu)
        {
            GUI.Label(new Rect(Screen.width - 400 - safeMargin, Screen.height - 50 - safeMargin, 400, 50), "System Paused. All processes stable.", footerStyle);
        }

        // --- DRAW MENU ITEMS ---
        float menuTop = Screen.height * 0.3f;
        float menuHeight = 70f;

        // Adjust menu items based on context
        string[] currentMenuItems;
        if (isMainMenu)
        {
            currentMenuItems = new string[] { "Start Game", "Exit to Desktop" };
        }
        else if (isGameOver)
        {
            currentMenuItems = new string[] { "Restart Level", "Exit to Main Menu", "Exit to Desktop" };
        }
        else
        {
            currentMenuItems = menuItems;
        }

        for (int i = 0; i < currentMenuItems.Length; i++)
        {
            Rect itemRect = new Rect(safeMargin, menuTop + (i * menuHeight), 600, menuHeight);

            // Check for mouse hover
            bool isHovering = itemRect.Contains(Event.current.mousePosition);

            // Set text color based on hover state
            menuItemStyle.normal.textColor = isHovering ? hoverColor : primaryColor;

            // Update selectedIndex based on hover
            if (isHovering)
            {
                selectedIndex = i;
            }

            // Draw the blinking '>' symbol next to the selected item
            if (selectedIndex == i)
            {
                GUI.Label(new Rect(itemRect.x - 30, itemRect.y, 20, menuHeight), ">", menuItemStyle);
            }

            // Apply a slight indent on hover for visual feedback
            if (isHovering)
            {
                itemRect.x += 10;
            }

            GUI.Label(itemRect, currentMenuItems[i], menuItemStyle);

            // Check for mouse click
            if (isHovering && Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                HandleMenuSelection(i, currentMenuItems);
                Event.current.Use(); // Consume the event
            }
        }
    }

    /// <summary>
    /// Executes an action based on which menu item was clicked.
    /// </summary>
    void HandleMenuSelection(int index, string[] currentMenuItems)
    {
        switch (currentMenuItems[index])
        {
            case "Resume":
                Debug.Log("Resuming game...");
                isPaused = false;
                Time.timeScale = 1f;
                GameManager.Instance.SetGameState(GameManager.GameState.IN_GAME);
                break;
            case "Restart Level":
                Debug.Log("Restarting level...");
                GameManager.Instance.SetGameState(GameManager.GameState.LEVEL_LOADING);
                break;
            case "Exit to Main Menu":
                Debug.Log("Exiting to main menu...");
                GameManager.Instance.SetGameState(GameManager.GameState.MAIN_MENU);
                break;
            case "Exit to Desktop":
                Debug.Log("Exiting to desktop...");
                Application.Quit();
                break;
            case "Start Game":
                Debug.Log("Starting game...");
                SceneManager.LoadScene("Game"); // Replace "GameScene" with your actual game scene name
                GameManager.Instance.SetGameState(GameManager.GameState.IN_GAME);
                break;
        }
    }


    /// <summary>
    /// Called after the camera has finished rendering, allowing for custom GL drawing.
    /// </summary>
    void OnPostRender()
    {
        if (!isPaused || particles == null) return;

        // Null check for the material.
        if (glLineMaterial == null)
        {
            Debug.LogError("GL Line Material is not assigned in the PauseMenuController Inspector!");
            return;
        }

        // The center of the sphere in world space, placed in front of the camera.
        Vector3 sphereCenter = transform.position + transform.forward * sphereDistance;

        glLineMaterial.SetPass(0); // Activate the material's first pass.
        GL.PushMatrix(); // Save the current matrix.
        GL.MultMatrix(Matrix4x4.Translate(sphereCenter)); // Move the drawing space to the sphere's center.

        // --- DRAW CONNECTION LINES ---
        GL.Begin(GL.LINES);
        GL.Color(new Color(primaryColor.r, primaryColor.g, primaryColor.b, 0.15f));

        for (int i = 0; i < particles.Count; i++)
        {
            for (int j = i + 1; j < particles.Count; j++)
            {
                float dist = Vector3.Distance(particles[i].position, particles[j].position);
                if (dist < maxConnectionDistance)
                {
                    GL.Vertex(particles[i].position);
                    GL.Vertex(particles[j].position);
                }
            }
        }
        GL.End();

        // --- DRAW PARTICLES ---
        // We draw particles as small quads (squares).
        GL.Begin(GL.QUADS);
        GL.Color(new Color(primaryColor.r, primaryColor.g, primaryColor.b, 0.8f));
        
        foreach (var p in particles)
        {
            // Get the camera's right and up vectors to billboard the quads
            Vector3 camRight = transform.right * 0.05f; // 0.05f is the particle size
            Vector3 camUp = transform.up * 0.05f;

            GL.Vertex(p.position - camRight - camUp);
            GL.Vertex(p.position + camRight - camUp);
            GL.Vertex(p.position + camRight + camUp);
            GL.Vertex(p.position - camRight + camUp);
        }
        GL.End();

        GL.PopMatrix(); // Restore the original matrix.
    }
}
