using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;

namespace FortuneValley.UI.Popups
{
    /// <summary>
    /// Shared detail popup for insurance information.
    /// Three togglable content panels: owned policy, available policy, transaction.
    /// Uses InsuranceDetailFormatter for all formatting and comparison logic.
    ///
    /// LEARNING DESIGN: The coverage comparison section is the strongest
    /// learning signal, showing both insured and uninsured students
    /// the concrete value (or missed value) of insurance coverage.
    /// </summary>
    public class InsuranceDetailPopup : UIPopup
    {
        // ===============================================================
        // CONTENT PANELS
        // ===============================================================

        [Header("Panel 1 - Owned Policy")]
        [SerializeField] private GameObject _ownedPolicyPanel;
        [SerializeField] private TMP_Text _ownedNameText;
        [SerializeField] private TMP_Text _ownedTypeText;
        [SerializeField] private TMP_Text _ownedLotText;
        [SerializeField] private TMP_Text _ownedPremiumText;
        [SerializeField] private TMP_Text _ownedDeductibleText;
        [SerializeField] private TMP_Text _ownedCoverageText;
        [SerializeField] private TMP_Text _ownedPremiumsPaidText;
        [SerializeField] private TMP_Text _ownedStatusText;
        [SerializeField] private TMP_Text _ownedCoveredAccidentsText;
        [SerializeField] private Button _cancelPolicyButton;

        [Header("Panel 2 - Available Policy")]
        [SerializeField] private GameObject _availablePolicyPanel;
        [SerializeField] private TMP_Text _availableNameText;
        [SerializeField] private TMP_Text _availableTypeText;
        [SerializeField] private TMP_Text _availablePremiumText;
        [SerializeField] private TMP_Text _availableDeductibleText;
        [SerializeField] private TMP_Text _availableCoverageText;
        [SerializeField] private TMP_Text _availableCoveredAccidentsText;

        [Header("Panel 3 - Transaction")]
        [SerializeField] private GameObject _transactionPanel;
        [SerializeField] private TMP_Text _transactionTypeText;
        [SerializeField] private TMP_Text _transactionLotText;
        [SerializeField] private TMP_Text _transactionAmountText;
        [SerializeField] private TMP_Text _transactionDescriptionText;

        [Header("Comparison Section (inside Transaction panel)")]
        [SerializeField] private GameObject _comparisonSection;
        [SerializeField] private TMP_Text _comparisonText;

        // ===============================================================
        // STATE
        // ===============================================================

        private string _currentLotId;
        private InsurancePolicyType _currentPolicyType;

        // ===============================================================
        // CONFIGURE METHODS
        // ===============================================================

        /// <summary>
        /// Configure for owned policy detail (from Home tab).
        /// </summary>
        public void ConfigureOwnedPolicy(
            ActiveInsurancePolicy policy,
            string lotDisplayName,
            IReadOnlyList<InsurancePolicyConfig> configs)
        {
            HideAllPanels();
            if (_ownedPolicyPanel != null) _ownedPolicyPanel.SetActive(true);

            _currentLotId = policy.LotId;
            _currentPolicyType = policy.PolicyType;

            var details = InsuranceDetailFormatter.FormatOwnedPolicy(policy, lotDisplayName);

            SetText(_ownedNameText, details.PolicyName);
            SetText(_ownedTypeText, details.PolicyType);
            SetText(_ownedLotText, details.LotName);
            SetText(_ownedPremiumText, details.Premium);
            SetText(_ownedDeductibleText, details.Deductible);
            SetText(_ownedCoverageText, details.Coverage);
            SetText(_ownedPremiumsPaidText, details.TotalPremiumsPaid);
            SetText(_ownedStatusText, details.Status);

            if (_ownedCoveredAccidentsText != null)
            {
                _ownedCoveredAccidentsText.text = details.CoveredAccidentIds.Count > 0
                    ? string.Join(", ", details.CoveredAccidentIds)
                    : "None";
            }

            // Wire cancel button
            if (_cancelPolicyButton != null)
            {
                _cancelPolicyButton.onClick.RemoveAllListeners();
                _cancelPolicyButton.onClick.AddListener(HandleCancelPolicy);
            }
        }

