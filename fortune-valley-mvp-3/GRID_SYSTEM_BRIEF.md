# Fortune Valley: City Grid System — Phase 1 Build Brief (Revised 2026-04-10)

## Purpose of this document

This is a handoff brief for an engineering agent who will wire up the Phase 1 grid system in the Homebase scene. The agent has not seen the prior brainstorm conversation. Everything that agent needs is in this document.

**Before writing any code, the agent MUST:**

1. Read `CLAUDE.md` at the project root. The Architecture Principles Table in section 4.1 is binding on every file produced. Special attention to the **No duplicate types** rule: several types in this brief already exist and MUST be reused, not recreated.
2. Read `Assets/Scripts/City/CityManager.cs` end to end.
3. Read every file in `Assets/Scripts/Grid/` (9 files) before touching any of them. These are existing, functional types that this task extends.
4. Confirm with the user that this brief matches their expectations before starting any integration work.

## Game context (one paragraph)

Fortune Valley is a 2.5D isometric financial literacy proof-of-concept. The player runs a restaurant chain in a small fixed-grid city. The player expands by purchasing empty lots (cash or loans), placing restaurants on them, and upgrading each restaurant through three visual tiers. An AI rival expands in parallel, applying soft pressure through lot scarcity (there is no hard win condition, only bankruptcy as a hard lose). The grid holds 7 interactive building lots plus ambient scenery: roads, a park, and non-interactive background buildings.

## The big correction: the scene is already laid out

**This is not a blank-slate city-building task.** The Homebase scene already contains a fully laid-out visual city:

- Around 75 ambient buildings under `World/Buildings`
- Roads, terrain, and environment props under `World/Environment`
- 9 interactive lot GameObjects under `World/Vacant Lots` with `LotVisual` components
- 4 neighborhood blocks under `World/Neighborhood`
- 88+ ambient scenery props (trees, rocks, hills, cliffs) under `World/City_Flat_Surroundings`
- A camera already framing the city at `(-12.49, 14.67, 19.55)` with rotation `(26.55, 119.38, 0)`

A reference screenshot is at `Assets/Screenshots/grid_probe_scene.png`. The agent should view this before starting.

**What is missing is the code-side grid metadata.** The scene exists spatially. There is no runtime lookup, no cell typing, no snap tool, and no integration between the existing `FortuneValley.Grid` scaffolded code and the scene objects.

## Scene isolation (critical)

The project has two gameplay scenes:

- `Assets/Scenes/Homebase.unity` — the main city hub (the target of this task)
- `Assets/Scenes/Investing_LearningGame.unity` — a separate Learning Level scene

These scenes are fully isolated per project rules. The Investing Learning Game has its own 5-lot setup that is NOT related to the Homebase grid system. A project-wide grep confirms **the Investing Learning Game scene has zero references to any `FortuneValley.Grid` type**. This task must not touch it. Anything the agent does to grid types must:

- Stay inside `Assets/Scripts/Grid/`
- Be attached only to objects in the Homebase scene
- Not add any code path that runs in both scenes and reaches across the boundary

## Existing grid code (must reuse, must not duplicate)

The `Assets/Scripts/Grid/` folder contains a substantial scaffolded grid implementation. All files use the namespace `FortuneValley.Grid` and compile into the `FortuneValley.Core` assembly (the root asmdef at `Assets/Scripts/FortuneValley.asmdef`, which is named `FortuneValley.Core`).

### Existing types in `FortuneValley.Grid`

