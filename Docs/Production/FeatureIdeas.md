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