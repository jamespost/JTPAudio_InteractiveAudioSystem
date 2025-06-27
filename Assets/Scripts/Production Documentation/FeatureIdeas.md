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
