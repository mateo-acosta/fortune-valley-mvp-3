using System;

namespace FortuneValley.Domain.Entities.WebPanels
{
    /// <summary>
    /// One of the player's three picked Life Goals, shaped for the
    /// PlayerProfile iframe constellation. The bridge derives realized_age
    /// from LifeGoalEntry.realized_at_day so the panel does not have to
    /// know about the day-to-age conversion.
    ///
    /// just_realized is a one-shot flag the bridge sets the same frame a
    /// goal flips realized=true; the iframe reads it once to play the
    /// bloom animation, then the bridge clears it on the next push.
    /// </summary>
    [Serializable]
    public class ProfileGoalRowDTO
    {
        public string goal_id;          // matches LifeGoalSO.GoalId
        public int tier;                // 0 = Starter, 1 = Mid, 2 = Ambitious
        public float threshold;         // total net worth threshold
        public bool realized;
        public int realized_age;        // -1 if not realized
        public bool just_realized;      // one-shot bloom trigger
    }
}
