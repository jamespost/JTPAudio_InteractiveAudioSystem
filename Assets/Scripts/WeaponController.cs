using UnityEngine;
using System.Collections;
using UnityEngine.UI; // Added for UI components

/// <summary>
/// Handles weapon functionality including firing, reloading, and event-driven interactions.
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

    [Header("Debug Settings")]
    [Tooltip("Enable debug mode to visualize hit points and log additional information.")]
    public bool debugMode = false;

    private int currentAmmo;
    private bool isReloading;
    private float nextFireTime;
    private ObjectPooler vfxPooler;
    private Camera mainCamera;

    private GameObject ammoDebugTextObject;
    private Text ammoDebugTextMesh;

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
        }
    }

    private void Update()
    {
        HandleInput();

        if (enableAmmoUIDisplay && ammoDebugTextMesh != null)
        {
            // Update the ammo text to represent current ammo
            ammoDebugTextMesh.text = $"Ammo: {currentAmmo}/{weaponData.clipSize}";

            // Ensure the debug text canvas faces the camera
            if (Camera.main != null)
            {
                ammoDebugTextObject.transform.rotation = Quaternion.LookRotation(ammoDebugTextObject.transform.position - Camera.main.transform.position);
            }
        }
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

            // Play out of ammo sound
            if (weaponData.outOfAmmoSound != null)
            {
                AudioManager.Instance.PostEvent(weaponData.outOfAmmoSound.eventID, this.gameObject);
            }

            return;
        }

        currentAmmo--;
        nextFireTime = Time.time + weaponData.fireRate;

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

        yield return new WaitForSeconds(weaponData.reloadSpeed);

        currentAmmo = weaponData.clipSize;
        isReloading = false;
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
    }
}
