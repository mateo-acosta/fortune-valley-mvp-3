namespace FortuneValley.Domain.Notifications.Contexts
{
    /// <summary>
    /// Context for a "lot purchased" banner. Only player purchases surface
    /// as banners; rival purchases flow through the existing
    /// RivalPurchaseOverlay.
    /// </summary>
    public readonly struct LotPurchasedContext
    {
        public string LotId { get; }

        public LotPurchasedContext(string lotId)
        {
            LotId = lotId;
        }
    }
}
