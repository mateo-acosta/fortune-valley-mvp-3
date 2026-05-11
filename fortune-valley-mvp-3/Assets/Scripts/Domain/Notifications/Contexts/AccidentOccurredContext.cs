namespace FortuneValley.Domain.Notifications.Contexts
{
    /// <summary>
    /// Context for an "accident occurred" banner. Separate from resolution
    /// because damage cost is known at roll time; payer breakdown is not.
    /// </summary>
    public readonly struct AccidentOccurredContext
    {
        public string LotId { get; }
        public string AccidentName { get; }
        public float DamageCost { get; }

        public AccidentOccurredContext(string lotId, string accidentName, float damageCost)
        {
            LotId = lotId;
            AccidentName = accidentName;
            DamageCost = damageCost;
        }
    }
}
