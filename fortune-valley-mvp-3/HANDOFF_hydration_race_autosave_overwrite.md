# HANDOFF: Hydration race - autosave overwrites real save on cold dev boot (CRITICAL)

**Status:** Diagnosis complete and code-traced. Fix NOT implemented.
**Owner:** Fresh Claude Code instance picking this up (run in isolation - see Coordination).
**Date opened:** 2026-05-18
**Severity:** CRITICAL. On the dev environment a returning player's server save is permanently overwritten with fresh-game defaults on a cold boot. Mid-game progress lost.

---

## TL;DR

The 2026-05-15 corruption fix gated the 15 destructive `HandleGameStart` resets on `GameEvents.SaveStateRestoredFromServer`. That gate is correct but only helps if the server save has already arrived when `OnGameStart` fires. **Nothing makes the game wait for the save round-trip, and the autosave has no gate at all.** On a cold dev boot the `GET /api/game/state` is slow or fails (Fly scale-to-zero, dev Postgres no auto-start), the fixed ~3s countdown finishes first, `OnGameStart` fires with `SaveStateRestoredFromServer == false`, all 15 systems reset to fresh defaults, and ~10 ticks later `AutoSaveController` POSTs that fresh state over the real server row. Permanent.

**The fix:** two barriers. (1) Do not run the destructive `OnGameStart` path until the save round-trip resolves (`SaveStateRestoredFromServer` OR `HasServerConfirmedFreshUser` OR a bounded timeout). (2) `AutoSaveController.PerformSave()` must refuse to POST until that same condition holds.

---

## What the user experienced (verbatim context)

User was mid-game in the **deployed web dev app** (browser, `https://alora-finance-dev.fly.dev`, NOT the Unity Editor). After playing, they suddenly saw: income `$0/year`, investing balance gone, net worth reset to `$10,000`, mid-game. Game was still running when reported.

This is the fresh-game default state written over their progress.

---

## This is NOT caused by the in-flight UI work (read before touching anything)

A separate Claude instance is concurrently finishing three unrelated UI fixes (liquid-cash label, best-quiz-streak persistence, investing recently-viewed) on branch `feat/economy-retune`. Those changes are **uncommitted, never built, never deployed** - they did not and cannot affect the dev environment. The dev app runs a previously-deployed build. This hydration race is a pre-existing latent bug, independent of that work. Confirmed: nothing the UI instance changed reached dev.

---

## Root cause (full code trace)

Boot path for a returning player:

`GameFlowController` (`Assets/Scripts/Managers/GameFlowController.cs`):
Title -> rules carousel -> `RaiseStartCountdown()` -> countdown (~3s, fixed) -> `HandleCountdownComplete()` -> `_gameManager.StartGame()` (`GameFlowController.cs:148`).

`GameManager.StartGame()` (`Assets/Scripts/Managers/GameManager.cs:141`):
1. `WireAutoSave()` (`:150`) - constructs `GameStateDTOBuilder`, raises `OnStateBuildFuncProvided` so `AutoSaveController._buildStateFunc` is set.
2. `GameEvents.RaiseGameStart()` (`:151`) - all `OnGameStart` subscribers run synchronously:
   - 15 destructive subscribers: `if (GameEvents.SaveStateRestoredFromServer) return;` else reset to fresh defaults (`CurrencyManager.ResetBalance`, `InvestmentSystem.ResetPortfolioState`, `RestaurantSystem._currentLevel = 1`, `TimeManager.ResetTime`, etc.). Full list in `HANDOFF_savegame_corruption_fix.md` Category A.
   - `TimeManager.HandleGameStart` -> `ResetTime(); StartTime()` -> `OnTick` begins.

The save JSON arrives on a **separate, network-bound timeline**: JS bridge (`alora-finance-main-website/app/javascript/controllers/unity_bridge_controller.js`) does `GET /api/game/state`, then `SendMessage("GameSaveBootstrapper","OnSaveLoaded",json)` -> `GameSaveBootstrapper.Apply` (`Assets/Scripts/Core/GameSaveBootstrapper.cs:117`) -> sets `GameEvents.SaveStateRestoredFromServer = true` (`:156`), raises `OnSaveStateLoaded`. The countdown is a fixed timer; it is NOT a wait on this round-trip.

**Gap 1 - no start barrier.** If `GET`/`SendMessage` lands after the countdown finishes, `OnGameStart` fires with `SaveStateRestoredFromServer == false`. All 15 systems reset to fresh-game defaults. (`GameManager.StartGame` / `GameFlowController.HandleCountdownComplete`.)

**Gap 2 - no autosave barrier.** `AutoSaveController.PerformSave()` (`Assets/Scripts/Core/AutoSaveController.cs:98`) gates only on `_apiClient.CanPersist()` and `_buildStateFunc != null`. It does NOT check `SaveStateRestoredFromServer` or `HasServerConfirmedFreshUser`. `HandleTick` POSTs every `_saveIntervalTicks = 10` ticks (`:22`, `:77-84`). So ~10 ticks after `OnGameStart` the fresh default state is POSTed over the real server row. A late-arriving save re-hydrates Unity's in-memory view but cannot un-corrupt the row the autosave already overwrote; the next `GET` returns the corrupted row.

