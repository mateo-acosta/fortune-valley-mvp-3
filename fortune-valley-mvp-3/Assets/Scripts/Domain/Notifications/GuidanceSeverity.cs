namespace FortuneValley.Domain.Notifications
{
    /// <summary>
    /// Severity ordering drives both visual styling (via BannerSeverityPalette) and
    /// queue eviction priority: when the queue is full, the lowest-severity oldest
    /// banner is evicted to make room for a higher-severity incoming banner.
    /// Numeric ordering matters; do not reorder values.
    /// </summary>
    public enum GuidanceSeverity
    {
        Info = 0,
        Positive = 1,
        Warning = 2,
        Alert = 3,
        Critical = 4
    }
}
