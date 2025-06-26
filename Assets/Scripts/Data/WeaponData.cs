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

    [Tooltip("Number of bullets in a clip.")]
    public int clipSize = 30;

    [Tooltip("Time it takes to reload.")]
    public float reloadSpeed = 1.5f;

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
}
