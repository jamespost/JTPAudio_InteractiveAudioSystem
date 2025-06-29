# **Project Resonance: Technical Brief & Programming Guidelines**

## **Code Style & Best Practices**

- **Consistency:** Follow C# naming conventions (PascalCase for classes/methods, camelCase for variables).
- **Comments:** Use XML documentation for public APIs and concise inline comments for complex logic.
- **Events & Audio:** Always reference our existing EventManager and AudioManager systems for event handling and audio playback. Do not implement new event or audio systems—extend or use the established patterns.
- **Inspector Exposure:** Use `[Header]`, `[Tooltip]`, and `[SerializeField]` to keep the Inspector organized and designer-friendly.
- **Single Responsibility:** Each script/class should have one clear purpose.
- **Avoid Hardcoding:** Use ScriptableObjects for all tunable data.

> **Note:** Before introducing new patterns or systems, review how similar functionality is handled in our current architecture, especially for events and audio, to maintain project cohesion.

1.  **Objective**
    1.  This document outlines the technical implementation strategy for *Project Resonance*. The primary goal is to build a robust, performant, and scalable foundation for a wave-based FPS.

2.  **Core Pillars**
    1.  **Modularity & Decoupling:** Systems should be self-contained and communicate indirectly. Avoid hard-coded dependencies between core systems like AI, Player, and UI.
    2.  **Data-Driven Design:** Game variables (weapon stats, enemy health, wave composition) must be handled through ScriptableObjects. This empowers rapid design iteration without code changes.
    3.  **Performance by Design:** Prioritize efficient algorithms and memory management from the start. We will be spawning many enemies, so performance is key.
    4.  **Designer-Friendly Inspector:** All key gameplay variables must be exposed and clearly labeled in the Unity Inspector. A designer should never need to open a code file to tweak game balance, timing, or "game feel" elements.


3.  **Core Architectural Mandates [COMPLETED]**
    1.  **Event-Driven Architecture [COMPLETED]**
        1.  **Implementation:** Create a static EventManager class or use C# Action events within individual components. This will be the primary method of cross-system communication.
        2.  **Example Events:** OnPlayerDamaged(float damage), OnEnemyDied(Vector3 position), OnWaveStateChanged(WaveState newState), OnAmmoChanged(int current, int max).
        3.  **Designer Rationale:** This is critical for roles like sound design. It allows an AudioManager to subscribe to any gameplay event and play the appropriate sound without ever needing to modify player, weapon, or enemy code. A sound designer can wire up the entire game's soundscape from one central location.
    2.  **Data-Centric ScriptableObjects [COMPLETED]**
        1.  **Implementation:** For any entity with unique stats (weapons, enemies, pickups), create a corresponding ScriptableObject data container.
        2.  **Examples:** WeaponData (damage, fire rate, clip size, reload speed), EnemyData (move speed, health, attack damage, attack range).
        3.  **Designer Rationale:** This is the primary method for achieving a designer-friendly workflow. It allows for creating endless variations of game content by simply creating new assets in the Project window. A designer can balance the entire game without writing a line of code.
    3.  **Object Pooling [COMPLETED]**
        1.  **Implementation:** Create a generic ObjectPooler class early on. It will manage the lifecycle of frequently instantiated objects like projectiles, impact VFX, and shell casings.
        2.  **Rationale:** Drastically reduces garbage collection and CPU overhead from repeated Instantiate() and Destroy() calls, which is critical for maintaining a smooth framerate during intense waves.