| File | Type | Purpose | Status |
|---|---|---|---|
| `TileType.cs` | `enum TileType` | Tile category: Empty, Road, Park, Building, Lot, Water, Special, Border | Keep as-is |
| `IsometricGridConfig.cs` | `ScriptableObject` | Grid dimensions, tile size, editor colors | Keep, **update defaults** |
| `GridMapData.cs` | `ScriptableObject` | Persists a 2D grid of TileType + PlacedAssetData | Keep, but NOT authored yet. Phase 1 does not require authoring this. |
| `GridTile.cs` | `MonoBehaviour` | Per-cell runtime component. Stores Vector2Int position, TileType, optional linked CityLotDefinition, placed asset instance. | Keep as-is. This replaces the "CityCell" concept. |
| `IsometricUtils.cs` | Static class | GridToWorld / WorldToGrid math | **BROKEN — math must be fixed** (see below) |
| `GridLotLinker.cs` | Static class | Validates lot positions against GridMapData tiles | Keep as-is |
| `BorderConfig.cs` | `ScriptableObject` | Border/edge configuration for the city | Do not touch in Phase 1 |
| `CityBorder.cs` | `MonoBehaviour` | Runtime border rendering | Do not touch in Phase 1 |
| `CameraBoundsController.cs` | `MonoBehaviour` | Camera clamping to city bounds | Do not touch in Phase 1 |

### Related existing types in `FortuneValley.Core`

| File | Type | Purpose |
|---|---|---|
| `Assets/Scripts/Data/PlaceableAsset.cs` | `ScriptableObject` + `enum AssetCategory` | Asset that can be placed on a GridTile. Defines placement rules. Referenced by `GridTile`. |
| `Assets/Scripts/Data/CityLotDefinition.cs` | `ScriptableObject` | Lot definition with LotId, BaseCost, IncomeBonus, **and `Vector2Int _gridPosition` at line 47** |
| `Assets/Scripts/City/CityManager.cs` | `MonoBehaviour` | Tracks lots via `_allLots` list, handles ownership and win condition |

### Existing editor

- `Assets/Scripts/Editor/GridMapDataEditor.cs` is a custom inspector for GridMapData. Phase 1 does not use or modify it.

### CRITICAL: the "No duplicate types" rule

Per CLAUDE.md section 4.1, creating a new enum or class with a name that already exists in the project is a BLOCKING violation. The following types **already exist** and must be reused in Phase 1:

- `TileType` — do NOT create `CellType`. Use the existing enum.
- `GridTile` — do NOT create `CityCell`. Use the existing MonoBehaviour.
- `IsometricGridConfig` — do NOT create `CityGridConfig`. Update the existing ScriptableObject and create an asset from it.
- `GridMapData` — do NOT create a replacement. Phase 1 does not require authoring a GridMapData asset, but the type must not be duplicated.
- `PlaceableAsset` — do NOT create `CityAsset`. Use the existing type.
- Vector2Int is Unity's built-in type — do NOT create a `GridPosition` struct.

If the agent identifies a missing capability that cannot be expressed through the existing types, stop and flag it before creating a new type.

## The broken math (highest priority fix)

`Assets/Scripts/Grid/IsometricUtils.cs` uses isometric diamond projection:

```
worldX = (gridX - gridY) * halfWidth
worldZ = (gridX + gridY) * halfHeight
```

This is the correct formula for a sprite-based 2D isometric game rendered on a flat canvas. **Fortune Valley is not that.** Fortune Valley is a full 3D scene with 3D meshes, viewed from an isometric camera angle. The assets and the scene are laid out on an **orthogonal** grid:

- `road_straight.fbx` is a 2m x 2m tile with its local origin at world center, axis-aligned.
- Existing lot GameObjects sit at positions like `(-8, 1.05, 0)`, `(10, 1.05, 0)`, `(12, 1.05, 18)`. These are orthogonal, not isometric diamond.

The correct math is simple orthogonal:

```
worldX = gridX * tileWidth
worldZ = gridY * tileHeight
```

**The agent must fix `IsometricUtils.cs` to use orthogonal math.** Two acceptable approaches:

1. **Replace the math in the existing file.** Keep the class name `IsometricUtils` (renaming creates a churn that might break other callers — grep first). Replace the diamond math with orthogonal math in `GridToWorld`, `WorldToGridFloat`, `WorldToGrid`, `SnapToGrid`, and `GetTileCorners`.
2. **Rename `IsometricUtils` to `GridCoordUtils`.** Only do this if a full project grep confirms no file outside `Assets/Scripts/Grid/` references `IsometricUtils`. If anything else references it, go with option 1 to avoid a rename cascade.

Grep before choosing. Prefer the path that touches the fewest files.

## Confirmed grid parameters (measured via Unity MCP)

These values were measured directly from the running Unity editor. They are not defaults. They are facts.

| Parameter | Value | How it was measured |
|---|---|---|
| **Cell size (X and Z)** | **2.0 meters** | `road_straight.fbx` MeshRenderer localBounds.size = (2.0, 0.1, 2.0). `park_base.fbx` = same. `building_A.fbx` = (2.0, 1.65, 2.0). All existing lot GameObjects have scale (2, 0.1, 2) |
| **Grid origin** | `Vector3.zero` | Existing lot positions cleanly map to cells when origin is at world zero |
| **Grid dimensions** | **30 wide x 20 deep** (recommended) | Existing lots span X=-8 to 16 (cells -4 to 8) and Z=0 to 24 (cells 0 to 12). Ambient city extends beyond. 30x20 at 2m = 60m x 40m comfortably frames the whole scene. |
| **Scene** | `Assets/Scenes/Homebase.unity` | Confirmed. Do NOT build this in any other scene. |

### Update required to `IsometricGridConfig`

The existing defaults in `IsometricGridConfig.cs`:

- `_gridWidth = 30` (matches recommendation, keep)
- `_gridHeight = 30` (change to 20 for this project, or keep 30 if the agent prefers square grids)
- `_tileWidth = 1.0f` **(WRONG, must change to 2.0f)**
- `_tileHeight = 0.5f` **(WRONG, must change to 2.0f for orthogonal 2D-plane mapping)**

These are defaults only and do not affect already-created assets. Since no `IsometricGridConfig.asset` exists yet, changing the defaults is safe.

## The lot count drift (known, do NOT fix in Phase 1)

There is real drift between three different sources:

| Source | Lot count | Notes |
|---|---|---|
| `CityManager.cs:24` code comment | "Create 5 of these for the POC" | **Stale comment.** The code no longer has 5 lots. Do not fix the comment in Phase 1; it is cosmetic. |
| `CityManager._allLots` serialized list | **7** | Lot_Bakery, Lot_Bistro, Lot_Cafe, Lot_Corner, Lot_Diner, Lot_Hotel, Lot_Tower. These 7 `CityLotDefinition` assets are in `Assets/Scripts/Data/`. This is the authoritative count per the current design direction. |
| Homebase scene `LotVisual` GameObjects | **9** | 2 named `Lot_Cafe` / `Lot_Cafe (1)` plus `Vacant_Lot (1)` through `Vacant_Lot (7)`. All layer 8 "Lots", all scaled (2, 0.1, 2). |

**The 9 vs 7 mismatch is a real bug** but it is not the grid task. Phase 1 should:

- Tag **all 9 LotVisual GameObjects** with `GridTile` components so the grid has complete coverage.
- For the 7 GameObjects that correspond to CityLotDefinition assets, use `GridTile.LinkToCityLot` to wire the linkage.
- For the 2 extras (likely orphans), leave them tagged as `GridTile` with type `Lot` but unlinked. Log a warning via the sanity check.
- Add a note to `FIX_LOG.md` flagging the 9-vs-7 drift as a separate cleanup task.

Do NOT delete any LotVisual GameObjects. Do NOT create new CityLotDefinition assets. Do NOT rewire `CityManager._allLots`.

## Existing lot positions in the scene (measured)

All 9 LotVisual GameObjects, their world positions, and their target grid cell at 2m cell size with origin at world zero:

| Name | World position (x, y, z) | Grid cell (x, z) |
|---|---|---|
| `Lot_Cafe` | (-2.0, 1.05, 24.0) | (-1, 12) |
| `Lot_Cafe (1)` | (-2.0, 1.05, 18.0) | (-1, 9) |
| `Vacant_Lot (1)` | (12.0, 1.05, 24.0) | (6, 12) |
| `Vacant_Lot (2)` | (12.0, 1.05, 18.0) | (6, 9) |
| `Vacant_Lot (3)` | (16.0, 1.05, 0.0) | (8, 0) |
| `Vacant_Lot (4)` | (16.0, 1.05, 4.0) | (8, 2) |
| `Vacant_Lot (5)` | (10.0, 1.05, 0.0) | (5, 0) |
| `Vacant_Lot (6)` | (-8.0, 1.05, 0.0) | (-4, 0) |
| `Vacant_Lot (7)` | (-8.0, 1.05, 4.0) | (-4, 2) |

All Y values are 1.05 (lots sit above a ground layer). The grid does not care about Y. The snap tool must preserve Y.

Note: the grid has **negative X cells** (-1 and -4). `GridMapData.IsValidPosition` rejects negative coordinates. The runtime scanner this task creates should NOT use `GridMapData` for the lookup — it should store tiles in a plain `Dictionary<Vector2Int, GridTile>` so negative coordinates are allowed. Alternatively, the agent can shift the origin so all cells are non-negative (e.g. origin at `(-10, 0, 0)` makes cell (0, 0) correspond to world (-10, 0, 0)). Either is acceptable. **Default recommendation: use a Dictionary and allow negative cells.** This keeps the lots at their current world positions without touching the scene layout.

## Phase 1 deliverables

### 1. Fix `IsometricUtils` math

Replace isometric diamond math with orthogonal math in all of these methods:

- `GridToWorld(int gridX, int gridY, float tileWidth, float tileHeight)`
- `WorldToGridFloat(Vector3 worldPos, float tileWidth, float tileHeight)`
- `WorldToGrid(Vector3 worldPos, float tileWidth, float tileHeight)`
- `SnapToGrid(Vector3 worldPos, float tileWidth, float tileHeight)`
- `GetTileCorners(int gridX, int gridY, float tileWidth, float tileHeight)`

The new orthogonal math:

```csharp
Vector3 GridToWorld(int gridX, int gridY, float tileWidth, float tileHeight)
{
    return new Vector3(gridX * tileWidth, 0f, gridY * tileHeight);
}

Vector2Int WorldToGrid(Vector3 worldPos, float tileWidth, float tileHeight)
{
    return new Vector2Int(
        Mathf.RoundToInt(worldPos.x / tileWidth),
        Mathf.RoundToInt(worldPos.z / tileHeight)
    );
}
```

`GetTileCorners` should return axis-aligned rectangle corners, not diamond corners.

Update the XML doc comments on each method to say "orthogonal" instead of "isometric."

Also grep the project for any caller of `IsometricUtils` before making changes. If nothing outside the Grid folder calls it, the fix is contained. If something else depends on the diamond math, stop and ask the user.

### 2. Update `IsometricGridConfig` defaults

Change the serialized default values:

- `_tileWidth` from `1.0f` to `2.0f`
- `_tileHeight` from `0.5f` to `2.0f`
- `_gridHeight` from `30` to `20` (or keep 30 if the agent prefers square)

### 3. Create a `HomebaseGridConfig.asset`

Create a new `IsometricGridConfig` instance via the menu `Assets > Create > Fortune Valley > Grid > Grid Config`. Save it to `Assets/Data/HomebaseGridConfig.asset`. Verify that the saved values are `_tileWidth = 2.0`, `_tileHeight = 2.0`, `_gridWidth = 30`, `_gridHeight = 20`.

This asset is the runtime source of truth for Homebase's grid dimensions and tile size.

