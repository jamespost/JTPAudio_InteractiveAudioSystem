using UnityEngine;
using System.Collections;
using UnityEngine.UI; // Added for UI components

/// <summary>
/// Handles weapon functionality including firing, reloading, and event-driven interactions.
/// 
/// Features:
/// - Color-coded ammo display that changes based on ammo percentage
/// - Pulse effect when attempting to fire without ammo
/// - Increased UI size when ammo is empty
/// - Weapon rotation animation during reload
/// - Audio event placeholders for enhanced feedback
/// - Designer-friendly inspector controls with tooltips
/// </summary>
public class WeaponController : MonoBehaviour
{
    [Header("Weapon Data")]
    [Tooltip("The data asset containing weapon stats.")]
    public WeaponData weaponData;

    [Header("References")]
    [Tooltip("The transform where bullets or effects originate.")]
    public Transform firePoint;

    [Header("Feedback Settings")]
    [Tooltip("Impact VFX prefab for visual feedback.")]
    public GameObject impactVFX;

    [Tooltip("LayerMask for hit detection.")]
    public LayerMask hitLayers;

    [Header("UI Settings")]
    [Tooltip("Enable ammo UI display.")]
    public bool enableAmmoUIDisplay = false;

    [Tooltip("Optional transform to specify where the ammo UI should display.")]
    public Transform ammoUITransform;

    [Tooltip("Adjust the size of the ammo UI.")]
    [Range(0.1f, 5f)]
    public float ammoUISize = 1f;

    [Header("Ammo UI Color Settings")]
    [Tooltip("Color when ammo is full (75-100% of clip size).")]
    public Color ammoColorFull = Color.green;

    [Tooltip("Color when ammo is medium (25-75% of clip size).")]
    public Color ammoColorMedium = Color.yellow;

    [Tooltip("Color when ammo is low (1-25% of clip size).")]
    public Color ammoColorLow = Color.red;

    [Tooltip("Color when ammo is empty (0% of clip size).")]
    public Color ammoColorEmpty = Color.red;

    [Header("Ammo UI Effects")]
    [Tooltip("Size multiplier when ammo is empty.")]
    [Range(1f, 3f)]
    public float emptyAmmoSizeMultiplier = 1.5f;

    [Tooltip("Duration of the pulse effect when trying to shoot without ammo.")]
    [Range(0.1f, 1f)]
    public float pulseDuration = 0.3f;

    [Tooltip("How much the pulse effect scales the UI.")]
    [Range(1.1f, 2f)]
    public float pulseScale = 1.3f;

    [Header("Reload Animation")]
    [Tooltip("Enable weapon rotation animation during reload.")]
    public bool enableReloadAnimation = true;

    [Tooltip("Rotation angle on X-axis during reload (in degrees).")]
    [Range(0f, 45f)]
    public float reloadRotationAngle = 15f;

    [Tooltip("Speed of the reload rotation animation.")]
    [Range(0.1f, 5f)]
    public float reloadAnimationSpeed = 2f;

    [Header("Audio Events")]
    [Tooltip("Audio event for low ammo warning (placeholder).")]
    public string lowAmmoAudioEvent = "LowAmmoWarning";

    [Tooltip("Audio event for empty ammo pulse feedback (placeholder).")]
    public string emptyAmmoPulseAudioEvent = "EmptyAmmoPulse";

    [Tooltip("Audio event for ammo color change feedback (placeholder).")]
    public string ammoColorChangeAudioEvent = "AmmoColorChange";

    [Header("Debug Settings")]
    [Tooltip("Enable debug mode to visualize hit points and log additional information.")]
    public bool debugMode = false;

    // --- Static Events for UI System ---
    /// <summary>
    /// Static event that fires when ammo changes.
    /// Parameters: currentAmmo, maxAmmo
    /// </summary>
    public static event System.Action<int, int> OnAmmoChanged;

    private int currentAmmo;
    private bool isReloading;
    private float nextFireTime;
    private ObjectPooler vfxPooler;
    private Camera mainCamera;

