# Feature Ideas

This document serves as a catch-all for new features and ideas that arise during development. Features listed here can be reviewed, prioritized, and implemented as needed.

## Damage Numbers
- **Description:** Display damage numbers that pop off of entities when they take damage.
- **Implementation:**
  - Use a similar setup to the health display system, utilizing a `World Space Canvas`.
  - Numbers should fade out and move upward over time.
  - Allow designers to tweak the font, size, and animation speed via `EntityData`.
  - Should probably integrate with the objectpooler somehow for efficiency.
- **Rationale:** Provides visual feedback to players, enhancing the gameplay experience and making damage more satisfying.

## Enemy Taunts
- **Description:** Enemies occasionally taunt the player during combat.
- **Implementation:**
  - Use audio clips triggered by specific events (e.g., player low health, enemy near death).
  - Allow designers to assign taunt audio clips via `EnemyData`.
  - Should also include updates to state machine for enemies and handling of taunt animations
- **Rationale:** Adds personality to enemies and makes combat more engaging.


## Screenshake System

- **Description:** Implement an event-based, data-driven screenshake system that can be triggered by various gameplay events such as firing weapons, explosions, taking damage, or landing from high falls.
- **Implementation:**
  - Define a `ScreenshakeEvent` data structure that specifies shake intensity, duration, and curve.
  - Expose screenshake parameters in relevant data assets (e.g., `WeaponData`, `EnemyData`, `EnvironmentData`).
  - Utilize the `EventManager` to listen for gameplay events and trigger screenshake with the appropriate parameters.
  - Allow designers to configure which events cause screenshake and customize their effects without writing custom code for each case.
  - Allow for experimentation with audio events being able to define the 'impact' of a sound to also drive screenshake.
- **Rationale:** Provides consistent and easily tunable feedback for impactful events, improving immersion and reducing code duplication.

## Shader Ideas
- **Description:** New shaders to be developed for various visual effects.
- **Implementation:**
  - Simulated Subsurface Scattering for Deep-Sea Creature Skin: A shader designed for Unity's Built-in Render Pipeline, focusing on translucent, bioluminescent skin with high performance. See the [Shader Brief](./SimulatedSubsurfaceScatteringShader.md) for more details.
- **Rationale:** To create more visually stunning and immersive environments and characters.

## Sense of Speed

- **Description:** Enhance the player's sense of speed during gameplay actions such as sprinting, jumping, falling, or other high-velocity movements.
- **Implementation:**
  - Dynamically increase the camera's Field of View (FOV) during high-speed actions to create a sense of acceleration.
  - Add motion blur effects proportional to the player's velocity.
  - Implement "speedy lines" or streaks on the screen to simulate wind or motion.
  - Use subtle camera shake or vibration to emphasize rapid movement.
  - Integrate audio cues like wind rushing sounds to complement the visual effects.
  - Allow designers to tweak parameters such as FOV range, motion blur intensity, and visual effect styles via a dedicated `SpeedEffectData` asset.
  - Ensure compatibility with existing systems like the `EventManager` to trigger effects based on gameplay events.
- **Rationale:** Improves gamefeel by making high-speed actions more immersive and exhilarating, enhancing player engagement and satisfaction.

## Data-Driven Enemy Movement System

- **Description:** Transform enemy movement systems (e.g., move toward player, hide, run away, group up) into a data-driven architecture, enabling designers to easily assign and configure movement abilities for new enemy types.
- **Implementation:**
  - Define a `MovementBehavior` data structure that encapsulates movement logic, parameters, and triggers.
  - Create a library of reusable movement behaviors (e.g., chase, flee, patrol, group up) that can be assigned to enemies via `EnemyData` assets.
  - Integrate with the existing state machine to allow seamless transitions between movement behaviors.
  - Provide a visual editor for designers to configure movement behaviors and set priorities or conditions for switching between them.
  - Ensure compatibility with the `EventManager` to trigger movement behaviors based on gameplay events (e.g., player proximity, health thresholds).
  - Allow for custom scripting hooks to extend movement logic if needed.
- **Rationale:** Simplifies the process of creating and iterating on enemy movement patterns, empowering designers to experiment and innovate without requiring extensive programming support.

## Point of Interest System

- **Description:** Implement a "Point of Interest" (POI) system for NPCs and enemies to guide their behavior, such as where to look or where to move. This system should support both designer-placed POIs and dynamically generated ones.
- **Implementation:**
  - Define a `PointOfInterest` data structure that includes attributes like position, type (e.g., visual, movement), priority, and conditions for activation.
  - Allow designers to place POIs in scenes using a visual editor, with options to configure their attributes.
  - Develop a dynamic POI system that generates POIs based on gameplay events (e.g., player actions, environmental changes).
  - Integrate with NPC and enemy AI to enable behaviors like looking at a POI attached to the player's camera or moving to a POI behind cover.
  - Ensure compatibility with the `EventManager` to trigger POI updates or activations based on events.
  - Provide scripting hooks for custom POI logic if needed.
- **Rationale:** Enhances the realism and responsiveness of NPC and enemy behavior, allowing for more dynamic and engaging interactions. Empowers designers to create complex scenarios without requiring extensive programming.

## Enemy Thought Display System

- **Description:** Create a UI system to display information above enemies, showing what they are "thinking" or doing. This system will aid in designing and debugging AI systems.
- **Implementation:**
  - Use a `World Space Canvas` to display text or icons above enemies.
  - Integrate with the AI system to show current states, goals, or actions (e.g., "Chasing Player," "Hiding," "Looking for Cover").
  - Allow designers to toggle the display on or off for debugging purposes.
  - Provide customization options for the UI, such as font size, color, and iconography.
  - Ensure the system is lightweight and does not impact performance when disabled.
  - Optionally, include a log or history of recent actions for deeper debugging insights.
- **Rationale:** Helps designers and developers understand and refine AI behavior, making it easier to identify issues and improve gameplay.