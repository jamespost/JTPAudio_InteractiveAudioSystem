# Melee Combat & Hitbox System Setup Guide

This guide explains how to set up melee attacks for enemies (or players) using the **HitboxComponent** and **MeleeCombatManager**. This system replaces the old distance-based or collision-stay damage logic with precise, timing-based hitboxes.

## 1. The Core Components

To make an enemy deal melee damage, it needs three main parts:
1.  **MeleeCombatManager**: The "brain" that manages hitboxes and deals damage.
2.  **HitboxComponent**: The physical trigger (collider) that detects hits.
3.  **MeleeAttackAbility**: The GAS Ability that defines *when* and *how much* damage occurs.

---

## 2. Step-by-Step Setup

### Step 1: Prepare the Enemy Prefab
1.  Select your Enemy GameObject.
2.  Add the **`MeleeCombatManager`** component.
3.  Ensure the Enemy also has an **`AbilitySystemComponent`** and an **`AttributeSet`** (standard GAS setup).

### Step 2: Create the Hitbox
You should not put the hitbox on the main enemy object (to avoid physics issues). Use a child object.

1.  Right-click the **`Melee Combat Manager`** component header in the Inspector.
2.  Select **"Create Hitbox Child"**.
    *   This creates a child GameObject named "NewHitbox".
    *   It automatically adds a `BoxCollider` (Trigger) and `HitboxComponent`.
3.  Select the new child object.
4.  **Rename it** to something descriptive (e.g., `TeethHitbox`, `SwordHitbox`, `LeftClaw`).
5.  **Position & Scale it**:
    *   Use the Scene view tools to place the green box where the damage should happen (e.g., around the mouth or weapon).
    *   *Tip: If you can't see it, ensure Gizmos are enabled in the Scene view.*

### Step 3: Configure the Ability
You need a `GameplayAbility` asset to tell the system to use this hitbox.

1.  Create a new Ability (or use an existing one):
    *   Right-click in Project -> Create -> **GAS -> Abilities -> Melee Attack**.
2.  **Settings**:
    *   **Default Damage**: Base damage amount (e.g., 10).
    *   **Damage Effect**: Assign the standard `GE_Damage` effect.
3.  **Choose Your Mode**:

#### Option A: Animation Driven (Best for Humanoids)
Use this if your enemy has an Animator and an Attack animation clip.
1.  Check **Use Animation**.
2.  Set **Animation Trigger Name** (e.g., "Attack").
3.  Open the Animation Clip in the Animation window.
4.  **Add Events**:
    *   Start of swing: Function `ActivateHitbox`, String `YourHitboxName`.
    *   End of swing: Function `DeactivateHitbox`, String `YourHitboxName`.

#### Option B: Programmatic / Timer Driven (Best for TEEF/Simple Enemies)
Use this if your enemy moves via physics or code and doesn't have a complex attack animation.
1.  **Uncheck** `Use Animation`.
2.  **Hitbox Name**: Enter the exact name of your hitbox GameObject (e.g., `TeethHitbox`).
3.  **Hitbox Start Delay**: Time before the hitbox turns on (e.g., 0.1s).
4.  **Hitbox Active Duration**: How long it stays on (e.g., 0.5s).

### Step 4: Assign to AI
1.  Select your Enemy Prefab.
2.  Find the AI script (`EnemyAI` or `EnemyTeefAI`).
3.  Assign your new Ability asset to the **Attack Ability** slot.

---

## 3. Troubleshooting

### "I'm doing double damage!"
*   **Cause**: The hitbox is hitting multiple colliders on the player (e.g., CharacterController + CapsuleCollider).
*   **Fix**: Ensure **Smart Hit Detection** is checked on the `HitboxComponent`. This groups all colliders on the same actor into one "hit".

### "The Player takes damage even after dying (Negative Health)"
*   **Cause**: The enemy keeps attacking the corpse.
*   **Fix**: The system now automatically prevents this, but ensure your `Health` component logic properly disables the player's collider or sets a "Dead" tag if you want enemies to stop trying entirely.

### "No damage is happening"
1.  **Check the Name**: Does the `Hitbox Name` in the Ability match the GameObject name of the hitbox?
2.  **Check the Layer**: Ensure the Hitbox object is on a layer that can collide with the Player (usually "Default" or "Enemy").
3.  **Check the Console**: Look for warnings like "Could not find hitbox: [Name]".

### "The Hitbox is huge/tiny"
*   Select the Hitbox child object and adjust the **Size** on the `BoxCollider` component. Do not just scale the transform if possible, as it can skew rotation.

---

## 4. Advanced: Multiple Hitboxes
You can have multiple hitboxes on one enemy (e.g., "LeftHand", "RightHand", "Head").
*   Add multiple child Hitbox objects.
*   In your Animation, specify which one to activate by name in the Event string parameter.
*   The `MeleeCombatManager` handles the list automatically.