        /// <summary>
        /// Configure for available policy detail (from Explore tab).
        /// </summary>
        public void ConfigureAvailablePolicy(InsurancePolicyConfig config)
        {
            HideAllPanels();
            if (_availablePolicyPanel != null) _availablePolicyPanel.SetActive(true);

            var details = InsuranceDetailFormatter.FormatAvailablePolicy(config);

            SetText(_availableNameText, details.PolicyName);
            SetText(_availableTypeText, details.PolicyType);
            SetText(_availablePremiumText, details.Premium);
            SetText(_availableDeductibleText, details.Deductible);
            SetText(_availableCoverageText, details.Coverage);

            if (_availableCoveredAccidentsText != null)
            {
                _availableCoveredAccidentsText.text = details.CoveredAccidentNames.Count > 0
                    ? string.Join(", ", details.CoveredAccidentNames)
                    : "None";
            }
        }

        /// <summary>
        /// Configure for transaction detail (from History tab).
        /// Shows coverage comparison for AccidentResolved transactions.
        /// </summary>
        public void ConfigureTransaction(
            TransactionRecord record,
            IReadOnlyList<InsurancePolicyConfig> configs)
        {
            HideAllPanels();
            if (_transactionPanel != null) _transactionPanel.SetActive(true);

            var details = InsuranceDetailFormatter.FormatTransaction(record);

            SetText(_transactionTypeText, details.TypeLabel);
            SetText(_transactionLotText, details.LotId);
            SetText(_transactionAmountText, details.Amount);
            SetText(_transactionDescriptionText, details.Description);

            // Show comparison for all accident resolutions
            bool showComparison = record.Type == TransactionType.AccidentResolved;
            if (_comparisonSection != null)
                _comparisonSection.SetActive(showComparison);

            if (showComparison && _comparisonText != null)
            {
                // Parse wasCovered from description (contains "covered by insurance" or "uninsured")
                bool wasCovered = record.Description != null
                    && record.Description.Contains("covered by insurance");

                // For AccidentResolved, Amount is the player cost.
                // We need total damage; parse from description or estimate.
                // Description format: "Fire at lot_1: $1000 damage (covered...), you paid $200"
                float playerCost = record.Amount;
                float totalDamage = playerCost; // fallback if parsing fails

                // Try to parse total damage from description
                int dollarIdx = record.Description != null ? record.Description.IndexOf('$') : -1;
                if (dollarIdx >= 0)
                {
                    int spaceIdx = record.Description.IndexOf(' ', dollarIdx + 1);
                    if (spaceIdx > dollarIdx + 1)
                    {
                        string numStr = record.Description.Substring(dollarIdx + 1, spaceIdx - dollarIdx - 1);
                        numStr = numStr.Replace(",", "");
                        float.TryParse(numStr, out totalDamage);
                    }
                }

                var comparison = InsuranceDetailFormatter.CalculateBestCoverageComparison(
                    totalDamage, playerCost, wasCovered, configs);

                _comparisonText.text = comparison.HasComparison
                    ? comparison.ComparisonText
                    : "No coverage options available for comparison.";
            }
        }

        // ===============================================================
        // CANCEL POLICY
        // ===============================================================

        private void HandleCancelPolicy()
        {
            GameEvents.RaiseCancelInsuranceRequested(_currentLotId, _currentPolicyType);
            OnCancelClicked(); // closes popup via UIPopup base
        }

        // ===============================================================
        // HELPERS
        // ===============================================================

        private void HideAllPanels()
        {
            if (_ownedPolicyPanel != null) _ownedPolicyPanel.SetActive(false);
            if (_availablePolicyPanel != null) _availablePolicyPanel.SetActive(false);
            if (_transactionPanel != null) _transactionPanel.SetActive(false);
            if (_comparisonSection != null) _comparisonSection.SetActive(false);
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null) text.text = value;
        }
    }
}
