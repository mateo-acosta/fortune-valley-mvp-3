namespace FortuneValley.Domain.Notifications
{
    /// <summary>
    /// Optional click-target for a banner. None = display-only; any other value
    /// triggers a navigation request when the banner is clicked.
    /// </summary>
    public enum GuidanceTargetIntent
    {
        None = 0,
        LoanPanel,
        LotsPanel,
        InvestingPanel,
        InsurancePanel,
        CreditCardPanel
    }
}
