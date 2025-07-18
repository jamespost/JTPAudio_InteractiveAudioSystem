### **Introduction**

This plan expands on your Game Design Document, turning each checklist item into a concrete development task. It outlines suggested script names, implementation logic for Unity, and specific points where audio hooks should be placed. This will allow you to build the game's "feel" right alongside its functionality.

### **Milestone 1: The "Grey Box" Prototype (1-2 Weeks)**

**Goal:** The focus here is pure mechanics. We want to get a character moving and shooting in a test environment as quickly as possible. Ignore aesthetics completely. Think of this as building the raw engine of the car before worrying about the paint job.

#### **Key Features & Tasks:**

* **Player Controller**  
  * **Component:** Use Unity's CharacterController component. It's generally easier for non-physics-based FPS movement than a Rigidbody.  
  * **Script:** Create a PlayerMovement.cs.  
    * **Movement (WASD):** In the Update() method, get input using Input.GetAxis("Horizontal") and Input.GetAxis("Vertical"). Combine these into a Vector3 to define the direction. Use characterController.Move(direction \* speed \* Time.deltaTime) to move the player.  
    * **Look (Mouse):** Get mouse input with Input.GetAxis("Mouse X") and Input.GetAxis("Mouse Y"). Use the X input to rotate the entire player body horizontally. Use the Y input to rotate only the player's camera vertically, clamping the rotation to prevent looking all the way up and down.  
    * **Jumping:** In Update(), check for Input.GetButtonDown("Jump"). Before applying upward force, also check if characterController.isGrounded is true to prevent double-jumping.  
    * **Sprinting:** In Update(), check if the sprint key is held down (Input.GetKey(KeyCode.LeftShift)). If it is, use a higher sprintingSpeed variable in your movement calculation; otherwise, use the normal walkingSpeed.  
    * **Audio Hooks:**  
      * **Footsteps:** Even now, you can add a placeholder. In PlayerMovement.cs, check if the player is grounded and moving. If so, trigger a footstep sound on a timer. You can have separate calls for walking and sprinting to test the logic.  
