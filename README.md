# JTP Audio - Interactive Audio System

This document provides a comprehensive guide to the JTP Interactive Audio System for Unity. This system is designed to be a flexible, powerful, and designer-friendly solution for managing all aspects of your game's audio.

## Table of Contents
1.  [Introduction](#introduction)
2.  [Core Concepts](#core-concepts)
3.  [Getting Started](#getting-started)
4.  [How to Use (Examples)](#how-to-use-examples)
    *   [Playing a Simple Sound](#playing-a-simple-sound)
    *   [Creating Variations with RandomContainer](#creating-variations-with-randomcontainer)
    *   [Implementing Dynamic Footsteps](#implementing-dynamic-footsteps)
    *   [Real-Time Parameter Control (RTPC)](#real-time-parameter-control-rtpc)
    *   [Managing the Mix with Audio States](#managing-the-mix-with-audio-states)
    *   [Dynamic Sound Priority with IAudioThreat](#dynamic-sound-priority-with-iaudiothreat)
5.  [AudioManager API Reference](#audiomanager-api-reference)
6.  [Best Practices](#best-practices)

---

## Introduction

The JTP Interactive Audio System is a ScriptableObject-based audio engine built for Unity. It moves beyond simple "fire-and-forget" sound effects, providing a structured framework for creating dynamic, responsive, and immersive soundscapes. The system is inspired by features found in professional audio middleware like FMOD and Wwise, but with the simplicity and integration of native Unity components.

**Key Features:**

*   **Event-Driven:** Trigger complex sound behaviors with a single line of code.
*   **Voice Management:** An automatic pooling and priority system manages resource limits, preventing performance issues and ensuring critical sounds are always heard.
*   **Dynamic Audio:** Use `GameSwitches`, `GameParameters`, and `AudioStates` to make your soundscape react to the game in real-time.
*   **Designer-Friendly:** Most of the system is configured through ScriptableObject assets in the Unity Editor, minimizing the need for constant code changes.
*   **Advanced Sound Behaviors:** Easily create variations, sequences, and state-based sound logic using different `Container` types.
*   **Dynamic Priority:** A unique `IAudioThreat` interface allows sounds to dynamically increase their importance based on gameplay context.

---

## Core Concepts

The system is built around a few key ScriptableObject assets. Understanding how they work together is key to using the system effectively.

*   **`AudioManager.cs`**: The heart of the system. This singleton MonoBehaviour manages all audio playback, resource pooling, and state tracking. You will have one `AudioManager` in your scene.

*   **`AudioEvent.cs`**: The primary way to trigger a sound. An `AudioEvent` is a ScriptableObject that links a unique string ID (e.g., "Player_Footstep") to a sound behavior. It defines the sound's priority, which mixer group it uses, and what sound to play via a `BaseContainer`.

*   **`BaseContainer.cs`**: An abstract class for playback logic. You will use one of its child classes:
    *   **`SimpleContainer`**: Plays a single `AudioClip`.
    *   **`RandomContainer`**: Plays a random `AudioClip` from a list, with options for randomizing pitch and volume.
    *   **`SequenceContainer`**: Plays `AudioClip`s in a defined order.
    *   **`SwitchContainer`**: Plays a different `Container` based on the current value of a `GameSwitch`.

*   **`GameSwitch.cs`**: Represents a collection of states, like surface types ("Grass", "Wood", "Metal"). You change the switch's value from code, and `SwitchContainers` react to it.

*   **`GameParameter.cs`**: A float value that can be changed in real-time (e.g., "PlayerHealth", "EngineRPM"). These can be used to modulate the volume and pitch of individual `AudioEvents`.

*   **`AudioState.cs`**: Defines a global state for the game's mix (e.g., "MainMenu", "Combat", "Paused"). Setting a state transitions to a specific `AudioMixerSnapshot`, allowing for broad changes to the overall soundscape.

*   **`IAudioThreat`**: An interface you can add to your own game components (e.g., on an enemy script). It provides a "threat level" from 0 to 1, which the `AudioManager` uses to dynamically boost a sound's priority. This is perfect for ensuring that the sounds from the closest or most dangerous enemies are prioritized in a chaotic scene.

---

## Getting Started

1.  **Create the AudioManager:**
    *   Create an empty GameObject in your main scene and name it "AudioManager".
    *   Attach the `AudioManager.cs` script to it.
    *   Drag this "AudioManager" GameObject into your project assets to create a prefab.

2.  **Configure the AudioManager:**
    *   Select the `AudioManager` prefab or the instance in your scene.
    *   **Main Mixer:** Create a Unity `AudioMixer` (e.g., "MainMixer") and assign it to the `Main Mixer` field. This is required for `AudioStates` and `GameParameters` to control mixer effects.
    *   **Data Assets:** In the `AudioManager` inspector, you need to register your `GameSwitches`, `GameParameters`, and `AudioStates`. Drag and drop the ScriptableObject assets you create into the corresponding lists. This allows the `AudioManager` to know they exist at startup.
    *   **Pooling:** Adjust the `Initial Pool Size` and `Can Pool Grow` settings based on your project's needs. A pool of 16-32 sources is a good starting point.

3.  **Create Audio Assets:**
    *   Create folders in your project to organize your audio assets, for example: `Audio/Events`, `Audio/Containers`, `Audio/Switches`.
    *   Use the `Create > Audio` menu to create your `AudioEvent`, `Container`, `GameSwitch`, `GameParameter`, and `AudioState` assets.

---

## How to Use (Examples)

### Playing a Simple Sound

1.  **Create a Container:** Right-click in your project > `Create > Audio > Containers > Simple Container`. Name it `SC_PlayerJump`. Assign your jump `AudioClip` to its `Clip` field.
2.  **Create an Event:** Right-click > `Create > Audio > Audio Event`. Name it `AE_PlayerJump`.
    *   Set the `Event ID` to "Player_Jump".
    *   Drag your `SC_PlayerJump` container into the `Container` field.
    *   Assign an `AudioMixerGroup` if you have one (e.g., "SFX").
3.  **Trigger from Code:** In your player movement script, call `PostEvent`:

    ```csharp
    if (Input.GetButtonDown("Jump"))
    {
        // ... your jump logic ...
        AudioManager.Instance.PostEvent("Player_Jump", this.gameObject);
    }
    ```

### Creating Variations with RandomContainer

To make sounds less repetitive, use a `RandomContainer`.

1.  **Create a Container:** Right-click > `Create > Audio > Containers > Random Container`. Name it `RC_ImpactFlesh`.
2.  **Configure:**
    *   Add multiple flesh impact `AudioClip`s to the `Clips` list.
    *   Set `Avoid Repeat` to `true`.
    *   Adjust the `min/max Volume DB` and `min/max Pitch Semitones` to add subtle, natural-sounding variations to each impact.
3.  **Create and Post Event:** Create an `AudioEvent` named `AE_ImpactFlesh` with the ID "Impact_Flesh" and assign your `RC_ImpactFlesh` container to it. Post the event from your combat script.

### Implementing Dynamic Footsteps

This is a classic use case for a `SwitchContainer`.

1.  **Create the Switch:** Right-click > `Create > Audio > Game Switch`. Name it `GS_SurfaceType`.
    *   Set the `Switch ID` to "SurfaceType".
    *   Set the `Default Value` to "Dirt".
2.  **Create Random Containers:** Create a `RandomContainer` for each surface type (e.g., `RC_Footsteps_Grass`, `RC_Footsteps_Wood`). Populate them with the corresponding footstep sounds.
3.  **Create the Switch Container:** Right-click > `Create > Audio > Containers > Switch Container`. Name it `SWC_Footsteps`.
    *   Drag your `GS_SurfaceType` asset into the `Game Switch` field.
    *   In the `Mappings` list, add entries for each surface.
        *   `Switch Value`: "Grass", `Container`: `RC_Footsteps_Grass`
        *   `Switch Value`: "Wood", `Container`: `RC_Footsteps_Wood`
    *   Assign a default container if desired.
4.  **Create the Event:** Create an `AudioEvent` `AE_Footsteps` with the ID "Player_Footstep" and assign your `SWC_Footsteps` container.
5.  **Update the Switch from Code:** In your player controller, use a raycast to detect the ground surface and update the switch.

    ```csharp
    void CheckSurface()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 3f))
        {
            // Assuming your ground objects have tags like "Grass", "Wood", etc.
            string surfaceTag = hit.collider.tag;
            AudioManager.Instance.SetSwitch("SurfaceType", surfaceTag);
        }
    }

    // Called from an animation event on the footstep animation
    void PlayFootstepSound()
    {
        AudioManager.Instance.PostEvent("Player_Footstep", this.gameObject);
    }
    ```

### Real-Time Parameter Control (RTPC)

Control sound properties dynamically with a `GameParameter`.

1.  **Create the Parameter:** Right-click > `Create > Audio > Game Parameter`. Name it `GP_VehicleSpeed`.
    *   Set `Parameter ID` to "VehicleSpeed".
    *   Set `Min Value` to 0 and `Max Value` to 150 (or your car's max speed).
2.  **Configure the Event:** Open your engine loop `AudioEvent`.
    *   In the `Parameter Modulation` section, add a new entry.
    *   `Parameter`: Drag in your `GP_VehicleSpeed` asset.
    *   `Target Property`: `Pitch`.
    *   `Mapping Curve`: Adjust the curve so that the Y-axis (pitch multiplier) increases as the X-axis (normalized speed) goes from 0 to 1.
3.  **Update the Parameter from Code:** In your vehicle script's `Update` method:

    ```csharp
    void Update()
    {
        float currentSpeed = myRigidbody.velocity.magnitude;
        AudioManager.Instance.SetParameter("VehicleSpeed", currentSpeed);
    }
    ```

### Managing the Mix with Audio States

1.  **Create Mixer Snapshots:** In your `AudioMixer`, create snapshots for different scenarios. For example:
    *   `Gameplay`: Normal mix.
    *   `Paused`: Duck the volume of the "SFX" and "Music" groups.
    *   `Combat`: Slightly increase the "SFX" group volume and apply a low-pass filter to the "Music" group.
2.  **Create Audio States:** For each snapshot, create a corresponding `AudioState` asset (`Create > Audio > Audio State`).
    *   Name them `AS_Gameplay`, `AS_Paused`, `AS_Combat`.
    *   Set the `State ID` (e.g., "Gameplay", "Paused", "Combat").
    *   Assign the corresponding `AudioMixerSnapshot` to each one.
    *   Set a `Transition Time`.
3.  **Set the State from Code:**

    ```csharp
    public void PauseGame()
    {
        Time.timeScale = 0;
        AudioManager.Instance.SetState("Paused");
    }

    public void ResumeGame()
    {
        Time.timeScale = 1;
        AudioManager.Instance.SetState("Gameplay");
    }
    ```

### Dynamic Sound Priority with IAudioThreat

Ensure important sounds are heard in a busy scene.

1.  **Implement the Interface:** On a script for an object that can pose a threat (e.g., `EnemyAI.cs`), implement the `IAudioThreat` interface.

    ```csharp
    public class EnemyAI : MonoBehaviour, IAudioThreat
    {
        private Transform player;

        void Start()
        {
            player = GameObject.FindGameObjectWithTag("Player").transform;
        }

        public float GetCurrentThreat()
        {
            // Example: Threat is based on distance to the player.
            // 1.0 = very close (max threat), 0.0 = far away (no threat).
            float distance = Vector3.Distance(transform.position, player.position);
            float maxThreatDistance = 10f; // Closer than 10m is max threat
            float threat = Mathf.Clamp01(1 - (distance / maxThreatDistance));
            return threat;
        }

        void Attack()
        {
            // When the enemy attacks, post an event. The AudioManager will automatically
            // check its IAudioThreat component and adjust the sound's priority.
            AudioManager.Instance.PostEvent("Enemy_Attack", this.gameObject);
        }
    }
    ```
2.  **How it Works:** When you call `PostEvent("Enemy_Attack", ...)`, the `AudioManager` performs `GetComponent<IAudioThreat>()` on the `EnemyAI` GameObject. It calls `GetCurrentThreat()`, gets the 0-1 value, and adds it to the `AudioEvent`'s base priority. A sound from an enemy right next to the player will now have a much higher priority than the same sound from an enemy far away, making it far less likely to be culled by the voice management system.

---

## AudioManager API Reference

These are the primary public methods for interacting with the audio system.

*   `void PostEvent(string eventName, GameObject sourceObject)`
    *   The main function for playing a sound.
    *   `eventName`: The `eventID` of the `AudioEvent` you want to play.
    *   `sourceObject`: The GameObject that is emitting the sound. Used for 3D positioning and for the `IAudioThreat` check.

*   `void SetState(string stateId)`
    *   Transitions the `AudioMixer` to a new snapshot.
    *   `stateId`: The `stateID` of the `AudioState` to activate.

*   `void SetParameter(string parameterId, float value)`
    *   Updates the value of a `GameParameter`.
    *   `parameterId`: The `parameterID` of the `GameParameter` to change.
    *   `value`: The new value.

*   `void SetSwitch(string switchId, string value)`
    *   Updates the value of a `GameSwitch`.
    *   `switchId`: The `switchID` of the `GameSwitch` to change.
    *   `value`: The new string value (e.g., "Grass", "Wood").

---

## Best Practices

*   **Organization:** Keep your audio ScriptableObjects organized in folders that mirror their type (Events, Containers, Switches, etc.). This makes them much easier to find and manage.
*   **Naming Conventions:** Use consistent prefixes for your assets (e.g., `AE_` for AudioEvents, `RC_` for RandomContainers, `GS_` for GameSwitches). This makes them instantly identifiable in inspector fields.
*   **Register Your Assets:** Remember to drag all `GameSwitch`, `GameParameter`, and `AudioState` assets into their respective lists on the `AudioManager` component. If you don't, the system won't know they exist.
*   **Use the `IAudioThreat` Sparingly:** The dynamic threat calculation is powerful but has a small performance cost (`GetComponent`). Use it for sounds that truly need dynamic prioritization, like enemy vocalizations, weapons, or impacts in a combat scenario. For ambient or UI sounds, a fixed priority is usually sufficient.
*   **Leverage Mixer Groups:** Route your `AudioEvents` to different `AudioMixerGroup`s (SFX, Music, UI, Voice). This gives you broad volume control from the `AudioMixer` window and is essential for `AudioStates` to function correctly.
