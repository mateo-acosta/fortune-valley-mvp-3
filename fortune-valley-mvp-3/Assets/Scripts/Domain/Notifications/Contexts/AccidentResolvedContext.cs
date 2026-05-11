namespace FortuneValley.Domain.Notifications.Contexts
{
    /// <summary>
    /// Context for an "accident resolved" banner. Carries both what the
    /// incident cost in total and what the player actually paid after
    /// insurance coverage, so copy can teach the value of the policy.
    /// </summary>
    public readonly struct AccidentResolvedContext
    {
        public string LotId { get; }
        public string AccidentName { get; }
        public float TotalDamageCost { get; }
        public bool WasCovered { get; }
        public float PlayerCost { get; }

        public AccidentResolvedContext(
            string lotId, string accidentName,
            float totalDamageCost, bool wasCovered, float playerCost)
        {
            LotId = lotId;
            AccidentName = accidentName;
            TotalDamageCost = totalDamageCost;
            WasCovered = wasCovered;
            PlayerCost = playerCost;
        }
    }
}
