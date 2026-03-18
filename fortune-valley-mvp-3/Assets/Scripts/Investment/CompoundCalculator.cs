using UnityEngine;

namespace FortuneValley.Core
{
    /// <summary>
    /// Pure math functions for compound interest calculations.
    /// Separated for testability and educational clarity.
    ///
    /// LEARNING NOTE: Students can examine these formulas to understand
    /// exactly how compound interest works mathematically.
    /// </summary>
    public static class CompoundCalculator
    {
        /// <summary>
        /// Calculate future value with compound interest.
        ///
        /// Formula: FV = P × (1 + r/n)^(n×t)
        ///
        /// Where:
        /// - P = Principal (initial investment)
        /// - r = Annual interest rate (as decimal, e.g., 0.05 for 5%)
        /// - n = Number of times interest compounds per year
        /// - t = Time in years
        /// </summary>
        /// <param name="principal">Initial investment amount</param>
        /// <param name="annualRate">Annual interest rate as decimal</param>
        /// <param name="compoundsPerYear">How many times per year interest compounds</param>
        /// <param name="years">Number of years</param>
        /// <returns>Future value of the investment</returns>
        public static float FutureValue(float principal, float annualRate, int compoundsPerYear, float years)
        {
            if (principal <= 0 || compoundsPerYear <= 0)
                return principal;

            float ratePerPeriod = annualRate / compoundsPerYear;
            float totalPeriods = compoundsPerYear * years;

            return principal * Mathf.Pow(1f + ratePerPeriod, totalPeriods);
        }

        /// <summary>
        /// Calculate how long until an investment doubles.
        ///
        /// Rule of 72: Years to double ≈ 72 / (rate × 100)
        ///
        /// This is an approximation that's close enough for learning purposes.
        /// </summary>
        /// <param name="annualRate">Annual interest rate as decimal</param>
        /// <returns>Approximate years to double</returns>
        public static float YearsToDouble(float annualRate)
        {
            if (annualRate <= 0)
                return float.PositiveInfinity;

            return 72f / (annualRate * 100f);
        }

        /// <summary>
        /// Calculate the total interest earned over a period.
        /// </summary>
        public static float TotalInterestEarned(float principal, float annualRate, int compoundsPerYear, float years)
        {
            float futureValue = FutureValue(principal, annualRate, compoundsPerYear, years);
            return futureValue - principal;
        }

        /// <summary>
        /// Compare simple interest vs compound interest.
        /// Returns the extra money earned from compounding.
        ///
        /// LEARNING NOTE: This shows WHY compound interest is powerful.
        /// Simple interest: I = P × r × t
        /// Compound interest gives you more because you earn interest on interest.
        /// </summary>
        public static float CompoundingAdvantage(float principal, float annualRate, int compoundsPerYear, float years)
        {
            // Simple interest
            float simpleInterest = principal * annualRate * years;
            float simpleTotal = principal + simpleInterest;

            // Compound interest
            float compoundTotal = FutureValue(principal, annualRate, compoundsPerYear, years);

            // The advantage of compounding
            return compoundTotal - simpleTotal;
        }

        /// <summary>
        /// Generate a plain-language explanation of compound interest.
        /// </summary>
        public static string ExplainCompoundInterest(float principal, float annualRate, int compoundsPerYear, int years)
        {
            float futureValue = FutureValue(principal, annualRate, compoundsPerYear, years);
            float totalGain = futureValue - principal;
            float compoundAdvantage = CompoundingAdvantage(principal, annualRate, compoundsPerYear, years);
            float yearsToDouble = YearsToDouble(annualRate);

            return $"Starting with ${principal:F0} at {annualRate * 100:F1}% annual interest:\n\n" +
                   $"After {years} year(s), you'll have: ${futureValue:F2}\n" +
                   $"Total earned: ${totalGain:F2}\n\n" +
                   $"Compounding {compoundsPerYear}x per year earns you ${compoundAdvantage:F2} MORE\n" +
                   $"than simple interest would!\n\n" +
                   $"At this rate, your money doubles in ~{yearsToDouble:F1} years.";
        }

        /// <summary>
        /// Convert game ticks to years for calculation purposes.
        /// </summary>
        /// <param name="ticks">Number of game ticks</param>
        /// <param name="ticksPerYear">How many ticks represent one year (default: 365 for daily ticks)</param>
        public static float TicksToYears(int ticks, int ticksPerYear = 365)
        {
            return (float)ticks / ticksPerYear;
        }
    }
}
