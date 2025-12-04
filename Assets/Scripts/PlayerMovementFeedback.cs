using UnityEngine;

public class PlayerMovementFeedback : MonoBehaviour
{
    [Header("References")]
    public PlayerController playerController;
    public Transform cameraTransform;

    [Header("Head Bob Settings")]
    public bool enableHeadBob = true;
    [Tooltip("Base frequency for the bob cycle.")]
    public float bobFrequency = 10f;
    [Tooltip("Base amplitude for the vertical bob.")]
    public float bobVerticalAmplitude = 0.05f;
    [Tooltip("Base amplitude for the horizontal bob.")]
    public float bobHorizontalAmplitude = 0.03f;
    
    [Header("Sprint Bob Multipliers")]
    public float sprintFrequencyMultiplier = 1.5f;
    public float sprintAmplitudeMultiplier = 1.2f;

    [Header("Events")]
    public System.Action<bool> OnFootstep; // true = right, false = left

    // State
    private float cyclePosition = 0;
    private Vector3 originalCameraPosition;
    private float currentBobFactor = 0; // Smoothly transitions 0->1 based on movement

    public float CurrentCycle => cyclePosition;

    private void Start()
    {
        if (playerController == null)
            playerController = GetComponent<PlayerController>();
        
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        if (cameraTransform != null)
            originalCameraPosition = cameraTransform.localPosition;
    }

    private void Update()
    {
        if (playerController == null) return;

        HandleBob();
    }

    private void HandleBob()
    {
        Vector3 velocity = playerController.Velocity;
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
        float speed = horizontalVelocity.magnitude;

        // Determine target bob factor (0 if idle/airborne, 1 if moving)
        float targetBobFactor = (speed > 0.1f && playerController.IsGrounded) ? 1f : 0f;
        currentBobFactor = Mathf.Lerp(currentBobFactor, targetBobFactor, Time.deltaTime * 5f);

        if (currentBobFactor > 0.01f)
        {
            // Calculate Frequency
            float frequency = bobFrequency;
            float amplitudeV = bobVerticalAmplitude;
            float amplitudeH = bobHorizontalAmplitude;

            if (playerController.IsSprinting)
            {
                frequency *= sprintFrequencyMultiplier;
                amplitudeV *= sprintAmplitudeMultiplier;
                amplitudeH *= sprintAmplitudeMultiplier;
            }

            // Advance Cycle
            // We use speed to drive the cycle so it syncs with movement
            // But we clamp the speed multiplier so it doesn't go crazy or stop completely
            float speedMultiplier = Mathf.Clamp(speed / playerController.walkSpeed, 0.5f, 2f);
            float cycleIncrement = (frequency * speedMultiplier) * Time.deltaTime;
            
            float previousCycle = cyclePosition;
            cyclePosition += cycleIncrement;

            // Wrap cycle (0 to 2PI)
            if (cyclePosition > Mathf.PI * 2)
            {
                cyclePosition -= Mathf.PI * 2;
            }

            // Detect Footsteps
            // We assume a full cycle (2PI) is two steps (Left -> Right)
            // Step 1 (Left) at PI/2 (Peak of Sin? No, usually trough is impact)
            // Let's say Sin wave: -1 is impact.
            // Sin(3PI/2) = -1.
            // But we have two steps.
            // Let's use Sin for vertical bob. It dips twice per cycle?
            // Standard: Vertical = Sin(2*time). Horizontal = Cos(time).
            // If Vertical = Sin(2*cycle), it dips at 3PI/4 and 7PI/4.
            // Let's simplify:
            // Cycle 0..PI is Left Step. PI..2PI is Right Step.
            // Impact at PI/2 and 3PI/2?
            
            // Let's trigger events when passing PI and 2PI (or 0)
            if (previousCycle < Mathf.PI && cyclePosition >= Mathf.PI)
            {
                OnFootstep?.Invoke(true); // Right Foot
            }
            else if (previousCycle > Mathf.PI && cyclePosition < previousCycle) // Wrapped
            {
                OnFootstep?.Invoke(false); // Left Foot
            }

            // Apply Head Bob
            if (enableHeadBob && cameraTransform != null)
            {
                // Vertical: Dips twice per cycle (Left step, Right step)
                // Sin(2 * cycle) goes 0 -> 1 -> 0 -> -1 -> 0 in PI.
                // We want it to be high at mid-step, low at impact.
                // If impact is at PI and 2PI:
                // Sin(2 * PI) = 0.
                // We want -1 at PI.
                // Sin(2 * cycle + PI/2)?
                // Let's just use Mathf.Sin(2 * cyclePosition) and see.
                
                float yOffset = Mathf.Sin(2 * cyclePosition) * amplitudeV * currentBobFactor;
                float xOffset = Mathf.Cos(cyclePosition) * amplitudeH * currentBobFactor;

                cameraTransform.localPosition = originalCameraPosition + new Vector3(xOffset, yOffset, 0);
            }
        }
        else
        {
            // Reset to neutral
            cyclePosition = 0;
            if (enableHeadBob && cameraTransform != null)
            {
                cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, originalCameraPosition, Time.deltaTime * 5f);
            }
        }
    }
}
