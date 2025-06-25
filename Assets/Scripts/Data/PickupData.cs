// Assets/Scripts/Data/PickupData.cs
using UnityEngine;

public enum PickupType
{
    Health,
    Ammo
}

[CreateAssetMenu(fileName = "New PickupData", menuName = "Data/Pickup Data")]
public class PickupData : ScriptableObject
{
    [Header("Pickup Details")]
    [Tooltip("The type of pickup.")]
    public PickupType pickupType;

    [Tooltip("The value of the pickup (e.g., amount of health or ammo).")]
    public float value = 25f;

    [Tooltip("The 3D model for the pickup in the world.")]
    public GameObject pickupModel;

    [Tooltip("Sound played when the item is picked up.")]
    public AudioEvent pickupSound;
}