    private GameObject ammoDebugTextObject;
    private Text ammoDebugTextMesh;

    // Animation and effect variables
    private Vector3 originalWeaponRotation;
    private bool isRotatingForReload = false;
    private Coroutine reloadAnimationCoroutine;
    private Coroutine pulseFeedbackCoroutine;
    private Vector3 originalAmmoUIScale;
    private Color lastAmmoColor;
    private int lastAmmoThreshold = -1;

    [Header("Reload Animation")]
    [Tooltip("Transform to rotate during reload (e.g., gun model).")]
    public Transform reloadRotationTransform;

    private void Awake()
    {
        // Initialize Object Pooler for VFX
        vfxPooler = FindObjectOfType<ObjectPooler>();
        if (vfxPooler == null)
        {
            Debug.LogWarning("[WeaponController] ObjectPooler not found in the scene. VFX pooling may not work as expected.");
        }

        // Cache the main camera
        mainCamera = Camera.main;

        // Store original weapon rotation
        originalWeaponRotation = transform.eulerAngles;

        // Default firePoint to the position of the camera if not assigned
        if (firePoint == null)
        {
            GameObject defaultFirePoint = new GameObject("DefaultFirePoint");
            defaultFirePoint.transform.SetParent(mainCamera.transform);
            defaultFirePoint.transform.localPosition = new Vector3(0, 0, 0); // Position of the camera
            firePoint = defaultFirePoint.transform;
        }
    }

