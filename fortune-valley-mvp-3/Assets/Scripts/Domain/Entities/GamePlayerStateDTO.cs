using System;

namespace FortuneValley.Domain.Entities
{
    /// <summary>
    /// Serializable DTO matching the Rails game_player_states schema.
    /// Used for autosave payloads sent to/from the server.
    /// </summary>
    [Serializable]
    public class GamePlayerStateDTO
    {
        // Typed columns
        public string game_mode;
        // Legacy "day" naming (Stage 0a alias chain). Removed in Stage 0c.
        public int current_day;
        public float checking_balance;
        public float credit_balance;
        public float investment_balance;
        public int credit_score;
        public int budget_variance_streak;
        // Longest correct-answer quiz streak reached this life. Persisted so the
        // Player Profile "Best quiz streak" survives reloads. Requires the
        // matching Rails column + strong-params permit to actually persist.
        public int best_quiz_streak;
        public float tax_liability_ytd;
        public float monthly_income;

        // JSONB columns (serialized as arrays/objects)
        public string[] lots_owned;
        public string[] rival_lots_owned;
        public string[] learning_levels_completed;
        public InvestmentHoldingDTO[] investment_holdings;

        // Financial system state
        public ActiveLoanDTO[] active_loans;
        public ActiveInsurancePolicyDTO[] insurance_policies;
        public FranchiseLevelDTO[] franchise_levels;
        public int consecutive_insolvent_months;
        public bool bankruptcy_flag;
        public int restaurant_level;
        public int current_tick;

        // Per-lot cosmetic neighbor picks (one entry per filled tier slot on each owned block).
        // Populated by GameStateDTOBuilder from CityManager once the block system is wired.
        public CosmeticVariantChoice[] cosmetic_variants;

        // Per-player-owned-lot acquisition cost (actual paid amount, includes rival
        // buyout markup). Hydrated into CityManager._acquisitionCost so
        // BusinessAssetValue contribution to Total Net Worth is correct on returning
        // players. Null on legacy saves -- Hydrate treats null as empty and the
        // total falls back to 0 for those lots (pre-existing behavior preserved).
        public AcquisitionCostEntry[] acquisition_costs;

        // Per-building pending income buckets (restaurant + each player-owned lot).
        // Written by DailyIncomeAccumulator.Snapshot; consumed by Hydrate on load.
        // Null on legacy saves; Hydrate treats null as empty.
        public PendingIncomeEntryDTO[] pending_incomes;

        // Persistence schema version. 0 = legacy tick-accumulation model
        // (pre-daily-locked-coin-rebuild). Hydrate paths branch on this.
        // New saves write 1+.
        public int schema_version;

        // Set true when the player has finished (or skipped) the onboarding tutorial.
        // Server-side default is false; missing field on response defaults to false on deserialize.
        public bool tutorial_completed;

        // Life Goals: the player's three locked-in picks (one per tier).
        // Empty / null on legacy saves -- triggers fresh-tutorial flow on load.
        public LifeGoalEntry[] selected_goals;

        // Player age in years (25 at game start, 65 at retirement).
        // Derived from current_day; persisted so the HTML status panel can
        // render it without recomputing.
        public int current_age;

        // Total Net Worth: liquid + business asset values (lot acquisitionCost
        // + paid tier upgrade costs). Conservative formula. Persisted by
        // GameStateDTOBuilder for the HTML status panel.
        public float total_net_worth;

        // Liquid Net Worth: checking + investing - credit card debt - outstanding loan principal.
        public float liquid_net_worth;

        // Stage 0a additions: new "tick" naming written in parallel with the
        // legacy current_day / current_tick fields. Hydration in Stage 0b
        // will prefer these and fall back to the legacy fields on older saves.
        //   current_tick_count  : gameplay heartbeat counter (= legacy current_day)
        //   current_engine_pulse: atomic 0.4s pulse counter (= legacy current_tick)
        // Yearly_income mirrors monthly_income; both fields written for one
        // commit window so 0b can swap readers safely.
        public int current_tick_count;
        public int current_engine_pulse;
        public float yearly_income;
    }
}
