using UnityEngine;

public class WeaponSway : MonoBehaviour
{
    [Header("References")]
    public Transform swayTransform;

    private WeaponData weaponData;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private float inputX;
    private float inputY;
    private float movementX;
    private float movementY;

    // Vertical Sway State
    private Vector3 currentVerticalSwayPos;
    private Vector3 currentVerticalSwayRot;
    
    // Sprint Sway State
    private Vector3 currentSprintSwayPos;
    private Vector3 currentSprintSwayRot;
    
    private PlayerController playerController;
    private PlayerMovementFeedback movementFeedback;

    private bool isInitialized = false;

    private void Start()
    {
        if (swayTransform == null)
        {
            swayTransform = transform;
        }
        initialPosition = swayTransform.localPosition;
        initialRotation = swayTransform.localRotation;

        // Find PlayerController
        playerController = GetComponentInParent<PlayerController>();
        if (playerController != null)
        {
            playerController.OnJump += OnJump;
            playerController.OnLand += OnLand;
            playerController.OnSprintStart += OnSprintStart;
            playerController.OnSprintEnd += OnSprintEnd;
            
            // Find Feedback
            movementFeedback = playerController.GetComponent<PlayerMovementFeedback>();
        }
    }

    private void OnDestroy()
    {
        if (playerController != null)
        {
            playerController.OnJump -= OnJump;
            playerController.OnLand -= OnLand;
            playerController.OnSprintStart -= OnSprintStart;
            playerController.OnSprintEnd -= OnSprintEnd;
        }
    }

    private void OnJump()
    {
        if (weaponData == null) return;
        currentVerticalSwayPos += weaponData.jumpSwayPosition;
        currentVerticalSwayRot += weaponData.jumpSwayRotation;
    }

    private void OnLand(float verticalVelocity)
    {
        if (weaponData == null) return;
        
        // Scale impact by velocity (velocity is negative on impact)
        float impactFactor = Mathf.Clamp01(Mathf.Abs(verticalVelocity) * weaponData.landSwayMultiplier);
        
        currentVerticalSwayPos += weaponData.landSwayPosition * impactFactor;
        currentVerticalSwayRot += weaponData.landSwayRotation * impactFactor;
    }

    private void OnSprintStart()
    {
        if (weaponData == null) return;
        currentSprintSwayPos += weaponData.sprintStartSwayPosition;
        currentSprintSwayRot += weaponData.sprintStartSwayRotation;
    }

    private void OnSprintEnd()
    {
        if (weaponData == null) return;
        currentSprintSwayPos += weaponData.sprintEndSwayPosition;
        currentSprintSwayRot += weaponData.sprintEndSwayRotation;
    }

    public void Initialize(WeaponData data)
    {
        weaponData = data;
        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized || weaponData == null) return;

        CalculateSway();
    }

    private void CalculateSway()
    {
        // Get Input
        inputX = -Input.GetAxis("Mouse X");
        inputY = -Input.GetAxis("Mouse Y");
        
        movementX = -Input.GetAxis("Horizontal");
        movementY = -Input.GetAxis("Vertical");

        // Calculate Look Sway (Position)
        float moveX = Mathf.Clamp(inputX * weaponData.swayAmount, -weaponData.maxSwayAmount, weaponData.maxSwayAmount);
        float moveY = Mathf.Clamp(inputY * weaponData.swayAmount, -weaponData.maxSwayAmount, weaponData.maxSwayAmount);

        Vector3 finalSwayPosition = new Vector3(moveX, moveY, 0);

        // Calculate Look Sway (Rotation)
        float rotX = Mathf.Clamp(inputY * weaponData.swayRotationAmount, -weaponData.maxSwayRotation, weaponData.maxSwayRotation);
        float rotY = Mathf.Clamp(inputX * weaponData.swayRotationAmount, -weaponData.maxSwayRotation, weaponData.maxSwayRotation);

        Quaternion finalSwayRotation = Quaternion.Euler(new Vector3(rotX, rotY, rotY)); // Adding Z rotation for tilt

        // Calculate Movement Sway (Inertia)
        float moveSwayX = Mathf.Clamp(movementX * weaponData.movementSwayX, -weaponData.movementSwayX, weaponData.movementSwayX);
        float moveSwayY = Mathf.Clamp(movementY * weaponData.movementSwayY, -weaponData.movementSwayY, weaponData.movementSwayY);
        
        // Apply Sprint Multiplier
        if (playerController != null && playerController.IsSprinting)
        {
            moveSwayX *= weaponData.sprintSwayMultiplier;
            moveSwayY *= weaponData.sprintSwayMultiplier;
        }

        Vector3 finalMovementSway = new Vector3(moveSwayX, 0, moveSwayY);

        // Recover Vertical Sway
        currentVerticalSwayPos = Vector3.Lerp(currentVerticalSwayPos, Vector3.zero, Time.deltaTime * weaponData.verticalSwayRecoverySpeed);
        currentVerticalSwayRot = Vector3.Lerp(currentVerticalSwayRot, Vector3.zero, Time.deltaTime * weaponData.verticalSwayRecoverySpeed);

        // Recover Sprint Sway
        currentSprintSwayPos = Vector3.Lerp(currentSprintSwayPos, Vector3.zero, Time.deltaTime * weaponData.sprintSwayRecoverySpeed);
        currentSprintSwayRot = Vector3.Lerp(currentSprintSwayRot, Vector3.zero, Time.deltaTime * weaponData.sprintSwayRecoverySpeed);

        // Calculate Weapon Bob
        Vector3 bobPosition = Vector3.zero;
        Quaternion bobRotation = Quaternion.identity;

        if (movementFeedback != null && movementFeedback.CurrentCycle > 0)
        {
            float cycle = movementFeedback.CurrentCycle * weaponData.bobFrequencyMultiplier;
            float bobFactor = 1f;
            
            if (playerController != null && playerController.IsSprinting)
            {
                bobFactor = weaponData.sprintBobMultiplier;
            }

            // Figure-8 pattern
            // X = Cos(t), Y = Sin(2t)
            float xBob = Mathf.Cos(cycle) * weaponData.bobPositionAmount.x * bobFactor;
            float yBob = Mathf.Sin(cycle * 2) * weaponData.bobPositionAmount.y * bobFactor;
            
            bobPosition = new Vector3(xBob, yBob, 0);

            // Rotation Bob
            float xRotBob = Mathf.Sin(cycle * 2) * weaponData.bobRotationAmount.x * bobFactor;
            float yRotBob = Mathf.Cos(cycle) * weaponData.bobRotationAmount.y * bobFactor;
            float zRotBob = Mathf.Cos(cycle) * weaponData.bobRotationAmount.z * bobFactor;

            bobRotation = Quaternion.Euler(xRotBob, yRotBob, zRotBob);
        }

        // Apply Position Sway
        swayTransform.localPosition = Vector3.Lerp(swayTransform.localPosition, initialPosition + finalSwayPosition + finalMovementSway + currentVerticalSwayPos + currentSprintSwayPos + bobPosition, Time.deltaTime * weaponData.swaySmoothness);

        // Apply Rotation Sway
        swayTransform.localRotation = Quaternion.Slerp(swayTransform.localRotation, initialRotation * finalSwayRotation * Quaternion.Euler(currentVerticalSwayRot) * Quaternion.Euler(currentSprintSwayRot) * bobRotation, Time.deltaTime * weaponData.swayRotationSmoothness);
    }
}
