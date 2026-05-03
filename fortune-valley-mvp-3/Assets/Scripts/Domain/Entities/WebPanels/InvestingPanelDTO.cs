using System;

namespace FortuneValley.Domain.Entities.WebPanels
{
    /// <summary>
    /// Wire payload from Unity to the HTML investing panel iframe.
    /// JsonUtility-serialized; fields stay flat (no Dictionary, no nullable
    /// structs). Field names match the iframe's mockState shape so the JS
    /// side can swap payload in for mock with no rename.
    /// </summary>
    [Serializable]
    public class InvestingPanelDTO
    {
        // Home tab
        public float checkingBalance;
        public float investingBalance;          // total portfolio market value (mirrors UGUI _balanceText)
        public float totalPortfolioValue;       // same value, separate field for the "Invested" stat (mirrors UGUI _investmentsValueText)
        public float lifetimeTotalGain;
        public string riskProfile;              // "Low" | "Medium" | "High" | "No Holdings"
        public float[] portfolioValueHistory;   // last 30 ticks

        // Portfolio tab
        public ActiveHoldingDTO[] holdings;

        // Trade + Explore tabs
        public AvailableInvestmentDTO[] available;
    }
}
