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
    /// Insurance Home tab: grid of all active policies across all lots.
    /// One card per active policy. Filterable by policy type and lot.
    ///
    /// LEARNING DESIGN: Students see every active policy at a glance,
    /// including costs and coverage, making it easy to evaluate their
    /// overall insurance portfolio.
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

        [Header("Filter Row 1 - Policy Type")]
        [SerializeField] private FilterRowController _policyTypeFilter;
        [SerializeField] private InsurancePolicyType[] _policyTypeMapping;

        [Header("Filter Row 2 - Lot Dropdown")]
        [SerializeField] private TMP_Dropdown _lotDropdown;

        [Header("Card Backgrounds")]
        [SerializeField] private Sprite _generalBackground;
        [SerializeField] private Sprite _nonGeneralBackground;

        [Header("Summary Stats")]
        [SerializeField] private TMP_Text _activeCountText;
        [SerializeField] private TMP_Text _totalPremiumText;

        [Header("Card Grid")]
        [SerializeField] private Transform _cardContainer;
        [SerializeField] private GameObject _cardPrefab;

        [Header("Empty State")]
        [SerializeField] private GameObject _emptyStateObject;

        // ===============================================================
        // STATE
        // ===============================================================

        private List<InsuranceCardItemView> _cardViews = new List<InsuranceCardItemView>();
        private List<string> _lotIdMapping;

        // ===============================================================
        // LIFECYCLE
        // ===============================================================

        protected override void OnEnable()
        {
            GameEvents.OnInsurancePurchased += HandleInsuranceChanged;
            GameEvents.OnInsuranceCanceled += HandleInsuranceCanceled;
            GameEvents.OnLotPurchased += HandleLotPurchased;
            GameEvents.OnInsurancePremiumCharged += HandlePremiumCharged;

            if (_policyTypeFilter != null)
                _policyTypeFilter.OnSelectionChanged += HandleFilterChanged;
            if (_lotDropdown != null)
                _lotDropdown.onValueChanged.AddListener(HandleLotFilterChanged);

            base.OnEnable(); // calls Refresh()
        }

        protected override void OnDisable()
        {
            GameEvents.OnInsurancePurchased -= HandleInsuranceChanged;
            GameEvents.OnInsuranceCanceled -= HandleInsuranceCanceled;
            GameEvents.OnLotPurchased -= HandleLotPurchased;
            GameEvents.OnInsurancePremiumCharged -= HandlePremiumCharged;

            if (_policyTypeFilter != null)
                _policyTypeFilter.OnSelectionChanged -= HandleFilterChanged;
            if (_lotDropdown != null)
                _lotDropdown.onValueChanged.RemoveListener(HandleLotFilterChanged);

            base.OnDisable();
        }

        // ===============================================================
        // EVENT HANDLERS
        // ===============================================================

        private void HandleInsuranceChanged(string lotId, string policyId) => Refresh();
        private void HandleInsuranceCanceled(string lotId, InsurancePolicyType type) => Refresh();
        private void HandlePremiumCharged(string lotId, string policyId, float amount) => Refresh();
        private void HandleFilterChanged(int index) => Refresh();
        private void HandleLotFilterChanged(int index) => Refresh();

        private void HandleLotPurchased(string lotId, Owner owner)
        {
            if (owner == Owner.Player) Refresh();
        }

        // ===============================================================
        // REFRESH
        // ===============================================================

        protected override void Refresh()
        {
            if (_insuranceSystem == null || _cityManager == null) return;

            // Build lot dropdown from current ownership (property reads)
            InsuranceFilterLogic.PopulateLotDropdown(
                _lotDropdown, _cityManager.AllLots, _cityManager.LotOwnership,
                out _lotIdMapping);

            // Read current filter values
            InsurancePolicyType? policyTypeFilter = MapPolicyTypeIndex(
                _policyTypeFilter != null ? _policyTypeFilter.SelectedIndex : 0);
            string lotIdFilter = GetSelectedLotId();

            // Get all policies (property read, no method call)
            var allPolicies = _insuranceSystem.Portfolio != null
                ? _insuranceSystem.Portfolio.AllPolicies
                : null;

            // Filter (static pure C# logic handles active-only filtering)
            var filtered = InsuranceFilterLogic.FilterActivePolicies(
                allPolicies, policyTypeFilter, lotIdFilter);

            // Summary stats
            InsuranceSummaryCalculator.CalculateHomeSummary(
                filtered, out int activeCount, out float totalMonthlyPremium);

            if (_activeCountText != null)
                _activeCountText.text = $"Active Policies: {activeCount}";
            if (_totalPremiumText != null)
                _totalPremiumText.text = $"Annual Premiums: ${totalMonthlyPremium:N2}";

            // Rebuild card grid
            ClearCards();

            if (_cardPrefab != null && _cardContainer != null)
            {
                var allLots = _cityManager.AllLots;

                for (int i = 0; i < filtered.Count; i++)
                {
                    var policy = filtered[i];
                    var card = Instantiate(_cardPrefab, _cardContainer);
                    var view = card.GetComponent<InsuranceCardItemView>();

                    if (view != null)
                    {
                        string lotName = LotDisplayHelper.GetDisplayName(allLots, policy.LotId);

                        view.SetName(policy.PolicyId);
                        view.SetType(policy.PolicyType.ToString());
                        view.SetPremium($"${policy.MonthlyPremium:N2}/mo");
                        view.SetDetail(lotName);
                        view.SetStatus(policy.IsPastDue ? "Past Due" : "Active");
                        view.SetBackground(InsuranceFilterLogic.GetBackgroundSprite(
                            policy.PolicyType, _generalBackground, _nonGeneralBackground));
                        view.SetActionLabel("Details");

                        // Capture for closure
                        var capturedPolicy = policy;
                        var capturedLotName = lotName;
                        view.SetActionCallback(() => ShowPolicyDetail(capturedPolicy, capturedLotName));

                        _cardViews.Add(view);
                    }
                }
            }

            // Empty state
            if (_emptyStateObject != null)
                _emptyStateObject.SetActive(filtered.Count == 0);
        }

        // ===============================================================
        // DETAIL POPUP
        // ===============================================================

        private void ShowPolicyDetail(ActiveInsurancePolicy policy, string lotDisplayName)
        {
            if (_uiManager == null) return;

            var popup = _uiManager.InsuranceDetailPopup as InsuranceDetailPopup;
            if (popup == null) return;

            var configs = _insuranceSystem != null ? _insuranceSystem.AvailablePolicies : null;
            popup.ConfigureOwnedPolicy(policy, lotDisplayName, configs);
            _uiManager.ShowPopup(popup);
        }

        // ===============================================================
        // HELPERS
        // ===============================================================

        private InsurancePolicyType? MapPolicyTypeIndex(int index)
        {
            if (index <= 0 || _policyTypeMapping == null) return null;
            int arrayIndex = index - 1;
            if (arrayIndex >= _policyTypeMapping.Length) return null;
            return _policyTypeMapping[arrayIndex];
        }

        private string GetSelectedLotId()
        {
            if (_lotDropdown == null || _lotIdMapping == null) return null;
            int idx = _lotDropdown.value;
            if (idx < 0 || idx >= _lotIdMapping.Count) return null;
            return _lotIdMapping[idx];
        }

        private void ClearCards()
        {
            for (int i = 0; i < _cardViews.Count; i++)
            {
                if (_cardViews[i] != null)
                    Destroy(_cardViews[i].gameObject);
            }
            _cardViews.Clear();
        }
    }
}
