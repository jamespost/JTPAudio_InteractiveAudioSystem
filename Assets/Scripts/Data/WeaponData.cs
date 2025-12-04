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

    [Header("Weapon Model Recoil")]
    [Tooltip("Positional kickback for the weapon model (Local Position).")]
    public Vector3 weaponKickback = new Vector3(0, 0, -0.1f);

    [Tooltip("Rotational recoil for the weapon model (Local Rotation).")]
    public Vector3 weaponRecoilRotation = new Vector3(-10f, 0, 0);

    [Tooltip("How fast the weapon model moves to the recoil target.")]
    public float weaponSnappiness = 10f;

    [Tooltip("How fast the weapon model returns to original position.")]
    public float weaponReturnSpeed = 10f;

    [Header("Weapon Sway")]
    [Tooltip("Amount of sway when looking around.")]
    public float swayAmount = 0.02f;

    [Tooltip("Maximum amount of sway.")]
    public float maxSwayAmount = 0.06f;

    [Tooltip("Smoothing value for sway movement.")]
    public float swaySmoothness = 4f;

    [Tooltip("Amount of sway rotation when looking around.")]
    public float swayRotationAmount = 4f;

    [Tooltip("Maximum amount of sway rotation.")]
    public float maxSwayRotation = 10f;

    [Tooltip("Smoothing value for sway rotation.")]
    public float swayRotationSmoothness = 12f;

    [Header("Weapon Movement Sway")]
    [Tooltip("Amount of positional sway from movement.")]
    public float movementSwayX = 0.05f;

    [Tooltip("Amount of positional sway from movement.")]
    public float movementSwayY = 0.05f;

    [Tooltip("Smoothing value for movement sway.")]
    public float movementSwaySmoothness = 6f;

    [Header("Vertical Sway (Jump/Land)")]
    [Tooltip("Positional sway when jumping.")]
    public Vector3 jumpSwayPosition = new Vector3(0, -0.1f, 0);

    [Tooltip("Rotational sway when jumping.")]
    public Vector3 jumpSwayRotation = new Vector3(5f, 0, 0);

    [Tooltip("Positional sway when landing.")]
    public Vector3 landSwayPosition = new Vector3(0, -0.2f, 0);

    [Tooltip("Rotational sway when landing.")]
    public Vector3 landSwayRotation = new Vector3(10f, 0, 0);

    [Tooltip("How fast the vertical sway recovers.")]
    public float verticalSwayRecoverySpeed = 4f;

    [Tooltip("Multiplier for landing sway based on impact velocity.")]
    public float landSwayMultiplier = 0.1f;

    [Header("Sprint Sway")]
    [Tooltip("Multiplier for movement sway when sprinting.")]
    public float sprintSwayMultiplier = 2f;

    [Tooltip("Positional sway when starting to sprint.")]
    public Vector3 sprintStartSwayPosition = new Vector3(0, -0.05f, -0.05f);

    [Tooltip("Rotational sway when starting to sprint.")]
    public Vector3 sprintStartSwayRotation = new Vector3(5f, 0, 0);

    [Tooltip("Positional sway when stopping sprint.")]
    public Vector3 sprintEndSwayPosition = new Vector3(0, 0.05f, 0.05f);

    [Tooltip("Rotational sway when stopping sprint.")]
    public Vector3 sprintEndSwayRotation = new Vector3(-5f, 0, 0);

    [Tooltip("How fast the sprint sway recovers.")]
    public float sprintSwayRecoverySpeed = 5f;
}
