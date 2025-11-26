# Gameplay Ability System (GAS) Implementation Plan

## Overview
This document outlines a phased approach to implementing an Unreal Engine-style Gameplay Ability System (GAS) in Unity for the `JTPAudio_InteractiveAudioSystem` project. The goal is to decouple game logic (Abilities) from data (Attributes) and presentation (Cues), making the codebase more modular and easier to extend.

## Phase 1: The Foundation (Tags & Attributes)
**Goal:** Establish the vocabulary and data structure for the system.

### 1.1 Gameplay Tags
*   **Concept:** Hierarchical labels (e.g., `State.Stunned`, `Element.Fire`) used to query state and control logic flow.
*   **Implementation:**
    *   Create `GameplayTag` (ScriptableObject).
    *   Create `GameplayTagContainer` (Struct/Class) to hold a set of tags.
    *   Implement helper methods: `HasTag()`, `HasAny()`, `HasAll()`.

### 1.2 Attributes & Attribute Sets
*   **Concept:** Floating point values representing actor stats (Health, Mana, Speed).
*   **Implementation:**
    *   Create `Attribute` class (BaseValue, CurrentValue).
    *   Create `AttributeSet` (ScriptableObject or Monobehaviour) to define collections of attributes for different entity types (e.g., `PlayerAttributeSet`, `EnemyAttributeSet`).
    *   **Migration:** Plan to replace `Health.cs` logic with a `Health` Attribute.

## Phase 2: The Engine (Ability System Component & Effects)
**Goal:** Create the central processor that manages state and applies changes.

### 2.1 Ability System Component (ASC)
*   **Concept:** The "brain" attached to any actor (Player, Enemy) that wants to use abilities.
*   **Implementation:**
    *   Create `AbilitySystemComponent` : `MonoBehaviour`.
    *   It should hold: `AttributeSet`, `GameplayTagContainer`, and a list of `ActiveAbilities`.

### 2.2 Gameplay Effects (GE)
*   **Concept:** The *only* way to change an Attribute or add a Tag.
*   **Implementation:**
    *   Create `GameplayEffect` (ScriptableObject).
    *   **Properties:**
        *   **Duration Policy:** Instant (Damage), Infinite (Equipment stats), Duration (Buff/Debuff).
        *   **Modifiers:** Add, Multiply, Override operations on Attributes.
        *   **Granted Tags:** Tags applied while the effect is active.
    *   Implement `ApplyGameplayEffectToSelf` and `ApplyGameplayEffectToTarget` on the ASC.

## Phase 3: The Actions (Gameplay Abilities)
**Goal:** Define what actors can actually *do*.

### 3.1 Gameplay Ability Base
*   **Concept:** A modular piece of logic (Cast Spell, Swing Sword, Dash).
*   **Implementation:**
    *   Create `GameplayAbility` (ScriptableObject).
    *   **Key Methods:** `CanActivate()`, `Activate()`, `EndAbility()`, `CommitAbility()`.
    *   **Properties:**
        *   `Cost` (GameplayEffect).
        *   `Cooldown` (GameplayEffect).
        *   `ActivationTags` (Tags required/blocked).

### 3.2 Ability Lifecycle
*   **Implementation:**
    *   Implement the flow: Check Costs -> Check Cooldowns -> Apply Cooldowns/Costs -> Execute Logic -> End.
    *   Integrate with `PlayerController` input to trigger abilities on the ASC.

## Phase 4: Feedback & Integration (Cues & Audio)
**Goal:** Connect the logic to the visual and audio systems.

### 4.1 Gameplay Cues
*   **Concept:** Visual and Audio feedback decoupled from logic.
*   **Implementation:**
    *   Create `GameplayCue` (MonoBehaviour) that listens for Tag additions/removals or specific Events.
    *   **Integration:** Hook into `JTPAudio` system. For example, a `State.LowHealth` tag could trigger a specific audio loop or filter.

### 4.2 Migration & Refactor
*   **Tasks:**
    *   Refactor `WeaponController` to trigger `FireAbility` instead of direct logic.
    *   Refactor `EnemyAI` to use Abilities for attacks.
    *   Replace `Health.cs` usage with `ASC.GetAttributeValue("Health")`.

## Phase 5: Advanced Features (Optional/Later)
*   **Targeting System:** Reusable targeting logic (AOE, Raycast).
*   **Effect Execution Calculations:** Complex damage formulas.
*   **UI Integration:** Bind UI bars directly to Attribute changes via events.
