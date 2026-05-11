namespace FortuneValley.UI
{
    /// <summary>
    /// Computed display values for a loan product applied to a specific lot price.
    /// Produced by LoanDisplayCalculator, consumed by CreditExploreSubPanel.
    /// </summary>
    public readonly struct LoanDisplayValues
    {
        /// <summary>Loan amount after down payment (lotPrice - downPayment).</summary>
        public float Principal { get; }

        /// <summary>Down payment amount (lotPrice * downPaymentPercent).</summary>
        public float DownPayment { get; }

        /// <summary>Monthly amortized payment.</summary>
        public float MonthlyPayment { get; }

        // Stage 0a alias: per-cycle payment (= 1 in-game year per cycle).
        public float YearlyPayment => MonthlyPayment;

        /// <summary>Total cost over the life of the loan (payments + down payment).</summary>
        public float TotalCost { get; }

        /// <summary>APR expressed as a percentage (e.g., 8.0 for 8%).</summary>
        public float APRPercent { get; }

        /// <summary>Minimum credit score required by this loan product.</summary>
        public int MinCreditScore { get; }

        /// <summary>Loan term in in-game years.</summary>
        public int TermYears { get; }

        public LoanDisplayValues(
            float principal,
            float downPayment,
            float monthlyPayment,
            float totalCost,
            float aprPercent,
            int minCreditScore,
            int termYears)
        {
            Principal = principal;
            DownPayment = downPayment;
            MonthlyPayment = monthlyPayment;
            TotalCost = totalCost;
            APRPercent = aprPercent;
            MinCreditScore = minCreditScore;
            TermYears = termYears;
        }
    }
}
