using System;

namespace FortuneValley.Domain.Entities
{
    [Serializable]
    public class ActiveLoanDTO
    {
        public string loan_id;
        public string lot_id;
        public float principal;
        public float remaining_balance;

        // Legacy "monthly" naming (Stage 0a alias chain). Removed in Stage 0c.
        public float monthly_payment;
        public int payments_made;
        public int term_months;
        public float apr;
        public float down_payment;
        public int start_day;

        // Stage 0a additions: new "tick" naming written in parallel with legacy
        // fields. Hydration in Stage 0b will prefer the new fields and fall
        // back to the legacy fields when loading older saves.
        public float yearly_payment;
        public int term_ticks;
        public int start_tick;
    }
}
