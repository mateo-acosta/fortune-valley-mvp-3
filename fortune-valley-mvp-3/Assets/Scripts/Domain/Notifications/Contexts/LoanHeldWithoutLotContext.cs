namespace FortuneValley.Domain.Notifications.Contexts
{
    /// <summary>
    /// Context for the "you took a loan but haven't bought the lot yet"
    /// nudge. Fires once per pending loan when its age exceeds the
    /// configured tick threshold without a matching lot purchase.
    /// </summary>
    public readonly struct LoanHeldWithoutLotContext
    {
        public string LoanId { get; }
        public string LotId { get; }
        public float Principal { get; }
        public int TicksAged { get; }

        public LoanHeldWithoutLotContext(string loanId, string lotId, float principal, int ticksAged)
        {
            LoanId = loanId;
            LotId = lotId;
            Principal = principal;
            TicksAged = ticksAged;
        }
    }
}