    private void Start()
    {
        currentAmmo = weaponData.clipSize;

        // Fire initial ammo event
        OnAmmoChanged?.Invoke(currentAmmo, weaponData.clipSize);

        if (enableAmmoUIDisplay)
        {
            // Create a new GameObject for the ammo debug text
            ammoDebugTextObject = new GameObject("AmmoDebugCanvas");

            // Use the specified transform or default to the weapon's transform
            Transform parentTransform = ammoUITransform != null ? ammoUITransform : transform;
            ammoDebugTextObject.transform.SetParent(parentTransform);
            ammoDebugTextObject.transform.localPosition = new Vector3(0, 2, 0); // Position above the weapon

            // Add a Canvas component for UI rendering
            Canvas canvas = ammoDebugTextObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            CanvasScaler canvasScaler = ammoDebugTextObject.AddComponent<CanvasScaler>();
            canvasScaler.dynamicPixelsPerUnit = 100f; // Adjust for crispness

            // Add a Text component for displaying ammo count
            GameObject textObject = new GameObject("AmmoText");
            textObject.transform.SetParent(ammoDebugTextObject.transform);
            Text text = textObject.AddComponent<Text>();
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.yellow; // Default color
            text.fontSize = 32;

            // Load the ShareTechMono-Regular font from the Fonts folder
            Font shareTechMonoFont = Resources.Load<Font>("Fonts/ShareTechMono-Regular");
            if (shareTechMonoFont != null)
            {
                text.font = shareTechMonoFont;
            }
            else
            {
                Debug.LogError("ShareTechMono-Regular font not found in Resources/Fonts folder.");
            }

            // Adjust RectTransform for proper scaling
            RectTransform rectTransform = text.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, 50); // Keep sizeDelta fixed
            rectTransform.localPosition = Vector3.zero;

            ammoDebugTextObject.transform.localScale = Vector3.one * ammoUISize;

            ammoDebugTextMesh = text;
            originalAmmoUIScale = ammoDebugTextObject.transform.localScale;
            lastAmmoColor = GetAmmoColor(currentAmmo);
            ammoDebugTextMesh.color = lastAmmoColor;
        }
    }

    private void Update()
    {
        HandleInput();
        UpdateAmmoUI();
    }

    private void HandleInput()
    {
        if (Input.GetButtonDown("Fire1") && Time.time >= nextFireTime && !isReloading)
        {
            Fire();
        }

        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < weaponData.clipSize && !isReloading)
        {
            StartCoroutine(Reload());
        }
    }

    private void Fire()
    {
        if (currentAmmo <= 0)
        {
            Debug.Log("Out of ammo!");

            // Pulse the ammo UI in red
            if (pulseFeedbackCoroutine != null)
            {
                StopCoroutine(pulseFeedbackCoroutine);
            }
            pulseFeedbackCoroutine = StartCoroutine(PulseAmmoUI());

            // Play out of ammo sound
            if (weaponData.outOfAmmoSound != null)
            {
                AudioManager.Instance.PostEvent(weaponData.outOfAmmoSound.eventID, this.gameObject);
            }

            // Placeholder for empty ammo pulse audio feedback
            // AudioManager.Instance.PostEvent(emptyAmmoPulseAudioEvent, gameObject);

            return;
        }

        currentAmmo--;
        nextFireTime = Time.time + weaponData.fireRate;

        // Fire ammo changed event
        OnAmmoChanged?.Invoke(currentAmmo, weaponData.clipSize);

        // Trigger weapon fire event
        EventManager.TriggerWeaponFired();

        // Play fire sound
        if (weaponData.fireSound != null)
        {
            AudioManager.Instance.PostEvent(weaponData.fireSound.eventID, this.gameObject);
        }

        // Raycast for hit detection
        RaycastHit hit;
        if (Physics.Raycast(firePoint.position, firePoint.forward, out hit, weaponData.range, hitLayers))
        {
            // Log hit information
            if (debugMode)
            {
                Debug.Log($"[WeaponController] Hit object: {hit.collider.name} at position: {hit.point}");
            }

            // Apply damage if the hit object has a Health component
            Health health = hit.collider.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(weaponData.damage);
            }

            // Spawn impact VFX
            if (impactVFX != null && vfxPooler != null)
            {
                GameObject vfx = vfxPooler.GetPooledObject(impactVFX);
                if (vfx != null)
                {
                    vfx.transform.position = hit.point;
                    vfx.transform.rotation = Quaternion.LookRotation(hit.normal);
                    vfx.SetActive(true);
                }
                else
                {
                    Debug.LogWarning("[WeaponController] No pooled object available for impact VFX. Check ObjectPooler configuration.");
                }
            }
            else if (impactVFX == null)
            {
                Debug.LogWarning("[WeaponController] Impact VFX prefab is not assigned. Visual feedback will not be shown.");
            }
            else if (vfxPooler == null)
            {
                Debug.LogWarning("[WeaponController] ObjectPooler is not initialized. VFX pooling will not work.");
            }

            // Play impact sound
            if (weaponData.impactSound != null)
            {
                AudioManager.Instance.PostEvent(weaponData.impactSound.eventID, hit.collider.gameObject);
            }

            // Draw debug gizmo if debug mode is enabled
            if (debugMode)
            {
                DebugDrawHitPoint(hit.point);
            }
        }
    }

    private IEnumerator Reload()
    {
        isReloading = true;

        // Trigger reload event
        EventManager.TriggerWeaponReloaded();

        // Play reload sound
        if (weaponData.reloadSound != null)
        {
            AudioManager.Instance.PostEvent(weaponData.reloadSound.eventID, gameObject);
        }

        // Start reload animation
        if (reloadAnimationCoroutine != null)
        {
            StopCoroutine(reloadAnimationCoroutine);
        }
        reloadAnimationCoroutine = StartCoroutine(AnimateReloadRotation());

        yield return new WaitForSeconds(weaponData.reloadSpeed);

        currentAmmo = weaponData.clipSize;
        isReloading = false;

        // Fire ammo changed event after reload
        OnAmmoChanged?.Invoke(currentAmmo, weaponData.clipSize);
    }

    private void DebugDrawHitPoint(Vector3 position)
    {
        GameObject debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        debugSphere.transform.position = position;
        debugSphere.transform.localScale = Vector3.one; // 1m sphere
        debugSphere.GetComponent<Collider>().enabled = false; // Disable collider
        Destroy(debugSphere, 2f); // Destroy after 2 seconds
    }

    private void OnEnable()
    {
        EventManager.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        EventManager.OnGameStateChanged -= HandleGameStateChanged;
        
        // Clean up any running coroutines
        if (reloadAnimationCoroutine != null)
        {
            StopCoroutine(reloadAnimationCoroutine);
            reloadAnimationCoroutine = null;
        }
        
        if (pulseFeedbackCoroutine != null)
        {
            StopCoroutine(pulseFeedbackCoroutine);
            pulseFeedbackCoroutine = null;
        }
    }

    private void HandleGameStateChanged(GameManager.GameState newState)
    {
    if (newState == GameManager.GameState.GAME_OVER)
    {
        enabled = false;
    }
    else if (newState == GameManager.GameState.IN_GAME)
    {
        enabled = true;
        // Re-initialize critical components to ensure weapon works after scene reload
        StartCoroutine(ReinitializeWeapon());
    }
}

