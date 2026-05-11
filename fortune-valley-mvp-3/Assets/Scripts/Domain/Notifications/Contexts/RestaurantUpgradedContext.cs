namespace FortuneValley.Domain.Notifications.Contexts
{
    /// <summary>
    /// Context for a "restaurant upgraded" banner. NewLevel is 1-based
    /// (tier 1 = dilapidated, tier 2 = finished, tier 3 = thriving) to match
    /// the existing RestaurantSystem contract.
    /// </summary>
    public readonly struct RestaurantUpgradedContext
    {
        public int NewLevel { get; }

        public RestaurantUpgradedContext(int newLevel)
        {
            NewLevel = newLevel;
        }
    }
}
