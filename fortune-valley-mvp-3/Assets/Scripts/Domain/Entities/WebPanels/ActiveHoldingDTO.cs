using System;

namespace FortuneValley.Domain.Entities.WebPanels
{
    /// <summary>
    /// One row in the iframe's "current holdings" list (Portfolio tab and
    /// Trade-tab "shares owned" stat). Mirrors ActiveInvestment runtime
    /// fields. id is the underlying ScriptableObject asset name so the
    /// iframe can match it against the available list.
    /// </summary>
    [Serializable]
    public class ActiveHoldingDTO
    {
        public string id;             // ScriptableObject asset name of the underlying definition
        public string name;
        public int shares;
        public float currentValue;
        public float totalGain;
        public float avgCost;
        public float currentPrice;
        public string category;
        public string industry;
        public string risk;
        public float[] priceHistory;  // Last 30 ticks of the underlying definition's price
    }
}
