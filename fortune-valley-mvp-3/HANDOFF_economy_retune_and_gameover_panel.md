# HANDOFF: Economy Retune + Game-Over Panel Fix

Status: DESIGN LOCKED on most points, 4 questions still open with the user. No code or asset changes made yet. This document is the single source of truth to continue the work.

Branch at time of writing: `fix/profile-remove-realize-ticks` (a dedicated branch should be cut for this work).

---

## 1. The goal (why we are doing this)

Fortune Valley is a Unity educational POC. Hypothesis: a game that requires financial knowledge to progress causes students to learn. Core learning outcomes: opportunity cost, compound interest, time value of money, risk vs reward.

Two separate problems were raised by the user (Mateo):

### Problem A: Game-over panel never shows results
At retirement (age 65) the end-game summary screen does not display its content. User guessed "panel inactive at export." That guess is directionally right but the true cause is deeper (see Section 4).

### Problem B: The economy hands out far too much money
The user retired with $12M net worth after ~5 properties and hit the $2M "ambitious" goal by age 40. Income is so high that restaurant/property cash flow alone trivially beats every goal, so investing (a core learning outcome) is pointless. Also, every dollar collected is deposited as pure profit with no notion of revenue or costs, which teaches the wrong mental model. The user wants a realistic small-business-owner feel: modest steady cash flow from the restaurant, real wealth coming from investing the surplus.

These two are unrelated and ship as TWO separate commits.

---

## 2. Locked design decisions (confirmed by the user)

Economy targets:
- A focused, smart player who invests well and uses loans wisely should retire (~age 65) with about **$2.5M**.
- A careless player should retire near **$100k**.
- Mid goal ($500k) reached around age 35-45. Ambitious goal ($2M) reached around age 60-65 and ONLY achievable by investing the surplus, not restaurant/lot cash alone.
- Goal thresholds stay **$100k / $500k / $2M** (do NOT change them).
- Restaurant = modest steady cash flow. Wealth = investing the surplus. This is the pedagogical north star.

Revenue/Cost framing (cosmetic only):
- Add 3 lines to the **Restaurant upgrade panel only** (nowhere else):
  - `Revenue: $X / year`
  - `Operating costs: -$Y / year`
  - `Net profit (to your bank): $Z / year`
- The collected/deposited number does NOT change and IS the net profit (Z). Revenue and Costs are made-up display numbers, no game system computes them, nothing else reads them.
- Net margin improves with level: **L1 = 10%, L2 = 13%, L3 = 15%**. Revenue = Net / margin. Costs = Revenue - Net.
- Collection mechanism is completely unchanged.

Rival:
- Lower rival income too, but cut it LESS than the player so it stays a credible competitor for scarce lots. Specifically `_incomePerTick` 25 -> **8** (player engine is cut harder). The 12-of-19 lot soft cap stays.

Scope guardrails (do NOT change):
- Life length (40 years, age 25 to 65, 30 days/year, 10 ticks per day).
- The starter lot mechanic.
- The goal default thresholds ($100k / $500k / $2M).
- Investment return rates (already realistic 3.5% to 30%). Untouched.
- Loan APRs/terms are touched ONLY as needed for accessibility (see Section 5), nothing else financial.

Saves:
- This is a save-breaking change. Any in-flight or legacy save loads as a fresh game (the wipe path already exists for empty `selected_goals`). Confirmed OK to force-wipe.

Stale duplicate config assets:
- Do NOT delete the duplicates now (user wants a dedicated cleanup session later). A memory note exists: `stale_duplicate_configs.md`.

---

## 3. Verified LIVE wiring (use these exact paths, ignore duplicates)

Verified via Unity MCP on the `HomebaseSceneManager` GameObject in `Assets/Scenes/Homebase.unity`:

- Restaurant config (LIVE): `Assets/Data/RestaurantConfig.asset`
  - Current: `_baseIncomePerTick: 84`, `_incomeMultipliers: [1, 2.5, 5]`, `_upgradeCosts: [5000, 15000]`, `_maxLevel: 3`
- Rival config (LIVE): `Assets/Data/RivalConfig.asset`
  - Current: `_startingMoney: 3000`, `_incomePerTick: 25`, `_purchaseInterval: 400`
- Lot configs (LIVE): `Assets/Scripts/Data/Lot_Block01..19.asset` (NOT the `Assets/Data/` path)
  - Player starter lot = `lot_block07` ($50,000). Rival starter = Block 19. New purchases spawn Tier 1, starter starts Tier 2.
- Start cash: `CurrencyManager._startingCheckingBalance = 10000` (scene value, GameObject `HomebaseSceneManager`).
- Investments (LIVE): `Assets/Data/Stock_*`, `ETF_*`, `Bond_*`, `TBill_*`, `NewInvestment_*`. Leave untouched.

STALE duplicates the scene does NOT use (do not edit, do not delete yet):
- `Assets/Scripts/Data/RestaurantConfig.asset`
- `Assets/Scripts/Data/RivalConfig.asset`

