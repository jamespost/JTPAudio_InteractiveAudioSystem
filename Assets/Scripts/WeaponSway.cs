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
    private PlayerController playerController;

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
        }
    }

    private void OnDestroy()
    {
        if (playerController != null)
        {
            playerController.OnJump -= OnJump;
            playerController.OnLand -= OnLand;
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
        
        Vector3 finalMovementSway = new Vector3(moveSwayX, 0, moveSwayY);

        // Recover Vertical Sway
        currentVerticalSwayPos = Vector3.Lerp(currentVerticalSwayPos, Vector3.zero, Time.deltaTime * weaponData.verticalSwayRecoverySpeed);
        currentVerticalSwayRot = Vector3.Lerp(currentVerticalSwayRot, Vector3.zero, Time.deltaTime * weaponData.verticalSwayRecoverySpeed);

        // Apply Position Sway
        swayTransform.localPosition = Vector3.Lerp(swayTransform.localPosition, initialPosition + finalSwayPosition + finalMovementSway + currentVerticalSwayPos, Time.deltaTime * weaponData.swaySmoothness);

        // Apply Rotation Sway
        swayTransform.localRotation = Quaternion.Slerp(swayTransform.localRotation, initialRotation * finalSwayRotation * Quaternion.Euler(currentVerticalSwayRot), Time.deltaTime * weaponData.swayRotationSmoothness);
    }
}
