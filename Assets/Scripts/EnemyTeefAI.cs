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
    private Rigidbody playerRb;
    private Health healthComponent;
    private float attackCooldownTimer;
    private ObjectPooler enemyPooler;

    // Pathfinding variables
    private NavMeshPath path;
    private float pathUpdateTimer;
    private Vector3 currentTargetPosition;

    [Header("Bite & Fling Settings")]
    [Tooltip("Min/max time before another jaw bite is attempted.")]
    public Vector2 biteIntervalRange = new Vector2(1.5f, 3f);

    [Tooltip("How long the head braces (clamps down) before hurling itself.")]
    public float biteBraceDuration = 0.35f;

    [Tooltip("Extra drag applied while biting to make it feel stuck to the floor.")]
    public float biteAnchorDrag = 4f;

    [Tooltip("Downward force applied when the jaw bites the floor.")]
    public float biteDownForce = 30f;

    [Tooltip("Whether the head needs clear line of sight before attempting a bite.")]
    public bool requireLineOfSight = false;

    [Tooltip("If a bite can't line up, force one after this extra delay (seconds).")]
    public float biteForceTriggerDelay = 0.75f;

    [Tooltip("Horizontal impulse when the head yanks forward.")]
    public float flingForce = 20f;

    [Tooltip("Upward impulse so the fling arcs instead of skimming the ground.")]
    public float flingUpwardForce = 6f;

    [Tooltip("How aggressively the head spins during the fling.")]
    public float flingTorqueMultiplier = 12f;

    [Tooltip("Minimum time we stay airborne before looking to recover.")]
    public float flingHangTime = 0.2f;

    [Tooltip("Delay after landing before rolling resumes.")]
    public float flingRecoverTime = 0.45f;

    [Tooltip("How far ahead on the path we look when choosing a fling direction.")]
    public float pathLookAheadDistance = 3f;

    [Tooltip("Ground probe distance when checking if the head is planted.")]
    public float groundCheckDistance = 0.6f;

    [Tooltip("Layer mask used when raycasting for the floor.")]
    public LayerMask groundLayers = ~0;

    private enum LocomotionState
    {
        Rolling,
        Biting,
        Flinging,
        Recovering
    }

    private LocomotionState locomotionState = LocomotionState.Rolling;
    private float stateTimer;
    private float biteIntervalTimer;
    private Vector3 cachedFlingDirection;
    private float defaultDrag;
    private float defaultAngularDrag;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            defaultDrag = rb.linearDamping;
            defaultAngularDrag = rb.angularDamping;
        }
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
            playerRb = playerObj.GetComponent<Rigidbody>();
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

        ScheduleNextBite();
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
            rb.linearDamping = defaultDrag;
            rb.angularDamping = defaultAngularDrag;
        }

        locomotionState = LocomotionState.Rolling;
        stateTimer = 0f;
        ScheduleNextBite();

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

        UpdatePathToPlayer();

        switch (locomotionState)
        {
            case LocomotionState.Rolling:
                HandleRollingState();
                break;
            case LocomotionState.Biting:
                HandleBiteState();
                break;
            case LocomotionState.Flinging:
                HandleFlingState();
                break;
            case LocomotionState.Recovering:
                HandleRecoverState();
                break;
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

    private void UpdatePathToPlayer()
    {
        pathUpdateTimer -= Time.fixedDeltaTime;
        if (pathUpdateTimer > 0f || player == null)
        {
            return;
        }

        pathUpdateTimer = 0.2f;
        NavMesh.CalculatePath(transform.position, player.position, NavMesh.AllAreas, path);

        if (path.status == NavMeshPathStatus.PathComplete || path.status == NavMeshPathStatus.PathPartial)
        {
            if (path.corners.Length > 1)
            {
                currentTargetPosition = path.corners[1];
            }
            else
            {
                currentTargetPosition = player.position;
            }
        }
        else
        {
            currentTargetPosition = player.position;
        }
    }

    private void HandleRollingState()
    {
        Vector3 direction = GetPlanarDirection(currentTargetPosition);
        if (direction.sqrMagnitude > 0.001f)
        {
            ApplyRollingForces(direction);
        }

        biteIntervalTimer -= Time.fixedDeltaTime;
        if (biteIntervalTimer <= 0f && CanTriggerBite(direction))
        {
            EnterState(LocomotionState.Biting);
            return;
        }

        if (biteIntervalTimer <= -Mathf.Abs(biteForceTriggerDelay) && IsGrounded())
        {
            EnterState(LocomotionState.Biting);
        }
    }

    private void HandleBiteState()
    {
        stateTimer -= Time.fixedDeltaTime;

        // Keep jaws pinned to the floor so the fling feels anchored.
        rb.AddForce(Vector3.down * biteDownForce * Time.fixedDeltaTime, ForceMode.Acceleration);

        if (stateTimer <= 0f)
        {
            EnterState(LocomotionState.Flinging);
        }
    }

    private void HandleFlingState()
    {
        stateTimer -= Time.fixedDeltaTime;

        if (stateTimer <= 0f && IsGrounded())
        {
            EnterState(LocomotionState.Recovering);
        }
    }

    private void HandleRecoverState()
    {
        stateTimer -= Time.fixedDeltaTime;
        if (stateTimer <= 0f)
        {
            EnterState(LocomotionState.Rolling);
        }
    }

    private void EnterState(LocomotionState newState)
    {
        locomotionState = newState;

        switch (newState)
        {
            case LocomotionState.Rolling:
                rb.linearDamping = defaultDrag;
                rb.angularDamping = defaultAngularDrag;
                stateTimer = 0f;
                break;
            case LocomotionState.Biting:
                stateTimer = biteBraceDuration;
                rb.linearDamping = defaultDrag + biteAnchorDrag;
                rb.angularDamping = defaultAngularDrag + biteAnchorDrag * 0.5f;
                rb.linearVelocity *= 0.2f;
                rb.angularVelocity *= 0.2f;
                rb.AddForce(Vector3.down * biteDownForce, ForceMode.Acceleration);
                break;
            case LocomotionState.Flinging:
                stateTimer = flingHangTime;
                rb.linearDamping = defaultDrag;
                rb.angularDamping = defaultAngularDrag;
                cachedFlingDirection = DetermineFlingDirection();
                Vector3 impulse = cachedFlingDirection * flingForce + Vector3.up * flingUpwardForce;
                rb.AddForce(impulse, ForceMode.Impulse);
                Vector3 torqueAxis = Vector3.Cross(Vector3.up, cachedFlingDirection);
                if (torqueAxis.sqrMagnitude > 0.001f)
                {
                    rb.AddTorque(torqueAxis.normalized * flingTorqueMultiplier, ForceMode.Impulse);
                }
                ScheduleNextBite();
                break;
            case LocomotionState.Recovering:
                stateTimer = flingRecoverTime;
                break;
        }
    }

    private void ApplyRollingForces(Vector3 direction)
    {
        float force = enemyData.moveSpeed * rollForceMultiplier;
        rb.AddForce(direction * force, ForceMode.Force);

        Vector3 rotationAxis = Vector3.Cross(Vector3.up, direction);
        rb.AddTorque(rotationAxis * force, ForceMode.Force);

        if (rb.linearVelocity.magnitude > enemyData.moveSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * enemyData.moveSpeed;
        }
    }

    private bool CanTriggerBite(Vector3 rollDirection)
    {
        if (rollDirection.sqrMagnitude < 0.1f) return false;
        if (!IsGrounded()) return false;
        if (rb.linearVelocity.magnitude < enemyData.moveSpeed * 0.35f) return false;

        Vector3 toPlayer = GetPlanarDirection(player.position);
        float facingDot = Vector3.Dot(rollDirection, toPlayer);
        if (facingDot < 0.4f) return false;

        return !requireLineOfSight || HasLineOfSightToPlayer();
    }

    private Vector3 DetermineFlingDirection()
    {
        Vector3 lookAhead = FindLookAheadPoint();
        Vector3 direction = (lookAhead - transform.position);
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
        {
            direction = (player.position - transform.position);
            direction.y = 0f;
        }

        if (playerRb != null)
        {
            Vector3 predictive = player.position + GetVelocity(playerRb) * 0.25f - transform.position;
            predictive.y = 0f;
            if (predictive.sqrMagnitude > direction.sqrMagnitude * 0.5f)
            {
                direction = Vector3.Lerp(direction, predictive, 0.5f);
            }
        }

        return direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;
    }

    private Vector3 FindLookAheadPoint()
    {
        if (path != null && path.corners != null && path.corners.Length > 1)
        {
            float accumulated = 0f;
            Vector3 prev = transform.position;
            for (int i = 1; i < path.corners.Length; i++)
            {
                Vector3 corner = path.corners[i];
                float segment = Vector3.Distance(prev, corner);
                accumulated += segment;
                if (accumulated >= pathLookAheadDistance)
                {
                    return corner;
                }
                prev = corner;
            }
            return path.corners[path.corners.Length - 1];
        }

        return currentTargetPosition != Vector3.zero ? currentTargetPosition : player.position;
    }

    private Vector3 GetPlanarDirection(Vector3 target)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0f;
        return dir.sqrMagnitude > 0f ? dir.normalized : transform.forward;
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundCheckDistance, groundLayers, QueryTriggerInteraction.Ignore);
    }

    private bool HasLineOfSightToPlayer()
    {
        Vector3 origin = transform.position + Vector3.up * 0.25f;
        Vector3 toPlayer = player.position - origin;
        float distance = toPlayer.magnitude;
        if (distance <= 0.01f) return true;

        return !Physics.Raycast(origin, toPlayer.normalized, distance, groundLayers, QueryTriggerInteraction.Ignore);
    }

    private void ScheduleNextBite()
    {
        float min = Mathf.Min(biteIntervalRange.x, biteIntervalRange.y);
        float max = Mathf.Max(biteIntervalRange.x, biteIntervalRange.y);
        biteIntervalTimer = Random.Range(min, max);
    }

    private Vector3 GetVelocity(Rigidbody body)
    {
        return body != null ? body.linearVelocity : Vector3.zero;
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
