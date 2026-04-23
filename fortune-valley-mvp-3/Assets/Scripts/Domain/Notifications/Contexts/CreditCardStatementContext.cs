namespace FortuneValley.Domain.Notifications.Contexts
{
    /// <summary>
    /// Context for a "credit card statement ready" banner. Matches the
    /// OnCreditCardStatementReady event payload (statementBalance,
    /// minimumPayment, interestCharged).
    /// </summary>
    public readonly struct CreditCardStatementContext
    {
        public float StatementBalance { get; }
        public float MinimumPayment { get; }
        public float InterestCharged { get; }

        public CreditCardStatementContext(float statementBalance, float minimumPayment, float interestCharged)
        {
            StatementBalance = statementBalance;
            MinimumPayment = minimumPayment;
            InterestCharged = interestCharged;
        }
    }
}
