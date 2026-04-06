namespace FortuneValley.Core
{
    /// <summary>
    /// Pure C# helper for debt-to-income ratio calculation.
    /// Extracted from MonthlyPaymentDayController to keep
    /// arithmetic out of MonoBehaviour methods.
    ///
    /// LEARNING DESIGN: DTI is a real-world metric lenders use.
    /// Students see their DTI on each billing cycle, learning that
    /// high debt relative to income limits borrowing options.
    /// </summary>
    public static class DtiCalculator
    {
        /// <summary>
        /// Calculate debt-to-income ratio.
        /// Returns 0 if monthlyIncome is zero to avoid division by zero.
        /// </summary>
        public static float Compute(float totalMonthlyDebt, float monthlyIncome)
        {
            if (monthlyIncome <= 0f) return 0f;
            return totalMonthlyDebt / monthlyIncome;
        }

        /// <summary>
        /// Compute monthly income from per-tick income, ticks per day, and billing cycle length.
        /// </summary>
        public static float ComputeMonthlyIncome(float incomePerTick, int ticksPerDay, int billingCycleDays)
        {
            return incomePerTick * ticksPerDay * billingCycleDays;
        }

        /// <summary>
        /// Compute total monthly debt (loan payments + CC minimum payment).
        /// </summary>
        public static float ComputeTotalMonthlyDebt(float loanMonthlyPayments, float ccMinimumPayment)
        {
            return loanMonthlyPayments + ccMinimumPayment;
        }
    }
}
