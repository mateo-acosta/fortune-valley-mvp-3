# Fix Log

Running backlog of things to fix, change, or revisit. Mateo dictates, Claude logs.

Format: newest entries at the top of each section. Each entry has a date and a short description. Move items between sections as status changes.

---

## Open

- **2026-04-13** — Insurance UI does not clearly explain the difference between General Protection and Non-General (specialty) Protection policies. Players can't tell what each type actually covers, what accidents/events fall under which bucket, or why they'd pick one over the other. Add copy (tooltip, description field on each policy card, or a side-by-side comparison) that lists covered accident types per policy tier so the choice is informed.
- **2026-04-13** — Insurance appears to allow buying two policies simultaneously. Expected behavior: one active policy at a time (or explicit per-lot assignment). Also, there is no visible UI to select/assign an insurance policy to a specific lot. Clarify the intended model (one policy covers all lots vs per-lot policy) and either enforce single-policy or add a lot-selection step in the insurance purchase flow.
- **2026-04-10** — Lot count drift: `CityManager._allLots` has 7 `CityLotDefinition` assets wired (Lot_Bakery, Lot_Bistro, Lot_Cafe, Lot_Corner, Lot_Diner, Lot_Hotel, Lot_Tower), but the Homebase scene has 9 `LotVisual` GameObjects (2 cafes plus 7 vacant lots). 2 scene objects are orphans with no matching `CityLotDefinition`. Also, the `CityManager.cs:24` comment still says "5 for POC" which is stale. Reconcile the scene objects with the lot definitions, or document which 2 are intentional extras. NOT part of the grid task.
- **2026-04-10** — `CityManager.CheckWinCondition()` at CityManager.cs:259 still enforces the old "own all lots to win" logic. The design direction has shifted to city-builder with no hard win (only bankruptcy as a hard lose). Update `CheckWinCondition` and the `OnGameEnd` wiring to match the new direction. See `game_vision_prd` memory for the locked direction.
- **2026-04-10** — `Assets/Scripts/Grid/IsometricUtils.cs` uses isometric diamond projection math, which is wrong for this project (the art is full 3D, not sprite-based isometric). Must be replaced with orthogonal math before any grid work. Details in `GRID_SYSTEM_BRIEF.md`.
- **2026-04-10** — Investing home subpanel graph appears to be wired to the investing Learning Level scene's graph, not to the player's Homebase holdings. The Homebase subpanel should show only the holdings and a graph of those holdings, fully isolated from the Learning Level scene. This is a scene-isolation violation and a bigger-than-surface fix. See memory `scene_isolation.md` for the hard rule.

---

## In Progress

_Nothing in progress._

---

## Done

- **2026-04-13** — HUD credit balance sometimes displayed the investing portfolio value. Root cause was two-part: `GameHUD._checkingDisplay`/`_investingDisplay` were null in the Homebase scene, so `HandleGameStart` never collapsed the three status bars, and no `_labelText` was wired on any `AccountDisplay`, leaving three unlabeled bars. Fix: removed the collapse logic in `GameHUD` (user wants all three balances always visible independently) and added an inline label prefix in `AccountDisplay` when `_labelText` is null ("Checking:", "Investing:", "Credit:"). Each display already self-subscribes to its correct event based on `_accountType`, so the underlying values were never crossed.

---

## Notes

- Anything Mateo mentions as broken, annoying, or needing change goes in Open.
- When work starts, move to In Progress.
- When finished (and confirmed), move to Done with the completion date.
- Items can be dropped entirely if Mateo says they're no longer relevant.