Time math: yearly income = (per-pulse hidden value) x 10 pulses/day x 30 days/year = **x300**.

Current lot values (`Assets/Scripts/Data/Lot_Block*.asset`), `_baseCost` / `_incomeBonus`:
- Block01 400000 / 292, Block02 120000 / 100, Block03 35000 / 25, Block04 25000 / 17,
  Block05 80000 / 67, Block06 200000 / 167, Block07 50000 / 42 (starter),
  Block08-19 60000 / 42 (each also `_tier2UpgradeCost: 5000`, `_tier3UpgradeCost: 15000`).
- Tier multipliers live in `Assets/Scripts/Data/CityLotDefinition.cs`: T1 x0.5, T2 x1.0, T3 x2.0. `_incomeBonus` is the T2 baseline.

---

## 4. Problem A details: game-over panel root cause + fix

Root cause (verified in code and live scene): an event-ordering race, NOT a build artifact. It fails 100% of the time including in the editor.

- `GameEndPanel` (`Assets/Scripts/UI/Panels/GameEndPanel.cs`) subscribes to `OnGameEndWithSummary` / `OnGameEnd` in `OnEnable()`, which only runs when its GameObject is activated.
- The GameObject (`Canvas/PopUpPanels/GameEndPanel`) is INACTIVE in the Homebase scene.
- The only thing that activates it is `GameFlowController.HandleGameEnd()` (`Assets/Scripts/Managers/GameFlowController.cs:151`), which is itself a subscriber to `OnGameEndWithSummary`.
- C# multicast delegates snapshot their invocation list at invoke time, so the panel (subscribing mid-dispatch) never receives that dispatch. `OnGameEnd` already fired even earlier. Result: `HandleGameEnd` / `Show()` / `DisplaySummary()` never run on the panel.
- Secondary: on the live `GameEndPanel` component, `_panelRoot` is null and several serialized text refs are null (`_outcomeBackground`, `_outcomeIcon`, `_decisionsContainer`, `_decisionItemPrefab`, `_headlineText`, `_investmentInsightText`, `_opportunityCostText`, `_whatIfText`).

Recommended fix (separate small commit, do this independently of the economy work):
- Keep the GameEndPanel GameObject active in the scene and gate visibility via `Show()` / `_panelRoot` like other panels, so it subscribes at scene load and receives the event directly. Then `GameFlowController` no longer needs to activate it.
- Re-wire the null serialized fields.
- `UIPanel.Show()` with null `_panelRoot` and no CanvasGroup falls back to `gameObject.SetActive(true)`; the chosen approach must account for this. Confirm with the user whether `_panelRoot` should be wired or the CanvasGroup pattern used.

---

## 5. Problem B details: the method and the proposed numbers

Method: **yield targeting**. Do not pick numbers arbitrarily. Pick a realistic annual return and payback period, then back-solve the hidden per-pulse value.
- Example: a $60,000 lot at a target 8% yield should earn $4,800/year. Divide by 300 to get the hidden `_incomeBonus` = 16 (currently 42).
- Restaurant has one master dial `_baseIncomePerTick`; levels are that x1 / x2.5 / x5, so changing the base scales all levels proportionally.
- Upgrade costs are set to roughly 4 to 5 years of the extra income they unlock (currently upgrades pay back in ~6 weeks, which is the core bug).

Proposed economy (subject to the open questions in Section 6, especially lot prices):

Restaurant (`Assets/Data/RestaurantConfig.asset`):
- `_baseIncomePerTick`: 84 -> **12** (L1 $3,600/yr, L2 $9,000/yr, L3 $18,000/yr)
- `_incomeMultipliers`: keep `[1, 2.5, 5]`
- `_upgradeCosts`: `[5000, 15000]` -> **[20000, 40000]** (payback ~3.5 to 4.5 yrs)
- New cosmetic field needed: per-level net margin array, e.g. `_netMarginByLevel = [0.10, 0.13, 0.15]`, plus a getter. `RestaurantConfig.cs` is Data layer (safe to add a serialized field + property).

Lots (`Assets/Scripts/Data/Lot_Block01..19.asset`): set `_incomeBonus = round(baseCost * 0.08 / 300)` for a uniform ~8% T2 yield (user chose Option A: same rate for every lot). Raise `_tier2UpgradeCost` / `_tier3UpgradeCost` so tier upgrades pay back in ~4 to 6 years. Lots that do not set explicit upgrade costs fall back to `CityLotDefinition.cs` defaults; verify those defaults and scale per lot during implementation.
- LOT PRICES: see open question 1. Likely must come DOWN in proportion to income for affordability (see below).

Rival (`Assets/Data/RivalConfig.asset`):
- `_incomePerTick`: 25 -> **8**
- `_startingMoney`: 3000 -> ~**1500** (tune by feel)

