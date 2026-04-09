using System.Collections.Generic;
using UnityEngine;
using TMPro;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;
using FortuneValley.UI.Components;
using FortuneValley.UI.Popups;

namespace FortuneValley.UI.Panels.Insurance
{
    /// <summary>
    /// Insurance Home tab: per-lot coverage list with Manage buttons.
    ///
    /// LEARNING DESIGN: Students see every lot's exposure at a glance,
    /// making it easy to spot unprotected properties before accidents occur.
    /// </summary>
    public class InsuranceHomeSubPanel : SubPanelBase
    {
        // ===============================================================
        // REFERENCES
        // ===============================================================

        [Header("Dependencies")]
        [SerializeField] private CityManager _cityManager;
        [SerializeField] private InsuranceSystem _insuranceSystem;
        [SerializeField] private UIManager _uiManager;

        [Header("Policy Configs")]
        [SerializeField] private InsurancePolicyConfig _generalPolicyConfig;
        [SerializeField] private InsurancePolicyConfig _nonGeneralPolicyConfig;

        [Header("List")]
        [SerializeField] private Transform _listContainer;
        [SerializeField] private PolicyListItem _policyItemPrefab;

        [Header("Summary")]
        [SerializeField] private TextMeshProUGUI _totalPremiumText;

        // ===============================================================
        // STATE
        // ===============================================================

        private List<PolicyListItem> _listItems = new List<PolicyListItem>();

        // ===============================================================
        // LIFECYCLE
        // ===============================================================

        protected override void OnEnable()
        {
            GameEvents.OnInsurancePurchased += HandleInsuranceChanged;
            GameEvents.OnInsuranceCanceled += HandleInsuranceCanceled;
            GameEvents.OnLotPurchased += HandleLotPurchased;

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            GameEvents.OnInsurancePurchased -= HandleInsuranceChanged;
            GameEvents.OnInsuranceCanceled -= HandleInsuranceCanceled;
            GameEvents.OnLotPurchased -= HandleLotPurchased;

            base.OnDisable();
        }

        // ===============================================================
        // EVENT HANDLERS
        // ===============================================================

        private void HandleInsuranceChanged(string lotId, string policyId) => Refresh();
        private void HandleInsuranceCanceled(string lotId, InsurancePolicyType policyType) => Refresh();

        private void HandleLotPurchased(string lotId, Owner owner)
        {
            if (owner == Owner.Player) Refresh();
        }

        // ===============================================================
        // REFRESH
        // ===============================================================

        protected override void Refresh()
        {
            ClearList();

            if (_cityManager == null || _insuranceSystem == null) return;

            // Property reads only
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

                if (_policyItemPrefab != null && _listContainer != null)
                {
                    var item = Instantiate(_policyItemPrefab, _listContainer);
                    item.Setup(lot.LotId, lot.DisplayName, hasGeneral, hasNonGeneral, OnManageClicked);
                    _listItems.Add(item);
                }
            }

            if (_totalPremiumText != null && _insuranceSystem != null)
                _totalPremiumText.text = $"Monthly Premiums: ${_insuranceSystem.TotalMonthlyPremiums:N2}";
        }

        // ===============================================================
        // MANAGE BUTTON
        // ===============================================================

        private void OnManageClicked(string lotId)
        {
            if (_uiManager == null || _cityManager == null || _insuranceSystem == null) return;
            if (_generalPolicyConfig == null || _nonGeneralPolicyConfig == null) return;

            string lotName = LotDisplayHelper.GetDisplayName(_cityManager.AllLots, lotId);

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

        // ===============================================================
        // HELPERS
        // ===============================================================

        private void ClearList()
        {
            for (int i = 0; i < _listItems.Count; i++)
            {
                if (_listItems[i] != null)
                    Destroy(_listItems[i].gameObject);
            }
            _listItems.Clear();
        }

        private static bool CheckHasPolicy(
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