* **Core Combat**  
  * **Basic Weapon (Pistol)**  
    * **Script:** WeaponController.cs (this will manage the player's ability to shoot).  
    * **Functionality:**  
      * **Fire:** In Update(), check for Input.GetButtonDown("Fire1"). When pressed, perform a Physics.Raycast from the center of the camera forward.  
      * **Hit Detection:** If the raycast hits an object, check if it has an "Enemy" tag or a Health.cs component.  
      * **Ammo:** Use an int currentAmmo variable. The Fire() function should only work if currentAmmo \> 0\. Decrement the ammo count with each shot.  
      * **Reload:** Check for Input.GetKeyDown(KeyCode.R). When pressed, start a coroutine or timer to simulate reload time. When it finishes, set currentAmmo back to maxAmmo.  
    * **Audio Hooks:**  
      * In Fire(), if a shot is successful, call your AudioManager to play the pistol fire sound.  
      * If the player tries to fire with 0 ammo, call the "empty clip click" sound.  
      * At the start and end of the reload function, you can place hooks for "clip out" and "clip in" sounds.  
  * **Player Health**  
    * **Script:** Health.cs. This script will be on the Player.  
    * **Functionality:** Create a public function TakeDamage(int damageAmount). This will subtract from an int currentHealth variable. In this function, check if currentHealth \<= 0\. If so, trigger the game over state.  
* **Enemy AI**  
  * **Single Enemy Type**  
    * **GameObject:** Create a simple "Enemy" prefab. A Unity capsule or cylinder is perfect for this. Give it a NavMeshAgent component.  
    * **Script:** EnemyAI.cs.  
    * **AI Behavior:**  
      * **Pathfinding:** In the Unity editor, create a NavMesh for your level (Window \> AI \> Navigation). In the enemy's Update() method, use navMeshAgent.SetDestination() to target the player's transform position. This is all you need for basic "move to player" behavior.  
      * **Attack:** In Update(), check the navMeshAgent.remainingDistance. If the distance is less than your desired attack range (e.g., 2 meters), stop the agent from moving and call an Attack() function.  
      * **Melee Damage:** The Attack() function should find the Health.cs component on the player and call its TakeDamage() method. Add a cooldown so the enemy doesn't attack every single frame.  
    * **Audio Hooks:**  
      * In the Attack() function, add a hook for the enemy's melee swipe/attack sound.  
      * When the enemy is destroyed, add a hook for its death sound.  
* **Environment**  
  * **Test Arena:** Create a new scene. Use a Plane for the floor. Use scaled Cube objects for walls and a few obstacles. Do not add any textures or materials yet. This is about layout, not looks.

### **Milestone 2: The Game Loop (1 Week)** - **Status: [COMPLETE]**

**Goal:** To wrap the core mechanics in a playable structure. A player should be able to start, play, and end a game session.

#### **Key Features & Tasks:**

* **Wave Management System**  
  * **Script:** Create an empty GameObject called GameManager and attach a WaveManager.cs script.  
  * **Logic:**  
    * Define an array of Transforms to act as spawn points.  
    * Use a coroutine to manage the wave loop.  
    * **SpawnWave() Coroutine:**  
      1. Increment the waveNumber.  
      2. Use a for loop to instantiate a number of enemies (e.g., waveNumber \* 3). Place them at random spawn points. Add each new enemy to a List\<EnemyAI\>.  
      3. Use yield return new WaitUntil(() \=\> enemies.Count \== 0); to pause the coroutine until all enemies are defeated. (You'll need to make sure enemies remove themselves from this list upon death).  
      4. Announce "Wave Complete".  
      5. yield return new WaitForSeconds(5f); to give the player a break.  
      6. Loop back to step 1\.  
  * **Audio Hooks:**  
    * When the SpawnWave coroutine begins, call the "Wave Start" announcer sound.  
    * After the WaitUntil, call the "Wave Complete" sound.  
* **User Interface (UI)**  
  * **Setup:** In your scene, create a Canvas (GameObject \> UI \> Canvas).  
  * **Elements:** Add TextMeshPro \- Text objects to the canvas for Health, Ammo, and Wave Number.  
  * **Script:** Create a UIManager.cs script and attach it to the Canvas.  
    * **Functionality:** Create public functions like UpdateHealthText(int health), UpdateAmmoText(int current, int max), and UpdateWaveText(int wave).  
    * **Connections:** The Player's Health.cs will call UIManager.UpdateHealthText() whenever it takes damage. The WeaponController.cs will call UIManager.UpdateAmmoText() when it fires or reloads. The WaveManager.cs will call UIManager.UpdateWaveText() at the start of each wave.  
* **Game State Management**  
  * **Scenes:** Create three scenes: MainMenu, Game, and GameOver.  
  * **Main Menu:** The MainMenu scene will have a Canvas with a "Start Game" button. The button's OnClick() event will call a function that uses UnityEngine.SceneManagement.SceneManager.LoadScene("Game");.  
  * **Game Over:** When the player's health reaches zero, call a function in your GameManager to load the GameOver scene. This scene's UI will display the final wave reached and have a "Restart" button that loads the Game scene again.

### **Milestone 3: The "Juice" \- Polish & Game Feel (2 Weeks)**

**Goal:** This is your specialty. The goal is to make every action feel impactful and satisfying. A game with great "juice" feels fun to play even when you're doing simple things.

#### **Key Features & Tasks:**

* **Sound Design Implementation**  
  * **Setup:** Create a dedicated AudioManager.cs that persists across scenes (DontDestroyOnLoad). This script will hold all your audio clips and have public functions to play them by name (e.g., PlaySound("PistolFire")). This centralizes your audio, making it easy to manage.  
  * **Go back through all the scripts from Milestones 1 & 2 and replace any placeholder audio calls with proper calls to your new AudioManager.** This is where you implement all the sounds you listed: weapon fire, reloads, empty clicks, footsteps, damage grunts, death sounds, enemy noises, UI clicks, and announcers.  
* **Visual Feedback**  
  * **Weapon Effects:**  
    * **Muzzle Flash:** Parent a small, bright ParticleSystem or a Light component to the tip of the gun barrel. When Fire() is called, enable it for a fraction of a second.  
    * **Shell Ejection:** Instantiate a small "shell casing" prefab with a Rigidbody at an ejector port Transform on your weapon model. Give it a small sideways force.  
  * **Impact Effects:**  
    * Create two ParticleSystem prefabs: one for hitting walls (sparks, dust) and one for hitting enemies (blood, goo).  
    * In your WeaponController.cs, when your raycast hits something, check the tag of the object hit. If it's "Enemy", instantiate the blood effect at raycastHit.point. If it's "Environment", instantiate the sparks effect.  
  * **Hit-Marker:**  
    * On your UI Canvas, add a small Image at the center (your crosshair). By default, it should be disabled.  
    * In WeaponController.cs, if your raycast successfully hits an enemy, enable this UI image. Start a short coroutine to disable it again after \~0.1 seconds.  
  * **Player Feedback (Damage Vignette):**  
    * On your UI Canvas, add a full-screen Image with a red, feathered vignette texture. Set its color to be transparent by default.  
    * In the player's Health.cs, in the TakeDamage() function, start a coroutine that quickly fades this image's alpha up to \~50% and then fades it back down to 0 over half a second.

### **Milestone 4: Content & Environment (1-2 Weeks)**

**Goal:** To build out from the core loop, adding variety and a more believable space to play in. This turns the proof-of-concept into a more complete game slice.

#### **Key Features & Tasks:**

* **Final Environment Art**  
  * **Arena Design:** Replace your grey box cubes with actual 3D models. Focus on gameplay first: create interesting sightlines, areas of cover, and maybe some verticality (ramps, platforms). Good flow is more important than detailed models. Use a modular kit if possible to speed up construction.  
  * **Lighting:** Use a mix of a single Directional Light for global illumination and several Point Lights or Spot Lights to create mood and highlight important areas (like spawn doors or choke points). Consider baking your lighting (Window \> Rendering \> Lighting) for much better performance.  
* **Gameplay Variety**  
  * **New Weapon (Shotgun Example):**  
    * **Logic:** In WeaponController.cs, create a new fire mode. Instead of one Physics.Raycast, use a for loop to fire 5-8 raycasts at once. For each raycast, add a slight random offset to its direction to create a spread pattern. Each ray that hits an enemy applies damage.  
    * **Weapon Switching:** Add a WeaponManager.cs to your player. It will hold an array of your weapon GameObjects. Based on Input (mouse wheel or number keys), it will enable the desired weapon's GameObject and disable the others.  
  * **New Enemy (Ranged Example):**  
    * **Behavior:** Copy your EnemyAI.cs to RangedEnemyAI.cs. Change the logic so that it uses the NavMeshAgent.stoppingDistance property to keep its distance from the player.  
    * **Attacking:** When it stops in range, instead of a melee attack, it will instantiate a "projectile" prefab.  
    * **Projectile:** Create a projectile prefab (e.g., a sphere with a Rigidbody and a Projectile.cs script). The RangedEnemyAI will give it a velocity towards the player. The Projectile.cs script will use OnCollisionEnter to detect what it hit. If it hits the player, it calls the player's TakeDamage() function and then destroys itself.  
  * **Audio Hooks:**  
    * Don't forget sounds for the new weapon and new enemy\! The ranged enemy will need a projectile firing sound and a projectile impact sound.  
* **Pickups**  
  * **Spawning:** In the Health.cs script (used by enemies), modify the Die() function. Add a check, like if (Random.Range(0f, 1f) \<= dropChance), to determine if a pickup should spawn. If so, Instantiate() a health or ammo pickup prefab at the enemy's position.  
  * **Pickup Logic:**  
    * Create an "Ammo" and "Health" pickup prefab. Give them a collider set to be a Trigger.  
    * Attach a Pickup.cs script. This script will have a public enum to define its type (Health or Ammo).  
    * Use the OnTriggerEnter(Collider other) method. Check if other.CompareTag("Player"). If it is, give the player the appropriate resource (call a function on the WeaponController to add ammo, or on the Health script to add health).  
    * **Audio Hook:** When the pickup is collected, play a satisfying "power-up" sound.  
    * Finally, Destroy(gameObject) to remove the pickup from the world.