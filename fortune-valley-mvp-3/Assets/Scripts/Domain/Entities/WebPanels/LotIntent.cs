using System;

namespace FortuneValley.Domain.Entities.WebPanels
{
    /// <summary>
    /// Wire payload from the HTML lot detail panel for any lot-scoped
    /// action (purchase, explore loan, upgrade, insure). JsonUtility
    /// deserialized by LotWebBridge.Request* handlers.
    /// </summary>
    [Serializable]
    public class LotIntent
    {
        public string lotId;    // CityLotDefinition.LotId
        public int loanId;      // Reserved for future loan-funded buy. Currently 0 = cash.
    }
}
