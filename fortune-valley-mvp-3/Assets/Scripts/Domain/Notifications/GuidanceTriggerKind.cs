namespace FortuneValley.Domain.Notifications
{
    /// <summary>
    /// Discrete trigger identities for guidance tips. Each kind maps to a
    /// specific upstream game event; the dispatcher translates the event
    /// payload into a typed context (e.g. LoanTakenContext) that a
    /// message builder consumes.
    /// </summary>
    public enum GuidanceTriggerKind
    {
        LoanTaken = 0,
        LoanHeldWithoutLotPurchase,
        LotPurchased,
        RestaurantUpgraded,
        MonthlyCycleSummary,
        MonthlyPaymentMissed,
        CreditCardStatementReady,
        AccidentOccurred,
        AccidentResolved,
        InvestmentCompounded,
        LargePortfolioMovement,
        RivalTargetingLot,
        CreditScoreChanged
    }
}