4.  **Milestone 1: Core Systems Implementation (1-2 Weeks) [COMPLETED]**
    1.  **Goal:** Build the foundational, data-driven, and designer-accessible systems.
    2.  **Player Controller (PlayerController.cs) [COMPLETED]**
        1.  **Movement:** Utilize Unity's CharacterController.
        2.  **Input Handling:** Isolate input polling to its own function or script.
        3.  **Inspector Cleanup:** Use [Header("Movement Stats")] and [Tooltip("...")] attributes to clearly organize and explain variables like walkSpeed, sprintSpeed, and jumpHeight in the Inspector.
    3.  **Universal Health & Damage System (Health.cs) [COMPLETED]**
        1.  **Component:** A single Health.cs script for both Player and Enemies.
        2.  **Data:** It must reference a ScriptableObject (e.g., EntityData) to set its maxHealth.
        3.  **API:** A public TakeDamage(float amount) method.
        4.  **Events:** It must fire off events: public event Action<float> OnDamaged; and public event Action OnDied;.
        5.  **Inspector Cleanup:** Clearly expose the EntityData slot and any damage multipliers for easy tweaking.
    4.  **Weapon System (WeaponController.cs) [COMPLETED]**
        1.  **Data-Driven:** The controller must be a lightweight "shell" that gets all its core stats (damage, fire rate, clip size, reload speed, audio clips) from an assigned WeaponData ScriptableObject.
        2.  **Events:** It must fire events for other systems to consume: OnFire(), OnReload(), OnHit(bool isEnemy).
        3.  **Inspector Cleanup:** The only thing a designer should need to do to this component is drag in a WeaponData asset.
        4.  **Extendable:** The weapon system needs to be able to handle any new type of weapon designers come up with, including switching between different weapon types during gameplay. This milestone only needs to focus on the player using one weapon, but designers should be able to add an arbitrary number of new weapons over time
    5.  **Enemy AI (EnemyAI.cs) [COMPLETED]**
        1.  **Goal:** Implement a simple 3D object (e.g., a colored capsule or cube) as a stand-in enemy prefab. This stand-in must demonstrate fully functional AI covering all features below, allowing designers and programmers to test and iterate on enemy behavior before final art is available.
        2.  **State Machine:** Implement a formal state machine (CHASING, ATTACKING, IDLE).
        3.  **Pathfinding:** Use NavMeshAgent and optimize SetDestination calls.
        4.  **Inspector Cleanup:** Expose key variables from its EnemyData like attackRange, attackSpeed, and stoppingDistance (and movement related info). Use [Header("AI Behavior")] to separate these from other component variables.

**Sub-Milestone 1.5: Weapon Hit Detection and Feedback (1 Week)**
    1.  **Goal:** Enhance the WeaponController to detect hits, apply damage to objects with a Health component, and provide visual and audio feedback.
    2.  **WeaponController Enhancements:**
        1.  **Hit Detection:**
            - Implement raycasting or projectile-based hit detection.
            - Ensure accurate detection of objects hit by the weapon.
        2.  **Damage Application:**
            - Check if the hit object has a Health component.
            - Apply damage to the Health component of the hit object.
        3.  **Feedback Systems:**
            - **Visual Feedback:**
                - Spawn impact VFX (e.g., sparks, blood splatter) at the hit location.
                - Use Object Pooling for VFX to optimize performance.
            - **Audio Feedback:**
                - Play appropriate sound effects (e.g., bullet impact, ricochet) based on the material of the hit object. Always use AudioManager.PostEvent for triggering ANY audio
    3.  **Testing and Debugging:**
        - Ensure the system works seamlessly with existing Health, EventManager, and AudioManager systems.
        - Test with various object types (e.g., enemies, environment props) to verify functionality.
    4.  **Designer Accessibility:**
        - Expose VFX and SFX settings in the Inspector for easy customization.
        - Allow designers to assign different feedback assets (both VFX and SFX) for different materials or object types.

5.  **Milestone 2: Game Loop Architecture (1 Week)**
    1.  **Goal:** Structure the gameplay flow with a focus on designer-editable waves.
    2.  **Game Manager (GameManager.cs)**
        1.  **Singleton:** A persistent singleton managing the overall application state (MAIN_MENU, PLAYING, GAME_OVER).
    3.  **Wave System (WaveManager.cs)**
        1.  **Data-Driven Waves:** Create a WaveData ScriptableObject. This asset will contain a list of spawn groups (whatToSpawn, count, spawnPoint, delay). **This is essential.** It turns wave design into a visual, data-entry task in the Inspector.
        2.  **Spawning:** The WaveManager reads the WaveData asset for the current level, spawns enemies accordingly, and subscribes to their OnDied events to track progress. **All enemy spawning must be handled through the ObjectPooler to ensure optimal performance and avoid unnecessary instantiation.**
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

