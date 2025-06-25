// Assets/Scripts/Data/EnemyData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "New EnemyData", menuName = "Data/Enemy Data")]
public class EnemyData : EntityData
{
    [Header("AI Behavior")]
    [Tooltip("How fast the enemy moves.")]
    public float moveSpeed = 3.5f;

    [Tooltip("Damage dealt by the enemy's attack.")]
    public float attackDamage = 10f;

    [Tooltip("Range of the enemy's attack.")]
    public float attackRange = 1.5f;

    [Tooltip("How quickly the enemy can attack.")]
    public float attackSpeed = 1f;

    [Tooltip("How close the enemy gets to the player.")]
    public float stoppingDistance = 1f;
}
