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
        public string name;
        public int shares;
        public float avg_price;
        public float current_value;
    }
}
