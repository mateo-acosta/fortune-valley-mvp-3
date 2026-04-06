using System.Collections.Generic;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Domain.Entities
{
    /// <summary>
    /// Runtime state for an active insurance policy on a specific lot.
    /// Stores copied primitive values from config at creation time
    /// (Domain layer cannot reference Core ScriptableObjects).
    ///
    /// LEARNING DESIGN: Students see the ongoing cost of insurance
    /// premiums and learn that coverage only pays off when accidents occur.
    /// </summary>
    [System.Serializable]
    public class ActiveInsurancePolicy
    {
        private string _policyId;
        private string _lotId;
        private InsurancePolicyType _policyType;
        private float _monthlyPremium;
        private float _deductible;
        private float _coveragePercent;
        private List<string> _coveredAccidentIds;
        private int _startDay;
        private bool _isActive;
        private bool _isPastDue;
        private float _totalPremiumsPaid;

        public ActiveInsurancePolicy(
            string policyId,
            string lotId,
            InsurancePolicyType policyType,
            float monthlyPremium,
            float deductible,
            float coveragePercent,
            List<string> coveredAccidentIds,
            int startDay)
        {
            _policyId = policyId;
            _lotId = lotId;
            _policyType = policyType;
            _monthlyPremium = monthlyPremium;
            _deductible = deductible;
            _coveragePercent = coveragePercent;
            _coveredAccidentIds = coveredAccidentIds ?? new List<string>();
            _startDay = startDay;
            _isActive = true;
            _isPastDue = false;
            _totalPremiumsPaid = 0f;
        }

        // Read-only accessors
        public string PolicyId => _policyId;
        public string LotId => _lotId;
        public InsurancePolicyType PolicyType => _policyType;
        public float MonthlyPremium => _monthlyPremium;
        public float Deductible => _deductible;
        public float CoveragePercent => _coveragePercent;
        public IReadOnlyList<string> CoveredAccidentIds => _coveredAccidentIds;
        public int StartDay => _startDay;
        public bool IsActive => _isActive;
        public bool IsPastDue => _isPastDue;
        public float TotalPremiumsPaid => _totalPremiumsPaid;

        /// <summary>
        /// Check if this policy covers a specific accident type.
        /// </summary>
        public bool CoversAccident(string accidentId)
        {
            if (!_isActive) return false;

            for (int i = 0; i < _coveredAccidentIds.Count; i++)
            {
                if (_coveredAccidentIds[i] == accidentId) return true;
            }
            return false;
        }

        /// <summary>
        /// Calculate the player's cost for a covered accident.
        /// Returns the deductible amount (player pays deductible, insurance covers the rest).
        /// </summary>
        public float CalculateCoveredCost(float damageCost)
        {
            // Player pays the deductible, capped at the damage cost
            return _deductible < damageCost ? _deductible : damageCost;
        }

        /// <summary>
        /// Record a premium payment.
        /// </summary>
        public void RecordPremiumPaid()
        {
            _totalPremiumsPaid += _monthlyPremium;
            _isPastDue = false;
        }

        /// <summary>
        /// Mark this policy's premium as past due (CC charge failed).
        /// </summary>
        public void MarkPastDue()
        {
            _isPastDue = true;
        }

        /// <summary>
        /// Deactivate this policy (player canceled or policy lapsed).
        /// </summary>
        public void Deactivate()
        {
            _isActive = false;
        }

        /// <summary>
        /// Reactivate a past-due policy (premium was paid).
        /// </summary>
        public void Reactivate()
        {
            _isActive = true;
            _isPastDue = false;
        }
    }
}
