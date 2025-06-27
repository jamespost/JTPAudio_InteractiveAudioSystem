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

    private int currentAmmo;
    private bool isReloading;
    private float nextFireTime;
    private ObjectPooler vfxPooler;
    private Camera mainCamera;

    private void Awake()
    {
        // Initialize Object Pooler for VFX
        vfxPooler = FindObjectOfType<ObjectPooler>();

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
            }

            // Play impact sound
            if (weaponData.impactSound != null)
            {
                AudioManager.Instance.PostEvent(weaponData.impactSound.eventID, hit.collider.gameObject);
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
}
