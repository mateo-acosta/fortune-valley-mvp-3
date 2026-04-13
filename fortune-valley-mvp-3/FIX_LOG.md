# Fix Log

Running backlog of things to fix, change, or revisit. Mateo dictates, Claude logs.

Format: newest entries at the top of each section. Each entry has a date and a short description. Move items between sections as status changes.

---

## Open

- **2026-04-10** — Lot count drift: `CityManager._allLots` has 7 `CityLotDefinition` assets wired (Lot_Bakery, Lot_Bistro, Lot_Cafe, Lot_Corner, Lot_Diner, Lot_Hotel, Lot_Tower), but the Homebase scene has 9 `LotVisual` GameObjects (2 cafes plus 7 vacant lots). 2 scene objects are orphans with no matching `CityLotDefinition`. Also, the `CityManager.cs:24` comment still says "5 for POC" which is stale. Reconcile the scene objects with the lot definitions, or document which 2 are intentional extras. NOT part of the grid task.
- **2026-04-10** — `CityManager.CheckWinCondition()` at CityManager.cs:259 still enforces the old "own all lots to win" logic. The design direction has shifted to city-builder with no hard win (only bankruptcy as a hard lose). Update `CheckWinCondition` and the `OnGameEnd` wiring to match the new direction. See `game_vision_prd` memory for the locked direction.
- **2026-04-10** — `Assets/Scripts/Grid/IsometricUtils.cs` uses isometric diamond projection math, which is wrong for this project (the art is full 3D, not sprite-based isometric). Must be replaced with orthogonal math before any grid work. Details in `GRID_SYSTEM_BRIEF.md`.
- **2026-04-10** — HUD credit balance sometimes displays the investing balance instead of the checking/credit balance. Source of the value is unclear and likely wrong. Need to trace where the HUD credit field is bound and confirm it reads the right account.
- **2026-04-10** — Investing home subpanel graph appears to be wired to the investing Learning Level scene's graph, not to the player's Homebase holdings. The Homebase subpanel should show only the holdings and a graph of those holdings, fully isolated from the Learning Level scene. This is a scene-isolation violation and a bigger-than-surface fix. See memory `scene_isolation.md` for the hard rule.

---

## In Progress

_Nothing in progress._

---

## Done

_Nothing completed yet._

---

## Notes

- Anything Mateo mentions as broken, annoying, or needing change goes in Open.
- When work starts, move to In Progress.
- When finished (and confirmed), move to Done with the completion date.
- Items can be dropped entirely if Mateo says they're no longer relevant.
