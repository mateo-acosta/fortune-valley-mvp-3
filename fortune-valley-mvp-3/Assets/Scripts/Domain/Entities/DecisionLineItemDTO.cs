using System;

namespace FortuneValley.Domain.Entities
{
    /// <summary>
    /// Serializable DTO for a single line item within a decision event.
    /// Matches the Rails game_decision_line_items schema.
    /// </summary>
    [Serializable]
    public class DecisionLineItemDTO
    {
        public string account_affected;
        public float change_amount;
        public string flow_category;
        public string budget_category;
        public float running_balance;
    }
}
