namespace FortuneValley.UI.Panels.Credit
{
    /// <summary>
    /// Computes income estimates and DTI ratios for the credit explore panel.
    /// Extracted from CreditExploreSubPanel to keep arithmetic
    /// out of the MonoBehaviour.
    /// </summary>
    public static class CreditExploreIncomeEstimator
    {
        /// <summary>
        /// Rough monthly income estimate for DTI calculation.
        /// POC proxy using a fraction of checking balance.
        /// </summary>
        public static float EstimateMonthlyIncome(float checkingBalance, float fraction)
        {
            return checkingBalance > 0f ? checkingBalance * fraction : 1f;
        }

        /// <summary>
        /// Debt-to-income ratio. Returns 0 if income is zero or negative.
        /// </summary>
        public static float ComputeDtiRatio(float monthlyDebt, float monthlyIncome)
        {
            return monthlyIncome > 0f ? monthlyDebt / monthlyIncome : 0f;
        }
    }
}
