# HANDOFF: Save-State Corruption on Reload (CRITICAL)

**Status:** Diagnosis complete and verified live. Fix not yet implemented.
**Owner:** Next Claude / agent picking this up.
**Date opened:** 2026-05-15
**Severity:** CRITICAL — every browser refresh permanently corrupts the returning player's server-side save (lots, balances, credit score, day, tiers, loans, insurance, investments all reset).

---

## TL;DR

`OnGameStart` fires AFTER `OnSaveStateLoaded` for returning players. 15 subscribers have `HandleGameStart` methods that reset state to fresh-game defaults. Those resets overwrite the just-hydrated values. Within ~10 ticks, AutoSaveController POSTs the reset state to the server, permanently destroying the player's progress.

**The fix:** gate every destructive `HandleGameStart` on `GameEvents.LastLoadedSaveDto == null`. That way returning players (DTO present) skip the reset and trust hydration; fresh players (DTO null) run the reset as before.

---

## Live Evidence (dev, FV_v1.0.14, student1@student.com, 2026-05-15)

**GET `/api/game/state` on page load returned (server state, pre-corruption):**
```json
{
  "current_day": 86,
  "checking_balance": "86010.0",
  "credit_score": 680,
  "lots_owned": ["lot_block07", "lot_block03"],
  "rival_lots_owned": ["lot_block04", "lot_block19"],
  "franchise_levels": [
    {"lot_id":"lot_block03","tier":1},
    {"lot_id":"lot_block04","tier":1},
    {"lot_id":"lot_block07","tier":2},
    {"lot_id":"lot_block19","tier":3}
  ],
  "acquisition_costs":[{"cost":35000,"lot_id":"lot_block03"}]
}
```

**Seconds later Unity POST `/api/game/state` overwrote it with:**
```json
{
  "current_day": 1,
  "checking_balance": 11260,
  "credit_score": 650,
  "lots_owned": ["lot_block07"],
  "rival_lots_owned": ["lot_block19"],
  "franchise_levels": [
    {"lot_id":"lot_block07","tier":2},
    {"lot_id":"lot_block19","tier":2}
  ],
  "acquisition_costs": []
}
```

Block03 (player) and block04 (rival) erased. Block19 downgraded tier 3 → 2. Day 86 → 1. Checking $86,010 → $11,260. Credit 680 → 650. Acquisition costs lost.

**Unity console order (`msgid`):**
```
96  [GameSaveBootstrapper] OnSaveLoaded received: 715 bytes
99  [GameSaveBootstrapper] restored homebase day=160 lots=1
101 [GameManager] Game started!     ← OnGameStart fires AFTER hydrate
```

That confirms `OnGameStart` fires after `OnSaveStateLoaded` on returning-player boot.

---

## Why `OnGameStart` fires AFTER hydrate

1. Unity scene loads; all `Awake()` + `OnEnable()` complete; all systems subscribe to events.
2. JS bridge (`unity_bridge_controller.js`) waits for Unity ready, then calls `unityInstance.SendMessage("GameSaveBootstrapper", "OnSaveLoaded", json)`.
3. `GameSaveBootstrapper.Apply(json)`:
   - Parses DTO.
   - Sets `GameEvents.LastLoadedSaveDto = dto`.
   - Raises `OnSaveStateLoaded(dto)` (**Phase 1**).
   - Queues Phase 2 for next `Update()`.
4. Phase 1 subscribers run synchronously. `CityManager.Hydrate(dto)` fills `_lotOwnership`, fires per-lot events, swappers paint correctly. `CurrencyManager.HandleSaveStateLoaded` sets checking. Etc.
5. Some time after this — empirically AFTER step 4 — `GameManager.Start()` calls `StartGame()` which raises `OnGameStart`.
6. All 22 `OnGameStart` subscribers run. 15 of them do destructive resets that overwrite the hydrated state.
7. Phase 2 fires next frame. `CityManager.RaiseAllOwnedLotEvents` iterates `_lotOwnership` — but it was wiped to starters-only in step 6, so the catch-up has nothing left.
8. Within 10 ticks AutoSaveController POSTs the reset state. Server data permanently destroyed.

