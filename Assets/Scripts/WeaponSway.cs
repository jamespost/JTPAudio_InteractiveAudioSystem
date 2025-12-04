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

    // ADS State
    private bool isAiming = false;
    private Vector3 currentBasePosition;
    private Quaternion currentBaseRotation;

    // Smoothed Sway State
    private Vector3 currentSwayPosition;
    private Quaternion currentSwayRotation;

    private bool isInitialized = false;

    private void Start()
    {
        if (swayTransform == null)
        {
            swayTransform = transform;
        }
        initialPosition = swayTransform.localPosition;
        initialRotation = swayTransform.localRotation;
        
        currentBasePosition = initialPosition;
        currentBaseRotation = initialRotation;
        
        currentSwayRotation = Quaternion.identity;

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

    public void SetAiming(bool aiming)
    {
        isAiming = aiming;
    }

    private void Update()
    {
        if (!isInitialized || weaponData == null) return;

        CalculateSway();
    }

    private void CalculateSway()
    {
        // Handle ADS Transition
        Vector3 targetPos = isAiming ? weaponData.adsPosition : initialPosition;
        Quaternion targetRot = isAiming ? Quaternion.Euler(weaponData.adsRotation) : initialRotation;
        
        currentBasePosition = Vector3.Lerp(currentBasePosition, targetPos, Time.deltaTime * weaponData.adsSpeed);
        currentBaseRotation = Quaternion.Slerp(currentBaseRotation, targetRot, Time.deltaTime * weaponData.adsSpeed);

        // Get Input
        inputX = -Input.GetAxis("Mouse X");
        inputY = -Input.GetAxis("Mouse Y");
        
        movementX = -Input.GetAxis("Horizontal");
        movementY = -Input.GetAxis("Vertical");

        // Calculate Multipliers
        float swayMult = isAiming ? weaponData.adsSwayMultiplier : 1f;

        // Calculate Look Sway (Position)
        float moveX = Mathf.Clamp(inputX * weaponData.swayAmount * swayMult, -weaponData.maxSwayAmount, weaponData.maxSwayAmount);
        float moveY = Mathf.Clamp(inputY * weaponData.swayAmount * swayMult, -weaponData.maxSwayAmount, weaponData.maxSwayAmount);

        Vector3 finalSwayPosition = new Vector3(moveX, moveY, 0);

        // Calculate Look Sway (Rotation)
        float rotX = Mathf.Clamp(inputY * weaponData.swayRotationAmount * swayMult, -weaponData.maxSwayRotation, weaponData.maxSwayRotation);
        float rotY = Mathf.Clamp(inputX * weaponData.swayRotationAmount * swayMult, -weaponData.maxSwayRotation, weaponData.maxSwayRotation);

        Quaternion finalSwayRotation = Quaternion.Euler(new Vector3(rotX, rotY, rotY)); // Adding Z rotation for tilt

        // Calculate Movement Sway (Inertia)
        float moveSwayX = Mathf.Clamp(movementX * weaponData.movementSwayX * swayMult, -weaponData.movementSwayX, weaponData.movementSwayX);
        float moveSwayY = Mathf.Clamp(movementY * weaponData.movementSwayY * swayMult, -weaponData.movementSwayY, weaponData.movementSwayY);
        
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
            
            // Reduce bob when aiming
            if (isAiming) bobFactor *= 0.1f;

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

        // Smooth the Sway independently
        Vector3 targetSwayPos = finalSwayPosition + finalMovementSway + currentVerticalSwayPos + currentSprintSwayPos + bobPosition;
        Quaternion targetSwayRot = finalSwayRotation * Quaternion.Euler(currentVerticalSwayRot) * Quaternion.Euler(currentSprintSwayRot) * bobRotation;

        currentSwayPosition = Vector3.Lerp(currentSwayPosition, targetSwayPos, Time.deltaTime * weaponData.swaySmoothness);
        currentSwayRotation = Quaternion.Slerp(currentSwayRotation, targetSwayRot, Time.deltaTime * weaponData.swayRotationSmoothness);

        // Apply Final Position (Base + Sway)
        swayTransform.localPosition = currentBasePosition + currentSwayPosition;
        swayTransform.localRotation = currentBaseRotation * currentSwayRotation;
    }
}
