using UnityEngine;
using System.Collections;

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

    [Header("Debug Settings")]
    [Tooltip("Enable debug mode to visualize hit points and log additional information.")]
    public bool debugMode = false;

    private int currentAmmo;
    private bool isReloading;
    private float nextFireTime;
    private ObjectPooler vfxPooler;
    private Camera mainCamera;

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
    }

    private void Update()
    {
        HandleInput();
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
        }
    }
}
