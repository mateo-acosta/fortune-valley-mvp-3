using System.Collections.Generic;

namespace FortuneValley.UI
{
    /// <summary>
    /// Formatted display data for an available insurance policy detail view.
    /// All strings are pre-formatted for direct UI binding.
    /// </summary>
    public readonly struct AvailablePolicyDetails
    {
        public string PolicyName { get; }
        public string PolicyType { get; }
        public string Premium { get; }
        public string Deductible { get; }
        public string Coverage { get; }
        public IReadOnlyList<string> CoveredAccidentNames { get; }

        public AvailablePolicyDetails(
            string policyName, string policyType,
            string premium, string deductible, string coverage,
            IReadOnlyList<string> coveredAccidentNames)
        {
            PolicyName = policyName;
            PolicyType = policyType;
            Premium = premium;
            Deductible = deductible;
            Coverage = coverage;
            CoveredAccidentNames = coveredAccidentNames;
        }
    }
}