private System.Collections.IEnumerator ReinitializeWeapon()
{
    yield return new WaitForEndOfFrame();
    
    // Re-cache the main camera if it's null
    if (mainCamera == null)
    {
        mainCamera = Camera.main;
    }

    // Re-find the VFX pooler if it's null
    if (vfxPooler == null)
    {
        vfxPooler = FindObjectOfType<ObjectPooler>();
    }

    // Re-setup firePoint if it's null
    if (firePoint == null && mainCamera != null)
    {
        GameObject defaultFirePoint = new GameObject("DefaultFirePoint");
        defaultFirePoint.transform.SetParent(mainCamera.transform);
        defaultFirePoint.transform.localPosition = new Vector3(0, 0, 0);
        firePoint = defaultFirePoint.transform;
    }

    // Reset weapon rotation to original
    transform.eulerAngles = originalWeaponRotation;
    isRotatingForReload = false;

    // Reset ammo UI state
    if (enableAmmoUIDisplay && ammoDebugTextObject != null)
    {
        originalAmmoUIScale = ammoDebugTextObject.transform.localScale;
        lastAmmoColor = GetAmmoColor(currentAmmo);
        lastAmmoThreshold = GetAmmoThreshold(currentAmmo);
    }
}

/// <summary>
/// Get the appropriate color for the current ammo count
/// </summary>
private Color GetAmmoColor(int ammo)
{
    if (ammo <= 0)
        return ammoColorEmpty;
    
    float ammoPercentage = (float)ammo / weaponData.clipSize;
    
    if (ammoPercentage > 0.75f)
        return ammoColorFull;
    else if (ammoPercentage > 0.25f)
        return ammoColorMedium;
    else
        return ammoColorLow;
}

/// <summary>
/// Get the threshold category for ammo count (for audio feedback)
/// </summary>
private int GetAmmoThreshold(int ammo)
{
    if (ammo <= 0) return 0; // Empty
    
    float ammoPercentage = (float)ammo / weaponData.clipSize;
    
    if (ammoPercentage > 0.75f) return 3; // Full
    else if (ammoPercentage > 0.25f) return 2; // Medium
    else return 1; // Low
}

