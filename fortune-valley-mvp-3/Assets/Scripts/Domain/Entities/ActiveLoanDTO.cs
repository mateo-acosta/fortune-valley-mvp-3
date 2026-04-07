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
        public float monthly_payment;
        public int payments_made;
        public int term_months;
        public float apr;
        public float down_payment;
        public int start_day;
    }
}
