using UnityEngine;

public class WeaponRecoil : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The transform to apply recoil to. If null, uses this transform.")]
    public Transform recoilTransform;

    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private Vector3 targetPosition;
    private Vector3 currentPosition;

    private Vector3 targetRotation;
    private Vector3 currentRotation;

    private float snappiness;
    private float returnSpeed;

    private bool isInitialized = false;
    public bool enableRecoil = true;

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (recoilTransform == null)
        {
            recoilTransform = transform;
        }

        originalPosition = recoilTransform.localPosition;
        originalRotation = recoilTransform.localRotation;
        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized || !enableRecoil) return;

        targetPosition = Vector3.Lerp(targetPosition, Vector3.zero, returnSpeed * Time.deltaTime);
        currentPosition = Vector3.Lerp(currentPosition, targetPosition, snappiness * Time.deltaTime);

        targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, returnSpeed * Time.deltaTime);
        currentRotation = Vector3.Slerp(currentRotation, targetRotation, snappiness * Time.deltaTime);

        recoilTransform.localPosition = originalPosition + currentPosition;
        recoilTransform.localRotation = originalRotation * Quaternion.Euler(currentRotation);
    }

    public void Fire(Vector3 kickback, Vector3 rotationRecoil, float snap, float retSpeed)
    {
        if (!enableRecoil) return;

        targetPosition += kickback;
        targetRotation += rotationRecoil;
        snappiness = snap;
        returnSpeed = retSpeed;
    }
    
    public void ResetRecoil()
    {
        targetPosition = Vector3.zero;
        targetRotation = Vector3.zero;
        currentPosition = Vector3.zero;
        currentRotation = Vector3.zero;
        
        if (recoilTransform != null)
        {
            recoilTransform.localPosition = originalPosition;
            recoilTransform.localRotation = originalRotation;
        }
    }
}
