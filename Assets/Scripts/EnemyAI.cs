using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// EnemyAI Script
/// 
/// This script controls the behavior of enemy characters in the game. It uses Unity's NavMeshAgent for pathfinding
/// and interacts with the player by attacking when in range. The script is data-driven, relying on the EnemyData
/// ScriptableObject to define key stats such as movement speed, attack range, and damage.
/// 
/// How to Use:
/// 1. Attach this script to an enemy GameObject with a NavMeshAgent component.
/// 2. Create an EnemyData ScriptableObject and configure the desired stats (e.g., move speed, attack range, damage).
/// 3. Assign the EnemyData ScriptableObject to the "Enemy Data" field in the Inspector.
/// 4. Ensure the player GameObject has the "Player" tag and a Health component to interact with this script.
/// 5. Customize attack animations, sounds, or additional logic in the Attack() method as needed.
/// 
/// This script is designed to be modular and designer-friendly, allowing easy tweaking of enemy behavior via the Inspector.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    // Reference to the EnemyData ScriptableObject, which contains stats like move speed, attack range, etc.
    [Tooltip("ScriptableObject containing enemy stats such as move speed, attack range, and damage.")]
    public EnemyData enemyData;

    // Reference to the NavMeshAgent component for pathfinding.
    private NavMeshAgent navMeshAgent;

    // Reference to the player's transform for tracking and attacking.
    private Transform player;

    // Cooldown timer to manage attack intervals.
    private float attackCooldown;

    private void Start()
    {
        // Initialize NavMeshAgent and find the player by tag.
        navMeshAgent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player").transform;

        // Apply movement and stopping distance settings from EnemyData.
        if (enemyData != null)
        {
            navMeshAgent.speed = enemyData.moveSpeed;
            navMeshAgent.stoppingDistance = enemyData.stoppingDistance;
        }
    }

    private void Update()
    {
        // Exit if the player is not found.
        if (player == null) return;

        // Continuously set the destination to the player's position.
        navMeshAgent.SetDestination(player.position);

        // Check if the enemy is within attack range of the player.
        if (Vector3.Distance(transform.position, player.position) <= enemyData.attackRange)
        {
            // Attack if the cooldown has elapsed.
            if (attackCooldown <= 0f)
            {
                Attack();
                attackCooldown = 1f / enemyData.attackSpeed; // Reset the cooldown based on attack speed.
            }
        }

        // Decrease the cooldown timer over time.
        attackCooldown -= Time.deltaTime;
    }

    private void Attack()
    {
        // Attempt to damage the player if they have a Health component.
        Health playerHealth = player.GetComponent<Health>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(enemyData.attackDamage); // Deal damage to the player.
        }

        // Log the attack and trigger any related animations or sounds.
        Debug.Log("Enemy attacked the player!");
    }

    public void TakeDamage(float damage)
    {
        // Log the damage taken and handle enemy destruction if health is depleted.
        Debug.Log($"Enemy took {damage} damage!");

        // If health reaches 0, destroy the enemy GameObject.
        Destroy(gameObject);
    }
}
