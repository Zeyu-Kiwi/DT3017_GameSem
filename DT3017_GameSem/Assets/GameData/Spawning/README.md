# Daily item spawning

## Scene setup

`Level` contains **Daily Item Spawns**, with one `DailyItemSpawner` and three editable child `ItemSpawnZone` objects. Select a zone and enable Gizmos to see its green box. Move/resize that box to cover the desired surfaces. Keep zones upright and give duplicated zones different Zone IDs.

Level currently has `disable/GameSystems` turned off, including its original day-cycle manager. The spawner is assigned to that manager and uses **Fallback Day 1** while it is unavailable. Enable/assign an active day cycle when this scene is ready for daily progression. The spawner listens to `DayCycleManager.DayStarted`; it does not run its own clock or enable other scene systems.

## Table and pickups

The starter table is `Assets/GameData/Spawning/DefaultDailySpawnTable.asset`. Its Stick/Wood/Metal Scrap weights are 4/5/3, with one unit per pickup. Rolls allow repeats.

- Create another table with **Assets > Create > Game > Spawning > Daily Item Spawn Table**.
- Each entry takes an existing **Prefab**, an **Item** definition, a relative weight and an inclusive quantity range. The Item can be omitted if the prefab root already has a configured ItemPickup. Weight zero disables the entry.
- Use a table on the spawner as its default, or assign a zone's **Table Override**.
- Each zone rolls between **Minimum Items** and **Maximum Items**, inclusively. Both zero disables spawning in that zone.
- Register the table's item assets in the scene's InventoryManager, even with zero starting quantity. Unregistered and invalid entries are skipped with a warning.
- The starter table references the original imported world model prefabs (`stick.fbx`, `wood.fbx`, `scrap_metal.fbx`) under `Assets/Art Assets/models/items`, not the smaller inventory/drawer variants. Spawned copies keep their authored scale and rotation. ItemPickup and a collider are added to the copy only when needed, using the normal interaction prompt, outline, shine and collection. Source prefabs are not edited, duplicated or normalized. Existing collider/Rigidbody settings are retained; the spawner does not add physics bodies to model-only prefabs.

## Placement and daily reset

Spawn attempts raycast vertically through each zone and accept upward-facing surfaces on **Ground Layers**. **Blocking Layers** prevent items being embedded in geometry or other pickups. Placement retries are limited by **Placement Attempts**; an obstructed/empty zone may spawn fewer than its requested count.

Each new day removes only pickups created by this spawner and generates the next batch. Collected inventory and independently placed pickups are retained. Repeated notification for the same day does not duplicate loot. In Play mode, use the spawner's component menu **Respawn Current Day (Play Mode)** to rebuild the batch.

The seed, day and stable zone IDs drive local System.Random rolls. A forced reroll reproduces the same result when tables and collision geometry are unchanged; other Unity random consumers do not affect table selection.

## Extension points

The core intentionally does not implement planned schedules or history-based rarity changes yet:

- `ResolveTable(SpawnContext)` can be overridden to select a pre-planned table for a day or zone.
- `GetSelectionWeight(Entry, SpawnContext)` can be overridden to reduce/increase weights based on the previous batch.
- `CurrentSpawns` and `PreviousSpawns` record successful spawns (item, quantity, zone, day and initial position), including items later collected. A forced same-day reroll does not overwrite previous-day history.
- `SpawnCompleted` notifies other systems after a batch finishes.

History is runtime/session data, not a save-game implementation. When jumping over day numbers, PreviousSpawns refers to the last generated batch, with its actual day preserved in each record.