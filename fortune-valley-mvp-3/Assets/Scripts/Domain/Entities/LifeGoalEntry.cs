using System;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Domain.Entities
{
    /// <summary>
    /// One of the player's three picked life goals. DTO-style: public snake_case
    /// fields so Unity's JsonUtility can round-trip it through GamePlayerStateDTO.
    /// The threshold is cached at selection time so Domain-layer comparisons do
    /// not need to look up the LifeGoalSO (which lives in Core).
    /// </summary>
    [Serializable]
    public class LifeGoalEntry
    {
        public string goal_id;
        public LifeGoalTier tier;
        public float threshold;
        public bool realized;

        // Legacy "day" naming (Stage 0a alias chain). Removed in Stage 0c.
        public int realized_at_day;

        // Stage 0a addition: new "tick" naming written in parallel with the
        // legacy realized_at_day field. MarkRealized writes both. Hydration
        // in Stage 0b will prefer realized_at_tick and fall back to legacy.
        public int realized_at_tick;

        public LifeGoalEntry() { }

        public LifeGoalEntry(string goalId, LifeGoalTier tier, float threshold)
        {
            goal_id = goalId;
            this.tier = tier;
            this.threshold = threshold;
            realized = false;
            realized_at_day = -1;
            realized_at_tick = -1;
        }

        public void MarkRealized(int dayRealized)
        {
            realized = true;
            realized_at_day = dayRealized;
            realized_at_tick = dayRealized;
        }
    }
}
