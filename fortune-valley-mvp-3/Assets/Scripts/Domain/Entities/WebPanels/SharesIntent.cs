using System;

namespace FortuneValley.Domain.Entities.WebPanels
{
    /// <summary>
    /// Wire payload from the HTML investing panel for buy/sell intents.
    /// JsonUtility-deserialized; both Buy and Sell share the same shape so
    /// a single DTO covers both paths.
    /// </summary>
    [Serializable]
    public class SharesIntent
    {
        public string symbol;   // ScriptableObject name, e.g. "Stock_Tech_Low"
        public int qty;
    }
}
