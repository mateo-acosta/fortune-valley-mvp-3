using System;

namespace FortuneValley.Domain.Entities.WebPanels
{
    /// <summary>
    /// One row in the iframe's "available investments" list (Trade and
    /// Explore tabs). The id field is the ScriptableObject asset name and
    /// is what the iframe sends back via parent.FV.requestIntent.
    /// </summary>
    [Serializable]
    public class AvailableInvestmentDTO
    {
        public string id;             // ScriptableObject asset name, e.g. "Stock_Tech_Low"
        public string name;           // Display name, e.g. "Tech Low"
        public float currentPrice;
        public float changePercent;   // % change from first price in priceHistory
        public string risk;           // "Low" | "Medium" | "High"
        public string category;       // "Stock" | "ETF" | "Bond" | "TBill"
        public string industry;       // "None" | "Technology" | "Financials" | etc.
        public float[] priceHistory;  // Last 30 ticks of prices
    }
}
