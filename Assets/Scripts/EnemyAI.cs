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

    // Reference to the Health component for damage handling and death events.
    private Health healthComponent;

    // Add an enum for the state machine
    private enum EnemyState
    {
        IDLE,
        CHASING,
        ATTACKING
    }

    // Current state of the enemy
    private EnemyState currentState = EnemyState.IDLE;

    private ObjectPooler enemyPooler;

    private void Awake()
    {
        // Initialize the ObjectPooler for enemies
        enemyPooler = FindObjectOfType<ObjectPooler>();
        if (enemyPooler == null)
        {
            Debug.LogError("[EnemyAI] ObjectPooler not found in the scene. Ensure an ObjectPooler is set up for enemies.");
        }
    }

    private void Start()
    {
        // Initialize NavMeshAgent and find the player by tag.
        navMeshAgent = GetComponent<NavMeshAgent>();
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogError("Player GameObject with tag 'Player' not found in the scene.");
        }

        // Initialize Health component.
        healthComponent = GetComponent<Health>();
        if (healthComponent == null)
        {
            Debug.LogError("Health component is missing on " + gameObject.name);
        }
        else
        {
            // Subscribe to the OnDied event
            healthComponent.OnDied += HandleDeath;
        }

        // Apply movement and stopping distance settings from EnemyData.
        if (enemyData != null)
        {
            navMeshAgent.speed = enemyData.moveSpeed;
            navMeshAgent.stoppingDistance = enemyData.stoppingDistance;
        }
    }

    private void OnEnable()
    {
        // Reset enemy state when reactivated from the pool
        ResetEnemyState();

        // Subscribe to the OnDied event of the Health component
        if (healthComponent != null)
        {
            healthComponent.OnDied += HandleDeath;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from the OnDied event to avoid memory leaks
        if (healthComponent != null)
        {
            healthComponent.OnDied -= HandleDeath;
        }
    }

    private void Update()
    {
        // Exit if the player is not found.
        if (player == null) return;

        // State machine logic
        switch (currentState)
        {
            case EnemyState.IDLE:
                HandleIdleState();
                break;

            case EnemyState.CHASING:
                HandleChasingState();
                break;

            case EnemyState.ATTACKING:
                HandleAttackingState();
                break;
        }
    }

    private void HandleIdleState()
    {
        // Transition to CHASING if the player is within detection range
        if (Vector3.Distance(transform.position, player.position) <= enemyData.detectionRange)
        {
            currentState = EnemyState.CHASING;
        }
    }

    private void HandleChasingState()
    {
        // Continuously set the destination to the player's position
        navMeshAgent.SetDestination(player.position);

        // Transition to ATTACKING if within attack range
        if (Vector3.Distance(transform.position, player.position) <= enemyData.attackRange)
        {
            //Debug.Log("Transitioning to ATTACKING state.");
            currentState = EnemyState.ATTACKING;
        }
    }

    private void HandleAttackingState()
    {
        // Attack if the cooldown has elapsed
        if (attackCooldown <= 0f)
        {
            //Debug.Log("Cooldown elapsed. Attacking player.");
            Attack();
            attackCooldown = 1f / enemyData.attackSpeed; // Reset the cooldown based on attack speed
        }

        // Decrease the cooldown timer over time
        attackCooldown -= Time.deltaTime;

        // Transition back to CHASING if the player moves out of attack range
        if (Vector3.Distance(transform.position, player.position) > enemyData.attackRange)
        {
            //Debug.Log("Player moved out of attack range. Transitioning to CHASING state.");
            currentState = EnemyState.CHASING;
        }
    }

    private void Attack()
    {
        // Attempt to damage the player if they have a Health component.
        Health playerHealth = player.GetComponent<Health>();
        if (playerHealth != null)
        {
            //Debug.Log("Player has Health component. Applying damage: " + enemyData.attackDamage);
            playerHealth.TakeDamage(enemyData.attackDamage); // Deal damage to the player.
        }
        else
        {
            //Debug.LogWarning("Player does not have a Health component. Cannot apply damage.");
        }

        // Log the attack and trigger any related animations or sounds.
        //Debug.Log("Enemy attacked the player!");
    }

    public void TakeDamage(float damage)
    {
        // Delegate damage handling to the Health component.
        if (healthComponent != null)
        {
            healthComponent.TakeDamage(damage);
            healthComponent.OnDied += HandleDeath;
        }
    }

    private void HandleDeath()
    {
        // TODO: Trigger death VFX (e.g., explosion, blood splatter)
        // TODO: Play death SFX using AudioManager

        // Return the enemy to the pool
        if (enemyPooler != null)
        {
            gameObject.SetActive(false);
            enemyPooler.ReturnToPool(gameObject.tag, gameObject);
        }
        else
        {
            Debug.LogWarning("[EnemyAI] ObjectPooler is not initialized. Destroying enemy instead.");
            Destroy(gameObject);
        }
    }

    private void ResetEnemyState()
    {
        // Reset any necessary variables or states for the enemy
        // Example: Reset health, position, or AI state
        currentState = EnemyState.IDLE;
        attackCooldown = 0f;
        if (navMeshAgent != null)
        {
            navMeshAgent.ResetPath();
        }
        if (healthComponent != null)
        {
            healthComponent.ResetHealth();
        }
    }
}
