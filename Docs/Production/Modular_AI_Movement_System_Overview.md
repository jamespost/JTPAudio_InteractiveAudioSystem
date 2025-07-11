Modular AI Movement System - Technical Design Document
Author: Gemini
Date: July 11, 2025
Version: 1.1
1. Overview
1.1. Problem Statement
The current EnemyAI implementation has its movement logic tightly coupled with its state machine. Specifically, movement is hard-coded as "chase the player" using NavMeshAgent.SetDestination(). This approach is rigid and makes it difficult for designers to create varied and unique movement patterns for different non-player characters (NPCs) without modifying the core EnemyAI script.
1.2. Goal
The objective is to refactor the AI movement system to be modular, extensible, and designer-driven. This will be achieved by abstracting movement logic into self-contained, reusable components that can be created and configured by designers directly in the Unity Editor. An NPC must be able to possess multiple movement behaviors and have the logic to decide which one is appropriate to use based on the gameplay context (e.g., its health, distance to player, etc.).
1.3. Proposed Solution: The Strategy Pattern
We will implement a Strategy Pattern using ScriptableObjects. Each distinct movement algorithm (e.g., Chase, Flee, Patrol, Strafe) will be encapsulated in its own MovementBehavior ScriptableObject.
The EnemyAI script will be refactored to act as the "Context". It will no longer contain specific movement logic itself. Instead, it will hold a reference to a currentMovementBehavior and delegate the task of movement to it. The EnemyAI will be responsible for selecting the appropriate behavior from a list of available options based on its internal state and surrounding conditions.
This approach offers several key advantages:
Designer-Friendly: Designers can create, mix, and match movement behaviors as assets in the project.
Modular & Reusable: A "Flee" behavior can be created once and assigned to any NPC type.
Extensible: New movement patterns can be added simply by creating a new class that inherits from the base MovementBehavior, with no changes required to the EnemyAI script.
Decoupled: The core AI logic (state management, target selection) is separated from the movement execution logic.
2. Core Architecture
The system will be composed of three main parts:
MovementBehavior (ScriptableObject): An abstract base class that defines the interface for all movement strategies. Concrete implementations (e.g., ChaseBehavior, FleeBehavior) will inherit from this.
EnemyData (ScriptableObject): This data container will be modified to hold a list of all MovementBehavior assets that a specific enemy type can use.
EnemyAI (MonoBehaviour): The main controller script will be refactored to select and execute a MovementBehavior from the list provided by its EnemyData.
2.1. System Class Diagram
classDiagram
    direction LR
    class MonoBehaviour
    class ScriptableObject

    class EnemyAI {
        +EnemyData enemyData
        -MovementBehavior currentMovementBehavior
        +Update()
        -HandleCombatState()
        -SwitchBehavior~T~()
    }
    MonoBehaviour <|-- EnemyAI

    class EnemyData {
        +List~MovementBehavior~ movementBehaviors
        +MovementBehavior initialBehavior
    }
    ScriptableObject <|-- EnemyData

    class MovementBehavior {
        <<Abstract>>
        +ExecuteMove(agent, player, self)*
    }
    ScriptableObject <|-- MovementBehavior

    class ChaseBehavior {
        +float moveSpeed
        +ExecuteMove(agent, player, self)
    }
    MovementBehavior <|-- ChaseBehavior

    class FleeBehavior {
        +float moveSpeed
        +float fleeDistance
        +ExecuteMove(agent, player, self)
    }
    MovementBehavior <|-- FleeBehavior

    EnemyAI --> "1" EnemyData : has reference to
    EnemyAI --> "1" MovementBehavior : executes current
    EnemyData o--> "*" MovementBehavior : holds list of


