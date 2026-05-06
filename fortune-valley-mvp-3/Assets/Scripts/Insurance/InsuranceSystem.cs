using UnityEngine;
using System.Collections.Generic;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;
using FortuneValley.Domain.Interfaces;

namespace FortuneValley.Core
{
    /// <summary>
    /// Manages insurance policies and resolves accident claims.
    /// Subscribes to purchase/cancel intent events from UI and
    /// accident events from AccidentSystem. Delegates collection
    /// logic to InsurancePortfolio (pure C#).
    ///
    /// LEARNING DESIGN: Students learn that insurance is a trade-off:
    /// pay a small regular cost (premium) to avoid large unexpected costs.
    /// Seeing uninsured losses makes the value of coverage tangible.
    ///
    /// Implements IBankruptcyResettable: on soft bankruptcy, all active
    /// policies are dropped (lots returned to "for sale" lose coverage too).
    /// </summary>
    public class InsuranceSystem : MonoBehaviour, IBankruptcyResettable
    {
        // ===============================================================
        // CONFIGURATION
        // ===============================================================

        [Header("Available Policies")]
        [Tooltip("All insurance policy types available for purchase")]
        [SerializeField] private List<InsurancePolicyConfig> _availablePolicies;

        [Header("Debug")]
        [SerializeField] private bool _logTransactions;

        // ===============================================================
        // RUNTIME STATE
        // ===============================================================

        private InsurancePortfolio _portfolio;

        // ===============================================================
        // PUBLIC ACCESSORS
        // ===============================================================

        /// <summary>
        /// Access the portfolio for reading policy state.
        /// </summary>
        public InsurancePortfolio Portfolio => _portfolio;

        /// <summary>
        /// Available policy configs for browsing in UI.
        /// </summary>
        public IReadOnlyList<InsurancePolicyConfig> AvailablePolicies => _availablePolicies;

        /// <summary>
        /// Total monthly premiums across all active policies.
        /// </summary>
        public float TotalMonthlyPremiums => _portfolio != null
            ? _portfolio.GetTotalMonthlyPremiums()
            : 0f;

        // ===============================================================
        // LIFECYCLE
        // ===============================================================

        private void OnEnable()
        {
            // POC: insurance system disabled. Skip all subscriptions so the
            // system never reacts to purchase/cancel/accident events.
            if (!FeatureFlags.InsuranceEnabled) return;

            GameEvents.OnGameStart += HandleGameStart;
            GameEvents.OnPurchaseInsuranceRequested += HandlePurchaseRequested;
            GameEvents.OnCancelInsuranceRequested += HandleCancelRequested;
            GameEvents.OnAccidentOccurred += HandleAccidentOccurred;
        }

        private void OnDisable()
        {
            if (!FeatureFlags.InsuranceEnabled) return;

            GameEvents.OnGameStart -= HandleGameStart;
            GameEvents.OnPurchaseInsuranceRequested -= HandlePurchaseRequested;
            GameEvents.OnCancelInsuranceRequested -= HandleCancelRequested;
            GameEvents.OnAccidentOccurred -= HandleAccidentOccurred;
        }

        private void HandleGameStart()
        {
            _portfolio = new InsurancePortfolio();
        }

        /// <summary>
        /// IBankruptcyResettable. Soft reset: drop all active policies
        /// (the lots they covered are also being released to "for sale").
        /// </summary>
        public void OnBankruptcyReset()
        {
            _portfolio = new InsurancePortfolio();
        }

        // ===============================================================
        // PURCHASE / CANCEL (via intent events from UI)
        // ===============================================================

        private const int InitialStartDay = 0;

