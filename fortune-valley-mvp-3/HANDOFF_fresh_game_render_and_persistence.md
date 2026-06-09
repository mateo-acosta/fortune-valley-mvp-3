# HANDOFF: Fresh-game render race + restaurant_level persistence + profile goals

**Status:** Diagnosis complete and verified. Fixes NOT yet implemented.
**Owner:** Next agent picking this up.
**Date opened:** 2026-05-15
**Severity:** HIGH. Every fresh student sees broken starter lots; every returning player loses restaurant income on refresh; profile panel shows wrong goals.

---

## TL;DR

Three bugs found during a fresh-student dev test (student9) after the savegame gate-correction (FV_v1.0.16) reached `alora-finance-dev`:

- **Bug A (Unity):** Fresh game — player's + rival's starter Tier-2 lots show "For Sale" from game start, only fixed by a hard refresh. (Income is correct pre-refresh.)
- **Bug B (Rails, latent):** Hard refresh — income rate collapses to `+$0/year` (lots restore fine). Unity sends `restaurant_level`; Rails silently drops it.
- **Bug C (Unity):** Player Profile panel shows static/sample life goals, not the ones the player selected.

User decisions already made:
- Bug A fix = **option 1 (deferred re-emit)**, not the bigger swapper refactor.
- Bug B = fix `restaurant_level` **and** do a full DTO<->Rails field-parity audit.
- Execution = implement A + B + C together, **one** Unity build + **one** Rails deploy, then re-smoketest with a brand-new dev student.

The authoritative plan also lives at `~/.claude/plans/savegame-render-and-persistence-trio.md`. This handoff is the self-contained version.

---

## Repos & environment

- **Unity:** `/Users/mateoacosta/Downloads/GitHub/fortune-valley-v3` (git root). Unity project subdir: `fortune-valley-mvp-3/`. Unity 6000.3.6f1. Unity MCP is used for compile checks + tests.
- **Rails:** `/Users/mateoacosta/Downloads/GitHub/AloraFinance_Website/alora-finance-main-website` (git root is the parent `AloraFinance_Website/`). Rails 7.1, RSpec.
- **Dev URL:** https://alora-finance-dev.fly.dev (Fly app `alora-finance-dev`, config `fly.dev.toml`).
- **Prod:** Fly app `alora-finance`, config `fly.toml` (plain `fly deploy`). Do NOT touch prod without explicit go-ahead.
- Unity Editor is normally open with `Homebase.unity`; Unity MCP connected. The test runner only starts when the Unity Editor window is FOCUSED (otherwise `run_tests` returns "failed to initialize"). Ask the user to click Unity if needed.
- The chrome-devtools MCP cannot attach while the user is actively driving the MCP browser (profile lock). Debug from code or ask the user for console/network dumps.

---

## What is ALREADY shipped (lineage — do not redo)

| Item | Where | State |
|---|---|---|
| Fix A: savegame corruption gate (15 `HandleGameStart` gated on `LastLoadedSaveDto`) | Unity PR #4, merged `fa0a989` | superseded by gate-correction |
| Fix A correction: gate on `SaveStateRestoredFromServer` not `LastLoadedSaveDto`; CityManager self-heal `_lotOwnership.Count>0` | Unity PR #5, merge `9001a89` | on Unity `main` |
| Fix B: Rails `selected_goals` jsonb persistence (column + strong params + validator + specs) | Rails PR #14, merge `eb0c79d` | on Rails `main`, deployed dev |
| FV_v1.0.15 WebGL build (Fix A) | Rails commit `e62e70c` | superseded |
| FV_v1.0.16 WebGL build (gate-correction) | Rails commit `e0f0dc0` | **currently live on dev** |