`GameManager.Start()` ordering is the surprise. Unity's per-component `Start()` runs once after all `Awake/OnEnable`, but the JS-bridge `SendMessage` arrives asynchronously after Unity signals "ready," which can land before or after `GameManager.Start()` runs. In current dev builds it consistently lands BEFORE `Start()`. Don't rely on that ordering; fix at the subscriber level instead of trying to reorder.

---

## The Complete Subscriber Audit

22 subscribers total. Grep used: `grep -rn "OnGameStart\s*+=\|OnGameStart\s*-=" Assets/Scripts/`.

### 🔴 CATEGORY A — Destructive (MUST be gated)

For each, add `if (GameEvents.LastLoadedSaveDto != null) return;` as the first line of `HandleGameStart`. Do NOT remove any existing reset logic; just guard it.

| # | File:Line | Current body |
|---|---|---|
| A1 | `Assets/Scripts/City/CityManager.cs:223` | `ResetOwnership(); SeedStarterLots(); GameEvents.RaiseCityInitialized(_allLots.Count);` |
| A2 | `Assets/Scripts/City/LotVisual.cs` | `_currentOwner = Owner.None; _isRivalTarget = false; _daysUntilRivalPurchase = 0; UpdateVisuals();` |
| A3 | `Assets/Scripts/City/RestaurantVisualTierSwapper.cs:51` | `_owner = Owner.None; _tier = 0; ApplyVisual();` |
| A4 | `Assets/Scripts/Core/TimeManager.cs` | `ResetTime(); StartTime();` |
| A5 | `Assets/Scripts/CreditCard/CreditScoreSystem.cs` | `ResetCardAndScore();` |
| A6 | `Assets/Scripts/Currency/CurrencyManager.cs` | `ResetBalance();` |
| A7 | `Assets/Scripts/Investment/InvestmentSystem.cs` | `ResetPortfolioState();` |
| A8 | `Assets/Scripts/Investment/PortfolioHistoryTracker.cs` | `_totalWealthHistory.Clear(); _netGainHistory.Clear(); _portfolioValueHistory.Clear(); TakeSnapshot();` |
| A9 | `Assets/Scripts/Investment/StockPriceHistoryStore.cs` | `_history.Clear(); …pre-populate 30 days…` |
| A10 | `Assets/Scripts/Loan/LoanSystem.cs` | `_portfolio = new LoanPortfolio();` |
| A11 | `Assets/Scripts/Insurance/InsuranceSystem.cs` | `_portfolio = new InsurancePortfolio();` |
| A12 | `Assets/Scripts/Restaurant/RestaurantSystem.cs` | `_currentLevel = 1; _totalEarned = 0f;` |
| A13 | `Assets/Scripts/Rival/RivalAI.cs` | `_money = _config.StartingMoney; …RaiseRivalBalanceChanged; reset timing; RefreshAvailableLotsCache();` |
| A14 | `Assets/Scripts/UI/HUD/LifeGoalsHud.cs:73` | `UpdateAgeText(StartingAge); _allGoalsRealized = false; SetActive(false);` |
| A15 | `Assets/Scripts/UI/World/LotWorldCanvas.cs` | `_owner = Owner.None; _tier = 0; RefreshDisplay();` |

### 🟢 CATEGORY B — Safe (leave as-is)

These do not write to game state. They read-and-display, or self-guard, or are pure setup:

