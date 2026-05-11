using System;

namespace FortuneValley.Domain.Entities
{
    /// <summary>
    /// Persisted slice of one per-building IncomeAccumulator. Null on legacy saves
    /// (schema_version == 0); DailyIncomeAccumulator.Hydrate runs a migration
    /// path that relocks every bucket in that case.
    /// </summary>
    [Serializable]
    public class PendingIncomeEntryDTO
    {
        public string building_id;
        public float daily_payout;
        public int ticks_remaining;
        public bool is_ready;
    }
}
