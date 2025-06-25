using System;
using UnityEngine;

/// <summary>
/// A static class that manages game-wide events using C# Actions.
/// This creates a decoupled architecture, allowing different systems (like Audio, UI, Gameplay)
/// to communicate without direct references to each other.
///
/// HOW TO USE:
/// 1. To SUBSCRIBE to an event: EventManager.OnPlayerDamaged += YourFunction;
/// 2. To UNSUBSCRIBE from an event: EventManager.OnPlayerDamaged -= YourFunction;
/// 3. To TRIGGER an event: EventManager.TriggerPlayerDamaged(10f);
/// </summary>
public static class EventManager
{
    // =================================== PLAYER EVENTS ====================================
    /// <summary>
    /// Called when the player takes damage.
    /// Provides the amount of damage taken.
    /// </summary>
    public static event Action<float> OnPlayerDamaged;

    /// <summary>
    /// Invokes the OnPlayerDamaged event with the specified damage amount.
    /// </summary>
    /// <param name="damageAmount">The amount of damage dealt to the player.</param>
    public static void TriggerPlayerDamaged(float damageAmount)
    {
        // The ?.Invoke() is a null-conditional operator.
        // It safely checks if OnPlayerDamaged is not null (i.e., has subscribers) before trying to invoke it.
        // This prevents NullReferenceException errors.
        OnPlayerDamaged?.Invoke(damageAmount);
    }

    /// <summary>
    /// Called when the player's health reaches zero.
    /// </summary>
    public static event Action OnPlayerDied;

    /// <summary>
    /// Invokes the OnPlayerDied event.
    /// </summary>
    public static void TriggerPlayerDied()
    {
        OnPlayerDied?.Invoke();
    }


    // ==================================== ENEMY EVENTS ====================================
    /// <summary>
    /// Called when an enemy is destroyed.
    /// Provides the position where the enemy died, useful for spawning effects or pickups.
    /// </summary>
    public static event Action<Vector3> OnEnemyDied;

    /// <summary>
    /// Invokes the OnEnemyDied event.
    /// </summary>
    /// <param name="position">The world position where the enemy was destroyed.</param>
    public static void TriggerEnemyDied(Vector3 position)
    {
        OnEnemyDied?.Invoke(position);
    }


    // =================================== WEAPON EVENTS ====================================
    /// <summary>
    /// Called when the player's weapon is fired.
    /// Useful for triggering audio and visual effects.
    /// </summary>
    public static event Action OnWeaponFired;

    public static void TriggerWeaponFired()
    {
        OnWeaponFired?.Invoke();
    }

    /// <summary>
    /// Called when the player reloads their weapon.
    /// </summary>
    public static event Action OnWeaponReloaded;

    public static void TriggerWeaponReloaded()
    {
        OnWeaponReloaded?.Invoke();
    }

    /// <summary>
    /// Called when the player's ammo count changes.
    /// Provides the current ammo and the maximum ammo for UI updates.
    /// </summary>
    public static event Action<int, int> OnAmmoChanged;

    /// <summary>
    /// Invokes the OnAmmoChanged event.
    /// </summary>
    /// <param name="currentAmmo">The current ammo in the clip.</param>
    /// <param name="maxAmmo">The maximum ammo the clip can hold.</param>
    public static void TriggerAmmoChanged(int currentAmmo, int maxAmmo)
    {
        OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
    }


    // ================================= GAME STATE EVENTS ==================================
    /// <summary>
    /// Called when a new wave starts.
    /// Provides the number of the wave that is starting.
    /// </summary>
    public static event Action<int> OnWaveStarted;

    /// <summary>
    /// Invokes the OnWaveStarted event.
    /// </summary>
    /// <param name="waveNumber">The number of the wave that is beginning.</param>
    public static void TriggerWaveStarted(int waveNumber)
    {
        OnWaveStarted?.Invoke(waveNumber);
    }

    /// <summary>
    /// Called when the current wave is successfully completed.
    /// </summary>
    public static event Action OnWaveCompleted;

    public static void TriggerWaveCompleted()
    {
        OnWaveCompleted?.Invoke();
    }

    /// <summary>
    /// Called when the game over condition is met.
    /// </summary>
    public static event Action OnGameOver;

    public static void TriggerGameOver()
    {
        OnGameOver?.Invoke();
    }
}
