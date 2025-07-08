# Dynamic UI System: Technical Design Document

## 1. Overview

This document outlines the architecture for a dynamic, event-driven UI system for Unity. The system is designed to be decoupled, designer-friendly, and performant, allowing any game system to request UI elements without holding a direct reference to the UI.

### Core Design Principles:

* **Decoupled:** Game logic communicates with the UI system exclusively through a global `EventManager`. It never references UI components directly.
* **Designer-Friendly:** UI elements and their behaviors (priority, target location, prefab) are defined using `ScriptableObject` assets (`UIRequestData`), allowing for configuration without code changes.
* **Organized:** The screen is divided into logical `UIChannels` (e.g., `CenterPrompts`, `PlayerHUD`), each managing its own layout and request queue. This prevents UI elements from overlapping chaotically.
* **Performant:** All UI elements are instantiated via an `ObjectPooler` to avoid garbage collection spikes from `Instantiate`/`Destroy` calls during runtime.

---

## 2. System Architecture

The system is composed of several key components that work together to manage the lifecycle of a UI request.

```mermaid
graph TD
    subgraph Game Logic
        A[PlayerInteraction Script] -- 1. Creates content --> B;
        B(UIRequestPayload) -- 2. Fires Event --> C{EventManager};
    end

    subgraph UI System
        C -- 3. Notifies Subscribers --> D(UIManager);
        D -- 4. Routes request based on ChannelID --> E[UIChannel];
        E -- 5. Requests Prefab --> F(ObjectPooler);
        F -- 6. Provides GameObject --> E;
        E -- 7. Gets Data from Payload --> G[UI Prefab Instance];
        G -- 8. Populates Itself --> G;
    end

    subgraph Configuration
        H(UIRequestData.asset) -- Defines prefab, priority, channel --> B;
    end

    A -.-> H;
