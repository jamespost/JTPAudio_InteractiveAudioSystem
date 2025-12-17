using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class TentacleHeadController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3.5f;
    public float rotationSpeed = 120f;
    public float acceleration = 8f;
    public float stoppingDistance = 2.0f;

    [Header("Targeting")]
    public string targetTag = "Player";
    public float detectionRange = 20f;
    
    private NavMeshAgent agent;
    private Transform target;
    private TentacleLocomotion locomotion;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        locomotion = GetComponent<TentacleLocomotion>();

        // Configure Agent
        agent.speed = moveSpeed;
        agent.angularSpeed = rotationSpeed;
        agent.acceleration = acceleration;
        agent.stoppingDistance = stoppingDistance;
        
        // Disable auto-braking so we can handle it or let the agent slide a bit if needed
        // agent.autoBraking = false; 

        FindTarget();
    }

    private void Update()
    {
        if (target == null)
        {
            FindTarget();
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        if (distanceToTarget <= detectionRange)
        {
            agent.SetDestination(target.position);
        }
        
        // Optional: Pass speed to locomotion if we want to change step speed dynamically
        // if (locomotion != null)
        // {
        //     locomotion.stepDuration = Mathf.Lerp(0.4f, 0.2f, agent.velocity.magnitude / agent.speed);
        // }
    }

    private void FindTarget()
    {
        GameObject player = GameObject.FindGameObjectWithTag(targetTag);
        if (player != null)
        {
            target = player.transform;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