If the cold `GET` fails outright, the bridge sends empty -> `GameEvents.HasServerConfirmedFreshUser = true` (`GameSaveBootstrapper.cs:144`), no hydrate at all, and the fresh game is autosaved over the real row as if legitimate.

---

## Why dev specifically, not prod

Dev Fly app scales to zero and dev Postgres does not auto-start (see memory `deploy_unity_build_url_sync.md`: "fly deploy scale-to-zero false negative"; `HANDOFF_fresh_game_render_and_persistence.md` deploy section: "Dev Postgres does NOT auto-start"). A cold `GET /api/game/state` is slow or fails while the machine/DB wakes. The fixed ~3s countdown finishes long before a cold backend answers, so `OnGameStart` wins the race on every cold boot, the resets run, and the fast local 10-tick autosave clock overwrites the real save before the slow/failed restore resolves. Prod stays warm, the `GET` beats the countdown, so the bug is latent there but live on dev.

---

## Fix plan (both barriers required)

Honor the memory invariant `lastloadedsavedto_dual_writer.md`: gate on `SaveStateRestoredFromServer`, NEVER `LastLoadedSaveDto` (autosave write-through also sets `LastLoadedSaveDto`). The two flags are defined in `Assets/Scripts/Core/GameEvents.cs` (`SaveStateRestoredFromServer` ~line 885, `HasServerConfirmedFreshUser` ~line 896; both intentionally survive `ClearAllSubscriptions`).

Define one resolved condition: `saveRoundTripResolved = SaveStateRestoredFromServer || HasServerConfirmedFreshUser || boundedTimeoutElapsed`. The timeout is the dev safety valve so a permanently-asleep backend cannot soft-lock the game forever; pick a generous bound (suggest a serialized field, default ~15-20s of unscaled time) and confirm the value with the user before finalizing (no bare numeric literals per `CLAUDE.md` arch rules).

1. **Start barrier.** Hold the destructive `OnGameStart` path until `saveRoundTripResolved`. Options to evaluate (pick one, justify in the PR):
   - Defer `StartGame()`/the `RaiseGameStart()` call from `GameFlowController.HandleCountdownComplete` until resolved (a small waiter that fires `StartGame` when the condition flips, with the timeout fallback). Cleanest single choke point.
   - Or: keep `StartGame()` timing but make the 15 destructive `HandleGameStart` bodies no-op until resolved and re-emit fresh-seed once resolved-as-fresh. Larger blast radius; mirrors the prior gate work. Prefer the first.
   The fresh-player path (`HasServerConfirmedFreshUser`) and the replay-tutorial path (`ReplayTutorialService` clears `SaveStateRestoredFromServer`) must still seed a fresh game correctly. Re-verify both.
2. **Autosave barrier.** In `AutoSaveController.PerformSave()` add an early return unless `saveRoundTripResolved`. This is the hard backstop: even if Gap 1's barrier is bypassed by some path, an un-hydrated state can never be POSTed over the server row. Make sure `FlushFinalSave()` / `OnGameEnd` / `OnGoalRealized` / `OnSoftBankruptcyReset` paths also respect it (they all funnel through `PerformSave`).

Add regression coverage (PlayMode): cold-boot simulation where `OnGameStart` fires with `SaveStateRestoredFromServer == false` and no save delivered, assert no POST occurs and destructive resets did not run, then deliver the save and assert hydrate + first POST equals restored state. Reset both persistence statics in teardown (extend `SaveTestsBase` - it already resets the persistence statics; see `HANDOFF_fresh_game_render_and_persistence.md` "Unity test gate").

---

## Coordination / isolation (status: UI fixes ALREADY MERGED to main)

The three UI fixes are **no longer uncommitted** - they merged to `origin/main` via Unity PR #12 (`mateo-acosta/fortune-valley-mvp-3#12`, merge commit `e85d886`) and Rails PR #18 (`Alora-Finance/AloraFinance_Website#18`, merge commit `cf9038c`). There is NO live working-tree collision anymore. Branch off the LATEST `main` and you automatically include the UI fixes.

