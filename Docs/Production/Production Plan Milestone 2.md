# **Production Plan: Milestone 2 - Game Loop Architecture**

**Goal:** To structure the core gameplay into a complete, replayable loop, managed by robust, data-driven, and event-based systems. This milestone focuses on creating the systems that will control the game flow from the main menu, through waves of enemies, to a game over screen.

---

**Estimated Time:** 1 Week

**Key Systems to Develop:**
1.  `GameManager.cs` (Game State Control)
2.  `WaveData.cs` (ScriptableObject for Wave Design)
3.  `WaveManager.cs` (Wave and Enemy Spawning Logic)
4.  `UIManager.cs` (HUD and UI Updates)

---

### **Step-by-Step Task Breakdown:**

#### **Task 1: Foundational Game State Management**

**Objective:** Create a `GameManager` to control the overall flow of the application between different states (Menu, Gameplay, Game Over).

1.  **Create Scenes: [COMPLETED]**
    *   Create three new Unity scenes: `MainMenu`, `Game`, and `GameOver`.

2.  **Implement `GameManager.cs`: [COMPLETED]**
    *   Create the `GameManager.cs` script.
    *   Implement it as a persistent singleton (using `DontDestroyOnLoad`) to ensure it exists across all scenes.
    *   Define an `enum` for game states: `MainMenu`, `Playing`, `GameOver`.
    *   Create public methods to manage state transitions, e.g., `GoToState(GameState newState)`. These methods will handle loading the appropriate scenes (`SceneManager.LoadScene`).

3.  **Basic Scene UI: [COMPLETED]**
    *   Utilize PauseMenuController to handle dynamic menu elements and calls to the game manager to handle scene changes

#### **Task 2: Data-Driven Wave System [COMPLETED]**

**Objective:** Design and implement a flexible wave system where all wave properties are defined in data assets, not in code. This is a critical step for enabling rapid design iteration.

1.  **Create `WaveData` ScriptableObject: [COMPLETED]**
    *   Create a new C# script, `WaveData.cs`, that inherits from `ScriptableObject`.
    *   Inside this script, define a serializable class or struct for a single spawn instruction, e.g., `SpawnGroup`, which should contain:
        *   `GameObject whatToSpawn` (The enemy prefab)
        *   `int count` (How many to spawn)
        *   `Transform spawnPoint` (Where to spawn them)
        *   `float delay` (Delay before this group spawns)
    *   The `WaveData` asset itself should contain a list or array of these `SpawnGroup` objects. This single asset will define an entire wave.
    *   Create a second ScriptableObject, `LevelData.cs`, that holds an array of `WaveData` assets. This allows you to define the sequence of all waves for a level.

2.  **Implement `WaveManager.cs`: [COMPLETED]**
    *   [COMPLETED] Create the `WaveManager.cs` script and attach it to a `GameManager` GameObject in the `Game` scene.
    *   [COMPLETED] Add a public field to assign the `LevelData` asset for the current level.
    *   Implement the main `SpawnWave` coroutine. This coroutine will:
        *   Iterate through the `WaveData` assets in the assigned `LevelData`.
        *   For each wave, post the "Wave Start" audio event using `AudioManager.PostEvent`.
        *   Read the `SpawnGroup` list from the current `WaveData`.
        *   **Crucially, use the existing `ObjectPooler` to request and spawn enemies instead of `Instantiate()`**.
        *   Subscribe to the `OnDied` event of each spawned enemy to track how many are left.
        *   Once all enemies in a wave are defeated, post the "Wave Complete" audio event.
        *   Wait for a short delay before starting the next wave.

#### **Task 3: Event-Driven User Interface [COMPLETED]**

**Objective:** Create a reactive UI system that updates automatically by listening to game events, without holding any direct references to game logic controllers.

1.  **Implement `UIManager.cs`:**
    *   Create the `UIManager.cs` script and attach it to a `Canvas` GameObject in the `Game` scene.
    *   In its `OnEnable` method, subscribe to events from other systems (e.g., `Player.OnHealthChanged`, `Weapon.OnAmmoChanged`, `WaveManager.OnWaveChanged`, anything that wavemanager or enemys broadcast).
    *   In its `OnDisable` method, unsubscribe from all events to prevent memory leaks.
    *   Create handler functions that take the relevant data (e.g., `HandleHealthUpdate(int newHealth)`) and update the UI text elements.

2.  **Create HUD Elements:**
    *   On the UI Canvas, add `TextMeshPro - Text` objects for:
        *   Health
        *   Ammo (e.g., "30/150")
        *   Wave Number (e.g., "Wave: 1")
    *   Connect these text objects to the `UIManager.cs` script.

3.  **Integrate Events:**
    *   Ensure the `Health.cs`, `WeaponController.cs`, and `WaveManager.cs` scripts have the necessary public events (`public static event Action<int> OnHealthChanged;`, etc.) and that they are invoked at the correct times (when damage is taken, a shot is fired, or a new wave starts).

---

**Milestone 2 Completion Criteria:**
*   The game is fully playable from a main menu.
*   The `WaveManager` correctly spawns sequential waves of enemies based on the data set in the `LevelData` and `WaveData` ScriptableObjects.
*   The UI correctly displays player health, ammo, and the current wave number, updating in real-time based on game events.
*   Upon player death, a "Game Over" screen is shown with an option to restart, successfully looping the gameplay.
*   All enemy spawning is handled by the object pooler, and all audio is triggered via the `AudioManager`.

---

### **Known Issues**

The following issues must be resolved before Milestone 2 can be considered complete:

1. **Enemy Spawn Colliders:[FIXED]** Enemies spawn with colliders that "bump" the player. This may be caused by enemies pooling from the same location as the player start when their colliders are activated.

2. **Wave Count UI: [FIXED]** The current wave count does not properly update in the UI.

3. **Missing Win State:[FIXED]** There is no "success" or "win state" when all waves are defeated. The dynamic ui wave count should say all waves completed, the game should pop up the pause menu but the pause menu should say in big text "YOU WON" and have the menu options for restarting the level, exiting to main menu, or exiting to desktop like it does in a normal pause setup.

4. **Placeholder Text in UI:** Player health and ammo display placeholder text at the start of the level. These values only update once their respective systems send event messages to the corresponding UI elements.
