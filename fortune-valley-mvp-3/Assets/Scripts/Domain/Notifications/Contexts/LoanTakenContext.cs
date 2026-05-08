namespace FortuneValley.Domain.Notifications.Contexts
{
    /// <summary>
    /// Typed fields needed to render a "loan originated" banner. The
    /// corresponding builder consumes these positionally so templates
    /// authored in a GuidanceTipSO never reference untyped field names.
    /// </summary>
    public readonly struct LoanTakenContext
    {
        public float Principal { get; }
        public string LotId { get; }
        public int TermYears { get; }
        public float MonthlyPayment { get; }

        // Stage 0a alias: per-cycle payment (= 1 in-game year per cycle).
        public float YearlyPayment => MonthlyPayment;

        public LoanTakenContext(float principal, string lotId, int termYears, float monthlyPayment)
        {
            Principal = principal;
            LotId = lotId;
            TermYears = termYears;
            MonthlyPayment = monthlyPayment;
        }
    }
}
