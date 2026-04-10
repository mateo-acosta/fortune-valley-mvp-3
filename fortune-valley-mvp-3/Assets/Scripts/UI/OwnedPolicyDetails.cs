using System.Collections.Generic;

namespace FortuneValley.UI
{
    /// <summary>
    /// Formatted display data for an owned insurance policy detail view.
    /// All strings are pre-formatted for direct UI binding.
    /// </summary>
    public readonly struct OwnedPolicyDetails
    {
        public string PolicyName { get; }
        public string PolicyType { get; }
        public string LotName { get; }
        public string Premium { get; }
        public string Deductible { get; }
        public string Coverage { get; }
        public string TotalPremiumsPaid { get; }
        public string Status { get; }
        public IReadOnlyList<string> CoveredAccidentIds { get; }

        public OwnedPolicyDetails(
            string policyName, string policyType, string lotName,
            string premium, string deductible, string coverage,
            string totalPremiumsPaid, string status,
            IReadOnlyList<string> coveredAccidentIds)
        {
            PolicyName = policyName;
            PolicyType = policyType;
            LotName = lotName;
            Premium = premium;
            Deductible = deductible;
            Coverage = coverage;
            TotalPremiumsPaid = totalPremiumsPaid;
            Status = status;
            CoveredAccidentIds = coveredAccidentIds;
        }
    }
}
