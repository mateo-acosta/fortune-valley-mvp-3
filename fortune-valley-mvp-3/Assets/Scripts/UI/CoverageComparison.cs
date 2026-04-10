namespace FortuneValley.UI
{
    /// <summary>
    /// Result of comparing actual accident cost against best available coverage.
    /// Used by the detail popup to show educational comparison text.
    /// </summary>
    public readonly struct CoverageComparison
    {
        public string PlayerPaid { get; }
        public string TotalDamage { get; }
        public bool WasCovered { get; }
        public bool HasComparison { get; }
        public string BestPolicyName { get; }
        public string BestDeductible { get; }
        public string WouldHavePaid { get; }
        public string ComparisonText { get; }

        public CoverageComparison(
            string playerPaid, string totalDamage,
            bool wasCovered, bool hasComparison,
            string bestPolicyName, string bestDeductible,
            string wouldHavePaid, string comparisonText)
        {
            PlayerPaid = playerPaid;
            TotalDamage = totalDamage;
            WasCovered = wasCovered;
            HasComparison = hasComparison;
            BestPolicyName = bestPolicyName;
            BestDeductible = bestDeductible;
            WouldHavePaid = wouldHavePaid;
            ComparisonText = comparisonText;
        }
    }
}
