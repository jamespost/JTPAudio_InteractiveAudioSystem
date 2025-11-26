# Screen Shake System

This system provides an extensible and scalable way to handle screen shake events in the game.

## Components

1.  **ScreenShakeManager**: The core singleton that manages active shakes.
2.  **ScreenShakeSettings**: A ScriptableObject that defines the properties of a shake (duration, strength, frequency, etc.).
3.  **PlayerDamageShake**: A component that listens for player damage and triggers a shake.

## Setup

1.  **Add the Manager**:
    *   Create an empty GameObject in your scene (or use an existing manager object).
    *   Add the `ScreenShakeManager` component to it.
    *   Assign the Main Camera to the `Camera Transform` field (optional, it will find `Camera.main` automatically).

2.  **Create Shake Profiles**:
    *   Right-click in the Project window -> `Create` -> `Camera` -> `Screen Shake Settings`.
    *   Name it (e.g., `DamageShake`, `ExplosionShake`).
    *   Adjust the settings:
        *   **Duration**: How long the shake lasts.
        *   **Position/Rotation Strength**: How much it moves/rotates.
        *   **Frequency**: How fast it shakes.
        *   **Falloff**: Curve for fade-out.
        *   **Noise**: Use Perlin noise for smoother, more natural shakes.

3.  **Setup Player Damage Shake**:
    *   Select your Player object (the one with the `Health` component).
    *   Add the `PlayerDamageShake` component.
    *   Assign your `DamageShake` profile to the `Damage Shake Settings` field.
    *   Adjust `Damage To Shake Multiplier` to control how much damage affects the shake intensity.

## Usage for Designers

*   **Scripted Events**: You can trigger shakes from any script by calling:
    ```csharp
    ScreenShakeManager.Instance.Shake(myShakeSettings);
    ```
*   **Explosions/Impacts**: To trigger a shake that falls off with distance:
    ```csharp
    ScreenShakeManager.Instance.ShakeFromPoint(explosionPosition, radius, myShakeSettings);
    ```

## Extensibility

*   To add new types of triggers (e.g., recoil, heavy landing), create a new script that references a `ScreenShakeSettings` asset and calls `ScreenShakeManager.Instance.Shake()`.
