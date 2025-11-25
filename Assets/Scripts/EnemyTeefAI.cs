using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// EnemyTeefAI Script
/// 
/// Custom AI for the "Enemy TEEF" character.
/// This enemy moves by rolling towards the player using physics-based forces (Rigidbody).
/// It uses NavMesh.CalculatePath for pathfinding but Physics for movement.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyTeefAI : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("ScriptableObject containing enemy stats.")]
    public EnemyData enemyData;

    [Tooltip("Force multiplier for the rolling movement.")]
    public float rollForceMultiplier = 10f;

    private Transform player;
    private Rigidbody rb;
    private Health healthComponent;
    private float attackCooldownTimer;
    private ObjectPooler enemyPooler;

    // Pathfinding variables
    private NavMeshPath path;
    private float pathUpdateTimer;
    private Vector3 currentTargetPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        enemyPooler = FindFirstObjectByType<ObjectPooler>();
        path = new NavMeshPath();

        // CRITICAL: Disable NavMeshAgent if present to prevent it from fighting the physics
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = false;
        }
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        healthComponent = GetComponent<Health>();
        if (healthComponent != null)
        {
            healthComponent.OnDied += HandleDeath;
        }

        // Ensure the Rigidbody can rotate so it rolls
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.None;
        }
    }

    private void OnEnable()
    {
        if (healthComponent != null)
        {
            healthComponent.OnDied += HandleDeath;
        }
        // Reset physics state when respawning
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Fix spawn height to prevent spawning inside the floor
        // Since ObjectPooler now sets position BEFORE OnEnable, we can fix this immediately
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            // Raycast down to find the true ground level
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up * 5f, Vector3.down, out hit, 10f))
            {
                // Place the object so its bottom touches the ground
                float targetY = hit.point.y + col.bounds.extents.y;
                transform.position = new Vector3(transform.position.x, targetY + 0.1f, transform.position.z); // +0.1f buffer
            }
        }
    }

    private void OnDisable()
    {
        if (healthComponent != null)
        {
            healthComponent.OnDied -= HandleDeath;
        }
    }

    private void FixedUpdate()
    {
        if (player == null) return;

        // Update path periodically to save performance
        pathUpdateTimer -= Time.fixedDeltaTime;
        if (pathUpdateTimer <= 0)
        {
            pathUpdateTimer = 0.2f; // Update 5 times a second
            NavMesh.CalculatePath(transform.position, player.position, NavMesh.AllAreas, path);
            
            // Find the next target point
            if (path.status == NavMeshPathStatus.PathComplete || path.status == NavMeshPathStatus.PathPartial)
            {
                if (path.corners.Length > 1)
                {
                    currentTargetPosition = path.corners[1]; // Next corner
                }
                else
                {
                    currentTargetPosition = player.position; // Fallback
                }
            }
            else
            {
                currentTargetPosition = player.position; // Fallback if path fails
            }
        }

        // Calculate direction to the next path corner instead of directly to player
        Vector3 direction = (currentTargetPosition - transform.position).normalized;
        
        // Flatten direction to keep them on the ground
        direction.y = 0; 
        direction.Normalize();

        // Apply force to move towards the player
        float force = enemyData.moveSpeed * rollForceMultiplier;
        rb.AddForce(direction * force, ForceMode.Force);

        // Apply torque to force the rolling visual
        // Rotate around the axis perpendicular to movement and up
        Vector3 rotationAxis = Vector3.Cross(Vector3.up, direction);
        rb.AddTorque(rotationAxis * force, ForceMode.Force);

        // Cap velocity to prevent them from accelerating infinitely
        // We use moveSpeed as the max speed limit
        if (rb.linearVelocity.magnitude > enemyData.moveSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * enemyData.moveSpeed;
        }
    }

    private void Update()
    {
        // Manage attack cooldown
        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer -= Time.deltaTime;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        // Attack when colliding with the player
        if (collision.gameObject.CompareTag("Player"))
        {
            if (attackCooldownTimer <= 0f)
            {
                Attack(collision.gameObject);
                attackCooldownTimer = 1f / enemyData.attackSpeed;
            }
        }
    }

    private void Attack(GameObject target)
    {
        Health targetHealth = target.GetComponent<Health>();
        if (targetHealth != null)
        {
            targetHealth.TakeDamage(enemyData.attackDamage);
        }
    }

    private void HandleDeath()
    {
        // Return to pool or destroy
        if (enemyPooler != null)
        {
            gameObject.SetActive(false);
            enemyPooler.ReturnToPool(gameObject.tag, gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
