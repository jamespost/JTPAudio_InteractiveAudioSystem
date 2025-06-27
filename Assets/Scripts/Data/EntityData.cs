// Assets/Scripts/Data/EntityData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "New EntityData", menuName = "Data/Entity Data")]
public class EntityData : ScriptableObject
{
    [Header("Health")]
    [Tooltip("Maximum health of the entity.")]
    public float maxHealth = 100f;

    [Header("Health Display")]
    [Tooltip("Size multiplier for the health display element.")]
    public float healthDisplaySize = 1f;
}