/// <summary>
/// Update ammo UI color and size based on current ammo
/// </summary>
private void UpdateAmmoUI()
{
    if (!enableAmmoUIDisplay || ammoDebugTextMesh == null)
        return;

    // Update ammo text
    if (isReloading)
    {
        ammoDebugTextMesh.text = "RELOADING";
    }
    else if (currentAmmo <= 0)
    {
        ammoDebugTextMesh.text = "OUT OF AMMO";
    }
    else
    {
        ammoDebugTextMesh.text = $"Ammo: {currentAmmo}/{weaponData.clipSize}";
    }

    // Update color
    Color newColor = GetAmmoColor(currentAmmo);
    if (newColor != lastAmmoColor)
    {
        ammoDebugTextMesh.color = newColor;
        lastAmmoColor = newColor;

        // Placeholder for audio feedback on color change
        // AudioManager.Instance.PostEvent(ammoColorChangeAudioEvent, gameObject);
    }

    // Update size based on ammo status
    Vector3 targetScale = originalAmmoUIScale;
    if (currentAmmo <= 0)
    {
        targetScale = originalAmmoUIScale * emptyAmmoSizeMultiplier;
    }
    ammoDebugTextObject.transform.localScale = targetScale;

    // Check for threshold changes for audio feedback
    int currentThreshold = GetAmmoThreshold(currentAmmo);
    if (currentThreshold != lastAmmoThreshold && currentThreshold == 1) // Entering low ammo
    {
        // Placeholder for low ammo audio warning
        // AudioManager.Instance.PostEvent(lowAmmoAudioEvent, gameObject);
    }
    lastAmmoThreshold = currentThreshold;

    // Ensure the debug text canvas faces the camera
    if (Camera.main != null)
    {
        ammoDebugTextObject.transform.rotation = Quaternion.LookRotation(ammoDebugTextObject.transform.position - Camera.main.transform.position);
    }
}

/// <summary>
/// Pulse the ammo UI when trying to shoot without ammo
/// </summary>
private IEnumerator PulseAmmoUI()
{
    if (!enableAmmoUIDisplay || ammoDebugTextObject == null)
        yield break;

    Vector3 originalScale = ammoDebugTextObject.transform.localScale;
    Vector3 pulseTargetScale = originalScale * pulseScale;
    
    float elapsedTime = 0f;
    float halfDuration = pulseDuration / 2f;

    // Pulse up
    while (elapsedTime < halfDuration)
    {
        elapsedTime += Time.deltaTime;
        float progress = elapsedTime / halfDuration;
        ammoDebugTextObject.transform.localScale = Vector3.Lerp(originalScale, pulseTargetScale, progress);
        yield return null;
    }

    // Pulse down
    elapsedTime = 0f;
    while (elapsedTime < halfDuration)
    {
        elapsedTime += Time.deltaTime;
        float progress = elapsedTime / halfDuration;
        ammoDebugTextObject.transform.localScale = Vector3.Lerp(pulseTargetScale, originalScale, progress);
        yield return null;
    }

    // Ensure we end at the correct scale
    ammoDebugTextObject.transform.localScale = originalScale;
}

/// <summary>
/// Animate weapon rotation during reload
/// </summary>
private IEnumerator AnimateReloadRotation()
{
    if (!enableReloadAnimation || reloadRotationTransform == null)
        yield break;

    isRotatingForReload = true;

    // Cache the original local rotation
    Quaternion originalLocalRotation = reloadRotationTransform.localRotation;
    Quaternion targetLocalRotation = Quaternion.Euler(originalLocalRotation.eulerAngles + new Vector3(reloadRotationAngle, 0, 0));

    float elapsedTime = 0f;
    float animationDuration = weaponData.reloadSpeed / 2f; // Use half of reload speed for each rotation phase

    // Rotate down
    while (elapsedTime < animationDuration)
    {
        elapsedTime += Time.deltaTime;
        float progress = elapsedTime / animationDuration;
        reloadRotationTransform.localRotation = Quaternion.Lerp(originalLocalRotation, targetLocalRotation, progress);
        yield return null;
    }

    // Hold the rotation while reloading
    reloadRotationTransform.localRotation = targetLocalRotation;

    // Wait for reload to finish (this will be controlled by the reload coroutine)
    yield return new WaitWhile(() => isReloading);

    // Rotate back up
    elapsedTime = 0f;
    while (elapsedTime < animationDuration)
    {
        elapsedTime += Time.deltaTime;
        float progress = elapsedTime / animationDuration;
        reloadRotationTransform.localRotation = Quaternion.Lerp(targetLocalRotation, originalLocalRotation, progress);
        yield return null;
    }

    // Ensure we end at the original rotation
    reloadRotationTransform.localRotation = originalLocalRotation;
    isRotatingForReload = false;
}
}
