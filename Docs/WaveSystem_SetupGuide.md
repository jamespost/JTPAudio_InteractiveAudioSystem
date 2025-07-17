# Wave System Setup Guide

## Overview

The Wave System in this project consists of a hierarchical structure:
- **LevelData** (ScriptableObject) - Contains multiple waves for a complete level
- **WaveData** (ScriptableObject) - Defines a single wave with multiple spawn groups
- **SpawnGroup** (Serializable class) - Defines what enemies to spawn, how many, where, and when

## Understanding the Hierarchy

```
LevelData (e.g., "Level 1")
├── WaveData (Wave 1)
│   ├── SpawnGroup 1 (3 BasicEnemies, spawn immediately)
│   ├── SpawnGroup 2 (2 FastEnemies, spawn after 5 seconds)
│   └── SpawnGroup 3 (1 BossEnemy, spawn after 10 seconds)
├── WaveData (Wave 2)
│   ├── SpawnGroup 1 (5 BasicEnemies, spawn immediately)
│   └── SpawnGroup 2 (3 ArmoredEnemies, spawn after 8 seconds)
└── WaveData (Wave 3)
    └── SpawnGroup 1 (1 FinalBoss, spawn immediately)
```

## Step-by-Step Setup Guide

### Step 1: Create Enemy Prefabs and Set Up Object Pooler

1. **Create enemy prefabs** in `Assets/Prefabs/`
2. **Ensure each enemy has**:
   - A `Health` component
   - A collider
   - Any AI/movement scripts
3. **Set up Object Pooler**:
   - Add enemy prefabs to the ObjectPooler's pool
   - Give each enemy type a unique **Pool Tag** (e.g., "BasicEnemy", "FastEnemy", "BossEnemy")
   - Set appropriate pool sizes (e.g., 10 for basic enemies, 3 for bosses *remember this will be a hard cap on how many concurrent enemies of a specific type can "exist" at the same time*)

### Step 2: Create Spawn Groups (Individual Enemy Spawns)

A **Spawn Group** defines a single type of enemy spawn within a wave.

**What each Spawn Group contains:**
- `whatToSpawn` - The enemy prefab to spawn
- `count` - How many of this enemy type to spawn
- `spawnPoint` - Where to spawn them (can be null for NavMesh spawning)
- `delay` - How long to wait before spawning this group
- `poolTag` - The ObjectPooler tag for this enemy type

**Example Spawn Groups:**
```
Spawn Group 1:
- whatToSpawn: BasicEnemy_Prefab
- count: 3
- spawnPoint: null (uses NavMesh spawning)
- delay: 0 seconds
- poolTag: "BasicEnemy"

Spawn Group 2:
- whatToSpawn: FastEnemy_Prefab
- count: 2
- spawnPoint: null
- delay: 5 seconds
- poolTag: "FastEnemy"
```

### Step 3: Create WaveData (Individual Waves)

A **Wave** is a collection of spawn groups that must all be defeated before the next wave begins.

1. **Right-click in Project window** → Create → Wave System → Wave Data
2. **Name it descriptively** (e.g., "Level1_Wave1", "Level1_Wave2")
3. **Add Spawn Groups**:
   - Click the "+" button to add spawn groups
   - Configure each spawn group as described above
4. **Set wave properties**:
   - `waveName` - Descriptive name for designers
   - `spawnGroups` - Array of all spawn groups in this wave

**Wave Design Tips:**
- **Start simple**: Begin with 1-2 spawn groups per wave
- **Stagger spawns**: Use different delay times to create interesting pacing
- **Mix enemy types**: Combine different enemies for variety
- **Escalate difficulty**: Later waves should have more enemies or tougher types

### Step 4: Create LevelData (Complete Levels)

A **Level** contains all the waves for a complete gameplay experience.

1. **Right-click in Project window** → Create → Wave System → Level Data
2. **Name it descriptively** (e.g., "Level1_Complete", "Tutorial_Level")
3. **Add Wave Data assets**:
   - Drag your created WaveData assets into the `waves` array
   - Order them from first wave to last wave
4. **Set level properties**:
   - `levelName` - Display name for the level
   - `waves` - Array of WaveData assets in order