- `Assets/Scripts/Core/GameSessionController.cs` — self-guarded by `_sessionStarted`; starts session via JS bridge.
- `Assets/Scripts/Insurance/AccidentSystem.cs` — builds accident roller from SO defs.
- `Assets/Scripts/Payment/MonthlyPaymentDayController.cs` — `_state = PaymentState.Idle;` (state machine reset; idempotent enough — re-derives from day boundary).
- `Assets/Scripts/Restaurant/DailyIncomeAccumulator.cs` — clears warning cache + `EnsureBucket(RestaurantBuildingId)` (idempotent).
- `Assets/Scripts/UI/GameEndChatIntegration.cs` — destroys chat UI if any.
- `Assets/Scripts/UI/HUD/GameTimerDisplay.cs` — sets text to "Age 25" (mildly wrong for returning player for one frame; harmless).
- `Assets/Scripts/UI/HUD/ScoreboardDisplay.cs`, `RivalIncomeRateHUD.cs`, `TotalIncomeRateHUD.cs` — read current state, refresh UI.
- `Assets/Scripts/UI/Panels/Investing/InvestingTradeSubPanel.cs` — `ClearRecentlyViewed()` (UI cache only).

If you want to also gate the GameTimerDisplay so the age text doesn't briefly flash to 25 for returning players, that's optional polish. Not required for the data-corruption fix.

---

## Replay-Tutorial Path Must Still Work

`Assets/Scripts/Managers/Tutorial/ReplayTutorialService.cs:RequestReplay`:
1. Calls `_apiClient.WipePlayerState(gameMode)` (server wipe).
2. Clears the local PlayerPrefs tutorial flag.
3. Mutates `_playerStateAccessor.Current.tutorial_completed = false`.
4. Raises `OnTutorialStartRequested(isReplay: true)`.
5. **Does NOT touch `LastLoadedSaveDto` or `HasSaveBeenRestored`.**

After replay-tutorial completes, the tutorial flow eventually triggers `GameManager.StartGame()` → `OnGameStart`. With our gate in place, the destructive resets would be **blocked** because `LastLoadedSaveDto` is still the pre-wipe stale DTO. That breaks replay-tutorial.

**Fix to land alongside the gates:** in `ReplayTutorialService.RequestReplay` (after `WipePlayerState`), null out the cached DTO:

```csharp
GameEvents.LastLoadedSaveDto = null;
GameEvents.HasSaveBeenRestored = false;
```

This ensures the gates pass for replay-tutorial flow, so subscribers reset to fresh-game defaults as intended.

---

## How `LastLoadedSaveDto` Lifecycle Works

`Assets/Scripts/Core/GameEvents.cs` defines `LastLoadedSaveDto` as a static property.

Writers (verified by grep `LastLoadedSaveDto\s*=`):
- `Assets/Scripts/Core/GameSaveBootstrapper.cs:152` — set on initial save load from server.
- `Assets/Scripts/Core/AutoSaveController.cs:109` — write-through cache after every autosave (`GameEvents.LastLoadedSaveDto = state;`).
- After this fix: `Assets/Scripts/Managers/Tutorial/ReplayTutorialService.cs` — explicitly null on replay-tutorial.

Implications:
- For a **fresh** player on initial boot: `LastLoadedSaveDto == null` until the first autosave fires. So when `OnGameStart` fires on initial boot, gates pass; resets run; fresh game is seeded correctly. ✓
- For a **returning** player on initial boot: `LastLoadedSaveDto != null` after Phase 1. Gates block; hydration data is preserved. ✓
- **Within a session**: `LastLoadedSaveDto` is updated on every autosave. So if any code path raises `OnGameStart` mid-session (e.g. someone wires `RestartGame()` later), the gate would block it. Currently `RestartGame()` has zero production callers (verified by grep), so this is a non-issue today. Worth flagging in case someone adds one.

---

## Implementation Plan

1. **Branch:** `git checkout -b fix/savegame-corruption-on-reload` from `main`.
2. **Edit each of the 15 Category A files** to gate `HandleGameStart` on `LastLoadedSaveDto == null`. The diff is one line per file. Example for `CurrencyManager.cs`:

   ```csharp
   private void HandleGameStart()
   {
       if (GameEvents.LastLoadedSaveDto != null) return; // returning player: hydrate owns state
       ResetBalance();
   }
   ```

3. **Edit `ReplayTutorialService.cs`** to clear `LastLoadedSaveDto` and `HasSaveBeenRestored` after `WipePlayerState`.

