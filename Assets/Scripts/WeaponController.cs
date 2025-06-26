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

    private int currentAmmo;
    private bool isReloading;
    private float nextFireTime;

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

        // TODO: Add projectile instantiation or raycast logic here
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
