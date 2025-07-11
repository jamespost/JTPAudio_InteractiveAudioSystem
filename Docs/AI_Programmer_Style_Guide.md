# AI Programmer Style Guide

This document outlines the core principles and best practices to follow when developing new features for this project. The goal is to maintain a clean, efficient, and collaborative codebase.

## Core Principles

### 1. Player Feedback is Paramount
For every action a player takes, there must be corresponding visual and audio feedback. This is crucial for player experience and game feel. Ensure that any new mechanic has clear, immediate, and satisfying feedback.

### 2. Audio Implementation
All sound effects (SFX) must be triggered via the `audiomanager.postevent` method. This ensures that our audio system can manage all sounds centrally.

### 3. Object Pooling
To minimize CPU overhead from object instantiation and destruction, all reusable objects (e.g., projectiles, enemies, particle effects) must be managed through the `ObjectPooler`. Avoid using `Instantiate()` and `Destroy()` directly for objects that are frequently created and removed.

### 4. Code Modularity and Readability
Write code that is modular and easy to understand.
-   **Single Responsibility:** Each class should have a single, well-defined purpose.
-   **Avoid Bloat:** Do not let classes become overly large or complex. If a class is doing too much, refactor it into smaller, more specialized classes.
-   **Readability:** Use clear and consistent naming conventions for variables, methods, and classes. Comment your code where necessary, but strive to make the code self-documenting.

### 5. Designer-Friendly Features
All systems and features must be accessible and configurable by designers.
-   Expose parameters in the Unity Inspector using `[SerializeField]`.
-   Use tooltips (`[Tooltip("...")]`) to explain what each parameter does.
-   Provide custom editors or inspectors where it can simplify a designer's workflow.

### 6. Event-Driven Architecture
We use a global `EventManager` to handle in-game events.
-   **Announce Events:** When a significant event occurs (e.g., player takes damage, enemy is defeated, objective completed), announce it through the `EventManager`.
-   **Listen for Events:** Other systems can then subscribe to these events and react accordingly. This promotes loose coupling between different parts of the game and makes the codebase more flexible and easier to maintain.

## Additional Best Practices

### 7. Performance Considerations
Avoid using the `Update()` method for frequent or expensive calculations. Instead:
- Use `InvokeRepeating()` or coroutines for periodic tasks.
- Leverage Unity's event-driven systems where possible.
- Minimize allocations in `Update()` to reduce garbage collection overhead.
- For time-dependent logic (e.g., damage over time), always use `Time.deltaTime` to ensure framerate independence.

### 8. Scene Transitions and Event Management
This game involves frequent scene loading and reloading. Ensure that:
- All events are properly subscribed to when a scene is loaded.
- Events are unsubscribed from when a scene is unloaded or an object is destroyed.
- Use Unity's `OnEnable()` and `OnDisable()` methods to manage event subscriptions dynamically.
- Test features thoroughly to ensure they work seamlessly across multiple scene transitions.
