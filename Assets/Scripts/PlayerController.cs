using UnityEngine;
using System.Collections; // Required for IEnumerator

/// <summary>
/// Handles player movement, input, and interactions using Unity's CharacterController.
/// </summary>
/// 
/// remarks>To set up the inputs in your Unity project for the PlayerController script, follow these steps:
/// <summary>
///Open the Input Manager:

///In Unity, go to Edit > Project Settings > Input Manager.
///Add Input Axes:

///Add or configure the following axes:
///Expand the Input Manager section in the Project Settings.
///Horizontal: Used for horizontalInput (A/D or Left/Right arrows).
///Name: Horizontal
///Type: Joystick Axis
///Axis: X axis
///Vertical: Used for verticalInput (W/S or Up/Down arrows).
///Name: Vertical
///Type: Joystick Axis
///Axis: Y axis
///Add Buttons:

///Add or configure the following buttons:
///Jump: Used for jumpPressed (Spacebar).
///Name: Jump
///Positive Button: space
///Sprint: Used for sprintPressed (Left Shift).
///This is handled using Input.GetKey(KeyCode.LeftShift) in the script, so no additional setup is needed in the Input Manager.
///Save and Test:

///Save your changes and test the inputs in Play mode to ensure they work as expected.
///If you're using Unity's newer Input System, you'll need to configure an Input Action Asset and map the actions accordingly
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // --- Movement Settings ---
    [Header("Movement Stats")]
    [Tooltip("The speed at which the player walks.")]
    public float walkSpeed = 5.0f; // Base walking speed
    [Tooltip("The speed at which the player sprints.")]
    public float sprintSpeed = 10.0f; // Speed when sprinting
    [Tooltip("The height of the player's jump.")]
    public float jumpHeight = 2.0f; // Determines jump force
    [Tooltip("The gravity applied to the player.")]
    public float gravity = -9.81f; // Gravity force applied to the player

    // --- Mouse Look Settings ---
    [Header("Mouse Look Settings")]
    [Tooltip("The sensitivity of the mouse movement on the X axis.")]
    public float mouseSensitivityX = 400.0f; // Horizontal sensitivity multiplier
    [Tooltip("The sensitivity of the mouse movement on the Y axis.")]
    public float mouseSensitivityY = 400.0f; // Vertical sensitivity multiplier
    [Tooltip("The smoothing factor for mouse movement.")]
    public float mouseSmoothing = 0.05f; // Smoothing factor for mouse movement
    [Tooltip("The minimum vertical angle the camera can look.")]
    public float minVerticalAngle = -60.0f; // Minimum vertical look angle
    [Tooltip("The maximum vertical angle the camera can look.")]
    public float maxVerticalAngle = 60.0f; // Maximum vertical look angle

    // --- Jump Forgiveness Settings ---
    [Header("Jump Forgiveness Settings")]
    [Tooltip("The time window after leaving a ledge where the player can still jump.")]
    public float coyoteTime = 0.2f; // Time allowed to jump after leaving a ledge
    [Tooltip("The time window before landing where a jump input is buffered.")]
    public float jumpBufferTime = 0.2f; // Time to buffer jump input before landing

    // --- Multi-Jump Settings ---
    [Header("Multi-Jump Settings")]
    [Tooltip("The maximum number of jumps the player can perform before landing.")]
    public int maxJumps = 1; // Default to single jump

    // --- Internal Variables ---
    private CharacterController controller; // Reference to the CharacterController component
    private Vector3 playerVelocity; // Tracks the player's velocity for gravity and jumping
    private bool isGrounded; // Checks if the player is on the ground
    private float verticalRotation = 0.0f; // Tracks the vertical rotation of the camera

    // --- Input Variables ---
    private float horizontalInput; // Horizontal movement input (A/D or Left/Right arrows)
    private float verticalInput; // Vertical movement input (W/S or Up/Down arrows)
    private bool jumpPressed; // Tracks if the jump button was pressed
    private bool sprintPressed; // Tracks if the sprint key is being held

    // --- Internal Variables for Mouse Look ---
    private Vector2 currentMouseDelta; // Smoothed mouse delta
    private Vector2 currentMouseVelocity; // Velocity for smoothing

    // --- Internal Variables for Jump Forgiveness ---
    private float coyoteTimeCounter; // Tracks remaining coyote time
    private float jumpBufferCounter; // Tracks remaining jump buffer time

    // --- Variable Jump Settings ---
    [Header("Variable Jump Settings")]
    [Tooltip("Multiplier for gravity when the jump button is released early.")]
    public float lowJumpMultiplier = 2.0f; // Stronger gravity for short jumps
    [Tooltip("Multiplier for gravity during descent.")]
    public float fallMultiplier = 2.5f; // Stronger gravity for falling

    // --- Startup Settings ---
    [Header("Startup Settings")]
    [Tooltip("The delay in seconds before player controls are enabled after the game starts. This helps prevent inputs being stored before the game is ready.")]
    [Range(0, 5)]
    public float startupDelay = 1.0f; // Default delay of 1 second

    private bool controlsEnabled = false; // Tracks whether controls are enabled
    private int currentJumpCount = 0; // Tracks the number of jumps performed

    /// <summary>
    /// Initializes the CharacterController component and locks the cursor.
    /// </summary>
    private void Start()
    {
        controller = GetComponent<CharacterController>();

        // Zero out the mouse delta to prevent initial jump in view
        currentMouseDelta = Vector2.zero;
        currentMouseVelocity = Vector2.zero;
        verticalRotation = 0.0f;
        Camera.main.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

        // Start the delay timer to enable controls
        StartCoroutine(EnableControlsAfterDelay());
    }

    /// <summary>
    /// Coroutine to enable controls after the specified startup delay.
    /// </summary>
    private IEnumerator EnableControlsAfterDelay()
    {
        yield return new WaitForSeconds(startupDelay);
        controlsEnabled = true;
    }

    /// <summary>
    /// Called every frame to handle input, movement, jumping, and audio.
    /// </summary>
    void Update()
    {
        if (!controlsEnabled) return; // Skip input handling if controls are disabled

        // Check if the player is dead
        Health healthComponent = GetComponent<Health>();
        if (healthComponent != null && healthComponent.CurrentHealth <= 0)
        {
            return; // Stop all movement and interactions if the player is dead
        }

        HandleInput(); // Polls player input
        HandleMovementAndJump(); // Handles all movement in one integrated method
        HandleMouseLook(); // Handles mouse look
        HandleAudio(); // Placeholder for audio logic
    }

    /// <summary>
    /// Reads and stores player input values.
    /// </summary>
    private void HandleInput()
    {
        horizontalInput = Input.GetAxis("Horizontal"); // Left/Right movement
        verticalInput = Input.GetAxis("Vertical"); // Forward/Backward movement
        jumpPressed = Input.GetButtonDown("Jump"); // Jump button press
        sprintPressed = Input.GetKey(KeyCode.LeftShift); // Sprint key hold
    }

    /// <summary>
    /// Calculates the initial velocity required to reach the specified jump height under custom gravity.
    /// </summary>
    /// <param name="jumpHeight">The desired jump height.</param>
    /// <param name="gravity">The base gravity value.</param>
    /// <param name="fallMultiplier">The multiplier for gravity during descent.</param>
    /// <returns>The required initial velocity.</returns>
    private float CalculateJumpVelocity(float jumpHeight, float gravity, float fallMultiplier)
    {
        // Adjust gravity to account for the fall multiplier
        float adjustedGravity = gravity * fallMultiplier;

        // Use the kinematic equation: v^2 = u^2 + 2as (where u = 0 at the peak)
        // Rearrange to solve for initial velocity (v): v = sqrt(2 * -adjustedGravity * jumpHeight)
        return Mathf.Sqrt(2 * -adjustedGravity * jumpHeight);
    }

    /// <summary>
    /// Handles all movement including horizontal movement, jumping, and gravity in one integrated method.
    /// </summary>
    private void HandleMovementAndJump()
    {
        // Check grounded state FIRST, before any movement
        isGrounded = controller.isGrounded;

        // Reset jump count if grounded
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            currentJumpCount = 0; // Reset jump count when grounded
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // Update jump buffer counter
        if (jumpPressed)
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // Handle jumping with coyote time, jump buffering, and multi-jump logic
        if (jumpBufferCounter > 0 && (coyoteTimeCounter > 0 || currentJumpCount < maxJumps))
        {
            // Calculate the required initial velocity for the jump
            playerVelocity.y = CalculateJumpVelocity(jumpHeight, gravity, fallMultiplier);
            jumpBufferCounter = 0; // Consume the buffered jump
            currentJumpCount++; // Increment jump count
        }

        // Apply custom gravity curves
        if (playerVelocity.y > 0 && !jumpPressed)
        {
            // Apply stronger gravity for short jumps
            playerVelocity.y += gravity * (lowJumpMultiplier - 1) * Time.deltaTime;
        }
        else if (playerVelocity.y < 0)
        {
            // Apply stronger gravity for falling
            playerVelocity.y += gravity * (fallMultiplier - 1) * Time.deltaTime;
        }

        // Reset vertical velocity if grounded and falling (but not if we just jumped)
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f; // Small downward force to keep grounded
        }

        // Apply gravity to vertical velocity
        playerVelocity.y += gravity * Time.deltaTime;

        // Calculate horizontal movement direction based on input
        Vector3 horizontalMove = transform.right * horizontalInput + transform.forward * verticalInput;

        // Determine movement speed (sprint or walk)
        float currentSpeed = sprintPressed ? sprintSpeed : walkSpeed;

        // Combine horizontal and vertical movement
        Vector3 totalMovement = (horizontalMove * currentSpeed * Time.deltaTime) + (playerVelocity * Time.deltaTime);

        // Apply all movement to the CharacterController in one call
        controller.Move(totalMovement);
    }

    /// <summary>
    /// Handles mouse look for rotating the camera and player with smoothing.
    /// </summary>
    private void HandleMouseLook()
    {
        // Get raw mouse input
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivityX * Time.deltaTime;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivityY * Time.deltaTime;

        // Smooth the mouse input
        Vector2 targetMouseDelta = new Vector2(mouseX, mouseY);
        currentMouseDelta = Vector2.SmoothDamp(currentMouseDelta, targetMouseDelta, ref currentMouseVelocity, mouseSmoothing);

        // Adjust vertical rotation and clamp it
        verticalRotation -= currentMouseDelta.y;
        verticalRotation = Mathf.Clamp(verticalRotation, minVerticalAngle, maxVerticalAngle);

        // Apply vertical rotation to the camera
        Camera.main.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

        // Rotate the player horizontally
        transform.Rotate(Vector3.up * currentMouseDelta.x);
    }

    /// <summary>
    /// Placeholder for handling audio logic (e.g., footsteps, jump sounds).
    /// </summary>
    private void HandleAudio()
    {
        // Placeholder for audio handling logic
    }

    private void OnEnable()
    {
        EventManager.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        EventManager.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void HandleGameStateChanged(GameManager.GameState newState)
    {
        if (newState == GameManager.GameState.GAME_OVER)
        {
            controlsEnabled = false;
        }
        else if (newState == GameManager.GameState.IN_GAME)
        {
            controlsEnabled = true;
        }
        else if (newState == GameManager.GameState.PAUSED)
        {
            // Reset mouse smoothing variables to prevent momentum carryover
            currentMouseDelta = Vector2.zero;
            currentMouseVelocity = Vector2.zero;
        }
    }
}