using System;

namespace FortuneValley.Domain.Entities
{
    [Serializable]
    public class ActiveInsurancePolicyDTO
    {
        public string policy_id;
        public string lot_id;
        public string policy_type;
        public float monthly_premium;
        public float deductible;
        public int start_day;
    }
}