### Step 5: Assign to WaveManager

1. **Find the WaveManager** in your Game scene (usually on a GameManager GameObject)
2. **Assign your LevelData** to the `currentLevelData` field
3. **Test the setup** by playing the game

## Common Mistakes to Avoid

### Mistake 1: Confusing Spawn Groups with Waves
- **Wrong**: Creating one wave with many spawn groups thinking each group is a separate wave
- **Right**: Each wave should contain related spawn groups that form a cohesive challenge

### Mistake 2: Forgetting Pool Tags
- **Problem**: Setting up spawn groups but forgetting to match the `poolTag` with ObjectPooler tags
- **Solution**: Always verify pool tags match between WaveData and ObjectPooler setup

### Mistake 3: Not Testing Enemy Counts
- **Problem**: Creating waves with too many enemies that overwhelm the player
- **Solution**: Start with small numbers and playtest, then gradually increase

### Mistake 4: Ignoring Spawn Delays
- **Problem**: Setting all spawn group delays to 0, causing all enemies to spawn at once
- **Solution**: Stagger spawns with 2-5 second delays for better pacing

## Example: Creating a Simple 3-Wave Level

### Wave 1 - Tutorial Wave
```
SpawnGroup 1: 2 BasicEnemies, delay 0s
SpawnGroup 2: 1 BasicEnemy, delay 10s
```

### Wave 2 - Mixed Challenge
```
SpawnGroup 1: 3 BasicEnemies, delay 0s
SpawnGroup 2: 2 FastEnemies, delay 5s
SpawnGroup 3: 1 BasicEnemy, delay 15s
```

### Wave 3 - Boss Wave
```
SpawnGroup 1: 1 BossEnemy, delay 0s
SpawnGroup 2: 2 BasicEnemies, delay 20s (reinforcements)
```

## Debugging Wave Issues

### Check Debug Console
The WaveManager provides detailed debug output:
- "Starting wave X, invoking OnWaveChanged event"
- "Wave will spawn X total enemies"
- "Enemy died. Enemies remaining in wave: X"
- "All enemies defeated! Wave complete."

### Common Debug Messages
- **"Failed to spawn object from pool"** → Check pool tags match
- **"Could not find valid NavMesh spawn position"** → Ensure NavMesh is baked
- **"No LevelData assigned"** → Assign LevelData to WaveManager
- **Waves not advancing** → Check if enemies have Health component with OnDied event

### Verification Checklist
- [ ] Enemy prefabs have Health components
- [ ] Pool tags match between WaveData and ObjectPooler
- [ ] WaveData assets are assigned to LevelData
- [ ] LevelData is assigned to WaveManager
- [ ] NavMesh is baked in the scene (for automatic spawning)
- [ ] Enemy counts are reasonable for playtesting

## Advanced Tips

### Spawn Point Strategy
- **Leave spawnPoint null** to use automatic NavMesh-based spawning
- **Assign specific Transform** for precise spawn locations
- **Mix both approaches** within the same wave for variety

### Balancing Difficulty
- **Enemy count progression**: 2-3 → 4-6 → 7-10 enemies per wave
- **Type mixing**: Start with basic enemies, introduce special types gradually
- **Timing variation**: Vary delay patterns to keep players engaged

### Performance Considerations
- **Object Pooler sizing**: Set pool sizes to accommodate your largest wave + some buffer
- **Spawn delays**: Don't spawn all enemies simultaneously to reduce frame drops
- **Enemy cleanup**: Ensure enemies are properly returned to pool when killed

## File Organization

Recommended folder structure:
```
Assets/
├── ScriptableObjects/
│   ├── LevelData/
│   │   ├── Tutorial_Level.asset
│   │   ├── Level1_Complete.asset
│   │   └── Level2_Complete.asset
│   └── WaveData/
│       ├── Tutorial_Wave1.asset
│       ├── Level1_Wave1.asset
│       ├── Level1_Wave2.asset
│       └── Level1_Wave3.asset
└── Prefabs/
    └── Enemies/
        ├── BasicEnemy.prefab
        ├── FastEnemy.prefab
        └── BossEnemy.prefab
```

This organization makes it easy to find and manage your wave configurations as your project grows.
