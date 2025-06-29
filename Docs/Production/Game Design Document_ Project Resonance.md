# **Game Design Document: Project Resonance**

*A Milestone-Based Plan for a Solo-Developed Wave FPS*

### **1\. Game Overview**

* **Concept:** A fast-paced, wave-based first-person shooter where the player must survive against increasingly difficult hordes of enemies in a single, contained arena.  
* **Genre:** Wave Survival FPS  
* **Target Platform:** PC (Standalone)  
* **Core Loop:** Survive Wave \-\> Prepare \-\> Survive Next Wave \-\> Repeat.  
* **Unique Selling Point (for this PoC):** A tight, satisfying core combat loop with a strong focus on "game feel" through impactful visuals and high-quality audio.

### **Milestone 1: The "Grey Box" Prototype (1-2 Weeks)**

**Goal:** To create the absolute minimum playable experience. Focus entirely on functionality, not aesthetics. The game should be mechanically playable by the end of this milestone.

**Key Features & Tasks:**

* **Player Controller:**  
  * \[ \] **Movement:** Standard FPS controls (WASD for movement, mouse for looking).  
  * \[ \] **Actions:** Implement jumping and sprinting mechanics.  
* **Core Combat:**  
  * \[ \] **Basic Weapon:** A single hitscan weapon (e.g., a pistol).  
    * Functionality: Fire, reload (manual), and ammo count.  
  * \[ \] **Player Health:** A simple health system. Player can take damage and be "killed" (triggering a game over state).  
* **Enemy AI:**  
  * \[ \] **Single Enemy Type:** A basic "zombie" or "grunt" archetype.  
  * \[ \] **AI Behavior:**  
    * Spawn at designated points.  
    * Pathfind towards the player (basic "move to player" is fine).  
    * Attack the player when in range (simple melee damage).  
  * \[ \] **Enemy Health:** Can take damage and be destroyed.  
* **Environment:**  
  * \[ \] **Test Arena:** A "grey box" level. Use simple geometric shapes (cubes, planes) to create a floor, walls, and a few obstacles for cover. No textures or complex models needed.

**Milestone 1 Completion Criteria:** You can move around a simple level, shoot a functional weapon, and kill a basic enemy that can also harm and kill you.

### **Milestone 2: The Game Loop (1 Week)**

**Goal:** To turn the prototype into a structured game with a clear beginning, middle, and end.

**Key Features & Tasks:**

* **Wave Management System:**  
  * \[ \] **Wave Spawner:** A script that controls the flow of the game.  
  * \[ \] **Wave Logic:** Spawns a set number of enemies for Wave 1\. Once all are defeated, a short delay occurs, and Wave 2 begins with more/stronger enemies.  
  * \[ \] **Difficulty Scaling:** For now, simply increase the number of enemies per wave.  
* **User Interface (UI):**  
  * \[ \] **Essential HUD:** Create a minimal UI to display:  
    * Player Health  
    * Current Ammo / Max Ammo  
    * Current Wave Number  
* **Game State Management:**  
  * \[ \] **Main Menu:** A simple screen with a "Start Game" button.  
  * \[ \] **Game Over Screen:** Appears on player death, showing "Game Over" and the wave number reached. Include a "Restart" button.

**Milestone 2 Completion Criteria:** You can start the game from a menu, fight through sequential waves of enemies, and reach a game over state, after which you can restart the loop.

### **Milestone 3: The "Juice" \- Polish & Game Feel (2 Weeks)**

**Goal:** To make the core actions feel satisfying and responsive. This is where your sound design skills will shine and elevate the experience from a tech demo to a *game*.

**Key Features & Tasks:**

* **Sound Design Implementation:**  
  * \[ \] **Weapon Audio:**  
    * Distinct fire sound.  
    * Mechanical reload sounds (clip out, clip in, slide).  
    * Empty clip click sound.  
  * \[ \] **Player Audio:**  
    * Footsteps (different for walking/sprinting).  
    * Take damage grunt/hit sound.  
    * Death sound.  
  * \[ \] **Enemy Audio:**  
    * Spawn sound.  
    * Idle/movement sounds (growls, mechanical whirs).  
    * Attack sound (e.g., a swipe or shot).  
    * Death sound.  
  * \[ \] **UI & Announcer Audio:**  
    * Button click sounds in the menu.  
    * A sound/voiceover for "Wave Start" and "Wave Complete".  
* **Visual Feedback:**  
  * \[ \] **Weapon Effects:** Muzzle flash, shell ejection particle effect.  
  * \[ \] **Impact Effects:** A visual effect for when your bullets hit a wall vs. when they hit an enemy (e.g., sparks vs. blood splash).  
  * \[ \] **Hit-Marker:** A small UI element that briefly appears at the crosshair when you successfully hit an enemy.  
  * \[ \] **Player Feedback:** A red flash/vignette on the screen when taking damage.

**Milestone 3 Completion Criteria:** The game feels audibly and visually responsive. Actions have weight and impact. Playing it starts to feel fun and addictive.

### **Milestone 4: Content & Environment (1-2 Weeks)**

**Goal:** To expand the core game with more variety and a more engaging playspace.

**Key Features & Tasks:**

* **Final Environment Art:**  
  * \[ \] **Arena Design:** Replace the grey box with a properly designed and modeled arena. Think about sightlines, cover, and verticality. Good themes: abandoned warehouse, sci-fi cargo bay, ancient temple chamber.  
  * \[ \] **Basic Lighting:** Implement a simple lighting setup to give the scene mood and improve visibility.  
* **Gameplay Variety:**  
  * \[ \] **New Weapon:** Add one more weapon that offers a different playstyle (e.g., a slow, powerful shotgun or a rapid-fire SMG). Implement a weapon switching system.  
  * \[ \] **New Enemy:** Add one new enemy type.  
    * *Idea:* A ranged enemy that shoots slow-moving projectiles the player must dodge.  
    * *Idea:* A fast but fragile "rusher" enemy that tries to swarm the player.  
* **Pickups:**  
  * \[ \] **Health & Ammo:** Enemies have a chance to drop health and ammo pickups upon death.

**Milestone 4 Completion Criteria:** The game is now set in a proper environment and features more than one weapon and enemy, adding a layer of tactical choice. The core loop is complete and replayable. You now have a full proof-of-concept.