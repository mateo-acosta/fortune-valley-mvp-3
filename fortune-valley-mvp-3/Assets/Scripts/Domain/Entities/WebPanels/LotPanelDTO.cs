using System;

namespace FortuneValley.Domain.Entities.WebPanels
{
    /// <summary>
    /// Wire payload from Unity to the HTML lot detail panel iframe.
    /// JsonUtility-serialized; field names match the panel's mockState
    /// shape so the JS side can swap payload in for mock with no rename.
    ///
    /// All income figures are pre-converted to per-year values so the
    /// panel never has to know about engine pulses, ticks, or days.
    /// </summary>
    [Serializable]
    public class LotPanelDTO
    {
        // Identity
        public string lotId;
        public string displayName;
        public string description;

        // Ownership state. Strings keep the JS contract obvious.
        // Allowed values: "none", "rival", "player".
        public string owner;
        public int tier;            // 1..3 when owner="player"; 0 otherwise

        // Costs (absolute dollar amounts, no tier scaling)
        public float baseCost;
        public float resolvedCost;  // baseCost * rivalMultiplier when owner="rival", else baseCost
        public float rivalMultiplier;
        public float tier2Cost;     // upgrade T1 -> T2
        public float tier3Cost;     // upgrade T2 -> T3

        // Income (yearly, pre-computed using EnginePulsesPerTick * TicksPerYear)
        public float incomePerYear;                // baseline (T2 equivalent) per year
        public float incomeAtCurrentTierPerYear;   // current tier yearly, when owned
        public float incomeAtNextTierPerYear;      // next-tier yearly, when upgrade is available

        // Payback in whole years, rounded up. 0 means "under a year".
        // -1 sentinel means no income, payback not applicable.
        public int paybackYears;

        // Player wallet
        public float checkingBalance;

        // Flags
        public bool insuranceEnabled;
        public bool upgradePending;
        public bool isMaxTier;
    }
}
