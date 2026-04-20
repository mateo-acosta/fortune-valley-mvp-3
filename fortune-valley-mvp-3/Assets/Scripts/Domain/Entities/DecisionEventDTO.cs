using System;

namespace FortuneValley.Domain.Entities
{
    /// <summary>
    /// Serializable DTO for a financial decision event sent to the FDLS.
    /// Matches the Rails game_decision_events schema.
    /// metadata_json carries a pre-serialized JSON hash of per-type details
    /// (quiz answer keys, loan context, etc). Rails parses it on create.
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
        public string quiz_category;      // populated only for decision_type == "quiz_answer"
        public long client_timestamp_ms;  // Unix ms from client; preserves ordering under burst flushes
        public string metadata_json;      // raw JSON string; Rails parses into jsonb
        public DecisionLineItemDTO[] line_items;
    }
}
