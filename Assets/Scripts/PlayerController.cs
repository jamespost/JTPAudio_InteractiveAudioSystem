using UnityEngine;

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
    public float mouseSensitivityX = 300.0f; // Horizontal sensitivity multiplier
    [Tooltip("The sensitivity of the mouse movement on the Y axis.")]
    public float mouseSensitivityY = 300.0f; // Vertical sensitivity multiplier
    [Tooltip("The smoothing factor for mouse movement.")]
    public float mouseSmoothing = 0.05f; // Smoothing factor for mouse movement
    [Tooltip("The minimum vertical angle the camera can look.")]
    public float minVerticalAngle = -60.0f; // Minimum vertical look angle
    [Tooltip("The maximum vertical angle the camera can look.")]
    public float maxVerticalAngle = 60.0f; // Maximum vertical look angle

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



    /// <summary>
    /// Initializes the CharacterController component and locks the cursor.
    /// </summary>
    private void Start()
    {
        controller = GetComponent<CharacterController>();

        // Lock and hide the cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// Called every frame to handle input, movement, jumping, and audio.
    /// </summary>
    void Update()
    {
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
    /// Handles all movement including horizontal movement, jumping, and gravity in one integrated method.
    /// </summary>
    private void HandleMovementAndJump()
    {
        // Check grounded state FIRST, before any movement
        isGrounded = controller.isGrounded;

        // Handle jumping BEFORE horizontal movement to ensure grounded state is accurate
        if (jumpPressed && isGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
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
}