4. **Run editor tests** via Unity MCP:
   - `mcp__UnityMCP__refresh_unity` (scope=scripts, compile=request)
   - `mcp__UnityMCP__read_console` (filter "CS0" or "error") — must be clean
   - `mcp__UnityMCP__run_tests` mode=EditMode assembly_names=["FortuneValley.Tests.Editor"]
   - `mcp__UnityMCP__run_tests` mode=PlayMode assembly_names=["FortuneValley.Tests.Runtime"]
   - Expect the same baseline as v1.0.14: 1073 EditMode pass / 0 fail; the 13 pre-existing PlayMode failures (unrelated). New tests should still be green.

5. **Optional: add a regression test** that asserts:
   - Hydrate populates owner/tier/balance.
   - `OnGameStart` raised.
   - Owner/tier/balance unchanged after `OnGameStart` (i.e. gates worked).
   Place at `Assets/Tests/Runtime/SaveCorruptionOnReloadRegressionTests.cs`.

6. **Commit + PR + merge** following the same pattern as commit `5e5f2b5` / PR #3.

---

## Deploy Plan (after merge)

Identical to FV_v1.0.14 deploy (see this session's history):

1. Build WebGL in Unity (`File > Build Settings`).
2. Copy `data.unityweb` (only file that changes for a C#-only diff) from `fortune-valley-mvp-3/Build/<out>/Build/` into `alora-finance-main-website/public/simulation/Build/`. Delete the previous `*.data.unityweb` first.
3. Update `app/javascript/controllers/unity_bridge_controller.js:29` `dataUrl` default to the new hash filename.
4. Commit in `alora-finance-main-website` repo.
5. `fly deploy -c fly.dev.toml` (dev first).
6. Test with `student1@student.com` — see Verification below.
7. `fly deploy -c fly.prod.toml` once dev verified.

**Note on the deployed-build hash convention:** Unity Mono WebGL puts compiled C# in `data.unityweb`, NOT `wasm.unityweb`. So `wasm/framework/loader` hashes stay constant across pure C# changes; only `data` hash changes. Confirmed by the FV_v1.0.13 → FV_v1.0.14 diff.

---

## Verification on Dev

**CRITICAL:** Before testing, the `student1@student.com` server state is already corrupted by prior testing. You need a fresh seed. Two options:

**Option A (easier): pick a different student or create a new test student.** Most accurate test — verifies returning-player flow from scratch.

**Option B: manually reset `student1` via Rails console** (skip the replay-tutorial flow since it's also under test):
```
fly ssh console -a alora-finance-dev
bin/rails runner "GamePlayerState.where(student_id: Student.find_by(...).id).destroy_all"
```

Then in the browser:
1. Log in as the fresh student → land in tutorial → complete tutorial → land in Homebase.
2. Buy 2 non-starter lots, upgrade one to tier 3. Note checking balance, day, lot list.
3. Wait at least 10 ticks (≈10s of real time) for autosave.
4. Open DevTools, capture the most recent `POST /api/game/state` request body — confirm it shows the lots + balances you set.
5. Hard refresh (Cmd-Shift-R).
6. **Immediately after the refresh completes**, capture:
   - The `GET /api/game/state` response.
   - Wait ~5 seconds, then capture the next `POST /api/game/state` request body.
7. Compare GET response vs POST request body. They should be EQUAL on the typed fields (checking, lots_owned, rival_lots_owned, franchise_levels, current_day, credit_score, acquisition_costs).
8. Visually confirm: all owned lots show their tier mesh (no "For Sale" signs on owned blocks).
9. Sanity-check the slider — slider STILL won't show (separate Rails-side `selected_goals` bug; see "Out of Scope" below).

If the GET-vs-POST comparison passes, the fix is working. If POST is still resetting fields, find which subscriber missed a gate.

---

## Out of Scope (Defer to Later Handoffs)

1. **NetWorthProgressSlider never shows on reload.** Root cause confirmed: Rails has no `selected_goals` column. Unity sends it, Rails silently drops via strong params filter. Needs Rails migration + validator + strong params + model annotation. Independent fix; documented separately in this session's transcript. Do NOT bundle with this fix.

2. **`tutorial_completed: false` always on server even at day 86.** Confirmed in same `/api/game/state` response. Independent persistence bug; likely IntroTutorialController never flushes the flag OR strong params drop it (it IS in `state_params`, so likely a Unity-side write timing issue). Defer.

3. **Decision endpoint 422s.** Unity sends `session_id: ""` and `quiz_category: ""`. Rails validates both and rejects. Memory says session lifecycle was never wired. Decisions silently fail; events table stays empty. Defer.

4. **GameTimerDisplay flash to "Age 25" on reload.** Cosmetic; not data corruption. Optional polish — could gate it on `LastLoadedSaveDto == null` too.

---

## Files I Already Inspected (so you don't have to)

- `Assets/Scripts/Core/GameSaveBootstrapper.cs` — Phase 1 / Phase 2 dispatch flow
- `Assets/Scripts/Core/GameEvents.cs` (around line 1040 — `ClearAllSubscriptions`)
- `Assets/Scripts/Core/SaveRestoreCatchUp.cs` (recently added in commit `5e5f2b5`)
- `Assets/Scripts/Core/AutoSaveController.cs` (10-tick interval; `OnSaveRequested` debounce 0.5s; write-through cache to `LastLoadedSaveDto`)
- `Assets/Scripts/City/CityManager.cs` (`Hydrate`, `ResetOwnership`, `SeedStarterLots`, `RaiseAllOwnedLotEvents`)
- `Assets/Scripts/City/RestaurantVisualTierSwapper.cs`
- `Assets/Scripts/UI/HUD/LifeGoalsHud.cs`
- `Assets/Scripts/LifeGoals/LifeGoalSelectionService.cs` (`HydrateFromDto`)
- `Assets/Scripts/LifeGoals/NetWorthService.cs` (`HandleSnapshotRequest` honors `_dirty`)
- `Assets/Scripts/LifeGoals/GoalProgressTracker.cs` (early-returns on null selection)
- `Assets/Scripts/Managers/Tutorial/ReplayTutorialService.cs`
- `Assets/Scripts/Managers/GameManager.cs` (`Start()` → `StartGame()` → `OnGameStart`)
- `Assets/Scripts/Data/CityLotDefinition.cs`
- All 22 `HandleGameStart` bodies (full output captured in session transcript)

Live scene state was verified via Unity MCP `find_gameobjects` + `mcpforunity://scene/gameobject/{id}/component/{name}` — 38 `RestaurantVisualTierSwapper` instances (2 per block × 19 blocks); 1 `CityManager`; 1 `LifeGoalsHud` with `_progressSlider` correctly wired to `NetWorthProgressSlider`.

---

## Key Code References (file:line)

| What | Where |
|---|---|
| Phase 1 dispatch | `GameSaveBootstrapper.cs:153` (`RaiseSaveStateLoaded(dto)`) |
| Phase 2 dispatch | `GameSaveBootstrapper.cs:80-82` (in `Update()`) |
| `LastLoadedSaveDto` set on save load | `GameSaveBootstrapper.cs:152` |
| `LastLoadedSaveDto` set on autosave | `AutoSaveController.cs:109` |
| `CityManager.Hydrate` | `CityManager.cs:609` |
| `CityManager.RaiseAllOwnedLotEvents` | `CityManager.cs:681` |
| `OnGameStart` raised (auto-start) | `GameManager.cs:151` inside `StartGame()` |
| `OnGameStart` raised (restart, currently uncalled) | `GameManager.cs:171` inside `RestartGame()` |
| Strong params on Rails state controller | `alora-finance-main-website/app/controllers/api/game/states_controller.rb` `state_params` |

---

## Why I Did Not Implement This Already

User wanted to tackle one issue at a time and create a clean handoff for a fresh agent. The diagnosis is verified end-to-end against the live dev environment; the fix is mechanically simple but spans 16 files and benefits from a focused execution pass. No reason to spread it across a debugging-heavy conversation — write it cleanly here, execute it cleanly in the next session.