UI (cosmetic, restaurant upgrade panel only): `Assets/Scripts/UI/Panels/RestaurantUpgradePanel.cs`. Currently shows `Income: $X/year` at `Refresh()` (line ~137-141) using `_restaurantSystem.IncomePerTick * ticksPerDay * LifespanConstants.TicksPerYear`. Add 3 TextMeshProUGUI fields (Revenue / Operating costs / Net profit) and compute from the existing income figure and the new margin. Adding `[SerializeField]` on a MonoBehaviour requires Unity MCP inspector verification per CLAUDE.md; new text objects under the panel must be created and wired via Unity MCP.

### The loan accessibility problem (critical, drove the open questions)

Loan gate (`Assets/Scripts/UI/LoanEligibilityFilter.cs`): each loan needs credit score >= min, DTI <= max, and loan size >= required principal. Starting credit score = 650 (`Assets/Data/CreditScoringConfig.asset`).

| Loan | APR | Term | Down | Min score | Max size |
|---|---|---|---|---|---|
| Starter | 15% | 5 yr | 25% | 450 | $25,000 |
| Standard | 8% | 10 yr | 15% | 600 | $100,000 |
| Premium | 4% | 15 yr | 10% | 720 | $500,000 |

The problem: lots cost the same but income is ~7x lower. Financing a $60k lot on the Standard loan costs ~$7,600/year while the lot earns only ~$4,800/year (negative cash flow), and the debt-to-income limit (0.5) blocks a second loan once income is this low. So lowering income while keeping lot prices makes the game nearly unplayable. The credit-score climb (650 -> 720 unlocks cheap Premium) is good learning and should be KEPT; only the DTI/affordability side needs loosening, plus the lot-price correction.

---

## 6. OPEN QUESTIONS (must be answered by the user before implementing the economy)

1. Confirm the correction: bring lot prices DOWN in proportion to income (keeping ~8% yield) so a lot costs "a few years of income," not 30. (This refines the earlier "keep lot prices" answer.)
2. How should financing a lot feel: (a) lot earns slightly MORE than its loan payment (smart leverage, encouraging) or (b) slightly LESS for the first years then profitable (realistic carrying cost, teaches debt risk)?
3. Loan accessibility target: confirm "first lot financeable from the start with starting credit and no debt; more loans as the first is paid down" (achieved by raising the DTI cap and slightly lengthening terms).
4. Keep the credit-score progression (start 650, pay on time to climb toward 720 and unlock the cheap Premium loan), loosening only the DTI side?

The game-over panel fix (Section 4) is approved as a separate small commit and can proceed independently while economy questions are pending. Confirm with the user whether to wire `_panelRoot` or use the CanvasGroup pattern.

---

## 7. Implementation sequencing

1. (Independent, can start now) Game-over panel fix as its own commit on its own branch. Verify in the live scene via Unity MCP that the panel shows a populated summary at retirement.
2. (After open questions answered) Economy retune as one commit:
   - Edit `Assets/Data/RestaurantConfig.asset` (LIVE), all 19 `Assets/Scripts/Data/Lot_Block*.asset` (LIVE), `Assets/Data/RivalConfig.asset` (LIVE).
   - Adjust loan configs (`Assets/Data/LoanConfig_1..3.asset`) per answers to Q2-Q4.
   - Add the margin field to `RestaurantConfig.cs` and the 3 cosmetic lines + wiring to `RestaurantUpgradePanel.cs` (Unity MCP for scene wiring).
   - Do NOT edit the stale `Assets/Scripts/Data/RestaurantConfig.asset` or `Assets/Scripts/Data/RivalConfig.asset`.
3. Playtest-tune by feel; the numbers above are a calculated first cut, not final.

---

## 8. Hard constraints (from project CLAUDE.md)

- Strict layer architecture. Cross-system communication via `GameEvents.Raise*()`. Domain layer has zero `UnityEngine` usings. One type per file. No public fields (use `[SerializeField] private`). No `FindObjectOfType`. No singletons.
- Mandatory pre-write and post-write declaration/checklist blocks before/after editing any `.cs` file (see CLAUDE.md section 4.1).
- Adding/modifying a `[SerializeField]` on a MonoBehaviour requires a Unity MCP inspector verification call confirming the field is wired in the scene.
- Do not modify `.unity` / prefab / ProjectSettings files unless explicitly instructed. Scene wiring is done via Unity MCP.
- No em dashes anywhere (copy, comments, docs, UI text). Student-friendly financial language.
- Unity MCP: one instance, `fortune-valley-mvp-3`. Active scene Homebase. Check `mcpforunity://instances` and read the custom-tools resource first.

## 9. Relevant memory files

`life_goals_design.md`, `financial_systems_design.md`, `stale_duplicate_configs.md`, `feedback_planning_style.md` (Mateo prefers discussion before formal plans), `feedback_ui_redesign_workflow.md` (multi-round clarifying questions, no emoji/em dashes), `homebase_panel_map.md`, `ui_architecture.md`.
