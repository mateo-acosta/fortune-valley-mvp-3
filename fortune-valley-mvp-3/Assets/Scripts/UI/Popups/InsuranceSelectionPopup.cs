using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Core;
using FortuneValley.Domain.Enums;

namespace FortuneValley.UI.Popups
{
    /// <summary>
    /// Per-building popup for purchasing or canceling insurance policies.
    /// Shows available policy types with premiums and coverage details.
    /// Fires intent events; InsuranceSystem handles actual purchase/cancel.
    ///
    /// LEARNING DESIGN: Students compare premium costs vs deductibles,
    /// learning that cheaper insurance means higher out-of-pocket costs.
    /// </summary>
    public class InsuranceSelectionPopup : UIPopup
    {
        // ===============================================================
        // REFERENCES
        // ===============================================================

        [Header("Display")]
        [SerializeField] private TextMeshProUGUI _lotNameText;
        [SerializeField] private TextMeshProUGUI _generalStatusText;
        [SerializeField] private TextMeshProUGUI _nonGeneralStatusText;

        [Header("General Protection")]
        [SerializeField] private TextMeshProUGUI _generalPremiumText;
        [SerializeField] private TextMeshProUGUI _generalDeductibleText;
        [SerializeField] private Button _generalToggleButton;
        [SerializeField] private TextMeshProUGUI _generalToggleLabel;

        [Header("Non-General Protection")]
        [SerializeField] private TextMeshProUGUI _nonGeneralPremiumText;
        [SerializeField] private TextMeshProUGUI _nonGeneralDeductibleText;
        [SerializeField] private Button _nonGeneralToggleButton;
        [SerializeField] private TextMeshProUGUI _nonGeneralToggleLabel;

        [Header("Actions")]
        [SerializeField] private Button _closeButton;

        // ===============================================================
        // STATE
        // ===============================================================

        private string _currentLotId;
        private string _generalPolicyId;
        private string _nonGeneralPolicyId;
        private bool _hasGeneralPolicy;
        private bool _hasNonGeneralPolicy;

        // ===============================================================
        // LIFECYCLE
        // ===============================================================

        private void Start()
        {
            if (_generalToggleButton != null)
                _generalToggleButton.onClick.AddListener(OnGeneralToggleClicked);
            if (_nonGeneralToggleButton != null)
                _nonGeneralToggleButton.onClick.AddListener(OnNonGeneralToggleClicked);
            if (_closeButton != null)
                _closeButton.onClick.AddListener(OnCancelClicked);
        }

        private void OnEnable()
        {
            GameEvents.OnInsurancePurchased += HandleInsurancePurchased;
            GameEvents.OnInsuranceCanceled += HandleInsuranceCanceled;
        }

        private void OnDisable()
        {
            GameEvents.OnInsurancePurchased -= HandleInsurancePurchased;
            GameEvents.OnInsuranceCanceled -= HandleInsuranceCanceled;
        }

        // ===============================================================
        // CONFIGURATION
        // ===============================================================

        /// <summary>
        /// Configure the popup for a specific lot.
        /// Called by the controller before Show().
        /// </summary>
        public void Configure(
            string lotId, string lotName,
            string generalPolicyId, float generalPremium, float generalDeductible, bool hasGeneral,
            string nonGeneralPolicyId, float nonGeneralPremium, float nonGeneralDeductible, bool hasNonGeneral)
        {
            _currentLotId = lotId;
            _generalPolicyId = generalPolicyId;
            _nonGeneralPolicyId = nonGeneralPolicyId;
            _hasGeneralPolicy = hasGeneral;
            _hasNonGeneralPolicy = hasNonGeneral;

            if (_lotNameText != null)
                _lotNameText.text = lotName;

            // General protection details
            if (_generalPremiumText != null)
                _generalPremiumText.text = $"Premium: ${generalPremium:N2}/mo";
            if (_generalDeductibleText != null)
                _generalDeductibleText.text = $"Deductible: ${generalDeductible:N2}";

            // Non-general protection details
            if (_nonGeneralPremiumText != null)
                _nonGeneralPremiumText.text = $"Premium: ${nonGeneralPremium:N2}/mo";
            if (_nonGeneralDeductibleText != null)
                _nonGeneralDeductibleText.text = $"Deductible: ${nonGeneralDeductible:N2}";

            UpdateToggleDisplay();
        }

        // ===============================================================
        // EVENT HANDLERS
        // ===============================================================

        private void HandleInsurancePurchased(string lotId, string policyId)
        {
            if (lotId != _currentLotId) return;

            if (policyId == _generalPolicyId) _hasGeneralPolicy = true;
            else if (policyId == _nonGeneralPolicyId) _hasNonGeneralPolicy = true;

            UpdateToggleDisplay();
        }

        private void HandleInsuranceCanceled(string lotId, InsurancePolicyType policyType)
        {
            if (lotId != _currentLotId) return;

            if (policyType == InsurancePolicyType.GeneralProtection) _hasGeneralPolicy = false;
            else if (policyType == InsurancePolicyType.NonGeneralProtection) _hasNonGeneralPolicy = false;

            UpdateToggleDisplay();
        }

        // ===============================================================
        // BUTTON CALLBACKS
        // ===============================================================

        private void OnGeneralToggleClicked()
        {
            if (_hasGeneralPolicy)
                GameEvents.RaiseCancelInsuranceRequested(_currentLotId, InsurancePolicyType.GeneralProtection);
            else
                GameEvents.RaisePurchaseInsuranceRequested(_currentLotId, _generalPolicyId);
        }

        private void OnNonGeneralToggleClicked()
        {
            if (_hasNonGeneralPolicy)
                GameEvents.RaiseCancelInsuranceRequested(_currentLotId, InsurancePolicyType.NonGeneralProtection);
            else
                GameEvents.RaisePurchaseInsuranceRequested(_currentLotId, _nonGeneralPolicyId);
        }

        // ===============================================================
        // DISPLAY
        // ===============================================================

        private void UpdateToggleDisplay()
        {
            if (_generalToggleLabel != null)
                _generalToggleLabel.text = _hasGeneralPolicy ? "Cancel" : "Purchase";
            if (_generalStatusText != null)
                _generalStatusText.text = _hasGeneralPolicy ? "Active" : "Not Active";

            if (_nonGeneralToggleLabel != null)
                _nonGeneralToggleLabel.text = _hasNonGeneralPolicy ? "Cancel" : "Purchase";
            if (_nonGeneralStatusText != null)
                _nonGeneralStatusText.text = _hasNonGeneralPolicy ? "Active" : "Not Active";
        }
    }
}
