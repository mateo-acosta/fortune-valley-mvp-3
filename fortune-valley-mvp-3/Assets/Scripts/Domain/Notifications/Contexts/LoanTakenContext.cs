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
        public int TermMonths { get; }
        public float MonthlyPayment { get; }

        public LoanTakenContext(float principal, string lotId, int termMonths, float monthlyPayment)
        {
            Principal = principal;
            LotId = lotId;
            TermMonths = termMonths;
            MonthlyPayment = monthlyPayment;
        }
    }
}