3. Implementation Plan
This section details the necessary code changes and additions for the AI programmer.
3.1. Step 1: Create MovementBehavior Base Class
Create a new abstract ScriptableObject class that will serve as the foundation for all movement behaviors.
File: Assets/Scripts/AI/Movement/MovementBehavior.cs
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Abstract ScriptableObject that defines the interface for all AI movement behaviors.
/// Concrete implementations of this class will contain specific movement algorithms (e.g., Chase, Flee).
/// </summary>
public abstract class MovementBehavior : ScriptableObject
{
    /// <summary>
    /// The core method of the behavior. This is called by the AI controller to execute the movement logic.
    /// </summary>
    /// <param name="agent">The NavMeshAgent component of the AI entity.</param>
    /// <param name="player">A reference to the player's transform, which is the primary target.</param>
    /// <param name="self">A reference to the AI's own transform.</param>
    public abstract void ExecuteMove(NavMeshAgent agent, Transform player, Transform self);
}


3.2. Step 2: Create Concrete Movement Behaviors
Implement a few concrete behaviors to demonstrate the system's functionality. Designers can use these as templates to create more.
File: Assets/Scripts/AI/Movement/Behaviors/ChaseBehavior.cs
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// A movement behavior where the AI actively chases the player.
/// </summary>
[CreateAssetMenu(fileName = "New ChaseBehavior", menuName = "AI/Movement Behaviors/Chase")]
public class ChaseBehavior : MovementBehavior
{
    [Tooltip("The speed at which the AI moves while chasing the player.")]
    public float moveSpeed = 3.5f;

    public override void ExecuteMove(NavMeshAgent agent, Transform player, Transform self)
    {
        // Null checks to prevent runtime errors.
        if (agent == null || player == null)
        {
            return;
        }

        // Apply the specified move speed to the agent.
        agent.speed = moveSpeed;

        // Set the agent's destination to the player's current position.
        agent.SetDestination(player.position);
    }
}


File: Assets/Scripts/AI/Movement/Behaviors/FleeBehavior.cs
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// A movement behavior where the AI attempts to flee from the player.
/// </summary>
[CreateAssetMenu(fileName = "New FleeBehavior", menuName = "AI/Movement Behaviors/Flee")]
public class FleeBehavior : MovementBehavior
{
    [Tooltip("The speed at which the AI moves while fleeing.")]
    public float moveSpeed = 5f;

    [Tooltip("How far the AI will try to get from the player when it flees.")]
    public float fleeDistance = 10f;

    public override void ExecuteMove(NavMeshAgent agent, Transform player, Transform self)
    {
        // Null checks to prevent runtime errors.
        if (agent == null || player == null || self == null)
        {
            return;
        }

        // Apply the specified move speed to the agent.
        agent.speed = moveSpeed;

        // Calculate a point away from the player to flee to.
        Vector3 directionFromPlayer = (self.position - player.position).normalized;
        Vector3 fleeDestination = self.position + directionFromPlayer * fleeDistance;

        // Set the agent's destination to the calculated flee point.
        agent.SetDestination(fleeDestination);
    }
}


3.3. Step 3: Modify EnemyData ScriptableObject
Update the EnemyData class to include a list of available movement behaviors. This is where designers will assign the behaviors to an enemy type. I am assuming EnemyData inherits from EntityData.
File: Assets/Scripts/Data/EnemyData.cs (or equivalent)
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject containing all data for a specific enemy type, including stats and behaviors.
/// </summary>
[CreateAssetMenu(fileName = "New EnemyData", menuName = "Data/Enemy Data")]
public class EnemyData : EntityData // Assuming inheritance from EntityData
{
    [Header("Movement")]
    [Tooltip("The default movement speed. Can be overridden by specific MovementBehaviors.")]
    public float moveSpeed = 3.5f;

    [Tooltip("The distance from the player at which the enemy will stop moving towards them.")]
    public float stoppingDistance = 1.5f;

    [Header("Combat")]
    [Tooltip("The range at which the enemy will detect the player and transition to a combat state.")]
    public float detectionRange = 15f;
    
