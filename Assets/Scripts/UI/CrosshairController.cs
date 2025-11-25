using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the dynamic crosshair/reticle to visualize weapon bloom.
/// </summary>
public class CrosshairController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The RectTransform of the crosshair image.")]
    public RectTransform crosshairRect;

    [Header("Settings")]
    [Tooltip("Base size of the crosshair when bloom is 0.")]
    public float baseSize = 50f;

    [Tooltip("How much the crosshair expands per degree of bloom.")]
    public float bloomScaleFactor = 10f;

    [Tooltip("Smoothing speed for the crosshair animation.")]
    public float smoothingSpeed = 15f;

    private float currentTargetSize;

    private void OnEnable()
    {
        WeaponController.OnBloomChanged += HandleBloomChanged;
    }

    private void OnDisable()
    {
        WeaponController.OnBloomChanged -= HandleBloomChanged;
    }

    private void Start()
    {
        if (crosshairRect == null)
        {
            crosshairRect = GetComponent<RectTransform>();
        }
        currentTargetSize = baseSize;
    }

    private void Update()
    {
        if (crosshairRect != null)
        {
            float currentSize = crosshairRect.sizeDelta.x;
            float newSize = Mathf.Lerp(currentSize, currentTargetSize, Time.deltaTime * smoothingSpeed);
            crosshairRect.sizeDelta = new Vector2(newSize, newSize);
        }
    }

    private void HandleBloomChanged(float currentBloom, float maxBloom)
    {
        currentTargetSize = baseSize + (currentBloom * bloomScaleFactor);
    }
}
