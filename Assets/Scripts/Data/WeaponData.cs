// Assets/Scripts/Data/WeaponData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "New WeaponData", menuName = "Data/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Weapon Stats")]
    [Tooltip("Damage per shot.")]
    public float damage = 10f;

    [Tooltip("Time between shots.")]
    public float fireRate = 0.1f;

    [Tooltip("Is the weapon automatic?")]
    public bool isAutomatic = false;

    [Tooltip("Number of projectiles fired per shot (e.g. 1 for rifle, 8 for shotgun).")]
    public int projectilesPerShot = 1;

    [Tooltip("Number of bullets in a clip.")]
    public int clipSize = 30;

    [Tooltip("Time it takes to reload.")]
    public float reloadSpeed = 1.5f;

    [Tooltip("Maximum range of the weapon.")]
    public float range = 50f;

    [Header("Audio")]
    [Tooltip("Sound played when firing.")]
    public AudioEvent fireSound;

    [Tooltip("Sound played when reloading.")]
    public AudioEvent reloadSound;

    [Tooltip("Sound played when the weapon hits an enemy.")]
    public AudioEvent hitEnemySound;

    [Tooltip("Sound played when the weapon hits something else.")]
    public AudioEvent hitOtherSound;

    [Tooltip("Sound played when out of ammo.")]
    public AudioEvent outOfAmmoSound;

    [Tooltip("Sound played on impact.")]
    public AudioEvent impactSound;

    [Header("Bloom Settings")]
    [Tooltip("Minimum spread angle in degrees when standing still.")]
    public float minBloomAngle = 0.1f;

    [Tooltip("Maximum spread angle in degrees.")]
    public float maxBloomAngle = 5f;

    [Tooltip("How much bloom is added per shot.")]
    public float bloomGrowthRate = 1f;

    [Tooltip("How fast bloom recovers per second.")]
    public float bloomRecoveryRate = 10f;

    [Tooltip("Multiplier for bloom when moving.")]
    public float movementBloomMultiplier = 2f;

    [Tooltip("Multiplier for bloom when crouching (if applicable).")]
    public float crouchBloomMultiplier = 0.5f;

    [Header("Recoil Settings")]
    [Tooltip("Vertical recoil (X-axis rotation).")]
    public float recoilX = 2f;

    [Tooltip("Horizontal recoil (Y-axis rotation).")]
    public float recoilY = 0.5f;

    [Tooltip("Kickback force (Z-axis position).")]
    public float recoilZ = 0.1f;

    [Tooltip("How fast the recoil moves to the target position.")]
    public float snappiness = 6f;

    [Tooltip("How fast the recoil returns to zero.")]
    public float returnSpeed = 2f;
}
