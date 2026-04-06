using System.Collections.Generic;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Core
{
    /// <summary>
    /// Manages the collection of active insurance policies.
    /// Pure C# class extracted from InsuranceSystem to keep
    /// loops and collection logic out of MonoBehaviours.
    /// </summary>
    public class InsurancePortfolio
    {
        private readonly List<ActiveInsurancePolicy> _policies = new List<ActiveInsurancePolicy>();

        public IReadOnlyList<ActiveInsurancePolicy> AllPolicies => _policies;

        /// <summary>
        /// Add a new policy to the portfolio.
        /// Returns false if a policy of the same type already exists on this lot.
        /// </summary>
        public bool Add(ActiveInsurancePolicy policy)
        {
            if (policy == null) return false;

            // Check for duplicate (same lot + same type)
            for (int i = 0; i < _policies.Count; i++)
            {
                if (_policies[i].LotId == policy.LotId
                    && _policies[i].PolicyType == policy.PolicyType
                    && _policies[i].IsActive)
                {
                    return false;
                }
            }

            _policies.Add(policy);
            return true;
        }

        /// <summary>
        /// Deactivate a policy on a specific lot.
        /// Returns false if no matching active policy found.
        /// </summary>
        public bool Cancel(string lotId, InsurancePolicyType policyType)
        {
            for (int i = 0; i < _policies.Count; i++)
            {
                if (_policies[i].LotId == lotId
                    && _policies[i].PolicyType == policyType
                    && _policies[i].IsActive)
                {
                    _policies[i].Deactivate();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Get all active policies for a specific lot.
        /// </summary>
        public List<ActiveInsurancePolicy> GetForLot(string lotId)
        {
            var result = new List<ActiveInsurancePolicy>();
            for (int i = 0; i < _policies.Count; i++)
            {
                if (_policies[i].LotId == lotId && _policies[i].IsActive)
                {
                    result.Add(_policies[i]);
                }
            }
            return result;
        }

        /// <summary>
        /// Check if a lot has a specific policy type active.
        /// </summary>
        public bool HasPolicy(string lotId, InsurancePolicyType policyType)
        {
            for (int i = 0; i < _policies.Count; i++)
            {
                if (_policies[i].LotId == lotId
                    && _policies[i].PolicyType == policyType
                    && _policies[i].IsActive)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Get all active policies (for premium charging).
        /// </summary>
        public List<ActiveInsurancePolicy> GetAllActive()
        {
            var result = new List<ActiveInsurancePolicy>();
            for (int i = 0; i < _policies.Count; i++)
            {
                if (_policies[i].IsActive)
                {
                    result.Add(_policies[i]);
                }
            }
            return result;
        }

        /// <summary>
        /// Find the best covering policy for an accident on a lot.
        /// Returns null if no policy covers this accident.
        /// </summary>
        public ActiveInsurancePolicy FindCoverage(string lotId, string accidentId)
        {
            for (int i = 0; i < _policies.Count; i++)
            {
                if (_policies[i].LotId == lotId
                    && _policies[i].IsActive
                    && _policies[i].CoversAccident(accidentId))
                {
                    return _policies[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Sum of all active policy monthly premiums.
        /// </summary>
        public float GetTotalMonthlyPremiums()
        {
            float total = 0f;
            for (int i = 0; i < _policies.Count; i++)
            {
                if (_policies[i].IsActive)
                {
                    total += _policies[i].MonthlyPremium;
                }
            }
            return total;
        }

        /// <summary>
        /// Process premium charges for all active policies.
        /// Calls the provided action for each active policy's premium.
        /// Keeps loops out of MonoBehaviours.
        /// </summary>
        public void ProcessPremiums(System.Action<float, string> onChargeRequested)
        {
            for (int i = 0; i < _policies.Count; i++)
            {
                if (!_policies[i].IsActive) continue;

                var policy = _policies[i];
                onChargeRequested(policy.MonthlyPremium, $"Insurance premium: {policy.PolicyId}");
                policy.RecordPremiumPaid();
            }
        }

        /// <summary>
        /// Clear all policies (game reset / bankruptcy).
        /// </summary>
        public void Clear()
        {
            _policies.Clear();
        }

        // ===============================================================
        // STATIC HELPERS (config lookups)
        // ===============================================================

        /// <summary>
        /// Find a policy config by ID from a list of available configs.
        /// </summary>
        public static InsurancePolicyConfig FindPolicyConfig(
            System.Collections.Generic.IReadOnlyList<InsurancePolicyConfig> configs, string policyId)
        {
            for (int i = 0; i < configs.Count; i++)
            {
                if (configs[i].PolicyId == policyId)
                    return configs[i];
            }
            return null;
        }

        /// <summary>
        /// Build a list of covered accident IDs from a policy config.
        /// </summary>
        public static List<string> BuildCoveredAccidentIds(InsurancePolicyConfig config)
        {
            var ids = new List<string>();
            var covered = config.CoveredAccidents;
            for (int i = 0; i < covered.Count; i++)
            {
                if (covered[i] != null)
                    ids.Add(covered[i].AccidentId);
            }
            return ids;
        }
    }
}
