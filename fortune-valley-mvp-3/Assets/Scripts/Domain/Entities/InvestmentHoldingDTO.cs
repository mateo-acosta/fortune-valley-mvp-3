using System;

namespace FortuneValley.Domain.Entities
{
    /// <summary>
    /// Serializable DTO for a single investment holding,
    /// nested inside GamePlayerStateDTO.investment_holdings.
    /// </summary>
    [Serializable]
    public class InvestmentHoldingDTO
    {
        // User-facing label (e.g. "Tech Index Fund").
        public string name;

        // Stable identifier matching InvestmentDefinition.name. Used by
        // InvestmentSystem.Hydrate to look the SO back up on restore. Required
        // for round-trip; legacy saves without this field will fail to rehydrate
        // their holdings (Hydrate logs and skips per-holding).
        public string instrument_id;

        public int shares;
        public float avg_price;
        public float current_value;
    }
}