    [Tooltip("The range within which the enemy can perform an attack.")]
    public float attackRange = 2f;

    [Tooltip("The number of attacks the enemy can perform per second.")]
    public float attackSpeed = 1f;

    [Tooltip("The amount of damage dealt by each attack.")]
    public float attackDamage = 10f;

    [Header("Behaviors")]
    [Tooltip("A list of all movement behaviors this enemy can potentially use.")]
    public List<MovementBehavior> movementBehaviors;

    [Tooltip("The default movement behavior to use when the enemy first enters a combat state.")]
    public MovementBehavior initialBehavior;
}


3.4. Step 4: Refactor EnemyAI Controller
This is the most significant change. The EnemyAI script will be updated to manage and execute the behaviors from the EnemyData list.
File: Assets/Scripts/EnemyAI.cs
using UnityEngine;
using UnityEngine.AI;
using System.Linq; // Required for FindBehaviorOfType

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    // --- Existing Variables ---
    [Tooltip("ScriptableObject containing enemy stats such as move speed, attack range, and damage.")]
    public EnemyData enemyData;
    private NavMeshAgent navMeshAgent;
    private Transform player;
    private float attackCooldown;
    private Health healthComponent;
    private ObjectPooler enemyPooler;

    // --- NEW: Behavior Management Variables ---
    [Header("Behavior Debugging")]
    [Tooltip("The current movement behavior being executed. (Read-Only)")]
    [SerializeField] // Visible in inspector for debugging but not editable.
    private MovementBehavior currentMovementBehavior;

    // Modified state machine to be more generic.
    private enum EnemyState
    {
        IDLE,
        COMBAT, // Replaces CHASING and ATTACKING
        DEAD
    }
    private EnemyState currentState = EnemyState.IDLE;

    // --- Existing Methods (Awake, Start, OnEnable, OnDisable) ---
    // ... (No major changes needed in Awake, OnEnable, OnDisable)

    private void Start()
    {
        // Initialize components
        navMeshAgent = GetComponent<NavMeshAgent>();
        healthComponent = GetComponent<Health>();
        
        // Find Player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogError("[EnemyAI] Player GameObject with tag 'Player' not found in the scene.");
        }

        // Subscribe to death event
        if (healthComponent != null)
        {
            healthComponent.OnDied += HandleDeath;
        }
        else
        {
             Debug.LogError("[EnemyAI] Health component is missing on " + gameObject.name);
        }

        // Apply initial settings from EnemyData
        if (enemyData != null)
        {
            navMeshAgent.stoppingDistance = enemyData.stoppingDistance;
            // Set the initial behavior from EnemyData
            if (enemyData.initialBehavior != null)
            {
                currentMovementBehavior = enemyData.initialBehavior;
            }
        }
        else
        {
            Debug.LogError("[EnemyAI] EnemyData is not assigned on " + gameObject.name);
        }
    }

    private void Update()
    {
        if (player == null || currentState == EnemyState.DEAD || player.GetComponent<Health>()?.CurrentHealth <= 0)
        {
            // If no player, we are dead, or the player is dead, do nothing.
            if (navMeshAgent.hasPath) navMeshAgent.ResetPath();
            return;
        }

        // Main state machine logic
        switch (currentState)
        {
            case EnemyState.IDLE:
                HandleIdleState();
                break;
            case EnemyState.COMBAT:
                HandleCombatState();
                break;
        }
    }

    private void HandleIdleState()
    {
        // Transition to COMBAT if the player is within detection range
        if (Vector3.Distance(transform.position, player.position) <= enemyData.detectionRange)
        {
            currentState = EnemyState.COMBAT;
        }
    }

    /// <summary>
    /// REFACTORED: This state now handles all combat logic, including
    /// deciding which behavior to use and when to attack.
    /// </summary>
    private void HandleCombatState()
    {
        // --- Behavior Selection Logic ---
        // Example: If health is low, try to find and switch to a FleeBehavior.
        if (healthComponent.CurrentHealth < healthComponent.MaxHealth * 0.25f)
        {
            SwitchBehavior<FleeBehavior>();
        }
        // Add more complex selection logic here as needed.
        // e.g., if player is far, switch to chase. if player is close, switch to strafe.


        // --- Movement Execution ---
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Only move if we are outside attack range.
        if (distanceToPlayer > enemyData.attackRange)
        {
            // Execute the current movement behavior.
            if (currentMovementBehavior != null)
            {
                currentMovementBehavior.ExecuteMove(navMeshAgent, player, transform);
            }
        }
        else // We are within attack range
        {
            // Stop moving to perform an attack.
            if(navMeshAgent.hasPath) navMeshAgent.ResetPath();

            // --- Attack Logic ---
            if (attackCooldown <= 0f)
            {
                Attack();
                attackCooldown = 1f / enemyData.attackSpeed; // Reset cooldown
            }
        }

        // Always tick down the attack cooldown while in combat.
        if (attackCooldown > 0)
        {
            attackCooldown -= Time.deltaTime;
        }
    }

    /// <summary>
    /// NEW: A generic helper method to find and switch to a specific type of behavior
    /// from the enemy's available list.
    /// </summary>
    /// <typeparam name="T">The type of MovementBehavior to find.</typeparam>
    private void SwitchBehavior<T>() where T : MovementBehavior
    {
        // If we are already using this type of behavior, no need to switch.
        if (currentMovementBehavior is T || enemyData?.movementBehaviors == null)
        {
            return;
        }

        // Find the first behavior of the requested type in our list.
        var desiredBehavior = enemyData.movementBehaviors.OfType<T>().FirstOrDefault();
        if (desiredBehavior != null)
        {
            // Switch to the new behavior.
            currentMovementBehavior = desiredBehavior;
        }
    }

    private void HandleDeath()
    {
        currentState = EnemyState.DEAD;
        // ... existing death logic (pooling, vfx, etc)
    }

    private void ResetEnemyState()
    {
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
        // Reset to the initial behavior
        if (enemyData != null && enemyData.initialBehavior != null)
        {
            currentMovementBehavior = enemyData.initialBehavior;
        }
    }

    // --- Attack, TakeDamage, etc. methods remain largely unchanged ---
    // ...
}


