namespace FortuneValley.Domain.Notifications.Contexts
{
    /// <summary>
    /// Aggregated totals for a completed monthly payment cycle. Replaces
    /// the per-subsystem banner volume (loan + credit card + insurance +
    /// tax) that would otherwise fire within milliseconds of day-30 rollover
    /// and overflow the banner queue.
    /// </summary>
    public readonly struct MonthlyCycleSummaryContext
    {
        public int DayNumber { get; }
        public float LoanPayments { get; }
        public float CreditCardPayment { get; }
        public float InsurancePremiums { get; }
        public float Taxes { get; }
        public float TotalPaid => LoanPayments + CreditCardPayment + InsurancePremiums + Taxes;

        public MonthlyCycleSummaryContext(
            int dayNumber,
            float loanPayments,
            float creditCardPayment,
            float insurancePremiums,
            float taxes)
        {
            DayNumber = dayNumber;
            LoanPayments = loanPayments;
            CreditCardPayment = creditCardPayment;
            InsurancePremiums = insurancePremiums;
            Taxes = taxes;
        }
    }
}
