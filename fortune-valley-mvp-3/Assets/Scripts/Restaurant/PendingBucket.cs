namespace FortuneValley.Core
{
    /// <summary>
    /// Per-building coin state for the tap-to-collect loop.
    ///
    /// Daily-locked model: on day-start, <see cref="DailyPayout"/> is snapshotted
    /// from the building's current per-tick rate * ticksPerDay and does not
    /// change during the day. <see cref="TicksRemaining"/> counts down each
    /// tick; at zero, <see cref="IsReady"/> flips to true and the player can
    /// collect. Each bucket's countdown resets on collect, so per-bucket day
    /// cycles intentionally drift from TimeManager.CurrentDay.
    /// </summary>
    public struct PendingBucket
    {
        public float DailyPayout { get; set; }
        public int TicksRemaining { get; set; }
        public bool IsReady { get; set; }
    }
}
