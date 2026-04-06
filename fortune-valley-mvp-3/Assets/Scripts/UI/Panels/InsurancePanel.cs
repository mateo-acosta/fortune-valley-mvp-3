using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;
using FortuneValley.UI.Components;
using FortuneValley.UI.Popups;

namespace FortuneValley.UI.Panels
{
    /// <summary>
    /// Insurance management panel using a lot-first UX.
    /// Lists all player-owned lots with their coverage status.
    /// Tapping Manage on a lot opens InsuranceSelectionPopup.
    ///
    /// LEARNING DESIGN: Students see every lot's exposure at a glance,
    /// making it easy to spot unprotected properties before accidents occur.
    /// </summary>
    public class InsurancePanel : UIPanel
    {
        // ===============================================================
        // REFERENCES
        // ===============================================================

        [Header("List")]
        [SerializeField] private Transform _listContainer;
        [SerializeField] private PolicyListItem _policyItemPrefab;

        [Header("Summary")]
        [SerializeField] private TextMeshProUGUI _totalPremiumText;
        [SerializeField] private Button _closeButton;

        [Header("Dependencies")]
        [SerializeField] private CityManager _cityManager;
        [SerializeField] private InsuranceSystem _insuranceSystem;
        [SerializeField] private UIManager _uiManager;

        [Header("Policy Configs")]
        [Tooltip("General protection policy config used to populate InsuranceSelectionPopup")]
        [SerializeField] private InsurancePolicyConfig _generalPolicyConfig;
        [Tooltip("Non-general protection policy config used to populate InsuranceSelectionPopup")]
        [SerializeField] private InsurancePolicyConfig _nonGeneralPolicyConfig;

        // ===============================================================
        // STATE
        // ===============================================================

        private List<PolicyListItem> _listItems = new List<PolicyListItem>();

        // ===============================================================
        // LIFECYCLE
        // ===============================================================

        private void Start()
        {
            if (_cityManager == null) Debug.LogError("[InsurancePanel] _cityManager not wired.");
            if (_insuranceSystem == null) Debug.LogError("[InsurancePanel] _insuranceSystem not wired.");
            if (_uiManager == null) Debug.LogError("[InsurancePanel] _uiManager not wired.");

            if (_closeButton != null)
                _closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        private void OnEnable()
        {
            GameEvents.OnInsurancePurchased += HandleInsuranceChanged;
            GameEvents.OnInsuranceCanceled += HandleInsuranceCanceled;
            GameEvents.OnLotPurchased += HandleLotPurchased;
        }

        private void OnDisable()
        {
            GameEvents.OnInsurancePurchased -= HandleInsuranceChanged;
            GameEvents.OnInsuranceCanceled -= HandleInsuranceCanceled;
            GameEvents.OnLotPurchased -= HandleLotPurchased;
        }

        // ===============================================================
        // PANEL OVERRIDE
        // ===============================================================

        protected override void OnShow()
        {
            RefreshList();
        }

        // ===============================================================
        // EVENT HANDLERS
        // ===============================================================

        private void HandleInsuranceChanged(string lotId, string policyId)
        {
            if (IsVisible) RefreshList();
        }

        private void HandleInsuranceCanceled(string lotId, InsurancePolicyType policyType)
        {
            if (IsVisible) RefreshList();
        }

        private void HandleLotPurchased(string lotId, Owner owner)
        {
            if (IsVisible && owner == Owner.Player) RefreshList();
        }

        // ===============================================================
        // LIST MANAGEMENT
        // ===============================================================

        private void RefreshList()
        {
            ClearList();

            if (_cityManager == null || _insuranceSystem == null) return;

            // Read properties once to avoid repeated cross-layer calls
            var allLots = _cityManager.AllLots;
            var ownership = _cityManager.LotOwnership;
            var policies = _insuranceSystem.Portfolio != null
                ? _insuranceSystem.Portfolio.AllPolicies : null;

            for (int i = 0; i < allLots.Count; i++)
            {
                var lot = allLots[i];
                if (!ownership.TryGetValue(lot.LotId, out var owner) || owner != Owner.Player)
                    continue;

                bool hasGeneral = CheckHasPolicy(policies, lot.LotId, InsurancePolicyType.GeneralProtection);
                bool hasNonGeneral = CheckHasPolicy(policies, lot.LotId, InsurancePolicyType.NonGeneralProtection);

                CreateListItem(lot.LotId, lot.DisplayName, hasGeneral, hasNonGeneral);
            }

            UpdateSummary();
        }

        private void CreateListItem(string lotId, string lotName, bool hasGeneral, bool hasNonGeneral)
        {
            if (_policyItemPrefab == null || _listContainer == null) return;

            var item = Instantiate(_policyItemPrefab, _listContainer);
            item.Setup(lotId, lotName, hasGeneral, hasNonGeneral, OnManageClicked);
            _listItems.Add(item);
        }

        private void ClearList()
        {
            for (int i = 0; i < _listItems.Count; i++)
            {
                if (_listItems[i] != null)
                    Destroy(_listItems[i].gameObject);
            }
            _listItems.Clear();
        }

        private void UpdateSummary()
        {
            if (_totalPremiumText == null || _insuranceSystem == null) return;
            _totalPremiumText.text = $"Monthly Premiums: ${_insuranceSystem.TotalMonthlyPremiums:N2}";
        }

        // ===============================================================
        // MANAGE BUTTON
        // ===============================================================

        private void OnManageClicked(string lotId)
        {
            if (_uiManager == null || _cityManager == null || _insuranceSystem == null) return;
            if (_generalPolicyConfig == null || _nonGeneralPolicyConfig == null) return;

            // Find lot display name via property reads only
            string lotName = lotId;
            var allLots = _cityManager.AllLots;
            for (int i = 0; i < allLots.Count; i++)
            {
                if (allLots[i].LotId == lotId)
                {
                    lotName = allLots[i].DisplayName;
                    break;
                }
            }

            var policies = _insuranceSystem.Portfolio != null
                ? _insuranceSystem.Portfolio.AllPolicies : null;
            bool hasGeneral = CheckHasPolicy(policies, lotId, InsurancePolicyType.GeneralProtection);
            bool hasNonGeneral = CheckHasPolicy(policies, lotId, InsurancePolicyType.NonGeneralProtection);

            var popup = _uiManager.InsuranceSelectionPopup as InsuranceSelectionPopup;
            if (popup == null) return;

            popup.Configure(
                lotId, lotName,
                _generalPolicyConfig.PolicyId, _generalPolicyConfig.MonthlyPremium, _generalPolicyConfig.Deductible, hasGeneral,
                _nonGeneralPolicyConfig.PolicyId, _nonGeneralPolicyConfig.MonthlyPremium, _nonGeneralPolicyConfig.Deductible, hasNonGeneral);

            _uiManager.ShowPopup(popup);
        }

        /// <summary>
        /// Check if a lot has a specific policy type active.
        /// Uses local iteration over the property-read AllPolicies list
        /// to avoid calling methods with parameters on Core-layer references.
        /// </summary>
        private bool CheckHasPolicy(
            IReadOnlyList<ActiveInsurancePolicy> policies,
            string lotId, InsurancePolicyType policyType)
        {
            if (policies == null) return false;
            for (int i = 0; i < policies.Count; i++)
            {
                if (policies[i].LotId == lotId
                    && policies[i].PolicyType == policyType
                    && policies[i].IsActive)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
