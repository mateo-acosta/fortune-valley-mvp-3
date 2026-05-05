namespace FortuneValley.Core
{
    /// <summary>
    /// Per-building income accumulator for the automatic end-of-day deposit model.
    ///
    /// Each tick, <see cref="DailyPayout"/> grows by <see cref="CachedPerTickRate"/>.
    /// On the OnDayEnd tick, DailyIncomeAccumulator collects every accumulator
    /// (raises OnIncomeCollectRequested with CollectReason.DayEnd), the standard
    /// IncomeCollectionController pipeline deposits the running total to checking,
    /// and DailyPayout resets to 0 for the next day.
    ///
    /// CachedPerTickRate avoids recomputing ComputeDayRate every tick. RateDirty
    /// is flipped true whenever an event arrives that could change the rate
    /// (lot purchase / ownership loss, lot tier change, restaurant level upgrade,
    /// save state load); the next HandleTick recomputes and clears the flag.
    ///
    /// IsReady and TicksRemaining are vestigial. IsReady is still toggled true
    /// in HandleDayEnd so IncomeCollectionController.HandleCollectRequested
    /// passes its existing guard. TicksRemaining is preserved on the DTO for
    /// save-format backward compatibility but no longer participates in logic.
    /// </summary>
    public class IncomeAccumulator
    {
        public float DailyPayout { get; set; }
        public float CachedPerTickRate { get; set; }
        public bool RateDirty { get; set; } = true;
        public bool IsReady { get; set; }
        public int TicksRemaining { get; set; }
    }
}
