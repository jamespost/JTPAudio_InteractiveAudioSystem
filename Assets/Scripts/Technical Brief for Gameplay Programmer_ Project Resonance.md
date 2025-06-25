# **Project Resonance: Technical Brief & Programming Guidelines**

1.  **Objective**
    1.  This document outlines the technical implementation strategy for *Project Resonance*. The primary goal is to build a robust, performant, and scalable foundation for a wave-based FPS.

2.  **Core Pillars**
    1.  **Modularity & Decoupling:** Systems should be self-contained and communicate indirectly. Avoid hard-coded dependencies between core systems like AI, Player, and UI.
    2.  **Data-Driven Design:** Game variables (weapon stats, enemy health, wave composition) must be handled through ScriptableObjects. This empowers rapid design iteration without code changes.
    3.  **Performance by Design:** Prioritize efficient algorithms and memory management from the start. We will be spawning many enemies, so performance is key.
    4.  **Designer-Friendly Inspector:** All key gameplay variables must be exposed and clearly labeled in the Unity Inspector. A designer should never need to open a code file to tweak game balance, timing, or "game feel" elements.

3.  **Core Architectural Mandates**
    1.  **Event-Driven Architecture [COMPLETED]**
        1.  **Implementation:** Create a static EventManager class or use C# Action events within individual components. This will be the primary method of cross-system communication.
        2.  **Example Events:** OnPlayerDamaged(float damage), OnEnemyDied(Vector3 position), OnWaveStateChanged(WaveState newState), OnAmmoChanged(int current, int max).
        3.  **Designer Rationale:** This is critical for roles like sound design. It allows an AudioManager to subscribe to any gameplay event and play the appropriate sound without ever needing to modify player, weapon, or enemy code. A sound designer can wire up the entire game's soundscape from one central location.
    2.  **Data-Centric ScriptableObjects [COMPLETED]**
        1.  **Implementation:** For any entity with unique stats (weapons, enemies, pickups), create a corresponding ScriptableObject data container.
        2.  **Examples:** WeaponData (damage, fire rate, clip size, reload speed), EnemyData (move speed, health, attack damage, attack range).
        3.  **Designer Rationale:** This is the primary method for achieving a designer-friendly workflow. It allows for creating endless variations of game content by simply creating new assets in the Project window. A designer can balance the entire game without writing a line of code.
    3.  **Object Pooling**
        1.  **Implementation:** Create a generic ObjectPooler class early on. It will manage the lifecycle of frequently instantiated objects like projectiles, impact VFX, and shell casings.
        2.  **Rationale:** Drastically reduces garbage collection and CPU overhead from repeated Instantiate() and Destroy() calls, which is critical for maintaining a smooth framerate during intense waves.

4.  **Milestone 1: Core Systems Implementation (1-2 Weeks)**
    1.  **Goal:** Build the foundational, data-driven, and designer-accessible systems.
    2.  **Player Controller (PlayerController.cs) [PARTIALLY COMPLETED]**
        1.  **Movement:** Utilize Unity's CharacterController.
        2.  **Input Handling:** Isolate input polling to its own function or script.
        3.  **Inspector Cleanup:** Use [Header("Movement Stats")] and [Tooltip("...")] attributes to clearly organize and explain variables like walkSpeed, sprintSpeed, and jumpHeight in the Inspector.
    3.  **Universal Health & Damage System (Health.cs)**
        1.  **Component:** A single Health.cs script for both Player and Enemies.
        2.  **Data:** It must reference a ScriptableObject (e.g., EntityData) to set its maxHealth.
        3.  **API:** A public TakeDamage(float amount) method.
        4.  **Events:** It must fire off events: public event Action<float> OnDamaged; and public event Action OnDied;.
        5.  **Inspector Cleanup:** Clearly expose the EntityData slot and any damage multipliers for easy tweaking.
    4.  **Weapon System (WeaponController.cs)**
        1.  **Data-Driven:** The controller must be a lightweight "shell" that gets all its core stats (damage, fire rate, clip size, reload speed, audio clips) from an assigned WeaponData ScriptableObject.
        2.  **Events:** It must fire events for other systems to consume: OnFire(), OnReload(), OnHit(bool isEnemy).
        3.  **Inspector Cleanup:** The only thing a designer should need to do to this component is drag in a WeaponData asset.
    5.  **Enemy AI (EnemyAI.cs)**
        1.  **State Machine:** Implement a formal state machine (CHASING, ATTACKING, IDLE).
        2.  **Pathfinding:** Use NavMeshAgent and optimize SetDestination calls.
        3.  **Inspector Cleanup:** Expose key variables from its EnemyData like attackRange, attackSpeed, and stoppingDistance. Use [Header("AI Behavior")] to separate these from other component variables.

5.  **Milestone 2: Game Loop Architecture (1 Week)**
    1.  **Goal:** Structure the gameplay flow with a focus on designer-editable waves.
    2.  **Game Manager (GameManager.cs)**
        1.  **Singleton:** A persistent singleton managing the overall application state (MAIN_MENU, PLAYING, GAME_OVER).
    3.  **Wave System (WaveManager.cs)**
        1.  **Data-Driven Waves:** Create a WaveData ScriptableObject. This asset will contain a list of spawn groups (whatToSpawn, count, spawnPoint, delay). **This is essential.** It turns wave design into a visual, data-entry task in the Inspector.
        2.  **Spawning:** The WaveManager reads the WaveData asset for the current level, spawns enemies accordingly, and subscribes to their OnDied events to track progress.
        3.  **Inspector Cleanup:** The WaveManager component itself should have a single, clear slot for the designer to drag the WaveData asset for that level.
    4.  **UI System (UIManager.cs)**
        1.  **Event-Driven:** The UIManager is entirely reactive, subscribing to events from other systems. It has no knowledge of game logic. This means a designer can change what a UI element does just by changing what event it listens to.

6.  **Milestone 3: Feedback & Polish Systems (2 Weeks)**
    1.  **Goal:** Implement "juice" via systems that are fully configurable by a designer.
    2.  **Audio System Integration [COMPLETED]**
        1.  An existing Audio System will be integrated into the project (relevant scripts should be under Scripts/Audio in the project files).
        2.  All other systems that will interact with audio need to be integrated accordingly with this system.
    3.  **VFX & Feedback System (FeedbackController.cs)**
        1.  **Object Pooling is Mandatory:** All feedback effects (muzzle flashes, impact particles, etc.) must be pooled.
        2.  **Event-Driven & Designer-Friendly:** Similar to the AudioManager, this system should allow a designer to link ParticleSystem prefabs to game events directly in the Inspector.

7.  **Milestone 4: Content Expansion (1-2 Weeks)**
    1.  **Goal:** Prove the architecture's flexibility by allowing for rapid, code-free content creation.
    2.  **New Content Workflow (Designer-Centric):**
        1.  **New Weapon:** Right-click in Project -> Create -> Data -> WeaponData. Fill out the stats, drag in the audio clips and VFX prefabs. Assign this new asset to the player. **Zero code required.**
        2.  **New Enemy:** Create a new EnemyData asset. Configure its stats. Drag it onto a new enemy prefab. Add the prefab to a WaveData asset. **Zero code required.**
    3.  **Systems Refinement:**
        1.  **Weapon Switching (WeaponManager.cs):** Should have a simple list in the Inspector where a designer can drag-and-drop the starting WeaponData assets for the player.
        2.  **Pickup System (Pickup.cs):** Pickups must be data-driven via a PickupData ScriptableObject that defines their type, value, model, and sound effects. A designer can create new pickup types just by creating new assets.