### 4. Create `CityGridRuntime` MonoBehaviour

This is the one new file in Phase 1. It is the missing runtime scanner.

- Path: `Assets/Scripts/Grid/CityGridRuntime.cs`
- Namespace: `FortuneValley.Grid`
- Layer: Core assembly (same as the rest of the Grid folder)

Serialized fields:

- `[SerializeField] private IsometricGridConfig _config` — reference to the HomebaseGridConfig asset
- `[SerializeField] private Vector3 _origin` — world position of cell (0, 0). Default `Vector3.zero`.
- `[SerializeField] private bool _logGridBuild` — debug flag

Runtime state:

- `private Dictionary<Vector2Int, GridTile> _tilesByPosition`

Lifecycle:

- `Awake()`: iterate `GetComponentsInChildren<GridTile>(true)` and populate the dictionary. Log a warning on duplicate positions and skip duplicates. If `_logGridBuild` is true, log the total count and a count per `TileType`.
- `OnDestroy()`: clear the dictionary.

Public API:

- `GridTile GetTile(int x, int z)`
- `GridTile GetTile(Vector2Int position)`
- `bool TryGetTile(Vector2Int position, out GridTile tile)`
- `IEnumerable<GridTile> GetTilesOfType(TileType type)`
- `Vector3 CellToWorld(Vector2Int position)` uses `_origin + new Vector3(position.x * _config.TileWidth, 0, position.y * _config.TileHeight)`
- `Vector2Int WorldToCell(Vector3 worldPos)` inverse, relative to `_origin`
- Property getters for `Origin`, `Width`, `Height`, `CellSize`

Rules:

- Uses the existing `GridTile` and `IsometricGridConfig` types. Does NOT duplicate any of their fields.
- Dictionary-based lookup so negative cell coordinates are allowed.
- No interaction with `GridMapData` in Phase 1. Phase 1 is scene-authored.

### 5. Scene integration

In the Homebase scene:

1. Create a new empty GameObject at `World/CityGrid` (position `(0, 0, 0)`, no parent offset).
2. Add the `CityGridRuntime` component. Wire `_config` to `HomebaseGridConfig.asset`. Set `_origin` to `Vector3.zero`. Enable `_logGridBuild` for the initial verification.
3. Reparent the existing `World/Vacant Lots` subtree under `World/CityGrid/Vacant Lots` so the runtime scanner finds them.
4. On each of the 9 LotVisual GameObjects, add a `GridTile` component. Set:
   - `_gridPosition` to the grid cell from the table above.
   - `_tileType` to `TileType.Lot`.
   - Leave `_placedAssetInstance` null.
