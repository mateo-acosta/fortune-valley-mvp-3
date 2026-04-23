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
        public int current_day;
        public float checking_balance;
        public float credit_balance;
        public float investment_balance;
        public int credit_score;
        public int budget_variance_streak;
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
        public int consecutive_insolvent_months;
        public bool bankruptcy_flag;
        public int restaurant_level;
        public int current_tick;

        // Per-lot cosmetic neighbor picks (one entry per filled tier slot on each owned block).
        // Populated by GameStateDTOBuilder from CityManager once the block system is wired.
        public CosmeticVariantChoice[] cosmetic_variants;

        // Set true when the player has finished (or skipped) the onboarding tutorial.
        // Server-side default is false; missing field on response defaults to false on deserialize.
        public bool tutorial_completed;
    }
}
