using System.Collections.Generic;
using UnityEngine;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Core
{
    /// <summary>
    /// Configuration for an insurance policy type.
    /// Each policy covers specific accident types and has its own
    /// premium, deductible, and coverage percentage.
    ///
    /// LEARNING DESIGN: Students compare premium cost vs deductible vs
    /// coverage, learning that cheaper insurance has higher out-of-pocket costs.
    /// </summary>
    [CreateAssetMenu(fileName = "InsurancePolicyConfig", menuName = "Fortune Valley/Insurance Policy Config")]
    public class InsurancePolicyConfig : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique identifier for this policy type")]
        [SerializeField] private string _policyId;

        [Tooltip("Display name shown to the player")]
        [SerializeField] private string _displayName;

        [Tooltip("General or Non-General protection")]
        [SerializeField] private InsurancePolicyType _policyType;

        [Header("Costs")]
        [Tooltip("Monthly premium charged to credit card")]
        [SerializeField] private float _monthlyPremium;

        [Tooltip("Amount the player pays out of pocket per claim")]
        [SerializeField] private float _deductible;

        [Tooltip("Percentage of damage cost covered by insurance (0 to 1)")]
        [SerializeField] private float _coveragePercent;

        [Header("Coverage")]
        [Tooltip("Accident types this policy covers")]
        [SerializeField] private List<AccidentDefinition> _coveredAccidents;

        // Read-only accessors
        public string PolicyId => _policyId;
        public string DisplayName => _displayName;
        public InsurancePolicyType PolicyType => _policyType;
        public float MonthlyPremium => _monthlyPremium;
        public float Deductible => _deductible;
        public float CoveragePercent => _coveragePercent;
        public IReadOnlyList<AccidentDefinition> CoveredAccidents => _coveredAccidents;
    }
}