        private void HandlePurchaseRequested(string lotId, string policyId)
        {
            if (_portfolio == null || _availablePolicies == null) return;

            // Delegate config lookup to pure C# helper
            InsurancePolicyConfig config = InsurancePortfolio.FindPolicyConfig(_availablePolicies, policyId);
            if (config == null)
            {
                if (_logTransactions)
                    Debug.Log($"[InsuranceSystem] Policy '{policyId}' not found.");
                return;
            }

            // Delegate covered ID extraction to pure C# helper
            var coveredIds = InsurancePortfolio.BuildCoveredAccidentIds(config);

            var policy = new ActiveInsurancePolicy(
                config.PolicyId,
                lotId,
                config.PolicyType,
                config.MonthlyPremium,
                config.Deductible,
                config.CoveragePercent,
                coveredIds,
                InitialStartDay
            );

            bool added = _portfolio.Add(policy);

            if (added)
            {
                if (_logTransactions)
                    Debug.Log($"[InsuranceSystem] Purchased {config.DisplayName} for lot {lotId}.");

                GameEvents.RaiseInsurancePurchased(lotId, policyId);
            }
            else
            {
                if (_logTransactions)
                    Debug.Log($"[InsuranceSystem] Duplicate policy rejected: {config.DisplayName} on lot {lotId}.");
            }
        }

        private void HandleCancelRequested(string lotId, InsurancePolicyType policyType)
        {
            if (_portfolio == null) return;

            // Look up fee before canceling (Cancel deactivates the policy)
            float cancellationFee = _portfolio.GetCancellationFee(lotId, policyType);

            bool canceled = _portfolio.Cancel(lotId, policyType);
            if (!canceled) return;

            // Charge 50% cancellation fee to credit card
            // Fee always goes through (adds to CC debt if needed, consistent with all CC charges)
            if (cancellationFee > 0f)
            {
                GameEvents.RaiseCreditCardChargeRequested(
                    cancellationFee,
                    $"Insurance cancellation fee: {policyType} on {lotId}");
            }

            if (_logTransactions)
                Debug.Log($"[InsuranceSystem] Canceled {policyType} on lot {lotId}. Fee: ${cancellationFee:F2}");

            GameEvents.RaiseInsuranceCanceled(lotId, policyType);
        }

        // ===============================================================
        // ACCIDENT RESOLUTION (sole handler per Review Decision #3)
        // ===============================================================

        private void HandleAccidentOccurred(AccidentRollResult accident)
        {
            if (_portfolio == null) return;

            // Check if any active policy covers this accident
            var coveringPolicy = _portfolio.FindCoverage(accident.LotId, accident.AccidentId);
            bool wasCovered = coveringPolicy != null;
            float playerCost;

            if (wasCovered)
            {
                // Player pays deductible only
                playerCost = coveringPolicy.CalculateCoveredCost(accident.DamageCost);
            }
            else
            {
                // Player pays full repair cost
                playerCost = accident.DamageCost;
            }

            // Charge to credit card
            GameEvents.RaiseCreditCardChargeRequested(playerCost, $"Accident: {accident.AccidentName}");

            if (_logTransactions)
            {
                string coverageStatus = wasCovered ? $"covered (deductible: ${playerCost:F2})" : $"NOT covered (full cost: ${playerCost:F2})";
                Debug.Log($"[InsuranceSystem] Accident '{accident.AccidentName}' on lot {accident.LotId}: {coverageStatus}");
            }

            GameEvents.RaiseAccidentResolved(accident.LotId, accident.AccidentName, accident.DamageCost, wasCovered, playerCost);
        }

        // ===============================================================
        // PREMIUM CHARGING (called by MonthlyPaymentDayController)
        // ===============================================================

        /// <summary>
        /// Charge monthly premiums for all active policies to the credit card.
        /// Called by MonthlyPaymentDayController on payment day.
        /// Delegates loop to InsurancePortfolio.
        /// </summary>
        public void ChargePremiums()
        {
            if (_portfolio == null) return;

            _portfolio.ProcessPremiums(
                GameEvents.RaiseCreditCardChargeRequested,
                GameEvents.RaiseInsurancePremiumCharged);
        }
    }
}
