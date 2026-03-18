# Fortune Valley - Unity Setup Guide

## Quick Start

### Step 1: Create ScriptableObject Assets

In Unity, right-click in `Assets/Data/` and create:

**Restaurant Config:**
- Right-click → Create → Fortune Valley → Restaurant Config
- Name it `RestaurantConfig`
- Default values are already sensible

**Rival Config:**
- Right-click → Create → Fortune Valley → Rival Config
- Name it `RivalConfig`

**Investments (create 3):**
- Right-click → Create → Fortune Valley → Investment Definition

Suggested investments:

| Asset Name | Display Name | Risk | Annual Rate | Volatility | Compound Freq |
|------------|--------------|------|-------------|------------|---------------|
| SavingsAccount | Savings Account | Low | 0.03 (3%) | (1, 1) | 30 |
| Bonds | Government Bonds | Medium | 0.06 (6%) | (0.8, 1.2) | 30 |
| Stocks | Stock Market | High | 0.12 (12%) | (0.5, 1.8) | 30 |

**City Lots (create 5):**
- Right-click → Create → Fortune Valley → City Lot

Suggested lots:

| Asset Name | Display Name | Base Cost | Income Bonus |
|------------|--------------|-----------|--------------|
| Lot_Corner | Corner Shop | 500 | 2 |
| Lot_Cafe | Cozy Café | 800 | 3 |
| Lot_Bakery | Bakery | 1200 | 5 |
| Lot_Diner | Downtown Diner | 2000 | 8 |
| Lot_Bistro | Fancy Bistro | 3500 | 12 |
| Lot_Hotel | City Hotel | 6000 | 20 |
| Lot_Tower | Business Tower | 10000 | 35 |

---

### Step 2: Create Game Manager GameObject

1. Create an empty GameObject named `GameManager`
2. Add these components:
   - `GameManager`
   - `TimeManager`
   - `CurrencyManager`
   - `RestaurantSystem`
   - `InvestmentSystem`
   - `CityManager`
   - `RivalAI`

3. Wire the references:
   - GameManager needs references to all other components
   - RestaurantSystem needs: RestaurantConfig, CurrencyManager
   - InvestmentSystem needs: CurrencyManager, TimeManager, list of InvestmentDefinitions
   - CityManager needs: list of CityLotDefinitions, CurrencyManager
   - RivalAI needs: RivalConfig, CityManager

---

### Step 3: Test

1. Press Play
2. Check Console for:
   - `[GameManager] Game started!`
   - `[TimeManager] Tick X` (if logging enabled)
   - Income generation messages

3. Use Debug inspector to verify:
   - CurrencyManager.Balance increases each tick
   - TimeManager.CurrentTick increments

---

## Architecture Overview

```
GameManager (coordinator)
    │
    ├── TimeManager ──────► OnTick event ──────┐
    │                                          │
    ├── CurrencyManager ◄──────────────────────┼── RestaurantSystem
    │         │                                │
    │         │                                ├── InvestmentSystem
    │         │                                │
    │         └── OnCurrencyChanged ──────────►│── RivalAI
    │                                          │
    └── CityManager ◄──────────────────────────┘
              │
              └── OnLotPurchased, OnGameEnd
```

---

## Key Tuning Parameters

| System | Parameter | Effect |
|--------|-----------|--------|
| TimeManager | Seconds Per Tick | Game speed (lower = faster) |
| RestaurantConfig | Base Income Per Tick | Safe income rate |
| RestaurantConfig | Upgrade Costs/Multipliers | Upgrade economy |
| RivalConfig | Purchase Interval | How often rival buys |
| RivalConfig | Income Per Tick | Rival's earning speed |
| InvestmentDefinition | Annual Return Rate | Investment attractiveness |
| InvestmentDefinition | Compounding Frequency | How often gains compound |
| CityLotDefinition | Base Cost | Lot affordability |
| CityLotDefinition | Income Bonus | Incentive to buy lots |

---

## Learning Checkpoints

After playtesting, students should be able to answer:

1. **Opportunity Cost**: "Why didn't I buy that lot immediately?"
2. **Compound Interest**: "Why did my investment grow faster over time?"
3. **Time Value of Money**: "Why was investing early better than waiting?"
4. **Risk vs Reward**: "Why did stocks sometimes lose money but savings didn't?"

If students can't answer these, adjust the tuning to make the signals clearer.