5. For the 7 lots that match a `CityLotDefinition` asset, set `_linkedLot` to the matching definition. The match mapping:
   - `Lot_Cafe` → `Lot_Cafe.asset`
   - `Vacant_Lot (1)` → `Lot_Bakery.asset` (tentative — the agent should cross-reference `CityManager._allLots` ordering with the scene's lot names. If ambiguous, stop and ask the user.)
   - Remaining 5 mappings follow from the same cross-reference.
6. For the 2 orphan lots (no matching definition), leave `_linkedLot` null. These are the 9-vs-7 drift placeholders.

**Do NOT modify any scene object other than those listed above. Do NOT move any existing GameObject's world position. The existing scene layout must be preserved exactly.**

### 6. Populate `CityLotDefinition.GridPosition` on the 7 lot assets

Each `CityLotDefinition` asset in `Assets/Scripts/Data/` already has a `Vector2Int _gridPosition` field. Populate it with the cell coordinates matching the scene's LotVisual position. The agent should save the assets after editing.

### 7. Sanity check in `CityManager`

In `CityManager.HandleGameStart` (CityManager.cs:159), add a sanity pass:

- Find the `CityGridRuntime` in the scene via a serialized reference (NOT `FindFirstObjectByType` — add a `[SerializeField] private CityGridRuntime _cityGrid` field).
- For every lot in `_allLots`, query the grid for a tile at `lot.GridPosition`. If the tile is null, log a warning. If the tile's `TileType` is not `Lot`, log a warning. If the tile's `LinkedLot` does not match the lot, log a warning.
- This is a validation pass only. Do NOT fail, do NOT throw.

### 8. Editor snap tool

- Path: `Assets/Scripts/Editor/CityGridSnapTool.cs`
- Namespace: `FortuneValley.Editor`
- Reference the `FortuneValley.Editor.asmdef` which is at `Assets/Scripts/Editor/FortuneValley.Editor.asmdef`.

Behavior:

- Menu item `Tools/Fortune Valley/Snap Selected To Grid`.
- For every selected GameObject with a `GridTile` component:
  - Find the active scene's `CityGridRuntime` (editor can use `FindFirstObjectByType` since it is Editor code and the no-Find rule applies to runtime only — but double-check CLAUDE.md wording).
  - Read `GridTile._gridPosition`, `CityGridRuntime._origin`, and the config's `TileWidth` / `TileHeight`.
  - Snap transform to `origin + new Vector3(gridPos.x * tileWidth, currentY, gridPos.y * tileHeight)`.
- Skip GameObjects without a `GridTile`.
- If no `CityGridRuntime` in the active scene, log an error and abort.
- Phase 1 does not need a custom inspector for GridTile. Default inspector is acceptable.

### 9. Do NOT touch in Phase 1

- `GridMapData` — do not create assets, do not author tile maps
- `BorderConfig`, `CityBorder`, `CameraBoundsController` — out of scope
- `LotVisual`, `LotEdgeGlow` — out of scope
- `CheckWinCondition()` in CityManager — out of scope, known drift from the new design
- `CityManager._allLots` list — out of scope, the 9-vs-7 drift is a separate task
- The Investing Learning Game scene — scene isolation, do not open
- The existing lot positions — do not move anything

## Phase 2 foreshadowing (DO NOT BUILD)

Phase 2 will add a `RoadGraph` static builder that walks the grid, reads `TileType.Road` tiles, and connects neighboring road tiles based on `GridTile._tileType` and rotation. Cars will pathfind on this graph.

Phase 1 must make Phase 2 trivial to add. Specifically:

- `GridTile` already stores `_tileType`. The agent does not need to add rotation to `GridTile` in Phase 1 (rotation is not needed to identify lots). But if roads are later tagged, rotation will be needed. Leave this as a future addition.
- `CityGridRuntime` already exposes `GetTilesOfType(TileType.Road)`, so a future `RoadGraph.Build(cityGrid)` call has everything it needs.
- No Phase 1 decision should bake assumptions that block Phase 2.

If the agent identifies a Phase 1 decision that would complicate Phase 2, stop and flag it before proceeding.

## Scope and rough budget

- Grep and confirm `IsometricUtils` callers: 10 minutes
- Fix `IsometricUtils` orthogonal math: 30 minutes
- Update `IsometricGridConfig` defaults: 5 minutes
- Create `HomebaseGridConfig.asset`: 5 minutes
- Write `CityGridRuntime.cs`: 1 to 2 hours
- Attach `GridTile` to 9 scene lots and populate positions: 1 hour
- Populate `CityLotDefinition.GridPosition` on 7 assets: 30 minutes
- `CityManager` sanity check: 30 minutes
- Editor snap tool: 1 to 2 hours
- Pre-write declarations, post-write checklists, Unity MCP console verification: 1 hour
- Play test and regression check: 30 minutes

**Total Phase 1: roughly 1 day of focused work.** Less than my previous estimate because most of the data model already exists.

## Definition of done for Phase 1

- [ ] `IsometricUtils` math is orthogonal. A unit test or manual verification confirms cell (5, 0) maps to world `(10, 0, 0)` at 2m cell size.
- [ ] `IsometricGridConfig` defaults updated to `_tileWidth = 2.0`, `_tileHeight = 2.0`.
- [ ] `HomebaseGridConfig.asset` exists at `Assets/Data/HomebaseGridConfig.asset` with verified values.
- [ ] `Assets/Scripts/Grid/CityGridRuntime.cs` exists, compiles, and has no CLAUDE.md violations.
- [ ] Homebase scene has a `World/CityGrid` GameObject with `CityGridRuntime` attached and `_config` wired to `HomebaseGridConfig.asset`.
- [ ] All 9 existing lot GameObjects have a `GridTile` component with correct `_gridPosition` and `_tileType = Lot`.
- [ ] The 7 lots linked to `CityLotDefinition` assets have `_linkedLot` wired correctly. The 2 orphans are flagged in logs.
- [ ] All 7 `CityLotDefinition` assets in `Assets/Scripts/Data/` have their `_gridPosition` field populated.
- [ ] On Play, `CityGridRuntime.Awake` populates its dictionary. When `_logGridBuild` is true, it logs 9 tiles total, 9 of type Lot.
- [ ] `CityGridRuntime.GetTilesOfType(TileType.Lot)` returns exactly 9 `GridTile` instances.
- [ ] `CityManager` logs at most 2 warnings at game start (for the 2 orphan lots that have no `CityLotDefinition`).
- [ ] The editor menu item `Tools/Fortune Valley/Snap Selected To Grid` successfully snaps a selected `GridTile` GameObject to its grid position.
- [ ] Every new or modified `.cs` file has a completed Pre-write declaration and Post-write checklist (per CLAUDE.md section 4.1) with every item marked pass.
- [ ] Unity MCP `read_console` shows no new errors or warnings related to the grid system.
- [ ] The game still boots and the existing Homebase financial panels still work (investing, insurance, credit). No regressions.
- [ ] `FIX_LOG.md` has a new entry noting the 9-vs-7 lot drift as a separate cleanup task.

## First steps for the agent picking this up

1. Read `CLAUDE.md` section 4.1 carefully, especially the Pre-write declaration rule, Post-write checklist rule, and **No duplicate types** rule.
2. Read every file in `Assets/Scripts/Grid/` and `Assets/Scripts/Data/PlaceableAsset.cs` and `Assets/Scripts/Data/CityLotDefinition.cs` and `Assets/Scripts/City/CityManager.cs`. Understand what exists before touching anything.
3. View the screenshot at `Assets/Screenshots/grid_probe_scene.png` to see the current Homebase layout.
4. Grep the project for `IsometricUtils` callers to confirm the math fix is contained.
5. Confirm with the user the 7-lot-to-9-scene-object mapping (especially which `Vacant_Lot` GameObject corresponds to which of `Lot_Bakery`, `Lot_Bistro`, `Lot_Corner`, `Lot_Diner`, `Lot_Hotel`, `Lot_Tower`). The cafes are obvious. The other 5 are ambiguous without user input.
6. Produce a Pre-write declaration block for `CityGridRuntime.cs` and `CityGridSnapTool.cs`. Verify every field before writing code.
7. Work in order: fix `IsometricUtils` math first (since everything else depends on correct math), then update `IsometricGridConfig` defaults, then create the config asset, then write `CityGridRuntime`, then do scene integration, then `CityLotDefinition` field population, then `CityManager` sanity check, then editor snap tool.
8. After each file is written, run the Post-write checklist and fix any FAILs before writing the next file.
9. After code compiles cleanly, use Unity MCP `read_console` to verify no errors.
10. Do NOT mark Phase 1 done until a Play test shows the grid populating correctly and no regressions appear in existing Homebase flows.
11. If any decision in this brief conflicts with CLAUDE.md, CLAUDE.md wins. Stop and ask the user before proceeding.