Currently on dev: Unity gate-correction (PR #5) + Rails `selected_goals`. The three bugs below are what remains.

Key architectural invariant (saved to memory `lastloadedsavedto_dual_writer.md`): `GameEvents.LastLoadedSaveDto` has two writers — `GameSaveBootstrapper.Apply` (real server restore, Phase 1, before `OnGameStart`) and `AutoSaveController.PerformSave` (autosave write-through cache). Never gate game-start logic on `LastLoadedSaveDto != null`. Use `GameEvents.SaveStateRestoredFromServer` (set true only by `GameSaveBootstrapper.Apply`, cleared by `ReplayTutorialService`, never by autosave). These statics survive `ClearAllSubscriptions` in production, so test isolation must reset them (`SaveTestsBase` + the two PlayMode save fixtures).

---

## Bug A — fresh-game starter-lot render race (Unity)

### Root cause (confirmed by code trace)

`RestaurantVisualTierSwapper.HandleGameStart` (`fortune-valley-mvp-3/Assets/Scripts/City/RestaurantVisualTierSwapper.cs:51`) — and the equivalents in `LotVisual.cs` (`HandleGameStart` ~line 227) and `LotWorldCanvas.cs` (`HandleGameStart` ~line 140) — self-reset to "For Sale" when `GameEvents.SaveStateRestoredFromServer == false`:

```csharp
private void HandleGameStart()
{
    if (GameEvents.SaveStateRestoredFromServer) return;
    _owner = Owner.None;
    _tier = 0;
    ApplyVisual();   // shows the "For Sale" sign
}
```

On a fresh game, `CityManager.HandleGameStart` (`CityManager.cs:230`) runs `SeedStarterLots()` (`CityManager.cs:244`) which raises `OnLotPurchased`/`OnLotTierChanged` **inline during the `OnGameStart` dispatch**. Every swapper's `HandleGameStart` is ALSO subscribed to `OnGameStart`. C# multicast delegates invoke subscribers in subscription order (component `OnEnable` order), so any swapper whose `HandleGameStart` runs AFTER `CityManager`'s wipes the just-seeded paint back to "For Sale".

The **refresh / returning-player path is immune** because: (a) `SaveStateRestoredFromServer == true` so swappers skip their self-reset, and (b) there is a deferred Phase-2 re-emit: `SaveRestoreCatchUp` → `CityManager.HandleSaveRestored` (`CityManager.cs:209`) → `RaiseAllOwnedLotEvents()` (`CityManager.cs:687`) fires **next frame**, after all resets. The fresh-seed path has NO equivalent deferred re-emit — that is the entire bug. It was always latent ordering fragility; the gating made it deterministically bad for fresh games.

### Fix — option 1: deferred re-emit (mirror the proven restore Phase-2)

In `fortune-valley-mvp-3/Assets/Scripts/City/CityManager.cs`:

1. Add field: `private bool _reemitOwnedLotsQueued;`
2. In `HandleGameStart`, in the fresh-game branch (the path that runs `ResetOwnership(); SeedStarterLots(); GameEvents.RaiseCityInitialized(...)`), after those calls add: `_reemitOwnedLotsQueued = true;`
3. Add (or extend existing) `private void Update()`:
   ```csharp
   private void Update()
   {
       if (_reemitOwnedLotsQueued)
       {
           _reemitOwnedLotsQueued = false;
           RaiseAllOwnedLotEvents();
       }
   }
   ```
   This re-emits owned-lot ownership/tier events one frame later — after every swapper's `HandleGameStart` reset has already run that frame — so the starters repaint and stick. `RaiseAllOwnedLotEvents()` already exists and only emits non-None lots.

Constraints:
- Verify `CityManager` has no pre-existing `Update()`; if it does, fold the check in (don't add a second one — "one type per file" and method-scope rules).
- Keep `Update()` allocation-free per the project's frame-method rule. `RaiseAllOwnedLotEvents` iterates a `Dictionary` (acceptable; runs once on the queued frame, not steady-state per-frame).
- Do NOT remove the swapper self-resets (option 2 rejected — blast radius across fresh/restore/bankruptcy).
- Restore path is unchanged (still uses `SaveRestoreCatchUp`). This is additive to the fresh path only.

Files: `fortune-valley-mvp-3/Assets/Scripts/City/CityManager.cs`.

---

## Bug B — restaurant_level not persisted + field-parity audit (Rails)

### Root cause (verified)

- Unity sends it: `GameStateDTOBuilder.cs:73` → `restaurant_level = _restaurantSystem.CurrentLevel`; declared `GamePlayerStateDTO.cs:36 public int restaurant_level;`.
- Rails drops it: NOT in `db/schema.rb` `game_player_states`, NOT in `app/controllers/api/game/states_controller.rb#state_params`, NOT in `app/models/game_player_state.rb` annotation.
- Effect: GET omits `restaurant_level` → `JsonUtility` deserializes the missing int to `0` → `RestaurantSystem.Hydrate` (`RestaurantSystem.cs:135`) sets `_currentLevel = 0` → `IncomePerTick = _config.GetIncomeForLevel(0)` = 0 → `TotalIncomeRateHUD` shows `+$0/year`. Happens on every returning-player refresh. Latent before this work; only surfaced now because refresh-income was tested for the first time.

### Step B1 — field-parity audit FIRST (sizes B2)

Build a definitive table: every public field on `fortune-valley-mvp-3/Assets/Scripts/Domain/Entities/GamePlayerStateDTO.cs` vs:
- (a) column in `db/schema.rb` `create_table "game_player_states"`,
- (b) permitted in `state_params` (`app/controllers/api/game/states_controller.rb`),
- (c) round-tripped by `present_state` / `as_json`.

Explicitly check these suspects: `restaurant_level`, `current_tick`, `current_tick_count`, `current_engine_pulse`, `yearly_income`, `liquid_net_worth`, `total_net_worth`, `bankruptcy_flag`, `current_age`, and the Stage-0a alias/legacy fields. Classify each dropped field:
- (i) **must persist** → add column + strong param (+ validation),
- (ii) **derived/transient** (Unity recomputes from other restored state) → safe to keep dropping; document why in the audit output,
- (iii) **ambiguous** → STOP and ask the user.

Report the audit table + the must-persist list to the user before writing the migration if the list is larger than just `restaurant_level`.

### Step B2 — persist must-persist fields (mirror the selected_goals change)

For `restaurant_level` (and any other must-persist scalars from B1):

- Migration `db/migrate/<YYYYMMDDHHMMSS>_add_restaurant_level_to_game_player_states.rb`:
  ```ruby
  # frozen_string_literal: true
  class AddRestaurantLevelToGamePlayerStates < ActiveRecord::Migration[7.1]
    def change
      add_column :game_player_states, :restaurant_level, :integer, default: 1
    end
  end
  ```
  Default `1` so legacy rows hydrate to a valid base level, not 0. **Confirm with the user that `RestaurantConfig.GetIncomeForLevel(1) > 0`** (base/dilapidated tier still earns) before relying on this floor.
- `state_params`: add `:restaurant_level` to the scalar permit list (alongside `:credit_score`, `:current_day`, etc.). NOT a jsonb key list.
- Validation: numericality on the model (integer, >= 1, <= max tier) — match existing model-validation style; this is scalar so it does NOT go in `game_player_state_jsonb_validators.rb`.
- `bundle exec annotate --models` (annotate-rails is configured; runs on `db:migrate`). Never hand-edit `db/schema.rb`.
- Tests: add to `spec/models/game_player_state_spec.rb` (valid/invalid level) and a round-trip block in `spec/requests/api/game/states_spec.rb` mirroring the `selected_goals` / `acquisition_costs` blocks (POST then GET, assert `restaurant_level` round-trips; assert default 1 on fresh record).
- If B1 surfaces more must-persist fields, fold them into the SAME migration (clearly named) only after user confirms scope.

Files: new migration; `db/schema.rb` (regenerated); `app/models/game_player_state.rb`; `app/controllers/api/game/states_controller.rb`; `spec/models/game_player_state_spec.rb`; `spec/requests/api/game/states_spec.rb`.

---

## Bug C — Profile panel goals static/sample (Unity)

### What's known

- The Profile panel is rendered by JS in the Unity WebGL template (`fortune-valley-mvp-3/Assets/WebGLTemplates/FortuneValley/`), fed by `ProfileWebBridge` pushing a `ProfilePanelDTO`. There is NO Rails-side profile view (confirmed: no `app/views`/`app/javascript` profile template).
- `ProfileWebBridgeLogic.FillGoals()` (`fortune-valley-mvp-3/Assets/Scripts/Managers/WebPanels/ProfileWebBridgeLogic.cs:226`): when `_selection == null` it sends an empty `selected_goals[]`; otherwise it fills from `_selection.Entries`.
- `_selection` is set via `ProfileWebBridge.SetSelection` from either: the live `OnLifeGoalsSelected` event (`ProfileWebBridge.cs:156` `HandleLifeGoalsSelected`), or the `LastLoadedSaveDto.selected_goals` catch-up in `OnEnable` (`ProfileWebBridge.cs:61-74`, requires length==3 + valid tier composition).

### Step C1 — scoped investigation (before coding)

Determine, for a fresh student who picked goals on a post-FV_v1.0.16 dev build, which is true:
1. Does `ProfileWebBridge` subscribe to `OnLifeGoalsSelected` BEFORE the tutorial goal-pick raises it? `ProfileWebBridge` is a `WebPanelBridge` — check its subscription lifecycle (does `Subscribe()` run at scene load, or only when the panel registers/opens?). If late, the live event is missed and it depends entirely on the save catch-up.
2. Does `LifeGoalSelectionService` capture the selection and does `GameStateDTOBuilder.selected_goals` (`GameStateDTOBuilder.cs:77`) put a non-null 3-entry array into the POST? (Possible tie-in with the fresh-game-start family from Bug A.)
3. Does the WebGL template JS render hardcoded placeholder/sample goal rows when it receives an empty/zero-length `selected_goals`? Inspect the profile-panel goal rendering in `Assets/WebGLTemplates/FortuneValley/`.
4. Confirm `selected_goals` actually round-trips for the test student (server row has it post-tutorial; GET returns it) — rules out a pure deploy-timing artifact (Fix B only reached dev with FV_v1.0.15; a student who did the tutorial before that never had goals persisted).

### Step C2 — fix (shape depends on C1)

Likely one of:
- **Subscription timing:** have `ProfileWebBridge` pull the current selection from `LifeGoalSelectionService` (the live source of truth) on panel open / first push, instead of relying only on `LastLoadedSaveDto` (null before first save). May need a getter/accessor or an event replay.
- **Template-JS placeholder:** fix the WebGL template JS to render an empty/"no goals yet" state on an empty array and to re-render when a non-empty `selected_goals` arrives.
- **Selection never persists:** fix the capture/persist path (may overlap Bug A's fresh-game-start family).

Files (TBD after C1): likely `fortune-valley-mvp-3/Assets/Scripts/Managers/WebPanels/ProfileWebBridge.cs` and/or `Assets/WebGLTemplates/FortuneValley/` JS.

---

## Implementation order

1. Branch Unity `fix/fresh-game-render-and-profile-goals` from `main`.
2. Implement Bug A (CityManager deferred re-emit).
3. Do Bug C Step C1 investigation; implement C2.
4. Unity MCP verify (see "Unity test gate" below).
5. Commit Unity; push; open PR → `main`. User merges.
6. Branch Rails `chore/restaurant-level-and-field-parity` from `main`.
7. Bug B Step B1 audit → report → B2 implement.
8. `bin/rails db:migrate` locally; `bundle exec rspec` (targeted green; full-suite pre-existing failures must be unchanged — see baseline note).
9. Commit Rails; push; open PR → `main`. User merges.
10. User rebuilds WebGL from Unity `main`; gives build path.
11. Bundle swap into Rails + `unity_bridge_controller.js` hash update (see "Bundle swap" below); commit "FV_v1.0.17 build" on Rails `main`; push.
12. `fly deploy -c fly.dev.toml` (see "Deploy" below).
13. User smoketests a BRAND-NEW dev student (see "Verification").
14. Prod only after dev passes and user approves.

---

## Unity test gate (via Unity MCP)

Baselines to match exactly (from FV_v1.0.16):
- EditMode (`FortuneValley.Tests.Editor`): **1098 total, 1073 passed, 0 failed, 25 skipped**.
- PlayMode (`FortuneValley.Tests.Runtime`): **137 total, 13 pre-existing failures** (BuildingCollectButtonTests ×1, CityManagerTests ×7, IntegrationTests ×2, UIManagerTests ×3), byte-identical messages, no new failures.

Procedure:
1. `mcp__UnityMCP__refresh_unity` scope=scripts compile=request mode=force wait_for_ready=true. Then wait ~45-60s (domain reload drops the MCP bridge briefly; poll `mcpforunity://editor/state` until `is_compiling:false` and reconnected).
2. `mcp__UnityMCP__read_console` types=["error"] filter_text="CS" — must be only MCP-bridge lifecycle lines, no `error CS####`.
3. `mcp__UnityMCP__run_tests` mode=EditMode assembly_names=["FortuneValley.Tests.Editor"] include_failed_tests=true → poll `get_test_job` (wait_timeout 300).
4. Same for PlayMode mode=PlayMode assembly_names=["FortuneValley.Tests.Runtime"].
5. **Test runner only starts when the Unity Editor window is focused.** If `get_test_job` returns "failed to initialize (tests did not start within timeout)" with `editor_is_focused:false`, ask the user to click the Unity window, then retry.
6. If Bug A's deferred re-emit warrants coverage, add a PlayMode test asserting starter lots are owned/painted after `OnGameStart` on a fresh game (flag false) — keep EditMode/PlayMode baselines green otherwise.
7. If a new EditMode test exercises `GameSaveBootstrapper.Apply` it MUST reset `GameEvents.SaveStateRestoredFromServer` in teardown (extend `SaveTestsBase` — it already resets the persistence statics) or it will cascade-fail the suite.

---

## Git / PR conventions

- Unity repo remote: `mateo-acosta/fortune-valley-mvp-3`. PRs merge with a merge commit (`gh pr merge <n> --merge`) to match #3/#4/#5. No CI gates on this repo (`statusCheckRollup` empty).
- Rails repo remote: `Alora-Finance/AloraFinance_Website`. Feature work goes via PR (#14 style). WebGL **build** commits go DIRECTLY to Rails `main` (no PR) — matches `e62e70c`, `e0f0dc0`, `32e6f10`, `2c2156f`.
- Commits: HEREDOC message, end with `Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>`. Never `--no-verify`, never amend, never force-push.
- The classifier may block push/PR/merge/deploy if a prior instruction said not to — get explicit user confirmation; the user may run `gh`/`fly` themselves via the `!` prefix.
- Pre-existing working-tree drift in the Unity repo: `Assets/Scenes/Homebase.unity` and `ProjectSettings/EditorSettings.asset` (Unity auto-rewrites the latter). NEVER stage these — they are unrelated session noise and ProjectSettings is a hard no.
- `HANDOFF_*.md` files at `fortune-valley-mvp-3/` are untracked working docs — do not commit them.

---

## Bundle swap procedure (Rails repo, after a new WebGL build)

The deployed build lives in `alora-finance-main-website/public/simulation/Build/` as 4 hashed files. A pure C# change typically changes `data` + `loader` + `wasm` hashes; `framework` often stays. Verify by listing both dirs.

1. Compare new build vs deployed:
   ```
   ls -la <BUILD_OUT>/Build/
   ls -la /Users/mateoacosta/Downloads/GitHub/AloraFinance_Website/alora-finance-main-website/public/simulation/Build/
   ```
2. `rm` the OLD hashed files whose hash changed; `cp` the NEW ones in. (`framework` unchanged → leave it.)
3. Update `app/javascript/controllers/unity_bridge_controller.js` `static values` block: `loaderUrl`, `dataUrl`, `frameworkUrl`, `codeUrl` (codeUrl = wasm). Update ONLY the ones whose hash changed. Path form: `/simulation/Build/<hash>.<ext>`.
4. `git add` the Build dir + the controller; commit on Rails `main`:
   ```
   chore(unity): FV_v1.0.17 build — <one-line summary of the fixes>
   ```
   (66MB `data.unityweb` triggers a benign GitHub LFS size warning on push — expected, ignore; do NOT migrate to LFS, that's out of scope.)
5. `git push origin main`.

---

## Deploy (dev)

Multi-toml Fly setup. Plain `fly deploy` = PROD. Always pass `-c` for dev.

1. **Dev Postgres does NOT auto-start.** Check first:
   ```
   cd /Users/mateoacosta/Downloads/GitHub/AloraFinance_Website/alora-finance-main-website
   fly status -a alora-finance-dev-db
   ```
   If the machine STATE is not `started`, start it and wait ~15s:
   ```
   fly machine start <machine-id> -a alora-finance-dev-db
   ```
   (If the deploy's `release_command` = `bin/rails db:prepare` fails with `could not translate host name "alora-finance-dev-db.internal"`, the DB machine is asleep — start it and redeploy.)
2. Deploy:
   ```
   cd /Users/mateoacosta/Downloads/GitHub/AloraFinance_Website/alora-finance-main-website
   fly deploy -c fly.dev.toml
   ```
3. The Bug B `restaurant_level` migration runs in `release_command`. Confirm the deploy log shows `✔ release_command ... completed successfully` BEFORE machines roll — if the release command fails the migration did NOT apply.
4. The `WARNING The app is not listening on the expected address` line is the expected dev auto-stop race (one machine sleeps, the other passes health checks). Deploy is fine if it ends with `✔ ... is now in a good state` + `DNS configuration verified`.
5. Verify the bundle is live (substitute the real new hashes):
   ```
   curl -s -o /dev/null -w "%{http_code}\n" https://alora-finance-dev.fly.dev/
   curl -s "https://alora-finance-dev.fly.dev/simulation/Build/<NEW_data_hash>.data.unityweb" -o /dev/null -w "%{http_code} %{size_download}\n"
   curl -s "https://alora-finance-dev.fly.dev/simulation/Build/<OLD_data_hash>.data.unityweb" -o /dev/null -w "%{http_code} (expect 404)\n"
   ```
6. Prod deploy (only after dev verified + user approval): `fly deploy -c fly.prod.toml` (per memory `fly_deploy_alora.md`; confirm prod toml/app name before running — prod app is `alora-finance`, `fly.toml`).

---

## Verification (brand-new dev student — REQUIRED, not a contaminated account)

Older accounts (student1, student9, student10) have server rows from broken runs. `CityManager`'s `_lotOwnership.Count > 0` self-heal will paper over Bug A on them — only a FRESH student honestly tests the fresh-tutorial path.

1. Sign up / log in as a new student → land in tutorial.
2. **Pick the 3 Life Goals during the tutorial.**
3. Complete tutorial → countdown → Homebase.
4. **No refresh — check immediately:**
   - Player starter lot shows a Tier-2 restaurant (NOT "For Sale"). [Bug A]
   - Rival starter lot shows a Tier-2 restaurant (NOT "For Sale"). [Bug A]
   - Income rate is `+$X/year` > 0. [Bug A consequence]
   - Open Player Profile → the 3 goals shown == the ones picked. [Bug C]
5. Buy a non-starter lot, upgrade one to Tier 3. Wait ~10s for autosave.
6. **Hard refresh (Cmd-Shift-R):**
   - Lots / tiers / day / balances persist. [pre-existing Fix A — must not regress]
   - Income still `+$X/year` > 0. [Bug B]
   - NetWorthProgressSlider visible. [Fix B — must not regress]
   - Profile goals still correct. [Bug C]
7. DevTools: GET `/api/game/state` response == next POST body on typed fields incl. `restaurant_level` and `selected_goals`.

If step 4 still shows "For Sale" → Bug A fix failed (check `Update()` actually fires `RaiseAllOwnedLotEvents` and that no earlier `Update` shadows it). If step 6 income is still $0 → Bug B migration didn't apply or `restaurant_level` still not permitted (check release_command log + `state_params`).

---

## Hard rules

- Do NOT modify `.unity` scenes, prefabs, or ProjectSettings. Never stage the `Homebase.unity` / `EditorSettings.asset` drift.
- Do NOT reintroduce a `LastLoadedSaveDto != null` gate. Use `SaveStateRestoredFromServer`.
- Do NOT bundle unrelated changes. Bug A+C = one Unity PR; Bug B = one Rails PR; build = direct Rails main commit.
- No `--no-verify`, no amend, no force-push, no prod deploy without explicit user approval.
- Confirm with the user before: a >1-field B1 must-persist list, the `restaurant_level` default-1 floor (needs `GetIncomeForLevel(1) > 0`), and any Bug C fix that touches the WebGL template.
- One type per file; event-driven cross-layer; `Update()` allocation-free (project arch rules in `fortune-valley-mvp-3/CLAUDE.md`).

---

## Key file:line references

| What | Where |
|---|---|
| Swapper self-reset (Bug A) | `Assets/Scripts/City/RestaurantVisualTierSwapper.cs:51`; `LotVisual.cs` ~227; `LotWorldCanvas.cs` ~140 |
| Fresh-game seed | `Assets/Scripts/City/CityManager.cs:230` (`HandleGameStart`), `:244` (`SeedStarterLots`) |
| Deferred re-emit target | `Assets/Scripts/City/CityManager.cs:687` (`RaiseAllOwnedLotEvents`), `:209` (`HandleSaveRestored`) |
| Restore Phase-2 pattern to mirror | `Assets/Scripts/Core/GameSaveBootstrapper.cs` (`_reconcileQueued` + `Update()`); `Assets/Scripts/Core/SaveRestoreCatchUp.cs` |
| Gate flag | `Assets/Scripts/Core/GameEvents.cs` (`SaveStateRestoredFromServer` ~874); set `GameSaveBootstrapper.cs:156`; cleared `ReplayTutorialService.cs:45` |
| restaurant_level send (Bug B) | `Assets/Scripts/Core/GameStateDTOBuilder.cs:73`; `Assets/Scripts/Domain/Entities/GamePlayerStateDTO.cs:36` |
| restaurant_level hydrate | `Assets/Scripts/Restaurant/RestaurantSystem.cs:135` (`Hydrate`), `:61` (`IncomePerTick`) |
| Income HUD | `Assets/Scripts/UI/HUD/TotalIncomeRateHUD.cs` (`Refresh()`) |
| Rails strong params (Bug B) | `app/controllers/api/game/states_controller.rb` `state_params` |
| Rails selected_goals precedent | `app/models/concerns/game_player_state_jsonb_validators.rb`; `spec/requests/api/game/states_spec.rb` (`selected_goals round-trip`) |
| Profile goals (Bug C) | `Assets/Scripts/Managers/WebPanels/ProfileWebBridge.cs:61,156`; `ProfileWebBridgeLogic.cs:226` (`FillGoals`); `Assets/WebGLTemplates/FortuneValley/` |
| Memory: gate invariant | `~/.claude/.../memory/lastloadedsavedto_dual_writer.md` |
| Full plan | `~/.claude/plans/savegame-render-and-persistence-trio.md` |

---

## Why this wasn't implemented in this session

Diagnosis was completed and verified across all three bugs; the user asked for a clean, self-contained handoff + plan before execution so a focused pass can implement A + B + C, do one build, one deploy, and one fresh-student smoketest rather than spreading it across a debugging-heavy conversation.
