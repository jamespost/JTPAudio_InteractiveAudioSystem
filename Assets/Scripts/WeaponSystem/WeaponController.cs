using System;
using UnityEngine;
using UnityEngine.InputSystem; // Import the Input System namespace
using UnityEngine.Events; // Import UnityEvent for EventManager integration
using JTPAudio; // Import the custom audio system namespace

public class WeaponController : MonoBehaviour
{
    [Header("Weapon Configuration")]
    [Tooltip("Drag the WeaponData asset here.")]
    public WeaponData weaponData;

    private int currentAmmo;
    private float nextFireTime;

    // Events
    public event Action OnFire;
    public event Action OnReload;
    public event Action<bool> OnHit;

    // Input Actions
    private PlayerInput playerInput;
    private InputAction fireAction;
    private InputAction reloadAction;

    private void Awake()
    {
        // Initialize PlayerInput and bind actions
        playerInput = GetComponent<PlayerInput>();
        fireAction = playerInput.actions["Fire"];
        reloadAction = playerInput.actions["Reload"];
    }

    private void Start()
    {
        if (weaponData == null)
        {
            Debug.LogError("WeaponData is not assigned!", this);
            return;
        }

        currentAmmo = weaponData.clipSize;
    }

    private void OnEnable()
    {
        // Enable input actions
        fireAction.Enable();
        reloadAction.Enable();
    }

    private void OnDisable()
    {
        // Disable input actions
        fireAction.Disable();
        reloadAction.Disable();
    }

    private void Update()
    {
        if (fireAction.triggered && Time.time >= nextFireTime)
        {
            Fire();
        }

        if (reloadAction.triggered)
        {
            Reload();
        }
    }

    private void Fire()
    {
        if (currentAmmo <= 0)
        {
            Debug.Log("Out of ammo!");
            return;
        }

        currentAmmo--;
        nextFireTime = Time.time + 1f / weaponData.fireRate;

        // Trigger fire event
        OnFire?.Invoke();
        EventManager.TriggerEvent("OnFire"); // Notify EventManager

        // Play fire sound using AudioManager
        if (!string.IsNullOrEmpty(weaponData.fireEventName))
        {
            AudioManager.Instance.PostEvent(weaponData.fireEventName, gameObject);
        }

        // Simulate hit detection (example)
        bool hitEnemy = UnityEngine.Random.value > 0.5f; // Replace with actual hit detection logic
        OnHit?.Invoke(hitEnemy);
        EventManager.TriggerEvent("OnHit", hitEnemy); // Notify EventManager with hit result
    }

    private void Reload()
    {
        currentAmmo = weaponData.clipSize;

        // Trigger reload event
        OnReload?.Invoke();
        EventManager.TriggerEvent("OnReload"); // Notify EventManager

        // Play reload sound using AudioManager
        if (!string.IsNullOrEmpty(weaponData.reloadEventName))
        {
            AudioManager.Instance.PostEvent(weaponData.reloadEventName, gameObject);
        }
    }
}