- **Work in a separate git worktree off the latest `main`.** From the Unity repo root (`/Users/mateoacosta/Downloads/GitHub/fortune-valley-v3`):
  `git fetch origin && git worktree add ../fv-hydration-fix -b fix/hydration-race-autosave-overwrite origin/main`
  Work entirely in `../fv-hydration-fix`. If a worktree was already created off an older `main` (pre-#12), rebase it onto `origin/main` before the final build so the UI fixes are included.
- **UI-fix files now ON `main` (already merged - read them as-is, do not revert):**
  - Unity: `GameEvents.cs` (a `BestQuizStreak` static property sits near the other survive-reload statics ~`:897`), `GameStateDTOBuilder.cs`, `GamePlayerStateDTO.cs`, `ProfilePanelDTO.cs`, `GameManager.cs` (one added line in `HandleSaveStateLoaded` only - NOT in `StartGame`), `QuestionManager.cs`, `ProfileWebBridgeLogic.cs`, `PlayerProfile.html`, `investing-system.html`.
  - Rails: `states_controller.rb`, `game_player_state.rb`, migration `20260518000001_add_best_quiz_streak_to_game_player_states.rb` (merged; Rails local `main` already reconciled).
  - **Overlap is now harmless:** your fix will touch `GameManager.cs` (`StartGame`) and likely `GameEvents.cs` / `AutoSaveController.cs`. The UI edits are in different regions (`GameManager.HandleSaveStateLoaded`; a new `GameEvents` static). Since you branch off `main` which already contains them, you edit on top - no cross-PR merge conflict.
- **Single combined build.** The user is deferring the Unity build/deploy until THIS fix is merged, then doing ONE rebuild from `main` (UI fixes + this hydration fix together) and one dev deploy. So: implement, MCP-verify, PR to `main`, user merges. Do not trigger a separate build/deploy for this fix alone.
- `feat/economy-retune` is dead (already merged long ago); ignore it entirely. Do NOT branch from it.

---

## Test gate, deploy, hard rules

Reuse the procedures already documented in `HANDOFF_fresh_game_render_and_persistence.md` (same directory): "Unity test gate (via Unity MCP)" for compile + EditMode/PlayMode baselines, "Bundle swap procedure" and "Deploy (dev)" for shipping, "Hard rules" verbatim. Key points:

- Unity MCP: `refresh_unity` (scope=scripts, compile=request, force), poll `editor_state` until not compiling, `read_console` types=["error"] filter "CS" must be clean, then `run_tests` EditMode + PlayMode. Match the FV_v1.0.16 baselines noted in that handoff; no new failures.
- The test runner only starts when the Unity Editor window is focused; ask the user to click Unity if `get_test_job` reports "failed to initialize".
- Do NOT modify `.unity` scenes, prefabs, or `ProjectSettings`. Never stage the `Homebase.unity` / `EditorSettings.asset` drift.
- `CLAUDE.md` arch rules apply: pre-write declaration + post-write checklist per `.cs` file, one type per file, event-driven cross-layer, no bare numeric literals (the timeout must be a serialized/config field), `Update()` allocation-free, no em dashes anywhere.
- This is dev-impacting and prod-latent. Dev deploy via `fly deploy -c fly.dev.toml` (start the dev Postgres machine first - see the deploy section in the other handoff). Prod only after dev verified and explicit user approval.
- `HANDOFF_*.md` files are untracked working docs; do not commit them.

---

## Key file:line references

| What | Where |
|---|---|
| Boot -> StartGame | `Assets/Scripts/Managers/GameFlowController.cs:146-149` (`HandleCountdownComplete`) |
| `StartGame()` -> `RaiseGameStart` | `Assets/Scripts/Managers/GameManager.cs:141-160` (`:150` WireAutoSave, `:151` RaiseGameStart) |
| Destructive resets (gated) | 15 `HandleGameStart` bodies - full list in `HANDOFF_savegame_corruption_fix.md` Category A |
| Gate flag set on real restore | `Assets/Scripts/Core/GameSaveBootstrapper.cs:156` (`SaveStateRestoredFromServer = true`) |
| Empty-payload fresh-user flag | `Assets/Scripts/Core/GameSaveBootstrapper.cs:144` (`HasServerConfirmedFreshUser = true`) |
| Autosave (NO gate - Gap 2) | `Assets/Scripts/Core/AutoSaveController.cs:98-115` (`PerformSave`); `:77-84` (`HandleTick`, `_saveIntervalTicks = 10` at `:22`) |
| Gate flags defined | `Assets/Scripts/Core/GameEvents.cs` (~`:885` `SaveStateRestoredFromServer`, ~`:896` `HasServerConfirmedFreshUser`) |
| Replay-tutorial clears restore flag | `Assets/Scripts/Managers/Tutorial/ReplayTutorialService.cs` |
| Memory invariant | `~/.claude/projects/-Users-mateoacosta-Downloads-GitHub-fortune-valley-v3/memory/lastloadedsavedto_dual_writer.md` |
| Prior corruption diagnosis (lineage) | `HANDOFF_savegame_corruption_fix.md`, `HANDOFF_fresh_game_render_and_persistence.md` (same dir) |

---

## Why not implemented here / current sequencing

This race was diagnosed (fully code-traced above) while another instance shipped three unrelated UI fixes. Those UI fixes are now MERGED to `main` (Unity PR #12, Rails PR #18); Rails local `main` is reconciled with the live FV_v1.0.19 build commits + the merged `best_quiz_streak` change (not yet pushed - it goes up with the next bundle-swap commit).

Current plan, set by the user: implement THIS hydration fix next, MCP-verify, PR to `main`, user merges. THEN the user does a single Unity rebuild from `main` (which by then contains the UI fixes + this fix), one bundle swap into Rails `main`, one `fly deploy -c fly.dev.toml`. No standalone build/deploy for this fix. What remains: implementation, regression tests, then the combined build + dev cold-boot smoketest.
