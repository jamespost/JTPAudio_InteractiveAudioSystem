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

    // --- Internal Variables ---
    private CharacterController controller; // Reference to the CharacterController component
    private Vector3 playerVelocity; // Tracks the player's velocity for gravity and jumping
    private bool isGrounded; // Checks if the player is on the ground

    // --- Input Variables ---
    private float horizontalInput; // Horizontal movement input (A/D or Left/Right arrows)
    private float verticalInput; // Vertical movement input (W/S or Up/Down arrows)
    private bool jumpPressed; // Tracks if the jump button was pressed
    private bool sprintPressed; // Tracks if the sprint key is being held

    /// <summary>
    /// Initializes the CharacterController component.
    /// </summary>
    private void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    /// <summary>
    /// Called every frame to handle input, movement, jumping, and audio.
    /// </summary>
    void Update()
    {
        HandleInput(); // Polls player input
        HandleMovement(); // Handles horizontal movement
        HandleJump(); // Handles jumping and gravity
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
    /// Handles player movement based on input and applies it to the CharacterController.
    /// </summary>
    private void HandleMovement()
    {
        isGrounded = controller.isGrounded; // Check if the player is grounded

        // Reset vertical velocity if grounded
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = 0f;
        }

        // Calculate movement direction based on input
        Vector3 move = transform.right * horizontalInput + transform.forward * verticalInput;

        // Determine movement speed (sprint or walk)
        float currentSpeed = sprintPressed ? sprintSpeed : walkSpeed;

        // Apply movement to the CharacterController
        controller.Move(move * currentSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Handles jumping and applies gravity to the player.
    /// </summary>
    private void HandleJump()
    {
        // Apply jump force if jump is pressed and player is grounded
        if (jumpPressed && isGrounded)
        {
            playerVelocity.y += Mathf.Sqrt(jumpHeight * -3.0f * gravity);
        }

        // Apply gravity to vertical velocity
        playerVelocity.y += gravity * Time.deltaTime;

        // Apply vertical movement to the CharacterController
        controller.Move(playerVelocity * Time.deltaTime);
    }

    /// <summary>
    /// Placeholder for handling audio logic (e.g., footsteps, jump sounds).
    /// </summary>
    private void HandleAudio()
    {
        // Placeholder for audio handling logic
    }
}