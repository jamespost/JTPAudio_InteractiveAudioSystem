using UnityEngine;

public class RecoilController : MonoBehaviour
{
    [Header("References")]
    public PlayerController playerController;
    public Transform cameraTransform;

    // Rotation State
    private Vector3 currentRotation;
    private Vector3 targetRotation;
    
    // Position State
    private Vector3 currentPosition;
    private Vector3 targetPosition;
    private Vector3 initialCameraPosition;

    // Settings applied per shot
    private float snappiness;
    private float returnSpeed;

    private void Start()
    {
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }
        
        if (cameraTransform == null)
        {
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }
        
        if (cameraTransform != null)
        {
            initialCameraPosition = cameraTransform.localPosition;
        }
    }

    private void Update()
    {
        // Rotation
        targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, returnSpeed * Time.deltaTime);
        currentRotation = Vector3.Slerp(currentRotation, targetRotation, snappiness * Time.deltaTime);
        
        if (playerController != null)
        {
            playerController.recoilOffsetRotation = currentRotation;
        }

        // Position (Kickback)
        targetPosition = Vector3.Lerp(targetPosition, Vector3.zero, returnSpeed * Time.deltaTime);
        currentPosition = Vector3.Slerp(currentPosition, targetPosition, snappiness * Time.deltaTime);
        
        if (cameraTransform != null)
        {
            cameraTransform.localPosition = initialCameraPosition + currentPosition;
        }
    }

    public void RecoilFire(float recoilX, float recoilY, float recoilZ, float snap, float ret)
    {
        // Rotation: X is Pitch (Up), Y is Yaw (Side)
        // We use recoilX for Pitch Up (-X)
        // We use recoilY for Random Yaw
        // We add a small random roll based on Y for extra feel
        targetRotation += new Vector3(-recoilX, Random.Range(-recoilY, recoilY), Random.Range(-recoilY, recoilY) * 0.5f);
        
        // Position: Z is Kickback (-Z)
        targetPosition += new Vector3(0, 0, -recoilZ);
        
        snappiness = snap;
        returnSpeed = ret;
    }
}
