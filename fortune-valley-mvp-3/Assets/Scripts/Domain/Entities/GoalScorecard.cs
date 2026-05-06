using System;

namespace FortuneValley.Domain.Entities
{
    /// <summary>
    /// Final-life snapshot of goal outcomes, produced by GameSummaryBuilder
    /// and consumed by GameEndPanel. Pure data; no logic.
    ///
    /// Renderer resolves goal display names by looking up goal_id against
    /// the LifeGoalCatalog (UI layer is permitted to import Core).
    /// </summary>
    [Serializable]
    public class GoalScorecard
    {
        public LifeGoalEntry[] realized;
        public LifeGoalEntry[] missed;
        public int retirement_age;
        public bool bankruptcy_flag;

        public GoalScorecard()
        {
            realized = Array.Empty<LifeGoalEntry>();
            missed = Array.Empty<LifeGoalEntry>();
        }

        public int RealizedCount => realized != null ? realized.Length : 0;
        public int TotalGoalCount => RealizedCount + (missed != null ? missed.Length : 0);
    }
}