4. Designer Workflow
With this system in place, the workflow for a designer to create a new movement pattern is as follows:
Create Behavior Asset: In the Project window, right-click and navigate to Create > AI/Movement Behaviors. Select the desired behavior type (e.g., Chase).
Configure Behavior: Select the newly created asset and configure its properties (e.g., moveSpeed) in the Inspector.
Assign to Enemy: Select the EnemyData ScriptableObject for the desired enemy type.
Add to List: Drag the new MovementBehavior asset from the Project window into the Movement Behaviors list in the EnemyData's Inspector.
Set Initial Behavior: (Optional) Drag a behavior into the Initial Behavior slot to define the default movement.
The enemy prefab using this EnemyData is now equipped with the new behavior and the EnemyAI's logic can decide to use it at runtime.
5. Future Considerations
Advanced Behavior Selection: The HandleCombatState method can be expanded with more sophisticated logic for choosing behaviors based on timers, player actions, or environmental factors.
Behavior-Specific Data: Behaviors can be expanded to include more unique data. For example, a PatrolBehavior could contain a list of Vector3 waypoints.
Composite Behaviors: A CompositeBehavior could be created that holds a list of other behaviors and executes them in sequence, adding another layer of complexity.
Animation Integration: Behaviors could include fields for animation triggers (e.g., string animationTriggerName) to better sync movement with character animation. This would be a great place to collaborate with you, the sound designer, to also trigger specific movement sounds (footsteps, wooshes, etc.) from the behavior assets.
