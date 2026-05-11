namespace FortuneValley.Domain.Notifications.Contexts
{
    /// <summary>
    /// Context for the "rival targeting a lot" heads-up banner. Complements
    /// (does not replace) RivalPurchaseOverlay, which still fires on the
    /// actual purchase.
    /// </summary>
    public readonly struct RivalTargetingLotContext
    {
        public string LotId { get; }

        public RivalTargetingLotContext(string lotId)
        {
            LotId = lotId;
        }
    }
}
