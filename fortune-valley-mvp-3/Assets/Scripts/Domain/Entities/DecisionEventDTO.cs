using System;

namespace FortuneValley.Domain.Entities
{
    /// <summary>
    /// Serializable DTO for a financial decision event sent to the FDLS.
    /// Matches the Rails game_decision_events schema.
    /// </summary>
    [Serializable]
    public class DecisionEventDTO
    {
        public string session_id;
        public string game_mode;
        public int in_game_day;
        public string decision_type;
        public string instrument_id;
        public float gross_amount;
        public string category;
        public DecisionLineItemDTO[] line_items;
    }
}
